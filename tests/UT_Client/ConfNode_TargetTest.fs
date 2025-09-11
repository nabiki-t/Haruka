//=============================================================================
// Haruka Software Storage.
// ConfNode_TargetTest.fs : Test cases for ConfNode_Target class.
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

type ConfNode_Target_Test() =

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

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constractor_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_Target( st, rel, confnode_me.fromPrim 1UL, defaultConf ) :> IConfigureNode
        Assert.True(( n.NodeID = confnode_me.fromPrim 1UL ))
        Assert.True(( n.NodeTypeName = "Target" ))

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
        let n = ConfNode_Target( st, rel, cid, te )
        rel.AddNode n   // The value cannot be referenced unless n is added to ConfNodeRelation.
        Assert.True(( ( n :> IConfigureNode ).NodeID = cid ))
        Assert.True(( n.Values.IdentNumber = tnodeidx_me.fromPrim 0u ))
        Assert.True(( n.Values.TargetPortalGroupTag = tpgt_me.zero ))
        Assert.True(( n.Values.TargetName = "" ))
        Assert.True(( n.Values.TargetAlias = "" ))
        Assert.True(( n.Values.LUN = [] ))
        match n.Values.Auth with
        | TargetGroupConf.U_None() ->
            ()
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
                    Value = "1254";
                }
                {
                    Name = "TPGT";
                    Value = "145";
                }
                {
                    Name = "Name";
                    Value = "asdf";
                }
                {
                    Name = "Alias";
                    Value = "123rfg";
                }
                {
                    Name = "Auth";
                    Value = "None";
                }
            ];
        }
        let n = ConfNode_Target( st, rel, cid, te )
        rel.AddNode n   // The value cannot be referenced unless n is added to ConfNodeRelation.
        Assert.True(( ( n :> IConfigureNode ).NodeID = cid ))
        Assert.True(( n.Values.IdentNumber = tnodeidx_me.fromPrim 1254u ))
        Assert.True(( n.Values.TargetPortalGroupTag = tpgt_me.fromPrim 145us ))
        Assert.True(( n.Values.TargetName = "asdf" ))
        Assert.True(( n.Values.TargetAlias = "123rfg" ))
        Assert.True(( n.Values.LUN = [] ))
        match n.Values.Auth with
        | TargetGroupConf.U_None() ->
            ()
        | _ -> Assert.Fail __LINE__      

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
                    Name = "ID";
                    Value = "1254";
                }
                {
                    Name = "TPGT";
                    Value = "145";
                }
                {
                    Name = "Name";
                    Value = "asdf";
                }
                {
                    Name = "Alias";
                    Value = "123rfg";
                }
                {
                    Name = "Auth";
                    Value = "CHAP";
                }
                {
                    Name = "InitiatorAuth.UserName";
                    Value = "a123";
                }
                {
                    Name = "InitiatorAuth.Password";
                    Value = "b123";
                }
                {
                    Name = "TargetAuth.UserName";
                    Value = "c123";
                }
                {
                    Name = "TargetAuth.Password";
                    Value = "d123";
                }
            ];
        }
        let n = ConfNode_Target( st, rel, cid, te )
        rel.AddNode n   // The value cannot be referenced unless n is added to ConfNodeRelation.
        Assert.True(( ( n :> IConfigureNode ).NodeID = cid ))
        Assert.True(( n.Values.IdentNumber = tnodeidx_me.fromPrim 1254u ))
        Assert.True(( n.Values.TargetPortalGroupTag = tpgt_me.fromPrim 145us ))
        Assert.True(( n.Values.TargetName = "asdf" ))
        Assert.True(( n.Values.TargetAlias = "123rfg" ))
        Assert.True(( n.Values.LUN = [] ))
        match n.Values.Auth with
        | TargetGroupConf.U_CHAP( x ) ->
            Assert.True(( x.InitiatorAuth.UserName = "a123" ))
            Assert.True(( x.InitiatorAuth.Password = "b123" ))
            Assert.True(( x.TargetAuth.UserName = "c123" ))
            Assert.True(( x.TargetAuth.Password = "d123" ))
            ()
        | _ -> Assert.Fail __LINE__

    [<Fact>]
    member _.CreateUpdatedNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_Target( st, rel, confnode_me.fromPrim 1UL, defaultConf ) :> IConfigureNode
        Assert.True(( n.NodeID = confnode_me.fromPrim 1UL ))
        Assert.True(( n.NodeTypeName = "Target" ))

        let confVal2 : TargetGroupConf.T_Target = {
            IdentNumber = tnodeidx_me.fromPrim 1u;
            TargetPortalGroupTag = tpgt_me.fromPrim 1us;
            TargetName = "bbb";
            TargetAlias = "ggg";
            LUN = [ lun_me.fromPrim 2UL ];
            Auth = TargetGroupConf.U_None();
        }

        let n2 = ( n :?> ConfNode_Target ).CreateUpdatedNode( confVal2 ) :> IConfigureNode
        Assert.True(( n.NodeID = n2.NodeID ))
        Assert.True(( n.NodeTypeName = n2.NodeTypeName ))

        let pc1 = PrivateCaller( n )
        let pc2 = PrivateCaller( n2 )
        Assert.True(( pc1.GetField( "m_MessageTable" ) = pc2.GetField( "m_MessageTable" ) ))
        Assert.True(( pc1.GetField( "m_ConfNodes" ) = pc2.GetField( "m_ConfNodes" ) ))

        let rc = pc2.GetField( "m_Value" ) :?> TargetGroupConf.T_Target
        Assert.True(( rc.TargetPortalGroupTag = tpgt_me.fromPrim 1us ))
        Assert.True(( rc.TargetAlias = "ggg" ))

    [<Fact>]
    member _.Validate_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                TargetPortalGroupTag = tpgt_me.fromPrim 1us;
        }

        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

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
                TargetName = "****";
        }

        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_TARGET_NAME_FORMAT" ))

    [<Fact>]
    member _.Validate_004() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                TargetAlias = String.replicate Constants.MAX_TARGET_ALIAS_STR_LENGTH "a";
        }

        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_005() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                TargetAlias = String.replicate ( Constants.MAX_TARGET_ALIAS_STR_LENGTH + 1 ) "a";
        }

        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_TARGET_ALIAS_TOO_LONG" ))

    [<Fact>]
    member _.Validate_006() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "";
                        Password = "aaa";
                    };
                    TargetAuth = {
                        UserName = "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_CHAP_AUTH_USERNAME_FORMAT" ))

    [<Fact>]
    member _.Validate_007() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "aaa";
                    };
                    TargetAuth = {
                        UserName = "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_008() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = String.replicate Constants.MAX_USER_NAME_STR_LENGTH "a";
                        Password = "aaa";
                    };
                    TargetAuth = {
                        UserName = "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_009() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = String.replicate ( Constants.MAX_USER_NAME_STR_LENGTH + 1 ) "a";
                        Password = "aaa";
                    };
                    TargetAuth = {
                        UserName = "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_CHAP_AUTH_USERNAME_FORMAT" ))

    [<Fact>]
    member _.Validate_010() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "&&&&&";
                        Password = "aaa";
                    };
                    TargetAuth = {
                        UserName = "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_CHAP_AUTH_USERNAME_FORMAT" ))

    [<Fact>]
    member _.Validate_011() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "";
                    };
                    TargetAuth = {
                        UserName = "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_CHAP_AUTH_PASSWORD_FORMAT" ))

    [<Fact>]
    member _.Validate_012() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "a";
                    };
                    TargetAuth = {
                        UserName = "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_013() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = String.replicate Constants.MAX_PASSWORD_STR_LENGTH "a";
                    };
                    TargetAuth = {
                        UserName = "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_014() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = String.replicate ( Constants.MAX_PASSWORD_STR_LENGTH + 1 ) "a";
                    };
                    TargetAuth = {
                        UserName = "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_CHAP_AUTH_PASSWORD_FORMAT" ))

    [<Fact>]
    member _.Validate_015() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "$$$";
                    };
                    TargetAuth = {
                        UserName = "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_CHAP_AUTH_PASSWORD_FORMAT" ))

    [<Fact>]
    member _.Validate_016() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "a";
                    };
                    TargetAuth = {
                        UserName = "";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_CHAP_AUTH_PASSWORD_OMIT" ))

    [<Fact>]
    member _.Validate_017() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "a";
                    };
                    TargetAuth = {
                        UserName = "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_018() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "a";
                    };
                    TargetAuth = {
                        UserName = String.replicate Constants.MAX_USER_NAME_STR_LENGTH "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_019() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "a";
                    };
                    TargetAuth = {
                        UserName = String.replicate ( Constants.MAX_USER_NAME_STR_LENGTH + 1 ) "a";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_CHAP_AUTH_USERNAME_FORMAT" ))

    [<Fact>]
    member _.Validate_020() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "a";
                    };
                    TargetAuth = {
                        UserName = "&&&";
                        Password = "aaa";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_CHAP_AUTH_USERNAME_FORMAT" ))

    [<Fact>]
    member _.Validate_021() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "a";
                    };
                    TargetAuth = {
                        UserName = "a"
                        Password = "";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_CHAP_AUTH_PASSWORD_FORMAT" ))

    [<Fact>]
    member _.Validate_022() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "a";
                    };
                    TargetAuth = {
                        UserName = "a"
                        Password = "a";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_023() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "a";
                    };
                    TargetAuth = {
                        UserName = "a"
                        Password = String.replicate Constants.MAX_PASSWORD_STR_LENGTH "a";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_024() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "a";
                    };
                    TargetAuth = {
                        UserName = "a"
                        Password = String.replicate ( Constants.MAX_PASSWORD_STR_LENGTH + 1 ) "a";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_CHAP_AUTH_PASSWORD_FORMAT" ))

    [<Fact>]
    member _.Validate_025() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "a";
                    };
                    TargetAuth = {
                        UserName = "a"
                        Password = "$$$";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_CHAP_AUTH_PASSWORD_FORMAT" ))

    [<Fact>]
    member _.Validate_026() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf = {
            defaultConf with
                Auth = TargetGroupConf.U_CHAP( {
                    InitiatorAuth = {
                        UserName = "a";
                        Password = "a";
                    };
                    TargetAuth = {
                        UserName = ""
                        Password = "";
                    };
                } );
        }
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_027() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddRelation tg.NodeID n.NodeID

        let vdd = [|
            for i = 1 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD do
                let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
                rel.AddNode dd
                yield dd
        |]
        for i = 0 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD - 1 do
            rel.AddRelation n.NodeID vdd.[i].NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.Validate_028() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddRelation tg.NodeID n.NodeID

        let vdd = [|
            for i = 1 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD + 1 do
                let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
                rel.AddNode dd
                yield dd
        |]
        for i = 0 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD do
            rel.AddRelation n.NodeID vdd.[i].NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_LUN_CNT_IN_TARGET" ))


    [<Fact>]
    member _.Validate_029() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddRelation tg.NodeID n.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_MISSING_LU" ))

    [<Fact>]
    member _.Validate_030() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID

        let dm1 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 1u, "" ) :> IMediaNode
        let dm2 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 2u, "" ) :> IMediaNode
        let dm3 = new ConfNode_DummyMedia( st, rel, rel.NextID, mediaidx_me.fromPrim 3u, "" ) :> IMediaNode
        rel.AddNode dm1
        rel.AddNode dm2
        rel.AddNode dm3
        rel.AddRelation n.NodeID dm1.NodeID
        rel.AddRelation n.NodeID dm2.NodeID
        rel.AddRelation n.NodeID dm3.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 3 ))
        Assert.True(( fst r.[0] = dm3.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_INVALID_RELATION" ))
        Assert.True(( fst r.[1] = dm2.NodeID ))
        Assert.True(( ( snd r.[1] ).StartsWith "CHKMSG_INVALID_RELATION" ))
        Assert.True(( fst r.[2] = dm1.NodeID ))
        Assert.True(( ( snd r.[2] ).StartsWith "CHKMSG_INVALID_RELATION" ))

    [<Fact>]
    member _.Validate_031() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tg = new ConfNode_TargetGroup( st, rel, rel.NextID, GlbFunc.newTargetGroupID(), "a", true, ModifiedStatus.NotModified ) :> IConfigFileNode
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 0UL, "" ) :> ILUNode
        rel.AddNode tg
        rel.AddNode n
        rel.AddNode dd1
        rel.AddRelation tg.NodeID n.NodeID
        rel.AddRelation n.NodeID dd1.NodeID

        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 0UL, "" ) :> ILUNode
        rel.AddNode dd2
        rel.AddRelation n.NodeID dd2.NodeID

        let dd3 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 0UL, "" ) :> ILUNode
        rel.AddNode dd3
        rel.AddRelation n.NodeID dd3.NodeID

        let r = n.Validate []
        Assert.True(( r.Length = 3 ))
        for i = 0 to 2 do
            Assert.True(( ( snd r.[i] ).StartsWith "CHKMSG_INVALID_LUN_VALUE" ))

    [<Fact>]
    member _.Validate_032() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let tg1 = new DummyNode( rel.NextID, "D1" ) :> IConfigureNode
        let tg2 = new DummyNode( rel.NextID, "D1" ) :> IConfigureNode
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode tg1
        rel.AddNode tg2
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation tg1.NodeID n.NodeID
        rel.AddRelation tg2.NodeID n.NodeID
        rel.AddRelation n.NodeID dd.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_TOO_MANY_PARENT" ))

    [<Fact>]
    member _.Validate_033() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        let dd = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 1UL, "" ) :> ILUNode
        rel.AddNode n
        rel.AddNode dd
        rel.AddRelation n.NodeID dd.NodeID
        let r = n.Validate []
        Assert.True(( r.Length = 1 ))
        Assert.True(( fst r.[0] = n.NodeID ))
        Assert.True(( ( snd r.[0] ).StartsWith "CHKMSG_MISSING_PARENT" ))

    [<Fact>]
    member _.Values_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : TargetGroupConf.T_Target = {
            IdentNumber = tnodeidx_me.fromPrim 2u;
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            TargetName = "abcdefg";
            TargetAlias = "aabbccd";
            LUN = [ lun_me.fromPrim 1UL; lun_me.fromPrim 2UL; lun_me.fromPrim 3UL ];
            Auth = TargetGroupConf.U_None();
        }
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        rel.AddNode n

        let v = ( n :?> ConfNode_Target ).Values
        Assert.True(( v.IdentNumber = tnodeidx_me.fromPrim 2u ))
        Assert.True(( v.TargetName = "abcdefg" ))
        Assert.True(( v.TargetAlias = "aabbccd" ))
        Assert.True(( v.LUN = [] ))


    [<Fact>]
    member _.Values_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : TargetGroupConf.T_Target = {
            IdentNumber = tnodeidx_me.fromPrim 2u;
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            TargetName = "abcdefg";
            TargetAlias = "aabbccd";
            LUN = [ lun_me.fromPrim 1UL ];
            Auth = TargetGroupConf.U_None();
        }
        let n = new ConfNode_Target( st, rel, rel.NextID, conf ) :> IConfigureNode
        rel.AddNode n

        let dd1 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 10UL, "" ) :> ILUNode
        rel.AddNode dd1
        rel.AddRelation n.NodeID dd1.NodeID

        let dd2 = new ConfNode_DummyDeviceLU( st, rel, rel.NextID, lun_me.fromPrim 11UL, "" ) :> ILUNode
        rel.AddNode dd2
        rel.AddRelation n.NodeID dd2.NodeID

        let v = ( n :?> ConfNode_Target ).Values
        Assert.True(( v.IdentNumber = tnodeidx_me.fromPrim 2u ))
        Assert.True(( v.TargetName = "abcdefg" ))
        Assert.True(( v.TargetAlias = "aabbccd" ))
        Assert.True(( v.LUN = [ lun_me.fromPrim 10UL; lun_me.fromPrim 11UL ] ))

    [<Fact>]
    member _.GetChildNode_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode n
        let r = n.GetChildNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetChildNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode n
        let r = n.GetDescendantNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetDescendantNodes_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode n
        let r = n.GetParentNodes<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetParentNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
        rel.AddNode n
        let r = n.GetAncestorNode<IConfigureNode>()
        Assert.True(( r.Length = 0 ))

    [<Fact>]
    member _.GetAncestorNode_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let dm = new DummyNode( rel.NextID, "" ) :> IConfigureNode
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
        let n = new ConfNode_Target( st, rel, rel.NextID, defaultConf ) :> IConfigureNode
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
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with IdentNumber = tnodeidx_me.fromPrim 0u } );
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with IdentNumber = tnodeidx_me.fromPrim 1u } );
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_Target.GenNewID v
        Assert.True(( n = tnodeidx_me.fromPrim 2u ))

    [<Fact>]
    member _.GenNewID_002() =
        let n = ConfNode_Target.GenNewID []
        Assert.True(( n = tnodeidx_me.fromPrim 0u ))

    [<Fact>]
    member _.GenNewID_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with IdentNumber = tnodeidx_me.fromPrim 0u } );
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with IdentNumber = tnodeidx_me.fromPrim 1u } );
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with IdentNumber = tnodeidx_me.fromPrim 2u } );
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with IdentNumber = tnodeidx_me.fromPrim UInt32.MaxValue } );
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_Target.GenNewID v
        Assert.True(( n = tnodeidx_me.fromPrim 3u ))

    [<Fact>]
    member _.GenDefaultTargetName_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with TargetName = "iqn.1999-01.com.example:000" } );
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with TargetName = "iqn.1999-01.com.example:001" } );
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with TargetName = "iqn.1999-01.com.example:0002" } );
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with TargetName = "iqn.1999-01.com.example:3" } );
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_Target.GenDefaultTargetName v
        Assert.True(( n = "iqn.1999-01.com.example:004" ))

    [<Fact>]
    member _.GenDefaultTargetName_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with TargetName = "iqn.1999-01.com.example:000" } );
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with TargetName = "iqn.aaaa:000" } );
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_Target.GenDefaultTargetName v
        Assert.True(( n = "iqn.1999-01.com.example:001" ))

    [<Fact>]
    member _.GenDefaultTargetName_003() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let v = [
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with TargetName = "iqn.1999-01.com.example:000" } );
            new ConfNode_Target( st, rel, rel.NextID, { defaultConf with TargetName = "iqn.1999-01.com.example:001a" } );
        ]
        for i in v do rel.AddNode i
        let n = ConfNode_Target.GenDefaultTargetName v
        Assert.True(( n = "iqn.1999-01.com.example:001" ))

    [<Fact>]
    member _.GenDefaultTargetName_004() =
        let n = ConfNode_Target.GenDefaultTargetName []
        Assert.True(( n = "iqn.1999-01.com.example:000" ))

    [<Fact>]
    member _.SortKey_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : TargetGroupConf.T_Target = {
            IdentNumber = tnodeidx_me.fromPrim 423u;
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            TargetName = "a456aa";
            TargetAlias = "bbggrrtt";
            LUN = [ lun_me.fromPrim 1UL ];
            Auth = TargetGroupConf.U_None();
        }
        let n = new ConfNode_Target( st, rel, confnode_me.fromPrim 756UL, conf ) :> IConfigureNode
        let v = n.SortKey
        Assert.True(( v.Length = 5 ))
        Assert.True(( v.[0] = ClientConst.SORT_KEY_TYPE_Target ))
        Assert.True(( v.[1] = "bbggrrtt" ))
        Assert.True(( v.[2] = "a456aa" ))
        Assert.True(( v.[3] = sprintf "%08X" 423u ))
        Assert.True(( v.[4] = sprintf "%016X" 756UL ))
    
    [<Fact>]
    member _.TempExportData_001() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : TargetGroupConf.T_Target = {
            IdentNumber = tnodeidx_me.fromPrim 423u;
            TargetPortalGroupTag = tpgt_me.fromPrim 99us;
            TargetName = "a456aa";
            TargetAlias = "bbggrrtt";
            LUN = [ lun_me.fromPrim 1UL ];
            Auth = TargetGroupConf.U_None();
        }
        let n = new ConfNode_Target( st, rel, confnode_me.fromPrim 756UL, conf ) :> IConfigureNode
        let v = n.TempExportData
        Assert.True(( v.TypeName = ClientConst.TEMPEXP_NN_Target ))
        Assert.True(( v.NodeID = 756UL ))
        Assert.True(( v.Values.Length = 5 ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "ID" ) |> _.Value = "423" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "TPGT" ) |> _.Value = "99" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "Name" ) |> _.Value = "a456aa" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "Alias" ) |> _.Value = "bbggrrtt" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "Auth" ) |> _.Value = "None" ))
    
    [<Fact>]
    member _.TempExportData_002() =
        let st = new StringTable( "" )
        let rel = new ConfNodeRelation()
        let conf : TargetGroupConf.T_Target = {
            IdentNumber = tnodeidx_me.fromPrim 423u;
            TargetPortalGroupTag = tpgt_me.fromPrim 99us;
            TargetName = "a456aa";
            TargetAlias = "bbggrrtt";
            LUN = [ lun_me.fromPrim 1UL ];
            Auth = TargetGroupConf.U_CHAP({
                InitiatorAuth = {
                    UserName = "q1122";
                    Password = "w2233";
                };
                TargetAuth = {
                    UserName = "e3344";
                    Password = "r4455";
                };
            });
        }
        let n = new ConfNode_Target( st, rel, confnode_me.fromPrim 756UL, conf ) :> IConfigureNode
        let v = n.TempExportData
        Assert.True(( v.TypeName = ClientConst.TEMPEXP_NN_Target ))
        Assert.True(( v.NodeID = 756UL ))
        Assert.True(( v.Values.Length = 9 ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "ID" ) |> _.Value = "423" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "TPGT" ) |> _.Value = "99" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "Name" ) |> _.Value = "a456aa" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "Alias" ) |> _.Value = "bbggrrtt" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "Auth" ) |> _.Value = "CHAP" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "InitiatorAuth.UserName" ) |> _.Value = "q1122" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "InitiatorAuth.Password" ) |> _.Value = "w2233" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "TargetAuth.UserName" ) |> _.Value = "e3344" ))
        Assert.True(( v.Values |> Seq.find ( fun itr -> itr.Name = "TargetAuth.Password" ) |> _.Value = "r4455" ))
