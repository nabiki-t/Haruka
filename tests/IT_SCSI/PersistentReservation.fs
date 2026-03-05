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
    let Wait_UpdateFile ( fname : string ) ( ftime : DateTime ) : unit =
        while ( ftime = File.GetLastWriteTimeUtc fname ) do
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

    let ClearReservationKey ( r : SCSI_Initiator ) ( lun : LUN_T ) ( key : RESVKEY_T ) : Task<unit> =
        task {
            // clear reservation key
            let! itt = r.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK lun NACA.T key
            let! _ = r.WaitSCSIResponseGoodStatus itt
            ()
        }

    static member Inquiry ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let! itt_msense = r.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0uy 256us NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member ModeSelect6 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let param : ModeParameter6 = {
                ModeDataLength = 0uy;
                MediumType = 0uy;
                WriteProtect = false;
                DisablePageOut_ForceUnitAccess = false;
                BlockDescriptorLength = 0uy;
                Block = None;
                Control = None;
                Cache = None;
                InformationalExceptionsControl = None;
            }
            let! itt_msense = r.Send_ModeSelect6 TaskATTRCd.SIMPLE_TASK lun PF.T SP.F param NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member ModeSelect10 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let param : ModeParameter10 = {
                ModeDataLength = 0us;
                MediumType = 0uy;
                WriteProtect = false;
                DisablePageOut_ForceUnitAccess = false;
                LongLBA = true;
                BlockDescriptorLength = 0us;
                Block = None;
                Control = None;
                Cache = None;
                InformationalExceptionsControl = None;
            }
            let! itt_msense = r.Send_ModeSelect10 TaskATTRCd.SIMPLE_TASK lun PF.T SP.F param NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member ModeSense6 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let! itt_msense = r.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F 0uy 0x0Auy 0x00uy 255uy NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member ModeSense10 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let! itt_msense = r.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun LLBAA.T DBD.F 0uy 0x0Auy 0x00uy 256us NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member PreFetch10 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let! itt_msense = r.Send_PreFetch10 TaskATTRCd.SIMPLE_TASK lun IMMED.F blkcnt_me.zero32 ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member PreFetch16 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let! itt_msense = r.Send_PreFetch16 TaskATTRCd.SIMPLE_TASK lun IMMED.F blkcnt_me.zero64 ( blkcnt_me.ofUInt32 1u ) NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member ReportLUNs ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let! itt_msense = r.Send_ReportLUNs TaskATTRCd.SIMPLE_TASK lun 0uy 256u NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member RequestSense ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let! itt_msense = r.Send_RequestSense TaskATTRCd.SIMPLE_TASK lun DESC.T 255uy NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member TestUnitReady ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let! itt_msense = r.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK lun NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member FormatUnit ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let! itt_msense = r.Send_FormatUnit TaskATTRCd.SIMPLE_TASK lun NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member Read6 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let bs = if Constants.MEDIA_BLOCK_SIZE = 512UL then Blocksize.BS_512 else Blocksize.BS_4096
            let! itt_msense = r.Send_Read6 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 bs ( blkcnt_me.ofUInt8 1uy ) NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member Read10 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let bs = if Constants.MEDIA_BLOCK_SIZE = 512UL then Blocksize.BS_512 else Blocksize.BS_4096
            let! itt_msense = r.Send_Read10 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 bs ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member Read12 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let bs = if Constants.MEDIA_BLOCK_SIZE = 512UL then Blocksize.BS_512 else Blocksize.BS_4096
            let! itt_msense = r.Send_Read12 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 bs ( blkcnt_me.ofUInt32 1u ) NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member Read16 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let bs = if Constants.MEDIA_BLOCK_SIZE = 512UL then Blocksize.BS_512 else Blocksize.BS_4096
            let! itt_msense = r.Send_Read16 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero64 bs ( blkcnt_me.ofUInt32 1u ) NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member ReadCapacity10 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let bs = if Constants.MEDIA_BLOCK_SIZE = 512UL then Blocksize.BS_512 else Blocksize.BS_4096
            let! itt_msense = r.Send_ReadCapacity10 TaskATTRCd.SIMPLE_TASK lun NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member ReadCapacity16 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let bs = if Constants.MEDIA_BLOCK_SIZE = 512UL then Blocksize.BS_512 else Blocksize.BS_4096
            let! itt_msense = r.Send_ReadCapacity16 TaskATTRCd.SIMPLE_TASK lun 256u NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member SynchronizeCache10 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let bs = if Constants.MEDIA_BLOCK_SIZE = 512UL then Blocksize.BS_512 else Blocksize.BS_4096
            let! itt_msense = r.Send_SynchronizeCache10 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 ( blkcnt_me.ofUInt16 1us ) NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member SynchronizeCache16 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let bs = if Constants.MEDIA_BLOCK_SIZE = 512UL then Blocksize.BS_512 else Blocksize.BS_4096
            let! itt_msense = r.Send_SynchronizeCache16 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero64 ( blkcnt_me.ofUInt32 1u ) NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member Write6 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let bs = if Constants.MEDIA_BLOCK_SIZE = 512UL then Blocksize.BS_512 else Blocksize.BS_4096
            let senddata = PooledBuffer.Rent ( int Constants.MEDIA_BLOCK_SIZE )
            let! itt_msense = r.Send_Write6 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 bs senddata NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member Write10 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let bs = if Constants.MEDIA_BLOCK_SIZE = 512UL then Blocksize.BS_512 else Blocksize.BS_4096
            let senddata = PooledBuffer.Rent ( int Constants.MEDIA_BLOCK_SIZE )
            let! itt_msense = r.Send_Write10 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 bs senddata NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member Write12 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let bs = if Constants.MEDIA_BLOCK_SIZE = 512UL then Blocksize.BS_512 else Blocksize.BS_4096
            let senddata = PooledBuffer.Rent ( int Constants.MEDIA_BLOCK_SIZE )
            let! itt_msense = r.Send_Write12 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 bs senddata NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member Write16 ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let bs = if Constants.MEDIA_BLOCK_SIZE = 512UL then Blocksize.BS_512 else Blocksize.BS_4096
            let senddata = PooledBuffer.Rent ( int Constants.MEDIA_BLOCK_SIZE )
            let! itt_msense = r.Send_Write16 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero64 bs senddata NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member ReportSupportedOperationCodes ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let! itt_msense = r.Send_ReportSupportedOperationCodes TaskATTRCd.SIMPLE_TASK lun 0uy 0uy 0us 256u NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member ReportSupportedTaskManagementFunctions ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let! itt_msense = r.Send_ReportSupportedTaskManagementFunctions TaskATTRCd.SIMPLE_TASK lun 256u NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
        }

    static member PersistentReserveIn ( r : SCSI_Initiator ) ( lun : LUN_T ) ( ex : ScsiCmdStatCd ) : Task<unit> =
        task {
            let! itt_msense = r.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK lun 0uy 256us NACA.T
            let! resp_cmd = r.WaitSCSIResponse itt_msense
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))
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
            let fexist =
                if File.Exists prfname then
                    Some( File.GetLastWriteTimeUtc prfname )
                else
                    None

            // register reservation key
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1
            if fexist.IsSome then
                Wait_UpdateFile prfname fexist.Value
            else
                Wait_CreateFile prfname

            // LU Reset
            let! itt_tmf = r1.SendTMFRequest_LogicalUnitReset BitI.F g_LUN1
            let! res_tmf = r1.WaitTMFResponse itt_tmf
            Assert.True(( res_tmf = TaskMgrResCd.FUNCTION_COMPLETE ))

            // Get reservarion key
            let! res_pr_in1 = PR_ReadKey r1 g_LUN1
            Assert.True(( res_pr_in1.ReservationKey = [| resvkey |] ))

            do! ClearReservationKey r1 g_LUN1 resvkey
            do! r1.Close()
        }
        
    // PR will not be lost due to Target Device reset.
    [<Fact>]
    member _.PersistReservation_TargetDeviceReset_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey = resvkey_me.fromPrim 1UL
            let prfname = GetPRFileName g_LUN1
            let fexist =
                if File.Exists prfname then
                    Some( File.GetLastWriteTimeUtc prfname )
                else
                    None

            // register reservation key
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1
            if fexist.IsSome then
                Wait_UpdateFile prfname fexist.Value
            else
                Wait_CreateFile prfname

            // Target device reset
            let! _ = r1.SendTMFRequest_LogicalUnitReset BitI.F g_LUN0
            do! checkDisconnected r1

            let! r2 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = r1.SessionParams.ISID } m_defaultConnParam

            // Get reservarion key
            let! res_pr_in1 = PR_ReadKey r2 g_LUN1
            Assert.True(( res_pr_in1.ReservationKey = [| resvkey |] ))

            do! ClearReservationKey r2 g_LUN1 resvkey
            do! r2.Close()
        }
        
    // For the same initiator port and LU, a unique reservation key is required for each I_T nexus.
    [<Fact>]
    member _.DuplicateResvKey_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey = resvkey_me.fromPrim 1UL

            // register reservation key
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1
            do! r1.Close()

            // Reconnect with the same ISID
            let! r2 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = r1.SessionParams.ISID } m_defaultConnParam

            // register reservation key ( failed )
            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey false false true [||]
            let! resp_2 = r2.WaitSCSIResponse itt_pr_out2
            Assert.True(( resp_2.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            do! ClearReservationKey r2 g_LUN1 resvkey
            do! r2.Close()
        }

    // The same reservation key can be used on different initiator ports.
    [<Fact>]
    member _.DuplicateResvKey_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey = resvkey_me.fromPrim 1UL

            // register reservation key
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // Reconnect with a different ISID
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // register reservation key. ( success )
            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey false false true [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2
            do! ClearReservationKey r2 g_LUN1 resvkey
            do! r2.Close()
        }

    // The same reservation key can be used on different LU.
    [<Fact>]
    member _.DuplicateResvKey_003 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey = resvkey_me.fromPrim 1UL

            // register reservation key LU1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // register reservation key LU0
            let! itt_pr_out2 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN0 NACA.T resvkey_me.zero resvkey false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            do! ClearReservationKey r1 g_LUN1 resvkey
            do! ClearReservationKey r1 g_LUN0 resvkey
            do! r1.Close()
        }

    // The same reservation key can be used by two I_T Nexuses through different targets.
    [<Fact>]
    member _.DuplicateResvKey_004 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let sessParam = {
                m_defaultSessParam with
                    TargetName = "iqn.2020-05.example.com:target2";
                    ISID = r1.SessionParams.ISID;
            }
            let! r2 = SCSI_Initiator.CreateWithISID sessParam m_defaultConnParam
            let resvkey = resvkey_me.fromPrim 1UL

            // register reservation key on session r1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // register reservation key on session r2
            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey false false true [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2

            // unregister on session r1
            let! itt_pr_out3 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey resvkey_me.zero false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            // unregister on session r2
            let! itt_pr_out4 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey resvkey_me.zero false false true [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out4

            do! r1.Close()
            do! r2.Close()
        }

    static member PersistentReservation_000_data : obj[][] = [|
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
                                                                                                            
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
                                                                                                                                
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
                                                                                                                                
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
                                                                                                                                
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
                                                                                                                                
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
                                                                                                                                
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
                                                                                                                                
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
                                                                                                                                
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
                                                                                                                                
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
                                                                                                                                
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
                                                                                                                                
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
    |]

    [<Theory>]
    [<MemberData( "PersistentReservation_000_data" )>]
    member _.PersistentReservation_000 ( regist : bool ) ( prtype : PR_TYPE )  ( func : ( SCSI_Initiator -> LUN_T -> ScsiCmdStatCd -> Task<unit> ) ) ( exp : ScsiCmdStatCd ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL
            let resvkey2 = resvkey_me.fromPrim 2UL

            // register reservation key on session 1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey1 false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // register reservation key on session 2
            if regist then
                let! itt_pr_out3 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey2 false false true [||]
                let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out3
                ()

            do! func r2 g_LUN1 exp

            // unregister reservation key on session 2
            if regist then
                let! itt_pr_out5 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey2 resvkey_me.zero false false true [||]
                let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out5
                ()

            // release reservation
            let! itt_pr_out4 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out4

            // unregister reservation key on session 1
            let! itt_pr_out6 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey1 resvkey_me.zero false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

            do! r1.Close()
            do! r2.Close()
        }

    static member PersistentReserveOut_CLEAR_001_data : obj[][] = [|
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  ScsiCmdStatCd.GOOD; |];
    |]

    [<Theory>]
    [<MemberData( "PersistentReserveOut_CLEAR_001_data" )>]
    member _.PersistentReserveOut_CLEAR_001 ( regist : bool ) ( prtype : PR_TYPE ) ( exp : ScsiCmdStatCd ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL
            let resvkey2 = resvkey_me.fromPrim 2UL

            // register reservation key on session 1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey1 false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // register reservation key on session 2
            if regist then
                let! itt_pr_out3 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey2 false false true [||]
                let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out3
                ()

            let! itt_pr_out4 = r2.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey2
            let! resp_cmd = r2.WaitSCSIResponse itt_pr_out4
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = exp ))

            if resp_cmd.Status = ScsiCmdStatCd.GOOD then
                // clear unit attention on session 1
                let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
                let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
                Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))
            else
                // unregister reservation key on session 2
                if regist then
                    let! itt_pr_out5 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey2 resvkey_me.zero false false true [||]
                    let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out5
                    ()

                // release reservation
                let! itt_pr_out6 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

                // unregister reservation key on session 1
                let! itt_pr_out7 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey1 resvkey_me.zero false false true [||]
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out7
                ()

            do! r1.Close()
            do! r2.Close()
        }

    static member PersistentReserveOut_PREEMPT_001_data : obj[][] = [|
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   1UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  1UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  1UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   0UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; 1UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  0UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   1UL; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  1UL; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  1UL; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   0UL; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; 1UL; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  0UL; ScsiCmdStatCd.GOOD; |];
    |]


    [<Theory>]
    [<MemberData( "PersistentReserveOut_PREEMPT_001_data" )>]
    member _.PersistentReserveOut_PREEMPT_001 ( regist : bool ) ( prtype : PR_TYPE ) ( sark : uint64 ) ( exp : ScsiCmdStatCd ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL
            let resvkey2 = resvkey_me.fromPrim 2UL
            let srResvKey = resvkey_me.fromPrim sark

            // register reservation key on session 1
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey1 false false true [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // register reservation key on session 2
            if regist then
                let! itt_pr_out3 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey2 false false true [||]
                let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out3
                ()

            // PREEMPT
            let! itt_pr_out4 = r2.Send_PROut_PREEMPT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE resvkey2 srResvKey
            let! resp_cmd = r2.WaitSCSIResponse itt_pr_out4
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = exp ))

            // clear PR
            if regist then
                do! ClearReservationKey r2 g_LUN1 resvkey2
                let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
                let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
                Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))
            else
                do! ClearReservationKey r1 g_LUN1 resvkey1

            do! r1.Close()
            do! r2.Close()
        }
