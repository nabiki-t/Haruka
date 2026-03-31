//=============================================================================
// Haruka Software Storage.
// PersistentReserveOut5.fs : Test cases for PERSISTENT RESERVE OUT command.
//

//=============================================================================
// Comment
// Clarification of specifications regarding the interaction between ACA and PREEMPT AND ABORT service action.

(*
Condition
A1 : The persistent reservation is not an all registrants type (Including cases where there is no persistent reservation)
 A11 : The PR-OUT command preempts the faulted I_T nexus.
 A12 : The PR-OUT command preempts the non-faulted I_T nexus.
A2 : The persistent reservation is an all registrants type (Including cases where faulted I_T nexus is not registered.)
 A21 : SERVICE ACTION RESERVATION KEY=0
 A22 : SERVICE ACTION RESERVATION KEY<>0 ( Corresponding I_T Nexus exists. )
 A22 : SERVICE ACTION RESERVATION KEY<>0 ( No corresponding I_T Nexus exists. )

B1 : The task is ACA attribute.
B2 : The task is not ACA attribute.

C1 : The PR-OUT command was sent from the faulted I_T nexus.
C2 : The PR-OUT command was sent from the non-faulted I_T nexus.


Pattern
The persistent reservation is not an all registrants type.
 A11-B1-C1 : The faulted I_T nexus attempts to preempt itself in the ACA task.
   Task execution      : Execute (The original ACA rules)
   Tasks to be aborted : All tasks sent from I_T Nexus (=itself) that are subject to preemption.
   ACA to be cleared   : ACA related to I_T Nexus (= itself) that is subject to preemption.
   reffer to SAM-2 5.9.1.4, SAM-2 5.9.1.7 c), SPC-3 5.6.10.5 c), SPC-3 5.6.10.5 d)
 A11-B1-C2 : The non-faulted I_T nexus performs a preemption to the faulted I_T nexus in the ACA task.
   Task execution      : Do not execute (ACA ACTIVE)
   The status will not change.
   reffer SAM-2 5.9.1.5.2 Table 30
 A11-B2-C1 : The faulted I_T nexus attempts to preempt itself in the non-ACA task.
   Task execution      : Do not execute (ACA ACTIVE)
   The status will not change.
   reffer SAM-2 5.9.1.4
 A11-B2-C2 : The non-faulted I_T nexus performs a preemption to the faulted I_T nexus in the non ACA task.
   Task execution      : Execute (Execute tasks under special rules)
   Tasks to be aborted : All tasks sent from I_T Nexus that are subject to preemption.
   ACA to be cleared   : ACA related to I_T Nexus that is subject to preemption.
   reffer SAM-2 5.9.1.5.1, SAM-2 5.9.1.5.2 Table 30, SAM-2 5.9.1.7 d), SPC-3 5.6.10.5 a) B), SPC-3 5.6.10.5 c), SPC-3 5.6.10.5 d)
 A12-B1-C1 : The faulted I_T nexus performs a preemption to the non-faulted I_T nexus in the ACA task.
   Task execution      : Execute (The original ACA rules)
   Tasks to be aborted : All tasks sent from I_T Nexus that are subject to preemption.
   ACA to be cleared   : ACA remains. ( Since the I_T Nexus to be preempted is not a faulted I_T Nexus, there is no ACA to clear. )
   reffer SAM-2 5.9.1.4
 A12-B1-C2 : The non-faulted I_T nexus performs a preemption to the non-faulted I_T nexus in the ACA task.
   Task execution      : Do not execute (ACA ACTIVE)
   The status will not change.
   reffer SAM-2 5.9.1.5.2 Table 30
 A12-B2-C1 : The faulted I_T nexus performs a preemption to the non-faulted I_T nexus in the non-ACA task.
   Task execution      : Do not execute (ACA ACTIVE)
   The status will not change.
   reffer SAM-2 5.9.1.4
 A12-B2-C2 : The non-faulted I_T nexus performs a preemption to the non-faulted I_T nexus in the non-ACA task.
   Task execution      : Do not execute (ACA ACTIVE)
                         According to Special Rule SPC-3 5.6.10.5 a) A) for Special Rule SAM-2 5.9.1.5.1,
                         the command terminates with ACA ACTIVE.
   The status will not change.
   reffer SAM-2 5.9.1.5.1, SPC-3 5.6.10.5 a) A)

