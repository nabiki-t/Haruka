//=============================================================================
// Haruka Software Storage.
// Conf_NetworkPortal.fs : It represents configurations of Network Portal.
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
///  Target device node used at the configurations.
/// </summary>
/// <param name="m_MessageTable">
///  Message resource reader.
/// </param>
/// <param name="m_ConfNodes">
///  Configuration nodes holder.
/// </param>
/// <param name="argNodeID">
///  Node ID of this node.
/// </param>
/// <param name="m_NetworkPortal">
///  Configuration values.
/// </param>
type ConfNode_NetworkPortal(
        m_MessageTable : StringTable,
        m_ConfNodes : ConfNodeRelation,
        m_NodeID : CONFNODE_T,
        m_NetworkPortal: TargetDeviceConf.T_NetworkPortal
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
    ///  Node ID should be set this node.
    /// </param>
    /// <param name="newNPID">
    ///  Network portal ID should be set this node.
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
        let conf : TargetDeviceConf.T_NetworkPortal = {
            IdentNumber = Functions.SearchAndConvert d "ID" ( UInt32.Parse >> netportidx_me.fromPrim ) netportidx_me.zero;
            TargetPortalGroupTag = Functions.SearchAndConvert d "TPGT" ( UInt16.Parse >> tpgt_me.fromPrim ) tpgt_me.zero;
            TargetAddress = Functions.SearchAndConvert d "TargetAddress" id "";
            PortNumber = Functions.SearchAndConvert d "PortNumber" UInt16.Parse Constants.DEFAULT_ISCSI_PORT_NUM;
            DisableNagle = Functions.SearchAndConvert d "DisableNagle" bool.Parse Constants.DEF_DISABLE_NAGLE_IN_NP;
            ReceiveBufferSize = Functions.SearchAndConvert d "ReceiveBufferSize" Int32.Parse Constants.DEF_RECEIVE_BUFFER_SIZE_IN_NP;
            SendBufferSize = Functions.SearchAndConvert d "SendBufferSize" Int32.Parse Constants.DEF_SEND_BUFFER_SIZE_IN_NP;
            WhiteList =
                Functions.SearchAndConvert d "WhiteList" ( ( fun s -> s.Split "\t" ) >> Seq.map IPCondition.Parse >> Seq.toList ) [];
        }
        new ConfNode_NetworkPortal( argMessageTable, argConfNodes, newNodeID, conf )

    //=========================================================================
    // Interface method

    /// <inheritdoc />
    interface IConfigureNode with
        
        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.Validate
        override this.Validate ( msgList : ( CONFNODE_T * string ) list ) : ( CONFNODE_T * string ) list =
            let childNodes = m_ConfNodes.GetChild m_NodeID |> Seq.toArray
            let parentNoded = m_ConfNodes.GetParentNodeList<IConfigureNode> m_NodeID
            let curID = ( this :> IConfigureNode ).NodeID
            let curNodeTypeName = ( this :> IConfigureNode ).NodeTypeName

            msgList
            |> ( fun argmsg ->
                if m_NetworkPortal.TargetPortalGroupTag <> tpgt_me.zero then
                    ( m_NodeID, m_MessageTable.GetMessage( "CHKMSG_UNSUPPORTED_TPGT_VALUE" ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_NetworkPortal.TargetAddress
                let maxr = Constants.MAX_TARGET_ADDRESS_STR_LENGTH
                if maxr < v.Length then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_TARGET_ADDRESS_TOO_LONG", maxr.ToString() )
                    ( m_NodeID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_NetworkPortal.PortNumber
                if v < 1us then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_PORT_NUMBER", v.ToString() )
                    ( m_NodeID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_NetworkPortal.ReceiveBufferSize
                if v < 0 then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_RECEIVE_BUFFER_SIZE" )
                    ( m_NodeID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_NetworkPortal.SendBufferSize
                if v < 0 then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_SEND_BUFFER_SIZE" )
                    ( m_NodeID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_NetworkPortal.WhiteList.Length
                if v > Constants.MAX_IP_WHITELIST_COUNT then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_IP_WHITELIST_TOO_LONG", ( sprintf "%d" Constants.MAX_IP_WHITELIST_COUNT ) )
                    ( m_NodeID, msg ) :: argmsg
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
                |> Array.fold ( fun wMsgList itr ->
                    let node = m_ConfNodes.GetNode itr
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_RELATION", node.NodeTypeName, ( this :> IConfigureNode ).NodeTypeName )
                    ( itr, msg ) :: wMsgList
                ) argmsg
            )

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeID
        override _.NodeID : CONFNODE_T =
            m_NodeID

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeTypeName
        override _.NodeTypeName : string =
            ClientConst.NODE_TYPE_NAME_NetworkPortal

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
            if m_NetworkPortal.TargetAddress.Length = 0 then
                sprintf "PortNo=%d" m_NetworkPortal.PortNumber
            else
                sprintf "%s : %d" m_NetworkPortal.TargetAddress m_NetworkPortal.PortNumber

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.ShortDescriptString property
        override this.ShortDescriptString : string =
            sprintf "%s : ID=%d : Address=%s : port=%d"
                ( this :> IConfigureNode ).NodeTypeName
                m_NetworkPortal.IdentNumber
                m_NetworkPortal.TargetAddress
                m_NetworkPortal.PortNumber

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.FullDescriptString property
        override this.FullDescriptString : string list =
            [
                yield sprintf "Node type : %s" ( this :> IConfigureNode ).NodeTypeName
                yield         "Values :"
                yield sprintf "  ID(uint32) : %d" m_NetworkPortal.IdentNumber
                yield sprintf "  TPGT(uint16) : %d" m_NetworkPortal.TargetPortalGroupTag
                yield sprintf "  TargetAddress(string) : %s" m_NetworkPortal.TargetAddress
                yield sprintf "  PortNumber(uint16) : %d" m_NetworkPortal.PortNumber
                yield sprintf "  DisableNagle(bool) : %b" m_NetworkPortal.DisableNagle
                yield sprintf "  ReceiveBufferSize(int) : %d" m_NetworkPortal.ReceiveBufferSize
                yield sprintf "  SendBufferSize(int) : %d" m_NetworkPortal.SendBufferSize
                yield sprintf "  WhiteList : "
                for cond in m_NetworkPortal.WhiteList do
                    yield sprintf "    %s " ( IPCondition.ToString cond )
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.SortKey =
            [
                ClientConst.SORT_KEY_TYPE_NetworkPortal;
                m_NetworkPortal.TargetAddress;
                sprintf "%04X" m_NetworkPortal.PortNumber;
                sprintf "%08X" m_NetworkPortal.IdentNumber;
                sprintf "%016X" m_NodeID;
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override this.TempExportData : TempExport.T_Node =
            {
                TypeName = ClientConst.TEMPEXP_NN_NetworkPortal;
                NodeID = confnode_me.toPrim m_NodeID;
                Values = [
                    yield {
                        Name = "ID";
                        Value = sprintf "%d" m_NetworkPortal.IdentNumber;
                    };
                    yield {
                        Name = "TPGT";
                        Value = sprintf "%d" m_NetworkPortal.TargetPortalGroupTag;
                    };
                    yield {
                        Name = "TargetAddress";
                        Value = m_NetworkPortal.TargetAddress;
                    };
                    yield {
                        Name = "PortNumber";
                        Value = sprintf "%d" m_NetworkPortal.PortNumber;
                    };
                    yield {
                        Name = "DisableNagle";
                        Value = sprintf "%b" m_NetworkPortal.DisableNagle;
                    };
                    yield {
                        Name = "ReceiveBufferSize";
                        Value = sprintf "%d" m_NetworkPortal.ReceiveBufferSize;
                    };
                    yield {
                        Name = "SendBufferSize";
                        Value = sprintf "%d" m_NetworkPortal.SendBufferSize;
                    };
                    yield {
                        Name = "WhiteList";
                        Value = 
                            m_NetworkPortal.WhiteList
                            |> Seq.map IPCondition.ToString
                            |> String.concat "\t" ;
                    };
                ]
            }
    //=========================================================================
    // static method

    /// <summary>
    ///  Generate new ident number
    /// </summary>
    /// <param name="v">
    ///  Already exists network portal nodes.
    /// </param>
    /// <returns>
    ///  Ident number for network portal node.
    /// </returns>
    static member GenNewID ( v : ConfNode_NetworkPortal seq ) : NETPORTIDX_T =
        v
        |> Seq.map _.NetworkPortal.IdentNumber
        |> Seq.toArray
        |> Functions.GenUniqueNumber ( (+) 1u<netportidx_me> ) ( netportidx_me.fromPrim 0u )

    //=========================================================================
    // public method

    /// m_NetworkPortal value property
    member _.NetworkPortal : TargetDeviceConf.T_NetworkPortal =
        m_NetworkPortal

    /// <summary>
    /// Create new object that has updated configuration values.
    /// </summary>
    /// <param name="argNetworkPortal">
    ///  Configuration values.
    /// </param>
    /// <returns>
    ///  Created new node that has specified new values and same node ID.
    /// </returns>
    member _.CreateUpdatedNode 
        ( argNetworkPortal: TargetDeviceConf.T_NetworkPortal ) : ConfNode_NetworkPortal = 
        new ConfNode_NetworkPortal( m_MessageTable, m_ConfNodes, m_NodeID, argNetworkPortal )
