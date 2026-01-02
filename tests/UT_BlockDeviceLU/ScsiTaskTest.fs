//=============================================================================
// Haruka Software Storage.
// ScsiTaskTest.fs : Test cases for ScsiTask class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open System.IO
open System.Text
open System.Threading.Tasks
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

type ScsiTask_Test () =

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
            new ScsiTask(
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

    let createDefScsiTask ( cmd : SCSICommandPDU ) ( cdb : ICDB ) ( data : SCSIDataOutPDU list ) ( acaNoncompliant : bool ) =
        createDefScsiTaskWithPRManager cmd cdb data acaNoncompliant ""

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    member _.CreateTestDir ( caseName : string ) =
        let w1 = Functions.AppendPathName ( Path.GetTempPath() ) ( "ScsiTaskTest_Test" + caseName )
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
        [| ReadDefectData :> obj |];
        [| ReadLong :> obj |];
        [| ReassignBlocks :> obj |];
        [| StartStopUnit :> obj |];
        [| Verify :> obj |];
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
        let t, ilu = createDefScsiTask defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false 

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
    member _.NotifyTerminate_001() =
        let mutable cnt = 0
        let s = {
            OperationCode = 12uy;
            EVPD = true;
            PageCode = 0x83uy;
            AllocationLength = 0xFFFFus;
            Control = 0uy;
        }
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let t, ilu = createDefScsiTask scsiCommand s [ scsiDataOut ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ _ _ _ ->
            Assert.True(( recvdl = 30u ))
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
        let s = {
            OperationCode = 12uy;
            EVPD = true;
            PageCode = 0x83uy;
            AllocationLength = 0xFFFFus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTask defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
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
        let mutable cnt = 0
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = true;
            PageCode = 0x83uy;
            AllocationLength = 0xFFFFus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTask defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ _ _ _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            cnt <- cnt + 1
        )
        psStub.p_GetSessionParameter <- ( fun () ->
            {
                defaultSessionParam with
                    TargetConf = {
                        defaultSessionParam.TargetConf with
                            TargetName = "TARGET_NAME";
                    }
                    TargetPortalGroupTag = tpgt_me.zero;
            }
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )
        t.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))

        t.NotifyTerminate true
        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))

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
        let t, ilu = createDefScsiTask scsiCommand s [ scsiDataOut ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ indata alloclen _ ->
            Assert.True(( recvdl = 30u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy;   // PERIPHERAL QUALIFIER(0) PERIPHERAL DEVICE TYPE(0)
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
        psStub.p_GetSessionParameter <- ( fun () ->
            {
                defaultSessionParam with
                    TargetConf = {
                        defaultSessionParam.TargetConf with
                            TargetName = "targetname0";
                    }
                    TargetPortalGroupTag = tpgt_me.zero;
            }
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
        let t, ilu = createDefScsiTask defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; // PERIPHERAL QUALIFIER, PERIPHERAL DEVICE TYPE
                0x83uy; // PAGE CODE
                0x00uy; 0x4Cuy; // PAGE LENGTH

                // DISCRIPTOR 1 ( 36 bytes )
                0x03uy; // PROTOCOL IDENTIFIER(0h)  CODE SET(3h)
                0x08uy; // PIV(0), ASSOCIATION(0), IDENTIFIER TYPE(8)
                0x00uy; // Reserved
                let str1 = 
                    ( "targetname0,L,0x0000000000000000" )
                    |> Encoding.UTF8.GetBytes
                    |> Functions.PadBytesArray 4 256
                ( byte str1.Length ); // IDENTIFIER LENGTH
                yield! str1;

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
        psStub.p_GetSessionParameter <- ( fun () ->
            {
                defaultSessionParam with
                    TargetConf = {
                        defaultSessionParam.TargetConf with
                            TargetName = "targetname0";
                    }
                    TargetPortalGroupTag = tpgt_me.zero;
            }
        )
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
        let t, ilu = createDefScsiTask defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; // PERIPHERAL QUALIFIER, PERIPHERAL DEVICE TYPE
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
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

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
        let t, ilu = createDefScsiTask defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        ilu.p_OptimalTransferLength <- ( fun () -> blkcnt_me.ofUInt32 0x08u )
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy;   // PERIPHERAL QUALIFIER(0) PERIPHERAL DEVICE TYPE(0)
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
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

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
            PageCode = 0x00uy;
            AllocationLength = 0xDDDDus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTask defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; // PERIPHERAL QUALIFIER, PERIPHERAL DEVICE TYPE
                0x00uy; // PAGE CODE
                0x00uy; // Reserved
                0x06uy; // PAGE LENGTH
                0x00uy; 0x80uy; 0x83uy; 0x86uy; 0xB0uy; 0xB1uy;
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0xDDDDu ))
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
    member _.Inquiry_006() =
        let mutable cnt = 0
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = true;
            PageCode = 0x01uy;
            AllocationLength = 0xDDDDus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTask defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt <- cnt + 1
        )
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

        Assert.True(( cnt = 0 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Inquiry_007() =
        let mutable cnt = 0
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = false;
            PageCode = 0x01uy;
            AllocationLength = 0xDDDDus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTask defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt <- cnt + 1
        )
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

        Assert.True(( cnt = 0 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Inquiry_008() =
        let mutable cnt = 0
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = false;
            PageCode = 0x00uy;
            AllocationLength = 0xCCCCus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTask defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; // PERIPHERAL QUALIFIER, PERIPHERAL DEVICE TYPE
                0x00uy; // RMB
                0x05uy; // VERSION(5)
                0x22uy; // NORMACA(1) HISUP(0) RESPONSE DATA FORMAT(2)
                0x5Buy; // ADDITIONAL LENGTH( 96 bytes length - 4 )
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
                0x03uy; 0x20uy; // VERSION DESCRIPTOR 5(SBC-2)
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
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Inquiry_009() =
        let mutable cnt = 0
        let mutable cnt1 = 0
        let s = {
            OperationCode = 12uy;
            EVPD = false;
            PageCode = 0x00uy;
            AllocationLength = 0xDDDDus;
            Control = 0uy;
        }
        let t, ilu = createDefScsiTask defaultSCSICommandPDU s [ defaultSCSIDataOutPDU ] false
        let psStub = t.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt <- cnt + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        t.NotifyTerminate false

        t.Execute()()
        |> Functions.RunTaskSynchronously

        Assert.True(( cnt = 0 ))
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
        let select_task, ilu = createDefScsiTask defaultSCSICommandPDU select_cdb data false
        let select_psStub = select_task.Source.ProtocolService :?> CProtocolService_Stub
        select_psStub.p_SendSCSIResponse <- ( fun _ _ _ recvLen resp stat _ indata alloclen _ ->
            cnt1 <- cnt1 + 1
            Assert.True(( recvLen = 12u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt2 <- cnt2 + 1
        )

        let pc = new PrivateCaller( select_task )
        let select_ModeParameter = pc.GetField( "m_ModeParameter" ) :?> ModeParameter

        select_task.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))
        Assert.True(( select_ModeParameter.BlockLength = 0x00AABBCCUL ))

    [<Fact>]
    member _.ModeSelect_002() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let select_cdb = {
            OperationCode = 0x55uy;
            PF = true;
            SP = true;
            ParameterListLength = 24us;
            Control = 0uy;
        }
        let cmd = {
            defaultSCSICommandPDU with
                DataSegment =
                    let v = [|
                        0x00uy; 0x00uy; 0x00uy; 0x00uy;
                        0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
                        0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                        0xAAuy; 0xAAuy;                 // dummy buffer
                    |]
                    PooledBuffer.Rent( v, 12 )
        }
        let data = [
            {
                defaultSCSIDataOutPDU with
                    BufferOffset = 12u;
                    DataSegment = 
                        let v = [|
                            0x11uy; 0x22uy; 0x33uy; 0x44uy;
                            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                            0xFFuy; 0xEEuy; 0xDDuy; 0xCCuy; // BLOCK LENGTH
                            0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // dummy buffer
                        |]
                        PooledBuffer.Rent( v, 12 )
            }
        ]
        let select_task, ilu = createDefScsiTask cmd select_cdb data false
        let select_psStub = select_task.Source.ProtocolService :?> CProtocolService_Stub
        select_psStub.p_SendSCSIResponse <- ( fun _ _ _ recvLen resp stat _ indata alloclen _ ->
            cnt1 <- cnt1 + 1
            Assert.True(( recvLen = 24u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt2 <- cnt2 + 1
        )
        let pc = new PrivateCaller( select_task )
        let select_ModeParameter = pc.GetField( "m_ModeParameter" ) :?> ModeParameter

        select_task.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))
        Assert.True(( select_ModeParameter.BlockLength = 0xFFEEDDCCUL ))

    [<Fact>]
    member _.ModeSelect_003() =
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
                    DataSegment = [|
                        0x00uy; 0x00uy; 0x00uy; 0x08uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER, BLOCK DESCRIPTOR LENGTH
                        0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                        0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
                    |] |> PooledBuffer.Rent
            }
        ]
        let select_task, ilu = createDefScsiTask defaultSCSICommandPDU select_cdb data false
        let select_psStub = select_task.Source.ProtocolService :?> CProtocolService_Stub
        select_psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt1 <- cnt1 + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt2 <- cnt2 + 1
        )
        let pc = new PrivateCaller( select_task )
        let select_ModeParameter = pc.GetField( "m_ModeParameter" ) :?> ModeParameter

        select_task.NotifyTerminate false
        select_task.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 0 ))
        Assert.True(( cnt2 = 1 ))
        Assert.True(( select_ModeParameter.BlockLength = 0x00AABBCCUL ))

    [<Fact>]
    member _.ModeSelect_004() =
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
                    DataSegment = [|
                        0x00uy; 0x00uy; 0x00uy; 0x08uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER, BLOCK DESCRIPTOR LENGTH
                        0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                        0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
                    |] |> PooledBuffer.Rent
            }
        ]
        let select_task, ilu = createDefScsiTask defaultSCSICommandPDU select_cdb data false
        let select_psStub = select_task.Source.ProtocolService :?> CProtocolService_Stub
        select_psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt1 <- cnt1 + 1
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
            cnt2 <- cnt2 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_MESSAGE_ERROR ))
            | _ ->
                Assert.Fail __LINE__
        )

        select_task.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

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
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let sense_task, ilu = createDefScsiTask scsiCommand sense_cdb [ scsiDataOut ] false

        let sense_psStub = sense_task.Source.ProtocolService :?> CProtocolService_Stub
        sense_psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvdl = 30u ))
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
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        let media_stub = ( sense_task :?> ScsiTask ).Media :?> CMedia_Stub
        media_stub.p_GetBlockCount <- ( fun () -> 0xAABBUL )
        media_stub.p_GetWriteProtect <- ( fun () -> false )

        //select_ModeParameter.BlockLength <- 0x00AABBCCUL
        sense_task.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ModeSense_002() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let sense_cdb = {
            OperationCode = 0x5Auy;
            LLBAA = true;
            DBD = false;
            PC = 0uy;
            PageCode = 0x0Auy;
            SubPageCode = 0uy;
            AllocationLength = 0xFFus;
            Control = 0uy;
        }
        let sense_task, ilu = createDefScsiTask defaultSCSICommandPDU sense_cdb [] false

        let sense_psStub = sense_task.Source.ProtocolService :?> CProtocolService_Stub
        sense_psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; 0x22uy; 0x00uy; 0x00uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
                0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
                0x88uy; 0x99uy; 0xAAuy; 0xBBuy; // BLOCK COUNT
                0xCCuy; 0xDDuy; 0xEEuy; 0xFFuy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
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
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        let media_stub = ( sense_task :?> ScsiTask ).Media :?> CMedia_Stub
        media_stub.p_GetBlockCount <- ( fun () -> 0x8899AABBCCDDEEFFUL )
        media_stub.p_GetWriteProtect <- ( fun () -> false )

        //select_ModeParameter.BlockLength <- 0x00AABBCCUL
        sense_task.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ModeSense_003() =
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
        let sense_task, ilu = createDefScsiTask defaultSCSICommandPDU sense_cdb [] false
        let sense_psStub = sense_task.Source.ProtocolService :?> CProtocolService_Stub
        sense_psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )
        let media_stub = ( sense_task :?> ScsiTask ).Media :?> CMedia_Stub
        media_stub.p_GetBlockCount <- ( fun () -> 0xAABBUL )
        media_stub.p_GetWriteProtect <- ( fun () -> false )

        sense_task.NotifyTerminate false
        sense_task.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt2 = 0 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.ModeSense_004() =
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
        let sense_task, ilu = createDefScsiTask defaultSCSICommandPDU sense_cdb [] false
        let sense_psStub = sense_task.Source.ProtocolService :?> CProtocolService_Stub
        sense_psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
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

        let media_stub = ( sense_task :?> ScsiTask ).Media :?> CMedia_Stub
        media_stub.p_GetBlockCount <- ( fun () -> 0xAABBUL )
        media_stub.p_GetWriteProtect <- ( fun () -> false )

        sense_task.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.ReportLUNs_001() =
        let mutable cnt1 = 0
        let cdb = {
            OperationCode = 0x0Auy;
            SelectReport = 0x00uy;
            AllocationLength = 15u;
            Control = 0uy;
        }
        let sense_task, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
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

    [<Fact>]
    member _.ReportLUNs_002() =
        let mutable cnt1 = 0
        let cdb = {
            OperationCode = 0x0Auy;
            SelectReport = 0x03uy;
            AllocationLength = 16u;
            Control = 0uy;
        }
        let dbtask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = dbtask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_GetLUNs <- ( fun () ->
            [|
                lun_me.zero;
                lun_me.fromPrim 1UL;
            |]
        )
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

        dbtask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.ReportLUNs_003() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0x0Auy;
            SelectReport = 0x00uy;
            AllocationLength = 16u;
            Control = 0uy;
        }
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let dbtask, ilu = createDefScsiTask scsiCommand cdb [ scsiDataOut ] false
        let psStub = dbtask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_GetLUNs <- ( fun () ->
            [|
                lun_me.fromPrim 0x1122334455667788UL;
                lun_me.fromPrim 0x123456789ABCDEF0UL;
            |]
        )
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvdl = 30u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // LUN LIST LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x77uy; 0x88uy; 0x55uy; 0x66uy;
                0x33uy; 0x44uy; 0x11uy; 0x22uy;
                0xDEuy; 0xF0uy; 0x9Auy; 0xBCuy;
                0x56uy; 0x78uy; 0x12uy; 0x34uy;
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        dbtask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportLUNs_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0x0Auy;
            SelectReport = 0x00uy;
            AllocationLength = 16u;
            Control = 0uy;
        }
        let dbtask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = dbtask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_GetLUNs <- ( fun () ->
            [|
                lun_me.fromPrim 0x1122334455667788UL;
            |]
        )
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        dbtask.NotifyTerminate false
        dbtask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 0 ))

    [<Fact>]
    member _.ReportLUNs_005() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0x0Auy;
            SelectReport = 0x01uy;
            AllocationLength = 16u;
            Control = 0uy;
        }
        let dbtask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = dbtask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_GetLUNs <- ( fun () ->
            [|
                lun_me.fromPrim 0x1122334455667788UL;
                lun_me.fromPrim 0x123456789ABCDEF0UL;
            |]
        )
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN LIST LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        dbtask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportLUNs_006() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0x0Auy;
            SelectReport = 0x02uy;
            AllocationLength = 16u;
            Control = 0uy;
        }
        let dbtask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = dbtask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_GetLUNs <- ( fun () ->
            [|
                lun_me.fromPrim 0x1122334455667788UL;
                lun_me.fromPrim 0x123456789ABCDEF0UL;
            |]
        )
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // LUN LIST LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x77uy; 0x88uy; 0x55uy; 0x66uy;
                0x33uy; 0x44uy; 0x11uy; 0x22uy;
                0xDEuy; 0xF0uy; 0x9Auy; 0xBCuy;
                0x56uy; 0x78uy; 0x12uy; 0x34uy;
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        dbtask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_001() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x00uy;
            RequestedOperationCode = 0x00uy;
            RequestedServiceAction = 0x00us;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let stask, ilu = createDefScsiTask scsiCommand cdb [scsiDataOut] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvdl = 30u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata SupportedOperationCodeConst.SupportedAllOperationCommands ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    static member m_ReportSupportedOperationCodes_002_data = [|
        [| 0x12uy :> obj; SupportedOperationCodeConst.CdbUsageData_INQUIRY :> obj; |];
        [| 0x15uy :> obj; SupportedOperationCodeConst.CdbUsageData_MODE_SELECT_6 :> obj; |];
        [| 0x55uy :> obj; SupportedOperationCodeConst.CdbUsageData_MODE_SELECT_10 :> obj; |];
        [| 0x1Auy :> obj; SupportedOperationCodeConst.CdbUsageData_MODE_SENSE_6 :> obj; |];
        [| 0x5Auy :> obj; SupportedOperationCodeConst.CdbUsageData_MODE_SENSE_10 :> obj; |];
        [| 0xA0uy :> obj; SupportedOperationCodeConst.CdbUsageData_REPORT_LUNS :> obj; |];
        [| 0x03uy :> obj; SupportedOperationCodeConst.CdbUsageData_REQUEST_SENSE :> obj; |];
        [| 0x00uy :> obj; SupportedOperationCodeConst.CdbUsageData_TEST_UNIT_READY :> obj; |];
        [| 0x04uy :> obj; SupportedOperationCodeConst.CdbUsageData_FORMAT_UNIT :> obj; |];
        [| 0x34uy :> obj; SupportedOperationCodeConst.CdbUsageData_PRE_FETCH_10 :> obj; |];
        [| 0x90uy :> obj; SupportedOperationCodeConst.CdbUsageData_PRE_FETCH_16 :> obj; |];
        [| 0x08uy :> obj; SupportedOperationCodeConst.CdbUsageData_READ_6 :> obj; |];
        [| 0x28uy :> obj; SupportedOperationCodeConst.CdbUsageData_READ_10 :> obj; |];
        [| 0xA8uy :> obj; SupportedOperationCodeConst.CdbUsageData_READ_12 :> obj; |];
        [| 0x88uy :> obj; SupportedOperationCodeConst.CdbUsageData_READ_16  :> obj; |];
        [| 0x25uy :> obj; SupportedOperationCodeConst.CdbUsageData_READ_CAPACITY_10 :> obj; |];
        [| 0x35uy :> obj; SupportedOperationCodeConst.CdbUsageData_SYNCHRONIZE_CACHE_10 :> obj; |];
        [| 0x91uy :> obj; SupportedOperationCodeConst.CdbUsageData_SYNCHRONIZE_CACHE_16 :> obj; |];
        [| 0x0Auy :> obj; SupportedOperationCodeConst.CdbUsageData_WRITE_6 :> obj; |];
        [| 0x2Auy :> obj; SupportedOperationCodeConst.CdbUsageData_WRITE_10 :> obj; |];
        [| 0xAAuy :> obj; SupportedOperationCodeConst.CdbUsageData_WRITE_12 :> obj; |];
        [| 0x8Auy :> obj; SupportedOperationCodeConst.CdbUsageData_WRITE_16 :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_ReportSupportedOperationCodes_002_data" )>]
    member _.ReportSupportedOperationCodes_002 ( opCode : byte ) ( result : byte[] ) =
        let mutable cnt1 = 0
        let mutable cnt2 = 0

        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x01uy;
            RequestedOperationCode = opCode;
            RequestedServiceAction = 0x00us;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata result ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun _ -> cnt1 <- cnt1 + 1 )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_003() =
        let mutable cnt1 = 0
        let vOPCode = [|
            0x5Euy; // PERSISTENT RESERVE IN
            0x5Fuy; // PERSISTENT RESERVE OUT
            0xA3uy; // REPORT SUPPORTED OPERATION CODES / REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS
            0x7Fuy; // READ(32) / WRITE(32)
        |]
        for i = 0 to vOPCode.Length - 1 do
            let cdb = {
                OperationCode = 0xA3uy;
                ServiceAction = 0x00uy;
                ReportingOptions = 0x01uy;
                RequestedOperationCode = vOPCode.[i];
                RequestedServiceAction = 0x00us;
                AllocationLength = 16u;
                Control = 0x00uy;
            }
            let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
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
            Assert.True(( cnt1 = i + 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x01uy;
            RequestedOperationCode = 0x01uy;
            RequestedServiceAction = 0x00us;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |] ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_005() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let vSA = [|
            0x00us;
            0x01us;
            0x02us;
            0x03us;
        |]
        for i = 0 to vSA.Length - 1 do
            let cdb = {
                OperationCode = 0xA3uy;
                ServiceAction = 0x00uy;
                ReportingOptions = 0x02uy;
                RequestedOperationCode = 0x5Euy;    // PERSISTENT RESERVE IN
                RequestedServiceAction = vSA.[i];
                AllocationLength = 16u;
                Control = 0x00uy;
            }
            let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
            let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
            psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
                cnt2 <- cnt2 + 1
                Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
                Assert.True(( stat = ScsiCmdStatCd.GOOD ))
                let v = SupportedOperationCodeConst.CdbUsageData_PERSISTENT_RESERVE_IN( byte vSA.[i] )
                Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
                Assert.True(( alloclen = 0x10u ))
            )
            ilu.p_NotifyTerminateTask <- ( fun argTask ->
                cnt1 <- cnt1 + 1
            )

            stask.Execute()()
            |> Functions.RunTaskSynchronously
            Assert.True(( cnt1 = i + 1 ))
            Assert.True(( cnt2 = i + 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_006() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x02uy;
            RequestedOperationCode = 0x5Euy;    // PERSISTENT RESERVE IN
            RequestedServiceAction = 0x04us;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |] ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_007() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let vSA = [|
            0x00us; 0x01us;
            0x02us; 0x03us;
            0x04us; 0x05us;
            0x06us; 0x07us;
        |]
        for i = 0 to vSA.Length - 1 do
            let cdb = {
                OperationCode = 0xA3uy;
                ServiceAction = 0x00uy;
                ReportingOptions = 0x02uy;
                RequestedOperationCode = 0x5Fuy;    // PERSISTENT RESERVE OUT
                RequestedServiceAction = vSA.[i];
                AllocationLength = 16u;
                Control = 0x00uy;
            }
            let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
            let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
            psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
                cnt2 <- cnt2 + 1
                Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
                Assert.True(( stat = ScsiCmdStatCd.GOOD ))
                let v = SupportedOperationCodeConst.CdbUsageData_PERSISTENT_RESERVE_OUT ( byte vSA.[i] )
                Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
                Assert.True(( alloclen = 0x10u ))
            )
            ilu.p_NotifyTerminateTask <- ( fun argTask ->
                cnt1 <- cnt1 + 1
            )

            stask.Execute()()
            |> Functions.RunTaskSynchronously
            Assert.True(( cnt1 = i + 1 ))
            Assert.True(( cnt2 = i + 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_008() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x02uy;
            RequestedOperationCode = 0x5Fuy;    // PERSISTENT RESERVE OUT
            RequestedServiceAction = 0x08us;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |] ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_009() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x02uy;
            RequestedOperationCode = 0xA3uy;    // REPORT SUPPORTED OPERATION CODES / REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS
            RequestedServiceAction = 0x0Cus;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata SupportedOperationCodeConst.CdbUsageData_REPORT_SUPPORTED_OPERATION_CODES ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_010() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x02uy;
            RequestedOperationCode = 0xA3uy;    // REPORT SUPPORTED OPERATION CODES / REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS
            RequestedServiceAction = 0x0Dus;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata SupportedOperationCodeConst.CdbUsageData_REPORT_SUPPORTED_TASK_MANAGEMENT_FUNCTIONS ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_011() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x02uy;
            RequestedOperationCode = 0xA3uy;    // REPORT SUPPORTED OPERATION CODES / REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS
            RequestedServiceAction = 0x01us;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |] ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_012() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x02uy;
            RequestedOperationCode = 0x7Fuy;    // READ(32) / WRITE(32)
            RequestedServiceAction = 0x0009us;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata SupportedOperationCodeConst.CdbUsageData_READ_32 ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_013() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x02uy;
            RequestedOperationCode = 0x7Fuy;    // READ(32) / WRITE(32)
            RequestedServiceAction = 0x000Bus;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata SupportedOperationCodeConst.CdbUsageData_WRITE_32 ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_014() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x02uy;
            RequestedOperationCode = 0x7Fuy;    // READ(32) / WRITE(32)
            RequestedServiceAction = 0x0000us;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |] ))
            Assert.True(( alloclen = 0x10u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Theory>]
    [<InlineData( 0x8Auy )>]
    [<InlineData( 0xAAuy )>]
    [<InlineData( 0x2Auy )>]
    [<InlineData( 0x0Auy )>]
    [<InlineData( 0x91uy )>]
    [<InlineData( 0x35uy )>]
    [<InlineData( 0x25uy )>]
    [<InlineData( 0x88uy )>]
    [<InlineData( 0xA8uy )>]
    [<InlineData( 0x28uy )>]
    [<InlineData( 0x08uy )>]
    [<InlineData( 0x90uy )>]
    [<InlineData( 0x34uy )>]
    [<InlineData( 0x04uy )>]
    [<InlineData( 0x00uy )>]
    [<InlineData( 0x03uy )>]
    [<InlineData( 0xA0uy )>]
    [<InlineData( 0x5Auy )>]
    [<InlineData( 0x1Auy )>]
    [<InlineData( 0x55uy )>]
    [<InlineData( 0x15uy )>]
    [<InlineData( 0x12uy )>]
    member _.ReportSupportedOperationCodes_015( i : byte ) =
        let mutable cnt1 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x02uy;
            RequestedOperationCode = i;
            RequestedServiceAction = 0x0000us;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
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
    member _.ReportSupportedOperationCodes_016() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x00uy;
            RequestedOperationCode = 0x00uy;
            RequestedServiceAction = 0x00us;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.NotifyTerminate false
        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt2 = 0 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.ReportSupportedOperationCodes_017() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x00uy;
            ReportingOptions = 0x00uy;
            RequestedOperationCode = 0x00uy;
            RequestedServiceAction = 0x00us;
            AllocationLength = 16u;
            Control = 0x00uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
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

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt2 = 1 ))
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
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
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
    member _.ReportSupportedTaskManagementFunctions_002() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : ReportSupportedTaskManagementFunctionsCDB = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x0Duy;
            AllocationLength = 4u;
            Control = 0uy;
        }
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let stask, ilu = createDefScsiTask scsiCommand cdb [scsiDataOut] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvdl = 30u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata [| 0xF8uy; 0x00uy; 0x00uy; 0x00uy; |] ))
            Assert.True(( alloclen = 4u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReportSupportedTaskManagementFunctions_003() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : ReportSupportedTaskManagementFunctionsCDB = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x0Duy;
            AllocationLength = 4u;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.NotifyTerminate false
        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 0 ))

    [<Fact>]
    member _.ReportSupportedTaskManagementFunctions_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : ReportSupportedTaskManagementFunctionsCDB = {
            OperationCode = 0xA3uy;
            ServiceAction = 0x0Duy;
            AllocationLength = 4u;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
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

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

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
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let stask, ilu = createDefScsiTask scsiCommand cdb [scsiDataOut] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let luStub = ( stask :?> ScsiTask ).LU :?> CInternalLU_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvdl = 30u ))
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
        luStub.p_GetUnitAttention <- ( fun itn ->
            Assert.True(( itn = stask.Source.I_TNexus ))
            ValueNone
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.RequestSense_002() =
        let mutable cnt2 = 0
        let mutable cnt1 = 0
        let cdb : RequestSenseCDB = {
            OperationCode = 0xA3uy;
            DESC = true;
            AllocationLength = 4uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let luStub = ( stask :?> ScsiTask ).LU :?> CInternalLU_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
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
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 4u ))

        )
        luStub.p_GetUnitAttention <- ( fun itn ->
            Assert.True(( itn = stask.Source.I_TNexus ))
            ValueSome <|
                SCSIACAException(
                    {
                        I_TNexus = new ITNexus( "INIT", isid_me.zero, "TARG", tpgt_me.zero );
                        CID = cid_me.fromPrim 0us;
                        ConCounter = concnt_me.fromPrim 0;
                        TSIH = tsih_me.fromPrim 0us;
                        ProtocolService = new CProtocolService_Stub();
                        SessionKiller = new HKiller()
                    },
                    ScsiCmdStatCd.CHECK_CONDITION,
                    SenseData(
                        true,
                        SenseKeyCd.ILLEGAL_REQUEST,
                        ASCCd.INVALID_FIELD_IN_CDB,
                        ValueNone,
                        ValueNone,
                        ValueNone,
                        ValueNone,
                        ValueNone,
                        ValueNone
                    ),
                    ""
                )
        )
        luStub.p_ClearUnitAttention <- ( fun itn ->
            Assert.True(( itn = stask.Source.I_TNexus ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.RequestSense_003() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : RequestSenseCDB = {
            OperationCode = 0xA3uy;
            DESC = true;
            AllocationLength = 4uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let luStub = ( stask :?> ScsiTask ).LU :?> CInternalLU_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
        )
        luStub.p_GetUnitAttention <- ( fun itn ->
            Assert.True(( itn = stask.Source.I_TNexus ))
            ValueNone
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.NotifyTerminate false
        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 0 ))

    [<Fact>]
    member _.RequestSense_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : RequestSenseCDB = {
            OperationCode = 0xA3uy;
            DESC = true;
            AllocationLength = 4uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let luStub = ( stask :?> ScsiTask ).LU :?> CInternalLU_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
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
        luStub.p_GetUnitAttention <- ( fun itn ->
            Assert.True(( itn = stask.Source.I_TNexus ))
            ValueNone
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

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.TestUnitReady_001() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : TestUnitReadyCDB = {
            OperationCode = 0x00uy;
            Control = 0uy;
        }
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let stask, ilu = createDefScsiTask scsiCommand cdb [scsiDataOut] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ indata _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvdl = 30u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata Array.empty ))
        )
        mediaStub.p_TestUnitReady <- ( fun itt source ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            ValueNone
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.TestUnitReady_002() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : TestUnitReadyCDB = {
            OperationCode = 0x00uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat sensedata indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.CHECK_CONDITION ))
            let v =
                [|
                    yield 0x72uy;   // RESPONSE CODE
                    yield ( byte SenseKeyCd.NOT_READY >>> 0 ) &&& 0x0Fuy;  //  SENSE KEY
                    yield ( uint16 ASCCd.LOGICAL_UNIT_DOES_NOT_RESPOND_TO_SELECTION ) >>> 8 |> byte; // ADDITIONAL SENSE CODE
                    yield ( uint16 ASCCd.LOGICAL_UNIT_DOES_NOT_RESPOND_TO_SELECTION ) &&& 0x00FFus |> byte;
                    yield 0uy;  //  Reserved
                    yield 0uy;
                    yield 0uy;
                    yield 0uy;  // ADDITIONAL SENSE LENGTH
                |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray sensedata v ))
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata Array.empty ))
            Assert.True(( alloclen = 0u ))
        )
        mediaStub.p_TestUnitReady <- ( fun itt source ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            ValueSome( ASCCd.LOGICAL_UNIT_DOES_NOT_RESPOND_TO_SELECTION )
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.TestUnitReady_003() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : TestUnitReadyCDB = {
            OperationCode = 0x00uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
        )
        mediaStub.p_TestUnitReady <- ( fun itt source ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            ValueNone
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.NotifyTerminate false
        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt2 = 0 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.TestUnitReady_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : TestUnitReadyCDB = {
            OperationCode = 0x00uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
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
        mediaStub.p_TestUnitReady <- ( fun itt source ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            ValueNone
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

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.FormatUnit_001() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : FormatUnitCDB = {
            OperationCode = 0x04uy;
            FMTPINFO = false;
            RTO_REQ = false;
            LONGLIST = false;
            FMTDATA = false;
            CMPLIST = false;
            DefectListFormat = 0uy;
            Control = 0uy;
        }
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let stask, ilu = createDefScsiTask scsiCommand cdb [scsiDataOut] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvdl = 30u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )
        mediaStub.p_Format <- ( fun itt source ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            Task.FromResult()
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.FormatUnit_002() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : FormatUnitCDB = {
            OperationCode = 0x04uy;
            FMTPINFO = false;
            RTO_REQ = false;
            LONGLIST = false;
            FMTDATA = false;
            CMPLIST = false;
            DefectListFormat = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
        )
        mediaStub.p_Format <- ( fun itt source ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            Task.FromResult()
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.NotifyTerminate false
        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt2 = 0 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.FormatUnit_003() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : FormatUnitCDB = {
            OperationCode = 0x04uy;
            FMTPINFO = false;
            RTO_REQ = false;
            LONGLIST = false;
            FMTDATA = false;
            CMPLIST = false;
            DefectListFormat = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
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
        mediaStub.p_Format <- ( fun itt source ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            Task.FromResult()
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

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.PreFetch_001() =
        let mutable cnt1 = 0
        let cdb : PreFetchCDB = {
            OperationCode = 0x34uy;
            IMMED = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 5UL;
            PrefetchLength = blkcnt_me.ofUInt32 3u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            Assert.Fail __LINE__
        )
        mediaStub.p_GetBlockCount <- ( fun () ->
            0x0000000000000005UL
        )
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt1 <- cnt1 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            | _ ->
                Assert.Fail __LINE__
        )
        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.PreFetch_002() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : PreFetchCDB = {
            OperationCode = 0x34uy;
            IMMED = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 5UL;
            PrefetchLength = blkcnt_me.ofUInt32 3u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( stat = ScsiCmdStatCd.CONDITION_MET ))
        )
        mediaStub.p_GetBlockCount <- ( fun () ->
            0x0000000000000008UL
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.PreFetch_003() =
        let mutable cnt1 = 0
        let cdb : PreFetchCDB = {
            OperationCode = 0x34uy;
            IMMED = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 5UL;
            PrefetchLength = blkcnt_me.ofUInt32 4u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let dbtask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let mediaStub = ( dbtask :?> ScsiTask ).Media :?> CMedia_Stub
        mediaStub.p_GetBlockCount <- ( fun () -> 8UL )
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt1 <- cnt1 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            | _ as x -> raise x
        )

        dbtask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.PreFetch_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : PreFetchCDB = {
            OperationCode = 0x34uy;
            IMMED = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 5UL;
            PrefetchLength = blkcnt_me.ofUInt32 0u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( stat = ScsiCmdStatCd.CONDITION_MET ))
        )
        mediaStub.p_GetBlockCount <- ( fun () -> 5UL )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.PreFetch_005() =
        let mutable cnt1 = 0
        let cdb : PreFetchCDB = {
            OperationCode = 0x34uy;
            IMMED = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 6UL;
            PrefetchLength = blkcnt_me.ofUInt32 1u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        mediaStub.p_GetBlockCount <- ( fun () -> 5UL )
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt1 <- cnt1 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            | _ as x ->
                raise x
        )
        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.PreFetch_006() =
        let mutable cnt1 = 0
        let cdb : PreFetchCDB = {
            OperationCode = 0x34uy;
            IMMED = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0xFFFFFFFFFFFFFFFFUL;
            PrefetchLength = blkcnt_me.ofUInt32 1u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        mediaStub.p_GetBlockCount <- ( fun () -> 5UL )
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt1 <- cnt1 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            | _ ->
                Assert.Fail __LINE__
        )
        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.Read_001() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : ReadCDB = {
            OperationCode = 0x08uy;
            RdProtect = 0uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBUL;
            TransferLength = blkcnt_me.ofUInt32 3u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let stask, ilu = createDefScsiTask scsiCommand cdb [scsiDataOut] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        mediaStub.p_GetBlockCount <- ( fun () -> 0xFFFFUL )
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvdl = 30u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( indata.Length = ( int Constants.MEDIA_BLOCK_SIZE ) * 3 ))
            Assert.True(( alloclen = ( uint Constants.MEDIA_BLOCK_SIZE ) * 3u ))
        )
        mediaStub.p_Read <- ( fun itt source lba buf ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            Assert.True(( lba = blkcnt_me.ofUInt64 0xAABBUL ))
            Assert.True(( buf.Count = ( int Constants.MEDIA_BLOCK_SIZE ) * 3 ))
            Task.FromResult( buf.Count )
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )
        ilu.p_NotifyReadBytesCount <- ( fun _ s ->
            Assert.True(( s = ( int64 Constants.MEDIA_BLOCK_SIZE ) * 3L ))
        )
        ilu.p_NotifyReadTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.Read_002() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : ReadCDB = {
            OperationCode = 0x08uy;
            RdProtect = 0uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBUL;
            TransferLength = blkcnt_me.ofUInt32 3u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let wDataLen = Constants.MEDIA_BLOCK_SIZE * 3UL
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        mediaStub.p_GetBlockCount <- ( fun () -> 0xFFFFUL )
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
        )
        mediaStub.p_Read <- ( fun itt source lba buf ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            Assert.True(( lba = blkcnt_me.ofUInt64 0xAABBUL ))
            Assert.True(( buf.Count = int wDataLen ))
            Task.FromResult( buf.Count )
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )
        ilu.p_NotifyReadBytesCount <- ( fun _ s ->
            Assert.True(( s = int wDataLen ))
        )

        stask.NotifyTerminate false
        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 0 ))

    [<Fact>]
    member _.Read_003() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : ReadCDB = {
            OperationCode = 0x08uy;
            RdProtect = 0uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0xAABBUL;
            TransferLength = blkcnt_me.ofUInt32 3u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let wDataLen = Constants.MEDIA_BLOCK_SIZE * 3UL
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        mediaStub.p_GetBlockCount <- ( fun () -> 0xFFFFUL )
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
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
        mediaStub.p_Read <- ( fun itt source lba buf ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            Assert.True(( lba = blkcnt_me.ofUInt64 0xAABBUL ))
            Assert.True(( buf.Count = int wDataLen ))
            Task.FromResult( buf.Count )
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
        ilu.p_NotifyReadBytesCount <- ( fun _ s ->
            Assert.True(( s = int wDataLen ))
        )
        ilu.p_NotifyReadTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.Read_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : ReadCDB = {
            OperationCode = 0x08uy;
            RdProtect = 0uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 512UL;
            TransferLength = blkcnt_me.ofUInt32 0u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            Assert.True(( indata.Length = 0 ))
            Assert.True(( alloclen = 0u ))
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )
        ilu.p_NotifyReadBytesCount <- ( fun _ s ->
            Assert.True(( s = 0 ))
        )
        ilu.p_NotifyReadTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.Read_005() =
        let mutable cnt3 = 0
        let cdb : ReadCDB = {
            OperationCode = 0x08uy;
            RdProtect = 0uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 512UL;
            TransferLength = blkcnt_me.ofUInt32 1u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyReadBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyReadTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Read_006() =
        let mutable cnt3 = 0
        let cdb : ReadCDB = {
            OperationCode = 0x08uy;
            RdProtect = 0uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0xFFFFFFFFFFFFFFFFUL;
            TransferLength = blkcnt_me.ofUInt32 1u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyReadBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyReadTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Read_007() =
        let mutable cnt3 = 0
        let cdb : ReadCDB = {
            OperationCode = 0x08uy;
            RdProtect = 0uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 513UL;
            TransferLength = blkcnt_me.ofUInt32 0u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyReadBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyReadTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Read_008() =
        let mutable cnt3 = 0
        let cdb : ReadCDB = {
            OperationCode = 0x08uy;
            RdProtect = 0uy;
            DPO = true; // not supported
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0UL;
            TransferLength = blkcnt_me.ofUInt32 1u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyReadBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyReadTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Read_009() =
        let mutable cnt3 = 0
        let cdb : ReadCDB = {
            OperationCode = 0x08uy;
            RdProtect = 0uy;
            DPO = false;
            FUA = true; // not supported
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0UL;
            TransferLength = blkcnt_me.ofUInt32 1u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyReadBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyReadTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Read_010() =
        let mutable cnt3 = 0
        let cdb : ReadCDB = {
            OperationCode = 0x08uy;
            RdProtect = 0uy;
            DPO = false;
            FUA = false;
            FUA_NV = true; // not supported
            LogicalBlockAddress = blkcnt_me.ofUInt64 0UL;
            TransferLength = blkcnt_me.ofUInt32 1u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyReadBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyReadTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Read_011() =
        let mutable cnt3 = 0
        let cdb : ReadCDB = {
            OperationCode = 0x08uy;
            RdProtect = 1uy; // not supported
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0UL;
            TransferLength = blkcnt_me.ofUInt32 1u;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyReadBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyReadTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

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
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let stask, ilu = createDefScsiTask scsiCommand cdb [scsiDataOut] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvdl = 30u ))
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
    member _.ReadCapacity_002() =
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
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // RETURNED LOGICAL BLOCK ADDRESS
                yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK LENGTH IN BYTE
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0x10u ))
        )
        mediaStub.p_ReadCapacity <- ( fun itt source ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            0x100000000UL
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member _.ReadCapacity_003() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : ReadCapacityCDB = {
            OperationCode = 0x9Euy;
            ServiceAction = 0x10uy;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0UL;
            PMI = false;
            AllocationLength = 0x10u;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // RETURNED LOGICAL BLOCK ADDRESS
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // RETURNED LOGICAL BLOCK ADDRESS
                yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK LENGTH IN BYTE
                0x00uy; // RTO_EN, PROT_EN
                0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
            Assert.True(( alloclen = 0x10u ))
        )
        mediaStub.p_ReadCapacity <- ( fun itt source ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            0x100000000UL
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReadCapacity_004() =
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
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
        )
        mediaStub.p_ReadCapacity <- ( fun itt source ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            0x100000000UL
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.NotifyTerminate false
        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 0 ))

    [<Fact>]
    member _.ReadCapacity_005() =
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
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
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
        mediaStub.p_ReadCapacity <- ( fun itt source ->
            Assert.True(( itt = itt_me.fromPrim 0u ))
            0x100000000UL
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

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Theory>]
    [<InlineData( 5UL, 3UL, 3u )>]
    [<InlineData( 5UL, 6UL, 0u )>]
    [<InlineData( 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFEUL, 2u )>]
    member _.SynchronizeCache_001 ( mbc : uint64, lba : uint64, nob : uint32 ) =
        let mutable cnt1 = 0
        let cdb : SynchronizeCacheCDB = {
            OperationCode = 0x35uy;
            SyncNV = true;
            IMMED = true;
            LogicalBlockAddress = blkcnt_me.ofUInt64 lba;
            NumberOfBlocks = blkcnt_me.ofUInt32 nob;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            Assert.Fail __LINE__
        )
        mediaStub.p_GetBlockCount <- ( fun () -> mbc )
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt1 <- cnt1 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            | _ ->
                Assert.Fail __LINE__
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))

    [<Theory>]
    [<InlineData( 5UL, 3UL, 2u )>]
    [<InlineData( 5UL, 5UL, 0u )>]
    [<InlineData( 0UL, 0UL, 0u )>]
    member _.SynchronizeCache_002 ( mbc : uint64, lba : uint64, nob : uint32 ) =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : SynchronizeCacheCDB = {
            OperationCode = 0x35uy;
            SyncNV = true;
            IMMED = true;
            LogicalBlockAddress = blkcnt_me.ofUInt64 lba;
            NumberOfBlocks = blkcnt_me.ofUInt32 nob;
            GroupNumber = 0uy;
            Control = 0uy;
        }
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 10 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let stask, ilu = createDefScsiTask scsiCommand cdb [scsiDataOut] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ _ _ _ ->
            Assert.True(( recvdl = 30u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            cnt2 <- cnt2 + 1
        )
        mediaStub.p_GetBlockCount <- ( fun () -> mbc )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.Write_001() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let mutable cnt4 = 0
        let cdb : WriteCDB = {
            OperationCode = 0x0Auy;
            WRPROTECT = 0x00uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0x00UL;
            GroupNumber = 0x00uy;
            TransferLength = 40960u / ( uint32 Constants.MEDIA_BLOCK_SIZE ) |> blkcnt_me.ofUInt32;
            Control = 0uy;
        }
        let cmd = {
            defaultSCSICommandPDU with
                DataSegment = PooledBuffer.RentAndInit 16;
        }
        let data = [
            { 
                defaultSCSIDataOutPDU with
                    F = false;
                    BufferOffset = 2u;
                    DataSegment = PooledBuffer.RentAndInit 2560;
            };
            { 
                defaultSCSIDataOutPDU with
                    F = false;
                    BufferOffset = 20480u;
                    DataSegment = PooledBuffer.RentAndInit 10240;
            };
            { 
                defaultSCSIDataOutPDU with
                    F = false;
                    BufferOffset = 40960u;
                    DataSegment = PooledBuffer.RentAndInit 10;
            };
            { 
                defaultSCSIDataOutPDU with
                    F = false;
                    BufferOffset = 30976u;
                    DataSegment = PooledBuffer.RentAndInit 640;
            };
            { 
                defaultSCSIDataOutPDU with
                    F = true;
                    BufferOffset = 35968u;
                    DataSegment = PooledBuffer.RentAndInit 8192;
            };
        ]
        let stask, ilu = createDefScsiTask cmd cdb data false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvLen resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvLen = 21658u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )
        mediaStub.p_Write <- ( fun itt source lba offset buf ->
            cnt1 <- cnt1 + 1
            Assert.True(( itt = itt_me.fromPrim 0u ))
            match cnt1 with
            | 1 ->
                Assert.True(( lba = blkcnt_me.ofUInt64 0UL ))
                Assert.True(( offset = 0UL ))
                Assert.True(( buf.Count = 16 ))
            | 2 ->
                Assert.True(( lba = blkcnt_me.ofUInt64 ( 2UL / Constants.MEDIA_BLOCK_SIZE ) ))
                Assert.True(( offset = 2UL ))
                Assert.True(( buf.Count = 2560 ))
            | 3 ->
                Assert.True(( lba = blkcnt_me.ofUInt64( 20480UL / Constants.MEDIA_BLOCK_SIZE ) ))
                Assert.True(( offset = 0UL ))
                Assert.True(( buf.Count = 10240 ))
            | 4 ->
                Assert.True(( lba = blkcnt_me.ofUInt64( 30976UL / Constants.MEDIA_BLOCK_SIZE ) ))
                Assert.True(( offset = 30976UL % Constants.MEDIA_BLOCK_SIZE ))
                Assert.True(( buf.Count = 640 ))
            | 5 ->
                Assert.True(( lba = blkcnt_me.ofUInt64( 35968UL / Constants.MEDIA_BLOCK_SIZE ) ))
                Assert.True(( offset = 35968UL % Constants.MEDIA_BLOCK_SIZE ))
                Assert.True(( buf.Count = 4992 ))
            | _ ->
                Assert.Fail __LINE__
            Task.FromResult( buf.Count )
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt3 <- cnt3 + 1
        )
        ilu.p_NotifyWrittenBytesCount <- ( fun _ s ->
            cnt4 <- cnt4 + 1
            match cnt4 with
            | 1 ->
                Assert.True(( s = 16 ))
            | 2 ->
                Assert.True(( s = 2560 ))
            | 3 ->
                Assert.True(( s = 10240 ))
            | 4 ->
                Assert.True(( s = 640 ))
            | 5 ->
                Assert.True(( s = 4992 ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyWriteTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 5 ))
        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt3 = 1 ))
        Assert.True(( cnt4 = 5 ))

    [<Fact>]
    member _.Write_002() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let cdb : WriteCDB = {
            OperationCode = 0x0Auy;
            WRPROTECT = 0x00uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0x00UL;
            GroupNumber = 0x00uy;
            TransferLength = blkcnt_me.ofUInt32 1u;
            Control = 0uy;
        }
        let data = [
            { 
                defaultSCSIDataOutPDU with
                    F = false;
                    BufferOffset = 0u;
                    DataSegment = PooledBuffer.RentAndInit 512;
            };
        ]
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb data false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
        )
        mediaStub.p_Write <- ( fun itt source lba offset buf ->
            cnt1 <- cnt1 + 1
            Assert.True(( itt = itt_me.fromPrim 0u ))
            Assert.True(( lba = blkcnt_me.ofUInt64 0x00UL ))
            Assert.True(( offset = 0UL ))
            Assert.True(( buf.Count = 512 ))
            Task.FromResult( buf.Count )
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt3 <- cnt3 + 1
        )
        ilu.p_NotifyWrittenBytesCount <- ( fun _ s ->
            Assert.True(( s = 512 ))
        )

        stask.NotifyTerminate false
        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 0 ))
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Write_003() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let cdb : WriteCDB = {
            OperationCode = 0x0Auy;
            WRPROTECT = 0x00uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0x00UL;
            GroupNumber = 0x00uy;
            TransferLength = blkcnt_me.ofUInt32 1u;
            Control = 0uy;
        }
        let data = [
            { 
                defaultSCSIDataOutPDU with
                    F = false;
                    BufferOffset = 0u;
                    DataSegment = PooledBuffer.RentAndInit 512;
            };
        ]
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb data false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
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
        mediaStub.p_Write <- ( fun _ _ _ _ buf ->
            cnt1 <- cnt1 + 1
            Task.FromResult( buf.Count )
        )
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_MESSAGE_ERROR ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyWrittenBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyWriteTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Write_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let cdb : WriteCDB = {
            OperationCode = 0x0Auy;
            WRPROTECT = 0x00uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 512UL;
            GroupNumber = 0x00uy;
            TransferLength = blkcnt_me.ofUInt32 0u;
            Control = 0uy;
        }
        let data = []
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb data false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let mediaStub = ( stask :?> ScsiTask ).Media :?> CMedia_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvLen resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvLen = 0u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )
        mediaStub.p_Write <- ( fun _ _ _ _ buf ->
            cnt1 <- cnt1 + 1
            Task.FromResult( buf.Count )
        )
        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt3 <- cnt3 + 1
        )
        ilu.p_NotifyWrittenBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyWriteTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 0 ))
        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Write_005() =
        let mutable cnt3 = 0
        let cdb : WriteCDB = {
            OperationCode = 0x0Auy;
            WRPROTECT = 0x00uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 512UL;
            GroupNumber = 0x00uy;
            TransferLength = blkcnt_me.ofUInt32 1u;
            Control = 0uy;
        }
        let data = []
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb data false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyWrittenBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyWriteTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Write_006() =
        let mutable cnt3 = 0
        let cdb : WriteCDB = {
            OperationCode = 0x0Auy;
            WRPROTECT = 0x00uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0xFFFFFFFFFFFFFFFFUL;
            GroupNumber = 0x00uy;
            TransferLength = blkcnt_me.ofUInt32 1u;
            Control = 0uy;
        }
        let data = []
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb data false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyWrittenBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyWriteTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Write_007() =
        let mutable cnt3 = 0
        let cdb : WriteCDB = {
            OperationCode = 0x0Auy;
            WRPROTECT = 0x00uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 513UL;
            GroupNumber = 0x00uy;
            TransferLength = blkcnt_me.ofUInt32 0u;
            Control = 0uy;
        }
        let data = []
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb data false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyWrittenBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyWriteTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Write_008() =
        let mutable cnt3 = 0
        let cdb : WriteCDB = {
            OperationCode = 0x0Auy;
            WRPROTECT = 0x00uy;
            DPO = true; // not supported
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0UL;
            GroupNumber = 0x00uy;
            TransferLength = blkcnt_me.ofUInt32 1u;
            Control = 0uy;
        }
        let data = []
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb data false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyWrittenBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyWriteTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Write_009() =
        let mutable cnt3 = 0
        let cdb : WriteCDB = {
            OperationCode = 0x0Auy;
            WRPROTECT = 0x00uy;
            DPO = false;
            FUA = true; // not supported
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0UL;
            GroupNumber = 0x00uy;
            TransferLength = blkcnt_me.ofUInt32 1u;
            Control = 0uy;
        }
        let data = []
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb data false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyWrittenBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyWriteTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Write_010() =
        let mutable cnt3 = 0
        let cdb : WriteCDB = {
            OperationCode = 0x0Auy;
            WRPROTECT = 0x00uy;
            DPO = false;
            FUA = false;
            FUA_NV = true; // not supported
            LogicalBlockAddress = blkcnt_me.ofUInt64 0UL;
            GroupNumber = 0x00uy;
            TransferLength = blkcnt_me.ofUInt32 1u;
            Control = 0uy;
        }
        let data = []
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb data false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyWrittenBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyWriteTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Write_011() =
        let mutable cnt3 = 0
        let cdb : WriteCDB = {
            OperationCode = 0x0Auy;
            WRPROTECT = 0x01uy; // not supported
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0UL;
            GroupNumber = 0x00uy;
            TransferLength = blkcnt_me.ofUInt32 1u;
            Control = 0uy;
        }
        let data = []
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb data false
        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyWrittenBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyWriteTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member _.Write_012() =
        let mutable cnt3 = 0
        let cdb : WriteCDB = {
            OperationCode = 0x0Auy;
            WRPROTECT = 0x00uy;
            DPO = false;
            FUA = false;
            FUA_NV = false;
            LogicalBlockAddress = blkcnt_me.ofUInt64 0UL;
            GroupNumber = 0x00uy;
            TransferLength = blkcnt_me.ofUInt32 1u;
            Control = 0uy;
        }
        let data = []
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb data false

        let stask_pc = PrivateCaller( stask )
        let modeParam = stask_pc.GetField( "m_ModeParameter" ) :?> ModeParameter
        let modeParam_pc = PrivateCaller( modeParam )
        modeParam_pc.SetField( "m_SWP", true )   // Set SWP flag

        ilu.p_NotifyTerminateTaskWithException <- ( fun argTask argEx ->
            cnt3 <- cnt3 + 1
            match argEx with
            | :? SCSIACAException as x ->
                Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( x.SenseKey = SenseKeyCd.DATA_PROTECT ))
                Assert.True(( x.ASC = ASCCd.WRITE_PROTECTED ))
            | _ ->
                Assert.Fail __LINE__
        )
        ilu.p_NotifyWrittenBytesCount <- ( fun _ _ -> () )
        ilu.p_NotifyWriteTickCount <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member this.PersistentReserveIn_001() =
        let pDirName = this.CreateTestDir "PersistentReserveIn_001"
        let fname = Functions.AppendPathName pDirName "PersistentReserveIn_001.txt"

        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0x1111111111111111UL, false;
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0x2222222222222222UL, false;
                new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0x3333333333333333UL, true;
                new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : PersistentReserveInCDB = {
            OperationCode = 0x5Euy;
            ServiceAction = 0x00uy; // READ KEY
            AllocationLength = 128us;
            Control = 0uy;
        }
        let scsiCommand = { defaultSCSICommandPDU with DataSegment = PooledBuffer.Rent 30 }
        let scsiDataOut = { defaultSCSIDataOutPDU with DataSegment = PooledBuffer.Rent 20 }
        let stask, ilu = createDefScsiTaskWithPRManager scsiCommand cdb [ scsiDataOut ] false fname
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ recvdl resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( recvdl = 50u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // PRGENERATION
                0x00uy; 0x00uy; 0x00uy; 0x20uy; // ADDITIONAL LENGTH
                0x11uy; 0x11uy; 0x11uy; 0x11uy; // KEY 1
                0x11uy; 0x11uy; 0x11uy; 0x11uy;
                0x22uy; 0x22uy; 0x22uy; 0x22uy; // KEY 2
                0x22uy; 0x22uy; 0x22uy; 0x22uy;
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // KEY 3
                0x33uy; 0x33uy; 0x33uy; 0x33uy;
                0x44uy; 0x44uy; 0x44uy; 0x44uy; // KEY 4
                0x44uy; 0x44uy; 0x44uy; 0x44uy;
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
        )

        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PersistentReserveIn_002() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : PersistentReserveInCDB = {
            OperationCode = 0x5Euy;
            ServiceAction = 0x01uy; // READ RESERVATION
            AllocationLength = 128us;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
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

        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))
    
    [<Fact>]
    member this.PersistentReserveIn_003() =
        let pDirName = this.CreateTestDir "PersistentReserveIn_003"
        let fname = Functions.AppendPathName pDirName "PersistentReserveIn_003.txt"
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0x1111111111111111UL, false;
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0x2222222222222222UL, false;
                new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0x3333333333333333UL, true;
                new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : PersistentReserveInCDB = {
            OperationCode = 0x5Euy;
            ServiceAction = 0x01uy; // READ RESERVATION
            AllocationLength = 128us;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTaskWithPRManager defaultSCSICommandPDU cdb [] false fname
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // PRGENERATION
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // ADDITIONAL LENGTH
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // RESERVATION KEY
                0x33uy; 0x33uy; 0x33uy; 0x33uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
                0x00uy;                         // Reserved
                0x05uy;                         // SCOPE, TYPE
                0x00uy; 0x00uy;                 // Obsolute
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
        )

        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PersistentReserveIn_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cdb : PersistentReserveInCDB = {
            OperationCode = 0x5Euy;
            ServiceAction = 0x02uy; // REPORT CAPABILITIES
            AllocationLength = 128us;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ indata alloclen _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            let v = [|
                0x00uy; 0x08uy;                 // LENGTH
                0x0Duy;                         // CRH, SPI_C, ATP_C, PTPL_C
                0x80uy;                         // TMV, PTPL_A
                0xEAuy; 0x01uy;                 // PERSISTENT RESERVATION TYPE MASK
                0x00uy; 0x00uy;                 // Reserved
            |]
            Assert.True(( PooledBuffer.ValueEqualsWithArray indata v ))
        )

        ilu.p_NotifyTerminateTask <- ( fun argTask ->
            cnt1 <- cnt1 + 1
        )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member this.PersistentReserveIn_005() =
        let mutable cnt1 = 0
        let cdb : PersistentReserveInCDB = {
            OperationCode = 0x5Euy;
            ServiceAction = 0xFFuy; // unknown
            AllocationLength = 128us;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false

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
    member this.PersistentReserveOut_001() =
        let mutable cnt1 = 0
        let cdb : PersistentReserveOutCDB = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0xFFuy;
            Scope = 0uy;
            PRType = PR_TYPE.EXCLUSIVE_ACCESS;
            ParameterListLength = 0u;
            Control = 0uy;
        }
        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [] false

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
    member this.PersistentReserveOut_002() =
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

        let stask, ilu = createDefScsiTask defaultSCSICommandPDU cdb [ data ] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let pc = new PrivateCaller( stask )
        let prManager = pc.GetField( "m_PRManager" ) :?> PRManager

        psStub.p_SendSCSIResponse <- ( fun _ _ _ resvLen resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resvLen = 24u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )

        ilu.p_NotifyTerminateTask <- ( fun _ ->
            cnt1 <- cnt1 + 1
        )
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

    [<Fact>]
    member this.PersistentReserveOut_003() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let cmd = {
            defaultSCSICommandPDU with
                DataSegment = [|
                    0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // RESERVATION KEY 
                    0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
                    0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
                    0x00uy; 0x00uy; 0x00uy; 0x00uy;
                    0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
                    0x01uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
                    0x00uy;                         // Reserved
                    0x00uy; 0x00uy;                 // Obsolute
                |] |> PooledBuffer.Rent
        }
        let cdb : PersistentReserveOutCDB = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0x01uy; // RESERVE
            Scope = 0uy;
            PRType = PR_TYPE.EXCLUSIVE_ACCESS;
            ParameterListLength = PooledBuffer.ulength cmd.DataSegment;
            Control = 0uy;
        }

        let stask, ilu = createDefScsiTask cmd cdb [] false
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let pc = new PrivateCaller( stask )
        let prManager = pc.GetField( "m_PRManager" ) :?> PRManager

        // register initial value
        let param =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY  
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
                0x01uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
                0x00uy;                         // Reserved
                0x00uy; 0x00uy;                 // Obsolute
            |]
            |> PooledBuffer.Rent
        let v = prManager.Register stask.Source ( itt_me.fromPrim 0u ) NO_RESERVATION param.uCount param
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        psStub.p_SendSCSIResponse <- ( fun _ _ _ resvLen resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resvLen = 24u ))
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )

        ilu.p_NotifyTerminateTask <- ( fun _ ->
            cnt1 <- cnt1 + 1
        )
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
        Assert.True(( prinfo.m_Holder.Value = stask.Source.I_TNexus ))
        Assert.True(( prinfo.m_Type = EXCLUSIVE_ACCESS ))

    [<Fact>]
    member this.PersistentReserveOut_004() =
        let pDirName = this.CreateTestDir "PersistentReserveOut_004"
        let fname = Functions.AppendPathName pDirName "PersistentReserveOut_004.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL, true;
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let data = {
            defaultSCSIDataOutPDU with
                DataSegment = [|
                    0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // RESERVATION KEY 
                    0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
                    0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
                    0x00uy; 0x00uy; 0x00uy; 0x00uy;
                    0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
                    0x01uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
                    0x00uy;                         // Reserved
                    0x00uy; 0x00uy;                 // Obsolute
                |] |> PooledBuffer.Rent
        }
        let cdb : PersistentReserveOutCDB = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0x02uy; // RELEASE
            Scope = 0uy;
            PRType = PR_TYPE.EXCLUSIVE_ACCESS;
            ParameterListLength = PooledBuffer.ulength data.DataSegment;
            Control = 0uy;
        }

        let stask, ilu = createDefScsiTaskWithPRManager defaultSCSICommandPDU cdb [ data ] false fname
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let pc = new PrivateCaller( stask )
        let prManager = pc.GetField( "m_PRManager" ) :?> PRManager
        let pc2 = new PrivateCaller( prManager )
        let prinfo = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN1 ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))
        Assert.True(( prinfo.m_Type = EXCLUSIVE_ACCESS ))

        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )

        ilu.p_NotifyTerminateTask <- ( fun _ ->
            cnt1 <- cnt1 + 1
        )
        ilu.p_LUN <- ( fun () -> lun_me.zero )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

        let prinfo2 = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo2.m_PRGeneration = 0u ))
        Assert.True(( prinfo2.m_Registrations.Count = 1 ))
        Assert.True(( prinfo2.m_Registrations.ContainsKey stask.Source.I_TNexus ))
        Assert.True(( prinfo2.m_Holder.IsNone ))
        Assert.True(( prinfo2.m_Type = NO_RESERVATION ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PersistentReserveOut_005() =
        let pDirName = this.CreateTestDir "PersistentReserveOut_005"
        let fname = Functions.AppendPathName pDirName "PersistentReserveOut_005.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL, true;
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let data = {
            defaultSCSIDataOutPDU with
                DataSegment = [|
                    0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // RESERVATION KEY 
                    0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
                    0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
                    0x00uy; 0x00uy; 0x00uy; 0x00uy;
                    0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
                    0x01uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
                    0x00uy;                         // Reserved
                    0x00uy; 0x00uy;                 // Obsolute
                |] |> PooledBuffer.Rent
        }
        let cdb : PersistentReserveOutCDB = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0x03uy; // CLEAR
            Scope = 0uy;
            PRType = PR_TYPE.EXCLUSIVE_ACCESS;
            ParameterListLength = PooledBuffer.ulength data.DataSegment;
            Control = 0uy;
        }

        let stask, ilu = createDefScsiTaskWithPRManager defaultSCSICommandPDU cdb [ data ] false fname
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let pc = new PrivateCaller( stask )
        let prManager = pc.GetField( "m_PRManager" ) :?> PRManager
        let pc2 = new PrivateCaller( prManager )
        let prinfo = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN1 ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))
        Assert.True(( prinfo.m_Type = EXCLUSIVE_ACCESS ))

        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )

        ilu.p_NotifyTerminateTask <- ( fun _ ->
            cnt1 <- cnt1 + 1
        )
        ilu.p_LUN <- ( fun () -> lun_me.zero )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

        let prinfo2 = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo2.m_PRGeneration = 1u ))
        Assert.True(( prinfo2.m_Registrations.Count = 0 ))
        Assert.True(( prinfo2.m_Holder.IsNone ))
        Assert.True(( prinfo2.m_Type = NO_RESERVATION ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PersistentReserveOut_006() =
        let pDirName = this.CreateTestDir "PersistentReserveOut_006"
        let fname = Functions.AppendPathName pDirName "PersistentReserveOut_006.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
        let initITN2 = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL, false;
                initITN2, resvkey_me.fromPrim 0x1111111111111111UL, true;
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let data = {
            defaultSCSIDataOutPDU with
                DataSegment = [|
                    0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // RESERVATION KEY 
                    0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
                    0x11uy; 0x11uy; 0x11uy; 0x11uy; // SERVICE ACTION RESERVATION KEY(0)
                    0x11uy; 0x11uy; 0x11uy; 0x11uy;
                    0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
                    0x01uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
                    0x00uy;                         // Reserved
                    0x00uy; 0x00uy;                 // Obsolute
                |] |> PooledBuffer.Rent
        }
        let cdb : PersistentReserveOutCDB = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0x04uy; // PREEMPT
            Scope = 0uy;
            PRType = PR_TYPE.EXCLUSIVE_ACCESS;
            ParameterListLength = PooledBuffer.ulength data.DataSegment;
            Control = 0uy;
        }

        let stask, ilu = createDefScsiTaskWithPRManager defaultSCSICommandPDU cdb [ data ] false fname
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let pc = new PrivateCaller( stask )
        let prManager = pc.GetField( "m_PRManager" ) :?> PRManager
        let pc2 = new PrivateCaller( prManager )
        let prinfo = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 2 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN1 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN2 ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))
        Assert.True(( prinfo.m_Type = EXCLUSIVE_ACCESS ))

        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )

        ilu.p_NotifyTerminateTask <- ( fun _ ->
            cnt1 <- cnt1 + 1
        )
        ilu.p_LUN <- ( fun () -> lun_me.zero )
        ilu.p_EstablishUnitAttention <- ( fun _ _ -> () )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

        let prinfo2 = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo2.m_PRGeneration = 1u ))
        Assert.True(( prinfo2.m_Registrations.Count = 1 ))
        Assert.True(( prinfo2.m_Registrations.ContainsKey initITN1 ))
        Assert.True(( prinfo2.m_Holder.Value = initITN1 ))
        Assert.True(( prinfo2.m_Type = EXCLUSIVE_ACCESS ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PersistentReserveOut_007() =
        let pDirName = this.CreateTestDir "PersistentReserveOut_007"
        let fname = Functions.AppendPathName pDirName "PersistentReserveOut_007.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
            |]
            fname
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let data = {
            defaultSCSIDataOutPDU with
                DataSegment = [|
                    0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
                    0x00uy; 0x00uy; 0x00uy; 0x00uy;
                    0xFFuy; 0xEEuy; 0xDDuy; 0xCCuy; // SERVICE ACTION RESERVATION KEY  
                    0xBBuy; 0xAAuy; 0x99uy; 0x88uy;
                    0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
                    0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
                    0x00uy;                         // Reserved
                    0x00uy; 0x00uy;                 // Obsolute
                |] |> PooledBuffer.Rent
        }
        let cdb : PersistentReserveOutCDB = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0x06uy; // REGISTER AND IGNORE EXISTING KEY
            Scope = 0uy;
            PRType = PR_TYPE.EXCLUSIVE_ACCESS;
            ParameterListLength = PooledBuffer.ulength data.DataSegment;
            Control = 0uy;
        }

        let stask, ilu = createDefScsiTaskWithPRManager defaultSCSICommandPDU cdb [ data ] false fname
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let pc = new PrivateCaller( stask )
        let prManager = pc.GetField( "m_PRManager" ) :?> PRManager
        let pc2 = new PrivateCaller( prManager )
        let prinfo = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN1 ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))
        Assert.True(( prinfo.m_Type = EXCLUSIVE_ACCESS ))

        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )

        ilu.p_NotifyTerminateTask <- ( fun _ ->
            cnt1 <- cnt1 + 1
        )
        ilu.p_LUN <- ( fun () -> lun_me.zero )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

        let pc2 = new PrivateCaller( prManager )
        let prinfo = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN1 ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))
        Assert.True(( prinfo.m_Type = EXCLUSIVE_ACCESS ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PersistentReserveOut_008() =
        let pDirName = this.CreateTestDir "PersistentReserveOut_008"
        let fname = Functions.AppendPathName pDirName "PersistentReserveOut_008.txt"

        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        let ansITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 4uy 4us 4uy 4us, "target000", tpgt_me.fromPrim 0xFFFFus )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let data = {
            defaultSCSIDataOutPDU with
                DataSegment = [|
                    0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
                    0x11uy; 0x11uy; 0x11uy; 0x11uy;
                    0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
                    0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
                    0x00uy;                         // Reserved
                    0x00uy;                         // UNREG(0), APTPL(0)
                    0xFFuy; 0xFFuy;                 // RELATIVE TARGET PORT IDENTIFIER
                    0x00uy; 0x00uy; 0x00uy; 0x24uy; // TRANSPORTID PARAMETER DATA LENGTH

                    // TransportID
                    0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
                    0x00uy;                         // Reserved
                    0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
                    yield! Encoding.UTF8.GetBytes "initiator000"
                    yield! Encoding.UTF8.GetBytes ",i,0x"
                    yield! Encoding.UTF8.GetBytes "440004040004"
                    0x00uy; 0x00uy; 0x00uy;
                |] |> PooledBuffer.Rent
        }
        let cdb : PersistentReserveOutCDB = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0x07uy; // REGISTER AND MOVE
            Scope = 0uy;
            PRType = PR_TYPE.NO_RESERVATION;
            ParameterListLength = PooledBuffer.ulength data.DataSegment;
            Control = 0uy;
        }

        let stask, ilu = createDefScsiTaskWithPRManager defaultSCSICommandPDU cdb [ data ] false fname
        let psStub = stask.Source.ProtocolService :?> CProtocolService_Stub
        let pc = new PrivateCaller( stask )
        let prManager = pc.GetField( "m_PRManager" ) :?> PRManager
        let pc2 = new PrivateCaller( prManager )
        let prinfo = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN1 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN2 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN3 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN4 ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))
        Assert.True(( prinfo.m_Type = EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))

        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )

        ilu.p_NotifyTerminateTask <- ( fun _ ->
            cnt1 <- cnt1 + 1
        )
        ilu.p_LUN <- ( fun () -> lun_me.zero )

        stask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

        let prinfo2 = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo2.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo2.m_PRGeneration = 1u ))
        Assert.True(( prinfo2.m_Registrations.Count = 5 ))
        Assert.True(( prinfo2.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo2.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo2.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo2.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo2.m_Registrations.Item( ansITN1 ) =  resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo2.m_Holder.Value = ansITN1 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PersistentReserveOut_009() =
        let pDirName = this.CreateTestDir "PersistentReserveOut_009"
        let fname = Functions.AppendPathName pDirName "PersistentReserveOut_009.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
        let initITN2 = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL, false;
                initITN2, resvkey_me.fromPrim 0x1111111111111111UL, true;
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let data = {
            defaultSCSIDataOutPDU with
                DataSegment = [|
                    0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // RESERVATION KEY 
                    0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
                    0x11uy; 0x11uy; 0x11uy; 0x11uy; // SERVICE ACTION RESERVATION KEY(0)
                    0x11uy; 0x11uy; 0x11uy; 0x11uy;
                    0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
                    0x01uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
                    0x00uy;                         // Reserved
                    0x00uy; 0x00uy;                 // Obsolute
                |] |> PooledBuffer.Rent
        }
        let cdb : PersistentReserveOutCDB = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0x05uy; // PREEMPT AND ABORT
            Scope = 0uy;
            PRType = PR_TYPE.EXCLUSIVE_ACCESS;
            ParameterListLength = PooledBuffer.ulength data.DataSegment;
            Control = 0uy;
        }

        let scsiTask, ilu = createDefScsiTaskWithPRManager defaultSCSICommandPDU cdb [ data ] false fname
        let psStub = scsiTask.Source.ProtocolService :?> CProtocolService_Stub
        let pc = new PrivateCaller( scsiTask )
        let prManager = pc.GetField( "m_PRManager" ) :?> PRManager
        let pc2 = new PrivateCaller( prManager )
        let prinfo = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 2 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN1 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN2 ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))
        Assert.True(( prinfo.m_Type = EXCLUSIVE_ACCESS ))

        psStub.p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ _ _ _ ->
            cnt2 <- cnt2 + 1
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        )

        ilu.p_NotifyTerminateTask <- ( fun _ ->
            cnt1 <- cnt1 + 1
        )
        ilu.p_LUN <- ( fun () -> lun_me.zero )
        ilu.p_EstablishUnitAttention <- ( fun _ _ -> () )
        ilu.p_AbortTasksFromSpecifiedITNexus <- ( fun taskObj itn abortAllACATasks ->
            cnt3 <- cnt3 + 1
            Assert.True(( taskObj = scsiTask ))
            Assert.True(( itn.Length = 1 ))
            Assert.True(( itn.[0] = initITN2 ))
            Assert.True(( not abortAllACATasks ))
        )

        scsiTask.Execute()()
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt3 = 1 ))

        let prinfo2 = ( pc2.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj
        Assert.True(( prinfo2.m_PRGeneration = 1u ))
        Assert.True(( prinfo2.m_Registrations.Count = 1 ))
        Assert.True(( prinfo2.m_Registrations.ContainsKey initITN1 ))
        Assert.True(( prinfo2.m_Holder.Value = initITN1 ))
        Assert.True(( prinfo2.m_Type = EXCLUSIVE_ACCESS ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName
