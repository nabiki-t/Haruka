//=============================================================================
// Haruka Software Storage.
// ConfNode_VHDXMediaTest.fs : Test cases for ConfNode_VHDXMedia class.
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

type ConfNode_VHDXMedia_Test() =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let defaultConf : TargetGroupConf.T_VHDXFile = {
        IdentNumber = mediaidx_me.fromPrim 1u;
        MediaName = "";
        FileName = "aaaa";
        WriteProtect = true;
    }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constractor_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, confnode_me.fromPrim 1UL, defaultConf ) :> IMediaNode
        Assert.StrictEqual( confnode_me.fromPrim 1UL, n.NodeID )
        Assert.StrictEqual( "VHDX Media", n.NodeTypeName )
        Assert.StrictEqual( TargetGroupConf.T_MEDIA.U_VHDXFile( defaultConf ), n.MediaConfData )
        Assert.StrictEqual( mediaidx_me.fromPrim 1u, n.IdentNumber )

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
        let n = ConfNode_VHDXMedia( st, rel, cid, te ) :> IMediaNode
        Assert.StrictEqual( cid, n.NodeID )
        Assert.StrictEqual( mediaidx_me.fromPrim 0u, n.IdentNumber )
        Assert.StrictEqual( "", n.Name )
        match n.MediaConfData with
        | TargetGroupConf.T_MEDIA.U_VHDXFile( x ) ->
            Assert.StrictEqual( "", x.FileName )
            Assert.False( x.WriteProtect )
        | _ -> Assert.Fail __LINE__

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
                    Value = "4445";
                }
                {
                    Name = "MediaName";
                    Value = "alkrio";
                }
                {
                    Name = "FileName";
                    Value = "tghuk";
                }
                {
                    Name = "WriteProtect";
                    Value = "true";
                }
            ];
        }
        let n = ConfNode_VHDXMedia( st, rel, cid, te ) :> IMediaNode
        Assert.StrictEqual( cid, n.NodeID )
        Assert.StrictEqual( mediaidx_me.fromPrim 4445u, n.IdentNumber )
        Assert.StrictEqual( "alkrio", n.Name )
        match n.MediaConfData with
        | TargetGroupConf.T_MEDIA.U_VHDXFile( x ) ->
            Assert.StrictEqual( "tghuk", x.FileName )
            Assert.True( x.WriteProtect )
        | _ -> Assert.Fail __LINE__


    [<Fact>]
    member _.CreateUpdatedNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, confnode_me.fromPrim 1UL, defaultConf ) :> IMediaNode

        let confVal2 : TargetGroupConf.T_VHDXFile = {
            IdentNumber = mediaidx_me.fromPrim 2u;
            MediaName = "ggg";
            FileName = "aaaa";
            WriteProtect = false;
        }

        let n2 = ( n :?> ConfNode_VHDXMedia ).CreateUpdatedNode( confVal2 ) :> IMediaNode

        Assert.StrictEqual( n2.NodeID, n.NodeID )
        Assert.StrictEqual( n2.NodeTypeName, n.NodeTypeName )
        match n2.MediaConfData with
        | TargetGroupConf.T_MEDIA.U_VHDXFile( x ) ->
            Assert.StrictEqual( mediaidx_me.fromPrim 2u, x.IdentNumber )
            Assert.StrictEqual( "ggg", x.MediaName )
            Assert.StrictEqual( "aaaa", x.FileName )
            Assert.False( x.WriteProtect )
        | _ ->
            Assert.Fail __LINE__

        let pc1 = PrivateCaller( n )
        let pc2 = PrivateCaller( n2 )
        Assert.StrictEqual( pc1.GetField( "m_MessageTable" ), pc2.GetField( "m_MessageTable" ) )
        Assert.StrictEqual( pc1.GetField( "m_ConfNodes" ), pc2.GetField( "m_ConfNodes" ) )

    [<Fact>]
    member _.Validate_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.Empty( r )

    [<Fact>]
    member _.Validate_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate [ ( confnode_me.fromPrim 99UL, "msg1" ) ]
        Assert.StrictEqual( 1, r.Length )
        Assert.StrictEqual( ( confnode_me.fromPrim 99UL, "msg1" ), r.[0] )

    [<Fact>]
    member _.Validate_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal : TargetGroupConf.T_VHDXFile = {
            IdentNumber = mediaidx_me.fromPrim 1u;
            MediaName = "ggg";
            FileName = "";
            WriteProtect = true;
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> IConfigureNode
        let d1 = new DummyNode( rel.NextID, "D1" ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode d1
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        rel.AddRelation n.NodeID d1.NodeID
        let r = n.Validate []
        Assert.StrictEqual( 2, r.Length )

    [<Fact>]
    member _.Validate_IdentNumber_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> IConfigureNode
        let confVal = {
            defaultConf with
                IdentNumber = mediaidx_me.fromPrim 0u;
        }
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.StrictEqual( 1, r.Length )
        Assert.StrictEqual( n.NodeID, fst r.[0] )
        Assert.StartsWith( "CHKMSG_INVALID_MEDIA_ID_VALUE", ( snd r.[0] ) )

    [<Fact>]
    member _.Validate_FileName_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                FileName = "";
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.StrictEqual( 1, r.Length )
        Assert.StrictEqual( n.NodeID, fst r.[0] )
        Assert.StartsWith( "CHKMSG_INVALID_FILE_NAME_LENGTH", ( snd r.[0] ) )

    [<Fact>]
    member _.Validate_FileName_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                FileName = "a";
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.Empty( r )

    [<Fact>]
    member _.Validate_FileName_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                FileName = String.replicate Constants.MAX_FILENAME_STR_LENGTH "a";
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.Empty( r )

    [<Fact>]
    member _.Validate_FileName_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                FileName = String.replicate ( Constants.MAX_FILENAME_STR_LENGTH + 1 ) "a";
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.StrictEqual( 1, r.Length )
        Assert.StrictEqual( n.NodeID, fst r.[0] )
        Assert.StartsWith( "CHKMSG_INVALID_FILE_NAME_LENGTH", ( snd r.[0] ) )

    [<Fact>]
    member _.Validate_InvalidRelation_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let d1 = new DummyNode( rel.NextID, "D1" ) :> IConfigureNode
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode

        rel.AddNode lu
        rel.AddNode d1
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        rel.AddRelation n.NodeID d1.NodeID

        let r = n.Validate []
        Assert.StrictEqual( 1, r.Length )
        Assert.StrictEqual( d1.NodeID, fst r.[0] )
        Assert.StartsWith( "CHKMSG_INVALID_RELATION", ( snd r.[0] ) )

    [<Fact>]
    member _.Validate_ParentCount_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.Validate []
        Assert.StrictEqual( 1, r.Length )
        Assert.StrictEqual( n.NodeID, fst r.[0] )
        Assert.StartsWith( "CHKMSG_MISSING_PARENT", ( snd r.[0] ) )

    [<Fact>]
    member _.Validate_ParentCount_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let lu1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "", Constants.LU_DEF_MULTIPLICITY ) :> IConfigureNode
        let lu2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 2UL, "", Constants.LU_DEF_MULTIPLICITY ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode lu1
        rel.AddNode lu2
        rel.AddNode n
        rel.AddRelation lu1.NodeID n.NodeID
        rel.AddRelation lu2.NodeID n.NodeID
        let r = n.Validate []
        Assert.StrictEqual( 1, r.Length )
        Assert.StrictEqual( n.NodeID, fst r.[0] )
        Assert.StartsWith( "CHKMSG_TOO_MANY_PARENT", ( snd r.[0] ) )

    [<Fact>]
    member _.GetChildNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.Empty( r )

    [<Fact>]
    member _.GetChildNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm
        rel.AddRelation n.NodeID dm.NodeID
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.StrictEqual( 1, r.Length )
        Assert.True( Functions.IsSame dm r.[0] )
        
    [<Fact>]
    member _.GetChildNode_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation n.NodeID dm2.NodeID
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.StrictEqual( 2, r.Length )
        Assert.True(( r = [ dm1; dm2 ] || r = [ dm2; dm1 ] ))
        
    [<Fact>]
    member _.GetChildNode_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        Assert.StrictEqual( 1, r.Length )
        Assert.True( Functions.IsSame dm3 r.[0] )

    [<Fact>]
    member _.GetDescendantNodes_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.Empty( r )

    [<Fact>]
    member _.GetDescendantNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm
        rel.AddRelation n.NodeID dm.NodeID
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.StrictEqual( 1, r.Length )
        Assert.True( Functions.IsSame dm r.[0] )
        
    [<Fact>]
    member _.GetDescendantNodes_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        rel.AddNode n
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation dm1.NodeID dm2.NodeID
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.StrictEqual( 2, r.Length )
        Assert.True(( r = [ dm1; dm2 ] || r = [ dm2; dm1 ] ))
        
    [<Fact>]
    member _.GetDescendantNodes_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        Assert.StrictEqual( 1, r.Length )
        Assert.True(( Functions.IsSame dm2 r.[0] ))

    [<Fact>]
    member _.GetParentNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.Empty( r )

    [<Fact>]
    member _.GetParentNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode dm
        rel.AddNode n
        rel.AddRelation dm.NodeID n.NodeID
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.StrictEqual( 1, r.Length )
        Assert.True( Functions.IsSame dm r.[0] )
        
    [<Fact>]
    member _.GetParentNode_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode n
        rel.AddRelation dm1.NodeID n.NodeID
        rel.AddRelation dm2.NodeID n.NodeID
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.StrictEqual( 2, r.Length )
        Assert.True(( r = [ dm1; dm2 ] || r = [ dm2; dm1 ] ))
        
    [<Fact>]
    member _.GetParentNode_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm3 = new DummyNode2( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode dm3
        rel.AddNode n
        rel.AddRelation dm1.NodeID n.NodeID
        rel.AddRelation dm2.NodeID n.NodeID
        rel.AddRelation dm3.NodeID n.NodeID
        let r = n.GetParentNodes<DummyNode2>()
        Assert.StrictEqual( 1, r.Length )
        Assert.True(( Functions.IsSame dm3 r.[0] ))

    [<Fact>]
    member _.GetAncestorNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.Empty( r )

    [<Fact>]
    member _.GetAncestorNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode dm
        rel.AddNode n
        rel.AddRelation dm.NodeID n.NodeID
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.StrictEqual( 1, r.Length )
        Assert.True( Functions.IsSame dm r.[0] )
        
    [<Fact>]
    member _.GetAncestorNode_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode n
        rel.AddRelation dm1.NodeID n.NodeID
        rel.AddRelation dm2.NodeID dm1.NodeID
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.StrictEqual( 2, r.Length )
        Assert.True(( r = [ dm1; dm2 ] || r = [ dm2; dm1 ] ))
        
    [<Fact>]
    member _.GetAncestorNode_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm1 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let dm2 = new DummyNode2( rel.NextID, "" ) :> IConfigureNode
        let dm3 = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_VHDXMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode dm3
        rel.AddNode n
        rel.AddRelation dm1.NodeID n.NodeID
        rel.AddRelation dm2.NodeID dm1.NodeID
        rel.AddRelation dm3.NodeID dm2.NodeID
        let r = n.GetAncestorNode<DummyNode2>()
        Assert.StrictEqual( 1, r.Length )
        Assert.True(( Object.ReferenceEquals( r.[0], dm2 ) ))

    [<Fact>]
    member _.GenNewD_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_VHDXMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 1u } ) :> IMediaNode
            new ConfNode_VHDXMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 2u } ) :> IMediaNode
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_VHDXMedia.GenNewID v
        Assert.StrictEqual( mediaidx_me.fromPrim 3u, n )

    [<Fact>]
    member _.GenNewID_002() =
        let n = ConfNode_VHDXMedia.GenNewID []
        Assert.StrictEqual( mediaidx_me.fromPrim 1u, n )

    [<Fact>]
    member _.GenNewID_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_VHDXMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 0u } ) :> IMediaNode
            new ConfNode_VHDXMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 1u } ) :> IMediaNode
            new ConfNode_VHDXMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 2u } ) :> IMediaNode
            new ConfNode_VHDXMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim UInt32.MaxValue } ) :> IMediaNode
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_VHDXMedia.GenNewID v
        Assert.StrictEqual( mediaidx_me.fromPrim 3u, n )

    [<Fact>]
    member _.GenNewID_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_VHDXMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim UInt32.MaxValue } ) :> IMediaNode
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_VHDXMedia.GenNewID v
        Assert.StrictEqual( mediaidx_me.fromPrim 1u, n )

    [<Fact>]
    member _.SortKey_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal2 : TargetGroupConf.T_VHDXFile = {
            IdentNumber = mediaidx_me.fromPrim 2u;
            MediaName = "gffgg";
            FileName = "aaaa";
            WriteProtect = false;
        }
        let n = new ConfNode_VHDXMedia( st, rel, confnode_me.fromPrim 1UL, confVal2 ) :> IMediaNode
        let v = n.SortKey
        Assert.StrictEqual( 5, v.Length )
        Assert.StrictEqual( ClientConst.SORT_KEY_TYPE_VHDXMedia, v.[0] )
        Assert.StrictEqual( "gffgg", v.[1] )
        Assert.StrictEqual( "aaaa", v.[2] )
        Assert.StrictEqual( sprintf "%08X" 2u, v.[3] )
        Assert.StrictEqual( sprintf "%016X" 1UL, v.[4] )
    
    [<Fact>]
    member _.TempExportData_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal2 : TargetGroupConf.T_VHDXFile = {
            IdentNumber = mediaidx_me.fromPrim 2u;
            MediaName = "gffgg";
            FileName = "aaaa";
            WriteProtect = true;
        }
        let n = new ConfNode_VHDXMedia( st, rel, confnode_me.fromPrim 1UL, confVal2 ) :> IMediaNode
        let v = n.TempExportData
        Assert.StrictEqual( ClientConst.TEMPEXP_NN_VHDXMedia, v.TypeName )
        Assert.StrictEqual( 1UL, v.NodeID )
        Assert.StrictEqual( 4, v.Values.Length )
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "ID" ) |> _.Value = "2" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "MediaName" ) |> _.Value = "gffgg" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "FileName" ) |> _.Value = "aaaa" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "WriteProtect" ) |> _.Value = "true" ))


