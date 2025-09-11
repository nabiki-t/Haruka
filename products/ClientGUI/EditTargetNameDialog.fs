//=============================================================================
// Haruka Software Storage.
// EditTargetNameDialog.fs : Implement the function to display EditTargetName dialog window.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.Windows
open System.Windows.Controls
open System.Text.RegularExpressions

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

/// <summary>
///  EditTargetNameDialog class.
/// </summary>
/// <param name="m_Config">
///  Loaded configurations for GUI client.
/// </param>
/// <param name="m_OldTargetName">
///  Current configured target name..
/// </param>
type EditTargetNameDialog( m_Config : GUIConfig, m_OldTargetName : string ) as this =

    /// Load XAML resouces for dialog window.
    let m_Window = m_Config.UIElem.Get( PropertyViewIndex.PVI_EDIT_TARGET_NAME ) :?> Window

    // Get controll objects in Dialog.
    let m_IQNFormatRadio = m_Window.FindName( "IQNFormatRadio" ) :?> RadioButton
    let m_YearTextBox = m_Window.FindName( "YearTextBox" ) :?> TextBox
    let m_MonthTextBox = m_Window.FindName( "MonthTextBox" ) :?> TextBox
    let m_DomainTextBlock = m_Window.FindName( "DomainTextBlock" ) :?> TextBlock
    let m_DomainTextBox = m_Window.FindName( "DomainTextBox" ) :?> TextBox
    let m_OptStrTextBlock = m_Window.FindName( "OptStrTextBlock" ) :?> TextBlock
    let m_OptStrTextBox = m_Window.FindName( "OptStrTextBox" ) :?> TextBox
    let m_EUIFormatRadio = m_Window.FindName( "EUIFormatRadio" ) :?> RadioButton
    let m_EUI64TextBox = m_Window.FindName( "EUI64TextBox" ) :?> TextBox
    let m_UnknownFormatRadio = m_Window.FindName( "UnknownFormatRadio" ) :?> RadioButton
    let m_OtherTextBox = m_Window.FindName( "OtherTextBox" ) :?> TextBox
    let m_RandomButton = m_Window.FindName( "RandomButton" ) :?> Button
    let m_OKButton = m_Window.FindName( "OKButton" ) :?> Button
    let m_CancelButton = m_Window.FindName( "CancelButton" ) :?> Button

    /// Result
    let mutable m_Result : DialogResult< string > =
        DialogResult.Cancel

    /// pattern match object
    let m_Reg_IQN = new Regex( "iqn\.([0-9]{4})-([0-9]{2})\.([\-\.a-z0-9]{1,})(\:[\-\.\:a-z0-9]{1,}|)" )
    let m_Reg_EUI = new Regex( "eui\.([0-9a-f]{16})" )
    let m_Reg_Year = new Regex( "[0-9]{4}" )
    let m_Reg_Month = new Regex( "[0-9]{2}" )
    let m_Reg_Domain = new Regex( "[\-\.a-zA-X0-9]{1,}" )
    let m_Reg_OptStr = new Regex( "[\-\.\:a-zA-X0-9]*" )
    let m_Reg_EUINumber = new Regex( "[0-9a-fA-F]{16}" )

    do
        // Set controller localized text
        m_Window.Title <- m_Config.CtrlText.Get( "EditTargetNameDialog", "TITLE" )
        m_Config.SetLocalizedText "EditTargetNameDialog" m_Window

        // Set controller initial status
        m_Window.IsEnabled <- true

        let wiqn = m_Reg_IQN.Match m_OldTargetName
        let weui = m_Reg_EUI.Match m_OldTargetName
        if wiqn.Success then
            m_IQNFormatRadio.IsChecked <- true
            m_YearTextBox.IsEnabled <- true
            m_MonthTextBox.IsEnabled <- true
            m_DomainTextBox.IsEnabled <- true
            m_OptStrTextBox.IsEnabled <- true
            m_EUIFormatRadio.IsChecked <- false
            m_EUI64TextBox.IsEnabled <- false
            m_UnknownFormatRadio.IsChecked <- false
            m_OtherTextBox.IsEnabled <- false
            m_RandomButton.IsEnabled <- true

            m_YearTextBox.Text <- wiqn.Groups.Item(1).Value
            m_MonthTextBox.Text <- wiqn.Groups.Item(2).Value
            m_DomainTextBox.Text <- wiqn.Groups.Item(3).Value
            let optStr = wiqn.Groups.Item(4).Value
            if optStr.Length > 0 then
                m_OptStrTextBox.Text <- optStr.[ 1 .. ]
            else
                m_OptStrTextBox.Text <- ""
            m_EUI64TextBox.Text <- ""
            m_OtherTextBox.Text <- ""

        elif weui.Success then
            m_IQNFormatRadio.IsChecked <- false
            m_YearTextBox.IsEnabled <- false
            m_MonthTextBox.IsEnabled <- false
            m_DomainTextBox.IsEnabled <- false
            m_OptStrTextBox.IsEnabled <- false
            m_EUIFormatRadio.IsChecked <- true
            m_EUI64TextBox.IsEnabled <- true
            m_UnknownFormatRadio.IsChecked <- false
            m_OtherTextBox.IsEnabled <- false
            m_RandomButton.IsEnabled <- true

            m_YearTextBox.Text <- ""
            m_MonthTextBox.Text <- ""
            m_DomainTextBlock.Text <- ""
            m_OptStrTextBlock.Text <- ""
            m_EUI64TextBox.Text <- weui.Groups.Item(1).Value
            m_OtherTextBox.Text <- ""

        else
            m_IQNFormatRadio.IsChecked <- false
            m_YearTextBox.IsEnabled <- false
            m_MonthTextBox.IsEnabled <- false
            m_DomainTextBox.IsEnabled <- false
            m_OptStrTextBox.IsEnabled <- false
            m_EUIFormatRadio.IsChecked <- false
            m_EUI64TextBox.IsEnabled <- false
            m_UnknownFormatRadio.IsChecked <- true
            m_OtherTextBox.IsEnabled <- true
            m_RandomButton.IsEnabled <- false

            m_YearTextBox.Text <- ""
            m_MonthTextBox.Text <- ""
            m_DomainTextBlock.Text <- ""
            m_OptStrTextBlock.Text <- ""
            m_EUI64TextBox.Text <- ""
            m_OtherTextBox.Text <- m_OldTargetName


        // Register event handler
        m_Window.KeyDown.AddHandler ( fun _ e -> this.OnKeyDown_Window e )
        m_IQNFormatRadio.Click.AddHandler ( fun _ _ -> this.OnClick_IQNFormatRadio() )
        m_EUIFormatRadio.Click.AddHandler ( fun _ _ -> this.OnClick_EUIFormatRadio() )
        m_UnknownFormatRadio.Click.AddHandler ( fun _ _ -> this.OnClick_UnknownFormatRadio() )
        m_YearTextBox.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged() )
        m_MonthTextBox.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged() )
        m_DomainTextBox.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged() )
        m_OptStrTextBox.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged() )
        m_EUI64TextBox.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged() )
        m_OtherTextBox.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged() )
        m_RandomButton.Click.AddHandler ( fun _ _ -> this.OnClick_RandomButton() )
        m_OKButton.Click.AddHandler ( fun _ _ -> this.OnClick_OkButton() )
        m_CancelButton.Click.AddHandler ( fun _ _ -> this.OnClick_CancelButton() )

    /// <summary>
    ///  Display the window
    /// </summary>
    /// <remarks>
    ///  This method will not return until the window is closed.
    /// </remarks>
    member _.Show () : DialogResult< string > =
        m_Window.ShowDialog() |> ignore
        m_Result

    /// <summary>
    ///  Dialog result property.
    /// </summary>
    member _.Result : DialogResult< string > =
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
            this.OnClick_OkButton()
        | _ -> ()

    /// <summary>
    ///  "iqn. format" radio button is selected.
    /// </summary>
    member private this.OnClick_IQNFormatRadio() : unit =
        m_IQNFormatRadio.IsChecked <- true
        m_YearTextBox.IsEnabled <- true
        m_MonthTextBox.IsEnabled <- true
        m_DomainTextBox.IsEnabled <- true
        m_OptStrTextBox.IsEnabled <- true
        m_EUIFormatRadio.IsChecked <- false
        m_EUI64TextBox.IsEnabled <- false
        m_UnknownFormatRadio.IsChecked <- false
        m_OtherTextBox.IsEnabled <- false
        m_RandomButton.IsEnabled <- true
        this.OnTextChanged()

    /// <summary>
    ///  "eui. format" radio button is selected.
    /// </summary>
    member private this.OnClick_EUIFormatRadio() : unit =
        m_IQNFormatRadio.IsChecked <- false
        m_YearTextBox.IsEnabled <- false
        m_MonthTextBox.IsEnabled <- false
        m_DomainTextBox.IsEnabled <- false
        m_OptStrTextBox.IsEnabled <- false
        m_EUIFormatRadio.IsChecked <- true
        m_EUI64TextBox.IsEnabled <- true
        m_UnknownFormatRadio.IsChecked <- false
        m_OtherTextBox.IsEnabled <- false
        m_RandomButton.IsEnabled <- true
        this.OnTextChanged()

    /// <summary>
    ///  "Other" radio button is selected.
    /// </summary>
    member private this.OnClick_UnknownFormatRadio() : unit =
        m_IQNFormatRadio.IsChecked <- false
        m_YearTextBox.IsEnabled <- false
        m_MonthTextBox.IsEnabled <- false
        m_DomainTextBox.IsEnabled <- false
        m_OptStrTextBox.IsEnabled <- false
        m_EUIFormatRadio.IsChecked <- false
        m_EUI64TextBox.IsEnabled <- false
        m_UnknownFormatRadio.IsChecked <- true
        m_OtherTextBox.IsEnabled <- true
        m_RandomButton.IsEnabled <- false
        this.OnTextChanged()

    /// <summary>
    ///  Input value of text box has changed.
    ///  If entered value is acceptable, OK button will be enebled.
    /// </summary>
    member private _.OnTextChanged() : unit =
        let iqnRadio = m_IQNFormatRadio.IsChecked
        let euiRadio = m_EUIFormatRadio.IsChecked
        let f = 
            if iqnRadio.HasValue && iqnRadio.Value then
                m_Reg_Year.IsMatch m_YearTextBox.Text &&
                m_Reg_Month.IsMatch m_MonthTextBox.Text &&
                m_Reg_Domain.IsMatch m_DomainTextBox.Text &&
                m_Reg_OptStr.IsMatch m_OptStrTextBox.Text
            elif euiRadio.HasValue && euiRadio.Value then
                m_Reg_EUINumber.IsMatch m_EUI64TextBox.Text
            else
                true
        m_OKButton.IsEnabled <- f
        
    /// <summary>
    ///  "Random" button was clicked.
    /// </summary>
    member private _.OnClick_RandomButton() : unit =
        let r = Random()
        let iqnRadio = m_IQNFormatRadio.IsChecked
        let euiRadio = m_EUIFormatRadio.IsChecked
        if iqnRadio.HasValue && iqnRadio.Value then
            m_YearTextBox.Text <- "1999"
            m_MonthTextBox.Text <- "01"
            m_DomainTextBox.Text <- "com.example"
            m_OptStrTextBox.Text <- sprintf "%08x" ( r.Next() )
        elif euiRadio.HasValue && euiRadio.Value then
            m_EUI64TextBox.Text <- sprintf "%016x" ( r.NextInt64() )
        else
            ()  // ignore

    /// <summary>
    ///  "OK" button is clicked.
    /// </summary>
    /// <remarks>
    ///  This method may be called from OnKeyDown_Window method even if the OK button is disabled.
    /// </remarks>
    member private _.OnClick_OkButton() : unit =
        let iqnRadio = m_IQNFormatRadio.IsChecked
        let euiRadio = m_EUIFormatRadio.IsChecked
        let f, vstr = 
            if iqnRadio.HasValue && iqnRadio.Value then
                let f2 =
                    m_Reg_Year.IsMatch m_YearTextBox.Text &&
                    m_Reg_Month.IsMatch m_MonthTextBox.Text &&
                    m_Reg_Domain.IsMatch m_DomainTextBox.Text &&
                    m_Reg_OptStr.IsMatch m_OptStrTextBox.Text
                let v2 =
                    "iqn." +
                    m_YearTextBox.Text + "-" +
                    m_MonthTextBox.Text + "." +
                    m_DomainTextBox.Text.ToLower() +
                    if m_OptStrTextBox.Text.Length > 0 then
                        ":" + m_OptStrTextBox.Text.ToLower()
                    else
                        ""
                ( f2, v2 )
            elif euiRadio.HasValue && euiRadio.Value then
                let f2 = m_Reg_EUINumber.IsMatch m_EUI64TextBox.Text
                let v2 = "eui." + m_EUI64TextBox.Text.ToLower()
                ( f2, v2 )
            else
                true, m_OtherTextBox.Text.ToLower()

        if not f then
            ()  // ignore
        else
            m_Result <- DialogResult.Ok( vstr )
            m_Window.Close()
            
    /// <summary>
    ///  "Cancel" button is clicked.
    /// </summary>
    member private _.OnClick_CancelButton() : unit =
        m_Result <- DialogResult.Cancel
        m_Window.Close()
