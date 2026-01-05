//=============================================================================
// Haruka Software Storage.
// SessionTest.fs : Test cases for Session class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.TargetDevice

//=============================================================================
// Import declaration

open System
open System.IO
open System.Text
open System.Threading
open System.Threading.Tasks

open Xunit
open Xunit.Abstractions

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.IODataTypes
open Haruka.Test
open System.Collections.Immutable

//=============================================================================
// Class implementation

type Session_Test ( m_TestLogWriter : ITestOutputHelper ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    static member defaultSessionParam = {
            MaxConnections = 3us;
            TargetGroupID = tgid_me.Zero;
            TargetConf = {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetName = "abcT";
                TargetAlias = "";
                LUN = [ lun_me.zero ];
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

    static member GenDefaultStubs() =
        let smStub = new CStatus_Stub()
        let luStub = new CLU_Stub()
        smStub.p_RemoveSession <- ignore
        smStub.p_GetActiveTarget <-
            ( fun _ ->
                [ { 
                    IdentNumber = tnodeidx_me.fromPrim 0u;
                    TargetName = "abcT";
                    TargetAlias = "abcTA";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.zero ];
                    Auth = TargetGroupConf.U_None();
                } ]
            )
        smStub.p_GetLU <-
            ( fun _ ->
                ValueSome ( luStub :> ILU )
            )
        smStub, luStub

    static member CreateDefaultSessionObject
        ( sessParam : IscsiNegoParamSW )
        ( conParam : IscsiNegoParamCO )
        ( cid : CID_T )
        ( firstCmdSN : CMDSN_T )
        ( nopOutITT : ITT_T ) 
        ( nopOutImm : bool ) =

        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let killer = new HKiller()
        let smStub, luStub = Session_Test.GenDefaultStubs()
        let sp, cp = GlbFunc.GetNetConn()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, firstCmdSN, killer ) :> ISession
        let pc = new PrivateCaller( sess )

        // Add test connection to session object in the target
        Assert.True <| sess.AddNewConnection sp DateTime.UtcNow cid netportidx_me.zero tpgt_me.zero conParam
        Assert.True( sess.IsExistCID( cid ) )

        // send Nop-Out PDU
        let nopoutpdu1 = {
            Session_Test.defaultNopOUTPDUValues with
                I = nopOutImm;
                CmdSN = firstCmdSN;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = nopOutITT;
                PingData = PooledBuffer.Empty;
        }
        PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, nopoutpdu1 )
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        // receive Nop-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.NOP_IN ) )
        let nopinpdu1 = pdu1 :?> NOPInPDU
        Assert.True(( nopinpdu1.InitiatorTaskTag = nopOutITT ))
        Assert.True(( nopinpdu1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu ))
        Assert.True(( PooledBuffer.ValueEqualsWithArray nopinpdu1.PingData Array.empty ))
        Assert.True( ( nopinpdu1.StatSN = statsn_me.zero ) )

        let con = sess.GetConnection cid ( concnt_me.fromPrim 1 )
        Assert.True(( con.IsSome ))
        let sentPDUs = con.Value.GetSentResponsePDUForSNACK( statsn_me.zero ) 0u
        Assert.True(( sentPDUs.Length = 1 ))
        Assert.True(( sentPDUs.[0].Opcode = OpcodeCd.NOP_IN ))
        Assert.True(( sentPDUs.[0].InitiatorTaskTag = nopOutITT ))

        if not nopOutImm then
            Assert.True( ( nopinpdu1.ExpCmdSN = ( firstCmdSN + cmdsn_me.fromPrim 1u ) ) )
            let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
            Assert.True(( wexpcmdsn = firstCmdSN + cmdsn_me.fromPrim 1u ))

        sess.NoticeUnlockResponseFence ResponseFenceNeedsFlag.R_Mode

        ( sess, pc, killer, smStub, luStub, sp, cp )

    static member defaultNopOUTPDUValues = {
            I = true;
            LUN = lun_me.fromPrim 0x0001020304050607UL;     // LUN is not checked and responsed same value.
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

    static member defaultScsiCommandPDUValues = {
        I = true;
        F = true;
        R = false;
        W = true;
        ATTR = TaskATTRCd.SIMPLE_TASK;
        LUN = lun_me.zero;
        InitiatorTaskTag = itt_me.fromPrim 1u;
        ExpectedDataTransferLength = 256u;
        CmdSN = cmdsn_me.zero;
        ExpStatSN = statsn_me.fromPrim 1u;
        ScsiCDB = [| 0uy .. 15uy |];
        DataSegment = PooledBuffer.Rent [| 0uy .. 255uy |];
        BidirectionalExpectedReadDataLength = 0u;
        ByteCount = 0u;
    }

    static member defaultScsiResponsePDUValues = {
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
        SenseData = ArraySegment<byte>( Array.empty, 0, 0 );
        ResponseData = ArraySegment<byte>( Array.empty, 0, 0 );
        ResponseFence = ResponseFenceNeedsFlag.Immediately;
        DataInBuffer = PooledBuffer.Empty;
        LUN = lun_me.zero;
    }

    static member defaultScsiDataOutPDUValues = {
        F = true;
        LUN = lun_me.zero;
        InitiatorTaskTag = itt_me.fromPrim 1u;
        TargetTransferTag = ttt_me.fromPrim 0u;
        ExpStatSN = statsn_me.zero;
        DataSN = datasn_me.zero;
        BufferOffset = 0u;
        DataSegment = PooledBuffer.Rent [| 0uy .. 255uy |];
        ByteCount = 0u;
    }

    static member defaultTextRequestPDUValues = {
        I = true;
        F = true;
        C = false;
        LUN = lun_me.zero;
        InitiatorTaskTag = itt_me.fromPrim 1u;
        TargetTransferTag = ttt_me.fromPrim 0u;
        CmdSN = cmdsn_me.zero;
        ExpStatSN = statsn_me.zero;
        TextRequest = Array.empty;
        ByteCount = 0u;
    }

    static member defaultTaskManagementRequestPDUValues = {
        I = false;
        Function = TaskMgrReqCd.ABORT_TASK;
        LUN = lun_me.zero;
        InitiatorTaskTag = itt_me.fromPrim 1u;
        ReferencedTaskTag = itt_me.fromPrim 0u;
        CmdSN = cmdsn_me.fromPrim 1u;
        ExpStatSN = statsn_me.zero;
        RefCmdSN = cmdsn_me.fromPrim 1u;
        ExpDataSN = datasn_me.zero;
        ByteCount = 0u;
    }

    static member defaultTaskManagementResponsePDUValues = {
        Response = TaskMgrResCd.FUNCTION_COMPLETE;
        InitiatorTaskTag = itt_me.fromPrim 0u;
        StatSN = statsn_me.zero;
        ExpCmdSN = cmdsn_me.zero;
        MaxCmdSN = cmdsn_me.zero;
        ResponseFence = ResponseFenceNeedsFlag.Immediately;
    }

    static member defaultLogoutRequestPDUValues = {
        I = false;
        ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
        InitiatorTaskTag = itt_me.fromPrim 1u;
        CID = cid_me.fromPrim 0us;
        CmdSN = cmdsn_me.zero;
        ExpStatSN = statsn_me.zero;
        ByteCount = 0u;
    }

    static member defaultSNACKRequestPDUValues = {
        Type = SnackReqTypeCd.STATUS;
        LUN = lun_me.zero;
        InitiatorTaskTag = itt_me.fromPrim 1u;
        TargetTransferTag = ttt_me.fromPrim 0u;
        ExpStatSN = statsn_me.zero;
        BegRun = 0u;
        RunLength = 0u;
        ByteCount = 0u;
    }

    static member GetWaitingQueue ( pc : PrivateCaller ) =
        ( pc.GetField( "m_ProcessWaitQueue" ) :?> OptimisticLock< ProcessWaitQueue > ).obj.WaitingQueue

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constractor() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()
        let cdt = DateTime.UtcNow

        let s = new Session( smStub, cdt, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer )

        Assert.True(( cdt = ( s :> ISession ).CreateDate ))
        Assert.True(( 3us = ( s :> ISession ).SessionParameter.MaxConnections ))
        Assert.True(( "abcT" = ( s :> ISession ).SessionParameter.TargetConf.TargetName ))
        Assert.True(( "abcI" = ( s :> ISession ).SessionParameter.InitiatorName ))
        Assert.True(( "abcIA" = ( s :> ISession ).SessionParameter.InitiatorAlias ))
        Assert.True(( tpgt_me.zero = ( s :> ISession ).SessionParameter.TargetPortalGroupTag ))
        Assert.True(( false = ( s :> ISession ).SessionParameter.InitialR2T ))
        Assert.True(( true = ( s :> ISession ).SessionParameter.ImmediateData ))
        Assert.True(( 4096u = ( s :> ISession ).SessionParameter.MaxBurstLength ))
        Assert.True(( 4096u = ( s :> ISession ).SessionParameter.FirstBurstLength ))
        Assert.True(( 1us = ( s :> ISession ).SessionParameter.DefaultTime2Wait ))
        Assert.True(( 1us = ( s :> ISession ).SessionParameter.DefaultTime2Retain ))
        Assert.True(( 1us = ( s :> ISession ).SessionParameter.MaxOutstandingR2T ))
        Assert.True(( true = ( s :> ISession ).SessionParameter.DataPDUInOrder ))
        Assert.True(( true = ( s :> ISession ).SessionParameter.DataSequenceInOrder ))
        Assert.True(( 0uy = ( s :> ISession ).SessionParameter.ErrorRecoveryLevel ))
        Assert.True(( tsih_me.fromPrim 0us = ( s :> ISession ).TSIH ))
        Assert.True(( itNexus = ( s :> ISession ).I_TNexus ))
        Assert.True(( false = ( s :> ISession ).IsExistCID( cid_me.fromPrim 0us ) ))

    [<Fact>]
    member _.AddNewConnection_001() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()
        let sp, cp = GlbFunc.GetNetConn()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        let psus = {
            Session_Test.defaultNopOUTPDUValues with
                InitiatorTaskTag = itt_me.fromPrim 0xF0F0F0F0u;
        }

        use w = new SemaphoreSlim( 1 )
        w.Wait() |> ignore

        [|
            fun () -> task {
                let r = sess.AddNewConnection sp DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
                Assert.True( r )
                let pc = new PrivateCaller( sess )
                Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 1 ) )
                Assert.True( sess.IsExistCID( cid_me.fromPrim 0us ) )
                Assert.False( sess.IsExistCID( cid_me.fromPrim 1us ) )

                PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, psus )
                |> Functions.TaskIgnore
                |> Functions.RunTaskSynchronously

                do! w.WaitAsync()
                sess.DestroySession()
                sp.Close()
                sp.Dispose()
            };
            fun () -> task {
                let! nopInPdu =
                    PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                Assert.True(( nopInPdu.Opcode = OpcodeCd.NOP_IN ))
                let lpdu = nopInPdu :?> NOPInPDU
                Assert.True( ( lpdu.LUN = psus.LUN ) )
                Assert.True( ( lpdu.InitiatorTaskTag = psus.InitiatorTaskTag ) )
                Assert.True( ( PooledBuffer.ValueEquals lpdu.PingData psus.PingData ) )
                w.Release() |> ignore
                cp.Close()
                cp.Dispose()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore



    [<Fact>]
    member _.AddNewConnection_002() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()
        let sp1, cp1 = GlbFunc.GetNetConn()
        let sp2, cp2 = GlbFunc.GetNetConn()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        let psus = {
            Session_Test.defaultNopOUTPDUValues with
                InitiatorTaskTag = itt_me.fromPrim 0xF0F0F0F0u;
        }
        use w = new SemaphoreSlim( 1 )
        w.Wait() |> ignore

        [|
            fun () -> task {
                let r1 = sess.AddNewConnection sp1 DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
                Assert.True( r1 )
                let pc = new PrivateCaller( sess )
                Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 1 ) )

                PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp1, psus )
                |> Functions.TaskIgnore
                |> Functions.RunTaskSynchronously

                let r2 = sess.AddNewConnection sp2 DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam  // same CID
                Assert.False( r2 )
                Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 2 ) )

                Assert.True( sess.IsExistCID( cid_me.fromPrim 0us ) )
                Assert.False( sess.IsExistCID( cid_me.fromPrim 1us ) )

                do! w.WaitAsync()
                sess.DestroySession()
                GlbFunc.ClosePorts [| sp1; sp2 |]
            };
            fun () -> task {
                let! nopInPdu =
                    PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp1, Standpoint.Initiator )
                Assert.True( ( nopInPdu.Opcode = OpcodeCd.NOP_IN ) )
                let lpdu = nopInPdu :?> NOPInPDU
                Assert.True( ( lpdu.LUN = psus.LUN ) )
                Assert.True( ( lpdu.InitiatorTaskTag = psus.InitiatorTaskTag ) )
                Assert.True( ( PooledBuffer.ValueEquals lpdu.PingData psus.PingData ) )

                w.Release() |> ignore
                GlbFunc.ClosePorts [| cp1; cp2 |]
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore


    [<Fact>]
    member _.AddNewConnection_003() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()
        let sp1, cp1 = GlbFunc.GetNetConn()
        let sp2, cp2 = GlbFunc.GetNetConn()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        let psus1 = {
            Session_Test.defaultNopOUTPDUValues with
                InitiatorTaskTag = itt_me.fromPrim 0xF0F0F0F0u;
                PingData = PooledBuffer.Rent [| 0x00uy .. 0xFFuy |];
        }

        let psus2 = {
            Session_Test.defaultNopOUTPDUValues with
                InitiatorTaskTag = itt_me.fromPrim 0xEEEEEEEEu;
                PingData = PooledBuffer.Empty;
        }

        let ww = [|
            for _ in 1 .. 2 do
                let w = new SemaphoreSlim( 1 )
                w.Wait()
                yield w
        |]

        [|
            fun () -> task {
                let r1 = sess.AddNewConnection sp1 DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero  Session_Test.defaultConnectionParam
                Assert.True( r1 )
                let pc = new PrivateCaller( sess )
                Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 1 ) )
                let conn1 = sess.GetConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )
                sess.PushReceivedPDU conn1.Value psus1

                do! ww.[0].WaitAsync()

                let r2 = sess.AddNewConnection sp2 DateTime.UtcNow ( cid_me.fromPrim 1us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
                Assert.True( r2 )
                Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 2 ) )
                let conn2 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 2 )
                sess.PushReceivedPDU conn2.Value psus2

                do! ww.[1].WaitAsync()

                Assert.True( sess.IsExistCID( cid_me.fromPrim 0us ) )
                Assert.True( sess.IsExistCID( cid_me.fromPrim 1us ) )
                Assert.False( sess.IsExistCID( cid_me.fromPrim 2us ) )

                sess.DestroySession()
                GlbFunc.ClosePorts [| sp1; sp2 |]
            };
            fun () -> task {
                let! nopInPdu1 = PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp1, Standpoint.Initiator )
                Assert.True( ( nopInPdu1.Opcode = OpcodeCd.NOP_IN ) )
                let lpdu1 = nopInPdu1 :?> NOPInPDU
                Assert.True( ( lpdu1.LUN = psus1.LUN ) )
                Assert.True( ( lpdu1.InitiatorTaskTag = psus1.InitiatorTaskTag ) )
                Assert.True( ( PooledBuffer.ValueEquals lpdu1.PingData psus1.PingData ) )

                ww.[0].Release() |> ignore

                let! nopInPdu2 = PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp2, Standpoint.Initiator )
                Assert.True( ( nopInPdu2.Opcode = OpcodeCd.NOP_IN ) )
                let lpdu2 = nopInPdu2 :?> NOPInPDU
                Assert.True( ( lpdu2.LUN = psus2.LUN ) )
                Assert.True( ( lpdu2.InitiatorTaskTag = psus2.InitiatorTaskTag ) )
                Assert.True( ( PooledBuffer.ValueEquals lpdu2.PingData psus2.PingData ) )

                ww.[1].Release() |> ignore

                GlbFunc.ClosePorts [| sp1; sp2 |]
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore
        ww |> Array.iter ( fun i -> i.Dispose() )


    [<Fact>]
    member _.AddNewConnection_004() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()
        let sp, cp = GlbFunc.GetNetConnV( 4 )

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        let r1 = sess.AddNewConnection sp.[0] DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True( r1 )
        let pc = new PrivateCaller( sess )
        Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 1 ) )

        let r2 = sess.AddNewConnection sp.[1] DateTime.UtcNow ( cid_me.fromPrim 1us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True( r2 )
        Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 2 ) )

        let r3 = sess.AddNewConnection sp.[2] DateTime.UtcNow ( cid_me.fromPrim 2us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True( r3 )
        Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 3 ) )

        let r4 = sess.AddNewConnection sp.[3] DateTime.UtcNow ( cid_me.fromPrim 3us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.False( r4 )
        Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 4 ) )

        Assert.False( sess.IsExistCID( cid_me.fromPrim 3us ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 0us ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 1us ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 2us ) )

        sess.DestroySession()
        GlbFunc.ClosePorts sp
        GlbFunc.ClosePorts cp

    [<Fact>]
    member _.AddNewConnection_005() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        use sp = new MemoryStream()
        sess.DestroySession()
        let r = sess.AddNewConnection sp DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.False( r )
        let pc = new PrivateCaller( sess )
        Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 0 ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 0us ) )


    [<Fact>]
    member _.AddNewConnection_006() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        // Notice terminate session
        sess.DestroySession()

        use sp = new MemoryStream()

        let r = sess.AddNewConnection sp DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.False r

        sp.Close()

    [<Fact>]
    member _.RemoveConnection_001() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        sess.RemoveConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 0 )
        let pc = new PrivateCaller( sess )
        let m_ConnectionCounter = pc.GetField( "m_ConnectionCounter" ) :?> int
        Assert.True(( m_ConnectionCounter = 0 ))
        Assert.False( sess.IsExistCID( cid_me.fromPrim 0us ) )

        sess.DestroySession()

    [<Fact>]
    member _.RemoveConnection_002() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()
        let sps, cps = GlbFunc.GetNetConnV( 5 )

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        let pc = new PrivateCaller( sess )

        let r1 = sess.AddNewConnection sps.[0] DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r1
        let r2 = sess.AddNewConnection sps.[1] DateTime.UtcNow ( cid_me.fromPrim 1us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r2
        let r3 = sess.AddNewConnection sps.[2] DateTime.UtcNow ( cid_me.fromPrim 2us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r3

        sess.RemoveConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 2 ) 

        Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 3 ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 0us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 1us ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 2us ) )
        let m_CIDs = pc.GetField( "m_CIDs" ) :?> OptimisticLock< ImmutableDictionary< uint16, CIDInfo > >
        Assert.True(( m_CIDs.obj.Count = 2 ))
        Assert.False( ( pc.GetField( "m_Killer" ) :?> IKiller ).IsNoticed )

        let r4 = sess.AddNewConnection sps.[3] DateTime.UtcNow ( cid_me.fromPrim 3us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r4
        let r5 = sess.AddNewConnection sps.[4] DateTime.UtcNow ( cid_me.fromPrim 4us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.False r5

        Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 5 ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 0us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 1us ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 2us ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 3us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 4us ) )
        let m_CIDs = pc.GetField( "m_CIDs" ) :?> OptimisticLock< ImmutableDictionary< uint16, CIDInfo > >
        Assert.True(( m_CIDs.obj.Count = 3 ))

        sess.RemoveConnection ( cid_me.fromPrim 2us ) ( concnt_me.fromPrim 3 ) 
        Assert.True( sess.IsExistCID( cid_me.fromPrim 0us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 1us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 2us ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 3us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 4us ) )
        let m_CIDs = pc.GetField( "m_CIDs" ) :?> OptimisticLock< ImmutableDictionary< uint16, CIDInfo > >
        Assert.True(( m_CIDs.obj.Count = 2 ))
        Assert.False( ( pc.GetField( "m_Killer" ) :?> IKiller ).IsNoticed )

        sess.RemoveConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 99 )
        let m_CIDs = pc.GetField( "m_CIDs" ) :?> OptimisticLock< ImmutableDictionary< uint16, CIDInfo > >
        Assert.True(( m_CIDs.obj.Count = 2 ))

        sess.RemoveConnection ( cid_me.fromPrim 99us ) ( concnt_me.fromPrim 0 )
        let m_CIDs = pc.GetField( "m_CIDs" ) :?> OptimisticLock< ImmutableDictionary< uint16, CIDInfo > >
        Assert.True(( m_CIDs.obj.Count = 2 ))

        sess.RemoveConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 ) 
        Assert.False( sess.IsExistCID( cid_me.fromPrim 0us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 1us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 2us ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 3us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 4us ) )
        let m_CIDs = pc.GetField( "m_CIDs" ) :?> OptimisticLock< ImmutableDictionary< uint16, CIDInfo > >
        Assert.True(( m_CIDs.obj.Count = 1 ))
        Assert.False( ( pc.GetField( "m_Killer" ) :?> IKiller ).IsNoticed )

        sess.RemoveConnection ( cid_me.fromPrim 3us ) ( concnt_me.fromPrim 4 ) 
        Assert.False( sess.IsExistCID( cid_me.fromPrim 0us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 1us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 2us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 3us ) )
        Assert.False( sess.IsExistCID( cid_me.fromPrim 4us ) )
        let m_CIDs = pc.GetField( "m_CIDs" ) :?> OptimisticLock< ImmutableDictionary< uint16, CIDInfo > >
        Assert.True(( m_CIDs.obj.Count = 0 ))
        Assert.True( ( pc.GetField( "m_Killer" ) :?> IKiller ).IsNoticed )

        sess.DestroySession()

        GlbFunc.ClosePorts cps
        GlbFunc.ClosePorts sps

    [<Fact>]
    member _.RemoveConnection_003() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        // destroy session
        sess.DestroySession()

        sess.RemoveConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 0 ) 
        let pc = new PrivateCaller( sess )
        Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 0 ) )

    [<Fact>]
    member _.RemoveConnection_004() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 0us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let sp2, cp2 = GlbFunc.GetNetConn()
        let conn1 = sess.GetConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )

        let r1 = sess.AddNewConnection sp2 DateTime.UtcNow ( cid_me.fromPrim 1us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r1
        let conn2 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 2 )

        let mutable cnt = 0
        luStub.p_SCSICommand <- ( fun _ pdu _ ->
            Assert.True(( pdu.CmdSN = cmdsn_me.fromPrim 2u ))
            cnt <- 1
        )

        // Send SCSI Command PDU1
        let scsicmd1 = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = false;  // unresolved
                CmdSN = cmdsn_me.fromPrim 1u;
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 10u;
                DataSegment = PooledBuffer.Empty;
        }
        sess.PushReceivedPDU conn1.Value scsicmd1    // Still not running
        Assert.True(( cnt = 0 ))

        // Send SCSI Command PDU1
        let scsicmd2 = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;  // resolved
                CmdSN = cmdsn_me.fromPrim 2u;
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 0u;
                DataSegment = PooledBuffer.Empty;
        }
        sess.PushReceivedPDU conn2.Value scsicmd2    // Still not running
        Assert.True(( cnt = 0 ))

        sess.RemoveConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )
        Assert.True(( cnt = 1 ))                    // scsicmd2 executed

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; sp2; cp; cp2; |]

    [<Fact>]
    member _.ReinstateConnection_001() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()
        let sps, cps = GlbFunc.GetNetConnV( 5 )

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        let pc = new PrivateCaller( sess )

        let r1 = sess.AddNewConnection sps.[0] DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r1
        let r2 = sess.AddNewConnection sps.[1] DateTime.UtcNow ( cid_me.fromPrim 1us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r2
        let r3 = sess.AddNewConnection sps.[2] DateTime.UtcNow ( cid_me.fromPrim 2us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r3

        let r4 = sess.ReinstateConnection sps.[3] DateTime.UtcNow ( cid_me.fromPrim 1us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r4

        Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 4 ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 0us ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 1us ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 2us ) )
        let m_CIDs = pc.GetField( "m_CIDs" ) :?> OptimisticLock< ImmutableDictionary< uint16, CIDInfo > >
        Assert.True(( m_CIDs.obj.Count = 3 ))
        Assert.False( ( pc.GetField( "m_Killer" ) :?> IKiller ).IsNoticed )

        try
            let _ = sps.[1].ReadByte()
            Assert.Fail __LINE__
        with
        | :? ObjectDisposedException ->
            ()

        sess.DestroySession()
        GlbFunc.ClosePorts sps
        sps |> Array.iter ( fun i -> i.Dispose() )
        GlbFunc.ClosePorts cps

    [<Fact>]
    member _.ReinstateConnection_002() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()
        let sps, cps = GlbFunc.GetNetConnV( 3 )

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        let pc = new PrivateCaller( sess )

        let r1 = sess.AddNewConnection sps.[0] DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r1
        let r2 = sess.ReinstateConnection sps.[1] DateTime.UtcNow ( cid_me.fromPrim 1us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r2
        let r3 = sess.ReinstateConnection sps.[2] DateTime.UtcNow ( cid_me.fromPrim 2us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r3

        Assert.True( ( pc.GetField( "m_ConnectionCounter" ) = box 3 ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 0us ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 1us ) )
        Assert.True( sess.IsExistCID( cid_me.fromPrim 2us ) )
        let m_CIDs = pc.GetField( "m_CIDs" ) :?> OptimisticLock< ImmutableDictionary< uint16, CIDInfo > >
        Assert.True(( m_CIDs.obj.Count = 3 ))
        Assert.False( ( pc.GetField( "m_Killer" ) :?> IKiller ).IsNoticed )

        sess.DestroySession()
        GlbFunc.ClosePorts sps
        GlbFunc.ClosePorts cps

    [<Fact>]
    member _.ReinstateConnection_003() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        // Notice terminate session
        sess.DestroySession()

        // stream
        use sp = new MemoryStream()

        // first pdus
        let rs = sess.ReinstateConnection sp DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.False rs
        sp.Close()

    [<Fact>]
    member _.ReinstateConnection_004() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 0us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let sp2, cp2 = GlbFunc.GetNetConn()
        let sp3, cp3 = GlbFunc.GetNetConn()
        let conn1 = sess.GetConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )

        let r1 = sess.AddNewConnection sp2 DateTime.UtcNow ( cid_me.fromPrim 1us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r1
        let conn2 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 2 )

        let mutable cnt = 0
        luStub.p_SCSICommand <- ( fun _ pdu _ ->
            Assert.True(( pdu.CmdSN = cmdsn_me.fromPrim 2u ))
            cnt <- 1
        )

        // Send SCSI Command PDU1
        let scsicmd1 = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = false;  // unresolved
                CmdSN = cmdsn_me.fromPrim 1u;
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 10u;
                DataSegment = PooledBuffer.Empty;
        }
        sess.PushReceivedPDU conn1.Value scsicmd1    // Still not running
        Assert.True(( cnt = 0 ))

        // Send SCSI Command PDU1
        let scsicmd2 = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;  // resolved
                CmdSN = cmdsn_me.fromPrim 2u;
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 0u;
                DataSegment = PooledBuffer.Empty;
        }
        sess.PushReceivedPDU conn2.Value scsicmd2    // Still not running
        Assert.True(( cnt = 0 ))

        let rs = sess.ReinstateConnection sp3 DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True(( cnt = 1 ))                    // scsicmd2 executed

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; sp2; sp3; cp; cp2; cp3; |]

    [<Fact>]
    member _.PushReceivedPDU_001() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()
        let sp, cp = GlbFunc.GetNetConn()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        let noppdu2 = {
            Session_Test.defaultNopOUTPDUValues with
                InitiatorTaskTag = itt_me.fromPrim 2u;
        }

        let r1 = sess.AddNewConnection sp DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r1
        Assert.True( sess.IsExistCID( cid_me.fromPrim 0us ) )
        let conn1 = sess.GetConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )

        sess.DestroySession()
        sess.PushReceivedPDU conn1.Value noppdu2    // session is already terminated
        sp.Close()
        sp.Dispose()

        cp.Close()
        cp.Dispose()
        
    [<Fact>]
    member _.PushReceivedPDU_003_Immidiate_NopOUT_001() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let killer = new HKiller()
        let smStub, _ = Session_Test.GenDefaultStubs()
        let sp, cp = GlbFunc.GetNetConn()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession
        let pc = new PrivateCaller( sess )

        // pdus
        let noppdu2 = {
            Session_Test.defaultNopOUTPDUValues with
                InitiatorTaskTag = itt_me.fromPrim 1u;
        }

        // Add test connection to session object in the target
        let r1 = sess.AddNewConnection sp DateTime.UtcNow ( cid_me.fromPrim 0us ) netportidx_me.zero tpgt_me.zero Session_Test.defaultConnectionParam
        Assert.True r1
        Assert.True( sess.IsExistCID( cid_me.fromPrim 0us ) )
        let conn1 = sess.GetConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )

        // receive PDU in the target
        sess.PushReceivedPDU conn1.Value noppdu2

        // receive Nop-IN PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.NOP_IN ) )
        let itt2 = pdu2.InitiatorTaskTag
        Assert.True(( itt2 = itt_me.fromPrim 1u ))

        let cnt = ( Session_Test.GetWaitingQueue pc ).Count
        Assert.True(( 0 = cnt ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_004_Immidiate_NopOUT_002() =
        // Create session object
        let sess, _, killer, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 0us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )

        // PDUs
        let secondpdu = {
            // If ITT is not 0xFFFFFFFF, TTT must be 0xFFFFFFFF
            Session_Test.defaultNopOUTPDUValues with
                TargetTransferTag = ttt_me.fromPrim 0u; // error
        }

        // do the test
        sess.PushReceivedPDU conn1.Value secondpdu

        Assert.True( ( killer :> IKiller ).IsNoticed )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_005_Immidiate_NopOUT_003() =
        // Create session object
        let sess, pc, killer, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 0us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )

        let secondpdu = {
            Session_Test.defaultNopOUTPDUValues with
                InitiatorTaskTag = itt_me.fromPrim 1u;
        }

        let b = new Barrier( 2 )
        let dummyObj = CComponent_Stub()
        dummyObj.p_Terminate <- ( fun () -> b.SignalAndWait() )
        ( killer :> IKiller ).Add dummyObj

        cp.Socket.Disconnect false |> ignore
        cp.Dispose()    // Close connection

        // do the test
        sess.PushReceivedPDU conn1.Value secondpdu

        b.SignalAndWait()

        sess.DestroySession()
        sp.Dispose()

    [<Fact>]
    member _.PushReceivedPDU_006_Immidiate_NopOUT_004() =
        // Create session object
        let sess, _, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 0us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )

        let pdu2 = {
            Session_Test.defaultNopOUTPDUValues with
                InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu;
        }
        let pdu3 = {
            Session_Test.defaultNopOUTPDUValues with
                InitiatorTaskTag = itt_me.fromPrim 1u;
        }

        // do the test in the target
        sess.PushReceivedPDU conn1.Value pdu2
        sess.PushReceivedPDU conn1.Value pdu2
        sess.PushReceivedPDU conn1.Value pdu2
        sess.PushReceivedPDU conn1.Value pdu3

        // receive Nop-In PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.NOP_IN ) )
        let itt2 = pdu2.InitiatorTaskTag
        Assert.True(( itt2 = itt_me.fromPrim 1u ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_006_Immidiate_NopOUT_005() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 0us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )

        let pdu2 = {
            Session_Test.defaultNopOUTPDUValues with
                InitiatorTaskTag = itt_me.fromPrim 1u;  // Same value to next SCSI Command PDU
        }
        // SCSI Command PDU
        let cmdpdu = {
            Session_Test.defaultScsiCommandPDUValues with
                I = true;
                F = false;
                W = true;
                R = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;  // Waiting for unsolicited data
        }

        // Receive SCSI Command PDU in the target
        sess.PushReceivedPDU conn1.Value cmdpdu

        // SCSI Command PDU is queued.
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Receive Nop-OUT PDU that has ITT value same as the queued SCSI Command PDU in the target.
        sess.PushReceivedPDU conn1.Value pdu2

        // Receive Reject response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu2 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq2.Count ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_007_Immidiate_ScsiCommand_001() =
        // Create session object
        let sess, _, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Gen SCSI Command PDU
        let pdu2 = Session_Test.defaultScsiCommandPDUValues

        let mutable cnt = 0
        luStub.p_SCSICommand <-
            ( fun ( source : CommandSourceInfo ) ( command : SCSICommandPDU ) ( data : SCSIDataOutPDU list ) ->
            Assert.True(( source.CID = cid_me.fromPrim 1us ))
            Assert.True(( command.ATTR = TaskATTRCd.SIMPLE_TASK ))
            Assert.True(( command.CmdSN = cmdsn_me.zero ))
            Assert.True(( command.ExpectedDataTransferLength = 256u ))
            Assert.True(( command.BidirectionalExpectedReadDataLength = 0u ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray command.DataSegment [| 0uy .. 255uy |] ))
            Assert.True(( command.InitiatorTaskTag = itt_me.fromPrim 1u ))
            Assert.True(( command.LUN = lun_me.zero ))
            Assert.True(( data.Length = 0 ))
            cnt <- 1
        )

        // Send SCSI Command PDU
        sess.PushReceivedPDU conn1.Value pdu2

        Assert.True(( cnt = 1 ))
                
        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_008_Immidiate_ScsiCommand_002() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                }
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Gen SCSI Command PDU
        let cmdpdu = {
            Session_Test.defaultScsiCommandPDUValues with
                I = true;
                W = true;
                R = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }

        // Gen SCSI Data-Out PDU
        let datapdu = {
            Session_Test.defaultScsiDataOutPDUValues with
                F = true;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                BufferOffset = 10u;
                DataSegment = PooledBuffer.RentAndInit 30;
        }

        let mutable cnt = 0
        luStub.p_SCSICommand <-
            ( fun ( source : CommandSourceInfo ) ( command : SCSICommandPDU ) ( data : SCSIDataOutPDU list ) ->
            Assert.True(( source.CID = cid_me.fromPrim 1us ))
            Assert.True(( command.ATTR = TaskATTRCd.SIMPLE_TASK ))
            Assert.True(( command.CmdSN = cmdsn_me.zero ))
            Assert.True(( command.ExpectedDataTransferLength = 40u ))
            Assert.True(( command.BidirectionalExpectedReadDataLength = 0u ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray command.DataSegment ( Array.zeroCreate( 10 ) ) ))
            Assert.True(( command.InitiatorTaskTag = itt_me.fromPrim 1u ))
            Assert.True(( command.LUN = lun_me.zero ))
            Assert.True(( data.Length = 1 ))
            Assert.True(( data.Item( 0 ).DataSegment |> PooledBuffer.length = 30 ))
            cnt <- 1
        )

        // Receive SCSI Command PDU in the target
        sess.PushReceivedPDU conn1.Value cmdpdu

        // Receive R2T PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.R2T ) )
        Assert.True(( pdu2.InitiatorTaskTag = itt_me.fromPrim 1u ))
        let r2tpdu = pdu2 :?> R2TPDU
        Assert.True(( 10u = r2tpdu.BufferOffset ))
        Assert.True(( 30u = r2tpdu.DesiredDataTransferLength ))

        // Check status in the target
        Assert.True(( 0 = cnt ))
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Receive SCSI Data-Out PDU in the target
        sess.PushReceivedPDU conn1.Value datapdu
        Assert.True(( 1 = cnt ))

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq2.Count ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_009_Immidiate_ScsiCommand_003() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = false;
                }
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Gen SCSI Command PDU
        let cmdpdu = {
            Session_Test.defaultScsiCommandPDUValues with
                I = true;
                W = true;
                R = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }

        // Gen SCSI Data-Out PDU
        let datapdu = {
            Session_Test.defaultScsiDataOutPDUValues with
                F = true;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                BufferOffset = 10u;
                DataSegment = PooledBuffer.RentAndInit 30;
        }

        let mutable cnt = 0
        luStub.p_SCSICommand <-
            ( fun ( source : CommandSourceInfo ) ( command : SCSICommandPDU ) ( data : SCSIDataOutPDU list ) ->
                Assert.True(( source.CID = cid_me.fromPrim 1us ))
                Assert.True(( command.ATTR = TaskATTRCd.SIMPLE_TASK ))
                Assert.True(( command.CmdSN = cmdsn_me.zero ))
                Assert.True(( command.ExpectedDataTransferLength = 40u ))
                Assert.True(( command.BidirectionalExpectedReadDataLength = 0u ))
                Assert.True(( PooledBuffer.ValueEqualsWithArray command.DataSegment ( Array.zeroCreate( 10 ) ) ))
                Assert.True(( command.InitiatorTaskTag = itt_me.fromPrim 1u ))
                Assert.True(( command.LUN = lun_me.zero ))
                Assert.True(( data.Length = 1 ))
                Assert.True(( data.Item( 0 ).DataSegment |> PooledBuffer.length = 30 ))
                cnt <- 1
            )

        // Receive SCSI Data-Out PDU
        sess.PushReceivedPDU conn1.Value datapdu
        Assert.True(( cnt = 0 ))
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Receive SCSI Command PDU
        sess.PushReceivedPDU conn1.Value cmdpdu
        Assert.True(( cnt = 1 ))

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq2.Count ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_010_Immidiate_ScsiCommand_004() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = false;
                }
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // SCSI Command PDU(ITT=1)
        let cmdpdu1 = {
            Session_Test.defaultScsiCommandPDUValues with
                I = true;
                W = true;
                R = false;
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                CmdSN = cmdsn_me.fromPrim 2u;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }

        // SCSI Data-Out PDU(ITT=1)
        let datapdu1 = {
            Session_Test.defaultScsiDataOutPDUValues with
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 10u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let datapdu2 = {
            Session_Test.defaultScsiDataOutPDUValues with
                F = true;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.fromPrim 1u;
                BufferOffset = 20u;
                DataSegment = PooledBuffer.RentAndInit 20;
        }

        // SCSI Command PDU(ITT=2)
        let cmdpdu2 = {
            Session_Test.defaultScsiCommandPDUValues with   // F = true;
                I = true;
                W = true;
                R = false;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                CmdSN = cmdsn_me.fromPrim 1u;
                ExpectedDataTransferLength = 20u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }

        // SCSI Data-Out PDU(ITT=2)
        let datapdu3 = {
            Session_Test.defaultScsiDataOutPDUValues with
                F = true;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 10u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }

        let mutable cnt = 0
        luStub.p_SCSICommand <-
            ( fun ( source : CommandSourceInfo ) ( command : SCSICommandPDU ) ( data : SCSIDataOutPDU list ) ->
                cnt <- cnt + 1
                if cnt = 1 then
                    Assert.True(( source.CID = cid_me.fromPrim 1us ))
                    Assert.True(( command.ATTR = TaskATTRCd.SIMPLE_TASK ))
                    Assert.True(( command.CmdSN = cmdsn_me.fromPrim 2u ))
                    Assert.True(( command.ExpectedDataTransferLength = 40u ))
                    Assert.True(( command.BidirectionalExpectedReadDataLength = 0u ))
                    Assert.True(( PooledBuffer.ValueEqualsWithArray command.DataSegment ( Array.zeroCreate 10 ) ))
                    Assert.True(( command.InitiatorTaskTag = itt_me.fromPrim 1u ))
                    Assert.True(( command.LUN = lun_me.zero ))
                    Assert.True(( data.Length = 2 ))
                    Assert.True(( data.Item( 0 ).DataSegment |> PooledBuffer.length = 20 ))
                    Assert.True(( data.Item( 1 ).DataSegment |> PooledBuffer.length = 10 ))
                else
                    Assert.True(( source.CID = cid_me.fromPrim 1us ))
                    Assert.True(( command.ATTR = TaskATTRCd.SIMPLE_TASK ))
                    Assert.True(( command.CmdSN = cmdsn_me.fromPrim 1u ))
                    Assert.True(( command.ExpectedDataTransferLength = 20u ))
                    Assert.True(( command.BidirectionalExpectedReadDataLength = 0u ))
                    Assert.True(( PooledBuffer.ValueEqualsWithArray command.DataSegment ( Array.zeroCreate 10 ) ))
                    Assert.True(( command.InitiatorTaskTag = itt_me.fromPrim 2u ))
                    Assert.True(( command.LUN = lun_me.zero ))
                    Assert.True(( data.Length = 1 ))
                    Assert.True(( data.Item( 0 ).DataSegment |> PooledBuffer.length = 10 ))
            )

        // Receive SCSI Data-Out PDU(ITT=1)
        sess.PushReceivedPDU conn1.Value datapdu1
        Assert.True(( sess.IsAlive ))
        Assert.True(( cnt = 0 ))
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))
        let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
        Assert.True(( cmdsn_me.zero = wexpcmdsn ))

        // Receive SCSI Data-Out PDU(ITT=2)
        sess.PushReceivedPDU conn1.Value datapdu3
        Assert.True(( sess.IsAlive ))
        Assert.True(( cnt = 0 ))
        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 2 = iwq2.Count ))
        let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
        Assert.True(( cmdsn_me.zero = wexpcmdsn ))

        // Receive SCSI Command PDU(ITT=1)
        sess.PushReceivedPDU conn1.Value cmdpdu1
        Assert.True(( sess.IsAlive ))
        Assert.True(( cnt = 0 ))
        let iwq3 = Session_Test.GetWaitingQueue pc
        Assert.True(( 2 = iwq3.Count ))
        let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
        Assert.True(( cmdsn_me.zero = wexpcmdsn ))

        // Receive SCSI Data-Out PDU(ITT=1)
        sess.PushReceivedPDU conn1.Value datapdu2
        Assert.True(( sess.IsAlive ))
        Assert.True(( cnt = 1 ))
        let iwq4 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq4.Count ))
        let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
        Assert.True(( cmdsn_me.zero = wexpcmdsn ))

        // Receive SCSI Command PDU(ITT=2)
        sess.PushReceivedPDU conn1.Value cmdpdu2
        Assert.True(( sess.IsAlive ))
        Assert.True(( cnt = 2 ))
        let iwq5 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq5.Count ))
        let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
        Assert.True(( cmdsn_me.zero = wexpcmdsn ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_011_Immidiate_ScsiCommand_005() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = false;
                }
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // SCSI Command PDU
        let cmdpdu = {
            Session_Test.defaultScsiCommandPDUValues with
                I = true;
                W = true;
                R = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }

        // Text negotication request PDU
        let textpdu = {
            Session_Test.defaultTextRequestPDUValues with
                I = true;
                F = false;
                C = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
        }

        // Receive Text request PDU in the target
        sess.PushReceivedPDU conn1.Value textpdu
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Receive Text negotiation response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.TEXT_RES ) )

        // Receive SCSI Command PDU in the target
        sess.PushReceivedPDU conn1.Value cmdpdu

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq2.Count ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_012_Immidiate_ScsiCommand_006() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = false;
                }
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // SCSI Data-Out PDU
        let datapdu = {
            Session_Test.defaultScsiDataOutPDUValues with
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 10u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }

        // Text negotication request PDU
        let textpdu = {
            Session_Test.defaultTextRequestPDUValues with
                I = true;
                F = false;
                C = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
        }

        // Receive Text request PDU in the target
        sess.PushReceivedPDU conn1.Value textpdu
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Receive Text negotiation response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.TEXT_RES ) )

        // Receive SCSI Data-Out PDU in the target (failed)
        sess.PushReceivedPDU conn1.Value datapdu

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq2.Count ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_012_Immidiate_ScsiCommand_007() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        ImmediateData = false;
                }
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // SCSI Command PDU
        let cmdpdu = {
            Session_Test.defaultScsiCommandPDUValues with
                I = true;
                W = true;
                R = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 10u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }

        // Receive SCSI Comment PDU with immediate data.
        sess.PushReceivedPDU conn1.Value cmdpdu

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_012_Immidiate_ScsiCommand_008() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // SCSI Command PDU
        let cmdpdu = {
            Session_Test.defaultScsiCommandPDUValues with
                I = true;
                F = false;
                W = true;
                R = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 10u;
                DataSegment = PooledBuffer.Empty;
        }

        // Receive SCSI Comment PDU.
        sess.PushReceivedPDU conn1.Value cmdpdu

        // SCSI Data-Out PDU ( Out of range )
        let datapdu = {
            Session_Test.defaultScsiDataOutPDUValues with
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 10u;
                DataSegment = PooledBuffer.RentAndInit 1;
        }

        // Receive SCSI Data-Out PDU
        sess.PushReceivedPDU conn1.Value datapdu

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_012_Immidiate_ScsiCommand_009() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // SCSI Command PDU
        let cmdpdu = {
            Session_Test.defaultScsiCommandPDUValues with
                I = true;
                F = false;
                W = true;
                R = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 10u;
                DataSegment = PooledBuffer.Empty;
        }

        // Receive SCSI Comment PDU.
        sess.PushReceivedPDU conn1.Value cmdpdu

        // SCSI Data-Out PDU ( Out of range )
        let datapdu = {
            Session_Test.defaultScsiDataOutPDUValues with
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 11u;
                DataSegment = PooledBuffer.Empty;
        }

        // Receive SCSI Data-Out PDU
        sess.PushReceivedPDU conn1.Value datapdu

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_012_Immidiate_ScsiCommand_010() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // SCSI Command PDU
        let cmdpdu = {
            Session_Test.defaultScsiCommandPDUValues with
                I = true;
                F = false;
                W = true;
                R = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 10u;
                DataSegment = PooledBuffer.Empty;
        }

        // Receive SCSI Comment PDU.
        sess.PushReceivedPDU conn1.Value cmdpdu

        // SCSI Data-Out PDU ( Out of range )
        let datapdu = {
            Session_Test.defaultScsiDataOutPDUValues with
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                DataSegment = PooledBuffer.Rent 11; // too long
        }

        // Receive SCSI Data-Out PDU
        sess.PushReceivedPDU conn1.Value datapdu

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_013_Immidiate_TaskManagement_001() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Task Management Function PDU
        let tmfpdu = {
            Session_Test.defaultTaskManagementRequestPDUValues with
                I = true;
                Function = TaskMgrReqCd.ABORT_TASK;
        }

        let mutable cnt = 0
        luStub.p_AbortTask <- ( fun source initiatorTaskTag referencedTaskTag ->
            cnt <- cnt + 1
            Assert.True(( source.CID = cid_me.fromPrim 1us ))
            Assert.True(( source.ConCounter = concnt_me.fromPrim 1 ))
            Assert.True(( initiatorTaskTag = itt_me.fromPrim 1u ))
            Assert.True(( referencedTaskTag = itt_me.fromPrim 0u ))
        )

        // Receive Task Management Function Request PDU
        sess.PushReceivedPDU conn1.Value tmfpdu
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq1.Count ))
        Assert.True(( 1 = cnt ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_014_Immidiate_TaskManagement_002() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Task Management Function PDU
        let tmfpdu = {
            Session_Test.defaultTaskManagementRequestPDUValues with
                I = true;
                Function = TaskMgrReqCd.ABORT_TASK;
                InitiatorTaskTag = itt_me.fromPrim 1u;
        }

        // Text negotication request PDU
        let textpdu = {
            Session_Test.defaultTextRequestPDUValues with
                I = true;
                F = false;
                C = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
        }

        // Receive Text request PDU in the target
        sess.PushReceivedPDU conn1.Value textpdu
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Receive Text negotiation response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.TEXT_RES ) )

        let mutable cnt = 0
        luStub.p_AbortTask <- ( fun _ _ _ ->
            cnt <- cnt + 1
        )

        // Receive Task Management Function Request PDU in the target (failed)
        sess.PushReceivedPDU conn1.Value tmfpdu

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

        Assert.True(( 0 = cnt ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_015_Immidiate_Logout_001() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Logout request PDU
        let logoutpdu = {
            Session_Test.defaultLogoutRequestPDUValues with
                I = true;
                CID = cid_me.fromPrim 1us;
                ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
        }

        // Receive Logout Request PDU
        sess.PushReceivedPDU conn1.Value logoutpdu
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq1.Count ))

        // Receive Logout response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.LOGOUT_RES ) )
        let itt2 = pdu2.InitiatorTaskTag
        Assert.True(( itt2 = itt_me.fromPrim 1u ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_016_Immidiate_Logout_002() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Logout request PDU
        let logoutpdu = {
            Session_Test.defaultLogoutRequestPDUValues with
                I = true;
                CID = cid_me.fromPrim 1us;
                ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
                InitiatorTaskTag = itt_me.fromPrim 1u;
        }

        // Text negotication request PDU
        let textpdu = {
            Session_Test.defaultTextRequestPDUValues with
                I = true;
                F = false;
                C = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
        }
        
        // Receive Text request PDU in the target
        sess.PushReceivedPDU conn1.Value textpdu
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Receive Text negotiation response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.TEXT_RES ) )

        // Receive Logout Request PDU in the target
        sess.PushReceivedPDU conn1.Value logoutpdu

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq2.Count ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_017_SNACK_001() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // SNACK request PDU
        let snackpdu = Session_Test.defaultSNACKRequestPDUValues

        // Receive SNACK Request PDU
        sess.PushReceivedPDU conn1.Value snackpdu
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq1.Count ))

        // Receive Reject response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu2 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.COM_NOT_SUPPORT ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_019_Immidiate_TextRequest_001() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Text Negotiation request PDU
        let txtpdu1 = {
             Session_Test.defaultTextRequestPDUValues with
                I = true;
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes( "InitiatorAlias=AAA" )
            }
        let txtpdu2 = {
             Session_Test.defaultTextRequestPDUValues with
                I = true;
                F = true;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes( "MaxRecvDataSegmentLength=8192" )
            }

        // Receive text negotiation request PDU1
        sess.PushReceivedPDU conn1.Value txtpdu1
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Receive text negotiation responce1 in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.TEXT_RES ) )
        let txtrespdu1 = pdu2 :?> TextResponsePDU
        Assert.True(( txtrespdu1.InitiatorTaskTag = itt_me.fromPrim 1u ))

        // Receive text negotiation request PDU2
        sess.PushReceivedPDU conn1.Value txtpdu2

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq2.Count ))

        // Receive text negotiation responce2 in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.TEXT_RES ) )
        let txtrespdu2 = pdu3 :?> TextResponsePDU
        Assert.True(( txtrespdu2.InitiatorTaskTag = itt_me.fromPrim 1u ))

        // check updated connection parameter value
        let rConnection = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )
        Assert.True(( rConnection.Value.CurrentParams.MaxRecvDataSegmentLength_I = 8192u ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_020_Immidiate_TextRequest_002() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // SCSI Command PDU
        let scsipdu = {
            Session_Test.defaultScsiCommandPDUValues with
                I = true;
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.Empty;
        }

        // Text negotication request PDU
        let textpdu = {
            Session_Test.defaultTextRequestPDUValues with
                I = true;
                F = false;
                C = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
        }
        
        // Receive SCSI Command PDU in the target
        sess.PushReceivedPDU conn1.Value scsipdu
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Receive Text request PDU in the target
        sess.PushReceivedPDU conn1.Value textpdu

        // Receive Reject response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu2 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq2.Count ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_021_NonImmidiate_NopOUT_001() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 0us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )

        // pdus
        let noppdu2 = {
                Session_Test.defaultNopOUTPDUValues with
                    I = false;
                    CmdSN = cmdsn_me.fromPrim 1u;
                    ExpStatSN = statsn_me.fromPrim 1u;
                    InitiatorTaskTag = itt_me.fromPrim 1u;
            }

        // receive second No-Out PDU in the target
        sess.PushReceivedPDU conn1.Value noppdu2 

        // receive second Nop-IN PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.NOP_IN ) )
        let nopinpdu2 = pdu2 :?> NOPInPDU
        Assert.True( ( nopinpdu2.InitiatorTaskTag = itt_me.fromPrim 1u ) )
        Assert.True( ( nopinpdu2.ExpCmdSN = cmdsn_me.fromPrim 2u ) )
        Assert.True( ( nopinpdu2.StatSN = statsn_me.fromPrim 1u ) )
        let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
        Assert.True(( wexpcmdsn = cmdsn_me.fromPrim 2u ))

        let cnt = ( Session_Test.GetWaitingQueue pc ).Count
        Assert.True(( 0 = cnt ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_022_NonImmidiate_NopOUT_002() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 0us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )

        // pdus
        let noppdu2 = {
                Session_Test.defaultNopOUTPDUValues with
                    I = false;
                    CmdSN = cmdsn_me.zero;       // error
                    ExpStatSN = statsn_me.fromPrim 1u;
                    InitiatorTaskTag = itt_me.fromPrim 1u;
            }

        // receive second No-Out PDU in the target
        sess.PushReceivedPDU conn1.Value noppdu2 

        // receive second Nop-IN PDU in the initiator
        // Receive Reject response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu2 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
        Assert.True(( rejectpdu.ExpCmdSN = cmdsn_me.fromPrim 1u ))

        let cnt = ( Session_Test.GetWaitingQueue pc ).Count
        Assert.True(( 0 = cnt ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_023_NonImmidiate_NopOUT_003() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 0us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 0us ) ( concnt_me.fromPrim 1 )

        // pdus
        let noppdu2 = {
                Session_Test.defaultNopOUTPDUValues with
                    I = false;
                    CmdSN = cmdsn_me.fromPrim ( 2u + Constants.BDLU_MAX_TASKSET_SIZE );       // error
                    ExpStatSN = statsn_me.fromPrim 1u;
                    InitiatorTaskTag = itt_me.fromPrim 1u;
            }

        // receive second No-Out PDU in the target
        sess.PushReceivedPDU conn1.Value noppdu2 

        // Receive Reject response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu2 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
        Assert.True(( rejectpdu.ExpCmdSN = cmdsn_me.fromPrim 1u ))

        let cnt = ( Session_Test.GetWaitingQueue pc ).Count
        Assert.True(( 0 = cnt ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_024_NonImmidiate_TextRequest_001() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Text Negotiation request PDU
        let txtpdu1 = {
             Session_Test.defaultTextRequestPDUValues with
                I = false;
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                CmdSN = cmdsn_me.fromPrim 1u;
                TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes( "InitiatorAlias=AAA" )
            }
        let txtpdu2 = {
             Session_Test.defaultTextRequestPDUValues with
                I = false;
                F = true;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                CmdSN = cmdsn_me.fromPrim 2u;
                TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes( "MaxRecvDataSegmentLength=8192" )
            }

        // Receive text negotiation request PDU1 in the target
        sess.PushReceivedPDU conn1.Value txtpdu1
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Receive text negotiation responce1 in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.TEXT_RES ) )
        let txtrespdu1 = pdu2 :?> TextResponsePDU
        Assert.True(( txtrespdu1.InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( txtrespdu1.ExpCmdSN = cmdsn_me.fromPrim 2u ) )
        Assert.True(( txtrespdu1.StatSN = statsn_me.fromPrim 1u ) )

        // Receive text negotiation request PDU2
        sess.PushReceivedPDU conn1.Value txtpdu2

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq2.Count ))

        // Receive text negotiation responce2 in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.TEXT_RES ) )
        let txtrespdu2 = pdu3 :?> TextResponsePDU
        Assert.True(( txtrespdu2.InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( txtrespdu2.ExpCmdSN = cmdsn_me.fromPrim 3u ) )
        Assert.True(( txtrespdu2.StatSN = statsn_me.fromPrim 2u ) )

        // check updated connection parameter value
        let rConnection = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )
        Assert.True(( rConnection.Value.CurrentParams.MaxRecvDataSegmentLength_I = 8192u ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_025_NonImmidiate_TextRequest_002() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Text Negotiation request PDU
        let txtpdu1 = {
             Session_Test.defaultTextRequestPDUValues with
                I = false;
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                CmdSN = cmdsn_me.zero;   // error
                TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes( "InitiatorAlias=AAA" )
            }

        // Receive text negotiation request PDU in the target
        sess.PushReceivedPDU conn1.Value txtpdu1
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq1.Count ))

        // Receive Reject response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu2 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
        Assert.True(( rejectpdu.ExpCmdSN = cmdsn_me.fromPrim 1u ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_026_NonImmidiate_TextRequest_003() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Text Negotiation request PDU
        let txtpdu1 = {
             Session_Test.defaultTextRequestPDUValues with
                I = false;
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                CmdSN = cmdsn_me.fromPrim 1u;
                TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes( "MaxRecvDataSegmentLength=8192" )
            }
        let txtpdu2 = {
             Session_Test.defaultTextRequestPDUValues with
                I = false;
                F = true;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                CmdSN = cmdsn_me.fromPrim 2u;
                TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes( "InitiatorAlias=AAA" )
            }

        // Receive text negotiation request PDU1
        sess.PushReceivedPDU conn1.Value txtpdu1
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Receive text negotiation responce1 in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.TEXT_RES ) )

        // Receive text negotiation request PDU2 in the target
        sess.PushReceivedPDU conn1.Value txtpdu2

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq2.Count ))

        // Receive text negotiation responce2 in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.TEXT_RES ) )

        // check updated connection parameter value
        let rConnection = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )
        Assert.True(( rConnection.Value.CurrentParams.MaxRecvDataSegmentLength_I = 4096u ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_027_NonImmidiate_TextRequest_004() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Text Negotiation request PDU
        let txtpdu1 = {
             Session_Test.defaultTextRequestPDUValues with
                I = false;
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                CmdSN = cmdsn_me.fromPrim 1u;
                TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes( "InitiatorAlias=AAA" )
            }
        let txtpdu2 = {
             Session_Test.defaultTextRequestPDUValues with
                I = true;       // Error
                F = true;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                CmdSN = cmdsn_me.fromPrim 1u;
                TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes( "MaxRecvDataSegmentLength=8192" )
            }

        // Receive text negotiation request1 PDU in the target
        sess.PushReceivedPDU conn1.Value txtpdu1
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Receive text negotiation responce1 in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.TEXT_RES ) )

        // Receive text negotiation request2 PDU in the target
        sess.PushReceivedPDU conn1.Value txtpdu2

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq2.Count ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_028_NonImmidiate_TextRequest_005() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Text Negotiation request PDU
        let txtpdu1 = {
             Session_Test.defaultTextRequestPDUValues with
                I = false;
                F = false;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                CmdSN = cmdsn_me.fromPrim 2u;
                TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes( "InitiatorAlias=AAA" )
            }
        let txtpdu2 = {
             Session_Test.defaultTextRequestPDUValues with
                I = false;
                F = true;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                CmdSN = cmdsn_me.fromPrim 1u;          // Error
                TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes( "MaxRecvDataSegmentLength=8192" )
            }

        // Receive text negotiation request1 PDU in the target
        sess.PushReceivedPDU conn1.Value txtpdu1
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // In this point initiator does no receive any response

        // Receive text negotiation request2 PDU in the target
        sess.PushReceivedPDU conn1.Value txtpdu2

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq2.Count ))

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_029_NonImmidiate_TaskManagement_001() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Task Management Function PDU
        let tmfpdu = {
            Session_Test.defaultTaskManagementRequestPDUValues with
                I = false;
                CmdSN = cmdsn_me.fromPrim 1u;
                ExpStatSN = statsn_me.fromPrim 1u;
                Function = TaskMgrReqCd.ABORT_TASK;
        }

        let mutable cnt = 0
        luStub.p_AbortTask <- ( fun source initiatorTaskTag referencedTaskTag ->
            cnt <- cnt + 1
            Assert.True(( source.CID = cid_me.fromPrim 1us ))
            Assert.True(( source.ConCounter = concnt_me.fromPrim 1 ))
            Assert.True(( initiatorTaskTag = itt_me.fromPrim 1u ))
            Assert.True(( referencedTaskTag = itt_me.fromPrim 0u ))
        )

        // Receive Task Management Function Request PDU
        sess.PushReceivedPDU conn1.Value tmfpdu
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq1.Count ))
        Assert.True(( 1 = cnt ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_030_NonImmidiate_TaskManagement_002() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Task Management Function PDU
        let tmfpdu = {
            Session_Test.defaultTaskManagementRequestPDUValues with
                I = false;
                CmdSN = cmdsn_me.zero;   // Error
                ExpStatSN = statsn_me.fromPrim 1u;
                Function = TaskMgrReqCd.ABORT_TASK;
        }

        let mutable cnt = 0
        luStub.p_AbortTask <- ( fun _ _ _ ->
            cnt <- cnt + 1
        )

        // Receive Task Management Function Request PDU
        sess.PushReceivedPDU conn1.Value tmfpdu
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq1.Count ))
        Assert.True(( 0 = cnt ))

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
        Assert.True(( rejectpdu.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( rejectpdu.StatSN = statsn_me.fromPrim 1u ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_031_NonImmidiate_Logout_001() =

        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Logout request PDU
        let logoutpdu = {
            Session_Test.defaultLogoutRequestPDUValues with
                I = false;
                CID = cid_me.fromPrim 1us;
                CmdSN = cmdsn_me.fromPrim 1u;
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
        }

        // Receive Logout Request PDU
        sess.PushReceivedPDU conn1.Value logoutpdu
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq1.Count ))

        Thread.Sleep 10

        // Receive Logout response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.LOGOUT_RES ) )
        let logoutres = pdu2 :?> LogoutResponsePDU
        Assert.True(( logoutres.InitiatorTaskTag = itt_me.fromPrim 1u ))
        Assert.True(( logoutres.ExpCmdSN = cmdsn_me.fromPrim 2u ))
        Assert.True(( logoutres.StatSN = statsn_me.fromPrim 1u ) )

        // Check server connection closed
        try
            sp.ReadByte() |> ignore
            //raise <| IOException( "" )
            Assert.Fail __LINE__
        with
        | :? ObjectDisposedException
        | :? IOException -> 
            ()

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_032_NonImmidiate_Logout_002() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Logout request PDU
        let logoutpdu = {
            Session_Test.defaultLogoutRequestPDUValues with
                I = false;
                CID = cid_me.fromPrim 1us;
                CmdSN = cmdsn_me.zero;   // Error
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
        }

        // Receive Logout Request PDU
        sess.PushReceivedPDU conn1.Value logoutpdu
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq1.Count ))

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
        Assert.True(( rejectpdu.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( rejectpdu.StatSN = statsn_me.fromPrim 1u ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_033_NonImmidiate_ScsiCommand_001() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                CmdSN = cmdsn_me.fromPrim 1u;
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 10u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }

        let mutable cnt = 0
        luStub.p_SCSICommand <-
            ( fun ( source : CommandSourceInfo ) ( command : SCSICommandPDU ) ( data : SCSIDataOutPDU list ) ->
                Assert.True(( source.CID = cid_me.fromPrim 1us ))
                Assert.True(( command.ATTR = TaskATTRCd.SIMPLE_TASK ))
                Assert.True(( command.CmdSN = cmdsn_me.fromPrim 1u ))
                Assert.True(( command.ExpectedDataTransferLength = 10u ))
                Assert.True(( command.BidirectionalExpectedReadDataLength = 0u ))
                Assert.True(( PooledBuffer.ValueEqualsWithArray command.DataSegment ( Array.zeroCreate 10 ) ))
                Assert.True(( command.InitiatorTaskTag = itt_me.fromPrim 1u ))
                Assert.True(( command.LUN = lun_me.zero ))
                Assert.True(( data.Length = 0 ))
                cnt <- 1
            )

        // Send SCSI Command PDU
        sess.PushReceivedPDU conn1.Value scsicmd
        let iwq = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq.Count ))
        Assert.True(( cnt = 1 ))
                
        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_034_NonImmidiate_ScsiCommand_002() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                CmdSN = cmdsn_me.zero;   // error
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 10u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }

        let mutable cnt = 0
        luStub.p_SCSICommand <- ( fun _ _ _ -> cnt <- 1 )

        // Send SCSI Command PDU
        sess.PushReceivedPDU conn1.Value scsicmd
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq1.Count ))

        // Receive Reject response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu = pdu3 :?> RejectPDU
        Assert.True(( rejectpdu.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
        Assert.True(( rejectpdu.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( rejectpdu.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( cnt = 0 ))
                
        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_035() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        let mutable cnt = 0
        luStub.p_SCSICommand <- ( fun _ _ _ -> cnt <- 1 )

        // Nop-Out ( CMDSN=3 )
        let nopoutpdu2 = {
            Session_Test.defaultNopOUTPDUValues with
                I = false;
                CmdSN = cmdsn_me.fromPrim 3u;
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 1u;
        }
        sess.PushReceivedPDU conn1.Value nopoutpdu2
        let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
        Assert.True(( wexpcmdsn = cmdsn_me.fromPrim 1u ))
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // Nop-Out ( CMDSN=2 )
        let nopoutpdu3 = {
            Session_Test.defaultNopOUTPDUValues with
                I = false;
                CmdSN = cmdsn_me.fromPrim 2u;
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 2u;
        }
        sess.PushReceivedPDU conn1.Value nopoutpdu3
        let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
        Assert.True(( wexpcmdsn = cmdsn_me.fromPrim 1u ))

        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 2 = iwq2.Count ))

        // Nop-Out ( Immidiate, CMDSN=3 )
        let nopoutpdu3 = {
            Session_Test.defaultNopOUTPDUValues with
                I = true;
                CmdSN = cmdsn_me.fromPrim 3u;
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 3u;
        }
        sess.PushReceivedPDU conn1.Value nopoutpdu3
        let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
        Assert.True(( wexpcmdsn = cmdsn_me.fromPrim 1u ))

        let iwq3 = Session_Test.GetWaitingQueue pc
        Assert.True(( 2 = iwq3.Count ))

        // receive Nop-In PDU in the initiator ( Immidiate, CMDSN=3 )
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.NOP_IN ) )
        let nopinpdu2 = pdu2 :?> NOPInPDU
        Assert.True( ( nopinpdu2.InitiatorTaskTag = itt_me.fromPrim 3u ) )
        Assert.True( ( nopinpdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ) )
        Assert.True( ( nopinpdu2.StatSN = statsn_me.fromPrim 1u ) )

        // SCSI Command PDU ( CMDSN=1 )
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = false;
                CmdSN = cmdsn_me.fromPrim 1u;
                ExpStatSN = statsn_me.fromPrim 2u;
                InitiatorTaskTag = itt_me.fromPrim 4u;
                ExpectedDataTransferLength = 10u;
                DataSegment = PooledBuffer.Empty;
        }

        sess.PushReceivedPDU conn1.Value scsicmd
        let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
        Assert.True(( wexpcmdsn = cmdsn_me.fromPrim 4u ))
        let iwq4 = Session_Test.GetWaitingQueue pc
        Assert.True(( 3 = iwq4.Count ))
        Assert.True(( 0 = cnt ))

        // SCSI Data-Out PDU
        let datapdu = {
            Session_Test.defaultScsiDataOutPDUValues with
                F = true;
                InitiatorTaskTag = itt_me.fromPrim 4u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                BufferOffset = 0u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        sess.PushReceivedPDU conn1.Value datapdu
        let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
        Assert.True(( wexpcmdsn = cmdsn_me.fromPrim 4u ))
        let iwq5 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq5.Count ))

        // Check runnning SCSI command ( CmdSN = 1 )
        Assert.True(( 1 = cnt ))

        // receive Nop-In PDU in the initiator ( CMDSN=2 )
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.NOP_IN ) )
        let nopinpdu3 = pdu3 :?> NOPInPDU
        Assert.True( ( nopinpdu3.InitiatorTaskTag = itt_me.fromPrim 2u ) )
        Assert.True( ( nopinpdu3.ExpCmdSN = cmdsn_me.fromPrim 4u ) )
        Assert.True( ( nopinpdu3.StatSN = statsn_me.fromPrim 2u ) )

        // receive Nop-In PDU in the initiator ( CMDSN=3 )
        let pdu4 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu4.Opcode = OpcodeCd.NOP_IN ) )
        let nopinpdu4 = pdu4 :?> NOPInPDU
        Assert.True( ( nopinpdu4.InitiatorTaskTag = itt_me.fromPrim 1u ) )
        Assert.True( ( nopinpdu4.ExpCmdSN = cmdsn_me.fromPrim 4u ) )
        Assert.True( ( nopinpdu4.StatSN = statsn_me.fromPrim 3u ) )
                
        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_036() =
        // Create session object
        let sess, pc, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 10u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 10u;
                        MaxRecvDataSegmentLength_T = 10u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        let mutable cnt = 0
        luStub.p_SCSICommand <- ( fun _ _ _ -> cnt <- 1 )

        // Nop-Out ( CMDSN=2 )
        let nopoutpdu2 = {
            Session_Test.defaultNopOUTPDUValues with
                I = false;
                CmdSN = cmdsn_me.fromPrim 2u;
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                PingData = PooledBuffer.Empty;
        }
        sess.PushReceivedPDU conn1.Value nopoutpdu2
        let struct( wexpcmdsn, _ )  = sess.UpdateMaxCmdSN()
        Assert.True(( wexpcmdsn = cmdsn_me.fromPrim 1u ))
        let iwq1 = Session_Test.GetWaitingQueue pc
        Assert.True(( 1 = iwq1.Count ))

        // SCSI Command PDU ( CMDSN=1 )
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = false;
                CmdSN = cmdsn_me.fromPrim 1u;
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 20u;
                DataSegment = PooledBuffer.Empty;
        }

        sess.PushReceivedPDU conn1.Value scsicmd
        let iwq2 = Session_Test.GetWaitingQueue pc
        Assert.True(( 2 = iwq2.Count ))
        Assert.True(( 0 = cnt ))

        // receive R2T PDU in the initiator ( CMDSN=1 )
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.R2T ) )
        let r2tpdu1 = pdu2 :?> R2TPDU
        Assert.True( ( r2tpdu1.InitiatorTaskTag = itt_me.fromPrim 2u ) )
        Assert.True( ( r2tpdu1.TargetTransferTag = ttt_me.fromPrim 0u ) )
        Assert.True( ( r2tpdu1.ExpCmdSN = cmdsn_me.fromPrim 3u ) )
        Assert.True( ( r2tpdu1.StatSN = statsn_me.fromPrim 1u ) )
        Assert.True( ( r2tpdu1.BufferOffset = 0u ) )
        Assert.True( ( r2tpdu1.DesiredDataTransferLength = 10u ) )

        // Nop-Out ( Immidiate, CMDSN=3 )
        let nopoutpdu3 = {
            Session_Test.defaultNopOUTPDUValues with
                I = true;
                CmdSN = cmdsn_me.fromPrim 3u;
                ExpStatSN = statsn_me.fromPrim 1u;
                InitiatorTaskTag = itt_me.fromPrim 3u;
                PingData = PooledBuffer.Empty;
        }
        sess.PushReceivedPDU conn1.Value nopoutpdu3
        let iwq3 = Session_Test.GetWaitingQueue pc
        Assert.True(( 2 = iwq3.Count ))
        Assert.True(( 0 = cnt ))

        // receive Nop-In PDU in the initiator ( Immidiate, CMDSN=3 )
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.NOP_IN ) )
        let nopinpdu2 = pdu3 :?> NOPInPDU
        Assert.True( ( nopinpdu2.InitiatorTaskTag = itt_me.fromPrim 3u ) )
        Assert.True( ( nopinpdu2.ExpCmdSN = cmdsn_me.fromPrim 3u ) )
        Assert.True( ( nopinpdu2.StatSN = statsn_me.fromPrim 1u ) )

        // SCSI Data-Out PDU
        let datapdu1 = {
            Session_Test.defaultScsiDataOutPDUValues with
                F = true;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                ExpStatSN = statsn_me.fromPrim 2u;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        sess.PushReceivedPDU conn1.Value datapdu1
        let iwq4 = Session_Test.GetWaitingQueue pc
        Assert.True(( 2 = iwq4.Count ))
        Assert.True(( 0 = cnt ))

        // receive R2T PDU in the initiator ( CMDSN=1 )
        let pdu4 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu4.Opcode = OpcodeCd.R2T ) )
        let r2tpdu2 = pdu4 :?> R2TPDU
        Assert.True( ( r2tpdu2.InitiatorTaskTag = itt_me.fromPrim 2u ) )
        Assert.True( ( r2tpdu2.TargetTransferTag = ttt_me.fromPrim 1u ) )
        Assert.True( ( r2tpdu2.ExpCmdSN = cmdsn_me.fromPrim 3u ) )
        Assert.True( ( r2tpdu2.StatSN = statsn_me.fromPrim 2u ) )
        Assert.True( ( r2tpdu2.BufferOffset = 10u ) )
        Assert.True( ( r2tpdu2.DesiredDataTransferLength = 10u ) )

        // Nop-Out ( CMDSN=3 )
        let nopoutpdu4 = {
            Session_Test.defaultNopOUTPDUValues with
                I = false;
                CmdSN = cmdsn_me.fromPrim 3u;
                ExpStatSN = statsn_me.fromPrim 2u;
                InitiatorTaskTag = itt_me.fromPrim 5u;
                PingData = PooledBuffer.Empty;
        }
        sess.PushReceivedPDU conn1.Value nopoutpdu4
        let iwq5 = Session_Test.GetWaitingQueue pc
        Assert.True(( 3 = iwq5.Count ))

        // SCSI Data-Out PDU
        let datapdu2 = {
            Session_Test.defaultScsiDataOutPDUValues with
                F = true;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                TargetTransferTag = ttt_me.fromPrim 1u;
                ExpStatSN = statsn_me.fromPrim 2u;
                DataSN = datasn_me.zero;
                BufferOffset = 10u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        sess.PushReceivedPDU conn1.Value datapdu2
        let iwq6 = Session_Test.GetWaitingQueue pc
        Assert.True(( 0 = iwq6.Count ))

        // Check runnning SCSI command ( CmdSN = 1 )
        Assert.True(( 1 = cnt ))

        // receive Nop-In PDU in the initiator ( CMDSN=2 )
        let pdu5 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu5.Opcode = OpcodeCd.NOP_IN ) )
        let nopinpdu3 = pdu5 :?> NOPInPDU
        Assert.True( ( nopinpdu3.InitiatorTaskTag = itt_me.fromPrim 1u ) )
        Assert.True( ( nopinpdu3.ExpCmdSN = cmdsn_me.fromPrim 4u ) )
        Assert.True( ( nopinpdu3.StatSN = statsn_me.fromPrim 2u ) )

        // receive Nop-In PDU in the initiator ( CMDSN=3 )
        let pdu6 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu6.Opcode = OpcodeCd.NOP_IN ) )
        let nopinpdu4 = pdu6 :?> NOPInPDU
        Assert.True( ( nopinpdu4.InitiatorTaskTag = itt_me.fromPrim 5u ) )
        Assert.True( ( nopinpdu4.ExpCmdSN = cmdsn_me.fromPrim 4u ) )
        Assert.True( ( nopinpdu4.StatSN = statsn_me.fromPrim 3u ) )
                
        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_037_SNACK_003_DataR2T() =
        // Create session object
        let sess, _, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        ErrorRecoveryLevel = 1uy;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Send SCSI Response and Data-In PDUs
        sess.SendSCSIResponse {
                Session_Test.defaultScsiCommandPDUValues with
                    I = false;
                    R = true;
                    W = false;
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    ExpectedDataTransferLength = 256u;
            }
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            0u
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 255uy |] )
            256u
            ResponseFenceNeedsFlag.R_Mode

        // Receive Data-In PDUs in the initiator
        for i in [| 0u .. 7u |] do
            let pdu2 =
                PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_DATA_IN ) )
            let pdu3 = pdu2 :?> SCSIDataInPDU
            Assert.True( ( pdu3.DataSN = datasn_me.fromPrim i ) )

        // Receive SCSI Response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )

        // Receive Data/R2T SNACK Request PDU in the target
        sess.PushReceivedPDU conn1.Value {
                Type = SnackReqTypeCd.DATA_R2T;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                ExpStatSN = statsn_me.zero;
                BegRun = 1u;
                RunLength = 2u;
                ByteCount = 0u;
        }

        // Receive Data-In PDUs in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let pdu4 = pdu3 :?> SCSIDataInPDU
        Assert.True( ( pdu4.DataSN = datasn_me.fromPrim 1u ) )

        let pdu5 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu5.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let pdu6 = pdu5 :?> SCSIDataInPDU
        Assert.True( ( pdu6.DataSN = datasn_me.fromPrim 2u ) )

        // Receive Data/R2T SNACK Request PDU in the target
        sess.PushReceivedPDU conn1.Value {
            Type = SnackReqTypeCd.DATA_R2T;
            LUN = lun_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 1u;
            TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            ExpStatSN = statsn_me.zero;
            BegRun = 2u;
            RunLength = 0u;
            ByteCount = 0u;
        }

        // Receive Data-In PDUs in the initiator
        for i in [| 2u .. 7u |] do
            let pdu7 =
                PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( ( pdu7.Opcode = OpcodeCd.SCSI_DATA_IN ) )
            let pdu8 = pdu7 :?> SCSIDataInPDU
            Assert.True( ( pdu8.DataSN = datasn_me.fromPrim i ) )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_038_SNACK_004_DataR2T() =
        // Create session object
        let sess, _, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        ErrorRecoveryLevel = 1uy;
                        MaxOutstandingR2T = 16us;
                        InitialR2T = true;
                        MaxBurstLength = 32u;
                        FirstBurstLength = 32u
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Receive SCSI Command PDU in the target
        sess.PushReceivedPDU conn1.Value {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = false;
                W = true;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 256u;
                CmdSN = cmdsn_me.zero;
                DataSegment = PooledBuffer.Empty;
        }

        // Receive R2T PDUs in the initiator
        for i in [| 0u .. 7u |] do
            let pdu2 =
                PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( ( pdu2.Opcode = OpcodeCd.R2T ) )
            let pdu3 = pdu2 :?> R2TPDU
            Assert.True( ( pdu3.R2TSN = datasn_me.fromPrim i ) )

        // Receive Data/R2T SNACK Request PDU in the target
        sess.PushReceivedPDU conn1.Value {
            Type = SnackReqTypeCd.DATA_R2T;
            LUN = lun_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 1u;
            TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            ExpStatSN = statsn_me.fromPrim 1u;
            BegRun = 1u;
            RunLength = 2u;
            ByteCount = 0u;
        }

        // ReceiveR2T PDUs in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.R2T ) )
        let pdu4 = pdu3 :?> R2TPDU
        Assert.True( ( pdu4.R2TSN = datasn_me.fromPrim 1u ) )

        let pdu5 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu5.Opcode = OpcodeCd.R2T ) )
        let pdu6 = pdu5 :?> R2TPDU
        Assert.True( ( pdu6.R2TSN = datasn_me.fromPrim 2u ) )

        // Receive Data/R2T SNACK Request PDU in the target
        sess.PushReceivedPDU conn1.Value {
            Type = SnackReqTypeCd.DATA_R2T;
            LUN = lun_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 1u;
            TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            ExpStatSN = statsn_me.fromPrim 1u;
            BegRun = 2u;
            RunLength = 0u;
            ByteCount = 0u;
        }

        // Receive R2T PDUs in the initiator
        for i in [| 2u .. 7u |] do
            let pdu7 =
                PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( ( pdu7.Opcode = OpcodeCd.R2T ) )
            let pdu8 = pdu7 :?> R2TPDU
            Assert.True( ( pdu8.R2TSN = datasn_me.fromPrim i ) )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_039_SNACK_005_Status() =
        // Create session object
        let sess, _, _, _, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        ErrorRecoveryLevel = 1uy;
                        MaxOutstandingR2T = 16us;
                        InitialR2T = true;
                        MaxBurstLength = 32u;
                        FirstBurstLength = 32u
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        for i = 1 to 4 do

            luStub.p_AbortTask <- ( fun source itt refitt ->
                Assert.True(( itt = itt_me.fromPrim ( uint32 i ) ))
                Assert.True(( refitt = itt_me.fromPrim 0u ))
                let rpdu = {
                    Response = TaskMgrResCd.FUNCTION_COMPLETE;
                    InitiatorTaskTag = itt;
                    StatSN = statsn_me.zero;
                    ExpCmdSN = cmdsn_me.zero;
                    MaxCmdSN = cmdsn_me.zero;
                    ResponseFence = ResponseFenceNeedsFlag.R_Mode;
                }
                source.ProtocolService.SendOtherResponse source.CID source.ConCounter rpdu lun_me.zero
            )

            // Receive Task Management Request PDU in the target
            sess.PushReceivedPDU conn1.Value {
                Session_Test.defaultTaskManagementRequestPDUValues with
                    I = false;
                    InitiatorTaskTag = itt_me.fromPrim ( uint32 i );
                    ReferencedTaskTag = itt_me.fromPrim 0u;
                    CmdSN = cmdsn_me.fromPrim ( uint32 i );
                    ExpStatSN = statsn_me.fromPrim 1u;
            }

            // Receive Task Management Response PDU in the initiator
            let pdu2 =
                PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )
            let pdu3 = pdu2 :?> TaskManagementFunctionResponsePDU
            Assert.True( ( pdu3.Response = TaskMgrResCd.FUNCTION_COMPLETE ) )
            Assert.True( ( pdu3.StatSN = statsn_me.fromPrim ( uint32 i ) ) )
            Assert.True( ( pdu3.ExpCmdSN = cmdsn_me.fromPrim ( uint32 i + 1u ) ) )

        // Receive status SNACK Request PDU in the target
        sess.PushReceivedPDU conn1.Value {
            Type = SnackReqTypeCd.STATUS;
            LUN = lun_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu;
            TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            ExpStatSN = statsn_me.fromPrim 1u;
            BegRun = 1u;
            RunLength = 2u;
            ByteCount = 0u;
        }

        // Receive Task Management Response PDUs in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )
        let pdu3 = pdu2 :?> TaskManagementFunctionResponsePDU
        Assert.True( ( pdu3.Response = TaskMgrResCd.FUNCTION_COMPLETE ) )
        Assert.True( ( pdu3.StatSN = statsn_me.fromPrim 1u ) )
        Assert.True( ( pdu3.ExpCmdSN = cmdsn_me.fromPrim 5u ) )

        let pdu4 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu4.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )
        let pdu5 = pdu4 :?> TaskManagementFunctionResponsePDU
        Assert.True( ( pdu5.Response = TaskMgrResCd.FUNCTION_COMPLETE ) )
        Assert.True( ( pdu5.StatSN = statsn_me.fromPrim 2u ) )
        Assert.True( ( pdu5.ExpCmdSN = cmdsn_me.fromPrim 5u ) )

        // Receive status SNACK Request PDU in the target
        sess.PushReceivedPDU conn1.Value {
            Type = SnackReqTypeCd.STATUS;
            LUN = lun_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 1u;
            TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            ExpStatSN = statsn_me.fromPrim 1u;
            BegRun = 2u;
            RunLength = 0u;
            ByteCount = 0u;
        }

        // Receive Task Management Response PDUs in the initiator
        for i in [| 2u .. 4u |] do
            let pdu6 =
                PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( ( pdu6.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )
            let pdu7 = pdu6 :?> TaskManagementFunctionResponsePDU
            Assert.True( ( pdu7.Response = TaskMgrResCd.FUNCTION_COMPLETE ) )
            Assert.True( ( pdu7.StatSN = statsn_me.fromPrim ( uint32 i ) ) )
            Assert.True( ( pdu7.ExpCmdSN = cmdsn_me.fromPrim 5u ) )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_040_SNACK_006_DataACK() =
        // Create session object
        let sess, _, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        ErrorRecoveryLevel = 1uy;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Send SCSI Response and Data-In PDUs
        sess.SendSCSIResponse {
                Session_Test.defaultScsiCommandPDUValues with
                    I = false;
                    R = true;
                    W = false;
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    ExpectedDataTransferLength = 256u;
            }
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            0u
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 255uy |] )
            256u
            ResponseFenceNeedsFlag.R_Mode

        // Receive Data-In PDUs in the initiator
        for i in [| 0u .. 6u |] do
            let pdu2 =
                PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_DATA_IN ) )
            let pdu3 = pdu2 :?> SCSIDataInPDU
            Assert.True( ( pdu3.DataSN = datasn_me.fromPrim i ) )

        // Receive last Data-In PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let pdu3 = pdu2 :?> SCSIDataInPDU
        Assert.True( ( pdu3.DataSN = datasn_me.fromPrim 7u ) )
        Assert.True( ( pdu3.F ) )
        Assert.True( ( pdu3.A ) )
        Assert.True( ( pdu3.LUN = lun_me.zero ) )
        Assert.True( ( pdu3.TargetTransferTag = ttt_me.fromPrim 1u ) )

        let receivedTTT = pdu3.TargetTransferTag

        // Receive SCSI Response PDU in the initiator
        let pdu4 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu4.Opcode = OpcodeCd.SCSI_RES ) )

        // Receive DataACK SNACK Request PDU in the target
        // (Acknowledge 0 to 3 Data-In PDUs)
        sess.PushReceivedPDU conn1.Value {
            Type = SnackReqTypeCd.DATA_ACK;
            LUN = lun_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu;
            TargetTransferTag = receivedTTT;
            ExpStatSN = statsn_me.zero;
            BegRun = 4u;
            RunLength = 0u;
            ByteCount = 0u;
        }

        // Receive Data/R2T SNACK Request PDU in the target
        // (Request to resend all of Data-In PDUs)
        sess.PushReceivedPDU conn1.Value {
            Type = SnackReqTypeCd.DATA_R2T;
            LUN = lun_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 1u;
            TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            ExpStatSN = statsn_me.zero;
            BegRun = 0u;
            RunLength = 0u;
            ByteCount = 0u;
        }

        // Receive Data-In PDUs in the initiator
        for i in [| 4u .. 7u |] do
            let pdu7 =
                PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( ( pdu7.Opcode = OpcodeCd.SCSI_DATA_IN ) )
            let pdu8 = pdu7 :?> SCSIDataInPDU
            Assert.True( ( pdu8.DataSN = datasn_me.fromPrim i ) )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_041_SNACK_007_RData() =
        // Create session object
        let sess, _, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        ErrorRecoveryLevel = 1uy;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Send SCSI Response and Data-In PDUs
        sess.SendSCSIResponse {
                Session_Test.defaultScsiCommandPDUValues with
                    I = false;
                    R = true;
                    W = false;
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    ExpectedDataTransferLength = 256u;
            }
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            0u
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 255uy |] )
            256u
            ResponseFenceNeedsFlag.W_Mode

        // Receive Data-In PDUs in the initiator
        for i in [| 0u .. 7u |] do
            let pdu2 =
                PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_DATA_IN ) )
            let pdu3 = pdu2 :?> SCSIDataInPDU
            let arDataSegment3 = pdu3.DataSegment
            Assert.True( ( pdu3.DataSN = datasn_me.fromPrim i ) )
            Assert.True( ( arDataSegment3.Count = 32 ) )

        // Receive SCSI Response PDU in the initiator
        let pdu4 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu4.Opcode = OpcodeCd.SCSI_RES ) )

        let conn = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )
        Assert.True(( conn.IsSome ))

        // Update initiator's MaxRecvDataSegmentLength value
        conn.Value.NotifyUpdateConnectionParameter {
            Session_Test.defaultConnectionParam with
                MaxRecvDataSegmentLength_I = 64u;
        }

        // Receive R-Data SNACK Request PDU in the target
        sess.PushReceivedPDU conn1.Value {
            Type = SnackReqTypeCd.RDATA_SNACK;
            LUN = lun_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 1u;
            TargetTransferTag = ttt_me.fromPrim 0xEFEFEFEFu;
            ExpStatSN = statsn_me.zero;
            BegRun = 0u;
            RunLength = 0u;
            ByteCount = 0u;
        }

        // Receive Data-In PDUs in the initiator
        for i in [| 0u .. 3u |] do
            let pdu7 =
                PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( ( pdu7.Opcode = OpcodeCd.SCSI_DATA_IN ) )
            let pdu8 = pdu7 :?> SCSIDataInPDU
            let arDataSegment8 = pdu8.DataSegment
            Assert.True( ( pdu8.DataSN = datasn_me.fromPrim i ) )
            Assert.True( ( arDataSegment8.Count = 64 ) )

        // Receive SCSI Response PDU in the initiator
        let pdu9 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu9.Opcode = OpcodeCd.SCSI_RES ) )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_042_SNACK_008_RData() =
        // Create session object
        let sess, _, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        ErrorRecoveryLevel = 1uy;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Send Nop-In PDU( R-Mode Lock )
        sess.SendOtherResponsePDU 
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            {
                Session_Test.defaultNopInPDUValues with
                    LUN = lun_me.zero;
                    InitiatorTaskTag = itt_me.fromPrim 1u;
            }

        // Receive Nop-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.NOP_IN ) )
        let pdu2 = pdu1 :?> NOPInPDU
        Assert.True( ( pdu2.StatSN = statsn_me.fromPrim 1u ) )

        // Send SCSI Response and Data-In PDUs ( W-Mode lock )
        sess.SendSCSIResponse {
                Session_Test.defaultScsiCommandPDUValues with
                    I = false;
                    R = true;
                    W = false;
                    InitiatorTaskTag = itt_me.fromPrim 2u;
                    ExpectedDataTransferLength = 256u;
            }
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            0u
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 255uy |] )
            256u
            ResponseFenceNeedsFlag.W_Mode

        // Receive Data-In PDUs in the initiator
        for i in [| 0u .. 7u |] do
            let pdu3 =
                PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( ( pdu3.Opcode = OpcodeCd.SCSI_DATA_IN ) )
            let pdu4 = pdu3 :?> SCSIDataInPDU
            let arDataSegment4 = pdu4.DataSegment
            Assert.True( ( pdu4.DataSN = datasn_me.fromPrim i ) )
            Assert.True( ( arDataSegment4.Count = 32 ) )

        let conn = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )
        Assert.True(( conn.IsSome ))

        // Update initiator's MaxRecvDataSegmentLength value
        conn.Value.NotifyUpdateConnectionParameter {
            Session_Test.defaultConnectionParam with
                MaxRecvDataSegmentLength_I = 64u;
        }

        // Receive R-Data SNACK Request PDU in the target
        sess.PushReceivedPDU conn1.Value {
            Type = SnackReqTypeCd.RDATA_SNACK;
            LUN = lun_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 2u;
            TargetTransferTag = ttt_me.fromPrim 0xEFEFEFEFu;
            ExpStatSN = statsn_me.fromPrim 1u;
            BegRun = 0u;
            RunLength = 0u;
            ByteCount = 0u;
        }

        // Receive Nop-Out PDU in the target ( unlock R-Mode lock, and lock W-Mode lock and send SCSI Response )
        PDU.SendPDU( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objidx_me.NewID(), cp, {
            Session_Test.defaultNopOUTPDUValues with
                I = false;
                LUN = lun_me.fromPrim 0x0UL
                InitiatorTaskTag = itt_me.fromPrim 3u;
                CmdSN = cmdsn_me.fromPrim 1u;
                ExpStatSN = statsn_me.fromPrim 2u;
        } )
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        // Receive Data-In PDUs in the initiator
        for i in [| 0u .. 3u |] do
            let pdu5 =
                PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            pdu5 |> ignore
            Assert.True( ( pdu5.Opcode = OpcodeCd.SCSI_DATA_IN ) )
            let pdu6 = pdu5 :?> SCSIDataInPDU
            let arDataSegment6 = pdu6.DataSegment
            Assert.True( ( pdu6.DataSN = datasn_me.fromPrim i ) )
            Assert.True( ( arDataSegment6.Count = 64 ) )

        // Receive SCSI Response PDU in the initiator
        let pdu7 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu7.Opcode = OpcodeCd.SCSI_RES ) )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.PushReceivedPDU_043_LoginRequest_001() =
        // Create session object
        let sess, _, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                true
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )

        // Receive Login Request PDU in the target
        sess.PushReceivedPDU conn1.Value {
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

        // Receive Reject PDU in the initiator
        let pdu5 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True(( pdu5.Opcode = OpcodeCd.REJECT ))
        let pdu6 = pdu5 :?> RejectPDU
        Assert.True(( pdu6.Reason = RejectReasonCd.PROTOCOL_ERR ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_001() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = {
            Session_Test.defaultSessionParam with
                InitialR2T = true;
                MaxBurstLength = 32u;
        }
        let conParam = {
            Session_Test.defaultConnectionParam with
                MaxRecvDataSegmentLength_I = 32u;
                MaxRecvDataSegmentLength_T = 32u;
        }
        let killer = new HKiller()
        let smStub, luStub = Session_Test.GenDefaultStubs()
        let sp, cp = GlbFunc.GetNetConn()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession
        let pc = new PrivateCaller( sess )

        let mutable cnt = 0
        luStub.p_SCSICommand <- ( fun source cmdPDU dataPDUs ->
            cnt <- 1
            Assert.True(( source.CID = cid_me.fromPrim 1us ))
            Assert.True(( cmdPDU.InitiatorTaskTag = itt_me.fromPrim 2u ))
            Assert.True(( dataPDUs.Length = 0 ))

            source.ProtocolService.SendSCSIResponse
                cmdPDU
                ( cid_me.fromPrim 1us )
                ( concnt_me.fromPrim 1 )
                15u
                iScsiSvcRespCd.COMMAND_COMPLETE
                ScsiCmdStatCd.GOOD
                PooledBuffer.Empty
                ( PooledBuffer.RentAndInit 10 )      // over
                256u
                ResponseFenceNeedsFlag.R_Mode
        )

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = true;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 10u;
                DataSegment = PooledBuffer.RentAndInit 15;       // over
                BidirectionalExpectedReadDataLength = 9u;
        }

        // Add test connection to session object in the target
        let r1 = sess.AddNewConnection sp DateTime.UtcNow ( cid_me.fromPrim 1us ) netportidx_me.zero tpgt_me.zero conParam
        Assert.True r1
        Assert.True( sess.IsExistCID( cid_me.fromPrim 1us ) )
        let conn1 = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )
        sess.PushReceivedPDU conn1.Value scsicmd

        // receive Data-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datain1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment1 = datain1.DataSegment

        Assert.True( datain1.F )
        Assert.False( datain1.S )
        Assert.True( datain1.LUN = lun_me.zero )
        Assert.True( datain1.InitiatorTaskTag = itt_me.fromPrim 2u )
        Assert.True( datain1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu )
        Assert.True( datain1.StatSN = statsn_me.zero )
        Assert.True( datain1.ExpCmdSN = cmdsn_me.fromPrim 1u )
        Assert.True( datain1.DataSN = datasn_me.zero )
        Assert.True( datain1.BufferOffset = 0u )
        Assert.True( arDataSegment1.ToArray() = ( Array.zeroCreate<byte> 9 ) )

        // receive SCSI response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu1 = pdu2 :?> SCSIResponsePDU
        let arSenseData1 = scsirespdu1.SenseData
        let arResponseData1 = scsirespdu1.ResponseData

        Assert.True( scsirespdu1.o )
        Assert.False( scsirespdu1.u )
        Assert.True( scsirespdu1.O )
        Assert.False( scsirespdu1.U )
        Assert.True(( scsirespdu1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu1.Status = ScsiCmdStatCd.GOOD ))
        Assert.True(( scsirespdu1.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu1.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu1.StatSN = statsn_me.zero ))
        Assert.True(( scsirespdu1.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu1.ExpDataSN = datasn_me.fromPrim 1u ))
        Assert.True(( scsirespdu1.BidirectionalReadResidualCount = 1u ))
        Assert.True(( scsirespdu1.ResidualCount = 5u ))
        Assert.True(( scsirespdu1.SenseLength = 0us ))
        Assert.True(( arSenseData1.ToArray() = Array.empty ))
        Assert.True(( arResponseData1.ToArray() = Array.empty ))

        Assert.True(( cnt = 1 ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_002() =
        // Create session object
        let sess, _, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 32u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = true;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 20u;
                DataSegment = PooledBuffer.RentAndInit 19;   // underflow
                BidirectionalExpectedReadDataLength = 15u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            ( PooledBuffer.RentAndInit 10 )   // underflow
            256u
            ResponseFenceNeedsFlag.R_Mode

        // receive Data-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datain1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment1 = datain1.DataSegment

        Assert.True( datain1.F )
        Assert.False( datain1.S )
        Assert.True( datain1.LUN = lun_me.zero )
        Assert.True( datain1.InitiatorTaskTag = itt_me.fromPrim 2u )
        Assert.True( datain1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu )
        Assert.True( datain1.StatSN = statsn_me.zero )
        Assert.True( datain1.ExpCmdSN = cmdsn_me.fromPrim 1u )
        Assert.True( datain1.DataSN = datasn_me.zero )
        Assert.True( datain1.BufferOffset = 0u )
        Assert.True( arDataSegment1.ToArray() = Array.zeroCreate( 10 ) )

        // receive SCSI response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu2 = pdu2 :?> SCSIResponsePDU
        let arSenseData2 =  scsirespdu2.SenseData
        let arResponseData = scsirespdu2.ResponseData

        Assert.False( scsirespdu2.o )
        Assert.True( scsirespdu2.u )
        Assert.False( scsirespdu2.O )
        Assert.True( scsirespdu2.U )
        Assert.True(( scsirespdu2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu2.Status = ScsiCmdStatCd.GOOD ))
        Assert.True(( scsirespdu2.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu2.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu2.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpDataSN = datasn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.BidirectionalReadResidualCount = 5u ))
        Assert.True(( scsirespdu2.ResidualCount = 1u ))
        Assert.True(( scsirespdu2.SenseLength = 0us ))
        Assert.True(( arSenseData2.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_003() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 32u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 15u;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            ( PooledBuffer.RentAndInit 20 )   // overflow
            256u
            ResponseFenceNeedsFlag.R_Mode

        // receive Data-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datain1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment = datain1.DataSegment

        Assert.True( datain1.F )
        Assert.False( datain1.S )
        Assert.True( datain1.LUN = lun_me.zero )
        Assert.True( datain1.InitiatorTaskTag = itt_me.fromPrim 2u )
        Assert.True( datain1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu )
        Assert.True( datain1.StatSN = statsn_me.zero )
        Assert.True( datain1.ExpCmdSN = cmdsn_me.fromPrim 1u )
        Assert.True( datain1.DataSN = datasn_me.zero )
        Assert.True( datain1.BufferOffset = 0u )
        Assert.True( arDataSegment.ToArray() = ( Array.zeroCreate<byte> 15 ) )

        // receive SCSI response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu2 = pdu2 :?> SCSIResponsePDU
        let arSenseData2 = scsirespdu2.SenseData
        let arResponseData = scsirespdu2.ResponseData

        Assert.False( scsirespdu2.o )
        Assert.False( scsirespdu2.u )
        Assert.True( scsirespdu2.O )
        Assert.False( scsirespdu2.U )
        Assert.True(( scsirespdu2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu2.Status = ScsiCmdStatCd.GOOD ))
        Assert.True(( scsirespdu2.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu2.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu2.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpDataSN = datasn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu2.ResidualCount = 5u ))
        Assert.True(( scsirespdu2.SenseLength = 0us ))
        Assert.True(( arSenseData2.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_004() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 32u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 16u;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            ( PooledBuffer.RentAndInit 10 )   // underflow
            256u
            ResponseFenceNeedsFlag.R_Mode

        // receive Data-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datain1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment = datain1.DataSegment

        Assert.True( datain1.F )
        Assert.False( datain1.S )
        Assert.True( datain1.LUN = lun_me.zero )
        Assert.True( datain1.InitiatorTaskTag = itt_me.fromPrim 2u )
        Assert.True( datain1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu )
        Assert.True( datain1.StatSN = statsn_me.zero )
        Assert.True( datain1.ExpCmdSN = cmdsn_me.fromPrim 1u )
        Assert.True( datain1.DataSN = datasn_me.zero )
        Assert.True( datain1.BufferOffset = 0u )
        Assert.True( arDataSegment.ToArray() = Array.zeroCreate( 10 ) )

        // receive SCSI response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu2 = pdu2 :?> SCSIResponsePDU
        let arSenseData = scsirespdu2.SenseData
        let arResponseData = scsirespdu2.ResponseData

        Assert.False( scsirespdu2.o )
        Assert.False( scsirespdu2.u )
        Assert.False( scsirespdu2.O )
        Assert.True( scsirespdu2.U )
        Assert.True(( scsirespdu2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu2.Status = ScsiCmdStatCd.GOOD ))
        Assert.True(( scsirespdu2.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu2.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu2.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpDataSN = datasn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu2.ResidualCount = 6u ))
        Assert.True(( scsirespdu2.SenseLength = 0us ))
        Assert.True(( arSenseData.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_005() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 32u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = false;
                W = true;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 16u;
                DataSegment = PooledBuffer.RentAndInit 18;       // overflow
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            PooledBuffer.Empty
            256u
            ResponseFenceNeedsFlag.R_Mode

        // receive SCSI response PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu1 = pdu1 :?> SCSIResponsePDU
        let arSenseData = scsirespdu1.SenseData
        let arResponseData = scsirespdu1.ResponseData

        Assert.False( scsirespdu1.o )
        Assert.False( scsirespdu1.u )
        Assert.True( scsirespdu1.O )
        Assert.False( scsirespdu1.U )
        Assert.True(( scsirespdu1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu1.Status = ScsiCmdStatCd.GOOD ))
        Assert.True(( scsirespdu1.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu1.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu1.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu1.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu1.ExpDataSN = datasn_me.zero ))
        Assert.True(( scsirespdu1.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu1.ResidualCount = 2u ))
        Assert.True(( scsirespdu1.SenseLength = 0us ))
        Assert.True(( arSenseData.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_006() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 32u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = false;
                W = true;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 16u;
                DataSegment = PooledBuffer.RentAndInit 13;       // underflow
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            PooledBuffer.Empty
            256u
            ResponseFenceNeedsFlag.R_Mode

        // receive SCSI response PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu1 = pdu1 :?> SCSIResponsePDU
        let arSenseData = scsirespdu1.SenseData
        let arResponseData = scsirespdu1.ResponseData

        Assert.False( scsirespdu1.o )
        Assert.False( scsirespdu1.u )
        Assert.False( scsirespdu1.O )
        Assert.True( scsirespdu1.U )
        Assert.True(( scsirespdu1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu1.Status = ScsiCmdStatCd.GOOD ))
        Assert.True(( scsirespdu1.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu1.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu1.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu1.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu1.ExpDataSN = datasn_me.zero ))
        Assert.True(( scsirespdu1.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu1.ResidualCount = 3u ))
        Assert.True(( scsirespdu1.SenseLength = 0us ))
        Assert.True(( arSenseData.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_007() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 32u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 16u;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            PooledBuffer.Empty        // zero
            256u
            ResponseFenceNeedsFlag.R_Mode

        // receive SCSI response PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu1 = pdu1 :?> SCSIResponsePDU
        let arSenseData = scsirespdu1.SenseData
        let arResponseData = scsirespdu1.ResponseData

        Assert.False( scsirespdu1.o )
        Assert.False( scsirespdu1.u )
        Assert.False( scsirespdu1.O )
        Assert.True( scsirespdu1.U )
        Assert.True(( scsirespdu1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu1.Status = ScsiCmdStatCd.GOOD ))
        Assert.True(( scsirespdu1.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu1.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu1.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu1.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu1.ExpDataSN = datasn_me.zero ))
        Assert.True(( scsirespdu1.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu1.ResidualCount = 16u ))
        Assert.True(( scsirespdu1.SenseLength = 0us ))
        Assert.True(( arSenseData.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_008() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 32u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 256u;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            ( PooledBuffer.Rent [| 0uy .. 29uy |] )   // all of sense data bytes can be stored in a SCSI Response PDU
            PooledBuffer.Empty
            256u
            ResponseFenceNeedsFlag.R_Mode

        // receive SCSI response PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu1 = pdu1 :?> SCSIResponsePDU
        let arSenseData = scsirespdu1.SenseData
        let arResponseData = scsirespdu1.ResponseData

        Assert.False( scsirespdu1.o )
        Assert.False( scsirespdu1.u )
        Assert.False( scsirespdu1.O )
        Assert.True( scsirespdu1.U )
        Assert.True(( scsirespdu1.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
        Assert.True(( scsirespdu1.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu1.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu1.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu1.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu1.ExpDataSN = datasn_me.zero ))
        Assert.True(( scsirespdu1.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu1.ResidualCount = 226u ))
        Assert.True(( scsirespdu1.SenseLength = 30us ))
        Assert.True(( arSenseData.ToArray() = [| 0uy .. 29uy |] ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_009() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 64u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 256u;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            ( PooledBuffer.Rent [| 0uy .. 30uy |] )   // all of sense data bytes can not be stored in a SCSI response PDU.
            PooledBuffer.Empty
            256u
            ResponseFenceNeedsFlag.R_Mode

        // receive SCSI response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu2 = pdu2 :?> SCSIResponsePDU
        let arSenseData = scsirespdu2.SenseData
        let arResponseData = scsirespdu2.ResponseData

        Assert.False( scsirespdu2.o )
        Assert.False( scsirespdu2.u )
        Assert.False( scsirespdu2.O )
        Assert.True( scsirespdu2.U )
        Assert.True(( scsirespdu2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu2.Status = ScsiCmdStatCd.CHECK_CONDITION ))
        Assert.True(( scsirespdu2.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu2.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu2.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpDataSN = datasn_me.zero ))
        Assert.True(( scsirespdu2.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu2.ResidualCount = 225u ))     // This value does not reflect truncated at the iSCSI layer.
        Assert.True(( scsirespdu2.SenseLength = 30us ))
        Assert.True(( arSenseData.ToArray() = [| 0uy .. 29uy |] ))  // truncated
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_010() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 64u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 256u;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 29uy |] )   // all of response data bytes can be stored in a SCSI response PDU.
            256u
            ResponseFenceNeedsFlag.R_Mode

        // receive Data-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datain1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment = datain1.DataSegment

        Assert.True( datain1.F )
        Assert.False( datain1.S )
        Assert.True( datain1.LUN = lun_me.zero )
        Assert.True( datain1.InitiatorTaskTag = itt_me.fromPrim 2u )
        Assert.True( datain1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu )
        Assert.True( datain1.StatSN = statsn_me.zero )
        Assert.True( datain1.ExpCmdSN = cmdsn_me.fromPrim 1u )
        Assert.True( datain1.DataSN = datasn_me.zero )
        Assert.True( datain1.BufferOffset = 0u )
        Assert.True( arDataSegment.ToArray() = [| 0uy .. 29uy |] )

        // receive SCSI response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu2 = pdu2 :?> SCSIResponsePDU
        let arSenseData = scsirespdu2.SenseData
        let arResponseData = scsirespdu2.ResponseData

        Assert.False( scsirespdu2.o )
        Assert.False( scsirespdu2.u )
        Assert.False( scsirespdu2.O )
        Assert.True( scsirespdu2.U )
        Assert.True(( scsirespdu2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu2.Status = ScsiCmdStatCd.CHECK_CONDITION ))
        Assert.True(( scsirespdu2.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu2.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu2.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpDataSN = datasn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu2.ResidualCount = 226u ))
        Assert.True(( scsirespdu2.SenseLength = 0us ))
        Assert.True(( arSenseData.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_011() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 64u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 256u;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 30uy |] )   // all of response data bytes can not be stored in a SCSI response PDU.
            256u
            ResponseFenceNeedsFlag.R_Mode

        // receive Data-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datainpdu1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment = datainpdu1.DataSegment

        Assert.True( datainpdu1.F )
        Assert.False( datainpdu1.A )
        Assert.False( datainpdu1.O )
        Assert.False( datainpdu1.U )
        Assert.False( datainpdu1.S )
        Assert.True(( datainpdu1.Status = ScsiCmdStatCd.GOOD ))
        Assert.True(( datainpdu1.LUN = lun_me.zero ))
        Assert.True(( datainpdu1.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( datainpdu1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu ))
        Assert.True(( datainpdu1.StatSN = statsn_me.zero ))
        Assert.True(( datainpdu1.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( datainpdu1.DataSN = datasn_me.zero ))
        Assert.True(( datainpdu1.BufferOffset = 0u ))
        Assert.True(( datainpdu1.ResidualCount = 0u ))
        Assert.True(( arDataSegment.ToArray() = [| 0uy .. 30uy |] ))

        // receive SCSI response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu2 = pdu2 :?> SCSIResponsePDU
        let arSenseData = scsirespdu2.SenseData
        let arResponseData = scsirespdu2.ResponseData

        Assert.False( scsirespdu2.o )
        Assert.False( scsirespdu2.u )
        Assert.False( scsirespdu2.O )
        Assert.True( scsirespdu2.U )
        Assert.True(( scsirespdu2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu2.Status = ScsiCmdStatCd.CHECK_CONDITION ))
        Assert.True(( scsirespdu2.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu2.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu2.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpDataSN = datasn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu2.ResidualCount = 225u ))
        Assert.True(( scsirespdu2.SenseLength = 0us ))
        Assert.True(( arSenseData.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_012() =
        // Create session object
        let sess, _, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 64u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 256u;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 31uy |] )   // all of response data bytes can be stored in a SCSI Data-In PDU.
            256u
            ResponseFenceNeedsFlag.R_Mode

        // receive Data-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datainpdu1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment = datainpdu1.DataSegment

        Assert.True( datainpdu1.F )
        Assert.False( datainpdu1.A )
        Assert.False( datainpdu1.O )
        Assert.False( datainpdu1.U )
        Assert.False( datainpdu1.S )
        Assert.True(( datainpdu1.Status = ScsiCmdStatCd.GOOD ))
        Assert.True(( datainpdu1.LUN = lun_me.zero ))
        Assert.True(( datainpdu1.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( datainpdu1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu ))
        Assert.True(( datainpdu1.StatSN = statsn_me.zero ))
        Assert.True(( datainpdu1.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( datainpdu1.DataSN = datasn_me.zero ))
        Assert.True(( datainpdu1.BufferOffset = 0u ))
        Assert.True(( datainpdu1.ResidualCount = 0u ))
        Assert.True(( arDataSegment.ToArray() = [| 0uy .. 31uy |] ))

        // receive SCSI response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu2 = pdu2 :?> SCSIResponsePDU
        let arSenseData = scsirespdu2.SenseData
        let arResponseData = scsirespdu2.ResponseData

        Assert.False( scsirespdu2.o )
        Assert.False( scsirespdu2.u )
        Assert.False( scsirespdu2.O )
        Assert.True( scsirespdu2.U )
        Assert.True(( scsirespdu2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu2.Status = ScsiCmdStatCd.CHECK_CONDITION ))
        Assert.True(( scsirespdu2.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu2.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu2.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.ExpDataSN = datasn_me.fromPrim 1u ))
        Assert.True(( scsirespdu2.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu2.ResidualCount = 224u ))
        Assert.True(( scsirespdu2.SenseLength = 0us ))
        Assert.True(( arSenseData.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_013() =
        // Create session object
        let sess, _, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 64u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 256u;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 32uy |] )   // all of response data bytes can not be stored in a SCSI Data-In PDU.
            256u
            ResponseFenceNeedsFlag.R_Mode

        // receive Data-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datainpdu1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment = datainpdu1.DataSegment

        Assert.False( datainpdu1.F )
        Assert.False( datainpdu1.A )
        Assert.False( datainpdu1.O )
        Assert.False( datainpdu1.U )
        Assert.False( datainpdu1.S )
        Assert.True(( datainpdu1.Status = ScsiCmdStatCd.GOOD ))
        Assert.True(( datainpdu1.LUN = lun_me.zero ))
        Assert.True(( datainpdu1.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( datainpdu1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu ))
        Assert.True(( datainpdu1.StatSN = statsn_me.zero ))
        Assert.True(( datainpdu1.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( datainpdu1.DataSN = datasn_me.zero ))
        Assert.True(( datainpdu1.BufferOffset = 0u ))
        Assert.True(( datainpdu1.ResidualCount = 0u ))
        Assert.True(( arDataSegment.ToArray() = [| 0uy .. 31uy |] ))

       // receive Data-In PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datainpdu2 = pdu2 :?> SCSIDataInPDU
        let arDataSegment = datainpdu2.DataSegment

        Assert.True( datainpdu2.F )
        Assert.False( datainpdu2.A )
        Assert.False( datainpdu2.O )
        Assert.False( datainpdu2.U )
        Assert.False( datainpdu2.S )
        Assert.True(( datainpdu2.Status = ScsiCmdStatCd.GOOD ))
        Assert.True(( datainpdu2.LUN = lun_me.zero ))
        Assert.True(( datainpdu2.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( datainpdu2.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu ))
        Assert.True(( datainpdu2.StatSN = statsn_me.zero ))
        Assert.True(( datainpdu2.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( datainpdu2.DataSN = datasn_me.fromPrim 1u ))
        Assert.True(( datainpdu2.BufferOffset = 32u ))
        Assert.True(( datainpdu2.ResidualCount = 0u ))
        Assert.True(( arDataSegment.ToArray() = [| 32uy |] ))

        // receive SCSI response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu3 = pdu3 :?> SCSIResponsePDU
        let arSenseData = scsirespdu3.SenseData
        let arResponseData = scsirespdu3.ResponseData

        Assert.False( scsirespdu3.o )
        Assert.False( scsirespdu3.u )
        Assert.False( scsirespdu3.O )
        Assert.True( scsirespdu3.U )
        Assert.True(( scsirespdu3.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu3.Status = ScsiCmdStatCd.CHECK_CONDITION ))
        Assert.True(( scsirespdu3.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu3.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu3.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.ExpDataSN = datasn_me.fromPrim 2u ))
        Assert.True(( scsirespdu3.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu3.ResidualCount = 223u ))
        Assert.True(( scsirespdu3.SenseLength = 0us ))
        Assert.True(( arSenseData.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_014() =
        // Create session object
        let sess, _, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 64u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 256u;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 31uy |] )   // truncated by allocation length value(10)
            10u                // TOO SHORT
            ResponseFenceNeedsFlag.R_Mode

        // ******************************************************************************
        // Originally, the length of the response data or sense data must be less than or equal to Alllocation Length.
        // If response data is truncated by alocation length, ResidualCount is set to an incorrect value.
        // ******************************************************************************

        // receive Data-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datain1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment = datain1.DataSegment

        Assert.True( datain1.F )
        Assert.False( datain1.S )
        Assert.True( datain1.LUN = lun_me.zero )
        Assert.True( datain1.InitiatorTaskTag = itt_me.fromPrim 2u )
        Assert.True( datain1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu )
        Assert.True( datain1.StatSN = statsn_me.zero )
        Assert.True( datain1.ExpCmdSN = cmdsn_me.fromPrim 1u )
        Assert.True( datain1.DataSN = datasn_me.zero )
        Assert.True( datain1.BufferOffset = 0u )
        Assert.True( arDataSegment.ToArray() = [| 0uy .. 9uy |] )    // truncated by allocation length


        // receive SCSI response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu3 = pdu3 :?> SCSIResponsePDU
        let arSenseData = scsirespdu3.SenseData
        let arResponseData = scsirespdu3.ResponseData

        Assert.False( scsirespdu3.o )
        Assert.False( scsirespdu3.u )
        Assert.False( scsirespdu3.O )
        Assert.True( scsirespdu3.U )
        Assert.True(( scsirespdu3.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu3.Status = ScsiCmdStatCd.CHECK_CONDITION ))
        Assert.True(( scsirespdu3.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu3.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu3.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.ExpDataSN = datasn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu3.ResidualCount = 224u ))
        Assert.True(( scsirespdu3.SenseLength = 0us ))
        Assert.True(( arSenseData.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_015() =
        // Create session object
        let sess, pc, _, _, _, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 16u;   // TOO SHORT
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 32u;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 31uy |] )   // truncated by MaxBurstLength length value(16)
            32u
            ResponseFenceNeedsFlag.R_Mode

        // receive Data-In PDU 1 in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datain1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment = datain1.DataSegment

        Assert.True( datain1.F )
        Assert.False( datain1.S )
        Assert.True( datain1.LUN = lun_me.zero )
        Assert.True( datain1.InitiatorTaskTag = itt_me.fromPrim 2u )
        Assert.True( datain1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu )
        Assert.True( datain1.StatSN = statsn_me.zero )
        Assert.True( datain1.ExpCmdSN = cmdsn_me.fromPrim 1u )
        Assert.True( datain1.DataSN = datasn_me.zero )
        Assert.True( datain1.BufferOffset = 0u )
        Assert.True( arDataSegment.ToArray() = [| 0uy .. 15uy |] )

        // receive Data-In PDU 2 in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datain1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment = datain1.DataSegment

        Assert.True( datain1.F )
        Assert.False( datain1.S )
        Assert.True( datain1.LUN = lun_me.zero )
        Assert.True( datain1.InitiatorTaskTag = itt_me.fromPrim 2u )
        Assert.True( datain1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu )
        Assert.True( datain1.StatSN = statsn_me.zero )
        Assert.True( datain1.ExpCmdSN = cmdsn_me.fromPrim 1u )
        Assert.True( datain1.DataSN = datasn_me.fromPrim 1u )
        Assert.True( datain1.BufferOffset = 16u )
        Assert.True( arDataSegment.ToArray() = [| 16uy .. 31uy |] )

        // receive SCSI response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu3 = pdu3 :?> SCSIResponsePDU
        let arSenseData = scsirespdu3.SenseData
        let arResponseData = scsirespdu3.ResponseData

        Assert.False( scsirespdu3.o )
        Assert.False( scsirespdu3.u )
        Assert.False( scsirespdu3.O )
        Assert.False( scsirespdu3.U )
        Assert.True(( scsirespdu3.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu3.Status = ScsiCmdStatCd.CHECK_CONDITION ))
        Assert.True(( scsirespdu3.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu3.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu3.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.ExpDataSN = datasn_me.fromPrim 2u ))
        Assert.True(( scsirespdu3.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu3.ResidualCount = 0u ))
        Assert.True(( scsirespdu3.SenseLength = 0us ))
        Assert.True(( arSenseData.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_016() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 64u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 16u;   // TOO SHORT
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 31uy |] )   // truncated by ExpectedDataTransferLength length value(16)
            32u
            ResponseFenceNeedsFlag.R_Mode

        // receive Data-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datain1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment = datain1.DataSegment

        Assert.True( datain1.F )
        Assert.False( datain1.S )
        Assert.True( datain1.LUN = lun_me.zero )
        Assert.True( datain1.InitiatorTaskTag = itt_me.fromPrim 2u )
        Assert.True( datain1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu )
        Assert.True( datain1.StatSN = statsn_me.zero )
        Assert.True( datain1.ExpCmdSN = cmdsn_me.fromPrim 1u )
        Assert.True( datain1.DataSN = datasn_me.zero )
        Assert.True( datain1.BufferOffset = 0u )
        Assert.True( arDataSegment.ToArray() = [| 0uy .. 15uy |] )    // truncated by ExpectedDataTransferLength length

        // receive SCSI response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu3 = pdu3 :?> SCSIResponsePDU
        let arSenseData = scsirespdu3.SenseData
        let arResponseData = scsirespdu3.ResponseData

        Assert.False( scsirespdu3.o )
        Assert.False( scsirespdu3.u )
        Assert.True( scsirespdu3.O )
        Assert.False( scsirespdu3.U )
        Assert.True(( scsirespdu3.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu3.Status = ScsiCmdStatCd.CHECK_CONDITION ))
        Assert.True(( scsirespdu3.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu3.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu3.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.ExpDataSN = datasn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.BidirectionalReadResidualCount = 0u ))
        Assert.True(( scsirespdu3.ResidualCount = 16u ))
        Assert.True(( scsirespdu3.SenseLength = 0us ))
        Assert.True(( arSenseData.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_017() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                {
                    Session_Test.defaultSessionParam with
                        InitialR2T = true;
                        MaxBurstLength = 64u;
                }
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                        MaxRecvDataSegmentLength_T = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = true;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 16u;
                DataSegment = PooledBuffer.Rent [| 0uy .. 15uy |];
                BidirectionalExpectedReadDataLength = 16u;  // TOO SHORT
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 31uy |] )   // truncated by BidirectionalExpectedReadDataLength length value(16)
            32u
            ResponseFenceNeedsFlag.R_Mode

        // receive Data-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously

        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )
        let datain1 = pdu1 :?> SCSIDataInPDU
        let arDataSegment = datain1.DataSegment

        Assert.True( datain1.F )
        Assert.False( datain1.S )
        Assert.True( datain1.LUN = lun_me.zero )
        Assert.True( datain1.InitiatorTaskTag = itt_me.fromPrim 2u )
        Assert.True( datain1.TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu )
        Assert.True( datain1.StatSN = statsn_me.zero )
        Assert.True( datain1.ExpCmdSN = cmdsn_me.fromPrim 1u )
        Assert.True( datain1.DataSN = datasn_me.zero )
        Assert.True( datain1.BufferOffset = 0u )
        Assert.True( arDataSegment.ToArray() = [| 0uy .. 15uy |] )    // truncated by BidirectionalExpectedReadDataLength length

        // receive SCSI response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu3 = pdu3 :?> SCSIResponsePDU
        let arSenseData = scsirespdu3.SenseData
        let arResponseData = scsirespdu3.ResponseData

        Assert.True( scsirespdu3.o )
        Assert.False( scsirespdu3.u )
        Assert.False( scsirespdu3.O )
        Assert.False( scsirespdu3.U )
        Assert.True(( scsirespdu3.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( scsirespdu3.Status = ScsiCmdStatCd.CHECK_CONDITION ))
        Assert.True(( scsirespdu3.InitiatorTaskTag = itt_me.fromPrim 2u ))
        Assert.True(( scsirespdu3.SNACKTag = snacktag_me.zero ))
        Assert.True(( scsirespdu3.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.ExpCmdSN = cmdsn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.ExpDataSN = datasn_me.fromPrim 1u ))
        Assert.True(( scsirespdu3.BidirectionalReadResidualCount = 16u ))
        Assert.True(( scsirespdu3.ResidualCount = 0u ))
        Assert.True(( scsirespdu3.SenseLength = 0us ))
        Assert.True(( arSenseData.ToArray() = Array.empty ))
        Assert.True(( arResponseData.ToArray() = Array.empty ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendSCSIResponse_018() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let conParam = Session_Test.defaultConnectionParam
        let killer = new HKiller()
        let smStub, luStub = Session_Test.GenDefaultStubs()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession
        let pc = new PrivateCaller( sess )
        

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = false;
                W = true;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = 16u;
                DataSegment = PooledBuffer.Rent [| 0uy .. 15uy |];
                BidirectionalExpectedReadDataLength = 0u;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )     // connection missing
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            PooledBuffer.Empty
            32u
            ResponseFenceNeedsFlag.R_Mode

    [<Theory>]
    [<InlineData( 16u, 32, 16u, 32 )>] // Originally, o and O should be 1.
    [<InlineData( 16u, 8, 16u, 8 )>] // Originally, u and U should be 1.
    member _.SendSCSIResponse_019 ( edtl : uint ) ( recvLen : int ) ( berdl : uint )  ( respLen : int ) =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = true;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                ExpectedDataTransferLength = edtl;
                DataSegment = PooledBuffer.RentAndInit recvLen;
                BidirectionalExpectedReadDataLength = berdl;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.TARGET_FAILURE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.RentAndInit respLen )
            32u
            ResponseFenceNeedsFlag.R_Mode

        // receive SCSI response PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.SCSI_RES ) )
        let scsirespdu3 = pdu3 :?> SCSIResponsePDU

        Assert.False( scsirespdu3.o )   // always false
        Assert.False( scsirespdu3.u )   // always false
        Assert.False( scsirespdu3.O )   // always false
        Assert.False( scsirespdu3.U )   // always false
        Assert.True(( scsirespdu3.ResidualCount = 0u ))                     // always zero
        Assert.True(( scsirespdu3.BidirectionalReadResidualCount = 0u ))    // always zero
        Assert.True(( scsirespdu3.SenseData.Count = 0 ))                    // always empty
        Assert.True(( scsirespdu3.ResponseData.Count = 0 ))                 // always empty
        Assert.True(( scsirespdu3.Response = iScsiSvcRespCd.TARGET_FAILURE )) 
        Assert.True(( scsirespdu3.Status = ScsiCmdStatCd.GOOD ))            // always GOOD
        Assert.True(( PooledBuffer.length scsirespdu3.DataInBuffer = 0 ))   // always empty

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.RejectPDUByHeader_001() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        //let conParam = Session_Test.defaultConnectionParam
        let killer = new HKiller()
        let smStub, luStub = Session_Test.GenDefaultStubs()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession
        let pc = new PrivateCaller( sess )

        sess.RejectPDUByHeader
            ( cid_me.fromPrim 1us )     // connection missing
            ( concnt_me.fromPrim 1 )
            [| 0uy .. 47uy |]
            RejectReasonCd.DATA_DIGEST_ERR

    [<Fact>]
    member _.RejectPDUByHeader_002() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // Send reject PDU from the target
        sess.RejectPDUByHeader
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            [| 0uy .. 47uy |]
            RejectReasonCd.DATA_DIGEST_ERR

        // receive Reject PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.REJECT ) )
        let rejectpdu1 = pdu3 :?> RejectPDU

        Assert.True(( rejectpdu1.Reason = RejectReasonCd.DATA_DIGEST_ERR ))
        Assert.True(( rejectpdu1.StatSN = statsn_me.fromPrim 1u ))
        Assert.True(( rejectpdu1.HeaderData = [| 0uy .. 47uy |] ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendOtherResponsePDU_001() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        //let conParam = Session_Test.defaultConnectionParam
        let killer = new HKiller()
        let smStub, luStub = Session_Test.GenDefaultStubs()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        // Send PDU from the target
        sess.SendOtherResponsePDU
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )    // connection missing
            {
                Response = TaskMgrResCd.FUNCTION_COMPLETE;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                StatSN = statsn_me.fromPrim 0x01020304u;
                ExpCmdSN = cmdsn_me.fromPrim 0x01020304u;
                MaxCmdSN = cmdsn_me.fromPrim 0x01020304u;
                ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
            }

    [<Fact>]
    member _.SendOtherResponsePDU_002() =
        let itNexus = new ITNexus( "abcI", isid_me.fromElem 0uy 1uy 2us 3uy 4us, "abcT", tpgt_me.zero )
        let sessParam = Session_Test.defaultSessionParam
        let conParam = Session_Test.defaultConnectionParam
        let killer = new HKiller()
        let smStub, luStub = Session_Test.GenDefaultStubs()

        // Create session object
        let sess = new Session( smStub, DateTime.UtcNow, itNexus, tsih_me.fromPrim 0us, sessParam, cmdsn_me.zero, killer ) :> ISession

        // Send PDU from the target
        sess.SendOtherResponsePDU
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )    // connection missing
            {
                Response = TaskMgrResCd.FUNCTION_COMPLETE;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                StatSN = statsn_me.fromPrim 0x01020304u;
                ExpCmdSN = cmdsn_me.fromPrim 0x01020304u;
                MaxCmdSN = cmdsn_me.fromPrim 0x01020304u;
                ResponseFence = ResponseFenceNeedsFlag.Immediately;
            }

    [<Fact>]
    member _.SendOtherResponsePDU_003() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // Send PDU from the target
        sess.SendOtherResponsePDU
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            {
                Response = TaskMgrResCd.FUNCTION_COMPLETE;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                StatSN = statsn_me.fromPrim 0x01020304u;
                ExpCmdSN = cmdsn_me.fromPrim 0x01020304u;
                MaxCmdSN = cmdsn_me.fromPrim 0x01020304u;
                ResponseFence = ResponseFenceNeedsFlag.Immediately;
            }
 
        // receive PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendOtherResponsePDU_004() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        // Send PDU from the target
        sess.SendOtherResponsePDU
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            {
                Response = TaskMgrResCd.FUNCTION_COMPLETE;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                StatSN = statsn_me.fromPrim 0x01020304u;
                ExpCmdSN = cmdsn_me.fromPrim 0x01020304u;
                MaxCmdSN = cmdsn_me.fromPrim 0x01020304u;
                ResponseFence = ResponseFenceNeedsFlag.R_Mode;
            }
 
        // receive PDU in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.SendOtherResponsePDU_005() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        let respdu1 = {
            Response = TaskMgrResCd.FUNCTION_COMPLETE;
            InitiatorTaskTag = itt_me.fromPrim 1u;
            StatSN = statsn_me.fromPrim 0x01020304u;
            ExpCmdSN = cmdsn_me.fromPrim 0x01020304u;
            MaxCmdSN = cmdsn_me.fromPrim 0x01020304u;
            ResponseFence = ResponseFenceNeedsFlag.R_Mode;
        }
        let respdu2 = {
            Response = TaskMgrResCd.FUNCTION_COMPLETE;
            InitiatorTaskTag = itt_me.fromPrim 2u;
            StatSN = statsn_me.fromPrim 0x01020304u;
            ExpCmdSN = cmdsn_me.fromPrim 0x01020304u;
            MaxCmdSN = cmdsn_me.fromPrim 0x01020304u;
            ResponseFence = ResponseFenceNeedsFlag.W_Mode;
        }

        // Send PDU1 from the target (R_Mode)
        sess.SendOtherResponsePDU ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 ) respdu1

        // receive PDU1 in the initiator
        let pdu3 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu3.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )
        Assert.True( ( pdu3.InitiatorTaskTag = itt_me.fromPrim 1u ) )

        // Send PDU2 from the target (W_Mode)
        sess.SendOtherResponsePDU ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 ) respdu2

        [|
            fun () -> task {
                do! Task.Delay 100
                sess.NoticeUnlockResponseFence ResponseFenceNeedsFlag.R_Mode
            };
            fun () -> task {
                let s1 = Environment.TickCount

                // receive PDU2 in the initiator
                let pdu4 =
                    PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                    |> Functions.RunTaskSynchronously
                Assert.True(( ( Environment.TickCount - s1 ) >= 90 ))
                Assert.True( ( pdu4.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )
                Assert.True( ( pdu4.InitiatorTaskTag = itt_me.fromPrim 2u ) )
                
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore
 
        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResponseFence_001() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        // Set R-Mode lock
        rfl.NormalLock( fun () -> () )

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                DataSegment = PooledBuffer.Empty;
        }

        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( PooledBuffer.ulength scsicmd.DataSegment )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 31uy |] )
            32u
            ResponseFenceNeedsFlag.R_Mode

        // receive Data-In PDU in the initiator
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )

        // receive SCSI response PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResponseFence_002() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        // Set R-Mode lock
        rfl.NormalLock( fun () -> () )

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                DataSegment = PooledBuffer.Empty;
        }

        // Send SCSI Response( Data-In & W_Mode SCSI Responce PDU )
        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( scsicmd.DataSegment |> PooledBuffer.length |> uint )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 31uy |] )
            32u
            ResponseFenceNeedsFlag.W_Mode

        // Data-In PDU is sent in immidiately
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )

        [|
            fun () -> task {
                do! Task.Delay 100
                // Unlock R_Mode lock
                sess.NoticeUnlockResponseFence ResponseFenceNeedsFlag.R_Mode
            };
            fun () -> task {
                let st = Environment.TickCount

                // receive SCSI response PDU in the initiator
                let pdu2 =
                    PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                    |> Functions.RunTaskSynchronously
                Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )

                // SCSI Response PDU is sent after R_Mode lock is unlocked.
                Assert.True(( Environment.TickCount - st > 90 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResponseFence_003() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        // Set W_Mode lock
        rfl.RFLock( fun () -> () )

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                DataSegment = PooledBuffer.Empty;
        }

        // Send SCSI Response( Data-In & W_Mode SCSI Responce PDU )
        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( scsicmd.DataSegment |> PooledBuffer.length |> uint )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 31uy |] )
            32u
            ResponseFenceNeedsFlag.R_Mode

        // Data-In PDU is sent in immidiately
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )

        [|
            fun () -> task {
                do! Task.Delay 100
                // Unlock W_Mode lock
                sess.NoticeUnlockResponseFence ResponseFenceNeedsFlag.W_Mode
            };
            fun () -> task {
                let st = Environment.TickCount

                // receive SCSI response PDU in the initiator
                let pdu2 =
                    PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                    |> Functions.RunTaskSynchronously
                Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )

                // SCSI Response PDU is sent after R_Mode lock is unlocked.
                Assert.True(( Environment.TickCount - st > 90 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResponseFence_004() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        // Set W_Mode lock
        rfl.RFLock( fun () -> () )

        // SCSI Command PDU
        let scsicmd = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = true;
                R = true;
                W = false;
                DataSegment = PooledBuffer.Empty;
        }

        // Send SCSI Response( Data-In & W_Mode SCSI Responce PDU )
        sess.SendSCSIResponse
            scsicmd
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            ( scsicmd.DataSegment |> PooledBuffer.length |> uint )
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.CHECK_CONDITION
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 31uy |] )
            32u
            ResponseFenceNeedsFlag.R_Mode

        // Data-In PDU is sent in immidiately
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_DATA_IN ) )

        [|
            fun () -> task {
                do! Task.Delay 100
                // Unlock W_Mode lock
                sess.NoticeUnlockResponseFence ResponseFenceNeedsFlag.W_Mode
            };
            fun () -> task {
                let st = Environment.TickCount

                // receive SCSI response PDU in the initiator
                let pdu2 =
                    PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                    |> Functions.RunTaskSynchronously
                Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )

                // SCSI Response PDU is sent after R_Mode lock is unlocked.
                Assert.True(( Environment.TickCount - st > 90 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResponseFence_005() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        // Set R_Mode lock
        rfl.NormalLock( fun () -> () )

        // Text Management Response PDU
        let tmrpdu = {
            Response = TaskMgrResCd.AUTH_FAILED;
            InitiatorTaskTag = itt_me.fromPrim 1u;
            StatSN = statsn_me.zero;
            ExpCmdSN = cmdsn_me.zero;
            MaxCmdSN = cmdsn_me.zero
            ResponseFence = ResponseFenceNeedsFlag.R_Mode;
        }

        // Send TMR PDU ( require R_Mode )
        sess.SendOtherResponsePDU
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            tmrpdu

        // R_Mode PDU is sent in immidiately under R_Mode lock
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResponseFence_006() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        // Set R_Mode lock
        rfl.NormalLock( fun () -> () )

        // Text Management Response PDU
        let tmrpdu = {
            Response = TaskMgrResCd.AUTH_FAILED;
            InitiatorTaskTag = itt_me.fromPrim 1u;
            StatSN = statsn_me.zero;
            ExpCmdSN = cmdsn_me.zero;
            MaxCmdSN = cmdsn_me.zero
            ResponseFence = ResponseFenceNeedsFlag.W_Mode;
        }

        // Send TMR PDU ( require W_Mode )
        sess.SendOtherResponsePDU
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            tmrpdu

        [|
            fun () -> task {
                do! Task.Delay 100
                // Unlock R_Mode lock
                sess.NoticeUnlockResponseFence ResponseFenceNeedsFlag.R_Mode
            };
            fun () -> task {
                let st = Environment.TickCount

                // receive TMR PDU in the initiator
                let pdu2 =
                    PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                    |> Functions.RunTaskSynchronously
                Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )

                // TMR PDU is sent after R_Mode lock is unlocked.
                Assert.True(( Environment.TickCount - st > 90 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResponseFence_007() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        // Set W_Mode lock
        rfl.RFLock( fun () -> () )

        // Text Management Response PDU
        let tmrpdu = {
            Response = TaskMgrResCd.AUTH_FAILED;
            InitiatorTaskTag = itt_me.fromPrim 1u;
            StatSN = statsn_me.zero;
            ExpCmdSN = cmdsn_me.zero;
            MaxCmdSN = cmdsn_me.zero
            ResponseFence = ResponseFenceNeedsFlag.R_Mode;
        }

        // Send TMR PDU ( require R_Mode )
        sess.SendOtherResponsePDU
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            tmrpdu

        [|
            fun () -> task {
                do! Task.Delay 100
                // Unlock W_Mode lock
                sess.NoticeUnlockResponseFence ResponseFenceNeedsFlag.W_Mode
            };
            fun () -> task {
                let st = Environment.TickCount

                // receive TMR PDU in the initiator
                let pdu2 =
                    PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                    |> Functions.RunTaskSynchronously
                Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )

                // TMR PDU is sent after W_Mode lock is unlocked.
                Assert.True(( Environment.TickCount - st > 90 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResponseFence_008() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 32u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        // Set W_Mode lock
        rfl.RFLock( fun () -> () )

        // Text Management Response PDU
        let tmrpdu = {
            Response = TaskMgrResCd.AUTH_FAILED;
            InitiatorTaskTag = itt_me.fromPrim 1u;
            StatSN = statsn_me.zero;
            ExpCmdSN = cmdsn_me.zero;
            MaxCmdSN = cmdsn_me.zero
            ResponseFence = ResponseFenceNeedsFlag.W_Mode;
        }

        // Send TMR PDU ( require W_Mode )
        sess.SendOtherResponsePDU
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            tmrpdu

        [|
            fun () -> task {
                do! Task.Delay 100
                // Unlock W_Mode lock
                sess.NoticeUnlockResponseFence ResponseFenceNeedsFlag.W_Mode
            };
            fun () -> task {
                let st = Environment.TickCount

                // receive TMR PDU in the initiator
                let pdu2 =
                    PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                    |> Functions.RunTaskSynchronously
                Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )

                // TMR PDU is sent after W_Mode lock is unlocked.
                Assert.True(( Environment.TickCount - st > 90 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResponseFence_009() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 512u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        // Set R_Mode lock
        rfl.NormalLock( fun () -> () )

        // Send reject PDU ( require R_Mode )
        sess.RejectPDUByLogi
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            Session_Test.defaultNopOUTPDUValues
            RejectReasonCd.COM_NOT_SUPPORT

        // R_Mode PDU is sent in immidiately under R_Mode lock
        let pdu1 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu1.Opcode = OpcodeCd.REJECT ) )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResponseFence_010() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 512u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        // Set W_Mode lock
        rfl.RFLock( fun () -> () )

        // Send reject PDU ( require R_Mode )
        sess.RejectPDUByLogi
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            Session_Test.defaultNopOUTPDUValues
            RejectReasonCd.COM_NOT_SUPPORT

        [|
            fun () -> task {
                do! Task.Delay 100
                // Unlock W_Mode lock
                sess.NoticeUnlockResponseFence ResponseFenceNeedsFlag.W_Mode
            };
            fun () -> task {
                let st = Environment.TickCount

                // receive reject PDU in the initiator
                let! pdu2 =
                    PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
                Assert.True( ( pdu2.Opcode = OpcodeCd.REJECT ) )

                // reject PDU is sent after W_Mode lock is unlocked.
                Assert.True(( Environment.TickCount - st > 90 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResendPDU_001() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 512u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        let con = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )
        Assert.True(( con.IsSome ))

        let conpc = PrivateCaller( con.Value )
        let m_ResendStat = conpc.GetField( "m_ResendStat" ) :?> OptimisticLock< ResendStatusRec >
        let m_SentRespPDUs = m_ResendStat.obj.m_SentRespPDUs

        Assert.True(( m_SentRespPDUs.Length = 1 ))

        // Send TMF PDU
        sess.ResendPDU
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            {
                Session_Test.defaultTaskManagementResponsePDUValues with
                    StatSN = statsn_me.fromPrim 0xEFEFEFEFu;
            }

        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )
        let pdu3 = pdu2 :?> TaskManagementFunctionResponsePDU
        Assert.True( ( pdu3.StatSN = statsn_me.fromPrim 0xEFEFEFEFu ) )

        Assert.True(( m_SentRespPDUs.Length = 1 ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResendPDU_002() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 512u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        let con = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )
        Assert.True(( con.IsSome ))

        let conpc = PrivateCaller( con.Value )
        let m_ResendStat = conpc.GetField( "m_ResendStat" ) :?> OptimisticLock< ResendStatusRec >
        let m_SentRespPDUs = m_ResendStat.obj.m_SentRespPDUs

        Assert.True(( m_SentRespPDUs.Length = 1 ))

        Assert.True(( rfl.LockStatus = 0L ))

        // Send TMF PDU ( require R_Mode )
        sess.ResendPDU
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            {
                Session_Test.defaultTaskManagementResponsePDUValues with
                    StatSN = statsn_me.fromPrim 0xEFEFEFEFu;
                    ResponseFence = ResponseFenceNeedsFlag.R_Mode;
            }

        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )
        let pdu3 = pdu2 :?> TaskManagementFunctionResponsePDU
        Assert.True( ( pdu3.StatSN = statsn_me.fromPrim 0xEFEFEFEFu ) )

        Assert.True(( m_SentRespPDUs.Length = 1 ))
        Assert.True(( rfl.LockStatus = 1L ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResendPDU_003() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 512u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        let con = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )
        Assert.True(( con.IsSome ))

        let conpc = PrivateCaller( con.Value )
        let m_ResendStat = conpc.GetField( "m_ResendStat" ) :?> OptimisticLock< ResendStatusRec >
        let m_SentRespPDUs = m_ResendStat.obj.m_SentRespPDUs

        Assert.True(( m_SentRespPDUs.Length = 1 ))

        Assert.True(( rfl.LockStatus = 0 ))

        // Send TMF PDU ( require W_Mode )
        sess.ResendPDU
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            {
                Session_Test.defaultTaskManagementResponsePDUValues with
                    StatSN = statsn_me.fromPrim 0xABABABABu;
                    ResponseFence = ResponseFenceNeedsFlag.W_Mode;
            }

        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ) )
        let pdu3 = pdu2 :?> TaskManagementFunctionResponsePDU
        Assert.True( ( pdu3.StatSN = statsn_me.fromPrim 0xABABABABu ) )

        Assert.True(( m_SentRespPDUs.Length = 1 ))
        Assert.True(( rfl.LockStatus = -1 ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResendPDUForRSnack_001() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 512u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false

        let con = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )
        Assert.True(( con.IsSome ))

        let conpc = PrivateCaller( con.Value )
        let m_ResendStat1 = conpc.GetField( "m_ResendStat" ) :?> OptimisticLock< ResendStatusRec >
        let m_SentRespPDUs1 = m_ResendStat1.obj.m_SentRespPDUs

        Assert.True(( m_SentRespPDUs1.Length = 1 ))

        // Send reject PDU ( require R_Mode )
        sess.ResendPDUForRSnack
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            {
                Session_Test.defaultScsiResponsePDUValues with
                    StatSN = statsn_me.fromPrim 0xEFEFEFEFu;
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    ResponseFence = ResponseFenceNeedsFlag.Immediately;
            }

        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )
        let pdu3 = pdu2 :?> SCSIResponsePDU
        Assert.True( ( pdu3.StatSN = statsn_me.fromPrim 0xEFEFEFEFu ) )

        let m_ResendStat2 = conpc.GetField( "m_ResendStat" ) :?> OptimisticLock< ResendStatusRec >
        let m_SentRespPDUs2 = m_ResendStat2.obj.m_SentRespPDUs
        Assert.True(( m_SentRespPDUs2.Length = 2 ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResendPDUForRSnack_002() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 512u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        let con = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )
        Assert.True(( con.IsSome ))

        let conpc = PrivateCaller( con.Value )
        let m_ResendStat1 = conpc.GetField( "m_ResendStat" ) :?> OptimisticLock< ResendStatusRec >
        let m_SentRespPDUs1 = m_ResendStat1.obj.m_SentRespPDUs

        Assert.True(( m_SentRespPDUs1.Length = 1 ))

        // Send reject PDU ( require R_Mode )
        sess.ResendPDUForRSnack
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            {
                Session_Test.defaultScsiResponsePDUValues with
                    StatSN = statsn_me.fromPrim 0xEFEFEFEFu;
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    ResponseFence = ResponseFenceNeedsFlag.R_Mode;
            }

        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )
        let pdu3 = pdu2 :?> SCSIResponsePDU
        Assert.True( ( pdu3.StatSN = statsn_me.fromPrim 0xEFEFEFEFu ) )

        let m_ResendStat2 = conpc.GetField( "m_ResendStat" ) :?> OptimisticLock< ResendStatusRec >
        let m_SentRespPDUs2 = m_ResendStat2.obj.m_SentRespPDUs
        Assert.True(( m_SentRespPDUs2.Length = 2 ))
        Assert.True(( rfl.LockStatus = 1 ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.ResendPDUForRSnack_003() =
        // Create session object
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                {
                    Session_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 512u;
                }
                ( cid_me.fromPrim 1us )
                ( cmdsn_me.zero )
                ( itt_me.fromPrim 0u )
                false
        let rfl = pc.GetField( "m_RespFense" ) :?> ResponseFence

        let con = sess.GetConnection ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 1 )
        Assert.True(( con.IsSome ))

        let conpc = PrivateCaller( con.Value )
        let m_ResendStat1 = conpc.GetField( "m_ResendStat" ) :?> OptimisticLock< ResendStatusRec >
        let m_SentRespPDUs1 = m_ResendStat1.obj.m_SentRespPDUs

        Assert.True(( m_SentRespPDUs1.Length = 1 ))

        // Send reject PDU ( require W_Mode )
        sess.ResendPDUForRSnack
            ( cid_me.fromPrim 1us )
            ( concnt_me.fromPrim 1 )
            {
                Session_Test.defaultScsiResponsePDUValues with
                    StatSN = statsn_me.fromPrim 0xEFEFEFEFu;
                    InitiatorTaskTag = itt_me.fromPrim 1u;
                    ResponseFence = ResponseFenceNeedsFlag.W_Mode;
            }

        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.SCSI_RES ) )
        let pdu3 = pdu2 :?> SCSIResponsePDU
        Assert.True( ( pdu3.StatSN = statsn_me.fromPrim 0xEFEFEFEFu ) )

        let m_ResendStat2 = conpc.GetField( "m_ResendStat" ) :?> OptimisticLock< ResendStatusRec >
        let m_SentRespPDUs2 = m_ResendStat2.obj.m_SentRespPDUs
        Assert.True(( m_SentRespPDUs2.Length = 2 ))
        Assert.True(( rfl.LockStatus = -1 ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.Abort_iSCSITask_001() =
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                cid_me.zero
                cmdsn_me.zero
                ( itt_me.fromPrim 0u )
                false
        let r = sess.Abort_iSCSITask ( fun _ ->
            Assert.Fail __LINE__
            false
        )
        Assert.False( r )

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.Abort_iSCSITask_002() =
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                cid_me.zero
                cmdsn_me.zero
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection cid_me.zero ( concnt_me.fromPrim 1 )

        // pdus
        let scsi1pdu = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = false;
                R = false;
                W = true;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                CmdSN = cmdsn_me.fromPrim 1u;
                DataSegment = PooledBuffer.Empty;
        }

        // receive PDU in the target
        sess.PushReceivedPDU conn1.Value scsi1pdu
        let cnt1 = ( Session_Test.GetWaitingQueue pc ).Count
        Assert.True(( 1 = cnt1 ))

        let r = sess.Abort_iSCSITask ( fun t ->
            Assert.True(( t.InitiatorTaskTag = ValueSome( itt_me.fromPrim 1u ) ))
            Assert.True(( t.TaskType = iSCSITaskType.SCSICommand ))
            false
        )
        Assert.False( r )

        let cnt1 = ( Session_Test.GetWaitingQueue pc ).Count
        Assert.True(( 1 = cnt1 ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.Abort_iSCSITask_003() =
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                cid_me.zero
                cmdsn_me.zero
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection cid_me.zero ( concnt_me.fromPrim 1 )
        let mutable cnt = 0

        // pdus
        let scsi1pdu = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = false;
                R = false;
                W = true;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                CmdSN = cmdsn_me.fromPrim 1u;
                DataSegment = PooledBuffer.Empty;
        }
        let noppdu2 = {
            Session_Test.defaultNopOUTPDUValues with
                I = false;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                CmdSN = cmdsn_me.fromPrim 2u;
        }

        // receive PDU in the target
        sess.PushReceivedPDU conn1.Value scsi1pdu
        sess.PushReceivedPDU conn1.Value noppdu2
        let cnt1 = ( Session_Test.GetWaitingQueue pc ).Count
        Assert.True(( 2 = cnt1 ))

        let r = sess.Abort_iSCSITask ( fun t ->
            cnt <- cnt + 1
            if cnt = 1 then
                Assert.True(( t.InitiatorTaskTag = ValueSome( itt_me.fromPrim 1u ) ))
                Assert.True(( t.TaskType = iSCSITaskType.SCSICommand ))
                true
            else
                Assert.True(( t.InitiatorTaskTag = ValueSome( itt_me.fromPrim 2u ) ))
                Assert.True(( t.TaskType = iSCSITaskType.NOPOut ))
                false
        )
        Assert.True( r )
        Assert.True(( cnt = 2 ))

        // receive Nop-IN PDU in the initiator
        let pdu2 =
            PDU.Receive( 4096u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, cp, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( ( pdu2.Opcode = OpcodeCd.NOP_IN ) )
        let itt2 = pdu2.InitiatorTaskTag
        Assert.True(( itt2 = itt_me.fromPrim 2u ))

        let cnt1 = ( Session_Test.GetWaitingQueue pc ).Count
        Assert.True(( 0 = cnt1 ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

    [<Fact>]
    member _.Abort_iSCSITask_004() =
        let sess, pc, killer, smStub, luStub, sp, cp =
            Session_Test.CreateDefaultSessionObject
                Session_Test.defaultSessionParam
                Session_Test.defaultConnectionParam
                cid_me.zero
                cmdsn_me.zero
                ( itt_me.fromPrim 0u )
                false
        let conn1 = sess.GetConnection cid_me.zero ( concnt_me.fromPrim 1 )
        let mutable cnt = 0

        // pdus
        let scsi1pdu = {
            Session_Test.defaultScsiCommandPDUValues with
                I = false;
                F = false;
                R = false;
                W = true;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                CmdSN = cmdsn_me.fromPrim 1u;
                DataSegment = PooledBuffer.Empty;
        }
        let noppdu2 = {
            Session_Test.defaultNopOUTPDUValues with
                I = false;
                InitiatorTaskTag = itt_me.fromPrim 2u;
                CmdSN = cmdsn_me.fromPrim 2u;
        }

        // receive PDU in the target
        sess.PushReceivedPDU conn1.Value scsi1pdu
        sess.PushReceivedPDU conn1.Value noppdu2
        let cnt1 = ( Session_Test.GetWaitingQueue pc ).Count
        Assert.True(( 2 = cnt1 ))

        let r = sess.Abort_iSCSITask ( fun t ->
            cnt <- cnt + 1
            if cnt = 1 then
                Assert.True(( t.InitiatorTaskTag = ValueSome( itt_me.fromPrim 1u ) ))
                Assert.True(( t.TaskType = iSCSITaskType.SCSICommand ))
                true
            else
                Assert.True(( t.InitiatorTaskTag = ValueSome( itt_me.fromPrim 2u ) ))
                Assert.True(( t.TaskType = iSCSITaskType.NOPOut ))
                true
        )
        Assert.True( r )
        Assert.True(( cnt = 2 ))

        let cnt1 = ( Session_Test.GetWaitingQueue pc ).Count
        Assert.True(( 0 = cnt1 ))

        sess.DestroySession()
        GlbFunc.ClosePorts [| sp; cp; |]

