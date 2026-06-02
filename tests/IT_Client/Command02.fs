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
        p.RunCommand "create targetgroup" "Created" "TD> "
        p.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo1 ) "Created" "TD> "
        p.RunCommand "select 0" "" "TG> "

        // Create LU
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

[<Collection( "Command02" )>]
type Command02( fx : Command02_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let m_WorkPath = fx.WorkPath
    let m_ControllPortNo = fx.ControllPortNo
    let m_Client = fx.ClientProc

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

    // If the Target Group is activated, the settings cannot be changed.
    [<Fact>]
    member _.Set_TargetGroup_Activated_001 () =
        m_Client.RunCommand "select 0" "" "TD> "
        m_Client.RunCommand "start" "Started" "TD> "
        let tgidx = m_Client.GetIndexNumber "Target Group" "TD> "
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
        let tgidx = m_Client.GetIndexNumber "Target Group" "TD> "
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
        let tgidx = m_Client.GetIndexNumber "Target Group" "TD> "
        m_Client.RunCommand ( sprintf "select %d"  tgidx ) "" "TG> "
        m_Client.RunCommand "inactivate" "Inactivated" "TG> "
        m_Client.RunCommand "unload" "Unloaded" "TG> "
        m_Client.RunCommand "set NAME aaa" "" "TG> "
        m_Client.RunCommand "kill" "Killed" "TG> "
        m_Client.RunCommand "unselect" "" "TD> "
        m_Client.RunCommand "unselect" "" "CR> "
        m_Client.RunCommand "reload /y" "" "CR> "
