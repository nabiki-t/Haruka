//=============================================================================
// Haruka Software Storage.
// ConfNode_PlainFileMedia.fs : It represents configurations of plain file media.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Client

//=============================================================================
// Import declaration

open System
open System.IO
open System.Collections.Generic

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
/// <param name="m_Value">
///  Configuration Values.
/// </param>
type ConfNode_PlainFileMedia(
        m_MessageTable : StringTable,
        m_ConfNodes : ConfNodeRelation,
        m_NodeID : CONFNODE_T,
        m_Value : TargetGroupConf.T_PlainFile
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
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = Functions.SearchAndConvert d "ID" ( UInt32.Parse >> mediaidx_me.fromPrim ) mediaidx_me.zero;
            MediaName = Functions.SearchAndConvert d "MediaName" id "";
            FileName = Functions.SearchAndConvert d "FileName" id "";
            MaxMultiplicity = Functions.SearchAndConvert d "MaxMultiplicity" UInt32.Parse 0u;
            QueueWaitTimeOut = Functions.SearchAndConvert d "QueueWaitTimeOut" Int32.Parse 0;
            WriteProtect = Functions.SearchAndConvert d "WriteProtect" bool.Parse false;
        }
        new ConfNode_PlainFileMedia( argMessageTable, argConfNodes, newNodeID, conf )

    //=========================================================================
    // Interface method

    /// <inheritdoc />
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
                if m_Value.IdentNumber = mediaidx_me.zero then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_MEDIA_ID_VALUE" ) 
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_Value.FileName.Length
                let maxr = Constants.MAX_FILENAME_STR_LENGTH
                if v < 1 || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_FILE_NAME_LENGTH", maxr.ToString() ) 
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_Value.MaxMultiplicity
                let minr = Constants.PLAINFILE_MIN_MAXMULTIPLICITY
                let maxr = Constants.PLAINFILE_MAX_MAXMULTIPLICITY
                if v < minr || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_MAXMULTIPLICITY", v.ToString(), minr.ToString(), maxr.ToString() ) 
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_Value.QueueWaitTimeOut
                let minr = Constants.PLAINFILE_MIN_QUEUEWAITTIMEOUT
                let maxr = Constants.PLAINFILE_MAX_QUEUEWAITTIMEOUT
                if v < minr || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_QUEUEWAITTIMEOUT", v.ToString(), minr.ToString(), maxr.ToString() ) 
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
            ClientConst.NODE_TYPE_NAME_PlainFileMedia

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
            if m_Value.MediaName.Length > 0 then
                m_Value.MediaName
            else
                Path.GetFileName m_Value.FileName

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.ShortDescriptString property
        override this.ShortDescriptString : string =
            sprintf "%s : File=%s" ( this :> IMediaNode ).NodeTypeName m_Value.FileName

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.FullDescriptString property
        override this.FullDescriptString : string list =
            [
                yield sprintf "Node type : %s" ( this :> IMediaNode ).NodeTypeName
                yield         "Values :"
                yield sprintf "  ID(uint32) : %d" m_Value.IdentNumber
                yield sprintf "  MediaName(string) : %s" m_Value.MediaName
                yield sprintf "  FileName(string) : %s" m_Value.FileName
                yield sprintf "  MaxMultiplicity(uint32) : %d" m_Value.MaxMultiplicity
                yield sprintf "  QueueWaitTimeOut(int) : %d" m_Value.QueueWaitTimeOut
                yield sprintf "  WriteProtect(bool) : %b" m_Value.WriteProtect
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.SortKey =
            [
                ClientConst.SORT_KEY_TYPE_PlainFileMedia;
                m_Value.MediaName;
                sprintf "%s" m_Value.FileName;
                sprintf "%08X" m_Value.IdentNumber;
                sprintf "%016X" m_NodeID;
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.TempExportData : TempExport.T_Node =
            {
                TypeName = ClientConst.TEMPEXP_NN_PlainFileMedia;
                NodeID = confnode_me.toPrim m_NodeID;
                Values = [
                    yield {
                        Name = "ID";
                        Value = sprintf "%d" m_Value.IdentNumber;
                    }
                    yield {
                        Name = "MediaName";
                        Value = m_Value.MediaName;
                    }
                    yield {
                        Name = "FileName";
                        Value = m_Value.FileName;
                    }
                    yield {
                        Name = "MaxMultiplicity";
                        Value = sprintf "%d" m_Value.MaxMultiplicity;
                    }
                    yield {
                        Name = "QueueWaitTimeOut";
                        Value = sprintf "%d" m_Value.QueueWaitTimeOut;
                    }
                    yield {
                        Name = "WriteProtect";
                        Value = sprintf "%b" m_Value.WriteProtect;
                    }
                ]
            }

        // --------------------------------------------------------------------
        // Implementation of IMediaNode.MediaConfData property
        override  _.MediaConfData : TargetGroupConf.T_MEDIA =
            TargetGroupConf.T_MEDIA.U_PlainFile( m_Value )

        // --------------------------------------------------------------------
        // Implementation of IMediaNode.MediaConfData property
        override _.IdentNumber : MEDIAIDX_T =
            m_Value.IdentNumber

        // --------------------------------------------------------------------
        // Implementation of IMediaNode.Name property
        override _.Name : string =
            m_Value.MediaName

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
    /// <param name="argValue">
    ///  Configuration Values.
    /// </param>
    /// <returns>
    ///  Created new node that has specified new values and same node ID.
    /// </returns>
    member _.CreateUpdatedNode 
        ( argValue : TargetGroupConf.T_PlainFile ) : ConfNode_PlainFileMedia =
        new ConfNode_PlainFileMedia( m_MessageTable, m_ConfNodes, m_NodeID, argValue )

    member _.Values = m_Value
