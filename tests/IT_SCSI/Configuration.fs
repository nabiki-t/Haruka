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


[<CollectionDefinition( "SCSI_Configuration" )>]
type SCSI_Configuration_Fixture() =

    let m_iSCSIPortNo1 = GlbFunc.nextTcpPortNo()
    let m_iSCSIPortNo2 = GlbFunc.nextTcpPortNo()
    let m_iSCSIPortNo3 = GlbFunc.nextTcpPortNo()
    let m_iSCSIPortNo4 = GlbFunc.nextTcpPortNo()
    let m_MediaSize = 65536u

    // Add default configurations
    let AddDefaultConf( client : ClientProc ): unit =

        // Target device1, Target group1
        client.RunCommand "create" "Created" "CR> "
        client.RunCommand "select 0" "" "TD> "
        client.RunCommand "set loglevel VERBOSE" "" "TD> "
        client.RunCommand "create targetgroup" "Created" "TD> "
        client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo1 ) "Created" "TD> "
        client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo2 ) "Created" "TD> "
        client.RunCommand "select 0" "" "TG> "

        // Target1, LU1 to LU16
        client.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        client.RunCommand "select 0" "" "T > "
        for i = 1 to 16 do
            client.RunCommand ( sprintf "create /l %d" i ) "Created" "T > "
            client.RunCommand ( sprintf "select %d" ( i - 1 ) ) "" "LU> "
            client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "LU> "
            client.RunCommand "unselect" "" "T > "
        client.RunCommand "unselect" "" "TG> "

        // Target2, LU17 to LU32
        client.RunCommand "create /n iqn.2020-05.example.com:target2" "Created" "TG> "
        client.RunCommand "select 1" "" "T > "
        for i = 1 to 16 do
            client.RunCommand ( sprintf "create /l %d" ( i + 16 ) ) "Created" "T > "
            client.RunCommand ( sprintf "select %d" ( i - 1 ) ) "" "LU> "
            client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "LU> "
            client.RunCommand "unselect" "" "T > "
        client.RunCommand "unselect" "" "TG> "
        client.RunCommand "unselect" "" "TD> "
        client.RunCommand "unselect" "" "CR> "

        // Target device 2, Target group 1
        client.RunCommand "create" "Created" "CR> "
        client.RunCommand "select 1" "" "TD> "
        client.RunCommand "set loglevel VERBOSE" "" "TD> "
        client.RunCommand "create targetgroup" "Created" "TD> "
        client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo3 ) "Created" "TD> "
        client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo4 ) "Created" "TD> "
        client.RunCommand "select 0" "" "TG> "

        // Target1, LU1 to LU16
        client.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        client.RunCommand "select 0" "" "T > "
        for i = 1 to 16 do
            client.RunCommand ( sprintf "create /l %d" i ) "Created" "T > "
            client.RunCommand ( sprintf "select %d" ( i - 1 ) ) "" "LU> "
            client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "LU> "
            client.RunCommand "unselect" "" "T > "
        client.RunCommand "unselect" "" "TG> "

        // Target2, LU1 to LU16
        client.RunCommand "create /n iqn.2020-05.example.com:target2" "Created" "TG> "
        client.RunCommand "select 1" "" "T > "
        for i = 1 to 16 do
            client.RunCommand ( sprintf "attach /l %d" i ) "Attach LU" "T > "
        client.RunCommand "unselect" "" "TG> "
        client.RunCommand "unselect" "" "TD> "
        client.RunCommand "unselect" "" "CR> "

        // publish and start TD
        client.RunCommand "validate" "All configurations are vlidated" "CR> "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "CR> "
        client.RunCommand "select 0" "" "TD> "
        client.RunCommand "start" "Started" "TD> "
        client.RunCommand "unselect" "" "CR> "
        client.RunCommand "select 1" "" "TD> "
        client.RunCommand "start" "Started" "TD> "

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

    interface ICollectionFixture<SCSI_Configuration_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.iSCSIPortNo1 = m_iSCSIPortNo1
    member _.iSCSIPortNo2 = m_iSCSIPortNo2
    member _.iSCSIPortNo3 = m_iSCSIPortNo3
    member _.iSCSIPortNo4 = m_iSCSIPortNo4
    member _.MediaSize = m_MediaSize
    member _.MediaBlockSize = 
        if Constants.MEDIA_BLOCK_SIZE = 512UL then     // 4096 or 512 bytes
            Blocksize.BS_512
        else
            Blocksize.BS_4096

