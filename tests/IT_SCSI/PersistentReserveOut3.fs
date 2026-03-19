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

    let CheckNoRegistrations ( r : SCSI_Initiator ) ( lun : LUN_T ) : Task<unit> =
        task {
            let! fstat1 = PR_ReadFullStatus r lun
            Assert.True(( fstat1.FullStatusDescriptor.Length = 0 ))
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
            do! CheckNoRegistrations r1 g_LUN1

            let! itt_pr_out1 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey_me.zero
            let! res_pr_out1 = r1.WaitSCSIResponse itt_pr_out1
            Assert.True(( res_pr_out1.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            do! CheckNoRegistrations r1 g_LUN1
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
            do! CheckNoRegistrations r1 g_LUN1

            // register
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // reserve
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd1.[0].ReservationHolder ))
            Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue prtype ))

            // unregister
            let! itt_pr_out3 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            do! CheckNoRegistrations r1 g_LUN1
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
            do! CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // register r2
            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2

            // reserve
            let! itt_pr_out3 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
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

            // unregister
            let! itt_pr_out5 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out5
            let! itt_pr_out4 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out4

            do! CheckNoRegistrations r1 g_LUN1
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
            do! CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // register r2
            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2

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

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
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

            // unregister
            let! itt_pr_out5 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out5
            let! itt_pr_out6 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

            do! CheckNoRegistrations r1 g_LUN1
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
            do! CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

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

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            let fsd1 =
                fstat1.FullStatusDescriptor
                |> Array.sortBy _.iSCSIName
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd1.[0].ReservationHolder ))
            Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue ( if exresult then prtype2 else prtype1 ) ))

            // unregister
            let! itt_pr_out5 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            do! CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }

    [<Fact>]
    member _.Reserve_FromRegistered_RegKeyMismatch_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            do! CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // reserve r1
            let! itt_pr_out3 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE g_ResvKey2
            let! res_pr_out3 = r1.WaitSCSIResponse itt_pr_out3
            Assert.True(( res_pr_out3.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            // unregister
            let! itt_pr_out5 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            do! CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }

    [<Fact>]
    member _.Reserve_InvalidScope_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            do! CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

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

            // unregister
            let! itt_pr_out5 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            do! CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }

    // A PERSISTENT RESERVE OUT command with RELEASE service action is received from an unregistered I_T nexus.
    [<Fact>]
    member _.Release_FromUnregistered_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            do! CheckNoRegistrations r1 g_LUN1

            let! itt_pr_out1 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE resvkey_me.zero
            let! res_pr_out1 = r1.WaitSCSIResponse itt_pr_out1
            Assert.True(( res_pr_out1.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            do! CheckNoRegistrations r1 g_LUN1
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
            do! CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // release
            let! itt_pr_out2 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.False(( fsd1.[0].ReservationHolder ))
            Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

            // unregister
            let! itt_pr_out5 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            do! CheckNoRegistrations r1 g_LUN1
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
            do! CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // register r2
            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2

            // reserve r1
            let! itt_pr_out3 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
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
            Assert.False(( fsd1.[1].ReservationHolder ))
            Assert.True(( fsd1.[1].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

            // release
            let! itt_pr_out4 = r2.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype3 g_ResvKey2
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out4

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
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
            Assert.False(( fsd1.[1].ReservationHolder ))
            Assert.True(( fsd1.[1].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))

            // unregister r2
            let! itt_pr_out5 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey2 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out5

            // unregister r1
            let! itt_pr_out6 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

            do! CheckNoRegistrations r1 g_LUN1
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
            do! CheckNoRegistrations r1 g_LUN1

            // register r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // reserve
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype1 g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( fsd1.[0].ReservationHolder ))
            Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            // release
            let! itt_pr_out3 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype2 g_ResvKey1
            if exresult then
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3
                let! fstat2 = PR_ReadFullStatus r1 g_LUN1
                let fsd2 = fstat2.FullStatusDescriptor
                Assert.True(( fsd2.Length = 1 ))
                Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
                Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
                Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
                Assert.False(( fsd2.[0].ReservationHolder ))
                Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
            else
                let! res_pr_out3 = r1.WaitSCSIResponse itt_pr_out3
                Assert.True(( res_pr_out3.Status = ScsiCmdStatCd.CHECK_CONDITION ))

                // clear ACA
                let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
                let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
                Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

                let! fstat2 = PR_ReadFullStatus r1 g_LUN1
                let fsd2 = fstat2.FullStatusDescriptor
                Assert.True(( fsd2.Length = 1 ))
                Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
                Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
                Assert.True(( fsd2.[0].RelativeTargetPortIdentifier = 1us ))
                Assert.True(( fsd2.[0].ReservationHolder ))
                Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue prtype1 ))

            // unregister
            let! itt_pr_out5 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1 resvkey_me.zero SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out5

            do! CheckNoRegistrations r1 g_LUN1
            do! r1.Close()
        }
