//=============================================================================
// Haruka Software Storage.
// Command06.fs : Test cases for client commands.
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

[<CollectionDefinition( "Command06" )>]
type Command06_Fixture() =

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
    interface ICollectionFixture<Command06_Fixture>

    member _.ControllerProc = m_Controller
    member _.ClientProc = m_Client
    member _.ControllPortNo = m_ControllPortNo
    member _.WorkPath = m_WorkPath
    member _.iSCSIPortNo1 = m_iSCSIPortNo1
    member _.MediaSize = m_MediaSize
    member _.MediaBlockSizse = m_MediaBlockSizse

[<Collection( "Command06" )>]
type Command06( fx : Command06_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let m_WorkPath = fx.WorkPath
    let m_ControllPortNo = fx.ControllPortNo
    let m_Client = fx.ClientProc
    let iSCSIPortNo1 = fx.iSCSIPortNo1
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

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Theory>]
    [<InlineData( "load" )>]
    [<InlineData( "unload" )>]
    [<InlineData( "activate" )>]
    [<InlineData( "inactivate" )>]
    member _.Load_UnloadedMod_001 ( cmd : string ) =
        // Start the target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        // select and modify the target group
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unload" "Unloaded" "TG> "
        m_Client.RunCommand "set NAME aaaaaaa" "" "TG> "
        m_Client.CheckStatus "TG_00000001" "UNLOADED(MOD)" "TG> "

        // try to load target group, it failed.
        m_Client.RunCommand cmd "Configuration is modified" "TG> "
        m_Client.CheckStatus "TG_00000001" "UNLOADED(MOD)" "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Theory>]
    [<InlineData( "load", "Unexpected", "LOADED" )>]
    [<InlineData( "unload", "Unloaded", "UNLOADED" )>]
    [<InlineData( "activate", "Activated", "ACTIVE" )>]
    [<InlineData( "inactivate", "Specified target group is", "LOADED" )>]
    member _.Load_Loaded_001 ( cmd : string ) ( expResp : string ) ( nextStat : string ) =
        // Start the target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        // select and inactivate the target group
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.CheckStatus "TG_00000001" "LOADED" "TG> "

        // load/unload/activate/inactivate the target group
        m_Client.RunCommand cmd expResp "TG> "
        m_Client.CheckStatus "TG_00000001" nextStat "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Theory>]
    [<InlineData( "load", "Unexpected", "ACTIVE" )>]
    [<InlineData( "unload", "Specified target group is", "ACTIVE" )>]
    [<InlineData( "activate", "Specified target group is", "ACTIVE" )>]
    [<InlineData( "inactivate", "Inactivated", "LOADED" )>]
    member _.Load_Active_001 ( cmd : string ) ( expResp : string ) ( nextStat : string ) =
        // Start the target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        // select the target group
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.CheckStatus "TG_00000001" "ACTIVE" "TG> "

        // try to load target group, it failed.
        m_Client.RunCommand cmd expResp "TG> "
        m_Client.CheckStatus "TG_00000001" nextStat "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Theory>]
    [<InlineData( "load" )>]
    [<InlineData( "unload" )>]
    [<InlineData( "activate" )>]
    [<InlineData( "inactivate" )>]
    member _.Load_UnloadedAMod_001 ( cmd : string ) =
        // Start the target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        // select and modify the target group 1
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unload" "Unloaded" "TG> "
        m_Client.RunCommand "set ID TG_00000002" "" "TG> "
        m_Client.RunCommand "set NAME wwwwwwwww" "" "TG> "
        m_Client.CheckStatus "wwwwwwwww" "UNLOAD(A-MOD)" "TG> "

        // try to load target group, it failed.
        m_Client.RunCommand cmd "Configuration is modified" "TG> "
        m_Client.CheckStatus "wwwwwwwww" "UNLOAD(A-MOD)" "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

    [<Theory>]
    [<InlineData( "load" )>]
    [<InlineData( "unload" )>]
    [<InlineData( "activate" )>]
    [<InlineData( "inactivate" )>]
    member _.Load_UnloadedLMod_001 ( cmd : string ) =
        // Start the target device
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TD> "

        // Inactivate the target group 2
        let tgidx = m_Client.GetIndexNumber "TG_00000002" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "

        // select and modify the target group 1
        let tgidx = m_Client.GetIndexNumber "TG_00000001" "TD> "
        m_Client.RunCommand ( sprintf "select %d" tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unload" "Unloaded" "TG> "
        m_Client.RunCommand "set ID TG_00000002" "" "TG> "
        m_Client.RunCommand "set NAME wwwwwwwww" "" "TG> "
        m_Client.CheckStatus "wwwwwwwww" "UNLOAD(L-MOD)" "TG> "

        // try to load target group, it failed.
        m_Client.RunCommand cmd "Configuration is modified" "TG> "
        m_Client.CheckStatus "wwwwwwwww" "UNLOAD(L-MOD)" "TG> "
        m_Client.CheckStatus "TD_00000001" "RUNNING" "TG> "

        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "kill" "Killed" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "

