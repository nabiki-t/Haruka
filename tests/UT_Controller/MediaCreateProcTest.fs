//=============================================================================
// Haruka Software Storage.
// MediaCreateProcTest.fs : Test cases for MediaCreateProc class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Controller

//=============================================================================
// Import declaration

open System
open System.IO

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test
open Haruka.Controller
open Haruka.IODataTypes
open System.Diagnostics
open System.Threading

//=============================================================================
// Class implementation

type MediaCreateProc_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    static let m_Stub_ExePath = 
        let curExeName = System.Reflection.Assembly.GetEntryAssembly()
        let curExeDir = Path.GetDirectoryName curExeName.Location
        Functions.AppendPathName curExeDir "TestCommon.exe"

    static member Init ( caseName : string ) =
        let dname = Functions.AppendPathName ( Path.GetTempPath() ) "MediaCreateProc_Test_" + caseName
        if Directory.Exists dname then GlbFunc.DeleteDir dname
        GlbFunc.CreateDir dname |> ignore
        dname

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constractor_001() =
        let dname = MediaCreateProc_Test.Init "Constractor_001"
        let p : HarukaCtrlerCtrlReq.T_PlainFile = {
            FileName = "a g b";
            FileSize = 1L;
        }
        let spc = new StubProcCtrl( dname )
        let m = MediaCreateProc( HarukaCtrlerCtrlReq.U_PlainFile( p ), dname, m_Stub_ExePath )
        spc.Wait()

        let args = spc.GetArguments()
        Assert.True(( args.[0] = "IM" ))
        Assert.True(( args.[1] = "PLAINFILE" ))
        Assert.True(( args.[2] = "/f" ))
        Assert.True(( args.[3] = "a g b" ))
        Assert.True(( args.[4] = "/s" ))
        Assert.True(( args.[5] = "1" ))
        Assert.True(( args.[6] = "/x" ))

        let pc = PrivateCaller( m )
        let m_Proc = pc.GetField( "m_Proc" ) :?> Process option
        Assert.True(( m_Proc.IsSome ))

        spc.Terminate( 0 )
        spc.Dispose()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Constractor_002() =
        let dname = MediaCreateProc_Test.Init "Constractor_002"
        let p : HarukaCtrlerCtrlReq.T_PlainFile = {
            FileName = "a";
            FileSize = 1L;
        }
        let m = MediaCreateProc( HarukaCtrlerCtrlReq.U_PlainFile( p ), dname, "aaaa" )

        let pc = PrivateCaller( m )
        let m_Proc = pc.GetField( "m_Proc" ) :?> Process option
        Assert.True(( m_Proc.IsNone ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.procMediaCreateStdout_001() =
        let dname = MediaCreateProc_Test.Init "procMediaCreateStdout_001"
        let p : HarukaCtrlerCtrlReq.T_PlainFile = {
            FileName = "a";
            FileSize = 1L;
        }
        let fname1 = Functions.AppendPathName dname "aaa.txt"
        let fname2 = Functions.AppendPathName dname "bbb.txt"

        File.WriteAllText( fname1, "" )
        File.WriteAllText( fname2, "" )

        let spc = new StubProcCtrl( dname )
        let m = MediaCreateProc( HarukaCtrlerCtrlReq.U_PlainFile( p ), dname, m_Stub_ExePath )
        spc.Wait()

        let msg1 = InitMediaMessage.ReaderWriter.ToString { LineType = InitMediaMessage.U_CreateFile( fname1 ) }
        spc.SetStdOutResult msg1

        let msg2 = InitMediaMessage.ReaderWriter.ToString { LineType = InitMediaMessage.U_CreateFile( fname2 ) }
        spc.SetStdOutResult msg2

        let mutable cnt = 0
        while m.CreatedFile.Length <> 2 && cnt < 200 do
            Thread.Sleep 5
            cnt <- cnt + 1

        Assert.True(( m.CreatedFile.Length = 2 ))
        Assert.True(( m.CreatedFile.[0] = fname1 ))
        Assert.True(( m.CreatedFile.[1] = fname2 ))

        spc.Terminate( 0 )  // normal exit
        spc.Dispose()

        cnt <- 0
        while cnt < 200 do
            match m.Progress with
            | NormalEnd( _ ) ->
                cnt <- 99999
            | _ ->
                Thread.Sleep 5
                cnt <- cnt + 1
        Assert.True(( cnt = 99999 ))

        Assert.True(( File.Exists fname1 ))
        Assert.True(( File.Exists fname2 ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.procMediaCreateStdout_002() =
        let dname = MediaCreateProc_Test.Init "procMediaCreateStdout_002"
        let p : HarukaCtrlerCtrlReq.T_PlainFile = {
            FileName = "a";
            FileSize = 1L;
        }
        let fname1 = Functions.AppendPathName dname "aaa.txt"
        let fname2 = Functions.AppendPathName dname "bbb.txt"

        File.WriteAllText( fname1, "" )
        File.WriteAllText( fname2, "" )

        let spc = new StubProcCtrl( dname )
        let m = MediaCreateProc( HarukaCtrlerCtrlReq.U_PlainFile( p ), dname, m_Stub_ExePath )
        spc.Wait()

        let msg1 = InitMediaMessage.ReaderWriter.ToString { LineType = InitMediaMessage.U_CreateFile( fname1 ) }
        spc.SetStdOutResult msg1

        let msg2 = InitMediaMessage.ReaderWriter.ToString { LineType = InitMediaMessage.U_CreateFile( fname2 ) }
        spc.SetStdOutResult msg2

        let mutable cnt = 0
        while m.CreatedFile.Length <> 2 && cnt < 200 do
            Thread.Sleep 5
            cnt <- cnt + 1

        Assert.True(( m.CreatedFile.Length = 2 ))
        Assert.True(( m.CreatedFile.[0] = fname1 ))
        Assert.True(( m.CreatedFile.[1] = fname2 ))

        spc.Terminate( 1 )  // error exit
        spc.Dispose()

        cnt <- 0
        while cnt < 200 do
            match m.Progress with
            | AbnormalEnd( _ ) ->
                cnt <- 99999
            | _ ->
                Thread.Sleep 5
                cnt <- cnt + 1
        Assert.True(( cnt = 99999 ))

        Assert.False(( File.Exists fname1 ))
        Assert.False(( File.Exists fname2 ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.procMediaCreateStdout_003() =
        let dname = MediaCreateProc_Test.Init "procMediaCreateStdout_003"
        let p : HarukaCtrlerCtrlReq.T_PlainFile = {
            FileName = "a";
            FileSize = 1L;
        }

        let spc = new StubProcCtrl( dname )
        let m = MediaCreateProc( HarukaCtrlerCtrlReq.U_PlainFile( p ), dname, m_Stub_ExePath )
        spc.Wait()
        spc.Terminate( 1 )
        spc.Dispose()

        let mutable cnt = 0
        while cnt < 200 do
            match m.Progress with
            | AbnormalEnd( _ ) ->
                cnt <- 99999
            | _ ->
                Thread.Sleep 5
                cnt <- cnt + 1
        Assert.True(( cnt = 99999 ))

        Assert.True(( m.CreatedFile.Length = 0 ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.procMediaCreateStdout_004() =
        let dname = MediaCreateProc_Test.Init "procMediaCreateStdout_004"
        let p : HarukaCtrlerCtrlReq.T_PlainFile = {
            FileName = "a";
            FileSize = 1L;
        }
        let fname1 = "aaa"
        let spc = new StubProcCtrl( dname )
        let m = MediaCreateProc( HarukaCtrlerCtrlReq.U_PlainFile( p ), dname, m_Stub_ExePath )
        spc.Wait()

        let msg1 = InitMediaMessage.ReaderWriter.ToString { LineType = InitMediaMessage.U_CreateFile( fname1 ) }
        spc.SetStdOutResult msg1

        let mutable cnt = 0
        while m.CreatedFile.Length = 0 && cnt < 200 do
            Thread.Sleep 5
            cnt <- cnt + 1

        Assert.True(( m.CreatedFile.Length = 1 ))
        Assert.True(( m.CreatedFile.[0] = fname1 ))

        spc.Terminate( 1 )
        spc.Dispose()

        cnt <- 0
        while cnt < 200 do
            match m.Progress with
            | AbnormalEnd( _ ) ->
                cnt <- 99999
            | _ ->
                Thread.Sleep 5
                cnt <- cnt + 1
        Assert.True(( cnt = 99999 ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.procMediaCreateStdout_005() =
        let mutable cnt = 0
        let dname = MediaCreateProc_Test.Init "procMediaCreateStdout_005"
        let p : HarukaCtrlerCtrlReq.T_PlainFile = {
            FileName = "a";
            FileSize = 1L;
        }

        let spc = new StubProcCtrl( dname )
        let m = MediaCreateProc( HarukaCtrlerCtrlReq.U_PlainFile( p ), dname, m_Stub_ExePath )
        spc.Wait()

        match m.Progress with
        | NotStarted ->
            ()
        | _ ->
            Assert.Fail __LINE__

        let msg1 = InitMediaMessage.ReaderWriter.ToString { LineType = InitMediaMessage.U_Progress( 1uy ) }
        spc.SetStdOutResult msg1

        cnt <- 0
        while cnt < 100 do
            match m.Progress with
            | ProgressCreation( x ) ->
                Assert.True(( x = 1uy ))
                cnt <- 99999
            | _ ->
                Thread.Sleep 10
                cnt <- cnt + 1
        Assert.True(( cnt = 99999 ))

        spc.Terminate( 0 )
        spc.Dispose()

        cnt <- 0
        while cnt < 100 do
            match m.Progress with
            | NormalEnd( _ ) ->
                cnt <- 99999
            | _ ->
                Thread.Sleep 10
                cnt <- cnt + 1

        Assert.True(( cnt = 99999 ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.procMediaCreateStdout_006() =
        let mutable cnt = 0
        let dname = MediaCreateProc_Test.Init "procMediaCreateStdout_006"
        let p : HarukaCtrlerCtrlReq.T_PlainFile = {
            FileName = "a";
            FileSize = 1L;
        }

        let spc = new StubProcCtrl( dname )
        let m = MediaCreateProc( HarukaCtrlerCtrlReq.U_PlainFile( p ), dname, m_Stub_ExePath )
        spc.Wait()

        let msgstr1 = String.replicate Constants.INITMEDIA_MAX_ERRMSG_LENGTH "a"
        let msg1 = InitMediaMessage.ReaderWriter.ToString { LineType = InitMediaMessage.U_ErrorMessage( msgstr1 ) }
        spc.SetStdOutResult msg1
        let msg2 = InitMediaMessage.ReaderWriter.ToString { LineType = InitMediaMessage.U_ErrorMessage( "bbb" ) }
        spc.SetStdOutResult msg2

        spc.Terminate( 0 )
        spc.Dispose()

        cnt <- 0
        while cnt < 100 do
            match m.Progress with
            | NormalEnd( _ ) ->
                cnt <- 99999
            | _ ->
                Thread.Sleep 10
                cnt <- cnt + 1
        Assert.True(( cnt = 99999 ))

        Assert.True(( m.ErrorMessages.Length = 2 ))
        Assert.True(( m.ErrorMessages.[0] = msgstr1 ))
        Assert.True(( m.ErrorMessages.[1] = "bbb" ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.procMediaCreateStdout_007() =
        let mutable cnt = 0
        let dname = MediaCreateProc_Test.Init "procMediaCreateStdout_007"
        let p : HarukaCtrlerCtrlReq.T_PlainFile = {
            FileName = "a";
            FileSize = 1L;
        }

        let spc = new StubProcCtrl( dname )
        let m = MediaCreateProc( HarukaCtrlerCtrlReq.U_PlainFile( p ), dname, m_Stub_ExePath )
        spc.Wait()

        for i = 1 to ( Constants.INITMEDIA_MAX_ERRMSG_COUNT + 1 ) do
            let msg1 = InitMediaMessage.ReaderWriter.ToString { LineType = InitMediaMessage.U_ErrorMessage( "a" ) }
            spc.SetStdOutResult msg1

        spc.Terminate( 0 )
        spc.Dispose()

        cnt <- 0
        while cnt < 100 do
            match m.Progress with
            | NormalEnd( _ ) ->
                cnt <- 99999
            | _ ->
                Thread.Sleep 10
                cnt <- cnt + 1
        Assert.True(( cnt = 99999 ))

        Assert.True(( m.ErrorMessages.Length = Constants.INITMEDIA_MAX_ERRMSG_COUNT ))
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.procMediaCreateStdout_008() =
        let mutable cnt = 0
        let dname = MediaCreateProc_Test.Init "procMediaCreateStdout_008"
        let p : HarukaCtrlerCtrlReq.T_PlainFile = {
            FileName = "a";
            FileSize = 1L;
        }

        let spc = new StubProcCtrl( dname )
        let m = MediaCreateProc( HarukaCtrlerCtrlReq.U_PlainFile( p ), dname, m_Stub_ExePath )
        spc.Wait()

        let msg1 = InitMediaMessage.ReaderWriter.ToString { LineType = InitMediaMessage.U_ErrorMessage( "a" ) }
        spc.SetStdOutResult msg1

        spc.SetStdOutResult "aaaaaa"

        let msg2 = InitMediaMessage.ReaderWriter.ToString { LineType = InitMediaMessage.U_CreateFile( "b" ) }
        spc.SetStdOutResult msg2

        spc.Terminate( 0 )
        spc.Dispose()

        cnt <- 0
        while cnt < 100 do
            match m.Progress with
            | NormalEnd( _ ) ->
                cnt <- 99999
            | _ ->
                Thread.Sleep 10
                cnt <- cnt + 1
        Assert.True(( cnt = 99999 ))

        Assert.True(( m.ErrorMessages.Length = 1 ))
        Assert.True(( m.CreatedFile.Length = 1 ))
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Kill_001() =
        let dname = MediaCreateProc_Test.Init "Kill_001"
        let p : HarukaCtrlerCtrlReq.T_PlainFile = {
            FileName = "a";
            FileSize = 1L;
        }
        let fname1 = Functions.AppendPathName dname "aaa.txt"
        File.WriteAllText( fname1, "" )

        let spc = new StubProcCtrl( dname )
        let m = MediaCreateProc( HarukaCtrlerCtrlReq.U_PlainFile( p ), dname, m_Stub_ExePath )
        spc.Wait()

        Assert.False( m.SubprocessHasTerminated )

        let msg1 = InitMediaMessage.ReaderWriter.ToString { LineType = InitMediaMessage.U_CreateFile( fname1 ) }
        spc.SetStdOutResult msg1

        let mutable cnt = 0
        while m.CreatedFile.Length = 0 && cnt < 200 do
            Thread.Sleep 5
            cnt <- cnt + 1

        Assert.True(( m.CreatedFile.Length = 1 ))
        Assert.True(( m.CreatedFile.[0] = fname1 ))

        m.Kill()
        spc.Dispose()

        cnt <- 0
        while cnt < 200 do
            match m.Progress with
            | AbnormalEnd( _ ) ->
                cnt <- 99999
            | _ ->
                Thread.Sleep 5
                cnt <- cnt + 1
        Assert.True(( cnt = 99999 ))

        Assert.True( m.SubprocessHasTerminated )
        Assert.False(( File.Exists fname1 ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Kill_002() =
        let p : HarukaCtrlerCtrlReq.T_PlainFile = {
            FileName = "a";
            FileSize = 1L;
        }
        let m = MediaCreateProc( HarukaCtrlerCtrlReq.U_PlainFile( p ), "", "aaa" )

        Assert.True( m.SubprocessHasTerminated )
        m.Kill()
        Assert.True( m.SubprocessHasTerminated )
