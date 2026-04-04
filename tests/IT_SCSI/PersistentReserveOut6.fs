//=============================================================================
// Haruka Software Storage.
// PersistentReserveOut6.fs : Test cases for PERSISTENT RESERVE OUT command.
//

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
open Haruka.BlockDeviceLU
open Haruka.Test

//=============================================================================
// Class implementation

[<CollectionDefinition( "SCSI_PersistentReserveOut6" )>]
type SCSI_PersistentReserveOut6_Fixture() =

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

    interface ICollectionFixture<SCSI_PersistentReserveOut6_Fixture>

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


[<Collection( "SCSI_PersistentReserveOut6" )>]
type SCSI_PersistentReserveOut6( fx : SCSI_PersistentReserveOut6_Fixture ) =

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
    let m_InitName = "iqn.2020-05.example.com:initiator"

    // default session parameters
    let m_defaultSessParam = {
        InitiatorName = m_InitName;
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

    let ClearACA ( r : SCSI_Initiator ) ( lun : LUN_T ) : Task<unit> =
        task {
            let! itt_tmf = r.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf = r.WaitTMFResponse itt_tmf
            Assert.True(( res_tmf = TaskMgrResCd.FUNCTION_COMPLETE ))
        }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.RegisterAndMove_Unregistered_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 0 ))

            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey2 UNREG.T APTPL.T 1us ( m_InitName, None )
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! r1.Close()
        }

    [<Fact>]
    member _.RegisterAndMove_Unregistered_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            let! itt = r2.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey3 g_ResvKey2 UNREG.T APTPL.T 1us ( m_InitName, None )
            let! res = r2.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r2 g_LUN1

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r2.Close()
        }

    static member RegisterAndMove_Unregistered_003_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |]
    |]

    [<Theory>]
    [<MemberData( "RegisterAndMove_Unregistered_003_data" )>]
    member _.RegisterAndMove_Unregistered_003 ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            let! itt = r2.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey3 g_ResvKey2 UNREG.T APTPL.T 1us ( m_InitName, None )
            let! res = r2.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r2.Close()
        }

    [<Fact>]
    member _.RegisterAndMove_Registered_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            do! PR_Register r1 g_LUN1 g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey2 UNREG.T APTPL.T 1us ( m_InitName, None )
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r2.Close()
        }

    [<Theory>]
    [<MemberData( "RegisterAndMove_Unregistered_003_data" )>]
    member _.RegisterAndMove_Registered_002 ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            let! itt = r2.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey2 g_ResvKey2 UNREG.T APTPL.T 1us ( m_InitName, None )
            let! res = r2.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r2.Close()
        }

    [<Theory>]
    [<MemberData( "RegisterAndMove_Unregistered_003_data" )>]
    member _.RegisterAndMove_Holder_ResvKeyUnmatch_001 ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey2 g_ResvKey2 UNREG.T APTPL.T 1us ( m_InitName, None )
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r2.Close()
        }

    [<Theory>]
    [<MemberData( "RegisterAndMove_Unregistered_003_data" )>]
    member _.RegisterAndMove_Holder_SARK_0_001 ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 resvkey_me.zero UNREG.T APTPL.T 1us ( m_InitName, None )
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r2.Close()
        }

    static member RegisterAndMove_Holder_AllRegistrants_001_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |]
    |]

    [<Theory>]
    [<MemberData( "RegisterAndMove_Holder_AllRegistrants_001_data" )>]
    member _.RegisterAndMove_Holder_AllRegistrants_001 ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey3 UNREG.T APTPL.T 1us ( m_InitName, None )
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r2.Close()
        }

    static member RegisterAndMove_Holder_UNREG_001_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   |]
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  |]
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
    |]

    [<Theory>]
    [<MemberData( "RegisterAndMove_Holder_UNREG_001_data" )>]
    member _.RegisterAndMove_Holder_UNREG_001 ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let itn_r1 = r1.ITNexus
            let itn_r2 = r2.ITNexus

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r2 g_LUN1
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].ReservationHolder ))
            Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue prtype ))

            let transid = ( m_InitName, Some r2.SessionParams.ISID )
            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey3 UNREG.T APTPL.T 1us transid
            let! _ = r1.WaitSCSIResponseGoodStatus itt

            let! fstat2 = PR_ReadFullStatus r2 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor
            Assert.True(( fsd2.Length = 1 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey3 ))
            Assert.True(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue prtype ))

            do! PR_Unregister r2 g_LUN1 g_ResvKey3
            do! r1.Close()
            do! r2.Close()
        }

    [<Theory>]
    [<MemberData( "RegisterAndMove_Holder_UNREG_001_data" )>]
    member _.RegisterAndMove_Holder_Self_001 ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            let transid = ( m_InitName, Some r1.SessionParams.ISID )
            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey3 UNREG.T APTPL.T 1us transid
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( res.Sense.Value.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))
            do! ClearACA r1 g_LUN1

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2 = fstat1 ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
        }

    [<Theory>]
    [<MemberData( "RegisterAndMove_Holder_UNREG_001_data" )>]
    member _.RegisterAndMove_Holder_Self_002 ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            let transid = ( m_InitName, None )
            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey3 UNREG.T APTPL.T 1us transid
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( res.Sense.Value.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))
            do! ClearACA r1 g_LUN1

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2 = fstat1 ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r2.Close()
        }

    [<Theory>]
    [<MemberData( "RegisterAndMove_Holder_UNREG_001_data" )>]
    member _.RegisterAndMove_Holder_WithISID_001 ( prtype : PR_TYPE ) =
        task {
            let isids = GetSortedISID 2
            let! r1 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = isids.[0] } m_defaultConnParam
            let! r2 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = isids.[1] } m_defaultConnParam
            let itn_r1 = r1.ITNexus
            let itn_r2 = r2.ITNexus

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r2 g_LUN1
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].ReservationHolder ))
            Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue prtype ))

            let transid = ( m_InitName, Some r2.SessionParams.ISID )
            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey2 UNREG.F APTPL.T 1us transid
            let! _ = r1.WaitSCSIResponseGoodStatus itt

            let! fstat2 = PR_ReadFullStatus r2 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor
            Assert.True(( fsd2.Length = 2 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.False(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
            Assert.True(( fsd2.[1].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd2.[1].ReservationHolder ))
            Assert.True(( fsd2.[1].Type = PR_TYPE.toNumericValue prtype ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! r1.Close()
            do! r2.Close()
        }

    [<Theory>]
    [<MemberData( "RegisterAndMove_Holder_UNREG_001_data" )>]
    member _.RegisterAndMove_Holder_WithISID_002 ( prtype : PR_TYPE ) =
        task {
            let isids = GetSortedISID 2
            let! r1 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = isids.[0] } m_defaultConnParam
            let! r2 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = isids.[1] } m_defaultConnParam
            let itn_r1 = r1.ITNexus
            let itn_r2 = r2.ITNexus

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r2 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 2 ))

            let transid = ( m_InitName, Some r2.SessionParams.ISID )
            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey3 UNREG.F APTPL.T 1us transid
            let! _ = r1.WaitSCSIResponseGoodStatus itt

            let! fstat2 = PR_ReadFullStatus r2 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor
            Assert.True(( fsd2.Length = 2 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.False(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
            Assert.True(( fsd2.[1].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey3 ))
            Assert.True(( fsd2.[1].ReservationHolder ))
            Assert.True(( fsd2.[1].Type = PR_TYPE.toNumericValue prtype ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! PR_Unregister r2 g_LUN1 g_ResvKey3
            do! r1.Close()
            do! r2.Close()
        }

    [<Theory>]
    [<MemberData( "RegisterAndMove_Holder_UNREG_001_data" )>]
    member _.RegisterAndMove_Holder_WithoutISID_001 ( prtype : PR_TYPE ) =
        task {
            let isids = GetSortedISID 2
            let initName1 = "iqn.2020-05.example.com:initiator1"
            let initName2 = "iqn.2020-05.example.com:initiator2"
            let sessparam1 = {
                m_defaultSessParam with
                    InitiatorName = initName1;
                    ISID = isids.[0];
            }
            let sessparam2 = {
                m_defaultSessParam with
                    InitiatorName = initName2;
                    ISID = isids.[1];
            }
            let! r1 = SCSI_Initiator.CreateWithISID sessparam1 m_defaultConnParam
            let! r2 = SCSI_Initiator.CreateWithISID sessparam2 m_defaultConnParam
            let itn_r1 = r1.ITNexus
            let itn_r2 = r2.ITNexus

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r2 g_LUN1
            let fsd1 = fstat1.FullStatusDescriptor
            Assert.True(( fsd1.Length = 1 ))
            Assert.True(( fsd1.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd1.[0].ReservationKey = g_ResvKey1 ))
            Assert.True(( fsd1.[0].ReservationHolder ))
            Assert.True(( fsd1.[0].Type = PR_TYPE.toNumericValue prtype ))

            let transid = ( initName2, None )
            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey2 UNREG.F APTPL.T 1us transid
            let! _ = r1.WaitSCSIResponseGoodStatus itt

            let! fstat2 = PR_ReadFullStatus r2 g_LUN1
            Assert.True(( fstat2.PersistentReservationsGeneration > fstat1.PersistentReservationsGeneration ))
            let fsd2 = fstat2.FullStatusDescriptor |> Array.sortBy _.iSCSIName
            Assert.True(( fsd2.Length = 2 ))
            Assert.True(( fsd2.[0].iSCSIName = itn_r1.InitiatorPortName ))
            Assert.True(( fsd2.[0].ReservationKey = g_ResvKey1 ))
            Assert.False(( fsd2.[0].ReservationHolder ))
            Assert.True(( fsd2.[0].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
            Assert.True(( fsd2.[1].iSCSIName = itn_r2.InitiatorPortName ))
            Assert.True(( fsd2.[1].ReservationKey = g_ResvKey2 ))
            Assert.True(( fsd2.[1].ReservationHolder ))
            Assert.True(( fsd2.[1].Type = PR_TYPE.toNumericValue prtype ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! r1.Close()
            do! r2.Close()
        }

    [<Theory>]
    [<MemberData( "RegisterAndMove_Holder_UNREG_001_data" )>]
    member _.RegisterAndMove_Holder_WithoutISID_002 ( prtype : PR_TYPE ) =
        task {
            let isids = GetSortedISID 3
            let initName1 = "iqn.2020-05.example.com:initiator1"
            let initName2 = "iqn.2020-05.example.com:initiator2"
            let sessparam1 = {
                m_defaultSessParam with
                    InitiatorName = initName1;
                    ISID = isids.[0];
            }
            let sessparam2 = {
                m_defaultSessParam with
                    InitiatorName = initName2;
                    ISID = isids.[1];
            }
            let sessparam3 = {
                m_defaultSessParam with
                    InitiatorName = initName2;
                    ISID = isids.[2];
            }
            let! r1 = SCSI_Initiator.CreateWithISID sessparam1 m_defaultConnParam
            let! r2 = SCSI_Initiator.CreateWithISID sessparam2 m_defaultConnParam
            let! r3 = SCSI_Initiator.CreateWithISID sessparam3 m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Reserve r1 g_LUN1 prtype g_ResvKey1

            let! fstat1 = PR_ReadFullStatus r2 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 1 ))

            let transid = ( initName2, None )
            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey2 UNREG.F APTPL.T 1us transid
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( res.Sense.Value.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))
            do! ClearACA r1 g_LUN1

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat2 = fstat1 ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r2.Close()
            do! r3.Close()
        }
    
    [<Fact>]
    member _.RegisterAndMove_APTPL_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let prfname = GetPRFileName g_LUN1

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 0 ))

            // register ( APTPL=0 )
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.F [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // wait for delete PR file
            GlbFunc.WaitForFileDelete prfname

            do! PR_Reserve r1 g_LUN1 PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1

            // Register And Move ( APTPL = 1 )
            let transid = ( m_InitName, Some r2.SessionParams.ISID )
            let! itt = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS g_ResvKey1 g_ResvKey2 UNREG.F APTPL.T 1us transid
            let! _ = r1.WaitSCSIResponseGoodStatus itt

            // wait for create PR file
            GlbFunc.WaitForFileCreate prfname

            let! fstat2 = PR_ReadFullStatus r2 g_LUN1
            Assert.True(( fstat2.FullStatusDescriptor.Length = 2 ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! r1.Close()
            do! r2.Close()
        }

    [<Theory>]
    [<InlineData( 0x08uy )>]
    [<InlineData( 0x09uy )>]
    [<InlineData( 0x1Euy )>]
    [<InlineData( 0x1Fuy )>]
    member _.PR_Out_Unknown_ServiceAction_001 ( sr : byte ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! fstat1 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1.FullStatusDescriptor.Length = 0 ))

            let param : Haruka.BlockDeviceLU.BasicParameterList = {
                ReservationKey = g_ResvKey1;
                ServiceActionReservationKey = g_ResvKey1;
                SPEC_I_PT = false;
                ALL_TG_PT = false;
                APTPL = false;
                TransportID = [||];
            }
            let! itt = r1.Send_PersistentReserveOut_BasicParam TaskATTRCd.SIMPLE_TASK g_LUN1 sr 0uy PR_TYPE.EXCLUSIVE_ACCESS param NACA.T
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1

            let! fstat2 = PR_ReadFullStatus r1 g_LUN1
            Assert.True(( fstat1 = fstat2 ))

            do! r1.Close()
        }
