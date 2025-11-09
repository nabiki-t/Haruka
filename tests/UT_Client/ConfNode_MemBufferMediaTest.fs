//=============================================================================
// Haruka Software Storage.
// ConfNode_MemBufferMediaTest.fs : Test cases for ConfNode_MemBufferMedia class.
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

type ConfNode_MemBufferMedia_Test() =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let defaultConf : TargetGroupConf.T_MemBuffer = {
        IdentNumber = mediaidx_me.fromPrim 1u;
        MediaName = "";
        BytesCount = 512UL;
    }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constractor_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_MemBufferMedia( st, rel, confnode_me.fromPrim 1UL, defaultConf ) :> IMediaNode
        Assert.True(( n.NodeID = confnode_me.fromPrim 1UL ))
        Assert.True(( n.NodeTypeName = "Memory Buffer Media" ))
        Assert.True(( n.MediaConfData = TargetGroupConf.T_MEDIA.U_MemBuffer( defaultConf ) ))
        Assert.True(( n.IdentNumber = mediaidx_me.fromPrim 1u ))
        Assert.True(( n.IdentNumber = mediaidx_me.fromPrim 1u ))

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
        let n = ConfNode_MemBufferMedia( st, rel, cid, te ) :> IMediaNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.IdentNumber = mediaidx_me.fromPrim 0u ))
        Assert.True(( n.Name = "" ))
        match n.MediaConfData with
        | TargetGroupConf.T_MEDIA.U_MemBuffer( x ) ->
            Assert.True(( x.BytesCount = 0UL ))
        | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.CreateUpdatedNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_MemBufferMedia( st, rel, confnode_me.fromPrim 1UL, defaultConf ) :> IMediaNode

        let confVal2 : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 2u;
            MediaName = "ggg";
            BytesCount = 1024UL;
        }

        let n2 = ( n :?> ConfNode_MemBufferMedia ).CreateUpdatedNode( confVal2 ) :> IMediaNode

        Assert.True(( n.NodeID = n2.NodeID ))
        Assert.True(( n.NodeTypeName = n2.NodeTypeName ))
        match n2.MediaConfData with
        | TargetGroupConf.T_MEDIA.U_MemBuffer( x ) ->
            Assert.True(( x.IdentNumber = mediaidx_me.fromPrim 2u ))
            Assert.True(( x.MediaName = "ggg" ))
            Assert.True(( x.BytesCount = 1024UL ))
        | _ ->
            Assert.Fail __LINE__

        let pc1 = PrivateCaller( n )
        let pc2 = PrivateCaller( n2 )
        Assert.True(( pc1.GetField( "m_MessageTable" ) = pc2.GetField( "m_MessageTable" ) ))
        Assert.True(( pc1.GetField( "m_ConfNodes" ) = pc2.GetField( "m_ConfNodes" ) ))

    [<Fact>]
    member _.Validate_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r = [] ))

    [<Fact>]
    member _.Validate_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate [ ( confnode_me.fromPrim 99UL, "msg1" ) ]
        Assert.True(( r = [ ( confnode_me.fromPrim 99UL, "msg1" ) ] ))

    [<Fact>]
    member _.Validate_IdentNumber_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let confVal = {
            defaultConf with
                IdentNumber = mediaidx_me.fromPrim 0u;
        }
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_MEDIA_ID_VALUE" ))

    [<Fact>]
    member _.Validate_BytesCount_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let mediaMaxSize = ( uint64 Array.MaxLength ) * Constants.MEDIA_BLOCK_SIZE * Constants.MEMBUFFER_BUF_LINE_BLOCK_SIZE
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let confVal = {
            defaultConf with
                BytesCount = mediaMaxSize;
        }
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))        

    [<Fact>]
    member _.Validate_BytesCount_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let mediaMaxSize = ( uint64 Array.MaxLength ) * Constants.MEDIA_BLOCK_SIZE * Constants.MEMBUFFER_BUF_LINE_BLOCK_SIZE
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let confVal = {
            defaultConf with
                BytesCount = mediaMaxSize + 1UL;
        }
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_MEDIA_SIZE" ))

    [<Fact>]
    member _.Validate_InvalidRelation_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let d1 = new DummyNode( rel.NextID, "D1" ) :> IConfigureNode
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode

        rel.AddNode lu
        rel.AddNode d1
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        rel.AddRelation n.NodeID d1.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = d1.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_RELATION" ))

    [<Fact>]
    member _.Validate_ParentCount_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_MISSING_PARENT" ))

    [<Fact>]
    member _.Validate_ParentCount_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let lu1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let lu2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "" ) :> IConfigureNode
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode lu1
        rel.AddNode lu2
        rel.AddNode n
        rel.AddRelation lu1.NodeID n.NodeID
        rel.AddRelation lu2.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_TOO_MANY_PARENT" ))

    [<Fact>]
    member _.GetChildNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetChildNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetDescendantNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetParentNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetAncestorNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_MemBufferMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
    member _.GenNewD_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_MemBufferMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 1u } ) :> IMediaNode
            new ConfNode_MemBufferMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 2u } ) :> IMediaNode
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_MemBufferMedia.GenNewID v
        Assert.True(( n = mediaidx_me.fromPrim 3u ))

    [<Fact>]
    member _.GenNewID_002() =
        let n = ConfNode_MemBufferMedia.GenNewID []
        Assert.True(( n = mediaidx_me.fromPrim 1u ))

    [<Fact>]
    member _.GenNewID_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_MemBufferMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 0u } ) :> IMediaNode
            new ConfNode_MemBufferMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 1u } ) :> IMediaNode
            new ConfNode_MemBufferMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 2u } ) :> IMediaNode
            new ConfNode_MemBufferMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim UInt32.MaxValue } ) :> IMediaNode
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_MemBufferMedia.GenNewID v
        Assert.True(( n = mediaidx_me.fromPrim 3u ))

    [<Fact>]
    member _.GenNewID_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_MemBufferMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim UInt32.MaxValue } ) :> IMediaNode
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_MemBufferMedia.GenNewID v
        Assert.True(( n = mediaidx_me.fromPrim 1u ))

    [<Fact>]
    member _.SortKey_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 2u;
            MediaName = "ggg";
            BytesCount = 1024UL;
        }
        let n = new ConfNode_MemBufferMedia( st, rel, confnode_me.fromPrim 1UL, confVal ) :> IMediaNode
        let v = n.SortKey
        Assert.True(( v.Length = 5 ))
        Assert.True(( v.[0] = ClientConst.SORT_KEY_TYPE_MemBufferMedia ))
        Assert.True(( v.[1] = "ggg" ))
        Assert.True(( v.[2] = sprintf "%016X" 1024UL ))
        Assert.True(( v.[3] = sprintf "%08X" 2u ))
        Assert.True(( v.[4] = sprintf "%016X" 1UL ))
    
    [<Fact>]
    member _.TempExportData_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal2 : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 2u;
            MediaName = "gffgg";
            BytesCount = 999UL;
        }
        let n = new ConfNode_MemBufferMedia( st, rel, confnode_me.fromPrim 1UL, confVal2 ) :> IMediaNode
        let v = n.TempExportData
        Assert.True(( v.TypeName = ClientConst.TEMPEXP_NN_MemBufferMedia ))
        Assert.True(( v.NodeID = 1UL ))
        Assert.True(( v.Values.Length = 3 ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "ID" ) |> _.Value = "2" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "MediaName" ) |> _.Value = "gffgg" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "BytesCount" ) |> _.Value = "999" ))

