//=============================================================================
// Haruka Software Storage.
// TargetPropPage.fs : Implement the function to display Target property page in the main window.
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
///  Record type for setting items to display in LU list.
/// </summary>
type LUListItem = {
    /// Connected checkbox. 
    mutable Connected : bool;
    /// LU name column
    LUName : string;
    /// LUN column
    LUN : string;
    /// LU node ID
    NodeID : CONFNODE_T;
}

/// <summary>
///  TargetPropPage class.
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
type TargetPropPage(
    m_Config : GUIConfig,
    m_PropPage : Grid,
    m_MainWindow : IMainWindowIFForPP,
    m_ServerStatus : ServerStatus,
    m_CtrlConnection : CtrlConnection,
    m_NodeID : CONFNODE_T
) as this =


    // Get controll objects in main window.
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
    let m_TargetIDTextBox = m_PropPage.FindName( "TargetIDTextBox" ) :?> TextBox
    let m_TargetPortalGroupTagValueTextBlock = m_PropPage.FindName( "TargetPortalGroupTagValueTextBlock" ) :?> TextBlock
    let m_TargetNameTextBox = m_PropPage.FindName( "TargetNameTextBox" ) :?> TextBox
    let m_GenTargetNameButton = m_PropPage.FindName( "GenTargetNameButton" ) :?> Button
    let m_TargetAliasTextBox = m_PropPage.FindName( "TargetAliasTextBox" ) :?> TextBox
    let m_AuthTypeCombo = m_PropPage.FindName( "AuthTypeCombo" ) :?> ComboBox
    let m_InitiatorAuthUserNameTextBox = m_PropPage.FindName( "InitiatorAuthUserNameTextBox" ) :?> TextBox
    let m_InitiatorAuthPasswordTextBox = m_PropPage.FindName( "InitiatorAuthPasswordTextBox" ) :?> TextBox
    let m_TargetAuthUserNameTextBox = m_PropPage.FindName( "TargetAuthUserNameTextBox" ) :?> TextBox
    let m_TargetAuthPasswordTextBox = m_PropPage.FindName( "TargetAuthPasswordTextBox" ) :?> TextBox
    let m_LogicalUnitListView = m_PropPage.FindName( "LogicalUnitListView" ) :?> ListView

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
        m_Config.SetLocalizedText "TargetPropPage" m_PropPage

        // Set event handler
        m_DestroySessionButton.Click.AddHandler ( fun _ _ -> this.OnClick_DestroySessionButton() )
        m_EditButton.Click.AddHandler ( fun _ _ -> this.OnClick_EditButton() )
        m_ApplyButton.Click.AddHandler ( fun _ _ -> this.OnClick_ApplyButton() )
        m_DiscardButton.Click.AddHandler ( fun _ _ -> this.OnClick_DiscardButton() )
        m_GenTargetNameButton.Click.AddHandler ( fun _ _ -> this.OnClick_GenTargetNameButton() )
        m_AuthTypeCombo.SelectionChanged.AddHandler ( fun _ _ -> this.OnSelectionChanged_AuthTypeCombo() )
        m_Timer.Tick.AddHandler ( fun _ _ -> this.OnTimer() )
        m_SessionsExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetPropPage" m_SessionsExpander.Name true )
        m_SessionsExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetPropPage" m_SessionsExpander.Name false )
        m_ConfigurationExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetPropPage" m_ConfigurationExpander.Name true )
        m_ConfigurationExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "TargetPropPage" m_ConfigurationExpander.Name false )

        // set configuration values
        m_DestroySessionButton.IsEnabled <- false
        m_EditButton.IsEnabled <- false
        m_ApplyButton.IsEnabled <- false
        m_DiscardButton.IsEnabled <- false
        m_GenTargetNameButton.IsEnabled <- false

        m_ReceiveBytesHeightTextBlock.Text <- ""
        m_SendBytesHeightTextBlock.Text <- ""

        m_Timer.Interval <- new TimeSpan( 0, 0, int Constants.RECOUNTER_SPAN_SEC )
        m_Timer.Start()

        // Set default value
        m_SessionsExpander.IsExpanded <- UserOpeStat.GetExpanded "TargetPropPage" m_SessionsExpander.Name
        m_ConfigurationExpander.IsExpanded <- UserOpeStat.GetExpanded "TargetPropPage" m_ConfigurationExpander.Name

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
            let tn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_Target
            let tgn = ( m_ServerStatus.GetAncestorTargetGroup tn ).Value
            let tdn = ( m_ServerStatus.GetAncestorTargetDevice tn ).Value

            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                // Get the ancestor target device running status
                do! m_MainWindow.UpdateRunningStatus()
                let struct( tdActived, tgLoaded ) =
                    m_MainWindow.IsTGLoaded tdn.TargetDeviceID tgn.TargetGroupID

                if tdActived && tgLoaded then
                    let! sessions = m_CtrlConnection.GetSession_InTarget tdn.TargetDeviceID tn.Values.IdentNumber
                    let! connections = m_CtrlConnection.GetConnection_InTarget tdn.TargetDeviceID tn.Values.IdentNumber
                    m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                        this.ShowConfigValue true false
                        m_SessionsCtrlUpdater.InitialShow sessions connections
                        m_MainWindow.SetProgress false
                    ) |> ignore
                else
                    m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                        this.ShowConfigValue false false
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
    ///  "Destory Session" button was clicked.
    /// </summary>
    member private _.OnClick_DestroySessionButton() : unit =
        let tn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_Target
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice tn ).Value

        match m_SessionsCtrlUpdater.GetSelectedItemInfo() with
        | Some( SessionTreeItemType.Session( _, tviTSIH ) ) ->
            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                // Request to drop session to the controller.
                do! m_CtrlConnection.DestructSession tdn.TargetDeviceID tviTSIH

                // get updated session information
                let! sessions = m_CtrlConnection.GetSession_InTarget tdn.TargetDeviceID tn.Values.IdentNumber
                let! connections = m_CtrlConnection.GetConnection_InTarget tdn.TargetDeviceID tn.Values.IdentNumber
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
            let tn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_Target

            let targetID =
                let r, w = UInt32.TryParse m_TargetIDTextBox.Text
                if not r then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_TARGET_ID",
                            "0",
                            sprintf "%d" UInt32.MaxValue
                        )
                    raise <| Exception msg
                else
                    w |> tnodeidx_me.fromPrim

            let targetName = m_TargetNameTextBox.Text
            if Constants.ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_OBJ.IsMatch targetName |> not then
                let msg = m_Config.MessagesText.GetMessage( "MSG_INVALID_TARGET_NAME" )
                raise <| Exception msg

            let targetAlias = m_TargetAliasTextBox.Text
            if targetAlias.Length > Constants.MAX_TARGET_ALIAS_STR_LENGTH then
                let msg =
                    m_Config.MessagesText.GetMessage(
                        "MSG_INVALID_TARGET_ALIAS",
                        sprintf "%d" Constants.MAX_TARGET_ALIAS_STR_LENGTH
                    )
                raise <| Exception msg

            let auth =
                if m_AuthTypeCombo.SelectedIndex = 0 then
                    let initiatorUserName = m_InitiatorAuthUserNameTextBox.Text
                    if Constants.USER_NAME_REGEX_OBJ.IsMatch initiatorUserName |> not || initiatorUserName.Length < 1 then
                        let msg =
                            m_Config.MessagesText.GetMessage(
                                "MSG_INVALID_INITIATOR_USER_NAME",
                                sprintf "%d" Constants.MAX_USER_NAME_STR_LENGTH
                            )
                        raise <| Exception msg

                    let initiatorPassword = m_InitiatorAuthPasswordTextBox.Text
                    if Constants.PASSWORD_REGEX_OBJ.IsMatch initiatorPassword |> not || initiatorPassword.Length < 1 then
                        let msg =
                            m_Config.MessagesText.GetMessage(
                                "MSG_INVALID_INITIATOR_PASSWORD",
                                sprintf "%d" Constants.MAX_PASSWORD_STR_LENGTH
                            )
                        raise <| Exception msg

                    let targetUserName = m_TargetAuthUserNameTextBox.Text
                    if Constants.USER_NAME_REGEX_OBJ.IsMatch targetUserName |> not || targetUserName.Length < 1 then
                        let msg =
                            m_Config.MessagesText.GetMessage(
                                "MSG_INVALID_TARGET_USER_NAME",
                                sprintf "%d" Constants.MAX_USER_NAME_STR_LENGTH
                            )
                        raise <| Exception msg

                    let targetPassword = m_TargetAuthPasswordTextBox.Text
                    if Constants.PASSWORD_REGEX_OBJ.IsMatch targetPassword |> not || targetPassword.Length < 1 then
                        let msg =
                            m_Config.MessagesText.GetMessage(
                                "MSG_INVALID_TARGET_PASSWORD",
                                sprintf "%d" Constants.MAX_PASSWORD_STR_LENGTH
                            )
                        raise <| Exception msg

                    TargetGroupConf.U_CHAP( {
                        InitiatorAuth = {
                            UserName = initiatorUserName;
                            Password = initiatorPassword;
                        }
                        TargetAuth = {
                            UserName = targetUserName;
                            Password = targetPassword;
                        }
                    } )
                else
                    TargetGroupConf.U_None()

            // update target node
            let conf : TargetGroupConf.T_Target = {
                tn.Values with
                    IdentNumber = targetID;
                    TargetName = targetName;
                    TargetAlias = targetAlias;
                    LUN = [];   // ignored
                    Auth = auth;
            }
            let newNode = m_ServerStatus.UpdateTargetNode tn conf

            // connect or disconnect to LU nodes
            for i = 0 to m_LogicalUnitListView.Items.Count - 1 do
                let cb = m_LogicalUnitListView.Items.Item( i ) :?> LUListItem
                let lunode = m_ServerStatus.GetNode cb.NodeID :?> ILUNode
                if cb.Connected then
                    m_ServerStatus.AddTargetLURelation newNode lunode
                else
                    m_ServerStatus.DeleteTargetLURelation newNode lunode

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
    ///  Generate target name button was clicked.
    /// </summary>
    member private _.OnClick_GenTargetNameButton() : unit =
        let oldTN = m_TargetNameTextBox.Text
        let dlg = new EditTargetNameDialog( m_Config, oldTN )
        match dlg.Show() with
        | DialogResult.Ok( x ) ->
            m_TargetNameTextBox.Text <- x
        | _ -> ()

    /// <summary>
    ///  "Enable Authentification" combo box selection was changed.
    /// </summary>
    /// <remarks>
    ///  It considered that the configuration editiong is enabled.
    /// </remarks>
    member private _.OnSelectionChanged_AuthTypeCombo() : unit =
        let f = m_AuthTypeCombo.SelectedIndex = 0
        m_InitiatorAuthUserNameTextBox.IsEnabled <- f
        m_InitiatorAuthPasswordTextBox.IsEnabled <- f
        m_TargetAuthUserNameTextBox.IsEnabled <- f
        m_TargetAuthPasswordTextBox.IsEnabled <- f

    /// <summary>
    ///  Timer event.
    /// </summary>
    member private this.OnTimer() : unit = 
        m_Timer.Stop()
        let tn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_Target
        let tgn = ( m_ServerStatus.GetAncestorTargetGroup tn ).Value
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice tn ).Value

        m_MainWindow.ProcCtrlQuery false ( fun () -> task {
            // Get the ancestor target device status
            do! m_MainWindow.UpdateRunningStatus()
            let struct( tdActived, tgLoaded ) =
                m_MainWindow.IsTGLoaded tdn.TargetDeviceID tgn.TargetGroupID

            let! sessions, connections = task {
                if tdActived && tgLoaded then
                    let! ws = m_CtrlConnection.GetSession_InTarget tdn.TargetDeviceID tn.Values.IdentNumber
                    let! wc = m_CtrlConnection.GetConnection_InTarget tdn.TargetDeviceID tn.Values.IdentNumber
                    return ( Some ws, Some wc )
                else
                    return None, None
            }

            m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                if tdActived && tgLoaded then
                    m_SessionsCtrlUpdater.Update sessions.Value connections.Value
                else
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
        let tn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_Target
        let tgn = ( m_ServerStatus.GetAncestorTargetGroup tn ).Value

        m_EditButton.IsEnabled <- ( not editmode ) && ( not loaded )
        m_ApplyButton.IsEnabled <- editmode && ( not loaded )
        m_DiscardButton.IsEnabled <- editmode && ( not loaded )
        m_ErrorMessageLabel.Text <- ""

        m_TargetIDTextBox.Text <- sprintf "%d" ( tnodeidx_me.toPrim tn.Values.IdentNumber )
        m_TargetIDTextBox.IsEnabled <- editmode && ( not loaded )
        m_TargetPortalGroupTagValueTextBlock.Text <- sprintf "%d" ( tpgt_me.toPrim tn.Values.TargetPortalGroupTag )
        m_TargetNameTextBox.Text <- tn.Values.TargetName
        m_TargetNameTextBox.IsEnabled <- editmode && ( not loaded )
        m_GenTargetNameButton.IsEnabled <- editmode && ( not loaded )
        m_TargetAliasTextBox.Text <- tn.Values.TargetAlias
        m_TargetAliasTextBox.IsEnabled <- editmode && ( not loaded )
        m_AuthTypeCombo.SelectedIndex <-
            match tn.Values.Auth with
            | TargetGroupConf.U_CHAP( x ) ->
                m_InitiatorAuthUserNameTextBox.Text <- x.InitiatorAuth.UserName
                m_InitiatorAuthUserNameTextBox.IsEnabled <- editmode && ( not loaded )
                m_InitiatorAuthPasswordTextBox.Text <- x.InitiatorAuth.Password
                m_InitiatorAuthPasswordTextBox.IsEnabled <- editmode && ( not loaded )
                m_TargetAuthUserNameTextBox.Text <- x.TargetAuth.UserName
                m_TargetAuthUserNameTextBox.IsEnabled <- editmode && ( not loaded )
                m_TargetAuthPasswordTextBox.Text <- x.TargetAuth.Password
                m_TargetAuthPasswordTextBox.IsEnabled <- editmode && ( not loaded )
                0
            | TargetGroupConf.U_None() ->
                m_InitiatorAuthUserNameTextBox.Text <- ""
                m_InitiatorAuthUserNameTextBox.IsEnabled <- false
                m_InitiatorAuthPasswordTextBox.Text <- ""
                m_InitiatorAuthPasswordTextBox.IsEnabled <- false
                m_TargetAuthUserNameTextBox.Text <- ""
                m_TargetAuthUserNameTextBox.IsEnabled <- false
                m_TargetAuthPasswordTextBox.Text <- ""
                m_TargetAuthPasswordTextBox.IsEnabled <- false
                1
        m_AuthTypeCombo.IsEnabled <- editmode && ( not loaded )
        m_LogicalUnitListView.IsEnabled <- editmode && ( not loaded )
        m_LogicalUnitListView.Items.Clear()

        let connectedLUs =
            ( tn :> IConfigureNode ).GetChildNodes<ILUNode>()
            |> Seq.map  _.NodeID
        for ilu in ( tgn :> IConfigureNode ).GetDescendantNodes<ILUNode>() do
            let cb = {
                Connected = ( connectedLUs |> Seq.exists ( (=) ilu.NodeID ) );
                LUName = ilu.LUName;
                LUN = lun_me.toString ilu.LUN;
                NodeID = ilu.NodeID;
            }
            m_LogicalUnitListView.Items.Add cb |> ignore

