namespace Haruka.Test.UT.Client

open System
open System.Collections
open System.Collections.Generic

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.IODataTypes
open Haruka.Test

type DummyNode( m_NodeID : CONFNODE_T, m_TypeName : string ) =
    interface IConfigureNode with
        override _.Validate ( _ : ( CONFNODE_T * string ) list ) : ( CONFNODE_T * string ) list = []
        override _.NodeID : CONFNODE_T = m_NodeID
        override _.NodeTypeName : string = m_TypeName
        override _.GetChildNodes< 'T when 'T :> IConfigureNode >() : 'T list = []
        override _.GetDescendantNodes< 'T when 'T :> IConfigureNode >() : 'T list = []
        override _.GetParentNodes< 'T when 'T :> IConfigureNode >() : 'T list = []
        override _.GetAncestorNode< 'T when 'T :> IConfigureNode >() : 'T list = []
        override _.MinDescriptString : string = ""
        override _.ShortDescriptString : string = ""
        override _.FullDescriptString : string list = []
        override _.SortKey : string list = []
        override _.TempExportData : TempExport.T_Node = { TypeName = ""; NodeID = 0UL; Values = []; }

type DummyNode2( m_NodeID : CONFNODE_T, m_TypeName : string ) =
    interface IConfigureNode with
        override _.Validate ( _ : ( CONFNODE_T * string ) list ) : ( CONFNODE_T * string ) list = []
        override _.NodeID : CONFNODE_T = m_NodeID
        override _.NodeTypeName : string = m_TypeName
        override _.GetChildNodes< 'T when 'T :> IConfigureNode >() : 'T list = []
        override _.GetDescendantNodes< 'T when 'T :> IConfigureNode >() : 'T list = []
        override _.GetParentNodes< 'T when 'T :> IConfigureNode >() : 'T list = []
        override _.GetAncestorNode< 'T when 'T :> IConfigureNode >() : 'T list = []
        override _.MinDescriptString : string = ""
        override _.ShortDescriptString : string = ""
        override _.FullDescriptString : string list = []
        override _.SortKey : string list = []
        override _.TempExportData : TempExport.T_Node = { TypeName = ""; NodeID = 0UL; Values = []; }


