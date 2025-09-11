//=============================================================================
// Haruka Software Storage.
// ConfNode_DummyMedia.fs : It represents configurations of Dummy media.
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
/// <param name="m_Name">
///  Media name.
/// </param>
/// <param name="m_IdentNumber">
///  Media node id.
/// </param>
type ConfNode_DummyMedia(
        m_MessageTable : StringTable,
        m_ConfNodes : ConfNodeRelation,
        m_NodeID : CONFNODE_T,
        m_IdentNumber : MEDIAIDX_T,
        m_Name : string
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
        let tgid = Functions.SearchAndConvert d "ID" ( UInt32.Parse >> mediaidx_me.fromPrim ) mediaidx_me.zero
        let name = Functions.SearchAndConvert d "MediaName" id ""
        new ConfNode_DummyMedia( argMessageTable, argConfNodes, newNodeID, tgid, name )

    //=========================================================================
    // Interface method

    interface IMediaNode with
        
        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.Validate
        override this.Validate ( msgList : ( CONFNODE_T * string ) list ) : ( CONFNODE_T * string ) list =
            let childNodes = m_ConfNodes.GetChildNodeList<IConfigureNode> m_NodeID
            let parentNoded = m_ConfNodes.GetParentNodeList<IConfigureNode> m_NodeID
            let curID = ( this :> IConfigureNode ).NodeID
            let curNodeTypeName = ( this :> IConfigureNode ).NodeTypeName

            msgList
            |> ( fun argmsg ->
                if m_IdentNumber = mediaidx_me.zero then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_MEDIA_ID_VALUE" ) 
                    ( curID, msg ) :: argmsg
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
                childNodes
                |> List.fold ( fun wMsgList itr ->
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_RELATION", itr.NodeTypeName, ( this :> IConfigureNode ).NodeTypeName )
                    ( itr.NodeID, msg ) :: wMsgList
                ) argmsg
            )

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeID
        override _.NodeID : CONFNODE_T =
            m_NodeID

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeTypeName
        override _.NodeTypeName : string =
            ClientConst.NODE_TYPE_NAME_DummyMedia

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
            if m_Name.Length > 0 then
                m_Name
            else
                ClientConst.NODE_TYPE_NAME_DummyMedia

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.ShortDescriptString property
        override this.ShortDescriptString : string = 
            sprintf "%s : ID=%d : Name=%s" ( this :> IMediaNode ).NodeTypeName m_IdentNumber m_Name

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.FullDescriptString property
        override this.FullDescriptString : string list =
            [
                yield sprintf "Node type : %s" ( this :> IMediaNode ).NodeTypeName
                yield sprintf "  ID(uint32)    : %d" m_IdentNumber
                yield sprintf "  MediaName(string)    : %s" m_Name
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.SortKey =
            [
                ClientConst.SORT_KEY_TYPE_DummyMedia;
                m_Name;
                sprintf "%08X" m_IdentNumber;
                sprintf "%016X" m_NodeID;
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.TempExportData : TempExport.T_Node =
            {
                TypeName = ClientConst.TEMPEXP_NN_DummyMedia;
                NodeID = confnode_me.toPrim m_NodeID;
                Values = [
                    yield {
                        Name = "ID";
                        Value = sprintf "%d" m_IdentNumber;
                    }
                    yield {
                        Name = "MediaName";
                        Value = m_Name;
                    }
                ]
            }

        // --------------------------------------------------------------------
        // Implementation of IMediaNode.MediaConfData property
        override _.MediaConfData : TargetGroupConf.T_MEDIA =
            TargetGroupConf.T_MEDIA.U_DummyMedia( {
                IdentNumber = m_IdentNumber;
                MediaName = m_Name;
            })

        // --------------------------------------------------------------------
        // Implementation of IMediaNode.MediaConfData property
        override _.IdentNumber : MEDIAIDX_T =
            m_IdentNumber

        // --------------------------------------------------------------------
        // Implementation of IMediaNode.Name property
        override _.Name : string =
            m_Name

    //=========================================================================
    // static method

    /// <summary>
    ///  Generate new media node ID
    /// </summary>
    /// <param name="v">
    ///  Already exists media nodes.
    /// </param>
    /// <returns>
    ///  Media node ID.
    /// </returns>
    static member GenNewID ( v : IMediaNode seq ) : MEDIAIDX_T =
        v
        |> Seq.map _.IdentNumber
        |> Seq.toArray
        |> Functions.GenUniqueNumber ( fun i -> i + 1u<mediaidx_me> |> max 1u<mediaidx_me> ) ( mediaidx_me.fromPrim 1u )

    //=========================================================================
    // public method

    /// <summary>
    /// Create new object that has updated configuration values.
    /// </summary>
    /// <returns>
    ///  Created new node that has specified new values and same node ID.
    /// </returns>
    member _.CreateUpdatedNode ( argID : MEDIAIDX_T ) ( argName : string ) : ConfNode_DummyMedia =
        new ConfNode_DummyMedia( m_MessageTable, m_ConfNodes, m_NodeID, argID, argName )
