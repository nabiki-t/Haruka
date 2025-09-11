//=============================================================================
// Haruka Software Storage.
// ConfigureNode.fs : It defines commonly used interface at configuration object.
//


//=============================================================================
// Namespace declaration

namespace Haruka.Client

//=============================================================================
// Import declaration

open System

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Type definition

/// values in that node has been updated from when loaded from controller, or not.
type ModifiedStatus =
    | Modified
    | NotModified

/// Measure for configuration node ID.
[<Measure>]
type confnode_me =

    /// <summary>Convert confnode_me value to primitive value.</summary>
    /// <param name="v">confnode_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint64<confnode_me> ) : uint64 =
        uint64 v

    /// <summary>Convert primitive value to confnode_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint64 ) : uint64<confnode_me> =
        ( uint64 v ) * 1UL<confnode_me>

    /// zero value fo confnode_me
    static member zero = 0UL<confnode_me>

/// Data types of CONFNODE_T
type CONFNODE_T = uint64<confnode_me>

//=============================================================================
// Interface declaration

/// Interface impremented all of configuration node object.
type IConfigureNode =

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Validate configuration.
    /// </summary>
    /// <param name="msgList">
    ///   Error message list in progress.
    /// </param>
    /// <returns>
    ///   Error message list with processing results added to msgList.
    /// </returns>
    abstract Validate : msgList : ( CONFNODE_T * string ) list -> ( CONFNODE_T * string ) list

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get node ID of this object.
    /// </summary>
    /// <returns>
    ///   Configuration node ID.
    /// </returns>
    abstract NodeID : CONFNODE_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get node type string name.
    /// </summary>
    /// <returns>
    ///   string that represents configuration node type.
    /// </returns>
    abstract NodeTypeName : string

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get child node list.
    /// </summary>
    /// <returns>
    ///   List of nodes that belongs to this node.
    /// </returns>
    abstract GetChildNodes< 'T when 'T :> IConfigureNode > : unit -> 'T list

    /// <summary>
    /// Get all child nodes recursively.
    /// </summary>
    /// <returns>
    /// A list of the node's children and descendant nodes
    /// </returns>
    abstract GetDescendantNodes< 'T when 'T :> IConfigureNode > : unit -> 'T list

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get parent node list.
    /// </summary>
    /// <returns>
    ///   List of nodes that own this node.
    /// </returns>
    abstract GetParentNodes< 'T when 'T :> IConfigureNode > : unit -> 'T list

    /// <summary>
    /// Get all parent nodes recursively.
    /// </summary>
    /// <returns>
    /// A list of the parent and ancestor nodes of this node
    /// </returns>
    abstract GetAncestorNode< 'T when 'T :> IConfigureNode > : unit -> 'T list

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get minimized description string.
    /// </summary>
    /// <returns>
    ///   Human readable node name.
    /// </returns>
    abstract MinDescriptString : string

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get short description string.
    /// </summary>
    /// <returns>
    ///   Human readable, one-line string that descripts this node.
    /// </returns>
    abstract ShortDescriptString : string

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get full description string.
    /// </summary>
    /// <returns>
    ///   Human readable strings that descripts all values of this node.
    /// </returns>
    abstract FullDescriptString : string list

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the sort key used to determine the ordering of nodes.
    /// </summary>
    abstract SortKey : string list

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get node data for tempolary export.
    /// </summary>
    abstract TempExportData : TempExport.T_Node



/// Interface impremented by the node which creates the configuration file.
type IConfigFileNode =
    inherit IConfigureNode

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Modified or not.
    /// </summary>
    abstract Modified : ModifiedStatus

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Set modified flag to NotModified.
    /// </summary>
    abstract ResetModifiedFlag : unit -> IConfigFileNode


/// Interface impremented by the media node.
type IMediaNode =
    inherit IConfigureNode

    // ------------------------------------------------------------------------
    /// <summary>
    /// Get media configuration data.
    /// </summary>
    abstract MediaConfData : TargetGroupConf.T_MEDIA

    // ------------------------------------------------------------------------
    /// <summary>
    /// Get media identifier number.
    /// </summary>
    abstract IdentNumber : MEDIAIDX_T

    // ------------------------------------------------------------------------
    /// <summary>
    /// Get media name.
    /// </summary>
    abstract Name : string


/// Interface impremented by the LU node.
type ILUNode =
    inherit IConfigureNode

    // ------------------------------------------------------------------------
    /// <summary>
    /// property of LUN
    /// </summary>
    abstract LUN : LUN_T

    // ------------------------------------------------------------------------
    /// <summary>
    /// property of LUName
    /// </summary>
    abstract LUName : string

    // ------------------------------------------------------------------------
    /// <summary>
    /// Get configuration data
    /// </summary>
    abstract LUConfData : TargetGroupConf.T_LogicalUnit


