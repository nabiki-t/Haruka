//=============================================================================
// Haruka Software Storage.
// PlainFileMediaPropPage.fs : Implement the function to display Plain File Media property page in the main window.
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
///  PlainFileMediaPropPage class.
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
type PlainFileMediaPropPage(
    m_Config : GUIConfig,
    m_PropPage : Grid,
    m_MainWindow : IMainWindowIFForPP,
    m_ServerStatus : ServerStatus,
    m_CtrlConnection : CtrlConnection,
    m_NodeID : CONFNODE_T
) as this =

    // Get controll objects in the property page.
    let m_UsageExpander = m_PropPage.FindName( "UsageExpander" ) :?> Expander
    let m_ReadBytesHeightTextBlock = m_PropPage.FindName( "ReadBytesHeightTextBlock" ) :?> TextBlock
    let m_ReadBytesCanvas = m_PropPage.FindName( "ReadBytesCanvas" ) :?> Canvas
    let m_WrittenBytesHeightTextBlock = m_PropPage.FindName( "WrittenBytesHeightTextBlock" ) :?> TextBlock
    let m_WrittenBytesCanvas = m_PropPage.FindName( "WrittenBytesCanvas" ) :?> Canvas
    let m_ReadTickCountHeightTextBlock = m_PropPage.FindName( "ReadTickCountHeightTextBlock" ) :?> TextBlock
    let m_ReadTickCountCanvas = m_PropPage.FindName( "ReadTickCountCanvas" ) :?> Canvas
    let m_WriteTickCountHeightTextBlock = m_PropPage.FindName( "WriteTickCountHeightTextBlock" ) :?> TextBlock
    let m_WriteTickCountCanvas = m_PropPage.FindName( "WriteTickCountCanvas" ) :?> Canvas
    let m_ReadCountHeightTextBlock = m_PropPage.FindName( "ReadCountHeightTextBlock" ) :?> TextBlock
    let m_ReadCountCanvas = m_PropPage.FindName( "ReadCountCanvas" ) :?> Canvas
    let m_WrittenCountHeightTextBlock = m_PropPage.FindName( "WrittenCountHeightTextBlock" ) :?> TextBlock
    let m_WrittenCountCanvas = m_PropPage.FindName( "WrittenCountCanvas" ) :?> Canvas

    let m_ConfigurationExpander = m_PropPage.FindName( "ConfigurationExpander" ) :?> Expander
    let m_EditButton = m_PropPage.FindName( "EditButton" ) :?> Button
    let m_ApplyButton = m_PropPage.FindName( "ApplyButton" ) :?> Button
    let m_DiscardButton = m_PropPage.FindName( "DiscardButton" ) :?> Button
    let m_ErrorMessageLabel = m_PropPage.FindName( "ErrorMessageLabel" ) :?> TextBlock
    let m_IdentNumberTextBox = m_PropPage.FindName( "IdentNumberTextBox" ) :?> TextBox
    let m_MediaNameTextBox = m_PropPage.FindName( "MediaNameTextBox" ) :?> TextBox
    let m_FileNameTextBox = m_PropPage.FindName( "FileNameTextBox" ) :?> TextBox
    let m_MaxMultiplicityTextBox = m_PropPage.FindName( "MaxMultiplicityTextBox" ) :?> TextBox
    let m_QueueWaitTimeOutTextBox = m_PropPage.FindName( "QueueWaitTimeOutTextBox" ) :?> TextBox
    let m_WriteProtectCombo = m_PropPage.FindName( "WriteProtectCombo" ) :?> ComboBox

    // Graph Writer object
    let m_ReadBytesGraphWriter = new GraphWriter( m_ReadBytesCanvas, GraphColor.GC_RED, GuiConst.USAGE_GRAPH_PNT_CNT )
    let m_WrittenBytesGraphWriter = new GraphWriter( m_WrittenBytesCanvas, GraphColor.GC_YELLOW, GuiConst.USAGE_GRAPH_PNT_CNT )
    let m_ReadTickCountGraphWriter = new GraphWriter( m_ReadTickCountCanvas, GraphColor.GC_GREEN, GuiConst.USAGE_GRAPH_PNT_CNT )
    let m_WriteTickCountGraphWriter = new GraphWriter( m_WriteTickCountCanvas, GraphColor.GC_CYAN, GuiConst.USAGE_GRAPH_PNT_CNT )
    let m_ReadCountGraphWriter = new GraphWriter( m_ReadCountCanvas, GraphColor.GC_BLUE, GuiConst.USAGE_GRAPH_PNT_CNT )
    let m_WrittenCountGraphWriter = new GraphWriter( m_WrittenCountCanvas, GraphColor.GC_PURPLE, GuiConst.USAGE_GRAPH_PNT_CNT )

    // Graph updater
    let m_GraphUpdater = new MediaUsageCtrlUpdater(
        m_ReadBytesHeightTextBlock, m_ReadBytesGraphWriter,
        m_WrittenBytesHeightTextBlock, m_WrittenBytesGraphWriter,
        m_ReadTickCountHeightTextBlock, m_ReadTickCountGraphWriter,
        m_WriteTickCountHeightTextBlock, m_WriteTickCountGraphWriter,
        m_ReadCountHeightTextBlock, m_ReadCountGraphWriter,
        m_WrittenCountHeightTextBlock, m_WrittenCountGraphWriter
    )

    // timer object
    let m_Timer = new DispatcherTimer()

    // Initialize procedure for the property page.
    do
        // Set controller localized text
        m_Config.SetLocalizedText "PlainFileMediaPropPage" m_PropPage

        // Set event handler
        m_EditButton.Click.AddHandler ( fun _ _ -> this.OnClick_EditButton() )
        m_ApplyButton.Click.AddHandler ( fun _ _ -> this.OnClick_ApplyButton() )
        m_DiscardButton.Click.AddHandler ( fun _ _ -> this.OnClick_DiscardButton() )
        m_Timer.Tick.AddHandler ( fun _ _ -> this.OnTimer() )
        m_UsageExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "PlainFileMediaPropPage" m_UsageExpander.Name true )
        m_UsageExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "PlainFileMediaPropPage" m_UsageExpander.Name false )
        m_ConfigurationExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "PlainFileMediaPropPage" m_ConfigurationExpander.Name true )
        m_ConfigurationExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "PlainFileMediaPropPage" m_ConfigurationExpander.Name false )

        m_EditButton.IsEnabled <- false
        m_ApplyButton.IsEnabled <- false
        m_DiscardButton.IsEnabled <- false

        m_ReadBytesHeightTextBlock.Text <- ""
        m_WrittenBytesHeightTextBlock.Text <- ""
        m_ReadTickCountHeightTextBlock.Text <- ""
        m_WriteTickCountHeightTextBlock.Text <- ""
        m_ReadCountHeightTextBlock.Text <- ""
        m_WrittenCountHeightTextBlock.Text <- ""

        m_Timer.Interval <- new TimeSpan( 0, 0, int Constants.RECOUNTER_SPAN_SEC )
        m_Timer.Start()

        // Set default value
        m_UsageExpander.IsExpanded <- UserOpeStat.GetExpanded "PlainFileMediaPropPage" m_UsageExpander.Name
        m_ConfigurationExpander.IsExpanded <- UserOpeStat.GetExpanded "PlainFileMediaPropPage" m_ConfigurationExpander.Name

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
            let mn = m_ServerStatus.GetNode m_NodeID :?> IMediaNode
            let tgn = ( m_ServerStatus.GetAncestorTargetGroup mn ).Value
            let tdn = ( m_ServerStatus.GetAncestorTargetDevice mn ).Value
            let lu = ( m_ServerStatus.GetAncestorLogicalUnit mn ).Value

            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                do! m_MainWindow.UpdateRunningStatus()
                let struct( tdActived, tgLoaded ) =
                    m_MainWindow.IsTGLoaded tdn.TargetDeviceID tgn.TargetGroupID

                if tdActived && tgLoaded then
                    let! luStat = m_CtrlConnection.GetMediaStatus tdn.TargetDeviceID lu.LUN mn.IdentNumber
                    m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                        this.ShowConfigValue true false
                        m_GraphUpdater.Update luStat.ReadBytesCount luStat.WrittenBytesCount luStat.ReadTickCount luStat.WriteTickCount
                        m_MainWindow.SetProgress false
                    ) |> ignore
                else
                    m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                        this.ShowConfigValue false false
                        m_GraphUpdater.Clear()
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
    ///  "Edit" button was clicked.
    /// </summary>
    /// <remarks>
    ///  If this button is enabled, it is considered that the target device is unloaded, or, the target group was is unloaded state.
    /// </remarks>
    member private this.OnClick_EditButton() : unit =
        this.ShowConfigValue false true

    /// <summary>
    ///  "Apply" button was clicked.
    /// </summary>
    member private this.OnClick_ApplyButton() : unit = 
        try
            let mn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_PlainFileMedia

            let identNumber =
                let r, w = UInt32.TryParse m_IdentNumberTextBox.Text
                if not r then
                    let msg = m_Config.MessagesText.GetMessage( "MSG_INVALID_MEDIA_INDEX_NUMBER" )
                    raise <| Exception msg
                w

            let mediaName = m_MediaNameTextBox.Text
            if mediaName.Length > Constants.MAX_MEDIA_NAME_STR_LENGTH then
                let msg = m_Config.MessagesText.GetMessage( "MSG_INVALID_MEDIA_NAME", sprintf "%d" Constants.MAX_MEDIA_NAME_STR_LENGTH )
                raise <| Exception msg

            let fileName = m_FileNameTextBox.Text
            if fileName.Length > Constants.MAX_FILENAME_STR_LENGTH then
                let msg = m_Config.MessagesText.GetMessage( "MSG_INVALID_FILE_NAME", sprintf "%d" Constants.MAX_FILENAME_STR_LENGTH )
                raise <| Exception msg

            let maxMultiplicity =
                let r, w = UInt32.TryParse m_IdentNumberTextBox.Text
                if not r || w < Constants.PLAINFILE_MIN_MAXMULTIPLICITY || w > Constants.PLAINFILE_MAX_MAXMULTIPLICITY then
                    let msg = m_Config.MessagesText.GetMessage( "MSG_INVALID_FILE_NAME", sprintf "%d" Constants.MAX_FILENAME_STR_LENGTH )
                    raise <| Exception msg
                w

            let queueWaitTimeOut =
                let r, w = Int32.TryParse m_QueueWaitTimeOutTextBox.Text
                if not r || w < Constants.PLAINFILE_MIN_QUEUEWAITTIMEOUT || w > Constants.PLAINFILE_MAX_QUEUEWAITTIMEOUT then
                    let msg = m_Config.MessagesText.GetMessage( "MSG_INVALID_QUEUE_WAIT_TIME", sprintf "%d" Constants.PLAINFILE_MIN_QUEUEWAITTIMEOUT, sprintf "%d" Constants.PLAINFILE_MAX_QUEUEWAITTIMEOUT )
                    raise <| Exception msg
                w

            let writeProtect = m_WriteProtectCombo.SelectedIndex = 0

            let conf : TargetGroupConf.T_PlainFile = {
                IdentNumber = mediaidx_me.fromPrim identNumber;
                MediaName = mediaName;
                FileName = fileName;
                MaxMultiplicity = maxMultiplicity;
                QueueWaitTimeOut = queueWaitTimeOut;
                WriteProtect = writeProtect;
            }

            let newNode = m_ServerStatus.UpdatePlainFileMediaNode mn conf
            this.ShowConfigValue false false
            m_MainWindow.NoticeUpdateConfig newNode

        with
        | _ as x ->
            m_ErrorMessageLabel.Text <- x.Message

    /// <summary>
    ///  "Discard" button was clicked.
    /// </summary>
    member private this.OnClick_DiscardButton() : unit = 
        this.ShowConfigValue false false

    /// <summary>
    ///  Timer event.
    /// </summary>
    member private _.OnTimer() : unit = 
        m_Timer.Stop()
        let mn = m_ServerStatus.GetNode m_NodeID :?> IMediaNode
        let tgn = ( m_ServerStatus.GetAncestorTargetGroup mn ).Value
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice mn ).Value
        let lu = ( m_ServerStatus.GetAncestorLogicalUnit mn ).Value

        m_MainWindow.ProcCtrlQuery false ( fun () -> task {
            // Get the ancestor target device running status
            do! m_MainWindow.UpdateRunningStatus()
            let struct( tdActived, tgLoaded ) =
                m_MainWindow.IsTGLoaded tdn.TargetDeviceID tgn.TargetGroupID

            if tdActived && tgLoaded then
                let! luStat = m_CtrlConnection.GetMediaStatus tdn.TargetDeviceID lu.LUN mn.IdentNumber
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    m_GraphUpdater.Update luStat.ReadBytesCount luStat.WrittenBytesCount luStat.ReadTickCount luStat.WriteTickCount
                    m_Timer.Start()
                ) |> ignore
            else
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    m_GraphUpdater.Clear()
                    m_Timer.Start()
                ) |> ignore
        })

    ///////////////////////////////////////////////////////////////////////////
    // Private method

    /// <summary>
    ///  Set configuration values to the controller.
    /// </summary>
    /// <param name="loaded">
    ///  If the Target Group is loaded, this values is true.
    /// </param>
    /// <param name="editmode">
    ///  If configurations editiong mode is enabled, this value is true.
    /// </param>
    member private _.ShowConfigValue ( loaded : bool ) ( editmode : bool ) : unit =
        let mn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_PlainFileMedia
        m_EditButton.IsEnabled <- ( not editmode ) && ( not loaded )
        m_ApplyButton.IsEnabled <- editmode && ( not loaded )
        m_DiscardButton.IsEnabled <- editmode && ( not loaded )
        m_ErrorMessageLabel.Text <- ""

        m_IdentNumberTextBox.IsEnabled <- editmode && ( not loaded )
        m_IdentNumberTextBox.Text <- sprintf "%d" ( mediaidx_me.toPrim mn.Values.IdentNumber )
        m_MediaNameTextBox.IsEnabled <- editmode && ( not loaded )
        m_MediaNameTextBox.Text <- mn.Values.MediaName
        m_FileNameTextBox.IsEnabled <- editmode && ( not loaded )
        m_FileNameTextBox.Text <- mn.Values.FileName
        m_MaxMultiplicityTextBox.IsEnabled <- editmode && ( not loaded )
        m_MaxMultiplicityTextBox.Text <- sprintf "%d" mn.Values.MaxMultiplicity
        m_QueueWaitTimeOutTextBox.IsEnabled <- editmode && ( not loaded )
        m_QueueWaitTimeOutTextBox.Text <- sprintf "%d" mn.Values.QueueWaitTimeOut
        m_WriteProtectCombo.IsEnabled <- editmode && ( not loaded )
        if mn.Values.WriteProtect then
            m_WriteProtectCombo.SelectedIndex <- 0
        else
            m_WriteProtectCombo.SelectedIndex <- 1


