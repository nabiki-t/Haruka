namespace Haruka.Test.IT.ISCSI

open System
open System.Threading
open System.Threading.Tasks
open System.IO
open System.Diagnostics
open System.Net.Sockets

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.Test
open Xunit.Abstractions
open System.Text
open System.Net

[<CollectionDefinition( "iSCSI_Numbering" )>]
type iSCSI_Numbering_Fixture() =

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
        client.RunCommand "create debug" "Created" "LU> "
        client.RunCommand "select 0" "" "MD> "
        client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "MD> "

        client.RunCommand "validate" "All configurations are vlidated" "MD> "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "MD> "
        client.RunCommand "start" "Started" "MD> "
        client.RunCommand "add trap /e TestUnitReady /a Delay /ms 1000" "Trap added" "MD> "

        client.RunCommand "logout" "" "--> "

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

    interface ICollectionFixture<iSCSI_Numbering_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.iSCSIPortNo = m_iSCSIPortNo
    member _.MediaSize = m_MediaSize
    member _.MediaBlockSize = uint Constants.MEDIA_BLOCK_SIZE   // 4096 or 512 bytes


[<Collection( "iSCSI_Numbering" )>]
type iSCSI_Numbering( fx : iSCSI_Numbering_Fixture ) =

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

    let scsiWrite10CDB ( transferLength : uint16 ) =
        let w =
            ( int16 ) transferLength
            |> IPAddress.HostToNetworkOrder
            |> BitConverter.GetBytes
        [|
            0x2Auy;                         // OPERATION CODE( Write 10 )
            0x00uy;                         // WRPROTECT(000), DPO(0), FUA(0), FUA_NV(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LBA
            0x00uy;                         // GROUP NUMBER(0)
            w.[0]; w.[1];                   // TRANSFER LENGTH
            0x02uy;                         // NACA(1), LINK(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // padding
            0x00uy; 0x00uy;
        |]

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

    // Sequence of non-immediate commands.
    [<Fact>]
    member _.CmdSN_Sequense_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Not-Out
            for i = 0 to 9 do
                let! _, cmdsn = r1.SendNOPOutPDU g_CID0 false g_LUN1 ( ttt_me.fromPrim 0xFFFFFFFFu ) PooledBuffer.Empty
                Assert.True(( cmdsn = cmdsn_me.fromPrim( uint i ) ))
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                ()

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // The target discards the PDU and retransmits it.
    [<Fact>]
    member _.CmdSN_DiscardPDU_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out 1
            let! _, cmdsn_0 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            Assert.True(( cmdsn_0 = cmdsn_me.zero ))
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0

            // Nop-Out 2
            let sendData = PooledBuffer.RentAndInit 4096
            Random.Shared.NextBytes( sendData.ArraySegment.AsSpan() )
            // Destroys the value of the data segment
            let! _, cmdsn_1 = r1.SendNOPOutPDU_Test id ( ValueSome( 1024u, 2048u ) ) g_CID0 false g_LUN1 g_DefTTT sendData
            Assert.True(( cmdsn_1 = cmdsn_me.fromPrim 1u ))
            let! pdu1 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu1.Reason = RejectReasonCd.DATA_DIGEST_ERR ))
            sendData.Return()

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Re-send Nop-Out 2
            let sendData = PooledBuffer.RentAndInit 4096
            let! _, cmdsn_1_2 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT sendData
            Assert.True(( cmdsn_1_2 = cmdsn_me.fromPrim 1u ))
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            sendData.Return()

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Retransmission of PDUs with the same CmdSN. Nop-Out
    [<Fact>]
    member _.CmdSN_RetransPDU_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out 1
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1_1.ExpCmdSN = cmdsn_me.fromPrim 1u ))

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Re-send Nop-Out 1
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu1_2.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu1_2.ExpCmdSN = cmdsn_me.fromPrim 1u ))

            // Nop-Out 2
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2_1.ExpCmdSN = cmdsn_me.fromPrim 2u ))

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send TaskMgrReq with same CmdSN as Nop-Out 2
            let! _, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 false TaskMgrReqCd.ABORT_TASK g_LUN1 ( itt_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( datasn_me.zero )
            let! pdu2_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu2_2.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu2_2.ExpCmdSN = cmdsn_me.fromPrim 2u ))

            // Nop-Out 3
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu3_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu3_1.ExpCmdSN = cmdsn_me.fromPrim 3u ))

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send Logout request with same CmdSN as Nop-Out 3
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! pdu3_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu3_2.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu3_2.ExpCmdSN = cmdsn_me.fromPrim 3u ))

            // Nop-Out 4
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu4_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu4_1.ExpCmdSN = cmdsn_me.fromPrim 4u ))

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send SCSI Command with same CmdSN as Nop-Out 4
            let writeCDB = scsiWrite10CDB 0us
            let! _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
            let! pdu4_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu4_2.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu4_2.ExpCmdSN = cmdsn_me.fromPrim 4u ))

            // Nop-Out 5
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu5_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu5_1.ExpCmdSN = cmdsn_me.fromPrim 5u ))

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Text request PDU with same CmdSN as Nop-Out 5
            let! _ = r1.SendTextRequestPDU g_CID0 false false false g_LUN1 g_DefTTT [||]
            let! pdu5_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu5_2.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu5_2.ExpCmdSN = cmdsn_me.fromPrim 5u ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Send until CmdSN reaches MaxCmdSN.
    [<Fact>]
    member _.CmdSN_MaxCmdSN_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out 1
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1_1.ExpCmdSN = cmdsn_me.fromPrim 1u ))

            // Send SCSI Command PDU until CmdSN reaches MaxCmdSN.
            let pduCount = ( cmdsn_me.toPrim pdu1_1.MaxCmdSN ) - ( cmdsn_me.toPrim pdu1_1.ExpCmdSN ) + 1u |> int
            let vitt = Array.zeroCreate<ITT_T> pduCount
            for i = 1 to pduCount do
                let writeCDB = scsiWrite10CDB 1us
                let! itt, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
                vitt.[ i - 1 ] <- itt

                // Send immidiate Nop-Out PDU
                let! _, _ = r1.SendNOPOutPDU g_CID0 true g_LUN1 g_DefTTT PooledBuffer.Empty
                let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.True(( pdu2.ExpCmdSN = cmdsn_me.incr ( uint i ) pdu1_1.ExpCmdSN ))
                Assert.True(( pdu2.MaxCmdSN = pdu1_1.MaxCmdSN ))

            // Send Data-Out PDUs and receive SCSI Response PDUs
            let sendData = PooledBuffer.RentAndInit( int m_MediaBlockSize )
            for i = 0 to pduCount - 1 do
                let! _ = r1.SendSCSIDataOutPDU g_CID0 true vitt.[i] g_LUN1 g_DefTTT datasn_me.zero 0u sendData
                let! _ = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
                ()

            // Send Nop-Out PDU
            let! _, wcmdsn = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.MaxCmdSN = cmdsn_me.incr ( Constants.BDLU_MAX_TASKSET_SIZE + 1u ) wcmdsn ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Send CmdSN over MaxCmdSN.
    [<Fact>]
    member _.CmdSN_MaxCmdSN_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out 1
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1_1.ExpCmdSN = cmdsn_me.fromPrim 1u ))

            // Send SCSI Command PDU until CmdSN reaches MaxCmdSN.
            let pduCount = ( cmdsn_me.toPrim pdu1_1.MaxCmdSN ) - ( cmdsn_me.toPrim pdu1_1.ExpCmdSN ) + 1u |> int
            let vitt = Array.zeroCreate<ITT_T> pduCount
            for i = 1 to pduCount do
                let writeCDB = scsiWrite10CDB 1us
                let! itt, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
                vitt.[ i - 1 ] <- itt

            // Send additional SCSI Command PDU with CmdSN overs MaxCmdSN.
            let writeCDB = scsiWrite10CDB 1us
            let! _, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
            let! _ = r1.ReceiveSpecific<RejectPDU> g_CID0

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send Data-Out PDUs and receive SCSI Response PDUs
            let sendData = PooledBuffer.RentAndInit( int m_MediaBlockSize )
            for i = 0 to pduCount - 1 do
                let! _ = r1.SendSCSIDataOutPDU g_CID0 true vitt.[i] g_LUN1 g_DefTTT datasn_me.zero 0u sendData
                let! _ = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
                ()

            // Send Nop-Out PDU
            let! _, wcmdsn = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.MaxCmdSN = cmdsn_me.incr ( Constants.BDLU_MAX_TASKSET_SIZE + 1u ) wcmdsn ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    [<Fact>]
    member _.CmdSN_Rewind_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
            }
            let! r1 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam1 m_defaultConnParam ( cmdsn_me.fromPrim 0xFFFFFFFDu )

            // Send some of Nop-Out PDU
            let! _, cmdsn1 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1 = cmdsn_me.fromPrim 0xFFFFFFFDu ))
            Assert.True(( pdu1.ExpCmdSN = cmdsn_me.fromPrim 0xFFFFFFFEu ))

            let! _, cmdsn2 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn2 = cmdsn_me.fromPrim 0xFFFFFFFEu ))
            Assert.True(( pdu2.ExpCmdSN = cmdsn_me.fromPrim 0xFFFFFFFFu ))

            let! _, cmdsn3 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn3 = cmdsn_me.fromPrim 0xFFFFFFFFu ))
            Assert.True(( pdu3.ExpCmdSN = cmdsn_me.zero ))

            let! _, cmdsn4 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu4 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn4 = cmdsn_me.zero ))
            Assert.True(( pdu4.ExpCmdSN = cmdsn_me.fromPrim 1u ))

            let! _, cmdsn5 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu5 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn5 = cmdsn_me.fromPrim 1u ))
            Assert.True(( pdu5.ExpCmdSN = cmdsn_me.fromPrim 2u ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Send CmdSN in reverse order
    [<Fact>]
    member _.CmdSN_ReverseOder_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out 1
            let! _, cmdsn1 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1 = cmdsn_me.zero ))

            // skip CmdSN=1
            r1.SetNextCmdSN ( cmdsn_me.fromPrim 2u )

            // Nop-Out 3
            let sendData3 = PooledBuffer.RentAndInit 4096
            sendData3.Array.[0] <- 3uy
            let! _, cmdsn3 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT sendData3
            sendData3.Return()
            Assert.True(( cmdsn3 = cmdsn_me.fromPrim 2u ))

            // Rewind to CmdSN=1
            r1.SetNextCmdSN ( cmdsn_me.fromPrim 1u )

            // Nop-Out 2
            let sendData2 = PooledBuffer.RentAndInit 4096
            sendData2.Array.[0] <- 2uy
            let! _, cmdsn2 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT sendData2
            sendData2.Return()
            Assert.True(( cmdsn2 = cmdsn_me.fromPrim 1u ))

            // Receive Nop-In 2
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.ExpCmdSN = cmdsn_me.fromPrim 3u ))   // CmdSN=2 already received by the target
            Assert.True(( pdu2.PingData.Array.[0] = 2uy ))

            // Receive Nop-In 3
            let! pdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu3.ExpCmdSN = cmdsn_me.fromPrim 3u ))
            Assert.True(( pdu3.PingData.Array.[0] = 3uy ))

            // skip CmdSN=2
            r1.SetNextCmdSN ( cmdsn_me.fromPrim 3u )

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Multi sessions
    [<Fact>]
    member _.CmdSN_MultiSession_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let! r2 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out 1 at session 1
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Nop-Out 1 at session 2
            let! _, cmdsn2_1 = r2.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r2.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn2_1 = cmdsn_me.zero ))

            // Nop-Out 2 at session 1
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 1u ))

            // Nop-Out 2 at session 2
            let! _, cmdsn2_2 = r2.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r2.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 1u ))

            // Nop-Out 3 at session 1
            let! _, cmdsn1_3 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_3 = cmdsn_me.fromPrim 2u ))

            // Nop-Out 3 at session 2
            let! _, cmdsn2_2 = r2.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r2.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 2u ))

            // logout at session 1
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0

            // logout at session 2
            let! _ = r2.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r2.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Multi connections
    [<Fact>]
    member _.CmdSN_MultiConnections_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Nop-Out 1 at connection 1
            let! _, cmdsn2_1 = r1.SendNOPOutPDU g_CID1 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_1 = cmdsn_me.fromPrim 1u ))

            // Nop-Out 2 at connection 0
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 2u ))

            // Nop-Out 2 at connection 1
            let! _, cmdsn2_2 = r1.SendNOPOutPDU g_CID1 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 3u ))

            // Nop-Out 3 at connection 0
            let! _, cmdsn1_3 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_3 = cmdsn_me.fromPrim 4u ))

            // Nop-Out 3 at connection 1
            let! _, cmdsn2_2 = r1.SendNOPOutPDU g_CID1 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 5u ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Reconnecting a connection. session persists.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Nop-Out 1 at connection 1
            let! _, cmdsn2_1 = r1.SendNOPOutPDU g_CID1 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_1 = cmdsn_me.fromPrim 1u ))

            // logout connection 0
            let! _, cmdsn_logout = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( cmdsn_logout = cmdsn_me.fromPrim 2u ))
            r1.RemoveConnectionEntry g_CID0 |> ignore

            // Re-connect connection 0
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2 at connection 0
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 3u ))

            // Nop-Out 2 at connection 1
            let! _, cmdsn2_2 = r1.SendNOPOutPDU g_CID1 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 4u ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID1 false LogoutReqReasonCd.CLOSE_SESS g_CID1
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID1
            ()
        }
        
    // Reconnecting a connection. session persists.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Nop-Out 1 at connection 1
            let! _, cmdsn2_1 = r1.SendNOPOutPDU g_CID1 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_1 = cmdsn_me.fromPrim 1u ))

            // Drop connection 0
            r1.CloseConnection g_CID0

            // Re-connect connection 0
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2 at connection 0
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 2u ))

            // Nop-Out 2 at connection 1
            let! _, cmdsn2_2 = r1.SendNOPOutPDU g_CID1 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 3u ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID1 false LogoutReqReasonCd.CLOSE_SESS g_CID1
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID1
            ()
        }

    // Reconnecting a connection. session persists.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_003() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Nop-Out 1 at connection 1
            let! _, cmdsn2_1 = r1.SendNOPOutPDU g_CID1 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_1 = cmdsn_me.fromPrim 1u ))

            // Re-connect connection 0
            r1.RemoveConnectionEntry g_CID0 |> ignore
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2 at connection 0
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 2u ))

            // Nop-Out 2 at connection 1
            let! _, cmdsn2_2 = r1.SendNOPOutPDU g_CID1 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 3u ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID1 false LogoutReqReasonCd.CLOSE_SESS g_CID1
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID1
            ()
        }

    // Reconnecting a connection. session does not persist.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_004() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ErrorRecoveryLevel = 1uy;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 1
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // logout connection 0
            let! _, cmdsn_logout = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( cmdsn_logout = cmdsn_me.fromPrim 1u ))

            // Re-connect ( reuse ISID )
            let! r2 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 2
            let! _, cmdsn1_2 = r2.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r2.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.zero ))

            // logout
            let! _ = r2.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r2.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Reconnecting a connection. session does not persist.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_005() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ErrorRecoveryLevel = 1uy;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 1
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Drop connection 0
            r1.CloseConnection g_CID0

            // Re-connect ( reuse ISID )
            let! r2 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 2
            let! _, cmdsn1_2 = r2.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r2.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.zero ))

            // logout
            let! _ = r2.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r2.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Reconnecting a connection. session persists.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_006() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Send SCSI Command PDU at connection 0
            let writeCDB = scsiWrite10CDB 1us
            let! _, cmdsn_sc = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
            Assert.True(( cmdsn_sc = cmdsn_me.fromPrim 1u ))

            // Drop connection 0
            r1.CloseConnection g_CID0

            // Re-connect connection 0
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2 at connection 0
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! wpdu = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 2u ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID1 false LogoutReqReasonCd.CLOSE_SESS g_CID1
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID1
            ()
        }
        
    // Reconnecting a connection. session persists.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_007() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Send SCSI Command PDU at connection 0
            let writeCDB = scsiWrite10CDB 1us
            let! _, cmdsn_sc = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
            Assert.True(( cmdsn_sc = cmdsn_me.fromPrim 1u ))

            // Re-connect connection 0
            r1.RemoveConnectionEntry g_CID0 |> ignore
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2 at connection 0
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! wpdu = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 2u ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID1 false LogoutReqReasonCd.CLOSE_SESS g_CID1
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID1
            ()
        }

    // Sequence of non-immediate commands.
    [<Fact>]
    member _.StatSN_Sequense_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Not-Out
            for i = 0 to 9 do
                let sendExpStatSN = r1.Connection( g_CID0 ).ExpStatSN
                let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
                let! pdu = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.True(( pdu.StatSN = sendExpStatSN ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // the next command is sent without incrementing ExpStatSN.
    [<Fact>]
    member _.StatSN_LostResponse_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ErrorRecoveryLevel = 1uy;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Send Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let sendData1 = PooledBuffer.RentAndInit 4096
            sendData1.Array.[0] <- 1uy
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT sendData1
            sendData1.Return()

            // Receive Nop-In 1
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))
            Assert.True(( pdu1.PingData.Array.[0] = 1uy ))

            // rewind ExpStatSN
            r1.Connection( g_CID0 ).RewindExtStatSN( statsn_me.fromPrim 1u )

            // Send Nop-Out 2
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN  // same as sendExpStatSN1
            let sendData2 = PooledBuffer.RentAndInit 4096
            sendData2.Array.[0] <- 2uy
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT sendData2
            sendData2.Return()

            // Receive Nop-In 2
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = statsn_me.next sendExpStatSN2 ))
            Assert.True(( pdu2.PingData.Array.[0] = 2uy ))

            // Send SNACK request
            do! r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.STATUS g_LUN1 g_DefITT g_DefTTT ( statsn_me.toPrim sendExpStatSN1 ) 1u

            // Receive Nop-In 1
            let! pdu1_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1_2.StatSN = sendExpStatSN1 ))
            Assert.True(( pdu1_2.PingData.Array.[0] = 1uy ))

            // rewind ExpStatSN
            r1.Connection( g_CID0 ).SkipExtStatSN( statsn_me.fromPrim 1u )

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Add ExpStatSN before receiving status.
    [<Fact>]
    member _.StatSN_Skip_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ErrorRecoveryLevel = 1uy;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Send Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! itt1, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive Nop-In 1
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))
            Assert.True(( pdu1.InitiatorTaskTag = itt1 ))

            // skip ExpStatSN
            r1.Connection( g_CID0 ).SkipExtStatSN( statsn_me.fromPrim 1u )

            // Send Nop-Out 2
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN  // sendExpStatSN2 = sendExpStatSN1 + 2
            let! itt2, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive Nop-In 2
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = statsn_me.decr 1u sendExpStatSN2 ))
            Assert.True(( pdu2.InitiatorTaskTag = itt2 ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Resend request for received status.
    [<Fact>]
    member _.StatSN_Rewind_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ErrorRecoveryLevel = 1uy;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Send Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! itt1, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))
            Assert.True(( pdu1.InitiatorTaskTag = itt1 ))

            // Send Nop-Out 2
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN
            let! itt2, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))
            Assert.True(( pdu2.InitiatorTaskTag = itt2 ))

            // rewind ExpStatSN
            r1.Connection( g_CID0 ).RewindExtStatSN( statsn_me.fromPrim 2u )

            // Send Nop-Out 3
            let sendExpStatSN3 = r1.Connection( g_CID0 ).ExpStatSN  // sendExpStatSN3 = sendExpStatSN1
            let! itt3, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu3.StatSN = statsn_me.next sendExpStatSN2 ))
            Assert.True(( pdu3.InitiatorTaskTag = itt3 ))

            // Send SNACK request ( sendExpStatSN3 = sendExpStatSN1, Already acknowledged )
            do! r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.STATUS g_LUN1 g_DefITT g_DefTTT ( statsn_me.toPrim sendExpStatSN3 ) 1u
            let! pdu3 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu3.Reason = RejectReasonCd.PROTOCOL_ERR ))

            // Send SNACK request ( sendExpStatSN2 )
            do! r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.STATUS g_LUN1 g_DefITT g_DefTTT ( statsn_me.toPrim sendExpStatSN2 ) 2u

            // Receive Nop-In 2
            let! pdu2_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2_1.StatSN = sendExpStatSN2 ))
            Assert.True(( pdu2.InitiatorTaskTag = itt2 ))

            // Receive Nop-In 3
            let! pdu3_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu3_2.StatSN = statsn_me.next sendExpStatSN2 ))
            Assert.True(( pdu3_2.InitiatorTaskTag = itt3 ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Sequence of non-immediate commands.
    [<Fact>]
    member _.StatSN_Immidiate_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Not-Out 1 (non-immidiate)
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 ( ttt_me.fromPrim 0xFFFFFFFFu ) PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // Not-Out 2 (immidiate)
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN
            Assert.True(( sendExpStatSN2 = statsn_me.next sendExpStatSN1 ))
            let! _, _ = r1.SendNOPOutPDU g_CID0 true g_LUN1 ( ttt_me.fromPrim 0xFFFFFFFFu ) PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))

            // Not-Out 3 (non-immidiate)
            let sendExpStatSN3 = r1.Connection( g_CID0 ).ExpStatSN
            Assert.True(( sendExpStatSN3 = statsn_me.next sendExpStatSN2 ))
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 ( ttt_me.fromPrim 0xFFFFFFFFu ) PooledBuffer.Empty
            let! pdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu3.StatSN = sendExpStatSN3 ))

            // Not-Out 4 (immidiate)
            let sendExpStatSN4 = r1.Connection( g_CID0 ).ExpStatSN
            Assert.True(( sendExpStatSN4 = statsn_me.next sendExpStatSN3 ))
            let! _, _ = r1.SendNOPOutPDU g_CID0 true g_LUN1 ( ttt_me.fromPrim 0xFFFFFFFFu ) PooledBuffer.Empty
            let! pdu4 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu4.StatSN = sendExpStatSN4 ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Multi connections
    [<Fact>]
    member _.StatSN_MultiConnections_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let sendExpStatSN0_1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu0_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu0_1.StatSN = sendExpStatSN0_1 ))

            // Nop-Out 1 at connection 1
            let sendExpStatSN1_1 = r1.Connection( g_CID1 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID1 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_1 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( pdu1_1.StatSN = sendExpStatSN1_1 ))

            // Nop-Out 2 at connection 0
            let sendExpStatSN0_2 = r1.Connection( g_CID0 ).ExpStatSN
            Assert.True(( sendExpStatSN0_2 = statsn_me.next sendExpStatSN0_1 ))
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu0_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu0_2.StatSN = sendExpStatSN0_2 ))

            // Nop-Out 2 at connection 1
            let sendExpStatSN1_2 = r1.Connection( g_CID1 ).ExpStatSN
            Assert.True(( sendExpStatSN1_2 = statsn_me.next sendExpStatSN1_1 ))
            let! _ = r1.SendNOPOutPDU g_CID1 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_2 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( pdu1_2.StatSN = sendExpStatSN1_2 ))

            // Nop-Out 3 at connection 0
            let sendExpStatSN0_3 = r1.Connection( g_CID0 ).ExpStatSN
            Assert.True(( sendExpStatSN0_3 = statsn_me.next sendExpStatSN0_2 ))
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu0_3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu0_3.StatSN = sendExpStatSN0_3 ))

            // Nop-Out 3 at connection 1
            let sendExpStatSN1_3 = r1.Connection( g_CID1 ).ExpStatSN
            Assert.True(( sendExpStatSN1_3 = statsn_me.next sendExpStatSN1_2 ))
            let! _ = r1.SendNOPOutPDU g_CID1 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_3 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( pdu1_3.StatSN = sendExpStatSN1_3 ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Too little ExpStatSN
    [<Fact>]
    member _.StatSN_MaxStatSNDiff_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ErrorRecoveryLevel = 0uy;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // Nop-Out 2
            let tobeNextStatSN2 = statsn_me.next pdu1.StatSN
            let sendExpStatSN2 = statsn_me.decr Constants.MAX_STATSN_DIFF tobeNextStatSN2
            r1.Connection( g_CID0 ).SetNextExtStatSN sendExpStatSN2
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = statsn_me.next sendExpStatSN1 ))

            // Nop-Out 3
            let tobeNextStatSN3 = statsn_me.next pdu2.StatSN
            let sendExpStatSN3 = statsn_me.decr ( Constants.MAX_STATSN_DIFF + 1u ) tobeNextStatSN3
            r1.Connection( g_CID0 ).SetNextExtStatSN sendExpStatSN3
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            try
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    // Excessive ExpStatSN
    [<Fact>]
    member _.StatSN_MaxStatSNDiff_002() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ErrorRecoveryLevel = 0uy;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // Nop-Out 2
            let tobeNextStatSN2 = statsn_me.next pdu1.StatSN
            let sendExpStatSN2 = statsn_me.incr Constants.MAX_STATSN_DIFF tobeNextStatSN2
            r1.Connection( g_CID0 ).SetNextExtStatSN sendExpStatSN2
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = statsn_me.next sendExpStatSN1 ))

            // Nop-Out 3
            let tobeNextStatSN3 = statsn_me.next pdu2.StatSN
            let sendExpStatSN3 = statsn_me.incr ( Constants.MAX_STATSN_DIFF + 1u ) tobeNextStatSN3
            r1.Connection( g_CID0 ).SetNextExtStatSN sendExpStatSN3
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            try
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    // Reconnecting a connection. Reconnect after logout.
    [<Fact>]
    member _.StatSN_ReconnectConnection_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Some of Nop-Out
            for i = 0 to 10 do
                let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                ()

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // logout connection 0
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0

            // Re-connect connection 0
            r1.RemoveConnectionEntry g_CID0 |> ignore
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))

            // Reconnecting the connection resets StatSN back to the beginning.
            Assert.True(( statsn_me.lessThan sendExpStatSN2 sendExpStatSN1 ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Reconnecting a connection. Reconnect after drop the connection.
    [<Fact>]
    member _.StatSN_ReconnectConnection_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Some of Nop-Out
            for i = 0 to 10 do
                let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                ()

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // Drop connection 0
            r1.CloseConnection g_CID0

            // Re-connect connection 0
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))

            // Reconnecting the connection resets StatSN back to the beginning.
            Assert.True(( statsn_me.lessThan sendExpStatSN2 sendExpStatSN1 ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Reconnecting a connection.  Reconnect with implicit logout. Initial ExpStatSN=0.
    [<Fact>]
    member _.StatSN_ReconnectConnection_003() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Some of Nop-Out
            for i = 0 to 10 do
                let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                ()

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // This pattern is considered an implicit logout from the target's perspective.
            // However, since ExpStatSN is set to 0, the initiator considers this to be a reconnection after logging out.
            // Therefore, the target will accept the reconnection with StatSN reset to 0.

            // Re-connect connection 0
            r1.RemoveConnectionEntry g_CID0 |> ignore   // reset ExpStatSN value
            do! r1.AddConnection m_defaultConnParam     // start with ExpStatSN=0

            // Nop-Out 2
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))

            // Reconnecting the connection resets StatSN back to the beginning.
            Assert.True(( statsn_me.lessThan sendExpStatSN2 sendExpStatSN1 ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Reconnecting a connection.  Reconnect with implicit logout.
    [<Fact>]
    member _.StatSN_ReconnectConnection_004() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Some of Nop-Out
            for i = 0 to 10 do
                let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                ()

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // Re-connect connection 0
            do! r1.AddConnection m_defaultConnParam     // Inherits the value of ExpStatSN

            // Nop-Out 2
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))

            // StatSN value is not reset
            Assert.True(( statsn_me.lessThan sendExpStatSN1 sendExpStatSN2 ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }

    // Reconnecting a connection.  Reconnect with implicit logout. Fake ExpStatSN.
    [<Fact>]
    member _.StatSN_ReconnectConnection_005() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Some of Nop-Out
            for i = 0 to 10 do
                let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                ()

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // Re-connect connection 0
            r1.Connection( g_CID0 ).SkipExtStatSN( statsn_me.fromPrim 1u )
            try
                do! r1.AddConnection m_defaultConnParam     // Inherits the value of ExpStatSN
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()

            // logout. Connection 1 is still alive.
            let! _ = r1.SendLogoutRequestPDU g_CID1 false LogoutReqReasonCd.CLOSE_SESS g_CID1
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID1
            ()
        }
