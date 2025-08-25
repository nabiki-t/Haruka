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
            // login
            let sessParam1 = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

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
            let sessParam1 = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 1
            let! _, cmdsn_0 = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            Assert.True(( cmdsn_0 = cmdsn_me.fromPrim 0u ))
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0

            // Nop-Out 2
            let sendData = PooledBuffer.RentAndInit 4096
            Random.Shared.NextBytes( sendData.ArraySegment.AsSpan() )
            // Destroys the value of the data segment
            let! _, cmdsn_1 = r1.SendNOPOutPDU_Test id ( ValueSome( 1024u, 2048u ) ) g_CID0 false g_LUN1 g_DefTTT sendData
            Assert.True(( cmdsn_1 = cmdsn_me.fromPrim 1u ))
            let! pdu1 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu1.Reason = RejectResonCd.DATA_DIGEST_ERR ))
            sendData.Return()

            // Rewind the initiator's CmdSN value
            r1.CmdSN <- cmdsn_0

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
            let sessParam1 = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 1
            let oldCmdSN1 = r1.CmdSN
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1_1.ExpCmdSN = cmdsn_me.fromPrim 1u ))

            // Rewind the initiator's CmdSN value
            r1.CmdSN <- oldCmdSN1

            // Re-send Nop-Out 1
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu1_2.Reason = RejectResonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu1_2.ExpCmdSN = cmdsn_me.fromPrim 1u ))

            // Nop-Out 2
            let oldCmdSN2 = r1.CmdSN
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2_1.ExpCmdSN = cmdsn_me.fromPrim 2u ))

            // Rewind the initiator's CmdSN value
            r1.CmdSN <- oldCmdSN2

            // Send TaskMgrReq with same CmdSN as Nop-Out 2
            let! _, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 false TaskMgrReqCd.ABORT_TASK g_LUN1 ( itt_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( datasn_me.fromPrim 0u )
            let! pdu2_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu2_2.Reason = RejectResonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu2_2.ExpCmdSN = cmdsn_me.fromPrim 2u ))

            // Nop-Out 3
            let oldCmdSN3 = r1.CmdSN
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu3_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu3_1.ExpCmdSN = cmdsn_me.fromPrim 3u ))

            // Rewind the initiator's CmdSN value
            r1.CmdSN <- oldCmdSN3

            // Send Logout request with same CmdSN as Nop-Out 3
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! pdu3_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu3_2.Reason = RejectResonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu3_2.ExpCmdSN = cmdsn_me.fromPrim 3u ))

            // Nop-Out 4
            let oldCmdSN4 = r1.CmdSN
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu4_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu4_1.ExpCmdSN = cmdsn_me.fromPrim 4u ))

            // Rewind the initiator's CmdSN value
            r1.CmdSN <- oldCmdSN4

            // Send SCSI Command with same CmdSN as Nop-Out 4
            let writeCDB = scsiWrite10CDB 0us
            let! _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
            let! pdu4_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu4_2.Reason = RejectResonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu4_2.ExpCmdSN = cmdsn_me.fromPrim 4u ))

            // Nop-Out 5
            let oldCmdSN5 = r1.CmdSN
            let! _, _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu5_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu5_1.ExpCmdSN = cmdsn_me.fromPrim 5u ))

            // Rewind the initiator's CmdSN value
            r1.CmdSN <- oldCmdSN5

            // Text request PDU with same CmdSN as Nop-Out 5
            let! _ = r1.SendTextRequestPDU g_CID0 false false false g_LUN1 g_DefTTT [||]
            let! pdu5_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu5_2.Reason = RejectResonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu5_2.ExpCmdSN = cmdsn_me.fromPrim 5u ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            ()
        }
