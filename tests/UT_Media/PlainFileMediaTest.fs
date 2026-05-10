//=============================================================================
// Haruka Software Storage.
// PlainFileMediaTest.fs : Test cases for PlainFileMedia class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Media

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading
open System.Threading.Tasks

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test
open Haruka.Media
open Haruka.IODataTypes

//=============================================================================
// Class implementation

type PlainFileMedia_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let getCmdSource ( k : IKiller ) : CommandSourceInfo = 
        {
            I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
            CID = cid_me.zero;
            ConCounter = concnt_me.zero;
            TSIH = tsih_me.zero;
            ProtocolService = new CProtocolService_Stub() :> IProtocolService
            SessionKiller = k
        }

    let CreateZeroFile ( fname : string ) ( len : int ) =
        use s = File.CreateText( fname )
        s.Write( Array.zeroCreate<char>( len ) )
        s.Close()

    let getDefaultConf ( fname : string ) : TargetGroupConf.T_PlainFile = {
        IdentNumber = mediaidx_me.fromPrim 0u;
        MediaName = "";
        FileName = fname;
        QueueWaitTimeOut = 1000;
        WriteProtect = false;
    }

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.ReleaseMutex() |> ignore
 
    member _.CreateTestDir ( caseName : string ) =
        let w1 = Functions.AppendPathName ( Path.GetTempPath() ) "PlainFileMedia_Test_" + caseName
        GlbFunc.CreateDir w1 |> ignore
        w1

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member this.Constructor_OpenFailed_001() =
        let pDirName = this.CreateTestDir "Constructor_OpenFailed_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf = getDefaultConf testfname
        let k1 = new HKiller() :> IKiller
        CreateZeroFile testfname 4096

        let ws = new FileStream( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 1, true )
        try
            let _ = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u )
            Assert.Fail __LINE__
        with
        | :? IOException as x ->
            ()
        | _ ->
            Assert.Fail __LINE__

        ws.Close()
        ws.Dispose()
        k1.NoticeTerminate()

        GlbFunc.DeleteFile testfname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Constructor_FileMissing_001() =
        let pDirName = this.CreateTestDir "Constructor_FileMissing_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf = getDefaultConf testfname
        let k1 = new HKiller() :> IKiller
        try
            GlbFunc.DeleteFile( testfname )
        with
        | _ -> ()

        try
            let _ = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u )
            Assert.Fail __LINE__
        with
        | :? IOException as x ->
            ()
        | _ ->
            Assert.Fail __LINE__

        k1.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Closing_001() =
        let pDirName = this.CreateTestDir "Closing_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf = getDefaultConf testfname
        let k1 = new HKiller() :> IKiller
        CreateZeroFile testfname 4096

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia

        f.Closing()

        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.TestUnitReady_001() =
        let pDirName = this.CreateTestDir "TestUnitReady_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf = getDefaultConf testfname
        let k1 = new HKiller() :> IKiller
        CreateZeroFile testfname 4096

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia

        let src = getCmdSource k1
        Assert.True( ( f.TestUnitReady ( itt_me.fromPrim 0u ) src ) = ValueNone )

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ReadCapacity_001() =
        let pDirName = this.CreateTestDir "ReadCapacity_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf = getDefaultConf testfname
        let k1 = new HKiller() :> IKiller
        let src = getCmdSource k1
        let wBlockSize = int Constants.MEDIA_BLOCK_SIZE
        CreateZeroFile testfname ( wBlockSize * 8 )

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia
        let blockCnt = f.ReadCapacity ( itt_me.fromPrim 0u ) src  
        Assert.True( ( blockCnt = 8UL ) )

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Theory>]
    [<InlineData( 9UL, 0 )>]
    [<InlineData( 8UL, 1 )>]
    [<InlineData( 7UL, 2 )>]
    [<InlineData( 0UL, 9 )>]
    [<InlineData( 0xFFFFFFFFFFFFFFFFUL, 2 )>]
    [<InlineData( 0x7FFFFFFFFFFFFFFFUL, 2 )>]
    member this.Read_OutOfRange_001 ( lba : uint64 ) ( cnt : int ) =
        task {
            let pDirName = this.CreateTestDir "Read_OutOfRange_001"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf = getDefaultConf testfname
            let k1 = new HKiller() :> IKiller
            let src = getCmdSource k1
            let wBlockSize = int Constants.MEDIA_BLOCK_SIZE
            CreateZeroFile testfname ( wBlockSize * 8 )

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia
            try
                let v = Array.zeroCreate<byte>( cnt * wBlockSize )
                let! _ = f.Read ( itt_me.fromPrim 0u ) src ( blkcnt_me.ofUInt64 lba ) ( ArraySegment v )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "Out of media capacity" ) )
            | _ ->
                Assert.Fail __LINE__


            k1.NoticeTerminate()
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }

    [<Theory>]
    [<InlineData( 8UL, 0 )>]
    [<InlineData( 7UL, 1 )>]
    [<InlineData( 0UL, 8 )>]
    member this.Read_Suceed_001 ( lba : uint64 ) ( cnt : int ) =
        task {
            let pDirName = this.CreateTestDir "Read_Suceed_001"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf = getDefaultConf testfname
            let k1 = new HKiller() :> IKiller
            let src = getCmdSource k1
            let wrand = new Random()
            let wBlockSize = int Constants.MEDIA_BLOCK_SIZE

            let wrotedata = Array.zeroCreate<byte>( wBlockSize * 8 )
            wrand.NextBytes( wrotedata )
            File.WriteAllBytes( testfname, wrotedata )

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia
            let buf = Array.zeroCreate<byte>( cnt * wBlockSize )
            let! r = f.Read ( itt_me.fromPrim 0u ) src ( blkcnt_me.ofUInt64 lba ) ( ArraySegment buf )
            Assert.True( ( buf = wrotedata.[ int lba * wBlockSize .. ( int lba + cnt ) * wBlockSize - 1 ] ))
            Assert.True(( r = buf.Length ))

            k1.NoticeTerminate()
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }

    [<Fact>]
    member this.Read_ReadOnly_001 () =
        task {
            let pDirName = this.CreateTestDir "Read_ReadOnly_001"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf = {
                getDefaultConf testfname with
                    WriteProtect = true;
            }
            let k1 = new HKiller() :> IKiller
            let src = getCmdSource k1
            let wrand = new Random()
            let wBlockSize = int Constants.MEDIA_BLOCK_SIZE

            let wrotedata = Array.zeroCreate<byte>( wBlockSize * 8 )
            wrand.NextBytes( wrotedata )
            File.WriteAllBytes( testfname, wrotedata )
            File.SetAttributes( testfname, FileAttributes.ReadOnly )

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia
            let buf = Array.zeroCreate<byte>( wBlockSize )
            let! r = f.Read ( itt_me.fromPrim 0u ) src ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment buf )
            Assert.True( ( buf = wrotedata.[ .. wBlockSize - 1 ] ))
            Assert.True(( r = buf.Length ))

            k1.NoticeTerminate()
            File.SetAttributes( testfname, FileAttributes.None )
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }
    [<Fact>]
    member this.Read_AfterClose_001 () =
        task {
            let pDirName = this.CreateTestDir "Read_AfterClose_001"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf = getDefaultConf testfname
            let k1 = new HKiller() :> IKiller
            let src = getCmdSource k1
            let wrand = new Random()
            let wBlockSize = int Constants.MEDIA_BLOCK_SIZE

            let wrotedata = Array.zeroCreate<byte>( wBlockSize * 8 )
            wrand.NextBytes( wrotedata )
            File.WriteAllBytes( testfname, wrotedata )

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia
            f.Closing()

            let buf = Array.zeroCreate<byte>( wBlockSize )
            try
                let! _ = f.Read ( itt_me.fromPrim 0u ) src ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment buf )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException ->
                ()
            | _ ->
                Assert.Fail __LINE__

            k1.NoticeTerminate()
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }

    [<Theory>]
    [<InlineData( 9UL, 0UL, 0 )>]
    [<InlineData( 8UL, 0UL, 1 )>]
    [<InlineData( 7UL, 0UL, 2 )>]
    [<InlineData( 0UL, 0UL, 9 )>]
    [<InlineData( 0xFFFFFFFFFFFFFFFFUL, 0UL, 2 )>]
    [<InlineData( 0x7FFFFFFFFFFFFFFFUL, 0UL, 2 )>]
    [<InlineData( 0UL, 1UL, 8 )>]
    member this.Write_OutOfRange_001 ( lba : uint64 ) ( offset : uint64 ) ( cnt : int ) =
        task {
            let pDirName = this.CreateTestDir "Write_OutOfRange_001"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf = getDefaultConf testfname
            let k1 = new HKiller() :> IKiller
            let src = getCmdSource k1
            let wBlockSize = int Constants.MEDIA_BLOCK_SIZE
            CreateZeroFile testfname ( wBlockSize * 8 )

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia
            try
                let v = Array.zeroCreate<byte>( cnt * wBlockSize )
                let! _ = f.Write ( itt_me.fromPrim 0u ) src ( blkcnt_me.ofUInt64 lba ) offset ( ArraySegment v )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "Out of media capacity" ) )
            | _ ->
                Assert.Fail __LINE__

            k1.NoticeTerminate()
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }

    [<Theory>]
    [<InlineData( 8UL, 0UL, 0 )>]
    [<InlineData( 7UL, 0UL, 1 )>]
    [<InlineData( 0UL, 0UL, 8 )>]
    [<InlineData( 0UL, 1UL, 7 )>]
    member this.Write_Suceed_001 ( lba : uint64 ) ( offset : uint64 ) ( cnt : int ) =
        task {
            let pDirName = this.CreateTestDir "Write_Suceed_001"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf = getDefaultConf testfname
            let k1 = new HKiller() :> IKiller
            let src = getCmdSource k1
            let wBlockSize = int Constants.MEDIA_BLOCK_SIZE
            let wrand = new Random()
            CreateZeroFile testfname ( wBlockSize * 8 )

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia
            let buf = Array.zeroCreate<byte>( cnt * wBlockSize )
            wrand.NextBytes( buf )
            let! r = f.Write ( itt_me.fromPrim 0u ) src ( blkcnt_me.ofUInt64 lba ) offset ( ArraySegment buf )
            Assert.True(( r = buf.Length ))
            f.Closing()

            let compbuf = File.ReadAllBytes testfname
            Assert.True( ( compbuf.[ int lba * wBlockSize + int offset .. ( int lba + cnt ) * wBlockSize + int offset - 1 ] = buf ))

            k1.NoticeTerminate()
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }

    [<Fact>]
    member this.Write_AfterClose_001 () =
        task {
            let pDirName = this.CreateTestDir "Write_AfterClose_001"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf = getDefaultConf testfname
            let k1 = new HKiller() :> IKiller
            let src = getCmdSource k1
            let wrand = new Random()
            let wBlockSize = int Constants.MEDIA_BLOCK_SIZE

            let wrotedata = Array.zeroCreate<byte>( wBlockSize * 8 )
            wrand.NextBytes( wrotedata )
            File.WriteAllBytes( testfname, wrotedata )

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia
            f.Closing()

            let buf = Array.zeroCreate<byte>( wBlockSize )
            try
                let! _ = f.Write ( itt_me.fromPrim 0u ) src ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment buf )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException ->
                ()
            | _ ->
                Assert.Fail __LINE__

            k1.NoticeTerminate()
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }

    [<Fact>]
    member this.Write_ReadOnly_001 () =
        task {
            let pDirName = this.CreateTestDir "Write_ReadOnly_001"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf = {
                getDefaultConf testfname with
                    WriteProtect = true;
            }
            let k1 = new HKiller() :> IKiller
            let src = getCmdSource k1
            let wrand = new Random()
            let wBlockSize = int Constants.MEDIA_BLOCK_SIZE

            let wrotedata = Array.zeroCreate<byte>( wBlockSize * 8 )
            wrand.NextBytes( wrotedata )
            File.WriteAllBytes( testfname, wrotedata )

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia
            f.Closing()

            let buf = Array.zeroCreate<byte>( wBlockSize )
            try
                let! _ = f.Write ( itt_me.fromPrim 0u ) src ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment buf )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException ->
                ()
            | _ ->
                Assert.Fail __LINE__

            k1.NoticeTerminate()
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }

    [<Fact>]
    member this.Format_001() =
        let pDirName = this.CreateTestDir "Format_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf = getDefaultConf testfname
        let k1 = new HKiller() :> IKiller
        let src = getCmdSource k1
        CreateZeroFile testfname ( 512 * 16 )

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia
        f.Format ( itt_me.fromPrim 0u ) src
        |> GlbFunc.RunSync

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.MediaControl_001() =
        let k1 = new HKiller() :> IKiller
        let pDirName = this.CreateTestDir "MediaControl_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf = getDefaultConf testfname
        CreateZeroFile testfname ( 512 * 16 )

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia

        let r =
            f.MediaControl( MediaCtrlReq.U_Debug( MediaCtrlReq.U_GetAllTraps() ) )
            |> Functions.RunTaskSynchronously
        match r with
        | MediaCtrlRes.U_Unexpected( x ) ->
            Assert.True(( x.StartsWith "Plain file media does not support media controls" ))
        | _ ->
            Assert.Fail __LINE__

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName
        
    [<Fact>]
    member this.GetSubMedia_001() =
        let k1 = new HKiller() :> IKiller
        let pDirName = this.CreateTestDir "GetSubMedia_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf = getDefaultConf testfname
        CreateZeroFile testfname ( 512 * 16 )

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 1u ) :> IMedia
        Assert.True(( f.GetSubMedia() = [] ))

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName
