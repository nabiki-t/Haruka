//=============================================================================
// Haruka Software Storage.
// ConfNode_TargetGroup.fs : It represents configurations of Target Group.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Client

//=============================================================================
// Import declaration

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System.Xml
open System.Xml.Schema

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Type definition

/// <summary>
///  Target group node used at the configurations.
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
/// <param name="m_TargetGroupID">
///  Target group ID.
/// </param>
/// <param name="m_TargetGroupName">
///  Target group Name.
/// </param>
/// <param name="m_EnabledAtStart">
///  Configuration value.
/// </param>
/// <param name="m_Modified">
///  Values are modified or not.
/// </param>
type ConfNode_TargetGroup(
        m_MessageTable : StringTable,
        m_ConfNodes : ConfNodeRelation,
        m_NodeID : CONFNODE_T,
        m_TargetGroupID : TGID_T,
        m_TargetGroupName : string,
        m_EnabledAtStart : bool,
        m_Modified : ModifiedStatus
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
        let tgid = Functions.SearchAndConvert d "ID" tgid_me.fromString tgid_me.Zero
        let name = Functions.SearchAndConvert d "Name" id ""
        let eas = Functions.SearchAndConvert d "EnabledAtStart" bool.Parse true
        new ConfNode_TargetGroup( argMessageTable, argConfNodes, newNodeID, tgid, name, eas, ModifiedStatus.Modified )

    //=========================================================================
    // Interface method

    /// <inheritdoc />
    interface IConfigFileNode with
        
        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.Validate
        override this.Validate ( msgList : ( CONFNODE_T * string ) list ) : ( CONFNODE_T * string ) list =
            let childNodes = m_ConfNodes.GetChildNodeList<IConfigureNode> m_NodeID
            let childNodes_Target = m_ConfNodes.GetChildNodeList<ConfNode_Target> m_NodeID
            let childNodes_AccessibleLU = this.GetAccessibleLUNodes()
            let childNodes_IsolatedLU = this.GetIsolatedLUNodes()
            let childNodes_Other =
                childNodes
                |> List.filter ( function :? ConfNode_Target -> false | _ -> true )
                |> List.filter ( function :? ILUNode -> false | _ -> true )
            let curID = ( this :> IConfigureNode ).NodeID
            let parentNoded = m_ConfNodes.GetParentNodeList<IConfigureNode> m_NodeID
            let curNodeTypeName = ( this :> IConfigureNode ).NodeTypeName

            msgList
            |> ( fun argmsg ->
                let v = m_TargetGroupName.Length
                let maxr = Constants.MAX_TARGET_GROUP_NAME_STR_LENGTH
                if maxr < v then
                    ( m_NodeID, m_MessageTable.GetMessage( "CHKMSG_TARGET_GROUP_NAME_TOO_LONG", maxr.ToString() ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = childNodes_Target.Length
                let maxr = Constants.MAX_TARGET_COUNT_IN_TD
                if maxr < v then
                    ( m_NodeID, m_MessageTable.GetMessage( "CHKMSG_OUT_OF_TARGET_COUNT_IN_TG", maxr.ToString() ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                if childNodes_Target.Length <= 0 then
                    ( m_NodeID, m_MessageTable.GetMessage( "CHKMSG_MISSING_TARGET" ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = childNodes_AccessibleLU.Length
                let maxr = Constants.MAX_LOGICALUNIT_COUNT_IN_TD
                if maxr < v then
                    ( m_NodeID, m_MessageTable.GetMessage( "CHKMSG_OUT_OF_LU_COUNT_IN_TG", maxr.ToString() ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                if childNodes_AccessibleLU.Length <= 0 then
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
                childNodes_IsolatedLU
                |> List.fold ( fun wMsgList itr ->
                    let msg = m_MessageTable.GetMessage( "CHKMSG_ISOLATED_LU", itr.NodeTypeName, curNodeTypeName )
                    ( itr.NodeID, msg ) :: wMsgList
                ) argmsg
            )
            |> ( fun argmsg ->
                childNodes_Other
                |> List.fold ( fun wMsgList itr ->
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_RELATION", itr.NodeTypeName, curNodeTypeName )
                    ( itr.NodeID, msg ) :: wMsgList
                ) argmsg
            )
            |> ( fun argmsg ->
                childNodes_Target
                |> List.fold ( fun wMsgList itr -> ( itr :> IConfigureNode ).Validate wMsgList ) argmsg
            )

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeID
        override _.NodeID : CONFNODE_T =
            m_NodeID

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeTypeName
        override _.NodeTypeName : string =
            ClientConst.NODE_TYPE_NAME_TargetGroup

        // ------------------------------------------------------------------------
        // Implementation of IConfigFileNode.IsModified
        override _.Modified : ModifiedStatus =
            m_Modified

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
            if m_TargetGroupName.Length > 0 then
                m_TargetGroupName
            else
                tgid_me.toString m_TargetGroupID

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.ShortDescriptString property
        override this.ShortDescriptString : string =
            sprintf "%s : ID=%s : Name=%s" ( this :> IConfigureNode ).NodeTypeName ( tgid_me.toString m_TargetGroupID ) m_TargetGroupName

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.FullDescriptString property
        override this.FullDescriptString : string list =
            [
                yield sprintf "Node type : %s" ( this :> IConfigureNode ).NodeTypeName
                yield         "Values :"
                yield sprintf "  ID(TargetGroupID) : %s" ( tgid_me.toString m_TargetGroupID )
                yield sprintf "  Name(string) : %s" m_TargetGroupName
                yield sprintf "  EnabledAtStart(bool) : %b" m_EnabledAtStart
                yield sprintf "Modified : %b" ( m_Modified = ModifiedStatus.Modified )
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.SortKey =
            [
                ClientConst.SORT_KEY_TYPE_TargetGroup;
                m_TargetGroupName;
                tgid_me.toString m_TargetGroupID;
                sprintf "%016X" m_NodeID;
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.TempExportData : TempExport.T_Node =
            {
                TypeName = ClientConst.TEMPEXP_NN_TargetGroup;
                NodeID = confnode_me.toPrim m_NodeID;
                Values = [
                    yield {
                        Name = "ID";
                        Value = tgid_me.toString m_TargetGroupID;
                    }
                    yield {
                        Name = "Name";
                        Value = m_TargetGroupName;
                    }
                    yield {
                        Name = "EnabledAtStart";
                        Value = sprintf "%b" m_EnabledAtStart;
                    }
                ]
            }

        // ------------------------------------------------------------------------
        // Implementation of IConfigFileNode.ResetModifiedFlag property
        override _.ResetModifiedFlag() : IConfigFileNode =
            new ConfNode_TargetGroup(
                m_MessageTable,
                m_ConfNodes,
                m_NodeID,
                m_TargetGroupID,
                m_TargetGroupName,
                m_EnabledAtStart,
                ModifiedStatus.NotModified
            )

    //=========================================================================
    // static method

    /// <summary>
    ///  Generate new target group ID
    /// </summary>
    /// <param name="v">
    ///  Already exists taget group nodes.
    /// </param>
    /// <returns>
    ///  Target group ID.
    /// </returns>
    static member GenNewTargetGroupID ( v : ConfNode_TargetGroup seq ) : TGID_T =
        v
        |> Seq.map _.TargetGroupID
        |> tgid_me.NewID 

    /// <summary>
    ///  Generate new target group name.
    /// </summary>
    /// <param name="v">
    ///  Already exists taget group nodes.
    /// </param>
    /// <returns>
    ///  Generated target group name.
    /// </returns>
    static member GenDefaultTargetGroupName ( v : ConfNode_TargetGroup seq ) : string =
        let prefix = "TargetGroup_"
        v
        |> Seq.map _.TargetGroupName
        |> Seq.filter _.StartsWith( prefix )
        |> Seq.map ( fun itr -> itr.[ prefix.Length .. ] )
        |> Seq.map UInt32.TryParse
        |> Seq.filter fst
        |> Seq.map snd
        |> Functions.GenUniqueNumber ( (+) 1u ) 0u
        |> sprintf "%s%05d" prefix

    //=========================================================================
    // public method

    /// property of TargetGroupID
    member _.TargetGroupID : TGID_T =
        m_TargetGroupID

    /// property of TargetGroupName
    member _.TargetGroupName : string =
        m_TargetGroupName

    /// property of EnabledAtStart
    member _.EnabledAtStart : bool =
        m_EnabledAtStart

    /// Get LU nodes that can be accessed from one or more target nodes.
    member this.GetAccessibleLUNodes() : ILUNode list =
        m_ConfNodes.GetChildNodeList<ConfNode_Target> m_NodeID
        |> Seq.map ( fun itr -> ( itr :> IConfigureNode ).NodeID )
        |> Seq.map m_ConfNodes.GetChild
        |> Seq.concat
        |> Seq.distinct
        |> Seq.map m_ConfNodes.GetNode
        |> Seq.filter ( function :? ILUNode -> true | _ -> false )
        |> Seq.map ( fun itr -> itr :?> ILUNode )
        |> Seq.toList

    /// Get LU nodes that can not be accessed from any target nodes.
    member _.GetIsolatedLUNodes() : ILUNode list =
        m_NodeID
        |> m_ConfNodes.GetChildNodeList<ILUNode>
        |> List.filter ( fun itr -> itr.GetParentNodes<IConfigureNode>().Length <= 1 )

    /// <summary>
    /// Create new object that has updated configuration values.
    /// </summary>
    /// <param name="argTargetGroupID">
    ///  Target group ID.
    /// </param>
    /// <param name="argTargetGroupName">
    ///  Target group Name.
    /// </param>
    /// <param name="argEnabledAtStart">
    ///  Configuration value.
    /// </param>
    /// <returns>
    ///  Created new node that has specified new values and same node ID.
    /// </returns>
    member _.CreateUpdatedNode 
        ( argTargetGroupID : TGID_T )
        ( argTargetGroupName : string )
        ( argEnabledAtStart : bool ) : ConfNode_TargetGroup =
        new ConfNode_TargetGroup(
            m_MessageTable, m_ConfNodes, m_NodeID, argTargetGroupID, argTargetGroupName, argEnabledAtStart, ModifiedStatus.Modified
        )

    /// <summary>
    ///  Create new object that has same value but modified flag set to "Modified".
    /// </summary>
    /// <returns>
    ///  Created new object.
    /// </returns>
    member this.SetModified() : ConfNode_TargetGroup =
        this.CreateUpdatedNode m_TargetGroupID m_TargetGroupName m_EnabledAtStart

    /// <summary>
    /// Get configuration data.
    /// </summary>
    /// <remarks>
    /// This method returns only target device configuration string.
    /// But it not include target group, or other node configurations.
    /// </remarks>
    member this.GetConfigureData() : TargetGroupConf.T_TargetGroup =
        let childNodes_Target = m_ConfNodes.GetChildNodeList<ConfNode_Target> m_NodeID
        let targetConfData =
            childNodes_Target
            |> Seq.map _.Values
            |> Seq.toList
        let childNodes_LU =
            this.GetAccessibleLUNodes()
            |> Seq.map _.LUConfData
            |> Seq.toList

        {
            TargetGroupID = m_TargetGroupID;
            TargetGroupName = m_TargetGroupName;
            EnabledAtStart = m_EnabledAtStart;
            Target = targetConfData;
            LogicalUnit = childNodes_LU;
        } : TargetGroupConf.T_TargetGroup

