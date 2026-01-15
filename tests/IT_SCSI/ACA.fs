//=============================================================================
// Haruka Software Storage.
// ACA.fs : Test cases to verify the behavior of CA and ACA as specified in SAM-2 5.9.1.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.IT.SCSI

//=============================================================================
// Import declaration

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Threading
open System.Threading.Tasks

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.BlockDeviceLU
open Haruka.Client
open Haruka.Test

//=============================================================================
// Class implementation


[<CollectionDefinition( "SCSI_ACACases" )>]
type SCSI_ACACases_Fixture() =

    let m_iSCSIPortNo = GlbFunc.nextTcpPortNo()
    let m_MediaSize = 65536u

    // Add default configurations
    let AddDefaultConf( client : ClientProc ): unit =

        ///////////////////////////////
        // Target Device 0

        // Target device, Target group
        client.RunCommand "create" "Created" "CR> "
        client.RunCommand "select 0" "" "TD> "
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

        client.RunCommand "validate" "All configurations are vlidated" "MD> "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "MD> "
        client.RunCommand "start" "Started" "MD> "

    // Start controller and client
    let m_Controller, m_Client =
        let workPath =
            let tempPath = Path.GetTempPath()
            Functions.AppendPathName tempPath ( Guid.NewGuid().ToString( "N" ) )
        let controllPortNo = GlbFunc.nextTcpPortNo()
        let controller, client = ControllerFunc.StartHarukaController workPath controllPortNo
        AddDefaultConf client
        controller, client

    interface IDisposable with
        member _.Dispose (): unit =
            m_Client.Kill()

    interface ICollectionFixture<SCSI_ACACases_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.iSCSIPortNo = m_iSCSIPortNo
    member _.MediaSize = m_MediaSize
    member _.MediaBlockSize = 
        if Constants.MEDIA_BLOCK_SIZE = 512UL then     // 4096 or 512 bytes
            Blocksize.BS_512
        else
            Blocksize.BS_4096

