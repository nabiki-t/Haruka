//=============================================================================
// Haruka Software Storage.
// ConfNode_DummyDeviceLU.fs : It represents configurations of Dummy device logical unit.
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
/// <param name="m_LUN">
///  Logical unit number.
/// </param>
/// <param name="m_LUName">
///  Logical unit name.
/// </param>
type ConfNode_DummyDeviceLU(
        m_MessageTable : StringTable,
        m_ConfNodes : ConfNodeRelation,
        m_NodeID : CONFNODE_T,
        m_LUN : LUN_T,
        m_LUName : string
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
        let lun = Functions.SearchAndConvert d "LUN" lun_me.fromStringValue ( lun_me.fromPrim 1UL )
        let name = Functions.SearchAndConvert d "Name" id ""
        new ConfNode_DummyDeviceLU( argMessageTable, argConfNodes, newNodeID, lun, name )

    //=========================================================================
    // Interface method

    /// <inheritdoc />
    interface ILUNode with
        
        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.Validate
        override this.Validate ( msgList : ( CONFNODE_T * string ) list ) : ( CONFNODE_T * string ) list =
            let childNodes = m_ConfNodes.GetChildNodeList<IConfigureNode> m_NodeID
            let curID = ( this :> IConfigureNode ).NodeID
            let childNodes_DummyMedia, childNodes_OtherNode =
                childNodes
                |> List.partition ( function :? ConfNode_DummyMedia -> true | _ -> false )
            let allTargetGroupNodes = m_ConfNodes.GetAllParentNodeList<ConfNode_TargetGroup> m_NodeID
            let parentNoded = m_ConfNodes.GetParentNodeList<IConfigureNode> m_NodeID
            let curNodeTypeName = ( this :> IConfigureNode ).NodeTypeName

            msgList
            |> ( fun argmsg ->
                if m_LUN = lun_me.zero || lun_me.toPrim m_LUN < Constants.MIN_LUN_VALUE || lun_me.toPrim m_LUN > Constants.MAX_LUN_VALUE then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_LUN_VALUE", m_LUN.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_LUName.Length
                let maxr = Constants.MAX_LU_NAME_STR_LENGTH
                if maxr < v then
                    ( m_NodeID, m_MessageTable.GetMessage( "CHKMSG_LU_NAME_TOO_LONG", maxr.ToString() ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                if 0 <> childNodes_DummyMedia.Length then
                    ( curID, m_MessageTable.GetMessage( "CHKMSG_OUT_OF_MEDIA_COUNT", "0" ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                if parentNoded.Length < 1 then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_MISSING_PARENT", curNodeTypeName )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                if allTargetGroupNodes.Length > 1 then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_BELONGS_MULTI_GROUP" )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                childNodes_OtherNode
                |> List.fold ( fun wMsgList itr ->
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_RELATION", itr.NodeTypeName, curNodeTypeName )
                    ( itr.NodeID, msg ) :: wMsgList
                ) argmsg
            )

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeID property
        override _.NodeID : CONFNODE_T =
            m_NodeID

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeTypeName property
        override _.NodeTypeName : string =
            ClientConst.NODE_TYPE_NAME_DummyDeviceLU

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
            if m_LUName.Length > 0 then
                m_LUName
            else
                m_LUN |> lun_me.toString

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.ShortDescriptString property
        override this.ShortDescriptString : string =
            sprintf "%s : LUN=%s" ( this :> IConfigureNode ).NodeTypeName ( lun_me.toString m_LUN )

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.FullDescriptString property
        override this.FullDescriptString : string list =
            [
                yield sprintf "Node type : %s" ( this :> IConfigureNode ).NodeTypeName
                yield sprintf "Values  :"
                yield sprintf "  LUN : %s" ( lun_me.toString m_LUN )
                yield sprintf "  Name(string) : %s" m_LUName
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.SortKey =
            [
                ClientConst.SORT_KEY_TYPE_DummyDeviceLU;
                m_LUName;
                ( lun_me.toString m_LUN );
                sprintf "%016X" m_NodeID;
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.TempExportData : TempExport.T_Node =
            {
                TypeName = ClientConst.TEMPEXP_NN_DummyDeviceLU;
                NodeID = confnode_me.toPrim m_NodeID;
                Values = [
                    yield {
                        Name = "LUN";
                        Value = lun_me.toString m_LUN;
                    }
                    yield {
                        Name = "Name";
                        Value = m_LUName;
                    }
                ]
            }

        // --------------------------------------------------------------------
        // Implementation of ILUNode.LUN property
        override _.LUN : LUN_T =
            m_LUN

        // --------------------------------------------------------------------
        // Implementation of ILUNode.LUName property
        override _.LUName : string =
            m_LUName

        // --------------------------------------------------------------------
        // Implementation of ILUNode.LUConfData property
        override _.LUConfData : TargetGroupConf.T_LogicalUnit =
            {
                LUN = m_LUN;
                LUName = "";
                WorkPath = "";
                LUDevice = TargetGroupConf.T_DEVICE.U_DummyDevice();
            }

    //=========================================================================
    // static method

    /// <summary>
    ///  Generate default LUN.
    /// </summary>
    /// <param name="v">
    ///  Already exists LU nodes.
    /// </param>
    /// <returns>
    ///  Generated LUN.
    /// </returns>
    static member GenDefaultLUN ( v : ILUNode seq ) : LUN_T =
        v
        |> Seq.map _.LUN
        |> Seq.toArray
        |> Functions.GenUniqueNumber ( (+) 1UL<lun_me> >> max ( lun_me.fromPrim 1UL ) ) ( lun_me.fromPrim 1UL )

    //=========================================================================
    // public method

    /// <summary>
    /// Create new object that has updated configuration values.
    /// </summary>
    /// <param name="argLUN">
    ///  Logical unit number.
    /// </param>
    /// <param name="argLUName">
    ///  Logical unit name.
    /// </param>
    /// <returns>
    ///  Created new node that has specified new values and same node ID.
    /// </returns>
    member _.CreateUpdatedNode 
        ( argLUN : LUN_T ) 
        ( argLUName : string ) : ConfNode_DummyDeviceLU =
        new ConfNode_DummyDeviceLU( m_MessageTable, m_ConfNodes, m_NodeID, argLUN, argLUName )

