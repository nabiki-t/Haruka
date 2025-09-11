//=============================================================================
// Haruka Software Storage.
// CreateMediaFile.fs : Implement the function to display Create Media File dialog window.
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
open System.Collections.ObjectModel

open Haruka.Constants
open Haruka.IODataTypes
open Haruka.Commons
open Haruka.Client

//=============================================================================
// Class implementation

/// <summary>
///  Data type for status list.
/// </summary>
/// <param name="stat">
///  status value
/// </param>
type StatusListType( stat : HarukaCtrlerCtrlRes.T_Procs ) =

    /// Event object for property updated
    let propertyChanged = new Event< PropertyChangedEventHandler, PropertyChangedEventArgs >()

    /// PID    
    let m_PID = stat.ProcID

    /// Path Name
    let mutable m_PathName = stat.PathName

    /// File Type
    let mutable m_FileType = stat.FileType

    /// Status
    let mutable m_Status = StatusListType.GetStatusString stat.Status

    /// progress. If Status equals "Progress" or "Recovery", otherwise zero.
    let mutable m_Progress = StatusListType.GetProgressValue stat.Status

    /// Error Message
    let mutable m_Message = String.concat Environment.NewLine stat.ErrorMessage

    /// Error Message
    let mutable m_Tarminated = StatusListType.TerminatedOrNot stat.Status

    // Imprementation of INotifyPropertyChanged
    interface INotifyPropertyChanged with

        /// PropertyChanged event.
        [<CLIEvent>]
        member _.PropertyChanged = 
            propertyChanged.Publish

    /// Get PID field property.
    member _.PID = m_PID

    /// Get PathName field property.
    member _.PathName with get() = m_PathName

    /// Get FileType field property.
    member _.FileType with get() = m_FileType

    /// Get Status field property.
    member _.Status with get() = m_Status

    /// Get Progress field property.
    member _.Progress with get() = m_Progress

    /// Get Message field property.
    member _.Message with get() = m_Message

    /// Get process status property.
    member _.Terminated with get() = m_Tarminated


    /// <summary>
    ///  Update filed values.
    /// </summary>
    /// <param name="v">
    ///  Updated PathName filed value.
    /// </param>
    member this.Update ( v : HarukaCtrlerCtrlRes.T_Procs ) : unit =
        if m_PathName <> v.PathName then
            m_PathName <- v.PathName
            this.OnPropertyChanged "FileName"

        if m_FileType <> v.FileType then
            m_FileType <- v.FileType
            this.OnPropertyChanged "FileType"

        let wStat = StatusListType.GetStatusString v.Status
        if m_Status <> wStat then
            m_Status <- wStat
            this.OnPropertyChanged "Status"

        let wProc = StatusListType.GetProgressValue v.Status
        if m_Progress <> wProc then
            m_Progress <- wProc
            this.OnPropertyChanged "Progress"

        let wmsg = String.concat Environment.NewLine v.ErrorMessage
        if m_Message <> wmsg then
            m_Message <- wmsg
            this.OnPropertyChanged "Message"

        m_Tarminated <- StatusListType.TerminatedOrNot v.Status

    /// <summary>
    ///  Get Status field string from retrieved status value.
    /// </summary>
    /// <param name="s">
    ///  Retrieved status value.
    /// </param>
    /// <returns>
    ///  Status string shown in the status list.
    /// </returns>
    static member private GetStatusString ( s : HarukaCtrlerCtrlRes.T_Status ) : string =
        match s with
        | HarukaCtrlerCtrlRes.U_NotStarted() ->
            "NotStarted"
        | HarukaCtrlerCtrlRes.U_ProgressCreation x ->
            "Progress"
        | HarukaCtrlerCtrlRes.U_Recovery x ->
            "Recovery"
        | HarukaCtrlerCtrlRes.U_NormalEnd() ->
            "Succeeded"
        | HarukaCtrlerCtrlRes.U_AbnormalEnd() ->
            "Failed"

    /// <summary>
    ///  Get Progress field value from retrieved status value.
    /// </summary>
    /// <param name="s">
    ///  Retrieved status value.
    /// </param>
    /// <returns>
    ///  Progress value shown in the status list.
    /// </returns>
    static member private GetProgressValue ( s : HarukaCtrlerCtrlRes.T_Status ) : int =
        match s with
        | HarukaCtrlerCtrlRes.U_NotStarted() ->
            0
        | HarukaCtrlerCtrlRes.U_ProgressCreation x ->
            int x
        | HarukaCtrlerCtrlRes.U_Recovery x ->
            int x
        | HarukaCtrlerCtrlRes.U_NormalEnd() ->
            0
        | HarukaCtrlerCtrlRes.U_AbnormalEnd() ->
            0

    /// <summary>
    ///  Determine InitMediaProc had been terminated or not.
    /// </summary>
    /// <param name="s">
    ///  Retrieved status from the controller.
    /// </param>
    /// <returns>
    ///  True, if the process had been terminated. Otherwise false.
    /// </returns>
    static member private TerminatedOrNot ( s : HarukaCtrlerCtrlRes.T_Status ) : bool =
        match s with
        | HarukaCtrlerCtrlRes.U_NormalEnd() ->
            true
        | HarukaCtrlerCtrlRes.U_AbnormalEnd() ->
            true
        | _ ->
            false

    /// <summary>
    ///  Notify updated property.
    /// </summary>
    /// <param name="propertyName">
    ///  Updated property filed name.
    /// </param>
    member private this.OnPropertyChanged ( propertyName : string ) =
        propertyChanged.Trigger( this, PropertyChangedEventArgs propertyName )




/// <summary>
///  CreateMediaFile class.
/// </summary>
/// <param name="m_Config">
///  Loaded configurations for GUI client.
/// </param>
/// <param name="m_MainWindow">
///  The main window object.
/// </param>
type CreateMediaFile(
    m_Config : GUIConfig,
    m_MainWindow : IMainWindowIFForPP
) as this =

    /// Load XAML resouces for dialog window.
    let m_Dialog = m_Config.UIElem.Get( PropertyViewIndex.PVI_CREATE_MEDIA_FILE ) :?> Window

    // Get controll objects in Dialog.
    let m_PathNameTextBox = m_Dialog.FindName( "PathNameTextBox" ) :?> TextBox
    let m_FileTypeCombo = m_Dialog.FindName( "FileTypeCombo" ) :?> ComboBox
    let m_FileSizeTextBox = m_Dialog.FindName( "FileSizeTextBox" ) :?> TextBox
    let m_BlockSizeCombo = m_Dialog.FindName( "BlockSizeCombo" ) :?> ComboBox
    let m_CreateButton = m_Dialog.FindName( "CreateButton" ) :?> Button
    let m_KillButton = m_Dialog.FindName( "KillButton" ) :?> Button
    let m_StatusListView = m_Dialog.FindName( "StatusListView" ) :?> ListView

    // register unloaded callback
    let m_UnloadCBHandler = m_MainWindow.SubscribeLogoutEvent this.OnLogout

    // timer object
    let m_Timer = new DispatcherTimer()

    /// status list contents, indexed by PID.
    /// If a PID is reused, unrelated entries will be updated, but this risk is low and can be ignored.
    let m_Status = new ObservableCollection< StatusListType >()

    do
        // Set controller localized text
        m_Dialog.Title <- m_Config.CtrlText.Get( "CreateMediaFile", "TITLE" )
        m_Config.SetLocalizedText "CreateMediaFile" m_Dialog

        // Set controller initial status
        this.SetDialogEnable true

        // Register event handler
        m_Dialog.Closed.AddHandler this.OnClosed_Window
        m_PathNameTextBox.TextChanged.AddHandler this.OnTextChanged_PathNameTextBox
        m_FileTypeCombo.SelectionChanged.AddHandler this.OnSelectionChanged_FileTypeCombo
        m_FileSizeTextBox.TextChanged.AddHandler this.OnTextChanged_FileSizeTextBox
        m_BlockSizeCombo.SelectionChanged.AddHandler this.OnSelectionChanged_BlockSizeCombo
        m_StatusListView.SelectionChanged.AddHandler this.OnSelectionChanged_StatusListView

        m_CreateButton.Click.AddHandler this.OnClicked_CreateButton
        m_KillButton.Click.AddHandler this.OnClicked_KillButton
        m_Timer.Tick.AddHandler ( fun _ _ -> this.OnTimer() )

        m_Timer.Interval <- new TimeSpan( 0, 0, 1 )
        this.OnTimer()

        // Set default value
        m_PathNameTextBox.Text <- ""
        m_FileTypeCombo.SelectedIndex <- 0
        m_FileSizeTextBox.Text <- ""
        m_BlockSizeCombo.SelectedIndex <- -1
        m_StatusListView.Items.Clear()
        m_StatusListView.ItemsSource <- m_Status
        m_KillButton.IsEnabled <- false


    ///////////////////////////////////////////////////////////////////////////
    // Public method

    /// <summary>
    ///  Display the window
    /// </summary>
    member _.Show () : unit =
        m_Dialog.ShowDialog() |> ignore

    ///////////////////////////////////////////////////////////////////////////
    // Event handler

    /// <summary>
    ///  On dialog Closed.
    /// </summary>
    /// <param name="sender">Event sender object.</param>
    /// <param name="e">Event argument.</param>
    member private _.OnClosed_Window ( sender : obj ) ( e : EventArgs ) : unit =
        m_Timer.Stop()
        m_MainWindow.UnsubscribeLogoutEvent m_UnloadCBHandler

    /// <summary>
    ///  On text changed at PathNameTextBox.
    /// </summary>
    /// <param name="sender">Event sender object.</param>
    /// <param name="e">Event argument.</param>
    member private this.OnTextChanged_PathNameTextBox ( sender : obj ) ( e : EventArgs ) : unit =
        this.CheckCreatable()

    /// <summary>
    ///  On selection changed at FileTypeCombo.
    /// </summary>
    /// <param name="sender">Event sender object.</param>
    /// <param name="e">Event argument.</param>
    member private this.OnSelectionChanged_FileTypeCombo ( sender : obj ) ( e : RoutedEventArgs ) : unit =
        this.CheckCreatable()

    /// <summary>
    ///  On text changed at FileSizeTextBox.
    /// </summary>
    /// <param name="sender">Event sender object.</param>
    /// <param name="e">Event argument.</param>
    member private this.OnTextChanged_FileSizeTextBox ( sender : obj ) ( e : EventArgs ) : unit =
        this.CheckCreatable()

    /// <summary>
    ///  On selection changed at BlockSizeCombo.
    /// </summary>
    /// <param name="sender">Event sender object.</param>
    /// <param name="e">Event argument.</param>
    member private this.OnSelectionChanged_BlockSizeCombo ( sender : obj ) ( e : EventArgs ) : unit =
        this.CheckCreatable()

    /// <summary>
    ///  On selection changed at StatusListView.
    /// </summary>
    /// <param name="sender">Event sender object.</param>
    /// <param name="e">Event argument.</param>
    member private this.OnSelectionChanged_StatusListView ( sender : obj ) ( e : EventArgs ) : unit =
        let idx = m_StatusListView.SelectedIndex
        if idx >= 0 && idx < m_Status.Count then
            m_KillButton.IsEnabled <-
                ( m_Status.Item idx ).Terminated |> not
        else
            m_KillButton.IsEnabled <- false

    /// <summary>
    ///  On clicked at CreateButton.
    /// </summary>
    /// <param name="sender">Event sender object.</param>
    /// <param name="e">Event argument.</param>
    member private this.OnClicked_CreateButton ( sender : obj ) ( e : RoutedEventArgs ) : unit =
        try
            let wFileSize = m_FileSizeTextBox.Text |> Int64.Parse
            let wFileName = m_PathNameTextBox.Text
            if wFileName.Length <= 0 then raise <| FormatException ""
            
            m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                let cc = m_MainWindow.GetCtrlConnection()
                if cc.IsSome then
                    let! _ = cc.Value.CreateMediaFile_PlainFile wFileName ( wFileSize * 1024L * 1024L )
                    let! stat = cc.Value.GetInitMediaStatus()
                    m_Dialog.Dispatcher.InvokeAsync ( fun () ->
                        this.UpdateStatusList stat
                        m_MainWindow.SetProgress false
                        m_Timer.Start()
                    ) |> ignore
            })

        with
        | :? FormatException
        | :? OverflowException ->
            ()

    /// <summary>
    ///  On clicked at KillButton.
    /// </summary>
    /// <param name="sender">Event sender object.</param>
    /// <param name="e">Event argument.</param>
    member private _.OnClicked_KillButton ( sender : obj ) ( e : RoutedEventArgs ) : unit =
        m_Timer.Stop()
        let idx = m_StatusListView.SelectedIndex
        if idx >= 0 && idx < m_Status.Count then
            let wstat = m_Status.Item idx
            if wstat.Terminated |> not then
                m_MainWindow.ProcCtrlQuery true ( fun () -> task {
                    let cc = m_MainWindow.GetCtrlConnection()
                    if cc.IsSome then
                        do! cc.Value.KillInitMediaProc wstat.PID
                        let! stat = cc.Value.GetInitMediaStatus()
                        m_Dialog.Dispatcher.InvokeAsync ( fun () ->
                            this.UpdateStatusList stat
                            m_MainWindow.SetProgress false
                            if stat.Length > 0 then
                                m_Timer.Start()
                        ) |> ignore
                })

    /// <summary>
    ///  Timer event.
    /// </summary>
    member private this.OnTimer() : unit = 
        m_Timer.Stop()
        m_MainWindow.ProcCtrlQuery false ( fun () -> task {
            let cc = m_MainWindow.GetCtrlConnection()
            if cc.IsSome then
                let! stat = cc.Value.GetInitMediaStatus()
                m_Dialog.Dispatcher.InvokeAsync ( fun () ->
                    this.UpdateStatusList stat
                    if stat.Length > 0 then
                        m_Timer.Start()
                ) |> ignore
        })

    ///////////////////////////////////////////////////////////////////////////
    // Private method

    /// <summary>
    ///  Enable/disable the dialog.
    /// </summary>
    /// <param name="f">
    ///  True, if to enable the dialog.
    /// </param>
    member private _.SetDialogEnable ( f : bool ) =
        m_Dialog.IsEnabled <- f
        m_PathNameTextBox.IsEnabled <- f
        m_FileTypeCombo.IsEnabled <- f
        m_FileSizeTextBox.IsEnabled <- f
        m_BlockSizeCombo.IsEnabled <- false
        m_CreateButton.IsEnabled <- f
        m_KillButton.IsEnabled <- f
        m_StatusListView.IsEnabled <- f

    /// <summary>
    ///  Called when connection has been disconnected.
    /// </summary>
    member private this.OnLogout() : unit =
        m_Timer.Stop()
        this.SetDialogEnable false

    /// <summary>
    ///  Update the status list based on the status obtained from the Controller.
    /// </summary>
    /// <param name="pl">
    ///  Status
    /// </param>
    member private _.UpdateStatusList ( pl : HarukaCtrlerCtrlRes.T_Procs list ) : unit =
        for itr in pl do
            let w = m_Status |> Seq.tryFind ( fun itr2 -> itr2.PID = itr.ProcID )
            match w with
            | Some x ->
                x.Update itr
            | None ->
                m_Status.Add ( StatusListType itr )

        let idx = m_StatusListView.SelectedIndex
        if idx >= 0 && idx < m_Status.Count then
            m_KillButton.IsEnabled <-
                ( m_Status.Item idx ).Terminated |> not
        else
            m_KillButton.IsEnabled <- false

    /// <summary>
    ///  Determine if the Create button should be enabled.
    /// </summary>
    member private _.CheckCreatable() =
        match m_FileTypeCombo.SelectedIndex with
        | 0 ->  // plain file
            let r, _ = Int64.TryParse m_FileSizeTextBox.Text
            m_CreateButton.IsEnabled <- 
                m_PathNameTextBox.Text.Length > 0 && r
        | _ ->
            m_CreateButton.IsEnabled <- false
