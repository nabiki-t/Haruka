//=============================================================================
// Haruka Software Storage.
// SendErrorStatusTest.fs : Test cases for SendErrorStatusTest class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.BlockDeviceLU

//=============================================================================
// Import declaration

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.BlockDeviceLU
open Haruka.Test

//=============================================================================
// Class implementation

type SendErrorStatusTest_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let createDefTask ( dSense : bool ) ( m_RespCode : iScsiSvcRespCd ) ( m_StatCode : ScsiCmdStatCd ) ( m_SenseData : SenseData ) = 
        let lu = new CInternalLU_Stub()
        let t =
            new SendErrorStatusTask(
                new CStatus_Stub(),
                {
                    I_TNexus = new ITNexus( "INIT", isid_me.zero, "TARG", tpgt_me.zero );
                    CID = cid_me.fromPrim 0us;
                    ConCounter = concnt_me.fromPrim 0;
                    TSIH = tsih_me.fromPrim 0us;
                    ProtocolService = new CProtocolService_Stub();
                    SessionKiller = new HKiller()
                },
                {
                    I = false;
                    F = true;
                    R = false;
                    W = false;
                    ATTR = TaskATTRCd.SIMPLE_TASK;
                    LUN = lun_me.zero
                    InitiatorTaskTag = itt_me.fromPrim 0u;
                    ExpectedDataTransferLength = 0u;
                    CmdSN = cmdsn_me.zero;
                    ExpStatSN = statsn_me.zero;
                    ScsiCDB = Array.empty;
                    DataSegment = PooledBuffer.Empty;
                    BidirectionalExpectedReadDataLength = 0u;
                    ByteCount = 0u;
                },
                lu,
                dSense,
                m_RespCode,
                m_StatCode,
                m_SenseData
            ) :> IBlockDeviceTask
        t, lu

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Execute_001() =
        let s = SenseData(
                    true,
                    SenseKeyCd.ILLEGAL_REQUEST,
                    ASCCd.INVALID_FIELD_IN_CDB,
                    None,
                    None,
                    None,
                    None,
                    None,
                    None
        )
        let t, ilu = createDefTask true iScsiSvcRespCd.COMMAND_COMPLETE ScsiCmdStatCd.GOOD s
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        let mutable cnt = 0
        let mutable cnt1 = 0
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat sensedata _ _ _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v =
                [|
                    yield 0x72uy;   // RESPONSE CODE
                    yield ( byte SenseKeyCd.ILLEGAL_REQUEST >>> 0 ) &&& 0x0Fuy;  //  SENSE KEY
                    yield ( uint16 ASCCd.INVALID_FIELD_IN_CDB ) >>> 8 |> byte; // ADDITIONAL SENSE CODE
                    yield ( uint16 ASCCd.INVALID_FIELD_IN_CDB ) &&& 0x00FFus |> byte;
                    yield 0uy;  //  Reserved
                    yield 0uy;
                    yield 0uy;
                    yield 0uy;  // ADDITIONAL SENSE LENGTH
                |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray sensedata v ))
            cnt <- cnt + 1
        )

        ilu.p_NotifyTerminateTask <- ( fun _ ->
            cnt1 <- cnt1 + 1
        )

        t.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Execute_002() =
        let s = SenseData(
                    true,
                    SenseKeyCd.ILLEGAL_REQUEST,
                    ASCCd.INVALID_FIELD_IN_CDB,
                    None,
                    None,
                    None,
                    None,
                    None,
                    None
        )
        let t, ilu = createDefTask false iScsiSvcRespCd.COMMAND_COMPLETE ScsiCmdStatCd.GOOD s
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        let mutable cnt = 0
        let mutable cnt1 = 0
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat sensedata _ _ _ ->
            cnt <- cnt + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun _ ->
            cnt1 <- cnt1 + 1
        )

        t.NotifyTerminate false
        t.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt = 0 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.NotifyTerminate_001() =
        let mutable cnt = 0
        let s = SenseData(
                    true,
                    SenseKeyCd.ILLEGAL_REQUEST,
                    ASCCd.INVALID_FIELD_IN_CDB,
                    None,
                    None,
                    None,
                    None,
                    None,
                    None
        )
        let t, ilu = createDefTask false iScsiSvcRespCd.COMMAND_COMPLETE ScsiCmdStatCd.GOOD s
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ _ _ _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.TASK_ABORTED ))
            cnt <- cnt + 1
        )
        t.NotifyTerminate true
        Assert.True(( cnt = 1 ))

        t.NotifyTerminate true
        Assert.True(( cnt = 1 ))

    [<Fact>]
    member _.NotifyTerminate_002() =
        let mutable cnt = 0
        let s = SenseData(
                    true,
                    SenseKeyCd.ILLEGAL_REQUEST,
                    ASCCd.INVALID_FIELD_IN_CDB,
                    None,
                    None,
                    None,
                    None,
                    None,
                    None
        )
        let t, ilu = createDefTask false iScsiSvcRespCd.COMMAND_COMPLETE ScsiCmdStatCd.GOOD s
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt <- cnt + 1
        )

        t.NotifyTerminate false
        Assert.True(( cnt = 0 ))

        t.NotifyTerminate true
        Assert.True(( cnt = 0 ))

    [<Fact>]
    member _.NotifyTerminate_003() =
        let s = SenseData(
                    true,
                    SenseKeyCd.ILLEGAL_REQUEST,
                    ASCCd.INVALID_FIELD_IN_CDB,
                    None,
                    None,
                    None,
                    None,
                    None,
                    None
        )
        let t, ilu = createDefTask true iScsiSvcRespCd.COMMAND_COMPLETE ScsiCmdStatCd.GOOD s
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        let mutable cnt = 0
        let mutable cnt1 = 0
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat sensedata _ _ _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v =
                [|
                    yield 0x72uy;   // RESPONSE CODE
                    yield ( byte SenseKeyCd.ILLEGAL_REQUEST >>> 0 ) &&& 0x0Fuy;  //  SENSE KEY
                    yield ( uint16 ASCCd.INVALID_FIELD_IN_CDB ) >>> 8 |> byte; // ADDITIONAL SENSE CODE
                    yield ( uint16 ASCCd.INVALID_FIELD_IN_CDB ) &&& 0x00FFus |> byte;
                    yield 0uy;  //  Reserved
                    yield 0uy;
                    yield 0uy;
                    yield 0uy;  // ADDITIONAL SENSE LENGTH
                |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray sensedata v ))
            cnt <- cnt + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun _ ->
            cnt1 <- cnt1 + 1
        )

        t.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))

        t.NotifyTerminate true
        Assert.True(( cnt = 1 ))


    [<Fact>]
    member _.NotifyTerminate_004() =
        let s = SenseData(
                    true,
                    SenseKeyCd.ILLEGAL_REQUEST,
                    ASCCd.INVALID_FIELD_IN_CDB,
                    None,
                    None,
                    None,
                    None,
                    None,
                    None
        )
        let t, ilu = createDefTask true iScsiSvcRespCd.COMMAND_COMPLETE ScsiCmdStatCd.GOOD s
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        let mutable cnt = 0
        let mutable cnt1 = 0
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat sensedata _ _ _ ->
            cnt <- cnt + 1
            let source =  {
                    I_TNexus = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
                    CID = cid_me.fromPrim 0us;
                    ConCounter = concnt_me.fromPrim 0;
                    TSIH = tsih_me.fromPrim 0us;
                    ProtocolService = new CProtocolService_Stub()
                    SessionKiller = HKiller()
                }
            raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_MESSAGE_ERROR, "" )
        )
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt1 <- cnt1 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_MESSAGE_ERROR ))
            | _ ->
                Assert.Fail __LINE__
        )

        t.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))
