//=============================================================================
// Haruka Software Storage.
// MainWindow.fs : Implement the function to display MainWindow in Haruka client GUI.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.Collections.Generic
open System.Windows
open System.Windows.Controls
open System.Threading.Tasks
open System.ComponentModel

open Haruka.Constants
open Haruka.IODataTypes
open Haruka.Commons
open Haruka.Client

//=============================================================================
// Class implementation

/// <summary>
///  MainWindow class.
/// </summary>
/// <param name="m_ExeDir">
///  Directory path name that stores ClientGUI.exe file.
/// </param>
type MainWindow( m_ExeDir : string ) as this =

    /// Load GUI configurations
    let m_Config = new GUIConfig( m_ExeDir )

    /// background task queue
    let m_BKTaskQueue = new TaskQueue()

    /// Get XAML resouces for main window.
    let m_Window = m_Config.UIElem.Get( PropertyViewIndex.PVI_MAIN_WINDOW ) :?> Window

    // Get controll objects in main window.
    let m_MenuLogin = m_Window.FindName( "Menu_Login" ) :?> MenuItem
    let m_MenuLogout = m_Window.FindName( "Menu_Logout" ) :?> MenuItem
    let m_MenuReload = m_Window.FindName( "Menu_Reload" ) :?> MenuItem
    let m_MenuVerify = m_Window.FindName( "Menu_Verify" ) :?> MenuItem
    let m_MenuCreateMediaFile = m_Window.FindName( "Menu_CreateMediaFile" ) :?> MenuItem
    let m_MenuPulish = m_Window.FindName( "Menu_Pulish" ) :?> MenuItem
    let m_MenuExit = m_Window.FindName( "Menu_Exit" ) :?> MenuItem
    let m_MenuAbout = m_Window.FindName( "Menu_About" ) :?> MenuItem
    let m_StructTree = m_Window.FindName( "StructTree" ) :?> TreeView
    let m_VerifyResultList = m_Window.FindName( "VerifyResultList" ) :?> ListView
    let m_RequestingProgress = m_Window.FindName( "RequestingProgress" ) :?> ProgressBar
    let m_PropGrid = m_Window.FindName( "PropGrid" ) :?> ScrollViewer

    let m_ContMenu_TVI_Controller = m_Window.FindName "ContMenu_TVI_Controller" :?> ContextMenu
    let m_ContMenu_TVI_Controller_Select = m_Window.FindName "ContMenu_TVI_Controller_Select" :?> MenuItem
    let m_ContMenu_TVI_Controller_Logout = m_Window.FindName "ContMenu_TVI_Controller_Logout" :?> MenuItem
    let m_ContMenu_TVI_Controller_Reload = m_Window.FindName "ContMenu_TVI_Controller_Reload" :?> MenuItem
    let m_ContMenu_TVI_Controller_ADDTargetDevice = m_Window.FindName "ContMenu_TVI_Controller_AddTargetDevice" :?> MenuItem
    let m_ContMenu_TVI_Controller_Paste = m_Window.FindName "ContMenu_TVI_Controller_Paste" :?> MenuItem

    let m_ContMenu_TVI_TargetDevice = m_Window.FindName "ContMenu_TVI_TargetDevice" :?> ContextMenu
    let m_ContMenu_TVI_TargetDevice_Select = m_Window.FindName "ContMenu_TVI_TargetDevice_Select" :?> MenuItem
    let m_ContMenu_TVI_TargetDevice_Start = m_Window.FindName "ContMenu_TVI_TargetDevice_Start" :?> MenuItem
    let m_ContMenu_TVI_TargetDevice_Stop = m_Window.FindName "ContMenu_TVI_TargetDevice_Stop" :?> MenuItem
    let m_ContMenu_TVI_TargetDevice_AddTargetGroup = m_Window.FindName "ContMenu_TVI_TargetDevice_AddTargetGroup" :?> MenuItem
    let m_ContMenu_TVI_TargetDevice_AddNetworkPortal = m_Window.FindName "ContMenu_TVI_TargetDevice_AddNetworkPortal" :?> MenuItem
    let m_ContMenu_TVI_TargetDevice_Copy = m_Window.FindName "ContMenu_TVI_TargetDevice_Copy" :?> MenuItem
    let m_ContMenu_TVI_TargetDevice_Paste = m_Window.FindName "ContMenu_TVI_TargetDevice_Paste" :?> MenuItem
    let m_ContMenu_TVI_TargetDevice_Delete = m_Window.FindName "ContMenu_TVI_TargetDevice_Delete" :?> MenuItem

    let m_ContMenu_TVI_TargetGroup = m_Window.FindName "ContMenu_TVI_TargetGroup" :?> ContextMenu
    let m_ContMenu_TVI_TargetGroup_Select = m_Window.FindName "ContMenu_TVI_TargetGroup_Select" :?> MenuItem
    let m_ContMenu_TVI_TargetGroup_Activate = m_Window.FindName "ContMenu_TVI_TargetGroup_Activate" :?> MenuItem
    let m_ContMenu_TVI_TargetGroup_Load = m_Window.FindName "ContMenu_TVI_TargetGroup_Load" :?> MenuItem
    let m_ContMenu_TVI_TargetGroup_Inactivate = m_Window.FindName "ContMenu_TVI_TargetGroup_Inactivate" :?> MenuItem
    let m_ContMenu_TVI_TargetGroup_Unload = m_Window.FindName "ContMenu_TVI_TargetGroup_Unload" :?> MenuItem
    let m_ContMenu_TVI_TargetGroup_AddTarget = m_Window.FindName "ContMenu_TVI_TargetGroup_AddTarget" :?> MenuItem
    let m_ContMenu_TVI_TargetGroup_AddBlockDeviceLU = m_Window.FindName "ContMenu_TVI_TargetGroup_AddBlockDeviceLU" :?> MenuItem
    let m_ContMenu_TVI_TargetGroup_Copy = m_Window.FindName "ContMenu_TVI_TargetGroup_Copy" :?> MenuItem
    let m_ContMenu_TVI_TargetGroup_Paste = m_Window.FindName "ContMenu_TVI_TargetGroup_Paste" :?> MenuItem
    let m_ContMenu_TVI_TargetGroup_Delete = m_Window.FindName "ContMenu_TVI_TargetGroup_Delete" :?> MenuItem

    let m_ContMenu_TVI_NetworkPortal = m_Window.FindName "ContMenu_TVI_NetworkPortal" :?> ContextMenu
    let m_ContMenu_TVI_NetworkPortal_Select = m_Window.FindName "ContMenu_TVI_NetworkPortal_Select" :?> MenuItem
    let m_ContMenu_TVI_NetworkPortal_Copy = m_Window.FindName "ContMenu_TVI_NetworkPortal_Copy" :?> MenuItem
    let m_ContMenu_TVI_NetworkPortal_Delete = m_Window.FindName "ContMenu_TVI_NetworkPortal_Delete" :?> MenuItem

    let m_ContMenu_TVI_Target = m_Window.FindName "ContMenu_TVI_Target" :?> ContextMenu
    let m_ContMenu_TVI_Target_Select = m_Window.FindName "ContMenu_TVI_Target_Select" :?> MenuItem
    let m_ContMenu_TVI_Target_Copy = m_Window.FindName "ContMenu_TVI_Target_Copy" :?> MenuItem
    let m_ContMenu_TVI_Target_Delete = m_Window.FindName "ContMenu_TVI_Target_Delete" :?> MenuItem

    let m_ContMenu_TVI_BlockDeviceLU = m_Window.FindName "ContMenu_TVI_BlockDeviceLU" :?> ContextMenu
    let m_ContMenu_TVI_BlockDeviceLU_Select = m_Window.FindName "ContMenu_TVI_BlockDeviceLU_Select" :?> MenuItem
    let m_ContMenu_TVI_BlockDeviceLU_AddPlainFileMedia = m_Window.FindName "ContMenu_TVI_BlockDeviceLU_AddPlainFileMedia" :?> MenuItem
    let m_ContMenu_TVI_BlockDeviceLU_AddMemBufferMedia = m_Window.FindName "ContMenu_TVI_BlockDeviceLU_AddMemBufferMedia" :?> MenuItem
    let m_ContMenu_TVI_BlockDeviceLU_AddDebugMedia = m_Window.FindName "ContMenu_TVI_BlockDeviceLU_AddDebugMedia" :?> MenuItem
    let m_ContMenu_TVI_BlockDeviceLU_Copy = m_Window.FindName "ContMenu_TVI_BlockDeviceLU_Copy" :?> MenuItem
    let m_ContMenu_TVI_BlockDeviceLU_Paste = m_Window.FindName "ContMenu_TVI_BlockDeviceLU_Paste" :?> MenuItem
    let m_ContMenu_TVI_BlockDeviceLU_Delete = m_Window.FindName "ContMenu_TVI_BlockDeviceLU_Delete" :?> MenuItem

    let m_ContMenu_TVI_DummyDeviceLU = m_Window.FindName "ContMenu_TVI_DummyDeviceLU" :?> ContextMenu
    let m_ContMenu_TVI_DummyDeviceLU_Select = m_Window.FindName "ContMenu_TVI_DummyDeviceLU_Select" :?> MenuItem
    let m_ContMenu_TVI_DummyDeviceLU_Delete = m_Window.FindName "ContMenu_TVI_DummyDeviceLU_Delete" :?> MenuItem

    let m_ContMenu_TVI_PlainFileMedia = m_Window.FindName "ContMenu_TVI_PlainFileMedia" :?> ContextMenu
    let m_ContMenu_TVI_PlainFileMedia_Select = m_Window.FindName "ContMenu_TVI_PlainFileMedia_Select" :?> MenuItem
    let m_ContMenu_TVI_PlainFileMedia_Copy = m_Window.FindName "ContMenu_TVI_PlainFileMedia_Copy" :?> MenuItem
    let m_ContMenu_TVI_PlainFileMedia_Delete = m_Window.FindName "ContMenu_TVI_PlainFileMedia_Delete" :?> MenuItem

    let m_ContMenu_TVI_MemBufferMedia = m_Window.FindName "ContMenu_TVI_MemBufferMedia" :?> ContextMenu
    let m_ContMenu_TVI_MemBufferMedia_Select = m_Window.FindName "ContMenu_TVI_MemBufferMedia_Select" :?> MenuItem
    let m_ContMenu_TVI_MemBufferMedia_Copy = m_Window.FindName "ContMenu_TVI_MemBufferMedia_Copy" :?> MenuItem
    let m_ContMenu_TVI_MemBufferMedia_Delete = m_Window.FindName "ContMenu_TVI_MemBufferMedia_Delete" :?> MenuItem

    let m_ContMenu_TVI_DebugMedia = m_Window.FindName "ContMenu_TVI_DebugMedia" :?> ContextMenu
    let m_ContMenu_TVI_DebugMedia_Select = m_Window.FindName "ContMenu_TVI_DebugMedia_Select" :?> MenuItem
    let m_ContMenu_TVI_DebugMedia_AddPlainFileMedia = m_Window.FindName "ContMenu_TVI_DebugMedia_AddPlainFileMedia" :?> MenuItem
    let m_ContMenu_TVI_DebugMedia_AddMemBufferMedia = m_Window.FindName "ContMenu_TVI_DebugMedia_AddMemBufferMedia" :?> MenuItem
    let m_ContMenu_TVI_DebugMedia_AddDebugMedia = m_Window.FindName "ContMenu_TVI_DebugMedia_AddDebugMedia" :?> MenuItem
    let m_ContMenu_TVI_DebugMedia_Copy = m_Window.FindName "ContMenu_TVI_DebugMedia_Copy" :?> MenuItem
    let m_ContMenu_TVI_DebugMedia_Paste = m_Window.FindName "ContMenu_TVI_DebugMedia_Paste" :?> MenuItem
    let m_ContMenu_TVI_DebugMedia_Delete = m_Window.FindName "ContMenu_TVI_DebugMedia_Delete" :?> MenuItem

    let m_ContMenu_TVI_DummyMedia = m_Window.FindName "ContMenu_TVI_DummyMedia" :?> ContextMenu
    let m_ContMenu_TVI_DummyMedia_Select = m_Window.FindName "ContMenu_TVI_DummyMedia_Select" :?> MenuItem
    let m_ContMenu_TVI_DummyMedia_Delete = m_Window.FindName "ContMenu_TVI_DummyMedia_Delete" :?> MenuItem

    // the connection object for the controller, and, the server status object.
    let mutable m_Document : {|
        Conn : CtrlConnection;
        Stat : ServerStatus;
    |} option = None

    /// <summary>
    ///  Target device IDs of the currently running target devices.
    /// </summary>
    /// <remarks>
    ///  This values are cache of the target device running status.
    ///  It is used to simplify the processing to update GUI controlls.
    /// </remarks>
    let mutable m_RunningTDIDs : TDID_T[] = Array.empty

    /// <summary>
    ///  Target group IDs of the currently loaded target groups.
    /// </summary>
    /// <remarks>
    ///  This values are cache of the target group status.
    ///  It is used to simplify the processing to update GUI controlls.
    /// </remarks>
    let m_LoadedTGIDs = new Dictionary< TDID_T, TGID_T[] >()

    /// <summary>
    ///  Target group IDs of the currently activated target groups.
    /// </summary>
    /// <remarks>
    ///  This values are cache of the target group status.
    ///  It is used to simplify the processing to update GUI controlls.
    /// </remarks>
    let m_ActivatedTGIDs = new Dictionary< TDID_T, TGID_T[] >()

    /// <summary>
    ///  Logout event handler.
    /// </summary>
    let m_UnloadEventHandler = new Dictionary< int, ( unit -> unit ) >()

    // Initialize procedure for window.
    do
        // Set controller localized text
        m_Config.SetLocalizedText "MainWindow" m_Window

        m_RequestingProgress.IsIndeterminate <- false
        m_RequestingProgress.Value <- 0.0

        // Register event handler
        m_MenuLogin.Click.AddHandler this.OnClick_MenuLogin
        m_MenuLogout.Click.AddHandler this.OnClick_MenuLogout
        m_MenuReload.Click.AddHandler this.OnClick_MenuReload
        m_MenuExit.Click.AddHandler this.OnClick_MenuExit
        m_MenuVerify.Click.AddHandler this.OnClick_MenuVerify
        m_MenuPulish.Click.AddHandler this.OnClick_MenuPulish
        m_MenuCreateMediaFile.Click.AddHandler this.OnClick_CreateMediaFile
        m_MenuAbout.Click.AddHandler this.OnClick_MenuAbout

        m_PropGrid.SizeChanged.AddHandler ( fun _ e -> this.OnSizeChanged_PropGrid e )
        m_VerifyResultList.MouseDoubleClick.AddHandler ( fun _ e -> this.OnDoubleClick_VerifyResultList() )
        m_ContMenu_TVI_Controller.Opened.AddHandler this.OnOpened_ContMenu_TVI_Controller
        m_ContMenu_TVI_Controller.Closed.AddHandler this.OnClosed_ContMenu
        m_ContMenu_TVI_Controller_Select.Click.AddHandler this.OnClicked_ContMenu_TVI_Select
        m_ContMenu_TVI_Controller_Logout.Click.AddHandler this.OnClick_MenuLogout
        m_ContMenu_TVI_Controller_Reload.Click.AddHandler this.OnClick_MenuReload
        m_ContMenu_TVI_Controller_ADDTargetDevice.Click.AddHandler this.OnClicked_ContMenu_TVI_Controller_ADDTargetDevice
        m_ContMenu_TVI_Controller_Paste.Click.AddHandler this.OnClicked_ContMenu_TVI_Controller_Paste
        m_ContMenu_TVI_TargetDevice.Opened.AddHandler this.OnOpened_ContMenu_TVI_TargetDevice
        m_ContMenu_TVI_TargetDevice.Closed.AddHandler this.OnClosed_ContMenu
        m_ContMenu_TVI_TargetDevice_Select.Click.AddHandler this.OnClicked_ContMenu_TVI_Select
        m_ContMenu_TVI_TargetDevice_Start.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetDevice_Start
        m_ContMenu_TVI_TargetDevice_Stop.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetDevice_Stop
        m_ContMenu_TVI_TargetDevice_AddTargetGroup.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetDevice_AddTargetGroup
        m_ContMenu_TVI_TargetDevice_AddNetworkPortal.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetDevice_AddNetworkPortal
        m_ContMenu_TVI_TargetDevice_Copy.Click.AddHandler ( this.OnClicked_ContMenu_TVI_Copy ClientConst.CB_FORMAT_TargetDevice true )
        m_ContMenu_TVI_TargetDevice_Paste.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetDevice_Paste
        m_ContMenu_TVI_TargetDevice_Delete.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetDevice_Delete
        m_ContMenu_TVI_TargetGroup.Opened.AddHandler this.OnOpened_ContMenu_TVI_TargetGroup
        m_ContMenu_TVI_TargetGroup.Closed.AddHandler this.OnClosed_ContMenu
        m_ContMenu_TVI_TargetGroup_Select.Click.AddHandler this.OnClicked_ContMenu_TVI_Select
        m_ContMenu_TVI_TargetGroup_Activate.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetGroup_Activate
        m_ContMenu_TVI_TargetGroup_Load.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetGroup_Load
        m_ContMenu_TVI_TargetGroup_Inactivate.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetGroup_Inactivate
        m_ContMenu_TVI_TargetGroup_Unload.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetGroup_Unload
        m_ContMenu_TVI_TargetGroup_AddTarget.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetGroup_AddTarget
        m_ContMenu_TVI_TargetGroup_AddBlockDeviceLU.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetGroup_AddBlockDeviceLU
        m_ContMenu_TVI_TargetGroup_Copy.Click.AddHandler ( this.OnClicked_ContMenu_TVI_Copy ClientConst.CB_FORMAT_TargetGroup true )
        m_ContMenu_TVI_TargetGroup_Paste.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetGroup_Paste
        m_ContMenu_TVI_TargetGroup_Delete.Click.AddHandler this.OnClicked_ContMenu_TVI_TargetGroup_Delete
        m_ContMenu_TVI_NetworkPortal.Opened.AddHandler this.OnOpened_ContMenu_TVI_NetworkPortal
        m_ContMenu_TVI_NetworkPortal.Closed.AddHandler this.OnClosed_ContMenu
        m_ContMenu_TVI_NetworkPortal_Select.Click.AddHandler this.OnClicked_ContMenu_TVI_Select
        m_ContMenu_TVI_NetworkPortal_Copy.Click.AddHandler ( this.OnClicked_ContMenu_TVI_Copy ClientConst.CB_FORMAT_NetworkPortal false )
        m_ContMenu_TVI_NetworkPortal_Delete.Click.AddHandler this.OnClicked_ContMenu_TVI_NetworkPortal_Delete
        m_ContMenu_TVI_Target.Opened.AddHandler this.OnOpened_ContMenu_TVI_Target
        m_ContMenu_TVI_Target.Closed.AddHandler this.OnClosed_ContMenu
        m_ContMenu_TVI_Target_Select.Click.AddHandler this.OnClicked_ContMenu_TVI_Select
        m_ContMenu_TVI_Target_Copy.Click.AddHandler ( this.OnClicked_ContMenu_TVI_Copy ClientConst.CB_FORMAT_Target false )
        m_ContMenu_TVI_Target_Delete.Click.AddHandler this.OnClicked_ContMenu_TVI_Target_Delete
        m_ContMenu_TVI_BlockDeviceLU.Opened.AddHandler this.OnOpened_ContMenu_TVI_BlockDeviceLU
        m_ContMenu_TVI_BlockDeviceLU.Closed.AddHandler this.OnClosed_ContMenu
        m_ContMenu_TVI_BlockDeviceLU_Select.Click.AddHandler this.OnClicked_ContMenu_TVI_Select
        m_ContMenu_TVI_BlockDeviceLU_AddPlainFileMedia.Click.AddHandler this.OnClicked_ContMenu_TVI_BlockDeviceLU_AddPlainFileMedia
        m_ContMenu_TVI_BlockDeviceLU_AddMemBufferMedia.Click.AddHandler this.OnClicked_ContMenu_TVI_BlockDeviceLU_AddMemBufferMedia
        m_ContMenu_TVI_BlockDeviceLU_AddDebugMedia.Click.AddHandler this.OnClicked_ContMenu_TVI_BlockDeviceLU_AddDebugMedia
        m_ContMenu_TVI_BlockDeviceLU_Copy.Click.AddHandler ( this.OnClicked_ContMenu_TVI_Copy ClientConst.CB_FORMAT_BlockDeviceLU true )
        m_ContMenu_TVI_BlockDeviceLU_Paste.Click.AddHandler this.OnClicked_ContMenu_TVI_BlockDeviceLU_Paste
        m_ContMenu_TVI_BlockDeviceLU_Delete.Click.AddHandler this.OnClicked_ContMenu_TVI_Target_Delete  // Same as the prodecure of to delete target node
        m_ContMenu_TVI_DummyDeviceLU.Opened.AddHandler this.OnOpened_ContMenu_TVI_DummyDeviceLU
        m_ContMenu_TVI_DummyDeviceLU.Closed.AddHandler this.OnClosed_ContMenu
        m_ContMenu_TVI_DummyDeviceLU_Select.Click.AddHandler this.OnClicked_ContMenu_TVI_Select
        m_ContMenu_TVI_DummyDeviceLU_Delete.Click.AddHandler this.OnClicked_ContMenu_TVI_Target_Delete  // Same as the prodecure of to delete target node
        m_ContMenu_TVI_PlainFileMedia.Opened.AddHandler this.OnOpened_ContMenu_TVI_PlainFileMedia
        m_ContMenu_TVI_PlainFileMedia.Closed.AddHandler this.OnClosed_ContMenu
        m_ContMenu_TVI_PlainFileMedia_Select.Click.AddHandler this.OnClicked_ContMenu_TVI_Select
        m_ContMenu_TVI_PlainFileMedia_Copy.Click.AddHandler ( this.OnClicked_ContMenu_TVI_Copy ClientConst.CB_FORMAT_PlainFileMedia true )
        m_ContMenu_TVI_PlainFileMedia_Delete.Click.AddHandler this.OnClicked_ContMenu_TVI_Target_Delete // Same as the prodecure of to delete target node
        m_ContMenu_TVI_MemBufferMedia.Opened.AddHandler this.OnOpened_ContMenu_TVI_MemBufferMedia
        m_ContMenu_TVI_MemBufferMedia.Closed.AddHandler this.OnClosed_ContMenu
        m_ContMenu_TVI_MemBufferMedia_Select.Click.AddHandler this.OnClicked_ContMenu_TVI_Select
        m_ContMenu_TVI_MemBufferMedia_Copy.Click.AddHandler ( this.OnClicked_ContMenu_TVI_Copy ClientConst.CB_FORMAT_MemBufferMedia true )
        m_ContMenu_TVI_MemBufferMedia_Delete.Click.AddHandler this.OnClicked_ContMenu_TVI_Target_Delete // Same as the prodecure of to delete target node
        m_ContMenu_TVI_DummyMedia.Opened.AddHandler this.OnOpened_ContMenu_TVI_DummyMedia
        m_ContMenu_TVI_DummyMedia.Closed.AddHandler this.OnClosed_ContMenu
        m_ContMenu_TVI_DummyMedia_Select.Click.AddHandler this.OnClicked_ContMenu_TVI_Select
        m_ContMenu_TVI_DummyMedia_Delete.Click.AddHandler this.OnClicked_ContMenu_TVI_Target_Delete     // Same as the prodecure of to delete target node
        m_ContMenu_TVI_DebugMedia.Opened.AddHandler this.OnOpened_ContMenu_TVI_DebugMedia
        m_ContMenu_TVI_DebugMedia.Closed.AddHandler this.OnClosed_ContMenu
        m_ContMenu_TVI_DebugMedia_Select.Click.AddHandler this.OnClicked_ContMenu_TVI_Select
        m_ContMenu_TVI_DebugMedia_AddPlainFileMedia.Click.AddHandler this.OnClicked_ContMenu_TVI_DebugMedia_AddPlainFileMedia
        m_ContMenu_TVI_DebugMedia_AddMemBufferMedia.Click.AddHandler this.OnClicked_ContMenu_TVI_DebugMedia_AddMemBufferMedia
        m_ContMenu_TVI_DebugMedia_AddDebugMedia.Click.AddHandler this.OnClicked_ContMenu_TVI_DebugMedia_AddDebugMedia
        m_ContMenu_TVI_DebugMedia_Copy.Click.AddHandler ( this.OnClicked_ContMenu_TVI_Copy ClientConst.CB_FORMAT_DebugMedia true )
        m_ContMenu_TVI_DebugMedia_Paste.Click.AddHandler this.OnClicked_ContMenu_TVI_DebugMedia_Paste
        m_ContMenu_TVI_DebugMedia_Delete.Click.AddHandler this.OnClicked_ContMenu_TVI_Target_Delete // Same as the prodecure of to delete target node
        m_Window.Closing.AddHandler this.OnClosing

        // Set window size
        m_Window.Width <- UserOpeStat.MainWindowWidth
        m_Window.Height <- UserOpeStat.MainWindowHeight

        // set initial status
        this.UpdateForUnloaded()

    ///////////////////////////////////////////////////////////////////////////
    // IMainWindowIFForPP interface

    interface IMainWindowIFForPP with

        // Notify of updated configure node status.
        override this.NoticeUpdateStat ( node : IConfigureNode ) : unit =
            let ss = m_Document.Value.Stat

            // Get target device and target group status
            let runningTD = ( this :> IMainWindowIFForPP ).GetRunningTDIDs()
            let loadedTG = ( this :> IMainWindowIFForPP ).GetLoadedTGIDs()
            let activeTG = ( this :> IMainWindowIFForPP ).GetActivatedTGIDs()

            // update Target Device node icon function
            let updateTargetDeviceIcon ( tdNode : ConfNode_TargetDevice ) ( tdTVI : TreeViewItem ) =
                let actived = Seq.exists ( (=) tdNode.TargetDeviceID ) runningTD
                let modified = ( tdNode :> IConfigFileNode ).Modified
                if modified = ModifiedStatus.Modified then
                    this.UpdateTreeViewItemIcon tdTVI IconImageIndex.III_TARGET_DEVICE_MODIFIED
                elif actived then
                    this.UpdateTreeViewItemIcon tdTVI IconImageIndex.III_TARGET_DEVICE_RUNNING
                else
                    this.UpdateTreeViewItemIcon tdTVI IconImageIndex.III_TARGET_DEVICE_UNLOADED

            // update Target Group node icon function
            let updateTargetGroupIcon ( tgNode : ConfNode_TargetGroup ) ( tgTVI : TreeViewItem ) : unit =
                let tdn = ss.GetAncestorTargetDevice tgNode
                let tdActived = Seq.exists ( (=) tdn.Value.TargetDeviceID ) runningTD
                let isModified = ( tgNode :> IConfigFileNode ).Modified = ModifiedStatus.Modified
                let tgActived =
                    let r, v = activeTG.TryGetValue tdn.Value.TargetDeviceID
                    if r then
                        Seq.exists ( (=) tgNode.TargetGroupID ) v
                    else
                        false
                let tgLoaded =
                    let r, v = loadedTG.TryGetValue tdn.Value.TargetDeviceID
                    if r then
                        Seq.exists ( (=) tgNode.TargetGroupID ) v
                    else
                        false
                if isModified then
                    this.UpdateTreeViewItemIcon tgTVI IconImageIndex.III_TARGET_GROUP_MODIFIED
                elif not tdActived then
                    this.UpdateTreeViewItemIcon tgTVI IconImageIndex.III_TARGET_GROUP_UNLOADED
                elif not tgLoaded then
                    this.UpdateTreeViewItemIcon tgTVI IconImageIndex.III_TARGET_GROUP_UNLOADED
                elif not tgActived then
                    this.UpdateTreeViewItemIcon tgTVI IconImageIndex.III_TARGET_GROUP_LOADED
                else
                    this.UpdateTreeViewItemIcon tgTVI IconImageIndex.III_TARGET_GROUP_ACTIVE

            match node with
            | :? ConfNode_Controller as x ->
                // update controller node icon
                let cntvi = this.SearchTreeViewItemFromConfigureNode node
                let modified = ( x :> IConfigFileNode ).Modified = ModifiedStatus.Modified
                if modified then
                    this.UpdateTreeViewItemIcon cntvi.Value IconImageIndex.III_CONTROLLER_MODIFIED
                else
                    this.UpdateTreeViewItemIcon cntvi.Value IconImageIndex.III_CONTROLLER

            | :? ConfNode_TargetDevice as x ->
                let tdtvi = this.SearchTreeViewItemFromConfigureNode node
                updateTargetDeviceIcon x tdtvi.Value

                // Update target group icon
                for itr in ( x :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>() do
                    updateTargetGroupIcon itr ( this.SearchTreeViewItemFromConfigureNode itr ).Value

            | :? ConfNode_NetworkPortal as x ->
                let tdn = ss.GetAncestorTargetDevice x
                let tdtvi = this.SearchTreeViewItemFromConfigureNode tdn.Value
                updateTargetDeviceIcon tdn.Value tdtvi.Value

            | :? ConfNode_TargetGroup as x ->
                let tgtvi = this.SearchTreeViewItemFromConfigureNode node
                updateTargetGroupIcon x tgtvi.Value

            | :? ConfNode_Target
            | :? ConfNode_BlockDeviceLU
            | :? ConfNode_DummyDeviceLU
            | :? ConfNode_PlainFileMedia
            | :? ConfNode_MemBufferMedia
            | :? ConfNode_DummyMedia
            | :? ConfNode_DebugMedia as x ->
                let tgn = ss.GetAncestorTargetGroup x
                let tdtvi = this.SearchTreeViewItemFromConfigureNode tgn.Value
                updateTargetGroupIcon tgn.Value tdtvi.Value

            | _ -> ()   // ignore

        // Notify of updated configure node status.
        override this.NoticeUpdateConfig ( node : IConfigureNode ) : unit =
            // When the configuration is updated, the tree view item icon also must be updated.
            ( this :> IMainWindowIFForPP ).NoticeUpdateStat node

            // When the configuration is updated, the tree view item label also must be updated.
            let tvi = this.SearchTreeViewItemFromConfigureNode node
            let nodeText = node.MinDescriptString
            this.UpdateTreeViewItemLabel tvi.Value nodeText

            // When the configuration is updated, siblings of the specified node must be re-ordered.
            match node with
            | :? ConfNode_Controller ->
                // There is only one the controller node, nothing to do.
                ()
            | _ ->
                let ptvi = tvi.Value.Parent :?> TreeViewItem
                let ss = m_Document.Value.Stat
                [|
                    for i in ptvi.Items -> ( i :?> TreeViewItem )
                    ptvi.Items.Clear()
                |]
                |> Array.sortWith ( fun a b ->
                    let anode = a.Tag :?> uint64 |> confnode_me.fromPrim |> ss.GetNode
                    let bnode = b.Tag :?> uint64 |> confnode_me.fromPrim |> ss.GetNode
                    Functions.CompareMultiLevelKey anode.SortKey bnode.SortKey
                )
                |> Array.iter ( ptvi.Items.Add >> ignore )

                tvi.Value.IsSelected <- true

        // Get effective server status object.
        override _.GetServerStatus() : ServerStatus option =
            if m_Document.IsSome then
                Some m_Document.Value.Stat
            else
                None

        // Get effective connection object.
        override _.GetCtrlConnection() : CtrlConnection option =
            if m_Document.IsSome then
                Some m_Document.Value.Conn
            else
                None

        // Run the task that queries the controller.
        override this.ProcCtrlQuery ( setProg : bool ) ( f1 : unit -> Task<unit> ) : unit =
            if setProg then
                ( this :> IMainWindowIFForPP ).SetProgress true
            m_BKTaskQueue.Enqueue ( fun () -> task {
                try
                    do! f1()
                with
                | :? RequestError as x ->
                    m_Window.Dispatcher.InvokeAsync ( fun () ->
                        let title = m_Config.MessagesText.GetMessage( "MSG_MSGBOX_TITLE" )
                        MessageBox.Show( x.Message, title, MessageBoxButton.OK, MessageBoxImage.Warning ) |> ignore
                        if setProg then
                            ( this :> IMainWindowIFForPP ).SetProgress false
                    ) |> ignore
                | _ as x ->
                    m_Window.Dispatcher.InvokeAsync ( fun () ->
                        let title = m_Config.MessagesText.GetMessage( "MSG_MSGBOX_TITLE" )
                        MessageBox.Show( x.Message, title, MessageBoxButton.OK, MessageBoxImage.Warning ) |> ignore
                        this.UpdateForUnloaded()
                        if setProg then
                            ( this :> IMainWindowIFForPP ).SetProgress false
                    ) |> ignore
            })

        // logout
        override this.Logout () : unit =
            this.UpdateForUnloaded()

        // register logout event handler
        override this.SubscribeLogoutEvent ( e : ( unit -> unit ) ) : int =
            let n = Functions.GenUniqueNumber<int> ( (+) 1 ) 0 m_UnloadEventHandler.Keys
            m_UnloadEventHandler.Add( n, e )
            n

        // unregister logout event handler
        override this.UnsubscribeLogoutEvent ( idx : int ) : unit =
            m_UnloadEventHandler.Remove idx |> ignore

        // Set progress bar to enable or disable.
        override _.SetProgress ( f : bool ) : unit =
            m_Window.IsEnabled <- not f
            m_RequestingProgress.IsIndeterminate <- f

        // Update target device and target group running status cache.
        override _.UpdateRunningStatus() : Task<unit> =
            task {
                if m_Document.IsNone then
                    // clear all of cache information
                    m_RunningTDIDs <- Array.empty
                    m_LoadedTGIDs.Clear()
                    m_ActivatedTGIDs.Clear()
                else
                    let cc = m_Document.Value.Conn

                    // Get running target device IDs
                    let! runningTDlist = cc.GetTargetDeviceProcs()
                    m_RunningTDIDs <- List.toArray runningTDlist

                    // get loaded target group IDs
                    m_LoadedTGIDs.Clear()
                    for itrtd in m_RunningTDIDs do
                        let! w = cc.GetLoadedTargetGroups itrtd
                        let tglist = w |> Seq.map _.ID |> Seq.toArray
                        m_LoadedTGIDs.Add( itrtd, tglist )

                    // get active target group IDs.
                    m_ActivatedTGIDs.Clear()
                    for itrtd in m_RunningTDIDs do
                        let! w = cc.GetActiveTargetGroups itrtd
                        let tglist = w |> Seq.map _.ID |> Seq.toArray
                        m_ActivatedTGIDs.Add( itrtd, tglist )
            }

        // Get cached running status of the target devices.
        override _.GetRunningTDIDs() : TDID_T[] =
            m_RunningTDIDs

        // Get cached target group IDs that is currently loaded target groups.
        override _.GetLoadedTGIDs() : Dictionary< TDID_T, TGID_T[] > =
            m_LoadedTGIDs

        // Get cached target group IDs that is currently activated target groups.
        override _.GetActivatedTGIDs() : Dictionary< TDID_T, TGID_T[] > =
            m_ActivatedTGIDs

        // Get specified target device is active and target group is loaded or not.
        override _.IsTGLoaded ( tdid : TDID_T ) ( tgid : TGID_T ) : struct( bool * bool ) =
            let tdActived =
                Array.exists ( (=) tdid ) m_RunningTDIDs
            let tgLoaded =
                if not tdActived then
                    false
                else
                    match m_LoadedTGIDs.TryGetValue tdid with
                    | true, v ->
                        Array.exists ( (=) tgid ) v
                    | false, _ ->
                        false
            struct( tdActived, tgLoaded )


    ///////////////////////////////////////////////////////////////////////////
    // Public method

    /// <summary>
    ///  Display the window
    /// </summary>
    /// <param name="apl">
    ///  The application class object.
    /// </param>
    /// <remarks>
    ///  This method will not return until the window is closed.
    /// </remarks>
    member _.Show ( apl : Application ) : int =
        apl.ShutdownMode <- ShutdownMode.OnMainWindowClose
        apl.Run m_Window

    ///////////////////////////////////////////////////////////////////////////
    // Event handler

    /// <summary>
    ///  "Login" menu was selected.
    /// </summary>
    member private this.OnClick_MenuLogin( sender : obj ) ( e : RoutedEventArgs ) : unit =

        if m_Document.IsNone then
            let d = LoginDialog( m_Config )
            let r = d.Show()
            match r with
            | DialogResult.Cancel ->
                ()
            | DialogResult.Ok( cc ) ->
                m_Window.IsEnabled <- false
                m_BKTaskQueue.Enqueue ( fun () -> task {
                    try
                        // load all configurations
                        let ss = new ServerStatus( m_Config.ClientCLIText )
                        do! ss.LoadConfigure cc false
                        m_Document <- Some {|
                            Conn = cc;
                            Stat = ss;
                        |}

                        // Get target device and target group status
                        do! ( this :> IMainWindowIFForPP ).UpdateRunningStatus()

                        // update controller
                        m_Window.Dispatcher.InvokeAsync ( fun () ->
                            this.UpdateForLoaded ss m_RunningTDIDs m_LoadedTGIDs m_ActivatedTGIDs
                            m_Window.IsEnabled <- true
                        ) |> ignore
                    with
                    | _ as x ->
                        m_Document <- None
                        m_Window.Dispatcher.InvokeAsync ( fun () ->
                            let title = m_Config.MessagesText.GetMessage( "MSG_MSGBOX_TITLE" )
                            MessageBox.Show( x.Message, title, MessageBoxButton.OK, MessageBoxImage.Warning ) |> ignore
                            this.UpdateForUnloaded()
                            m_Window.IsEnabled <- true
                        ) |> ignore
                })

    /// <summary>
    ///  "Logoff" menu was selected.
    /// </summary>
    member private this.OnClick_MenuLogout( sender : obj ) ( e : RoutedEventArgs ) : unit =
        let f =
            if m_Document.IsNone then
                false
            elif m_Document.Value.Stat.IsModified then
                let title = m_Config.MessagesText.GetMessage( "MSG_MSGBOX_TITLE" )
                let msg = m_Config.MessagesText.GetMessage( "MSG_ALREADY_LOGGEDIN" )
                let r = MessageBox.Show( msg, title, MessageBoxButton.YesNo, MessageBoxImage.Information )
                ( r = MessageBoxResult.Yes )
            else
                true

        if f then
            m_Window.IsEnabled <- false
            m_BKTaskQueue.Enqueue ( fun () -> task {
                try
                    do! m_Document.Value.Conn.Logout()
                    m_Document.Value.Conn.Dispose()
                    m_Document <- None
                    m_Window.Dispatcher.InvokeAsync ( fun () ->
                        this.UpdateForUnloaded()
                        m_Window.IsEnabled <- true
                    )|> ignore
                with
                | _ as x ->
                    m_Document <- None
                    m_Window.Dispatcher.InvokeAsync ( fun () ->
                        let title = m_Config.MessagesText.GetMessage( "MSG_MSGBOX_TITLE" )
                        MessageBox.Show( x.Message, title, MessageBoxButton.OK, MessageBoxImage.Warning ) |> ignore
                        m_Window.IsEnabled <- true
                    ) |> ignore
            })

    /// <summary>
    ///  "Reload" menu was selected.
    /// </summary>
    member private this.OnClick_MenuReload( sender : obj ) ( e : RoutedEventArgs ) : unit =
        let f =
            if m_Document.IsNone then
                false
            elif m_Document.Value.Stat.IsModified then
                // If the configuration has been modified, query continue or not to the user.
                let title = m_Config.MessagesText.GetMessage( "MSG_MSGBOX_TITLE" )
                let msg = m_Config.MessagesText.GetMessage( "MSG_CONFIGURATION_MODIFIER" )
                MessageBox.Show( msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question ) = MessageBoxResult.Yes
            else
                true

        if f then
            let cc = m_Document.Value.Conn
            ( this :> IMainWindowIFForPP ).ProcCtrlQuery true ( fun () -> task {
                // load all configurations
                let ssnew = new ServerStatus( m_Config.ClientCLIText )
                do! ssnew.LoadConfigure cc false
                m_Document <- Some {|
                    Conn = cc;
                    Stat = ssnew;
                |}

                // Get target device and target group status
                do! ( this :> IMainWindowIFForPP ).UpdateRunningStatus()

                // update controller
                m_Window.Dispatcher.InvokeAsync ( fun () ->
                    this.UpdateForLoaded ssnew m_RunningTDIDs m_LoadedTGIDs m_ActivatedTGIDs
                    ( this :> IMainWindowIFForPP ).SetProgress false
                ) |> ignore
            })

    /// <summary>
    ///  "Exit" menu was selected.
    /// </summary>
    member private _.OnClick_MenuExit( sender : obj ) ( e : RoutedEventArgs ) : unit =
        let f =
            if m_Document.IsNone then
                true
            elif m_Document.Value.Stat.IsModified then
                // If the configuration has been modified, query continue or not to the user.
                let title = m_Config.MessagesText.GetMessage( "MSG_MSGBOX_TITLE" )
                let msg = m_Config.MessagesText.GetMessage( "MSG_CONFIGURATION_MODIFIER" )
                MessageBox.Show( msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question ) = MessageBoxResult.Yes
            else
                true
        if f then exit( 0 )

    /// <summary>
    ///  "Verify" menu was selected.
    /// </summary>
    member private this.OnClick_MenuVerify( sender : obj ) ( e : RoutedEventArgs ) : unit =
        match m_Document with
        | None -> ()
        | Some doc ->
            m_VerifyResultList.Items.Clear()
            let tviNodes =
                this.GetTreeViewItemAndConfigNodePair()
                |> Seq.map KeyValuePair
                |> Dictionary

            let validResult = doc.Stat.Validate()
            for ( nt, msg ) in validResult do
                let node = doc.Stat.GetNode nt
                let tviNodeName =
                    let a = tviNodes.Item nt
                    let b = a.Header :?> StackPanel
                    let c = b.Children.Item 1 :?> TextBlock
                    c.Text
                let lvi = new ListViewItem( Content = {| NodeName = tviNodeName; Message = msg |}, Tag = node )
                m_VerifyResultList.Items.Add lvi |> ignore

    /// <summary>
    ///  "Pulish" menu was selected.
    /// </summary>
    member private this.OnClick_MenuPulish( sender : obj ) ( e : RoutedEventArgs ) : unit =
        this.OnClick_MenuVerify sender e
        if m_Document.IsNone then
            ()  // ignore
        elif m_VerifyResultList.Items.Count > 0 then
            () // ignore
        else
            let ss = m_Document.Value.Stat
            let cc = m_Document.Value.Conn
            ( this :> IMainWindowIFForPP ).ProcCtrlQuery true ( fun () -> task {
                // publish
                do! ss.Publish cc
                m_Window.Dispatcher.InvokeAsync ( fun () ->
                    ( this :> IMainWindowIFForPP ).SetProgress false

                    // update tree view items
                    let ctrlNode = ss.ControllerNode
                    ( this :> IMainWindowIFForPP ).NoticeUpdateStat ctrlNode

                    ( ctrlNode :> IConfigureNode ).GetChildNodes<ConfNode_TargetDevice>()
                    |> List.iter ( this :> IMainWindowIFForPP ).NoticeUpdateStat

                    ( ctrlNode :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                    |> List.iter ( this :> IMainWindowIFForPP ).NoticeUpdateStat

                ) |> ignore
            })

    /// <summary>
    ///  "Pulish" menu was selected.
    /// </summary>
    member private this.OnClick_CreateMediaFile( sender : obj ) ( e : RoutedEventArgs ) : unit =
        if m_Document.IsNone then
            ()  // ignore
        else
            let d = CreateMediaFile( m_Config, this )
            d.Show()

    /// <summary>
    ///  "About" menu was selected.
    /// </summary>
    member private _.OnClick_MenuAbout( sender : obj ) ( e : RoutedEventArgs ) : unit =
        let title = m_Config.MessagesText.GetMessage( "MSG_MSGBOX_TITLE" )
        let msg = m_Config.MessagesText.GetMessage( "MSG_ABOUT", sprintf "%d" Constants.MAJOR_VERSION, sprintf "%d" Constants.MINOR_VERSION, sprintf "%d" Constants.PRODUCT_RIVISION )
        MessageBox.Show( msg, title, MessageBoxButton.OK, MessageBoxImage.Information ) |> ignore

    /// <summary>
    ///  Size of the grid controller for a property page was changed.
    /// </summary>
    /// <param name="e">
    ///  Event object
    /// </param>
    member private _.OnSizeChanged_PropGrid ( e : SizeChangedEventArgs ) : unit =
        match m_PropGrid.Content with
        | :? IPropPageInterface as x ->
            let p = e.PreviousSize
            let n = e.NewSize
            let v = m_PropGrid.ViewportWidth
            let w = v - ( p.Width - n.Width )
            x.SetPageWidth w
        | _ -> ()

    /// <summary>
    ///  Tree view item was selected.
    /// </summary>
    member private this.OnSelect_TreeViewItem ( confnode : IConfigureNode ) : unit =
        this.ShowPropertyPage confnode

    /// <summary>
    ///  Verify Result List was double clicked.
    /// </summary>
    member private this.OnDoubleClick_VerifyResultList() : unit =
        let si = m_VerifyResultList.SelectedIndex
        if si < 0 then
            ()  // ignore
        else
            let sitem = m_VerifyResultList.Items.Item si :?> ListViewItem
            let node = sitem.Tag :?> IConfigureNode
            let tvi = this.SearchTreeViewItemFromConfigureNode node
            tvi.Value.IsSelected <- true
            //this.ShowPropertyPage node

    /// <summary>
    ///  Context menu for the tree view item has closed.
    /// </summary>
    /// <remarks>
    ///  This method is shared for all of context menues for tree view items.
    /// </remarks>
    member private this.OnClosed_ContMenu ( sender : obj ) ( e : RoutedEventArgs ) =
        // Clear the tag value which had been set at opened event.
        let menu = sender :?> ContextMenu
        menu.Tag <- null

    /// <summary>
    ///  Context menu for the tree view item of the controller node has opened.
    /// </summary>
    member private this.OnOpened_ContMenu_TVI_Controller ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let menu = sender :?> ContextMenu
            let confnode = this.GetContextMenuSelectedNode doc.Stat menu
            menu.Tag <- confnode
            let pastFlg = true
            m_ContMenu_TVI_Controller_Paste.IsEnabled <- pastFlg

    /// <summary>
    ///  "Select" item of context menu is clicked.
    /// </summary>
    member private _.OnClicked_ContMenu_TVI_Select ( sender : obj ) ( e : RoutedEventArgs ) =
        let confnode = this.GetContMenu_CurrentNode<IConfigureNode> sender
        let tvi = this.SearchTreeViewItemFromConfigureNode confnode
        if tvi.IsSome then
            tvi.Value.IsSelected <- true

    /// <summary>
    ///  "copy" item of context menu is clicked.
    /// </summary>
    member private _.OnClicked_ContMenu_TVI_Copy ( formatName : string ) ( recursive : bool ) ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let menuitem = sender :?> MenuItem
            let menu = menuitem.Parent :?> ContextMenu
            let confnode = menu.Tag :?> IConfigureNode
            let s = doc.Stat.ExportTemporaryDump confnode.NodeID recursive
            Clipboard.SetData( formatName, s )

    /// <summary>
    ///  "Add target device" item of context menu for the controller node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_Controller_ADDTargetDevice ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<IConfigureNode> sender
            let tvi = this.SearchTreeViewItemFromConfigureNode confnode
            let oldtds = doc.Stat.GetTargetDeviceNodes()

            // gen new TargetDeviceID
            let newTdid = ConfNode_TargetDevice.GenNewTargetDeviceID oldtds

            // gen default name
            let tdName = ConfNode_TargetDevice.GenDefaultTargetDeviceName oldtds

            // create new target device node
            let newNegParam : TargetDeviceConf.T_NegotiableParameters = {
                MaxRecvDataSegmentLength = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
                MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength;
                FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength;
                DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait;
                DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain;
                MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
            }
            let newLogParam : TargetDeviceConf.T_LogParameters = {
                SoftLimit = Constants.LOGPARAM_DEF_SOFTLIMIT;
                HardLimit = Constants.LOGPARAM_DEF_HARDLIMIT;
                LogLevel = LogLevel.LOGLEVEL_INFO;
            }
            let newnode = doc.Stat.AddTargetDeviceNode newTdid tdName newNegParam newLogParam

            // Add tree view item for newly created target device.
            let loadedTG = new Dictionary< TDID_T, TGID_T[] >()
            let activeTG = new Dictionary< TDID_T, TGID_T[] >()
            this.CreateTreeViewItem_TargetDevice doc.Stat Array.empty loadedTG activeTG newnode
            |> this.AddTreeViewItem tvi.Value

    /// <summary>
    ///  "Paste" item of context menu for the controller node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_Controller_Paste ( sender : obj ) ( e : RoutedEventArgs ) =
        if m_Document.IsSome then
            let confnode = this.GetContMenu_CurrentNode<IConfigureNode> sender
            let parenttvi = this.SearchTreeViewItemFromConfigureNode confnode
            let ss = m_Document.Value.Stat
            try
                // Import
                let w = Clipboard.GetData ClientConst.CB_FORMAT_TargetDevice |> string
                let newtdnode = ( ss.ImportTemporaryDump w ss.ControllerNodeID true ) :?> ConfNode_TargetDevice

                // gen new TargetDeviceID
                let oldtds = ss.GetTargetDeviceNodes()
                let newTdid = ConfNode_TargetDevice.GenNewTargetDeviceID oldtds

                // Renumber TargetDeviceID to make it unique.
                let newtdnode2 = ss.UpdateTargetDeviceNode newtdnode newTdid newtdnode.TargetDeviceName newtdnode.NegotiableParameters newtdnode.LogParameters

                let dummy = Dictionary< TDID_T, TGID_T[] >()
                let tvi = this.CreateTreeViewItem_TargetDevice ss Array.empty dummy dummy newtdnode2
                this.AddTreeViewItem parenttvi.Value tvi
            with
            | _ -> ()

    /// <summary>
    ///  Context menu for the tree view item of the target device node has opened.
    /// </summary>
    member private this.OnOpened_ContMenu_TVI_TargetDevice ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let menu = sender :?> ContextMenu
            let confnode = this.GetContextMenuSelectedNode doc.Stat menu
            menu.Tag <- confnode
            let tdid = ( confnode :?> ConfNode_TargetDevice ).TargetDeviceID
            let actived = Seq.exists ( (=) tdid ) m_RunningTDIDs
            let isModified = ( confnode :?> IConfigFileNode ).Modified = ModifiedStatus.Modified

            // set menu item status
            m_ContMenu_TVI_TargetDevice_Start.IsEnabled <- ( not actived ) && ( not isModified )
            m_ContMenu_TVI_TargetDevice_Stop.IsEnabled <- actived && ( not isModified )
            m_ContMenu_TVI_TargetDevice_AddTargetGroup.IsEnabled <- true
            m_ContMenu_TVI_TargetDevice_AddNetworkPortal.IsEnabled <- ( not actived ) || isModified

            if Clipboard.ContainsData ClientConst.CB_FORMAT_NetworkPortal then
                m_ContMenu_TVI_TargetDevice_Paste.IsEnabled <- ( not actived ) || isModified
            elif Clipboard.ContainsData ClientConst.CB_FORMAT_TargetGroup then
                m_ContMenu_TVI_TargetDevice_Paste.IsEnabled <- true
            else
                m_ContMenu_TVI_TargetDevice_Paste.IsEnabled <- false

            m_ContMenu_TVI_TargetDevice_Delete.IsEnabled <- ( not actived ) || isModified

    /// <summary>
    ///  "start" item of context menu for the target device node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetDevice_Start ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetDevice> sender

            ( this :> IMainWindowIFForPP ).ProcCtrlQuery true ( fun () -> task {
                // Start selected target device
                do! doc.Conn.StartTargetDeviceProc confnode.TargetDeviceID

                // update running status cache
                do! ( this :> IMainWindowIFForPP ).UpdateRunningStatus()

                // set menu item status
                m_Window.Dispatcher.InvokeAsync ( fun () ->
                    ( this :> IMainWindowIFForPP ).NoticeUpdateStat confnode
                    ( m_PropGrid.Content :?> IPropPageInterface ).UpdateViewContent()
                    ( this :> IMainWindowIFForPP ).SetProgress false
                ) |> ignore
            })

    /// <summary>
    ///  "stop" item of context menu for the target device node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetDevice_Stop ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetDevice> sender

            ( this :> IMainWindowIFForPP ).ProcCtrlQuery true ( fun () -> task {
                // Stop selected target device
                do! doc.Conn.KillTargetDeviceProc confnode.TargetDeviceID

                // update running status cache
                do! ( this :> IMainWindowIFForPP ).UpdateRunningStatus()

                // set menu item status
                m_Window.Dispatcher.InvokeAsync ( fun () ->
                    ( this :> IMainWindowIFForPP ).NoticeUpdateStat confnode
                    ( m_PropGrid.Content :?> IPropPageInterface ).UpdateViewContent()
                    ( this :> IMainWindowIFForPP ).SetProgress false
                ) |> ignore
            })

    /// <summary>
    ///  "Add target group" item of context menu for the target device node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetDevice_AddTargetGroup ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetDevice> sender
            let tvi = this.SearchTreeViewItemFromConfigureNode confnode
            let oldtgs = ( confnode :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()

            // gen new TargetGroupID
            let newTgid = ConfNode_TargetGroup.GenNewTargetGroupID oldtgs

            // gen default name
            let tgName = ConfNode_TargetGroup.GenDefaultTargetGroupName oldtgs

            // create target group node
            let newnode = doc.Stat.AddTargetGroupNode confnode newTgid tgName true

            // Add tree view item for newly created target group node.
            this.CreateTreeViewItem_TargetGroup doc.Stat Array.empty Array.empty newnode
            |> this.AddTreeViewItem tvi.Value

    /// <summary>
    ///  "Add network portal" item of context menu for the target device node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetDevice_AddNetworkPortal ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetDevice> sender
            let tvi = this.SearchTreeViewItemFromConfigureNode confnode

            // gen ident number
            let newIdent =
                ( confnode :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                |> ConfNode_NetworkPortal.GenNewID

            let conf : TargetDeviceConf.T_NetworkPortal = {
                IdentNumber = newIdent;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetAddress = "";
                PortNumber = Constants.DEFAULT_ISCSI_PORT_NUM;
                DisableNagle = Constants.DEF_DISABLE_NAGLE_IN_NP;
                ReceiveBufferSize = Constants.DEF_RECEIVE_BUFFER_SIZE_IN_NP;
                SendBufferSize = Constants.DEF_SEND_BUFFER_SIZE_IN_NP;
                WhiteList = [];
            }

            // create new network portal node
            let newnode = doc.Stat.AddNetworkPortalNode confnode conf

            // Add tree view item for newly created network portal node.
            this.CreateTreeViewItem_NetworkPortal newnode
            |> this.AddTreeViewItem tvi.Value

            // Update tree view icon
            doc.Stat.GetNode ( confnode :> IConfigureNode ).NodeID
            |> ( this :> IMainWindowIFForPP ).NoticeUpdateStat

    /// <summary>
    ///  "Paste" item of context menu for the target device node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetDevice_Paste ( sender : obj ) ( e : RoutedEventArgs ) =
        if m_Document.IsSome then
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetDevice> sender
            let parenttvi = this.SearchTreeViewItemFromConfigureNode confnode
            let ss = m_Document.Value.Stat

            if Clipboard.ContainsData ClientConst.CB_FORMAT_NetworkPortal then
                // Generate new network portal ID
                let newIdent =
                    ( confnode :> IConfigureNode ).GetDescendantNodes<ConfNode_NetworkPortal>()
                    |> ConfNode_NetworkPortal.GenNewID

                // Import
                let w = Clipboard.GetData ClientConst.CB_FORMAT_NetworkPortal |> string
                let npnode = ( ss.ImportTemporaryDump w ( confnode :> IConfigureNode ).NodeID false ) :?> ConfNode_NetworkPortal

                // update ID
                let conf = {
                    npnode.NetworkPortal with
                        IdentNumber = newIdent;
                }
                let npnode2 = ss.UpdateNetworkPortalNode npnode conf

                // Add tree view item for newly created network portal node.
                this.CreateTreeViewItem_NetworkPortal npnode2
                |> this.AddTreeViewItem parenttvi.Value

                // Update tree view icon
                ss.GetNode ( confnode :> IConfigureNode ).NodeID
                |> ( this :> IMainWindowIFForPP ).NoticeUpdateStat

            elif Clipboard.ContainsData ClientConst.CB_FORMAT_TargetGroup then

                let oldTGNodes = ( confnode :> IConfigureNode ).GetDescendantNodes<ConfNode_TargetGroup>()
                let oldTNodes =  ( confnode :> IConfigureNode ).GetDescendantNodes<ConfNode_Target>()
                let oldMediaNodes = ( confnode :> IConfigureNode ).GetDescendantNodes<IMediaNode>()

                // Import
                let w = Clipboard.GetData ClientConst.CB_FORMAT_TargetGroup |> string
                let tgnode = ( ss.ImportTemporaryDump w ( confnode :> IConfigureNode ).NodeID true ) :?> ConfNode_TargetGroup

                // Update target group ID
                let newtgid = ConfNode_TargetGroup.GenNewTargetGroupID oldTGNodes
                let tgnode2 = ss.UpdateTargetGroupNode tgnode newtgid tgnode.TargetGroupName tgnode.EnabledAtStart 

                // update all media node ID
                this.RenumberMediaNodes ss oldMediaNodes tgnode2

                // update all target node ID
                ( tgnode2 :> IConfigureNode ).GetChildNodes<ConfNode_Target>()
                |> Seq.fold ( fun statTNodes itr ->
                    let conf = {
                        itr.Values with
                            IdentNumber = statTNodes |> ConfNode_Target.GenNewID
                    }
                    let newNode = ss.UpdateTargetNode itr conf
                    newNode :: statTNodes
                ) oldTNodes
                |> ignore

                // Add tree view item for newly created target group node.
                this.CreateTreeViewItem_TargetGroup ss Array.empty Array.empty tgnode2
                |> this.AddTreeViewItem parenttvi.Value

            else
                ()

    /// <summary>
    ///  "Delete" item of context menu for the target device node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetDevice_Delete ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetDevice> sender
            let tvi = this.SearchTreeViewItemFromConfigureNode confnode

            // Delete current target device node
            doc.Stat.DeleteTargetDeviceNode confnode

            // Remove tree view item.
            this.RemoveTreeViewItem tvi.Value

    /// <summary>
    ///  Context menu for the tree view item of the target group node has opened.
    /// </summary>
    member private this.OnOpened_ContMenu_TVI_TargetGroup ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let menu = sender :?> ContextMenu
            let confnode = this.GetContextMenuSelectedNode doc.Stat menu
            menu.Tag <- confnode
            let tdnode = doc.Stat.GetAncestorTargetDevice confnode
            let tgid = ( confnode :?> ConfNode_TargetGroup ).TargetGroupID
            let tdid = tdnode.Value.TargetDeviceID

            let tdActived = Seq.exists ( (=) tdid ) m_RunningTDIDs
            let tgActived =
                match m_ActivatedTGIDs.TryGetValue tdid with
                | true, v ->
                    Seq.exists ( (=) tgid ) v
                | false, _ ->
                    false
            let tgLoaded =
                match m_LoadedTGIDs.TryGetValue tdid with
                | true, v ->
                    Seq.exists ( (=) tgid ) v
                | false, _ ->
                    false
            let isModified = ( confnode :?> IConfigFileNode ).Modified = ModifiedStatus.Modified

            let editable = ( isModified || not tdActived || not tgLoaded )
            m_ContMenu_TVI_TargetGroup_AddTarget.IsEnabled <- editable
            m_ContMenu_TVI_TargetGroup_AddBlockDeviceLU.IsEnabled <- editable
            m_ContMenu_TVI_TargetGroup_Delete.IsEnabled <- editable

            let pastFlg = Clipboard.ContainsData ClientConst.CB_FORMAT_Target || Clipboard.ContainsData ClientConst.CB_FORMAT_BlockDeviceLU
            m_ContMenu_TVI_TargetGroup_Paste.IsEnabled <- pastFlg && editable

            if isModified then
                // Target Group configuration is modified
                m_ContMenu_TVI_TargetGroup_Activate.IsEnabled <- false
                m_ContMenu_TVI_TargetGroup_Load.IsEnabled <- false
                m_ContMenu_TVI_TargetGroup_Inactivate.IsEnabled <- false
                m_ContMenu_TVI_TargetGroup_Unload.IsEnabled <- false

            elif not tdActived then
                // Target Device is lot loaded
                m_ContMenu_TVI_TargetGroup_Activate.IsEnabled <- false
                m_ContMenu_TVI_TargetGroup_Load.IsEnabled <- false
                m_ContMenu_TVI_TargetGroup_Inactivate.IsEnabled <- false
                m_ContMenu_TVI_TargetGroup_Unload.IsEnabled <- false

            elif not tgLoaded then
                // Target Group is unloaded
                m_ContMenu_TVI_TargetGroup_Activate.IsEnabled <- true
                m_ContMenu_TVI_TargetGroup_Load.IsEnabled <- true
                m_ContMenu_TVI_TargetGroup_Inactivate.IsEnabled <- false
                m_ContMenu_TVI_TargetGroup_Unload.IsEnabled <- false

            elif not tgActived then
                // Target Group is loaded but not activated
                m_ContMenu_TVI_TargetGroup_Activate.IsEnabled <- true
                m_ContMenu_TVI_TargetGroup_Load.IsEnabled <- false
                m_ContMenu_TVI_TargetGroup_Inactivate.IsEnabled <- false
                m_ContMenu_TVI_TargetGroup_Unload.IsEnabled <- true

            else
                // Target Group is activated
                m_ContMenu_TVI_TargetGroup_Activate.IsEnabled <- false
                m_ContMenu_TVI_TargetGroup_Load.IsEnabled <- false
                m_ContMenu_TVI_TargetGroup_Inactivate.IsEnabled <- true
                m_ContMenu_TVI_TargetGroup_Unload.IsEnabled <- true

    /// <summary>
    ///  "Activate" item of context menu for the target group node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetGroup_Activate ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetGroup> sender
            let tdnode = doc.Stat.GetAncestorTargetDevice confnode
            let tgid = confnode.TargetGroupID
            let tdid = tdnode.Value.TargetDeviceID

            ( this :> IMainWindowIFForPP ).ProcCtrlQuery true ( fun () -> task {
                // If the target group is not loaded, load this target group.
                let! loadedTGs = doc.Conn.GetLoadedTargetGroups tdid
                let loaded =
                    loadedTGs
                    |> Seq.map _.ID
                    |> Seq.exists ( (=) tgid ) 
                if not loaded then
                    do! doc.Conn.LoadTargetGroup tdid tgid

                // Activate selected target group
                do! doc.Conn.ActivateTargetGroup tdid tgid

                // update running status cache
                do! ( this :> IMainWindowIFForPP ).UpdateRunningStatus()

                // set menu item status
                m_Window.Dispatcher.InvokeAsync ( fun () ->
                    ( this :> IMainWindowIFForPP ).NoticeUpdateStat confnode
                    ( m_PropGrid.Content :?> IPropPageInterface ).UpdateViewContent()
                    ( this :> IMainWindowIFForPP ).SetProgress false
                ) |> ignore
            })

    /// <summary>
    ///  "Load" item of context menu for the target group node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetGroup_Load ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetGroup> sender
            let tdnode = doc.Stat.GetAncestorTargetDevice confnode
            let tgid = confnode.TargetGroupID
            let tdid = tdnode.Value.TargetDeviceID

            ( this :> IMainWindowIFForPP ).ProcCtrlQuery true ( fun () -> task {
                // Load selected target group
                do! doc.Conn.LoadTargetGroup tdid tgid

                // update running status cache
                do! ( this :> IMainWindowIFForPP ).UpdateRunningStatus()

                // set menu item status
                m_Window.Dispatcher.InvokeAsync ( fun () ->
                    ( this :> IMainWindowIFForPP ).NoticeUpdateStat confnode
                    ( m_PropGrid.Content :?> IPropPageInterface ).UpdateViewContent()
                    ( this :> IMainWindowIFForPP ).SetProgress false
                ) |> ignore
            })

    /// <summary>
    ///  "Inactivate" item of context menu for the target group node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetGroup_Inactivate ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetGroup> sender
            let tdnode = doc.Stat.GetAncestorTargetDevice confnode
            let tgid = confnode.TargetGroupID
            let tdid = tdnode.Value.TargetDeviceID

            ( this :> IMainWindowIFForPP ).ProcCtrlQuery true ( fun () -> task {
                // Inactivate selected target group
                do! doc.Conn.InactivateTargetGroup tdid tgid

                // update running status cache
                do! ( this :> IMainWindowIFForPP ).UpdateRunningStatus()

                // set menu item status
                m_Window.Dispatcher.InvokeAsync ( fun () ->
                    ( this :> IMainWindowIFForPP ).NoticeUpdateStat confnode
                    ( m_PropGrid.Content :?> IPropPageInterface ).UpdateViewContent()
                    ( this :> IMainWindowIFForPP ).SetProgress false
                ) |> ignore
            })

    /// <summary>
    ///  "Unload" item of context menu for the target group node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetGroup_Unload ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetGroup> sender
            let tdnode = doc.Stat.GetAncestorTargetDevice confnode
            let tgid = confnode.TargetGroupID
            let tdid = tdnode.Value.TargetDeviceID

            ( this :> IMainWindowIFForPP ).ProcCtrlQuery true ( fun () -> task {
                // If the target group is activated, inactivate this target group.
                let! activeTGs = doc.Conn.GetActiveTargetGroups tdid
                let actived =
                    activeTGs
                    |> Seq.map _.ID
                    |> Seq.exists ( (=) tgid ) 
                if actived then
                    do! doc.Conn.InactivateTargetGroup tdid tgid

                // unload selected target group
                do! doc.Conn.UnloadTargetGroup tdid tgid

                // update running status cache
                do! ( this :> IMainWindowIFForPP ).UpdateRunningStatus()

                // set menu item status
                m_Window.Dispatcher.InvokeAsync ( fun () ->
                    ( this :> IMainWindowIFForPP ).NoticeUpdateStat confnode
                    ( m_PropGrid.Content :?> IPropPageInterface ).UpdateViewContent()
                    ( this :> IMainWindowIFForPP ).SetProgress false
                ) |> ignore
            })

    /// <summary>
    ///  "Add target" item of context menu for the target group node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetGroup_AddTarget ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetGroup> sender
            let tdnode = doc.Stat.GetAncestorTargetDevice confnode
            let tvi = this.SearchTreeViewItemFromConfigureNode confnode

            // get all target nodes in the target device
            let targetNodes = 
                ( tdnode.Value :> IConfigureNode ).GetChildNodes<ConfNode_TargetGroup>()
                |> Seq.map ( fun itr -> ( itr :> IConfigureNode ).GetChildNodes<ConfNode_Target>() )
                |> Seq.concat

            // generate default target name
            let defName = ConfNode_Target.GenDefaultTargetName targetNodes

            // gen ident number
            let newIdent = ConfNode_Target.GenNewID targetNodes

            let conf : TargetGroupConf.T_Target = {
                IdentNumber = newIdent;
                TargetPortalGroupTag = tpgt_me.zero;
                TargetName = defName;
                TargetAlias = sprintf "Target_%d" newIdent;
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            }

            // create new target node
            let newnode = doc.Stat.AddTargetNode confnode conf

            // Add tree view item for newly created target node.
            this.CreateTreeViewItem_Target newnode
            |> this.AddTreeViewItem tvi.Value

            // Update tree view icon
            doc.Stat.GetNode ( confnode :> IConfigureNode ).NodeID
            |> ( this :> IMainWindowIFForPP ).NoticeUpdateStat

    /// <summary>
    ///  "Add block device LU" item of context menu for the target group node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetGroup_AddBlockDeviceLU ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetGroup> sender
            let tdnode = doc.Stat.GetAncestorTargetDevice confnode
            let tvi = this.SearchTreeViewItemFromConfigureNode confnode

            let lun =
                ( tdnode.Value :> IConfigureNode ).GetDescendantNodes<ILUNode>()
                |> ConfNode_BlockDeviceLU.GenDefaultLUN 
            let luname = sprintf "LU_%d" ( lun_me.toPrim lun )

            // create new block device LU node
            let mult = Constants.LU_DEF_MULTIPLICITY
            let fbs = Blocksize.BS_512
            let otl = blkcnt_me.ofUInt32 Constants.LU_DEF_OPTIMAL_TRANSFER_LENGTH
            let newnode = doc.Stat.AddBlockDeviceLUNode_InTargetGroup confnode lun luname mult fbs otl

            // Add tree view item for newly created target node.
            this.CreateTreeViewItem_LU doc.Stat newnode
            |> this.AddTreeViewItem tvi.Value

            // Update tree view icon
            doc.Stat.GetNode ( confnode :> IConfigureNode ).NodeID
            |> ( this :> IMainWindowIFForPP ).NoticeUpdateStat

    /// <summary>
    ///  "Paste" item of context menu for the target group node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetGroup_Paste ( sender : obj ) ( e : RoutedEventArgs ) =
        if m_Document.IsSome then
            let ss = m_Document.Value.Stat
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetGroup> sender
            let tdnode = ss.GetAncestorTargetDevice confnode
            let parenttvi = this.SearchTreeViewItemFromConfigureNode confnode

            if Clipboard.ContainsData ClientConst.CB_FORMAT_Target then
                let oldTNodes =  ( tdnode.Value :> IConfigureNode ).GetDescendantNodes<ConfNode_Target>()

                // Import
                let w = Clipboard.GetData ClientConst.CB_FORMAT_Target |> string
                let tnode = ( ss.ImportTemporaryDump w ( confnode :> IConfigureNode ).NodeID false ) :?> ConfNode_Target

                // update target node ID
                let conf = {
                    tnode.Values with
                        IdentNumber = ConfNode_Target.GenNewID oldTNodes
                }
                let tnode2 = ss.UpdateTargetNode tnode conf

                // Add tree view item for newly created target node.
                this.CreateTreeViewItem_Target tnode2
                |> this.AddTreeViewItem parenttvi.Value

                // Update tree view icon
                ss.GetNode ( confnode :> IConfigureNode ).NodeID
                |> ( this :> IMainWindowIFForPP ).NoticeUpdateStat

            elif Clipboard.ContainsData ClientConst.CB_FORMAT_BlockDeviceLU then
                let oldMediaNodes = ( confnode :> IConfigureNode ).GetDescendantNodes<IMediaNode>()

                // Import
                let w = Clipboard.GetData ClientConst.CB_FORMAT_BlockDeviceLU |> string
                let lunode = ( ss.ImportTemporaryDump w ( confnode :> IConfigureNode ).NodeID true ) :?> ConfNode_BlockDeviceLU

                // update all media node ID
                this.RenumberMediaNodes ss oldMediaNodes lunode

                // Add tree view item for newly created block device LU node.
                this.CreateTreeViewItem_LU ss lunode
                |> this.AddTreeViewItem parenttvi.Value

                // Update tree view icon
                ss.GetNode ( confnode :> IConfigureNode ).NodeID
                |> ( this :> IMainWindowIFForPP ).NoticeUpdateStat

            else
                ()

    /// <summary>
    ///  "Delete" item of context menu for the target group node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_TargetGroup_Delete ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_TargetGroup> sender
            let tvi = this.SearchTreeViewItemFromConfigureNode confnode

            // Delete current target group node
            doc.Stat.DeleteTargetGroupNode confnode

            // Remove tree view item.
            this.RemoveTreeViewItem tvi.Value

    /// <summary>
    ///  Context menu for the tree view item of the network portal node has opened.
    /// </summary>
    member private this.OnOpened_ContMenu_TVI_NetworkPortal ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let menu = sender :?> ContextMenu
            let confnode = this.GetContextMenuSelectedNode doc.Stat menu
            menu.Tag <- confnode

            let tdnode = doc.Stat.GetAncestorTargetDevice confnode
            let tdid = tdnode.Value.TargetDeviceID
            let tdActived = Seq.exists ( (=) tdid ) m_RunningTDIDs
            m_ContMenu_TVI_NetworkPortal_Delete.IsEnabled <- ( not tdActived )

    /// <summary>
    ///  "Delete" item of context menu for the network portal node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_NetworkPortal_Delete ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<ConfNode_NetworkPortal> sender
            let tvi = this.SearchTreeViewItemFromConfigureNode confnode

            // Delete current network portal node
            doc.Stat.DeleteNetworkPortalNode confnode

            // Remove tree view item.
            this.RemoveTreeViewItem tvi.Value

            // Update tree view icon
            doc.Stat.GetAncestorTargetDevice confnode
            |> Option.iter ( this :> IMainWindowIFForPP ).NoticeUpdateStat

    /// <summary>
    ///  Context menu for the tree view item of the target node has opened.
    /// </summary>
    member private this.OnOpened_ContMenu_TVI_Target ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let menu = sender :?> ContextMenu
            let confnode = this.GetContextMenuSelectedNode doc.Stat menu
            menu.Tag <- confnode
            let editable = this.IsEditable_TargetGroupNode doc.Stat confnode
            m_ContMenu_TVI_Target_Delete.IsEnabled <- editable

    /// <summary>
    ///  "Delete" item of context menu for the target node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_Target_Delete ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let confnode = this.GetContMenu_CurrentNode<IConfigureNode> sender
            let tvi = this.SearchTreeViewItemFromConfigureNode confnode

            // Delete current target node
            doc.Stat.DeleteNodeInTargetGroup confnode

            // Remove tree view item.
            this.RemoveTreeViewItem tvi.Value

            // Update tree view icon
            doc.Stat.GetAncestorTargetGroup confnode
            |> Option.iter ( this :> IMainWindowIFForPP ).NoticeUpdateStat

    /// <summary>
    ///  Context menu for the tree view item of the block device LU node has opened.
    /// </summary>
    member private this.OnOpened_ContMenu_TVI_BlockDeviceLU ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let menu = sender :?> ContextMenu
            let confnode = this.GetContextMenuSelectedNode doc.Stat menu
            menu.Tag <- confnode
            let pastFlg =
                [| ClientConst.CB_FORMAT_PlainFileMedia; ClientConst.CB_FORMAT_MemBufferMedia; ClientConst.CB_FORMAT_DebugMedia; |]
                |> Seq.exists Clipboard.ContainsData
            let editable = this.IsEditable_TargetGroupNode doc.Stat confnode
            m_ContMenu_TVI_BlockDeviceLU_Paste.IsEnabled <- pastFlg && editable
            m_ContMenu_TVI_BlockDeviceLU_AddPlainFileMedia.IsEnabled <- editable
            m_ContMenu_TVI_BlockDeviceLU_AddMemBufferMedia.IsEnabled <- editable
            m_ContMenu_TVI_BlockDeviceLU_AddDebugMedia.IsEnabled <- editable
            m_ContMenu_TVI_BlockDeviceLU_Delete.IsEnabled <- editable

    /// <summary>
    ///  "Add plain file media" item of context menu is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_BlockDeviceLU_AddPlainFileMedia ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            this.GetContMenu_CurrentNode<ConfNode_BlockDeviceLU> sender
            |> this.AddPlainFileMedia doc.Stat

    /// <summary>
    ///  "Add MemBuffer media" item of context menu for the block device LU node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_BlockDeviceLU_AddMemBufferMedia ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            this.GetContMenu_CurrentNode<ConfNode_BlockDeviceLU> sender
            |> this.AddMemBufferMedia doc.Stat

    /// <summary>
    ///  "Add Debug media" item of context menu for the block device LU node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_BlockDeviceLU_AddDebugMedia ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            this.GetContMenu_CurrentNode<ConfNode_BlockDeviceLU> sender
            |> this.AddDebugMedia doc.Stat

    /// <summary>
    ///  "Paste" item of context menu for the block device LU node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_BlockDeviceLU_Paste ( sender : obj ) ( e : RoutedEventArgs ) =
        if m_Document.IsSome then
            let ss = m_Document.Value.Stat
            let confnode = this.GetContMenu_CurrentNode<ConfNode_BlockDeviceLU> sender
            let tdnode = ss.GetAncestorTargetDevice confnode
            let parenttvi = this.SearchTreeViewItemFromConfigureNode confnode
            let oldMediaNodes =  ( tdnode.Value :> IConfigureNode ).GetDescendantNodes<IMediaNode>()

            let vformat = [|
                ClientConst.CB_FORMAT_PlainFileMedia;
                ClientConst.CB_FORMAT_MemBufferMedia;
                ClientConst.CB_FORMAT_DebugMedia;
            |]

            let fmtname =
                vformat
                |> Array.tryFind Clipboard.ContainsData
            if fmtname.IsSome then
                let cbdata = Clipboard.GetData fmtname.Value |> string
                let mnode = ( ss.ImportTemporaryDump cbdata ( confnode :> IConfigureNode ).NodeID false ) :?> IMediaNode

                // update all media node ID
                this.RenumberMediaNodes ss oldMediaNodes mnode

                // Add tree view item for newly created target node.
                this.CreateTreeViewItem_Media ss mnode
                |> this.AddTreeViewItem parenttvi.Value

                // Update tree view icon
                ss.GetAncestorTargetGroup mnode
                |> Option.iter ( this :> IMainWindowIFForPP ).NoticeUpdateStat

    /// <summary>
    ///  Context menu for the tree view item of the dummy device LU node has opened.
    /// </summary>
    member private this.OnOpened_ContMenu_TVI_DummyDeviceLU ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let menu = sender :?> ContextMenu
            let confnode = this.GetContextMenuSelectedNode doc.Stat menu
            menu.Tag <- confnode
            let editable = this.IsEditable_TargetGroupNode doc.Stat confnode
            m_ContMenu_TVI_DummyDeviceLU_Delete.IsEnabled <- editable

    /// <summary>
    ///  Context menu for the tree view item of the plain file media node has opened.
    /// </summary>
    member private this.OnOpened_ContMenu_TVI_PlainFileMedia ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let menu = sender :?> ContextMenu
            let confnode = this.GetContextMenuSelectedNode doc.Stat menu
            menu.Tag <- confnode
            let editable = this.IsEditable_TargetGroupNode doc.Stat confnode
            m_ContMenu_TVI_PlainFileMedia_Delete.IsEnabled <- editable

    /// <summary>
    ///  Context menu for the tree view item of the plain file media node has opened.
    /// </summary>
    member private this.OnOpened_ContMenu_TVI_MemBufferMedia ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let menu = sender :?> ContextMenu
            let confnode = this.GetContextMenuSelectedNode doc.Stat menu
            menu.Tag <- confnode
            let editable = this.IsEditable_TargetGroupNode doc.Stat confnode
            m_ContMenu_TVI_MemBufferMedia_Delete.IsEnabled <- editable

    /// <summary>
    ///  Context menu for the tree view item of the plain file media node has opened.
    /// </summary>
    member private this.OnOpened_ContMenu_TVI_DummyMedia ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let menu = sender :?> ContextMenu
            let confnode = this.GetContextMenuSelectedNode doc.Stat menu
            menu.Tag <- confnode
            let editable = this.IsEditable_TargetGroupNode doc.Stat confnode
            m_ContMenu_TVI_DummyMedia_Delete.IsEnabled <- editable

    /// <summary>
    ///  Context menu for the tree view item of the target node has opened.
    /// </summary>
    member private this.OnOpened_ContMenu_TVI_DebugMedia ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            let menu = sender :?> ContextMenu
            let confnode = this.GetContextMenuSelectedNode doc.Stat menu
            menu.Tag <- confnode
            let pastFlg =
                [| ClientConst.CB_FORMAT_PlainFileMedia; ClientConst.CB_FORMAT_MemBufferMedia; ClientConst.CB_FORMAT_DebugMedia; |]
                |> Seq.exists Clipboard.ContainsData
            let editable = this.IsEditable_TargetGroupNode doc.Stat confnode
            m_ContMenu_TVI_DebugMedia_AddPlainFileMedia.IsEnabled <- editable
            m_ContMenu_TVI_DebugMedia_AddMemBufferMedia.IsEnabled <- editable
            m_ContMenu_TVI_DebugMedia_AddDebugMedia.IsEnabled <- editable
            m_ContMenu_TVI_DebugMedia_Paste.IsEnabled <- pastFlg && editable
            m_ContMenu_TVI_DebugMedia_Delete.IsEnabled <- editable

    /// <summary>
    ///  "Add plain file media" item of context menu for the debug media node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_DebugMedia_AddPlainFileMedia ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            this.GetContMenu_CurrentNode<ConfNode_DebugMedia> sender
            |> this.AddPlainFileMedia doc.Stat

    /// <summary>
    ///  "Add MemBuffer media" item of context menu for the debug media node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_DebugMedia_AddMemBufferMedia ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            this.GetContMenu_CurrentNode<ConfNode_DebugMedia> sender
            |> this.AddMemBufferMedia doc.Stat

    /// <summary>
    ///  "Add Debug media" item of context menu for the debug media node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_DebugMedia_AddDebugMedia ( sender : obj ) ( e : RoutedEventArgs ) =
        match m_Document with
        | None -> ()
        | Some doc ->
            this.GetContMenu_CurrentNode<ConfNode_DebugMedia> sender
            |> this.AddDebugMedia doc.Stat

    /// <summary>
    ///  "Paste" item of context menu for the debug media node is clicked.
    /// </summary>
    member private this.OnClicked_ContMenu_TVI_DebugMedia_Paste ( sender : obj ) ( e : RoutedEventArgs ) =
        if m_Document.IsSome then
            let ss = m_Document.Value.Stat
            let confnode = this.GetContMenu_CurrentNode<ConfNode_DebugMedia> sender
            let tdnode = ss.GetAncestorTargetDevice confnode
            let parenttvi = this.SearchTreeViewItemFromConfigureNode confnode
            let oldMediaNodes =  ( tdnode.Value :> IConfigureNode ).GetDescendantNodes<IMediaNode>()

            let vformat = [|
                ClientConst.CB_FORMAT_PlainFileMedia;
                ClientConst.CB_FORMAT_MemBufferMedia;
                ClientConst.CB_FORMAT_DebugMedia;
            |]

            let fmtname =
                vformat
                |> Array.tryFind Clipboard.ContainsData
            if fmtname.IsSome then
                let cbdata = Clipboard.GetData fmtname.Value |> string
                let mnode = ( ss.ImportTemporaryDump cbdata ( confnode :> IConfigureNode ).NodeID false ) :?> IMediaNode

                // update all media node ID
                this.RenumberMediaNodes ss oldMediaNodes mnode

                // Add tree view item for newly created media node.
                this.CreateTreeViewItem_Media ss mnode
                |> this.AddTreeViewItem parenttvi.Value

                // Update tree view icon
                ss.GetAncestorTargetGroup mnode
                |> Option.iter ( this :> IMainWindowIFForPP ).NoticeUpdateStat

    /// <summary>
    ///  Main window will close.
    /// </summary>
    member private this.OnClosing ( sender : obj ) ( e : CancelEventArgs ) =
        UserOpeStat.MainWindowWidth <- int m_Window.Width
        UserOpeStat.MainWindowHeight <- int m_Window.Height

    ///////////////////////////////////////////////////////////////////////////
    // Private method

    /// <summary>
    ///  Update controller status when configuration was loaded.
    /// </summary>
    /// <param name="ss">
    ///  Loaded configuration.
    /// </param>
    /// <param name="runningTD">
    ///  The target device IDs array that are running now.
    /// </param>
    /// <param name="loadedTG">
    ///  The target group IDs dictionaly that has been loaded.
    /// </param>
    /// <param name="activeTG">
    ///  The target group IDs dictionaly that has been activated.
    /// </param>
    member private this.UpdateForLoaded
            ( ss : ServerStatus )
            ( runningTD : TDID_T[] )
            ( loadedTG : Dictionary< TDID_T, TGID_T[] > )
            ( activeTG : Dictionary< TDID_T, TGID_T[] > ) : unit =
        m_MenuLogin.IsEnabled <- false
        m_MenuLogout.IsEnabled <- true
        m_MenuReload.IsEnabled <- true
        m_MenuVerify.IsEnabled <- true
        m_MenuCreateMediaFile.IsEnabled <- true
        m_MenuPulish.IsEnabled <- true
        m_StructTree.IsEnabled <- true
        m_VerifyResultList.IsEnabled <- true
        m_StructTree.Items.Clear()
        m_VerifyResultList.Items.Clear()

        // Update tree view control.
        let controllerNode = ( ss.ControllerNode :> IConfigureNode )
        let modified = ( controllerNode :?> IConfigFileNode ).Modified = ModifiedStatus.Modified
        let icon = if modified then IconImageIndex.III_CONTROLLER_MODIFIED else IconImageIndex.III_CONTROLLER
        let tviController = this.CreateTreeViewItem controllerNode icon m_ContMenu_TVI_Controller
        m_StructTree.Items.Add tviController |> ignore

        // Set child items
        tviController.Items.Clear()
        let children = controllerNode.GetChildNodes<ConfNode_TargetDevice>()
        for itr in children do
            itr
            |> this.CreateTreeViewItem_TargetDevice ss runningTD loadedTG activeTG
            |> tviController.Items.Add
            |> ignore

        // Show initial property page
        tviController.IsSelected <- true

    /// <summary>
    ///  Add a tree view item of the Target device node to the tree view controll.
    /// </summary>
    /// <param name="ss">
    ///  Loaded configuration.
    /// </param>
    /// <param name="runningTD">
    ///  The target device IDs array that are running now.
    /// </param>
    /// <param name="loadedTG">
    ///  The target group IDs dictionaly that has been loaded.
    /// </param>
    /// <param name="activeTG">
    ///  The target group IDs dictionaly that has been activated.
    /// </param>
    /// <param name="tdnode">
    ///  The target device node which should be added to the tree view controll.
    /// </param>
    member private this.CreateTreeViewItem_TargetDevice
            ( ss : ServerStatus )
            ( runningTD : TDID_T[] )
            ( loadedTG : Dictionary< TDID_T, TGID_T[] > )
            ( activeTG : Dictionary< TDID_T, TGID_T[] > )
            ( tdnode : ConfNode_TargetDevice ) : TreeViewItem =

        // Create a tree view item of the target device node
        let tdicon =
            if Seq.exists ( (=) tdnode.TargetDeviceID ) runningTD then
                IconImageIndex.III_TARGET_DEVICE_RUNNING
            elif ( tdnode :> IConfigFileNode ).Modified = ModifiedStatus.Modified then
                IconImageIndex.III_TARGET_DEVICE_MODIFIED
            else
                IconImageIndex.III_TARGET_DEVICE_UNLOADED
        let tdtvi = this.CreateTreeViewItem tdnode tdicon m_ContMenu_TVI_TargetDevice

        let wltg =
            let r, w = loadedTG.TryGetValue tdnode.TargetDeviceID
            if r then w else Array.empty
        let watg =
            let r, w = activeTG.TryGetValue tdnode.TargetDeviceID
            if r then w else Array.empty

        // Set child items
        tdtvi.Items.Clear()
        let children = ( tdnode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
        for itrChild in children do
            match itrChild with
            | :? ConfNode_TargetGroup as x ->
                this.CreateTreeViewItem_TargetGroup ss wltg watg x
                |> tdtvi.Items.Add
                |> ignore
            | :? ConfNode_NetworkPortal as x ->
                this.CreateTreeViewItem_NetworkPortal x
                |> tdtvi.Items.Add
                |> ignore
            | _ ->
                () // ignore

        tdtvi

    /// <summary>
    ///  Create a tree view item of the Network portal node.
    /// </summary>
    /// <param name="npnode">
    ///  The network portal node which should be added to the tree view controll.
    /// </param>
    member private this.CreateTreeViewItem_NetworkPortal ( npnode : ConfNode_NetworkPortal ) : TreeViewItem =
        this.CreateTreeViewItem npnode IconImageIndex.III_NETWORK_PORTAL m_ContMenu_TVI_NetworkPortal

    /// <summary>
    ///  Create a tree view item of the Target group node.
    /// </summary>
    /// <param name="ss">
    ///  Loaded configuration.
    /// </param>
    /// <param name="loadedTG">
    ///  The target group IDs dictionaly that has been loaded.
    /// </param>
    /// <param name="activeTG">
    ///  The target group IDs dictionaly that has been activated.
    /// </param>
    /// <param name="tgnode">
    ///  The target group node which should be added to the tree view controll.
    /// </param>
    member private this.CreateTreeViewItem_TargetGroup
            ( ss : ServerStatus )
            ( loadedTG : TGID_T[] )
            ( activeTG : TGID_T[] )
            ( tgnode : ConfNode_TargetGroup ) : TreeViewItem =

        // create a tree view item of the target group node
        let icon =
            if Seq.exists ( (=) tgnode.TargetGroupID ) activeTG then
                IconImageIndex.III_TARGET_GROUP_ACTIVE
            elif Seq.exists ( (=) tgnode.TargetGroupID ) loadedTG then
                IconImageIndex.III_TARGET_GROUP_LOADED
            elif ( tgnode :> IConfigFileNode ).Modified = ModifiedStatus.Modified then
                IconImageIndex.III_TARGET_GROUP_MODIFIED
            else
                IconImageIndex.III_TARGET_GROUP_UNLOADED
        let tviTG = this.CreateTreeViewItem tgnode icon m_ContMenu_TVI_TargetGroup

        // Set child items
        tviTG.Items.Clear()
        let children = ( tgnode :> IConfigureNode ).GetChildNodes<IConfigureNode>()
        for itrChild in children do
            match itrChild with
            | :? ConfNode_Target as x ->
                this.CreateTreeViewItem_Target x
                |> tviTG.Items.Add
                |> ignore
            | :? ILUNode as x ->
                this.CreateTreeViewItem_LU ss x
                |> tviTG.Items.Add
                |> ignore
            | _ ->
                ()  // ignore

        tviTG

    /// <summary>
    ///  Create a tree view item of the target node.
    /// </summary>
    /// <param name="tnode">
    ///  The target node which should be added to the tree view controll.
    /// </param>
    member private this.CreateTreeViewItem_Target ( tnode : ConfNode_Target ) : TreeViewItem =
        this.CreateTreeViewItem tnode IconImageIndex.III_TARGET m_ContMenu_TVI_Target

    /// <summary>
    ///  Create a tree view item of the LU node.
    /// </summary>
    /// <param name="ss">
    ///  Loaded configuration.
    /// </param>
    /// <param name="lunode">
    ///  The LU node which should be added to the tree view controll.
    /// </param>
    member private this.CreateTreeViewItem_LU
            ( ss : ServerStatus )
            ( lunode : ILUNode ) : TreeViewItem =

        // create a tree view item of the LU node
        let icon, contMenu =
            match lunode with
            | :? ConfNode_BlockDeviceLU ->
                IconImageIndex.III_LU_BLOCK_DEVICE, m_ContMenu_TVI_BlockDeviceLU
            | _ ->
                IconImageIndex.III_LU_DUMMY_DEVICE, m_ContMenu_TVI_DummyDeviceLU
        let tviLU = this.CreateTreeViewItem lunode icon contMenu

        // Set child items
        tviLU.Items.Clear()
        let children = lunode.GetChildNodes<IMediaNode>()
        for itr in children do
            itr
            |> this.CreateTreeViewItem_Media ss
            |> tviLU.Items.Add
            |> ignore

        tviLU

    /// <summary>
    ///  Add a tree view item of the media node to the tree view controll.
    /// </summary>
    /// <param name="ss">
    ///  Loaded configuration.
    /// </param>
    /// <param name="medianode">
    ///  The media node which should be added to the tree view controll.
    /// </param>
    member private this.CreateTreeViewItem_Media
            ( ss : ServerStatus )
            ( medianode : IMediaNode ) : TreeViewItem =

        // create a tree view item of the media node
        let icon, contMenu =
            match medianode with
            | :? ConfNode_PlainFileMedia as x ->
                IconImageIndex.III_MEDIA_PLAIN_FILE, m_ContMenu_TVI_PlainFileMedia
            | :? ConfNode_MemBufferMedia as x ->
                IconImageIndex.III_MEDIA_MEM_BUFFER, m_ContMenu_TVI_MemBufferMedia
            | :? ConfNode_DebugMedia as x ->
                IconImageIndex.III_MEDIA_DEBUG, m_ContMenu_TVI_DebugMedia
            | _ ->
                IconImageIndex.III_MEDIA_DUMMY, m_ContMenu_TVI_DummyMedia
        let tviMedia = this.CreateTreeViewItem medianode icon contMenu

        // Set child items
        tviMedia.Items.Clear()
        let children = medianode.GetChildNodes<IMediaNode>()
        for itr in children do
            itr
            |> this.CreateTreeViewItem_Media ss
            |> tviMedia.Items.Add
            |> ignore

        tviMedia

    member private this.CreateTreeViewItem
            ( confnode : IConfigureNode )
            ( img : IconImageIndex )
            ( contMenu : ContextMenu ) : TreeViewItem =

        let sp = new StackPanel( Orientation = Orientation.Horizontal )
        let icon = m_Config.Icons.Get img
        let img = 
            if icon.IsSome then
                new Image(
                    Source = icon.Value,
                    Width = GuiConst.ICO_WIDTH,
                    Height = GuiConst.ICO_HEIGHT
                ) :> UIElement
            else
                new Grid(
                    Width = GuiConst.ICO_WIDTH,
                    Height = GuiConst.ICO_HEIGHT
                ) :> UIElement
        let nodeItemLabel = confnode.MinDescriptString
        sp.Children.Add( img ) |> ignore
        sp.Children.Add( new TextBlock( Text = nodeItemLabel, VerticalAlignment = VerticalAlignment.Center, Margin=Thickness( 5.0 ) ) ) |> ignore
        let tvi1 = new TreeViewItem( Header = sp, Tag = confnode_me.toPrim confnode.NodeID )

        tvi1.Selected.AddHandler ( fun sender e ->
            if ( sender :?> TreeViewItem ).IsSelected then
                this.OnSelect_TreeViewItem confnode
        )

        tvi1.ContextMenu <- contMenu
        tvi1

    /// <summary>
    ///  It shows the property page for specified configure node.
    /// </summary>
    /// <param name="confNode">
    ///  The configure node that should be shown in the property page.
    /// </param>
    member private this.ShowPropertyPage ( confNode : IConfigureNode ) : unit =

        let pp : IPropPageInterface =
            match confNode with
            | :? ConfNode_Controller ->
                let g = m_Config.UIElem.Get PropertyViewIndex.PVI_CONTROLLER :?> Grid
                new ControllerPropPage( m_Config, g, this, m_Document.Value.Stat, confNode.NodeID )
            | :? ConfNode_TargetDevice ->
                let g = m_Config.UIElem.Get PropertyViewIndex.PVI_TARGET_DEVICE :?> Grid
                new TargetDevicePropPage( m_Config, g, this, m_Document.Value.Stat, m_Document.Value.Conn, confNode.NodeID )
            | :? ConfNode_NetworkPortal ->
                let g = m_Config.UIElem.Get PropertyViewIndex.PVI_NETWORK_PORTAL :?> Grid
                new NetworkPortalPropPage( m_Config, g, this, m_Document.Value.Stat, m_Document.Value.Conn, confNode.NodeID )
            | :? ConfNode_TargetGroup ->
                let g = m_Config.UIElem.Get PropertyViewIndex.PVI_TARGET_GROUP :?> Grid
                new TargetGroupPropPage( m_Config, g, this, m_Document.Value.Stat, m_Document.Value.Conn, confNode.NodeID )
            | :? ConfNode_Target ->
                let g = m_Config.UIElem.Get PropertyViewIndex.PVI_TARGET :?> Grid
                new TargetPropPage( m_Config, g, this, m_Document.Value.Stat, m_Document.Value.Conn, confNode.NodeID )
            | :? ConfNode_BlockDeviceLU ->
                let g = m_Config.UIElem.Get PropertyViewIndex.PVI_LU_BLOCK_DEVICE :?> Grid
                new BlockDeviceLUPropPage( m_Config, g, this, m_Document.Value.Stat, m_Document.Value.Conn, confNode.NodeID )
            | :? ConfNode_DummyDeviceLU ->
                let g = m_Config.UIElem.Get PropertyViewIndex.PVI_LU_DUMMY_DEVICE :?> Grid
                new DummyDeviceLUPropPage( m_Config, g, this, m_Document.Value.Stat, m_Document.Value.Conn, confNode.NodeID )
            | :? ConfNode_PlainFileMedia ->
                let g = m_Config.UIElem.Get PropertyViewIndex.PVI_MEDIA_PLAIN_FILE :?> Grid
                new PlainFileMediaPropPage( m_Config, g, this, m_Document.Value.Stat, m_Document.Value.Conn, confNode.NodeID )
            | :? ConfNode_MemBufferMedia ->
                let g = m_Config.UIElem.Get PropertyViewIndex.PVI_MEDIA_MEM_BUFFER :?> Grid
                new MemBufferMediaPropPage( m_Config, g, this, m_Document.Value.Stat, m_Document.Value.Conn, confNode.NodeID )
            | :? ConfNode_DummyMedia ->
                let g = m_Config.UIElem.Get PropertyViewIndex.PVI_MEDIA_DUMMY :?> Grid
                new DummyMediaPropPage( m_Config, g, this, m_Document.Value.Stat, m_Document.Value.Conn, confNode.NodeID )
            | :? ConfNode_DebugMedia ->
                let g = m_Config.UIElem.Get PropertyViewIndex.PVI_MEDIA_DEBUG :?> Grid
                new DebugMediaPropPage( m_Config, g, this, m_Document.Value.Stat, m_Document.Value.Conn, confNode.NodeID )
            | _ ->
                let g = m_Config.UIElem.Get PropertyViewIndex.PVI_INITIAL :?> Grid
                new InitialPropPage( m_Config, g )

        // Notify closing old page
        match m_PropGrid.Content with
        | :? IPropPageInterface as x ->
            x.OnClosePage()
        | _ -> ()

        // Show new property page
        m_PropGrid.Content <- pp
        pp.SetPageWidth m_PropGrid.ViewportWidth
        if m_Document.IsSome then
            pp.InitializeViewContent()

    /// <summary>
    ///  Update controller status when configuration was unloaded.
    /// </summary>
    member private _.UpdateForUnloaded() : unit =
        // Invoke unload event
        for itr in m_UnloadEventHandler.Values do
            itr()

        // Notify closing old page
        match m_PropGrid.Content with
        | :? IPropPageInterface as x ->
            x.OnClosePage()
        | _ -> ()

        m_MenuLogin.IsEnabled <- true
        m_MenuLogout.IsEnabled <- false
        m_MenuReload.IsEnabled <- false
        m_MenuVerify.IsEnabled <- false
        m_MenuPulish.IsEnabled <- false
        m_MenuCreateMediaFile.IsEnabled <- false
        m_StructTree.IsEnabled <- false
        m_VerifyResultList.IsEnabled <- false
        m_StructTree.Items.Clear()
        m_VerifyResultList.Items.Clear()
        let g = m_Config.UIElem.Get PropertyViewIndex.PVI_INITIAL :?> Grid
        let pp = new InitialPropPage( m_Config, g )
        m_PropGrid.Content <- pp

    /// <summary>
    ///  Update icon image of specified tree view item.
    /// </summary>
    /// <param name="tvi">
    ///  The tree view item of which the icon image should be updated.
    /// </param>
    /// <param name="img">
    ///  The icon image that should be set to specified tree view item.
    /// </param>
    member private _.UpdateTreeViewItemIcon ( tvi : TreeViewItem ) ( img : IconImageIndex ) : unit =
        let sp = tvi.Header :?> StackPanel
        let icon = m_Config.Icons.Get img
        let e =
            if icon.IsSome then
                new Image(
                    Source = icon.Value,
                    Width = GuiConst.ICO_WIDTH,
                    Height = GuiConst.ICO_HEIGHT
                ) :> UIElement
            else
                new Grid(
                    Width = GuiConst.ICO_WIDTH,
                    Height = GuiConst.ICO_HEIGHT
                ) :> UIElement
        sp.Children.RemoveAt( 0 )
        sp.Children.Insert( 0, e )

    /// <summary>
    ///  Update label of specified tree view item.
    /// </summary>
    /// <param name="tvi">
    ///  The tree view item of which the icon image should be updated.
    /// </param>
    /// <param name="text">
    ///  The label string that should be set to specified tree view item.
    /// </param>
    member private _.UpdateTreeViewItemLabel ( tvi : TreeViewItem ) ( text : string ) : unit =
        let sp = tvi.Header :?> StackPanel
        let tb = sp.Children.Item 1 :?> TextBlock
        tb.Text <- text

    /// <summary>
    ///  Get all pairs of tree view item and configure node.
    /// </summary>
    /// <returns>
    ///  sequence of the tree view item and configure node pairs.
    /// </returns>
    member private _.GetTreeViewItemAndConfigNodePair() : ( CONFNODE_T * TreeViewItem ) seq =
        let rec loop2 ( r : TreeViewItem ) : ( CONFNODE_T * TreeViewItem ) seq =
            seq {
                for itr in r.Items do
                    let tvi = itr :?> TreeViewItem
                    let cnd = tvi.Tag :?> uint64 |> confnode_me.fromPrim
                    yield ( cnd, tvi )
                    yield! loop2 tvi
            }
        seq {
            for itr in m_StructTree.Items do
                let tvi = itr :?> TreeViewItem
                let cnd = tvi.Tag :?> uint64 |> confnode_me.fromPrim
                yield ( cnd, tvi )
                yield! loop2 tvi
        }

    /// <summary>
    ///  Search the tree view item which has specified configure node.
    /// </summary>
    /// <param name="cn">
    ///  configure node.
    /// </param>
    /// <returns>
    ///  The tree view item that has pecified configure node at argument "cn", or None.
    /// </returns>
    member private this.SearchTreeViewItemFromConfigureNode ( cn : IConfigureNode ) : TreeViewItem option =
        this.GetTreeViewItemAndConfigNodePair()
        |> Seq.tryFind ( fst >> (=) cn.NodeID )
        |> Option.map snd

    /// <summary>
    ///  Delete specified tree view item
    /// </summary>
    /// <param name="tvi">
    ///  The tree view item that should be removed.
    /// </param>
    member private this.RemoveTreeViewItem ( tvi : TreeViewItem ) : unit =

        // Get parent tree view item
        let ptvi = tvi.Parent :?> TreeViewItem

        // delete specified tree view item
        ptvi.Items.Remove tvi

        // If the property page of the deleted target has been showed,
        // change to the page of which the parent node.
        match m_PropGrid.Content with
        | :? IPropPageInterface as x ->
            if x.GetNodeID() = confnode_me.fromPrim ( tvi.Tag :?> uint64 ) then
                ptvi.IsSelected <- true
        | _ -> ()

    /// <summary>
    ///  Add new tree view item to child of specified tree view item.
    /// </summary>
    /// <param name="ptvi">
    ///  Parent tree view item.
    /// </param>
    /// <param name="ctvi">
    ///  Tree view item should be added..
    /// </param>
    member private _.AddTreeViewItem ( ptvi : TreeViewItem ) ( ctvi : TreeViewItem ) : unit =
        let ss = m_Document.Value.Stat
        [|
            for i in ptvi.Items -> ( i :?> TreeViewItem )
            yield ctvi
            ptvi.Items.Clear()
        |]
        |> Seq.sortWith ( fun a b ->
            let anode = a.Tag :?> uint64 |> confnode_me.fromPrim |> ss.GetNode
            let bnode = b.Tag :?> uint64 |> confnode_me.fromPrim |> ss.GetNode
            Functions.CompareMultiLevelKey anode.SortKey bnode.SortKey
        )
        |> Seq.iter ( ptvi.Items.Add >> ignore )


    /// <summary>
    ///  Get ContextMenu object that is parent of event sender MenuItem object.
    /// </summary>
    /// <param name="sender">
    ///  MenuItem object that given by the event hander.
    /// </param>
    member private this.GetContextMenuObj ( sender : obj ) : ContextMenu =
        match sender with
        | :? MenuItem as x ->
            this.GetContextMenuObj x.Parent
        | _ as x ->
            x :?> ContextMenu

    /// <summary>
    ///  Gets the configure node object for which the context menu is to be displayed.
    /// </summary>
    /// <param name="sender">
    ///  MenuItem object that given by the event hander.
    /// </param>
    /// <returns>
    ///  The configure node object.
    /// </returns>
    member private this.GetContMenu_CurrentNode<'T> ( sender : obj ) : 'T =
        let c = this.GetContextMenuObj sender
        c.Tag :?> 'T

    /// <summary>
    ///  Get the node at the display position of the context menu
    /// </summary>
    /// <param name="ss">
    ///  ServerStatus object
    /// </param>
    /// <param name="menu">
    ///  Context menu object
    /// </param>
    member private _.GetContextMenuSelectedNode ( ss : ServerStatus ) ( menu : ContextMenu ) : IConfigureNode =
        menu.PlacementTarget :?> TreeViewItem
        |> _.Tag :?> uint64
        |> confnode_me.fromPrim
        |> ss.GetNode

    /// <summary>
    ///  Update all of media ID of pasted media nodes.
    /// </summary>
    /// <param name="ss">
    ///  ServerStatus object
    /// </param>
    /// <param name="oldMediaNodes">
    ///  Media nodes list before pasting.
    /// </param>
    /// <param name="addedNewNode">
    ///  The pasted node.
    /// </param>
    member private _.RenumberMediaNodes ( ss : ServerStatus ) ( oldMediaNodes : IMediaNode list ) ( addedNewNode : IConfigureNode ) : unit =

        addedNewNode.GetDescendantNodes<IMediaNode>()
        |> Seq.fold ( fun statMediaNodes itr ->
            match itr with
            | :? ConfNode_DummyMedia as x ->
                let newMediaID = statMediaNodes |> ConfNode_DummyMedia.GenNewID
                let newNode = ss.UpdateDummyMediaNode x newMediaID ( x :> IMediaNode ).Name
                ( newNode :> IMediaNode ) :: statMediaNodes
            | :? ConfNode_PlainFileMedia as x ->
                let conf = {
                    x.Values with
                        IdentNumber = statMediaNodes |> ConfNode_PlainFileMedia.GenNewID
                }
                let newNode = ss.UpdatePlainFileMediaNode x conf
                ( newNode :> IMediaNode ) :: statMediaNodes
            | :? ConfNode_MemBufferMedia as x ->
                let conf = {
                    x.Values with
                        IdentNumber = statMediaNodes |> ConfNode_MemBufferMedia.GenNewID
                }
                let newNode = ss.UpdateMemBufferMediaNode x conf
                ( newNode :> IMediaNode ) :: statMediaNodes
            | _ ->
                statMediaNodes
        ) oldMediaNodes
        |> ignore

    /// <summary>
    ///  Whether the nodes under the TargetGroup node can be edited
    /// </summary>
    /// <param name="ss">
    ///  ServerStatus
    /// </param>
    /// <param name="confNode">
    ///  Selected node.
    /// </param>
    member private this.IsEditable_TargetGroupNode ( ss : ServerStatus )  ( confNode : IConfigureNode ) : bool =
        let tgid = ss.GetAncestorTargetGroup confNode |> Option.get |> _.TargetGroupID
        let tdid = ss.GetAncestorTargetDevice confNode |> Option.get |> _.TargetDeviceID
        let tdActived = Seq.exists ( (=) tdid ) m_RunningTDIDs
        let tgLoaded =
            match m_LoadedTGIDs.TryGetValue tdid with
            | true, v ->
                Seq.exists ( (=) tgid ) v
            | false, _ ->
                false
        ( not tdActived || not tgLoaded )

    member private this.AddPlainFileMedia ( ss : ServerStatus )( selectedNode : IConfigureNode ) =
        let tdnode = ss.GetAncestorTargetDevice selectedNode
        let tvi = this.SearchTreeViewItemFromConfigureNode selectedNode

        // gen ident number
        let newIdent =
            ( tdnode.Value :> IConfigureNode ).GetDescendantNodes<IMediaNode>()
            |> ConfNode_PlainFileMedia.GenNewID

        // create new plain file media node
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = newIdent;
            MediaName = sprintf "PlainFile_%d" newIdent;
            FileName = "";
            MaxMultiplicity = Constants.PLAINFILE_DEF_MAXMULTIPLICITY;
            QueueWaitTimeOut = Constants.PLAINFILE_DEF_QUEUEWAITTIMEOUT;
            WriteProtect = false;
        }
        let newnode = ss.AddPlainFileMediaNode selectedNode conf

        // Add tree view item for newly created plain file media node.
        this.CreateTreeViewItem_Media ss newnode
        |> this.AddTreeViewItem tvi.Value

        // Update tree view icon
        ss.GetAncestorTargetGroup newnode
        |> Option.iter ( this :> IMainWindowIFForPP ).NoticeUpdateStat

    member private this.AddMemBufferMedia ( ss : ServerStatus )( selectedNode : IConfigureNode ) =
        let tdnode = ss.GetAncestorTargetDevice selectedNode
        let tvi = this.SearchTreeViewItemFromConfigureNode selectedNode

        // gen ident number
        let newIdent =
            ( tdnode.Value :> IConfigureNode ).GetDescendantNodes<IMediaNode>()
            |> ConfNode_MemBufferMedia.GenNewID

        // create new MemBuffer media node
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = newIdent;
            MediaName = sprintf "MemBuffer_%d" newIdent;
            BytesCount = 0UL;
        }
        let newnode = ss.AddMemBufferMediaNode selectedNode conf

        // Add tree view item for newly created plain file media node.
        this.CreateTreeViewItem_Media ss newnode
        |> this.AddTreeViewItem tvi.Value

        // Update tree view icon
        ss.GetAncestorTargetGroup newnode
        |> Option.iter ( this :> IMainWindowIFForPP ).NoticeUpdateStat

    member private this.AddDebugMedia ( ss : ServerStatus )( selectedNode : IConfigureNode ) =
        let tdnode = ss.GetAncestorTargetDevice selectedNode
        let tvi = this.SearchTreeViewItemFromConfigureNode selectedNode

        // gen ident number
        let newIdent =
            ( tdnode.Value :> IConfigureNode ).GetDescendantNodes<IMediaNode>()
            |> ConfNode_DebugMedia.GenNewID

        // gen media name
        let newName = sprintf "Debug_%d" newIdent;

        // create new Debug media node
        let newnode = ss.AddDebugMediaNode selectedNode newIdent newName

        // Add tree view item for newly created plain file media node.
        this.CreateTreeViewItem_Media ss newnode
        |> this.AddTreeViewItem tvi.Value

        // Update tree view icon
        ss.GetAncestorTargetGroup newnode
        |> Option.iter ( this :> IMainWindowIFForPP ).NoticeUpdateStat

