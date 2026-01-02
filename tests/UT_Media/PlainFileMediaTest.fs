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
// Type definition

type CFileStream_Stub
    (
        argPath : string,
        argFileMode : FileMode,
        argFileAccess : FileAccess,
        argFileShare : FileShare,
        argBufSize : int,
        argUseAsync : bool
    ) =
    inherit FileStream( argPath, argFileMode, argFileAccess, argFileShare, argBufSize, argUseAsync )

    member val PreSeek : unit -> unit = id with get, set
    member val PreRead : ( int * int ) -> ( int * int ) = id with get, set
    member val PreWrite : ( int * int ) -> ( int * int ) = id with get, set

    override this.Flush() =
        base.Flush()

    override this.Seek( offset : int64, origin : SeekOrigin ) =
        this.PreSeek()
        base.Seek( offset, origin )

    override this.ReadAsync( buffer : byte[], offset : int, count : int, c : CancellationToken ) =
        let wOffset, wCount = this.PreRead( offset, count )
        base.ReadAsync( buffer, wOffset, wCount, c )

    override this.WriteAsync( buffer : byte[], offset : int, count : int, c : CancellationToken ) =
        let wOffset, wCount = this.PreWrite( offset, count )
        base.WriteAsync( buffer, wOffset, wCount, c )

//=============================================================================
// Class implementation

