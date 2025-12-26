//=============================================================================
// Haruka Software Storage.
// ConfNode_ControllerTest.fs : Test cases for ConfNode_Controller class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Client

//=============================================================================
// Import declaration

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.IODataTypes
open Haruka.Test

//=============================================================================
// Class implementation

type ConfNode_Controller_Test() =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let defaultconf : HarukaCtrlConf.T_HarukaCtrl = {
        RemoteCtrl = Some {
            PortNum = 1us;
            Address = "a";
            WhiteList = [];
        };
        LogMaintenance = Some {
            OutputDest = HarukaCtrlConf.U_ToFile( {
                TotalLimit = Constants.LOGMNT_DEF_TOTALLIMIT + 1u;
                MaxFileCount = Constants.LOGMNT_DEF_MAXFILECOUNT + 1u;
                ForceSync = true;
            })
        }
        LogParameters = Some {
            SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT + 1u;
            HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT + 1u;
            LogLevel = LogLevel.LOGLEVEL_ERROR;
        };
    }

    let defTargetDeviceConf : TargetDeviceConf.T_TargetDevice = {
        NetworkPortal = [];
        NegotiableParameters = None;
        LogParameters = None;
        DeviceName = "abc";
    }

    let defaultTargetConf : TargetGroupConf.T_Target = {
        IdentNumber = tnodeidx_me.fromPrim 1u;
        TargetPortalGroupTag = tpgt_me.fromPrim 0us;
        TargetName = "aaa";
        TargetAlias = "";
        LUN = [ lun_me.fromPrim 1UL ];
        Auth = TargetGroupConf.U_None();
    }

    let defNetworkPortalConf : TargetDeviceConf.T_NetworkPortal = {
        IdentNumber = netportidx_me.fromPrim 0u;
        TargetPortalGroupTag = tpgt_me.zero;
        TargetAddress = "a";
        PortNumber = 1us;
        DisableNagle = false;
        ReceiveBufferSize = 8190;
        SendBufferSize = 8192;
        WhiteList = [];
    }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constractor_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let n = ConfNode_Controller( st, rel, cid, defaultconf, ModifiedStatus.NotModified ) :> IConfigFileNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.NodeTypeName = "Controller" ))
        Assert.True(( n.Modified = ModifiedStatus.NotModified ))
        Assert.True(( ( n :?> ConfNode_Controller ).RemoteCtrlValue = defaultconf.RemoteCtrl.Value ))
        Assert.True(( ( n :?> ConfNode_Controller ).LogMaintenanceValue = defaultconf.LogMaintenance.Value ))
        Assert.True(( ( n :?> ConfNode_Controller ).LogParametersValue = defaultconf.LogParameters.Value ))

    [<Fact>]
    member _.Constractor_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let n = ConfNode_Controller( st, rel, cid ) :> IConfigFileNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.NodeTypeName = "Controller" ))
        Assert.True(( n.Modified = ModifiedStatus.NotModified ))
        Assert.True(( ( n :?> ConfNode_Controller ).RemoteCtrlValue.PortNum = Constants.DEFAULT_MNG_CLI_PORT_NUM ))
        Assert.True(( ( n :?> ConfNode_Controller ).RemoteCtrlValue.Address = "::1" ))
        Assert.True(( ( n :?> ConfNode_Controller ).RemoteCtrlValue.WhiteList = [] ))
        match ( n :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToStdout( x ) ->
            Assert.True(( x = Constants.LOGMNT_DEF_TOTALLIMIT ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( n :?> ConfNode_Controller ).LogParametersValue.SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT ))
        Assert.True(( ( n :?> ConfNode_Controller ).LogParametersValue.HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT ))
        Assert.True(( ( n :?> ConfNode_Controller ).LogParametersValue.LogLevel = LogLevel.LOGLEVEL_INFO ))

    [<Fact>]
    member _.Constractor_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let te : TempExport.T_Node = {
            TypeName = "aaaa";  // ignored
            NodeID = 999UL;     // ignored
            Values = [
                {
                    Name = "aaa";
                    Value = "bbb";
                }
            ];
        }
        let n = ConfNode_Controller( st, rel, cid, te ) :> IConfigFileNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.Modified = ModifiedStatus.Modified ))
        Assert.True(( ( n :?> ConfNode_Controller ).RemoteCtrlValue.PortNum = Constants.DEFAULT_MNG_CLI_PORT_NUM ))
        Assert.True(( ( n :?> ConfNode_Controller ).RemoteCtrlValue.Address = "::1" ))
        Assert.True(( ( n :?> ConfNode_Controller ).RemoteCtrlValue.WhiteList = [] ))
        match ( n :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToStdout( x ) ->
            Assert.True(( x = Constants.LOGMNT_DEF_TOTALLIMIT ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( n :?> ConfNode_Controller ).LogParametersValue.SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT ))
        Assert.True(( ( n :?> ConfNode_Controller ).LogParametersValue.HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT ))
        Assert.True(( ( n :?> ConfNode_Controller ).LogParametersValue.LogLevel = LogLevel.LOGLEVEL_INFO ))

    [<Fact>]
    member _.Constractor_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let te : TempExport.T_Node = {
            TypeName = "aaaa";  // ignored
            NodeID = 999UL;     // ignored
            Values = [
                {
                    Name = "RemoteCtrl.PortNumber";
                    Value = "123";
                }
                {
                    Name = "RemoteCtrl.Address";
                    Value = "address123";
                }
                {
                    Name = "RemoteCtrl.WhiteList";
                    Value = "Linklocal\tMulticast";
                }
                {
                    Name = "LogMaintenance.OutputStdout";
                    Value = "true";
                }
                {
                    Name = "LogMaintenance.TotalLimit";
                    Value = "456";
                }
                {
                    Name = "LogParameters.SoftLimit";
                    Value = "789";
                }
                {
                    Name = "LogParameters.HardLimit";
                    Value = "741";
                }
                {
                    Name = "LogParameters.LogLevel";
                    Value = "WARNING";
                }
            ];
        }
        let n = ConfNode_Controller( st, rel, cid, te ) :> IConfigFileNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.Modified = ModifiedStatus.Modified ))
        Assert.True(( ( n :?> ConfNode_Controller ).RemoteCtrlValue.PortNum = 123us ))
        Assert.True(( ( n :?> ConfNode_Controller ).RemoteCtrlValue.Address = "address123" ))
        Assert.True(( ( n :?> ConfNode_Controller ).RemoteCtrlValue.WhiteList = [ IPCondition.Linklocal; IPCondition.Multicast ] ))
        match ( n :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToStdout( x ) ->
            Assert.True(( x = 456u ))
        | _ ->
            Assert.Fail __LINE__
        Assert.True(( ( n :?> ConfNode_Controller ).LogParametersValue.SoftLimit = 789u ))
        Assert.True(( ( n :?> ConfNode_Controller ).LogParametersValue.HardLimit = 741u ))
        Assert.True(( ( n :?> ConfNode_Controller ).LogParametersValue.LogLevel = LogLevel.LOGLEVEL_WARNING ))

    [<Fact>]
    member _.Constractor_005() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let te : TempExport.T_Node = {
            TypeName = "aaaa";  // ignored
            NodeID = 999UL;     // ignored
            Values = [
                {
                    Name = "LogMaintenance.OutputStdout";
                    Value = "false";
                }
                {
                    Name = "LogMaintenance.TotalLimit";
                    Value = "852";
                }
                {
                    Name = "LogMaintenance.MaxFileCount";
                    Value = "963";
                }
                {
                    Name = "LogMaintenance.ForceSync";
                    Value = "true";
                }
            ];
        }
        let n = ConfNode_Controller( st, rel, cid, te ) :> IConfigFileNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.Modified = ModifiedStatus.Modified ))
        match ( n :?> ConfNode_Controller ).LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToFile( x ) ->
            Assert.True(( x.TotalLimit = 852u ))
            Assert.True(( x.MaxFileCount = 963u ))
            Assert.True(( x.ForceSync = true ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.Validate_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogParameters = Some {
                    defaultconf.LogParameters.Value with
                        SoftLimit = Constants.LOGPARAM_MIN_SOFTLIMIT;
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_003() =
        if Constants.LOGPARAM_MIN_SOFTLIMIT > 0u then
            let st = new StringTable( "" )
            let rel = new ConfNodeRelation()
            let conf = {
                defaultconf with
                    LogParameters = Some {
                        defaultconf.LogParameters.Value with
                            SoftLimit = Constants.LOGPARAM_MIN_SOFTLIMIT - 1u;
                    }
            }
            let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
            rel.AddNode n
            let r = n.Validate []
            Assert.True(( r.Length = 1 ))
            Assert.True(( fst r.[0] = n.NodeID ))
            Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_SOFTLIMIT" ))

    [<Fact>]
    member _.Validate_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogParameters = Some {
                    defaultconf.LogParameters.Value with
                        SoftLimit = Constants.LOGPARAM_MAX_SOFTLIMIT;
                        HardLimit = Constants.LOGPARAM_MAX_HARDLIMIT;
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_005() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogParameters = Some {
                    defaultconf.LogParameters.Value with
                        SoftLimit = Constants.LOGPARAM_MAX_SOFTLIMIT + 1u;
                        HardLimit = Constants.LOGPARAM_MAX_HARDLIMIT;
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_SOFTLIMIT" ))

    [<Fact>]
    member _.Validate_006() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogParameters = Some {
                    defaultconf.LogParameters.Value with
                        SoftLimit = Constants.LOGPARAM_MIN_SOFTLIMIT;
                        HardLimit = Constants.LOGPARAM_MIN_HARDLIMIT;
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_007() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogParameters = Some {
                    defaultconf.LogParameters.Value with
                        SoftLimit = Constants.LOGPARAM_MIN_SOFTLIMIT;
                        HardLimit = Constants.LOGPARAM_MIN_HARDLIMIT - 1u;
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_HARDLIMIT" ))

    [<Fact>]
    member _.Validate_008() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogParameters = Some {
                    defaultconf.LogParameters.Value with
                        HardLimit = Constants.LOGPARAM_MAX_HARDLIMIT;
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_009() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogParameters = Some {
                    defaultconf.LogParameters.Value with
                        HardLimit = Constants.LOGPARAM_MAX_HARDLIMIT + 1u;
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_HARDLIMIT" ))

    [<Fact>]
    member _.Validate_010() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let wval = ( Constants.LOGPARAM_MIN_HARDLIMIT + Constants.LOGPARAM_MAX_SOFTLIMIT ) / 2u
        let conf = {
            defaultconf with
                LogParameters = Some {
                    defaultconf.LogParameters.Value with
                        SoftLimit = wval;
                        HardLimit = wval;
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_011() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let wval = ( Constants.LOGPARAM_MIN_HARDLIMIT + Constants.LOGPARAM_MAX_SOFTLIMIT ) / 2u
        let conf = {
            defaultconf with
                LogParameters = Some {
                    defaultconf.LogParameters.Value with
                        SoftLimit = wval + 1u;
                        HardLimit = wval;
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_LOGPARAM_HARDLIMIT_LESS_THAN_SOFTLIMIT" ))

    [<Fact>]
    member _.Validate_012() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogMaintenance = Some {
                    OutputDest = HarukaCtrlConf.U_ToFile( {
                        TotalLimit = Constants.LOGMNT_MIN_TOTALLIMIT;
                        MaxFileCount = Constants.LOGMNT_DEF_MAXFILECOUNT + 1u;
                        ForceSync = true;
                    })
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_013() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogMaintenance = Some {
                    OutputDest = HarukaCtrlConf.U_ToFile( {
                        TotalLimit = Constants.LOGMNT_MIN_TOTALLIMIT - 1u;
                        MaxFileCount = Constants.LOGMNT_DEF_MAXFILECOUNT + 1u;
                        ForceSync = true;
                    })
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_TOTALLIMIT" ))

    [<Fact>]
    member _.Validate_014() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogMaintenance = Some {
                    OutputDest = HarukaCtrlConf.U_ToFile( {
                        TotalLimit = Constants.LOGMNT_MAX_TOTALLIMIT;
                        MaxFileCount = Constants.LOGMNT_DEF_MAXFILECOUNT + 1u;
                        ForceSync = true;
                    })
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_015() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogMaintenance = Some {
                    OutputDest = HarukaCtrlConf.U_ToFile( {
                        TotalLimit = Constants.LOGMNT_MAX_TOTALLIMIT + 1u;
                        MaxFileCount = Constants.LOGMNT_DEF_MAXFILECOUNT + 1u;
                        ForceSync = true;
                    })
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_TOTALLIMIT" ))

    [<Fact>]
    member _.Validate_016() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogMaintenance = Some {
                    OutputDest = HarukaCtrlConf.U_ToFile( {
                        TotalLimit = Constants.LOGMNT_DEF_TOTALLIMIT + 1u;
                        MaxFileCount = Constants.LOGMNT_MIN_MAXFILECOUNT;
                        ForceSync = true;
                    })
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_017() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogMaintenance = Some {
                    OutputDest = HarukaCtrlConf.U_ToFile( {
                        TotalLimit = Constants.LOGMNT_DEF_TOTALLIMIT + 1u;
                        MaxFileCount = Constants.LOGMNT_MIN_MAXFILECOUNT - 1u;
                        ForceSync = true;
                    })
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_MAXFILECOUNT" ))

    [<Fact>]
    member _.Validate_018() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogMaintenance = Some {
                    OutputDest = HarukaCtrlConf.U_ToStdout( Constants.LOGMNT_MIN_TOTALLIMIT )
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_019() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogMaintenance = Some {
                    OutputDest = HarukaCtrlConf.U_ToStdout( Constants.LOGMNT_MIN_TOTALLIMIT - 1u )
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_TOTALLIMIT" ))

    [<Fact>]
    member _.Validate_020() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogMaintenance = Some {
                    OutputDest = HarukaCtrlConf.U_ToStdout( Constants.LOGMNT_MAX_TOTALLIMIT )
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_021() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                LogMaintenance = Some {
                    OutputDest = HarukaCtrlConf.U_ToStdout( Constants.LOGMNT_MAX_TOTALLIMIT + 1u )
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_TOTALLIMIT" ))

    [<Fact>]
    member _.Validate_022() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                RemoteCtrl = Some {
                    defaultconf.RemoteCtrl.Value with
                        PortNum = 0us;
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_REMOTE_CTRL_PORT_NUM" ))

    [<Fact>]
    member _.Validate_023() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let testconf : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = Some {
                PortNum = Constants.DEFAULT_MNG_CLI_PORT_NUM;
                Address = "::1";
                WhiteList = [
                    for i = 1 to Constants.MAX_IP_WHITELIST_COUNT do
                        yield IPCondition.Any
                ];
            };
            LogMaintenance = None;
            LogParameters = None;
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, testconf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_024() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let testconf : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = Some {
                PortNum = Constants.DEFAULT_MNG_CLI_PORT_NUM;
                Address = "::1";
                WhiteList = [
                    for i = 1 to Constants.MAX_IP_WHITELIST_COUNT + 1 do
                        yield IPCondition.Any
                ];
            };
            LogMaintenance = None;
            LogParameters = None;
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, testconf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_IP_WHITELIST_TOO_LONG" ))

    [<Fact>]
    member _.Validate_025() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                RemoteCtrl = Some {
                    defaultconf.RemoteCtrl.Value with
                        Address = "";
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_REMOTE_CTRL_ADDRESS" ))

    [<Fact>]
    member _.Validate_026() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                RemoteCtrl = Some {
                    defaultconf.RemoteCtrl.Value with
                        Address = "a";
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_027() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                RemoteCtrl = Some {
                    defaultconf.RemoteCtrl.Value with
                        Address = String.replicate ( Constants.MAX_CTRL_ADDRESS_STR_LENGTH ) "a";
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_028() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultconf with
                RemoteCtrl = Some {
                    defaultconf.RemoteCtrl.Value with
                        Address = String.replicate ( Constants.MAX_CTRL_ADDRESS_STR_LENGTH + 1 ) "a";
                }
        }
        let n = ConfNode_Controller( st, rel, rel.NextID, conf, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_REMOTE_CTRL_ADDRESS" ))

    [<Fact>]
    member _.Validate_029() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defNetworkPortalConf ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode n
        rel.AddNode td
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation td.NodeID tg.NodeID
        rel.AddRelation td.NodeID np.NodeID
        rel.AddRelation n.NodeID td.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_030() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode n

        for i = 1 to Constants.MAX_TARGET_DEVICE_COUNT do
            let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
            let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defNetworkPortalConf ) :> IConfigureNode
            let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
            let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
            let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode td
            rel.AddNode np
            rel.AddNode tg
            rel.AddNode tn
            rel.AddNode dd
            rel.AddRelation tn.NodeID dd.NodeID
            rel.AddRelation tg.NodeID tn.NodeID
            rel.AddRelation td.NodeID tg.NodeID
            rel.AddRelation td.NodeID np.NodeID
            rel.AddRelation n.NodeID td.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_031() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode n

        for i = 1 to Constants.MAX_TARGET_DEVICE_COUNT + 1 do
            let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
            let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defNetworkPortalConf ) :> IConfigureNode
            let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
            let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
            let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode td
            rel.AddNode np
            rel.AddNode tg
            rel.AddNode tn
            rel.AddNode dd
            rel.AddRelation tn.NodeID dd.NodeID
            rel.AddRelation tg.NodeID tn.NodeID
            rel.AddRelation td.NodeID tg.NodeID
            rel.AddRelation td.NodeID np.NodeID
            rel.AddRelation n.NodeID td.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_OUT_OF_TARGET_DEVICE_COUNT" ))

    [<Fact>]
    member _.Validate_032() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode n

        let tdid = GlbFunc.newTargetDeviceID()
        let td1 = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defTargetDeviceConf ) :> IConfigFileNode
        let np1 = new ConfNode_NetworkPortal( st, rel, rel.NextID, defNetworkPortalConf ) :> IConfigureNode
        let tg1 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode td1
        rel.AddNode np1
        rel.AddNode tg1
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation tg1.NodeID tn1.NodeID
        rel.AddRelation td1.NodeID tg1.NodeID
        rel.AddRelation td1.NodeID np1.NodeID

        let td2 = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defTargetDeviceConf ) :> IConfigFileNode
        let np2 = new ConfNode_NetworkPortal( st, rel, rel.NextID, defNetworkPortalConf ) :> IConfigureNode
        let tg2 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode td2
        rel.AddNode np2
        rel.AddNode tg2
        rel.AddNode tn2
        rel.AddNode dd2
        rel.AddRelation tn2.NodeID dd2.NodeID
        rel.AddRelation tg2.NodeID tn2.NodeID
        rel.AddRelation td2.NodeID tg2.NodeID
        rel.AddRelation td2.NodeID np2.NodeID

        rel.AddRelation n.NodeID td1.NodeID
        rel.AddRelation n.NodeID td2.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 2 ))
        Assert.True(( fst r.[0] = td1.NodeID || fst r.[0] = td2.NodeID ))
        Assert.True(( fst r.[1] = td1.NodeID || fst r.[1] = td2.NodeID ))
        Assert.True(( not ( ( fst r.[0] ) = ( fst r.[1] ) ) ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_DUPLICATE_TARGET_DEVICE_ID" ))
        Assert.True(( ( snd r.[1] ).StartsWith "CHKMSG_DUPLICATE_TARGET_DEVICE_ID" ))

    [<Fact>]
    member _.Validate_033() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dn = DummyNode( rel.NextID, "a" ) :> IConfigureNode
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode dn
        rel.AddNode n
        rel.AddRelation dn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_CTRL_NODE_NOT_ROOT" ))

    [<Fact>]
    member _.Validate_034() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dn = DummyNode( rel.NextID, "a" ) :> IConfigureNode
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode dn
        rel.AddNode n
        rel.AddRelation n.NodeID dn.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = dn.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_RELATION" ))

    [<Fact>]
    member _.Validate_035() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defNetworkPortalConf ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dn = DummyNode( rel.NextID, "a" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode td
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddNode dn
        rel.AddRelation dd.NodeID dn.NodeID
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation td.NodeID tg.NodeID
        rel.AddRelation td.NodeID np.NodeID
        rel.AddRelation n.NodeID td.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = dn.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_RELATION" ))

    [<Fact>]
    member _.Validate_036() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode n
        let r = n.Validate [ ( confnode_me.fromPrim 99UL, "abc" ) ]
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = confnode_me.fromPrim 99UL ))
        Assert.True(( snd r.[0] = "abc" ))


    [<Fact>]
    member _.CreateUpdatedNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID )
        let nextval : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = Some {
                PortNum = 2us;
                Address = "b";
                WhiteList = [];
            };
            LogMaintenance = Some {
                OutputDest = HarukaCtrlConf.U_ToFile( {
                    TotalLimit = Constants.LOGMNT_DEF_TOTALLIMIT + 2u;
                    MaxFileCount = Constants.LOGMNT_DEF_MAXFILECOUNT + 2u;
                    ForceSync = true;
                })
            }
            LogParameters = Some {
                SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT + 2u;
                HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT + 2u;
                LogLevel = LogLevel.LOGLEVEL_FAILED;
            };
        }
        let n2 = n.CreateUpdatedNode nextval
        Assert.True(( n2.RemoteCtrlValue = nextval.RemoteCtrl.Value ))
        Assert.True(( n2.LogMaintenanceValue = nextval.LogMaintenance.Value ))
        Assert.True(( n2.LogParametersValue = nextval.LogParameters.Value ))

    [<Fact>]
    member _.GetChildNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode n
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetChildNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm
        rel.AddRelation n.NodeID dm.NodeID
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = dm ))
        
    [<Fact>]
    member _.GetChildNode_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation n.NodeID dm2.NodeID
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 2 ))
        Assert.True(( r = [ dm1; dm2 ] || r = [ dm2; dm1 ] ))
        
    [<Fact>]
    member _.GetChildNode_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm3 = new DummyNode2( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode dm3
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation n.NodeID dm2.NodeID
        rel.AddRelation n.NodeID dm3.NodeID
        let r = n.GetChildNodes<DummyNode2>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( Functions.IsSame dm3 r.[0] ))

    [<Fact>]
    member _.GetDescendantNodes_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode n
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetDescendantNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm
        rel.AddRelation n.NodeID dm.NodeID
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = dm ))
        
    [<Fact>]
    member _.GetDescendantNodes_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation dm1.NodeID dm2.NodeID
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.True(( r.Length = 2 ))
        Assert.True(( r = [ dm1; dm2 ] || r = [ dm2; dm1 ] ))
        
    [<Fact>]
    member _.GetDescendantNodes_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode2( rel.NextID, "" ) :> IConfigureNode
        let dm3 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode dm3
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation dm1.NodeID dm2.NodeID
        rel.AddRelation dm2.NodeID dm3.NodeID
        let r = n.GetDescendantNodes<DummyNode2>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( Functions.IsSame dm2 r.[0] ))

    [<Fact>]
    member _.GetParentNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode n
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetParentNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode dm
        rel.AddNode n
        rel.AddRelation dm.NodeID n.NodeID
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = dm ))
        
    [<Fact>]
    member _.GetParentNode_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode n
        rel.AddRelation dm1.NodeID n.NodeID
        rel.AddRelation dm2.NodeID n.NodeID
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.True(( r.Length = 2 ))
        Assert.True(( r = [ dm1; dm2 ] || r = [ dm2; dm1 ] ))
        
    [<Fact>]
    member _.GetParentNode_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm3 = new DummyNode2( rel.NextID, "" ) :> IConfigureNode
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode dm3
        rel.AddNode n
        rel.AddRelation dm1.NodeID n.NodeID
        rel.AddRelation dm2.NodeID n.NodeID
        rel.AddRelation dm3.NodeID n.NodeID
        let r = n.GetParentNodes<DummyNode2>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( Functions.IsSame dm3 r.[0] ))

    [<Fact>]
    member _.GetAncestorNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode n
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetAncestorNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode dm
        rel.AddNode n
        rel.AddRelation dm.NodeID n.NodeID
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = dm ))
        
    [<Fact>]
    member _.GetAncestorNode_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode n
        rel.AddRelation dm1.NodeID n.NodeID
        rel.AddRelation dm2.NodeID dm1.NodeID
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 2 ))
        Assert.True(( r = [ dm1; dm2 ] || r = [ dm2; dm1 ] ))
        
    [<Fact>]
    member _.GetAncestorNode_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode2( rel.NextID, "" ) :> IConfigureNode
        let dm3 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode dm3
        rel.AddNode n
        rel.AddRelation dm1.NodeID n.NodeID
        rel.AddRelation dm2.NodeID dm1.NodeID
        rel.AddRelation dm3.NodeID dm2.NodeID
        let r = n.GetAncestorNode<DummyNode2>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( Functions.IsSame dm2 r.[0] ))
        
    [<Fact>]
    member _.ResetModifiedFlag_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, rel.NextID ) :> IConfigFileNode
        let nextval : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = Some( ( n :?> ConfNode_Controller ).RemoteCtrlValue )
            LogMaintenance = Some( ( n :?> ConfNode_Controller ).LogMaintenanceValue )
            LogParameters = Some( ( n :?> ConfNode_Controller ).LogParametersValue )
        }

        let n2 = ( n :?> ConfNode_Controller ).CreateUpdatedNode nextval :> IConfigFileNode
        Assert.True(( n2.Modified = ModifiedStatus.Modified ))

        let n3 = n2.ResetModifiedFlag()
        Assert.True(( n3.Modified = ModifiedStatus.NotModified ))
        Assert.True(( ( n3 :?> ConfNode_Controller ).RemoteCtrlValue = nextval.RemoteCtrl.Value ))
        Assert.True(( ( n3 :?> ConfNode_Controller ).LogMaintenanceValue = nextval.LogMaintenance.Value ))
        Assert.True(( ( n3 :?> ConfNode_Controller ).LogParametersValue = nextval.LogParameters.Value ))

    [<Fact>]
    member _.SortKey_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = ConfNode_Controller( st, rel, confnode_me.fromPrim 14567UL ) :> IConfigFileNode
        let v = n.SortKey
        Assert.True(( v.Length = 2 ))
        Assert.True(( v.[0] = ClientConst.SORT_KEY_TYPE_Controller ))
        Assert.True(( v.[1] = sprintf "%016X" 14567UL ))

    [<Fact>]
    member _.TempExportData_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = None;
            LogMaintenance = None;
            LogParameters = None;
        }
        let n = ConfNode_Controller( st, rel, confnode_me.fromPrim 14567UL, conf, ModifiedStatus.Modified ) :> IConfigureNode
        let v = n.TempExportData
        Assert.True(( v.NodeID = 14567UL ))
        Assert.True(( v.TypeName = ClientConst.TEMPEXP_NN_Controller ))
        Assert.True(( v.Values.Length = 8 ))

        let remoteCtrl = ( n :?> ConfNode_Controller ).RemoteCtrlValue
        let logMaintenance = ( n :?> ConfNode_Controller ).LogMaintenanceValue
        let logParameters = ( n :?> ConfNode_Controller ).LogParametersValue
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "RemoteCtrl.PortNumber" ) |> _.Value = sprintf "%d" remoteCtrl.PortNum ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "RemoteCtrl.Address" ) |> _.Value = remoteCtrl.Address ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "RemoteCtrl.WhiteList" ) |> _.Value = "" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LogMaintenance.OutputStdout" ) |> _.Value = "true" ))
        match logMaintenance.OutputDest with
        | HarukaCtrlConf.U_ToStdout( x ) ->
            Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LogMaintenance.TotalLimit" ) |> _.Value = sprintf "%d" x ))
        | _ -> Assert.Fail __LINE__
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LogParameters.SoftLimit" ) |> _.Value = sprintf "%d" logParameters.SoftLimit ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LogParameters.HardLimit" ) |> _.Value = sprintf "%d" logParameters.HardLimit ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LogParameters.LogLevel" ) |> _.Value = ( logParameters.LogLevel |> LogLevel.toString ) ))

    [<Fact>]
    member _.TempExportData_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = Some {
                PortNum = 0us;
                Address = "";
                WhiteList = [
                    IPCondition.Any;
                ];
            };
            LogMaintenance = None;
            LogParameters = None;
        }
        let n = ConfNode_Controller( st, rel, confnode_me.fromPrim 14567UL, conf, ModifiedStatus.Modified ) :> IConfigureNode
        let v = n.TempExportData
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "RemoteCtrl.WhiteList" ) |> _.Value = "Any" ))

    [<Fact>]
    member _.TempExportData_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = Some {
                PortNum = 0us;
                Address = "";
                WhiteList = [
                    IPCondition.Loopback;
                    IPCondition.Linklocal;
                ];
            };
            LogMaintenance = None;
            LogParameters = None;
        }
        let n = ConfNode_Controller( st, rel, confnode_me.fromPrim 14567UL, conf, ModifiedStatus.Modified ) :> IConfigureNode
        let v = n.TempExportData
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "RemoteCtrl.WhiteList" ) |> _.Value = "Loopback\tLinklocal" ))

    [<Fact>]
    member _.TempExportData_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = None;
            LogMaintenance = Some {
                OutputDest =
                    HarukaCtrlConf.U_ToFile({
                        TotalLimit = 123u;
                        MaxFileCount = 456u;
                        ForceSync = false;
                    })
            }
            LogParameters = None;
        }
        let n = ConfNode_Controller( st, rel, confnode_me.fromPrim 14567UL, conf, ModifiedStatus.Modified ) :> IConfigureNode
        let v = n.TempExportData

        let logMaintenance = ( n :?> ConfNode_Controller ).LogMaintenanceValue
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LogMaintenance.OutputStdout" ) |> _.Value = "false" ))
        match logMaintenance.OutputDest with
        | HarukaCtrlConf.U_ToFile( x ) ->
            Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LogMaintenance.TotalLimit" ) |> _.Value = sprintf "%d" x.TotalLimit ))
            Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LogMaintenance.MaxFileCount" ) |> _.Value = sprintf "%d" x.MaxFileCount ))
            Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LogMaintenance.ForceSync" ) |> _.Value = sprintf "%b" x.ForceSync ))
        | _ -> Assert.Fail __LINE__

