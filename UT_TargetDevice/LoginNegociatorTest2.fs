namespace Haruka.Test.UT.TargetDevice

open System
open System.Text
open System.Net.Sockets
open System.Threading.Tasks

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes
open Haruka.TargetDevice
open Haruka.Test


type LoginNegociator_Test2 () =

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
            TargetConf = {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetName = "";
                TargetAlias = "";
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }
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

    let createDefaultTargetTask ( mrdsl : uint32 ) ( vTargetNames : string[] ) ( vTargetAddresses : string[] ) ( vPortNumbers : uint16[] ) ( sp : NetworkStream ) =
        task {
            Assert.True(( vTargetAddresses.Length = vPortNumbers.Length ))

            let k1 = new HKiller() :> IKiller
            let stat1 =  new CStatus_Stub()
            let pcon = new LoginNegociator( stat1, sp, DateTime.UtcNow, tpgt_me.zero, netportidx_me.zero, k1 )
            let con = pcon :> ILoginNegociator

            stat1.p_GetActiveTarget <- ( fun () ->
                [
                    for idx = 0 to vTargetNames.Length - 1 do
                        yield {
                            IdentNumber = tnodeidx_me.fromPrim ( uint32 idx );
                            TargetName = vTargetNames.[idx];
                            TargetAlias = vTargetNames.[idx];
                            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                            LUN = [ lun_me.zero; ];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        }
                ]
            )
            stat1.p_GetNetworkPortal <- ( fun () ->
                [
                    for idx = 0 to vTargetAddresses.Length - 1 do
                        yield {
                            IdentNumber = netportidx_me.fromPrim ( uint32 idx );
                            TargetAddress = vTargetAddresses.[idx];
                            PortNumber = vPortNumbers.[idx];
                            DisableNagle = false;
                            ReceiveBufferSize = 0;
                            SendBufferSize = 0;
                            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                            WhiteList = [];
                        }
                ]
            )
            stat1.p_GetISCSINegoParamCO <- ( fun () -> 
                {
                    g_defaultiSCSINegoParamCO with
                        MaxRecvDataSegmentLength_I = mrdsl;
                        MaxRecvDataSegmentLength_T = mrdsl;
                }
            )
            stat1.p_GetISCSINegoParamSW <- ( fun () ->
                {
                    g_defaultiSCSINegoParamSW with
                        ErrorRecoveryLevel = 0uy;
                }
            )

            //try
            Assert.True ( con.Start true )
            //with
            //| _ as _ ->
            //    Assert.Fail __LINE__
            k1.NoticeTerminate()
        }

    let defaultLoginRqTextValue ( mrdsl : uint32 ) =
        IscsiTextEncode.CreateTextKeyValueString
            {
                TextKeyValues.defaultTextKeyValues with
                    MaxRecvDataSegmentLength_I = TextValueType.Value( mrdsl );
                    InitiatorName = TextValueType.Value( "initiator001" );
                    SessionType = TextValueType.Value( "Discovery" );
            }
            {
                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                    NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitSend;
                    NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                    NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
            };

    let initiatorLoginSequense ( mrdsl : uint32 ) ( cp : NetworkStream ) =
        task {
            do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                    {
                        g_defaultLoginRequest with
                            T = true;
                            CSG = LoginReqStateCd.OPERATIONAL;
                            NSG = LoginReqStateCd.FULL;
                            TextRequest = defaultLoginRqTextValue mrdsl;
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
                            CSG = LoginReqStateCd.OPERATIONAL;
                            NSG = LoginReqStateCd.FULL;
                            ExpStatSN = statsn_me.fromPrim 1u;
                            TextRequest = Array.empty;
                    }
                )
                |> Functions.TaskIgnore

            let! recvPDU3 = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
            Assert.True( ( recvPDU3.Opcode = OpcodeCd.LOGIN_RES ) );
            let recvPDU3L = recvPDU3 :?> LoginResponsePDU
            Assert.True( recvPDU3L.T = true )
            Assert.True( recvPDU3L.CSG = LoginReqStateCd.OPERATIONAL )
            Assert.True( recvPDU3L.NSG = LoginReqStateCd.FULL )
            Assert.True( recvPDU3L.ExpCmdSN = cmdsn_me.fromPrim 0u )
            Assert.True( recvPDU3L.MaxCmdSN = cmdsn_me.fromPrim 0u )
            Assert.True( recvPDU3L.StatSN = statsn_me.fromPrim 1u )
        }

    let initiatorLogoutSequense ( mrdsl : uint32 ) ( cp : NetworkStream ) ( pduCnt : int ) =
        task {
            // send logout request
            do! PDU.SendPDU( mrdsl, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                    {
                        I = false;
                        ReasonCode = LogoutReqReasonCd.CLOSE_SESS;
                        InitiatorTaskTag = itt_me.fromPrim 0u;
                        CID = cid_me.fromPrim 0us;
                        CmdSN = cmdsn_me.fromPrim ( uint32 pduCnt + 1u );
                        ExpStatSN = statsn_me.fromPrim ( uint32 pduCnt + 3u );
                        ByteCount = 0u;
                    }
                )
                |> Functions.TaskIgnore

            // receive logout responce
            let! recvPDU5 = PDU.Receive( mrdsl, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
            Assert.True( ( recvPDU5.Opcode = OpcodeCd.LOGOUT_RES ) );
            let recvPDU5L = recvPDU5 :?> LogoutResponsePDU
            Assert.True(( recvPDU5L.Response = LogoutResCd.SUCCESS ))
            Assert.True( recvPDU5L.ExpCmdSN = cmdsn_me.fromPrim ( uint32 pduCnt + 2u ) )
            Assert.True( recvPDU5L.MaxCmdSN = cmdsn_me.fromPrim ( uint32 pduCnt + 2u ) )
            Assert.True( recvPDU5L.StatSN = statsn_me.fromPrim ( uint32 pduCnt + 3u ) )
        }

    let receiveResultLoop struct ( cnt : int, vResult : byte[][], mrdsl : uint32, cp : NetworkStream ) :
            Task<LoopState< struct( int * byte[][] * uint32 * NetworkStream ), int > > =
        task {
            // receive result PDU
            let! recvPDU4 = PDU.Receive( mrdsl, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
            Assert.True( ( recvPDU4.Opcode = OpcodeCd.TEXT_RES ) );
            let recvPDU4L = recvPDU4 :?> TextResponsePDU
            Assert.True(( recvPDU4L.TextResponse = vResult.[cnt] ))
            Assert.True( recvPDU4L.ExpCmdSN = cmdsn_me.fromPrim ( uint32 cnt + 1u ) )
            Assert.True( recvPDU4L.MaxCmdSN = cmdsn_me.fromPrim ( uint32 cnt + 1u ) )
            Assert.True( recvPDU4L.StatSN = statsn_me.fromPrim ( uint32 cnt + 2u ) )

            if recvPDU4L.C then
                // Send empty text request PDU and continue to receive result PDU
                do! PDU.SendPDU( mrdsl, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0ul;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim( uint32 cnt + 1u );
                            ExpStatSN = statsn_me.fromPrim ( uint32 cnt + 3u );
                            TextRequest = Array.empty;
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore

                return Continue( struct( cnt + 1, vResult, mrdsl, cp ) )
            else
                return Terminate( cnt )
        }

    let createSimpleInitiatorTask ( sendTargetsValue : string ) ( vResult : byte[][] ) ( mrdsl : uint32 ) ( cp : NetworkStream ) =
        task {
            do! initiatorLoginSequense mrdsl cp

            do! PDU.SendPDU( mrdsl, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                    {
                        I = false;
                        F = false;
                        C = false;
                        LUN = lun_me.zero;
                        InitiatorTaskTag = itt_me.fromPrim 0ul;
                        TargetTransferTag = ttt_me.fromPrim 0u;
                        CmdSN = cmdsn_me.fromPrim 0u;
                        ExpStatSN = statsn_me.fromPrim 2u;
                        TextRequest = 
                            IscsiTextEncode.CreateTextKeyValueString
                                {
                                    TextKeyValues.defaultTextKeyValues with
                                        SendTargets = TextValueType.Value( sendTargetsValue );
                                }
                                {
                                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                        NegoStat_SendTargets = NegoStatusValue.NSG_WaitSend;
                                };
                        ByteCount = 0u;
                    }
                )
                |> Functions.TaskIgnore

            let! loopCnt = Functions.loopAsyncWithArgs receiveResultLoop struct( 0, vResult, mrdsl, cp )

            do! initiatorLogoutSequense mrdsl cp loopCnt

            cp.Close()
        }


    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    [<Fact>]
    member _.DiscoverySession_001() =
        let sp, cp = GlbFunc.GetNetConn()
        let exResult = [|
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName=target001"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0"
            yield '\u0000'B
        |]
       
        Functions.RunTaskInPallalel [|
            fun () -> createSimpleInitiatorTask "All" [| exResult |] 8192u cp;
            fun () -> createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_002() =
        let sp, cp = GlbFunc.GetNetConn()
        let exResult = [|
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName=target001"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName=target002"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0"
            yield '\u0000'B
        |]
       
        Functions.RunTaskInPallalel [|
            fun () -> createSimpleInitiatorTask "All" [| exResult |] 8192u cp;
            fun () -> createDefaultTargetTask 8192u [| "target001"; "target002" |] [| "127.0.0.1" |] [| 100us |] sp;
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_003() =
        let sp, cp = GlbFunc.GetNetConn()
        let exResult = [|
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName=target001"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.2:101,0"
            yield '\u0000'B
        |]
       
        Functions.RunTaskInPallalel [|
            fun () -> createSimpleInitiatorTask "All" [| exResult |] 8192u cp;
            fun () -> createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1"; "127.0.0.2" |] [| 100us; 101us |] sp;
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]


    [<Fact>]
    member _.DiscoverySession_004() =
        let sp, cp = GlbFunc.GetNetConn()
        let exResult = [|
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName=target001"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.2:101,0"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName=target002"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.2:101,0"
            yield '\u0000'B
        |]
       
        Functions.RunTaskInPallalel [|
            fun () -> createSimpleInitiatorTask "All" [| exResult |] 8192u cp;
            fun () -> createDefaultTargetTask 8192u [| "target001"; "target002" |] [| "127.0.0.1"; "127.0.0.2" |] [| 100us; 101us |] sp;
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_005() =
        let sp, cp = GlbFunc.GetNetConn()
        let exResult = [|
            [|
                yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName="
                yield! Encoding.GetEncoding( "utf-8" ).GetBytes ( String.replicate 501 "a" );
            |]
            [|
                yield! Encoding.GetEncoding( "utf-8" ).GetBytes "aaaaaaaaaaa";
                yield '\u0000'B
                yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0";
                yield '\u0000'B
            |]
        |]

        
        Functions.RunTaskInPallalel [|
            fun () -> createSimpleInitiatorTask "All" exResult 512u cp;
            fun () -> createDefaultTargetTask 512u [| String.replicate 512 "a"; |] [| "127.0.0.1" |] [| 100us |] sp;
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_006() =
        let sp, cp = GlbFunc.GetNetConn()
        let exResult = [|
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName=target001"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0"
            yield '\u0000'B
        |]
       
        Functions.RunTaskInPallalel [|
            fun () -> createSimpleInitiatorTask "target001" [| exResult |] 8192u cp;
            fun () -> createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_007() =
        let sp, cp = GlbFunc.GetNetConn()
        let exResult = [|
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName=target002"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0"
            yield '\u0000'B
        |]
       
        Functions.RunTaskInPallalel [|
            fun () -> createSimpleInitiatorTask "target002" [| exResult |] 8192u cp;
            fun () -> createDefaultTargetTask 8192u [| "target001"; "target002" |] [| "127.0.0.1" |] [| 100us |] sp;
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]



    [<Fact>]
    member _.DiscoverySession_008() =
        let sp, cp = GlbFunc.GetNetConn()
        let exResult = [|
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName=target001"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.2:101,0"
            yield '\u0000'B
        |]
       
        Functions.RunTaskInPallalel [|
            fun () -> createSimpleInitiatorTask "target001" [| exResult |] 8192u cp;
            fun () -> createDefaultTargetTask 8192u [| "target001"; "target002" |] [| "127.0.0.1"; "127.0.0.2" |] [| 100us; 101us |] sp;
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]


    [<Fact>]
    member _.DiscoverySession_009() =
        let sp, cp = GlbFunc.GetNetConn()
        let exResult = [|
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName=target001"
            yield '\u0000'B
            yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0"
            yield '\u0000'B
        |]

        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 8192u cp

                let defTextReqPDU =
                    {
                        I = false;
                        F = false;
                        C = false;
                        LUN = lun_me.zero;
                        InitiatorTaskTag = itt_me.fromPrim 0ul;
                        TargetTransferTag = ttt_me.fromPrim 0u;
                        CmdSN = cmdsn_me.fromPrim 0u;
                        ExpStatSN = statsn_me.fromPrim 2u;
                        TextRequest = 
                            IscsiTextEncode.CreateTextKeyValueString
                                {
                                    TextKeyValues.defaultTextKeyValues with
                                        SendTargets = TextValueType.Value( "All" );
                                }
                                {
                                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                        NegoStat_SendTargets = NegoStatusValue.NSG_WaitSend;
                                };
                        ByteCount = 0u;
                    }

                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp, defTextReqPDU )
                    |> Functions.TaskIgnore
                let! loopCnt1 = Functions.loopAsyncWithArgs receiveResultLoop struct( 0, [| exResult |], 8192u, cp )

                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp, 
                        {
                            defTextReqPDU with
                                CmdSN = cmdsn_me.fromPrim( uint32( loopCnt1 + 1 ) );
                                ExpStatSN = statsn_me.fromPrim( uint32( loopCnt1 + 3 ) );
                        }
                    )
                    |> Functions.TaskIgnore

                let! loopCnt2 = Functions.loopAsyncWithArgs receiveResultLoop struct( loopCnt1 + 1, [| Array.empty; exResult |], 8192u, cp )

                do! initiatorLogoutSequense 8192u cp ( loopCnt1 + loopCnt2 + 2 )

                cp.Close()
            }
       
        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_010() =
        let sp, cp = GlbFunc.GetNetConn()

        let initiator =
            fun () -> task {
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                CSG = LoginReqStateCd.OPERATIONAL;
                                NSG = LoginReqStateCd.FULL;
                                VersionMin = 1uy;
                                VersionMax = 1uy;
                                TextRequest = defaultLoginRqTextValue 8192u;
                        }
                    )
                    |> Functions.TaskIgnore
                let! recvPDU2 = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU2.Opcode = OpcodeCd.LOGIN_RES ) );
                let recvPDU2L = recvPDU2 :?> LoginResponsePDU
                Assert.True( recvPDU2L.Status = LoginResStatCd.UNSUPPORTED_VERSION )
        }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True(( x.Message = "Unsupported version is requested." ))
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_011() =
        let sp, cp = GlbFunc.GetNetConn()

        let initiator =
            fun () -> task {
                let exResult = [|
                    yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName=target001"
                    yield '\u0000'B
                    yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0"
                    yield '\u0000'B
                |]

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
                                                SessionType = TextValueType.Value( "Discovery" );
                                                AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_None |] );
                                        }
                                        {
                                            TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                                NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
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
                                TextRequest = Array.empty;
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

                let! recvPDU5 = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU5.Opcode = OpcodeCd.LOGIN_RES ) );
                let recvPDU5L = recvPDU5 :?> LoginResponsePDU
                Assert.True( recvPDU5L.T = true )
                Assert.True( recvPDU5L.CSG = LoginReqStateCd.OPERATIONAL )
                Assert.True( recvPDU5L.NSG = LoginReqStateCd.FULL )
                Assert.True( recvPDU5L.ExpCmdSN = cmdsn_me.fromPrim 0u )
                Assert.True( recvPDU5L.MaxCmdSN = cmdsn_me.fromPrim 0u )
                Assert.True( recvPDU5L.StatSN = statsn_me.fromPrim 0u )

                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0ul;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            TextRequest = 
                                IscsiTextEncode.CreateTextKeyValueString
                                    {
                                        TextKeyValues.defaultTextKeyValues with
                                            SendTargets = TextValueType.Value( "All" );
                                    }
                                    {
                                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                            NegoStat_SendTargets = NegoStatusValue.NSG_WaitSend;
                                    };
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore

                let! loopCnt = Functions.loopAsyncWithArgs receiveResultLoop struct( 0, [| exResult |], 8192u, cp )

                do! initiatorLogoutSequense 8192u cp loopCnt

                cp.Close()
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_012() =
        let sp, cp = GlbFunc.GetNetConn()

        let initiator =
            fun () -> task {
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            g_defaultLoginRequest with
                                T = true;
                                CSG = LoginReqStateCd.OPERATIONAL;
                                NSG = LoginReqStateCd.FULL;
                                TextRequest = defaultLoginRqTextValue 8192u;
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

                // Unexpected PDU
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            Type = SnackReqTypeCd.DATA_ACK;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 1u;
                            BegRun = 0u;
                            RunLength = 0u;
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "Unexpected PDU" )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_013() =
        let sp, cp = GlbFunc.GetNetConn()

        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 8192u cp

                // Unexpected PDU
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            Type = SnackReqTypeCd.DATA_ACK;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            BegRun = 0u;
                            RunLength = 0u;
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "Invalid PDU type in discovery session." )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_014() =
        let sp, cp = GlbFunc.GetNetConn()

        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 8192u cp
                do! initiatorLogoutSequense 8192u cp -1
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                do! createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_015() =
        let sp, cp = GlbFunc.GetNetConn()

        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 8192u cp

                // send logout request
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            ReasonCode = LogoutReqReasonCd.CLOSE_CONN;   // Invalid reason
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            CID = cid_me.fromPrim 0us;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "Invalid logout reason" )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_016() =
        let sp, cp = GlbFunc.GetNetConn()

        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 8192u cp

                // send invalid text request
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            TextRequest = 
                                IscsiTextEncode.CreateTextKeyValueString
                                    {
                                        TextKeyValues.defaultTextKeyValues with
                                            AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP |] );
                                    }
                                    {
                                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                            NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                                    };
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "Invalid text key was received" )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]


    [<Fact>]
    member _.DiscoverySession_017() =
        let initiator ( sendTargetsVal : string ) ( cp : NetworkStream ) =
            fun () -> task {
                do! initiatorLoginSequense 8192u cp

                // send invalid text request
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            TextRequest = 
                                IscsiTextEncode.CreateTextKeyValueString
                                    {
                                        TextKeyValues.defaultTextKeyValues with
                                            SendTargets = TextValueType.Value( sendTargetsVal );
                                    }
                                    {
                                        TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                            NegoStat_SendTargets = NegoStatusValue.NSG_WaitSend;
                                    };
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        let sendVal = [| "NotUnderstood"; "Irrelevant"; "Reject" |];

        for itr in sendVal do
            let sp, cp = GlbFunc.GetNetConn()
            Functions.RunTaskInPallalel [|
                initiator itr cp;
                fun () -> task {
                    try
                        do! createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
                        Assert.Fail __LINE__
                    with
                    | :? SessionRecoveryException as x ->
                        Assert.True( x.Message.StartsWith "Invalid SendTargets value was received in discovery session" )
                    | _ ->
                        Assert.Fail __LINE__
                }
            |]
            |> Functions.RunTaskSynchronously
            |> ignore
            GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_018() =
        let sp, cp = GlbFunc.GetNetConn()
        let initiator =
            let exResult = [|
                yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetName=target001"
                yield '\u0000'B
                yield! Encoding.GetEncoding( "utf-8" ).GetBytes "TargetAddress=127.0.0.1:100,0"
                yield '\u0000'B
            |]

            fun () -> task {
                do! initiatorLoginSequense 8192u cp

                // text request part 1
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = true;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "SendTargets="
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
                let! loopCnt1 = Functions.loopAsyncWithArgs receiveResultLoop struct( 0, [| Array.empty |], 8192u, cp )
                Assert.True(( loopCnt1 = 0 ))

                // text request part 2
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 1u;
                            ExpStatSN = statsn_me.fromPrim 3u;
                            TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "All"
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore

                let! loopCnt2 = Functions.loopAsyncWithArgs receiveResultLoop struct( 1, [| Array.empty; exResult |], 8192u, cp )
                Assert.True(( loopCnt2 = 1 ))

                do! initiatorLogoutSequense 8192u cp 1
                
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_019() =
        let sp, cp = GlbFunc.GetNetConn()
        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 8192u cp

                // text request part 1
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = true;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "SendTargets="
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
                let! loopCnt1 = Functions.loopAsyncWithArgs receiveResultLoop struct( 0, [| Array.empty |], 8192u, cp )
                Assert.True(( loopCnt1 = 0 ))

                // Unexpected PDU
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            Type = SnackReqTypeCd.DATA_ACK;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 3u;
                            BegRun = 0u;
                            RunLength = 0u;
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "Unexpected PDU was received in discovery session." )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_020() =
        let sp, cp = GlbFunc.GetNetConn()
        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 8192u cp

                // text request part 1
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                            {
                                I = false;
                                F = false;
                                C = true;
                                LUN = lun_me.zero;
                                InitiatorTaskTag = itt_me.fromPrim 0u;
                                TargetTransferTag = ttt_me.fromPrim 0u;
                                CmdSN = cmdsn_me.fromPrim 0u;
                                ExpStatSN = statsn_me.fromPrim 2u;
                                TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "SendTargets="
                                ByteCount = 0u;
                            }
                        )
                        |> Functions.TaskIgnore
                let! loopCnt1 = Functions.loopAsyncWithArgs receiveResultLoop struct( 0, [| Array.empty |], 8192u, cp )
                Assert.True(( loopCnt1 = 0 ))

                // text request part 2
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 1u;
                            ExpStatSN = statsn_me.fromPrim 99u; // Unexpected
                            TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "All"
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "Unexpected ExpStatSN was received in discovery session." )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_021() =
        let sp, cp = GlbFunc.GetNetConn()
        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 8192u cp

                // text request part 1
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = true;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "SendTargets="
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
                let! loopCnt1 = Functions.loopAsyncWithArgs receiveResultLoop struct( 0, [| Array.empty |], 8192u, cp )
                Assert.True(( loopCnt1 = 0 ))

                // text request part 2
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 99u;        // Unexpected
                            ExpStatSN = statsn_me.fromPrim 3u;
                            TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "All"
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "Unexpected CmdSN was received in discovery session." )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_022() =
        let sp, cp = GlbFunc.GetNetConn()
        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 8192u cp

                // text request part 1
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "aaaaaaaaaaaaaaaaaaaaa"
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 8192u [| "target001" |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "In iSCSI text request PDU, Text request data is invalid in discovery session." )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_023() =
        let sp, cp = GlbFunc.GetNetConn()
        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 512u cp

                // text request part 1
                do! PDU.SendPDU( 512u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0ul;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "SendTargets=All"
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore

                // receive result PDU
                let! recvPDU4 = PDU.Receive( 512u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU4.Opcode = OpcodeCd.TEXT_RES ) );
                let recvPDU4L = recvPDU4 :?> TextResponsePDU
                Assert.True( recvPDU4L.ExpCmdSN = cmdsn_me.fromPrim 1u )
                Assert.True( recvPDU4L.MaxCmdSN = cmdsn_me.fromPrim 1u )
                Assert.True( recvPDU4L.StatSN = statsn_me.fromPrim 2u )

                // Unexpected PDU
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            Type = SnackReqTypeCd.DATA_ACK;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0u;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 3u;
                            BegRun = 0u;
                            RunLength = 0u;
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore

            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 512u [| String.replicate 512 "a"; |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "Unexpected PDU was received." )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_024() =
        let sp, cp = GlbFunc.GetNetConn()
        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 512u cp

                // text request part 1
                do! PDU.SendPDU( 512u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0ul;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "SendTargets=All"
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore

                // receive result PDU
                let! recvPDU4 = PDU.Receive( 512u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU4.Opcode = OpcodeCd.TEXT_RES ) );
                let recvPDU4L = recvPDU4 :?> TextResponsePDU
                Assert.True( recvPDU4L.ExpCmdSN = cmdsn_me.fromPrim 1u )
                Assert.True( recvPDU4L.MaxCmdSN = cmdsn_me.fromPrim 1u )
                Assert.True( recvPDU4L.StatSN = statsn_me.fromPrim 2u )

                // Unexpected PDU
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0ul;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 1u;
                            ExpStatSN = statsn_me.fromPrim 3u;
                            TextRequest = [| byte 'a'; |];
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 512u [| String.replicate 512 "a"; |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "Response of Text response PDU with" )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_025() =
        let sp, cp = GlbFunc.GetNetConn()
        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 512u cp

                // text request part 1
                do! PDU.SendPDU( 512u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0ul;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "SendTargets=All"
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore

                // receive result PDU
                let! recvPDU4 = PDU.Receive( 512u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU4.Opcode = OpcodeCd.TEXT_RES ) );
                let recvPDU4L = recvPDU4 :?> TextResponsePDU
                Assert.True( recvPDU4L.ExpCmdSN = cmdsn_me.fromPrim 1u )
                Assert.True( recvPDU4L.MaxCmdSN = cmdsn_me.fromPrim 1u )
                Assert.True( recvPDU4L.StatSN = statsn_me.fromPrim 2u )

                // Unexpected PDU
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = true;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0ul;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 1u;
                            ExpStatSN = statsn_me.fromPrim 3u;
                            TextRequest = Array.empty;
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 512u [| String.replicate 512 "a"; |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "Response of Text response PDU with" )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_026() =
        let sp, cp = GlbFunc.GetNetConn()
        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 512u cp

                // text request part 1
                do! PDU.SendPDU( 512u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0ul;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "SendTargets=All"
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore

                // receive result PDU
                let! recvPDU4 = PDU.Receive( 512u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU4.Opcode = OpcodeCd.TEXT_RES ) );
                let recvPDU4L = recvPDU4 :?> TextResponsePDU
                Assert.True( recvPDU4L.ExpCmdSN = cmdsn_me.fromPrim 1u )
                Assert.True( recvPDU4L.MaxCmdSN = cmdsn_me.fromPrim 1u )
                Assert.True( recvPDU4L.StatSN = statsn_me.fromPrim 2u )

                // Unexpected PDU
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0ul;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 99u;  // Invalid
                            ExpStatSN = statsn_me.fromPrim 3u;
                            TextRequest = Array.empty;
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 512u [| String.replicate 512 "a"; |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "Unexpected CmdSN was received" )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.DiscoverySession_027() =
        let sp, cp = GlbFunc.GetNetConn()
        let initiator =
            fun () -> task {
                do! initiatorLoginSequense 512u cp

                // text request part 1
                do! PDU.SendPDU( 512u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0ul;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 0u;
                            ExpStatSN = statsn_me.fromPrim 2u;
                            TextRequest = Encoding.GetEncoding( "utf-8" ).GetBytes "SendTargets=All"
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore

                // receive result PDU
                let! recvPDU4 = PDU.Receive( 512u, DigestType.DST_None, DigestType.DST_None, ValueSome tsih_me.zero, ValueSome cid_me.zero, ValueSome concnt_me.zero, cp, Standpoint.Initiator )
                Assert.True( ( recvPDU4.Opcode = OpcodeCd.TEXT_RES ) );
                let recvPDU4L = recvPDU4 :?> TextResponsePDU
                Assert.True( recvPDU4L.ExpCmdSN = cmdsn_me.fromPrim 1u )
                Assert.True( recvPDU4L.MaxCmdSN = cmdsn_me.fromPrim 1u )
                Assert.True( recvPDU4L.StatSN = statsn_me.fromPrim 2u )

                // Unexpected PDU
                do! PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueSome( tsih1 ), ValueSome( cid1 ), ValueSome( ccnt1 ), objidx_me.NewID(), cp,
                        {
                            I = false;
                            F = false;
                            C = false;
                            LUN = lun_me.zero;
                            InitiatorTaskTag = itt_me.fromPrim 0ul;
                            TargetTransferTag = ttt_me.fromPrim 0u;
                            CmdSN = cmdsn_me.fromPrim 1u;
                            ExpStatSN = statsn_me.fromPrim 99u;    // Invalid
                            TextRequest = Array.empty;
                            ByteCount = 0u;
                        }
                    )
                    |> Functions.TaskIgnore
            }

        Functions.RunTaskInPallalel [|
            initiator;
            fun () -> task {
                try
                    do! createDefaultTargetTask 512u [| String.replicate 512 "a"; |] [| "127.0.0.1" |] [| 100us |] sp;
                    Assert.Fail __LINE__
                with
                | :? SessionRecoveryException as x ->
                    Assert.True( x.Message.StartsWith "Unexpected ExpStatSN was received" )
                | _ ->
                    Assert.Fail __LINE__
            }
        |]
        |> Functions.RunTaskSynchronously
        |> ignore
        GlbFunc.ClosePorts [| sp; cp |]

    [<Fact>]
    member _.GenTargetAddressString_001() =
        let np : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.zero;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetAddress = "aaa";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 8192;
            SendBufferSize = 8192;
            WhiteList = [];
        }
        let lp = System.Net.IPAddress.Parse "::1"
        let s = PrivateCaller.Invoke< LoginNegociator >( "GenTargetAddressString", np, lp ) :?> string
        Assert.True(( s = "aaa" ))

    [<Fact>]
    member _.GenTargetAddressString_002() =
        let np : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.zero;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetAddress = "127.0.0.1";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 8192;
            SendBufferSize = 8192;
            WhiteList = [];
        }
        let lp = System.Net.IPAddress.Parse "::1"
        let s = PrivateCaller.Invoke< LoginNegociator >( "GenTargetAddressString", np, lp ) :?> string
        Assert.True(( s = "127.0.0.1" ))

    [<Fact>]
    member _.GenTargetAddressString_003() =
        let np : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.zero;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetAddress = "::FFFF:127.99.88.77";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 8192;
            SendBufferSize = 8192;
            WhiteList = [];
        }
        let lp = System.Net.IPAddress.Parse "::1"
        let s = PrivateCaller.Invoke< LoginNegociator >( "GenTargetAddressString", np, lp ) :?> string
        Assert.True(( s = "127.99.88.77" ))

    [<Fact>]
    member _.GenTargetAddressString_004() =
        let np : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.zero;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetAddress = "AAAA:BBBB:CCCC:DDDD:EEEE:FFFF:1111:2222";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 8192;
            SendBufferSize = 8192;
            WhiteList = [];
        }
        let lp = System.Net.IPAddress.Parse "::1"
        let s = PrivateCaller.Invoke< LoginNegociator >( "GenTargetAddressString", np, lp ) :?> string
        Assert.True(( s.ToUpper() = "[AAAA:BBBB:CCCC:DDDD:EEEE:FFFF:1111:2222]" ))

    [<Fact>]
    member _.GenTargetAddressString_005() =
        let np : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.zero;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetAddress = "::1";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 8192;
            SendBufferSize = 8192;
            WhiteList = [];
        }
        let lp = System.Net.IPAddress.Parse "::1"
        let s = PrivateCaller.Invoke< LoginNegociator >( "GenTargetAddressString", np, lp ) :?> string
        Assert.True(( s = "[::1]" ))

    [<Fact>]
    member _.GenTargetAddressString_006() =
        let np : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.zero;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetAddress = "";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 8192;
            SendBufferSize = 8192;
            WhiteList = [];
        }
        let lp = System.Net.IPAddress.Parse "192.168.1.1"
        let s = PrivateCaller.Invoke< LoginNegociator >( "GenTargetAddressString", np, lp ) :?> string
        Assert.True(( s = "192.168.1.1" ))

    [<Fact>]
    member _.GenTargetAddressString_007() =
        let np : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.zero;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetAddress = "";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 8192;
            SendBufferSize = 8192;
            WhiteList = [];
        }
        let lp = System.Net.IPAddress.Parse "::FFFF:192.168.88.99"
        let s = PrivateCaller.Invoke< LoginNegociator >( "GenTargetAddressString", np, lp ) :?> string
        Assert.True(( s = "192.168.88.99" ))

    [<Fact>]
    member _.GenTargetAddressString_008() =
        let np : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.zero;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetAddress = "";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 8192;
            SendBufferSize = 8192;
            WhiteList = [];
        }
        let lp = System.Net.IPAddress.Parse "AAAA:BBBB:CCCC:DDDD:EEEE:FFFF:1111:2222"
        let s = PrivateCaller.Invoke< LoginNegociator >( "GenTargetAddressString", np, lp ) :?> string
        Assert.True(( s.ToUpper() = "[AAAA:BBBB:CCCC:DDDD:EEEE:FFFF:1111:2222]" ))

    [<Fact>]
    member _.GenTargetAddressString_009() =
        let np : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.zero;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetAddress = "";
            PortNumber = 0us;
            DisableNagle = false;
            ReceiveBufferSize = 8192;
            SendBufferSize = 8192;
            WhiteList = [];
        }
        let lp = System.Net.IPAddress.Parse "::1"
        let s = PrivateCaller.Invoke< LoginNegociator >( "GenTargetAddressString", np, lp ) :?> string
        Assert.True(( s = "[::1]" ))
