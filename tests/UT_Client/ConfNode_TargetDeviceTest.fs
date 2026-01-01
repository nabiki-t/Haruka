//=============================================================================
// Haruka Software Storage.
// ConfNode_TargetDeviceTest.fs : Test cases for ConfNode_TargetDevice class.
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

type ConfNode_TargetDevice_Test() =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let defaultConf : TargetDeviceConf.T_TargetDevice = {
        // NetworkPortal values in TargetDeviceConf.T_TargetDevice is ignored.
        // Network poratl node value is used for configurations of network portal.
        NetworkPortal = [ {
            IdentNumber = netportidx_me.fromPrim 0u;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetAddress = "a";
            PortNumber = 1us;
            DisableNagle = false;
            ReceiveBufferSize = 8192;
            SendBufferSize = 8192;
            WhiteList = [];
        }];
        NegotiableParameters = Some( {
            MaxRecvDataSegmentLength = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
            MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength;
            FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength;
            DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait;
            DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain;
            MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
        });
        LogParameters = Some({
            SoftLimit = 1024u;
            HardLimit = 1024u;
            LogLevel = LogLevel.LOGLEVEL_INFO;
        });
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

    let defContConf : HarukaCtrlConf.T_HarukaCtrl = {
        RemoteCtrl = None;
        LogMaintenance = None;
        LogParameters = None;
    }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constractor_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let tdid = GlbFunc.newTargetDeviceID()
        let n = new ConfNode_TargetDevice( st, rel, cid, tdid, "a", defaultConf.NegotiableParameters.Value, defaultConf.LogParameters.Value, ModifiedStatus.NotModified ) :> IConfigFileNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.NodeTypeName = "Target Device" ))
        Assert.True(( n.Modified = ModifiedStatus.NotModified ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).TargetDeviceID = tdid ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).TargetDeviceName = "a" ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).NegotiableParameters = defaultConf.NegotiableParameters.Value ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).LogParameters = defaultConf.LogParameters.Value ))

    [<Fact>]
    member _.Constractor_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let tdid = GlbFunc.newTargetDeviceID()
        let n = new ConfNode_TargetDevice( st, rel, cid, tdid, defaultConf ) :> IConfigFileNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.NodeTypeName = "Target Device" ))
        Assert.True(( n.Modified = ModifiedStatus.NotModified ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).TargetDeviceID = tdid ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).TargetDeviceName = defaultConf.DeviceName ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).NegotiableParameters = defaultConf.NegotiableParameters.Value ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).LogParameters = defaultConf.LogParameters.Value ))

    [<Fact>]
    member _.Constractor_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = None;
        }
        let n = new ConfNode_TargetDevice( st, rel, cid, tdid, conf ) :> IConfigFileNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.NodeTypeName = "Target Device" ))
        Assert.True(( n.Modified = ModifiedStatus.NotModified ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).TargetDeviceID = tdid ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).TargetDeviceName = defaultConf.DeviceName ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).NegotiableParameters.MaxRecvDataSegmentLength = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).NegotiableParameters.MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).NegotiableParameters.FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).NegotiableParameters.DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).NegotiableParameters.DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).NegotiableParameters.MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).LogParameters = defaultConf.LogParameters.Value ))

    [<Fact>]
    member _.Constractor_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                LogParameters = None;
        }
        let n = new ConfNode_TargetDevice( st, rel, cid, tdid, conf ) :> IConfigFileNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.NodeTypeName = "Target Device" ))
        Assert.True(( n.Modified = ModifiedStatus.NotModified ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).TargetDeviceID = tdid ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).TargetDeviceName = defaultConf.DeviceName ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).NegotiableParameters = defaultConf.NegotiableParameters.Value ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).LogParameters.SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).LogParameters.HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT ))
        Assert.True(( ( n :?> ConfNode_TargetDevice ).LogParameters.LogLevel = LogLevel.LOGLEVEL_INFO ))

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
                    Name = "aaa";
                    Value = "bbb";
                }
            ];
        }
        let n = ConfNode_TargetDevice( st, rel, cid, te )
        Assert.True(( ( n :> IConfigFileNode ).NodeID = cid ))
        Assert.True(( ( n :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
        Assert.True(( n.TargetDeviceID = tdid_me.Zero ))
        Assert.True(( n.TargetDeviceName = "" ))
        Assert.True(( n.NegotiableParameters.MaxRecvDataSegmentLength = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength ))
        Assert.True(( n.NegotiableParameters.MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength ))
        Assert.True(( n.NegotiableParameters.FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength ))
        Assert.True(( n.NegotiableParameters.DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait ))
        Assert.True(( n.NegotiableParameters.DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain ))
        Assert.True(( n.NegotiableParameters.MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T ))
        Assert.True(( n.LogParameters.SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT ))
        Assert.True(( n.LogParameters.HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT ))
        Assert.True(( n.LogParameters.LogLevel = LogLevel.LOGLEVEL_INFO ))

    [<Fact>]
    member _.Constractor_006() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let tdid = GlbFunc.newTargetDeviceID()
        let te : TempExport.T_Node = {
            TypeName = "aaaa";  // ignored
            NodeID = 999UL;     // ignored
            Values = [
                {
                    Name = "ID";
                    Value = tdid_me.toString tdid;
                }
                {
                    Name = "Name";
                    Value = "a56";
                }
                {
                    Name = "NegotiableParameters.MaxRecvDataSegmentLength";
                    Value = "9";
                }
                {
                    Name = "NegotiableParameters.MaxBurstLength";
                    Value = "8";
                }
                {
                    Name = "NegotiableParameters.FirstBurstLength";
                    Value = "7";
                }
                {
                    Name = "NegotiableParameters.DefaultTime2Wait";
                    Value = "6";
                }
                {
                    Name = "NegotiableParameters.DefaultTime2Retain";
                    Value = "5";
                }
                {
                    Name = "NegotiableParameters.MaxOutstandingR2T";
                    Value = "4";
                }
                {
                    Name = "LogParameters.SoftLimit";
                    Value = "3";
                }
                {
                    Name = "LogParameters.HardLimit";
                    Value = "2";
                }
                {
                    Name = "LogParameters.LogLevel";
                    Value = "ERROR";
                }
            ];
        }
        let n = ConfNode_TargetDevice( st, rel, cid, te )
        Assert.True(( ( n :> IConfigFileNode ).NodeID = cid ))
        Assert.True(( ( n :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
        Assert.True(( n.TargetDeviceID = tdid ))
        Assert.True(( n.TargetDeviceName = "a56" ))
        Assert.True(( n.NegotiableParameters.MaxRecvDataSegmentLength = 9u ))
        Assert.True(( n.NegotiableParameters.MaxBurstLength = 8u ))
        Assert.True(( n.NegotiableParameters.FirstBurstLength = 7u ))
        Assert.True(( n.NegotiableParameters.DefaultTime2Wait = 6us ))
        Assert.True(( n.NegotiableParameters.DefaultTime2Retain = 5us ))
        Assert.True(( n.NegotiableParameters.MaxOutstandingR2T = 4us ))
        Assert.True(( n.LogParameters.SoftLimit = 3u ))
        Assert.True(( n.LogParameters.HardLimit = 2u ))
        Assert.True(( n.LogParameters.LogLevel = LogLevel.LOGLEVEL_ERROR ))


    [<Fact>]
    member _.Validate_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        MaxRecvDataSegmentLength = Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        MaxRecvDataSegmentLength = Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength - 1u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_MAXRECVDATASEGMENTLENGTH" ))

    [<Fact>]
    member _.Validate_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        MaxRecvDataSegmentLength = Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_005() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        MaxRecvDataSegmentLength = Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength + 1u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_MAXRECVDATASEGMENTLENGTH" ))

    [<Fact>]
    member _.Validate_006() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        MaxBurstLength = Constants.NEGOPARAM_MIN_MaxBurstLength + 0u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_007() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        MaxBurstLength = Constants.NEGOPARAM_MIN_MaxBurstLength - 1u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_MAXBURSTLENGTH" ))

    [<Fact>]
    member _.Validate_008() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        MaxBurstLength = Constants.NEGOPARAM_MAX_MaxBurstLength + 0u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_009() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        MaxBurstLength = Constants.NEGOPARAM_MAX_MaxBurstLength + 1u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_MAXBURSTLENGTH" ))

    [<Fact>]
    member _.Validate_010() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        FirstBurstLength = Constants.NEGOPARAM_MIN_FirstBurstLength + 0u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_011() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        FirstBurstLength = Constants.NEGOPARAM_MIN_FirstBurstLength - 1u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_FIRSTBURSTLENGTH" ))

    [<Fact>]
    member _.Validate_012() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        FirstBurstLength = Constants.NEGOPARAM_MAX_FirstBurstLength + 0u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_013() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        FirstBurstLength = Constants.NEGOPARAM_MAX_FirstBurstLength + 1u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_FIRSTBURSTLENGTH" ))

    [<Fact>]
    member _.Validate_014() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        DefaultTime2Wait = Constants.NEGOPARAM_MIN_DefaultTime2Wait + 0us;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_015() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        DefaultTime2Wait = Constants.NEGOPARAM_MIN_DefaultTime2Wait - 1us;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_DEFAULTTIME2WAIT" ))

    [<Fact>]
    member _.Validate_016() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        DefaultTime2Wait = Constants.NEGOPARAM_MAX_DefaultTime2Wait + 0us;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_017() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        DefaultTime2Wait = Constants.NEGOPARAM_MAX_DefaultTime2Wait + 1us;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_DEFAULTTIME2WAIT" ))

    [<Fact>]
    member _.Validate_018() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        DefaultTime2Retain = Constants.NEGOPARAM_MIN_DefaultTime2Retain + 0us;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_019() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        DefaultTime2Retain = Constants.NEGOPARAM_MIN_DefaultTime2Retain - 1us;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_DEFAULTTIME2RETAIN" ))

    [<Fact>]
    member _.Validate_020() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        DefaultTime2Retain = Constants.NEGOPARAM_MAX_DefaultTime2Retain + 0us;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_021() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        DefaultTime2Retain = Constants.NEGOPARAM_MAX_DefaultTime2Retain + 1us;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_DEFAULTTIME2RETAIN" ))

    [<Fact>]
    member _.Validate_022() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        MaxOutstandingR2T = Constants.NEGOPARAM_MIN_MaxOutstandingR2T + 0us;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_023() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        MaxOutstandingR2T = Constants.NEGOPARAM_MIN_MaxOutstandingR2T - 1us;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_MAXOUTSTANDINGR2T" ))

    [<Fact>]
    member _.Validate_024() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        MaxOutstandingR2T = Constants.NEGOPARAM_MAX_MaxOutstandingR2T + 0us;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_025() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                NegotiableParameters = Some({
                    defaultConf.NegotiableParameters.Value with
                        MaxOutstandingR2T = Constants.NEGOPARAM_MAX_MaxOutstandingR2T + 1us;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_MAXOUTSTANDINGR2T" ))

    [<Fact>]
    member _.Validate_026() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                LogParameters = Some({
                    defaultConf.LogParameters.Value with
                        SoftLimit = Constants.LOGPARAM_MIN_SOFTLIMIT + 0u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_027() =
        if Constants.LOGPARAM_MIN_SOFTLIMIT = 0u then
            // If min limit value of SoftLimit is 0, SoftLimit value cannot be less than 0.
            ()
        else
            let st = new StringTable( "" )
            let rel = new ConfNodeRelation()
            let tdid = GlbFunc.newTargetDeviceID()
            let conf = {
                defaultConf with
                    LogParameters = Some({
                        defaultConf.LogParameters.Value with
                            SoftLimit = Constants.LOGPARAM_MIN_SOFTLIMIT - 1u;
                    })
            }
            let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
            let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
            let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
            let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
            let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
            let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode cn
            rel.AddNode n
            rel.AddNode np
            rel.AddNode tg
            rel.AddNode tn
            rel.AddNode dd
            rel.AddRelation tn.NodeID dd.NodeID
            rel.AddRelation tg.NodeID tn.NodeID
            rel.AddRelation n.NodeID tg.NodeID
            rel.AddRelation n.NodeID np.NodeID
            rel.AddRelation cn.NodeID n.NodeID
            let r = n.Validate []
            Assert.True(( r.Length = 1 ))
            Assert.True(( fst r.[0] = n.NodeID ))
            Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_SOFTLIMIT" ))

    [<Fact>]
    member _.Validate_028() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                LogParameters = Some({
                    defaultConf.LogParameters.Value with
                        SoftLimit = Constants.LOGPARAM_MAX_SOFTLIMIT + 0u;
                        HardLimit = Constants.LOGPARAM_MAX_HARDLIMIT;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_029() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                LogParameters = Some({
                    defaultConf.LogParameters.Value with
                        SoftLimit = Constants.LOGPARAM_MAX_SOFTLIMIT + 1u;
                        HardLimit = Constants.LOGPARAM_MAX_HARDLIMIT;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_SOFTLIMIT" ))

    [<Fact>]
    member _.Validate_030() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                LogParameters = Some({
                    defaultConf.LogParameters.Value with
                        SoftLimit = Constants.LOGPARAM_MIN_SOFTLIMIT;
                        HardLimit = Constants.LOGPARAM_MIN_HARDLIMIT + 0u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_031() =
        if Constants.LOGPARAM_MIN_HARDLIMIT = 0u then
            // If min limit value of HardLimit is 0, HardLimit value cannot be less than 0.
            ()
        else
            let st = new StringTable( "" )
            let rel = new ConfNodeRelation()
            let tdid = GlbFunc.newTargetDeviceID()
            let conf = {
                defaultConf with
                    LogParameters = Some({
                        defaultConf.LogParameters.Value with
                            SoftLimit = Constants.LOGPARAM_MIN_SOFTLIMIT;
                            HardLimit = Constants.LOGPARAM_MIN_HARDLIMIT - 1u;
                    })
            }
            let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
            let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
            let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
            let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
            let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
            let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode cn
            rel.AddNode n
            rel.AddNode np
            rel.AddNode tg
            rel.AddNode tn
            rel.AddNode dd
            rel.AddRelation tn.NodeID dd.NodeID
            rel.AddRelation tg.NodeID tn.NodeID
            rel.AddRelation n.NodeID tg.NodeID
            rel.AddRelation n.NodeID np.NodeID
            rel.AddRelation cn.NodeID n.NodeID
            let r = n.Validate []
            Assert.True(( r.Length = 1 ))
            Assert.True(( fst r.[0] = n.NodeID ))
            Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_HARDLIMIT" ))

    [<Fact>]
    member _.Validate_032() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                LogParameters = Some({
                    defaultConf.LogParameters.Value with
                        HardLimit = Constants.LOGPARAM_MAX_HARDLIMIT + 0u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_033() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                LogParameters = Some({
                    defaultConf.LogParameters.Value with
                        HardLimit = Constants.LOGPARAM_MAX_HARDLIMIT + 1u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LOGPARAM_HARDLIMIT" ))

    [<Fact>]
    member _.Validate_034() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let wlp = ( Constants.LOGPARAM_MIN_HARDLIMIT + Constants.LOGPARAM_MAX_SOFTLIMIT ) / 2u;
        let conf = {
            defaultConf with
                LogParameters = Some({
                    defaultConf.LogParameters.Value with
                        SoftLimit = wlp;
                        HardLimit = wlp;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_035() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let wlp = ( Constants.LOGPARAM_MIN_HARDLIMIT + Constants.LOGPARAM_MAX_SOFTLIMIT ) / 2u;
        let conf = {
            defaultConf with
                LogParameters = Some({
                    defaultConf.LogParameters.Value with
                        SoftLimit = wlp;
                        HardLimit = wlp - 1u;
                })
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_LOGPARAM_HARDLIMIT_LESS_THAN_SOFTLIMIT" ))

    [<Fact>]
    member _.Validate_036() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let wlp = ( Constants.LOGPARAM_MIN_HARDLIMIT + Constants.LOGPARAM_MAX_SOFTLIMIT ) / 2u;
        let conf = {
            defaultConf with
                DeviceName = String.replicate Constants.MAX_DEVICE_NAME_STR_LENGTH "a"
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_037() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let conf = {
            defaultConf with
                DeviceName = String.replicate ( Constants.MAX_DEVICE_NAME_STR_LENGTH + 1 ) "a"
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, conf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_TARGET_DEVICE_NAME_TOO_LONG" ))

    [<Fact>]
    member _.Validate_038() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation cn.NodeID n.NodeID

        for i = 1 to Constants.MAX_NETWORK_PORTAL_COUNT do
            let netConf = {
                defaultConf.NetworkPortal.[0] with
                    IdentNumber = netportidx_me.fromPrim ( uint32 i );
            }
            let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, netConf ) :> IConfigureNode
            rel.AddNode np
            rel.AddRelation n.NodeID np.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_039() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation cn.NodeID n.NodeID

        for i = 1 to Constants.MAX_NETWORK_PORTAL_COUNT + 1 do
            let netConf = {
                defaultConf.NetworkPortal.[0] with
                    IdentNumber = netportidx_me.fromPrim ( uint32 i );
            }
            let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, netConf ) :> IConfigureNode
            rel.AddNode np
            rel.AddRelation n.NodeID np.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_OUT_OF_NETWORK_PORTAL_COUNT" ))

    [<Fact>]
    member _.Validate_040() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation cn.NodeID n.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_MISSING_NETWORK_PORTAL" ))

    [<Fact>]
    member _.Validate_041() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID

        for i = 1 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD do
            let targetConf = {
                defaultTargetConf with
                    IdentNumber = tnodeidx_me.fromPrim ( uint32 i );
                    TargetName = sprintf "aaa%d" i
                    LUN = [ lun_me.fromPrim ( uint64 i ) ];
            }
            let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), ( sprintf "a%d" i ), true, ModifiedStatus.NotModified ) :> IConfigFileNode
            let tn = new ConfNode_Target( st, rel, rel.NextID, targetConf ) :> IConfigureNode
            let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim ( uint64 i ), "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode tg
            rel.AddNode tn
            rel.AddNode dd
            rel.AddRelation tn.NodeID dd.NodeID
            rel.AddRelation tg.NodeID tn.NodeID
            rel.AddRelation n.NodeID tg.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_042() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID

        for i = 1 to Constants.MAX_TARGET_GROUP_COUNT_IN_TD + 1 do
            let targetConf = {
                defaultTargetConf with
                    IdentNumber = tnodeidx_me.fromPrim ( uint32 i );
                    TargetName = sprintf "aaa%d" i
                    LUN = [ lun_me.fromPrim 1UL ];
            }
            let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), ( sprintf "a%d" i ), true, ModifiedStatus.NotModified ) :> IConfigFileNode
            let tn = new ConfNode_Target( st, rel, rel.NextID, targetConf ) :> IConfigureNode
            let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim ( uint64 i ), "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode tg
            rel.AddNode tn
            rel.AddNode dd
            rel.AddRelation tn.NodeID dd.NodeID
            rel.AddRelation tg.NodeID tn.NodeID
            rel.AddRelation n.NodeID tg.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 4 ))
        let idx = r |> List.findIndex ( fun itr -> ( snd itr ).StartsWith "CHKMSG_OUT_OF_TARGET_GROUP_COUNT" )
        Assert.True(( fst r.[idx] = n.NodeID ))

    [<Fact>]
    member _.Validate_043() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_MISSING_TARGET_GROUP" ))

    [<Fact>]
    member _.Validate_044() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation cn.NodeID n.NodeID

        for i = 1 to Constants.MAX_TARGET_COUNT_IN_TD do
            let targetConf = {
                defaultTargetConf with
                    IdentNumber = tnodeidx_me.fromPrim ( uint32 i );
                    TargetName = sprintf "aaa%d" i
                    LUN = [ lun_me.fromPrim ( uint64 i ) ];
            }
            let tn = new ConfNode_Target( st, rel, rel.NextID, targetConf ) :> IConfigureNode
            let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim ( uint64 i ), "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode tn
            rel.AddNode dd
            rel.AddRelation tn.NodeID dd.NodeID
            rel.AddRelation tg.NodeID tn.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_045() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode dd
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation cn.NodeID n.NodeID

        for i = 1 to Constants.MAX_TARGET_COUNT_IN_TD do
            let targetConf = {
                defaultTargetConf with
                    IdentNumber = tnodeidx_me.fromPrim ( uint32 i );
                    TargetName = sprintf "aaa%d" i
                    LUN = [ lun_me.fromPrim ( uint64 i ) ];
            }
            let tn = new ConfNode_Target( st, rel, rel.NextID, targetConf ) :> IConfigureNode
            rel.AddNode tn
            rel.AddRelation tn.NodeID dd.NodeID
            rel.AddRelation tg.NodeID tn.NodeID

        let tg2 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "b", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode dd2
        rel.AddNode tg2
        rel.AddRelation n.NodeID tg2.NodeID
        let widx = Constants.MAX_TARGET_COUNT_IN_TD + 1
        let targetConf = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim ( uint32 widx );
                TargetName = sprintf "aaa%d" widx
                LUN = [ lun_me.fromPrim ( uint64 widx ) ];
        }
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, targetConf ) :> IConfigureNode
        rel.AddNode tn2
        rel.AddRelation tn2.NodeID dd2.NodeID
        rel.AddRelation tg2.NodeID tn2.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_OUT_OF_TARGET_COUNT_IN_TD" ))

    [<Fact>]
    member _.Validate_046() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation cn.NodeID n.NodeID

        let targetConf1 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "aaa1"
                LUN = [ lun_me.fromPrim 1UL ];
        }
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, targetConf1 ) :> IConfigureNode
        rel.AddNode tn1
        rel.AddRelation tg.NodeID tn1.NodeID

        for i = 1 to Constants.MAX_TARGET_COUNT_IN_TD do
            let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim ( uint64 i ), "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode dd
            rel.AddRelation tn1.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_047() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg1 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg1
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation n.NodeID tg1.NodeID
        rel.AddRelation cn.NodeID n.NodeID

        let targetConf1 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "aaa1"
                LUN = [ lun_me.fromPrim 1UL ];
        }
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, targetConf1 ) :> IConfigureNode
        rel.AddNode tn1
        rel.AddRelation tg1.NodeID tn1.NodeID

        for i = 1 to Constants.MAX_TARGET_COUNT_IN_TD do
            let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim ( uint64 i ), "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode dd
            rel.AddRelation tn1.NodeID dd.NodeID

        let tg2 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode tg2
        rel.AddRelation n.NodeID tg2.NodeID
        let targetConf2 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 2u;
                TargetName = "aaa2"
                LUN = [ lun_me.fromPrim 2UL ];
        }
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, targetConf2 ) :> IConfigureNode
        rel.AddNode tn2
        rel.AddRelation tg2.NodeID tn2.NodeID

        for i = 1 to 1 do
            let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim ( uint64 ( i + Constants.MAX_TARGET_COUNT_IN_TD ) ), "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode dd
            rel.AddRelation tn2.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 2 ))
        let idx = r |> List.findIndex ( fun itr -> ( snd itr ).StartsWith "CHKMSG_OUT_OF_LU_COUNT_IN_TD" )
        Assert.True(( fst r.[idx] = n.NodeID ))

    [<Fact>]
    member _.Validate_048() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let npconf1 = {
            defaultConf.NetworkPortal.[0] with
                IdentNumber = netportidx_me.fromPrim 0u;
        }
        let npconf2 = {
            defaultConf.NetworkPortal.[0] with
                IdentNumber = netportidx_me.fromPrim 1u;
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np1 = new ConfNode_NetworkPortal( st, rel, rel.NextID, npconf1 ) :> IConfigureNode
        let np2 = new ConfNode_NetworkPortal( st, rel, rel.NextID, npconf2 ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np1
        rel.AddNode np2
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np1.NodeID
        rel.AddRelation n.NodeID np2.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_049() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let npconf1 = {
            defaultConf.NetworkPortal.[0] with
                IdentNumber = netportidx_me.fromPrim 0u;
        }
        let npconf2 = {
            defaultConf.NetworkPortal.[0] with
                IdentNumber = netportidx_me.fromPrim 0u;
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np1 = new ConfNode_NetworkPortal( st, rel, rel.NextID, npconf1 ) :> IConfigureNode
        let np2 = new ConfNode_NetworkPortal( st, rel, rel.NextID, npconf2 ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np1
        rel.AddNode np2
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np1.NodeID
        rel.AddRelation n.NodeID np2.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 2 ))
        Assert.True(( fst r.[0] = np1.NodeID || fst r.[0] = np2.NodeID ))
        Assert.True(( fst r.[1] = np1.NodeID || fst r.[1] = np2.NodeID ))
        Assert.True(( not ( fst r.[0] = fst r.[1] ) ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_DUPLICATE_NETWORK_PORTAL_ID" ))
        Assert.True(( ( snd r.[1] ).StartsWith "CHKMSG_DUPLICATE_NETWORK_PORTAL_ID" ))

    [<Fact>]
    member _.Validate_050() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let targetConf1 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "aaa"
        }
        let targetConf2 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 2u;
                TargetName = "bbb"
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg1 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, targetConf1 ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let tg2 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "b", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, targetConf2 ) :> IConfigureNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode

        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg1
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode tg2
        rel.AddNode tn2
        rel.AddNode dd2
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation tg1.NodeID tn1.NodeID
        rel.AddRelation n.NodeID tg1.NodeID
        rel.AddRelation tn2.NodeID dd2.NodeID
        rel.AddRelation tg2.NodeID tn2.NodeID
        rel.AddRelation n.NodeID tg2.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_051() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tgid = GlbFunc.newTargetGroupID()
        let targetConf1 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "aaa"
        }
        let targetConf2 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 2u;
                TargetName = "bbb"
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg1 = new ConfNode_TargetGroup( st, rel, rel.NextID, tgid, "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, targetConf1 ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let tg2 = new ConfNode_TargetGroup( st, rel, rel.NextID, tgid, "b", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, targetConf2 ) :> IConfigureNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode

        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg1
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode tg2
        rel.AddNode tn2
        rel.AddNode dd2
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation tg1.NodeID tn1.NodeID
        rel.AddRelation n.NodeID tg1.NodeID
        rel.AddRelation tn2.NodeID dd2.NodeID
        rel.AddRelation tg2.NodeID tn2.NodeID
        rel.AddRelation n.NodeID tg2.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 2 ))
        Assert.True(( fst r.[0] = tg1.NodeID || fst r.[0] = tg2.NodeID ))
        Assert.True(( fst r.[1] = tg1.NodeID || fst r.[1] = tg2.NodeID ))
        Assert.True(( not ( fst r.[0] = fst r.[1] ) ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_DUPLICATE_TARGET_GROUP_ID" ))
        Assert.True(( ( snd r.[1] ).StartsWith "CHKMSG_DUPLICATE_TARGET_GROUP_ID" ))

    [<Fact>]
    member _.Validate_052() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let targetConf1 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "aaa"
        }
        let targetConf2 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 2u;
                TargetName = "bbb"
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, targetConf1 ) :> IConfigureNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, targetConf2 ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn1
        rel.AddNode tn2
        rel.AddNode dd
        rel.AddRelation tn1.NodeID dd.NodeID
        rel.AddRelation tn2.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn1.NodeID
        rel.AddRelation tg.NodeID tn2.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_053() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let targetConf1 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "aaa"
        }
        let targetConf2 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "bbb"
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, targetConf1 ) :> IConfigureNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, targetConf2 ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn1
        rel.AddNode tn2
        rel.AddNode dd
        rel.AddRelation tn1.NodeID dd.NodeID
        rel.AddRelation tn2.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn1.NodeID
        rel.AddRelation tg.NodeID tn2.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 2 ))
        Assert.True(( fst r.[0] = tn1.NodeID || fst r.[0] = tn2.NodeID ))
        Assert.True(( fst r.[1] = tn1.NodeID || fst r.[1] = tn2.NodeID ))
        Assert.True(( not ( fst r.[0] = fst r.[1] ) ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_DUPLICATE_TARGET_ID" ))
        Assert.True(( ( snd r.[1] ).StartsWith "CHKMSG_DUPLICATE_TARGET_ID" ))

    [<Fact>]
    member _.Validate_054() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let mult = Constants.LU_DEF_MULTIPLICITY
        let fbs = Blocksize.BS_512
        let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
        let targetConf1 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "aaa"
        }
        let targetConf2 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 2u;
                TargetName = "bbb"
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg1 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, targetConf1 ) :> IConfigureNode
        let dd1 = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", mult, fbs, otl ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let tg2 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "b", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, targetConf2 ) :> IConfigureNode
        let dd2 = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "", mult, fbs, otl ) :> ILUNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg1
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddNode tg2
        rel.AddNode tn2
        rel.AddNode dd2
        rel.AddNode dm2
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation tg1.NodeID tn1.NodeID
        rel.AddRelation dd2.NodeID dm2.NodeID
        rel.AddRelation tn2.NodeID dd2.NodeID
        rel.AddRelation tg2.NodeID tn2.NodeID
        rel.AddRelation n.NodeID tg1.NodeID
        rel.AddRelation n.NodeID tg2.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 2 ))
        Assert.True(( fst r.[0] = dm1.NodeID || fst r.[0] = dm2.NodeID ))
        Assert.True(( fst r.[1] = dm1.NodeID || fst r.[1] = dm2.NodeID ))
        Assert.True(( not ( fst r.[0] = fst r.[1] ) ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_DUPLICATE_MEDIA_ID" ))
        Assert.True(( ( snd r.[1] ).StartsWith "CHKMSG_DUPLICATE_MEDIA_ID" ))

    [<Fact>]
    member _.Validate_055() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let targetConf1 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "aaa"
        }
        let targetConf2 = {
            defaultTargetConf with
                IdentNumber = tnodeidx_me.fromPrim 2u;
                TargetName = "aaa"
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, targetConf1 ) :> IConfigureNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, targetConf2 ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn1
        rel.AddNode tn2
        rel.AddNode dd
        rel.AddRelation tn1.NodeID dd.NodeID
        rel.AddRelation tn2.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn1.NodeID
        rel.AddRelation tg.NodeID tn2.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 2 ))
        Assert.True(( fst r.[0] = tn1.NodeID || fst r.[0] = tn2.NodeID ))
        Assert.True(( fst r.[1] = tn1.NodeID || fst r.[1] = tn2.NodeID ))
        Assert.True(( not ( fst r.[0] = fst r.[1] ) ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_DUPLICATE_TARGET_NAME" ))
        Assert.True(( ( snd r.[1] ).StartsWith "CHKMSG_DUPLICATE_TARGET_NAME" ))

    [<Fact>]
    member _.Validate_056() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd1
        rel.AddNode dd2
        rel.AddRelation tn.NodeID dd1.NodeID
        rel.AddRelation tn.NodeID dd2.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 2 ))
        Assert.True(( fst r.[0] = dd1.NodeID || fst r.[0] = dd2.NodeID ))
        Assert.True(( fst r.[1] = dd1.NodeID || fst r.[1] = dd2.NodeID ))
        Assert.True(( not ( fst r.[0] = fst r.[1] ) ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_DUPLICATE_LUN" ))
        Assert.True(( ( snd r.[1] ).StartsWith "CHKMSG_DUPLICATE_LUN" ))

    [<Fact>]
    member _.Validate_057() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddNode dm2
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation n.NodeID dm2.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = dm2.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_RELATION" ))

    [<Fact>]
    member _.Validate_058() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let npConf = {
            defaultConf.NetworkPortal.[0] with
                TargetPortalGroupTag = tpgt_me.fromPrim 1us;
        }
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, npConf ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = np.NodeID ))

    [<Fact>]
    member _.Validate_059() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tgname = String.replicate ( Constants.MAX_TARGET_GROUP_NAME_STR_LENGTH + 1 ) "a" 
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), tgname, true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = tg.NodeID ))

    [<Fact>]
    member _.Validate_060() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn.NodeID n.NodeID
        let r = n.Validate [ ( confnode_me.fromPrim 99UL, "aaa" ) ]
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = ( confnode_me.fromPrim 99UL, "aaa" ) ))

    [<Fact>]
    member _.Validate_061() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let cn1 = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let cn2 = new ConfNode_Controller( st, rel, rel.NextID, defContConf, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode cn1
        rel.AddNode cn2
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        rel.AddRelation cn1.NodeID n.NodeID
        rel.AddRelation cn2.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_TOO_MANY_PARENT" ))

    [<Fact>]
    member _.Validate_062() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_MISSING_PARENT" ))

    [<Fact>]
    member _.GetAccessibleLUNodes_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        rel.AddNode n
        let r = ( n :?> ConfNode_TargetDevice ).GetAccessibleLUNodes()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetAccessibleLUNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd
        rel.AddNode dm
        rel.AddRelation dd.NodeID dm.NodeID
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID
        let r = ( n :?> ConfNode_TargetDevice ).GetAccessibleLUNodes()
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = dd ))

    [<Fact>]
    member _.GetAccessibleLUNodes_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg1 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let tg2 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "b", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg1
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddNode tg2
        rel.AddNode tn2
        rel.AddNode dd2
        rel.AddNode dm2
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation tg1.NodeID tn1.NodeID
        rel.AddRelation dd2.NodeID dm2.NodeID
        rel.AddRelation tn2.NodeID dd2.NodeID
        rel.AddRelation tg2.NodeID tn2.NodeID
        rel.AddRelation n.NodeID tg1.NodeID
        rel.AddRelation n.NodeID tg2.NodeID
        rel.AddRelation n.NodeID np.NodeID

        let r = ( n :?> ConfNode_TargetDevice ).GetAccessibleLUNodes()
        Assert.True(( r.Length = 2 ))
        Assert.True(( r = [ dd1; dd2 ] || r = [ dd2; dd1 ] ))

    [<Fact>]
    member _.GetAccessibleLUNodes_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddNode tn2
        rel.AddNode dd2
        rel.AddNode dm2
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation tg.NodeID tn1.NodeID
        rel.AddRelation dd2.NodeID dm2.NodeID
        rel.AddRelation tn2.NodeID dd2.NodeID
        rel.AddRelation tg.NodeID tn2.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID

        let r = ( n :?> ConfNode_TargetDevice ).GetAccessibleLUNodes()
        Assert.True(( r.Length = 2 ))
        Assert.True(( r = [ dd1; dd2 ] || r = [ dd2; dd1 ] ))

    [<Fact>]
    member _.GetAccessibleLUNodes_005() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddNode dd2
        rel.AddNode dm2
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation tn.NodeID dd1.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation dd2.NodeID dm2.NodeID
        rel.AddRelation tn.NodeID dd2.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID

        let r = ( n :?> ConfNode_TargetDevice ).GetAccessibleLUNodes()
        Assert.True(( r.Length = 2 ))
        Assert.True(( r = [ dd1; dd2 ] || r = [ dd2; dd1 ] ))

    [<Fact>]
    member _.GetAccessibleLUNodes_006() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn1
        rel.AddNode dd
        rel.AddNode dm
        rel.AddNode tn2
        rel.AddRelation dd.NodeID dm.NodeID
        rel.AddRelation tn1.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn1.NodeID
        rel.AddRelation tn2.NodeID dd.NodeID
        rel.AddRelation tg.NodeID tn2.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID

        let r = ( n :?> ConfNode_TargetDevice ).GetAccessibleLUNodes()
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = dd ))

    [<Fact>]
    member _.GetAccessibleLUNodes_007() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid = GlbFunc.newTargetDeviceID()
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf ) :> IConfigFileNode
        let np = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf.NetworkPortal.[0] ) :> IConfigureNode
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultTargetConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode np
        rel.AddNode tg
        rel.AddNode tn
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddNode dd2
        rel.AddNode dm2
        rel.AddRelation dd2.NodeID dm2.NodeID
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation tn.NodeID dd1.NodeID
        rel.AddRelation tg.NodeID tn.NodeID
        rel.AddRelation tg.NodeID dd2.NodeID
        rel.AddRelation n.NodeID tg.NodeID
        rel.AddRelation n.NodeID np.NodeID

        let r = ( n :?> ConfNode_TargetDevice ).GetAccessibleLUNodes()
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = dd1 ))

    [<Fact>]
    member _.CreateUpdatedNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tdid1 = GlbFunc.newTargetDeviceID()
        let n1 = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid1, defaultConf ) :> IConfigFileNode

        let tdid2 = GlbFunc.newTargetDeviceID()
        let p1 = {
            defaultConf.NegotiableParameters.Value with
                MaxRecvDataSegmentLength = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength + 1u;
        }
        let p2 = {
            defaultConf.LogParameters.Value with
                SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT + 1u;
        }
        let n2 = ( n1 :?> ConfNode_TargetDevice ).CreateUpdatedNode tdid2 "XXX" p1 p2
        Assert.True(( n2.TargetDeviceID = tdid2 ))
        Assert.True(( n2.TargetDeviceName = "XXX" ))
        Assert.True(( n2.NegotiableParameters.MaxRecvDataSegmentLength = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength + 1u ))
        Assert.True(( n2.LogParameters.SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT + 1u ))
        Assert.True(( ( n2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
        Assert.True(( ( n2 :> IConfigFileNode ).NodeID = n1.NodeID ))

    [<Fact>]
    member _.GetConfigureData_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                NetworkPortal = []; // NetworkPortal values are ignored
        }
        let nconf1 = {
            defaultConf.NetworkPortal.[0] with
                IdentNumber = netportidx_me.fromPrim 0u;
        }
        let nconf2 = {
            defaultConf.NetworkPortal.[0] with
                IdentNumber = netportidx_me.fromPrim 1u;
        }
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), conf )  :> IConfigureNode
        let np1 = new ConfNode_NetworkPortal( st, rel, rel.NextID, nconf1 ) :> IConfigureNode
        let np2 = new ConfNode_NetworkPortal( st, rel, rel.NextID, nconf2 ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode np1
        rel.AddNode np2
        rel.AddRelation n.NodeID np1.NodeID
        rel.AddRelation n.NodeID np2.NodeID
        
        let r = ( n :?> ConfNode_TargetDevice ).GetConfigureData()
        Assert.True(( r.DeviceName = defaultConf.DeviceName ))
        Assert.True(( r.NegotiableParameters.IsSome ))
        Assert.True(( r.NegotiableParameters.Value = defaultConf.NegotiableParameters.Value ))
        Assert.True(( r.LogParameters.IsSome ))
        Assert.True(( r.LogParameters.Value = defaultConf.LogParameters.Value ))
        Assert.True(( r.NetworkPortal.Length = 2 ))
        Assert.True(( r.NetworkPortal = [ nconf1; nconf2 ] || r.NetworkPortal = [ nconf2; nconf1 ] ))

    [<Fact>]
    member _.GetChildNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        rel.AddNode n
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetChildNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
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
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
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
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
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
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        rel.AddNode n
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetDescendantNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
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
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
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
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
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
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        rel.AddNode n
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetParentNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
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
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
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
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
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
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
        rel.AddNode n
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetAncestorNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
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
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
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
        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defaultConf ) :> IConfigFileNode
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
        let tdid = GlbFunc.newTargetDeviceID()

        let n = new ConfNode_TargetDevice( st, rel, rel.NextID, tdid, defaultConf )
        Assert.True(( ( n :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))

        let n2 = n.SetModified()
        Assert.True(( ( n2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))

        let n3 = ( n2 :> IConfigFileNode ).ResetModifiedFlag()
        Assert.True(( n3.Modified = ModifiedStatus.NotModified ))

    [<Fact>]
    member _.GenNewTargetDeviceID_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.Zero, defaultConf );
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.fromPrim( 1u ), defaultConf );
        ]
        let n = ConfNode_TargetDevice.GenNewTargetDeviceID v
        Assert.True(( n = tdid_me.fromPrim( 2u ) ))

    [<Fact>]
    member _.GenNewTargetDeviceID_002() =
        let n = ConfNode_TargetDevice.GenNewTargetDeviceID []
        Assert.True(( n = tdid_me.fromPrim( 1u ) ))

    [<Fact>]
    member _.GenNewTargetDeviceID_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.Zero, defaultConf );
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.fromPrim( 1u ), defaultConf );
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.fromPrim( 2u ), defaultConf );
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.fromPrim( UInt32.MaxValue ), defaultConf );
        ]
        let n = ConfNode_TargetDevice.GenNewTargetDeviceID v
        Assert.True(( n = tdid_me.fromPrim( 3u ) ))

    [<Fact>]
    member _.GenDefaultTargetDeviceName_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.Zero, { defaultConf with DeviceName="TargetDevice_00000" } );
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.fromPrim( 1u ), { defaultConf with DeviceName="TargetDevice_00001" } );
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.fromPrim( 2u ), { defaultConf with DeviceName="TargetDevice_000002" } );
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.fromPrim( 3u ), { defaultConf with DeviceName="TargetDevice_3" } );
        ]
        let n = ConfNode_TargetDevice.GenDefaultTargetDeviceName v
        Assert.True(( n = "TargetDevice_00004" ))

    [<Fact>]
    member _.GenDefaultTargetDeviceName_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.Zero, { defaultConf with DeviceName="TargetDevice_00000" } );
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.fromPrim( 1u ), { defaultConf with DeviceName="aaa_00001" } );
        ]
        let n = ConfNode_TargetDevice.GenDefaultTargetDeviceName v
        Assert.True(( n = "TargetDevice_00001" ))

    [<Fact>]
    member _.GenDefaultTargetDeviceName_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.Zero, { defaultConf with DeviceName="TargetDevice_00000" } );
            new ConfNode_TargetDevice( st, rel, rel.NextID, tdid_me.fromPrim( 1u ), { defaultConf with DeviceName="TargetDevice_00001a" } );
        ]
        let n = ConfNode_TargetDevice.GenDefaultTargetDeviceName v
        Assert.True(( n = "TargetDevice_00001" ))

    [<Fact>]
    member _.GenDefaultTargetDeviceName_004() =
        let n = ConfNode_TargetDevice.GenDefaultTargetDeviceName []
        Assert.True(( n = "TargetDevice_00000" ))

    [<Fact>]
    member _.SortKey_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : TargetDeviceConf.T_TargetDevice = {
            NetworkPortal = [];
            NegotiableParameters = None;
            LogParameters = None;
            DeviceName = "abcrrtt";
        }
        let cid = confnode_me.fromPrim 14567UL
        let tdid = GlbFunc.newTargetDeviceID()
        let n = new ConfNode_TargetDevice( st, rel, cid, tdid, conf ) :> IConfigFileNode
        let v = n.SortKey
        Assert.True(( v.Length = 4 ))
        Assert.True(( v.[0] = ClientConst.SORT_KEY_TYPE_TargetDevice ))
        Assert.True(( v.[1] = "abcrrtt" ))
        Assert.True(( v.[2] = tdid_me.toString tdid ))
        Assert.True(( v.[3] = sprintf "%016X" 14567UL ))

    [<Fact>]
    member _.TempExportData_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = confnode_me.fromPrim 14567UL
        let tdid = GlbFunc.newTargetDeviceID()
        let negoParam : TargetDeviceConf.T_NegotiableParameters = {
            MaxRecvDataSegmentLength = 1u;
            MaxBurstLength = 2u;
            FirstBurstLength = 3u;
            DefaultTime2Wait = 4us;
            DefaultTime2Retain = 5us;
            MaxOutstandingR2T = 6us;
        }
        let logParam : TargetDeviceConf.T_LogParameters = {
            SoftLimit = 7u;
            HardLimit = 8u;
            LogLevel = LogLevel.LOGLEVEL_INFO;
        }
        let n = new ConfNode_TargetDevice( st, rel, cid, tdid, "aabbcc", negoParam, logParam, ModifiedStatus.Modified ) :> IConfigureNode
        let v = n.TempExportData
        Assert.True(( v.TypeName = ClientConst.TEMPEXP_NN_TargetDevice ))
        Assert.True(( v.NodeID = 14567UL ))
        Assert.True(( v.Values.Length = 11 ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "ID" ) |> _.Value = ( tdid_me.toString tdid ) ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "Name" ) |> _.Value = "aabbcc" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "NegotiableParameters.MaxRecvDataSegmentLength" ) |> _.Value = "1" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "NegotiableParameters.MaxBurstLength" ) |> _.Value = "2" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "NegotiableParameters.FirstBurstLength" ) |> _.Value = "3" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "NegotiableParameters.DefaultTime2Wait" ) |> _.Value = "4" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "NegotiableParameters.DefaultTime2Retain" ) |> _.Value = "5" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "NegotiableParameters.MaxOutstandingR2T" ) |> _.Value = "6" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LogParameters.SoftLimit" ) |> _.Value = "7" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LogParameters.HardLimit" ) |> _.Value = "8" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LogParameters.LogLevel" ) |> _.Value = "INFO" ))


