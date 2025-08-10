//=============================================================================
// Haruka Software Storage.
// InitialPropPage.fs : Implement the function to display Initial property page in the main window.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration


open System
open System.IO
open System.Collections.Generic
open System.Xml
open System.Xaml
open System.Windows
open System.Windows.Markup
open System.Windows.Controls
open System.ComponentModel
open System.Globalization
open System.Threading.Tasks

open Haruka.Client
open Haruka.Constants

/// Interface of property page classes.
type IPropPageInterface =
    /// <summary>
    ///  Get loaded property page UI object.
    /// </summary>
    abstract GetUIElement : unit -> UIElement

    /// <summary>
    ///  Set enable or disable to the property page.
    /// </summary>
    /// <param name="isEnable">
    ///  enabled or disabled.
    /// </param>
    abstract SetEnable : isEnable:bool -> unit

    /// This method is called when the property page will be showed.
    abstract InitializeViewContent : unit -> unit

    /// This method is called when status is updated.
    abstract UpdateViewContent : unit -> unit

    /// <summary>
    ///  Set proparty page size
    /// </summary>
    /// <param name="width">
    ///  New property page width.
    /// </param>
    abstract SetPageWidth : width:float -> unit

    /// Notification of closed this page
    abstract OnClosePage : unit -> unit

    /// Get current node ID
    abstract GetNodeID : unit -> CONFNODE_T

/// Interface of The MainWindow class.
/// It is used by the property page classes.
type IMainWindowIFForPP =

    /// <summary>
    ///  Notify of updated configure node status.
    /// </summary>
    /// <param name="node">
    ///  Updated configure node.
    /// </pram>
    /// <remarks>
    ///  The operating status of the target device and target group is based on the cache value.
    /// </remarks>
    abstract NoticeUpdateStat : node:IConfigureNode -> unit

    /// <summary>
    ///  Notify of updated configure node configuration values.
    /// </summary>
    /// <param name="node">
    ///  Updated configure node.
    /// </pram>
    abstract NoticeUpdateConfig : node:IConfigureNode -> unit

    /// <summary>
    ///  Get effective server status object.
    ///  If not logged in, it returns None.
    /// </summary>
    abstract GetServerStatus : unit -> ServerStatus option

    /// <summary>
    ///  Get effective connection object.
    ///  If not logged in, it returns None.
    /// </summary>
    abstract GetCtrlConnection : unit -> CtrlConnection option

    /// <summary>
    ///  Run the task that queries the controller.
    /// </summary>
    /// <param name="setProg">
    ///  Enable progress bar action or not.
    /// </param>
    /// <param name="f1">
    ///  A task that queries the controller.
    /// </param>
    /// <usage>
    ///  <code>
    ///    m_MainWindow.ProcCtrlQuery true ( fun () -> task {
    ///      do! *** do controller request ***
    ///      m_PropPage.Dispatcher.InvokeAsync ( fun () ->
    ///        *** update controllers ***
    ///        m_MainWindow.SetProgress false
    ///      ) |> ignore
    ///    })
    ///  </code>
    /// </usage>
    abstract ProcCtrlQuery : setProg:bool -> f1:( unit -> Task<unit> ) -> unit

    /// logout
    abstract Logout : unit -> unit

    /// register logout event handler
    abstract SubscribeLogoutEvent : e : ( unit -> unit ) -> int

    /// unregister logout event handler
    abstract UnsubscribeLogoutEvent : idx : int -> unit

    /// <summary>
    ///  Set progress bar to enable or disable.
    /// </summary>
    /// <param name="f">
    ///  Next progress bar status
    /// </param>
    abstract SetProgress : f:bool -> unit

    /// <summary>
    ///  Update target device and target group running status cache.
    /// </summary>
    /// <remarks>
    ///  This method must be called in background thread. It cannot execute by GUI thread.
    /// </remarks>
    abstract UpdateRunningStatus : unit -> Task<unit>

    /// <summary>
    ///  Get cached running status of the target devices.
    /// </summary>
    /// <returns>
    ///  Target device IDs of the Currentry running target devices.
    /// </returns>
    /// <remarks>
    ///  This information is updated by calling UpdateRunningStatus method.
    /// </remarks>
    abstract GetRunningTDIDs : unit -> TDID_T[]

    /// <summary>
    ///  Get cached target group IDs that is currently loaded target groups.
    /// </summary>
    /// <returns>
    ///  Target group IDs of the currentry loaded target groups.
    /// </returns>
    /// <remarks>
    ///  This information is updated by calling UpdateRunningStatus method.
    /// </remarks>
    abstract GetLoadedTGIDs : unit -> Dictionary< TDID_T, TGID_T[] >

    /// <summary>
    ///  Get cached target group IDs that is currently activated target groups.
    /// </summary>
    /// <returns>
    ///  Target group IDs of the currentry activated target groups.
    /// </returns>
    /// <remarks>
    ///  This information is updated by calling UpdateRunningStatus method.
    /// </remarks>
    abstract GetActivatedTGIDs : unit -> Dictionary< TDID_T, TGID_T[] >

    /// <summary>
    ///  Get specified target device is active and target group is loaded or not.
    /// </summary>
    /// <param name="tdid">
    ///  The target device ID.
    /// </param>
    /// <param name="tgid">
    ///  The target group ID.
    /// </param>
    /// <returns>
    ///  Pair of following values.
    ///  True if the target device is activated otherwise false. True if the target group is loaded otherwise false.
    /// </returns>
    abstract IsTGLoaded : tdid:TDID_T -> tgid:TGID_T -> struct( bool * bool )

/// <summary>
///  Converter for property page class to UIElement.
/// </summary>
type PropPageConverter() =
    inherit TypeConverter() with

        /// Convert property page class object to UIElement.
        override _.ConvertTo ( context : ITypeDescriptorContext, culture : CultureInfo, value : obj, destinationType : Type ) : obj =
            if destinationType = typeof<UIElement> then
                let f = value :?> IPropPageInterface
                f.GetUIElement()
            else
                base.ConvertTo( context, culture, value, destinationType )

        /// If specified destination is UIElement, it returns true. Otherwise false.
        override _.CanConvertTo  ( context : ITypeDescriptorContext, destinationType : Type ) : bool =
            destinationType = typeof<UIElement>

