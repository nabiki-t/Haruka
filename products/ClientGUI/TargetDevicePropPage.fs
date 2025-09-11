//=============================================================================
// Haruka Software Storage.
// TargetDevicePropPage.fs : Implement the function to display Target Device property page in the main window.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Threading
open System.ComponentModel

open Haruka.Constants
open Haruka.IODataTypes
open Haruka.Commons
open Haruka.Client

//=============================================================================
// Class implementation

/// <summary>
///  TargetDevicePropPage class.
/// </summary>
/// <param name="m_Config">
///  Loaded GUI configurations.
/// </param>
/// <param name="m_PropPage">
///  The grid object for this property page that is loaded from the XAML file.
/// </param>
/// <param name="m_MainWindow">
///  The main window object.
/// </param>
/// <param name="m_ServerStatus">
///  Server status object.
/// </param>
/// <param name="m_CtrlConnection">
///  Connection.
/// </param>
/// <param name="m_NodeID">
///  node ID.
/// </param>
[< TypeConverter( typeof<PropPageConverter> ) >]
type TargetDevicePropPage(
    m_Config : GUIConfig,
    m_PropPage : Grid,
    m_MainWindow : IMainWindowIFForPP,
    m_ServerStatus : ServerStatus,
    m_CtrlConnection : CtrlConnection,
    m_NodeID : CONFNODE_T
) as this =

    // Get controll objects in main window.
    let m_StatusExpander = m_PropPage.FindName( "StatusExpander" ) :?> Expander
    let m_CurrentStatusImage = m_PropPage.FindName( "CurrentStatusImage" ) :?> Image
    let m_CurrentStatusTextBlock = m_PropPage.FindName( "CurrentStatusTextBlock" ) :?> TextBlock
    let m_StartButton = m_PropPage.FindName( "StartButton" ) :?> Button
    let m_StopButton = m_PropPage.FindName( "StopButton" ) :?> Button
    let m_CurrentSoftLimitTextBox = m_PropPage.FindName( "CurrentSoftLimitTextBox" ) :?> TextBox
    let m_CurrentHardLimitTextBox = m_PropPage.FindName( "CurrentHardLimitTextBox" ) :?> TextBox
    let m_CurrentLogLevelCombo = m_PropPage.FindName( "CurrentLogLevelCombo" ) :?> ComboBox
    let m_SetCurLogParamErrorMessageLabel = m_PropPage.FindName( "SetCurLogParamErrorMessageLabel" ) :?> TextBlock
    let m_ApplyCurLogParamButton = m_PropPage.FindName( "ApplyCurLogParamButton" ) :?> Button
    let m_LoadCurLogParamButton = m_PropPage.FindName( "LoadCurLogParamButton" ) :?> Button
    let m_SessionsExpander = m_PropPage.FindName( "SessionsExpander" ) :?> Expander
    let m_SessionTree = m_PropPage.FindName( "SessionTree" ) :?> TreeView
    let m_DestroySessionButton = m_PropPage.FindName( "DestroySessionButton" ) :?> Button
    let m_SessionParametersListView = m_PropPage.FindName( "SessionParametersListView" ) :?> ListView
    let m_ReceiveBytesHeightTextBlock = m_PropPage.FindName( "ReceiveBytesHeightTextBlock" ) :?> TextBlock
    let m_ReceiveBytesCanvas = m_PropPage.FindName( "ReceiveBytesCanvas" ) :?> Canvas
    let m_SendBytesHeightTextBlock = m_PropPage.FindName( "SendBytesHeightTextBlock" ) :?> TextBlock
    let m_SendBytesCanvas = m_PropPage.FindName( "SendBytesCanvas" ) :?> Canvas
    let m_ConfigurationExpander = m_PropPage.FindName( "ConfigurationExpander" ) :?> Expander
    let m_EditButton = m_PropPage.FindName( "EditButton" ) :?> Button
    let m_ApplyButton = m_PropPage.FindName( "ApplyButton" ) :?> Button
    let m_DiscardButton = m_PropPage.FindName( "DiscardButton" ) :?> Button
    let m_ErrorMessageLabel = m_PropPage.FindName( "ErrorMessageLabel" ) :?> TextBlock
    let m_TargetDeviceIDTextBox = m_PropPage.FindName( "TargetDeviceIDTextBox" ) :?> TextBox
    let m_TargetDeviceNameTextBox = m_PropPage.FindName( "TargetDeviceNameTextBox" ) :?> TextBox
    let m_MaxRecvDataSegmentLengthTextBox = m_PropPage.FindName( "MaxRecvDataSegmentLengthTextBox" ) :?> TextBox
    let m_MaxBurstLengthTextBox = m_PropPage.FindName( "MaxBurstLengthTextBox" ) :?> TextBox
    let m_FirstBurstLengthTextBox = m_PropPage.FindName( "FirstBurstLengthTextBox" ) :?> TextBox
    let m_DefaultTime2WaitTextBox = m_PropPage.FindName( "DefaultTime2WaitTextBox" ) :?> TextBox
    let m_DefaultTime2RetainTextBox = m_PropPage.FindName( "DefaultTime2RetainTextBox" ) :?> TextBox
    let m_MaxOutstandingR2TTextBox = m_PropPage.FindName( "MaxOutstandingR2TTextBox" ) :?> TextBox
    let m_SoftLimitTextBox = m_PropPage.FindName( "SoftLimitTextBox" ) :?> TextBox
    let m_HardLimitTextBox = m_PropPage.FindName( "HardLimitTextBox" ) :?> TextBox
    let m_LogLevelCombo = m_PropPage.FindName( "LogLevelCombo" ) :?> ComboBox

    // Graph Writer object
    let m_ReceiveBytesGraphWriter = new GraphWriter( m_ReceiveBytesCanvas, GraphColor.GC_RED, GuiConst.USAGE_GRAPH_PNT_CNT )
    let m_SendBytesGraphWriter = new GraphWriter( m_SendBytesCanvas, GraphColor.GC_GREEN, GuiConst.USAGE_GRAPH_PNT_CNT )

    // timer object
    let m_Timer = new DispatcherTimer()

    // controlls in "Sessions" expander updater object.
    let m_SessionsCtrlUpdater =
        new SessionsCtrlUpdater(
            m_SessionTree,
            m_DestroySessionButton,
            m_SessionParametersListView,
            m_ReceiveBytesHeightTextBlock,
            m_ReceiveBytesGraphWriter,
            m_SendBytesHeightTextBlock,
            m_SendBytesGraphWriter )

    // Initialize procedure for he property page.
    do
        // Set controller localized text
        m_Config.SetLocalizedText "TargetDevicePropPage" m_PropPage

        // Set event handler
        m_StartButton.Click.AddHandler ( fun _ _ -> this.OnClick_StartButton() )
        m_StopButton.Click.AddHandler ( fun _ _ -> this.OnClick_StopButton() )
        m_ApplyCurLogParamButton.Click.AddHandler ( fun _ _ -> this.OnClick_ApplyCurLogParamButton() )
        m_LoadCurLogParamButton.Click.AddHandler ( fun _ _ -> this.OnClick_LoadCurLogParamButton() )
        m_DestroySessionButton.Click.AddHandler ( fun _ _ -> this.OnClick_DestroySessionButton() )
        m_EditButton.Click.AddHandler ( fun _ _ -> this.OnClick_EditButton() )
        m_ApplyButton.Click.AddHandler ( fun _ _ -> this.OnClick_ApplyButton() )
        m_DiscardButton.Click.AddHandler ( fun _ _ -> this.OnClick_DiscardButton() )
        m_Timer.Tick.AddHandler ( fun _ _ -> this.OnTimer() )
        m_StatusExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetDevicePropPage" m_StatusExpander.Name true )
        m_StatusExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetDevicePropPage" m_StatusExpander.Name false )
        m_SessionsExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetDevicePropPage" m_SessionsExpander.Name true )
        m_SessionsExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetDevicePropPage" m_SessionsExpander.Name false )
        m_ConfigurationExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetDevicePropPage" m_ConfigurationExpander.Name true )
        m_ConfigurationExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetDevicePropPage" m_ConfigurationExpander.Name false )

        m_StartButton.IsEnabled <- false
        m_StopButton.IsEnabled <- false
        m_CurrentSoftLimitTextBox.IsEnabled <- false
        m_CurrentHardLimitTextBox.IsEnabled <- false
        m_CurrentLogLevelCombo.IsEnabled <- false
        m_ApplyCurLogParamButton.IsEnabled <- false
        m_LoadCurLogParamButton.IsEnabled <- false
        m_DestroySessionButton.IsEnabled <- false
        m_EditButton.IsEnabled <- false
        m_ApplyButton.IsEnabled <- false
        m_DiscardButton.IsEnabled <- false

        m_CurrentSoftLimitTextBox.Text <- ""
        m_CurrentHardLimitTextBox.Text <- ""
        m_CurrentLogLevelCombo.SelectedIndex <- -1
        m_SetCurLogParamErrorMessageLabel.Text <- ""
        m_ReceiveBytesHeightTextBlock.Text <- ""
        m_SendBytesHeightTextBlock.Text <- ""

        // Set default value
        m_StatusExpander.IsExpanded <- UserOpeStat.GetExpanded "TargetDevicePropPage" m_StatusExpander.Name
        m_SessionsExpander.IsExpanded <- UserOpeStat.GetExpanded "TargetDevicePropPage" m_SessionsExpander.Name
        m_ConfigurationExpander.IsExpanded <- UserOpeStat.GetExpanded "TargetDevicePropPage" m_ConfigurationExpander.Name

        m_Timer.Interval <- new TimeSpan( 0, 0, int Constants.RECOUNTER_SPAN_SEC )
        m_Timer.Start()

    ///////////////////////////////////////////////////////////////////////////
    // IPropPageInterface interface

    interface IPropPageInterface with

        // Get loaded property page UI object.
        override _.GetUIElement (): UIElement =
            m_PropPage

        // Set enable or disable to the property page.
        override _.SetEnable ( isEnable : bool ) : unit =
            m_PropPage.IsEnabled <- isEnable

        // The property page will be showed.
        override this.InitializeViewContent() : unit =
            let tdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetDevice
            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                // Get target device status
                do! m_MainWindow.UpdateRunningStatus()
                let activeTDID = m_MainWindow.GetRunningTDIDs()
                let actived = Seq.exists ( (=) tdn.TargetDeviceID ) activeTDID
                let isModified = ( tdn :> IConfigFileNode ).Modified = ModifiedStatus.Modified

                let! logParam = task {
                    if actived then
                        let! w = m_CtrlConnection.GetLogParameters tdn.TargetDeviceID
                        return Some w
                    else
                        return None
                }

                let! sessions, connections = task {
                    if actived then
                        let! ws = m_CtrlConnection.GetSession_InTargetDevice tdn.TargetDeviceID
                        let! wc = m_CtrlConnection.GetConnection_InTargetDevice tdn.TargetDeviceID
                        return ( Some ws, Some wc )
                    else
                        return None, None
                }

                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    this.ShowConfigValue ( actived && ( not isModified ) ) false
                    this.ShowCurrentLogParam actived isModified logParam

                    m_StartButton.IsEnabled <- not actived && ( not isModified )
                    m_StopButton.IsEnabled <- actived && ( not isModified )
                    //m_EditButton.IsEnabled <- not actived

                    if actived then
                        m_SessionsCtrlUpdater.InitialShow sessions.Value connections.Value
                    else
                        m_SessionsCtrlUpdater.Clear()

                    m_MainWindow.SetProgress false
                ) |> ignore
            })

        // The status is updated.
        override this.UpdateViewContent() : unit =
            ( this :> IPropPageInterface ).InitializeViewContent()

        // Set property page size
        override _.SetPageWidth ( width : float ) : unit =
            m_PropPage.Width <- width

        // Notification of closed this page
        override _.OnClosePage() : unit =
            // Stop timer
            m_Timer.Stop()

        // Get current node ID
        override _.GetNodeID() =
            m_NodeID

    ///////////////////////////////////////////////////////////////////////////
    // Event handler

    /// <summary>
    ///  On timer handler.
    /// </summary>
    member private this.OnTimer() : unit =
        m_Timer.Stop()
        let tdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetDevice
        m_MainWindow.ProcCtrlQuery false ( fun () -> task {
            // Get target device status
            do! m_MainWindow.UpdateRunningStatus()
            let activeTDID = m_MainWindow.GetRunningTDIDs()
            let actived = Seq.exists ( (=) tdn.TargetDeviceID ) activeTDID
            let! sessions, connections = task {
                if actived then
                    let! ws = m_CtrlConnection.GetSession_InTargetDevice tdn.TargetDeviceID
                    let! wc = m_CtrlConnection.GetConnection_InTargetDevice tdn.TargetDeviceID
                    return ( Some ws, Some wc )
                else
                    return None, None
            }

            m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                if actived then
                    m_SessionsCtrlUpdater.Update sessions.Value connections.Value
                else
                    m_SessionsCtrlUpdater.Clear()
                m_Timer.Start()
            ) |> ignore
        })

    /// <summary>
    ///  "Start" button is clicked.
    /// </summary>
    /// <remarks>
    ///  When the start button is enabled, it is considered that configuration is not modified.
    ///  After the start button is clicked, it is considered that the target device process is started.
    ///  If configuration editiong mode is enabled, edited values are discarded.
    /// </remarks>
    member private this.OnClick_StartButton() : unit =
        let tdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetDevice
        m_MainWindow.ProcCtrlQuery true ( fun () -> task {
            // Start the target device
            do! m_CtrlConnection.StartTargetDeviceProc tdn.TargetDeviceID

            // Get current status
            let! logParam = m_CtrlConnection.GetLogParameters tdn.TargetDeviceID
            let! sessions = m_CtrlConnection.GetSession_InTargetDevice tdn.TargetDeviceID
            let! connections = m_CtrlConnection.GetConnection_InTargetDevice tdn.TargetDeviceID

            // update running status cache
            do! m_MainWindow.UpdateRunningStatus()

            m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                this.ShowCurrentLogParam true false ( Some logParam )
                m_StartButton.IsEnabled <- false
                m_StopButton.IsEnabled <- true
                this.ShowConfigValue true false
                m_SessionsCtrlUpdater.InitialShow sessions connections
                m_MainWindow.NoticeUpdateStat tdn
                m_MainWindow.SetProgress false
            ) |> ignore
        })

    /// <summary>
    ///  "Stop" button is clicked.
    /// </summary>
    /// <remarks>
    ///  When the stop button is enabled, it is considered that configuration is not modified.
    ///  After the stop button is clicked, it is considered that the target device process is stopped.
    /// </remarks>
    member private this.OnClick_StopButton() : unit =
        let tdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetDevice
        m_MainWindow.ProcCtrlQuery true ( fun () -> task {
            // stop the target device
            do! m_CtrlConnection.KillTargetDeviceProc tdn.TargetDeviceID

            // update running status cache
            do! m_MainWindow.UpdateRunningStatus()

            m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                this.ShowCurrentLogParam false false None

                m_StartButton.IsEnabled <- true
                m_StopButton.IsEnabled <- false
                this.ShowConfigValue false false

                m_SessionTree.Items.Clear()
                m_SessionParametersListView.Items.Clear()
                m_ReceiveBytesHeightTextBlock.Text <- ""
                m_SendBytesHeightTextBlock.Text <- ""
                m_ReceiveBytesGraphWriter.SetValue Array.empty 1.0
                m_SendBytesGraphWriter.SetValue Array.empty 1.0

                m_MainWindow.NoticeUpdateStat tdn
                m_MainWindow.SetProgress false
            ) |> ignore
        })

    /// <summary>
    ///  "Apply" button is clicked.
    /// </summary>
    member private _.OnClick_ApplyCurLogParamButton() : unit =
        let r1, softLimit = UInt32.TryParse m_CurrentSoftLimitTextBox.Text
        let r2, hardLimit = UInt32.TryParse m_CurrentHardLimitTextBox.Text
        if not r1 || softLimit < Constants.LOGPARAM_MIN_SOFTLIMIT || softLimit > Constants.LOGPARAM_MAX_SOFTLIMIT then
            let msg =
                m_Config.MessagesText.GetMessage(
                    "MSG_INVALID_SOFT_LIMIT",
                    sprintf "%d" Constants.LOGPARAM_MIN_SOFTLIMIT,
                    sprintf "%d" Constants.LOGPARAM_MAX_SOFTLIMIT
                )
            m_SetCurLogParamErrorMessageLabel.Text <- msg
        elif not r2 || hardLimit < Constants.LOGPARAM_MIN_HARDLIMIT || hardLimit > Constants.LOGPARAM_MAX_HARDLIMIT then
            let msg =
                m_Config.MessagesText.GetMessage(
                    "MSG_INVALID_HARD_LIMIT",
                    sprintf "%d" Constants.LOGPARAM_MIN_HARDLIMIT,
                    sprintf "%d" Constants.LOGPARAM_MAX_HARDLIMIT
                )
            m_SetCurLogParamErrorMessageLabel.Text <- msg
        elif softLimit > hardLimit then
            let msg = m_Config.MessagesText.GetMessage( "MSG_INVALID_LOG_PARAM_RESTRICT" )
            m_SetCurLogParamErrorMessageLabel.Text <- msg
        else
            m_SetCurLogParamErrorMessageLabel.Text <- ""
            let logLevel =
                match m_CurrentLogLevelCombo.SelectedIndex with
                | 0 -> LogLevel.LOGLEVEL_VERBOSE
                | 1 -> LogLevel.LOGLEVEL_INFO
                | 2 -> LogLevel.LOGLEVEL_WARNING
                | 3 -> LogLevel.LOGLEVEL_ERROR
                | 4 -> LogLevel.LOGLEVEL_FAILED
                | 5 -> LogLevel.LOGLEVEL_OFF
                | _ -> LogLevel.LOGLEVEL_INFO

            let tdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetDevice
            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                let param : TargetDeviceConf.T_LogParameters = {
                    SoftLimit = softLimit;
                    HardLimit = hardLimit;
                    LogLevel = logLevel;
                }
                do! m_CtrlConnection.SetLogParameters tdn.TargetDeviceID param
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    m_MainWindow.SetProgress false
                ) |> ignore
            })

    /// <summary>
    ///  "Load" button is clicked.
    /// </summary>
    member private _.OnClick_LoadCurLogParamButton() : unit =
        let tdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetDevice
        m_SetCurLogParamErrorMessageLabel.Text <- ""
        m_MainWindow.ProcCtrlQuery true ( fun () -> task {
            let! logParam = m_CtrlConnection.GetLogParameters tdn.TargetDeviceID
            m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                m_CurrentSoftLimitTextBox.Text <- sprintf "%d" logParam.SoftLimit
                m_CurrentHardLimitTextBox.Text <- sprintf "%d" logParam.HardLimit
                m_CurrentLogLevelCombo.SelectedIndex <-
                    match logParam.LogLevel with
                    | LogLevel.LOGLEVEL_VERBOSE -> 0
                    | LogLevel.LOGLEVEL_INFO -> 1
                    | LogLevel.LOGLEVEL_WARNING -> 2
                    | LogLevel.LOGLEVEL_ERROR -> 3
                    | LogLevel.LOGLEVEL_FAILED -> 4
                    | LogLevel.LOGLEVEL_OFF -> 5
                m_MainWindow.SetProgress false
            ) |> ignore
        })

    /// <summary>
    ///  "Destroy Session" button is clicked.
    /// </summary>
    member private _.OnClick_DestroySessionButton() : unit =
        let tdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetDevice
        match m_SessionsCtrlUpdater.GetSelectedItemInfo() with
        | Some( SessionTreeItemType.Session( _, tviTSIH ) ) ->
            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                // Request to drop session to the controller.
                do! m_CtrlConnection.DestructSession tdn.TargetDeviceID tviTSIH

                // get updated session information
                let! sessions = m_CtrlConnection.GetSession_InTargetDevice tdn.TargetDeviceID
                let! connections = m_CtrlConnection.GetConnection_InTargetDevice tdn.TargetDeviceID

                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    m_SessionsCtrlUpdater.Update sessions connections
                    m_MainWindow.SetProgress false
                ) |> ignore
            } )

        | _ ->
            // If selected tree view item is not session, thie pattern is unexpected and ignore this event.
            ()

    /// <summary>
    ///  Enter configuration editing mode.
    /// </summary>
    /// <remarks>
    ///  If "Edit" button is enabled, it considered that the target device process is not running.
    /// </remarks>
    member private this.OnClick_EditButton() : unit =
        this.ShowConfigValue false true

    /// <summary>
    ///  Update configuration value and exit editing mode.
    /// </summary>
    /// <remarks>
    ///  This operation modify the configuration, thus "Start" button will be disabled because the target device process should not be started.
    /// </remarks>
    member private this.OnClick_ApplyButton() : unit =
        try
            let tdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetDevice

            let targetDeviceID =
                let r, w = UInt32.TryParse m_TargetDeviceIDTextBox.Text
                if not r then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_TARGET_DEVICE_ID",
                            "0",
                            sprintf "%d" UInt32.MaxValue
                        )
                    raise <| Exception msg
                else
                    tdid_me.fromPrim w

            let targetDeviceName = m_TargetDeviceNameTextBox.Text
            if targetDeviceName.Length > Constants.MAX_DEVICE_NAME_STR_LENGTH then
                let msg =
                    m_Config.MessagesText.GetMessage(
                        "MSG_INVALID_TARGET_DEVICE_NAME",
                        sprintf "%d" Constants.MAX_DEVICE_NAME_STR_LENGTH
                    )
                raise <| Exception msg

            let maxRecvDataSegmentLength =
                let r, w = UInt32.TryParse m_MaxRecvDataSegmentLengthTextBox.Text
                if not r || w < Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength || w > Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_MaxRecvDataSegmentLength",
                            sprintf "%d" Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength,
                            sprintf "%d" Constants.NEGOPARAM_MAX_MaxRecvDataSegmentLength
                        )
                    raise <| Exception msg
                else
                    w

            let maxBurstLength =
                let r, w = UInt32.TryParse m_MaxBurstLengthTextBox.Text
                if not r || w < Constants.NEGOPARAM_MIN_MaxBurstLength || w > Constants.NEGOPARAM_MAX_MaxBurstLength then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_MaxBurstLength",
                            sprintf "%d" Constants.NEGOPARAM_MIN_MaxBurstLength,
                            sprintf "%d" Constants.NEGOPARAM_MAX_MaxBurstLength
                        )
                    raise <| Exception msg
                else
                    w

            let firstBurstLength =
                let r, w = UInt32.TryParse m_FirstBurstLengthTextBox.Text
                if not r || w < Constants.NEGOPARAM_MIN_FirstBurstLength || w > Constants.NEGOPARAM_MAX_FirstBurstLength then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_FirstBurstLength",
                            sprintf "%d" Constants.NEGOPARAM_MIN_FirstBurstLength,
                            sprintf "%d" Constants.NEGOPARAM_MAX_FirstBurstLength
                        )
                    raise <| Exception msg
                else
                    w

            let defaultTime2Wait =
                let r, w = UInt16.TryParse m_DefaultTime2WaitTextBox.Text
                if not r || w < Constants.NEGOPARAM_MIN_DefaultTime2Wait || w > Constants.NEGOPARAM_MAX_DefaultTime2Wait then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_DefaultTime2Wait",
                            sprintf "%d" Constants.NEGOPARAM_MIN_DefaultTime2Wait,
                            sprintf "%d" Constants.NEGOPARAM_MAX_DefaultTime2Wait
                        )
                    raise <| Exception msg
                else
                    w

            let defaultTime2Retain =
                let r, w = UInt16.TryParse m_DefaultTime2RetainTextBox.Text
                if not r || w < Constants.NEGOPARAM_MIN_DefaultTime2Retain || w > Constants.NEGOPARAM_MAX_DefaultTime2Retain then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_DefaultTime2Retain",
                            sprintf "%d" Constants.NEGOPARAM_MIN_DefaultTime2Retain,
                            sprintf "%d" Constants.NEGOPARAM_MAX_DefaultTime2Retain
                        )
                    raise <| Exception msg
                else
                    w

            let maxOutstandingR2T =
                let r, w = UInt16.TryParse m_MaxOutstandingR2TTextBox.Text
                if not r || w < Constants.NEGOPARAM_MIN_MaxOutstandingR2T || w > Constants.NEGOPARAM_MAX_MaxOutstandingR2T then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_MaxOutstandingR2T",
                            sprintf "%d" Constants.NEGOPARAM_MIN_MaxOutstandingR2T,
                            sprintf "%d" Constants.NEGOPARAM_MAX_MaxOutstandingR2T
                        )
                    raise <| Exception msg
                else
                    w

            let softLimit =
                let r, w = UInt32.TryParse m_SoftLimitTextBox.Text
                if not r || w < Constants.LOGPARAM_MIN_SOFTLIMIT || w > Constants.LOGPARAM_MAX_SOFTLIMIT then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_SOFT_LIMIT",
                            sprintf "%d" Constants.LOGPARAM_MIN_SOFTLIMIT,
                            sprintf "%d" Constants.LOGPARAM_MAX_SOFTLIMIT
                        )
                    raise <| Exception msg
                else
                    w

            let hardLimit =
                let r, w = UInt32.TryParse m_HardLimitTextBox.Text
                if not r || w < Constants.LOGPARAM_MIN_HARDLIMIT || w > Constants.LOGPARAM_MAX_HARDLIMIT then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_HARD_LIMIT",
                            sprintf "%d" Constants.LOGPARAM_MIN_HARDLIMIT,
                            sprintf "%d" Constants.LOGPARAM_MAX_HARDLIMIT
                        )
                    raise <| Exception msg
                else
                    w

            let logLevel =
                match m_LogLevelCombo.SelectedIndex with
                | 0 -> LogLevel.LOGLEVEL_VERBOSE
                | 1 -> LogLevel.LOGLEVEL_INFO
                | 2 -> LogLevel.LOGLEVEL_WARNING
                | 3 -> LogLevel.LOGLEVEL_ERROR
                | 4 -> LogLevel.LOGLEVEL_FAILED
                | 5 -> LogLevel.LOGLEVEL_OFF
                | _ -> LogLevel.LOGLEVEL_INFO

            let negoParam : TargetDeviceConf.T_NegotiableParameters= {
                MaxRecvDataSegmentLength = maxRecvDataSegmentLength;
                MaxBurstLength = maxBurstLength;
                FirstBurstLength = firstBurstLength;
                DefaultTime2Wait = defaultTime2Wait;
                DefaultTime2Retain = defaultTime2Retain;
                MaxOutstandingR2T = maxOutstandingR2T;
            }
            let logParam : TargetDeviceConf.T_LogParameters = {
                SoftLimit = softLimit;
                HardLimit = hardLimit;
                LogLevel = logLevel;
            }

            let newNode = m_ServerStatus.UpdateTargetDeviceNode tdn targetDeviceID targetDeviceName negoParam logParam
            this.ShowConfigValue false false
            this.ShowCurrentLogParam false true None
            m_StartButton.IsEnabled <- false
            m_MainWindow.NoticeUpdateConfig newNode

        with
        | _ as x ->
            m_ErrorMessageLabel.Text <- x.Message

    /// <summary>
    ///  Discard modified configuration value.
    /// </summary>
    /// <remarks>
    ///  If configuration values are already modified, "Start" button will be disabled.
    /// </remarks>
    member private this.OnClick_DiscardButton() =
        let tdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetDevice
        this.ShowConfigValue false false
        m_StartButton.IsEnabled <- ( tdn :> IConfigFileNode ).Modified = ModifiedStatus.NotModified

    ///////////////////////////////////////////////////////////////////////////
    // Private method

    /// <summary>
    ///  Set current log parameters to the controller.
    /// </summary>
    /// <param name="actived">
    ///  The target device process is activated or not.
    ///  If the target device is stopped, the all of controllers of current log parameters are disabled.
    /// </param>
    /// <param name="modified">
    ///  Whether the configuration has been modified.
    /// </param>
    /// <param name="logParam">
    ///  Log parameters should be set to the controller.
    /// </param>
    member private _.ShowCurrentLogParam ( actived : bool ) ( modified : bool ) ( logParam : TargetDeviceConf.T_LogParameters option ) : unit =
        if actived && not modified then
            let img = m_Config.Icons.Get IconImageIndex.III_STATUS_RUNNING
            if img.IsSome && not ( Object.ReferenceEquals( m_CurrentStatusImage.Source, img ) ) then
                m_CurrentStatusImage.Source <- img.Value
            m_CurrentStatusTextBlock.Text <- "Running"
            m_CurrentSoftLimitTextBox.Text <- sprintf "%d" logParam.Value.SoftLimit
            m_CurrentHardLimitTextBox.Text <- sprintf "%d" logParam.Value.HardLimit
            m_CurrentLogLevelCombo.SelectedIndex <-
                match logParam.Value.LogLevel with
                | LogLevel.LOGLEVEL_VERBOSE -> 0
                | LogLevel.LOGLEVEL_INFO -> 1
                | LogLevel.LOGLEVEL_WARNING -> 2
                | LogLevel.LOGLEVEL_ERROR -> 3
                | LogLevel.LOGLEVEL_FAILED -> 4
                | LogLevel.LOGLEVEL_OFF -> 5
        else
            let statIcon =
                if not modified then
                    m_CurrentStatusTextBlock.Text <- "Unloaded"
                    IconImageIndex.III_STATUS_UNLOADED
                else
                    m_CurrentStatusTextBlock.Text <- "Modified"
                    IconImageIndex.III_STATUS_MODIFIED

            let img = m_Config.Icons.Get statIcon
            if img.IsSome && not ( Object.ReferenceEquals( m_CurrentStatusImage.Source, img ) ) then
                m_CurrentStatusImage.Source <- img.Value

            m_CurrentSoftLimitTextBox.Text <- ""
            m_CurrentHardLimitTextBox.Text <- ""
            m_CurrentLogLevelCombo.SelectedIndex <- -1

        m_CurrentSoftLimitTextBox.IsEnabled <- actived && not modified
        m_CurrentHardLimitTextBox.IsEnabled <- actived && not modified
        m_CurrentLogLevelCombo.IsEnabled <- actived && not modified
        m_ApplyCurLogParamButton.IsEnabled <- actived && not modified
        m_LoadCurLogParamButton.IsEnabled <- actived && not modified

    /// <summary>
    ///  Set configuration values to the controller.
    /// </summary>
    /// <param name="runningmode">
    ///  If the Target Device is running, this values is true.
    /// </param>
    /// <param name="editmode">
    ///  If configurations editiong mode is enabled, this value is true.
    /// </param>
    member private _.ShowConfigValue ( runningmode : bool ) ( editmode : bool ) : unit =
        let tdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetDevice

        m_EditButton.IsEnabled <- ( not editmode ) && ( not runningmode )
        m_ApplyButton.IsEnabled <- editmode && ( not runningmode )
        m_DiscardButton.IsEnabled <- editmode && ( not runningmode )
        m_ErrorMessageLabel.Text <- ""
        m_TargetDeviceIDTextBox.Text <- sprintf "%d" ( tdid_me.toPrim tdn.TargetDeviceID )
        m_TargetDeviceIDTextBox.IsEnabled <- editmode && ( not runningmode )
        m_TargetDeviceNameTextBox.Text <- tdn.TargetDeviceName
        m_TargetDeviceNameTextBox.IsEnabled <- editmode && ( not runningmode )
        m_MaxRecvDataSegmentLengthTextBox.Text <- sprintf "%d" tdn.NegotiableParameters.MaxRecvDataSegmentLength
        m_MaxRecvDataSegmentLengthTextBox.IsEnabled <- editmode && ( not runningmode )
        m_MaxBurstLengthTextBox.Text <- sprintf "%d" tdn.NegotiableParameters.MaxBurstLength
        m_MaxBurstLengthTextBox.IsEnabled <- editmode && ( not runningmode )
        m_FirstBurstLengthTextBox.Text <- sprintf "%d" tdn.NegotiableParameters.FirstBurstLength
        m_FirstBurstLengthTextBox.IsEnabled <- editmode && ( not runningmode )
        m_DefaultTime2WaitTextBox.Text <- sprintf "%d" tdn.NegotiableParameters.DefaultTime2Wait
        m_DefaultTime2WaitTextBox.IsEnabled <- editmode && ( not runningmode )
        m_DefaultTime2RetainTextBox.Text <- sprintf "%d" tdn.NegotiableParameters.DefaultTime2Retain
        m_DefaultTime2RetainTextBox.IsEnabled <- editmode && ( not runningmode )
        m_MaxOutstandingR2TTextBox.Text <- sprintf "%d" tdn.NegotiableParameters.MaxOutstandingR2T
        m_MaxOutstandingR2TTextBox.IsEnabled <- editmode && ( not runningmode )
        m_SoftLimitTextBox.Text <- sprintf "%d" tdn.LogParameters.SoftLimit
        m_SoftLimitTextBox.IsEnabled <- editmode && ( not runningmode )
        m_HardLimitTextBox.Text <- sprintf "%d" tdn.LogParameters.HardLimit
        m_HardLimitTextBox.IsEnabled <- editmode && ( not runningmode )
        m_LogLevelCombo.SelectedIndex <-
            match tdn.LogParameters.LogLevel with
            | LogLevel.LOGLEVEL_VERBOSE -> 0
            | LogLevel.LOGLEVEL_INFO -> 1
            | LogLevel.LOGLEVEL_WARNING -> 2
            | LogLevel.LOGLEVEL_ERROR -> 3
            | LogLevel.LOGLEVEL_FAILED -> 4
            | LogLevel.LOGLEVEL_OFF -> 5
        m_LogLevelCombo.IsEnabled <- editmode && ( not runningmode )

