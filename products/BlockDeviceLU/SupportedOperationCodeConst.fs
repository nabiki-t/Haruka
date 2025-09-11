//=============================================================================
// Haruka Software Storage.
// SupportedOperationCodeConst.fs : Defines constant values used in REPORT SUPPORTED OPERATION CODES command.

//=============================================================================
// Namespace declaration

namespace Haruka.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

/// SupportedOperationCodeConst class has constatnt values used in REPORT SUPPORTED OPERATION CODES command.
type SupportedOperationCodeConst() =

    /// Supported OPERATION CODE and SERVICE ACTION list value,
    /// that is used when REPORTING OPTIONS value is 000b.
    static member SupportedAllOperationCommands : byte[] =
        [|
            yield! Functions.Int32ToNetworkBytes_NewVec 312;
            yield! [|
                0x12uy;         // INQUIRY
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x06uy; // CDB LENGTH
            |]
            yield! [|
                0x15uy;         // MODE SELECT(6)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x06uy; // CDB LENGTH
            |]
            yield! [|
                0x55uy;         // MODE SELECT(10)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x1Auy;         // MODE SENSE(6)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x06uy; // CDB LENGTH
            |]
            yield! [|
                0x5Auy;         // MODE SENSE(10)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x5Euy;         // PERSISTENT RESERVE IN
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action(READ KEYS)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x5Euy;         // PERSISTENT RESERVE IN
                0x00uy;         // Reserved
                0x00uy; 0x01uy; // Service Action(READ KEYS)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x5Euy;         // PERSISTENT RESERVE IN
                0x00uy;         // Reserved
                0x00uy; 0x02uy; // Service Action(REPORT CAPABILITIES)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x5Euy;         // PERSISTENT RESERVE IN
                0x00uy;         // Reserved
                0x00uy; 0x03uy; // Service Action(READ FULL STATUS)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x5Fuy;         // PERSISTENT RESERVE OUT
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action(REGISTER)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x5Fuy;         // PERSISTENT RESERVE OUT
                0x00uy;         // Reserved
                0x00uy; 0x01uy; // Service Action(RESERVE)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x5Fuy;         // PERSISTENT RESERVE OUT
                0x00uy;         // Reserved
                0x00uy; 0x02uy; // Service Action(RELEASE)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x5Fuy;         // PERSISTENT RESERVE OUT
                0x00uy;         // Reserved
                0x00uy; 0x03uy; // Service Action(CLEAR)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x5Fuy;         // PERSISTENT RESERVE OUT
                0x00uy;         // Reserved
                0x00uy; 0x04uy; // Service Action(PREEMPT)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x5Fuy;         // PERSISTENT RESERVE OUT
                0x00uy;         // Reserved
                0x00uy; 0x05uy; // Service Action(PREEMPT AND ABORT)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x5Fuy;         // PERSISTENT RESERVE OUT
                0x00uy;         // Reserved
                0x00uy; 0x06uy; // Service Action(REGISTER AND IGNORE EXISTING KEY)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x5Fuy;         // PERSISTENT RESERVE OUT
                0x00uy;         // Reserved
                0x00uy; 0x07uy; // Service Action(REGISTER AND MOVE)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x34uy;         // PRE-FETCH(10)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x90uy;         // PRE-FETCH(16)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x10uy; // CDB LENGTH
            |]
            yield! [|
                0xA0uy;         // REPORT LUNS
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x0Cuy; // CDB LENGTH
            |]
            yield! [|
                0xA3uy;         // REPORT SUPPORTED OPERATION CODES
                0x00uy;         // Reserved
                0x00uy; 0x0Cuy; // Service Action(0C)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Cuy; // CDB LENGTH
            |]
            yield! [|
                0xA3uy;         // REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS
                0x00uy;         // Reserved
                0x00uy; 0x0Duy; // Service Action(0C)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x0Cuy; // CDB LENGTH
            |]
            yield! [|
                0x03uy;         // REQUEST SENSE
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x06uy; // CDB LENGTH
            |]
            yield! [|
                0x00uy;         // TEST UNIT READY
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x06uy; // CDB LENGTH
            |]
            yield! [|
                0x04uy;         // FORMAT UNIT
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x06uy; // CDB LENGTH
            |]
            yield! [|
                0x08uy;         // READ(6)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x06uy; // CDB LENGTH
            |]
            yield! [|
                0x28uy;         // READ(10)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0xA8uy;         // READ(12)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x0Cuy; // CDB LENGTH
            |]
            yield! [|
                0x88uy;         // READ(16)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x10uy; // CDB LENGTH
            |]
            yield! [|
                0x7Fuy;         // READ(32)
                0x00uy;         // Reserved
                0x00uy; 0x09uy; // Service Action(0009h)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x20uy; // CDB LENGTH
            |]
            yield! [|
                0x25uy;         // READ CAPACITY(10)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x9Euy;         // READ CAPACITY(16)
                0x00uy;         // Reserved
                0x00uy; 0x10uy; // Service Action(10h)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x10uy; // CDB LENGTH
            |]
            yield! [|
                0x35uy;         // SYNCHRONIZE CACHE(10)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0x91uy;         // SYNCHRONIZE CACHE(16)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action(00h)
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x10uy; // CDB LENGTH
            |]
            yield! [|
                0x0Auy;         // WRITE(6)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x06uy; // CDB LENGTH
            |]
            yield! [|
                0x2Auy;         // WRITE(10)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x0Auy; // CDB LENGTH
            |]
            yield! [|
                0xAAuy;         // WRITE(12)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x0Cuy; // CDB LENGTH
            |]
            yield! [|
                0x8Auy;         // WRITE(16)
                0x00uy;         // Reserved
                0x00uy; 0x00uy; // Service Action
                0x00uy; 0x00uy; // Reserved, SERVACTV
                0x00uy; 0x10uy; // CDB LENGTH
            |]
            yield! [|
                0x7Fuy;         // WRITE(32)
                0x00uy;         // Reserved
                0x00uy; 0x0Buy; // Service Action(000Bh)
                0x00uy; 0x01uy; // Reserved, SERVACTV
                0x00uy; 0x20uy; // CDB LENGTH
            |]
        |]

    /// one_command parameter data for INQUIRY CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_INQUIRY : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x06uy; // CDB SIZE
            0x12uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x01uy;         // EVPD
            0xFFuy;         // PAGE CODE
            0xFFuy; 0xFFuy; // ALLOCATION LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for MODE SELECT(6) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_MODE_SELECT_6 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x06uy; // CDB SIZE
            0x15uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x10uy;         // PF, SP
            0x00uy; 0x00uy; // Reserved
            0xFFuy;         // PARAMETER LIST LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for MODE SELECT(10) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_MODE_SELECT_10 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Auy; // CDB SIZE
            0x55uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x10uy;         // PF, SP
            0x00uy; 0x00uy; // Reserved
            0x00uy; 0x00uy; // Reserved
            0x00uy;         // Reserved
            0xFFuy; 0xFFuy; // PARAMETER LIST LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for MODE SENSE(6) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_MODE_SENSE_6 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x06uy; // CDB SIZE
            0x1Auy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x08uy;         // DBD
            0xFFuy;         // PC, PAGE CODE
            0xFFuy;         // SUBPAGE CODE
            0xFFuy;         // ALLOCATION LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for MODE SENSE(10) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_MODE_SENSE_10 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Auy; // CDB SIZE
            0x5Auy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x18uy;         // LLBAA, DBD
            0xFFuy;         // PC, PAGE CODE
            0xFFuy;         // SUBPAGE CODE
            0x00uy; 0x00uy; // Reserved
            0x00uy;         // Reserved
            0xFFuy; 0xFFuy; // ALLOCATION LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for REPORT LUNS CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_REPORT_LUNS : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Cuy; // CDB SIZE
            0xA0uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy;         // Reserved
            0xFFuy;         // SELECT REPORT
            0x00uy; 0x00uy; // Reserved
            0x00uy;         // Reserved
            0xFFuy; 0xFFuy; // ALLOCATION LENGTH
            0xFFuy; 0xFFuy; // ALLOCATION LENGTH
            0x00uy;         // Reserved
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for REQUEST SENSE CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_REQUEST_SENSE : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x06uy; // CDB SIZE
            0x03uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x01uy;         // DESC
            0x00uy; 0x00uy; // Reserved
            0xFFuy;         // ALLOCATION LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for TEST UNIT READY CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_TEST_UNIT_READY : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x06uy; // CDB SIZE
            0x00uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy; 0x00uy; // Reserved
            0x00uy; 0x00uy; // Reserved
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for FORMAT UNIT CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_FORMAT_UNIT : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x06uy; // CDB SIZE
            0x04uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy;         // FMTPINFO, RTO_REQ, LONGLIST, FMTDATA, CMPLIST,  DEFECT LIST FORMAT
            0x00uy; 0x00uy; // Vendor specific, obsoluted
            0x00uy;         // Reserved
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for PRE-FETCH(10) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_PRE_FETCH_10 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Auy; // CDB SIZE
            0x34uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy;         // IMMED
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0x00uy;         // GROUP NUMBER
            0xFFuy; 0xFFuy; // PREFETCH LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for PRE-FETCH(16) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_PRE_FETCH_16 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Fuy; // CDB SIZE
            0x90uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy;         // IMMED
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // PREFETCH LENGTH
            0xFFuy; 0xFFuy; // PREFETCH LENGTH
            0x00uy;         // GROUP NUMBER
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for READ(6) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_READ_6 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x06uy; // CDB SIZE
            0x08uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x1Fuy;         // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy;         // TRANSFER LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for READ(10) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_READ_10 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Auy; // CDB SIZE
            0x28uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy;         // RDPROTECT, DPO, FUA, FUA_NV
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0x00uy;         // GROUP NUMBER
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for READ(12) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_READ_12 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Cuy; // CDB SIZE
            0xA8uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy;         // RDPROTECT, DPO, FUA, FUA_NV
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
            0x00uy;         // GROUP NUMBER
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for READ(16) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_READ_16 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x10uy; // CDB SIZE
            0x88uy;         // OPERATION CODE  ( following is in  CDB USAGE DATA field )
            0x00uy;         // RDPROTECT, DPO, FUA, FUA_NV
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
            0x00uy;         // GROUP NUMBER
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for READ(32) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_READ_32 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x20uy; // CDB SIZE
            0x7Fuy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x04uy;         // CONTROL
            0x00uy; 0x00uy; // Reserved
            0x00uy; 0x00uy; // Reserved
            0x00uy;         // GROUP NUMBER
            0xFFuy;         // ADDITIONAL CDB LENGTH
            0x00uy; 0x09uy; // SERVICE ACTION
            0x00uy;         // RDPROTECT, DPO, FUA, FUA_NV
            0x00uy;         // Reserved
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0x00uy; 0x00uy; // EXPECTED INITIAL LOGICAL BLOCK REFERENCE TAG
            0x00uy; 0x00uy; // EXPECTED INITIAL LOGICAL BLOCK REFERENCE TAG
            0x00uy; 0x00uy; // EXPECTED LOGICAL BLOCK APPLICATION TAG
            0x00uy; 0x00uy; // LOGICAL BLOCK APPLICATION TAG MASK
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
        |]

    /// one_command parameter data for READ CAPACITY(10) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_READ_CAPACITY_10 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Auy; // CDB SIZE
            0x25uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy;         // Reserved
            0x00uy; 0x00uy; // LOGICAL BLOCK ADDRESS
            0x00uy; 0x00uy; // LOGICAL BLOCK ADDRESS
            0x00uy; 0x00uy; // Reserved
            0x00uy;         // PMI
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for READ CAPACITY(16) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_READ_CAPACITY_16 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x10uy; // CDB SIZE
            0x9Euy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x10uy;         // SERVICE ACTION
            0x00uy; 0x00uy; // LOGICAL BLOCK ADDRESS
            0x00uy; 0x00uy; // LOGICAL BLOCK ADDRESS
            0x00uy; 0x00uy; // LOGICAL BLOCK ADDRESS
            0x00uy; 0x00uy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // ALLOCATION LENGTH
            0xFFuy; 0xFFuy; // ALLOCATION LENGTH
            0x00uy;         // PMI
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for SYNCHRONIZE CACHE(10) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_SYNCHRONIZE_CACHE_10 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Auy; // CDB SIZE
            0x35uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy;         // SYNC_NV, IMMED
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0x00uy;         // GROUP NUMBER
            0xFFuy; 0xFFuy; // NUMBER OF BLOCKS
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for SYNCHRONIZE CACHE(16) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_SYNCHRONIZE_CACHE_16 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Auy; // CDB SIZE
            0x91uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy;         // SYNC_NV, IMMED
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // NUMBER OF BLOCKS
            0xFFuy; 0xFFuy; // NUMBER OF BLOCKS
            0x00uy;         // GROUP NUMBER
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for WRITE(6) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_WRITE_6 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x06uy; // CDB SIZE
            0x0Auy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x1Fuy;         // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy;         // TRANSFER LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for WRITE(10) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_WRITE_10 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Auy; // CDB SIZE
            0x2Auy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy;         // WRPROTECT, DPO, FUA, FUA_NV
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0x00uy;         // GROUP NUMBER
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for WRITE(12) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_WRITE_12 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Cuy; // CDB SIZE
            0xAAuy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy;         // WRPROTECT, DPO, FUA, FUA_NV
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
            0x00uy;         // GROUP NUMBER
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for WRITE(16) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_WRITE_16 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x10uy; // CDB SIZE
            0x8Auy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x00uy;         // WRPROTECT, DPO, FUA, FUA_NV
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
            0x00uy;         // GROUP NUMBER
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for WRITE(32) CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_WRITE_32 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x20uy; // CDB SIZE
            0x7Fuy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x04uy;         // CONTROL
            0x00uy; 0x00uy; // Reserved
            0x00uy; 0x00uy; // Reserved
            0x00uy;         // GROUP NUMBER
            0xFFuy;         // ADDITIONAL CDB LENGTH
            0x00uy; 0x0Buy; // SERVICE ACTION
            0x00uy;         // WRPROTECT, DPO, FUA, FUA_NV
            0x00uy;         // Reserved
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0x00uy; 0x00uy; // EXPECTED INITIAL LOGICAL BLOCK REFERENCE TAG
            0x00uy; 0x00uy; // EXPECTED INITIAL LOGICAL BLOCK REFERENCE TAG
            0x00uy; 0x00uy; // EXPECTED LOGICAL BLOCK APPLICATION TAG
            0x00uy; 0x00uy; // LOGICAL BLOCK APPLICATION TAG MASK
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
            0xFFuy; 0xFFuy; // TRANSFER LENGTH
        |]
