//=============================================================================
// Haruka Software Storage.
// Commands02.fs : Test cases for various commands.
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

[<CollectionDefinition( "SCSI_Commands02" )>]
type SCSI_Commands02_Fixture() =

    static let m_MediaSize = 65536u

    let m_iSCSIPortNo = GlbFunc.nextTcpPortNo()

    // Add default configurations
    let AddDefaultConf( client : ClientProc ): unit =

        ///////////////////////////////
        // Target Device 0

        // Target device, Target group
        client.RunCommand "create" "Created" "CR> "
        client.RunCommand "select 0" "" "TD> "
        client.RunCommand "set Name targetDevice001" "" "TD> "
        client.RunCommand "set ID TD_00000063" "" "TD> "
        client.RunCommand "set loglevel VERBOSE" "" "TD> "
        client.RunCommand "create targetgroup" "Created" "TD> "
        client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo ) "Created" "TD> "
        client.RunCommand "select 0" "" "TG> "

        // Target1, LU1
        client.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        client.RunCommand "select 0" "" "T > "
        client.RunCommand "set ID 1" "" "T > "
        client.RunCommand "create /l 1" "Created" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "LU> "
        client.RunCommand "unselect" "" "T > "
        client.RunCommand "unselect" "" "TG> "

        client.RunCommand "validate" "All configurations are vlidated" "TG> "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "TG> "
        client.RunCommand "start" "Started" "TG> "

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

    interface ICollectionFixture<SCSI_Commands02_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.iSCSIPortNo = m_iSCSIPortNo
    member _.WorkPath = m_WorkPath

    static member MediaSize = m_MediaSize
    static member MediaBlockSize = 
        if Constants.MEDIA_BLOCK_SIZE = 512UL then     // 4096 or 512 bytes
            Blocksize.BS_512
        else
            Blocksize.BS_4096

