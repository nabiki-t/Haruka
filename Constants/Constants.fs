//=============================================================================
// Haruka Software Storage.
// Constatns.fs : Defines the commonly used constatns in Haruka project.
//

//=============================================================================
// Namespace declaration

/// <summary>
///   Definitions of constant values, that used commonly in Haruka project.
/// </summary>
namespace Haruka.Constants

//=============================================================================
// Import declaration

open System
open System.Text.RegularExpressions
open System.Runtime.InteropServices

//=============================================================================
// iSCSI Initiator Opcode values

type OpcodeCd =
    /// Initiator Opcode : NOP-Out. 
    | NOP_OUT = 0x00uy
    /// Initiator Opcode : SCSI Command
    | SCSI_COMMAND = 0x01uy
    /// Initiator Opcode : SCSI task management request
    | SCSI_TASK_MGR_REQ = 0x02uy
    /// Initiator Opcode : Login request
    | LOGIN_REQ = 0x03uy
    /// Initiator Opcode : Text request
    | TEXT_REQ = 0x04uy
    /// Initiator Opcode : SCSI Data-Out
    | SCSI_DATA_OUT = 0x05uy
    /// Initiator Opcode : Logout request
    | LOGOUT_REQ = 0x06uy
    /// Initiator Opcode : SNACK request
    | SNACK = 0x10uy
    /// Target Opcode : NOP-In
    | NOP_IN = 0x20uy
    /// Target Opcode : SCSI response
    | SCSI_RES = 0x21uy
    /// Target Opcode : SCSI task management response
    | SCSI_TASK_MGR_RES = 0x22uy
    /// Target Opcode : Login response
    | LOGIN_RES = 0x23uy
    /// Target Opcode : Text response
    | TEXT_RES = 0x24uy
    /// Target Opcode : SCSI Data-In
    | SCSI_DATA_IN = 0x25uy
    /// Target Opcode : Logout response
    | LOGOUT_RES = 0x26uy
    /// Target Opcode : Ready To Transfer(R2T)
    | R2T = 0x31uy
    /// Target Opcode : Asyncronous message
    | ASYNC = 0x32uy
    /// Target Opcode : Reject
    | REJECT = 0x3Fuy



//=============================================================================
// Task attribute values

type TaskATTRCd =
    /// attribute(ATTR) values : Tagless task
    | TAGLESS_TASK = 0x00uy
    /// attribute(ATTR) values : SIMPLE task
    | SIMPLE_TASK = 0x01uy
    /// attribute(ATTR) values : ORDERED task
    | ORDERED_TASK = 0x02uy
    /// attribute(ATTR) values : HEAD OF QUEUE task
    | HEAD_OF_QUEUE_TASK = 0x03uy
    /// attribute(ATTR) values : ACA task
    | ACA_TASK = 0x04uy

//=============================================================================
// AHSType values

type AHSTypeCd =
    /// Reserved
    | RESERVED = 0x00uy
    /// Extended CDB
    | EXTENDED_CDB = 0x01uy
    /// Expected Bidirectional Read Data Length
    | EXPECTED_LENGTH = 0x02uy


//=============================================================================
// iSCSI service response code

type iScsiSvcRespCd =
    /// iSCSI service response code : Command Complete at Target
    | COMMAND_COMPLETE = 0x00uy
    /// iSCSI service response code : Target Failure
    | TARGET_FAILURE = 0x01uy

//=============================================================================
// Task Management Function Request PDU Function values

type TaskMgrReqCd =
    /// Task Management Function Request PDU Function values : ABORT_TASK
    | ABORT_TASK = 0x01uy
    /// Task Management Function Request PDU Function values : ABORT_TASK_SET
    | ABORT_TASK_SET = 0x02uy
    /// Task Management Function Request PDU Function values : CLEAR ACA
    | CLEAR_ACA = 0x03uy
    /// Task Management Function Request PDU Function values : CLEAR TASK SET
    | CLEAR_TASK_SET = 0x04uy
    /// Task Management Function Request PDU Function values : LOGICAL UNIT RESET
    | LOGICAL_UNIT_RESET = 0x05uy
    /// Task Management Function Request PDU Function values : TARGET WARK RESET
    | TARGET_WARM_RESET = 0x06uy
    /// Task Management Function Request PDU Function values : TARGET COLD RESET
    | TARGET_COLD_RESET = 0x07uy
    /// Task Management Function Request PDU Function values : TASK REASSIGN
    | TASK_REASSIGN = 0x08uy


//=============================================================================
// Task Management Function Response PDU Response values

type TaskMgrResCd =
    /// Task Management Function Response PDU Response values : Function complete
    | FUCTION_COMPLETE = 0x00uy
    /// Task Management Function Response PDU Response values : Task does not exist
    | TASK_NOT_EXIST = 0x01uy
    /// Task Management Function Response PDU Response values : LUN does not exist
    | LUN_NOT_EXIST = 0x02uy
    /// Task Management Function Response PDU Response values : Task still allegiant
    | TASK_STILL_ALLEGIANT = 0x03uy
    /// Task Management Function Response PDU Response values : Task allegiance reassignment not supported
    | TASK_REASSIGN_NOT_SUPPORT = 0x04uy
    /// Task Management Function Response PDU Response values : Task management function not supported
    | TASK_MGR_NOT_SUPPORT = 0x05uy
    /// Task Management Function Response PDU Response values : Function authorization failed
    | AUTH_FAILED = 0x06uy
    /// Task Management Function Response PDU Response values : Function rejected
    | FUNCTION_REJECT = 0x07uy


//=============================================================================
// Asyncronous message PDU AsyncEvent values

type AsyncEventCd =
    /// Asyncronous message PDU AsyncEvent values : sence data
    | SENCE_DATA = 0x00uy
    /// Asyncronous message PDU AsyncEvent values : logout request
    | LOGOUT_REQ = 0x01uy
    /// Asyncronous message PDU AsyncEvent values : Connection close request
    | CONNECTION_CLOSE = 0x02uy
    /// Asyncronous message PDU AsyncEvent values : Session close
    | SESSION_CLOSE = 0x03uy
    /// Asyncronous message PDU AsyncEvent values : Parameter negotiation request
    | PARAM_NEGOTIATION_REQ = 0x04uy


//=============================================================================
// Login request PDU CSG, NSG values

type LoginReqStateCd =
    /// Login request PDU CSG, NSG values : Sequrity Negotiation
    | SEQURITY = 0uy
    /// Login request PDU CSG, NSG values : Login Operational Negotiation
    | OPERATIONAL = 1uy
    /// Login request PDU CSG, NSG values : Full Feature Phase
    | FULL = 3uy



//=============================================================================
// Login response PDU StatusClass, StatusDetail values

type LoginResStatCd =
    /// Login response PDU StatusClass values : Success
    | SUCCESS = 0x0000us
    /// Login response PDU StatusClass values : Redirect temporarily
    | REDIRECT_TMP = 0x0101us
    /// Login response PDU StatusClass values : Redirect permanently
    | REDIRECT_PERM = 0x0102us
    /// Login response PDU StatusClass values : Initiator error
    | INITIATOR_ERR = 0x0200us
    /// Login response PDU StatusClass values : Authentication failure
    | AUTH_FAILURE = 0x0201us
    /// Login response PDU StatusClass values : Authorization failure
    | NOT_ALLOWED = 0x0202us
    /// Login response PDU StatusClass values : Not found
    | NOT_FOUND = 0x0203us
    /// Login response PDU StatusClass values : Target removed
    | TARGET_REMOVED = 0x0204us
    /// Login response PDU StatusClass values : Unsupported version
    | UNSUPPORTED_VERSION = 0x0205us
    /// Login response PDU StatusClass values : Too many connections
    | TOO_MANY_CONS = 0x0206us
    /// Login response PDU StatusClass values : Missing parameter
    | MISSING_PARAMS = 0x0207us
    /// Login response PDU StatusClass values : Can't include in session
    | UNSUPPORT_MCS = 0x0208us
    /// Login response PDU StatusClass values : Session type not supported
    | UNSUPPORT_SESS_TYPE = 0x0209us
    /// Login response PDU StatusClass values : Session does not exist
    | SESS_NOT_EXIST = 0x020Aus
    /// Login response PDU StatusClass values : Invalid during login
    | INVALID_LOGIN = 0x020Bus
    /// Login response PDU StatusClass values : Target error
    | TARGET_ERROR = 0x0300us
    /// Login response PDU StatusClass values : Service unavailable 
    | SERVICE_UNAVAILABLE = 0x0301us
    /// Login response PDU StatusClass values : Out of resources
    | OUT_OF_RESOURCE = 0x0302us


//=============================================================================
// Logout request PDU Reason values

type LogoutReqReasonCd =
    /// Logout request PDU Reason values : close the session
    | CLOSE_SESS = 0x00uy
    /// Logout request PDU Reason values : close the connection
    | CLOSE_CONN = 0x01uy
    /// Logout request PDU Reason values : remove the connection for recovery
    | RECOVERY = 0x02uy


//=============================================================================
// Logout response PDU Response values

type LogoutResCd =
    /// Logout response PDU Response values : success
    | SUCCESS = 0x00uy
    /// Logout response PDU Response values : CID not found.
    | CID_NOT_FOUND = 0x01uy
    /// Logout response PDU Response values : connection recovery is not supported.
    | RECOVERY_NOT_SUPPORT = 0x02uy
    /// Logout response PDU Response values : cleanup failed
    | CLEANUP_FAILED = 0x03uy


//=============================================================================
// SNACK request PDU Type values

type SnackReqTypeCd =
    /// SNACK request PDU Type values : Data/R2T SNACK
    | DATA_R2T = 0x00uy
    /// SNACK request PDU Type values : Status SNACK
    | STATUS = 0x01uy
    /// SNACK request PDU Type values : Data ACK
    | DATA_ACK = 0x02uy
    /// SNACK request PDU Type values : R-Data SNACK
    | RDATA_SNACK = 0x03uy


//=============================================================================
// Reject PDU Reason values

type RejectResonCd =
    /// Reject PDU Reason values : Data (payload) Digest Error
    | DATA_DIGEST_ERR = 0x02uy
    /// Reject PDU Reason values : SNACK Reject
    | SNACK_REJECT = 0x03uy
    /// Reject PDU Reason values : Protocol error
    | PROTOCOL_ERR = 0x04uy
    /// Reject PDU Reason values : Command not supported
    | COM_NOT_SUPPORT = 0x05uy
    /// Reject PDU Reason values : Immediate Command Reject
    | IMMIDIATE_COM_REJECT = 0x06uy
    /// Reject PDU Reason values : Task in progress 
    | TASK_IN_PROGRESS = 0x07uy
    /// Reject PDU Reason values : Invalid Data ACK
    | INVALID_DATA_ACK = 0x08uy
    /// Reject PDU Reason values : Invalid PDU field 
    | INVALID_PDU_FIELD = 0x09uy
    /// Reject PDU Reason values : Long Operation Reject
    | LONG_OPE_REJECT = 0x0Auy
    /// Reject PDU Reason values : Negotiation Reset
    | NEGOTIATION_RESET = 0x0Buy
    /// Reject PDU Reason values : Waiting for Logout
    | WAIT_FOR_LOGOUT = 0x0Cuy


//=============================================================================
// SCSI Command status code

type ScsiCmdStatCd =
    /// SCSI Command status : GOOD
    | GOOD = 0x00uy
    /// SCSI Command status : CHECK CONDITION
    | CHECK_CONDITION = 0x02uy
    /// SCSI Command status : CONDITION MET
    | CONDITION_MET = 0x04uy
    /// SCSI Command status : BUSY
    | BUSY = 0x08uy
    /// SCSI Command status : INTERMEDIATE
    | INTERMEDIATE = 0x10uy
    /// SCSI Command status : INTERMEDIATE-CONDITION MET
    | INTERMEDIATE_CONDITION_MET = 0x14uy
    /// SCSI Command status : RESERVATION CONFLICT
    | RESERVATION_CONFLICT = 0x18uy
    /// SCSI Command status : TASK SET FULL
    | TASK_SET_FULL = 0x28uy
    /// SCSI Command status : ACA ACTIVE
    | ACA_ACTIVE = 0x30uy
    /// SCSI Command status : TASK ABORTED
    | TASK_ABORTED = 0x40uy


//=============================================================================
// SenseKey values.

type SenseKeyCd =
    /// SenseKey(NO SENSE)
    | NO_SENSE = 0x00uy
    /// SenseKey(RECOVERED ERROR)
    | RECOVERED_ERROR = 0x01uy
    /// SenseKey(NOT READY)
    | NOT_READY = 0x02uy
    /// SenseKey(MEDIUM ERROR)
    | MEDIUM_ERROR = 0x03uy
    /// SenseKey(HARDWARE ERROR)
    | HARDWARE_ERROR = 0x04uy
    /// SenseKey(ILLEGAL REQUEST)
    | ILLEGAL_REQUEST = 0x05uy
    /// SenseKey(UNIT ATTENTION)
    | UNIT_ATTENTION = 0x06uy
    /// SenseKey(DATA PROTECT)
    | DATA_PROTECT = 0x07uy
    /// SenseKey(BLANK CHECK)
    | BLANK_CHECK = 0x08uy
    /// SenseKey(VENDOR SPECIFIC)
    | VENDOR_SPECIFIC = 0x09uy
    /// SenseKey(COPY ABORTED)
    | COPY_ABORTED = 0x0Auy
    /// SenseKey(ABORTED COMMAND)
    | ABORTED_COMMAND = 0x0Buy
    /// SenseKey(VOLUME OVERFLOW)
    | VOLUME_OVERFLOW = 0x0Duy
    /// SenseKey(MISCOMPARE)
    | MISCOMPARE = 0x0Euy


//=============================================================================
// Additional sense code and additional sense code qualifier values

