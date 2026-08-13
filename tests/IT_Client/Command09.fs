//=============================================================================
// Haruka Software Storage.
// Command09.fs : Test cases for client commands.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.IT.Client

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Threading.Tasks
open System.IO
open System.Net

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

//=============================================================================
// Class implementation

[<CollectionDefinition( "Command09" )>]
type Command09_Fixture() =

    let m_ControllPortNo = GlbFunc.nextTcpPortNo()
    let m_iSCSIPortNo1 = GlbFunc.nextTcpPortNo()
    let m_iSCSIPortNo2 = GlbFunc.nextTcpPortNo()
    let m_MediaSize = 65536u
    let m_MediaBlockSize = 512      // 4096 or 512 bytes

    let m_WorkPath =
        let tempPath = Path.GetTempPath()
        Functions.AppendPathName tempPath ( Guid.NewGuid().ToString( "N" ) )

    let m_Controller =
        ControllerFunc.InitializeConfigDir m_WorkPath m_ControllPortNo
        ControllerFunc.StartController m_WorkPath

    let m_Client =
        let p = ClientProc( "::1", m_ControllPortNo, m_WorkPath )
        p.RunCommand "create" "Created" "CR> "
        p.RunCommand "select 0" "" "TD> "
        p.RunCommand "set loglevel VERBOSE" "" "TD> "
        p.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo1 ) "Created" "TD> "
        p.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo2 ) "Created" "TD> "

        // Create Target Group 1
        p.RunCommand "create targetgroup" "Created" "TD> "
        p.RunCommand "select 2" "" "TG> "
        p.RunCommand "set ID TG_00000001" "" "TG> "
        p.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        p.RunCommand "select 0" "" "T > "
        p.RunCommand "create /l 1" "Created" "T > "
        p.RunCommand "select 0" "" "LU> "
        p.RunCommand ( sprintf "create membuffer %d" m_MediaSize ) "Created" "LU> "
        p.RunCommand "select 0" "" "MD> "
        p.RunCommand ( sprintf "set BlockSize %d" m_MediaBlockSize ) "" "MD> "
        p.RunCommand "unselect" "" "LU> "
        p.RunCommand "unselect" "" "T > "
        p.RunCommand "unselect" "" "TG> "
        p.RunCommand "unselect" "" "TD> "

        // Create Target Group 2
        p.RunCommand "create targetgroup" "Created" "TD> "
        p.RunCommand "select 3" "" "TG> "

        p.RunCommand "set ID TG_00000002" "" "TG> "
        p.RunCommand "create /n iqn.2020-05.example.com:target2" "Created" "TG> "
        p.RunCommand "select 0" "" "T > "
        p.RunCommand "create /l 2" "Created" "T > "
        p.RunCommand "select 0" "" "LU> "
        p.RunCommand ( sprintf "create membuffer %d" m_MediaSize ) "Created" "LU> "
        p.RunCommand "select 0" "" "MD> "
        p.RunCommand ( sprintf "set BlockSize %d" m_MediaBlockSize ) "" "MD> "
        p.RunCommand "unselect" "" "LU> "
        p.RunCommand "unselect" "" "T > "
        p.RunCommand "unselect" "" "TG> "

        p.RunCommand "create /n iqn.2020-05.example.com:target3" "Created" "TG> "
        p.RunCommand "select 1" "" "T > "
        p.RunCommand "create /l 3" "Created" "T > "
        p.RunCommand "select 0" "" "LU> "
        p.RunCommand ( sprintf "create membuffer %d" m_MediaSize ) "Created" "LU> "
        p.RunCommand "select 0" "" "MD> "
        p.RunCommand ( sprintf "set BlockSize %d" m_MediaBlockSize ) "" "MD> "
        p.RunCommand "unselect" "" "LU> "
        p.RunCommand "unselect" "" "T > "
        p.RunCommand "unselect" "" "TG> "

        p.RunCommand "unselect" "" "TD> "
        p.RunCommand "unselect" "" "CR> "

        // publish and start TD
        p.RunCommand "validate" "All configurations are vlidated" "CR> "
        p.RunCommand "publish" "All configurations are uploaded to the controller" "CR> "
        p

    interface IDisposable with
        member _.Dispose (): unit =
            ()
    interface ICollectionFixture<Command09_Fixture>

    member _.ControllerProc = m_Controller
    member _.ClientProc = m_Client
    member _.ControllPortNo = m_ControllPortNo
    member _.WorkPath = m_WorkPath
    member _.iSCSIPortNo1 = m_iSCSIPortNo1
    member _.iSCSIPortNo2 = m_iSCSIPortNo2
    member _.MediaSize = m_MediaSize
    member _.MediaBlockSize = m_MediaBlockSize

