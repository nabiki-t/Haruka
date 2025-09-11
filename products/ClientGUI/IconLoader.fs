//=============================================================================
// Haruka Software Storage.
// IconLoader.fs : Load icon images that used for Haruka client GUI.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.IO
open System.Windows
open System.Windows.Forms
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Markup
open System.Collections.Generic
open System.Collections.Frozen

open Haruka.Constants
open Haruka.Commons
open Haruka.Client

//=============================================================================
// Type definition

/// Constants that represents the icon image.
type IconImageIndex =
    | III_CONTROLLER
    | III_CONTROLLER_MODIFIED
    | III_TARGET_DEVICE_UNLOADED
    | III_TARGET_DEVICE_MODIFIED
    | III_TARGET_DEVICE_RUNNING
    | III_NETWORK_PORTAL
    | III_TARGET_GROUP_UNLOADED
    | III_TARGET_GROUP_MODIFIED
    | III_TARGET_GROUP_LOADED
    | III_TARGET_GROUP_ACTIVE
    | III_TARGET
    | III_LU_BLOCK_DEVICE
    | III_LU_DUMMY_DEVICE
    | III_MEDIA_PLAIN_FILE
    | III_MEDIA_MEM_BUFFER
    | III_MEDIA_DUMMY
    | III_MEDIA_DEBUG
    | III_STATUS_RUNNING
    | III_STATUS_UNLOADED
    | III_STATUS_MODIFIED

//=============================================================================
// Class implementation

/// <summary>
///  This class has all of icon images that is used at GUI controll.
/// </summary>
/// <param name="m_ExeDir">
///  The directory path name where the application EXE file is stored.
/// </param>
type IconLoader ( m_ExeDir : string ) =

    /// Icon images collection
    let m_Icons : FrozenDictionary< IconImageIndex, RenderTargetBitmap option > =
        let ssf =
            let h1 = Screen.PrimaryScreen.Bounds.Height
            let h2 = SystemParameters.PrimaryScreenHeight
            ( float h1 ) / h2

        let dirPath = Functions.AppendPathName m_ExeDir "icon"

        [|
            ( IconImageIndex.III_CONTROLLER, "Controller" )
            ( IconImageIndex.III_CONTROLLER_MODIFIED, "ControllerModified" )
            ( IconImageIndex.III_TARGET_DEVICE_UNLOADED, "TargetDevieUnloaded" )
            ( IconImageIndex.III_TARGET_DEVICE_MODIFIED, "TargetDevieModified" )
            ( IconImageIndex.III_TARGET_DEVICE_RUNNING, "TargetDevieRunning" )
            ( IconImageIndex.III_NETWORK_PORTAL, "NetworkPortal" )
            ( IconImageIndex.III_TARGET_GROUP_UNLOADED, "TargetGroupUnloaded" )
            ( IconImageIndex.III_TARGET_GROUP_MODIFIED, "TargetGroupModified" )
            ( IconImageIndex.III_TARGET_GROUP_LOADED, "TargetGroupLoaded" )
            ( IconImageIndex.III_TARGET_GROUP_ACTIVE, "TargetGroupActive" )
            ( IconImageIndex.III_TARGET, "Target" )
            ( IconImageIndex.III_LU_BLOCK_DEVICE, "LUBlockDevice" )
            ( IconImageIndex.III_LU_DUMMY_DEVICE, "LUDummyDevice" )
            ( IconImageIndex.III_MEDIA_PLAIN_FILE, "MediaPlainFile" )
            ( IconImageIndex.III_MEDIA_MEM_BUFFER, "MediaMembuffer" )
            ( IconImageIndex.III_MEDIA_DUMMY, "MediaDummy" )
            ( IconImageIndex.III_MEDIA_DEBUG, "MediaDebug" )
            ( IconImageIndex.III_STATUS_RUNNING, "StatusRunning" )
            ( IconImageIndex.III_STATUS_UNLOADED, "StatusUnloaded" )
            ( IconImageIndex.III_STATUS_MODIFIED, "StatusModified" )
        |]
        |> Seq.map ( fun ( tid, tn ) -> 
            try
                let db =
                    let fname = Functions.AppendPathName dirPath ( sprintf "%s.xaml" tn )
                    let data = File.ReadAllText fname
                    let rd = XamlReader.Parse( data ) :?> ResourceDictionary
                    rd.[ tn ] :?> DrawingBrush

                let drawingVisual =
                    let d = new DrawingVisual()
                    use context = d.RenderOpen()
                    context.DrawDrawing( db.Drawing )
                    d

                let icoW = GuiConst.ICO_WIDTH * ssf
                let icoH = GuiConst.ICO_HEIGHT * ssf
                let dpiX = 96.0 * icoW / GuiConst.SOURCE_ICO_WIDTH
                let dpiY = 96.0 * icoH / GuiConst.SOURCE_ICO_HEIGHT
                let rtb = new RenderTargetBitmap( int icoW, int icoH, dpiX, dpiY, PixelFormats.Default )
                rtb.Render( drawingVisual )
                ( tid, Some( rtb ) )
            with
            | _ ->
                ( tid, None )
        )
        |> Functions.ToFrozenDictionary

    /// <summary>
    ///  get icon image
    /// </summary>
    /// <param name="idx">
    ///  Index of the icon images.
    /// </param>
    /// <returns>
    ///  loaded icon image, or None if it failed to load the icon image.
    /// </returns>
    member _.Get( idx : IconImageIndex ) : ImageSource option =
        let r = m_Icons.GetValueOrDefault( idx, None )
        if r.IsSome then
            Some( r.Value :> ImageSource )
        else
            None


