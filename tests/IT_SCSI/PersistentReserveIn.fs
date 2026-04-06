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

    let GetSortedISID ( cnt : int ) =
        Array.init cnt ( fun _ -> GlbFunc.newISID() )
        |> Array.sortBy isid_me.toPrim

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

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.ReadKeys_002 ( arglun : uint64 ) =
        task {
            let lun = lun_me.fromPrim arglun
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target2" } m_defaultConnParam
            let! r3 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            do! PR_Register r1 lun g_ResvKey1
            do! PR_Register r2 lun g_ResvKey2
            do! PR_Register r3 lun g_ResvKey1

            let! itt_pr_in1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 0uy 256us NACA.T 
            let! res_pr_in1 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in1
            let v = res_pr_in1.ReservationKey |> Array.sort
            Assert.True(( v.Length = 3 ))
            Assert.True(( v.[0] = g_ResvKey1 ))
            Assert.True(( v.[1] = g_ResvKey1 ))
            Assert.True(( v.[2] = g_ResvKey2 ))

            let! itt_pr_in2 = r2.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 0uy 256us NACA.T 
            let! res_pr_in2 = r2.Wait_PersistentReserveIn_ReadKey itt_pr_in2
            Assert.True(( res_pr_in1 = res_pr_in2 ))

            let! itt_pr_in3 = r3.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 0uy 256us NACA.T 
            let! res_pr_in3 = r3.Wait_PersistentReserveIn_ReadKey itt_pr_in3
            Assert.True(( res_pr_in1 = res_pr_in3 ))

            let! itt_pr_in4 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN2 0uy 256us NACA.T 
            let! res_pr_in4 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in4
            Assert.True(( res_pr_in4.ReservationKey.Length = 0 ))

            do! PR_Unregister r3 lun g_ResvKey1
            do! PR_Unregister r2 lun g_ResvKey2
            do! PR_Unregister r1 lun g_ResvKey1
            do! r1.Close()
            do! r2.Close()
            do! r3.Close()
        }

    [<Theory>]
    [<InlineData( 0us,  false, 0u,  0 )>]
    [<InlineData( 4us,  true,  0u,  0 )>]
    [<InlineData( 8us,  true,  16u, 0 )>]
    [<InlineData( 12us, true,  16u, 0 )>]
    [<InlineData( 16us, true,  16u, 1 )>]
    [<InlineData( 20us, true,  16u, 1 )>]
    [<InlineData( 24us, true,  16u, 2 )>]
    member _.ReadKeys_003 ( len : uint16 ) ( prg : bool ) ( al : uint32 ) ( cnt : int ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Register r2 g_LUN1 g_ResvKey2

            let! itt_pr_in1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T 
            let! res_pr_in1 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in1

            let! itt_pr_in2 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy len NACA.T 
            let! res_pr_in2 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in2
            if prg then
                Assert.True(( res_pr_in2.PersistentReservationsGeneration = res_pr_in1.PersistentReservationsGeneration ))
            else
                Assert.False(( res_pr_in2.PersistentReservationsGeneration = res_pr_in1.PersistentReservationsGeneration ))
            Assert.True(( res_pr_in2.AdditionalLength = al ))
            Assert.True(( res_pr_in2.ReservationKey.Length = cnt ))

            do! PR_Unregister r2 g_LUN1 g_ResvKey2
            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
            do! r2.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, true,  false, false )>]
    [<InlineData( 0UL, false, false, false )>]
    [<InlineData( 1UL, true,  true,  true  )>]
    [<InlineData( 1UL, false, true,  false )>]
    member _.ReportCapabilities_001 ( arglun : uint64 ) ( arg_aptpl : bool ) ( arg_ptpl_c : bool ) ( arg_ptpl_a : bool )  =
        task {
            let lun = lun_me.fromPrim arglun
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK lun NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F ( APTPL.ofBool arg_aptpl ) [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! itt1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 2uy 256us NACA.T 
            let! res1 = r1.Wait_PersistentReserveIn_ReportCapabilities itt1
            Assert.True(( res1.Length = 8us ))
            Assert.True(( res1.CompatibleReservationHandling = false ))
            Assert.True(( res1.SpecifyInitiatorPortCapable = true ))
            Assert.True(( res1.AllTargetPortsCapable = true ))
            Assert.True(( res1.PersistThroughPowerLossCapable = arg_ptpl_c ))
            Assert.True(( res1.TypeMaskValid = true ))
            Assert.True(( res1.PersistThroughPowerLossActivated = arg_ptpl_a ))
            Assert.True(( res1.WriteExclusive_AllRegistrants = true ))
            Assert.True(( res1.ExclusiveAccess_RegistrantsOnly = true ))
            Assert.True(( res1.WriteExclusive_RegistrantsOnly = true ))
            Assert.True(( res1.ExclusiveAccess = true ))
            Assert.True(( res1.WriteExclusive = true ))
            Assert.True(( res1.ExclusiveAccess_AllRegistrants = true ))

            do! PR_Unregister r1 lun g_ResvKey1
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0us, 0us )>]
    [<InlineData( 1us, 0us )>]
    [<InlineData( 2us, 8us )>]
    member _.ReportCapabilities_002 ( al : uint16 ) ( len : uint16 ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 2uy al NACA.T 
            let! res1 = r1.Wait_PersistentReserveIn_ReportCapabilities itt1
            Assert.True(( res1.Length = len ))

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.ReadReservation_001 ( arglun : uint64 ) =
        task {
            let lun = lun_me.fromPrim arglun
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 1uy 256us NACA.T 
            let! res1 = r1.Wait_PersistentReserveIn_ReadReservation itt1
            Assert.True(( res1.AdditionalLength = 0u ))

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.ReadReservation_002 ( arglun : uint64 ) =
        task {
            let lun = lun_me.fromPrim arglun
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            do! PR_Register r1 lun g_ResvKey1

            let! itt1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 1uy 256us NACA.T 
            let! res1 = r1.Wait_PersistentReserveIn_ReadReservation itt1
            Assert.True(( res1.AdditionalLength = 0u ))

            do! PR_Unregister r1 lun g_ResvKey1
            do! r1.Close()
        }

    static member ReadReservation_003_data : obj[][] = [|
        [| 0UL; PR_TYPE.WRITE_EXCLUSIVE;                   |]
        [| 0UL; PR_TYPE.EXCLUSIVE_ACCESS;                  |]
        [| 0UL; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| 0UL; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |]
        [| 0UL; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
        [| 0UL; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |]
        [| 1UL; PR_TYPE.WRITE_EXCLUSIVE;                   |]
        [| 1UL; PR_TYPE.EXCLUSIVE_ACCESS;                  |]
        [| 1UL; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| 1UL; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |]
        [| 1UL; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
        [| 1UL; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |]
    |]

    [<Theory>]
    [<MemberData( "ReadReservation_003_data" )>]
    member _.ReadReservation_003 ( arglun : uint64 ) ( prtype : PR_TYPE ) =
        task {
            let lun = lun_me.fromPrim arglun
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            do! PR_Register r1 lun g_ResvKey1
            do! PR_Reserve r1 lun prtype g_ResvKey1

            let! itt1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 1uy 256us NACA.T 
            let! res1 = r1.Wait_PersistentReserveIn_ReadReservation itt1
            Assert.True(( res1.AdditionalLength = 0x10u ))
            if PR_TYPE.isAllRegistrants prtype then
                Assert.True(( res1.ReservationKey = resvkey_me.zero ))
            else
                Assert.True(( res1.ReservationKey = g_ResvKey1 ))
            Assert.True(( res1.Scope = 0uy ))
            Assert.True(( res1.Type = PR_TYPE.toNumericValue prtype ))

            do! PR_Unregister r1 lun g_ResvKey1
            do! r1.Close()
        }

    [<Fact>]
    member _.ReadReservation_004 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Reserve r1 g_LUN1 PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1

            let! itt1 = r2.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 1uy 256us NACA.T 
            let! res1 = r2.Wait_PersistentReserveIn_ReadReservation itt1
            Assert.True(( res1.AdditionalLength = 0x10u ))
            Assert.True(( res1.ReservationKey = g_ResvKey1 ))
            Assert.True(( res1.Scope = 0uy ))
            Assert.True(( res1.Type = PR_TYPE.toNumericValue PR_TYPE.WRITE_EXCLUSIVE ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r2.Close()
            do! r1.Close()
        }

    [<Fact>]
    member _.ReadReservation_005 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Reserve r1 g_LUN1 PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1

            let! itt1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN2 1uy 256us NACA.T 
            let! res1 = r1.Wait_PersistentReserveIn_ReadReservation itt1
            Assert.True(( res1.AdditionalLength = 0x0u ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0us, false, 0u, 0UL )>]
    [<InlineData( 4us, true, 0u, 0UL )>]
    [<InlineData( 8us, true, 0x10u, 0UL )>]
    [<InlineData( 12us, true, 0x10u, 0UL )>]
    [<InlineData( 16us, true, 0x10u, 1UL )>]
    member _.ReadReservation_006 ( all : uint16 ) ( prg : bool ) ( adl : uint32 ) ( rk : uint64 ) =
        task {
            let resk = resvkey_me.fromPrim rk
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            do! PR_Register r1 g_LUN1 g_ResvKey1
            do! PR_Reserve r1 g_LUN1 PR_TYPE.WRITE_EXCLUSIVE g_ResvKey1
            let! fstat1 = PR_ReadFullStatus r1 g_LUN1

            let! itt1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 1uy all NACA.T 
            let! res1 = r1.Wait_PersistentReserveIn_ReadReservation itt1
            if prg then
                Assert.True(( res1.PersistentReservationsGeneration = fstat1.PersistentReservationsGeneration ))
            else
                Assert.True(( res1.PersistentReservationsGeneration = 0u ))
            Assert.True(( res1.AdditionalLength = adl ))
            Assert.True(( res1.ReservationKey = resk ))

            do! PR_Unregister r1 g_LUN1 g_ResvKey1
            do! r1.Close()
        }
        
    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.ReadFullStatus_001 ( arglun : uint64 ) =
        task {
            let lun = lun_me.fromPrim arglun
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 3uy 256us NACA.T 
            let! res1 = r1.Wait_PersistentReserveIn_ReadFullStatus itt1
            Assert.True(( res1.AdditionalLength = 0u ))
            Assert.True(( res1.FullStatusDescriptor.Length = 0 ))

            do! r1.Close()
        }
        
    static member ReadFullStatus_002_data : obj[][] = [|
        [| 0UL; PR_TYPE.WRITE_EXCLUSIVE;                   |]
        [| 0UL; PR_TYPE.EXCLUSIVE_ACCESS;                  |]
        [| 0UL; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| 0UL; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
        [| 1UL; PR_TYPE.WRITE_EXCLUSIVE;                   |]
        [| 1UL; PR_TYPE.EXCLUSIVE_ACCESS;                  |]
        [| 1UL; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |]
        [| 1UL; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |]
    |]

    [<Theory>]
    [<MemberData( "ReadFullStatus_002_data" )>]
    member _.ReadFullStatus_002 ( arglun : uint64 ) ( prtype : PR_TYPE ) =
        task {
            let lun = lun_me.fromPrim arglun
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let itn_r1 = r1.ITNexus
            do! PR_Register r1 lun g_ResvKey1
            do! PR_Reserve r1 lun prtype g_ResvKey1

            let! itt1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 3uy 256us NACA.T 
            let! res1 = r1.Wait_PersistentReserveIn_ReadFullStatus itt1
            Assert.True(( res1.FullStatusDescriptor.Length = 1 ))
            let v = res1.FullStatusDescriptor
            Assert.True(( v.[0].ReservationKey = g_ResvKey1 ))
            Assert.False(( v.[0].AllTargetPorts ))
            Assert.True(( v.[0].ReservationHolder ))
            Assert.True(( v.[0].Scope = 0uy ))
            Assert.True(( v.[0].Type = PR_TYPE.toNumericValue prtype ))
            Assert.True(( v.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( v.[0].FormatCode = 1uy ))
            Assert.True(( v.[0].ProtocalIdentifier = 5uy ))
            Assert.True(( v.[0].iSCSIName = itn_r1.InitiatorPortName ))

            do! PR_Unregister r1 lun g_ResvKey1
            do! r1.Close()
        }

    [<Theory>]
    [<MemberData( "ReadFullStatus_002_data" )>]
    member _.ReadFullStatus_003 ( arglun : uint64 ) ( prtype : PR_TYPE ) =
        task {
            let lun = lun_me.fromPrim arglun
            let isids = GetSortedISID 2
            let param1 = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target1";
                    ISID = isids.[0];
            }
            let param2 = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2";
                    ISID = isids.[1];
            }
            let! r1 = SCSI_Initiator.CreateWithISID param1 m_defaultConnParam
            let! r2 = SCSI_Initiator.CreateWithISID param2 m_defaultConnParam
            let itn_r1 = r1.ITNexus
            let itn_r2 = r2.ITNexus

            do! PR_Register r1 lun g_ResvKey1
            do! PR_Register r2 lun g_ResvKey2
            do! PR_Reserve r1 lun prtype g_ResvKey1

            let! itt1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 3uy 256us NACA.T 
            let! res1 = r1.Wait_PersistentReserveIn_ReadFullStatus itt1
            Assert.True(( res1.FullStatusDescriptor.Length = 2 ))
            let v = res1.FullStatusDescriptor |> Array.sortBy _.iSCSIName

            Assert.True(( v.[0].ReservationKey = g_ResvKey1 ))
            Assert.False(( v.[0].AllTargetPorts ))
            Assert.True(( v.[0].ReservationHolder ))
            Assert.True(( v.[0].Scope = 0uy ))
            Assert.True(( v.[0].Type = PR_TYPE.toNumericValue prtype ))
            Assert.True(( v.[0].RelativeTargetPortIdentifier = 1us ))
            Assert.True(( v.[0].FormatCode = 1uy ))
            Assert.True(( v.[0].ProtocalIdentifier = 5uy ))
            Assert.True(( v.[0].iSCSIName = itn_r1.InitiatorPortName ))

            Assert.True(( v.[1].ReservationKey = g_ResvKey2 ))
            Assert.False(( v.[1].AllTargetPorts ))
            Assert.False(( v.[1].ReservationHolder ))
            Assert.True(( v.[1].Scope = 0uy ))
            Assert.True(( v.[1].Type = PR_TYPE.toNumericValue PR_TYPE.NO_RESERVATION ))
            Assert.True(( v.[1].RelativeTargetPortIdentifier = 2us ))
            Assert.True(( v.[1].FormatCode = 1uy ))
            Assert.True(( v.[1].ProtocalIdentifier = 5uy ))
            Assert.True(( v.[1].iSCSIName = itn_r2.InitiatorPortName ))

            do! PR_Unregister r2 lun g_ResvKey2
            do! PR_Unregister r1 lun g_ResvKey1
            do! r1.Close()
        }

