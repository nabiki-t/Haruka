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
    [<InlineData( 0UL, 3uy, 0x00uy )>]
    [<InlineData( 0UL, 3uy, 0xFFuy )>]
    [<InlineData( 1UL, 0uy, 0x00uy )>]
    [<InlineData( 1UL, 0uy, 0xFFuy )>]
    [<InlineData( 1UL, 3uy, 0x00uy )>]
    [<InlineData( 1UL, 3uy, 0xFFuy )>]
    member _.ModeSense6_CacheModePage_Current_001 ( lu : uint64 ) ( pc : byte ) ( subpage : byte ) =
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
    [<InlineData( 0UL, 0uy, 0x00uy )>]
    [<InlineData( 0UL, 0uy, 0xFFuy )>]
    [<InlineData( 0UL, 3uy, 0x00uy )>]
    [<InlineData( 0UL, 3uy, 0xFFuy )>]
    [<InlineData( 1UL, 0uy, 0x00uy )>]
    [<InlineData( 1UL, 0uy, 0xFFuy )>]
    [<InlineData( 1UL, 3uy, 0x00uy )>]
    [<InlineData( 1UL, 3uy, 0xFFuy )>]
    member _.ModeSense6_ControlModePage_Current_001 ( lu : uint64 ) ( pc : byte ) ( subpage : byte ) =
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

            let param1 = {
                res1 with
                    Control = Some {
                        res1.Control.Value with
                            DescriptorFormatSenseData = not res1.Control.Value.DescriptorFormatSenseData;
                            SoftwareWriteProtect = not res1.Control.Value.SoftwareWriteProtect;
                    }
            }
            let! itt2 = r1.Send_ModeSelect6 TaskATTRCd.SIMPLE_TASK lun PF.T SP.F param1 NACA.T
            let! _ = r1.WaitSCSIResponseGoodStatus itt2

            let! itt3 = r1.Send_ModeSense6 TaskATTRCd.SIMPLE_TASK lun DBD.F pc 0x0Auy subpage 255uy NACA.T
            let! res3 = r1.Wait_ModeSense6 itt3
            Assert.True(( res3.Control.Value.DescriptorFormatSenseData = param1.Control.Value.DescriptorFormatSenseData ))
            Assert.True(( res3.Control.Value.SoftwareWriteProtect = param1.Control.Value.SoftwareWriteProtect ))

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0uy, 0x00uy )>]
    [<InlineData( 0UL, 0uy, 0xFFuy )>]
    [<InlineData( 0UL, 3uy, 0x00uy )>]
    [<InlineData( 0UL, 3uy, 0xFFuy )>]
    [<InlineData( 1UL, 0uy, 0x00uy )>]
    [<InlineData( 1UL, 0uy, 0xFFuy )>]
    [<InlineData( 1UL, 3uy, 0x00uy )>]
    [<InlineData( 1UL, 3uy, 0xFFuy )>]
    member _.ModeSense6_InformationalExceptionsControlModePage_Current_001 ( lu : uint64 ) ( pc : byte ) ( subpage : byte ) =
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
