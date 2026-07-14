//=============================================================================
// Haruka Software Storage.
// Command04.fs : Test cases for client commands.
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

[<CollectionDefinition( "Command04" )>]
type Command04_Fixture() =

    let m_ControllPortNo = GlbFunc.nextTcpPortNo()
    let m_iSCSIPortNo1 = GlbFunc.nextTcpPortNo()
    let m_iSCSIPortNo2 = GlbFunc.nextTcpPortNo()
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
        p.RunCommand ( sprintf "set BlockSize %d" m_MediaBlockSizse ) "" "MD> "
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
        p.RunCommand ( sprintf "set BlockSize %d" m_MediaBlockSizse ) "" "MD> "
        p.RunCommand "unselect" "" "LU> "
        p.RunCommand "unselect" "" "T > "
        p.RunCommand "unselect" "" "TG> "

        p.RunCommand "create /n iqn.2020-05.example.com:target3" "Created" "TG> "
        p.RunCommand "select 1" "" "T > "
        p.RunCommand "create /l 3" "Created" "T > "
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
    interface ICollectionFixture<Command04_Fixture>

    member _.ControllerProc = m_Controller
    member _.ClientProc = m_Client
    member _.ControllPortNo = m_ControllPortNo
    member _.WorkPath = m_WorkPath
    member _.iSCSIPortNo1 = m_iSCSIPortNo1
    member _.iSCSIPortNo2 = m_iSCSIPortNo2
    member _.MediaSize = m_MediaSize
    member _.MediaBlockSizse = m_MediaBlockSizse