The persistent reservation is an all registrants type
 A21-B1-C1 : The faulted I_T nexus performs a preemption in the ACA task and SERVICE ACTION RESERVATION KEY=0.
   Task execution      : Execute (The original ACA rules)
   Tasks to be aborted : All of tasks.
   ACA to be cleared   : All of ACA ( ACA cleared based on special rules ).
   reffer SAM-2 5.9.1.4,  SAM-2 5.9.1.7 c), SPC-3 5.6.10.5 e) A)
 A21-B1-C2 : The non-faulted I_T nexus performs a preemption in the ACA task and SERVICE ACTION RESERVATION KEY=0.
   Task execution      : Do not execute (ACA ACTIVE)
   The status will not change.
   reffer SAM-2 5.9.1.5.2 Table 30
 A21-B2-C1 : The faulted I_T nexus performs a preemption in the non-ACA task and SERVICE ACTION RESERVATION KEY=0.
   Task execution      : Do not execute (ACA ACTIVE)
   The status will not change.
   reffer SAM-2 5.9.1.4
 A21-B2-C2 : The non-faulted I_T nexus performs a preemption in the non-ACA task and SERVICE ACTION RESERVATION KEY=0.
   Task execution      : Execute (Execute tasks under special rules)
   Tasks to be aborted : All of tasks.
   ACA to be cleared   : All of ACA.
   reffer SAM-2 5.9.1.5.1, SAM-2 5.9.1.5.2 Table 30, SAM-2 5.9.1.7 d), SPC-3 5.6.10.5 e) A)
 A22-B1-C1 : The faulted I_T nexus performs a preemption in the ACA task and SERVICE ACTION RESERVATION KEY<>0(Corresponding I_T Nexus exists).
   Task execution      : Execute (The original ACA rules)
   Tasks to be aborted : All tasks sent from I_T Nexus that are subject to preemption.
   ACA to be cleared   : ACA related to I_T Nexus that is subject to preemption ( ACA cleared based on special rules ).
   reffer SAM-2 5.9.1.4,  SAM-2 5.9.1.7 c), SPC-3 5.6.10.5 e) B)
 A22-B1-C2 : The non-faulted I_T nexus performs a preemption in the ACA task and SERVICE ACTION RESERVATION KEY<>0(Corresponding I_T Nexus exists).
   Task execution      : Do not execute (ACA ACTIVE)
   The status will not change.
   reffer SAM-2 5.9.1.5.2 Table 30
 A22-B2-C1 : The faulted I_T nexus performs a preemption in the non-ACA task and SERVICE ACTION RESERVATION KEY<>0(Corresponding I_T Nexus exists).
   Task execution      : Do not execute (ACA ACTIVE)
   The status will not change.
   reffer SAM-2 5.9.1.4
 A22-B2-C2 : The non-faulted I_T nexus performs a preemption in the non-ACA task and SERVICE ACTION RESERVATION KEY<>0(Corresponding I_T Nexus exists).
   Task execution      : Execute (Execute tasks under special rules)
   Tasks to be aborted : All tasks sent from I_T Nexus that are subject to preemption.
   ACA to be cleared   : ACA related to I_T Nexus that is subject to preemption.
   reffer SAM-2 5.9.1.5.1, SAM-2 5.9.1.5.2 Table 30, SAM-2 5.9.1.7 d), SPC-3 5.6.10.5 e) B)
 A23-B1-C1 : The faulted I_T nexus performs a preemption in the ACA task and SERVICE ACTION RESERVATION KEY<>0(No corresponding I_T Nexus exists.).
   Task execution      : The task is executed according to the original ACA rules and terminated with a RESERVATION CONFLICT.
   The status will not change.
   reffer SAM-2 5.9.1.4,  SAM-2 5.9.1.7 c), SPC-3 5.6.10.4.4
 A23-B1-C2 : The non-faulted I_T nexus performs a preemption in the ACA task and SERVICE ACTION RESERVATION KEY<>0(No corresponding I_T Nexus exists).
   Task execution      : Do not execute (ACA ACTIVE)
   The status will not change.
   reffer SAM-2 5.9.1.5.2 Table 30
 A23-B2-C1 : The faulted I_T nexus performs a preemption in the non-ACA task and SERVICE ACTION RESERVATION KEY<>0(No corresponding I_T Nexus exists).
   Task execution      : Do not execute (ACA ACTIVE)
   The status will not change.
   reffer SAM-2 5.9.1.4
 A23-B2-C2 : The non-faulted I_T nexus performs a preemption in the non-ACA task and SERVICE ACTION RESERVATION KEY<>0(No corresponding I_T Nexus exists).
   Task execution      : Execute (Execute tasks under special rules)
   Tasks to be aborted : All tasks sent from I_T Nexus that are subject to preemption.
   ACA to be cleared   : ACA related to I_T Nexus that is subject to preemption.
   reffer SAM-2 5.9.1.5.1, SAM-2 5.9.1.5.2 Table 30, SPC-3 5.6.10.4.4
*)


//=============================================================================
// Namespace declaration

namespace Haruka.Test.IT.SCSI

//=============================================================================
// Import declaration

open System
open System.IO
open System.Text.RegularExpressions
open System.Threading.Tasks

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

//=============================================================================
// Class implementation

