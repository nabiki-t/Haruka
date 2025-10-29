//=============================================================================
// Haruka Software Storage.
// iSCSI_LoginTest1.fs : Test cases for iSCSI login.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.IT.ISCSI

//=============================================================================
// Import declaration

open System
open System.IO
open System.Text

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.Test

//=============================================================================
// Class implementation

[<CollectionDefinition( "iSCSI_LoginTest1" )>]
type iSCSI_LoginTest1_Fixture() =

    // Add default configurations
    let AddDefaultConf( client : ClientProc ) : int =
        let iscsiPortNo = GlbFunc.nextTcpPortNo()
        client.RunCommand "create" "Created" "CR> "
        client.RunCommand "select 0" "" "TD> "
        client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" iscsiPortNo ) "Created" "TD> "
        client.RunCommand "create targetgroup" "Created" "TD> "
        client.RunCommand "set LogParameters.LogLevel VERBOSE" "" "TD> "
        client.RunCommand "select 1" "" "TG> "

        // target1, LU=1, No auth required, 64KB
        client.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        client.RunCommand "select 0" "" "T > "
        client.RunCommand "set ALIAS target001" "" "T > "
        client.RunCommand "create /l 1" "Created" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand "create debug" "Created" "LU> "
        client.RunCommand "select 0" "" "MD> "
        client.RunCommand "create membuffer /s 65536" "Created" "MD> "
        client.RunCommand "unselect" "" "LU> "
        client.RunCommand "unselect" "" "T > "
        client.RunCommand "unselect" "" "TG> "

        // target2, LU=2, Both auth required, 32KB
        client.RunCommand "create /n iqn.2020-05.example.com:target2" "Created" "TG> "
        client.RunCommand "select 1" "" "T > "
        client.RunCommand "setchap /iu iuser1 /ip ipass1 /tu tuser1 /tp tpass1" "Set CHAP authentication" "T > "
        client.RunCommand "set ALIAS target002" "" "T > "
        client.RunCommand "create /l 2" "Created" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand "create debug" "Created" "LU> "
        client.RunCommand "select 0" "" "MD> "
        client.RunCommand "create membuffer /s 32768" "Created" "MD> "
        client.RunCommand "unselect" "" "LU> "
        client.RunCommand "unselect" "" "T > "
        let rv = client.RunCommandGetResp "values" "T > "
        Assert.True(( rv.Length = 15 ))
        client.RunCommand "unselect" "" "TG> "

        // target3, LU=2, Initiator auth required, 16KB
        client.RunCommand "create /n iqn.2020-05.example.com:target3" "Created" "TG> "
        client.RunCommand "select 2" "" "T > "
        client.RunCommand "set ALIAS target003" "" "T > "
        client.RunCommand "setchap /iu iuser2 /ip ipass2" "Set CHAP authentication" "T > "
        client.RunCommand "create /l 3" "Created" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand "create debug" "Created" "LU> "
        client.RunCommand "select 0" "" "MD> "
        client.RunCommand "create membuffer /s 16384" "Created" "MD> "
        client.RunCommand "unselect" "" "LU> "
        client.RunCommand "unselect" "" "T > "
        client.RunCommand "unselect" "" "TG> "

        client.RunCommand "validate" "All configurations are vlidated" "TG> "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "TG> "
        client.RunCommand "start" "Started" "TG> "
        client.RunCommand "logout" "" "--> "
        iscsiPortNo

    // Start controller and client
    let m_Controller, m_Client, m_iSCSIPortNo =
        let workPath =
            let tempPath = Path.GetTempPath()
            Functions.AppendPathName tempPath ( Guid.NewGuid().ToString( "N" ) )
        let controllPortNo = GlbFunc.nextTcpPortNo()
        let controller, client = TestFunctions.StartHarukaController workPath controllPortNo
        let iscsiPort = AddDefaultConf client
        controller, client, iscsiPort

    interface IDisposable with
        member _.Dispose (): unit =
            m_Client.Kill()

    interface ICollectionFixture<iSCSI_LoginTest1_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.iSCSIPortNo = m_iSCSIPortNo


