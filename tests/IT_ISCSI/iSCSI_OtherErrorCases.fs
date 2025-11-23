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
open Haruka.Client
open Haruka.Test

//=============================================================================
// Class implementation


[<CollectionDefinition( "iSCSI_OtherErrorCases" )>]
type iSCSI_OtherErrorCases_Fixture() =

    let m_iSCSIPortNo = GlbFunc.nextTcpPortNo()
    let m_MediaSize = 65536u

    // Add default configurations
    let AddDefaultConf( client : ClientProc ): unit =

        ///////////////////////////////
        // Target Device 0

        // Target device, Target group
        client.RunCommand "create" "Created" "CR> "
        client.RunCommand "select 0" "" "TD> "
        client.RunCommand ( sprintf "set MAXRECVDATASEGMENTLENGTH %d" 4096 ) "" "TD> "
        client.RunCommand ( sprintf "set MAXBURSTLENGTH %d" 16384 ) "" "TD> "
        client.RunCommand ( sprintf "set FIRSTBURSTLENGTH %d" 16384 ) "" "TD> "
        client.RunCommand "create targetgroup" "Created" "TD> "
        client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo ) "Created" "TD> "
        client.RunCommand "select 0" "" "TG> "

        // Target, LU
        client.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        client.RunCommand "select 0" "" "T > "
        client.RunCommand "create /l 1" "Created" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "LU> "

        client.RunCommand "validate" "All configurations are vlidated" "LU> "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "LU> "
        client.RunCommand "start" "Started" "LU> "

    // Start controller and client
    let m_Controller, m_Client =
        let workPath =
            let tempPath = Path.GetTempPath()
            Functions.AppendPathName tempPath ( Guid.NewGuid().ToString( "N" ) )
        let controllPortNo = GlbFunc.nextTcpPortNo()
        let controller, client = TestFunctions.StartHarukaController workPath controllPortNo
        AddDefaultConf client
        controller, client

    interface IDisposable with
        member _.Dispose (): unit =
            m_Client.Kill()

    interface ICollectionFixture<iSCSI_OtherErrorCases_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.iSCSIPortNo = m_iSCSIPortNo
    member _.MediaSize = m_MediaSize
    member _.MediaBlockSize = uint Constants.MEDIA_BLOCK_SIZE   // 4096 or 512 bytes


