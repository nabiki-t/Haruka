//=============================================================================
// Haruka Software Storage.
// ConvertToCDB.fs : Definition of the ConvertScsiCommandPDUToCDB function that
// converts the iSCSI SCSICommandPCD receiving from the initiator to 
// the SCSI CDB.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.BlockDeviceLU

//=============================================================================
// Import declaration

open System

open Haruka.Constants
open Haruka.Commons
open Haruka.BlockDeviceLU

//=============================================================================
// Type definition

/// <summary>
///   This class defines ConvertScsiCommandPDUToCDB function.
/// </summary>
type ConvertToCDB() =

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to InquiryCDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted InquiryCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToInquiryCDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        assert( command.ScsiCDB.Length >= 6 )
        assert( command.ScsiCDB.[0] = 0x12uy )
        let r : InquiryCDB = {
            OperationCode = command.ScsiCDB.[0];
            EVPD = Functions.CheckBitflag command.ScsiCDB.[1] 0x01uy
            PageCode = command.ScsiCDB.[2]
            AllocationLength = Functions.NetworkBytesToUInt16 command.ScsiCDB 3
            Control = command.ScsiCDB.[5]
        }
        if r.EVPD = false && r.PageCode <> 0uy then
            let errmsg = "EVPD bit in CDB is 0, but PageCode is not 0(PageCode=" + ( string r.PageCode ) + ")."
            let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 1uy; FieldPointer = 1us },
                errmsg
            )
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to ModeSelect6CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ModeSelectCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToModeSelect6CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        assert( command.ScsiCDB.Length >= 6 )
        assert( command.ScsiCDB.[0] = 0x15uy )
        let r : ModeSelectCDB = {
            OperationCode = command.ScsiCDB.[0];
            PF = Functions.CheckBitflag command.ScsiCDB.[1] 0x10uy;
            SP = Functions.CheckBitflag command.ScsiCDB.[1] 0x01uy;
            ParameterListLength = uint16 command.ScsiCDB.[4];
            Control = command.ScsiCDB.[5];
        }
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to ModeSelect10CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ModeSelectCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToModeSelect10CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        if command.ScsiCDB.Length < 10 then
            let errmsg = "CDB length in MODE SELECT(10) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x55uy )
        let r : ModeSelectCDB = {
            OperationCode = command.ScsiCDB.[0];
            PF = Functions.CheckBitflag command.ScsiCDB.[1] 0x10uy;
            SP = Functions.CheckBitflag command.ScsiCDB.[1] 0x01uy;
            ParameterListLength = Functions.NetworkBytesToUInt16 command.ScsiCDB 7
            Control = command.ScsiCDB.[9];
        }
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to ModeSense6CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ModeSenseCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToModeSense6CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        assert( command.ScsiCDB.Length >= 6 )
        assert( command.ScsiCDB.[0] = 0x1Auy )
        let r : ModeSenseCDB = {
            OperationCode = command.ScsiCDB.[0];
            LLBAA = false;
            DBD = Functions.CheckBitflag command.ScsiCDB.[1] 0x08uy;
            PC = ( command.ScsiCDB.[2] &&& 0xC0uy ) >>> 6;
            PageCode = command.ScsiCDB.[2] &&& 0x3Fuy;
            SubPageCode = command.ScsiCDB.[3];
            AllocationLength = uint16 command.ScsiCDB.[4];
            Control = command.ScsiCDB.[5];
        }
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to ModeSense10CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ModeSenseCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToModeSense10CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        if command.ScsiCDB.Length < 10 then
            let errmsg = "CDB length in MODE SENSE(10) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x5Auy )
        let r : ModeSenseCDB = {
            OperationCode = command.ScsiCDB.[0];
            LLBAA = Functions.CheckBitflag command.ScsiCDB.[1] 0x10uy;
            DBD = Functions.CheckBitflag command.ScsiCDB.[1] 0x08uy;
            PC = ( command.ScsiCDB.[2] &&& 0xC0uy ) >>> 6;
            PageCode = command.ScsiCDB.[2] &&& 0x3Fuy;
            SubPageCode = command.ScsiCDB.[3];
            AllocationLength = Functions.NetworkBytesToUInt16 command.ScsiCDB 7
            Control = command.ScsiCDB.[9];
        }
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to PersistentReserveInCDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted PersistentReserveInCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToPersistentReserveInCDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
        if command.ScsiCDB.Length < 10 then
            let errmsg = "CDB length in PERSISTENT RESERVE IN CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x5Euy )
        let r : PersistentReserveInCDB = {
            OperationCode = command.ScsiCDB.[0];
            ServiceAction = command.ScsiCDB.[1] &&& 0x1Fuy;
            AllocationLength = Functions.NetworkBytesToUInt16 command.ScsiCDB 7
            Control = command.ScsiCDB.[9];
        }
        if ( r.ServiceAction <> 0x00uy && r.ServiceAction <> 0x01uy && r.ServiceAction <> 0x02uy && r.ServiceAction <> 0x03uy ) then
            let errmsg = sprintf "In PERSISTENT RESERVE IN CDB, invalid SERVICE ACTION value(0x%02X). " r.ServiceAction
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 4uy; FieldPointer = 1us },
                errmsg
            )

        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to PersistentReserveOutCDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted PersistentReserveOutCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToPersistentReserveOutCDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
        if command.ScsiCDB.Length < 10 then
            let errmsg = "CDB length in PERSISTENT RESERVE OUT CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x5Fuy )
        let wSA = command.ScsiCDB.[1] &&& 0x1Fuy
        let r : PersistentReserveOutCDB = {
            OperationCode = command.ScsiCDB.[0];
            ServiceAction = wSA;
            Scope = ( command.ScsiCDB.[2] &&& 0xF0uy ) >>> 4;
            PRType =
                if wSA = 0x01uy || wSA = 0x02uy || wSA = 0x04uy || wSA = 0x05uy then
                    // Type field is enabled when service action is RESERVE, RELEASE, PREEMPT or PREEMPT AND ABORT.
                    match command.ScsiCDB.[2] &&& 0x0Fuy with
                    | 0x01uy ->
                        PR_TYPE.WRITE_EXCLUSIVE
                    | 0x03uy ->
                        PR_TYPE.EXCLUSIVE_ACCESS
                    | 0x05uy ->
                        PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY
                    | 0x06uy ->
                        PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
                    | 0x07uy ->
                        PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS
                    | 0x08uy ->
                        PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS
                    | _ as r ->
                        let errmsg = sprintf "In PERSISTENT RESERVE OUT CDB, invalid TYPE value(0x%02X)." r
                        HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                        raise <| SCSIACAException (
                            source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                            { CommandData = true; BPV = true; BitPointer = 3uy; FieldPointer = 2us },
                            errmsg
                        )
                else
                    // Type field must be ignored when service action is REGISTER, CLEAR, REGISTER AND IGNORE EXISTING KEY or REGISTER AND MOVE.
                    PR_TYPE.NO_RESERVATION
            ParameterListLength = Functions.NetworkBytesToUInt32 command.ScsiCDB 5
            Control = command.ScsiCDB.[9];
        }
        if ( r.ServiceAction >= 0x08uy ) then
            let errmsg = sprintf "In PERSISTENT RESERVE OUT CDB, invalid SERVICE ACTION value(0x%02X)." r.ServiceAction
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 4uy; FieldPointer = 1us },
                errmsg
            )

        if ( r.Scope <> 0x00uy ) then
            let errmsg = sprintf "In PERSISTENT RESERVE OUT CDB, invalid SCOPE value(0x%02X)." r.Scope
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 2us },
                errmsg
            )

        r :> ICDB


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to PRE-FETCH(10) data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted PreFetchCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToPreFetch10CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
        if command.ScsiCDB.Length < 10 then
            let errmsg = "CDB length in PRE-FETCH(10) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x34uy )
        let r : PreFetchCDB = {
            OperationCode = command.ScsiCDB.[0];
            IMMED = Functions.CheckBitflag command.ScsiCDB.[1] 0x02uy;
            LogicalBlockAddress = uint64( Functions.NetworkBytesToUInt32 command.ScsiCDB 2 );
            GroupNumber = command.ScsiCDB.[6] &&& 0x1Fuy;
            PrefetchLength = uint32( Functions.NetworkBytesToUInt16 command.ScsiCDB 7 );
            Control = command.ScsiCDB.[9];
        }

        r :> ICDB


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to PRE-FETCH(16) data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted PreFetchCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToPreFetch16CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
        if command.ScsiCDB.Length < 16 then
            let errmsg = "CDB length in PRE-FETCH(16) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x90uy )
        let r : PreFetchCDB = {
            OperationCode = command.ScsiCDB.[0];
            IMMED = Functions.CheckBitflag command.ScsiCDB.[1] 0x02uy;
            LogicalBlockAddress = Functions.NetworkBytesToUInt64 command.ScsiCDB 2;
            PrefetchLength = Functions.NetworkBytesToUInt32 command.ScsiCDB 10;
            GroupNumber = command.ScsiCDB.[14] &&& 0x1Fuy;
            Control = command.ScsiCDB.[15];
        }

        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to ReportLUNsCDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ReportLUNsCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToReportLUNsCDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
        if command.ScsiCDB.Length < 12 then
            let errmsg = "CDB length in REPORT LUNs CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0xA0uy )
        let r : ReportLUNsCDB = {
            OperationCode = command.ScsiCDB.[0];
            SelectReport = command.ScsiCDB.[2];
            AllocationLength = Functions.NetworkBytesToUInt32 command.ScsiCDB 6
            Control = command.ScsiCDB.[11];
        }
        if ( r.SelectReport <> 0x00uy && r.SelectReport <> 0x01uy && r.SelectReport <> 0x02uy ) then
            let errmsg = sprintf "In REPORT LUNs CDB, invalid SELECT REPORT value(0x%02X)." r.SelectReport
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 2us },
                errmsg
            )

        if ( r.AllocationLength < 16u ) then
            let errmsg = sprintf "In REPORT LUNs CDB, ALLOCATION LENGTH value(%d) is too short." r.AllocationLength
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 6us },
                errmsg
            )

        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to RequestSenseCDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted RequestSenseCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToRequestSenseCDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        assert( command.ScsiCDB.Length >= 6 )
        assert( command.ScsiCDB.[0] = 0x03uy )
        let r : RequestSenseCDB = {
            OperationCode = command.ScsiCDB.[0];
            DESC = Functions.CheckBitflag command.ScsiCDB.[1] 0x01uy;
            AllocationLength = command.ScsiCDB.[4];
            Control = command.ScsiCDB.[5];
        }
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to TestUnitReadyCDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted TestUnitReadyCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToTestUnitReadyCDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        assert( command.ScsiCDB.Length >= 6 )
        assert( command.ScsiCDB.[0] = 0x00uy )
        let r : TestUnitReadyCDB = {
            OperationCode = command.ScsiCDB.[0];
            Control = command.ScsiCDB.[5];
        }
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to FormatUnitCDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted FormatUnitCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToFormatUnitCDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        assert( command.ScsiCDB.Length >= 6 )
        assert( command.ScsiCDB.[0] = 0x04uy )
        let r : FormatUnitCDB = {
            OperationCode = command.ScsiCDB.[0];
            FMTPINFO = Functions.CheckBitflag command.ScsiCDB.[1] 0x80uy;
            RTO_REQ = Functions.CheckBitflag command.ScsiCDB.[1] 0x40uy;
            LONGLIST = Functions.CheckBitflag command.ScsiCDB.[1] 0x20uy;
            FMTDATA = Functions.CheckBitflag command.ScsiCDB.[1] 0x10uy;
            CMPLIST = Functions.CheckBitflag command.ScsiCDB.[1] 0x08uy;
            DefectListFormat = command.ScsiCDB.[1] &&& 0x07uy;
            Control = command.ScsiCDB.[5];
        }
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to Read6CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ReadCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToRead6CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        assert( command.ScsiCDB.Length >= 6 )
        assert( command.ScsiCDB.[0] = 0x08uy )
        let r : ReadCDB = {
            OperationCode = command.ScsiCDB.[0];
            RdProtect = 0uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = ( uint64( command.ScsiCDB.[1] &&& 0x1Fuy ) <<< 16 ) ||| ( uint64( command.ScsiCDB.[2] ) <<< 8 ) ||| uint64 command.ScsiCDB.[3];
            TransferLength = if command.ScsiCDB.[4] = 0uy then 256u else uint32 command.ScsiCDB.[4];
            GroupNumber = 0uy;
            Control = command.ScsiCDB.[5];
        }
        r :> ICDB
    
    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to Read10CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ReadCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToRead10CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
        if command.ScsiCDB.Length < 10 then
            let errmsg = sprintf "CDB length in READ(10) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x28uy )
        let r : ReadCDB = {
            OperationCode = command.ScsiCDB.[0];
            RdProtect = ( command.ScsiCDB.[1] &&& 0xE0uy ) >>> 5;
            DPO = Functions.CheckBitflag command.ScsiCDB.[1] 0x10uy;
            FUA = Functions.CheckBitflag command.ScsiCDB.[1] 0x08uy;
            FUA_NV = Functions.CheckBitflag command.ScsiCDB.[1] 0x02uy;
            LogicalBlockAddress = uint64 ( Functions.NetworkBytesToUInt32 command.ScsiCDB 2 );
            GroupNumber = command.ScsiCDB.[6] &&& 0x1Fuy;
            TransferLength = uint32( Functions.NetworkBytesToUInt16 command.ScsiCDB 7 ) ;
            Control = command.ScsiCDB.[9];
        }
        if r.RdProtect > 5uy then
            let errmsg = sprintf "In READ(10) CDB, invalid RDPROTECT value(0x%02X)." r.RdProtect
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 1us },
                errmsg
            )

        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to Read12CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ReadCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToRead12CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )

        if command.ScsiCDB.Length < 12 then
            let errmsg = sprintf "CDB length in READ(12) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0xA8uy )
        let r : ReadCDB = {
            OperationCode = command.ScsiCDB.[0];
            RdProtect = ( command.ScsiCDB.[1] &&& 0xE0uy ) >>> 5;
            DPO = Functions.CheckBitflag command.ScsiCDB.[1] 0x10uy;
            FUA = Functions.CheckBitflag command.ScsiCDB.[1] 0x08uy;
            FUA_NV = Functions.CheckBitflag command.ScsiCDB.[1] 0x02uy;
            LogicalBlockAddress = uint64( Functions.NetworkBytesToUInt32 command.ScsiCDB 2 );
            TransferLength = Functions.NetworkBytesToUInt32 command.ScsiCDB 6;
            GroupNumber = command.ScsiCDB.[10] &&& 0x1Fuy;
            Control = command.ScsiCDB.[11];
        }
        if r.RdProtect > 5uy then
            let errmsg = sprintf " In READ(12) CDB, invalid RDPROTECT value(0x%02X)." r.RdProtect
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 1us },
                errmsg
            )

        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to Read16CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ReadCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToRead16CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
        if command.ScsiCDB.Length < 16 then
            let errmsg = sprintf "CDB length in READ(16) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x88uy )
        let r : ReadCDB = {
            OperationCode = command.ScsiCDB.[0];
            RdProtect = ( command.ScsiCDB.[1] &&& 0xE0uy ) >>> 5;
            DPO = Functions.CheckBitflag command.ScsiCDB.[1] 0x10uy;
            FUA = Functions.CheckBitflag command.ScsiCDB.[1] 0x08uy;
            FUA_NV = Functions.CheckBitflag command.ScsiCDB.[1] 0x02uy;
            LogicalBlockAddress = Functions.NetworkBytesToUInt64 command.ScsiCDB 2;
            TransferLength = Functions.NetworkBytesToUInt32 command.ScsiCDB 10
            GroupNumber = command.ScsiCDB.[14] &&& 0x1Fuy;
            Control = command.ScsiCDB.[15];
        }
        if r.RdProtect > 5uy then
            let errmsg = sprintf " In READ(16) CDB, invalid RDPROTECT value(0x%02X)." r.RdProtect
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 1us },
                errmsg
            )

        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to ReadCapacity10CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ReadCapacityCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToReadCapacity10CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        if command.ScsiCDB.Length < 10 then
            let errmsg = sprintf "CDB length in READ CAPACITY(10) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x25uy )
        let r : ReadCapacityCDB = {
            OperationCode = command.ScsiCDB.[0];
            ServiceAction = 0uy;
            LogicalBlockAddress = uint64( Functions.NetworkBytesToUInt32 command.ScsiCDB 2 );
            PMI = Functions.CheckBitflag command.ScsiCDB.[8] 0x01uy;
            AllocationLength = 8u;
            Control = command.ScsiCDB.[9];
        }
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to SynchronizeCacheCDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ReadCapacityCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToSynchronizeCache10CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        if command.ScsiCDB.Length < 10 then
            let errmsg = sprintf "CDB length in SYNCHRONIZE CACHE(10) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x35uy )
        let r : SynchronizeCacheCDB = {
            OperationCode = command.ScsiCDB.[0];
            SyncNV = Functions.CheckBitflag command.ScsiCDB.[1] 0x04uy;
            IMMED = Functions.CheckBitflag command.ScsiCDB.[1] 0x02uy;
            LogicalBlockAddress = uint64( Functions.NetworkBytesToUInt32 command.ScsiCDB 2 );
            NumberOfBlocks = uint32( Functions.NetworkBytesToUInt16 command.ScsiCDB 7 );
            GroupNumber = command.ScsiCDB.[6] &&& 0x1Fuy;
            Control = command.ScsiCDB.[9];
        }
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to SynchronizeCacheCDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ReadCapacityCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToSynchronizeCache16CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        if command.ScsiCDB.Length < 16 then
            let errmsg = sprintf "CDB length in SYNCHRONIZE CACHE(16) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x91uy )
        let r : SynchronizeCacheCDB = {
            OperationCode = command.ScsiCDB.[0];
            SyncNV = Functions.CheckBitflag command.ScsiCDB.[1] 0x04uy;
            IMMED = Functions.CheckBitflag command.ScsiCDB.[1] 0x02uy;
            LogicalBlockAddress = uint64( Functions.NetworkBytesToUInt64 command.ScsiCDB 2 );
            NumberOfBlocks = uint32( Functions.NetworkBytesToUInt32 command.ScsiCDB 10 );
            GroupNumber = command.ScsiCDB.[14] &&& 0x1Fuy;
            Control = command.ScsiCDB.[15];
        }
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to Write6CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted WriteCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToWrite6CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        assert( command.ScsiCDB.Length >= 6 )
        assert( command.ScsiCDB.[0] = 0x0Auy )
        let r : WriteCDB = {
            OperationCode = command.ScsiCDB.[0];
            WRPROTECT = 0uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = uint64( command.ScsiCDB.[1] &&& 0x1Fuy ) <<< 16 ||| ( uint64( command.ScsiCDB.[2] ) <<< 8 ) ||| uint64( command.ScsiCDB.[3] );
            GroupNumber = 0uy;
            TransferLength = if command.ScsiCDB.[4] = 0uy then 256u else uint32 command.ScsiCDB.[4];
            Control = command.ScsiCDB.[5];
        }
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to Write10CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted WriteCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToWrite10CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
        if command.ScsiCDB.Length < 10 then
            let errmsg = sprintf "CDB length in WRITE(10) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x2Auy )
        let r : WriteCDB = {
            OperationCode = command.ScsiCDB.[0];
            WRPROTECT = ( command.ScsiCDB.[1] &&& 0xE0uy ) >>> 5;
            DPO = Functions.CheckBitflag command.ScsiCDB.[1] 0x10uy;
            FUA = Functions.CheckBitflag command.ScsiCDB.[1] 0x08uy;
            FUA_NV = Functions.CheckBitflag command.ScsiCDB.[1] 0x02uy;
            LogicalBlockAddress = uint64( Functions.NetworkBytesToUInt32 command.ScsiCDB 2 );
            GroupNumber = command.ScsiCDB.[6] &&& 0x1Fuy;
            TransferLength = uint32( Functions.NetworkBytesToUInt16 command.ScsiCDB 7 );
            Control = command.ScsiCDB.[9];
        }
        if r.WRPROTECT >= 5uy then
            let errmsg = sprintf "In WRITE(10) CDB, invalid WRPROTECT value(0x%02X)." r.WRPROTECT
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 1us },
                errmsg
            )

        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to Write12CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted WriteCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToWrite12CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
        if command.ScsiCDB.Length < 12 then
            let errmsg = sprintf "CDB length in WRITE(12) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0xAAuy )
        let r : WriteCDB = {
            OperationCode = command.ScsiCDB.[0];
            WRPROTECT = ( command.ScsiCDB.[1] &&& 0xE0uy ) >>> 5;
            DPO = Functions.CheckBitflag command.ScsiCDB.[1] 0x10uy;
            FUA = Functions.CheckBitflag command.ScsiCDB.[1] 0x08uy;
            FUA_NV = Functions.CheckBitflag command.ScsiCDB.[1] 0x02uy;
            LogicalBlockAddress = uint64( Functions.NetworkBytesToUInt32 command.ScsiCDB 2 );
            TransferLength = Functions.NetworkBytesToUInt32 command.ScsiCDB 6;
            GroupNumber = command.ScsiCDB.[10] &&& 0x1Fuy;
            Control = command.ScsiCDB.[11];
        }
        if r.WRPROTECT >= 5uy then
            let errmsg = sprintf "In WRITE(12) CDB, invalid WRPROTECT value(0x%02X)." r.WRPROTECT
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 1us },
                errmsg
            )

        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to Write16CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted WriteCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToWrite16CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )

        if command.ScsiCDB.Length < 16 then
            let errmsg = sprintf "CDB length in WRITE(16) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x8Auy )
        let r : WriteCDB = {
            OperationCode = command.ScsiCDB.[0];
            WRPROTECT = ( command.ScsiCDB.[1] &&& 0xE0uy ) >>> 5;
            DPO = Functions.CheckBitflag command.ScsiCDB.[1] 0x10uy;
            FUA = Functions.CheckBitflag command.ScsiCDB.[1] 0x08uy;
            FUA_NV = Functions.CheckBitflag command.ScsiCDB.[1] 0x02uy;
            LogicalBlockAddress = Functions.NetworkBytesToUInt64 command.ScsiCDB 2;
            TransferLength = Functions.NetworkBytesToUInt32 command.ScsiCDB 10;
            GroupNumber = command.ScsiCDB.[14] &&& 0x1Fuy;
            Control = command.ScsiCDB.[15];
        }
        if r.WRPROTECT >= 5uy then
            let errmsg = sprintf "In WRITE(16) CDB, invalid WRPROTECT value(0x%02X)." r.WRPROTECT
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 1us },
                errmsg
            )

        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to ReportSupportedOperationCodesCDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ReportSupportedOperationCodesCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToReportSupportedOpCodesCDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )

        if command.ScsiCDB.Length < 12 then
            let errmsg = sprintf "CDB length in REPORT SUPPORTED OPERATION CODES CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0xA3uy )
        assert( command.ScsiCDB.[1] &&& 0x1Fuy = 0x0Cuy )
        let r : ReportSupportedOperationCodesCDB = {
            OperationCode = command.ScsiCDB.[0];
            ServiceAction = command.ScsiCDB.[1] &&& 0x1Fuy;
            ReportingOptions = command.ScsiCDB.[2] &&& 0x7uy;
            RequestedOperationCode = command.ScsiCDB.[3];
            RequestedServiceAction = Functions.NetworkBytesToUInt16 command.ScsiCDB 4;
            AllocationLength = Functions.NetworkBytesToUInt32 command.ScsiCDB 6;
            Control = command.ScsiCDB.[11];
        }
        if r.ReportingOptions >= 3uy then
            let errmsg = sprintf "In REPORT SUPPORTED OPERATION CODES CDB, invalid REPORTING OPTIONS value(0x%02X)." r.ReportingOptions
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 1us },
                errmsg
            )

        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to ReportSupportedTaskManagementFunctionsCDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ReportSupportedTaskManagementFunctionsCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToReportSupportedTMFsCDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )

        if command.ScsiCDB.Length < 12 then
            let errmsg = sprintf "CDB length in REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0xA3uy )
        assert( command.ScsiCDB.[1] &&& 0x1Fuy = 0x0Duy )
        let r : ReportSupportedTaskManagementFunctionsCDB = {
            OperationCode = command.ScsiCDB.[0];
            ServiceAction = command.ScsiCDB.[1] &&& 0x1Fuy;
            AllocationLength = Functions.NetworkBytesToUInt32 command.ScsiCDB 6;
            Control = command.ScsiCDB.[11];
        }
        if r.AllocationLength < 4u then
            let errmsg = sprintf "In REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS CDB, AllocationLength value(%d) is too short." r.AllocationLength
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 6us },
                errmsg
            )

        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to ReadCapacity16CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted ReadCapacityCDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member private ToReadCapacity16CDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        if command.ScsiCDB.Length < 16 then
            let errmsg = sprintf "CDB length in READ CAPACITY(16) CDB is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        assert( command.ScsiCDB.[0] = 0x9Euy )
        assert( command.ScsiCDB.[1] &&& 0x1Fuy = 0x10uy )
        let r : ReadCapacityCDB = {
            OperationCode = command.ScsiCDB.[0];
            ServiceAction = command.ScsiCDB.[1] &&& 0x1Fuy;
            LogicalBlockAddress = Functions.NetworkBytesToUInt64 command.ScsiCDB 2;
            PMI = Functions.CheckBitflag command.ScsiCDB.[15] 0x01uy; 
            AllocationLength = Functions.NetworkBytesToUInt32 command.ScsiCDB 10;
            Control = command.ScsiCDB.[15];
        }
        r :> ICDB

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Convert SCSICommandPDU to CDB data structure.
    /// </summary>
    /// <param name="source">
    ///   Information of the connection that received SCSI command PDU.
    /// </param>
    /// <param name="objID">
    ///   Object identifier of caller object.
    /// </param>
    /// <param name="command">
    ///   Received SCSI command PDU from the initiator.
    /// </param>
    /// <returns>
    ///   Converted CDB object.
    ///   If received PDU has illegal data value in CDB, it raise an exception.
    /// </returns>
    static member public ConvertScsiCommandPDUToCDB ( source : CommandSourceInfo ) ( objID : OBJIDX_T ) ( command : SCSICommandPDU ) : ICDB =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( command.InitiatorTaskTag ), ValueSome( command.LUN ) )

        if command.ScsiCDB.Length < 6 then
            let errmsg = sprintf "CDB length is too short(length=" + ( string command.ScsiCDB.Length ) + ")."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us },
                errmsg
            )

        // OPERATION CODE field is in the first byte of SCSI SCB bytes.
        let OperationCode = command.ScsiCDB.[0]

        match OperationCode with
        | 0x12uy ->     // INQUIRY
            ConvertToCDB.ToInquiryCDB source objID command
        | 0x15uy ->     // MODE SELECT(6)
            ConvertToCDB.ToModeSelect6CDB source objID command
        | 0x55uy ->     // MODE SELECT(10)
            ConvertToCDB.ToModeSelect10CDB source objID command
        | 0x1Auy ->     // MODE SENSE(6)
            ConvertToCDB.ToModeSense6CDB source objID command
        | 0x5Auy ->     // MODE SENSE(10)
            ConvertToCDB.ToModeSense10CDB source objID command
        | 0x5Euy ->     // PERSISTENT RESERVE IN
            ConvertToCDB.ToPersistentReserveInCDB source objID command
        | 0x5Fuy ->     // PERSISTENT RESERVE OUT
            ConvertToCDB.ToPersistentReserveOutCDB source objID command
        | 0x34uy ->     // PRE-FETCH(10)
            ConvertToCDB.ToPreFetch10CDB source objID command
        | 0x90uy ->     // PRE-FETCH(16)
            ConvertToCDB.ToPreFetch16CDB source objID command
        | 0xA0uy ->     // REPORT LUNS
            ConvertToCDB.ToReportLUNsCDB source objID command
        | 0x03uy ->     // REQUEST SENSE
            ConvertToCDB.ToRequestSenseCDB source objID command
        | 0x00uy ->     // TEST UNIT READY
            ConvertToCDB.ToTestUnitReadyCDB source objID command
        | 0x04uy ->     // FORMAT UNIT
            ConvertToCDB.ToFormatUnitCDB source objID command
        | 0x08uy ->     // READ(6)
            ConvertToCDB.ToRead6CDB source objID command
        | 0x28uy ->     // READ(10)
            ConvertToCDB.ToRead10CDB source objID command
        | 0xA8uy ->     // READ(12)
            ConvertToCDB.ToRead12CDB source objID command
        | 0x88uy ->     // READ(16)
            ConvertToCDB.ToRead16CDB source objID command
        | 0x25uy ->     // READ CAPACITY(10)
            ConvertToCDB.ToReadCapacity10CDB source objID command
        | 0x35uy ->     // SYNCHRONIZE CACHE(10)
            ConvertToCDB.ToSynchronizeCache10CDB source objID command
        | 0x91uy ->     // SYNCHRONIZE CACHE(16)
            ConvertToCDB.ToSynchronizeCache16CDB source objID command
        | 0x0Auy ->     // WRITE(6)
            ConvertToCDB.ToWrite6CDB source objID command
        | 0x2Auy ->     // WRITE(10)
            ConvertToCDB.ToWrite10CDB source objID command
        | 0xAAuy ->     // WRITE(12)
            ConvertToCDB.ToWrite12CDB source objID command
        | 0x8Auy ->     // WRITE(16)
            ConvertToCDB.ToWrite16CDB source objID command
        | 0xA3uy ->
            match command.ScsiCDB.[1] &&& 0x1Fuy with
            | 0x0Cuy -> // REPORT SUPPORTED OPERATION CODES
                ConvertToCDB.ToReportSupportedOpCodesCDB source objID command
            | 0x0Duy -> // REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS
                ConvertToCDB.ToReportSupportedTMFsCDB source objID command
            | _ ->      // Unknown service action code
                let errmsg =
                    sprintf
                        "Unsupported service action code(Operation Code=0xA3, Service Action Code=0x%02X)."
                        ( command.ScsiCDB.[1] &&& 0x1Fuy )
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 4uy; FieldPointer = 1us },
                    errmsg
                )

        | 0x9Euy ->
            match command.ScsiCDB.[1] &&& 0x1Fuy with
            | 0x10uy -> // READ CAPACITY(16)
                ConvertToCDB.ToReadCapacity16CDB source objID command
            | _ ->      // Unknown service action code
                let errmsg =
                    sprintf
                        "Unsupported service action code(Operation Code=0x9E, Service Action Code=0x%02X)."
                        ( command.ScsiCDB.[1] &&& 0x1Fuy )
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 4uy; FieldPointer = 1us },
                    errmsg
                )

        | _ ->          // Unknown operation code
            let errmsg = sprintf "Unsupported operation code(0x%02X)." OperationCode
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_COMMAND_OPERATION_CODE, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_COMMAND_OPERATION_CODE,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 0us },
                errmsg
            )
