//=============================================================================
// Haruka Software Storage.
// ControllerPropPage.fs : Implement the function to display Controller property page in the main window.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.Windows 
open System.Windows.Controls
open System.Windows.Controls.Primitives

open Haruka.Constants
open Haruka.Commons
open Haruka.Client


/// <summary>
///  Configuration master class.
/// </summary>
/// <param name="m_ExeDir">
///  Directory path name that stores ClientGUI.exe file.
/// </param>
type GUIConfig( m_ExeDir : string ) =

    /// localized string that is set to controlls.
    let m_CtrlText = new StringTable( "ClientGUI_GUIText" )

    /// Message strings for CLI client.
    let m_ClientCLIText = new StringTable( "Client" )

    /// Message strings for GUI client.
    let m_MessagesText = new StringTable( "ClientGUI_Messages" )

    /// Icons
    let m_Icons = new IconLoader( m_ExeDir )

    /// Property pages
    let m_UIElem = new XamlLoader( m_ExeDir )

    /// Get localizes string for controlls
    member _.CtrlText : StringTable =
        m_CtrlText

    /// Get message strings for CLI client.
    member _.ClientCLIText : StringTable =
        m_ClientCLIText

    /// Get message strings for CLI client.
    member _.MessagesText : StringTable =
        m_MessagesText

    /// Get icons
    member _.Icons : IconLoader =
        m_Icons

    /// Get property pages
    member _.UIElem : XamlLoader =
        m_UIElem

    /// <summary>
    ///  Set localized text to the controller
    /// </sumary>
    /// <param name="s">
    ///  section name
    /// </param>
    /// <param name="r">
    ///  UI element
    /// </param>
    member _.SetLocalizedText ( s : string ) ( r : FrameworkElement ) : unit =
        m_CtrlText.GetNames s
        |> Seq.iter ( fun kn -> 
            let vtxt = m_CtrlText.Get( s, kn )
            match r.FindName( kn ) with
            | :? Button as x -> x.Content <- vtxt
            | :? CheckBox as x -> x.Content <- vtxt
            | :? Label as x -> x.Content <- vtxt
            | :? RadioButton as x -> x.Content <- vtxt
            | :? TabItem as x -> x.Header <- vtxt
            | :? TextBlock as x -> x.Text <- vtxt
            | :? TextBox as x -> x.Text <- vtxt
            | :? ComboBoxItem as x -> x.Content <- vtxt
            | :? Expander as x -> x.Header <- vtxt
            | :? GroupBox as x -> x.Header <- vtxt
            | :? ListBoxItem as x -> x.Content <- vtxt
            | :? GridViewColumn as x -> x.Header <- vtxt
            | :? MenuItem as x -> x.Header <- vtxt
            | :? StatusBarItem as x -> x.Content <- vtxt
            | :? TreeViewItem as x -> x.Header <- vtxt
            | _ -> ()
        )
        