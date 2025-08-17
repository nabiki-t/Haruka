namespace Haruka.Test.UT.TargetDevice

open System
open System.Text
open System.Threading
open System.Threading.Tasks
open System.Security.Cryptography

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes
open Haruka.TargetDevice
open Haruka.Test


type LoginNegociator_Test1 () =

    let g_isid0 = isid_me.zero;

    let g_defaultLoginRequest =
        {
            T = false;
            C = false;
            CSG = LoginReqStateCd.SEQURITY;
            NSG = LoginReqStateCd.SEQURITY;
            VersionMax = 0x00uy;
            VersionMin = 0x00uy;
            ISID = g_isid0;
            TSIH = tsih_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
            CID = cid_me.zero;
            CmdSN = cmdsn_me.zero;
            ExpStatSN = statsn_me.zero;
            TextRequest = [| |];
            ByteCount = 0u;
        }

    let g_defaultiSCSINegoParamSW =
        {
            MaxConnections = Constants.NEGOPARAM_MaxConnections;
            TargetGroupID = tgid_me.Zero;
            TargetConf = 
                {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetName = "";
                TargetAlias = "";
                TargetPortalGroupTag = tpgt_me.zero;
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            };
            InitiatorName = "";
            InitiatorAlias = "";
            TargetPortalGroupTag = tpgt_me.zero;
            InitialR2T = false;
            ImmediateData = false;
            MaxBurstLength = 8192u;
            FirstBurstLength = 8192u;
            DefaultTime2Wait = 2us;
            DefaultTime2Retain = 20us;
            MaxOutstandingR2T = 2us;
            DataPDUInOrder =  false;
            DataSequenceInOrder = false;
            ErrorRecoveryLevel = 2uy;
        }

    let g_defaultiSCSINegoParamCO =
        {
            AuthMethod = Array.empty;
            HeaderDigest = [| DigestType.DST_None |];
            DataDigest = [| DigestType.DST_None |];
            MaxRecvDataSegmentLength_I = 8192u;
            MaxRecvDataSegmentLength_T = 8192u;
        }

    let tsih1 = tsih_me.fromPrim 1us
    let cid1 = cid_me.fromPrim 1us
    let ccnt1 = concnt_me.fromPrim 1

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    [<Fact>]
    member _.New_CLoginNegociator_001() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let server =
           fun () -> task {
               ( new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, new HKiller() :> IKiller ) :> ILoginNegociator ) |> ignore
           }
        let client = fun () -> Task.FromResult ()
        
        Functions.RunTaskInPallalel [| server; client |]
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.Start_001() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        let initiator = 
            fun () -> task {
                let v : byte[] = Array.zeroCreate 10
                cp.Write( v, 0, 10 )
                cp.Close()
            }

        let k1 = new HKiller() :> IKiller
        let rec loop ( cnt : int ) ( k : IKiller ) =
            if k1.IsNoticed then
                true
            else
                if cnt > 20 then
                    false
                else
                    Thread.Sleep 50
                    loop ( cnt + 1 ) k1

        let target = 
            fun () -> task {
                let con =
                    new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ):> ILoginNegociator
                
                con.Start false |> ignore

                // Connection is closed, LoginNegociator is terminated.
                Assert.True( loop 0 k1 )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.Start_002() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let initiator = 
            fun () -> task {
                let v : byte[] = Array.zeroCreate 10
                cp.Write( v, 0, 10 )
                cp.Close()
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? ConnectionErrorException as x ->
                    Assert.True( x.Message = "Connection closed." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ProcessISCSIRequest_002() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- (
            fun arg ->
                Assert.True(  arg.Equals( new ITNexus( "init1", g_isid0, "target1", tpgt_me.zero ) ) )
                raise <| new Exception( "Test_New_ProcessISCSIRequest_002" )
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
                cp.Close()
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | _ as x ->
                    Assert.True( ( x.Message ="Test_New_ProcessISCSIRequest_002" ) )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveFirstPDU_001() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = [| 1uy .. 255uy |]
                        }
                    )
                    |> Functions.TaskIgnore
                let! res =
                    PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                Assert.True( ( res.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.MISSING_PARAMS ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "In iSCSI Login request PDU, required text key does not exist." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]


    [<Fact>]
    member _.ReceiveFirstPDU_002() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = [| 0uy; 0uy; |]
                        }
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "In iSCSI Login request PDU, Login Text Key data format error." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveFirstPDU_003() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.ISV_Irrelevant;
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "In iSCSI Login request PDU, TargetName value should not be reserved value." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveFirstPDU_004() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "AABCDEFGHH._@sss" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "In iSCSI Login request PDU, TargetName value is invalid. format error." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }
            
        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveFirstPDU_005() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
                let! res =
                    PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                Assert.True( ( res.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.MISSING_PARAMS ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "In iSCSI Login request PDU, InitiatorName text key does not exist." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveFirstPDU_006() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "AABCDEFGHH._@sss" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "In iSCSI Login request PDU, InitiatorName value is not iSCSI-name-value." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveFirstPDU_007() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.ISV_NotUnderstood;
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "In iSCSI Login request PDU, InitiatorName value is invalid." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveFirstPDU_008() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "aabcdefghi" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "In iSCSI Login request PDU, SessionType value(aabcdefghi) is invalid." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveFirstPDU_009() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.ISV_Reject;
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "In iSCSI Login request PDU, SessionType value is invalid." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveFirstPDU_010() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
                let! res =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.MISSING_PARAMS ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "In iSCSI Login request PDU, if SessionType is not Discovery session, TargetName key should exist." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveFirstPDU_011() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        g_defaultLoginRequest
                    )
                    |> Functions.TaskIgnore
                let! res =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.MISSING_PARAMS ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "In iSCSI Login request PDU, required text key does not exist." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ProcessNomalSession_001() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 0us )  // *** In target, search result from I_T next is missing.
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TSIH = tsih_me.fromPrim 0x01us;   // *** I->T TSIH is not 0
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
                let! res =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.SESS_NOT_EXIST ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True(( x.Message.StartsWith "Login failed. Specified session is missing." ))
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ProcessNomalSession_002() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )        // *** In target, search result from I_T next is exist, but not matched.
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TSIH = tsih_me.fromPrim 0x01us;                    // *** I->T TSIH is not 0
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
                let! res =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.SESS_NOT_EXIST ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True(( x.Message.StartsWith "Login failed. Specified session is missing." ))
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ProcessNomalSession_003() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> raise <| new Exception ( "Test_ProcessNomalSession_003" ) )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TSIH = tsih_me.zero;                    // *** I->T TSIH is 0
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | _ as x ->
                    Assert.True( ( x.Message ="Test_ProcessNomalSession_003" ) )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |] 
        
    [<Fact>]
    member _.ProcessNomalSession_004() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us ) // *** In target, search result from I_T next is exist, and it is matched.
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetSession <- ( fun _ -> raise <| new Exception( "Test_ProcessNomalSession_004" ) )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TSIH = tsih_me.fromPrim 0x02us;                    // *** I->T TSIH is not 0
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
                }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | _ as x ->
                    Assert.True( ( x.Message ="Test_ProcessNomalSession_004" ) )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ProcessNomalSession_005() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                VersionMax = 0x01uy;
                                VersionMin = 0x01uy;                // VersionMin is not 0
                                TSIH = tsih_me.fromPrim 0x00us;     // *** I->T TSIH is 0
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
                let! res =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.UNSUPPORTED_VERSION ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "Unsupported version is requested." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ProcessNomalSession_006() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 0u;
                            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                            TargetName = "";
                            TargetAlias = "";
                            LUN = [];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TSIH = tsih_me.fromPrim 0x00us;                    // *** I->T TSIH is 0
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
                let! res =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.NOT_FOUND ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( ( x.Message = "TargetName missing." ) )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]


    [<Fact>]
    member _.ProcessNomalSession_008() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user01";
                                        Password = "password01";
                                    };
                                    TargetAuth = {
                                        UserName = "user01";
                                        Password = "password01";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                CSG = LoginReqStateCd.OPERATIONAL;
                                TSIH = tsih_me.fromPrim 0x00us;                    // *** I->T TSIH is 0
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Authentication required." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ProcessNomalSession_009() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user01";
                                        Password = "password01";
                                    };
                                    TargetAuth = {
                                        UserName = "user01";
                                        Password = "password01";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TSIH = tsih_me.fromPrim 0x00us;                    // *** I->T TSIH is 0
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
                cp.Close()
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? ConnectionErrorException as x ->
                    Assert.True( x.Message = "Connection closed." )
                | _ as x ->
                    match x.InnerException with
                    | :? ConnectionErrorException as x ->
                        Assert.True( x.Message = "Connection closed." )
                    | _ as y ->
                        Assert.Fail ( __LINE__ + " : " + y.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ProcessNomalSession_010() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 0us )
        stat1.p_GenNewTSIH <- ( fun _ -> tsih_me.fromPrim 1us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )
        stat1.p_CreateNewSession <- ( fun _ _ _ _ ->
            raise <| Exception( "exp_ProcessNomalSession_010" )
        )

        let initiator =
            fun () -> task {
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                CSG = LoginReqStateCd.SEQURITY;
                                NSG = LoginReqStateCd.OPERATIONAL;
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "initiator001" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_None |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        };
                        }
                    )
                    |> Functions.TaskIgnore
                let! recvPDU2 = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU2.Opcode = OpcodeCd.LOGIN_RES ) );
                let recvPDU2L = recvPDU2 :?> LoginResponsePDU
                Assert.True( recvPDU2L.T = false )
                Assert.True( recvPDU2L.ExpCmdSN = cmdsn_me.fromPrim 0u )
                Assert.True( recvPDU2L.MaxCmdSN = cmdsn_me.fromPrim 0u )
                Assert.True( recvPDU2L.StatSN = statsn_me.fromPrim 0u )

                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                CSG = LoginReqStateCd.SEQURITY;
                                NSG = LoginReqStateCd.OPERATIONAL;
                                TextRequest = Array.empty;
                        }
                    )
                    |> Functions.TaskIgnore

                let! recvPDU3 = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU3.Opcode = OpcodeCd.LOGIN_RES ) );
                let recvPDU3L = recvPDU3 :?> LoginResponsePDU
                Assert.True( recvPDU3L.T = true )
                Assert.True( recvPDU3L.CSG = LoginReqStateCd.SEQURITY )
                Assert.True( recvPDU3L.NSG = LoginReqStateCd.OPERATIONAL )
                Assert.True( recvPDU3L.ExpCmdSN = cmdsn_me.fromPrim 0u )
                Assert.True( recvPDU3L.MaxCmdSN = cmdsn_me.fromPrim 0u )
                Assert.True( recvPDU3L.StatSN = statsn_me.fromPrim 0u )

                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                CSG = LoginReqStateCd.OPERATIONAL;
                                NSG = LoginReqStateCd.FULL;
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                HeaderDigest = TextValueType.Value( [| DigestType.DST_None |] );
                                                DataDigest = TextValueType.Value( [| DigestType.DST_None |] );
                                                MaxConnections = TextValueType.Value( 8us );
                                                InitialR2T = TextValueType.Value( true );
                                                ImmediateData = TextValueType.Value( true );
                                                MaxRecvDataSegmentLength_I = TextValueType.Value( 8192u );
                                                MaxBurstLength = TextValueType.Value( 8192u );
                                                FirstBurstLength = TextValueType.Value( 8192u );
                                                DefaultTime2Wait = TextValueType.Value( 2us );
                                                DefaultTime2Retain = TextValueType.Value( 2us );
                                                MaxOutstandingR2T = TextValueType.Value( 2us );
                                                DataPDUInOrder = TextValueType.Value( true );
                                                DataSequenceInOrder = TextValueType.Value( true );
                                                ErrorRecoveryLevel = TextValueType.Value( 0uy );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_HeaderDigest = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_DataDigest = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_MaxConnections = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_MaxBurstLength = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_FirstBurstLength = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_DefaultTime2Wait = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_DefaultTime2Retain = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_MaxOutstandingR2T = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_DataPDUInOrder = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_DataSequenceInOrder = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_ErrorRecoveryLevel = NegoStatusValue.NSG_WaitSend;
                                        };
                        }
                    )
                    |> Functions.TaskIgnore
                let! recvPDU4 = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU4.Opcode = OpcodeCd.LOGIN_RES ) );
                let recvPDU4L = recvPDU4 :?> LoginResponsePDU
                Assert.True( recvPDU4L.T = false )
                Assert.True( recvPDU4L.ExpCmdSN = cmdsn_me.fromPrim 0u )
                Assert.True( recvPDU4L.MaxCmdSN = cmdsn_me.fromPrim 0u )
                Assert.True( recvPDU4L.StatSN = statsn_me.fromPrim 0u )

                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                CSG = LoginReqStateCd.OPERATIONAL;
                                NSG = LoginReqStateCd.FULL;
                                TextRequest = Array.empty;
                        }
                    )
                    |> Functions.TaskIgnore

                // last PDU of login phase will not be received
                cp.Close()
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | _ as x ->
                    Assert.True(( x.Message = "exp_ProcessNomalSession_010" ))
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ProcessNomalSession_011() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 0us )
        stat1.p_GenNewTSIH <- ( fun _ -> tsih_me.fromPrim 1us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )
        stat1.p_CreateNewSession <- ( fun _ _ _ _ ->
            raise <| Exception( "exp_ProcessNomalSession_011" )
        )

        let initiator =
            fun () -> task {
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                CSG = LoginReqStateCd.SEQURITY;
                                NSG = LoginReqStateCd.FULL;
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "initiator001" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_None |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        };
                        }
                    )
                    |> Functions.TaskIgnore
                let! recvPDU2 = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU2.Opcode = OpcodeCd.LOGIN_RES ) );
                let recvPDU2L = recvPDU2 :?> LoginResponsePDU
                Assert.True( recvPDU2L.T = false )
                Assert.True( recvPDU2L.ExpCmdSN = cmdsn_me.fromPrim 0u )
                Assert.True( recvPDU2L.MaxCmdSN = cmdsn_me.fromPrim 0u )
                Assert.True( recvPDU2L.StatSN = statsn_me.fromPrim 0u )

                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                CSG = LoginReqStateCd.SEQURITY;
                                NSG = LoginReqStateCd.FULL;
                                TextRequest = Array.empty;
                        }
                    )
                    |> Functions.TaskIgnore

                // last PDU of login phase will not be received
                cp.Close()
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | _ as x ->
                    Assert.True(( x.Message = "exp_ProcessNomalSession_011" ))
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]



    [<Fact>]
    member _.ReceiveLoginRequest_001() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let initiator = 
            fun () -> task {
                ()
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                let po2 = new PrivateCaller( pcon )
                try
                    let! rKeyVal, pdu = po2.Invoke( "ReceiveLoginRequest", g_defaultLoginRequest ) :?> Task<struct( TextKeyValues * LoginRequestPDU )>
                    Assert.True( ( pdu = g_defaultLoginRequest ) )
                    Assert.True( ( rKeyVal = TextKeyValues.defaultTextKeyValues ) )
                with
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveLoginRequest_002() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let k1 = new HKiller() :> IKiller
        let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
        let po2 = new PrivateCaller( pcon )

        task {
            try
                let beforePDU =
                    {
                        g_defaultLoginRequest with
                            TextRequest =
                                IscsiTextEncode.CreateTextKeyValueString
                                    {
                                        TextKeyValues.defaultTextKeyValues with
                                            SessionType = TextValueType.Value( "Normal" );
                                    }
                                    {
                                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                            NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                    }
                            
                    }
                let! rKeyVal, pdu =
                    po2.Invoke( "ReceiveLoginRequest", beforePDU ) :?> Task<struct( TextKeyValues * LoginRequestPDU )>
                Assert.True( ( pdu = beforePDU ) )
                Assert.True( ( rKeyVal.SessionType = TextValueType.Value( "Normal" ) ) )
            with
            | _ as x ->
                Assert.Fail ( __LINE__ + " : " + x.Message )
            k1.NoticeTerminate()
        }
        |> Functions.RunTaskSynchronously

        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveLoginRequest_003() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let pdu00 =
            {
                g_defaultLoginRequest with
                    C = true;
                    TextRequest =
                        IscsiTextEncode.CreateTextKeyValueString
                            {
                                TextKeyValues.defaultTextKeyValues with
                                    SessionType = TextValueType.Value( "Normal" );
                            }
                            {
                                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                    NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                            }
                            
            }

        let pdu01 =
            {
                g_defaultLoginRequest with
                    C = false;
                    TextRequest =
                        IscsiTextEncode.CreateTextKeyValueString
                            {
                                TextKeyValues.defaultTextKeyValues with
                                    InitiatorName = TextValueType.Value( "init1" );
                                    TargetName = TextValueType.Value( "target1" );
                            }
                            {
                                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                    NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                    NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                            }
                    ByteCount = 88u;
            }

        let initiator = 
            fun () -> task {
                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res1 :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.SUCCESS ) );
                Assert.True( res2.TextResponse = Array.empty );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        pdu01
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                let po2 = new PrivateCaller( pcon )
                try
                    let! rKeyVal, pdu =
                        po2.Invoke( "ReceiveLoginRequest", pdu00 ) :?> Task<struct( TextKeyValues * LoginRequestPDU )>
                    Assert.True( ( pdu = pdu01 ) )
                    Assert.True( ( rKeyVal.SessionType = TextValueType.Value( "Normal" ) ) )
                    Assert.True( ( rKeyVal.InitiatorName = TextValueType.Value( "init1" ) ) )
                    Assert.True( ( rKeyVal.TargetName = TextValueType.Value( "target1" ) ) )
                with
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveLoginRequest_004() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let pdu00 =
            {
                g_defaultLoginRequest with
                    C = true;
                    TextRequest =
                        IscsiTextEncode.CreateTextKeyValueString
                            {
                                TextKeyValues.defaultTextKeyValues with
                                    SessionType = TextValueType.Value( "Normal" );
                            }
                            {
                                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                    NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                            }
                            
            }

        let pdu01 =
            {
                g_defaultLoginRequest with
                    C = true;
                    TextRequest =
                        IscsiTextEncode.CreateTextKeyValueString
                            {
                                TextKeyValues.defaultTextKeyValues with
                                    InitiatorName = TextValueType.Value( "init1" );
                                    TargetName = TextValueType.Value( "target1" );
                            }
                            {
                                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                    NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                    NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                            }
            }

        let pdu02 =
            {
                g_defaultLoginRequest with
                    C = false;
                    TextRequest =
                        IscsiTextEncode.CreateTextKeyValueString
                            {
                                TextKeyValues.defaultTextKeyValues with
                                    MaxConnections = TextValueType.Value( 5us );
                                    InitiatorAlias = TextValueType.Value( "alias001" );
                            }
                            {
                                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                    NegoStat_MaxConnections = NegoStatusValue.NSG_WaitSend;
                                    NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitSend;
                            }
                    ByteCount = 92u;
            }

        let initiator = 
            fun () -> task {
                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res1 :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.SUCCESS ) );
                Assert.True( res2.TextResponse = Array.empty );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        pdu01
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4 = res3 :?> LoginResponsePDU
                Assert.True( ( res4.T = false ) );
                Assert.True( ( res4.C = false ) );
                Assert.True( ( res4.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res4.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res4.VersionActive = 0uy ) );
                Assert.True( ( res4.VersionMax = 0uy ) );
                Assert.True( ( res4.ISID = g_isid0 ) );
                Assert.True( ( res4.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res4.Status = LoginResStatCd.SUCCESS ) );
                Assert.True( res4.TextResponse = Array.empty );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        pdu02
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                let po2 = new PrivateCaller( pcon )
                try
                    let! rKeyVal, pdu =
                        po2.Invoke( "ReceiveLoginRequest", pdu00 ) :?> Task<struct( TextKeyValues * LoginRequestPDU )>
                    Assert.True( ( pdu = pdu02 ) )
                    Assert.True( ( rKeyVal.SessionType = TextValueType.Value( "Normal" ) ) )
                    Assert.True( ( rKeyVal.InitiatorName = TextValueType.Value( "init1" ) ) )
                    Assert.True( ( rKeyVal.TargetName = TextValueType.Value( "target1" ) ) )
                    Assert.True( ( rKeyVal.MaxConnections = TextValueType.Value( 5us ) ) )
                    Assert.True( ( rKeyVal.InitiatorAlias = TextValueType.Value( "alias001" ) ) )
                with
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.ReceiveLoginRequest_005() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        let k1 = new HKiller() :> IKiller
        let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
        let po2 = new PrivateCaller( pcon )

        task {
            try
                let beforePDU =
                    {
                        g_defaultLoginRequest with
                            TextRequest = [| 'A'B; 'A'B; 'A'B; |]
                    }
                let! _, _ =
                    po2.Invoke( "ReceiveLoginRequest", beforePDU ) :?> Task<struct( TextKeyValues * LoginRequestPDU )>
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                Assert.True( ( x.Message = "In iSCSI Login request PDU, Text request data is invalid." ) )
            | _ as x ->
                Assert.Fail ( __LINE__ + " : " + x.Message )
            k1.NoticeTerminate()
        }
        |> Functions.RunTaskSynchronously
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SendNegotiationResponse_001() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let recvPDU = g_defaultLoginRequest
        let sendTextKey00 =
            {
                TextKeyValues.defaultTextKeyValues with
                    InitiatorName = TextValueType.Value( "init1" );
                    TargetName = TextValueType.Value( "target1" );
                    SessionType = TextValueType.Value( "Normal" );
            }
        let sendStatus00 =
            {
                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                    NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                    NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                    NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
            }

        let initiator = 
            fun () -> task {
                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res1 :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.SUCCESS ) );

                let kv = IscsiTextEncode.RecognizeTextKeyData true [| res2.TextResponse |]
                Assert.True( kv.IsSome )
                Assert.True( ( kv.Value.InitiatorName = TextValueType.Value( "init1" ) ) );
                Assert.True( ( kv.Value.TargetName = TextValueType.Value( "target1" ) ) );
                Assert.True( ( kv.Value.SessionType = TextValueType.Value( "Normal" ) ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                let po2 = new PrivateCaller( pcon )
                try
                    let! rstat =
                        let tsih : TSIH_T voption = ValueNone
                        po2.Invoke( "SendNegotiationResponse", sendTextKey00, sendStatus00, false, tsih, recvPDU ) :?> Task<TextKeyValuesStatus>
                    Assert.True( ( rstat.NegoStat_InitiatorName = NegoStatusValue.NSV_Negotiated ) )
                    Assert.True( ( rstat.NegoStat_TargetName = NegoStatusValue.NSV_Negotiated ) )
                    Assert.True( ( rstat.NegoStat_SessionType = NegoStatusValue.NSV_Negotiated ) )
                with
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SendNegotiationResponse_002() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let recvPDU = g_defaultLoginRequest
        let sendTextKey00 =
            {
                TextKeyValues.defaultTextKeyValues with
                    InitiatorName = TextValueType.Value( "init1" );
                    TargetName = TextValueType.Value( "target1" );
                    TargetAlias = TextValueType.Value( String( 'A', 8200 ) );
                    SessionType = TextValueType.Value( "Normal" );
            }
        let sendStatus00 =
            {
                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                    NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                    NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                    NegoStat_TargetAlias = NegoStatusValue.NSG_WaitSend;
                    NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
            }

        let initiator = 
            fun () -> task {
                let! res1 =
                    PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res1 :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = true ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.SUCCESS ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        g_defaultLoginRequest
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4 = res3 :?> LoginResponsePDU
                Assert.True( ( res4.T = false ) );
                Assert.True( ( res4.C = false ) );
                Assert.True( ( res4.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res4.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res4.VersionActive = 0uy ) );
                Assert.True( ( res4.VersionMax = 0uy ) );
                Assert.True( ( res4.ISID = g_isid0 ) );
                Assert.True( ( res4.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res4.Status = LoginResStatCd.SUCCESS ) );

                let kv = IscsiTextEncode.RecognizeTextKeyData true [| res2.TextResponse; res4.TextResponse; |]
                Assert.True( kv.IsSome )
                Assert.True( ( kv.Value.InitiatorName = TextValueType.Value( "init1" ) ) );
                Assert.True( ( kv.Value.TargetName = TextValueType.Value( "target1" ) ) );
                Assert.True( ( kv.Value.TargetAlias = TextValueType.Value( String( 'A', 8200 ) ) ) );
                Assert.True( ( kv.Value.SessionType = TextValueType.Value( "Normal" ) ) );
            }
        
        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                let po2 = new PrivateCaller( pcon )
                try
                    let! rstat =
                        let tsih : TSIH_T voption = ValueNone
                        po2.Invoke( "SendNegotiationResponse", sendTextKey00, sendStatus00, false, tsih, recvPDU ) :?> Task<TextKeyValuesStatus>
                    Assert.True( ( rstat.NegoStat_InitiatorName = NegoStatusValue.NSV_Negotiated ) )
                    Assert.True( ( rstat.NegoStat_TargetName = NegoStatusValue.NSV_Negotiated ) )
                    Assert.True( ( rstat.NegoStat_TargetAlias = NegoStatusValue.NSV_Negotiated ) )
                    Assert.True( ( rstat.NegoStat_SessionType = NegoStatusValue.NSV_Negotiated ) )
                with
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SendNegotiationResponse_003() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let recvPDU = g_defaultLoginRequest
        let sendTextKey00 =
            {
                TextKeyValues.defaultTextKeyValues with
                    InitiatorName = TextValueType.Value( "init1" );
                    TargetName = TextValueType.Value( "target1" );
                    TargetAlias = TextValueType.Value( String( 'A', 8200 ) );
                    SessionType = TextValueType.Value( "Normal" );
            }
        let sendStatus00 =
            {
                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                    NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                    NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                    NegoStat_TargetAlias = NegoStatusValue.NSG_WaitSend;
                    NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
            }

        let initiator = 
            fun () -> task {
                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res1 :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = true ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.SUCCESS ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = [| 0uy .. 255uy |];
                        }
                    )
                    |> Functions.TaskIgnore
            }
        
        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                let po2 = new PrivateCaller( pcon )
                try
                    let! _ =
                        let tsih : TSIH_T voption = ValueNone
                        po2.Invoke( "SendNegotiationResponse", sendTextKey00, sendStatus00, false, tsih, recvPDU ) :?> Task<TextKeyValuesStatus>
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True(( x.Message.StartsWith "Response of Login response PDU with C bit set to 1" ))
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SendNegotiationResponse_004() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let recvPDU = g_defaultLoginRequest
        let sendTextKey00 = TextKeyValues.defaultTextKeyValues
        let sendStatus00 = TextKeyValuesStatus.defaultTextKeyValuesStatus

        let initiator = 
            fun () -> task {
                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res1 :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.SUCCESS ) );
                Assert.True( res2.TextResponse = Array.empty );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                let po2 = new PrivateCaller( pcon )
                try
                    let! rstat =
                        let tsih : TSIH_T voption = ValueNone
                        po2.Invoke( "SendNegotiationResponse", sendTextKey00, sendStatus00, false, tsih, recvPDU ) :?> Task<TextKeyValuesStatus>
                    Assert.True( ( rstat.NegoStat_InitiatorName = NegoStatusValue.NSV_Negotiated ) )
                    Assert.True( ( rstat.NegoStat_TargetName = NegoStatusValue.NSV_Negotiated ) )
                    Assert.True( ( rstat.NegoStat_SessionType = NegoStatusValue.NSV_Negotiated ) )
                with
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SendNegotiationResponse_005() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()

        let recvPDU = {
            g_defaultLoginRequest with
                CSG = LoginReqStateCd.SEQURITY;
                NSG = LoginReqStateCd.OPERATIONAL;
                T = true;
        }
        let sendTextKey00 = TextKeyValues.defaultTextKeyValues
        let sendStatus00 = TextKeyValuesStatus.defaultTextKeyValuesStatus

        let initiator = 
            fun () -> task {
                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res1 :?> LoginResponsePDU
                Assert.True( ( res2.T = true ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.OPERATIONAL ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.TSIH = tsih_me.fromPrim 987us ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.SUCCESS ) );
                Assert.True( res2.TextResponse = Array.empty );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                let po2 = new PrivateCaller( pcon )
                try
                    let! rstat =
                        let tsih : TSIH_T voption = ValueSome ( tsih_me.fromPrim 987us )
                        po2.Invoke( "SendNegotiationResponse", sendTextKey00, sendStatus00, true, tsih, recvPDU ) :?> Task<TextKeyValuesStatus>
                    Assert.True( ( rstat.NegoStat_InitiatorName = NegoStatusValue.NSV_Negotiated ) )
                    Assert.True( ( rstat.NegoStat_TargetName = NegoStatusValue.NSV_Negotiated ) )
                    Assert.True( ( rstat.NegoStat_SessionType = NegoStatusValue.NSV_Negotiated ) )
                with
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_001() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user01";
                                        Password = "password01";
                                    };
                                    TargetAuth = {
                                        UserName = "user01";
                                        Password = "password01";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res1 :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.SUCCESS ) );

                let kv = IscsiTextEncode.RecognizeTextKeyData true [| res2.TextResponse; |]
                Assert.True( kv.IsSome )
                Assert.True( ( kv.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                AuthMethod = TextValueType.ISV_NotUnderstood;
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Unknown negotiation error." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_002() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user01";
                                        Password = "password01";
                                    };
                                    TargetAuth = {
                                        UserName = "user01";
                                        Password = "password01";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_None |] );

                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res1 :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "AuthMethod mismatch" )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_003() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user01";
                                        Password = "password01";
                                    };
                                    TargetAuth = {
                                        UserName = "user01";
                                        Password = "password01";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2 = res1 :?> LoginResponsePDU
                Assert.True( ( res2.T = false ) );
                Assert.True( ( res2.C = false ) );
                Assert.True( ( res2.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2.VersionActive = 0uy ) );
                Assert.True( ( res2.VersionMax = 0uy ) );
                Assert.True( ( res2.ISID = g_isid0 ) );
                Assert.True( ( res2.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData  true [| res2.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorAlias = TextValueType.Value( "initiatoralias" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4 = res3 :?> LoginResponsePDU
                Assert.True( ( res4.T = false ) );
                Assert.True( ( res4.C = false ) );
                Assert.True( ( res4.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res4.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res4.VersionActive = 0uy ) );
                Assert.True( ( res4.VersionMax = 0uy ) );
                Assert.True( ( res4.ISID = g_isid0 ) );
                Assert.True( ( res4.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res4.Status = LoginResStatCd.SUCCESS ) );
                Assert.True( res4.TextResponse = Array.empty );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res5 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res5.Opcode = OpcodeCd.LOGIN_RES ) );
                let res6 = res5 :?> LoginResponsePDU
                Assert.True( ( res6.T = false ) );
                Assert.True( ( res6.C = false ) );
                Assert.True( ( res6.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res6.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res6.VersionActive = 0uy ) );
                Assert.True( ( res6.VersionMax = 0uy ) );
                Assert.True( ( res6.ISID = g_isid0 ) );
                Assert.True( ( res6.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res6.Status = LoginResStatCd.SUCCESS ) );
                Assert.True( res6.TextResponse = Array.empty );
                cp.Close()
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? ConnectionErrorException as x ->
                    Assert.True( x.Message = "Connection closed." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_004() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_KRB5; AuthMethodCandidateValue.AMC_None |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.T = false ) );
                Assert.True( ( res1L.C = false ) );
                Assert.True( ( res1L.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res1L.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res1L.VersionActive = 0uy ) );
                Assert.True( ( res1L.VersionMax = 0uy ) );
                Assert.True( ( res1L.ISID = g_isid0 ) );
                Assert.True( ( res1L.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_None |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        g_defaultLoginRequest
                    )
                    |> Functions.TaskIgnore

                let! res2 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res2.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2L = res2 :?> LoginResponsePDU
                Assert.True( ( res2L.T = false ) );
                Assert.True( ( res2L.C = false ) );
                Assert.True( ( res2L.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2L.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res2L.VersionActive = 0uy ) );
                Assert.True( ( res2L.VersionMax = 0uy ) );
                Assert.True( ( res2L.ISID = g_isid0 ) );
                Assert.True( ( res2L.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res2L.Status = LoginResStatCd.SUCCESS ) );
                Assert.True( res2L.TextResponse = Array.empty );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                NSG = LoginReqStateCd.OPERATIONAL;
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorAlias = TextValueType.Value( "initiatoralias" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.T = true ) );
                Assert.True( ( res3L.C = false ) );
                Assert.True( ( res3L.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res3L.NSG = LoginReqStateCd.OPERATIONAL ) );
                Assert.True( ( res3L.VersionActive = 0uy ) );
                Assert.True( ( res3L.VersionMax = 0uy ) );
                Assert.True( ( res3L.ISID = g_isid0 ) );
                Assert.True( ( res3L.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );
                Assert.True( res3L.TextResponse =  Array.empty );
                cp.Close()
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? ConnectionErrorException as x ->
                    Assert.True( x.Message = "Connection closed." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_005() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_KRB5; AuthMethodCandidateValue.AMC_None |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.T = false ) );
                Assert.True( ( res1L.C = false ) );
                Assert.True( ( res1L.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res1L.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res1L.VersionActive = 0uy ) );
                Assert.True( ( res1L.VersionMax = 0uy ) );
                Assert.True( ( res1L.ISID = g_isid0 ) );
                Assert.True( ( res1L.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_None |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                NSG = LoginReqStateCd.OPERATIONAL;
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorAlias = TextValueType.Value( "initiatoralias" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.T = true ) );
                Assert.True( ( res3L.C = false ) );
                Assert.True( ( res3L.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res3L.NSG = LoginReqStateCd.OPERATIONAL ) );
                Assert.True( ( res3L.VersionActive = 0uy ) );
                Assert.True( ( res3L.VersionMax = 0uy ) );
                Assert.True( ( res3L.ISID = g_isid0 ) );
                Assert.True( ( res3L.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );
                Assert.True( res3L.TextResponse =  Array.empty );
                cp.Close()
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? ConnectionErrorException as x ->
                    Assert.True( x.Message = "Connection closed." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_006() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_KRB5; AuthMethodCandidateValue.AMC_None |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.T = false ) );
                Assert.True( ( res1L.C = false ) );
                Assert.True( ( res1L.CSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res1L.NSG = LoginReqStateCd.SEQURITY ) );
                Assert.True( ( res1L.VersionActive = 0uy ) );
                Assert.True( ( res1L.VersionMax = 0uy ) );
                Assert.True( ( res1L.ISID = g_isid0 ) );
                Assert.True( ( res1L.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) );
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_None |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                NSG = LoginReqStateCd.OPERATIONAL;
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorAlias = TextValueType.Value( "initiatoralias" );
                                                TargetName = TextValueType.Value( "target1" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Unknown negotiation error." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_001() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "user2";
                                        Password = "password2";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_KRB5; AuthMethodCandidateValue.AMC_None |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.ISV_Irrelevant;
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Protocol error. CHAP_A value is invalid." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_002() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "user2";
                                        Password = "password2";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_KRB5; AuthMethodCandidateValue.AMC_None; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 0us; 1us; 2us; 3us; 4us; 6us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
                let! res2 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res2.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2L = res2 :?> LoginResponsePDU
                Assert.True( ( res2L.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Proposed CHAP_A value is not supported." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_003() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "user2";
                                        Password = "password2";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res2 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res2.Opcode = OpcodeCd.LOGIN_RES ) );
                let res2L = res2 :?> LoginResponsePDU
                Assert.True( ( res2L.Status = LoginResStatCd.SUCCESS ) );
                Assert.True( res2L.TextResponse = Array.empty );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 0us; 1us; 2us; 3us; 4us; 5us; 6us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );

                let kv3 = IscsiTextEncode.RecognizeTextKeyData true [| res3L.TextResponse; |]
                Assert.True( kv3.IsSome )
                Assert.True( ( kv3.Value.CHAP_A = TextValueType.Value( [| 5us; |] ) ) );
                Assert.True( kv3.Value.CHAP_I.GetValue >= 0us && kv3.Value.CHAP_I.GetValue <= 255us );

                let _ = kv3.Value.CHAP_C.GetValue

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_N = TextValueType.ISV_NotUnderstood;
                                                CHAP_R = TextValueType.Value( [| 0uy .. 255uy |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res4 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res4.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4L = res4 :?> LoginResponsePDU
                Assert.True( ( res4L.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Protocol error. CHAP_N value is invalid." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_004() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "user2";
                                        Password = "password2";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 5us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );

                let kv3 = IscsiTextEncode.RecognizeTextKeyData true [| res3L.TextResponse; |]
                Assert.True( kv3.IsSome )
                Assert.True( ( kv3.Value.CHAP_A = TextValueType.Value( [| 5us; |] ) ) );
                Assert.True( kv3.Value.CHAP_I.GetValue >= 0us && kv3.Value.CHAP_I.GetValue <= 255us );

                let _ = kv3.Value.CHAP_C.GetValue

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_N = TextValueType.Value( "aaaaa01" );
                                                CHAP_R = TextValueType.ISV_Reject;
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res4 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res4.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4L = res4 :?> LoginResponsePDU
                Assert.True( ( res4L.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Protocol error. CHAP_R value is invalid." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_005() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "user2";
                                        Password = "password2";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 5us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore
                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );

                let kv3 = IscsiTextEncode.RecognizeTextKeyData true [| res3L.TextResponse; |]
                Assert.True( kv3.IsSome )
                Assert.True( ( kv3.Value.CHAP_A = TextValueType.Value( [| 5us; |] ) ) );
                Assert.True( kv3.Value.CHAP_I.GetValue >= 0us && kv3.Value.CHAP_I.GetValue <= 255us );

                let _ = kv3.Value.CHAP_C.GetValue

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_N = TextValueType.Value( "aaaaa01" );
                                                CHAP_R = TextValueType.Value( [| 0uy; |] )
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res4 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome ccnt1, cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res4.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4L = res4 :?> LoginResponsePDU
                Assert.True( ( res4L.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Invalid user name or password." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_006() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "user2";
                                        Password = "password2";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome ( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 5us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );

                let kv3 = IscsiTextEncode.RecognizeTextKeyData true [| res3L.TextResponse; |]
                Assert.True( kv3.IsSome )
                Assert.True( ( kv3.Value.CHAP_A = TextValueType.Value( [| 5us; |] ) ) );
                Assert.True( kv3.Value.CHAP_I.GetValue >= 0us && kv3.Value.CHAP_I.GetValue <= 255us );

                let initiatorChallange = kv3.Value.CHAP_C.GetValue
                let chapRValue = 
                    ( MD5.Create() ).ComputeHash
                        [|
                            yield byte kv3.Value.CHAP_I.GetValue;
                            yield! Encoding.UTF8.GetBytes "password1";
                            yield! initiatorChallange
                        |]

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_N = TextValueType.Value( "aaaaa01" );
                                                CHAP_R = TextValueType.Value( chapRValue )
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res4 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res4.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4L = res4 :?> LoginResponsePDU
                Assert.True( ( res4L.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Invalid user name or password." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_007() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "user2";
                                        Password = "password2";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 5us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );

                let kv3 = IscsiTextEncode.RecognizeTextKeyData true [| res3L.TextResponse; |]
                Assert.True( kv3.IsSome )
                Assert.True( ( kv3.Value.CHAP_A = TextValueType.Value( [| 5us; |] ) ) );
                Assert.True( kv3.Value.CHAP_I.GetValue >= 0us && kv3.Value.CHAP_I.GetValue <= 255us );

                let initiatorChallange = kv3.Value.CHAP_C.GetValue
                let chapRValue = 
                    ( MD5.Create() ).ComputeHash
                        [|
                            yield byte kv3.Value.CHAP_I.GetValue;
                            yield! Encoding.UTF8.GetBytes "password1";
                            yield! initiatorChallange
                        |]

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_N = TextValueType.Value( "user1" );
                                                CHAP_R = TextValueType.Value( chapRValue );
                                                CHAP_I = TextValueType.ISV_Irrelevant;
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_I = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res4 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res4.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4L = res4 :?> LoginResponsePDU
                Assert.True( ( res4L.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Protocol error. CHAP_I value is invalid." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_008() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "user2";
                                        Password = "password2";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 5us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );

                let kv3 = IscsiTextEncode.RecognizeTextKeyData true [| res3L.TextResponse; |]
                Assert.True( kv3.IsSome )
                Assert.True( ( kv3.Value.CHAP_A = TextValueType.Value( [| 5us; |] ) ) );
                Assert.True( ( kv3.Value.CHAP_I.GetValue >= 0us && kv3.Value.CHAP_I.GetValue <= 255us ) );

                let initiatorChallange = kv3.Value.CHAP_C.GetValue
                let chapRValue = 
                    ( MD5.Create() ).ComputeHash
                        [|
                            yield byte kv3.Value.CHAP_I.GetValue;
                            yield! Encoding.UTF8.GetBytes "password1";
                            yield! initiatorChallange
                        |]

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_N = TextValueType.Value( "user1" );
                                                CHAP_R = TextValueType.Value( chapRValue );
                                                CHAP_C = TextValueType.ISV_NotUnderstood;
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_C = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res4 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res4.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4L = res4 :?> LoginResponsePDU
                Assert.True( ( res4L.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Protocol error. CHAP_C value is invalid." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_009() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "user2";
                                        Password = "password2";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 5us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );

                let kv3 = IscsiTextEncode.RecognizeTextKeyData true [| res3L.TextResponse; |]
                Assert.True( kv3.IsSome )
                Assert.True( ( kv3.Value.CHAP_A = TextValueType.Value( [| 5us; |] ) ) );
                Assert.True( kv3.Value.CHAP_I.GetValue >= 0us && kv3.Value.CHAP_I.GetValue <= 255us );

                let initiatorChallange = kv3.Value.CHAP_C.GetValue
                let chapRValue = 
                    ( MD5.Create() ).ComputeHash
                        [|
                            yield byte kv3.Value.CHAP_I.GetValue;
                            yield! Encoding.UTF8.GetBytes "password1";
                            yield! initiatorChallange
                        |]

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_N = TextValueType.Value( "user1" );
                                                CHAP_R = TextValueType.Value( chapRValue );
                                                CHAP_I = TextValueType.Value( 64us );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_I = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res4 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res4.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4L = res4 :?> LoginResponsePDU
                Assert.True( ( res4L.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Protocol error. CHAP_I or CHAP_C value is invalid." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_010() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "user2";
                                        Password = "password2";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )
        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 5us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );

                let kv3 = IscsiTextEncode.RecognizeTextKeyData true [| res3L.TextResponse; |]
                Assert.True( kv3.IsSome )
                Assert.True( ( kv3.Value.CHAP_A = TextValueType.Value( [| 5us; |] ) ) );
                Assert.True( kv3.Value.CHAP_I.GetValue >= 0us && kv3.Value.CHAP_I.GetValue <= 255us );

                let initiatorChallange = kv3.Value.CHAP_C.GetValue
                let chapRValue = 
                    ( MD5.Create() ).ComputeHash
                        [|
                            yield byte kv3.Value.CHAP_I.GetValue;
                            yield! Encoding.UTF8.GetBytes "password1";
                            yield! initiatorChallange
                        |]

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_N = TextValueType.Value( "user1" );
                                                CHAP_R = TextValueType.Value( chapRValue );
                                                CHAP_C = TextValueType.Value( [| 16uy .. 200uy |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_C = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res4 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res4.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4L = res4 :?> LoginResponsePDU
                Assert.True( ( res4L.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Protocol error. CHAP_I or CHAP_C value is invalid." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_011() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "user2";
                                        Password = "password2";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )


        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 5us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );

                let kv3 = IscsiTextEncode.RecognizeTextKeyData true [| res3L.TextResponse; |]
                Assert.True( kv3.IsSome )
                Assert.True( ( kv3.Value.CHAP_A = TextValueType.Value( [| 5us; |] ) ) );
                Assert.True( kv3.Value.CHAP_I.GetValue >= 0us && kv3.Value.CHAP_I.GetValue <= 255us );

                let initiatorChallange = kv3.Value.CHAP_C.GetValue
                let chapRValue = 
                    ( MD5.Create() ).ComputeHash
                        [|
                            yield byte kv3.Value.CHAP_I.GetValue;
                            yield! Encoding.UTF8.GetBytes "password1";
                            yield! initiatorChallange
                        |]

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_N = TextValueType.Value( "user1" );
                                                CHAP_R = TextValueType.Value( chapRValue );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res4 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res4.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4L = res4 :?> LoginResponsePDU
                Assert.True( ( res4L.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Invalid user name or password." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_012() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "user2";
                                        Password = "password2";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )
        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 5us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );

                let kv3 = IscsiTextEncode.RecognizeTextKeyData true [| res3L.TextResponse; |]
                Assert.True( kv3.IsSome )
                Assert.True( ( kv3.Value.CHAP_A = TextValueType.Value( [| 5us; |] ) ) );
                Assert.True( kv3.Value.CHAP_I.GetValue >= 0us && kv3.Value.CHAP_I.GetValue <= 255us );

                let initiatorChallange = kv3.Value.CHAP_C.GetValue
                let chapRValue = 
                    ( MD5.Create() ).ComputeHash
                        [|
                            yield byte kv3.Value.CHAP_I.GetValue;
                            yield! Encoding.UTF8.GetBytes "password1";
                            yield! initiatorChallange
                        |]

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_N = TextValueType.Value( "user1" );
                                                CHAP_R = TextValueType.Value( chapRValue );
                                                CHAP_I = TextValueType.Value( 64us );
                                                CHAP_C = TextValueType.Value( [| 16uy .. 200uy |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_I = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_C = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let chapRValue2 = 
                    ( MD5.Create() ).ComputeHash
                        [|
                            yield byte 64us;
                            yield! Encoding.UTF8.GetBytes "password2";
                            yield! [| 16uy .. 200uy |]
                        |]

                let! res4 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res4.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4L = res4 :?> LoginResponsePDU
                Assert.True( ( res4L.Status = LoginResStatCd.SUCCESS ) );

                let kv4 = IscsiTextEncode.RecognizeTextKeyData true [| res4L.TextResponse; |]
                Assert.True( kv4.IsSome )
                Assert.True( ( kv4.Value.CHAP_N = TextValueType.Value( "user2" ) ) );
                Assert.True( ( kv4.Value.CHAP_R = TextValueType.Value( chapRValue2 ) ) );
                cp.Close()
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? ConnectionErrorException as x ->
                    Assert.True( x.Message = "Connection closed." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_013() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "";
                                        Password = "";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 5us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );

                let kv3 = IscsiTextEncode.RecognizeTextKeyData true [| res3L.TextResponse; |]
                Assert.True( kv3.IsSome )
                Assert.True( ( kv3.Value.CHAP_A = TextValueType.Value( [| 5us; |] ) ) );
                Assert.True( kv3.Value.CHAP_I.GetValue >= 0us && kv3.Value.CHAP_I.GetValue <= 255us );

                let initiatorChallange = kv3.Value.CHAP_C.GetValue
                let chapRValue = 
                    ( MD5.Create() ).ComputeHash
                        [|
                            yield byte kv3.Value.CHAP_I.GetValue;
                            yield! Encoding.UTF8.GetBytes "password1";
                            yield! initiatorChallange
                        |]

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_N = TextValueType.Value( "user1" );
                                                CHAP_R = TextValueType.Value( chapRValue );
                                                CHAP_I = TextValueType.Value( 64us );
                                                CHAP_C = TextValueType.Value( [| 16uy .. 200uy |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_I = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_C = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res4 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res4.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4L = res4 :?> LoginResponsePDU
                Assert.True( ( res4L.Status = LoginResStatCd.AUTH_FAILURE ) );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Invalid user name or password." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.SequrityNegotiation_WithCHAP_014() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        stat1.p_GetTSIH <- ( fun _ -> tsih_me.fromPrim 2us )
        stat1.p_GetISCSINegoParamCO <- ( fun () -> g_defaultiSCSINegoParamCO )
        stat1.p_GetISCSINegoParamSW <- ( fun () -> g_defaultiSCSINegoParamSW )
        stat1.p_GenNewTSIH <- ( fun () -> tsih_me.fromPrim 20us )
        stat1.p_GetActiveTargetGroup <- ( fun () ->
            [
                {
                    TargetGroupID = tgid_me.Zero;
                    TargetGroupName = "targetgroup001";
                    EnabledAtStart = true;
                    Target = [
                        {
                            IdentNumber = tnodeidx_me.fromPrim 10u;
                            TargetName = "target1"
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.zero;
                            LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                            Auth = TargetGroupConf.T_Auth.U_CHAP(
                                {
                                    InitiatorAuth = {
                                        UserName = "user1";
                                        Password = "password1";
                                    };
                                    TargetAuth = {
                                        UserName = "";
                                        Password = "";
                                    };
                                }
                            );
                        }
                    ];
                    LogicalUnit = [];
                }
            ]
        )

        let initiator = 
            fun () -> task {
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                InitiatorName = TextValueType.Value( "init1" );
                                                TargetName = TextValueType.Value( "target1" );
                                                SessionType = TextValueType.Value( "Normal" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res1 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res1.Opcode = OpcodeCd.LOGIN_RES ) );
                let res1L = res1 :?> LoginResponsePDU
                Assert.True( ( res1L.Status = LoginResStatCd.SUCCESS ) );

                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| res1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( ( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 0us ) ) );
                Assert.True( ( kv1.Value.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_A = TextValueType.Value( [| 5us; |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res3 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res3.Opcode = OpcodeCd.LOGIN_RES ) );
                let res3L = res3 :?> LoginResponsePDU
                Assert.True( ( res3L.Status = LoginResStatCd.SUCCESS ) );

                let kv3 = IscsiTextEncode.RecognizeTextKeyData true [| res3L.TextResponse; |]
                Assert.True( kv3.IsSome )
                Assert.True( ( kv3.Value.CHAP_A = TextValueType.Value( [| 5us; |] ) ) );
                Assert.True( kv3.Value.CHAP_I.GetValue >= 0us && kv3.Value.CHAP_I.GetValue <= 255us );

                let initiatorChallange = kv3.Value.CHAP_C.GetValue
                let chapRValue = 
                    ( MD5.Create() ).ComputeHash
                        [|
                            yield byte kv3.Value.CHAP_I.GetValue;
                            yield! Encoding.UTF8.GetBytes "password1";
                            yield! initiatorChallange
                        |]

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                TextRequest = 
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                CHAP_N = TextValueType.Value( "user1" );
                                                CHAP_R = TextValueType.Value( chapRValue );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                                        }
                        }
                    )
                    |> Functions.TaskIgnore

                let! res4 =
                    try
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    with
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                        reraise()
                Assert.True( ( res4.Opcode = OpcodeCd.LOGIN_RES ) );
                let res4L = res4 :?> LoginResponsePDU
                Assert.True( ( res4L.Status = LoginResStatCd.SUCCESS ) );
                Assert.True( res4L.TextResponse = Array.empty );
                cp.Close()
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let con = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 ) :> ILoginNegociator
                try
                    con.Start true |> ignore
                    Assert.Fail __LINE__
                with
                | :? ConnectionErrorException as x ->
                    Assert.True( x.Message = "Connection closed." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }

        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.CreateOrAddNewConnection_002() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        let sess1 = new CSession_Stub()
        let i_tNext = new ITNexus( "initiator001", isid_me.zero, "target001", tpgt_me.zero )
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argSWParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = {
                    IdentNumber = tnodeidx_me.fromPrim 1u;
                    TargetName = "target001"
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
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

        stat1.p_CreateNewSession <- (
            fun a1 a2 a3 _ ->
                Assert.True( ( a1 = i_tNext ) )
                Assert.True( ( a2 = tsih1 ) )
                Assert.True( ( a3 = argSWParam ) )
                ValueSome( sess1 :> ISession )
        )

        let k1 = new HKiller() :> IKiller
        let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.fromPrim 3u, k1 )
        let po2 = new PrivateCaller( pcon )

        sess1.p_GetSessionParameter <- (
            fun _ ->
                {
                    MaxConnections = Constants.NEGOPARAM_MaxConnections;
                    TargetGroupID = tgid_me.Zero;
                    TargetConf = {
                        IdentNumber = tnodeidx_me.fromPrim 1u;
                        TargetName = "target001"
                        TargetAlias = "";
                        TargetPortalGroupTag = tpgt_me.zero;
                        LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
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
        )

        sess1.p_AddNewConnection <- (
            fun _ _ newCID npidx tpgt _ ->
                Assert.True( ( newCID = cid_me.fromPrim 15us ) )
                Assert.True( ( npidx = netportidx_me.fromPrim 3u ) )
                Assert.True( ( tpgt = tpgt_me.zero ) )
                true
        )

        try
            po2.Invoke(
                "CreateOrAddNewConnection",
                true,                   // isLeadingCon
                false,                  // isConnRebuild
                tsih_me.zero,           // dropSessionTSIH
                tsih1,                  // newTSIH
                i_tNext,                // i_tNextIdent
                cid_me.fromPrim 15us,   // recvCID
                cmdsn_me.fromPrim 111u, // newCmdSN
                statsn_me.fromPrim 222u,// firstExpStatSN
                argCOParam,             // iSCSIParamsCO : IscsiNegoParamCO
                argSWParam              // iSCSIParamsSW
            ) |> ignore
        with
        | _ as x ->
            Assert.Fail ( __LINE__ + " : " + x.Message )
        k1.NoticeTerminate()
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.CreateOrAddNewConnection_003() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        let sess1 = new CSession_Stub()
        let sess2 = new CSession_Stub()
        let i_tNext = new ITNexus( "initiator001", isid_me.zero, "target001", tpgt_me.zero )
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argSWParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = {
                    IdentNumber = tnodeidx_me.fromPrim 1u;
                    TargetName = "target001"
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
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

        stat1.p_GetSession <- (
            fun a1 ->
                Assert.True( tsih_me.fromPrim 99us = a1 )
                ValueSome( sess1 :> ISession )
        )
        sess1.p_DestroySession <- ( fun _ -> () )

        stat1.p_CreateNewSession <- (
            fun a1 a2 a3 a4 ->
                Assert.True( ( a1 = i_tNext ) )
                Assert.True( ( a2 = tsih_me.fromPrim 10us ) )
                Assert.True( ( a3 = argSWParam ) )
                Assert.True( ( a4 = cmdsn_me.fromPrim 123u ) )
                ValueSome( sess2 :> ISession )
        )

        sess2.p_GetSessionParameter <- ( fun _ -> argSWParam )
        sess2.p_AddNewConnection <- (
            fun _ _ newCID npidx tpgt _ ->
                Assert.True( ( newCID = cid_me.fromPrim 15us ) )
                Assert.True( ( npidx = netportidx_me.zero ) )
                Assert.True( ( tpgt = tpgt_me.zero ) )
                true
        )

        let k1 = new HKiller() :> IKiller
        let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
        let po2 = new PrivateCaller( pcon )
        try
            po2.Invoke(
                "CreateOrAddNewConnection",
                true,                   // isLeadingCon
                false,                  // isConnRebuild
                tsih_me.fromPrim 99us,  // dropSessionTSIH
                tsih_me.fromPrim 10us,  // newTSIH
                i_tNext,                // i_tNextIdent
                cid_me.fromPrim 15us,   // recvCID
                cmdsn_me.fromPrim 123u, // newCmdSN
                statsn_me.fromPrim 222u,// firstExpStatSN
                argCOParam,             // iSCSIParamsCO : IscsiNegoParamCO
                argSWParam              // iSCSIParamsSW
            ) |> ignore
        with
        | _ as x ->
            Assert.Fail ( __LINE__ + " : " + x.Message )
        k1.NoticeTerminate()
        GlbFunc.ClosePorts [| sp; cp |]
       
    [<Fact>]
    member _.CreateOrAddNewConnection_004() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        let sess1 = new CSession_Stub()
        let sess2 = new CSession_Stub()
        let i_tNext = new ITNexus( "initiator001", isid_me.zero, "target001", tpgt_me.zero )
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argSWParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = {
                    IdentNumber = tnodeidx_me.fromPrim 1u;
                    TargetName = "target001"
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
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

        stat1.p_GetSession <- (
            fun a1 ->
                Assert.True( tsih_me.fromPrim 99us = a1 )
                ValueSome( sess1 :> ISession )
        )
        sess1.p_DestroySession <- ( fun _ -> () )
        stat1.p_CreateNewSession <- (
            fun a1 a2 a3 a4 ->
                Assert.True( ( a1 = i_tNext ) )
                Assert.True( ( a2 = tsih_me.fromPrim 4us ) )
                Assert.True( ( a3 = argSWParam ) )
                Assert.True( ( a4 = cmdsn_me.fromPrim 136u ) )
                ValueSome( sess2 :> ISession )
        )

        let k1 = new HKiller() :> IKiller
        let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.fromPrim 4u, k1 )
        let po2 = new PrivateCaller( pcon )

        sess2.p_GetSessionParameter <- (
            fun _ ->
                {
                    argSWParam with
                        InitiatorName = "initiator999";
                        InitiatorAlias = "INITIATOR001";
                }
        )

        sess2.p_AddNewConnection <- (
            fun _ _ newCID npidx tpgt _ ->
                Assert.True( ( newCID = cid_me.fromPrim 5us ) )
                Assert.True( ( npidx = netportidx_me.fromPrim 4u ) )
                Assert.True( ( tpgt = tpgt_me.zero ) )
                false
        )

        try
            po2.Invoke(
                "CreateOrAddNewConnection",
                true,                   // isLeadingCon
                false,                  // isConnRebuild
                tsih_me.fromPrim 99us,  // dropSessionTSIH
                tsih_me.fromPrim 4us,   // newTSIH
                i_tNext,                // i_tNextIdent
                cid_me.fromPrim 5us,    // recvCID
                cmdsn_me.fromPrim 136u, // newCmdSN
                statsn_me.fromPrim 222u,// firstExpStatSN
                argCOParam,             // iSCSIParamsCO : IscsiNegoParamCO
                argSWParam              // iSCSIParamsSW
            ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( x.Message.Contains "Consistency error. A different initiator attempted to log in to an existing session." )
        | _ as x ->
            Assert.Fail ( __LINE__ + " : " + x.Message )
        k1.NoticeTerminate()
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.CreateOrAddNewConnection_005() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        let sess1 = new CSession_Stub()
        let sess2 = new CSession_Stub()

        let i_tNext = new ITNexus( "initiator001", isid_me.zero, "target001", tpgt_me.zero )
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argSWParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = {
                    IdentNumber = tnodeidx_me.fromPrim 1u;
                    TargetName = "target001"
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
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

        stat1.p_GetSession <- (
            fun a1 ->
                Assert.True( tsih_me.fromPrim 99us = a1 )
                ValueSome( sess1 :> ISession )
        )
        sess1.p_DestroySession <- ( fun _ -> () )
        stat1.p_CreateNewSession <- (
            fun a1 a2 a3 a4 ->
                Assert.True( ( a1 = i_tNext ) )
                Assert.True( ( a2 = tsih_me.fromPrim 4us ) )
                Assert.True( ( a3 = argSWParam ) )
                Assert.True( ( a4 = cmdsn_me.fromPrim 136u ) )
                ValueSome( sess2 :> ISession )
        )

        let k1 = new HKiller() :> IKiller
        let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.fromPrim 6u, k1 )
        let po2 = new PrivateCaller( pcon )

        sess2.p_GetSessionParameter <- ( fun _ -> argSWParam )

        sess2.p_AddNewConnection <- (
            fun _ _ newCID npidx tpgt _ ->
                Assert.True( ( newCID = cid_me.fromPrim 5us ) )
                Assert.True( ( npidx = netportidx_me.fromPrim 6u ) )
                Assert.True( ( tpgt = tpgt_me.zero ) )
                false
        )

        try
            po2.Invoke(
                "CreateOrAddNewConnection",
                true,                   // isLeadingCon
                false,                  // isConnRebuild
                tsih_me.fromPrim 99us,  // dropSessionTSIH
                tsih_me.fromPrim 4us,   // newTSIH
                i_tNext,                // i_tNextIdent
                cid_me.fromPrim 5us,    // recvCID
                cmdsn_me.fromPrim 136u, // newCmdSN
                statsn_me.fromPrim 222u,// firstExpStatSN
                argCOParam,             // iSCSIParamsCO : IscsiNegoParamCO
                argSWParam              // iSCSIParamsSW
            ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( x.Message = "Consistency error. May be specified connection is already exist." )
        | _ as x ->
            Assert.Fail ( __LINE__ + " : " + x.Message )
        k1.NoticeTerminate()
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.CreateOrAddNewConnection_006() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        let sess1 = new CSession_Stub()
        let i_tNext = new ITNexus( "initiator001", isid_me.zero, "target001", tpgt_me.zero )
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argSWParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = {
                    IdentNumber = tnodeidx_me.fromPrim 1u;
                    TargetName = "target001"
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
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

        stat1.p_GetSession <- (
            fun a1 ->
                Assert.True( tsih_me.fromPrim 4us = a1 )
                ValueSome( sess1 :> ISession )
        )

        let k1 = new HKiller() :> IKiller
        let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.fromPrim 7u, k1 )
        let po2 = new PrivateCaller( pcon )

        sess1.p_ReinstateConnection <- (
            fun _ _ newCID npidx tpgt _ ->
                Assert.True( ( newCID = cid_me.fromPrim 15us ) )
                Assert.True( ( npidx = netportidx_me.fromPrim 7u ) )
                Assert.True( ( tpgt = tpgt_me.zero ) )
                true
        )

        try
            po2.Invoke(
                "CreateOrAddNewConnection",
                false,                  // isLeadingCon
                true,                   // isConnRebuild
                tsih_me.zero,           // dropSessionTSIH
                tsih_me.fromPrim 4us,   // newTSIH
                i_tNext,                // i_tNextIdent
                cid_me.fromPrim 15us,   // recvCID
                cmdsn_me.fromPrim 124u, // newCmdSN
                statsn_me.fromPrim 222u,// firstExpStatSN
                argCOParam,             // iSCSIParamsCO : IscsiNegoParamCO
                argSWParam              // iSCSIParamsSW
            ) |> ignore
        with
        | _ as x ->
            Assert.Fail ( __LINE__ + " : " + x.Message )
        k1.NoticeTerminate()
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.CreateOrAddNewConnection_007() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        let sess1 = new CSession_Stub()

        let i_tNext = new ITNexus( "initiator001", isid_me.zero, "target001", tpgt_me.zero )
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argSWParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = {
                    IdentNumber = tnodeidx_me.fromPrim 1u;
                    TargetName = "target001"
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
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

        stat1.p_GetSession <- (
            fun a1 ->
                Assert.True( tsih_me.fromPrim 4us = a1 )
                ValueSome( sess1 :> ISession )
        )

        let k1 = new HKiller() :> IKiller
        let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.fromPrim 8u, k1 )
        let po2 = new PrivateCaller( pcon )

        sess1.p_ReinstateConnection <- (
            fun _ _ newCID npidx tpgt _ ->
                Assert.True( ( newCID = cid_me.fromPrim 14us ) )
                Assert.True( ( npidx = netportidx_me.fromPrim 8u ) )
                Assert.True( ( tpgt = tpgt_me.zero ) )
                false
        )

        try
            po2.Invoke(
                "CreateOrAddNewConnection",
                false,                  // isLeadingCon
                true,                   // isConnRebuild
                tsih_me.zero,           // dropSessionTSIH
                tsih_me.fromPrim 4us,   // newTSIH
                i_tNext,                // i_tNextIdent
                cid_me.fromPrim 14us,   // recvCID
                cmdsn_me.fromPrim 111u, // newCmdSN
                statsn_me.fromPrim 222u,// firstExpStatSN
                argCOParam,             // iSCSIParamsCO : IscsiNegoParamCO
                argSWParam              // iSCSIParamsSW
            ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( x.Message = "Consistency error. May be specified connection is not exist." )
        | _ as x ->
            Assert.Fail ( __LINE__ + " : " + x.Message )
        k1.NoticeTerminate()
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.CreateOrAddNewConnection_008() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        let sess1 = new CSession_Stub()
        let i_tNext = new ITNexus( "initiator001", isid_me.zero, "target001", tpgt_me.zero )
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argSWParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = {
                    IdentNumber = tnodeidx_me.fromPrim 1u;
                    TargetName = "target001"
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
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

        stat1.p_GetSession <- (
            fun a1 ->
                Assert.True( tsih_me.fromPrim 4us = a1 )
                ValueSome( sess1 :> ISession )
        )

        let k1 = new HKiller() :> IKiller
        let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.fromPrim 89u, k1 )
        let po2 = new PrivateCaller( pcon )

        sess1.p_ReinstateConnection <- (
            fun _ _ newCID npidx tpgt _ ->
                Assert.True( ( newCID = cid_me.fromPrim 12us ) )
                Assert.True( ( npidx = netportidx_me.fromPrim 89u ) )
                Assert.True( ( tpgt = tpgt_me.zero ) )
                true
        )

        try
            po2.Invoke(
                "CreateOrAddNewConnection",
                false,                  // isLeadingCon
                true,                   // isConnRebuild
                tsih_me.zero,           // dropSessionTSIH
                tsih_me.fromPrim 4us,   // newTSIH
                i_tNext,                // i_tNextIdent
                cid_me.fromPrim 12us,   // recvCID
                cmdsn_me.fromPrim 111u, // newCmdSN
                statsn_me.fromPrim 222u,// firstExpStatSN
                argCOParam,             // iSCSIParamsCO : IscsiNegoParamCO
                argSWParam              // iSCSIParamsSW
            ) |> ignore
        with
        | _ as x ->
            Assert.Fail ( __LINE__ + " : " + x.Message )
        k1.NoticeTerminate()
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.CreateOrAddNewConnection_009() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        let sess1 = new CSession_Stub()
        let i_tNext = new ITNexus( "initiator001", isid_me.zero, "target001", tpgt_me.zero )
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argSWParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = {
                    IdentNumber = tnodeidx_me.fromPrim 1u;
                    TargetName = "target001"
                    TargetAlias = "";
                    TargetPortalGroupTag = tpgt_me.zero;
                    LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
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

        stat1.p_GetSession <- (
            fun a1 ->
                Assert.True( tsih_me.fromPrim 4us = a1 )
                ValueSome( sess1 :> ISession )
        )

        let k1 = new HKiller() :> IKiller
        let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
        let po2 = new PrivateCaller( pcon )

        sess1.p_ReinstateConnection <- (
            fun _ _ _ _ _ _ ->
                Assert.Fail __LINE__
                true
        )

        try
            k1.NoticeTerminate()
            po2.Invoke(
                "CreateOrAddNewConnection",
                false,                  // isLeadingCon
                true,                   // isConnRebuild
                tsih_me.zero,           // dropSessionTSIH
                tsih_me.fromPrim 4us,   // newTSIH
                i_tNext,                // i_tNextIdent
                cid_me.fromPrim 12us,   // recvCID
                cmdsn_me.fromPrim 111u, // newCmdSN
                statsn_me.fromPrim 222u,// firstExpStatSN
                argCOParam,             // iSCSIParamsCO : IscsiNegoParamCO
                argSWParam              // iSCSIParamsSW
            ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( x.Message = "Termination requested." ) )
        | _ as x ->
            Assert.Fail ( __LINE__ + " : " + x.Message )
        k1.NoticeTerminate()
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.OperationalNegotiation_001() =
        let sp, cp = GlbFunc.GetNetConn()
        let stat1 =  new CStatus_Stub()
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argTargetConfig : TargetGroupConf.T_Target =
            {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetName = "target001";
                TargetAlias = "";
                TargetPortalGroupTag = tpgt_me.zero;
                LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                Auth = TargetGroupConf.T_Auth.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "";
                        Password = "";
                    };
                    TargetAuth = {
                        UserName = "";
                        Password = "";
                    };
                } )
            }
        let argSWParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = argTargetConfig;
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

        let initiator = 
            fun () -> task {
                let! recvPDU1 = 
                    PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih_me.zero ), ValueSome( cid_me.zero ), ValueSome( concnt_me.zero ), cp, Standpoint.Initiator )
                Assert.True( ( recvPDU1.Opcode = OpcodeCd.LOGIN_RES ) );
                let recvPDU1L = recvPDU1 :?> LoginResponsePDU
                Assert.True( recvPDU1L.Status = LoginResStatCd.AUTH_FAILURE );
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                let po2 = new PrivateCaller( pcon )
                try
                    let a =
                        po2.Invoke(
                            "OperationalNegotiation",
                            true,            // isLeadingCon
                            true,            // isConnRebuild
                            argTargetConfig, // targetConfig
                            {
                                g_defaultLoginRequest with
                                    CSG = LoginReqStateCd.OPERATIONAL;
                                    TextRequest =
                                        IscsiTextEncode.CreateTextKeyValueString
                                            {
                                                TextKeyValues.defaultTextKeyValues with
                                                    InitiatorName = TextValueType.Value( "initiator001" );
                                                    TargetName = TextValueType.Value( "target001" );
                                                    SessionType = TextValueType.Value( "Normal" );
                                            }
                                            {
                                                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                    NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                            };
                            },           // firstPDU
                            argCOParam,  // coParam
                            argSWParam,  // swParam
                            false,       // isAuthentified
                            tsih_me.zero // newTSIH
                        ) :?> Task<struct ( LoginResponsePDU * IscsiNegoParamCO * IscsiNegoParamSW )>

                    let! _, _, _ = a

                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message = "Authentication required." )
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }
       
        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.OperationalNegotiation_002() =
        let stat1 =  new CStatus_Stub()
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argTargetConfig : TargetGroupConf.T_Target =
            {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetName = "target002";
                TargetAlias = "TARGETALIAS002";
                TargetPortalGroupTag = tpgt_me.fromPrim 99us;
                LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
        let argSWParam =
            {
                MaxConnections = 88us;
                TargetGroupID = tgid_me.Zero;
                TargetConf = argTargetConfig;
                InitiatorName = "initiator002";
                InitiatorAlias = "INITIATOR002";
                TargetPortalGroupTag = tpgt_me.fromPrim 99us;
                InitialR2T = true;
                ImmediateData = false;
                MaxBurstLength = 65536u;
                FirstBurstLength = 32768u;
                DefaultTime2Wait = 2us;
                DefaultTime2Retain = 20us;
                MaxOutstandingR2T = 3us;
                DataPDUInOrder = true;
                DataSequenceInOrder = false;
                ErrorRecoveryLevel = 1uy;
            }
        
        let errorTextRequest =
            [|
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            CHAP_A = TextValueType.Value( [| 0us; 5us |] );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            CHAP_I = TextValueType.Value( 10us );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_CHAP_I = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            CHAP_C = TextValueType.Value( [| 0uy .. 255uy |] );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_CHAP_C = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            CHAP_N = TextValueType.Value( "aaaa" );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            CHAP_R = TextValueType.Value( [| 0uy .. 255uy |] );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            SendTargets = TextValueType.Value( "bbbbbbbb" );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_SendTargets = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            TargetAddress = TextValueType.Value( "cccccc" );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_TargetAddress = NegoStatusValue.NSG_WaitSend;
                    };
            |]

        for i = 0 to errorTextRequest.Length - 1 do
            let sp, cp = GlbFunc.GetNetConn()

            let initiator = 
                fun () -> task {
                    let! recvPDU1 = 
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih_me.zero ), ValueSome( cid_me.zero ), ValueSome( ccnt1 ), cp, Standpoint.Initiator )
                    Assert.True( ( recvPDU1.Opcode = OpcodeCd.LOGIN_RES ) );
                    let recvPDU1L = recvPDU1 :?> LoginResponsePDU
                    Assert.True( recvPDU1L.Status = LoginResStatCd.SUCCESS );
                    let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| recvPDU1L.TextResponse; |]
                    Assert.True( kv1.IsSome )
                    Assert.True( kv1.Value.HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None |] ) );
                    Assert.True( kv1.Value.DataDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None |] ) );
                    Assert.True( kv1.Value.MaxConnections = TextValueType.Value( 88us ) );
                    Assert.True( kv1.Value.TargetAlias = TextValueType.Value( "TARGETALIAS002" ) );
                    Assert.True( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 99us ) );
                    Assert.True( kv1.Value.InitialR2T = TextValueType.Value( true ) );
                    Assert.True( kv1.Value.ImmediateData = TextValueType.Value( false ) );
                    Assert.True( kv1.Value.MaxBurstLength = TextValueType.Value( 65536u ) );
                    Assert.True( kv1.Value.FirstBurstLength = TextValueType.Value( 32768u ) );
                    Assert.True( kv1.Value.DefaultTime2Wait = TextValueType.Value( 2us ) );
                    Assert.True( kv1.Value.DefaultTime2Retain = TextValueType.Value( 20us ) );
                    Assert.True( kv1.Value.MaxOutstandingR2T = TextValueType.Value( 3us ) );
                    Assert.True( kv1.Value.DataPDUInOrder = TextValueType.Value( true ) );
                    Assert.True( kv1.Value.DataSequenceInOrder = TextValueType.Value( false ) );
                    Assert.True( kv1.Value.ErrorRecoveryLevel = TextValueType.Value( 1uy ) );

                    do! PDU.SendPDU(
                            8192u,
                            DigestType.DST_None,
                            DigestType.DST_None,
                            ValueSome( tsih1 ),
                            ValueSome( cid1 ),
                            ValueSome( ccnt1 ),
                            objidx_me.NewID(),
                            cp,
                            {
                                g_defaultLoginRequest with
                                    TextRequest = errorTextRequest.[i]
                            }
                        )
                        |> Functions.TaskIgnore
                }

            let k1 = new HKiller() :> IKiller
            let target = 
                fun () -> task {
                    let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                    //let con = pcon :> ILoginNegociator
                    let po2 = new PrivateCaller( pcon )
                    try
                        let! _, _, _ =
                            po2.Invoke(
                                "OperationalNegotiation",
                                true,            // isLeadingCon
                                true,            // isConnRebuild
                                argTargetConfig, // targetConfig
                                {
                                    g_defaultLoginRequest with
                                        CSG = LoginReqStateCd.OPERATIONAL;
                                        TextRequest =
                                            IscsiTextEncode.CreateTextKeyValueString
                                                {
                                                    TextKeyValues.defaultTextKeyValues with
                                                        InitiatorName = TextValueType.Value( "initiator002" );
                                                        TargetName = TextValueType.Value( "target002" );
                                                        SessionType = TextValueType.Value( "Normal" );
                                                }
                                                {
                                                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                        NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                        NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                        NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                };
                                },           // firstPDU
                                argCOParam,  // coParam
                                argSWParam,  // swParam
                                false,       // isAuthentified
                                tsih_me.zero // newTSIH
                            ) :?> Task<struct ( LoginResponsePDU * IscsiNegoParamCO * IscsiNegoParamSW )>
                        Assert.Fail __LINE__
                    with
                    | :? SessionRecoveryException as x ->
                        Assert.True( x.Message = "Invalid text key was received." )
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                    k1.NoticeTerminate()
                }
       
            Functions.RunTaskInPallalel [| initiator; target; |]
            |> Functions.RunTaskSynchronously
            |> ignore
            GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.OperationalNegotiation_003() =
        let stat1 =  new CStatus_Stub()
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argTargetConfig : TargetGroupConf.T_Target =
            {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetName = "target002";
                TargetAlias = "TARGETALIAS002";
                TargetPortalGroupTag = tpgt_me.fromPrim 99us;
                LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
        let argSWParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = argTargetConfig;
                InitiatorName = "initiator002";
                InitiatorAlias = "INITIATOR002";
                TargetPortalGroupTag = tpgt_me.fromPrim 99us;
                InitialR2T = true;
                ImmediateData = false;
                MaxBurstLength = 65536u;
                FirstBurstLength = 32768u;
                DefaultTime2Wait = 2us;
                DefaultTime2Retain = 20us;
                MaxOutstandingR2T = 3us;
                DataPDUInOrder = true;
                DataSequenceInOrder = false;
                ErrorRecoveryLevel = 1uy;
            }
        let errorTextRequest =
            [|
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            MaxConnections = TextValueType.Value( Constants.NEGOPARAM_MaxConnections );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_MaxConnections = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            InitialR2T = TextValueType.Value( true );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            ImmediateData = TextValueType.Value( true );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            MaxBurstLength = TextValueType.Value( 8192u );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_MaxBurstLength = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            FirstBurstLength = TextValueType.Value( 8192u );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_FirstBurstLength = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            DefaultTime2Wait = TextValueType.Value( 2us );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_DefaultTime2Wait = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            DefaultTime2Retain = TextValueType.Value( 2us );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_DefaultTime2Retain = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            MaxOutstandingR2T = TextValueType.Value( 2us );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_MaxOutstandingR2T = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            DataPDUInOrder = TextValueType.Value( true );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_DataPDUInOrder = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            DataSequenceInOrder = TextValueType.Value( true );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_DataSequenceInOrder = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            ErrorRecoveryLevel = TextValueType.Value( 1uy );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_ErrorRecoveryLevel = NegoStatusValue.NSG_WaitSend;
                    };
            |]

        for i = 0 to errorTextRequest.Length - 1 do
            let sp, cp = GlbFunc.GetNetConn()

            let initiator = 
                fun () -> task {
                    let! recvPDU1 = 
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih_me.zero ), ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                    Assert.True( ( recvPDU1.Opcode = OpcodeCd.LOGIN_RES ) );
                    let recvPDU1L = recvPDU1 :?> LoginResponsePDU
                    Assert.True( recvPDU1L.Status = LoginResStatCd.SUCCESS );
                    let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| recvPDU1L.TextResponse; |]
                    Assert.True( kv1.IsSome )
                    Assert.True( kv1.Value.HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None |] ) );
                    Assert.True( kv1.Value.DataDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None |] ) );
                    Assert.True( kv1.Value.MaxConnections = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.TargetAlias = TextValueType.Value( "TARGETALIAS002" ) );
                    Assert.True( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 99us ) );
                    Assert.True( kv1.Value.InitialR2T = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.ImmediateData = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.MaxBurstLength = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.FirstBurstLength = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.DefaultTime2Wait = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.DefaultTime2Retain = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.MaxOutstandingR2T = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.DataPDUInOrder = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.DataSequenceInOrder = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.ErrorRecoveryLevel = TextValueType.ISV_Missing );

                    do! PDU.SendPDU(
                            8192u,
                            DigestType.DST_None,
                            DigestType.DST_None,
                            ValueSome( tsih1 ),
                            ValueSome( cid1 ),
                            ValueSome( ccnt1 ),
                            objidx_me.NewID(),
                            cp,
                            {
                                g_defaultLoginRequest with
                                    TextRequest = errorTextRequest.[i]
                            }
                        )
                        |> Functions.TaskIgnore
                }

            let k1 = new HKiller() :> IKiller
            let target = 
                fun () -> task {
                    let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                    //let con = pcon :> ILoginNegociator
                    let po2 = new PrivateCaller( pcon )
                    try
                        let! _, _, _ =
                            po2.Invoke(
                                "OperationalNegotiation",
                                false,           // isLeadingCon
                                true,            // isConnRebuild
                                argTargetConfig, // targetConfig
                                {
                                    g_defaultLoginRequest with
                                        CSG = LoginReqStateCd.OPERATIONAL;
                                        TextRequest =
                                            IscsiTextEncode.CreateTextKeyValueString
                                                {
                                                    TextKeyValues.defaultTextKeyValues with
                                                        InitiatorName = TextValueType.Value( "initiator002" );
                                                        TargetName = TextValueType.Value( "target002" );
                                                        SessionType = TextValueType.Value( "Normal" );
                                                }
                                                {
                                                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                        NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                        NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                        NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                                };
                                },           // firstPDU
                                argCOParam,  // coParam
                                argSWParam,  // swParam
                                false,       // isAuthentified
                                tsih_me.zero // newTSIH
                            ) :?> Task<struct ( LoginResponsePDU * IscsiNegoParamCO * IscsiNegoParamSW )>
                        Assert.Fail __LINE__
                    with
                    | :? SessionRecoveryException as x ->
                        Assert.True( x.Message = "Invalid text key was received." )
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                    k1.NoticeTerminate()
                }
       
            Functions.RunTaskInPallalel [| initiator; target; |]
            |> Functions.RunTaskSynchronously
            |> ignore
            GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.OperationalNegotiation_004() =
        let stat1 =  new CStatus_Stub()
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argTargetConfig : TargetGroupConf.T_Target =
            {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetName = "target002";
                TargetAlias = "TARGETALIAS002";
                TargetPortalGroupTag = tpgt_me.fromPrim 99us;
                LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                Auth = TargetGroupConf.T_Auth.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "";
                        Password = "";
                    };
                    TargetAuth = {
                        UserName = "";
                        Password = "";
                    };
                } )
            }
        let argSWParam =
            {
                MaxConnections = Constants.NEGOPARAM_MaxConnections;
                TargetGroupID = tgid_me.Zero;
                TargetConf = argTargetConfig;
                InitiatorName = "initiator002";
                InitiatorAlias = "INITIATOR002";
                TargetPortalGroupTag = tpgt_me.fromPrim 99us;
                InitialR2T = true;
                ImmediateData = false;
                MaxBurstLength = 65536u;
                FirstBurstLength = 32768u;
                DefaultTime2Wait = 2us;
                DefaultTime2Retain = 20us;
                MaxOutstandingR2T = 3us;
                DataPDUInOrder = true;
                DataSequenceInOrder = false;
                ErrorRecoveryLevel = 1uy;
            }
        
        let errorTextRequest =
            [|
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            SessionType = TextValueType.Value( "Normal" );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            InitiatorName = TextValueType.Value( "initiator002" );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            TargetName = TextValueType.Value( "target002" );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                    };
                IscsiTextEncode.CreateTextKeyValueString
                    {
                        TextKeyValues.defaultTextKeyValues with
                            TargetPortalGroupTag = TextValueType.Value( 0us );
                    }
                    {
                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                            NegoStat_TargetPortalGroupTag = NegoStatusValue.NSG_WaitSend;
                    };
            |]

        for i = 0 to errorTextRequest.Length - 1 do
            let sp, cp = GlbFunc.GetNetConn()
            let initiator = 
                fun () -> task {
                    let! recvPDU1 = 
                        PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih_me.zero ), ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                    Assert.True( ( recvPDU1.Opcode = OpcodeCd.LOGIN_RES ) );
                    let recvPDU1L = recvPDU1 :?> LoginResponsePDU
                    Assert.True( recvPDU1L.Status = LoginResStatCd.SUCCESS );
                    let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| recvPDU1L.TextResponse; |]
                    Assert.True( kv1.IsSome )
                    Assert.True( kv1.Value.HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; |] ) );
                    Assert.True( kv1.Value.DataDigest = TextValueType.Value( [| DigestType.DST_CRC32C; |] ) );
                    Assert.True( kv1.Value.MaxConnections = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.TargetAlias = TextValueType.Value( "TARGETALIAS002" ) );
                    Assert.True( kv1.Value.TargetPortalGroupTag = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.InitialR2T = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.ImmediateData = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.MaxBurstLength = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.FirstBurstLength = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.DefaultTime2Wait = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.DefaultTime2Retain = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.MaxOutstandingR2T = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.DataPDUInOrder = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.DataSequenceInOrder = TextValueType.ISV_Missing );
                    Assert.True( kv1.Value.ErrorRecoveryLevel = TextValueType.ISV_Missing );

                    do! PDU.SendPDU(
                            8192u,
                            DigestType.DST_None,
                            DigestType.DST_None,
                            ValueSome( tsih1 ),
                            ValueSome( cid1 ),
                            ValueSome( ccnt1 ),
                            objidx_me.NewID(),
                            cp,
                            {
                                g_defaultLoginRequest with
                                    TextRequest = errorTextRequest.[i]
                            }
                        )
                        |> Functions.TaskIgnore
                }

            let k1 = new HKiller() :> IKiller
            let target = 
                fun () -> task {
                    let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                    //let con = pcon :> ILoginNegociator
                    let po2 = new PrivateCaller( pcon )
                    try
                        let! _, _, _ =
                            po2.Invoke(
                                "OperationalNegotiation",
                                false,           // isLeadingCon
                                true,            // isConnRebuild
                                argTargetConfig, // targetConfig
                                {
                                    g_defaultLoginRequest with
                                        CSG = LoginReqStateCd.OPERATIONAL;
                                        TextRequest =
                                            IscsiTextEncode.CreateTextKeyValueString
                                                {
                                                    TextKeyValues.defaultTextKeyValues with
                                                        HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None |] );
                                                        DataDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None |] );
                                                        InitiatorAlias = TextValueType.Value( "INITIATOR002" );
                                                }
                                                {
                                                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                        NegoStat_HeaderDigest = NegoStatusValue.NSG_WaitSend;
                                                        NegoStat_DataDigest = NegoStatusValue.NSG_WaitSend;
                                                        NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitSend;
                                                };
                                },           // firstPDU
                                argCOParam,  // coParam
                                argSWParam,  // swParam
                                true,        // isAuthentified
                                tsih_me.zero // newTSIH
                            ) :?> Task<struct ( LoginResponsePDU * IscsiNegoParamCO * IscsiNegoParamSW )>
                        Assert.Fail __LINE__
                    with
                    | :? SessionRecoveryException as x ->
                        Assert.True( x.Message = "Invalid text key was received." )
                    | _ as x ->
                        Assert.Fail ( __LINE__ + " : " + x.Message )
                    k1.NoticeTerminate()
                }
       
            Functions.RunTaskInPallalel [| initiator; target; |]
            |> Functions.RunTaskSynchronously
            |> ignore
            GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.OperationalNegotiation_005() =
        let stat1 =  new CStatus_Stub()
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argTargetConfig : TargetGroupConf.T_Target =
            {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetName = "target002";
                TargetAlias = "TARGETALIAS002";
                TargetPortalGroupTag = tpgt_me.fromPrim 99us;
                LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                Auth = TargetGroupConf.T_Auth.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "";
                        Password = "";
                    };
                    TargetAuth = {
                        UserName = "";
                        Password = "";
                    };
                } )
            }
        let argSWParam =
            {
                MaxConnections = 88us;
                TargetGroupID = tgid_me.Zero;
                TargetConf = argTargetConfig;
                InitiatorName = "initiator002";
                InitiatorAlias = "INITIATOR002";
                TargetPortalGroupTag = tpgt_me.fromPrim 99us;
                InitialR2T = true;
                ImmediateData = false;
                MaxBurstLength = 65536u;
                FirstBurstLength = 32768u;
                DefaultTime2Wait = 2us;
                DefaultTime2Retain = 20us;
                MaxOutstandingR2T = 3us;
                DataPDUInOrder = true;
                DataSequenceInOrder = false;
                ErrorRecoveryLevel = 1uy;
            }
        let sp, cp = GlbFunc.GetNetConn()

        let initiator = 
            fun () -> task {
                let! recvPDU1 = 
                    PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU1.Opcode = OpcodeCd.LOGIN_RES ) );
                let recvPDU1L = recvPDU1 :?> LoginResponsePDU
                Assert.True( recvPDU1L.Status = LoginResStatCd.SUCCESS );
                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| recvPDU1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( kv1.Value.HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; |] ) );
                Assert.True( kv1.Value.DataDigest = TextValueType.Value( [| DigestType.DST_CRC32C; |] ) );
                Assert.True( kv1.Value.MaxConnections = TextValueType.Value( 88us ) );
                Assert.True( kv1.Value.TargetAlias = TextValueType.Value( "TARGETALIAS002" ) );
                Assert.True( kv1.Value.TargetPortalGroupTag = TextValueType.ISV_Missing );
                Assert.True( kv1.Value.InitialR2T = TextValueType.Value( true ) );
                Assert.True( kv1.Value.ImmediateData = TextValueType.Value( false ) );
                Assert.True( kv1.Value.MaxRecvDataSegmentLength_T = TextValueType.Value( 8192u ) );
                Assert.True( kv1.Value.MaxBurstLength = TextValueType.Value( 23456u ) );
                Assert.True( kv1.Value.FirstBurstLength = TextValueType.Value( 32768u ) );
                Assert.True( kv1.Value.DefaultTime2Wait = TextValueType.Value( 2us ) );
                Assert.True( kv1.Value.DefaultTime2Retain = TextValueType.Value( 6us ) );
                Assert.True( kv1.Value.MaxOutstandingR2T = TextValueType.Value( 3us ) );
                Assert.True( kv1.Value.DataPDUInOrder = TextValueType.Value( true ) );
                Assert.True( kv1.Value.DataSequenceInOrder = TextValueType.Value( true ) );
                Assert.True( kv1.Value.ErrorRecoveryLevel = TextValueType.Value( 0uy ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                CSG = LoginReqStateCd.OPERATIONAL;
                                NSG = LoginReqStateCd.FULL;
                        }
                    )
                    |> Functions.TaskIgnore
