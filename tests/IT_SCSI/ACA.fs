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

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
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
            let! itt2 = r.SendTaskManagementFunctionRequest BitI.F TaskMgrReqCd.CLEAR_ACA g_LUN1 ( itt_me.fromPrim 0xFFFFFFFFu ) ( ValueSome cmdsn_me.zero ) datasn_me.zero
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
            let! itt2 = r.SendTaskManagementFunctionRequest BitI.F TaskMgrReqCd.CLEAR_ACA g_LUN1 ( itt_me.fromPrim 0xFFFFFFFFu ) ( ValueSome cmdsn_me.zero ) datasn_me.zero
            let! result2 = r.WaitTMFResponse itt2
            Assert.True(( result2 = TaskMgrResCd.FUNCTION_COMPLETE ))

            do! r.Close()
        }
