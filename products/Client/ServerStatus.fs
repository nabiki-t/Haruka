//=============================================================================
// Haruka Software Storage.
// ServerStatus.fs : It represents configurations and status of Haruka server.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Client

//=============================================================================
// Import declaration

open System
open System.Collections.Generic
open System.Threading.Tasks

open Haruka.Commons
open Haruka.IODataTypes
open Haruka.Constants

/// <summary>
///  Definition of ServerStatus class.
/// </summary>
/// <param name="m_MessageTable">
///  Message resource reader.
/// </param>
type ServerStatus(
    m_MessageTable : StringTable
) =

    /// Configuration nodes holder.
    let m_ConfNodes = new ConfNodeRelation()

    /// Node ID of the contoller node.
    let m_ControllerNodeID = confnode_me.fromPrim 1UL

    /// <summary>
    ///  Load configurations from controller.
    /// </summary>
    /// <param name="con">
    ///  Connection to the controller.
    /// </param>
    /// <param name="forCLI">
    ///  If this object will be used at CLI client, specify true. Otherwise false.
    /// </param>
    /// <remarks>
    ///   All current loaded configuration values would be cleared.
    ///   At CLI client, LU nodes will be child of the target node only.
    ///   But for GUI client, LU nodes will be child of both of the target node and target group node.
    /// </remarks>
    abstract LoadConfigure : CtrlConnection -> bool -> Task
    default this.LoadConfigure con forCLI =
        task {
            // Initialize all of configuration nodes and get contoroller configuration.
            let! ctrlConf = this.LoadCtrlConfigFromController con
            m_ConfNodes.AddNode ctrlConf

            // Get target device list
            let! tdList = ServerStatus.LoadTargetDeviceIDList con

            // Add target device and target group configuration to component dict.
            for itr in tdList do
                do! this.LoadTargetDeviceConfig con ( ctrlConf :> IConfigureNode ).NodeID itr forCLI
        }

    /// <summary>
    ///  Load controller configuration values from the controller.
    /// </summary>
    /// <param name="con">
    ///  Connection to the controller.
    /// </param>
    /// <returns>
    ///  loaded contoroller configuration, or default values.
    /// </returns>
    /// <remarks>
    ///  This method clear all of other configuration node.
    ///  It is not possible to obtain only the setting values of the controller node.
    /// </remarks>
    member private _.LoadCtrlConfigFromController( con : CtrlConnection ) : Task< ConfNode_Controller > =
        task {
            // Initialize all of configuration node.
            m_ConfNodes.Initialize()
            let ctrlNodeID = m_ConfNodes.NextID // Node ID of controller node always must be 1.
            assert( ctrlNodeID = m_ControllerNodeID )

            try
                // Get contoroller configuration
                let! res = con.GetControllerConfig()
                return ConfNode_Controller( m_MessageTable, m_ConfNodes, ctrlNodeID, res, ModifiedStatus.NotModified )
            with
            | :? RequestError as x ->
                // If returned message was bloken, return default value.
                return ConfNode_Controller( m_MessageTable, m_ConfNodes, ctrlNodeID )
        }

    /// <summary>
    ///  Load target device IDs from the controller.
    /// </summary>
    /// <param name="con">
    ///  Connection object to the controller.
    /// </param>
    /// <returns>
    ///  Returned target device IDs list.
    /// </returns>
    static member private LoadTargetDeviceIDList( con : CtrlConnection ) : Task< TDID_T list > =
        task {
            try
                return! con.GetTargetDeviceDir()
            with
            | :? RequestError as x ->
                return []
        }

    /// <summary>
    ///  Load target device configuration from the controller.
    /// </summary>
    /// <param name="con">
    ///  Connection object to the controller.
    /// </param>
    /// <param name="parentID">
    ///  Parent node ID.
    /// </param>
    /// <param name="tdid">
    ///  Target device ID.
    /// </param>
    /// <param name="forCLI">
    ///  If this object will be used at CLI client, specify true. Otherwise false.
    /// </param>
    member private this.LoadTargetDeviceConfig( con : CtrlConnection ) ( parentID : CONFNODE_T ) ( tdid : TDID_T ) ( forCLI : bool ) : Task =
        task {
            try
                // Get target device configuration data
                let! tdConf = con.GetTargetDeviceConfig tdid
                let tdConfID = m_ConfNodes.NextID
                let c = ConfNode_TargetDevice( m_MessageTable, m_ConfNodes, tdConfID, tdid, tdConf )

                // Add component
                m_ConfNodes.AddNode c
                m_ConfNodes.AddRelation parentID tdConfID

                // Add Network Portal configuration node to dictionary
                tdConf.NetworkPortal
                |> List.iter ( fun itr ->
                    let npConfID = m_ConfNodes.NextID
                    let c = ConfNode_NetworkPortal( m_MessageTable, m_ConfNodes, npConfID, itr )
                    m_ConfNodes.AddNode c
                    m_ConfNodes.AddRelation tdConfID npConfID
                )

                // load target group configuration
                do! this.LoadTargetGroupConfig con tdid tdConfID forCLI

            with
            | :? RequestError as x ->
                // If returned message was bloken, return default value.
                ()
        }

    /// <summary>
    ///  Load target group configuration from the controller.
    /// </summary>
    /// <param name="con">
    ///  Connection object to the controller.
    /// </param>
    /// <param name="tdid">
    ///  Target device ID.
    /// </param>
    /// <param name="parentID">
    ///  Parent node ID. This ID must be target device node ID.
    /// </param>
    /// <param name="forCLI">
    ///  If this object will be used at CLI client, specify true. Otherwise false.
    /// </param>
    member private this.LoadTargetGroupConfig ( con : CtrlConnection ) ( tdid : TDID_T ) ( parentID : CONFNODE_T ) ( forCLI : bool ) : Task =
        task {
            try
                // get all target group configuration in the specified target device.
                let! res = con.GetAllTargetGroupConfig tdid
                // Insert target group configuration to component dictionary
                res
                |> List.iter ( this.RecogniseTargetGroupConfig parentID forCLI )
                ()
            with
            | :? RequestError as x ->
                // If returned message was bloken, skip this target configuration.
                ()
        }

    /// <summary>
    ///  Add target group configuration node to the dictionary.
    /// </summary>
    /// <param name="parentID">
    ///  Parent node ID. This ID must be target device node ID.
    /// </param>
    /// <param name="forCLI">
    ///  If this object will be used at CLI client, specify true. Otherwise false.
    /// </param>
    /// <param name="argConf">
    ///  Target group configuration that is gotten from controller.
    /// </param>
    member private this.RecogniseTargetGroupConfig ( parentID : CONFNODE_T ) ( forCLI : bool ) ( argConf : TargetGroupConf.T_TargetGroup ) : unit =
        try
            // Add a target group component
            let tgConfID = m_ConfNodes.NextID
            let tg =
                ConfNode_TargetGroup(
                    m_MessageTable, 
                    m_ConfNodes,
                    tgConfID,
                    argConf.TargetGroupID,
                    argConf.TargetGroupName,
                    argConf.EnabledAtStart,
                    ModifiedStatus.NotModified
            )
            m_ConfNodes.AddNode tg
            m_ConfNodes.AddRelation parentID tgConfID

            // Add target components
            let tid2LUList =
                argConf.Target
                |> Seq.map ( fun tgcT ->
                    let tConfID = m_ConfNodes.NextID
                    let ssT = ConfNode_Target( m_MessageTable, m_ConfNodes, tConfID, { tgcT with LUN = [] } )
                    m_ConfNodes.AddNode ssT
                    m_ConfNodes.AddRelation tgConfID tConfID
                    ( tConfID, tgcT.LUN )
                )
                |> Seq.toArray

            // used LUN list
            let usedLUN =
                tid2LUList
                |> Seq.map snd
                |> Seq.concat
                |> Seq.distinct
                |> Seq.toArray

            // Add LU and media components
            let lu2IDDict =
                argConf.LogicalUnit
                |> Seq.choose ( fun clu ->
                    // check this LUN of lu is used by target or not
                    if Array.exists ( (=) clu.LUN ) usedLUN then
                        match clu.LUDevice with
                        | TargetGroupConf.T_DEVICE.U_BlockDevice( x ) ->
                            let luConfID = m_ConfNodes.NextID
                            let lu = ConfNode_BlockDeviceLU( m_MessageTable, m_ConfNodes, luConfID, clu.LUN, clu.LUName )
                            m_ConfNodes.AddNode lu

                            // Add media components
                            this.AddMediaComponentToDict luConfID x.Peripheral

                            Some <| KeyValuePair< LUN_T, CONFNODE_T >( clu.LUN, luConfID )

                        | TargetGroupConf.T_DEVICE.U_DummyDevice( x ) ->
                            let luConfID = m_ConfNodes.NextID
                            let lu = ConfNode_DummyDeviceLU( m_MessageTable, m_ConfNodes, luConfID, clu.LUN, clu.LUName )
                            m_ConfNodes.AddNode lu
                            Some <| KeyValuePair< LUN_T, CONFNODE_T >( clu.LUN, luConfID )
                    else
                        None
                )
                |> Dictionary

            // Add target to LU relations
            tid2LUList
            |> Array.iter ( fun ( tid, lunList ) ->
                lunList
                |> List.iter ( fun lun ->
                    let r, luid = lu2IDDict.TryGetValue lun
                    if r then
                        m_ConfNodes.AddRelation tid luid
                    // If specified LUN in target node configuration, it is silently ignored.
                )
            )

            // If this ServerStatus object is used for GUI client,
            // add the target group node to the LU node relation.
            if not forCLI then
                for itr in lu2IDDict do
                    m_ConfNodes.AddRelation tgConfID itr.Value

        with
        | :? RequestError as x ->
            ()

    /// <summary>
    ///  Add media configuration node to the dictionary.
    /// </summary>
    /// <param name="parentID">
    ///  Parent node ID.
    /// </param>
    /// <param name="m">
    ///  Media configuration that is gotten from controller.
    /// </param>
    member private this.AddMediaComponentToDict ( parentID : CONFNODE_T ) ( m : TargetGroupConf.T_MEDIA ) : unit =
        let mediaID = m_ConfNodes.NextID
        match m with
        | TargetGroupConf.T_MEDIA.U_PlainFile( x ) ->
            let sfConf = new ConfNode_PlainFileMedia( m_MessageTable, m_ConfNodes, mediaID, x )
            m_ConfNodes.AddNode sfConf
            m_ConfNodes.AddRelation parentID mediaID

        | TargetGroupConf.T_MEDIA.U_MemBuffer( x ) ->
            let sfConf = new ConfNode_MemBufferMedia( m_MessageTable, m_ConfNodes, mediaID, x )
            m_ConfNodes.AddNode sfConf
            m_ConfNodes.AddRelation parentID mediaID

        | TargetGroupConf.T_MEDIA.U_DummyMedia( x ) ->
            let sfConf = new ConfNode_DummyMedia( m_MessageTable, m_ConfNodes, mediaID, x.IdentNumber, x.MediaName )
            m_ConfNodes.AddNode sfConf
            m_ConfNodes.AddRelation parentID mediaID

        | TargetGroupConf.T_MEDIA.U_DebugMedia( x ) ->
            let sfConf = new ConfNode_DebugMedia( m_MessageTable, m_ConfNodes, mediaID, x.IdentNumber, x.MediaName )
            m_ConfNodes.AddNode sfConf
            m_ConfNodes.AddRelation parentID mediaID
            this.AddMediaComponentToDict mediaID x.Peripheral

    /// <summary>
    /// Upload all of modified configuration files to cotroller.
    /// </summary>
    /// <param name="con">
    /// Connection object to controller.
    /// </param>
    abstract Publish : CtrlConnection -> Task
    default this.Publish con =

        // check configuration error is exist or not.
        if ( this.Validate() ).Length > 0 then
            raise <| ConfigurationError( m_MessageTable.GetMessage( "ERRMSG_VALIDATION_FAILED" ) )

        task {
            // search modified node
            let modnode =
                m_ConfNodes.AllNodes
                |> Seq.filter ( function :? IConfigFileNode as x -> x.Modified = ModifiedStatus.Modified | _ -> false )
                |> Seq.map ( fun itr -> itr :?> IConfigFileNode )
                |> Seq.toArray

            // search modified controller node
            let modnode_controller =
                modnode
                |> Seq.filter ( function :? ConfNode_Controller -> true | _ -> false )
                |> Seq.map ( fun itr -> itr :?> ConfNode_Controller )
                |> Seq.toArray

            // search modified target device node
            let modnode_TD =
                modnode
                |> Seq.filter ( function :? ConfNode_TargetDevice -> true | _ -> false )
                |> Seq.map ( fun itr -> itr :?> ConfNode_TargetDevice )
                |> Seq.toArray

            // all of tartget device node
            let allTD =
                m_ConfNodes.AllNodes
                |> Seq.filter ( function :? ConfNode_TargetDevice -> true | _ -> false )
                |> Seq.toArray

            // search modified target group node
            let modnode_TG =
                modnode
                |> Seq.filter ( function :? ConfNode_TargetGroup -> true | _ -> false )
                |> Seq.map ( fun itr -> itr :?> ConfNode_TargetGroup )
                |> Seq.toArray

            let allTG =
                allTD
                |> Seq.map ( fun itr ->
                    ( itr :?> ConfNode_TargetDevice ).TargetDeviceID,
                    itr.GetChildNodes<ConfNode_TargetGroup>()
                )
                |> Seq.map KeyValuePair
                |> Dictionary

            // Update controller configuration files
            for itr in modnode_controller do
                // upload
                do! con.SetControllerConfig ( itr.GetConfigureData() )
                // Reset modified flag to NotModified
                m_ConfNodes.Update ( ( itr :> IConfigFileNode ).ResetModifiedFlag() )

            // Update target device configuration files
            for itr in modnode_TD do
                // Create target device directory
                do! con.CreateTargetDeviceDir( itr.TargetDeviceID )
                // Upload target device configuration file
                let confStr = itr.GetConfigureData()
                do! con.CreateTargetDeviceConfig itr.TargetDeviceID  confStr
                // Reset modified flag to NotModified
                m_ConfNodes.Update ( ( itr :> IConfigFileNode ).ResetModifiedFlag() )

            // Delete target device working directory for removed target device.
            let! effectiveTDID = con.GetTargetDeviceDir()
            let removedTDID =
                effectiveTDID
                |> Seq.filter ( fun itr ->
                    allTD
                    |> Seq.exists ( fun itr2 -> ( itr2 :?> ConfNode_TargetDevice ).TargetDeviceID = itr )
                    |> not
                )
                |> Seq.toArray
            for itr in removedTDID do
                // delete target device directory
                do! con.DeleteTargetDeviceDir itr

            // Divite modified target group IDs by target device ID.
            let wd =
                allTD
                |> Seq.map ( fun itr -> itr :?> ConfNode_TargetDevice )
                |> Seq.map ( fun itr -> itr.TargetDeviceID, ( itr, HashSet< ConfNode_TargetGroup >() ) )
                |> Seq.map KeyValuePair
                |> Dictionary

            for itr in modnode_TG do
                // Get target device ID where current target group is belongings to.
                let tdnode =
                    let nid = ( itr :> IConfigureNode ).NodeID
                    m_ConfNodes.GetParentNodeList<ConfNode_TargetDevice> nid
                if tdnode.Length <> 1 then
                    raise <| ConfigurationError( m_MessageTable.GetMessage( "ERRMSG_UNEXPEDTED_CONFIGURATION_ERROR" ) )
                let tdid = tdnode.[0].TargetDeviceID
                let _, h = wd.[ tdid ]
                h.Add( itr ) |> ignore

            for itr in wd do
                let tdid = itr.Key              // target device ID
                let tdnode, mod_tgnode = itr.Value  // target device node and target group nodes
                let all_tgnodes = allTG.Item( tdnode.TargetDeviceID )

                // Get removed target group IDs
                let! effectiveTGIDs = con.GetTargetGroupID( tdid )
                let removedTGIDs =
                    effectiveTGIDs
                    |> Seq.filter ( fun etgid ->
                        all_tgnodes
                        |> Seq.exists ( fun tgnodeItr -> tgnodeItr.TargetGroupID = etgid )
                        |> not
                    )
                    |> Seq.toArray

                // Get removed LUN
                let! effectiveLUNs = con.GetLUWorkDir tdid
                let configuredLUNs =
                    tdnode.GetAccessibleLUNodes()
                    |> Seq.map ( fun itr -> itr.LUN )
                    |> Seq.toArray
                let removedLUNs =
                    effectiveLUNs
                    |> Seq.filter ( fun elun ->
                        configuredLUNs
                        |> Seq.exists ( (=) elun )
                        |> not
                    )
                    |> Seq.toArray

                // Delete removed target group configuration file
                for rmtgid in removedTGIDs do
                    do! con.DeleteTargetGroupConfig tdid rmtgid

                // Delete removed LU work directory
                for rmlu in removedLUNs do
                    do! con.DeleteLUWorkDir tdid rmlu

                // upload modified target group configuration files
                for itrtg in mod_tgnode do
                    // Upload
                    let confstr = itrtg.GetConfigureData()
                    do! con.CreateTargetGroupConfig tdid confstr
                    // Reset modified flag to NotModified.
                    m_ConfNodes.Update ( ( itrtg :> IConfigFileNode ).ResetModifiedFlag() )

                    // Delete and create modified LU working directory
                    let luInTg =
                        itrtg.GetAccessibleLUNodes()
                        |> Seq.map ( fun itr -> itr.LUN )
                    for lunitr in luInTg do
                        do! con.DeleteLUWorkDir tdid lunitr
                        do! con.CreateLUWorkDir tdid lunitr
        }

    /// <summary>
    ///  Validate specified  configuration object.
    /// </summary>
    /// <returns>
    ///  Check result mesasges.
    /// </returns>
    abstract Validate : unit -> ( CONFNODE_T * string ) list
    default _.Validate() =
        m_ConfNodes.GetNode( m_ControllerNodeID ).Validate []
        |> List.rev

    /// <summary>
    ///  Get configure node from node ID.
    /// </summary>
    /// <param name="nodeID">
    ///  Node ID which would be get.
    /// </param>
    /// <returns>
    ///  configure node.
    /// </returns>
    abstract GetNode : CONFNODE_T -> IConfigureNode
    default _.GetNode nodeID =
        m_ConfNodes.GetNode nodeID

    /// <summary>
    ///  Get modified node is exist or not.
    /// </summary>
    abstract IsModified : bool
    default _.IsModified =
        m_ConfNodes.AllNodes
        |> Seq.filter ( fun itr -> match itr with :? IConfigFileNode -> true | _ -> false )
        |> Seq.map ( fun itr -> itr :?> IConfigFileNode )
        |> Seq.exists ( fun itr -> itr.Modified = ModifiedStatus.Modified )


    /// <summary>
    ///  Get node ID of the controller node.
    /// </summary>
    /// <remarks>
    ///  Controller node ID is always 1.
    /// </remarks>
    abstract ControllerNodeID : CONFNODE_T
    default _.ControllerNodeID =
        m_ControllerNodeID

    /// <summary>
    ///  Get the controller node.
    /// </summary>
    abstract ControllerNode : ConfNode_Controller
    default _.ControllerNode =
        // Controller node ID is always 1.
        ( m_ConfNodes.GetNode ( m_ControllerNodeID ) ) :?> ConfNode_Controller

    /// <summary>
    ///  Update controller node.
    /// </summary>
    /// <param name="conf">
    ///  Parameters that are set to new controller node.
    /// </param>
    /// <returns>
    ///  Updated controller node instance.
    /// </returns>
    /// <remarks>
    ///  If a new node is added after being deleted, the node ID will be changed and relational child node are deleted.
    ///  This method can update attribute value without changing node ID and relations.
    /// </remarks>
    abstract UpdateControllerNode : HarukaCtrlConf.T_HarukaCtrl -> ConfNode_Controller
    default this.UpdateControllerNode conf =
        let newNode = this.ControllerNode.CreateUpdatedNode conf
        m_ConfNodes.Update newNode
        newNode

    /// <summary>
    ///  Get target device nodes list.
    /// </summary>
    abstract GetTargetDeviceNodes : unit -> ConfNode_TargetDevice list
    default _.GetTargetDeviceNodes() =
        m_ConfNodes.GetChildNodeList<ConfNode_TargetDevice> m_ControllerNodeID

    /// <summary>
    ///  Create target device node.
    /// </summary>
    /// <param name="argTargetDeviceID">
    ///  Target device ID
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
    ///  Created new target device node.
    /// </returns>
    /// <remarks>
    ///  Added target device node has no network portal node or target group node.
    ///  So, in this state, configuration files can't be uploaded.
    /// </remarks>
    abstract AddTargetDeviceNode : TDID_T -> string -> TargetDeviceConf.T_NegotiableParameters -> TargetDeviceConf.T_LogParameters -> ConfNode_TargetDevice
    default _.AddTargetDeviceNode argTargetDeviceID argTargetDeviceName argNegotiableParameters argLogParameters =
        let nid = m_ConfNodes.NextID
        let n = 
            new ConfNode_TargetDevice(
                m_MessageTable,
                m_ConfNodes,
                nid,
                argTargetDeviceID,
                argTargetDeviceName,
                argNegotiableParameters,
                argLogParameters,
                ModifiedStatus.Modified 
            )
        m_ConfNodes.AddNode n
        m_ConfNodes.AddRelation m_ControllerNodeID nid
        n

    /// <summary>
    ///  Delete specified target device node, and all of child nodes.
    /// </summary>
    /// <param name="tdnode">
    ///  The target device node which should be deleted.
    /// </param>
    /// <remarks>
    ///  Target device node which can be deleted must have been modified or the corresponding target device process must have been terminated.
    /// </remarks>
    abstract DeleteTargetDeviceNode : ConfNode_TargetDevice -> unit
    default _.DeleteTargetDeviceNode tdnode =
        m_ConfNodes.DeleteAllChildNodeList ( tdnode :> IConfigureNode ).NodeID

    /// <summary>
    ///  Update target device node.
    /// </summary>
    /// <param name="tdnode">
    ///  The target device node which should be updated.
    /// </param>
    /// <param name="argTargetDeviceID">
    ///  Target device ID
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
    ///  Updated target device node.
    /// </returns>
    /// <remarks>
    ///  Target device node which can be updated must have been modified or the corresponding target device process must have been terminated.
    ///  If a new node is added after being deleted, the node ID will be changed and relational child node are deleted.
    ///  This method can update attribute value without changing node ID and relations.
    /// </remarks>
    abstract UpdateTargetDeviceNode : ConfNode_TargetDevice -> TDID_T -> string -> TargetDeviceConf.T_NegotiableParameters -> TargetDeviceConf.T_LogParameters -> ConfNode_TargetDevice
    default _.UpdateTargetDeviceNode tdnode argTargetDeviceID argTargetDeviceName argNegotiableParameters argLogParameters =
        let nn = tdnode.CreateUpdatedNode argTargetDeviceID argTargetDeviceName argNegotiableParameters argLogParameters
        m_ConfNodes.Update nn
        nn

    /// <summary>
    ///  Add new network portal node to child of specified target device node.
    /// </summary>
    /// <param name="tdNodeID">
    ///  ID of the target device node which is added new network portal node.
    /// </param>
    /// <param name="argNetworkPortal">
    ///  Configuration values of newly created network portal node.
    /// </param>
    /// <returns>
    ///  The newly created network portal node.
    /// </returns>
    /// <remarks>
    ///  Target device node which can be updated must have been modified or the corresponding target device process must have been terminated.
    /// </remarks>
    abstract AddNetworkPortalNode : ConfNode_TargetDevice -> TargetDeviceConf.T_NetworkPortal -> ConfNode_NetworkPortal
    default _.AddNetworkPortalNode tdnode argNetworkPortal =
        let tdNodeID = ( tdnode :> IConfigureNode ).NodeID
        let nnp = new ConfNode_NetworkPortal( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, argNetworkPortal )
        m_ConfNodes.AddNode nnp
        m_ConfNodes.AddRelation tdNodeID ( nnp :> IConfigureNode ).NodeID
        if ( tdnode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            m_ConfNodes.Update ( tdnode.SetModified() )
        nnp

    /// <summary>
    ///  Delete network portal node.
    /// </summary>
    /// <param name="npnode">
    ///  The deleted network portal node.
    /// </param>
    /// <remarks>
    ///  Target device node which can be updated must have been modified or the corresponding target device process must have been terminated.
    /// </remarks>
    abstract DeleteNetworkPortalNode : ConfNode_NetworkPortal -> unit
    default this.DeleteNetworkPortalNode npnode =
        let npNodeID = ( npnode :> IConfigureNode ).NodeID
        let tdNode = this.IdentifyTargetDeviceNode npnode
        m_ConfNodes.DeleteAllChildNodeList npNodeID
        if ( tdNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            m_ConfNodes.Update ( tdNode.SetModified() )

    /// <summary>
    ///  Update network portal node.
    /// </summary>
    /// <param name="npnode">
    ///  The network portal node will be updated.
    /// </param>
    /// <param name="argNetworkPortal">
    ///  Updated configuration values.
    /// </param>
    /// <returns>
    ///  The updated network portal node.
    /// </returns>
    /// <remarks>
    ///  Target device node which can be updated must have been modified or the corresponding target device process must have been terminated.
    ///  If a new node is added after being deleted, the node ID will be changed and relational child node are deleted.
    ///  This method can update attribute value without changing node ID and relations.
    /// </remarks>
    abstract UpdateNetworkPortalNode : ConfNode_NetworkPortal -> TargetDeviceConf.T_NetworkPortal -> ConfNode_NetworkPortal
    default this.UpdateNetworkPortalNode npnode argNetworkPortal =
        let tdNode = this.IdentifyTargetDeviceNode npnode
        let nnp = npnode.CreateUpdatedNode argNetworkPortal
        m_ConfNodes.Update nnp
        if ( tdNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            m_ConfNodes.Update ( tdNode.SetModified() )
        nnp

    /// <summary>
    ///  Create target group node.
    /// </summary>
    /// <param name="tdnode">
    ///  Target device node which is added to newly create target group node.
    /// </param>
    /// <param name="argTargetGroupID">
    ///  Target group ID.
    ///  </param>
    /// <param name="argTargetGroupName">
    ///  Target group name.
    /// </param>
    /// <param name="argEnabledAtStart">
    ///  Flag value of EnabledAtStart.
    /// </param>
    /// <returns>
    ///  Newly create target group node.
    /// </returns>
    /// <remarks>
    ///  Newly create node is added to specified target device node as a child.
    ///  Target groups can be added regardless of target device status.
    ///  Added target group node has no target nodes or LU nodes. So, in this state, configuration files can't be uploaded.
    /// </remarks>
    abstract AddTargetGroupNode : ConfNode_TargetDevice -> TGID_T -> string -> bool -> ConfNode_TargetGroup
    default _.AddTargetGroupNode tdnode argTargetGroupID argTargetGroupName argEnabledAtStart =
        let nid = m_ConfNodes.NextID
        let n =
            new ConfNode_TargetGroup(
                m_MessageTable,
                m_ConfNodes,
                nid,
                argTargetGroupID,
                argTargetGroupName,
                argEnabledAtStart,
                ModifiedStatus.Modified
            )
        m_ConfNodes.AddNode n
        m_ConfNodes.AddRelation ( tdnode :> IConfigureNode ).NodeID nid
        n

    /// <summary>
    ///  Delete target group node.
    /// </summary>
    /// <param name="tgnode">
    ///  The target group node which will be deleted.
    /// </param>
    /// <remarks>
    ///  Target group node which can be deleted must have been modified or the corresponding target group process must have been unloaded.
    /// </remarks>
    abstract DeleteTargetGroupNode : ConfNode_TargetGroup -> unit
    default _.DeleteTargetGroupNode tgnode =
        let tgNodeID = ( tgnode :> IConfigureNode ).NodeID
        m_ConfNodes.DeleteAllChildNodeList tgNodeID

    /// <summary>
    ///  Update target group node.
    /// </summary>
    /// <param name="tgnode">
    ///  The target group node which will be updated.
    /// </param>
    /// <param name="argTargetGroupID">
    ///  Target group ID.
    ///  </param>
    /// <param name="argTargetGroupName">
    ///  Target group name.
    /// </param>
    /// <param name="argEnabledAtStart">
    ///  Flag value of EnabledAtStart.
    /// </param>
    /// <returns>
    ///  Updated target group node.
    /// </returns>
    /// <remarks>
    ///  Target group node which can be updated must have been modified or the corresponding target group process must have been unloaded.
    ///  If a new node is added after being deleted, the node ID will be changed and relational child node are deleted.
    ///  This method can update attribute value without changing node ID and relations.
    /// </remarks>
    abstract UpdateTargetGroupNode : ConfNode_TargetGroup -> TGID_T -> string -> bool -> ConfNode_TargetGroup
    default _.UpdateTargetGroupNode tgnode argTargetGroupID argTargetGroupName argEnabledAtStart =
        let n = tgnode.CreateUpdatedNode argTargetGroupID argTargetGroupName argEnabledAtStart
        m_ConfNodes.Update n
        n

    /// <summary>
    ///  Delete specified node which is descendant of target group node.
    /// </summary>
    /// <param name="argNode">
    ///  The node which will be deleted. It must be descendant of target group node.
    ///  For example, ConfNode_Target, ConfNode_BlockDeviceLU, ConfNode_PlainFileMedia, etc.
    /// </param>
    /// <remarks>
    ///  Target group node which can be updated must have been modified or the corresponding target group process must have been unloaded.
    ///  All descendants of specified node will be deleted. For example, LU that only belongs to specified target are also deleted.
    /// </remarks>
    abstract DeleteNodeInTargetGroup : IConfigureNode -> unit
    default this.DeleteNodeInTargetGroup argNode =
        let tNodeID = argNode.NodeID
        let tgNode = this.IdentifyTargetGroupNode argNode
        m_ConfNodes.DeleteAllChildNodeList tNodeID
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update

    /// <summary>
    ///  Add a target node as a child of the specified target group node.
    /// </summary>
    /// <param name="tgNode">
    ///  The target group node which target node add to.
    /// </param>
    /// <param name="argConf">
    ///  Configuration values of target node. LUN list is ignored.
    /// </param>
    /// <returns>
    ///  Created target node.
    /// </returns>
    /// <remarks>
    ///  Target group node which can be updated must have been modified or the corresponding target group process must have been unloaded.
    ///  Added target node has no LU nodes. So, in this state, configuration files can't be uploaded.
    /// </remarks>
    abstract AddTargetNode : ConfNode_TargetGroup -> TargetGroupConf.T_Target -> ConfNode_Target
    default this.AddTargetNode tgNode argConf =
        let tgNodeID = ( tgNode :> IConfigureNode ).NodeID
        let n = new ConfNode_Target( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, argConf )
        m_ConfNodes.AddNode n
        m_ConfNodes.AddRelation tgNodeID ( n :> IConfigureNode ).NodeID
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Update target node.
    /// </summary>
    /// <param name="tnode">
    ///  The target node which will be updated.
    /// </param>
    /// <param name="argConf">
    ///  Configuration values of target node. LUN list is ignored.
    /// </param>
    /// <returns>
    ///  Updated target node.
    /// </returns>
    /// <remarks>
    ///  Target group node which can be updated must have been modified or the corresponding target group process must have been unloaded.
    ///  If a new node is added after being deleted, the node ID will be changed and relational child node are deleted.
    ///  This method can update attribute value without changing node ID and relations.
    /// </remarks>
    abstract UpdateTargetNode : ConfNode_Target -> TargetGroupConf.T_Target -> ConfNode_Target
    default this.UpdateTargetNode tnode argConf =
        let tgNode = this.IdentifyTargetGroupNode tnode
        let n = tnode.CreateUpdatedNode argConf
        m_ConfNodes.Update n
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Add relation of target node and LU node.
    /// </summary>
    /// <param name="tNode">
    ///  Parent node.
    /// </param>
    /// <param name="luNode">
    ///  Child node.
    /// </param>
    abstract AddTargetLURelation : ConfNode_Target -> ILUNode -> unit
    default this.AddTargetLURelation tNode luNode =
        let tgNode = this.IdentifyTargetGroupNode tNode
        m_ConfNodes.AddRelation ( tNode :> IConfigureNode ).NodeID ( luNode :> IConfigureNode ).NodeID
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update

    /// <summary>
    ///  Delete relation of target node and LU node.
    /// </summary>
    /// <param name="tNode">
    ///  Parent node.
    /// </param>
    /// <param name="luNode">
    ///  Child node.
    /// </param>
    /// <remarks>
    ///  If the parent of the specified LU node no longer exists, that LU node will also be deleted.
    /// </remarks>
    abstract DeleteTargetLURelation : ConfNode_Target -> ILUNode -> unit
    default this.DeleteTargetLURelation tNode luNode =
        let tgNode = this.IdentifyTargetGroupNode tNode
        // Delete relation
        m_ConfNodes.DeleteRelation ( tNode :> IConfigureNode ).NodeID luNode.NodeID
        // If LU node has no parent, delete that LU node.
        if luNode.GetParentNodes<IConfigureNode>().Length = 0 then
            m_ConfNodes.DeleteAllChildNodeList luNode.NodeID
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update

    /// <summary>
    ///  Add block device LU node as a child of the specified target node.
    /// </summary>
    /// <param name="tnode">
    ///  The target node which newly created block device node will be added to.
    /// </param>
    /// <param name="argLUN">
    ///  LUN of the new LU.
    /// </param>
    /// <param name="argLUName">
    ///  LU name of the new LU.
    /// </param>
    /// <returns>
    ///  Created block device LU node.
    ///  Added LU node has no media nodes. So, in this state, configuration files can't be uploaded.
    /// </returns>
    abstract AddBlockDeviceLUNode : ConfNode_Target -> LUN_T -> string -> ConfNode_BlockDeviceLU
    default this.AddBlockDeviceLUNode tnode argLUN argLUName =
        let tgNode = this.IdentifyTargetGroupNode tnode
        let n = new ConfNode_BlockDeviceLU( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, argLUN, argLUName )
        m_ConfNodes.AddNode n
        m_ConfNodes.AddRelation ( tnode :> IConfigureNode ).NodeID  ( n :> IConfigureNode ).NodeID
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Add block device LU node as a child of the specified target group node.
    /// </summary>
    /// <param name="tgnode">
    ///  The  group node which newly created block device node will be added to.
    /// </param>
    /// <param name="argLUN">
    ///  LUN of the new LU.
    /// </param>
    /// <param name="argLUName">
    ///  LU name of the new LU.
    /// </param>
    /// <returns>
    ///  Created block device LU node.
    ///  Added LU node has no media nodes. So, in this state, configuration files can't be uploaded.
    /// </returns>
    /// <remarks>
    ///  The block device LU node will be child of specified target group node.
    ///  So this LU is no accessible from any target until adding relation from one.
    /// </remarks>
    abstract AddBlockDeviceLUNode_InTargetGroup : ConfNode_TargetGroup -> LUN_T -> string -> ConfNode_BlockDeviceLU
    default this.AddBlockDeviceLUNode_InTargetGroup tgnode argLUN argLUName =
        let n = new ConfNode_BlockDeviceLU( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, argLUN, argLUName )
        m_ConfNodes.AddNode n
        m_ConfNodes.AddRelation ( tgnode :> IConfigureNode ).NodeID  ( n :> IConfigureNode ).NodeID
        if ( tgnode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgnode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Update block device LU node.
    /// </summary>
    /// <param name="lunode">
    ///  The dummy device LU node that will be updated.
    /// </param>
    /// <param name="argLUN">
    ///  LUN of the new LU.
    /// </param>
    /// <param name="argLUName">
    ///  LU name of the new LU.
    /// </param>
    /// <returns>
    ///  Updated block device LU node.
    ///  If a new node is added after being deleted, the node ID will be changed and relational child node are deleted.
    ///  This method can update attribute value without changing node ID and relations.
    /// </returns>
    abstract UpdateBlockDeviceLUNode : ConfNode_BlockDeviceLU -> LUN_T -> string -> ConfNode_BlockDeviceLU
    default this.UpdateBlockDeviceLUNode lunode argLUN argLUName =
        let tgNode = this.IdentifyTargetGroupNode lunode
        let n = lunode.CreateUpdatedNode argLUN argLUName
        m_ConfNodes.Update n
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Add dummy device LU node as a child of the specified target node.
    /// </summary>
    /// <param name="tnode">
    ///  The target node which newly created dummy device node will be added to.
    /// </param>
    /// <param name="argLUN">
    ///  LUN of the new LU.
    /// </param>
    /// <param name="argLUName">
    ///  LU name of the new LU.
    /// </param>
    /// <returns>
    ///  Created dummy device LU node.
    ///  Added LU node has no media nodes. So, in this state, configuration files can't be uploaded.
    /// </returns>
    abstract AddDummyDeviceLUNode : ConfNode_Target -> LUN_T -> string -> ConfNode_DummyDeviceLU
    default this.AddDummyDeviceLUNode tnode argLUN argLUName =
        let tgNode = this.IdentifyTargetGroupNode tnode
        let n = new ConfNode_DummyDeviceLU( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, argLUN, argLUName )
        m_ConfNodes.AddNode n
        m_ConfNodes.AddRelation ( tnode :> IConfigureNode ).NodeID  ( n :> IConfigureNode ).NodeID
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Update dummy device LU node.
    /// </summary>
    /// <param name="lunode">
    ///  The dummy device LU node that will be updated.
    /// </param>
    /// <param name="argLUN">
    ///  LUN of the new LU.
    /// </param>
    /// <param name="argLUName">
    ///  LU name of the new LU.
    /// </param>
    /// <returns>
    ///  Updated dummy device LU node.
    ///  If a new node is added after being deleted, the node ID will be changed and relational child node are deleted.
    ///  This method can update attribute value without changing node ID and relations.
    /// </returns>
    abstract UpdateDummyDeviceLUNode : ConfNode_DummyDeviceLU -> LUN_T -> string -> ConfNode_DummyDeviceLU
    default this.UpdateDummyDeviceLUNode lunode argLUN argLUName =
        let tgNode = this.IdentifyTargetGroupNode lunode
        let n = lunode.CreateUpdatedNode argLUN argLUName
        m_ConfNodes.Update n
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Add plain file media node as a child of the specified LU or media node.
    /// </summary>
    /// <param name="parentNode">
    ///  The parent node which newly created media node will be added to.
    /// </param>
    /// <param name="argValue">
    ///  Configuration values of newly created plain file media node.
    /// </param>
    /// <returns>
    ///  Created plain file media node.
    /// </returns>
    abstract AddPlainFileMediaNode : IConfigureNode -> TargetGroupConf.T_PlainFile -> ConfNode_PlainFileMedia
    default this.AddPlainFileMediaNode parentNode argValue =
        let tgNode = this.IdentifyTargetGroupNode parentNode
        let n = new ConfNode_PlainFileMedia( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, argValue )
        m_ConfNodes.AddNode n
        m_ConfNodes.AddRelation parentNode.NodeID  ( n :> IConfigureNode ).NodeID
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Update plain file media node.
    /// </summary>
    /// <param name="mediaNode">
    ///  The plain file media node that will be updated.
    /// </param>
    /// <param name="argValue">
    ///  Configuration values of newly created plain file media node.
    /// </param>
    /// <returns>
    ///  Updated plain file media node.
    ///  If a new node is added after being deleted, the node ID will be changed and relational child node are deleted.
    ///  This method can update attribute value without changing node ID and relations.
    /// </returns>
    abstract UpdatePlainFileMediaNode : ConfNode_PlainFileMedia -> TargetGroupConf.T_PlainFile -> ConfNode_PlainFileMedia
    default this.UpdatePlainFileMediaNode mediaNode argValue =
        let tgNode = this.IdentifyTargetGroupNode mediaNode
        let n = mediaNode.CreateUpdatedNode argValue
        m_ConfNodes.Update n
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Add memory buffer media node as a child of the specified LU or media node.
    /// </summary>
    /// <param name="parentNode">
    ///  The parent node which newly created media node will be added to.
    /// </param>
    /// <param name="argConf">
    ///  Configuration values of newly created memory buffer media node..
    /// </param>
    /// <returns>
    ///  Created memory buffer media node.
    /// </returns>
    abstract AddMemBufferMediaNode : IConfigureNode -> TargetGroupConf.T_MemBuffer -> ConfNode_MemBufferMedia
    default this.AddMemBufferMediaNode parentNode argConf =
        let tgNode = this.IdentifyTargetGroupNode parentNode
        let n = new ConfNode_MemBufferMedia( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, argConf )
        m_ConfNodes.AddNode n
        m_ConfNodes.AddRelation parentNode.NodeID  ( n :> IConfigureNode ).NodeID
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Update memory buffer media node.
    /// </summary>
    /// <param name="mediaNode">
    ///  The plain file media node that will be updated.
    /// </param>
   /// <param name="argConf">
    ///  Configuration values of newly created memory buffer media node..
    /// </param>
    /// <returns>
    ///  Updated memory buffer media node.
    ///  If a new node is added after being deleted, the node ID will be changed and relational child node are deleted.
    ///  This method can update attribute value without changing node ID and relations.
    /// </returns>
    abstract UpdateMemBufferMediaNode : ConfNode_MemBufferMedia -> TargetGroupConf.T_MemBuffer -> ConfNode_MemBufferMedia
    default this.UpdateMemBufferMediaNode mediaNode argConf =
        let tgNode = this.IdentifyTargetGroupNode mediaNode
        let n = mediaNode.CreateUpdatedNode argConf
        m_ConfNodes.Update n
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Add dummy media node as a child of the specified LU or media node.
    /// </summary>
    /// <param name="parentNode">
    ///  The parent node which newly created media node will be added to.
    /// </param>
    /// <param name="argIdent">
    ///  Media identifier number.
    /// </param>
    /// <param name="argName">
    ///  Media name.
    /// </param>
    /// <returns>
    ///  Created dummy media node.
    /// </returns>
    abstract AddDummyMediaNode : IConfigureNode -> MEDIAIDX_T -> string -> ConfNode_DummyMedia
    default this.AddDummyMediaNode parentNode argIdent argName =
        let tgNode = this.IdentifyTargetGroupNode parentNode
        let n = new ConfNode_DummyMedia( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, argIdent, argName )
        m_ConfNodes.AddNode n
        m_ConfNodes.AddRelation parentNode.NodeID  ( n :> IConfigureNode ).NodeID
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Update dummy media node.
    /// </summary>
    /// <param name="mediaNode">
    ///  The plain file media node that will be updated.
    /// </param>
    /// <param name="argIdent">
    ///  Media identifier number.
    /// </param>
    /// <returns>
    ///  Updated dummy media node.
    ///  If a new node is added after being deleted, the node ID will be changed and relational child node are deleted.
    ///  This method can update attribute value without changing node ID and relations.
    /// </returns>
    abstract UpdateDummyMediaNode : ConfNode_DummyMedia -> MEDIAIDX_T -> string -> ConfNode_DummyMedia
    default this.UpdateDummyMediaNode mediaNode argIdent argName =
        let tgNode = this.IdentifyTargetGroupNode mediaNode
        let n = mediaNode.CreateUpdatedNode argIdent argName
        m_ConfNodes.Update n
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Add debug media node as a child of the specified LU or media node.
    /// </summary>
    /// <param name="parentNode">
    ///  The parent node which newly created media node will be added to.
    /// </param>
    /// <param name="argIdent">
    ///  Media identifier number.
    /// </param>
    /// <param name="argName">
    ///  Media name.
    /// </param>
    /// <returns>
    ///  Created debug media node.
    /// </returns>
    abstract AddDebugMediaNode : IConfigureNode -> MEDIAIDX_T -> string -> ConfNode_DebugMedia
    default this.AddDebugMediaNode parentNode argIdent argName =
        let tgNode = this.IdentifyTargetGroupNode parentNode
        let n = new ConfNode_DebugMedia( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, argIdent, argName )
        m_ConfNodes.AddNode n
        m_ConfNodes.AddRelation parentNode.NodeID  ( n :> IConfigureNode ).NodeID
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Update debug buffer media node.
    /// </summary>
    /// <param name="mediaNode">
    ///  The plain file media node that will be updated.
    /// </param>
    /// <param name="argIdent">
    ///  Media identifier number.
    /// </param>
    /// <param name="argName">
    ///  Media name.
    /// </param>
    /// <returns>
    ///  Updated debug media node.
    ///  If a new node is added after being deleted, the node ID will be changed and relational child node are deleted.
    ///  This method can update attribute value without changing node ID and relations.
    /// </returns>
    abstract UpdateDebugMediaNode : ConfNode_DebugMedia -> MEDIAIDX_T -> string -> ConfNode_DebugMedia
    default this.UpdateDebugMediaNode mediaNode argIdent argName =
        let tgNode = this.IdentifyTargetGroupNode mediaNode
        let n = mediaNode.CreateUpdatedNode argIdent argName
        m_ConfNodes.Update n
        if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
            tgNode.SetModified() |> m_ConfNodes.Update
        n

    /// <summary>
    ///  Search and return the target device node that holds specified node.
    /// </summary>
    /// <param name="node">
    ///  Descendant node from which to search for the target device node.
    /// </param>
    /// <returns>
    ///  The target device node that holds specified node, or None if failed to search.
    /// </returns>
    /// <remarks>
    ///  If specified node at argument is a target device node, this function returns specified node.
    /// </remarks>
    abstract GetAncestorTargetDevice : IConfigureNode -> ConfNode_TargetDevice option
    default this.GetAncestorTargetDevice node =
        match node with
        | :? ConfNode_TargetDevice as x ->
            Some x
        | _ ->
            try
                Some <| this.IdentifyTargetDeviceNode node
            with
            | _ -> None

    /// <summary>
    ///  Search and return the target group node that holds specified node.
    /// </summary>
    /// <param name="node">
    ///  Descendant node from which to search for the target group node.
    /// </param>
    /// <returns>
    ///  The target group node that holds specified node, or None if failed to search.
    /// </returns>
    /// <remarks>
    ///  If specified node at argument is a target group node, this function returns specified node.
    /// </remarks>
    abstract GetAncestorTargetGroup : IConfigureNode -> ConfNode_TargetGroup option
    default this.GetAncestorTargetGroup node =
        match node with
        | :? ConfNode_TargetGroup as x ->
            Some x
        | _ ->
            try
                Some <| this.IdentifyTargetGroupNode node
            with
            | _ -> None

    /// <summary>
    ///  Search and return the logical unit node that holds specified node.
    /// </summary>
    /// <param name="node">
    ///  Descendant node from which to search for the target group node.
    /// </param>
    /// <returns>
    ///  The logical unit node that holds specified node, or None if failed to search.
    /// </returns>
    /// <remarks>
    ///  If specified node at argument is a logical unit node, this function returns specified node.
    /// </remarks>
    abstract GetAncestorLogicalUnit : IConfigureNode -> ILUNode option
    default _.GetAncestorLogicalUnit node =
        match node with
        | :? ILUNode as x ->
            Some x
        | _ ->
            let parents = m_ConfNodes.GetAllParentNodeList<ILUNode> node.NodeID
            if parents.Length <> 1 then
                None
            else
                Some( parents.[0] )

    /// <summary>
    ///  Search for the target device node which specified node is belongs to.
    /// </summary>
    /// <param name="node">
    ///  The node that is descendants of a target device node.
    /// </param>
    /// <returns>
    ///  Target device node.
    /// </returns>
    /// <exceptions>
    ///  If failed to search for target device node, it raise EditError exception.
    /// </exceptions>
    /// <remarks>
    ///  If specified node is a target device node, this function raise EditError exception.
    /// </remarks>
    member private _.IdentifyTargetDeviceNode ( node : IConfigureNode ) : ConfNode_TargetDevice =
        let tdNodes = m_ConfNodes.GetAllParentNodeList<ConfNode_TargetDevice> node.NodeID
        if tdNodes.Length <> 1 then
            let i = confnode_me.toPrim node.NodeID
            raise <| EditError( m_MessageTable.GetMessage( "ERRMSG_FAILED_IDENT_TARGET_DEVICE", i.ToString() ) )
        tdNodes.[0]

    /// <summary>
    ///  Search for the target group node which specified node is belongs to.
    /// </summary>
    /// <param name="node">
    ///  The node that is descendants of a target group node.
    /// </param>
    /// <returns>
    ///  Target group node.
    /// </returns>
    /// <exceptions>
    ///  If failed to search for target group node, it raise EditError exception.
    /// </exceptions>
    /// <remarks>
    ///  If specified node is a target group node, this function raise EditError exception.
    /// </remarks>
    member private _.IdentifyTargetGroupNode ( node : IConfigureNode ) : ConfNode_TargetGroup =
        let tgNodes = m_ConfNodes.GetAllParentNodeList<ConfNode_TargetGroup> node.NodeID
        if tgNodes.Length <> 1 then
            let i = confnode_me.toPrim node.NodeID
            raise <| EditError( m_MessageTable.GetMessage( "ERRMSG_FAILED_IDENT_TARGET_GROUP", i.ToString() ) )
        tgNodes.[0]

    /// <summary>
    ///  Check specified target device is unloaded or not.
    /// </summary>
    /// <param name="con">
    ///  Connection object to the controller.
    /// </param>
    /// <param name="tdNode">
    ///  Target device node.
    /// </param>
    /// <returns>
    ///  If the ancestor target group is not unloaded, it returns false, otherwise true.
    /// </returns>
    abstract TryCheckTargetDeviceUnloaded : CtrlConnection -> IConfigureNode -> Task<bool>
    default this.TryCheckTargetDeviceUnloaded ( con : CtrlConnection ) ( node : IConfigureNode ) : Task<bool> =
        task {
            let tdNode =
                match node with
                | :? ConfNode_TargetDevice as x ->
                    x
                | _ ->
                    this.IdentifyTargetDeviceNode node
            if ( tdNode :> IConfigFileNode ).Modified = ModifiedStatus.Modified then
                return true
            else
                // check corresponding target device process has been terminated or not.
                let! tdlist = con.GetTargetDeviceProcs()
                return not( List.exists ( (=) tdNode.TargetDeviceID ) tdlist )
                //raise <| EditError( m_MessageTable.GetMessage( "ERRMSG_TARGET_DEVICE_RUNNING" ) )
        }

    /// <summary>
    ///  Check specified target device is unloaded or not. If target device is sitll running, it raise to an exception.
    /// </summary>
    /// <param name="con">
    ///  Connection object to the controller.
    /// </param>
    /// <param name="tdNode">
    ///  Target device node.
    /// </param>
    /// <exceptions>
    ///  If specified target device is running, EditError exception is raised.
    /// </exceptions>
    abstract CheckTargetDeviceUnloaded : CtrlConnection -> IConfigureNode -> Task
    default this.CheckTargetDeviceUnloaded ( con : CtrlConnection ) ( node : IConfigureNode ) : Task =
        task {
            let! r = this.TryCheckTargetDeviceUnloaded con node
            if not r then
                raise <| EditError( m_MessageTable.GetMessage( "ERRMSG_TARGET_DEVICE_RUNNING" ) )
        }

    /// <summary>
    ///  Check specified target group is unloaded or not.
    /// </summary>
    /// <param name="con">
    ///  Connection object to the controller.
    /// </param>
    /// <param name="node">
    ///  Nodes that are descendants of the target group.
    /// </param>
    /// <returns>
    ///  If the ancestor target group is not unloaded, it returns false, otherwise true.
    /// </returns>
    abstract TryCheckTargetGroupUnloaded : CtrlConnection -> IConfigureNode -> Task<bool>
    default this.TryCheckTargetGroupUnloaded ( con : CtrlConnection ) ( node : IConfigureNode ) : Task<bool> =
        task {
            let tdNode = this.IdentifyTargetDeviceNode node
            let tgNode =
                match node with
                | :? ConfNode_TargetGroup as x ->
                    x
                | _ ->
                    this.IdentifyTargetGroupNode node
            if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.Modified then
                return true
            else
                // If the target device is unloaded, the target group it belongs to has already been unloaded.
                let! tdlist = con.GetTargetDeviceProcs()
                if List.exists ( (=) tdNode.TargetDeviceID ) tdlist then
                    // check corresponding target group has been unloaded or not.
                    let! wlist = con.GetLoadedTargetGroups ( tdNode.TargetDeviceID )
                    return 
                        wlist
                        |> List.map _.ID
                        |> List.exists ( (=) tgNode.TargetGroupID )
                        |> not
                else
                    return true
        }

    /// <summary>
    ///  Check specified target group is unloaded or not. If target group is sitll loaded or activated, it raise to an exception.
    /// </summary>
    /// <param name="con">
    ///  Connection object to the controller.
    /// </param>
    /// <param name="node">
    ///  Nodes that are descendants of the target group.
    /// </param>
    /// <exceptions>
    ///  If the ancestor target group is not unloaded, EditError exception is raised.
    /// </exceptions>
    abstract CheckTargetGroupUnloaded : CtrlConnection -> IConfigureNode -> Task
    default this.CheckTargetGroupUnloaded ( con : CtrlConnection ) ( node : IConfigureNode ) : Task =
        task {
            let! r = this.TryCheckTargetGroupUnloaded con node
            if not r then
                raise <| EditError( m_MessageTable.GetMessage( "ERRMSG_TARGET_GROUP_LOADED" ) )
        }

    /// <summary>
    ///  Export temporary dump data.
    /// </summary>
    /// <param name="n">
    ///  Node ID should be exported.
    /// </param>
    /// <param name="recursive">
    ///  Output all descendants nodes or not.
    /// </param>
    /// <returns>
    ///  Temporary dump data.
    /// </returns>
    /// <remarks>
    ///  The node specified at 'n' and all of children are exported.
    /// </remarks>
    abstract ExportTemporaryDump : CONFNODE_T -> bool -> string
    default this.ExportTemporaryDump ( n : CONFNODE_T ) ( recursive : bool ) : string =
        let outputNodes = [
            yield m_ConfNodes.GetNode n
            if recursive then
                yield! m_ConfNodes.GetAllChildNodeList<IConfigureNode> n
        ]
        let t : TempExport.T_ExportData = {
            AppVersion = {
                Major = Constants.MAJOR_VERSION;
                Minor = Constants.MINOR_VERSION;
                Rivision = Constants.PRODUCT_RIVISION;
            };
            RootNode = n |> confnode_me.toPrim;
            Relationship = 
                if recursive then
                    outputNodes
                    |> Seq.map _.NodeID
                    |> Seq.map ( fun itr ->
                        {
                            NodeID = itr |> confnode_me.toPrim; 
                            Child = m_ConfNodes.GetChild itr |> List.map confnode_me.toPrim;
                        } : TempExport.T_Relationship
                    )
                    |> Seq.toList
                else
                    [{
                        NodeID = confnode_me.toPrim n; 
                        Child = [];
                    }]
            Node = 
                outputNodes
                |> List.map _.TempExportData
        }
        TempExport.ReaderWriter.ToString t

    /// <summary>
    ///  Import temporary dump data to the shild of specified node.
    /// </summary>
    /// <param name="testr">
    ///  The temporary dump data.
    /// </param>
    /// <param name="n">
    ///  Specify the node that should be the parent of the loaded data.
    /// </param>
    /// <param name="recursive">
    ///  Import all descendants nodes or not.
    /// </param>
    /// <returns>
    ///  Loaded node.
    /// </returns>
    /// <remarks>
    ///  As long as there are no fatal errors, it will try to read as much as possible even if there are errors in the data.
    ///  Therefore, the data loaded may not necessarily match the data exported.
    /// </remarks>
    abstract ImportTemporaryDump : string -> CONFNODE_T -> bool -> IConfigureNode
    default this.ImportTemporaryDump ( testr: string ) ( n : CONFNODE_T ) ( recursive : bool ) : IConfigureNode =
        let t = TempExport.ReaderWriter.LoadString testr
        let rootNodeID = t.RootNode

        // check data version
        if t.AppVersion.Major > Constants.MAJOR_VERSION ||
            t.AppVersion.Major = Constants.MAJOR_VERSION && t.AppVersion.Minor > Constants.MINOR_VERSION then
                raise <| EditError( m_MessageTable.GetMessage( "ERRMSG_TEMP_EXPORT_VERSION_MISMATCH" ) )

        // Load all node
        let d =
            t.Node
            |> Seq.filter ( fun itr -> if recursive then true else itr.NodeID = rootNodeID )
            |> Seq.choose ( fun itr ->
                if itr.TypeName = ClientConst.TEMPEXP_NN_Controller then
                    Some( itr.NodeID, new ConfNode_Controller( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, itr ) :> IConfigureNode )
                elif itr.TypeName = ClientConst.TEMPEXP_NN_TargetDevice then
                    Some( itr.NodeID, new ConfNode_TargetDevice( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, itr ) :> IConfigureNode )
                elif itr.TypeName = ClientConst.TEMPEXP_NN_NetworkPortal then
                    Some( itr.NodeID, new ConfNode_NetworkPortal( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, itr ) :> IConfigureNode )
                elif itr.TypeName = ClientConst.TEMPEXP_NN_TargetGroup then
                    Some( itr.NodeID, new ConfNode_TargetGroup( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, itr ) :> IConfigureNode )
                elif itr.TypeName = ClientConst.TEMPEXP_NN_Target then
                    Some( itr.NodeID, new ConfNode_Target( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, itr ) :> IConfigureNode )
                elif itr.TypeName = ClientConst.TEMPEXP_NN_DummyDeviceLU then
                    Some( itr.NodeID, new ConfNode_DummyDeviceLU( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, itr ) :> IConfigureNode )
                elif itr.TypeName =  ClientConst.TEMPEXP_NN_BlockDeviceLU then
                    Some( itr.NodeID, new ConfNode_BlockDeviceLU( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, itr ) :> IConfigureNode )
                elif itr.TypeName = ClientConst.TEMPEXP_NN_PlainFileMedia then
                    Some( itr.NodeID, new ConfNode_PlainFileMedia( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, itr ) :> IConfigureNode )
                elif itr.TypeName = ClientConst.TEMPEXP_NN_MemBufferMedia then
                    Some( itr.NodeID, new ConfNode_MemBufferMedia( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, itr ) :> IConfigureNode )
                elif itr.TypeName = ClientConst.TEMPEXP_NN_DummyMedia then
                    Some( itr.NodeID, new ConfNode_DummyMedia( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, itr ) :> IConfigureNode )
                elif itr.TypeName = ClientConst.TEMPEXP_NN_DebugMedia then
                    Some( itr.NodeID, new ConfNode_DebugMedia( m_MessageTable, m_ConfNodes, m_ConfNodes.NextID, itr ) :> IConfigureNode )
                else
                    None
            )
            |> Seq.map KeyValuePair
            |> Dictionary

        // check root node exists
        let addedRootNode =
            let r, v = d.TryGetValue rootNodeID
            if not r then
                raise <| EditError( m_MessageTable.GetMessage( "ERRMSG_TEMP_EXPORT_MISSING_ROOT" ) )
            v

        // Add loaded nodes
        let rec loop ( np : CONFNODE_T ) ( oc : uint64 ) =
            let r, v = d.TryGetValue oc
            let rel = t.Relationship |> Seq.tryFind ( fun itr -> itr.NodeID = oc )
            if r then
                if m_ConfNodes.Exists v.NodeID then
                    m_ConfNodes.AddRelation np v.NodeID
                else
                    m_ConfNodes.AddNode v
                    m_ConfNodes.AddRelation np v.NodeID
                    if rel.IsSome then
                        rel.Value.Child
                        |> List.iter ( loop v.NodeID )
        loop n rootNodeID

        // Set modified flag
        let parentNode = m_ConfNodes.GetNode n
        match parentNode with
        | :? ConfNode_Controller as x ->
            ()  // Nothing to do

        | :? ConfNode_TargetDevice as x ->
            match addedRootNode with
            | :? ConfNode_TargetGroup ->
                ()  // Nothing to do
            | _ ->
                if ( x :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
                    x.SetModified() |> m_ConfNodes.Update

        | :? ConfNode_NetworkPortal as x ->
            let tdNode = this.IdentifyTargetDeviceNode x
            if ( tdNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
                tdNode.SetModified() |> m_ConfNodes.Update

        | :? ConfNode_TargetGroup as x ->
            if ( x :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
                x.SetModified() |> m_ConfNodes.Update

        | _ as x ->
            let tgNode = this.IdentifyTargetGroupNode x
            if ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.NotModified then
                tgNode.SetModified() |> m_ConfNodes.Update

        // return loaded node
        addedRootNode