[<Collection( "SCSI_Commands02" )>]
type SCSI_Commands02( fx : SCSI_Commands02_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_CID1 = cid_me.fromPrim 1us
    let g_CID2 = cid_me.fromPrim 2us

    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL

    let g_ResvKey1 = resvkey_me.fromPrim 1UL
    let g_ResvKey2 = resvkey_me.fromPrim 2UL

    let m_ClientProc = fx.clientProc
    let iSCSIPortNo = fx.iSCSIPortNo
    let m_WorkPath = fx.WorkPath
    let m_InitName = "iqn.2020-05.example.com:initiator"

    static let m_MediaSize = SCSI_Commands01_Fixture.MediaSize
    static let m_MediaBlockSize = SCSI_Commands01_Fixture.MediaBlockSize
    static let m_MediaBlockCount = m_MediaSize / ( Blocksize.toUInt32 m_MediaBlockSize )

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

    let ClearACA ( r : SCSI_Initiator ) ( lun : LUN_T ) : Task<unit> =
        task {
            let! itt_tmf1 = r.SendTMFRequest_ClearACA BitI.F lun
            let! res_tmf1 = r.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))
        }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Theory>]
    [<InlineData( 0UL, 0uy, 0 )>]
    [<InlineData( 0UL, 4uy, 4 )>]
    [<InlineData( 0UL, 12uy, 12 )>]
    [<InlineData( 0UL, 255uy, 56 )>]
    [<InlineData( 1UL, 0uy, 0 )>]
    [<InlineData( 1UL, 4uy, 4 )>]
    [<InlineData( 1UL, 12uy, 12 )>]
    [<InlineData( 1UL, 255uy, 56 )>]
    member _.ModeSense6_001 ( lu : uint64 ) ( allen : byte ) ( explen : int ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F 0uy 0x3Fuy 0uy allen NACA.T
            let! res = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( res.Length = explen ))

            let r = GenScsiParams.ModeSense6 res
            if res.Length >= 4 then
                Assert.True(( r.ModeDataLength > 0uy ))
                Assert.True(( r.MediumType = 0uy ))
                Assert.True(( r.BlockDescriptorLength > 0uy ))

            if res.Length >= 12 then
                Assert.True(( r.Block.IsSome ))
                if lu = 0UL then
                    Assert.True(( ( r.Block.Value.BlockCount |> blkcnt_me.toUInt64 ) = 0UL ))
                    Assert.True(( r.Block.Value.BlockLength = 0u ))
                else
                    Assert.True(( ( r.Block.Value.BlockCount |> blkcnt_me.toUInt64 ) > 0UL ))
                    Assert.True(( r.Block.Value.BlockLength > 0u ))
            res.Return()
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.ModeSense6_DBD_001 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.T 0uy 0x3Fuy 0uy 255uy NACA.T
            let! res = r1.WaitSCSIResponseGoodStatus itt
            let r = GenScsiParams.ModeSense6 res
            Assert.True(( r.Block.IsNone ))
            res.Return()
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0uy, 0x00uy, 0x00uy )>]
    [<InlineData( 0uy, 0x07uy, 0x00uy )>]
    [<InlineData( 0uy, 0x09uy, 0x00uy )>]
    [<InlineData( 0uy, 0x0Buy, 0x00uy )>]
    [<InlineData( 0uy, 0x1Buy, 0x00uy )>]
    [<InlineData( 0uy, 0x1Duy, 0x00uy )>]
    [<InlineData( 0uy, 0x3Euy, 0x00uy )>]
    [<InlineData( 1uy, 0x00uy, 0x00uy )>]
    [<InlineData( 1uy, 0x07uy, 0x00uy )>]
    [<InlineData( 1uy, 0x09uy, 0x00uy )>]
    [<InlineData( 1uy, 0x0Buy, 0x00uy )>]
    [<InlineData( 1uy, 0x1Buy, 0x00uy )>]
    [<InlineData( 1uy, 0x1Duy, 0x00uy )>]
    [<InlineData( 1uy, 0x3Euy, 0x00uy )>]
    [<InlineData( 2uy, 0x00uy, 0x00uy )>]
    [<InlineData( 2uy, 0x07uy, 0x00uy )>]
    [<InlineData( 2uy, 0x09uy, 0x00uy )>]
    [<InlineData( 2uy, 0x0Buy, 0x00uy )>]
    [<InlineData( 2uy, 0x1Buy, 0x00uy )>]
    [<InlineData( 2uy, 0x1Duy, 0x00uy )>]
    [<InlineData( 2uy, 0x3Euy, 0x00uy )>]
    [<InlineData( 3uy, 0x00uy, 0x00uy )>]
    [<InlineData( 3uy, 0x07uy, 0x00uy )>]
    [<InlineData( 3uy, 0x09uy, 0x00uy )>]
    [<InlineData( 3uy, 0x0Buy, 0x00uy )>]
    [<InlineData( 3uy, 0x1Buy, 0x00uy )>]
    [<InlineData( 3uy, 0x1Duy, 0x00uy )>]
    [<InlineData( 3uy, 0x3Euy, 0x00uy )>]
    [<InlineData( 0uy, 0x08uy, 0x01uy )>]
    [<InlineData( 0uy, 0x08uy, 0xFEuy )>]
    [<InlineData( 0uy, 0x0Auy, 0x01uy )>]
    [<InlineData( 0uy, 0x0Auy, 0xFEuy )>]
    [<InlineData( 0uy, 0x1Cuy, 0x01uy )>]
    [<InlineData( 0uy, 0x1Cuy, 0xFEuy )>]
    [<InlineData( 0uy, 0x3Fuy, 0x01uy )>]
    [<InlineData( 0uy, 0x3Fuy, 0xFEuy )>]
    [<InlineData( 1uy, 0x08uy, 0x01uy )>]
    [<InlineData( 1uy, 0x08uy, 0xFEuy )>]
    [<InlineData( 1uy, 0x0Auy, 0x01uy )>]
    [<InlineData( 1uy, 0x0Auy, 0xFEuy )>]
    [<InlineData( 1uy, 0x1Cuy, 0x01uy )>]
    [<InlineData( 1uy, 0x1Cuy, 0xFEuy )>]
    [<InlineData( 1uy, 0x3Fuy, 0x01uy )>]
    [<InlineData( 1uy, 0x3Fuy, 0xFEuy )>]
    [<InlineData( 2uy, 0x08uy, 0x01uy )>]
    [<InlineData( 2uy, 0x08uy, 0xFEuy )>]
    [<InlineData( 2uy, 0x0Auy, 0x01uy )>]
    [<InlineData( 2uy, 0x0Auy, 0xFEuy )>]
    [<InlineData( 2uy, 0x1Cuy, 0x01uy )>]
    [<InlineData( 2uy, 0x1Cuy, 0xFEuy )>]
    [<InlineData( 2uy, 0x3Fuy, 0x01uy )>]
    [<InlineData( 2uy, 0x3Fuy, 0xFEuy )>]
    [<InlineData( 3uy, 0x08uy, 0x01uy )>]
    [<InlineData( 3uy, 0x08uy, 0xFEuy )>]
    [<InlineData( 3uy, 0x0Auy, 0x01uy )>]
    [<InlineData( 3uy, 0x0Auy, 0xFEuy )>]
    [<InlineData( 3uy, 0x1Cuy, 0x01uy )>]
    [<InlineData( 3uy, 0x1Cuy, 0xFEuy )>]
    [<InlineData( 3uy, 0x3Fuy, 0x01uy )>]
    [<InlineData( 3uy, 0x3Fuy, 0xFEuy )>]
    member _.ModeSense6_UnsupportedPaceCode_001 ( pc : byte ) ( page : byte ) ( subpage : byte ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK g_LUN1 DBD.F pc page subpage 255uy NACA.T
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0uy, 0x00uy )>]
    [<InlineData( 0UL, 0uy, 0xFFuy )>]
    [<InlineData( 0UL, 1uy, 0x00uy )>]
    [<InlineData( 0UL, 1uy, 0xFFuy )>]
    [<InlineData( 0UL, 2uy, 0x00uy )>]
    [<InlineData( 0UL, 2uy, 0xFFuy )>]
    [<InlineData( 0UL, 3uy, 0x00uy )>]
    [<InlineData( 0UL, 3uy, 0xFFuy )>]
    [<InlineData( 1UL, 0uy, 0x00uy )>]
    [<InlineData( 1UL, 0uy, 0xFFuy )>]
    [<InlineData( 1UL, 1uy, 0x00uy )>]
    [<InlineData( 1UL, 1uy, 0xFFuy )>]
    [<InlineData( 1UL, 2uy, 0x00uy )>]
    [<InlineData( 1UL, 2uy, 0xFFuy )>]
    [<InlineData( 1UL, 3uy, 0x00uy )>]
    [<InlineData( 1UL, 3uy, 0xFFuy )>]
    member _.ModeSense6_CacheModePage_001 ( lu : uint64 ) ( pc : byte ) ( subpage : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F pc 0x08uy subpage 255uy NACA.T
            let! res = r1.Wait_ModeSense6 itt
            Assert.True(( res.Block.IsSome ))
            Assert.True(( res.Control.IsNone ))
            Assert.True(( res.Cache.IsSome ))
            Assert.True(( res.Cache.Value.PageLength = 0x12uy ))
            Assert.True(( res.InformationalExceptionsControl.IsNone ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x00uy )>]
    [<InlineData( 0UL, 0xFFuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    [<InlineData( 1UL, 0xFFuy )>]
    member _.ModeSense6_ControlModePage_Current_001 ( lu : uint64 ) ( subpage : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // Initialize to default values
            let! itt1 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F 0x2uy 0x0Auy subpage 255uy NACA.T
            let! res1 = r1.Wait_ModeSense6 itt1

            let! itt2 = r1.Send_ModeSelect6 TaskATTRCd.SIMPLE_TASK lun PF.T SP.F res1 NACA.T
            let! _ = r1.WaitSCSIResponseGoodStatus itt2

            // Get the current value
            let! itt3 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F 0x0uy 0x0Auy subpage 255uy NACA.T
            let! res3 = r1.Wait_ModeSense6 itt3
            Assert.True(( res3.Block.IsSome ))
            Assert.True(( res3.Control.IsSome ))
            Assert.True(( res3.Control.Value.PageLength = 0x0Auy ))
            Assert.True(( res3.Cache.IsNone ))
            Assert.True(( res3.InformationalExceptionsControl.IsNone ))

            // Update the current value
            let param1 = {
                res3 with
                    Control = Some {
                        res3.Control.Value with
                            DescriptorFormatSenseData = not res3.Control.Value.DescriptorFormatSenseData;
                            SoftwareWriteProtect = not res3.Control.Value.SoftwareWriteProtect;
                    }
            }
            let! itt4 = r1.Send_ModeSelect6 TaskATTRCd.SIMPLE_TASK lun PF.T SP.F param1 NACA.T
            let! _ = r1.WaitSCSIResponseGoodStatus itt4

            // Confirm that the current value has been updated.
            let! itt5 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F 0x0uy 0x0Auy subpage 255uy NACA.T
            let! res5 = r1.Wait_ModeSense6 itt5
            Assert.True(( res5.Control.Value.DescriptorFormatSenseData = param1.Control.Value.DescriptorFormatSenseData ))
            Assert.True(( res5.Control.Value.SoftwareWriteProtect = param1.Control.Value.SoftwareWriteProtect ))

            // Verify that the same value is obtained using the MODE SENSE(6) command.
            let! itt6 = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun LLBAA.T DBD.F 0x0uy 0x0Auy subpage 255us NACA.T
            let! res6 = r1.Wait_ModeSense10 itt6
            Assert.True(( res5.Control.Value = res6.Control.Value ))

            // Verify that the default values ​​have not been changed.
            let! itt7 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F 0x2uy 0x0Auy subpage 255uy NACA.T
            let! res7 = r1.Wait_ModeSense6 itt7
            Assert.True(( res7.Control.Value.DescriptorFormatSenseData <> param1.Control.Value.DescriptorFormatSenseData ))
            Assert.True(( res7.Control.Value.SoftwareWriteProtect <> param1.Control.Value.SoftwareWriteProtect ))

            // Verify that the saced values ​​have not been changed.
            let! itt8 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F 0x3uy 0x0Auy subpage 255uy NACA.T
            let! res8 = r1.Wait_ModeSense6 itt8
            Assert.True(( res8.Control.Value.DescriptorFormatSenseData <> param1.Control.Value.DescriptorFormatSenseData ))
            Assert.True(( res8.Control.Value.SoftwareWriteProtect <> param1.Control.Value.SoftwareWriteProtect ))

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 1uy, 0x00uy )>]
    [<InlineData( 0UL, 1uy, 0xFFuy )>]
    [<InlineData( 0UL, 2uy, 0x00uy )>]
    [<InlineData( 0UL, 2uy, 0xFFuy )>]
    [<InlineData( 0UL, 3uy, 0x00uy )>]
    [<InlineData( 0UL, 3uy, 0xFFuy )>]
    [<InlineData( 1UL, 1uy, 0x00uy )>]
    [<InlineData( 1UL, 1uy, 0xFFuy )>]
    [<InlineData( 1UL, 2uy, 0x00uy )>]
    [<InlineData( 1UL, 2uy, 0xFFuy )>]
    [<InlineData( 1UL, 3uy, 0x00uy )>]
    [<InlineData( 1UL, 3uy, 0xFFuy )>]
    member _.ModeSense6_ControlModePage_002 ( lu : uint64 ) ( pc : byte ) ( subpage : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt1 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F pc 0x0Auy subpage 255uy NACA.T
            let! res1 = r1.Wait_ModeSense6 itt1
            Assert.True(( res1.Block.IsSome ))
            Assert.True(( res1.Control.IsSome ))
            Assert.True(( res1.Control.Value.PageLength = 0x0Auy ))
            Assert.True(( res1.Cache.IsNone ))
            Assert.True(( res1.InformationalExceptionsControl.IsNone ))

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0uy, 0x00uy )>]
    [<InlineData( 0UL, 0uy, 0xFFuy )>]
    [<InlineData( 0UL, 1uy, 0x00uy )>]
    [<InlineData( 0UL, 1uy, 0xFFuy )>]
    [<InlineData( 0UL, 2uy, 0x00uy )>]
    [<InlineData( 0UL, 2uy, 0xFFuy )>]
    [<InlineData( 0UL, 3uy, 0x00uy )>]
    [<InlineData( 0UL, 3uy, 0xFFuy )>]
    [<InlineData( 1UL, 0uy, 0x00uy )>]
    [<InlineData( 1UL, 0uy, 0xFFuy )>]
    [<InlineData( 1UL, 1uy, 0x00uy )>]
    [<InlineData( 1UL, 1uy, 0xFFuy )>]
    [<InlineData( 1UL, 2uy, 0x00uy )>]
    [<InlineData( 1UL, 2uy, 0xFFuy )>]
    [<InlineData( 1UL, 3uy, 0x00uy )>]
    [<InlineData( 1UL, 3uy, 0xFFuy )>]
    member _.ModeSense6_InformationalExceptionsControlModePage_001 ( lu : uint64 ) ( pc : byte ) ( subpage : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F pc 0x1Cuy subpage 255uy NACA.T
            let! res = r1.Wait_ModeSense6 itt
            Assert.True(( res.Block.IsSome ))
            Assert.True(( res.Control.IsNone ))
            Assert.True(( res.Cache.IsNone ))
            Assert.True(( res.InformationalExceptionsControl.IsSome ))
            Assert.True(( res.InformationalExceptionsControl.Value.PageLength = 0x0Auy ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0uy, 0x00uy )>]
    [<InlineData( 0UL, 0uy, 0xFFuy )>]
    [<InlineData( 0UL, 1uy, 0x00uy )>]
    [<InlineData( 0UL, 1uy, 0xFFuy )>]
    [<InlineData( 0UL, 2uy, 0x00uy )>]
    [<InlineData( 0UL, 2uy, 0xFFuy )>]
    [<InlineData( 0UL, 3uy, 0x00uy )>]
    [<InlineData( 0UL, 3uy, 0xFFuy )>]
    [<InlineData( 1UL, 0uy, 0x00uy )>]
    [<InlineData( 1UL, 0uy, 0xFFuy )>]
    [<InlineData( 1UL, 1uy, 0x00uy )>]
    [<InlineData( 1UL, 1uy, 0xFFuy )>]
    [<InlineData( 1UL, 2uy, 0x00uy )>]
    [<InlineData( 1UL, 2uy, 0xFFuy )>]
    [<InlineData( 1UL, 3uy, 0x00uy )>]
    [<InlineData( 1UL, 3uy, 0xFFuy )>]
    member _.ModeSense6_AllPages_001 ( lu : uint64 ) ( pc : byte ) ( subpage : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F pc 0x3Fuy subpage 255uy NACA.T
            let! res = r1.Wait_ModeSense6 itt
            Assert.True(( res.Block.IsSome ))
            Assert.True(( res.Control.IsSome ))
            Assert.True(( res.Control.Value.PageLength = 0x0Auy ))
            Assert.True(( res.Cache.IsSome ))
            Assert.True(( res.Cache.Value.PageLength = 0x12uy ))
            Assert.True(( res.InformationalExceptionsControl.IsSome ))
            Assert.True(( res.InformationalExceptionsControl.Value.PageLength = 0x0Auy ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL  )>]
    member _.ModeSense6_LINK_001 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let cdb = GenScsiCDB.ModeSense6 DBD.F 0uy 0x3Fuy 0uy 255uy NACA.T LINK.T
            let! itt_pf1 = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK lun cdb PooledBuffer.Empty 255u
            let! res_pf1 = r1.WaitSCSIResponse itt_pf1
            Assert.True(( res_pf1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 lun

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, false, 0us, 0 )>]
    [<InlineData( 0UL, false, 8us, 8 )>]
    [<InlineData( 0UL, false, 16us, 16 )>]
    [<InlineData( 0UL, false, 255us, 60 )>]
    [<InlineData( 0UL, true,  0us, 0 )>]
    [<InlineData( 0UL, true,  8us, 8 )>]
    [<InlineData( 0UL, true,  24us, 24 )>]
    [<InlineData( 0UL, true,  255us, 68 )>]
    [<InlineData( 1UL, false, 0us, 0 )>]
    [<InlineData( 1UL, false, 8us, 8 )>]
    [<InlineData( 1UL, false, 16us, 16 )>]
    [<InlineData( 1UL, false, 255us, 60 )>]
    [<InlineData( 1UL, true,  0us, 0 )>]
    [<InlineData( 1UL, true,  8us, 8 )>]
    [<InlineData( 1UL, true,  24us, 24 )>]
    [<InlineData( 1UL, true,  255us, 68 )>]
    member _.ModeSense10_001 ( lu : uint64 ) ( llbaa : bool ) ( allen : uint16 ) ( explen : int ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun ( LLBAA.ofBool llbaa ) DBD.F 0uy 0x3Fuy 0uy allen NACA.T
            let! res = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( res.Length = explen ))

            let r = GenScsiParams.ModeSense10 res
            if res.Length >= 8 then
                Assert.True(( r.ModeDataLength > 0us ))
                Assert.True(( r.MediumType = 0uy ))
                Assert.True(( r.BlockDescriptorLength > 0us ))

            if res.Length >= 16 then
                Assert.True(( r.Block.IsSome ))
                if lu = 0UL then
                    Assert.True(( ( r.Block.Value.BlockCount |> blkcnt_me.toUInt64 ) = 0UL ))
                    Assert.True(( r.Block.Value.BlockLength = 0u ))
                else
                    Assert.True(( ( r.Block.Value.BlockCount |> blkcnt_me.toUInt64 ) > 0UL ))
                    Assert.True(( r.Block.Value.BlockLength > 0u ))
            res.Return()
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, true  )>]
    [<InlineData( 0UL, false )>]
    [<InlineData( 1UL, true  )>]
    [<InlineData( 1UL, false )>]
    member _.ModeSense10_DBD_001 ( lu : uint64 ) ( llbaa : bool ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun ( LLBAA.ofBool llbaa ) DBD.T 0uy 0x3Fuy 0uy 255us NACA.T
            let! res = r1.WaitSCSIResponseGoodStatus itt
            let r = GenScsiParams.ModeSense10 res
            Assert.True(( r.Block.IsNone ))
            res.Return()
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0uy, 0x00uy, 0x00uy )>]
    [<InlineData( 0uy, 0x07uy, 0x00uy )>]
    [<InlineData( 0uy, 0x09uy, 0x00uy )>]
    [<InlineData( 0uy, 0x0Buy, 0x00uy )>]
    [<InlineData( 0uy, 0x1Buy, 0x00uy )>]
    [<InlineData( 0uy, 0x1Duy, 0x00uy )>]
    [<InlineData( 0uy, 0x3Euy, 0x00uy )>]
    [<InlineData( 1uy, 0x00uy, 0x00uy )>]
    [<InlineData( 1uy, 0x07uy, 0x00uy )>]
    [<InlineData( 1uy, 0x09uy, 0x00uy )>]
    [<InlineData( 1uy, 0x0Buy, 0x00uy )>]
    [<InlineData( 1uy, 0x1Buy, 0x00uy )>]
    [<InlineData( 1uy, 0x1Duy, 0x00uy )>]
    [<InlineData( 1uy, 0x3Euy, 0x00uy )>]
    [<InlineData( 2uy, 0x00uy, 0x00uy )>]
    [<InlineData( 2uy, 0x07uy, 0x00uy )>]
    [<InlineData( 2uy, 0x09uy, 0x00uy )>]
    [<InlineData( 2uy, 0x0Buy, 0x00uy )>]
    [<InlineData( 2uy, 0x1Buy, 0x00uy )>]
    [<InlineData( 2uy, 0x1Duy, 0x00uy )>]
    [<InlineData( 2uy, 0x3Euy, 0x00uy )>]
    [<InlineData( 3uy, 0x00uy, 0x00uy )>]
    [<InlineData( 3uy, 0x07uy, 0x00uy )>]
    [<InlineData( 3uy, 0x09uy, 0x00uy )>]
    [<InlineData( 3uy, 0x0Buy, 0x00uy )>]
    [<InlineData( 3uy, 0x1Buy, 0x00uy )>]
    [<InlineData( 3uy, 0x1Duy, 0x00uy )>]
    [<InlineData( 3uy, 0x3Euy, 0x00uy )>]
    [<InlineData( 0uy, 0x08uy, 0x01uy )>]
    [<InlineData( 0uy, 0x08uy, 0xFEuy )>]
    [<InlineData( 0uy, 0x0Auy, 0x01uy )>]
    [<InlineData( 0uy, 0x0Auy, 0xFEuy )>]
    [<InlineData( 0uy, 0x1Cuy, 0x01uy )>]
    [<InlineData( 0uy, 0x1Cuy, 0xFEuy )>]
    [<InlineData( 0uy, 0x3Fuy, 0x01uy )>]
    [<InlineData( 0uy, 0x3Fuy, 0xFEuy )>]
    [<InlineData( 1uy, 0x08uy, 0x01uy )>]
    [<InlineData( 1uy, 0x08uy, 0xFEuy )>]
    [<InlineData( 1uy, 0x0Auy, 0x01uy )>]
    [<InlineData( 1uy, 0x0Auy, 0xFEuy )>]
    [<InlineData( 1uy, 0x1Cuy, 0x01uy )>]
    [<InlineData( 1uy, 0x1Cuy, 0xFEuy )>]
    [<InlineData( 1uy, 0x3Fuy, 0x01uy )>]
    [<InlineData( 1uy, 0x3Fuy, 0xFEuy )>]
    [<InlineData( 2uy, 0x08uy, 0x01uy )>]
    [<InlineData( 2uy, 0x08uy, 0xFEuy )>]
    [<InlineData( 2uy, 0x0Auy, 0x01uy )>]
    [<InlineData( 2uy, 0x0Auy, 0xFEuy )>]
    [<InlineData( 2uy, 0x1Cuy, 0x01uy )>]
    [<InlineData( 2uy, 0x1Cuy, 0xFEuy )>]
    [<InlineData( 2uy, 0x3Fuy, 0x01uy )>]
    [<InlineData( 2uy, 0x3Fuy, 0xFEuy )>]
    [<InlineData( 3uy, 0x08uy, 0x01uy )>]
    [<InlineData( 3uy, 0x08uy, 0xFEuy )>]
    [<InlineData( 3uy, 0x0Auy, 0x01uy )>]
    [<InlineData( 3uy, 0x0Auy, 0xFEuy )>]
    [<InlineData( 3uy, 0x1Cuy, 0x01uy )>]
    [<InlineData( 3uy, 0x1Cuy, 0xFEuy )>]
    [<InlineData( 3uy, 0x3Fuy, 0x01uy )>]
    [<InlineData( 3uy, 0x3Fuy, 0xFEuy )>]
    member _.ModeSense10_UnsupportedPaceCode_001 ( pc : byte ) ( page : byte ) ( subpage : byte ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK g_LUN1 LLBAA.T DBD.F pc page subpage 255us NACA.T
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( true,  0UL, 0uy, 0x00uy )>]
    [<InlineData( true,  0UL, 0uy, 0xFFuy )>]
    [<InlineData( true,  0UL, 1uy, 0x00uy )>]
    [<InlineData( true,  0UL, 1uy, 0xFFuy )>]
    [<InlineData( true,  0UL, 2uy, 0x00uy )>]
    [<InlineData( true,  0UL, 2uy, 0xFFuy )>]
    [<InlineData( true,  0UL, 3uy, 0x00uy )>]
    [<InlineData( true,  0UL, 3uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 0uy, 0x00uy )>]
    [<InlineData( true,  1UL, 0uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 1uy, 0x00uy )>]
    [<InlineData( true,  1UL, 1uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 2uy, 0x00uy )>]
    [<InlineData( true,  1UL, 2uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 3uy, 0x00uy )>]
    [<InlineData( true,  1UL, 3uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 0uy, 0x00uy )>]
    [<InlineData( false, 0UL, 0uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 1uy, 0x00uy )>]
    [<InlineData( false, 0UL, 1uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 2uy, 0x00uy )>]
    [<InlineData( false, 0UL, 2uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 3uy, 0x00uy )>]
    [<InlineData( false, 0UL, 3uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 0uy, 0x00uy )>]
    [<InlineData( false, 1UL, 0uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 1uy, 0x00uy )>]
    [<InlineData( false, 1UL, 1uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 2uy, 0x00uy )>]
    [<InlineData( false, 1UL, 2uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 3uy, 0x00uy )>]
    [<InlineData( false, 1UL, 3uy, 0xFFuy )>]
    member _.ModeSense10_CacheModePage_001 ( llbaa : bool ) ( lu : uint64 ) ( pc : byte ) ( subpage : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun ( LLBAA.ofBool llbaa ) DBD.F pc 0x08uy subpage 255us NACA.T
            let! res = r1.Wait_ModeSense10 itt
            Assert.True(( res.Block.IsSome ))
            Assert.True(( res.Control.IsNone ))
            Assert.True(( res.Cache.IsSome ))
            Assert.True(( res.Cache.Value.PageLength = 0x12uy ))
            Assert.True(( res.InformationalExceptionsControl.IsNone ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( true,  0UL, 0x00uy )>]
    [<InlineData( true,  0UL, 0xFFuy )>]
    [<InlineData( true,  1UL, 0x00uy )>]
    [<InlineData( true,  1UL, 0xFFuy )>]
    [<InlineData( false, 0UL, 0x00uy )>]
    [<InlineData( false, 0UL, 0xFFuy )>]
    [<InlineData( false, 1UL, 0x00uy )>]
    [<InlineData( false, 1UL, 0xFFuy )>]
    member _.ModeSense10_ControlModePage_Current_001 ( llbaa : bool ) ( lu : uint64 ) ( subpage : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // Initialize to default values
            let! itt1 = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun ( LLBAA.ofBool llbaa ) DBD.F 0x2uy 0x0Auy subpage 255us NACA.T
            let! res1 = r1.Wait_ModeSense10 itt1

            let! itt2 = r1.Send_ModeSelect10 TaskATTRCd.SIMPLE_TASK lun PF.T SP.F res1 NACA.T
            let! _ = r1.WaitSCSIResponseGoodStatus itt2

            // Get the current value
            let! itt3 = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun ( LLBAA.ofBool llbaa ) DBD.F 0x0uy 0x0Auy subpage 255us NACA.T
            let! res3 = r1.Wait_ModeSense10 itt3
            Assert.True(( res3.Block.IsSome ))
            Assert.True(( res3.Control.IsSome ))
            Assert.True(( res3.Control.Value.PageLength = 0x0Auy ))
            Assert.True(( res3.Cache.IsNone ))
            Assert.True(( res3.InformationalExceptionsControl.IsNone ))
            Assert.True(( res1 = res3 ))

            // Update the current value
            let param1 = {
                res3 with
                    Control = Some {
                        res3.Control.Value with
                            DescriptorFormatSenseData = not res3.Control.Value.DescriptorFormatSenseData;
                            SoftwareWriteProtect = not res3.Control.Value.SoftwareWriteProtect;
                    }
            }
            let! itt4 = r1.Send_ModeSelect10 TaskATTRCd.SIMPLE_TASK lun PF.T SP.F param1 NACA.T
            let! _ = r1.WaitSCSIResponseGoodStatus itt4

            // Confirm that the current value has been updated.
            let! itt5 = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun ( LLBAA.ofBool llbaa ) DBD.F 0x0uy 0x0Auy subpage 255us NACA.T
            let! res5 = r1.Wait_ModeSense10 itt5
            Assert.True(( res5.Control.Value.DescriptorFormatSenseData = param1.Control.Value.DescriptorFormatSenseData ))
            Assert.True(( res5.Control.Value.SoftwareWriteProtect = param1.Control.Value.SoftwareWriteProtect ))

            // Verify that the same value is obtained using the MODE SENSE(6) command.
            let! itt6 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F 0x0uy 0x0Auy subpage 255uy NACA.T
            let! res6 = r1.Wait_ModeSense6 itt6
            Assert.True(( res5.Control.Value = res6.Control.Value ))

            // Verify that the default values ​​have not been changed.
            let! itt7 = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun ( LLBAA.ofBool llbaa ) DBD.F 0x2uy 0x0Auy subpage 255us NACA.T
            let! res7 = r1.Wait_ModeSense10 itt7
            Assert.True(( res7.Control.Value.DescriptorFormatSenseData <> param1.Control.Value.DescriptorFormatSenseData ))
            Assert.True(( res7.Control.Value.SoftwareWriteProtect <> param1.Control.Value.SoftwareWriteProtect ))

            // Verify that the saced values ​​have not been changed.
            let! itt8 = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun ( LLBAA.ofBool llbaa ) DBD.F 0x3uy 0x0Auy subpage 255us NACA.T
            let! res8 = r1.Wait_ModeSense10 itt8
            Assert.True(( res8.Control.Value.DescriptorFormatSenseData <> param1.Control.Value.DescriptorFormatSenseData ))
            Assert.True(( res8.Control.Value.SoftwareWriteProtect <> param1.Control.Value.SoftwareWriteProtect ))

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( true,  0UL, 1uy, 0x00uy )>]
    [<InlineData( true,  0UL, 1uy, 0xFFuy )>]
    [<InlineData( true,  0UL, 2uy, 0x00uy )>]
    [<InlineData( true,  0UL, 2uy, 0xFFuy )>]
    [<InlineData( true,  0UL, 3uy, 0x00uy )>]
    [<InlineData( true,  0UL, 3uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 1uy, 0x00uy )>]
    [<InlineData( true,  1UL, 1uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 2uy, 0x00uy )>]
    [<InlineData( true,  1UL, 2uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 3uy, 0x00uy )>]
    [<InlineData( true,  1UL, 3uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 1uy, 0x00uy )>]
    [<InlineData( false, 0UL, 1uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 2uy, 0x00uy )>]
    [<InlineData( false, 0UL, 2uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 3uy, 0x00uy )>]
    [<InlineData( false, 0UL, 3uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 1uy, 0x00uy )>]
    [<InlineData( false, 1UL, 1uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 2uy, 0x00uy )>]
    [<InlineData( false, 1UL, 2uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 3uy, 0x00uy )>]
    [<InlineData( false, 1UL, 3uy, 0xFFuy )>]
    member _.ModeSense10_ControlModePage_002 ( llbaa : bool ) ( lu : uint64 ) ( pc : byte ) ( subpage : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt1 = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun ( LLBAA.ofBool llbaa ) DBD.F pc 0x0Auy subpage 255us NACA.T
            let! res1 = r1.Wait_ModeSense10 itt1
            Assert.True(( res1.Block.IsSome ))
            Assert.True(( res1.Control.IsSome ))
            Assert.True(( res1.Control.Value.PageLength = 0x0Auy ))
            Assert.True(( res1.Cache.IsNone ))
            Assert.True(( res1.InformationalExceptionsControl.IsNone ))

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( true,  0UL, 0uy, 0x00uy )>]
    [<InlineData( true,  0UL, 0uy, 0xFFuy )>]
    [<InlineData( true,  0UL, 1uy, 0x00uy )>]
    [<InlineData( true,  0UL, 1uy, 0xFFuy )>]
    [<InlineData( true,  0UL, 2uy, 0x00uy )>]
    [<InlineData( true,  0UL, 2uy, 0xFFuy )>]
    [<InlineData( true,  0UL, 3uy, 0x00uy )>]
    [<InlineData( true,  0UL, 3uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 0uy, 0x00uy )>]
    [<InlineData( true,  1UL, 0uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 1uy, 0x00uy )>]
    [<InlineData( true,  1UL, 1uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 2uy, 0x00uy )>]
    [<InlineData( true,  1UL, 2uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 3uy, 0x00uy )>]
    [<InlineData( true,  1UL, 3uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 0uy, 0x00uy )>]
    [<InlineData( false, 0UL, 0uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 1uy, 0x00uy )>]
    [<InlineData( false, 0UL, 1uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 2uy, 0x00uy )>]
    [<InlineData( false, 0UL, 2uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 3uy, 0x00uy )>]
    [<InlineData( false, 0UL, 3uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 0uy, 0x00uy )>]
    [<InlineData( false, 1UL, 0uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 1uy, 0x00uy )>]
    [<InlineData( false, 1UL, 1uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 2uy, 0x00uy )>]
    [<InlineData( false, 1UL, 2uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 3uy, 0x00uy )>]
    [<InlineData( false, 1UL, 3uy, 0xFFuy )>]
    member _.ModeSense10_InformationalExceptionsControlModePage_001 ( llbaa : bool ) ( lu : uint64 ) ( pc : byte ) ( subpage : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun ( LLBAA.ofBool llbaa ) DBD.F pc 0x1Cuy subpage 255us NACA.T
            let! res = r1.Wait_ModeSense10 itt
            Assert.True(( res.Block.IsSome ))
            Assert.True(( res.Control.IsNone ))
            Assert.True(( res.Cache.IsNone ))
            Assert.True(( res.InformationalExceptionsControl.IsSome ))
            Assert.True(( res.InformationalExceptionsControl.Value.PageLength = 0x0Auy ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( true,  0UL, 0uy, 0x00uy )>]
    [<InlineData( true,  0UL, 0uy, 0xFFuy )>]
    [<InlineData( true,  0UL, 1uy, 0x00uy )>]
    [<InlineData( true,  0UL, 1uy, 0xFFuy )>]
    [<InlineData( true,  0UL, 2uy, 0x00uy )>]
    [<InlineData( true,  0UL, 2uy, 0xFFuy )>]
    [<InlineData( true,  0UL, 3uy, 0x00uy )>]
    [<InlineData( true,  0UL, 3uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 0uy, 0x00uy )>]
    [<InlineData( true,  1UL, 0uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 1uy, 0x00uy )>]
    [<InlineData( true,  1UL, 1uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 2uy, 0x00uy )>]
    [<InlineData( true,  1UL, 2uy, 0xFFuy )>]
    [<InlineData( true,  1UL, 3uy, 0x00uy )>]
    [<InlineData( true,  1UL, 3uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 0uy, 0x00uy )>]
    [<InlineData( false, 0UL, 0uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 1uy, 0x00uy )>]
    [<InlineData( false, 0UL, 1uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 2uy, 0x00uy )>]
    [<InlineData( false, 0UL, 2uy, 0xFFuy )>]
    [<InlineData( false, 0UL, 3uy, 0x00uy )>]
    [<InlineData( false, 0UL, 3uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 0uy, 0x00uy )>]
    [<InlineData( false, 1UL, 0uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 1uy, 0x00uy )>]
    [<InlineData( false, 1UL, 1uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 2uy, 0x00uy )>]
    [<InlineData( false, 1UL, 2uy, 0xFFuy )>]
    [<InlineData( false, 1UL, 3uy, 0x00uy )>]
    [<InlineData( false, 1UL, 3uy, 0xFFuy )>]
    member _.ModeSense10_AllPages_001 ( llbaa : bool ) ( lu : uint64 ) ( pc : byte ) ( subpage : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ModeSense10 TaskATTRCd.SIMPLE_TASK lun ( LLBAA.ofBool llbaa ) DBD.F pc 0x3Fuy subpage 255us NACA.T
            let! res = r1.Wait_ModeSense10 itt
            Assert.True(( res.Block.IsSome ))
            Assert.True(( res.Control.IsSome ))
            Assert.True(( res.Control.Value.PageLength = 0x0Auy ))
            Assert.True(( res.Cache.IsSome ))
            Assert.True(( res.Cache.Value.PageLength = 0x12uy ))
            Assert.True(( res.InformationalExceptionsControl.IsSome ))
            Assert.True(( res.InformationalExceptionsControl.Value.PageLength = 0x0Auy ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL  )>]
    member _.ModeSense10_LINK_001 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let cdb = GenScsiCDB.ModeSense10 LLBAA.T DBD.F 0uy 0x3Fuy 0uy 255us NACA.T LINK.T
            let! itt_pf1 = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK lun cdb PooledBuffer.Empty 255u
            let! res_pf1 = r1.WaitSCSIResponse itt_pf1
            Assert.True(( res_pf1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 lun

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.ModeSelect6_001 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_sense1 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F 0x00uy 0x3Fuy 0x00uy 255uy NACA.T
            let! mp1 = r1.Wait_ModeSense6 itt_sense1

            let paramBytes = GenScsiParams.ModeSelect6 mp1
            let cdb = GenScsiCDB.ModeSelect6 PF.T SP.T ( byte paramBytes.Length ) NACA.T LINK.F
            let! itt_select = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK lun cdb paramBytes paramBytes.uLength
            let! _ = r1.WaitSCSIResponseGoodStatus itt_select
            paramBytes.Return()

            let! itt_sense2 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F 0x00uy 0x3Fuy 0x00uy 255uy NACA.T
            let! mp2 = r1.Wait_ModeSense6 itt_sense2
            Assert.True(( mp2 = mp1 ))

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0, false )>]
    [<InlineData( 4,  false )>]
    [<InlineData( 11, false )>]
    [<InlineData( 12, true  )>]
    member _.ModeSelect6_Block_InvalidLength_001 ( paramlen : int ) ( exp : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_sense1 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK g_LUN1 DBD.F 0x00uy 0x3Fuy 0x00uy 255uy NACA.T
            let! mp1 = r1.Wait_ModeSense6 itt_sense1

            let paramBytes1 = GenScsiParams.ModeSelect6 mp1
            let paramBytes2 = PooledBuffer.Rent( paramBytes1.Array.[ 0 .. paramlen - 1 ] )
            let cdb = GenScsiCDB.ModeSelect6 PF.T SP.T ( byte paramBytes2.Length ) NACA.T LINK.F
            let! itt_select = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb paramBytes2 paramBytes2.uLength

            let! res = r1.WaitSCSIResponse itt_select
            if exp then
                Assert.True(( res.Status = ScsiCmdStatCd.GOOD ))
            else
                Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                do! ClearACA r1 g_LUN1

            do! r1.Close()
        }

    [<Fact>]
    member _.ModeSelect6_Block_InvalidUpdate_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_sense1 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK g_LUN1 DBD.F 0x00uy 0x3Fuy 0x00uy 255uy NACA.T
            let! mp1 = r1.Wait_ModeSense6 itt_sense1
            let mp2 = {
                mp1 with
                    Block = Some {
                        mp1.Block.Value with
                            BlockCount = blkcnt_me.ofUInt64 1UL;
                            BlockLength = 1u;
                    };
                    Control = None;
                    Cache = None;
                    InformationalExceptionsControl = None;
            }

            let paramBytes1 = GenScsiParams.ModeSelect6 mp2
            let cdb = GenScsiCDB.ModeSelect6 PF.T SP.T ( byte paramBytes1.Length ) NACA.T LINK.F
            let! itt_select = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb paramBytes1 paramBytes1.uLength
            let! res = r1.WaitSCSIResponse itt_select
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 4,  true  )>]
    [<InlineData( 5,  false )>]
    [<InlineData( 15, false )>]
    [<InlineData( 16, true  )>]
    member _.ModeSelect6_Control_InvalidLength_001 ( paramlen : int ) ( exp : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_sense1 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK g_LUN1 DBD.F 0x00uy 0x3Fuy 0x00uy 255uy NACA.T
            let! mp1 = r1.Wait_ModeSense6 itt_sense1
            let mp2 = {
                mp1 with
                    Block = None;
                    Cache = None;
                    InformationalExceptionsControl = None;
            }

            let paramBytes1 = GenScsiParams.ModeSelect6 mp2
            let paramBytes2 = PooledBuffer.Rent( paramBytes1.Array.[ 0 .. paramlen - 1 ] )
            let cdb = GenScsiCDB.ModeSelect6 PF.T SP.T ( byte paramBytes2.Length ) NACA.T LINK.F
            let! itt_select = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb paramBytes2 paramBytes2.uLength

            let! res = r1.WaitSCSIResponse itt_select
            if exp then
                Assert.True(( res.Status = ScsiCmdStatCd.GOOD ))
            else
                Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                do! ClearACA r1 g_LUN1

            do! r1.Close()
        }

    [<Fact>]
    member _.ModeSelect6_Control_InvalidUpdate_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_sense1 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK g_LUN1 DBD.F 0x00uy 0x3Fuy 0x00uy 255uy NACA.T
            let! mp1 = r1.Wait_ModeSense6 itt_sense1
            let cv = mp1.Control.Value
            let mp2 = {
                mp1 with
                    Block = None;
                    Control = Some {
                        cv with
                            TaskSetType = ~~~ cv.TaskSetType;
                            AllowTaskManagementFunctionOnly = not cv.AllowTaskManagementFunctionOnly;
                            DescriptorFormatSenseData = not cv.DescriptorFormatSenseData;
                            GlobalLoggingTargetSaveDisable = not cv.GlobalLoggingTargetSaveDisable;
                            ReportLogExceptionCondition = not cv.ReportLogExceptionCondition;
                            QueueAlgorithmModifier = ~~~ cv.QueueAlgorithmModifier;
                            QueueErrorManagement = ~~~ cv.QueueErrorManagement;
                            ReportACheck = not cv.ReportACheck;
                            UnitAttentionInterlocksControl = ~~~ cv.UnitAttentionInterlocksControl;
                            SoftwareWriteProtect = not cv.SoftwareWriteProtect;
                            ApplicationTagOwner = not cv.ApplicationTagOwner;
                            TaskAbortedStatus = not cv.TaskAbortedStatus;
                            AutoLoadMode = ~~~ cv.AutoLoadMode;
                            BusyTimeOutPeriod = ~~~ cv.BusyTimeOutPeriod;
                            ExtendedSelfTestCompletionTime = ~~~ cv.ExtendedSelfTestCompletionTime;
                    }
                    Cache = None;
                    InformationalExceptionsControl = None;
            }

            let paramBytes1 = GenScsiParams.ModeSelect6 mp2
            let cdb = GenScsiCDB.ModeSelect6 PF.T SP.T ( byte paramBytes1.Length ) NACA.T LINK.F
            let! itt_select = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb paramBytes1 paramBytes1.uLength
            let! res = r1.WaitSCSIResponse itt_select
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 4,  true  )>]
    [<InlineData( 5,  false )>]
    [<InlineData( 23, false )>]
    [<InlineData( 24, true  )>]
    member _.ModeSelect6_Cache_InvalidLength_001 ( paramlen : int ) ( exp : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_sense1 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK g_LUN1 DBD.F 0x00uy 0x3Fuy 0x00uy 255uy NACA.T
            let! mp1 = r1.Wait_ModeSense6 itt_sense1
            let mp2 = {
                mp1 with
                    Block = None;
                    Control = None;
                    InformationalExceptionsControl = None;
            }

            let paramBytes1 = GenScsiParams.ModeSelect6 mp2
            let paramBytes2 = PooledBuffer.Rent( paramBytes1.Array.[ 0 .. paramlen - 1 ] )
            let cdb = GenScsiCDB.ModeSelect6 PF.T SP.T ( byte paramBytes2.Length ) NACA.T LINK.F
            let! itt_select = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb paramBytes2 paramBytes2.uLength

            let! res = r1.WaitSCSIResponse itt_select
            if exp then
                Assert.True(( res.Status = ScsiCmdStatCd.GOOD ))
            else
                Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                do! ClearACA r1 g_LUN1

            do! r1.Close()
        }

    [<Fact>]
    member _.ModeSelect6_Cache_InvalidUpdate_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_sense1 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK g_LUN1 DBD.F 0x00uy 0x3Fuy 0x00uy 255uy NACA.T
            let! mp1 = r1.Wait_ModeSense6 itt_sense1
            let cv = mp1.Cache.Value
            let mp2 = {
                mp1 with
                    Block = None;
                    Control = None;
                    Cache = Some {
                        cv with
                            InitiatorControl = not cv.InitiatorControl;
                            AbortPreFetch = not cv.AbortPreFetch;
                            CachingAnalysisPermitted = not cv.CachingAnalysisPermitted;
                            Discontinuity = not cv.Discontinuity;
                            SizeEnable = not cv.SizeEnable;
                            WritebackCacheEnable = not cv.WritebackCacheEnable;
                            MultiplicationFactor = not cv.MultiplicationFactor;
                            ReadCacheDisable = not cv.ReadCacheDisable;
                            DemandReadRetentionPriority = ~~~ cv.DemandReadRetentionPriority;
                            WriteRetentionPriority = ~~~ cv.WriteRetentionPriority;
                            ForceSequentialWrite = not cv.ForceSequentialWrite;
                            LogicalBlockCacheSegmentSize = not cv.LogicalBlockCacheSegmentSize;
                            DisableReadAhead = not cv.DisableReadAhead;
                            NonVolatileDisabled = not cv.NonVolatileDisabled;
                            NumberOfCacheSegments = ~~~ cv.NumberOfCacheSegments;
                            CacheSegmentSize = ~~~ cv.CacheSegmentSize;
                    };
                    InformationalExceptionsControl = None;
            }

            let paramBytes1 = GenScsiParams.ModeSelect6 mp2
            let cdb = GenScsiCDB.ModeSelect6 PF.T SP.T ( byte paramBytes1.Length ) NACA.T LINK.F
            let! itt_select = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb paramBytes1 paramBytes1.uLength

            let! res = r1.WaitSCSIResponse itt_select
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 4,  true  )>]
    [<InlineData( 5,  false )>]
    [<InlineData( 15, false )>]
    [<InlineData( 16, true  )>]
    member _.ModeSelect6_InformationalExceptionsControl_InvalidLength_001 ( paramlen : int ) ( exp : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_sense1 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK g_LUN1 DBD.F 0x00uy 0x3Fuy 0x00uy 255uy NACA.T
            let! mp1 = r1.Wait_ModeSense6 itt_sense1
            let mp2 = {
                mp1 with
                    Block = None;
                    Cache = None;
                    Control = None;
            }

            let paramBytes1 = GenScsiParams.ModeSelect6 mp2
            let paramBytes2 = PooledBuffer.Rent( paramBytes1.Array.[ 0 .. paramlen - 1 ] )
            let cdb = GenScsiCDB.ModeSelect6 PF.T SP.T ( byte paramBytes2.Length ) NACA.T LINK.F
            let! itt_select = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb paramBytes2 paramBytes2.uLength

            let! res = r1.WaitSCSIResponse itt_select
            if exp then
                Assert.True(( res.Status = ScsiCmdStatCd.GOOD ))
            else
                Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                do! ClearACA r1 g_LUN1

            do! r1.Close()
        }

    [<Fact>]
    member _.ModeSelect6_InformationalExceptionsControl_InvalidUpdate_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_sense1 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK g_LUN1 DBD.F 0x00uy 0x3Fuy 0x00uy 255uy NACA.T
            let! mp1 = r1.Wait_ModeSense6 itt_sense1
            let cv = mp1.InformationalExceptionsControl.Value
            let mp2 = {
                mp1 with
                    Block = None;
                    Cache = None;
                    Control = None;
                    InformationalExceptionsControl = Some {
                        cv with
                            Performance = not cv.Performance;
                            EnableBackgroundFunction = not cv.EnableBackgroundFunction;
                            EnableWarning = not cv.EnableWarning;
                            DisableExceptionControl = not cv.DisableExceptionControl;
                            Test = not cv.Test;
                            LogError = not cv.LogError;
                            MethodOfReportingInformationalExceptions = ~~~ cv.MethodOfReportingInformationalExceptions;
                            IntervalTimer = ~~~ cv.IntervalTimer;
                            ReportCount = ~~~ cv.ReportCount;
                    }
            }

            let paramBytes1 = GenScsiParams.ModeSelect6 mp2
            let cdb = GenScsiCDB.ModeSelect6 PF.T SP.T ( byte paramBytes1.Length ) NACA.T LINK.F
            let! itt_select = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb paramBytes1 paramBytes1.uLength
            let! res = r1.WaitSCSIResponse itt_select
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1

            do! r1.Close()
        }
