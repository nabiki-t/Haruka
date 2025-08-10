namespace Haruka.Test.UT.Client

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.IODataTypes
open Haruka.Test

type ConfNode_NetworkPortal_Test() =

    let defaultConf : TargetDeviceConf.T_NetworkPortal = {
        IdentNumber = netportidx_me.fromPrim 0u;
        TargetPortalGroupTag = tpgt_me.zero;
        TargetAddress = "a";
        PortNumber = 1us;
        DisableNagle = false;
        ReceiveBufferSize = 8190;
        SendBufferSize = 8192;
        WhiteList = [];
    }

    let defTargetDeviceConf : TargetDeviceConf.T_TargetDevice = {
        NetworkPortal = [];
        NegotiableParameters = None;
        LogParameters = None;
        DeviceName = "abc";
    }

    [<Fact>]
    member _.Constractor_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let n = new ConfNode_NetworkPortal( st, rel, cid, defaultConf ) :> IConfigureNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.NodeTypeName = "Network Portal" ))
        Assert.True(( ( n :?> ConfNode_NetworkPortal ).NetworkPortal = defaultConf ))

    [<Fact>]
    member _.Constractor_002() =
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
        let n = ConfNode_NetworkPortal( st, rel, cid, te )
        Assert.True(( ( n :> IConfigureNode ).NodeID = cid ))
        Assert.True(( n.NetworkPortal.IdentNumber = netportidx_me.fromPrim 0u ))
        Assert.True(( n.NetworkPortal.TargetPortalGroupTag = tpgt_me.zero ))
        Assert.True(( n.NetworkPortal.TargetAddress = "" ))
        Assert.True(( n.NetworkPortal.PortNumber = Constants.DEFAULT_ISCSI_PORT_NUM ))
        Assert.True(( n.NetworkPortal.DisableNagle = Constants.DEF_DISABLE_NAGLE_IN_NP ))
        Assert.True(( n.NetworkPortal.ReceiveBufferSize = Constants.DEF_RECEIVE_BUFFER_SIZE_IN_NP ))
        Assert.True(( n.NetworkPortal.SendBufferSize = Constants.DEF_SEND_BUFFER_SIZE_IN_NP ))
        Assert.True(( n.NetworkPortal.WhiteList = [] ))

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
                    Name = "ID";
                    Value = "11423";
                }
                {
                    Name = "TPGT";
                    Value = "985";
                }
                {
                    Name = "TargetAddress";
                    Value = "aassdr";
                }
                {
                    Name = "PortNumber";
                    Value = "9658";
                }
                {
                    Name = "DisableNagle";
                    Value = "false";
                }
                {
                    Name = "ReceiveBufferSize";
                    Value = "76589";
                }
                {
                    Name = "SendBufferSize";
                    Value = "9865";
                }
                {
                    Name = "WhiteList";
                    Value = "";
                }
            ];
        }
        let n = ConfNode_NetworkPortal( st, rel, cid, te )
        Assert.True(( ( n :> IConfigureNode ).NodeID = cid ))
        Assert.True(( n.NetworkPortal.IdentNumber = netportidx_me.fromPrim 11423u ))
        Assert.True(( n.NetworkPortal.TargetPortalGroupTag = tpgt_me.fromPrim 985us ))
        Assert.True(( n.NetworkPortal.TargetAddress = "aassdr" ))
        Assert.True(( n.NetworkPortal.PortNumber = 9658us ))
        Assert.True(( n.NetworkPortal.DisableNagle = false ))
        Assert.True(( n.NetworkPortal.ReceiveBufferSize = 76589 ))
        Assert.True(( n.NetworkPortal.SendBufferSize = 9865 ))
        Assert.True(( n.NetworkPortal.WhiteList = [] ))

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
                    Name = "WhiteList";
                    Value = "IPv4Linklocal";
                }
            ];
        }
        let n = ConfNode_NetworkPortal( st, rel, cid, te )
        Assert.True(( n.NetworkPortal.WhiteList = [ IPCondition.IPv4Linklocal ] ))

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
                    Name = "WhiteList";
                    Value = "IPv4Linklocal\tIPv6Private";
                }
            ];
        }
        let n = ConfNode_NetworkPortal( st, rel, cid, te )
        Assert.True(( n.NetworkPortal.WhiteList = [ IPCondition.IPv4Linklocal; IPCondition.IPv6Private; ] ))

    [<Fact>]
    member _.Validate_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                TargetPortalGroupTag = tpgt_me.fromPrim 1us
        }
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, conf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_UNSUPPORTED_TPGT_VALUE" ))

    [<Fact>]
    member _.Validate_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                TargetAddress = ""
        }
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, conf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                TargetAddress = String.replicate Constants.MAX_TARGET_ADDRESS_STR_LENGTH "a"
        }
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, conf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_005() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                TargetAddress = String.replicate ( Constants.MAX_TARGET_ADDRESS_STR_LENGTH + 1 ) "a"
        }
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, conf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_TARGET_ADDRESS_TOO_LONG" ))

    [<Fact>]
    member _.Validate_006() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                PortNumber = 0us
        }
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, conf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_PORT_NUMBER" ))

    [<Fact>]
    member _.Validate_007() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dm = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode td
        rel.AddNode n
        rel.AddNode dm
        rel.AddRelation td.NodeID n.NodeID
        rel.AddRelation n.NodeID dm.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = dm.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_RELATION" ))

    [<Fact>]
    member _.Validate_008() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let wid = rel.NextID
        let r = n.Validate [ ( wid, "abc" ) ]
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = wid ))
        Assert.True(( snd r.[0] = "abc" ))

    [<Fact>]
    member _.Validate_009() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                TargetPortalGroupTag = tpgt_me.fromPrim 1us
        }
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, conf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let wid = rel.NextID
        let r = n.Validate [ ( wid, "abc" ) ]
        Assert.True(( r.Length = 2 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_UNSUPPORTED_TPGT_VALUE" ))
        Assert.True(( fst r.[1] = wid ))
        Assert.True(( snd r.[1] = "abc" ))

    [<Fact>]
    member _.Validate_010() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_MISSING_PARENT" ))

    [<Fact>]
    member _.Validate_011() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td1 = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let td2 = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode td1
        rel.AddNode td2
        rel.AddNode n
        rel.AddRelation td1.NodeID n.NodeID
        rel.AddRelation td2.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_TOO_MANY_PARENT" ))

    [<Fact>]
    member _.Validate_012() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let testconf = {
            defaultConf with
                ReceiveBufferSize = 0;
        }
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, testconf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_013() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let testconf = {
            defaultConf with
                ReceiveBufferSize = -1;
        }
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, testconf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_RECEIVE_BUFFER_SIZE" ))

    [<Fact>]
    member _.Validate_014() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let testconf = {
            defaultConf with
                SendBufferSize = 0;
        }
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, testconf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_015() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let testconf = {
            defaultConf with
                SendBufferSize = -1;
        }
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, testconf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_SEND_BUFFER_SIZE" ))

    [<Fact>]
    member _.Validate_016() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let testconf = {
            defaultConf with
                WhiteList = [
                    for i = 1 to Constants.MAX_IP_WHITELIST_COUNT do
                        yield IPCondition.Any;
                ]
        }
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, testconf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_017() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let testconf = {
            defaultConf with
                WhiteList = [
                    for i = 1 to Constants.MAX_IP_WHITELIST_COUNT + 1 do
                        yield IPCondition.Any;
                ]
        }
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, testconf ) :> IConfigureNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_IP_WHITELIST_TOO_LONG" ))

    [<Fact>]
    member _.CreateUpdatedNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf )
        let conf : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.fromPrim 1u;
            TargetPortalGroupTag = tpgt_me.fromPrim 1us;
            TargetAddress = "b";
            PortNumber = 2us;
            DisableNagle = true;
            ReceiveBufferSize = 4096;
            SendBufferSize = 4096;
            WhiteList = [];
        }
        let n2 = n.CreateUpdatedNode( conf )
        Assert.True(( n2.NetworkPortal = conf ))

    [<Fact>]
    member _.GetChildNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode n
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetChildNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        Assert.True(( Object.ReferenceEquals( r.[0], dm3 ) ))

    [<Fact>]
    member _.GetDescendantNodes_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode n
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetDescendantNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        Assert.True(( Object.ReferenceEquals( r.[0], dm2 ) ))

    [<Fact>]
    member _.GetParentNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode n
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetParentNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode dm3
        rel.AddNode n
        rel.AddRelation dm1.NodeID n.NodeID
        rel.AddRelation dm2.NodeID n.NodeID
        rel.AddRelation dm3.NodeID n.NodeID
        let r = n.GetParentNodes<DummyNode2>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( Object.ReferenceEquals( r.[0], dm3 ) ))

    [<Fact>]
    member _.GetAncestorNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode n
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetAncestorNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_NetworkPortal( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode dm3
        rel.AddNode n
        rel.AddRelation dm1.NodeID n.NodeID
        rel.AddRelation dm2.NodeID dm1.NodeID
        rel.AddRelation dm3.NodeID dm2.NodeID
        let r = n.GetAncestorNode<DummyNode2>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( Object.ReferenceEquals( r.[0], dm2 ) ))

    [<Fact>]
    member _.GenNewD_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_NetworkPortal( st, rel, rel.NextID, { defaultConf with IdentNumber = netportidx_me.fromPrim 0u } );
            new ConfNode_NetworkPortal( st, rel, rel.NextID, { defaultConf with IdentNumber = netportidx_me.fromPrim 1u } );
        ]
        let n = ConfNode_NetworkPortal.GenNewID v
        Assert.True(( n = netportidx_me.fromPrim 2u ))

    [<Fact>]
    member _.GenNewID_002() =
        let n = ConfNode_NetworkPortal.GenNewID []
        Assert.True(( n = netportidx_me.fromPrim 0u ))

    [<Fact>]
    member _.GenNewID_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_NetworkPortal( st, rel, rel.NextID, { defaultConf with IdentNumber = netportidx_me.fromPrim 0u } );
            new ConfNode_NetworkPortal( st, rel, rel.NextID, { defaultConf with IdentNumber = netportidx_me.fromPrim 1u } );
            new ConfNode_NetworkPortal( st, rel, rel.NextID, { defaultConf with IdentNumber = netportidx_me.fromPrim 2u } );
            new ConfNode_NetworkPortal( st, rel, rel.NextID, { defaultConf with IdentNumber = netportidx_me.fromPrim UInt32.MaxValue } );
        ]
        let n = ConfNode_NetworkPortal.GenNewID v
        Assert.True(( n = netportidx_me.fromPrim 3u ))

    [<Fact>]
    member _.SortKey_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.fromPrim 456u;
            TargetPortalGroupTag = tpgt_me.zero;
            TargetAddress = "aawweea";
            PortNumber = 159us;
            DisableNagle = false;
            ReceiveBufferSize = 8190;
            SendBufferSize = 8192;
            WhiteList = [];
        }
        let n = new ConfNode_NetworkPortal( st, rel, confnode_me.fromPrim 998UL, conf ) :> IConfigureNode
        let v = n.SortKey
        Assert.True(( v.Length = 5 ))
        Assert.True(( v.[0] = ClientConst.SORT_KEY_TYPE_NetworkPortal ))
        Assert.True(( v.[1] = "aawweea" ))
        Assert.True(( v.[2] = sprintf "%04X" 159us ))
        Assert.True(( v.[3] = sprintf "%08X" 456u ))
        Assert.True(( v.[4] = sprintf "%016X" 998UL ))
    
    [<Fact>]
    member _.TempExportData_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.fromPrim 456u;
            TargetPortalGroupTag = tpgt_me.fromPrim 159us;
            TargetAddress = "aawweea";
            PortNumber = 1155us;
            DisableNagle = false;
            ReceiveBufferSize = 8190;
            SendBufferSize = 8192;
            WhiteList = [];
        }
        let n = new ConfNode_NetworkPortal( st, rel, confnode_me.fromPrim 998UL, conf ) :> IConfigureNode
        let v = n.TempExportData
        Assert.True(( v.TypeName = ClientConst.TEMPEXP_NN_NetworkPortal ))
        Assert.True(( v.NodeID = 998UL ))
        Assert.True(( v.Values.Length = 8 ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "ID" ) |> _.Value = "456" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "TPGT" ) |> _.Value = "159" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "TargetAddress" ) |> _.Value = "aawweea" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "PortNumber" ) |> _.Value = "1155" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "DisableNagle" ) |> _.Value = "false" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "ReceiveBufferSize" ) |> _.Value = "8190" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "SendBufferSize" ) |> _.Value = "8192" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "WhiteList" ) |> _.Value = "" ))
    
    [<Fact>]
    member _.TempExportData_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = netportidx_me.fromPrim 456u;
            TargetPortalGroupTag = tpgt_me.fromPrim 159us;
            TargetAddress = "aawweea";
            PortNumber = 1155us;
            DisableNagle = false;
            ReceiveBufferSize = 8190;
            SendBufferSize = 8192;
            WhiteList = [ IPCondition.IPv4Global; IPCondition.IPv6Any; ];
        }
        let n = new ConfNode_NetworkPortal( st, rel, confnode_me.fromPrim 998UL, conf ) :> IConfigureNode
        let v = n.TempExportData
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "WhiteList" ) |> _.Value = "IPv4Global\tIPv6Any" ))