(*
                let! recvPDU2 = 
                    PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, false )
                Assert.True( ( recvPDU2.Opcode = OpcodeCd.LOGIN_RES ) );
                let recvPDU2L = recvPDU2 :?> LoginResponsePDU
                Assert.True( recvPDU2L.T = true )
                Assert.True( recvPDU2L.CSG = LoginReqStateCd.OPERATIONAL )
                Assert.True( recvPDU2L.NSG = LoginReqStateCd.FULL )
                Assert.True( recvPDU2L.TSIH = tsih_me.fromPrim 99us )

                // dummy ( full feature phase )
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_CRC32C,
                        DigestType.DST_CRC32C,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp, g_defaultLoginRequest
                    )
                    |> Functions.TaskIgnore
*)
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                //let con = pcon :> ILoginNegociator
                let po2 = new PrivateCaller( pcon )
                try
                    let! nextPDU, resultCO, resultSW =
                        po2.Invoke(
                            "OperationalNegotiation",
                            true,            // isLeadingCon
                            false,           // isConnRebuild
                            argTargetConfig, // targetConfig
                            {
                                g_defaultLoginRequest with
                                    CSG = LoginReqStateCd.OPERATIONAL;
                                    TextRequest =
                                        IscsiTextEncode.CreateTextKeyValueString
                                            {
                                                TextKeyValues.defaultTextKeyValues with
                                                    HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None |] );
                                                    DataDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None |] );
                                                    MaxConnections = TextValueType.Value( 128us );
                                                    InitiatorAlias = TextValueType.Value( "INITIATOR002" );
                                                    InitialR2T = TextValueType.Value( true );
                                                    ImmediateData = TextValueType.Value( true );
                                                    MaxRecvDataSegmentLength_I = TextValueType.Value( 12345u );
                                                    MaxBurstLength = TextValueType.Value( 23456u );
                                                    FirstBurstLength = TextValueType.Value( 34567u );
                                                    DefaultTime2Wait = TextValueType.Value( 5us );
                                                    DefaultTime2Retain = TextValueType.Value( 6us );
                                                    MaxOutstandingR2T = TextValueType.Value( 7us );
                                                    DataPDUInOrder = TextValueType.Value( true );
                                                    DataSequenceInOrder = TextValueType.Value( true );
                                                    ErrorRecoveryLevel = TextValueType.Value( 0uy );
                                            }
                                            {
                                                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                    NegoStat_HeaderDigest = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_DataDigest = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_MaxConnections = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_MaxBurstLength = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_FirstBurstLength = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_DefaultTime2Wait = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_DefaultTime2Retain = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_MaxOutstandingR2T = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_DataPDUInOrder = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_DataSequenceInOrder = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_ErrorRecoveryLevel = NegoStatusValue.NSG_WaitSend;
                                            };
                            },           // firstPDU
                            argCOParam,  // coParam
                            argSWParam,  // swParam
                            true,        // isAuthentified
                            ( tsih_me.fromPrim 99us )   // newTSIH
                        ) :?> Task<struct ( LoginResponsePDU * IscsiNegoParamCO * IscsiNegoParamSW )>

                    Assert.True( ( nextPDU :ILogicalPDU ).Opcode = OpcodeCd.LOGIN_RES );
                    Assert.True( nextPDU.T = true )
                    Assert.True( nextPDU.CSG = LoginReqStateCd.OPERATIONAL )
                    Assert.True( nextPDU.NSG = LoginReqStateCd.FULL )
                    Assert.True( nextPDU.TSIH = tsih_me.fromPrim 99us )

                    Assert.True( resultCO.AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None; |] ); // not negotiated
                    Assert.True( resultCO.HeaderDigest = [| DigestType.DST_CRC32C |] );
                    Assert.True( resultCO.DataDigest = [| DigestType.DST_CRC32C |] );
                    Assert.True( resultCO.MaxRecvDataSegmentLength_I = 12345u );
                    Assert.True( resultCO.MaxRecvDataSegmentLength_T = 8192u );
                    Assert.True( resultSW.MaxConnections = 88us );
                    Assert.True( resultSW.TargetConf = argTargetConfig );
                    Assert.True( resultSW.InitiatorName = "initiator002" );
                    Assert.True( resultSW.InitiatorAlias = "INITIATOR002" );
                    Assert.True( resultSW.TargetPortalGroupTag = tpgt_me.fromPrim 99us );
                    Assert.True( resultSW.InitialR2T = true );
                    Assert.True( resultSW.ImmediateData = false );
                    Assert.True( resultSW.MaxBurstLength = 23456u );
                    Assert.True( resultSW.FirstBurstLength = 32768u );
                    Assert.True( resultSW.DefaultTime2Wait = 2us );
                    Assert.True( resultSW.DefaultTime2Retain = 6us );
                    Assert.True( resultSW.MaxOutstandingR2T = 3us );
                    Assert.True( resultSW.DataPDUInOrder = true );
                    Assert.True( resultSW.DataSequenceInOrder = true );
                    Assert.True( resultSW.ErrorRecoveryLevel = 0uy );
                with
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }
       
        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.OperationalNegotiation_006() =
        let stat1 =  new CStatus_Stub()
        let argCOParam =
            {
                AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |];
                HeaderDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                DataDigest = [| DigestType.DST_CRC32C; DigestType.DST_None; |];
                MaxRecvDataSegmentLength_I = 8192u;
                MaxRecvDataSegmentLength_T = 8192u;
            }
        let argTargetConfig : TargetGroupConf.T_Target =
            {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetName = "target002";
                TargetAlias = "TARGETALIAS002";
                TargetPortalGroupTag = tpgt_me.fromPrim 99us;
                LUN = [ lun_me.fromPrim 0x0000000000000001UL; ];
                Auth = TargetGroupConf.T_Auth.U_None();
            }

        let argSWParam =
            {
                MaxConnections = 88us;
                TargetGroupID = tgid_me.Zero;
                TargetConf = argTargetConfig;
                InitiatorName = "initiator002";
                InitiatorAlias = "INITIATOR002";
                TargetPortalGroupTag = tpgt_me.fromPrim 99us;
                InitialR2T = false;
                ImmediateData = false;
                MaxBurstLength = 65536u;
                FirstBurstLength = 32768u;
                DefaultTime2Wait = 2us;
                DefaultTime2Retain = 20us;
                MaxOutstandingR2T = 3us;
                DataPDUInOrder = true;
                DataSequenceInOrder = false;
                ErrorRecoveryLevel = 1uy;
            }

        let sp, cp = GlbFunc.GetNetConn()

        let initiator = 
            fun () -> task {
                let! recvPDU1 = 
                    PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU1.Opcode = OpcodeCd.LOGIN_RES ) );
                let recvPDU1L = recvPDU1 :?> LoginResponsePDU
                Assert.True( recvPDU1L.Status = LoginResStatCd.SUCCESS );
                let kv1 = IscsiTextEncode.RecognizeTextKeyData true [| recvPDU1L.TextResponse; |]
                Assert.True( kv1.IsSome )
                Assert.True( kv1.Value.HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None; |] ) );
                Assert.True( kv1.Value.DataDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_None; |] ) );
                Assert.True( kv1.Value.MaxConnections = TextValueType.Value( 88us ) );
                Assert.True( kv1.Value.TargetAlias = TextValueType.Value( "TARGETALIAS002" ) );
                Assert.True( kv1.Value.TargetPortalGroupTag = TextValueType.Value( 99us ) );
                Assert.True( kv1.Value.InitialR2T = TextValueType.Value( false ) );
                Assert.True( kv1.Value.ImmediateData = TextValueType.Value( false ) );
                Assert.True( kv1.Value.MaxRecvDataSegmentLength_T = TextValueType.Value( 8192u ) );
                Assert.True( kv1.Value.MaxBurstLength = TextValueType.Value( 65536u ) );
                Assert.True( kv1.Value.FirstBurstLength = TextValueType.Value( 32768u ) );
                Assert.True( kv1.Value.DefaultTime2Wait = TextValueType.Value( 2us ) );
                Assert.True( kv1.Value.DefaultTime2Retain = TextValueType.Value( 20us ) );
                Assert.True( kv1.Value.MaxOutstandingR2T = TextValueType.Value( 3us ) );
                Assert.True( kv1.Value.DataPDUInOrder = TextValueType.Value( true ) );
                Assert.True( kv1.Value.DataSequenceInOrder = TextValueType.Value( false ) );
                Assert.True( kv1.Value.ErrorRecoveryLevel = TextValueType.Value( 1uy ) );

                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_None,
                        DigestType.DST_None,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                CSG = LoginReqStateCd.OPERATIONAL;
                                NSG = LoginReqStateCd.FULL;
                                TextRequest =
                                    IscsiTextEncode.CreateTextKeyValueString
                                        {
                                            TextKeyValues.defaultTextKeyValues with
                                                HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; |] );
                                                DataDigest = TextValueType.Value( [| DigestType.DST_CRC32C; |] );
                                                MaxConnections = TextValueType.Value( 44us );
                                                InitiatorAlias = TextValueType.Value( "INITIATOR002" );
                                                InitialR2T = TextValueType.Value( false );
                                                ImmediateData = TextValueType.Value( false );
                                                MaxRecvDataSegmentLength_I = TextValueType.Value( 12345u );
                                                MaxBurstLength = TextValueType.Value( 23456u );
                                                FirstBurstLength = TextValueType.Value( 32768u );
                                                DefaultTime2Wait = TextValueType.Value( 2us );
                                                DefaultTime2Retain = TextValueType.Value( 6us );
                                                MaxOutstandingR2T = TextValueType.Value( 1us );
                                                DataPDUInOrder = TextValueType.Value( true );
                                                DataSequenceInOrder = TextValueType.Value( true );
                                                ErrorRecoveryLevel = TextValueType.Value( 0uy );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_HeaderDigest = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_DataDigest = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_MaxConnections = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_MaxBurstLength = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_FirstBurstLength = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_DefaultTime2Wait = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_DefaultTime2Retain = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_MaxOutstandingR2T = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_DataPDUInOrder = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_DataSequenceInOrder = NegoStatusValue.NSG_WaitSend;
                                                NegoStat_ErrorRecoveryLevel = NegoStatusValue.NSG_WaitSend;
                                        };
                        }
                    )
                    |> Functions.TaskIgnore
