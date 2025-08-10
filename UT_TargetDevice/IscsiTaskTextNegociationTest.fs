namespace Haruka.Test.UT.TargetDevice

open System
open System.Text

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes
open Haruka.TargetDevice
open Haruka.Test


type IscsiTaskTextNegociation_Test () =

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore


    static member defaultTextResponsePDU = {
        F = false;
        C = false;
        LUN = lun_me.fromPrim 0x0011223344556677UL;
        InitiatorTaskTag = itt_me.fromPrim 1u;
        TargetTransferTag = ttt_me.fromPrim 2u;
        StatSN = statsn_me.fromPrim 3u;
        ExpCmdSN = cmdsn_me.fromPrim 4u;
        MaxCmdSN = cmdsn_me.fromPrim 5u;
        TextResponse = Array.empty;
    }

    static member defaultRequestPDU = {
        I = false;
        F = false;
        C = false;
        LUN = lun_me.fromPrim 0x0011223344556677UL;
        InitiatorTaskTag = itt_me.fromPrim 1u;
        TargetTransferTag = ttt_me.fromPrim 2u;
        CmdSN = cmdsn_me.fromPrim 3u;
        ExpStatSN = statsn_me.fromPrim 4u;
        TextRequest = Array.empty;
        ByteCount = 0u;
    }

    static member defaultIscsiNegoParamCO = {
        AuthMethod = [| AuthMethodCandidateValue.AMC_None |];
        HeaderDigest = [| DigestType.DST_None |];
        DataDigest = [| DigestType.DST_None |];
        MaxRecvDataSegmentLength_I = 8192u;
        MaxRecvDataSegmentLength_T = 8192u;
    }

    static member defaultIscsiNegoParamSW = {
        MaxConnections = Constants.NEGOPARAM_MaxConnections;
        TargetGroupID = tgid_me.Zero;
        TargetConf = {
            IdentNumber = tnodeidx_me.fromPrim 0u;
            TargetName = "targeta0";
            TargetAlias = "";
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            LUN = [];
            Auth = TargetGroupConf.T_Auth.U_None();
        };
        InitiatorName = "initiatora0";
        InitiatorAlias = "Initiator_Alias";
        TargetPortalGroupTag = tpgt_me.fromPrim 2us;
        InitialR2T = true;
        ImmediateData = true;
        MaxBurstLength = 8192u;
        FirstBurstLength = 8192u;
        DefaultTime2Wait = 1us;
        DefaultTime2Retain = 2us;
        MaxOutstandingR2T = 3us;
        DataPDUInOrder = true;
        DataSequenceInOrder = true;
        ErrorRecoveryLevel = 0uy;
    }



    [<Fact>]
    member _.Constractor_001() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r = new IscsiTaskTextNegociation(
            objidx_me.NewID(),
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.I,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.InitiatorTaskTag,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.CmdSN,
            TextKeyValues.defaultTextKeyValues,
            TextKeyValuesStatus.defaultTextKeyValuesStatus,
            [],
            [
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 11u;
                };
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 12u;
                };
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 13u;
                };
            ],
            false,
            false,
            false
        )
        Assert.True(( iSCSITaskType.TextNegociation = ( r :> IIscsiTask ).TaskType ))
        Assert.True(( itt_me.fromPrim 1u = ValueOption.get ( r :> IIscsiTask ).InitiatorTaskTag ))
        Assert.True( ( r :> IIscsiTask ).IsExecutable )
        Assert.True(( struct( cid_me.fromPrim 0us, concnt_me.fromPrim 0 ) = ( r :> IIscsiTask ).AllegiantConnection ))
        Assert.False( ( r :> IIscsiTask ).IsRemovable )
        Assert.Same( sessStub :> ISession, r.Session )
        Assert.True(( TextKeyValues.defaultTextKeyValues = r.CurrentNegoParam ))
        Assert.True(( TextKeyValuesStatus.defaultTextKeyValuesStatus = r.CurrentNegoStatus ))
        Assert.True(( [] = r.ContPDUs ))
        Assert.True(( ttt_me.fromPrim 11u = ( ValueOption.get r.NextResponsePDU ).TargetTransferTag ))
        Assert.True(( 2 = r.ContResponsePDUs.Length ))
        Assert.True(( ttt_me.fromPrim 12u = r.ContResponsePDUs.Item( 0 ).TargetTransferTag ))
        Assert.True(( ttt_me.fromPrim 13u = r.ContResponsePDUs.Item( 1 ).TargetTransferTag ))

    [<Fact>]
    member _.Constractor_002() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r = new IscsiTaskTextNegociation(
            objidx_me.NewID(),
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.I,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.InitiatorTaskTag,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.CmdSN,
            TextKeyValues.defaultTextKeyValues,
            TextKeyValuesStatus.defaultTextKeyValuesStatus,
            [],
            [],
            false,
            false,
            false
        )
        Assert.False( ( r :> IIscsiTask ).IsExecutable )
        Assert.False( ( r :> IIscsiTask ).IsRemovable )
        Assert.True(( [] = r.ContPDUs ))
        Assert.True( ValueOption.isNone r.NextResponsePDU )
        Assert.True(( 0 = r.ContResponsePDUs.Length ))

    [<Fact>]
    member _.Constractor_003() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r = new IscsiTaskTextNegociation(
            objidx_me.NewID(),
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.I,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.InitiatorTaskTag,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.CmdSN,
            TextKeyValues.defaultTextKeyValues,
            TextKeyValuesStatus.defaultTextKeyValuesStatus,
            [],
            [
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        F = true;
                };
            ],
            false,
            true,
            false
        )
        Assert.True( ( r :> IIscsiTask ).IsExecutable )
        Assert.False( ( r :> IIscsiTask ).IsRemovable )
        Assert.True(( [] = r.ContPDUs ))
        Assert.True( ValueOption.isSome r.NextResponsePDU )
        Assert.True(( 0 = r.ContResponsePDUs.Length ))

    [<Fact>]
    member _.Constractor_004() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r = IscsiTaskTextNegociation.CreateWithInitParams(
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            IscsiTaskTextNegociation_Test.defaultRequestPDU,
            IscsiTaskTextNegociation_Test.defaultIscsiNegoParamSW,
            IscsiTaskTextNegociation_Test.defaultIscsiNegoParamCO
        )
        Assert.True(( TextValueType.Value( "Initiator_Alias" ) = r.CurrentNegoParam.InitiatorAlias ))
        Assert.True(( TextValueType.Value( 8192u ) = r.CurrentNegoParam.MaxRecvDataSegmentLength_I ))
        Assert.True(( NegoStatusValue.NSV_Negotiated = r.CurrentNegoStatus.NegoStat_InitiatorAlias ))
        Assert.True(( NegoStatusValue.NSV_Negotiated = r.CurrentNegoStatus.NegoStat_MaxRecvDataSegmentLength_I ))

    [<Fact>]
    member _.Constractor_005() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let task1 = IscsiTaskTextNegociation.CreateWithInitParams(
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            {
                IscsiTaskTextNegociation_Test.defaultRequestPDU with
                    InitiatorTaskTag = itt_me.fromPrim 1u;
            },
            IscsiTaskTextNegociation_Test.defaultIscsiNegoParamSW,
            IscsiTaskTextNegociation_Test.defaultIscsiNegoParamCO
        )

        try
            let _ = IscsiTaskTextNegociation.UpdateNegoStatByReqPDU(
                task1,
                {
                    IscsiTaskTextNegociation_Test.defaultRequestPDU with
                        InitiatorTaskTag = itt_me.fromPrim 2u;      // ITT is error
                }
            )
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException -> ()
        | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.Constractor_006() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let task1 = IscsiTaskTextNegociation.CreateWithInitParams(
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            {
                IscsiTaskTextNegociation_Test.defaultRequestPDU with
                    I = true;
            },
            IscsiTaskTextNegociation_Test.defaultIscsiNegoParamSW,
            IscsiTaskTextNegociation_Test.defaultIscsiNegoParamCO
        )

        try
            let _ = IscsiTaskTextNegociation.UpdateNegoStatByReqPDU(
                task1,
                {
                    IscsiTaskTextNegociation_Test.defaultRequestPDU with
                        I = false;      // I bit is error
                }
            )
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException -> ()
        | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.Constractor_007() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let task1 = new IscsiTaskTextNegociation(
            objidx_me.NewID(),
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.I,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.InitiatorTaskTag,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.CmdSN,
            TextKeyValues.defaultTextKeyValues,
            TextKeyValuesStatus.defaultTextKeyValuesStatus,
            [],
            [
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 11u;
                };
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 12u;
                };
            ],
            false,
            false,
            false
        )

        try
            let _ = IscsiTaskTextNegociation.UpdateNegoStatByReqPDU(
                task1,
                {
                    IscsiTaskTextNegociation_Test.defaultRequestPDU with
                        C = true;   // target is sending response PDU, but C bit in request PDU is 1.
                }
            )
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException -> ()
        | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.Constractor_008() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let task1 = IscsiTaskTextNegociation.CreateWithInitParams(
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            IscsiTaskTextNegociation_Test.defaultRequestPDU,
            IscsiTaskTextNegociation_Test.defaultIscsiNegoParamSW,
            IscsiTaskTextNegociation_Test.defaultIscsiNegoParamCO
        )

        let task2 = IscsiTaskTextNegociation.UpdateNegoStatByReqPDU(
            task1,
            {
                IscsiTaskTextNegociation_Test.defaultRequestPDU with
                    C = true;
                    TextRequest = [| 0uy .. 9uy |];
            }
        )

        // target send empty response PDU
        Assert.True( ValueOption.isSome task2.NextResponsePDU )
        Assert.False( ( ValueOption.get task2.NextResponsePDU ).F )
        Assert.False( ( ValueOption.get task2.NextResponsePDU ).C )
        Assert.True(( IscsiTaskTextNegociation_Test.defaultRequestPDU.LUN = ( ValueOption.get task2.NextResponsePDU ).LUN ))
        Assert.True(( IscsiTaskTextNegociation_Test.defaultRequestPDU.InitiatorTaskTag = ( ValueOption.get task2.NextResponsePDU ).InitiatorTaskTag ))
        Assert.True(( IscsiTaskTextNegociation_Test.defaultRequestPDU.TargetTransferTag = ( ValueOption.get task2.NextResponsePDU ).TargetTransferTag ))
        Assert.True(( 0 = ( ValueOption.get task2.NextResponsePDU ).TextResponse.Length ))
        Assert.True(( 0 = task2.ContResponsePDUs.Length ))
        Assert.True(( 1 = task2.ContPDUs.Length ))
        Assert.True( task2.ContPDUs.Item( 0 ).C )
        Assert.True(( [| 0uy .. 9uy |] = task2.ContPDUs.Item( 0 ).TextRequest ))

    [<Fact>]
    member _.Constractor_009() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let task1 = new IscsiTaskTextNegociation(
            objidx_me.NewID(),
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.I,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.InitiatorTaskTag,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.CmdSN,
            TextKeyValues.defaultTextKeyValues,
            TextKeyValuesStatus.defaultTextKeyValuesStatus,
            [],
            [
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 11u;
                };
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 12u;
                };
            ],
            false,
            false,
            false
        )

        try
            let _ = IscsiTaskTextNegociation.UpdateNegoStatByReqPDU(
                task1,
                {
                    IscsiTaskTextNegociation_Test.defaultRequestPDU with
                        C = false;
                        TextRequest = [| 0uy .. 9uy |]; // target is sending response PDU, but TextRequest data in request PDU is exist.
                }
            )
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException -> ()
        | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.Constractor_010() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let task1 = new IscsiTaskTextNegociation(
            objidx_me.NewID(),
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.I,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.InitiatorTaskTag,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.CmdSN,
            TextKeyValues.defaultTextKeyValues,
            TextKeyValuesStatus.defaultTextKeyValuesStatus,
            [],
            [   // list of response PDU
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 11u;
                };
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 12u;
                };
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 13u;
                };
            ],
            false,
            false,
            false
        )

        let task2 = IscsiTaskTextNegociation.UpdateNegoStatByReqPDU(
            task1,
            {
                IscsiTaskTextNegociation_Test.defaultRequestPDU with
                    C = false;
            }
        )

        // ignore empty request PDU
        Assert.True(( 0 = task2.ContPDUs.Length ))

        // next PDU to send
        Assert.True(( ttt_me.fromPrim 12u = ( ValueOption.get task2.NextResponsePDU ).TargetTransferTag ))

        // remain PDU to send
        Assert.True(( 1 = task2.ContResponsePDUs.Length ))
        Assert.True(( ttt_me.fromPrim 13u = task2.ContResponsePDUs.Item( 0 ).TargetTransferTag ))
       
    [<Fact>]
    member _.Constractor_011() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let task1 = IscsiTaskTextNegociation.CreateWithInitParams(
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            IscsiTaskTextNegociation_Test.defaultRequestPDU,
            IscsiTaskTextNegociation_Test.defaultIscsiNegoParamSW,
            IscsiTaskTextNegociation_Test.defaultIscsiNegoParamCO
        )

        try
            let _ = IscsiTaskTextNegociation.UpdateNegoStatByReqPDU(
                task1,
                {
                    IscsiTaskTextNegociation_Test.defaultRequestPDU with
                        C = false;
                        TextRequest = [| 0uy .. 9uy |]; // not recognized
                }
            )
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException -> ()
        | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.Constractor_012() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueNone )
        )
        let task1 = new IscsiTaskTextNegociation(
            objidx_me.NewID(),
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.I,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.InitiatorTaskTag,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.CmdSN,
            {
                TextKeyValues.defaultTextKeyValues with
                    TargetAlias = TextValueType.Value( Encoding.UTF8.GetString( [| for i in 0 .. 512 -> 0x61uy |] ) );
            },
            {
                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                    NegoStat_TargetAlias = NegoStatusValue.NSG_WaitSend;
            },
            [
                {
                    IscsiTaskTextNegociation_Test.defaultRequestPDU with
                        C = true;
                        TextRequest = Encoding.UTF8.GetBytes "MaxRecvDataSegment";
                }
            ],
            [],
            false,
            true,
            false
        )

        let task2 = IscsiTaskTextNegociation.UpdateNegoStatByReqPDU(
            task1,
            {
                IscsiTaskTextNegociation_Test.defaultRequestPDU with
                    C = false;
                    CmdSN = cmdsn_me.fromPrim 4u;
                    TextRequest = Encoding.UTF8.GetBytes "Length=512";
            }
        )

        Assert.True( ValueOption.isSome task2.NextResponsePDU )
        Assert.False( ( ValueOption.get task2.NextResponsePDU ).F )
        Assert.True( ( ValueOption.get task2.NextResponsePDU ).C )
        Assert.True(( cmdsn_me.fromPrim 4u = ValueOption.get ( task2 :> IIscsiTask ).CmdSN ))
        Assert.True(( 512 = ( ValueOption.get task2.NextResponsePDU ).TextResponse.Length ))
        Assert.True(( "TargetAlias=aaa" = Encoding.UTF8.GetString( ( ValueOption.get task2.NextResponsePDU ).TextResponse.[ 0 .. 14 ] ) ))
        Assert.True(( 1 = task2.ContResponsePDUs.Length ))
        Assert.False( task2.ContResponsePDUs.Item( 0 ).F )
        Assert.False( task2.ContResponsePDUs.Item( 0 ).C )
        Assert.True(( 14 = task2.ContResponsePDUs.Item( 0 ).TextResponse.Length ))
        Assert.True(( 0 = task2.ContPDUs.Length ))
        Assert.True(( TextValueType.Value( 512u ) = task2.CurrentNegoParam.MaxRecvDataSegmentLength_I ))
        Assert.True(( NegoStatusValue.NSV_Negotiated = task2.CurrentNegoStatus.NegoStat_MaxRecvDataSegmentLength_I ))
        Assert.True(( NegoStatusValue.NSG_WaitSend = task2.CurrentNegoStatus.NegoStat_TargetAlias ))

        let task3 = IscsiTaskTextNegociation.UpdateNegoStatByReqPDU(
            task2,
            {
                IscsiTaskTextNegociation_Test.defaultRequestPDU with
                    C = false;
                    CmdSN = cmdsn_me.fromPrim 5u;
            }
        )
        Assert.True( ValueOption.isSome task3.NextResponsePDU )
        Assert.False( ( ValueOption.get task3.NextResponsePDU ).F )
        Assert.False( ( ValueOption.get task3.NextResponsePDU ).C )
        Assert.True(( cmdsn_me.fromPrim 5u = ValueOption.get ( task3 :> IIscsiTask ).CmdSN ))
        Assert.True(( 14 = ( ValueOption.get task3.NextResponsePDU ).TextResponse.Length ))
        Assert.True(( 0 = task3.ContResponsePDUs.Length ))
        Assert.True(( TextValueType.Value( 512u ) = task3.CurrentNegoParam.MaxRecvDataSegmentLength_I ))
        Assert.True(( NegoStatusValue.NSV_Negotiated = task3.CurrentNegoStatus.NegoStat_MaxRecvDataSegmentLength_I ))
        Assert.True(( NegoStatusValue.NSV_Negotiated = task3.CurrentNegoStatus.NegoStat_TargetAlias ))

    [<Fact>]
    member _.GetExecuteTask_001() =
        let mutable cnt1 = 0
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_SendOtherResponsePDU = ( fun cid concnt pdu ->
                cnt1 <- cnt1 + 1
                let respdu = pdu :?> TextResponsePDU
                Assert.False( respdu.C )
                Assert.True(( ttt_me.fromPrim 11u = respdu.TargetTransferTag ))
            )
        )

        let task1 = new IscsiTaskTextNegociation(
            objidx_me.NewID(),
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.I,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.InitiatorTaskTag,
            IscsiTaskTextNegociation_Test.defaultRequestPDU.CmdSN,
            TextKeyValues.defaultTextKeyValues,
            TextKeyValuesStatus.defaultTextKeyValuesStatus,
            [],
            [
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 11u;
                };
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 12u;
                };
                {
                    IscsiTaskTextNegociation_Test.defaultTextResponsePDU with
                        TargetTransferTag = ttt_me.fromPrim 13u;
                };
            ],
            false,
            false,
            false
        )

        let struct( ext, nxt ) = ( task1 :> IIscsiTask ).GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.True(( 1 = cnt1 ))

    [<Fact>]
    member _.GetExecuteTask_003() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let connStub = new CConnection_Stub(
            p_NotifyUpdateConnectionParameter = ( fun param ->
                cnt3 <- cnt3 + 1
                Assert.True(( 512u = param.MaxRecvDataSegmentLength_I ))
            ),
            p_CurrentParams = ( fun _ ->
                IscsiTaskTextNegociation_Test.defaultIscsiNegoParamCO
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub ) ),
            p_NoticeUpdateSessionParameter = ( fun param ->
                cnt2 <- cnt2 + 1
                Assert.True(( "Initiator_Alias001" = param.InitiatorAlias ))
            ),
            p_GetSessionParameter = ( fun _ -> IscsiTaskTextNegociation_Test.defaultIscsiNegoParamSW ),
            p_SendOtherResponsePDU = ( fun cid concnt pdu ->
                cnt1 <- cnt1 + 1
                let respdu = pdu :?> TextResponsePDU
                Assert.False( respdu.C )
            )
        )

        let reqpdu = {
            IscsiTaskTextNegociation_Test.defaultRequestPDU with
                C = false;
                F = true;
                TextRequest = [|
                    yield! Encoding.UTF8.GetBytes "MaxRecvDataSegmentLength=512"
                    yield 0uy
                    yield! Encoding.UTF8.GetBytes "InitiatorAlias=Initiator_Alias001"
                |]
        }
        let task1 = IscsiTaskTextNegociation.CreateWithInitParams(
            sessStub :> ISession,
            cid_me.fromPrim 0us,
            concnt_me.fromPrim 0,
            reqpdu,
            IscsiTaskTextNegociation_Test.defaultIscsiNegoParamSW,
            IscsiTaskTextNegociation_Test.defaultIscsiNegoParamCO
        )
        let task2 = IscsiTaskTextNegociation.UpdateNegoStatByReqPDU( task1, reqpdu )

        Assert.True( ValueOption.isSome task2.NextResponsePDU )
        Assert.True( ( ValueOption.get task2.NextResponsePDU ).F )
        Assert.False( ( ValueOption.get task2.NextResponsePDU ).C )
        Assert.True( ( task2 :> IIscsiTask ).IsExecutable )
        Assert.False( ( task2 :> IIscsiTask ).IsRemovable )

        let struct( ext, nxt ) = ( task2 :> IIscsiTask ).GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.False( nxt.IsExecutable )
        Assert.True( nxt.IsRemovable )
        Assert.True(( 1 = cnt1 ))
        Assert.True(( 1 = cnt2 ))
        Assert.True(( 1 = cnt3 ))
