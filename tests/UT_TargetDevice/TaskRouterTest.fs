//=============================================================================
// Haruka Software Storage.
// TaskRouterTest.fs : Test cases for TaskRouter class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.TargetDevice

//=============================================================================
// Import declaration

open System
open System.Collections.Generic
open System.Collections.Frozen

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.IODataTypes
open Haruka.Test

#nowarn "1240"

//=============================================================================
// Class implementation

type TaskRouter_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    static member createDefaultTaskRouter() =
        let k1 = new HKiller() :> IKiller
        let status_stub = new CStatus_Stub()
        let session = new CSession_Stub()
        let swParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = 
                    {
                    IdentNumber = tnodeidx_me.fromPrim 0u;
                    TargetName = "target001001";
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [];
                    Auth = TargetGroupConf.T_Auth.U_None();
                };
                InitiatorName = "initiator001";
                InitiatorAlias = "INITIATOR001";
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                InitialR2T = true;
                ImmediateData = true;
                MaxBurstLength = 65536u;
                FirstBurstLength = 65536u;
                DefaultTime2Wait = 2us;
                DefaultTime2Retain = 20us;
                MaxOutstandingR2T = 2us;
                DataPDUInOrder = true;
                DataSequenceInOrder = false;
                ErrorRecoveryLevel = 1uy;
            }
        let lu1 = new CLU_Stub()
        status_stub.p_GetLU <- ( fun _ -> ValueSome( lu1 :> ILU ) ) 
        let taskRouter =
            new TaskRouter(
                status_stub,
                session,
                new ITNexus(
                    "initiator001",
                    isid_me.zero,
                    "target001001",
                    tpgt_me.zero
                ),
                ( tsih_me.fromPrim 12us ),
                swParam,
                k1
            ) :> IProtocolService
        
        let lus =
            let v = [|
                lun_me.fromPrim 3UL
            |]
            v.ToFrozenSet()
        PrivateCaller( taskRouter ).SetField( "m_LUN", lus )

        k1, status_stub, session, taskRouter, lu1

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    // Test case when a login is attempted to an existing LU.
    [<Fact>]
    member _.Constructor_001() =
        let k1 = new HKiller() :> IKiller
        let status_stub = new CStatus_Stub()
        let session = new CSession_Stub()

        let swParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = { 
                    IdentNumber = tnodeidx_me.fromPrim 0u;
                    TargetName = "target001001";
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.fromPrim 1UL ];              // Attempt to log in to LU1
                    Auth = TargetGroupConf.T_Auth.U_None();
                };
                InitiatorName = "initiator001";
                InitiatorAlias = "INITIATOR001";
                TargetPortalGroupTag = tpgt_me.zero;
                InitialR2T = true;
                ImmediateData = true;
                MaxBurstLength = 65536u;
                FirstBurstLength = 65536u;
                DefaultTime2Wait = 2us;
                DefaultTime2Retain = 20us;
                MaxOutstandingR2T = 2us;
                DataPDUInOrder = true;
                DataSequenceInOrder = false;
                ErrorRecoveryLevel = 1uy;
            }

        status_stub.p_GetLU <- ( fun argLUN ->
            Assert.True( ( argLUN = lun_me.fromPrim 1UL || argLUN = lun_me.fromPrim 0UL ) )
            let r = new CLU_Stub()
            r.dummy <- box argLUN
            ValueSome( r :> ILU )   // Respond as if it exists
        )
        
        let taskRouter =
            new TaskRouter(
                status_stub,
                session,
                new ITNexus(
                    "initiator001",
                    isid_me.zero,
                    "target-001-001",
                    tpgt_me.zero
                ),
                ( tsih_me.fromPrim 1us ),
                swParam,
                k1
            )
        let lus = PrivateCaller( taskRouter ).GetField( "m_LUN" ) :?> FrozenSet< LUN_T >
        Assert.True(( lus.Count = 2 ))
        Assert.True(( lus.Contains( lun_me.fromPrim 0UL ) ))
        Assert.True(( lus.Contains( lun_me.fromPrim 1UL ) ))

        k1.NoticeTerminate()

    // Test case when a login attempt is made specifying a non-existent LU.
    [<Fact>]
    member _.Constructor_002() =
        let k1 = new HKiller() :> IKiller
        let status_stub = new CStatus_Stub()
        let session = new CSession_Stub()

        let swParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = { 
                    IdentNumber = tnodeidx_me.fromPrim 0u;
                    TargetName = "target001001";
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.fromPrim 2UL ];              // Attempt to log in to LU2
                    Auth = TargetGroupConf.T_Auth.U_None();
                };
                InitiatorName = "initiator001";
                InitiatorAlias = "INITIATOR001";
                TargetPortalGroupTag = tpgt_me.zero;
                InitialR2T = true;
                ImmediateData = true;
                MaxBurstLength = 65536u;
                FirstBurstLength = 65536u;
                DefaultTime2Wait = 2us;
                DefaultTime2Retain = 20us;
                MaxOutstandingR2T = 2us;
                DataPDUInOrder = true;
                DataSequenceInOrder = false;
                ErrorRecoveryLevel = 1uy;
            }
        status_stub.p_GetLU <- ( fun lun -> 
            Assert.True(( lun = lun_me.fromPrim 2UL || lun = lun_me.fromPrim 0UL ))
            ValueNone   // Respond as if it does not exist
        )
        
        try
            let _ =
                new TaskRouter(
                    status_stub,
                    session,
                    new ITNexus(
                        "initiator001",
                        isid_me.zero,
                        "target-001-001",
                        tpgt_me.zero
                    ),
                    ( tsih_me.fromPrim 1us ),
                    swParam,
                    k1
                )
            // It must raise an exception. It failed to login.
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ -> ()

        k1.NoticeTerminate()

    // Test case when logging in with two LUs specified
    [<Fact>]
    member _.Constructor_003() =
        let k1 = new HKiller() :> IKiller
        let status_stub = new CStatus_Stub()
        let session = new CSession_Stub()
        let swParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = { 
                    IdentNumber = tnodeidx_me.fromPrim 0u;
                    TargetName = "target001001";
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.fromPrim 1UL; lun_me.fromPrim 3UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                };
                InitiatorName = "initiator001";
                InitiatorAlias = "INITIATOR001";
                TargetPortalGroupTag = tpgt_me.zero;
                InitialR2T = true;
                ImmediateData = true;
                MaxBurstLength = 65536u;
                FirstBurstLength = 65536u;
                DefaultTime2Wait = 2us;
                DefaultTime2Retain = 20us;
                MaxOutstandingR2T = 2us;
                DataPDUInOrder = true;
                DataSequenceInOrder = false;
                ErrorRecoveryLevel = 1uy;
            }
        
        status_stub.p_GetLU <- ( fun argLUN ->
            match lun_me.toPrim argLUN with
            | 0UL
            | 1UL
            | 3UL ->
                let r = new CLU_Stub()
                r.dummy <- box argLUN
                ValueSome( r :> ILU )
            | _ ->
                Assert.Fail __LINE__
                ValueNone
        )

        let taskRouter =
            new TaskRouter(
                status_stub,
                session,
                new ITNexus(
                    "initiator001",
                    isid_me.zero,
                    "target-001-001",
                    tpgt_me.zero
                ),
                ( tsih_me.fromPrim 1us ),
                swParam,
                k1
            )
        let lus = PrivateCaller( taskRouter ).GetField( "m_LUN" ) :?> FrozenSet< LUN_T >
        Assert.True(( lus.Count = 3 ))
        Assert.True(( lus.Contains( lun_me.fromPrim 0UL ) ))
        Assert.True(( lus.Contains( lun_me.fromPrim 1UL ) ))
        Assert.True(( lus.Contains( lun_me.fromPrim 3UL ) ))

        k1.NoticeTerminate()

    // Test case when logging in with four LUs specified
    [<Fact>]
    member _.Constructor_004() =
        let k1 = new HKiller() :> IKiller
        let status_stub = new CStatus_Stub()
        let session = new CSession_Stub()

        let swParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = { 
                    IdentNumber = tnodeidx_me.fromPrim 0u;
                    TargetName = "target001001";
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.fromPrim 1UL; lun_me.fromPrim 2UL; lun_me.fromPrim 3UL; lun_me.fromPrim 4UL; ];
                    Auth = TargetGroupConf.T_Auth.U_None();
                };
                InitiatorName = "initiator001";
                InitiatorAlias = "INITIATOR001";
                TargetPortalGroupTag = tpgt_me.zero;
                InitialR2T = true;
                ImmediateData = true;
                MaxBurstLength = 65536u;
                FirstBurstLength = 65536u;
                DefaultTime2Wait = 2us;
                DefaultTime2Retain = 20us;
                MaxOutstandingR2T = 2us;
                DataPDUInOrder = true;
                DataSequenceInOrder = false;
                ErrorRecoveryLevel = 1uy;
            }
        status_stub.p_GetLU <- ( fun argLUN ->
            match lun_me.toPrim argLUN with
            | 0x0000000000000000UL
            | 0x0000000000000001UL
            | 0x0000000000000002UL
            | 0x0000000000000003UL
            | 0x0000000000000004UL ->
                ValueSome( new CLU_Stub() :> ILU )
            | _ ->
                Assert.Fail __LINE__
                ValueNone
        )
        
        let taskRouter =
            new TaskRouter(
                status_stub,
                session,
                new ITNexus(
                    "initiator001",
                    isid_me.zero,
                    "target-001-001",
                    tpgt_me.zero
                ),
                ( tsih_me.fromPrim 1us ),
                swParam,
                k1
            )
        let lus = PrivateCaller( taskRouter ).GetField( "m_LUN" ) :?> FrozenSet< LUN_T >
        Assert.True(( lus.Count = 5 ))
        Assert.True(( lus.Contains( lun_me.fromPrim 0UL ) ))
        Assert.True(( lus.Contains( lun_me.fromPrim 1UL ) ))
        Assert.True(( lus.Contains( lun_me.fromPrim 2UL ) ))
        Assert.True(( lus.Contains( lun_me.fromPrim 3UL ) ))
        Assert.True(( lus.Contains( lun_me.fromPrim 4UL ) ))

        k1.NoticeTerminate()

    // Transfer AbortTask request to an existing LU
    [<Fact>]
    member _.AbortTask_001() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        let mutable wcnt = 0
        lu1.p_AbortTask <- ( fun ( source : CommandSourceInfo ) ( initiatorTaskTag : ITT_T ) ( referencedTaskTag : ITT_T ) ->
            Assert.True( ( cid_me.fromPrim 1us = source.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = source.ConCounter ) )
            Assert.True( ( tsih_me.fromPrim 12us = source.TSIH ) )
            Assert.True( ( itt_me.fromPrim 3u = initiatorTaskTag ) )
            Assert.True( ( itt_me.fromPrim 4u = referencedTaskTag ) )
            wcnt <- 1
        )

        let s1 = new CISCSITask_Stub()
        s1.p_GetAllegiantConnection <- ( fun _ -> ( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) )
        s1.p_GetInitiatorTaskTag <- ( fun _ -> ValueSome( itt_me.fromPrim 3u ) )
        taskRouter.AbortTask s1 ( lun_me.fromPrim 3UL ) ( itt_me.fromPrim 4u )
        Assert.True( ( 1 = wcnt ) )

        k1.NoticeTerminate()

    // Transferring an AbortTask request to a non-existent LU
    [<Fact>]
    member _.AbortTask_002() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        try
            let s2 = new CISCSITask_Stub()
            s2.p_GetAllegiantConnection <- ( fun _ -> ( cid_me.fromPrim 11us, concnt_me.fromPrim 12 ) )
            s2.p_GetInitiatorTaskTag <- ( fun _ -> ValueSome( itt_me.fromPrim 13u ) )
            taskRouter.AbortTask s2 ( lun_me.fromPrim 2UL ) ( itt_me.fromPrim 14u )
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( x.Message.Contains( "Unknown LU target" ) )

        k1.NoticeTerminate()

    // Transfer AbortTaskSet request to an existing LU
    [<Fact>]
    member _.AbortTaskSet_001() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        let mutable wcnt = 0
        lu1.p_AbortTaskSet <- ( fun ( source : CommandSourceInfo ) ( initiatorTaskTag : ITT_T ) ->
            Assert.True( ( cid_me.fromPrim 1us = source.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = source.ConCounter ) )
            Assert.True( ( tsih_me.fromPrim 12us = source.TSIH ) )
            Assert.True( ( itt_me.fromPrim 3u = initiatorTaskTag ) )
            wcnt <- 1
        )

        let s1 = new CISCSITask_Stub()
        s1.p_GetAllegiantConnection <- ( fun _ -> ( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) )
        s1.p_GetInitiatorTaskTag <- ( fun _ -> ValueSome( itt_me.fromPrim 3u ) )
        taskRouter.AbortTaskSet s1 ( lun_me.fromPrim 3UL )
        Assert.True( ( 1 = wcnt ) )

        k1.NoticeTerminate()

    // Transferring an AbortTaskSet request to a non-existent LU
    [<Fact>]
    member _.AbortTaskSet_002() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        try
            let s2 = new CISCSITask_Stub()
            s2.p_GetAllegiantConnection <- ( fun _ -> ( cid_me.fromPrim 11us, concnt_me.fromPrim 12 ) )
            s2.p_GetInitiatorTaskTag <- ( fun _ -> ValueSome( itt_me.fromPrim 13u ) )
            taskRouter.AbortTaskSet s2 ( lun_me.fromPrim 1UL )
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( x.Message.Contains( "Unknown LU target" ) )

        k1.NoticeTerminate()

    // Transfer ClearACA request to an existing LU
    [<Fact>]
    member _.ClearACA_001() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        let mutable wcnt = 0
        lu1.p_ClearACA <- ( fun ( source : CommandSourceInfo ) ( initiatorTaskTag : ITT_T ) ->
            Assert.True( ( cid_me.fromPrim 1us = source.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = source.ConCounter ) )
            Assert.True( ( tsih_me.fromPrim 12us = source.TSIH ) )
            Assert.True( ( itt_me.fromPrim 3u = initiatorTaskTag ) )
            wcnt <- 1
        )

        let s1 = new CISCSITask_Stub()
        s1.p_GetAllegiantConnection <- ( fun _ -> ( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) )
        s1.p_GetInitiatorTaskTag <- ( fun _ -> ValueSome( itt_me.fromPrim 3u ) )
        taskRouter.ClearACA s1 ( lun_me.fromPrim 3UL )
        Assert.True( ( 1 = wcnt ) )

        k1.NoticeTerminate()

    // Transferring an ClearACA request to a non-existent LU
    [<Fact>]
    member _.ClearACA_002() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        try
            let s2 = new CISCSITask_Stub()
            s2.p_GetAllegiantConnection <- ( fun _ -> ( cid_me.fromPrim 11us, concnt_me.fromPrim 12 ) )
            s2.p_GetInitiatorTaskTag <- ( fun _ -> ValueSome( itt_me.fromPrim 13u ) )
            taskRouter.ClearACA s2 ( lun_me.fromPrim 1UL )
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( x.Message.Contains( "Unknown LU target" ) )

        k1.NoticeTerminate()

    // Transfer ClearTaskSet request to an existing LU
    [<Fact>]
    member _.ClearTaskSet_001() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        let mutable wcnt = 0
        lu1.p_ClearTaskSet <- ( fun ( source : CommandSourceInfo ) ( initiatorTaskTag : ITT_T ) ->
            Assert.True( ( cid_me.fromPrim 1us = source.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = source.ConCounter ) )
            Assert.True( ( tsih_me.fromPrim 12us = source.TSIH ) )
            Assert.True( ( itt_me.fromPrim 3u = initiatorTaskTag ) )
            wcnt <- 1
        )

        let s1 = new CISCSITask_Stub()
        s1.p_GetAllegiantConnection <- ( fun _ -> ( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) )
        s1.p_GetInitiatorTaskTag <- ( fun _ -> ValueSome( itt_me.fromPrim 3u ) )
        taskRouter.ClearTaskSet s1 ( lun_me.fromPrim 3UL )
        Assert.True( ( 1 = wcnt ) )

        k1.NoticeTerminate()

    // Transferring an ClearTaskSet request to a non-existent LU
    [<Fact>]
    member _.ClearTaskSet_002() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        try
            let s2 = new CISCSITask_Stub()
            s2.p_GetAllegiantConnection <- ( fun _ -> ( cid_me.fromPrim 11us, concnt_me.fromPrim 12 ) )
            s2.p_GetInitiatorTaskTag <- ( fun _ -> ValueSome( itt_me.fromPrim 13u ) )
            taskRouter.ClearTaskSet s2 ( lun_me.fromPrim 1UL )
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( x.Message.Contains( "Unknown LU target" ) )

        k1.NoticeTerminate()

    // Transfer LogicalUnitReset request to an existing LU
    [<Fact>]
    member _.LogicalUnitReset_001() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        let mutable wcnt = 0
        lu1.p_LogicalUnitReset <- ( fun ( source : CommandSourceInfo voption ) ( initiatorTaskTag : ITT_T voption ) ->
            Assert.True( ( cid_me.fromPrim 1us = source.Value.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = source.Value.ConCounter ) )
            Assert.True( ( tsih_me.fromPrim 12us = source.Value.TSIH ) )
            Assert.True( ( itt_me.fromPrim 3u = initiatorTaskTag.Value ) )
            wcnt <- 1
        )

        let s1 = new CISCSITask_Stub()
        s1.p_GetAllegiantConnection <- ( fun _ -> ( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) )
        s1.p_GetInitiatorTaskTag <- ( fun _ -> ValueSome( itt_me.fromPrim 3u ) )
        taskRouter.LogicalUnitReset s1 ( lun_me.fromPrim 3UL )
        Assert.True( ( 1 = wcnt ) )

        k1.NoticeTerminate()

    // Transferring an LogicalUnitReset request to a non-existent LU
    [<Fact>]
    member _.LogicalUnitReset_002() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        try
            let s2 = new CISCSITask_Stub()
            s2.p_GetAllegiantConnection <- ( fun _ -> ( cid_me.fromPrim 11us, concnt_me.fromPrim 12 ) )
            s2.p_GetInitiatorTaskTag <- ( fun _ -> ValueSome( itt_me.fromPrim 13u ) )
            taskRouter.LogicalUnitReset s2 ( lun_me.fromPrim 1UL )
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( x.Message.Contains( "Unknown LU target" ) )

        k1.NoticeTerminate()

    // Transfer SCSICommand request to an existing LU
    [<Fact>]
    member _.SCSICommand_001() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        let mutable wcnt = 0
        lu1.p_SCSICommand <- ( fun ( source : CommandSourceInfo ) ( command : SCSICommandPDU ) ( data : SCSIDataOutPDU list ) ->
            Assert.True( ( cid_me.fromPrim 1us = source.CID ) )
            Assert.True( ( concnt_me.fromPrim 2 = source.ConCounter ) )
            Assert.True( ( tsih_me.fromPrim 12us = source.TSIH ) )
            Assert.True( ( command.InitiatorTaskTag = itt_me.fromPrim 3u ) )
            Assert.True( ( data.Length = 0 ) )
            wcnt <- 1
        )

        let argcmd = {
            I = true;
            F = true;
            R = true;
            W = true;
            ATTR = TaskATTRCd.TAGLESS_TASK;
            LUN = lun_me.fromPrim 3UL;
            InitiatorTaskTag = itt_me.fromPrim 3u;
            ExpectedDataTransferLength = 0u;
            CmdSN = cmdsn_me.zero;
            ExpStatSN = statsn_me.zero;
            ScsiCDB = Array.empty;
            DataSegment = PooledBuffer.Empty;
            BidirectionalExpectedReadDataLength = 0u;
            ByteCount = 0u;
        }

        taskRouter.SCSICommand ( cid_me.fromPrim 1us ) ( concnt_me.fromPrim 2 ) argcmd []
        Assert.True( ( 1 = wcnt ) )

        k1.NoticeTerminate()

    // Transferring an SCSICommand request to a non-existent LU
    [<Fact>]
    member _.SCSICommand_002() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        let argcmd = {
            I = true;
            F = true;
            R = true;
            W = true;
            ATTR = TaskATTRCd.TAGLESS_TASK;
            LUN = lun_me.fromPrim 99UL;
            InitiatorTaskTag = itt_me.fromPrim 3u;
            ExpectedDataTransferLength = 0u;
            CmdSN = cmdsn_me.zero;
            ExpStatSN = statsn_me.zero;
            ScsiCDB = Array.empty;
            DataSegment = PooledBuffer.Empty;
            BidirectionalExpectedReadDataLength = 0u;
            ByteCount = 0u;
        }

        try
            taskRouter.SCSICommand ( cid_me.fromPrim 11us ) ( concnt_me.fromPrim 12 ) argcmd []
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( x.Message.Contains( "Unknown LU target" ) )

        k1.NoticeTerminate()

    // Sends a SCSI response to the surviving session
    [<Fact>]
    member _.SendSCSIResponse_001() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        let mutable wcnt = 0
        session.p_SendSCSIResponse <-
            ( fun 
                ( reqCmdPDU : SCSICommandPDU )
                ( cid : CID_T )
                ( counter : CONCNT_T )
                ( recvDataLength : uint32 )
                ( argRespCode : iScsiSvcRespCd )
                ( argStatCode : ScsiCmdStatCd )
                ( senseData : PooledBuffer )
                ( resData : PooledBuffer )
                ( allocationLength : uint32 ) 
                ( needResponseFence : ResponseFenceNeedsFlag ) ->
                        Assert.True( reqCmdPDU.I )
                        Assert.True( reqCmdPDU.F )
                        Assert.True( reqCmdPDU.R )
                        Assert.True( reqCmdPDU.W )
                        Assert.True( reqCmdPDU.ATTR = TaskATTRCd.TAGLESS_TASK )
                        Assert.True( reqCmdPDU.LUN = lun_me.fromPrim 3UL )
                        Assert.True( reqCmdPDU.InitiatorTaskTag = itt_me.fromPrim 3u )
                        Assert.True( reqCmdPDU.ExpectedDataTransferLength = 0u )
                        Assert.True( reqCmdPDU.CmdSN = cmdsn_me.zero )
                        Assert.True( reqCmdPDU.ExpStatSN = statsn_me.zero )
                        Assert.True( ( cid = cid_me.fromPrim 1us ) )
                        Assert.True(( (=) counter concnt_me.zero ))
                        Assert.True( 10u = recvDataLength )
                        Assert.True( iScsiSvcRespCd.COMMAND_COMPLETE = argRespCode )
                        Assert.True( ScsiCmdStatCd.GOOD = argStatCode )
                        Assert.True( PooledBuffer.ValueEqualsWithArray senseData Array.empty )
                        Assert.True( PooledBuffer.ValueEqualsWithArray resData [| 0uy .. 255uy |] )
                        Assert.True( 4096u = allocationLength )
                        Assert.True(( (=) needResponseFence ResponseFenceNeedsFlag.R_Mode ))
                        wcnt <- wcnt + 1
                )

        let argcmd = {
            I = true;
            F = true;
            R = true;
            W = true;
            ATTR = TaskATTRCd.TAGLESS_TASK;
            LUN = lun_me.fromPrim 3UL;
            InitiatorTaskTag = itt_me.fromPrim 3u;
            ExpectedDataTransferLength = 0u;
            CmdSN = cmdsn_me.zero;
            ExpStatSN = statsn_me.zero;
            ScsiCDB = Array.empty;
            DataSegment = PooledBuffer.Empty;
            BidirectionalExpectedReadDataLength = 0u;
            ByteCount = 0u;
        }

        taskRouter.SendSCSIResponse
            argcmd
            ( cid_me.fromPrim 1us )
            concnt_me.zero
            10u
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 255uy |] )
            4096u
            ResponseFenceNeedsFlag.R_Mode
        Assert.True( ( 1 = wcnt ) )

        k1.NoticeTerminate()

    // Sends a SCSI response to the terminated session.
    [<Fact>]
    member _.SendSCSIResponse_002() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        // Destroy the session first.
        k1.NoticeTerminate()

        session.p_SendSCSIResponse <-
            ( fun _ _ _ _ _ _ _ _ _ _ -> Assert.Fail __LINE__ )

        let argcmd = {
            I = true;
            F = true;
            R = true;
            W = true;
            ATTR = TaskATTRCd.TAGLESS_TASK;
            LUN = lun_me.fromPrim 3UL;
            InitiatorTaskTag = itt_me.fromPrim 3u;
            ExpectedDataTransferLength = 0u;
            CmdSN = cmdsn_me.zero;
            ExpStatSN = statsn_me.zero;
            ScsiCDB = Array.empty;
            DataSegment = PooledBuffer.Empty;
            BidirectionalExpectedReadDataLength = 0u;
            ByteCount = 0u;
        }

        taskRouter.SendSCSIResponse
            argcmd
            ( cid_me.fromPrim 1us )
            concnt_me.zero
            10u
            iScsiSvcRespCd.COMMAND_COMPLETE
            ScsiCmdStatCd.GOOD
            PooledBuffer.Empty
            ( PooledBuffer.Rent [| 0uy .. 255uy |] )
            4096u
            ResponseFenceNeedsFlag.R_Mode

    // Sends a response other than SCSI response to the surviving session
    [<Fact>]
    member _.SendOtherResponse_001() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        let mutable wcnt = 0
        session.p_SendOtherResponsePDU <-
            ( fun cid counter pdu ->
                wcnt <- wcnt + 1
                Assert.True(( cid = cid_me.fromPrim 1us ))
                Assert.True(( counter = concnt_me.zero ))
            )

        taskRouter.SendOtherResponse
            ( cid_me.fromPrim 1us )
            concnt_me.zero
            {
                Response = TaskMgrResCd.FUNCTION_COMPLETE;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                ResponseFence = ResponseFenceNeedsFlag.R_Mode;
            }
            lun_me.zero

        k1.NoticeTerminate()
        Assert.True(( wcnt = 1 ))

    // Sends a response other than SCSI response to the terminated session.
    [<Fact>]
    member _.SendOtherResponse_002() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()

        session.p_SendOtherResponsePDU <-
            ( fun cid counter pdu -> Assert.Fail __LINE__ )

        // Destroy the session first.
        k1.NoticeTerminate()

        taskRouter.SendOtherResponse
                ( cid_me.fromPrim 1us )
                concnt_me.zero
                {
                    Response = TaskMgrResCd.FUNCTION_COMPLETE;
                    InitiatorTaskTag = itt_me.fromPrim 0u;
                    StatSN = statsn_me.zero;
                    ExpCmdSN = cmdsn_me.zero;
                    MaxCmdSN = cmdsn_me.zero;
                    ResponseFence = ResponseFenceNeedsFlag.R_Mode;
                }
                lun_me.zero

    // Session recovery notification.
    [<Fact>]
    member _.NoticeSessionRecovery_001() =
        let k1, status_stub, session, taskRouter, lu1 =
            TaskRouter_Test.createDefaultTaskRouter()
        let mutable cnt = 0

        session.p_DestroySession <-
            ( fun () -> cnt <- cnt + 1 )

        taskRouter.NoticeSessionRecovery( "" )
        Assert.True(( 1 = cnt ))
        k1.NoticeTerminate()

    // Execution of the GetLUNs method when multiple LUs exist.
    [<Fact>]
    member _.GetLUNs_001() =
        let k1, status_stub, session, taskRouter, _ =
            TaskRouter_Test.createDefaultTaskRouter()
        let pc = PrivateCaller( taskRouter )
        let lu1 = new CLU_Stub() :> ILU
        let m_LUN =
            let v = [|
                lun_me.fromPrim 1UL;
                lun_me.fromPrim 2UL;
                lun_me.fromPrim 3UL;
            |]
            v.ToFrozenSet()
        pc.SetField( "m_LUN", m_LUN )

        let expect = [|
            lun_me.fromPrim 1UL;
            lun_me.fromPrim 2UL;
            lun_me.fromPrim 3UL;
        |]
        Assert.True(( expect = taskRouter.GetLUNs() ))
        k1.NoticeTerminate()

    // Executing the GetLUNs method when the LU does not exist.
    [<Fact>]
    member _.GetLUNs_002() =
        let k1, status_stub, session, taskRouter, _ =
            TaskRouter_Test.createDefaultTaskRouter()
        let pc = PrivateCaller( taskRouter )
        pc.SetField( "m_LUN", ( [||] : LUN_T[] ).ToFrozenSet() )
        
        let expect = Array.empty
        Assert.True(( expect = taskRouter.GetLUNs() ))
        k1.NoticeTerminate()

    // Execution of the GetTaskQueueUsage method when no LUs exist.
    [<Fact>]
    member _.GetTaskQueueUsage_001() =
        let k1, status_stub, session, taskRouter, _ =
            TaskRouter_Test.createDefaultTaskRouter()
        let pc = PrivateCaller( taskRouter )
        pc.SetField( "m_LUN", ( [||] : LUN_T[] ).ToFrozenSet() )

        Assert.True(( 0 = taskRouter.GetTaskQueueUsage() ))
        k1.NoticeTerminate()

    // Execution of the GetTaskQueueUsage method when only LU0 exists.
    [<Fact>]
    member _.GetTaskQueueUsage_002() =
        let k1, status_stub, session, taskRouter, _ =
            TaskRouter_Test.createDefaultTaskRouter()
        let pc = PrivateCaller( taskRouter )
        let m_LUN =
            let v = [|
                lun_me.fromPrim 0UL;
            |]
            v.ToFrozenSet()
        pc.SetField( "m_LUN", m_LUN )

        Assert.True(( 0 = taskRouter.GetTaskQueueUsage() ))
        k1.NoticeTerminate()

    // Execution of the GetLUNs method when LUs other than LU0 exist.
    [<Fact>]
    member _.GetTaskQueueUsage_003() =
        let k1, status_stub, session, taskRouter, _ =
            TaskRouter_Test.createDefaultTaskRouter()
        let pc = PrivateCaller( taskRouter )

        let lu1 = new CLU_Stub(
            p_GetTaskQueueUsage = ( fun argtsih ->
                Assert.True(( argtsih = tsih_me.fromPrim 12us ))
                98
            )
        )

        let m_LUN =
            let v = [|
                lun_me.fromPrim 0UL;
                lun_me.fromPrim 1UL;
            |]
            v.ToFrozenSet()
        pc.SetField( "m_LUN", m_LUN )

        status_stub.p_GetLU <- ( fun lun ->
            Assert.True(( lun = lun_me.fromPrim 1UL ))
            ValueSome( lu1 )
        )

        Assert.True(( 98 = taskRouter.GetTaskQueueUsage() ))
        k1.NoticeTerminate()

    // Execution of the GetLUNs method when there are multiple LUs other than LU0.
    [<Fact>]
    member _.GetTaskQueueUsage_004() =
        let k1, status_stub, session, taskRouter, _ =
            TaskRouter_Test.createDefaultTaskRouter()
        let pc = PrivateCaller( taskRouter )

        let lu1 = new CLU_Stub(
            p_GetTaskQueueUsage = ( fun argtsih ->
                Assert.True(( argtsih = tsih_me.fromPrim 12us ))
                5
            )
        )
        let lu2 = new CLU_Stub(
            p_GetTaskQueueUsage = ( fun argtsih ->
                Assert.True(( argtsih = tsih_me.fromPrim 12us ))
                8
            )
        )
        let lu3 = new CLU_Stub(
            p_GetTaskQueueUsage = ( fun argtsih ->
                Assert.True(( argtsih = tsih_me.fromPrim 12us ))
                4
            )
        )

        let m_LUN =
            let v = [|
                lun_me.fromPrim 0UL;
                lun_me.fromPrim 1UL;
                lun_me.fromPrim 2UL;
                lun_me.fromPrim 3UL;
            |]
            v.ToFrozenSet()
        pc.SetField( "m_LUN", m_LUN )

        status_stub.p_GetLU <- ( fun lun ->
            if lun = lun_me.fromPrim 1UL then
                ValueSome lu1
            elif lun = lun_me.fromPrim 2UL then
                ValueSome lu2
            elif lun = lun_me.fromPrim 3UL then
                ValueSome lu3
            else
                Assert.Fail __LINE__
                ValueNone
        )

        Assert.True(( 8 = taskRouter.GetTaskQueueUsage() ))
        k1.NoticeTerminate()
