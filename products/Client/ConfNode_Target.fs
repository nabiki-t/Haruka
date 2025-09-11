//=============================================================================
// Haruka Software Storage.
// ConfNode_Target.fs : It represents configurations of Target.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Client

//=============================================================================
// Import declaration

open System
open System.Collections.Generic

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Class implementation

/// <summary>
///  Target node used at the configurations.
/// </summary>
/// <param name="m_MessageTable">
///  Message resource reader.
/// </param>
/// <param name="m_ConfNodes">
///  Configuration nodes holder.
/// </param>
/// <param name="m_NodeID">
///  Node ID of this node.
/// </param>
/// <param name="m_Value">
///  Configuration value.
/// </param>
/// <remarks>
///  m_Value.LUN value is not used. It must be ignored.
/// </remarks>
type ConfNode_Target(
        m_MessageTable : StringTable,
        m_ConfNodes : ConfNodeRelation,
        m_NodeID : CONFNODE_T,
        m_Value : TargetGroupConf.T_Target
    ) =

    /// <summary>
    ///  Constructor for temp export format data.
    /// </summary>
    /// <param name="argMessageTable">
    ///  Message resource reader.
    /// </param>
    /// <param name="argConfNodes">
    ///  Configuration nodes holder.
    /// </param>
    /// <param name="newNodeID">
    ///  Node ID should be set this node..
    /// </param>
    /// <param name="tempExp">
    ///  Temp export format data.
    /// </param>
    /// <remarks>
    ///  Unknown data is ignored, and missing data is set to default values.
    /// </remarks>
    new ( argMessageTable : StringTable, argConfNodes : ConfNodeRelation, newNodeID : CONFNODE_T, tempExp : TempExport.T_Node ) =
        let d =
            tempExp.Values
            |> Seq.map ( fun itr -> KeyValuePair( itr.Name, itr.Value ) )
            |> Dictionary
        let conf : TargetGroupConf.T_Target = {
            IdentNumber = Functions.SearchAndConvert d "ID" ( UInt32.Parse >> tnodeidx_me.fromPrim ) ( tnodeidx_me.fromPrim 0u );
            TargetPortalGroupTag = Functions.SearchAndConvert d "TPGT" ( UInt16.Parse >> tpgt_me.fromPrim ) tpgt_me.zero;
            TargetName = Functions.SearchAndConvert d "Name" id "";
            TargetAlias = Functions.SearchAndConvert d "Alias" id "";
            LUN = []
            Auth =
                let auth = Functions.SearchAndConvert d "Auth" id "None";
                if auth = "CHAP" then
                    TargetGroupConf.U_CHAP({
                        InitiatorAuth = {
                            UserName = Functions.SearchAndConvert d "InitiatorAuth.UserName" id "";
                            Password = Functions.SearchAndConvert d "InitiatorAuth.Password" id "";
                        }
                        TargetAuth = {
                            UserName = Functions.SearchAndConvert d "TargetAuth.UserName" id "";
                            Password = Functions.SearchAndConvert d "TargetAuth.Password" id "";
                        }
                    })
                else
                    TargetGroupConf.U_None();
        }
        new ConfNode_Target( argMessageTable, argConfNodes, newNodeID, conf )

    //=========================================================================
    // Interface method

    interface IConfigureNode with
        
        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.Validate
        override this.Validate ( msgList : ( CONFNODE_T * string ) list ) : ( CONFNODE_T * string ) list =
            let childNodes = m_ConfNodes.GetChildNodeList<IConfigureNode> m_NodeID
            let childNodes_LU =
                childNodes
                |> List.filter ( function :? ILUNode -> true | _ -> false )
            let childNodes_Other =
                childNodes
                |> List.filter ( function :? ILUNode -> false | _ -> true )
            let curID = ( this :> IConfigureNode ).NodeID
            let parentNoded = m_ConfNodes.GetParentNodeList<IConfigureNode> m_NodeID
            let curNodeTypeName = ( this :> IConfigureNode ).NodeTypeName

            msgList
            |> ( fun argmsg ->
                let v = m_Value.TargetPortalGroupTag
                if v <> tpgt_me.zero then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_UNSUPPORTED_TPGT_VALUE" )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_Value.TargetName
                if not ( Constants.ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_OBJ.IsMatch( v ) ) then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_TARGET_NAME_FORMAT", v )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_Value.TargetAlias.Length
                let maxr = Constants.MAX_TARGET_ALIAS_STR_LENGTH
                if maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_TARGET_ALIAS_TOO_LONG", maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                match m_Value.Auth with
                | TargetGroupConf.U_CHAP( x ) ->
                    argmsg
                    |> ( fun argmsg2 ->
                        let v = x.InitiatorAuth.UserName
                        if not ( Constants.USER_NAME_REGEX_OBJ.IsMatch( v ) ) then
                            let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_CHAP_AUTH_USERNAME_FORMAT" )
                            ( curID, msg ) :: argmsg2
                        else
                            argmsg2
                    )
                    |> ( fun argmsg2 ->
                        let v = x.InitiatorAuth.Password
                        if not ( Constants.PASSWORD_REGEX_OBJ.IsMatch( v ) ) then
                            let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_CHAP_AUTH_PASSWORD_FORMAT" )
                            ( curID, msg ) :: argmsg2
                        else
                            argmsg2
                    )
                    |> ( fun argmsg2 ->
                        let v = x.TargetAuth.UserName
                        if v.Length = 0 || Constants.USER_NAME_REGEX_OBJ.IsMatch( v ) then
                            argmsg2
                        else
                            let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_CHAP_AUTH_USERNAME_FORMAT" )
                            ( curID, msg ) :: argmsg2
                    )
                    |> ( fun argmsg2 ->
                        let u = x.TargetAuth.UserName
                        let p = x.TargetAuth.Password
                        if u.Length > 0 then
                            if not ( Constants.PASSWORD_REGEX_OBJ.IsMatch( p ) ) then
                                let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_CHAP_AUTH_PASSWORD_FORMAT" )
                                ( curID, msg ) :: argmsg2
                            else
                                argmsg2
                        else
                            if p.Length <> 0 then
                                let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_CHAP_AUTH_PASSWORD_OMIT" )
                                ( curID, msg ) :: argmsg2
                            else
                                argmsg2
                    )
                | TargetGroupConf.U_None( _ ) ->
                    argmsg
            )
            |> ( fun argmsg ->
                let v = childNodes_LU.Length
                let maxr = Constants.MAX_LOGICALUNIT_COUNT_IN_TD
                if maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_LUN_CNT_IN_TARGET", maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                if childNodes_LU.Length <= 0 then
                    ( m_NodeID, m_MessageTable.GetMessage( "CHKMSG_MISSING_LU" ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                if parentNoded.Length > 1 then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_TOO_MANY_PARENT", curNodeTypeName, "1" )
                    ( curID, msg ) :: argmsg
                elif parentNoded.Length < 1 then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_MISSING_PARENT", curNodeTypeName )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                childNodes_Other
                |> List.fold ( fun wMsgList itr ->
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_RELATION", itr.NodeTypeName, curNodeTypeName )
                    ( itr.NodeID, msg ) :: wMsgList
                ) argmsg
            )
            |> ( fun argmsg ->
                childNodes_LU
                |> List.fold ( fun wMsgList itr -> itr.Validate wMsgList ) argmsg
            )

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeID
        override _.NodeID : CONFNODE_T =
            m_NodeID

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeTypeName
        override _.NodeTypeName : string =
            ClientConst.NODE_TYPE_NAME_Target

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.GetChildNodes method
        override _.GetChildNodes< 'T when 'T :> IConfigureNode >() : 'T list =
            m_ConfNodes.GetChildNodeList<'T> m_NodeID

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.GetDescendantNodes method
        override _.GetDescendantNodes< 'T when 'T :> IConfigureNode >() : 'T list =
            m_ConfNodes.GetAllChildNodeList<'T> m_NodeID

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.GetParentNodes method
        override _.GetParentNodes< 'T when 'T :> IConfigureNode >() : 'T list =
            m_ConfNodes.GetParentNodeList<'T> m_NodeID

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.GetAncestorNode method
        override _.GetAncestorNode< 'T when 'T :> IConfigureNode >() : 'T list =
            m_ConfNodes.GetAllParentNodeList<'T> m_NodeID

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.MinDescriptString property
        override _.MinDescriptString : string =
            if m_Value.TargetAlias.Length > 0 then
                m_Value.TargetAlias
            else
                m_Value.TargetName

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.ShortDescriptString property
        override this.ShortDescriptString : string =
            sprintf "%s : ID=%d : Name=%s" ( this :> IConfigureNode ).NodeTypeName m_Value.IdentNumber m_Value.TargetName

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.FullDescriptString property
        override this.FullDescriptString : string list =
            let childLUN =
                m_ConfNodes.GetChildNodeList<ILUNode> m_NodeID
                |> List.map _.LUN
            [
                yield sprintf "Node type : %s" ( this :> IConfigureNode ).NodeTypeName
                yield         "Values :"
                yield sprintf "  ID(uint32)    : %d" m_Value.IdentNumber
                yield sprintf "  TPGT(uint16)  : %d" m_Value.TargetPortalGroupTag
                yield sprintf "  Name(string)  : %s" m_Value.TargetName
                yield sprintf "  Alias(string) : %s" m_Value.TargetAlias
                yield         "  LUN : "
                for i in childLUN do
                    yield sprintf "    %s" ( lun_me.toString i )
                match m_Value.Auth with
                | TargetGroupConf.T_Auth.U_None _ ->
                    yield         "  Auth : None"
                | TargetGroupConf.T_Auth.U_CHAP x ->
                    yield         "  Auth : CHAP"
                    yield         "    Initiator Auth :"
                    yield sprintf "      UserName(string) : %s" x.InitiatorAuth.UserName
                    yield sprintf "      Password(string) : %s" x.InitiatorAuth.Password
                    yield         "    Target Auth :"
                    yield sprintf "      UserName(string) : %s" x.TargetAuth.UserName
                    yield sprintf "      Password(string) : %s" x.TargetAuth.Password
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.SortKey =
            [
                ClientConst.SORT_KEY_TYPE_Target;
                m_Value.TargetAlias;
                m_Value.TargetName;
                sprintf "%08X" m_Value.IdentNumber;
                sprintf "%016X" m_NodeID;
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override this.TempExportData : TempExport.T_Node =
            {
                TypeName = ClientConst.TEMPEXP_NN_Target;
                NodeID = confnode_me.toPrim m_NodeID;
                Values = [
                    yield {
                        Name = "ID";
                        Value = sprintf "%d" m_Value.IdentNumber;
                    };
                    yield {
                        Name = "TPGT";
                        Value = sprintf "%d" m_Value.TargetPortalGroupTag;
                    };
                    yield {
                        Name = "Name";
                        Value = m_Value.TargetName;
                    };
                    yield {
                        Name = "Alias";
                        Value = m_Value.TargetAlias;
                    };
                    match m_Value.Auth with
                    | TargetGroupConf.T_Auth.U_None _ ->
                        yield {
                            Name = "Auth";
                            Value = "None";
                        };
                    | TargetGroupConf.T_Auth.U_CHAP x ->
                        yield {
                            Name = "Auth";
                            Value = "CHAP";
                        };
                        yield {
                            Name = "InitiatorAuth.UserName";
                            Value = x.InitiatorAuth.UserName;
                        };
                        yield {
                            Name = "InitiatorAuth.Password";
                            Value = x.InitiatorAuth.Password;
                        };
                        yield {
                            Name = "TargetAuth.UserName";
                            Value = x.TargetAuth.UserName;
                        };
                        yield {
                            Name = "TargetAuth.Password";
                            Value = x.TargetAuth.Password;
                        };
                ]
            }

    //=========================================================================
    // static method

    /// <summary>
    ///  Generate new target node ID
    /// </summary>
    /// <param name="v">
    ///  Already exists taget nodes.
    /// </param>
    /// <returns>
    ///  Target node ID.
    /// </returns>
    static member GenNewID ( v : ConfNode_Target seq ) : TNODEIDX_T =
        v
        |> Seq.map _.Values.IdentNumber
        |> Seq.toArray
        |> Functions.GenUniqueNumber ( (+) 1u<tnodeidx_me> ) ( tnodeidx_me.fromPrim 0u )

    /// <summary>
    ///  Generate new target name.
    /// </summary>
    /// <param name="v">
    ///  Already exists taget nodes.
    /// </param>
    /// <returns>
    ///  Generated target name.
    /// </returns>
    static member GenDefaultTargetName ( v : ConfNode_Target seq ) : string =
        v
        |> Seq.map _.Values.TargetName
        |> Seq.filter _.StartsWith( Constants.DEF_ISCSI_TARGET_NAME_PREFIX )
        |> Seq.map ( fun itr -> itr.[ Constants.DEF_ISCSI_TARGET_NAME_PREFIX.Length .. ] )
        |> Seq.map UInt32.TryParse
        |> Seq.filter fst
        |> Seq.map snd
        |> Functions.GenUniqueNumber ( (+) 1u ) 0u
        |> sprintf "%s%03d" Constants.DEF_ISCSI_TARGET_NAME_PREFIX

    //=========================================================================
    // public method

    /// Configuration values property
    member this.Values : TargetGroupConf.T_Target =
        // m_Value.LUN value must be ignored.
        // Thus, create new LUN list.
        let childLUN =
            m_ConfNodes.GetChildNodeList<ILUNode> m_NodeID
            |> List.map _.LUN
        {
            m_Value with
                LUN = childLUN
        }

    /// <summary>
    /// Create new object that has updated configuration values.
    /// </summary>
    /// <param name="argValue">
    ///  Configuration values.
    /// </param>
    /// <returns>
    ///  Created new node that has specified new values and same node ID.
    /// </returns>
    member _.CreateUpdatedNode 
        ( argValue : TargetGroupConf.T_Target ) : ConfNode_Target =
        new ConfNode_Target( m_MessageTable, m_ConfNodes, m_NodeID, argValue )