[<Collection( "Command04" )>]
type Command04( fx : Command04_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let m_WorkPath = fx.WorkPath
    let m_ControllPortNo = fx.ControllPortNo
    let m_Client = fx.ClientProc
    let iSCSIPortNo1 = fx.iSCSIPortNo1
    let iSCSIPortNo2 = fx.iSCSIPortNo2
    let g_CID0 = cid_me.zero
    let m_MediaSize = fx.MediaSize
    let m_MediaBlockSizse = fx.MediaBlockSizse

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
    member _.Create_Target_TDUnloaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "TG> "
        m_Client.CheckStatus "TG_00000001" "UNLOADED" "TG> "

        let v = m_Client.RunCommandGetResp "list" "TG> "

        // Add a target
        m_Client.RunCommand "create" "Created" "TG> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "UNLOADED(MOD)" "TG> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "TG> "

        let v2 = m_Client.RunCommandGetResp "list" "TG> "
        Assert.StrictEqual( v.Length + 1, v2.Length )

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Create_Target_TDUnloadedMod_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "set NAME bbb" "" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED(MOD)" "TG> "
        m_Client.CheckStatus "TG_00000001" "UNLOADED" "TG> "

        let v = m_Client.RunCommandGetResp "list" "TG> "

        // Add a target
        m_Client.RunCommand "create" "Created" "TG> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "UNLOADED(MOD)" "TG> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED(MOD)" "TG> "

        let v2 = m_Client.RunCommandGetResp "list" "TG> "
        Assert.StrictEqual( v.Length + 1, v2.Length )

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Create_Target_TDUnloadedRMod_001 () =

        // create target device 2
        m_Client.RunCommand "create /n GGG" "Created" "CR> "
        let tgidx = m_Client.GetIndexNumber "GGG" "CR> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TD> "
        m_Client.RunCommand "set ID TD_00000002" "" "TD> "
        m_Client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" iSCSIPortNo1 ) "Created" "TD> "
        m_Client.RunCommand "create targetgroup" "Created" "TD> "
        m_Client.RunCommand "select 1" "" "TG> "
        m_Client.RunCommand "set ID TG_00000001" "" "TG> "
        m_Client.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        m_Client.RunCommand "select 0" "" "T > "
        m_Client.RunCommand "create /l 1" "Created" "T > "
        m_Client.RunCommand "select 0" "" "LU> "
        m_Client.RunCommand ( sprintf "create membuffer %d" m_MediaSize ) "Created" "LU> "
        m_Client.RunCommand "select 0" "" "MD> "
        m_Client.RunCommand ( sprintf "set BlockSize %d" m_MediaBlockSizse ) "" "MD> "
        m_Client.RunCommand "unselect" "" "LU> "
        m_Client.RunCommand "unselect" "" "T > "
        m_Client.RunCommand "unselect" "" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "

        // publish and start target device 2
        m_Client.RunCommand "publish" "All configurations are uploaded" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "

        // modify target device 1
        m_Client.RunCommand "unselect" "" "CR> "
        let tgidx = m_Client.GetIndexNumber "TD_00000001" "CR> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TD> "
        m_Client.RunCommand "set ID TD_00000002" "" "TD> "
        m_Client.CheckStatus "TD_00000002" "UNLOAD(R-MOD)" "TD> "

        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.CheckStatus "TG_00000001" "UNLOADED" "TG> "

        let v = m_Client.RunCommandGetResp "list" "TG> "

        // Add a target
        m_Client.RunCommand "create" "Created" "TG> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "UNLOADED(MOD)" "TG> "
        m_Client.CheckStatus "TD_00000002" "UNLOAD(R-MOD)" "TG> "

        let v2 = m_Client.RunCommandGetResp "list" "TG> "
        Assert.StrictEqual( v.Length + 1, v2.Length )

        // stop and delete target device 2
        m_Client.RunCommand "reload /y" "" "CR> "
        let tgidx = m_Client.GetIndexNumber "TD_00000002" "CR> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "delete" "Deleted" "CR> "
        m_Client.RunCommand "publish" "All configurations are uploaded" "CR> "

        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Create_Target_Unloaded_001 () =
        // Start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        // unload target group
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unload" "Unloaded" "TG> "
        m_Client.CheckStatus "TG_00000001" "UNLOADED" "TG> "

        let v = m_Client.RunCommandGetResp "list" "TG> "

        // Add a target
        m_Client.RunCommand "create" "Created" "TG> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "UNLOADED(MOD)" "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        let v2 = m_Client.RunCommandGetResp "list" "TG> "
        Assert.StrictEqual( v.Length + 1, v2.Length )

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Create_Target_UnloadedMod_001 () =
        // Start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        // unload target group
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unload" "Unloaded" "TG> "
        m_Client.RunCommand "set NAME ggggggg" "" "TG> "
        m_Client.CheckStatus "TG_00000001" "UNLOADED(MOD)" "TG> "

        let v = m_Client.RunCommandGetResp "list" "TG> "

        // Add a target
        m_Client.RunCommand "create" "Created" "TG> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "UNLOADED(MOD)" "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        let v2 = m_Client.RunCommandGetResp "list" "TG> "
        Assert.StrictEqual( v.Length + 1, v2.Length )

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Create_Target_UnloadedAMod_001 () =
        // Start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        // unload target group
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unload" "Unloaded" "TG> "
        m_Client.RunCommand "set ID TG_00000002" "" "TG> "
        m_Client.RunCommand "set NAME wwwwwwwww" "" "TG> "
        m_Client.CheckStatus "wwwwwwwww" "UNLOAD(A-MOD)" "TG> "

        let v = m_Client.RunCommandGetResp "list" "TG> "

        // Add a target
        m_Client.RunCommand "create" "Created" "TG> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "wwwwwwwww" "UNLOAD(A-MOD)" "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        let v2 = m_Client.RunCommandGetResp "list" "TG> "
        Assert.StrictEqual( v.Length + 1, v2.Length )

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Create_Target_UnloadedLMod_001 () =
        // Start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        // Inactivate the target group 2
        let tgidx = m_Client.GetIndexNumber "TG_00000002" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "

        // unload target group 1
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unload" "Unloaded" "TG> "
        m_Client.RunCommand "set ID TG_00000002" "" "TG> "
        m_Client.RunCommand "set NAME wwwwwwwww" "" "TG> "
        m_Client.CheckStatus "wwwwwwwww" "UNLOAD(L-MOD)" "TG> "

        let v = m_Client.RunCommandGetResp "list" "TG> "

        // Add a target
        m_Client.RunCommand "create" "Created" "TG> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "wwwwwwwww" "UNLOAD(L-MOD)" "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        let v2 = m_Client.RunCommandGetResp "list" "TG> "
        Assert.StrictEqual( v.Length + 1, v2.Length )

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Create_Target_Active_001 () =
        // Start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.CheckStatus "TG_00000001" "ACTIVE" "TG> "

        let v = m_Client.RunCommandGetResp "list" "TG> "

        // Try to add a target, it failed.
        m_Client.RunCommand "create" "Unexpected" "TG> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "ACTIVE" "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        let v2 = m_Client.RunCommandGetResp "list" "TG> "
        Assert.StrictEqual( v.Length, v2.Length )

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Create_Target_Loaded_001 () =
        // Start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        // inactivate target group
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.CheckStatus "TG_00000001" "LOADED" "TG> "

        let v = m_Client.RunCommandGetResp "list" "TG> "

        // Try to add a target, it failed.
        m_Client.RunCommand "create" "Unexpected" "TG> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "LOADED" "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        let v2 = m_Client.RunCommandGetResp "list" "TG> "
        Assert.StrictEqual( v.Length, v2.Length )

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Delete_Target_TDUnloaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "TG> "
        m_Client.CheckStatus "TG_00000001" "UNLOADED" "TG> "

        let v = m_Client.RunCommandGetResp "list" "TG> "
        Assert.StrictEqual( 1, v.Length )

        // Delete a target
        let tgidx = m_Client.GetIndexNumber "iqn.2020-05.example.com:target1" "TG> "
        m_Client.RunCommand ( sprintf "delete /i %d" tgidx ) "Deleted" "TG> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "UNLOADED(MOD)" "TG> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "TG> "

        let v2 = m_Client.RunCommandGetResp "list" "TG> "
        Assert.StartsWith( "There are no child nodes", v2.[0] )

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Delete_Target_Active_001 () =
        // Start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.CheckStatus "TG_00000001" "ACTIVE" "TG> "

        let v = m_Client.RunCommandGetResp "list" "TG> "

        // Try to delete a target, it failed.
        let tgidx = m_Client.GetIndexNumber "iqn.2020-05.example.com:target1" "TG> "
        m_Client.RunCommand ( sprintf "delete /i %d" tgidx ) "Unexpected" "TG> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "ACTIVE" "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        let v2 = m_Client.RunCommandGetResp "list" "TG> "
        Assert.StrictEqual( v.Length, v2.Length )

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Create_LU_TDUnloaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "select 0" "" "T > "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "T > "
        m_Client.CheckStatus "TG_00000001" "UNLOADED" "T > "

        let v = m_Client.RunCommandGetResp "list" "T > "

        // Create LU
        m_Client.RunCommand "create /l 2" "Created" "T > "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "UNLOADED(MOD)" "T > "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "T > "

        let v2 = m_Client.RunCommandGetResp "list" "T > "
        Assert.StrictEqual( v.Length + 1, v2.Length )

        m_Client.RunCommand "unselect" "" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Create_LU_Active_001 () =
        // Start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "select 0" "" "T > "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "T > "
        m_Client.CheckStatus "TG_00000001" "ACTIVE" "T > "

        let v = m_Client.RunCommandGetResp "list" "T > "

        // Try to create LU, it failed.
        m_Client.RunCommand "create /l 2" "Unexpected" "T > "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "ACTIVE" "T > "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "T > "

        let v2 = m_Client.RunCommandGetResp "list" "T > "
        Assert.StrictEqual( v.Length, v2.Length )

        m_Client.RunCommand "unselect" "" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Delete_LU_TDUnloaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "select 0" "" "T > "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "T > "
        m_Client.CheckStatus "TG_00000001" "UNLOADED" "T > "

        let v = m_Client.RunCommandGetResp "list" "T > "
        Assert.StrictEqual( 1, v.Length )

        // Delete LU
        m_Client.RunCommand "delete /i 0" "Deleted" "T > "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "UNLOADED(MOD)" "T > "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "T > "

        let v2 = m_Client.RunCommandGetResp "list" "T > "
        Assert.StartsWith( "There are no child nodes", v2.[0] )

        m_Client.RunCommand "unselect" "" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Delete_LU_Active_001 () =
        // Start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "select 0" "" "T > "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "T > "
        m_Client.CheckStatus "TG_00000001" "ACTIVE" "T > "

        let v = m_Client.RunCommandGetResp "list" "T > "

        // Try to delete LU, it failed.
        m_Client.RunCommand "delete /i 0" "Unexpected" "T > "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "ACTIVE" "T > "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "T > "

        let v2 = m_Client.RunCommandGetResp "list" "T > "
        Assert.StrictEqual( v.Length, v2.Length )

        m_Client.RunCommand "unselect" "" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.Attach_TDUnloaded_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000002" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "TG> "
        m_Client.CheckStatus "TG_00000002" "UNLOADED" "TG> "

        // select target2
        let tgidx = m_Client.GetIndexNumber "target2" "TG> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "T > "
        let v = m_Client.RunCommandGetResp "list" "T > "
        Assert.StrictEqual( 1, v.Length )

        // Attach LU3
        m_Client.RunCommand "attach 3" "Attach LU" "T > "
        let v2 = m_Client.RunCommandGetResp "list" "T > "
        Assert.StrictEqual( 2, v2.Length )

        // Target group will be in a modified state.
        m_Client.CheckStatus "TG_00000002" "UNLOADED(MOD)" "T > "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "T > "

        // LU3 has multiple parents.
        let tgidx = m_Client.GetIndexNumber "LUN=3" "T > "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "LU> "
        let v3 = m_Client.RunCommandGetResp "listparent" "LU> "
        Assert.StrictEqual( 2, v3.Length )

        // select target3
        m_Client.RunCommand "unselect /p 1" "" "T > "
        let v4 = m_Client.RunCommandGetResp "pwd" "T > "
        Assert.StrictEqual( 1, v4.Length )
        Assert.Contains( "target3", v4.[0] )

        let v5 = m_Client.RunCommandGetResp "list" "T > "
        Assert.StrictEqual( 1, v5.Length )

        // detach LU3
        m_Client.RunCommand "detach 3" "Detach LU" "T > "
        let v6 = m_Client.RunCommandGetResp "list" "T > "
        Assert.StartsWith( "There are no child nodes", v6.[0] )

        // select target2
        m_Client.RunCommand "unselect" "" "TG> "
        let tgidx = m_Client.GetIndexNumber "target2" "TG> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "T > "

        let v7 = m_Client.RunCommandGetResp "list" "T > "
        Assert.StrictEqual( 2, v7.Length )

        // detach LU2
        m_Client.RunCommand "detach 2" "Detach LU" "T > "
        let v8 = m_Client.RunCommandGetResp "list" "T > "
        Assert.StrictEqual( 1, v8.Length )

        // detach LU3
        m_Client.RunCommand "detach 3" "Detach LU" "T > "
        let v8 = m_Client.RunCommandGetResp "list" "T > "
        Assert.StartsWith( "There are no child nodes", v8.[0] )

        m_Client.RunCommand "unselect" "" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Theory>]
    [<InlineData( "attach 3" )>]
    [<InlineData( "detach 2" )>]
    member _.Attach_Active_001 ( cmd : string ) =
        // Start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000002" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        let tgidx = m_Client.GetIndexNumber "target2" "TG> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "T > "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "T > "
        m_Client.CheckStatus "TG_00000002" "ACTIVE" "T > "

        let v = m_Client.RunCommandGetResp "list" "T > "

        // Try to attach or detach LU, it failed.
        m_Client.RunCommand cmd "Unexpected" "T > "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000002" "ACTIVE" "T > "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "T > "

        let v2 = m_Client.RunCommandGetResp "list" "T > "
        Assert.StrictEqual( v.Length, v2.Length )

        m_Client.RunCommand "unselect" "" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Theory>]
    [<InlineData( "create plainfile aaa" )>]
    [<InlineData( "create membuffer 65536" )>]
    [<InlineData( "create debug" )>]
    member _.Create_media_TDUnloaded_001 ( cmd : string ) =
        m_Client.RunCommand "select 0" "" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "select 0" "" "T > "
        m_Client.RunCommand "select 0" "" "LU> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "LU> "
        m_Client.CheckStatus "TG_00000001" "UNLOADED" "LU> "

        let v = m_Client.RunCommandGetResp "list" "LU> "
        Assert.StrictEqual( 1, v.Length )

        // create media
        m_Client.RunCommand cmd "Created" "LU> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "UNLOADED(MOD)" "LU> "
        m_Client.CheckStatus "TD_00000001" "UNLOADED" "LU> "

        let v2 = m_Client.RunCommandGetResp "list" "LU> "
        Assert.StrictEqual( 2, v2.Length )

        m_Client.RunCommand "unselect" "" "T > "
        m_Client.RunCommand "unselect" "" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Theory>]
    [<InlineData( "create plainfile aaa" )>]
    [<InlineData( "create membuffer 65536" )>]
    [<InlineData( "create debug" )>]
    member _.Create_media_Active_001 ( cmd : string ) =
        // Start target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "select 0" "" "T > "
        m_Client.RunCommand "select 0" "" "LU> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "LU> "
        m_Client.CheckStatus "TG_00000001" "ACTIVE" "LU> "

        let v = m_Client.RunCommandGetResp "list" "LU> "
        Assert.StrictEqual( 1, v.Length )

        // Try to create media, it failed.
        m_Client.RunCommand cmd "Unexpected" "LU> "

        // Target Group will be in a modified state.
        m_Client.CheckStatus "TG_00000001" "ACTIVE" "LU> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "LU> "

        let v2 = m_Client.RunCommandGetResp "list" "LU> "
        Assert.StrictEqual( v.Length, v2.Length )

        m_Client.RunCommand "unselect" "" "T > "
        m_Client.RunCommand "unselect" "" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Fact>]
    member _.InitiMedia_PlainFile_001 () =
        let fname = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName() )

        let v = m_Client.RunCommandGetResp "imstatus" "CR> "
        Assert.Empty v

        m_Client.RunCommand ( sprintf "initmedia plainfile %s 65536" fname ) "Started" "CR> "

        let mutable flg = true
        while flg do
            Thread.Sleep 10
            let v = m_Client.RunCommandGetResp "imstatus" "CR> "
            if v.Length > 0 then
                flg <- v.[0].Contains "Succeeded" |> not

        Assert.True( File.Exists fname )
        let fdata = File.ReadAllBytes fname
        Assert.StrictEqual( 65536, fdata.Length )
        File.Delete fname

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