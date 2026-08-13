//=============================================================================
// Haruka Software Storage.
// Command03.fs : Test cases for client commands.
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

[<CollectionDefinition( "Command03" )>]
type Command03_Fixture() =

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
        p.RunCommand ( sprintf "create membuffer %d" m_MediaSize ) "Created" "LU> "
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
        p.RunCommand ( sprintf "create membuffer %d" m_MediaSize ) "Created" "LU> "
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
    interface ICollectionFixture<Command03_Fixture>

    member _.ControllerProc = m_Controller
    member _.ClientProc = m_Client
    member _.ControllPortNo = m_ControllPortNo
    member _.WorkPath = m_WorkPath
    member _.iSCSIPortNo1 = m_iSCSIPortNo1

[<Collection( "Command03" )>]
type Command03( fx : Command03_Fixture ) =

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

    // Unloaded target devices can be deleted.
    [<Fact>]
    member _.Delete_TargetDevice_Unloaded_001 () =
        // Start existing target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "

        // create new target device
        m_Client.RunCommand "create" "Created" "CR> "
        let v1 = m_Client.RunCommandGetResp "list" "CR> "
        m_Client.RunCommand "select 1" "" "TD> "
        m_Client.CheckStatus "TD_00000002" "UNLOADED(MOD)" "TD> "

        // Delete target device
        m_Client.RunCommand "delete" "Deleted" "CR> "
        let v2 = m_Client.RunCommandGetResp "list" "CR> "
        Assert.StrictEqual( v2.Length + 1, v1.Length )

        // Stop running target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // Unloaded target devices can be deleted.
    [<Fact>]
    member _.Delete_TargetDevice_Unloaded_002 () =
        // Start existing target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "

        // create new target device
        m_Client.RunCommand "create" "Created" "CR> "
        let v1 = m_Client.RunCommandGetResp "list" "CR> "

        // Delete target device
        m_Client.RunCommand "delete /i 1" "Deleted" "CR> "
        let v2 = m_Client.RunCommandGetResp "list" "CR> "
        Assert.StrictEqual( v2.Length + 1, v1.Length )

        // Stop running target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // Unloaded target devices can be deleted.
    [<Fact>]
    member _.Delete_TargetDevice_UnloadedMod_001 () =
        // Start existing target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "

        // create new target device
        m_Client.RunCommand "create" "Created" "CR> "
        let v1 = m_Client.RunCommandGetResp "list" "CR> "
        m_Client.RunCommand "select 1" "" "TD> "
        m_Client.RunCommand "set NAME aaaaa" "" "TD> "
        m_Client.CheckStatus "aaaaa" "UNLOADED(MOD)" "TD> "

        // Delete the target device
        m_Client.RunCommand "delete" "Deleted" "CR> "
        let v2 = m_Client.RunCommandGetResp "list" "CR> "
        Assert.StrictEqual( v2.Length + 1, v1.Length )

        // Stop running target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // Unloaded target devices can be deleted.
    [<Fact>]
    member _.Delete_TargetDevice_UnloadedRMod_001 () =

        // Start existing target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "

        // create new target device
        m_Client.RunCommand "create" "Created" "CR> "
        let v1 = m_Client.RunCommandGetResp "list" "CR> "
        m_Client.RunCommand "select 1" "" "TD> "
        m_Client.RunCommand "set NAME aaaaa" "" "TD> "
        m_Client.RunCommand "set ID TD_00000001" "" "TD> "
        m_Client.CheckStatus "aaaaa" "UNLOAD(R-MOD)" "TD> "

        // Delete the target device
        m_Client.RunCommand "delete" "Deleted" "CR> "
        let v2 = m_Client.RunCommandGetResp "list" "CR> "
        Assert.StrictEqual( v2.Length + 1, v1.Length )

        // Stop running target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // Running target devices can not be deleted.
    [<Fact>]
    member _.Delete_TargetDevice_Running_001 () =
        let v1 = m_Client.RunCommandGetResp "list" "CR> "

        // Start existing target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "

        // try to delete running target device, it failed.
        m_Client.RunCommand "delete" "Unexpected error" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        let v2 = m_Client.RunCommandGetResp "list" "CR> "
        Assert.StrictEqual( v2.Length, v1.Length )

        // stop running target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the target device is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_TDUnloaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "TD> "
        let v1 = m_Client.RunCommandGetResp "list" "TD> "

        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d"  tgidx ) "" "TG> "
        m_Client.RunCommand "delete" "Deleted" "TD> "

        m_Client.CheckStatus "TD_00000001" "UNLOADED" "TD> "
        let v2 = m_Client.RunCommandGetResp "list" "TD> "
        Assert.StrictEqual( v2.Length + 1, v1.Length )

        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the target device is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_TDUnloaded_002 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "TD> "
        let v1 = m_Client.RunCommandGetResp "list" "TD> "

        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "delete /i %d"  tgidx ) "Deleted" "TD> "

        m_Client.CheckStatus "TD_00000001" "UNLOADED" "TD> "
        let v2 = m_Client.RunCommandGetResp "list" "TD> "
        Assert.StrictEqual( v2.Length + 1, v1.Length )

        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the target device is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_TDUnloaded_RMod_001 () =
        // Start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "

        // create new target device
        m_Client.RunCommand "create" "Created" "CR> "
        m_Client.RunCommand "select 1" "" "TD> "
        m_Client.RunCommand "set NAME aaaaa" "" "TD> "
        m_Client.RunCommand "set ID TD_00000001" "" "TD> "
        m_Client.CheckStatus "aaaaa" "UNLOAD(R-MOD)" "TD> "

        // create a target group
        m_Client.RunCommand "create targetgroup /n bbbbb" "Created" "TD> "
        m_Client.RunCommand "select 0" "" "TG> "
        m_Client.CheckStatus "bbbbb" "UNLOADED(MOD)" "TG> "

        // delete target group
        m_Client.RunCommand "delete" "Deleted" "TD> "

        let v2 = m_Client.RunCommandGetResp "list" "TD> "
        Assert.StrictEqual( 1, v2.Length )
        Assert.StartsWith( "There are no child nodes", v2.[0] )

        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the target device is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_TDUnloaded_Mod_002 () =
        // Modify target device config
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "set NAME ggg" "" "TD> "
        m_Client.CheckStatus "ggg" "UNLOADED(MOD)" "TD> "

        let v = m_Client.RunCommandGetResp "list" "TD> "

        // delete target group
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "delete /i %d"  tgidx ) "Deleted" "TD> "

        let v2 = m_Client.RunCommandGetResp "list" "TD> "
        Assert.StrictEqual( v.Length, v2.Length + 1 )

        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the target group is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_Unloaded_Mod_001 () =
        // start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "

        // create new target group
        m_Client.RunCommand "create targetgroup /n TG003" "Created" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG003" "TD> "

        let v = m_Client.RunCommandGetResp "list" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.CheckStatus "TG003" "UNLOADED(MOD)" "TG> "

        // delete target group
        m_Client.RunCommand "delete"  "Deleted" "TD> "

        let v2 = m_Client.RunCommandGetResp "list" "TD> "
        Assert.StrictEqual( v.Length, v2.Length + 1 )

        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the target group is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_Unloaded_001 () =

        // start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "

        let v = m_Client.RunCommandGetResp "list" "TD> "

        // unload target group
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate"  "Inactivated" "TG> "
        m_Client.RunCommand "unload"  "Unloaded" "TG> "
        m_Client.CheckStatus "TG_00000001" "UNLOADED" "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        // Delete unloaded target group
        m_Client.RunCommand "delete"  "Deleted" "TD> "

        let v2 = m_Client.RunCommandGetResp "list" "TD> "
        Assert.StrictEqual( v.Length, v2.Length + 1 )
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the target group is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_Unloaded_LMod_001 () =

        // start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "

        let v = m_Client.RunCommandGetResp "list" "TD> "

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
        m_Client.CheckStatus "aaaaa" "UNLOAD(L-MOD)" "TG> "

        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        // delete target group 2
        m_Client.RunCommand "delete"  "Deleted" "TD> "

        let v2 = m_Client.RunCommandGetResp "list" "TD> "
        Assert.StrictEqual( v.Length, v2.Length + 1 )
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the target group is unloaded, the target group can be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_Unloaded_AMod_001 () =
        // start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "

        let v = m_Client.RunCommandGetResp "list" "TD> "

        // Unload Targetgroup 2 and edit ID
        let tgidx = m_Client.GetIndexNumber "TG_00000002" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate"  "Inactivated" "TG> "
        m_Client.RunCommand "unload"  "Unloaded" "TG> "
        m_Client.RunCommand "set ID TG_00000001"  "" "TG> "
        m_Client.RunCommand "set NAME aaaaa"  "" "TG> "
        m_Client.CheckStatus "aaaaa" "UNLOAD(A-MOD)" "TG> "

        // delete target group 2
        m_Client.RunCommand "delete"  "Deleted" "TD> "

        let v2 = m_Client.RunCommandGetResp "list" "TD> "
        Assert.StrictEqual( v.Length, v2.Length + 1 )
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the target group is not unloaded, the target group can not be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_Loaded_001 () =
        // start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "

        let v = m_Client.RunCommandGetResp "list" "TD> "

        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate"  "Inactivated" "TG> "
        m_Client.CheckStatus "TG_00000001" "LOADED" "TG> "

        // try to delete target group, it failed
        m_Client.RunCommand "delete"  "Unexpected error" "TG> "

        m_Client.RunCommand "unselect" "" "TD> "

        let v2 = m_Client.RunCommandGetResp "list" "TD> "
        Assert.StrictEqual( v.Length, v2.Length )
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    // If the target group is not unloaded, the target group can not be deleted.
    [<Fact>]
    member _.Delete_TargetGroup_Active_001 () =
        // start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "

        let v = m_Client.RunCommandGetResp "list" "TD> "

        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.CheckStatus "TG_00000001" "ACTIVE" "TG> "

        // try to delete target group, it failed
        m_Client.RunCommand "delete"  "Unexpected error" "TG> "

        m_Client.RunCommand "unselect" "" "TD> "

        let v2 = m_Client.RunCommandGetResp "list" "TD> "
        Assert.StrictEqual( v.Length, v2.Length )
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "
