//=============================================================================
// Haruka Software Storage.
// Read.fs : Test cases for READ commands.
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

[<CollectionDefinition( "SCSI_Read" )>]
type SCSI_Read_Fixture() =

    static let m_MediaSize = uint32 Constants.MEDIA_BLOCK_SIZE * 256u
    static let m_MediaBlockSize = 
        if Constants.MEDIA_BLOCK_SIZE = 512UL then     // 4096 or 512 bytes
            Blocksize.BS_512
        else
            Blocksize.BS_4096

    let m_iSCSIPortNo = GlbFunc.nextTcpPortNo()

    // Add default configurations
    let AddDefaultConf( client : ClientProc ): unit =

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
        PortNo = m_iSCSIPortNo;
        CID = cid_me.zero;
        Initiator_UserName = "";
        Initiator_Password = "";
        Target_UserName = "";
        Target_Password = "";
        HeaderDigest = DigestType.DST_CRC32C;
        DataDigest = DigestType.DST_CRC32C;
        MaxRecvDataSegmentLength_I = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
        MaxRecvDataSegmentLength_T = Constants.NEGOPARAM_DEF_MaxRecvDataSegmentLength;
    }

    // Write random data to media.
    let m_FilledData =
        let buf = PooledBuffer.Rent( int m_MediaSize )
        Random.Shared.NextBytes buf.ArraySegment
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r.Send_Write10 TaskATTRCd.SIMPLE_TASK ( lun_me.fromPrim 1UL ) blkcnt_me.zero32 m_MediaBlockSize buf NACA.T
            let! _ = r.WaitSCSIResponseGoodStatus itt
            do! r.Close()
        }
        |> Functions.RunTaskSynchronously
        let rdata = buf.GetPartialBytes 0 buf.Length
        buf.Return()
        rdata

    interface IDisposable with
        member _.Dispose (): unit =
            m_Client.Kill()

    interface ICollectionFixture<SCSI_Read_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.iSCSIPortNo = m_iSCSIPortNo
    member _.WorkPath = m_WorkPath
    member _.DefaultSessParam = m_defaultSessParam
    member _.DefaultConnParam = m_defaultConnParam
    member _.FilledData = m_FilledData

    static member MediaSize = m_MediaSize
    static member MediaBlockSize = m_MediaBlockSize

[<Collection( "SCSI_Read" )>]
type SCSI_Read( fx : SCSI_Read_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_LUN1 = lun_me.fromPrim 1UL

    let m_ClientProc = fx.clientProc
    let iSCSIPortNo = fx.iSCSIPortNo
    let m_WorkPath = fx.WorkPath
    let m_defaultSessParam = fx.DefaultSessParam
    let m_defaultConnParam = fx.DefaultConnParam
    let m_FilledData = fx.FilledData

    static let m_MediaSize = SCSI_Read_Fixture.MediaSize
    static let m_MediaBlockSize = SCSI_Read_Fixture.MediaBlockSize
    static let m_MediaBlockCount = m_MediaSize / ( Blocksize.toUInt32 m_MediaBlockSize )

    let ClearACA ( r : SCSI_Initiator ) ( lun : LUN_T ) : Task<unit> =
        task {
            let! itt_tmf1 = r.SendTMFRequest_ClearACA BitI.F lun
            let! res_tmf1 = r.WaitTMFResponse itt_tmf1
            Assert.True(( res_tmf1 = TaskMgrResCd.FUNCTION_COMPLETE ))
        }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    static member Read6_001_data : obj[][] = [|
        [| 0u;                     0uy;   256u; 0u;                     |]
        [| 0u;                     1uy;   1u;   0u;                     |]
        [| 0u;                     255uy; 255u; 0u;                     |]
        [| m_MediaBlockCount - 1u; 1uy;   1u;   m_MediaBlockCount - 1u; |]
    |]

    [<Theory>]
    [<MemberData( "Read6_001_data" )>]
    member _.Read6_001 ( lba : uint32 ) ( transLen : byte ) ( exBlockCnt : uint32 ) ( exStartBlkPos : uint32 ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_read = r1.Send_Read6 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 lba ) m_MediaBlockSize ( blkcnt_me.ofUInt8 transLen ) NACA.T
            let! readData = r1.WaitSCSIResponseGoodStatus itt_read

            let exBytesLen = Blocksize.toUInt32 m_MediaBlockSize * exBlockCnt
            Assert.True(( readData.uLength = exBytesLen ))
            
            let exStartBytesPos = Blocksize.toUInt32 m_MediaBlockSize * exStartBlkPos
            for i = 0 to int exBytesLen - 1 do
                Assert.True(( m_FilledData.[ int exStartBytesPos + i ] = readData.Array.[ i ] ))
            
            readData.Return()
            do! r1.Close()
        }
        
    static member Read6_002_data : obj[][] = [|
        [| m_MediaBlockCount - 1u; 2uy; |]
        [| m_MediaBlockCount;      1uy; |]
        [| m_MediaBlockCount + 1u; 0uy; |]
    |]

    [<Theory>]
    [<MemberData( "Read6_002_data" )>]
    member _.Read6_002 ( lba : uint32 ) ( transLen : byte ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt = r1.Send_Read6 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 lba ) m_MediaBlockSize ( blkcnt_me.ofUInt8 transLen ) NACA.T
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( res.Sense.Value.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))

            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    [<Fact>]
    member _.Read6_LINK_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let cdb = GenScsiCDB.Read6 blkcnt_me.zero32 blkcnt_me.zero8 NACA.T LINK.T
            let edtl = Blocksize.toUInt32 m_MediaBlockSize * 256u
            let! itt = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb PooledBuffer.Empty edtl
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    static member Read10_001_data : obj[][] = [|
        [| 0u;                     0us;   0u;   0u;                     |]
        [| 0u;                     1us;   1u;   0u;                     |]
        [| 0u;                     255us; 255u; 0u;                     |]
        [| m_MediaBlockCount - 1u; 1us;   1u;   m_MediaBlockCount - 1u; |]
        [| m_MediaBlockCount;      0us;   0u;   0u;                     |]
    |]

    [<Theory>]
    [<MemberData( "Read10_001_data" )>]
    member _.Read10_001 ( lba : uint32 ) ( transLen : uint16 ) ( exBlockCnt : uint32 ) ( exStartBlkPos : uint32 ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_read = r1.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 lba ) m_MediaBlockSize ( blkcnt_me.ofUInt16 transLen ) NACA.T
            let! readData = r1.WaitSCSIResponseGoodStatus itt_read

            let exBytesLen = Blocksize.toUInt32 m_MediaBlockSize * exBlockCnt
            Assert.True(( readData.uLength = exBytesLen ))
            
            let exStartBytesPos = Blocksize.toUInt32 m_MediaBlockSize * exStartBlkPos
            for i = 0 to int exBytesLen - 1 do
                Assert.True(( m_FilledData.[ int exStartBytesPos + i ] = readData.Array.[ i ] ))
            
            readData.Return()
            do! r1.Close()
        }
        
    static member Read10_002_data : obj[][] = [|
        [| m_MediaBlockCount - 1u; 2us; |]
        [| m_MediaBlockCount;      1us; |]
        [| m_MediaBlockCount + 1u; 0us; |]
    |]

    [<Theory>]
    [<MemberData( "Read10_002_data" )>]
    member _.Read10_002 ( lba : uint32 ) ( transLen : uint16 ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt = r1.Send_Read10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( blkcnt_me.ofUInt32 lba ) m_MediaBlockSize ( blkcnt_me.ofUInt16 transLen ) NACA.T
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( res.Sense.Value.ASC = ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE ))

            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    [<Fact>]
    member _.Read10_LINK_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let cdb = GenScsiCDB.Read10 0uy DPO.F FUA.F FUA_NV.F blkcnt_me.zero32 0uy blkcnt_me.zero16 NACA.T LINK.T
            let! itt = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb PooledBuffer.Empty 0u
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))

            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