[<CollectionDefinition( "SCSI_PersistentReserveOut5" )>]
type SCSI_PersistentReserveOut5_Fixture() =

    let m_iSCSIPortNo = GlbFunc.nextTcpPortNo()
    let m_MediaSize = 65536u

    // Add default configurations
    let AddDefaultConf( client : ClientProc ): unit =

        ///////////////////////////////
        // Target Device 0

        // Target device, Target group
        client.RunCommand "create" "Created" "CR> "
        client.RunCommand "select 0" "" "TD> "
        client.RunCommand "set ID TD_00000063" "" "TD> "
        client.RunCommand "set loglevel VERBOSE" "" "TD> "
        client.RunCommand "create targetgroup" "Created" "TD> "
        client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo ) "Created" "TD> "
        client.RunCommand "select 0" "" "TG> "

        // Target, LU
        client.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        client.RunCommand "select 0" "" "T > "
        client.RunCommand "set ID 1" "" "T > "
        client.RunCommand "create /l 1" "Created" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand "create debug" "Created" "LU> "
        client.RunCommand "select 0" "" "MD> "
        client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "MD> "
        client.RunCommand "unselect" "" "LU> "
        client.RunCommand "unselect" "" "T > "
        client.RunCommand "unselect" "" "TG> "
        client.RunCommand "create /n iqn.2020-05.example.com:target2" "Created" "TG> "
        client.RunCommand "select 1" "" "T > "
        client.RunCommand "set ID 2" "" "T > "
        client.RunCommand "attach /l 1" "Attach LU" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand "select 0" "" "MD> "

        client.RunCommand "validate" "All configurations are vlidated" "MD> "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "MD> "
        client.RunCommand "start" "Started" "MD> "

    let m_WorkPath = Functions.AppendPathName ( Path.GetTempPath() ) ( Guid.NewGuid().ToString( "N" ) )

    // Start controller and client
    let m_Controller, m_Client =
        let controllPortNo = GlbFunc.nextTcpPortNo()
        let controller, client = ControllerFunc.StartHarukaController m_WorkPath controllPortNo
        AddDefaultConf client
        controller, client

    interface IDisposable with
        member _.Dispose (): unit =
            m_Client.Kill()

    interface ICollectionFixture<SCSI_PersistentReserveOut5_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.iSCSIPortNo = m_iSCSIPortNo
    member _.MediaSize = m_MediaSize
    member _.MediaBlockSize = 
        if Constants.MEDIA_BLOCK_SIZE = 512UL then     // 4096 or 512 bytes
            Blocksize.BS_512
        else
            Blocksize.BS_4096
    member _.WorkPath = m_WorkPath


