//=============================================================================
// Haruka Software Storage.
// IscsiTaskOnePDUCommandTest.fs : Test cases for IscsiTaskOnePDUCommand class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.TargetDevice

//=============================================================================
// Import declaration

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes
open Haruka.TargetDevice
open Haruka.Test

//=============================================================================
// Class implementation

type IscsiTaskOnePDUCommand_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
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

    static member defaultTaskManagementRequestPDUValues = {
        I = true;
        Function = TaskMgrReqCd.ABORT_TASK;
        LUN = lun_me.fromPrim 0x0001020304050607UL;
        InitiatorTaskTag = itt_me.fromPrim 0u;
        ReferencedTaskTag = itt_me.fromPrim 1u;
        CmdSN = cmdsn_me.fromPrim 0xAAAAAAAAu;
        ExpStatSN = statsn_me.fromPrim 0xBBBBBBBBu;
        RefCmdSN = cmdsn_me.fromPrim 0xCCCCCCCCu;
        ExpDataSN = datasn_me.fromPrim 0xDDDDDDDDu;
        ByteCount = 0u;
    }

    static member defaultLogoutRequestPDUValues = {
        I = true;
        ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
        InitiatorTaskTag = itt_me.fromPrim 0u;
        CID = cid_me.fromPrim 0us;
        CmdSN = cmdsn_me.fromPrim 0xAAAAAAAAu;
        ExpStatSN = statsn_me.fromPrim 0xBBBBBBBBu;
        ByteCount = 0u;
    }


    static member defaultSNACKRequestPDUValues = {
        Type = SnackReqTypeCd.STATUS;
        LUN = lun_me.fromPrim 0x0001020304050607UL;
        InitiatorTaskTag = itt_me.fromPrim 0u;
        TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
        ExpStatSN = statsn_me.fromPrim 0xDDDDDDDDu;
        BegRun = 0u;
        RunLength = 3u;
        ByteCount = 0u;
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
        TargetPortalGroupTag = tpgt_me.fromPrim 0us;
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
        DataSegment = ArraySegment.Empty;
        ResponseFence = ResponseFenceNeedsFlag.Immediately;
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

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constractor_001() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )

        let r =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                IscsiTaskOnePDUCommand_Test.defaultNopOUTPDUValues,
                false
            ) :> IIscsiTask
        Assert.True(( ValueSome( itt_me.fromPrim 0u ) = r.InitiatorTaskTag ))
        Assert.True(( iSCSITaskType.NOPOut = r.TaskType ))
        Assert.True(( "NOP-Out" = r.TaskTypeName ))
        Assert.True(( ValueSome( itt_me.fromPrim 0u )= r.InitiatorTaskTag ))
        Assert.True(( ValueSome( cmdsn_me.fromPrim 0xDDDDDDDDu ) = r.CmdSN ))
        Assert.True(( ValueSome( true ) = r.Immidiate ))
        Assert.True( r.IsExecutable )
        Assert.True(( struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) = r.AllegiantConnection ))
        Assert.False( r.IsRemovable )

    [<Fact>]
    member _.Constractor_002() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues,
                false
            ) :> IIscsiTask
        Assert.True(( ValueSome( itt_me.fromPrim 0u ) = r.InitiatorTaskTag ))
        Assert.True(( iSCSITaskType.SCSITaskManagement = r.TaskType ))
        Assert.True(( "SCSI Task management request" = r.TaskTypeName ))
        Assert.True(( ValueSome( itt_me.fromPrim 0u ) = r.InitiatorTaskTag ))
        Assert.True(( ValueSome( cmdsn_me.fromPrim 0xAAAAAAAAu ) = r.CmdSN ))
        Assert.True(( ValueSome( true ) = r.Immidiate ))
        Assert.True( r.IsExecutable )
        Assert.True(( struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) = r.AllegiantConnection ))
        Assert.False( r.IsRemovable )

    [<Fact>]
    member _.Constractor_003() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                IscsiTaskOnePDUCommand_Test.defaultLogoutRequestPDUValues,
                false
            ) :> IIscsiTask
        Assert.True(( ValueSome( itt_me.fromPrim 0u ) = r.InitiatorTaskTag ))
        Assert.True(( iSCSITaskType.Logout = r.TaskType ))
        Assert.True(( "Logout request" = r.TaskTypeName ))
        Assert.True(( ValueSome( itt_me.fromPrim 0u ) = r.InitiatorTaskTag ))
        Assert.True(( ValueSome( cmdsn_me.fromPrim 0xAAAAAAAAu ) = r.CmdSN ))
        Assert.True(( ValueSome( true ) = r.Immidiate ))
        Assert.True( r.IsExecutable )
        Assert.True(( struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) = r.AllegiantConnection ))
        Assert.False( r.IsRemovable )

    [<Fact>]
    member _.Constractor_004() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues,
                false
            ) :> IIscsiTask
        Assert.True(( ValueSome( itt_me.fromPrim 0u ) = r.InitiatorTaskTag ))
        Assert.True(( iSCSITaskType.SNACK = r.TaskType ))
        Assert.True(( "SNACK" = r.TaskTypeName ))
        Assert.True(( ValueSome( itt_me.fromPrim 0u ) = r.InitiatorTaskTag ))
        Assert.True(( ValueSome( cmdsn_me.zero ) = r.CmdSN ))
        Assert.True(( ValueSome( true ) = r.Immidiate ))
        Assert.True( r.IsExecutable )
        Assert.True(( struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) = r.AllegiantConnection ))
        Assert.False( r.IsRemovable )

    [<Fact>]
    member _.Constractor_005() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    Reason = RejectReasonCd.COM_NOT_SUPPORT;
                    StatSN = statsn_me.zero;
                    ExpCmdSN = cmdsn_me.zero;
                    MaxCmdSN = cmdsn_me.zero;
                    DataSN_or_R2TSN = datasn_me.zero;
                    HeaderData = Array.empty;
                },
                false
            ) :> IIscsiTask
        try
            let _ = r.TaskType
            Assert.Fail __LINE__
        with
        | :? InternalAssertionException as x ->
            ()

    [<Fact>]
    member _.GetExecuteTask_001() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    Reason = RejectReasonCd.COM_NOT_SUPPORT;
                    StatSN = statsn_me.zero;
                    ExpCmdSN = cmdsn_me.zero;
                    MaxCmdSN = cmdsn_me.zero;
                    DataSN_or_R2TSN = datasn_me.zero;
                    HeaderData = Array.empty;
                },
                false
            ) :> IIscsiTask
        try
            let struct( ext, nxt ) = r.GetExecuteTask()
            Assert.True( nxt.Executed )
            ext()
            Assert.Fail __LINE__
        with
        | :? InternalAssertionException as x ->
            ()

    [<Fact>]
    member _.GetExecuteTask_003_NopOut_002() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultNopOUTPDUValues with
                        InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()   // Nothig to do

    [<Fact>]
    member _.GetExecuteTask_004_NopOut_003() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultNopOUTPDUValues with
                        InitiatorTaskTag = itt_me.fromPrim 0x0u;
                        TargetTransferTag = ttt_me.fromPrim 1u;
                },
                false
            ) :> IIscsiTask

        try
            let struct( ext, nxt ) = iscsitask.GetExecuteTask()
            Assert.True( nxt.Executed )
            ext()
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            ()

    [<Fact>]
    member _.GetExecuteTask_005_NopOut_004() =
        let mutable cnt = 0
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun cid concnt ->
                cnt <- 1
                ValueNone
            )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultNopOUTPDUValues with
                        InitiatorTaskTag = itt_me.fromPrim 0x0u;
                        TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()   // Nothig to do
        Assert.True(( cnt = 1 ))

    [<Fact>]
    member _.GetExecuteTask_005_NopOut_005() =
        let mutable cnt = 0
        let connStub = new CConnection_Stub(
                p_CurrentParams = ( fun _ -> {
                    AuthMethod = [| AuthMethodCandidateValue.AMC_None |];
                    HeaderDigest = [| DigestType.DST_None |];
                    DataDigest = [| DigestType.DST_None |];
                    MaxRecvDataSegmentLength_I = 8192u;
                    MaxRecvDataSegmentLength_T = 8192u;
                })
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun cid concnt -> 
                Assert.True(( cid = cid_me.fromPrim 1us ))
                Assert.True(( concnt = concnt_me.fromPrim 2 ))
                ValueSome( connStub :> IConnection )
            ),
            p_SendOtherResponsePDU = ( fun cid concount pdu ->
                cnt <- cnt + 1
                Assert.False( pdu.Immidiate )
                Assert.True(( OpcodeCd.NOP_IN = pdu.Opcode ))
                Assert.True(( itt_me.fromPrim 0x0u = pdu.InitiatorTaskTag ))
                let noppdu = pdu :?> NOPInPDU
                Assert.True(( lun_me.fromPrim 0x0001020304050607UL = noppdu.LUN ))
                Assert.True(( ttt_me.fromPrim 0xFFFFFFFFu = noppdu.TargetTransferTag ))
                Assert.True(( PooledBuffer.ValueEqualsWithArray noppdu.PingData [| 0x00uy .. 0xFFuy |] ))
            )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultNopOUTPDUValues with
                        InitiatorTaskTag = itt_me.fromPrim 0x0u;
                        TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))

    [<Fact>]
    member _.GetExecuteTask_005_NopOut_006() =
        let mutable cnt = 0
        let connStub = new CConnection_Stub(
                p_CurrentParams = ( fun _ -> {
                    AuthMethod = [| AuthMethodCandidateValue.AMC_None |];
                    HeaderDigest = [| DigestType.DST_None |];
                    DataDigest = [| DigestType.DST_None |];
                    MaxRecvDataSegmentLength_I = 8192u;
                    MaxRecvDataSegmentLength_T = 8192u;
                })
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun cid concnt -> 
                Assert.True(( cid = cid_me.fromPrim 1us ))
                Assert.True(( concnt = concnt_me.fromPrim 2 ))
                ValueSome( connStub :> IConnection )
            ),
            p_SendOtherResponsePDU = ( fun cid concount pdu ->
                cnt <- cnt + 1
                Assert.False( pdu.Immidiate )
                Assert.True(( OpcodeCd.NOP_IN = pdu.Opcode ))
                Assert.True(( itt_me.fromPrim 0x0u = pdu.InitiatorTaskTag ))
                let noppdu = pdu :?> NOPInPDU
                Assert.True(( lun_me.fromPrim 0x0001020304050607UL = noppdu.LUN ))
                Assert.True(( ttt_me.fromPrim 0xFFFFFFFFu = noppdu.TargetTransferTag ))
                Assert.True(( PooledBuffer.length noppdu.PingData = 8192 ))
            )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultNopOUTPDUValues with
                        InitiatorTaskTag = itt_me.fromPrim 0x0u;
                        TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                        PingData = PooledBuffer.Rent 8193;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))

    [<Fact>]
    member _.GetExecuteTask_006_ExecuteTaskManagementRequest_001() =
        let mutable cnt = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let taskRouterStub = new CProtocolService_Stub()
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSCSITaskRouter = ( fun () -> cnt <- cnt + 1; taskRouterStub )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues with
                        Function = TaskMgrReqCd.ABORT_TASK;
                },
                false
            ) :> IIscsiTask

        sessStub.p_AbortTask <- ( fun f ->
            cnt3 <- cnt3 + 1
            Assert.False(( f iscsitask ))

            let dt1 = CISCSITask_Stub( p_GetInitiatorTaskTag = fun _ -> ValueSome( itt_me.fromPrim 99u ) )
            Assert.False(( f dt1 ))

            let oITT = IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues.ReferencedTaskTag
            let dt2 = CISCSITask_Stub( p_GetInitiatorTaskTag = fun _ -> ValueSome( oITT ) )
            Assert.True(( f dt2 ))

            let dt3 = CISCSITask_Stub( p_GetInitiatorTaskTag = fun _ -> ValueNone )
            Assert.False(( f dt3 ))

            true
        )

        taskRouterStub.p_AbortTask <- ( fun iscsitask lun rtt ->
            cnt2 <- cnt2 + 1
            Assert.True(( iSCSITaskType.SCSITaskManagement = iscsitask.TaskType ))
            Assert.True(( itt_me.fromPrim 0u = ValueOption.get iscsitask.InitiatorTaskTag ))
            Assert.True(( struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) = iscsitask.AllegiantConnection ))
            Assert.True(( lun_me.fromPrim 0x0001020304050607UL = lun ))
            Assert.True(( itt_me.fromPrim 1u = rtt ))
        )

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))
        Assert.True(( 1 = cnt2 ))
        Assert.True(( 1 = cnt3 ))

    [<Fact>]
    member _.GetExecuteTask_007_ExecuteTaskManagementRequest_002() =
        let mutable cnt = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let taskRouterStub = new CProtocolService_Stub()
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSCSITaskRouter = ( fun () -> cnt <- cnt + 1; taskRouterStub )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues with
                        Function = TaskMgrReqCd.ABORT_TASK_SET;
                },
                false
            ) :> IIscsiTask

        sessStub.p_AbortTask <- ( fun f ->
            cnt3 <- cnt3 + 1
            Assert.False(( f iscsitask ))

            let dt1 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueSome( lun_me.fromPrim 99UL ) )
            Assert.False(( f dt1 ))

            let oLUN = IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues.LUN
            let dt2 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueSome( oLUN ) )
            Assert.True(( f dt2 ))

            let dt3 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueNone )
            Assert.False(( f dt3 ))

            true
        )

        taskRouterStub.p_AbortTaskSet <- ( fun iscsitask lun ->
            cnt2 <- cnt2 + 1
            Assert.True(( iSCSITaskType.SCSITaskManagement = iscsitask.TaskType ))
            Assert.True(( itt_me.fromPrim 0u = ValueOption.get iscsitask.InitiatorTaskTag ))
            Assert.True(( struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) = iscsitask.AllegiantConnection ))
            Assert.True(( lun_me.fromPrim 0x0001020304050607UL = lun ))
        )

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))
        Assert.True(( 1 = cnt2 ))
        Assert.True(( 1 = cnt3 ))

    [<Fact>]
    member _.GetExecuteTask_008_ExecuteTaskManagementRequest_003() =
        let mutable cnt = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let taskRouterStub = new CProtocolService_Stub()
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSCSITaskRouter = ( fun () -> cnt <- cnt + 1; taskRouterStub )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues with
                        Function = TaskMgrReqCd.CLEAR_ACA;
                },
                false
            ) :> IIscsiTask

        sessStub.p_AbortTask <- ( fun f ->
            cnt3 <- cnt3 + 1
            Assert.False(( f iscsitask ))

            // Not a SCSI task
            let dt1 = CISCSITask_Stub()
            Assert.False(( f dt1 ))

            // SCSI Command PDU is not present
            let scsiTask1 =
                new IscsiTaskScsiCommand(
                    objidx_me.NewID(), sessStub :> ISession,
                    cid_me.fromPrim 0us, concnt_me.fromPrim 0,
                    ValueNone,
                    [], Array.empty, DATARECVSTAT.UNSOLICITED, 0u, false
                )
            Assert.False(( f scsiTask1 ))

            // Not an ACA task
            let cmdpdu2 = {
                IscsiTaskOnePDUCommand_Test.defaultScsiCommandPDUValues with
                    ATTR = TaskATTRCd.SIMPLE_TASK;
                    LUN = IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues.LUN;
            }
            let scsiTask2 =
                new IscsiTaskScsiCommand(
                    objidx_me.NewID(), sessStub :> ISession,
                    cid_me.fromPrim 0us, concnt_me.fromPrim 0,
                    ValueSome cmdpdu2,
                    [], Array.empty, DATARECVSTAT.UNSOLICITED, 0u, false
                )
            Assert.False(( f scsiTask2 ))   

            // LUN do not match
            let cmdpdu3 = {
                IscsiTaskOnePDUCommand_Test.defaultScsiCommandPDUValues with
                    ATTR = TaskATTRCd.ACA_TASK;
                    LUN = lun_me.fromPrim 9999UL;
            }
            let scsiTask3 =
                new IscsiTaskScsiCommand(
                    objidx_me.NewID(), sessStub :> ISession,
                    cid_me.fromPrim 0us, concnt_me.fromPrim 0,
                    ValueSome cmdpdu3,
                    [], Array.empty, DATARECVSTAT.UNSOLICITED, 0u, false
                )
            Assert.False(( f scsiTask3 ))

            // ACA task and LUN match
            let cmdpdu4 = {
                IscsiTaskOnePDUCommand_Test.defaultScsiCommandPDUValues with
                    ATTR = TaskATTRCd.ACA_TASK;
                    LUN = IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues.LUN;
            }
            let scsiTask4 =
                new IscsiTaskScsiCommand(
                    objidx_me.NewID(), sessStub :> ISession,
                    cid_me.fromPrim 0us, concnt_me.fromPrim 0,
                    ValueSome cmdpdu4,
                    [], Array.empty, DATARECVSTAT.UNSOLICITED, 0u, false
                )
            Assert.True(( f scsiTask4 ))

            true
        )

        taskRouterStub.p_ClearACA <- ( fun iscsitask lun ->
            cnt2 <- cnt2 + 1
            Assert.True(( iSCSITaskType.SCSITaskManagement = iscsitask.TaskType ))
            Assert.True(( itt_me.fromPrim 0u = ValueOption.get iscsitask.InitiatorTaskTag ))
            Assert.True(( struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) = iscsitask.AllegiantConnection ))
            Assert.True(( lun_me.fromPrim 0x0001020304050607UL = lun ))
        )

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))
        Assert.True(( 1 = cnt2 ))
        Assert.True(( 1 = cnt3 ))

    [<Fact>]
    member _.GetExecuteTask_009_ExecuteTaskManagementRequest_004() =
        let mutable cnt = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let taskRouterStub = new CProtocolService_Stub()
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSCSITaskRouter = ( fun () -> cnt <- cnt + 1; taskRouterStub )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues with
                        Function = TaskMgrReqCd.CLEAR_TASK_SET;
                },
                false
            ) :> IIscsiTask

        sessStub.p_AbortTask <- ( fun f ->
            cnt3 <- cnt3 + 1
            Assert.False(( f iscsitask ))

            let dt1 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueSome( lun_me.fromPrim 99UL ) )
            Assert.False(( f dt1 ))

            let oLUN = IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues.LUN
            let dt2 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueSome( oLUN ) )
            Assert.True(( f dt2 ))

            let dt3 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueNone )
            Assert.False(( f dt3 ))

            true
        )

        taskRouterStub.p_ClearTaskSet <- ( fun iscsitask lun ->
            cnt2 <- cnt2 + 1
            Assert.True(( iSCSITaskType.SCSITaskManagement = iscsitask.TaskType ))
            Assert.True(( itt_me.fromPrim 0u = ValueOption.get iscsitask.InitiatorTaskTag ))
            Assert.True(( struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) = iscsitask.AllegiantConnection ))
            Assert.True(( lun_me.fromPrim 0x0001020304050607UL = lun ))
        )

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))
        Assert.True(( 1 = cnt2 ))
        Assert.True(( 1 = cnt3 ))

    [<Fact>]
    member _.GetExecuteTask_010_ExecuteTaskManagementRequest_005() =
        let mutable cnt = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let taskRouterStub = new CProtocolService_Stub()
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSCSITaskRouter = ( fun () -> cnt <- cnt + 1; taskRouterStub )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues with
                        Function = TaskMgrReqCd.LOGICAL_UNIT_RESET;
                },
                false
            ) :> IIscsiTask

        sessStub.p_AbortTask <- ( fun f ->
            cnt3 <- cnt3 + 1
            Assert.False(( f iscsitask ))

            let dt1 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueSome( lun_me.fromPrim 99UL ) )
            Assert.False(( f dt1 ))

            let oLUN = IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues.LUN
            let dt2 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueSome( oLUN ) )
            Assert.True(( f dt2 ))

            let dt3 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueNone )
            Assert.False(( f dt3 ))

            true
        )

        taskRouterStub.p_LogicalUnitReset <- ( fun iscsitask lun ->
            cnt2 <- cnt2 + 1
            Assert.True(( iSCSITaskType.SCSITaskManagement = iscsitask.TaskType ))
            Assert.True(( itt_me.fromPrim 0u = ValueOption.get iscsitask.InitiatorTaskTag ))
            Assert.True(( struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) = iscsitask.AllegiantConnection ))
            Assert.True(( lun_me.fromPrim 0x0001020304050607UL = lun ))
        )

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))
        Assert.True(( 1 = cnt2 ))
        Assert.True(( 1 = cnt3 ))

    [<Fact>]
    member _.GetExecuteTask_011_ExecuteTaskManagementRequest_006() =
        let mutable cnt = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let taskRouterStub = new CProtocolService_Stub()
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSCSITaskRouter = ( fun () -> cnt <- cnt + 1; taskRouterStub )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues with
                        Function = TaskMgrReqCd.TARGET_WARM_RESET
                },
                false
            ) :> IIscsiTask

        sessStub.p_AbortTask <- ( fun f ->
            cnt3 <- cnt3 + 1
            Assert.False(( f iscsitask ))

            let dt1 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueSome( lun_me.fromPrim 99UL ) )
            Assert.True(( f dt1 ))

            let oLUN = IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues.LUN
            let dt2 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueSome( oLUN ) )
            Assert.True(( f dt2 ))

            let dt3 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueNone )
            Assert.True(( f dt3 ))

            true
        )

        taskRouterStub.p_TargetReset <- ( fun iscsitask lun ->
            cnt2 <- cnt2 + 1
            Assert.True(( iSCSITaskType.SCSITaskManagement = iscsitask.TaskType ))
            Assert.True(( itt_me.fromPrim 0u = ValueOption.get iscsitask.InitiatorTaskTag ))
            Assert.True(( struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) = iscsitask.AllegiantConnection ))
            Assert.True(( lun_me.fromPrim 0x0001020304050607UL = lun ))
        )

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))
        Assert.True(( 1 = cnt2 ))
        Assert.True(( 1 = cnt3 ))

    [<Fact>]
    member _.GetExecuteTask_013_ExecuteTaskManagementRequest_008() =
        let mutable cnt = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let taskRouterStub = new CProtocolService_Stub()
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSCSITaskRouter = ( fun () -> cnt <- cnt + 1; taskRouterStub )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues with
                        Function = TaskMgrReqCd.TARGET_COLD_RESET
                },
                false
            ) :> IIscsiTask

        sessStub.p_AbortTask <- ( fun f ->
            cnt3 <- cnt3 + 1
            Assert.False(( f iscsitask ))

            let dt1 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueSome( lun_me.fromPrim 99UL ) )
            Assert.True(( f dt1 ))

            let oLUN = IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues.LUN
            let dt2 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueSome( oLUN ) )
            Assert.True(( f dt2 ))

            let dt3 = CISCSITask_Stub( p_GetLUN = fun _ -> ValueNone )
            Assert.True(( f dt3 ))

            true
        )

        taskRouterStub.p_TargetReset <- ( fun iscsitask lun ->
            cnt2 <- cnt2 + 1
            Assert.True(( iSCSITaskType.SCSITaskManagement = iscsitask.TaskType ))
            Assert.True(( itt_me.fromPrim 0u = ValueOption.get iscsitask.InitiatorTaskTag ))
            Assert.True(( struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) = iscsitask.AllegiantConnection ))
            Assert.True(( lun_me.fromPrim 0x0001020304050607UL = lun ))
        )

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))
        Assert.True(( 1 = cnt2 ))
        Assert.True(( 1 = cnt3 ))

    [<Fact>]
    member _.GetExecuteTask_016_ExecuteTaskManagementRequest_011() =
        let mutable cnt2 = 0
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_SendOtherResponsePDU = ( fun cid concount pdu ->
                cnt2 <- cnt2 + 1
                Assert.True(( OpcodeCd.SCSI_TASK_MGR_RES = pdu.Opcode ))
                let respdu = pdu :?> TaskManagementFunctionResponsePDU
                Assert.True(( TaskMgrResCd.TASK_REASSIGN_NOT_SUPPORT = respdu.Response ))
                Assert.True(( itt_me.fromPrim 0u = respdu.InitiatorTaskTag ))
            )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultTaskManagementRequestPDUValues with
                        Function = TaskMgrReqCd.TASK_REASSIGN
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_017_LogoutRequest_001() =
        let mutable cnt = 0
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_SendOtherResponsePDU = ( fun cid concnt pdu ->
                cnt <- cnt + 1
                Assert.True(( cid_me.fromPrim 1us = cid ))
                Assert.True(( concnt_me.fromPrim 2 = concnt ))
                Assert.True(( ( pdu :?> LogoutResponsePDU ).Response = LogoutResCd.RECOVERY_NOT_SUPPORT ))
            )
        )

        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultLogoutRequestPDUValues with
                        ReasonCode = LogoutReqReasonCd.RECOVERY
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))

    [<Fact>]
    member _.GetExecuteTask_018_LogoutRequest_002() =
        let mutable cnt = 0
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetAllConnections = ( fun () -> [||] ),
            p_SendOtherResponsePDU = ( fun cid concnt pdu ->
                cnt <- cnt + 1
                Assert.True(( cid_me.fromPrim 1us = cid ))
                Assert.True(( concnt_me.fromPrim 2 = concnt ))
                Assert.True(( ( pdu :?> LogoutResponsePDU ).Response = LogoutResCd.CID_NOT_FOUND ))
                Assert.False(( ( pdu :?> LogoutResponsePDU ).CloseAllegiantConnection ))
            )
        )

        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultLogoutRequestPDUValues with
                        ReasonCode = LogoutReqReasonCd.CLOSE_SESS
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))

    [<Fact>]
    member _.GetExecuteTask_019_LogoutRequest_003() =
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let connStub1 = new CConnection_Stub()
        let connStub2 = new CConnection_Stub()
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetAllConnections = ( fun () -> [| connStub1; connStub2; |] ),
            p_SendOtherResponsePDU = ( fun cid concnt pdu ->
                cnt2 <- cnt2 + 1
                if cnt2 = 1 then
                    Assert.True(( cid_me.fromPrim 1us = cid ))
                    Assert.True(( concnt_me.fromPrim 1 = concnt ))
                    Assert.True(( ( pdu :?> LogoutResponsePDU ).Response = LogoutResCd.SUCCESS ))
                    Assert.True(( ( pdu :?> LogoutResponsePDU ).CloseAllegiantConnection ))
                else
                    Assert.Fail __LINE__
            ),
            p_RemoveConnection = ( fun cid concnt ->
                Assert.True(( cid = cid_me.fromPrim 2us ))
                Assert.True(( concnt = concnt_me.fromPrim 2 ))
            )
        )
        connStub1.p_CID <- ( fun _ -> cid_me.fromPrim 1us )
        connStub1.p_ConCounter <- ( fun _ -> concnt_me.fromPrim 1 )
        connStub2.p_CID <- ( fun _ -> cid_me.fromPrim 2us )
        connStub2.p_ConCounter <- ( fun _ -> concnt_me.fromPrim 2 )
        connStub2.p_Close <- ( fun _ -> cnt3 <- cnt3 + 1 )

        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 1,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultLogoutRequestPDUValues with
                        ReasonCode = LogoutReqReasonCd.CLOSE_SESS
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt2 ))
        Assert.True(( 1 = cnt3 ))

    [<Fact>]
    member _.GetExecuteTask_020_LogoutRequest_004() =
        let mutable cnt2 = 0
        let connStub1 = new CConnection_Stub()
        let connStub2 = new CConnection_Stub()
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetAllConnections = ( fun () -> [| connStub1; connStub2; |] ),
            p_SendOtherResponsePDU = ( fun cid concnt pdu ->
                cnt2 <- cnt2 + 1
                if cnt2 = 1 then
                    Assert.True(( cid_me.fromPrim 1us = cid ))
                    Assert.True(( concnt_me.fromPrim 1 = concnt ))
                    Assert.True(( ( pdu :?> LogoutResponsePDU ).Response = LogoutResCd.SUCCESS ))
                    Assert.True(( ( pdu :?> LogoutResponsePDU ).CloseAllegiantConnection ))
                else
                    Assert.Fail __LINE__
            )
        )
        connStub1.p_CID <- ( fun _ -> cid_me.fromPrim 1us )
        connStub1.p_ConCounter <- ( fun _ -> concnt_me.fromPrim 1 )
        connStub2.p_CID <- ( fun _ -> cid_me.fromPrim 2us )
        connStub2.p_ConCounter <- ( fun _ -> concnt_me.fromPrim 2 )

        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 1,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultLogoutRequestPDUValues with
                        ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
                        CID = cid_me.fromPrim 1us;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_021_LogoutRequest_005() =
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let connStub1 = new CConnection_Stub()
        let connStub2 = new CConnection_Stub()
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetAllConnections = ( fun () -> [| connStub1; connStub2; |] ),
            p_SendOtherResponsePDU = ( fun cid concnt pdu ->
                cnt2 <- cnt2 + 1
                if cnt2 = 1 then
                    Assert.True(( cid_me.fromPrim 1us = cid ))
                    Assert.True(( concnt_me.fromPrim 1 = concnt ))
                    Assert.True(( ( pdu :?> LogoutResponsePDU ).Response = LogoutResCd.SUCCESS ))
                    Assert.False(( ( pdu :?> LogoutResponsePDU ).CloseAllegiantConnection ))
                else
                    Assert.Fail __LINE__
            ),
            p_RemoveConnection = ( fun cid concnt ->
                Assert.True(( cid = cid_me.fromPrim 2us ))
                Assert.True(( concnt = concnt_me.fromPrim 2 ))
            )
        )
        connStub1.p_CID <- ( fun _ -> cid_me.fromPrim 1us )
        connStub1.p_ConCounter <- ( fun _ -> concnt_me.fromPrim 1 )
        connStub2.p_CID <- ( fun _ -> cid_me.fromPrim 2us )
        connStub2.p_ConCounter <- ( fun _ -> concnt_me.fromPrim 2 )
        connStub2.p_Close <- ( fun _ -> cnt3 <- cnt3 + 1 )

        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 1,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultLogoutRequestPDUValues with
                        ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
                        CID = cid_me.fromPrim 2us;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt2 ))
        Assert.True(( 1 = cnt3 ))

    [<Fact>]
    member _.GetExecuteTask_022_LogoutRequest_006() =
        let mutable cnt2 = 0
        let connStub1 = new CConnection_Stub()
        let connStub2 = new CConnection_Stub()
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetAllConnections = ( fun () -> [| connStub1; connStub2; |] ),
            p_SendOtherResponsePDU = ( fun cid concnt pdu ->
                cnt2 <- cnt2 + 1
                if cnt2 = 1 then
                    Assert.True(( cid_me.fromPrim 1us = cid ))
                    Assert.True(( concnt_me.fromPrim 1 = concnt ))
                    Assert.True(( ( pdu :?> LogoutResponsePDU ).Response = LogoutResCd.CID_NOT_FOUND ))
                    Assert.False(( ( pdu :?> LogoutResponsePDU ).CloseAllegiantConnection ))
                else
                    Assert.Fail __LINE__
            )
        )
        connStub1.p_CID <- ( fun _ -> cid_me.fromPrim 1us )
        connStub1.p_ConCounter <- ( fun _ -> concnt_me.fromPrim 1 )
        connStub2.p_CID <- ( fun _ -> cid_me.fromPrim 2us )
        connStub2.p_ConCounter <- ( fun _ -> concnt_me.fromPrim 2 )

        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 1,
                { 
                    IscsiTaskOnePDUCommand_Test.defaultLogoutRequestPDUValues with
                        ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
                        CID = cid_me.fromPrim 3us;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt2 ))


    [<Fact>]
    member _.GetExecuteTask_023_SNACK_001_ERR0() =
        let mutable cnt = 0
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_RejectPDUByLogi = ( fun _ _ _ _ -> cnt <- cnt + 1 ),
            p_GetSessionParameter = ( fun () -> IscsiTaskOnePDUCommand_Test.defaultSessionParameter )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues,
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))

    [<Fact>]
    member _.GetExecuteTask_024_SNACK_002_ConMissing() =
        let mutable cnt = 0
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueNone ),
            p_RejectPDUByLogi = ( fun _ _ _ _ -> cnt <- cnt + 1 ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues,
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 0 = cnt ))

    [<Fact>]
    member _.GetExecuteTask_025_SNACK_003_DataR2T() =
        let mutable cnt = 0
        let mutable cnt2 = 0
        let connStub1 = new CConnection_Stub(
            p_GetSentDataInPDUForSNACK = (
                fun itt begrun runlength ->
                    Assert.True(( itt = itt_me.fromPrim 1u ))
                    Assert.True(( begrun = datasn_me.fromPrim 5u ))
                    Assert.True(( runlength = 6u ))
                    cnt2 <- cnt2 + 1
                    Array.empty
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_RejectPDUByLogi = ( fun cid counter pdu argReason ->
                Assert.True(( cid = cid_me.fromPrim 1us ))
                Assert.True(( counter = concnt_me.fromPrim 2 ))
                Assert.True(( pdu.Opcode = OpcodeCd.SNACK ))
                Assert.True(( argReason = RejectReasonCd.PROTOCOL_ERR ))
                cnt <- cnt + 1
            ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.DATA_R2T;
                        InitiatorTaskTag = itt_me.fromPrim 1u;
                        BegRun = 5u;
                        RunLength = 6u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))
        Assert.True(( 1 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_026_SNACK_004_DataR2T() =
        let mutable cnt = 0
        let mutable cnt2 = 0
        let connStub1 = new CConnection_Stub(
            p_GetSentDataInPDUForSNACK = (
                fun itt begrun runlength ->
                    Assert.True(( itt = itt_me.fromPrim 1u ))
                    Assert.True(( begrun = datasn_me.fromPrim 5u ))
                    Assert.True(( runlength = 7u ))
                    cnt2 <- cnt2 + 1
                    [|
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                F = false;
                                DataSN = datasn_me.zero;
                        };
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                F = true;
                                DataSN = datasn_me.fromPrim 1u;
                        }
                    |]
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDU = ( fun cid concnt pdu ->
                cnt <- cnt + 1
                Assert.True(( cid = cid_me.fromPrim 1us ))
                Assert.True(( concnt = concnt_me.fromPrim 2 ))
                if cnt = 1 then
                    Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                    let wpdu = pdu :?> SCSIDataInPDU
                    Assert.True(( wpdu.F = false ))
                    Assert.True(( wpdu.DataSN = datasn_me.zero ))
                elif cnt = 2 then
                    Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                    let wpdu = pdu :?> SCSIDataInPDU
                    Assert.True(( wpdu.F = true ))
                    Assert.True(( wpdu.DataSN = datasn_me.fromPrim 1u ))
            )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.DATA_R2T;
                        InitiatorTaskTag = itt_me.fromPrim 1u;
                        BegRun = 5u;
                        RunLength = 7u
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 2 = cnt ))
        Assert.True(( 1 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_027_SNACK_005_Status() =
        let mutable cnt = 0
        let mutable cnt2 = 0
        let connStub1 = new CConnection_Stub(
            p_GetSentResponsePDUForSNACK = (
                fun begrun length ->
                    Assert.True(( begrun = statsn_me.fromPrim 2u ))
                    Assert.True(( length = 5u ))
                    cnt2 <- cnt2 + 1
                    Array.empty
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_RejectPDUByLogi = ( fun cid counter pdu argReason ->
                Assert.True(( cid = cid_me.fromPrim 1us ))
                Assert.True(( counter = concnt_me.fromPrim 2 ))
                Assert.True(( pdu.Opcode = OpcodeCd.SNACK ))
                Assert.True(( argReason = RejectReasonCd.PROTOCOL_ERR ))
                cnt <- cnt + 1
            ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.STATUS;
                        BegRun = 2u;
                        RunLength = 5u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt ))
        Assert.True(( 1 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_028_SNACK_006_Status() =
        let mutable cnt = 0
        let mutable cnt2 = 0
        let connStub1 = new CConnection_Stub(
            p_GetSentResponsePDUForSNACK = (
                fun begrun length ->
                    Assert.True(( begrun = statsn_me.fromPrim 2u ))
                    Assert.True(( length = 5u ))
                    cnt2 <- cnt2 + 1
                    [|
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                StatSN = statsn_me.fromPrim 2u;
                        };
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                StatSN = statsn_me.fromPrim 3u;
                        };
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                StatSN = statsn_me.fromPrim 4u;
                        }
                    |]
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDU = ( fun cid concnt pdu ->
                cnt <- cnt + 1
                Assert.True(( cid = cid_me.fromPrim 1us ))
                Assert.True(( concnt = concnt_me.fromPrim 2 ))
                if cnt = 1 then
                    Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                    let wpdu = pdu :?> SCSIResponsePDU
                    Assert.True(( wpdu.StatSN = statsn_me.fromPrim 2u ))
                elif cnt = 2 then
                    Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                    let wpdu = pdu :?> SCSIResponsePDU
                    Assert.True(( wpdu.StatSN = statsn_me.fromPrim 3u ))
                elif cnt = 3 then
                    Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                    let wpdu = pdu :?> SCSIResponsePDU
                    Assert.True(( wpdu.StatSN = statsn_me.fromPrim 4u ))
            )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.STATUS;
                        BegRun = 2u;
                        RunLength = 5u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 3 = cnt ))
        Assert.True(( 1 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_029_SNACK_007_DataACK() =
        let mutable cnt2 = 0
        let connStub1 = new CConnection_Stub(
            p_NotifyDataAck = (
                fun ttt lun datasn ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( ttt = ttt_me.fromPrim 0xFFFFFFFFu ))
                    Assert.True(( lun = lun_me.fromPrim 0x0001020304050607UL ))
                    Assert.True(( datasn = datasn_me.fromPrim 12u ))
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.DATA_ACK;
                        BegRun = 12u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_030_SNACK_008_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent [| 0uy .. 255uy |]
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0u ))
                    (
                        Array.empty,
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = dataInBuffer.ArraySegment;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                DataInBuffer = dataInBuffer;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 512u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                    let w = pdu :?> SCSIResponsePDU
                    Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                    Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                    Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                    Assert.True(( w.ExpDataSN = datasn_me.zero ))
                    let ar = w.SenseData
                    Assert.True(( ar.ToArray() = [| 0uy .. 255uy |] ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt2 ))


    [<Fact>]
    member _.GetExecuteTask_031_SNACK_009_RDataSnack() =
        let mutable cnt2 = 0
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0u ))
                    (
                        Array.empty,
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                ResponseData = ArraySegment.Empty;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 512u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                    let w = pdu :?> SCSIResponsePDU
                    Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                    Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                    Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                    Assert.True(( w.ExpDataSN = datasn_me.zero ))
                    let arSenseData = w.SenseData
                    let arResponseData = w.ResponseData
                    Assert.True(( arSenseData.ToArray() = Array.empty ))
                    Assert.True(( arResponseData.ToArray() = Array.empty ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_032_SNACK_010_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent [| 0uy .. 253uy |]
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0u ))
                    (
                        Array.empty,
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                ResponseData = dataInBuffer.ArraySegment;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                                DataInBuffer = dataInBuffer;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 256u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                    let w = pdu :?> SCSIResponsePDU
                    Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                    Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                    Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                    Assert.True(( w.ExpDataSN = datasn_me.zero ))
                    let arSenseData = w.SenseData
                    let arResponseData = w.ResponseData
                    Assert.True(( arSenseData.ToArray() = Array.empty ))
                    Assert.True(( arResponseData.ToArray() = [| 0uy .. 253uy |] ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.W_Mode ))
            ),
            p_GetNextTTT = ( fun () -> ttt_me.fromPrim 3u )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_033_SNACK_011_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent [| 0uy .. 254uy |]
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    (
                        Array.empty,
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                ResponseData = dataInBuffer.ArraySegment;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                DataInBuffer = dataInBuffer;
                                LUN = lun_me.fromPrim 888UL;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 256u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    if cnt2 = 1 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = true ))
                        Assert.True(( w.A = true ))
                        Assert.True(( w.LUN = lun_me.fromPrim 888UL ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 333u ))
                        Assert.True(( w.DataSN = datasn_me.zero ))
                        Assert.True(( w.BufferOffset = 0u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.ToArray() = [| 0uy .. 254uy |] ))
                    else
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                        let w = pdu :?> SCSIResponsePDU
                        Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                        Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                        Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                        Assert.True(( w.ExpDataSN = datasn_me.fromPrim 1u ))
                        let arSenseData = w.SenseData
                        let arResponseData = w.ResponseData
                        Assert.True(( arSenseData.ToArray() = Array.empty ))
                        Assert.True(( arResponseData.ToArray() = Array.empty ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            ),
            p_GetNextTTT = ( fun () -> ttt_me.fromPrim 333u )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 2 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_034_SNACK_012_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent [| 0uy .. 255uy |]
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    (
                        Array.empty,
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                ResponseData = dataInBuffer.ArraySegment;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                DataInBuffer = dataInBuffer;
                                LUN = lun_me.fromPrim 777UL;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 256u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    if cnt2 = 1 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = true ))
                        Assert.True(( w.A = true ))
                        Assert.True(( w.LUN = lun_me.fromPrim 777UL ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 444u ))
                        Assert.True(( w.DataSN = datasn_me.zero ))
                        Assert.True(( w.BufferOffset = 0u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.ToArray() = [| 0uy .. 255uy |] ))
                    else
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                        let w = pdu :?> SCSIResponsePDU
                        Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                        Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                        Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                        Assert.True(( w.ExpDataSN = datasn_me.fromPrim 1u ))
                        let arSenseData = w.SenseData
                        let arResponseData = w.ResponseData
                        Assert.True(( arSenseData.ToArray() = Array.empty ))
                        Assert.True(( arResponseData.ToArray() = Array.empty ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            ),
            p_GetNextTTT = ( fun () -> ttt_me.fromPrim 444u )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 2 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_035_SNACK_013_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent 257
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    (
                        Array.empty,
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                ResponseData = dataInBuffer.ArraySegment;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                DataInBuffer = dataInBuffer;
                                LUN = lun_me.fromPrim 777UL
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 256u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    if cnt2 = 1 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = false ))
                        Assert.True(( w.A = false ))
                        Assert.True(( w.LUN = lun_me.zero ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 0xffffffffu ))
                        Assert.True(( w.DataSN = datasn_me.zero ))
                        Assert.True(( w.BufferOffset = 0u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.Count = 256 ))
                    elif cnt2 = 2 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = true ))
                        Assert.True(( w.A = true ))
                        Assert.True(( w.LUN = lun_me.fromPrim 777UL ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 444u ))
                        Assert.True(( w.DataSN = datasn_me.fromPrim 1u ))
                        Assert.True(( w.BufferOffset = 256u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.Count = 1 ))
                    else
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                        let w = pdu :?> SCSIResponsePDU
                        Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                        Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                        Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                        Assert.True(( w.ExpDataSN = datasn_me.fromPrim 2u ))
                        let arSenseData = w.SenseData
                        let arResponseData = w.ResponseData
                        Assert.True(( arSenseData.ToArray() = Array.empty ))
                        Assert.True(( arResponseData.ToArray() = Array.empty ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            ),
            p_GetNextTTT = ( fun () -> ttt_me.fromPrim 444u )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 3 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_036_SNACK_014_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent 512
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    (
                        Array.empty,
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                ResponseData = dataInBuffer.ArraySegment;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                DataInBuffer = dataInBuffer;
                                LUN = lun_me.fromPrim 777UL;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 256u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    if cnt2 = 1 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = false ))
                        Assert.True(( w.A = false ))
                        Assert.True(( w.LUN = lun_me.zero ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 0xffffffffu ))
                        Assert.True(( w.DataSN = datasn_me.zero ))
                        Assert.True(( w.BufferOffset = 0u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.Count = 256 ))
                    elif cnt2 = 2 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = true ))
                        Assert.True(( w.A = true ))
                        Assert.True(( w.LUN = lun_me.fromPrim 777UL ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 555u ))
                        Assert.True(( w.DataSN = datasn_me.fromPrim 1u ))
                        Assert.True(( w.BufferOffset = 256u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.Count = 256 ))
                    else
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                        let w = pdu :?> SCSIResponsePDU
                        Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                        Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                        Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                        Assert.True(( w.ExpDataSN = datasn_me.fromPrim 2u ))
                        let arSenseData = w.SenseData
                        let arResponseData = w.ResponseData
                        Assert.True(( arSenseData.ToArray() = Array.empty ))
                        Assert.True(( arResponseData.ToArray() = Array.empty ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            ),
            p_GetNextTTT = ( fun () -> ttt_me.fromPrim 555u )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 3 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_037_SNACK_015_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent 513
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    (
                        Array.empty,
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                ResponseData = dataInBuffer.ArraySegment;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                DataInBuffer = dataInBuffer;
                                LUN = lun_me.fromPrim 777UL;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 256u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    if cnt2 = 1 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = false ))
                        Assert.True(( w.A = false ))
                        Assert.True(( w.LUN = lun_me.zero ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 0xffffffffu ))
                        Assert.True(( w.DataSN = datasn_me.zero ))
                        Assert.True(( w.BufferOffset = 0u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.Count = 256 ))
                    elif cnt2 = 2 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = false ))
                        Assert.True(( w.A = false ))
                        Assert.True(( w.LUN = lun_me.zero ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 0xffffffffu ))
                        Assert.True(( w.DataSN = datasn_me.fromPrim 1u ))
                        Assert.True(( w.BufferOffset = 256u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.Count = 256 ))
                    elif cnt2 = 3 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = true ))
                        Assert.True(( w.A = true ))
                        Assert.True(( w.LUN = lun_me.fromPrim 777UL ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 555u ))
                        Assert.True(( w.DataSN = datasn_me.fromPrim 2u ))
                        Assert.True(( w.BufferOffset = 512u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.Count = 1 ))
                    else
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                        let w = pdu :?> SCSIResponsePDU
                        Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                        Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                        Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                        Assert.True(( w.ExpDataSN = datasn_me.fromPrim 3u ))
                        let arSenseData = w.SenseData
                        let arResponseData = w.ResponseData
                        Assert.True(( arSenseData.ToArray() = Array.empty ))
                        Assert.True(( arResponseData.ToArray() = Array.empty ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            ),
            p_GetNextTTT = ( fun () -> ttt_me.fromPrim 555u )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 4 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_038_SNACK_016_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent 254
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    (
                        [|
                            {
                                IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                    F = true;
                                    A = true;
                                    LUN = lun_me.fromPrim 0x3UL;
                                    InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                    TargetTransferTag = ttt_me.fromPrim 0xFEDCBA98u;
                                    DataSN = datasn_me.zero;
                                    BufferOffset = 0u;
                                    DataSegment = dataInBuffer.ArraySegment;
                            }
                        |],
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                ResponseData = ArraySegment.Empty;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                DataInBuffer = dataInBuffer;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 256u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                    let w = pdu :?> SCSIResponsePDU
                    Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                    Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                    Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                    Assert.True(( w.ExpDataSN = datasn_me.zero ))
                    let arSenseData = w.SenseData
                    let arResponseData = w.ResponseData
                    Assert.True(( arSenseData.ToArray() = Array.empty ))
                    Assert.True(( arResponseData.ToArray().Length = 254 ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_039_SNACK_017_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent 255
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    (
                        [|
                            {
                                IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                    F = true;
                                    A = true;
                                    LUN = lun_me.fromPrim 0x3UL;
                                    InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                    TargetTransferTag = ttt_me.fromPrim 0xFEDCBA98u;
                                    DataSN = datasn_me.zero;
                                    BufferOffset = 0u;
                                    DataSegment = dataInBuffer.ArraySegment;
                            }
                        |],
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                ResponseData = ArraySegment.Empty;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                LUN = lun_me.fromPrim 3UL;
                                DataInBuffer = dataInBuffer;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 256u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    if cnt2 = 1 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = true ))
                        Assert.True(( w.A = true ))
                        Assert.True(( w.LUN = lun_me.fromPrim 0x3UL ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 666u ))
                        Assert.True(( w.DataSN = datasn_me.zero ))
                        Assert.True(( w.BufferOffset = 0u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.ToArray().Length = 255 ))
                    else
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                        let w = pdu :?> SCSIResponsePDU
                        Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                        Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                        Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                        Assert.True(( w.ExpDataSN = datasn_me.fromPrim 1u ))
                        let arSenseData = w.SenseData
                        let arResponseData = w.ResponseData
                        Assert.True(( arSenseData.ToArray() = Array.empty ))
                        Assert.True(( arResponseData.ToArray() = Array.empty ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            ),
            p_GetNextTTT = ( fun () -> ttt_me.fromPrim 666u )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 2 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_040_SNACK_018_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent 257
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    (
                        [|
                            {
                                IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                    F = true;
                                    A = true;
                                    LUN = lun_me.fromPrim 0x3UL;
                                    InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                    TargetTransferTag = ttt_me.fromPrim 0xFEDCBA98u;
                                    DataSN = datasn_me.zero;
                                    BufferOffset = 0u;
                                    DataSegment = dataInBuffer.ArraySegment;
                            }
                        |],
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                ResponseData = ArraySegment.Empty;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                DataInBuffer = dataInBuffer;
                                LUN = lun_me.fromPrim 3UL;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 256u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    if cnt2 = 1 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = false ))
                        Assert.True(( w.A = false ))
                        Assert.True(( w.LUN = lun_me.zero ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 0xffffffffu ))
                        Assert.True(( w.DataSN = datasn_me.zero ))
                        Assert.True(( w.BufferOffset = 0u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.ToArray().Length = 256 ))
                    elif cnt2 = 2 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = true ))
                        Assert.True(( w.A = true ))
                        Assert.True(( w.LUN = lun_me.fromPrim 3UL ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 666u ))
                        Assert.True(( w.DataSN = datasn_me.fromPrim 1u ))
                        Assert.True(( w.BufferOffset = 256u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.ToArray().Length = 1 ))
                    else
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                        let w = pdu :?> SCSIResponsePDU
                        Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                        Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                        Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                        Assert.True(( w.ExpDataSN = datasn_me.fromPrim 2u ))
                        let arSenseData = w.SenseData
                        let arResponseData = w.ResponseData
                        Assert.True(( arSenseData.ToArray() = Array.empty ))
                        Assert.True(( arResponseData.ToArray() = Array.empty ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            ),
            p_GetNextTTT = ( fun () -> ttt_me.fromPrim 666u )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 3 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_041_SNACK_019_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent [| 0uy .. 253uy |]
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    (
                        [|
                            {
                                IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                    F = false;
                                    A = false;
                                    InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                    DataSN = datasn_me.zero;
                                    BufferOffset = 0u;
                                    DataSegment = ArraySegment( dataInBuffer.Array, 0, 128 );
                            };
                            {
                                IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                    F = true;
                                    A = false;
                                    InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                    DataSN = datasn_me.fromPrim 1u;
                                    BufferOffset = 128u;
                                    DataSegment = ArraySegment( dataInBuffer.Array, 128, 126 );
                            }
                        |],
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                ResponseData = ArraySegment.Empty;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                DataInBuffer = dataInBuffer;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 256u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                    let w = pdu :?> SCSIResponsePDU
                    Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                    Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                    Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                    Assert.True(( w.ExpDataSN = datasn_me.zero ))
                    let arSenseData = w.SenseData
                    let arResponseData = w.ResponseData
                    Assert.True(( arSenseData.ToArray() = Array.empty ))
                    Assert.True(( arResponseData.ToArray() = [| 0uy .. 253uy |] ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_042_SNACK_020_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent [| 0uy .. 255uy |]
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    (
                        [|
                            {
                                IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                    F = false;
                                    A = false;
                                    InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                    DataSN = datasn_me.zero;
                                    BufferOffset = 0u;
                                    DataSegment = ArraySegment( dataInBuffer.Array, 0, 128 );
                            };
                            {
                                IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                    F = true;
                                    A = false;
                                    InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                    DataSN = datasn_me.fromPrim 1u;
                                    BufferOffset = 128u;
                                    DataSegment = ArraySegment( dataInBuffer.Array, 128, 128 );
                            }
                        |],
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                ResponseData = ArraySegment.Empty;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                DataInBuffer = dataInBuffer;
                                LUN = lun_me.fromPrim 5555UL;

                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 256u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    if cnt2 = 1 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = true ))
                        Assert.True(( w.A = true ))
                        Assert.True(( w.LUN = lun_me.fromPrim 5555UL ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 666u ))
                        Assert.True(( w.DataSN = datasn_me.zero ))
                        Assert.True(( w.BufferOffset = 0u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.ToArray() = [| 0uy .. 255uy |] ))
                    else
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                        let w = pdu :?> SCSIResponsePDU
                        Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                        Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                        Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                        Assert.True(( w.ExpDataSN = datasn_me.fromPrim 1u ))
                        let arSenseData = w.SenseData
                        let arResponseData = w.ResponseData
                        Assert.True(( arSenseData.ToArray() = Array.empty ))
                        Assert.True(( arResponseData.ToArray() = Array.empty ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            ),
            p_GetNextTTT = ( fun () -> ttt_me.fromPrim 666u )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 2 = cnt2 ))

    [<Fact>]
    member _.GetExecuteTask_043_SNACK_021_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent [| 0uy .. 255uy |]
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    (
                        [|
                            {
                                IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                    F = false;
                                    A = false;
                                    InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                    DataSN = datasn_me.fromPrim 2u;
                                    BufferOffset = 128u;
                                    DataSegment = ArraySegment( dataInBuffer.Array, 128, 64 );
                            };
                            {
                                IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                    F = true;
                                    A = true;
                                    LUN = lun_me.fromPrim 0x3UL;
                                    InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                    TargetTransferTag = ttt_me.fromPrim 0xFEDCBA98u;
                                    DataSN = datasn_me.fromPrim 3u;
                                    BufferOffset = 192u;
                                    DataSegment = ArraySegment( dataInBuffer.Array, 192, 64 );
                            }
                        |],
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                ResponseData = ArraySegment.Empty;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                DataInBuffer = dataInBuffer;
                                LUN = lun_me.fromPrim 0x3UL;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 40u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    if cnt2 = 1 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = false ))
                        Assert.True(( w.A = false ))
                        Assert.True(( w.LUN = lun_me.zero ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 0xffffffffu ))
                        Assert.True(( w.DataSN = datasn_me.fromPrim 2u ))
                        Assert.True(( w.BufferOffset = 128u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.ToArray() = [| 128uy .. 167uy |] ))
                    elif cnt2 = 2 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = false ))
                        Assert.True(( w.A = false ))
                        Assert.True(( w.LUN = lun_me.zero ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 0xffffffffu ))
                        Assert.True(( w.DataSN = datasn_me.fromPrim 3u ))
                        Assert.True(( w.BufferOffset = 168u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.ToArray() = [| 168uy .. 207uy |] ))
                    elif cnt2 = 3 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = false ))
                        Assert.True(( w.A = false ))
                        Assert.True(( w.LUN = lun_me.zero ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 0xffffffffu ))
                        Assert.True(( w.DataSN = datasn_me.fromPrim 4u ))
                        Assert.True(( w.BufferOffset = 208u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.ToArray() = [| 208uy .. 247uy |] ))
                    elif cnt2 = 4 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = true ))
                        Assert.True(( w.A = true ))
                        Assert.True(( w.LUN = lun_me.fromPrim 3UL ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 666u ))
                        Assert.True(( w.DataSN = datasn_me.fromPrim 5u ))
                        Assert.True(( w.BufferOffset = 248u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.ToArray() = [| 248uy .. 255uy |] ))
                    else
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                        let w = pdu :?> SCSIResponsePDU
                        Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                        Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                        Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                        Assert.True(( w.ExpDataSN = datasn_me.fromPrim 6u ))
                        let arSenseData = w.SenseData
                        let arResponseData = w.ResponseData
                        Assert.True(( arSenseData.ToArray() = Array.empty ))
                        Assert.True(( arResponseData.ToArray() = Array.empty ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            ),
            p_GetNextTTT = ( fun () -> ttt_me.fromPrim 666u )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 5 = cnt2 ))


    [<Fact>]
    member _.GetExecuteTask_044_SNACK_022_RDataSnack() =
        let mutable cnt2 = 0
        let dataInBuffer = PooledBuffer.Rent [| 0uy .. 255uy |]
        let connStub1 = new CConnection_Stub(
            p_R_SNACKRequest = (
                fun itt f ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    f()
            ),
            p_GetSentSCSIResponsePDUForR_SNACK = (
                fun itt ->
                    Assert.True(( itt = itt_me.fromPrim 0x01020304u ))
                    (
                        [|
                            {
                                IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                    F = false;
                                    A = false;
                                    InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                    DataSN = datasn_me.fromPrim 2u;
                                    BufferOffset = 128u;
                                    DataSegment = ArraySegment( dataInBuffer.Array, 128, 64 );
                            };
                            {
                                IscsiTaskOnePDUCommand_Test.defaultSCSIDataInPDUValues with
                                    F = true;
                                    A = false;
                                    InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                    DataSN = datasn_me.fromPrim 3u;
                                    BufferOffset = 192u;
                                    DataSegment = ArraySegment( dataInBuffer.Array, 192, 64 );
                            }
                        |],
                        {
                            IscsiTaskOnePDUCommand_Test.defaultSCSIResponsePDUValues with
                                SenseData = ArraySegment.Empty;
                                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                                ResponseData = ArraySegment.Empty;
                                StatSN = statsn_me.fromPrim 10u;
                                ExpCmdSN = cmdsn_me.fromPrim 11u;
                                ExpDataSN = datasn_me.zero;
                                DataInBuffer = dataInBuffer;
                                LUN = lun_me.fromPrim 123UL;
                        }
                    )
            ),
            p_CurrentParams = (
                fun () -> {
                    IscsiTaskOnePDUCommand_Test.defaultConnectionParam with
                        MaxRecvDataSegmentLength_I = 256u
                }
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub1 ) ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskOnePDUCommand_Test.defaultSessionParameter with
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_ResendPDUForRSnack = (
                fun cid concnt pdu ->
                    cnt2 <- cnt2 + 1
                    Assert.True(( cid = cid_me.fromPrim 1us ))
                    Assert.True(( concnt = concnt_me.fromPrim 2 ))
                    if cnt2 = 1 then
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_DATA_IN ))
                        let w = pdu :?> SCSIDataInPDU
                        Assert.True(( w.F = true ))
                        Assert.True(( w.A = true ))
                        Assert.True(( w.LUN = lun_me.fromPrim 123UL ))
                        Assert.True(( w.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ))
                        Assert.True(( w.TargetTransferTag = ttt_me.fromPrim 777u ))
                        Assert.True(( w.DataSN = datasn_me.fromPrim 2u ))
                        Assert.True(( w.BufferOffset = 128u ))
                        let arDataSegment = w.DataSegment
                        Assert.True(( arDataSegment.ToArray() = [| 128uy .. 255uy |] ))
                    else
                        Assert.True(( pdu.Opcode = OpcodeCd.SCSI_RES ))
                        let w = pdu :?> SCSIResponsePDU
                        Assert.True(( w.SNACKTag = snacktag_me.fromPrim 0xEEEEEEEEu ))
                        Assert.True(( w.StatSN = statsn_me.fromPrim 10u ))
                        Assert.True(( w.ExpCmdSN = cmdsn_me.fromPrim 11u ))
                        Assert.True(( w.ExpDataSN = datasn_me.fromPrim 3u ))
                        let arSenseData = w.SenseData
                        let arResponseData = w.ResponseData
                        Assert.True(( arSenseData.ToArray() = Array.empty ))
                        Assert.True(( arResponseData.ToArray() = Array.empty ))
            ),
            p_NoticeUnlockResponseFence = (
                fun flg -> Assert.True(( flg = ResponseFenceNeedsFlag.R_Mode ))
            ),
            p_GetNextTTT = ( fun () -> ttt_me.fromPrim 777u )
        )
        let iscsitask =
            new IscsiTaskOnePDUCommand(
                sessStub :> ISession,
                cid_me.fromPrim 1us,
                concnt_me.fromPrim 2,
                {
                    IscsiTaskOnePDUCommand_Test.defaultSNACKRequestPDUValues with
                        Type = SnackReqTypeCd.RDATA_SNACK;
                        InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                        TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                        BegRun = 0u;
                        RunLength = 0u;
                },
                false
            ) :> IIscsiTask

        let struct( ext, nxt ) = iscsitask.GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 2 = cnt2 ))
