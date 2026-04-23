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

    static let m_MediaSize = SCSI_Commands02_Fixture.MediaSize
    static let m_MediaBlockSize = SCSI_Commands02_Fixture.MediaBlockSize
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
    [<InlineData( 0UL, 0u,  0,  0 )>]
    [<InlineData( 0UL, 4u,  4,  0 )>]
    [<InlineData( 0UL, 8u,  8,  0 )>]
    [<InlineData( 0UL, 12u, 12, 1 )>]
    member _.ReportSupportedOperationCodes_AllCommand_001 ( lu : uint64 ) ( allen : uint32 ) ( exlen : int ) ( cdbcnt : int ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ReportSupportedOperationCodes TaskATTRCd.SIMPLE_TASK lun 0uy 0uy 0us allen NACA.T
            let! res = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( res.Length = exlen ))

            let r = GenScsiParams.ReportSupportedOperationCodes_AllCommand res
            if exlen > 0 then
                Assert.True(( r.CommandDataLength > 0u ))
            else
                Assert.True(( r.CommandDataLength = 0u ))
            Assert.True(( r.Descs.Length = cdbcnt ))

            do! r1.Close()
        }

    [<Fact>]
    member _.ReportSupportedOperationCodes_OneCommand_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt1 = r1.Send_ReportSupportedOperationCodes TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 0uy 0us 1024u NACA.T
            let! opcodes = r1.Wait_ReportSupportedOperationCodes_AllCommand itt1

            for itr in opcodes.Descs do
                let! itt2 =
                    if itr.ServiceActionValid then
                        r1.Send_ReportSupportedOperationCodes TaskATTRCd.SIMPLE_TASK g_LUN1 2uy itr.OperationCode itr.ServiceAction 1024u NACA.T
                    else
                        r1.Send_ReportSupportedOperationCodes TaskATTRCd.SIMPLE_TASK g_LUN1 1uy itr.OperationCode 0us 1024u NACA.T
                let! res = r1.Wait_ReportSupportedOperationCodes_OneCommand itt2
                Assert.True(( res.Support = 3uy ))

            do! r1.Close()
        }

    [<Fact>]
    member _.ReportSupportedOperationCodes_OneCommand_UnsupportedOpCode_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // unknown operation code
            let! itt1 = r1.Send_ReportSupportedOperationCodes TaskATTRCd.SIMPLE_TASK g_LUN1 1uy 0xFFuy 0us 1024u NACA.T
            let! res = r1.Wait_ReportSupportedOperationCodes_OneCommand itt1
            Assert.True(( res.Support = 1uy ))

            do! r1.Close()
        }

    [<Fact>]
    member _.ReportSupportedOperationCodes_OneCommand_UnsupportedOpCode_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // operation code that has service action ( 0x5Euy : PERSISTENT RESERVE IN )
            let! itt1 = r1.Send_ReportSupportedOperationCodes TaskATTRCd.SIMPLE_TASK g_LUN1 1uy 0x5Euy 0us 1024u NACA.T
            let! res = r1.WaitSCSIResponse itt1
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    [<Fact>]
    member _.ReportSupportedOperationCodes_OneCommand_UnsupportedOpCode_003 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // unknown operation code
            let! itt1 = r1.Send_ReportSupportedOperationCodes TaskATTRCd.SIMPLE_TASK g_LUN1 2uy 0xFFuy 0us 1024u NACA.T
            let! res = r1.Wait_ReportSupportedOperationCodes_OneCommand itt1
            Assert.True(( res.Support = 1uy ))

            do! r1.Close()
        }

    [<Fact>]
    member _.ReportSupportedOperationCodes_OneCommand_UnsupportedOpCode_004 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            // operation code that has no service action ( 0x12uy : INQUIRY )
            let! itt1 = r1.Send_ReportSupportedOperationCodes TaskATTRCd.SIMPLE_TASK g_LUN1 2uy 0x12uy 0us 1024u NACA.T
            let! res = r1.WaitSCSIResponse itt1
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    [<Fact>]
    member _.ReportSupportedOperationCodes_LINK_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let cdb = GenScsiCDB.ReportSupportedOperationCodes 0uy 0uy 0us 1024u NACA.T LINK.T
            let! itt = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb PooledBuffer.Empty 1024u
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0u )>]
    [<InlineData( 3u )>]
    member _.ReportSupportedTaskManagementFunctions_001 ( allen : uint32 ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt1 = r1.Send_ReportSupportedTaskManagementFunctions TaskATTRCd.SIMPLE_TASK g_LUN1 allen NACA.T
            let! res = r1.WaitSCSIResponse itt1
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( res.Sense.Value.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.ReportSupportedTaskManagementFunctions_002 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt1 = r1.Send_ReportSupportedTaskManagementFunctions TaskATTRCd.SIMPLE_TASK lun 4u NACA.T
            let! res = r1.Wait_ReportSupportedTaskManagementFunctions itt1
            Assert.True(( res.AbortTaskSupported ))
            Assert.True(( res.AbortTaskSetSupported ))
            Assert.True(( res.ClearACASupported ))
            Assert.True(( res.ClearTaskSetSupported ))
            Assert.True(( res.LUResetSupported ))
            Assert.False(( res.QueryTaskSupported ))
            Assert.False(( res.TargetResetSupported ))
            Assert.False(( res.WakeupSupported ))

            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    [<Fact>]
    member _.ReportSupportedTaskManagementFunctions_003 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let cdb = GenScsiCDB.ReportSupportedTaskManagementFunctions 1024u NACA.T LINK.T
            let! itt = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb PooledBuffer.Empty 1024u
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }



