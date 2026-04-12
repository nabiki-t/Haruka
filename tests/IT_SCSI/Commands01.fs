//=============================================================================
// Haruka Software Storage.
// Commands01.fs : Test cases for various commands.
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

[<CollectionDefinition( "SCSI_Commands01" )>]
type SCSI_Commands01_Fixture() =

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

        // Target2, LU2 ... LU255
        client.RunCommand "create /n iqn.2020-05.example.com:target2" "Created" "TG> "
        client.RunCommand "select 1" "" "T > "
        client.RunCommand "set ID 2" "" "T > "
        for i = 2 to Constants.MAX_LOGICALUNIT_COUNT_IN_TD do
            client.RunCommand ( sprintf "create /l %d" i ) "Created" "T > "
            client.RunCommand ( sprintf "select %d" ( i - 2 ) ) "" "LU> "
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

    interface ICollectionFixture<SCSI_Commands01_Fixture>

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

[<Collection( "SCSI_Commands01" )>]
type SCSI_Commands01( fx : SCSI_Commands01_Fixture ) =

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
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.Inquiry_Standard_001 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.F 0uy 0us NACA.T
            let! res = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( res.Length = 0 ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.Inquiry_Standard_002 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.F 1uy 0us NACA.T
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( res.Sense.Value.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            do! ClearACA r1 lun
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_Standard_003 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.F 0uy 5us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 5 ))
            let res = GenScsiParams.Inquiry_Standerd r
            r.Return()
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.False(( res.RemovableMedium ))
            Assert.True(( res.Version = 5uy ))
            Assert.True(( res.NormalACASupported ))
            Assert.False(( res.HierarchicalSupported ))
            Assert.True(( res.RsponseDataFormat = 2uy ))
            Assert.True(( res.AdditionalLength > 0uy ))
            Assert.False(( res.SCCSupported ))
            Assert.False(( res.AccessControlsCoordinator ))
            Assert.True(( res.TargetPortGroupSupport = 0uy ))
            Assert.False(( res.ThirdPartyCopy ))
            Assert.False(( res.Protect ))
            Assert.False(( res.BQueue ))
            Assert.False(( res.CmdQueue ))
            Assert.False(( res.EnclosureServices ))
            Assert.False(( res.MultiPort ))
            Assert.False(( res.MediumChanger ))
            Assert.False(( res.LinkedCommand ))
            Assert.True(( res.T10VendorIdentification = "" ))
            Assert.True(( res.ProduceIdentification = "" ))
            Assert.True(( res.ProductRevisionLevel = "" ))
            Assert.True(( res.VersionDescriptor.Length = 0 ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_Standard_004 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.F 0uy 96us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 96 ))
            let res = GenScsiParams.Inquiry_Standerd r
            r.Return()
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.False(( res.RemovableMedium ))
            Assert.True(( res.Version = 5uy ))
            Assert.True(( res.NormalACASupported ))
            Assert.False(( res.HierarchicalSupported ))
            Assert.True(( res.RsponseDataFormat = 2uy ))
            Assert.True(( res.AdditionalLength > 0uy ))
            Assert.False(( res.SCCSupported ))
            Assert.False(( res.AccessControlsCoordinator ))
            Assert.True(( res.TargetPortGroupSupport = 0uy ))
            Assert.False(( res.ThirdPartyCopy ))
            Assert.False(( res.Protect ))
            Assert.False(( res.BQueue ))
            Assert.True(( res.CmdQueue ))
            Assert.False(( res.EnclosureServices ))
            Assert.True(( res.MultiPort ))
            Assert.False(( res.MediumChanger ))
            Assert.False(( res.LinkedCommand ))
            Assert.True(( res.T10VendorIdentification = "" ))
            Assert.True(( res.ProduceIdentification <> "" ))
            Assert.True(( res.ProductRevisionLevel <> "" ))
            Assert.True(( res.VersionDescriptor.Length = 8 ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x80uy )>]
    [<InlineData( 0UL, 0x83uy )>]
    [<InlineData( 0UL, 0x86uy )>]
    [<InlineData( 0UL, 0xB0uy )>]
    [<InlineData( 0UL, 0xB1uy )>]
    [<InlineData( 0UL, 0x00uy )>]
    [<InlineData( 1UL, 0x80uy )>]
    [<InlineData( 1UL, 0x83uy )>]
    [<InlineData( 1UL, 0x86uy )>]
    [<InlineData( 1UL, 0xB0uy )>]
    [<InlineData( 1UL, 0xB1uy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_AllocLen0_001 ( lu : uint64 ) ( pageCode : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T pageCode 0us NACA.T
            let! res = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( res.Length = 0 ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_UnitSerialNumber_001 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0x80uy 4us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 4 ))
            let res = GenScsiParams.Inquiry_UnitSerialNumberVPD r
            r.Return()
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageLength > 0uy ))
            Assert.True(( res.ProductSerialNumber = "" ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_UnitSerialNumber_002 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0x80uy 8us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 8 ))
            let res = GenScsiParams.Inquiry_UnitSerialNumberVPD r
            r.Return()
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageCode = 0x80uy ))
            Assert.True(( res.PageLength > 0uy ))
            Assert.True(( res.ProductSerialNumber <> "" ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_DeviceIdentification_001 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0x83uy 4us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 4 ))
            let res = GenScsiParams.Inquiry_DeviceIdentifierVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageCode = 0x83uy ))
            Assert.True(( res.PageLength > 0uy ))
            Assert.True(( res.IdentifierDescriptor.Length = 0 ))
            r.Return()
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0x00uy )>]
    member _.Inquiry_DeviceIdentification_002 ( expPDT : byte ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0x83uy 256us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            let res = GenScsiParams.Inquiry_DeviceIdentifierVPD r
            r.Return()
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageCode = 0x83uy ))
            Assert.True(( res.PageLength > 0uy ))
            Assert.True(( res.IdentifierDescriptor.Length = 4 ))
            let v = res.IdentifierDescriptor

            // logical unit
            Assert.True(( v.[0].ProtocolIdentifier = 0uy ))
            Assert.True(( v.[0].CodeSet = 3uy ))
            Assert.False(( v.[0].ProtocolIdentifierValid ))
            Assert.True(( v.[0].Association = 0uy ))
            Assert.True(( v.[0].IdentifierType = 8uy ))
            let expIdentifier =
                let lunstr = lun_me.toBytes_NewVec g_LUN1 |> Convert.ToHexString
                r1.ITNexus.TargetName + ",L,0x" + lunstr
            Assert.True(( v.[0].IdentifierStr = expIdentifier ))

            // target port
            Assert.True(( v.[1].ProtocolIdentifier = 5uy ))
            Assert.True(( v.[1].CodeSet = 3uy ))
            Assert.True(( v.[1].ProtocolIdentifierValid ))
            Assert.True(( v.[1].Association = 1uy ))
            Assert.True(( v.[1].IdentifierType = 8uy ))
            Assert.True(( v.[1].IdentifierStr = r1.ITNexus.TargetPortName ))

            // Relative target port identifier
            Assert.True(( v.[2].ProtocolIdentifier = 5uy ))
            Assert.True(( v.[2].CodeSet = 1uy ))
            Assert.True(( v.[2].ProtocolIdentifierValid ))
            Assert.True(( v.[2].Association = 1uy ))
            Assert.True(( v.[2].IdentifierType = 4uy ))
            Assert.True(( v.[2].IdentifierBin = [| 0uy; 0uy; 0uy; 1uy |] ))

            // SCSI target device
            Assert.True(( v.[3].ProtocolIdentifier = 5uy ))
            Assert.True(( v.[3].CodeSet = 3uy ))
            Assert.True(( v.[3].ProtocolIdentifierValid ))
            Assert.True(( v.[3].Association = 2uy ))
            Assert.True(( v.[3].IdentifierType = 8uy ))
            Assert.True(( v.[3].IdentifierStr = "targetDevice001" ))

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0x0Cuy )>]
    member _.Inquiry_DeviceIdentification_003 ( expPDT : byte ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN0 EVPD.T 0x83uy 256us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            let res = GenScsiParams.Inquiry_DeviceIdentifierVPD r
            r.Return()
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageCode = 0x83uy ))
            Assert.True(( res.PageLength > 0uy ))
            Assert.True(( res.IdentifierDescriptor.Length = 2 ))
            let v = res.IdentifierDescriptor

            // target port
            Assert.True(( v.[0].ProtocolIdentifier = 5uy ))
            Assert.True(( v.[0].CodeSet = 3uy ))
            Assert.True(( v.[0].ProtocolIdentifierValid ))
            Assert.True(( v.[0].Association = 1uy ))
            Assert.True(( v.[0].IdentifierType = 8uy ))
            Assert.True(( v.[0].IdentifierStr = r1.ITNexus.TargetPortName ))

            // SCSI target device
            Assert.True(( v.[1].ProtocolIdentifier = 5uy ))
            Assert.True(( v.[1].CodeSet = 3uy ))
            Assert.True(( v.[1].ProtocolIdentifierValid ))
            Assert.True(( v.[1].Association = 2uy ))
            Assert.True(( v.[1].IdentifierType = 8uy ))
            Assert.True(( v.[1].IdentifierStr = "targetDevice001" ))

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_Extended_001 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0x86uy 4us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 4 ))
            let res = GenScsiParams.Inquiry_ExtendedInquiryDataVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageCode = 0x86uy ))
            Assert.True(( res.PageLength = 0x3Cuy ))
            Assert.False(( res.ReferenceTagOwnership ))
            Assert.False(( res.GuardCheck ))
            Assert.False(( res.ApplicationTagCheck ))
            Assert.False(( res.ReferenceTagCheck ))
            Assert.False(( res.GroupingFunctionSupported ))
            Assert.False(( res.PrioritySupported ))
            Assert.False(( res.HeadOfQueueSupported ))
            Assert.False(( res.OrderedSupported ))
            Assert.False(( res.SimpleSupported ))
            Assert.False(( res.NonVolatileSupported ))
            Assert.False(( res.VolatileSupported ))
            r.Return()
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_Extended_002 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0x86uy 64us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 64 ))
            let res = GenScsiParams.Inquiry_ExtendedInquiryDataVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageCode = 0x86uy ))
            Assert.True(( res.PageLength = 0x3Cuy ))
            Assert.False(( res.ReferenceTagOwnership ))
            Assert.False(( res.GuardCheck ))
            Assert.False(( res.ApplicationTagCheck ))
            Assert.False(( res.ReferenceTagCheck ))
            Assert.False(( res.GroupingFunctionSupported ))
            Assert.False(( res.PrioritySupported ))
            Assert.True(( res.HeadOfQueueSupported ))
            Assert.True(( res.OrderedSupported ))
            Assert.True(( res.SimpleSupported ))
            Assert.False(( res.NonVolatileSupported ))
            Assert.False(( res.VolatileSupported ))
            r.Return()
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_BlockLimits_001 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0xB0uy 4us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 4 ))
            let res = GenScsiParams.Inquiry_BlockLimitVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageCode = 0xB0uy ))
            Assert.True(( res.PageLength = 0x0Cuy ))
            Assert.True(( res.OptimalTransferLengthGramularity = blkcnt_me.zero16 ))
            Assert.True(( res.MaximumTransferLength = blkcnt_me.zero32 ))
            Assert.True(( res.OptimalTransferLength = blkcnt_me.zero32 ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_BlockLimits_002 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0xB0uy 16us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 16 ))
            let res = GenScsiParams.Inquiry_BlockLimitVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageCode = 0xB0uy ))
            Assert.True(( res.PageLength = 0x0Cuy ))
            Assert.True(( blkcnt_me.toUInt16 res.OptimalTransferLengthGramularity > 0us ))
            Assert.True(( res.MaximumTransferLength = blkcnt_me.zero32 ))
            Assert.True(( blkcnt_me.toUInt32 res.OptimalTransferLength > 0u ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_BlockDeviceCharacteristics_001 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0xB1uy 4us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 4 ))
            let res = GenScsiParams.Inquiry_BlockDeviceCharacteristicsVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageCode = 0xB1uy ))
            Assert.True(( res.PageLength = 0x3Cus ))
            Assert.True(( res.MediumRotationRate = 0us ))
            Assert.True(( res.ProductType = 0uy ))
            Assert.True(( res.WriteAfterBlockEraseRequired = 0uy ))
            Assert.True(( res.WriteAfterCryptographicEraseRequired = 0uy ))
            Assert.True(( res.NominalFormFactor = 0uy ))
            Assert.False(( res.ForceUnitAccessBehavior ))
            Assert.False(( res.VerifyByteCheckUnmappedLBASupported ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_BlockDeviceCharacteristics_002 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0xB1uy 64us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 64 ))
            let res = GenScsiParams.Inquiry_BlockDeviceCharacteristicsVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageCode = 0xB1uy ))
            Assert.True(( res.PageLength = 0x3Cus ))
            Assert.True(( res.MediumRotationRate = 0us ))
            Assert.True(( res.ProductType = 0uy ))
            Assert.True(( res.WriteAfterBlockEraseRequired = 0uy ))
            Assert.True(( res.WriteAfterCryptographicEraseRequired = 0uy ))
            Assert.True(( res.NominalFormFactor = 0uy ))
            Assert.False(( res.ForceUnitAccessBehavior ))
            Assert.False(( res.VerifyByteCheckUnmappedLBASupported ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_SupportedVPDPages_001 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0x00uy 4us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 4 ))
            let res = GenScsiParams.Inquiry_SupportedVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageCode = 0x00uy ))
            Assert.True(( res.PageLength = 0x06uy ))
            Assert.True(( res.SupportedVPGPages.Length = 0 ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x0Cuy )>]
    [<InlineData( 1UL, 0x00uy )>]
    member _.Inquiry_SupportedVPDPages_002 ( lu : uint64 ) ( expPDT : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0x00uy 10us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 10 ))
            let res = GenScsiParams.Inquiry_SupportedVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = expPDT ))
            Assert.True(( res.PageCode = 0x00uy ))
            Assert.True(( res.PageLength = 0x06uy ))
            Assert.True(( res.SupportedVPGPages = [| 0x00uy; 0x80uy; 0x83uy; 0x86uy; 0xB0uy; 0xB1uy |] ))
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, 0x01uy )>]
    [<InlineData( 0UL, 0x7Fuy )>]
    [<InlineData( 0UL, 0x85uy )>]
    [<InlineData( 0UL, 0x87uy )>]
    [<InlineData( 0UL, 0x81uy )>]
    [<InlineData( 0UL, 0x82uy )>]
    [<InlineData( 0UL, 0x88uy )>]
    [<InlineData( 0UL, 0x84uy )>]
    [<InlineData( 0UL, 0xB2uy )>]
    [<InlineData( 0UL, 0xFFuy )>]
    [<InlineData( 1UL, 0x01uy )>]
    [<InlineData( 1UL, 0x7Fuy )>]
    [<InlineData( 1UL, 0x85uy )>]
    [<InlineData( 1UL, 0x87uy )>]
    [<InlineData( 1UL, 0x81uy )>]
    [<InlineData( 1UL, 0x82uy )>]
    [<InlineData( 1UL, 0x88uy )>]
    [<InlineData( 1UL, 0x84uy )>]
    [<InlineData( 1UL, 0xB2uy )>]
    [<InlineData( 1UL, 0xFFuy )>]
    member _.Inquiry_UnsupportedPages_001 ( lu : uint64 ) ( page : byte ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T page 256us NACA.T
            let! r = r1.WaitSCSIResponse itt
            Assert.True(( r.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( r.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( r.Sense.Value.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            do! ClearACA r1 lun
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.Inquiry_UA_001 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK lun NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK lun NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2

            let! itt_pr_out3 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK lun NACA.T g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! itt_iq1 = r2.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.F 0x00uy 256us NACA.T
            let! res_iq1 = r2.WaitSCSIResponseGoodStatus itt_iq1
            Assert.True(( res_iq1.Length = 96 ))

            let! itt_tur1 = r2.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK lun NACA.T
            let! res_tur1 = r2.WaitSCSIResponse itt_tur1
            Assert.True(( res_tur1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res_tur1.Sense.Value.SenseKey = SenseKeyCd.UNIT_ATTENTION ))
            Assert.True(( res_tur1.Sense.Value.ASC = ASCCd.RESERVATIONS_PREEMPTED ))

            do! r1.Close()
            do! r2.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.Inquiry_LINK_001 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let cdb = GenScsiCDB.Inquiry EVPD.F 0x00uy 256us NACA.T LINK.T
            let! itt = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK lun cdb PooledBuffer.Empty 256u
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 lun

            do! r1.Close()
        }

    static member PreFetch10_001_data : obj[][] = [|
        [| true;  0u;                     0us;                            true;  |]
        [| true;  0u;                     1us;                            true;  |]
        [| true;  0u;                     uint16 m_MediaBlockCount;       true;  |]
        [| true;  m_MediaBlockCount;      0u;                             true;  |]
        [| true;  m_MediaBlockCount - 1u; 1u;                             true;  |]
        [| true;  0u;                     uint16 m_MediaBlockCount + 1us; false; |]
        [| true;  m_MediaBlockCount;      1us;                            false; |]
        [| false; 0u;                     0us;                            true;  |]
    |]

    [<Theory>]
    [<MemberData( "PreFetch10_001_data" )>]
    member _.PreFetch10_001 ( immed : bool ) ( lba : uint32 ) ( cnt : uint16 ) ( exp : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_pf1 = r1.Send_PreFetch10 TaskATTRCd.SIMPLE_TASK g_LUN1 ( IMMED.ofBool immed ) ( blkcnt_me.ofUInt32 lba ) ( blkcnt_me.ofUInt16 cnt ) NACA.T
            let! res_pf1 = r1.WaitSCSIResponse itt_pf1
            if exp then
                Assert.True(( res_pf1.Status = ScsiCmdStatCd.CONDITION_MET ))
            else
                Assert.True(( res_pf1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                do! ClearACA r1 g_LUN1

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL  )>]
    member _.PreFetch10_LINK_001 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let cdb = GenScsiCDB.PreFetch10 IMMED.T blkcnt_me.zero32 0uy blkcnt_me.zero16 NACA.T LINK.T
            let! itt_pf1 = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK lun cdb PooledBuffer.Empty 0u
            let! res_pf1 = r1.WaitSCSIResponse itt_pf1
            Assert.True(( res_pf1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 lun

            do! r1.Close()
        }

    static member PreFetch16_001_data : obj[][] = [|
        [| true;  0UL;                            0u;                     0uy; true;  |]
        [| true;  0UL;                            1u;                     0uy; true;  |]
        [| true;  0UL;                            m_MediaBlockCount;      0uy; true;  |]
        [| true;  uint64 m_MediaBlockCount;       0u;                     0uy; true;  |]
        [| true;  uint64 m_MediaBlockCount - 1UL; 1u;                     0uy; true;  |]
        [| true;  0UL;                            m_MediaBlockCount + 1u; 0uy; false; |]
        [| true;  uint64 m_MediaBlockCount;       1us;                    0uy; false; |]
        [| false; 0UL;                            0us;                    0uy; true;  |]
        [| true;  0UL;                            0us;                    1uy; true;  |]
    |]

    [<Theory>]
    [<MemberData( "PreFetch16_001_data" )>]
    member _.PreFetch16_001 ( immed : bool ) ( lba : uint64 ) ( cnt : uint32 ) ( gpn : byte ) ( exp : bool ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let cdb = GenScsiCDB.PreFetch16 ( IMMED.ofBool immed ) ( blkcnt_me.ofUInt64 lba ) gpn ( blkcnt_me.ofUInt32 cnt ) NACA.T LINK.F
            let! itt_pf1 = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb PooledBuffer.Empty 0u
            let! res_pf1 = r1.WaitSCSIResponse itt_pf1
            if exp then
                Assert.True(( res_pf1.Status = ScsiCmdStatCd.CONDITION_MET ))
            else
                Assert.True(( res_pf1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                do! ClearACA r1 g_LUN1

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.PreFetch16_LINK_001 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let cdb = GenScsiCDB.PreFetch16 IMMED.T blkcnt_me.zero64 0uy blkcnt_me.zero32 NACA.T LINK.T
            let! itt_pf1 = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK lun cdb PooledBuffer.Empty 0u
            let! res_pf1 = r1.WaitSCSIResponse itt_pf1
            Assert.True(( res_pf1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 lun

            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL, false )>]
    [<InlineData( 1UL, true  )>]
    member _.TestUnitReady_001 ( lu : uint64 ) ( exp : bool ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt_tur1 = r1.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK lun NACA.T
            let! res_tur1 = r1.WaitSCSIResponse itt_tur1
            if exp then
                Assert.True(( res_tur1.Status = ScsiCmdStatCd.GOOD ))
            else
                Assert.True(( res_tur1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
                do! ClearACA r1 lun
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.TestUnitReady_LINK_001 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let cdb = GenScsiCDB.TestUnitReady NACA.T LINK.T
            let! itt_tur1 =  r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK lun cdb PooledBuffer.Empty 0u
            let! res_tur1 = r1.WaitSCSIResponse itt_tur1
            Assert.True(( res_tur1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 lun
            do! r1.Close()
        }

    [<Fact>]
    member _.FormatUnit_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_FormatUnit TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! _ = r1.WaitSCSIResponseGoodStatus itt
            do! r1.Close()
        }

    [<Fact>]
    member _.FormatUnit_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let cdb = GenScsiCDB.FormatUnit FMTPINFO.T RTO_REQ.T LONGLIST.T FMTDATA.T CMPLIST.T 1uy NACA.T LINK.F
            let! itt = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb PooledBuffer.Empty 0u
            let! _ = r1.WaitSCSIResponseGoodStatus itt
            do! r1.Close()
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 1UL )>]
    member _.FormatUnit_LINK_001 ( lu : uint64 ) =
        task {
            let lun = lun_me.fromPrim lu
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let cdb = GenScsiCDB.FormatUnit FMTPINFO.F RTO_REQ.F LONGLIST.F FMTDATA.F CMPLIST.F 0uy NACA.T LINK.T
            let! itt = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK lun cdb PooledBuffer.Empty 0u
            let! res_tur1 = r1.WaitSCSIResponse itt
            Assert.True(( res_tur1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 lun
            do! r1.Close()
        }

    [<Fact>]
    member _.ReadCapacity10_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ReadCapacity10 TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! blockCount, blockSize = r1.Wait_ReadCapacity10 itt
            Assert.True(( blockCount = m_MediaBlockCount - 1u ))
            Assert.True(( blockSize = Blocksize.toUInt32 m_MediaBlockSize ))
            do! r1.Close()
        }

    [<Fact>]
    member _.ReadCapacity10_LINK_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let cdb = GenScsiCDB.ReadCapacity10 blkcnt_me.zero32 PMI.F NACA.T LINK.T
            let! itt = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb PooledBuffer.Empty 8u
            let! res_tur1 = r1.WaitSCSIResponse itt
            Assert.True(( res_tur1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    static member ReadCapacity16_001_data : obj[][] = [|
        [| 0u; 0; 0UL; 0u |]
        [| 4u; 4; 0UL; 0u |]
        [| 8u; 8; uint64 m_MediaBlockCount - 1UL; 0u |]
        [| 12u; 12; uint64 m_MediaBlockCount - 1UL; Blocksize.toUInt32 m_MediaBlockSize |]
        [| 32u; 32; uint64 m_MediaBlockCount - 1UL; Blocksize.toUInt32 m_MediaBlockSize |]
        [| 64u; 32; uint64 m_MediaBlockCount - 1UL; Blocksize.toUInt32 m_MediaBlockSize |]
    |]

    [<Theory>]
    [<MemberData( "ReadCapacity16_001_data" )>]
    member _.ReadCapacity16_001 ( allen : uint32 ) ( retlen : int ) ( blkcnt : uint64 ) ( blksize : uint32 ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ReadCapacity16 TaskATTRCd.SIMPLE_TASK g_LUN1 allen NACA.T
            let! res = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( res.Length = retlen ))
            let para = GenScsiParams.ReadCapacity16 res
            Assert.True(( para.ReturnedLogicalBlockAddress = blkcnt ))
            Assert.True(( para.BlockLengthInBytes = blksize ))
            Assert.False(( para.ProtectionEnable ))
            Assert.False(( para.ReferenceTagOwnEnable ))
            do! r1.Close()
        }

    [<Fact>]
    member _.ReadCapacity16_LINK_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let cdb = GenScsiCDB.ReadCapacity16 blkcnt_me.zero64 256u PMI.F NACA.T LINK.T
            let! itt = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb PooledBuffer.Empty 256u
            let! res_tur1 = r1.WaitSCSIResponse itt
            Assert.True(( res_tur1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    static member ReportLUNs_001_data : obj[][] = [|
        [| 0uy; 0u;   0u;  [||] |]
        [| 0uy; 8u;   16u; [||] |]
        [| 0uy; 16u;  16u; [| lun_me.fromPrim 0UL; |] |]
        [| 0uy; 24u;  16u; [| lun_me.fromPrim 0UL; lun_me.fromPrim 1UL |] |]
        [| 0uy; 256u; 16u; [| lun_me.fromPrim 0UL; lun_me.fromPrim 1UL |] |]
        [| 1uy; 256u; 0u;  [||] |]
        [| 2uy; 256u; 16u; [| lun_me.fromPrim 0UL; lun_me.fromPrim 1UL |] |]
    |]

    [<Theory>]
    [<MemberData( "ReportLUNs_001_data" )>]
    member _.ReportLUNs_001 ( report : byte ) ( allen : uint32 ) ( exlen : uint32 ) ( exluns : LUN_T[] ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ReportLUNs TaskATTRCd.SIMPLE_TASK g_LUN1 report allen NACA.T
            let! listLength, luns = r1.Wait_ReportLUNs itt
            Assert.True(( listLength = exlen ))
            Assert.True(( luns = exluns ))
            do! r1.Close()
        }

    [<Fact>]
    member _.ReportLUNs_UnknownReportType_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ReportLUNs TaskATTRCd.SIMPLE_TASK g_LUN1 3uy 256u NACA.T
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    [<Fact>]
    member _.ReportLUNs_UA_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! r2 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam

            let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey1 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1

            let! itt_pr_out2 = r2.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T resvkey_me.zero g_ResvKey2 SPEC_I_PT.F ALL_TG_PT.F APTPL.T [||]
            let! _ = r2.WaitSCSIResponseGoodStatus itt_pr_out2

            let! itt_pr_out3 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T g_ResvKey1
            let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out3

            let! itt_rl1 = r2.Send_ReportLUNs TaskATTRCd.SIMPLE_TASK g_LUN1 0uy 256u NACA.T
            let! _, luns = r2.Wait_ReportLUNs itt_rl1
            Assert.True(( luns.Length = 2 ))

            let! itt_tur1 = r2.Send_TestUnitReady TaskATTRCd.SIMPLE_TASK g_LUN1 NACA.T
            let! res_tur1 = r2.WaitSCSIResponse itt_tur1
            Assert.True(( res_tur1.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res_tur1.Sense.Value.SenseKey = SenseKeyCd.UNIT_ATTENTION ))
            Assert.True(( res_tur1.Sense.Value.ASC = ASCCd.RESERVATIONS_PREEMPTED ))

            do! r1.Close()
            do! r2.Close()
        }

    [<Fact>]
    member _.ReportLUNs_LUN0_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_ReportLUNs TaskATTRCd.SIMPLE_TASK g_LUN0 2uy 256u NACA.T
            let! listLength, luns = r1.Wait_ReportLUNs itt
            Assert.True(( listLength = 16u ))
            Assert.True(( luns = [| lun_me.fromPrim 0UL; lun_me.fromPrim 1UL |] ))
            do! r1.Close()
        }

    [<Fact>]
    member _.ReportLUNs_LUN0_002 () =
        task {
            let param = { m_defaultSessParam with TargetName = "iqn.2020-05.example.com:target2" }
            let! r1 = SCSI_Initiator.Create param m_defaultConnParam
            let! itt = r1.Send_ReportLUNs TaskATTRCd.SIMPLE_TASK g_LUN0 2uy 2048u NACA.T
            let! listLength, luns = r1.Wait_ReportLUNs itt
            Assert.True(( listLength = 2040u ))
            Assert.True(( luns.Length = 255 ))
            do! r1.Close()
        }

    [<Fact>]
    member _.ReportLUNs_LINK_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let cdb = GenScsiCDB.ReportLUNs 0uy 256u NACA.T LINK.T
            let! itt = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK g_LUN1 cdb PooledBuffer.Empty 256u
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }
