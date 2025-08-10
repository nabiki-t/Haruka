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
///  ControllerPropPage class.
/// </summary>
/// <param name="m_Config">
///  Loaded GUI configurations.
/// </param>
/// <param name="m_PropPage">
///  The grid object for this property page that is loaded from the XAML file.
/// </param>
/// <param name="m_TreeViewItem">
///  The TreeViewItem of the TreeView on the MainWindow that corresponds to this property page.
/// </param>
/// <param name="m_MainWindow">
///  The main window object.
/// </param>
/// <param name="m_ServerStatus">
///  Server status object.
/// </param>
/// <param name="m_NodeID">
///  node ID.
/// </param>
[< TypeConverter( typeof<PropPageConverter> ) >]
type ControllerPropPage(
    m_Config : GUIConfig,
    m_PropPage : Grid,
    m_MainWindow : IMainWindowIFForPP,
    m_ServerStatus : ServerStatus,
    m_NodeID : CONFNODE_T
) as this =

    // Get controll objects in main window.
    let m_ConfigurationExpander = m_PropPage.FindName( "ConfigurationExpander" ) :?> Expander
    let m_EditButton = m_PropPage.FindName( "EditButton" ) :?> Button
    let m_ApplyButton = m_PropPage.FindName( "ApplyButton" ) :?> Button
    let m_DiscardButton = m_PropPage.FindName( "DiscardButton" ) :?> Button
    let m_ErrorMessageLabel = m_PropPage.FindName( "ErrorMessageLabel" ) :?> TextBlock
    let m_PortNumberTextBox = m_PropPage.FindName( "PortNumberTextBox" ) :?> TextBox
    let m_AddressTextBox = m_PropPage.FindName( "AddressTextBox" ) :?> TextBox
    let m_IPWhiteListListBox = m_PropPage.FindName( "IPWhiteListListBox" ) :?> ListBox
    let m_AddIPWhiteListButton = m_PropPage.FindName( "AddIPWhiteListButton" ) :?> Button
    let m_RemoveIPWhiteListButton = m_PropPage.FindName( "RemoveIPWhiteListButton" ) :?> Button
    let m_StanserdOutputRadio = m_PropPage.FindName( "StanserdOutputRadio" ) :?> RadioButton
    let m_StdoutLMTotalLimitTextBox = m_PropPage.FindName( "StdoutLMTotalLimitTextBox" ) :?> TextBox
    let m_FileRadio = m_PropPage.FindName( "FileRadio" ) :?> RadioButton
    let m_FileTotalLimitTextBox = m_PropPage.FindName( "FileTotalLimitTextBox" ) :?> TextBox
    let m_MaxFileCountTextBox = m_PropPage.FindName( "MaxFileCountTextBox" ) :?> TextBox
    let m_ForceSyncCombo = m_PropPage.FindName( "ForceSyncCombo" ) :?> ComboBox
    let m_SoftLimitTextBox = m_PropPage.FindName( "SoftLimitTextBox" ) :?> TextBox
    let m_HardLimitTextBox = m_PropPage.FindName( "HardLimitTextBox" ) :?> TextBox
    let m_LogLevelCombo = m_PropPage.FindName( "LogLevelCombo" ) :?> ComboBox

    // Initialize procedure for wihe property page.
    do
        // Set controller localized text
        m_Config.SetLocalizedText "ControllerPropPage" m_PropPage

        // Set event handler
        m_EditButton.Click.AddHandler ( fun _ _ -> this.OnClick_EditButton() )
        m_ApplyButton.Click.AddHandler ( fun _ _ -> this.OnClick_ApplyButton() )
        m_DiscardButton.Click.AddHandler ( fun _ _ -> this.OnClick_DiscardButton() )
        m_IPWhiteListListBox.MouseDoubleClick.AddHandler ( fun _ _ -> this.OnDoubleClick_IPWhiteListListBox() )
        m_AddIPWhiteListButton.Click.AddHandler ( fun _ _ -> this.OnClick_AddIPWhiteListButton() )
        m_RemoveIPWhiteListButton.Click.AddHandler ( fun _ _ -> this.OnClick_RemoveIPWhiteListButton() )
        m_StanserdOutputRadio.Click.AddHandler ( fun _ _ -> this.OnClick_StanserdOutputRadio() )
        m_FileRadio.Click.AddHandler ( fun _ _ -> this.OnClick_FileRadio() )
        m_ConfigurationExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "ControllerPropPage" m_ConfigurationExpander.Name true )
        m_ConfigurationExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "ControllerPropPage" m_ConfigurationExpander.Name false )

        m_LogLevelCombo.SelectedIndex <- 0

        // Set default value
        m_ConfigurationExpander.IsExpanded <- UserOpeStat.GetExpanded "ControllerPropPage" m_ConfigurationExpander.Name

    ///////////////////////////////////////////////////////////////////////////
    // IPropPageInterface interface

    /// <inheritdoc />
    interface IPropPageInterface with
        // Get loaded property page UI object.
        override _.GetUIElement (): UIElement =
            m_PropPage

        // Set enable or disable to the property page.
        override _.SetEnable ( isEnable : bool ) : unit =
            m_PropPage.IsEnabled <- isEnable

        // The property page will be showed.
        override this.InitializeViewContent() : unit =
            // Show initial status
            this.ShowConfigValue false

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
            m_NodeID

    ///////////////////////////////////////////////////////////////////////////
    // Private method

    /// <summary>
    ///   Set configuration values to the controllers.
    /// </summary>
    /// <param name="enabled">
    ///   If edition mode is set ( "Edit" button had been pushed ), enabled is true.
    /// </param>
    member private _.ShowConfigValue ( enabled : bool ) : unit =
        let ctrln = m_ServerStatus.GetNode m_NodeID :?> ConfNode_Controller

        m_EditButton.IsEnabled <- ( not enabled )
        m_ApplyButton.IsEnabled <- enabled
        m_DiscardButton.IsEnabled <- enabled
        m_ErrorMessageLabel.Text <- ""

        m_PortNumberTextBox.Text <- sprintf "%d" ctrln.RemoteCtrlValue.PortNum
        m_PortNumberTextBox.IsEnabled <- enabled
        m_AddressTextBox.Text <- ctrln.RemoteCtrlValue.Address
        m_AddressTextBox.IsEnabled <- enabled
        m_IPWhiteListListBox.Items.Clear()
        for itr in ctrln.RemoteCtrlValue.WhiteList do
            m_IPWhiteListListBox.Items.Add( IPCondition.ToString itr ) |> ignore
        m_IPWhiteListListBox.IsEnabled <- true      // always true
        m_AddIPWhiteListButton.IsEnabled <- enabled && ( m_IPWhiteListListBox.Items.Count < Constants.MAX_IP_WHITELIST_COUNT )
        m_RemoveIPWhiteListButton.IsEnabled <- enabled && ( m_IPWhiteListListBox.Items.Count > 0 )

        match ctrln.LogMaintenanceValue.OutputDest with
        | HarukaCtrlConf.U_ToStdout( x ) ->
            m_StanserdOutputRadio.IsChecked <- true
            m_StanserdOutputRadio.IsEnabled <- enabled
            m_StdoutLMTotalLimitTextBox.Text <- sprintf "%d" x
            m_StdoutLMTotalLimitTextBox.IsEnabled <- true && enabled
            m_FileRadio.IsChecked <- false
            m_FileRadio.IsEnabled <- enabled
            m_FileTotalLimitTextBox.Text <- ""
            m_FileTotalLimitTextBox.IsEnabled <- false
            m_MaxFileCountTextBox.Text <- ""
            m_MaxFileCountTextBox.IsEnabled <- false
            m_ForceSyncCombo.SelectedIndex <- -1
            m_ForceSyncCombo.IsEnabled <- false
        | HarukaCtrlConf.U_ToFile( x ) ->
            m_StanserdOutputRadio.IsChecked <- false
            m_StanserdOutputRadio.IsEnabled <- enabled
            m_StdoutLMTotalLimitTextBox.IsEnabled <- false
            m_StdoutLMTotalLimitTextBox.Text <- ""
            m_FileRadio.IsChecked <- true
            m_FileRadio.IsEnabled <- enabled
            m_FileTotalLimitTextBox.Text <- sprintf "%d" x.TotalLimit
            m_FileTotalLimitTextBox.IsEnabled <- true && enabled
            m_MaxFileCountTextBox.Text <- sprintf "%d" x.MaxFileCount
            m_MaxFileCountTextBox.IsEnabled <- true && enabled
            m_ForceSyncCombo.SelectedIndex <- if x.ForceSync then 0 else 1
            m_ForceSyncCombo.IsEnabled <- true && enabled

        m_SoftLimitTextBox.Text <- sprintf "%d" ctrln.LogParametersValue.SoftLimit
        m_SoftLimitTextBox.IsEnabled <- enabled
        m_HardLimitTextBox.Text <- sprintf "%d" ctrln.LogParametersValue.HardLimit
        m_HardLimitTextBox.IsEnabled <- enabled
        m_LogLevelCombo.SelectedIndex <-
            match ctrln.LogParametersValue.LogLevel with
            | LogLevel.LOGLEVEL_VERBOSE -> 0
            | LogLevel.LOGLEVEL_INFO -> 1
            | LogLevel.LOGLEVEL_WARNING -> 2
            | LogLevel.LOGLEVEL_ERROR -> 3
            | LogLevel.LOGLEVEL_FAILED -> 4
            | LogLevel.LOGLEVEL_OFF -> 5
        m_LogLevelCombo.IsEnabled <- enabled

    ///////////////////////////////////////////////////////////////////////////
    // Private method

    /// "Edit" button is clicked.
    member private this.OnClick_EditButton() =
        this.ShowConfigValue true

    /// <summary>
    ///  "Apply" button is clicked.
    /// </summary>
    /// <remarks>
    ///  Input values are checked and if all values are valid, the configuration node is updated. 
    /// </remarks>
    member private this.OnClick_ApplyButton() =
        try
            let r, portNumber = UInt16.TryParse m_PortNumberTextBox.Text
            if not r || portNumber = 0us then
                raise <| Exception( m_Config.MessagesText.GetMessage( "MSG_INVALID_PORTNO" ) )

            let address = m_AddressTextBox.Text
            if address.Length <= 0 || address.Length > Constants.MAX_CTRL_ADDRESS_STR_LENGTH then
                let msg = m_Config.MessagesText.GetMessage( "MSG_INVALID_ADDRESS", ( sprintf "%d" Constants.MAX_CTRL_ADDRESS_STR_LENGTH ) )
                raise <| Exception( msg )

            let IPWhiteList = seq {
                let cnt = m_IPWhiteListListBox.Items.Count
                if cnt > 0 then
                    for i = 0 to cnt - 1 do
                        yield m_IPWhiteListListBox.Items.Item(i) |> string |> IPCondition.Parse
            }

            let logMent =
                let s = m_StanserdOutputRadio.IsChecked
                if s.HasValue && s.Value then
                    let r, totalLimit = UInt32.TryParse m_StdoutLMTotalLimitTextBox.Text
                    if not r || totalLimit < Constants.LOGMNT_MIN_TOTALLIMIT || totalLimit > Constants.LOGMNT_MAX_TOTALLIMIT then
                        let msg =
                            m_Config.MessagesText.GetMessage(
                                "MSG_INVALID_TOTAL_LIMIT",
                                sprintf "%d" Constants.LOGMNT_MIN_TOTALLIMIT,
                                sprintf "%d" Constants.LOGMNT_MAX_TOTALLIMIT
                            )
                        raise <| Exception( msg )
                    HarukaCtrlConf.U_ToStdout( totalLimit )
                else
                    let r, totalLimit = UInt32.TryParse m_FileTotalLimitTextBox.Text
                    if not r || totalLimit < Constants.LOGMNT_MIN_TOTALLIMIT || totalLimit > Constants.LOGMNT_MAX_TOTALLIMIT then
                        let msg =
                            m_Config.MessagesText.GetMessage(
                                "MSG_INVALID_TOTAL_LIMIT",
                                sprintf "%d" Constants.LOGMNT_MIN_TOTALLIMIT,
                                sprintf "%d" Constants.LOGMNT_MAX_TOTALLIMIT
                            )
                        raise <| Exception( msg )

                    let r, maxFileCount = UInt32.TryParse m_MaxFileCountTextBox.Text
                    if not r || totalLimit < Constants.LOGMNT_MIN_MAXFILECOUNT || totalLimit > Constants.LOGMNT_MAX_MAXFILECOUNT then
                        let msg =
                            m_Config.MessagesText.GetMessage(
                                "MSG_INVALID_MAX_FILE_COUNT",
                                sprintf "%d" Constants.LOGMNT_MIN_MAXFILECOUNT,
                                sprintf "%d" Constants.LOGMNT_MAX_MAXFILECOUNT
                            )
                        raise <| Exception( msg )

                    let forceSync = ( m_ForceSyncCombo.SelectedIndex = 0 )
                    HarukaCtrlConf.U_ToFile( {
                        TotalLimit = totalLimit;
                        MaxFileCount = maxFileCount;
                        ForceSync = forceSync;
                    })

            let r, softLimit = UInt32.TryParse m_SoftLimitTextBox.Text
            if not r || softLimit < Constants.LOGPARAM_MIN_SOFTLIMIT || softLimit > Constants.LOGPARAM_MAX_SOFTLIMIT then
                let msg =
                    m_Config.MessagesText.GetMessage(
                        "MSG_INVALID_SOFT_LIMIT",
                        sprintf "%d" Constants.LOGPARAM_MIN_SOFTLIMIT,
                        sprintf "%d" Constants.LOGPARAM_MAX_SOFTLIMIT
                    )
                raise <| Exception( msg )

            let r, hardLimit = UInt32.TryParse m_HardLimitTextBox.Text
            if not r || hardLimit < Constants.LOGPARAM_MIN_HARDLIMIT || hardLimit > Constants.LOGPARAM_MAX_HARDLIMIT then
                let msg =
                    m_Config.MessagesText.GetMessage(
                        "MSG_INVALID_HARD_LIMIT",
                        sprintf "%d" Constants.LOGPARAM_MIN_HARDLIMIT,
                        sprintf "%d" Constants.LOGPARAM_MAX_HARDLIMIT
                    )
                raise <| Exception( msg )

            let logLevel =
                match m_LogLevelCombo.SelectedIndex with
                | 0 -> LogLevel.LOGLEVEL_VERBOSE
                | 1 -> LogLevel.LOGLEVEL_INFO
                | 2 -> LogLevel.LOGLEVEL_WARNING
                | 3 -> LogLevel.LOGLEVEL_ERROR
                | 4 -> LogLevel.LOGLEVEL_FAILED
                | 5 -> LogLevel.LOGLEVEL_OFF
                | _ -> LogLevel.LOGLEVEL_INFO
            
            m_ServerStatus.UpdateControllerNode {
                RemoteCtrl = Some {
                    PortNum = portNumber;
                    Address = address;
                    WhiteList = IPWhiteList |> Seq.toList;
                };
                LogMaintenance = Some {
                    OutputDest = logMent;
                }
                LogParameters = Some {
                    SoftLimit = softLimit;
                    HardLimit = hardLimit;
                    LogLevel = logLevel;
                }
            }
            |> m_MainWindow.NoticeUpdateConfig

            this.ShowConfigValue false
        with
        | _ as x ->
            m_ErrorMessageLabel.Text <- x.Message

    /// "Discard" button is clicked.
    member private _.OnClick_DiscardButton() =
        this.ShowConfigValue false

    /// The items in IPWhiteList list box is double clicked.
    member private _.OnDoubleClick_IPWhiteListListBox() =
        let s = m_IPWhiteListListBox.SelectedIndex
        if s <> -1 then
            let v = m_IPWhiteListListBox.Items.Item( s ) :?> string
            let ipv = IPCondition.Parse v
            let d = new EditIPWhiteListDialog( m_Config, Some ipv )
            match d.Show() with
            | DialogResult.Cancel ->
                ()
            | DialogResult.Ok( x ) ->
                m_IPWhiteListListBox.Items.Item( s ) <- ( IPCondition.ToString x )

    /// The button of add IP white list is clicked.
    member private _.OnClick_AddIPWhiteListButton() =
        let d = new EditIPWhiteListDialog( m_Config, None )
        if m_IPWhiteListListBox.Items.Count < Constants.MAX_IP_WHITELIST_COUNT then
            match d.Show() with
            | DialogResult.Cancel ->
                ()
            | DialogResult.Ok( x ) ->
                m_IPWhiteListListBox.Items.Add( IPCondition.ToString x ) |> ignore
                m_RemoveIPWhiteListButton.IsEnabled <- true
                if m_IPWhiteListListBox.Items.Count >= Constants.MAX_IP_WHITELIST_COUNT then
                    m_AddIPWhiteListButton.IsEnabled <- false

    /// The button of remove IP white list is clicked.
    member private _.OnClick_RemoveIPWhiteListButton() =
        let s = m_IPWhiteListListBox.SelectedIndex
        if s <> -1 then
            m_IPWhiteListListBox.Items.RemoveAt s
            m_AddIPWhiteListButton.IsEnabled <- true
            if m_IPWhiteListListBox.Items.Count <= 0 then
                m_RemoveIPWhiteListButton.IsEnabled <- false

    /// "Standerd Output" radio button in Log Maintenance is clicked.
    member private _.OnClick_StanserdOutputRadio() =
        m_StanserdOutputRadio.IsChecked <- true
        m_StdoutLMTotalLimitTextBox.IsEnabled <- true
        m_FileRadio.IsChecked <- false
        m_FileTotalLimitTextBox.IsEnabled <- false
        m_MaxFileCountTextBox.IsEnabled <- false
        m_ForceSyncCombo.IsEnabled <- false

    /// "File" radio button in Log Maintenance is clicked.
    member private _.OnClick_FileRadio() =
        m_StanserdOutputRadio.IsChecked <- false
        m_StdoutLMTotalLimitTextBox.IsEnabled <- false
        m_FileRadio.IsChecked <- true
        m_FileTotalLimitTextBox.IsEnabled <- true
        m_MaxFileCountTextBox.IsEnabled <- true
        m_ForceSyncCombo.IsEnabled <- true

