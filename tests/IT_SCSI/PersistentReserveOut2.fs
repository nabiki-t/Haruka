//=============================================================================
// Haruka Software Storage.
// PersistentReserveOut2.fs : Test cases for PERSISTENT RESERVE OUT command.
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

[<CollectionDefinition( "SCSI_PersistentReserveOut2" )>]
type SCSI_PersistentReserveOut2_Fixture() =

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

    interface ICollectionFixture<SCSI_PersistentReserveOut2_Fixture>

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


[<Collection( "SCSI_PersistentReserveOut2" )>]
type SCSI_PersistentReserveOut2( fx : SCSI_PersistentReserveOut2_Fixture ) =

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

    // RESERVATION KEY=0 and SERVICE ACTION RESERVATION KEY=0 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an unregistered I_T nexus.
    // It will return GOOD without doing anything and terminate.
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromUnregistered_NothingToDo_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // REGISTER, with RESERVATION KEY=0 and SERVICE ACTION RESERVATION KEY=0
            let! itt_pr_out1 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg1 = prg2 ))
            do! r1.Close()
        }

    // RESERVATION KEY=0 and SERVICE ACTION RESERVATION KEY<>0 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an unregistered I_T nexus
    // It register the I_T nexus with the value specified in SERVICE ACTION RESERVATION KEY.
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromUnregistered_Register_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // REGISTER_AND_IGNORE_EXISTING_KEY, with SERVICE ACTION RESERVATION KEY = 1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat2.FullStatusDescriptor.Length = 1 ))
            Assert.True(( fstat2.FullStatusDescriptor.[0].ReservationKey = g_ResvKey1 ))

            // unregister
            let! itt_pr_out2 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }

    // and SERVICE ACTION RESERVATION KEY<>0 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an unregistered I_T nexus
    // SPEC_I_PT = 0, but TransportID is not empty. 
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromUnregistered_Register_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1, SPEC_I_PT=0, TransportID is not empty.
            let transid = [| ( "aaaaa", None ) |]
            let! itt_pr_out2 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T transid
            let! res_pr_out2 = r1.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // clear ACA
            let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 = prg1 ))
            do! r1.Close()
        }

    // SERVICE ACTION RESERVATION KEY<>0 and SPEC_I_PT=1 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an unregistered I_T nexus.
    // It terminate with CHECK CONDITION.
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromUnregistered_SPEC_I_PT_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // REGISTER_AND_IGNORE_EXISTING_KEY, with SERVICE ACTION RESERVATION KEY = 1, SPEC_I_PT = 1
            let transid = [| ( "aaaaa", Some r1.SessionParams.ISID ) |]
            let! itt_pr_out1 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 SPEC_I_PT.T ALL_TG_PT.F APTPL.T transid
            let! res_pr_out1 = r1.WaitSCSIResponse itt_pr_out1
            Assert.True(( res_pr_out1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // clear ACA
            let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 = prg1 ))
            do! r1.Close()
        }

    // SERVICE ACTION RESERVATION KEY<>0 and ALL_TG_PT=1 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an unregistered I_T nexus.
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromUnregistered_ALL_TG_PT_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let itn_r1 = GetITNexus r1
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1 for all target port
            let! itt_pr_out1 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.T APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > prg1 ))
            let fsd2 = fstat2.FullStatusDescriptor
            Assert.True(( fsd2.Length = 2 ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2 |> Array.exists ( fun itr -> itr.RelativeTargetPortIdentifier = 1us ) ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[1].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2 |> Array.exists ( fun itr -> itr.RelativeTargetPortIdentifier = 2us ) ))

            // unregister r1 ( target 1 )
            let! itt_pr_out2 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let r2_sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2";
                    ISID = r1.SessionParams.ISID;
            }
            let! r2 = SCSI_Initiator.CreateWithISID r2_sessParam m_defaultConnParam

            // unregister r2 ( target 2 )
            let! itt_pr_out3 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out3

            let! fstat5 = PR_ReadFullStatus r2 g_LUN1
            Assert.True(( fstat5.FullStatusDescriptor.Length = 0 ))

            do! r1.Close()
            do! r2.Close()
        }
        
    // SERVICE ACTION RESERVATION KEY<>0 and ALL_TG_PT=1 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an unregistered I_T nexus.
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromUnregistered_ALL_TG_PT_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let r2_sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2";
                    ISID = r1.SessionParams.ISID;
            }
            let! r2 = SCSI_Initiator.CreateWithISID r2_sessParam m_defaultConnParam
            let itn_r1 = GetITNexus r1
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1
            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat2.FullStatusDescriptor.Length = 1 ))

            // register r2 for all target port
            // ( r1 is already registered )
            let! itt_pr_out2 = r2.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.T APTPL.T [||]
            let! res_pr_out2 = r2.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            let msg = 
                res_pr_out2.Sense.Value.VendorSpecific.Value.VendorSpecific
                |> System.Text.Encoding.UTF8.GetString
            Assert.True(( msg.EndsWith "already registered." ))

            // clear ACA
            let! itt_tmf1 = r2.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r2.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

            let! fstat3 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat3.PersistentReservationsGeneration = fstat2.PersistentReservationsGeneration ))
            let fsd3 = fstat3.FullStatusDescriptor
            Assert.True(( fsd3.Length = 1 ))
            Assert.True(( fsd3.[0].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd3.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd3.[0].RelativeTargetPortIdentifier = 1us ))

            // unregister r1 ( target 1 )
            let! itt_pr_out3 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
            do! r2.Close()
        }

    // SERVICE ACTION RESERVATION KEY<>0 and ALL_TG_PT=1 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an unregistered I_T nexus.
    // SPEC_I_PT = 0, but TransportID is not empty. 
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromUnregistered_ALL_TG_PT_003 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1, SPEC_I_PT=0, TransportID is not empty.
            let transid = [| ( "aaaaa", None ) |]
            let! itt_pr_out2 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.T APTPL.T transid
            let! res_pr_out2 = r1.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // clear ACA
            let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg1 = prg2 ))
            do! r1.Close()
        }

    // SERVICE ACTION RESERVATION KEY<>0, ALL_TG_PT=1 and SPEC_I_PT=1 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an unregistered I_T nexus.
    // It terminate with CHECK CONDITION.
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromUnregistered_ALL_TG_PT_SPEC_I_PT_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // REGISTER_AND_IGNORE_EXISTING_KEY, with SERVICE ACTION RESERVATION KEY = 1, SPEC_I_PT = 1, ALL_TG_PT = 1
            let transid = [| ( "aaaaa", Some r1.SessionParams.ISID ) |]
            let! itt_pr_out1 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 SPEC_I_PT.T ALL_TG_PT.T APTPL.T transid
            let! res_pr_out1 = r1.WaitSCSIResponse itt_pr_out1
            Assert.True(( res_pr_out1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // clear ACA
            let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg1 = prg2 ))
            do! r1.Close()
        }

    // SERVICE ACTION RESERVATION KEY=0 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an registered I_T nexus.
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromRegistered_Unregister_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // unregister
            let! itt_pr_out2 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat1.PersistentReservationsGeneration ))
            do! r1.Close()
        }

    // SPEC_I_PT=1 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an registered I_T nexus.
    [<Theory>]
    [<InlineData( 0UL, false )>]
    [<InlineData( 1UL, false )>]
    [<InlineData( 0UL, true )>]
    [<InlineData( 1UL, true )>]
    member _.RegisterAndIgnoreExistingKey_FromRegistered_SPEC_I_PT_001 ( sarv : uint64 ) ( atp : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // unregister ( SPEC_I_PT = 1 )
            let! itt_pr_out2 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim sarv ) SPEC_I_PT.T ( ALL_TG_PT.ofBool atp ) APTPL.T [||]
            let! res_pr_out2 = r1.WaitSCSIResponse itt_pr_out2
            Assert.True(( res_pr_out2.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // clear ACA
            let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            Assert.True(( fstat2.FullStatusDescriptor.Length = 1 ))

            // unregister
            let! itt_pr_out3 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }

    // SERVICE ACTION RESERVATION KEY=0 and ALL_TG_PT=1 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an registered I_T nexus.
    // Unregistered I_T Nexus will be ignored.
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromRegistered_Unregister_ALL_TG_PT_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let r2params = {
                m_defaultSessParam with
                    TargetName="iqn.2020-05.example.com:target2"
                    ISID = r1.SessionParams.ISID;
            }
            let! r2 = SCSI_Initiator.CreateWithISID r2params m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1 ( target 1 )
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // unregister ( ALL_TG_PT = 1 )
            let! itt_pr_out2 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.T APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat1.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r2.Close()
        }

    // SERVICE ACTION RESERVATION KEY=0 and ALL_TG_PT=1 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an registered I_T nexus.
    // I_T Nexus connections from the same initiator to different targets will also be unregistered.
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromRegistered_Unregister_ALL_TG_PT_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let r2params = {
                m_defaultSessParam with
                    TargetName="iqn.2020-05.example.com:target2"
                    ISID = r1.SessionParams.ISID;
            }
            let! r2 = SCSI_Initiator.CreateWithISID r2params m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1 ( target 1 )
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // register r2 ( target 2 )
            let! itt_pr_out1 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            // unregister ( ALL_TG_PT = 1 )
            let! itt_pr_out2 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.T APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat1.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r2.Close()
        }

    static member RegisterAndIgnoreExistingKey_FromRegistered_Unregister_NotHolder_001_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   false; PR_TYPE.NO_RESERVATION |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  false; PR_TYPE.NO_RESERVATION |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; PR_TYPE.NO_RESERVATION |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; PR_TYPE.NO_RESERVATION |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS |]
    |]

    // Unregister the I_T Nexus which is not reservation holders.
    // It will not affect existing reservation.
    [<Theory>]
    [<MemberData( "RegisterAndIgnoreExistingKey_FromRegistered_Unregister_NotHolder_001_data" )>]
    member _.RegisterAndIgnoreExistingKey_FromRegistered_Unregister_NotHolder_001 ( prtype1 : PR_TYPE ) ( isholder2 : bool ) ( prtype2 : PR_TYPE ) =
        task {
            let isids =
                Array.init 2 ( fun _ -> GlbFunc.newISID() )
                |> Array.sortBy isid_me.toPrim
            let! r1 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = isids.[0] } m_defaultConnParam
            let! r2 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = isids.[1] } m_defaultConnParam
            let itn_r1 = GetITNexus r1
            let itn_r2 = GetITNexus r2
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1 
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // reserve
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd1.[0].ReservationHolder ))
            Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            // register r2
            let! itt_pr_out3 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out3

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 =
                fstat2.FullStatusDescriptor
                |> Array.sortBy _.iSCSIName
            Assert.True(( fsd2.Length = 2 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            Assert.True(( fsd2.[1].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd2.[1].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[1].ReservationHolder = isholder2 ))
            Assert.True(( fsd2.[1].Type = PR_TYPE.toNumericValue prtype2 ))

            // unregister r2
            let! itt_pr_out4 = r2.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.T APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out4

            let! fstat3 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat3.PersistentReservationsGeneration > fstat2.PersistentReservationsGeneration ))
            let fsd3 = fstat3.FullStatusDescriptor
            Assert.True(( fsd3.Length = 1 ))
            Assert.True(( fsd3.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd3.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd3.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd3.[0].ReservationHolder ))
            Assert.True(( fsd3.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            // unregister r1
            let! itt_pr_out5 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.T APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat3.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r2.Close()
        }

    static member RegisterAndIgnoreExistingKey_FromRegistered_Unregister_LastOne_001_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS; |]
    |]

    // Unregister the I_T Nexus which is reservation holder and last one.
    [<Theory>]
    [<MemberData( "RegisterAndIgnoreExistingKey_FromRegistered_Unregister_LastOne_001_data" )>]
    member _.RegisterAndIgnoreExistingKey_FromRegistered_Unregister_LastOne_001 ( prtype1 : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let itn_r1 = GetITNexus r1
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // reserve
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd1.[0].ReservationHolder ))
            Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            // unregister r1
            let! itt_pr_out3 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.T APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat1.PersistentReservationsGeneration ))
            do! r1.Close()
        }

    static member RegisterAndIgnoreExistingKey_FromRegistered_Unregister_Holder_001_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   false; PR_TYPE.NO_RESERVATION;                   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  false; PR_TYPE.NO_RESERVATION;                   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; PR_TYPE.NO_RESERVATION;                   true ; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; PR_TYPE.NO_RESERVATION;                   true;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS; false; |]
    |]

    // Unregister the I_T Nexus which is reservation holder and not last one.
    [<Theory>]
    [<MemberData( "RegisterAndIgnoreExistingKey_FromRegistered_Unregister_Holder_001_data" )>]
    member _.RegisterAndIgnoreExistingKey_FromRegistered_Unregister_Holder_001 ( prtype1 : PR_TYPE ) ( isholder2 : bool ) ( prtype2 : PR_TYPE ) ( isua : bool ) =
        task {
            let isids =
                Array.init 2 ( fun _ -> GlbFunc.newISID() )
                |> Array.sortBy isid_me.toPrim
            let! r1 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = isids.[0] } m_defaultConnParam
            let! r2 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = isids.[1] } m_defaultConnParam
            let itn_r1 = GetITNexus r1
            let itn_r2 = GetITNexus r2
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1 
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // reserve
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // register r2
            let! itt_pr_out3 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out3

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            let fsd1 =
                fstat1.FullStatusDescriptor
                |> Array.sortBy _.iSCSIName
            Assert.True(( fsd1.Length = 2 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd1.[0].ReservationHolder ))
            Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            Assert.True(( fsd1.[1].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd1.[1].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd1.[1].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd1.[1].ReservationHolder = isholder2 ))
            Assert.True(( fsd1.[1].Type = PR_TYPE.toNumericValue prtype2 ))

            // unregister r1
            let! itt_pr_out4 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.T APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out4

            // check UA status
            let! itt_read1 = r2.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            if isua then
                let! res_read1 = r2.WaitSCSIResponse itt_read1
                Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( res_read1.Sense.Value.SenseKey = SenseKeyCd.UNIT_ATTENTION ))
                Assert.True(( res_read1.Sense.Value.ASC = ASCCd.RESERVATIONS_RELEASED ))
            else
                let! res_read1 = r2.WaitSCSIResponseGoodStatus itt_read1
                res_read1.Return()

            let! fstat2 = PR_ReadFullStatus r2 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor
            Assert.True(( fsd2.Length = 1 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[0].ReservationHolder = isholder2 ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue prtype2 ))

            // unregister r2
            let! itt_pr_out5 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.T APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out5

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat2.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r2.Close()
        }

    // Different SERVICE ACTION RESERVATION KEY in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an registered I_T nexus.
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromRegistered_UpdateKey_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let itn_r1 = GetITNexus r1
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register with RESERVATION KEY = 1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // Update reservation key
            let! itt_pr_out2 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat3 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat3.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd3 = fstat3.FullStatusDescriptor
            Assert.True(( fsd3.Length = 1 ))
            Assert.True(( fsd3.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd3.[0].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd3.[0].RelativeTargetPortIdentifier = 1us ))

            // Unregister
            let! itt_pr_out3 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat3.PersistentReservationsGeneration ))
            do! r1.Close()
        }

    // Different SERVICE ACTION RESERVATION KEY and ALL_TG_PT=1 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an registered I_T nexus.
    // If there is a mix of registered and unregistered targets, the reservation keys for registered targets will be updated,
    // and unregistered targets will be registered.
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromRegistered_UpdateKey_ALL_TG_PT_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let r1params = {
                m_defaultSessParam with
                    TargetName="iqn.2020-05.example.com:target2"
                    ISID = r1.SessionParams.ISID;
            }
            let itn_r1 = GetITNexus r1
            let! r2 = SCSI_Initiator.CreateWithISID r1params m_defaultConnParam
            let itn_r2 = GetITNexus r2
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1 ( target 1 )
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].RelativeTargetPortIdentifier = 1us ))

            // Update reservation key ( ALL_TG_PT = 1 )
            let! itt_pr_out2 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.T APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 =
                fstat2.FullStatusDescriptor
                |> Array.sortBy _.RelativeTargetPortIdentifier
            Assert.True(( fsd2.Length = 2 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[1].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd2.[1].RelativeTargetPortIdentifier = 2us ))

            // Unregister r1 ( target 1 )
            let! itt_pr_out3 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! fstat3 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat3.PersistentReservationsGeneration > fstat2.PersistentReservationsGeneration ))
            Assert.True(( fstat3.FullStatusDescriptor.Length = 1 ))

            // Unregister r2 ( target 2 )
            let! itt_pr_out4 = r2.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out4

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat3.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r2.Close()
        }

    // SERVICE ACTION RESERVATION KEY=0 and ALL_TG_PT=1 in a PERSISTENT RESERVE OUT command with REGISTER_AND_IGNORE_EXISTING_KEY service action is received from an registered I_T nexus.
    // I_T Nexus from the same initiator to different targets will also be updated.
    [<Fact>]
    member _.RegisterAndIgnoreExistingKey_FromRegistered_UpdateKey_ALL_TG_PT_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let r1params = {
                m_defaultSessParam with
                    TargetName="iqn.2020-05.example.com:target2"
                    ISID = r1.SessionParams.ISID;
            }
            let itn_r1 = GetITNexus r1
            let! r2 = SCSI_Initiator.CreateWithISID r1params m_defaultConnParam
            let itn_r2 = GetITNexus r2
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1 ( target 1 )
            let! itt_pr_out1 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // register r2 ( target 2 )
            let! itt_pr_out2 = r2.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            let fsd1 =
                fstat1.FullStatusDescriptor
                |> Array.sortBy _.RelativeTargetPortIdentifier
            Assert.True(( fsd1.Length = 2 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd1.[1].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd1.[1].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd1.[1].RelativeTargetPortIdentifier = 2us ))

            // update reservation key ( ALL_TG_PT = 1 )
            let! itt_pr_out3 = r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey3 SPEC_I_PT.F ALL_TG_PT.T APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 =
                fstat2.FullStatusDescriptor
                |> Array.sortBy _.RelativeTargetPortIdentifier
            Assert.True(( fsd2.Length = 2 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey3 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[1].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey3 ))
            Assert.True(( fsd2.[1].RelativeTargetPortIdentifier = 2us ))

            // unregister r1
            let! itt_pr_out4 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey3 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out4

            // unregister r2
            let! itt_pr_out5 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey3 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out5

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg2 > fstat2.PersistentReservationsGeneration ))
            do! r1.Close()
            do! r2.Close()
        }

