//=============================================================================
// Haruka Software Storage.
// ConvertToCDBTest.fs : Test cases for ConvertToCDB class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.BlockDeviceLU

//=============================================================================
// Import declaration

open System

open Xunit

open Haruka
open Haruka.Constants
open Haruka.Commons
open Haruka.BlockDeviceLU
open Haruka.Test

//=============================================================================
// Class implementation

type ConvertToCDB_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let defaultScsiCommand : SCSICommandPDU = {
        I = true;
        F = true;
        R = true;
        W = true;
        ATTR = TaskATTRCd.TAGLESS_TASK;
        LUN = lun_me.fromPrim 1UL;
        InitiatorTaskTag = itt_me.fromPrim 2u;
        ExpectedDataTransferLength = 3u;
        CmdSN = cmdsn_me.fromPrim 4u;
        ExpStatSN = statsn_me.fromPrim 5u;
        ScsiCDB = Array.empty;
        DataSegment = PooledBuffer.RentAndInit 1;
        BidirectionalExpectedReadDataLength = 6u;
        ByteCount = 0u;
    }

    let defaultSource = {
        I_TNexus = new ITNexus( "initiator", isid_me.zero, "target", tpgt_me.zero )
        CID = cid_me.fromPrim 2us;
        ConCounter = concnt_me.fromPrim 2;
        TSIH = tsih_me.zero;
        ProtocolService = new CProtocolService_Stub();
        SessionKiller = new HKiller()
    }

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.ConvertToCDB_0001() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 1uy; 2uy; 3uy; 4uy; 5uy; |];
        }

        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( x.Message.Contains( "CDB length is too short(length=5)" ) )

    [<Fact>]
    member _.ConvertToCDB_0002() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xFFuy; 1uy; 2uy; 3uy; 4uy; 5uy; |];
        }

        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( defaultSource.CID = x.CommandSource.CID ) )
            Assert.True( ( defaultSource.ConCounter = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_COMMAND_OPERATION_CODE = x.ASC ) )
            Assert.True( ( x.Message.Contains( "Unsupported operation code(0xFF)." ) ) )

    [<Fact>]
    member _.ConvertToCDB_0003() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA3uy; 0xFFuy; 2uy; 3uy; 4uy; 5uy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim  2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "Unsupported service action code(Operation Code=0xA3" ) ) )

    [<Fact>]
    member _.ConvertToCDB_0004() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x9Euy; 0xFFuy; 2uy; 3uy; 4uy; 5uy; |];
        }

        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "Unsupported service action code(Operation Code=0x9E" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToInquiryCDB_0100() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x12uy; 0x00uy; 0x01uy; 0x00uy; 0x04uy; 0xFFuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "EVPD bit in CDB is 0, but PageCode is not 0" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToInquiryCDB_0101() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x12uy; 0x01uy; 0x00uy; 0x03uy; 0x04uy; 0xEEuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.Inquiry = cdb.Type ) )
        let w = cdb :?> InquiryCDB
        Assert.True( w.OperationCode = 0x12uy )
        Assert.True( w.EVPD )
        Assert.True( w.PageCode = 0uy )
        Assert.True( w.AllocationLength = 772us )
        Assert.True( w.Control = 0xEEuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToModeSelect6CDB_0200() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x15uy; 0x11uy; 0x00uy; 0x00uy; 0xDDuy; 0xBBuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.ModeSelect = cdb.Type ) )
        let w = cdb :?> ModeSelectCDB
        Assert.True( w.OperationCode = 0x15uy )
        Assert.True( w.PF )
        Assert.True( w.SP )
        Assert.True( w.ParameterListLength = 0xDDus )
        Assert.True( w.Control = 0xBBuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToModeSelect10CDB_0300() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x55uy; 0x11uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in MODE SELECT(10) CDB" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToModeSelect10CDB_0301() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x55uy; 0x11uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.ModeSelect = cdb.Type ) )
        let w = cdb :?> ModeSelectCDB
        Assert.True( w.OperationCode = 0x55uy )
        Assert.True( w.PF )
        Assert.True( w.SP )
        Assert.True( w.ParameterListLength = 0xAABBus )
        Assert.True( w.Control = 0xCCuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToModeSense6CDB_0400() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x1Auy; 0x08uy; 0xCAuy; 0xBBuy; 0xCCuy; 0xDDuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.ModeSense = cdb.Type ) )
        let w = cdb :?> ModeSenseCDB
        Assert.True( w.OperationCode = 0x1Auy )
        Assert.True( w.DBD )
        Assert.True( w.PC = 0x03uy )
        Assert.True( w.PageCode = 0x0Auy )
        Assert.True( w.SubPageCode = 0xBBuy )
        Assert.True( w.AllocationLength = 0xCCus )
        Assert.True( w.Control = 0xDDuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToModeSense10CDB_0500() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x5Auy; 0x11uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in MODE SENSE(10) CDB" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToModeSense10CDB_0501() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x5Auy; 0x18uy; 0xCAuy; 0xBBuy; 0x00uy; 0x00uy; 0x00uy; 0xCCuy; 0xDDuy; 0xEEuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.ModeSense = cdb.Type ) )
        let w = cdb :?> ModeSenseCDB
        Assert.True( w.OperationCode = 0x5Auy )
        Assert.True( w.LLBAA )
        Assert.True( w.DBD )
        Assert.True( w.PC = 0x03uy )
        Assert.True( w.PageCode = 0x0Auy )
        Assert.True( w.SubPageCode = 0xBBuy )
        Assert.True( w.AllocationLength = 0xCCDDus )
        Assert.True( w.Control = 0xEEuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToPersistentReserveInCDB_0600() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x5Euy; 0x11uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in PERSISTENT RESERVE IN CDB" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToPersistentReserveInCDB_0601() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x5Euy; 0x04uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In PERSISTENT RESERVE IN CDB, invalid SERVICE ACTION value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToPersistentReserveInCDB_0602() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x5Euy; 0x03uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.PersistentReserveIn = cdb.Type ) )
        let w = cdb :?> PersistentReserveInCDB
        Assert.True( w.OperationCode = 0x5Euy )
        Assert.True( w.ServiceAction = 0x03uy )
        Assert.True( w.AllocationLength = 0xAABBus )
        Assert.True( w.Control = 0xCCuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToPersistentReserveOutCDB_0700() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x5Fuy; 0x11uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in PERSISTENT RESERVE OUT CDB" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToPersistentReserveOutCDB_0701() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x5Fuy; 0x08uy; 0x01uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In PERSISTENT RESERVE OUT CDB, invalid SERVICE ACTION value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToPersistentReserveOutCDB_0702() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x5Fuy; 0x07uy; 0xF1uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In PERSISTENT RESERVE OUT CDB, invalid SCOPE value" ) ) )

    [<Theory>]
    [<InlineData( 0x01uy )>]
    [<InlineData( 0x02uy )>]
    [<InlineData( 0x04uy )>]
    [<InlineData( 0x05uy )>]
    member _.ConvertScsiCommandPDUToPersistentReserveOutCDB_0703( argServiceAction : byte ) =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x5Fuy; argServiceAction; 0x04uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In PERSISTENT RESERVE OUT CDB, invalid TYPE value" ) ) )

    [<Theory>]
    [<InlineData( 0x00uy )>]
    [<InlineData( 0x03uy )>]
    [<InlineData( 0x06uy )>]
    [<InlineData( 0x07uy )>]
    member _.ConvertScsiCommandPDUToPersistentReserveOutCDB_0704( argServiceAction : byte ) =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x5Fuy; argServiceAction; 0x04uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.PersistentReserveOut = cdb.Type ) )
        let w = cdb :?> PersistentReserveOutCDB
        Assert.True( w.OperationCode = 0x5Fuy )
        Assert.True( w.ServiceAction = argServiceAction )
        Assert.True( w.Scope = 0x00uy )
        Assert.True( w.PRType = PR_TYPE.NO_RESERVATION )
        Assert.True( w.ParameterListLength = 0x0000AABBu )
        Assert.True( w.Control = 0xCCuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToPersistentReserveOutCDB_0705() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x5Fuy; 0x04uy; 0x04uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In PERSISTENT RESERVE OUT CDB, invalid TYPE value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToPersistentReserveOutCDB_0706() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x5Fuy; 0x01uy; 0x08uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xEEuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.PersistentReserveOut = cdb.Type ) )
        let w = cdb :?> PersistentReserveOutCDB
        Assert.True( w.OperationCode = 0x5Fuy )
        Assert.True( w.ServiceAction = 0x01uy )
        Assert.True( w.Scope = 0x00uy )
        Assert.True( w.PRType = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS )
        Assert.True( w.ParameterListLength = 0xAABBCCDDu )
        Assert.True( w.Control = 0xEEuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReportLUNsCDB_0800() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA0uy; 0x11uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0x00uy; 0x00uy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in REPORT LUNs CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReportLUNsCDB_0801() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA0uy; 0x00uy; 0x03uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0x00uy; 0x00uy; 0x00uy |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In REPORT LUNs CDB, invalid SELECT REPORT value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReportLUNsCDB_0802() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA0uy; 0x00uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x03uy; 0x00uy; 0x00uy |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In REPORT LUNs CDB, ALLOCATION LENGTH value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReportLUNsCDB_0803() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA0uy; 0x00uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy; 0xABuy; 0xCDuy; 0xEFuy; 0xAAuy; 0x00uy; 0xBBuy |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.ReportLUNs = cdb.Type ) )
        let w = cdb :?> ReportLUNsCDB
        Assert.True( w.OperationCode = 0xA0uy )
        Assert.True( w.SelectReport = 0x02uy )
        Assert.True( w.AllocationLength = 0xABCDEFAAu )
        Assert.True( w.Control = 0xBBuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToRequestSenseCDB_0900() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x03uy; 0x01uy; 0x02uy; 0x00uy; 0xAAuy; 0xBBuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.RequestSense = cdb.Type ) )
        let w = cdb :?> RequestSenseCDB
        Assert.True( w.OperationCode = 0x03uy )
        Assert.True( w.DESC )
        Assert.True( w.AllocationLength = 0xAAuy )
        Assert.True( w.Control = 0xBBuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToTestUnitReadyCDB_1000() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x00uy; 0x00uy; 0x02uy; 0x00uy; 0x00uy; 0xAAuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.TestUnitReady = cdb.Type ) )
        let w = cdb :?> TestUnitReadyCDB
        Assert.True( w.OperationCode = 0x00uy )
        Assert.True( w.Control = 0xAAuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToFormatUnitCDB_1100() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x04uy; 0xAAuy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.FormatUnit = cdb.Type ) )
        let w = cdb :?> FormatUnitCDB
        Assert.True( w.OperationCode = 0x04uy )
        Assert.True( w.FMTPINFO )
        Assert.False( w.RTO_REQ )
        Assert.True( w.LONGLIST )
        Assert.False( w.FMTDATA  )
        Assert.True( w.CMPLIST )
        Assert.True( w.DefectListFormat = 0x02uy )
        Assert.True( w.Control = 0xAAuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToFormatUnitCDB_1101() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x04uy; 0x55uy; 0x00uy; 0x00uy; 0x00uy; 0xBBuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.FormatUnit = cdb.Type ) )
        let w = cdb :?> FormatUnitCDB
        Assert.True( w.OperationCode = 0x04uy )
        Assert.False( w.FMTPINFO )
        Assert.True( w.RTO_REQ )
        Assert.False( w.LONGLIST )
        Assert.True( w.FMTDATA  )
        Assert.False( w.CMPLIST )
        Assert.True( w.DefectListFormat = 0x05uy )
        Assert.True( w.Control = 0xBBuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToRead6CDB_1200() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x08uy; 0x1Auy; 0xBBuy; 0xCCuy; 0xDDuy; 0xEEuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.Read = cdb.Type ) )
        let w = cdb :?> ReadCDB
        Assert.True( w.OperationCode = 0x08uy )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0x001ABBCCUL )
        Assert.True( w.TransferLength = blkcnt_me.ofUInt32 0xDDu )
        Assert.True( w.Control = 0xEEuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToRead6CDB_1201() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x08uy; 0x1Auy; 0xBBuy; 0xCCuy; 0x00uy; 0xEEuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.Read = cdb.Type ) )
        let w = cdb :?> ReadCDB
        Assert.True( w.OperationCode = 0x08uy )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0x001ABBCCUL )
        Assert.True( w.TransferLength = blkcnt_me.ofUInt32 256u )
        Assert.True( w.Control = 0xEEuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToRead10CDB_1300() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x28uy; 0xEAuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x1Fuy; 0xEEuy; 0xFFuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in READ(10) CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToRead10CDB_1301() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x28uy; 0xEAuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x1Fuy; 0xEEuy; 0xFFuy; 0xABuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In READ(10) CDB, invalid RDPROTECT value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToRead10CDB_1302() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x28uy; 0x6Auy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x1Fuy; 0xEEuy; 0xFFuy; 0xABuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.Read = cdb.Type ) )
        let w = cdb :?> ReadCDB
        Assert.True( w.OperationCode = 0x28uy )
        Assert.True( w.RdProtect = 0x03uy )
        Assert.False( w.DPO )
        Assert.True( w.FUA )
        Assert.True( w.FUA_NV )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBCCDDUL )
        Assert.True( w.GroupNumber = 0x1Fuy )
        Assert.True( w.TransferLength = blkcnt_me.ofUInt32 0xEEFFu )
        Assert.True( w.Control = 0xABuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToRead12CDB_1400() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA8uy; 0xEAuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x1Fuy; 0xEEuy; 0xFFuy; 0x00uy; 0x00uy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in READ(12) CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToRead12CDB_1401() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA8uy; 0xEAuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xEEuy; 0xFFuy; 0xAAuy; 0xBBuy; 0xaFuy; 0xABuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In READ(12) CDB, invalid RDPROTECT value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToRead12CDB_1402() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA8uy; 0x72uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xEEuy; 0xFFuy; 0xAAuy; 0xBBuy; 0x1Fuy; 0xABuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.Read = cdb.Type ) )
        let w = cdb :?> ReadCDB
        Assert.True( w.OperationCode = 0xA8uy )
        Assert.True( w.RdProtect = 0x03uy )
        Assert.True( w.DPO )
        Assert.False( w.FUA )
        Assert.True( w.FUA_NV )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBCCDDUL )
        Assert.True( w.GroupNumber = 0x1Fuy )
        Assert.True( w.TransferLength = blkcnt_me.ofUInt32 0xEEFFAABBu )
        Assert.True( w.Control = 0xABuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToRead16CDB_1500() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x88uy; 0x0Auy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 0x00uy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in READ(16) CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToRead16CDB_1501() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x88uy; 0xFAuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 0x18uy; 0xAAuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In READ(16) CDB, invalid RDPROTECT value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToRead16CDB_1502() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x88uy; 0x78uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 0x18uy; 0xAAuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.Read = cdb.Type ) )
        let w = cdb :?> ReadCDB
        Assert.True( w.OperationCode = 0x88uy )
        Assert.True( w.RdProtect = 0x03uy )
        Assert.True( w.DPO )
        Assert.True( w.FUA )
        Assert.False( w.FUA_NV )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBCCDDAABBCCDDUL )
        Assert.True( w.GroupNumber = 0x18uy )
        Assert.True( w.TransferLength = blkcnt_me.ofUInt32 0xDEADBEEFu )
        Assert.True( w.Control = 0xAAuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReadCapacity10CDB_1600() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x25uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x00uy; 0x00uy; 0x01uy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in READ CAPACITY(10) CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReadCapacity10CDB_1601() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x25uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x00uy; 0x00uy; 0x01uy; 0xAAuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.ReadCapacity = cdb.Type ) )
        let w = cdb :?> ReadCapacityCDB
        Assert.True( w.OperationCode = 0x25uy )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBCCDDUL )
        Assert.True( w.PMI )
        Assert.True( w.Control = 0xAAuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToWrite6CDB_1700() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x0Auy; 0x1Auy; 0xBBuy; 0xCCuy; 0xDDuy; 0xEEuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.Write = cdb.Type ) )
        let w = cdb :?> WriteCDB
        Assert.True( w.OperationCode = 0x0Auy )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0x001ABBCCUL )
        Assert.True( w.TransferLength = blkcnt_me.ofUInt32 0xDDu )
        Assert.True( w.Control = 0xEEuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToWrite6CDB_1701() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x0Auy; 0x1Auy; 0xBBuy; 0xCCuy; 0x00uy; 0xEEuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.Write = cdb.Type ) )
        let w = cdb :?> WriteCDB
        Assert.True( w.OperationCode = 0x0Auy )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0x001ABBCCUL )
        Assert.True( w.TransferLength = blkcnt_me.ofUInt32 256u )
        Assert.True( w.Control = 0xEEuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToWrite10CDB_1800() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x2Auy; 0xEAuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x1Fuy; 0xEEuy; 0xFFuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in WRITE(10) CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToWrite10CDB_1801() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x2Auy; 0xEAuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x1Fuy; 0xEEuy; 0xFFuy; 0xABuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In WRITE(10) CDB, invalid WRPROTECT value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToWrite10CDB_1802() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x2Auy; 0x6Auy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x1Fuy; 0xEEuy; 0xFFuy; 0xABuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.Write = cdb.Type ) )
        let w = cdb :?> WriteCDB
        Assert.True( w.OperationCode = 0x2Auy )
        Assert.True( w.WRPROTECT = 0x03uy )
        Assert.False( w.DPO )
        Assert.True( w.FUA )
        Assert.True( w.FUA_NV )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBCCDDUL )
        Assert.True( w.GroupNumber = 0x1Fuy )
        Assert.True( w.TransferLength = blkcnt_me.ofUInt32 0xEEFFu )
        Assert.True( w.Control = 0xABuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToWrite12CDB_1900() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xAAuy; 0xEAuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x1Fuy; 0xEEuy; 0xFFuy; 0x00uy; 0x00uy; |];
        }

        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in WRITE(12) CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToWrite12CDB_1901() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xAAuy; 0xEAuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xEEuy; 0xFFuy; 0xAAuy; 0xBBuy; 0xaFuy; 0xABuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In WRITE(12) CDB, invalid WRPROTECT value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToWrite12CDB_1902() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xAAuy; 0x72uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xEEuy; 0xFFuy; 0xAAuy; 0xBBuy; 0x1Fuy; 0xABuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.Write = cdb.Type ) )
        let w = cdb :?> WriteCDB
        Assert.True( w.OperationCode = 0xAAuy )
        Assert.True( w.WRPROTECT = 0x03uy )
        Assert.True( w.DPO )
        Assert.False( w.FUA )
        Assert.True( w.FUA_NV )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBCCDDUL )
        Assert.True( w.GroupNumber = 0x1Fuy )
        Assert.True( w.TransferLength = blkcnt_me.ofUInt32 0xEEFFAABBu )
        Assert.True( w.Control = 0xABuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToWrite16CDB_2000() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x8Auy; 0x0Auy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 0x00uy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in WRITE(16) CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToWrite16CDB_2001() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x8Auy; 0xFAuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 0x18uy; 0xAAuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In WRITE(16) CDB, invalid WRPROTECT value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToWrite16CDB_2002() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x8Auy; 0x78uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 0x18uy; 0xAAuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.Write = cdb.Type ) )
        let w = cdb :?> WriteCDB
        Assert.True( w.OperationCode = 0x8Auy )
        Assert.True( w.WRPROTECT = 0x03uy )
        Assert.True( w.DPO )
        Assert.True( w.FUA )
        Assert.False( w.FUA_NV )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBCCDDAABBCCDDUL )
        Assert.True( w.GroupNumber = 0x18uy )
        Assert.True( w.TransferLength = blkcnt_me.ofUInt32 0xDEADBEEFu )
        Assert.True( w.Control = 0xAAuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReportSupportedOperationCodesCDB_2100() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA3uy; 0x0Cuy; 0x07uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x00uy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in REPORT SUPPORTED OPERATION CODES CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReportSupportedOperationCodesCDB_2101() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA3uy; 0x0Cuy; 0x07uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x00uy; 0xAAuy |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In REPORT SUPPORTED OPERATION CODES CDB, invalid REPORTING OPTIONS value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReportSupportedOperationCodesCDB_2102() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA3uy; 0x0Cuy; 0x02uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x00uy; 0xAAuy |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.ReportSupportedOperationCodes = cdb.Type ) )
        let w = cdb :?> ReportSupportedOperationCodesCDB
        Assert.True( w.OperationCode = 0xA3uy )
        Assert.True( w.ServiceAction = 0x0Cuy )
        Assert.True( w.ReportingOptions = 0x02uy )
        Assert.True( w.RequestedOperationCode = 0xAAuy )
        Assert.True( w.RequestedServiceAction = 0xBBCCus )
        Assert.True( w.AllocationLength = 0xAABBCCDDu )
        Assert.True( w.Control = 0xAAuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReportSupportedTaskManagementFunctionsCDB_2200() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA3uy; 0x0Duy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x00uy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReportSupportedTaskManagementFunctionsCDB_2201() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA3uy; 0x0Duy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x03uy; 0x00uy; 0xBBuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "In REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS CDB, AllocationLength value" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReportSupportedTaskManagementFunctionsCDB_2202() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0xA3uy; 0x0Duy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x00uy; 0xBBuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.ReportSupportedTaskManagementFunctions = cdb.Type ) )
        let w = cdb :?> ReportSupportedTaskManagementFunctionsCDB
        Assert.True( w.OperationCode = 0xA3uy )
        Assert.True( w.ServiceAction = 0x0Duy )
        Assert.True( w.AllocationLength = 0xAABBCCDDu )
        Assert.True( w.Control = 0xBBuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReadCapacity16CDB_2300() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x9Euy; 0x10uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xABuy; 0xCDuy; 0xEFuy; 0xABuy; 0x00uy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in READ CAPACITY(16) CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToReadCapacity16CDB_2301() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x9Euy; 0x10uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xABuy; 0xCDuy; 0xEFuy; 0xABuy; 0x00uy; 0xBBuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.ReadCapacity = cdb.Type ) )
        let w = cdb :?> ReadCapacityCDB
        Assert.True( w.OperationCode = 0x9Euy )
        Assert.True( w.ServiceAction = 0x10uy )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBCCDDAABBCCDDUL )
        Assert.True( w.AllocationLength = 0xABCDEFABu )
        Assert.True( w.Control = 0xBBuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToSynchronizeCache10CDB_2400() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x35uy; 0x06uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xFFuy; 0x11uy; 0x22uy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in SYNCHRONIZE CACHE(10) CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToSynchronizeCache10CDB_2401() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x35uy; 0x06uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xFFuy; 0x11uy; 0x22uy; 0x55uy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.SynchronizeCache = cdb.Type ) )
        let w = cdb :?> SynchronizeCacheCDB
        Assert.True( w.OperationCode = 0x35uy )
        Assert.True( w.SyncNV = true )
        Assert.True( w.IMMED = true )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0x00000000AABBCCDDUL )
        Assert.True( w.NumberOfBlocks = blkcnt_me.ofUInt32 0x00001122u )
        Assert.True( w.GroupNumber = 0x1Fuy )
        Assert.True( w.Control = 0x55uy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToSynchronizeCache16CDB_2500() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x91uy; 0x06uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xEEuy; 0xFFuy; 0x00uy; 0x01uy; 0x01uy; 0x02uy; 0x03uy; 0x04uy; 0xFFuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in SYNCHRONIZE CACHE(16) CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToSynchronizeCache16CDB_2501() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x91uy; 0x06uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xEEuy; 0xFFuy; 0x00uy; 0x01uy; 0x01uy; 0x02uy; 0x03uy; 0x04uy; 0xFFuy; 0x22uy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.SynchronizeCache = cdb.Type ) )
        let w = cdb :?> SynchronizeCacheCDB
        Assert.True( w.OperationCode = 0x91uy )
        Assert.True( w.SyncNV = true )
        Assert.True( w.IMMED = true )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBCCDDEEFF0001UL )
        Assert.True( w.NumberOfBlocks = blkcnt_me.ofUInt32 0x01020304u )
        Assert.True( w.GroupNumber = 0x1Fuy )
        Assert.True( w.Control = 0x22uy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToPreFetch10CDB_2600() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x34uy; 0x02uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xFFuy; 0x11uy; 0x22uy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in PRE-FETCH(10) CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToPreFetch10CDB_2601() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x34uy; 0x02uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xFFuy; 0x11uy; 0x22uy; 0xBBuy; |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.PreFetch = cdb.Type ) )
        let w = cdb :?> PreFetchCDB
        Assert.True( w.OperationCode = 0x34uy )
        Assert.True( w.IMMED = true )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0x00000000AABBCCDDUL )
        Assert.True( w.GroupNumber = 0x1Fuy )
        Assert.True( w.PrefetchLength = blkcnt_me.ofUInt32 0x00001122u )
        Assert.True( w.Control = 0xBBuy )

    [<Fact>]
    member _.ConvertScsiCommandPDUToPreFetch16CDB_2700() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x90uy; 0x02uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x11uy; 0x22uy; 0x33uy; 0x44uy; 0xFFuy; |];
        }
        try
            ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True( ( cid_me.fromPrim 2us = x.CommandSource.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = x.CommandSource.ConCounter ) )
            Assert.True( ( ScsiCmdStatCd.CHECK_CONDITION = x.Status ) )
            Assert.True( ( SenseKeyCd.ILLEGAL_REQUEST = x.SenseKey ) )
            Assert.True( ( ASCCd.INVALID_FIELD_IN_CDB = x.ASC ) )
            Assert.True( ( x.Message.Contains( "CDB length in PRE-FETCH(16) CDB is too short" ) ) )

    [<Fact>]
    member _.ConvertScsiCommandPDUToPreFetch16CDB_2701() =
        let command : SCSICommandPDU = {
            defaultScsiCommand with
                ScsiCDB = [| 0x90uy; 0x02uy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; 0x11uy; 0x22uy; 0x33uy; 0x44uy; 0xFFuy; 0x33uy |];
        }
        let cdb = ConvertToCDB.ConvertScsiCommandPDUToCDB defaultSource ( objidx_me.NewID() ) command
        Assert.True( ( CDBTypes.PreFetch = cdb.Type ) )
        let w = cdb :?> PreFetchCDB
        Assert.True( w.OperationCode = 0x90uy )
        Assert.True( w.IMMED = true )
        Assert.True( w.LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBCCDDAABBCCDDUL )
        Assert.True( w.GroupNumber = 0x1Fuy )
        Assert.True( w.PrefetchLength = blkcnt_me.ofUInt32 0x11223344u )
        Assert.True( w.Control = 0x33uy )
