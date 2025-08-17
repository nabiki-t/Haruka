//=============================================================================
// Haruka Software Storage.
// InitMedia.fs : Create an initial media file.

//=============================================================================
// Namespace declaration

namespace Haruka.Controller

//=============================================================================
// Import declaration

open System
open System.IO

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Type definition

type InitMedia() =

    /// <summary>
    ///  Output a message to stdout.
    /// </summary>
    /// <param name="xml">
    ///  Write the message by XML string or not.
    /// </param>
    /// <param name="msg">
    ///  message which shall be output.
    /// </param>
    static let OutputMsg ( xml : bool ) ( msg : InitMediaMessage.T_LineType ) : unit =
        if xml then
            printfn "%s" ( InitMediaMessage.ReaderWriter.ToString { LineType = msg } )
        else
            match msg with
            | InitMediaMessage.U_Start( _ ) ->
                printfn "START"
            | InitMediaMessage.U_CreateFile( x ) ->
                printfn "CREATE FILE : %s" x
            | InitMediaMessage.U_Progress( x ) ->
                printfn "PROGRESS    : %d" x
            | InitMediaMessage.U_End( x ) ->
                printfn "END"
            | InitMediaMessage.U_ErrorMessage( x ) ->
                printfn "ERROR       : %s" x

    /// <summary>
    ///  Create palin file.
    /// </summary>
    /// <param name="st">
    ///  Message table. 
    /// </param>
    /// <param name="cmd">
    ///  command line arguments.
    /// </param>
    static member CreatePlainFile ( st : StringTable ) ( cmd : CommandParser<CtrlCmdType> ) : bool =
        let fname = cmd.DefaultNamedString "/f" ""
        let fsize = cmd.DefaultNamedInt64 "/s" 0L
        let xml = cmd.NamedArgs.ContainsKey( "/x" )

        if fname.Length > Constants.MAX_FILENAME_STR_LENGTH then
            OutputMsg xml ( InitMediaMessage.U_ErrorMessage "Too long file name." )
            OutputMsg xml ( InitMediaMessage.U_End "failed" )
            false
        else
            OutputMsg xml ( InitMediaMessage.U_Start() )
            OutputMsg xml ( InitMediaMessage.U_Progress 0uy )

            try
                use f = File.Open( fname, FileMode.CreateNew )
                OutputMsg xml ( InitMediaMessage.U_CreateFile fname )

                f.SetLength fsize
                f.Close()
                OutputMsg xml ( InitMediaMessage.U_Progress 100uy )
                OutputMsg xml ( InitMediaMessage.U_End "succeed" )
                true
            with
            | _ as x ->
                OutputMsg xml ( InitMediaMessage.U_ErrorMessage x.Message.[ .. Constants.INITMEDIA_MAX_ERRMSG_LENGTH ] )
                OutputMsg xml ( InitMediaMessage.U_End "failed" )
                false