type ConfNodeRelation_Test() =

    [<Fact>]
    member _.Constractor_001() =
        let n = new ConfNodeRelation()
        //let pc = PrivateCaller( n )
        Assert.True(( n.NextID = confnode_me.fromPrim 1UL ))
        Assert.True(( n.AllNodes.Count = 0 ))

    [<Fact>]
    member _.Initialize_001() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))
        Assert.True(( n.NextID = confnode_me.fromPrim 1UL ))
        Assert.True(( n.NextID = confnode_me.fromPrim 2UL ))
        n.Initialize()
        Assert.True(( n.NextID = confnode_me.fromPrim 1UL ))

    [<Fact>]
    member _.Initialize_002() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))
        Assert.True(( n.NextID = confnode_me.fromPrim 1UL ))
        n.AddNode( new DummyNode( confnode_me.fromPrim 1UL, "" ) )
        Assert.True(( n.AllNodes.Count = 1 ))
        n.Initialize()
        Assert.True(( n.NextID = confnode_me.fromPrim 1UL ))
        Assert.True(( n.AllNodes.Count = 0 ))

    [<Fact>]
    member _.Initialize_003() =
        let n = new ConfNodeRelation()

        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2

        n.AddRelation nid1 nid2

        let plist1 = n.GetParent nid2
        Assert.True( plist1.Length = 1 )
        Assert.True( plist1.[0] = nid1 )

        let plist2 = n.GetChild nid1
        Assert.True( plist2.Length = 1 )
        Assert.True( plist2.[0] = nid2 )

        Assert.True(( n.AllNodes.Count = 2 ))

        n.Initialize()

        Assert.True(( n.AllNodes.Count = 0 ))
        try
            let _ = n.GetParent nid2
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException -> ()

        try
            let _ = n.GetChild nid1
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException -> ()


    [<Fact>]
    member _.AddNode_001() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        n.AddNode node1

        Assert.True(( n.AllNodes.Count = 1 ))
        let wnodes = n.AllNodes |> Seq.toList
        Assert.True(( wnodes = [ node1 ] ))

        let node2 = new DummyNode( n.NextID, "2" )
        n.AddNode node2

        Assert.True(( n.AllNodes.Count = 2 ))
        let wnodes = n.AllNodes |> Seq.toList
        Assert.True(( wnodes = [ node1; node2 ] || wnodes = [ node2; node1 ] ))

    [<Fact>]
    member _.AddNode_002() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        n.AddNode node1

        try
            n.AddNode node1
            Assert.Fail __LINE__
        with
        | :? ArgumentException -> ()
        | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.AddNode_003() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))
        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))
        n.Delete nid1
        Assert.True(( n.AllNodes.Count = 0 ))
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

    [<Fact>]
    member _.Exists_001() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID

        Assert.False( n.Exists nid1 )
        n.AddNode node1
        Assert.True( n.Exists nid1 )

    [<Fact>]
    member _.AddRelation_001() =
        let n = new ConfNodeRelation()

        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        n.AddRelation nid1 nid2

        let plist1 = n.GetParent nid2
        Assert.True( plist1.Length = 1 )
        Assert.True( plist1.[0] = nid1 )

        let plist2 = n.GetChild nid1
        Assert.True( plist2.Length = 1 )
        Assert.True( plist2.[0] = nid2 )

        Assert.True(( n.AllNodes.Count = 2 ))

        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))
        n.AddRelation nid1 nid3

        let plist3 = n.GetParent nid3
        Assert.True( plist3.Length = 1 )
        Assert.True( plist3.[0] = nid1 )

        let plist4 = n.GetChild nid1
        Assert.True( plist4.Length = 2 )
        Assert.True( ( plist4 |> List.sort ) = [ nid2; nid3 ]  )

        let node4 = new DummyNode( n.NextID, "4" )
        let nid4 = ( node4 :> IConfigureNode ).NodeID
        n.AddNode node4
        Assert.True(( n.AllNodes.Count = 4 ))
        n.AddRelation nid4 nid3

        let plist5 = n.GetParent nid3
        Assert.True( plist5.Length = 2 )
        Assert.True( ( plist5 |> List.sort ) = [ nid1; nid4 ]  )

        let plist6 = n.GetChild nid4
        Assert.True( plist6.Length = 1 )
        Assert.True(( plist6 = [ nid3 ] ))

    [<Fact>]
    member _.AddRelation_002() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        try
            n.AddRelation ( confnode_me.fromPrim 0UL ) ( confnode_me.fromPrim 1UL )
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException ->
            ()

    [<Fact>]
    member _.AddRelation_003() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        try
            n.AddRelation nid1 ( confnode_me.fromPrim 99UL )
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException ->
            ()

    [<Fact>]
    member _.AddRelation_004() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        n.AddRelation nid1 nid1

        let plist1 = n.GetParent nid1
        Assert.True( plist1.Length = 1 )
        Assert.True( plist1.[0] = nid1 )

        let plist2 = n.GetChild nid1
        Assert.True( plist2.Length = 1 )
        Assert.True( plist2.[0] = nid1 )

    [<Fact>]
    member _.AddRelation_005() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        n.AddRelation nid1 nid2
        n.AddRelation nid1 nid2

        let plist1 = n.GetParent nid1
        Assert.True( plist1.Length = 0 )

        let plist2 = n.GetChild nid1
        Assert.True( plist2.Length = 1 )
        Assert.True( plist2.[0] = nid2 )

        let plist3 = n.GetParent nid2
        Assert.True( plist3.Length = 1 )
        Assert.True( plist3.[0] = nid1 )

        let plist4 = n.GetChild nid2
        Assert.True( plist4.Length = 0 )

    [<Fact>]
    member _.DeleteRelation_001() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))
        n.AddRelation nid1 nid2

        let plist1 = n.GetParent nid2
        Assert.True(( plist1 = [nid1] ))

        let clist1 = n.GetChild nid1
        Assert.True(( clist1 = [nid2] ))

        n.DeleteRelation nid1 nid2

        let plist2 = n.GetParent nid2
        Assert.True(( plist2 = [] ))

        let clist2 = n.GetChild nid1
        Assert.True(( clist2 = [] ))

    [<Fact>]
    member _.DeleteRelation_002() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        let plist1 = n.GetParent nid2
        Assert.True(( plist1 = [] ))

        let clist1 = n.GetChild nid1
        Assert.True(( clist1 = [] ))

        n.DeleteRelation nid1 nid2

        let plist2 = n.GetParent nid2
        Assert.True(( plist2 = [] ))

        let clist2 = n.GetChild nid1
        Assert.True(( clist2 = [] ))

    [<Fact>]
    member _.DeleteRelation_003() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))
        n.AddRelation nid1 nid2

        let plist1 = n.GetParent nid2
        Assert.True(( plist1 = [nid1] ))

        let clist1 = n.GetChild nid1
        Assert.True(( clist1 = [nid2] ))

        n.DeleteRelation nid1 nid3

        let plist1 = n.GetParent nid2
        Assert.True(( plist1 = [nid1] ))

        let clist1 = n.GetChild nid1
        Assert.True(( clist1 = [nid2] ))

    [<Fact>]
    member _.DeleteRelation_004() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))
        n.AddRelation nid1 nid2

        let plist1 = n.GetParent nid2
        Assert.True(( plist1 = [nid1] ))

        let clist1 = n.GetChild nid1
        Assert.True(( clist1 = [nid2] ))

        n.DeleteRelation nid3 nid2

        let plist1 = n.GetParent nid2
        Assert.True(( plist1 = [nid1] ))

        let clist1 = n.GetChild nid1
        Assert.True(( clist1 = [nid2] ))

    [<Fact>]
    member _.DeleteRelation_005() =
        let n = new ConfNodeRelation()
        let nid1 = n.NextID
        let nid2 = n.NextID
        Assert.True(( n.AllNodes.Count = 0 ))
        try
            n.DeleteRelation nid1 nid2
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ -> ()

    [<Fact>]
    member _.Delete_001() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))
        n.Delete ( confnode_me.fromPrim 99UL )
        Assert.True(( n.AllNodes.Count = 0 ))

    [<Fact>]
    member _.Delete_002() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        n.Delete nid1
        Assert.True(( n.AllNodes.Count = 0 ))

    [<Fact>]
    member _.Delete_003() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))

        n.AddRelation nid1 nid2
        n.AddRelation nid2 nid3
        n.Delete nid1

        try
            let _ = n.GetParent nid1
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException -> ()

        try
            let _ = n.GetChild nid1
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException -> ()

        let plist1 = n.GetParent nid2
        Assert.True( plist1.Length = 0 )

        let plist2 = n.GetChild nid2
        Assert.True( plist2.Length = 1 )
        Assert.True(( plist2 = [ nid3 ] ))

        let plist3 = n.GetParent nid3
        Assert.True( plist3.Length = 1 )
        Assert.True(( plist3 = [ nid2 ] ))

        let plist4 = n.GetChild nid3
        Assert.True( plist4.Length = 0 )

    [<Fact>]
    member _.Delete_004() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))

        n.AddRelation nid1 nid2
        n.AddRelation nid2 nid3
        n.Delete nid2

        let plist1 = n.GetParent nid1
        Assert.True( plist1.Length = 0 )

        let plist2 = n.GetChild nid1
        Assert.True( plist2.Length = 0 )

        try
            let _ = n.GetParent nid2
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException -> ()

        try
            let _ = n.GetChild nid2
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException -> ()

        let plist3 = n.GetParent nid3
        Assert.True( plist3.Length = 0 )

        let plist4 = n.GetChild nid3
        Assert.True( plist4.Length = 0 )

    [<Fact>]
    member _.Delete_005() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))

        n.AddRelation nid1 nid2
        n.AddRelation nid1 nid3
        n.Delete nid2

        let plist1 = n.GetParent nid1
        Assert.True( plist1.Length = 0 )

        let plist2 = n.GetChild nid1
        Assert.True( plist2.Length = 1 )
        Assert.True(( plist2 = [ nid3 ] ))

        try
            let _ = n.GetParent nid2
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException -> ()

        try
            let _ = n.GetChild nid2
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException -> ()

        let plist3 = n.GetParent nid3
        Assert.True( plist3.Length = 1 )
        Assert.True(( plist3 = [ nid1 ] ))

        let plist4 = n.GetChild nid3
        Assert.True( plist4.Length = 0 )

    [<Fact>]
    member _.Delete_006() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))

        n.AddRelation nid2 nid1
        n.AddRelation nid3 nid1
        n.Delete nid2

        let plist1 = n.GetParent nid1
        Assert.True( plist1.Length = 1 )
        Assert.True(( plist1 = [ nid3 ] ))

        let plist2 = n.GetChild nid1
        Assert.True( plist2.Length = 0 )

        try
            let _ = n.GetParent nid2
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException -> ()

        try
            let _ = n.GetChild nid2
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException -> ()

        let plist3 = n.GetParent nid3
        Assert.True( plist3.Length = 0 )

        let plist4 = n.GetChild nid3
        Assert.True( plist4.Length = 1 )
        Assert.True(( plist4 = [ nid1 ] ))

    [<Fact>]
    member _.Delete_007() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        n.AddRelation nid1 nid1
        n.Delete nid1
        Assert.True(( n.AllNodes.Count = 0 ))

    [<Fact>]
    member _.Update_001() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))

        n.AddRelation nid1 nid2
        n.AddRelation nid2 nid3

        let plist1 = n.GetChildNodeList<IConfigureNode> nid1
        Assert.True( plist1.Length = 1 )
        Assert.True(( plist1.[0].NodeID = nid2 ))
        Assert.True(( plist1.[0].NodeTypeName = "2" ))

        let plist2 = n.GetParentNodeList<IConfigureNode> nid3
        Assert.True( plist2.Length = 1 )
        Assert.True(( plist2.[0].NodeID = nid2 ))
        Assert.True(( plist2.[0].NodeTypeName = "2" ))

        let node4 = new DummyNode( nid2, "4" )
        n.Update node4
        Assert.True(( n.AllNodes.Count = 3 ))

        let plist3 = n.GetChildNodeList<IConfigureNode> nid1
        Assert.True( plist3.Length = 1 )
        Assert.True(( plist3.[0].NodeID = nid2 ))
        Assert.True(( plist3.[0].NodeTypeName = "4" ))

        let plist4 = n.GetParentNodeList<IConfigureNode> nid3
        Assert.True( plist4.Length = 1 )
        Assert.True(( plist4.[0].NodeID = nid2 ))
        Assert.True(( plist4.[0].NodeTypeName = "4" ))

    [<Fact>]
    member _.Update_002() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        n.Update node1
        Assert.True(( n.AllNodes.Count = 1 ))

    [<Fact>]
    member _.Update_003() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        n.AddRelation nid1 nid1

        let plist1 = n.GetChildNodeList<IConfigureNode> nid1
        Assert.True( plist1.Length = 1 )
        Assert.True(( plist1.[0].NodeID = nid1 ))
        Assert.True(( plist1.[0].NodeTypeName = "1" ))

        let plist2 = n.GetParentNodeList<IConfigureNode> nid1
        Assert.True( plist2.Length = 1 )
        Assert.True(( plist2.[0].NodeID = nid1 ))
        Assert.True(( plist2.[0].NodeTypeName = "1" ))

        let node2 = new DummyNode( nid1, "2" )
        n.Update node2
        Assert.True(( n.AllNodes.Count = 1 ))

        let plist1 = n.GetChildNodeList<IConfigureNode> nid1
        Assert.True( plist1.Length = 1 )
        Assert.True(( plist1.[0].NodeID = nid1 ))
        Assert.True(( plist1.[0].NodeTypeName = "2" ))

        let plist2 = n.GetParentNodeList<IConfigureNode> nid1
        Assert.True( plist2.Length = 1 )
        Assert.True(( plist2.[0].NodeID = nid1 ))
        Assert.True(( plist2.[0].NodeTypeName = "2" ))

    [<Fact>]
    member _.GetNode_001() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        try
            let _ = n.GetNode ( confnode_me.fromPrim 0UL )
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException ->
            ()

    [<Fact>]
    member _.GetNode_002() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let r = n.GetNode nid1
        Assert.True(( r = node1 ))
        Assert.True(( r.NodeTypeName = "1" ))
        Assert.True(( r.NodeID = nid1 ))

    [<Fact>]
    member _.GetNode_003() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        try
            let _ = n.GetNode ( confnode_me.fromPrim 99UL )
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException ->
            ()

    [<Fact>]
    member _.GetChild_001() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        try
            let _ = n.GetChild ( confnode_me.fromPrim 0UL )
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException ->
            ()

    [<Fact>]
    member _.GetChild_002() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let r = n.GetChild nid1
        Assert.True(( r = [] ))

    [<Fact>]
    member _.GetChild_003() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        n.AddRelation nid1 nid1

        let r = n.GetChild nid1
        Assert.True(( r = [ nid1 ] ))

    [<Fact>]
    member _.GetChild_004() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        n.AddRelation nid1 nid2

        let r = n.GetChild nid1
        Assert.True(( r = [ nid2 ] ))

    [<Fact>]
    member _.GetChild_005() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        n.AddRelation nid1 nid2

        let r = n.GetChild nid2
        Assert.True(( r = [] ))

    [<Fact>]
    member _.GetChild_006() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))

        n.AddRelation nid1 nid2
        n.AddRelation nid1 nid3

        let r = n.GetChild nid1
        Assert.True(( ( r |> List.sort ) = [ nid2; nid3 ] ))

    [<Fact>]
    member _.GetChildNodeList_001() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        try
            let _ = n.GetChildNodeList<IConfigureNode> ( confnode_me.fromPrim 0UL )
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException ->
            ()

    [<Fact>]
    member _.GetChildNodeList_002() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let r = n.GetChildNodeList<IConfigureNode> nid1
        Assert.True(( r = [] ))

    [<Fact>]
    member _.GetChildNodeList_003() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        n.AddRelation nid1 nid1

        let r = n.GetChildNodeList<IConfigureNode> nid1
        Assert.True(( r = [ node1 ] ))
        Assert.True(( r.[0].NodeID = nid1 ))

    [<Fact>]
    member _.GetChildNodeList_004() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        n.AddRelation nid1 nid2

        let r = n.GetChildNodeList<IConfigureNode> nid1
        Assert.True(( r = [ node2 ] ))

    [<Fact>]
    member _.GetChildNodeList_005() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        n.AddRelation nid1 nid2

        let r = n.GetChildNodeList<IConfigureNode> nid2
        Assert.True(( r = [] ))

    [<Fact>]
    member _.GetChildNodeList_006() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))

        n.AddRelation nid1 nid2
        n.AddRelation nid1 nid3

        let r = n.GetChildNodeList<IConfigureNode> nid1
        Assert.True(( r.Length = 2 ))
        if r.[0] = node2 then
            Assert.True(( r.[1] = node3 ))
        elif r.[0] = node3 then
            Assert.True(( r.[1] = node2 ))
        else
            Assert.Fail __LINE__

    [<Fact>]
    member _.GetChildNodeList_007() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode2( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))

        n.AddRelation nid1 nid2
        n.AddRelation nid1 nid3

        let r = n.GetChildNodeList<DummyNode2> nid1
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = node2 ))

    [<Fact>]
    member _.GetParent_001() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        try
            let _ = n.GetParent ( confnode_me.fromPrim 0UL )
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException ->
            ()

    [<Fact>]
    member _.GetParent_002() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let r = n.GetParent nid1
        Assert.True(( r = [] ))

    [<Fact>]
    member _.GetParent_003() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        n.AddRelation nid1 nid1

        let r = n.GetParent nid1
        Assert.True(( r = [ nid1 ] ))

    [<Fact>]
    member _.GetParent_004() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        n.AddRelation nid1 nid2

        let r = n.GetParent nid2
        Assert.True(( r = [ nid1 ] ))

    [<Fact>]
    member _.GetParent_005() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        n.AddRelation nid1 nid2

        let r = n.GetParent nid1
        Assert.True(( r = [] ))

    [<Fact>]
    member _.GetParent_006() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))

        n.AddRelation nid2 nid1
        n.AddRelation nid3 nid1

        let r = n.GetParent nid1
        Assert.True(( ( r |> List.sort ) = [ nid2; nid3 ] ))

    [<Fact>]
    member _.GetParentNodeList_001() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        try
            let _ = n.GetParentNodeList<IConfigureNode> ( confnode_me.fromPrim 0UL )
            Assert.Fail __LINE__
        with
        | :? KeyNotFoundException ->
            ()

    [<Fact>]
    member _.GetParentNodeList_002() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let r = n.GetParentNodeList<IConfigureNode> nid1
        Assert.True(( r = [] ))

    [<Fact>]
    member _.GetParentNodeList_003() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        n.AddRelation nid1 nid1

        let r = n.GetParentNodeList<IConfigureNode> nid1
        Assert.True(( r = [ node1 ] ))

    [<Fact>]
    member _.GetParentNodeList_004() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        n.AddRelation nid1 nid2

        let r = n.GetParentNodeList<IConfigureNode> nid2
        Assert.True(( r = [ node1 ] ))

    [<Fact>]
    member _.GetParentNodeList_005() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        n.AddRelation nid1 nid2

        let r = n.GetParentNodeList<IConfigureNode> nid1
        Assert.True(( r = [] ))

    [<Fact>]
    member _.GetParentNodeList_006() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))

        n.AddRelation nid2 nid1
        n.AddRelation nid3 nid1

        let r = n.GetParentNodeList<IConfigureNode> nid1
        Assert.True(( r.Length = 2 ))
        if r.[0] = node2 then
            Assert.True(( r.[1] = node3 ))
        elif r.[0] = node3 then
            Assert.True(( r.[1] = node2 ))
        else
            Assert.Fail __LINE__

    [<Fact>]
    member _.GetParentNodeList_007() =
        let n = new ConfNodeRelation()
        Assert.True(( n.AllNodes.Count = 0 ))

        let node1 = new DummyNode( n.NextID, "1" )
        let nid1 = ( node1 :> IConfigureNode ).NodeID
        n.AddNode node1
        Assert.True(( n.AllNodes.Count = 1 ))

        let node2 = new DummyNode2( n.NextID, "2" )
        let nid2 = ( node2 :> IConfigureNode ).NodeID
        n.AddNode node2
        Assert.True(( n.AllNodes.Count = 2 ))

        let node3 = new DummyNode( n.NextID, "3" )
        let nid3 = ( node3 :> IConfigureNode ).NodeID
        n.AddNode node3
        Assert.True(( n.AllNodes.Count = 3 ))

        n.AddRelation nid2 nid1
        n.AddRelation nid3 nid1

        let r = n.GetParentNodeList<DummyNode2> nid1
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = node2 ))

    [<Fact>]
    member _.GetAllChildNodeList_001() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        n.AddNode node1
        Assert.True(( n.GetAllChildNodeList<IConfigureNode>( node1.NodeID  ).Length = 0 ))

    [<Fact>]
    member _.GetAllChildNodeList_002() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddRelation node1.NodeID node2.NodeID
        let r = n.GetAllChildNodeList<IConfigureNode>( node1.NodeID )
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = node2 ))

    [<Fact>]
    member _.GetAllChildNodeList_003() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node11 = new DummyNode( n.NextID, "11" ) :> IConfigureNode
        let node111 = new DummyNode( n.NextID, "111" ) :> IConfigureNode
        let node112 = new DummyNode( n.NextID, "112" ) :> IConfigureNode
        let node113 = new DummyNode( n.NextID, "113" ) :> IConfigureNode
        let node12 = new DummyNode( n.NextID, "12" ) :> IConfigureNode
        let node121 = new DummyNode( n.NextID, "121" ) :> IConfigureNode
        let node122 = new DummyNode( n.NextID, "122" ) :> IConfigureNode
        let node123 = new DummyNode( n.NextID, "123" ) :> IConfigureNode
        let node13 = new DummyNode( n.NextID, "13" ) :> IConfigureNode
        let node131 = new DummyNode( n.NextID, "131" ) :> IConfigureNode
        let node132 = new DummyNode( n.NextID, "132" ) :> IConfigureNode
        let node133 = new DummyNode( n.NextID, "133" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node21 = new DummyNode( n.NextID, "21" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node11
        n.AddNode node111
        n.AddNode node112
        n.AddNode node113
        n.AddNode node12
        n.AddNode node121
        n.AddNode node122
        n.AddNode node123
        n.AddNode node13
        n.AddNode node131
        n.AddNode node132
        n.AddNode node133
        n.AddNode node2
        n.AddNode node21
        n.AddRelation node1.NodeID node11.NodeID
        n.AddRelation node11.NodeID node111.NodeID
        n.AddRelation node11.NodeID node112.NodeID
        n.AddRelation node11.NodeID node113.NodeID
        n.AddRelation node1.NodeID node12.NodeID
        n.AddRelation node12.NodeID node121.NodeID
        n.AddRelation node12.NodeID node122.NodeID
        n.AddRelation node12.NodeID node123.NodeID
        n.AddRelation node1.NodeID node13.NodeID
        n.AddRelation node13.NodeID node131.NodeID
        n.AddRelation node13.NodeID node132.NodeID
        n.AddRelation node13.NodeID node133.NodeID
        n.AddRelation node2.NodeID node21.NodeID
        let r = n.GetAllChildNodeList<IConfigureNode>( node1.NodeID )
        Assert.True(( r.Length = 12 ))
        [ node11; node111; node112; node113; node12; node121; node122; node123; node13; node131; node132; node133 ]
        |> List.iter ( fun itr ->
            Assert.True(( r |> List.exists ( (=) itr ) ))
        )

    [<Fact>]
    member _.GetAllChildNodeList_004() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        n.AddNode node1
        n.AddRelation node1.NodeID node1.NodeID
        let r = n.GetAllChildNodeList<IConfigureNode>( node1.NodeID )
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = node1 ))

    [<Fact>]
    member _.GetAllChildNodeList_005() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddRelation node1.NodeID node2.NodeID
        n.AddRelation node2.NodeID node2.NodeID
        let r = n.GetAllChildNodeList<IConfigureNode>( node1.NodeID )
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = node2 ))

    [<Fact>]
    member _.GetAllChildNodeList_006() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddRelation node1.NodeID node2.NodeID
        n.AddRelation node2.NodeID node3.NodeID
        n.AddRelation node3.NodeID node2.NodeID
        let r = n.GetAllChildNodeList<IConfigureNode>( node1.NodeID )
        Assert.True(( r.Length = 2 ))
        Assert.True(( r = [ node2; node3 ] || r = [ node3; node2 ] ))

    [<Fact>]
    member _.GetAllChildNodeList_007() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        let node4 = new DummyNode( n.NextID, "4" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddNode node4
        n.AddRelation node1.NodeID node2.NodeID
        n.AddRelation node2.NodeID node3.NodeID
        n.AddRelation node3.NodeID node4.NodeID
        n.AddRelation node4.NodeID node2.NodeID
        let r = n.GetAllChildNodeList<IConfigureNode>( node1.NodeID )
        Assert.True(( r.Length = 3 ))
        [ node2; node3; node4; ]
        |> List.iter ( fun itr ->
            Assert.True(( r |> List.exists ( (=) itr ) ))
        )

    [<Fact>]
    member _.GetAllChildNodeList_008() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddRelation node1.NodeID node2.NodeID
        n.AddRelation node2.NodeID node3.NodeID
        n.AddRelation node3.NodeID node1.NodeID
        let r = n.GetAllChildNodeList<IConfigureNode>( node2.NodeID )
        Assert.True(( r.Length = 3 ))
        [ node1; node2; node3; ]
        |> List.iter ( fun itr ->
            Assert.True(( r |> List.exists ( (=) itr ) ))
        )

    [<Fact>]
    member _.GetAllChildNodeList_009() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode2( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        let node4 = new DummyNode2( n.NextID, "4" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddNode node4
        n.AddRelation node1.NodeID node2.NodeID
        n.AddRelation node2.NodeID node3.NodeID
        n.AddRelation node3.NodeID node4.NodeID
        let r = n.GetAllChildNodeList<DummyNode2>( node1.NodeID )
        Assert.True(( r.Length = 2 ))
        Assert.True((
            Object.ReferenceEquals( r.[0], node2 ) && Object.ReferenceEquals( r.[1], node4 ) ||
            Object.ReferenceEquals( r.[1], node2 ) && Object.ReferenceEquals( r.[0], node4 )
        ))

    [<Fact>]
    member _.GetAllParentNodeList_001() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        n.AddNode node1
        Assert.True(( n.GetAllParentNodeList<IConfigureNode>( node1.NodeID  ).Length = 0 ))

    [<Fact>]
    member _.GetAllParentNodeList_002() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddRelation node2.NodeID node1.NodeID
        let r = n.GetAllParentNodeList<IConfigureNode>( node1.NodeID )
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = node2 ))

    [<Fact>]
    member _.GetAllParentNodeList_003() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node11 = new DummyNode( n.NextID, "11" ) :> IConfigureNode
        let node111 = new DummyNode( n.NextID, "111" ) :> IConfigureNode
        let node112 = new DummyNode( n.NextID, "112" ) :> IConfigureNode
        let node113 = new DummyNode( n.NextID, "113" ) :> IConfigureNode
        let node12 = new DummyNode( n.NextID, "12" ) :> IConfigureNode
        let node121 = new DummyNode( n.NextID, "121" ) :> IConfigureNode
        let node122 = new DummyNode( n.NextID, "122" ) :> IConfigureNode
        let node123 = new DummyNode( n.NextID, "123" ) :> IConfigureNode
        let node13 = new DummyNode( n.NextID, "13" ) :> IConfigureNode
        let node131 = new DummyNode( n.NextID, "131" ) :> IConfigureNode
        let node132 = new DummyNode( n.NextID, "132" ) :> IConfigureNode
        let node133 = new DummyNode( n.NextID, "133" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node21 = new DummyNode( n.NextID, "21" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node11
        n.AddNode node111
        n.AddNode node112
        n.AddNode node113
        n.AddNode node12
        n.AddNode node121
        n.AddNode node122
        n.AddNode node123
        n.AddNode node13
        n.AddNode node131
        n.AddNode node132
        n.AddNode node133
        n.AddNode node2
        n.AddNode node21
        n.AddRelation node11.NodeID node1.NodeID
        n.AddRelation node111.NodeID node11.NodeID
        n.AddRelation node112.NodeID node11.NodeID
        n.AddRelation node113.NodeID node11.NodeID
        n.AddRelation node12.NodeID node1.NodeID
        n.AddRelation node121.NodeID node12.NodeID
        n.AddRelation node122.NodeID node12.NodeID
        n.AddRelation node123.NodeID node12.NodeID
        n.AddRelation node13.NodeID node1.NodeID
        n.AddRelation node131.NodeID node13.NodeID
        n.AddRelation node132.NodeID node13.NodeID
        n.AddRelation node133.NodeID node13.NodeID
        n.AddRelation node21.NodeID node2.NodeID
        let r = n.GetAllParentNodeList<IConfigureNode>( node1.NodeID )
        Assert.True(( r.Length = 12 ))
        [ node11; node111; node112; node113; node12; node121; node122; node123; node13; node131; node132; node133 ]
        |> List.iter ( fun itr ->
            Assert.True(( r |> List.exists ( (=) itr ) ))
        )

    [<Fact>]
    member _.GetAllParentNodeList_004() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        n.AddNode node1
        n.AddRelation node1.NodeID node1.NodeID
        let r = n.GetAllParentNodeList<IConfigureNode>( node1.NodeID )
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = node1 ))

    [<Fact>]
    member _.GetAllParentNodeList_005() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddRelation node2.NodeID node1.NodeID
        n.AddRelation node2.NodeID node2.NodeID
        let r = n.GetAllParentNodeList<IConfigureNode>( node1.NodeID )
        Assert.True(( r.Length = 1 ))
        Assert.True(( r.[0] = node2 ))

    [<Fact>]
    member _.GetAllParentNodeList_006() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddRelation node2.NodeID node1.NodeID
        n.AddRelation node3.NodeID node2.NodeID
        n.AddRelation node2.NodeID node3.NodeID
        let r = n.GetAllParentNodeList<IConfigureNode>( node1.NodeID )
        Assert.True(( r.Length = 2 ))
        Assert.True(( r = [ node2; node3 ] || r = [ node3; node2 ] ))

    [<Fact>]
    member _.GetAllParentNodeList_007() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        let node4 = new DummyNode( n.NextID, "4" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddNode node4
        n.AddRelation node2.NodeID node1.NodeID
        n.AddRelation node3.NodeID node2.NodeID
        n.AddRelation node4.NodeID node3.NodeID
        n.AddRelation node2.NodeID node4.NodeID
        let r = n.GetAllParentNodeList<IConfigureNode>( node1.NodeID )
        Assert.True(( r.Length = 3 ))
        [ node2; node3; node4; ]
        |> List.iter ( fun itr ->
            Assert.True(( r |> List.exists ( (=) itr ) ))
        )

    [<Fact>]
    member _.GetAllParentNodeList_008() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddRelation node2.NodeID node1.NodeID
        n.AddRelation node3.NodeID node2.NodeID
        n.AddRelation node1.NodeID node3.NodeID
        let r = n.GetAllParentNodeList<IConfigureNode>( node2.NodeID )
        Assert.True(( r.Length = 3 ))
        [ node1; node2; node3; ]
        |> List.iter ( fun itr ->
            Assert.True(( r |> List.exists ( (=) itr ) ))
        )

    [<Fact>]
    member _.GetAllParentNodeList_009() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode2( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        let node4 = new DummyNode2( n.NextID, "4" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddNode node4
        n.AddRelation node2.NodeID node1.NodeID
        n.AddRelation node3.NodeID node2.NodeID
        n.AddRelation node4.NodeID node3.NodeID
        let r = n.GetAllParentNodeList<DummyNode2>( node1.NodeID )
        Assert.True(( r.Length = 2 ))
        Assert.True((
            Object.ReferenceEquals( r.[0], node2 ) && Object.ReferenceEquals( r.[1], node4 ) ||
            Object.ReferenceEquals( r.[1], node2 ) && Object.ReferenceEquals( r.[0], node4 )
        ))

    [<Fact>]
    member _.DeleteAllChildNodeList_001() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        n.AddNode node1
        n.DeleteAllChildNodeList node1.NodeID
        Assert.True(( n.AllNodes.Count = 0 ))

    [<Fact>]
    member _.DeleteAllChildNodeList_002() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddRelation node1.NodeID node2.NodeID
        n.DeleteAllChildNodeList node1.NodeID
        Assert.True(( n.AllNodes.Count = 0 ))

    [<Fact>]
    member _.DeleteAllChildNodeList_003() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node11 = new DummyNode( n.NextID, "11" ) :> IConfigureNode
        let node111 = new DummyNode( n.NextID, "111" ) :> IConfigureNode
        let node112 = new DummyNode( n.NextID, "112" ) :> IConfigureNode
        let node113 = new DummyNode( n.NextID, "113" ) :> IConfigureNode
        let node12 = new DummyNode( n.NextID, "12" ) :> IConfigureNode
        let node121 = new DummyNode( n.NextID, "121" ) :> IConfigureNode
        let node122 = new DummyNode( n.NextID, "122" ) :> IConfigureNode
        let node123 = new DummyNode( n.NextID, "123" ) :> IConfigureNode
        let node13 = new DummyNode( n.NextID, "13" ) :> IConfigureNode
        let node131 = new DummyNode( n.NextID, "131" ) :> IConfigureNode
        let node132 = new DummyNode( n.NextID, "132" ) :> IConfigureNode
        let node133 = new DummyNode( n.NextID, "133" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node21 = new DummyNode( n.NextID, "21" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node11
        n.AddNode node111
        n.AddNode node112
        n.AddNode node113
        n.AddNode node12
        n.AddNode node121
        n.AddNode node122
        n.AddNode node123
        n.AddNode node13
        n.AddNode node131
        n.AddNode node132
        n.AddNode node133
        n.AddNode node2
        n.AddNode node21
        n.AddRelation node1.NodeID node11.NodeID
        n.AddRelation node11.NodeID node111.NodeID
        n.AddRelation node11.NodeID node112.NodeID
        n.AddRelation node11.NodeID node113.NodeID
        n.AddRelation node1.NodeID node12.NodeID
        n.AddRelation node12.NodeID node121.NodeID
        n.AddRelation node12.NodeID node122.NodeID
        n.AddRelation node12.NodeID node123.NodeID
        n.AddRelation node1.NodeID node13.NodeID
        n.AddRelation node13.NodeID node131.NodeID
        n.AddRelation node13.NodeID node132.NodeID
        n.AddRelation node13.NodeID node133.NodeID
        n.AddRelation node2.NodeID node21.NodeID
        n.DeleteAllChildNodeList node1.NodeID
        Assert.True(( n.AllNodes.Count = 2 ))

        let _ = n.GetNode node2.NodeID
        Assert.True(( ( n.GetParent node2.NodeID ).Length = 0 ))
        Assert.True(( ( n.GetChild node2.NodeID ).[0] = node21.NodeID ))
        let _ = n.GetNode node21.NodeID
        Assert.True(( ( n.GetParent node21.NodeID ).[0] = node2.NodeID ))
        Assert.True(( ( n.GetChild node21.NodeID ).Length = 0 ))

    [<Fact>]
    member _.DeleteAllChildNodeList_004() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        n.AddNode node1
        n.AddRelation node1.NodeID node1.NodeID
        n.DeleteAllChildNodeList node1.NodeID
        Assert.True(( n.AllNodes.Count = 0 ))

    [<Fact>]
    member _.DeleteAllChildNodeList_005() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddRelation node1.NodeID node2.NodeID
        n.AddRelation node2.NodeID node2.NodeID
        n.DeleteAllChildNodeList node1.NodeID
        Assert.True(( n.AllNodes.Count = 1 ))

        let _ = n.GetNode node2.NodeID
        Assert.True(( ( n.GetParent node2.NodeID ).[0] = node2.NodeID ))
        Assert.True(( ( n.GetChild node2.NodeID ).[0] = node2.NodeID ))

    [<Fact>]
    member _.DeleteAllChildNodeList_006() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddRelation node1.NodeID node2.NodeID
        n.AddRelation node2.NodeID node3.NodeID
        n.AddRelation node3.NodeID node2.NodeID
        n.DeleteAllChildNodeList node1.NodeID
        Assert.True(( n.AllNodes.Count = 2 ))

        let _ = n.GetNode node2.NodeID
        Assert.True(( ( n.GetParent node2.NodeID ).[0] = node3.NodeID ))
        Assert.True(( ( n.GetChild node2.NodeID ).[0] = node3.NodeID ))
        let _ = n.GetNode node3.NodeID
        Assert.True(( ( n.GetParent node3.NodeID ).[0] = node2.NodeID ))
        Assert.True(( ( n.GetChild node3.NodeID ).[0] = node2.NodeID ))

    [<Fact>]
    member _.DeleteAllChildNodeList_007() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        let node4 = new DummyNode( n.NextID, "4" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddNode node4
        n.AddRelation node1.NodeID node2.NodeID
        n.AddRelation node2.NodeID node3.NodeID
        n.AddRelation node3.NodeID node4.NodeID
        n.AddRelation node4.NodeID node2.NodeID
        n.DeleteAllChildNodeList node1.NodeID
        Assert.True(( n.AllNodes.Count = 3 ))

        let _ = n.GetNode node2.NodeID
        Assert.True(( ( n.GetParent node2.NodeID ).[0] = node4.NodeID ))
        Assert.True(( ( n.GetChild node2.NodeID ).[0] = node3.NodeID ))
        let _ = n.GetNode node3.NodeID
        Assert.True(( ( n.GetParent node3.NodeID ).[0] = node2.NodeID ))
        Assert.True(( ( n.GetChild node3.NodeID ).[0] = node4.NodeID ))
        let _ = n.GetNode node4.NodeID
        Assert.True(( ( n.GetParent node4.NodeID ).[0] = node3.NodeID ))
        Assert.True(( ( n.GetChild node4.NodeID ).[0] = node2.NodeID ))

    [<Fact>]
    member _.DeleteAllChildNodeList_008() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddRelation node1.NodeID node2.NodeID
        n.AddRelation node2.NodeID node3.NodeID
        n.AddRelation node3.NodeID node1.NodeID
        n.DeleteAllChildNodeList node2.NodeID
        Assert.True(( n.AllNodes.Count = 0 ))

    [<Fact>]
    member _.DeleteAllChildNodeList_009() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddRelation node1.NodeID node2.NodeID
        n.AddRelation node2.NodeID node3.NodeID
        n.DeleteAllChildNodeList node2.NodeID
        Assert.True(( n.AllNodes.Count = 1 ))

        let _ = n.GetNode node1.NodeID
        Assert.True(( ( n.GetParent node1.NodeID ).Length = 0 ))
        Assert.True(( ( n.GetChild node1.NodeID ).Length = 0 ))

    [<Fact>]
    member _.DeleteAllChildNodeList_010() =
        let n = new ConfNodeRelation()
        let node1 = new DummyNode( n.NextID, "1" ) :> IConfigureNode
        let node2 = new DummyNode( n.NextID, "2" ) :> IConfigureNode
        let node3 = new DummyNode( n.NextID, "3" ) :> IConfigureNode
        let node4 = new DummyNode( n.NextID, "4" ) :> IConfigureNode
        n.AddNode node1
        n.AddNode node2
        n.AddNode node3
        n.AddNode node4
        n.AddRelation node1.NodeID node2.NodeID
        n.AddRelation node1.NodeID node3.NodeID
        n.AddRelation node2.NodeID node4.NodeID
        n.AddRelation node3.NodeID node4.NodeID
        n.DeleteAllChildNodeList node3.NodeID
        Assert.True(( n.AllNodes.Count = 3 ))

        let _ = n.GetNode node1.NodeID
        Assert.True(( ( n.GetParent node1.NodeID ).Length = 0 ))
        Assert.True(( ( n.GetChild node1.NodeID ).[0] = node2.NodeID ))
        let _ = n.GetNode node2.NodeID
        Assert.True(( ( n.GetParent node2.NodeID ).[0] = node1.NodeID ))
        Assert.True(( ( n.GetChild node2.NodeID ).[0] = node4.NodeID ))
        let _ = n.GetNode node4.NodeID
        Assert.True(( ( n.GetParent node4.NodeID ).[0] = node2.NodeID ))
        Assert.True(( ( n.GetChild node4.NodeID ).Length = 0 ))
