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

    // Get the file name for the Persistent Reservation.
    let GetPRFileName ( lun : LUN_T ) : string =
        let c = Path.DirectorySeparatorChar
        let tdid = tdid_me.fromPrim 99u
        sprintf "%s%c%s%c%s%c%s" m_WorkPath c ( tdid_me.toString tdid ) c ( lun_me.WorkDirName lun ) c Constants.PR_SAVE_FILE_NAME

    let GetITNexus ( r : SCSI_Initiator ) =
        let sp = r.SessionParams
        // TPGT is specified in the configuration and is always 0.
        ITNexus( sp.InitiatorName, sp.ISID, sp.TargetName, tpgt_me.zero )

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

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    // A PERSISTENT RESERVE OUT command with CLEAR service action is received from an unregistered I_T nexus.
    // It will terminated with RESERVATION_CONFLICT status.
    [<Fact>]
    member _.Reserve_FromUnregistered_001 () =
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
    member _.Reserve_FromRegistered_LastOne_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

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
    member _.Reserve_FromRegistered_RegKeyMismatch_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // clear
            let! itt_pr_out2 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2
            let! res_pr_out2 = r1.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            // unregister
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat2.PersistentReservationsGeneration ))
            do! r1.Close()
        }

    // A PERSISTENT RESERVE OUT command with CLEAR service action is received from an registered I_T nexus, while other registrations exist.
    [<Fact>]
    member _.Reserve_FromRegistered_NoReservation_001 () =
        task {
            let isids =
                Array.init 2 ( fun _ -> GlbFunc.newISID() )
                |> Array.sortBy isid_me.toPrim
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
            let! itt_pr_out01 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out01
            let! itt_pr_out02 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out02
            let! itt_pr_out03 = r3.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey3 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r3.WaitSCSIResponseGoodStatus itt_pr_out03
            let! itt_pr_out04 = r4.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey4 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r4.WaitSCSIResponseGoodStatus itt_pr_out04

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 4 ))

            // clear
            let! itt_pr_out05 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out05

            // check UA for initiator r1, r2
            // Normally, a UA is not established for r1. 
            // However, since r1 and r2 share the same initiator, a UA is established for r2's initiator.
            let! itt_read1 = r1.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! res_read1 = r1.WaitSCSIResponse itt_read1
            Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res_read1.Sense.Value.SenseKey = SenseKeyCd.UNIT_ATTENTION ))
            Assert.True(( res_read1.Sense.Value.ASC = ASCCd.RESERVATIONS_PREEMPTED ))

            let! itt_read2 = r2.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! res_read2 = r2.WaitSCSIResponseGoodStatus itt_read2
            res_read2.Return()

            // check UA for initiator r3, r4
            let! itt_read3 = r3.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! res_read3 = r3.WaitSCSIResponse itt_read3
            Assert.True(( res_read3.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res_read3.Sense.Value.SenseKey = SenseKeyCd.UNIT_ATTENTION ))
            Assert.True(( res_read3.Sense.Value.ASC = ASCCd.RESERVATIONS_PREEMPTED ))

            let! itt_read4 = r4.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! res_read4 = r4.WaitSCSIResponseGoodStatus itt_read4
            res_read4.Return()

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat1.PersistentReservationsGeneration ))
            do! r1.Close()
        }

    // A PERSISTENT RESERVE OUT command with CLEAR service action is received from an registered I_T nexus, while other registrations exist.
    [<Fact>]
    member _.Reserve_FromRegistered_NoReservation_002 () =
        task {
            let isids =
                Array.init 2 ( fun _ -> GlbFunc.newISID() )
                |> Array.sortBy isid_me.toPrim
            let spr1 = { m_defaultSessParam with ISID = isids.[0] }
            let spr3 = { m_defaultSessParam with ISID = isids.[1] }
            let spr4 = { m_defaultSessParam with ISID = isids.[1]; TargetName = "iqn.2020-05.example.com:target2" }
            let! r1 = SCSI_Initiator.CreateWithISID spr1 m_defaultConnParam
            let! r3 = SCSI_Initiator.CreateWithISID spr3 m_defaultConnParam
            let! r4 = SCSI_Initiator.CreateWithISID spr4 m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1, r3, r4
            let! itt_pr_out01 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out01
            let! itt_pr_out03 = r3.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey3 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r3.WaitSCSIResponseGoodStatus itt_pr_out03
            let! itt_pr_out04 = r4.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey4 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r4.WaitSCSIResponseGoodStatus itt_pr_out04

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 3 ))

            // clear
            let! itt_pr_out05 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out05

            // UA is not established for r1
            let! itt_read2 = r1.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! res_read2 = r1.WaitSCSIResponseGoodStatus itt_read2
            res_read2.Return()

            // check UA for initiator r3, r4
            let! itt_read3 = r4.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! res_read3 = r4.WaitSCSIResponse itt_read3
            Assert.True(( res_read3.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res_read3.Sense.Value.SenseKey = SenseKeyCd.UNIT_ATTENTION ))
            Assert.True(( res_read3.Sense.Value.ASC = ASCCd.RESERVATIONS_PREEMPTED ))

            let! itt_read4 = r3.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! res_read4 = r3.WaitSCSIResponseGoodStatus itt_read4
            res_read4.Return()

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat1.PersistentReservationsGeneration ))
            do! r1.Close()
        }

