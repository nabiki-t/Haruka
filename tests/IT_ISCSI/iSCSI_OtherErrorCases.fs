//=============================================================================
// Haruka Software Storage.
// iSCSI_OtherErrorCases.fs : Test cases for iSCSI various error cases.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.IT.ISCSI

//=============================================================================
// Import declaration

open System
open System.IO
open System.Net
open System.Threading.Tasks

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.Test

//=============================================================================
// Class implementation

[<Collection( "iSCSI_Numbering" )>]     // Reuse existing test fixtures
type iSCSI_OtherErrorCases( fx : iSCSI_Numbering_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_CID1 = cid_me.fromPrim 1us
    let g_CID2 = cid_me.fromPrim 2us

    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL

    let g_DefITT = itt_me.fromPrim 0xFFFFFFFFu
    let g_DefTTT = ttt_me.fromPrim 0xFFFFFFFFu

    let iSCSIPortNo = fx.iSCSIPortNo
    let m_MediaSize = fx.MediaSize
    let m_MediaBlockSize = fx.MediaBlockSize

    // default session parameters
    let m_defaultSessParam = {
        InitiatorName = "iqn.2020-05.example.com:initiator";
        InitiatorAlias = "aaa";
        TargetName = "iqn.2020-05.example.com:target1";
        TargetAlias = "";
        ISID = isid_me.fromPrim 1UL;
        TSIH = tsih_me.fromPrim 0us;
        MaxConnections = Constants.NEGOPARAM_MaxConnections;
        InitialR2T = false;
        ImmediateData = true;
        MaxBurstLength = 262144u;
        FirstBurstLength = 262144u;
        DefaultTime2Wait = 2us;
        DefaultTime2Retain = 20us;
        MaxOutstandingR2T = 16us;
        DataPDUInOrder = false;
        DataSequenceInOrder = false;
        ErrorRecoveryLevel = 0uy;
    }

    // default connection parameters
    let m_defaultConnParam = {
        PortNo = iSCSIPortNo;
        CID = g_CID0;
        Initiator_UserName = "";
        Initiator_Password = "";
        Target_UserName = "";
        Target_Password = "";
        HeaderDigest = DigestType.DST_CRC32C;
        DataDigest = DigestType.DST_CRC32C;
        MaxRecvDataSegmentLength_I = 4096u;
        MaxRecvDataSegmentLength_T = 4096u;
    }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.ResidualCount_Read_Underflow_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // send read command with block count = 1, but iSCSI ExpectedDataTransferLength = 65536
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy 1us false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true true false TaskATTRCd.SIMPLE_TASK g_LUN1 65536u readCDB PooledBuffer.Empty 0u

            // Reseive Data-In PDU
            let! dpdu = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( dpdu.InitiatorTaskTag = itt ))
            Assert.True(( dpdu.DataSegment.Count = int m_MediaBlockSize ))

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.ResponseData.Count = 0 ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.U ))
            Assert.True(( rpdu.ResidualCount = 65536u - m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 false
        }
        
    [<Fact>]
    member _.ResidualCount_Read_Underflow_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // send read command with block count = 0, but iSCSI ExpectedDataTransferLength = 65536
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy 0us false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true true false TaskATTRCd.SIMPLE_TASK g_LUN1 65536u readCDB PooledBuffer.Empty 0u

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.ResponseData.Count = 0 ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.U ))
            Assert.True(( rpdu.ResidualCount = 65536u ))

            do! r1.CloseSession g_CID0 false
        }

    [<Fact>]
    member _.ResidualCount_Read_Overflow_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam { m_defaultConnParam with MaxRecvDataSegmentLength_I = 8192u }
            let blockCount = 8192u / m_MediaBlockSize

            // send read command with read size = 8192 bytes, but iSCSI ExpectedDataTransferLength = 512
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true true false TaskATTRCd.SIMPLE_TASK g_LUN1 512u readCDB PooledBuffer.Empty 0u

            // Reseive Data-In PDU
            let! dpdu = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( dpdu.InitiatorTaskTag = itt ))
            Assert.True(( dpdu.DataSegment.Count = 512 ))

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.ResponseData.Count = 0 ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.O ))
            Assert.True(( rpdu.ResidualCount = 8192u - 512u ))

            do! r1.CloseSession g_CID0 false
        }

    [<Fact>]
    member _.ResidualCount_Read_Overflow_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam { m_defaultConnParam with MaxRecvDataSegmentLength_I = 8192u }
            let blockCount = 8192u / m_MediaBlockSize

            // send read command with read size = 8192 bytes, but iSCSI ExpectedDataTransferLength = 0
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true true false TaskATTRCd.SIMPLE_TASK g_LUN1 0u readCDB PooledBuffer.Empty 0u

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.ResponseData.Count = 0 ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.O ))
            Assert.True(( rpdu.ResidualCount = 8192u ))

            do! r1.CloseSession g_CID0 false
        }

    [<Fact>]
    member _.Immidiate_NopOut_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            // Send Nop-Out with immidiate flag
            let! itt2, _ = r1.SendNOPOutPDU g_CID0 true g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive Nop-In PDU
            let! nopinPDU = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU.InitiatorTaskTag = itt2 ))

            // Send DataOut PDU
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 false
        }

    [<Fact>]
    member _.Immidiate_SCSICommand_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! ittW, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            // send SCSI read command with immidiate flag
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! ittR, _ = r1.SendSCSICommandPDU g_CID0 true true true false TaskATTRCd.SIMPLE_TASK g_LUN1 4096u readCDB PooledBuffer.Empty 0u

            // receive Data-In PDU for SCSI read command
            let! datainPDU = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( datainPDU.InitiatorTaskTag = ittR ))

            // receive SCSI Response for SCSI read command
            let! respPDU_R = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( respPDU_R.InitiatorTaskTag = ittR ))
            Assert.True(( respPDU_R.Status = ScsiCmdStatCd.GOOD ))

            // Send DataOut PDU
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU g_CID0 true ittW g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // receive SCSI Response for SCSI write command
            let! respPDU_W = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( respPDU_W.InitiatorTaskTag = ittW ))
            Assert.True(( respPDU_W.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 false
        }

    [<Fact>]
    member _.Immidiate_TaskManagementFunctionRequest_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // Send Nop-Out and receive Nop-In
            let! ittNOP, cmdsnNOP = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU.InitiatorTaskTag = ittNOP ))

            // send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! ittW, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            // send TMF request with immidiate flag
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 true TaskMgrReqCd.ABORT_TASK g_LUN1 ittNOP cmdsnNOP datasn_me.zero

            // receive TMP response
            let! tmfPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfPDU.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Send DataOut PDU
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU g_CID0 true ittW g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // receive SCSI Response for SCSI write command
            let! respPDU_W = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( respPDU_W.InitiatorTaskTag = ittW ))
            Assert.True(( respPDU_W.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 false
        }

    [<Fact>]
    member _.Immidiate_TextRequest_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! ittW, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            // send Text request with immidiate flag
            let! ittTX, _ = r1.SendTextRequestPDU g_CID0 true true false ValueNone g_LUN1 g_DefTTT [||]

            // receive text response
            let! txrPDU = r1.ReceiveSpecific<TextResponsePDU> g_CID0
            Assert.True(( txrPDU.F ))
            Assert.True(( txrPDU.InitiatorTaskTag = ittTX ))
            Assert.True(( txrPDU.TextResponse.Length = 0 ))

            // Send DataOut PDU
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU g_CID0 true ittW g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // receive SCSI Response for SCSI write command
            let! respPDU_W = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( respPDU_W.InitiatorTaskTag = ittW ))
            Assert.True(( respPDU_W.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 false
        }

    [<Fact>]
    member _.Immidiate_Logout_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! ittW, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            do! r1.CloseSession g_CID0 true
        }

    [<Fact>]
    member _.Immidiate_SNACK_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession { m_defaultSessParam with ErrorRecoveryLevel = 1uy } m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN

            // Send Nop-Out and receive Nop-In
            let! ittNOP, cmdsnNOP = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU.InitiatorTaskTag = ittNOP ))
            Assert.True(( nopinPDU.StatSN = sendExpStatSN1 ))

            // rewind ExpStatSN
            r1.Connection( g_CID0 ).RewindExtStatSN( statsn_me.fromPrim 1u )

            // send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! ittW, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            // Send SNACK request
            do! r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.STATUS g_LUN1 g_DefITT g_DefTTT ( statsn_me.toPrim sendExpStatSN1 ) 1u

            // Receive Nop-In 1
            let! nopinPDU_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_2.InitiatorTaskTag = ittNOP ))
            Assert.True(( nopinPDU_2.StatSN = sendExpStatSN1 ))

            // rewind ExpStatSN
            r1.Connection( g_CID0 ).SkipExtStatSN( statsn_me.fromPrim 1u )

            // Send DataOut PDU
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU g_CID0 true ittW g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // receive SCSI Response for SCSI write command
            let! respPDU_W = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( respPDU_W.InitiatorTaskTag = ittW ))
            Assert.True(( respPDU_W.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 false
        }

    [<Theory>]
    [<InlineData( 0uy )>]
    [<InlineData( 1uy )>]
    member _.HeaderDigestError_001 ( erl : byte ) =
        task {
            let sessParam = { m_defaultSessParam with ErrorRecoveryLevel = erl }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam m_defaultConnParam
            Assert.True(( r1.Connection( g_CID0 ).Params.HeaderDigest = DigestType.DST_CRC32C ))

            // Send Nop-Out PDU with header digest error
            let! _ = r1.SendNOPOutPDU_Test id ( ValueSome( 8u, 40u ) ) g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty

            // session recovery
            try
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    [<Theory>]
    [<InlineData( 0uy )>]
    [<InlineData( 1uy )>]
    member _.DataDigestError_NopOut_001 ( erl : byte ) =
        task {
            let sessParam = { m_defaultSessParam with ErrorRecoveryLevel = erl }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam m_defaultConnParam

            // Send Nop-Out and receive Nop-In
            let! ittNOP_1, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // Send Nop-Out PDU with data digest error
            let sendData = PooledBuffer.RentAndInit 1024
            let! _ = r1.SendNOPOutPDU_Test id ( ValueSome( 100u, 100u ) ) g_CID0 false g_LUN1 g_DefTTT sendData
            sendData.Return()

            // Receive Reject PDU
            let! rejectPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejectPDU.Reason = RejectReasonCd.DATA_DIGEST_ERR ))

            // rewind CmdSN
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send Nop-Out and receive Nop-In
            let! ittNOP_2, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_2.InitiatorTaskTag = ittNOP_2 ))

            do! r1.CloseSession g_CID0 false
        }

    [<Theory>]
    [<InlineData( 0uy )>]
    [<InlineData( 1uy )>]
    member _.DataDigestError_SCSICommand_001 ( erl : byte ) =
        task {
            let sessParam = { m_defaultSessParam with ErrorRecoveryLevel = erl }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam m_defaultConnParam

            // Send Nop-Out and receive Nop-In
            let! ittNOP_1, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // Send SCSI Command PDU with data digest error
            let sendData = PooledBuffer.RentAndInit 1024
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy 1us false false
            let! _ = r1.SendSCSICommandPDU_Test id ( ValueSome( 100u, 100u ) ) g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 1024u writeCDB sendData 0u
            sendData.Return()

            // Receive Reject PDU
            let! rejectPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejectPDU.Reason = RejectReasonCd.DATA_DIGEST_ERR ))

            // rewind CmdSN
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send Nop-Out and receive Nop-In
            let! ittNOP_2, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_2.InitiatorTaskTag = ittNOP_2 ))

            do! r1.CloseSession g_CID0 false
        }

    [<Theory>]
    [<InlineData( 0uy )>]
    [<InlineData( 1uy )>]
    member _.DataDigestError_SCSIDataOut_001 ( erl : byte ) =
        task {
            let sessParam = { m_defaultSessParam with ErrorRecoveryLevel = erl }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // Send Nop-Out and receive Nop-In
            let! ittNOP_1, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // Send SCSI Command PDU
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            // Send Data-Out PDU with data digest error
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU_Test id ( ValueSome( 100u, 100u ) ) g_CID0 true itt g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // Receive Reject PDU
            let! rejectPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejectPDU.Reason = RejectReasonCd.DATA_DIGEST_ERR ))

            // Re-Send Data-Out PDU
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // Receive SCSI Response PDU
            let! respPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( respPDU.InitiatorTaskTag = itt ))
            Assert.True(( respPDU.Status = ScsiCmdStatCd.GOOD ))

            // Send Nop-Out and receive Nop-In
            let! ittNOP_2, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_2.InitiatorTaskTag = ittNOP_2 ))

            do! r1.CloseSession g_CID0 false
        }

    [<Theory>]
    [<InlineData( 0uy )>]
    [<InlineData( 1uy )>]
    member _.DataDigestError_TextRequest_001 ( erl : byte ) =
        task {
            let sessParam = { m_defaultSessParam with ErrorRecoveryLevel = erl }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam m_defaultConnParam

            // Send Nop-Out and receive Nop-In
            let! ittNOP_1, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // send Text request with immidiate flag
            let sendData = Array.zeroCreate<byte> 256
            let! _ = r1.SendTextRequestPDU_Test id ( ValueSome( 100u, 100u ) ) g_CID0 false true false ValueNone g_LUN1 g_DefTTT sendData

            // Receive Reject PDU
            let! rejectPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejectPDU.Reason = RejectReasonCd.DATA_DIGEST_ERR ))

            // rewind CmdSN
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send Nop-Out and receive Nop-In
            let! ittNOP_2, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_2.InitiatorTaskTag = ittNOP_2 ))

            do! r1.CloseSession g_CID0 false
        }

    [<Fact>]
    member _.LoginRequestInFullFeaturePhase_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send Login request PDU as first PDU in full feature phase
            let loginRequest =
                {
                    T = false;
                    C = false;
                    CSG = LoginReqStateCd.SEQURITY;
                    NSG = LoginReqStateCd.SEQURITY;
                    VersionMax = 0x00uy;
                    VersionMin = 0x00uy;
                    ISID = isid_me.zero;
                    TSIH = tsih_me.zero;
                    InitiatorTaskTag = itt_me.fromPrim 0u;
                    CID = cid_me.zero;
                    CmdSN = cmdsn_me.zero;
                    ExpStatSN = statsn_me.zero;
                    TextRequest = [||];
                    ByteCount = 0u;
                }
            let conn = r1.Connection( g_CID0 ).Connection
            let mrdslt = r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_T
            let! _ = PDU.SendPDU( mrdslt, DigestType.DST_CRC32C, DigestType.DST_CRC32C, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), conn, loginRequest )
            conn.Flush()

            // Receive Reject PDU
            let! rejectPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejectPDU.Reason = RejectReasonCd.PROTOCOL_ERR ))

            do! r1.CloseSession g_CID0 false
        }

    [<Fact>]
    member _.LoginRequestInFullFeaturePhase_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send Nop-Out and receive Nop-In
            let! ittNOP_1, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // Send Login request PDU as second PDU in full feature phase
            let loginRequest =
                {
                    T = false;
                    C = false;
                    CSG = LoginReqStateCd.SEQURITY;
                    NSG = LoginReqStateCd.SEQURITY;
                    VersionMax = 0x00uy;
                    VersionMin = 0x00uy;
                    ISID = isid_me.zero;
                    TSIH = tsih_me.zero;
                    InitiatorTaskTag = itt_me.fromPrim 0u;
                    CID = cid_me.zero;
                    CmdSN = cmdsn_me.zero;
                    ExpStatSN = statsn_me.zero;
                    TextRequest = [||];
                    ByteCount = 0u;
                }
            let conn = r1.Connection( g_CID0 ).Connection
            let mrdslt = r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_T
            let! _ = PDU.SendPDU( mrdslt, DigestType.DST_CRC32C, DigestType.DST_CRC32C, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), conn, loginRequest )
            conn.Flush()

            // Receive Reject PDU
            let! rejectPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejectPDU.Reason = RejectReasonCd.PROTOCOL_ERR ))

            do! r1.CloseSession g_CID0 false
        }

    [<Fact>]
    member _.UnexpectedPDUInLoginPhase_001() =
        task {
            let conn = GlbFunc.ConnectToServer( iSCSIPortNo )

            // Send Nop-Out PDU
            let pdu = {
                I = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                PingData = PooledBuffer.Empty;
                ByteCount = 0u;
            }
            let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), conn, pdu )
            Assert.True(( conn.ReadByte() = -1 ))
        }

    [<Fact>]
    member _.UnexpectedPDUInLoginPhase_002() =
        task {
            let conn = GlbFunc.ConnectToServer( iSCSIPortNo )
            let objid = objidx_me.NewID()

            // Send first login request PDU
            let negoValue1 =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetName = TextValueType.Value( m_defaultSessParam.TargetName );
                        InitiatorName = TextValueType.Value( m_defaultSessParam.InitiatorName );
                        SessionType = TextValueType.Value( "Normal" );
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_None |] );
                }
            let negoStat1 =
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                        NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                        NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                }
            let loginRequest =
                {
                    T = false;
                    C = false;
                    CSG = LoginReqStateCd.SEQURITY;
                    NSG = LoginReqStateCd.SEQURITY;
                    VersionMax = 0x00uy;
                    VersionMin = 0x00uy;
                    ISID = isid_me.zero;
                    TSIH = tsih_me.zero;
                    InitiatorTaskTag = itt_me.fromPrim 0u;
                    CID = cid_me.zero;
                    CmdSN = cmdsn_me.zero;
                    ExpStatSN = statsn_me.zero;
                    TextRequest = IscsiTextEncode.CreateTextKeyValueString negoValue1 negoStat1;
                    ByteCount = 0u;
                }
            let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objid, conn, loginRequest )

            // Receive login response
            let! pdu1 = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, conn, Standpoint.Initiator )
            Assert.True(( pdu1.Opcode = OpcodeCd.LOGIN_RES ))

            // Send Nop-Out PDU
            let pdu = {
                I = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                PingData = PooledBuffer.Empty;
                ByteCount = 0u;
            }
            let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objid, conn, pdu )

            // In iSCSI specification, The target should have responded with a Login response PDU before disconnecting,
            // but is escalating to session recovery.

            // connection disconnected
            Assert.True(( conn.ReadByte() = -1 ))
        }

    // Send Nop-Out PDU in Discovery session
    [<Fact>]
    member _.UnexpectedPDUInDiscoverySession_001() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
                    TargetName = "";
            }
            let! r1 = iSCSI_Initiator.LoginForDiscoverySession sessParam m_defaultConnParam
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            try
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()
        }

    // Send SCSI Command PDU in Discovery session
    [<Fact>]
    member _.UnexpectedPDUInDiscoverySession_002() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
                    TargetName = "";
            }
            let! r1 = iSCSI_Initiator.LoginForDiscoverySession sessParam m_defaultConnParam
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy 0us false false
            let! _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u
            try
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()
        }

    // Send SCSI Data-Out PDU in Discovery session
    [<Fact>]
    member _.UnexpectedPDUInDiscoverySession_003() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
                    TargetName = "";
            }
            let! r1 = iSCSI_Initiator.LoginForDiscoverySession sessParam m_defaultConnParam
            do! r1.SendSCSIDataOutPDU g_CID0 true ( itt_me.fromPrim 1u ) g_LUN1 g_DefTTT datasn_me.zero 0u PooledBuffer.Empty
            try
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()
        }

    // Send TMF request PDU in Discovery session
    [<Fact>]
    member _.UnexpectedPDUInDiscoverySession_004() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
                    TargetName = "";
            }
            let! r1 = iSCSI_Initiator.LoginForDiscoverySession sessParam m_defaultConnParam
            let! _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 false TaskMgrReqCd.ABORT_TASK g_LUN1 ( itt_me.fromPrim 1u ) cmdsn_me.zero datasn_me.zero
            try
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()
        }

    // Send SNACK PDU in Discovery session
    [<Fact>]
    member _.UnexpectedPDUInDiscoverySession_005() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
                    TargetName = "";
            }
            let! r1 = iSCSI_Initiator.LoginForDiscoverySession sessParam m_defaultConnParam
            do! r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.STATUS g_LUN1 g_DefITT g_DefTTT 0u 1u
            try
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()
        }

    [<Fact>]
    member _.TextRequest_NegotiationReset_001() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    ErrorRecoveryLevel = 1uy;
                    MaxBurstLength = 3000u;
            }
            let connParam = {
                m_defaultConnParam with
                    MaxRecvDataSegmentLength_I = 1200u;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam connParam

            let accessLength = 8192u
            let accessBlockcount = accessLength / m_MediaBlockSize  // block size must be 512 or 4096
            let expRsult = [|
                ( 0u, 0u, 1200, false );
                ( 1u, 1200u, 1200, false );
                ( 2u, 2400u, 600, true );
                ( 3u, 3000u, 1200, false );
                ( 4u, 4200u, 1200, false );
                ( 5u, 5400u, 600, true );
                ( 6u, 6000u, 1200, false );
                ( 7u, 7200u, 992, true );
            |]

            // Send SCSI Read command
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 accessBlockcount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true true false TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength readCDB PooledBuffer.Empty 0u

            // Receive SCSI Data-In PDUs
            for i = 0 to expRsult.Length - 1 do
                let ( expdn, expOffset, expLength, expF ) = expRsult.[i]
                let! rpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu2.F = expF ))
                Assert.True(( rpdu2.A = expF ))
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.DataSN = datasn_me.fromPrim expdn ))
                Assert.True(( rpdu2.BufferOffset = expOffset ))
                Assert.True(( rpdu2.DataSegment.Count = expLength ))

            // Receive SCSI Response PDU
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // Note that at this point, no acknowledgement has been returned.
            r1.Connection( g_CID0 ).RewindExtStatSN ( statsn_me.fromPrim 1u )

            // Send text request. MaxRecvDataSegmentLength_I -> 1300
            let textRequest = 
                let negoValue1 = {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.Value( 1300u );
                }
                let negoStat1 = {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitSend;
                }
                IscsiTextEncode.CreateTextKeyValueString negoValue1 negoStat1
            let! itt4, _ = r1.SendTextRequestPDU g_CID0 false false false ValueNone g_LUN1 g_DefTTT textRequest

            // Receive text response PDU
            let! rpdu4 = r1.ReceiveSpecific<TextResponsePDU> g_CID0
            Assert.False(( rpdu4.F ))
            Assert.True(( rpdu4.InitiatorTaskTag = itt4 ))
            Assert.True(( rpdu4.TextResponse.Length = 0 ))

            // Send text request. Negotiation reset. ( TTT = 0xFFFFFFFF )
            let! _ = r1.SendTextRequestPDU g_CID0 false true false ( ValueSome itt4 ) g_LUN1 g_DefTTT [||]

            // Receive text response PDU
            let! rpdu5 = r1.ReceiveSpecific<TextResponsePDU> g_CID0
            Assert.True(( rpdu5.F ))
            Assert.True(( rpdu5.InitiatorTaskTag = itt4 ))
            Assert.True(( rpdu5.TextResponse.Length = 0 ))

            // Send R-SNACK request
            let! _ = r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.RDATA_SNACK g_LUN1 itt ( ttt_me.fromPrim 0x12345678u ) 0u 0u

            // Receive SCSI Data-In PDUs
            for i = 0 to expRsult.Length - 1 do
                let ( expdn, expOffset, expLength, expF ) = expRsult.[i]
                let! rpdu6 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu6.F = expF ))
                Assert.True(( rpdu6.A = expF ))
                Assert.True(( rpdu6.InitiatorTaskTag = itt ))
                Assert.True(( rpdu6.DataSN = datasn_me.fromPrim expdn ))
                Assert.True(( rpdu6.BufferOffset = expOffset ))
                Assert.True(( rpdu6.DataSegment.Count = expLength ))

            // Receive SCSI Response PDU
            let! rpdu7 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu7.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu7.SNACKTag = snacktag_me.fromPrim 0x12345678u ))

            r1.Connection( g_CID0 ).SkipExtStatSN ( statsn_me.fromPrim 1u )
            do! r1.CloseSession g_CID0 false
        }

    [<Fact>]
    member _.TextRequest_EmptyReset_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send empty text request.
            let! itt4, _ = r1.SendTextRequestPDU g_CID0 false false false ValueNone g_LUN1 g_DefTTT [||]

            // Receive text response PDU
            let! rpdu4 = r1.ReceiveSpecific<TextResponsePDU> g_CID0
            Assert.False(( rpdu4.F ))
            Assert.True(( rpdu4.InitiatorTaskTag = itt4 ))
            Assert.True(( rpdu4.TextResponse.Length = 0 ))

            // Send empty text request. F=1
            let! _ = r1.SendTextRequestPDU g_CID0 false true false ( ValueSome itt4 ) g_LUN1 g_DefTTT [||]

            // Receive text response PDU
            let! rpdu5 = r1.ReceiveSpecific<TextResponsePDU> g_CID0
            Assert.True(( rpdu5.F ))
            Assert.True(( rpdu5.InitiatorTaskTag = itt4 ))
            Assert.True(( rpdu5.TextResponse.Length = 0 ))

            do! r1.CloseSession g_CID0 false
        }

    [<Fact>]
    member _.Unknown_Opcode_001() =
        task {
            let connParam = { m_defaultConnParam with HeaderDigest = DigestType.DST_None }
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam connParam

            // Send PDU with unknown opcode
            let v = Array.zeroCreate<byte> 48
            for i = 0 to 63 do
                if not ( Array.exists ( (=) i ) [| 0x00; 0x01; 0x02; 0x03; 0x04; 0x05; 0x06; 0x10 |] ) then
                    v.[0] <- byte i
                    do! r1.Connection( g_CID0 ).Connection.WriteAsync( v, 0, 48 )

                    // Receive reject PDU
                    let! rpdu1 = r1.ReceiveSpecific<RejectPDU> g_CID0
                    Assert.True(( rpdu1.Reason = RejectReasonCd.COM_NOT_SUPPORT ))

            do! r1.CloseSession g_CID0 false
        }
