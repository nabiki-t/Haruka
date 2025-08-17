//=============================================================================
// Haruka Software Storage.
// XamlLoader.fs : Load the XAML files.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.IO
open System.Xml
open System.Windows
open System.Windows.Markup
open System.Collections.Generic
open System.Collections.Frozen

open Haruka.Constants
open Haruka.Commons
open Haruka.Client

/// Constants that represents the icon image.
type PropertyViewIndex =
    | PVI_LOGIN_DIALOG
    | PVI_EDIT_IPWHITELIST
    | PVI_EDIT_DEBUG_TRAP
    | PVI_EDIT_TARGET_NAME
    | PVI_CREATE_MEDIA_FILE
    | PVI_INITIAL
    | PVI_CONTROLLER
    | PVI_TARGET_DEVICE
    | PVI_NETWORK_PORTAL
    | PVI_TARGET_GROUP
    | PVI_TARGET
    | PVI_LU_BLOCK_DEVICE
    | PVI_LU_DUMMY_DEVICE
    | PVI_MEDIA_PLAIN_FILE
    | PVI_MEDIA_MEM_BUFFER
    | PVI_MEDIA_DUMMY
    | PVI_MEDIA_DEBUG
    | PVI_MAIN_WINDOW

/// <summary>
///  This class has all of XAML configurations that is used at GUI controll.
/// </summary>
/// <param name="m_ExeDir">
///  The directory path name where the application EXE file is stored.
/// </param>
/// <remarks>
///  If failed to load any XAML file, constractor raised exception.
///  In this case, application must be terminated.
/// </remarks>
type XamlLoader ( m_ExeDir : string ) =

    /// Icon images collection
    let m_UIEleme : FrozenDictionary< PropertyViewIndex, string > = 
        let dirPath = Functions.AppendPathName m_ExeDir "XAML"
        [|
            ( PropertyViewIndex.PVI_LOGIN_DIALOG, "LoginDialog" );
            ( PropertyViewIndex.PVI_EDIT_IPWHITELIST, "EditIPWhiteList" );
            ( PropertyViewIndex.PVI_EDIT_DEBUG_TRAP, "EditDebugTrap" );
            ( PropertyViewIndex.PVI_EDIT_TARGET_NAME, "EditTargetName" );
            ( PropertyViewIndex.PVI_CREATE_MEDIA_FILE, "CreateMediaFile" );
            ( PropertyViewIndex.PVI_INITIAL, "InitialPropPage" );
            ( PropertyViewIndex.PVI_CONTROLLER, "ControllerPropPage" );
            ( PropertyViewIndex.PVI_TARGET_DEVICE, "TargetDevicePropPage" );
            ( PropertyViewIndex.PVI_NETWORK_PORTAL, "NetworkPortalPropPage" );
            ( PropertyViewIndex.PVI_TARGET_GROUP, "TargetGroupPropPage" );
            ( PropertyViewIndex.PVI_TARGET, "TargetPropPage" );
            ( PropertyViewIndex.PVI_LU_BLOCK_DEVICE, "BlockDeviceLUPropPage" );
            ( PropertyViewIndex.PVI_LU_DUMMY_DEVICE, "DummyDeviceLUPropPage" );
            ( PropertyViewIndex.PVI_MEDIA_PLAIN_FILE, "PlainFileMediaPropPage" );
            ( PropertyViewIndex.PVI_MEDIA_MEM_BUFFER, "MemBufferMediaPropPage" );
            ( PropertyViewIndex.PVI_MEDIA_DUMMY, "DummyMediaPropPage" );
            ( PropertyViewIndex.PVI_MEDIA_DEBUG, "DebugMediaPropPage" );
            ( PropertyViewIndex.PVI_MAIN_WINDOW, "MainWindow" );
        |]
        |> Seq.map ( fun ( pid, fn ) ->
            ( pid, Functions.AppendPathName dirPath ( sprintf "%s.xaml" fn ) )
        )
        |> Seq.map ( fun ( pid, fn ) ->
            ( pid, File.ReadAllText fn )
        )
        |> Functions.ToFrozenDictionary

    /// <summary>
    ///  get property pages
    /// </summary>
    /// <param name="idx">
    ///  Index of property page.
    /// </param>
    /// <returns>
    ///  property page UIElement
    /// </returns>
    member _.Get( idx : PropertyViewIndex ) : UIElement =
        XamlReader.Parse( m_UIEleme.Item idx ) :?> UIElement
