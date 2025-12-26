//=============================================================================
// Haruka Software Storage.
// ConfNode_BlockDeviceLU.fs : It represents configurations of Block device logical unit.
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
/// <param name="m_LUN">
///  Logical unit number.
/// </param>
/// <param name="m_LUName">
///  Logical unit name.
/// </param>
/// <param name="m_MaxMultiplicity">
///  Number of concurrent SCSI tasks within a LU.
/// </param>
type ConfNode_BlockDeviceLU(
        m_MessageTable : StringTable,
        m_ConfNodes : ConfNodeRelation,
        m_NodeID : CONFNODE_T,
        m_LUN : LUN_T,
        m_LUName : string,
        m_MaxMultiplicity : uint32
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
        let maxMultiplicity : uint32 = Functions.SearchAndConvert d "MaxMultiplicity" UInt32.Parse Constants.LU_DEF_MULTIPLICITY
        new ConfNode_BlockDeviceLU( argMessageTable, argConfNodes, newNodeID, lun, name, maxMultiplicity )

    //=========================================================================
    // Interface method

    interface ILUNode with
        
        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.Validate
        override this.Validate ( msgList : ( CONFNODE_T * string ) list ) : ( CONFNODE_T * string ) list =
            let curID = ( this :> IConfigureNode ).NodeID
            let childNodes = m_ConfNodes.GetChildNodeList<IConfigureNode> m_NodeID
            let childNodes_ValidMedia, childNodes_OtherNode =
                childNodes
                |> List.partition ( function :? IMediaNode -> true | _ -> false )
            let allTargetGroupNodes =
                m_ConfNodes.GetAllParentNodeList<ConfNode_TargetGroup> m_NodeID
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
                if m_MaxMultiplicity < Constants.LU_MIN_MULTIPLICITY || m_MaxMultiplicity > Constants.LU_MAX_MULTIPLICITY then
                    let vs = sprintf "%d" m_MaxMultiplicity
                    let mins = sprintf "%d" Constants.LU_MIN_MULTIPLICITY
                    let maxs = sprintf "%d" Constants.LU_MAX_MULTIPLICITY
                    ( m_NodeID, m_MessageTable.GetMessage( "CHKMSG_INVALID_LU_MAXMULTIPLICITY", vs, mins, maxs ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                if 1 < childNodes_ValidMedia.Length then
                    ( curID, m_MessageTable.GetMessage( "CHKMSG_OUT_OF_MEDIA_COUNT", "1" ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                if childNodes_ValidMedia.Length <= 0 then
                    ( curID, m_MessageTable.GetMessage( "CHKMSG_MISSING_MEDIA" ) ) :: argmsg
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
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_RELATION", itr.NodeTypeName, ( this :> IConfigureNode ).NodeTypeName )
                    ( itr.NodeID, msg ) :: wMsgList
                ) argmsg
            )
            |> ( fun argmsg ->
                childNodes_ValidMedia
                |> List.fold ( fun wMsgList itr -> itr.Validate wMsgList ) argmsg
            )

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeID property
        override _.NodeID : CONFNODE_T =
            m_NodeID

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeTypeName property
        override _.NodeTypeName : string =
            ClientConst.NODE_TYPE_NAME_BlockDeviceLU

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
                yield sprintf "  LUN  : %s" ( lun_me.toString m_LUN )
                yield sprintf "  Name(string)  : %s" m_LUName
                yield sprintf "  MaxMultiplicity(uint32) : %d" m_MaxMultiplicity
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.SortKey =
            [
                ClientConst.SORT_KEY_TYPE_BlockDeviceLU;
                m_LUName;
                ( lun_me.toString m_LUN );
                sprintf "%016X" m_NodeID;
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.TempExportData : TempExport.T_Node =
            {
                TypeName = ClientConst.TEMPEXP_NN_BlockDeviceLU;
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
                    yield {
                        Name = "MaxMultiplicity";
                        Value = sprintf "%d" m_MaxMultiplicity;
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
        // Implementation of ILUNode.MaxMultiplicity property
        override _.MaxMultiplicity : uint32 =
            m_MaxMultiplicity

        // --------------------------------------------------------------------
        // Implementation of ILUNode.LUConfData property
        override _.LUConfData : TargetGroupConf.T_LogicalUnit =
            let childNodes = m_ConfNodes.GetChildNodeList<IConfigureNode> m_NodeID      // child media node must be only 1
            if childNodes.Length <> 1 then
                raise <| Exception( "Unexpected error. ConfNode_BlockDeviceLU must have only one child node." )
            let media : TargetGroupConf.T_MEDIA=
                match childNodes.Head with
                | :? IMediaNode as x ->
                    x.MediaConfData
                | _ ->
                    raise <| Exception( "Unexpected error. ConfNode_BlockDeviceLU must have media node." )
            {
                LUN = m_LUN;
                LUName = m_LUName;
                WorkPath = "";
                MaxMultiplicity = m_MaxMultiplicity;
                LUDevice = TargetGroupConf.T_DEVICE.U_BlockDevice( { Peripheral = media; } );
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
    /// <param name="argMaxMultiplicity">
    ///  Number of concurrent SCSI tasks within a LU.
    /// </param>
    /// <returns>
    ///  Created new node that has specified new values and same node ID.
    /// </returns>
    member _.CreateUpdatedNode 
        ( argLUN : LUN_T )
        ( argLUName : string ) 
        ( argMaxMultiplicity : uint32 ) : ConfNode_BlockDeviceLU =
        new ConfNode_BlockDeviceLU( m_MessageTable, m_ConfNodes, m_NodeID, argLUN, argLUName, argMaxMultiplicity )
