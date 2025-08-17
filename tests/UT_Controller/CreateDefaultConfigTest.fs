namespace Haruka.Test.UT.Controller

open System
open System.IO
open System.Collections.Generic

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Controller
open Haruka.IODataTypes
open Haruka.Test

type CreateDefaultConfig_Test () =

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    static member Init ( caseName : string ) =
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "CreateDefaultConfig_Test_" + caseName
        if Directory.Exists dname then GlbFunc.DeleteDir dname
        GlbFunc.CreateDir dname |> ignore
        dname

    [<Fact>]
    member _.CreateDefaultConfig_001() =
        let dname = CreateDefaultConfig_Test.Init "CreateDefaultConfig_001"
        GlbFunc.DeleteDir dname

        let d = 
            [
                ( "/p", EV_uint32( 99u ) );
                ( "/a", EV_String( "abc" ) );
            ]
            |> Seq.map KeyValuePair
            |> Dictionary
        let v = [| EV_String( dname ) |]
        let cmd = CommandParser( CtrlCmdType.InitWorkDir, d, v )
        let st = StringTable( "" )
        Haruka.Controller.main.CreateDefaultConfig st cmd

        let conffname = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
        Assert.True(( File.Exists conffname ))

        let conf = HarukaCtrlConf.ReaderWriter.LoadFile conffname
        Assert.True(( conf.RemoteCtrl.IsSome ))
        Assert.True(( conf.RemoteCtrl.Value.PortNum = 99us ))
        Assert.True(( conf.RemoteCtrl.Value.Address = "abc" ))
        Assert.True(( conf.LogMaintenance.IsNone ))
        Assert.True(( conf.LogParameters.IsNone ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateDefaultConfig_002() =
        let dname = CreateDefaultConfig_Test.Init "CreateDefaultConfig_002"
        GlbFunc.DeleteDir dname

        let d = new Dictionary< string, EnteredValue >()
        let v = [| EV_String( dname ) |]
        let cmd = CommandParser( CtrlCmdType.InitWorkDir, d, v )
        let st = StringTable( "" )
        Haruka.Controller.main.CreateDefaultConfig st cmd

        let conffname = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
        Assert.True(( File.Exists conffname ))

        let conf = HarukaCtrlConf.ReaderWriter.LoadFile conffname
        Assert.True(( conf.RemoteCtrl.IsSome ))
        Assert.True(( conf.RemoteCtrl.Value.PortNum = Constants.DEFAULT_MNG_CLI_PORT_NUM ))
        Assert.True(( conf.RemoteCtrl.Value.Address = "::1" ))
        Assert.True(( conf.LogMaintenance.IsNone ))
        Assert.True(( conf.LogParameters.IsNone ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateDefaultConfig_003() =
        let dname = CreateDefaultConfig_Test.Init "CreateDefaultConfig_003"
        let wfname = Functions.AppendPathName dname "aaa.txt"
        File.WriteAllText( wfname, "" )

        let d = new Dictionary< string, EnteredValue >()
        let v = [| EV_String( dname ) |]
        let cmd = CommandParser( CtrlCmdType.InitWorkDir, d, v )
        let st = StringTable( "" )
        Haruka.Controller.main.CreateDefaultConfig st cmd

        let conffname = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
        Assert.True(( File.Exists wfname ))
        Assert.False(( File.Exists conffname ))
        Assert.True(( Directory.Exists dname ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateDefaultConfig_004() =
        let dname = CreateDefaultConfig_Test.Init "CreateDefaultConfig_004"
        let wfname = Functions.AppendPathName dname "aaa.txt"
        File.WriteAllText( wfname, "" )

        let d = 
            [ ( "/o", EV_NoValue ) ]
            |> Seq.map KeyValuePair
            |> Dictionary
        let v = [| EV_String( dname ) |]
        let cmd = CommandParser( CtrlCmdType.InitWorkDir, d, v )
        let st = StringTable( "" )
        Haruka.Controller.main.CreateDefaultConfig st cmd

        let conffname = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
        Assert.True(( File.Exists conffname ))

        let conf = HarukaCtrlConf.ReaderWriter.LoadFile conffname
        Assert.True(( conf.RemoteCtrl.IsSome ))
        Assert.True(( conf.RemoteCtrl.Value.PortNum = Constants.DEFAULT_MNG_CLI_PORT_NUM ))
        Assert.True(( conf.RemoteCtrl.Value.Address = "::1" ))
        Assert.True(( conf.LogMaintenance.IsNone ))
        Assert.True(( conf.LogParameters.IsNone ))

        Assert.False(( File.Exists wfname ))

        GlbFunc.DeleteDir dname
