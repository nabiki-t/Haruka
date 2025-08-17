namespace Haruka.Test.UT.Controller

open System
open System.IO
open System.Threading.Tasks
open System.Text
open System.Net.Sockets
open System.Threading
open System.Threading.Tasks.Dataflow

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test
open Haruka.Controller
open Haruka.IODataTypes


type LogAggregator_Test1 () =

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    static member CreateTestDir ( caseName : string ) =
        let w1 = Functions.AppendPathName ( Path.GetTempPath() ) "LogAggregator_Test_" + caseName
        if Directory.Exists w1 then GlbFunc.DeleteDir w1
        GlbFunc.CreateDir w1 |> ignore
        w1

    [<Fact>]
    member _.AddChild_001() =
        let dname = LogAggregator_Test1.CreateTestDir "AddChild_001"
        let k = HKiller() :> IKiller
        let sp, cp = GlbFunc.CreateAnonymousPipe()
        use sr = new StreamReader( cp )
        use sw = new StreamWriter( sp )

        let logParam : HarukaCtrlConf.T_LogMaintenance = {
            OutputDest = HarukaCtrlConf.U_ToFile({
                TotalLimit = 100u;
                MaxFileCount = 1u;
                ForceSync = true;
            })
        }
        let la = new LogAggregator(
            dname,
            logParam,
            k,
            ( fun () -> DateTime.Parse( "2000/01/01" ) ),
            ( fun () -> 0 )
        )
        let pc = PrivateCaller( la )

        la.Initialize()
        la.AddChild sr

        let files0 = Directory.GetFiles dname
        Assert.True( files0.Length = 0 )

        sw.WriteLine( "aaa" )
        sw.Flush()

        let logfname = Functions.AppendPathName dname "20000101.txt"
        GlbFunc.WaitForFileCreate logfname
        Assert.True( File.Exists logfname )

        let m_MsgQueue = pc.GetField( "m_MsgQueue" ) :?> BufferBlock<string>
        while m_MsgQueue.Count > 0 do
            Thread.Sleep 20

        k.NoticeTerminate()

        let fcont =
            Functions.ReadAllTextAsync logfname
            |> Functions.RunTaskSynchronously
            |> Functions.GetOkValue ""
        Assert.True(( fcont = "aaa" + Environment.NewLine ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddChild_002() =
        let dname = LogAggregator_Test1.CreateTestDir "AddChild_002"
        let k = HKiller() :> IKiller
        let sp, cp = GlbFunc.CreateAnonymousPipe()
        use sr = new StreamReader( cp )
        use sw = new StreamWriter( sp )

        let logParam : HarukaCtrlConf.T_LogMaintenance = {
            OutputDest = HarukaCtrlConf.U_ToFile({
                TotalLimit = 1u;
                MaxFileCount = 1u;
                ForceSync = true;
            })
        }
        let la = new LogAggregator(
            dname,
            logParam,
            k,
            ( fun () -> DateTime.Parse( "2000/01/01" ) ),
            ( fun () -> 0 )
        )

        la.Initialize()
        la.AddChild sr

        Assert.True cp.IsConnected

        sw.Close()
        sw.Dispose()

        Functions.loopAsync ( fun () -> Task.FromResult cp.IsConnected )
        |> Functions.RunTaskSynchronously
        Assert.False cp.IsConnected

        k.NoticeTerminate()
        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.AddChild_003() =
        let dname = LogAggregator_Test1.CreateTestDir "AddChild_003"
        let k = HKiller() :> IKiller
        let sp, cp = GlbFunc.CreateAnonymousPipe()
        use sr = new StreamReader( cp )
        use sw = new StreamWriter( sp )
        let mutable cnt = 0

        let logParam : HarukaCtrlConf.T_LogMaintenance = {
            OutputDest = HarukaCtrlConf.U_ToFile({
                TotalLimit = 3u;
                MaxFileCount = 1u;
                ForceSync = true;
            })
        }
        let la = new LogAggregator(
            dname,
            logParam,
            k,
            ( fun () -> DateTime.Parse( "2000/01/01" ) ),
            ( fun () -> 
                cnt <- cnt + 1
                if cnt <= 4 then
                    0
                else
                    1001
            )
        )
        let pc = PrivateCaller( la )

        la.Initialize()
        la.AddChild sr

        let files0 = Directory.GetFiles dname
        Assert.True( files0.Length = 0 )

        sw.WriteLine( "aaa" )
        sw.WriteLine( "bbb" )
        sw.WriteLine( "ccc" )
        sw.WriteLine( "ddd" )   // ommitted
        sw.WriteLine( "eee" )
        sw.WriteLine( "fff" )
        sw.WriteLine( "ggg" )
        sw.WriteLine( "hhh" )   // ommitted
        sw.Flush()

        let logfname = Functions.AppendPathName dname "20000101.txt"
        GlbFunc.WaitForFileCreate logfname
        Assert.True( File.Exists logfname )

        let m_MsgQueue = pc.GetField( "m_MsgQueue" ) :?> BufferBlock<string>
        while m_MsgQueue.Count > 0 do
            Thread.Sleep 20

        k.NoticeTerminate()

        let expectText =
            "aaa" + Environment.NewLine +
            "bbb" + Environment.NewLine +
            "ccc" + Environment.NewLine +
            "eee" + Environment.NewLine +
            "fff" + Environment.NewLine +
            "ggg" + Environment.NewLine
        
        let fcont =
            Functions.ReadAllTextAsync logfname
            |> Functions.RunTaskSynchronously
            |> Functions.GetOkValue ""
        Assert.True(( fcont = expectText ))

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Initialize_001() =
        let dname = LogAggregator_Test1.CreateTestDir "Initialize_001"
        let k = HKiller() :> IKiller
        let sp, cp = GlbFunc.CreateAnonymousPipe()
        use sr = new StreamReader( cp )
        use sw = new StreamWriter( sp )
        let mutable cnt = 0
        let vDate = [|
            DateTime.Parse( "2000/01/01" );
            DateTime.Parse( "2000/01/02" );
            DateTime.Parse( "2000/01/03" );
            DateTime.Parse( "2000/01/04" );
            DateTime.Parse( "2000/01/05" );
        |]
        let vFName = [|
            Functions.AppendPathName dname "20000101.txt";
            Functions.AppendPathName dname "20000102.txt";
            Functions.AppendPathName dname "20000103.txt";
            Functions.AppendPathName dname "20000104.txt";
            Functions.AppendPathName dname "20000105.txt";
        |]
        let expectFileCount = [|
            1; 2; 2; 2; 2
        |]

        let logParam : HarukaCtrlConf.T_LogMaintenance = {
            OutputDest = HarukaCtrlConf.U_ToFile({
                TotalLimit = 100u;
                MaxFileCount = 1u;
                ForceSync = true;
            })
        }
        let la = new LogAggregator(
            dname,
            logParam,
            k,
            ( fun () ->
                cnt <- cnt + 1
                vDate.[ ( cnt - 1 ) % vDate.Length ]
            ),
            ( fun () -> 0 )
        )
        let pc = PrivateCaller( la )
        let m_MsgQueue = pc.GetField( "m_MsgQueue" ) :?> BufferBlock<string>

        la.Initialize()
        la.AddChild sr

        let files0 = Directory.GetFiles dname
        Assert.True( files0.Length = 0 )

        for i = 0 to 4 do
            sw.WriteLine( "aaa" )
            sw.Flush()

            GlbFunc.WaitForFileCreate vFName.[i]
            while m_MsgQueue.Count > 0 do Thread.Sleep 20

            Assert.True( File.Exists vFName.[i] )
            let files1 = Directory.GetFiles dname
            Assert.True( files1.Length = expectFileCount.[i] )

        k.NoticeTerminate()

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Initialize_002() =
        let dname = LogAggregator_Test1.CreateTestDir "Initialize_002"
        let k = HKiller() :> IKiller
        let sp, cp = GlbFunc.CreateAnonymousPipe()
        use sr = new StreamReader( cp )
        use sw = new StreamWriter( sp )
        let mutable cnt = 0
        let vDate = [|
            DateTime.Parse( "2000/01/01" );
            DateTime.Parse( "2000/01/02" );
            DateTime.Parse( "2000/01/03" );
            DateTime.Parse( "2000/01/04" );
            DateTime.Parse( "2000/01/05" );
            DateTime.Parse( "2000/01/06" );
        |]
        let vFName = [|
            Functions.AppendPathName dname "20000101.txt";
            Functions.AppendPathName dname "20000102.txt";
            Functions.AppendPathName dname "20000103.txt";
            Functions.AppendPathName dname "20000104.txt";
            Functions.AppendPathName dname "20000105.txt";
            Functions.AppendPathName dname "20000106.txt";
        |]
        let expectFileCount = [|
            1; 2; 3; 4; 4; 4
        |]

        let logParam : HarukaCtrlConf.T_LogMaintenance = {
            OutputDest = HarukaCtrlConf.U_ToFile({
                TotalLimit = 100u;
                MaxFileCount = 3u;
                ForceSync = true;
            })
        }
        let la = new LogAggregator(
            dname,
            logParam,
            k,
            ( fun () ->
                cnt <- cnt + 1
                vDate.[ ( cnt - 1 ) % vDate.Length ]
            ),
            ( fun () -> 0 )
        )
        let pc = PrivateCaller( la )
        let m_MsgQueue = pc.GetField( "m_MsgQueue" ) :?> BufferBlock<string>

        la.Initialize()
        la.AddChild sr

        let files0 = Directory.GetFiles dname
        Assert.True( files0.Length = 0 )

        for i = 0 to 5 do
            sw.WriteLine( "aaa" )
            sw.Flush()

            GlbFunc.WaitForFileCreate vFName.[i]
            while m_MsgQueue.Count > 0 do Thread.Sleep 20

            Assert.True( File.Exists vFName.[i] )
            let files1 = Directory.GetFiles dname
            Assert.True( files1.Length = expectFileCount.[i] )

        k.NoticeTerminate()

        GlbFunc.DeleteDir dname

    [<Fact>]
    member _.Initialize_003() =
        let dname = LogAggregator_Test1.CreateTestDir "Initialize_003"
        let k = HKiller() :> IKiller
        let sp, cp = GlbFunc.CreateAnonymousPipe()
        use sr = new StreamReader( cp )
        use sw = new StreamWriter( sp )
        let mutable cnt = 0
        let vDate = [|
            DateTime.Parse( "2000/01/01" );
            DateTime.Parse( "2000/01/01" );
            DateTime.Parse( "2000/01/02" );
            DateTime.Parse( "2000/01/02" );
            DateTime.Parse( "2000/01/03" );
            DateTime.Parse( "2000/01/03" );
        |]
        let vFName = [|
            Functions.AppendPathName dname "20000101.txt";
            Functions.AppendPathName dname "20000102.txt";
            Functions.AppendPathName dname "20000103.txt";
        |]

        Directory.CreateDirectory vFName.[1] |> ignore

        let logParam : HarukaCtrlConf.T_LogMaintenance = {
            OutputDest = HarukaCtrlConf.U_ToFile({
                TotalLimit = 100u;
                MaxFileCount = 10u;
                ForceSync = true;
            })
        }
        let la = new LogAggregator(
            dname,
            logParam,
            k,
            ( fun () ->
                cnt <- cnt + 1
                vDate.[ ( cnt - 1 ) % vDate.Length ]
            ),
            ( fun () -> 0 )
        )
        let pc = PrivateCaller( la )
        let m_MsgQueue = pc.GetField( "m_MsgQueue" ) :?> BufferBlock<string>

        la.Initialize()
        la.AddChild sr

        let files0 = Directory.GetFiles dname
        Assert.True( files0.Length = 0 )

        sw.WriteLine( "aaa" )
        sw.Flush()
        sw.WriteLine( "bbb" )
        sw.Flush()
        sw.WriteLine( "ccc" )   // output error
        sw.Flush()
        sw.WriteLine( "ddd" )   // output error
        sw.Flush()
        sw.WriteLine( "eee" )
        sw.Flush()
        sw.WriteLine( "fff" )
        sw.Flush()

        GlbFunc.WaitForFileCreate vFName.[2]
        while m_MsgQueue.Count > 0 do Thread.Sleep 20
        Assert.True( File.Exists vFName.[0] )
        Assert.False( File.Exists vFName.[1] )
        Assert.True( File.Exists vFName.[2] )

        k.NoticeTerminate()

        let expectText1 =
            "aaa" + Environment.NewLine +
            "bbb" + Environment.NewLine
        let fcont1 =
            Functions.ReadAllTextAsync vFName.[0]
            |> Functions.RunTaskSynchronously
            |> Functions.GetOkValue ""
        Assert.True(( fcont1 = expectText1 ))

        let expectText2 =
            "eee" + Environment.NewLine +
            "fff" + Environment.NewLine
        let fcont2 =
            Functions.ReadAllTextAsync vFName.[2]
            |> Functions.RunTaskSynchronously
            |> Functions.GetOkValue ""
        Assert.True(( fcont2 = expectText2 ))

        GlbFunc.DeleteDir dname