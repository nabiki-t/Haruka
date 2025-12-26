//=============================================================================
// Haruka Software Storage.
// CtrlConnectionTest2.fs : Test cases for CtrlConnection class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Client

//=============================================================================
// Import declaration

open System
open System.IO

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.Controller
open Haruka.IODataTypes
open Haruka.Test

//=============================================================================
// Class implementation

type CtrlConnection_Test2() =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    static member defaultSessParam =
        {
            MaxConnections = Constants.NEGOPARAM_MaxConnections;
            TargetGroupID = tgid_me.Zero;
            TargetConf = {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetName = "target001001";
                TargetAlias = "";
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            };
            InitiatorName = "initiator001";
            InitiatorAlias = ""
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            InitialR2T = false;
            ImmediateData = false;
            MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength;
            FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength;
            DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait;
            DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain;
            MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
            DataPDUInOrder = true;
            DataSequenceInOrder = true;
            ErrorRecoveryLevel = 0uy;
        }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.CreateTargetGroupConfig_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetGroupConfig_001"
        let wtgid1 = GlbFunc.newTargetGroupID()
        let wtgconf1 : TargetGroupConf.T_TargetGroup =
            {
                TargetGroupID = wtgid1;
                TargetGroupName = "aaaaaaaaaa";
                EnabledAtStart = false;
                Target = [
                    {
                        IdentNumber = tnodeidx_me.fromPrim 0u;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "abcd";
                        TargetAlias = "eee";
                        LUN = [ lun_me.fromPrim 1UL; ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }
                ];
                LogicalUnit = [
                    {
                        LUN = lun_me.fromPrim 1UL;
                        LUName = "";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    }
                ];
            }

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetGroupConfig request
                let! createTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let createTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetGroupConfigRequestStr
                match createTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = wtgid1 ))
                    Assert.True(( x.Config = TargetGroupConf.ReaderWriter.ToString wtgconf1 ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_CreateTargetGroupConfigResult({
                            TargetDeviceID = tdid;
                            TargetGroupID = wtgid1;
                            Result = false;
                            ErrorMessage = "aaaaaaaaa5";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.CreateTargetGroupConfig tdid wtgconf1
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaa5" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetGroupConfig_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetGroupConfig_002"
        let wtgid1 = GlbFunc.newTargetGroupID()
        let wtgconf1 : TargetGroupConf.T_TargetGroup =
            {
                TargetGroupID = wtgid1;
                TargetGroupName = "aaaaaaaaaa";
                EnabledAtStart = false;
                Target = [
                    {
                        IdentNumber = tnodeidx_me.fromPrim 0u;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "abcd";
                        TargetAlias = "eee";
                        LUN = [ lun_me.fromPrim 1UL; ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }
                ];
                LogicalUnit = [
                    {
                        LUN = lun_me.fromPrim 1UL;
                        LUName = "";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    }
                ];
            }

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetGroupConfig request
                let! createTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let createTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetGroupConfigRequestStr
                match createTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = wtgid1 ))
                    Assert.True(( x.Config = TargetGroupConf.ReaderWriter.ToString wtgconf1 ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_CreateTargetGroupConfigResult({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            TargetGroupID = wtgid1;
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.CreateTargetGroupConfig tdid wtgconf1
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetGroupConfig_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetGroupConfig_003"
        let wtgid1 = GlbFunc.newTargetGroupID()
        let wtgconf1 : TargetGroupConf.T_TargetGroup =
            {
                TargetGroupID = wtgid1;
                TargetGroupName = "aaaaaaaaaa";
                EnabledAtStart = false;
                Target = [
                    {
                        IdentNumber = tnodeidx_me.fromPrim 0u;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "abcd";
                        TargetAlias = "eee";
                        LUN = [ lun_me.fromPrim 1UL; ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }
                ];
                LogicalUnit = [
                    {
                        LUN = lun_me.fromPrim 1UL;
                        LUName = "";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    }
                ];
            }

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetGroupConfig request
                let! createTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let createTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetGroupConfigRequestStr
                match createTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = wtgid1 ))
                    Assert.True(( x.Config = TargetGroupConf.ReaderWriter.ToString wtgconf1 ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_CreateTargetGroupConfigResult({
                            TargetDeviceID = tdid;
                            TargetGroupID = GlbFunc.newTargetGroupID();
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.CreateTargetGroupConfig tdid wtgconf1
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetGroupConfig_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetGroupConfig_004"
        let wtgid1 = GlbFunc.newTargetGroupID()
        let wtgconf1 : TargetGroupConf.T_TargetGroup =
            {
                TargetGroupID = wtgid1;
                TargetGroupName = "aaaaaaaaaa";
                EnabledAtStart = false;
                Target = [
                    {
                        IdentNumber = tnodeidx_me.fromPrim 0u;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "abcd";
                        TargetAlias = "eee";
                        LUN = [ lun_me.fromPrim 1UL; ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }
                ];
                LogicalUnit = [
                    {
                        LUN = lun_me.fromPrim 1UL;
                        LUName = "";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    }
                ];
            }

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetGroupConfig request
                let! createTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let createTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetGroupConfigRequestStr
                match createTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = wtgid1 ))
                    Assert.True(( x.Config = TargetGroupConf.ReaderWriter.ToString wtgconf1 ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "gghhjjj" )
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.CreateTargetGroupConfig tdid wtgconf1
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "gghhjjj" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetGroupConfig_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetGroupConfig_005"
        let wtgid1 = GlbFunc.newTargetGroupID()
        let wtgconf1 : TargetGroupConf.T_TargetGroup =
            {
                TargetGroupID = wtgid1;
                TargetGroupName = "aaaaaaaaaa";
                EnabledAtStart = false;
                Target = [
                    {
                        IdentNumber = tnodeidx_me.fromPrim 0u;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "abcd";
                        TargetAlias = "eee";
                        LUN = [ lun_me.fromPrim 1UL; ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }
                ];
                LogicalUnit = [
                    {
                        LUN = lun_me.fromPrim 1UL;
                        LUName = "";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    }
                ];
            }

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateTargetGroupConfig request
                let! createTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let createTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString createTargetGroupConfigRequestStr
                match createTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = wtgid1 ))
                    Assert.True(( x.Config = TargetGroupConf.ReaderWriter.ToString wtgconf1 ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceConfig({
                            TargetDeviceID = tdid;
                            Config = "";
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.CreateTargetGroupConfig tdid wtgconf1
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetGroupConfig_006() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetGroupConfig_006"
        let wtgid1 = GlbFunc.newTargetGroupID()
        let wtgconf1 : TargetGroupConf.T_TargetGroup =
            {
                TargetGroupID = wtgid1;
                TargetGroupName = ( String.replicate 512 "a" );
                EnabledAtStart = false;
                Target = [];
                LogicalUnit = [];
            }

        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.CreateTargetGroupConfig tdid wtgconf1
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    ()
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateTargetGroupConfig_007() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateTargetGroupConfig_007"
        let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
        Directory.CreateDirectory wdname |> ignore
        let wtgid1 = GlbFunc.newTargetGroupID()
        let wtgconf1 : TargetGroupConf.T_TargetGroup =
            {
                TargetGroupID = wtgid1;
                TargetGroupName = "aaaaaaaaaa";
                EnabledAtStart = false;
                Target = [
                    {
                        IdentNumber = tnodeidx_me.fromPrim 0u;
                        TargetPortalGroupTag = tpgt_me.zero;
                        TargetName = "abcd";
                        TargetAlias = "eee";
                        LUN = [ lun_me.fromPrim 1UL; ];
                        Auth = TargetGroupConf.T_Auth.U_None();
                    }
                ];
                LogicalUnit = [
                    {
                        LUN = lun_me.fromPrim 1UL;
                        LUName = "";
                        WorkPath = "";
                        MaxMultiplicity = Constants.LU_DEF_MULTIPLICITY;
                        LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
                    }
                ];
            }
        let tgconfName = Functions.AppendPathName wdname ( tgid_me.toString wtgid1 )

        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false

                Assert.False(( File.Exists tgconfName ))

                do! cc1.CreateTargetGroupConfig tdid wtgconf1
                k.NoticeTerminate()

                Assert.True(( File.Exists tgconfName ))
                let resultConf = TargetGroupConf.ReaderWriter.LoadFile tgconfName
                Assert.True(( resultConf = wtgconf1 ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetGroupConfig_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteTargetGroupConfig_001"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteTargetGroupConfig request
                let! deleteTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let deleteTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteTargetGroupConfigRequestStr
                match deleteTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = tgid ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetGroupConfigResult({
                            TargetDeviceID = tdid;
                            TargetGroupID = tgid;
                            Result = false;
                            ErrorMessage = "aaaaaaaaab";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteTargetGroupConfig tdid tgid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaaaaab" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetGroupConfig_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteTargetGroupConfig_002"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteTargetGroupConfig request
                let! deleteTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let deleteTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteTargetGroupConfigRequestStr
                match deleteTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = tgid ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetGroupConfigResult({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            TargetGroupID = tgid;
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteTargetGroupConfig tdid tgid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetGroupConfig_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteTargetGroupConfig_003"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteTargetGroupConfig request
                let! deleteTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let deleteTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteTargetGroupConfigRequestStr
                match deleteTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = tgid ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetGroupConfigResult({
                            TargetDeviceID = tdid;
                            TargetGroupID = GlbFunc.newTargetGroupID();
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteTargetGroupConfig tdid tgid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetGroupConfig_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteTargetGroupConfig_004"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteTargetGroupConfig request
                let! deleteTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let deleteTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteTargetGroupConfigRequestStr
                match deleteTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = tgid ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "amnjuvdrgdfgv" )
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteTargetGroupConfig tdid tgid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "amnjuvdrgdfgv" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetGroupConfig_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteTargetGroupConfig_005"
        let tgid = GlbFunc.newTargetGroupID()
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteTargetGroupConfig request
                let! deleteTargetGroupConfigRequestStr = Functions.FramingReceiver c 
                let deleteTargetGroupConfigRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteTargetGroupConfigRequestStr
                match deleteTargetGroupConfigRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteTargetGroupConfig( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.TargetGroupID = tgid ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteTargetGroupConfig response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_SetControllerConfigResult({
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteTargetGroupConfig tdid tgid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteTargetGroupConfig_006() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteTargetGroupConfig_006"
        let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
        let tgid = GlbFunc.newTargetGroupID()
        let tgconfName = Functions.AppendPathName wdname ( tgid_me.toString tgid )
        Directory.CreateDirectory wdname |> ignore
        File.WriteAllBytes( tgconfName, Array.empty )
        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                Assert.True(( File.Exists tgconfName ))
                do! cc1.DeleteTargetGroupConfig tdid tgid
                k.NoticeTerminate()
                Assert.False(( File.Exists tgconfName ))
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLUWorkDir_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLUWorkDir_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetLUWorkDir request
                let! getLUWorkDirRequestStr = Functions.FramingReceiver c 
                let getLUWorkDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getLUWorkDirRequestStr
                match getLUWorkDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetLUWorkDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetLUWorkDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_LUWorkDirs({
                            TargetDeviceID = tdid;
                            Name = [];
                            ErrorMessage = "aaaaaadaaab";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetLUWorkDir tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaaaaadaaab" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLUWorkDir_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLUWorkDir_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetLUWorkDir request
                let! getLUWorkDirRequestStr = Functions.FramingReceiver c 
                let getLUWorkDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getLUWorkDirRequestStr
                match getLUWorkDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetLUWorkDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetLUWorkDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_LUWorkDirs({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            Name = [];
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetLUWorkDir tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLUWorkDir_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLUWorkDir_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetLUWorkDir request
                let! getLUWorkDirRequestStr = Functions.FramingReceiver c 
                let getLUWorkDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getLUWorkDirRequestStr
                match getLUWorkDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetLUWorkDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetLUWorkDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "bbbvvvv" )
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetLUWorkDir tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "bbbvvvv" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLUWorkDir_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLUWorkDir_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetLUWorkDir request
                let! getLUWorkDirRequestStr = Functions.FramingReceiver c 
                let getLUWorkDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getLUWorkDirRequestStr
                match getLUWorkDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetLUWorkDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetLUWorkDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetGroupConfigResult({
                            TargetDeviceID = tdid;
                            TargetGroupID = GlbFunc.newTargetGroupID();
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetLUWorkDir tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLUWorkDir_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLUWorkDir_005"
        let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
        Directory.CreateDirectory wdname |> ignore
        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetLUWorkDir tdid
                Assert.True(( r = [] ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetLUWorkDir_006() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetLUWorkDir_006"
        let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
        Directory.CreateDirectory wdname |> ignore
        let wlun = lun_me.fromPrim 3UL
        let wlname = Functions.AppendPathName wdname ( lun_me.WorkDirName wlun )
        Directory.CreateDirectory wlname |> ignore
        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetLUWorkDir tdid
                Assert.True(( r = [ wlun ] ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteLUWorkDir_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteLUWorkDir_001"
        let wlun = lun_me.fromPrim 3UL
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteLUWorkDir request
                let! deleteLUWorkDirRequestStr = Functions.FramingReceiver c 
                let deleteLUWorkDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteLUWorkDirRequestStr
                match deleteLUWorkDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteLUWorkDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.LUN = wlun ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteLUWorkDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteLUWorkDirResult({
                            TargetDeviceID = tdid;
                            LUN = wlun;
                            Result = false;
                            ErrorMessage = "aaadaaadaaab";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteLUWorkDir tdid wlun
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "aaadaaadaaab" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteLUWorkDir_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteLUWorkDir_002"
        let wlun = lun_me.fromPrim 3UL
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteLUWorkDir request
                let! deleteLUWorkDirRequestStr = Functions.FramingReceiver c 
                let deleteLUWorkDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteLUWorkDirRequestStr
                match deleteLUWorkDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteLUWorkDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.LUN = wlun ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteLUWorkDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteLUWorkDirResult({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            LUN = wlun;
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteLUWorkDir tdid wlun
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteLUWorkDir_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteLUWorkDir_003"
        let wlun = lun_me.fromPrim 3UL
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteLUWorkDir request
                let! deleteLUWorkDirRequestStr = Functions.FramingReceiver c 
                let deleteLUWorkDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteLUWorkDirRequestStr
                match deleteLUWorkDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteLUWorkDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.LUN = wlun ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteLUWorkDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteLUWorkDirResult({
                            TargetDeviceID = tdid;
                            LUN = lun_me.fromPrim 4UL;
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteLUWorkDir tdid wlun
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteLUWorkDir_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteLUWorkDir_004"
        let wlun = lun_me.fromPrim 3UL
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteLUWorkDir request
                let! deleteLUWorkDirRequestStr = Functions.FramingReceiver c 
                let deleteLUWorkDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteLUWorkDirRequestStr
                match deleteLUWorkDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteLUWorkDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.LUN = wlun ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteLUWorkDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "QQQQQQQQQQQQQ" )
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteLUWorkDir tdid wlun
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "QQQQQQQQQQQQQ" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteLUWorkDir_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteLUWorkDir_005"
        let wlun = lun_me.fromPrim 3UL
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive DeleteLUWorkDir request
                let! deleteLUWorkDirRequestStr = Functions.FramingReceiver c 
                let deleteLUWorkDirRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString deleteLUWorkDirRequestStr
                match deleteLUWorkDirRequest.Request with
                | HarukaCtrlerCtrlReq.U_DeleteLUWorkDir( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.LUN = wlun ))
                | _ ->
                    Assert.Fail __LINE__

                // send DeleteLUWorkDir response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetGroupConfigResult({
                            TargetDeviceID = tdid;
                            TargetGroupID = GlbFunc.newTargetGroupID();
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.DeleteLUWorkDir tdid wlun
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.DeleteLUWorkDir_006() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "DeleteLUWorkDir_006"
        let wdname = Functions.AppendPathName dname ( tdid_me.toString tdid )
        Directory.CreateDirectory wdname |> ignore
        let wlun = lun_me.fromPrim 3UL
        let wlname = Functions.AppendPathName wdname ( lun_me.WorkDirName wlun )
        Directory.CreateDirectory wlname |> ignore

        [|
            fun () -> task {
                CtrlConnection_Test1.CreateDefaultCtrlConf dname "::1" portNo
                let c = new Controller( dname, k, GlbFunc.tdExePath, GlbFunc.imExePath )
                c.LoadInitialTargetDeviceProcs()
                c.WaitRequest()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                Assert.True(( Directory.Exists wlname ))
                do! cc1.DeleteLUWorkDir tdid wlun
                Assert.False(( Directory.Exists wlname ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceProcs_001() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetTargetDeviceProcs_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceProcs request
                let! getTargetDeviceProcsRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceProcsRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceProcsRequestStr
                match getTargetDeviceProcsRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceProcs( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceProcs response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceProcs({
                            TargetDeviceID = [];
                            ErrorMessage = "WWWWWWWWWWWWW";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetTargetDeviceProcs()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "WWWWWWWWWWWWW" ))
                | _ as x ->
                    Assert.Fail ( __LINE__ + x.Message )

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceProcs_002() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetTargetDeviceProcs_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceProcs request
                let! getTargetDeviceProcsRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceProcsRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceProcsRequestStr
                match getTargetDeviceProcsRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceProcs( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceProcs response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "EEEEEEEEEEEEEEE" )
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetTargetDeviceProcs()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "EEEEEEEEEEEEEEE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceProcs_003() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetTargetDeviceProcs_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceProcs request
                let! getTargetDeviceProcsRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceProcsRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceProcsRequestStr
                match getTargetDeviceProcsRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceProcs( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceProcs response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_DeleteTargetGroupConfigResult({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            TargetGroupID = GlbFunc.newTargetGroupID();
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! r = cc1.GetTargetDeviceProcs()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetTargetDeviceProcs_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetTargetDeviceProcs_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceProcs request
                let! getTargetDeviceProcsRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceProcsRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceProcsRequestStr
                match getTargetDeviceProcsRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceProcs( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceProcs response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceProcs({
                            TargetDeviceID = [];
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetTargetDeviceProcs()
                Assert.True(( r = [] ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname
        
    [<Fact>]
    member _.GetTargetDeviceProcs_005() =
        let portNo, dname, k, st, _ = CtrlConnection_Test1.Init "GetTargetDeviceProcs_005"
        let tdids = [ GlbFunc.newTargetDeviceID(); GlbFunc.newTargetDeviceID(); GlbFunc.newTargetDeviceID(); ]
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetTargetDeviceProcs request
                let! getTargetDeviceProcsRequestStr = Functions.FramingReceiver c 
                let getTargetDeviceProcsRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString getTargetDeviceProcsRequestStr
                match getTargetDeviceProcsRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetTargetDeviceProcs( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send GetTargetDeviceProcs response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceProcs({
                            TargetDeviceID = tdids;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetTargetDeviceProcs()
                Assert.True(( r = tdids ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.KillTargetDeviceProc_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "KillTargetDeviceProc_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive KillTargetDeviceProc request
                let! killTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let killTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString killTargetDeviceProcRequestStr
                match killTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_KillTargetDeviceProc( x ) ->
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.SessionID = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send KillTargetDeviceProc response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_KillTargetDeviceProcResult({
                            TargetDeviceID = tdid;
                            Result = false;
                            ErrorMessage = "RRRRRRRRRRRR";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.KillTargetDeviceProc tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "RRRRRRRRRRRR" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.KillTargetDeviceProc_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "KillTargetDeviceProc_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive KillTargetDeviceProc request
                let! killTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let killTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString killTargetDeviceProcRequestStr
                match killTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_KillTargetDeviceProc( x ) ->
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.SessionID = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send KillTargetDeviceProc response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_KillTargetDeviceProcResult({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.KillTargetDeviceProc tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.KillTargetDeviceProc_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "KillTargetDeviceProc_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive KillTargetDeviceProc request
                let! killTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let killTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString killTargetDeviceProcRequestStr
                match killTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_KillTargetDeviceProc( x ) ->
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.SessionID = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send KillTargetDeviceProc response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "TTTTTTTTTTTTTTTTTT" )
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.KillTargetDeviceProc tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "TTTTTTTTTTTTTTTTTT" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.KillTargetDeviceProc_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "KillTargetDeviceProc_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive KillTargetDeviceProc request
                let! killTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let killTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString killTargetDeviceProcRequestStr
                match killTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_KillTargetDeviceProc( x ) ->
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.SessionID = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send KillTargetDeviceProc response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs({
                            TargetDeviceID = [];
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.KillTargetDeviceProc tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__

                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.KillTargetDeviceProc_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "KillTargetDeviceProc_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive KillTargetDeviceProc request
                let! killTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let killTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString killTargetDeviceProcRequestStr
                match killTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_KillTargetDeviceProc( x ) ->
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.SessionID = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send KillTargetDeviceProc response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_KillTargetDeviceProcResult({
                            TargetDeviceID = tdid;
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.KillTargetDeviceProc tdid
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.StartTargetDeviceProc_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "StartTargetDeviceProc_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive StartTargetDeviceProc request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_StartTargetDeviceProc( x ) ->
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.SessionID = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send StartTargetDeviceProc response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_StartTargetDeviceProcResult({
                            TargetDeviceID = tdid;
                            Result = false;
                            ErrorMessage = "YYYYYYYYYYYYYYYY";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.StartTargetDeviceProc tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "YYYYYYYYYYYYYYYY" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.StartTargetDeviceProc_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "StartTargetDeviceProc_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive StartTargetDeviceProc request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_StartTargetDeviceProc( x ) ->
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.SessionID = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send StartTargetDeviceProc response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_StartTargetDeviceProcResult({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.StartTargetDeviceProc tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.StartTargetDeviceProc_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "StartTargetDeviceProc_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive StartTargetDeviceProc request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_StartTargetDeviceProc( x ) ->
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.SessionID = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send StartTargetDeviceProc response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "QQWWEEERR" )
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.StartTargetDeviceProc tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "QQWWEEERR" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.StartTargetDeviceProc_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "StartTargetDeviceProc_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive StartTargetDeviceProc request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_StartTargetDeviceProc( x ) ->
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.SessionID = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send StartTargetDeviceProc response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_KillTargetDeviceProcResult({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.StartTargetDeviceProc tdid
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.StartTargetDeviceProc_005() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "StartTargetDeviceProc_005"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive StartTargetDeviceProc request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_StartTargetDeviceProc( x ) ->
                    Assert.True(( x.TargetDeviceID = tdid ))
                    Assert.True(( x.SessionID = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send StartTargetDeviceProc response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_StartTargetDeviceProcResult({
                            TargetDeviceID = tdid;
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.StartTargetDeviceProc tdid
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateMediaFile_PlainFile_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateMediaFile_PlainFile_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateMediaFile request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateMediaFile( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    match x.MediaType with
                    | HarukaCtrlerCtrlReq.U_PlainFile( y ) ->
                        Assert.True(( y.FileName = "aaa" ))
                        Assert.True(( y.FileSize = 123L ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateMediaFileResult response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_CreateMediaFileResult({
                            Result = false;
                            ProcID = 0UL;
                            ErrorMessage = "XXYYZZAA";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.CreateMediaFile_PlainFile "aaa" 123L
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "XXYYZZAA" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateMediaFile_PlainFile_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateMediaFile_PlainFile_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateMediaFile request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateMediaFile( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    match x.MediaType with
                    | HarukaCtrlerCtrlReq.U_PlainFile( y ) ->
                        Assert.True(( y.FileName = "aaa1" ))
                        Assert.True(( y.FileSize = 1231L ))
                | _ ->
                    Assert.Fail __LINE__

                // send UnexpectedError response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "XXDFTG" )
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.CreateMediaFile_PlainFile "aaa1" 1231L
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "XXDFTG" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateMediaFile_PlainFile_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateMediaFile_PlainFile_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateMediaFile request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateMediaFile( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    match x.MediaType with
                    | HarukaCtrlerCtrlReq.U_PlainFile( y ) ->
                        Assert.True(( y.FileName = "aaa2" ))
                        Assert.True(( y.FileSize = 1232L ))
                | _ ->
                    Assert.Fail __LINE__

                // send incorrect response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_KillTargetDeviceProcResult({
                            TargetDeviceID = GlbFunc.newTargetDeviceID();
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.CreateMediaFile_PlainFile "aaa2" 1232L
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.CreateMediaFile_PlainFile_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "CreateMediaFile_PlainFile_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive CreateMediaFile request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_CreateMediaFile( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    match x.MediaType with
                    | HarukaCtrlerCtrlReq.U_PlainFile( y ) ->
                        Assert.True(( y.FileName = "aaa2" ))
                        Assert.True(( y.FileSize = 1232L ))
                | _ ->
                    Assert.Fail __LINE__

                // send CreateMediaFileResult response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_CreateMediaFileResult({
                            Result = true;
                            ProcID = 999UL;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.CreateMediaFile_PlainFile "aaa2" 1232L
                Assert.True(( r = 999UL ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetInitMediaStatus_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetInitMediaStatus_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetInitMediaStatus request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetInitMediaStatus( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send InitMediaStatus response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_InitMediaStatus({
                            Procs = [];
                            ErrorMessage = "AADDFFGG";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetInitMediaStatus()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "AADDFFGG" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetInitMediaStatus_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetInitMediaStatus_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetInitMediaStatus request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetInitMediaStatus( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send UnexpectedError response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "GGHHJJ" )
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetInitMediaStatus()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "GGHHJJ" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetInitMediaStatus_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetInitMediaStatus_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetInitMediaStatus request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetInitMediaStatus( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send incorrect response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs({
                            TargetDeviceID = [];
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    let! _ = cc1.GetInitMediaStatus()
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.GetInitMediaStatus_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "GetInitMediaStatus_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive GetInitMediaStatus request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_GetInitMediaStatus( x ) ->
                    Assert.True(( x = sessID ))
                | _ ->
                    Assert.Fail __LINE__

                // send InitMediaStatus response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_InitMediaStatus({
                            Procs = [
                                {
                                    ProcID = 0UL;
                                    PathName = "aaddf";
                                    FileType = "ttgghh";
                                    Status = HarukaCtrlerCtrlRes.T_Status.U_NotStarted();
                                    ErrorMessage = [ "a" ];
                                };
                                {
                                    ProcID = 1UL;
                                    PathName = "ttyyuu";
                                    FileType = "hhjjkk";
                                    Status = HarukaCtrlerCtrlRes.T_Status.U_ProgressCreation( 1uy );
                                    ErrorMessage = [];
                                };
                            ];
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                let! r = cc1.GetInitMediaStatus()
                Assert.True(( r.Length = 2 ))
                Assert.True(( r.[0].ProcID = 0UL ))
                Assert.True(( r.[0].PathName = "aaddf" ))
                Assert.True(( r.[0].FileType = "ttgghh" ))
                Assert.True(( r.[0].Status = HarukaCtrlerCtrlRes.T_Status.U_NotStarted() ))
                Assert.True(( r.[0].ErrorMessage = [ "a" ] ))
                Assert.True(( r.[1].ProcID = 1UL ))
                Assert.True(( r.[1].PathName = "ttyyuu" ))
                Assert.True(( r.[1].FileType = "hhjjkk" ))
                Assert.True(( r.[1].Status = HarukaCtrlerCtrlRes.U_ProgressCreation( 1uy ) ))
                Assert.True(( r.[1].ErrorMessage = [] ))
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.KillInitMediaProc_001() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "KillInitMediaProc_001"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive KillInitMediaProc request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_KillInitMediaProc( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.ProcID = 1UL ))
                | _ ->
                    Assert.Fail __LINE__

                // send KillInitMediaProcResult response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_KillInitMediaProcResult({
                            Result = false;
                            ErrorMessage = "AADDFFGG";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.KillInitMediaProc 1UL
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "AADDFFGG" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.KillInitMediaProc_002() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "KillInitMediaProc_002"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive KillInitMediaProc request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_KillInitMediaProc( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.ProcID = 1UL ))
                | _ ->
                    Assert.Fail __LINE__

                // send UnexpectedError response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_UnexpectedError( "GGHHJJ" )
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.KillInitMediaProc 1UL
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "GGHHJJ" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.KillInitMediaProc_003() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "KillInitMediaProc_003"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive KillInitMediaProc request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_KillInitMediaProc( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.ProcID = 2UL ))
                | _ ->
                    Assert.Fail __LINE__

                // send incorrect response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_TargetDeviceDirs({
                            TargetDeviceID = [];
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                try
                    do! cc1.KillInitMediaProc 2UL
                    Assert.Fail __LINE__
                with
                | :? RequestError as x ->
                    Assert.True(( x.Message.StartsWith "ERRMSG_UNEXPECTED_RESPONSE" ))
                | _ as x ->
                    Assert.Fail __LINE__
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.KillInitMediaProc_004() =
        let portNo, dname, k, st, tdid = CtrlConnection_Test1.Init "KillInitMediaProc_004"
        [|
            fun () -> task {
                let! sl, c, sessID = CtrlConnection_Test1.StubLogin portNo

                // receive KillInitMediaProc request
                let! startTargetDeviceProcRequestStr = Functions.FramingReceiver c 
                let startTargetDeviceProcRequest = HarukaCtrlerCtrlReq.ReaderWriter.LoadString startTargetDeviceProcRequestStr
                match startTargetDeviceProcRequest.Request with
                | HarukaCtrlerCtrlReq.U_KillInitMediaProc( x ) ->
                    Assert.True(( x.SessionID = sessID ))
                    Assert.True(( x.ProcID = 2UL ))
                | _ ->
                    Assert.Fail __LINE__

                // send KillInitMediaProcResult response
                let rb2 =
                    HarukaCtrlerCtrlRes.ReaderWriter.ToString {
                        Response = HarukaCtrlerCtrlRes.T_Response.U_KillInitMediaProcResult({
                            Result = true;
                            ErrorMessage = "";
                        })
                    }
                do! Functions.FramingSender c rb2
                c.Dispose()
                sl.Stop()
            };
            fun () -> task {
                let! cc1 = CtrlConnection.Connect st "::1" portNo false
                do! cc1.KillInitMediaProc 2UL
                k.NoticeTerminate()
            }
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.RunTaskSynchronously
        |> ignore

        GlbFunc.DeleteDir dname