(*
                let! recvPDU2 = 
                    PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, false )
                Assert.True( ( recvPDU2.Opcode = OpcodeCd.LOGIN_RES ) );
                let recvPDU2L = recvPDU2 :?> LoginResponsePDU
                Assert.True( recvPDU2L.T = true )
                Assert.True( recvPDU2L.CSG = LoginReqStateCd.OPERATIONAL )
                Assert.True( recvPDU2L.NSG = LoginReqStateCd.FULL )
                Assert.True( recvPDU2L.TSIH = tsih_me.fromPrim 199us )

                // dummy ( full feature phase )
                do! PDU.SendPDU(
                        8192u,
                        DigestType.DST_CRC32C,
                        DigestType.DST_CRC32C,
                        ValueSome( tsih1 ),
                        ValueSome( cid1 ),
                        ValueSome( ccnt1 ),
                        objidx_me.NewID(),
                        cp,
                        g_defaultLoginRequest
                    )
                    |> Functions.TaskIgnore
*)
            }

        let k1 = new HKiller() :> IKiller
        let target = 
            fun () -> task {
                let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
                //let con = pcon :> ILoginNegociator
                let po2 = new PrivateCaller( pcon )
                try
                    let! nextPDU, resultCO, resultSW =
                        po2.Invoke(
                            "OperationalNegotiation",
                            true,            // isLeadingCon
                            false,           // isConnRebuild
                            argTargetConfig, // targetConfig
                            {
                                g_defaultLoginRequest with
                                    CSG = LoginReqStateCd.OPERATIONAL;
                                    TextRequest =
                                        IscsiTextEncode.CreateTextKeyValueString
                                            {
                                                TextKeyValues.defaultTextKeyValues with
                                                    InitiatorName = TextValueType.Value( "initiator002" );
                                                    TargetName = TextValueType.Value( "target002" );
                                                    SessionType = TextValueType.Value( "Normal" );
                                            }
                                            {
                                                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                    NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_TargetName = NegoStatusValue.NSG_WaitSend;
                                                    NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                                            };

                            },           // firstPDU
                            argCOParam,  // coParam
                            argSWParam,  // swParam
                            false,       // isAuthentified
                            ( tsih_me.fromPrim 199us )   // newTSIH
                        ) :?> Task<struct ( LoginResponsePDU * IscsiNegoParamCO * IscsiNegoParamSW )>

                    Assert.True( ( nextPDU :> ILogicalPDU ).Opcode = OpcodeCd.LOGIN_RES );
                    Assert.True( nextPDU.T = true )
                    Assert.True( nextPDU.CSG = LoginReqStateCd.OPERATIONAL )
                    Assert.True( nextPDU.NSG = LoginReqStateCd.FULL )
                    Assert.True( nextPDU.TSIH = tsih_me.fromPrim 199us )

                    Assert.True( resultCO.AuthMethod = [| AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None |] ); // not negotiated
                    Assert.True( resultCO.HeaderDigest = [| DigestType.DST_CRC32C |] );
                    Assert.True( resultCO.DataDigest = [| DigestType.DST_CRC32C |] );
                    Assert.True( resultCO.MaxRecvDataSegmentLength_I = 12345u );
                    Assert.True( resultCO.MaxRecvDataSegmentLength_T = 8192u );
                    Assert.True( resultSW.MaxConnections = 44us );
                    Assert.True( resultSW.TargetConf = argTargetConfig );
                    Assert.True( resultSW.InitiatorName = "initiator002" );
                    Assert.True( resultSW.InitiatorAlias = "INITIATOR002" );
                    Assert.True( resultSW.TargetPortalGroupTag = tpgt_me.fromPrim 99us );
                    Assert.True( resultSW.InitialR2T = false );
                    Assert.True( resultSW.ImmediateData = false );
                    Assert.True( resultSW.MaxBurstLength = 23456u );
                    Assert.True( resultSW.FirstBurstLength = 32768u );
                    Assert.True( resultSW.DefaultTime2Wait = 2us );
                    Assert.True( resultSW.DefaultTime2Retain = 6us );
                    Assert.True( resultSW.MaxOutstandingR2T = 1us );
                    Assert.True( resultSW.DataPDUInOrder = true );
                    Assert.True( resultSW.DataSequenceInOrder = true );
                    Assert.True( resultSW.ErrorRecoveryLevel = 0uy );
                with
                | _ as x ->
                    Assert.Fail ( __LINE__ + " : " + x.Message )
                k1.NoticeTerminate()
            }
       
        Functions.RunTaskInPallalel [| initiator; target; |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

