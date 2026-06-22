//=============================================================================
// Haruka Software Storage.
// MemBufferMedia.fs : Test cases for memory buffer media.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.IT.SCSI

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading.Tasks

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

//=============================================================================
// Class implementation

[<CollectionDefinition( "SCSI_MemBufferMedia" )>]
type SCSI_MemBufferMedia_Fixture() =

    static let m_BlockSize1 = 512
    static let m_MediaSize1 = uint32 m_BlockSize1 * 1048576u    // 512MB

    static let m_BlockSize2 = 4096
    static let m_MediaSize2 = uint32 m_BlockSize2 * 131072u     // 512MB

    let m_iSCSIPortNo = GlbFunc.nextTcpPortNo()

    // Add default configurations
    let AddDefaultConf( client : ClientProc ): unit =

        // Target device, Target group
        client.RunCommand "create" "Created" "CR> "
        client.RunCommand "select 0" "" "TD> "
        client.RunCommand "set Name targetDevice001" "" "TD> "
        client.RunCommand "set ID TD_00000063" "" "TD> "
        client.RunCommand "set loglevel VERBOSE" "" "TD> "
        client.RunCommand ( sprintf "set MAXBURSTLENGTH %d" ( Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength + 101u ) ) "" "TD> "
        client.RunCommand "create targetgroup" "Created" "TD> "
        client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo ) "Created" "TD> "
        client.RunCommand "select 0" "" "TG> "

        // Target1, LU1, LU2
        client.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        client.RunCommand "select 0" "" "T > "
        client.RunCommand "set ID 1" "" "T > "
        client.RunCommand "create /l 1" "Created" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand ( sprintf "create membuffer %d" m_MediaSize1 ) "Created" "LU> "
        client.RunCommand "select 0" "" "MD> "
        client.RunCommand ( sprintf "set BlockSize %d" m_BlockSize1 ) "" "MD> "
        client.RunCommand "unselect" "" "LU> "
        client.RunCommand "unselect" "" "T > "
        client.RunCommand "create /l 2" "Created" "T > "
        client.RunCommand "select 1" "" "LU> "
        client.RunCommand ( sprintf "create membuffer %d" m_MediaSize2 ) "Created" "LU> "
        client.RunCommand "select 0" "" "MD> "
        client.RunCommand ( sprintf "set BlockSize %d" m_BlockSize2 ) "" "MD> "
        client.RunCommand "unselect" "" "LU> "
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
        MaxBurstLength = Constants.NEGOPARAM_MIN_MaxBurstLength + 997u;
        FirstBurstLength = Constants.NEGOPARAM_MIN_FirstBurstLength + 509u;
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
        PortNo = m_iSCSIPortNo;
        CID = cid_me.zero;
        Initiator_UserName = "";
        Initiator_Password = "";
        Target_UserName = "";
        Target_Password = "";
        HeaderDigest = DigestType.DST_CRC32C;
        DataDigest = DigestType.DST_CRC32C;
        MaxRecvDataSegmentLength_I = Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength + 97u;
        MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_MIN_MaxRecvDataSegmentLength + 101u;
    }

    interface IDisposable with
        member _.Dispose (): unit =
            m_Client.Kill()

    interface ICollectionFixture<SCSI_MemBufferMedia_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.iSCSIPortNo = m_iSCSIPortNo
    member _.WorkPath = m_WorkPath
    member _.DefaultSessParam = m_defaultSessParam
    member _.DefaultConnParam = m_defaultConnParam

    static member MediaSize1 = m_MediaSize1
    static member BlockSize1 = if m_BlockSize1 = 512 then Blocksize.BS_512 else Blocksize.BS_4096

    static member MediaSize2 = m_MediaSize2
    static member BlockSize2 = if m_BlockSize2 = 512 then Blocksize.BS_512 else Blocksize.BS_4096

