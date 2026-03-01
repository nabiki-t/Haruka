//=============================================================================
// Haruka Software Storage.
// PersistentReservation.fs : Test cases for Persistent Reservation behavior.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.IT.SCSI

//=============================================================================
// Import declaration

open System
open System.IO
open System.Net
open System.Threading
open System.Threading.Tasks

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.Test
open System.Text.RegularExpressions

//=============================================================================
// Class implementation

[<CollectionDefinition( "SCSI_PersistentReservation" )>]
type SCSI_PersistentReservation_Fixture() =

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
        client.RunCommand "attach /l 1" "Attach LU" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand "select 0" "" "MD> "

        client.RunCommand "validate" "All configurations are vlidated" "MD> "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "MD> "
        client.RunCommand "start" "Started" "MD> "
        client.RunCommand "add trap /e TestUnitReady /a Delay /ms 1000" "Trap added" "MD> "

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

    interface ICollectionFixture<SCSI_PersistentReservation_Fixture>

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


[<Collection( "SCSI_PersistentReservation" )>]
type SCSI_PersistentReservation( fx : SCSI_PersistentReservation_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_CID1 = cid_me.fromPrim 1us
    let g_CID2 = cid_me.fromPrim 2us

    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL

    let g_DefITT = itt_me.fromPrim 0xFFFFFFFFu
    let g_DefTTT = ttt_me.fromPrim 0xFFFFFFFFu

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

    // Wait until a file is created
    let Wait_CreateFile ( fname : string ) : unit =
        while ( File.Exists fname |> not ) do
            Thread.Sleep 10

    // Wait until a file is updated
    let Wait_UpdateFile( fname : string ) : unit =
        let lwt = File.GetLastWriteTimeUtc fname
        while ( lwt = File.GetLastWriteTimeUtc fname ) do
            Thread.Sleep 10

    let checkDisconnected ( s : SCSI_Initiator ) : Task =
        task {
            try
                while true do
                    let! itt = s.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
                    let! _ = s.WaitSCSIResponse itt
                    ()
            with
            | :? SessionRecoveryException
            | :? ConnectionErrorException ->
                ()
        }

    let PR_ReadKey ( r : SCSI_Initiator ) ( lun : LUN_T ) : Task<PR_ReadKey> =
        task {
            let! itt_pr_in1 = r.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 0uy 512us NACA.T
            return! r.Wait_PersistentReserveIn_ReadKey itt_pr_in1
        }


    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    // PR will not be lost due to LU reset
    [<Fact>]
    member _.PersistReservation_LUReset_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey = resvkey_me.fromPrim 1UL
            let prfname = GetPRFileName g_LUN1
            File.Delete prfname

            // register reservation key
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1
            Wait_CreateFile prfname

            // LU Reset
            let! itt_tmf = r1.SendTMFRequest_LogicalUnitReset BitI.F g_LUN1
            let! res_tmf = r1.WaitTMFResponse itt_tmf
            Assert.True(( res_tmf = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Get reservarion key
            let! res_pr_in1 = PR_ReadKey r1 g_LUN1
            Assert.True(( res_pr_in1.ReservationKey.Length = 1 ))
            Assert.True(( res_pr_in1.ReservationKey.[0] = resvkey ))

            // clear reservation key
            let! itt_pr_out2 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            do! r1.Close()
        }
        
    // PR will not be lost due to Target Device reset.
    [<Fact>]
    member _.PersistReservation_TargetDeviceReset_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey = resvkey_me.fromPrim 1UL
            let prfname = GetPRFileName g_LUN1
            File.Delete prfname

            // register reservation key
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1
            Wait_CreateFile prfname

            // Target device reset
            let! _ = r1.SendTMFRequest_LogicalUnitReset BitI.F g_LUN0
            do! checkDisconnected r1

            let! r2 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = r1.SessionParams.ISID } m_defaultConnParam

            // Get reservarion key
            let! res_pr_in1 = PR_ReadKey r2 g_LUN1
            Assert.True(( res_pr_in1.ReservationKey.Length = 1 ))
            Assert.True(( res_pr_in1.ReservationKey.[0] = resvkey ))

            // clear reservation key
            let! itt_pr_out2 = r2.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2

            do! r2.Close()
        }
