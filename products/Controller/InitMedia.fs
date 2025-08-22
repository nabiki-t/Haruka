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
    /// <param name="outfile">
    ///  Output file stream.
    /// </param>
    /// <param name="xml">
    ///  Write the message by XML string or not.
    /// </param>
    /// <param name="msg">
    ///  message which shall be output.
    /// </param>
    static let OutputMsg ( outfile : TextWriter ) ( xml : bool ) ( msg : InitMediaMessage.T_LineType ) : unit =
        if xml then
            fprintfn outfile "%s" ( InitMediaMessage.ReaderWriter.ToString { LineType = msg } )
        else
            match msg with
            | InitMediaMessage.U_Start( _ ) ->
                fprintfn outfile "START"
            | InitMediaMessage.U_CreateFile( x ) ->
                fprintfn outfile "CREATE FILE : %s" x
            | InitMediaMessage.U_Progress( x ) ->
                fprintfn outfile "PROGRESS    : %d" x
            | InitMediaMessage.U_End( x ) ->
                fprintfn outfile "END"
            | InitMediaMessage.U_ErrorMessage( x ) ->
                fprintfn outfile "ERROR       : %s" x

    /// <summary>
    ///  Create palin file.
    /// </summary>
    /// <param name="outfile">
    ///  Output file stream.
    /// </param>
    /// <param name="st">
    ///  Message table. 
    /// </param>
    /// <param name="cmd">
    ///  command line arguments.
    /// </param>
    static member CreatePlainFile ( outfile : TextWriter ) ( st : StringTable ) ( cmd : CommandParser<CtrlCmdType> ) : bool =
        let fname = cmd.DefaultNamedString "/f" ""
        let fsize = cmd.DefaultNamedInt64 "/s" 0L
        let xml = cmd.NamedArgs.ContainsKey( "/x" )

        if fname.Length > Constants.MAX_FILENAME_STR_LENGTH then
            OutputMsg outfile xml ( InitMediaMessage.U_ErrorMessage "Too long file name." )
            OutputMsg outfile xml ( InitMediaMessage.U_End "failed" )
            false
        else
            OutputMsg outfile xml ( InitMediaMessage.U_Start() )
            OutputMsg outfile xml ( InitMediaMessage.U_Progress 0uy )

            try
                use f = File.Open( fname, FileMode.CreateNew )
                OutputMsg outfile xml ( InitMediaMessage.U_CreateFile fname )

                f.SetLength fsize
                f.Close()
                OutputMsg outfile xml ( InitMediaMessage.U_Progress 100uy )
                OutputMsg outfile xml ( InitMediaMessage.U_End "succeed" )
                true
            with
            | _ as x ->
                OutputMsg outfile xml ( InitMediaMessage.U_ErrorMessage x.Message.[ .. Constants.INITMEDIA_MAX_ERRMSG_LENGTH ] )
                OutputMsg outfile xml ( InitMediaMessage.U_End "failed" )
                false
