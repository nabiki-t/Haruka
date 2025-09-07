namespace Haruka.Test.IT.ISCSI

open System
open System.Threading
open System.Threading.Tasks
open System.IO
open System.Diagnostics
open System.Net.Sockets

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.Test
open Xunit.Abstractions
open System.Text
open System.Net

[<CollectionDefinition( "iSCSI_LoginTest2" )>]
type iSCSI_LoginTest2_Fixture() =

    let m_TargetCount = 
        uint64 Constants.MAX_LOGICALUNIT_COUNT_IN_TD
        |> min ( uint64 Constants.MAX_TARGET_COUNT_IN_TD )
        |> min ( uint64 Constants.MAX_LOGICALUNIT_COUNT_IN_TD )
        |> min Constants.MAX_LUN_VALUE

    let m_LUCount = m_TargetCount

    let m_TD0_iSCSIPortNo = [|
        for i = 0 to Constants.MAX_NETWORK_PORTAL_COUNT - 1 do
            GlbFunc.nextTcpPortNo()
    |]
    let m_TDx_iSCSIPortNo = [|
        for i = 0 to Constants.MAX_TARGET_DEVICE_COUNT - 2 do
            GlbFunc.nextTcpPortNo()
    |]

    let m_TD0_MediaSize =
        let w =
            134217728u
            |> max Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength
            |> max Constants.NEGOPARAM_MAX_MaxBurstLength
            |> max Constants.NEGOPARAM_MAX_FirstBurstLength
        Functions.AddPaddingLengthUInt32 w 4096u

    // Add default configurations
    let AddDefaultConf( client : ClientProc ): unit =

        ///////////////////////////////
        // Target Device 0
        client.RunCommand "create" "Created" "CR> "
        client.RunCommand "select 0" "" "TD> "
        client.RunCommand ( sprintf "set MAXRECVDATASEGMENTLENGTH %d" Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength ) "" "TD> "
        client.RunCommand ( sprintf "set MAXBURSTLENGTH %d" Constants.NEGOPARAM_MAX_MaxBurstLength ) "" "TD> "
        client.RunCommand ( sprintf "set FIRSTBURSTLENGTH %d" Constants.NEGOPARAM_MAX_FirstBurstLength ) "" "TD> "
        client.RunCommand "create targetgroup" "Created" "TD> "
        for pn in m_TD0_iSCSIPortNo do
            client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" pn ) "Created" "TD> "
        client.RunCommand "select 0" "" "TG> "

        // target1-255, LU=1-255
        for i = 1 to ( int m_TargetCount ) do
            client.RunCommand ( sprintf "create /n iqn.2020-05.example.com:target%d" i ) "Created" "TG> "
            client.RunCommand ( sprintf "select %d" ( i - 1 ) ) "" "T > "
            client.RunCommand ( sprintf "create /l %d" i ) "Created" "T > "
            client.RunCommand "select 0" "" "LU> "
            client.RunCommand ( sprintf "create membuffer /s %d" m_TD0_MediaSize ) "Created" "LU> "
            client.RunCommand "unselect" "" "T > "
            client.RunCommand "unselect" "" "TG> "

        client.RunCommand "unselect" "" "TD> "
        client.RunCommand "unselect" "" "CR> "

        ///////////////////////////////
        // Target Device 1-15
        for i = 0 to Constants.MAX_TARGET_DEVICE_COUNT - 2 do
            client.RunCommand "create" "Created" "CR> "
            client.RunCommand ( sprintf "select %d" ( i + 1 ) ) "" "TD> "
            client.RunCommand ( sprintf "set MAXRECVDATASEGMENTLENGTH %d" Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength ) "" "TD> "
            client.RunCommand ( sprintf "set MAXBURSTLENGTH %d" Constants.NEGOPARAM_MIN_MaxBurstLength ) "" "TD> "
            client.RunCommand ( sprintf "set FIRSTBURSTLENGTH %d" Constants.NEGOPARAM_MIN_FirstBurstLength ) "" "TD> "
            client.RunCommand ( sprintf "set MAXOUTSTANDINGR2T %d" 16 ) "" "TD> "
            client.RunCommand "create targetgroup" "Created" "TD> "
            client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_TDx_iSCSIPortNo.[i] ) "Created" "TD> "
            client.RunCommand "select 0" "" "TG> "
            client.RunCommand "create /n iqn.2020-05.example.com:target2-1" "Created" "TG> "
            client.RunCommand "select 0" "" "T > "
            client.RunCommand "create /l 1" "Created" "T > "
            client.RunCommand "select 0" "" "LU> "
            client.RunCommand "create membuffer /s 65536" "Created" "LU> "
            client.RunCommand "unselect" "" "T > "
            client.RunCommand "unselect" "" "TG> "
            client.RunCommand "unselect" "" "TD> "
            client.RunCommand "unselect" "" "CR> "

        client.RunCommand "validate" "All configurations are vlidated" "CR> "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "CR> "

        // Start all target devices
        for i = 0 to Constants.MAX_TARGET_DEVICE_COUNT - 1 do
            client.RunCommand ( sprintf "select %d" i ) "" "TD> "
            client.RunCommand "start" "Started" "TD> "
            client.RunCommand "unselect" "" "CR> "

        client.RunCommand "logout" "" "--> "
        ()

    // Start controller and client
    let m_Controller, m_Client =
        let workPath =
            let tempPath = Path.GetTempPath()
            Functions.AppendPathName tempPath ( Guid.NewGuid().ToString( "N" ) )
        let controllPortNo = GlbFunc.nextTcpPortNo()
        let controller, client = TestFunctions.StartHarukaController workPath controllPortNo
        AddDefaultConf client
        controller, client

    interface IDisposable with
        member _.Dispose (): unit =
            m_Client.Kill()

    interface ICollectionFixture<iSCSI_LoginTest2_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.TD0_iSCSIPortNo = m_TD0_iSCSIPortNo
    member _.TDx_iSCSIPortNo = m_TDx_iSCSIPortNo
    member _.TD0_MediaSize = m_TD0_MediaSize
    member _.targetCount = m_TargetCount
    member _.luCount = m_LUCount
    member _.MediaBlockSize = uint Constants.MEDIA_BLOCK_SIZE