[<Collection( "SCSI_ACACases" )>]
type SCSI_ACACases( fx : SCSI_ACACases_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL
    let iSCSIPortNo = fx.iSCSIPortNo
    let m_MediaSize = fx.MediaSize
    let m_MediaBlockSize = fx.MediaBlockSize
    let m_ClientProc = fx.clientProc

    // default session parameters
    let m_defaultSessParam = {
        InitiatorName = "iqn.2020-05.example.com:initiator";
        InitiatorAlias = "aaa";
        TargetName = "iqn.2020-05.example.com:target1";
        TargetAlias = "";
        ISID = isid_me.fromPrim 1UL;
        TSIH = tsih_me.fromPrim 0us;
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

    // Get a list of tasks that are stalled by the debug media wait action.
    let GetStuckTasks() : ( string * TSIH_T * ITT_T ) array =
        let rx = Regex( "^ *([^ ]*) *\( *TSIH *= *([0-9]*) *, *ITT *= *([0-9]*) *\) *$" )
        m_ClientProc.RunCommandGetResp "task list" "MD> "
        |> Array.choose( fun itr ->
            let m = rx.Match itr
            if not m.Success then
                None
            else
                let method = m.Groups.[1].Value |> _.ToUpperInvariant()
                let tsih = m.Groups.[2].Value |> UInt16.Parse |> tsih_me.fromPrim
                let itt = m.Groups.[3].Value |> UInt32.Parse |> itt_me.fromPrim
                Some( method, tsih, itt )
        )


    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.CheckStanderdInquiry_NormACA_001() =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.F 0uy 128us NACA.T
            let! result = r.Wait_Inquiry_Standerd itt
            Assert.True(( result.NormalACASupported ))  // Haruka supports ACA
            do! r.Close()
        }

    [<Fact>]
    member _.SenseData_FixedFormat_ACA_001() =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // Set Sense Data to use fixed format.
            let! itt_msense = r.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK g_LUN1 LLBAA.T DBD.F 0uy 0x0Auy 0x00uy 256us NACA.T
            let! res_msense = r.Wait_ModeSense10 itt_msense
            Assert.True(( res_msense.Control.IsSome ))
            let param = {
                res_msense with
                    Control = Some({
                        res_msense.Control.Value with
                            DescriptorFormatSenseData = false
                    })
            }
            let! itt_mselect = r.Send_ModeSelect10 TaskATTRCd.SIMPLE_TASK g_LUN1 PF.T SP.T param NACA.T
            let! _ = r.WaitSCSIResponseGoogStatus itt_mselect

            // raise ACA
            let! itt = r.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0xFFFFFFFFu ) m_MediaBlockSize ( blkcnt_me.ofUInt16 0xFFFFus ) NACA.T
            let! result = r.WaitSCSIResponse itt
            Assert.True(( result.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( result.Sense.IsSome ))
            Assert.True(( result.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( result.Sense.Value.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            Assert.True(( result.Sense.Value.BlockCommand.IsSome ))         // There is no useful information
            Assert.True(( result.Sense.Value.Information.IsNone ))
            Assert.True(( result.Sense.Value.CommandSpecific.IsSome ))      // There is no useful information
            Assert.True(( result.Sense.Value.FieldReplaceableUnit.IsSome )) // There is no useful information
            Assert.True(( result.Sense.Value.FieldPointer.IsNone ))
            Assert.True(( result.Sense.Value.ActualRetryCount.IsNone ))
            Assert.True(( result.Sense.Value.ProgressIndication.IsNone ))
            Assert.True(( result.Sense.Value.SegmentPointer.IsNone ))
            Assert.True(( result.Sense.Value.VendorSpecific.IsSome ))   // message

            // Clear ACA
            let! itt2 = r.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! result2 = r.WaitTMFResponse itt2
            Assert.True(( result2 = TaskMgrResCd.FUNCTION_COMPLETE ))

            do! r.Close()
        }

    [<Fact>]
    member _.SenseData_DescriptorFormat_ACA_001() =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // Set Sense Data to use descriptor format.
            let! itt_msense = r.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK g_LUN1 LLBAA.T DBD.F 0uy 0x0Auy 0x00uy 256us NACA.T
            let! res_msense = r.Wait_ModeSense10 itt_msense
            Assert.True(( res_msense.Control.IsSome ))
            let param = {
                res_msense with
                    Control = Some({
                        res_msense.Control.Value with
                            DescriptorFormatSenseData = true
                    })
            }
            let! itt_mselect = r.Send_ModeSelect10 TaskATTRCd.SIMPLE_TASK g_LUN1 PF.T SP.T param NACA.T
            let! _ = r.WaitSCSIResponseGoogStatus itt_mselect

            // raise ACA
            let! itt = r.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0xFFFFFFFFu ) m_MediaBlockSize ( blkcnt_me.ofUInt16 0xFFFFus ) NACA.T
            let! result = r.WaitSCSIResponse itt
            Assert.True(( result.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( result.Sense.IsSome ))
            Assert.True(( result.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( result.Sense.Value.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))
            Assert.True(( result.Sense.Value.BlockCommand.IsNone ))
            Assert.True(( result.Sense.Value.Information.IsNone ))
            Assert.True(( result.Sense.Value.CommandSpecific.IsNone ))
            Assert.True(( result.Sense.Value.FieldReplaceableUnit.IsNone ))
            Assert.True(( result.Sense.Value.FieldPointer.IsNone ))
            Assert.True(( result.Sense.Value.ActualRetryCount.IsNone ))
            Assert.True(( result.Sense.Value.ProgressIndication.IsNone ))
            Assert.True(( result.Sense.Value.SegmentPointer.IsNone ))
            Assert.True(( result.Sense.Value.VendorSpecific.IsSome ))   // message

            // Clear ACA
            let! itt2 = r.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! result2 = r.WaitTMFResponse itt2
            Assert.True(( result2 = TaskMgrResCd.FUNCTION_COMPLETE ))

            do! r.Close()
        }
        
    /// When an ACA occurs, running tasks from all initiator ports will continue to run to completion 
    /// (Haruka does not support the function to make them blocking tasks).
    /// When an ACA occurs, dormant tasks from all initiator ports will remain dormant state.
    /// See SAM-2 5.9.1.2 and 7.7.2
    [<Fact>]
    member _.ModeParameter_Qerr_TST_Behavior_001() =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let writeData1 = PooledBuffer.Rent( Blocksize.toUInt32 m_MediaBlockSize |> int32 )
            let writeData2 = PooledBuffer.Rent( Blocksize.toUInt32 m_MediaBlockSize |> int32 )
            let writeData3 =
                let v = PooledBuffer.Rent( Blocksize.toUInt32 m_MediaBlockSize |> int32 )
                Random.Shared.NextBytes( v.ArraySegment.AsSpan() )
                v
            let writeData4 =
                let v = PooledBuffer.Rent( Blocksize.toUInt32 m_MediaBlockSize |> int32 )
                Random.Shared.NextBytes( v.ArraySegment.AsSpan() )
                v

            // Check mode parameter value
            let! itt_msense = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK g_LUN1 LLBAA.T DBD.F 0uy 0x0Auy 0x00uy 256us NACA.T
            let! res_msense = r1.Wait_ModeSense10 itt_msense
            Assert.True(( res_msense.Control.IsSome ))
            Assert.True(( res_msense.Control.Value.QueueErrorManagement = 0uy ))
            Assert.True(( res_msense.Control.Value.TaskSetType = 0uy ))

            // Add debug media trap
            m_ClientProc.RunCommand "add trap /e Write /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Write /slba 3 /elba 3 /a ACA" "Trap added" "MD> "

            // Write request 1 at session 1 ( it raise ACA )
            let! itt_s1_w1 = r1.Send_Write10 TaskATTRCd.HEAD_OF_QUEUE_TASK g_LUN1 ( blkcnt_me.ofUInt32 3u ) m_MediaBlockSize writeData1 NACA.T

            // Confirm that above task is stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 10

            // Read request 1 at session 1 ( Enters a dormant task state )
            let! itt_s1_r1 = r1.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Read request 2 at session 2 ( Enters a dormant task state )
            let! itt_s2_r2 = r2.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 1u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Just in case, wait until the simple task is queued
            do! Task.Delay 100

            // Write request 3 at session 1 ( Overtake SIMPLE tasks )
            let! itt_s1_w3 = r1.Send_Write10 TaskATTRCd.HEAD_OF_QUEUE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize writeData3 NACA.T

            // Write request 4 at session 2 ( Overtake SIMPLE tasks )
            let! itt_s2_w4 = r2.Send_Write10 TaskATTRCd.HEAD_OF_QUEUE_TASK g_LUN1 ( blkcnt_me.ofUInt32 1u ) m_MediaBlockSize writeData4 NACA.T

            // Confirm that 3 tasks are stuck
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 3 ) do
                do! Task.Delay 10
            let tasklist1 = GetStuckTasks()
            Assert.True(( tasklist1.Length = 3 ))
            Assert.True(( Array.exists( (=) ( "WRITE", r1.TSIH, itt_s1_w1 ) ) tasklist1 ))
            Assert.True(( Array.exists( (=) ( "WRITE", r1.TSIH, itt_s1_w3 ) ) tasklist1 ))
            Assert.True(( Array.exists( (=) ( "WRITE", r2.TSIH, itt_s2_w4 ) ) tasklist1 ))

            // Resume write request 1, which should have failed.
            // Read requests 1 and 2 should be able to be executed once the previously submitted Head of Queue task ( write request 1 ) is completed, 
            // but because that task failed, they cannot be resumed.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_s1_w1 ) "Task(" "MD> "

            // Check result the write request 1.
            let! res_s1_w1 = r1.WaitSCSIResponse itt_s1_w1
            Assert.True(( res_s1_w1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // Send acknowledgement.
            do! r1.Send_StatusACK()

            // Resume write requests 3 and 4. They should complete successfully.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_s1_w3 ) "Task(" "MD> "
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r2.TSIH itt_s2_w4 ) "Task(" "MD> "

            // Check result the write request 3 and 4.
            let! _ = r1.WaitSCSIResponseGoogStatus itt_s1_w3
            let! _ = r2.WaitSCSIResponseGoogStatus itt_s2_w4

            // There should be no stuck tasks.
            Assert.True(( ( GetStuckTasks() ).Length = 0 ))

            // Clear ACA. This resumes Read requests 1 and 2, which were in a paused state.
            let! itt2 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! result2 = r1.WaitTMFResponse itt2
            Assert.True(( result2 = TaskMgrResCd.FUNCTION_COMPLETE ))

            do! r1.Send_StatusACK()

            // Receive the results of read requests 1 and 2.
            let! res_s1_r1 = r1.WaitSCSIResponseGoogStatus itt_s1_r1
            let! res_s2_r2 = r2.WaitSCSIResponseGoogStatus itt_s2_r2

            // Verify that the write data has been read by Write requests 3 and 4, which were executed earlier.
            Assert.True(( PooledBuffer.ValueEquals res_s1_r1 writeData3 ))
            Assert.True(( PooledBuffer.ValueEquals res_s2_r2 writeData4 ))

            // Clear debug media traps
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "

            do! r1.Close()
            do! r2.Close()
            writeData1.Return()
            writeData2.Return()
        }

    [<Theory>]
    [<InlineData( false, TaskATTRCd.HEAD_OF_QUEUE_TASK, true )>]
    [<InlineData( false, TaskATTRCd.HEAD_OF_QUEUE_TASK, false )>]
    [<InlineData( false, TaskATTRCd.ORDERED_TASK, true )>]
    [<InlineData( false, TaskATTRCd.ORDERED_TASK, false )>]
    [<InlineData( false, TaskATTRCd.SIMPLE_TASK, true )>]
    [<InlineData( false, TaskATTRCd.SIMPLE_TASK, false )>]
    [<InlineData( false, TaskATTRCd.TAGLESS_TASK, false )>]
    [<InlineData( false, TaskATTRCd.TAGLESS_TASK, true )>]
    [<InlineData( true, TaskATTRCd.HEAD_OF_QUEUE_TASK, true )>]
    [<InlineData( true, TaskATTRCd.HEAD_OF_QUEUE_TASK, false )>]
    [<InlineData( true, TaskATTRCd.ORDERED_TASK, true )>]
    [<InlineData( true, TaskATTRCd.ORDERED_TASK, false )>]
    [<InlineData( true, TaskATTRCd.SIMPLE_TASK, true )>]
    [<InlineData( true, TaskATTRCd.SIMPLE_TASK, false )>]
    [<InlineData( true, TaskATTRCd.TAGLESS_TASK, false )>]
    [<InlineData( true, TaskATTRCd.TAGLESS_TASK, true )>]
    member _.NonACA_NormalTask_Success_001 ( argca : bool, taskCode : TaskATTRCd, argNACA : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let writeData1 = PooledBuffer.Rent( Blocksize.toUInt32 m_MediaBlockSize |> int32 )

            if argca then
                // it raise CA
                let! itt_w1_ca = r1.Send_Write10 taskCode g_LUN1 ( blkcnt_me.ofUInt32 0xFFFFFFFFu ) m_MediaBlockSize writeData1 NACA.F
                let! res_w1_ca = r1.WaitSCSIResponse itt_w1_ca
                Assert.True(( res_w1_ca.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            let naca = NACA.ofBool argNACA
            let! itt_w1 = r1.Send_Write10 taskCode g_LUN1 ( blkcnt_me.ofUInt32 3u ) m_MediaBlockSize writeData1 naca
            let! res_r1 = r1.WaitSCSIResponseGoogStatus itt_w1
            res_r1.Return()

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( false, TaskATTRCd.HEAD_OF_QUEUE_TASK, true )>]
    [<InlineData( false, TaskATTRCd.HEAD_OF_QUEUE_TASK, false )>]
    [<InlineData( false, TaskATTRCd.ORDERED_TASK, true )>]
    [<InlineData( false, TaskATTRCd.ORDERED_TASK, false )>]
    [<InlineData( false, TaskATTRCd.SIMPLE_TASK, true )>]
    [<InlineData( false, TaskATTRCd.SIMPLE_TASK, false )>]
    [<InlineData( false, TaskATTRCd.TAGLESS_TASK, true )>]
    [<InlineData( false, TaskATTRCd.TAGLESS_TASK, false )>]
    [<InlineData( true, TaskATTRCd.HEAD_OF_QUEUE_TASK, true )>]
    [<InlineData( true, TaskATTRCd.HEAD_OF_QUEUE_TASK, false )>]
    [<InlineData( true, TaskATTRCd.ORDERED_TASK, true )>]
    [<InlineData( true, TaskATTRCd.ORDERED_TASK, false )>]
    [<InlineData( true, TaskATTRCd.SIMPLE_TASK, true )>]
    [<InlineData( true, TaskATTRCd.SIMPLE_TASK, false )>]
    [<InlineData( true, TaskATTRCd.TAGLESS_TASK, true )>]
    [<InlineData( true, TaskATTRCd.TAGLESS_TASK, false )>]
    member _.NonACA_NormalTask_Failed_CA_001 ( argca : bool, taskCode : TaskATTRCd, raiseaca : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let writeData1 = PooledBuffer.Rent( Blocksize.toUInt32 m_MediaBlockSize |> int32 )

            if argca then
                // it raise CA
                let! itt_w1_ca = r1.Send_Write10 taskCode g_LUN1 ( blkcnt_me.ofUInt32 0xFFFFFFFFu ) m_MediaBlockSize writeData1 NACA.F
                let! res_w1_ca = r1.WaitSCSIResponse itt_w1_ca
                Assert.True(( res_w1_ca.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // register reservation key
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim 0UL ) ( resvkey_me.fromPrim 111UL ) false false false [||]
            let! _ = r1.WaitSCSIResponseGoogStatus itt_pr_out1

            if raiseaca then
                // send task with NACA=1
                let! itt_w2_aca = r1.Send_Write10 taskCode g_LUN1 ( blkcnt_me.ofUInt32 0xFFFFFFFFu ) m_MediaBlockSize writeData1 NACA.T
                let! res_w2_aca = r1.WaitSCSIResponse itt_w2_aca
                Assert.True(( res_w2_aca.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            
                // Check that ACA is established
                let! itt_w3 = r1.Send_Write10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize writeData1 NACA.T
                let! res_w3 = r1.WaitSCSIResponse itt_w3
                Assert.True(( res_w3.Status = ScsiCmdStatCd.ACA_ACTIVE ))

                // clear ACA
                let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
                let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
                Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))
            else
                // send task with NACA=0
                let! itt_w4_ca = r1.Send_Write10 taskCode g_LUN1 ( blkcnt_me.ofUInt32 0xFFFFFFFFu ) m_MediaBlockSize writeData1 NACA.F
                let! res_w4_ca = r1.WaitSCSIResponse itt_w4_ca
                Assert.True(( res_w4_ca.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            
            // Get reservarion key. Make sure the reservation remains intact.
            let! itt_pr_in1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T
            let! res_pr_in1 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in1
            Assert.True(( res_pr_in1.ReservationKey.Length = 1 ))
            Assert.True(( res_pr_in1.ReservationKey.[0] = resvkey_me.fromPrim 111UL ))

            // clear reservation key
            let! itt_pr_out2 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim 111UL )
            let! _ = r1.WaitSCSIResponseGoogStatus itt_pr_out2

            let! itt_pr_in2 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T
            let! itt_pr_in2 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in2
            Assert.True(( itt_pr_in2.ReservationKey.Length = 0 ))

            writeData1.Return()
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( false, true )>]
    [<InlineData( false, false )>]
    [<InlineData( true, true )>]
    [<InlineData( true, false )>]
    member _.NonACA_ACATask_CA_001 ( argca : bool, raiseaca : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let writeData1 = PooledBuffer.Rent( Blocksize.toUInt32 m_MediaBlockSize |> int32 )

            if argca then
                // it raise CA
                let! itt_w1_ca = r1.Send_Write10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0xFFFFFFFFu ) m_MediaBlockSize writeData1 NACA.F
                let! res_w1_ca = r1.WaitSCSIResponse itt_w1_ca
                Assert.True(( res_w1_ca.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // register reservation key
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim 0UL ) ( resvkey_me.fromPrim 111UL ) false false false [||]
            let! _ = r1.WaitSCSIResponseGoogStatus itt_pr_out1

            if raiseaca then
                // send task with NACA=1
                let! itt_w2_aca = r1.Send_Write10 TaskATTRCd.ACA_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize writeData1 NACA.T
                let! res_w2_aca = r1.WaitSCSIResponse itt_w2_aca
                Assert.True(( res_w2_aca.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( res_w2_aca.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( res_w2_aca.Sense.Value.ASC = ASCCd.INVALID_MESSAGE_ERROR ))
            
                // Check that ACA is established
                let! itt_w3 = r1.Send_Write10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize writeData1 NACA.T
                let! res_w3 = r1.WaitSCSIResponse itt_w3
                Assert.True(( res_w3.Status = ScsiCmdStatCd.ACA_ACTIVE ))

                // clear ACA
                let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
                let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
                Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))
            else
                // send ACA task with NACA=0
                let! itt_w4_ca = r1.Send_Write10 TaskATTRCd.ACA_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize writeData1 NACA.F
                let! res_w4_ca = r1.WaitSCSIResponse itt_w4_ca
                Assert.True(( res_w4_ca.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                Assert.True(( res_w4_ca.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
                Assert.True(( res_w4_ca.Sense.Value.ASC = ASCCd.INVALID_MESSAGE_ERROR ))
            
            // Get reservarion key. Make sure the reservation remains intact.
            let! itt_pr_in1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T
            let! res_pr_in1 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in1
            Assert.True(( res_pr_in1.ReservationKey.Length = 1 ))
            Assert.True(( res_pr_in1.ReservationKey.[0] = resvkey_me.fromPrim 111UL ))

            // clear reservation key
            let! itt_pr_out2 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim 111UL )
            let! _ = r1.WaitSCSIResponseGoogStatus itt_pr_out2

            let! itt_pr_in2 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T
            let! itt_pr_in2 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in2
            Assert.True(( itt_pr_in2.ReservationKey.Length = 0 ))

            writeData1.Return()
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( TaskATTRCd.HEAD_OF_QUEUE_TASK, true )>]
    [<InlineData( TaskATTRCd.HEAD_OF_QUEUE_TASK, false )>]
    [<InlineData( TaskATTRCd.ORDERED_TASK, true )>]
    [<InlineData( TaskATTRCd.ORDERED_TASK, false )>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK, true )>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK, false )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK, true )>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK, false )>]
    member _.ACAActive_NormalTask_001 ( taskCode : TaskATTRCd, argNaca : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let writeData1 = PooledBuffer.Rent( Blocksize.toUInt32 m_MediaBlockSize |> int32 )

            // register reservation key
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim 0UL ) ( resvkey_me.fromPrim 222UL ) false false false [||]
            let! _ = r1.WaitSCSIResponseGoogStatus itt_pr_out1

            // raise ACA
            let! itt_w1_aca = r1.Send_Write10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0xFFFFFFFFu ) m_MediaBlockSize writeData1 NACA.T
            let! res_w1_aca = r1.WaitSCSIResponse itt_w1_aca
            Assert.True(( res_w1_aca.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // send task
            let naca = NACA.ofBool argNaca
            let! itt_w2 = r1.Send_Write10 taskCode g_LUN1 blkcnt_me.zero32 m_MediaBlockSize writeData1 naca
            let! res_w2 = r1.WaitSCSIResponse itt_w2
            Assert.True(( res_w2.Status = ScsiCmdStatCd.ACA_ACTIVE ))

            // clear ACA
            let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))
            
            // Get reservarion key. Make sure the reservation remains intact.
            let! itt_pr_in1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T
            let! res_pr_in1 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in1
            Assert.True(( res_pr_in1.ReservationKey.Length = 1 ))
            Assert.True(( res_pr_in1.ReservationKey.[0] = resvkey_me.fromPrim 222UL ))

            // clear reservation key
            let! itt_pr_out2 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim 222UL )
            let! _ = r1.WaitSCSIResponseGoogStatus itt_pr_out2

            let! itt_pr_in2 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T
            let! itt_pr_in2 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in2
            Assert.True(( itt_pr_in2.ReservationKey.Length = 0 ))

            writeData1.Return()
            do! r1.Close()
        }

    // ACA is established.
    // There are an ACA task in the task set.
    // The ACA task(NACA=0/1) will fail with ACA ACTIVE.
    [<Theory>]
    [<InlineData( true )>]
    [<InlineData( false )>]
    member _.ACAActive_ACATask__ACATaskExists_001 ( argNaca : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let writeData1 = PooledBuffer.Rent( Blocksize.toUInt32 m_MediaBlockSize |> int32 )

            // register reservation key
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim 0UL ) ( resvkey_me.fromPrim 222UL ) false false false [||]
            let! _ = r1.WaitSCSIResponseGoogStatus itt_pr_out1

            // raise ACA
            let! itt_w1_aca = r1.Send_Write10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0xFFFFFFFFu ) m_MediaBlockSize writeData1 NACA.T
            let! res_w1_aca = r1.WaitSCSIResponse itt_w1_aca
            Assert.True(( res_w1_aca.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // Add debug media trap
            m_ClientProc.RunCommand "add trap /e Write /a Wait" "Trap added" "MD> "

            // Sending a task with ACA attributes and intentionally leaving it stuck
            let! itt_w2_stuck = r1.Send_Write10 TaskATTRCd.ACA_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize writeData1 NACA.T

            // Confirm that above task is stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 10

            // send ACA task
            let naca = NACA.ofBool argNaca
            let! itt_w3_aca = r1.Send_Write10 TaskATTRCd.ACA_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize writeData1 naca
            let! res_w3_aca = r1.WaitSCSIResponse itt_w3_aca
            Assert.True(( res_w3_aca.Status = ScsiCmdStatCd.ACA_ACTIVE ))
            do! r1.Send_StatusACK()

            // Resume stucked task.
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_w2_stuck ) "Task(" "MD> "
            let! _ = r1.WaitSCSIResponseGoogStatus itt_w2_stuck

            // clear ACA
            let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))
            
            // Get reservarion key. Make sure the reservation remains intact.
            let! itt_pr_in1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T
            let! res_pr_in1 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in1
            Assert.True(( res_pr_in1.ReservationKey.Length = 1 ))
            Assert.True(( res_pr_in1.ReservationKey.[0] = resvkey_me.fromPrim 222UL ))

            // clear reservation key
            let! itt_pr_out2 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim 222UL )
            let! _ = r1.WaitSCSIResponseGoogStatus itt_pr_out2

            let! itt_pr_in2 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T
            let! itt_pr_in2 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in2
            Assert.True(( itt_pr_in2.ReservationKey.Length = 0 ))

            // Clear debug media traps
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "

            writeData1.Return()
            do! r1.Close()
        }

    // ACA is established.
    // There are no ACA tasks in the task set.
    // The ACA task(NACA=0/1) will complete successfully.
    [<Theory>]
    [<InlineData( true )>]
    [<InlineData( false )>]
    member _.ACAActive_ACATask_ACATaskNotExists_Success_001 ( argNaca : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let writeData1 = PooledBuffer.Rent( Blocksize.toUInt32 m_MediaBlockSize |> int32 )

            // register reservation key
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim 0UL ) ( resvkey_me.fromPrim 222UL ) false false false [||]
            let! _ = r1.WaitSCSIResponseGoogStatus itt_pr_out1

            // raise ACA
            let! itt_w1_aca = r1.Send_Write10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0xFFFFFFFFu ) m_MediaBlockSize writeData1 NACA.T
            let! res_w1_aca = r1.WaitSCSIResponse itt_w1_aca
            Assert.True(( res_w1_aca.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // send ACA task
            let naca = NACA.ofBool argNaca
            let! itt_w3_aca = r1.Send_Write10 TaskATTRCd.ACA_TASK g_LUN1 blkcnt_me.zero32 m_MediaBlockSize writeData1 naca
            let! _ = r1.WaitSCSIResponseGoogStatus itt_w3_aca

            // clear ACA
            let! itt_tmf1 = r1.SendTMFRequest_ClearACA BitI.F g_LUN1
            let! res_tmf1 = r1.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))
            
            // Get reservarion key. Make sure the reservation remains intact.
            let! itt_pr_in1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T
            let! res_pr_in1 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in1
            Assert.True(( res_pr_in1.ReservationKey.Length = 1 ))
            Assert.True(( res_pr_in1.ReservationKey.[0] = resvkey_me.fromPrim 222UL ))

            // clear reservation key
            let! itt_pr_out2 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim 222UL )
            let! _ = r1.WaitSCSIResponseGoogStatus itt_pr_out2

            let! itt_pr_in2 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T
            let! itt_pr_in2 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in2
            Assert.True(( itt_pr_in2.ReservationKey.Length = 0 ))

            writeData1.Return()
            do! r1.Close()
        }

    // ACA is established.
    // There are no ACA tasks in the task set.
    // The ACA task(NACA=0) failed.
    // In this case, the existing ACA is cleared, sense data is returned, and a new ACA is not established.
    // As a result, the ACA is cleared and things return to normal.
    // And, the task that was in a dormant state will be executed.
    // Note: If there is an iSCSI task with an ACA attribute in the session queue, that task will remain as is.
    [<Fact>]
    member _.ACAActive_ACATask_ACATaskNotExists_Failed_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let writeData1 = PooledBuffer.Rent( Blocksize.toUInt32 m_MediaBlockSize |> int32 )

            // Add debug media trap
            m_ClientProc.RunCommand "add trap /e Write /a Wait" "Trap added" "MD> "
            m_ClientProc.RunCommand "add trap /e Write /slba 3 /elba 3 /a ACA" "Trap added" "MD> "

            // register reservation key
            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim 0UL ) ( resvkey_me.fromPrim 222UL ) false false false [||]
            let! _ = r1.WaitSCSIResponseGoogStatus itt_pr_out1

            // Send HOQ task(T1), and stucked at debug media
            let! itt_t1 = r1.Send_Write10 TaskATTRCd.HEAD_OF_QUEUE_TASK g_LUN1 ( blkcnt_me.ofUInt32 3u ) m_MediaBlockSize writeData1 NACA.T

            // Check that the T1 HOQ task is stuck.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 10

            // Send a Simple task(T2), which goes into the Dormant Task state.
            let! itt_t2 = r1.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 0u ) m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T

            // Resume T1 HOQ task, and it raise ACA
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_t1 ) "Task(" "MD> "

            // Check result the T1.
            let! res_t1 = r1.WaitSCSIResponse itt_t1
            Assert.True(( res_t1.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // Send ACA task with NACA=0(T3), and stucked at debug media
            let! itt_t3 = r1.Send_Write10 TaskATTRCd.ACA_TASK g_LUN1 ( blkcnt_me.ofUInt32 3u ) m_MediaBlockSize writeData1 NACA.F

            // Resume T3 ACA task with NACA=0, and it failed. ACA is cleared.
            do! Task.Delay 10
            while ( ( GetStuckTasks() ).Length < 1 ) do
                do! Task.Delay 10
            m_ClientProc.RunCommand ( sprintf "task resume /t %d /i %d" r1.TSIH itt_t3 ) "Task(" "MD> "

            let! res_t3 = r1.WaitSCSIResponse itt_t3
            Assert.True(( res_t3.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            // Receive the results of T2 Simple task
            let! res_t2 = r1.WaitSCSIResponseGoogStatus itt_t2
            res_t2.Return()

            // Get reservarion key. Make sure the reservation remains intact.
            let! itt_pr_in1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256us NACA.T
            let! res_pr_in1 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in1
            Assert.True(( res_pr_in1.ReservationKey.Length = 1 ))
            Assert.True(( res_pr_in1.ReservationKey.[0] = resvkey_me.fromPrim 222UL ))

            // clear reservation key
            let! itt_pr_out2 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T ( resvkey_me.fromPrim 222UL )
            let! _ = r1.WaitSCSIResponseGoogStatus itt_pr_out2

            // Clear debug media traps
            m_ClientProc.RunCommand "clear trap" "Traps cleared" "MD> "

            writeData1.Return()
            do! r1.Close()
        }

