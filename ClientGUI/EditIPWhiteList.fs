//=============================================================================
// Haruka Software Storage.
// EditIPWhiteList.fs : Implement the function to display EditIPWhiteList dialog window.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.Windows
open System.Windows.Controls
open System.Net

open Haruka.Constants
open Haruka.IODataTypes
open Haruka.Commons
open Haruka.Client

/// <summary>
///  EditIPWhiteListDialog class.
/// </summary>
/// <param name="m_Config">
///  Loaded configurations for GUI client.
/// </param>
type EditIPWhiteListDialog( m_Config : GUIConfig, m_Initial : IPCondition option ) as this =

    /// Get XAML resouces for EditIPWhiteListDialog.
    let m_Window = m_Config.UIElem.Get( PropertyViewIndex.PVI_EDIT_IPWHITELIST ) :?> Window

    // Get controll objects in the dialog window.
    let m_TypeCombo = m_Window.FindName( "TypeCombo" ) :?> ComboBox
    let m_IPAddressTextBox = m_Window.FindName( "IPAddressTextBox" ) :?> TextBox
    let m_MaskTextBox = m_Window.FindName( "MaskTextBox" ) :?> TextBox
    let m_OKButton = m_Window.FindName( "OKButton" ) :?> Button
    let m_CancelButton = m_Window.FindName( "CancelButton" ) :?> Button

    /// Result
    let mutable m_Result : DialogResult< ( IPCondition ) > = DialogResult.Cancel

    do
        // Set localized text to the controlls.
        m_Window.Title <- m_Config.CtrlText.Get( "EditIPWhiteList", "TITLE" )
        m_Config.SetLocalizedText "EditIPWhiteList" m_Window

        // Set controller initial status
        m_Window.IsEnabled <- true

        if m_Initial.IsSome then
            m_TypeCombo.SelectedIndex <-
                match m_Initial.Value with
                | IPCondition.Any -> 0
                | IPCondition.Loopback -> 1
                | IPCondition.Linklocal -> 2
                | IPCondition.Private -> 3
                | IPCondition.Multicast -> 4
                | IPCondition.Global -> 5
                | IPCondition.IPv4Any -> 6
                | IPCondition.IPv4Loopback -> 7
                | IPCondition.IPv4Linklocal -> 8
                | IPCondition.IPv4Private -> 9
                | IPCondition.IPv4Multicast -> 10
                | IPCondition.IPv4Global -> 11
                | IPCondition.IPv6Any -> 12 
                | IPCondition.IPv6Loopback -> 13
                | IPCondition.IPv6Linklocal -> 14
                | IPCondition.IPv6Private -> 15
                | IPCondition.IPv6Multicast -> 16
                | IPCondition.IPv6Global -> 17
                | IPCondition.IPFilter( _, _ ) -> 18

            match m_Initial.Value with
            | IPCondition.IPFilter( ipa, mas ) ->
                m_IPAddressTextBox.IsEnabled <- true
                m_IPAddressTextBox.Text <- ( IPAddress( ipa ) ).ToString()
                m_MaskTextBox.IsEnabled <- true
                m_MaskTextBox.Text <- ( IPAddress( mas ) ).ToString()
            | _ ->
                m_IPAddressTextBox.IsEnabled <- false
                m_IPAddressTextBox.Text <- ""
                m_MaskTextBox.IsEnabled <- false
                m_MaskTextBox.Text <- ""
            m_OKButton.IsEnabled <- true
        else
            m_TypeCombo.SelectedIndex <- -1
            m_IPAddressTextBox.IsEnabled <- false
            m_IPAddressTextBox.Text <- ""
            m_MaskTextBox.IsEnabled <- false
            m_MaskTextBox.Text <- ""
            m_OKButton.IsEnabled <- false

        // Register event handler
        m_Window.KeyDown.AddHandler ( fun _ e -> this.OnKeyDown_Window e )
        m_TypeCombo.SelectionChanged.AddHandler ( fun _ _ -> this.OnSelectionChanged_TypeCombo() )
        m_IPAddressTextBox.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged_IPAddressTextBox() )
        m_MaskTextBox.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged_MaskTextBox() )
        m_OKButton.Click.AddHandler ( fun _ _ -> this.OnClick_OKButton() )
        m_CancelButton.Click.AddHandler ( fun _ _ -> this.OnClick_CancelButton() )

    /// <summary>
    ///  Display the window
    /// </summary>
    /// <remarks>
    ///  This method will not return until the window is closed.
    /// </remarks>
    member _.Show () : DialogResult<IPCondition> =
        m_Window.ShowDialog() |> ignore
        m_Result

    /// <summary>
    ///  Dialog result property.
    /// </summary>
    member _.Result : DialogResult<IPCondition> =
        m_Result

    /// <summary>
    ///  On key push down at dialog window.
    /// </summary>
    /// <param name="e">
    ///  event argument
    /// </param>
    member private this.OnKeyDown_Window ( e : Input.KeyEventArgs ) : unit =
        match e.Key with
        | Input.Key.Escape ->
            m_Result <- DialogResult.Cancel
            m_Window.Close()
        | Input.Key.Enter ->
            if m_OKButton.IsEnabled then
                this.OnClick_OKButton()
            ()
        | _ -> ()

    /// <summary>
    ///  Selection of TypeCombo is changed.
    /// </summary>
    member private this.OnSelectionChanged_TypeCombo() : unit =
        this.Validate()

    /// <summary>
    ///  IPAddressTextBox is edited.
    /// </summary>
    member private this.OnTextChanged_IPAddressTextBox() : unit =
        this.Validate()

    /// <summary>
    ///  MaskTextBox is edited.
    /// </summary>
    member private this.OnTextChanged_MaskTextBox() : unit =
        this.Validate()

    /// <summary>
    ///  "OK" button was clicked.
    /// </summary>
    member private _.OnClick_OKButton() : unit =
        m_Result <-
            match m_TypeCombo.SelectedIndex with 
            | 0 ->
                DialogResult.Ok( IPCondition.Any )
            | 1 ->
                DialogResult.Ok( IPCondition.Loopback )
            | 2 ->
                DialogResult.Ok( IPCondition.Linklocal )
            | 3 ->
                DialogResult.Ok( IPCondition.Private )
            | 4 ->
                DialogResult.Ok( IPCondition.Multicast )
            | 5 ->
                DialogResult.Ok( IPCondition.Global )
            | 6 ->
                DialogResult.Ok( IPCondition.IPv4Any )
            | 7 ->
                DialogResult.Ok( IPCondition.IPv4Loopback )
            | 8 ->
                DialogResult.Ok( IPCondition.IPv4Linklocal )
            | 9 ->
                DialogResult.Ok( IPCondition.IPv4Private )
            | 10 ->
                DialogResult.Ok( IPCondition.IPv4Multicast )
            | 11 ->
                DialogResult.Ok( IPCondition.IPv4Global )
            | 12 ->
                DialogResult.Ok( IPCondition.IPv6Any )
            | 13 ->
                DialogResult.Ok( IPCondition.IPv6Loopback )
            | 14 ->
                DialogResult.Ok( IPCondition.IPv6Linklocal )
            | 15 ->
                DialogResult.Ok( IPCondition.IPv6Private )
            | 16 ->
                DialogResult.Ok( IPCondition.IPv6Multicast )
            | 17 ->
                DialogResult.Ok( IPCondition.IPv6Global )
            | 18 ->
                let r1, ip1 = IPAddress.TryParse m_IPAddressTextBox.Text
                let r2, ip2 = IPAddress.TryParse m_MaskTextBox.Text
                if not r1 || not r2 then
                    // unexpected
                    DialogResult.Cancel
                else
                    DialogResult.Ok( IPCondition.IPFilter( ip1.GetAddressBytes(), ip2.GetAddressBytes() ) )
            | _ -> // Unexpected
                DialogResult.Cancel
        m_Window.Close()

    /// <summary>
    ///  "Cancel" button was clicked.
    /// </summary>
    member private _.OnClick_CancelButton() : unit =
        m_Result <- DialogResult.Cancel
        m_Window.Close()

    /// <summary>
    ///  Validate entered values.
    ///  If the values are valid, the OK button will be enabled. Otherwise, the OK button will be disabled.
    /// </summary>
    member private _.Validate() : unit =
        let s = m_TypeCombo.SelectedIndex
        if s = -1 then
            m_OKButton.IsEnabled <- false
            m_IPAddressTextBox.IsEnabled <- false
            m_MaskTextBox.IsEnabled <- false
        elif s = 18 then
            m_IPAddressTextBox.IsEnabled <- true
            m_MaskTextBox.IsEnabled <- true
            let r1, ip1 = IPAddress.TryParse m_IPAddressTextBox.Text
            let r2, ip2 = IPAddress.TryParse m_MaskTextBox.Text
            m_OKButton.IsEnabled <-
                r1 && r2 && ip1.GetAddressBytes().Length = ip2.GetAddressBytes().Length
        else
            m_OKButton.IsEnabled <- true
            m_IPAddressTextBox.IsEnabled <- false
            m_MaskTextBox.IsEnabled <- false



