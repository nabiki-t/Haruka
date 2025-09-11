//=============================================================================
// Haruka Software Storage.
// ConfNode_TargetDevice.fs : It represents configurations of Target device.
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
///  Target device node used at the configurations.
/// </summary>
/// <param name="m_MessageTable">
///  Message resource reader.
/// </param>
/// <param name="m_ConfNodes">
///  Configuration nodes holder.
/// </param>
/// <param name="m_TargetDeviceID">
///  Target device ID.
/// </param>
/// <param name="m_TargetDeviceName">
///  Target device name.
/// </param>
/// <param name="m_NodeID">
///  Node ID of this node.
/// </param>
/// <param name="m_NegotiableParameters">
///  Configuration values.
/// </param>
/// <param name="m_LogParameters">
///  Configuration values.
/// </param>
/// <param name="m_Modified">
///  Values are modified or not.
/// </param>
type ConfNode_TargetDevice(
        m_MessageTable : StringTable,
        m_ConfNodes : ConfNodeRelation,
        m_NodeID : CONFNODE_T,
        m_TargetDeviceID : TDID_T,
        m_TargetDeviceName : string,
        m_NegotiableParameters : TargetDeviceConf.T_NegotiableParameters,
        m_LogParameters : TargetDeviceConf.T_LogParameters,
        m_Modified : ModifiedStatus
    ) =

    /// <summary>
    ///  Create ConfNode_TargetDevice instance from configuration values that is gotton from controller.
    /// </summary>
    /// <param name="argMessageTable">
    ///  Message resource reader.
    /// </param>
    /// <param name="argConfNodes">
    ///  Configuration nodes holder.
    /// </param>
    /// <param name="argTargetDeviceID">
    ///  Target device ID.
    /// </param>
    /// <param name="argNodeID">
    ///  Node ID of this node.
    /// </param>
    /// <param name="argValue">
    ///  Configuration values.
    /// </param>
    /// <remarks>
    ///  Modified flag is set to "NotModified". argValue.NetworkPortal values are ignored.
    /// </remarks>
    new (
        argMessageTable : StringTable, 
        argConfNodes : ConfNodeRelation,
        argNodeID : CONFNODE_T,
        argTargetDeviceID : TDID_T,
        argValue : TargetDeviceConf.T_TargetDevice
    ) =
        let np : TargetDeviceConf.T_NegotiableParameters =
            Option.defaultValue
                {
                    MaxRecvDataSegmentLength = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                    MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength;
                    FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength;
                    DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait;
                    DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain;
                    MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
                }
                argValue.NegotiableParameters
        let lp : TargetDeviceConf.T_LogParameters = 
            Option.defaultValue
                {
                    SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
                    HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
                    LogLevel = LogLevel.LOGLEVEL_INFO;
                }
                argValue.LogParameters

        new ConfNode_TargetDevice( argMessageTable, argConfNodes, argNodeID, argTargetDeviceID, argValue.DeviceName, np, lp, ModifiedStatus.NotModified )

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
        let tdid = Functions.SearchAndConvert d "ID" tdid_me.fromString tdid_me.Zero;
        let tdname = Functions.SearchAndConvert d "Name" id "";
        let np : TargetDeviceConf.T_NegotiableParameters = {
            MaxRecvDataSegmentLength = Functions.SearchAndConvert d "NegotiableParameters.MaxRecvDataSegmentLength" UInt32.Parse Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
            MaxBurstLength = Functions.SearchAndConvert d "NegotiableParameters.MaxBurstLength" UInt32.Parse Constants.NEGOPARAM_DEF_MaxBurstLength;
            FirstBurstLength = Functions.SearchAndConvert d "NegotiableParameters.FirstBurstLength" UInt32.Parse Constants.NEGOPARAM_DEF_FirstBurstLength;
            DefaultTime2Wait = Functions.SearchAndConvert d "NegotiableParameters.DefaultTime2Wait" UInt16.Parse Constants.NEGOPARAM_DEF_DefaultTime2Wait;
            DefaultTime2Retain = Functions.SearchAndConvert d "NegotiableParameters.DefaultTime2Retain" UInt16.Parse Constants.NEGOPARAM_DEF_DefaultTime2Retain;
            MaxOutstandingR2T = Functions.SearchAndConvert d "NegotiableParameters.MaxOutstandingR2T" UInt16.Parse Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
        }
        let lp : TargetDeviceConf.T_LogParameters = {
            SoftLimit = Functions.SearchAndConvert d "LogParameters.SoftLimit" UInt32.Parse Constants.LOGPARAM_DEF_SOFTLIMIT;
            HardLimit = Functions.SearchAndConvert d "LogParameters.HardLimit" UInt32.Parse Constants.LOGPARAM_DEF_HARDLIMIT;
            LogLevel = Functions.SearchAndConvert d "LogParameters.LogLevel" LogLevel.fromString LogLevel.LOGLEVEL_INFO;
        }
        new ConfNode_TargetDevice( argMessageTable, argConfNodes, newNodeID, tdid, tdname, np, lp, ModifiedStatus.Modified )

    //=========================================================================
    // Interface method

    interface IConfigFileNode with
        
        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.Validate
        override this.Validate ( msgList : ( CONFNODE_T * string ) list ) : ( CONFNODE_T * string ) list =
            let curID = ( this :> IConfigureNode ).NodeID
            let childNodes_NetworkPortal = m_ConfNodes.GetChildNodeList<ConfNode_NetworkPortal> m_NodeID
            let childNodes_TargetGroup = m_ConfNodes.GetChildNodeList<ConfNode_TargetGroup> m_NodeID
            let childNodes_OtherNode =
                m_ConfNodes.GetChildNodeList<IConfigureNode> m_NodeID
                |> List.filter ( function :? ConfNode_TargetGroup -> false | :? ConfNode_NetworkPortal -> false | _ -> true )
            let grandchildNodes_Target =
                childNodes_TargetGroup
                |> Seq.map ( fun itr -> ( itr :> IConfigureNode ).GetChildNodes<ConfNode_Target>() )
                |> Seq.toArray
            let grandchildNodes_LU =
                childNodes_TargetGroup
                |> Seq.map _.GetAccessibleLUNodes()
                |> Seq.toArray
            let grandchildNodes_Media =
                childNodes_TargetGroup
                |> Seq.map ( fun itr -> ( itr :> IConfigureNode ).GetDescendantNodes<IMediaNode>() )
                |> Seq.concat
                |> Seq.toArray
            let parentNodes = m_ConfNodes.GetParentNodeList<IConfigureNode> m_NodeID
            let curNodeTypeName = ( this :> IConfigureNode ).NodeTypeName

            msgList
            |> ( fun argmsg ->
                let v = m_NegotiableParameters.MaxRecvDataSegmentLength
                let minr = Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength
                let maxr = Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength
                if v < minr || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_MAXRECVDATASEGMENTLENGTH", v.ToString(), minr.ToString(), maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_NegotiableParameters.MaxBurstLength
                let minr = Constants.NEGOPARAM_MIN_MaxBurstLength
                let maxr = Constants.NEGOPARAM_MAX_MaxBurstLength
                if v < minr || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_MAXBURSTLENGTH", v.ToString(), minr.ToString(), maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_NegotiableParameters.FirstBurstLength
                let minr = Constants.NEGOPARAM_MIN_FirstBurstLength
                let maxr = Constants.NEGOPARAM_MAX_FirstBurstLength
                if v < minr || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_FIRSTBURSTLENGTH", v.ToString(), minr.ToString(), maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_NegotiableParameters.DefaultTime2Wait
                let minr = Constants.NEGOPARAM_MIN_DefaultTime2Wait
                let maxr = Constants.NEGOPARAM_MAX_DefaultTime2Wait
                if v < minr || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_DEFAULTTIME2WAIT", v.ToString(), minr.ToString(), maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_NegotiableParameters.DefaultTime2Retain
                let minr = Constants.NEGOPARAM_MIN_DefaultTime2Retain
                let maxr = Constants.NEGOPARAM_MAX_DefaultTime2Retain
                if v < minr || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_DEFAULTTIME2RETAIN", v.ToString(), minr.ToString(), maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_NegotiableParameters.MaxOutstandingR2T
                let minr = Constants.NEGOPARAM_MIN_MaxOutstandingR2T
                let maxr = Constants.NEGOPARAM_MAX_MaxOutstandingR2T
                if v < minr || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_MAXOUTSTANDINGR2T", v.ToString(), minr.ToString(), maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_LogParameters.SoftLimit
                let minr = Constants.LOGPARAM_MIN_SOFTLIMIT
                let maxr = Constants.LOGPARAM_MAX_SOFTLIMIT
                if v < minr || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_LOGPARAM_SOFTLIMIT", v.ToString(), minr.ToString(), maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = m_LogParameters.HardLimit
                let minr = Constants.LOGPARAM_MIN_HARDLIMIT
                let maxr = Constants.LOGPARAM_MAX_HARDLIMIT
                if v < minr || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_LOGPARAM_HARDLIMIT", v.ToString(), minr.ToString(), maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let vh = m_LogParameters.HardLimit
                let vs = m_LogParameters.SoftLimit
                if vh < vs then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_LOGPARAM_HARDLIMIT_LESS_THAN_SOFTLIMIT", vh.ToString(), vs.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let maxr = Constants.MAX_DEVICE_NAME_STR_LENGTH
                if maxr < m_TargetDeviceName.Length then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_TARGET_DEVICE_NAME_TOO_LONG", maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let maxr = Constants.MAX_NETWORK_PORTAL_COUNT
                if maxr < childNodes_NetworkPortal.Length then
                    ( curID, m_MessageTable.GetMessage( "CHKMSG_OUT_OF_NETWORK_PORTAL_COUNT", maxr.ToString() ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                if childNodes_NetworkPortal.Length <= 0 then
                    ( curID, m_MessageTable.GetMessage( "CHKMSG_MISSING_NETWORK_PORTAL" ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let maxr = Constants.MAX_TARGET_GROUP_COUNT_IN_TD
                if maxr < childNodes_TargetGroup.Length then
                    ( curID, m_MessageTable.GetMessage( "CHKMSG_OUT_OF_TARGET_GROUP_COUNT", maxr.ToString() ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                if childNodes_TargetGroup.Length <= 0 then
                    ( curID, m_MessageTable.GetMessage( "CHKMSG_MISSING_TARGET_GROUP" ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let targetCount =
                    grandchildNodes_Target
                    |> Seq.map Seq.length
                    |> Seq.sum
                let maxr = Constants.MAX_TARGET_COUNT_IN_TD
                if maxr < targetCount then
                    ( curID, m_MessageTable.GetMessage( "CHKMSG_OUT_OF_TARGET_COUNT_IN_TD", maxr.ToString() ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let targetCount =
                    grandchildNodes_LU
                    |> Seq.map Seq.length
                    |> Seq.sum
                let maxr = Constants.MAX_LOGICALUNIT_COUNT_IN_TD
                if maxr < targetCount then
                    ( curID, m_MessageTable.GetMessage( "CHKMSG_OUT_OF_LU_COUNT_IN_TD", maxr.ToString() ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                childNodes_NetworkPortal
                |> Seq.countBy ( fun itr -> itr.NetworkPortal.IdentNumber )
                |> Seq.filter ( fun ( _, cnt ) -> cnt > 1 )
                |> Seq.map ( fun ( npid, _ ) -> childNodes_NetworkPortal |> Seq.filter ( fun itr -> itr.NetworkPortal.IdentNumber = npid ) )
                |> Seq.concat
                |> Seq.fold ( fun wMsgList itr ->
                        let npid = netportidx_me.toPrim itr.NetworkPortal.IdentNumber
                        let msg = m_MessageTable.GetMessage( "CHKMSG_DUPLICATE_NETWORK_PORTAL_ID", npid.ToString() )
                        ( ( itr :> IConfigureNode ).NodeID, msg ) :: wMsgList
                   ) argmsg
            )
            |> ( fun argmsg ->
                childNodes_TargetGroup
                |> Seq.countBy ( fun itr -> itr.TargetGroupID )
                |> Seq.filter ( fun ( _, cnt ) -> cnt > 1 )
                |> Seq.map ( fun ( tgid, _ ) -> childNodes_TargetGroup |> Seq.filter ( fun itr -> itr.TargetGroupID.Equals tgid ) )
                |> Seq.concat
                |> Seq.fold ( fun wMsgList itr ->
                        let tgid = itr.TargetGroupID
                        let msg = m_MessageTable.GetMessage( "CHKMSG_DUPLICATE_TARGET_GROUP_ID", tgid_me.toString tgid )
                        ( ( itr :> IConfigureNode ).NodeID, msg ) :: wMsgList
                   ) argmsg
            )
            |> ( fun argmsg ->
                let listTarget = grandchildNodes_Target |> Seq.concat
                listTarget
                |> Seq.countBy ( fun itr -> itr.Values.TargetName )
                |> Seq.filter ( fun ( _, cnt ) -> cnt > 1 )
                |> Seq.map ( fun ( tname, _ ) -> listTarget |> Seq.filter ( fun itr -> itr.Values.TargetName = tname ) )
                |> Seq.concat
                |> Seq.fold ( fun wMsgList itr ->
                        let tname = itr.Values.TargetName
                        let msg = m_MessageTable.GetMessage( "CHKMSG_DUPLICATE_TARGET_NAME", tname )
                        ( ( itr :> IConfigureNode ).NodeID, msg ) :: wMsgList
                   ) argmsg
            )
            |> ( fun argmsg ->
                let listTarget = grandchildNodes_Target |> Seq.concat
                listTarget
                |> Seq.countBy ( fun itr -> itr.Values.IdentNumber )
                |> Seq.filter ( fun ( _, cnt ) -> cnt > 1 )
                |> Seq.map ( fun ( tid, _ ) -> listTarget |> Seq.filter ( fun itr -> itr.Values.IdentNumber = tid ) )
                |> Seq.concat
                |> Seq.fold ( fun wMsgList itr ->
                        let tid = tnodeidx_me.toPrim itr.Values.IdentNumber
                        let msg = m_MessageTable.GetMessage( "CHKMSG_DUPLICATE_TARGET_ID", tid.ToString() )
                        ( ( itr :> IConfigureNode ).NodeID, msg ) :: wMsgList
                   ) argmsg
            )
            |> ( fun argmsg ->
                grandchildNodes_Media
                |> Seq.countBy ( fun itr -> itr.IdentNumber )
                |> Seq.filter ( snd >> (<) 1 )
                |> Seq.map ( fun ( midx, _ ) -> grandchildNodes_Media |> Seq.filter ( fun itr -> itr.IdentNumber = midx ) )
                |> Seq.concat
                |> Seq.fold ( fun wMsgList itr ->
                        let midx = itr.IdentNumber
                        let msg = m_MessageTable.GetMessage( "CHKMSG_DUPLICATE_MEDIA_ID", midx.ToString() )
                        ( ( itr :> IConfigureNode ).NodeID, msg ) :: wMsgList
                   ) argmsg
            )
            |> ( fun argmsg ->
                let lun_NodeID =
                    grandchildNodes_LU
                    |> Seq.concat
                    |> Seq.map ( fun itr -> ( itr.LUN, ( itr :> IConfigureNode ).NodeID ) )
                lun_NodeID
                |> Seq.countBy fst
                |> Seq.filter ( fun ( _, cnt ) -> cnt > 1 )
                |> Seq.map ( fun ( lun, _ ) -> lun_NodeID |> Seq.filter ( fst >> (=) lun ) )
                |> Seq.concat
                |> Seq.fold ( fun wMsgList ( lun, nodeid ) ->
                    let lunstr = lun_me.toString lun
                    let msg = m_MessageTable.GetMessage( "CHKMSG_DUPLICATE_LUN", lunstr )
                    ( nodeid, msg ) :: wMsgList
                   ) argmsg
            )
            |> ( fun argmsg ->
                if parentNodes.Length > 1 then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_TOO_MANY_PARENT", curNodeTypeName, "1" )
                    ( curID, msg ) :: argmsg
                elif parentNodes.Length < 1 then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_MISSING_PARENT", curNodeTypeName )
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
            |> ( fun argmsg ->
                childNodes_NetworkPortal
                |> Seq.fold ( fun wMsgList itr -> ( itr :> IConfigureNode ).Validate wMsgList ) argmsg
            )
            |> ( fun argmsg ->
                childNodes_TargetGroup
                |> Seq.fold ( fun wMsgList itr -> ( itr :> IConfigureNode ).Validate wMsgList ) argmsg
            )


        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeID
        override _.NodeID : CONFNODE_T =
            m_NodeID

        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.NodeTypeName
        override _.NodeTypeName : string =
            ClientConst.NODE_TYPE_NAME_TargetDevice

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
            if m_TargetDeviceName.Length > 0 then
                m_TargetDeviceName
            else
                tdid_me.toString m_TargetDeviceID

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.ShortDescriptString property
        override this.ShortDescriptString : string =
            sprintf "%s : ID=%s : Name=%s" ( this :> IConfigureNode ).NodeTypeName ( tdid_me.toString m_TargetDeviceID ) m_TargetDeviceName

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.FullDescriptString property
        override this.FullDescriptString : string list =
            [
                yield sprintf "Node type : %s" ( this :> IConfigureNode ).NodeTypeName
                yield         "Values :"
                yield sprintf "  ID(TargetDeviceID) : %s" ( tdid_me.toString m_TargetDeviceID )
                yield sprintf "  Name(string) : %s" m_TargetDeviceName
                yield sprintf "  NegotiableParameters :"
                yield sprintf "    MaxRecvDataSegmentLength(uint32) : %d" m_NegotiableParameters.MaxRecvDataSegmentLength
                yield sprintf "    MaxBurstLength(uint32) : %d" m_NegotiableParameters.MaxBurstLength
                yield sprintf "    FirstBurstLength(uint32) : %d" m_NegotiableParameters.FirstBurstLength
                yield sprintf "    DefaultTime2Wait(uint16) : %d" m_NegotiableParameters.DefaultTime2Wait
                yield sprintf "    DefaultTime2Retain(uint16) : %d" m_NegotiableParameters.DefaultTime2Retain
                yield sprintf "    MaxOutstandingR2T(uint16) : %d" m_NegotiableParameters.MaxOutstandingR2T
                yield sprintf "  LogParameters :"
                yield sprintf "    SoftLimit(uint32) : %d" m_LogParameters.SoftLimit
                yield sprintf "    HardLimit(uint32) : %d" m_LogParameters.HardLimit
                yield sprintf "    LogLevel : %s" ( LogLevel.toString m_LogParameters.LogLevel )
                yield sprintf "Modified : %b" ( m_Modified = ModifiedStatus.Modified )
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.SortKey =
            [
                ClientConst.SORT_KEY_TYPE_TargetDevice;
                m_TargetDeviceName;
                tdid_me.toString m_TargetDeviceID;
                sprintf "%016X" m_NodeID;
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.TempExportData : TempExport.T_Node =
            {
                TypeName = ClientConst.TEMPEXP_NN_TargetDevice;
                NodeID = confnode_me.toPrim m_NodeID;
                Values = [
                    yield {
                        Name = "ID";
                        Value = tdid_me.toString m_TargetDeviceID;
                    };
                    yield {
                        Name = "Name";
                        Value = m_TargetDeviceName;
                    };
                    yield {
                        Name = "NegotiableParameters.MaxRecvDataSegmentLength";
                        Value = sprintf "%d" m_NegotiableParameters.MaxRecvDataSegmentLength;
                    };
                    yield {
                        Name = "NegotiableParameters.MaxBurstLength";
                        Value = sprintf "%d" m_NegotiableParameters.MaxBurstLength;
                    };
                    yield {
                        Name = "NegotiableParameters.FirstBurstLength";
                        Value = sprintf "%d" m_NegotiableParameters.FirstBurstLength;
                    };
                    yield {
                        Name = "NegotiableParameters.DefaultTime2Wait";
                        Value = sprintf "%d" m_NegotiableParameters.DefaultTime2Wait;
                    };
                    yield {
                        Name = "NegotiableParameters.DefaultTime2Retain";
                        Value = sprintf "%d" m_NegotiableParameters.DefaultTime2Retain;
                    };
                    yield {
                        Name = "NegotiableParameters.MaxOutstandingR2T";
                        Value = sprintf "%d" m_NegotiableParameters.MaxOutstandingR2T;
                    };
                    yield {
                        Name = "LogParameters.SoftLimit";
                        Value = sprintf "%d" m_LogParameters.SoftLimit;
                    };
                    yield {
                        Name = "LogParameters.HardLimit";
                        Value = sprintf "%d" m_LogParameters.HardLimit;
                    };
                    yield {
                        Name = "LogParameters.LogLevel";
                        Value = LogLevel.toString m_LogParameters.LogLevel;
                    };
                ]
            }

        // ------------------------------------------------------------------------
        // Implementation of IConfigFileNode.IsModified property
        override _.Modified : ModifiedStatus =
            m_Modified
        
        // ------------------------------------------------------------------------
        // Implementation of IConfigFileNode.ResetModifiedFlag property
        override _.ResetModifiedFlag() : IConfigFileNode =
            new ConfNode_TargetDevice(
                m_MessageTable,
                m_ConfNodes,
                m_NodeID,
                m_TargetDeviceID,
                m_TargetDeviceName,
                m_NegotiableParameters,
                m_LogParameters,
                ModifiedStatus.NotModified
            )

    //=========================================================================
    // static method

    /// <summary>
    ///  Generate new target device ID
    /// </summary>
    /// <param name="v">
    ///  Already exists taget device nodes.
    /// </param>
    /// <returns>
    ///  Target device ID.
    /// </returns>
    static member GenNewTargetDeviceID ( v : ConfNode_TargetDevice seq ) : TDID_T =
        v
        |> Seq.map _.TargetDeviceID
        |> tdid_me.NewID 

    /// <summary>
    ///  Generate new target device name.
    /// </summary>
    /// <param name="v">
    ///  Already exists taget device nodes.
    /// </param>
    /// <returns>
    ///  Generated target device name.
    /// </returns>
    static member GenDefaultTargetDeviceName ( v : ConfNode_TargetDevice seq ) : string =
        let prefix = "TargetDevice_"
        v
        |> Seq.map _.TargetDeviceName
        |> Seq.filter _.StartsWith( prefix )
        |> Seq.map ( fun itr -> itr.[ prefix.Length .. ] )
        |> Seq.map UInt32.TryParse
        |> Seq.filter fst
        |> Seq.map snd
        |> Functions.GenUniqueNumber ( (+) 1u ) 0u
        |> sprintf "%s%05d" prefix

    //=========================================================================
    // public method

    /// TargetDeviceID value property
    member _.TargetDeviceID : TDID_T =
        m_TargetDeviceID

    /// TargetDeviceName value property
    member _.TargetDeviceName : string =
        m_TargetDeviceName

    /// NegotiableParameters value property
    member _.NegotiableParameters : TargetDeviceConf.T_NegotiableParameters =
        m_NegotiableParameters

    /// LogParameters value property
    member _.LogParameters : TargetDeviceConf.T_LogParameters =
        m_LogParameters

    /// Get LU nodes that can be accessed from one or more target nodes.
    member this.GetAccessibleLUNodes() : ILUNode list =
        m_ConfNodes.GetChildNodeList<ConfNode_TargetGroup> m_NodeID
        |> Seq.map _.GetAccessibleLUNodes()
        |> Seq.concat
        |> Seq.toList

    /// <summary>
    /// Create new object that has updated configuration values.
    /// </summary>
    /// <param name="argTargetDeviceID">
    ///  Target device ID.
    /// </param>
    /// <param name="argTargetDeviceName">
    ///  Target device name.
    /// </param>
    /// <param name="argNegotiableParameters">
    ///  Configuration values.
    /// </param>
    /// <param name="argLogParameters">
    ///  Configuration values.
    /// </param>
    /// <returns>
    ///  Created new node that has specified new values and same node ID.
    /// </returns>
    member _.CreateUpdatedNode 
        ( argTargetDeviceID : TDID_T )
        ( argTargetDeviceName : string )
        ( argNegotiableParameters : TargetDeviceConf.T_NegotiableParameters )
        ( argLogParameters : TargetDeviceConf.T_LogParameters ) : ConfNode_TargetDevice = 
        new ConfNode_TargetDevice(
            m_MessageTable, m_ConfNodes, m_NodeID, argTargetDeviceID, argTargetDeviceName, argNegotiableParameters, argLogParameters, ModifiedStatus.Modified
        )

    /// <summary>
    ///  Create new object that has same value but modified flag set to "Modified".
    /// </summary>
    /// <returns>
    ///  Created new object.
    /// </returns>
    member this.SetModified() : ConfNode_TargetDevice =
        this.CreateUpdatedNode m_TargetDeviceID m_TargetDeviceName m_NegotiableParameters m_LogParameters

    /// <summary>
    /// Get configuration data.
    /// </summary>
    /// <remarks>
    /// This method returns only target device configuration string.
    /// But it not include target group, or other node configurations.
    /// </remarks>
    member _.GetConfigureData() : TargetDeviceConf.T_TargetDevice =
        let childNodes_NetworkPortal =
            m_ConfNodes.GetChildNodeList<ConfNode_NetworkPortal> m_NodeID
            |> List.map ( fun itr -> itr.NetworkPortal )
        {
            NetworkPortal = childNodes_NetworkPortal;
            NegotiableParameters = Some m_NegotiableParameters;
            LogParameters = Some m_LogParameters;
            DeviceName = m_TargetDeviceName;
        }