type PlainFileMedia_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore
 
    member _.CreateTestDir ( caseName : string ) =
        let w1 = Functions.AppendPathName ( Path.GetTempPath() ) "PlainFileMedia_Test_" + caseName
        GlbFunc.CreateDir w1 |> ignore
        w1

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member this.Constructor_001() =
        let pDirName = this.CreateTestDir "Constructor_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"

        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 16u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.CreateText( testfname )
            s.Write( Array.zeroCreate<char>( 4096 ) )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL )

        let pr = new PrivateCaller( f )
        let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

        Assert.True( ( vfs.Length = 16 ) )
        for i = 0 to vfs.Length - 1 do
            Assert.True(( vfs.[i].Length = 4096L ))

        Assert.True(( ( pr.GetField( "m_FileSize" ) :?> int64 ) = 4096L ))

        let sema = pr.GetField( "m_MulSema" ) :?> SemaphoreSlim;
        for i = 0 to vfs.Length do
            if i < vfs.Length then
                Assert.True(( sema.Wait( 1 ) ))
            else
                Assert.False(( sema.Wait( 1 ) ))

        try
            File.Delete testfname
            Assert.Fail __LINE__
        with
        | :? IOException ->
            ()

        k1.NoticeTerminate()

        GlbFunc.DeleteFile testfname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Constructor_002() =
        let pDirName = this.CreateTestDir "Constructor_002"
        let testfname = Functions.AppendPathName pDirName "a.txt"

        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.CreateText( testfname )
            s.Write( Array.zeroCreate<char>( 4096 ) )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL )

        let pr = new PrivateCaller( f )
        let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

        Assert.True( ( vfs.Length = 1 ) )
        let sema = pr.GetField( "m_MulSema" ) :?> SemaphoreSlim;
        for i = 0 to vfs.Length do
            if i < vfs.Length then
                Assert.True(( sema.Wait( 1 ) ))
            else
                Assert.False(( sema.Wait( 1 ) ))

        k1.NoticeTerminate()

        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Constructor_003() =
        let pDirName = this.CreateTestDir "Constructor_003"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 16u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.CreateText( testfname )
            s.Write( Array.zeroCreate<char>( 4096 ) )
            s.Close()

        let ws = new FileStream( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 1, true )

        try
            let _ = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL )
            Assert.Fail __LINE__
        with
        | :? IOException as x ->
            Assert.True( x.Message.Contains( testfname ) )

        ws.Close()
        ws.Dispose()
        k1.NoticeTerminate()

        GlbFunc.DeleteFile testfname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Constructor_004() =
        let pDirName = this.CreateTestDir "Constructor_004"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 16u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        try
            GlbFunc.DeleteFile( testfname )
        with
        | _ ->
            ()

        try
            let _ = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL )
            Assert.Fail __LINE__
        with
        | :? IOException as x ->
            Assert.True( x.Message.Contains( testfname ) )

        k1.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Closing_001() =
        let pDirName = this.CreateTestDir "Closing_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 16u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.CreateText( testfname )
            s.Write( Array.zeroCreate<char>( 4096 ) )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia

        f.Closing()

        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.TestUnitReady_001() =
        let pDirName = this.CreateTestDir "TestUnitReady_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 16u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.CreateText( testfname )
            s.Write( Array.zeroCreate<char>( 4096 ) )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        Assert.True( ( f.TestUnitReady ( itt_me.fromPrim 0u ) sourcei ) = ValueNone )

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ReadCapacity_001() =
        let pDirName = this.CreateTestDir "ReadCapacity_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 16u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller
        let wBlockSize = int Constants.MEDIA_BLOCK_SIZE

        do 
            use s = File.CreateText( testfname )
            s.Write( Array.zeroCreate<char>( wBlockSize * 8 ) )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        let blockCnt = f.ReadCapacity ( itt_me.fromPrim 0u ) sourcei  
        Assert.True( ( blockCnt = 8UL ) )

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Read_001() =
        task {
            let pDirName = this.CreateTestDir "Read_001"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf : TargetGroupConf.T_PlainFile = {
                IdentNumber = mediaidx_me.fromPrim 0u;
                MediaName = "";
                FileName = testfname;
                MaxMultiplicity = 1u;
                QueueWaitTimeOut = 1000;
                WriteProtect = false;
            }
            let k1 = new HKiller() :> IKiller
            let wBlockSize = int Constants.MEDIA_BLOCK_SIZE

            do 
                use s = File.CreateText( testfname )
                s.Write( Array.zeroCreate<char>( wBlockSize * 8 ) )
                s.Close()

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
            let pr = new PrivateCaller( f )
            let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

            vfs.[0].Close()
            vfs.[0].Dispose()
            vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

            let sourcei : CommandSourceInfo =
                {
                    I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                    CID = cid_me.zero;
                    ConCounter = concnt_me.zero;
                    TSIH = tsih_me.zero;
                    ProtocolService = new CProtocolService_Stub() :> IProtocolService
                    SessionKiller = k1
                }

            try
                let v = Array.zeroCreate<byte>( 1 * wBlockSize )
                let! _ = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 8UL ) ( ArraySegment v )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "Out of media capacity" ) )

            let v = Array.zeroCreate<byte>( 1 * wBlockSize )
            let! r = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 7UL ) ( ArraySegment v )
            Assert.True(( r = v.Length ))

            try
                let v = Array.zeroCreate<byte>( 2 * wBlockSize )
                let! _ = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 7UL ) ( ArraySegment v )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "Out of media capacity" ) )

            let v = Array.zeroCreate<byte>( 2 * wBlockSize )
            let! r = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 6UL ) ( ArraySegment v )
            Assert.True(( r = v.Length ))

            try
                let v = Array.zeroCreate<byte>( 3 * wBlockSize )
                let! _ = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 6UL ) ( ArraySegment v )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "Out of media capacity" ) )

            let v = Array.zeroCreate<byte>( 8 * wBlockSize )
            let! r = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment v )
            Assert.True(( r = v.Length ))

            k1.NoticeTerminate()
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }

    [<Fact>]
    member this.Read_002() =
        task {
            let pDirName = this.CreateTestDir "Read_002"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf : TargetGroupConf.T_PlainFile = {
                IdentNumber = mediaidx_me.fromPrim 0u;
                MediaName = "";
                FileName = testfname;
                MaxMultiplicity = 1u;
                QueueWaitTimeOut = 1000;
                WriteProtect = false;
            }
            let k1 = new HKiller() :> IKiller
            let wrand = new Random()
            let wBlockSize = int Constants.MEDIA_BLOCK_SIZE

            let wrotedata = Array.zeroCreate<byte>( wBlockSize * 8 )
            wrand.NextBytes( wrotedata )
            do 
                use s = File.Create( testfname )
                s.Write( wrotedata, 0, wrotedata.Length )
                s.Close()

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
            let pr = new PrivateCaller( f )
            let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

            vfs.[0].Close()
            vfs.[0].Dispose()
            vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

            let sourcei : CommandSourceInfo =
                {
                    I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                    CID = cid_me.zero;
                    ConCounter = concnt_me.zero;
                    TSIH = tsih_me.zero;
                    ProtocolService = new CProtocolService_Stub() :> IProtocolService
                    SessionKiller = k1
                }

            let buf = Array.zeroCreate<byte>( 1 * wBlockSize )
            let! r = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment buf )
            Assert.True( ( buf = wrotedata.[ 0 * wBlockSize .. 1 * wBlockSize - 1 ] ))
            Assert.True(( r = buf.Length ))

            let buf = Array.zeroCreate<byte>( 2 * wBlockSize )
            let! r = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 1UL ) ( ArraySegment buf )
            Assert.True( ( buf = wrotedata.[ 1 * wBlockSize .. 3 * wBlockSize - 1 ] ))
            Assert.True(( r = buf.Length ))

            let buf = Array.zeroCreate<byte>( 3 * wBlockSize )
            let! r = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 3UL ) ( ArraySegment buf )
            Assert.True( ( buf = wrotedata.[ 3 * wBlockSize .. 6 * wBlockSize - 1 ] ))
            Assert.True(( r = buf.Length ))

            let buf = Array.zeroCreate<byte>( 2 * wBlockSize )
            let! r = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 6UL ) ( ArraySegment buf )
            Assert.True( ( buf = wrotedata.[ 6 * wBlockSize .. 8 * wBlockSize - 1 ] ))
            Assert.True(( r = buf.Length ))

            k1.NoticeTerminate()
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }

    [<Fact>]
    member this.Read_004() =
        let pDirName = this.CreateTestDir "Read_004"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 100;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.Create( testfname )
            s.SetLength( 4096L )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let pr = new PrivateCaller( f )

        let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

        vfs.[0].Close()
        vfs.[0].Dispose()
        vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        let s1 = new SemaphoreSlim( 1 )
        let s2 = new SemaphoreSlim( 1 )

        s1.Wait()
        s2.Wait()

        [|
            // Thread2
            fun () -> task {
                do! Task.Delay 1
                do! s1.WaitAsync()   // Wait for thread1 reach read method.

                try
                    let buf = Array.zeroCreate<byte>( 512 )
                    let! _ = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment buf )
                    Assert.Fail __LINE__
                with
                | :? AggregateException as x2 ->
                    let x = x2.InnerException :?> SCSIACAException
                    Assert.True( x.Message.Contains( "Media access timed out" ) )
                    Assert.True( ( x.SenseKey = SenseKeyCd.MEDIUM_ERROR ) )
                    Assert.True( ( x.ASC = ASCCd.TIMEOUT_ON_LOGICAL_UNIT ) )
                | :? SCSIACAException as x ->
                    Assert.True( x.Message.Contains( "Media access timed out" ) )
                    Assert.True( ( x.SenseKey = SenseKeyCd.MEDIUM_ERROR ) )
                    Assert.True( ( x.ASC = ASCCd.TIMEOUT_ON_LOGICAL_UNIT ) )

                s2.Release() |> ignore
            };

            // Thread 1
            fun () -> task {
                do! Task.Delay 1
                ( vfs.[0] :?> CFileStream_Stub ).PreRead <-
                    (
                        fun ( offset : int, count : int ) ->
                            s1.Release() |> ignore
                            s2.Wait()   // Wait for thread2 exit read method
                            ( offset, count )
                    )
                let buf = Array.zeroCreate<byte>( 512 )
                let! r = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment buf )
                Assert.True(( r = buf.Length ))
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Read_005() =
        task {
            let pDirName = this.CreateTestDir "Read_005"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf : TargetGroupConf.T_PlainFile = {
                IdentNumber = mediaidx_me.fromPrim 0u;
                MediaName = "";
                FileName = testfname;
                MaxMultiplicity = 1u;
                QueueWaitTimeOut = 1000;
                WriteProtect = false;
            }
            let k1 = new HKiller() :> IKiller

            do 
                use s = File.Create( testfname )
                s.SetLength( 4096L )
                s.Close()

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
            let pr = new PrivateCaller( f )

            let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

            vfs.[0].Close()
            vfs.[0].Dispose()
            vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

            let sourcei : CommandSourceInfo =
                {
                    I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                    CID = cid_me.zero;
                    ConCounter = concnt_me.zero;
                    TSIH = tsih_me.zero;
                    ProtocolService = new CProtocolService_Stub() :> IProtocolService
                    SessionKiller = k1
                }

            let mutable cnt = 0

            ( vfs.[0] :?> CFileStream_Stub ).PreRead <-
                (
                    fun ( offset : int, count : int ) ->
                        cnt <- cnt + 1
                        if cnt = 1 then
                            raise( new IOException() )
                        ( offset, count )
                )
            let buf = Array.zeroCreate<byte>( 512 )
            let! r = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment buf )
            Assert.True(( r = buf.Length ))

            k1.NoticeTerminate()
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }

    [<Fact>]
    member this.Read_006() =
        let pDirName = this.CreateTestDir "Read_006"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.Create( testfname )
            s.SetLength( 4096L )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let pr = new PrivateCaller( f )

        let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

        vfs.[0].Close()
        vfs.[0].Dispose()
        vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        let mutable cnt = 0

        ( vfs.[0] :?> CFileStream_Stub ).PreRead <-
            (
                fun ( offset : int, count : int ) ->
                    cnt <- cnt + 1
                    if cnt < 10 then
                        raise( new IOException() )
                    ( offset, count )
            )
        let buf = Array.zeroCreate<byte>( 512 )
        let r =
            f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment buf )
            |> GlbFunc.RunSync
        Assert.True(( r = buf.Length ))

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Read_007() =
        let pDirName = this.CreateTestDir "Read_007"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.Create( testfname )
            s.SetLength( 4096L )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let pr = new PrivateCaller( f )

        let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

        vfs.[0].Close()
        vfs.[0].Dispose()
        vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        let mutable cnt = 0

        task {
            ( vfs.[0] :?> CFileStream_Stub ).PreRead <-
                (
                    fun ( offset : int, count : int ) ->
                        cnt <- cnt + 1
                        if cnt < 11 then
                            raise( new IOException() )
                        ( offset, count )
                )
            try
                let buf = Array.zeroCreate<byte>( 512 )
                let! _ = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment buf )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "I/O retry count overed" ) )
                Assert.True( ( x.SenseKey = SenseKeyCd.MEDIUM_ERROR ) )
                Assert.True( ( x.ASC = ASCCd.UNRECOVERED_READ_ERROR ) )
        }
        |> GlbFunc.RunSync

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Read_008() =
        let pDirName = this.CreateTestDir "Read_008"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller
        let wrand = new Random()

        let wrotedata = Array.zeroCreate<byte>( 4096 )
        wrand.NextBytes( wrotedata )
        do 
            use s = File.Create( testfname )
            s.Write( wrotedata, 0, wrotedata.Length )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let pr = new PrivateCaller( f )

        let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

        vfs.[0].Close()
        vfs.[0].Dispose()
        vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        let mutable cnt = 0

        ( vfs.[0] :?> CFileStream_Stub ).PreRead <-
            (
                fun ( offset : int, count : int ) ->
                    cnt <- cnt + 1
                    match cnt with
                    | 1 ->
                        Assert.True( ( offset = 0 ))
                        Assert.True( ( count = 512 ))
                        ( 0, 100 )
                    | 2 ->
                        Assert.True( ( offset = 100 ))
                        Assert.True( ( count = 412 ))
                        ( 100, 50 )
                    | 3 ->
                        Assert.True( ( offset = 150 ))
                        Assert.True( ( count = 362 ))
                        ( 150, 362 )
                    | _ ->
                        Assert.Fail __LINE__
                        ( 0, 0 )
            )
        let buf = Array.zeroCreate<byte>( 512 )
        let r =
            f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment buf )
            |> GlbFunc.RunSync
        Assert.True(( buf = wrotedata.[ 0 .. 511 ] ))
        Assert.True(( r = buf.Length ))

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Read_009() =
        let pDirName = this.CreateTestDir "Read_009"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.Create( testfname )
            s.SetLength( 4096L )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let pr = new PrivateCaller( f )

        let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

        vfs.[0].Close()
        vfs.[0].Dispose()
        vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        let mutable cnt = 0

        task {
            ( vfs.[0] :?> CFileStream_Stub ).PreRead <-
                (
                    fun ( offset : int, count : int ) ->
                        cnt <- cnt + 1
                        if cnt = 1 then
                            vfs.[0].Close()
                        ( offset, count )
                )
            try
                let buf = Array.zeroCreate<byte>( 512 )
                let! _ = f.Read ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment buf )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "Unexpected I/O error was occured" ) )
                Assert.True( ( x.SenseKey = SenseKeyCd.MEDIUM_ERROR ) )
                Assert.True( ( x.ASC = ASCCd.UNRECOVERED_READ_ERROR ) )
        }
        |> GlbFunc.RunSync

        k1.NoticeTerminate()

        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Write_001() =
        task {
            let pDirName = this.CreateTestDir "Write_001"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf : TargetGroupConf.T_PlainFile = {
                IdentNumber = mediaidx_me.fromPrim 0u;
                MediaName = "";
                FileName = testfname;
                MaxMultiplicity = 1u;
                QueueWaitTimeOut = 1000;
                WriteProtect = false;
            }
            let k1 = new HKiller() :> IKiller
            let wBlockSize = int Constants.MEDIA_BLOCK_SIZE

            do 
                use s = File.CreateText( testfname )
                s.Write( Array.zeroCreate<char>( wBlockSize * 8 ) )
                s.Close()

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
            let sourcei : CommandSourceInfo =
                {
                    I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                    CID = cid_me.zero;
                    ConCounter = concnt_me.zero;
                    TSIH = tsih_me.zero;
                    ProtocolService = new CProtocolService_Stub() :> IProtocolService
                    SessionKiller = k1
                }

            try
                let v = Array.zeroCreate<byte>( 1 * wBlockSize )
                let! _ = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 8UL ) 0UL ( ArraySegment v )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "Out of media capacity" ) )

            let v = Array.zeroCreate<byte>( 1 * wBlockSize )
            let! r = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 7UL ) 0UL ( ArraySegment v )
            Assert.True(( r = v.Length ))

            try
                let v = Array.zeroCreate<byte>( 2 * wBlockSize )
                let! _ = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 7UL ) 0UL ( ArraySegment v )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "Out of media capacity" ) )

            let v = Array.zeroCreate<byte>( 2 * wBlockSize )
            let! r = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 6UL ) 0UL ( ArraySegment v )
            Assert.True(( r = v.Length ))

            try
                let v = Array.zeroCreate<byte>( 3 * wBlockSize )
                let! _ = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 6UL ) 0UL ( ArraySegment v )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "Out of media capacity" ) )

            let v = Array.zeroCreate<byte>( 8 * wBlockSize )
            let! r = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment v )
            Assert.True(( r = v.Length ))

            k1.NoticeTerminate()
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }

    [<Fact>]
    member this.Write_002() =
        task {
            let pDirName = this.CreateTestDir "Write_002"
            let testfname = Functions.AppendPathName pDirName "a.txt"
            let stat_stub = new CStatus_Stub()
            let conf : TargetGroupConf.T_PlainFile = {
                IdentNumber = mediaidx_me.fromPrim 0u;
                MediaName = "";
                FileName = testfname;
                MaxMultiplicity = 1u;
                QueueWaitTimeOut = 1000;
                WriteProtect = false;
            }
            let k1 = new HKiller() :> IKiller
            let wBlockSize = int Constants.MEDIA_BLOCK_SIZE
            let wrand = new Random()

            do 
                use s = File.Create( testfname )
                s.Write( Array.zeroCreate<byte>( wBlockSize * 8 ), 0, wBlockSize * 8 )
                s.Close()

            let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
            let sourcei : CommandSourceInfo =
                {
                    I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                    CID = cid_me.zero;
                    ConCounter = concnt_me.zero;
                    TSIH = tsih_me.zero;
                    ProtocolService = new CProtocolService_Stub() :> IProtocolService
                    SessionKiller = k1
                }

            do
                use s = new FileStream( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true )
                s.Write( Array.zeroCreate<byte>( wBlockSize * 8 ), 0, wBlockSize * 8 )
                s.Close()

            let buf = Array.zeroCreate<byte>( 1 * wBlockSize )
            wrand.NextBytes( buf )
            let! r = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment buf )
            Assert.True(( r = buf.Length ))

            do
                use s = new FileStream( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true )
                let readdata = Array.zeroCreate<byte>( wBlockSize * 8 )
                s.Read( readdata, 0, wBlockSize * 8 ) |> ignore
                s.Close()
                let compbuf = Array.zeroCreate<byte>( wBlockSize * 8 )
                Array.blit buf 0 compbuf 0 wBlockSize
                Assert.True( ( compbuf = readdata ))

            do
                use s = new FileStream( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true )
                s.Write( Array.zeroCreate<byte>( wBlockSize * 8 ), 0, wBlockSize * 8 )
                s.Close()

            let buf = Array.zeroCreate<byte>( 2 * wBlockSize )
            wrand.NextBytes( buf )
            let! r = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 1UL ) 0UL ( ArraySegment buf )
            Assert.True(( r = buf.Length ))

            do
                use s = new FileStream( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true )
                let readdata = Array.zeroCreate<byte>( wBlockSize * 8 )
                s.Read( readdata, 0, wBlockSize * 8 ) |> ignore
                s.Close()
                let compbuf = Array.zeroCreate<byte>( wBlockSize * 8 )
                Array.blit buf 0 compbuf wBlockSize ( wBlockSize * 2 )
                Assert.True( ( compbuf = readdata ))

            do
                use s = new FileStream( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true )
                s.Write( Array.zeroCreate<byte>( wBlockSize * 8 ), 0, wBlockSize * 8 )
                s.Close()

            let buf = Array.zeroCreate<byte>( 3 * wBlockSize )
            wrand.NextBytes( buf )
            let! r = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 3UL ) 0UL ( ArraySegment buf )
            Assert.True(( r = buf.Length ))

            do
                use s = new FileStream( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true )
                let readdata = Array.zeroCreate<byte>( wBlockSize * 8 )
                s.Read( readdata, 0, wBlockSize * 8 ) |> ignore
                s.Close()
                let compbuf = Array.zeroCreate<byte>( wBlockSize * 8 )
                Array.blit buf 0 compbuf ( wBlockSize * 3 ) ( wBlockSize * 3 )
                Assert.True( ( compbuf = readdata ))

            do
                use s = new FileStream( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true )
                s.Write( Array.zeroCreate<byte>( wBlockSize * 8 ), 0, wBlockSize * 8 )
                s.Close()

            let buf = Array.zeroCreate<byte>( 2 * wBlockSize )
            wrand.NextBytes( buf )
            let! r = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 6UL ) 0UL ( ArraySegment buf )
            Assert.True(( r = buf.Length ))

            do
                use s = new FileStream( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true )
                let readdata = Array.zeroCreate<byte>( wBlockSize * 8 )
                s.Read( readdata, 0, wBlockSize * 8 ) |> ignore
                s.Close()
                let compbuf = Array.zeroCreate<byte>( wBlockSize * 8 )
                Array.blit buf 0 compbuf ( wBlockSize * 6 ) ( wBlockSize * 2 )
                Assert.True( ( compbuf = readdata ))

            k1.NoticeTerminate()
            GlbFunc.DeleteFile( testfname )
            GlbFunc.DeleteDir pDirName
        }

    [<Fact>]
    member this.Write_004() =
        let pDirName = this.CreateTestDir "Write_004"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 100;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.Create( testfname )
            s.SetLength( 4096L )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let pr = new PrivateCaller( f )

        let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

        vfs.[0].Close()
        vfs.[0].Dispose()
        vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        let s1 = new SemaphoreSlim( 1 )
        let s2 = new SemaphoreSlim( 1 )

        s1.Wait()
        s2.Wait()

        [|
            // Thread 1
            fun () -> task {
                do! Task.Delay 1
                ( vfs.[0] :?> CFileStream_Stub ).PreWrite <-
                    (
                        fun ( offset : int, count : int ) ->
                            s1.Release() |> ignore
                            s2.Wait()   // Wait for thread2 exit read method
                            ( offset, count )
                    )
                let buf = Array.zeroCreate<byte>( 512 )
                let r =
                    f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment buf )
                    |> Functions.RunTaskSynchronously
                Assert.True(( r = buf.Length ))
            };

            // Thread2
            fun () -> task {
                do! Task.Delay 1
                do! s1.WaitAsync()   // Wait for thread1 reach read method.

                try
                    let buf = Array.zeroCreate<byte>( 512 )
                    f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment buf )
                    |> Functions.RunTaskSynchronously
                    |> ignore
                    Assert.Fail __LINE__
                with
                | :? AggregateException as x2 ->
                    let x = x2.InnerException :?> SCSIACAException
                    Assert.True( x.Message.Contains( "Media access timed out" ) )
                    Assert.True( ( x.SenseKey = SenseKeyCd.MEDIUM_ERROR ) )
                    Assert.True( ( x.ASC = ASCCd.TIMEOUT_ON_LOGICAL_UNIT ) )
                | :? SCSIACAException as x ->
                    Assert.True( x.Message.Contains( "Media access timed out" ) )
                    Assert.True( ( x.SenseKey = SenseKeyCd.MEDIUM_ERROR ) )
                    Assert.True( ( x.ASC = ASCCd.TIMEOUT_ON_LOGICAL_UNIT ) )

                s2.Release() |> ignore
            };
        |]
        |> Functions.RunTaskInPallalel
        |> Functions.TaskIgnore
        |> Functions.RunTaskSynchronously

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName


    [<Fact>]
    member this.Write_005() =
        let pDirName = this.CreateTestDir "Write_005"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.Create( testfname )
            s.SetLength( 4096L )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let pr = new PrivateCaller( f )

        let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

        vfs.[0].Close()
        vfs.[0].Dispose()
        vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        let mutable cnt = 0

        ( vfs.[0] :?> CFileStream_Stub ).PreWrite <-
            (
                fun ( offset : int, count : int ) ->
                    cnt <- cnt + 1
                    if cnt = 1 then
                        raise( new IOException() )
                    ( offset, count )
            )
        let buf = Array.zeroCreate<byte>( 512 )
        let r =
            f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment buf )
            |> GlbFunc.RunSync
        Assert.True(( r = buf.Length ))

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName


    [<Fact>]
    member this.Write_006() =
        let pDirName = this.CreateTestDir "Write_006"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.Create( testfname )
            s.SetLength( 4096L )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let pr = new PrivateCaller( f )

        let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

        vfs.[0].Close()
        vfs.[0].Dispose()
        vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        let mutable cnt = 0

        ( vfs.[0] :?> CFileStream_Stub ).PreWrite <-
            (
                fun ( offset : int, count : int ) ->
                    cnt <- cnt + 1
                    if cnt < 10 then
                        raise( new IOException() )
                    ( offset, count )
            )
        let buf = Array.zeroCreate<byte>( 512 )
        let r =
            f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment buf )
            |> GlbFunc.RunSync
        Assert.True(( r = buf.Length ))

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Write_007() =
        let pDirName = this.CreateTestDir "Write_007"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.Create( testfname )
            s.SetLength( 4096L )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let pr = new PrivateCaller( f )

        let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

        vfs.[0].Close()
        vfs.[0].Dispose()
        vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        let mutable cnt = 0

        task {
            ( vfs.[0] :?> CFileStream_Stub ).PreWrite <-
                (
                    fun ( offset : int, count : int ) ->
                        cnt <- cnt + 1
                        if cnt < 11 then
                            raise( new IOException() )
                        ( offset, count )
                )
            try
                let buf = Array.zeroCreate<byte>( 512 )
                let! _ = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment buf )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "I/O retry count overed" ) )
                Assert.True( ( x.SenseKey = SenseKeyCd.MEDIUM_ERROR ) )
                Assert.True( ( x.ASC = ASCCd.WRITE_ERROR ) )
        }
        |> GlbFunc.RunSync

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Write_009() =
        let pDirName = this.CreateTestDir "Write_009"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.Create( testfname )
            s.SetLength( 4096L )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let pr = new PrivateCaller( f )

        let vfs = pr.GetField( "m_vfile" ) :?> FileStream[]

        vfs.[0].Close()
        vfs.[0].Dispose()
        vfs.[0] <- new CFileStream_Stub( testfname, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 1, true ) :> FileStream

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        let mutable cnt = 0

        task {
            ( vfs.[0] :?> CFileStream_Stub ).PreWrite <-
                (
                    fun ( offset : int, count : int ) ->
                        cnt <- cnt + 1
                        if cnt = 1 then
                            vfs.[0].Close()
                        ( offset, count )
                )
            try
                let buf = Array.zeroCreate<byte>( 512 )
                let! _ = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment buf )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "Unexpected I/O error was occured" ) )
                Assert.True( ( x.SenseKey = SenseKeyCd.MEDIUM_ERROR ) )
                Assert.True( ( x.ASC = ASCCd.WRITE_ERROR ) )
        }
        |> GlbFunc.RunSync

        k1.NoticeTerminate()

        GlbFunc.DeleteFile testfname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Write_010() =
        let pDirName = this.CreateTestDir "Write_010"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 1u;
            QueueWaitTimeOut = 1000;
            WriteProtect = true;
        }
        let k1 = new HKiller() :> IKiller
        let wBlockSize = int Constants.MEDIA_BLOCK_SIZE

        do 
            use s = File.CreateText( testfname )
            s.Write( Array.zeroCreate<char>( wBlockSize * 8 ) )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        task {
            try
                let v = Array.zeroCreate<byte>( 1 * wBlockSize )
                let! _ = f.Write ( itt_me.fromPrim 0u ) sourcei ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment v )
                Assert.Fail __LINE__
            with
            | :? SCSIACAException as x ->
                Assert.True( x.Message.Contains( "Write protected" ) )
        }
        |> GlbFunc.RunSync

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Format_001() =
        let pDirName = this.CreateTestDir "Format_001"
        let testfname = Functions.AppendPathName pDirName "a.txt"
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 16u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }
        let k1 = new HKiller() :> IKiller

        do 
            use s = File.CreateText( testfname )
            s.Write( Array.zeroCreate<char>( 512 * 16 ) )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let sourcei : CommandSourceInfo =
            {
                I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
                CID = cid_me.zero;
                ConCounter = concnt_me.zero;
                TSIH = tsih_me.zero;
                ProtocolService = new CProtocolService_Stub() :> IProtocolService
                SessionKiller = k1
            }

        f.Format ( itt_me.fromPrim 0u ) sourcei
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
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 16u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }

        do
            use s = File.CreateText( testfname )
            s.Write( Array.zeroCreate<char>( 512 * 16 ) )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia

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
        let conf : TargetGroupConf.T_PlainFile = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            FileName = testfname;
            MaxMultiplicity = 16u;
            QueueWaitTimeOut = 1000;
            WriteProtect = false;
        }

        do
            use s = File.CreateText( testfname )
            s.Write( Array.zeroCreate<char>( 512 * 16 ) )
            s.Close()

        let f = new PlainFileMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL ) :> IMedia
        Assert.True(( f.GetSubMedia() = [] ))

        k1.NoticeTerminate()
        GlbFunc.DeleteFile( testfname )
        GlbFunc.DeleteDir pDirName
