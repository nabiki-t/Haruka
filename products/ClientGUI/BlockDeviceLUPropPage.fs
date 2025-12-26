//=============================================================================
// Haruka Software Storage.
// BlockDeviceLUPropPage.fs : Implement the function to display Block Device LU property page in the main window.
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
open System.Threading.Tasks


open Haruka.Constants
open Haruka.IODataTypes
open Haruka.Commons
open Haruka.Client

//=============================================================================
// Class implementation

/// <summary>
///  BlockDeviceLUPropPage class.
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
type BlockDeviceLUPropPage(
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

    let m_StatusExpander = m_PropPage.FindName( "StatusExpander" ) :?> Expander
    let m_LUResetButton = m_PropPage.FindName( "LUResetButton" ) :?> Button
    let m_ACAStatusTextBox = m_PropPage.FindName( "ACAStatusTextBox" ) :?> TextBox
    let m_ITNexusTextBox = m_PropPage.FindName( "ITNexusTextBox" ) :?> TextBox
    let m_StatusCodeTextBox = m_PropPage.FindName( "StatusCodeTextBox" ) :?> TextBox
    let m_SenseKeyTextBox = m_PropPage.FindName( "SenseKeyTextBox" ) :?> TextBox
    let m_AdditionalSenseCodeTextBox = m_PropPage.FindName( "AdditionalSenseCodeTextBox" ) :?> TextBox
    let m_CurrentErrorTextBox = m_PropPage.FindName( "CurrentErrorTextBox" ) :?> TextBox

    let m_ConfigurationExpander = m_PropPage.FindName( "ConfigurationExpander" ) :?> Expander
    let m_EditButton = m_PropPage.FindName( "EditButton" ) :?> Button
    let m_ApplyButton = m_PropPage.FindName( "ApplyButton" ) :?> Button
    let m_DiscardButton = m_PropPage.FindName( "DiscardButton" ) :?> Button
    let m_ErrorMessageLabel = m_PropPage.FindName( "ErrorMessageLabel" ) :?> TextBlock
    let m_LUNTextBox = m_PropPage.FindName( "LUNTextBox" ) :?> TextBox
    let m_LUNameTextBox = m_PropPage.FindName( "LUNameTextBox" ) :?> TextBox
    let m_MaxMultiplicityTextBox = m_PropPage.FindName( "MaxMultiplicityTextBox" ) :?> TextBox

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
        m_Config.SetLocalizedText "BlockDeviceLUPropPage" m_PropPage

        // Set event handler
        m_LUResetButton.Click.AddHandler ( fun _ _ -> this.OnClick_LUResetButton() )
        m_EditButton.Click.AddHandler ( fun _ _ -> this.OnClick_EditButton() )
        m_ApplyButton.Click.AddHandler ( fun _ _ -> this.OnClick_ApplyButton() )
        m_DiscardButton.Click.AddHandler ( fun _ _ -> this.OnClick_DiscardButton() )
        m_Timer.Tick.AddHandler ( fun _ _ -> this.OnTimer() )
        m_UsageExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "BlockDeviceLUPropPage" m_UsageExpander.Name true )
        m_UsageExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "BlockDeviceLUPropPage" m_UsageExpander.Name false )
        m_StatusExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "BlockDeviceLUPropPage" m_StatusExpander.Name true )
        m_StatusExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "BlockDeviceLUPropPage" m_StatusExpander.Name false )
        m_ConfigurationExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "BlockDeviceLUPropPage" m_ConfigurationExpander.Name true )
        m_ConfigurationExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "BlockDeviceLUPropPage" m_ConfigurationExpander.Name false )

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
        m_UsageExpander.IsExpanded <- UserOpeStat.GetExpanded "BlockDeviceLUPropPage" m_UsageExpander.Name
        m_StatusExpander.IsExpanded <- UserOpeStat.GetExpanded "BlockDeviceLUPropPage" m_StatusExpander.Name
        m_ConfigurationExpander.IsExpanded <- UserOpeStat.GetExpanded "BlockDeviceLUPropPage" m_ConfigurationExpander.Name

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
            let bdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_BlockDeviceLU
            let tgn = ( m_ServerStatus.GetAncestorTargetGroup bdn ).Value
            let tdn = ( m_ServerStatus.GetAncestorTargetDevice bdn ).Value

            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                // Get the ancestor target device running status
                do! m_MainWindow.UpdateRunningStatus()
                let struct( tdActived, tgLoaded ) =
                    m_MainWindow.IsTGLoaded tdn.TargetDeviceID tgn.TargetGroupID

                if tdActived && tgLoaded then
                    let! luStat = m_CtrlConnection.GetLUStatus tdn.TargetDeviceID ( bdn :> ILUNode ).LUN

                    m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                        this.ShowConfigValue true false
                        m_LUResetButton.IsEnabled <- true
                        this.ShowACAStatus ( Some luStat )
                        m_GraphUpdater.Update luStat.ReadBytesCount luStat.WrittenBytesCount luStat.ReadTickCount luStat.WriteTickCount
                        m_MainWindow.SetProgress false
                    ) |> ignore
                else
                    m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                        this.ShowConfigValue false false
                        m_LUResetButton.IsEnabled <- false
                        this.ShowACAStatus None
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
    ///  "Reset" button was clicked.
    /// </summary>
    /// <remarks>
    ///  If this button is enabled, it is considered that the target device is activated and the target group is activated or loaded state.
    /// </remarks>
    member private this.OnClick_LUResetButton() : unit =
            let bdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_BlockDeviceLU
            let tdn = ( m_ServerStatus.GetAncestorTargetDevice bdn ).Value

            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                do! m_CtrlConnection.LUReset tdn.TargetDeviceID ( bdn :> ILUNode ).LUN
                let! luStat = m_CtrlConnection.GetLUStatus tdn.TargetDeviceID ( bdn :> ILUNode ).LUN

                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    this.ShowConfigValue true false
                    this.ShowACAStatus ( Some luStat )
                    m_GraphUpdater.Update luStat.ReadBytesCount luStat.WrittenBytesCount luStat.ReadTickCount luStat.WriteTickCount
                    m_MainWindow.SetProgress false
                ) |> ignore
            })

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
            let bdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_BlockDeviceLU

            let lun =
                try
                    lun_me.fromStringValue m_LUNTextBox.Text
                with
                | _ ->
                    let msg = m_Config.MessagesText.GetMessage( "MSG_INVALID_LUN" )
                    raise <| Exception msg

            let luName = m_LUNameTextBox.Text
            if luName.Length > Constants.MAX_LU_NAME_STR_LENGTH then
                let msg = m_Config.MessagesText.GetMessage( "MSG_INVALID_LU_NAME" )
                raise <| Exception msg

            let maxMultiplicity = 
                try
                    UInt32.Parse m_MaxMultiplicityTextBox.Text
                with
                | _ ->
                    let mins = sprintf "%d" Constants.LU_MIN_MULTIPLICITY
                    let maxs = sprintf "%d" Constants.LU_MAX_MULTIPLICITY
                    let msg = m_Config.MessagesText.GetMessage( "MSG_INVALID_LU_MAXMULTIPLICITY", mins, maxs )
                    raise <| Exception msg

            let newNode = m_ServerStatus.UpdateBlockDeviceLUNode bdn lun luName maxMultiplicity
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
    member private this.OnTimer() : unit = 
        m_Timer.Stop()
        let bdn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_BlockDeviceLU
        let tgn = ( m_ServerStatus.GetAncestorTargetGroup bdn ).Value
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice bdn ).Value

        m_MainWindow.ProcCtrlQuery false ( fun () -> task {
            // Get the ancestor target device running status
            do! m_MainWindow.UpdateRunningStatus()
            let struct( tdActived, tgLoaded ) =
                m_MainWindow.IsTGLoaded tdn.TargetDeviceID tgn.TargetGroupID

            if tdActived && tgLoaded then
                let! luStat = m_CtrlConnection.GetLUStatus tdn.TargetDeviceID ( bdn :> ILUNode ).LUN
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    this.ShowACAStatus ( Some luStat )
                    m_LUResetButton.IsEnabled <- true
                    m_GraphUpdater.Update luStat.ReadBytesCount luStat.WrittenBytesCount luStat.ReadTickCount luStat.WriteTickCount
                    m_Timer.Start()
                ) |> ignore
            else
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    this.ShowACAStatus None
                    m_LUResetButton.IsEnabled <- false
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
        let bdn = m_ServerStatus.GetNode m_NodeID :?> ILUNode
        m_EditButton.IsEnabled <- ( not editmode ) && ( not loaded )
        m_ApplyButton.IsEnabled <- editmode && ( not loaded )
        m_DiscardButton.IsEnabled <- editmode && ( not loaded )
        m_ErrorMessageLabel.Text <- ""

        m_LUNTextBox.IsEnabled <- editmode && ( not loaded )
        m_LUNTextBox.Text <- lun_me.toString bdn.LUN
        m_LUNameTextBox.IsEnabled <- editmode && ( not loaded )
        m_LUNameTextBox.Text <- bdn.LUName
        m_MaxMultiplicityTextBox.Text <- sprintf "%d" bdn.MaxMultiplicity

    /// <summary>
    ///  Set ACA status value to the controllers
    /// </summary>
    /// <param name="luStat">
    ///  Loaded LU status. If the LU is not loaded, this value can be None.
    /// </param>
    member private _.ShowACAStatus ( luStat : TargetDeviceCtrlRes.T_LUStatus_Success option ) : unit =
        if luStat.IsNone then
            // The LU is not loaded.
            m_ACAStatusTextBox.Text <- ""
            m_ITNexusTextBox.Text <- ""
            m_StatusCodeTextBox.Text <- ""
            m_SenseKeyTextBox.Text <- ""
            m_AdditionalSenseCodeTextBox.Text <- ""
            m_CurrentErrorTextBox.Text <- ""
        elif luStat.Value.ACAStatus.IsNone then
            // The LU is loaded but ACA is not established.
            m_ACAStatusTextBox.Text <- "None"
            m_ITNexusTextBox.Text <- ""
            m_StatusCodeTextBox.Text <- ""
            m_SenseKeyTextBox.Text <- ""
            m_AdditionalSenseCodeTextBox.Text <- ""
            m_CurrentErrorTextBox.Text <- ""
        else
            // ACA established.
            let x = luStat.Value.ACAStatus.Value
            m_ACAStatusTextBox.Text <- "Established"
            m_ITNexusTextBox.Text <- x.ITNexus.ToString()
            m_StatusCodeTextBox.Text <-
                LanguagePrimitives.EnumOfValue< byte, ScsiCmdStatCd > x.StatusCode 
                |> Constants.getScsiCmdStatNameFromValue
            m_SenseKeyTextBox.Text <-
                LanguagePrimitives.EnumOfValue< byte, SenseKeyCd > x.SenseKey 
                |> Constants.getSenseKeyNameFromValue
            m_AdditionalSenseCodeTextBox.Text <-
                LanguagePrimitives.EnumOfValue< uint16, ASCCd > x.AdditionalSenseCode 
                |> Constants.getAscAndAscqNameFromValue
            m_CurrentErrorTextBox.Text <- ""

