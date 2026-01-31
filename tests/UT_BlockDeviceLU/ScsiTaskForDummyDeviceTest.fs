//=============================================================================
// Haruka Software Storage.
// ScsiTaskForDummyDeviceTest.fs : Test cases for ScsiTaskForDummyDevice class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open System.IO
open System.Text
open System.Collections.Immutable

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.BlockDeviceLU
open Haruka.IODataTypes
open Haruka.Test

//=============================================================================
// Type definition


//=============================================================================
// Class implementation

type ScsiTaskForDummyDeviceTest_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let defaultSCSICommandPDU = {
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
    }

    let defaultSCSIDataOutPDU = {
        F = true;
        LUN = lun_me.zero;
        InitiatorTaskTag = itt_me.fromPrim 0u;
        TargetTransferTag = ttt_me.fromPrim 0u;
        ExpStatSN = statsn_me.zero;
        DataSN = datasn_me.zero;
        BufferOffset = 0u;
        DataSegment = PooledBuffer.Empty;
        ByteCount = 0u;
    }

    let defaultSessionParam = {
            MaxConnections = Constants.NEGOPARAM_MaxConnections;
            TargetGroupID = tgid_me.Zero;
            TargetConf = {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetName = "targetname0";
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

    let createDefScsiTaskWithPRManager ( cmd : SCSICommandPDU ) ( cdb : ICDB ) ( data : SCSIDataOutPDU list ) ( acaNoncompliant : bool ) ( prFName : string ) =
        let media = new CMedia_Stub(
            p_GetBlockCount = ( fun () -> 512UL )
        )
        let stat = new CStatus_Stub(
            p_GetTargetFromLUN = ( fun lun ->
                [
                    {
                        IdentNumber = tnodeidx_me.fromPrim 0u;
                        TargetName = "target000";
                        TargetAlias = "";
                        TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                        LUN = [ lun_me.zero; lun_me.fromPrim 1UL; ];
                        Auth = TargetGroupConf.T_Auth.U_CHAP(
                            {
                                InitiatorAuth = {
                                    UserName = "";
                                    Password = "";
                                };
                                TargetAuth = {
                                    UserName = "";
                                    Password = "";
                                }
                            }
                        );
                    };
                ]
            ),
            p_GetITNexusFromLUN = ( fun lun ->
                [|
                    new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
                    new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us );
                |]
            )
        )
        let lu = new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero )
        let t =
            new ScsiTaskForDummyDevice(
                stat,
                {
                    I_TNexus = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
                    CID = cid_me.fromPrim 0us;
                    ConCounter = concnt_me.fromPrim 0;
                    TSIH = tsih_me.fromPrim 0us;
                    ProtocolService = new CProtocolService_Stub();
                    SessionKiller = new HKiller()
                },
                cmd,
                cdb,
                data,
                lu,
                media,
                new ModeParameter(
                    media,
                    lun_me.zero
                ),
                new PRManager( stat, lu, lun_me.zero, prFName, new HKiller() ),
                acaNoncompliant
            ) :> IBlockDeviceTask
        t, lu

    let createDefScsiTaskForDummyDevice ( cmd : SCSICommandPDU ) ( cdb : ICDB ) ( data : SCSIDataOutPDU list ) ( acaNoncompliant : bool ) =
        createDefScsiTaskWithPRManager cmd cdb data acaNoncompliant ""

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    member _.CreateTestDir ( caseName : string ) =
        let w1 = Functions.AppendPathName ( Path.GetTempPath() ) ( "ScsiTaskForDummyDeviceTest_Test" + caseName )
        GlbFunc.CreateDir w1 |> ignore
        w1
   

    ///////////////////////////////////////////////////////////////////////////
    // Test cases 

    static member m_Execute_001_data = [|
        [| ChangeAliases :> obj |];
        [| ExtendedCopy :> obj |];
        [| LogSelect :> obj |];
        [| LogSense :> obj |];
        [| PreventAllowMediumRemoval :> obj |];
        [| ReadAttribute :> obj |];
        [| ReadBuffer :> obj |];
        [| ReadMediaSerialNumber :> obj |];
        [| ReceiveCopyResults :> obj |];
        [| ReceiveDiagnosticResults :> obj |];
        [| ReportAliases :> obj |];
        [| ReportDeviceIdentifier :> obj |];
        [| ReportPriority :> obj |];
        [| ReportTargetPortGroups :> obj |];
        [| ReportTimestamp :> obj |];
        [| SendDiagnostic :> obj |];
        [| SetDeviceIdentifier :> obj |];
        [| SetPriority :> obj |];
        [| SetTargetPortGroups :> obj |];
        [| SetTimestamp :> obj |];
        [| WriteAttribute :> obj |];
        [| WriteBuffer :> obj |];
        [| AccessControlIn :> obj |];
        [| AccessControlOut :> obj |];
        [| FormatUnit :> obj |];
        [| PreFetch :> obj |];
        [| Read :> obj |];
        [| ReadDefectData :> obj |];
        [| ReadLong :> obj |];
        [| ReassignBlocks :> obj |];
        [| StartStopUnit :> obj |];
        [| Verify :> obj |];
        [| Write :> obj |];
        [| WriteAndVerify :> obj |];
        [| WriteLong :> obj |];
        [| WriteSame :> obj |];
        [| XDRead :> obj |];
        [| XDWrite :> obj |];
        [| XDWriteRead :> obj |];
        [| XPWrite :> obj |];
    |]

    [<Theory>]
    [<MemberData( "m_Execute_001_data" )>]
    member _.Execute_001 ( cdb : CDBTypes ) =
        let s = {
                m_Type = cdb;
                m_OperationCode = 0uy;
                m_ServiceAction = 0us;
                m_NACA = false;
                m_LINK = false;
        }
        let t, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false 

        let mutable cnt = 0
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt <- cnt + 1
            Assert.True(( argTask = t ))
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_COMMAND_OPERATION_CODE ))
            | _ ->
                Assert.Fail __LINE__
        )

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt = 1 ))

    [<Fact>]
    member _.Inquiry_001() =
        let mutable cnt = 0
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = true;
            PageCode = 0x80uy;  //  Unit Serial Number VPD page
            AllocationLength = 0xFFFFus;
            Control = 0uy;
        }
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let t, ilu = createDefScsiTaskForDummyDevice scsiCommand s [ scsiDataOut ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ indata alloclen _ ->
            Assert.True(( recvdl = 30u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x0Cuy;   // PERIPHERAL QUALIFIER(0b) PERIPHERAL DEVICE TYPE(0C)
                0x80uy;   // PAGE CODE(0x80)
                0x00uy;   // Reserved
                0x04uy;   // PAGE LENGTH(4)
                0x20uy;   // PRODUCT SERIAL NUMBER
                0x20uy;   // PRODUCT SERIAL NUMBER
                0x20uy;   // PRODUCT SERIAL NUMBER
                0x20uy;   // PRODUCT SERIAL NUMBER
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0xFFFFu ))
            cnt <- cnt + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Inquiry_002() =
        let mutable cnt = 0
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = true;
            PageCode = 0x83uy;
            AllocationLength = 0xFFFFus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x0Cuy; // PERIPHERAL QUALIFIER, PERIPHERAL DEVICE TYPE
                0x83uy; // PAGE CODE
                0x00uy; 0x28uy; // PAGE LENGTH

                // DISCRIPTOR 2 ( 24 bytes )
                0x53uy; // PROTOCOL IDENTIFIER(5h)  CODE SET(3h)
                0x98uy; // PIV(1) ASSOCIATION(01b) IDENTIFIER TYPE(8h)
                0x00uy; // Reserved
                let str2 = 
                    ( "targetname0,t,0x0000" )
                    |> Encoding.UTF8.GetBytes
                    |> Functions.PadBytesArray 4 256
                ( byte str2.Length ); // IDENTIFIER LENGTH
                yield! str2;

                // DISCRIPTOR 3 ( 16 bytes )
                0x53uy; // PROTOCOL IDENTIFIER(5h)  CODE SET(3h)
                0xA8uy; // PIV(1) ASSOCIATION(10b) IDENTIFIER TYPE(8h)
                0x00uy; // Reserved
                let str3 = 
                    ( "targetname0" )
                    |> Encoding.UTF8.GetBytes
                    |> Functions.PadBytesArray 4 256
                ( byte str3.Length ); // IDENTIFIER LENGTH
                yield! str3;
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0xFFFFu ))
            cnt <- cnt + 1
        )
        psStub.p_GetSessionParameter <- ( fun () -> defaultSessionParam )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Inquiry_003() =
        let mutable cnt = 0
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = true;
            PageCode = 0x86uy;
            AllocationLength = 0xEEEEus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x0Cuy; // PERIPHERAL QUALIFIER, PERIPHERAL DEVICE TYPE
                0x86uy; // PAGE CODE
                0x00uy; // Reserved
                0x3Cuy; // PAGE LENGTH
                0x00uy; // RTO(0) GRD_CHK(0) APP_CHK REF_CHK(0)
                0x07uy; // GROUP_SUP(0) PRIOR_SUP(0) HEADSUP(1) ORDSUP(1) SIMPSUP(1)
                0x00uy; // NV_SUP(0) V_SUP(0)
                0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0xEEEEu ))
            cnt <- cnt + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun _ -> cnt1 <- cnt1 + 1 )

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Inquiry_004() =
        let mutable cnt = 0
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = true;
            PageCode = 0xB0uy;  //  Block Limits VPD page
            AllocationLength = 0xEEEEus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x0Cuy;   // PERIPHERAL QUALIFIER(0) PERIPHERAL DEVICE TYPE(0)
                0xB0uy;   // PAGE CODE(0xB0)
                0x00uy;   // Reserved
                0x0Cuy;   // PAGE LENGTH
                0x00uy;   // Reserved
                0x00uy;   // Reserved
                0x00uy;   // OPTIMAL TRANSFER LENGTH GRAMULARITY
                0x01uy;
                0x00uy;   // MAXIMUM TRANSFER LENGTH
                0x00uy;
                0x00uy;
                0x00uy;
                0x00uy;   // OPTIMAL TRANSFER LENGTH
                0x00uy;
                0x00uy;
                0x08uy;
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0xEEEEu ))
            cnt <- cnt + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun _ -> cnt1 <- cnt1 + 1 )

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Inquiry_005() =
        let mutable cnt = 0
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = true;
            PageCode = 0xB1uy;  // Block Device Characteristics VPD page
            AllocationLength = 0xEEEEus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x0Cuy;   // PERIPHERAL QUALIFIER(0b) PERIPHERAL DEVICE TYPE(0Ch)
                0xB1uy;   // PAGE CODE(0xB1)
                0x00uy;   // PAGE LENGTH(0x003C)
                0x3Cuy;   // PAGE LENGTH
                0x00uy;   // MEDIUM ROTATION RATE(0x0000)
                0x00uy;   // MEDIUM ROTATION RATE
                0x00uy;   // PRODUCT TYPE(0x00)
                0x00uy;   // WABEREQ(0)/WACEREQ(0)/NOMINAL FORM FACTOR(0)
                0x00uy;   // FUAB(0)/VBULS(0)
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy;
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0xEEEEu ))
            cnt <- cnt + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun _ -> cnt1 <- cnt1 + 1 )

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Inquiry_006() =
        let mutable cnt = 0
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = true;
            PageCode = 0x00uy;
            AllocationLength = 0xDDDDus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x0Cuy; // PERIPHERAL QUALIFIER, PERIPHERAL DEVICE TYPE
                0x00uy; // PAGE CODE
                0x00uy; // Reserved
                0x06uy; // PAGE LENGTH
                0x00uy; 0x80uy; 0x83uy; 0x86uy; 0xB0uy; 0xB1uy;
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0xDDDDu ))
            cnt <- cnt + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun _ -> cnt1 <- cnt1 + 1 )

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Inquiry_007() =
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = true;
            PageCode = 0x01uy;
            AllocationLength = 0xDDDDus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt1 <- cnt1 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            | _ ->
                Assert.Fail __LINE__
        )

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Inquiry_008() =
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = false;
            PageCode = 0x01uy;
            AllocationLength = 0xDDDDus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt1 <- cnt1 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            | _ ->
                Assert.Fail __LINE__
        )

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Inquiry_009() =
        let mutable cnt = 0
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = false;
            PageCode = 0x00uy;
            AllocationLength = 0xCCCCus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x0Cuy; // PERIPHERAL QUALIFIER, PERIPHERAL DEVICE TYPE
                0x00uy; // RMB
                0x05uy; // VERSION(5)
                0x22uy; // NORMACA(1) HISUP(0) RESPONSE DATA FORMAT(2)
                0x5Cuy; // ADDITIONAL LENGTH( 96 bytes length - 4 )
                0x00uy; // SCCS(0) ACC(0) TPGS(00b) 3PC(0) PROTECT(0)
                0x10uy; // BQUE(0) ENCSERV(0) VS(0) MULTIP(1) MCHNGR(0) ADDR16(0)
                0x02uy; // WBUS16(0) SYNC(0) LINKED(0) CMDQUE(1) VS(0)
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // T10 VENDOR IDENTIFICATION
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                yield! ( "HARUKA S.S." )
                        |> Encoding.UTF8.GetBytes
                        |> Functions.PadBytesArray 16 16
                yield! ( "100" )
                        |> Encoding.UTF8.GetBytes
                        |> Functions.PadBytesArray 4 4
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; // CLOCKING(0) QAS(0) IUS(0)
                0x00uy; // Reserved
                0x00uy; 0x40uy; // VERSION DESCRIPTOR 1(SAM-2)
                0x09uy; 0x60uy; // VERSION DESCRIPTOR 2(iSCSI)
                0x09uy; 0x60uy; // VERSION DESCRIPTOR 3(iSCSI)
                0x03uy; 0x00uy; // VERSION DESCRIPTOR 4(SPC-3)
                0x01uy; 0xE0uy; // VERSION DESCRIPTOR 5(SCC-2)
                0x00uy; 0x00uy; // VERSION DESCRIPTOR 6
                0x00uy; 0x00uy; // VERSION DESCRIPTOR 7
                0x00uy; 0x00uy; // VERSION DESCRIPTOR 8
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0xCCCCu ))
            cnt <- cnt + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun _ -> cnt1 <- cnt1 + 1 )

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Inquiry_010() =
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = false;
            PageCode = 0x00uy;
            AllocationLength = 0xDDDDus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        t.NotifyTerminate false

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.ModeSelect_001() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let select_cdb = {
            OperationCode = 0x15uy;
            PF = true;
            SP = true;
            ParameterListLength = 12us;
            Control = 0uy;
        }
        let data = [
            {
                defaultSCSIDataOutPDU with
                    DataSegment =
                        let v = [|
                            0x00uy; 0x00uy; 0x00uy; 0x08uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER, BLOCK DESCRIPTOR LENGTH
                            0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                            0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
                            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // Dummy buffer
                        |]
                        PooledBuffer.Rent( v, 12 )
            }
        ]
        let select_task, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU select_cdb data false
        let select_psStub = select_task.Source.ProtocolService :?> CProtocolService_Stub
        select_psStub.p_SendSCSIResponse <- ( fun _ _ _ recvLen resp stat _ indata alloclen _ ->
            cnt1 <- cnt1 + 1
            Assert.True(( recvLen = 12u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )
        ilu.p_NotifyTerminateTask <- ( fun _ ->  cnt2 <- cnt2 + 1 )

        let pc_dummy = new PrivateCaller( select_task )
        let internalScsiTask = pc_dummy.GetField( "m_ScsiTask" ) :?> ScsiTask
        let pc_st = new PrivateCaller( internalScsiTask )
        let select_ModeParameter = pc_st.GetField( "m_ModeParameter" ) :?> ModeParameter

        select_task.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))
        Assert.True(( select_ModeParameter.BlockLength = 0x00AABBCCUL ))

    [<Fact>]
    member _.ModeSense_001() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let sense_cdb = {
            OperationCode = 0x1Auy;
            LLBAA = false;
            DBD = false;
            PC = 0uy;
            PageCode = 0x0Auy;
            SubPageCode = 0uy;
            AllocationLength = 0xFFus;
            Control = 0uy;
        }
        let sense_task, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU sense_cdb [] false

        let sense_psStub = sense_task.Source.ProtocolService :?> CProtocolService_Stub
        sense_psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x17uy; 0x00uy; 0x00uy; 0x08uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER, BLOCK DESCRIPTOR LENGTH
                0x00uy; 0x00uy; 0xAAuy; 0xBBuy; // BLOCK COUNT
                yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK LENGTH
                0x0Auy;                         // PS, SPF, PAGE CODE
                0x0Auy;                         // PAGE LENGTH
                0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
                0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
                0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
                0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
                0x00uy; 0x00uy;                 // Obsolute
                0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
                0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0xFFu ))
        )
        ilu.p_NotifyTerminateTask <- ( fun _ -> cnt1 <- cnt1 + 1 )

        let pc_dummy = new PrivateCaller( sense_task )
        let internalScsiTask = pc_dummy.GetField( "m_ScsiTask" ) :?> ScsiTask
        let media_stub = internalScsiTask.Media :?> CMedia_Stub
        media_stub.p_GetBlockCount <- ( fun () -> 0xAABBUL )
        media_stub.p_GetWriteProtect <- ( fun () -> false )

        sense_task.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportLUNs_001() =
        let mutable cnt1 = 0
        let cdb = {
            OperationCode = 0x0Auy;
            SelectReport = 0x00uy;
            AllocationLength = 15u;
            Control = 0uy;
        }
        let sense_task, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU cdb [] false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt1 <- cnt1 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            | _ ->
                Assert.Fail __LINE__
        )

        sense_task.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))


    static member m_ReportSupportedOperationCodes_001_data = [|
        [|
            0x00uy :> obj;  0x00uy :> obj; 0x00us :> obj;
            SupportedOperationCodeConst.SupportedOperationCommandsDummyDevice :> obj;
        |]
        [|
            0x01uy :> obj;  0x12uy :> obj; 0x00us :> obj;
            SupportedOperationCodeConst.CdbUsageData_INQUIRY :> obj;
        |]
        [|
            0x01uy :> obj;  0x15uy :> obj; 0x00us :> obj;
            SupportedOperationCodeConst.CdbUsageData_MODE_SELECT_6 :> obj;
        |]
        [|
            0x01uy :> obj;  0x55uy :> obj; 0x00us :> obj;
            SupportedOperationCodeConst.CdbUsageData_MODE_SELECT_10 :> obj;
        |]
        [|
            0x01uy :> obj;  0x1Auy :> obj; 0x00us :> obj;
            SupportedOperationCodeConst.CdbUsageData_MODE_SENSE_6 :> obj;
        |]
        [|
            0x01uy :> obj;  0x5Auy :> obj; 0x00us :> obj;
            SupportedOperationCodeConst.CdbUsageData_MODE_SENSE_10 :> obj;
        |]
        [|
            0x01uy :> obj;  0xA0uy :> obj; 0x00us :> obj;
            SupportedOperationCodeConst.CdbUsageData_REPORT_LUNS :> obj;
        |]
        [|
            0x01uy :> obj;  0x03uy :> obj; 0x00us :> obj;
            SupportedOperationCodeConst.CdbUsageData_REQUEST_SENSE :> obj;
        |]
        [|
            0x01uy :> obj;  0x00uy :> obj; 0x00us :> obj;
            SupportedOperationCodeConst.CdbUsageData_TEST_UNIT_READY :> obj;
        |]
        [|
            0x01uy :> obj;  0x25uy :> obj; 0x00us :> obj;
            SupportedOperationCodeConst.CdbUsageData_READ_CAPACITY_10 :> obj;
        |]
        [|
            0x01uy :> obj;  0x35uy :> obj; 0x00us :> obj;
            SupportedOperationCodeConst.CdbUsageData_SYNCHRONIZE_CACHE_10 :> obj;
        |]
        [|
            0x01uy :> obj;  0x91uy :> obj; 0x00us :> obj;
            SupportedOperationCodeConst.CdbUsageData_SYNCHRONIZE_CACHE_16 :> obj;
        |]
        [|
            0x01uy :> obj;  0xFFuy :> obj; 0x00us :> obj;   // unknown operation code.
            [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |] :> obj;
        |]
        [|
            0x02uy :> obj;  0x5Euy :> obj; 0x00us :> obj;   // PERSISTENT RESERVE IN
            ( SupportedOperationCodeConst.CdbUsageData_PERSISTENT_RESERVE_IN 0x00uy ) :> obj;
        |]
        [|
            0x02uy :> obj;  0x5Euy :> obj; 0x03us :> obj;   // PERSISTENT RESERVE IN
            ( SupportedOperationCodeConst.CdbUsageData_PERSISTENT_RESERVE_IN 0x03uy ) :> obj;
        |]
        [|
            0x02uy :> obj;  0x5Euy :> obj; 0x04us :> obj;   // PERSISTENT RESERVE IN, unknown service action
            [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |] :> obj;
        |]
        [|
            0x02uy :> obj;  0x5Fuy :> obj; 0x00us :> obj;   // PERSISTENT RESERVE OUT
            ( SupportedOperationCodeConst.CdbUsageData_PERSISTENT_RESERVE_OUT 0x00uy ) :> obj;
        |]
        [|
            0x02uy :> obj;  0x5Fuy :> obj; 0x07us :> obj;   // PERSISTENT RESERVE OUT
            ( SupportedOperationCodeConst.CdbUsageData_PERSISTENT_RESERVE_OUT 0x07uy ) :> obj;
        |]
        [|
            0x02uy :> obj;  0x5Fuy :> obj; 0x08us :> obj;   // PERSISTENT RESERVE OUT, unknown service action
            [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |] :> obj;
        |]
        [|
            0x02uy :> obj;  0x9Euy :> obj; 0x10us :> obj;   // READ CAPACITY(16)
            SupportedOperationCodeConst.CdbUsageData_READ_CAPACITY_16 :> obj;
        |]
        [|
            0x02uy :> obj;  0x9Euy :> obj; 0x00us :> obj;   // READ CAPACITY(16), unknown service action
            [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |] :> obj;
        |]
        [|
            0x02uy :> obj;  0xA3uy :> obj; 0x0Cus :> obj;   // REPORT SUPPORTED OPERATION CODES
            SupportedOperationCodeConst.CdbUsageData_REPORT_SUPPORTED_OPERATION_CODES :> obj;
        |]
        [|
            0x02uy :> obj;  0xA3uy :> obj; 0x0Dus :> obj;   // REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS
            SupportedOperationCodeConst.CdbUsageData_REPORT_SUPPORTED_TASK_MANAGEMENT_FUNCTIONS :> obj;
        |]
        [|
            0x02uy :> obj;  0xA3uy :> obj; 0x00us :> obj;   // REPORT SUPPORTED OPERATION CODES / REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS, unknown service action
            [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |] :> obj;
        |]
        [|
            0x02uy :> obj;  0xFFuy :> obj; 0x00us :> obj;   // unknown operation code.
            [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |] :> obj;
        |]
    |]

    [<Theory>]
    [<MemberData( "m_ReportSupportedOperationCodes_001_data" )>]
    member _.ReportSupportedOperationCodes_001 ( argRO : byte ) ( argROC : byte ) ( argRSA : uint16 ) ( rxpResult : byte[] ) =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = argRO;
            RequestedOperationCode = argROC;
            RequestedServiceAction = argRSA;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata rxpResult ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun _ -> cnt1 <- cnt1 + 1 )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Theory>]
    [<InlineData( 0x01uy, 0x5Euy, "REQUESTED OPERATION CODE 0x5E" )>]    // PERSISTENT RESERVE IN
    [<InlineData( 0x01uy, 0x5Fuy, "REQUESTED OPERATION CODE 0x5F" )>]    // PERSISTENT RESERVE OUT
    [<InlineData( 0x01uy, 0x9Euy, "REQUESTED OPERATION CODE 0x9E" )>]    // READ CAPACITY(16)
    [<InlineData( 0x01uy, 0xA3uy, "REQUESTED OPERATION CODE 0xA3" )>]    // REPORT SUPPORTED OPERATION CODES / REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS
    [<InlineData( 0x02uy, 0x12uy, "REQUESTED OPERATION CODE 0x12" )>]    // INQUIRY
    [<InlineData( 0x02uy, 0x15uy, "REQUESTED OPERATION CODE 0x15" )>]    // MODE SELECT(6)
    [<InlineData( 0x02uy, 0x55uy, "REQUESTED OPERATION CODE 0x55" )>]    // MODE SELECT(10)
    [<InlineData( 0x02uy, 0x1Auy, "REQUESTED OPERATION CODE 0x1A" )>]    // MODE SENSE(6)
    [<InlineData( 0x02uy, 0x5Auy, "REQUESTED OPERATION CODE 0x5A" )>]    // MODE SENSE(10)
    [<InlineData( 0x02uy, 0xA0uy, "REQUESTED OPERATION CODE 0xA0" )>]    // REPORT LUNS
    [<InlineData( 0x02uy, 0x03uy, "REQUESTED OPERATION CODE 0x03" )>]    // REQUEST SENSE
    [<InlineData( 0x02uy, 0x00uy, "REQUESTED OPERATION CODE 0x00" )>]    // TEST UNIT READY
    [<InlineData( 0x02uy, 0x25uy, "REQUESTED OPERATION CODE 0x25" )>]    // READ CAPACITY(10)
    [<InlineData( 0x02uy, 0x35uy, "REQUESTED OPERATION CODE 0x35" )>]    // SYNCHRONIZE CACHE(10)
    [<InlineData( 0x02uy, 0x91uy, "REQUESTED OPERATION CODE 0x91" )>]    // SYNCHRONIZE CACHE(16)
    [<InlineData( 0x03uy, 0x00uy, "Invalie REPORTING OPTIONS field value" )>]    // unknown REPORTING OPTIONS
    member _.ReportSupportedOperationCodes_002 ( argRO : byte ) ( argROC : byte ) ( expResult : string ) =
        let mutable cnt1 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = argRO;
            RequestedOperationCode = argROC;
            RequestedServiceAction = 0x00us;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let sense_task, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU cdb [] false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt1 <- cnt1 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
                Assert.True(( x.Message.StartsWith expResult ))
            | _ ->
                Assert.Fail __LINE__
        )

        sense_task.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.ReportSupportedTaskManagementFunctions_001() =
        let mutable cnt1 = 0
        let cdb : ReportSupportedTaskManagementFunctionsCDB = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x0Duy;
            AllocationLength = 3u;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU cdb [] false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt1 <- cnt1 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            | _ ->
                Assert.Fail __LINE__
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.RequestSense_001() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : RequestSenseCDB = {
            OperationCode = 0xA3uy;
            DESC = true;
            AllocationLength = 4uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            let v =
                [|
                    yield 0x72uy;   // RESPONSE CODE
                    yield ( byte SenseKeyCd.NO_SENSE >>> 0 ) &&& 0x0Fuy;  //  SENSE KEY
                    yield ( uint16 ASCCd.NO_ADDITIONAL_SENSE_INFORMATION ) >>> 8 |> byte; // ADDITIONAL SENSE CODE
                    yield ( uint16 ASCCd.NO_ADDITIONAL_SENSE_INFORMATION ) &&& 0x00FFus |> byte;
                    yield 0uy;  //  Reserved
                    yield 0uy;
                    yield 0uy;
                    yield 0uy;  // ADDITIONAL SENSE LENGTH
                |]
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 4u ))

        )
        ilu.p_GetUnitAttention <- ( fun itn ->
            Assert.True(( itn = stask.Source.I_TNexus ))
            ValueNone
        )
        ilu.p_NotifyTerminateTask <- ( fun _ -> cnt1 <- cnt1 + 1 )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.TestUnitReady_001() =
        let mutable cnt1 = 0
        let cdb : TestUnitReadyCDB = {
            OperationCode = 0x00uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU cdb [] false
        ilu.p_NotifyTerminateTaskWithException <- ( fun _ argEx ->
            cnt1 <- cnt1 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.NOT_READY ))
                Assert.True(( x.ASC = ASCCd.MEDIUM_NOT_PRESENT ))
            | _ ->
                Assert.Fail __LINE__
        )
        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.ReadCapacity_001() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : ReadCapacityCDB = {
            OperationCode = 0x25uy;
            ServiceAction = 0x00uy;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0UL;
            PMI = false;
            AllocationLength = 0x10u;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub

        let pc_dummy = new PrivateCaller( stask )
        let internalScsiTask = pc_dummy.GetField( "m_ScsiTask" ) :?> ScsiTask
        let mediaStub = internalScsiTask.Media :?> CMedia_Stub

        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; 0x00uy; 0xAAuy; 0xBAuy; // RETURNED LOGICAL BLOCK ADDRESS
                yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK LENGTH IN BYTE
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0x10u ))
        )
        mediaStub.p_ReadCapacity <- ( fun itt source ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            0xAABBUL
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.SynchronizeCache_001() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : SynchronizeCacheCDB = {
            OperationCode = 0x35uy;
            SyncNV = true;
            IMMED = true;
            LogicalBlockAddress = blkcnt_me.ofUInt64 2UL;
            NumberOfBlocks = blkcnt_me.ofUInt32 3u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let stask, ilu = createDefScsiTaskForDummyDevice scsiCommand cdb [scsiDataOut] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub

        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvdl = 30u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )
        ilu.p_NotifyTerminateTask <- ( fun _ -> cnt1 <- cnt1 + 1 )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member this.PersistentReserveIn_001() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : PersistentReserveInCDB = {
            OperationCode = 0x5Euy;
            ServiceAction = 0x01uy; // READ RESERVATION
            AllocationLength = 128us;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // PRGENERATION
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ADDITIONAL LENGTH
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
        )

        ilu.p_NotifyTerminateTask <- ( fun _ -> cnt1 <- cnt1 + 1 )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member this.PersistentReserveOut_001() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let data = {
            defaultSCSIDataOutPDU with
                DataSegment =
                    let w = [|
                        0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
                        0x00uy; 0x00uy; 0x00uy; 0x00uy;
                        0xFFuy; 0xEEuy; 0xDDuy; 0xCCuy; // SERVICE ACTION RESERVATION KEY  
                        0xBBuy; 0xAAuy; 0x99uy; 0x88uy;
                        0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
                        0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
                        0x00uy;                         // Reserved
                        0x00uy; 0x00uy;                 // Obsolute
                        0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // dummy buffer
                    |]
                    PooledBuffer.Rent( w, w.Length - 4 )
        }
        let cdb : PersistentReserveOutCDB = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0x00uy; // REGISTER
            Scope = 0uy;
            PRType = PR_TYPE.EXCLUSIVE_ACCESS;
            ParameterListLength = PooledBuffer.ulength data.DataSegment;
            Control = 0uy;
        }

        let stask, ilu = createDefScsiTaskForDummyDevice defaultSCSICommandPDU cdb [ data ] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub

        let pc_dummy = new PrivateCaller( stask )
        let internalScsiTask = pc_dummy.GetField( "m_ScsiTask" ) :?> ScsiTask
        let pc_ist = new PrivateCaller( internalScsiTask )
        let prManager = pc_ist.GetField( "m_PRManager" ) :?> PRManager

        psStub.p_SendSCSIResponse <- ( fun _ _ _ resvLen resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resvLen = 24u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )

        ilu.p_NotifyTerminateTask <- ( fun _ -> cnt1 <- cnt1 + 1 )
        ilu.p_LUN <- ( fun () -> lun_me.zero )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

        let pc2 = new PrivateCaller( prManager )
        let prinfo = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey stask.Source.I_TNexus ))