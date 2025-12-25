//=============================================================================
// Haruka Software Storage.
// iSCSI_Numbering.fs : Test cases for iSCSI numbering.
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
        let controller, client = ControllerFunc.StartHarukaController workPath controllPortNo
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
        TSIH = tsih_me.zero;
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

    let CreateSession ( argErrorRecoveryLevel : byte ) ( argMaxBurstLength : uint ) ( argMaxRecvDataSegmentLength_I : uint ) : Task<iSCSI_Initiator> =
        task {
            let sessParam = {
                m_defaultSessParam with
                    MaxBurstLength = argMaxBurstLength;
                    ErrorRecoveryLevel = argErrorRecoveryLevel;
            }
            let connParam = {
                m_defaultConnParam with
                    MaxRecvDataSegmentLength_I = argMaxRecvDataSegmentLength_I
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam connParam
            Assert.True(( r1.Params.MaxBurstLength = argMaxBurstLength ))
            Assert.True(( r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_I = argMaxRecvDataSegmentLength_I ))
            Assert.True(( r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_T = 4096u ))
            return r1
        }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    // Sequence of non-immediate commands.
    [<Fact>]
    member _.CmdSN_Sequense_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Not-Out
            for i = 0 to 9 do
                let! _, cmdsn = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
                Assert.True(( cmdsn = cmdsn_me.fromPrim( uint i ) ))
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                ()

            do! r1.CloseSession g_CID0 BitI.F
        }

    // The target discards the PDU and retransmits it.
    [<Fact>]
    member _.CmdSN_DiscardPDU_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out 1
            let! _, cmdsn_0 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            Assert.True(( cmdsn_0 = cmdsn_me.zero ))
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0

            // Nop-Out 2
            let sendData = PooledBuffer.RentAndInit 4096
            Random.Shared.NextBytes( sendData.ArraySegment.AsSpan() )
            // Destroys the value of the data segment
            let! _, cmdsn_1 = r1.SendNOPOutPDU_Test id ( ValueSome( 1024u, 2048u ) ) g_CID0 BitI.F g_LUN1 g_DefTTT sendData
            Assert.True(( cmdsn_1 = cmdsn_me.fromPrim 1u ))
            let! pdu1 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu1.Reason = RejectReasonCd.DATA_DIGEST_ERR ))
            sendData.Return()

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Re-send Nop-Out 2
            let sendData = PooledBuffer.RentAndInit 4096
            let! _, cmdsn_1_2 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT sendData
            Assert.True(( cmdsn_1_2 = cmdsn_me.fromPrim 1u ))
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            sendData.Return()

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Retransmission of PDUs with the same CmdSN. Nop-Out
    [<Fact>]
    member _.CmdSN_RetransPDU_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out 1
            let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1_1.ExpCmdSN = cmdsn_me.fromPrim 1u ))

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Re-send Nop-Out 1
            let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu1_2.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu1_2.ExpCmdSN = cmdsn_me.fromPrim 1u ))

            // Nop-Out 2
            let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2_1.ExpCmdSN = cmdsn_me.fromPrim 2u ))

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send TaskMgrReq with same CmdSN as Nop-Out 2
            let! _, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.F TaskMgrReqCd.ABORT_TASK g_LUN1 ( itt_me.fromPrim 1u ) ( ValueSome ( cmdsn_me.fromPrim 1u ) ) datasn_me.zero
            let! pdu2_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu2_2.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu2_2.ExpCmdSN = cmdsn_me.fromPrim 2u ))

            // Nop-Out 3
            let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu3_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu3_1.ExpCmdSN = cmdsn_me.fromPrim 3u ))

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send Logout request with same CmdSN as Nop-Out 3
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! pdu3_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu3_2.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu3_2.ExpCmdSN = cmdsn_me.fromPrim 3u ))

            // Nop-Out 4
            let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu4_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu4_1.ExpCmdSN = cmdsn_me.fromPrim 4u ))

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send SCSI Command with same CmdSN as Nop-Out 4
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy 0us NACA.F LINK.F
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
            let! pdu4_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu4_2.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu4_2.ExpCmdSN = cmdsn_me.fromPrim 4u ))

            // Nop-Out 5
            let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu5_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu5_1.ExpCmdSN = cmdsn_me.fromPrim 5u ))

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Text request PDU with same CmdSN as Nop-Out 5
            let! _ = r1.SendTextRequestPDU g_CID0 BitI.F BitF.F BitC.F ValueNone g_LUN1 g_DefTTT [||]
            let! pdu5_2 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( pdu5_2.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
            Assert.True(( pdu5_2.ExpCmdSN = cmdsn_me.fromPrim 5u ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Send until CmdSN reaches MaxCmdSN.
    [<Fact>]
    member _.CmdSN_MaxCmdSN_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out 1
            let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1_1.ExpCmdSN = cmdsn_me.fromPrim 1u ))

            // Send SCSI Command PDU until CmdSN reaches MaxCmdSN.
            let pduCount = ( cmdsn_me.toPrim pdu1_1.MaxCmdSN ) - ( cmdsn_me.toPrim pdu1_1.ExpCmdSN ) + 1u |> int
            let vitt = Array.zeroCreate<ITT_T> pduCount
            for i = 1 to pduCount do
                let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy 1us NACA.F LINK.F
                let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
                vitt.[ i - 1 ] <- itt

                // Send immidiate Nop-Out PDU
                let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.True(( pdu2.ExpCmdSN = cmdsn_me.incr ( uint i ) pdu1_1.ExpCmdSN ))
                Assert.True(( pdu2.MaxCmdSN = pdu1_1.MaxCmdSN ))

            // Send Data-Out PDUs and receive SCSI Response PDUs
            let sendData = PooledBuffer.RentAndInit( int m_MediaBlockSize )
            for i = 0 to pduCount - 1 do
                do! r1.SendSCSIDataOutPDU g_CID0 BitF.T vitt.[i] g_LUN1 g_DefTTT datasn_me.zero 0u sendData
                let! _ = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
                ()

            // Send Nop-Out PDU
            let! _, wcmdsn = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.MaxCmdSN = cmdsn_me.incr ( Constants.BDLU_MAX_TASKSET_SIZE + 1u ) wcmdsn ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Send CmdSN over MaxCmdSN.
    [<Fact>]
    member _.CmdSN_MaxCmdSN_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out 1
            let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1_1.ExpCmdSN = cmdsn_me.fromPrim 1u ))

            // Send SCSI Command PDU until CmdSN reaches MaxCmdSN.
            let pduCount = ( cmdsn_me.toPrim pdu1_1.MaxCmdSN ) - ( cmdsn_me.toPrim pdu1_1.ExpCmdSN ) + 1u |> int
            let vitt = Array.zeroCreate<ITT_T> pduCount
            for i = 1 to pduCount do
                let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy 1us NACA.F LINK.F
                let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
                vitt.[ i - 1 ] <- itt

            // Send additional SCSI Command PDU with CmdSN overs MaxCmdSN.
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy 1us NACA.F LINK.F
            let! _, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
            let! _ = r1.ReceiveSpecific<RejectPDU> g_CID0

            // Rewind the initiator's CmdSN value
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )

            // Send Data-Out PDUs and receive SCSI Response PDUs
            let sendData = PooledBuffer.RentAndInit( int m_MediaBlockSize )
            for i = 0 to pduCount - 1 do
                do! r1.SendSCSIDataOutPDU g_CID0 BitF.T vitt.[i] g_LUN1 g_DefTTT datasn_me.zero 0u sendData
                let! _ = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
                ()

            // Send Nop-Out PDU
            let! _, wcmdsn = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.MaxCmdSN = cmdsn_me.incr ( Constants.BDLU_MAX_TASKSET_SIZE + 1u ) wcmdsn ))

            do! r1.CloseSession g_CID0 BitI.F
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
            let! _, cmdsn1 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1 = cmdsn_me.fromPrim 0xFFFFFFFDu ))
            Assert.True(( pdu1.ExpCmdSN = cmdsn_me.fromPrim 0xFFFFFFFEu ))

            let! _, cmdsn2 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn2 = cmdsn_me.fromPrim 0xFFFFFFFEu ))
            Assert.True(( pdu2.ExpCmdSN = cmdsn_me.fromPrim 0xFFFFFFFFu ))

            let! _, cmdsn3 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn3 = cmdsn_me.fromPrim 0xFFFFFFFFu ))
            Assert.True(( pdu3.ExpCmdSN = cmdsn_me.zero ))

            let! _, cmdsn4 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu4 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn4 = cmdsn_me.zero ))
            Assert.True(( pdu4.ExpCmdSN = cmdsn_me.fromPrim 1u ))

            let! _, cmdsn5 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu5 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn5 = cmdsn_me.fromPrim 1u ))
            Assert.True(( pdu5.ExpCmdSN = cmdsn_me.fromPrim 2u ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Send CmdSN in reverse order
    [<Fact>]
    member _.CmdSN_ReverseOder_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out 1
            let! _, cmdsn1 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1 = cmdsn_me.zero ))

            // skip CmdSN=1
            r1.SetNextCmdSN ( cmdsn_me.fromPrim 2u )

            // Nop-Out 3
            let sendData3 = PooledBuffer.RentAndInit 4096
            sendData3.Array.[0] <- 3uy
            let! _, cmdsn3 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT sendData3
            sendData3.Return()
            Assert.True(( cmdsn3 = cmdsn_me.fromPrim 2u ))

            // Rewind to CmdSN=1
            r1.SetNextCmdSN ( cmdsn_me.fromPrim 1u )

            // Nop-Out 2
            let sendData2 = PooledBuffer.RentAndInit 4096
            sendData2.Array.[0] <- 2uy
            let! _, cmdsn2 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT sendData2
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

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Multi sessions
    [<Fact>]
    member _.CmdSN_MultiSession_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            let! r2 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out 1 at session 1
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Nop-Out 1 at session 2
            let! _, cmdsn2_1 = r2.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r2.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn2_1 = cmdsn_me.zero ))

            // Nop-Out 2 at session 1
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 1u ))

            // Nop-Out 2 at session 2
            let! _, cmdsn2_2 = r2.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r2.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 1u ))

            // Nop-Out 3 at session 1
            let! _, cmdsn1_3 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_3 = cmdsn_me.fromPrim 2u ))

            // Nop-Out 3 at session 2
            let! _, cmdsn2_2 = r2.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r2.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 2u ))

            do! r1.CloseSession g_CID0 BitI.F  // logout at session 1
            do! r2.CloseSession g_CID0 BitI.F  // logout at session 2
        }

    // Multi connections
    [<Fact>]
    member _.CmdSN_MultiConnections_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Nop-Out 1 at connection 1
            let! _, cmdsn2_1 = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_1 = cmdsn_me.fromPrim 1u ))

            // Nop-Out 2 at connection 0
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 2u ))

            // Nop-Out 2 at connection 1
            let! _, cmdsn2_2 = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 3u ))

            // Nop-Out 3 at connection 0
            let! _, cmdsn1_3 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_3 = cmdsn_me.fromPrim 4u ))

            // Nop-Out 3 at connection 1
            let! _, cmdsn2_2 = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 5u ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Reconnecting a connection. session persists.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Nop-Out 1 at connection 1
            let! _, cmdsn2_1 = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_1 = cmdsn_me.fromPrim 1u ))

            // logout connection 0
            let! _, cmdsn_logout = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( cmdsn_logout = cmdsn_me.fromPrim 2u ))
            r1.RemoveConnectionEntry g_CID0 |> ignore

            // Re-connect connection 0
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2 at connection 0
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 3u ))

            // Nop-Out 2 at connection 1
            let! _, cmdsn2_2 = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 4u ))

            do! r1.CloseSession g_CID1 BitI.F
        }
        
    // Reconnecting a connection. session persists.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Nop-Out 1 at connection 1
            let! _, cmdsn2_1 = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_1 = cmdsn_me.fromPrim 1u ))

            // Drop connection 0
            r1.CloseConnection g_CID0

            // Re-connect connection 0
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2 at connection 0
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 2u ))

            // Nop-Out 2 at connection 1
            let! _, cmdsn2_2 = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 3u ))

            do! r1.CloseSession g_CID1 BitI.F
        }

    // Reconnecting a connection. session persists.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_003() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Nop-Out 1 at connection 1
            let! _, cmdsn2_1 = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_1 = cmdsn_me.fromPrim 1u ))

            // Re-connect connection 0
            r1.RemoveConnectionEntry g_CID0 |> ignore
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2 at connection 0
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 2u ))

            // Nop-Out 2 at connection 1
            let! _, cmdsn2_2 = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( cmdsn2_2 = cmdsn_me.fromPrim 3u ))

            do! r1.CloseSession g_CID1 BitI.F
        }

    // Reconnecting a connection. session does not persist.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_004() =
        task {
            let sessParam1 = { m_defaultSessParam with ErrorRecoveryLevel = 1uy; }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 1
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // logout connection 0
            let! _, cmdsn_logout = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( cmdsn_logout = cmdsn_me.fromPrim 1u ))

            // Re-connect ( reuse ISID )
            let! r2 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 2
            let! _, cmdsn1_2 = r2.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r2.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.zero ))

            do! r2.CloseSession g_CID0 BitI.F
        }

    // Reconnecting a connection. session does not persist.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_005() =
        task {
            let sessParam1 = { m_defaultSessParam with ErrorRecoveryLevel = 1uy; }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 1
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Drop connection 0
            r1.CloseConnection g_CID0

            // Re-connect ( reuse ISID )
            let! r2 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 2
            let! _, cmdsn1_2 = r2.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r2.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.zero ))

            do! r2.CloseSession g_CID0 BitI.F
        }

    // Reconnecting a connection. session persists.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_006() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Send SCSI Command PDU at connection 0
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy 1us NACA.F LINK.F
            let! _, cmdsn_sc = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
            Assert.True(( cmdsn_sc = cmdsn_me.fromPrim 1u ))

            // Drop connection 0
            r1.CloseConnection g_CID0

            // Re-connect connection 0
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2 at connection 0
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! wpdu = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 2u ))

            do! r1.CloseSession g_CID1 BitI.F
        }
        
    // Reconnecting a connection. session persists.
    [<Fact>]
    member _.CmdSN_ReconnectConnection_007() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let! _, cmdsn1_1 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_1 = cmdsn_me.zero ))

            // Send SCSI Command PDU at connection 0
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy 1us NACA.F LINK.F
            let! _, cmdsn_sc = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB PooledBuffer.Empty 0u
            Assert.True(( cmdsn_sc = cmdsn_me.fromPrim 1u ))

            // Re-connect connection 0
            r1.RemoveConnectionEntry g_CID0 |> ignore
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2 at connection 0
            let! _, cmdsn1_2 = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! wpdu = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( cmdsn1_2 = cmdsn_me.fromPrim 2u ))

            do! r1.CloseSession g_CID1 BitI.F
        }

    // Sequence of non-immediate commands.
    [<Fact>]
    member _.StatSN_Sequense_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Not-Out
            for i = 0 to 9 do
                let sendExpStatSN = r1.Connection( g_CID0 ).ExpStatSN
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
                let! pdu = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.True(( pdu.StatSN = sendExpStatSN ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // the next command is sent without incrementing ExpStatSN.
    [<Fact>]
    member _.StatSN_LostResponse_001() =
        task {
            let sessParam1 = { m_defaultSessParam with ErrorRecoveryLevel = 1uy; }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Send Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let sendData1 = PooledBuffer.RentAndInit 4096
            sendData1.Array.[0] <- 1uy
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT sendData1
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
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT sendData2
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

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Add ExpStatSN before receiving status.
    [<Fact>]
    member _.StatSN_Skip_001() =
        task {
            let sessParam1 = { m_defaultSessParam with ErrorRecoveryLevel = 1uy; }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Send Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! itt1, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive Nop-In 1
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))
            Assert.True(( pdu1.InitiatorTaskTag = itt1 ))

            // skip ExpStatSN
            r1.Connection( g_CID0 ).SkipExtStatSN( statsn_me.fromPrim 1u )

            // Send Nop-Out 2
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN  // sendExpStatSN2 = sendExpStatSN1 + 2
            let! itt2, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty

            // Receive Nop-In 2
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = statsn_me.decr 1u sendExpStatSN2 ))
            Assert.True(( pdu2.InitiatorTaskTag = itt2 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Resend request for received status.
    [<Fact>]
    member _.StatSN_Rewind_001() =
        task {
            let sessParam1 = { m_defaultSessParam with ErrorRecoveryLevel = 1uy; }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Send Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! itt1, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))
            Assert.True(( pdu1.InitiatorTaskTag = itt1 ))

            // Send Nop-Out 2
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN
            let! itt2, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))
            Assert.True(( pdu2.InitiatorTaskTag = itt2 ))

            // rewind ExpStatSN
            r1.Connection( g_CID0 ).RewindExtStatSN( statsn_me.fromPrim 2u )

            // Send Nop-Out 3
            let sendExpStatSN3 = r1.Connection( g_CID0 ).ExpStatSN  // sendExpStatSN3 = sendExpStatSN1
            let! itt3, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
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

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Sequence of non-immediate commands.
    [<Fact>]
    member _.StatSN_Immidiate_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Not-Out 1 (non-immidiate)
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // Not-Out 2 (immidiate)
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN
            Assert.True(( sendExpStatSN2 = statsn_me.next sendExpStatSN1 ))
            let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))

            // Not-Out 3 (non-immidiate)
            let sendExpStatSN3 = r1.Connection( g_CID0 ).ExpStatSN
            Assert.True(( sendExpStatSN3 = statsn_me.next sendExpStatSN2 ))
            let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu3.StatSN = sendExpStatSN3 ))

            // Not-Out 4 (immidiate)
            let sendExpStatSN4 = r1.Connection( g_CID0 ).ExpStatSN
            Assert.True(( sendExpStatSN4 = statsn_me.next sendExpStatSN3 ))
            let! _, _ = r1.SendNOPOutPDU g_CID0 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu4 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu4.StatSN = sendExpStatSN4 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Multi connections
    [<Fact>]
    member _.StatSN_MultiConnections_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out 1 at connection 0
            let sendExpStatSN0_1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu0_1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu0_1.StatSN = sendExpStatSN0_1 ))

            // Nop-Out 1 at connection 1
            let sendExpStatSN1_1 = r1.Connection( g_CID1 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_1 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( pdu1_1.StatSN = sendExpStatSN1_1 ))

            // Nop-Out 2 at connection 0
            let sendExpStatSN0_2 = r1.Connection( g_CID0 ).ExpStatSN
            Assert.True(( sendExpStatSN0_2 = statsn_me.next sendExpStatSN0_1 ))
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu0_2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu0_2.StatSN = sendExpStatSN0_2 ))

            // Nop-Out 2 at connection 1
            let sendExpStatSN1_2 = r1.Connection( g_CID1 ).ExpStatSN
            Assert.True(( sendExpStatSN1_2 = statsn_me.next sendExpStatSN1_1 ))
            let! _ = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_2 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( pdu1_2.StatSN = sendExpStatSN1_2 ))

            // Nop-Out 3 at connection 0
            let sendExpStatSN0_3 = r1.Connection( g_CID0 ).ExpStatSN
            Assert.True(( sendExpStatSN0_3 = statsn_me.next sendExpStatSN0_2 ))
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu0_3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu0_3.StatSN = sendExpStatSN0_3 ))

            // Nop-Out 3 at connection 1
            let sendExpStatSN1_3 = r1.Connection( g_CID1 ).ExpStatSN
            Assert.True(( sendExpStatSN1_3 = statsn_me.next sendExpStatSN1_2 ))
            let! _ = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1_3 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            Assert.True(( pdu1_3.StatSN = sendExpStatSN1_3 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Too little ExpStatSN
    [<Fact>]
    member _.StatSN_MaxStatSNDiff_001() =
        task {
            let sessParam1 = { m_defaultSessParam with ErrorRecoveryLevel = 0uy; }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // Nop-Out 2
            let tobeNextStatSN2 = statsn_me.next pdu1.StatSN
            let sendExpStatSN2 = statsn_me.decr Constants.MAX_STATSN_DIFF tobeNextStatSN2
            r1.Connection( g_CID0 ).SetNextExtStatSN sendExpStatSN2
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = statsn_me.next sendExpStatSN1 ))

            // Nop-Out 3
            let tobeNextStatSN3 = statsn_me.next pdu2.StatSN
            let sendExpStatSN3 = statsn_me.decr ( Constants.MAX_STATSN_DIFF + 1u ) tobeNextStatSN3
            r1.Connection( g_CID0 ).SetNextExtStatSN sendExpStatSN3
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
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
            let sessParam1 = { m_defaultSessParam with ErrorRecoveryLevel = 0uy; }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // Nop-Out 2
            let tobeNextStatSN2 = statsn_me.next pdu1.StatSN
            let sendExpStatSN2 = statsn_me.incr Constants.MAX_STATSN_DIFF tobeNextStatSN2
            r1.Connection( g_CID0 ).SetNextExtStatSN sendExpStatSN2
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = statsn_me.next sendExpStatSN1 ))

            // Nop-Out 3
            let tobeNextStatSN3 = statsn_me.next pdu2.StatSN
            let sendExpStatSN3 = statsn_me.incr ( Constants.MAX_STATSN_DIFF + 1u ) tobeNextStatSN3
            r1.Connection( g_CID0 ).SetNextExtStatSN sendExpStatSN3
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
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
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                ()

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // logout connection 0
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! _ = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0

            // Re-connect connection 0
            r1.RemoveConnectionEntry g_CID0 |> ignore
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))

            // Reconnecting the connection resets StatSN back to the beginning.
            Assert.True(( statsn_me.lessThan sendExpStatSN2 sendExpStatSN1 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Reconnecting a connection. Reconnect after drop the connection.
    [<Fact>]
    member _.StatSN_ReconnectConnection_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Some of Nop-Out
            for i = 0 to 10 do
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                ()

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // Drop connection 0
            r1.CloseConnection g_CID0

            // Re-connect connection 0
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out 2
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))

            // Reconnecting the connection resets StatSN back to the beginning.
            Assert.True(( statsn_me.lessThan sendExpStatSN2 sendExpStatSN1 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Reconnecting a connection.  Reconnect with implicit logout. Initial ExpStatSN=0.
    [<Fact>]
    member _.StatSN_ReconnectConnection_003() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Some of Nop-Out
            for i = 0 to 10 do
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                ()

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
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
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))

            // Reconnecting the connection resets StatSN back to the beginning.
            Assert.True(( statsn_me.lessThan sendExpStatSN2 sendExpStatSN1 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Reconnecting a connection.  Reconnect with implicit logout.
    [<Fact>]
    member _.StatSN_ReconnectConnection_004() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Some of Nop-Out
            for i = 0 to 10 do
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                ()

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))

            // Re-connect connection 0
            do! r1.AddConnection m_defaultConnParam     // Inherits the value of ExpStatSN

            // Nop-Out 2
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! pdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))

            // StatSN value is not reset
            Assert.True(( statsn_me.lessThan sendExpStatSN1 sendExpStatSN2 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Reconnecting a connection.  Reconnect with implicit logout. Fake ExpStatSN.
    [<Fact>]
    member _.StatSN_ReconnectConnection_005() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Some of Nop-Out
            for i = 0 to 10 do
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                ()

            // Nop-Out 1
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
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
            do! r1.CloseSession g_CID1 BitI.F
        }

    [<Fact>]
    member _.StatSN_CheckCondition_001() =
        task {
            let sendData = PooledBuffer.Rent( int m_MediaBlockSize )
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // SCSI Write command ( failed )
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0xFFFFFFFFu 0uy 1us NACA.F LINK.F
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB sendData 0u
            let! pdu1 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))
            Assert.True(( pdu1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // Send TaskMgrReq for clear ACA
            let sendExpStatSN2 = r1.Connection( g_CID0 ).ExpStatSN
            let! _, _ = r1.SendTaskManagementFunctionRequestPDU g_CID0 BitI.F TaskMgrReqCd.CLEAR_ACA g_LUN1 g_DefITT ( ValueSome cmdsn_me.zero ) datasn_me.zero
            let! pdu2 = r1.ReceiveSpecific<TaskManagementFunctionResponsePDU> g_CID0
            Assert.True(( pdu2.StatSN = sendExpStatSN2 ))
            Assert.True(( pdu2.Response = TaskMgrResCd.FUNCTION_COMPLETE ))

            // SCSI Write command ( succeed )
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy 1us NACA.F LINK.F
            let sendExpStatSN1 = r1.Connection( g_CID0 ).ExpStatSN
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 m_MediaBlockSize writeCDB sendData 0u
            let! pdu1 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( pdu1.StatSN = sendExpStatSN1 ))
            Assert.True(( pdu1.Status = ScsiCmdStatCd.GOOD ))

            // logout.
            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.DataSN_Read_NoDataPDU_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // SCSI Read
            let readCDB = GenScsiCDB.Read10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy 0us NACA.F LINK.F
            let! _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 0u readCDB PooledBuffer.Empty 0u

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout.
            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.DataSN_Read_OneDataPDU_001() =
        task {
            let! r1 = CreateSession 0uy 16384u 4096u
            let accessLength = 4096u
            let accessBlockcount = accessLength / m_MediaBlockSize  // block size must be 512 or 4096
            //let pduCount = 1u

            // SCSI Read
            let readCDB = GenScsiCDB.Read10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockcount ) NACA.F LINK.F
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength readCDB PooledBuffer.Empty 0u

            // SCSI Data-In
            let! rpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( rpdu2.InitiatorTaskTag = itt ))
            Assert.True(( rpdu2.DataSN = datasn_me.zero ))
            Assert.True(( rpdu2.BufferOffset = 0u ))
            Assert.True(( rpdu2.DataSegment.Count = int accessLength ))

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout.
            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.DataSN_Read_MultiDataPDU_001() =
        task {
            let! r1 = CreateSession 0uy 16384u 4096u
            let pduCount = 2u
            let accessLength = 4096u * pduCount
            let accessBlockcount = accessLength / m_MediaBlockSize  // block size must be 512 or 4096

            // SCSI Read
            let readCDB = GenScsiCDB.Read10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockcount ) NACA.F LINK.F
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength readCDB PooledBuffer.Empty 0u

            // SCSI Data-In
            for i = 0u to pduCount - 1u do
                let! rpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.DataSN = datasn_me.fromPrim i ))
                Assert.True(( rpdu2.BufferOffset = i * 4096u ))
                Assert.True(( rpdu2.DataSegment.Count = 4096 ))

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.DataSN_Read_MultiBurst_001() =
        task {
            let! r1 = CreateSession 1uy 3000u 800u
            let accessLength = 16384u
            let accessBlockcount = accessLength / m_MediaBlockSize  // block size must be 512 or 4096
            let expRsult = [|
                ( 0u, 0u, 800, false );
                ( 1u, 800u, 800, false );
                ( 2u, 1600u, 800, false );
                ( 3u, 2400u, 600, true );
                ( 4u, 3000u, 800, false );
                ( 5u, 3800u, 800, false );
                ( 6u, 4600u, 800, false );
                ( 7u, 5400u, 600, true );
                ( 8u, 6000u, 800, false );
                ( 9u, 6800u, 800, false );
                ( 10u, 7600u, 800, false );
                ( 11u, 8400u, 600, true );
                ( 12u, 9000u, 800, false );
                ( 13u, 9800u, 800, false );
                ( 14u, 10600u, 800, false );
                ( 15u, 11400u, 600, true );
                ( 16u, 12000u, 800, false );
                ( 17u, 12800u, 800, false );
                ( 18u, 13600u, 800, false );
                ( 19u, 14400u, 600, true );
                ( 20u, 15000u, 800, false );
                ( 21u, 15800u, 584, true );
            |]

            // SCSI Read
            let readCDB = GenScsiCDB.Read10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockcount ) NACA.F LINK.F
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength readCDB PooledBuffer.Empty 0u

            for i = 0 to expRsult.Length - 1 do
                let ( expdn, expOffset, expLength, expF ) = expRsult.[i]

                // SCSI Data-In
                let! rpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu2.F = expF ))
                Assert.True(( rpdu2.A = expF ))
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.DataSN = datasn_me.fromPrim expdn ))
                Assert.True(( rpdu2.BufferOffset = expOffset ))
                Assert.True(( rpdu2.DataSegment.Count = expLength ))

                if rpdu2.A then
                    // Data ACK
                    let begRun = rpdu2.DataSN |> datasn_me.next |> datasn_me.toPrim
                    let! _ = r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.DATA_ACK g_LUN1 g_DefITT rpdu2.TargetTransferTag begRun 0u
                    ()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.DataSN_Read_MultiBurst_002() =
        task {
            let! r1 = CreateSession 0uy 3000u 800u
            let accessLength = 8192u
            let accessBlockcount = accessLength / m_MediaBlockSize  // block size must be 512 or 4096
            let expRsult = [|
                ( 0u, 0u, 800, false );
                ( 1u, 800u, 800, false );
                ( 2u, 1600u, 800, false );
                ( 3u, 2400u, 600, true );
                ( 4u, 3000u, 800, false );
                ( 5u, 3800u, 800, false );
                ( 6u, 4600u, 800, false );
                ( 7u, 5400u, 600, true );
                ( 8u, 6000u, 800, false );
                ( 9u, 6800u, 800, false );
                ( 10u, 7600u, 592, true );
            |]

            // SCSI Read
            let readCDB = GenScsiCDB.Read10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockcount ) NACA.F LINK.F
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength readCDB PooledBuffer.Empty 0u

            for i = 0 to expRsult.Length - 1 do
                let ( expdn, expOffset, expLength, expF ) = expRsult.[i]

                // SCSI Data-In
                let! rpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu2.F = expF ))
                Assert.False(( rpdu2.A ))
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.DataSN = datasn_me.fromPrim expdn ))
                Assert.True(( rpdu2.BufferOffset = expOffset ))
                Assert.True(( rpdu2.DataSegment.Count = expLength ))

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Retransmit data with SNACK request
    [<Fact>]
    member _.DataSN_Read_SNACK_001() =
        task {
            let! r1 = CreateSession 1uy 3000u 1200u
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

            // SCSI Read
            let readCDB = GenScsiCDB.Read10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockcount ) NACA.F LINK.F
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength readCDB PooledBuffer.Empty 0u

            // SCSI Data-In
            for i = 0 to expRsult.Length - 1 do
                let ( expdn, expOffset, expLength, expF ) = expRsult.[i]
                let! rpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu2.F = expF ))
                Assert.True(( rpdu2.A = expF ))
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.DataSN = datasn_me.fromPrim expdn ))
                Assert.True(( rpdu2.BufferOffset = expOffset ))
                Assert.True(( rpdu2.DataSegment.Count = expLength ))

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // Send SNACK request
            // Note that at this point, no acknowledgement has been returned.
            r1.Connection( g_CID0 ).RewindExtStatSN ( statsn_me.fromPrim 1u )
            let! _ = r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.DATA_R2T g_LUN1 itt g_DefTTT 4u 0u

            // SCSI Data-In
            for i = 4 to 7 do
                let ( expdn, expOffset, expLength, expF ) = expRsult.[i]
                let! rpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu2.F = expF ))
                Assert.True(( rpdu2.A = expF ))
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.DataSN = datasn_me.fromPrim expdn ))
                Assert.True(( rpdu2.BufferOffset = expOffset ))
                Assert.True(( rpdu2.DataSegment.Count = expLength ))

            r1.Connection( g_CID0 ).SkipExtStatSN ( statsn_me.fromPrim 1u )

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Retransmitting the data, which involves re-segmenting the data
    [<Fact>]
    member _.DataSN_Read_SNACK_002() =
        task {
            let! r1 = CreateSession 1uy 3000u 1200u
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

            // Send SCSI write
            let writeData = Array.zeroCreate ( int accessLength )
            Random.Shared.NextBytes( writeData.AsSpan() )
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockcount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Receive R2T 1
            let! r2t1 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2t1.BufferOffset = 0u ))
            Assert.True(( r2t1.DesiredDataTransferLength = 3000u ))

            // Send Data-Out PDU 1
            let writeData1 = PooledBuffer.Rent( writeData, 0, 3000 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2t1.TargetTransferTag datasn_me.zero r2t1.BufferOffset writeData1
            writeData1.Return()

            // Receive R2T 2
            let! r2t2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2t2.BufferOffset = 3000u ))
            Assert.True(( r2t2.DesiredDataTransferLength = 3000u ))

            // Send Data-Out PDU 2
            let writeData2 = PooledBuffer.Rent( writeData, 3000, 3000 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2t2.TargetTransferTag datasn_me.zero r2t2.BufferOffset writeData2
            writeData2.Return()

            // Receive R2T 3
            let! r2t3 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2t3.BufferOffset = 6000u ))
            Assert.True(( r2t3.DesiredDataTransferLength = 2192u ))

            // Send Data-Out PDU 2
            let writeData3 = PooledBuffer.Rent( writeData, 6000, 2192 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2t3.TargetTransferTag datasn_me.zero r2t3.BufferOffset writeData3
            writeData3.Return()

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))

            // SCSI Read
            let readCDB = GenScsiCDB.Read10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockcount ) NACA.F LINK.F
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength readCDB PooledBuffer.Empty 0u

            // SCSI Data-In
            for i = 0 to expRsult.Length - 1 do
                let ( expdn, expOffset, expLength, expF ) = expRsult.[i]
                let! rpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu2.F = expF ))
                Assert.True(( rpdu2.A = expF ))
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.DataSN = datasn_me.fromPrim expdn ))
                Assert.True(( rpdu2.BufferOffset = expOffset ))
                Assert.True(( rpdu2.DataSegment.Count = expLength ))
                let recvData = rpdu2.DataSegment.ToArray()
                let expData = writeData.[ int expOffset .. int expOffset + expLength - 1 ]
                Assert.True(( recvData = expData ))

                if expdn = 2u then
                    // Data ACK
                    let! _ = r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.DATA_ACK g_LUN1 g_DefITT rpdu2.TargetTransferTag 3u 0u
                    ()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // Note that at this point, no acknowledgement has been returned.
            r1.Connection( g_CID0 ).RewindExtStatSN ( statsn_me.fromPrim 1u )

            // Text request. MaxRecvDataSegmentLength_I -> 800
            let textRequest = 
                let negoValue1 = {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.Value( 800u );
                }
                let negoStat1 = {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitSend;
                }
                IscsiTextEncode.CreateTextKeyValueString negoValue1 negoStat1
            let! itt4, _ = r1.SendTextRequestPDU g_CID0 BitI.T BitF.T BitC.F ValueNone g_LUN1 g_DefTTT textRequest

            let! rpdu4 = r1.ReceiveSpecific<TextResponsePDU> g_CID0
            Assert.True(( rpdu4.F ))
            Assert.True(( rpdu4.InitiatorTaskTag = itt4 ))
            Assert.True(( rpdu4.TextResponse.Length = 0 ))

            // update initiator side parameter value
            r1.FakeConnectionParameter g_CID0 { m_defaultConnParam with MaxRecvDataSegmentLength_I = 800u }

            // Send R-SNACK request
            let! _ = r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.RDATA_SNACK g_LUN1 itt ( ttt_me.fromPrim 0x12345678u ) 0u 0u

            let expRsult2 = [|
                ( 3u, 3000u, 800, false );
                ( 4u, 3800u, 800, false );
                ( 5u, 4600u, 800, false );
                ( 6u, 5400u, 600, true );
                ( 7u, 6000u, 800, false );
                ( 8u, 6800u, 800, false );
                ( 9u, 7600u, 592, true );
            |]

            // SCSI Data-In
            for i = 0 to expRsult2.Length - 1 do
                let ( expdn, expOffset, expLength, expF ) = expRsult2.[i]
                let! rpdu5 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu5.F = expF ))
                Assert.True(( rpdu5.A = expF ))
                Assert.True(( rpdu5.InitiatorTaskTag = itt ))
                Assert.True(( rpdu5.DataSN = datasn_me.fromPrim expdn ))
                Assert.True(( rpdu5.BufferOffset = expOffset ))
                Assert.True(( rpdu5.DataSegment.Count = expLength ))
                let recvData = rpdu5.DataSegment.ToArray()
                let expData = writeData.[ int expOffset .. int expOffset + expLength - 1 ]
                Assert.True(( recvData = expData ))

            // SCSI Response
            let! rpdu6 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu6.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu6.SNACKTag = snacktag_me.fromPrim 0x12345678u ))

            r1.Connection( g_CID0 ).SkipExtStatSN ( statsn_me.fromPrim 1u )
            do! r1.CloseSession g_CID0 BitI.F
        }

    // Retransmitting the data, which involves re-segmenting the data
    [<Fact>]
    member _.DataSN_Read_SNACK_003() =
        task {
            let! r1 = CreateSession 1uy 3000u 1200u
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

            // Send SCSI write
            let writeData = Array.zeroCreate ( int accessLength )
            Random.Shared.NextBytes( writeData.AsSpan() )
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockcount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Receive R2T 1
            let! r2t1 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2t1.BufferOffset = 0u ))
            Assert.True(( r2t1.DesiredDataTransferLength = 3000u ))

            // Send Data-Out PDU 1
            let writeData1 = PooledBuffer.Rent( writeData, 0, 3000 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2t1.TargetTransferTag datasn_me.zero r2t1.BufferOffset writeData1
            writeData1.Return()

            // Receive R2T 2
            let! r2t2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2t2.BufferOffset = 3000u ))
            Assert.True(( r2t2.DesiredDataTransferLength = 3000u ))

            // Send Data-Out PDU 2
            let writeData2 = PooledBuffer.Rent( writeData, 3000, 3000 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2t2.TargetTransferTag datasn_me.zero r2t2.BufferOffset writeData2
            writeData2.Return()

            // Receive R2T 3
            let! r2t3 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2t3.BufferOffset = 6000u ))
            Assert.True(( r2t3.DesiredDataTransferLength = 2192u ))

            // Send Data-Out PDU 2
            let writeData3 = PooledBuffer.Rent( writeData, 6000, 2192 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2t3.TargetTransferTag datasn_me.zero r2t3.BufferOffset writeData3
            writeData3.Return()

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))

            // SCSI Read
            let readCDB = GenScsiCDB.Read10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockcount ) NACA.F LINK.F
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength readCDB PooledBuffer.Empty 0u

            // SCSI Data-In
            for i = 0 to expRsult.Length - 1 do
                let ( expdn, expOffset, expLength, expF ) = expRsult.[i]
                let! rpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu2.F = expF ))
                Assert.True(( rpdu2.A = expF ))
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.DataSN = datasn_me.fromPrim expdn ))
                Assert.True(( rpdu2.BufferOffset = expOffset ))
                Assert.True(( rpdu2.DataSegment.Count = expLength ))
                let recvData = rpdu2.DataSegment.ToArray()
                let expData = writeData.[ int expOffset .. int expOffset + expLength - 1 ]
                Assert.True(( recvData = expData ))

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // Note that at this point, no acknowledgement has been returned.
            r1.Connection( g_CID0 ).RewindExtStatSN ( statsn_me.fromPrim 1u )

            // Text request. MaxRecvDataSegmentLength_I -> 1300
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
            let! itt4, _ = r1.SendTextRequestPDU g_CID0 BitI.T BitF.T BitC.F ValueNone g_LUN1 g_DefTTT textRequest

            let! rpdu4 = r1.ReceiveSpecific<TextResponsePDU> g_CID0
            Assert.True(( rpdu4.F ))
            Assert.True(( rpdu4.InitiatorTaskTag = itt4 ))
            Assert.True(( rpdu4.TextResponse.Length = 0 ))

            // update initiator side parameter value
            r1.FakeConnectionParameter g_CID0 { m_defaultConnParam with MaxRecvDataSegmentLength_I = 1300u }

            // Send R-SNACK request
            let! _ = r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.RDATA_SNACK g_LUN1 itt ( ttt_me.fromPrim 0x12345678u ) 0u 0u

            let expRsult2 = [|
                ( 0u, 0u, 1300, false );
                ( 1u, 1300u, 1300, false );
                ( 2u, 2600u, 400, true );
                ( 3u, 3000u, 1300, false );
                ( 4u, 4300u, 1300, false );
                ( 5u, 5600u, 400, true );
                ( 6u, 6000u, 1300, false );
                ( 7u, 7300u, 892, true );
            |]

            // SCSI Data-In
            for i = 0 to expRsult2.Length - 1 do
                let ( expdn, expOffset, expLength, expF ) = expRsult2.[i]
                let! rpdu5 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu5.F = expF ))
                Assert.True(( rpdu5.A = expF ))
                Assert.True(( rpdu5.InitiatorTaskTag = itt ))
                Assert.True(( rpdu5.DataSN = datasn_me.fromPrim expdn ))
                Assert.True(( rpdu5.BufferOffset = expOffset ))
                Assert.True(( rpdu5.DataSegment.Count = expLength ))
                let recvData = rpdu5.DataSegment.ToArray()
                let expData = writeData.[ int expOffset .. int expOffset + expLength - 1 ]
                Assert.True(( recvData = expData ))

            // SCSI Response
            let! rpdu6 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu6.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( rpdu6.SNACKTag = snacktag_me.fromPrim 0x12345678u ))

            r1.Connection( g_CID0 ).SkipExtStatSN ( statsn_me.fromPrim 1u )
            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.R2T_NoUnsolicitedData_NoWrittenData_001() =
        task {
            let! r1 = CreateSession 0uy 16384u 16384u
            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize

            // read media data
            let readCDB = GenScsiCDB.Read10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let! readITT1, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength readCDB PooledBuffer.Empty 0u
            let! readPDU1_1 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
            Assert.True(( readPDU1_1.F ))
            Assert.True(( readPDU1_1.InitiatorTaskTag = readITT1 ))
            let! readPDU1_2 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( readPDU1_2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( readPDU1_2.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( readPDU1_2.InitiatorTaskTag = readITT1 ))

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy 0us NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 0u writeCDB PooledBuffer.Empty 0u

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.False(( writeRespPDU.o ))
            Assert.False(( writeRespPDU.u ))
            Assert.False(( writeRespPDU.O ))
            Assert.False(( writeRespPDU.U ))
            Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
            Assert.True(( writeRespPDU.ResidualCount = 0u ))
            Assert.True(( writeRespPDU.SenseLength = 0us ))
            Assert.True(( writeRespPDU.SenseData.Count = 0 ))
            Assert.True(( writeRespPDU.ResponseData.Count = 0 ))

            // check no data changed
            let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
            Assert.True(( readPDU1_1.DataSegment.ToArray() = wroteData ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.R2T_NoUnsolicitedData_1R2T_1DataOutPDU_001() =
        task {
            let! r1 = CreateSession 0uy 16384u 16384u
            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Receive R2T
            let! r2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2tPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( r2tPDU.R2TSN = datasn_me.zero ))
            Assert.True(( r2tPDU.BufferOffset = 0u ))
            Assert.True(( r2tPDU.DesiredDataTransferLength = accessLength ))

            // Send data-Out PDU ( write random data )
            let writtenData = PooledBuffer.Rent( int accessLength )
            Random.Shared.NextBytes( writtenData.Array )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2tPDU.TargetTransferTag datasn_me.zero 0u writtenData

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
            Assert.True(( writeRespPDU.ResidualCount = 0u ))

            // Verify that the sent data was written correctly.
            let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
            Assert.True(( PooledBuffer.ValueEqualsWithArray writtenData wroteData ))

            do! r1.CloseSession g_CID0 BitI.F
            writtenData.Return()
        }

    static member m_R2T_NoUnsolicitedData_1R2T_MultiDataOutPDU_001_data = [|
        [|
            [|//  DataSN  TTT                F       Data Start  Data End  Fake data
                ( 0u,     Option<uint>.None, BitF.F, 0,          1023,     false );
                ( 1u,     Option<uint>.None, BitF.F, 1024,       2047,     false );
                ( 2u,     Option<uint>.None, BitF.F, 2048,       3071,     false );
                ( 3u,     Option<uint>.None, BitF.T, 3072,       4095,     false );
            |] :> obj;
            0u :> obj;  // Residual Count
        |];
        [|
            [|//  DataSN  TTT                F       Data Start  Data End  Fake data
                ( 1u,     Option<uint>.None, BitF.F, 2048,       3071,     false );
                ( 0u,     Option<uint>.None, BitF.F, 0,          1023,     false );
                ( 3u,     Option<uint>.None, BitF.F, 3072,       4095,     false );
                ( 2u,     Option<uint>.None, BitF.T, 1024,       2047,     false );
            |] :> obj;
            0u :> obj;  // Residual Count
        |];
        [|
            [|//  DataSN  TTT                F       Data Start  Data End  Fake data
                ( 0u,     Option<uint>.None, BitF.F, 0,          2047,     false );
                ( 1u,     Option<uint>.None, BitF.F, 1024,       3071,     false );
                ( 2u,     Option<uint>.None, BitF.T, 2048,       4095,     false );
            |] :> obj;
            2048u :> obj;// Haruka considers this to be an overflow because the initiator sent too many data.
        |];
        [|
            [|//  DataSN  TTT                F       Data Start  Data End  Fake data
                ( 0u,     Option<uint>.None, BitF.F, 0,          -1,       false );    // Empty Data-Out PDU
                ( 1u,     Option<uint>.None, BitF.F, 0,          1023,     false );
                ( 2u,     Option<uint>.None, BitF.F, 1024,       1023,     false );    // Empty Data-Out PDU
                ( 3u,     Option<uint>.None, BitF.F, 1024,       2047,     false );
                ( 4u,     Option<uint>.None, BitF.F, 2048,       2047,     false );    // Empty Data-Out PDU
                ( 5u,     Option<uint>.None, BitF.F, 2048,       3071,     false );
                ( 6u,     Option<uint>.None, BitF.F, 3072,       3071,     false );    // Empty Data-Out PDU
                ( 7u,     Option<uint>.None, BitF.F, 3072,       4095,     false );
                ( 8u,     Option<uint>.None, BitF.T, 4096,       4095,     false );    // Empty Data-Out PDU
            |] :> obj;
            0u :> obj;  // Residual Count
        |];
        [|
            [|//  DataSN  TTT                F       Data Start  Data End  Fake data
                ( 99u,    Option<uint>.None, BitF.F, 0,          1023,     false );
                ( 50u,    Option<uint>.None, BitF.F, 1024,       2047,     false );
                ( 99u,    Option<uint>.None, BitF.F, 2048,       3071,     false );
                ( 4u,     Option<uint>.None, BitF.T, 3072,       4095,     false );
            |] :> obj;
            0u :> obj;  // Residual Count
        |];
        [|
            [|//  DataSN  TTT                  F       Data Start  Data End  Fake data
                ( 0u,     Option<uint>.None,   BitF.F, 0,          1023,     false );
                ( 1u,     Option<uint>.None,   BitF.F, 1024,       2047,     false );
                ( 2u,     Some( 0xFFFFFFFFu ), BitF.F, 2048,       3071,     false );    // Received as is as unsolicited data.
                ( 3u,     Option<uint>.None,   BitF.T, 3072,       4095,     false );
            |] :> obj;
            0u :> obj;  // Residual Count
        |];
        [|
            [|//  DataSN  TTT                  F       Data Start  Data End  Fake data
                ( 0u,     Some( 0x11111111u ), BitF.F, 0,          4095,     true  );   // ignored
                ( 1u,     Option<uint>.None,   BitF.F, 0,          4095,     false );
                ( 2u,     Some( 0x22222222u ), BitF.F, 0,          4095,     true  );   // ignored
                ( 3u,     Option<uint>.None,   BitF.T, 4096,       4095,     false );
            |] :> obj;
            0u :> obj;  // Residual Count
        |];
    |]

    [<Theory>]
    [<MemberData( "m_R2T_NoUnsolicitedData_1R2T_MultiDataOutPDU_001_data" )>]
    member _.R2T_NoUnsolicitedData_1R2T_MultiDataOutPDU_001( v : ( uint * uint option * BitF * int * int * bool )[] ) ( expResidualCount : uint32 ) =
        task {
            let! r1 = CreateSession 0uy 16384u 16384u
            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Receive R2T
            let! r2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2tPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( r2tPDU.R2TSN = datasn_me.zero ))
            Assert.True(( r2tPDU.BufferOffset = 0u ))
            Assert.True(( r2tPDU.DesiredDataTransferLength = accessLength ))

            let writtenData = Array.zeroCreate( int accessLength )
            Random.Shared.NextBytes( writtenData )

            // Send data-Out PDU
            for i = 0 to v.Length - 1 do
                let argDataSN, argTTT, argF, argStart, argEnd, argFake = v.[i]
                let wttt =
                    match argTTT with
                    | Some( x ) -> ttt_me.fromPrim x
                    | None -> r2tPDU.TargetTransferTag
                let writtenData1 = PooledBuffer.Rent writtenData.[ argStart .. argEnd ]
                if argFake then
                    Random.Shared.NextBytes( writtenData1.ArraySegment.AsSpan() )
                do! r1.SendSCSIDataOutPDU g_CID0 argF writeITT g_LUN1 wttt ( datasn_me.fromPrim argDataSN ) ( uint32 argStart ) writtenData1
                writtenData1.Return()

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
            Assert.True(( writeRespPDU.ResidualCount = expResidualCount ))

            // Verify that the sent data was written correctly.
            let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
            Assert.True(( writtenData = wroteData ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Out of range Data-Out PDU
    [<Fact>]
    member _.R2T_NoUnsolicitedData_1R2T_MultiDataOutPDU_002() =
        task {
            let! r1 = CreateSession 0uy 16384u 16384u
            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Receive R2T
            let! r2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2tPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( r2tPDU.R2TSN = datasn_me.zero ))
            Assert.True(( r2tPDU.BufferOffset = 0u ))
            Assert.True(( r2tPDU.DesiredDataTransferLength = accessLength ))

            let writtenData = Array.zeroCreate( int accessLength )
            Random.Shared.NextBytes( writtenData )

            // Send data-Out PDU 1
            let writtenData1 = PooledBuffer.Rent( writtenData, 0, 4096 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.F writeITT g_LUN1 r2tPDU.TargetTransferTag datasn_me.zero 0u writtenData1

            // Send data-Out PDU 2 ( Out of range )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2tPDU.TargetTransferTag ( datasn_me.fromPrim 1u ) 2048u writtenData1
            writtenData1.Return()

            // Receive Reject PDU
            let! rejectPDU = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rejectPDU.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

            // Send data-Out PDU 3
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2tPDU.TargetTransferTag ( datasn_me.fromPrim 2u ) 4096u PooledBuffer.Empty
            writtenData1.Return()

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
            Assert.True(( writeRespPDU.ResidualCount = 0u ))

            // Verify that the sent data was written correctly.
            let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
            Assert.True(( writtenData = wroteData ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    // Incomplete data transfer. ErrorRecoveryLevel=0
    [<Fact>]
    member _.R2T_NoUnsolicitedData_1R2T_MultiDataOutPDU_003() =
        task {
            let! r1 = CreateSession 0uy 16384u 16384u
            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Receive R2T
            let! r2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2tPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( r2tPDU.R2TSN = datasn_me.zero ))
            Assert.True(( r2tPDU.BufferOffset = 0u ))
            Assert.True(( r2tPDU.DesiredDataTransferLength = accessLength ))

            let writtenData = Array.zeroCreate( int accessLength )
            Random.Shared.NextBytes( writtenData )

            // Send data-Out PDU 1
            let writtenData1 = PooledBuffer.Rent( writtenData, 0, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.F writeITT g_LUN1 r2tPDU.TargetTransferTag datasn_me.zero 0u writtenData1
            writtenData1.Return()

            // Send data-Out PDU 2
            let writtenData2 = PooledBuffer.Rent( writtenData, 1024, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2tPDU.TargetTransferTag ( datasn_me.fromPrim 1u ) 1024u writtenData2
            writtenData2.Return()

            // Receive SCSI Response PDU
            try
                let! _ = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    // Incomplete data transfer. ErrorRecoveryLevel>0
    [<Fact>]
    member _.R2T_NoUnsolicitedData_1R2T_MultiDataOutPDU_004() =
        task {
            let! r1 = CreateSession 1uy 16384u 16384u
            let sessParam = { m_defaultSessParam with ErrorRecoveryLevel = 1uy }
            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Receive R2T
            let! r2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2tPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( r2tPDU.R2TSN = datasn_me.zero ))
            Assert.True(( r2tPDU.BufferOffset = 0u ))
            Assert.True(( r2tPDU.DesiredDataTransferLength = accessLength ))

            let writtenData = Array.zeroCreate( int accessLength )
            Random.Shared.NextBytes( writtenData )

            // Send data-Out PDU 1
            let writtenData1 = PooledBuffer.Rent( writtenData, 0, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.F writeITT g_LUN1 r2tPDU.TargetTransferTag datasn_me.zero 0u writtenData1
            writtenData1.Return()

            // Send data-Out PDU 2
            let writtenData2 = PooledBuffer.Rent( writtenData, 1024, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2tPDU.TargetTransferTag ( datasn_me.fromPrim 1u ) 1024u writtenData2
            writtenData2.Return()

            // Receive recovery R2T PDU
            let! r2tPDU2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2tPDU2.InitiatorTaskTag = writeITT ))
            Assert.True(( r2tPDU2.R2TSN = datasn_me.fromPrim 1u ))
            Assert.True(( r2tPDU2.BufferOffset = 2048u ))
            Assert.True(( r2tPDU2.DesiredDataTransferLength = 2048u ))

            // Send data-Out PDU 2
            let writtenData3 = PooledBuffer.Rent( writtenData, 2048, 2048 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2tPDU2.TargetTransferTag ( datasn_me.fromPrim 2u ) 2048u writtenData3
            writtenData3.Return()

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
            Assert.True(( writeRespPDU.ResidualCount = 0u ))

            // Verify that the sent data was written correctly.
            let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
            Assert.True(( writtenData = wroteData ))

            do! r1.CloseSession g_CID0 BitI.F
        }
        
    // Send Data-Out PDUs in order for multiple R2Ts
    [<Fact>]
    member _.R2T_NoUnsolicitedData_MultiR2T_Ordered_001() =
        task {
            let! r1 = CreateSession 0uy 4096u 4096u
            let accessLength = 8192u
            let accessBlockCount = accessLength / m_MediaBlockSize

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Receive two R2T
            let vTTT = Array.zeroCreate< TTT_T >( 2 )
            for i = 0 to 1 do
                let! r2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
                Assert.True(( r2tPDU.InitiatorTaskTag = writeITT ))
                Assert.True(( r2tPDU.R2TSN = datasn_me.fromPrim ( uint32 i ) ))
                Assert.True(( r2tPDU.BufferOffset = ( uint32 i ) * 4096u ))
                Assert.True(( r2tPDU.DesiredDataTransferLength = 4096u ))
                vTTT.[i] <- r2tPDU.TargetTransferTag

            let writtenData = Array.zeroCreate( int accessLength )
            Random.Shared.NextBytes( writtenData )

            // Send data-Out PDU 0 for R2T 0
            let writtenData0 = PooledBuffer.Rent( writtenData, 0, 2048 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.F writeITT g_LUN1 vTTT.[0] datasn_me.zero 0u writtenData0
            writtenData0.Return()

            // Send data-Out PDU 1 for R2T 0
            let writtenData1 = PooledBuffer.Rent( writtenData, 2048, 2048 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 vTTT.[0] ( datasn_me.fromPrim 1u ) 2048u writtenData1
            writtenData1.Return()

            // Send data-Out PDU 2 for R2T 1
            let writtenData2 = PooledBuffer.Rent( writtenData, 4096, 2048 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.F writeITT g_LUN1 vTTT.[1] datasn_me.zero 4096u writtenData2
            writtenData2.Return()

            // Send data-Out PDU 3 for R2T 2
            let writtenData3 = PooledBuffer.Rent( writtenData, 6144, 2048 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 vTTT.[1] ( datasn_me.fromPrim 1u ) 6144u writtenData3
            writtenData3.Return()

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
            Assert.True(( writeRespPDU.ResidualCount = 0u ))

            // Verify that the sent data was written correctly.
            let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
            Assert.True(( writtenData = wroteData ))

            do! r1.CloseSession g_CID0 BitI.F
        }
        
    // Sending Data-Out PDUs in parallel to multiple R2Ts
    [<Fact>]
    member _.R2T_NoUnsolicitedData_MultiR2T_Interleaved_001() =
        task {
            let! r1 = CreateSession 0uy 4096u 4096u
            let accessLength = 8192u
            let accessBlockCount = accessLength / m_MediaBlockSize

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Receive two R2T
            let vTTT = Array.zeroCreate< TTT_T >( 2 )
            for i = 0 to 1 do
                let! r2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
                Assert.True(( r2tPDU.InitiatorTaskTag = writeITT ))
                Assert.True(( r2tPDU.R2TSN = datasn_me.fromPrim ( uint32 i ) ))
                Assert.True(( r2tPDU.BufferOffset = ( uint32 i ) * 4096u ))
                Assert.True(( r2tPDU.DesiredDataTransferLength = 4096u ))
                vTTT.[i] <- r2tPDU.TargetTransferTag

            let writtenData = Array.zeroCreate( int accessLength )
            Random.Shared.NextBytes( writtenData )

            // Send data-Out PDU 0 for R2T 0
            let writtenData0 = PooledBuffer.Rent( writtenData, 0, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.F writeITT g_LUN1 vTTT.[0] datasn_me.zero 0u writtenData0
            writtenData0.Return()

            // Send data-Out PDU 1 for R2T 1
            let writtenData1 = PooledBuffer.Rent( writtenData, 4096, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.F writeITT g_LUN1 vTTT.[1] datasn_me.zero 4096u writtenData1
            writtenData1.Return()

            // Send data-Out PDU 2 for R2T 0
            let writtenData2 = PooledBuffer.Rent( writtenData, 1024, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.F writeITT g_LUN1 vTTT.[0] ( datasn_me.fromPrim 1u ) 1024u writtenData2
            writtenData2.Return()

            // Send data-Out PDU 3 for R2T 1
            let writtenData3 = PooledBuffer.Rent( writtenData, 5120, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.F writeITT g_LUN1 vTTT.[1] ( datasn_me.fromPrim 1u ) 5120u writtenData3
            writtenData3.Return()

            // Send data-Out PDU 4 for R2T 0
            let writtenData4 = PooledBuffer.Rent( writtenData, 2048, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.F writeITT g_LUN1 vTTT.[0] ( datasn_me.fromPrim 2u ) 2048u writtenData4
            writtenData4.Return()

            // Send data-Out PDU 5 for R2T 1
            let writtenData5 = PooledBuffer.Rent( writtenData, 6144, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.F writeITT g_LUN1 vTTT.[1] ( datasn_me.fromPrim 2u ) 6144u writtenData5
            writtenData5.Return()

            // Send data-Out PDU 6 for R2T 0
            let writtenData6 = PooledBuffer.Rent( writtenData, 3072, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 vTTT.[0] ( datasn_me.fromPrim 3u ) 3072u writtenData6
            writtenData6.Return()

            // Send data-Out PDU 7 for R2T 1
            let writtenData7 = PooledBuffer.Rent( writtenData, 7168, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 vTTT.[1] ( datasn_me.fromPrim 3u ) 7168u writtenData7
            writtenData7.Return()

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
            Assert.True(( writeRespPDU.ResidualCount = 0u ))

            // Verify that the sent data was written correctly.
            let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
            Assert.True(( writtenData = wroteData ))

            do! r1.CloseSession g_CID0 BitI.F
        }
        
    // Incomplete data transmission
    [<Fact>]
    member _.R2T_NoUnsolicitedData_MultiR2T_Incomplete_001() =
        task {
            let! r1 = CreateSession 1uy 4096u 4096u
            let accessLength = 8192u
            let accessBlockCount = accessLength / m_MediaBlockSize

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Receive two R2T
            let vTTT = Array.zeroCreate< TTT_T >( 2 )
            for i = 0 to 1 do
                let! r2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
                Assert.True(( r2tPDU.InitiatorTaskTag = writeITT ))
                Assert.True(( r2tPDU.R2TSN = datasn_me.fromPrim ( uint32 i ) ))
                Assert.True(( r2tPDU.BufferOffset = ( uint32 i ) * 4096u ))
                Assert.True(( r2tPDU.DesiredDataTransferLength = 4096u ))
                vTTT.[i] <- r2tPDU.TargetTransferTag

            let writtenData = Array.zeroCreate( int accessLength )
            Random.Shared.NextBytes( writtenData )

            // Send data-Out PDU 0 for R2T 0 ( F=1 )
            let writtenData0 = PooledBuffer.Rent( writtenData, 0, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 vTTT.[0] datasn_me.zero 0u writtenData0
            writtenData0.Return()

            // Send data-Out PDU 1 for R2T 1 ( F=1 )
            let writtenData1 = PooledBuffer.Rent( writtenData, 4096, 1024 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 vTTT.[1] datasn_me.zero 4096u writtenData1
            writtenData1.Return()

            // Receive Recovery R2T 0
            let! r_r2tPDU0 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r_r2tPDU0.InitiatorTaskTag = writeITT ))
            Assert.True(( r_r2tPDU0.R2TSN = datasn_me.fromPrim 2u ))
            Assert.True(( r_r2tPDU0.BufferOffset = 1024u ))
            Assert.True(( r_r2tPDU0.DesiredDataTransferLength = 3072u ))

            // Receive Recovery R2T 1
            let! r_r2tPDU1 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r_r2tPDU1.InitiatorTaskTag = writeITT ))
            Assert.True(( r_r2tPDU1.R2TSN = datasn_me.fromPrim 3u ))
            Assert.True(( r_r2tPDU1.BufferOffset = 5120u ))
            Assert.True(( r_r2tPDU1.DesiredDataTransferLength = 3072u ))

            // Send data-Out PDU 2 for Recovery R2T 0
            let writtenData2 = PooledBuffer.Rent( writtenData, 1024, 3072 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r_r2tPDU0.TargetTransferTag datasn_me.zero 1024u writtenData2
            writtenData2.Return()

            // Send data-Out PDU 3 for Recovery R2T 1
            let writtenData3 = PooledBuffer.Rent( writtenData, 5120, 3072 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r_r2tPDU1.TargetTransferTag datasn_me.zero 5120u writtenData3
            writtenData3.Return()

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
            Assert.True(( writeRespPDU.ResidualCount = 0u ))

            // Verify that the sent data was written correctly.
            let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
            Assert.True(( writtenData = wroteData ))

            do! r1.CloseSession g_CID0 BitI.F
        }
        
    // Out of range data transmission
    [<Fact>]
    member _.R2T_NoUnsolicitedData_MultiR2T_OutOfRange_001() =
        task {
            let! r1 = CreateSession 1uy 4096u 4096u
            let accessLength = 8192u
            let accessBlockCount = accessLength / m_MediaBlockSize

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Receive two R2T
            let vTTT = Array.zeroCreate< TTT_T >( 2 )
            for i = 0 to 1 do
                let! r2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
                Assert.True(( r2tPDU.InitiatorTaskTag = writeITT ))
                Assert.True(( r2tPDU.R2TSN = datasn_me.fromPrim ( uint32 i ) ))
                Assert.True(( r2tPDU.BufferOffset = ( uint32 i ) * 4096u ))
                Assert.True(( r2tPDU.DesiredDataTransferLength = 4096u ))
                vTTT.[i] <- r2tPDU.TargetTransferTag

            let writtenData = Array.zeroCreate( int accessLength )
            Random.Shared.NextBytes( writtenData )

            // Send data-Out PDU 0 for R2T 0 ( F=1 )
            let writtenData0 = PooledBuffer.Rent( writtenData, 4096, 1024 )     // Out of range for R2T 0
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 vTTT.[0] datasn_me.zero 4096u writtenData0
            writtenData0.Return()

            // Send data-Out PDU 1 for R2T 1 ( F=1 )
            let writtenData1 = PooledBuffer.Rent( writtenData, 0, 1024 )    // Out of range for R2T 1
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 vTTT.[1] datasn_me.zero 0u writtenData1
            writtenData1.Return()

            // Regardless of whether an R2T is requested, all data within the range specified by ExpectedDataTransferLength is received.
            // Then, a Recovery R2T is generated for the missing data.

            // Receive Recovery R2T 0
            let! r_r2tPDU0 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r_r2tPDU0.InitiatorTaskTag = writeITT ))
            Assert.True(( r_r2tPDU0.R2TSN = datasn_me.fromPrim 2u ))
            Assert.True(( r_r2tPDU0.BufferOffset = 1024u ))
            Assert.True(( r_r2tPDU0.DesiredDataTransferLength = 3072u ))

            // Receive Recovery R2T 1
            let! r_r2tPDU1 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r_r2tPDU1.InitiatorTaskTag = writeITT ))
            Assert.True(( r_r2tPDU1.R2TSN = datasn_me.fromPrim 3u ))
            Assert.True(( r_r2tPDU1.BufferOffset = 5120u ))
            Assert.True(( r_r2tPDU1.DesiredDataTransferLength = 3072u ))

            // Send data-Out PDU 2 for Recovery R2T 0
            let writtenData2 = PooledBuffer.Rent( writtenData, 1024, 3072 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r_r2tPDU0.TargetTransferTag datasn_me.zero 1024u writtenData2
            writtenData2.Return()

            // Send data-Out PDU 3 for Recovery R2T 1
            let writtenData3 = PooledBuffer.Rent( writtenData, 5120, 3072 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r_r2tPDU1.TargetTransferTag datasn_me.zero 5120u writtenData3
            writtenData3.Return()

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
            Assert.True(( writeRespPDU.ResidualCount = 0u ))

            // Verify that the sent data was written correctly.
            let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
            Assert.True(( writtenData = wroteData ))

            do! r1.CloseSession g_CID0 BitI.F
        }
                
    // request resinding R2T
    [<Fact>]
    member _.R2T_NoUnsolicitedData_MultiR2T_SNACKR2T_001() =
        task {
            let! r1 = CreateSession 1uy 4096u 4096u
            let accessLength = 8192u
            let accessBlockCount = accessLength / m_MediaBlockSize

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Receive two R2T
            let vTTT = Array.zeroCreate< TTT_T >( 2 )
            for i = 0 to 1 do
                let! r2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
                Assert.True(( r2tPDU.InitiatorTaskTag = writeITT ))
                Assert.True(( r2tPDU.R2TSN = datasn_me.fromPrim ( uint32 i ) ))
                Assert.True(( r2tPDU.BufferOffset = ( uint32 i ) * 4096u ))
                Assert.True(( r2tPDU.DesiredDataTransferLength = 4096u ))
                vTTT.[i] <- r2tPDU.TargetTransferTag

            // send R2T SNACK
            do! r1.SendSNACKRequestPDU g_CID0 SnackReqTypeCd.DATA_R2T g_LUN1 writeITT g_DefTTT 0u 1u

            // Receive R2T 0
            let! r2tPDU2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( r2tPDU2.InitiatorTaskTag = writeITT ))
            Assert.True(( r2tPDU2.R2TSN = datasn_me.zero ))
            Assert.True(( r2tPDU2.BufferOffset = 0u ))
            Assert.True(( r2tPDU2.DesiredDataTransferLength = 4096u ))
            Assert.True(( r2tPDU2.TargetTransferTag = vTTT.[0] ))

            let writtenData = Array.zeroCreate( int accessLength )
            Random.Shared.NextBytes( writtenData )

            // Send data-Out PDU 0 for R2T 0
            let writtenData0 = PooledBuffer.Rent( writtenData, 0, 4096 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 vTTT.[0] datasn_me.zero 0u writtenData0
            writtenData0.Return()

            // Send data-Out PDU 1 for R2T 1 ( F=1 )
            let writtenData1 = PooledBuffer.Rent( writtenData, 4096, 4096 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 vTTT.[1] datasn_me.zero 4096u writtenData1
            writtenData1.Return()

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
            Assert.True(( writeRespPDU.ResidualCount = 0u ))

            // Verify that the sent data was written correctly.
            let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
            Assert.True(( writtenData = wroteData ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Theory>]
    //            ERL  SCSICommand  Unsolicited  Expected       Solicited    Sess      Expected       Data-Out for  RecoveryR2T
    //                 data         Data-Out     R2T            Data-Out     Recovery  RecoveryR2T    RecoveryR2T   retry or not
    [<InlineData( 0u,  0,           0, 0,        0u, 4096u,     0, 0,        true,     0u, 0u,        0, 0,         false )>]
    [<InlineData( 1u,  0,           0, 0,        0u, 4096u,     0, 0,        false,    0u, 4096u,     0, 0,         true )>]
    [<InlineData( 1u,  0,           0, 0,        0u, 4096u,     0, 0,        false,    0u, 4096u,     0, 4096,      false )>]
    [<InlineData( 0u,  1024,        0, 0,        1024u, 3072u,  0, 0,        true,     0u, 0u,        0, 0,         false )>]
    [<InlineData( 1u,  1024,        0, 0,        1024u, 3072u,  0, 0,        false,    1024u, 3072u,  0, 0,         true )>]
    [<InlineData( 1u,  1024,        0, 0,        1024u, 3072u,  0, 0,        false,    1024u, 3072u,  1024, 3072,   false )>]
    [<InlineData( 1u,  4096,        0, 0,        0u, 0u,        0, 0,        false,    0u, 0u,        0, 0,         false )>]
    [<InlineData( 0u,  0,           0, 1024,     1024u, 3072u,  0, 0,        true,     0u, 0u,        0, 0,         false )>]
    [<InlineData( 1u,  0,           0, 1024,     1024u, 3072u,  0, 0,        false,    1024u, 3072u,  0, 0,         true )>]
    [<InlineData( 1u,  0,           0, 1024,     1024u, 3072u,  0, 0,        false,    1024u, 3072u,  1024, 3072,   false )>]
    [<InlineData( 0u,  1024,        1024, 1024,  2048u, 2048u,  0, 0,        true,     0u, 0u,        0, 0,         false )>]
    [<InlineData( 1u,  1024,        1024, 1024,  2048u, 2048u,  0, 0,        false,    2048u, 2048u,  0, 0,         true )>]
    [<InlineData( 1u,  1024,        1024, 1024,  2048u, 2048u,  0, 0,        false,    2048u, 2048u,  2048, 2048,   false )>]
    [<InlineData( 1u,  0,           0, 4096,     0u, 0u,        0, 0,        false,    0u, 0u,        0, 0,         false )>]
    [<InlineData( 1u,  1024,        1024, 3072,  0u, 0u,        0, 0,        false,    0u, 0u,        0, 0,         false )>]
    [<InlineData( 0u,  0,           0, 0,        0u, 4096u,     0, 1024,     true,     0u, 0u,        0, 0,         false )>]
    [<InlineData( 1u,  0,           0, 0,        0u, 4096u,     0, 1024,     false,    1024u, 3072u,  0, 0,         true )>]
    [<InlineData( 1u,  0,           0, 0,        0u, 4096u,     0, 1024,     false,    1024u, 3072u,  1024, 3072,   false )>]
    [<InlineData( 0u,  1024,        0, 0,        1024u, 3072u,  1024, 1024,  true,     0u, 0u,        0, 0,         false )>]
    [<InlineData( 1u,  1024,        0, 0,        1024u, 3072u,  1024, 1024,  false,    2048u, 2048u,  0, 0,         true )>]
    [<InlineData( 1u,  1024,        0, 0,        1024u, 3072u,  1024, 1024,  false,    2048u, 2048u,  2048, 2048,   false )>]
    [<InlineData( 0u,  0,           0, 1024,     1024u, 3072u,  1024, 1024,  true,     0u, 0u,        0, 0,         false )>]
    [<InlineData( 1u,  0,           0, 1024,     1024u, 3072u,  1024, 1024,  false,    2048u, 2048u,  0, 0,         true )>]
    [<InlineData( 1u,  0,           0, 1024,     1024u, 3072u,  1024, 1024,  false,    2048u, 2048u,  2048, 2048,   false )>]
    [<InlineData( 0u,  1024,        1024, 1024,  2048u, 2048u,  2048, 1024,  true,     0u, 0u,        0, 0,         false )>]
    [<InlineData( 1u,  1024,        1024, 1024,  2048u, 2048u,  2048, 1024,  false,    3072u, 1024u,  0, 0,         true )>]
    [<InlineData( 1u,  1024,        1024, 1024,  2048u, 2048u,  2048, 1024,  false,    3072u, 1024u,  3072, 1024,   false )>]
    [<InlineData( 0u,  0,           0, 0,        0u,    4096u,  0, 4096,     false,    0u, 0u,        0, 0,         false )>]
    [<InlineData( 0u,  1024,        0, 0,        1024u, 3072u,  1024, 3072,  false,    0u, 0u,        0, 0,         false )>]
    [<InlineData( 0u,  0,           0, 1024,     1024u, 3072u,  1024, 3072,  false,    0u, 0u,        0, 0,         false )>]
    [<InlineData( 0u,  1024,        1024, 1024,  2048u, 2048u,  2048, 2048,  false,    0u, 0u,        0, 0,         false )>]
    member _.UnsolicitedData_001
            ( errorRecoveryLevel : byte )
            ( range1Count : int32 )                                     // SCSI Command Unsolicited data
            ( range2Start : int32 ) ( range2Count : int32 )             // Unsolicited Data-Out PDU
            ( expR2TBufferOffset : uint32 ) ( expR2TLength : uint32 )   // Expected R2T PDU
            ( range3Start : int32 ) ( range3Count : int32 )             // Solicited Data-Out PDU for R2T
            ( isSessionRecovery : bool )
            ( expRR2TBufferOffset : uint32 ) ( expRR2TLength : uint32 ) // Expected Recovery R2T PDU
            ( range4Start : int32 ) ( range4Count : int32 )             // Data-Out PDU for Recovery R2T
            ( retryRecoveryR2T : bool ) =
        task {
            let! r1 = CreateSession errorRecoveryLevel 4096u 4096u
            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize
            let writtenData = Array.zeroCreate( int accessLength )
            Random.Shared.NextBytes( writtenData )

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let writtenData1 = PooledBuffer.Rent( writtenData, 0, range1Count )
            let scsiCommandFFlag = ( range2Count = 0 ) |> BitF.ofBool
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F scsiCommandFFlag BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB writtenData1 0u
            writtenData1.Return()

            // Send Unsolicited Data-Out PDU
            if scsiCommandFFlag = BitF.F then
                let writtenData2 = PooledBuffer.Rent( writtenData, range2Start, range2Count )
                do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 g_DefTTT datasn_me.zero ( uint32 range2Start ) writtenData2
                writtenData2.Return()

            // Receive R2T PDU
            let mutable r2tTTT = g_DefTTT
            if expR2TLength > 0u then
                let! r2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
                Assert.True(( r2tPDU.InitiatorTaskTag = writeITT ))
                Assert.True(( r2tPDU.R2TSN = datasn_me.zero ))
                Assert.True(( r2tPDU.BufferOffset = expR2TBufferOffset ))
                Assert.True(( r2tPDU.DesiredDataTransferLength = expR2TLength ))
                r2tTTT <- r2tPDU.TargetTransferTag

            // Send Solicited Data-Out PDU
            if expR2TLength > 0u then
                let writtenData3 = PooledBuffer.Rent( writtenData, range3Start, range3Count )
                do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 r2tTTT datasn_me.zero ( uint32 range3Start ) writtenData3
                writtenData3.Return()

            if isSessionRecovery then
                try
                    let! _ = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException
                | :? ConnectionErrorException ->
                    ()
            else
                // Receive Recovery R2T PDU
                let mutable rr2tTTT = g_DefTTT
                if expRR2TLength > 0u then
                    let! rr2tPDU = r1.ReceiveSpecific<R2TPDU> g_CID0
                    Assert.True(( rr2tPDU.InitiatorTaskTag = writeITT ))
                    Assert.True(( rr2tPDU.R2TSN = datasn_me.fromPrim 1u ))
                    Assert.True(( rr2tPDU.BufferOffset = expRR2TBufferOffset ))
                    Assert.True(( rr2tPDU.DesiredDataTransferLength = expRR2TLength ))
                    rr2tTTT <- rr2tPDU.TargetTransferTag

                // Send Solicited Data-Out PDU
                if expRR2TLength > 0u then
                    let writtenData4 = PooledBuffer.Rent( writtenData, range4Start, range4Count )
                    do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 rr2tTTT datasn_me.zero ( uint32 range4Start ) writtenData4
                    writtenData4.Return()

                // Receive recovery R2T again.
                if retryRecoveryR2T then
                    let! rr2tPDU2 = r1.ReceiveSpecific<R2TPDU> g_CID0
                    Assert.True(( rr2tPDU2.InitiatorTaskTag = writeITT ))
                    Assert.True(( rr2tPDU2.R2TSN = datasn_me.fromPrim 2u ))

                    // Send all of solicited Data
                    let writtenData5 = PooledBuffer.Rent( writtenData, int rr2tPDU2.BufferOffset, int rr2tPDU2.DesiredDataTransferLength )
                    do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT g_LUN1 rr2tPDU2.TargetTransferTag datasn_me.zero rr2tPDU2.BufferOffset writtenData5
                    writtenData5.Return()

                // Receive SCSI Response PDU
                let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
                Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
                Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
                Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT ))
                Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
                Assert.True(( writeRespPDU.ResidualCount = 0u ))

                // Verify that the sent data was written correctly.
                let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
                Assert.True(( writtenData = wroteData ))

                do! r1.CloseSession g_CID0 BitI.F
        }

    static member m_UnsolicitedData_VariousDataOutPDU_001_data = [|
        [|
            [|//  DataSN  TTT          F       Data Start  Data End  Fake data
                ( 0u,     0xFFFFFFFFu, BitF.F, 0,          1023,     false );
                ( 1u,     0xFFFFFFFFu, BitF.F, 1024,       2047,     false );
                ( 2u,     0xFFFFFFFFu, BitF.F, 2048,       3071,     false );
                ( 3u,     0xFFFFFFFFu, BitF.T, 3072,       4095,     false );
            |] :> obj;
            0u :> obj;  // Residual Count
        |];
        [|
            [|//  DataSN  TTT          F       Data Start  Data End  Fake data
                ( 3u,     0xFFFFFFFFu, BitF.F, 2048,       3071,     false );
                ( 1u,     0xFFFFFFFFu, BitF.F, 3072,       4095,     false );
                ( 0u,     0xFFFFFFFFu, BitF.F, 1024,       2047,     false );
                ( 2u,     0xFFFFFFFFu, BitF.T, 0,          1023,     false );
            |] :> obj;
            0u :> obj;  // Residual Count
        |];
        [|
            [|//  DataSN  TTT          F       Data Start  Data End  Fake data
                ( 0u,     0xFFFFFFFFu, BitF.F, 0,          3071,     false );
                ( 1u,     0xFFFFFFFFu, BitF.F, 1024,       2047,     false );
                ( 2u,     0xFFFFFFFFu, BitF.T, 3072,       4095,     false );
            |] :> obj;
            1024u :> obj;  // Residual Count
        |];
        [|
            [|//  DataSN  TTT          F       Data Start  Data End  Fake data
                ( 0u,     0xFFFFFFFFu, BitF.F, 0,          -1,       false );   // 0 byte Data-Out
                ( 1u,     0xFFFFFFFFu, BitF.F, 0,          2047,     false );
                ( 2u,     0xFFFFFFFFu, BitF.F, 2048,       2047,     false );   // 0 byte Data-Out
                ( 3u,     0xFFFFFFFFu, BitF.F, 2048,       4095,     false );
                ( 4u,     0xFFFFFFFFu, BitF.T, 4096,       4095,     false );   // 0 byte Data-Out
            |] :> obj;
            0u :> obj;  // Residual Count
        |];
        [|
            [|//  DataSN  TTT          F       Data Start  Data End  Fake data
                ( 99u,    0xFFFFFFFFu, BitF.F, 0,          2047,     false );
                ( 13u,    0xFFFFFFFFu, BitF.F, 2048,       3071,     false );
                ( 99u,    0xFFFFFFFFu, BitF.T, 3072,       4095,     false );
            |] :> obj;
            0u :> obj;  // Residual Count
        |];
        [|
            [|//  DataSN  TTT          F       Data Start  Data End  Fake data
                ( 0u,     0x11111111u, BitF.F, 0,          4095,     true );     // ignored
                ( 1u,     0xFFFFFFFFu, BitF.F, 0,          2047,     false );
                ( 2u,     0xFFFFFFFFu, BitF.F, 2048,       4095,     false );
                ( 3u,     0x22222222u, BitF.T, 4096,       4095,     true );     // ignored
                ( 4u,     0xFFFFFFFFu, BitF.T, 4096,       4095,     false );
            |] :> obj;
            0u :> obj;  // Residual Count
        |];
    |]

    [<Theory>]
    [<MemberData( "m_UnsolicitedData_VariousDataOutPDU_001_data" )>]
    member _.UnsolicitedData_VariousDataOutPDU_001 ( v : ( uint * uint * BitF * int * int * bool )[] ) ( expResidualCount : uint32 ) =
        task {
            let! r1 = CreateSession 1uy 4096u 4096u
            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize
            let writtenData = Array.zeroCreate( int accessLength )
            Random.Shared.NextBytes( writtenData )

            // Send SCSI write
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let! writeITT, _ = r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // Send data-Out PDU
            for i = 0 to v.Length - 1 do
                let argDataSN, argTTT, argF, argStart, argEnd, argFake = v.[i]
                let writtenData1 = PooledBuffer.Rent writtenData.[ argStart .. argEnd ]
                if argFake then
                    Random.Shared.NextBytes( writtenData1.ArraySegment.AsSpan() )
                do! r1.SendSCSIDataOutPDU g_CID0 argF writeITT g_LUN1 ( ttt_me.fromPrim argTTT ) ( datasn_me.fromPrim argDataSN ) ( uint32 argStart ) writtenData1
                writtenData1.Return()

            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT ))
            Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
            Assert.True(( writeRespPDU.ResidualCount = expResidualCount ))

            // Verify that the sent data was written correctly.
            let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
            Assert.True(( writtenData = wroteData ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.UnsolicitedData_DropScsiCommandPDU_001() =
        task {
            let! r1 = CreateSession 1uy 4096u 4096u
            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize
            let writtenData = Array.zeroCreate( int accessLength )
            Random.Shared.NextBytes( writtenData )

            let oldITT = r1.ITT

            // Send SCSI write ( dropped at target )
            let writeCDB = GenScsiCDB.Write10 0uy DPO.F FUA.F FUA_NV.F 0u 0uy ( uint16 accessBlockCount ) NACA.F LINK.F
            let writtenData0 = PooledBuffer.Rent( writtenData, 0, 1024 )
            let! writeITT1, writeCmdSN1 =
                r1.SendSCSICommandPDU_Test id ( ValueSome( 500u, 600u ) ) g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB writtenData0 0u

            // Send Data-Out PDU
            let writtenData1 = PooledBuffer.Rent( writtenData, 1024, 3072 )
            do! r1.SendSCSIDataOutPDU g_CID0 BitF.T writeITT1 g_LUN1 g_DefTTT datasn_me.zero 1024u writtenData1
            writtenData1.Return()

            // Receive Reject PDU
            let! _ = r1.ReceiveSpecific<RejectPDU> g_CID0

            // rewind CmdAN and ITT
            r1.RewindCmdSN ( cmdsn_me.fromPrim 1u )
            r1.ITT <- oldITT

            // Re-Send SCSI write
            let! writeITT2, writeCmdSN2 =
                r1.SendSCSICommandPDU g_CID0 BitI.F BitF.F BitR.F BitW.T TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB writtenData0 0u
            Assert.True(( writeITT1 = writeITT2 ))
            Assert.True(( writeCmdSN1 = writeCmdSN2 ))
            writtenData0.Return()


            // Receive SCSI Response PDU
            let! writeRespPDU = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( writeRespPDU.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( writeRespPDU.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( writeRespPDU.InitiatorTaskTag = writeITT1 ))
            Assert.True(( writeRespPDU.BidirectionalReadResidualCount = 0u ))
            Assert.True(( writeRespPDU.ResidualCount = 0u ))

            // Verify that the sent data was written correctly.
            let! wroteData = r1.ReadMediaData g_CID0 g_LUN1 0u accessBlockCount m_MediaBlockSize
            Assert.True(( writtenData = wroteData ))

            do! r1.CloseSession g_CID0 BitI.F
        }
