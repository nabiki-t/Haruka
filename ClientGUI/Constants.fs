//=============================================================================
// Haruka Software Storage.
// Constants.fs : Constants definitions for ClientGUI.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.Windows

open Haruka.Constants

/// <summary>
///  Data type that represents result of dialog.
/// </summary>
type DialogResult< 'a > =
    | Ok of 'a
    | Cancel

/// <summary>
///  reason of closing window.
/// </summary>
type WinCloseReason< 'a > =
    | Internal  // By internal event
    | User      // By user operation


[<NoComparison>]
type SessionTreeItemType =
    | Session of ( RoutedEventHandler * TSIH_T )
    | Connection of ( RoutedEventHandler * CID_T * CONCNT_T )

/// <summary>
///  Constants definitions for ClientGUI
/// </summary>
type GuiConst() =


    /// Icon width on GUI
    static member ICO_WIDTH : float =
        24.0

    /// Icon height on GUI
    static member ICO_HEIGHT : float =
        24.0

    /// Icon source image width
    static member SOURCE_ICO_WIDTH : float =
        64.0

    /// Icon source image height
    static member SOURCE_ICO_HEIGHT : float =
        64.0

    static member USAGE_GRAPH_PNT_CNT : int =
        int ( Constants.RESCOUNTER_LENGTH_SEC / Constants.RECOUNTER_SPAN_SEC )

/// <summary>
///  Datatype for parameter list view
/// </summary>
type ParamListViewItem = {
    Name : string;
    Value : string;
}