(*
    /// one_command parameter data for WRITE SAME(10).
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_WRITE_SAME_10 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Auy; // CDB SIZE
            0x41uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0xE6uy;         // WRPROTECT, LBDATA
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0x00uy;         // GROUP NUMBER 
            0xFFuy; 0xFFuy; // NUMBER OF BLOCKES
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for WRITE SAME(16).
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_WRITE_SAME_16 : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x10uy; // CDB SIZE
            0x93uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0xE6uy;         // WRPROTECT, LBDATA
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // LOGICAL BLOCK ADDRESS
            0xFFuy; 0xFFuy; // NUMBER OF BLOCKES
            0xFFuy; 0xFFuy; // NUMBER OF BLOCKES
            0x00uy;         // GROUP NUMBER
            0x04uy;         // CONTROL
        |]
*)
    /// <summary>
    ///  one_command parameter data for PERSISTENT RESERVE IN CDB.
    ///  That is used when REPORTING OPTIONS value is 001b or 010b.
    /// </summary>
    /// <param name="sa">
    ///  SERVICE ACTION value.
    /// </param>
    static member CdbUsageData_PERSISTENT_RESERVE_IN ( sa : byte ) : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Auy; // CDB SIZE
            0x5Euy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            sa;             // SERVICE ACTION
            0x00uy; 0x00uy; // Reserved
            0x00uy; 0x00uy; // Reserved
            0x00uy;         // Reserved
            0xFFuy; 0xFFuy; //  ALLOCATION LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for PERSISTENT RESERVE OUT CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_PERSISTENT_RESERVE_OUT ( sa : byte ) : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Auy; // CDB SIZE
            0x5Fuy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            sa;             // SERVICE ACTION
            0xFFuy; 0xFFuy; // SCOPE, TYPE
            0x00uy; 0x00uy; // Reserved
            0xFFuy; 0xFFuy; // PARAMETER LIST LENGTH
            0xFFuy; 0xFFuy; // PARAMETER LIST LENGTH
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for REPORT SUPPORTED OPERATION CODES CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_REPORT_SUPPORTED_OPERATION_CODES : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Cuy; // CDB SIZE
            0xA3uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x0Cuy;         // SERVICE ACTION
            0x07uy;         // REPORTING OPTIONS
            0xFFuy;         // REQUESTED OPERATION CODE
            0xFFuy; 0xFFuy; // REQUESTED SERVICE ACTION
            0xFFuy; 0xFFuy; // ALLOCATION LENGTH
            0xFFuy; 0xFFuy; // ALLOCATION LENGTH
            0x00uy;         // Reserved
            0x04uy;         // CONTROL
        |]

    /// one_command parameter data for REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS CDB.
    /// That is used when REPORTING OPTIONS value is 001b or 010b.
    static member CdbUsageData_REPORT_SUPPORTED_TASK_MANAGEMENT_FUNCTIONS : byte[] =
        [|
            0x00uy;         // Reserved
            0x03uy;         // SUPPORT
            0x00uy; 0x0Cuy; // CDB SIZE
            0xA3uy;         // OPERATION CODE ( following is in  CDB USAGE DATA field )
            0x0Duy;         // SERVICE ACTION
            0x00uy; 0x00uy; // Reserved
            0x00uy; 0x00uy; // Reserved
            0xFFuy; 0xFFuy; // ALLOCATION LENGTH
            0xFFuy; 0xFFuy; // ALLOCATION LENGTH
            0x00uy;         // Reserved
            0x04uy;         // CONTROL
        |]