type ASCCd =
    /// Additional Sense Data(ACCESS DENIED - ACL LUN CONFLICT)
    | ACCESS_DENIED_ACL_LUN_CONFLICT = 0x200Bus
    /// Additional Sense Data(ACCESS DENIED - ENROLLMENT CONFLICT)
    | ACCESS_DENIED_ENROLLMENT_CONFLICT = 0x2008us
    /// Additional Sense Data(ACCESS DENIED - INITIATOR PENDING-ENROLLED)
    | ACCESS_DENIED_INITIATOR_PENDING_ENROLLED = 0x2001us
    /// Additional Sense Data(ACCESS DENIED - INVALID LU IDENTIFIER)
    | ACCESS_DENIED_INVALID_LU_IDENTIFIER = 0x2009us
    /// Additional Sense Data(ACCESS DENIED - INVALID MGMT ID KEY)
    | ACCESS_DENIED_INVALID_MGMT_ID_KEY = 0x2003us
    /// Additional Sense Data(ACCESS DENIED - INVALID PROXY TOKEN)
    | ACCESS_DENIED_INVALID_PROXY_TOKEN = 0x200Aus
    /// Additional Sense Data(ACCESS DENIED - NO ACCESS RIGHTS)
    | ACCESS_DENIED_NO_ACCESS_RIGHTS = 0x2002us
    /// Additional Sense Data(ACK/NAK TIMEOUT)
    | ACK_NAK_TIMEOUT = 0x4B03us
    /// Additional Sense Data(ADD LOGICAL UNIT FAILED)
    | ADD_LOGICAL_UNIT_FAILED = 0x6702us
    /// Additional Sense Data(ADDRESS MARK NOT FOUND FOR DATA FIELD)
    | ADDRESS_MARK_NOT_FOUND_FOR_DATA_FIELD = 0x1300us
    /// Additional Sense Data(ADDRESS MARK NOT FOUND FOR ID FIELD)
    | ADDRESS_MARK_NOT_FOUND_FOR_ID_FIELD = 0x1200us
    /// Additional Sense Data(ASSIGN FAILURE OCCURRED)
    | ASSIGN_FAILURE_OCCURRED = 0x6708us
    /// Additional Sense Data(ASSOCIATED WRITE PROTECT)
    | ASSOCIATED_WRITE_PROTECT = 0x2703us
    /// Additional Sense Data(ASYMMETRIC ACCESS STATE CHANGED)
    | ASYMMETRIC_ACCESS_STATE_CHANGED = 0x2A06us
    /// Additional Sense Data(ASYNCHRONOUS INFORMATION PROTECTION ERROR DETECTED)
    | ASYNCHRONOUS_INFORMATION_PROTECTION_ERROR_DETECTED = 0x4704us
    /// Additional Sense Data(ATTACHMENT OF LOGICAL UNIT FAILED)
    | ATTACHMENT_OF_LOGICAL_UNIT_FAILED = 0x6706us
    /// Additional Sense Data(AUDIO PLAY OPERATION IN PROGRESS)
    | AUDIO_PLAY_OPERATION_IN_PROGRESS = 0x0011us
    /// Additional Sense Data(AUDIO PLAY OPERATION PAUSED)
    | AUDIO_PLAY_OPERATION_PAUSED = 0x0012us
    /// Additional Sense Data(AUDIO PLAY OPERATION STOPPED DUE TO ERROR)
    | AUDIO_PLAY_OPERATION_STOPPED_DUE_TO_ERROR = 0x0014us
    /// Additional Sense Data(AUDIO PLAY OPERATION SUCCESSFULLY COMPLETED)
    | AUDIO_PLAY_OPERATION_SUCCESSFULLY_COMPLETED = 0x0013us
    /// Additional Sense Data(AUTOMATIC DOCUMENT FEEDER COVER UP)
    | AUTOMATIC_DOCUMENT_FEEDER_COVER_UP = 0x6600us
    /// Additional Sense Data(AUTOMATIC DOCUMENT FEEDER LIFT UP)
    | AUTOMATIC_DOCUMENT_FEEDER_LIFT_UP = 0x6601us
    /// Additional Sense Data(AUXILIARY MEMORY OUT OF SPACE)
    | AUXILIARY_MEMORY_OUT_OF_SPACE = 0x5506us
    /// Additional Sense Data(AUXILIARY MEMORY READ ERROR)
    | AUXILIARY_MEMORY_READ_ERROR = 0x1112us
    /// Additional Sense Data(AUXILIARY MEMORY WRITE ERROR)
    | AUXILIARY_MEMORY_WRITE_ERROR = 0x0C0Bus
    /// Additional Sense Data(BEGINNING-OF-PARTITION/MEDIUM DETECTED)
    | BEGINNING_OF_PARTITION_MEDIUM_DETECTED = 0x0004us
    /// Additional Sense Data(BLOCK NOT COMPRESSIBLE)
    | BLOCK_NOT_COMPRESSIBLE = 0x0C06us
    /// Additional Sense Data(BLOCK SEQUENCE ERROR)
    | BLOCK_SEQUENCE_ERROR = 0x1404us
    /// Additional Sense Data(BUS DEVICE RESET FUNCTION OCCURRED)
    | BUS_DEVICE_RESET_FUNCTION_OCCURRED = 0x2903us
    /// Additional Sense Data(CANNOT DECOMPRESS USING DECLARED ALGORITHM)
    | CANNOT_DECOMPRESS_USING_DECLARED_ALGORITHM = 0x110Eus
    /// Additional Sense Data(CANNOT FORMAT MEDIUM - INCOMPATIBLE MEDIUM)
    | CANNOT_FORMAT_MEDIUM_INCOMPATIBLE_MEDIUM = 0x3006us
    /// Additional Sense Data(CANNOT READ MEDIUM - INCOMPATIBLE FORMAT)
    | CANNOT_READ_MEDIUM_INCOMPATIBLE_FORMAT = 0x3002us
    /// Additional Sense Data(CANNOT READ MEDIUM - UNKNOWN FORMAT)
    | CANNOT_READ_MEDIUM_UNKNOWN_FORMAT = 0x3001us
    /// Additional Sense Data(CANNOT WRITE - APPLICATION CODE MISMATCH)
    | CANNOT_WRITE_APPLICATION_CODE_MISMATCH = 0x3008us
    /// Additional Sense Data(CANNOT WRITE MEDIUM - INCOMPATIBLE FORMAT)
    | CANNOT_WRITE_MEDIUM_INCOMPATIBLE_FORMAT = 0x3005us
    /// Additional Sense Data(CANNOT WRITE MEDIUM - UNKNOWN FORMAT)
    | CANNOT_WRITE_MEDIUM_UNKNOWN_FORMAT = 0x3004us
    /// Additional Sense Data(CAPACITY DATA HAS CHANGED)
    | CAPACITY_DATA_HAS_CHANGED = 0x2A09us
    /// Additional Sense Data(CARTRIDGE FAULT)
    | CARTRIDGE_FAULT = 0x5200us
    /// Additional Sense Data(CD CONTROL ERROR)
    | CD_CONTROL_ERROR = 0x7300us
    /// Additional Sense Data(CDB DECRYPTION ERROR)
    | CDB_DECRYPTION_ERROR = 0x2401us
    /// Additional Sense Data(CHANGED OPERATING DEFINITION)
    | CHANGED_OPERATING_DEFINITION = 0x3F02us
    /// Additional Sense Data(CIRC UNRECOVERED ERROR)
    | CIRC_UNRECOVERED_ERROR = 0x1106us
    /// Additional Sense Data(CLEANING CARTRIDGE INSTALLED)
    | CLEANING_CARTRIDGE_INSTALLED = 0x3003us
    /// Additional Sense Data(CLEANING FAILURE)
    | CLEANING_FAILURE = 0x3007us
    /// Additional Sense Data(CLEANING REQUEST REJECTED)
    | CLEANING_REQUEST_REJECTED = 0x300Aus
    /// Additional Sense Data(CLEANING REQUESTED)
    | CLEANING_REQUESTED = 0x0017us
    /// Additional Sense Data(COMMAND PHASE ERROR)
    | COMMAND_PHASE_ERROR = 0x4A00us
    /// Additional Sense Data(COMMAND SEQUENCE ERROR)
    | COMMAND_SEQUENCE_ERROR = 0x2C00us
    /// Additional Sense Data(COMMAND TO LOGICAL UNIT FAILED)
    | COMMAND_TO_LOGICAL_UNIT_FAILED = 0x6E00us
    /// Additional Sense Data(COMMANDS CLEARED BY ANOTHER INITIATOR)
    | COMMANDS_CLEARED_BY_ANOTHER_INITIATOR = 0x2F00us
    /// Additional Sense Data(COMPONENT DEVICE ATTACHED)
    | COMPONENT_DEVICE_ATTACHED = 0x3F04us
    /// Additional Sense Data(COMPRESSION CHECK MISCOMPARE ERROR)
    | COMPRESSION_CHECK_MISCOMPARE_ERROR = 0x0C04us
    /// Additional Sense Data(CONDITIONAL WRITE PROTECT)
    | CONDITIONAL_WRITE_PROTECT = 0x2706us
    /// Additional Sense Data(CONFIGURATION FAILURE)
    | CONFIGURATION_FAILURE = 0x6700us
    /// Additional Sense Data(CONFIGURATION OF INCAPABLE LOGICAL UNITS FAILED)
    | CONFIGURATION_OF_INCAPABLE_LOGICAL_UNITS_FAILED = 0x6701us
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE ACCESS TIMES TOO HIGH)
    | CONTROLLER_IMPENDING_FAILURE_ACCESS_TIMES_TOO_HIGH = 0x5D25us
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE CHANNEL PARAMETRICS)
    | CONTROLLER_IMPENDING_FAILURE_CHANNEL_PARAMETRICS = 0x5D27us
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE CONTROLLER DETECTED)
    | CONTROLLER_IMPENDING_FAILURE_CONTROLLER_DETECTED = 0x5D28us
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE DATA ERROR RATE TOO HIGH)
    | CONTROLLER_IMPENDING_FAILURE_DATA_ERROR_RATE_TOO_HIGH = 0x5D22us
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT)
    | CONTROLLER_IMPENDING_FAILURE_DRIVE_CALIBRATION_RETRY_COUNT = 0x5D2Cus
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH)
    | CONTROLLER_IMPENDING_FAILURE_DRIVE_ERROR_RATE_TOO_HIGH = 0x5D21us
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE GENERAL HARD DRIVE FAILURE)
    | CONTROLLER_IMPENDING_FAILURE_GENERAL_HARD_DRIVE_FAILURE = 0x5D20us
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE SEEK ERROR RATE TOO HIGH)
    | CONTROLLER_IMPENDING_FAILURE_SEEK_ERROR_RATE_TOO_HIGH = 0x5D23us
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE SEEK TIME PERFORMANCE)
    | CONTROLLER_IMPENDING_FAILURE_SEEK_TIME_PERFORMANCE = 0x5D2Aus
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE SPIN-UP RETRY COUNT)
    | CONTROLLER_IMPENDING_FAILURE_SPIN_UP_RETRY_COUNT = 0x5D2Bus
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE START UNIT TIMES TOO HIGH)
    | CONTROLLER_IMPENDING_FAILURE_START_UNIT_TIMES_TOO_HIGH = 0x5D26us
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE THROUGHPUT PERFORMANCE)
    | CONTROLLER_IMPENDING_FAILURE_THROUGHPUT_PERFORMANCE = 0x5D29us
    /// Additional Sense Data(CONTROLLER IMPENDING FAILURE TOO MANY BLOCK REASSIGNS)
    | CONTROLLER_IMPENDING_FAILURE_TOO_MANY_BLOCK_REASSIGNS = 0x5D24us
    /// Additional Sense Data(COPY CANNOT EXECUTE SINCE HOST CANNOT DISCONNECT)
    | COPY_CANNOT_EXECUTE_SINCE_HOST_CANNOT_DISCONNECT = 0x2B00us
    /// Additional Sense Data(COPY PROTECTION KEY EXCHANGE FAILURE - AUTHENTICATION FAILURE)
    | COPY_PROTECTION_KEY_EXCHANGE_FAILURE_AUTHENTICATION_FAILURE = 0x6F00us
    /// Additional Sense Data(COPY PROTECTION KEY EXCHANGE FAILURE - KEY NOT ESTABLISHED)
    | COPY_PROTECTION_KEY_EXCHANGE_FAILURE_KEY_NOT_ESTABLISHED = 0x6F02us
    /// Additional Sense Data(COPY PROTECTION KEY EXCHANGE FAILURE - KEY NOT PRESENT)
    | COPY_PROTECTION_KEY_EXCHANGE_FAILURE_KEY_NOT_PRESENT = 0x6F01us
    /// Additional Sense Data(COPY SEGMENT GRANULARITY VIOLATION)
    | COPY_SEGMENT_GRANULARITY_VIOLATION = 0x260Dus
    /// Additional Sense Data(COPY TARGET DEVICE DATA OVERRUN)
    | COPY_TARGET_DEVICE_DATA_OVERRUN = 0x0D05us
    /// Additional Sense Data(COPY TARGET DEVICE DATA UNDERRUN)
    | COPY_TARGET_DEVICE_DATA_UNDERRUN = 0x0D04us
    /// Additional Sense Data(COPY TARGET DEVICE NOT REACHABLE)
    | COPY_TARGET_DEVICE_NOT_REACHABLE = 0x0D02us
    /// Additional Sense Data(CREATION OF LOGICAL UNIT FAILED)
    | CREATION_OF_LOGICAL_UNIT_FAILED = 0x6707us
    /// Additional Sense Data(CURRENT PROGRAM AREA IS EMPTY)
    | CURRENT_PROGRAM_AREA_IS_EMPTY = 0x2C04us
    /// Additional Sense Data(CURRENT PROGRAM AREA IS NOT EMPTY)
    | CURRENT_PROGRAM_AREA_IS_NOT_EMPTY = 0x2C03us
    /// Additional Sense Data(CURRENT SESSION NOT FIXATED FOR APPEND)
    | CURRENT_SESSION_NOT_FIXATED_FOR_APPEND = 0x3009us
    /// Additional Sense Data(DATA BLOCK APPLICATION TAG CHECK FAILED)
    | DATA_BLOCK_APPLICATION_TAG_CHECK_FAILED = 0x1002us
    /// Additional Sense Data(DATA BLOCK GUARD CHECK FAILED)
    | DATA_BLOCK_GUARD_CHECK_FAILED = 0x1001us
    /// Additional Sense Data(DATA BLOCK REFERENCE TAG CHECK FAILED)
    | DATA_BLOCK_REFERENCE_TAG_CHECK_FAILED = 0x1003us
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE ACCESS TIMES TOO HIGH)
    | DATA_CHANNEL_IMPENDING_FAILURE_ACCESS_TIMES_TOO_HIGH = 0x5D35us
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE CHANNEL PARAMETRICS)
    | DATA_CHANNEL_IMPENDING_FAILURE_CHANNEL_PARAMETRICS = 0x5D37us
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE CONTROLLER DETECTED)
    | DATA_CHANNEL_IMPENDING_FAILURE_CONTROLLER_DETECTED = 0x5D38us
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE DATA ERROR RATE TOO HIGH)
    | DATA_CHANNEL_IMPENDING_FAILURE_DATA_ERROR_RATE_TOO_HIGH = 0x5D32us
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT)
    | DATA_CHANNEL_IMPENDING_FAILURE_DRIVE_CALIBRATION_RETRY_COUNT = 0x5D3Cus
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH)
    | DATA_CHANNEL_IMPENDING_FAILURE_DRIVE_ERROR_RATE_TOO_HIGH = 0x5D31us
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE GENERAL HARD DRIVE FAILURE)
    | DATA_CHANNEL_IMPENDING_FAILURE_GENERAL_HARD_DRIVE_FAILURE = 0x5D30us
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE SEEK ERROR RATE TOO HIGH)
    | DATA_CHANNEL_IMPENDING_FAILURE_SEEK_ERROR_RATE_TOO_HIGH = 0x5D33us
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE SEEK TIME PERFORMANCE)
    | DATA_CHANNEL_IMPENDING_FAILURE_SEEK_TIME_PERFORMANCE = 0x5D3Aus
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE SPIN-UP RETRY COUNT)
    | DATA_CHANNEL_IMPENDING_FAILURE_SPIN_UP_RETRY_COUNT = 0x5D3Bus
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE START UNIT TIMES TOO HIGH)
    | DATA_CHANNEL_IMPENDING_FAILURE_START_UNIT_TIMES_TOO_HIGH = 0x5D36us
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE THROUGHPUT PERFORMANCE)
    | DATA_CHANNEL_IMPENDING_FAILURE_THROUGHPUT_PERFORMANCE = 0x5D39us
    /// Additional Sense Data(DATA CHANNEL IMPENDING FAILURE TOO MANY BLOCK REASSIGNS)
    | DATA_CHANNEL_IMPENDING_FAILURE_TOO_MANY_BLOCK_REASSIGNS = 0x5D34us
    /// Additional Sense Data(DATA DECRYPTION ERROR)
    | DATA_DECRYPTION_ERROR = 0x2605us
    /// Additional Sense Data(DATA EXPANSION OCCURRED DURING COMPRESSION)
    | DATA_EXPANSION_OCCURRED_DURING_COMPRESSION = 0x0C05us
    /// Additional Sense Data(DATA LOSS ON LOGICAL UNIT)
    | DATA_LOSS_ON_LOGICAL_UNIT = 0x6900us
    /// Additional Sense Data(DATA OFFSET ERROR)
    | DATA_OFFSET_ERROR = 0x4B05us
    /// Additional Sense Data(DATA PATH FAILURE (SHOULD USE 40 NN))
    | DATA_PATH_FAILURE_NN = 0x4100us
    /// Additional Sense Data(DATA PHASE CRC ERROR DETECTED)
    | DATA_PHASE_CRC_ERROR_DETECTED = 0x4701us
    /// Additional Sense Data(DATA PHASE ERROR)
    | DATA_PHASE_ERROR = 0x4B00us
    /// Additional Sense Data(DATA RE-SYNCHRONIZATION ERROR)
    | DATA_RE_SYNCHRONIZATION_ERROR = 0x1107us
    /// Additional Sense Data(DATA SYNC ERROR - DATA AUTO-REALLOCATED)
    | DATA_SYNC_ERROR_DATA_AUTO_REALLOCATED = 0x1603us
    /// Additional Sense Data(DATA SYNC ERROR - DATA REWRITTEN)
    | DATA_SYNC_ERROR_DATA_REWRITTEN = 0x1601us
    /// Additional Sense Data(DATA SYNC ERROR - RECOMMEND REASSIGNMENT)
    | DATA_SYNC_ERROR_RECOMMEND_REASSIGNMENT = 0x1604us
    /// Additional Sense Data(DATA SYNC ERROR - RECOMMEND REWRITE)
    | DATA_SYNC_ERROR_RECOMMEND_REWRITE = 0x1602us
    /// Additional Sense Data(DATA SYNCHRONIZATION MARK ERROR)
    | DATA_SYNCHRONIZATION_MARK_ERROR = 0x1600us
    /// Additional Sense Data(DE-COMPRESSION CRC ERROR)
    | DE_COMPRESSION_CRC_ERROR = 0x110Dus
    /// Additional Sense Data(DECOMPRESSION EXCEPTION LONG ALGORITHM ID)
    | DECOMPRESSION_EXCEPTION_LONG_ALGORITHM_ID = 0x7100us
    /// Additional Sense Data(DECOMPRESSION EXCEPTION SHORT ALGORITHM ID OF NN)
    | DECOMPRESSION_EXCEPTION_SHORT_ALGORITHM_ID_OF_NN = 0x7000us
    /// Additional Sense Data(DEFECT LIST ERROR)
    | DEFECT_LIST_ERROR = 0x1900us
    /// Additional Sense Data(DEFECT LIST ERROR IN GROWN LIST)
    | DEFECT_LIST_ERROR_IN_GROWN_LIST = 0x1903us
    /// Additional Sense Data(DEFECT LIST ERROR IN PRIMARY LIST)
    | DEFECT_LIST_ERROR_IN_PRIMARY_LIST = 0x1902us
    /// Additional Sense Data(DEFECT LIST NOT AVAILABLE)
    | DEFECT_LIST_NOT_AVAILABLE = 0x1901us
    /// Additional Sense Data(DEFECT LIST NOT FOUND)
    | DEFECT_LIST_NOT_FOUND = 0x1C00us
    /// Additional Sense Data(DEFECT LIST UPDATE FAILURE)
    | DEFECT_LIST_UPDATE_FAILURE = 0x3201us
    /// Additional Sense Data(DEVICE IDENTIFIER CHANGED)
    | DEVICE_IDENTIFIER_CHANGED = 0x3F05us
    /// Additional Sense Data(DEVICE INTERNAL RESET)
    | DEVICE_INTERNAL_RESET = 0x2904us
    /// Additional Sense Data(DIAGNOSTIC FAILURE ON COMPONENT NN (80H-FFH))
    | DIAGNOSTIC_FAILURE_ON_COMPONENT_NN = 0x4000us
    /// Additional Sense Data(DOCUMENT JAM IN AUTOMATIC DOCUMENT FEEDER)
    | DOCUMENT_JAM_IN_AUTOMATIC_DOCUMENT_FEEDER = 0x6602us
    /// Additional Sense Data(DOCUMENT MISS FEED AUTOMATIC IN DOCUMENT FEEDER)
    | DOCUMENT_MISS_FEED_AUTOMATIC_IN_DOCUMENT_FEEDER = 0x6603us
    /// Additional Sense Data(DRIVE REGION MUST BE PERMANENT/REGION RESET COUNT ERROR)
    | DRIVE_REGION_MUST_BE_PERMANENT_REGION_RESET_COUNT_ERROR = 0x6F05us
    /// Additional Sense Data(ECHO BUFFER OVERWRITTEN)
    | ECHO_BUFFER_OVERWRITTEN = 0x3F0Fus
    /// Additional Sense Data(EMPTY OR PARTIALLY WRITTEN RESERVED TRACK)
    | EMPTY_OR_PARTIALLY_WRITTEN_RESERVED_TRACK = 0x7204us
    /// Additional Sense Data(ENCLOSURE FAILURE)
    | ENCLOSURE_FAILURE = 0x3400us
    /// Additional Sense Data(ENCLOSURE SERVICES CHECKSUM ERROR)
    | ENCLOSURE_SERVICES_CHECKSUM_ERROR = 0x3505us
    /// Additional Sense Data(ENCLOSURE SERVICES FAILURE)
    | ENCLOSURE_SERVICES_FAILURE = 0x3500us
    /// Additional Sense Data(ENCLOSURE SERVICES TRANSFER FAILURE)
    | ENCLOSURE_SERVICES_TRANSFER_FAILURE = 0x3503us
    /// Additional Sense Data(ENCLOSURE SERVICES TRANSFER REFUSED)
    | ENCLOSURE_SERVICES_TRANSFER_REFUSED = 0x3504us
    /// Additional Sense Data(ENCLOSURE SERVICES UNAVAILABLE)
    | ENCLOSURE_SERVICES_UNAVAILABLE = 0x3502us
    /// Additional Sense Data(END OF MEDIUM REACHED)
    | END_OF_MEDIUM_REACHED = 0x3B0Fus
    /// Additional Sense Data(END OF USER AREA ENCOUNTERED ON THIS TRACK)
    | END_OF_USER_AREA_ENCOUNTERED_ON_THIS_TRACK = 0x6300us
    /// Additional Sense Data(END-OF-DATA DETECTED)
    | END_OF_DATA_DETECTED = 0x0005us
    /// Additional Sense Data(END-OF-DATA NOT FOUND)
    | END_OF_DATA_NOT_FOUND = 0x1403us
    /// Additional Sense Data(END-OF-PARTITION/MEDIUM DETECTED)
    | END_OF_PARTITION_MEDIUM_DETECTED = 0x0002us
    /// Additional Sense Data(ERASE FAILURE)
    | ERASE_FAILURE = 0x5100us
    /// Additional Sense Data(ERASE FAILURE - INCOMPLETE ERASE OPERATION DETECTED)
    | ERASE_FAILURE_INCOMPLETE_ERASE_OPERATION_DETECTED = 0x5101us
    /// Additional Sense Data(ERASE OPERATION IN PROGRESS)
    | ERASE_OPERATION_IN_PROGRESS = 0x0018us
    /// Additional Sense Data(ERROR DETECTED BY THIRD PARTY TEMPORARY INITIATOR)
    | ERROR_DETECTED_BY_THIRD_PARTY_TEMPORARY_INITIATOR = 0x0D00us
    /// Additional Sense Data(ERROR LOG OVERFLOW)
    | ERROR_LOG_OVERFLOW = 0x0A00us
    /// Additional Sense Data(ERROR READING ISRC NUMBER)
    | ERROR_READING_ISRC_NUMBER = 0x1110us
    /// Additional Sense Data(ERROR READING UPC/EAN NUMBER)
    | ERROR_READING_UPC_EAN_NUMBER = 0x110Fus
    /// Additional Sense Data(ERROR TOO LONG TO CORRECT)
    | ERROR_TOO_LONG_TO_CORRECT = 0x1102us
    /// Additional Sense Data(ESN - DEVICE BUSY CLASS EVENT)
    | ESN_DEVICE_BUSY_CLASS_EVENT = 0x3806us
    /// Additional Sense Data(ESN - MEDIA CLASS EVENT)
    | ESN_MEDIA_CLASS_EVENT = 0x3804us
    /// Additional Sense Data(ESN - POWER MANAGEMENT CLASS EVENT)
    | ESN_POWER_MANAGEMENT_CLASS_EVENT = 0x3802us
    /// Additional Sense Data(EVENT STATUS NOTIFICATION)
    | EVENT_STATUS_NOTIFICATION = 0x3800us
    /// Additional Sense Data(EXCESSIVE WRITE ERRORS)
    | EXCESSIVE_WRITE_ERRORS = 0x0302us
    /// Additional Sense Data(EXCHANGE OF LOGICAL UNIT FAILD)
    | EXCHANGE_OF_LOGICAL_UNIT_FAILD = 0x6704us
    /// Additional Sense Data(FAILED TO SENSE BOTTOM-OF-FORM)
    | FAILED_TO_SENSE_BOTTOM_OF_FORM = 0x3B07us
    /// Additional Sense Data(FAILED TO SENSE TOP-OF-FORM)
    | FAILED_TO_SENSE_TOP_OF_FORM = 0x3B06us
    /// Additional Sense Data(FAILURE PREDICTION THRESHOLD EXCEEDED)
    | FAILURE_PREDICTION_THRESHOLD_EXCEEDED = 0x5D00us
    /// Additional Sense Data(FAILURE PREDICTION THRESHOLD EXCEEDED (FALSE))
    | FAILURE_PREDICTION_THRESHOLD_EXCEEDED_FALSE = 0x5DFFus
    /// Additional Sense Data(FILEMARK DETECTED)
    | FILEMARK_DETECTED = 0x0001us
    /// Additional Sense Data(FILEMARK OR SETMARK NOT FOUND)
    | FILEMARK_OR_SETMARK_NOT_FOUND = 0x1402us
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE ACCESS TIMES TOO HIGH)
    | FIRMWARE_IMPENDING_FAILURE_ACCESS_TIMES_TOO_HIGH = 0x5D65us
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE CHANNEL PARAMETRICS)
    | FIRMWARE_IMPENDING_FAILURE_CHANNEL_PARAMETRICS = 0x5D67us
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE CONTROLLER DETECTED)
    | FIRMWARE_IMPENDING_FAILURE_CONTROLLER_DETECTED = 0x5D68us
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE DATA ERROR RATE TOO HIGH)
    | FIRMWARE_IMPENDING_FAILURE_DATA_ERROR_RATE_TOO_HIGH = 0x5D62us
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT)
    | FIRMWARE_IMPENDING_FAILURE_DRIVE_CALIBRATION_RETRY_COUNT = 0x5D6Cus
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH)
    | FIRMWARE_IMPENDING_FAILURE_DRIVE_ERROR_RATE_TOO_HIGH = 0x5D61us
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE GENERAL HARD DRIVE FAILURE)
    | FIRMWARE_IMPENDING_FAILURE_GENERAL_HARD_DRIVE_FAILURE = 0x5D60us
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE SEEK ERROR RATE TOO HIGH)
    | FIRMWARE_IMPENDING_FAILURE_SEEK_ERROR_RATE_TOO_HIGH = 0x5D63us
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE SEEK TIME PERFORMANCE)
    | FIRMWARE_IMPENDING_FAILURE_SEEK_TIME_PERFORMANCE = 0x5D6Aus
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE SPIN-UP RETRY COUNT)
    | FIRMWARE_IMPENDING_FAILURE_SPIN_UP_RETRY_COUNT = 0x5D6Bus
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE START UNIT TIMES TOO HIGH)
    | FIRMWARE_IMPENDING_FAILURE_START_UNIT_TIMES_TOO_HIGH = 0x5D66us
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE THROUGHPUT PERFORMANCE)
    | FIRMWARE_IMPENDING_FAILURE_THROUGHPUT_PERFORMANCE = 0x5D69us
    /// Additional Sense Data(FIRMWARE IMPENDING FAILURE TOO MANY BLOCK REASSIGNS)
    | FIRMWARE_IMPENDING_FAILURE_TOO_MANY_BLOCK_REASSIGNS = 0x5D64us
    /// Additional Sense Data(FOCUS SERVO FAILURE)
    | FOCUS_SERVO_FAILURE = 0x0902us
    /// Additional Sense Data(FORMAT COMMAND FAILED)
    | FORMAT_COMMAND_FAILED = 0x3101us
    /// Additional Sense Data(GENERATION DOES NOT EXIST)
    | GENERATION_DOES_NOT_EXIST = 0x5800us
    /// Additional Sense Data(GROWN DEFECT LIST NOT FOUND)
    | GROWN_DEFECT_LIST_NOT_FOUND = 0x1C02us
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE ACCESS TIMES TOO HIGH)
    | HARDWARE_IMPENDING_FAILURE_ACCESS_TIMES_TOO_HIGH = 0x5D15us
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE CHANNEL PARAMETRICS)
    | HARDWARE_IMPENDING_FAILURE_CHANNEL_PARAMETRICS = 0x5D17us
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE CONTROLLER DETECTED)
    | HARDWARE_IMPENDING_FAILURE_CONTROLLER_DETECTED = 0x5D18us
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE DATA ERROR RATE TOO HIGH)
    | HARDWARE_IMPENDING_FAILURE_DATA_ERROR_RATE_TOO_HIGH = 0x5D12us
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT)
    | HARDWARE_IMPENDING_FAILURE_DRIVE_CALIBRATION_RETRY_COUNT = 0x5D1Cus
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH)
    | HARDWARE_IMPENDING_FAILURE_DRIVE_ERROR_RATE_TOO_HIGH = 0x5D11us
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE GENERAL HARD DRIVE FAILURE)
    | HARDWARE_IMPENDING_FAILURE_GENERAL_HARD_DRIVE_FAILURE = 0x5D10us
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE SEEK ERROR RATE TOO HIGH)
    | HARDWARE_IMPENDING_FAILURE_SEEK_ERROR_RATE_TOO_HIGH = 0x5D13us
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE SEEK TIME PERFORMANCE)
    | HARDWARE_IMPENDING_FAILURE_SEEK_TIME_PERFORMANCE = 0x5D1Aus
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE SPIN-UP RETRY COUNT)
    | HARDWARE_IMPENDING_FAILURE_SPIN_UP_RETRY_COUNT = 0x5D1Bus
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE START UNIT TIMES TOO HIGH)
    | HARDWARE_IMPENDING_FAILURE_START_UNIT_TIMES_TOO_HIGH = 0x5D16us
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE THROUGHPUT PERFORMANCE)
    | HARDWARE_IMPENDING_FAILURE_THROUGHPUT_PERFORMANCE = 0x5D19us
    /// Additional Sense Data(HARDWARE IMPENDING FAILURE TOO MANY BLOCK REASSIGNS)
    | HARDWARE_IMPENDING_FAILURE_TOO_MANY_BLOCK_REASSIGNS = 0x5D14us
    /// Additional Sense Data(HARDWARE WRITE PROTECTED)
    | HARDWARE_WRITE_PROTECTED = 0x2701us
    /// Additional Sense Data(HEAD SELECT FAULT)
    | HEAD_SELECT_FAULT = 0x0904us
    /// Additional Sense Data(I/O PROCESS TERMINATED)
    | I_O_PROCESS_TERMINATED = 0x0006us
    /// Additional Sense Data(ID CRC OR ECC ERROR)
    | ID_CRC_OR_ECC_ERROR = 0x1000us
    /// Additional Sense Data(IDLE CONDITION ACTIVATED BY COMMAND)
    | IDLE_CONDITION_ACTIVATED_BY_COMMAND = 0x5E03us
    /// Additional Sense Data(IDLE CONDITION ACTIVATED BY TIMER)
    | IDLE_CONDITION_ACTIVATED_BY_TIMER = 0x5E01us
    /// Additional Sense Data(ILLEGAL COMMAND WHILE IN EXPLICIT ADDRESS MODE)
    | ILLEGAL_COMMAND_WHILE_IN_EXPLICIT_ADDRESS_MODE = 0x2006us
    /// Additional Sense Data(ILLEGAL COMMAND WHILE IN IMPLICIT ADDRESS MODE)
    | ILLEGAL_COMMAND_WHILE_IN_IMPLICIT_ADDRESS_MODE = 0x2007us
    /// Additional Sense Data(ILLEGAL COMMAND WHILE IN WRITE CAPABLE STATE)
    | ILLEGAL_COMMAND_WHILE_IN_WRITE_CAPABLE_STATE = 0x2004us
    /// Additional Sense Data(ILLEGAL FUNCTION (USE 20 00, 24 00, OR 26 00))
    | ILLEGAL_FUNCTION = 0x2200us
    /// Additional Sense Data(ILLEGAL MODE FOR THIS TRACK)
    | ILLEGAL_MODE_FOR_THIS_TRACK = 0x6400us
    /// Additional Sense Data(ILLEGAL POWER CONDITION REQUEST)
    | ILLEGAL_POWER_CONDITION_REQUEST = 0x2C05us
    /// Additional Sense Data(IMPLICIT ASYMMETRIC ACCESS STATE TRANSITION FAILED)
    | IMPLICIT_ASYMMETRIC_ACCESS_STATE_TRANSITION_FAILED = 0x2A07us
    /// Additional Sense Data(IMPORT OR EXPORT ELEMENT ACCESSED)
    | IMPORT_OR_EXPORT_ELEMENT_ACCESSED = 0x2801us
    /// Additional Sense Data(INCOMPATIBLE MEDIUM INSTALLED)
    | INCOMPATIBLE_MEDIUM_INSTALLED = 0x3000us
    /// Additional Sense Data(INCOMPLETE BLOCK READ)
    | INCOMPLETE_BLOCK_READ = 0x1108us
    /// Additional Sense Data(INCORRECT COPY TARGET DEVICE TYPE)
    | INCORRECT_COPY_TARGET_DEVICE_TYPE = 0x0D03us
    /// Additional Sense Data(INFORMATION UNIT TOO LONG)
    | INFORMATION_UNIT_TOO_LONG = 0x0E02us
    /// Additional Sense Data(INFORMATION UNIT TOO SHORT)
    | INFORMATION_UNIT_TOO_SHORT = 0x0E01us
    /// Additional Sense Data(INFORMATION UNIT iuCRC ERROR DETECTED)
    | INFORMATION_UNIT_iuCRC_ERROR_DETECTED = 0x4703us
    /// Additional Sense Data(INFORMATIONAL, REFER TO LOG)
    | INFORMATIONAL_REFER_TO_LOG = 0x6A00us
    /// Additional Sense Data(INITIATOR DETECTED ERROR MESSAGE RECEIVED)
    | INITIATOR_DETECTED_ERROR_MESSAGE_RECEIVED = 0x4800us
    /// Additional Sense Data(INITIATOR RESPONSE TIMEOUT)
    | INITIATOR_RESPONSE_TIMEOUT = 0x4B06us
    /// Additional Sense Data(INLINE DATA LENGTH EXCEEDED)
    | INLINE_DATA_LENGTH_EXCEEDED = 0x260Bus
    /// Additional Sense Data(INQUIRY DATA HAS CHANGED)
    | INQUIRY_DATA_HAS_CHANGED = 0x3F03us
    /// Additional Sense Data(INSUFFICIENT ACCESS CONTROL RESOURCES)
    | INSUFFICIENT_ACCESS_CONTROL_RESOURCES = 0x5505us
    /// Additional Sense Data(INSUFFICIENT REGISTRATION RESOURCES)
    | INSUFFICIENT_REGISTRATION_RESOURCES = 0x5504us
    /// Additional Sense Data(INSUFFICIENT RESERVATION RESOURCES)
    | INSUFFICIENT_RESERVATION_RESOURCES = 0x5502us
    /// Additional Sense Data(INSUFFICIENT RESOURCES)
    | INSUFFICIENT_RESOURCES = 0x5503us
    /// Additional Sense Data(INSUFFICIENT TIME FOR OPERATION)
    | INSUFFICIENT_TIME_FOR_OPERATION = 0x2E00us
    /// Additional Sense Data(INTERNAL TARGET FAILURE)
    | INTERNAL_TARGET_FAILURE = 0x4400us
    /// Additional Sense Data(INVALID ADDRESS FOR WRITE)
    | INVALID_ADDRESS_FOR_WRITE = 0x2102us
    /// Additional Sense Data(INVALID BITS IN IDENTIFY MESSAGE)
    | INVALID_BITS_IN_IDENTIFY_MESSAGE = 0x3D00us
    /// Additional Sense Data(INVALID COMBINATION OF WINDOWS SPECIFIED)
    | INVALID_COMBINATION_OF_WINDOWS_SPECIFIED = 0x2C02us
    /// Additional Sense Data(INVALID COMMAND OPERATION CODE)
    | INVALID_COMMAND_OPERATION_CODE = 0x2000us
    /// Additional Sense Data(INVALID DATA-OUT BUFFER INTEGRITY CHECK VALUE)
    | INVALID_DATA_OUT_BUFFER_INTEGRITY_CHECK_VALUE = 0x260Fus
    /// Additional Sense Data(INVALID ELEMENT ADDRESS)
    | INVALID_ELEMENT_ADDRESS = 0x2101us
    /// Additional Sense Data(INVALID FIELD IN CDB)
    | INVALID_FIELD_IN_CDB = 0x2400us
    /// Additional Sense Data(INVALID FIELD IN COMMAND INFORMATION UNIT)
    | INVALID_FIELD_IN_COMMAND_INFORMATION_UNIT = 0x0E03us
    /// Additional Sense Data(INVALID FIELD IN PARAMETER LIST)
    | INVALID_FIELD_IN_PARAMETER_LIST = 0x2600us
    /// Additional Sense Data(INVALID INFORMATION UNIT)
    | INVALID_INFORMATION_UNIT = 0x0E00us
    /// Additional Sense Data(INVALID MESSAGE ERROR)
    | INVALID_MESSAGE_ERROR = 0x4900us
    /// Additional Sense Data(INVALID OPERATION FOR COPY SOURCE OR DESTINATION)
    | INVALID_OPERATION_FOR_COPY_SOURCE_OR_DESTINATION = 0x260Cus
    /// Additional Sense Data(INVALID PACKET SIZE)
    | INVALID_PACKET_SIZE = 0x6401us
    /// Additional Sense Data(INVALID PARAMETER WHILE PORT IS ENABLED)
    | INVALID_PARAMETER_WHILE_PORT_IS_ENABLED = 0x260Eus
    /// Additional Sense Data(INVALID RELEASE OF PERSISTENT RESERVATION)
    | INVALID_RELEASE_OF_PERSISTENT_RESERVATION = 0x2604us
    /// Additional Sense Data(INVALID TARGET PORT TRANSFER TAG RECEIVED)
    | INVALID_TARGET_PORT_TRANSFER_TAG_RECEIVED = 0x4B01us
    /// Additional Sense Data(I_T NEXUS LOSS OCCURRED)
    | I_T_NEXUS_LOSS_OCCURRED = 0x2907us
    /// Additional Sense Data(L-EC UNCORRECTABLE ERROR)
    | L_EC_UNCORRECTABLE_ERROR = 0x1105us
    /// Additional Sense Data(LAMP FAILURE)
    | LAMP_FAILURE = 0x6000us
    /// Additional Sense Data(LOCATE OPERATION FAILURE)
    | LOCATE_OPERATION_FAILURE = 0x1407us
    /// Additional Sense Data(LOCATE OPERATION IN PROGRESS)
    | LOCATE_OPERATION_IN_PROGRESS = 0x0019us
    /// Additional Sense Data(LOG COUNTER AT MAXIMUM)
    | LOG_COUNTER_AT_MAXIMUM = 0x5B02us
    /// Additional Sense Data(LOG EXCEPTION)
    | LOG_EXCEPTION = 0x5B00us
    /// Additional Sense Data(LOG LIST CODES EXHAUSTED)
    | LOG_LIST_CODES_EXHAUSTED = 0x5B03us
    /// Additional Sense Data(LOG PARAMETERS CHANGED)
    | LOG_PARAMETERS_CHANGED = 0x2A02us
    /// Additional Sense Data(LOGICAL BLOCK ADDRESS OUT OF RANGE)
    | LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE = 0x2100us
    /// Additional Sense Data(LOGICAL UNIT COMMUNICATION CRC ERROR (ULTRA-DMA/32))
    | LOGICAL_UNIT_COMMUNICATION_CRC_ERROR_ULTRA_DMA_32 = 0x0803us
    /// Additional Sense Data(LOGICAL UNIT COMMUNICATION FAILURE)
    | LOGICAL_UNIT_COMMUNICATION_FAILURE = 0x0800us
    /// Additional Sense Data(LOGICAL UNIT COMMUNICATION PARITY ERROR)
    | LOGICAL_UNIT_COMMUNICATION_PARITY_ERROR = 0x0802us
    /// Additional Sense Data(LOGICAL UNIT COMMUNICATION TIME-OUT)
    | LOGICAL_UNIT_COMMUNICATION_TIME_OUT = 0x0801us
    /// Additional Sense Data(LOGICAL UNIT DOES NOT RESPOND TO SELECTION)
    | LOGICAL_UNIT_DOES_NOT_RESPOND_TO_SELECTION = 0x0500us
    /// Additional Sense Data(LOGICAL UNIT FAILED SELF-CONFIGURATION)
    | LOGICAL_UNIT_FAILED_SELF_CONFIGURATION = 0x4C00us
    /// Additional Sense Data(LOGICAL UNIT FAILED SELF-TEST)
    | LOGICAL_UNIT_FAILED_SELF_TEST = 0x3E03us
    /// Additional Sense Data(LOGICAL UNIT FAILURE)
    | LOGICAL_UNIT_FAILURE = 0x3E01us
    /// Additional Sense Data(LOGICAL UNIT FAILURE PREDICTION THRESHOLD EXCEEDED)
    | LOGICAL_UNIT_FAILURE_PREDICTION_THRESHOLD_EXCEEDED = 0x5D02us
    /// Additional Sense Data(LOGICAL UNIT HAS NOT SELF-CONFIGURED YET)
    | LOGICAL_UNIT_HAS_NOT_SELF_CONFIGURED_YET = 0x3E00us
    /// Additional Sense Data(LOGICAL UNIT IS IN PROCESS OF BECOMING READY)
    | LOGICAL_UNIT_IS_IN_PROCESS_OF_BECOMING_READY = 0x0401us
    /// Additional Sense Data(LOGICAL UNIT NOT ACCESSIBLE, ASYMMETRIC ACCESS STATE TRANSITION)
    | LOGICAL_UNIT_NOT_ACCESSIBLE_ASYMMETRIC_ACCESS_STATE_TRANSITION = 0x040Aus
    /// Additional Sense Data(LOGICAL UNIT NOT ACCESSIBLE, TARGET PORT IN STANDBY STATE)
    | LOGICAL_UNIT_NOT_ACCESSIBLE_TARGET_PORT_IN_STANDBY_STATE = 0x040Bus
    /// Additional Sense Data(LOGICAL UNIT NOT ACCESSIBLE, TARGET PORT IN UNAVAILABLE STATE)
    | LOGICAL_UNIT_NOT_ACCESSIBLE_TARGET_PORT_IN_UNAVAILABLE_STATE = 0x040Cus
    /// Additional Sense Data(LOGICAL UNIT NOT CONFIGURED)
    | LOGICAL_UNIT_NOT_CONFIGURED = 0x6800us
    /// Additional Sense Data(LOGICAL UNIT NOT READY, AUXILIARY MEMORY NOT ACCESSIBLE)
    | LOGICAL_UNIT_NOT_READY_AUXILIARY_MEMORY_NOT_ACCESSIBLE = 0x0410us
    /// Additional Sense Data(LOGICAL UNIT NOT READY, CAUSE NOT REPORTABLE)
    | LOGICAL_UNIT_NOT_READY_CAUSE_NOT_REPORTABLE = 0x0400us
    /// Additional Sense Data(LOGICAL UNIT NOT READY, FORMAT IN PROGRESS)
    | LOGICAL_UNIT_NOT_READY_FORMAT_IN_PROGRESS = 0x0404us
    /// Additional Sense Data(LOGICAL UNIT NOT READY, INITIALIZING COMMAND REQUIRED)
    | LOGICAL_UNIT_NOT_READY_INITIALIZING_COMMAND_REQUIRED = 0x0402us
    /// Additional Sense Data(LOGICAL UNIT NOT READY, LONG WRITE IN PROGRESS)
    | LOGICAL_UNIT_NOT_READY_LONG_WRITE_IN_PROGRESS = 0x0408us
    /// Additional Sense Data(LOGICAL UNIT NOT READY, MANUAL INTERVENTION REQUIRED)
    | LOGICAL_UNIT_NOT_READY_MANUAL_INTERVENTION_REQUIRED = 0x0403us
    /// Additional Sense Data(LOGICAL UNIT NOT READY, NOTIFY (ENABLE SPINUP) REQUIRED)
    | LOGICAL_UNIT_NOT_READY_NOTIFY_ENABLE_SPINUP_REQUIRED = 0x0411us
    /// Additional Sense Data(LOGICAL UNIT NOT READY, OFFLINE)
    | LOGICAL_UNIT_NOT_READY_OFFLINE = 0x0412us
    /// Additional Sense Data(LOGICAL UNIT NOT READY, OPERATION IN PROGRESS)
    | LOGICAL_UNIT_NOT_READY_OPERATION_IN_PROGRESS = 0x0407us
    /// Additional Sense Data(LOGICAL UNIT NOT READY, REBUILD IN PROGRESS)
    | LOGICAL_UNIT_NOT_READY_REBUILD_IN_PROGRESS = 0x0405us
    /// Additional Sense Data(LOGICAL UNIT NOT READY, RECALCULATION IN PROGRESS)
    | LOGICAL_UNIT_NOT_READY_RECALCULATION_IN_PROGRESS = 0x0406us
    /// Additional Sense Data(LOGICAL UNIT NOT READY, SELF-TEST IN PROGRESS)
    | LOGICAL_UNIT_NOT_READY_SELF_TEST_IN_PROGRESS = 0x0409us
    /// Additional Sense Data(LOGICAL UNIT NOT SUPPORTED)
    | LOGICAL_UNIT_NOT_SUPPORTED = 0x2500us
    /// Additional Sense Data(LOGICAL UNIT SOFTWARE WRITE PROTECTED)
    | LOGICAL_UNIT_SOFTWARE_WRITE_PROTECTED = 0x2702us
    /// Additional Sense Data(LOGICAL UNIT UNABLE TO UPDATE SELF-TEST LOG)
    | LOGICAL_UNIT_UNABLE_TO_UPDATE_SELF_TEST_LOG = 0x3E04us
    /// Additional Sense Data(LOW POWER CONDITION ON)
    | LOW_POWER_CONDITION_ON = 0x5E00us
    /// Additional Sense Data(MECHANICAL POSITIONING ERROR)
    | MECHANICAL_POSITIONING_ERROR = 0x1501us
    /// Additional Sense Data(MECHANICAL POSITIONING OR CHANGER ERROR)
    | MECHANICAL_POSITIONING_OR_CHANGER_ERROR = 0x3B16us
    /// Additional Sense Data(MEDIA FAILURE PREDICTION THRESHOLD EXCEEDED)
    | MEDIA_FAILURE_PREDICTION_THRESHOLD_EXCEEDED = 0x5D01us
    /// Additional Sense Data(MEDIA LOAD OR EJECT FAILED)
    | MEDIA_LOAD_OR_EJECT_FAILED = 0x5300us
    /// Additional Sense Data(MEDIA REGION CODE IS MISMATCHED TO LOGICAL UNIT REGION)
    | MEDIA_REGION_CODE_IS_MISMATCHED_TO_LOGICAL_UNIT_REGION = 0x6F04us
    /// Additional Sense Data(MEDIUM AUXILIARY MEMORY ACCESSIBLE)
    | MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE = 0x3F11us
    /// Additional Sense Data(MEDIUM DESTINATION ELEMENT FULL)
    | MEDIUM_DESTINATION_ELEMENT_FULL = 0x3B0Dus
    /// Additional Sense Data(MEDIUM FORMAT CORRUPTED)
    | MEDIUM_FORMAT_CORRUPTED = 0x3100us
    /// Additional Sense Data(MEDIUM LOADABLE)
    | MEDIUM_LOADABLE = 0x3F10us
    /// Additional Sense Data(MEDIUM MAGAZINE INSERTED)
    | MEDIUM_MAGAZINE_INSERTED = 0x3B13us
    /// Additional Sense Data(MEDIUM MAGAZINE LOCKED)
    | MEDIUM_MAGAZINE_LOCKED = 0x3B14us
    /// Additional Sense Data(MEDIUM MAGAZINE NOT ACCESSIBLE)
    | MEDIUM_MAGAZINE_NOT_ACCESSIBLE = 0x3B11us
    /// Additional Sense Data(MEDIUM MAGAZINE REMOVED)
    | MEDIUM_MAGAZINE_REMOVED = 0x3B12us
    /// Additional Sense Data(MEDIUM MAGAZINE UNLOCKED)
    | MEDIUM_MAGAZINE_UNLOCKED = 0x3B15us
    /// Additional Sense Data(MEDIUM NOT FORMATTED)
    | MEDIUM_NOT_FORMATTED = 0x3010us
    /// Additional Sense Data(MEDIUM NOT PRESENT)
    | MEDIUM_NOT_PRESENT = 0x3A00us
    /// Additional Sense Data(MEDIUM NOT PRESENT - LOADABLE)
    | MEDIUM_NOT_PRESENT_LOADABLE = 0x3A03us
    /// Additional Sense Data(MEDIUM NOT PRESENT - MEDIUM AUXILIARY MEMORY ACCESSIBLE)
    | MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE = 0x3A04us
    /// Additional Sense Data(MEDIUM NOT PRESENT - TRAY CLOSED)
    | MEDIUM_NOT_PRESENT_TRAY_CLOSED = 0x3A01us
    /// Additional Sense Data(MEDIUM NOT PRESENT - TRAY OPEN)
    | MEDIUM_NOT_PRESENT_TRAY_OPEN = 0x3A02us
    /// Additional Sense Data(MEDIUM REMOVAL PREVENTED)
    | MEDIUM_REMOVAL_PREVENTED = 0x5302us
    /// Additional Sense Data(MEDIUM SOURCE ELEMENT EMPTY)
    | MEDIUM_SOURCE_ELEMENT_EMPTY = 0x3B0Eus
    /// Additional Sense Data(MESSAGE ERROR)
    | MESSAGE_ERROR = 0x4300us
    /// Additional Sense Data(MICROCODE HAS BEEN CHANGED)
    | MICROCODE_HAS_BEEN_CHANGED = 0x3F01us
    /// Additional Sense Data(MISCOMPARE DURING VERIFY OPERATION)
    | MISCOMPARE_DURING_VERIFY_OPERATION = 0x1D00us
    /// Additional Sense Data(MISCORRECTED ERROR)
    | MISCORRECTED_ERROR = 0x110Aus
    /// Additional Sense Data(MODE PARAMETERS CHANGED)
    | MODE_PARAMETERS_CHANGED = 0x2A01us
    /// Additional Sense Data(MODIFICATION OF LOGICAL UNIT FAILED)
    | MODIFICATION_OF_LOGICAL_UNIT_FAILED = 0x6703us
    /// Additional Sense Data(MULTIPLE LOGICAL UNIT FAILURES)
    | MULTIPLE_LOGICAL_UNIT_FAILURES = 0x6901us
    /// Additional Sense Data(MULTIPLE PERIPHERAL DEVICES SELECTED)
    | MULTIPLE_PERIPHERAL_DEVICES_SELECTED = 0x0700us
    /// Additional Sense Data(MULTIPLE READ ERRORS)
    | MULTIPLE_READ_ERRORS = 0x1103us
    /// Additional Sense Data(MULTIPLY ASSIGNED LOGICAL UNIT)
    | MULTIPLY_ASSIGNED_LOGICAL_UNIT = 0x6709us
    /// Additional Sense Data(NAK RECEIVED)
    | NAK_RECEIVED = 0x4B04us
    /// Additional Sense Data(NO ADDITIONAL SENSE INFORMATION)
    | NO_ADDITIONAL_SENSE_INFORMATION = 0x0000us
    /// Additional Sense Data(NO CURRENT AUDIO STATUS TO RETURN)
    | NO_CURRENT_AUDIO_STATUS_TO_RETURN = 0x0015us
    /// Additional Sense Data(NO DEFECT SPARE LOCATION AVAILABLE)
    | NO_DEFECT_SPARE_LOCATION_AVAILABLE = 0x3200us
    /// Additional Sense Data(NO GAP FOUND)
    | NO_GAP_FOUND = 0x1109us
    /// Additional Sense Data(NO INDEX/SECTOR SIGNAL)
    | NO_INDEX_SECTOR_SIGNAL = 0x0100us
    /// Additional Sense Data(NO MORE TRACK RESERVATIONS ALLOWED)
    | NO_MORE_TRACK_RESERVATIONS_ALLOWED = 0x7205us
    /// Additional Sense Data(NO REFERENCE POSITION FOUND)
    | NO_REFERENCE_POSITION_FOUND = 0x0600us
    /// Additional Sense Data(NO SEEK COMPLETE)
    | NO_SEEK_COMPLETE = 0x0200us
    /// Additional Sense Data(NO WRITE CURRENT)
    | NO_WRITE_CURRENT = 0x0301us
    /// Additional Sense Data(NONCE NOT UNIQUE)
    | NONCE_NOT_UNIQUE = 0x2406us
    /// Additional Sense Data(NONCE TIMESTAMP OUT OF RANGE)
    | NONCE_TIMESTAMP_OUT_OF_RANGE = 0x2407us
    /// Additional Sense Data(NOT READY TO READY CHANGE, MEDIUM MAY HAVE CHANGED)
    | NOT_READY_TO_READY_CHANGE_MEDIUM_MAY_HAVE_CHANGED = 0x2800us
    /// Additional Sense Data(NOT RESERVED)
    | NOT_RESERVED = 0x2C0Bus
    /// Additional Sense Data(OPERATION IN PROGRESS)
    | OPERATION_IN_PROGRESS = 0x0016us
    /// Additional Sense Data(OPERATOR MEDIUM REMOVAL REQUEST)
    | OPERATOR_MEDIUM_REMOVAL_REQUEST = 0x5A01us
    /// Additional Sense Data(OPERATOR REQUEST OR STATE CHANGE INPUT)
    | OPERATOR_REQUEST_OR_STATE_CHANGE_INPUT = 0x5A00us
    /// Additional Sense Data(OPERATOR SELECTED WRITE PERMIT)
    | OPERATOR_SELECTED_WRITE_PERMIT = 0x5A03us
    /// Additional Sense Data(OPERATOR SELECTED WRITE PROTECT)
    | OPERATOR_SELECTED_WRITE_PROTECT = 0x5A02us
    /// Additional Sense Data(OUT OF FOCUS)
    | OUT_OF_FOCUS = 0x6102us
    /// Additional Sense Data(OVERLAPPED COMMANDS ATTEMPTED)
    | OVERLAPPED_COMMANDS_ATTEMPTED = 0x4E00us
    /// Additional Sense Data(OVERWRITE ERROR ON UPDATE IN PLACE)
    | OVERWRITE_ERROR_ON_UPDATE_IN_PLACE = 0x2D00us
    /// Additional Sense Data(PACKET DOES NOT FIT IN AVAILABLE SPACE)
    | PACKET_DOES_NOT_FIT_IN_AVAILABLE_SPACE = 0x6301us
    /// Additional Sense Data(PAPER JAM)
    | PAPER_JAM = 0x3B05us
    /// Additional Sense Data(PARAMETER LIST LENGTH ERROR)
    | PARAMETER_LIST_LENGTH_ERROR = 0x1A00us
    /// Additional Sense Data(PARAMETER NOT SUPPORTED)
    | PARAMETER_NOT_SUPPORTED = 0x2601us
    /// Additional Sense Data(PARAMETER VALUE INVALID)
    | PARAMETER_VALUE_INVALID = 0x2602us
    /// Additional Sense Data(PARAMETERS CHANGED)
    | PARAMETERS_CHANGED = 0x2A00us
    /// Additional Sense Data(PARITY/DATA MISMATCH)
    | PARITY_DATA_MISMATCH = 0x6902us
    /// Additional Sense Data(PARTIAL DEFECT LIST TRANSFER)
    | PARTIAL_DEFECT_LIST_TRANSFER = 0x1F00us
    /// Additional Sense Data(PARTITION OR COLLECTION CONTAINS USER OBJECTS)
    | PARTITION_OR_COLLECTION_CONTAINS_USER_OBJECTS = 0x2C0Aus
    /// Additional Sense Data(PERIPHERAL DEVICE WRITE FAULT)
    | PERIPHERAL_DEVICE_WRITE_FAULT = 0x0300us
    /// Additional Sense Data(PERMANENT WRITE PROTECT)
    | PERMANENT_WRITE_PROTECT = 0x2705us
    /// Additional Sense Data(PERSISTENT PREVENT CONFLICT)
    | PERSISTENT_PREVENT_CONFLICT = 0x2C06us
    /// Additional Sense Data(PERSISTENT WRITE PROTECT)
    | PERSISTENT_WRITE_PROTECT = 0x2704us
    /// Additional Sense Data(PHY TEST FUNCTION IN PROGRESS)
    | PHY_TEST_FUNCTION_IN_PROGRESS = 0x4706us
    /// Additional Sense Data(POSITION ERROR RELATED TO TIMING)
    | POSITION_ERROR_RELATED_TO_TIMING = 0x5002us
    /// Additional Sense Data(POSITION PAST BEGINNING OF MEDIUM)
    | POSITION_PAST_BEGINNING_OF_MEDIUM = 0x3B0Cus
    /// Additional Sense Data(POSITION PAST END OF MEDIUM)
    | POSITION_PAST_END_OF_MEDIUM = 0x3B0Bus
    /// Additional Sense Data(POSITIONING ERROR DETECTED BY READ OF MEDIUM)
    | POSITIONING_ERROR_DETECTED_BY_READ_OF_MEDIUM = 0x1502us
    /// Additional Sense Data(POWER CALIBRATION AREA ALMOST FULL)
    | POWER_CALIBRATION_AREA_ALMOST_FULL = 0x7301us
    /// Additional Sense Data(POWER CALIBRATION AREA ERROR)
    | POWER_CALIBRATION_AREA_ERROR = 0x7303us
    /// Additional Sense Data(POWER CALIBRATION AREA IS FULL)
    | POWER_CALIBRATION_AREA_IS_FULL = 0x7302us
    /// Additional Sense Data(POWER ON OCCURRED)
    | POWER_ON_OCCURRED = 0x2901us
    /// Additional Sense Data(POWER ON, RESET, OR BUS DEVICE RESET OCCURRED)
    | POWER_ON_RESET_OR_BUS_DEVICE_RESET_OCCURRED = 0x2900us
    /// Additional Sense Data(POWER STATE CHANGE TO ACTIVE)
    | POWER_STATE_CHANGE_TO_ACTIVE = 0x5E41us
    /// Additional Sense Data(POWER STATE CHANGE TO DEVICE CONTROL)
    | POWER_STATE_CHANGE_TO_DEVICE_CONTROL = 0x5E47us
    /// Additional Sense Data(POWER STATE CHANGE TO IDLE)
    | POWER_STATE_CHANGE_TO_IDLE = 0x5E42us
    /// Additional Sense Data(POWER STATE CHANGE TO SLEEP)
    | POWER_STATE_CHANGE_TO_SLEEP = 0x5E45us
    /// Additional Sense Data(POWER STATE CHANGE TO STANDBY)
    | POWER_STATE_CHANGE_TO_STANDBY = 0x5E43us
    /// Additional Sense Data(POWER-ON OR SELF-TEST FAILURE (SHOULD USE 40 NN))
    | POWER_ON_OR_SELF_TEST_FAILURE_NN = 0x4200us
    /// Additional Sense Data(PREVIOUS BUSY STATUS)
    | PREVIOUS_BUSY_STATUS = 0x2C07us
    /// Additional Sense Data(PREVIOUS RESERVATION CONFLICT STATUS)
    | PREVIOUS_RESERVATION_CONFLICT_STATUS = 0x2C09us
    /// Additional Sense Data(PREVIOUS TASK SET FULL STATUS)
    | PREVIOUS_TASK_SET_FULL_STATUS = 0x2C08us
    /// Additional Sense Data(PRIMARY DEFECT LIST NOT FOUND)
    | PRIMARY_DEFECT_LIST_NOT_FOUND = 0x1C01us
    /// Additional Sense Data(PRIORITY CHANGED)
    | PRIORITY_CHANGED = 0x2A08us
    /// Additional Sense Data(PROGRAM MEMORY AREA IS FULL)
    | PROGRAM_MEMORY_AREA_IS_FULL = 0x7305us
    /// Additional Sense Data(PROGRAM MEMORY AREA UPDATE FAILURE)
    | PROGRAM_MEMORY_AREA_UPDATE_FAILURE = 0x7304us
    /// Additional Sense Data(PROTOCOL SERVICE CRC ERROR)
    | PROTOCOL_SERVICE_CRC_ERROR = 0x4705us
    /// Additional Sense Data(QUOTA ERROR)
    | QUOTA_ERROR = 0x5507us
    /// Additional Sense Data(RANDOM POSITIONING ERROR)
    | RANDOM_POSITIONING_ERROR = 0x1500us
    /// Additional Sense Data(READ ERROR - FAILED RETRANSMISSION REQUEST)
    | READ_ERROR_FAILED_RETRANSMISSION_REQUEST = 0x1113us
    /// Additional Sense Data(READ ERROR - LOSS OF STREAMING)
    | READ_ERROR_LOSS_OF_STREAMING = 0x1111us
    /// Additional Sense Data(READ OF SCRAMBLED SECTOR WITHOUT AUTHENTICATION)
    | READ_OF_SCRAMBLED_SECTOR_WITHOUT_AUTHENTICATION = 0x6F03us
    /// Additional Sense Data(READ PAST BEGINNING OF MEDIUM)
    | READ_PAST_BEGINNING_OF_MEDIUM = 0x3B0Aus
    /// Additional Sense Data(READ PAST END OF MEDIUM)
    | READ_PAST_END_OF_MEDIUM = 0x3B09us
    /// Additional Sense Data(READ PAST END OF USER OBJECT)
    | READ_PAST_END_OF_USER_OBJECT = 0x3B17us
    /// Additional Sense Data(READ RETRIES EXHAUSTED)
    | READ_RETRIES_EXHAUSTED = 0x1101us
    /// Additional Sense Data(REBUILD FAILURE OCCURRED)
    | REBUILD_FAILURE_OCCURRED = 0x6C00us
    /// Additional Sense Data(RECALCULATE FAILURE OCCURRED)
    | RECALCULATE_FAILURE_OCCURRED = 0x6D00us
    /// Additional Sense Data(RECORD NOT FOUND)
    | RECORD_NOT_FOUND = 0x1401us
    /// Additional Sense Data(RECORD NOT FOUND - DATA AUTO-REALLOCATED)
    | RECORD_NOT_FOUND_DATA_AUTO_REALLOCATED = 0x1406us
    /// Additional Sense Data(RECORD NOT FOUND - RECOMMEND REASSIGNMENT)
    | RECORD_NOT_FOUND_RECOMMEND_REASSIGNMENT = 0x1405us
    /// Additional Sense Data(RECORDED ENTITY NOT FOUND)
    | RECORDED_ENTITY_NOT_FOUND = 0x1400us
    /// Additional Sense Data(RECOVERED DATA - DATA AUTO-REALLOCATED)
    | RECOVERED_DATA_DATA_AUTO_REALLOCATED = 0x1802us
    /// Additional Sense Data(RECOVERED DATA - RECOMMEND REASSIGNMENT)
    | RECOVERED_DATA_RECOMMEND_REASSIGNMENT = 0x1805us
    /// Additional Sense Data(RECOVERED DATA - RECOMMEND REWRITE)
    | RECOVERED_DATA_RECOMMEND_REWRITE = 0x1806us
    /// Additional Sense Data(RECOVERED DATA USING PREVIOUS SECTOR ID)
    | RECOVERED_DATA_USING_PREVIOUS_SECTOR_ID = 0x1705us
    /// Additional Sense Data(RECOVERED DATA WITH CIRC)
    | RECOVERED_DATA_WITH_CIRC = 0x1803us
    /// Additional Sense Data(RECOVERED DATA WITH ECC - DATA REWRITTEN)
    | RECOVERED_DATA_WITH_ECC_DATA_REWRITTEN = 0x1807us
    /// Additional Sense Data(RECOVERED DATA WITH ERROR CORR. & RETRIES APPLIED)
    | RECOVERED_DATA_WITH_ERROR_CORR_AND_RETRIES_APPLIED = 0x1801us
    /// Additional Sense Data(RECOVERED DATA WITH ERROR CORRECTION APPLIED)
    | RECOVERED_DATA_WITH_ERROR_CORRECTION_APPLIED = 0x1800us
    /// Additional Sense Data(RECOVERED DATA WITH L-EC)
    | RECOVERED_DATA_WITH_L_EC = 0x1804us
    /// Additional Sense Data(RECOVERED DATA WITH LINKING)
    | RECOVERED_DATA_WITH_LINKING = 0x1808us
    /// Additional Sense Data(RECOVERED DATA WITH NEGATIVE HEAD OFFSET)
    | RECOVERED_DATA_WITH_NEGATIVE_HEAD_OFFSET = 0x1703us
    /// Additional Sense Data(RECOVERED DATA WITH NO ERROR CORRECTION APPLIED)
    | RECOVERED_DATA_WITH_NO_ERROR_CORRECTION_APPLIED = 0x1700us
    /// Additional Sense Data(RECOVERED DATA WITH POSITIVE HEAD OFFSET)
    | RECOVERED_DATA_WITH_POSITIVE_HEAD_OFFSET = 0x1702us
    /// Additional Sense Data(RECOVERED DATA WITH RETRIES)
    | RECOVERED_DATA_WITH_RETRIES = 0x1701us
    /// Additional Sense Data(RECOVERED DATA WITH RETRIES AND/OR CIRC APPLIED)
    | RECOVERED_DATA_WITH_RETRIES_AND_OR_CIRC_APPLIED = 0x1704us
    /// Additional Sense Data(RECOVERED DATA WITHOUT ECC - DATA AUTO-REALLOCATED)
    | RECOVERED_DATA_WITHOUT_ECC_DATA_AUTO_REALLOCATED = 0x1706us
    /// Additional Sense Data(RECOVERED DATA WITHOUT ECC - DATA REWRITTEN)
    | RECOVERED_DATA_WITHOUT_ECC_DATA_REWRITTEN = 0x1709us
    /// Additional Sense Data(RECOVERED DATA WITHOUT ECC - RECOMMEND REASSIGNMENT)
    | RECOVERED_DATA_WITHOUT_ECC_RECOMMEND_REASSIGNMENT = 0x1707us
    /// Additional Sense Data(RECOVERED DATA WITHOUT ECC - RECOMMEND REWRITE)
    | RECOVERED_DATA_WITHOUT_ECC_RECOMMEND_REWRITE = 0x1708us
    /// Additional Sense Data(RECOVERED ID WITH ECC CORRECTION)
    | RECOVERED_ID_WITH_ECC_CORRECTION = 0x1E00us
    /// Additional Sense Data(REDUNDANCY GROUP CREATED OR MODIFIED)
    | REDUNDANCY_GROUP_CREATED_OR_MODIFIED = 0x3F06us
    /// Additional Sense Data(REDUNDANCY GROUP DELETED)
    | REDUNDANCY_GROUP_DELETED = 0x3F07us
    /// Additional Sense Data(REDUNDANCY LEVEL GOT BETTER)
    | REDUNDANCY_LEVEL_GOT_BETTER = 0x6B01us
    /// Additional Sense Data(REDUNDANCY LEVEL GOT WORSE)
    | REDUNDANCY_LEVEL_GOT_WORSE = 0x6B02us
    /// Additional Sense Data(REGISTRATIONS PREEMPTED)
    | REGISTRATIONS_PREEMPTED = 0x2A05us
    /// Additional Sense Data(REMOVE OF LOGICAL UNIT FAILED)
    | REMOVE_OF_LOGICAL_UNIT_FAILED = 0x6705us
    /// Additional Sense Data(REPORTED LUNS DATA HAS CHANGED)
    | REPORTED_LUNS_DATA_HAS_CHANGED = 0x3F0Eus
    /// Additional Sense Data(REPOSITION ERROR)
    | REPOSITION_ERROR = 0x3B08us
    /// Additional Sense Data(RESERVATIONS PREEMPTED)
    | RESERVATIONS_PREEMPTED = 0x2A03us
    /// Additional Sense Data(RESERVATIONS RELEASED)
    | RESERVATIONS_RELEASED = 0x2A04us
    /// Additional Sense Data(REWIND OPERATION IN PROGRESS)
    | REWIND_OPERATION_IN_PROGRESS = 0x001Aus
    /// Additional Sense Data(RIBBON, INK, OR TONER FAILURE)
    | RIBBON_INK_OR_TONER_FAILURE = 0x3600us
    /// Additional Sense Data(RMA/PMA IS ALMOST FULL)
    | RMA_PMA_IS_ALMOST_FULL = 0x7306us
    /// Additional Sense Data(ROUNDED PARAMETER)
    | ROUNDED_PARAMETER = 0x3700us
    /// Additional Sense Data(RPL STATUS CHANGE)
    | RPL_STATUS_CHANGE = 0x5C00us
    /// Additional Sense Data(SAVING PARAMETERS NOT SUPPORTED)
    | SAVING_PARAMETERS_NOT_SUPPORTED = 0x3900us
    /// Additional Sense Data(SCAN HEAD POSITIONING ERROR)
    | SCAN_HEAD_POSITIONING_ERROR = 0x6200us
    /// Additional Sense Data(SCSI BUS RESET OCCURRED)
    | SCSI_BUS_RESET_OCCURRED = 0x2902us
    /// Additional Sense Data(SCSI PARITY ERROR)
    | SCSI_PARITY_ERROR = 0x4700us
    /// Additional Sense Data(SCSI PARITY ERROR DETECTED DURING ST DATA PHASE)
    | SCSI_PARITY_ERROR_DETECTED_DURING_ST_DATA_PHASE = 0x4702us
    /// Additional Sense Data(SCSI TO HOST SYSTEM INTERFACE FAILURE)
    | SCSI_TO_HOST_SYSTEM_INTERFACE_FAILURE = 0x5400us
    /// Additional Sense Data(SECURITY AUDIT VALUE FROZEN)
    | SECURITY_AUDIT_VALUE_FROZEN = 0x2404us
    /// Additional Sense Data(SECURITY WORKING KEY FROZEN)
    | SECURITY_WORKING_KEY_FROZEN = 0x2405us
    /// Additional Sense Data(SELECT OR RESELECT FAILURE)
    | SELECT_OR_RESELECT_FAILURE = 0x4500us
    /// Additional Sense Data(SEQUENTIAL POSITIONING ERROR)
    | SEQUENTIAL_POSITIONING_ERROR = 0x3B00us
    /// Additional Sense Data(SERVO IMPENDING FAILURE ACCESS TIMES TOO HIGH)
    | SERVO_IMPENDING_FAILURE_ACCESS_TIMES_TOO_HIGH = 0x5D45us
    /// Additional Sense Data(SERVO IMPENDING FAILURE CHANNEL PARAMETRICS)
    | SERVO_IMPENDING_FAILURE_CHANNEL_PARAMETRICS = 0x5D47us
    /// Additional Sense Data(SERVO IMPENDING FAILURE CONTROLLER DETECTED)
    | SERVO_IMPENDING_FAILURE_CONTROLLER_DETECTED = 0x5D48us
    /// Additional Sense Data(SERVO IMPENDING FAILURE DATA ERROR RATE TOO HIGH)
    | SERVO_IMPENDING_FAILURE_DATA_ERROR_RATE_TOO_HIGH = 0x5D42us
    /// Additional Sense Data(SERVO IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT)
    | SERVO_IMPENDING_FAILURE_DRIVE_CALIBRATION_RETRY_COUNT = 0x5D4Cus
    /// Additional Sense Data(SERVO IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH)
    | SERVO_IMPENDING_FAILURE_DRIVE_ERROR_RATE_TOO_HIGH = 0x5D41us
    /// Additional Sense Data(SERVO IMPENDING FAILURE GENERAL HARD DRIVE FAILURE)
    | SERVO_IMPENDING_FAILURE_GENERAL_HARD_DRIVE_FAILURE = 0x5D40us
    /// Additional Sense Data(SERVO IMPENDING FAILURE SEEK ERROR RATE TOO HIGH)
    | SERVO_IMPENDING_FAILURE_SEEK_ERROR_RATE_TOO_HIGH = 0x5D43us
    /// Additional Sense Data(SERVO IMPENDING FAILURE SEEK TIME PERFORMANCE)
    | SERVO_IMPENDING_FAILURE_SEEK_TIME_PERFORMANCE = 0x5D4Aus
    /// Additional Sense Data(SERVO IMPENDING FAILURE SPIN-UP RETRY COUNT)
    | SERVO_IMPENDING_FAILURE_SPIN_UP_RETRY_COUNT = 0x5D4Bus
    /// Additional Sense Data(SERVO IMPENDING FAILURE START UNIT TIMES TOO HIGH)
    | SERVO_IMPENDING_FAILURE_START_UNIT_TIMES_TOO_HIGH = 0x5D46us
    /// Additional Sense Data(SERVO IMPENDING FAILURE THROUGHPUT PERFORMANCE)
    | SERVO_IMPENDING_FAILURE_THROUGHPUT_PERFORMANCE = 0x5D49us
    /// Additional Sense Data(SERVO IMPENDING FAILURE TOO MANY BLOCK REASSIGNS)
    | SERVO_IMPENDING_FAILURE_TOO_MANY_BLOCK_REASSIGNS = 0x5D44us
    /// Additional Sense Data(SESSION FIXATION ERROR)
    | SESSION_FIXATION_ERROR = 0x7200us
    /// Additional Sense Data(SESSION FIXATION ERROR - INCOMPLETE TRACK IN SESSION)
    | SESSION_FIXATION_ERROR_INCOMPLETE_TRACK_IN_SESSION = 0x7203us
    /// Additional Sense Data(SESSION FIXATION ERROR WRITING LEAD-IN)
    | SESSION_FIXATION_ERROR_WRITING_LEAD_IN = 0x7201us
    /// Additional Sense Data(SESSION FIXATION ERROR WRITING LEAD-OUT)
    | SESSION_FIXATION_ERROR_WRITING_LEAD_OUT = 0x7202us
    /// Additional Sense Data(SET CAPACITY OPERATION IN PROGRESS)
    | SET_CAPACITY_OPERATION_IN_PROGRESS = 0x001Bus
    /// Additional Sense Data(SET TARGET PORT GROUPS COMMAND FAILED)
    | SET_TARGET_PORT_GROUPS_COMMAND_FAILED = 0x670Aus
    /// Additional Sense Data(SETMARK DETECTED)
    | SETMARK_DETECTED = 0x0003us
    /// Additional Sense Data(SLEW FAILURE)
    | SLEW_FAILURE = 0x3B04us
    /// Additional Sense Data(SOME COMMANDS CLEARED BY ISCSI PROTOCOL EVENT)
    | SOME_COMMANDS_CLEARED_BY_ISCSI_PROTOCOL_EVENT = 0x477Fus
    /// Additional Sense Data(SPARE AREA EXHAUSTION PREDICTION THRESHOLD EXCEEDED)
    | SPARE_AREA_EXHAUSTION_PREDICTION_THRESHOLD_EXCEEDED = 0x5D03us
    /// Additional Sense Data(SPARE CREATED OR MODIFIED)
    | SPARE_CREATED_OR_MODIFIED = 0x3F08us
    /// Additional Sense Data(SPARE DELETED)
    | SPARE_DELETED = 0x3F09us
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE ACCESS TIMES TOO HIGH)
    | SPINDLE_IMPENDING_FAILURE_ACCESS_TIMES_TOO_HIGH = 0x5D55us
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE CHANNEL PARAMETRICS)
    | SPINDLE_IMPENDING_FAILURE_CHANNEL_PARAMETRICS = 0x5D57us
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE CONTROLLER DETECTED)
    | SPINDLE_IMPENDING_FAILURE_CONTROLLER_DETECTED = 0x5D58us
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE DATA ERROR RATE TOO HIGH)
    | SPINDLE_IMPENDING_FAILURE_DATA_ERROR_RATE_TOO_HIGH = 0x5D52us
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT)
    | SPINDLE_IMPENDING_FAILURE_DRIVE_CALIBRATION_RETRY_COUNT = 0x5D5Cus
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH)
    | SPINDLE_IMPENDING_FAILURE_DRIVE_ERROR_RATE_TOO_HIGH = 0x5D51us
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE GENERAL HARD DRIVE FAILURE)
    | SPINDLE_IMPENDING_FAILURE_GENERAL_HARD_DRIVE_FAILURE = 0x5D50us
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE SEEK ERROR RATE TOO HIGH)
    | SPINDLE_IMPENDING_FAILURE_SEEK_ERROR_RATE_TOO_HIGH = 0x5D53us
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE SEEK TIME PERFORMANCE)
    | SPINDLE_IMPENDING_FAILURE_SEEK_TIME_PERFORMANCE = 0x5D5Aus
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE SPIN-UP RETRY COUNT)
    | SPINDLE_IMPENDING_FAILURE_SPIN_UP_RETRY_COUNT = 0x5D5Bus
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE START UNIT TIMES TOO HIGH)
    | SPINDLE_IMPENDING_FAILURE_START_UNIT_TIMES_TOO_HIGH = 0x5D56us
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE THROUGHPUT PERFORMANCE)
    | SPINDLE_IMPENDING_FAILURE_THROUGHPUT_PERFORMANCE = 0x5D59us
    /// Additional Sense Data(SPINDLE IMPENDING FAILURE TOO MANY BLOCK REASSIGNS)
    | SPINDLE_IMPENDING_FAILURE_TOO_MANY_BLOCK_REASSIGNS = 0x5D54us
    /// Additional Sense Data(SPINDLE SERVO FAILURE)
    | SPINDLE_SERVO_FAILURE = 0x0903us
    /// Additional Sense Data(SPINDLES NOT SYNCHRONIZED)
    | SPINDLES_NOT_SYNCHRONIZED = 0x5C02us
    /// Additional Sense Data(SPINDLES SYNCHRONIZED)
    | SPINDLES_SYNCHRONIZED = 0x5C01us
    /// Additional Sense Data(STANDBY CONDITION ACTIVATED BY COMMAND)
    | STANDBY_CONDITION_ACTIVATED_BY_COMMAND = 0x5E04us
    /// Additional Sense Data(STANDBY CONDITION ACTIVATED BY TIMER)
    | STANDBY_CONDITION_ACTIVATED_BY_TIMER = 0x5E02us
    /// Additional Sense Data(STATE CHANGE HAS OCCURRED)
    | STATE_CHANGE_HAS_OCCURRED = 0x6B00us
    /// Additional Sense Data(SYNCHRONOUS DATA TRANSFER ERROR)
    | SYNCHRONOUS_DATA_TRANSFER_ERROR = 0x1B00us
    /// Additional Sense Data(SYSTEM BUFFER FULL)
    | SYSTEM_BUFFER_FULL = 0x5501us
    /// Additional Sense Data(SYSTEM RESOURCE FAILURE)
    | SYSTEM_RESOURCE_FAILURE = 0x5500us
    /// Additional Sense Data(TAGGED OVERLAPPED COMMANDS (NN = TASK TAG))
    | TAGGED_OVERLAPPED_COMMANDS_NN = 0x4D00us
    /// Additional Sense Data(TAPE LENGTH ERROR)
    | TAPE_LENGTH_ERROR = 0x3300us
    /// Additional Sense Data(TAPE OR ELECTRONIC VERTICAL FORMS UNIT NOT READY)
    | TAPE_OR_ELECTRONIC_VERTICAL_FORMS_UNIT_NOT_READY = 0x3B03us
    /// Additional Sense Data(TAPE POSITION ERROR AT BEGINNING-OF-MEDIUM)
    | TAPE_POSITION_ERROR_AT_BEGINNING_OF_MEDIUM = 0x3B01us
    /// Additional Sense Data(TAPE POSITION ERROR AT END-OF-MEDIUM)
    | TAPE_POSITION_ERROR_AT_END_OF_MEDIUM = 0x3B02us
    /// Additional Sense Data(TARGET OPERATING CONDITIONS HAVE CHANGED)
    | TARGET_OPERATING_CONDITIONS_HAVE_CHANGED = 0x3F00us
    /// Additional Sense Data(THIRD PARTY DEVICE FAILURE)
    | THIRD_PARTY_DEVICE_FAILURE = 0x0D01us
    /// Additional Sense Data(THRESHOLD CONDITION MET)
    | THRESHOLD_CONDITION_MET = 0x5B01us
    /// Additional Sense Data(THRESHOLD PARAMETERS NOT SUPPORTED)
    | THRESHOLD_PARAMETERS_NOT_SUPPORTED = 0x2603us
    /// Additional Sense Data(TIMEOUT ON LOGICAL UNIT)
    | TIMEOUT_ON_LOGICAL_UNIT = 0x3E02us
    /// Additional Sense Data(TIMESTAMP CHANGED)
    | TIMESTAMP_CHANGED = 0x2A10us
    /// Additional Sense Data(TOO MANY SEGMENT DESCRIPTORS)
    | TOO_MANY_SEGMENT_DESCRIPTORS = 0x2608us
    /// Additional Sense Data(TOO MANY TARGET DESCRIPTORS)
    | TOO_MANY_TARGET_DESCRIPTORS = 0x2606us
    /// Additional Sense Data(TOO MANY WINDOWS SPECIFIED)
    | TOO_MANY_WINDOWS_SPECIFIED = 0x2C01us
    /// Additional Sense Data(TOO MUCH WRITE DATA)
    | TOO_MUCH_WRITE_DATA = 0x4B02us
    /// Additional Sense Data(TRACK FOLLOWING ERROR)
    | TRACK_FOLLOWING_ERROR = 0x0900us
    /// Additional Sense Data(TRACKING SERVO FAILURE)
    | TRACKING_SERVO_FAILURE = 0x0901us
    /// Additional Sense Data(TRANSCEIVER MODE CHANGED TO LVD)
    | TRANSCEIVER_MODE_CHANGED_TO_LVD = 0x2906us
    /// Additional Sense Data(TRANSCEIVER MODE CHANGED TO SINGLE-ENDED)
    | TRANSCEIVER_MODE_CHANGED_TO_SINGLE_ENDED = 0x2905us
    /// Additional Sense Data(UNABLE TO ACQUIRE VIDEO)
    | UNABLE_TO_ACQUIRE_VIDEO = 0x6101us
    /// Additional Sense Data(UNABLE TO RECOVER TABLE-OF-CONTENTS)
    | UNABLE_TO_RECOVER_TABLE_OF_CONTENTS = 0x5700us
    /// Additional Sense Data(UNEXPECTED INEXACT SEGMENT)
    | UNEXPECTED_INEXACT_SEGMENT = 0x260Aus
    /// Additional Sense Data(UNLOAD TAPE FAILURE)
    | UNLOAD_TAPE_FAILURE = 0x5301us
    /// Additional Sense Data(UNREACHABLE COPY TARGET)
    | UNREACHABLE_COPY_TARGET = 0x0804us
    /// Additional Sense Data(UNRECOVERED READ ERROR)
    | UNRECOVERED_READ_ERROR = 0x1100us
    /// Additional Sense Data(UNRECOVERED READ ERROR - AUTO REALLOCATE FAILED)
    | UNRECOVERED_READ_ERROR_AUTO_REALLOCATE_FAILED = 0x1104us
    /// Additional Sense Data(UNRECOVERED READ ERROR - RECOMMEND REASSIGNMENT)
    | UNRECOVERED_READ_ERROR_RECOMMEND_REASSIGNMENT = 0x110Bus
    /// Additional Sense Data(UNRECOVERED READ ERROR - RECOMMEND REWRITE THE DATA)
    | UNRECOVERED_READ_ERROR_RECOMMEND_REWRITE_THE_DATA = 0x110Cus
    /// Additional Sense Data(UNSUCCESSFUL SOFT RESET)
    | UNSUCCESSFUL_SOFT_RESET = 0x4600us
    /// Additional Sense Data(UNSUPPORTED ENCLOSURE FUNCTION)
    | UNSUPPORTED_ENCLOSURE_FUNCTION = 0x3501us
    /// Additional Sense Data(UNSUPPORTED SEGMENT DESCRIPTOR TYPE CODE)
    | UNSUPPORTED_SEGMENT_DESCRIPTOR_TYPE_CODE = 0x2609us
    /// Additional Sense Data(UNSUPPORTED TARGET DESCRIPTOR TYPE CODE)
    | UNSUPPORTED_TARGET_DESCRIPTOR_TYPE_CODE = 0x2607us
    /// Additional Sense Data(UPDATED BLOCK READ)
    | UPDATED_BLOCK_READ = 0x5900us
    /// Additional Sense Data(VERIFY OPERATION IN PROGRESS)
    | VERIFY_OPERATION_IN_PROGRESS = 0x001Cus
    /// Additional Sense Data(VIDEO ACQUISITION ERROR)
    | VIDEO_ACQUISITION_ERROR = 0x6100us
    /// Additional Sense Data(VOLTAGE FAULT)
    | VOLTAGE_FAULT = 0x6500us
    /// Additional Sense Data(VOLUME SET CREATED OR MODIFIED)
    | VOLUME_SET_CREATED_OR_MODIFIED = 0x3F0Aus
    /// Additional Sense Data(VOLUME SET DEASSIGNED)
    | VOLUME_SET_DEASSIGNED = 0x3F0Cus
    /// Additional Sense Data(VOLUME SET DELETED)
    | VOLUME_SET_DELETED = 0x3F0Bus
    /// Additional Sense Data(VOLUME SET REASSIGNED)
    | VOLUME_SET_REASSIGNED = 0x3F0Dus
    /// Additional Sense Data(WARNING)
    | WARNING = 0x0B00us
    /// Additional Sense Data(WARNING - ENCLOSURE DEGRADED)
    | WARNING_ENCLOSURE_DEGRADED = 0x0B02us
    /// Additional Sense Data(WARNING - SPECIFIED TEMPERATURE EXCEEDED)
    | WARNING_SPECIFIED_TEMPERATURE_EXCEEDED = 0x0B01us
    /// Additional Sense Data(WORM MEDIUM - OVERWRITE ATTEMPTED)
    | WORM_MEDIUM_OVERWRITE_ATTEMPTED = 0x300Cus
    /// Additional Sense Data(WRITE APPEND ERROR)
    | WRITE_APPEND_ERROR = 0x5000us
    /// Additional Sense Data(WRITE APPEND POSITION ERROR)
    | WRITE_APPEND_POSITION_ERROR = 0x5001us
    /// Additional Sense Data(WRITE ERROR)
    | WRITE_ERROR = 0x0C00us
    /// Additional Sense Data(WRITE ERROR - AUTO REALLOCATION FAILED)
    | WRITE_ERROR_AUTO_REALLOCATION_FAILED = 0x0C02us
    /// Additional Sense Data(WRITE ERROR - LOSS OF STREAMING)
    | WRITE_ERROR_LOSS_OF_STREAMING = 0x0C09us
    /// Additional Sense Data(WRITE ERROR - NOT ENOUGH UNSOLICITED DATA)
    | WRITE_ERROR_NOT_ENOUGH_UNSOLICITED_DATA = 0x0C0Dus
    /// Additional Sense Data(WRITE ERROR - PADDING BLOCKS ADDED)
    | WRITE_ERROR_PADDING_BLOCKS_ADDED = 0x0C0Aus
    /// Additional Sense Data(WRITE ERROR - RECOMMEND REASSIGNMENT)
    | WRITE_ERROR_RECOMMEND_REASSIGNMENT = 0x0C03us
    /// Additional Sense Data(WRITE ERROR - RECOVERED WITH AUTO REALLOCATION)
    | WRITE_ERROR_RECOVERED_WITH_AUTO_REALLOCATION = 0x0C01us
    /// Additional Sense Data(WRITE ERROR - RECOVERY FAILED)
    | WRITE_ERROR_RECOVERY_FAILED = 0x0C08us
    /// Additional Sense Data(WRITE ERROR - RECOVERY NEEDED)
    | WRITE_ERROR_RECOVERY_NEEDED = 0x0C07us
    /// Additional Sense Data(WRITE ERROR - UNEXPECTED UNSOLICITED DATA)
    | WRITE_ERROR_UNEXPECTED_UNSOLICITED_DATA = 0x0C0Cus
    /// Additional Sense Data(WRITE PROTECTED)
    | WRITE_PROTECTED = 0x2700us
    /// Additional Sense Data(ZONED FORMATTING FAILED DUE TO SPARE LINKING)
    | ZONED_FORMATTING_FAILED_DUE_TO_SPARE_LINKING = 0x3102us




/// Constants definitions
module Constants =

    type internal TypeMarker = interface end
    let constants_type_maker = typeof<TypeMarker>.DeclaringType

    //=============================================================================
    // Persistently stable constatns.

    /// Search by ignore case for path name or not
    let REGEXOPT_SEARCH_FLAG : RegexOptions =
        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
            RegexOptions.Compiled ||| RegexOptions.IgnoreCase ||| RegexOptions.CultureInvariant
        else
            RegexOptions.Compiled

    /// Name of this product.
    let PRODUCT_NAME : string =
        "Haruka"

    /// Description string of this production.
    let PRODUCT_DESCRIPTION : string =
        "Haruka software storage."

    /// Haruka controller program file name.
    let CONTROLLER_EXE_NAME : string =
        "Controller.exe"

    /// Haruka controller configuration file name
    let CONTROLLER_CONF_FILE_NAME : string =
        "Haruka.conf"

    /// Configuration directory lock file name.
    let CONTROLLER_LOCK_NAME : string =
        "lock.txt"

    /// Haruka controller log directory name
    let CONTROLLER_LOG_DIR_NAME : string =
        "log"

    /// string format regex patern of GUID
    let GUID_STRING_FORMAT_REGEX : string =
        "[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}"

    /// Target device working directory name prefix
    let TARGET_DEVICE_DIR_PREFIX : string =
        "TD_"

    /// Target device working directory name regex pattern
    let TARGET_DEVICE_DIR_NAME_REGEX : string =
        "^" + TARGET_DEVICE_DIR_PREFIX + "[0-9a-fA-F]{8}$"

    /// Target device working directory name regex pattern
    let TARGET_DEVICE_DIR_NAME_REGOBJ : Regex =
        new Regex( TARGET_DEVICE_DIR_NAME_REGEX, REGEXOPT_SEARCH_FLAG )

    /// Target device configuration file name. 
    let TARGET_DEVICE_CONF_FILE_NAME : string =
        "TargetDevice.conf"

    /// Target device program file name.
    let TARGET_DEVICE_EXE_NAME : string =
        "TargetDevice.exe"

    /// Target device program file name.
    let MEDIA_CREATION_EXE_NAME : string =
        "Controller.exe"

    /// Target group configuration file prefix.
    let TARGET_GRP_CONFIG_FILE_PREFIX : string =
        "TG_"

    /// Target group configuration file regex pattern
    let TARGET_GRP_CONFIG_FILE_NAME_REGEX : string =
        "^" + TARGET_GRP_CONFIG_FILE_PREFIX + "[0-9a-fA-F]{8}$"

    /// Target device working directory name regex pattern
    let TARGET_GRP_CONFIG_FILE_NAME_REGOBJ : Regex =
        new Regex( TARGET_GRP_CONFIG_FILE_NAME_REGEX, REGEXOPT_SEARCH_FLAG )

    /// Regex pattern which checks string matches long LUN pattern.
    let LONG_LUN_REGEX : string =
        "[0-9]{1,}"

    /// Logical unit working directory name prefix
    let LU_WORK_DIR_PREFIX : string =
        "LU_"

    /// Target device working directory name regex pattern
    let LU_WORK_DIR_NAME_REGEX : string =
        "^" + LU_WORK_DIR_PREFIX + LONG_LUN_REGEX + "$"

    /// Minimum legal LUN value
    let MIN_LUN_VALUE : uint64 =
        0UL

    /// Maximum legal LUN value
    let MAX_LUN_VALUE : uint64 =
        255UL

    /// Target device working directory name regex pattern
    let LU_WORK_DIR_NAME_REGOBJ : Regex =
        new Regex( LU_WORK_DIR_NAME_REGEX, REGEXOPT_SEARCH_FLAG )

    /// Contoroller session ID prefix value.
    let CTRL_SESS_ID_PREFIX : string =
        "CSI_"

    /// Persistent Reservation save file name.
    let PR_SAVE_FILE_NAME : string =
        "PersistentReservation.txt"

    /// XSD file name that used to validation of Persistent Reservation XML file.
    let PERSISTENT_RESERVATION_VALIDATE_FILE_NAME : string =
        "BlockDeviceLU.PRFileValidate.xsd"

    /// Data format of "standard-label" used at iSCSI text request/responce. Reffer RFC 3720 5.1 Text Format.
    let ISCSI_TEXT_STANDERD_LABE_REGEX_STR : string =
        "^[A-Z][a-zA-Z0-9\.\-+@_]{0,62}$"

    /// Regex object for ISCSI_TEXT_STANDERD_LABE_REGEX_STR
    let ISCSI_TEXT_STANDERD_LABE_REGEX_OBJ : Regex =
        new Regex( ISCSI_TEXT_STANDERD_LABE_REGEX_STR, RegexOptions.Compiled )

    /// Data format of "text-value" used at iSCSI text request/responce. Reffer RFC 3720 5.1 Text Format.
    let ISCSI_TEXT_TEXT_VALUE_REGEX_STR : string =
        "^[a-zA-Z0-9\.\-+@_/\[\]\:]*$"

    /// Regex object for ISCSI_TEXT_TEXT_VALUE_REGEX_STR
    let ISCSI_TEXT_TEXT_VALUE_REGEX_OBJ : Regex =
        new Regex( ISCSI_TEXT_TEXT_VALUE_REGEX_STR, RegexOptions.Compiled )

    /// Data format of "list-of-values" used at iSCSI text request/responce. Reffer RFC 3720 5.1 Text Format.
    let ISCSI_TEXT_LIST_OF_VALUES_REGEX_STR : string =
        "^[a-zA-Z0-9\.\-+@_/\[\]\:\,]*$"

    /// Regex object for ISCSI_TEXT_LIST_OF_VALUES_REGEX_STR
    let ISCSI_TEXT_LIST_OF_VALUES_REGEX_OBJ : Regex =
        new Regex( ISCSI_TEXT_LIST_OF_VALUES_REGEX_STR, RegexOptions.Compiled )

    /// Maximum "iSCSI-name-value" length. Reffer RFC 3720 3.2.6.2.
    let ISCSI_TEXT_MAX_ISCSI_NAME_LENGTH : int =
        223

    /// Data format of "iSCSI-name-value" used at iSCSI text request/responce. Reffer RFC 3720 5.1 Text Format.
    let ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_STR : string =
        sprintf "^[\-\.\:a-z0-9]{1,%d}$" ISCSI_TEXT_MAX_ISCSI_NAME_LENGTH

    /// Regex object for ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_STR
    let ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_OBJ : Regex =
        new Regex( ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_STR, RegexOptions.Compiled )

    /// Data format of "hex-constant" used at iSCSI text request/responce. Reffer RFC 3720 5.1 Text Format.
    let ISCSI_TEXT_HEX_CONSTANT_REGEX_STR : string =
        "^0(X|x)[0-9a-fA-F]+$"

    /// Regex object for ISCSI_TEXT_HEX_CONSTANT_REGEX_STR
    let ISCSI_TEXT_HEX_CONSTANT_REGEX_OBJ : Regex =
        new Regex( ISCSI_TEXT_HEX_CONSTANT_REGEX_STR, RegexOptions.Compiled )

    /// Data format of "decimal-constant" used at iSCSI text request/responce. Reffer RFC 3720 5.1 Text Format.
    let ISCSI_TEXT_DECIMAL_CONSTANT_REGEX_STR : string =
        "^0|([1-9][0-9]+)$"

    /// Regex object for ISCSI_TEXT_DECIMAL_CONSTANT_REGEX_STR
    let ISCSI_TEXT_DECIMAL_CONSTANT_REGEX_OBJ : Regex =
        new Regex( ISCSI_TEXT_DECIMAL_CONSTANT_REGEX_STR, RegexOptions.Compiled )

    /// Data format of "base64-constant" used at iSCSI text request/responce. Reffer RFC 3720 5.1 Text Format.
    let ISCSI_TEXT_BASE64_CONSTANT_REGEX_STR : string =
        "^0(B|b)[0-9a-zA-Z\+\/]+\=*$"

    /// Regex object for ISCSI_TEXT_BASE64_CONSTANT_REGEX_STR
    let ISCSI_TEXT_BASE64_CONSTANT_REGEX_OBJ : Regex =
        new Regex( ISCSI_TEXT_BASE64_CONSTANT_REGEX_STR, RegexOptions.Compiled )

    //=============================================================================
    // Constants definitions. Thay my be changed in the future.

    /// Product version number ( mejor )
    let MAJOR_VERSION : uint =
        1u

    /// Product version number ( minor )
    let MINOR_VERSION : uint =
        0u

    /// Product version number ( rivision )
    let PRODUCT_RIVISION : uint64 =
        0UL

    /// Product release date
    let PRODUCT_RELEASE_DATE : System.DateTime =
        System.DateTime( 2025, 1, 1, 0, 0, 0, System.DateTimeKind.Utc )

    //=============================================================================
    // In BHS bits

    /// Immidiate(I) bit
    let IMMIDIATE_BIT : byte =
        0x40uy

    /// Final(F) bit
    let FINAL_BIT : byte =
        0x80uy

    /// Transit(T) bit
    let TRANSIT_BIT : byte =
        0x80uy

    /// Continue(C) bit
    let CONTINUE_BIT : byte =
        0x40uy

    /// Read(R) bit
    let READ_BIT : byte =
        0x40uy

    /// Read(W) bit
    let WRITE_BIT : byte =
        0x20uy

    /// Ackowledge(A)
    let ACKNOWLEDGE_BIT : byte =
        0x40uy

    /// Bidirectional Read Residual Overflow(o)
    let BI_READ_RESIDUAL_OV_BIT : byte =
        0x10uy

    /// Bidirectional Read Residual Underflow(u)
    let BI_READ_RESIDUAL_UND_BIT : byte =
        0x08uy

    /// Residual Overflow(O)
    let RESIDUAL_OVERFLOW_BIT : byte =
        0x04uy

    /// Residual Underflow(U)
    let RESIDUAL_UNDERFLOW_BIT : byte =
        0x02uy

    /// Status(S)
    let STATUS_BIT : byte =
        0x01uy

    //=============================================================================
    // Internal parameters.

    /// Max task count in task set.
    let BDLU_MAX_TASKSET_SIZE : uint32 =
        64u

    /// Max log soft limit size.
    let LOGPARAM_MAX_SOFTLIMIT : uint32 =
        10000000u

    /// Min log soft limit size.
    let LOGPARAM_MIN_SOFTLIMIT : uint32 =
        0u

    /// Default log soft limit size.
    let LOGPARAM_DEF_SOFTLIMIT : uint32 =
        10000u

    /// Max log hard limit size.
    let LOGPARAM_MAX_HARDLIMIT : uint32 =
        20000000u

    /// Min log hard limit size.
    let LOGPARAM_MIN_HARDLIMIT : uint32 =
        100u

    /// Default log hard limit size.
    let LOGPARAM_DEF_HARDLIMIT : uint32 =
        20000u

    /// Max total log limit size.
    let LOGMNT_MAX_TOTALLIMIT : uint32 =
        10000000u

    /// Min total log limit size.
    let LOGMNT_MIN_TOTALLIMIT : uint32 =
        1u

    /// Default total log limit size.
    let LOGMNT_DEF_TOTALLIMIT : uint32 =
        30000u

    /// Default total log limit size.
    let LOGMNT_DEF_FORCESYNC : bool =
        false

    /// Min maximum log file count value.
    let LOGMNT_MIN_MAXFILECOUNT : uint32 =
        1u

    /// Max maximum log file count value.
    let LOGMNT_MAX_MAXFILECOUNT : uint32 =
        1024u

    /// Default max log file count
    let LOGMNT_DEF_MAXFILECOUNT : uint32 =
        30u

    /// Default log output cycle value.
    let DEFAULT_LOG_OUTPUT_CYCLE : uint32 =
        1000u

    /// Value of MaxConnections. Harula does not support the function of constraining the maximum number of connections.
    let NEGOPARAM_MaxConnections : uint16 =
        16us

    /// Maximum value of MaxRecvDataSegmentLength.
    let NEGOPARAM_MAX_MaxRecvDataSegmentLength : uint32 =
        16777215u

    /// Minimum value of MaxRecvDataSegmentLength.
    let NEGOPARAM_MIN_MaxRecvDataSegmentLength : uint32 =
        512u

    /// Default value of MaxRecvDataSegmentLength.
    let NEGOPARAM_DEF_MaxRecvDataSegmentLength : uint32 =
        8192u

    /// Maximum value of MaxBurstLength.
    let NEGOPARAM_MAX_MaxBurstLength : uint32 =
        16777215u

    /// Minimum value of MaxBurstLength.
    let NEGOPARAM_MIN_MaxBurstLength : uint32 =
        512u

    /// Default value of MaxBurstLength.
    let NEGOPARAM_DEF_MaxBurstLength : uint32 =
        262144u

    /// Maximum value of FirstBurstLength.
    let NEGOPARAM_MAX_FirstBurstLength : uint32 =
        16777215u

    /// Minimum value of FirstBurstLength.
    let NEGOPARAM_MIN_FirstBurstLength : uint32 =
        512u

    /// Default value of FirstBurstLength.
    let NEGOPARAM_DEF_FirstBurstLength : uint32 =
        65536u

    /// Maximum value of DefaultTime2Wait.
    let NEGOPARAM_MAX_DefaultTime2Wait : uint16 =
        3600us

    /// Minimum value of DefaultTime2Wait.
    let NEGOPARAM_MIN_DefaultTime2Wait : uint16 =
        0us

    /// Default value of DefaultTime2Wait.
    let NEGOPARAM_DEF_DefaultTime2Wait : uint16 =
        2us

    /// Maximum value of DefaultTime2Retain.
    let NEGOPARAM_MAX_DefaultTime2Retain : uint16 =
        3600us

    /// Minimum value of DefaultTime2Retain.
    let NEGOPARAM_MIN_DefaultTime2Retain : uint16 =
        0us

    /// Default value of DefaultTime2Retain.
    let NEGOPARAM_DEF_DefaultTime2Retain : uint16 =
        20us

    /// Maximum value of MaxOutstandingR2T.
    let NEGOPARAM_MAX_MaxOutstandingR2T : uint16 =
        65535us

    /// Minimum value of MaxOutstandingR2T.
    let NEGOPARAM_MIN_MaxOutstandingR2T : uint16 =
        1us

    /// Default value of MaxOutstandingR2T.
    let NEGOPARAM_DEF_MaxOutstandingR2T : uint16 =
        65535us

    /// In error recovery level zero, max differential of StatSN and ExpStatSN that occurs the session recovery.
    let MAX_STATSN_DIFF : uint32 =
        32u

    /// Max network portal count in target device.
    let MAX_NETWORK_PORTAL_COUNT : int =
        16

    /// Maximum string length that can be used in the TCP incoming standby address in controller.
    let MAX_CTRL_ADDRESS_STR_LENGTH : int =
        256

    /// Maximum string length that can be used in the TCP incoming standby address in target.
    let MAX_TARGET_ADDRESS_STR_LENGTH : int =
        32768

    /// Maximum string length allowed in target device name.
    let MAX_DEVICE_NAME_STR_LENGTH : int =
        512

    /// Maximum string length allowed in target group name.
    let MAX_TARGET_GROUP_NAME_STR_LENGTH : int =
        256

    /// Maximum string length allowed in target alias name.
    let MAX_TARGET_ALIAS_STR_LENGTH : int =
        256

    /// Maximum string length allowed in LU name.
    let MAX_LU_NAME_STR_LENGTH : int =
        256

    /// Maximum string length allowed in media name.
    let MAX_MEDIA_NAME_STR_LENGTH : int =
        256

    /// Maximum string length allowed in CHAP user name.
    let MAX_USER_NAME_STR_LENGTH : int =
        256

    /// Maximum string length allowed in CHAP password.
    let MAX_PASSWORD_STR_LENGTH : int =
        256

    /// Maximum string length allowed in file names.
    let MAX_FILENAME_STR_LENGTH : int =
        256

    let USER_NAME_REGEX_STR : string =
        sprintf "^[a-zA-Z0-9\.\-+@_/\[\]\:]{1,%d}$" MAX_USER_NAME_STR_LENGTH

    let USER_NAME_REGEX_OBJ : Regex =
        new Regex( USER_NAME_REGEX_STR, RegexOptions.Compiled )

    let PASSWORD_REGEX_STR : string =
        sprintf "^[a-zA-Z0-9\.\-+@_/\[\]\:]{1,%d}$" MAX_PASSWORD_STR_LENGTH

    let PASSWORD_REGEX_OBJ : Regex =
        new Regex( PASSWORD_REGEX_STR, RegexOptions.Compiled )

    /// Max target device count in on haruka controller configuration.
    let MAX_TARGET_DEVICE_COUNT : int =
        16

    /// Max target group count in one target device configuration.
    let MAX_TARGET_GROUP_COUNT_IN_TD : int =
        255

    /// Max target count in one target device.
    /// (= This value is same as maximum target count in one target group.)
    let MAX_TARGET_COUNT_IN_TD : int =
        255

    /// Max LU count in one target device.
    /// (= This value is same as maximum LU count in one target group.)
    let MAX_LOGICALUNIT_COUNT_IN_TD : int =
        255

    /// The block size of media reported to the initiator.
    let MEDIA_BLOCK_SIZE : uint64 =
        4096UL

    /// Minimum multiplicity for plain file media.
    let PLAINFILE_MIN_MAXMULTIPLICITY : uint32 =
        1u

    /// Maximum multiplicity for plain file media.
    let PLAINFILE_MAX_MAXMULTIPLICITY : uint32 =
        32u

    /// Default multiplicity for plain file media.
    let PLAINFILE_DEF_MAXMULTIPLICITY : uint32 =
        10u

    /// Minimum queue wait timeout value in millisecond for plain file media.
    let PLAINFILE_MIN_QUEUEWAITTIMEOUT : int =
        50

    /// Maximum queue wait timeout value in millisecond for plain file media.
    let PLAINFILE_MAX_QUEUEWAITTIMEOUT : int =
        3000000

    /// Default queue wait timeout value in millisecond for plain file media.
    let PLAINFILE_DEF_QUEUEWAITTIMEOUT : int =
        10000
    
    /// Buffer line size of Memory buffer media in block count.
    /// If block size(MEDIA_BLOCK_SIZE) is 512B and line size 64MB, it should be 131072 blocks.
    /// If block size is 4096B and line size 64MB, it should be 16384 blocks.
    let MEMBUFFER_BUF_LINE_BLOCK_SIZE : uint64 =
        131072UL

    /// A constant that indicates the maximum number of persistent reservation registrations that can be registered.
    let PRDATA_MAX_REGISTRATION_COUNT : int =
        65535

    /// iSCSI default port number.
    let DEFAULT_ISCSI_PORT_NUM : uint16 =
        3260us

    /// Management client default port number.
    let DEFAULT_MNG_CLI_PORT_NUM : uint16 =
        28260us

    /// Session expiration
    let CONTROLLER_SESSION_LIFE_TIME : float =
        180.0

    /// Maximum number of attempts to restart a child process in a given time period.
    let MAX_CHILD_PROC_RESTART_COUNT : int =
        3

    /// Default ReceiveBufferSize of network portal configuration.
    let DEF_RECEIVE_BUFFER_SIZE_IN_NP : int =
        262144

    /// Default SendBufferSize of network portal configuration.
    let DEF_SEND_BUFFER_SIZE_IN_NP : int =
        262144

    /// Default DisableNagle of network portal configuration.
    let DEF_DISABLE_NAGLE_IN_NP : bool =
        true

    /// Default iSCSI target name prefix
    let DEF_ISCSI_TARGET_NAME_PREFIX : string =
        "iqn.1999-01.com.example:"

    /// Directory name where the resource files are stored.
    let RESOURCE_DIR_NAME : string =
        "Resource"

    /// Resource file name which defines message string.
    let MESSAGE_RESX_FILE_NAME : string =
        "Messages"

    /// Default culture name that is used when the resource file corresponde for current culture is missing.
    let DEFAULT_CULTURE_NAME : string =
        "en-US"

    /// Maximum number of InitMedia processes that can run simultaneously.
    let INITMEDIA_MAX_MULTIPLICITY : int =
        4

    /// Maximum number of seconds that a terminated InitMedia process remains.
    let INITMEDIA_MAX_REMAIN_TIME : int =
        3600

    /// Maximum length of a error message that is wrote by InitMedia process.
    let INITMEDIA_MAX_ERRMSG_LENGTH : int =
        256

    /// Maximum number of error messages that is wrote by InitMedia process.
    let INITMEDIA_MAX_ERRMSG_COUNT : int =
        16

    /// Resource counter unit time in second.
    let RECOUNTER_SPAN_SEC : int64 =
        3L

    /// Time in seconds measured by recource counter.
    let RESCOUNTER_LENGTH_SEC : int64 =
        180L

    /// Muximum resource counter values count
    let RESCOUNTER_MAX_VAL_COUNT : int64 =
        ( RESCOUNTER_LENGTH_SEC / RECOUNTER_SPAN_SEC + 2L )

    /// Muximum session count in the target device
    /// Even in the maximum configuration, a minimum of two logins per target are allowed.
    let MAX_SESSION_COUNT_IN_TD : int =
        MAX_TARGET_COUNT_IN_TD * 2

    /// Muximum connection count in the target device
    let MAX_CONNECTION_COUNT_IN_TD : int =
        MAX_SESSION_COUNT_IN_TD * ( int NEGOPARAM_MaxConnections )

    /// Maximum number of sessions per target.
    let MAX_SESSION_COUNT_IN_TARGET : int =
        16

    /// Maximum number of sessions per LU
    /// Even in the maximum configuration, a minimum of two logins per target are allowed.
    let MAX_SESSION_COUNT_IN_LU : int =
        MAX_TARGET_COUNT_IN_TD * 2

    /// Maximum IP white list conditions count
    let MAX_IP_WHITELIST_COUNT : int =
        16

    let DEBUG_MEDIA_MAX_TRAP_COUNT : int =
        16

    //=============================================================================
    // Constants convertion functions

    /// Convert byte value to iSCSI Initiator Opcode values
    let byteToOpcodeCd ( a : byte ) ( errcont : byte -> OpcodeCd ) : OpcodeCd =
        if a = byte OpcodeCd.NOP_OUT then
            OpcodeCd.NOP_OUT
        elif a = byte OpcodeCd.SCSI_COMMAND then
            OpcodeCd.SCSI_COMMAND
        elif a = byte OpcodeCd.SCSI_TASK_MGR_REQ then
            OpcodeCd.SCSI_TASK_MGR_REQ
        elif a = byte OpcodeCd.LOGIN_REQ then
            OpcodeCd.LOGIN_REQ
        elif a = byte OpcodeCd.TEXT_REQ then
            OpcodeCd.TEXT_REQ
        elif a = byte OpcodeCd.SCSI_DATA_OUT then
            OpcodeCd.SCSI_DATA_OUT
        elif a = byte OpcodeCd.LOGOUT_REQ then
            OpcodeCd.LOGOUT_REQ
        elif a = byte OpcodeCd.SNACK then
            OpcodeCd.SNACK
        elif a = byte OpcodeCd.NOP_IN then
            OpcodeCd.NOP_IN
        elif a = byte OpcodeCd.SCSI_RES then
            OpcodeCd.SCSI_RES
        elif a = byte OpcodeCd.SCSI_TASK_MGR_RES then
            OpcodeCd.SCSI_TASK_MGR_RES
        elif a = byte OpcodeCd.LOGIN_RES then
            OpcodeCd.LOGIN_RES
        elif a = byte OpcodeCd.TEXT_RES then
            OpcodeCd.TEXT_RES
        elif a = byte OpcodeCd.SCSI_DATA_IN then
            OpcodeCd.SCSI_DATA_IN
        elif a = byte OpcodeCd.LOGOUT_RES then
            OpcodeCd.LOGOUT_RES
        elif a = byte OpcodeCd.R2T then
            OpcodeCd.R2T
        elif a = byte OpcodeCd.ASYNC then
            OpcodeCd.ASYNC
        elif a = byte OpcodeCd.REJECT then
            OpcodeCd.REJECT
        else 
            errcont a

    let getOpcodeNameFromValue : ( OpcodeCd -> string ) =
        function
        | OpcodeCd.NOP_OUT -> "NOP_OUT"
        | OpcodeCd.SCSI_COMMAND -> "SCSI_COMMAND"
        | OpcodeCd.SCSI_TASK_MGR_REQ -> "SCSI_TASK_MGR_REQ"
        | OpcodeCd.LOGIN_REQ -> "LOGIN_REQ"
        | OpcodeCd.TEXT_REQ -> "TEXT_REQ"
        | OpcodeCd.SCSI_DATA_OUT -> "SCSI_DATA_OUT"
        | OpcodeCd.LOGOUT_REQ -> "LOGOUT_REQ"
        | OpcodeCd.SNACK -> "SNACK"
        | OpcodeCd.NOP_IN -> "NOP_IN"
        | OpcodeCd.SCSI_RES -> "SCSI_RES"
        | OpcodeCd.SCSI_TASK_MGR_RES -> "SCSI_TASK_MGR_RES"
        | OpcodeCd.LOGIN_RES -> "LOGIN_RES"
        | OpcodeCd.TEXT_RES -> "TEXT_RES"
        | OpcodeCd.SCSI_DATA_IN -> "SCSI_DATA_IN"
        | OpcodeCd.LOGOUT_RES -> "LOGOUT_RES"
        | OpcodeCd.R2T -> "R2T"
        | OpcodeCd.ASYNC -> "ASYNC"
        | OpcodeCd.REJECT -> "REJECT"
        | _ as x -> sprintf "Unknown Opcode value(0x%02X)" ( byte x )

    let byteToTaskATTRCdCd ( a : byte ) ( errcont : byte -> TaskATTRCd ) : TaskATTRCd =
        if a = byte TaskATTRCd.TAGLESS_TASK then
            TaskATTRCd.TAGLESS_TASK
        elif a = byte TaskATTRCd.SIMPLE_TASK then
            TaskATTRCd.SIMPLE_TASK
        elif a = byte TaskATTRCd.ORDERED_TASK then
            TaskATTRCd.ORDERED_TASK
        elif a = byte TaskATTRCd.HEAD_OF_QUEUE_TASK then
            TaskATTRCd.HEAD_OF_QUEUE_TASK
        elif a = byte TaskATTRCd.ACA_TASK then
            TaskATTRCd.ACA_TASK
        else 
            errcont a

    let getTaskATTRCdNameFromValue : ( TaskATTRCd -> string ) =
        function
        | TaskATTRCd.TAGLESS_TASK -> "TAGLESS_TASK"
        | TaskATTRCd.SIMPLE_TASK -> "SIMPLE_TASK"
        | TaskATTRCd.ORDERED_TASK -> "ORDERED_TASK"
        | TaskATTRCd.HEAD_OF_QUEUE_TASK -> "HEAD_OF_QUEUE_TASK"
        | TaskATTRCd.ACA_TASK -> "ACA_TASK"
        | _ as x -> sprintf "Unknown attribute value(0x%02X)" ( byte x )


    let byteToAHSTypeCd ( a : byte ) ( errcont : byte -> AHSTypeCd ) : AHSTypeCd =
        if a = byte AHSTypeCd.RESERVED then
            AHSTypeCd.RESERVED
        elif a = byte AHSTypeCd.EXTENDED_CDB then
            AHSTypeCd.EXTENDED_CDB
        elif a = byte AHSTypeCd.EXPECTED_LENGTH then
            AHSTypeCd.EXPECTED_LENGTH
        else 
            errcont a

    let getAHSTypeNameFromValue : ( AHSTypeCd -> string ) =
        function
        | AHSTypeCd.RESERVED -> "RESERVED"
        | AHSTypeCd.EXTENDED_CDB -> "EXTENDED_CDB"
        | AHSTypeCd.EXPECTED_LENGTH -> "EXPECTED_LENGTH"
        | _ as x -> sprintf "Unknown AHSType value(0x%02X)" ( byte x )


    let byteToiScsiSvcRespCd ( a : byte ) ( errcont : byte -> iScsiSvcRespCd ) : iScsiSvcRespCd =
        if a = byte iScsiSvcRespCd.COMMAND_COMPLETE then
            iScsiSvcRespCd.COMMAND_COMPLETE
        elif a = byte iScsiSvcRespCd.TARGET_FAILURE then
            iScsiSvcRespCd.TARGET_FAILURE
        else 
            errcont a

    let getiScsiSvcRespNameFromValue : ( iScsiSvcRespCd -> string ) =
        function
        | iScsiSvcRespCd.COMMAND_COMPLETE -> "COMMAND_COMPLETE"
        | iScsiSvcRespCd.TARGET_FAILURE -> "TARGET_FAILURE"
        | _ as x -> sprintf "Unknown iSCSI service response code(0x%02X)" ( byte x )

    let byteToTaskMgrReqCd ( a : byte ) ( errcont : byte -> TaskMgrReqCd ) : TaskMgrReqCd =
        if a = byte TaskMgrReqCd.ABORT_TASK then
            TaskMgrReqCd.ABORT_TASK
        elif a = byte TaskMgrReqCd.ABORT_TASK_SET then
            TaskMgrReqCd.ABORT_TASK_SET
        elif a = byte TaskMgrReqCd.CLEAR_ACA then
            TaskMgrReqCd.CLEAR_ACA
        elif a = byte TaskMgrReqCd.CLEAR_TASK_SET then
            TaskMgrReqCd.CLEAR_TASK_SET
        elif a = byte TaskMgrReqCd.LOGICAL_UNIT_RESET then
            TaskMgrReqCd.LOGICAL_UNIT_RESET
        elif a = byte TaskMgrReqCd.TARGET_WARM_RESET then
            TaskMgrReqCd.TARGET_WARM_RESET
        elif a = byte TaskMgrReqCd.TARGET_COLD_RESET then
            TaskMgrReqCd.TARGET_COLD_RESET
        elif a = byte TaskMgrReqCd.TASK_REASSIGN then
            TaskMgrReqCd.TASK_REASSIGN
        else
            errcont a

    let getTaskMgrReqNameFromValue : ( TaskMgrReqCd -> string ) =
        function
        | TaskMgrReqCd.ABORT_TASK -> "ABORT_TASK"
        | TaskMgrReqCd.ABORT_TASK_SET -> "ABORT_TASK_SET"
        | TaskMgrReqCd.CLEAR_ACA -> "CLEAR_ACA"
        | TaskMgrReqCd.CLEAR_TASK_SET -> "CLEAR_TASK_SET"
        | TaskMgrReqCd.LOGICAL_UNIT_RESET -> "LOGICAL_UNIT_RESET"
        | TaskMgrReqCd.TARGET_WARM_RESET -> "TARGET_WARM_RESET"
        | TaskMgrReqCd.TARGET_COLD_RESET -> "TARGET_COLD_RESET"
        | TaskMgrReqCd.TASK_REASSIGN -> "TASK_REASSIGN"
        | _ as x -> sprintf "Unknown Task Management Function Request PDU Function value(0x%02X)" ( byte x )


    let byteToTaskMgrResCd ( a : byte ) ( errcont : byte -> TaskMgrResCd ) : TaskMgrResCd =
        if a = byte TaskMgrResCd.FUCTION_COMPLETE then
            TaskMgrResCd.FUCTION_COMPLETE
        elif a = byte TaskMgrResCd.TASK_NOT_EXIST then
            TaskMgrResCd.TASK_NOT_EXIST
        elif a = byte TaskMgrResCd.LUN_NOT_EXIST then
            TaskMgrResCd.LUN_NOT_EXIST
        elif a = byte TaskMgrResCd.TASK_STILL_ALLEGIANT then
            TaskMgrResCd.TASK_STILL_ALLEGIANT
        elif a = byte TaskMgrResCd.TASK_REASSIGN_NOT_SUPPORT then
            TaskMgrResCd.TASK_REASSIGN_NOT_SUPPORT
        elif a = byte TaskMgrResCd.TASK_MGR_NOT_SUPPORT then
            TaskMgrResCd.TASK_MGR_NOT_SUPPORT
        elif a = byte TaskMgrResCd.AUTH_FAILED then
            TaskMgrResCd.AUTH_FAILED
        elif a = byte TaskMgrResCd.FUNCTION_REJECT then
            TaskMgrResCd.FUNCTION_REJECT
        else errcont a

    let getTaskMgrResNameFromValue : ( TaskMgrResCd -> string ) =
        function
        | TaskMgrResCd.FUCTION_COMPLETE -> "FUCTION_COMPLETE"
        | TaskMgrResCd.TASK_NOT_EXIST -> "TASK_NOT_EXIST"
        | TaskMgrResCd.LUN_NOT_EXIST -> "LUN_NOT_EXIST"
        | TaskMgrResCd.TASK_STILL_ALLEGIANT -> "TASK_STILL_ALLEGIANT"
        | TaskMgrResCd.TASK_REASSIGN_NOT_SUPPORT -> "TASK_REASSIGN_NOT_SUPPORT"
        | TaskMgrResCd.TASK_MGR_NOT_SUPPORT -> "TASK_MGR_NOT_SUPPORT"
        | TaskMgrResCd.AUTH_FAILED -> "AUTH_FAILED"
        | TaskMgrResCd.FUNCTION_REJECT -> "FUNCTION_REJECT"
        | _ as x -> sprintf "Unknown Task Management Function Response PDU Response value(0x%02X)" ( byte x )

    let byteToAsyncEventCd ( a : byte ) ( errcont : byte -> AsyncEventCd ) : AsyncEventCd =
        if a = byte AsyncEventCd.SENCE_DATA then
            AsyncEventCd.SENCE_DATA
        elif a = byte AsyncEventCd.LOGOUT_REQ then
            AsyncEventCd.LOGOUT_REQ
        elif a = byte AsyncEventCd.CONNECTION_CLOSE then
            AsyncEventCd.CONNECTION_CLOSE
        elif a = byte AsyncEventCd.SESSION_CLOSE then
            AsyncEventCd.SESSION_CLOSE
        elif a = byte AsyncEventCd.PARAM_NEGOTIATION_REQ then
            AsyncEventCd.PARAM_NEGOTIATION_REQ
        else
            errcont a

    let getAsyncEventNameFromValue : ( AsyncEventCd -> string ) =
        function
        | AsyncEventCd.SENCE_DATA -> "SENCE_DATA"
        | AsyncEventCd.LOGOUT_REQ -> "LOGOUT_REQ"
        | AsyncEventCd.CONNECTION_CLOSE -> "CONNECTION_CLOSE"
        | AsyncEventCd.SESSION_CLOSE -> "SESSION_CLOSE"
        | AsyncEventCd.PARAM_NEGOTIATION_REQ -> "PARAM_NEGOTIATION_REQ"
        | _ as x -> sprintf "Unknown Asyncronous message PDU AsyncEvent value(0x%02X)" ( byte x )

    let byteToLoginReqStateCd ( a : byte ) ( errcont : byte -> LoginReqStateCd ) : LoginReqStateCd =
        if a = byte LoginReqStateCd.SEQURITY then
            LoginReqStateCd.SEQURITY
        elif a = byte LoginReqStateCd.OPERATIONAL then
            LoginReqStateCd.OPERATIONAL
        elif a = byte LoginReqStateCd.FULL then
            LoginReqStateCd.FULL
        else
            errcont a

    let getLoginReqStateNameFromValue : ( LoginReqStateCd -> string ) =
        function
        | LoginReqStateCd.SEQURITY -> "SEQURITY"
        | LoginReqStateCd.OPERATIONAL -> "OPERATIONAL"
        | LoginReqStateCd.FULL -> "FULL"
        | _ as x -> sprintf "Unknown Login request PDU CSG, NSG value(0x%02X)" ( byte x )

    let shortToLoginResStatCd ( a : uint16 ) ( errcont : uint16 -> LoginResStatCd ) : LoginResStatCd =
        if a = uint16 LoginResStatCd.SUCCESS then
            LoginResStatCd.SUCCESS
        elif a = uint16 LoginResStatCd.REDIRECT_TMP then
            LoginResStatCd.REDIRECT_TMP
        elif a = uint16 LoginResStatCd.REDIRECT_PERM then
            LoginResStatCd.REDIRECT_PERM
        elif a = uint16 LoginResStatCd.INITIATOR_ERR then
            LoginResStatCd.INITIATOR_ERR
        elif a = uint16 LoginResStatCd.AUTH_FAILURE then
            LoginResStatCd.AUTH_FAILURE
        elif a = uint16 LoginResStatCd.NOT_ALLOWED then
            LoginResStatCd.NOT_ALLOWED
        elif a = uint16 LoginResStatCd.NOT_FOUND then
            LoginResStatCd.NOT_FOUND
        elif a = uint16 LoginResStatCd.TARGET_REMOVED then
            LoginResStatCd.TARGET_REMOVED
        elif a = uint16 LoginResStatCd.UNSUPPORTED_VERSION then
            LoginResStatCd.UNSUPPORTED_VERSION
        elif a = uint16 LoginResStatCd.TOO_MANY_CONS then
            LoginResStatCd.TOO_MANY_CONS
        elif a = uint16 LoginResStatCd.MISSING_PARAMS then
            LoginResStatCd.MISSING_PARAMS
        elif a = uint16 LoginResStatCd.UNSUPPORT_MCS then
            LoginResStatCd.UNSUPPORT_MCS
        elif a = uint16 LoginResStatCd.UNSUPPORT_SESS_TYPE then
            LoginResStatCd.UNSUPPORT_SESS_TYPE
        elif a = uint16 LoginResStatCd.SESS_NOT_EXIST then
            LoginResStatCd.SESS_NOT_EXIST
        elif a = uint16 LoginResStatCd.INVALID_LOGIN then
            LoginResStatCd.INVALID_LOGIN
        elif a = uint16 LoginResStatCd.TARGET_ERROR then
            LoginResStatCd.TARGET_ERROR
        elif a = uint16 LoginResStatCd.SERVICE_UNAVAILABLE then
            LoginResStatCd.SERVICE_UNAVAILABLE
        elif a = uint16 LoginResStatCd.OUT_OF_RESOURCE then
            LoginResStatCd.OUT_OF_RESOURCE
        else
            errcont a

    let getLoginResStatNameFromValue : ( LoginResStatCd -> string ) =
        function
        | LoginResStatCd.SUCCESS -> "SUCCESS"
        | LoginResStatCd.REDIRECT_TMP -> "REDIRECT_TMP"
        | LoginResStatCd.REDIRECT_PERM -> "REDIRECT_PERM"
        | LoginResStatCd.INITIATOR_ERR -> "INITIATOR_ERR"
        | LoginResStatCd.AUTH_FAILURE -> "AUTH_FAILURE"
        | LoginResStatCd.NOT_ALLOWED -> "NOT_ALLOWED"
        | LoginResStatCd.NOT_FOUND -> "NOT_FOUND"
        | LoginResStatCd.TARGET_REMOVED -> "TARGET_REMOVED"
        | LoginResStatCd.UNSUPPORTED_VERSION -> "UNSUPPORTED_VERSION"
        | LoginResStatCd.TOO_MANY_CONS -> "TOO_MANY_CONS"
        | LoginResStatCd.MISSING_PARAMS -> "MISSING_PARAMS"
        | LoginResStatCd.UNSUPPORT_MCS -> "UNSUPPORT_MCS"
        | LoginResStatCd.UNSUPPORT_SESS_TYPE -> "UNSUPPORT_SESS_TYPE"
        | LoginResStatCd.SESS_NOT_EXIST -> "SESS_NOT_EXIST"
        | LoginResStatCd.INVALID_LOGIN -> "INVALID_LOGIN"
        | LoginResStatCd.TARGET_ERROR -> "TARGET_ERROR"
        | LoginResStatCd.SERVICE_UNAVAILABLE -> "SERVICE_UNAVAILABLE"
        | LoginResStatCd.OUT_OF_RESOURCE -> "OUT_OF_RESOURCE"
        | _ as x -> sprintf "Unknown Login response PDU StatusClass, StatusDetail value(0x%04X)" ( uint16 x )

    let byteToLogoutReqReasonCd ( a : byte ) ( errcont : byte -> LogoutReqReasonCd ) : LogoutReqReasonCd =
        if a = byte LogoutReqReasonCd.CLOSE_SESS then
            LogoutReqReasonCd.CLOSE_SESS
        elif a = byte LogoutReqReasonCd.CLOSE_CONN then
            LogoutReqReasonCd.CLOSE_CONN
        elif a = byte LogoutReqReasonCd.RECOVERY then
            LogoutReqReasonCd.RECOVERY
        else
            errcont a

    let getLogoutReqResonNameFromValue : ( LogoutReqReasonCd -> string ) =
        function
        | LogoutReqReasonCd.CLOSE_SESS -> "CLOSE_SESS"
        | LogoutReqReasonCd.CLOSE_CONN -> "CLOSE_CONN"
        | LogoutReqReasonCd.RECOVERY -> "RECOVERY"
        | _ as x -> sprintf "Unknown Logout request PDU Reason value(0x%02X)" ( byte x )

    let byteToLogoutResCd ( a : byte ) ( errcont : byte -> LogoutResCd ) : LogoutResCd =
        if a = byte LogoutResCd.SUCCESS then
            LogoutResCd.SUCCESS
        elif a = byte LogoutResCd.CID_NOT_FOUND then
            LogoutResCd.CID_NOT_FOUND
        elif a = byte LogoutResCd.RECOVERY_NOT_SUPPORT then
            LogoutResCd.RECOVERY_NOT_SUPPORT
        elif a = byte LogoutResCd.CLEANUP_FAILED then
            LogoutResCd.CLEANUP_FAILED
        else
            errcont a

    let getLogoutResNameFromValue : ( LogoutResCd -> string ) =
        function
        | LogoutResCd.SUCCESS -> "SUCCESS"
        | LogoutResCd.CID_NOT_FOUND -> "CID_NOT_FOUND"
        | LogoutResCd.RECOVERY_NOT_SUPPORT -> "RECOVERY_NOT_SUPPORT"
        | LogoutResCd.CLEANUP_FAILED -> "CLEANUP_FAILED"
        | _ as x -> sprintf "Unknown Logout response PDU Response value(0x%02X)" ( byte x )

    let byteToSnackReqTypeCd ( a : byte ) ( errcont : byte -> SnackReqTypeCd ) : SnackReqTypeCd =
        if a = byte SnackReqTypeCd.DATA_R2T then
            SnackReqTypeCd.DATA_R2T
        elif a = byte SnackReqTypeCd.STATUS then
            SnackReqTypeCd.STATUS
        elif a = byte SnackReqTypeCd.DATA_ACK then
            SnackReqTypeCd.DATA_ACK
        elif a = byte SnackReqTypeCd.RDATA_SNACK then
            SnackReqTypeCd.RDATA_SNACK
        else
            errcont a
   
    let getSnackReqTypeNameFromValue : ( SnackReqTypeCd -> string ) =
        function
        | SnackReqTypeCd.DATA_R2T -> "DATA_R2T"
        | SnackReqTypeCd.STATUS -> "STATUS"
        | SnackReqTypeCd.DATA_ACK -> "DATA_ACK"
        | SnackReqTypeCd.RDATA_SNACK -> "RDATA_SNACK"
        | _ as x -> sprintf "Unknown SNACK request PDU Type value(0x%02X)" ( byte x )

    let byteToRejectResonCd ( a : byte ) ( errcont : byte -> RejectResonCd ) : RejectResonCd =
        if a = byte RejectResonCd.DATA_DIGEST_ERR then
            RejectResonCd.DATA_DIGEST_ERR
        elif a = byte RejectResonCd.SNACK_REJECT then
            RejectResonCd.SNACK_REJECT
        elif a = byte RejectResonCd.PROTOCOL_ERR then
            RejectResonCd.PROTOCOL_ERR
        elif a = byte RejectResonCd.COM_NOT_SUPPORT then
            RejectResonCd.COM_NOT_SUPPORT
        elif a = byte RejectResonCd.IMMIDIATE_COM_REJECT then
            RejectResonCd.IMMIDIATE_COM_REJECT
        elif a = byte RejectResonCd.TASK_IN_PROGRESS then
            RejectResonCd.TASK_IN_PROGRESS
        elif a = byte RejectResonCd.INVALID_DATA_ACK then
            RejectResonCd.INVALID_DATA_ACK
        elif a = byte RejectResonCd.INVALID_PDU_FIELD then
            RejectResonCd.INVALID_PDU_FIELD
        elif a = byte RejectResonCd.LONG_OPE_REJECT then
            RejectResonCd.LONG_OPE_REJECT
        elif a = byte RejectResonCd.NEGOTIATION_RESET then
            RejectResonCd.NEGOTIATION_RESET
        elif a = byte RejectResonCd.WAIT_FOR_LOGOUT then
            RejectResonCd.WAIT_FOR_LOGOUT
        else
            errcont a

    let getRejectResonNameFomValue : ( RejectResonCd -> string ) =
        function
        | RejectResonCd.DATA_DIGEST_ERR -> "DATA_DIGEST_ERR"
        | RejectResonCd.SNACK_REJECT -> "SNACK_REJECT"
        | RejectResonCd.PROTOCOL_ERR -> "PROTOCOL_ERR"
        | RejectResonCd.COM_NOT_SUPPORT -> "COM_NOT_SUPPORT"
        | RejectResonCd.IMMIDIATE_COM_REJECT -> "IMMIDIATE_COM_REJECT"
        | RejectResonCd.TASK_IN_PROGRESS -> "TASK_IN_PROGRESS"
        | RejectResonCd.INVALID_DATA_ACK -> "INVALID_DATA_ACK"
        | RejectResonCd.INVALID_PDU_FIELD -> "INVALID_PDU_FIELD"
        | RejectResonCd.LONG_OPE_REJECT -> "LONG_OPE_REJECT"
        | RejectResonCd.NEGOTIATION_RESET -> "NEGOTIATION_RESET"
        | RejectResonCd.WAIT_FOR_LOGOUT -> "WAIT_FOR_LOGOUT"
        | _ as x -> sprintf "Unknown Reject PDU Reason value(0x%02X)" ( byte x )

    let byteToScsiCmdStatCd ( a : byte ) ( errcont : byte -> ScsiCmdStatCd ) : ScsiCmdStatCd =
        if a = byte ScsiCmdStatCd.GOOD then
            ScsiCmdStatCd.GOOD
        elif a = byte ScsiCmdStatCd.CHECK_CONDITION then
            ScsiCmdStatCd.CHECK_CONDITION
        elif a = byte ScsiCmdStatCd.CONDITION_MET then
            ScsiCmdStatCd.CONDITION_MET
        elif a = byte ScsiCmdStatCd.BUSY then
            ScsiCmdStatCd.BUSY
        elif a = byte ScsiCmdStatCd.INTERMEDIATE then
            ScsiCmdStatCd.INTERMEDIATE
        elif a = byte ScsiCmdStatCd.INTERMEDIATE_CONDITION_MET then
            ScsiCmdStatCd.INTERMEDIATE_CONDITION_MET
        elif a = byte ScsiCmdStatCd.RESERVATION_CONFLICT then
            ScsiCmdStatCd.RESERVATION_CONFLICT
        elif a = byte ScsiCmdStatCd.TASK_SET_FULL then
            ScsiCmdStatCd.TASK_SET_FULL
        elif a = byte ScsiCmdStatCd.ACA_ACTIVE then
            ScsiCmdStatCd.ACA_ACTIVE
        elif a = byte ScsiCmdStatCd.TASK_ABORTED then
            ScsiCmdStatCd.TASK_ABORTED
        else
            errcont a

    /// <summary>
    /// SCSI status code to string value.
    /// </summary>
    let getScsiCmdStatNameFromValue : ( ScsiCmdStatCd -> string ) =
        function
        | ScsiCmdStatCd.GOOD -> "GOOD"
        | ScsiCmdStatCd.CHECK_CONDITION -> "CHECK_CONDITION"
        | ScsiCmdStatCd.CONDITION_MET -> "CONDITION_MET"
        | ScsiCmdStatCd.BUSY -> "BUSY"
        | ScsiCmdStatCd.INTERMEDIATE -> "INTERMEDIATE"
        | ScsiCmdStatCd.INTERMEDIATE_CONDITION_MET -> "INTERMEDIATE_CONDITION_MET"
        | ScsiCmdStatCd.RESERVATION_CONFLICT -> "RESERVATION_CONFLICT"
        | ScsiCmdStatCd.TASK_SET_FULL -> "TASK_SET_FULL"
        | ScsiCmdStatCd.ACA_ACTIVE -> "ACA_ACTIVE"
        | ScsiCmdStatCd.TASK_ABORTED -> "TASK_ABORTED"
        | _ as x -> sprintf "Unknown SCSI Command status code value(0x%02X)" ( byte x )

    /// <summary>
    /// SenseKey code to string value
    /// </summary>
    let getSenseKeyNameFromValue : ( SenseKeyCd -> string ) =
        function
        | SenseKeyCd.NO_SENSE -> "NO SENSE"
        | SenseKeyCd.RECOVERED_ERROR -> "RECOVERED ERROR"
        | SenseKeyCd.NOT_READY -> "NOT READY"
        | SenseKeyCd.MEDIUM_ERROR -> "MEDIUM ERROR"
        | SenseKeyCd.HARDWARE_ERROR -> "HARDWARE ERROR"
        | SenseKeyCd.ILLEGAL_REQUEST -> "ILLEGAL REQUEST"
        | SenseKeyCd.UNIT_ATTENTION -> "UNIT ATTENTION"
        | SenseKeyCd.DATA_PROTECT -> "DATA PROTECT"
        | SenseKeyCd.BLANK_CHECK -> "BLANK CHECK"
        | SenseKeyCd.VENDOR_SPECIFIC -> "VENDOR SPECIFIC"
        | SenseKeyCd.COPY_ABORTED -> "COPY ABORTED"
        | SenseKeyCd.ABORTED_COMMAND -> "ABORTED COMMAND"
        | SenseKeyCd.VOLUME_OVERFLOW -> "VOLUME OVERFLOW"
        | SenseKeyCd.MISCOMPARE -> "MISCOMPARE"
        | _ as x -> sprintf "Unknown sensekey value (SenseKey=0x%02x)" ( byte x )


    //=============================================================================
    // Additional sense code and additional sense code qualifier string values

    /// <summary>
    /// Additional sense code and additional sense code qualifier to string value
    /// </summary>
    let getAscAndAscqNameFromValue : ( ASCCd -> string ) =
        function
        | ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT -> "ACCESS DENIED - ACL LUN CONFLICT"
        | ASCCd.ACCESS_DENIED_ENROLLMENT_CONFLICT -> "ACCESS DENIED - ENROLLMENT CONFLICT"
        | ASCCd.ACCESS_DENIED_INITIATOR_PENDING_ENROLLED -> "ACCESS DENIED - INITIATOR PENDING-ENROLLED"
        | ASCCd.ACCESS_DENIED_INVALID_LU_IDENTIFIER -> "ACCESS DENIED - INVALID LU IDENTIFIER"
        | ASCCd.ACCESS_DENIED_INVALID_MGMT_ID_KEY -> "ACCESS DENIED - INVALID MGMT ID KEY"
        | ASCCd.ACCESS_DENIED_INVALID_PROXY_TOKEN -> "ACCESS DENIED - INVALID PROXY TOKEN"
        | ASCCd.ACCESS_DENIED_NO_ACCESS_RIGHTS -> "ACCESS DENIED - NO ACCESS RIGHTS"
        | ASCCd.ACK_NAK_TIMEOUT -> "ACK/NAK TIMEOUT"
        | ASCCd.ADD_LOGICAL_UNIT_FAILED -> "ADD LOGICAL UNIT FAILED"
        | ASCCd.ADDRESS_MARK_NOT_FOUND_FOR_DATA_FIELD -> "ADDRESS MARK NOT FOUND FOR DATA FIELD"
        | ASCCd.ADDRESS_MARK_NOT_FOUND_FOR_ID_FIELD -> "ADDRESS MARK NOT FOUND FOR ID FIELD"
        | ASCCd.ASSIGN_FAILURE_OCCURRED -> "ASSIGN FAILURE OCCURRED"
        | ASCCd.ASSOCIATED_WRITE_PROTECT -> "ASSOCIATED WRITE PROTECT"
        | ASCCd.ASYMMETRIC_ACCESS_STATE_CHANGED -> "ASYMMETRIC ACCESS STATE CHANGED"
        | ASCCd.ASYNCHRONOUS_INFORMATION_PROTECTION_ERROR_DETECTED -> "ASYNCHRONOUS INFORMATION PROTECTION ERROR DETECTED"
        | ASCCd.ATTACHMENT_OF_LOGICAL_UNIT_FAILED -> "ATTACHMENT OF LOGICAL UNIT FAILED"
        | ASCCd.AUDIO_PLAY_OPERATION_IN_PROGRESS -> "AUDIO PLAY OPERATION IN PROGRESS"
        | ASCCd.AUDIO_PLAY_OPERATION_PAUSED -> "AUDIO PLAY OPERATION PAUSED"
        | ASCCd.AUDIO_PLAY_OPERATION_STOPPED_DUE_TO_ERROR -> "AUDIO PLAY OPERATION STOPPED DUE TO ERROR"
        | ASCCd.AUDIO_PLAY_OPERATION_SUCCESSFULLY_COMPLETED -> "AUDIO PLAY OPERATION SUCCESSFULLY COMPLETED"
        | ASCCd.AUTOMATIC_DOCUMENT_FEEDER_COVER_UP -> "AUTOMATIC DOCUMENT FEEDER COVER UP"
        | ASCCd.AUTOMATIC_DOCUMENT_FEEDER_LIFT_UP -> "AUTOMATIC DOCUMENT FEEDER LIFT UP"
        | ASCCd.AUXILIARY_MEMORY_OUT_OF_SPACE -> "AUXILIARY MEMORY OUT OF SPACE"
        | ASCCd.AUXILIARY_MEMORY_READ_ERROR -> "AUXILIARY MEMORY READ ERROR"
        | ASCCd.AUXILIARY_MEMORY_WRITE_ERROR -> "AUXILIARY MEMORY WRITE ERROR"
        | ASCCd.BEGINNING_OF_PARTITION_MEDIUM_DETECTED -> "BEGINNING-OF-PARTITION/MEDIUM DETECTED"
        | ASCCd.BLOCK_NOT_COMPRESSIBLE -> "BLOCK NOT COMPRESSIBLE"
        | ASCCd.BLOCK_SEQUENCE_ERROR -> "BLOCK SEQUENCE ERROR"
        | ASCCd.BUS_DEVICE_RESET_FUNCTION_OCCURRED -> "BUS DEVICE RESET FUNCTION OCCURRED"
        | ASCCd.CANNOT_DECOMPRESS_USING_DECLARED_ALGORITHM -> "CANNOT DECOMPRESS USING DECLARED ALGORITHM"
        | ASCCd.CANNOT_FORMAT_MEDIUM_INCOMPATIBLE_MEDIUM -> "CANNOT FORMAT MEDIUM - INCOMPATIBLE MEDIUM"
        | ASCCd.CANNOT_READ_MEDIUM_INCOMPATIBLE_FORMAT -> "CANNOT READ MEDIUM - INCOMPATIBLE FORMAT"
        | ASCCd.CANNOT_READ_MEDIUM_UNKNOWN_FORMAT -> "CANNOT READ MEDIUM - UNKNOWN FORMAT"
        | ASCCd.CANNOT_WRITE_APPLICATION_CODE_MISMATCH -> "CANNOT WRITE - APPLICATION CODE MISMATCH"
        | ASCCd.CANNOT_WRITE_MEDIUM_INCOMPATIBLE_FORMAT -> "CANNOT WRITE MEDIUM - INCOMPATIBLE FORMAT"
        | ASCCd.CANNOT_WRITE_MEDIUM_UNKNOWN_FORMAT -> "CANNOT WRITE MEDIUM - UNKNOWN FORMAT"
        | ASCCd.CAPACITY_DATA_HAS_CHANGED -> "CAPACITY DATA HAS CHANGED"
        | ASCCd.CARTRIDGE_FAULT -> "CARTRIDGE FAULT"
        | ASCCd.CD_CONTROL_ERROR -> "CD CONTROL ERROR"
        | ASCCd.CDB_DECRYPTION_ERROR -> "CDB DECRYPTION ERROR"
        | ASCCd.CHANGED_OPERATING_DEFINITION -> "CHANGED OPERATING DEFINITION"
        | ASCCd.CIRC_UNRECOVERED_ERROR -> "CIRC UNRECOVERED ERROR"
        | ASCCd.CLEANING_CARTRIDGE_INSTALLED -> "CLEANING CARTRIDGE INSTALLED"
        | ASCCd.CLEANING_FAILURE -> "CLEANING FAILURE"
        | ASCCd.CLEANING_REQUEST_REJECTED -> "CLEANING REQUEST REJECTED"
        | ASCCd.CLEANING_REQUESTED -> "CLEANING REQUESTED"
        | ASCCd.COMMAND_PHASE_ERROR -> "COMMAND PHASE ERROR"
        | ASCCd.COMMAND_SEQUENCE_ERROR -> "COMMAND SEQUENCE ERROR"
        | ASCCd.COMMAND_TO_LOGICAL_UNIT_FAILED -> "COMMAND TO LOGICAL UNIT FAILED"
        | ASCCd.COMMANDS_CLEARED_BY_ANOTHER_INITIATOR -> "COMMANDS CLEARED BY ANOTHER INITIATOR"
        | ASCCd.COMPONENT_DEVICE_ATTACHED -> "COMPONENT DEVICE ATTACHED"
        | ASCCd.COMPRESSION_CHECK_MISCOMPARE_ERROR -> "COMPRESSION CHECK MISCOMPARE ERROR"
        | ASCCd.CONDITIONAL_WRITE_PROTECT -> "CONDITIONAL WRITE PROTECT"
        | ASCCd.CONFIGURATION_FAILURE -> "CONFIGURATION FAILURE"
        | ASCCd.CONFIGURATION_OF_INCAPABLE_LOGICAL_UNITS_FAILED -> "CONFIGURATION OF INCAPABLE LOGICAL UNITS FAILED"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_ACCESS_TIMES_TOO_HIGH -> "CONTROLLER IMPENDING FAILURE ACCESS TIMES TOO HIGH"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_CHANNEL_PARAMETRICS -> "CONTROLLER IMPENDING FAILURE CHANNEL PARAMETRICS"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_CONTROLLER_DETECTED -> "CONTROLLER IMPENDING FAILURE CONTROLLER DETECTED"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_DATA_ERROR_RATE_TOO_HIGH -> "CONTROLLER IMPENDING FAILURE DATA ERROR RATE TOO HIGH"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_DRIVE_CALIBRATION_RETRY_COUNT -> "CONTROLLER IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_DRIVE_ERROR_RATE_TOO_HIGH -> "CONTROLLER IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_GENERAL_HARD_DRIVE_FAILURE -> "CONTROLLER IMPENDING FAILURE GENERAL HARD DRIVE FAILURE"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_SEEK_ERROR_RATE_TOO_HIGH -> "CONTROLLER IMPENDING FAILURE SEEK ERROR RATE TOO HIGH"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_SEEK_TIME_PERFORMANCE -> "CONTROLLER IMPENDING FAILURE SEEK TIME PERFORMANCE"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_SPIN_UP_RETRY_COUNT -> "CONTROLLER IMPENDING FAILURE SPIN-UP RETRY COUNT"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_START_UNIT_TIMES_TOO_HIGH -> "CONTROLLER IMPENDING FAILURE START UNIT TIMES TOO HIGH"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_THROUGHPUT_PERFORMANCE -> "CONTROLLER IMPENDING FAILURE THROUGHPUT PERFORMANCE"
        | ASCCd.CONTROLLER_IMPENDING_FAILURE_TOO_MANY_BLOCK_REASSIGNS -> "CONTROLLER IMPENDING FAILURE TOO MANY BLOCK REASSIGNS"
        | ASCCd.COPY_CANNOT_EXECUTE_SINCE_HOST_CANNOT_DISCONNECT -> "COPY CANNOT EXECUTE SINCE HOST CANNOT DISCONNECT"
        | ASCCd.COPY_PROTECTION_KEY_EXCHANGE_FAILURE_AUTHENTICATION_FAILURE -> "COPY PROTECTION KEY EXCHANGE FAILURE - AUTHENTICATION FAILURE"
        | ASCCd.COPY_PROTECTION_KEY_EXCHANGE_FAILURE_KEY_NOT_ESTABLISHED -> "COPY PROTECTION KEY EXCHANGE FAILURE - KEY NOT ESTABLISHED"
        | ASCCd.COPY_PROTECTION_KEY_EXCHANGE_FAILURE_KEY_NOT_PRESENT -> "COPY PROTECTION KEY EXCHANGE FAILURE - KEY NOT PRESENT"
        | ASCCd.COPY_SEGMENT_GRANULARITY_VIOLATION -> "COPY SEGMENT GRANULARITY VIOLATION"
        | ASCCd.COPY_TARGET_DEVICE_DATA_OVERRUN -> "COPY TARGET DEVICE DATA OVERRUN"
        | ASCCd.COPY_TARGET_DEVICE_DATA_UNDERRUN -> "COPY TARGET DEVICE DATA UNDERRUN"
        | ASCCd.COPY_TARGET_DEVICE_NOT_REACHABLE -> "COPY TARGET DEVICE NOT REACHABLE"
        | ASCCd.CREATION_OF_LOGICAL_UNIT_FAILED -> "CREATION OF LOGICAL UNIT FAILED"
        | ASCCd.CURRENT_PROGRAM_AREA_IS_EMPTY -> "CURRENT PROGRAM AREA IS EMPTY"
        | ASCCd.CURRENT_PROGRAM_AREA_IS_NOT_EMPTY -> "CURRENT PROGRAM AREA IS NOT EMPTY"
        | ASCCd.CURRENT_SESSION_NOT_FIXATED_FOR_APPEND -> "CURRENT SESSION NOT FIXATED FOR APPEND"
        | ASCCd.DATA_BLOCK_APPLICATION_TAG_CHECK_FAILED -> "DATA BLOCK APPLICATION TAG CHECK FAILED"
        | ASCCd.DATA_BLOCK_GUARD_CHECK_FAILED -> "DATA BLOCK GUARD CHECK FAILED"
        | ASCCd.DATA_BLOCK_REFERENCE_TAG_CHECK_FAILED -> "DATA BLOCK REFERENCE TAG CHECK FAILED"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_ACCESS_TIMES_TOO_HIGH -> "DATA CHANNEL IMPENDING FAILURE ACCESS TIMES TOO HIGH"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_CHANNEL_PARAMETRICS -> "DATA CHANNEL IMPENDING FAILURE CHANNEL PARAMETRICS"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_CONTROLLER_DETECTED -> "DATA CHANNEL IMPENDING FAILURE CONTROLLER DETECTED"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_DATA_ERROR_RATE_TOO_HIGH -> "DATA CHANNEL IMPENDING FAILURE DATA ERROR RATE TOO HIGH"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_DRIVE_CALIBRATION_RETRY_COUNT -> "DATA CHANNEL IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_DRIVE_ERROR_RATE_TOO_HIGH -> "DATA CHANNEL IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_GENERAL_HARD_DRIVE_FAILURE -> "DATA CHANNEL IMPENDING FAILURE GENERAL HARD DRIVE FAILURE"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_SEEK_ERROR_RATE_TOO_HIGH -> "DATA CHANNEL IMPENDING FAILURE SEEK ERROR RATE TOO HIGH"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_SEEK_TIME_PERFORMANCE -> "DATA CHANNEL IMPENDING FAILURE SEEK TIME PERFORMANCE"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_SPIN_UP_RETRY_COUNT -> "DATA CHANNEL IMPENDING FAILURE SPIN-UP RETRY COUNT"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_START_UNIT_TIMES_TOO_HIGH -> "DATA CHANNEL IMPENDING FAILURE START UNIT TIMES TOO HIGH"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_THROUGHPUT_PERFORMANCE -> "DATA CHANNEL IMPENDING FAILURE THROUGHPUT PERFORMANCE"
        | ASCCd.DATA_CHANNEL_IMPENDING_FAILURE_TOO_MANY_BLOCK_REASSIGNS -> "DATA CHANNEL IMPENDING FAILURE TOO MANY BLOCK REASSIGNS"
        | ASCCd.DATA_DECRYPTION_ERROR -> "DATA DECRYPTION ERROR"
        | ASCCd.DATA_EXPANSION_OCCURRED_DURING_COMPRESSION -> "DATA EXPANSION OCCURRED DURING COMPRESSION"
        | ASCCd.DATA_LOSS_ON_LOGICAL_UNIT -> "DATA LOSS ON LOGICAL UNIT"
        | ASCCd.DATA_OFFSET_ERROR -> "DATA OFFSET ERROR"
        | ASCCd.DATA_PHASE_CRC_ERROR_DETECTED -> "DATA PHASE CRC ERROR DETECTED"
        | ASCCd.DATA_PHASE_ERROR -> "DATA PHASE ERROR"
        | ASCCd.DATA_RE_SYNCHRONIZATION_ERROR -> "DATA RE-SYNCHRONIZATION ERROR"
        | ASCCd.DATA_SYNC_ERROR_DATA_AUTO_REALLOCATED -> "DATA SYNC ERROR - DATA AUTO-REALLOCATED"
        | ASCCd.DATA_SYNC_ERROR_DATA_REWRITTEN -> "DATA SYNC ERROR - DATA REWRITTEN"
        | ASCCd.DATA_SYNC_ERROR_RECOMMEND_REASSIGNMENT -> "DATA SYNC ERROR - RECOMMEND REASSIGNMENT"
        | ASCCd.DATA_SYNC_ERROR_RECOMMEND_REWRITE -> "DATA SYNC ERROR - RECOMMEND REWRITE"
        | ASCCd.DATA_SYNCHRONIZATION_MARK_ERROR -> "DATA SYNCHRONIZATION MARK ERROR"
        | ASCCd.DE_COMPRESSION_CRC_ERROR -> "DE-COMPRESSION CRC ERROR"
        | ASCCd.DECOMPRESSION_EXCEPTION_LONG_ALGORITHM_ID -> "DECOMPRESSION EXCEPTION LONG ALGORITHM ID"
        | ASCCd.DEFECT_LIST_ERROR -> "DEFECT LIST ERROR"
        | ASCCd.DEFECT_LIST_ERROR_IN_GROWN_LIST -> "DEFECT LIST ERROR IN GROWN LIST"
        | ASCCd.DEFECT_LIST_ERROR_IN_PRIMARY_LIST -> "DEFECT LIST ERROR IN PRIMARY LIST"
        | ASCCd.DEFECT_LIST_NOT_AVAILABLE -> "DEFECT LIST NOT AVAILABLE"
        | ASCCd.DEFECT_LIST_NOT_FOUND -> "DEFECT LIST NOT FOUND"
        | ASCCd.DEFECT_LIST_UPDATE_FAILURE -> "DEFECT LIST UPDATE FAILURE"
        | ASCCd.DEVICE_IDENTIFIER_CHANGED -> "DEVICE IDENTIFIER CHANGED"
        | ASCCd.DEVICE_INTERNAL_RESET -> "DEVICE INTERNAL RESET"
        | ASCCd.DOCUMENT_JAM_IN_AUTOMATIC_DOCUMENT_FEEDER -> "DOCUMENT JAM IN AUTOMATIC DOCUMENT FEEDER"
        | ASCCd.DOCUMENT_MISS_FEED_AUTOMATIC_IN_DOCUMENT_FEEDER -> "DOCUMENT MISS FEED AUTOMATIC IN DOCUMENT FEEDER"
        | ASCCd.DRIVE_REGION_MUST_BE_PERMANENT_REGION_RESET_COUNT_ERROR -> "DRIVE REGION MUST BE PERMANENT/REGION RESET COUNT ERROR"
        | ASCCd.ECHO_BUFFER_OVERWRITTEN -> "ECHO BUFFER OVERWRITTEN"
        | ASCCd.EMPTY_OR_PARTIALLY_WRITTEN_RESERVED_TRACK -> "EMPTY OR PARTIALLY WRITTEN RESERVED TRACK"
        | ASCCd.ENCLOSURE_FAILURE -> "ENCLOSURE FAILURE"
        | ASCCd.ENCLOSURE_SERVICES_CHECKSUM_ERROR -> "ENCLOSURE SERVICES CHECKSUM ERROR"
        | ASCCd.ENCLOSURE_SERVICES_FAILURE -> "ENCLOSURE SERVICES FAILURE"
        | ASCCd.ENCLOSURE_SERVICES_TRANSFER_FAILURE -> "ENCLOSURE SERVICES TRANSFER FAILURE"
        | ASCCd.ENCLOSURE_SERVICES_TRANSFER_REFUSED -> "ENCLOSURE SERVICES TRANSFER REFUSED"
        | ASCCd.ENCLOSURE_SERVICES_UNAVAILABLE -> "ENCLOSURE SERVICES UNAVAILABLE"
        | ASCCd.END_OF_MEDIUM_REACHED -> "END OF MEDIUM REACHED"
        | ASCCd.END_OF_USER_AREA_ENCOUNTERED_ON_THIS_TRACK -> "END OF USER AREA ENCOUNTERED ON THIS TRACK"
        | ASCCd.END_OF_DATA_DETECTED -> "END-OF-DATA DETECTED"
        | ASCCd.END_OF_DATA_NOT_FOUND -> "END-OF-DATA NOT FOUND"
        | ASCCd.END_OF_PARTITION_MEDIUM_DETECTED -> "END-OF-PARTITION/MEDIUM DETECTED"
        | ASCCd.ERASE_FAILURE -> "ERASE FAILURE"
        | ASCCd.ERASE_FAILURE_INCOMPLETE_ERASE_OPERATION_DETECTED -> "ERASE FAILURE - INCOMPLETE ERASE OPERATION DETECTED"
        | ASCCd.ERASE_OPERATION_IN_PROGRESS -> "ERASE OPERATION IN PROGRESS"
        | ASCCd.ERROR_DETECTED_BY_THIRD_PARTY_TEMPORARY_INITIATOR -> "ERROR DETECTED BY THIRD PARTY TEMPORARY INITIATOR"
        | ASCCd.ERROR_LOG_OVERFLOW -> "ERROR LOG OVERFLOW"
        | ASCCd.ERROR_READING_ISRC_NUMBER -> "ERROR READING ISRC NUMBER"
        | ASCCd.ERROR_READING_UPC_EAN_NUMBER -> "ERROR READING UPC/EAN NUMBER"
        | ASCCd.ERROR_TOO_LONG_TO_CORRECT -> "ERROR TOO LONG TO CORRECT"
        | ASCCd.ESN_DEVICE_BUSY_CLASS_EVENT -> "ESN - DEVICE BUSY CLASS EVENT"
        | ASCCd.ESN_MEDIA_CLASS_EVENT -> "ESN - MEDIA CLASS EVENT"
        | ASCCd.ESN_POWER_MANAGEMENT_CLASS_EVENT -> "ESN - POWER MANAGEMENT CLASS EVENT"
        | ASCCd.EVENT_STATUS_NOTIFICATION -> "EVENT STATUS NOTIFICATION"
        | ASCCd.EXCESSIVE_WRITE_ERRORS -> "EXCESSIVE WRITE ERRORS"
        | ASCCd.EXCHANGE_OF_LOGICAL_UNIT_FAILD -> "EXCHANGE OF LOGICAL UNIT FAILD"
        | ASCCd.FAILED_TO_SENSE_BOTTOM_OF_FORM -> "FAILED TO SENSE BOTTOM-OF-FORM"
        | ASCCd.FAILED_TO_SENSE_TOP_OF_FORM -> "FAILED TO SENSE TOP-OF-FORM"
        | ASCCd.FAILURE_PREDICTION_THRESHOLD_EXCEEDED -> "FAILURE PREDICTION THRESHOLD EXCEEDED"
        | ASCCd.FAILURE_PREDICTION_THRESHOLD_EXCEEDED_FALSE -> "FAILURE PREDICTION THRESHOLD EXCEEDED (FALSE)"
        | ASCCd.FILEMARK_DETECTED -> "FILEMARK DETECTED"
        | ASCCd.FILEMARK_OR_SETMARK_NOT_FOUND -> "FILEMARK OR SETMARK NOT FOUND"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_ACCESS_TIMES_TOO_HIGH -> "FIRMWARE IMPENDING FAILURE ACCESS TIMES TOO HIGH"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_CHANNEL_PARAMETRICS -> "FIRMWARE IMPENDING FAILURE CHANNEL PARAMETRICS"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_CONTROLLER_DETECTED -> "FIRMWARE IMPENDING FAILURE CONTROLLER DETECTED"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_DATA_ERROR_RATE_TOO_HIGH -> "FIRMWARE IMPENDING FAILURE DATA ERROR RATE TOO HIGH"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_DRIVE_CALIBRATION_RETRY_COUNT -> "FIRMWARE IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_DRIVE_ERROR_RATE_TOO_HIGH -> "FIRMWARE IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_GENERAL_HARD_DRIVE_FAILURE -> "FIRMWARE IMPENDING FAILURE GENERAL HARD DRIVE FAILURE"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_SEEK_ERROR_RATE_TOO_HIGH -> "FIRMWARE IMPENDING FAILURE SEEK ERROR RATE TOO HIGH"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_SEEK_TIME_PERFORMANCE -> "FIRMWARE IMPENDING FAILURE SEEK TIME PERFORMANCE"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_SPIN_UP_RETRY_COUNT -> "FIRMWARE IMPENDING FAILURE SPIN-UP RETRY COUNT"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_START_UNIT_TIMES_TOO_HIGH -> "FIRMWARE IMPENDING FAILURE START UNIT TIMES TOO HIGH"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_THROUGHPUT_PERFORMANCE -> "FIRMWARE IMPENDING FAILURE THROUGHPUT PERFORMANCE"
        | ASCCd.FIRMWARE_IMPENDING_FAILURE_TOO_MANY_BLOCK_REASSIGNS -> "FIRMWARE IMPENDING FAILURE TOO MANY BLOCK REASSIGNS"
        | ASCCd.FOCUS_SERVO_FAILURE -> "FOCUS SERVO FAILURE"
        | ASCCd.FORMAT_COMMAND_FAILED -> "FORMAT COMMAND FAILED"
        | ASCCd.GENERATION_DOES_NOT_EXIST -> "GENERATION DOES NOT EXIST"
        | ASCCd.GROWN_DEFECT_LIST_NOT_FOUND -> "GROWN DEFECT LIST NOT FOUND"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_ACCESS_TIMES_TOO_HIGH -> "HARDWARE IMPENDING FAILURE ACCESS TIMES TOO HIGH"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_CHANNEL_PARAMETRICS -> "HARDWARE IMPENDING FAILURE CHANNEL PARAMETRICS"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_CONTROLLER_DETECTED -> "HARDWARE IMPENDING FAILURE CONTROLLER DETECTED"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_DATA_ERROR_RATE_TOO_HIGH -> "HARDWARE IMPENDING FAILURE DATA ERROR RATE TOO HIGH"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_DRIVE_CALIBRATION_RETRY_COUNT -> "HARDWARE IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_DRIVE_ERROR_RATE_TOO_HIGH -> "HARDWARE IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_GENERAL_HARD_DRIVE_FAILURE -> "HARDWARE IMPENDING FAILURE GENERAL HARD DRIVE FAILURE"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_SEEK_ERROR_RATE_TOO_HIGH -> "HARDWARE IMPENDING FAILURE SEEK ERROR RATE TOO HIGH"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_SEEK_TIME_PERFORMANCE -> "HARDWARE IMPENDING FAILURE SEEK TIME PERFORMANCE"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_SPIN_UP_RETRY_COUNT -> "HARDWARE IMPENDING FAILURE SPIN-UP RETRY COUNT"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_START_UNIT_TIMES_TOO_HIGH -> "HARDWARE IMPENDING FAILURE START UNIT TIMES TOO HIGH"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_THROUGHPUT_PERFORMANCE -> "HARDWARE IMPENDING FAILURE THROUGHPUT PERFORMANCE"
        | ASCCd.HARDWARE_IMPENDING_FAILURE_TOO_MANY_BLOCK_REASSIGNS -> "HARDWARE IMPENDING FAILURE TOO MANY BLOCK REASSIGNS"
        | ASCCd.HARDWARE_WRITE_PROTECTED -> "HARDWARE WRITE PROTECTED"
        | ASCCd.HEAD_SELECT_FAULT -> "HEAD SELECT FAULT"
        | ASCCd.I_O_PROCESS_TERMINATED -> "I/O PROCESS TERMINATED"
        | ASCCd.ID_CRC_OR_ECC_ERROR -> "ID CRC OR ECC ERROR"
        | ASCCd.IDLE_CONDITION_ACTIVATED_BY_COMMAND -> "IDLE CONDITION ACTIVATED BY COMMAND"
        | ASCCd.IDLE_CONDITION_ACTIVATED_BY_TIMER -> "IDLE CONDITION ACTIVATED BY TIMER"
        | ASCCd.ILLEGAL_COMMAND_WHILE_IN_EXPLICIT_ADDRESS_MODE -> "ILLEGAL COMMAND WHILE IN EXPLICIT ADDRESS MODE"
        | ASCCd.ILLEGAL_COMMAND_WHILE_IN_IMPLICIT_ADDRESS_MODE -> "ILLEGAL COMMAND WHILE IN IMPLICIT ADDRESS MODE"
        | ASCCd.ILLEGAL_COMMAND_WHILE_IN_WRITE_CAPABLE_STATE -> "ILLEGAL COMMAND WHILE IN WRITE CAPABLE STATE"
        | ASCCd.ILLEGAL_FUNCTION -> "ILLEGAL FUNCTION"
        | ASCCd.ILLEGAL_MODE_FOR_THIS_TRACK -> "ILLEGAL MODE FOR THIS TRACK"
        | ASCCd.ILLEGAL_POWER_CONDITION_REQUEST -> "ILLEGAL POWER CONDITION REQUEST"
        | ASCCd.IMPLICIT_ASYMMETRIC_ACCESS_STATE_TRANSITION_FAILED -> "IMPLICIT ASYMMETRIC ACCESS STATE TRANSITION FAILED"
        | ASCCd.IMPORT_OR_EXPORT_ELEMENT_ACCESSED -> "IMPORT OR EXPORT ELEMENT ACCESSED"
        | ASCCd.INCOMPATIBLE_MEDIUM_INSTALLED -> "INCOMPATIBLE MEDIUM INSTALLED"
        | ASCCd.INCOMPLETE_BLOCK_READ -> "INCOMPLETE BLOCK READ"
        | ASCCd.INCORRECT_COPY_TARGET_DEVICE_TYPE -> "INCORRECT COPY TARGET DEVICE TYPE"
        | ASCCd.INFORMATION_UNIT_TOO_LONG -> "INFORMATION UNIT TOO LONG"
        | ASCCd.INFORMATION_UNIT_TOO_SHORT -> "INFORMATION UNIT TOO SHORT"
        | ASCCd.INFORMATION_UNIT_iuCRC_ERROR_DETECTED -> "INFORMATION UNIT iuCRC ERROR DETECTED"
        | ASCCd.INFORMATIONAL_REFER_TO_LOG -> "INFORMATIONAL, REFER TO LOG"
        | ASCCd.INITIATOR_DETECTED_ERROR_MESSAGE_RECEIVED -> "INITIATOR DETECTED ERROR MESSAGE RECEIVED"
        | ASCCd.INITIATOR_RESPONSE_TIMEOUT -> "INITIATOR RESPONSE TIMEOUT"
        | ASCCd.INLINE_DATA_LENGTH_EXCEEDED -> "INLINE DATA LENGTH EXCEEDED"
        | ASCCd.INQUIRY_DATA_HAS_CHANGED -> "INQUIRY DATA HAS CHANGED"
        | ASCCd.INSUFFICIENT_ACCESS_CONTROL_RESOURCES -> "INSUFFICIENT ACCESS CONTROL RESOURCES"
        | ASCCd.INSUFFICIENT_REGISTRATION_RESOURCES -> "INSUFFICIENT REGISTRATION RESOURCES"
        | ASCCd.INSUFFICIENT_RESERVATION_RESOURCES -> "INSUFFICIENT RESERVATION RESOURCES"
        | ASCCd.INSUFFICIENT_RESOURCES -> "INSUFFICIENT RESOURCES"
        | ASCCd.INSUFFICIENT_TIME_FOR_OPERATION -> "INSUFFICIENT TIME FOR OPERATION"
        | ASCCd.INTERNAL_TARGET_FAILURE -> "INTERNAL TARGET FAILURE"
        | ASCCd.INVALID_ADDRESS_FOR_WRITE -> "INVALID ADDRESS FOR WRITE"
        | ASCCd.INVALID_BITS_IN_IDENTIFY_MESSAGE -> "INVALID BITS IN IDENTIFY MESSAGE"
        | ASCCd.INVALID_COMBINATION_OF_WINDOWS_SPECIFIED -> "INVALID COMBINATION OF WINDOWS SPECIFIED"
        | ASCCd.INVALID_COMMAND_OPERATION_CODE -> "INVALID COMMAND OPERATION CODE"
        | ASCCd.INVALID_DATA_OUT_BUFFER_INTEGRITY_CHECK_VALUE -> "INVALID DATA-OUT BUFFER INTEGRITY CHECK VALUE"
        | ASCCd.INVALID_ELEMENT_ADDRESS -> "INVALID ELEMENT ADDRESS"
        | ASCCd.INVALID_FIELD_IN_CDB -> "INVALID FIELD IN CDB"
        | ASCCd.INVALID_FIELD_IN_COMMAND_INFORMATION_UNIT -> "INVALID FIELD IN COMMAND INFORMATION UNIT"
        | ASCCd.INVALID_FIELD_IN_PARAMETER_LIST -> "INVALID FIELD IN PARAMETER LIST"
        | ASCCd.INVALID_INFORMATION_UNIT -> "INVALID INFORMATION UNIT"
        | ASCCd.INVALID_MESSAGE_ERROR -> "INVALID MESSAGE ERROR"
        | ASCCd.INVALID_OPERATION_FOR_COPY_SOURCE_OR_DESTINATION -> "INVALID OPERATION FOR COPY SOURCE OR DESTINATION"
        | ASCCd.INVALID_PACKET_SIZE -> "INVALID PACKET SIZE"
        | ASCCd.INVALID_PARAMETER_WHILE_PORT_IS_ENABLED -> "INVALID PARAMETER WHILE PORT IS ENABLED"
        | ASCCd.INVALID_RELEASE_OF_PERSISTENT_RESERVATION -> "INVALID RELEASE OF PERSISTENT RESERVATION"
        | ASCCd.INVALID_TARGET_PORT_TRANSFER_TAG_RECEIVED -> "INVALID TARGET PORT TRANSFER TAG RECEIVED"
        | ASCCd.I_T_NEXUS_LOSS_OCCURRED -> "I_T NEXUS LOSS OCCURRED"
        | ASCCd.L_EC_UNCORRECTABLE_ERROR -> "L-EC UNCORRECTABLE ERROR"
        | ASCCd.LAMP_FAILURE -> "LAMP FAILURE"
        | ASCCd.LOCATE_OPERATION_FAILURE -> "LOCATE OPERATION FAILURE"
        | ASCCd.LOCATE_OPERATION_IN_PROGRESS -> "LOCATE OPERATION IN PROGRESS"
        | ASCCd.LOG_COUNTER_AT_MAXIMUM -> "LOG COUNTER AT MAXIMUM"
        | ASCCd.LOG_EXCEPTION -> "LOG EXCEPTION"
        | ASCCd.LOG_LIST_CODES_EXHAUSTED -> "LOG LIST CODES EXHAUSTED"
        | ASCCd.LOG_PARAMETERS_CHANGED -> "LOG PARAMETERS CHANGED"
        | ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE -> "LOGICAL BLOCK ADDRESS OUT OF RANGE"
        | ASCCd.LOGICAL_UNIT_COMMUNICATION_CRC_ERROR_ULTRA_DMA_32 -> "LOGICAL UNIT COMMUNICATION CRC ERROR (ULTRA-DMA/32)"
        | ASCCd.LOGICAL_UNIT_COMMUNICATION_FAILURE -> "LOGICAL UNIT COMMUNICATION FAILURE"
        | ASCCd.LOGICAL_UNIT_COMMUNICATION_PARITY_ERROR -> "LOGICAL UNIT COMMUNICATION PARITY ERROR"
        | ASCCd.LOGICAL_UNIT_COMMUNICATION_TIME_OUT -> "LOGICAL UNIT COMMUNICATION TIME-OUT"
        | ASCCd.LOGICAL_UNIT_DOES_NOT_RESPOND_TO_SELECTION -> "LOGICAL UNIT DOES NOT RESPOND TO SELECTION"
        | ASCCd.LOGICAL_UNIT_FAILED_SELF_CONFIGURATION -> "LOGICAL UNIT FAILED SELF-CONFIGURATION"
        | ASCCd.LOGICAL_UNIT_FAILED_SELF_TEST -> "LOGICAL UNIT FAILED SELF-TEST"
        | ASCCd.LOGICAL_UNIT_FAILURE -> "LOGICAL UNIT FAILURE"
        | ASCCd.LOGICAL_UNIT_FAILURE_PREDICTION_THRESHOLD_EXCEEDED -> "LOGICAL UNIT FAILURE PREDICTION THRESHOLD EXCEEDED"
        | ASCCd.LOGICAL_UNIT_HAS_NOT_SELF_CONFIGURED_YET -> "LOGICAL UNIT HAS NOT SELF-CONFIGURED YET"
        | ASCCd.LOGICAL_UNIT_IS_IN_PROCESS_OF_BECOMING_READY -> "LOGICAL UNIT IS IN PROCESS OF BECOMING READY"
        | ASCCd.LOGICAL_UNIT_NOT_ACCESSIBLE_ASYMMETRIC_ACCESS_STATE_TRANSITION -> "LOGICAL UNIT NOT ACCESSIBLE, ASYMMETRIC ACCESS STATE TRANSITION"
        | ASCCd.LOGICAL_UNIT_NOT_ACCESSIBLE_TARGET_PORT_IN_STANDBY_STATE -> "LOGICAL UNIT NOT ACCESSIBLE, TARGET PORT IN STANDBY STATE"
        | ASCCd.LOGICAL_UNIT_NOT_ACCESSIBLE_TARGET_PORT_IN_UNAVAILABLE_STATE -> "LOGICAL UNIT NOT ACCESSIBLE, TARGET PORT IN UNAVAILABLE STATE"
        | ASCCd.LOGICAL_UNIT_NOT_CONFIGURED -> "LOGICAL UNIT NOT CONFIGURED"
        | ASCCd.LOGICAL_UNIT_NOT_READY_AUXILIARY_MEMORY_NOT_ACCESSIBLE -> "LOGICAL UNIT NOT READY, AUXILIARY MEMORY NOT ACCESSIBLE"
        | ASCCd.LOGICAL_UNIT_NOT_READY_CAUSE_NOT_REPORTABLE -> "LOGICAL UNIT NOT READY, CAUSE NOT REPORTABLE"
        | ASCCd.LOGICAL_UNIT_NOT_READY_FORMAT_IN_PROGRESS -> "LOGICAL UNIT NOT READY, FORMAT IN PROGRESS"
        | ASCCd.LOGICAL_UNIT_NOT_READY_INITIALIZING_COMMAND_REQUIRED -> "LOGICAL UNIT NOT READY, INITIALIZING COMMAND REQUIRED"
        | ASCCd.LOGICAL_UNIT_NOT_READY_LONG_WRITE_IN_PROGRESS -> "LOGICAL UNIT NOT READY, LONG WRITE IN PROGRESS"
        | ASCCd.LOGICAL_UNIT_NOT_READY_MANUAL_INTERVENTION_REQUIRED -> "LOGICAL UNIT NOT READY, MANUAL INTERVENTION REQUIRED"
        | ASCCd.LOGICAL_UNIT_NOT_READY_NOTIFY_ENABLE_SPINUP_REQUIRED -> "LOGICAL UNIT NOT READY, NOTIFY (ENABLE SPINUP) REQUIRED"
        | ASCCd.LOGICAL_UNIT_NOT_READY_OFFLINE -> "LOGICAL UNIT NOT READY, OFFLINE"
        | ASCCd.LOGICAL_UNIT_NOT_READY_OPERATION_IN_PROGRESS -> "LOGICAL UNIT NOT READY, OPERATION IN PROGRESS"
        | ASCCd.LOGICAL_UNIT_NOT_READY_REBUILD_IN_PROGRESS -> "LOGICAL UNIT NOT READY, REBUILD IN PROGRESS"
        | ASCCd.LOGICAL_UNIT_NOT_READY_RECALCULATION_IN_PROGRESS -> "LOGICAL UNIT NOT READY, RECALCULATION IN PROGRESS"
        | ASCCd.LOGICAL_UNIT_NOT_READY_SELF_TEST_IN_PROGRESS -> "LOGICAL UNIT NOT READY, SELF-TEST IN PROGRESS"
        | ASCCd.LOGICAL_UNIT_NOT_SUPPORTED -> "LOGICAL UNIT NOT SUPPORTED"
        | ASCCd.LOGICAL_UNIT_SOFTWARE_WRITE_PROTECTED -> "LOGICAL UNIT SOFTWARE WRITE PROTECTED"
        | ASCCd.LOGICAL_UNIT_UNABLE_TO_UPDATE_SELF_TEST_LOG -> "LOGICAL UNIT UNABLE TO UPDATE SELF-TEST LOG"
        | ASCCd.LOW_POWER_CONDITION_ON -> "LOW POWER CONDITION ON"
        | ASCCd.MECHANICAL_POSITIONING_ERROR -> "MECHANICAL POSITIONING ERROR"
        | ASCCd.MECHANICAL_POSITIONING_OR_CHANGER_ERROR -> "MECHANICAL POSITIONING OR CHANGER ERROR"
        | ASCCd.MEDIA_FAILURE_PREDICTION_THRESHOLD_EXCEEDED -> "MEDIA FAILURE PREDICTION THRESHOLD EXCEEDED"
        | ASCCd.MEDIA_LOAD_OR_EJECT_FAILED -> "MEDIA LOAD OR EJECT FAILED"
        | ASCCd.MEDIA_REGION_CODE_IS_MISMATCHED_TO_LOGICAL_UNIT_REGION -> "MEDIA REGION CODE IS MISMATCHED TO LOGICAL UNIT REGION"
        | ASCCd.MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE -> "MEDIUM AUXILIARY MEMORY ACCESSIBLE"
        | ASCCd.MEDIUM_DESTINATION_ELEMENT_FULL -> "MEDIUM DESTINATION ELEMENT FULL"
        | ASCCd.MEDIUM_FORMAT_CORRUPTED -> "MEDIUM FORMAT CORRUPTED"
        | ASCCd.MEDIUM_LOADABLE -> "MEDIUM LOADABLE"
        | ASCCd.MEDIUM_MAGAZINE_INSERTED -> "MEDIUM MAGAZINE INSERTED"
        | ASCCd.MEDIUM_MAGAZINE_LOCKED -> "MEDIUM MAGAZINE LOCKED"
        | ASCCd.MEDIUM_MAGAZINE_NOT_ACCESSIBLE -> "MEDIUM MAGAZINE NOT ACCESSIBLE"
        | ASCCd.MEDIUM_MAGAZINE_REMOVED -> "MEDIUM MAGAZINE REMOVED"
        | ASCCd.MEDIUM_MAGAZINE_UNLOCKED -> "MEDIUM MAGAZINE UNLOCKED"
        | ASCCd.MEDIUM_NOT_FORMATTED -> "MEDIUM NOT FORMATTED"
        | ASCCd.MEDIUM_NOT_PRESENT -> "MEDIUM NOT PRESENT"
        | ASCCd.MEDIUM_NOT_PRESENT_LOADABLE -> "MEDIUM NOT PRESENT - LOADABLE"
        | ASCCd.MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE -> "MEDIUM NOT PRESENT - MEDIUM AUXILIARY MEMORY ACCESSIBLE"
        | ASCCd.MEDIUM_NOT_PRESENT_TRAY_CLOSED -> "MEDIUM NOT PRESENT - TRAY CLOSED"
        | ASCCd.MEDIUM_NOT_PRESENT_TRAY_OPEN -> "MEDIUM NOT PRESENT - TRAY OPEN"
        | ASCCd.MEDIUM_REMOVAL_PREVENTED -> "MEDIUM REMOVAL PREVENTED"
        | ASCCd.MEDIUM_SOURCE_ELEMENT_EMPTY -> "MEDIUM SOURCE ELEMENT EMPTY"
        | ASCCd.MESSAGE_ERROR -> "MESSAGE ERROR"
        | ASCCd.MICROCODE_HAS_BEEN_CHANGED -> "MICROCODE HAS BEEN CHANGED"
        | ASCCd.MISCOMPARE_DURING_VERIFY_OPERATION -> "MISCOMPARE DURING VERIFY OPERATION"
        | ASCCd.MISCORRECTED_ERROR -> "MISCORRECTED ERROR"
        | ASCCd.MODE_PARAMETERS_CHANGED -> "MODE PARAMETERS CHANGED"
        | ASCCd.MODIFICATION_OF_LOGICAL_UNIT_FAILED -> "MODIFICATION OF LOGICAL UNIT FAILED"
        | ASCCd.MULTIPLE_LOGICAL_UNIT_FAILURES -> "MULTIPLE LOGICAL UNIT FAILURES"
        | ASCCd.MULTIPLE_PERIPHERAL_DEVICES_SELECTED -> "MULTIPLE PERIPHERAL DEVICES SELECTED"
        | ASCCd.MULTIPLE_READ_ERRORS -> "MULTIPLE READ ERRORS"
        | ASCCd.MULTIPLY_ASSIGNED_LOGICAL_UNIT -> "MULTIPLY ASSIGNED LOGICAL UNIT"
        | ASCCd.NAK_RECEIVED -> "NAK RECEIVED"
        | ASCCd.NO_ADDITIONAL_SENSE_INFORMATION -> "NO ADDITIONAL SENSE INFORMATION"
        | ASCCd.NO_CURRENT_AUDIO_STATUS_TO_RETURN -> "NO CURRENT AUDIO STATUS TO RETURN"
        | ASCCd.NO_DEFECT_SPARE_LOCATION_AVAILABLE -> "NO DEFECT SPARE LOCATION AVAILABLE"
        | ASCCd.NO_GAP_FOUND -> "NO GAP FOUND"
        | ASCCd.NO_INDEX_SECTOR_SIGNAL -> "NO INDEX/SECTOR SIGNAL"
        | ASCCd.NO_MORE_TRACK_RESERVATIONS_ALLOWED -> "NO MORE TRACK RESERVATIONS ALLOWED"
        | ASCCd.NO_REFERENCE_POSITION_FOUND -> "NO REFERENCE POSITION FOUND"
        | ASCCd.NO_SEEK_COMPLETE -> "NO SEEK COMPLETE"
        | ASCCd.NO_WRITE_CURRENT -> "NO WRITE CURRENT"
        | ASCCd.NONCE_NOT_UNIQUE -> "NONCE NOT UNIQUE"
        | ASCCd.NONCE_TIMESTAMP_OUT_OF_RANGE -> "NONCE TIMESTAMP OUT OF RANGE"
        | ASCCd.NOT_READY_TO_READY_CHANGE_MEDIUM_MAY_HAVE_CHANGED -> "NOT READY TO READY CHANGE, MEDIUM MAY HAVE CHANGED"
        | ASCCd.NOT_RESERVED -> "NOT RESERVED"
        | ASCCd.OPERATION_IN_PROGRESS -> "OPERATION IN PROGRESS"
        | ASCCd.OPERATOR_MEDIUM_REMOVAL_REQUEST -> "OPERATOR MEDIUM REMOVAL REQUEST"
        | ASCCd.OPERATOR_REQUEST_OR_STATE_CHANGE_INPUT -> "OPERATOR REQUEST OR STATE CHANGE INPUT"
        | ASCCd.OPERATOR_SELECTED_WRITE_PERMIT -> "OPERATOR SELECTED WRITE PERMIT"
        | ASCCd.OPERATOR_SELECTED_WRITE_PROTECT -> "OPERATOR SELECTED WRITE PROTECT"
        | ASCCd.OUT_OF_FOCUS -> "OUT OF FOCUS"
        | ASCCd.OVERLAPPED_COMMANDS_ATTEMPTED -> "OVERLAPPED COMMANDS ATTEMPTED"
        | ASCCd.OVERWRITE_ERROR_ON_UPDATE_IN_PLACE -> "OVERWRITE ERROR ON UPDATE IN PLACE"
        | ASCCd.PACKET_DOES_NOT_FIT_IN_AVAILABLE_SPACE -> "PACKET DOES NOT FIT IN AVAILABLE SPACE"
        | ASCCd.PAPER_JAM -> "PAPER JAM"
        | ASCCd.PARAMETER_LIST_LENGTH_ERROR -> "PARAMETER LIST LENGTH ERROR"
        | ASCCd.PARAMETER_NOT_SUPPORTED -> "PARAMETER NOT SUPPORTED"
        | ASCCd.PARAMETER_VALUE_INVALID -> "PARAMETER VALUE INVALID"
        | ASCCd.PARAMETERS_CHANGED -> "PARAMETERS CHANGED"
        | ASCCd.PARITY_DATA_MISMATCH -> "PARITY/DATA MISMATCH"
        | ASCCd.PARTIAL_DEFECT_LIST_TRANSFER -> "PARTIAL DEFECT LIST TRANSFER"
        | ASCCd.PARTITION_OR_COLLECTION_CONTAINS_USER_OBJECTS -> "PARTITION OR COLLECTION CONTAINS USER OBJECTS"
        | ASCCd.PERIPHERAL_DEVICE_WRITE_FAULT -> "PERIPHERAL DEVICE WRITE FAULT"
        | ASCCd.PERMANENT_WRITE_PROTECT -> "PERMANENT WRITE PROTECT"
        | ASCCd.PERSISTENT_PREVENT_CONFLICT -> "PERSISTENT PREVENT CONFLICT"
        | ASCCd.PERSISTENT_WRITE_PROTECT -> "PERSISTENT WRITE PROTECT"
        | ASCCd.PHY_TEST_FUNCTION_IN_PROGRESS -> "PHY TEST FUNCTION IN PROGRESS"
        | ASCCd.POSITION_ERROR_RELATED_TO_TIMING -> "POSITION ERROR RELATED TO TIMING"
        | ASCCd.POSITION_PAST_BEGINNING_OF_MEDIUM -> "POSITION PAST BEGINNING OF MEDIUM"
        | ASCCd.POSITION_PAST_END_OF_MEDIUM -> "POSITION PAST END OF MEDIUM"
        | ASCCd.POSITIONING_ERROR_DETECTED_BY_READ_OF_MEDIUM -> "POSITIONING ERROR DETECTED BY READ OF MEDIUM"
        | ASCCd.POWER_CALIBRATION_AREA_ALMOST_FULL -> "POWER CALIBRATION AREA ALMOST FULL"
        | ASCCd.POWER_CALIBRATION_AREA_ERROR -> "POWER CALIBRATION AREA ERROR"
        | ASCCd.POWER_CALIBRATION_AREA_IS_FULL -> "POWER CALIBRATION AREA IS FULL"
        | ASCCd.POWER_ON_OCCURRED -> "POWER ON OCCURRED"
        | ASCCd.POWER_ON_RESET_OR_BUS_DEVICE_RESET_OCCURRED -> "POWER ON, RESET, OR BUS DEVICE RESET OCCURRED"
        | ASCCd.POWER_STATE_CHANGE_TO_ACTIVE -> "POWER STATE CHANGE TO ACTIVE"
        | ASCCd.POWER_STATE_CHANGE_TO_DEVICE_CONTROL -> "POWER STATE CHANGE TO DEVICE CONTROL"
        | ASCCd.POWER_STATE_CHANGE_TO_IDLE -> "POWER STATE CHANGE TO IDLE"
        | ASCCd.POWER_STATE_CHANGE_TO_SLEEP -> "POWER STATE CHANGE TO SLEEP"
        | ASCCd.POWER_STATE_CHANGE_TO_STANDBY -> "POWER STATE CHANGE TO STANDBY"
        | ASCCd.PREVIOUS_BUSY_STATUS -> "PREVIOUS BUSY STATUS"
        | ASCCd.PREVIOUS_RESERVATION_CONFLICT_STATUS -> "PREVIOUS RESERVATION CONFLICT STATUS"
        | ASCCd.PREVIOUS_TASK_SET_FULL_STATUS -> "PREVIOUS TASK SET FULL STATUS"
        | ASCCd.PRIMARY_DEFECT_LIST_NOT_FOUND -> "PRIMARY DEFECT LIST NOT FOUND"
        | ASCCd.PRIORITY_CHANGED -> "PRIORITY CHANGED"
        | ASCCd.PROGRAM_MEMORY_AREA_IS_FULL -> "PROGRAM MEMORY AREA IS FULL"
        | ASCCd.PROGRAM_MEMORY_AREA_UPDATE_FAILURE -> "PROGRAM MEMORY AREA UPDATE FAILURE"
        | ASCCd.PROTOCOL_SERVICE_CRC_ERROR -> "PROTOCOL SERVICE CRC ERROR"
        | ASCCd.QUOTA_ERROR -> "QUOTA ERROR"
        | ASCCd.RANDOM_POSITIONING_ERROR -> "RANDOM POSITIONING ERROR"
        | ASCCd.READ_ERROR_FAILED_RETRANSMISSION_REQUEST -> "READ ERROR - FAILED RETRANSMISSION REQUEST"
        | ASCCd.READ_ERROR_LOSS_OF_STREAMING -> "READ ERROR - LOSS OF STREAMING"
        | ASCCd.READ_OF_SCRAMBLED_SECTOR_WITHOUT_AUTHENTICATION -> "READ OF SCRAMBLED SECTOR WITHOUT AUTHENTICATION"
        | ASCCd.READ_PAST_BEGINNING_OF_MEDIUM -> "READ PAST BEGINNING OF MEDIUM"
        | ASCCd.READ_PAST_END_OF_MEDIUM -> "READ PAST END OF MEDIUM"
        | ASCCd.READ_PAST_END_OF_USER_OBJECT -> "READ PAST END OF USER OBJECT"
        | ASCCd.READ_RETRIES_EXHAUSTED -> "READ RETRIES EXHAUSTED"
        | ASCCd.REBUILD_FAILURE_OCCURRED -> "REBUILD FAILURE OCCURRED"
        | ASCCd.RECALCULATE_FAILURE_OCCURRED -> "RECALCULATE FAILURE OCCURRED"
        | ASCCd.RECORD_NOT_FOUND -> "RECORD NOT FOUND"
        | ASCCd.RECORD_NOT_FOUND_DATA_AUTO_REALLOCATED -> "RECORD NOT FOUND - DATA AUTO-REALLOCATED"
        | ASCCd.RECORD_NOT_FOUND_RECOMMEND_REASSIGNMENT -> "RECORD NOT FOUND - RECOMMEND REASSIGNMENT"
        | ASCCd.RECORDED_ENTITY_NOT_FOUND -> "RECORDED ENTITY NOT FOUND"
        | ASCCd.RECOVERED_DATA_DATA_AUTO_REALLOCATED -> "RECOVERED DATA - DATA AUTO-REALLOCATED"
        | ASCCd.RECOVERED_DATA_RECOMMEND_REASSIGNMENT -> "RECOVERED DATA - RECOMMEND REASSIGNMENT"
        | ASCCd.RECOVERED_DATA_RECOMMEND_REWRITE -> "RECOVERED DATA - RECOMMEND REWRITE"
        | ASCCd.RECOVERED_DATA_USING_PREVIOUS_SECTOR_ID -> "RECOVERED DATA USING PREVIOUS SECTOR ID"
        | ASCCd.RECOVERED_DATA_WITH_CIRC -> "RECOVERED DATA WITH CIRC"
        | ASCCd.RECOVERED_DATA_WITH_ECC_DATA_REWRITTEN -> "RECOVERED DATA WITH ECC - DATA REWRITTEN"
        | ASCCd.RECOVERED_DATA_WITH_ERROR_CORR_AND_RETRIES_APPLIED -> "RECOVERED DATA WITH ERROR CORR. & RETRIES APPLIED"
        | ASCCd.RECOVERED_DATA_WITH_ERROR_CORRECTION_APPLIED -> "RECOVERED DATA WITH ERROR CORRECTION APPLIED"
        | ASCCd.RECOVERED_DATA_WITH_L_EC -> "RECOVERED DATA WITH L-EC"
        | ASCCd.RECOVERED_DATA_WITH_LINKING -> "RECOVERED DATA WITH LINKING"
        | ASCCd.RECOVERED_DATA_WITH_NEGATIVE_HEAD_OFFSET -> "RECOVERED DATA WITH NEGATIVE HEAD OFFSET"
        | ASCCd.RECOVERED_DATA_WITH_NO_ERROR_CORRECTION_APPLIED -> "RECOVERED DATA WITH NO ERROR CORRECTION APPLIED"
        | ASCCd.RECOVERED_DATA_WITH_POSITIVE_HEAD_OFFSET -> "RECOVERED DATA WITH POSITIVE HEAD OFFSET"
        | ASCCd.RECOVERED_DATA_WITH_RETRIES -> "RECOVERED DATA WITH RETRIES"
        | ASCCd.RECOVERED_DATA_WITH_RETRIES_AND_OR_CIRC_APPLIED -> "RECOVERED DATA WITH RETRIES AND/OR CIRC APPLIED"
        | ASCCd.RECOVERED_DATA_WITHOUT_ECC_DATA_AUTO_REALLOCATED -> "RECOVERED DATA WITHOUT ECC - DATA AUTO-REALLOCATED"
        | ASCCd.RECOVERED_DATA_WITHOUT_ECC_DATA_REWRITTEN -> "RECOVERED DATA WITHOUT ECC - DATA REWRITTEN"
        | ASCCd.RECOVERED_DATA_WITHOUT_ECC_RECOMMEND_REASSIGNMENT -> "RECOVERED DATA WITHOUT ECC - RECOMMEND REASSIGNMENT"
        | ASCCd.RECOVERED_DATA_WITHOUT_ECC_RECOMMEND_REWRITE -> "RECOVERED DATA WITHOUT ECC - RECOMMEND REWRITE"
        | ASCCd.RECOVERED_ID_WITH_ECC_CORRECTION -> "RECOVERED ID WITH ECC CORRECTION"
        | ASCCd.REDUNDANCY_GROUP_CREATED_OR_MODIFIED -> "REDUNDANCY GROUP CREATED OR MODIFIED"
        | ASCCd.REDUNDANCY_GROUP_DELETED -> "REDUNDANCY GROUP DELETED"
        | ASCCd.REDUNDANCY_LEVEL_GOT_BETTER -> "REDUNDANCY LEVEL GOT BETTER"
        | ASCCd.REDUNDANCY_LEVEL_GOT_WORSE -> "REDUNDANCY LEVEL GOT WORSE"
        | ASCCd.REGISTRATIONS_PREEMPTED -> "REGISTRATIONS PREEMPTED"
        | ASCCd.REMOVE_OF_LOGICAL_UNIT_FAILED -> "REMOVE OF LOGICAL UNIT FAILED"
        | ASCCd.REPORTED_LUNS_DATA_HAS_CHANGED -> "REPORTED LUNS DATA HAS CHANGED"
        | ASCCd.REPOSITION_ERROR -> "REPOSITION ERROR"
        | ASCCd.RESERVATIONS_PREEMPTED -> "RESERVATIONS PREEMPTED"
        | ASCCd.RESERVATIONS_RELEASED -> "RESERVATIONS RELEASED"
        | ASCCd.REWIND_OPERATION_IN_PROGRESS -> "REWIND OPERATION IN PROGRESS"
        | ASCCd.RIBBON_INK_OR_TONER_FAILURE -> "RIBBON, INK, OR TONER FAILURE"
        | ASCCd.RMA_PMA_IS_ALMOST_FULL -> "RMA/PMA IS ALMOST FULL"
        | ASCCd.ROUNDED_PARAMETER -> "ROUNDED PARAMETER"
        | ASCCd.RPL_STATUS_CHANGE -> "RPL STATUS CHANGE"
        | ASCCd.SAVING_PARAMETERS_NOT_SUPPORTED -> "SAVING PARAMETERS NOT SUPPORTED"
        | ASCCd.SCAN_HEAD_POSITIONING_ERROR -> "SCAN HEAD POSITIONING ERROR"
        | ASCCd.SCSI_BUS_RESET_OCCURRED -> "SCSI BUS RESET OCCURRED"
        | ASCCd.SCSI_PARITY_ERROR -> "SCSI PARITY ERROR"
        | ASCCd.SCSI_PARITY_ERROR_DETECTED_DURING_ST_DATA_PHASE -> "SCSI PARITY ERROR DETECTED DURING ST DATA PHASE"
        | ASCCd.SCSI_TO_HOST_SYSTEM_INTERFACE_FAILURE -> "SCSI TO HOST SYSTEM INTERFACE FAILURE"
        | ASCCd.SECURITY_AUDIT_VALUE_FROZEN -> "SECURITY AUDIT VALUE FROZEN"
        | ASCCd.SECURITY_WORKING_KEY_FROZEN -> "SECURITY WORKING KEY FROZEN"
        | ASCCd.SELECT_OR_RESELECT_FAILURE -> "SELECT OR RESELECT FAILURE"
        | ASCCd.SEQUENTIAL_POSITIONING_ERROR -> "SEQUENTIAL POSITIONING ERROR"
        | ASCCd.SERVO_IMPENDING_FAILURE_ACCESS_TIMES_TOO_HIGH -> "SERVO IMPENDING FAILURE ACCESS TIMES TOO HIGH"
        | ASCCd.SERVO_IMPENDING_FAILURE_CHANNEL_PARAMETRICS -> "SERVO IMPENDING FAILURE CHANNEL PARAMETRICS"
        | ASCCd.SERVO_IMPENDING_FAILURE_CONTROLLER_DETECTED -> "SERVO IMPENDING FAILURE CONTROLLER DETECTED"
        | ASCCd.SERVO_IMPENDING_FAILURE_DATA_ERROR_RATE_TOO_HIGH -> "SERVO IMPENDING FAILURE DATA ERROR RATE TOO HIGH"
        | ASCCd.SERVO_IMPENDING_FAILURE_DRIVE_CALIBRATION_RETRY_COUNT -> "SERVO IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT"
        | ASCCd.SERVO_IMPENDING_FAILURE_DRIVE_ERROR_RATE_TOO_HIGH -> "SERVO IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH"
        | ASCCd.SERVO_IMPENDING_FAILURE_GENERAL_HARD_DRIVE_FAILURE -> "SERVO IMPENDING FAILURE GENERAL HARD DRIVE FAILURE"
        | ASCCd.SERVO_IMPENDING_FAILURE_SEEK_ERROR_RATE_TOO_HIGH -> "SERVO IMPENDING FAILURE SEEK ERROR RATE TOO HIGH"
        | ASCCd.SERVO_IMPENDING_FAILURE_SEEK_TIME_PERFORMANCE -> "SERVO IMPENDING FAILURE SEEK TIME PERFORMANCE"
        | ASCCd.SERVO_IMPENDING_FAILURE_SPIN_UP_RETRY_COUNT -> "SERVO IMPENDING FAILURE SPIN-UP RETRY COUNT"
        | ASCCd.SERVO_IMPENDING_FAILURE_START_UNIT_TIMES_TOO_HIGH -> "SERVO IMPENDING FAILURE START UNIT TIMES TOO HIGH"
        | ASCCd.SERVO_IMPENDING_FAILURE_THROUGHPUT_PERFORMANCE -> "SERVO IMPENDING FAILURE THROUGHPUT PERFORMANCE"
        | ASCCd.SERVO_IMPENDING_FAILURE_TOO_MANY_BLOCK_REASSIGNS -> "SERVO IMPENDING FAILURE TOO MANY BLOCK REASSIGNS"
        | ASCCd.SESSION_FIXATION_ERROR -> "SESSION FIXATION ERROR"
        | ASCCd.SESSION_FIXATION_ERROR_INCOMPLETE_TRACK_IN_SESSION -> "SESSION FIXATION ERROR - INCOMPLETE TRACK IN SESSION"
        | ASCCd.SESSION_FIXATION_ERROR_WRITING_LEAD_IN -> "SESSION FIXATION ERROR WRITING LEAD-IN"
        | ASCCd.SESSION_FIXATION_ERROR_WRITING_LEAD_OUT -> "SESSION FIXATION ERROR WRITING LEAD-OUT"
        | ASCCd.SET_CAPACITY_OPERATION_IN_PROGRESS -> "SET CAPACITY OPERATION IN PROGRESS"
        | ASCCd.SET_TARGET_PORT_GROUPS_COMMAND_FAILED -> "SET TARGET PORT GROUPS COMMAND FAILED"
        | ASCCd.SETMARK_DETECTED -> "SETMARK DETECTED"
        | ASCCd.SLEW_FAILURE -> "SLEW FAILURE"
        | ASCCd.SOME_COMMANDS_CLEARED_BY_ISCSI_PROTOCOL_EVENT -> "SOME COMMANDS CLEARED BY ISCSI PROTOCOL EVENT"
        | ASCCd.SPARE_AREA_EXHAUSTION_PREDICTION_THRESHOLD_EXCEEDED -> "SPARE AREA EXHAUSTION PREDICTION THRESHOLD EXCEEDED"
        | ASCCd.SPARE_CREATED_OR_MODIFIED -> "SPARE CREATED OR MODIFIED"
        | ASCCd.SPARE_DELETED -> "SPARE DELETED"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_ACCESS_TIMES_TOO_HIGH -> "SPINDLE IMPENDING FAILURE ACCESS TIMES TOO HIGH"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_CHANNEL_PARAMETRICS -> "SPINDLE IMPENDING FAILURE CHANNEL PARAMETRICS"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_CONTROLLER_DETECTED -> "SPINDLE IMPENDING FAILURE CONTROLLER DETECTED"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_DATA_ERROR_RATE_TOO_HIGH -> "SPINDLE IMPENDING FAILURE DATA ERROR RATE TOO HIGH"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_DRIVE_CALIBRATION_RETRY_COUNT -> "SPINDLE IMPENDING FAILURE DRIVE CALIBRATION RETRY COUNT"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_DRIVE_ERROR_RATE_TOO_HIGH -> "SPINDLE IMPENDING FAILURE DRIVE ERROR RATE TOO HIGH"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_GENERAL_HARD_DRIVE_FAILURE -> "SPINDLE IMPENDING FAILURE GENERAL HARD DRIVE FAILURE"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_SEEK_ERROR_RATE_TOO_HIGH -> "SPINDLE IMPENDING FAILURE SEEK ERROR RATE TOO HIGH"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_SEEK_TIME_PERFORMANCE -> "SPINDLE IMPENDING FAILURE SEEK TIME PERFORMANCE"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_SPIN_UP_RETRY_COUNT -> "SPINDLE IMPENDING FAILURE SPIN-UP RETRY COUNT"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_START_UNIT_TIMES_TOO_HIGH -> "SPINDLE IMPENDING FAILURE START UNIT TIMES TOO HIGH"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_THROUGHPUT_PERFORMANCE -> "SPINDLE IMPENDING FAILURE THROUGHPUT PERFORMANCE"
        | ASCCd.SPINDLE_IMPENDING_FAILURE_TOO_MANY_BLOCK_REASSIGNS -> "SPINDLE IMPENDING FAILURE TOO MANY BLOCK REASSIGNS"
        | ASCCd.SPINDLE_SERVO_FAILURE -> "SPINDLE SERVO FAILURE"
        | ASCCd.SPINDLES_NOT_SYNCHRONIZED -> "SPINDLES NOT SYNCHRONIZED"
        | ASCCd.SPINDLES_SYNCHRONIZED -> "SPINDLES SYNCHRONIZED"
        | ASCCd.STANDBY_CONDITION_ACTIVATED_BY_COMMAND -> "STANDBY CONDITION ACTIVATED BY COMMAND"
        | ASCCd.STANDBY_CONDITION_ACTIVATED_BY_TIMER -> "STANDBY CONDITION ACTIVATED BY TIMER"
        | ASCCd.STATE_CHANGE_HAS_OCCURRED -> "STATE CHANGE HAS OCCURRED"
        | ASCCd.SYNCHRONOUS_DATA_TRANSFER_ERROR -> "SYNCHRONOUS DATA TRANSFER ERROR"
        | ASCCd.SYSTEM_BUFFER_FULL -> "SYSTEM BUFFER FULL"
        | ASCCd.SYSTEM_RESOURCE_FAILURE -> "SYSTEM RESOURCE FAILURE"
        | ASCCd.TAPE_LENGTH_ERROR -> "TAPE LENGTH ERROR"
        | ASCCd.TAPE_OR_ELECTRONIC_VERTICAL_FORMS_UNIT_NOT_READY -> "TAPE OR ELECTRONIC VERTICAL FORMS UNIT NOT READY"
        | ASCCd.TAPE_POSITION_ERROR_AT_BEGINNING_OF_MEDIUM -> "TAPE POSITION ERROR AT BEGINNING-OF-MEDIUM"
        | ASCCd.TAPE_POSITION_ERROR_AT_END_OF_MEDIUM -> "TAPE POSITION ERROR AT END-OF-MEDIUM"
        | ASCCd.TARGET_OPERATING_CONDITIONS_HAVE_CHANGED -> "TARGET OPERATING CONDITIONS HAVE CHANGED"
        | ASCCd.THIRD_PARTY_DEVICE_FAILURE -> "THIRD PARTY DEVICE FAILURE"
        | ASCCd.THRESHOLD_CONDITION_MET -> "THRESHOLD CONDITION MET"
        | ASCCd.THRESHOLD_PARAMETERS_NOT_SUPPORTED -> "THRESHOLD PARAMETERS NOT SUPPORTED"
        | ASCCd.TIMEOUT_ON_LOGICAL_UNIT -> "TIMEOUT ON LOGICAL UNIT"
        | ASCCd.TIMESTAMP_CHANGED -> "TIMESTAMP CHANGED"
        | ASCCd.TOO_MANY_SEGMENT_DESCRIPTORS -> "TOO MANY SEGMENT DESCRIPTORS"
        | ASCCd.TOO_MANY_TARGET_DESCRIPTORS -> "TOO MANY TARGET DESCRIPTORS"
        | ASCCd.TOO_MANY_WINDOWS_SPECIFIED -> "TOO MANY WINDOWS SPECIFIED"
        | ASCCd.TOO_MUCH_WRITE_DATA -> "TOO MUCH WRITE DATA"
        | ASCCd.TRACK_FOLLOWING_ERROR -> "TRACK FOLLOWING ERROR"
        | ASCCd.TRACKING_SERVO_FAILURE -> "TRACKING SERVO FAILURE"
        | ASCCd.TRANSCEIVER_MODE_CHANGED_TO_LVD -> "TRANSCEIVER MODE CHANGED TO LVD"
        | ASCCd.TRANSCEIVER_MODE_CHANGED_TO_SINGLE_ENDED -> "TRANSCEIVER MODE CHANGED TO SINGLE-ENDED"
        | ASCCd.UNABLE_TO_ACQUIRE_VIDEO -> "UNABLE TO ACQUIRE VIDEO"
        | ASCCd.UNABLE_TO_RECOVER_TABLE_OF_CONTENTS -> "UNABLE TO RECOVER TABLE-OF-CONTENTS"
        | ASCCd.UNEXPECTED_INEXACT_SEGMENT -> "UNEXPECTED INEXACT SEGMENT"
        | ASCCd.UNLOAD_TAPE_FAILURE -> "UNLOAD TAPE FAILURE"
        | ASCCd.UNREACHABLE_COPY_TARGET -> "UNREACHABLE COPY TARGET"
        | ASCCd.UNRECOVERED_READ_ERROR -> "UNRECOVERED READ ERROR"
        | ASCCd.UNRECOVERED_READ_ERROR_AUTO_REALLOCATE_FAILED -> "UNRECOVERED READ ERROR - AUTO REALLOCATE FAILED"
        | ASCCd.UNRECOVERED_READ_ERROR_RECOMMEND_REASSIGNMENT -> "UNRECOVERED READ ERROR - RECOMMEND REASSIGNMENT"
        | ASCCd.UNRECOVERED_READ_ERROR_RECOMMEND_REWRITE_THE_DATA -> "UNRECOVERED READ ERROR - RECOMMEND REWRITE THE DATA"
        | ASCCd.UNSUCCESSFUL_SOFT_RESET -> "UNSUCCESSFUL SOFT RESET"
        | ASCCd.UNSUPPORTED_ENCLOSURE_FUNCTION -> "UNSUPPORTED ENCLOSURE FUNCTION"
        | ASCCd.UNSUPPORTED_SEGMENT_DESCRIPTOR_TYPE_CODE -> "UNSUPPORTED SEGMENT DESCRIPTOR TYPE CODE"
        | ASCCd.UNSUPPORTED_TARGET_DESCRIPTOR_TYPE_CODE -> "UNSUPPORTED TARGET DESCRIPTOR TYPE CODE"
        | ASCCd.UPDATED_BLOCK_READ -> "UPDATED BLOCK READ"
        | ASCCd.VERIFY_OPERATION_IN_PROGRESS -> "VERIFY OPERATION IN PROGRESS"
        | ASCCd.VIDEO_ACQUISITION_ERROR -> "VIDEO ACQUISITION ERROR"
        | ASCCd.VOLTAGE_FAULT -> "VOLTAGE FAULT"
        | ASCCd.VOLUME_SET_CREATED_OR_MODIFIED -> "VOLUME SET CREATED OR MODIFIED"
        | ASCCd.VOLUME_SET_DEASSIGNED -> "VOLUME SET DEASSIGNED"
        | ASCCd.VOLUME_SET_DELETED -> "VOLUME SET DELETED"
        | ASCCd.VOLUME_SET_REASSIGNED -> "VOLUME SET REASSIGNED"
        | ASCCd.WARNING -> "WARNING"
        | ASCCd.WARNING_ENCLOSURE_DEGRADED -> "WARNING - ENCLOSURE DEGRADED"
        | ASCCd.WARNING_SPECIFIED_TEMPERATURE_EXCEEDED -> "WARNING - SPECIFIED TEMPERATURE EXCEEDED"
        | ASCCd.WORM_MEDIUM_OVERWRITE_ATTEMPTED -> "WORM MEDIUM - OVERWRITE ATTEMPTED"
        | ASCCd.WRITE_APPEND_ERROR -> "WRITE APPEND ERROR"
        | ASCCd.WRITE_APPEND_POSITION_ERROR -> "WRITE APPEND POSITION ERROR"
        | ASCCd.WRITE_ERROR -> "WRITE ERROR"
        | ASCCd.WRITE_ERROR_AUTO_REALLOCATION_FAILED -> "WRITE ERROR - AUTO REALLOCATION FAILED"
        | ASCCd.WRITE_ERROR_LOSS_OF_STREAMING -> "WRITE ERROR - LOSS OF STREAMING"
        | ASCCd.WRITE_ERROR_NOT_ENOUGH_UNSOLICITED_DATA -> "WRITE ERROR - NOT ENOUGH UNSOLICITED DATA"
        | ASCCd.WRITE_ERROR_PADDING_BLOCKS_ADDED -> "WRITE ERROR - PADDING BLOCKS ADDED"
        | ASCCd.WRITE_ERROR_RECOMMEND_REASSIGNMENT -> "WRITE ERROR - RECOMMEND REASSIGNMENT"
        | ASCCd.WRITE_ERROR_RECOVERED_WITH_AUTO_REALLOCATION -> "WRITE ERROR - RECOVERED WITH AUTO REALLOCATION"
        | ASCCd.WRITE_ERROR_RECOVERY_FAILED -> "WRITE ERROR - RECOVERY FAILED"
        | ASCCd.WRITE_ERROR_RECOVERY_NEEDED -> "WRITE ERROR - RECOVERY NEEDED"
        | ASCCd.WRITE_ERROR_UNEXPECTED_UNSOLICITED_DATA -> "WRITE ERROR - UNEXPECTED UNSOLICITED DATA"
        | ASCCd.WRITE_PROTECTED -> "WRITE PROTECTED"
        | ASCCd.ZONED_FORMATTING_FAILED_DUE_TO_SPARE_LINKING -> "ZONED FORMATTING FAILED DUE TO SPARE LINKING"
        | _ as x ->
            let vh = uint16 x &&& 0xFF00us
            let vl = uint16 x &&& 0x00FFus
            if vh = (uint16)ASCCd.DATA_PATH_FAILURE_NN then
                sprintf "DATA PATH FAILURE (NN=0x%02x)" vl
            elif vh = (uint16)ASCCd.DECOMPRESSION_EXCEPTION_SHORT_ALGORITHM_ID_OF_NN then
                sprintf "DECOMPRESSION EXCEPTION SHORT ALGORITHM ID OF NN (NN=0x%02x)" vl
            elif vh = (uint16)ASCCd.DIAGNOSTIC_FAILURE_ON_COMPONENT_NN then
                sprintf "DIAGNOSTIC FAILURE ON COMPONENT NN (NN=0x%02x)" vl
            elif vh = (uint16)ASCCd.POWER_ON_OR_SELF_TEST_FAILURE_NN then
                sprintf "POWER-ON OR SELF-TEST FAILURE (NN=0x%02x)" vl
            elif vh = (uint16)ASCCd.TAGGED_OVERLAPPED_COMMANDS_NN then
                sprintf "TAGGED OVERLAPPED COMMANDS (NN=0x%02x)" vl
            else
                sprintf "Unknown ASC or ASCQ value. ASC=0x%02x, ASCQ=0x%02x" ( vh >>> 8 ) vl


