//=============================================================================
// Haruka Software Storage.
// ConfNode_TargetGroupTest.fs : Test cases for ConfNode_TargetGroup class.
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

type ConfNode_TargetGroup_Test() =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let defaultConf : TargetGroupConf.T_Target = {
        IdentNumber = tnodeidx_me.fromPrim 1u;
        TargetPortalGroupTag = tpgt_me.fromPrim 0us;
        TargetName = "aaa";
        TargetAlias = "";
        LUN = [ lun_me.fromPrim 1UL ];
        Auth = TargetGroupConf.U_None();
    }

    let defTargetDeviceConf : TargetDeviceConf.T_TargetDevice = {
        NetworkPortal = [];
        NegotiableParameters = None;
        LogParameters = None;
        DeviceName = "abc";
        EnableStatSNAckChecker = false;
    }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constractor_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tgid = GlbFunc.newTargetGroupID()
        let n = new ConfNode_TargetGroup( st, rel, confnode_me.fromPrim 1UL, tgid, "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        Assert.True(( n.NodeID = confnode_me.fromPrim 1UL ))
        Assert.True(( n.NodeTypeName = "Target Group" ))
        Assert.True(( n.Modified = ModifiedStatus.NotModified ))
        let n2 = n :?> ConfNode_TargetGroup
        Assert.True(( n2.TargetGroupID = tgid ))
        Assert.True(( n2.TargetGroupName = "a" ))
        Assert.True(( n2.EnabledAtStart = true ))

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
        let n = ConfNode_TargetGroup( st, rel, cid, te )
        Assert.True(( ( n :> IConfigFileNode ) .NodeID = cid ))
        Assert.True(( ( n :> IConfigFileNode ) .Modified = ModifiedStatus.Modified ))
        Assert.True(( n.TargetGroupID = tgid_me.Zero ))
        Assert.True(( n.TargetGroupName = "" ))
        Assert.True(( n.EnabledAtStart = true ))


    [<Fact>]
    member _.Constractor_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let cid = rel.NextID
        let tgid = GlbFunc.newTargetGroupID()
        let te : TempExport.T_Node = {
            TypeName = "aaaa";  // ignored
            NodeID = 999UL;     // ignored
            Values = [
                {
                    Name = "ID";
                    Value = tgid_me.toString tgid;
                }
                {
                    Name = "Name";
                    Value = "bbb";
                }
                {
                    Name = "EnabledAtStart";
                    Value = "false";
                }
            ];
        }
        let n = ConfNode_TargetGroup( st, rel, cid, te )
        Assert.True(( ( n :> IConfigFileNode ) .NodeID = cid ))
        Assert.True(( ( n :> IConfigFileNode ) .Modified = ModifiedStatus.Modified ))
        Assert.True(( n.TargetGroupID = tgid ))
        Assert.True(( n.TargetGroupName = "bbb" ))
        Assert.True(( n.EnabledAtStart = false ))

    [<Fact>]
    member _.Validate_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode td
        rel.AddNode n
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn.NodeID
        rel.AddRelation td.NodeID n.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode td
        rel.AddNode n
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn.NodeID
        rel.AddRelation td.NodeID n.NodeID

        let r = n.Validate [ ( confnode_me.zero, "abc" ) ]
        Assert.True(( r.Length = 1 ))
        Assert.True(( ( fst r.[0] ) = confnode_me.zero ))
        Assert.True(( ( snd r.[0] ) = "abc" ))

    [<Fact>]
    member _.Validate_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let gname = ""
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), gname, true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode td
        rel.AddNode n
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn.NodeID
        rel.AddRelation td.NodeID n.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let gname = String.replicate Constants.MAX_TARGET_GROUP_NAME_STR_LENGTH "a"
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), gname, true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode td
        rel.AddNode n
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn.NodeID
        rel.AddRelation td.NodeID n.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_005() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let gname = String.replicate ( Constants.MAX_TARGET_GROUP_NAME_STR_LENGTH + 1 ) "a"
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), gname, true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode td
        rel.AddNode n
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn.NodeID
        rel.AddRelation td.NodeID n.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( ( fst r.[0] ) = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_TARGET_GROUP_NAME_TOO_LONG" ))

    [<Fact>]
    member _.Validate_006() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID
        for i = 0 to Constants.MAX_TARGET_COUNT_IN_TD - 1 do
            let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
            let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode tn
            rel.AddNode dd
            rel.AddRelation tn.NodeID dd.NodeID
            rel.AddRelation n.NodeID tn.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_007() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID

        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode dd
        for i = 0 to Constants.MAX_TARGET_COUNT_IN_TD do
            let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
            rel.AddNode tn
            rel.AddRelation tn.NodeID dd.NodeID
            rel.AddRelation n.NodeID tn.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( ( fst r.[0] ) = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_OUT_OF_TARGET_COUNT_IN_TG" ))

    [<Fact>]
    member _.Validate_008() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 2 ))
        Assert.True(( ( fst r.[0] ) = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_MISSING_LU" ))
        Assert.True(( ( fst r.[1] ) = n.NodeID ))
        Assert.True(( ( snd r.[1] ).StartsWith "CHKMSG_MISSING_TARGET" ))

    [<Fact>]
    member _.Validate_009() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID

        // target 1
        let conf1 = {
            defaultConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "a";
        }
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, conf1 ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode tn1
        rel.AddNode dd
        rel.AddRelation tn1.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn1.NodeID

        // target 2
        let conf2 = {
            defaultConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "a";
        }
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, conf2 ) :> IConfigureNode
        rel.AddNode tn2
        rel.AddRelation n.NodeID tn2.NodeID
        for i = 0 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD - 2 do
            let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode dd2
            rel.AddRelation tn2.NodeID dd2.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_010() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode td
        rel.AddNode n
        rel.AddRelation td.NodeID n.NodeID

        // target 1
        let conf1 = {
            defaultConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "a";
        }
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, conf1 ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode tn1
        rel.AddNode dd
        rel.AddRelation tn1.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn1.NodeID

        // target 2
        let conf2 = {
            defaultConf with
                IdentNumber = tnodeidx_me.fromPrim 1u;
                TargetName = "a";
        }
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, conf2 ) :> IConfigureNode
        rel.AddNode tn2
        rel.AddRelation n.NodeID tn2.NodeID
        for i = 0 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD - 1 do
            let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
            rel.AddNode dd2
            rel.AddRelation tn2.NodeID dd2.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( ( fst r.[0] ) = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_OUT_OF_LU_COUNT_IN_TG" ))

    [<Fact>]
    member _.Validate_011() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode td
        rel.AddNode n
        rel.AddNode tn
        rel.AddNode dd
        rel.AddNode dm2
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn.NodeID
        rel.AddRelation n.NodeID dm2.NodeID
        rel.AddRelation td.NodeID n.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( ( fst r.[0] ) = dm2.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_RELATION" ))

    [<Fact>]
    member _.Validate_012() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode td
        rel.AddNode n
        rel.AddNode tn
        rel.AddNode dd
        rel.AddNode dm2
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn.NodeID
        rel.AddRelation tn.NodeID dm2.NodeID
        rel.AddRelation td.NodeID n.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( ( fst r.[0] ) = dm2.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_RELATION" ))

    [<Fact>]
    member _.Validate_013() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let td1 = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let td2 = new ConfNode_TargetDevice( st, rel, rel.NextID, GlbFunc.newTargetDeviceID(), defTargetDeviceConf ) :> IConfigFileNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode td1
        rel.AddNode td2
        rel.AddNode n
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn.NodeID
        rel.AddRelation td1.NodeID n.NodeID
        rel.AddRelation td2.NodeID n.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( ( fst r.[0] ) = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_TOO_MANY_PARENT" ))

    [<Fact>]
    member _.Validate_014() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        rel.AddNode n
        rel.AddNode tn
        rel.AddNode dd
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( ( fst r.[0] ) = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_MISSING_PARENT" ))

    [<Fact>]
    member _.GetAccessibleLUNodes_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) 
        rel.AddNode n
        let rl = n.GetAccessibleLUNodes()
        Assert.True(( rl.Length = 0 ))

    [<Fact>]
    member _.GetAccessibleLUNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode tn
        rel.AddNode dd
        rel.AddNode dm
        rel.AddRelation dd.NodeID dm.NodeID
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn.NodeID

        let rl = ( n :?> ConfNode_TargetGroup ).GetAccessibleLUNodes()
        Assert.True(( rl.Length = 1 ))
        Assert.True(( rl.[0] = dd ))

    [<Fact>]
    member _.GetAccessibleLUNodes_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddNode tn2
        rel.AddNode dd2
        rel.AddNode dm2
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation dd2.NodeID dm2.NodeID
        rel.AddRelation tn2.NodeID dd2.NodeID
        rel.AddRelation n.NodeID tn1.NodeID
        rel.AddRelation n.NodeID tn2.NodeID

        let rl = ( n :?> ConfNode_TargetGroup ).GetAccessibleLUNodes()
        Assert.True(( rl.Length = 2 ))
        Assert.True(( rl = [ dd1; dd2; ] || rl = [ dd2; dd1; ] ))

    [<Fact>]
    member _.GetAccessibleLUNodes_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddNode tn2
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation tn2.NodeID dd1.NodeID
        rel.AddRelation n.NodeID tn1.NodeID
        rel.AddRelation n.NodeID tn2.NodeID

        let rl = ( n :?> ConfNode_TargetGroup ).GetAccessibleLUNodes()
        Assert.True(( rl.Length = 1 ))
        Assert.True(( rl.[0] = dd1 ))

    [<Fact>]
    member _.GetAccessibleLUNodes_005() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddNode dd2
        rel.AddNode dm2
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation dd2.NodeID dm2.NodeID
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation n.NodeID tn1.NodeID
        rel.AddRelation n.NodeID dd2.NodeID

        let rl = ( n :?> ConfNode_TargetGroup ).GetAccessibleLUNodes()
        Assert.True(( rl.Length = 1 ))
        Assert.True(( rl.[0] = dd1 ))

    [<Fact>]
    member _.GetAccessibleLUNodes_006() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation n.NodeID tn1.NodeID
        rel.AddRelation n.NodeID dd1.NodeID

        let rl = ( n :?> ConfNode_TargetGroup ).GetAccessibleLUNodes()
        Assert.True(( rl.Length = 1 ))
        Assert.True(( rl.[0] = dd1 ))

    [<Fact>]
    member _.GetIsolatedLUNodes_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) 
        rel.AddNode n
        let rl = n.GetIsolatedLUNodes()
        Assert.True(( rl.Length = 0 ))

    [<Fact>]
    member _.GetIsolatedLUNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode tn
        rel.AddNode dd
        rel.AddNode dm
        rel.AddRelation dd.NodeID dm.NodeID
        rel.AddRelation tn.NodeID dd.NodeID
        rel.AddRelation n.NodeID tn.NodeID

        let rl = ( n :?> ConfNode_TargetGroup ).GetIsolatedLUNodes()
        Assert.True(( rl.Length = 0 ))

    [<Fact>]
    member _.GetIsolatedLUNodes_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddNode tn2
        rel.AddNode dd2
        rel.AddNode dm2
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation dd2.NodeID dm2.NodeID
        rel.AddRelation tn2.NodeID dd2.NodeID
        rel.AddRelation n.NodeID tn1.NodeID
        rel.AddRelation n.NodeID tn2.NodeID

        let rl = ( n :?> ConfNode_TargetGroup ).GetIsolatedLUNodes()
        Assert.True(( rl.Length = 0 ))

    [<Fact>]
    member _.GetIsolatedLUNodes_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let tn2 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddNode tn2
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation tn2.NodeID dd1.NodeID
        rel.AddRelation n.NodeID tn1.NodeID
        rel.AddRelation n.NodeID tn2.NodeID

        let rl = ( n :?> ConfNode_TargetGroup ).GetIsolatedLUNodes()
        Assert.True(( rl.Length = 0 ))

    [<Fact>]
    member _.GetIsolatedLUNodes_005() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddNode dd2
        rel.AddNode dm2
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation dd2.NodeID dm2.NodeID
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation n.NodeID tn1.NodeID
        rel.AddRelation n.NodeID dd2.NodeID

        let rl = ( n :?> ConfNode_TargetGroup ).GetIsolatedLUNodes()
        Assert.True(( rl.Length = 1 ))
        Assert.True(( rl = [ dd2 ] ))

    [<Fact>]
    member _.GetIsolatedLUNodes_006() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation n.NodeID tn1.NodeID
        rel.AddRelation n.NodeID dd1.NodeID

        let rl = ( n :?> ConfNode_TargetGroup ).GetIsolatedLUNodes()
        Assert.True(( rl.Length = 0 ))

    [<Fact>]
    member _.CreateUpdatedNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let gname1 = "a"
        let tgid1 = GlbFunc.newTargetGroupID()
        let n1 = new ConfNode_TargetGroup( st, rel, rel.NextID, tgid1, gname1, true, ModifiedStatus.NotModified )

        let tgid2 = GlbFunc.newTargetGroupID()
        let gname2 = "b"
        let n2 = n1.CreateUpdatedNode tgid2 gname2 false

        Assert.True(( n2.TargetGroupID = tgid2 ))
        Assert.True(( n2.TargetGroupName = gname2 ))
        Assert.False(( n2.EnabledAtStart ))
        Assert.True(( ( n2 :> IConfigFileNode ).Modified = ModifiedStatus.Modified ))
        Assert.True(( ( n1 :> IConfigFileNode ).Modified = ModifiedStatus.NotModified ))
        Assert.True(( ( n2 :> IConfigFileNode ).NodeID = ( n1 :> IConfigFileNode ).NodeID ))

    [<Fact>]
    member _.GetConfigureData_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let tn1 = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> ILUNode
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode tn1
        rel.AddNode dd1
        rel.AddNode dm1
        rel.AddRelation dd1.NodeID dm1.NodeID
        rel.AddRelation tn1.NodeID dd1.NodeID
        rel.AddRelation n.NodeID tn1.NodeID

        let conf = ( n :?> ConfNode_TargetGroup ).GetConfigureData()
        Assert.True(( conf.TargetGroupID = ( n :?> ConfNode_TargetGroup ).TargetGroupID ))
        Assert.True(( conf.TargetGroupName = "a" ))
        Assert.True(( conf.EnabledAtStart = true ))
        Assert.True(( conf.Target.Length = 1 ))
        Assert.True(( conf.Target.[0] = ( tn1 :?> ConfNode_Target ).Values ))
        Assert.True(( conf.LogicalUnit.Length = 1 ))
        Assert.True(( conf.LogicalUnit.[0] = dd1.LUConfData ))

    [<Fact>]
    member _.GetConfigureData_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n

        let conf = ( n :?> ConfNode_TargetGroup ).GetConfigureData()
        Assert.True(( conf.TargetGroupID = ( n :?> ConfNode_TargetGroup ).TargetGroupID ))
        Assert.True(( conf.TargetGroupName = "a" ))
        Assert.True(( conf.EnabledAtStart = true ))
        Assert.True(( conf.Target.Length = 0 ))
        Assert.True(( conf.LogicalUnit.Length = 0 ))

    [<Fact>]
    member _.GetChildNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetChildNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
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
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
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
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
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
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetDescendantNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
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
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
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
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
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
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetParentNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
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
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
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
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
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
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        rel.AddNode n
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetAncestorNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
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
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
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
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
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
        let n = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        Assert.True(( n.Modified = ModifiedStatus.NotModified ))

        let n2 = ( n :?> ConfNode_TargetGroup ).SetModified() :> IConfigFileNode
        Assert.True(( n2.Modified = ModifiedStatus.Modified ))

        let n3 = n2.ResetModifiedFlag()
        Assert.True(( n3.Modified = ModifiedStatus.NotModified ))

    [<Fact>]
    member _.GenNewTargetGroupID_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.Zero, "a", true, ModifiedStatus.NotModified );
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.fromPrim( 1u ), "b", true, ModifiedStatus.NotModified )
        ]
        let n = ConfNode_TargetGroup.GenNewTargetGroupID v
        Assert.True(( n = tgid_me.fromPrim( 2u ) ))

    [<Fact>]
    member _.GenNewTargetGroupID_002() =
        let n = ConfNode_TargetGroup.GenNewTargetGroupID []
        Assert.True(( n = tgid_me.fromPrim( 1u ) ))

    [<Fact>]
    member _.GenNewTargetGroupID_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.Zero, "a", true, ModifiedStatus.NotModified );
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.fromPrim( 1u ), "b", true, ModifiedStatus.NotModified )
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.fromPrim( 2u ), "c", true, ModifiedStatus.NotModified )
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.fromPrim( UInt32.MaxValue ), "d", true, ModifiedStatus.NotModified )
        ]
        let n = ConfNode_TargetGroup.GenNewTargetGroupID v
        Assert.True(( n = tgid_me.fromPrim( 3u ) ))

    [<Fact>]
    member _.GenDefaultTargetGroupName_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.Zero, "TargetGroup_00000", true, ModifiedStatus.NotModified );
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.fromPrim( 1u ), "TargetGroup_00001", true, ModifiedStatus.NotModified )
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.fromPrim( 2u ), "TargetGroup_000002", true, ModifiedStatus.NotModified )
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.fromPrim( 3u ), "TargetGroup_3", true, ModifiedStatus.NotModified )
        ]
        let n = ConfNode_TargetGroup.GenDefaultTargetGroupName v
        Assert.True(( n = "TargetGroup_00004" ))

    [<Fact>]
    member _.GenDefaultTargetGroupName_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.Zero, "TargetGroup_00000", true, ModifiedStatus.NotModified );
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.fromPrim( 1u ), "aaa_00001", true, ModifiedStatus.NotModified );
        ]
        let n = ConfNode_TargetGroup.GenDefaultTargetGroupName v
        Assert.True(( n = "TargetGroup_00001" ))

    [<Fact>]
    member _.GenDefaultTargetGroupName_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.Zero, "TargetGroup_00000", true, ModifiedStatus.NotModified );
            new ConfNode_TargetGroup( st, rel, rel.NextID, tgid_me.fromPrim( 1u ), "TargetGroup_00001a", true, ModifiedStatus.NotModified );
        ]
        let n = ConfNode_TargetGroup.GenDefaultTargetGroupName v
        Assert.True(( n = "TargetGroup_00001" ))

    [<Fact>]
    member _.GenDefaultTargetGroupName_004() =
        let n = ConfNode_TargetGroup.GenDefaultTargetGroupName []
        Assert.True(( n = "TargetGroup_00000" ))

    [<Fact>]
    member _.SortKey_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tgid = GlbFunc.newTargetGroupID()
        let n = new ConfNode_TargetGroup( st, rel, confnode_me.fromPrim 14567UL, tgid, "afrgy", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let v = n.SortKey
        Assert.True(( v.Length = 4 ))
        Assert.True(( v.[0] = ClientConst.SORT_KEY_TYPE_TargetGroup ))
        Assert.True(( v.[1] = "afrgy" ))
        Assert.True(( v.[2] = tgid_me.toString tgid ))
        Assert.True(( v.[3] = sprintf "%016X" 14567UL ))
    
    [<Fact>]
    member _.TempExportData_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tgid = GlbFunc.newTargetGroupID()
        let n = new ConfNode_TargetGroup( st, rel, confnode_me.fromPrim 14567UL, tgid, "afrgy", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let v = n.TempExportData
        Assert.True(( v.TypeName = ClientConst.TEMPEXP_NN_TargetGroup ))
        Assert.True(( v.NodeID = 14567UL ))
        Assert.True(( v.Values.Length = 3 ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "ID" ) |> _.Value = tgid_me.toString tgid ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "Name" ) |> _.Value = "afrgy" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "EnabledAtStart" ) |> _.Value = "true" ))

