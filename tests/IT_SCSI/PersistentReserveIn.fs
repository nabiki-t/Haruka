//=============================================================================
// Haruka Software Storage.
// PersistentReserveIn.fs : Test cases for PERSISTENT RESERVE OUT command.
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
open Haruka.Test

//=============================================================================
// Class implementation

[<CollectionDefinition( "SCSI_PersistentReserveIn" )>]
type SCSI_PersistentReserveIn_Fixture() =

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
        client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "LU> "
        client.RunCommand "unselect" "" "T > "
        client.RunCommand "create /l 2" "Created" "T > "
        client.RunCommand "select 1" "" "LU> "
        client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "LU> "
        client.RunCommand "unselect" "" "T > "
        client.RunCommand "unselect" "" "TG> "
        client.RunCommand "create /n iqn.2020-05.example.com:target2" "Created" "TG> "
        client.RunCommand "select 1" "" "T > "
        client.RunCommand "set ID 2" "" "T > "
        client.RunCommand "attach /l 1" "Attach LU" "T > "
        client.RunCommand "attach /l 2" "Attach LU" "T > "
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

    interface ICollectionFixture<SCSI_PersistentReserveIn_Fixture>

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


[<Collection( "SCSI_PersistentReserveIn" )>]
type SCSI_PersistentReserveIn( fx : SCSI_PersistentReserveIn_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_CID1 = cid_me.fromPrim 1us
    let g_CID2 = cid_me.fromPrim 2us

    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL
    let g_LUN2 = lun_me.fromPrim 2UL

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

    let PR_ReadFullStatus ( r : SCSI_Initiator ) ( lun : LUN_T ) : Task<PR_ReadFullStatus> =
        task {
            let! itt_pr_in1 = r.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 3uy 512us NACA.T
            return! r.Wait_PersistentReserveIn_ReadFullStatus itt_pr_in1
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

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.ReadKeys_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T 
            let! res = r1.Wait_PersistentReserveIn_ReadKey itt
            Assert.True(( res.ReservationKey.Length = 0 ))

            do! r1.Close()
        }

    [<Fact>]
    member _.ReadKeys_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target2" } m_defaultConnParam
            let! r3 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2
            do! PR_Register r3 g_LUN1 g_ResvKey1

            let! itt_pr_in1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T 
            let! res_pr_in1 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in1
            let v = res_pr_in1.ReservationKey |> Array.sort
            Assert.True(( v.Length = 3 ))
            Assert.True(( v.[0] = g_ResvKey1 ))
            Assert.True(( v.[1] = g_ResvKey1 ))
            Assert.True(( v.[2] = g_ResvKey2 ))

            let! itt_pr_in2 = r2.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T 
            let! res_pr_in2 = r2.Wait_PersistentReserveIn_ReadKey itt_pr_in2
            Assert.True(( res_pr_in1 = res_pr_in2 ))

            let! itt_pr_in3 = r3.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T 
            let! res_pr_in3 = r3.Wait_PersistentReserveIn_ReadKey itt_pr_in3
            Assert.True(( res_pr_in1 = res_pr_in3 ))

            let! itt_pr_in4 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN2 0uy 256us NACA.T 
            let! res_pr_in4 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in4
            Assert.True(( res_pr_in4.ReservationKey.Length = 0 ))

            do! PR_Unregister r3 g_LUN1 g_ResvKey1
            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r2.Close()
            do! r3.Close()
        }

    [<Fact>]
    member _.ReadKeys_003 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2

            let! itt_pr_in1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T 
            let! res_pr_in1 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in1

            let! itt_pr_in2 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 4us NACA.T 
            let! res_pr_in2 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in2
            Assert.True(( res_pr_in2.PersistentReservationsGeneration = res_pr_in1.PersistentReservationsGeneration ))
            Assert.True(( res_pr_in2.AdditionalLength = 0u ))
            let v2 = res_pr_in2.ReservationKey |> Array.sort
            Assert.True(( v2.Length = 0 ))

            let! itt_pr_in3 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 8us NACA.T 
            let! res_pr_in3 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in3
            Assert.True(( res_pr_in3.PersistentReservationsGeneration = res_pr_in1.PersistentReservationsGeneration ))
            Assert.True(( res_pr_in3.AdditionalLength = 16u ))
            let v3 = res_pr_in3.ReservationKey |> Array.sort
            Assert.True(( v3.Length = 0 ))

            let! itt_pr_in4 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 12us NACA.T 
            let! res_pr_in4 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in4
            Assert.True(( res_pr_in4.PersistentReservationsGeneration = res_pr_in1.PersistentReservationsGeneration ))
            Assert.True(( res_pr_in4.AdditionalLength = 16u ))
            let v4 = res_pr_in4.ReservationKey |> Array.sort
            Assert.True(( v4.Length = 0 ))

            let! itt_pr_in5 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 16us NACA.T 
            let! res_pr_in5 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in5
            Assert.True(( res_pr_in5.PersistentReservationsGeneration = res_pr_in1.PersistentReservationsGeneration ))
            Assert.True(( res_pr_in5.AdditionalLength = 16u ))
            let v5 = res_pr_in5.ReservationKey |> Array.sort
            Assert.True(( v5.Length = 1 ))

            let! itt_pr_in6 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 20us NACA.T 
            let! res_pr_in6 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in6
            Assert.True(( res_pr_in6.PersistentReservationsGeneration = res_pr_in1.PersistentReservationsGeneration ))
            Assert.True(( res_pr_in6.AdditionalLength = 16u ))
            let v6 = res_pr_in6.ReservationKey |> Array.sort
            Assert.True(( v6.Length = 1 ))

            let! itt_pr_in7 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 24us NACA.T 
            let! res_pr_in7 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in7
            Assert.True(( res_pr_in7.PersistentReservationsGeneration = res_pr_in1.PersistentReservationsGeneration ))
            Assert.True(( res_pr_in7.AdditionalLength = 16u ))
            let v7 = res_pr_in7.ReservationKey |> Array.sort
            Assert.True(( v7.Length = 2 ))

            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r2.Close()
        }
