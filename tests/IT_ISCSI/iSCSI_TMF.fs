//=============================================================================
// Haruka Software Storage.
// iSCSI_TMF.fs : Test cases for iSCSI task management functions.
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


[<CollectionDefinition( "iSCSI_TMF" )>]
type iSCSI_TMF_Fixture() =

    let m_iSCSIPortNo = GlbFunc.nextTcpPortNo()
    let m_MediaSize = 65536u

    // Add default configurations
    let AddDefaultConf( client : ClientProc ): unit =

        ///////////////////////////////
        // Target Device 0

        // Target device, Target group
        client.RunCommand "create" "Created" "CR> "
        client.RunCommand "select 0" "" "TD> "
        client.RunCommand "set MAXRECVDATASEGMENTLENGTH 4096" "" "TD> "
        client.RunCommand "set MAXBURSTLENGTH 16384" "" "TD> "
        client.RunCommand "set FIRSTBURSTLENGTH 16384" "" "TD> "
        client.RunCommand "set LOGLEVEL VERBOSE" "" "TD> "
        client.RunCommand "create targetgroup" "Created" "TD> "
        client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo ) "Created" "TD> "
        client.RunCommand "select 0" "" "TG> "

        // Target, LU1, LU2
        client.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        client.RunCommand "select 0" "" "T > "
        client.RunCommand "create /l 1" "Created" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand "create debug" "Created" "LU> "
        client.RunCommand "select 0" "" "MD> "
        client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "MD> "
        client.RunCommand "unselect" "" "LU> "
        client.RunCommand "unselect" "" "T > "
        client.RunCommand "create /l 2" "Created" "T > "
        client.RunCommand "select 1" "" "LU> "
        client.RunCommand "create debug" "Created" "LU> "
        client.RunCommand "select 0" "" "MD> "
        client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "MD> "
        client.RunCommand "unselect" "" "LU> "
        client.RunCommand "unselect" "" "T > "

        // publish, start target device
        client.RunCommand "validate" "All configurations are vlidated" "T > "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "T > "
        client.RunCommand "start" "Started" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand "select 0" "" "MD> "
        client.RunCommand "add trap /e Format /a Delay /ms 800" "Trap added." "MD> "
        client.RunCommand "unselect" "" "LU> "
        client.RunCommand "unselect" "" "T > "
        client.RunCommand "select 1" "" "LU> "
        client.RunCommand "select 0" "" "MD> "
        client.RunCommand "add trap /e Format /a Delay /ms 800" "Trap added." "MD> "
        client.RunCommand "unselect" "" "LU> "
        client.RunCommand "unselect" "" "T > "

    // Start controller and client
    let m_Controller, m_Client =
        let workPath =
            let tempPath = Path.GetTempPath()
            Functions.AppendPathName tempPath ( Guid.NewGuid().ToString( "N" ) )
        let controllPortNo = GlbFunc.nextTcpPortNo()
        let controller, client = ControllerFunc.StartHarukaController workPath controllPortNo
        AddDefaultConf client
        controller, client

    interface IDisposable with
        member _.Dispose (): unit =
            m_Client.Kill()

    interface ICollectionFixture<iSCSI_TMF_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.iSCSIPortNo = m_iSCSIPortNo
    member _.MediaSize = m_MediaSize
    member _.MediaBlockSize = uint Constants.MEDIA_BLOCK_SIZE   // 4096 or 512 bytes


