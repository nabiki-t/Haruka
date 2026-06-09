//=============================================================================
// Haruka Software Storage.
// Command02.fs : Test cases for client commands.
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

[<CollectionDefinition( "Command02" )>]
type Command02_Fixture() =

    let m_ControllPortNo = GlbFunc.nextTcpPortNo()
    let m_iSCSIPortNo1 = GlbFunc.nextTcpPortNo()
    let m_MediaSize = 65536u
    let m_MediaBlockSizse = 512      // 4096 or 512 bytes

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

        // Create Target Group 1
        p.RunCommand "create targetgroup" "Created" "TD> "
        p.RunCommand "select 1" "" "TG> "
        p.RunCommand "set ID TG_00000001" "" "TG> "
        p.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        p.RunCommand "select 0" "" "T > "
        p.RunCommand "create /l 1" "Created" "T > "
        p.RunCommand "select 0" "" "LU> "
        p.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "LU> "
        p.RunCommand "select 0" "" "MD> "
        p.RunCommand ( sprintf "set BlockSize %d" m_MediaBlockSizse ) "" "MD> "
        p.RunCommand "unselect" "" "LU> "
        p.RunCommand "unselect" "" "T > "
        p.RunCommand "unselect" "" "TG> "
        p.RunCommand "unselect" "" "TD> "

        // Create Target Group 2
        p.RunCommand "create targetgroup" "Created" "TD> "
        p.RunCommand "select 2" "" "TG> "
        p.RunCommand "set ID TG_00000002" "" "TG> "
        p.RunCommand "create /n iqn.2020-05.example.com:target2" "Created" "TG> "
        p.RunCommand "select 0" "" "T > "
        p.RunCommand "create /l 2" "Created" "T > "
        p.RunCommand "select 0" "" "LU> "
        p.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "LU> "
        p.RunCommand "select 0" "" "MD> "
        p.RunCommand ( sprintf "set BlockSize %d" m_MediaBlockSizse ) "" "MD> "
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
    interface ICollectionFixture<Command02_Fixture>

    member _.ControllerProc = m_Controller
    member _.ClientProc = m_Client
    member _.ControllPortNo = m_ControllPortNo
    member _.WorkPath = m_WorkPath
    member _.iSCSIPortNo1 = m_iSCSIPortNo1

