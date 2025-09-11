//=============================================================================
// Haruka Software Storage.
// ConfNodeRelation.fs : It defines ConfNodeRelation class that holds configuration node and 
// managements relationship of that nodes.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Client

//=============================================================================
// Import declaration

open System
open System.Collections.Generic
open System.Threading

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

/// This class holds configuration node and managements relationship of nodes.
type ConfNodeRelation() =

    /// CONFNODE_T value generator
    let mutable m_ElemCounter : uint64 = 0UL

    /// configuration nodes
    let m_Compornents = Dictionary< CONFNODE_T, IConfigureNode >()

    /// Parent to child relationship
    let m_P_C_Relation = Dictionary< CONFNODE_T, HashSet< CONFNODE_T > >()

    /// Child to parent relationship
    let m_C_P_Relation = Dictionary< CONFNODE_T, HashSet< CONFNODE_T > >()

    /// <summary>
    /// Clear all of nodes.
    /// </summary>
    member _.Initialize() : unit =
        // controller node has always 1 as node ID.
        m_ElemCounter <- 0UL
        m_Compornents.Clear()
        m_P_C_Relation.Clear()
        m_C_P_Relation.Clear()

    /// <summary>
    /// Get next CONFNODE_T value
    /// </summary>
    member _.NextID : CONFNODE_T =
        Interlocked.Increment( &m_ElemCounter ) |> confnode_me.fromPrim

    /// <summary>
    ///  Get all of nodes
    /// </summary>
    member _.AllNodes : Dictionary< CONFNODE_T, IConfigureNode >.ValueCollection =
        m_Compornents.Values

    /// <summary>
    /// Add new configuration node.
    /// </summary>
    /// <param name="node">
    ///  Node object that is inserted.
    /// </param>
    /// <remarks>
    ///  Node ID must not duplicate.
    ///  Added node has no relationships to other node.
    /// </remarks>
    member _.AddNode ( node : IConfigureNode ) : unit =
        let curID = node.NodeID
        m_Compornents.Add( curID, node )
        m_P_C_Relation.Add( curID, HashSet() )
        m_C_P_Relation.Add( curID, HashSet() )

    /// <summary>
    ///  Check specified node is exists or not.
    /// </summary>
    /// <param name="n">
    ///  Node ID.
    /// </param>
    /// <returns>
    ///  If specified node 'n' is registered already, it returns true. Otherwise false.
    /// </returns>
    member _.Exists ( n : CONFNODE_T ) : bool =
        m_Compornents.ContainsKey n

    /// <summary>
    /// Add new configuration node.
    /// </summary>
    /// <param name="parent">
    ///  Parent side tarminal of reration which will be added. 
    /// </param>
    /// <param name="child">
    ///  Child side tarminal of reration which will be added.
    /// </param>
    /// <remarks>
    ///  Parent and child node ID must exist.
    /// </remarks>
    member _.AddRelation ( parent : CONFNODE_T ) ( child : CONFNODE_T ) : unit =
        // Add parent to child relation.
        let  hc = m_P_C_Relation.Item parent
        hc.Add child |> ignore

        // Add child to parent relation
        let hp = m_C_P_Relation.Item child
        hp.Add parent |> ignore

    /// <summary>
    /// Delete existing relationship between two nodes.
    /// </summary>
    /// <param name="parent">
    ///  Parent side tarminal of reration which will be deleted. 
    /// </param>
    /// <param name="child">
    ///  Child side tarminal of reration which will be deleted.
    /// </param>
    /// <remarks>
    ///  Parent and child node ID must exist.
    /// </remarks>
    member _.DeleteRelation ( parent : CONFNODE_T ) ( child : CONFNODE_T ) : unit =
        // Delete parent to child reration
        let  hc = m_P_C_Relation.Item parent
        hc.Remove child |> ignore

        // Relete child to parent relation
        let hp = m_C_P_Relation.Item child
        hp.Remove parent |> ignore


    /// <summary>
    /// Delete cofiguration node.
    /// </summary>
    /// <param name="nodeID"> 
    /// Node ID of node that should be deleted.
    /// </param>
    member _.Delete ( nodeID : CONFNODE_T ) : unit =

        // Delete parent nodes to current node relations
        if m_C_P_Relation.ContainsKey nodeID then
            for itr in m_C_P_Relation.Item nodeID do
                ( m_P_C_Relation.Item itr ).Remove nodeID |> ignore

        // Delete child nodes to current node relations
        if m_P_C_Relation.ContainsKey nodeID then
            for itr in m_P_C_Relation.Item nodeID do
                ( m_C_P_Relation.Item itr ).Remove nodeID |> ignore

        // Delete current node to parent nodes relations
        m_C_P_Relation.Remove nodeID |> ignore

        // Delete current node to child nodes relations
        m_P_C_Relation.Remove nodeID |> ignore

        // delete current node
        m_Compornents.Remove nodeID |> ignore


    /// <summary>
    /// Update cofiguration node, that has same Node ID.
    /// </summary>
    /// <param name="newNode">
    /// New node.
    /// </param>
    /// <remarks>
    ///  Old and new node must have same Node ID.
    /// </remarks>
    member _.Update ( newNode : IConfigureNode ) : unit =
        m_Compornents.Remove newNode.NodeID |> ignore
        m_Compornents.Add( newNode.NodeID, newNode )

    /// <summary>
    /// Get nofiguration node
    /// </summary>
    /// <param name="nodeID">
    /// Node ID.
    /// </param>
    /// <returns>
    /// Node object that is specified by argument node ID.
    /// </returns>
    member _.GetNode ( nodeID : CONFNODE_T ) : IConfigureNode =
        m_Compornents.Item( nodeID )

    /// <summary>
    /// Get child node IDs..
    /// </summary>
    /// <param name="nodeID">
    /// Node ID for which is searched for child nodes.
    /// </param>
    /// <returns>
    /// IDs of child nodes.
    /// </returns>
    member _.GetChild ( nodeID : CONFNODE_T ) : CONFNODE_T list =
        m_P_C_Relation.Item nodeID
        |> Seq.sort
        |> Seq.toList

    /// <summary>
    /// Get child nodes.
    /// </summary>
    /// <param name="nodeID">
    /// Node ID for which is searched for child nodes.
    /// </param>
    /// <returns>
    /// Child nodes list.
    /// </returns>
    member _.GetChildNodeList< 'T when 'T :> IConfigureNode > ( nodeID : CONFNODE_T ) : 'T list =
        m_P_C_Relation.Item nodeID
        |> Seq.sort
        |> Seq.choose ( fun itr ->
            match m_Compornents.Item itr with
            | :? 'T as x -> Some x
            | _ -> None
        )
        |> Seq.toList

    /// <summary>
    /// Get parent node IDs list.
    /// </summary>
    /// <param name="nodeID">
    /// Node ID for which is searched for parent nodes.
    /// </param>
    /// <returns>
    /// IDs of parent nodes.
    /// </returns>
    member _.GetParent ( nodeID : CONFNODE_T ) : CONFNODE_T list =
        m_C_P_Relation.Item nodeID
        |> Seq.sort
        |> Seq.toList

    /// <summary>
    /// Get parent nodes list.
    /// </summary>
    /// <param name="nodeID">
    /// Node ID for which is searched for parent nodes.
    /// </param>
    /// <returns>
    /// Parent nodes list.
    /// </returns>
    member _.GetParentNodeList< 'T when 'T :> IConfigureNode > ( nodeID : CONFNODE_T ) : 'T list =
        m_C_P_Relation.Item nodeID
        |> Seq.sort
        |> Seq.choose ( fun itr ->
            match m_Compornents.Item itr with
            | :? 'T as x -> Some x
            | _ -> None
        )
        |> Seq.toList

    /// <summary>
    /// Get all child nodes recursively.
    /// </summary>
    /// <param name="nodeID">
    /// Node ID for which is searched for child nodes.
    /// </param>
    /// <returns>
    /// Nodes list.
    /// </returns>
    member this.GetAllChildNodeList< 'T when 'T :> IConfigureNode > ( nodeID : CONFNODE_T ) : 'T list =
        let rec loop ( nid : CONFNODE_T ) ( dic : Dictionary< CONFNODE_T, IConfigureNode > ) : Dictionary< CONFNODE_T, IConfigureNode > =
            this.GetChildNodeList<IConfigureNode> nid
            |> List.iter ( fun itr ->
                if dic.TryAdd( itr.NodeID, itr ) then
                    loop itr.NodeID dic |> ignore
            )
            dic
        loop nodeID ( new Dictionary< CONFNODE_T, IConfigureNode >() )
        |> Seq.choose ( fun itr ->
            match itr.Value with
            | :? 'T as x -> Some x
            | _ -> None
        )
        |> Seq.sortBy _.NodeID
        |> Seq.toList

    /// <summary>
    /// Get all parent nodes recursively.
    /// </summary>
    /// <param name="nodeID">
    /// Node ID for which is searched for parent nodes.
    /// </param>
    /// <returns>
    /// Nodes list.
    /// </returns>
    member this.GetAllParentNodeList< 'T when 'T :> IConfigureNode > ( nodeID : CONFNODE_T ) : 'T list =
        let rec loop ( nid : CONFNODE_T ) ( dic : Dictionary< CONFNODE_T, IConfigureNode > ) : Dictionary< CONFNODE_T, IConfigureNode > =
            this.GetParentNodeList<IConfigureNode> nid
            |> List.iter ( fun itr ->
                if dic.TryAdd( itr.NodeID, itr ) then
                    loop itr.NodeID dic |> ignore
            )
            dic
        loop nodeID ( new Dictionary< CONFNODE_T, IConfigureNode >() )
        |> Seq.choose ( fun itr ->
            match itr.Value with
            | :? 'T as x -> Some x
            | _ -> None
        )
        |> Seq.sortBy _.NodeID
        |> Seq.toList

    /// <summary>
    /// Delete all of child nodes recursively.
    /// </summary>
    /// <param name="nodeID">
    /// Node ID for which is deleted.
    /// </param>
    /// <remarks>
    /// If a node is child of another node, it will not be deleted.
    /// If there are circular references among the child nodes, they cannot be deleted.
    /// </remarks>
    member this.DeleteAllChildNodeList ( nodeID : CONFNODE_T ) : unit =
        let w =
            this.GetAllChildNodeList<IConfigureNode> nodeID
            |> Seq.choose ( fun itr -> if itr.NodeID <> nodeID then Some itr.NodeID else None )
            |> Seq.toArray

        // Delete specified node
        this.Delete nodeID

        let rec loop ( li : CONFNODE_T[] ) =
            // search stray child
            let li1, li2 =
                li |> Array.partition ( this.GetParent >> List.isEmpty )
            // If there are stray child, delete this. 
            if li1.Length > 0 then
                li1 |> Array.iter this.Delete
                // Delete newly created strays.
                loop li2
        loop w
