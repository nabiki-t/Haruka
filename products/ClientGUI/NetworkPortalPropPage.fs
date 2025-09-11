//=============================================================================
// Haruka Software Storage.
// NetworkPortalPropPage.fs : Implement the function to display Network Portal property page in the main window.
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
///  NetworkPortalPropPage class.
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
type NetworkPortalPropPage(
    m_Config : GUIConfig,
    m_PropPage : Grid,
    m_MainWindow : IMainWindowIFForPP,
    m_ServerStatus : ServerStatus,
    m_CtrlConnection : CtrlConnection,
    m_NodeID : CONFNODE_T
) as this =

    // Get controll objects in main window.
    let m_ConnectionExpander = m_PropPage.FindName( "ConnectionExpander" ) :?> Expander
    let m_ConnectionList = m_PropPage.FindName( "ConnectionList" ) :?> ListView
    let m_ConnectionParametersListView = m_PropPage.FindName( "ConnectionParametersListView" ) :?> ListView
    let m_ReceiveBytesPerConnectionHeightTextBlock = m_PropPage.FindName( "ReceiveBytesPerConnectionHeightTextBlock" ) :?> TextBlock
    let m_ReceiveBytesPerConnectionCanvas = m_PropPage.FindName( "ReceiveBytesPerConnectionCanvas" ) :?> Canvas
    let m_SendBytesPerConnectionHeightTextBlock = m_PropPage.FindName( "SendBytesPerConnectionHeightTextBlock" ) :?> TextBlock
    let m_SendBytesPerConnectionCanvas = m_PropPage.FindName( "SendBytesPerConnectionCanvas" ) :?> Canvas

    let m_TotalUsageExpander = m_PropPage.FindName( "TotalUsageExpander" ) :?> Expander
    let m_TotalReceiveBytesHeightTextBlock = m_PropPage.FindName( "TotalReceiveBytesHeightTextBlock" ) :?> TextBlock
    let m_TotalReceiveBytesCanvas = m_PropPage.FindName( "TotalReceiveBytesCanvas" ) :?> Canvas
    let m_TotalSendBytesHeightTextBlock = m_PropPage.FindName( "TotalSendBytesHeightTextBlock" ) :?> TextBlock
    let m_TotalSendBytesCanvas = m_PropPage.FindName( "TotalSendBytesCanvas" ) :?> Canvas

    let m_ConfigurationExpander = m_PropPage.FindName( "ConfigurationExpander" ) :?> Expander
    let m_EditButton = m_PropPage.FindName( "EditButton" ) :?> Button
    let m_ApplyButton = m_PropPage.FindName( "ApplyButton" ) :?> Button
    let m_DiscardButton = m_PropPage.FindName( "DiscardButton" ) :?> Button
    let m_ErrorMessageLabel = m_PropPage.FindName( "ErrorMessageLabel" ) :?> TextBlock
    let m_NetworkPortalIDTextBox = m_PropPage.FindName( "NetworkPortalIDTextBox" ) :?> TextBox
    let m_TargetPortalGroupTagValueTextBlock = m_PropPage.FindName( "TargetPortalGroupTagValueTextBlock" ) :?> TextBlock
    let m_TargetAddressTextBox = m_PropPage.FindName( "TargetAddressTextBox" ) :?> TextBox
    let m_PortNumberTextBox = m_PropPage.FindName( "PortNumberTextBox" ) :?> TextBox
    let m_DisableNagleCombo = m_PropPage.FindName( "DisableNagleCombo" ) :?> ComboBox
    let m_ReceiveBufferSizeTextBox = m_PropPage.FindName( "ReceiveBufferSizeTextBox" ) :?> TextBox
    let m_SendBufferSizeTextBox = m_PropPage.FindName( "SendBufferSizeTextBox" ) :?> TextBox
    let m_IPWhiteListListBox = m_PropPage.FindName( "IPWhiteListListBox" ) :?> ListBox
    let m_AddIPWhiteListButton = m_PropPage.FindName( "AddIPWhiteListButton" ) :?> Button
    let m_RemoveIPWhiteListButton = m_PropPage.FindName( "RemoveIPWhiteListButton" ) :?> Button

    // Graph Writer object
    let m_ReceiveBytesPerConnectionGraphWriter =
        new GraphWriter( m_ReceiveBytesPerConnectionCanvas, GraphColor.GC_RED, GuiConst.USAGE_GRAPH_PNT_CNT )
    let m_SendBytesPerConnectionGraphWriter =
        new GraphWriter( m_SendBytesPerConnectionCanvas, GraphColor.GC_GREEN, GuiConst.USAGE_GRAPH_PNT_CNT )
    let m_TotalReceiveBytesGraphWriter =
        new GraphWriter( m_TotalReceiveBytesCanvas, GraphColor.GC_CYAN, GuiConst.USAGE_GRAPH_PNT_CNT )
    let m_TotalSendBytesGraphWriter =
        new GraphWriter( m_TotalSendBytesCanvas, GraphColor.GC_BLUE, GuiConst.USAGE_GRAPH_PNT_CNT )

    // timer object
    let m_Timer = new DispatcherTimer()

    // Initialize procedure for he property page.
    do
        // Set controller localized text
        m_Config.SetLocalizedText "NetworkPortalPropPage" m_PropPage

        // Set event handler
        m_EditButton.Click.AddHandler ( fun _ _ -> this.OnClick_EditButton() )
        m_ApplyButton.Click.AddHandler ( fun _ _ -> this.OnClick_ApplyButton() )
        m_DiscardButton.Click.AddHandler ( fun _ _ -> this.OnClick_DiscardButton() )
        m_Timer.Tick.AddHandler ( fun _ _ -> this.OnTimer() )
        m_ConnectionExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "NetworkPortalPropPage" m_ConnectionExpander.Name true )
        m_ConnectionExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "NetworkPortalPropPage" m_ConnectionExpander.Name false )
        m_TotalUsageExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "NetworkPortalPropPage" m_TotalUsageExpander.Name true )
        m_TotalUsageExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "NetworkPortalPropPage" m_TotalUsageExpander.Name false )
        m_ConfigurationExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "NetworkPortalPropPage" m_ConfigurationExpander.Name true )
        m_ConfigurationExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "NetworkPortalPropPage" m_ConfigurationExpander.Name false )

        m_Timer.Interval <- new TimeSpan( 0, 0, int Constants.RECOUNTER_SPAN_SEC )
        m_Timer.Start()

        // Set default value
        m_ConnectionExpander.IsExpanded <- UserOpeStat.GetExpanded "NetworkPortalPropPage" m_ConnectionExpander.Name
        m_TotalUsageExpander.IsExpanded <- UserOpeStat.GetExpanded "NetworkPortalPropPage" m_TotalUsageExpander.Name
        m_ConfigurationExpander.IsExpanded <- UserOpeStat.GetExpanded "NetworkPortalPropPage" m_ConfigurationExpander.Name


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
            let npn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_NetworkPortal
            let tdn = ( m_ServerStatus.GetAncestorTargetDevice npn ).Value

            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                // Get the ancestor target device is running or not
                do! m_MainWindow.UpdateRunningStatus()
                let activeTDID = m_MainWindow.GetRunningTDIDs()
                let actived = Seq.exists ( (=) tdn.TargetDeviceID ) activeTDID

                let! connections = task {
                    if actived then
                        let! wc = m_CtrlConnection.GetConnection_InNetworkPortal tdn.TargetDeviceID npn.NetworkPortal.IdentNumber
                        return Some wc
                    else
                        return None
                }
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    this.ShowConfigValue actived false
                    m_EditButton.IsEnabled <- not actived
                    if actived then
                        this.UpdateConnectionList connections.Value
                        if m_ConnectionList.Items.Count > 0 then
                            ( m_ConnectionList.Items.Item( 0 ) :?> ListViewItem ).IsSelected <- true
                    else
                        m_ConnectionList.Items.Clear()
                        m_ConnectionParametersListView.Items.Clear()
                        m_ReceiveBytesPerConnectionHeightTextBlock.Text <- ""
                        m_ReceiveBytesPerConnectionGraphWriter.SetValue Array.empty 1.0
                        m_SendBytesPerConnectionHeightTextBlock.Text <- ""
                        m_SendBytesPerConnectionGraphWriter.SetValue Array.empty 1.0
                        m_TotalReceiveBytesHeightTextBlock.Text <- ""
                        m_TotalReceiveBytesGraphWriter.SetValue Array.empty 1.0
                        m_TotalSendBytesHeightTextBlock.Text <- ""
                        m_TotalSendBytesGraphWriter.SetValue Array.empty 1.0
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
            m_Timer.Stop()  // Stop timer

        // Get current node ID
        override _.GetNodeID() =
            m_NodeID

    ///////////////////////////////////////////////////////////////////////////
    // Event handler

    /// <summary>
    ///  "Edit" button was clicked.
    /// </summary>
    /// <remarks>
    ///  If "Edit" button is enabled, it considered that the target device to which this network poral belongs is unloaded.
    /// </remarks>
    member this.OnClick_EditButton() =
        this.ShowConfigValue false true

    /// <summary>
    ///  "Apply" button was clicked.
    /// </summary>
    member this.OnClick_ApplyButton() =
        try
            let npn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_NetworkPortal

            let networkPortalID =
                let r, w = UInt32.TryParse m_NetworkPortalIDTextBox.Text
                if not r then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_NetworkPortalID",
                            sprintf "%d" UInt32.MinValue,
                            sprintf "%d" UInt32.MaxValue
                        )
                    raise <| Exception msg
                else
                    netportidx_me.fromPrim w

            let targetAddress = m_TargetAddressTextBox.Text
            if targetAddress.Length > Constants.MAX_TARGET_ADDRESS_STR_LENGTH then
                let msg =
                    m_Config.MessagesText.GetMessage(
                        "MSG_INVALID_TargetAddress",
                        sprintf "%d" Constants.MAX_TARGET_ADDRESS_STR_LENGTH
                    )
                raise <| Exception msg

            let portNumber =
                let r, w = UInt16.TryParse m_PortNumberTextBox.Text
                if not r || w = 0us then
                    raise <| Exception( m_Config.MessagesText.GetMessage( "MSG_INVALID_PORTNO" ) )
                w

            let disableNagle =
                match m_DisableNagleCombo.SelectedIndex with
                | 0 -> true
                | 1 -> false
                | _ -> true

            let receiveBufferSize =
                let r, w = Int32.TryParse m_ReceiveBufferSizeTextBox.Text
                if not r || w < 0 then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_ReceiveBufferSize",
                            "0",
                            sprintf "%d" Int32.MaxValue
                        )
                    raise <| Exception msg
                else
                    w

            let sendBufferSize =
                let r, w = Int32.TryParse m_SendBufferSizeTextBox.Text
                if not r || w < 0 then
                    let msg =
                        m_Config.MessagesText.GetMessage(
                            "MSG_INVALID_SendBufferSize",
                            "0",
                            sprintf "%d" Int32.MaxValue
                        )
                    raise <| Exception msg
                else
                    w

            let ipWhiteList =
                seq {
                    let cnt = m_IPWhiteListListBox.Items.Count
                    if cnt > 0 then
                        for i = 0 to cnt - 1 do
                            yield m_IPWhiteListListBox.Items.Item(i) |> string |> IPCondition.Parse
                }
                |> Seq.toList

            let newConf : TargetDeviceConf.T_NetworkPortal = {
                IdentNumber = networkPortalID;
                TargetPortalGroupTag = npn.NetworkPortal.TargetPortalGroupTag;
                TargetAddress = targetAddress;
                PortNumber = portNumber;
                DisableNagle = disableNagle;
                ReceiveBufferSize = receiveBufferSize;
                SendBufferSize = sendBufferSize;
                WhiteList = ipWhiteList;
            }

            let newNode = m_ServerStatus.UpdateNetworkPortalNode npn newConf
            this.ShowConfigValue false false
            m_MainWindow.NoticeUpdateConfig newNode
        with
        | _ as x ->
            m_ErrorMessageLabel.Text <- x.Message

    /// <summary>
    ///  "Discard" button was clicked.
    /// </summary>
    member this.OnClick_DiscardButton() =
        this.ShowConfigValue false false

    /// <summary>
    ///  Timer event
    /// </summary>
    member this.OnTimer() =
        m_Timer.Stop()
        let npn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_NetworkPortal
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice npn ).Value

        m_MainWindow.ProcCtrlQuery false ( fun () -> task {
            // Get the ancestor target device running status
            do! m_MainWindow.UpdateRunningStatus()
            let activeTDID = m_MainWindow.GetRunningTDIDs()
            let actived = Seq.exists ( (=) tdn.TargetDeviceID ) activeTDID

            let! connections = task {
                if actived then
                    let! wc = m_CtrlConnection.GetConnection_InNetworkPortal tdn.TargetDeviceID npn.NetworkPortal.IdentNumber
                    return Some wc
                else
                    return None
            }

            m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                if actived then
                    this.UpdateConnectionList connections.Value
                else
                    m_ConnectionList.Items.Clear()
                    m_ConnectionParametersListView.Items.Clear()
                    m_ReceiveBytesPerConnectionHeightTextBlock.Text <- ""
                    m_ReceiveBytesPerConnectionGraphWriter.SetValue Array.empty 1.0
                    m_SendBytesPerConnectionHeightTextBlock.Text <- ""
                    m_SendBytesPerConnectionGraphWriter.SetValue Array.empty 1.0
                    m_TotalReceiveBytesHeightTextBlock.Text <- ""
                    m_TotalReceiveBytesGraphWriter.SetValue Array.empty 1.0
                    m_TotalSendBytesHeightTextBlock.Text <- ""
                    m_TotalSendBytesGraphWriter.SetValue Array.empty 1.0
                m_Timer.Start()
            ) |> ignore
        })

    ///////////////////////////////////////////////////////////////////////////
    // Private method

    /// <summary>
    ///  Set configuration values to the controller.
    /// </summary>
    /// <param name="runningmode">
    ///  If the target device to which this network portal belongs is running, this value is true.
    /// </param>
    /// <param name="editmode">
    ///  If configurations editiong mode is enabled, this value is true.
    /// </param>
    member private _.ShowConfigValue ( runningmode : bool ) ( editmode : bool ) : unit =
        let npn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_NetworkPortal

        m_EditButton.IsEnabled <- ( not editmode ) && ( not runningmode )
        m_ApplyButton.IsEnabled <- editmode && ( not runningmode )
        m_DiscardButton.IsEnabled <- editmode && ( not runningmode )
        m_ErrorMessageLabel.Text <- ""
        m_NetworkPortalIDTextBox.Text <- sprintf "%d" ( netportidx_me.toPrim npn.NetworkPortal.IdentNumber )
        m_NetworkPortalIDTextBox.IsEnabled <- editmode && ( not runningmode )
        m_TargetPortalGroupTagValueTextBlock.Text <- sprintf "%d" ( tpgt_me.toPrim npn.NetworkPortal.TargetPortalGroupTag )
        m_TargetAddressTextBox.Text <- npn.NetworkPortal.TargetAddress
        m_TargetAddressTextBox.IsEnabled <- editmode && ( not runningmode )
        m_PortNumberTextBox.Text <- sprintf "%d" ( npn.NetworkPortal.PortNumber )
        m_PortNumberTextBox.IsEnabled <- editmode && ( not runningmode )
        m_DisableNagleCombo.SelectedIndex <- if npn.NetworkPortal.DisableNagle then 0 else 1
        m_DisableNagleCombo.IsEnabled <- editmode && ( not runningmode )
        m_ReceiveBufferSizeTextBox.Text <- sprintf "%d" ( npn.NetworkPortal.ReceiveBufferSize )
        m_ReceiveBufferSizeTextBox.IsEnabled <- editmode && ( not runningmode )
        m_SendBufferSizeTextBox.Text <- sprintf "%d" ( npn.NetworkPortal.SendBufferSize )
        m_SendBufferSizeTextBox.IsEnabled <- editmode && ( not runningmode )

        m_IPWhiteListListBox.Items.Clear()
        for itr in npn.NetworkPortal.WhiteList do
            m_IPWhiteListListBox.Items.Add( IPCondition.ToString itr ) |> ignore
        m_AddIPWhiteListButton.IsEnabled <- editmode && ( not runningmode ) && ( m_IPWhiteListListBox.Items.Count < Constants.MAX_IP_WHITELIST_COUNT )
        m_RemoveIPWhiteListButton.IsEnabled <- editmode && ( not runningmode ) && ( m_IPWhiteListListBox.Items.Count > 0 )

    member private this.UpdateConnectionList ( conn : TargetDeviceCtrlRes.T_Connection list ) : unit =

        // Normalize total usage data
        let totalReceiveUsage =
            conn
            |> Seq.map _.ReceiveBytesCount
            |> m_TotalReceiveBytesGraphWriter.NormalizeValue
        let totalSentUsage =
            conn
            |> Seq.map _.SentBytesCount
            |> m_TotalSendBytesGraphWriter.NormalizeValue

        // calc total usage graph scale
        let totalSace, totalScaleLabel =
            seq{ totalReceiveUsage; totalSentUsage }
            |> Seq.concat
            |> GraphWriter.CalcScale_BytesPerSec 

        // update total usage graph
        m_TotalReceiveBytesHeightTextBlock.Text <- totalScaleLabel
        m_TotalReceiveBytesGraphWriter.SetValue totalReceiveUsage totalSace
        m_TotalReceiveBytesGraphWriter.UpdateGraph()
        m_TotalSendBytesHeightTextBlock.Text <- totalScaleLabel
        m_TotalSendBytesGraphWriter.SetValue totalSentUsage totalSace
        m_TotalSendBytesGraphWriter.UpdateGraph()

        conn
        |> Seq.sortWith ( fun c d -> concnt_me.Compare c.ConnectionID c.ConnectionCount d.ConnectionID d.ConnectionCount )
        |> Seq.fold ( fun ( idx : int ) ( itr : TargetDeviceCtrlRes.T_Connection ) ->

            // Normalize connection usage data
            let connReceiveUsage =
                m_ReceiveBytesPerConnectionGraphWriter.NormalizeValue [| itr.ReceiveBytesCount |]
            let connSentUsage =
                m_SendBytesPerConnectionGraphWriter.NormalizeValue [| itr.SentBytesCount |]

            // calc connection graph scale
            let connSace, connScaleLabel =
                seq{ connReceiveUsage; connSentUsage }
                |> Seq.concat
                |> GraphWriter.CalcScale_BytesPerSec 

            let conHandler = new RoutedEventHandler(
                fun ( sender : obj ) _ ->
                    if ( sender :?> ListViewItem ).IsSelected then
                        m_ReceiveBytesPerConnectionHeightTextBlock.Text <- connScaleLabel
                        m_ReceiveBytesPerConnectionGraphWriter.SetValue connReceiveUsage connSace
                        m_ReceiveBytesPerConnectionGraphWriter.UpdateGraph()
                        m_SendBytesPerConnectionHeightTextBlock.Text <- connScaleLabel
                        m_SendBytesPerConnectionGraphWriter.SetValue connSentUsage connSace
                        m_SendBytesPerConnectionGraphWriter.UpdateGraph()
                        this.ShowConnectionParameters itr
            )

            let c =
                if m_ConnectionList.Items.Count <= idx then
                    1
                else
                    let ctvi = m_ConnectionList.Items.Item( idx ) :?> ListViewItem
                    let _, tviCID, tviConCnt = ctvi.Tag :?> ( RoutedEventHandler * uint16 * int )
                    concnt_me.Compare ( cid_me.fromPrim tviCID ) ( concnt_me.fromPrim tviConCnt ) itr.ConnectionID itr.ConnectionCount

            if c = 0 then
                // update connection tree node
                let ctvi = m_ConnectionList.Items.Item( idx ) :?> ListViewItem
                let e, _, _ = ctvi.Tag :?> ( RoutedEventHandler * uint16 * int )
                ctvi.Selected.RemoveHandler e
                ctvi.Tag <- ( conHandler, itr.ConnectionID, itr.ConnectionCount )
                ctvi.Selected.AddHandler conHandler

                if ctvi.IsSelected then
                    m_ReceiveBytesPerConnectionHeightTextBlock.Text <- connScaleLabel
                    m_ReceiveBytesPerConnectionGraphWriter.SetValue connReceiveUsage connSace
                    m_ReceiveBytesPerConnectionGraphWriter.UpdateGraph()
                    m_SendBytesPerConnectionHeightTextBlock.Text <- connScaleLabel
                    m_SendBytesPerConnectionGraphWriter.SetValue connSentUsage connSace
                    m_SendBytesPerConnectionGraphWriter.UpdateGraph()

                idx + 1
            elif c < 0 then
                m_ConnectionList.Items.RemoveAt idx
                idx
            else
                // insert new connection tree node
                let ctvi = new ListViewItem( Content = {| TSIH=itr.TSIH; CID=itr.ConnectionID; ConCnt=itr.ConnectionCount |} )
                m_ConnectionList.Items.Insert( idx, ctvi )
                ctvi.Tag <- ( conHandler, itr.ConnectionID, itr.ConnectionCount )
                ctvi.Selected.AddHandler conHandler
                idx + 1
        ) 0
        |> ( fun idx -> while m_ConnectionList.Items.Count > idx do m_ConnectionList.Items.RemoveAt idx )

    /// <summary>
    ///  Set the connection paramter values to the parameter list controll.
    /// </summary>
    /// <param name="conn">
    ///  Selected connection parameter values.
    /// </param>
    member private _.ShowConnectionParameters ( conn : TargetDeviceCtrlRes.T_Connection ) : unit =
        m_ConnectionParametersListView.Items.Clear()
        m_ConnectionParametersListView.Items.Add { Name = "Connection ID"; Value = sprintf "%d" conn.ConnectionID } |> ignore
        m_ConnectionParametersListView.Items.Add { Name = "Connection Count"; Value = sprintf "%d" conn.ConnectionCount } |> ignore
        m_ConnectionParametersListView.Items.Add { Name = "AuthMethod"; Value = conn.ConnectionParameters.AuthMethod } |> ignore
        m_ConnectionParametersListView.Items.Add { Name = "HeaderDigest"; Value = conn.ConnectionParameters.HeaderDigest } |> ignore
        m_ConnectionParametersListView.Items.Add { Name = "DataDigest"; Value = conn.ConnectionParameters.DataDigest } |> ignore
        m_ConnectionParametersListView.Items.Add { Name = "MaxRecvDataSegmentLength(Initiator)"; Value = sprintf "%d" conn.ConnectionParameters.MaxRecvDataSegmentLength_I } |> ignore
        m_ConnectionParametersListView.Items.Add { Name = "MaxRecvDataSegmentLength(Target)"; Value = sprintf "%d" conn.ConnectionParameters.MaxRecvDataSegmentLength_T } |> ignore
        m_ConnectionParametersListView.Items.Add { Name = "Establish Time"; Value = conn.EstablishTime.ToString "o" } |> ignore


