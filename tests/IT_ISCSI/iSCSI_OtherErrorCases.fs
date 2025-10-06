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
            let! ittTX, _ = r1.SendTextRequestPDU g_CID0 true true false g_LUN1 g_DefTTT [||]

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
            let! _ = r1.SendTextRequestPDU_Test id ( ValueSome( 100u, 100u ) ) g_CID0 false true false g_LUN1 g_DefTTT sendData

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