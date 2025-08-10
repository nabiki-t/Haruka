//=============================================================================
// Haruka Software Storage.
// LoginDialog.fs : Implement the function to display Login dialog window.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.IO
open System.Xml
open System.Xaml
open System.Windows
open System.Windows.Markup
open System.Windows.Controls
open System.Threading
open System.Threading.Tasks

open Microsoft.Win32

open Haruka.Constants
open Haruka.IODataTypes
open Haruka.Commons
open Haruka.Client

/// <summary>
///  LoginDialog class.
/// </summary>
/// <param name="m_Config">
///  Loaded configurations for GUI client.
/// </param>
type LoginDialog( m_Config : GUIConfig ) as this =

    /// background task queue
    let m_BKTaskQueue = new TaskQueue()

    /// Load XAML resouces for dialog window.
    let m_Window = m_Config.UIElem.Get( PropertyViewIndex.PVI_LOGIN_DIALOG ) :?> Window

    // Get controll objects in Dialog.
    let m_LoginByDirectoryRadio = m_Window.FindName( "LoginByDirectoryRadio" ) :?> RadioButton
    let m_ConfigDirText = m_Window.FindName( "ConfigDirText" ) :?> TextBox
    let m_ConfigDirBrouseBtn = m_Window.FindName( "ConfigDirBrouseBtn" ) :?> Button
    let m_LoginByNetwordRadio = m_Window.FindName( "LoginByNetwordRadio" ) :?> RadioButton
    let m_HostNameText = m_Window.FindName( "HostNameText" ) :?> TextBox
    let m_PortNumberText = m_Window.FindName( "PortNumberText" ) :?> TextBox
    let m_ForceLoginCheck = m_Window.FindName( "ForceLoginCheck" ) :?> CheckBox
    let m_ErrorMessageTextBlock = m_Window.FindName( "ErrorMessageTextBlock" ) :?> TextBlock
    let m_LoginButton = m_Window.FindName( "LoginButton" ) :?> Button
    let m_CancelButton = m_Window.FindName( "CancelButton" ) :?> Button
    let m_LoginProgress = m_Window.FindName( "LoginProgress" ) :?> ProgressBar

    /// Result
    let mutable m_Result : DialogResult< CtrlConnection > =
        DialogResult.Cancel

    /// Window closeing reason
    let mutable m_CloseReason =
        WinCloseReason.User

    do
        // Set controller localized text
        m_Window.Title <- m_Config.CtrlText.Get( "LoginDialog", "TITLE" )
        m_Config.SetLocalizedText "LoginDialog" m_Window

        // Set controller initial status
        m_Window.IsEnabled <- true
        m_LoginByDirectoryRadio.IsChecked <- true
        m_ConfigDirText.IsEnabled <- true
        m_ConfigDirBrouseBtn.IsEnabled <- true
        m_LoginByNetwordRadio.IsChecked <- false
        m_HostNameText.IsEnabled <- false
        m_ForceLoginCheck.IsEnabled <- true
        m_ForceLoginCheck.IsChecked <- false
        m_ErrorMessageTextBlock.IsEnabled <- true
        m_PortNumberText.IsEnabled <- false
        m_LoginButton.IsEnabled <- false
        m_LoginProgress.IsIndeterminate <- false
        m_LoginProgress.Value <- 0.0

        // Register event handler
        m_Window.KeyDown.AddHandler ( fun _ e -> this.OnKeyDown_Window e )
        m_Window.Closing.AddHandler ( fun _ e -> this.OnClosing_Window e )
        m_Window.Closed.AddHandler ( fun _ e -> this.OnClosed_Window() )
        m_LoginByDirectoryRadio.Click.AddHandler ( fun _ _ -> this.OnClick_LoginByDirectoryRadio() )
        m_ConfigDirBrouseBtn.Click.AddHandler ( fun _ _ -> this.OnClick_ConfigDirBrouseBtn() )
        m_LoginByNetwordRadio.Click.AddHandler ( fun _ _ -> this.OnClick_LoginByNetwordRadio() )
        m_LoginButton.Click.AddHandler ( fun _ _ -> this.OnClick_LoginButton() )
        m_CancelButton.Click.AddHandler ( fun _ _ -> this.OnClick_CancelButton() )
        m_ConfigDirText.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged_ConfigDirText() )
        m_HostNameText.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged_HostNameText() )
        m_PortNumberText.TextChanged.AddHandler ( fun _ _ -> this.OnTextChanged_PortNumberText() )

        // Set default value
        if UserOpeStat.LoginDialog_Type = 0 then
            this.OnClick_LoginByDirectoryRadio()
        else
            this.OnClick_LoginByNetwordRadio()
        m_ConfigDirText.Text <- UserOpeStat.LoginDialog_Directory
        m_HostNameText.Text <- UserOpeStat.LoginDialog_HostName
        m_PortNumberText.Text <- UserOpeStat.LoginDialog_PortNumber

    /// <summary>
    ///  Display the window
    /// </summary>
    /// <param name="apl">
    ///  The application class object.
    /// </param>
    /// <remarks>
    ///  This method will not return until the window is closed.
    /// </remarks>
    member _.Show () : DialogResult<CtrlConnection> =
        m_Window.ShowDialog() |> ignore
        m_Result

    /// <summary>
    ///  Dialog result property.
    /// </summary>
    member _.Result : DialogResult<CtrlConnection> =
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
            if m_BKTaskQueue.RunWaitCount = 0u then
                m_Result <- DialogResult.Cancel
                m_CloseReason <- WinCloseReason.Internal
                m_Window.Close()
        | Input.Key.Enter ->
            if m_BKTaskQueue.RunWaitCount = 0u then
               this.OnClick_LoginButton()
        | _ -> ()

    /// <summary>
    ///  THe dialog window being closed.
    /// </summary>
    member private _.OnClosing_Window ( e : ComponentModel.CancelEventArgs ) : unit =
        e.Cancel <- ( m_CloseReason = WinCloseReason.User ) && ( m_BKTaskQueue.RunWaitCount > 0u )

    /// <summary>
    ///  THe dialog window had been closed.
    /// </summary>
    /// <param name="e">
    ///  event argument
    /// </param>
    member private _.OnClosed_Window () : unit =
        m_BKTaskQueue.Stop()

    /// <summary>
    ///  "LoginByDirectoryRadio" radio button was clicked.
    /// </summary>
    member private _.OnClick_LoginByDirectoryRadio() : unit =
        m_LoginByDirectoryRadio.IsChecked <- true
        m_ConfigDirText.IsEnabled <- true
        m_ConfigDirBrouseBtn.IsEnabled <- true
        m_LoginByNetwordRadio.IsChecked <- false
        m_HostNameText.IsEnabled <- false
        m_PortNumberText.IsEnabled <- false

    /// <summary>
    ///  "ConfigDirBrouseBtn" button was clicked.
    /// </summary>
    member private _.OnClick_ConfigDirBrouseBtn() : unit =
        let d = new OpenFolderDialog()
        let r = d.ShowDialog()
        if r.HasValue && r.Value then
            m_ConfigDirText.Text <- d.FolderName

    /// <summary>
    ///  "LoginByNetwordRadio" radio button was clicked.
    /// </summary>
    member private _.OnClick_LoginByNetwordRadio() : unit =
        m_LoginByDirectoryRadio.IsChecked <- false
        m_ConfigDirText.IsEnabled <- false
        m_ConfigDirBrouseBtn.IsEnabled <- false
        m_LoginByNetwordRadio.IsChecked <- true
        m_HostNameText.IsEnabled <- true
        m_PortNumberText.IsEnabled <- true

    /// <summary>
    ///  "Login" button was clicked.
    /// </summary>
    member private this.OnClick_LoginButton() : unit =
        if m_BKTaskQueue.RunWaitCount = 0u then
            let forceFlg =
                let f = m_ForceLoginCheck.IsChecked
                ( f.HasValue && f.Value )
            let s = m_LoginByDirectoryRadio.IsChecked
            m_ErrorMessageTextBlock.Text <- ""
            m_Result <- DialogResult.Cancel

            try
                let hostName, portNo =
                    if s.HasValue && s.Value = true then
                        // Get the server address and the port number from specified configuration file.
                        let dirPath = m_ConfigDirText.Text
                        let confFName = Functions.AppendPathName dirPath Constants.CONTROLLER_CONF_FILE_NAME
                        let conf = HarukaCtrlConf.ReaderWriter.LoadFile confFName
                        if conf.RemoteCtrl.IsSome then
                            conf.RemoteCtrl.Value.Address, conf.RemoteCtrl.Value.PortNum
                        else
                            "::1", Constants.DEFAULT_MNG_CLI_PORT_NUM
                    else
                        let portNum = UInt16.Parse m_PortNumberText.Text
                        m_HostNameText.Text, portNum

                m_Window.IsEnabled <- false
                m_LoginProgress.IsIndeterminate <- true
                m_BKTaskQueue.Enqueue ( fun () -> this.Login hostName portNo forceFlg )
            with
            | _ as x ->
                m_ErrorMessageTextBlock.Text <- x.Message
                m_Result <- DialogResult.Cancel

    /// <summary>
    ///  connect to specified controller.
    /// </summary>
    /// <param name="hostName">
    ///  controller host name or IP address string.
    /// </param>
    /// <param name="portNo">
    ///  TCP port number
    /// </param>
    /// <param name="forceFlg">
    ///   force login, or not.
    /// </param>
    member private _.Login ( hostName : string ) ( portNo : uint16 ) ( forceFlg : bool ) : Task<unit> =
        task {
            try
                let! t = CtrlConnection.Connect( m_Config.ClientCLIText ) hostName ( int portNo ) forceFlg
                m_Window.Dispatcher.InvokeAsync ( fun () ->
                    m_Result <- DialogResult.Ok( t )
                    m_CloseReason <- WinCloseReason.Internal

                    // save default value
                    let r = m_LoginByDirectoryRadio.IsChecked
                    if r.HasValue && r.Value then
                        UserOpeStat.LoginDialog_Type <- 0
                    else
                        UserOpeStat.LoginDialog_Type <- 1
                    UserOpeStat.LoginDialog_Directory <- m_ConfigDirText.Text
                    UserOpeStat.LoginDialog_HostName <- m_HostNameText.Text
                    UserOpeStat.LoginDialog_PortNumber <- m_PortNumberText.Text

                    m_Window.Close()
                ) |> ignore
            with
            | _ as x ->
                m_Window.Dispatcher.InvokeAsync ( fun () ->
                    m_Result <- DialogResult.Cancel
                    m_ErrorMessageTextBlock.Text <- x.Message
                    m_Window.IsEnabled <- true
                    m_LoginProgress.IsIndeterminate <- false
                    m_LoginButton.Focus()
                ) |> ignore
        }

    /// <summary>
    ///  "Cancel" button was clicked.
    /// </summary>
    member private _.OnClick_CancelButton() : unit =
        if m_BKTaskQueue.RunWaitCount = 0u then
            m_Result <- DialogResult.Cancel
            m_CloseReason <- WinCloseReason.Internal
            m_Window.Close()

    /// <summary>
    ///  "ConfigDirText" text box was updated.
    /// </summary>
    member private _.OnTextChanged_ConfigDirText() : unit =
        let s = m_LoginByDirectoryRadio.IsChecked
        if s.HasValue && s.Value = true then
            m_LoginButton.IsEnabled <- ( m_ConfigDirText.Text.Length > 0 )
        else
            m_LoginButton.IsEnabled <- ( m_HostNameText.Text.Length > 0 && m_PortNumberText.Text.Length > 0 )

    /// <summary>
    ///  "HostNameText" text box was updated.
    /// </summary>
    member private this.OnTextChanged_HostNameText() : unit =
        this.OnTextChanged_ConfigDirText()

    /// <summary>
    ///  "PortNumberText" text box was updated.
    /// </summary>
    member private this.OnTextChanged_PortNumberText() : unit =
        this.OnTextChanged_ConfigDirText()
