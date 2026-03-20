//=============================================================================
// Haruka Software Storage.
// PersistentReserveOut3.fs : Test cases for PERSISTENT RESERVE OUT command.
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

[<CollectionDefinition( "SCSI_PersistentReserveOut3" )>]
type SCSI_PersistentReserveOut3_Fixture() =

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

    interface ICollectionFixture<SCSI_PersistentReserveOut3_Fixture>

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


[<Collection( "SCSI_PersistentReserveOut3" )>]
type SCSI_PersistentReserveOut3( fx : SCSI_PersistentReserveOut3_Fixture ) =

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

    static member Reserve_FromUnregistered_001_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS; |]
    |]

    // A PERSISTENT RESERVE OUT command with RESERVE service action is received from an unregistered I_T nexus.
    // It will terminated with RESERVATION_CONFLICT status.
    [<Theory>]
    [<MemberData( "Reserve_FromUnregistered_001_data" )>]
    member _.Reserve_FromUnregistered_001 ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            let! itt_pr_out1 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey_me.zero
            let! res_pr_out1 = r1.WaitSCSIResponse itt_pr_out1
            Assert.True(( res_pr_out1.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg1 = prg2 ))
            do! r1.Close()
        }

    // A PERSISTENT RESERVE OUT command with RESERVE service action is received from an registered I_T nexus.
    // A case where no other registrations exist.
    [<Theory>]
    [<MemberData( "Reserve_FromUnregistered_001_data" )>]
    member _.Reserve_FromRegistered_001 ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let itn_r1 = GetITNexus r1
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // reserve
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor
            Assert.True(( fsd2.Length = 1 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue prtype ))

            // unregister
            let! itt_pr_out3 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! prg3 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg3 > fstat2.PersistentReservationsGeneration ))
            do! r1.Close()
        }

    static member Reserve_FromRegistered_002_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   false; PR_TYPE.NO_RESERVATION |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  false; PR_TYPE.NO_RESERVATION |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; PR_TYPE.NO_RESERVATION |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; PR_TYPE.NO_RESERVATION |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS |]
    |]

    // A PERSISTENT RESERVE OUT command with RESERVE service action is received from an registered I_T nexus.
    // A case where other registrations exist.
    [<Theory>]
    [<MemberData( "Reserve_FromRegistered_002_data" )>]
    member _.Reserve_FromRegistered_002 ( prtype1 : PR_TYPE ) ( isholder2 : bool ) ( prtype2 : PR_TYPE ) =
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

            // register r2
            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat2.FullStatusDescriptor.Length = 2 ))

            // reserve
            let! itt_pr_out3 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! fstat3 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat3.PersistentReservationsGeneration = fstat2.PersistentReservationsGeneration ))
            let fsd3 =
                fstat3.FullStatusDescriptor
                |> Array.sortBy _.iSCSIName
            Assert.True(( fsd3.Length = 2 ))
            Assert.True(( fsd3.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd3.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd3.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd3.[0].ReservationHolder ))
            Assert.True(( fsd3.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            Assert.True(( fsd3.[1].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd3.[1].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd3.[1].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd3.[1].ReservationHolder = isholder2 ))
            Assert.True(( fsd3.[1].Type = PR_TYPE.toNumericValue prtype2 ))

            // unregister
            let! itt_pr_out5 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out5
            let! itt_pr_out4 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out4

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
            do! r2.Close()
        }

    static member Reserve_FromRegistered_003_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.EXCLUSIVE_ACCESS;                  false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;  PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;  PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   true ; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; PR_TYPE.NO_RESERVATION;                   PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS; PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS; PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  true;  |]
    |]

    // A PERSISTENT RESERVE OUT command with RESERVE service action is received from an registered I_T nexus.
    // A case where a reservation is attempted when a reservation already exists.
    [<Theory>]
    [<MemberData( "Reserve_FromRegistered_003_data" )>]
    member _.Reserve_FromRegistered_003 ( prtype1 : PR_TYPE ) ( isholder2 : bool ) ( prtype2 : PR_TYPE ) ( attpr : PR_TYPE ) ( exresult : bool ) =
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

            // register r2
            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            // reserve r1
            let! itt_pr_out3 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            // reserve r2
            let! itt_pr_out4 = r2.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T attpr g_ResvKey2
            if exresult then
                let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out4
                ()
            else
                let! res_pr_out4 = r2.WaitSCSIResponse itt_pr_out4
                Assert.True(( res_pr_out4.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
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

            // unregister
            let! itt_pr_out5 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out5
            let! itt_pr_out6 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
            do! r2.Close()
        }

    static member Reserve_FromRegistered_004_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.WRITE_EXCLUSIVE;                   true;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.EXCLUSIVE_ACCESS;                  true;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  true;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   true ; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; true;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  true;  |]
    |]

    // A PERSISTENT RESERVE OUT command with RESERVE service action is received from an registered I_T nexus.
    // Cases where a holder of a reservation attempts to modify an existing reservation.
    [<Theory>]
    [<MemberData( "Reserve_FromRegistered_004_data" )>]
    member _.Reserve_FromRegistered_004 ( prtype1 : PR_TYPE ) ( prtype2 : PR_TYPE ) ( exresult : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let itn_r1 = GetITNexus r1
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // reserve r1
            let! itt_pr_out3 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            // modify reservation type
            let! itt_pr_out4 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype2 g_ResvKey1
            if exresult then
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out4
                ()
            else
                let! res_pr_out4 = r1.WaitSCSIResponse itt_pr_out4
                Assert.True(( res_pr_out4.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            let fsd2 =
                fstat2.FullStatusDescriptor
                |> Array.sortBy _.iSCSIName
            Assert.True(( fsd2.Length = 1 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue ( if exresult then prtype2 else prtype1 ) ))

            // unregister
            let! itt_pr_out5 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }

    [<Fact>]
    member _.Reserve_FromRegistered_RegKeyMismatch_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // reserve r1
            let! itt_pr_out3 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey2
            let! res_pr_out3 = r1.WaitSCSIResponse itt_pr_out3
            Assert.True(( res_pr_out3.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            Assert.True(( fstat2.FullStatusDescriptor.Length = 1 ))

            // unregister
            let! itt_pr_out5 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }

    [<Fact>]
    member _.Reserve_InvalidScope_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // reserve r1 ( SCOPE <> 0 )
            let param : Haruka.BlockDeviceLU.BasicParameterList = {
                ReservationKey = g_ResvKey2;
                ServiceActionReservationKey = resvkey_me.zero;
                SPEC_I_PT = false;
                ALL_TG_PT = false;
                APTPL = false;
                TransportID = [||];
            }
            let! itt_pr_out3 = r1.Send_PersistentReserveOut_BasicParam TaskATTRCd.SIMPLE_TASK g_LUN1 1uy 1uy PR_TYPE.WRITE_EXCLUSIVE param NACA.T
            let! res_pr_out3 = r1.WaitSCSIResponse itt_pr_out3
            Assert.True(( res_pr_out3.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // clear ACA
            let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            Assert.True(( fstat2.FullStatusDescriptor.Length = 1 ))

            // unregister
            let! itt_pr_out5 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }

    // A PERSISTENT RESERVE OUT command with RELEASE service action is received from an unregistered I_T nexus.
    [<Fact>]
    member _.Release_FromUnregistered_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            let! itt_pr_out1 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE resvkey_me.zero
            let! res_pr_out1 = r1.WaitSCSIResponse itt_pr_out1
            Assert.True(( res_pr_out1.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! prg2 = CheckNoRegistrations r1 g_LUN1
            Assert.True(( prg1 = prg2 ))
            do! r1.Close()
        }

    static member Release_FromRegistered_NotHolder_001_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
    |]

    // A PERSISTENT RESERVE OUT command with RELEASE service action is received from an registered but not holder I_T nexus.
    [<Theory>]
    [<MemberData( "Release_FromRegistered_NotHolder_001_data" )>]
    member _.Release_FromRegistered_NotHolder_001 ( prtype :PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let itn_r1 = GetITNexus r1
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // release
            let! itt_pr_out2 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor
            Assert.True(( fsd2.Length = 1 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.False(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

            // unregister
            let! itt_pr_out5 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }

    static member Release_FromRegistered_NotHolder_002_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.WRITE_EXCLUSIVE;                   |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.EXCLUSIVE_ACCESS;                  |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |]

        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.WRITE_EXCLUSIVE;                   |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.EXCLUSIVE_ACCESS;                  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |]

        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.WRITE_EXCLUSIVE;                   |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.EXCLUSIVE_ACCESS;                  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |]

        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.WRITE_EXCLUSIVE;                   |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.EXCLUSIVE_ACCESS;                  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |]
    |]

    // A PERSISTENT RESERVE OUT command with RELEASE service action is received from an registered but not holder I_T nexus.
    [<Theory>]
    [<MemberData( "Release_FromRegistered_NotHolder_002_data" )>]
    member _.Release_FromRegistered_NotHolder_002 ( prtype1 : PR_TYPE ) ( prtype3 : PR_TYPE ) =
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

            // register r2
            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            // reserve r1
            let! itt_pr_out3 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
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
            Assert.False(( fsd2.[1].ReservationHolder ))
            Assert.True(( fsd2.[1].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

            // release
            let! itt_pr_out4 = r2.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype3 g_ResvKey2
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out4

            let! fstat3 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat3.PersistentReservationsGeneration = fstat2.PersistentReservationsGeneration ))
            let fsd3 =
                fstat3.FullStatusDescriptor
                |> Array.sortBy _.iSCSIName
            Assert.True(( fsd3.Length = 2 ))
            Assert.True(( fsd3.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd3.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd3.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd3.[0].ReservationHolder ))
            Assert.True(( fsd3.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            Assert.True(( fsd3.[1].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd3.[1].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd3.[1].RelativeTargetPortIdentifier = 1us ))
            Assert.False(( fsd3.[1].ReservationHolder ))
            Assert.True(( fsd3.[1].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

            // unregister r2
            let! itt_pr_out5 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out5

            // unregister r1
            let! itt_pr_out6 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
            do! r2.Close()
        }

    // The reservation will be released when there are no other registrations.
    // If the current reservation and the reservation type in the RELEASE service action are different, the command will terminate with CHECK_CONDITION.
    [<Theory>]
    [<MemberData( "Reserve_FromRegistered_004_data" )>]
    member _.Release_FromHolder_NoOtheres_001 ( prtype1 : PR_TYPE ) ( prtype2 : PR_TYPE ) ( exresult : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let itn_r1 = GetITNexus r1
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // reserve
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor
            Assert.True(( fsd2.Length = 1 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            // release
            let! itt_pr_out3 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype2 g_ResvKey1
            if exresult then
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3
                let! fstat3 = PR_ReadFullStatus r1 g_LUN1
                Assert.True(( fstat3.PersistentReservationsGeneration = fstat2.PersistentReservationsGeneration ))
                let fsd3 = fstat3.FullStatusDescriptor
                Assert.True(( fsd3.Length = 1 ))
                Assert.True(( fsd3.[0].iSCSIName = itn_r1.InitiatorPortName ))
                Assert.True(( fsd3.[0].ReservationKey = g_ResvKey1 ))
                Assert.True(( fsd3.[0].RelativeTargetPortIdentifier = 1us ))
                Assert.False(( fsd3.[0].ReservationHolder ))
                Assert.True(( fsd3.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
            else
                let! res_pr_out3 = r1.WaitSCSIResponse itt_pr_out3
                Assert.True(( res_pr_out3.Status = ScsiCmdStatCd.CHECK_CONDITION ))

                // clear ACA
                let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
                let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
                Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

                let! fstat4 = PR_ReadFullStatus r1 g_LUN1
                Assert.True(( fstat4.PersistentReservationsGeneration = fstat2.PersistentReservationsGeneration ))
                let fsd4 = fstat4.FullStatusDescriptor
                Assert.True(( fsd4.Length = 1 ))
                Assert.True(( fsd4.[0].iSCSIName = itn_r1.InitiatorPortName ))
                Assert.True(( fsd4.[0].ReservationKey = g_ResvKey1 ))
                Assert.True(( fsd4.[0].RelativeTargetPortIdentifier = 1us ))
                Assert.True(( fsd4.[0].ReservationHolder ))
                Assert.True(( fsd4.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            // unregister
            let! itt_pr_out5 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }

    static member Release_FromHolder_ExistsRegistrations_001_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.WRITE_EXCLUSIVE;                   true;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE;                   PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.EXCLUSIVE_ACCESS;                  true;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]
    |]

    // Release the reservation from the reservation holder when other registrations already exist.
    // WRITE_EXCLUSIVE/EXCLUSIVE_ACCESS
    // The reservation will be releases, the registrations will remain, UA is not established.
    [<Theory>]
    [<MemberData( "Release_FromHolder_ExistsRegistrations_001_data" )>]
    member _.Release_FromHolder_ExistsRegistrations_001 ( prtype1 : PR_TYPE ) ( prtype2 : PR_TYPE ) ( exresult : bool ) =
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

            // register r2
            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            // reserve r1
            let! itt_pr_out3 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor
            Assert.True(( fsd2.Length = 2 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue prtype1 ))
            Assert.True(( fsd2.[1].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd2.[1].RelativeTargetPortIdentifier = 1us ))
            Assert.False(( fsd2.[1].ReservationHolder ))
            Assert.True(( fsd2.[1].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

            // release
            let! itt_pr_out4 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype2 g_ResvKey1
            if exresult then
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out4
                let! fstat3 = PR_ReadFullStatus r1 g_LUN1
                Assert.True(( fstat3.PersistentReservationsGeneration = fstat2.PersistentReservationsGeneration ))
                let fsd3 = fstat3.FullStatusDescriptor
                Assert.True(( fsd3.Length = 2 ))
                Assert.True(( fsd3.[0].iSCSIName = itn_r1.InitiatorPortName ))
                Assert.True(( fsd3.[0].ReservationKey = g_ResvKey1 ))
                Assert.True(( fsd3.[0].RelativeTargetPortIdentifier = 1us ))
                Assert.False(( fsd3.[0].ReservationHolder ))
                Assert.True(( fsd3.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
                Assert.True(( fsd3.[1].iSCSIName = itn_r2.InitiatorPortName ))
                Assert.True(( fsd3.[1].ReservationKey = g_ResvKey2 ))
                Assert.True(( fsd3.[1].RelativeTargetPortIdentifier = 1us ))
                Assert.False(( fsd3.[1].ReservationHolder ))
                Assert.True(( fsd3.[1].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
            else
                let! res_pr_out4 = r1.WaitSCSIResponse itt_pr_out4
                Assert.True(( res_pr_out4.Status = ScsiCmdStatCd.CHECK_CONDITION ))

                // clear ACA
                let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
                let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
                Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

                let! fstat3 = PR_ReadFullStatus r1 g_LUN1
                Assert.True(( fstat2 = fstat3 ))

            // unregister r1
            let! itt_pr_out5 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            // unregister r2
            let! itt_pr_out6 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out6

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }
        
    static member Release_FromHolder_ExistsRegistrations_002_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  true;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   true ; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; true;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  false; |]

        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  PR_TYPE.WRITE_EXCLUSIVE;                   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  PR_TYPE.EXCLUSIVE_ACCESS;                  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; false; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  true;  |]
    |]

    // Release the reservation from the reservation holder when other registrations already exist.
    // WRITE_EXCLUSIVE_REGISTRANTS_ONLY/EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
    // The reservation will be releases, the registrations will remain, UA will be established.
    [<Theory>]
    [<MemberData( "Release_FromHolder_ExistsRegistrations_002_data" )>]
    member _.Release_FromHolder_ExistsRegistrations_002 ( prtype1 : PR_TYPE ) ( prtype2 : PR_TYPE ) ( exresult : bool ) =
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
            let itn_r1 = GetITNexus r1
            let itn_r2 = GetITNexus r2
            let itn_r3 = GetITNexus r3
            let itn_r4 = GetITNexus r4
            let other_isHolder = PR_TYPE.isAllRegistrants prtype1
            let other_pr_type = if other_isHolder then prtype1 else PR_TYPE.NO_RESERVATION
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

            // reserve r1
            let! itt_pr_out05 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out05

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor |> Array.sortBy _.ReservationKey
            Assert.True(( fsd2.Length = 4 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            Assert.True(( fsd2.[1].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd2.[1].RelativeTargetPortIdentifier = 2us ))
            Assert.True(( fsd2.[1].ReservationHolder = other_isHolder ))
            Assert.True(( fsd2.[1].Type = PR_TYPE.toNumericValue other_pr_type ))

            Assert.True(( fsd2.[2].iSCSIName = itn_r3.InitiatorPortName ))
            Assert.True(( fsd2.[2].ReservationKey = g_ResvKey3 ))
            Assert.True(( fsd2.[2].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[2].ReservationHolder = other_isHolder ))
            Assert.True(( fsd2.[2].Type = PR_TYPE.toNumericValue other_pr_type ))

            Assert.True(( fsd2.[3].iSCSIName = itn_r4.InitiatorPortName ))
            Assert.True(( fsd2.[3].ReservationKey = g_ResvKey4 ))
            Assert.True(( fsd2.[3].RelativeTargetPortIdentifier = 2us ))
            Assert.True(( fsd2.[3].ReservationHolder = other_isHolder ))
            Assert.True(( fsd2.[3].Type = PR_TYPE.toNumericValue other_pr_type ))

            // release
            let! itt_pr_out06 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype2 g_ResvKey1
            if exresult then
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out06

                // check UA for initiator r1, r2
                // Normally, a UA is not established for r1. 
                // However, since r1 and r2 share the same initiator, a UA is established for r2's initiator.
                let! itt_read1 = r1.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
                let! res_read1 = r1.WaitSCSIResponse itt_read1
                Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( res_read1.Sense.Value.SenseKey = SenseKeyCd.UNIT_ATTENTION ))
                Assert.True(( res_read1.Sense.Value.ASC = ASCCd.RESERVATIONS_RELEASED ))

                let! itt_read2 = r2.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
                let! res_read2 = r2.WaitSCSIResponseGoodStatus itt_read2
                res_read2.Return()

                // check UA for initiator r3, r4
                let! itt_read3 = r3.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
                let! res_read3 = r3.WaitSCSIResponse itt_read3
                Assert.True(( res_read3.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( res_read3.Sense.Value.SenseKey = SenseKeyCd.UNIT_ATTENTION ))
                Assert.True(( res_read3.Sense.Value.ASC = ASCCd.RESERVATIONS_RELEASED ))

                let! itt_read4 = r4.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
                let! res_read4 = r4.WaitSCSIResponseGoodStatus itt_read4
                res_read4.Return()

                let! fstat3 = PR_ReadFullStatus r1 g_LUN1
                Assert.True(( fstat3.PersistentReservationsGeneration = fstat2.PersistentReservationsGeneration ))
                let fsd3 = fstat3.FullStatusDescriptor |> Array.sortBy _.ReservationKey
                Assert.True(( fsd3.Length = 4 ))
                Assert.True(( fsd3.[0].iSCSIName = itn_r1.InitiatorPortName ))
                Assert.True(( fsd3.[0].ReservationKey = g_ResvKey1 ))
                Assert.True(( fsd3.[0].RelativeTargetPortIdentifier = 1us ))
                Assert.False(( fsd3.[0].ReservationHolder ))
                Assert.True(( fsd3.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

                Assert.True(( fsd3.[1].iSCSIName = itn_r2.InitiatorPortName ))
                Assert.True(( fsd3.[1].ReservationKey = g_ResvKey2 ))
                Assert.True(( fsd3.[1].RelativeTargetPortIdentifier = 2us ))
                Assert.False(( fsd3.[1].ReservationHolder ))
                Assert.True(( fsd3.[1].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

                Assert.True(( fsd3.[2].iSCSIName = itn_r3.InitiatorPortName ))
                Assert.True(( fsd3.[2].ReservationKey = g_ResvKey3 ))
                Assert.True(( fsd3.[2].RelativeTargetPortIdentifier = 1us ))
                Assert.False(( fsd3.[2].ReservationHolder ))
                Assert.True(( fsd3.[2].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

                Assert.True(( fsd3.[3].iSCSIName = itn_r4.InitiatorPortName ))
                Assert.True(( fsd3.[3].ReservationKey = g_ResvKey4 ))
                Assert.True(( fsd3.[3].RelativeTargetPortIdentifier = 2us ))
                Assert.False(( fsd3.[3].ReservationHolder ))
                Assert.True(( fsd3.[3].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

            else
                let! res_pr_out06 = r1.WaitSCSIResponse itt_pr_out06
                Assert.True(( res_pr_out06.Status = ScsiCmdStatCd.CHECK_CONDITION ))

                // clear ACA
                let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
                let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
                Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

                let! fstat4 = PR_ReadFullStatus r1 g_LUN1
                Assert.True(( fstat2 = fstat4 ))

            // unregister r2, r3, r4, r1
            let! itt_pr_out07 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out07
            let! itt_pr_out08 = r3.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey3 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r3.WaitSCSIResponseGoodStatus itt_pr_out08
            let! itt_pr_out09 = r4.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey4 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r4.WaitSCSIResponseGoodStatus itt_pr_out09
            let! itt_pr_out10 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out10

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
            do! r2.Close()
            do! r3.Close()
            do! r4.Close()
        }
        
    static member Release_FromHolder_ExistsRegistrations_003_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
    |]

    // Release the reservation from the reservation holder when other registrations already exist.
    // WRITE_EXCLUSIVE_REGISTRANTS_ONLY/EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
    // The reservation will be releases, the registrations will remain, UA will be established.
    [<Theory>]
    [<MemberData( "Release_FromHolder_ExistsRegistrations_003_data" )>]
    member _.Release_FromHolder_ExistsRegistrations_003 ( prtype1 : PR_TYPE ) =
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
            let itn_r1 = GetITNexus r1
            let itn_r3 = GetITNexus r3
            let itn_r4 = GetITNexus r4
            let other_isHolder = PR_TYPE.isAllRegistrants prtype1
            let other_pr_type = if other_isHolder then prtype1 else PR_TYPE.NO_RESERVATION
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

            // reserve r1
            let! itt_pr_out05 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out05

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor |> Array.sortBy _.ReservationKey
            Assert.True(( fsd2.Length = 3 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            Assert.True(( fsd2.[1].iSCSIName = itn_r3.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey3 ))
            Assert.True(( fsd2.[1].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd2.[1].ReservationHolder = other_isHolder ))
            Assert.True(( fsd2.[1].Type = PR_TYPE.toNumericValue other_pr_type ))

            Assert.True(( fsd2.[2].iSCSIName = itn_r4.InitiatorPortName ))
            Assert.True(( fsd2.[2].ReservationKey = g_ResvKey4 ))
            Assert.True(( fsd2.[2].RelativeTargetPortIdentifier = 2us ))
            Assert.True(( fsd2.[2].ReservationHolder = other_isHolder ))
            Assert.True(( fsd2.[2].Type = PR_TYPE.toNumericValue other_pr_type ))

            // release
            let! itt_pr_out06 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out06

            // check UA for initiator r4, r3
            let! itt_read3 = r4.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! res_read3 = r4.WaitSCSIResponse itt_read3
            Assert.True(( res_read3.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res_read3.Sense.Value.SenseKey = SenseKeyCd.UNIT_ATTENTION ))
            Assert.True(( res_read3.Sense.Value.ASC = ASCCd.RESERVATIONS_RELEASED ))

            let! itt_read4 = r3.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! res_read4 = r3.WaitSCSIResponseGoodStatus itt_read4
            res_read4.Return()

            // *** Note that the UA has not been established for r1.
            let! fstat3 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat3.PersistentReservationsGeneration = fstat2.PersistentReservationsGeneration ))
            let fsd3 = fstat3.FullStatusDescriptor |> Array.sortBy _.ReservationKey
            Assert.True(( fsd3.Length = 3 ))
            Assert.True(( fsd3.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd3.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd3.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.False(( fsd3.[0].ReservationHolder ))
            Assert.True(( fsd3.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
            Assert.True(( fsd3.[1] = fsd2.[1] ))
            Assert.True(( fsd3.[2] = fsd2.[2] ))

            // unregister r3, r4, r1
            let! itt_pr_out08 = r3.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey3 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r3.WaitSCSIResponseGoodStatus itt_pr_out08
            let! itt_pr_out09 = r4.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey4 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r4.WaitSCSIResponseGoodStatus itt_pr_out09
            let! itt_pr_out10 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out10

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
            do! r3.Close()
            do! r4.Close()
        }

    [<Fact>]
    member _.Release_InvalidScope_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! prg1 = CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // reserve r1
            let! itt_pr_out05 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out05

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.PersistentReservationsGeneration > prg1 ))
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            // release r1 ( SCOPE <> 0 )
            let param : Haruka.BlockDeviceLU.BasicParameterList = {
                ReservationKey = g_ResvKey1;
                ServiceActionReservationKey = resvkey_me.zero;
                SPEC_I_PT = false;
                ALL_TG_PT = false;
                APTPL = false;
                TransportID = [||];
            }
            let! itt_pr_out3 = r1.Send_PersistentReserveOut_BasicParam TaskATTRCd.SIMPLE_TASK g_LUN1 2uy 1uy PR_TYPE.WRITE_EXCLUSIVE param NACA.T
            let! res_pr_out3 = r1.WaitSCSIResponse itt_pr_out3
            Assert.True(( res_pr_out3.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // clear ACA
            let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            Assert.True(( fstat2.FullStatusDescriptor.Length = 1 ))

            // unregister
            let! itt_pr_out5 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            let! _ = CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }