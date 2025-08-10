//=============================================================================
// Haruka Software Storage.
// ConfNode_Controller.fs : It represents configurations of controller.
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
///  Controller node used at the configurations.
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
///  Configuration values.
/// </param>
/// <param name="m_Modified">
///  Valued are modified or not.
/// </param>
/// <remarks>
///  RemoteCtrl, LogMaintenance and LogParameters are ommitted, default paremeter values are set.
/// </remarks>
type ConfNode_Controller(
        m_MessageTable : StringTable,
        m_ConfNodes : ConfNodeRelation,
        m_NodeID : CONFNODE_T,
        m_Value : HarukaCtrlConf.T_HarukaCtrl,
        m_Modified : ModifiedStatus
    ) =

    /// RemoteCtrl configuration values.
    let m_RemoteCtrl =
        if m_Value.RemoteCtrl.IsSome then
            m_Value.RemoteCtrl.Value
        else
            {
                PortNum = Constants.DEFAULT_MNG_CLI_PORT_NUM;
                Address = "::1";
                WhiteList = [];
            }

    /// LogMaintenance configuration values.
    let m_LogMaintenance =
        if m_Value.LogMaintenance.IsSome then
            m_Value.LogMaintenance.Value
        else
            {
                OutputDest = HarukaCtrlConf.U_ToStdout( Constants.LOGMNT_DEF_TOTALLIMIT )
            }

    /// LogParameters configuration values.
    let m_LogParameters =
        if m_Value.LogParameters.IsSome then
            m_Value.LogParameters.Value
        else
            {
                SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
                HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
                LogLevel = LogLevel.LOGLEVEL_INFO;
            }

    /// <summary>
    ///  Constructor with default value.
    /// </summary>
    /// <param name="argMessageTable">
    ///  Message resource reader.
    /// </param>
    /// <param name="argConfNodes">
    ///  Configuration nodes holder.
    /// </param>
    /// <param name="argNodeID">
    ///  Node ID of this node.
    /// </param>
    /// <remarks>
    ///  Modified flag is set to "NotModified".
    /// </remarks>
    new ( argMessageTable : StringTable, argConfNodes : ConfNodeRelation, argNodeID : CONFNODE_T ) =
        ConfNode_Controller (
            argMessageTable,
            argConfNodes,
            argNodeID,
            {
                RemoteCtrl = None;
                LogMaintenance = None;
                LogParameters = None;
            },
            ModifiedStatus.NotModified
        )

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
        let conf : HarukaCtrlConf.T_HarukaCtrl = {
            RemoteCtrl = Some {
                PortNum = Functions.SearchAndConvert d "RemoteCtrl.PortNumber" UInt16.Parse Constants.DEFAULT_MNG_CLI_PORT_NUM;
                Address = Functions.SearchAndConvert d "RemoteCtrl.Address" id "::1";
                WhiteList =
                    Functions.SearchAndConvert d "RemoteCtrl.WhiteList" ( ( fun s -> s.Split "\t" ) >> Seq.map IPCondition.Parse >> Seq.toList ) [];
            };
            LogMaintenance = Some {
                OutputDest =
                    let b = Functions.SearchAndConvert d "LogMaintenance.OutputStdout" ( bool.Parse ) true
                    if b then
                        HarukaCtrlConf.U_ToStdout(
                            Functions.SearchAndConvert d "LogMaintenance.TotalLimit" ( UInt32.Parse ) Constants.LOGMNT_DEF_TOTALLIMIT;
                        )
                    else
                        HarukaCtrlConf.U_ToFile({
                            TotalLimit = Functions.SearchAndConvert d "LogMaintenance.TotalLimit" ( UInt32.Parse ) Constants.LOGMNT_DEF_TOTALLIMIT;
                            MaxFileCount = Functions.SearchAndConvert d "LogMaintenance.MaxFileCount" ( UInt32.Parse ) Constants.LOGMNT_DEF_MAXFILECOUNT;
                            ForceSync = Functions.SearchAndConvert d "LogMaintenance.ForceSync" ( bool.Parse ) Constants.LOGMNT_DEF_FORCESYNC;
                        })
            }
            LogParameters = Some {
                SoftLimit = Functions.SearchAndConvert d "LogParameters.SoftLimit" UInt32.Parse Constants.LOGPARAM_DEF_SOFTLIMIT;
                HardLimit = Functions.SearchAndConvert d "LogParameters.HardLimit" UInt32.Parse Constants.LOGPARAM_DEF_HARDLIMIT;
                LogLevel = Functions.SearchAndConvert d "LogParameters.LogLevel" LogLevel.fromString LogLevel.LOGLEVEL_INFO;
            };
        }
        ConfNode_Controller ( argMessageTable, argConfNodes, newNodeID, conf, ModifiedStatus.Modified )


    //=========================================================================
    // Interface method

    /// <inheritdoc />
    interface IConfigFileNode with
        
        // --------------------------------------------------------------------
        // Implementation of IConfigureNode.Validate
        override this.Validate ( msgList : ( CONFNODE_T * string ) list ) : ( CONFNODE_T * string ) list =
            let curID = ( this :> IConfigureNode ).NodeID
            let childNodes = m_ConfNodes.GetChildNodeList<IConfigureNode> m_NodeID
            let parentNodes = m_ConfNodes.GetParentNodeList<IConfigureNode> m_NodeID

            msgList
            |> ( fun argmsg ->
                let v = this.LogParametersValue.SoftLimit
                let minr = Constants.LOGPARAM_MIN_SOFTLIMIT
                let maxr = Constants.LOGPARAM_MAX_SOFTLIMIT
                if v < minr || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_LOGPARAM_SOFTLIMIT", v.ToString(), minr.ToString(), maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = this.LogParametersValue.HardLimit
                let minr = Constants.LOGPARAM_MIN_HARDLIMIT
                let maxr = Constants.LOGPARAM_MAX_HARDLIMIT
                if v < minr || maxr < v then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_LOGPARAM_HARDLIMIT", v.ToString(), minr.ToString(), maxr.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let vh = this.LogParametersValue.HardLimit
                let vs = this.LogParametersValue.SoftLimit
                if vh < vs then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_LOGPARAM_HARDLIMIT_LESS_THAN_SOFTLIMIT", vh.ToString(), vs.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                match this.LogMaintenanceValue.OutputDest with
                | HarukaCtrlConf.U_ToFile( x ) ->
                    let v = x.TotalLimit
                    let minr = Constants.LOGMNT_MIN_TOTALLIMIT
                    let maxr = Constants.LOGMNT_MAX_TOTALLIMIT
                    if v < minr || maxr < v then
                        let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_LOGPARAM_TOTALLIMIT", v.ToString(), minr.ToString(), maxr.ToString() )
                        ( curID, msg ) :: argmsg
                    else
                        argmsg
                | HarukaCtrlConf.U_ToStdout( v ) ->
                    let minr = Constants.LOGMNT_MIN_TOTALLIMIT
                    let maxr = Constants.LOGMNT_MAX_TOTALLIMIT
                    if v < minr || maxr < v then
                        let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_LOGPARAM_TOTALLIMIT", v.ToString(), minr.ToString(), maxr.ToString() )
                        ( curID, msg ) :: argmsg
                    else
                        argmsg
            )
            |> ( fun argmsg ->
                match this.LogMaintenanceValue.OutputDest with
                | HarukaCtrlConf.U_ToFile( x ) ->
                    let v = x.MaxFileCount
                    let minr = Constants.LOGMNT_MIN_MAXFILECOUNT
                    let maxr = Constants.LOGMNT_MAX_MAXFILECOUNT
                    if v < minr || maxr < v then
                        let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_LOGPARAM_MAXFILECOUNT", v.ToString(), minr.ToString(), maxr.ToString() )
                        ( curID, msg ) :: argmsg
                    else
                        argmsg
                | HarukaCtrlConf.U_ToStdout( _ ) ->
                    argmsg
            )
            |> ( fun argmsg ->
                let v = this.RemoteCtrlValue.PortNum
                if v < 1us then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_REMOTE_CTRL_PORT_NUM", v.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = this.RemoteCtrlValue.Address
                if v.Length < 1 || v.Length > Constants.MAX_CTRL_ADDRESS_STR_LENGTH then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_INVALID_REMOTE_CTRL_ADDRESS", v.ToString() )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let v = this.RemoteCtrlValue.WhiteList.Length
                if v > Constants.MAX_IP_WHITELIST_COUNT then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_IP_WHITELIST_TOO_LONG", ( sprintf "%d" Constants.MAX_IP_WHITELIST_COUNT ) )
                    ( m_NodeID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let maxr = Constants.MAX_TARGET_DEVICE_COUNT
                if maxr < childNodes.Length then
                    ( curID, m_MessageTable.GetMessage( "CHKMSG_OUT_OF_TARGET_DEVICE_COUNT", maxr.ToString() ) ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                let tds = m_ConfNodes.GetChildNodeList<ConfNode_TargetDevice> m_NodeID
                tds
                |> Seq.countBy ( fun itr -> itr.TargetDeviceID )
                |> Seq.filter ( fun ( _, cnt ) -> cnt > 1 )
                |> Seq.map ( fun ( tdid, _ ) -> tds |> Seq.filter ( fun itr -> itr.TargetDeviceID = tdid ) )
                |> Seq.fold ( fun wMsgList itr ->
                    itr
                    |> Seq.fold ( fun wMsgList2 itr2 ->
                        let msg = m_MessageTable.GetMessage( "CHKMSG_DUPLICATE_TARGET_DEVICE_ID", tdid_me.toString itr2.TargetDeviceID )
                        ( ( itr2 :> IConfigureNode ).NodeID, msg ) :: wMsgList2
                    ) wMsgList
                ) argmsg
            )
            |> ( fun argmsg ->
                if parentNodes.Length > 0 then
                    let msg = m_MessageTable.GetMessage( "CHKMSG_CTRL_NODE_NOT_ROOT" )
                    ( curID, msg ) :: argmsg
                else
                    argmsg
            )
            |> ( fun argmsg ->
                childNodes
                |> Seq.fold ( fun wMsgList itr ->
                    match itr with
                    | :? ConfNode_TargetDevice ->
                        itr.Validate wMsgList
                    | _ ->
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
            ClientConst.NODE_TYPE_NAME_Controller

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
        override this.MinDescriptString : string =
            ( this :> IConfigureNode ).NodeTypeName

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.ShortDescriptString property
        override this.ShortDescriptString : string =
            ( this :> IConfigureNode ).NodeTypeName

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.FullDescriptString property
        override this.FullDescriptString : string list =
            [
                yield     sprintf "Node type : %s" ( this :> IConfigureNode ).NodeTypeName
                yield             "Values :"
                yield     sprintf "  RemoteCtrl :"
                yield     sprintf "    PortNumber(uint16) : %d" m_RemoteCtrl.PortNum
                yield     sprintf "    Address(string) : %s" m_RemoteCtrl.Address
                yield     sprintf "    WhiteList : "
                for cond in m_RemoteCtrl.WhiteList do
                    yield sprintf "      %s " ( IPCondition.ToString cond )
                yield     sprintf "  LogMaintenance :"
                match m_LogMaintenance.OutputDest with
                | HarukaCtrlConf.U_ToFile( x ) ->
                    yield sprintf "    OutputStdout(bool) : false" 
                    yield sprintf "    TotalLimit(uint32) : %d" x.TotalLimit
                    yield sprintf "    MaxFileCount(uint32) : %d" x.MaxFileCount
                    yield sprintf "    ForceSync(bool) : %b" x.ForceSync
                | HarukaCtrlConf.U_ToStdout( x ) ->
                    yield sprintf "    OutputStdout(bool) : true" 
                    yield sprintf "    TotalLimit(uint32) : %d" x
                yield     sprintf "  LogParameters :"
                yield     sprintf "    SoftLimit(uint32) : %d" m_LogParameters.SoftLimit
                yield     sprintf "    HardLimit(uint32) : %d" m_LogParameters.HardLimit
                yield     sprintf "    LogLevel : %s" ( LogLevel.toString m_LogParameters.LogLevel )
                yield     sprintf "Modified : %b" ( m_Modified = ModifiedStatus.Modified )
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.SortKey =
            [
                ClientConst.SORT_KEY_TYPE_Controller;
                sprintf "%016X" m_NodeID;
            ]

        // ------------------------------------------------------------------------
        // Implementation of IConfigureNode.SortKey property
        override _.TempExportData : TempExport.T_Node =
            {
                TypeName = ClientConst.TEMPEXP_NN_Controller;
                NodeID = confnode_me.toPrim m_NodeID;
                Values = [
                    yield {
                        Name = "RemoteCtrl.PortNumber";
                        Value = sprintf "%d" m_RemoteCtrl.PortNum;
                    };
                    yield {
                        Name = "RemoteCtrl.Address";
                        Value = m_RemoteCtrl.Address;
                    };
                    yield {
                        Name = "RemoteCtrl.WhiteList";
                        Value = 
                            m_RemoteCtrl.WhiteList
                            |> Seq.map IPCondition.ToString
                            |> String.concat "\t" ;
                    };
                    match m_LogMaintenance.OutputDest with
                    | HarukaCtrlConf.U_ToFile( x ) ->
                        yield {
                            Name = "LogMaintenance.OutputStdout";
                            Value = "false";
                        };
                        yield {
                            Name = "LogMaintenance.TotalLimit";
                            Value = sprintf "%d" x.TotalLimit;
                        };
                        yield {
                            Name = "LogMaintenance.MaxFileCount";
                            Value = sprintf "%d" x.MaxFileCount;
                        };
                        yield {
                            Name = "LogMaintenance.ForceSync";
                            Value = sprintf "%b" x.ForceSync;
                        };
                    | HarukaCtrlConf.U_ToStdout( x ) ->
                        yield {
                            Name = "LogMaintenance.OutputStdout";
                            Value = "true";
                        };
                        yield {
                            Name = "LogMaintenance.TotalLimit";
                            Value = sprintf "%d" x;
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
            new ConfNode_Controller( m_MessageTable, m_ConfNodes, m_NodeID, m_Value, ModifiedStatus.NotModified )

    //=========================================================================
    // Public method

    /// RemoteCtrl configuration values proterty.
    member _.RemoteCtrlValue : HarukaCtrlConf.T_RemoteCtrl =
        m_RemoteCtrl

    /// LogMaintenance configuration values proterty.
    member _.LogMaintenanceValue : HarukaCtrlConf.T_LogMaintenance =
        m_LogMaintenance

    /// LogParameters configuration values proterty.
    member _.LogParametersValue : HarukaCtrlConf.T_LogParameters =
        m_LogParameters

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
        ( argValue : HarukaCtrlConf.T_HarukaCtrl ) : ConfNode_Controller = 
        new ConfNode_Controller(
            m_MessageTable, m_ConfNodes, m_NodeID, argValue, ModifiedStatus.Modified
        )

    /// <summary>
    /// Get configuration data.
    /// </summary>
    /// <remarks>
    /// This method returns all configuration string of controller.
    /// RemoteCtrl, LogMaintenance and LogParameters are guaranteed to have a value.
    /// </remarks>
    member _.GetConfigureData() : HarukaCtrlConf.T_HarukaCtrl =
        {
            RemoteCtrl = Some m_RemoteCtrl;
            LogMaintenance = Some m_LogMaintenance;
            LogParameters = Some m_LogParameters;
        }