[<Collection( "iSCSI_LoginTest1" )>]
type iSCSI_LoginTest1( fx : iSCSI_LoginTest1_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_CID1 = cid_me.fromPrim 1us
    let g_CID2 = cid_me.fromPrim 2us
    let g_CID3 = cid_me.fromPrim 3us

    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL
    let g_DefTTT = ttt_me.fromPrim 0xFFFFFFFFu

    let m_iSCSIPortNo = fx.iSCSIPortNo

    // default session parameters
    let m_defaultSessParam = {
        InitiatorName = "iqn.2020-05.example.com:initiator";
        InitiatorAlias = "aaa";
        TargetName = "iqn.2020-05.example.com:target1";
        TargetAlias = "";
        ISID = isid_me.fromPrim 1UL;
        TSIH = tsih_me.zero;
        MaxConnections = 16us;
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
        PortNo = m_iSCSIPortNo;
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

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    // Authentication test
    // Target1 : No auth required
    // Initiator auth : None
    // Target auth : None
    // Result : Login successflly
    [<Fact>]
    member _.NoAuthRequiredTarget_001() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target1"; // No auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "";
                    Initiator_Password = "";
                    Target_UserName = "";
                    Target_Password = "";
            }

            // login
            let! r = iSCSI_Initiator.CreateInitialSession sessParam connParam
            Assert.True(( r.Params.TargetAlias = "target001" ))

            // Nop-Out
            let! _ = r.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // Logout
            do! r.CloseSession g_CID0 BitI.F
        }

    // Authentication test
    // Target1 : No auth required
    // Initiator auth : Required
    // Target auth : None
    // Result : Login failed
    [<Fact>]
    member _.NoAuthRequiredTarget_002() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target1"; // No auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "aaa";
                    Initiator_Password = "bbb";
                    Target_UserName = "";
                    Target_Password = "";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target1 : No auth required
    // Initiator auth : Required
    // Target auth : Required
    // Result : Login failed
    [<Fact>]
    member _.NoAuthRequiredTarget_003() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target1"; // No auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "aaa";
                    Initiator_Password = "bbb";
                    Target_UserName = "ccc";
                    Target_Password = "ddd";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : None
    // Target auth : None
    // Result : Login failed
    [<Fact>]
    member _.BothAuthRequiredTarget_001() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "";
                    Initiator_Password = "";
                    Target_UserName = "";
                    Target_Password = "";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : Required(collect)
    // Target auth : None
    // Result : Login failed
    [<Fact>]
    member _.BothAuthRequiredTarget_002() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "iuser1";
                    Initiator_Password = "ipass1";
                    Target_UserName = "";
                    Target_Password = "";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : Required(wrong username)
    // Target auth : None
    // Result : Login failed
    [<Fact>]
    member _.BothAuthRequiredTarget_003() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "aaa";
                    Initiator_Password = "ipass1";
                    Target_UserName = "";
                    Target_Password = "";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : Required(wrong password)
    // Target auth : None
    // Result : Login failed
    [<Fact>]
    member _.BothAuthRequiredTarget_004() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "iuser1";
                    Initiator_Password = "bbb";
                    Target_UserName = "";
                    Target_Password = "";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : Required(collect)
    // Target auth : Required(collect)
    // Result : Login successflly
    [<Fact>]
    member _.BothAuthRequiredTarget_005() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "iuser1";
                    Initiator_Password = "ipass1";
                    Target_UserName = "tuser1";
                    Target_Password = "tpass1";
            }

            // login
            let! r = iSCSI_Initiator.CreateInitialSession sessParam connParam
            Assert.True(( r.Params.TargetAlias = "target002" ))

            // Nop-Out
            let! _ = r.SendNOPOutPDU g_CID0 BitI.F ( lun_me.fromPrim 2UL ) g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // Logout
            do! r.CloseSession g_CID0 BitI.F
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : Required(collect)
    // Target auth : Required(wrong username)
    // Result : Login failed
    [<Fact>]
    member _.BothAuthRequiredTarget_006() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "iuser1";
                    Initiator_Password = "ipass1";
                    Target_UserName = "ccc";
                    Target_Password = "tpass1";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : Required(collect)
    // Target auth : Required(wrong password)
    // Result : Login failed
    [<Fact>]
    member _.BothAuthRequiredTarget_007() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "iuser1";
                    Initiator_Password = "ipass1";
                    Target_UserName = "tuser1";
                    Target_Password = "bbb";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : Required(wrong username)
    // Target auth : Required(collect)
    // Result : Login failed
    [<Fact>]
    member _.BothAuthRequiredTarget_008() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "aaa";
                    Initiator_Password = "ipass1";
                    Target_UserName = "tuser1";
                    Target_Password = "tpass1";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : Required(wrong password)
    // Target auth : Required(collect)
    // Result : Login failed
    [<Fact>]
    member _.BothAuthRequiredTarget_009() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "iuser1";
                    Initiator_Password = "bbb";
                    Target_UserName = "tuser1";
                    Target_Password = "tpass1";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : Required(wrong username)
    // Target auth : Required(wrong username)
    // Result : Login failed
    [<Fact>]
    member _.BothAuthRequiredTarget_010() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "aaa";
                    Initiator_Password = "ipass1";
                    Target_UserName = "ccc";
                    Target_Password = "tpass1";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : Required(wrong username)
    // Target auth : Required(wrong password)
    // Result : Login failed
    [<Fact>]
    member _.BothAuthRequiredTarget_011() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "aaa";
                    Initiator_Password = "ipass1";
                    Target_UserName = "tuser1";
                    Target_Password = "ddd";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : Required(wrong password)
    // Target auth : Required(wrong username)
    // Result : Login failed
    [<Fact>]
    member _.BothAuthRequiredTarget_012() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "iuser1";
                    Initiator_Password = "bbb";
                    Target_UserName = "ccc";
                    Target_Password = "tpass1";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target2 : Both auth required
    // Initiator auth : Required(wrong password)
    // Target auth : Required(wrong password)
    // Result : Login failed
    [<Fact>]
    member _.BothAuthRequiredTarget_013() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2"; // Both auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "iuser1";
                    Initiator_Password = "bbb";
                    Target_UserName = "tuser1";
                    Target_Password = "ddd";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target3 : Initiator auth required
    // Initiator auth : None
    // Target auth : None
    // Result : Login failed
    [<Fact>]
    member _.InitiatorAuthRequiredTarget_001() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target3"; // Initiator auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "";
                    Initiator_Password = "";
                    Target_UserName = "";
                    Target_Password = "";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target3 : Initiator auth required
    // Initiator auth : Required(collect)
    // Target auth : None
    // Result : Login successflly
    [<Fact>]
    member _.InitiatorAuthRequiredTarget_002() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target3"; // Initiator auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "iuser2";
                    Initiator_Password = "ipass2";
                    Target_UserName = "";
                    Target_Password = "";
            }

            // login
            let! r = iSCSI_Initiator.CreateInitialSession sessParam connParam
            Assert.True(( r.Params.TargetAlias = "target003" ))

            // Nop-Out
            let! _ = r.SendNOPOutPDU g_CID0 BitI.F ( lun_me.fromPrim 3UL ) g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // Logout
            do! r.CloseSession g_CID0 BitI.F
        }

    // Authentication test
    // Target3 : Initiator auth required
    // Initiator auth : Required(wrong username)
    // Target auth : None
    // Result : Login failed
    [<Fact>]
    member _.InitiatorAuthRequiredTarget_003() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target3"; // Initiator auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "aaa";
                    Initiator_Password = "ipass2";
                    Target_UserName = "";
                    Target_Password = "";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target3 : Initiator auth required
    // Initiator auth : Required(wrong password)
    // Target auth : None
    // Result : Login failed
    [<Fact>]
    member _.InitiatorAuthRequiredTarget_004() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target3"; // Initiator auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "iuser2";
                    Initiator_Password = "bbb";
                    Target_UserName = "";
                    Target_Password = "";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target3 : Initiator auth required
    // Initiator auth : Required(collect)
    // Target auth : Required
    // Result : Login failed
    [<Fact>]
    member _.InitiatorAuthRequiredTarget_005() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target3"; // Initiator auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "iuser2";
                    Initiator_Password = "ipass2";
                    Target_UserName = "ccc";
                    Target_Password = "ddd";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target3 : Initiator auth required
    // Initiator auth : Required(wrong username)
    // Target auth : Required
    // Result : Login failed
    [<Fact>]
    member _.InitiatorAuthRequiredTarget_006() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target3"; // Initiator auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "aaa";
                    Initiator_Password = "ipass2";
                    Target_UserName = "ccc";
                    Target_Password = "ddd";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Authentication test
    // Target3 : Initiator auth required
    // Initiator auth : Required(wrong password)
    // Target auth : Required
    // Result : Login failed
    [<Fact>]
    member _.InitiatorAuthRequiredTarget_007() =
        task {
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target3"; // Initiator auth required
            }
            let connParam = {
                m_defaultConnParam with
                    Initiator_UserName = "iuser2";
                    Initiator_Password = "bbb";
                    Target_UserName = "ccc";
                    Target_Password = "ddd";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam connParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()
        }

    // Logout for the session by last one connection.
    // After logging out, instruct the session to be reconstructed.
    // Result : Login successful as new session
    [<Fact>]
    member _.Logout_SessClose_LastOne_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with         // create new session
                    ISID = GlbFunc.newISID();
            }

            // login
            let! r1 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam1 m_defaultConnParam cmdsn_me.zero

            // Logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // login( Session reconstruction, reuse ISID, TSIH=0 )
            let! r2 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam1 { m_defaultConnParam with CID = g_CID1 } cmdsn_me.zero

            // Nop-Out
            let! _ = r2.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r2.ReceiveSpecific<NOPInPDU> g_CID1
            rpdu1.PingData.Return()

            // Logout
            do! r2.CloseSession g_CID1 BitI.F
        }

    // Logout for the session by last one connection.
    // After logging out, instruct adding connection to same session.
    // Result : Failed
    [<Fact>]
    member _.Logout_SessClose_LastOne_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))
            r1.RemoveConnectionEntry g_CID0 |> ignore

            // login( Add connection to same session, reuse ISID nad TSIH )
            try
                do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    // Log out of a session while other connections exist.
    // After logging out, instruct the session to be reconstructed.
    // Result : Login successful as new session
    [<Fact>]
    member _.Logout_SessClose_NotLastOne_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with         // create new session
                    ISID = GlbFunc.newISID();
            }

            // login
            let connParam1_1 = { m_defaultConnParam with CID = g_CID0; }
            let! r1 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam1 connParam1_1 cmdsn_me.zero

            // Add connection to existing session
            let connParam1_2 = { m_defaultConnParam with CID = g_CID1; }
            do! r1.AddConnection connParam1_2

            // Logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID0

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID1 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID1

            // login( Session reconstruction, reuse ISID, TSIH=0 )
            let connParam2 = { m_defaultConnParam with CID = g_CID2; }
            let! r3 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam1 connParam2 cmdsn_me.zero

            // Nop-Out
            let! _ = r3.SendNOPOutPDU g_CID2 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu4 = r3.ReceiveSpecific<NOPInPDU> g_CID2
            rpdu4.PingData.Return()

            // Logout
            do! r3.CloseSession g_CID2 BitI.F
        }

    // Log out of a session while other connections exist.
    // After logging out, instruct adding connection to same session.
    // Result : Failed
    [<Fact>]
    member _.Logout_SessClose_NotLastOne_002() =
        task {
            // Login and add a connection to existing session.
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_SESS g_CID0
            let! rpdu1 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu1.Response = LogoutResCd.SUCCESS ))

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID0

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID1 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID1

            // login( Add connection to same session )
            try
                do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    // Log out of a session while other connections exist.
    // After logging out, instruct the session to be reconstructed.
    // Result : Login successful as new session
    [<Fact>]
    member _.Logout_SessClose_NotLastOne_LogoutSibling_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with         // create new session
                    ISID = GlbFunc.newISID();
            }

            // login
            let! r1 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam1 m_defaultConnParam cmdsn_me.zero

            // Add connection to existing session
            let connParam1_2 = { m_defaultConnParam with CID = g_CID1; }
            do! r1.AddConnection connParam1_2

            // Log out sibling connections
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_SESS g_CID1
            let! rpdu1 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu1.Response = LogoutResCd.SUCCESS ))

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID0

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID1 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID1

            // login( Session reconstruction, reuse ISID, TSIH=0 )
            let connParam2 = { m_defaultConnParam with CID = g_CID2; }
            let! r3 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam1 connParam2 cmdsn_me.zero

            // Nop-Out
            let! _ = r3.SendNOPOutPDU g_CID2 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu3 = r3.ReceiveSpecific<NOPInPDU> g_CID2
            rpdu3.PingData.Return()

            // Logout
            do! r3.CloseSession g_CID2 BitI.F
        }

    // Log out of a session while other connections exist.
    // After logging out, instruct adding connection to same session.
    // Result : Failed
    [<Fact>]
    member _.Logout_SessClose_NotLastOne_LogoutSibling_002() =
        task {
            // Login and add a connection to existing session.
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1; }

            // Log out sibling connections
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_SESS g_CID1
            let! rpdu1 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu1.Response = LogoutResCd.SUCCESS ))

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID0

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID1 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID1

            // login( Add connection to same session )
            try
                let connParam2 = { m_defaultConnParam with CID = g_CID2; }
                do! r1.AddConnection connParam2
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    // Logout for the connection by last one connection.
    // After logging out, instruct adding connection to same session.
    // Result : Failed
    [<Fact>]
    member _.Logout_ConnClose_LastOne_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID0

            // login( Add connection to same session )
            try
                // Add connection to the session
                let connParam2 = {
                    m_defaultConnParam with
                        CID = g_CID1;
                }
                do! r1.AddConnection connParam2
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    // Logout for the connection by last one connection.
    // After logging out, instruct to rebuild connection.
    // Result : Failed
    [<Fact>]
    member _.Logout_ConnClose_LastOne_002() =
        task {
            let connParam1 = { m_defaultConnParam with CID = g_CID1; }
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam connParam1

            // Logout
            let! _ = r1.SendLogoutRequestPDU g_CID1 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID1
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID1
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID1 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID1

            // login( Add connection to same session )
            try
                // Add connection to the session
                let connParam2 = {
                    m_defaultConnParam with
                        CID = g_CID1;  // same CID
                }
                do! r1.AddConnection connParam2
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    // Log out connection  while other connections exist.
    // After logging out, instruct adding connection the session.
    // Result : Login successful.
    [<Fact>]
    member _.Logout_ConnClose_NotLastOne_001() =
        task {
            // Login and add a connection to existing session.
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID0

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            rpdu1.PingData.Return()

            // Add connection to the session
            let connParam1_3 = {
                m_defaultConnParam with
                    CID = g_CID2;
            }
            do! r1.AddConnection connParam1_3

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID2 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID2
            rpdu1.PingData.Return()

            // Logout( connection 1 )
            let! _ = r1.SendLogoutRequestPDU g_CID1 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID1
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID1
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // Logout( connection 2 )
            let! _ = r1.SendLogoutRequestPDU g_CID2 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID2
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID2
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

        }

    // Log out connection  while other connections exist.
    // After logging out, instruct to rebuild connection.
    // Result : Login successful.
    [<Fact>]
    member _.Logout_ConnClose_NotLastOne_002() =
        task {
            // Login and add a connection to existing session
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID0

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            rpdu1.PingData.Return()

            // Add connection to the session
            let connParam1_3 = {
                m_defaultConnParam with
                    CID = g_CID0;  // same CID as the logouted connection.
            }
            do! r1.AddConnection connParam1_3

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            rpdu1.PingData.Return()

            // Logout( connection 0 )
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // Logout( connection 1 )
            let! _ = r1.SendLogoutRequestPDU g_CID1 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID1
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID1
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

        }

    // Log out connection while other connections exist.
    // After logging out, instruct adding connection the session.
    // Result : Login successful.
    [<Fact>]
    member _.Logout_ConnClose_NotLastOne_LogoutSibling_001() =
        task {
            // Login and add a connection to existing session.
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID1
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID1 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID1

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // Add connection to the session
            let connParam1_3 = {
                m_defaultConnParam with
                    CID = g_CID2;
            }
            do! r1.AddConnection connParam1_3

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID2 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID2
            rpdu1.PingData.Return()

            // Logout( connection 0 )
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // Logout( connection 2 )
            let! _ = r1.SendLogoutRequestPDU g_CID2 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID2
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID2
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

        }

    // Log out connection  while other connections exist.
    // After logging out, instruct to rebuild connection.
    // Result : Login successful.
    [<Fact>]
    member _.Logout_ConnClose_NotLastOne_LogoutSibling_002() =
        task {
            // login and add a connection to existing session.
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Logout
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID1
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // Nop-out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID1 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID1
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
            r1.CloseConnection g_CID1

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // Add connection to the session. same CID as the logouted connection.
            let connParam1_3 = { m_defaultConnParam with CID = g_CID1; }
            do! r1.AddConnection connParam1_3

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            rpdu1.PingData.Return()

            // Logout( connection 0 )
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // Logout( connection 1 )
            let! _ = r1.SendLogoutRequestPDU g_CID1 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID1
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID1
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))
        }

    // Closing a non-existent connection
    [<Fact>]
    member _.Logout_ClosingNonexistentConn_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID1
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.CID_NOT_FOUND ))

            do! r1.CloseSession g_CID0 BitI.F
        }
        
    // Logging into a non-existent session.
    // Result : Login failed.
    [<Fact>]
    member _.SessMgr_LoginToNonExistentSess_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    TSIH = tsih_me.fromPrim 1us;    // other than 0
            }
            try
                let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()

        }

    // Logging into a non-existent session.
    // Result : Login failed.
    [<Fact>]
    member _.SessMgr_LoginToNonExistentSess_002() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
                    TSIH = tsih_me.zero;
            }
            let connParam1 = { m_defaultConnParam with CID = g_CID0; }
            let! r1 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam1 connParam1 cmdsn_me.zero

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // login ( same ISID, new TSIH )
            let sessParam2 = {
                sessParam1 with
                    TSIH = r1.Params.TSIH + ( tsih_me.fromPrim 1us );
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam2 connParam1 cmdsn_me.zero
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()

            // Logout( connection 0 )
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

        }

    // login new session.
    // Result : success.
    [<Fact>]
    member _.SessMgr_LoginNewSess_001() =
        task {
            // login session r1
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // login session r2 ( Notice, ISID is different from r1 )
            let! r2 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out
            let! _ = r2.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu2 = r2.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu2.PingData.Return()

            // Logout( sess r1 )
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu3 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu3.Response = LogoutResCd.SUCCESS ))

            // Logout( sess r2 )
            let! _ = r2.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu3 = r2.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu3.Response = LogoutResCd.SUCCESS ))

        }

    // Reestablishing a session while other connections exist
    // Result : success.
    [<Fact>]
    member _.SessMgr_SessionReEstablishment_001() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
            }
            let connParam1 = { m_defaultConnParam with CID = g_CID0; }
            let! r1 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam1 connParam1 cmdsn_me.zero

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // login ( same ISID with r1 )
            let connParam2 = { m_defaultConnParam with CID = g_CID1; }
            let! r2 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam1 connParam2 cmdsn_me.zero

            // Nop-Out(failed)
            try
                let! _ = r1.SendNOPOutPDU g_CID0 BitI.T g_LUN1 g_DefTTT PooledBuffer.Empty
                let! _ = r1.ReceiveSpecific<NOPInPDU> g_CID0
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()

            // Nop-Out
            let! _ = r2.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu2 = r2.ReceiveSpecific<NOPInPDU> g_CID1
            rpdu2.PingData.Return()

            // Logout( sess r2 )
            let! _ = r2.SendLogoutRequestPDU g_CID1 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID1
            let! rpdu3 = r2.ReceiveSpecific<LogoutResponsePDU> g_CID1
            Assert.True(( rpdu3.Response = LogoutResCd.SUCCESS ))

        }

    // Reestablishing a session when no other connections exist
    // Result : success.
    [<Fact>]
    member _.SessMgr_SessionReEstablishment_002() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
            }
            let! r1 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam1 m_defaultConnParam cmdsn_me.zero

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // Logout( sess r1 )
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu2 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu2.Response = LogoutResCd.SUCCESS ))

            // login ( same ISID with r1, same CID )
            let! r2 = iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sessParam1 m_defaultConnParam cmdsn_me.zero

            // Nop-Out
            let! _ = r2.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu3 = r2.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu3.PingData.Return()

            // Logout( sess r2 )
            let! _ = r2.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu4 = r2.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu4.Response = LogoutResCd.SUCCESS ))
        }

    // Adding a connection to an existing session.
    // Result : success.
    [<Fact>]
    member _.SessMgr_AddConnectionToSession_001() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // Add connection
            let connParam2 = { m_defaultConnParam with CID = g_CID1; }
            do! r1.AddConnection connParam2

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            rpdu2.PingData.Return()

            // Logout( conn0 )
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu3 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu3.Response = LogoutResCd.SUCCESS ))

            // Logout( conn1 )
            let! _ = r1.SendLogoutRequestPDU g_CID1 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID1
            let! rpdu4 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID1
            Assert.True(( rpdu4.Response = LogoutResCd.SUCCESS ))

        }

    // Adding a connection to an existing session.
    // Result : failed.
    [<Fact>]
    member _.SessMgr_AddConnectionToSession_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // Logout( conn0 )
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu3 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu3.Response = LogoutResCd.SUCCESS ))

            // Add connection ( failed )
            try
                do! r1.AddConnection { m_defaultConnParam with CID = g_CID1; }
                Assert.Fail __LINE__
            with
            | :? ConnectionErrorException
            | :? SessionRecoveryException ->
                ()

        }

    // Re-establishing a connection while other connections exist
    // Result : success.
    [<Fact>]
    member _.SessMgr_ConnectionReEstablish_001() =
        task {
            // Login and add a connection to the session.
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out ( conn0 )
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // Nop-Out ( conn1 )
            let! _ = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu2 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            rpdu2.PingData.Return()

            // connection re-establish
            let oldConn = r1.RemoveConnectionEntry g_CID1
            do! r1.AddConnection { m_defaultConnParam with CID = g_CID1 }

            // Nop-Out ( conn0 )
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu3.PingData.Return()

            // Nop-Out ( conn1 )
            let! _ = r1.SendNOPOutPDU g_CID1 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu4 = r1.ReceiveSpecific<NOPInPDU> g_CID1
            rpdu4.PingData.Return()

            // old conn1 is already disconnected.
            try
                let v = Array.zeroCreate<byte> 128
                oldConn.Connection.ReadExactly( v, 0, 128 )
                Assert.Fail __LINE__
            with
            | :? EndOfStreamException ->
                ()

            // Logout( conn0 )
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))

            // Logout( conn1 )
            let! _ = r1.SendLogoutRequestPDU g_CID1 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID1
            let! rpdu6 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID1
            Assert.True(( rpdu6.Response = LogoutResCd.SUCCESS ))

        }
        
    // Re-establishing a connection while other connections not exist
    // Result : success.
    [<Fact>]
    member _.SessMgr_ConnectionReEstablish_002() =
        task {
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // Nop-Out ( conn0 )
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu1 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu1.PingData.Return()

            // connection re-establish
            let oldConn = r1.RemoveConnectionEntry g_CID0
            do! r1.AddConnection m_defaultConnParam

            // Nop-Out ( conn0 )
            let! _ = r1.SendNOPOutPDU g_CID0 BitI.F g_LUN1 g_DefTTT PooledBuffer.Empty
            let! rpdu3 = r1.ReceiveSpecific<NOPInPDU> g_CID0
            rpdu3.PingData.Return()

            // old conn1 is already disconnected.
            try
                let v = Array.zeroCreate<byte> 128
                oldConn.Connection.ReadExactly( v, 0, 128 )
                Assert.Fail __LINE__
            with
            | :? EndOfStreamException ->
                ()

            // Logout( conn0 )
            let! _ = r1.SendLogoutRequestPDU g_CID0 BitI.F LogoutReqReasonCd.CLOSE_CONN g_CID0
            let! rpdu5 = r1.ReceiveSpecific<LogoutResponsePDU> g_CID0
            Assert.True(( rpdu5.Response = LogoutResCd.SUCCESS ))
        }
        
    // A non-existent target name was specified
    // Result : failed.
    [<Fact>]
    member _.Login_InvalidTargetName_002() =
        task {
            let sessParam1 = {
                m_defaultSessParam with
                    TargetName = "aaaaaaaaaaaaaaaaa";
            }
            try
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException ->
                ()
        }

    // A invalid initiator name was specified.
    // Result : failed.
    [<Fact>]
    member _.Login_InvalidInitiatorName_002() =
        task {
            let conn = GlbFunc.ConnectToServer( m_iSCSIPortNo )

            // Send initial login request
            let textReq = [|
                yield! ( Encoding.UTF8.GetBytes "InitiatorName=*****************************" )
                yield '\u0000'B
                yield! ( Encoding.UTF8.GetBytes "TargetName=iqn.2020-05.example.com:target1" )
                yield '\u0000'B
                yield! ( Encoding.UTF8.GetBytes "SessionType=Normal" )
                yield '\u0000'B
            |]
            let loginRequest =
                {
                    T = false;
                    C = false;
                    CSG = LoginReqStateCd.OPERATIONAL;
                    NSG = LoginReqStateCd.OPERATIONAL;
                    VersionMax = 0x00uy;
                    VersionMin = 0x00uy;
                    ISID = isid_me.fromPrim 1UL;
                    TSIH = tsih_me.zero;
                    InitiatorTaskTag = itt_me.fromPrim 0u;
                    CID = g_CID0;
                    CmdSN = cmdsn_me.zero;
                    ExpStatSN = statsn_me.zero;
                    TextRequest = textReq;
                    ByteCount = 0u;
                }
            let mrdsli = 8192u
            let headerDigest = DigestType.DST_None
            let dataDigest = DigestType.DST_None
            let objid = objidx_me.NewID()

            let! _ = PDU.SendPDU( mrdsli, headerDigest, dataDigest, ValueNone, ValueNone, ValueNone, objid, conn, loginRequest )
            try
                let! _ = PDU.Receive( mrdsli, headerDigest, dataDigest, ValueNone, ValueNone, ValueNone, conn, Standpoint.Initiator )
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    // A invalid session type was specified.
    // Result : failed.
    [<Fact>]
    member _.Login_InvalidSessionType_002() =
        task {
            let conn = GlbFunc.ConnectToServer( m_iSCSIPortNo )

            // Send initial login request
            let textReq = [|
                yield! ( Encoding.UTF8.GetBytes "InitiatorName=iqn.2020-05.example.com:initiator" )
                yield '\u0000'B
                yield! ( Encoding.UTF8.GetBytes "TargetName=iqn.2020-05.example.com:target1" )
                yield '\u0000'B
                yield! ( Encoding.UTF8.GetBytes "SessionType=aaaaaaaa" )
                yield '\u0000'B
            |]
            let loginRequest =
                {
                    T = false;
                    C = false;
                    CSG = LoginReqStateCd.OPERATIONAL;
                    NSG = LoginReqStateCd.OPERATIONAL;
                    VersionMax = 0x00uy;
                    VersionMin = 0x00uy;
                    ISID = isid_me.fromPrim 1UL;
                    TSIH = tsih_me.zero;
                    InitiatorTaskTag = itt_me.fromPrim 0u;
                    CID = g_CID0;
                    CmdSN = cmdsn_me.zero;
                    ExpStatSN = statsn_me.zero;
                    TextRequest = textReq;
                    ByteCount = 0u;
                }
            let mrdsli = 8192u
            let headerDigest = DigestType.DST_None
            let dataDigest = DigestType.DST_None
            let objid = objidx_me.NewID()

            let! _ = PDU.SendPDU( mrdsli, headerDigest, dataDigest, ValueNone, ValueNone, ValueNone, objid, conn, loginRequest )
            let! pdu1 = PDU.Receive( mrdsli, headerDigest, dataDigest, ValueNone, ValueNone, ValueNone, conn, Standpoint.Initiator )
            Assert.True(( ( pdu1 :?> LoginResponsePDU ).Status = LoginResStatCd.UNSUPPORT_SESS_TYPE ))
        }

    [<Fact>]
    member _.DiscoverySession_001() =
        task {
            let connParam1 = {
                m_defaultConnParam with
                    CID = g_CID0;
                    PortNo = m_iSCSIPortNo;
            }
            let! r1 = iSCSI_Initiator.QueryTargetNames connParam1 "All"
            Assert.True(( r1.Count = 3 ))

            let v1 = r1.[ "iqn.2020-05.example.com:target1" ]
            Assert.True(( v1.Length = 1 ))
            Assert.True(( v1.[0] = sprintf "[::1]:%d,0" m_iSCSIPortNo ))

            let v2 = r1.[ "iqn.2020-05.example.com:target2" ]
            Assert.True(( v2.Length = 1 ))
            Assert.True(( v2.[0] = sprintf "[::1]:%d,0" m_iSCSIPortNo ))

            let v3 = r1.[ "iqn.2020-05.example.com:target3" ]
            Assert.True(( v3.Length = 1 ))
            Assert.True(( v3.[0] = sprintf "[::1]:%d,0" m_iSCSIPortNo ))
        }

    [<Fact>]
    member _.DiscoverySession_002() =
        task {
            let connParam1 = {
                m_defaultConnParam with
                    CID = g_CID0;
                    PortNo = m_iSCSIPortNo;
            }
            let! r1 = iSCSI_Initiator.QueryTargetNames connParam1 "iqn.2020-05.example.com:target2"
            Assert.True(( r1.Count = 1 ))

            let v2 = r1.[ "iqn.2020-05.example.com:target2" ]
            Assert.True(( v2.Length = 1 ))
            Assert.True(( v2.[0] = sprintf "[::1]:%d,0" m_iSCSIPortNo ))
        }

    [<Fact>]
    member _.DiscoverySession_003() =
        task {
            try
                let connParam1 = {
                    m_defaultConnParam with
                        CID = g_CID0;
                        PortNo = m_iSCSIPortNo;
                }
                let! _ = iSCSI_Initiator.QueryTargetNames connParam1 ""
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    [<Fact>]
    member _.DiscoverySession_004() =
        task {
            let connParam1 = {
                m_defaultConnParam with
                    CID = g_CID0;
                    PortNo = m_iSCSIPortNo;
            }
            let! r1 = iSCSI_Initiator.QueryTargetNames connParam1 "aaaaaaaaaa"
            Assert.True(( r1.Count = 0 ))
        }

    [<Fact>]
    member _.SendTargets_TextRequest_001() =
        task {
            // login for full feature phase
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // send SendTargets text request
            let! result = r1.SendTargetsTextRequest g_CID0 "All"
            Assert.True(( result.Count = 0 ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.SendTargets_TextRequest_002() =
        task {
            // login for full feature phase
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // send SendTargets text request
            let! result = r1.SendTargetsTextRequest g_CID0 "iqn.2020-05.example.com:target2"
            Assert.True(( result.Count = 0 ))
            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.SendTargets_TextRequest_003() =
        task {
            // login for full feature phase
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // send SendTargets text request
            let! result = r1.SendTargetsTextRequest g_CID0 "iqn.2020-05.example.com:target1"
            Assert.True(( result.Count = 1 ))

            let v2 = result.[ "iqn.2020-05.example.com:target1" ]
            Assert.True(( v2.Length = 1 ))
            Assert.True(( v2.[0] = sprintf "[::1]:%d,0" m_iSCSIPortNo ))

            do! r1.CloseSession g_CID0 BitI.F
        }

    [<Fact>]
    member _.SendTargets_TextRequest_004() =
        task {
            // login for full feature phase
            let! r1 = iSCSI_Initiator.CreateInitialSession m_defaultSessParam m_defaultConnParam

            // send SendTargets text request
            let! result = r1.SendTargetsTextRequest g_CID0 ""
            Assert.True(( result.Count = 1 ))

            let v2 = result.[ "iqn.2020-05.example.com:target1" ]
            Assert.True(( v2.Length = 1 ))
            Assert.True(( v2.[0] = sprintf "[::1]:%d,0" m_iSCSIPortNo ))

            do! r1.CloseSession g_CID0 BitI.F
        }