[<Collection( "SCSI_Configuration" )>]
type SCSI_Configuration( fx : SCSI_Configuration_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL
    let g_LUN17 = lun_me.fromPrim 17UL
    let iSCSIPortNo1 = fx.iSCSIPortNo1
    let iSCSIPortNo2 = fx.iSCSIPortNo2
    let iSCSIPortNo3 = fx.iSCSIPortNo3
    let iSCSIPortNo4 = fx.iSCSIPortNo4
    let m_MediaSize = fx.MediaSize
    let m_MediaBlockSize = fx.MediaBlockSize
    let m_ClientProc = fx.clientProc

    // default session parameters( for Target 1 )
    let m_defaultSessParam1 = {
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

    // default session parameters( for Target 2 )
    let m_defaultSessParam2 = {
        m_defaultSessParam1 with
            TargetName = "iqn.2020-05.example.com:target2";
    }

    // default connection parameters ( for network portal 1 )
    let m_defaultConnParam1 = {
        PortNo = iSCSIPortNo1;
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

    // default connection parameters ( for network portal 1 )
    let m_defaultConnParam2 = {
        m_defaultConnParam1 with
            PortNo = iSCSIPortNo2;
    }


    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Theory>]
    [<InlineData( "iqn.2020-05.example.com:target1", 0UL, 0, 1 )>]    // Target Device 1, Target 1
    [<InlineData( "iqn.2020-05.example.com:target2", 16UL, 0, 1 )>]   // Target Device 1, Target 2
    [<InlineData( "iqn.2020-05.example.com:target1", 0UL, 2, 3 )>]    // Target Device 2, Target 1
    [<InlineData( "iqn.2020-05.example.com:target2", 0UL, 2, 3 )>]    // Target Device 2, Target 2
    member _.ReportLUNs_001 ( target : string ) ( lub : uint64 ) ( port1 : int ) ( port2 : int ) =
        task {
            let nport = [| iSCSIPortNo1; iSCSIPortNo2; iSCSIPortNo3; iSCSIPortNo4 |]
            let sessconf = { m_defaultSessParam1 with TargetName = target }
            let conconf1 = { m_defaultConnParam1 with PortNo = nport.[ port1 ] }
            let conconf2 = { m_defaultConnParam1 with PortNo = nport.[ port2 ] }
            let! r1_1 = SCSI_Initiator.Create sessconf conconf1   // network portal 2
            let! r1_2 = SCSI_Initiator.Create sessconf conconf2   // network portal 2

            let expLUNs1 = [|
                yield lun_me.fromPrim 0UL;
                for i = 1UL to 16UL do
                    yield lun_me.fromPrim ( i + lub );
            |]

            // Send ReportLUNs command to LUN 0. The result will be the same regardless of which network portal to use.
            // Only LUNs accessible are returned.
            let! itt_11_1 = r1_1.Send_ReportLUNs TaskATTRCd.SIMPLE_TASK g_LUN0 2uy 255u NACA.T
            let! _, res_11_1 = r1_1.Wait_ReportLUNs itt_11_1
            Assert.True(( ( Array.sort res_11_1 ) = expLUNs1 ))

            let! itt_12_1 = r1_2.Send_ReportLUNs TaskATTRCd.SIMPLE_TASK g_LUN0 2uy 255u NACA.T
            let! _, res_12_1 = r1_2.Wait_ReportLUNs itt_12_1
            Assert.True(( ( Array.sort res_12_1 ) = expLUNs1 ))

            // Verify that the reported LUNs are accessible.
            for lun in res_11_1 do
                let! itt = r1_1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0uy 255us NACA.T
                let! _ = r1_1.Wait_Inquiry_Standerd itt
                ()
            for lun in res_12_1 do
                let! itt = r1_2.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0uy 255us NACA.T
                let! _ = r1_2.Wait_Inquiry_Standerd itt
                ()

            // Send the ReportLUNs command to an LU other than LUN 0.
            let! itt_11_2 = r1_1.Send_ReportLUNs TaskATTRCd.SIMPLE_TASK ( lun_me.fromPrim ( 1UL + lub ) ) 2uy 255u NACA.T
            let! _, res_11_2 = r1_1.Wait_ReportLUNs itt_11_2
            Assert.True(( ( Array.sort res_11_2 ) = expLUNs1 ))

            let! itt_12_2 = r1_2.Send_ReportLUNs TaskATTRCd.SIMPLE_TASK ( lun_me.fromPrim ( 1UL + lub ) ) 2uy 255u NACA.T
            let! _, res_12_2 = r1_2.Wait_ReportLUNs itt_12_2
            Assert.True(( ( Array.sort res_12_2 ) = expLUNs1 ))

            // Verify that the reported LUNs are accessible.
            for lun in res_11_2 do
                let! itt = r1_1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0uy 255us NACA.T
                let! _ = r1_1.Wait_Inquiry_Standerd itt
                ()
            for lun in res_12_2 do
                let! itt = r1_2.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0uy 255us NACA.T
                let! _ = r1_2.Wait_Inquiry_Standerd itt
                ()

            do! r1_1.Close()
            do! r1_2.Close()
        }

    // Verify that the same LU can be accessed through different network portals and targets.
    [<Theory>]
    [<InlineData( "iqn.2020-05.example.com:target1", "iqn.2020-05.example.com:target1", 0, 1, 1UL, 16UL )>]
    [<InlineData( "iqn.2020-05.example.com:target2", "iqn.2020-05.example.com:target2", 0, 1, 17UL, 32UL )>]
    [<InlineData( "iqn.2020-05.example.com:target1", "iqn.2020-05.example.com:target2", 2, 3, 1UL, 16UL )>]
    member _.LUAccess_001 ( target1 : string ) ( target2 : string ) ( port1 : int ) ( port2 : int ) ( vlun1 : uint64 ) ( vlun2 : uint64 ) =
        task {
            let nport = [| iSCSIPortNo1; iSCSIPortNo2; iSCSIPortNo3; iSCSIPortNo4 |]
            let sessconf1 = { m_defaultSessParam1 with TargetName = target1 }
            let sessconf2 = { m_defaultSessParam1 with TargetName = target2 }
            let conconf1 = { m_defaultConnParam1 with PortNo = nport.[ port1 ] }
            let conconf2 = { m_defaultConnParam1 with PortNo = nport.[ port2 ] }
            let! r1 = SCSI_Initiator.Create sessconf1 conconf1
            let! r2 = SCSI_Initiator.Create sessconf2 conconf2
            let writeData1 = PooledBuffer.Rent( Blocksize.toUInt32 m_MediaBlockSize |> int32 )

            for itr = vlun1 to vlun2 do
                let lun = lun_me.fromPrim itr
                Random.Shared.NextBytes( writeData1.ArraySegment.AsSpan() )

                // write
                let! itt_w1 = r1.Send_Write10 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 m_MediaBlockSize writeData1 NACA.T
                let! _ = r1.WaitSCSIResponseGoogStatus itt_w1

                // read
                let! itt_r1 = r2.Send_Read10 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
                let! res_r1 = r2.WaitSCSIResponseGoogStatus itt_r1
                Assert.True(( PooledBuffer.ValueEquals writeData1 res_r1 ))

                Random.Shared.NextBytes( writeData1.ArraySegment.AsSpan() )

                // write
                let! itt_w2 = r2.Send_Write10 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 m_MediaBlockSize writeData1 NACA.T
                let! _ = r2.WaitSCSIResponseGoogStatus itt_w2

                // read
                let! itt_r2 = r1.Send_Read10 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
                let! res_r2 = r1.WaitSCSIResponseGoogStatus itt_r2
                Assert.True(( PooledBuffer.ValueEquals writeData1 res_r2 ))

            writeData1.Return()
            do! r1.Close()
            do! r2.Close()
        }
