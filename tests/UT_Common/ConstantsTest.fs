//=============================================================================
// Haruka Software Storage.
// ConstantsTest.fs : Test cases for Constants class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System

open Xunit

open Haruka.Constants

//=============================================================================
// Class implementation

type Constants_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Test_OpcodeCd() =
        let values = Enum.GetValues( typeof<OpcodeCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> OpcodeCd
            let r =
                Constants.byteToOpcodeCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    OpcodeCd.ASYNC
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToOpcodeCd( 0xFFuy ) ( fun v2 ->
                Assert.True(( v2 = 0xFFuy ))
                OpcodeCd.ASYNC
            )
        Assert.True(( r2 = OpcodeCd.ASYNC ))

    [<Fact>]
    member _.Test_OpcodeNameFromValue() =
        Assert.True(( "NOP_OUT" = Constants.getOpcodeNameFromValue OpcodeCd.NOP_OUT ))
        Assert.True(( "SCSI_COMMAND" = Constants.getOpcodeNameFromValue OpcodeCd.SCSI_COMMAND ))
        Assert.True(( "SCSI_TASK_MGR_REQ" = Constants.getOpcodeNameFromValue OpcodeCd.SCSI_TASK_MGR_REQ ))
        Assert.True(( "LOGIN_REQ" = Constants.getOpcodeNameFromValue OpcodeCd.LOGIN_REQ ))
        Assert.True(( "TEXT_REQ" = Constants.getOpcodeNameFromValue OpcodeCd.TEXT_REQ ))
        Assert.True(( "SCSI_DATA_OUT" = Constants.getOpcodeNameFromValue OpcodeCd.SCSI_DATA_OUT ))
        Assert.True(( "LOGOUT_REQ" = Constants.getOpcodeNameFromValue OpcodeCd.LOGOUT_REQ ))
        Assert.True(( "SNACK" = Constants.getOpcodeNameFromValue OpcodeCd.SNACK ))
        Assert.True(( "NOP_IN" = Constants.getOpcodeNameFromValue OpcodeCd.NOP_IN ))
        Assert.True(( "SCSI_RES" = Constants.getOpcodeNameFromValue OpcodeCd.SCSI_RES ))
        Assert.True(( "SCSI_TASK_MGR_RES" = Constants.getOpcodeNameFromValue OpcodeCd.SCSI_TASK_MGR_RES ))
        Assert.True(( "LOGIN_RES" = Constants.getOpcodeNameFromValue OpcodeCd.LOGIN_RES ))
        Assert.True(( "TEXT_RES" = Constants.getOpcodeNameFromValue OpcodeCd.TEXT_RES ))
        Assert.True(( "SCSI_DATA_IN" = Constants.getOpcodeNameFromValue OpcodeCd.SCSI_DATA_IN ))
        Assert.True(( "LOGOUT_RES" = Constants.getOpcodeNameFromValue OpcodeCd.LOGOUT_RES ))
        Assert.True(( "R2T" = Constants.getOpcodeNameFromValue OpcodeCd.R2T ))
        Assert.True(( "ASYNC" = Constants.getOpcodeNameFromValue OpcodeCd.ASYNC ))
        Assert.True(( "REJECT" = Constants.getOpcodeNameFromValue OpcodeCd.REJECT ))

    [<Fact>]
    member _.Test_TaskATTRCd() =
        let values = Enum.GetValues( typeof<TaskATTRCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> TaskATTRCd
            let r =
                Constants.byteToTaskATTRCdCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    TaskATTRCd.ACA_TASK
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToTaskATTRCdCd( 0xFFuy ) ( fun v2 ->
                Assert.True(( v2 = 0xFFuy ))
                TaskATTRCd.ACA_TASK
            )
        Assert.True(( r2 = TaskATTRCd.ACA_TASK ))


    [<Fact>]
    member _.Test_getTaskATTRCdNameFromValue() =
        Assert.True(( "TAGLESS_TASK" = Constants.getTaskATTRCdNameFromValue TaskATTRCd.TAGLESS_TASK ))
        Assert.True(( "SIMPLE_TASK" = Constants.getTaskATTRCdNameFromValue TaskATTRCd.SIMPLE_TASK ))
        Assert.True(( "ORDERED_TASK" = Constants.getTaskATTRCdNameFromValue TaskATTRCd.ORDERED_TASK ))
        Assert.True(( "HEAD_OF_QUEUE_TASK" = Constants.getTaskATTRCdNameFromValue TaskATTRCd.HEAD_OF_QUEUE_TASK ))
        Assert.True(( "ACA_TASK" = Constants.getTaskATTRCdNameFromValue TaskATTRCd.ACA_TASK ))
        
    [<Fact>]
    member _.Test_AHSTypeCd() =
        let values = Enum.GetValues( typeof<AHSTypeCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> AHSTypeCd
            let r =
                Constants.byteToAHSTypeCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    AHSTypeCd.EXPECTED_LENGTH
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToAHSTypeCd( 0xFFuy ) ( fun v2 ->
                Assert.True(( v2 = 0xFFuy ))
                AHSTypeCd.EXPECTED_LENGTH
            )
        Assert.True(( r2 = AHSTypeCd.EXPECTED_LENGTH ))


    [<Fact>]
    member _.Test_getAHSTypeNameFromValue() =
        Assert.True(( "RESERVED" = Constants.getAHSTypeNameFromValue AHSTypeCd.RESERVED ))
        Assert.True(( "EXTENDED_CDB" = Constants.getAHSTypeNameFromValue AHSTypeCd.EXTENDED_CDB ))
        Assert.True(( "EXPECTED_LENGTH" = Constants.getAHSTypeNameFromValue AHSTypeCd.EXPECTED_LENGTH ))

    [<Fact>]
    member _.Test_iScsiSvcRespCd() =
        let values = Enum.GetValues( typeof<iScsiSvcRespCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> iScsiSvcRespCd
            let r =
                Constants.byteToiScsiSvcRespCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    iScsiSvcRespCd.COMMAND_COMPLETE
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToiScsiSvcRespCd( 2uy ) ( fun v2 ->
                Assert.True(( v2 = 2uy ))
                iScsiSvcRespCd.COMMAND_COMPLETE
            )
        Assert.True(( r2 = iScsiSvcRespCd.COMMAND_COMPLETE ))
        
    [<Fact>]
    member _.Test_getiScsiSvcRespNameFromValue() =
        Assert.True(( "COMMAND_COMPLETE" = Constants.getiScsiSvcRespNameFromValue iScsiSvcRespCd.COMMAND_COMPLETE ))
        Assert.True(( "TARGET_FAILURE" = Constants.getiScsiSvcRespNameFromValue iScsiSvcRespCd.TARGET_FAILURE ))


    [<Fact>]
    member _.Test_TaskMgrReqCd() =
        let values = Enum.GetValues( typeof<TaskMgrReqCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> TaskMgrReqCd
            let r =
                Constants.byteToTaskMgrReqCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    TaskMgrReqCd.ABORT_TASK
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToTaskMgrReqCd( 0xFFuy ) ( fun v2 ->
                Assert.True(( v2 = 0xFFuy ))
                TaskMgrReqCd.ABORT_TASK
            )
        Assert.True(( r2 = TaskMgrReqCd.ABORT_TASK ))

    [<Fact>]
    member _.Test_getTaskMgrReqNameFromValue() =
        Assert.True(( "ABORT_TASK" = Constants.getTaskMgrReqNameFromValue TaskMgrReqCd.ABORT_TASK ))
        Assert.True(( "ABORT_TASK_SET" = Constants.getTaskMgrReqNameFromValue TaskMgrReqCd.ABORT_TASK_SET ))
        Assert.True(( "CLEAR_ACA" = Constants.getTaskMgrReqNameFromValue TaskMgrReqCd.CLEAR_ACA ))
        Assert.True(( "CLEAR_TASK_SET" = Constants.getTaskMgrReqNameFromValue TaskMgrReqCd.CLEAR_TASK_SET ))
        Assert.True(( "LOGICAL_UNIT_RESET" = Constants.getTaskMgrReqNameFromValue TaskMgrReqCd.LOGICAL_UNIT_RESET ))
        Assert.True(( "TARGET_WARM_RESET" = Constants.getTaskMgrReqNameFromValue TaskMgrReqCd.TARGET_WARM_RESET ))
        Assert.True(( "TASK_REASSIGN" = Constants.getTaskMgrReqNameFromValue TaskMgrReqCd.TASK_REASSIGN ))
        
    [<Fact>]
    member _.Test_TaskMgrResCd() =
        let values = Enum.GetValues( typeof<TaskMgrResCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> TaskMgrResCd
            let r =
                Constants.byteToTaskMgrResCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    TaskMgrResCd.FUNCTION_COMPLETE
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToTaskMgrResCd( 0xFFuy ) ( fun v2 ->
                Assert.True(( v2 = 0xFFuy ))
                TaskMgrResCd.FUNCTION_COMPLETE
            )
        Assert.True(( r2 = TaskMgrResCd.FUNCTION_COMPLETE ))

    [<Fact>]
    member _.Test_getTaskMgrResNameFromValue() =
        Assert.True(( "FUNCTION_COMPLETE" = Constants.getTaskMgrResNameFromValue TaskMgrResCd.FUNCTION_COMPLETE ))
        Assert.True(( "TASK_NOT_EXIST" = Constants.getTaskMgrResNameFromValue TaskMgrResCd.TASK_NOT_EXIST ))
        Assert.True(( "LUN_NOT_EXIST" = Constants.getTaskMgrResNameFromValue TaskMgrResCd.LUN_NOT_EXIST ))
        Assert.True(( "TASK_STILL_ALLEGIANT" = Constants.getTaskMgrResNameFromValue TaskMgrResCd.TASK_STILL_ALLEGIANT ))
        Assert.True(( "TASK_REASSIGN_NOT_SUPPORT" = Constants.getTaskMgrResNameFromValue TaskMgrResCd.TASK_REASSIGN_NOT_SUPPORT ))
        Assert.True(( "TASK_MGR_NOT_SUPPORT" = Constants.getTaskMgrResNameFromValue TaskMgrResCd.TASK_MGR_NOT_SUPPORT ))
        Assert.True(( "AUTH_FAILED" = Constants.getTaskMgrResNameFromValue TaskMgrResCd.AUTH_FAILED ))
        Assert.True(( "FUNCTION_REJECT" = Constants.getTaskMgrResNameFromValue TaskMgrResCd.FUNCTION_REJECT ))

    [<Fact>]
    member _.Test_AsyncEventCd() =
        let values = Enum.GetValues( typeof<AsyncEventCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> AsyncEventCd
            let r =
                Constants.byteToAsyncEventCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    AsyncEventCd.SENCE_DATA
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToAsyncEventCd( 0xFFuy ) ( fun v2 ->
                Assert.True(( v2 = 0xFFuy ))
                AsyncEventCd.SENCE_DATA
            )
        Assert.True(( r2 = AsyncEventCd.SENCE_DATA ))

    [<Fact>]
    member _.Test_getAsyncEventNameFromValue() =
        Assert.True(( "SENCE_DATA" = Constants.getAsyncEventNameFromValue AsyncEventCd.SENCE_DATA ))
        Assert.True(( "LOGOUT_REQ" = Constants.getAsyncEventNameFromValue AsyncEventCd.LOGOUT_REQ ))
        Assert.True(( "CONNECTION_CLOSE" = Constants.getAsyncEventNameFromValue AsyncEventCd.CONNECTION_CLOSE ))
        Assert.True(( "SESSION_CLOSE" = Constants.getAsyncEventNameFromValue AsyncEventCd.SESSION_CLOSE ))
        Assert.True(( "PARAM_NEGOTIATION_REQ" = Constants.getAsyncEventNameFromValue AsyncEventCd.PARAM_NEGOTIATION_REQ ))
        Assert.True(( "SENCE_DATA" = Constants.getAsyncEventNameFromValue AsyncEventCd.SENCE_DATA ))


    [<Fact>]
    member _.Test_LoginReqStateCd() =
        let values = Enum.GetValues( typeof<LoginReqStateCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> LoginReqStateCd
            let r =
                Constants.byteToLoginReqStateCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    LoginReqStateCd.SEQURITY
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToLoginReqStateCd( 0xFFuy ) ( fun v2 ->
                Assert.True(( v2 = 0xFFuy ))
                LoginReqStateCd.SEQURITY
            )
        Assert.True(( r2 = LoginReqStateCd.SEQURITY ))

    [<Fact>]
    member _.Test_getLoginReqStateNameFromValue() =
        Assert.True(( "SEQURITY" = Constants.getLoginReqStateNameFromValue LoginReqStateCd.SEQURITY ))
        Assert.True(( "OPERATIONAL" = Constants.getLoginReqStateNameFromValue LoginReqStateCd.OPERATIONAL ))
        Assert.True(( "FULL" = Constants.getLoginReqStateNameFromValue LoginReqStateCd.FULL ))

    [<Fact>]
    member _.Test_LoginResStatCd() =
        let values = Enum.GetValues( typeof<LoginResStatCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> LoginResStatCd
            let r =
                Constants.shortToLoginResStatCd( uint16 v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    LoginResStatCd.SUCCESS
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.shortToLoginResStatCd( 0xFFFFus ) ( fun v2 ->
                Assert.True(( v2 = 0xFFFFus ))
                LoginResStatCd.SUCCESS
            )
        Assert.True(( r2 = LoginResStatCd.SUCCESS ))

    [<Fact>]
    member _.Test_getLoginResStatNameFromValue() =
        Assert.True(( "SUCCESS" = Constants.getLoginResStatNameFromValue LoginResStatCd.SUCCESS ))
        Assert.True(( "REDIRECT_TMP" = Constants.getLoginResStatNameFromValue LoginResStatCd.REDIRECT_TMP ))
        Assert.True(( "REDIRECT_PERM" = Constants.getLoginResStatNameFromValue LoginResStatCd.REDIRECT_PERM ))
        Assert.True(( "INITIATOR_ERR" = Constants.getLoginResStatNameFromValue LoginResStatCd.INITIATOR_ERR ))
        Assert.True(( "AUTH_FAILURE" = Constants.getLoginResStatNameFromValue LoginResStatCd.AUTH_FAILURE ))
        Assert.True(( "NOT_ALLOWED" = Constants.getLoginResStatNameFromValue LoginResStatCd.NOT_ALLOWED ))
        Assert.True(( "NOT_FOUND" = Constants.getLoginResStatNameFromValue LoginResStatCd.NOT_FOUND ))
        Assert.True(( "TARGET_REMOVED" = Constants.getLoginResStatNameFromValue LoginResStatCd.TARGET_REMOVED ))
        Assert.True(( "UNSUPPORTED_VERSION" = Constants.getLoginResStatNameFromValue LoginResStatCd.UNSUPPORTED_VERSION ))
        Assert.True(( "TOO_MANY_CONS" = Constants.getLoginResStatNameFromValue LoginResStatCd.TOO_MANY_CONS ))
        Assert.True(( "MISSING_PARAMS" = Constants.getLoginResStatNameFromValue LoginResStatCd.MISSING_PARAMS ))
        Assert.True(( "UNSUPPORT_MCS" = Constants.getLoginResStatNameFromValue LoginResStatCd.UNSUPPORT_MCS ))
        Assert.True(( "UNSUPPORT_SESS_TYPE" = Constants.getLoginResStatNameFromValue LoginResStatCd.UNSUPPORT_SESS_TYPE ))
        Assert.True(( "SESS_NOT_EXIST" = Constants.getLoginResStatNameFromValue LoginResStatCd.SESS_NOT_EXIST ))
        Assert.True(( "INVALID_LOGIN" = Constants.getLoginResStatNameFromValue LoginResStatCd.INVALID_LOGIN ))
        Assert.True(( "TARGET_ERROR" = Constants.getLoginResStatNameFromValue LoginResStatCd.TARGET_ERROR ))
        Assert.True(( "SERVICE_UNAVAILABLE" = Constants.getLoginResStatNameFromValue LoginResStatCd.SERVICE_UNAVAILABLE ))
        Assert.True(( "OUT_OF_RESOURCE" = Constants.getLoginResStatNameFromValue LoginResStatCd.OUT_OF_RESOURCE ))

    [<Fact>]
    member _.Test_LogoutReqReasonCd() =
        let values = Enum.GetValues( typeof<LogoutReqReasonCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> LogoutReqReasonCd
            let r =
                Constants.byteToLogoutReqReasonCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    LogoutReqReasonCd.CLOSE_SESS
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToLogoutReqReasonCd( 0xFFuy ) ( fun v2 ->
                Assert.True(( v2 = 0xFFuy ))
                LogoutReqReasonCd.CLOSE_SESS
            )
        Assert.True(( r2 = LogoutReqReasonCd.CLOSE_SESS ))

    [<Fact>]
    member _.Test_getLogoutReqReasonNameFromValue() =
        Assert.True(( "CLOSE_SESS" = Constants.getLogoutReqReasonNameFromValue LogoutReqReasonCd.CLOSE_SESS ))
        Assert.True(( "CLOSE_CONN" = Constants.getLogoutReqReasonNameFromValue LogoutReqReasonCd.CLOSE_CONN ))
        Assert.True(( "RECOVERY" = Constants.getLogoutReqReasonNameFromValue LogoutReqReasonCd.RECOVERY ))

    [<Fact>]
    member _.Test_LogoutResCd() =
        let values = Enum.GetValues( typeof<LogoutResCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> LogoutResCd
            let r =
                Constants.byteToLogoutResCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    LogoutResCd.SUCCESS
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToLogoutResCd( 0xFFuy ) ( fun v2 ->
                Assert.True(( v2 = 0xFFuy ))
                LogoutResCd.SUCCESS
            )
        Assert.True(( r2 = LogoutResCd.SUCCESS ))

    [<Fact>]
    member _.Test_getLogoutResNameFromValue() =
        Assert.True(( "SUCCESS" = Constants.getLogoutResNameFromValue LogoutResCd.SUCCESS ))
        Assert.True(( "CID_NOT_FOUND" = Constants.getLogoutResNameFromValue LogoutResCd.CID_NOT_FOUND ))
        Assert.True(( "RECOVERY_NOT_SUPPORT" = Constants.getLogoutResNameFromValue LogoutResCd.RECOVERY_NOT_SUPPORT ))
        Assert.True(( "CLEANUP_FAILED" = Constants.getLogoutResNameFromValue LogoutResCd.CLEANUP_FAILED ))

    [<Fact>]
    member _.Test_SnackReqTypeCd() =
        let values = Enum.GetValues( typeof<SnackReqTypeCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> SnackReqTypeCd
            let r =
                Constants.byteToSnackReqTypeCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    SnackReqTypeCd.DATA_R2T
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToSnackReqTypeCd( 0xFFuy ) ( fun v2 ->
                Assert.True(( v2 = 0xFFuy ))
                SnackReqTypeCd.DATA_R2T
            )
        Assert.True(( r2 = SnackReqTypeCd.DATA_R2T ))

    [<Fact>]
    member _.Test_getSnackReqTypeNameFromValue() =
        Assert.True(( "DATA_R2T" = Constants.getSnackReqTypeNameFromValue SnackReqTypeCd.DATA_R2T ))
        Assert.True(( "STATUS" = Constants.getSnackReqTypeNameFromValue SnackReqTypeCd.STATUS ))
        Assert.True(( "DATA_ACK" = Constants.getSnackReqTypeNameFromValue SnackReqTypeCd.DATA_ACK ))
        Assert.True(( "RDATA_SNACK" = Constants.getSnackReqTypeNameFromValue SnackReqTypeCd.RDATA_SNACK ))

    [<Fact>]
    member _.Test_RejectReasonCd() =
        let values = Enum.GetValues( typeof<RejectReasonCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> RejectReasonCd
            let r =
                Constants.byteToRejectReasonCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    RejectReasonCd.DATA_DIGEST_ERR
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToRejectReasonCd( 0xFFuy ) ( fun v2 ->
                Assert.True(( v2 = 0xFFuy ))
                RejectReasonCd.DATA_DIGEST_ERR
            )
        Assert.True(( r2 = RejectReasonCd.DATA_DIGEST_ERR ))

    [<Fact>]
    member _.Test_getRejectReasonNameFomValue() =
        Assert.True(( "DATA_DIGEST_ERR" = Constants.getRejectReasonNameFomValue RejectReasonCd.DATA_DIGEST_ERR ))
        Assert.True(( "SNACK_REJECT" = Constants.getRejectReasonNameFomValue RejectReasonCd.SNACK_REJECT ))
        Assert.True(( "PROTOCOL_ERR" = Constants.getRejectReasonNameFomValue RejectReasonCd.PROTOCOL_ERR ))
        Assert.True(( "COM_NOT_SUPPORT" = Constants.getRejectReasonNameFomValue RejectReasonCd.COM_NOT_SUPPORT ))
        Assert.True(( "IMMIDIATE_COM_REJECT" = Constants.getRejectReasonNameFomValue RejectReasonCd.IMMIDIATE_COM_REJECT ))
        Assert.True(( "TASK_IN_PROGRESS" = Constants.getRejectReasonNameFomValue RejectReasonCd.TASK_IN_PROGRESS ))
        Assert.True(( "INVALID_DATA_ACK" = Constants.getRejectReasonNameFomValue RejectReasonCd.INVALID_DATA_ACK ))
        Assert.True(( "INVALID_PDU_FIELD" = Constants.getRejectReasonNameFomValue RejectReasonCd.INVALID_PDU_FIELD ))
        Assert.True(( "LONG_OPE_REJECT" = Constants.getRejectReasonNameFomValue RejectReasonCd.LONG_OPE_REJECT ))
        Assert.True(( "NEGOTIATION_RESET" = Constants.getRejectReasonNameFomValue RejectReasonCd.NEGOTIATION_RESET ))
        Assert.True(( "WAIT_FOR_LOGOUT" = Constants.getRejectReasonNameFomValue RejectReasonCd.WAIT_FOR_LOGOUT ))

    [<Fact>]
    member _.Test_ScsiCmdStatCd() =
        let values = Enum.GetValues( typeof<ScsiCmdStatCd> )
        for i = 0 to values.Length - 1 do
            let v = values.GetValue( i ) :?> ScsiCmdStatCd
            let r =
                Constants.byteToScsiCmdStatCd( byte v ) ( fun v2 ->
                    Assert.Fail __LINE__
                    ScsiCmdStatCd.GOOD
                )
            Assert.True(( r = v ))
        let r2 =
            Constants.byteToScsiCmdStatCd( 0xFFuy ) ( fun v2 ->
                Assert.True(( v2 = 0xFFuy ))
                ScsiCmdStatCd.GOOD
            )
        Assert.True(( r2 = ScsiCmdStatCd.GOOD ))

    [<Fact>]
    member _.Test_getScsiCmdStatNameFromValue() =
        Assert.True(( "GOOD" = Constants.getScsiCmdStatNameFromValue ScsiCmdStatCd.GOOD ))
        Assert.True(( "CHECK_CONDITION" = Constants.getScsiCmdStatNameFromValue ScsiCmdStatCd.CHECK_CONDITION ))
        Assert.True(( "CONDITION_MET" = Constants.getScsiCmdStatNameFromValue ScsiCmdStatCd.CONDITION_MET ))
        Assert.True(( "BUSY" = Constants.getScsiCmdStatNameFromValue ScsiCmdStatCd.BUSY ))
        Assert.True(( "INTERMEDIATE" = Constants.getScsiCmdStatNameFromValue ScsiCmdStatCd.INTERMEDIATE ))
        Assert.True(( "INTERMEDIATE_CONDITION_MET" = Constants.getScsiCmdStatNameFromValue ScsiCmdStatCd.INTERMEDIATE_CONDITION_MET ))
        Assert.True(( "RESERVATION_CONFLICT" = Constants.getScsiCmdStatNameFromValue ScsiCmdStatCd.RESERVATION_CONFLICT ))
        Assert.True(( "TASK_SET_FULL" = Constants.getScsiCmdStatNameFromValue ScsiCmdStatCd.TASK_SET_FULL ))
        Assert.True(( "ACA_ACTIVE" = Constants.getScsiCmdStatNameFromValue ScsiCmdStatCd.ACA_ACTIVE ))
        Assert.True(( "TASK_ABORTED" = Constants.getScsiCmdStatNameFromValue ScsiCmdStatCd.TASK_ABORTED ))