[<Collection( "iSCSI_OtherErrorCases" )>]     // Reuse existing test fixtures
type iSCSI_OtherErrorCases( fx : iSCSI_OtherErrorCases_Fixture ) =

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

    let m_ClientProc = fx.clientProc

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
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 65536u readCDB PooledBuffer.Empty 0u

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

            do! r1.CloseSession g_CID0 BitI.F
        }
        
    [<Fact>]
    member _.ResidualCount_Read_Underflow_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // send read command with block count = 0, but iSCSI ExpectedDataTransferLength = 65536
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy 0us false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 65536u readCDB PooledBuffer.Empty 0u

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.ResponseData.Count = 0 ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.U ))
            Assert.True(( rpdu.ResidualCount = 65536u ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.ResidualCount_Read_Overflow_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam { m_defaultConnParam with MaxRecvDataSegmentLength_I = 8192u }
            let blockCount = 8192u / m_MediaBlockSize

            // send read command with read size = 8192 bytes, but iSCSI ExpectedDataTransferLength = 512
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 512u readCDB PooledBuffer.Empty 0u

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

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.ResidualCount_Read_Overflow_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam { m_defaultConnParam with MaxRecvDataSegmentLength_I = 8192u }
            let blockCount = 8192u / m_MediaBlockSize

            // send read command with read size = 8192 bytes, but iSCSI ExpectedDataTransferLength = 0
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u readCDB PooledBuffer.Empty 0u

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.ResponseData.Count = 0 ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.O ))
            Assert.True(( rpdu.ResidualCount = 8192u ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.ResidualCount_BidirectionalCommand_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam { m_defaultConnParam with MaxRecvDataSegmentLength_I = 4096u }
            let blockCount = 4096u / m_MediaBlockSize

            // send write command with write size = 4096 bytes, but ExpectedDataTransferLength=4096 and BidirectionalExpectedReadDataLength=4096
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let writeData = PooledBuffer.RentAndInit 4096
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB writeData 4096u
            writeData.Return()

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.ResponseData.Count = 0 ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.u ))
            Assert.True(( rpdu.BidirectionalReadResidualCount = 4096u ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.ResidualCount_BidirectionalCommand_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam { m_defaultConnParam with MaxRecvDataSegmentLength_I = 4096u }
            let blockCount = 4096u / m_MediaBlockSize

            // send write command with write size = 4096 bytes, but ExpectedDataTransferLength=2048 and BidirectionalExpectedReadDataLength=4096
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let writeData = PooledBuffer.RentAndInit 4096
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 2048u writeCDB writeData 4096u
            writeData.Return()

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.ResponseData.Count = 0 ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.u ))
            Assert.False(( rpdu.U ))
            Assert.False(( rpdu.o ))
            Assert.True(( rpdu.O ))
            Assert.True(( rpdu.BidirectionalReadResidualCount = 4096u ))
            Assert.True(( rpdu.ResidualCount = 2048u ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.ResidualCount_BidirectionalCommand_003() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam { m_defaultConnParam with MaxRecvDataSegmentLength_I = 4096u }
            let blockCount = 4096u / m_MediaBlockSize

            // send read command with read size = 4096 bytes, but ExpectedDataTransferLength=1024 and BidirectionalExpectedReadDataLength=512
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let writeData = PooledBuffer.RentAndInit 4096
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 1024u readCDB writeData 512u
            writeData.Return()

            // receive SCSI Data-In PDU
            let! dpdu = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( dpdu.InitiatorTaskTag = itt ))
            Assert.True(( dpdu.DataSegment.Count = 512 ))

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.ResponseData.Count = 0 ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.False(( rpdu.u ))
            Assert.False(( rpdu.U ))
            Assert.True(( rpdu.o ))
            Assert.True(( rpdu.O ))
            Assert.True(( rpdu.BidirectionalReadResidualCount = 4096u - 512u ))
            Assert.True(( rpdu.ResidualCount = 3072u ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.ResidualCount_BidirectionalCommand_004() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam { m_defaultConnParam with MaxRecvDataSegmentLength_I = 4096u }
            let blockCount = 4096u / m_MediaBlockSize

            // send read command with read size = 4096 bytes, but ExpectedDataTransferLength=4096 and BidirectionalExpectedReadDataLength=5000
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let writeData = PooledBuffer.RentAndInit 4096
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 4096u readCDB writeData 5000u
            writeData.Return()

            // receive SCSI Data-In PDU
            let! dpdu = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( dpdu.InitiatorTaskTag = itt ))
            Assert.True(( dpdu.DataSegment.Count = 4096 ))

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.ResponseData.Count = 0 ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.u ))
            Assert.False(( rpdu.U ))
            Assert.False(( rpdu.o ))
            Assert.False(( rpdu.O ))
            Assert.True(( rpdu.ResidualCount = 0u ))
            Assert.True(( rpdu.BidirectionalReadResidualCount = 5000u - 4096u ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.Immidiate_NopOut_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            // Send Nop-Out with immidiate flag
            let! itt2, _ = r1.SendNOPOutPDU g_CID0 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive Nop-In PDU
            let! nopinPDU = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU.InitiatorTaskTag = itt2 ))

            // Send DataOut PDU
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T itt g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.Immidiate_SCSICommand_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! ittW, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            // send SCSI read command with immidiate flag
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! ittR, _ = r1.SendSCSICommandPDU g_CID0 BitI.T BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 4096u readCDB PooledBuffer.Empty 0u

            // receive Data-In PDU for SCSI read command
            let! datainPDU = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( datainPDU.InitiatorTaskTag = ittR ))

            // receive SCSI Response for SCSI read command
            let! respPDU_R = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( respPDU_R.InitiatorTaskTag = ittR ))
            Assert.True(( respPDU_R.Status = ScsiCmdStatCd.GOOD ))

            // Send DataOut PDU
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T ittW g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // receive SCSI Response for SCSI write command
            let! respPDU_W = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( respPDU_W.InitiatorTaskTag = ittW ))
            Assert.True(( respPDU_W.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.Immidiate_TaskManagementFunctionRequest_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // Send Nop-Out and receive Nop-In
            let! ittNOP, cmdsnNOP = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU.InitiatorTaskTag = ittNOP ))

            // send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! ittW, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            // send TMF request with immidiate flag
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.ABORT_TASK g_LUN1 ittNOP ( ValueSome cmdsnNOP ) datasn_me.zero

            // receive TMP response
            let! tmfPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfPDU.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Send DataOut PDU
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T ittW g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // receive SCSI Response for SCSI write command
            let! respPDU_W = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( respPDU_W.InitiatorTaskTag = ittW ))
            Assert.True(( respPDU_W.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.Immidiate_TextRequest_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! ittW, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            // send Text request with immidiate flag
            let! ittTX, _ = r1.SendTextRequestPDU g_CID0 BitI.T BitF.T BitC.F ValueNone g_LUN1 g_DefTTT [||]

            // receive text response
            let! txrPDU = r1.ReceiveSpecific<TextResponsePDU> g_CID0
            Assert.True(( txrPDU.F ))
            Assert.True(( txrPDU.InitiatorTaskTag = ittTX ))
            Assert.True(( txrPDU.TextResponse.Length = 0 ))

            // Send DataOut PDU
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T ittW g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // receive SCSI Response for SCSI write command
            let! respPDU_W = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( respPDU_W.InitiatorTaskTag = ittW ))
            Assert.True(( respPDU_W.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.Immidiate_Logout_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            do! r1.CloseSession g_CID0 BitI.T
        }

    [<Fact>]
    member _.Immidiate_SNACK_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession { m_defaultSessParam with ErrorRecoveryLevel = 1uy } m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN

            // Send Nop-Out and receive Nop-In
            let! ittNOP, cmdsnNOP = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU.InitiatorTaskTag = ittNOP ))
            Assert.True(( nopinPDU.StatSN = sendExpStatSN1 ))

            // rewind ExpStatSN
            r1.Connection( g_CID0 ).RewindExtStatSN( statsn_me.fromPrim 1u )

            // send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! ittW, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

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
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T ittW g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // receive SCSI Response for SCSI write command
            let! respPDU_W = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( respPDU_W.InitiatorTaskTag = ittW ))
            Assert.True(( respPDU_W.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 BitI.F
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
            let! _ = r1.SendNOPOutPDU_Test id ( ValueSome( 8u, 40u ) ) g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

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
            let! ittNOP_1, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // Send Nop-Out PDU with data digest error
            let sendData = PooledBuffer.RentAndInit 1024
            let! _ = r1.SendNOPOutPDU_Test id ( ValueSome( 100u, 100u ) ) g_CID0 BitI.F g_LUN1 g_DefTTT sendData
            sendData.Return()

            // Receive Reject PDU
            let! rejectPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejectPDU.Reason = RejectReasonCd.DATA_DIGEST_ERR ))

            // rewind CmdSN
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send Nop-Out and receive Nop-In
            let! ittNOP_2, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_2.InitiatorTaskTag = ittNOP_2 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Theory>]
    [<InlineData( 0uy )>]
    [<InlineData( 1uy )>]
    member _.DataDigestError_SCSICommand_001 ( erl : byte ) =
        task {
            let sessParam = { m_defaultSessParam with ErrorRecoveryLevel = erl }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam m_defaultConnParam

            // Send Nop-Out and receive Nop-In
            let! ittNOP_1, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // Send SCSI Command PDU with data digest error
            let sendData = PooledBuffer.RentAndInit 1024
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy 1us false false
            let! _ = r1.SendSCSICommandPDU_Test id ( ValueSome( 100u, 100u ) ) g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 1024u writeCDB sendData 0u
            sendData.Return()

            // Receive Reject PDU
            let! rejectPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejectPDU.Reason = RejectReasonCd.DATA_DIGEST_ERR ))

            // rewind CmdSN
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send Nop-Out and receive Nop-In
            let! ittNOP_2, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_2.InitiatorTaskTag = ittNOP_2 ))

            do! r1.CloseSession g_CID0 BitI.F
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
            let! ittNOP_1, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // Send SCSI Command PDU
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            // Send Data-Out PDU with data digest error
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU_Test id ( ValueSome( 100u, 100u ) ) g_CID0 BitF.T itt g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // Receive Reject PDU
            let! rejectPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejectPDU.Reason = RejectReasonCd.DATA_DIGEST_ERR ))

            // Re-Send Data-Out PDU
            let sendData = PooledBuffer.RentAndInit 4096
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T itt g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // Receive SCSI Response PDU
            let! respPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( respPDU.InitiatorTaskTag = itt ))
            Assert.True(( respPDU.Status = ScsiCmdStatCd.GOOD ))

            // Send Nop-Out and receive Nop-In
            let! ittNOP_2, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_2.InitiatorTaskTag = ittNOP_2 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Theory>]
    [<InlineData( 0uy )>]
    [<InlineData( 1uy )>]
    member _.DataDigestError_TextRequest_001 ( erl : byte ) =
        task {
            let sessParam = { m_defaultSessParam with ErrorRecoveryLevel = erl }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam m_defaultConnParam

            // Send Nop-Out and receive Nop-In
            let! ittNOP_1, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // send Text request with immidiate flag
            let sendData = Array.zeroCreate<byte> 256
            let! _ = r1.SendTextRequestPDU_Test id ( ValueSome( 100u, 100u ) ) g_CID0 BitI.F BitF.T BitC.F ValueNone g_LUN1 g_DefTTT sendData

            // Receive Reject PDU
            let! rejectPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejectPDU.Reason = RejectReasonCd.DATA_DIGEST_ERR ))

            // rewind CmdSN
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send Nop-Out and receive Nop-In
            let! ittNOP_2, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_2.InitiatorTaskTag = ittNOP_2 ))

            do! r1.CloseSession g_CID0 BitI.F
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

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.LoginRequestInFullFeaturePhase_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send Nop-Out and receive Nop-In
            let! ittNOP_1, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
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

            do! r1.CloseSession g_CID0 BitI.F
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
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
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
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u
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
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T ( itt_me.fromPrim 1u ) g_LUN1 g_DefTTT datasn_me.zero 0u PooledBuffer.Empty
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
            let! _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.F TaskMgrReqCd.ABORT_TASK g_LUN1 ( itt_me.fromPrim 1u ) ( ValueSome cmdsn_me.zero ) datasn_me.zero
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
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength readCDB PooledBuffer.Empty 0u

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
            let! itt4, _ = r1.SendTextRequestPDU g_CID0 BitI.F BitF.F BitC.F ValueNone g_LUN1 g_DefTTT textRequest

            // Receive text response PDU
            let! rpdu4 = r1.ReceiveSpecific<TextResponsePDU> g_CID0
            Assert.False(( rpdu4.F ))
            Assert.True(( rpdu4.InitiatorTaskTag = itt4 ))
            Assert.True(( rpdu4.TextResponse.Length = 0 ))

            // Send text request. Negotiation reset. ( TTT = 0xFFFFFFFF )
            let! _ = r1.SendTextRequestPDU g_CID0 BitI.F BitF.T BitC.F ( ValueSome itt4 ) g_LUN1 g_DefTTT [||]

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
            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.TextRequest_EmptyReset_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send empty text request.
            let! itt4, _ = r1.SendTextRequestPDU g_CID0 BitI.F BitF.F BitC.F ValueNone g_LUN1 g_DefTTT [||]

            // Receive text response PDU
            let! rpdu4 = r1.ReceiveSpecific<TextResponsePDU> g_CID0
            Assert.False(( rpdu4.F ))
            Assert.True(( rpdu4.InitiatorTaskTag = itt4 ))
            Assert.True(( rpdu4.TextResponse.Length = 0 ))

            // Send empty text request. F=1
            let! _ = r1.SendTextRequestPDU g_CID0 BitI.F BitF.T BitC.F ( ValueSome itt4 ) g_LUN1 g_DefTTT [||]

            // Receive text response PDU
            let! rpdu5 = r1.ReceiveSpecific<TextResponsePDU> g_CID0
            Assert.True(( rpdu5.F ))
            Assert.True(( rpdu5.InitiatorTaskTag = itt4 ))
            Assert.True(( rpdu5.TextResponse.Length = 0 ))

            do! r1.CloseSession g_CID0 BitI.F
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

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.ResponseFence_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send Nop-Out 1
            let! ittNopOut1, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive Nop-In PDU for Nop-Out 1
            let! nopIn1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopIn1.InitiatorTaskTag = ittNopOut1 ))

            // Rewind ExpStatSN ( Act as if no response to Nop-Out 1 has been received )
            r1.Connection( g_CID0 ).RewindExtStatSN ( statsn_me.fromPrim 1u )

            // Send SCSI write command ( it occurrs ACA )
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0xFFFFFFFEu 0uy 1us true false
            let sendData = PooledBuffer.Rent ( int m_MediaBlockSize )
            let! ittScsiCmd, _ =
                r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB sendData 0u
            sendData.Return()

            let mutable flg = true
            while flg do
                let lustat = m_ClientProc.RunCommandGetResp "lustatus" "LU> "
                flg <- lustat.[1].StartsWith "ACA : None"
                Threading.Thread.Sleep 100

            // Skip ExpStatSN ( Set the response to Nop-Out 1 )
            r1.Connection( g_CID0 ).SkipExtStatSN ( statsn_me.fromPrim 1u )

            // Send Nop-Out 2 ( Sends acknowledgement to Nop-Out 1 )
            let! ittNopOut2, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive SCSI Response PDU for SCSI write command
            let! scsiRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPDU.InitiatorTaskTag = ittScsiCmd ))

            // Send Nop-Out 3 ( Sends acknowledgement to SCSI Response )
            let! ittNopOut3, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive Nop-In PDU for Nop-Out 2
            let! nopIn2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopIn2.InitiatorTaskTag = ittNopOut2 ))

            // Receive Nop-In PDU for Nop-Out 3
            let! nopIn3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopIn3.InitiatorTaskTag = ittNopOut3 ))

            // clear ACA status
            let! _, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.F TaskMgrReqCd.CLEAR_ACA g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero
            let! tmdRespPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmdRespPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.ResponseFence_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send SCSI write command ( it occurrs ACA )
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0xFFFFFFFEu 0uy 1us true false
            let sendData = PooledBuffer.Rent ( int m_MediaBlockSize )
            let! ittScsiCmd, _ =
                r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB sendData 0u
            sendData.Return()

            // Receive SCSI Response PDU for SCSI write command
            let! scsiRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPDU.InitiatorTaskTag = ittScsiCmd ))

            // Send Nop-Out 1
            let! ittNopOut1, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive Nop-In PDU for Nop-Out 1
            let! nopIn1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopIn1.InitiatorTaskTag = ittNopOut1 ))

            // Rewind ExpStatSN ( Act as if no response to Nop-Out 1 has been received )
            r1.Connection( g_CID0 ).RewindExtStatSN ( statsn_me.fromPrim 1u )

            // clear ACA status
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.F TaskMgrReqCd.CLEAR_ACA g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            let mutable flg = true
            while flg do
                let lustat = m_ClientProc.RunCommandGetResp "lustatus" "LU> "
                flg <- lustat.[1].StartsWith "ACA : None" |> not
                Threading.Thread.Sleep 100

            // Skip ExpStatSN ( Set the response to Nop-Out 1 )
            r1.Connection( g_CID0 ).SkipExtStatSN ( statsn_me.fromPrim 1u )

            // Send Nop-Out 2 ( Sends acknowledgement to Nop-Out 1 )
            let! ittNopOut2, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive TMF response PDU for CLEAR_ACA TMF request.
            let! tmdRespPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmdRespPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))
            Assert.True(( tmdRespPDU.InitiatorTaskTag = ittTMF ))

            // Send Nop-Out 3 ( Sends acknowledgement to TMF Response )
            let! ittNopOut3, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive Nop-In PDU for Nop-Out 2
            let! nopIn2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopIn2.InitiatorTaskTag = ittNopOut2 ))

            // Receive Nop-In PDU for Nop-Out 3
            let! nopIn3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopIn3.InitiatorTaskTag = ittNopOut3 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.ResponseFence_003() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send SCSI write command ( it occurrs ACA )
            let writeCDB1 = GenScsiCDB.Write10 0uy false false false 0xFFFFFFFEu 0uy 1us true false
            let sendData1 = PooledBuffer.Rent ( int m_MediaBlockSize )
            let! ittScsiCmd1, _ =
                r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB1 sendData1 0u
            sendData1.Return()

            // Receive SCSI Response PDU for SCSI write command
            let! scsiRespPDU1 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPDU1.InitiatorTaskTag = ittScsiCmd1 ))
            Assert.True(( scsiRespPDU1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // Send Nop-Out 1
            let! ittNopOut1, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive Nop-In PDU for Nop-Out 1
            let! nopIn1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopIn1.InitiatorTaskTag = ittNopOut1 ))

            // Rewind ExpStatSN ( Act as if no response to Nop-Out 1 has been received )
            r1.Connection( g_CID0 ).RewindExtStatSN ( statsn_me.fromPrim 1u )

            // Send SCSI write command
            let writeCDB2 = GenScsiCDB.Write10 0uy false false false 0u 0uy 1us true false
            let sendData2 = PooledBuffer.Rent ( int m_MediaBlockSize )
            let! ittScsiCmd2, _ =
                r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB2 sendData2 0u
            sendData2.Return()

            Threading.Thread.Sleep 500

            // Skip ExpStatSN ( Set the response to Nop-Out 1 )
            r1.Connection( g_CID0 ).SkipExtStatSN ( statsn_me.fromPrim 1u )

            // Send Nop-Out 2 ( Sends acknowledgement to Nop-Out 1 )
            let! ittNopOut2, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive SCSI response for SCSI write command request.
            let! scsiRespPDU2 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPDU2.InitiatorTaskTag = ittScsiCmd2 ))
            Assert.True(( scsiRespPDU2.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // Send Nop-Out 3 ( Sends acknowledgement to SCSI response )
            let! ittNopOut3, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive Nop-In PDU for Nop-Out 2
            let! nopIn2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopIn2.InitiatorTaskTag = ittNopOut2 ))

            // Receive Nop-In PDU for Nop-Out 3
            let! nopIn3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopIn3.InitiatorTaskTag = ittNopOut3 ))


            // clear ACA status
            let! _, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.F TaskMgrReqCd.CLEAR_ACA g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero
            let! tmdRespPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmdRespPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.InvalidScsiCommand_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // send read command with read size = 4096 bytes, but R and W bits are both 0.
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 4096u readCDB PooledBuffer.Empty 0u

            // Format errors cause session recovery
            try
                let! _ = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()
        }

    [<Theory>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK )>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.ORDERED_TASK )>]
    [<InlineData( TaskATTRCd.HEAD_OF_QUEUE_TASK )>]
    member _.ScsiCommand_VariousTag_001 ( attr : TaskATTRCd ) =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // send read command
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F attr g_LUN1 4096u readCDB PooledBuffer.Empty 0u

            // receive SCSI Data-In PDU
            let! dpdu = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( dpdu.InitiatorTaskTag = itt ))
            Assert.True(( dpdu.DataSegment.Count = 4096 ))

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.ScsiCommand_ACATask_001 () =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // send read command
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.ACA_TASK g_LUN1 4096u readCDB PooledBuffer.Empty 0u

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( rpdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // clear ACA status
            let! _, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.F TaskMgrReqCd.CLEAR_ACA g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero
            let! tmdRespPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmdRespPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.ScsiCommand_ACATask_002 () =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // Send SCSI write command ( it occurrs ACA )
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0xFFFFFFFEu 0uy 1us true false
            let sendData = PooledBuffer.Rent ( int m_MediaBlockSize )
            let! ittWrite, _ =
                r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB sendData 0u
            sendData.Return()

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = ittWrite ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( rpdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // send read command with ACA task attribute
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.ACA_TASK g_LUN1 4096u readCDB PooledBuffer.Empty 0u

            // receive SCSI Data-In PDU
            let! dpdu = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( dpdu.InitiatorTaskTag = itt ))
            Assert.True(( dpdu.DataSegment.Count = 4096 ))

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // clear ACA status
            let! _, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.F TaskMgrReqCd.CLEAR_ACA g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero
            let! tmdRespPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmdRespPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.UnexpectedSNACKRequest_001 () =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession { m_defaultSessParam with ErrorRecoveryLevel=1uy } m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // Send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) true false
            let sendData = PooledBuffer.Rent 4096
            let! ittWrite, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB sendData 0u
            sendData.Return()

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = ittWrite ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // Send Nop-Out to acknowledge SCSI Response
            let! ittNopOut, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopInPDU = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopInPDU.InitiatorTaskTag = ittNopOut ))

            // Send SNACK request for already received status
            let! _ = r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.STATUS g_LUN1 g_DefITT g_DefTTT ( statsn_me.toPrim rpdu.StatSN ) 1u

            // receive Reject PDU
            let! rejPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejPDU.Reason = RejectReasonCd.PROTOCOL_ERR ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.UnexpectedSNACKRequest_002 () =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession { m_defaultSessParam with ErrorRecoveryLevel=1uy } m_defaultConnParam
            let blockCount = 4096u / m_MediaBlockSize

            // Send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0u 0uy ( uint16 blockCount ) true false
            let! ittWrite, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 4096u writeCDB PooledBuffer.Empty 0u

            // receive R2T PDU
            let! r2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2tPDU.InitiatorTaskTag = ittWrite ))

            // Send SNACK request for unsend R2T PDU
            let begrun = ( datasn_me.toPrim r2tPDU.R2TSN ) + 1u
            let! _ = r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.DATA_R2T g_LUN1 g_DefITT g_DefTTT begrun 1u

            // receive Reject PDU
            let! rejPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejPDU.Reason = RejectReasonCd.PROTOCOL_ERR ))

            // Send Data-Out PDU
            let sendData = PooledBuffer.Rent 4096
            let! _ = r1.SendSCSIDataOutPDU g_CID0 BitF.T ittWrite g_LUN1 r2tPDU.TargetTransferTag datasn_me.zero 0u sendData
            sendData.Return()

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = ittWrite ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.UnexpectedSNACKRequest_003 () =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession { m_defaultSessParam with ErrorRecoveryLevel=1uy } m_defaultConnParam
            let blockCount = 8192u / m_MediaBlockSize

            // send read command
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 8192u readCDB PooledBuffer.Empty 0u

            // Receive SCSI Data-In PDU 0
            let! dpdu1 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( dpdu1.InitiatorTaskTag = itt ))
            Assert.True(( dpdu1.BufferOffset = 0u ))
            Assert.True(( dpdu1.DataSegment.Count = 4096 ))
            Assert.True(( dpdu1.DataSN = datasn_me.fromPrim 0u ))

            // Receive SCSI Data-In PDU 1
            let! dpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( dpdu2.InitiatorTaskTag = itt ))
            Assert.True(( dpdu2.BufferOffset = 4096u ))
            Assert.True(( dpdu2.DataSegment.Count = 4096 ))
            Assert.True(( dpdu2.DataSN = datasn_me.fromPrim 1u ))

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // Send SNACK request for unsend Data-In PDU
            let! _ = r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.DATA_R2T g_LUN1 g_DefITT g_DefTTT 2u 1u

            // receive Reject PDU
            let! rejPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejPDU.Reason = RejectReasonCd.PROTOCOL_ERR ))

            // Send Nop-Out to acknowledge SCSI Response
            let! ittNopOut, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopInPDU = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopInPDU.InitiatorTaskTag = ittNopOut ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.UnexpectedSNACKRequest_004 () =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession { m_defaultSessParam with ErrorRecoveryLevel=1uy } m_defaultConnParam
            let blockCount = 8192u / m_MediaBlockSize

            // send read command
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy ( uint16 blockCount ) false false
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 8192u readCDB PooledBuffer.Empty 0u

            // Receive SCSI Data-In PDU 0
            let! dpdu1 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( dpdu1.InitiatorTaskTag = itt ))
            Assert.True(( dpdu1.BufferOffset = 0u ))
            Assert.True(( dpdu1.DataSegment.Count = 4096 ))
            Assert.True(( dpdu1.DataSN = datasn_me.fromPrim 0u ))

            // Receive SCSI Data-In PDU 1
            let! dpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( dpdu2.InitiatorTaskTag = itt ))
            Assert.True(( dpdu2.BufferOffset = 4096u ))
            Assert.True(( dpdu2.DataSegment.Count = 4096 ))
            Assert.True(( dpdu2.DataSN = datasn_me.fromPrim 1u ))

            // receive SCSI Response
            let! rpdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // Send acknowledgement for Data-In PDU 0 and 1
            let! _ = r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.DATA_ACK g_LUN1 g_DefITT g_DefTTT 0u 2u

            // Send SNACK request for acknowledged Data-In PDU
            let! _ = r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.DATA_R2T g_LUN1 g_DefITT g_DefTTT 1u 1u

            // receive Reject PDU
            let! rejPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejPDU.Reason = RejectReasonCd.PROTOCOL_ERR ))

            // Send Nop-Out to acknowledge SCSI Response
            let! ittNopOut, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopInPDU = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopInPDU.InitiatorTaskTag = ittNopOut ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.UnexpectedNopResponse_001 () =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send Nop-Out with unexpected ITT and TTT ( format error )
            let updater : ( NOPOutPDU -> NOPOutPDU ) = fun oldpdu -> {
                oldpdu with
                    InitiatorTaskTag = itt_me.fromPrim 0x11111111u
            }
            let! _ = r1.SendNOPOutPDU_Test updater ValueNone g_CID0 BitI.F g_LUN1 ( ttt_me.fromPrim 0x22222222u ) PooledBuffer.Empty

            // Format errors cause session recovery
            try
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()
        }

    // Even if a session recovery occurs, other sessions and LUs are not affected.
    [<Fact>]
    member _.SessionRecovery_001 () =
        task {
            // session 1
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // write data on session 1
            let writeData = Array.zeroCreate 8192
            Random.Shared.NextBytes( writeData )
            do! r1.WriteMediaData g_CID0 g_LUN1 0u m_MediaBlockSize writeData

            // session 2
            let! r2 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // read data on session 2
            let! readData1 = r2.ReadMediaData g_CID0 g_LUN1 0u ( 8192u / m_MediaBlockSize ) m_MediaBlockSize
            Assert.True(( writeData = readData1 ))

            // Establishing ACA status
            let writeCDB = GenScsiCDB.Write10 0uy false false false 0xFFFFFFFEu 0uy 1us true false
            let wsendData = PooledBuffer.Rent ( int m_MediaBlockSize )
            let! ittScsiCmd, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB wsendData 0u
            wsendData.Return()
            let! scsiRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPDU.InitiatorTaskTag = ittScsiCmd ))
            Assert.True(( scsiRespPDU.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( scsiRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // raise session recovery on session 1
            let! _ = r1.SendNOPOutPDU_Test id ( ValueSome( 8u, 40u ) ) g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            try
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()

            // check ACA status is retained after session recovery
            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy 1us true false
            let! itt, _ = r2.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) readCDB PooledBuffer.Empty 0u
            let! rpdu = r2.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu.InitiatorTaskTag = itt ))
            Assert.True(( rpdu.Status = ScsiCmdStatCd.ACA_ACTIVE ))
            Assert.True(( rpdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // reconnect session 1 with same ISID
            let sessParam = { m_defaultSessParam with ISID = r1.Params.ISID }
            let! r1_2 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam m_defaultConnParam cmdsn_me.zero

            // clear ACA status ( Notise that, CLEAR_ACA mast be requested by the fault initiator )
            let! ittTMF, _ = r1_2.SendTaskManagementFunctionRequestPDU g_CID0 BitI.F TaskMgrReqCd.CLEAR_ACA g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero
            let! tmdRespPDU = r1_2.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmdRespPDU.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmdRespPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // check media data is retained after session recovery
            let! readData1 = r2.ReadMediaData g_CID0 g_LUN1 0u ( 8192u / m_MediaBlockSize ) m_MediaBlockSize
            Assert.True(( writeData = readData1 ))

            // close
            do! r1_2.CloseSession g_CID0 BitI.F
            do! r2.CloseSession g_CID0 BitI.F
        }

    // Even if LU reset occurs, iSCSI sessions and are not affected.
    [<Fact>]
    member _.LUReset_001 () =
        task{
            // session 1
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // session 2
            let! r2 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // write data on session 1
            let writeData = Array.zeroCreate 8192
            Random.Shared.NextBytes( writeData )
            do! r1.WriteMediaData g_CID0 g_LUN1 0u m_MediaBlockSize writeData

            // read data on session 2
            let! readData1 = r2.ReadMediaData g_CID0 g_LUN1 0u ( 8192u / m_MediaBlockSize ) m_MediaBlockSize
            Assert.True(( writeData = readData1 ))

            // LU reset
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.F TaskMgrReqCd.LOGICAL_UNIT_RESET g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero
            let! tmdRespPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmdRespPDU.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmdRespPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Nop-Out on session 1
            let! ittNopOut1, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopInPDU1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopInPDU1.InitiatorTaskTag = ittNopOut1 ))

            // Nop-Out on session 2
            let! ittNopOut2, _ = r2.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopInPDU2 = r2.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopInPDU2.InitiatorTaskTag = ittNopOut2 ))

            // close
            do! r1.CloseSession g_CID0 BitI.F
            do! r2.CloseSession g_CID0 BitI.F
        }

    [<Theory>]
    [<InlineData( TaskMgrReqCd.ABORT_TASK )>]
    [<InlineData( TaskMgrReqCd.ABORT_TASK_SET )>]
    [<InlineData( TaskMgrReqCd.CLEAR_ACA )>]
    [<InlineData( TaskMgrReqCd.CLEAR_TASK_SET )>]
    [<InlineData( TaskMgrReqCd.LOGICAL_UNIT_RESET )>]
    [<InlineData( TaskMgrReqCd.TARGET_WARM_RESET )>]
    [<InlineData( TaskMgrReqCd.TARGET_COLD_RESET )>]
    member _.UnknownLUN_TMF_001 ( tmr : TaskMgrReqCd ) =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let errorLUN = lun_me.fromPrim 0xFFFFUL

            let! _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.F tmr errorLUN g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            try
                let! _ = r1.Receive g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    [<Fact>]
    member _.UnknownLUN_SCSICommand_001 () =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let errorLUN = lun_me.fromPrim 0xFFFFUL

            let readCDB = GenScsiCDB.Read10 0uy false false false 0u 0uy 1us false false
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK errorLUN 8192u readCDB PooledBuffer.Empty 0u

            try
                let! _ = r1.Receive g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    [<Fact>]
    member _.UnknownLUN_NOP_001 () =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let errorLUN = lun_me.fromPrim 0xFFFFUL

            // NOP does not use the LUN value
            let! ittNopOut1, _ = r1.SendNOPOutPDU g_CID0 BitI.F errorLUN g_DefTTT PooledBuffer.Empty
            let! nopInPDU1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopInPDU1.InitiatorTaskTag = ittNopOut1 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.UnknownLUN_TextRequest_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let errorLUN = lun_me.fromPrim 0xFFFFUL

            // Text-request and text-response does not use the LUN value
            let! itt4, _ = r1.SendTextRequestPDU g_CID0 BitI.F BitF.T BitC.F ValueNone errorLUN g_DefTTT [||]
            let! rpdu4 = r1.ReceiveSpecific<TextResponsePDU> g_CID0
            Assert.True(( rpdu4.F ))
            Assert.True(( rpdu4.InitiatorTaskTag = itt4 ))
            Assert.True(( rpdu4.TextResponse.Length = 0 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.UnknownLUN_SNACK_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession { m_defaultSessParam with ErrorRecoveryLevel = 1uy } m_defaultConnParam
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let errorLUN = lun_me.fromPrim 0xFFFFUL

            // Send Nop-Out and receive Nop-In
            let! ittNOP1, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU.InitiatorTaskTag = ittNOP1 ))
            Assert.True(( nopinPDU.StatSN = sendExpStatSN1 ))

            // rewind ExpStatSN
            r1.Connection( g_CID0 ).RewindExtStatSN( statsn_me.fromPrim 1u )

            // Send SNACK request with unknown LUN.
            // SNACK does not use the LUN value.
            do! r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.STATUS errorLUN g_DefITT g_DefTTT ( statsn_me.toPrim sendExpStatSN1 ) 1u

            // Receive Nop-In
            let! nopinPDU_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_2.InitiatorTaskTag = ittNOP1 ))
            Assert.True(( nopinPDU_2.StatSN = sendExpStatSN1 ))

            // skip ExpStatSN
            r1.Connection( g_CID0 ).SkipExtStatSN( statsn_me.fromPrim 1u )

            do! r1.CloseSession g_CID0 BitI.F
        }