[<Collection( "iSCSI_LoginTest2" )>]
type iSCSI_LoginTest2( fx : iSCSI_LoginTest2_Fixture ) =

    let g_CID0 = cid_me.zero
    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL
    let g_DefTTT = ttt_me.fromPrim 0xFFFFFFFFu

    let m_TD0_iSCSIPortNo = fx.TD0_iSCSIPortNo
    let m_TDx_iSCSIPortNo = fx.TDx_iSCSIPortNo
    let m_TD0_MediaSize = fx.TD0_MediaSize
    let m_TargetCount = fx.targetCount
    let m_LUCount = fx.luCount
    let m_MediaBlockSize = fx.MediaBlockSize

    // default session parameters
    let m_defaultSessParam = {
        InitiatorName = "iqn.2020-05.example.com:initiator";
        InitiatorAlias = "aaa";
        TargetName = "iqn.2020-05.example.com:target1";
        TargetAlias = "";
        ISID = isid_me.fromPrim 1UL;
        TSIH = tsih_me.fromPrim 0us;
        MaxConnections = Constants.NEGOPARAM_MaxConnections;
        InitialR2T = false;
        ImmediateData = true;
        MaxBurstLength = 262144u;
        FirstBurstLength = 262144u;
        DefaultTime2Wait = 2us;
        DefaultTime2Retain = 20us;
        MaxOutstandingR2T = 16us;
        DataPDUInOrder = false;
        DataSequenceInOrder = false;
        ErrorRecoveryLevel = 0uy;
    }

    // default connection parameters
    let m_defaultConnParam = {
        PortNo = m_TD0_iSCSIPortNo.[0];
        CID = g_CID0;
        Initiator_UserName = "";
        Initiator_Password = "";
        Target_UserName = "";
        Target_Password = "";
        HeaderDigest = DigestType.DST_CRC32C;
        DataDigest = DigestType.DST_CRC32C;
        MaxRecvDataSegmentLength_I = 262144u;
        MaxRecvDataSegmentLength_T = 262144u;
    }

    let scsiWrite10CDB ( transferLength : uint16 ) =
        let w =
            ( int16 ) transferLength
            |> IPAddress.HostToNetworkOrder
            |> BitConverter.GetBytes
        [|
            0x2Auy;                         // OPERATION CODE( Write 10 )
            0x00uy;                         // WRPROTECT(000), DPO(0), FUA(0), FUA_NV(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LBA
            0x00uy;                         // GROUP NUMBER(0)
            w.[0]; w.[1];                   // TRANSFER LENGTH
            0x02uy;                         // NACA(1), LINK(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // padding
            0x00uy; 0x00uy;
        |]

    let scsiRead10CDB ( transferLength : uint16 ) =
        let w =
            ( int16 ) transferLength
            |> IPAddress.HostToNetworkOrder
            |> BitConverter.GetBytes
        [|
            0x28uy;                         // OPERATION CODE( Read 10 )
            0x00uy;                         // RDPROTECT(000), DPO(0), FUA(0), FUA_NV(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LBA
            0x00uy;                         // GROUP NUMBER(0)
            w.[0]; w.[1];                   // TRANSFER LENGTH
            0x02uy;                         // NACA(1), LINK(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // padding
            0x00uy; 0x00uy;
        |]


    [<Fact>]
    member _.MaxSessPerTaget_001() =
        task {
            // login for same target
            let wv = Array.zeroCreate<iSCSI_Initiator> Constants.MAX_SESSION_COUNT_IN_TARGET
            for i = 1 to Constants.MAX_SESSION_COUNT_IN_TARGET do
                let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
                wv.[i-1] <- r1

            // add more aession and failed
            try
                let! _ = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()

            // logout
            for itr in wv do
                let! _ = itr.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
                let! rpdu5 = itr.ReceiveSpecific<LogoutResponsePDU> g_CID0
                Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.MaxSessPerTagetDevice_001() =
        task {
            let testSessCount = min Constants.MAX_SESSION_COUNT_IN_TD Constants.MAX_SESSION_COUNT_IN_LU
            let wv = Array.zeroCreate<iSCSI_Initiator> testSessCount

            for i = 0 to testSessCount - 1 do
                let targetnamenum = ( i % int m_TargetCount ) + 1
                let sessParam1 = {
                    m_defaultSessParam with
                        TargetName = sprintf "iqn.2020-05.example.com:target%d" targetnamenum
                }
                let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
                wv.[i] <- r1

            // add more session and failed
            try
                let! _ = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()

            // logout
            for itr in wv do
                let! _ = itr.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
                let! rpdu5 = itr.ReceiveSpecific<LogoutResponsePDU> g_CID0
                Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.MaxConnPerSession_001() =
        // Harula does not support the function of constraining the maximum number of connections.
        // MaxConnections value is always 16.
        task {
            // login for connection 0
            let connParam1 = {
                m_defaultConnParam with
                    CID = cid_me.zero;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam connParam1

            // add connection 1 to 16
            for i = 2 to int m_defaultSessParam.MaxConnections do
                let cp = {
                    m_defaultConnParam with
                        CID = cid_me.fromPrim( uint16 i )
                }
                do! r1.AddConnection cp

            // add more connection and failed.
            try
                let connParam2 = {
                    m_defaultConnParam with
                        CID = cid_me.fromPrim( m_defaultSessParam.MaxConnections + 1us )
                }
                do! r1.AddConnection connParam2
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()

            // logout
            let! _ = r1.SendLogoutRequestPDU cid_me.zero false LogoutReqReasonCd.CLOSE_SESS cid_me.zero
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginAllNetworkPortal_001() =
        task {
            let wv = Array.zeroCreate<iSCSI_Initiator> m_TD0_iSCSIPortNo.Length

            // login for network portal 0 to 16
            for i = 0 to wv.Length - 1 do
                let targetnamenum = ( i % int m_TargetCount ) + 1
                let sessParam1 = {
                    m_defaultSessParam with
                        TargetName = sprintf "iqn.2020-05.example.com:target%d" targetnamenum
                }
                let connParam1 = {
                    m_defaultConnParam with
                        CID = g_CID0;
                        PortNo = m_TD0_iSCSIPortNo.[i];
                }
                let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 connParam1
                wv.[i] <- r1

            // Nop-Out
            for i = 0 to wv.Length - 1 do
                let lun = ( i % int m_TargetCount ) + 1 |> uint64 |> lun_me.fromPrim
                let! _ = wv.[i].SendNOPOutPDU g_CID0 false lun g_DefTTT PooledBuffer.Empty
                let! rpdu3 = wv.[i].ReceiveSpecific<NOPInPDU> g_CID0
                rpdu3.PingData.Return()

            // logout
            for itr in wv do
                let! _ = itr.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
                let! rpdu5 = itr.ReceiveSpecific<LogoutResponsePDU> g_CID0
                Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginAllNetworkPortal_002() =
        task {
            // login for network portal 0
            let connParam1 = {
                m_defaultConnParam with
                    CID = cid_me.zero;
                    PortNo = m_TD0_iSCSIPortNo.[0];
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam connParam1

            // add connection for network portal 1 to 16
            let wconcnt = min m_TD0_iSCSIPortNo.Length ( int m_defaultSessParam.MaxConnections )
            for i = 1 to wconcnt - 1 do
                let connParam2 = {
                    m_defaultConnParam with
                        CID = cid_me.fromPrim ( uint16 i )
                        PortNo = m_TD0_iSCSIPortNo.[i];
                }
                do! r1.AddConnection connParam2

            // Nop-Out
            for i = 0 to wconcnt - 1 do
                let cid = cid_me.fromPrim ( uint16 i )
                let! _ = r1.SendNOPOutPDU cid false g_LUN1 g_DefTTT PooledBuffer.Empty
                let! rpdu3 = r1.ReceiveSpecific<NOPInPDU> cid
                rpdu3.PingData.Return()

            // logout
            let! _ = r1.SendLogoutRequestPDU cid_me.zero false LogoutReqReasonCd.CLOSE_SESS cid_me.zero
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> cid_me.zero
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginAllTargetDevice_001() =
        [|
            for i = 0 to Constants.MAX_TARGET_DEVICE_COUNT - 1 do
                fun () -> task {
                    // login
                    let portno =
                        if i = 0 then
                            m_TD0_iSCSIPortNo.[0];
                        else
                            m_TDx_iSCSIPortNo.[ i - 1 ];
                    let targetName =
                        if i = 0 then
                            "iqn.2020-05.example.com:target1"
                        else
                            "iqn.2020-05.example.com:target2-1"
                    let sessParam1 = {
                        m_defaultSessParam with
                            TargetName = targetName;
                    }
                    let connParam1 = {
                        m_defaultConnParam with
                            CID = cid_me.zero;
                            PortNo = portno;
                    }
                    let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 connParam1

                    // Nop-Out
                    let cid = cid_me.zero
                    let! _ = r1.SendNOPOutPDU cid false g_LUN1 g_DefTTT PooledBuffer.Empty
                    let! rpdu3 = r1.ReceiveSpecific<NOPInPDU> cid
                    rpdu3.PingData.Return()

                    // logout
                    let! _ = r1.SendLogoutRequestPDU cid_me.zero false LogoutReqReasonCd.CLOSE_SESS cid_me.zero
                    let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> cid_me.zero
                    Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
                }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore

    [<Fact>]
    member _.DiscoverySession_001() =
        task {
            let connParam1 = {
                m_defaultConnParam with
                    CID = g_CID0;
                    PortNo = m_TD0_iSCSIPortNo.[0];
                    MaxRecvDataSegmentLength_I = 8192u;
                    MaxRecvDataSegmentLength_T = 8192u;
            }
            let! r1 = iSCSI_Initiator.DiscoverySession connParam1 "All"
            Assert.True(( r1.Count = int m_TargetCount ))

            let expectTargetAddress =
                [|
                    for itr in m_TD0_iSCSIPortNo -> sprintf "[::1]:%d,0" itr
                |]
                |> Array.sort

            for i = 1 to int m_TargetCount do
                let targetName = sprintf "iqn.2020-05.example.com:target%d" i
                let v1 = r1.[ targetName ]
                Assert.True(( v1.Length = expectTargetAddress.Length ))
                Assert.True(( ( Array.sort v1 ) = expectTargetAddress ))
        }

    [<Fact>]
    member _.LoginNego_HeaderDigest_001() =
        task {
            let connParam1 = {
                m_defaultConnParam with
                    CID = g_CID0;
                    HeaderDigest = DigestType.DST_None;
                    DataDigest = DigestType.DST_CRC32C;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam connParam1

            Assert.True(( r1.Connection( g_CID0 ).Params.HeaderDigest = DigestType.DST_None ))
            Assert.True(( r1.Connection( g_CID0 ).Params.DataDigest = DigestType.DST_CRC32C ))

            // Nop-Out
            let buf = PooledBuffer.Rent [| 0uy .. 255uy |]
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf
            let! rpdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( PooledBuffer.ValueEquals rpdu3.PingData buf ))
            buf.Return()
            rpdu3.PingData.Return()

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_DataDigest_001() =
        task {
            let connParam1 = {
                m_defaultConnParam with
                    CID = g_CID0;
                    HeaderDigest = DigestType.DST_CRC32C;
                    DataDigest = DigestType.DST_None;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam connParam1

            Assert.True(( r1.Connection( g_CID0 ).Params.HeaderDigest = DigestType.DST_CRC32C ))
            Assert.True(( r1.Connection( g_CID0 ).Params.DataDigest = DigestType.DST_None ))

            // Nop-Out
            let buf = PooledBuffer.Rent [| 0uy .. 255uy |]
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf
            let! rpdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( PooledBuffer.ValueEquals rpdu3.PingData buf ))
            buf.Return()
            rpdu3.PingData.Return()

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_MaxRecvDataSegmentLength_001() =
        task {
            let rand = Random()
            let mrdsl_i = Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength
            let connParam1 = {
                m_defaultConnParam with
                    CID = g_CID0;
                    MaxRecvDataSegmentLength_I = mrdsl_i;
                    MaxRecvDataSegmentLength_T = 0u;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam connParam1
            Assert.True(( r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_I = mrdsl_i ))
            Assert.True(( r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength ))

            // Nop-Out ( Negociated MaxRecvDataSegmentLength )
            let buf1 = PooledBuffer.Rent( int mrdsl_i )
            rand.NextBytes( buf1.ArraySegment.AsSpan() )
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf1
            let! rpdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( PooledBuffer.ValueEquals rpdu3.PingData buf1 ))
            buf1.Return()
            rpdu3.PingData.Return()

            // Nop-Out ( Negociated MaxRecvDataSegmentLength + 1 )
            let buf2 = PooledBuffer.Rent( int mrdsl_i + 1 )
            rand.NextBytes( buf2.ArraySegment.AsSpan() )
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf2
            let! rpdu4 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            let buf2_2 = PooledBuffer.Truncate ( int mrdsl_i ) buf2
            Assert.True(( PooledBuffer.ValueEquals rpdu4.PingData buf2_2 ))
            buf2.Return()
            rpdu4.PingData.Return()

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_MaxRecvDataSegmentLength_002() =
        task {
            let rand = Random()
            let mrdsl_i = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength
            let connParam1 = {
                m_defaultConnParam with
                    CID = g_CID0;
                    MaxRecvDataSegmentLength_I = mrdsl_i;
                    MaxRecvDataSegmentLength_T = 0u;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam connParam1
            Assert.True(( r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_I = mrdsl_i ))
            Assert.True(( r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength ))

            // Nop-Out ( Negociated MaxRecvDataSegmentLength - 1 )
            let buf1 = PooledBuffer.Rent( int mrdsl_i - 1 )
            rand.NextBytes( buf1.ArraySegment.AsSpan() )
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf1
            let! rpdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( PooledBuffer.ValueEquals rpdu3.PingData buf1 ))
            buf1.Return()
            rpdu3.PingData.Return()

            // Nop-Out ( Negociated MaxRecvDataSegmentLength )
            let buf2 = PooledBuffer.Rent( int mrdsl_i )
            rand.NextBytes( buf2.ArraySegment.AsSpan() )
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf2
            let! rpdu4 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( PooledBuffer.ValueEquals rpdu4.PingData buf2 ))
            buf2.Return()
            rpdu4.PingData.Return()

            // Nop-Out ( Negociated MaxRecvDataSegmentLength + 1 )
            let buf3 = PooledBuffer.Rent( int mrdsl_i + 1 )
            rand.NextBytes( buf3.ArraySegment.AsSpan() )
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf3
            let! rpdu5 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            let buf3_2 = PooledBuffer.Truncate ( int mrdsl_i ) buf3
            Assert.True(( PooledBuffer.ValueEquals rpdu5.PingData buf3_2 ))
            buf3.Return()
            rpdu5.PingData.Return()

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_MaxRecvDataSegmentLength_003() =
        task {
            let rand = Random()
            let mrdsl_i = Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength
            let connParam1 = {
                m_defaultConnParam with
                    CID = g_CID0;
                    MaxRecvDataSegmentLength_I = mrdsl_i;
                    MaxRecvDataSegmentLength_T = 0u;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam connParam1
            Assert.True(( r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_I = mrdsl_i ))
            Assert.True(( r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength ))

            // Nop-Out ( Negociated MaxRecvDataSegmentLength - 1 )
            let buf1 = PooledBuffer.Rent( int mrdsl_i - 1 )
            rand.NextBytes( buf1.ArraySegment.AsSpan() )
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf1
            let! rpdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( PooledBuffer.ValueEquals rpdu3.PingData buf1 ))
            buf1.Return()
            rpdu3.PingData.Return()

            // Nop-Out ( Negociated MaxRecvDataSegmentLength )
            let buf2 = PooledBuffer.Rent( int mrdsl_i )
            rand.NextBytes( buf2.ArraySegment.AsSpan() )
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf2
            let! rpdu4 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( PooledBuffer.ValueEquals rpdu4.PingData buf2 ))
            buf2.Return()
            rpdu4.PingData.Return()

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_MaxRecvDataSegmentLength_004() =
        task {
            let rand = Random()
            let mrdsl_i = Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength
            let sessParam1 = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2-1";
            }
            let connParam1 = {
                m_defaultConnParam with
                    PortNo = m_TDx_iSCSIPortNo.[0]; // target device 1
                    CID = g_CID0;
                    MaxRecvDataSegmentLength_I = mrdsl_i;
                    MaxRecvDataSegmentLength_T = 0u;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 connParam1
            Assert.True(( r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_I = mrdsl_i ))
            Assert.True(( r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength ))

            // Nop-Out ( Negociated NEGOPARAM_MIN_MaxRecvDataSegmentLength - 1 )
            let buf1 = PooledBuffer.Rent( int Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength - 1 )
            rand.NextBytes( buf1.ArraySegment.AsSpan() )
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf1
            let! rpdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( PooledBuffer.ValueEquals rpdu3.PingData buf1 ))
            buf1.Return()
            rpdu3.PingData.Return()

            // Nop-Out ( Negociated NEGOPARAM_MIN_MaxRecvDataSegmentLength )
            let buf2 = PooledBuffer.Rent( int Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength )
            rand.NextBytes( buf2.ArraySegment.AsSpan() )
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf2
            let! rpdu4 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( PooledBuffer.ValueEquals rpdu4.PingData buf2 ))
            buf2.Return()
            rpdu4.PingData.Return()

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_MaxRecvDataSegmentLength_005() =
        task {
            let rand = Random()
            let mrdsl_i = Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength
            let sessParam1 = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2-1";
            }
            let connParam1 = {
                m_defaultConnParam with
                    PortNo = m_TDx_iSCSIPortNo.[0]; // target device 1
                    CID = g_CID0;
                    MaxRecvDataSegmentLength_I = mrdsl_i;
                    MaxRecvDataSegmentLength_T = 0u;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 connParam1
            Assert.True(( r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_I = mrdsl_i ))
            Assert.True(( r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength ))

            // Nop-Out ( Negociated NEGOPARAM_MIN_MaxRecvDataSegmentLength - 1 )
            let buf1 = PooledBuffer.Rent( int Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength - 1 )
            rand.NextBytes( buf1.ArraySegment.AsSpan() )
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf1
            let! rpdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( PooledBuffer.ValueEquals rpdu3.PingData buf1 ))
            buf1.Return()
            rpdu3.PingData.Return()

            // Nop-Out ( Negociated NEGOPARAM_MIN_MaxRecvDataSegmentLength )
            let buf2 = PooledBuffer.Rent( int Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength )
            rand.NextBytes( buf2.ArraySegment.AsSpan() )
            let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf2
            let! rpdu4 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            Assert.True(( PooledBuffer.ValueEquals rpdu4.PingData buf2 ))
            buf2.Return()
            rpdu4.PingData.Return()

            // Nop-Out ( Negociated NEGOPARAM_MIN_MaxRecvDataSegmentLength + 1 )
            r1.FakeConnectionParameter g_CID0 {
                r1.Connection( g_CID0 ).Params with
                    MaxRecvDataSegmentLength_T = 0xFFFFFFFFu;
            }
            let buf3 = PooledBuffer.Rent( int Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength + 1 )
            rand.NextBytes( buf3.ArraySegment.AsSpan() )
            try
                let! _ = r1.SendNOPOutPDU g_CID0 false g_LUN1 g_DefTTT buf3
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()
            buf3.Return()
        }

    [<Fact>]
    member _.LoginNego_InitialR2T_001() =
        task {
            let blockSize = m_MediaBlockSize
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = false;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.False(( r1.Params.InitialR2T ))

            // SCSI Write
            let writeCDB = scsiWrite10CDB 1us
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 blockSize writeCDB PooledBuffer.Empty 0u

            // SCSI Data-Out
            let sendData = PooledBuffer.RentAndInit ( int blockSize )
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 g_DefTTT ( datasn_me.zero ) 0u sendData
            sendData.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_InitialR2T_002() =
        task {
            let blockSize = m_MediaBlockSize
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = true;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.True(( r1.Params.InitialR2T ))

            // SCSI Write
            let writeCDB = scsiWrite10CDB 1us
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 blockSize writeCDB PooledBuffer.Empty 0u

            // R2T
            let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( rpdu2.InitiatorTaskTag = itt ))
            Assert.True(( rpdu2.R2TSN = datasn_me.zero ))
            Assert.True(( rpdu2.BufferOffset = 0u ))
            Assert.True(( rpdu2.DesiredDataTransferLength = blockSize ))

            // SCSI Data-Out
            let sendData = PooledBuffer.RentAndInit ( int blockSize )
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu2.TargetTransferTag ( datasn_me.zero ) 0u sendData
            sendData.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }
        
    [<Fact>]
    member _.LoginNego_ImmediateData_001() =
        task {
            let blockSize = m_MediaBlockSize
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = false;
                    ImmediateData = true;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.True(( r1.Params.ImmediateData ))

            // SCSI Write
            let writeCDB = scsiWrite10CDB 1us
            let sendData = PooledBuffer.RentAndInit ( int blockSize )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 blockSize writeCDB sendData 0u
            sendData.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }
        
    [<Fact>]
    member _.LoginNego_ImmediateData_002() =
        task {
            let blockSize = m_MediaBlockSize
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = false;
                    ImmediateData = true;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.True(( r1.Params.ImmediateData ))

            // SCSI Write
            let writeCDB = scsiWrite10CDB 1us
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 blockSize writeCDB PooledBuffer.Empty 0u

            // SCSI Data-Out
            let sendData = PooledBuffer.RentAndInit ( int blockSize )
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 g_DefTTT ( datasn_me.zero ) 0u sendData
            sendData.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }
        
    [<Fact>]
    member _.LoginNego_ImmediateData_003() =
        task {
            let blockSize = m_MediaBlockSize
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = false;
                    ImmediateData = false;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.False(( r1.Params.ImmediateData ))

            // SCSI Write
            let writeCDB = scsiWrite10CDB 1us
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 blockSize writeCDB PooledBuffer.Empty 0u

            // SCSI Data-Out
            let sendData = PooledBuffer.RentAndInit ( int blockSize )
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 g_DefTTT ( datasn_me.zero ) 0u sendData
            sendData.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_ImmediateData_004() =
        task {
            let blockSize = m_MediaBlockSize
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = false;
                    ImmediateData = false;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.False(( r1.Params.ImmediateData ))

            // SCSI Write
            let writeCDB = scsiWrite10CDB 1us
            let sendData = PooledBuffer.RentAndInit ( int blockSize )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 blockSize writeCDB sendData 0u
            sendData.Return()

            // Reject
            let! rpdu3 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rpdu3.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
            r1.CmdSN <- cmdsn_me.decr 1u r1.CmdSN

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }
        
    [<Fact>]
    member _.LoginNego_ImmediateData_005() =
        task {
            let blockSize = m_MediaBlockSize
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = true;
                    ImmediateData = true;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.True(( r1.Params.ImmediateData ))

            // SCSI Write
            let writeCDB = scsiWrite10CDB 2us   // 2block
            let sendData = PooledBuffer.RentAndInit ( int blockSize )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 ( blockSize * 2u ) writeCDB sendData 0u
            sendData.Return()

            // R2T
            let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( rpdu2.InitiatorTaskTag = itt ))
            Assert.True(( rpdu2.R2TSN = datasn_me.zero ))
            Assert.True(( rpdu2.BufferOffset = blockSize ))
            Assert.True(( rpdu2.DesiredDataTransferLength = blockSize ))

            // SCSI Data-Out
            let sendData = PooledBuffer.RentAndInit ( int blockSize )
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu2.TargetTransferTag ( datasn_me.zero ) blockSize sendData
            sendData.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }
        
    [<Fact>]
    member _.LoginNego_ImmediateData_006() =
        task {
            let blockSize = m_MediaBlockSize
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = true;
                    ImmediateData = true;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.True(( r1.Params.ImmediateData ))

            // SCSI Write
            let writeCDB = scsiWrite10CDB 2us   // 2block
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 ( blockSize * 2u ) writeCDB PooledBuffer.Empty 0u

            // R2T
            let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( rpdu2.InitiatorTaskTag = itt ))
            Assert.True(( rpdu2.R2TSN = datasn_me.zero ))
            Assert.True(( rpdu2.BufferOffset = 0u ))
            Assert.True(( rpdu2.DesiredDataTransferLength = blockSize * 2u ))

            // SCSI Data-Out
            let sendData = PooledBuffer.RentAndInit ( int blockSize * 2 )
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu2.TargetTransferTag ( datasn_me.zero ) 0u sendData
            sendData.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }
        
    [<Fact>]
    member _.LoginNego_ImmediateData_007() =
        task {
            let blockSize = m_MediaBlockSize
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = true;
                    ImmediateData = false;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.False(( r1.Params.ImmediateData ))

            // SCSI Write
            let writeCDB = scsiWrite10CDB 2us   // 2block
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 ( blockSize * 2u ) writeCDB PooledBuffer.Empty 0u

            // R2T
            let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( rpdu2.InitiatorTaskTag = itt ))
            Assert.True(( rpdu2.R2TSN = datasn_me.zero ))
            Assert.True(( rpdu2.BufferOffset = 0u ))
            Assert.True(( rpdu2.DesiredDataTransferLength = blockSize * 2u ))

            // SCSI Data-Out
            let sendData = PooledBuffer.RentAndInit ( int blockSize * 2 )
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu2.TargetTransferTag ( datasn_me.zero ) 0u sendData
            sendData.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }
        
    [<Fact>]
    member _.LoginNego_ImmediateData_008() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = true;
                    ImmediateData = false;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.False(( r1.Params.ImmediateData ))

            // SCSI Write
            let writeCDB = scsiWrite10CDB 2us   // 2block
            let sendData = PooledBuffer.RentAndInit ( int m_MediaBlockSize )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 ( m_MediaBlockSize * 2u ) writeCDB sendData 0u
            sendData.Return()

            // Reject
            let! rpdu3 = r1.ReceiveSpecific<RejectPDU> g_CID0
            Assert.True(( rpdu3.Reason = RejectReasonCd.INVALID_PDU_FIELD ))
            r1.CmdSN <- cmdsn_me.decr 1u r1.CmdSN

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Test case where MaxBurstLength value is the minimum.
    // NEGOPARAM_MIN_MaxBurstLength must be 512
    // Media block size must be 512 or 4096
    [<Fact>]
    member _.LoginNego_MaxBurstLength_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = true;
                    MaxBurstLength = Constants.NEGOPARAM_MIN_MaxBurstLength;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            let mbl = r1.Params.MaxBurstLength
            Assert.True(( mbl = Constants.NEGOPARAM_MIN_MaxBurstLength ))

            let accessLength =
                if m_MediaBlockSize > mbl then
                    m_MediaBlockSize        // 4096
                else
                    m_MediaBlockSize * 2u   // 512*2
            let accessBlockCount = accessLength / m_MediaBlockSize
            let r2tCount = accessLength / mbl

            // SCSI Write
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            for i = 0u to r2tCount - 1u do
                // R2T
                let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.R2TSN = datasn_me.fromPrim i ))
                Assert.True(( rpdu2.BufferOffset = mbl * i ))
                Assert.True(( rpdu2.DesiredDataTransferLength = mbl ))

                // SCSI Data-Out
                let sendData = PooledBuffer.RentAndInit ( int rpdu2.DesiredDataTransferLength )
                let datasn = datasn_me.zero
                do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu2.TargetTransferTag datasn rpdu2.BufferOffset sendData
                sendData.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // When the range accessed at one time is smaller than MaxBurstLength
    // NEGOPARAM_MIN_MaxBurstLength must be 512
    // Media block size must be 512 or 4096
    [<Fact>]
    member _.LoginNego_MaxBurstLength_002() =
        task {
            let mbl = 8192u
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = true;
                    MaxBurstLength = mbl;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.True(( r1.Params.MaxBurstLength = mbl ))

            let accessLength = m_MediaBlockSize // 512 or 4096
            let accessBlockCount = 1u

            // SCSI Write
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            // R2T
            let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( rpdu2.InitiatorTaskTag = itt ))
            Assert.True(( rpdu2.R2TSN = datasn_me.zero ))
            Assert.True(( rpdu2.BufferOffset = 0u ))
            Assert.True(( rpdu2.DesiredDataTransferLength = accessLength ))

            // SCSI Data-Out
            let sendData = PooledBuffer.RentAndInit ( int rpdu2.DesiredDataTransferLength )
            let datasn = datasn_me.zero
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu2.TargetTransferTag datasn rpdu2.BufferOffset sendData
            sendData.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Test case where MaxBurstLength value is the maximum.
    // Send data in multiple Data-Out PDUs regardless of the target's MaxRecvDataSegmentLength.
    [<Fact>]
    member _.LoginNego_MaxBurstLength_003() =
        task {
            let dataSegLen = 8192u
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = true;
                    MaxBurstLength = Constants.NEGOPARAM_MAX_MaxBurstLength;    // 16777215
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            let mbl = r1.Params.MaxBurstLength
            Assert.True(( mbl = Constants.NEGOPARAM_MAX_MaxBurstLength ))

            let accessLength = Functions.AddPaddingLengthUInt32 mbl m_MediaBlockSize
            let accessBlockCount = accessLength / m_MediaBlockSize
            let r2tCount =
                let w = accessLength / mbl
                if w * mbl <> accessLength then
                    w + 1u
                else
                    w

            // SCSI Write
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB PooledBuffer.Empty 0u

            for i = 0u to r2tCount - 1u do
                // R2T
                let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.R2TSN = datasn_me.fromPrim i ))
                Assert.True(( rpdu2.BufferOffset = mbl * i ))
                if mbl * ( i + 1u ) > accessLength then
                    Assert.True(( rpdu2.DesiredDataTransferLength = accessLength - ( mbl * i ) ))
                else
                    Assert.True(( rpdu2.DesiredDataTransferLength = mbl ))

                let dataOutCount =
                    let w = rpdu2.DesiredDataTransferLength / dataSegLen
                    if w * dataSegLen <> rpdu2.DesiredDataTransferLength then
                        w + 1u
                    else
                        w

                for j = 0u to dataOutCount - 1u do
                    // SCSI Data-Out
                    let finalFlg = j = dataOutCount - 1u
                    let offsetInSeq = j * dataSegLen
                    let sendLength =
                        if offsetInSeq + dataSegLen > rpdu2.DesiredDataTransferLength then
                            rpdu2.DesiredDataTransferLength - offsetInSeq
                        else
                            dataSegLen
                    let offset = offsetInSeq + rpdu2.BufferOffset
                    let datasn = datasn_me.fromPrim j
                    let sendData = PooledBuffer.RentAndInit ( int sendLength )
                    do! r1.SendSCSIDataOutPDU g_CID0 finalFlg itt g_LUN1 rpdu2.TargetTransferTag datasn offset sendData
                    sendData.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Test case where MaxBurstLength value is the minimum.
    // NEGOPARAM_MIN_MaxBurstLength must be 512
    // Media block size must be 512 or 4096
    [<Fact>]
    member _.LoginNego_MaxBurstLength_004() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = true;
                    MaxBurstLength = Constants.NEGOPARAM_MIN_MaxBurstLength;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            let mbl = r1.Params.MaxBurstLength
            Assert.True(( mbl = Constants.NEGOPARAM_MIN_MaxBurstLength ))

            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize
            let dataPDUCount = 4096u / 512u

            // SCSI Read
            let readCDB = scsiRead10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true true false TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength readCDB PooledBuffer.Empty 0u

            // SCSI Data-In
            for i = 0u to dataPDUCount - 1u do
                let! rpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.F ))
                Assert.True(( rpdu2.DataSN = datasn_me.fromPrim i ))
                Assert.True(( rpdu2.BufferOffset = i * 512u ))
                Assert.True(( rpdu2.DataSegment.Count = int mbl ))

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Test case where MaxBurstLength value is the maximum.
    // Send data in multiple Data-In PDUs regardless of the initiator's MaxRecvDataSegmentLength.
    [<Fact>]
    member _.LoginNego_MaxBurstLength_005() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = true;
                    MaxBurstLength = Constants.NEGOPARAM_MAX_MaxBurstLength;    // 16777215
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            let mbl = r1.Params.MaxBurstLength
            let mrdsl_i = r1.Connection( g_CID0 ).Params.MaxRecvDataSegmentLength_I
            Assert.True(( mbl = Constants.NEGOPARAM_MAX_MaxBurstLength ))

            let accessBlockCount = mbl / m_MediaBlockSize
            let accessLength = accessBlockCount * m_MediaBlockSize
            let dataInCnt =
                let w = accessLength / mrdsl_i
                if w * mrdsl_i <> accessLength then
                    w + 1u
                else
                    w

            // SCSI Read
            let readCDB = scsiRead10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true true false TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength readCDB PooledBuffer.Empty 0u

            // SCSI Data-In
            for i = 0u to dataInCnt - 1u do
                let! rpdu2 = r1.ReceiveSpecific<SCSIDataInPDU> g_CID0
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.DataSN = datasn_me.fromPrim i ))
                Assert.True(( rpdu2.BufferOffset = mrdsl_i * i ))
                if mrdsl_i * ( i + 1u ) > accessLength then
                    Assert.True(( rpdu2.DataSegment.Count = int ( accessLength - ( mrdsl_i * i ) ) ))
                else
                    Assert.True(( rpdu2.DataSegment.Count = int mrdsl_i ))

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_MaxBurstLength_006() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = true;
                    MaxBurstLength = Constants.NEGOPARAM_MAX_MaxBurstLength;    // 16777215
                    TargetName = "iqn.2020-05.example.com:target2-1";
            }
            let connParam1 = {
                m_defaultConnParam with
                    PortNo = m_TDx_iSCSIPortNo.[0]; // TargetDevice 1
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 connParam1
            let mbl = r1.Params.MaxBurstLength
            Assert.True(( mbl = Constants.NEGOPARAM_MIN_MaxBurstLength ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Test case where FirstBurstLength value is the minimum.
    // When sending data using SCSI Command PDU.
    [<Fact>]
    member _.LoginNego_FirstBurstLength_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = false;
                    MaxBurstLength = 4096u;
                    FirstBurstLength = Constants.NEGOPARAM_MIN_FirstBurstLength;    // 512
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            let fbl = r1.Params.FirstBurstLength
            Assert.True(( fbl = Constants.NEGOPARAM_MIN_FirstBurstLength ))

            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize  // Media block size must be 512 or 4096

            // SCSI Write
            let sendData = PooledBuffer.RentAndInit 512
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB sendData 0u
            sendData.Return()

            // R2T
            let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( rpdu2.InitiatorTaskTag = itt ))
            Assert.True(( rpdu2.R2TSN = datasn_me.zero ))
            Assert.True(( rpdu2.BufferOffset = 512u ))
            Assert.True(( rpdu2.DesiredDataTransferLength = accessLength - 512u ))

            // SCSI Data-Out
            let sendData2 = PooledBuffer.RentAndInit ( int accessLength - 512 )
            let datasn = datasn_me.zero
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu2.TargetTransferTag datasn rpdu2.BufferOffset sendData2
            sendData2.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Test case where FirstBurstLength value is the minimum.
    // Sending unsolicited data in a SCSI Data-Out PDU.
    [<Fact>]
    member _.LoginNego_FirstBurstLength_002() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = false;
                    MaxBurstLength = 4096u;
                    FirstBurstLength = Constants.NEGOPARAM_MIN_FirstBurstLength;    // 512
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            let fbl = r1.Params.FirstBurstLength
            Assert.True(( fbl = Constants.NEGOPARAM_MIN_FirstBurstLength ))

            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize  // Media block size must be 512 or 4096

            // SCSI Write
            let sendData = PooledBuffer.Empty
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB sendData 0u

            // SCSI Data-Out PDU
            let sendData2 = PooledBuffer.RentAndInit 512
            let datasn = datasn_me.zero
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 g_DefTTT datasn 0u sendData2
            sendData2.Return()

            // R2T
            let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( rpdu2.InitiatorTaskTag = itt ))
            Assert.True(( rpdu2.R2TSN = datasn_me.zero ))
            Assert.True(( rpdu2.BufferOffset = 512u ))
            Assert.True(( rpdu2.DesiredDataTransferLength = accessLength - 512u ))

            // SCSI Data-Out
            let sendData2 = PooledBuffer.RentAndInit ( int accessLength - 512 )
            let datasn = datasn_me.zero
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu2.TargetTransferTag datasn rpdu2.BufferOffset sendData2
            sendData2.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Test case where FirstBurstLength value is the minimum.
    // When sending unsolicited data exceeding FirstBurstLength.
    [<Fact>]
    member _.LoginNego_FirstBurstLength_003() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = false;
                    MaxBurstLength = 4096u;
                    FirstBurstLength = Constants.NEGOPARAM_MIN_FirstBurstLength;    // 512
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            let fbl = r1.Params.FirstBurstLength
            Assert.True(( fbl = Constants.NEGOPARAM_MIN_FirstBurstLength ))

            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize  // Media block size must be 512 or 4096

            // SCSI Write
            let sendData = PooledBuffer.Empty
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB sendData 0u

            // SCSI Data-Out PDU
            let sendData2 = PooledBuffer.RentAndInit ( int accessLength )
            let datasn = datasn_me.zero
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 g_DefTTT datasn 0u sendData2
            sendData2.Return()

            // SCSI Response
            // The Haruka does not validate the length of the unsolicited data by FirstBurstLength.
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Test case where FirstBurstLength value is the maximum.
    // When not to send unsolicited data.
    [<Fact>]
    member _.LoginNego_FirstBurstLength_004() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = false;
                    MaxBurstLength = Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength;
                    FirstBurstLength = Constants.NEGOPARAM_MAX_FirstBurstLength;    // 16777215
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            let fbl = r1.Params.FirstBurstLength
            Assert.True(( fbl = Constants.NEGOPARAM_MAX_FirstBurstLength ))

            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize  // Media block size must be 512 or 4096

            // SCSI Write
            let sendData = PooledBuffer.Empty
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB sendData 0u

            // R2T
            let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( rpdu2.InitiatorTaskTag = itt ))
            Assert.True(( rpdu2.R2TSN = datasn_me.zero ))
            Assert.True(( rpdu2.BufferOffset = 0u ))
            Assert.True(( rpdu2.DesiredDataTransferLength = accessLength ))

            // SCSI Data-Out
            let sendData2 = PooledBuffer.RentAndInit ( int accessLength )
            let datasn = datasn_me.zero
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu2.TargetTransferTag datasn rpdu2.BufferOffset sendData2
            sendData2.Return()

            // SCSI Response
            // The Haruka does not validate the length of the unsolicited data by FirstBurstLength.
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // Test case where FirstBurstLength value is the maximum.
    // Sending all data as unsolicited data.
    [<Fact>]
    member _.LoginNego_FirstBurstLength_005() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    InitialR2T = false;
                    MaxBurstLength = Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength;
                    FirstBurstLength = Constants.NEGOPARAM_MAX_FirstBurstLength;    // 16777215
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            let fbl = r1.Params.FirstBurstLength
            Assert.True(( fbl = Constants.NEGOPARAM_MAX_FirstBurstLength ))

            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize  // Media block size must be 512 or 4096

            // SCSI Write
            let sendData = PooledBuffer.RentAndInit 512
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB sendData 0u

            // SCSI Data-Out PDU
            let sendData2 = PooledBuffer.RentAndInit ( int accessLength - 512 )
            let datasn = datasn_me.zero
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 g_DefTTT datasn 512u sendData2
            sendData2.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    // When FirstBurstLength is limited by target requirements
    [<Fact>]
    member _.LoginNego_FirstBurstLength_006() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2-1";
                    InitialR2T = false;
                    FirstBurstLength = Constants.NEGOPARAM_MAX_FirstBurstLength;    // 16777215
            }
            let connParam1 = {
                m_defaultConnParam with
                    PortNo = m_TDx_iSCSIPortNo.[0]; // target device 1
                    CID = g_CID0;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 connParam1
            let fbl = r1.Params.FirstBurstLength
            Assert.True(( fbl = Constants.NEGOPARAM_MIN_FirstBurstLength ))     // 512

            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize  // Media block size must be 512 or 4096

            // SCSI Write
            let sendData = PooledBuffer.Empty
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false false false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB sendData 0u

            // SCSI Data-Out PDU
            // *** Haruka does not check MaxBurstLength and FirstBurstLength limits ***
            let sendData2 = PooledBuffer.RentAndInit 512
            for i = 0 to 7 do
                let datasn = datasn_me.fromPrim ( uint i )
                let offset = 512u * uint i
                let fFlag = i >= 7
                do! r1.SendSCSIDataOutPDU g_CID0 fFlag itt g_LUN1 g_DefTTT datasn offset sendData2
            sendData2.Return()

            // SCSI Response
            let! rpdu3 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu3.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_DefaultTime2Wait_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    DefaultTime2Wait = 1us; // less than target value.
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.True(( r1.Params.DefaultTime2Wait = 1us ))

            // Haruka does not check DefaultTime2Wait value.
            // Regardless of the negotiated result, the behavior remains the same.

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_DefaultTime2Wait_002() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    DefaultTime2Wait = 3us; // greater than target value.
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.True(( r1.Params.DefaultTime2Wait = 2us ))

            // Haruka does not check DefaultTime2Wait value.
            // Regardless of the negotiated result, the behavior remains the same.

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_DataPDUInOrder_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    DataPDUInOrder = true;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.True(( r1.Params.DataPDUInOrder ))

            // Haruka does not check DataPDUInOrder value.
            // Regardless of the negotiated result, the behavior remains the same.

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_DataPDUInOrder_002() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    DataPDUInOrder = false;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.False(( r1.Params.DataPDUInOrder ))

            // Haruka does not check DataPDUInOrder value.
            // Regardless of the negotiated result, the behavior remains the same.

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_DataSequenceInOrder_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    DataSequenceInOrder = true;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.True(( r1.Params.DataSequenceInOrder ))

            // Haruka does not check DataPDUInOrder value.
            // Regardless of the negotiated result, the behavior remains the same.

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_DataSequenceInOrder_0021() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    DataSequenceInOrder = false;
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.False(( r1.Params.DataSequenceInOrder ))

            // Haruka does not check DataPDUInOrder value.
            // Regardless of the negotiated result, the behavior remains the same.

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_ErrorRecoveryLevel_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ErrorRecoveryLevel = 0uy;   // Test case where the initiator requests ErrorRecoveryLevel 0.
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.True(( r1.Params.ErrorRecoveryLevel = 0uy ))

            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize  // Media block size must be 512 or 4096

            // SCSI Write
            let sendData = PooledBuffer.Empty
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB sendData 0u

            // Receive R2T
            let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0

            // SCSI Data-Out PDU ( 0 - 2047, F=true )
            let sendData2 = PooledBuffer.RentAndInit ( int accessLength - 2048 )
            let datasn = datasn_me.zero
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu2.TargetTransferTag datasn 0u sendData2
            sendData2.Return()

            try
                let! _ = r1.Receive g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    [<Fact>]
    member _.LoginNego_ErrorRecoveryLevel_002() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ErrorRecoveryLevel = 2uy;   // Test case where the initiator requests ErrorRecoveryLevel 2.
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            Assert.True(( r1.Params.ErrorRecoveryLevel = 1uy )) // Target requires ErrorRecoveryLevel 1.

            let accessLength = 4096u
            let accessBlockCount = accessLength / m_MediaBlockSize  // Media block size must be 512 or 4096

            // SCSI Write
            let sendData = PooledBuffer.Empty
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB sendData 0u

            // Receive R2T
            let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( rpdu2.InitiatorTaskTag = itt ))
            Assert.True(( rpdu2.R2TSN = datasn_me.zero ))
            Assert.True(( rpdu2.BufferOffset = 0u ))
            Assert.True(( rpdu2.DesiredDataTransferLength = 4096u ))

            // SCSI Data-Out PDU ( 0 - 2047, F=true )
            let sendData2 = PooledBuffer.RentAndInit ( int accessLength - 2048 )
            let datasn = datasn_me.zero
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu2.TargetTransferTag datasn 0u sendData2
            sendData2.Return()

            // Receive recovery R2T
            let! rpdu3 = r1.ReceiveSpecific<R2TPDU> g_CID0
            Assert.True(( rpdu3.InitiatorTaskTag = itt ))
            Assert.True(( rpdu3.R2TSN = datasn_me.fromPrim 1u ))
            Assert.True(( rpdu3.BufferOffset = 2048u ))
            Assert.True(( rpdu3.DesiredDataTransferLength = 2048u ))

            // SCSI Data-Out PDU ( 2048 - 4095, F=true )
            let sendData3 = PooledBuffer.RentAndInit ( int accessLength - 2048 )
            let datasn = datasn_me.fromPrim 1u
            do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu3.TargetTransferTag datasn 2048u sendData3
            sendData3.Return()

            // Receive SCSI Response
            let! rpdu4 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu4.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_MaxOutstandingR2T_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2-1";
                    InitialR2T = true;
                    MaxBurstLength = Constants.NEGOPARAM_MIN_MaxBurstLength;        // 512
                    FirstBurstLength = Constants.NEGOPARAM_MIN_FirstBurstLength;    // 512
                    MaxOutstandingR2T = 1us;    // minimum, Target requires 16
            }
            let connParam1 = {
                m_defaultConnParam with
                    PortNo = m_TDx_iSCSIPortNo.[0]; // connect to TargetDevice 1
            }

            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 connParam1
            Assert.True(( r1.Params.MaxOutstandingR2T = 1us ))

            let accessLength = 8192u    // expected R2T PDU count is 16.
            let accessBlockCount = accessLength / m_MediaBlockSize  // Media block size must be 512 or 4096

            // SCSI Write
            let sendData = PooledBuffer.Empty
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB sendData 0u

            for i= 0 to 15 do
                // Receive R2T PDU
                let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
                Assert.True(( rpdu2.InitiatorTaskTag = itt ))
                Assert.True(( rpdu2.R2TSN = datasn_me.fromPrim ( uint i ) ))
                Assert.True(( rpdu2.BufferOffset = 512u * ( uint i ) ))
                Assert.True(( rpdu2.DesiredDataTransferLength = 512u ))

                // Immidiate NOP-Out
                let! _ = r1.SendNOPOutPDU g_CID0 true g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0

                // SCSI Data-Out PDU
                let sendData2 = PooledBuffer.RentAndInit 512
                let datasn = datasn_me.fromPrim ( uint i )
                do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 rpdu2.TargetTransferTag datasn rpdu2.BufferOffset sendData2
                sendData2.Return()

            // Receive SCSI Response
            let! rpdu4 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu4.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }

    [<Fact>]
    member _.LoginNego_MaxOutstandingR2T_002() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2-1";
                    InitialR2T = true;
                    MaxBurstLength = Constants.NEGOPARAM_MIN_MaxBurstLength;        // 512
                    FirstBurstLength = Constants.NEGOPARAM_MIN_FirstBurstLength;    // 512
                    MaxOutstandingR2T = 32us;    // Target requires 16
            }
            let connParam1 = {
                m_defaultConnParam with
                    PortNo = m_TDx_iSCSIPortNo.[0]; // connect to TargetDevice 1
            }

            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 connParam1
            Assert.True(( r1.Params.MaxOutstandingR2T = 16us ))

            let accessLength = 8192u    // expected R2T PDU count is 16.
            let accessBlockCount = accessLength / m_MediaBlockSize  // Media block size must be 512 or 4096

            // SCSI Write
            let sendData = PooledBuffer.Empty
            let writeCDB = scsiWrite10CDB ( uint16 accessBlockCount )
            let! itt, _ = r1.SendSCSICommandPDU g_CID0 false true false true TaskATTRCd.SIMPLE_TASK g_LUN1 accessLength writeCDB sendData 0u

            // Receive first R2T PDU
            let vR2TPDU = Array.zeroCreate<R2TPDU> 16
            let! rpdu2 = r1.ReceiveSpecific<R2TPDU> g_CID0
            vR2TPDU.[0] <- rpdu2
            Assert.True(( rpdu2.InitiatorTaskTag = itt ))
            Assert.True(( rpdu2.R2TSN = datasn_me.zero ))
            Assert.True(( rpdu2.BufferOffset = 0u ))
            Assert.True(( rpdu2.DesiredDataTransferLength = 512u ))

            // Immidiate NOP-Out
            let! _ = r1.SendNOPOutPDU g_CID0 true g_LUN1 g_DefTTT PooledBuffer.Empty

            // receive following R2T PDUs
            for i= 1 to 15 do
                let! rpdu3 = r1.ReceiveSpecific<R2TPDU> g_CID0
                vR2TPDU.[i] <- rpdu3
                Assert.True(( rpdu3.InitiatorTaskTag = itt ))
                Assert.True(( rpdu3.R2TSN = datasn_me.fromPrim ( uint i ) ))
                Assert.True(( rpdu3.BufferOffset = 512u * ( uint i ) ))
                Assert.True(( rpdu3.DesiredDataTransferLength = 512u ))

            // Nop-In PDU, Response for Immidiate NOP-Out
            let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0

            // send SCSI Data-Out PDUs
            for i= 0 to 15 do
                let sendData2 = PooledBuffer.RentAndInit 512
                let datasn = datasn_me.fromPrim ( uint i )
                do! r1.SendSCSIDataOutPDU g_CID0 true itt g_LUN1 vR2TPDU.[i].TargetTransferTag datasn vR2TPDU.[i].BufferOffset sendData2
                sendData2.Return()

            // Receive SCSI Response
            let! rpdu4 = r1.ReceiveSpecific<SCSIResponsePDU> g_CID0
            Assert.True(( rpdu4.Status = ScsiCmdStatCd.GOOD ))

            // logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 false LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }
