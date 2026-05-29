//=============================================================================
// Haruka Software Storage.
// Command01.fs : Test cases for client commands.
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

[<CollectionDefinition( "Command01" )>]
type Command01_Fixture() =

    let m_ControllPortNo = GlbFunc.nextTcpPortNo()
    let m_WorkPath =
        let tempPath = Path.GetTempPath()
        Functions.AppendPathName tempPath ( Guid.NewGuid().ToString( "N" ) )
    let m_Controller =
        ControllerFunc.InitializeConfigDir m_WorkPath m_ControllPortNo
        ControllerFunc.StartController m_WorkPath

    interface IDisposable with
        member _.Dispose (): unit =
            ()
    interface ICollectionFixture<Command01_Fixture>

    member _.ControllerProc = m_Controller
    member _.ControllPortNo = m_ControllPortNo
    member _.WorkPath = m_WorkPath

[<Collection( "Command01" )>]
type Command01( fx : Command01_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let m_WorkPath = fx.WorkPath
    let m_ControllPortNo = fx.ControllPortNo

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Theory>]
    [<InlineData( "exit" )>]
    [<InlineData( "exit /y" )>]
    member _.Exit_001 ( command : string ) =
        let client = ClientProc m_WorkPath
        client.RunCommandForTerminate command
        GlbFunc.DeleteDir m_WorkPath

    [<Theory>]
    [<InlineData( "exit" )>]
    [<InlineData( "exit /y" )>]
    member _.Exit_002 ( command : string ) =
        let client = ClientProc( "::1", m_ControllPortNo, m_WorkPath )
        client.RunCommandForTerminate command
        GlbFunc.DeleteDir m_WorkPath

    [<Fact>]
    member _.Exit_003 () =
        let client = ClientProc( "::1", m_ControllPortNo, m_WorkPath )
        client.RunCommand "set LOGLEVEL VERBOSE" "" "CR> "
        let v = client.RunCommandGetResp "exit" "CR> "
        Assert.NotEmpty v
        client.RunCommandForTerminate "exit /y"
        GlbFunc.DeleteDir m_WorkPath

    [<Fact>]
    member _.Login_001 () =
        let client1 = ClientProc m_WorkPath
        client1.RunCommand ( sprintf "login /h ::1 /p %d" m_ControllPortNo ) "" "CR> "

        let client2 = ClientProc m_WorkPath
        let v1 = client2.RunCommandGetResp ( sprintf "login /h ::1 /p %d" m_ControllPortNo ) "--> "
        Assert.NotEmpty v1

        client1.RunCommand "nop" "" "CR> "

        client2.RunCommand ( sprintf "login /h ::1 /p %d /f" m_ControllPortNo ) "" "CR> "

        let v2 = client1.RunCommandGetResp "nop" "CR> "
        Assert.NotEmpty v2

        client2.RunCommandForTerminate "exit /y"
        GlbFunc.DeleteDir m_WorkPath

    [<Fact>]
    member _.Logout_001 () =
        let client = ClientProc m_WorkPath
        let v1 = client.RunCommandGetResp "logout" "--> "
        Assert.NotEmpty v1
        client.RunCommand ( sprintf "login /h ::1 /p %d" m_ControllPortNo ) "" "CR> "
        client.RunCommand "logout" "" "--> "
        client.RunCommandForTerminate "exit"
        GlbFunc.DeleteDir m_WorkPath

    [<Fact>]
    member _.Logout_002 () =
        let client = ClientProc( "::1", m_ControllPortNo, m_WorkPath )
        client.RunCommand "set LOGLEVEL VERBOSE" "" "CR> "
        let v = client.RunCommandGetResp "logout" "CR> "
        Assert.NotEmpty v
        client.RunCommand "logout /y" "" "--> "
        client.RunCommandForTerminate "exit"
        GlbFunc.DeleteDir m_WorkPath

    [<Fact>]
    member _.Reload_001 () =
        let client = ClientProc( "::1", m_ControllPortNo, m_WorkPath )
        client.RunCommand "reload" "" "CR> "
        client.RunCommandForTerminate "exit"
        GlbFunc.DeleteDir m_WorkPath

    [<Fact>]
    member _.Reload_002 () =
        let client = ClientProc( "::1", m_ControllPortNo, m_WorkPath )
        client.RunCommand "set LOGLEVEL VERBOSE" "" "CR> "
        let v = client.RunCommandGetResp "reload" "CR> "
        Assert.NotEmpty v
        client.RunCommandForTerminate "exit /y"
        GlbFunc.DeleteDir m_WorkPath

    [<Fact>]
    member _.Reload_003 () =
        let client = ClientProc( "::1", m_ControllPortNo, m_WorkPath )
        client.RunCommand "set LOGLEVEL VERBOSE" "" "CR> "
        client.RunCommand "reload /y" "" "CR> "
        client.RunCommandForTerminate "exit"
        GlbFunc.DeleteDir m_WorkPath
