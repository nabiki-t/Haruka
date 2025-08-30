﻿namespace Haruka.Test.UT.Client

open System
open System.Collections
open System.Collections.Generic

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.IODataTypes
open Haruka.Test

type ConfNode_DebugMedia_Test() =

    [<Fact>]
    member _.Constractor_001() =
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( new StringTable( "" ), rel, confnode_me.fromPrim 1UL, mediaidx_me.fromPrim 1u, "aaa" )
        rel.AddNode n
        Assert.True(( ( n :> IMediaNode ).NodeID = confnode_me.fromPrim 1UL ))
        Assert.True(( ( n :> IMediaNode ).NodeTypeName = "Debug Media" ))
        try
            let _ = ( n :> IMediaNode ).MediaConfData
            Assert.Fail __LINE__
        with
        | :? ConfigurationError ->
            ()
        
        Assert.True(( ( n :> IMediaNode ).IdentNumber = mediaidx_me.fromPrim 1u ))

    [<Fact>]
    member _.Constractor_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "aaa" )
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 3u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddRelation ( n :> IMediaNode ).NodeID dm1.NodeID
        rel.AddRelation ( n :> IMediaNode ).NodeID dm2.NodeID

        try
            let _ = ( n :> IMediaNode ).MediaConfData
            Assert.Fail __LINE__
        with
        | :? ConfigurationError ->
            ()
        
    [<Fact>]
    member _.Constractor_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "aaa" )
        let dlu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.zero, "" ) :> ILUNode
        rel.AddNode n
        rel.AddNode dlu
        rel.AddRelation ( n :> IMediaNode ).NodeID dlu.NodeID

        try
            let _ = ( n :> IMediaNode ).MediaConfData
            Assert.Fail __LINE__
        with
        | :? ConfigurationError ->
            ()

    [<Fact>]
    member _.Constractor_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "aaa" )
        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation ( n :> IMediaNode ).NodeID dm1.NodeID

        match ( n :> IMediaNode ).MediaConfData with
        | TargetGroupConf.T_MEDIA.U_DebugMedia( y ) ->
            Assert.True(( y.IdentNumber = mediaidx_me.fromPrim 1u ))
            Assert.True(( y.MediaName = "aaa" ))
        | _ ->
            Assert.Fail __LINE__


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
        let n = ConfNode_DebugMedia( st, rel, cid, te ) :> IMediaNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.IdentNumber = mediaidx_me.zero ))
        Assert.True(( n.Name = "" ))

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
                    Name = "ID";
                    Value = "4455";
                }
                {
                    Name = "MediaName";
                    Value = "bbb";
                }
            ];
        }
        let n = ConfNode_DebugMedia( st, rel, cid, te ) :> IMediaNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.IdentNumber = mediaidx_me.fromPrim 4455u ))
        Assert.True(( n.Name = "bbb" ))

    [<Fact>]
    member _.CreateUpdatedNode_001() =
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( new StringTable( "" ), rel, confnode_me.fromPrim 1UL, mediaidx_me.fromPrim 1u, "aaa" )
        let n2 = n.CreateUpdatedNode ( mediaidx_me.fromPrim 2u ) "bbb"
        Assert.True(( ( n :> IMediaNode ).NodeID = ( n2 :> IMediaNode ).NodeID ))
        Assert.True(( ( n :> IMediaNode ).NodeTypeName = ( n2 :> IMediaNode ).NodeTypeName ))
        Assert.True(( ( n2 :> IMediaNode ).IdentNumber = mediaidx_me.fromPrim 2u ))
        Assert.True(( ( n2 :> IMediaNode ).Name = "bbb" ))

        let pc1 = PrivateCaller( n )
        let pc2 = PrivateCaller( n2 )
        Assert.True(( Object.ReferenceEquals( pc1.GetField( "m_MessageTable" ), pc2.GetField( "m_MessageTable" ) ) ))
        Assert.True(( Object.ReferenceEquals( pc1.GetField( "m_ConfNodes" ), pc2.GetField( "m_ConfNodes" ) ) ))

    [<Fact>]
    member _.Validate_001() =
        let rel = new ConfNodeRelation()
        let msgtbl = new StringTable( "" )
        let lu = new ConfNode_DummyDeviceLU( msgtbl, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_DebugMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm1 = new ConfNode_DummyMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation lu.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_002() =
        let rel = new ConfNodeRelation()
        let msgtbl = new StringTable( "" )
        let lu = new ConfNode_DummyDeviceLU( msgtbl, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_DebugMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm1 = new ConfNode_DummyMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation lu.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        let r = n.Validate [ ( confnode_me.fromPrim 99UL, "msg1" ) ]
        Assert.True(( r = [ ( confnode_me.fromPrim 99UL, "msg1" ) ] ))

    [<Fact>]
    member _.Validate_003() =
        let rel = new ConfNodeRelation()
        let msgtbl = new StringTable( "" )
        let lu = new ConfNode_DummyDeviceLU( msgtbl, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_DebugMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 0u, "" ) :> IMediaNode
        let dm1 = new ConfNode_DummyMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation lu.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_MEDIA_ID_VALUE" ))

    [<Fact>]
    member _.Validate_004() =
        let rel = new ConfNodeRelation()
        let msgtbl = new StringTable( "" )
        let lu = new ConfNode_DummyDeviceLU( msgtbl, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_DebugMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode

        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_TOO_FEW_CHILD" ))

    [<Fact>]
    member _.Validate_005() =
        let rel = new ConfNodeRelation()
        let msgtbl = new StringTable( "" )
        let lu = new ConfNode_DummyDeviceLU( msgtbl, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_DebugMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm1 = new ConfNode_DummyMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        let dm2 = new ConfNode_DummyMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 3u, "" ) :> IMediaNode

        rel.AddNode lu
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddRelation lu.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation n.NodeID dm2.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_TOO_MANY_CHILD" ))

    [<Fact>]
    member _.Validate_006() =
        let rel = new ConfNodeRelation()
        let msgtbl = new StringTable( "" )
        let lu = new ConfNode_DummyDeviceLU( msgtbl, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_DebugMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let d1 = new ConfNode_DummyDeviceLU( msgtbl, rel, rel.NextID, lun_me.fromPrim 2UL, "" ) :> IConfigureNode

        rel.AddNode lu
        rel.AddNode n
        rel.AddNode d1
        rel.AddRelation lu.NodeID n.NodeID
        rel.AddRelation n.NodeID d1.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_RELATION" ))

    [<Fact>]
    member _.Validate_007() =
        let rel = new ConfNodeRelation()
        let msgtbl = new StringTable( "" )
        let lu1 = new ConfNode_DummyDeviceLU( msgtbl, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let lu2 = new ConfNode_DummyDeviceLU( msgtbl, rel, rel.NextID, lun_me.fromPrim 2UL, "" ) :> IConfigureNode
        let n = new ConfNode_DebugMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm1 = new ConfNode_DummyMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode lu1
        rel.AddNode lu2
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation lu1.NodeID n.NodeID
        rel.AddRelation lu2.NodeID n.NodeID
        rel.AddRelation n.NodeID dm1.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_TOO_MANY_PARENT" ))

    [<Fact>]
    member _.Validate_008() =
        let rel = new ConfNodeRelation()
        let msgtbl = new StringTable( "" )
        let n = new ConfNode_DebugMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm1 = new ConfNode_DummyMedia( msgtbl, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        rel.AddNode n
        rel.AddNode dm1
        rel.AddRelation n.NodeID dm1.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_MISSING_PARENT" ))

    [<Fact>]
    member _.GetChildNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode n
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetChildNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm
        rel.AddRelation n.NodeID dm.NodeID
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( Object.ReferenceEquals( r.[0], dm ) ))
        
    [<Fact>]
    member _.GetChildNode_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation n.NodeID dm2.NodeID
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 2 ))
        let wr =
            ( Object.ReferenceEquals( r.[0], dm1 ) && Object.ReferenceEquals( r.[1], dm2 ) ) ||
            ( Object.ReferenceEquals( r.[0], dm2 ) && Object.ReferenceEquals( r.[1], dm1 ) )
        Assert.True wr
        
    [<Fact>]
    member _.GetChildNode_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
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
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode n
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetDescendantNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
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
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation dm1.NodeID dm2.NodeID
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.True(( r.Length = 2 ))
        let wr =
            ( Object.ReferenceEquals( r.[0], dm1 ) && Object.ReferenceEquals( r.[1], dm2 ) ) ||
            ( Object.ReferenceEquals( r.[0], dm2 ) && Object.ReferenceEquals( r.[1], dm1 ) )
        Assert.True wr
        
    [<Fact>]
    member _.GetDescendantNodes_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
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
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode n
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetParentNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
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
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
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
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
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
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode n
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetAncestorNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode dm
        rel.AddNode n
        rel.AddRelation dm.NodeID n.NodeID
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 1 ))
        Assert.True(( Object.ReferenceEquals( r.[0], dm ) ))
        
    [<Fact>]
    member _.GetAncestorNode_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode n
        rel.AddRelation dm1.NodeID n.NodeID
        rel.AddRelation dm2.NodeID dm1.NodeID
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 2 ))
        let wr =
            ( Object.ReferenceEquals( r.[0], dm1 ) && Object.ReferenceEquals( r.[1], dm2 ) ) ||
            ( Object.ReferenceEquals( r.[0], dm2 ) && Object.ReferenceEquals( r.[1], dm1 ) )
        Assert.True wr

        
    [<Fact>]
    member _.GetAncestorNode_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode2( rel.NextID, "" ) :> IConfigureNode
        let dm3 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
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
            new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
            new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_DebugMedia.GenNewID v
        Assert.True(( n = mediaidx_me.fromPrim 3u ))

    [<Fact>]
    member _.GenNewID_002() =
        let n = ConfNode_DebugMedia.GenNewID []
        Assert.True(( n = mediaidx_me.fromPrim 1u ))

    [<Fact>]
    member _.GenNewID_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 0u, "" ) :> IMediaNode
            new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
            new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
            new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim UInt32.MaxValue, "" ) :> IMediaNode
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_DebugMedia.GenNewID v
        Assert.True(( n = mediaidx_me.fromPrim 3u ))

    [<Fact>]
    member _.GenNewID_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_DebugMedia( st, rel, rel.NextID, mediaidx_me.fromPrim UInt32.MaxValue, "" ) :> IMediaNode
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_DebugMedia.GenNewID v
        Assert.True(( n = mediaidx_me.fromPrim 1u ))

    [<Fact>]
    member _.SortKey_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( st, rel, confnode_me.fromPrim 99UL, mediaidx_me.fromPrim 1u, "aassdd" ) :> IMediaNode
        let v = n.SortKey
        Assert.True(( v.Length = 4 ))
        Assert.True(( v.[0] = ClientConst.SORT_KEY_TYPE_DebugMedia ))
        Assert.True(( v.[1] = "aassdd" ))
        Assert.True(( v.[2] = sprintf "%08X" 1u ))
        Assert.True(( v.[3] = sprintf "%016X" 99UL ))
    
    [<Fact>]
    member _.TempExportData_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_DebugMedia( st, rel, confnode_me.fromPrim 99UL, mediaidx_me.fromPrim 1u, "aassdd" ) :> IMediaNode
        let v = n.TempExportData
        Assert.True(( v.TypeName = ClientConst.TEMPEXP_NN_DebugMedia ))
        Assert.True(( v.NodeID = 99UL ))
        Assert.True(( v.Values.Length = 2 ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "ID" ) |> _.Value = "1" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "MediaName" ) |> _.Value = "aassdd" ))