[<Collection( "Command09" )>]
type Command09( fx : Command09_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let m_WorkPath = fx.WorkPath
    let m_ControllPortNo = fx.ControllPortNo
    let m_Client = fx.ClientProc
    let iSCSIPortNo1 = fx.iSCSIPortNo1
    let iSCSIPortNo2 = fx.iSCSIPortNo2
    let g_CID0 = cid_me.zero
    let g_LUN0 = lun_me.zero
    let g_LUN1 = lun_me.fromPrim 1UL
    let g_LUN2 = lun_me.fromPrim 2UL
    let g_LUN3 = lun_me.fromPrim 3UL
    let m_MediaSize = fx.MediaSize
    let m_MediaBlockSize = fx.MediaBlockSize
    let m_MediaBlockSizeBS = if m_MediaBlockSize = 512 then Blocksize.BS_512 else Blocksize.BS_4096

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
        MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength;
        FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength;
        DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait;
        DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain;
        MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
        DataPDUInOrder = false;
        DataSequenceInOrder = false;
        ErrorRecoveryLevel = 1uy;
        TaskReporting = TaskReportingType.TR_ResponseFence;
    }

    // default connection parameters
    let m_defaultConnParam = {
        PortNo = iSCSIPortNo1;
        CID = g_CID0;
        Initiator_UserName = "";
        Initiator_Password = "";
        Target_UserName = "";
        Target_Password = "";
        HeaderDigest = DigestType.DST_CRC32C;
        DataDigest = DigestType.DST_CRC32C;
        MaxRecvDataSegmentLength_I = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
        MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
    }

    // Check session counts
    let CheckSessionCount ( expcnt : int32 ) ( expPrompt : string ) =
        let mutable loopcnt = 0
        while loopcnt < 10 do
            Thread.Sleep 10
            let sesscnt =
                m_Client.RunCommandGetResp "sessions" expPrompt
                |> Array.filter _.Contains( "Session(" )
                |> Array.length
            if sesscnt = expcnt then
                loopcnt <- 99
            else
                loopcnt <- loopcnt + 1
        Assert.StrictEqual( 99, loopcnt )

    // Check connection counts
    let CheckConnectionCount ( expcnt : int32 ) ( expPrompt : string ) =
        let mutable loopcnt = 0
        while loopcnt < 10 do
            Thread.Sleep 10
            let sesscnt =
                m_Client.RunCommandGetResp "connections" expPrompt
                |> Array.filter _.Contains( "Connection(" )
                |> Array.length
            if sesscnt = expcnt then
                loopcnt <- 99
            else
                loopcnt <- loopcnt + 1
        Assert.StrictEqual( 99, loopcnt )


    ///////////////////////////////////////////////////////////////////////////
    // Test cases


    [<Fact>]
    member _.InitiMedia_PlainFile_002 () =
        let fname = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() )
        File.WriteAllBytes( fname, [| 0uy |] )
        Assert.True( File.Exists fname )

        let v = m_Client.RunCommandGetResp "imstatus" "CR> "
        Assert.Empty v

        m_Client.RunCommand ( sprintf "initmedia plainfile %s 65536" fname ) "Started" "CR> "

        let mutable flg = true
        while flg do
            Thread.Sleep 10
            let v = m_Client.RunCommandGetResp "imstatus" "CR> "
            if v.Length > 0 then
                flg <- v.[0].Contains "Failed" |> not

        Assert.True( File.Exists fname )
        let fdata = File.ReadAllBytes fname
        Assert.StrictEqual( 1, fdata.Length )
        File.Delete fname

    [<Fact>]
    member _.Sessions_TargetDevice_001 () =
        task {
            // Start target device
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "

            // connect to target 1
            let! r1 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target1" } m_defaultConnParam
            CheckSessionCount 1 "TD> "

            // connect to target 2
            let! r2 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target2" } m_defaultConnParam
            CheckSessionCount 2 "TD> "

            // connect to target 3
            let! r3 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target3" } m_defaultConnParam
            CheckSessionCount 3 "TD> "

            // Disconnect r2
            do! r2.Close()
            CheckSessionCount 2 "TD> "

            // Disconnect r3
            do! r3.Close()
            CheckSessionCount 1 "TD> "

            // Disconnect r1
            do! r1.Close()
            CheckSessionCount 0 "TD> "

            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    [<Fact>]
    member _.Sessions_TargetGroup_001 () =
        task {
            // Start target device
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000002" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "

            // connect to target 1
            let! r1 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target1" } m_defaultConnParam
            CheckSessionCount 0 "TG> "

            // connect to target 2
            let! r2 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target2" } m_defaultConnParam
            CheckSessionCount 1 "TG> "

            // connect to target 3
            let! r3 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target3" } m_defaultConnParam
            CheckSessionCount 2 "TG> "

            // Disconnect r2
            do! r2.Close()
            CheckSessionCount 1 "TG> "

            // Disconnect r3
            do! r3.Close()
            CheckSessionCount 0 "TG> "

            // Disconnect r1
            do! r1.Close()
            CheckSessionCount 0 "TG> "

            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    [<Fact>]
    member _.Sessions_Target_001 () =
        task {
            // Start target device
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000002" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
            let tgidx = m_Client.GetIndexNumber "target2" "TG> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "T > "

            // connect to target 1
            let! r1 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target1" } m_defaultConnParam
            CheckSessionCount 0 "T > "

            // connect to target 2
            let! r2 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target2" } m_defaultConnParam
            CheckSessionCount 1 "T > "

            // connect to target 3
            let! r3 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target3" } m_defaultConnParam
            CheckSessionCount 1 "T > "

            // Disconnect r3
            do! r3.Close()
            CheckSessionCount 1 "T > "

            // Disconnect r1
            do! r1.Close()
            CheckSessionCount 1 "T > "

            // Disconnect r2
            do! r2.Close()
            CheckSessionCount 0 "T > "

            m_Client.RunCommand "unselect" "" "TG> "
            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    [<Fact>]
    member _.Sessions_TDUnloaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "TD> "

        let r = m_Client.RunCommandGetResp "sessions" "TD> "
        Assert.StartsWith( "Target device process is not running", r.[0] )

        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Sessions_TGUnloaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        // unload target group 1
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unload" "Unloaded" "TG> "

        let r = m_Client.RunCommandGetResp "sessions" "TG> "
        Assert.StartsWith( "Specified target group is unloaded", r.[0] )

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Sessions_TGUnloadedAMod_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        // unload target group 1
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unload" "Unloaded" "TG> "
        m_Client.RunCommand "set ID TG_00000002" "" "TG> "
        m_Client.RunCommand "set NAME wwwwwwwww" "" "TG> "
        m_Client.CheckStatus "wwwwwwwww" "UNLOAD(A-MOD)" "TG> "

        let r = m_Client.RunCommandGetResp "sessions" "TG> "
        Assert.StartsWith( "The target group has been modified", r.[0] )

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Sesskill_001 () =
        task {
            // Start target device
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "

            // connect to target 1
            let! r1 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target1" } m_defaultConnParam
            CheckSessionCount 1 "TD> "

            m_Client.RunCommand ( sprintf "sesskill %d" r1.TSIH ) "Session terminated" "TD> "
            CheckSessionCount 0 "TD> "

            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    [<Fact>]
    member _.Sesskill_002 () =
        task {
            // Start target device
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "

            m_Client.RunCommand "sesskill 999" "Unexpected request error" "TD> "

            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    [<Fact>]
    member _.Sesskill_TDUnloaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "TD> "

        let r = m_Client.RunCommandGetResp "sesskill 999" "TD> "
        Assert.StartsWith( "Target device process is not running", r.[0] )

        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Connections_TargetDevice_001 () =
        task {
            // Start target device
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "

            // connect to target 1
            let! r1 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target1" } m_defaultConnParam
            CheckConnectionCount 1 "TD> "

            // connect to target 2
            let! r2 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target2" } m_defaultConnParam
            CheckConnectionCount 2 "TD> "

            // connect to target 3
            let! r3 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target3" } m_defaultConnParam
            CheckConnectionCount 3 "TD> "

            // Disconnect r2
            do! r2.Close()
            CheckConnectionCount 2 "TD> "

            // Disconnect r3
            do! r3.Close()
            CheckConnectionCount 1 "TD> "

            // Disconnect r1
            do! r1.Close()
            CheckConnectionCount 0 "TD> "

            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    [<Fact>]
    member _.Connections_NetworkPortal_001 () =
        task {
            // Start target device
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            let npidx = m_Client.GetIndexNumber ( sprintf "%d" iSCSIPortNo1 ) "TD> "
            m_Client.RunCommand ( sprintf "select %d" npidx ) "" "NP> "

            // Connect via np1.
            let! r1 = SCSI_Initiator.Create m_defaultSessParam { m_defaultConnParam with PortNo = iSCSIPortNo1 }
            CheckConnectionCount 1 "NP> "

            // Connect via np2.
            let! r2 = SCSI_Initiator.Create m_defaultSessParam { m_defaultConnParam with PortNo = iSCSIPortNo2 }
            CheckConnectionCount 1 "NP> "

            // Connect via np1.
            let! r3 = SCSI_Initiator.Create m_defaultSessParam { m_defaultConnParam with PortNo = iSCSIPortNo1 }
            CheckConnectionCount 2 "NP> "

            // Disconnect r2
            do! r2.Close()
            CheckConnectionCount 2 "NP> "

            // Disconnect r3
            do! r3.Close()
            CheckConnectionCount 1 "NP> "

            // Disconnect r1
            do! r1.Close()
            CheckConnectionCount 0 "NP> "

            m_Client.RunCommand "kill" "Killed" "NP> "
            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    [<Fact>]
    member _.Connections_TargetGroup_001 () =
        task {
            // Start target device
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000002" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "

            // connect to target 1
            let! r1 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target1" } m_defaultConnParam
            CheckConnectionCount 0 "TG> "

            // connect to target 2
            let! r2 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target2" } m_defaultConnParam
            CheckConnectionCount 1 "TG> "

            // connect to target 3
            let! r3 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target3" } m_defaultConnParam
            CheckConnectionCount 2 "TG> "

            // Disconnect r2
            do! r2.Close()
            CheckConnectionCount 1 "TG> "

            // Disconnect r3
            do! r3.Close()
            CheckConnectionCount 0 "TG> "

            // Disconnect r1
            do! r1.Close()
            CheckConnectionCount 0 "TG> "

            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    [<Fact>]
    member _.Connections_Target_001 () =
        task {
            // Start target device
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000002" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
            let tgidx = m_Client.GetIndexNumber "target2" "TG> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "T > "

            // connect to target 1
            let! r1 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target1" } m_defaultConnParam
            CheckConnectionCount 0 "T > "

            // connect to target 2
            let! r2 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target2" } m_defaultConnParam
            CheckConnectionCount 1 "T > "

            // connect to target 3
            let! r3 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target3" } m_defaultConnParam
            CheckConnectionCount 1 "T > "

            // Disconnect r3
            do! r3.Close()
            CheckConnectionCount 1 "T > "

            // Disconnect r1
            do! r1.Close()
            CheckConnectionCount 1 "T > "

            // Disconnect r2
            do! r2.Close()
            CheckConnectionCount 0 "T > "

            m_Client.RunCommand "unselect" "" "TG> "
            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    [<Fact>]
    member _.Connections_TDUnloaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "TD> "

        let r = m_Client.RunCommandGetResp "Connections" "TD> "
        Assert.StartsWith( "Target device process is not running", r.[0] )

        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.LUStatus_Normal_001 () =
        task {
            // Start target device
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
            let tgidx = m_Client.GetIndexNumber "target1" "TG> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "T > "
            m_Client.RunCommand "select 0" "" "LU> "

            let r = m_Client.RunCommandGetResp "lustatus" "LU> "
            Assert.True( r |> Array.exists ( fun i -> i.StartsWith "ACA : None" ) )
            Assert.True( r |> Array.exists ( fun i -> i.StartsWith "Tasks : None" ) )

            m_Client.RunCommand "unselect" "" "T > "
            m_Client.RunCommand "unselect" "" "TG> "
            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    [<Fact>]
    member _.LUStatus_ACA_001 () =
        task {
            // Start target device
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
            let tgidx = m_Client.GetIndexNumber "target1" "TG> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "T > "
            m_Client.RunCommand "select 0" "" "LU> "

            // connect to target 1
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // establish ACA
            let! itt_read = r1.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 UInt32.MaxValue ) m_MediaBlockSizeBS ( blkcnt_me.ofUInt16 10us ) NACA.T
            let! res_read = r1.WaitSCSIResponse itt_read
            Assert.StrictEqual( ScsiCmdStatCd.CHECK_CONDITION, res_read.Status )

            let r = m_Client.RunCommandGetResp "lustatus" "LU> "
            Assert.True( r |> Array.exists ( fun i -> i.StartsWith "ACA : {" ) )
            Assert.True( r |> Array.exists ( fun i -> i.StartsWith "Tasks : None" ) )

            do! r1.Close()

            m_Client.RunCommand "unselect" "" "T > "
            m_Client.RunCommand "unselect" "" "TG> "
            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }
