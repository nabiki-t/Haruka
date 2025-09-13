//=============================================================================
// Haruka Software Storage.
// ConnectionTest.fs : Test cases for Connection class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.TargetDevice

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Immutable

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes
open Haruka.TargetDevice
open Haruka.Test

//=============================================================================
// Class implementation

type Connection_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100000u, 200000u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    static member defaultNopOUTPDUValues = {
        I = true;
        LUN = lun_me.fromPrim 0x0001020304050607UL;
        InitiatorTaskTag = itt_me.fromPrim 0u;
        TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
        CmdSN = cmdsn_me.fromPrim 0xDDDDDDDDu;
        ExpStatSN = statsn_me.fromPrim 0xCCCCCCCCu;
        PingData = PooledBuffer.Rent [| 0x00uy .. 0xFFuy |];
        ByteCount = 0u;
    }

    static member defaultNopInPDUValues = {
        LUN = lun_me.fromPrim 0x0001020304050607UL;
        InitiatorTaskTag = itt_me.fromPrim 0u;
        TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
        StatSN = statsn_me.zero;
        ExpCmdSN = cmdsn_me.zero;
        MaxCmdSN = cmdsn_me.zero;
        PingData = PooledBuffer.Empty;
    }

    static member defaultSCSIResponsePDUValues = {
        o = false;
        u = false;
        O = false;
        U = false;
        Response = iScsiSvcRespCd.COMMAND_COMPLETE;
        Status = ScsiCmdStatCd.GOOD;
        InitiatorTaskTag = itt_me.fromPrim 0u;
        SNACKTag = snacktag_me.fromPrim 0u;
        StatSN = statsn_me.zero;
        ExpCmdSN = cmdsn_me.zero;
        MaxCmdSN = cmdsn_me.zero;
        ExpDataSN = datasn_me.zero;
        BidirectionalReadResidualCount = 0u;
        ResidualCount = 0u;
        SenseLength = 0us;
        SenseData = ArraySegment.Empty;
        ResponseData = ArraySegment.Empty;
        ResponseFence = ResponseFenceNeedsFlag.R_Mode;
        DataInBuffer = PooledBuffer.Empty;
        LUN = lun_me.zero;
    }

    static member defaultSCSIDataInPDUValues = {
        F = true;
        A = false;
        O = false;
        U = false;
        S = false;
        Status = ScsiCmdStatCd.GOOD;
        LUN = lun_me.zero;
        InitiatorTaskTag = itt_me.fromPrim 0u;
        TargetTransferTag = ttt_me.fromPrim 0xffffffffu;
        StatSN = statsn_me.zero;
        ExpCmdSN = cmdsn_me.zero;
        MaxCmdSN = cmdsn_me.zero;
        DataSN = datasn_me.zero;
        BufferOffset = 0u;
        ResidualCount = 0u;
        DataSegment = ArraySegment( [||], 0, 0 )
        ResponseFence = ResponseFenceNeedsFlag.Immediately;
    }

    static member defaultR2TPDUValues = {
        LUN = lun_me.zero;
        InitiatorTaskTag = itt_me.fromPrim 0u;
        TargetTransferTag = ttt_me.fromPrim 0u;
        StatSN = statsn_me.zero;
        ExpCmdSN = cmdsn_me.zero;
        MaxCmdSN = cmdsn_me.zero;
        R2TSN = datasn_me.zero;
        BufferOffset = 0u;
        DesiredDataTransferLength = 1u;
    }

    static member defaultSessionParameter = {
        MaxConnections = Constants.NEGOPARAM_MaxConnections;
        TargetGroupID = tgid_me.Zero;
        TargetConf = {
            IdentNumber = tnodeidx_me.fromPrim 0u;
            TargetName = "abcT";
            TargetAlias = "";
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            LUN = [];
            Auth = TargetGroupConf.T_Auth.U_None();
        };
        InitiatorName = "abcI";
        InitiatorAlias = "abcIA";
        TargetPortalGroupTag = tpgt_me.zero;
        InitialR2T = false;
        ImmediateData = true;
        MaxBurstLength = 4096u;
        FirstBurstLength = 4096u;
        DefaultTime2Wait = 1us;
        DefaultTime2Retain = 1us;
        MaxOutstandingR2T = 1us;
        DataPDUInOrder = true;
        DataSequenceInOrder = true;
        ErrorRecoveryLevel = 0uy;
    }

    static member defaultConnectionParam = {
        AuthMethod = [| AuthMethodCandidateValue.AMC_None |];
        HeaderDigest = [| DigestType.DST_None |];
        DataDigest = [| DigestType.DST_None |];
        MaxRecvDataSegmentLength_I = 4096u;
        MaxRecvDataSegmentLength_T = 4096u;
        
    }

    static member createDefaultConnectionObj(
        conParam : IscsiNegoParamCO,
        sessParam : IscsiNegoParamSW,
        initStatSN : STATSN_T
    ) =
        let statusStub = new CStatus_Stub()
        let sessStub = new CSession_Stub(
            p_UpdateMaxCmdSN = ( fun () -> ( cmdsn_me.fromPrim 1u, cmdsn_me.fromPrim 2u ) )
        )
        let sp, cp = GlbFunc.GetNetConn()
        let killer = new HKiller() :> IKiller
        let cdt = DateTime.UtcNow
        let con =
            new Connection(
                statusStub :> IStatus,
                tpgt_me.zero,
                sp,
                cdt,
                conParam,
                sessParam,
                sessStub,
                tsih_me.fromPrim 0us,
                cid_me.zero,
                concnt_me.zero,
                netportidx_me.zero,
                killer
            )
            :> IConnection

        let pc = PrivateCaller( con )
        pc.SetField( "m_StatSN", (*statsn_me.toPrim *)initStatSN )
        Assert.True(( con.ConnectedDate = cdt ))
        statusStub, sessStub, killer, sp, cp, con

    static member getSentRespPDUs ( con : IConnection ) =
        let pc = PrivateCaller( con )
        let m_ResendStat = pc.GetField( "m_ResendStat" ) :?> OptimisticLock< ResendStatusRec >
        m_ResendStat.obj.m_SentRespPDUs

    static member getSentDataInPDUs ( con : IConnection ) =
        let pc = PrivateCaller( con )
        let m_ResendStat = pc.GetField( "m_ResendStat" ) :?> OptimisticLock< ResendStatusRec >
        m_ResendStat.obj.m_SentDataInPDUs

    static member getR_SNACK_Request ( con : IConnection ) =
        let pc = PrivateCaller( con )
        let m_ResendStat = pc.GetField( "m_ResendStat" ) :?> OptimisticLock< ResendStatusRec >
        m_ResendStat.obj.m_R_SNACK_Request

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.StartFullFeaturePhase_001() =
        let _, sessStub, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let mutable cnt = 0
        let mutable cnt2 = 0
        let wlock = new SemaphoreSlim( 1 )

        sessStub.p_PushReceivedPDU <- ( fun argconn pdu ->
                cnt <- cnt + 1
                Assert.Same( con, argconn )
                Assert.True(( pdu.Opcode = OpcodeCd.NOP_OUT ))
                raise <| Exception( "" )
            )
        sessStub.p_GetConnection <- ( fun cid concnt ->
            Assert.True(( cid = cid_me.fromPrim 0us ))
            ValueSome con
        )
        sessStub.p_Terminate <- ( fun () ->
                cnt2 <- cnt2 + 1
                wlock.Release() |> ignore
            )

        killer.Add sessStub

        wlock.Wait()
        con.StartFullFeaturePhase()

        PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, {
            Connection_Test.defaultNopOUTPDUValues with
                ExpStatSN = statsn_me.zero;
        } )
        |> Functions.RunTaskSynchronously
        |> ignore

        wlock.Wait()
        Assert.True(( cnt = 1 ))
        Assert.True(( cnt2 = 1 ))
        Assert.True(( killer.IsNoticed ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.StartFullFeaturePhase_002() =
        let _, sessStub, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let mutable cnt = 0
        let wlock = new SemaphoreSlim( 1 )

        sessStub.p_PushReceivedPDU <- ( fun argconn pdu ->
                cnt <- cnt + 1
                Assert.Same( con, argconn )
                Assert.True(( pdu.Opcode = OpcodeCd.NOP_OUT ))
                raise <| Exception( "" )
            )
        sessStub.p_GetConnection <- ( fun cid concnt ->
            Assert.True(( cid = cid_me.fromPrim 0us ))
            wlock.Release() |> ignore
            ValueNone
        )

        killer.Add sessStub

        wlock.Wait()
        con.StartFullFeaturePhase()

        PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, {
            Connection_Test.defaultNopOUTPDUValues with
                ExpStatSN = statsn_me.zero;
        } )
        |> Functions.RunTaskSynchronously
        |> ignore

        wlock.Wait()
        Assert.True(( cnt = 1 ))
        Assert.False(( killer.IsNoticed ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendPDU_001() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.fromPrim 1u
            )

        con.SendPDU( Connection_Test.defaultNopInPDUValues )

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True(( rpdu.Opcode = OpcodeCd.NOP_IN ))
        let rpdu2 = rpdu :?> NOPInPDU
        Assert.True(( rpdu2.LUN = lun_me.fromPrim 0x0001020304050607UL ))
        Assert.True(( rpdu2.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( rpdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( rpdu2.MaxCmdSN = cmdsn_me.fromPrim 2u ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendPDU_002() =
        let _, _, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let b = new Barrier( 2 )
        let dummyObj = CComponent_Stub()
        dummyObj.p_Terminate <- ( fun () -> b.SignalAndWait() )
        killer.Add dummyObj

        sp.Close()
        cp.Close()

        con.SendPDU( Connection_Test.defaultNopInPDUValues )

        b.SignalAndWait()
        Assert.True(( killer.IsNoticed ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendPDU_003() =
        let _, _, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let mutable cnt = 0
        let pc = PrivateCaller( con )
        let s = new SemaphoreSlim( 1 )
        s.Wait()

        let vR_SNACK_Request =
            [|
                struct( 
                    itt_me.fromPrim 1u,
                    ( fun () ->
                        cnt <- cnt + 1
                        s.Release() |> ignore
                    )
                )
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = vR_SNACK_Request.ToImmutableArray();
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        sp.Close()

        con.SendPDU( {
                Connection_Test.defaultSCSIResponsePDUValues with
                    InitiatorTaskTag = itt_me.fromPrim 1u
            }
        )

        s.Wait()
        Assert.False(( killer.IsNoticed ))
        Assert.True(( cnt = 1 ))
        let m_R_SNACK_Request = Connection_Test.getR_SNACK_Request con
        Assert.True(( m_R_SNACK_Request.Length = 0 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ReSendPDU_001() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )

        con.ReSendPDU( {
                Connection_Test.defaultNopInPDUValues with
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    StatSN = statsn_me.fromPrim 5u;
            }
        )

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True(( rpdu.Opcode = OpcodeCd.NOP_IN ))
        let rpdu2 = rpdu :?> NOPInPDU
        Assert.True(( rpdu2.LUN = lun_me.fromPrim 0x0001020304050607UL ))
        Assert.True(( rpdu2.InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( rpdu2.StatSN = statsn_me.fromPrim 5u ))
        Assert.True(( rpdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( rpdu2.MaxCmdSN = cmdsn_me.fromPrim 2u ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ReSendPDU_002() =
        let _, _, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let b = new Barrier( 2 )
        let dummyObj = CComponent_Stub()
        dummyObj.p_Terminate <- ( fun () -> b.SignalAndWait() )
        killer.Add dummyObj

        GlbFunc.ClosePorts [| sp; |]

        con.ReSendPDU( {
                Connection_Test.defaultNopInPDUValues with
                    InitiatorTaskTag = itt_me.fromPrim 1u
            }
        )

        b.SignalAndWait()

        Assert.True(( killer.IsNoticed ))
        GlbFunc.ClosePorts [| cp; |]

    [<Fact>]
    member _.ReSendPDUForRSnack_001() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )

        con.ReSendPDUForRSnack( {
                Connection_Test.defaultNopInPDUValues with
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    StatSN = statsn_me.fromPrim 5u;
            }
        )

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True(( rpdu.Opcode = OpcodeCd.NOP_IN ))
        let rpdu2 = rpdu :?> NOPInPDU
        Assert.True(( rpdu2.LUN = lun_me.fromPrim 0x0001020304050607UL ))
        Assert.True(( rpdu2.InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( rpdu2.StatSN = statsn_me.fromPrim 5u ))
        Assert.True(( rpdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( rpdu2.MaxCmdSN = cmdsn_me.fromPrim 2u ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))
        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ReSendPDUForRSnack_002() =
        let _, _, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let b = new Barrier( 2 )
        let dummyObj = CComponent_Stub()
        dummyObj.p_Terminate <- ( fun () -> b.SignalAndWait() )
        killer.Add dummyObj

        sp.Close()

        con.ReSendPDUForRSnack( {
                Connection_Test.defaultNopInPDUValues with
                    InitiatorTaskTag = itt_me.fromPrim 1u
            }
        )

        b.SignalAndWait()
        Assert.True(( killer.IsNoticed ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ReSendPDUForRSnack_003() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        con.ReSendPDUForRSnack( {
                Connection_Test.defaultSCSIResponsePDUValues with
                    InitiatorTaskTag = itt_me.fromPrim 12u;
                    StatSN = statsn_me.fromPrim 5u;
            }
        )

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True(( rpdu.Opcode = OpcodeCd.SCSI_RES ))
        let rpdu2 = rpdu :?> SCSIResponsePDU
        Assert.True(( rpdu2.InitiatorTaskTag = itt_me.fromPrim 12u ))
        Assert.True(( rpdu2.StatSN = statsn_me.fromPrim 5u ))
        Assert.True(( rpdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( rpdu2.MaxCmdSN = cmdsn_me.fromPrim 2u ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 1 ))
        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))

        let struct( savedStatSN, savedPDU ) = m_SentRespPDUs.[0]
        Assert.True(( savedStatSN = statsn_me.fromPrim 5u ))
        Assert.True(( savedPDU.Opcode = OpcodeCd.SCSI_RES ))
        Assert.True(( savedPDU.InitiatorTaskTag = itt_me.fromPrim 12u ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ReSendPDUForRSnack_004() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        con.ReSendPDUForRSnack( {
                Connection_Test.defaultSCSIDataInPDUValues with
                    InitiatorTaskTag = itt_me.fromPrim 13u;
                    LUN = lun_me.fromPrim 0x0001020304050607UL;
                    DataSN = datasn_me.fromPrim 14u;
            }
        )

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True(( rpdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
        let rpdu2 = rpdu :?> SCSIDataInPDU
        Assert.True(( rpdu2.LUN = lun_me.fromPrim 0x0001020304050607UL ))
        Assert.True(( rpdu2.InitiatorTaskTag = itt_me.fromPrim 13u ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))
        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 1 ))

        let struct( rITT , rDataSN, savedPDU ) = m_SentDataInPDUs.[0]
        Assert.True(( rITT = itt_me.fromPrim 13u ))
        Assert.True(( rDataSN = datasn_me.fromPrim 14u ))
        Assert.True(( savedPDU.Opcode = OpcodeCd.SCSI_DATA_IN ))
        Assert.True(( savedPDU.InitiatorTaskTag = itt_me.fromPrim 13u ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.NotifyUpdateConnectionParameter_001() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )

        Assert.True(( con.CurrentParams.MaxRecvDataSegmentLength_I = 4096u ))

        con.NotifyUpdateConnectionParameter( {
            Connection_Test.defaultConnectionParam with
                MaxRecvDataSegmentLength_I = 100u;
        } )

        Assert.True(( con.CurrentParams.MaxRecvDataSegmentLength_I = 100u ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.NotifyR2TSatisfied_001() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )
        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >
                                .Empty
                                .Add( struct ( itt_me.fromPrim 0u, datasn_me.zero, Connection_Test.defaultSCSIDataInPDUValues ) );
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T *  ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        con.NotifyR2TSatisfied ( itt_me.fromPrim 0u ) ( datasn_me.zero )

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.NotifyDataAck_001() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )
        let vSentDataInPDU =
            [|
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            A = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0xffffffffu;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            A = false;
                            LUN = lun_me.fromPrim 1UL;
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            TargetTransferTag = ttt_me.fromPrim 0xffffffffu;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            A = true;
                            LUN = lun_me.fromPrim 1UL;
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            TargetTransferTag = ttt_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 2u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            A = false;
                            LUN = lun_me.fromPrim 1UL;
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 3u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            A = false;
                            LUN = lun_me.fromPrim 1UL;
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 2u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            A = false;
                            LUN = lun_me.fromPrim 1UL;
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                            TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = vSentDataInPDU.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        con.NotifyDataAck ( ttt_me.fromPrim 2u ) ( lun_me.fromPrim 1UL ) ( datasn_me.fromPrim 2u )

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 4 ))

        let r1 = Seq.tryFind ( fun struct( a, b, _ ) -> a = itt_me.fromPrim 0u && b = datasn_me.zero ) m_SentDataInPDUs
        Assert.True r1.IsSome

        let r2 = Seq.tryFind ( fun struct( a, b, _ ) -> a = itt_me.fromPrim 1u && b = datasn_me.fromPrim 2u ) m_SentDataInPDUs
        Assert.True r2.IsSome

        let r3 = Seq.tryFind ( fun struct( a, b, _ ) -> a = itt_me.fromPrim 1u && b = datasn_me.fromPrim 3u ) m_SentDataInPDUs
        Assert.True r3.IsSome

        let r4 = Seq.tryFind ( fun struct( a, b, _ ) -> a = itt_me.fromPrim 2u && b = datasn_me.zero ) m_SentDataInPDUs
        Assert.True r4.IsSome

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.NotifyDataAck_002() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )
        let vSentDataInPDU = 
            [|
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            A = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0xffffffffu;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = vSentDataInPDU.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        con.NotifyDataAck ( ttt_me.fromPrim 2u ) ( lun_me.fromPrim 1UL ) ( datasn_me.fromPrim 2u )

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 1 ))
        let struct( a, b, _ ) = m_SentDataInPDUs.[0]
        Assert.True(( a = itt_me.fromPrim 0u ))
        Assert.True(( b = datasn_me.zero ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.NotifyDataAck_003() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        con.NotifyDataAck ( ttt_me.fromPrim 2u ) ( lun_me.fromPrim 1UL ) ( datasn_me.fromPrim 2u )
        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentDataInPDUForSNACK_001() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentDataInPDU =
            [|
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF00u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF01u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF02u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 2u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF03u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 3u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF04u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 4u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF05u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = vSentDataInPDU.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentDataInPDUForSNACK ( itt_me.fromPrim 1u ) ( datasn_me.fromPrim 2u ) 2u
        Assert.True(( v.Length = 2 ))
        Assert.True(( v.[0].InitiatorTaskTag = itt_me.fromPrim 0xFF03u ))
        Assert.True(( v.[1].InitiatorTaskTag = itt_me.fromPrim 0xFF04u ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentDataInPDUForSNACK_002() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )
        let vSentDataInPDU =
            [|
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF00u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF01u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = vSentDataInPDU.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentDataInPDUForSNACK ( itt_me.fromPrim 2u ) ( datasn_me.fromPrim 2u ) 1u
        Assert.True(( v.Length = 0 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentDataInPDUForSNACK_003() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let v = con.GetSentDataInPDUForSNACK ( itt_me.fromPrim 2u ) ( datasn_me.fromPrim 2u ) 1u
        Assert.True(( v.Length = 0 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentDataInPDUForSNACK_004() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentDataInPDU =
            [|
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 0xFFFFFFFEu,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF00u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 0xFFFFFFFFu,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF01u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF02u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 2u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF03u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = vSentDataInPDU.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentDataInPDUForSNACK ( itt_me.fromPrim 0u ) ( datasn_me.zero ) 0u
        Assert.True(( v.Length = 4 ))
        Assert.True(( v.[0].InitiatorTaskTag = itt_me.fromPrim 0xFF00u ))
        Assert.True(( v.[1].InitiatorTaskTag = itt_me.fromPrim 0xFF01u ))
        Assert.True(( v.[2].InitiatorTaskTag = itt_me.fromPrim 0xFF02u ))
        Assert.True(( v.[3].InitiatorTaskTag = itt_me.fromPrim 0xFF03u ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentDataInPDUForSNACK_005() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentDataInPDU =
            [|
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF00u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 3u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF01u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 4u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF02u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 5u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF03u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = vSentDataInPDU.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentDataInPDUForSNACK ( itt_me.fromPrim 0u ) ( datasn_me.fromPrim 2u ) 0u
        Assert.True(( v.Length = 3 ))
        Assert.True(( v.[0].InitiatorTaskTag = itt_me.fromPrim 0xFF01u ))
        Assert.True(( v.[1].InitiatorTaskTag = itt_me.fromPrim 0xFF02u ))
        Assert.True(( v.[2].InitiatorTaskTag = itt_me.fromPrim 0xFF03u ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentDataInPDUForSNACK_006() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentDataInPDU =
            [|
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 4u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF00u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 5u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF01u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 6u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF02u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 7u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF03u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = vSentDataInPDU.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentDataInPDUForSNACK ( itt_me.fromPrim 0u ) ( datasn_me.zero ) 3u
        Assert.True(( v.Length = 3 ))
        Assert.True(( v.[0].InitiatorTaskTag = itt_me.fromPrim 0xFF00u ))
        Assert.True(( v.[1].InitiatorTaskTag = itt_me.fromPrim 0xFF01u ))
        Assert.True(( v.[2].InitiatorTaskTag = itt_me.fromPrim 0xFF02u ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentDataInPDUForSNACK_007() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentDataInPDU =
            [|
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 3u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF00u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 4u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF01u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 5u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF02u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 0u,
                    datasn_me.fromPrim 6u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 0xFF03u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = vSentDataInPDU.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentDataInPDUForSNACK ( itt_me.fromPrim 0u ) ( datasn_me.fromPrim 5u ) 3u
        Assert.True(( v.Length = 2 ))
        Assert.True(( v.[0].InitiatorTaskTag = itt_me.fromPrim 0xFF02u ))
        Assert.True(( v.[1].InitiatorTaskTag = itt_me.fromPrim 0xFF03u ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentResponsePDUForSNACK_001() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentRespPDUs = [||]
        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray()
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentResponsePDUForSNACK ( statsn_me.fromPrim 1u ) 3u
        Assert.True(( v.Length = 0 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentResponsePDUForSNACK_002() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentRespPDUs =
            [|
                for i in [| 2u; 1u |] do
                    yield struct(
                        statsn_me.fromPrim i,
                        {
                            Connection_Test.defaultSCSIResponsePDUValues with
                                StatSN = statsn_me.fromPrim i;
                        } :> ILogicalPDU
                    );
            |]
        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray()
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentResponsePDUForSNACK ( statsn_me.zero ) 0u
        Assert.True(( v.Length = 2 ))
        Assert.True(( ( v.[0] :?> SCSIResponsePDU ).StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( ( v.[1] :?> SCSIResponsePDU ).StatSN = statsn_me.fromPrim 2u ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentResponsePDUForSNACK_003() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentRespPDUs =
            [|
                for i in [| 4u; 3u; 2u |] do
                    yield struct(
                        statsn_me.fromPrim i,
                        {
                            Connection_Test.defaultSCSIResponsePDUValues with
                                StatSN = statsn_me.fromPrim i;
                        } :> ILogicalPDU
                    );
            |]
        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray()
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentResponsePDUForSNACK ( statsn_me.fromPrim 1u ) 0u
        Assert.True(( v.Length = 0 ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentResponsePDUForSNACK_004() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentRespPDUs =
            [|
                for i in [| 4u; 3u; 2u |] do
                    yield struct(
                        statsn_me.fromPrim i,
                        {
                            Connection_Test.defaultSCSIResponsePDUValues with
                                StatSN = statsn_me.fromPrim i;
                        } :> ILogicalPDU
                    );
            |]
        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray()
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentResponsePDUForSNACK ( statsn_me.fromPrim 3u ) 0u
        Assert.True(( v.Length = 2 ))
        Assert.True(( ( v.[0] :?> SCSIResponsePDU ).StatSN = statsn_me.fromPrim 3u ))
        Assert.True(( ( v.[1] :?> SCSIResponsePDU ).StatSN = statsn_me.fromPrim 4u ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentResponsePDUForSNACK_005() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentRespPDUs =
            [|
                for i in [| 4u; 3u; 2u |] do
                    yield struct(
                        statsn_me.fromPrim i,
                        {
                            Connection_Test.defaultSCSIResponsePDUValues with
                                StatSN = statsn_me.fromPrim i;
                        } :> ILogicalPDU
                    );
            |]
        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray()
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentResponsePDUForSNACK ( statsn_me.fromPrim 1u ) 2u
        Assert.True(( v.Length = 0 ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentResponsePDUForSNACK_006() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentRespPDUs =
            [|
                for i in [| 4u; 3u; 2u |] do
                    yield struct(
                        statsn_me.fromPrim i,
                        {
                            Connection_Test.defaultSCSIResponsePDUValues with
                                StatSN = statsn_me.fromPrim i;
                        } :> ILogicalPDU
                    );
            |]
        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray()
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentResponsePDUForSNACK ( statsn_me.fromPrim 3u ) 3u
        Assert.True(( v.Length = 0 ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentResponsePDUForSNACK_007() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentRespPDUs =
            [|
                for i in [| 4u; 3u; 2u |] do
                    yield struct(
                        statsn_me.fromPrim i,
                        {
                            Connection_Test.defaultSCSIResponsePDUValues with
                                StatSN = statsn_me.fromPrim i;
                        } :> ILogicalPDU
                    );
            |]
        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray()
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let v = con.GetSentResponsePDUForSNACK ( statsn_me.fromPrim 2u ) 3u
        Assert.True(( v.Length = 3 ))
        Assert.True(( ( v.[0] :?> SCSIResponsePDU ).StatSN = statsn_me.fromPrim 2u ))
        Assert.True(( ( v.[1] :?> SCSIResponsePDU ).StatSN = statsn_me.fromPrim 3u ))
        Assert.True(( ( v.[2] :?> SCSIResponsePDU ).StatSN = statsn_me.fromPrim 4u ))

        GlbFunc.ClosePorts [| sp; cp; |]


    [<Fact>]
    member _.GetSentSCSIResponsePDUForR_SNACK_001() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentDataInPDUs = 
            [|
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 2u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
            |]

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            StatSN = statsn_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct(
                    statsn_me.fromPrim  2u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                            StatSN = statsn_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray()
            m_SentDataInPDUs = vSentDataInPDUs.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let datain, resp = con.GetSentSCSIResponsePDUForR_SNACK ( itt_me.fromPrim 1u )
        Assert.True(( datain.Length = 2 ))
        Assert.True(( datain.[0].InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( datain.[1].InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( resp.InitiatorTaskTag = itt_me.fromPrim 1u ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 1 ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 1 ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentSCSIResponsePDUForR_SNACK_002() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentDataInPDUs = 
            [|
                struct (
                    itt_me.fromPrim 2u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 2u,
                    datasn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 3u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 3u;
                    } :> ILogicalPDU
                );
            |]

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            StatSN = statsn_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct(
                    statsn_me.fromPrim 2u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                            StatSN = statsn_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray()
            m_SentDataInPDUs = vSentDataInPDUs.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let datain, resp = con.GetSentSCSIResponsePDUForR_SNACK ( itt_me.fromPrim 1u )
        Assert.True(( datain.Length = 0 ))
        Assert.True(( resp.InitiatorTaskTag = itt_me.fromPrim 1u ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 1 ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 3 ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentSCSIResponsePDUForR_SNACK_003() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentDataInPDUs = 
            [|
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 2u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
            |]

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.fromPrim 2u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                            StatSN = statsn_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct(
                    statsn_me.fromPrim 3u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 3u;
                            StatSN = statsn_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray()
            m_SentDataInPDUs = vSentDataInPDUs.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        try
            let _, _ = con.GetSentSCSIResponsePDUForR_SNACK ( itt_me.fromPrim 1u )
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as _ ->
            ()

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 2 ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 1 ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentSCSIResponsePDUForR_SNACK_004() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            StatSN = statsn_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct(
                    statsn_me.fromPrim 2u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                            StatSN = statsn_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray()
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let datain, resp = con.GetSentSCSIResponsePDUForR_SNACK ( itt_me.fromPrim 1u )
        Assert.True(( datain.Length = 0 ))
        Assert.True(( resp.InitiatorTaskTag = itt_me.fromPrim 1u ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 1 ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentSCSIResponsePDUForR_SNACK_005() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentDataInPDUs = 
            [|
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 2u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = vSentDataInPDUs.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        try
            let _, _ = con.GetSentSCSIResponsePDUForR_SNACK ( itt_me.fromPrim 1u )
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as _ ->
            ()

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 1 ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentSCSIResponsePDUForR_SNACK_006() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentDataInPDUs = 
            [|
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultR2TPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 2u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim  2u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
            |]

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            StatSN = statsn_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray();
            m_SentDataInPDUs = vSentDataInPDUs.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let datain, resp = con.GetSentSCSIResponsePDUForR_SNACK ( itt_me.fromPrim 1u )
        Assert.True(( datain.Length = 2 ))
        Assert.True(( datain.[0].InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( datain.[1].InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( resp.InitiatorTaskTag = itt_me.fromPrim 1u ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 2 ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.GetSentSCSIResponsePDUForR_SNACK_007() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentDataInPDUs = 
            [|
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultR2TPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultR2TPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.fromPrim 2u,
                    {
                        Connection_Test.defaultR2TPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 2u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
            |]

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            StatSN = statsn_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray();
            m_SentDataInPDUs = vSentDataInPDUs.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let datain, resp = con.GetSentSCSIResponsePDUForR_SNACK ( itt_me.fromPrim 1u )
        Assert.True(( datain.Length = 0 ))
        Assert.True(( resp.InitiatorTaskTag = itt_me.fromPrim 1u ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 4 ))

        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.R_SNACKRequest_001() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )
        let mutable cnt = 0

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            StatSN = statsn_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray();
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        con.R_SNACKRequest ( itt_me.fromPrim 1u ) ( fun () -> cnt <- cnt + 1 )

        let m_R_SNACK_Request = Connection_Test.getR_SNACK_Request  con
        Assert.True(( m_R_SNACK_Request.Length = 0 ))
        Assert.True(( cnt = 1 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.R_SNACKRequest_002() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )
        let mutable cnt = 0

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            StatSN = statsn_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray();
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        con.R_SNACKRequest ( itt_me.fromPrim 2u ) ( fun () -> cnt <- cnt + 1 )

        let m_R_SNACK_Request = Connection_Test.getR_SNACK_Request  con
        Assert.True(( m_R_SNACK_Request.Length = 1 ))
        Assert.True(( cnt = 0 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.R_SNACKRequest_003() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )
        let mutable cnt = 0
        let mutable cnt2 = 0

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            StatSN = statsn_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
            |]

        let vR_SNACK_Request = 
            [|
                struct(
                    itt_me.fromPrim 1u,
                    ( fun () -> cnt2 <- cnt2 + 1 )
                )
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray();
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = vR_SNACK_Request.ToImmutableArray();
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        con.R_SNACKRequest ( itt_me.fromPrim 1u ) ( fun () -> cnt <- cnt + 1 )

        let m_R_SNACK_Request = Connection_Test.getR_SNACK_Request  con
        Assert.True(( m_R_SNACK_Request.Length = 0 ))
        Assert.True(( cnt = 1 ))
        Assert.True(( cnt2 = 0 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.R_SNACKRequest_004() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )
        let mutable cnt = 0
        let mutable cnt2 = 0

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.fromPrim 2u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 2u;
                            StatSN = statsn_me.fromPrim 2u;
                    } :> ILogicalPDU
                );
            |]

        let vR_SNACK_Request = 
            [|
                struct(
                    itt_me.fromPrim 1u,
                    ( fun () -> cnt2 <- cnt2 + 1 )
                )
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray();
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = vR_SNACK_Request.ToImmutableArray();
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        con.R_SNACKRequest ( itt_me.fromPrim 1u ) ( fun () -> cnt <- cnt + 1 )

        let m_R_SNACK_Request = Connection_Test.getR_SNACK_Request  con
        Assert.True(( m_R_SNACK_Request.Length = 1 ))
        Assert.True(( cnt = 0 ))
        Assert.True(( cnt2 = 0 ))

        let struct( witt, wfunc ) = m_R_SNACK_Request.[0]
        Assert.True(( witt = itt_me.fromPrim 1u ))
        wfunc()

        Assert.True(( cnt = 1 ))
        Assert.True(( cnt2 = 0 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendPDUInternal_001() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        sessStub.p_UpdateMaxCmdSN <- ( fun () -> ( cmdsn_me.fromPrim 1u, cmdsn_me.fromPrim 2u ) )
        sessStub.p_RemoveConnection <- ( fun _ _ -> Assert.Fail __LINE__ )
        let pc = PrivateCaller( con )

        let vSentDataInPDUs = 
            [|
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            Status = ScsiCmdStatCd.ACA_ACTIVE;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = vSentDataInPDUs.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let result =
            pc.Invoke( "SendPDUInternal", {
                    Connection_Test.defaultSCSIDataInPDUValues with
                        InitiatorTaskTag = itt_me.fromPrim 1u;
                        Status = ScsiCmdStatCd.GOOD;
                } ) :?> Task< ( unit -> unit ) voption >
            |> Functions.RunTaskSynchronously

        Assert.True(( result.IsNone ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 1 ))
        let struct( a, b, c ) = m_SentDataInPDUs.[0]
        Assert.True(( a = itt_me.fromPrim 1u ))
        Assert.True(( b = datasn_me.zero ))
        Assert.True(( ( c :?> SCSIDataInPDU ).Status = ScsiCmdStatCd.GOOD ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True(( rpdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
        let datainPDU = rpdu :?> SCSIDataInPDU
        Assert.True(( datainPDU.InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( datainPDU.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( datainPDU.MaxCmdSN = cmdsn_me.fromPrim 2u ))
        Assert.True(( datainPDU.Status = ScsiCmdStatCd.GOOD ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendPDUInternal_002() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        sessStub.p_UpdateMaxCmdSN <- ( fun () -> ( cmdsn_me.fromPrim 1u, cmdsn_me.fromPrim 2u ) )
        sessStub.p_RemoveConnection <- ( fun _ _ -> Assert.Fail __LINE__ )
        let pc = PrivateCaller( con )

        let result =
            pc.Invoke( "SendPDUInternal", {
                    Connection_Test.defaultSCSIDataInPDUValues with
                        InitiatorTaskTag = itt_me.fromPrim 1u;
                        Status = ScsiCmdStatCd.GOOD;
                } ) :?> Task< ( unit -> unit ) voption >
            |> Functions.RunTaskSynchronously

        Assert.True(( result.IsNone ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 1 ))
        let struct( a, b, c ) = m_SentDataInPDUs.[0]
        Assert.True(( a = itt_me.fromPrim 1u ))
        Assert.True(( b = datasn_me.zero ))
        Assert.True(( ( c :?> SCSIDataInPDU ).Status = ScsiCmdStatCd.GOOD ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True(( rpdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
        let datainPDU = rpdu :?> SCSIDataInPDU
        Assert.True(( datainPDU.InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( datainPDU.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( datainPDU.MaxCmdSN = cmdsn_me.fromPrim 2u ))
        Assert.True(( datainPDU.Status = ScsiCmdStatCd.GOOD ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendPDUInternal_003() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        sessStub.p_UpdateMaxCmdSN <- ( fun () -> ( cmdsn_me.fromPrim 1u, cmdsn_me.fromPrim 2u ) )
        sessStub.p_RemoveConnection <- ( fun _ _ -> Assert.Fail __LINE__ )
        let pc = PrivateCaller( con )

        let vR_SNACK_Request =
            [|
                struct(
                    itt_me.fromPrim 1u,
                    ( fun () -> Assert.Fail __LINE__ )
                )
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = vR_SNACK_Request.ToImmutableArray();
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        GlbFunc.ClosePorts [| sp; cp; |]

        let result =
            pc.Invoke( "SendPDUInternal", {
                    Connection_Test.defaultSCSIDataInPDUValues with
                        InitiatorTaskTag = itt_me.fromPrim 1u;
                        Status = ScsiCmdStatCd.GOOD;
                } ) :?> Task< ( unit -> unit ) voption >
            |> Functions.RunTaskSynchronously

        Assert.True(( result.IsNone ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 1 ))
        let struct( a, b, c ) = m_SentDataInPDUs.[0]
        Assert.True(( a = itt_me.fromPrim 1u ))
        Assert.True(( b = datasn_me.zero ))
        Assert.True(( ( c :?> SCSIDataInPDU ).Status = ScsiCmdStatCd.GOOD ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))

        let m_R_SNACK_Request = Connection_Test.getR_SNACK_Request con
        Assert.True(( m_R_SNACK_Request.Length = 1 ))

    [<Fact>]
    member _.SendPDUInternal_004() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        sessStub.p_UpdateMaxCmdSN <- ( fun () -> ( cmdsn_me.fromPrim 1u, cmdsn_me.fromPrim 2u ) )
        sessStub.p_RemoveConnection <- ( fun _ _ -> Assert.Fail __LINE__ )
        let pc = PrivateCaller( con )

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.zero,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            Status = ScsiCmdStatCd.ACA_ACTIVE;
                            StatSN = statsn_me.zero;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray();
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let result =
            pc.Invoke( "SendPDUInternal", {
                Connection_Test.defaultSCSIResponsePDUValues with
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    Status = ScsiCmdStatCd.GOOD;
                    StatSN = statsn_me.zero;
            } ) :?> Task< ( unit -> unit ) voption >
            |> Functions.RunTaskSynchronously

        Assert.True(( result.IsNone ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 1 ))
        let struct( rStatSN, rPDU ) = m_SentRespPDUs.[0]
        Assert.True(( rStatSN = statsn_me.zero ))
        Assert.True(( ( rPDU :?> SCSIResponsePDU ).Status = ScsiCmdStatCd.GOOD ))

        Assert.True(( ( pc.GetField( "m_StatSN" ) :?> uint ) = 1u ))

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True(( rpdu.Opcode = OpcodeCd.SCSI_RES ))
        let respPDU = rpdu :?> SCSIResponsePDU
        Assert.True(( respPDU.InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( respPDU.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( respPDU.MaxCmdSN = cmdsn_me.fromPrim 2u ))
        Assert.True(( respPDU.Status = ScsiCmdStatCd.GOOD ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendPDUInternal_005() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        sessStub.p_UpdateMaxCmdSN <- ( fun () -> ( cmdsn_me.fromPrim 1u, cmdsn_me.fromPrim 2u ) )
        sessStub.p_RemoveConnection <- ( fun _ _ -> Assert.Fail __LINE__ )
        let pc = PrivateCaller( con )

        let result =
            pc.Invoke( "SendPDUInternal", {
                Connection_Test.defaultSCSIResponsePDUValues with
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    Status = ScsiCmdStatCd.GOOD;
                    StatSN = statsn_me.zero;
            } ) :?> Task< ( unit -> unit ) voption >
            |> Functions.RunTaskSynchronously

        Assert.True(( result.IsNone ))
        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 1 ))
        let struct( rStatSN, rPDU ) = m_SentRespPDUs.[0]
        Assert.True(( rStatSN = statsn_me.zero ))
        Assert.True(( ( rPDU :?> SCSIResponsePDU ).Status = ScsiCmdStatCd.GOOD ))
        Assert.True(( ( pc.GetField( "m_StatSN" ) :?> uint ) = 1u ))

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously


        Assert.True(( rpdu.Opcode = OpcodeCd.SCSI_RES ))
        let respPDU = rpdu :?> SCSIResponsePDU
        Assert.True(( respPDU.InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( respPDU.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( respPDU.MaxCmdSN = cmdsn_me.fromPrim 2u ))
        Assert.True(( respPDU.Status = ScsiCmdStatCd.GOOD ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendPDUInternal_006() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        sessStub.p_UpdateMaxCmdSN <- ( fun () -> ( cmdsn_me.fromPrim 1u, cmdsn_me.fromPrim 2u ) )
        sessStub.p_RemoveConnection <- ( fun _ _ -> Assert.Fail __LINE__ )
        let pc = PrivateCaller( con )
        let mutable cnt = 0
        
        let vR_SNACK_Request = 
            [|
                struct(
                    ( itt_me.fromPrim 1u ),
                    ( fun () -> cnt <- cnt + 1 )
                )
            |]
        
        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = vR_SNACK_Request.ToImmutableArray();
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        GlbFunc.ClosePorts [| sp; cp; |]

        let result =
            pc.Invoke( "SendPDUInternal", {
                Connection_Test.defaultSCSIResponsePDUValues with
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    Status = ScsiCmdStatCd.GOOD;
                    StatSN = statsn_me.zero;
            } ) :?> Task< ( unit -> unit ) voption >
            |> Functions.RunTaskSynchronously

        Assert.True(( result.IsSome ))

        let m_R_SNACK_Request = Connection_Test.getR_SNACK_Request  con
        Assert.True(( m_R_SNACK_Request.Length = 0 ))

        Assert.True(( cnt = 0 ))
        result.Value()
        Assert.True(( cnt = 1 ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 1 ))
        let struct( rStatSN, rPDU ) = m_SentRespPDUs.[0]
        Assert.True(( rStatSN = statsn_me.zero ))
        Assert.True(( ( rPDU :?> SCSIResponsePDU ).Status = ScsiCmdStatCd.GOOD ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))

    [<Fact>]
    member _.SendPDUInternal_007() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        sessStub.p_UpdateMaxCmdSN <- ( fun () -> ( cmdsn_me.fromPrim 1u, cmdsn_me.fromPrim 2u ) )
        sessStub.p_RemoveConnection <- ( fun _ _ -> Assert.Fail __LINE__ )
        let pc = PrivateCaller( con )

        let vSentDataInPDUs = 
            [|
                struct (
                    itt_me.fromPrim 1u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultR2TPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 1u;
                            R2TSN = datasn_me.zero;
                            BufferOffset = 0xFFu;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = vSentDataInPDUs.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let result =
            pc.Invoke( "SendPDUInternal", {
                Connection_Test.defaultR2TPDUValues with
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    R2TSN = datasn_me.zero;
                    BufferOffset = 0xAAu;
            } ) :?> Task< ( unit -> unit ) voption >
            |> Functions.RunTaskSynchronously

        Assert.True(( result.IsNone ))
        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 1 ))
        let struct( a, b, c ) = m_SentDataInPDUs.[0]
        Assert.True(( a = itt_me.fromPrim 1u ))
        Assert.True(( b = datasn_me.zero ))
        Assert.True(( ( c :?> R2TPDU ).BufferOffset = 0xAAu ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously


        Assert.True(( rpdu.Opcode = OpcodeCd.R2T ))
        let r2tPDU = rpdu :?> R2TPDU
        Assert.True(( r2tPDU.InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( r2tPDU.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( r2tPDU.MaxCmdSN = cmdsn_me.fromPrim 2u ))
        Assert.True(( r2tPDU.BufferOffset = 0xAAu ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendPDUInternal_008() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        sessStub.p_UpdateMaxCmdSN <- ( fun () -> ( cmdsn_me.fromPrim 1u, cmdsn_me.fromPrim 2u ) )
        sessStub.p_RemoveConnection <- ( fun _ _ -> Assert.Fail __LINE__ )
        let pc = PrivateCaller( con )

        let result =
            pc.Invoke( "SendPDUInternal", {
                Connection_Test.defaultR2TPDUValues with
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    R2TSN = datasn_me.zero;
                    BufferOffset = 0xAAu;
            } ) :?> Task< ( unit -> unit ) voption >
            |> Functions.RunTaskSynchronously

        Assert.True(( result.IsNone ))
        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 1 ))
        let struct( a, b, c ) = m_SentDataInPDUs.[0]
        Assert.True(( a = itt_me.fromPrim 1u ))
        Assert.True(( b = datasn_me.zero ))
        Assert.True(( ( c :?> R2TPDU ).BufferOffset = 0xAAu ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously


        Assert.True(( rpdu.Opcode = OpcodeCd.R2T ))
        let r2tPDU = rpdu :?> R2TPDU
        Assert.True(( r2tPDU.InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( r2tPDU.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( r2tPDU.MaxCmdSN = cmdsn_me.fromPrim 2u ))
        Assert.True(( r2tPDU.BufferOffset = 0xAAu ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendPDUInternal_009() =
        let mutable cnt = 0
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        sessStub.p_UpdateMaxCmdSN <- ( fun () -> ( cmdsn_me.fromPrim 1u, cmdsn_me.fromPrim 2u ) )
        sessStub.p_RemoveConnection <- ( fun cid concnt ->
            cnt <- cnt + 1
            Assert.True(( cid = cid_me.fromPrim 0us ))
        )
        let pc = PrivateCaller( con )

        let result =
            pc.Invoke( "SendPDUInternal", {
                Response = LogoutResCd.SUCCESS;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                Time2Wait = 0us;
                Time2Retain = 0us;
                CloseAllegiantConnection = true;
            } ) :?> Task< ( unit -> unit ) voption >
            |> Functions.RunTaskSynchronously

        Assert.True(( result.IsNone ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 1 ))
        let struct( rStatSN, rPDU ) = m_SentRespPDUs.[0]
        Assert.True(( rStatSN = statsn_me.zero ))
        Assert.True(( ( rPDU :?> LogoutResponsePDU ).Response = LogoutResCd.SUCCESS ))

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True(( rpdu.Opcode = OpcodeCd.LOGOUT_RES ))
        let logoutPDU = rpdu :?> LogoutResponsePDU
        Assert.True(( logoutPDU.Response = LogoutResCd.SUCCESS ))
        Assert.True(( logoutPDU.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( logoutPDU.MaxCmdSN = cmdsn_me.fromPrim 2u ))
        Assert.True(( cnt = 1 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendPDUInternal_010() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        sessStub.p_UpdateMaxCmdSN <- ( fun () -> ( cmdsn_me.fromPrim 1u, cmdsn_me.fromPrim 2u ) )
        let pc = PrivateCaller( con )

        let result =
            pc.Invoke( "SendPDUInternal", {
                Response = LogoutResCd.SUCCESS;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                Time2Wait = 0us;
                Time2Retain = 0us;
                CloseAllegiantConnection = false;
            } ) :?> Task< ( unit -> unit ) voption >
            |> Functions.RunTaskSynchronously

        Assert.True(( result.IsNone ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 1 ))
        let struct( rStatSN, rPDU ) = m_SentRespPDUs.[0]
        Assert.True(( rStatSN = statsn_me.zero ))
        Assert.True(( ( rPDU :?> LogoutResponsePDU ).Response = LogoutResCd.SUCCESS ))

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True(( rpdu.Opcode = OpcodeCd.LOGOUT_RES ))
        let logoutPDU = rpdu :?> LogoutResponsePDU
        Assert.True(( logoutPDU.Response = LogoutResCd.SUCCESS ))
        Assert.True(( logoutPDU.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( logoutPDU.MaxCmdSN = cmdsn_me.fromPrim 2u ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendPDUInternal_011() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        sessStub.p_UpdateMaxCmdSN <- ( fun () -> ( cmdsn_me.fromPrim 1u, cmdsn_me.fromPrim 2u ) )
        sessStub.p_RemoveConnection <- ( fun _ _ -> Assert.Fail __LINE__ )
        let pc = PrivateCaller( con )

        let result =
            pc.Invoke( "SendPDUInternal", {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu;
                TargetTransferTag = ttt_me.fromPrim 1u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                PingData = PooledBuffer.Empty;
            } ) :?> Task< ( unit -> unit ) voption >
            |> Functions.RunTaskSynchronously

        Assert.True(( result.IsNone ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))

        let rpdu =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously


        Assert.True(( rpdu.Opcode = OpcodeCd.NOP_IN ))
        let nopinPDU = rpdu :?> NOPInPDU
        Assert.True(( nopinPDU.InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu ))
        Assert.True(( nopinPDU.TargetTransferTag = ttt_me.fromPrim 1u ))
        Assert.True(( nopinPDU.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( nopinPDU.MaxCmdSN = cmdsn_me.fromPrim 2u ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ReceivePDUInFullFeaturePhase_001() =
        let _, _, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        killer.NoticeTerminate()
        GlbFunc.ClosePorts [| sp; cp; |]

        pc.Invoke( "ReceivePDUInFullFeaturePhase" ) :?> Task< unit >
        |> Functions.RunTaskSynchronously

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))

    [<Fact>]
    member _.ReceivePDUInFullFeaturePhase_002() =
        let _, sessStub, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                {
                    Connection_Test.defaultSessionParameter with
                        ErrorRecoveryLevel = 0uy;
                },
                statsn_me.fromPrim ( Constants.MAX_STATSN_DIFF + 1u )
            )
        let pc = PrivateCaller( con )

        let mutable cnt = 0
        sessStub.p_DestroySession <- ( fun () ->
            cnt <- cnt + 1
            killer.NoticeTerminate()
        )
        sessStub.p_PushReceivedPDU <- ( fun _ _ ->
            Assert.Fail __LINE__
        )

        PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, {
            Connection_Test.defaultNopOUTPDUValues with
                ExpStatSN = statsn_me.zero;
        } )
        |> Functions.RunTaskSynchronously
        |> ignore

        pc.Invoke( "ReceivePDUInFullFeaturePhase" ) :?> Task< unit >
        |> Functions.RunTaskSynchronously

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))
        Assert.True(( cnt = 1 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ReceivePDUInFullFeaturePhase_003() =
        let _, sessStub, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                {
                    Connection_Test.defaultSessionParameter with
                        ErrorRecoveryLevel = 0uy;
                },
                statsn_me.fromPrim ( Constants.MAX_STATSN_DIFF + 0u )
            )
        let pc = PrivateCaller( con )

        let mutable cnt = 0
        sessStub.p_DestroySession <- ( fun () ->
            Assert.Fail __LINE__
        )
        sessStub.p_PushReceivedPDU <- ( fun _ pdu ->
            cnt <- cnt + 1
            Assert.True(( pdu.Opcode = OpcodeCd.NOP_OUT ))
            let nopoutPDU = pdu :?> NOPOutPDU
            Assert.True(( nopoutPDU.LUN = lun_me.fromPrim 0x00000000000000EFUL ))
            cp.Close()
            cp.Dispose()
            killer.NoticeTerminate()
        )

        PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, {
            Connection_Test.defaultNopOUTPDUValues with
                LUN = lun_me.fromPrim 0x00000000000000EFUL
                ExpStatSN = statsn_me.zero;
        } )
        |> Functions.RunTaskSynchronously
        |> ignore

        try
            pc.Invoke( "ReceivePDUInFullFeaturePhase" ) :?> Task< unit >
            |> Functions.RunTaskSynchronously
        with
        | :? ConnectionErrorException ->
            ()

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))
        Assert.True(( cnt = 1 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ReceivePDUInFullFeaturePhase_004() =
        let _, sessStub, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                {
                    Connection_Test.defaultSessionParameter with
                        ErrorRecoveryLevel = 0uy;
                },
                statsn_me.fromPrim ( 0u - Constants.MAX_STATSN_DIFF - 1u )
            )
        let pc = PrivateCaller( con )

        let mutable cnt = 0
        sessStub.p_DestroySession <- ( fun () ->
            cnt <- cnt + 1
            killer.NoticeTerminate()
        )
        sessStub.p_PushReceivedPDU <- ( fun _ _ ->
            Assert.Fail __LINE__
        )

        PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, {
            Connection_Test.defaultNopOUTPDUValues with
                ExpStatSN = statsn_me.zero;
        } )
        |> Functions.RunTaskSynchronously
        |> ignore

        pc.Invoke( "ReceivePDUInFullFeaturePhase" ) :?> Task< unit >
        |> Functions.RunTaskSynchronously

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))
        Assert.True(( cnt = 1 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ReceivePDUInFullFeaturePhase_005() =
        let _, sessStub, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                {
                    Connection_Test.defaultSessionParameter with
                        ErrorRecoveryLevel = 0uy;
                },
                statsn_me.fromPrim ( 0u - Constants.MAX_STATSN_DIFF - 0u )
            )
        let pc = PrivateCaller( con )

        let mutable cnt = 0
        sessStub.p_DestroySession <- ( fun () ->
            Assert.Fail __LINE__
        )
        sessStub.p_PushReceivedPDU <- ( fun _ pdu ->
            cnt <- cnt + 1
            Assert.True(( pdu.Opcode = OpcodeCd.NOP_OUT ))
            let nopoutPDU = pdu :?> NOPOutPDU
            Assert.True(( nopoutPDU.LUN = lun_me.fromPrim 0x00000000000000EFUL ))
            cp.Close()
            cp.Dispose()
            killer.NoticeTerminate()
        )

        PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, {
            Connection_Test.defaultNopOUTPDUValues with
                LUN = lun_me.fromPrim 0x00000000000000EFUL
                ExpStatSN = statsn_me.zero;
        } )
        |> Functions.RunTaskSynchronously
        |> ignore

        try
            pc.Invoke( "ReceivePDUInFullFeaturePhase" ) :?> Task< unit >
            |> Functions.RunTaskSynchronously
        with
        | :? ConnectionErrorException ->
            ()

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))
        Assert.True(( cnt = 1 ))
        GlbFunc.ClosePorts [| sp; cp; |]


    [<Fact>]
    member _.ReceivePDUInFullFeaturePhase_006() =
        let _, sessStub, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                {
                    Connection_Test.defaultSessionParameter with
                        ErrorRecoveryLevel = 1uy;
                },
                statsn_me.fromPrim ( Constants.MAX_STATSN_DIFF + 1u )
            )
        let pc = PrivateCaller( con )

        let mutable cnt = 0
        sessStub.p_DestroySession <- ( fun () ->
            Assert.Fail __LINE__
        )
        sessStub.p_PushReceivedPDU <- ( fun _ pdu ->
            cnt <- cnt + 1
            Assert.True(( pdu.Opcode = OpcodeCd.NOP_OUT ))
            let nopoutPDU = pdu :?> NOPOutPDU
            Assert.True(( nopoutPDU.LUN = lun_me.fromPrim 0x00000000000000EFUL ))
            cp.Close()
            cp.Dispose()
            killer.NoticeTerminate()
        )

        PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, {
            Connection_Test.defaultNopOUTPDUValues with
                LUN = lun_me.fromPrim 0x00000000000000EFUL
                ExpStatSN = statsn_me.zero;
        } )
        |> Functions.RunTaskSynchronously
        |> ignore

        try
            pc.Invoke( "ReceivePDUInFullFeaturePhase" ) :?> Task< unit >
            |> Functions.RunTaskSynchronously
        with
        | :? ConnectionErrorException ->
            ()

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))
        Assert.True(( cnt = 1 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ReceivePDUInFullFeaturePhase_007() =
        let _, sessStub, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                {
                    Connection_Test.defaultSessionParameter with
                        ErrorRecoveryLevel = 0uy;
                },
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let mutable cnt = 0
        sessStub.p_DestroySession <- ( fun () ->
            Assert.Fail __LINE__
        )
        sessStub.p_PushReceivedPDU <- ( fun _ pdu ->
            cnt <- cnt + 1
            if cnt = 1 then
                Assert.True(( pdu.Opcode = OpcodeCd.NOP_OUT ))
                let nopoutPDU = pdu :?> NOPOutPDU
                Assert.True(( nopoutPDU.LUN = lun_me.fromPrim 0x00000000000000EFUL ))
            elif cnt = 2 then
                Assert.True(( pdu.Opcode = OpcodeCd.NOP_OUT ))
                let nopoutPDU = pdu :?> NOPOutPDU
                Assert.True(( nopoutPDU.LUN = lun_me.fromPrim 0x00000000000000FFUL ))
                cp.Close()
                cp.Dispose()
                killer.NoticeTerminate()
            else
                Assert.Fail __LINE__
        )

        PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, {
            Connection_Test.defaultNopOUTPDUValues with
                LUN = lun_me.fromPrim 0x00000000000000EFUL
                ExpStatSN = statsn_me.zero;
        } )
        |> Functions.RunTaskSynchronously
        |> ignore

        PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, {
            Connection_Test.defaultNopOUTPDUValues with
                LUN = lun_me.fromPrim 0x00000000000000FFUL
                ExpStatSN = statsn_me.zero;
        } )
        |> Functions.RunTaskSynchronously
        |> ignore

        try
            pc.Invoke( "ReceivePDUInFullFeaturePhase" ) :?> Task< unit >
            |> Functions.RunTaskSynchronously
        with
        | :? ConnectionErrorException ->
            ()

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))
        Assert.True(( cnt = 2 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ReceivePDUInFullFeaturePhase_008() =
        let _, sessStub, killer, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                {
                    Connection_Test.defaultSessionParameter with
                        ErrorRecoveryLevel = 1uy;
                },
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let mutable cnt = 0
        sessStub.p_DestroySession <- ( fun () ->
            Assert.Fail __LINE__
        )
        sessStub.p_PushReceivedPDU <- ( fun _ _ ->
            Assert.Fail __LINE__
        )
        sessStub.p_RejectPDUByHeader <- ( fun cid counter _ _ ->
            cnt <- cnt + 1
            Assert.True(( cid = cid_me.fromPrim 0us ))
            Assert.True(( counter = concnt_me.fromPrim 0 ))
            cp.Close()
            cp.Dispose()
            killer.NoticeTerminate()
        )

        PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, Connection_Test.defaultNopInPDUValues )
        |> Functions.RunTaskSynchronously
        |> ignore

        try
            pc.Invoke( "ReceivePDUInFullFeaturePhase" ) :?> Task< unit >
            |> Functions.RunTaskSynchronously
        with
        | :? ConnectionErrorException ->
            ()

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))
        Assert.True(( cnt = 1 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ReceivePDUInFullFeaturePhase_009() =
        let _, _, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                {
                    Connection_Test.defaultSessionParameter with
                        ErrorRecoveryLevel = 1uy;
                },
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        GlbFunc.ClosePorts [| sp; cp; |]

        try
            pc.Invoke( "ReceivePDUInFullFeaturePhase" ) :?> Task< unit >
            |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? ConnectionErrorException ->
            ()

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))

    [<Fact>]
    member _.DeleteAcknowledgedPDU_001() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                {
                    Connection_Test.defaultSessionParameter with
                        ErrorRecoveryLevel = 1uy;
                },
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.zero,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            StatSN = statsn_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 10u;
                            ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                    } :> ILogicalPDU
                );
                struct(
                    statsn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultNopInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 11u; // NeedResponseFence = R_Mode
                            StatSN = statsn_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct(
                    statsn_me.fromPrim 2u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 12u;
                            StatSN = statsn_me.fromPrim 2u;
                            ResponseFence = ResponseFenceNeedsFlag.R_Mode;
                    } :> ILogicalPDU
                );
                struct(
                    statsn_me.fromPrim 3u,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 13u;
                            StatSN = statsn_me.fromPrim 3u;
                            ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                    } :> ILogicalPDU
                );
            |]

        let vSentDataInPDUs = 
            [|
                struct (
                    itt_me.fromPrim 10u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 10u;
                            DataSN = datasn_me.zero;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 10u,
                    datasn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 10u;
                            DataSN = datasn_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 13u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 13u;
                            DataSN = datasn_me.zero;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray();
            m_SentDataInPDUs = vSentDataInPDUs.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let mutable cnt = 0
        sessStub.p_NoticeUnlockResponseFence <- ( fun flg ->
            cnt <- cnt + 1
            if cnt = 1 then
                Assert.True(( flg = ResponseFenceNeedsFlag.W_Mode ))
            elif cnt = 2 then
                Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            elif cnt = 3 then
                Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
        )

        pc.Invoke( "DeleteAcknowledgedPDU", 3u ) |> ignore

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 1 ))
        let struct( rStatSN, rPDU ) = m_SentRespPDUs.[0]
        Assert.True(( rStatSN = statsn_me.fromPrim 3u ))
        Assert.True(( ( rPDU :?> SCSIResponsePDU ).InitiatorTaskTag = itt_me.fromPrim 13u ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 1 ))
        let struct( a, b, c ) = m_SentDataInPDUs.[0]
        Assert.True(( a = itt_me.fromPrim 13u ))
        Assert.True(( b = datasn_me.zero ))
        Assert.True(( ( c :?> SCSIDataInPDU ).InitiatorTaskTag = itt_me.fromPrim 13u ))
        Assert.True(( cnt = 3 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.DeleteAcknowledgedPDU_002() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                {
                    Connection_Test.defaultSessionParameter with
                        ErrorRecoveryLevel = 1uy;
                },
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentDataInPDUs = 
            [|
                struct (
                    itt_me.fromPrim 10u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 10u;
                            DataSN = datasn_me.zero;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 10u,
                    datasn_me.fromPrim 1u,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 10u;
                            DataSN = datasn_me.fromPrim 1u;
                    } :> ILogicalPDU
                );
                struct (
                    itt_me.fromPrim 13u,
                    datasn_me.zero,
                    {
                        Connection_Test.defaultSCSIDataInPDUValues with
                            InitiatorTaskTag = itt_me.fromPrim 13u;
                            DataSN = datasn_me.zero;
                    } :> ILogicalPDU
                );
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = ImmutableArray< struct( STATSN_T * ILogicalPDU ) >.Empty;
            m_SentDataInPDUs = vSentDataInPDUs.ToImmutableArray();
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        sessStub.p_NoticeUnlockResponseFence <- ( fun _ ->
            Assert.Fail __LINE__
        )

        pc.Invoke( "DeleteAcknowledgedPDU", 3u ) |> ignore

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))

        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 3 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.DeleteAcknowledgedPDU_003() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                {
                    Connection_Test.defaultSessionParameter with
                        ErrorRecoveryLevel = 1uy;
                },
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.zero,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            StatSN = statsn_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 10u;
                            ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                    } :> ILogicalPDU
                )
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray();
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        let mutable cnt = 0
        sessStub.p_NoticeUnlockResponseFence <- ( fun flg ->
            cnt <- cnt + 1
            Assert.True(( flg = ResponseFenceNeedsFlag.W_Mode ))
        )

        pc.Invoke( "DeleteAcknowledgedPDU", 3u ) |> ignore

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))
        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        Assert.True(( cnt = 1 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.DeleteAcknowledgedPDU_004() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                {
                    Connection_Test.defaultSessionParameter with
                        ErrorRecoveryLevel = 1uy;
                },
                statsn_me.zero
            )
        let pc = PrivateCaller( con )

        let vSentRespPDUs =
            [|
                struct(
                    statsn_me.zero,
                    {
                        Connection_Test.defaultSCSIResponsePDUValues with
                            StatSN = statsn_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 10u;
                            ResponseFence = ResponseFenceNeedsFlag.Immediately;
                    } :> ILogicalPDU
                )
            |]

        let m_ResendStat = OptimisticLock< ResendStatusRec >({
            m_SentRespPDUs = vSentRespPDUs.ToImmutableArray();
            m_SentDataInPDUs = ImmutableArray< struct ( ITT_T * DATASN_T * ILogicalPDU ) >.Empty;
            m_R_SNACK_Request = ImmutableArray< struct( ITT_T * ( unit -> unit ) ) >.Empty;
        })
        pc.SetField( "m_ResendStat", m_ResendStat )

        sessStub.p_NoticeUnlockResponseFence <- ( fun _ ->
            Assert.Fail __LINE__
        )

        pc.Invoke( "DeleteAcknowledgedPDU", 3u ) |> ignore

        let m_SentRespPDUs = Connection_Test.getSentRespPDUs con
        Assert.True(( m_SentRespPDUs.Length = 0 ))
        let m_SentDataInPDUs = Connection_Test.getSentDataInPDUs con
        Assert.True(( m_SentDataInPDUs.Length = 0 ))
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.Close_001() =
        let _, sessStub, _, sp, cp, con =
            Connection_Test.createDefaultConnectionObj(
                Connection_Test.defaultConnectionParam,
                Connection_Test.defaultSessionParameter,
                statsn_me.zero
            )

        con.Close()

        try
            sp.ReadByte() |> ignore
            Assert.Fail __LINE__
        with
        | :? ObjectDisposedException ->
            ()

        GlbFunc.ClosePorts [| sp; cp; |]
