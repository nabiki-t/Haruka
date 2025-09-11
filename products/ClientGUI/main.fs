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

//=============================================================================
// module implementation

[<EntryPoint>]
[<STAThread>]
let main ( argv : string[] ) : int =
    let curExeDir = 
        let curExeName = System.Reflection.Assembly.GetEntryAssembly()
        Path.GetDirectoryName curExeName.Location


    let mainWindow = new MainWindow( curExeDir )
    mainWindow.Show( new Application() )

