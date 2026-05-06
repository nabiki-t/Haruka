//=============================================================================
// Haruka Software Storage.
// PersistentReservation1.fs : Test cases for Persistent Reservation behavior.
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

[<CollectionDefinition( "SCSI_PersistentReservation1" )>]
type SCSI_PersistentReservation1_Fixture() =

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

    interface ICollectionFixture<SCSI_PersistentReservation1_Fixture>

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


[<Collection( "SCSI_PersistentReservation1" )>]
type SCSI_PersistentReservation( fx : SCSI_PersistentReservation1_Fixture ) =

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
        TaskReporting = TaskReportingType.TR_ResponseFence;
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

    let PR_Register ( r : SCSI_Initiator ) ( lun : LUN_T ) ( rsvkey : RESVKEY_T ) ( srrsvkey : RESVKEY_T ) : Task<unit> =
        task {
            let! itt_pr_out1 = r.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK lun NACA.T rsvkey srrsvkey SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r.WaitSCSIResponseGoodStatus itt_pr_out1
            ()
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
                    Some( GlbFunc.GetFileHash prfname )
                else
                    None

            // register reservation key
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey
            if fexist.IsSome then
                GlbFunc.WaitForFileUpdateByHash prfname fexist.Value
            else
                GlbFunc.WaitForFileCreate prfname

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
                    Some( GlbFunc.GetFileHash prfname )
                else
                    None

            // register reservation key
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey
            if fexist.IsSome then
                GlbFunc.WaitForFileUpdateByHash prfname fexist.Value
            else
                GlbFunc.WaitForFileCreate prfname

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
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey
            do! r1.Close()

            // Reconnect with the same ISID
            let! r2 = SCSI_Initiator.CreateWithISID { m_defaultSessParam with ISID = r1.SessionParams.ISID } m_defaultConnParam

            // register reservation key ( failed )
            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero resvkey SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
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
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey

            // Reconnect with a different ISID
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // register reservation key. ( success )
            do! PR_Register r2 g_LUN1 resvkey_me.zero resvkey

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
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey

            // register reservation key LU0
            do! PR_Register r1 g_LUN0 resvkey_me.zero resvkey

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
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey

            // register reservation key on session r2
            do! PR_Register r2 g_LUN1 resvkey_me.zero resvkey

            // unregister on session r1
            do! PR_Register r1 g_LUN1 resvkey resvkey_me.zero

            // unregister on session r2
            do! PR_Register r2 g_LUN1 resvkey resvkey_me.zero

            do! r1.Close()
            do! r2.Close()
        }

    static member PR_Effective_000_data : obj[][] = [|
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
    [<MemberData( "PR_Effective_000_data" )>]
    member _.PR_Effective_000 ( regist : bool ) ( prtype : PR_TYPE )  ( func : ( SCSI_Initiator -> LUN_T -> ScsiCmdStatCd -> Task<unit> ) ) ( exp : ScsiCmdStatCd ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL
            let resvkey2 = resvkey_me.fromPrim 2UL

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // register reservation key on session 2
            if regist then
                do! PR_Register r2 g_LUN1 resvkey_me.zero resvkey2

            do! func r2 g_LUN1 exp

            // unregister reservation key on session 2
            if regist then
                do! PR_Register r2 g_LUN1 resvkey2 resvkey_me.zero

            // release reservation
            let! itt_pr_out4 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out4

            // unregister reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey1 resvkey_me.zero

            do! r1.Close()
            do! r2.Close()
        }

    static member PR_Effective_001_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE;                   SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];

        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];

        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];

        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];

        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];

        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Inquiry;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ModeSelect6;                            ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ModeSelect10;                           ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ModeSense6;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ModeSense10;                            ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.PreFetch10;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.PreFetch16;                             ScsiCmdStatCd.CONDITION_MET; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReportLUNs;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.RequestSense;                           ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.TestUnitReady;                          ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.FormatUnit;                             ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Read6;                                  ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Read10;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Read12;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Read16;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReadCapacity10;                         ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReadCapacity16;                         ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.SynchronizeCache10;                     ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.SynchronizeCache16;                     ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Write6;                                 ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Write10;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Write12;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.Write16;                                ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReportSupportedOperationCodes;          ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.ReportSupportedTaskManagementFunctions; ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  SCSI_PersistentReservation.PersistentReserveIn;                    ScsiCmdStatCd.GOOD; |];
    |]

    [<Theory>]
    [<MemberData( "PR_Effective_001_data" )>]
    member _.PR_Effective_001 ( prtype : PR_TYPE )  ( func : ( SCSI_Initiator -> LUN_T -> ScsiCmdStatCd -> Task<unit> ) ) ( exp : ScsiCmdStatCd ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            do! func r1 g_LUN1 exp

            // release reservation
            let! itt_pr_out4 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out4

            // unregister reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey1 resvkey_me.zero

            do! r1.Close()
        }

    static member PR_Effective_PROut_CLEAR_001_data : obj[][] = [|
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
    [<MemberData( "PR_Effective_PROut_CLEAR_001_data" )>]
    member _.PR_Effective_PROut_CLEAR_001 ( regist : bool ) ( prtype : PR_TYPE ) ( exp : ScsiCmdStatCd ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL
            let resvkey2 = resvkey_me.fromPrim 2UL

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // register reservation key on session 2
            if regist then
                do! PR_Register r2 g_LUN1 resvkey_me.zero resvkey2

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
                    do! PR_Register r2 g_LUN1 resvkey2 resvkey_me.zero

                // release reservation
                let! itt_pr_out6 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

                // unregister reservation key on session 1
                do! PR_Register r1 g_LUN1 resvkey1 resvkey_me.zero

            do! r1.Close()
            do! r2.Close()
        }

    static member PR_Effective_PROut_CLEAR_002_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  ScsiCmdStatCd.GOOD; |];
    |]

    [<Theory>]
    [<MemberData( "PR_Effective_PROut_CLEAR_002_data" )>]
    member _.PR_Effective_PROut_CLEAR_002 ( prtype : PR_TYPE ) ( exp : ScsiCmdStatCd ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            let! itt_pr_out4 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey1
            let! resp_cmd = r1.WaitSCSIResponse itt_pr_out4
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = exp ))

            do! r1.Close()
        }

    static member PR_Effective_PROut_PREEMPT_001_data : obj[][] = [|
        [| false; false; PR_TYPE.WRITE_EXCLUSIVE;                   1UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; false; PR_TYPE.EXCLUSIVE_ACCESS;                  1UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  1UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   0UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; 1UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  0UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| false; true;  PR_TYPE.WRITE_EXCLUSIVE;                   1UL; ScsiCmdStatCd.GOOD; |];
        [| false; true;  PR_TYPE.EXCLUSIVE_ACCESS;                  1UL; ScsiCmdStatCd.GOOD; |];
        [| false; true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  1UL; ScsiCmdStatCd.GOOD; |];
        [| false; true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   0UL; ScsiCmdStatCd.GOOD; |];
        [| false; true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; 1UL; ScsiCmdStatCd.GOOD; |];
        [| false; true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  0UL; ScsiCmdStatCd.GOOD; |];
        [| true;  false; PR_TYPE.WRITE_EXCLUSIVE;                   1UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  false; PR_TYPE.EXCLUSIVE_ACCESS;                  1UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  1UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   0UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; 1UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  0UL; ScsiCmdStatCd.RESERVATION_CONFLICT; |];
        [| true;  true;  PR_TYPE.WRITE_EXCLUSIVE;                   1UL; ScsiCmdStatCd.GOOD; |];
        [| true;  true;  PR_TYPE.EXCLUSIVE_ACCESS;                  1UL; ScsiCmdStatCd.GOOD; |];
        [| true;  true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  1UL; ScsiCmdStatCd.GOOD; |];
        [| true;  true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   0UL; ScsiCmdStatCd.GOOD; |];
        [| true;  true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; 1UL; ScsiCmdStatCd.GOOD; |];
        [| true;  true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  0UL; ScsiCmdStatCd.GOOD; |];
    |]

    [<Theory>]
    [<MemberData( "PR_Effective_PROut_PREEMPT_001_data" )>]
    member _.PR_Effective_PROut_PREEMPT_001 ( func : bool ) ( regist : bool ) ( prtype : PR_TYPE ) ( sark : uint64 ) ( exp : ScsiCmdStatCd ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL
            let resvkey2 = resvkey_me.fromPrim 2UL
            let srResvKey = resvkey_me.fromPrim sark

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // register reservation key on session 2
            if regist then
                do! PR_Register r2 g_LUN1 resvkey_me.zero resvkey2

            // PREEMPT / PREEMPT_AND_ABORT
            let! itt_pr_out4 = 
                if func then
                    r2.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE resvkey2 srResvKey
                else
                    r2.Send_PROut_PREEMPT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE resvkey2 srResvKey
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

    static member PR_Effective_PROut_PREEMPT_002_data : obj[][] = [|
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   1UL; ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  1UL; ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  1UL; ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   0UL; ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; 1UL; ScsiCmdStatCd.GOOD; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  0UL; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   1UL; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  1UL; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  1UL; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   0UL; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; 1UL; ScsiCmdStatCd.GOOD; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  0UL; ScsiCmdStatCd.GOOD; |];
    |]

    [<Theory>]
    [<MemberData( "PR_Effective_PROut_PREEMPT_002_data" )>]
    member _.PR_Effective_PROut_PREEMPT_002 ( func : bool ) ( prtype : PR_TYPE ) ( sark : uint64 ) ( exp : ScsiCmdStatCd ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL
            let srResvKey = resvkey_me.fromPrim sark

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // PREEMPT / PREEMPT_AND_ABORT
            let! itt_pr_out4 = 
                if func then
                    r1.Send_PROut_PREEMPT_AND_ABORT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE resvkey1 srResvKey
                else
                    r1.Send_PROut_PREEMPT TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE resvkey1 srResvKey
            let! resp_cmd = r1.WaitSCSIResponse itt_pr_out4
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = exp ))

            // clear PR
            do! ClearReservationKey r1 g_LUN1 resvkey1

            do! r1.Close()
        }

    static member PR_Effective_PROut_REGISTER_001_data : obj[][] = [|
        [| false; false; PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| false; false; PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| false; false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| false; false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| false; false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| false; false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
        [| false; true;  PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| false; true;  PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| false; true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| false; true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| false; true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| false; true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
        [| true;  false; PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| true;  false; PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| true;  false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| true;  false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| true;  false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| true;  false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
        [| true;  true;  PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| true;  true;  PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| true;  true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| true;  true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| true;  true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| true;  true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
    |]

    [<Theory>]
    [<MemberData( "PR_Effective_PROut_REGISTER_001_data" )>]
    member _.PR_Effective_PROut_REGISTER_001 ( func : bool ) ( regist : bool ) ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL
            let resvkey2 = resvkey_me.fromPrim 2UL
            let resvkey3 = resvkey_me.fromPrim 3UL

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // register reservation key on session 2
            if regist then
                do! PR_Register r2 g_LUN1 resvkey_me.zero resvkey2

            // REGISTER / REGISTER_AND_IGNORE_EXISTING_KEY
            let! itt_pr_out4 =
                if func then
                    r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( if regist then resvkey2 else resvkey_me.zero ) resvkey3 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
                else
                    r2.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey3 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out4

            // unregister reservation key on session 2
            do! PR_Register r2 g_LUN1 resvkey3 resvkey_me.zero

            // release reservation
            let! itt_pr_out6 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

            // unregister reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey1 resvkey_me.zero

            do! r1.Close()
            do! r2.Close()
        }

    static member PR_Effective_PROut_REGISTER_002_data : obj[][] = [|
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
    |]

    [<Theory>]
    [<MemberData( "PR_Effective_PROut_REGISTER_002_data" )>]
    member _.PR_Effective_PROut_REGISTER_002 ( func : bool ) ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL
            let resvkey3 = resvkey_me.fromPrim 3UL

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // REGISTER / REGISTER_AND_IGNORE_EXISTING_KEY
            let! itt_pr_out4 =
                if func then
                    r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey1 resvkey3 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
                else
                    r1.Send_PROut_REGISTER_AND_IGNORE_EXISTING_KEY TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey3 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out4

            // release reservation
            let! itt_pr_out6 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey3
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

            // unregister reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey3 resvkey_me.zero

            do! r1.Close()
        }

    static member PR_Effective_PROut_REGISTER_AND_MOVE_001_data : obj[][] = [|
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
    |]

    [<Theory>]
    [<MemberData( "PR_Effective_PROut_REGISTER_AND_MOVE_001_data" )>]
    member _.PR_Effective_PROut_REGISTER_AND_MOVE_001 ( regist : bool ) ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL
            let resvkey2 = resvkey_me.fromPrim 2UL
            let resvkey3 = resvkey_me.fromPrim 3UL

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // register reservation key on session 2
            if regist then
                do! PR_Register r2 g_LUN1 resvkey_me.zero resvkey2

            // REGISTER_AND_MOVE
            let! itt_pr_out4 = r2.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS resvkey2 resvkey3 UNREG.T APTPL.T 0us ( "", None )
            let! resp_cmd = r2.WaitSCSIResponse itt_pr_out4
            resp_cmd.ResData.Return()
            if regist && PR_TYPE.isAllRegistrants prtype then
                Assert.True(( resp_cmd.Status = ScsiCmdStatCd.CHECK_CONDITION ))

                // clear ACA
                let! itt_tmf1 = r2.SendTMFRequest_ClearACA BitI.F g_LUN1
                let! res_tmf1 = r2.WaitTMFResponse itt_tmf1
                Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

            else
                Assert.True(( resp_cmd.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            // unregister reservation key on session 2
            if regist then
                do! PR_Register r2 g_LUN1 resvkey2 resvkey_me.zero

            // release reservation
            let! itt_pr_out6 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

            // unregister reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey1 resvkey_me.zero

            do! r1.Close()
            do! r2.Close()
        }

    static member PR_Effective_PROut_REGISTER_AND_MOVE_002_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
    |]

    [<Theory>]
    [<MemberData( "PR_Effective_PROut_REGISTER_AND_MOVE_002_data" )>]
    member _.PR_Effective_PROut_REGISTER_AND_MOVE_002 ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // REGISTER_AND_MOVE
            let! itt_pr_out4 = r1.Send_PROut_REGISTER_AND_MOVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.EXCLUSIVE_ACCESS resvkey1 resvkey_me.zero UNREG.T APTPL.T 0us ( "", None )
            let! resp_cmd = r1.WaitSCSIResponse itt_pr_out4
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // clear ACA
            let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))

            // release reservation
            let! itt_pr_out6 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

            // unregister reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey1 resvkey_me.zero

            do! r1.Close()
        }

    static member PR_Effective_PROut_RELEASE_001_data : obj[][] = [|
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
    [<MemberData( "PR_Effective_PROut_RELEASE_001_data" )>]
    member _.PR_Effective_PROut_RELEASE_001 ( regist : bool ) ( prtype : PR_TYPE ) ( ex : ScsiCmdStatCd ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL
            let resvkey2 = resvkey_me.fromPrim 2UL

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // register reservation key on session 2
            if regist then
                do! PR_Register r2 g_LUN1 resvkey_me.zero resvkey2

            // RELEASE
            let! itt_pr_out4 = r2.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey2
            let! resp_cmd = r2.WaitSCSIResponse itt_pr_out4
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))

            // unregister reservation key on session 2
            if regist then
                do! PR_Register r2 g_LUN1 resvkey2 resvkey_me.zero

            // get UA
            if regist && PR_TYPE.isAllRegistrants prtype then
                let! itt_read1 = r1.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
                let! res_read1 = r1.WaitSCSIResponse itt_read1
                Assert.True(( res_read1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( res_read1.Sense.Value.SenseKey = SenseKeyCd.UNIT_ATTENTION ))
                Assert.True(( res_read1.Sense.Value.ASC = ASCCd.RESERVATIONS_RELEASED ))

            // release reservation
            let! itt_pr_out6 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

            // unregister reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey1 resvkey_me.zero

            do! r1.Close()
            do! r2.Close()
        }

    static member PR_Effective_PROut_RELEASE_002_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; ScsiCmdStatCd.GOOD; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  ScsiCmdStatCd.GOOD; |];
    |]

    [<Theory>]
    [<MemberData( "PR_Effective_PROut_RELEASE_002_data" )>]
    member _.PR_Effective_PROut_RELEASE_002 ( prtype : PR_TYPE ) ( ex : ScsiCmdStatCd ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // RELEASE
            let! itt_pr_out4 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! resp_cmd = r1.WaitSCSIResponse itt_pr_out4
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ex ))

            // unregister reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey1 resvkey_me.zero

            do! r1.Close()
        }

    static member PR_Effective_PROut_RESERVE_001_data : obj[][] = [|
        [| false; PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| false; PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| false; PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| true;  PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| true;  PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
    |]
    [<Theory>]
    [<MemberData( "PR_Effective_PROut_RESERVE_001_data" )>]
    member _.PR_Effective_PROut_RESERVE_001 ( regist : bool ) ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL
            let resvkey2 = resvkey_me.fromPrim 2UL

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // register reservation key on session 2
            if regist then
                do! PR_Register r2 g_LUN1 resvkey_me.zero resvkey2

            // RESERVE
            let! itt_pr_out4 = r2.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T PR_TYPE.WRITE_EXCLUSIVE resvkey2
            let! resp_cmd = r2.WaitSCSIResponse itt_pr_out4
            resp_cmd.ResData.Return()
            Assert.True(( resp_cmd.Status = ScsiCmdStatCd.RESERVATION_CONFLICT ))

            // unregister reservation key on session 2
            if regist then
                do! PR_Register r2 g_LUN1 resvkey2 resvkey_me.zero

            // release reservation
            let! itt_pr_out6 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

            // unregister reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey1 resvkey_me.zero

            do! r1.Close()
            do! r2.Close()
        }

    static member PR_Effective_PROut_RESERVE_002_data : obj[][] = [|
        [| PR_TYPE.WRITE_EXCLUSIVE;                   |];
        [| PR_TYPE.EXCLUSIVE_ACCESS;                  |];
        [| PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;  |];
        [| PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;   |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY; |];
        [| PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;  |];
    |]
    [<Theory>]
    [<MemberData( "PR_Effective_PROut_RESERVE_002_data" )>]
    member _.PR_Effective_PROut_RESERVE_002 ( prtype : PR_TYPE ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let resvkey1 = resvkey_me.fromPrim 1UL

            // register reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey_me.zero resvkey1

            // reserve reservation
            let! itt_pr_out2 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2

            // RESERVE
            let! itt_pr_out4 = r1.Send_PROut_RESERVE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out4

            // release reservation
            let! itt_pr_out6 = r1.Send_PROut_RELEASE TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T prtype resvkey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out6

            // unregister reservation key on session 1
            do! PR_Register r1 g_LUN1 resvkey1 resvkey_me.zero

            do! r1.Close()
        }