[<Collection( "SCSI_MemBufferMedia" )>]
type SCSI_MemBufferMedia( fx : SCSI_MemBufferMedia_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_LUN1 = lun_me.fromPrim 1UL
    let g_LUN2 = lun_me.fromPrim 2UL

    let m_ClientProc = fx.clientProc
    let iSCSIPortNo = fx.iSCSIPortNo
    let m_WorkPath = fx.WorkPath
    let m_defaultSessParam = fx.DefaultSessParam
    let m_defaultConnParam = fx.DefaultConnParam

    static let m_MediaSize1 = SCSI_MemBufferMedia_Fixture.MediaSize1
    static let m_BlockSize1 = SCSI_MemBufferMedia_Fixture.BlockSize1
    static let m_BlockCount1 = m_MediaSize1 / ( Blocksize.toUInt32 m_BlockSize1 )

    static let m_MediaSize2 = SCSI_MemBufferMedia_Fixture.MediaSize2
    static let m_BlockSize2 = SCSI_MemBufferMedia_Fixture.BlockSize2
    static let m_BlockCount2 = m_MediaSize2 / ( Blocksize.toUInt32 m_BlockSize2 )

    let ClearACA ( r : SCSI_Initiator ) ( lun : LUN_T ) : Task<unit> =
        task {
            let! itt_tmf1 = r.SendTMFRequest_ClearACA BitI.F lun
            let! res_tmf1 = r.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))
        }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    static member ReadWrite_001_data : obj[][] = [|
        [| 1UL; 0u; 1   |]
        [| 1UL; 0u; 16  |]
        [| 1UL; 0u; 256 |]
        [| 1UL; ( uint32 Constants.MEMBUFFER_BUF_LINE_SIZE / 512u ) - 4u; 4 |]
        [| 1UL; ( uint32 Constants.MEMBUFFER_BUF_LINE_SIZE / 512u ) - 2u; 4 |]
        [| 1UL; ( uint32 Constants.MEMBUFFER_BUF_LINE_SIZE / 512u );      4 |]
        [| 1UL; m_BlockCount1 - 1u;   1   |]
        [| 1UL; m_BlockCount1 - 16u;  16  |]
        [| 1UL; m_BlockCount1 - 256u; 256 |]
        [| 2UL; 0u; 1   |]
        [| 2UL; 0u; 16  |]
        [| 2UL; 0u; 256 |]
        [| 2UL; ( uint32 Constants.MEMBUFFER_BUF_LINE_SIZE / 4096u ) - 4u; 4 |]
        [| 2UL; ( uint32 Constants.MEMBUFFER_BUF_LINE_SIZE / 4096u ) - 2u; 4 |]
        [| 2UL; ( uint32 Constants.MEMBUFFER_BUF_LINE_SIZE / 4096u )     ; 4 |]
        [| 2UL; m_BlockCount2 - 1u;   1   |]
        [| 2UL; m_BlockCount2 - 16u;  16  |]
        [| 2UL; m_BlockCount2 - 256u; 256 |]
    |]

    [<Theory>]
    [<MemberData( "ReadWrite_001_data" )>]
    member _.ReadWrite_001 ( lu : uint64 ) ( lba : uint32 ) ( blkcnt : int ) =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let lun = lun_me.fromPrim lu

            let v = PooledBuffer.Rent ( int ( Blocksize.toUInt32 m_BlockSize1 ) * blkcnt )
            Random.Shared.NextBytes( v.ArraySegment )
            let! itt_w = r.Send_Write10 TaskATTRCd.SIMPLE_TASK lun ( blkcnt_me.ofUInt32 lba ) m_BlockSize1 v NACA.T 
            let! _ = r.WaitSCSIResponseGoodStatus itt_w

            let! itt_r = r.Send_Read10 TaskATTRCd.SIMPLE_TASK lun ( blkcnt_me.ofUInt32 lba ) m_BlockSize1 ( blkcnt_me.ofUInt16( uint16 blkcnt ) ) NACA.T 
            let! v2 = r.WaitSCSIResponseGoodStatus itt_r

            Assert.True(( PooledBuffer.ValueEquals v v2 ))

            do! r.Close()
        }

    [<Fact>]
    member _.Format_001 () =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let v = PooledBuffer.Rent ( int ( Blocksize.toUInt32 m_BlockSize1 ) )
            Random.Shared.NextBytes( v.ArraySegment )
            let! itt_w = r.Send_Write10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_BlockSize1 v NACA.T
            let! _ = r.WaitSCSIResponseGoodStatus itt_w

            // Format
            let! itt_f = r.Send_FormatUnit TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! _ = r.WaitSCSIResponseGoodStatus itt_f

            // All data will be erased.
            let! itt_r = r.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_BlockSize1 ( blkcnt_me.ofUInt16 1us ) NACA.T 
            let! v2 = r.WaitSCSIResponseGoodStatus itt_r
            for i = 0 to v2.Length - 1 do
                Assert.StrictEqual( 0uy, v2.Array.[i] )

            do! r.Close()
        }

    [<Fact>]
    member _.LUReset_001 () =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let v = PooledBuffer.Rent ( int ( Blocksize.toUInt32 m_BlockSize1 ) )
            Random.Shared.NextBytes( v.ArraySegment )
            let! itt_w = r.Send_Write10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_BlockSize1 v NACA.T
            let! _ = r.WaitSCSIResponseGoodStatus itt_w

            // LU Reset
            let! itt_tmf = r.SendTMFRequest_LogicalUnitReset BitI.F g_LUN1
            let! res_tmf = r.WaitTMFResponse itt_tmf
            Assert.StrictEqual( TaskMgrResCd.FUNCTION_COMPLETE, res_tmf )

            // All data will be erased.
            let! itt_r = r.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 blkcnt_me.zero32 m_BlockSize1 ( blkcnt_me.ofUInt16 1us ) NACA.T 
            let! v2 = r.WaitSCSIResponseGoodStatus itt_r
            for i = 0 to v2.Length - 1 do
                Assert.StrictEqual( 0uy, v2.Array.[i] )

            do! r.Close()
        }

    static member ReadCapacity_001_data : obj[][] = [|
        [| 1UL; m_BlockSize1; m_BlockCount1 |]
        [| 2UL; m_BlockSize2; m_BlockCount2 |]
    |]

    [<Theory>]
    [<MemberData( "ReadCapacity_001_data" )>]
    member _.ReadCapacity_001 ( lu : uint64 ) ( expBlockSize : Blocksize ) ( expBlockCount : uint32 ) =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let lun = lun_me.fromPrim lu

            let! itt = r.Send_ReadCapacity10 TaskATTRCd.SIMPLE_TASK lun NACA.T
            let! struct( bc, bs ) = r.Wait_ReadCapacity10 itt

            Assert.StrictEqual( Blocksize.toUInt32 expBlockSize, bs )
            Assert.StrictEqual( expBlockCount - 1u, bc )

            do! r.Close()
        }