[<Collection( "SCSI_PersistentReserveOut5" )>]
type SCSI_PersistentReserveOut5( fx : SCSI_PersistentReserveOut5_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_CID1 = cid_me.fromPrim 1us
    let g_CID2 = cid_me.fromPrim 2us

    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL

    let g_ResvKey1 = resvkey_me.fromPrim 1UL
    let g_ResvKey2 = resvkey_me.fromPrim 2UL
    let g_ResvKey3 = resvkey_me.fromPrim 3UL
    let g_ResvKey4 = resvkey_me.fromPrim 4UL

    let m_ClientProc = fx.clientProc
    let iSCSIPortNo = fx.iSCSIPortNo
    let m_MediaSize = fx.MediaSize
    let m_MediaBlockSize = fx.MediaBlockSize
    let m_WorkPath = fx.WorkPath

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

    let GetSortedISID ( cnt : int ) =
        Array.init cnt ( fun _ -> GlbFunc.newISID() )
        |> Array.sortBy isid_me.toPrim

    // Get the file name for the Persistent Reservation.
    let GetPRFileName ( lun : LUN_T ) : string =
        let c = Path.DirectorySeparatorChar
        let tdid = tdid_me.fromPrim 99u
        sprintf "%s%c%s%c%s%c%s" m_WorkPath c ( tdid_me.toString tdid ) c ( lun_me.WorkDirName lun ) c Constants.PR_SAVE_FILE_NAME

    let PR_ReadFullStatus ( r : SCSI_Initiator ) ( lun : LUN_T ) : Task<PR_ReadFullStatus> =
        task {
            let! itt_pr_in1 = r.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 3uy 512us NACA.T
            return! r.Wait_PersistentReserveIn_ReadFullStatus itt_pr_in1
        }

    let CheckNoRegistrations ( r : SCSI_Initiator ) ( lun : LUN_T ) : Task<uint32> =
        task {
            let! fstat1 = PR_ReadFullStatus r lun
            Assert.True(( fstat1.FullStatusDescriptor.Length = 0 ))
            return fstat1.PersistentReservationsGeneration
        }

    let PR_Register ( r : SCSI_Initiator ) ( lun : LUN_T ) ( k : RESVKEY_T ) : Task<unit> =
        task {
            let! itt_pr_out1 = r.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK lun NACA.T resvkey_me.zero k SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r.WaitSCSIResponseGoodStatus itt_pr_out1
            ()
        }

    let PR_Unregister ( r : SCSI_Initiator ) ( lun : LUN_T ) ( k : RESVKEY_T ) : Task<unit> =
        task {
            let! itt_pr_out1 = r.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK lun NACA.T k resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r.WaitSCSIResponseGoodStatus itt_pr_out1
            ()
        }

    let PR_Reserve ( r : SCSI_Initiator ) ( lun : LUN_T ) ( prtype : PR_TYPE ) ( k : RESVKEY_T ) : Task<unit> =
        task {
            let! itt_pr_out1 = r.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK lun NACA.T prtype k
            let! _ = r.WaitSCSIResponseGoodStatus itt_pr_out1
            ()
        }

    let Check_UA_Established ( r : SCSI_Initiator ) ( lun : LUN_T ) ( expASC : ASCCd ) : Task<unit> =
        task {
            let! itt = r.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK lun NACA.T
            let! res = r.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res.Sense.Value.SenseKey = SenseKeyCd.UNIT_ATTENTION ))
            Assert.True(( res.Sense.Value.ASC = expASC ))
        }

    let Check_UA_Cleared ( r : SCSI_Initiator ) ( lun : LUN_T ) : Task<unit> =
        task {
            let! itt = r.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK lun NACA.T
            let! res = r.WaitSCSIResponseGoodStatus itt
            res.Return()
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
 
    // Clear ACA
    let ClearACA ( r : SCSI_Initiator ) ( lun : LUN_T ) : Task<unit> =
        task {
            let! itt_tmf1 = r.SendTMFRequest_ClearACA BitI.F lun
            let! res_tmf1 = r.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))
        }

    let WaitTaskStucked ( cnt : int ) =
        task {
            do! Task.Delay 5
            while ( ( GetStuckTasks() ).Length < cnt ) do
                do! Task.Delay 5
            let tasks = GetStuckTasks()
            Assert.True(( tasks.Length = cnt ))
        }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    static member A11_B1_C1_001_data : obj[][] = [|
        [| PR_TYPE.NO_RESERVATION;                    |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
    |]

    // The persistent reservation is not an all registrants type.
    // The faulted I_T nexus attempts to preempt itself in the ACA task.
    [<Theory>]
    [<MemberData( "A11_B1_C1_001_data" )>]
    member _.A11_B1_C1_001 ( prtype : PR_TYPE ) =
        task {
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a ACA" "Trap added" "MD> "
            let isids = GetSortedISID 2
            let! r1 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = isids.[0] } m_defaultConnParam
            let! r3 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = isids.[1] } m_defaultConnParam
            let itn_r1 = r1.ITNexus
            let itn_r3 = r3.ITNexus

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r3 g_LUN1 g_ResvKey3
            if prtype <> PR_TYPE.NO_RESERVATION then
                do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            // send ORDERED 1 task from r1, and wait that task is stuck
            let! itt_read1 = r1.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            do! WaitTaskStucked 1

            // send SIMPLE 1, SIMPLE 2 task from r1, r3
            let! _ = r1.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! itt_read3 = r3.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_read1 ) "Task(" "MD> "
            let! res_read1 = r1.WaitSCSIResponse itt_read1
            Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // PREEMPT AND ABORT (Succeed)
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.ACA_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // The command is executable.
            // ( The ACA has been cleared. Above SIMPLE 1 task is aborted. )
            let! itt_read4 = r1.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! res_read4 = r1.WaitSCSIResponseGoodStatus itt_read4
            res_read4.Return()

            // Above SIMPLE 2 task is executed.
            let! res_read3 = r3.WaitSCSIResponseGoodStatus itt_read3
            res_read3.Return()

            // The command is executable.
            let! itt_read5 = r3.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! res_read5 = r3.WaitSCSIResponseGoodStatus itt_read5
            res_read5.Return()

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            let fsd1 = fstat1.FullStatusDescriptor |> Array.sortBy _.iSCSIName
            if prtype = PR_TYPE.NO_RESERVATION then
                Assert.True(( fsd1.Length = 1 ))
                Assert.True(( fsd1.[0].iSCSIName = itn_r3.InitiatorPortName ))
                Assert.True(( fsd1.[0].ReservationKey = g_ResvKey3 ))
                Assert.False(( fsd1.[0].ReservationHolder ))
                Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
                do! PR_Unregister r3 g_LUN1 g_ResvKey3
            else
                Assert.True(( fsd1.Length = 2 ))
                Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
                Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
                Assert.True(( fsd1.[0].ReservationHolder ))
                Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue PR_TYPE.WRITE_EXCLUSIVE ))
                Assert.True(( fsd1.[1].iSCSIName = itn_r3.InitiatorPortName ))
                Assert.True(( fsd1.[1].ReservationKey = g_ResvKey3 ))
                Assert.False(( fsd1.[1].ReservationHolder ))
                Assert.True(( fsd1.[1].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
                do! PR_Unregister r1 g_LUN1 g_ResvKey1
                do! PR_Unregister r3 g_LUN1 g_ResvKey3

            do! r1.Close()
            do! r3.Close()
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
        }

    // The persistent reservation is not an all registrants type.
    // The non-faulted I_T nexus performs a preemption to the faulted I_T nexus in the ACA task.
    [<Theory>]
    [<MemberData( "A11_B1_C1_001_data" )>]
    member _.A11_B1_C2_001 ( prtype : PR_TYPE ) =
        task {
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a ACA" "Trap added" "MD> "
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r3 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r3 g_LUN1 g_ResvKey3
            if prtype <> PR_TYPE.NO_RESERVATION then
                do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            // send ORDERED 1 task from r1, and wait that task is stuck
            let! itt_read1 = r1.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            do! WaitTaskStucked 1

            // send SIMPLE 1, SIMPLE 2 task from r1, r3
            let! itt_tur1 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! itt_tur2 = r3.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_read1 ) "Task(" "MD> "
            let! res_read1 = r1.WaitSCSIResponse itt_read1
            Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // PREEMPT AND ABORT ( Terinated with ACA ACTIVE )
            let! itt_pr_out2 = r3.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.ACA_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey3 g_ResvKey1
            let! res_pr_out2 = r3.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // clear ACA ( The execution of the SIMPLE 1 and SIMPLE 2 tasks will resume. )
            do! ClearACA r1 g_LUN1

            // receive SIMPLE 1 and SIMPLE 2 response
            let! res_tur1 = r1.WaitSCSIResponseGoodStatus itt_tur1
            res_tur1.Return()
            let! res_tur2 = r3.WaitSCSIResponseGoodStatus itt_tur2
            res_tur2.Return()

            // There is no change to the reservation status.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r3 g_LUN1 g_ResvKey3
            do! PR_Unregister r1 g_LUN1 g_ResvKey1

            do! r1.Close()
            do! r3.Close()
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
        }

    // The persistent reservation is not an all registrants type.
    // The faulted I_T nexus attempts to preempt itself in the non-ACA task.
    [<Theory>]
    [<MemberData( "A11_B1_C1_001_data" )>]
    member _.A11_B2_C1_001 ( prtype : PR_TYPE ) =
        task {
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a ACA" "Trap added" "MD> "
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r3 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r3 g_LUN1 g_ResvKey3
            if prtype <> PR_TYPE.NO_RESERVATION then
                do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            // send ORDERED 1 task from r1, and wait that task is stuck
            let! itt_read1 = r1.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            do! WaitTaskStucked 1

            // send SIMPLE 1, SIMPLE 2 task from r1, r3
            let! itt_tur1 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! itt_tur2 = r3.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_read1 ) "Task(" "MD> "
            let! res_read1 = r1.WaitSCSIResponse itt_read1
            Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // PREEMPT AND ABORT ( Terinated with ACA ACTIVE )
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1 g_ResvKey1
            let! res_pr_out2 = r1.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // clear ACA ( The execution of the SIMPLE 1 and SIMPLE 2 tasks will resume. )
            do! ClearACA r1 g_LUN1

            // receive SIMPLE 1 and SIMPLE 2 response
            let! res_tur1 = r1.WaitSCSIResponseGoodStatus itt_tur1
            res_tur1.Return()
            let! res_tur2 = r3.WaitSCSIResponseGoodStatus itt_tur2
            res_tur2.Return()

            // There is no change to the reservation status.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r3 g_LUN1 g_ResvKey3
            do! PR_Unregister r1 g_LUN1 g_ResvKey1

            do! r1.Close()
            do! r3.Close()
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
        }

    // The persistent reservation is not an all registrants type.
    // The non-faulted I_T nexus performs a preemption to the faulted I_T nexus in the non ACA task.
    [<Theory>]
    [<MemberData( "A11_B1_C1_001_data" )>]
    member _.A11_B2_C2_001 ( prtype : PR_TYPE ) =
        task {
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a ACA" "Trap added" "MD> "
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r3 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let itn_r3 = r3.ITNexus

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r3 g_LUN1 g_ResvKey3
            if prtype <> PR_TYPE.NO_RESERVATION then
                do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            // send ORDERED 1 task from r1, and wait that task is stuck
            let! itt_read1 = r1.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            do! WaitTaskStucked 1

            // send SIMPLE 1, SIMPLE 2 task from r1, r3
            let! _ = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! itt_tur2 = r3.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_read1 ) "Task(" "MD> "
            let! res_read1 = r1.WaitSCSIResponse itt_read1
            Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // PREEMPT AND ABORT (Succeed)
            let! itt_pr_out1 = r3.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey3 g_ResvKey1
            let! _ = r3.WaitSCSIResponseGoodStatus itt_pr_out1

            // UA is established for r1
            do! Check_UA_Established r1 g_LUN1 ASCCd.REGISTRATIONS_PREEMPTED

            // The command is executable.
            // ( The ACA has been cleared. Above SIMPLE 1 task is aborted. )
            let! itt_tur3 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! _ = r1.WaitSCSIResponseGoodStatus itt_tur3

            // Above SIMPLE 2 task is executed.
            let! _ = r3.WaitSCSIResponseGoodStatus itt_tur2

            // The command is executable.
            let! itt_tur4 = r3.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! _ = r3.WaitSCSIResponseGoodStatus itt_tur4

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r3.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey3 ))
            if prtype = PR_TYPE.NO_RESERVATION then
                Assert.False(( fsd1.[0].ReservationHolder ))
                Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
            else
                Assert.True(( fsd1.[0].ReservationHolder ))
                Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue PR_TYPE.WRITE_EXCLUSIVE ))

            do! PR_Unregister r3 g_LUN1 g_ResvKey3
            do! r1.Close()
            do! r3.Close()
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
        }

    // The persistent reservation is not an all registrants type.
    // The faulted I_T nexus performs a preemption to the non-faulted I_T nexus in the ACA task.
    [<Theory>]
    [<MemberData( "A11_B1_C1_001_data" )>]
    member _.A12_B1_C1_001 ( prtype : PR_TYPE ) =
        task {
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a ACA" "Trap added" "MD> "
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r3 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let itn_r1 = r1.ITNexus

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r3 g_LUN1 g_ResvKey3
            if prtype <> PR_TYPE.NO_RESERVATION then
                do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            // send ORDERED 1 task from r1, and wait that task is stuck
            let! itt_read1 = r1.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            do! WaitTaskStucked 1

            // send SIMPLE 1, SIMPLE 2 task from r1, r3
            let! itt_tur1 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! _ = r3.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_read1 ) "Task(" "MD> "
            let! res_read1 = r1.WaitSCSIResponse itt_read1
            Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // PREEMPT AND ABORT (Succeed)
            let! itt_pr_out1 = r1.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.ACA_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1 g_ResvKey3
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // ACA remains
            let! itt_tur3 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! res_tur3 = r1.WaitSCSIResponse itt_tur3
            Assert.True(( res_tur3.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // clear ACA
            do! ClearACA r1 g_LUN1

            // UA is established for r3
            do! Check_UA_Established r3 g_LUN1 ASCCd.REGISTRATIONS_PREEMPTED

            // Above SIMPLE 1 task is executed.
            let! _ = r1.WaitSCSIResponseGoodStatus itt_tur1

            // The command is executable.
            let! itt_tur4 = r3.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! _ = r3.WaitSCSIResponseGoodStatus itt_tur4

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            if prtype = PR_TYPE.NO_RESERVATION then
                Assert.False(( fsd1.[0].ReservationHolder ))
                Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
            else
                Assert.True(( fsd1.[0].ReservationHolder ))
                Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue prtype ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r3.Close()
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
        }

    // The persistent reservation is not an all registrants type.
    // The non-faulted I_T nexus performs a preemption to the non-faulted I_T nexus in the ACA task.
    [<Theory>]
    [<MemberData( "A11_B1_C1_001_data" )>]
    member _.A12_B1_C2_001 ( prtype : PR_TYPE ) =
        task {
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a ACA" "Trap added" "MD> "
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r3 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Register r3 g_LUN1 g_ResvKey3
            if prtype <> PR_TYPE.NO_RESERVATION then
                do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 3 ))

            // send ORDERED 1 task from r1, and wait that task is stuck
            let! itt_read1 = r1.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            do! WaitTaskStucked 1

            // send SIMPLE 1, SIMPLE 2, SIMPLE 3 tasks from r1, r2, r3
            let! itt_tur1 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! itt_tur2 = r2.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! itt_tur3 = r3.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 3 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_read1 ) "Task(" "MD> "
            let! res_read1 = r1.WaitSCSIResponse itt_read1
            Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // PREEMPT AND ABORT ( Terinated with ACA ACTIVE )
            let! itt_pr_out2 = r3.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.ACA_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey3 g_ResvKey2
            let! res_pr_out2 = r3.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // ACA remains
            let! itt_tur4 = r2.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! res_tur4 = r2.WaitSCSIResponse itt_tur4
            Assert.True(( res_tur4.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // clear ACA ( The execution of the SIMPLE 1, SIMPLE 2, SIMPLE 3 tasks will resume. )
            do! ClearACA r1 g_LUN1

            // Receive SIMPLE 1, SIMPLE 2, SIMPLE 3 responses
            let! _ = r1.WaitSCSIResponseGoodStatus itt_tur1
            let! _ = r2.WaitSCSIResponseGoodStatus itt_tur2
            let! _ = r3.WaitSCSIResponseGoodStatus itt_tur3

            // There is no change to the reservation status.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r3 g_LUN1 g_ResvKey3
            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! PR_Unregister r1 g_LUN1 g_ResvKey1

            do! r1.Close()
            do! r2.Close()
            do! r3.Close()
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
        }

    // The persistent reservation is not an all registrants type.
    // The faulted I_T nexus performs a preemption to the non-faulted I_T nexus in the non-ACA task.
    [<Theory>]
    [<MemberData( "A11_B1_C1_001_data" )>]
    member _.A12_B2_C1_001 ( prtype : PR_TYPE ) =
        task {
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a ACA" "Trap added" "MD> "
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            if prtype <> PR_TYPE.NO_RESERVATION then
                do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            // send ORDERED 1 task from r1, and wait that task is stuck
            let! itt_read1 = r1.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            do! WaitTaskStucked 1

            // send SIMPLE 1, SIMPLE 2 tasks from r1, r2
            let! itt_tur1 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! itt_tur2 = r2.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_read1 ) "Task(" "MD> "
            let! res_read1 = r1.WaitSCSIResponse itt_read1
            Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // PREEMPT AND ABORT ( Terinated with ACA ACTIVE )
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1 g_ResvKey2
            let! res_pr_out2 = r1.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // ACA remains
            let! itt_tur4 = r2.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! res_tur4 = r2.WaitSCSIResponse itt_tur4
            Assert.True(( res_tur4.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // clear ACA ( The execution of the SIMPLE 1, SIMPLE 2 tasks will resume. )
            do! ClearACA r1 g_LUN1

            // Receive SIMPLE 1, SIMPLE 2 responses
            let! _ = r1.WaitSCSIResponseGoodStatus itt_tur1
            let! _ = r2.WaitSCSIResponseGoodStatus itt_tur2

            // There is no change to the reservation status.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! PR_Unregister r1 g_LUN1 g_ResvKey1

            do! r1.Close()
            do! r2.Close()
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
        }

    // The persistent reservation is not an all registrants type.
    // The non-faulted I_T nexus performs a preemption to the non-faulted I_T nexus in the non-ACA task.
    [<Theory>]
    [<MemberData( "A11_B1_C1_001_data" )>]
    member _.A12_B2_C2_001 ( prtype : PR_TYPE ) =
        task {
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a ACA" "Trap added" "MD> "
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r3 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Register r3 g_LUN1 g_ResvKey3
            if prtype <> PR_TYPE.NO_RESERVATION then
                do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 3 ))

            // send ORDERED 1 task from r1, and wait that task is stuck
            let! itt_read1 = r1.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            do! WaitTaskStucked 1

            // send SIMPLE 1, SIMPLE 2, SIMPLE 3 tasks from r1, r2, r3
            let! itt_tur1 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! itt_tur2 = r2.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! itt_tur3 = r3.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 3 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_read1 ) "Task(" "MD> "
            let! res_read1 = r1.WaitSCSIResponse itt_read1
            Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // PREEMPT AND ABORT ( Terinated with ACA ACTIVE )
            let! itt_pr_out2 = r2.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey2 g_ResvKey3
            let! res_pr_out2 = r2.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // ACA remains
            let! itt_tur4 = r3.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! res_tur4 = r3.WaitSCSIResponse itt_tur4
            Assert.True(( res_tur4.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // clear ACA ( The execution of the SIMPLE 1, SIMPLE 2 tasks will resume. )
            do! ClearACA r1 g_LUN1

            // Receive SIMPLE 1, SIMPLE 2 responses
            let! _ = r1.WaitSCSIResponseGoodStatus itt_tur1
            let! _ = r2.WaitSCSIResponseGoodStatus itt_tur2
            let! _ = r3.WaitSCSIResponseGoodStatus itt_tur3

            // There is no change to the reservation status.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r3 g_LUN1 g_ResvKey3
            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! PR_Unregister r1 g_LUN1 g_ResvKey1

            do! r1.Close()
            do! r2.Close()
            do! r3.Close()
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
        }

    static member A21_B1_C1_001_data : obj[][] = [|
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;  |]
    |]

    // The persistent reservation is an all registrants type
    // The faulted I_T nexus performs a preemption in the ACA task and SERVICE ACTION RESERVATION KEY=0.
    [<Theory>]
    [<MemberData( "A21_B1_C1_001_data" )>]
    member _.A21_B1_C1_001 ( prtype : PR_TYPE ) =
        task {
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a ACA" "Trap added" "MD> "
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let itn_r1 = r1.ITNexus

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            // send ORDERED 1 task from r1, and wait that task is stuck
            let! itt_read1 = r1.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            do! WaitTaskStucked 1

            // send SIMPLE 1, SIMPLE 2 task from r1, r2
            let! _ = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! _ = r2.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_read1 ) "Task(" "MD> "
            let! res_read1 = r1.WaitSCSIResponse itt_read1
            Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // PREEMPT AND ABORT (Succeed)
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.ACA_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1 resvkey_me.zero
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // UA is established for r2
            do! Check_UA_Established r2 g_LUN1 ASCCd.REGISTRATIONS_PREEMPTED

            // The command is executable.
            let! itt_tur3 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! _ = r1.WaitSCSIResponseGoodStatus itt_tur3
            let! itt_tur4 = r2.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! _ = r2.WaitSCSIResponseGoodStatus itt_tur4

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].ReservationHolder ))
            Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue PR_TYPE.WRITE_EXCLUSIVE ))
            do! PR_Unregister r1 g_LUN1 g_ResvKey1

            do! r1.Close()
            do! r2.Close()
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
        }

    // The persistent reservation is an all registrants type
    // The non-faulted I_T nexus performs a preemption in the ACA task and SERVICE ACTION RESERVATION KEY=0.
    [<Theory>]
    [<MemberData( "A21_B1_C1_001_data" )>]
    member _.A21_B1_C2_001 ( prtype : PR_TYPE ) =
        task {
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a ACA" "Trap added" "MD> "
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            if prtype <> PR_TYPE.NO_RESERVATION then
                do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            // send ORDERED 1 task from r1, and wait that task is stuck
            let! itt_read1 = r1.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            do! WaitTaskStucked 1

            // send SIMPLE 1, SIMPLE 2 tasks from r1, r2
            let! itt_tur1 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! itt_tur2 = r2.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_read1 ) "Task(" "MD> "
            let! res_read1 = r1.WaitSCSIResponse itt_read1
            Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // PREEMPT AND ABORT ( Terinated with ACA ACTIVE )
            let! itt_pr_out2 = r2.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.ACA_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1 resvkey_me.zero
            let! res_pr_out2 = r2.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // ACA remains
            let! itt_tur4 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! res_tur4 = r1.WaitSCSIResponse itt_tur4
            Assert.True(( res_tur4.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // clear ACA ( The execution of the SIMPLE 1, SIMPLE 2 tasks will resume. )
            do! ClearACA r1 g_LUN1

            // Receive SIMPLE 1, SIMPLE 2 responses
            let! _ = r1.WaitSCSIResponseGoodStatus itt_tur1
            let! _ = r2.WaitSCSIResponseGoodStatus itt_tur2

            // There is no change to the reservation status.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! PR_Unregister r1 g_LUN1 g_ResvKey1

            do! r1.Close()
            do! r2.Close()
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
        }

    // The persistent reservation is an all registrants type
    // The faulted I_T nexus performs a preemption in the non-ACA task and SERVICE ACTION RESERVATION KEY=0.
    [<Theory>]
    [<MemberData( "A21_B1_C1_001_data" )>]
    member _.A21_B2_C1_001 ( prtype : PR_TYPE ) =
        task {
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Read /slba 10 /elba 20 /a ACA" "Trap added" "MD> "
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            if prtype <> PR_TYPE.NO_RESERVATION then
                do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            // send ORDERED 1 task from r1, and wait that task is stuck
            let! itt_read1 = r1.Send_Read10 TaskATTRCd.ORDERED_TASK g_LUN1 ( blkcnt_me.ofUInt32 10u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            do! WaitTaskStucked 1

            // send SIMPLE 1, SIMPLE 2 tasks from r1, r2
            let! itt_tur1 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! itt_tur2 = r2.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T

            // Wait until above task is queued.
            do! Task.Delay 5
            while ( GetDormantTaskCount() < 2 ) do
                do! Task.Delay 5

            // Resume execution of ORDERED 1 task. ACA status is established.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_read1 ) "Task(" "MD> "
            let! res_read1 = r1.WaitSCSIResponse itt_read1
            Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // PREEMPT AND ABORT ( Terinated with ACA ACTIVE )
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1 resvkey_me.zero
            let! res_pr_out2 = r1.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // ACA remains
            let! itt_tur4 = r2.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! res_tur4 = r2.WaitSCSIResponse itt_tur4
            Assert.True(( res_tur4.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // clear ACA ( The execution of the SIMPLE 1, SIMPLE 2 tasks will resume. )
            do! ClearACA r1 g_LUN1

            // Receive SIMPLE 1, SIMPLE 2 responses
            let! _ = r1.WaitSCSIResponseGoodStatus itt_tur1
            let! _ = r2.WaitSCSIResponseGoodStatus itt_tur2

            // There is no change to the reservation status.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! PR_Unregister r1 g_LUN1 g_ResvKey1

            do! r1.Close()
            do! r2.Close()
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "
        }
