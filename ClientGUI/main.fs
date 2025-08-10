//=============================================================================
// Haruka Software Storage.
// main.fs : Harula client GUI main module.
//

//=============================================================================
// module name

module Haruka.ClientGUI.main

//=============================================================================
// Import declaration

open System
open System.IO
open System.Windows
open System.Xml
open System.Reflection


open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes
open Haruka.Client

[<EntryPoint>]
[<STAThread>]
let main ( argv : string[] ) : int =
    let curExeDir = 
        let curExeName = System.Reflection.Assembly.GetEntryAssembly()
        Path.GetDirectoryName curExeName.Location


    let mainWindow = new MainWindow( curExeDir )
    mainWindow.Show( new Application() )

