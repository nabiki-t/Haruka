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
open System.Threading.Tasks

open Haruka.Constants
open Haruka.IODataTypes
open Haruka.Commons
open Haruka.Client

/// <summary>
///  InitialPropPage class.
/// </summary>
/// <param name="m_Config">
///  Loaded configurations.
/// </param>
/// <param name="m_PropPage">
///  The grid object for this property page that is loaded from the XAML file.
/// </param>
[< TypeConverter( typeof<PropPageConverter> ) >]
type InitialPropPage(
    m_Config : GUIConfig,
    m_PropPage : Grid
) =

    // Initialize procedure for the property page.
    do
        // Set controller localized text
        m_Config.SetLocalizedText "InitialPropPage" m_PropPage

    /// <inheritdoc />
    interface IPropPageInterface with
        
        // Get loaded property page UI object.
        override _.GetUIElement (): UIElement = m_PropPage

        // Set enable or disable to the property page.
        override _.SetEnable ( isEnable : bool ) : unit =
            m_PropPage.IsEnabled <- isEnable

        // The property page will be showed.
        override _.InitializeViewContent() : unit =
            ()  // Nothing to do

        // The status is updated.
        override _.UpdateViewContent() : unit =
            ()  // Nothing to do

        // Set property page size
        override _.SetPageWidth ( width : float ) : unit =
            m_PropPage.Width <- width

        // Notification of closed this page
        override _.OnClosePage() : unit =
            ()  // Nothing to do
    
        // Get current node ID
        override _.GetNodeID() =
            confnode_me.zero
