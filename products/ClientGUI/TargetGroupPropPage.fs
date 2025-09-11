//=============================================================================
// Haruka Software Storage.
// TargetGroupPropPage.fs : Implement the function to display Target Group property page in the main window.
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
///  TargetGroupPropPage class.
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
type TargetGroupPropPage(
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
    let m_ActivateButton = m_PropPage.FindName( "ActivateButton" ) :?> Button
    let m_LoadButton = m_PropPage.FindName( "LoadButton" ) :?> Button
    let m_InactivateButton = m_PropPage.FindName( "InactivateButton" ) :?> Button
    let m_UnloadButton = m_PropPage.FindName( "UnloadButton" ) :?> Button

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
    let m_TargetGroupIDTextBox = m_PropPage.FindName( "TargetGroupIDTextBox" ) :?> TextBox
    let m_TargetGroupNameTextBox = m_PropPage.FindName( "TargetGroupNameTextBox" ) :?> TextBox
    let m_EnableAtStartCombo = m_PropPage.FindName( "EnableAtStartCombo" ) :?> ComboBox

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

    // Initialize procedure for the property page.
    do
        // Set controller localized text
        m_Config.SetLocalizedText "TargetGroupPropPage" m_PropPage

        // Set event handler
        m_ActivateButton.Click.AddHandler ( fun _ _ -> this.OnClick_ActivateButton() )
        m_LoadButton.Click.AddHandler ( fun _ _ -> this.OnClick_LoadButton() )
        m_InactivateButton.Click.AddHandler ( fun _ _ -> this.OnClick_InactivateButton() )
        m_UnloadButton.Click.AddHandler ( fun _ _ -> this.OnClick_UnloadButton() )
        m_DestroySessionButton.Click.AddHandler ( fun _ _ -> this.OnClick_DestroySessionButton() )
        m_EditButton.Click.AddHandler ( fun _ _ -> this.OnClick_EditButton() )
        m_ApplyButton.Click.AddHandler ( fun _ _ -> this.OnClick_ApplyButton() )
        m_DiscardButton.Click.AddHandler ( fun _ _ -> this.OnClick_DiscardButton() )
        m_Timer.Tick.AddHandler ( fun _ _ -> this.OnTimer() )
        m_StatusExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetGroupPropPage" m_StatusExpander.Name true )
        m_StatusExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetGroupPropPage" m_StatusExpander.Name false )
        m_SessionsExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetGroupPropPage" m_SessionsExpander.Name true )
        m_SessionsExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetGroupPropPage" m_SessionsExpander.Name false )
        m_ConfigurationExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetGroupPropPage" m_ConfigurationExpander.Name true )
        m_ConfigurationExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetGroupPropPage" m_ConfigurationExpander.Name false )

        m_ActivateButton.IsEnabled <- false
        m_LoadButton.IsEnabled <- false
        m_InactivateButton.IsEnabled <- false
        m_UnloadButton.IsEnabled <- false
        m_DestroySessionButton.IsEnabled <- false
        m_EditButton.IsEnabled <- false
        m_ApplyButton.IsEnabled <- false
        m_DiscardButton.IsEnabled <- false

        m_ReceiveBytesHeightTextBlock.Text <- ""
        m_SendBytesHeightTextBlock.Text <- ""

        m_Timer.Interval <- new TimeSpan( 0, 0, int Constants.RECOUNTER_SPAN_SEC )
        m_Timer.Start()

        // Set default value
        m_StatusExpander.IsExpanded <- UserOpeStat.GetExpanded "TargetGroupPropPage" m_StatusExpander.Name
        m_SessionsExpander.IsExpanded <- UserOpeStat.GetExpanded "TargetGroupPropPage" m_SessionsExpander.Name
        m_ConfigurationExpander.IsExpanded <- UserOpeStat.GetExpanded "TargetGroupPropPage" m_ConfigurationExpander.Name

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
            let tgn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetGroup
            let tdn = ( m_ServerStatus.GetAncestorTargetDevice tgn ).Value

            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                // Get the ancestor target device status
                do! m_MainWindow.UpdateRunningStatus()

                // Get target groups status
                let struct( tdActived, tgLoaded ) =
                    m_MainWindow.IsTGLoaded tdn.TargetDeviceID tgn.TargetGroupID
                let tgActived =
                    let activeTG = m_MainWindow.GetActivatedTGIDs()
                    match activeTG.TryGetValue tdn.TargetDeviceID with
                    | true, v ->
                        Seq.exists ( (=) tgn.TargetGroupID ) v
                    | false, _ ->
                        false
                let isModified = ( tgn :> IConfigFileNode ).Modified = ModifiedStatus.Modified

                if tdActived && tgLoaded then
                    let! sessions = m_CtrlConnection.GetSession_InTargetGroup tdn.TargetDeviceID tgn.TargetGroupID
                    let! connections = m_CtrlConnection.GetConnection_InTargetGroup tdn.TargetDeviceID tgn.TargetGroupID
                    m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                        this.ShowConfigValue ( tdActived && tgLoaded ) false
                        this.ShowTargetGroupStatus tdActived tgActived tgLoaded isModified
                        m_SessionsCtrlUpdater.InitialShow sessions connections
                        m_MainWindow.SetProgress false
                    ) |> ignore
                else
                    m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                        this.ShowConfigValue ( tdActived && tgLoaded ) false
                        this.ShowTargetGroupStatus tdActived tgActived tgLoaded isModified
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
        override _.OnClosePage() =
            // Stop timer
            m_Timer.Stop()

        // Get current node ID
        override _.GetNodeID() =
            m_NodeID

    ///////////////////////////////////////////////////////////////////////////
    // Event handler

    /// <summary>
    ///  "Activate" button was clicked.
    /// </summary>
    /// <remarks>
    ///  If this button is enabled, it is considered that the target device was loaded and the target group is unloaded or loaded state.
    /// </remarks>
    member private this.OnClick_ActivateButton() : unit =
        let tgn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetGroup
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice tgn ).Value
        this.ChangeTargetGroupState
            tdn tgn true true
            ( fun () -> task {
                // If the target group is not loaded, load this target group.
                let! loadedTGs = m_CtrlConnection.GetLoadedTargetGroups tdn.TargetDeviceID
                let loaded =
                    loadedTGs
                    |> Seq.map _.ID
                    |> Seq.exists ( (=) tgn.TargetGroupID ) 
                if not loaded then
                    do! m_CtrlConnection.LoadTargetGroup tdn.TargetDeviceID tgn.TargetGroupID

                // activate the target group
                do! m_CtrlConnection.ActivateTargetGroup tdn.TargetDeviceID tgn.TargetGroupID
            })

    /// <summary>
    ///  "Load" button was clicked.
    /// </summary>
    /// <remarks>
    ///  If this button is enabled, it is considered that the target device was loaded and the target group is unloaded state.
    /// </remarks>
    member private this.OnClick_LoadButton() : unit = 
        let tgn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetGroup
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice tgn ).Value
        this.ChangeTargetGroupState
            tdn tgn false true
            ( fun () -> m_CtrlConnection.LoadTargetGroup tdn.TargetDeviceID tgn.TargetGroupID )

    /// <summary>
    ///  "Inactivate" button was clicked.
    /// </summary>
    /// <remarks>
    ///  If this button is enabled, it is considered that the target device was loaded and the target group is active state.
    /// </remarks>
    member private this.OnClick_InactivateButton() : unit = 
        let tgn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetGroup
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice tgn ).Value
        this.ChangeTargetGroupState
            tdn tgn false true
            ( fun () -> m_CtrlConnection.InactivateTargetGroup tdn.TargetDeviceID tgn.TargetGroupID )

    /// <summary>
    ///  "Unload" button was clicked.
    /// </summary>
    /// <remarks>
    ///  If this button is enabled, it is considered that the target device was loaded and the target group is active or loaded state.
    /// </remarks>
    member private this.OnClick_UnloadButton() : unit =
        let tgn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetGroup
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice tgn ).Value
        this.ChangeTargetGroupState
            tdn tgn false false
            ( fun () ->task {
                // If the target group is activated, inactivate this target group.
                let! activeTGs = m_CtrlConnection.GetActiveTargetGroups tdn.TargetDeviceID
                let actived =
                    activeTGs
                    |> Seq.map _.ID
                    |> Seq.exists ( (=) tgn.TargetGroupID ) 
                if actived then
                    do! m_CtrlConnection.InactivateTargetGroup tdn.TargetDeviceID tgn.TargetGroupID

                // Unload the target group
                do! m_CtrlConnection.UnloadTargetGroup tdn.TargetDeviceID tgn.TargetGroupID 
            }) 

    member private this.ChangeTargetGroupState ( tdn : ConfNode_TargetDevice ) ( tgn : ConfNode_TargetGroup ) ( tgActived : bool ) ( tgLoaded : bool ) ( f : unit -> Task ) : unit = 
        m_MainWindow.ProcCtrlQuery true ( fun () -> task {
            // update target status
            do! f()
            // get current status
            let! sessions = m_CtrlConnection.GetSession_InTargetDevice tdn.TargetDeviceID
            let! connections = m_CtrlConnection.GetConnection_InTargetDevice tdn.TargetDeviceID
            // update status cache
            do! m_MainWindow.UpdateRunningStatus()
            m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                this.ShowConfigValue tgLoaded false
                this.ShowTargetGroupStatus true tgActived tgLoaded false
                m_SessionsCtrlUpdater.InitialShow sessions connections
                m_MainWindow.NoticeUpdateStat tgn
                m_MainWindow.SetProgress false
            ) |> ignore
        })

    /// <summary>
    ///  "Destory Session" button was clicked.
    /// </summary>
    member private _.OnClick_DestroySessionButton() : unit = 
        let tgn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetGroup
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice tgn ).Value

        match m_SessionsCtrlUpdater.GetSelectedItemInfo() with
        | Some( SessionTreeItemType.Session( _, tviTSIH ) ) ->
            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                // Request to drop session to the controller.
                do! m_CtrlConnection.DestructSession tdn.TargetDeviceID tviTSIH

                // get updated session information
                let! sessions = m_CtrlConnection.GetSession_InTargetGroup tdn.TargetDeviceID tgn.TargetGroupID
                let! connections = m_CtrlConnection.GetConnection_InTargetGroup tdn.TargetDeviceID tgn.TargetGroupID
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    m_SessionsCtrlUpdater.Update sessions connections
                    m_MainWindow.SetProgress false
                ) |> ignore
            } )

        | _ ->
            // If selected tree view item is not session, thie pattern is unexpected and ignore this event.
            ()

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
            let tgn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetGroup

            let targetGroupID =
                let r, w = UInt32.TryParse m_TargetGroupIDTextBox.Text
                if not r then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_TARGET_GROUP_ID",
                            "0",
                            sprintf "%d" UInt32.MaxValue
                        )
                    raise <| Exception msg
                else
                    tgid_me.fromPrim w

            let targetGroupName = m_TargetGroupNameTextBox.Text
            if targetGroupName.Length > Constants.MAX_TARGET_GROUP_NAME_STR_LENGTH then
                let msg =
                    m_Config.MessagesText.GetMessage(
                        "MSG_INVALID_TARGET_GROUP_NAME",
                        sprintf "%d" Constants.MAX_TARGET_GROUP_NAME_STR_LENGTH
                    )
                raise <| Exception msg
            let enableAtStart = m_EnableAtStartCombo.SelectedIndex = 0

            let newNode = m_ServerStatus.UpdateTargetGroupNode tgn targetGroupID targetGroupName enableAtStart
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
        let tgn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetGroup
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice tgn ).Value

        m_MainWindow.ProcCtrlQuery false ( fun () -> task {
            // Get the ancestor target device running status
            do! m_MainWindow.UpdateRunningStatus()

            // Get the target groups status
            let struct( tdActived, tgLoaded ) =
                m_MainWindow.IsTGLoaded tdn.TargetDeviceID tgn.TargetGroupID
            let tgActived =
                let activeTG = m_MainWindow.GetActivatedTGIDs()
                match activeTG.TryGetValue tdn.TargetDeviceID with
                | true, v ->
                    Seq.exists ( (=) tgn.TargetGroupID ) v
                | false, _ ->
                    false
            let isModified = ( tgn :> IConfigFileNode ).Modified = ModifiedStatus.Modified

            if tdActived && tgLoaded then
                let! sessions = m_CtrlConnection.GetSession_InTargetGroup tdn.TargetDeviceID tgn.TargetGroupID
                let! connections = m_CtrlConnection.GetConnection_InTargetGroup tdn.TargetDeviceID tgn.TargetGroupID
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    this.ShowTargetGroupStatus tdActived tgActived tgLoaded isModified
                    m_SessionsCtrlUpdater.Update sessions connections
                    m_Timer.Start()
                ) |> ignore
            else
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    this.ShowTargetGroupStatus tdActived tgActived tgLoaded isModified
                    m_SessionsCtrlUpdater.Clear()
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
        let tgn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_TargetGroup

        m_EditButton.IsEnabled <- ( not editmode ) && ( not loaded )
        m_ApplyButton.IsEnabled <- editmode && ( not loaded )
        m_DiscardButton.IsEnabled <- editmode && ( not loaded )
        m_ErrorMessageLabel.Text <- ""
        m_TargetGroupIDTextBox.Text <- sprintf "%d" ( tgid_me.toPrim tgn.TargetGroupID )
        m_TargetGroupIDTextBox.IsEnabled <- editmode && ( not loaded )
        m_TargetGroupNameTextBox.Text <- tgn.TargetGroupName
        m_TargetGroupNameTextBox.IsEnabled <- editmode && ( not loaded )
        m_EnableAtStartCombo.SelectedIndex <- if tgn.EnabledAtStart then 0 else 1
        m_EnableAtStartCombo.IsEnabled <- editmode && ( not loaded )


    member private _.ShowTargetGroupStatus ( tdActived : bool ) ( tgActived : bool ) ( tgLoaded : bool ) ( isModified : bool ) : unit =

        if isModified then
            // Target Group configuration is modified
            m_CurrentStatusTextBlock.Text <- "Modified"
            m_ActivateButton.IsEnabled <- false
            m_LoadButton.IsEnabled <- false
            m_InactivateButton.IsEnabled <- false
            m_UnloadButton.IsEnabled <- false
            let img = m_Config.Icons.Get IconImageIndex.III_TARGET_GROUP_MODIFIED
            if img.IsSome && not ( Object.ReferenceEquals( m_CurrentStatusImage.Source, img ) ) then
                m_CurrentStatusImage.Source <- img.Value

        elif not tdActived then
            // Target Device is lot loaded
            m_CurrentStatusTextBlock.Text <- "Target Device Unloaded"
            m_ActivateButton.IsEnabled <- false
            m_LoadButton.IsEnabled <- false
            m_InactivateButton.IsEnabled <- false
            m_UnloadButton.IsEnabled <- false
            let img = m_Config.Icons.Get IconImageIndex.III_TARGET_GROUP_UNLOADED
            if img.IsSome && not ( Object.ReferenceEquals( m_CurrentStatusImage.Source, img ) ) then
                m_CurrentStatusImage.Source <- img.Value

        elif not tgLoaded then
            // Target Group is unloaded
            m_CurrentStatusTextBlock.Text <- "Unloaded"
            m_ActivateButton.IsEnabled <- true
            m_LoadButton.IsEnabled <- true
            m_InactivateButton.IsEnabled <- false
            m_UnloadButton.IsEnabled <- false
            let img = m_Config.Icons.Get IconImageIndex.III_TARGET_GROUP_UNLOADED
            if img.IsSome && not ( Object.ReferenceEquals( m_CurrentStatusImage.Source, img ) ) then
                m_CurrentStatusImage.Source <- img.Value

        elif not tgActived then
            // Target Group is loaded but not activated
            m_CurrentStatusTextBlock.Text <- "Loaded"
            m_ActivateButton.IsEnabled <- true
            m_LoadButton.IsEnabled <- false
            m_InactivateButton.IsEnabled <- false
            m_UnloadButton.IsEnabled <- true
            let img = m_Config.Icons.Get IconImageIndex.III_TARGET_GROUP_LOADED
            if img.IsSome && not ( Object.ReferenceEquals( m_CurrentStatusImage.Source, img ) ) then
                m_CurrentStatusImage.Source <- img.Value

        else
            // Target Group is activated
            m_CurrentStatusTextBlock.Text <- "Active"
            m_ActivateButton.IsEnabled <- false
            m_LoadButton.IsEnabled <- false
            m_InactivateButton.IsEnabled <- true
            m_UnloadButton.IsEnabled <- true
            let img = m_Config.Icons.Get IconImageIndex.III_TARGET_GROUP_ACTIVE
            if img.IsSome && not ( Object.ReferenceEquals( m_CurrentStatusImage.Source, img ) ) then
                m_CurrentStatusImage.Source <- img.Value

