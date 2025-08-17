//=============================================================================
// Haruka Software Storage.
// EditDebugTrapDialog.fs : Implement the function to display EditDebugTrap dialog window.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.Windows
open System.Windows.Controls

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

/// <summary>
///  EditDebugTrapDialog class.
/// </summary>
/// <param name="m_Config">
///  Loaded configurations for GUI client.
/// </param>
type EditDebugTrapDialog( m_Config : GUIConfig ) as this =

    /// Load XAML resouces for dialog window.
    let m_Window = m_Config.UIElem.Get( PropertyViewIndex.PVI_EDIT_DEBUG_TRAP ) :?> Window

    // Get controll objects in Dialog.
    let m_EditDebugTrapDialog = m_Window.FindName( "EditDebugTrapDialog" ) :?> ComboBox
    let m_EventCombo = m_Window.FindName( "EventCombo" ) :?> ComboBox
    let m_StartLBATextBox = m_Window.FindName( "StartLBATextBox" ) :?> TextBox
    let m_EndLBATextBox = m_Window.FindName( "EndLBATextBox" ) :?> TextBox
    let m_ActionCombo = m_Window.FindName( "ActionCombo" ) :?> ComboBox
    let m_MessageTextBox = m_Window.FindName( "MessageTextBox" ) :?> TextBox
    let m_CountIndexTextBox = m_Window.FindName( "CountIndexTextBox" ) :?> TextBox
    let m_OKButton = m_Window.FindName( "OKButton" ) :?> Button
    let m_CancelButton = m_Window.FindName( "CancelButton" ) :?> Button

    /// Result
    let mutable m_Result : DialogResult< MediaCtrlReq.T_AddTrap > =
        DialogResult.Cancel

    do
        // Set controller localized text
        m_Window.Title <- m_Config.CtrlText.Get( "EditDebugTrapDialog", "TITLE" )
        m_Config.SetLocalizedText "EditDebugTrapDialog" m_Window

        // Set controller initial status
        m_Window.IsEnabled <- true
        m_EventCombo.SelectedIndex <- 0
        m_ActionCombo.SelectedIndex <- 0
        m_StartLBATextBox.Text <- ""
        m_EndLBATextBox.Text <- ""
        m_MessageTextBox.Text <- ""
        m_CountIndexTextBox.Text <- ""
        m_OKButton.IsEnabled <- true
        m_CancelButton.IsEnabled <- true
        m_StartLBATextBox.IsEnabled <- false
        m_EndLBATextBox.IsEnabled <- false
        m_MessageTextBox.IsEnabled <- true
        m_CountIndexTextBox.IsEnabled <- false

        // Register event handler
        m_Window.KeyDown.AddHandler ( fun _ e -> this.OnKeyDown_Window e )
        m_EventCombo.SelectionChanged.AddHandler ( fun _ _ -> this.OnSelectionChanged_EventCombo() )
        m_ActionCombo.SelectionChanged.AddHandler ( fun _ _ -> this.OnSelectionChanged_ActionCombo() )
        m_StartLBATextBox.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged() )
        m_EndLBATextBox.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged() )
        m_MessageTextBox.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged() )
        m_CountIndexTextBox.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged() )
        m_OKButton.Click.AddHandler ( fun _ _ -> this.OnClick_OkButton() )
        m_CancelButton.Click.AddHandler ( fun _ _ -> this.OnClick_CancelButton() )

    ///////////////////////////////////////////////////////////////////////////
    // Public method

    /// <summary>
    ///  Display the window
    /// </summary>
    /// <param name="apl">
    ///  The application class object.
    /// </param>
    /// <remarks>
    ///  This method will not return until the window is closed.
    /// </remarks>
    member _.Show () : DialogResult< MediaCtrlReq.T_AddTrap > =
        m_Window.ShowDialog() |> ignore
        m_Result

    /// <summary>
    ///  Dialog result property.
    /// </summary>
    member _.Result : DialogResult< MediaCtrlReq.T_AddTrap > =
        m_Result

    ///////////////////////////////////////////////////////////////////////////
    // Event handler

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
            this.OnClick_OkButton()
        | _ -> ()

    /// <summary>
    ///  Selection of EventCombo has changed.
    ///  If entered value is acceptable, OK button will be enebled.
    /// </summary>
    member private _.OnSelectionChanged_EventCombo() : unit =
        match m_EventCombo.SelectedIndex with
        | 2     // Read
        | 3 ->  // Write
            m_StartLBATextBox.IsEnabled <- true
            m_EndLBATextBox.IsEnabled <- true

        | _ ->
            m_StartLBATextBox.IsEnabled <- false
            m_EndLBATextBox.IsEnabled <- false

    /// <summary>
    ///  Selection of EventCombo has changed.
    ///  If entered value is acceptable, OK button will be enebled.
    /// </summary>
    member private _.OnSelectionChanged_ActionCombo() : unit =
        match m_ActionCombo.SelectedIndex with
        | 0     // ACA
        | 1 ->  // LUReset
            m_MessageTextBox.IsEnabled <- true
            m_CountIndexTextBox.IsEnabled <- false
        | 2     // Count
        | 3 ->  // Delay
            m_MessageTextBox.IsEnabled <- false
            m_CountIndexTextBox.IsEnabled <- true
        | _ ->
            m_MessageTextBox.IsEnabled <- false
            m_CountIndexTextBox.IsEnabled <- false

    /// <summary>
    ///  Input value of text box has changed.
    ///  If entered value is acceptable, OK button will be enebled.
    /// </summary>
    member private this.OnTextChanged() : unit =
        m_OKButton.IsEnabled <- this.Validate()

    /// <summary>
    ///  "OK" button is clicked.
    /// </summary>
    /// <remarks>
    ///  This method may be called from OnKeyDown_Window method even if the OK button is disabled.
    /// </remarks>
    member private this.OnClick_OkButton() : unit =
        if this.Validate() |> not then
            ()  // ignore
        else
            let eventVal =
                match m_EventCombo.SelectedIndex with
                | 0 ->  // TestUnitReady
                    MediaCtrlReq.U_TestUnitReady()
                | 1 ->  // ReadCapacity
                    MediaCtrlReq.U_ReadCapacity()
                | 2 ->  // Read
                    let slba =
                        let r, v = UInt64.TryParse m_StartLBATextBox.Text
                        if r then v else 0UL
                    let elba = 
                        let r, v = UInt64.TryParse m_EndLBATextBox.Text
                        if r then v else UInt64.MaxValue
                    MediaCtrlReq.U_Read( { StartLBA = slba; EndLBA = elba; } )
                | 3 ->  // Write
                    let slba =
                        let r, v = UInt64.TryParse m_StartLBATextBox.Text
                        if r then v else 0UL
                    let elba =
                        let r, v = UInt64.TryParse m_EndLBATextBox.Text
                        if r then v else UInt64.MaxValue
                    MediaCtrlReq.U_Write( { StartLBA = slba; EndLBA = elba; } )

                | 4 ->  // Format
                    MediaCtrlReq.U_Format()
                | _ ->  // This branch is never executed.
                    MediaCtrlReq.U_TestUnitReady()
            let actionVal =
                match m_ActionCombo.SelectedIndex with
                | 0 ->  // ACA
                    MediaCtrlReq.U_ACA( m_MessageTextBox.Text.[ .. 255 ] )
                | 1 ->  // LUReset
                    MediaCtrlReq.U_LUReset( m_MessageTextBox.Text.[ .. 255 ] )
                | 2 ->  // Count
                    let counter =
                        let r, v = Int32.TryParse m_CountIndexTextBox.Text
                        if r then v else 0
                    MediaCtrlReq.U_Count( counter )
                | 3 ->  // Delay
                    let counter =
                        let r, v = Int32.TryParse m_CountIndexTextBox.Text
                        if r then v else 0
                    MediaCtrlReq.U_Delay( counter )
                | _ ->  // This branch is never executed.
                    MediaCtrlReq.U_Count( 0 )

            m_Result <- DialogResult.Ok( { Event = eventVal; Action = actionVal; } )
            m_Window.Close()
            
    /// <summary>
    ///  "Cancel" button is clicked.
    /// </summary>
    member private _.OnClick_CancelButton() : unit =
        m_Result <- DialogResult.Cancel
        m_Window.Close()

    ///////////////////////////////////////////////////////////////////////////
    // Private method

    /// <summary>
    ///  Validate entered value.
    ///  If entered value is acceptable, true is returned.
    /// </summary>
    member private _.Validate() : bool =

        let eventResult =
            match m_EventCombo.SelectedIndex with
            | 0 ->  // TestUnitReady
                true
            | 1 ->  // ReadCapacity
                true
            | 2     // Read
            | 3 ->  // Write
                let slbaR, slbaV =
                    let t = m_StartLBATextBox.Text
                    if t.Length = 0 then
                        true, 0UL
                    else
                        UInt64.TryParse t
                let elbaR, elbaV =
                    let t = m_EndLBATextBox.Text
                    if t.Length = 0 then
                        true, UInt64.MaxValue
                    else
                        UInt64.TryParse t
                if slbaR && elbaR then
                    slbaV <= elbaV
                else
                    false
            | 4 ->  // Format
                true
            | _ ->
                false

        let actionResult =
            match m_ActionCombo.SelectedIndex with
            | 0 ->  // ACA
                true
            | 1 ->  // LUReset
                true
            | 2     // Count
            | 3 ->  // Delay
                let t = m_CountIndexTextBox.Text
                if t.Length = 0 then
                    true
                else
                    Int32.TryParse t |> fst
            | _ ->
                false

        eventResult && actionResult

