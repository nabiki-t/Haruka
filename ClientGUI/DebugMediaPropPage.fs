//=============================================================================
// Haruka Software Storage.
// DebugMediaPropPage.fs : Implement the function to display Dummy Media property page in the main window.
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

/// <summary>
///  DebugMediaPropPage class.
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
type DebugMediaPropPage(
    m_Config : GUIConfig,
    m_PropPage : Grid,
    m_MainWindow : IMainWindowIFForPP,
    m_ServerStatus : ServerStatus,
    m_CtrlConnection : CtrlConnection,
    m_NodeID : CONFNODE_T
) as this =

    // Get controll objects in the property page.
    let m_ConfigurationExpander = m_PropPage.FindName( "ConfigurationExpander" ) :?> Expander
    let m_EditButton = m_PropPage.FindName( "EditButton" ) :?> Button
    let m_ApplyButton = m_PropPage.FindName( "ApplyButton" ) :?> Button
    let m_DiscardButton = m_PropPage.FindName( "DiscardButton" ) :?> Button
    let m_ErrorMessageLabel = m_PropPage.FindName( "ErrorMessageLabel" ) :?> TextBlock
    let m_IdentNumberTextBox = m_PropPage.FindName( "IdentNumberTextBox" ) :?> TextBox
    let m_MediaNameTextBox = m_PropPage.FindName( "MediaNameTextBox" ) :?> TextBox
    let m_TrapExpander = m_PropPage.FindName( "TrapExpander" ) :?> Expander
    let m_AddTrapButton = m_PropPage.FindName( "AddTrapButton" ) :?> Button
    let m_ClearTrapButton = m_PropPage.FindName( "ClearTrapButton" ) :?> Button
    let m_TrapListView = m_PropPage.FindName( "TrapListView" ) :?> ListView

    // timer object
    let m_Timer = new DispatcherTimer()

    // Initialize procedure for the property page.
    do
        // Set controller localized text
        m_Config.SetLocalizedText "DebugMediaPropPage" m_PropPage

        // Set event handler
        m_EditButton.Click.AddHandler ( fun _ _ -> this.OnClick_EditButton() )
        m_ApplyButton.Click.AddHandler ( fun _ _ -> this.OnClick_ApplyButton() )
        m_DiscardButton.Click.AddHandler ( fun _ _ -> this.OnClick_DiscardButton() )
        m_ConfigurationExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "DebugMediaPropPage" m_ConfigurationExpander.Name true )
        m_ConfigurationExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "DebugMediaPropPage" m_ConfigurationExpander.Name false )

        m_TrapExpander.Expanded.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "DebugMediaPropPage" m_TrapExpander.Name true )
        m_TrapExpander.Collapsed.AddHandler ( fun _ _ -> UserOpeStat.SetExpanded "DebugMediaPropPage" m_TrapExpander.Name false )
        m_AddTrapButton.Click.AddHandler ( fun _ _ -> this.OnClick_AddTrapButton() )
        m_ClearTrapButton.Click.AddHandler ( fun _ _ -> this.OnClick_ClearTrapButton() )
        m_Timer.Tick.AddHandler ( fun _ _ -> this.OnTimer() )

        m_EditButton.IsEnabled <- false
        m_ApplyButton.IsEnabled <- false
        m_DiscardButton.IsEnabled <- false

        // Set default value
        m_ConfigurationExpander.IsExpanded <- UserOpeStat.GetExpanded "DebugMediaPropPage" m_ConfigurationExpander.Name
        m_TrapExpander.IsExpanded <- UserOpeStat.GetExpanded "DebugMediaPropPage" m_TrapExpander.Name
        m_TrapListView.Items.Clear()

        m_Timer.Interval <- new TimeSpan( 0, 0, 3 )
        m_Timer.Start()

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
            let dm = m_ServerStatus.GetNode m_NodeID :?> IMediaNode
            let tgn = ( m_ServerStatus.GetAncestorTargetGroup dm ).Value
            let tdn = ( m_ServerStatus.GetAncestorTargetDevice dm ).Value
            let lu = ( m_ServerStatus.GetAncestorLogicalUnit dm ).Value

            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                do! m_MainWindow.UpdateRunningStatus()
                let struct( tdActived, tgLoaded ) =
                    m_MainWindow.IsTGLoaded tdn.TargetDeviceID tgn.TargetGroupID

                if tdActived && tgLoaded then
                    let! traps = m_CtrlConnection.DebugMedia_GetAllTraps tdn.TargetDeviceID lu.LUN dm.IdentNumber
                    m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                        m_AddTrapButton.IsEnabled <- true
                        m_ClearTrapButton.IsEnabled <- true
                        m_TrapListView.IsEnabled <- true
                        this.ShowConfigValue true false
                        this.ShowAllTraps traps
                        m_MainWindow.SetProgress false
                    ) |> ignore
                else
                    m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                        m_AddTrapButton.IsEnabled <- false
                        m_ClearTrapButton.IsEnabled <- false
                        m_TrapListView.IsEnabled <- false
                        this.ShowConfigValue false false
                        this.ShowAllTraps []
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
            ()

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
            let mn = m_ServerStatus.GetNode m_NodeID :?> ConfNode_DebugMedia

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

            let newNode = m_ServerStatus.UpdateDebugMediaNode mn ( mediaidx_me.fromPrim identNumber ) mediaName
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
    ///  "Add trap" button was clicked.
    /// </summary>
    member private this.OnClick_AddTrapButton() : unit = 
        let dm = m_ServerStatus.GetNode m_NodeID :?> IMediaNode
        let tgn = ( m_ServerStatus.GetAncestorTargetGroup dm ).Value
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice dm ).Value
        let lu = ( m_ServerStatus.GetAncestorLogicalUnit dm ).Value

        let d = EditDebugTrapDialog( m_Config )
        match d.Show() with
        | DialogResult.Cancel ->
            ()
        | DialogResult.Ok( x ) ->
            m_MainWindow.ProcCtrlQuery false ( fun () -> task {
                do! m_MainWindow.UpdateRunningStatus()
                let struct( tdActived, tgLoaded ) =
                    m_MainWindow.IsTGLoaded tdn.TargetDeviceID tgn.TargetGroupID
                if tdActived && tgLoaded then
                    do! m_CtrlConnection.DebugMedia_AddTrap tdn.TargetDeviceID lu.LUN dm.IdentNumber x.Event x.Action
                    let! traps = m_CtrlConnection.DebugMedia_GetAllTraps tdn.TargetDeviceID lu.LUN dm.IdentNumber
                    m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                        this.ShowAllTraps traps
                    ) |> ignore
                else
                    m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                        this.ShowAllTraps []
                    ) |> ignore
            })

    /// <summary>
    ///  "Remove trap" button was clicked.
    /// </summary>
    member private this.OnClick_ClearTrapButton() : unit = 
        let dm = m_ServerStatus.GetNode m_NodeID :?> IMediaNode
        let tgn = ( m_ServerStatus.GetAncestorTargetGroup dm ).Value
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice dm ).Value
        let lu = ( m_ServerStatus.GetAncestorLogicalUnit dm ).Value

        m_MainWindow.ProcCtrlQuery false ( fun () -> task {
            do! m_MainWindow.UpdateRunningStatus()
            let struct( tdActived, tgLoaded ) =
                m_MainWindow.IsTGLoaded tdn.TargetDeviceID tgn.TargetGroupID
            if tdActived && tgLoaded then
                do! m_CtrlConnection.DebugMedia_ClearTraps tdn.TargetDeviceID lu.LUN dm.IdentNumber
                let! traps = m_CtrlConnection.DebugMedia_GetAllTraps tdn.TargetDeviceID lu.LUN dm.IdentNumber
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    this.ShowAllTraps traps
                ) |> ignore
            else
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    this.ShowAllTraps []
                ) |> ignore
        })


    /// <summary>
    ///  On timer handler.
    /// </summary>
    member private this.OnTimer() : unit =
        m_Timer.Stop()
        let dm = m_ServerStatus.GetNode m_NodeID :?> IMediaNode
        let tgn = ( m_ServerStatus.GetAncestorTargetGroup dm ).Value
        let tdn = ( m_ServerStatus.GetAncestorTargetDevice dm ).Value
        let lu = ( m_ServerStatus.GetAncestorLogicalUnit dm ).Value

        m_MainWindow.ProcCtrlQuery false ( fun () -> task {
            do! m_MainWindow.UpdateRunningStatus()
            let struct( tdActived, tgLoaded ) =
                m_MainWindow.IsTGLoaded tdn.TargetDeviceID tgn.TargetGroupID
            if tdActived && tgLoaded then
                let! traps = m_CtrlConnection.DebugMedia_GetAllTraps tdn.TargetDeviceID lu.LUN dm.IdentNumber
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    this.ShowAllTraps traps
                    m_Timer.Start()
                ) |> ignore
            else
                m_PropPage.Dispatcher.InvokeAsync ( fun () ->
                    this.ShowAllTraps []
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
        let mn = m_ServerStatus.GetNode m_NodeID :?> IMediaNode
        m_EditButton.IsEnabled <- ( not editmode ) && ( not loaded )
        m_ApplyButton.IsEnabled <- editmode && ( not loaded )
        m_DiscardButton.IsEnabled <- editmode && ( not loaded )
        m_ErrorMessageLabel.Text <- ""

        m_IdentNumberTextBox.IsEnabled <- editmode && ( not loaded )
        m_IdentNumberTextBox.Text <- sprintf "%d" ( mediaidx_me.toPrim mn.IdentNumber )
        m_MediaNameTextBox.IsEnabled <- editmode && ( not loaded )
        m_MediaNameTextBox.Text <- mn.Name

    member private _.ShowAllTraps ( argTraps : MediaCtrlRes.T_Trap list ) : unit =
        m_TrapListView.Items.Clear()
        for itr in argTraps do
            let eventStr =
                match itr.Event with
                | MediaCtrlRes.U_TestUnitReady() ->
                    "TestUnitReady"
                | MediaCtrlRes.U_ReadCapacity() ->
                    "ReadCapacity"
                | MediaCtrlRes.U_Read( x ) ->
                    sprintf "Read( 0x%016X, 0x%016X )" x.StartLBA x.EndLBA
                | MediaCtrlRes.U_Write( x ) ->
                    sprintf "Write( 0x%016X, 0x%016X )" x.StartLBA x.EndLBA
                | MediaCtrlRes.U_Format() ->
                    "Format"
            let actionStr =
                match itr.Action with
                | MediaCtrlRes.U_ACA( x ) ->
                    sprintf "ACA( %s )" x
                | MediaCtrlRes.U_LUReset( x ) ->
                    sprintf "LUReset( %s )" x
                | MediaCtrlRes.U_Count( x ) ->
                    sprintf "Count( Index=%d, Value=%d )" x.Index x.Value

            m_TrapListView.Items.Add {| Event = eventStr; Action = actionStr; |} |> ignore

    