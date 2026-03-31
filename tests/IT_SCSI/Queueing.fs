//=============================================================================
// Haruka Software Storage.
// Queueing.fs : Test cases for SCSI task queueing behavior.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.IT.SCSI

//=============================================================================
// Import declaration

open System
open System.IO
open System.Net
open System.Threading.Tasks

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.Test
open System.Text.RegularExpressions

//=============================================================================
// Class implementation

[<CollectionDefinition( "SCSI_Queueing" )>]
type SCSI_Queueing_Fixture() =

    let m_iSCSIPortNo = GlbFunc.nextTcpPortNo()
    let m_MediaSize = 65536u

    // Add default configurations
    let AddDefaultConf( client : ClientProc ): unit =

        ///////////////////////////////
        // Target Device 0

        // Target device, Target group
        client.RunCommand "create" "Created" "CR> "
        client.RunCommand "select 0" "" "TD> "
        client.RunCommand "set loglevel VERBOSE" "" "TD> "
        client.RunCommand "create targetgroup" "Created" "TD> "
        client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo ) "Created" "TD> "
        client.RunCommand "select 0" "" "TG> "

        // Target, LU
        client.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        client.RunCommand "select 0" "" "T > "
        client.RunCommand "create /l 1" "Created" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand "create debug" "Created" "LU> "
        client.RunCommand "select 0" "" "MD> "
        client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "MD> "

        client.RunCommand "validate" "All configurations are vlidated" "MD> "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "MD> "
        client.RunCommand "start" "Started" "MD> "
        client.RunCommand "add trap /e TestUnitReady /a Delay /ms 1000" "Trap added" "MD> "

    // Start controller and client
    let m_Controller, m_Client =
        let workPath = Functions.AppendPathName ( Path.GetTempPath() ) ( Guid.NewGuid().ToString( "N" ) )
        let controllPortNo = GlbFunc.nextTcpPortNo()
        let controller, client = ControllerFunc.StartHarukaController workPath controllPortNo
        AddDefaultConf client
        controller, client

    interface IDisposable with
        member _.Dispose (): unit =
            m_Client.Kill()

    interface ICollectionFixture<SCSI_Queueing_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.iSCSIPortNo = m_iSCSIPortNo
    member _.MediaSize = m_MediaSize
    member _.MediaBlockSize = 
        if Constants.MEDIA_BLOCK_SIZE = 512UL then     // 4096 or 512 bytes
            Blocksize.BS_512
        else
            Blocksize.BS_4096


