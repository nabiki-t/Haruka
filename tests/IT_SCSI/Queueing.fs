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
      
    // Submit two SIMPLE tasks at the same time and check that they are executed at the same time.
    [<Fact>]
    member _.Simple_Simple_001() =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "

            // Submit two SIMPLE tasks at the same time
            let! itt_r1 = r.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! itt_r2 = r.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that above task is stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 1 ) do
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
            let! result_r1 = r.WaitSCSIResponseGoogStatus itt_r1
            result_r1.Return()
            let! result_r2 = r.WaitSCSIResponseGoogStatus itt_r2
            result_r2.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // If a HEAD OF QUEUE task is submitted after a SIMPLE task, the two tasks will be executed simultaneously.
    [<Fact>]
    member _.Simple_HOQ_001() =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "

            // Submit SIMPLE task
            let! itt_r1 = r.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that above task is stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 10

            // Submit HEAD OF QUEUE task
            let! itt_r2 = r.Send_Read10 TaskATTRCd.HEAD_OF_QUEUE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

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
            let! result_r1 = r.WaitSCSIResponseGoogStatus itt_r1
            result_r1.Return()
            let! result_r2 = r.WaitSCSIResponseGoogStatus itt_r2
            result_r2.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }

    // If a SIMPLE task is submitted after a HEAD OF QUEUE task, the SIMPLE task will be executed after the HEAD OF QUEUE task has finished.
    [<Fact>]
    member _.Simple_HOQ_002() =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            m_ClientProc.RunCommand "add trap /e Read /a Wait" "Trap added" "MD> "

            // Submit a HEAD OF QUEUE task.
            let! itt_r1 = r.Send_Read10 TaskATTRCd.HEAD_OF_QUEUE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Confirm that the HEAD OF QUEUE task is stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 10

            // Submit a SIMPLE task.
            let! itt_r2 = r.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until the SIMPLE task is queued.
            do! Task.Delay 100

            // Only the HEAD OF QUEUE task is running.
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r1 ) ) tasks ))

            // Resume execution of the HEAD OF QUEUE task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r1 ) "Task(" "MD> "

            // Receive response of the HEAD OF QUEUE task.
            let! result_r1 = r.WaitSCSIResponseGoogStatus itt_r1
            result_r1.Return()

            // Confirm that the SIMPLE task is stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 10
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = 1 ))
            Assert.True(( Array.exists( (=) ( "READ", r.TSIH, itt_r2 ) ) tasks ))

            // Resume execution of the SIMPLE task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r.TSIH itt_r2 ) "Task(" "MD> "

            // Receive response of the SIMPLE task.
            let! result_r2 = r.WaitSCSIResponseGoogStatus itt_r2
            result_r2.Return()

            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
            do! r.Close()
        }