[<Collection( "Command02" )>]
type Command02( fx : Command02_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let m_WorkPath = fx.WorkPath
    let m_ControllPortNo = fx.ControllPortNo
    let m_Client = fx.ClientProc
    let iSCSIPortNo1 = fx.iSCSIPortNo1
    let g_CID0 = cid_me.zero

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

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    // Controller settings can be changed at any time.
    [<Fact>]
    member _.Set_Controller_001 () =
        m_Client.RunCommand "set LOGLEVEL VERBOSE" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the Target Device is unloaded, the settings can be changed.
    [<Fact>]
    member _.Set_TargetDevice_Unloaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "set LOGLEVEL INFO" "" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the Target Device is running, the settings cannot be changed.
    [<Fact>]
    member _.Set_TargetDevice_Running_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.RunCommand "set LOGLEVEL INFO" "Unexpected error" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the Target Device is unloaded, the settings can be changed.
    // If the same Target Device ID is specified as that of an operational Target Device,
    // it becomes ambiguous to determine whether or not the Target Device is operational.
    [<Fact>]
    member _.Set_TargetDevice_RunningMod_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "create" "Created" "CR> "
        m_Client.RunCommand "select 1" "" "TD> "
        m_Client.RunCommand "set ID TD_00000001" "" "TD> "
        m_Client.RunCommand "set NAME AAAAA" "" "TD> "
        let stat = m_Client.GetStatus "AAAAA" "TD> "
        Assert.StartsWith( "UNLOAD(R-MOD)", stat )
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the Target Group is activated, the settings cannot be changed.
    [<Fact>]
    member _.Set_TargetGroup_Activated_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d"  tgidx ) "" "TG> "
        let stat = m_Client.GetStatus "Target Group" "TG> "
        Assert.StartsWith( "ACTIVE", stat )
        m_Client.RunCommand "set NAME aaa" "Unexpected error" "TG> "
        m_Client.RunCommand "kill" "Killed" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the Target Group is loaded, the settings cannot be changed.
    [<Fact>]
    member _.Set_TargetGroup_Loaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d"  tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "set NAME aaa" "Unexpected error" "TG> "
        m_Client.RunCommand "kill" "Killed" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the Target Group is unloaded, the settings can be changed.
    [<Fact>]
    member _.Set_TargetGroup_Unloaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d"  tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unload" "Unloaded" "TG> "
        m_Client.RunCommand "set NAME aaa" "" "TG> "
        m_Client.RunCommand "kill" "Killed" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the Target Group is unloaded, the settings can be changed.
    // Even if the Target Group is unexpectedly loaded on the controller side, the client side will recognize it as being in an unloaded state.
    [<Fact>]
    member _.Set_TargetGroup_ActiveMod_001 () =
        let rec loop () =
            Thread.Sleep 10
            let v = m_Client.RunCommandGetResp "status" "TG> "
            if v.Length >= 3 && v.[2].StartsWith "UNLOAD(A-MOD)" then
                ()
            else
                loop()

        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "select %d"  tgidx ) "" "TG> "
            m_Client.RunCommand "inactivate" "Inactivated" "TG> "
            m_Client.RunCommand "unload" "Unloaded" "TG> "
            m_Client.RunCommand "set NAME aaa" "" "TG> "

            // restart target device process
            let sessParam = { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target2"; }
            let! r = SCSI_Initiator.Create sessParam m_defaultConnParam
            let! _ = r.SendTMFRequest_TargetColdReset BitI.F lun_me.zero

            loop ()

            m_Client.RunCommand "set NAME bbb" "" "TG> "
            m_Client.RunCommand "kill" "Killed" "TG> "
            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the Target Group is unloaded, the settings can be changed.
    [<Fact>]
    member _.Set_TargetGroup_ActiveMod_002 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "select %d"  tgidx ) "" "TG> "
            m_Client.RunCommand "inactivate" "Inactivated" "TG> "
            m_Client.RunCommand "unload" "Unloaded" "TG> "
            m_Client.RunCommand "set ID TG_00000002" "" "TG> "

            // If the same ID as an active Target Group is specified, it is indistinguishable from the ActiveMod state.
            let v = m_Client.RunCommandGetResp "status" "TG> "
            Assert.StartsWith( "UNLOAD(A-MOD)", v.[2] )

            m_Client.RunCommand "set ID TG_00000001" "" "TG> "
            m_Client.RunCommand "kill" "Killed" "TG> "
            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the Target Group is unloaded, the settings can be changed.
    // Even if the Target Group is unexpectedly loaded on the controller side, the client side will recognize it as being in an unloaded state.
    [<Fact>]
    member _.Set_TargetGroup_LoadedMod_001 () =
        let rec loop () =
            Thread.Sleep 10
            let v = m_Client.RunCommandGetResp "status" "TG> "
            if v.Length >= 3 && v.[2].StartsWith "UNLOAD(L-MOD)" then
                ()
            else
                loop()

        task {
            m_Client.RunCommand "select 0" "" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "select %d"  tgidx ) "" "TG> "
            m_Client.RunCommand "set ENABLEDATSTART false" "" "TG> "
            m_Client.RunCommand "publish" "All configurations are" "TG> "
            m_Client.RunCommand "start" "Started" "TG> "
            m_Client.RunCommand "unload" "Unloaded" "TG> "
            m_Client.RunCommand "set NAME aaa" "" "TG> "

            // restart target device process
            let sessParam = { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target2"; }
            let! r = SCSI_Initiator.Create sessParam m_defaultConnParam
            let! _ = r.SendTMFRequest_TargetColdReset BitI.F lun_me.zero

            loop ()

            m_Client.RunCommand "set NAME bbb" "" "TG> "
            m_Client.RunCommand "kill" "Killed" "TG> "
            m_Client.RunCommand "set ENABLEDATSTART true" "" "TG> "
            m_Client.RunCommand "publish" "All configurations are" "TG> "
            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the Target Group is unloaded, the settings can be changed.
    [<Fact>]
    member _.Set_TargetGroup_LoadedMod_002 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "

            let tgidx = m_Client.GetIndexNumber "TG_00000002" "TD> "
            m_Client.RunCommand ( sprintf "select %d"  tgidx ) "" "TG> "
            m_Client.RunCommand "inactivate" "Inactivated" "TG> "
            m_Client.RunCommand "unselect" "" "TD> "

            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "select %d"  tgidx ) "" "TG> "
            m_Client.RunCommand "inactivate" "Inactivated" "TG> "
            m_Client.RunCommand "unload" "Unloaded" "TG> "

            m_Client.RunCommand "set ID TG_00000002" "" "TG> "

            // If the same ID as an active Target Group is specified, it is indistinguishable from the ActiveMod state.
            let v = m_Client.RunCommandGetResp "status" "TG> "
            Assert.StartsWith( "UNLOAD(L-MOD)", v.[2] )

            m_Client.RunCommand "set ID TG_00000001" "" "TG> "
            m_Client.RunCommand "kill" "Killed" "TG> "
            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // A TargetDevice can be added if there are no other TargetDevices running.
    [<Fact>]
    member _.Create_TargetDevice_001 () =
        task {
            m_Client.RunCommand "create" "Created" "CR> "
            let v = m_Client.RunCommandGetResp "list" "CR> "
            Assert.StrictEqual( 2, v.Length )
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // A TargetDevice can be added if another TargetDevice is already running.
    [<Fact>]
    member _.Create_TargetDevice_002 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "create" "Created" "CR> "
            let v = m_Client.RunCommandGetResp "list" "CR> "
            Assert.StrictEqual( 2, v.Length )
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // Unloaded target devices can be deleted.
    [<Fact>]
    member _.Delete_TargetDevice_Unloaded_001 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "create" "Created" "CR> "
            m_Client.RunCommand "select 1" "" "TD> "
            let stat = m_Client.GetStatus "TD_00000002" "TD> "
            Assert.StartsWith( "UNLOADED", stat )
            m_Client.RunCommand "delete" "Deleted" "CR> "
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // Unloaded target devices can be deleted.
    [<Fact>]
    member _.Delete_TargetDevice_Unloaded_002 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "create" "Created" "CR> "
            m_Client.RunCommand "delete /i 1" "Deleted" "CR> "
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // Unloaded target devices can be deleted.
    [<Fact>]
    member _.Delete_TargetDevice_UnloadedMod_001 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "create" "Created" "CR> "
            m_Client.RunCommand "select 1" "" "TD> "
            m_Client.RunCommand "set NAME aaaaa" "" "TD> "
            let stat = m_Client.GetStatus "aaaaa" "TD> "
            Assert.StartsWith( "UNLOADED(MOD)", stat )
            m_Client.RunCommand "delete" "Deleted" "CR> "
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // Unloaded target devices can be deleted.
    [<Fact>]
    member _.Delete_TargetDevice_UnloadedRMod_001 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "create" "Created" "CR> "
            m_Client.RunCommand "select 1" "" "TD> "
            m_Client.RunCommand "set NAME aaaaa" "" "TD> "
            m_Client.RunCommand "set ID TD_00000001" "" "TD> "
            let stat = m_Client.GetStatus "aaaaa" "TD> "
            Assert.StartsWith( "UNLOAD(R-MOD)", stat )
            m_Client.RunCommand "delete" "Deleted" "CR> "
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // Running target devices can not be deleted.
    [<Fact>]
    member _.Delete_TargetDevice_Running_001 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            m_Client.RunCommand "delete" "Unexpected error" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the target device is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_TDUnloaded_001 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "select %d"  tgidx ) "" "TG> "
            m_Client.RunCommand "delete" "Deleted" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the target device is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_TDUnloaded_002 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "delete /i %d"  tgidx ) "Deleted" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the target device is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_TDUnloaded_RMod_001 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "create" "Created" "CR> "
            m_Client.RunCommand "select 1" "" "TD> "
            m_Client.RunCommand "set NAME aaaaa" "" "TD> "
            m_Client.RunCommand "set ID TD_00000001" "" "TD> "
            let stat = m_Client.GetStatus "aaaaa" "TD> "
            Assert.StartsWith( "UNLOAD(R-MOD)", stat )
            m_Client.RunCommand "create targetgroup /n bbbbb" "Created" "TD> "
            m_Client.RunCommand "select 0" "" "TG> "
            let stat = m_Client.GetStatus "bbbbb" "TG> "
            Assert.StartsWith( "UNLOADED(MOD)", stat )
            m_Client.RunCommand "delete" "Deleted" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the target device is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_TDUnloaded_Mod_002 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "set NAME ggg" "" "TD> "
            let stat = m_Client.GetStatus "ggg" "TD> "
            Assert.StartsWith( "UNLOADED(MOD)", stat )
            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "delete /i %d"  tgidx ) "Deleted" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the target group is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_Unloaded_Mod_001 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            m_Client.RunCommand "create targetgroup /n TG003" "Created" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG003" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
            let stat = m_Client.GetStatus "TG003" "TG> "
            Assert.StartsWith( "UNLOADED(MOD)", stat )
            m_Client.RunCommand "delete"  "Deleted" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the target group is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_Unloaded_001 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
            m_Client.RunCommand "inactivate"  "Inactivated" "TG> "
            m_Client.RunCommand "unload"  "Unloaded" "TG> "
            let stat = m_Client.GetStatus "TG_00000001" "TG> "
            Assert.StartsWith( "UNLOADED", stat )
            m_Client.RunCommand "delete"  "Deleted" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the target group is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_Unloaded_LMod_001 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "

            // Inactivate Targetgroup 1
            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
            m_Client.RunCommand "inactivate"  "Inactivated" "TG> "
            m_Client.RunCommand "unselect" "" "TD> "

            // Unload Targetgroup 2 and edit ID
            let tgidx = m_Client.GetIndexNumber "TG_00000002" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
            m_Client.RunCommand "inactivate"  "Inactivated" "TG> "
            m_Client.RunCommand "unload"  "Unloaded" "TG> "
            m_Client.RunCommand "set ID TG_00000001"  "" "TG> "
            m_Client.RunCommand "set NAME aaaaa"  "" "TG> "
            let stat = m_Client.GetStatus "aaaaa" "TG> "
            Assert.StartsWith( "UNLOAD(L-MOD)", stat )

            // delete target group 2
            m_Client.RunCommand "delete"  "Deleted" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the target group is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_Unloaded_AMod_001 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "

            // Unload Targetgroup 2 and edit ID
            let tgidx = m_Client.GetIndexNumber "TG_00000002" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
            m_Client.RunCommand "inactivate"  "Inactivated" "TG> "
            m_Client.RunCommand "unload"  "Unloaded" "TG> "
            m_Client.RunCommand "set ID TG_00000001"  "" "TG> "
            m_Client.RunCommand "set NAME aaaaa"  "" "TG> "
            let stat = m_Client.GetStatus "aaaaa" "TG> "
            Assert.StartsWith( "UNLOAD(A-MOD)", stat )

            // delete target group 2
            m_Client.RunCommand "delete"  "Deleted" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the target group is not unloaded, the target group can not be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_Loaded_001 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
            m_Client.RunCommand "inactivate"  "Inactivated" "TG> "
            let stat = m_Client.GetStatus "TG_00000001" "TG> "
            Assert.StartsWith( "LOADED", stat )
            m_Client.RunCommand "delete"  "Unexpected error" "TG> "
            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }

    // If the target group is not unloaded, the target group can not be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_Active_001 () =
        task {
            m_Client.RunCommand "select 0" "" "TD> "
            m_Client.RunCommand "start" "Started" "TD> "
            let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
            m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
            let stat = m_Client.GetStatus "TG_00000001" "TG> "
            Assert.StartsWith( "ACTIVE", stat )
            m_Client.RunCommand "delete"  "Unexpected error" "TG> "
            m_Client.RunCommand "unselect" "" "TD> "
            m_Client.RunCommand "kill" "Killed" "TD> "
            m_Client.RunCommand "unselect" "" "CR> "
            m_Client.RunCommand "reload /y" "" "CR> "
        }
