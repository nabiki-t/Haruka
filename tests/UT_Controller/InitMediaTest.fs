namespace Haruka.Test.UT.Controller

open System
open System.IO
open System.Collections.Generic

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Controller
open Haruka.IODataTypes
open Haruka.Test

type InitMedia_Test () =

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    static member Init ( caseName : string ) =
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "InitMedia_Test_" + caseName
        if Directory.Exists dname then GlbFunc.DeleteDir dname
        GlbFunc.CreateDir dname |> ignore
        dname

    [<Fact>]
    member _.CreatePlainFile_001() =
        let dname = InitMedia_Test.Init "CreatePlainFile_001"
        let fname = Functions.AppendPathName dname "CreatePlainFile_001.txt"
        let st = new StringTable( "" )

        let d =
            [
                ( "/f", EnteredValue.EV_String( fname ) );
                ( "/s", EnteredValue.EV_int64( 1L ) );
                ( "/x", EnteredValue.EV_NoValue );
            ]
            |> Seq.map KeyValuePair
            |> Dictionary< string, EnteredValue >
        let cmd = CommandParser( CtrlCmdType.InitMedia_PlainFile, d, Array.empty )

        let ms = new MemoryStream()
        let ws = new StreamWriter( ms )

        let r = InitMedia.CreatePlainFile ws st cmd
        Assert.True r

        ws.Flush()
        ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let rs = new StreamReader( ms )

        let m1 = InitMediaMessage.ReaderWriter.LoadString( rs.ReadLine() )
        match m1.LineType with
        | InitMediaMessage.U_Start _ ->
            ()
        | _ -> Assert.Fail __LINE__

        let m2 = InitMediaMessage.ReaderWriter.LoadString( rs.ReadLine() )
        match m2.LineType with
        | InitMediaMessage.U_Progress x ->
            Assert.True(( x = 0uy ))
        | _ -> Assert.Fail __LINE__

        let m3 = InitMediaMessage.ReaderWriter.LoadString( rs.ReadLine() )
        match m3.LineType with
        | InitMediaMessage.U_CreateFile x ->
            Assert.True(( x = fname ))
        | _ -> Assert.Fail __LINE__

        let m4 = InitMediaMessage.ReaderWriter.LoadString( rs.ReadLine() )
        match m4.LineType with
        | InitMediaMessage.U_Progress x ->
            Assert.True(( x = 100uy ))
        | _ -> Assert.Fail __LINE__

        let m5 = InitMediaMessage.ReaderWriter.LoadString( rs.ReadLine() )
        match m5.LineType with
        | InitMediaMessage.U_End _ ->
            ()
        | _ -> Assert.Fail __LINE__

        Assert.True(( File.Exists fname ))
        Assert.True(( FileInfo( fname ).Length = 1L ))

        GlbFunc.DeleteDir dname
        GlbFunc.AllDispose [| ms; ws; rs; |]

    [<Fact>]
    member _.CreatePlainFile_002() =
        let fname = String.replicate ( Constants.MAX_FILENAME_STR_LENGTH + 1 ) "a"
        let st = new StringTable( "" )

        let d =
            [
                ( "/f", EnteredValue.EV_String( fname ) );
                ( "/s", EnteredValue.EV_int64( 1L ) );
                ( "/x", EnteredValue.EV_NoValue );
            ]
            |> Seq.map KeyValuePair
            |> Dictionary< string, EnteredValue >
        let cmd = CommandParser( CtrlCmdType.InitMedia_PlainFile, d, Array.empty )

        let ms = new MemoryStream()
        let ws = new StreamWriter( ms )

        let r = InitMedia.CreatePlainFile ws st cmd
        Assert.False r

        ws.Flush()
        ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let rs = new StreamReader( ms )

        let m1 = InitMediaMessage.ReaderWriter.LoadString( rs.ReadLine() )
        match m1.LineType with
        | InitMediaMessage.U_ErrorMessage x ->
            Assert.True(( x.StartsWith "Too long file name." ))
        | _ -> Assert.Fail __LINE__

        let m5 = InitMediaMessage.ReaderWriter.LoadString( rs.ReadLine() )
        match m5.LineType with
        | InitMediaMessage.U_End x ->
            Assert.True(( x.StartsWith "failed" ))
        | _ -> Assert.Fail __LINE__

        GlbFunc.AllDispose [| ms; ws; rs; |]

    [<Fact>]
    member _.CreatePlainFile_003() =
        let dname = InitMedia_Test.Init "CreatePlainFile_003"
        let fname = Functions.AppendPathName dname "CreatePlainFile_003.txt"
        let st = new StringTable( "" )

        File.WriteAllText( fname, "" )

        let d =
            [
                ( "/f", EnteredValue.EV_String( fname ) );
                ( "/s", EnteredValue.EV_int64( 1L ) );
                ( "/x", EnteredValue.EV_NoValue );
            ]
            |> Seq.map KeyValuePair
            |> Dictionary< string, EnteredValue >
        let cmd = CommandParser( CtrlCmdType.InitMedia_PlainFile, d, Array.empty )

        let ms = new MemoryStream()
        let ws = new StreamWriter( ms )

        let r = InitMedia.CreatePlainFile ws st cmd
        Assert.False r

        ws.Flush()
        ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let rs = new StreamReader( ms )

        let m1 = InitMediaMessage.ReaderWriter.LoadString( rs.ReadLine() )
        match m1.LineType with
        | InitMediaMessage.U_Start _ ->
            ()
        | _ -> Assert.Fail __LINE__

        let m2 = InitMediaMessage.ReaderWriter.LoadString( rs.ReadLine() )
        match m2.LineType with
        | InitMediaMessage.U_Progress x ->
            Assert.True(( x = 0uy ))
        | _ -> Assert.Fail __LINE__

        let m1 = InitMediaMessage.ReaderWriter.LoadString( rs.ReadLine() )
        match m1.LineType with
        | InitMediaMessage.U_ErrorMessage _ ->
            ()
        | _ -> Assert.Fail __LINE__

        let m5 = InitMediaMessage.ReaderWriter.LoadString( rs.ReadLine() )
        match m5.LineType with
        | InitMediaMessage.U_End x ->
            Assert.True(( x.StartsWith "failed" ))
        | _ -> Assert.Fail __LINE__

        Assert.True(( File.Exists fname ))
        Assert.True(( FileInfo( fname ).Length = 0L ))

        GlbFunc.DeleteDir dname
        GlbFunc.AllDispose [| ms; ws; rs; |]

    [<Fact>]
    member _.CreatePlainFile_004() =
        let dname = InitMedia_Test.Init "CreatePlainFile_004"
        let fname = Functions.AppendPathName dname "CreatePlainFile_004.txt"
        let st = new StringTable( "" )

        GlbFunc.DeleteDir dname

        let d =
            [
                ( "/f", EnteredValue.EV_String( fname ) );
                ( "/s", EnteredValue.EV_int64( 1L ) );
                ( "/x", EnteredValue.EV_NoValue );
            ]
            |> Seq.map KeyValuePair
            |> Dictionary< string, EnteredValue >
        let cmd = CommandParser( CtrlCmdType.InitMedia_PlainFile, d, Array.empty )

        let ms = new MemoryStream()
        let ws = new StreamWriter( ms )

        let r = InitMedia.CreatePlainFile ws st cmd
        Assert.False r

        ws.Flush()
        ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let vLines = List< string >()
        use rs = new StreamReader( ms )
        while not rs.EndOfStream do
            vLines.Add( rs.ReadLine() )

        Assert.True(( vLines.Count = 4 ))

        let m1 = InitMediaMessage.ReaderWriter.LoadString( vLines.[0] )
        match m1.LineType with
        | InitMediaMessage.U_Start _ ->
            ()
        | _ -> Assert.Fail __LINE__

        let m2 = InitMediaMessage.ReaderWriter.LoadString( vLines.[1] )
        match m2.LineType with
        | InitMediaMessage.U_Progress x ->
            Assert.True(( x = 0uy ))
        | _ -> Assert.Fail __LINE__

        let m1 = InitMediaMessage.ReaderWriter.LoadString( vLines.[2] )
        match m1.LineType with
        | InitMediaMessage.U_ErrorMessage _ ->
            ()
        | _ -> Assert.Fail __LINE__

        let m5 = InitMediaMessage.ReaderWriter.LoadString( vLines.[3] )
        match m5.LineType with
        | InitMediaMessage.U_End x ->
            Assert.True(( x.StartsWith "failed" ))
        | _ -> Assert.Fail __LINE__

        Assert.False(( File.Exists fname ))
        GlbFunc.AllDispose [| ms; ws; rs; |]