[<Collection( "SCSI_Queueing" )>]
type SCSI_Queueing( fx : SCSI_Queueing_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_CID1 = cid_me.fromPrim 1us
    let g_CID2 = cid_me.fromPrim 2us

    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL

    let g_DefITT = itt_me.fromPrim 0xFFFFFFFFu
    let g_DefTTT = ttt_me.fromPrim 0xFFFFFFFFu

    let iSCSIPortNo = fx.iSCSIPortNo
    let m_MediaSize = fx.MediaSize
    let m_MediaBlockSize = fx.MediaBlockSize
    let m_ClientProc = fx.clientProc

    // default session parameters
    let m_defaultSessParam = {
        InitiatorName = "iqn.2020-05.example.com:initiator";
        InitiatorAlias = "aaa";
        TargetName = "iqn.2020-05.example.com:target1";
        TargetAlias = "";
        ISID = isid_me.fromPrim 1UL;
        TSIH = tsih_me.zero;
        MaxConnections = Constants.NEGOPARAM_MaxConnections;
        InitialR2T = false;
        ImmediateData = true;
        MaxBurstLength = Constants.NEGOPARAM_DEF_MaxBurstLength;
        FirstBurstLength = Constants.NEGOPARAM_DEF_FirstBurstLength;
        DefaultTime2Wait = Constants.NEGOPARAM_DEF_DefaultTime2Wait;
        DefaultTime2Retain = Constants.NEGOPARAM_DEF_DefaultTime2Retain;
        MaxOutstandingR2T = Constants.NEGOPARAM_DEF_MaxOutstandingR2T;
        DataPDUInOrder = false;
        DataSequenceInOrder = false;
        ErrorRecoveryLevel = 1uy;
    }

    // default connection parameters
    let m_defaultConnParam = {
        PortNo = iSCSIPortNo;
        CID = g_CID0;
        Initiator_UserName = "";
        Initiator_Password = "";
        Target_UserName = "";
        Target_Password = "";
        HeaderDigest = DigestType.DST_CRC32C;
        DataDigest = DigestType.DST_CRC32C;
        MaxRecvDataSegmentLength_I = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
        MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
    }

    // Get a list of tasks that are stalled by the debug media wait action.
    let GetStuckTasks() : ( string * TSIH_T * ITT_T ) array =
        let rx = Regex( "^ *([^ ]*) *\( *TSIH *= *([0-9]*) *, *ITT *= *([0-9]*) *\) *$" )
        m_ClientProc.RunCommandGetResp "task list" "MD> "
        |> Array.choose( fun itr ->
            let m = rx.Match itr
            if not m.Success then
                None
            else
                let method = m.Groups.[1].Value |> _.ToUpperInvariant()
                let tsih = m.Groups.[2].Value |> UInt16.Parse |> tsih_me.fromPrim
                let itt = m.Groups.[3].Value |> UInt32.Parse |> itt_me.fromPrim
                Some( method, tsih, itt )
        )

    let GetTaskSetStatus() : ( string * string ) [] =
        let rx = Regex( "^ *(Running|Dormant) : (.*)$" )
        m_ClientProc.RunCommand "unselect" "" "LU> "
        let stat = m_ClientProc.RunCommandGetResp "LUSTATUS" "LU> "
        m_ClientProc.RunCommand "select 0" "" "MD> "
        stat
        |> Array.choose( fun itr ->
            let m = rx.Match itr
            if not m.Success then
                None
            else
                Some( m.Groups.[1].Value, m.Groups.[2].Value )
        )

    let GetDormantTaskCount() : int =
        GetTaskSetStatus()
        |> Array.sumBy ( fun ( s, _ ) -> if s = "Dormant" then 1 else 0 )

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    // Check that standard INQUIRY's BQue is 0 and CmdQue is 1.
    [<Fact>]
    member _.CheckStanderdInquiry_Queueing_001() =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt = r.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.F 0uy 128us NACA.T
            let! result = r.Wait_Inquiry_Standerd itt
            Assert.False(( result.BQueue ))
            Assert.True(( result.CmdQueue ))

            do! r.Close()
        }

    // Two tasks are executed simultaneously.
    // If a second task is submitted after the first task is submitted, the two tasks will be executed simultaneously.
    [<Theory>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK, TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK, TaskATTRCd.TAGLESS_TASK )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK, TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK, TaskATTRCd.TAGLESS_TASK )>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK )>]
    [<InlineData( TaskATTRCd.ORDERED_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK )>]
    [<InlineData( TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK )>]
    member _.ConcurrencyExecution_001 ( at1 :  TaskATTRCd ) ( at2 :  TaskATTRCd ) =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "

            // Submit first task
            let! itt_r1 = r.Send_Read10 at1 g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that above task is stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 10

            // Submit second task
            let! itt_r2 = r.Send_Read10 at2 g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that above task is stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 2 ) do
                do! Task.Delay 10

            // Check that two tasks are running at the same time
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 2 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))

            // Resume execution of a waiting task
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r2 ) "Task(" "MD> "

            // Receive responses of above two tasks
            let! result_r1 = r.WaitSCSIResponseGoodStatus itt_r1
            result_r1.Return()
            let! result_r2 = r.WaitSCSIResponseGoodStatus itt_r2
            result_r2.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // The two tasks are executed sequentially.
    // When two tasks are submitted, the second task will be executed after the first task is completed.
    [<Theory>]
    [<InlineData( TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK )>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK, TaskATTRCd.ORDERED_TASK )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK, TaskATTRCd.ORDERED_TASK )>]
    [<InlineData( TaskATTRCd.ORDERED_TASK, TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.ORDERED_TASK, TaskATTRCd.TAGLESS_TASK )>]
    [<InlineData( TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK )>]
    [<InlineData( TaskATTRCd.ORDERED_TASK, TaskATTRCd.ORDERED_TASK )>]
    member _.SequentiallyExecution_001 ( at1 :  TaskATTRCd ) ( at2 :  TaskATTRCd ) =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "

            // Submit first task.
            let! itt_r1 = r.Send_Read10 at1 g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that the task 1 is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5

            // Submit second task.
            let! itt_r2 = r.Send_Read10 at2 g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until the task 2 is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 1 ) do
                do! Task.Delay 5

            // Only the task 1 is running.
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))

            // Resume execution of the task 1.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "

            // Receive response of the task 1.
            let! result_r1 = r.WaitSCSIResponseGoodStatus itt_r1
            result_r1.Return()

            // Confirm that the task 2 is stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 10
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))

            // Resume execution of the task 2.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r2 ) "Task(" "MD> "

            // Receive response of the task 2.
            let! result_r2 = r.WaitSCSIResponseGoodStatus itt_r2
            result_r2.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // Submit tasks in the order of HOQ1, SIMPLE2, HOQ3, and SIMPLE4.
    // If HOQ1 finishes first, HOQ3 and SIMPLE2 will be executed simultaneously.
    [<Theory>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK )>]
    member _.HOQ_Simple_HOQ_Simple_001 ( simpleatt : TaskATTRCd ) =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "

            // Submit a HEAD OF QUEUE 1 task.
            let! itt_r1 = r.Send_Read10 TaskATTRCd.HEAD_OF_QUEUE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that the HEAD OF QUEUE 1 task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5

            // Submit a SIMPLE 2 task.
            let! itt_r2 = r.Send_Read10 simpleatt g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until the SIMPLE 2 task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 1 ) do
                do! Task.Delay 5

            // Submit a HEAD OF QUEUE 3 task.
            let! itt_r3 = r.Send_Read10 TaskATTRCd.HEAD_OF_QUEUE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that the HEAD OF QUEUE 3 task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 2 ) do
                do! Task.Delay 5

            // Submit a SIMPLE 4 task.
            let! itt_r4 = r.Send_Read10 simpleatt g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until the SIMPLE 4 task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // Only two HEAD OF QUEUE tasks are running.
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 2 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r3 ) ) tasks ))

            // Resume execution of the HEAD OF QUEUE 1 task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "

            // Receive response of the HEAD OF QUEUE 1 task.
            let! result_r1 = r.WaitSCSIResponseGoodStatus itt_r1
            result_r1.Return()

            // Confirm that the SIMPLE 2 task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 2 ) do
                do! Task.Delay 5

            // The HEAD OF QUEUE 3 and SIMPLE 2 tasks are run simultaneously.
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 2 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r3 ) ) tasks ))

            // Resume execution of the HEAD OF QUEUE 3 task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r3 ) "Task(" "MD> "

            // Receive response of the HEAD OF QUEUE 3 task.
            let! result_r3 = r.WaitSCSIResponseGoodStatus itt_r3
            result_r3.Return()

            // Confirm that the SIMPLE 4 task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 2 ) do
                do! Task.Delay 5

            // The SIMPLE 2 and SIMPLE 4 tasks are run simultaneously.
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 2 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r4 ) ) tasks ))

            // Resume execution of the SIMPLE 2 and 4 tasks.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r2 ) "Task(" "MD> "
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r4 ) "Task(" "MD> "

            // Receive response of the SIMPLE task.
            let! result_r2 = r.WaitSCSIResponseGoodStatus itt_r2
            result_r2.Return()
            let! result_r4 = r.WaitSCSIResponseGoodStatus itt_r4
            result_r4.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // Submit tasks in the order of HOQ1, SIMPLE2, HOQ3, and SIMPLE4.
    // If HOQ3 finishes first, SIMPLE2 and SIMPLE4 will not be executed, and once HOQ1 finishes, SIMPLE2 and SIMPLE4 will be executed simultaneously.
    [<Theory>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK )>]
    member _.HOQ_Simple_HOQ_Simple_002 ( simpleatt : TaskATTRCd ) =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "

            // Submit a HEAD OF QUEUE 1 task.
            let! itt_r1 = r.Send_Read10 TaskATTRCd.HEAD_OF_QUEUE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that the HEAD OF QUEUE 1 task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5

            // Submit a SIMPLE 2 task.
            let! itt_r2 = r.Send_Read10 simpleatt g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until the SIMPLE 2 task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 1 ) do
                do! Task.Delay 5

            // Submit a HEAD OF QUEUE 3 task.
            let! itt_r3 = r.Send_Read10 TaskATTRCd.HEAD_OF_QUEUE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that the HEAD OF QUEUE 3 task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 2 ) do
                do! Task.Delay 5

            // Submit a SIMPLE 4 task.
            let! itt_r4 = r.Send_Read10 simpleatt g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until the SIMPLE 4 task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // Only two HEAD OF QUEUE tasks are running.
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 2 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r3 ) ) tasks ))

            // Resume execution of the HEAD OF QUEUE 3 task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r3 ) "Task(" "MD> "

            // Receive response of the HEAD OF QUEUE 1 task.
            let! result_r3 = r.WaitSCSIResponseGoodStatus itt_r3
            result_r3.Return()

            // Ensure that no SIMPLE tasks are executed.
            do! Task.Delay 100
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))

            // Resume execution of the HEAD OF QUEUE 1 task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "

            // Receive response of the HEAD OF QUEUE 1 task.
            let! result_r1 = r.WaitSCSIResponseGoodStatus itt_r1
            result_r1.Return()

            // Confirm that the SIMPLE 2 and 4 tasks are stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 2 ) do
                do! Task.Delay 10

            // The SIMPLE 2 and SIMPLE 4 tasks are run simultaneously.
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 2 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r4 ) ) tasks ))

            // Resume execution of the SIMPLE 2 and 4 tasks.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r2 ) "Task(" "MD> "
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r4 ) "Task(" "MD> "

            // Receive response of the SIMPLE task.
            let! result_r2 = r.WaitSCSIResponseGoodStatus itt_r2
            result_r2.Return()
            let! result_r4 = r.WaitSCSIResponseGoodStatus itt_r4
            result_r4.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // After submitting two SIMPLE tasks, submit the ORDERED task.
    // After the two SIMPLE tasks are completed, the ORDERED task is executed.
    [<Theory>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK )>]
    member _.Simple_Simple_Ordered_001 ( simpleatt : TaskATTRCd ) =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "

            // Submit two SIMPLE tasks.
            let! itt_r1 = r.Send_Read10 simpleatt g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! itt_r2 = r.Send_Read10 simpleatt g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that above two tasks are stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 2 ) do
                do! Task.Delay 5

            // Submit ORDERED task.
            let! itt_r3 = r.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until the task 2 is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 1 ) do
                do! Task.Delay 5

            // Only the SIMPLE tasks are running.
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 2 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))

            // Resume execution of the SIMPLE task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "
            let! result_r1 = r.WaitSCSIResponseGoodStatus itt_r1
            result_r1.Return()

            // ORDERED tasks have not yet been executed.
            do! Task.Delay 100
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))

            // Resume execution of the SIMPLE task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r2 ) "Task(" "MD> "
            let! result_r2 = r.WaitSCSIResponseGoodStatus itt_r2
            result_r2.Return()

            // The ORDERED task is executed.
            do! Task.Delay 5
            while ( GetStuckTasks() <> [| ( "READ", r.TSIH, itt_r3 ) |] ) do
                do! Task.Delay 5

            // Resume execution of the ORDERED task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r3 ) "Task(" "MD> "
            let! result_r3 = r.WaitSCSIResponseGoodStatus itt_r3
            result_r3.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // After submitting the ORDERED task, submit two SIMPLE tasks.
    // After the ORDERED task is completed, two SIMPLE tasks are executed.
    [<Theory>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK )>]
    member _.Ordered_Simple_Simple_001 ( simpleatt : TaskATTRCd ) =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "

            // Submit ORDERED task.
            let! itt_r1 = r.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that ORDERED task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5

            // Submit two SIMPLE tasks.
            let! itt_r2 = r.Send_Read10 simpleatt g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! itt_r3 = r.Send_Read10 simpleatt g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until the two SIMPLE tasks are queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // Only the ORDERED task is running.
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))

            // Resume execution of the ORDERED task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "
            let! result_r1 = r.WaitSCSIResponseGoodStatus itt_r1
            result_r1.Return()

            // Wait for two SIMPLE tasks to be executed
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 2 ) do
                do! Task.Delay 5
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 2 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r3 ) ) tasks ))

            // Resume execution of two SIMPLE tasks.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r2 ) "Task(" "MD> "
            let! result_r2 = r.WaitSCSIResponseGoodStatus itt_r2
            result_r2.Return()
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r3 ) "Task(" "MD> "
            let! result_r3 = r.WaitSCSIResponseGoodStatus itt_r3
            result_r3.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // Submit tasks in the order of SIMPLE, ORDERED, SIMPLE. Abort the dormant ORDERED task.
    // Two SIMPLE tasks are executed simultaneously.
    [<Theory>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK )>]
    member _.Simple_Ordered_Simple_001 ( simpleatt : TaskATTRCd ) =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "

            // Submit a SIMPLE task.
            let! itt_r1 = r.Send_Read10 simpleatt g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that SIMPLE task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5

            // Submit a ORDERED tasks.
            let! itt_r2 = r.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until the ORDERED task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 1 ) do
                do! Task.Delay 5

            // Submit a SIMPLE task.
            let! itt_r3 = r.Send_Read10 simpleatt g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until the SIMPLE task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // abort the ORDERD task
            let cmdsn = r.GetCmdSN itt_r2
            let! itt_tmf = r.SendTMFRequest_AbortNonImmidiateTask BitI.F g_LUN1 itt_r2 cmdsn.Value
            let! res_tmf = r.WaitTMFResponse itt_tmf
            Assert.True(( res_tmf = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Two SIMPLE tasks are executed simultaneously.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 2 ) do
                do! Task.Delay 10
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 2 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r3 ) ) tasks ))

            // Resume execution of two SIMPLE tasks.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "
            let! result_r1 = r.WaitSCSIResponseGoodStatus itt_r1
            result_r1.Return()
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r3 ) "Task(" "MD> "
            let! result_r3 = r.WaitSCSIResponseGoodStatus itt_r3
            result_r3.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // Submit tasks in the order of ORDERED1, ORDERED2, HOQ.
    // ORDERED1 and HOQ tasks are executed simultaneously.
    // If the HOQ task finishes first, the ORDERED2 task will be executed after the ORDERED1 task finishes.
    [<Fact>]
    member _.Ordered_Ordered_HOQ_001 () =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "

            // Submit a ORDERED 1 task.
            let! itt_r1 = r.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that ORDERED 1 task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5

            // Submit a ORDERED 2 tasks.
            let! itt_r2 = r.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until the ORDERED task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 1 ) do
                do! Task.Delay 5

            // Submit a HOQ task.
            let! itt_r3 = r.Send_Read10 TaskATTRCd.HEAD_OF_QUEUE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that HOQ task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 2 ) do
                do! Task.Delay 5

            // ORDERED1 and HOQ tasks are executed simultaneously.
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 2 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r3 ) ) tasks ))

            // Resume execution of the HOQ task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r3 ) "Task(" "MD> "
            let! result_r3 = r.WaitSCSIResponseGoodStatus itt_r3
            result_r3.Return()

            // ORDERED 2 task has not yet been executed
            do! Task.Delay 100
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))

            // Resume execution of the ORDERED 1 task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "
            let! result_r1 = r.WaitSCSIResponseGoodStatus itt_r1
            result_r1.Return()

            // Confirm that ORDERED 2 task is stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 10
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))

            // Resume execution of ORDERED 2 task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r2 ) "Task(" "MD> "
            let! result_r2 = r.WaitSCSIResponseGoodStatus itt_r2
            result_r2.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // Submit tasks in the order of ORDERED1, ORDERED2, HOQ.
    // ORDERED1 and HOQ tasks are executed simultaneously.
    // If the ORDERED1 task finishes first, the ORDERED2 task and the HOQ task will be executed simultaneously.
    [<Fact>]
    member _.Ordered_Ordered_HOQ_002 () =

        let rec Wait_Order_2_task ( r : SCSI_Initiator ) ( itt_r2 : ITT_T ) ( itt_r3 : ITT_T ) =
            let tasks = GetStuckTasks()
            let e1 = tasks.Length = 2
            let e2 = Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks
            let e3 = Array.exists( (=) ( "READ", r.TSIH, itt_r3 ) ) tasks
            if not( e1 && e2 && e3 ) then
                System.Threading.Thread.Sleep 5
                Wait_Order_2_task r itt_r2 itt_r3

        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "

            // Submit a ORDERED 1 task.
            let! itt_r1 = r.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that ORDERED 1 task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5

            // Submit a ORDERED 2 tasks.
            let! itt_r2 = r.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until the ORDERED 2 task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 1 ) do
                do! Task.Delay 5

            // Submit a HOQ task.
            let! itt_r3 = r.Send_Read10 TaskATTRCd.HEAD_OF_QUEUE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that HOQ task is stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 2 ) do
                do! Task.Delay 10

            // ORDERED 1 and HOQ tasks are executed simultaneously.
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 2 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r3 ) ) tasks ))

            // Resume execution of the ORDERED 1 task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "
            let! result_r1 = r.WaitSCSIResponseGoodStatus itt_r1
            result_r1.Return()

            // ORDERED2 task is executed
            do! Task.Delay 5
            Wait_Order_2_task r itt_r2 itt_r3

            // Resume execution of HOQ and ORDERED 2 task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r2 ) "Task(" "MD> "
            let! result_r2 = r.WaitSCSIResponseGoodStatus itt_r2
            result_r2.Return()
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r3 ) "Task(" "MD> "
            let! result_r3 = r.WaitSCSIResponseGoodStatus itt_r3
            result_r3.Return()
            
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // When ACA is not established, an ACA task is submitted.
    // The ACA task ends with a CHECK CONDITION.
    [<Fact>]
    member _.ACA_InNormalState_001 () =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // Submit a ACA task.
            let! itt_r1 = r.Send_Read10 TaskATTRCd.ACA_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! resp_r1 = r.WaitSCSIResponse itt_r1
            Assert.True(( resp_r1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // Clear ACA
            let! tmf_r1 = r.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! resp_tmf = r.WaitTMFResponse tmf_r1
            Assert.True(( resp_tmf = TaskMgrResCd.FUNCTION_COMPLETE ))

            do! r.Close()
        }

    // When ACA is established, SIMPLE, HOQ, ORDERED, TAGLESS task are submitted.
    // The other that ACA task ends with a CHECK CONDITION.
    [<Theory>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.ORDERED_TASK )>]
    [<InlineData( TaskATTRCd.HEAD_OF_QUEUE_TASK )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK )>]
    member _.NormalTask_InACAState_001 ( att : TaskATTRCd ) =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // establish ACA.
            let! itt_r1 = r.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0xFFFFFFFFu ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! resp_r1 = r.WaitSCSIResponse itt_r1
            Assert.True(( resp_r1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // Submit a task.
            let! itt_r1 = r.Send_Read10 att g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! resp_r1 = r.WaitSCSIResponse itt_r1
            Assert.True(( resp_r1.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // Clear ACA
            let! tmf_r1 = r.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! resp_tmf = r.WaitTMFResponse tmf_r1
            Assert.True(( resp_tmf = TaskMgrResCd.FUNCTION_COMPLETE ))

            do! r.Close()
        }

    // Submit the ACA task while the SIMPLE task or ORDERED task are dormant.
    // The ACA task is executed.
    // After the ACA condition is cleared, the dormant task resumes execution.
    [<Theory>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.ORDERED_TASK )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK )>]
    member _.ACA_InACA_001 ( att : TaskATTRCd ) =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /a ACA /slba 10 /elba 20" "Trap added" "MD> "

            // Submit a ORDERED 1 task.
            let! itt_r1 = r.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that ORDERED 1 task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))

            // Submit SIMPLE or ORDERED task.
            let! itt_r2 = r.Send_Read10 att g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 1 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "
            let! result_r1 = r.WaitSCSIResponse itt_r1
            Assert.True(( result_r1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // Task execution is not resumed.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length >= 1 ) do
                do! Task.Delay 5

            // Submit ACA task.
            let! itt_r4 = r.Send_Read10 TaskATTRCd.ACA_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // ACA task is executed.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r4 ) ) tasks ))

            // Resume execution of ACA task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r4 ) "Task(" "MD> "
            let! result_r4 = r.WaitSCSIResponseGoodStatus itt_r4
            result_r4.Return()

            // Clear ACA
            let! tmf_r1 = r.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! resp_tmf = r.WaitTMFResponse tmf_r1
            Assert.True(( resp_tmf = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Confirm that SIMPLE or ORDERED task is executed.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))

            // Resume execution of SIMPLE or ORDERED task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r2 ) "Task(" "MD> "
            let! result_r2 = r.WaitSCSIResponseGoodStatus itt_r2
            result_r2.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // Submit the ACA task while the SIMPLE task or ORDERED task are dormant.
    // After the ACA condition is cleared, the dormant task resumes execution.
    [<Theory>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.ORDERED_TASK )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK )>]
    member _.ClearACA_001 ( att : TaskATTRCd ) =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /a ACA /slba 10 /elba 20" "Trap added" "MD> "

            // Submit a ORDERED 1 task.
            let! itt_r1 = r.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that ORDERED 1 task is stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))

            // Submit SIMPLE or ORDERED task.
            let! itt_r2 = r.Send_Read10 att g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 1 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "
            let! result_r1 = r.WaitSCSIResponse itt_r1
            Assert.True(( result_r1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // Task execution is not resumed.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length >= 1 ) do
                do! Task.Delay 5

            // Clear ACA by failing the CA task.
            let! itt_r3 = r.Send_Read10 TaskATTRCd.ACA_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.F
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r3 ) "Task(" "MD> "
            let! result_r3 = r.WaitSCSIResponse itt_r3
            Assert.True(( result_r3.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // Confirm that SIMPLE or ORDERED task is executed.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))

            // Resume execution of SIMPLE or ORDERED task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r2 ) "Task(" "MD> "
            let! result_r2 = r.WaitSCSIResponseGoodStatus itt_r2
            result_r2.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // Submit SIMPLE1, SIMPLE2, and ORDERED tasks. Then SIMPLE1 fails and ACA is established.
    // After the ACA is cleared, the ORDERED task will not be executed until the SIMPLE2 task is completed.
    [<Fact>]
    member _.ClearACA_002 () =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /a ACA /slba 10 /elba 20" "Trap added" "MD> "

            // Submit a SIMPLE 1 and SIMPLE 2 task.
            let! itt_r1 = r.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! itt_r2 = r.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that above two tasks are stuck.
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 2 ) do
                do! Task.Delay 5

            // Submit ORDERED task.
            let! itt_r3 = r.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 1 ) do
                do! Task.Delay 5

            // The ORDERED tasks will not be executed
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 2 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))

            // Resume execution of SIMPLE 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "
            let! result_r1 = r.WaitSCSIResponse itt_r1
            Assert.True(( result_r1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // Clear ACA
            let! tmf_r1 = r.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! resp_tmf = r.WaitTMFResponse tmf_r1
            Assert.True(( resp_tmf = TaskMgrResCd.FUNCTION_COMPLETE ))

            // The ORDERED tasks will not be executed
            do! Task.Delay 100
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))

            // Resume execution of SIMPLE 2 task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r2 ) "Task(" "MD> "
            let! result_r2 = r.WaitSCSIResponseGoodStatus itt_r2
            result_r2.Return()

            // The ORDERED task is executed
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 5
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r3 ) ) tasks ))

            // Resume execution of the ORDERED task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r3 ) "Task(" "MD> "
            let! result_r3 = r.WaitSCSIResponseGoodStatus itt_r3
            result_r3.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

