//=============================================================================
// Haruka Software Storage.
// UserOpeStat.fs : Recording temporary values ​​related to user actions
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open Microsoft.Win32

//=============================================================================
// Class implementation

/// <summary>
///  Definition of UserOpeStat class.
/// </summary>
type UserOpeStat() =

    /// The name that will be the root of the registry key.
    static member private ROOT_REG_NAME : string =
        @"Software\nabiki_t\Haruka\ClientGUI"

    static member MainWindowWidth
        with get() : int =
            UserOpeStat.GetValue_DWORD "MainWindowWidth" 640
        and set ( x : int ) : unit =
            UserOpeStat.SetValue_DWORD "MainWindowWidth" x

    static member MainWindowHeight
        with get() : int =
            UserOpeStat.GetValue_DWORD "MainWindowHeight" 500
        and set ( x : int ) : unit =
            UserOpeStat.SetValue_DWORD "MainWindowHeight" x

    static member LoginDialog_Type
        with get() : int =
            UserOpeStat.GetValue_DWORD "LoginDialog_Type" 0
        and set ( x : int ) : unit =
            UserOpeStat.SetValue_DWORD "LoginDialog_Type" x

    static member LoginDialog_Directory
        with get() : string =
            UserOpeStat.GetValue_SZ "LoginDialog_Directory" ""
        and set ( x : string ) : unit =
            UserOpeStat.SetValue_SZ "LoginDialog_Directory" x

    static member LoginDialog_HostName
        with get() : string =
            UserOpeStat.GetValue_SZ "LoginDialog_HostName" ""
        and set ( x : string ) : unit =
            UserOpeStat.SetValue_SZ "LoginDialog_HostName" x

    static member LoginDialog_PortNumber
        with get() : string =
            UserOpeStat.GetValue_SZ "LoginDialog_PortNumber" ""
        and set ( x : string ) : unit =
            UserOpeStat.SetValue_SZ "LoginDialog_PortNumber" x


    static member GetExpanded( dlgName : string ) ( ctrlName : string ) : bool =
        UserOpeStat.GetValue_bool ( dlgName + "_" + ctrlName + "_Expanded" ) true

    static member SetExpanded( dlgName : string ) ( ctrlName : string ) ( x : bool ) : unit =
        UserOpeStat.SetValue_bool ( dlgName + "_" + ctrlName + "_Expanded" ) x



    /// <summary>
    ///  Set an integer value to the registry.
    /// </summary>
    /// <param name="name">
    ///  The name used to store the value.
    /// </param>
    /// <param name="v">
    ///  Integer value should be write to the regstry.
    /// </param>
    static member private SetValue_DWORD ( name : string ) ( v : int32 ) : unit =
        use r = Registry.CurrentUser.CreateSubKey UserOpeStat.ROOT_REG_NAME
        r.SetValue( name, v, RegistryValueKind.DWord )
        r.Close()

    /// <summary>
    ///  Set a boolean value to registry.
    /// </summary>
    /// <param name="name">
    ///  The name used to store the value.
    /// </param>
    /// <param name="v">
    ///  Boolean value should be write to the regstry.
    /// </param>
    static member private SetValue_bool ( name : string ) ( v : bool ) : unit =
        let b = if v then 1 else 0
        UserOpeStat.SetValue_DWORD name b

    /// <summary>
    ///  Set a string value to registry.
    /// </summary>
    /// <param name="name">
    ///  The name used to store the value.
    /// </param>
    /// <param name="v">
    ///  String value should be write to the regstry.
    /// </param>
    static member private SetValue_SZ ( name : string ) ( v : string ) : unit =
        use r = Registry.CurrentUser.CreateSubKey UserOpeStat.ROOT_REG_NAME
        r.SetValue( name, v, RegistryValueKind.String )
        r.Close()

    /// <summary>
    ///  Get the integer value from the registry.
    ///  If failed, it sets default value to the registry and returns this default value.
    /// </summary>
    /// <param name="name">
    ///  The name had used to store the value.
    /// </param>
    /// <param name="d">
    ///  Default value, it will be return if failed.
    /// </param>
    /// <returns>
    ///  Restored value.
    /// </returns>
    static member private GetValue_DWORD ( name : string ) ( d : Int32 ) : Int32 =
        use r = Registry.CurrentUser.CreateSubKey UserOpeStat.ROOT_REG_NAME
        try
            try
                r.GetValue( name ) :?> Int32
            with
            | _ ->
                UserOpeStat.SetValue_DWORD name d
                d
        finally
            r.Close()

    /// <summary>
    ///  Get the string value from the registry.
    ///  If failed, it sets default value to the registry and returns this default value.
    /// </summary>
    /// <param name="name">
    ///  The name had used to store the value.
    /// </param>
    /// <param name="d">
    ///  Default value, it will be return if failed.
    /// </param>
    /// <returns>
    ///  Restored value.
    /// </returns>
    static member private GetValue_SZ ( name : string ) ( d : string ) : string =
        use r = Registry.CurrentUser.CreateSubKey UserOpeStat.ROOT_REG_NAME
        try
            try
                r.GetValue( name ) :?> string
            with
            | _ ->
                UserOpeStat.SetValue_SZ name d
                d
        finally
            r.Close()

    /// <summary>
    ///  Get the boolean value from the registry.
    ///  If failed, it sets default value to the registry and returns this default value.
    /// </summary>
    /// <param name="name">
    ///  The name had used to store the value.
    /// </param>
    /// <param name="d">
    ///  Default value, it will be return if failed.
    /// </param>
    /// <returns>
    ///  Restored value.
    /// </returns>
    static member private GetValue_bool ( name : string ) ( d : bool ) : bool =
        let b = if d then 1 else 0
        ( UserOpeStat.GetValue_DWORD name b ) <> 0
