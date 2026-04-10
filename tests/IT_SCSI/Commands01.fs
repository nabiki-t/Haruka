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

    let m_iSCSIPortNo = GlbFunc.nextTcpPortNo()
    let m_MediaSize = 65536u

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

        // Target, LU
        client.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        client.RunCommand "select 0" "" "T > "
        client.RunCommand "set ID 1" "" "T > "
        client.RunCommand "create /l 1" "Created" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "LU> "

        client.RunCommand "validate" "All configurations are vlidated" "LU> "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "LU> "
        client.RunCommand "start" "Started" "LU> "

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
    member _.MediaSize = m_MediaSize
    member _.MediaBlockSize = 
        if Constants.MEDIA_BLOCK_SIZE = 512UL then     // 4096 or 512 bytes
            Blocksize.BS_512
        else
            Blocksize.BS_4096
    member _.WorkPath = m_WorkPath


[<Collection( "SCSI_Commands01" )>]
type SCSI_Commands01( fx : SCSI_Commands01_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_CID1 = cid_me.fromPrim 1us
    let g_CID2 = cid_me.fromPrim 2us

    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL

    let m_ClientProc = fx.clientProc
    let iSCSIPortNo = fx.iSCSIPortNo
    let m_MediaSize = fx.MediaSize
    let m_MediaBlockSize = fx.MediaBlockSize
    let m_WorkPath = fx.WorkPath
    let m_InitName = "iqn.2020-05.example.com:initiator"

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

    [<Fact>]
    member _.Inquiry_Standard_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.F 0uy 0us NACA.T
            let! res = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( res.Length = 0 ))
            do! r1.Close()
        }

    [<Fact>]
    member _.Inquiry_Standard_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.F 1uy 0us NACA.T
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( res.Sense.Value.ASC = ASCCd.INVALID_FIELD_IN_CDB ))
            do! ClearACA r1 g_LUN1
            do! r1.Close()
        }

    [<Fact>]
    member _.Inquiry_Standard_003 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.F 0uy 5us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 5 ))
            let res = GenScsiParams.Inquiry_Standerd r
            r.Return()
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
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

    [<Fact>]
    member _.Inquiry_Standard_004 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.F 0uy 96us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 96 ))
            let res = GenScsiParams.Inquiry_Standerd r
            r.Return()
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
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
    [<InlineData( 0x80uy )>]
    [<InlineData( 0x83uy )>]
    [<InlineData( 0x86uy )>]
    [<InlineData( 0xB0uy )>]
    [<InlineData( 0xB1uy )>]
    [<InlineData( 0x00uy )>]
    member _.Inquiry_AllocLen0_001 ( pageCode : byte ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0x80uy 0us NACA.T
            let! res = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( res.Length = 0 ))
            do! r1.Close()
        }

    [<Fact>]
    member _.Inquiry_UnitSerialNumber_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0x80uy 4us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 4 ))
            let res = GenScsiParams.Inquiry_UnitSerialNumberVPD r
            r.Return()
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
            Assert.True(( res.PageLength > 0uy ))
            Assert.True(( res.ProductSerialNumber = "" ))
            do! r1.Close()
        }

    [<Fact>]
    member _.Inquiry_UnitSerialNumber_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0x80uy 8us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 8 ))
            let res = GenScsiParams.Inquiry_UnitSerialNumberVPD r
            r.Return()
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
            Assert.True(( res.PageCode = 0x80uy ))
            Assert.True(( res.PageLength > 0uy ))
            Assert.True(( res.ProductSerialNumber <> "" ))
            do! r1.Close()
        }

    [<Fact>]
    member _.Inquiry_DeviceIdentification_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0x83uy 4us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 4 ))
            let res = GenScsiParams.Inquiry_DeviceIdentifierVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
            Assert.True(( res.PageCode = 0x83uy ))
            Assert.True(( res.PageLength > 0uy ))
            Assert.True(( res.IdentifierDescriptor.Length = 0 ))
            r.Return()
            do! r1.Close()
        }

    [<Fact>]
    member _.Inquiry_DeviceIdentification_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0x83uy 256us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            let res = GenScsiParams.Inquiry_DeviceIdentifierVPD r
            r.Return()
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
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

    [<Fact>]
    member _.Inquiry_Extended_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0x86uy 4us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 4 ))
            let res = GenScsiParams.Inquiry_ExtendedInquiryDataVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
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

    [<Fact>]
    member _.Inquiry_Extended_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0x86uy 64us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 64 ))
            let res = GenScsiParams.Inquiry_ExtendedInquiryDataVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
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

    [<Fact>]
    member _.Inquiry_BlockLimits_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0xB0uy 4us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 4 ))
            let res = GenScsiParams.Inquiry_BlockLimitVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
            Assert.True(( res.PageCode = 0xB0uy ))
            Assert.True(( res.PageLength = 0x0Cuy ))
            Assert.True(( res.OptimalTransferLengthGramularity = blkcnt_me.zero16 ))
            Assert.True(( res.MaximumTransferLength = blkcnt_me.zero32 ))
            Assert.True(( res.OptimalTransferLength = blkcnt_me.zero32 ))
            do! r1.Close()
        }

    [<Fact>]
    member _.Inquiry_BlockLimits_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0xB0uy 16us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 16 ))
            let res = GenScsiParams.Inquiry_BlockLimitVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
            Assert.True(( res.PageCode = 0xB0uy ))
            Assert.True(( res.PageLength = 0x0Cuy ))
            Assert.True(( blkcnt_me.toUInt16 res.OptimalTransferLengthGramularity > 0us ))
            Assert.True(( res.MaximumTransferLength = blkcnt_me.zero32 ))
            Assert.True(( blkcnt_me.toUInt32 res.OptimalTransferLength > 0u ))
            do! r1.Close()
        }

    [<Fact>]
    member _.Inquiry_BlockDeviceCharacteristics_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0xB1uy 4us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 4 ))
            let res = GenScsiParams.Inquiry_BlockDeviceCharacteristicsVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
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

    [<Fact>]
    member _.Inquiry_BlockDeviceCharacteristics_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0xB1uy 64us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 64 ))
            let res = GenScsiParams.Inquiry_BlockDeviceCharacteristicsVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
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

    [<Fact>]
    member _.Inquiry_SupportedVPDPages_001 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0x00uy 4us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 4 ))
            let res = GenScsiParams.Inquiry_SupportedVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
            Assert.True(( res.PageCode = 0x00uy ))
            Assert.True(( res.PageLength = 0x06uy ))
            Assert.True(( res.SupportedVPGPages.Length = 0 ))
            do! r1.Close()
        }

    [<Fact>]
    member _.Inquiry_SupportedVPDPages_002 () =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.T 0x00uy 10us NACA.T
            let! r = r1.WaitSCSIResponseGoodStatus itt
            Assert.True(( r.Length = 10 ))
            let res = GenScsiParams.Inquiry_SupportedVPD r
            Assert.True(( res.PeripheraQualifier = 0uy ))
            Assert.True(( res.PeripheralDeviceType = 0uy ))
            Assert.True(( res.PageCode = 0x00uy ))
            Assert.True(( res.PageLength = 0x06uy ))
            Assert.True(( res.SupportedVPGPages = [| 0x00uy; 0x80uy; 0x83uy; 0x86uy; 0xB0uy; 0xB1uy |] ))
            do! r1.Close()
        }
