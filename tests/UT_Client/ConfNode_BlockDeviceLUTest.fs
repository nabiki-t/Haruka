namespace Haruka.Test.UT.Client

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.IODataTypes
open Haruka.Test

type ConfNode_BlockDeviceLU_Test() =

    let defTargetConf : TargetGroupConf.T_Target = {
        IdentNumber = tnodeidx_me.fromPrim 1u;
        TargetPortalGroupTag = tpgt_me.fromPrim 0us;
        TargetName = "aaa";
        TargetAlias = "";
        LUN = [ lun_me.fromPrim 1UL ];
        Auth = TargetGroupConf.U_None();
    }

    [<Fact>]
    member _.Constractor_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        let dm = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "aaa" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode dm
        rel.AddRelation n.NodeID dm.NodeID
        Assert.True(( n.NodeID = confnode_me.fromPrim 1UL ))
        Assert.True(( n.NodeTypeName = "Block Device LU" ))
        Assert.True(( n.LUN = lun_me.fromPrim 1UL ))
        Assert.True(( n.LUConfData.LUN = lun_me.fromPrim 1UL ))
        Assert.True(( n.LUConfData.WorkPath = "" ))
        match n.LUConfData.LUDevice with
        | TargetGroupConf.T_DEVICE.U_BlockDevice( x ) ->
            match x.Peripheral with
            | TargetGroupConf.T_MEDIA.U_DummyMedia( y ) ->
                Assert.True(( y.IdentNumber = mediaidx_me.fromPrim 1u ))
                Assert.True(( y.MediaName = "aaa" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.Constractor_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode n
        try
            let _ = n.LUConfData
            Assert.Fail __LINE__
        with
        | _ as x ->
            Assert.True(( x.Message = "Unexpected error. ConfNode_BlockDeviceLU must have only one child node." ))

    [<Fact>]
    member _.Constractor_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode n
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "a" ) :> IMediaNode
        rel.AddNode dm1
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "b" ) :> IMediaNode
        rel.AddNode dm2
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation n.NodeID dm2.NodeID
        try
            let _ = n.LUConfData
            Assert.Fail __LINE__
        with
        | _ as x ->
            Assert.True(( x.Message = "Unexpected error. ConfNode_BlockDeviceLU must have only one child node." ))

    [<Fact>]
    member _.Constractor_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode n
        let dm1 = new DummyNode( rel.NextID, "d1" ) :> IConfigureNode
        rel.AddNode dm1
        rel.AddRelation n.NodeID dm1.NodeID
        try
            let _ = n.LUConfData
            Assert.Fail __LINE__
        with
        | _ as x ->
            Assert.True(( x.Message = "Unexpected error. ConfNode_BlockDeviceLU must have media node." ))

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
        let n = new ConfNode_BlockDeviceLU( st, rel, cid, te ) :> ILUNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.LUN = lun_me.fromPrim 1UL ))
        Assert.True(( n.LUName = "" ))

    [<Fact>]
    member _.Constractor_006() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let te : TempExport.T_Node = {
            TypeName = "aaaa";  // ignored
            NodeID = 999UL;     // ignored
            Values = [
                {
                    Name = "LUN";
                    Value = "4455";
                }
                {
                    Name = "Name";
                    Value = "bbb";
                }
            ];
        }
        let n = new ConfNode_BlockDeviceLU( st, rel, cid, te ) :> ILUNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.LUN = lun_me.fromPrim 4455UL ))
        Assert.True(( n.LUName = "bbb" ))

    [<Fact>]
    member _.Validate_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tn = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode tn
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation tn.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tn = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode tn
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation tn.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        let r = n.Validate [ ( confnode_me.fromPrim 1UL, "AAA" ) ]
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = confnode_me.fromPrim 1UL ))
        Assert.True(( snd r.[0] = "AAA" ))

    [<Fact>]
    member _.Validate_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tn = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 0UL, "" ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        let dm3 = new DummyNode( rel.NextID, "d1" ) :> IConfigureNode
        rel.AddNode tn
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode dm3
        rel.AddRelation tn.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation n.NodeID dm2.NodeID
        rel.AddRelation n.NodeID dm3.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 3 ))

    [<Fact>]
    member _.Validate_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tn = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 0UL, "" ) :> ILUNode
        let dm3 = new DummyNode( rel.NextID, "d1" ) :> IConfigureNode
        rel.AddNode tn
        rel.AddNode n
        rel.AddNode dm3
        rel.AddRelation tn.NodeID n.NodeID
        rel.AddRelation n.NodeID dm3.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 3 ))

    [<Fact>]
    member _.Validate_LUN_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tn = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 0UL, "" ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode tn
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation tn.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LUN_VALUE" ))

    [<Fact>]
    member _.Validate_LUN_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tn = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim ( Constants.MAX_LUN_VALUE + 1UL ), "" ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode tn
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation tn.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LUN_VALUE" ))

    [<Fact>]
    member _.Validate_LUName_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tn = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let luname = "a"
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, luname ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode tn
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation tn.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_LUName_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tn = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let luname = String.replicate ( Constants.MAX_LU_NAME_STR_LENGTH ) "a"
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, luname ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode tn
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation tn.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_LUName_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tn = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let luname = String.replicate ( Constants.MAX_LU_NAME_STR_LENGTH + 1 ) "a"
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, luname ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode tn
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation tn.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_LU_NAME_TOO_LONG" ))

    [<Fact>]
    member _.Validate_Media_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tn = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode tn
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddRelation tn.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation n.NodeID dm2.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_OUT_OF_MEDIA_COUNT" ))

    [<Fact>]
    member _.Validate_Media_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tn = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tn
        rel.AddNode n
        rel.AddRelation tn.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_MISSING_MEDIA" ))

    [<Fact>]
    member _.Validate_InvalidRelation_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tn = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm3 = new DummyNode( rel.NextID, "d1" ) :> IConfigureNode
        rel.AddNode tn
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm3
        rel.AddRelation tn.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation n.NodeID dm3.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = dm3.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_RELATION" ))

    [<Fact>]
    member _.Validate_Parent_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        let dm = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode dm
        rel.AddRelation n.NodeID dm.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_MISSING_PARENT" ))

    [<Fact>]
    member _.Validate_Parent_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tg1 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        let dm = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode tg1
        rel.AddNode tn1
        rel.AddNode tn2
        rel.AddNode n
        rel.AddNode dm
        rel.AddRelation tg1.NodeID tn1.NodeID
        rel.AddRelation tg1.NodeID tn2.NodeID
        rel.AddRelation tn1.NodeID n.NodeID
        rel.AddRelation tn2.NodeID n.NodeID
        rel.AddRelation n.NodeID dm.NodeID
        let r = n.Validate []
        Assert.True(( r = [] ))

    [<Fact>]
    member _.Validate_Parent_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tg1 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tg2 = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "b", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, defTargetConf ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        let dm = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode tg1
        rel.AddNode tg2
        rel.AddNode tn1
        rel.AddNode tn2
        rel.AddNode n
        rel.AddNode dm
        rel.AddRelation tg1.NodeID tn1.NodeID
        rel.AddRelation tg2.NodeID tn2.NodeID
        rel.AddRelation tn1.NodeID n.NodeID
        rel.AddRelation tn2.NodeID n.NodeID
        rel.AddRelation n.NodeID dm.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_BELONGS_MULTI_GROUP" ))

    [<Fact>]
    member _.GetMediaNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode n
        let r = n.GetDescendantNodes<IMediaNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetMediaNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        let dm = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode dm
        rel.AddRelation n.NodeID dm.NodeID
        let r = n.GetDescendantNodes<IMediaNode>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = dm ))

    [<Fact>]
    member _.GetMediaNode_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation n.NodeID dm2.NodeID
        let r = n.GetDescendantNodes<IMediaNode>()
        Assert.True(( r.Length = 2 ))
        Assert.True(( r = [ dm1; dm2 ] || r = [ dm2; dm1 ] ))

    [<Fact>]
    member _.GetChildNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode n
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetChildNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
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
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
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
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
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
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode n
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetDescendantNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
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
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
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
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
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
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode n
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetParentNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
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
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
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
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
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
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode n
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetAncestorNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
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
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
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
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
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
    member _.CreateUpdatedNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "aaa" )
        let n2 = n.CreateUpdatedNode ( lun_me.fromPrim 2UL ) "bbb" :> ILUNode
        Assert.True(( n2.LUN = lun_me.fromPrim 2UL ))
        Assert.True(( n2.LUName = "bbb" ))

    [<Fact>]
    member _.GenDefaultLUN_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "aaa" ) :> ILUNode;
            new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "aaa" ) :> ILUNode;
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_BlockDeviceLU.GenDefaultLUN v
        Assert.True(( n = lun_me.fromPrim 3UL ))

    [<Fact>]
    member _.GenDefaultLUN_002() =
        let n = ConfNode_BlockDeviceLU.GenDefaultLUN []
        Assert.True(( n = lun_me.fromPrim 1UL ))

    [<Fact>]
    member _.GenDefaultLUN_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 0UL, "aaa" ) :> ILUNode;
            new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "aaa" ) :> ILUNode;
            new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "aaa" ) :> ILUNode;
            new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim UInt64.MaxValue, "aaa" ) :> ILUNode;
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_BlockDeviceLU.GenDefaultLUN v
        Assert.True(( n = lun_me.fromPrim 3UL ))

    [<Fact>]
    member _.GenDefaultLUN_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_BlockDeviceLU( st, rel, rel.NextID, lun_me.fromPrim UInt64.MaxValue, "aaa" ) :> ILUNode;
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_BlockDeviceLU.GenDefaultLUN v
        Assert.True(( n = lun_me.fromPrim 1UL ))

    [<Fact>]
    member _.SortKey_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tgid = GlbFunc.newTargetGroupID()
        let n = new ConfNode_BlockDeviceLU( st, rel, confnode_me.fromPrim 14567UL, lun_me.fromPrim 445566UL, "aaatt" ) :> ILUNode;
        let v = n.SortKey
        Assert.True(( v.Length = 4 ))
        Assert.True(( v.[0] = ClientConst.SORT_KEY_TYPE_BlockDeviceLU ))
        Assert.True(( v.[1] = "aaatt" ))
        Assert.True(( v.[2] = ( 445566UL |> lun_me.fromPrim |> lun_me.toString ) ))
        Assert.True(( v.[3] = sprintf "%016X" 14567UL ))
    
    [<Fact>]
    member _.TempExportData_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_BlockDeviceLU( st, rel, confnode_me.fromPrim 112233UL, lun_me.fromPrim 456UL, "aakleiv" ) :> ILUNode;
        let v = n.TempExportData
        Assert.True(( v.TypeName = ClientConst.TEMPEXP_NN_BlockDeviceLU ))
        Assert.True(( v.NodeID = 112233UL ))
        Assert.True(( v.Values.Length = 2 ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "LUN" ) |> _.Value = "456" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "Name" ) |> _.Value = "aakleiv" ))

