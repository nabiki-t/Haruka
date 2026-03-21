//=============================================================================
// Haruka Software Storage.
// PersistentReserveOut4.fs : Test cases for PERSISTENT RESERVE OUT command.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.IT.SCSI

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading.Tasks

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

//=============================================================================
// Class implementation

[<CollectionDefinition( "SCSI_PersistentReserveOut4" )>]
type SCSI_PersistentReserveOut4_Fixture() =

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

    interface ICollectionFixture<SCSI_PersistentReserveOut4_Fixture>

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


[<Collection( "SCSI_PersistentReserveOut4" )>]
type SCSI_PersistentReserveOut4( fx : SCSI_PersistentReserveOut4_Fixture ) =

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
        Array.init 2 ( fun _ -> GlbFunc.newISID() )
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

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    // A PERSISTENT RESERVE OUT command with CLEAR service action is received from an unregistered I_T nexus.
    // It will terminated with RESERVATION_CONFLICT status.
    [<Fact>]
    member _.Clear_FromUnregistered_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            let! itt_pr_out1 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero
            let! res_pr_out1 = r1.WaitSCSIResponse itt_pr_out1
            Assert.True(( res_pr_out1.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg1 = prg2 ))
            do! r1.Close()
        }

    // A PERSISTENT RESERVE OUT command with CLEAR service action is received from an registered I_T nexus.
    [<Fact>]
    member _.Clear_FromRegistered_LastOne_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1
            do! PR_Register r1 g_LUN1 g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // clear
            let! itt_pr_out2 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat1.PersistentReservationsGeneration ))
            do! r1.Close()
        }

    // A PERSISTENT RESERVE OUT command with CLEAR service action that has invalid registration key is received from an registered I_T nexus.
    // It will terminated with RESERVATION_CONFLICT status.
    [<Fact>]
    member _.Clear_FromRegistered_RegKeyMismatch_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1
            do! PR_Register r1 g_LUN1 g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // clear
            let! itt_pr_out2 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2
            let! res_pr_out2 = r1.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat2.PersistentReservationsGeneration ))
            do! r1.Close()
        }

    static member Clear_FromRegistered_001_data : obj[][] = [|
        [| PR_TYPE.NO_RESERVATION;                    |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |]
    |]

    // A PERSISTENT RESERVE OUT command with CLEAR service action is received from an registered I_T nexus, while other registrations exist.
    [<Theory>]
    [<MemberData( "Clear_FromRegistered_001_data" )>]
    member _.Clear_FromRegistered_001 ( prtype : PR_TYPE ) =
        task {
            let isids = GetSortedISID 2
            let spr1 = { m_defaultSessParam with ISID = isids.[0] }
            let spr2 = { m_defaultSessParam with ISID = isids.[0]; TargetName = "iqn.2020-05.example.com:target2" }
            let spr3 = { m_defaultSessParam with ISID = isids.[1] }
            let spr4 = { m_defaultSessParam with ISID = isids.[1]; TargetName = "iqn.2020-05.example.com:target2" }
            let! r1 = SCSI_Initiator.CreateWithISID spr1 m_defaultConnParam
            let! r2 = SCSI_Initiator.CreateWithISID spr2 m_defaultConnParam
            let! r3 = SCSI_Initiator.CreateWithISID spr3 m_defaultConnParam
            let! r4 = SCSI_Initiator.CreateWithISID spr4 m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1, r2, r3, r4
            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Register r3 g_LUN1 g_ResvKey3
            do! PR_Register r4 g_LUN1 g_ResvKey4

            // reserve
            if prtype <> PR_TYPE.NO_RESERVATION then
                let! itt_pr_out05 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype g_ResvKey1
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out05
                ()

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 4 ))

            // clear
            let! itt_pr_out06 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out06

            // check UA for initiator r1, r2, r3, r4
            // Normally, a UA is not established for r1. 
            // However, since r1 and r2 share the same initiator, a UA is established for r2's initiator.
            do! Check_UA_Established r1 g_LUN1 ASCCd.RESERVATIONS_PREEMPTED
            do! Check_UA_Cleared r2 g_LUN1
            do! Check_UA_Established r3 g_LUN1 ASCCd.RESERVATIONS_PREEMPTED
            do! Check_UA_Cleared r4 g_LUN1

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat1.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r2.Close()
            do! r3.Close()
            do! r4.Close()
        }

    // A PERSISTENT RESERVE OUT command with CLEAR service action is received from an registered I_T nexus, while other registrations exist.
    [<Theory>]
    [<MemberData( "Clear_FromRegistered_001_data" )>]
    member _.Clear_FromRegistered_002 ( prtype : PR_TYPE ) =
        task {
            let isids = GetSortedISID 2
            let spr1 = { m_defaultSessParam with ISID = isids.[0] }
            let spr3 = { m_defaultSessParam with ISID = isids.[1] }
            let spr4 = { m_defaultSessParam with ISID = isids.[1]; TargetName = "iqn.2020-05.example.com:target2" }
            let! r1 = SCSI_Initiator.CreateWithISID spr1 m_defaultConnParam
            let! r3 = SCSI_Initiator.CreateWithISID spr3 m_defaultConnParam
            let! r4 = SCSI_Initiator.CreateWithISID spr4 m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1, r3, r4
            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r3 g_LUN1 g_ResvKey3
            do! PR_Register r4 g_LUN1 g_ResvKey4

            // reserve
            if prtype <> PR_TYPE.NO_RESERVATION then
                let! itt_pr_out05 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype g_ResvKey1
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out05
                ()

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 3 ))

            // clear
            let! itt_pr_out06 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out06

            // Check UA
            do! Check_UA_Cleared r1 g_LUN1
            do! Check_UA_Established r4 g_LUN1 ASCCd.RESERVATIONS_PREEMPTED
            do! Check_UA_Cleared r3 g_LUN1

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat1.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r3.Close()
            do! r4.Close()
        }

    [<Fact>]
    member _.Clear_InvalidScope_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1
            do! PR_Register r1 g_LUN1 g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // clear ( SCOPE <> 0 )
            let param : Haruka.BlockDeviceLU.BasicParameterList = {
                ReservationKey = g_ResvKey1;
                ServiceActionReservationKey = resvkey_me.zero;
                SPEC_I_PT = false;
                ALL_TG_PT = false;
                APTPL = false;
                TransportID = [||];
            }
            let! itt_pr_out3 = r1.Send_PersistentReserveOut_BasicParam TaskATTRCd.SIMPLE_TASK g_LUN1 3uy 1uy PR_TYPE.WRITE_EXCLUSIVE param NACA.T
            let! res_pr_out3 = r1.WaitSCSIResponse itt_pr_out3
            Assert.True(( res_pr_out3.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // clear ACA
            let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            Assert.True(( fstat2.FullStatusDescriptor.Length = 1 ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }

    [<Fact>]
    member _.Clear_APTPL_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let prfname = GetPRFileName g_LUN1

            if File.Exists prfname then
                // register ( APTPL=0 )
                let! itt_pr_out1 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.F [||]
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

                // wait for delete PR file
                GlbFunc.WaitForFileDelete prfname
            Assert.False(( File.Exists prfname ))

            // register ( APTPL=1 )
            let! itt_pr_out2 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // wait for create PR file
            GlbFunc.WaitForFileCreate prfname
            let fdate = GlbFunc.GetFileHash prfname

            // clear ( APTPL=0, ignored )
            let param : Haruka.BlockDeviceLU.BasicParameterList = {
                ReservationKey = g_ResvKey1;
                ServiceActionReservationKey = resvkey_me.zero;
                SPEC_I_PT = false;
                ALL_TG_PT = false;
                APTPL = false;
                TransportID = [||];
            }
            let! itt_pr_out3 = r1.Send_PersistentReserveOut_BasicParam TaskATTRCd.SIMPLE_TASK g_LUN1 3uy 0uy PR_TYPE.WRITE_EXCLUSIVE param NACA.T
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            // wait for update PR file
            GlbFunc.WaitForFileUpdateByHash prfname fdate

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }

    // A PERSISTENT RESERVE OUT command with PREEMPT service action is received from an unregistered I_T nexus.
    // It will terminated with RESERVATION_CONFLICT status.
    [<Fact>]
    member _.Preempt_FromUnregistered_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            let! itt_pr_out1 = r1.Send_PROut_PREEMPT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS resvkey_me.zero resvkey_me.zero
            let! res_pr_out1 = r1.WaitSCSIResponse itt_pr_out1
            Assert.True(( res_pr_out1.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg1 = prg2 ))
            do! r1.Close()
        }

    // A PERSISTENT RESERVE OUT command with PREEMPT service action that has invalid registration key is received from an registered I_T nexus.
    // It will terminated with RESERVATION_CONFLICT status.
    [<Fact>]
    member _.Preempt_FromRegistered_RegKeyMismatch_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1
            do! PR_Register r1 g_LUN1 g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // preempt
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey2 resvkey_me.zero
            let! res_pr_out2 = r1.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat2.PersistentReservationsGeneration ))
            do! r1.Close()
        }

    // A PERSISTENT RESERVE OUT command with PREEMPT service action is received from an registered I_T nexus while there is no reservation.
    // The reservation key specified in service action reservation key will be deleted.
    [<Fact>]
    member _.Preempt_FromRegistered_NoReservation_001 () =
        task {
            let isids = GetSortedISID 2
            let spr1 = { m_defaultSessParam with ISID = isids.[0] }
            let spr2 = { m_defaultSessParam with ISID = isids.[0]; TargetName = "iqn.2020-05.example.com:target2" }
            let spr3 = { m_defaultSessParam with ISID = isids.[1] }
            let spr4 = { m_defaultSessParam with ISID = isids.[1]; TargetName = "iqn.2020-05.example.com:target2" }
            let! r1 = SCSI_Initiator.CreateWithISID spr1 m_defaultConnParam
            let! r2 = SCSI_Initiator.CreateWithISID spr2 m_defaultConnParam
            let! r3 = SCSI_Initiator.CreateWithISID spr3 m_defaultConnParam
            let! r4 = SCSI_Initiator.CreateWithISID spr4 m_defaultConnParam
            let itn_r1 = r1.ITNexus
            let itn_r4 = r4.ITNexus
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Register r3 g_LUN1 g_ResvKey2
            do! PR_Register r4 g_LUN1 g_ResvKey3

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 4 ))

            // preempt ( TYPE is ignored, unregister r2 r3 )
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey2
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // check UA for initiator r1, r2, r3, r4
            // Normally, a UA is not established for r1. 
            // However, since r1 and r2 share the same initiator, a UA is established for r2's initiator.
            do! Check_UA_Established r1 g_LUN1 ASCCd.REGISTRATIONS_PREEMPTED
            do! Check_UA_Cleared r2 g_LUN1
            do! Check_UA_Established r3 g_LUN1 ASCCd.REGISTRATIONS_PREEMPTED
            do! Check_UA_Cleared r4 g_LUN1

            // The registrations for r2 and r3 have been removed.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor |> Array.sortBy _.iSCSIName
            Assert.True(( fsd2.Length = 2 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.False(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

            Assert.True(( fsd2.[1].iSCSIName = itn_r4.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey3 ))
            Assert.True(( fsd2.[1].RelativeTargetPortIdentifier = 2us ))
            Assert.False(( fsd2.[1].ReservationHolder ))
            Assert.True(( fsd2.[1].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! PR_Unregister r4 g_LUN1 g_ResvKey3
            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat2.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r2.Close()
            do! r3.Close()
            do! r4.Close()
        }
        
    // A PERSISTENT RESERVE OUT command with PREEMPT service action is received from an registered I_T nexus while there is no reservation.
    // The reservation key specified in service action reservation key will be deleted.
    [<Fact>]
    member _.Preempt_FromRegistered_NoReservation_002 () =
        task {
            let isids = GetSortedISID 2
            let spr1 = { m_defaultSessParam with ISID = isids.[0] }
            let spr3 = { m_defaultSessParam with ISID = isids.[1] }
            let spr4 = { m_defaultSessParam with ISID = isids.[1]; TargetName = "iqn.2020-05.example.com:target2" }
            let! r1 = SCSI_Initiator.CreateWithISID spr1 m_defaultConnParam
            let! r3 = SCSI_Initiator.CreateWithISID spr3 m_defaultConnParam
            let! r4 = SCSI_Initiator.CreateWithISID spr4 m_defaultConnParam
            let itn_r1 = r1.ITNexus
            let itn_r4 = r4.ITNexus
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r3 g_LUN1 g_ResvKey2
            do! PR_Register r4 g_LUN1 g_ResvKey3

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 3 ))

            // preempt ( TYPE is ignored, unregister r3 )
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey2
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // check UA for initiator r1, r3, r4
            do! Check_UA_Cleared r1 g_LUN1
            do! Check_UA_Established r4 g_LUN1 ASCCd.REGISTRATIONS_PREEMPTED
            do! Check_UA_Cleared r3 g_LUN1

            // The registrations for r3 have been removed.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor |> Array.sortBy _.iSCSIName
            Assert.True(( fsd2.Length = 2 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.False(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

            Assert.True(( fsd2.[1].iSCSIName = itn_r4.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey3 ))
            Assert.True(( fsd2.[1].RelativeTargetPortIdentifier = 2us ))
            Assert.False(( fsd2.[1].ReservationHolder ))
            Assert.True(( fsd2.[1].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! PR_Unregister r4 g_LUN1 g_ResvKey3
            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat2.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r3.Close()
            do! r4.Close()
        }

    // A PERSISTENT RESERVE OUT command with PREEMPT service action is received from an registered I_T nexus while there is no reservation.
    // If the reservation key specified in the service action reservation key does not exist, the command will terminate with RESERVATION CONFLICT.
    [<Fact>]
    member _.Preempt_FromRegistered_NoReservation_003 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1
            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            // preempt ( TYPE is ignored )
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey3
            let! res_pr_out2 = r1.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2 = fstat1 ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
            do! r2.Close()
        }

    static member Preempt_FromRegistered_AllRegistrants_001_data : obj[][] = [|
//        [| PR_TYPE.WRITE_EXCLUSIVE;                   |]
//        [| PR_TYPE.EXCLUSIVE_ACCESS;                  |]
//        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |]
//        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |]
    |]

    // A PERSISTENT RESERVE OUT command with PREEMPT service action is received from an registered I_T nexus while there is no reservation.
    // ALL_REGISTRANTS type, service action reservation key = 0.
    // All existing registrations will be unregistered, and new reservations will be established.
    [<Theory>]
    [<MemberData( "Preempt_FromRegistered_AllRegistrants_001_data" )>]
    member _.Preempt_FromRegistered_AllRegistrants_001 ( prtype : PR_TYPE ) =
        task {
            let isids = GetSortedISID 2
            let spr1 = { m_defaultSessParam with ISID = isids.[0] }
            let spr2 = { m_defaultSessParam with ISID = isids.[0]; TargetName = "iqn.2020-05.example.com:target2" }
            let spr3 = { m_defaultSessParam with ISID = isids.[1] }
            let spr4 = { m_defaultSessParam with ISID = isids.[1]; TargetName = "iqn.2020-05.example.com:target2" }
            let! r1 = SCSI_Initiator.CreateWithISID spr1 m_defaultConnParam
            let! r2 = SCSI_Initiator.CreateWithISID spr2 m_defaultConnParam
            let! r3 = SCSI_Initiator.CreateWithISID spr3 m_defaultConnParam
            let! r4 = SCSI_Initiator.CreateWithISID spr4 m_defaultConnParam
            let itn_r1 = r1.ITNexus
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Register r3 g_LUN1 g_ResvKey3
            do! PR_Register r4 g_LUN1 g_ResvKey4

            // reserve
            let! itt_pr_out1 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 4 ))
            for itr in fsd1 do
                Assert.True(( itr.ReservationHolder ))
                Assert.True(( itr.Type = PR_TYPE.toNumericValue prtype ))

            // preempt ( unregister r2, r3, r4 )
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1 resvkey_me.zero
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // check UA for initiator r1, r2, r3, r4
            // Normally, a UA is not established for r1. 
            // However, since r1 and r2 share the same initiator, a UA is established for r2's initiator.
            do! Check_UA_Established r1 g_LUN1 ASCCd.REGISTRATIONS_PREEMPTED
            do! Check_UA_Cleared r2 g_LUN1
            do! Check_UA_Established r3 g_LUN1 ASCCd.REGISTRATIONS_PREEMPTED
            do! Check_UA_Cleared r4 g_LUN1

            // The registrations for r2 r3 r4 have been removed.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor |> Array.sortBy _.iSCSIName
            Assert.True(( fsd2.Length = 1 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue PR_TYPE.WRITE_EXCLUSIVE ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat2.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r2.Close()
            do! r3.Close()
            do! r4.Close()
        }

    // A PERSISTENT RESERVE OUT command with PREEMPT service action is received from an registered I_T nexus while there is no reservation.
    // ALL_REGISTRANTS type, service action reservation key = 0.
    // All existing registrations will be unregistered, and new reservations will be established.
    [<Theory>]
    [<MemberData( "Preempt_FromRegistered_AllRegistrants_001_data" )>]
    member _.Preempt_FromRegistered_AllRegistrants_002 ( prtype : PR_TYPE ) =
        task {
            let isids = GetSortedISID 2
            let spr1 = { m_defaultSessParam with ISID = isids.[0] }
            let spr3 = { m_defaultSessParam with ISID = isids.[1] }
            let spr4 = { m_defaultSessParam with ISID = isids.[1]; TargetName = "iqn.2020-05.example.com:target2" }
            let! r1 = SCSI_Initiator.CreateWithISID spr1 m_defaultConnParam
            let! r3 = SCSI_Initiator.CreateWithISID spr3 m_defaultConnParam
            let! r4 = SCSI_Initiator.CreateWithISID spr4 m_defaultConnParam
            let itn_r1 = r1.ITNexus
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r3 g_LUN1 g_ResvKey3
            do! PR_Register r4 g_LUN1 g_ResvKey4

            // reserve
            let! itt_pr_out1 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 3 ))
            for itr in fsd1 do
                Assert.True(( itr.ReservationHolder ))
                Assert.True(( itr.Type = PR_TYPE.toNumericValue prtype ))

            // preempt ( unregister r2, r3, r4 )
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1 resvkey_me.zero
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // check UA for initiator r1, r3, r4
            do! Check_UA_Cleared r1 g_LUN1
            do! Check_UA_Established r3 g_LUN1 ASCCd.REGISTRATIONS_PREEMPTED
            do! Check_UA_Cleared r4 g_LUN1

            // The registrations for r3 r4 have been removed.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor |> Array.sortBy _.iSCSIName
            Assert.True(( fsd2.Length = 1 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue PR_TYPE.WRITE_EXCLUSIVE ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat2.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r3.Close()
            do! r4.Close()
        }

    // A PERSISTENT RESERVE OUT command with PREEMPT service action is received from an registered I_T nexus while there is no reservation.
    // ALL_REGISTRANTS type, service action reservation key <> 0.
    // The reservation key specified in service action reservation key will be deleted.
    [<Theory>]
    [<MemberData( "Preempt_FromRegistered_AllRegistrants_001_data" )>]
    member _.Preempt_FromRegistered_AllRegistrants_003 ( prtype : PR_TYPE ) =
        task {
            let isids = GetSortedISID 2
            let spr1 = { m_defaultSessParam with ISID = isids.[0] }
            let spr2 = { m_defaultSessParam with ISID = isids.[0]; TargetName = "iqn.2020-05.example.com:target2" }
            let spr3 = { m_defaultSessParam with ISID = isids.[1] }
            let spr4 = { m_defaultSessParam with ISID = isids.[1]; TargetName = "iqn.2020-05.example.com:target2" }
            let! r1 = SCSI_Initiator.CreateWithISID spr1 m_defaultConnParam
            let! r2 = SCSI_Initiator.CreateWithISID spr2 m_defaultConnParam
            let! r3 = SCSI_Initiator.CreateWithISID spr3 m_defaultConnParam
            let! r4 = SCSI_Initiator.CreateWithISID spr4 m_defaultConnParam
            let itn_r1 = r1.ITNexus
            let itn_r4 = r4.ITNexus
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Register r3 g_LUN1 g_ResvKey2
            do! PR_Register r4 g_LUN1 g_ResvKey3

            // reserve
            let! itt_pr_out1 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 4 ))
            for itr in fsd1 do
                Assert.True(( itr.ReservationHolder ))
                Assert.True(( itr.Type = PR_TYPE.toNumericValue prtype ))

            // preempt ( TYPE is ignored, unregister r2 r3 )
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1 g_ResvKey2
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // check UA for initiator r1, r2, r3, r4
            // Normally, a UA is not established for r1. 
            // However, since r1 and r2 share the same initiator, a UA is established for r2's initiator.
            do! Check_UA_Established r1 g_LUN1 ASCCd.REGISTRATIONS_PREEMPTED
            do! Check_UA_Cleared r2 g_LUN1
            do! Check_UA_Established r3 g_LUN1 ASCCd.REGISTRATIONS_PREEMPTED
            do! Check_UA_Cleared r4 g_LUN1

            // The registrations for r2 r3 have been removed.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor |> Array.sortBy _.iSCSIName
            Assert.True(( fsd2.Length = 2 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue prtype ))

            Assert.True(( fsd2.[1].iSCSIName = itn_r4.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey3 ))
            Assert.True(( fsd2.[1].RelativeTargetPortIdentifier = 2us ))
            Assert.True(( fsd2.[1].ReservationHolder ))
            Assert.True(( fsd2.[1].Type = PR_TYPE.toNumericValue prtype ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! PR_Unregister r4 g_LUN1 g_ResvKey3
            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat2.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r2.Close()
            do! r3.Close()
            do! r4.Close()
        }

    // A PERSISTENT RESERVE OUT command with PREEMPT service action is received from an registered I_T nexus while there is no reservation.
    // ALL_REGISTRANTS type, service action reservation key <> 0.
    // The reservation key specified in service action reservation key will be deleted.
    [<Theory>]
    [<MemberData( "Preempt_FromRegistered_AllRegistrants_001_data" )>]
    member _.Preempt_FromRegistered_AllRegistrants_004 ( prtype : PR_TYPE ) =
        task {
            let isids = GetSortedISID 2
            let spr1 = { m_defaultSessParam with ISID = isids.[0] }
            let spr3 = { m_defaultSessParam with ISID = isids.[1] }
            let spr4 = { m_defaultSessParam with ISID = isids.[1]; TargetName = "iqn.2020-05.example.com:target2" }
            let! r1 = SCSI_Initiator.CreateWithISID spr1 m_defaultConnParam
            let! r3 = SCSI_Initiator.CreateWithISID spr3 m_defaultConnParam
            let! r4 = SCSI_Initiator.CreateWithISID spr4 m_defaultConnParam
            let itn_r1 = r1.ITNexus
            let itn_r4 = r4.ITNexus
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r3 g_LUN1 g_ResvKey2
            do! PR_Register r4 g_LUN1 g_ResvKey3

            // reserve
            let! itt_pr_out1 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 3 ))
            for itr in fsd1 do
                Assert.True(( itr.ReservationHolder ))
                Assert.True(( itr.Type = PR_TYPE.toNumericValue prtype ))

            // preempt ( TYPE is ignored, unregister r3 )
            let! itt_pr_out2 = r1.Send_PROut_PREEMPT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey2
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // check UA for initiator r1, r3, r4
            do! Check_UA_Cleared r1 g_LUN1
            do! Check_UA_Established r3 g_LUN1 ASCCd.REGISTRATIONS_PREEMPTED
            do! Check_UA_Cleared r4 g_LUN1

            // The registrations for r3 have been removed.
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor |> Array.sortBy _.iSCSIName
            Assert.True(( fsd2.Length = 2 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue prtype ))

            Assert.True(( fsd2.[1].iSCSIName = itn_r4.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey3 ))
            Assert.True(( fsd2.[1].RelativeTargetPortIdentifier = 2us ))
            Assert.True(( fsd2.[1].ReservationHolder ))
            Assert.True(( fsd2.[1].Type = PR_TYPE.toNumericValue prtype ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! PR_Unregister r4 g_LUN1 g_ResvKey3
            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat2.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r3.Close()
            do! r4.Close()
        }
