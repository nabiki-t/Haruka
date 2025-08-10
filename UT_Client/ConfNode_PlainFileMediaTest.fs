namespace Haruka.Test.UT.Client

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Client
open Haruka.IODataTypes
open Haruka.Test

type ConfNode_PlainFileMedia_Test() =

    let defaultConf : TargetGroupConf.T_PlainFile = {
        IdentNumber = mediaidx_me.fromPrim 1u;
        MediaName = "";
        FileName = "aaaa";
        MaxMultiplicity = Constants.PLAINFILE_MIN_MAXMULTIPLICITY;
        QueueWaitTimeOut = Constants.PLAINFILE_MIN_QUEUEWAITTIMEOUT;
        WriteProtect = true;
    }

    [<Fact>]
    member _.Constractor_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_PlainFileMedia( st, rel, confnode_me.fromPrim 1UL, defaultConf ) :> IMediaNode
        Assert.True(( n.NodeID = confnode_me.fromPrim 1UL ))
        Assert.True(( n.NodeTypeName = "Plain File Media" ))
        Assert.True(( n.MediaConfData = TargetGroupConf.T_MEDIA.U_PlainFile( defaultConf ) ))
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
        let n = ConfNode_PlainFileMedia( st, rel, cid, te ) :> IMediaNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.IdentNumber = mediaidx_me.fromPrim 0u ))
        Assert.True(( n.Name = "" ))
        match n.MediaConfData with
        | TargetGroupConf.T_MEDIA.U_PlainFile( x ) ->
            Assert.True(( x.FileName = "" ))
            Assert.True(( x.MaxMultiplicity = 0u ))
            Assert.True(( x.QueueWaitTimeOut = 0 ))
            Assert.True(( x.WriteProtect = false ))
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
                    Name = "MaxMultiplicity";
                    Value = "988";
                }
                {
                    Name = "QueueWaitTimeOut";
                    Value = "75";
                }
                {
                    Name = "WriteProtect";
                    Value = "true";
                }
            ];
        }
        let n = ConfNode_PlainFileMedia( st, rel, cid, te ) :> IMediaNode
        Assert.True(( n.NodeID = cid ))
        Assert.True(( n.IdentNumber = mediaidx_me.fromPrim 4445u ))
        Assert.True(( n.Name = "alkrio" ))
        match n.MediaConfData with
        | TargetGroupConf.T_MEDIA.U_PlainFile( x ) ->
            Assert.True(( x.FileName = "tghuk" ))
            Assert.True(( x.MaxMultiplicity = 988u ))
            Assert.True(( x.QueueWaitTimeOut = 75 ))
            Assert.True(( x.WriteProtect = true ))
        | _ -> Assert.Fail __LINE__


    [<Fact>]
    member _.CreateUpdatedNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_PlainFileMedia( st, rel, confnode_me.fromPrim 1UL, defaultConf ) :> IMediaNode

        let confVal2 : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 2u;
            MediaName = "ggg";
            FileName = "aaaa";
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 2;
            WriteProtect = false;
        }

        let n2 = ( n :?> ConfNode_PlainFileMedia ).CreateUpdatedNode( confVal2 ) :> IMediaNode

        Assert.True(( n.NodeID = n2.NodeID ))
        Assert.True(( n.NodeTypeName = n2.NodeTypeName ))
        match n2.MediaConfData with
        | TargetGroupConf.T_MEDIA.U_PlainFile( x ) ->
            Assert.True(( x.IdentNumber = mediaidx_me.fromPrim 2u ))
            Assert.True(( x.MediaName = "ggg" ))
            Assert.True(( x.FileName = "aaaa" ))
            Assert.True(( x.MaxMultiplicity = 1u ))
            Assert.True(( x.QueueWaitTimeOut = 2 ))
            Assert.True(( x.WriteProtect = false ))
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate [ ( confnode_me.fromPrim 99UL, "msg1" ) ]
        Assert.True(( r = [ ( confnode_me.fromPrim 99UL, "msg1" ) ] ))

    [<Fact>]
    member _.Validate_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 1u;
            MediaName = "ggg";
            FileName = "";
            MaxMultiplicity = Constants.PLAINFILE_MIN_MAXMULTIPLICITY - 1u;
            QueueWaitTimeOut = Constants.PLAINFILE_MIN_QUEUEWAITTIMEOUT - 1;
            WriteProtect = true;
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let d1 = new DummyNode( rel.NextID, "D1" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode d1
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        rel.AddRelation n.NodeID d1.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 4 ))

    [<Fact>]
    member _.Validate_IdentNumber_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let confVal = {
            defaultConf with
                IdentNumber = mediaidx_me.fromPrim 0u;
        }
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_MEDIA_ID_VALUE" ))

    [<Fact>]
    member _.Validate_FileName_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                FileName = "";
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_FILE_NAME_LENGTH" ))

    [<Fact>]
    member _.Validate_FileName_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                FileName = "a";
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_FileName_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                FileName = String.replicate Constants.MAX_FILENAME_STR_LENGTH "a";
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_FileName_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                FileName = String.replicate ( Constants.MAX_FILENAME_STR_LENGTH + 1 ) "a";
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_FILE_NAME_LENGTH" ))

    [<Fact>]
    member _.Validate_MaxMultiplicity_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                MaxMultiplicity = Constants.PLAINFILE_MIN_MAXMULTIPLICITY - 1u;
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_MAXMULTIPLICITY" ))

    [<Fact>]
    member _.Validate_MaxMultiplicity_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                MaxMultiplicity = Constants.PLAINFILE_MIN_MAXMULTIPLICITY;
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_MaxMultiplicity_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                MaxMultiplicity = Constants.PLAINFILE_MAX_MAXMULTIPLICITY;
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_MaxMultiplicity_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                MaxMultiplicity = Constants.PLAINFILE_MAX_MAXMULTIPLICITY + 1u;
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_MAXMULTIPLICITY" ))

    [<Fact>]
    member _.Validate_QueueWaitTimeOut_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                QueueWaitTimeOut = Constants.PLAINFILE_MIN_QUEUEWAITTIMEOUT - 1;
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_QUEUEWAITTIMEOUT" ))

    [<Fact>]
    member _.Validate_QueueWaitTimeOut_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                QueueWaitTimeOut = Constants.PLAINFILE_MIN_QUEUEWAITTIMEOUT;
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_QueueWaitTimeOut_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                QueueWaitTimeOut = Constants.PLAINFILE_MAX_QUEUEWAITTIMEOUT;
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_QueueWaitTimeOut_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal = {
            defaultConf with
                QueueWaitTimeOut = Constants.PLAINFILE_MAX_QUEUEWAITTIMEOUT + 1;
        }
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, confVal ) :> IMediaNode
        rel.AddNode lu
        rel.AddNode n
        rel.AddRelation lu.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_QUEUEWAITTIMEOUT" ))

    [<Fact>]
    member _.Validate_InvalidRelation_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let d1 = new DummyNode( rel.NextID, "D1" ) :> IConfigureNode
        let lu = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode

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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetChildNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetDescendantNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetParentNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
        rel.AddNode n
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetAncestorNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
        let n = new ConfNode_PlainFileMedia( st, rel, rel.NextID, defaultConf ) :> IMediaNode
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
            new ConfNode_PlainFileMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 1u } ) :> IMediaNode
            new ConfNode_PlainFileMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 2u } ) :> IMediaNode
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_PlainFileMedia.GenNewID v
        Assert.True(( n = mediaidx_me.fromPrim 3u ))

    [<Fact>]
    member _.GenNewID_002() =
        let n = ConfNode_PlainFileMedia.GenNewID []
        Assert.True(( n = mediaidx_me.fromPrim 1u ))

    [<Fact>]
    member _.GenNewID_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_PlainFileMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 0u } ) :> IMediaNode
            new ConfNode_PlainFileMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 1u } ) :> IMediaNode
            new ConfNode_PlainFileMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim 2u } ) :> IMediaNode
            new ConfNode_PlainFileMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim UInt32.MaxValue } ) :> IMediaNode
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_PlainFileMedia.GenNewID v
        Assert.True(( n = mediaidx_me.fromPrim 3u ))

    [<Fact>]
    member _.GenNewID_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_PlainFileMedia( st, rel, rel.NextID, { defaultConf with IdentNumber = mediaidx_me.fromPrim UInt32.MaxValue } ) :> IMediaNode
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_PlainFileMedia.GenNewID v
        Assert.True(( n = mediaidx_me.fromPrim 1u ))

    [<Fact>]
    member _.SortKey_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal2 : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 2u;
            MediaName = "gffgg";
            FileName = "aaaa";
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 2;
            WriteProtect = false;
        }
        let n = new ConfNode_PlainFileMedia( st, rel, confnode_me.fromPrim 1UL, confVal2 ) :> IMediaNode
        let v = n.SortKey
        Assert.True(( v.Length = 5 ))
        Assert.True(( v.[0] = ClientConst.SORT_KEY_TYPE_PlainFileMedia ))
        Assert.True(( v.[1] = "gffgg" ))
        Assert.True(( v.[2] = "aaaa" ))
        Assert.True(( v.[3] = sprintf "%08X" 2u ))
        Assert.True(( v.[4] = sprintf "%016X" 1UL ))
    
    [<Fact>]
    member _.TempExportData_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let confVal2 : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 2u;
            MediaName = "gffgg";
            FileName = "aaaa";
            MaxMultiplicity = 189u;
            QueueWaitTimeOut = 24;
            WriteProtect = true;
        }
        let n = new ConfNode_PlainFileMedia( st, rel, confnode_me.fromPrim 1UL, confVal2 ) :> IMediaNode
        let v = n.TempExportData
        Assert.True(( v.TypeName = ClientConst.TEMPEXP_NN_PlainFileMedia ))
        Assert.True(( v.NodeID = 1UL ))
        Assert.True(( v.Values.Length = 6 ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "ID" ) |> _.Value = "2" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "MediaName" ) |> _.Value = "gffgg" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "FileName" ) |> _.Value = "aaaa" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "MaxMultiplicity" ) |> _.Value = "189" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "QueueWaitTimeOut" ) |> _.Value = "24" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "WriteProtect" ) |> _.Value = "true" ))