[<Collection( "iSCSI_TMF" )>]     // Reuse existing test fixtures
type iSCSI_TMF( fx : iSCSI_TMF_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_CID1 = cid_me.fromPrim 1us
    let g_CID2 = cid_me.fromPrim 2us

    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL
    let g_LUN2 = lun_me.fromPrim 2UL

    let g_DefITT = itt_me.fromPrim 0xFFFFFFFFu
    let g_DefTTT = ttt_me.fromPrim 0xFFFFFFFFu

    let iSCSIPortNo = fx.iSCSIPortNo
    let m_MediaSize = fx.MediaSize
    let m_MediaBlockSize = fx.MediaBlockSize
    let m_BlkCnt1 = blkcnt_me.ofUInt16 1us

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

    let receiveTMFResponse ( r1 : iSCSI_Initiator ) =
        // Continue sending NOP-Out until a TMF response is received
        let rec loop ( tmf : TaskManagementFunctionResponsePDU voption, scnt : int, rcnt : int ) =
            task {
                let! pdu = r1.Receive g_CID0
                match pdu with
                | :? TaskManagementFunctionResponsePDU as tmdRespPDU ->
                    // After receiving a TMF response, send Nop-Output at least once.
                    let! _ = r1.SendNOPOut_PingRequest g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
                    return struct( true, ( ValueSome tmdRespPDU, scnt + 1, rcnt ) )

                | :? NOPInPDU ->
                    if tmf.IsSome then
                        // After receiving a TMF response, repeat until the same number of Nop-Ins as the number of Nop-Outs sent are received.
                        return struct( ( scnt > rcnt + 1 ), ( tmf, scnt, rcnt + 1 ) )
                    else
                        // Continue sending NOP-Out until a TMF response is received
                        let! _ = r1.SendNOPOut_PingRequest g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
                        return struct( true, ( tmf, scnt + 1, rcnt + 1 ) )
                | _ ->
                    return struct( false, ( ValueNone, 0, 0 ) )
            }
        Functions.loopAsyncWithState loop ( ValueNone, 1, 0 )


    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    // abort a task that is in the iSCSI task queue.
    [<Fact>]
    member _.TMF_AbortTask_001 () =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy m_BlkCnt1 NACA.T LINK.F
            let! ittWrite, cmdSNWrite = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB PooledBuffer.Empty 0u

            // Nop-Out 1
            let! _ = r1.SendNOPOut_PingRequest g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Send Abort Task TMF request for SCSI write command
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.ABORT_TASK g_LUN1 ittWrite ( ValueSome cmdSNWrite ) datasn_me.zero

            let! ( tmdRespPDU, _, _ ) = receiveTMFResponse r1
            Assert.True(( tmdRespPDU.IsSome ))
            Assert.True(( tmdRespPDU.Value.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmdRespPDU.Value.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // SCSI read
            let! readData1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData1.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // abort a task that is in the iSCSI task queue.
    [<Fact>]
    member _.TMF_AbortTask_002 () =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send SCSI write command ( Immidiate command )
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy m_BlkCnt1 NACA.T LINK.F
            let! ittWrite, _ = r1.SendSCSICommandPDU g_CID0 BitI.T BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB PooledBuffer.Empty 0u

            // Nop-Out 1
            let! _ = r1.SendNOPOut_PingRequest g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Send Abort Task TMF request for SCSI write command
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.ABORT_TASK g_LUN1 ittWrite ValueNone datasn_me.zero

            let! ( tmdRespPDU, _, _ ) = receiveTMFResponse r1
            Assert.True(( tmdRespPDU.IsSome ))
            Assert.True(( tmdRespPDU.Value.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmdRespPDU.Value.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // SCSI read
            let! readData1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 ( blkcnt_me.ofUInt16 1us ) m_MediaBlockSize
            Assert.True(( readData1.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // abort a task that is in the SCSI task set.
    [<Fact>]
    member _.TMF_AbortTask_003 () =

        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send SCSI format command
            let formatCDB = GenScsiCDB.FormatUnit FMTPINFO.F RTO_REQ.F LONGLIST.F FMTDATA.F CMPLIST.F 0uy NACA.T LINK.F
            let! ittFormat, cmdSNFormat = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u formatCDB PooledBuffer.Empty 0u

            // Send Abort Task TMF request for SCSI write command
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.ABORT_TASK g_LUN1 ittFormat ( ValueSome cmdSNFormat ) datasn_me.zero

            // receive TMF response
            let! tmfPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfPDU.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Wait for a response from the SCSI Format command.
            do! Task.Delay 600

            // SCSI read
            let! readData1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData1.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // abort a task that is in the SCSI task set.
    [<Fact>]
    member _.TMF_AbortTask_004 () =

        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send SCSI format command ( Immidiate command )
            let formatCDB = GenScsiCDB.FormatUnit FMTPINFO.F RTO_REQ.F LONGLIST.F FMTDATA.F CMPLIST.F 0uy NACA.T LINK.F
            let! ittFormat, _ = r1.SendSCSICommandPDU g_CID0 BitI.T BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u formatCDB PooledBuffer.Empty 0u

            // Send Abort Task TMF request for SCSI write command
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.ABORT_TASK g_LUN1 ittFormat ValueNone datasn_me.zero

            // receive TMF response
            let! tmfPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfPDU.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Wait for a response from the SCSI Format command.
            do! Task.Delay 600

            // SCSI read
            let! readData1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData1.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // abort TMF task itself.
    [<Fact>]
    member _.TMF_AbortTask_005 () =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send SCSI write command
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy m_BlkCnt1 NACA.T LINK.F
            let! ittWrite, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB PooledBuffer.Empty 0u

            // Send Abort Task TMF request for itself
            let updater = fun ( oldpdu : TaskManagementFunctionRequestPDU ) ->
                {
                    oldpdu with
                        ReferencedTaskTag = oldpdu.InitiatorTaskTag;
                }
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU_Test updater ValueNone g_CID0 BitI.T TaskMgrReqCd.ABORT_TASK g_LUN1 g_DefITT ValueNone datasn_me.zero

            // Receive TMF response
            let! tmfRespPdu = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfRespPdu.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfRespPdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Send Data-Out PDU
            let sendData = PooledBuffer.Rent ( int m_MediaBlockSize )
            let! _ = r1.SendSCSIDataOutPDU g_CID0 BitF.T ittWrite g_LUN1 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // Receive SCSI Response
            let! scsiRespPdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu.InitiatorTaskTag = ittWrite ))
            Assert.True(( scsiRespPdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( scsiRespPdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // SCSI read
            let! readData1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData1.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Abort request for a non-existent task.
    [<Fact>]
    member _.TMF_AbortTask_006 () =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            let referencedTaskTag = itt_me.fromPrim 0x12345678u
            let refCmdSN = cmdsn_me.fromPrim 0x87654321u |> ValueSome
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.ABORT_TASK g_LUN1 referencedTaskTag refCmdSN datasn_me.zero

            // Receive TMF response
            let! tmfRespPdu = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfRespPdu.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfRespPdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // SCSI read
            let! readData1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData1.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // abort a task that is in the iSCSI task queue.
    [<Fact>]
    member _.TMF_AbortTaskSet_001 () =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send SCSI write command to LU 1
            let writeCDB_lu1 = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy m_BlkCnt1 NACA.T LINK.F
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB_lu1 PooledBuffer.Empty 0u

            // Send Nop-Out 1 to LU 1
            let! _ = r1.SendNOPOut_PingRequest g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Send SCSI write command to LU 2
            let writeCDB_lu2 = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy m_BlkCnt1 NACA.T LINK.F
            let! ittWrite_lu2, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN2 ( uint m_MediaBlockSize ) writeCDB_lu2 PooledBuffer.Empty 0u

            // Send Abort Task Set TMF request to LU 1
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.ABORT_TASK_SET g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // Receive TMF response
            let! tmfRespPdu = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfRespPdu.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfRespPdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Send SCSI Data-Out PDU for LU2
            let sendData = PooledBuffer.Rent ( int m_MediaBlockSize )
            let! _ = r1.SendSCSIDataOutPDU g_CID0 BitF.T ittWrite_lu2 g_LUN2 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // Receive SCSI Response from LU 2
            let! scsiRespPdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu.InitiatorTaskTag = ittWrite_lu2 ))
            Assert.True(( scsiRespPdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( scsiRespPdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // SCSI read from LU 1
            let! readData1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData1.Length = int m_MediaBlockSize ))

            // SCSI read from LU 2
            let! readData2 = r1.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData2.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // abort a task that is in the SCSI task set.
    [<Fact>]
    member _.TMF_AbortTaskSet_002 () =

        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let! r2 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam { m_defaultConnParam with CID = g_CID1 }

            // Send SCSI format command from session 1 to LU 1
            let formatCDB_lu1 = GenScsiCDB.FormatUnit FMTPINFO.F RTO_REQ.F LONGLIST.F FMTDATA.F CMPLIST.F 0uy NACA.T LINK.F
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u formatCDB_lu1 PooledBuffer.Empty 0u

            // Send SCSI format command from session 2 to LU 1
            let! ittFormat_s2l1, _ = r2.SendSCSICommandPDU g_CID1 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u formatCDB_lu1 PooledBuffer.Empty 0u

            // Send SCSI format command to LU 2
            let formatCDB_lu2 = GenScsiCDB.FormatUnit FMTPINFO.F RTO_REQ.F LONGLIST.F FMTDATA.F CMPLIST.F 0uy NACA.T LINK.F
            let! ittFormat_s1l2, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN2 0u formatCDB_lu2 PooledBuffer.Empty 0u

            do! Task.Delay 50

            // Send Abort Task Set TMF request from session to LU 1
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.ABORT_TASK_SET g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // receive TMF response
            let! tmfPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfPDU.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Send Nop-Out and receive Nop-In ( Send acknowledge for receiving TMF response )
            let! ittNOP_1, _ = r1.SendNOPOut_PingRequest g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // Receive SCSI Response from LU 1
            let! scsiRespPdu_s2 = r2.ReceiveSpecific<SCSIResponsePDU> g_CID1
            Assert.True(( scsiRespPdu_s2.InitiatorTaskTag = ittFormat_s2l1 ))
            Assert.True(( scsiRespPdu_s2.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( scsiRespPdu_s2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // Receive SCSI Response from LU 2
            let! scsiRespPdu_s1 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu_s1.InitiatorTaskTag = ittFormat_s1l2 ))
            Assert.True(( scsiRespPdu_s1.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( scsiRespPdu_s1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // SCSI read from LU 1
            let! readData1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData1.Length = int m_MediaBlockSize ))

            // SCSI read from LU 2
            let! readData2 = r1.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData2.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
            do! r2.CloseSession g_CID1 BitI.F
        }

    // abort an aca task that is in the iSCSI task queue.
    [<Fact>]
    member _.TMF_ClearACA_001 () =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let sendData = PooledBuffer.Rent ( int m_MediaBlockSize )

            // Send SCSI write command to LU 1, this command establlish ACA.
            let writeCDB1_lu1 = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F ( blkcnt_me.ofUInt32 0xFFFFFFFEu ) 0uy m_BlkCnt1 NACA.T LINK.F
            let! ittWrite1_lu1, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB1_lu1 sendData 0u

            // Receive SCSI response PDU from LU 1
            let! scsiRespPdu1_lu1 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu1_lu1.InitiatorTaskTag = ittWrite1_lu1 ))
            Assert.True(( scsiRespPdu1_lu1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( scsiRespPdu1_lu1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // Send SCSI write command to LU 2, this command establlish ACA.
            let writeCDB1_lu2 = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F ( blkcnt_me.ofUInt32 0xFFFFFFFEu ) 0uy m_BlkCnt1 NACA.T LINK.F
            let! ittWrite1_lu2, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN2 ( uint m_MediaBlockSize ) writeCDB1_lu2 sendData 0u

            // Receive SCSI response PDU from LU 2
            let! scsiRespPdu1_lu2 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu1_lu2.InitiatorTaskTag = ittWrite1_lu2 ))
            Assert.True(( scsiRespPdu1_lu2.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( scsiRespPdu1_lu2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // Send SCSI write command to LU 1
            let writeCDB2_lu1 = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy m_BlkCnt1 NACA.T LINK.F
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.ACA_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB2_lu1 PooledBuffer.Empty 0u

            // Send SCSI write command to LU 2
            let writeCDB2_lu2 = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy m_BlkCnt1 NACA.T LINK.F
            let! ittWrite2_lu2, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.ACA_TASK g_LUN2 ( uint m_MediaBlockSize ) writeCDB2_lu2 PooledBuffer.Empty 0u

            // Send Clear ACA TMF request to LU 1
            let! ittTMF_lu1, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.CLEAR_ACA g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // Receive TMF response
            let! tmfRespPdu_lu1 = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfRespPdu_lu1.InitiatorTaskTag = ittTMF_lu1 ))
            Assert.True(( tmfRespPdu_lu1.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Send SCSI Data-Out PDU for LU2
            let! _ = r1.SendSCSIDataOutPDU g_CID0 BitF.T ittWrite2_lu2 g_LUN2 g_DefTTT datasn_me.zero 0u sendData

            // Receive SCSI Response from LU 2
            let! scsiRespPdu2_lu2 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu2_lu2.InitiatorTaskTag = ittWrite2_lu2 ))
            Assert.True(( scsiRespPdu2_lu2.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( scsiRespPdu2_lu2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // SCSI read from LU 1
            let! readData1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData1.Length = int m_MediaBlockSize ))

            // Send Clear ACA TMF request to LU 2
            let! ittTMF_lu2, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.CLEAR_ACA g_LUN2 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // Receive TMF response
            let! tmfRespPdu_lu2 = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfRespPdu_lu2.InitiatorTaskTag = ittTMF_lu2 ))
            Assert.True(( tmfRespPdu_lu2.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // SCSI read from LU 2
            let! readData2 = r1.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData2.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
            sendData.Return()
        }

    // abort ac ACA task that is in the SCSI task set.
    [<Fact>]
    member _.TMF_ClearACA_002 () =

        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let sendData = PooledBuffer.Rent ( int m_MediaBlockSize )

            // Send SCSI write command to LU 1, this command establlish ACA.
            let writeCDB1_lu1 = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F ( blkcnt_me.ofUInt32 0xFFFFFFFEu ) 0uy m_BlkCnt1 NACA.T LINK.F
            let! ittWrite1_lu1, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB1_lu1 sendData 0u

            // Receive SCSI response PDU from LU 1
            let! scsiRespPdu1_lu1 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu1_lu1.InitiatorTaskTag = ittWrite1_lu1 ))
            Assert.True(( scsiRespPdu1_lu1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( scsiRespPdu1_lu1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // Send SCSI write command to LU 2, this command establlish ACA.
            let writeCDB1_lu2 = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F ( blkcnt_me.ofUInt32 0xFFFFFFFEu ) 0uy m_BlkCnt1 NACA.T LINK.F
            let! ittWrite1_lu2, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN2 ( uint m_MediaBlockSize ) writeCDB1_lu2 sendData 0u

            // Receive SCSI response PDU from LU 2
            let! scsiRespPdu1_lu2 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu1_lu2.InitiatorTaskTag = ittWrite1_lu2 ))
            Assert.True(( scsiRespPdu1_lu2.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( scsiRespPdu1_lu2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // Send SCSI format command to LU 1
            let formatCDB_lu1 = GenScsiCDB.FormatUnit FMTPINFO.F RTO_REQ.F LONGLIST.F FMTDATA.F CMPLIST.F 0uy NACA.T LINK.F
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.ACA_TASK g_LUN1 0u formatCDB_lu1 PooledBuffer.Empty 0u

            // Send SCSI format command to LU 2
            let formatCDB_lu2 = GenScsiCDB.FormatUnit FMTPINFO.F RTO_REQ.F LONGLIST.F FMTDATA.F CMPLIST.F 0uy NACA.T LINK.F
            let! ittFormat_lu2, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.ACA_TASK g_LUN2 0u formatCDB_lu2 PooledBuffer.Empty 0u

            do! Task.Delay 50

            // Send Clear ACA TMF request to LU 1
            let! ittTMF_lu1, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.CLEAR_ACA g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // Receive TMF response
            let! tmfRespPdu_lu1 = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfRespPdu_lu1.InitiatorTaskTag = ittTMF_lu1 ))
            Assert.True(( tmfRespPdu_lu1.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Send Nop-Out and receive Nop-In ( Send acknowledge for receiving TMF response )
            let! ittNOP_1, _ = r1.SendNOPOut_PingRequest g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // Receive SCSI Response from LU 2
            let! formatRespPdu_lu2 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( formatRespPdu_lu2.InitiatorTaskTag = ittFormat_lu2 ))
            Assert.True(( formatRespPdu_lu2.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( formatRespPdu_lu2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // SCSI read from LU 1
            let! readData1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData1.Length = int m_MediaBlockSize ))

            // Send Clear ACA TMF request to LU 2
            let! ittTMF_lu2, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.CLEAR_ACA g_LUN2 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // Receive TMF response
            let! tmfRespPdu_lu2 = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfRespPdu_lu2.InitiatorTaskTag = ittTMF_lu2 ))
            Assert.True(( tmfRespPdu_lu2.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // SCSI read from LU 2
            let! readData2 = r1.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData2.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
            sendData.Return()
        }

    // abort a task that is in the iSCSI task queue.
    [<Fact>]
    member _.TMF_ClearTaskSet_001 () =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send SCSI write command to LU 1
            let writeCDB_lu1 = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy m_BlkCnt1 NACA.T LINK.F
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB_lu1 PooledBuffer.Empty 0u

            // Send Nop-Out 1 to LU 1
            let! _ = r1.SendNOPOut_PingRequest g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Send SCSI write command to LU 2
            let writeCDB_lu2 = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy m_BlkCnt1 NACA.T LINK.F
            let! ittWrite_lu2, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN2 ( uint m_MediaBlockSize ) writeCDB_lu2 PooledBuffer.Empty 0u

            // Send Clear Task Set TMF request to LU 1
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.CLEAR_TASK_SET g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // Receive TMF response
            let! tmfRespPdu = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfRespPdu.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfRespPdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Send SCSI Data-Out PDU for LU2
            let sendData = PooledBuffer.Rent ( int m_MediaBlockSize )
            let! _ = r1.SendSCSIDataOutPDU g_CID0 BitF.T ittWrite_lu2 g_LUN2 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // Receive SCSI Response from LU 2
            let! scsiRespPdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu.InitiatorTaskTag = ittWrite_lu2 ))
            Assert.True(( scsiRespPdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( scsiRespPdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // SCSI read from LU 1
            let! readData1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData1.Length = int m_MediaBlockSize ))

            // SCSI read from LU 2
            let! readData2 = r1.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData2.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // abort tasks that is in the SCSI task set.
    [<Fact>]
    member _.TMF_ClearTaskSet_002 () =

        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let! r2 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let formatCDB = GenScsiCDB.FormatUnit FMTPINFO.F RTO_REQ.F LONGLIST.F FMTDATA.F CMPLIST.F 0uy NACA.T LINK.F

            // Send SCSI format command to LU 1 on session 1
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u formatCDB PooledBuffer.Empty 0u

            // Send SCSI format command to LU 1 on session 2
            let! ittFormat_s2l1, _ = r2.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u formatCDB PooledBuffer.Empty 0u

            // Send SCSI format command to LU 2
            let! ittFormat_s1l2, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN2 0u formatCDB PooledBuffer.Empty 0u

            do! Task.Delay 50

            // Send Clear Task Set TMF request to LU 1
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.CLEAR_TASK_SET g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // receive TMF response
            let! tmfPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfPDU.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Send Nop-Out and receive Nop-In ( Send acknowledge for receiving TMF response )
            let! ittNOP_1, _ = r1.SendNOPOut_PingRequest g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // Receive SCSI Response from LU 1 on session 2
            let! scsiRespPdu_s2 = r2.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu_s2.InitiatorTaskTag = ittFormat_s2l1 ))
            Assert.True(( scsiRespPdu_s2.Status = ScsiCmdStatCd.TASK_ABORTED ))
            Assert.True(( scsiRespPdu_s2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // Receive SCSI Response from LU 2
            let! scsiRespPdu_s1 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu_s1.InitiatorTaskTag = ittFormat_s1l2 ))
            Assert.True(( scsiRespPdu_s1.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( scsiRespPdu_s1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // SCSI read from LU 1 on session 1
            let! readData_s1l1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s1l1.Length = int m_MediaBlockSize ))

            // SCSI read from LU 1 on session 2
            let! readData_s2l1 = r2.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s2l1.Length = int m_MediaBlockSize ))

            // SCSI read from LU 2
            let! readData_s1l2 = r1.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s1l2.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
            do! r2.CloseSession g_CID0 BitI.F
        }

    // abort tasks that is in the iSCSI task queue.
    [<Fact>]
    member _.TMF_LogicalUnitReset_001 () =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send SCSI write command to LU 1
            let writeCDB_lu1 = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy m_BlkCnt1 NACA.T LINK.F
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB_lu1 PooledBuffer.Empty 0u

            // Send Nop-Out 1 to LU 1
            let! _ = r1.SendNOPOut_PingRequest g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Send SCSI write command to LU 2
            let writeCDB_lu2 = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy m_BlkCnt1 NACA.T LINK.F
            let! ittWrite_lu2, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN2 ( uint m_MediaBlockSize ) writeCDB_lu2 PooledBuffer.Empty 0u

            // Send Logical Unit Reset TMF request to LU 1
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.LOGICAL_UNIT_RESET g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // Receive TMF response
            let! tmfRespPdu = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfRespPdu.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfRespPdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Send SCSI Data-Out PDU for LU2
            let sendData = PooledBuffer.Rent ( int m_MediaBlockSize )
            let! _ = r1.SendSCSIDataOutPDU g_CID0 BitF.T ittWrite_lu2 g_LUN2 g_DefTTT datasn_me.zero 0u sendData
            sendData.Return()

            // Receive SCSI Response from LU 2
            let! scsiRespPdu = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu.InitiatorTaskTag = ittWrite_lu2 ))
            Assert.True(( scsiRespPdu.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( scsiRespPdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // SCSI read from LU 1
            let! readData1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData1.Length = int m_MediaBlockSize ))

            // SCSI read from LU 2
            let! readData2 = r1.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData2.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F

            m_ClientProc.RunCommand "select 0" "" "LU> "
            m_ClientProc.RunCommand "select 0" "" "MD> "
            m_ClientProc.RunCommand "add trap /e Format /a Delay /ms 800" "Trap added." "MD> "
            m_ClientProc.RunCommand "unselect" "" "LU> "
            m_ClientProc.RunCommand "unselect" "" "T > "
        }

    // abort tasks that is in the SCSI task set.
    [<Fact>]
    member _.TMF_LogicalUnitReset_002 () =

        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let! r2 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let formatCDB = GenScsiCDB.FormatUnit FMTPINFO.F RTO_REQ.F LONGLIST.F FMTDATA.F CMPLIST.F 0uy NACA.T LINK.F

            // Send SCSI format command to LU 1 on session 1
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u formatCDB PooledBuffer.Empty 0u

            // Send SCSI format command to LU 1 on session 2
            let! ittFormat_s2l1, _ = r2.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u formatCDB PooledBuffer.Empty 0u

            // Send SCSI format command to LU 2
            let! ittFormat_s1l2, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN2 0u formatCDB PooledBuffer.Empty 0u

            do! Task.Delay 50

            // Send Logical Unit Reset TMF request to LU 1
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.LOGICAL_UNIT_RESET g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // receive TMF response
            let! tmfPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfPDU.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Send Nop-Out and receive Nop-In ( Send acknowledge for receiving TMF response )
            let! ittNOP_1, _ = r1.SendNOPOut_PingRequest g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // Receive SCSI Response from LU 1 on session 2
            let! scsiRespPdu_s2 = r2.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu_s2.InitiatorTaskTag = ittFormat_s2l1 ))
            Assert.True(( scsiRespPdu_s2.Status = ScsiCmdStatCd.TASK_ABORTED ))
            Assert.True(( scsiRespPdu_s2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // Receive SCSI Response from LU 2
            let! scsiRespPdu_s1 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu_s1.InitiatorTaskTag = ittFormat_s1l2 ))
            Assert.True(( scsiRespPdu_s1.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( scsiRespPdu_s1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // SCSI read from LU 1 on session 1
            let! readData_s1l1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s1l1.Length = int m_MediaBlockSize ))

            // SCSI read from LU 1 on session 2
            let! readData_s2l1 = r2.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s2l1.Length = int m_MediaBlockSize ))

            // SCSI read from LU 2
            let! readData_s1l2 = r1.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s1l2.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
            do! r2.CloseSession g_CID0 BitI.F

            m_ClientProc.RunCommand "select 0" "" "LU> "
            m_ClientProc.RunCommand "select 0" "" "MD> "
            m_ClientProc.RunCommand "add trap /e Format /a Delay /ms 800" "Trap added." "MD> "
            m_ClientProc.RunCommand "unselect" "" "LU> "
            m_ClientProc.RunCommand "unselect" "" "T > "
        }

    // Perform LU reset on LUN 0.
    [<Theory>]
    [<InlineData( TaskMgrReqCd.LOGICAL_UNIT_RESET )>]
    [<InlineData( TaskMgrReqCd.TARGET_WARM_RESET )>]    // The result will be the same
    [<InlineData( TaskMgrReqCd.TARGET_COLD_RESET )>]    // The result will be the same
    member _.TMF_LogicalUnitReset_003 ( tmr : TaskMgrReqCd ) =

        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let! r2 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let formatCDB = GenScsiCDB.FormatUnit FMTPINFO.F RTO_REQ.F LONGLIST.F FMTDATA.F CMPLIST.F 0uy NACA.T LINK.F

            // Send SCSI format command to LU 1 on session 1
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u formatCDB PooledBuffer.Empty 0u

            // Send SCSI format command to LU 2 on session 2
            let! _ = r2.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN2 0u formatCDB PooledBuffer.Empty 0u

            do! Task.Delay 50

            // Send Logical Unit Reset TMF request to LU 0
            let! _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T tmr g_LUN0 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // receive response
            try
                let! _ = r1.Receive g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()

            // receive response
            try
                let! _ = r2.Receive g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()

            // Reconnect
            let! r3 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // SCSI read from LU 1
            let! readData_s1l1 = r3.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s1l1.Length = int m_MediaBlockSize ))

            // SCSI read from LU 2
            let! readData_s1l2 = r3.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s1l2.Length = int m_MediaBlockSize ))

            do! r3.CloseSession g_CID0 BitI.F

            m_ClientProc.RunCommand "select 0" "" "LU> "
            m_ClientProc.RunCommand "select 0" "" "MD> "
            m_ClientProc.RunCommand "add trap /e Format /a Delay /ms 800" "Trap added." "MD> "
            m_ClientProc.RunCommand "unselect" "" "LU> "
            m_ClientProc.RunCommand "unselect" "" "T > "

            m_ClientProc.RunCommand "select 1" "" "LU> "
            m_ClientProc.RunCommand "select 0" "" "MD> "
            m_ClientProc.RunCommand "add trap /e Format /a Delay /ms 800" "Trap added." "MD> "
            m_ClientProc.RunCommand "unselect" "" "LU> "
            m_ClientProc.RunCommand "unselect" "" "T > "
        }

    [<Theory>]
    [<InlineData( TaskMgrReqCd.TARGET_WARM_RESET )>]
    [<InlineData( TaskMgrReqCd.TARGET_COLD_RESET )>]
    member _.TMF_TargetReset_001 ( tmr : TaskMgrReqCd ) =
        task{
            let sendData = PooledBuffer.Rent ( int m_MediaBlockSize )
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let! r2 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Send SCSI write command to LU 1 and 2 on session 1
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy m_BlkCnt1 NACA.T LINK.F
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB PooledBuffer.Empty 0u
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN2 ( uint m_MediaBlockSize ) writeCDB PooledBuffer.Empty 0u

            // Send SCSI write command to LU 1 and 2 on session 2
            let! ittWrite_s2l1, _ = r2.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 ( uint m_MediaBlockSize ) writeCDB PooledBuffer.Empty 0u
            let! ittWrite_s2l2, _ = r2.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN2 ( uint m_MediaBlockSize ) writeCDB PooledBuffer.Empty 0u

            // Send Target Reset TMF request to LU 1
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T tmr g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // Receive TMF response
            let! tmfRespPdu = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfRespPdu.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfRespPdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Send SCSI Data-Out PDU to LU1 on session 2
            let! _ = r2.SendSCSIDataOutPDU g_CID0 BitF.T ittWrite_s2l1 g_LUN1 g_DefTTT datasn_me.zero 0u sendData

            // Receive SCSI Response from LU1 on session 2
            let! scsiRespPdu1 = r2.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu1.InitiatorTaskTag = ittWrite_s2l1 ))
            Assert.True(( scsiRespPdu1.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( scsiRespPdu1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // Send SCSI Data-Out PDU to LU2 on session 2
            let! _ = r2.SendSCSIDataOutPDU g_CID0 BitF.T ittWrite_s2l2 g_LUN2 g_DefTTT datasn_me.zero 0u sendData

            // Receive SCSI Response from LU2 on session 2
            let! scsiRespPdu2 = r2.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( scsiRespPdu2.InitiatorTaskTag = ittWrite_s2l2 ))
            Assert.True(( scsiRespPdu2.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( scsiRespPdu1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // SCSI read from LU 1 and 2 on session 1
            let! readData_s1l1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s1l1.Length = int m_MediaBlockSize ))
            let! readData_s1l2 = r1.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s1l2.Length = int m_MediaBlockSize ))

            // SCSI read from LU 1 and 2 on session 2
            let! readData_s2l1 = r2.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s2l1.Length = int m_MediaBlockSize ))
            let! readData_s2l2 = r2.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s2l2.Length = int m_MediaBlockSize ))

            sendData.Return()
            do! r1.CloseSession g_CID0 BitI.F

            m_ClientProc.RunCommand "select 0" "" "LU> "
            m_ClientProc.RunCommand "select 0" "" "MD> "
            m_ClientProc.RunCommand "add trap /e Format /a Delay /ms 800" "Trap added." "MD> "
            m_ClientProc.RunCommand "unselect" "" "LU> "
            m_ClientProc.RunCommand "unselect" "" "T > "

            m_ClientProc.RunCommand "select 1" "" "LU> "
            m_ClientProc.RunCommand "select 0" "" "MD> "
            m_ClientProc.RunCommand "add trap /e Format /a Delay /ms 800" "Trap added." "MD> "
            m_ClientProc.RunCommand "unselect" "" "LU> "
            m_ClientProc.RunCommand "unselect" "" "T > "
        }

    [<Theory>]
    [<InlineData( TaskMgrReqCd.TARGET_WARM_RESET )>]
    [<InlineData( TaskMgrReqCd.TARGET_COLD_RESET )>]
    member _.TMF_TargetReset_002 ( tmr : TaskMgrReqCd ) =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let! r2 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let formatCDB = GenScsiCDB.FormatUnit FMTPINFO.F RTO_REQ.F LONGLIST.F FMTDATA.F CMPLIST.F 0uy NACA.T LINK.F

            // Send SCSI format command to LU 1 and 2 on session 1
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u formatCDB PooledBuffer.Empty 0u
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN2 0u formatCDB PooledBuffer.Empty 0u

            // Send SCSI format command to LU 1 and LU 2 on session 2
            let! ittFormat_s2l1, _ = r2.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u formatCDB PooledBuffer.Empty 0u
            let! ittFormat_s2l2, _ = r2.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.F TaskATTRCd.SIMPLE_TASK g_LUN2 0u formatCDB PooledBuffer.Empty 0u

            do! Task.Delay 50

            // Send Target Reset TMF request
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T tmr g_LUN2 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // receive TMF response
            let! tmfPDU = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfPDU.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfPDU.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Receive SCSI Response on session 2
            let! scsiRespPdu_s2_1 = r2.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( ( scsiRespPdu_s2_1.InitiatorTaskTag = ittFormat_s2l1 ) || ( scsiRespPdu_s2_1.InitiatorTaskTag = ittFormat_s2l2 ) ))
            Assert.True(( scsiRespPdu_s2_1.Status = ScsiCmdStatCd.TASK_ABORTED ))
            Assert.True(( scsiRespPdu_s2_1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            let! scsiRespPdu_s2_2 = r2.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( ( scsiRespPdu_s2_2.InitiatorTaskTag = ittFormat_s2l1 ) || ( scsiRespPdu_s2_2.InitiatorTaskTag = ittFormat_s2l2 ) ))
            Assert.True(( scsiRespPdu_s2_2.Status = ScsiCmdStatCd.TASK_ABORTED ))
            Assert.True(( scsiRespPdu_s2_2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))

            // Send Nop-Out and receive Nop-In ( Send acknowledge for receiving TMF response )
            let! ittNOP_1, _ = r1.SendNOPOut_PingRequest g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! nopinPDU_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( nopinPDU_1.InitiatorTaskTag = ittNOP_1 ))

            // SCSI read from LU 1 and 2 on session 1
            let! readData_s1l1 = r1.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s1l1.Length = int m_MediaBlockSize ))
            let! readData_s1l2 = r1.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s1l2.Length = int m_MediaBlockSize ))

            // SCSI read from LU 1 and 2 on session 2
            let! readData_s2l1 = r2.ReadMediaData g_CID0 g_LUN1 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s2l1.Length = int m_MediaBlockSize ))
            let! readData_s2l2 = r2.ReadMediaData g_CID0 g_LUN2 blkcnt_me.zero32 m_BlkCnt1 m_MediaBlockSize
            Assert.True(( readData_s2l2.Length = int m_MediaBlockSize ))

            do! r1.CloseSession g_CID0 BitI.F
            do! r2.CloseSession g_CID0 BitI.F

            m_ClientProc.RunCommand "select 0" "" "LU> "
            m_ClientProc.RunCommand "select 0" "" "MD> "
            m_ClientProc.RunCommand "add trap /e Format /a Delay /ms 800" "Trap added." "MD> "
            m_ClientProc.RunCommand "unselect" "" "LU> "
            m_ClientProc.RunCommand "unselect" "" "T > "

            m_ClientProc.RunCommand "select 1" "" "LU> "
            m_ClientProc.RunCommand "select 0" "" "MD> "
            m_ClientProc.RunCommand "add trap /e Format /a Delay /ms 800" "Trap added." "MD> "
            m_ClientProc.RunCommand "unselect" "" "LU> "
            m_ClientProc.RunCommand "unselect" "" "T > "

        }

    [<Theory>]
    [<InlineData( 0uy, 0uy )>]
    [<InlineData( 1uy, 1uy )>]
    [<InlineData( 2uy, 1uy )>]
    member _.TMF_TaskReAssign_001 ( erl1 : byte ) ( erl2 : byte ) =
        task{
            let! r1 = iSCSI_Initiator.CreateInitialSession { m_defaultSessParam with ErrorRecoveryLevel = erl1 } m_defaultConnParam
            Assert.True(( r1.Params.ErrorRecoveryLevel = erl2 ))

            // Send Task Re-Assign TMF request
            let! ittTMF, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.T TaskMgrReqCd.TASK_REASSIGN g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero

            // Receive TMF response
            let! tmfRespPdu = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( tmfRespPdu.InitiatorTaskTag = ittTMF ))
            Assert.True(( tmfRespPdu.Response = TaskMgrResCd.TASK_REASSIGN_NOT_SUPPORT ))

            do! r1.CloseSession g_CID0 BitI.F
        }


