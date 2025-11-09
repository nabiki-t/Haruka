//=============================================================================
// Haruka Software Storage.
// ServerStatusTest1.fs : Test cases for ServerStatus class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Client

//=============================================================================
// Import declaration

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Diagnostics
open System.Collections.Generic
open System.Xml

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.Controller
open Haruka.IODataTypes
open Haruka.Test

//=============================================================================
// Class implementation

type ServerStatus_Test1() =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    static member Init ( caseName : string ) =
        let portNo = GlbFunc.nextTcpPortNo()
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "ServerStatus_Test_" + caseName
        if Directory.Exists dname then GlbFunc.DeleteDir dname
        GlbFunc.CreateDir dname |> ignore
        ( portNo, dname )

    static member defaultNego : TargetDeviceConf.T_NegotiableParameters = {
        MaxRecvDataSegmentLength = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
        MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength;
        FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength;
        DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait;
        DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain;
        MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
    }

    static member defaultLP : TargetDeviceConf.T_LogParameters = {
        SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
        HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
        LogLevel = LogLevel.LOGLEVEL_INFO;
    }

    static member defaultNP : TargetDeviceConf.T_NetworkPortal = {
        IdentNumber = netportidx_me.fromPrim 1u;
        TargetPortalGroupTag = tpgt_me.zero;
        TargetAddress = "::1";
        PortNumber = 123us;
        DisableNagle = true;
        ReceiveBufferSize = 8192;
        SendBufferSize = 8192;
        WhiteList = [];
    }

    static member defaultSF : TargetGroupConf.T_PlainFile = {
        IdentNumber = mediaidx_me.fromPrim 1u;
        MediaName = "";
        FileName = "";
        MaxMultiplicity = Constants.PLAINFILE_DEF_MAXMULTIPLICITY;
        QueueWaitTimeOut = Constants.PLAINFILE_DEF_QUEUEWAITTIMEOUT;
        WriteProtect = false;
    }

    static member defTarget ( ident : uint32 ) ( name : string ) : TargetGroupConf.T_Target = {
        IdentNumber = tnodeidx_me.fromPrim ident;
        TargetPortalGroupTag = tpgt_me.zero;
        TargetName = name;
        TargetAlias = "";
        LUN = [];
        Auth = TargetGroupConf.U_None();
    }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    ////////////////////////////////////////////////////////////////////////////
    // Stub procedure for server.

    static member StubLogin ( portNo : int ) =
        task {
            let sl = new TcpListener( IPAddress.Parse "::1", portNo )
            sl.Start ()
            let! s = sl.AcceptSocketAsync()
            let c = new NetworkStream( s )

            // Receive Login request
            let! loginRequestStr = Functions.FramingReceiver c
            let loginRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString loginRequestStr
            match loginRequest.Request with
            | HarukaCtrlerCtrlReq.U_Login( x ) ->
                ()
            | _ ->
                Assert.Fail __LINE__

            // send login response
            let sessID = CtrlSessionID.NewID()
            let rb =
                HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                    Response = HarukaCtrlerCtrlRes.T_Response.U_LoginResult( {
                            Result = true;
                            SessionID = sessID;
                    })
                }
            do! Functions.FramingSender c rb

            return ( sl, c, sessID )
        }

    static member RespDefaultCtrlConf
            ( c : NetworkStream )
            ( portNo : int ) =
        task {
            let! _ = Functions.FramingReceiver c
            let ctrlConf : HarukaCtrlConf.T_HarukaCtrl = {
                RemoteCtrl = Some { PortNum = uint16 portNo; Address = "::1"; WhiteList = []; };
                LogMaintenance = None;
                LogParameters = None;
            }
            let rb1 =
                HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                    Response = HarukaCtrlerCtrlRes.T_Response.U_ControllerConfig({
                        Config = HarukaCtrlConf.ReaderWriter.ToString ctrlConf
                        ErrorMessage = "";
                    })
                }
            do! Functions.FramingSender c rb1
        }

    static member RespTargetDeviceDirs
            ( c : NetworkStream )
            ( tdidList : TDID_T list ) =
        task {
            let! _ = Functions.FramingReceiver c 
            let rb2 =
                HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                    Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs({
                        TargetDeviceID = tdidList;
                        ErrorMessage = "";
                    })
                }
            do! Functions.FramingSender c rb2
        }

    static member RespDefaultTargetDeviceConfig
            ( c : NetworkStream )
            ( tdid : TDID_T ) =
        task {
            let! _ = Functions.FramingReceiver c 
            let ctrlConf : TargetDeviceConf.T_TargetDevice = {
                NetworkPortal = [ ServerStatus_Test1.defaultNP ];
                NegotiableParameters = None;
                LogParameters = None;
                DeviceName = "targetdevice000";
            }
            let rb3 =
                HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                    Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig({
                        TargetDeviceID = tdid;
                        Config = TargetDeviceConf.ReaderWriter.ToString ctrlConf
                        ErrorMessage = "";
                    })
                }
            do! Functions.FramingSender c rb3
        }

    static member RespLoadedTargetGroup
            ( c : NetworkStream )
            ( tdid : TDID_T )
            ( tginfo : TargetDeviceCtrlRes.T_LoadedTGInfo list ) =
        task {
            let! _ = Functions.FramingReceiver c 
            let targetDeviceCtrlResponse =
                TargetDeviceCtrlRes.ReaderWriter.ToString {
                    Response = TargetDeviceCtrlRes.U_LoadedTargetGroups({
                        LoadedTGInfo = tginfo
                    })
                }
            let rb5 =
                HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                    Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceCtrlResponse( {
                        TargetDeviceID = tdid;
                        Response = targetDeviceCtrlResponse;
                        ErrorMessage = "";
                    })
                }
            do! Functions.FramingSender c rb5
        }

    static member RespTargetDeviceProcs
            ( c : NetworkStream )
            ( tdid : TDID_T list ) =
        task {
            let! _ = Functions.FramingReceiver c 
            let rb5 =
                HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                    Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceProcs( {
                        TargetDeviceID = tdid;
                        ErrorMessage = "";
                    })
                }
            do! Functions.FramingSender c rb5
        }

    static member RespAllTargetGroupConfig
            ( c : NetworkStream )
            ( tdid : TDID_T )
            ( conf : HarukaCtrlerCtrlRes.T_TargetGroup ) =
        task {
            let! _ = Functions.FramingReceiver c 
            let rb4 =
                HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                    Response = HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig({
                        TargetDeviceID = tdid;
                        TargetGroup = [conf];
                        ErrorMessage = "";
                    })
                }
            do! Functions.FramingSender c rb4
        }

    static member StubLoginAndInit portNo tflg =
        task {
            let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo
            let tdid = GlbFunc.newTargetDeviceID()
            let tgid = GlbFunc.newTargetGroupID()
            do! ServerStatus_Test1.RespDefaultCtrlConf c portNo
            do! ServerStatus_Test1.RespTargetDeviceDirs c ( if tflg then [tdid] else [] )

            if tflg then
                do! ServerStatus_Test1.RespDefaultTargetDeviceConfig c tdid
                let tgconf =
                    TargetGroupConf.ReaderWriter.ToString {
                        TargetGroupID = tgid;
                        TargetGroupName = "targetgroup000";
                        EnabledAtStart = false;
                        Target = [{
                            IdentNumber = tnodeidx_me.fromPrim 0u;
                            TargetPortalGroupTag = tpgt_me.zero;
                            TargetName = "target000";
                            TargetAlias = "";
                            LUN = [ lun_me.fromPrim 1UL ];
                            Auth = TargetGroupConf.U_None();
                        }];
                        LogicalUnit = [{
                            LUN = lun_me.fromPrim 1UL;
                            LUName = "";
                            WorkPath = "";
                            LUDevice = TargetGroupConf.U_DummyDevice();
                        }];
                    }
                do! ServerStatus_Test1.RespAllTargetGroupConfig c tdid { TargetGroupID = tgid; Config = tgconf; }

            return ( sl, c, sessID, tdid, tgid )
        }

    ////////////////////////////////////////////////////////////////////////////
    // Write configuration files.

    static member WriteCtrlConfig ( dname : string ) ( portno : int ) =
        let ctrlConfFName = Functions.AppendPathName dname Constants.CONTROLLER_CONF_FILE_NAME
        let ctrlConf : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = Some {
                PortNum = uint16 portno;
                Address = "::1";
                WhiteList = [];
            };
            LogMaintenance = None;
            LogParameters = None;
        }
        HarukaCtrlConf.ReaderWriter.WriteFile ctrlConfFName ctrlConf

    static member WriteTargetDeviceConfig ( dname : string ) ( portno : int ) ( tdid : TDID_T ) =
        let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let tdConfFName = Functions.AppendPathName tdConfDName ( Constants.TARGET_DEVICE_CONF_FILE_NAME )
        let tdConf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [{
                ServerStatus_Test1.defaultNP with
                    PortNumber = uint16 portno;
            }];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "a001";
        }
        GlbFunc.CreateDir tdConfDName |> ignore
        TargetDeviceConf.ReaderWriter.WriteFile tdConfFName tdConf

    static member WriteTargetGroupConfig_WithDummyLU ( dname : string ) ( tdid : TDID_T ) ( tgid : TGID_T ) ( lun : LUN_T ) ( tgname : string ) ( tid : uint32 ) =
        let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let tgConfFName = Functions.AppendPathName tdConfDName ( tgid_me.toString tgid )
        let tgConf : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid;
            TargetGroupName = tgname;
            EnabledAtStart = true;
            Target = [{
                IdentNumber = tnodeidx_me.fromPrim tid;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetName = ( sprintf "target%03d" tid );
                TargetAlias = "";
                LUN = [ lun ];
                Auth = TargetGroupConf.U_None();
            }];
            LogicalUnit = [{
                LUN = lun;
                LUName = "";
                WorkPath = "";
                LUDevice = TargetGroupConf.U_DummyDevice();
            }];
        }
        TargetGroupConf.ReaderWriter.WriteFile tgConfFName tgConf
        let luDName = Functions.AppendPathName tdConfDName ( lun_me.WorkDirName lun )
        GlbFunc.CreateDir luDName |> ignore



    ////////////////////////////////////////////////////////////////////////////
    // Test code.

    [<Fact>]
    member _.LoadConfigure_001() =
        let portNo, dname = ServerStatus_Test1.Init "LoadConfigure_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetControllerConfig request
                let! getControllerConfigRequestStr = Functions.FramingReceiver c 
                let getControllerConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getControllerConfigRequestStr
                match getControllerConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetControllerConfig( x ) ->
                    Assert.True(( sessID = x ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetControllerConfig response
                let ctrlConf : HarukaCtrlConf.T_HarukaCtrl = {
                    RemoteCtrl = Some {
                        PortNum = uint16 portNo;
                        Address = "::1";
                        WhiteList = [];
                    };
                    LogMaintenance = None;
                    LogParameters = None;
                }
                let rb1 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_ControllerConfig({
                            Config = HarukaCtrlConf.ReaderWriter.ToString ctrlConf
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb1

                // receive GetTargetDeviceDir request
                let! getTargetDeviceDirRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceDirRequestStr
                match getTargetDeviceDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceDir( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs({
                            TargetDeviceID = [];
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2

                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                Assert.True(( ss.ControllerNode.RemoteCtrlValue.PortNum = uint16 portNo ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LoadConfigure_002() =
        let portNo, dname = ServerStatus_Test1.Init "LoadConfigure_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // GetControllerConfig
                let! _ = Functions.FramingReceiver c 
                do! Functions.FramingSender c "aaaaaaaa"

                // GetTargetDeviceDir
                let! recvstr2 = Functions.FramingReceiver c 
                let request2 = HarukaCtrlerCtrlReq.ReaderWriter.LoadString recvstr2
                match request2.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceDir( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs({
                            TargetDeviceID = [];
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                Assert.True(( ss.ControllerNode.RemoteCtrlValue.PortNum = Constants.DEFAULT_MNG_CLI_PORT_NUM ))
                Assert.True(( ss.GetTargetDeviceNodes().Length = 0 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LoadConfigure_003() =
        let portNo, dname = ServerStatus_Test1.Init "LoadConfigure_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // GetControllerConfig
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo

                // GetTargetDeviceDir
                let! _ = Functions.FramingReceiver c
                do! Functions.FramingSender c "aaaaaaaa"

                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                Assert.True(( ss.ControllerNode.RemoteCtrlValue.PortNum = uint16 portNo ))
                Assert.True(( ss.GetTargetDeviceNodes().Length = 0 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LoadTargetDeviceConfig_001() =
        let portNo, dname = ServerStatus_Test1.Init "LoadTargetDeviceConfig_001"
        let tdid = GlbFunc.newTargetDeviceID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // GetControllerConfig
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo

                // GetTargetDeviceDir
                do! ServerStatus_Test1.RespTargetDeviceDirs c [tdid]

                // receive GetTargetDeviceConfig request
                let! getTargetDeviceConfigStr = Functions.FramingReceiver c 
                let getTargetDeviceConfig = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceConfigStr
                match getTargetDeviceConfig.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceConfig( x ) ->
                    Assert.True(( sessID = x.SessionID ))
                    Assert.True(( tdid = x.TargetDeviceID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceConfig response
                let ctrlConf : TargetDeviceConf.T_TargetDevice = {
                    NetworkPortal = [ ServerStatus_Test1.defaultNP ];
                    NegotiableParameters = None;
                    LogParameters = None;
                    DeviceName = "abc";
                }
                let rb3 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig({
                            TargetDeviceID = tdid;
                            Config = TargetDeviceConf.ReaderWriter.ToString ctrlConf
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb3

                // receive GetAllTargetGroupConfig request
                let! getAllTargetGroupConfigStr = Functions.FramingReceiver c 
                let getAllTargetGroupConfig = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getAllTargetGroupConfigStr
                match getAllTargetGroupConfig.Request with
                | HarukaCtrlerCtrlReq.U_GetAllTargetGroupConfig( x ) ->
                    Assert.True(( sessID = x.SessionID ))
                    Assert.True(( tdid = x.TargetDeviceID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetAllTargetGroupConfig response
                let rb4 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_AllTargetGroupConfig({
                            TargetDeviceID = tdid;
                            TargetGroup = []
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb4

                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let tdlist = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist.Length = 1 ))
                Assert.True(( tdlist.[0].TargetDeviceID = tdid ))
                let nplist = ( tdlist.[0] :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                Assert.True(( nplist.Length = 1 ))
                Assert.True(( nplist.[0].NetworkPortal.IdentNumber = ServerStatus_Test1.defaultNP.IdentNumber ))
                Assert.True(( nplist.[0].NetworkPortal.PortNumber = ServerStatus_Test1.defaultNP.PortNumber ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LoadTargetDeviceConfig_002() =
        let portNo, dname = ServerStatus_Test1.Init "LoadTargetDeviceConfig_002"
        let tdid = GlbFunc.newTargetDeviceID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // GetControllerConfig
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo

                // GetTargetDeviceDir
                do! ServerStatus_Test1.RespTargetDeviceDirs c [tdid]

                // GetTargetDeviceConfig
                let! _ = Functions.FramingReceiver c 
                do! Functions.FramingSender c "aaaa"

                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let tdlist = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist.Length = 0 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LoadTargetGroupConfig_001() =
        let portNo, dname = ServerStatus_Test1.Init "LoadTargetGroupConfig_001"
        let tdid = GlbFunc.newTargetDeviceID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // ControllerConfig
                do! ServerStatus_Test1.RespDefaultCtrlConf c portNo

                // TargetDeviceDir
                do! ServerStatus_Test1.RespTargetDeviceDirs c [tdid]

                // TargetDeviceConfig
                do! ServerStatus_Test1.RespDefaultTargetDeviceConfig c tdid

                // receive GetAllTargetGroupConfig request
                let! getAllTargetGroupConfigStr = Functions.FramingReceiver c 
                let getAllTargetGroupConfig = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getAllTargetGroupConfigStr
                match getAllTargetGroupConfig.Request with
                | HarukaCtrlerCtrlReq.U_GetAllTargetGroupConfig( x ) ->
                    Assert.True(( sessID = x.SessionID ))
                    Assert.True(( tdid = x.TargetDeviceID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetAllTargetGroupConfig response
                do! Functions.FramingSender c "aaa"

                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let tdlist = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist.Length = 1 ))
                Assert.True(( tdlist.[0].TargetDeviceID = tdid ))
                let nplist = ( tdlist.[0] :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                Assert.True(( nplist.Length = 1 ))
                Assert.True(( nplist.[0].NetworkPortal.IdentNumber = ServerStatus_Test1.defaultNP.IdentNumber ))
                Assert.True(( nplist.[0].NetworkPortal.PortNumber = ServerStatus_Test1.defaultNP.PortNumber ))
                let tglist = ( tdlist.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tglist.Length = 0 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.LoadTargetGroupConfig_002() =
        let portNo, dname = ServerStatus_Test1.Init "LoadTargetGroupConfig_002"
        let tdid = GlbFunc.newTargetDeviceID()
        let tgid = GlbFunc.newTargetGroupID()
        let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let portalPortNo = GlbFunc.nextTcpPortNo()
        ServerStatus_Test1.WriteCtrlConfig dname portNo
        ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo tdid

        let tgConfFName = Functions.AppendPathName tdConfDName ( tgid_me.toString tgid )
        let tgConf : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid;
            TargetGroupName = "b001";
            EnabledAtStart = false;
            Target = [{
                IdentNumber = tnodeidx_me.fromPrim 99u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetName = "c001";
                TargetAlias = "";
                LUN = [ lun_me.fromPrim 88UL ];
                Auth = TargetGroupConf.U_None();
            }];
            LogicalUnit = [{
                LUN = lun_me.fromPrim 88UL;
                LUName = "";
                WorkPath = "";
                LUDevice = TargetGroupConf.U_DummyDevice();
            }];
        }
        TargetGroupConf.ReaderWriter.WriteFile tgConfFName tgConf
        let luDName = Functions.AppendPathName tdConfDName ( lun_me.WorkDirName tgConf.LogicalUnit.[0].LUN )
        GlbFunc.CreateDir luDName |> ignore

        let killer = new HKiller() :> IKiller
        let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
        ctrl.LoadInitialTargetDeviceProcs()
        ctrl.WaitRequest()

        try
            let st = new StringTable( "" )
            let ss = new ServerStatus( st )

            use cc1 =
                CtrlConnection.Connect st "::1" portNo false
                |> Functions.RunTaskSynchronously

            ss.LoadConfigure cc1 true
            |> Functions.RunTaskSynchronously

            let tdlist = ss.GetTargetDeviceNodes()
            Assert.True(( tdlist.Length = 1 ))
            Assert.True(( tdlist.[0].TargetDeviceID = tdid ))
            Assert.True(( tdlist.[0].TargetDeviceName = "a001" ))

            let nplist = ( tdlist.[0] :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
            Assert.True(( nplist.Length = 1 ))
            Assert.True(( nplist.[0].NetworkPortal.IdentNumber = ServerStatus_Test1.defaultNP.IdentNumber ))
            Assert.True(( nplist.[0].NetworkPortal.PortNumber = uint16 portalPortNo ))

            let tglist = ( tdlist.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
            Assert.True(( tglist.Length = 1 ))
            Assert.True(( tglist.[0].TargetGroupID = tgid ))
            Assert.True(( tglist.[0].TargetGroupName = "b001" ))

            let tlist = ( tglist.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
            Assert.True(( tlist.Length = 1 ))
            Assert.True(( tlist.[0].Values.IdentNumber = tnodeidx_me.fromPrim 99u ))
            Assert.True(( tlist.[0].Values.TargetName = "c001" ))
            Assert.True(( tlist.[0].Values.LUN = [ lun_me.fromPrim 88UL ] ))

            let lulist = tglist.[0].GetAccessibleLUNodes()
            Assert.True(( lulist.Length = 1 ))
            Assert.True(( lulist.[0].LUN = lun_me.fromPrim 88UL ))

        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.RecogniseTargetGroupConfig_001() =
        let portNo, dname = ServerStatus_Test1.Init "RecogniseTargetGroupConfig_001"
        let tdid = GlbFunc.newTargetDeviceID()
        let tgid = GlbFunc.newTargetGroupID()
        let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let portalPortNo = GlbFunc.nextTcpPortNo()
        ServerStatus_Test1.WriteCtrlConfig dname portNo
        ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo tdid

        let tgConfFName = Functions.AppendPathName tdConfDName ( tgid_me.toString tgid )
        let tgConf : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid;
            TargetGroupName = "b001";
            EnabledAtStart = false;
            Target = [{
                IdentNumber = tnodeidx_me.fromPrim 99u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetName = "c001";
                TargetAlias = "";
                LUN = [ lun_me.fromPrim 1UL; lun_me.fromPrim 2UL; lun_me.fromPrim 3UL; lun_me.fromPrim 4UL ];
                Auth = TargetGroupConf.U_None();
            }];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "";
                    WorkPath = "";
                    LUDevice = TargetGroupConf.U_BlockDevice({
                        Peripheral = TargetGroupConf.U_PlainFile({
                            IdentNumber = mediaidx_me.fromPrim 1u;
                            MediaName = "";
                            FileName = "aaa";
                            MaxMultiplicity = Constants.PLAINFILE_MIN_MAXMULTIPLICITY;
                            QueueWaitTimeOut = Constants.PLAINFILE_MIN_QUEUEWAITTIMEOUT;
                            WriteProtect = false;
                        });
                    });
                };
                {
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "";
                    WorkPath = "";
                    LUDevice = TargetGroupConf.U_BlockDevice({
                        Peripheral = TargetGroupConf.U_DummyMedia({
                            IdentNumber = mediaidx_me.fromPrim 2u;
                            MediaName = "";
                        });
                    });
                }
                {
                    LUN = lun_me.fromPrim 3UL;
                    LUName = "";
                    WorkPath = "";
                    LUDevice = TargetGroupConf.U_BlockDevice({
                        Peripheral = TargetGroupConf.U_MemBuffer({
                            IdentNumber = mediaidx_me.fromPrim 3u;
                            MediaName = "";
                            BytesCount = Constants.MEDIA_BLOCK_SIZE;
                        });
                    });
                }
                {
                    LUN = lun_me.fromPrim 4UL;
                    LUName = "";
                    WorkPath = "";
                    LUDevice = TargetGroupConf.U_BlockDevice({
                        Peripheral = TargetGroupConf.U_DebugMedia({
                            IdentNumber = mediaidx_me.fromPrim 4u;
                            MediaName = "";
                            Peripheral = TargetGroupConf.U_DummyMedia({
                                IdentNumber = mediaidx_me.fromPrim 5u;
                                MediaName = "";
                            });
                        });
                    });
                }
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile tgConfFName tgConf
        let luDName = Functions.AppendPathName tdConfDName ( lun_me.WorkDirName tgConf.LogicalUnit.[0].LUN )
        GlbFunc.CreateDir luDName |> ignore

        let killer = new HKiller() :> IKiller
        let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
        ctrl.LoadInitialTargetDeviceProcs()
        ctrl.WaitRequest()

        try

            let st = new StringTable( "" )
            let ss = new ServerStatus( st )
            use cc1 =
                CtrlConnection.Connect st "::1" portNo false
                |> Functions.RunTaskSynchronously

            ss.LoadConfigure cc1 true
            |> Functions.RunTaskSynchronously

            let tdlist = ss.GetTargetDeviceNodes()
            Assert.True(( tdlist.Length = 1 ))
            Assert.True(( tdlist.[0].TargetDeviceID = tdid ))
            Assert.True(( tdlist.[0].TargetDeviceName = "a001" ))

            let nplist = ( tdlist.[0] :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
            Assert.True(( nplist.Length = 1 ))
            Assert.True(( nplist.[0].NetworkPortal.IdentNumber = ServerStatus_Test1.defaultNP.IdentNumber ))
            Assert.True(( nplist.[0].NetworkPortal.PortNumber = uint16 portalPortNo ))

            let tglist = ( tdlist.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
            Assert.True(( tglist.Length = 1 ))
            Assert.True(( tglist.[0].TargetGroupID = tgid ))
            Assert.True(( tglist.[0].TargetGroupName = "b001" ))

            let tlist = ( tglist.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
            Assert.True(( tlist.Length = 1 ))
            Assert.True(( tlist.[0].Values.IdentNumber = tnodeidx_me.fromPrim 99u ))
            Assert.True(( tlist.[0].Values.TargetName = "c001" ))
            Assert.True(( tlist.[0].Values.LUN = [ lun_me.fromPrim 1UL; lun_me.fromPrim 2UL; lun_me.fromPrim 3UL; lun_me.fromPrim 4UL ] ))

            let lulist =
                tglist.[0].GetAccessibleLUNodes()
                |> List.sortBy ( fun itr -> itr.LUN )
            Assert.True(( lulist.Length = 4 ))
            Assert.True(( lulist.[0].LUN = lun_me.fromPrim 1UL ))
            Assert.True(( lulist.[1].LUN = lun_me.fromPrim 2UL ))
            Assert.True(( lulist.[2].LUN = lun_me.fromPrim 3UL ))
            Assert.True(( lulist.[3].LUN = lun_me.fromPrim 4UL ))

            let medialist1 = lulist.[0].GetDescendantNodes<IMediaNode>()
            Assert.True(( medialist1.Length = 1 ))

            match medialist1.[0] with
            | :? ConfNode_PlainFileMedia ->
                match medialist1.[0].MediaConfData with
                | TargetGroupConf.U_PlainFile( x ) ->
                    Assert.True(( x.FileName = "aaa" ))
                | _ ->
                    Assert.Fail __LINE__
            | _ ->
                Assert.Fail __LINE__
            Assert.True(( lulist.[1].LUN = lun_me.fromPrim 2UL ))

            let medialist2 = lulist.[1].GetDescendantNodes<IMediaNode>()
            Assert.True(( medialist2.Length = 1 ))

            match medialist2.[0] with
            | :? ConfNode_DummyMedia ->
                ()
            | _ ->
                Assert.Fail __LINE__

            let medialist3 = lulist.[2].GetDescendantNodes<IMediaNode>()
            Assert.True(( medialist3.Length = 1 ))

            match medialist3.[0] with
            | :? ConfNode_MemBufferMedia as x ->
                Assert.True(( x.Values.BytesCount = Constants.MEDIA_BLOCK_SIZE )) 
            | _ ->
                Assert.Fail __LINE__

            let medialist4 = lulist.[3].GetDescendantNodes<IMediaNode>()
            Assert.True(( medialist4.Length = 2 ))

            match medialist4.[0] with
            | :? ConfNode_DebugMedia as x ->
                Assert.True(( ( x :> IMediaNode ).IdentNumber = mediaidx_me.fromPrim 4u )) 
            | _ ->
                Assert.Fail __LINE__

        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.RecogniseTargetGroupConfig_002() =
        let portNo, dname = ServerStatus_Test1.Init "RecogniseTargetGroupConfig_002"
        let tdid = GlbFunc.newTargetDeviceID()
        let tgid = GlbFunc.newTargetGroupID()
        let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let portalPortNo = GlbFunc.nextTcpPortNo()
        ServerStatus_Test1.WriteCtrlConfig dname portNo
        ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo tdid

        let tgConfFName = Functions.AppendPathName tdConfDName ( tgid_me.toString tgid )
        let tgConf : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid;
            TargetGroupName = "b001";
            EnabledAtStart = false;
            Target = [{
                IdentNumber = tnodeidx_me.fromPrim 99u;
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                TargetName = "c001";
                TargetAlias = "";
                LUN = [ lun_me.fromPrim 1UL ];
                Auth = TargetGroupConf.U_None();
            }];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "";
                    WorkPath = "";
                    LUDevice = TargetGroupConf.U_BlockDevice({
                        Peripheral = TargetGroupConf.U_DummyMedia({
                            IdentNumber = mediaidx_me.fromPrim 1u;
                            MediaName = "";
                        });
                    });
                };
                {
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "";
                    WorkPath = "";
                    LUDevice = TargetGroupConf.U_BlockDevice({
                        Peripheral = TargetGroupConf.U_DummyMedia({
                            IdentNumber = mediaidx_me.fromPrim 2u;
                            MediaName = "";
                        });
                    });
                }
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile tgConfFName tgConf
        let luDName = Functions.AppendPathName tdConfDName ( lun_me.WorkDirName tgConf.LogicalUnit.[0].LUN )
        GlbFunc.CreateDir luDName |> ignore

        let killer = new HKiller() :> IKiller
        let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
        ctrl.LoadInitialTargetDeviceProcs()
        ctrl.WaitRequest()

        try
            task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let tdlist = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist.Length = 1 ))
                let tglist = ( tdlist.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tglist.Length = 1 ))
                let tlist = ( tglist.[0]  :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                Assert.True(( tlist.Length = 1 ))
                Assert.True(( tlist.[0].Values.IdentNumber = tnodeidx_me.fromPrim 99u ))
                Assert.True(( tlist.[0].Values.TargetName = "c001" ))
                Assert.True(( tlist.[0].Values.LUN = [ lun_me.fromPrim 1UL ] ))
                let lulist =
                    tglist.[0].GetAccessibleLUNodes()
                    |> List.sortBy ( fun itr -> itr.LUN )
                Assert.True(( lulist.Length = 1 ))
                Assert.True(( lulist.[0].LUN = lun_me.fromPrim 1UL ))
                let medialist2 = lulist.[0].GetDescendantNodes<IMediaNode>()
                Assert.True(( medialist2.Length = 1 ))
                Assert.True(( medialist2.[0].IdentNumber = mediaidx_me.fromPrim 1u ))
                match medialist2.[0] with
                | :? ConfNode_DummyMedia ->
                    ()
                | _ ->
                    Assert.Fail __LINE__

            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.RecogniseTargetGroupConfig_003() =
        let portNo, dname = ServerStatus_Test1.Init "RecogniseTargetGroupConfig_003"
        let tdid = GlbFunc.newTargetDeviceID()
        let tgid = GlbFunc.newTargetGroupID()
        let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let portalPortNo = GlbFunc.nextTcpPortNo()
        ServerStatus_Test1.WriteCtrlConfig dname portNo
        ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo tdid

        let tgConfFName = Functions.AppendPathName tdConfDName ( tgid_me.toString tgid )
        let tgConf : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid;
            TargetGroupName = "b001";
            EnabledAtStart = false;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 0u;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "c001";
                    TargetAlias = "";
                    LUN = [ lun_me.fromPrim 1UL ];
                    Auth = TargetGroupConf.U_None();
                };
                {
                    IdentNumber = tnodeidx_me.fromPrim 1u;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "c002";
                    TargetAlias = "";
                    LUN = [ lun_me.fromPrim 1UL ];
                    Auth = TargetGroupConf.U_None();
                };
            ];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "";
                    WorkPath = "";
                    LUDevice = TargetGroupConf.U_BlockDevice({
                        Peripheral = TargetGroupConf.U_DummyMedia({
                            IdentNumber = mediaidx_me.fromPrim 1u;
                            MediaName = "";
                        });
                    });
                };
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile tgConfFName tgConf
        let luDName = Functions.AppendPathName tdConfDName ( lun_me.WorkDirName tgConf.LogicalUnit.[0].LUN )
        GlbFunc.CreateDir luDName |> ignore

        let killer = new HKiller() :> IKiller
        let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
        ctrl.LoadInitialTargetDeviceProcs()
        ctrl.WaitRequest()

        try
            task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let tdlist = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist.Length = 1 ))
                let tglist = ( tdlist.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tglist.Length = 1 ))
                let tlist =
                    ( tglist.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                    |> List.sortBy ( fun itr -> itr.Values.IdentNumber )
                Assert.True(( tlist.Length = 2 ))
                Assert.True(( tlist.[0].Values.IdentNumber = tnodeidx_me.fromPrim 0u ))
                Assert.True(( tlist.[0].Values.TargetName = "c001" ))
                Assert.True(( tlist.[0].Values.LUN = [ lun_me.fromPrim 1UL ] ))

                Assert.True(( tlist.[1].Values.IdentNumber = tnodeidx_me.fromPrim 1u ))
                Assert.True(( tlist.[1].Values.TargetName = "c002" ))
                Assert.True(( tlist.[1].Values.LUN = [ lun_me.fromPrim 1UL ] ))

                let lulist1 =
                    tglist.[0].GetAccessibleLUNodes()
                    |> List.sortBy ( fun itr -> itr.LUN )
                Assert.True(( lulist1.Length = 1 ))
                Assert.True(( lulist1.[0].LUN = lun_me.fromPrim 1UL ))
                let medialist2 = lulist1.[0].GetDescendantNodes<IMediaNode>()
                Assert.True(( medialist2.Length = 1 ))
                Assert.True(( medialist2.[0].IdentNumber = mediaidx_me.fromPrim 1u ))

                let tg_childlu =
                    ( tglist.[0] :> IConfigureNode ).GetChildNodes<ILUNode>()
                    |> Seq.toArray
                Assert.True(( tg_childlu.Length = 0 ))

            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.RecogniseTargetGroupConfig_004() =
        let portNo, dname = ServerStatus_Test1.Init "RecogniseTargetGroupConfig_004"
        let tdid = GlbFunc.newTargetDeviceID()
        let tgid = GlbFunc.newTargetGroupID()
        let tdConfDName = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let portalPortNo = GlbFunc.nextTcpPortNo()
        ServerStatus_Test1.WriteCtrlConfig dname portNo
        ServerStatus_Test1.WriteTargetDeviceConfig dname portalPortNo tdid

        let tgConfFName = Functions.AppendPathName tdConfDName ( tgid_me.toString tgid )
        let tgConf : TargetGroupConf.T_TargetGroup = {
            TargetGroupID = tgid;
            TargetGroupName = "b001";
            EnabledAtStart = false;
            Target = [
                {
                    IdentNumber = tnodeidx_me.fromPrim 0u;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "c001";
                    TargetAlias = "";
                    LUN = [ lun_me.fromPrim 1UL ];
                    Auth = TargetGroupConf.U_None();
                };
                {
                    IdentNumber = tnodeidx_me.fromPrim 1u;
                    TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                    TargetName = "c002";
                    TargetAlias = "";
                    LUN = [ lun_me.fromPrim 1UL; lun_me.fromPrim 2UL; ];
                    Auth = TargetGroupConf.U_None();
                };
            ];
            LogicalUnit = [
                {
                    LUN = lun_me.fromPrim 1UL;
                    LUName = "LU1";
                    WorkPath = "";
                    LUDevice = TargetGroupConf.U_BlockDevice({
                        Peripheral = TargetGroupConf.U_DummyMedia({
                            IdentNumber = mediaidx_me.fromPrim 1u
                            MediaName = "";
                        });
                    });
                };
                {
                    LUN = lun_me.fromPrim 2UL;
                    LUName = "LU2";
                    WorkPath = "";
                    LUDevice = TargetGroupConf.U_BlockDevice({
                        Peripheral = TargetGroupConf.U_DummyMedia({
                            IdentNumber = mediaidx_me.fromPrim 2u
                            MediaName = "";
                        });
                    });
                };
            ];
        }
        TargetGroupConf.ReaderWriter.WriteFile tgConfFName tgConf
        let luDName = Functions.AppendPathName tdConfDName ( lun_me.WorkDirName tgConf.LogicalUnit.[0].LUN )
        GlbFunc.CreateDir luDName |> ignore

        let killer = new HKiller() :> IKiller
        let ctrl = new Controller( dname, killer, GlbFunc.tdExePath, GlbFunc.imExePath )
        ctrl.LoadInitialTargetDeviceProcs()
        ctrl.WaitRequest()

        try
            task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 false
                let tdlist = ss.GetTargetDeviceNodes()
                Assert.True(( tdlist.Length = 1 ))
                let tglist = ( tdlist.[0] :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                Assert.True(( tglist.Length = 1 ))
                let tlist =
                    ( tglist.[0] :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                    |> List.sortBy ( fun itr -> itr.Values.IdentNumber )
                Assert.True(( tlist.Length = 2 ))

                let t1lus = ( tlist.[0] :> IConfigureNode ).GetChildNodes<ILUNode>()
                Assert.True(( t1lus.Length = 1 ))
                Assert.True(( t1lus.[0].LUName = "LU1" ))

                let t2lus = ( tlist.[1] :> IConfigureNode ).GetChildNodes<ILUNode>()
                Assert.True(( t2lus.Length = 2 ))
                Assert.True(( t2lus.[0].LUName = "LU1" ))
                Assert.True(( t2lus.[1].LUName = "LU2" ))

                let tg_alllus = ( tglist.[0] :> IConfigureNode ).GetDescendantNodes<ILUNode>()
                Assert.True(( tg_alllus.Length = 2 ))

                let tg_childlu =
                    ( tglist.[0] :> IConfigureNode ).GetChildNodes<ILUNode>()
                    |> Seq.toArray
                Assert.True(( tg_childlu.Length = 2 ))
                Assert.True(( tg_childlu.[0].LUName = "LU1" ))
                Assert.True(( tg_childlu.[1].LUName = "LU2" ))

            }
            |> Functions.RunTaskSynchronously
            |> ignore
        finally
            killer.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Validate_001() =
        let portNo, dname = ServerStatus_Test1.Init "Validate_001"

        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo false
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let r = ss.Validate()
                Assert.True(( r.Length = 0 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Validate_002() =
        let portNo, dname = ServerStatus_Test1.Init "Validate_002"

        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo false
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true
                let n = ss.AddTargetDeviceNode ( GlbFunc.newTargetDeviceID() ) "a" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP :> IConfigureNode
                let r = ss.Validate()
                Assert.True(( r.Length > 0 ))
                Assert.True(( ( fst r.[0] ) = n.NodeID ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetNode_001() =
        let portNo, dname = ServerStatus_Test1.Init "GetNode_001"

        [|
            fun () -> task {
                let! sl, c, sessID, tdid, tgid = ServerStatus_Test1.StubLoginAndInit portNo false
                GlbFunc.ClosePorts [| c |]
                sl.Stop()
            };
            fun () -> task {
                let st = new StringTable( "" )
                let ss = new ServerStatus( st )
                use! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! ss.LoadConfigure cc1 true

                let ctrlNode1 = ss.GetNode( ss.ControllerNodeID )
                match ctrlNode1 with
                | :? ConfNode_Controller -> ()
                | _ -> Assert.Fail __LINE__

                let ctrlNode2 = ss.ControllerNode
                Assert.True(( ctrlNode1 = ctrlNode2 ))

                Assert.True(( ss.GetTargetDeviceNodes().Length = 0 ))

                let tdNode1 = ss.AddTargetDeviceNode ( GlbFunc.newTargetDeviceID() ) "a" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP :> IConfigureNode
                Assert.True(( ss.GetNode( tdNode1.NodeID ) = tdNode1 ))

                let tdList1 = ss.GetTargetDeviceNodes()
                Assert.True(( tdList1.Length = 1 ))
                Assert.True(( tdList1.[0] = ( tdNode1 :?> ConfNode_TargetDevice ) ))

                let tgNode1 = ss.AddTargetGroupNode tdList1.[0] ( GlbFunc.newTargetGroupID() ) "b" false :> IConfigureNode
                Assert.True(( ss.GetNode( tgNode1.NodeID ) = tgNode1 ))

                let tdList2 = ss.GetTargetDeviceNodes()
                Assert.True(( tdList2.Length = 1 ))
                Assert.True(( tdList2.[0] = ( tdNode1 :?> ConfNode_TargetDevice ) ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.ExportTemporaryDump_001() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        try
            ss.ExportTemporaryDump ( confnode_me.fromPrim 1UL ) true |> ignore
            Assert.Fail __LINE__
        with
        | _ -> ()

    [<Fact>]
    member _.ExportTemporaryDump_002() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.AddNode cc
        let te = ss.ExportTemporaryDump ss.ControllerNodeID true
        let t = TempExport.ReaderWriter.LoadString te
        Assert.True(( t.AppVersion.Major = Constants.MAJOR_VERSION ))
        Assert.True(( t.AppVersion.Minor = Constants.MINOR_VERSION ))
        Assert.True(( t.AppVersion.Rivision = Constants.PRODUCT_RIVISION ))
        Assert.True(( t.RootNode = confnode_me.toPrim ss.ControllerNodeID ))
        Assert.True(( t.Relationship.Length = 1 ))
        Assert.True(( t.Relationship.[0].NodeID = confnode_me.toPrim ss.ControllerNodeID ))
        Assert.True(( t.Relationship.[0].Child.Length = 0 ))
        Assert.True(( t.Node.Length = 1 ))
        Assert.True(( t.Node.[0].TypeName = ClientConst.TEMPEXP_NN_Controller ))
        Assert.True(( t.Node.[0].NodeID = confnode_me.toPrim ss.ControllerNodeID ))

    [<Fact>]
    member _.ExportTemporaryDump_003() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc
        let tdnode = ss.AddTargetDeviceNode ( GlbFunc.newTargetDeviceID() ) "" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP
        let npnode = ss.AddNetworkPortalNode tdnode ServerStatus_Test1.defaultNP

        let te = ss.ExportTemporaryDump ( tdnode :> IConfigureNode ).NodeID true
        let t = TempExport.ReaderWriter.LoadString te

        let tdnode_id = ( tdnode :> IConfigureNode ).NodeID |> confnode_me.toPrim
        let npnode_id = ( npnode :> IConfigureNode ).NodeID |> confnode_me.toPrim

        Assert.True(( t.RootNode = tdnode_id ))
        Assert.True(( t.Relationship.Length = 2 ))
        if t.Relationship.[0].NodeID = tdnode_id then
            Assert.True(( t.Relationship.[0].Child.Length = 1 ))
            Assert.True(( t.Relationship.[0].Child.[0] = npnode_id ))
            Assert.True(( t.Relationship.[1].NodeID = npnode_id ))
            Assert.True(( t.Relationship.[1].Child.Length = 0 ))
        else
            Assert.True(( t.Relationship.[0].NodeID = npnode_id ))
            Assert.True(( t.Relationship.[0].Child.Length = 0 ))
            Assert.True(( t.Relationship.[1].NodeID = tdnode_id ))
            Assert.True(( t.Relationship.[1].Child.Length = 1 ))
            Assert.True(( t.Relationship.[1].Child.[0] = npnode_id ))

        Assert.True(( t.Node.Length = 2 ))

        if t.Node.[0].TypeName = ClientConst.TEMPEXP_NN_TargetDevice then
            Assert.True(( t.Node.[0].NodeID = tdnode_id ))
            Assert.True(( t.Node.[1].TypeName = ClientConst.TEMPEXP_NN_NetworkPortal ))
            Assert.True(( t.Node.[1].NodeID = npnode_id ))
        else
            Assert.True(( t.Node.[0].TypeName = ClientConst.TEMPEXP_NN_NetworkPortal ))
            Assert.True(( t.Node.[0].NodeID = npnode_id ))
            Assert.True(( t.Node.[1].TypeName = ClientConst.TEMPEXP_NN_TargetDevice ))
            Assert.True(( t.Node.[1].NodeID = tdnode_id ))

    [<Fact>]
    member _.ExportTemporaryDump_004() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc
        let tdnode = ss.AddTargetDeviceNode ( GlbFunc.newTargetDeviceID() ) "" ServerStatus_Test1.defaultNego ServerStatus_Test1.defaultLP
        let _ = ss.AddNetworkPortalNode tdnode ServerStatus_Test1.defaultNP
        
        let te = ss.ExportTemporaryDump ( cc :> IConfigureNode ).NodeID false
        let t = TempExport.ReaderWriter.LoadString te

        let cnodeid = ( cc :> IConfigureNode ).NodeID |> confnode_me.toPrim

        Assert.True(( t.RootNode = cnodeid ))
        Assert.True(( t.Relationship.Length = 1 ))
        Assert.True(( t.Relationship.[0].NodeID = cnodeid ))
        Assert.True(( t.Relationship.[0].Child.Length = 0 ))

        Assert.True(( t.Node.Length = 1 ))
        Assert.True(( t.Node.[0].TypeName = ClientConst.TEMPEXP_NN_Controller ))
        Assert.True(( t.Node.[0].NodeID = cnodeid ))

    [<Fact>]
    member _.ImportTemporaryDump_001() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        try
            let _ = ss.ImportTemporaryDump "aaaaaa" ss.ControllerNodeID true
            Assert.Fail __LINE__
        with
        | :? XmlException -> ()

    [<Fact>]
    member _.ImportTemporaryDump_002() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION + 1u;
                Minor = Constants.MINOR_VERSION;
                Rivision = Constants.PRODUCT_RIVISION;
            };
            RootNode = 2UL;
            Relationship = [{
                NodeID = 2UL;
                Child = [];
            }];
            Node = [{
                TypeName = ClientConst.TEMPEXP_NN_TargetDevice;
                NodeID = 2UL;
                Values = [];
            }]
        }
        let s = TempExport.ReaderWriter.ToString t
        try
            let _ = ss.ImportTemporaryDump s ss.ControllerNodeID true
            Assert.Fail __LINE__
        with
        | :? EditError as x ->
            Assert.True(( x.Message.StartsWith "ERRMSG_TEMP_EXPORT_VERSION_MISMATCH" ))

    [<Fact>]
    member _.ImportTemporaryDump_003() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION;
                Minor = Constants.MINOR_VERSION + 1u;
                Rivision = Constants.PRODUCT_RIVISION;
            };
            RootNode = 2UL;
            Relationship = [{
                NodeID = 2UL;
                Child = [];
            }];
            Node = [{
                TypeName = ClientConst.TEMPEXP_NN_TargetDevice;
                NodeID = 2UL;
                Values = [];
            }]
        }
        let s = TempExport.ReaderWriter.ToString t
        try
            let _ = ss.ImportTemporaryDump s ss.ControllerNodeID true
            Assert.Fail __LINE__
        with
        | :? EditError as x ->
            Assert.True(( x.Message.StartsWith "ERRMSG_TEMP_EXPORT_VERSION_MISMATCH" ))

    static member m_ImportTemporaryDump_004_data = [|
        [| ClientConst.TEMPEXP_NN_Controller :> obj; "Haruka.Client.ConfNode_Controller" :> obj; |];
        [| ClientConst.TEMPEXP_NN_TargetDevice :> obj; "Haruka.Client.ConfNode_TargetDevice" :> obj; |];
        [| ClientConst.TEMPEXP_NN_NetworkPortal :> obj; "Haruka.Client.ConfNode_NetworkPortal" :> obj; |];
        [| ClientConst.TEMPEXP_NN_TargetGroup :> obj; "Haruka.Client.ConfNode_TargetGroup" :> obj; |];
        [| ClientConst.TEMPEXP_NN_Target :> obj; "Haruka.Client.ConfNode_Target" :> obj; |];
        [| ClientConst.TEMPEXP_NN_DummyDeviceLU :> obj; "Haruka.Client.ConfNode_DummyDeviceLU" :> obj; |];
        [| ClientConst.TEMPEXP_NN_BlockDeviceLU :> obj; "Haruka.Client.ConfNode_BlockDeviceLU" :> obj; |];
        [| ClientConst.TEMPEXP_NN_PlainFileMedia :> obj; "Haruka.Client.ConfNode_PlainFileMedia" :> obj; |];
        [| ClientConst.TEMPEXP_NN_MemBufferMedia :> obj; "Haruka.Client.ConfNode_MemBufferMedia" :> obj; |];
        [| ClientConst.TEMPEXP_NN_DummyMedia :> obj; "Haruka.Client.ConfNode_DummyMedia" :> obj; |];
        [| ClientConst.TEMPEXP_NN_DebugMedia :> obj; "Haruka.Client.ConfNode_DebugMedia" :> obj; |];
    |]

    [<Theory>]
    [<MemberData( "m_ImportTemporaryDump_004_data" )>]
    member _.ImportTemporaryDump_004 ( tempExpName : string ) ( resultTypeName : string ) =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION;
                Minor = Constants.MINOR_VERSION;
                Rivision = Constants.PRODUCT_RIVISION + 1UL;
            };
            RootNode = 2UL;
            Relationship = [{
                NodeID = 2UL;
                Child = [];
            }];
            Node = [{
                TypeName = tempExpName;
                NodeID = 2UL;
                Values = [];
            }]
        }
        let s = TempExport.ReaderWriter.ToString t
        let newnode = ss.ImportTemporaryDump s ss.ControllerNodeID true

        let cn = ( ss.ControllerNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
        Assert.True(( cn.Length = 1 ))
        Assert.True(( Functions.IsSame cn.[0] newnode ))
        Assert.True(( newnode.GetType().ToString() = resultTypeName ))

    [<Fact>]
    member _.ImportTemporaryDump_005() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION;
                Minor = Constants.MINOR_VERSION;
                Rivision = Constants.PRODUCT_RIVISION + 1UL;
            };
            RootNode = 2UL;
            Relationship = [{
                NodeID = 2UL;
                Child = [];
            }];
            Node = [{
                TypeName = "aaaa";
                NodeID = 2UL;
                Values = [];
            }]
        }
        let s = TempExport.ReaderWriter.ToString t
        try
            let _ = ss.ImportTemporaryDump s ss.ControllerNodeID true
            Assert.Fail __LINE__
        with
        | :? EditError as x ->
            Assert.True(( x.Message.StartsWith "ERRMSG_TEMP_EXPORT_MISSING_ROOT" ))

    [<Fact>]
    member _.ImportTemporaryDump_006() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION;
                Minor = Constants.MINOR_VERSION;
                Rivision = Constants.PRODUCT_RIVISION + 1UL;
            };
            RootNode = 2UL;
            Relationship = [{
                NodeID = 2UL;
                Child = [];
            }];
            Node = [{
                TypeName = ClientConst.TEMPEXP_NN_Controller;
                NodeID = 2UL;
                Values = [];
            }]
        }
        let s = TempExport.ReaderWriter.ToString t
        try
            let _ = ss.ImportTemporaryDump s ( confnode_me.fromPrim 99UL ) true
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException ->
            ()

    [<Fact>]
    member _.ImportTemporaryDump_007() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION;
                Minor = Constants.MINOR_VERSION;
                Rivision = Constants.PRODUCT_RIVISION + 1UL;
            };
            RootNode = 102UL;
            Relationship = [
                {
                    NodeID = 102UL;
                    Child = [ 103UL; 104UL; ];
                }
                {
                    NodeID = 103UL;
                    Child = [];
                }
                {
                    NodeID = 104UL;
                    Child = [];
                }
            ];
            Node = [
                {
                    TypeName = ClientConst.TEMPEXP_NN_TargetDevice;
                    NodeID = 102UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 103UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 104UL;
                    Values = [];
                }
            ]
        }
        let s = TempExport.ReaderWriter.ToString t
        let tdnode = ss.ImportTemporaryDump s ss.ControllerNodeID true
        
        let cn = ( ss.ControllerNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
        Assert.True(( cn.Length = 1 ))
        Assert.True(( Functions.IsSame cn.[0] tdnode ))
        match tdnode with
        | :? ConfNode_TargetDevice -> ()
        | _ -> Assert.Fail __LINE__

        let npnodes = tdnode.GetChildNodes<IConfigureNode>()
        Assert.True(( npnodes.Length = 2 ))
        for i in npnodes do
            match i with
            | :? ConfNode_NetworkPortal -> ()
            | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.ImportTemporaryDump_008() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION;
                Minor = Constants.MINOR_VERSION;
                Rivision = Constants.PRODUCT_RIVISION + 1UL;
            };
            RootNode = 102UL;
            Relationship = [
                {
                    NodeID = 102UL;
                    Child = [ 103UL; 104UL; ];
                }
                {
                    NodeID = 104UL;
                    Child = [];
                }
            ];
            Node = [
                {
                    TypeName = ClientConst.TEMPEXP_NN_TargetDevice;
                    NodeID = 102UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 103UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 104UL;
                    Values = [];
                }
            ]
        }
        let s = TempExport.ReaderWriter.ToString t
        let tdnode = ss.ImportTemporaryDump s ss.ControllerNodeID true
        
        let cn = ( ss.ControllerNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
        Assert.True(( cn.Length = 1 ))
        Assert.True(( Functions.IsSame cn.[0] tdnode ))
        match tdnode with
        | :? ConfNode_TargetDevice -> ()
        | _ -> Assert.Fail __LINE__

        let npnodes = tdnode.GetChildNodes<IConfigureNode>()
        Assert.True(( npnodes.Length = 2 ))
        for i in npnodes do
            match i with
            | :? ConfNode_NetworkPortal -> ()
            | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.ImportTemporaryDump_009() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION;
                Minor = Constants.MINOR_VERSION;
                Rivision = Constants.PRODUCT_RIVISION + 1UL;
            };
            RootNode = 102UL;
            Relationship = [
                {
                    NodeID = 102UL;
                    Child = [ 103UL; 104UL; ];
                }
            ];
            Node = [
                {
                    TypeName = ClientConst.TEMPEXP_NN_TargetDevice;
                    NodeID = 102UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 103UL;
                    Values = [];
                }
            ]
        }
        let s = TempExport.ReaderWriter.ToString t
        let tdnode = ss.ImportTemporaryDump s ss.ControllerNodeID true
        
        let cn = ( ss.ControllerNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
        Assert.True(( cn.Length = 1 ))
        Assert.True(( Functions.IsSame cn.[0] tdnode ))
        match tdnode with
        | :? ConfNode_TargetDevice -> ()
        | _ -> Assert.Fail __LINE__

        let npnodes = tdnode.GetChildNodes<IConfigureNode>()
        Assert.True(( npnodes.Length = 1 ))
        match npnodes.[0] with
        | :? ConfNode_NetworkPortal -> ()
        | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.ImportTemporaryDump_010() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION;
                Minor = Constants.MINOR_VERSION;
                Rivision = Constants.PRODUCT_RIVISION + 1UL;
            };
            RootNode = 102UL;
            Relationship = [
                {
                    NodeID = 102UL;
                    Child = [ 103UL; ];
                }
            ];
            Node = [
                {
                    TypeName = ClientConst.TEMPEXP_NN_TargetDevice;
                    NodeID = 102UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 103UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 104UL;
                    Values = [];
                }
            ]
        }
        let s = TempExport.ReaderWriter.ToString t
        let tdnode = ss.ImportTemporaryDump s ss.ControllerNodeID true
        
        let cn = ( ss.ControllerNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
        Assert.True(( cn.Length = 1 ))
        Assert.True(( Functions.IsSame cn.[0] tdnode ))
        match tdnode with
        | :? ConfNode_TargetDevice -> ()
        | _ -> Assert.Fail __LINE__

        let npnodes = tdnode.GetChildNodes<IConfigureNode>()
        Assert.True(( npnodes.Length = 1 ))
        match npnodes.[0] with
        | :? ConfNode_NetworkPortal -> ()
        | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.ImportTemporaryDump_011() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION;
                Minor = Constants.MINOR_VERSION;
                Rivision = Constants.PRODUCT_RIVISION + 1UL;
            };
            RootNode = 102UL;
            Relationship = [
                {
                    NodeID = 102UL;
                    Child = [ 103UL; 104UL; ];
                }
                {
                    NodeID = 103UL;
                    Child = [ 104UL; ];
                }
            ];
            Node = [
                {
                    TypeName = ClientConst.TEMPEXP_NN_TargetDevice;
                    NodeID = 102UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 103UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 104UL;
                    Values = [];
                }
            ]
        }
        let s = TempExport.ReaderWriter.ToString t
        let tdnode = ss.ImportTemporaryDump s ss.ControllerNodeID true
        
        let cn = ( ss.ControllerNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
        Assert.True(( cn.Length = 1 ))
        Assert.True(( Functions.IsSame cn.[0] tdnode ))
        match tdnode with
        | :? ConfNode_TargetDevice -> ()
        | _ -> Assert.Fail __LINE__

        let npnodes = tdnode.GetChildNodes<IConfigureNode>()
        Assert.True(( npnodes.Length = 2 ))
        for i in npnodes do
            match i with
            | :? ConfNode_NetworkPortal -> ()
            | _ -> Assert.Fail __LINE__

        let c1 = npnodes.[0].GetChildNodes<IConfigureNode>()
        let c2 = npnodes.[1].GetChildNodes<IConfigureNode>()
        if c1.Length > 0 then
            Assert.True(( c1.Length = 1 ))
            Assert.True(( c2.Length = 0 ))
            match c1.[0] with
            | :? ConfNode_NetworkPortal -> ()
            | _ -> Assert.Fail __LINE__
        else
            Assert.True(( c1.Length = 0 ))
            Assert.True(( c2.Length = 1 ))
            match c2.[0] with
            | :? ConfNode_NetworkPortal -> ()
            | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.ImportTemporaryDump_012() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION;
                Minor = Constants.MINOR_VERSION;
                Rivision = Constants.PRODUCT_RIVISION + 1UL;
            };
            RootNode = 102UL;
            Relationship = [
                {
                    NodeID = 102UL;
                    Child = [ 103UL; ];
                }
                {
                    NodeID = 103UL;
                    Child = [ 104UL; ];
                }
                {
                    NodeID = 104UL;
                    Child = [ 102UL; ];
                }
            ];
            Node = [
                {
                    TypeName = ClientConst.TEMPEXP_NN_TargetDevice;
                    NodeID = 102UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 103UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 104UL;
                    Values = [];
                }
            ]
        }
        let s = TempExport.ReaderWriter.ToString t
        let tdnode = ss.ImportTemporaryDump s ss.ControllerNodeID true
        
        let cn = ( ss.ControllerNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
        Assert.True(( cn.Length = 1 ))
        Assert.True(( Functions.IsSame cn.[0] tdnode ))
        match tdnode with
        | :? ConfNode_TargetDevice -> ()
        | _ -> Assert.Fail __LINE__

        let npnodes = tdnode.GetChildNodes<IConfigureNode>()
        Assert.True(( npnodes.Length = 1 ))
        match npnodes.[0] with
        | :? ConfNode_NetworkPortal -> ()
        | _ -> Assert.Fail __LINE__

        let c1 = npnodes.[0].GetChildNodes<IConfigureNode>()
        Assert.True(( c1.Length = 1 ))
        match c1.[0] with
        | :? ConfNode_NetworkPortal -> ()
        | _ -> Assert.Fail __LINE__

        let c2 = c1.[0].GetChildNodes<IConfigureNode>()
        Assert.True(( c2.Length = 1 ))
        match c2.[0] with
        | :? ConfNode_TargetDevice -> ()
        | _ -> Assert.Fail __LINE__
        Assert.True(( Functions.IsSame c2[0] tdnode ))

    [<Fact>]
    member _.ImportTemporaryDump_013() =
        let st = new StringTable( "" )
        let ss = new ServerStatus( st )
        let pr = PrivateCaller ss
        let rel = pr.GetField( "m_ConfNodes" ) :?> ConfNodeRelation
        let cc = new ConfNode_Controller( st, rel, ss.ControllerNodeID )
        rel.NextID |> ignore
        rel.AddNode cc

        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION;
                Minor = Constants.MINOR_VERSION;
                Rivision = Constants.PRODUCT_RIVISION;
            };
            RootNode = 102UL;
            Relationship = [
                {
                    NodeID = 102UL;
                    Child = [ 103UL; 104UL; ];
                }
            ];
            Node = [
                {
                    TypeName = ClientConst.TEMPEXP_NN_TargetDevice;
                    NodeID = 102UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 103UL;
                    Values = [];
                }
                {
                    TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                    NodeID = 104UL;
                    Values = [];
                }
            ]
        }
        let s = TempExport.ReaderWriter.ToString t
        let tdnode = ss.ImportTemporaryDump s ss.ControllerNodeID false
        
        let cn = ( ss.ControllerNode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
        Assert.True(( cn.Length = 1 ))
        Assert.True(( Functions.IsSame cn.[0] tdnode ))
        match tdnode with
        | :? ConfNode_TargetDevice -> ()
        | _ -> Assert.Fail __LINE__

        let npnodes = tdnode.GetChildNodes<IConfigureNode>()
        Assert.True(( npnodes.Length = 0 ))







