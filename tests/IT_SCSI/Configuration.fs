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
open System.Collections.Generic
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
                let! _ = r1.WaitSCSIResponseGoodStatus itt_w1

                // read
                let! itt_r1 = r2.Send_Read10 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
                let! res_r1 = r2.WaitSCSIResponseGoodStatus itt_r1
                Assert.True(( PooledBuffer.ValueEquals writeData1 res_r1 ))

                Random.Shared.NextBytes( writeData1.ArraySegment.AsSpan() )

                // write
                let! itt_w2 = r2.Send_Write10 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 m_MediaBlockSize writeData1 NACA.T
                let! _ = r2.WaitSCSIResponseGoodStatus itt_w2

                // read
                let! itt_r2 = r1.Send_Read10 TaskATTRCd.SIMPLE_TASK lun blkcnt_me.zero32 m_MediaBlockSize ( blkcnt_me.ofUInt16 1us ) NACA.T
                let! res_r2 = r1.WaitSCSIResponseGoodStatus itt_r2
                Assert.True(( PooledBuffer.ValueEquals writeData1 res_r2 ))

            writeData1.Return()
            do! r1.Close()
            do! r2.Close()
        }

    // Verify that the same LU can be accessed through different network portals and targets.
    [<Theory>]
    [<InlineData( "iqn.2020-05.example.com:target1", "iqn.2020-05.example.com:target1", 0, 1, 0UL, 16UL )>]
    [<InlineData( "iqn.2020-05.example.com:target2", "iqn.2020-05.example.com:target2", 0, 1, 17UL, 32UL )>]
    [<InlineData( "iqn.2020-05.example.com:target1", "iqn.2020-05.example.com:target2", 2, 3, 0UL, 16UL )>]
    member _.LUAccess_002 ( target1 : string ) ( target2 : string ) ( port1 : int ) ( port2 : int ) ( vlun1 : uint64 ) ( vlun2 : uint64 ) =
        task {
            let nport = [| iSCSIPortNo1; iSCSIPortNo2; iSCSIPortNo3; iSCSIPortNo4 |]
            let sessconf1 = { m_defaultSessParam1 with TargetName = target1 }
            let sessconf2 = { m_defaultSessParam1 with TargetName = target2 }
            let conconf1 = { m_defaultConnParam1 with PortNo = nport.[ port1 ] }
            let conconf2 = { m_defaultConnParam1 with PortNo = nport.[ port2 ] }
            let! r1 = SCSI_Initiator.Create sessconf1 conconf1
            let! r2 = SCSI_Initiator.Create sessconf2 conconf2
            let vResKey =
                [|
                    for itr = vlun1 to vlun2 do
                        yield ( lun_me.fromPrim itr, resvkey_me.fromPrim ( itr * 97UL + 1UL ) )
                |]
                |> Array.map KeyValuePair
                |> Dictionary

            // register reservation key
            for itr in vResKey do
                let! itt_pr_out1 = r1.Send_PROut_REGISTER TaskATTRCd.SIMPLE_TASK itr.Key NACA.T resvkey_me.zero itr.Value SPEC_I_PT.F ALL_TG_PT.F APTPL.F [||]
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out1
                ()

            // Get reservarion key
            for itr in vResKey do
                let! itt_pr_in1 = r1.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK itr.Key 0uy 256us NACA.T
                let! res_pr_in1 = r1.Wait_PersistentReserveIn_ReadKey itt_pr_in1
                Assert.True(( res_pr_in1.ReservationKey.Length = 1 ))
                Assert.True(( res_pr_in1.ReservationKey.[0] = itr.Value ))

                let! itt_pr_in2 = r2.Send_PersistentReserveIn TaskATTRCd.SIMPLE_TASK itr.Key 0uy 256us NACA.T
                let! res_pr_in2 = r2.Wait_PersistentReserveIn_ReadKey itt_pr_in2
                Assert.True(( res_pr_in2.ReservationKey.Length = 1 ))
                Assert.True(( res_pr_in2.ReservationKey.[0] = itr.Value ))

            // clear reservation key
            for itr in vResKey do
                let! itt_pr_out2 = r1.Send_PROut_CLEAR TaskATTRCd.SIMPLE_TASK itr.Key NACA.T itr.Value
                let! _ = r1.WaitSCSIResponseGoodStatus itt_pr_out2
                ()

            do! r1.Close()
            do! r2.Close()
        }

    // Check the operation codes supported
    [<Theory>]
    [<InlineData( 0UL, 26 )>]
    [<InlineData( 1UL, 39 )>]
    member _.GetOpcode_001 ( argLUN : uint64 ) ( exp : int ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam1 m_defaultConnParam1
            let lun = lun_me.fromPrim argLUN
            let! itt_rsoc = r1.Send_ReportSupportedOperationCodes TaskATTRCd.SIMPLE_TASK lun 0uy 0uy 0us 1024u NACA.T
            let! res_rsoc = r1.Wait_ReportSupportedOperationCodes_AllCommand itt_rsoc
            Assert.True(( res_rsoc.Descs.Length = exp ))
            do! r1.Close()
        }

    static member NotSupportedOperationCode_001_data : obj[][] = [|
        [|  0x0UL; 11; [| 0xA4uy; 0x0Buy; |]; |]  // CHANGE ALIASES
        [|  0x0UL; 15; [| 0x83uy |];          |]  // EXTENDED COPY
        [|  0x0UL;  9; [| 0x4Cuy |];          |]  // LOG SELECT
        [|  0x0UL;  9; [| 0x4Duy |];          |]  // LOG SENSE
        [|  0x0UL;  5; [| 0x1Euy |];          |]  // PREVENT ALLOW MEDIUM REMOVAL
        [|  0x0UL; 15; [| 0x8Cuy |];          |]  // READ ATTRIBUTE
        [|  0x0UL;  9; [| 0x3Cuy |];          |]  // READ BUFFER
        [|  0x0UL; 11; [| 0xABuy; 0x01uy; |]; |]  // READ MEDIA SERIAL NUMBER
        [|  0x0UL; 15; [| 0x84uy |];          |]  // RECEIVE COPY RESULTS
        [|  0x0UL;  5; [| 0x1Cuy |];          |]  // RECEIVE DIAGNOSTIC RESULTS
        [|  0x0UL; 11; [| 0xA3uy; 0x0Buy; |]; |]  // REPORT ALIASES
        [|  0x0UL; 11; [| 0xA3uy; 0x05uy; |]; |]  // REPORT DEVICE IDENTIFIER
        [|  0x0UL; 11; [| 0xA3uy; 0x0Euy; |]; |]  // REPORT PRIORITY
        [|  0x0UL; 11; [| 0xA3uy; 0x0Auy; |]; |]  // REPORT TARGET PORT GROUPS
        [|  0x0UL; 11; [| 0xA3uy; 0x0Fuy; |]; |]  // REPORT TIMESTAMP
        [|  0x0UL;  5; [| 0x1Duy; |];         |]  // SEND DIAGNOSTIC
        [|  0x0UL; 11; [| 0xA4uy; 0x06uy; |]; |]  // SET DEVICE IDENTIFIER
        [|  0x0UL; 11; [| 0xA4uy; 0x0Euy; |]; |]  // SET PRIORITY
        [|  0x0UL; 11; [| 0xA4uy; 0x0Auy; |]; |]  // SET TARGET PORT GROUPS
        [|  0x0UL; 11; [| 0xA4uy; 0x0Fuy; |]; |]  // SET TIMESTAMP
        [|  0x0UL; 15; [| 0x8Duy; |];         |]  // WRITE ATTRIBUTE
        [|  0x0UL;  9; [| 0x3Buy; |];         |]  // WRITE BUFFER
        [|  0x0UL; 15; [| 0x86uy; |];         |]  // ACCESS CONTROL IN
        [|  0x0UL; 15; [| 0x87uy; |];         |]  // ACCESS CONTROL OUT
        [|  0x0UL;  5; [| 0x04uy; |];         |]  // FORMAT UNIT
        [|  0x0UL;  9; [| 0x34uy; |];         |]  // PRE-FETCH(10)
        [|  0x0UL; 15; [| 0x90uy; |];         |]  // PRE-FETCH(16)
        [|  0x0UL;  5; [| 0x08uy; |];         |]  // READ(6)
        [|  0x0UL;  9; [| 0x28uy; |];         |]  // READ(10)
        [|  0x0UL; 11; [| 0xA8uy; |];         |]  // READ(12)
        [|  0x0UL; 15; [| 0x88uy; |];         |]  // READ(16)
        [|  0x0UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x09uy; |];         |]  // READ(32)
        [|  0x0UL;  9; [| 0x37uy; |];         |]  // READ DEFECT DATA(10)
        [|  0x0UL; 11; [| 0xB7uy; |];         |]  // READ DEFECT DATA(12)
        [|  0x0UL;  9; [| 0x3Euy; |];         |]  // READ LONG(10)
        [|  0x0UL; 15; [| 0x9Euy; 0x11uy |];  |]  // READ LONG(16)
        [|  0x0UL;  5; [| 0x07uy; |];         |]  // REASSIGN BLOCKS
        [|  0x0UL;  5; [| 0x1Buy; |];         |]  // START STOP UNIT
        [|  0x0UL;  9; [| 0x2Fuy; |];         |]  // VERIFY(10)
        [|  0x0UL; 11; [| 0xAFuy; |];         |]  // VERIFY(12)
        [|  0x0UL; 15; [| 0x8Fuy; |];         |]  // VERIFY(16)
        [|  0x0UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x0Auy; |];         |]  // VERIFY(32)
        [|  0x0UL;  5; [| 0x0Auy; |];         |]  // WRITE(6)
        [|  0x0UL;  9; [| 0x2Auy; |];         |]  // WRITE(10)
        [|  0x0UL; 11; [| 0xAAuy; |];         |]  // WRITE(12)
        [|  0x0UL; 15; [| 0x8Auy; |];         |]  // WRITE(16)
        [|  0x0UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x0Buy; |];         |]  // WRITE(32)
        [|  0x0UL;  9; [| 0x2Euy; |];         |]  // WRITE AND VERIFY(10)
        [|  0x0UL; 11; [| 0xAEuy; |];         |]  // WRITE AND VERIFY(12)
        [|  0x0UL; 15; [| 0x8Euy; |];         |]  // WRITE AND VERIFY(16)
        [|  0x0UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x0Cuy; |];         |]  // WRITE AND VERIFY(32)
        [|  0x0UL;  9; [| 0x3Fuy; |];         |]  // WRITE LONG(10)
        [|  0x0UL; 15; [| 0x9Fuy; |];         |]  // WRITE LONG(16)
        [|  0x0UL;  9; [| 0x41uy; |];         |]  // WRITE SAME(10)
        [|  0x0UL; 15; [| 0x93uy; |];         |]  // WRITE SAME(16)
        [|  0x0UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x0Duy; |];         |]  // WRITE SAME(32)
        [|  0x0UL;  9; [| 0x52uy; |];         |]  // XDREAD(10)
        [|  0x0UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x03uy; |];         |]  // XDREAD(32)
        [|  0x0UL;  9; [| 0x50uy; |];         |]  // XDWRITE(10)
        [|  0x0UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x04uy; |];         |]  // XDWRITE(32)
        [|  0x0UL;  9; [| 0x53uy; |];         |]  // XDWRITEREAD(10)
        [|  0x0UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x07uy; |];         |]  // XDWRITEREAD(32)
        [|  0x0UL;  9; [| 0x51uy; |];         |]  // XPWRITE(10)
        [|  0x0UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x06uy; |];         |]  // XPWRITE(32)
        [|  0x1UL; 11; [| 0xA4uy; 0x0Buy; |]; |]  // CHANGE ALIASES
        [|  0x1UL; 15; [| 0x83uy |];          |]  // EXTENDED COPY
        [|  0x1UL;  9; [| 0x4Cuy |];          |]  // LOG SELECT
        [|  0x1UL;  9; [| 0x4Duy |];          |]  // LOG SENSE
        [|  0x1UL;  5; [| 0x1Euy |];          |]  // PREVENT ALLOW MEDIUM REMOVAL
        [|  0x1UL; 15; [| 0x8Cuy |];          |]  // READ ATTRIBUTE
        [|  0x1UL;  9; [| 0x3Cuy |];          |]  // READ BUFFER
        [|  0x1UL; 11; [| 0xABuy; 0x01uy; |]; |]  // READ MEDIA SERIAL NUMBER
        [|  0x1UL; 15; [| 0x84uy |];          |]  // RECEIVE COPY RESULTS
        [|  0x1UL;  5; [| 0x1Cuy |];          |]  // RECEIVE DIAGNOSTIC RESULTS
        [|  0x1UL; 11; [| 0xA3uy; 0x0Buy; |]; |]  // REPORT ALIASES
        [|  0x1UL; 11; [| 0xA3uy; 0x05uy; |]; |]  // REPORT DEVICE IDENTIFIER
        [|  0x1UL; 11; [| 0xA3uy; 0x0Euy; |]; |]  // REPORT PRIORITY
        [|  0x1UL; 11; [| 0xA3uy; 0x0Auy; |]; |]  // REPORT TARGET PORT GROUPS
        [|  0x1UL; 11; [| 0xA3uy; 0x0Fuy; |]; |]  // REPORT TIMESTAMP
        [|  0x1UL;  5; [| 0x1Duy; |];         |]  // SEND DIAGNOSTIC
        [|  0x1UL; 11; [| 0xA4uy; 0x06uy; |]; |]  // SET DEVICE IDENTIFIER
        [|  0x1UL; 11; [| 0xA4uy; 0x0Euy; |]; |]  // SET PRIORITY
        [|  0x1UL; 11; [| 0xA4uy; 0x0Auy; |]; |]  // SET TARGET PORT GROUPS
        [|  0x1UL; 11; [| 0xA4uy; 0x0Fuy; |]; |]  // SET TIMESTAMP
        [|  0x1UL; 15; [| 0x8Duy; |];         |]  // WRITE ATTRIBUTE
        [|  0x1UL;  9; [| 0x3Buy; |];         |]  // WRITE BUFFER
        [|  0x1UL; 15; [| 0x86uy; |];         |]  // ACCESS CONTROL IN
        [|  0x1UL; 15; [| 0x87uy; |];         |]  // ACCESS CONTROL OUT
        [|  0x1UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x09uy; |];         |]  // READ(32)
        [|  0x1UL;  9; [| 0x37uy; |];         |]  // READ DEFECT DATA(10)
        [|  0x1UL; 11; [| 0xB7uy; |];         |]  // READ DEFECT DATA(12)
        [|  0x1UL;  9; [| 0x3Euy; |];         |]  // READ LONG(10)
        [|  0x1UL; 15; [| 0x9Euy; 0x11uy |];  |]  // READ LONG(16)
        [|  0x1UL;  5; [| 0x07uy; |];         |]  // REASSIGN BLOCKS
        [|  0x1UL;  5; [| 0x1Buy; |];         |]  // START STOP UNIT
        [|  0x1UL;  9; [| 0x2Fuy; |];         |]  // VERIFY(10)
        [|  0x1UL; 11; [| 0xAFuy; |];         |]  // VERIFY(12)
        [|  0x1UL; 15; [| 0x8Fuy; |];         |]  // VERIFY(16)
        [|  0x1UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x0Auy; |];         |]  // VERIFY(32)
        [|  0x1UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x0Buy; |];         |]  // WRITE(32)
        [|  0x1UL;  9; [| 0x2Euy; |];         |]  // WRITE AND VERIFY(10)
        [|  0x1UL; 11; [| 0xAEuy; |];         |]  // WRITE AND VERIFY(12)
        [|  0x1UL; 15; [| 0x8Euy; |];         |]  // WRITE AND VERIFY(16)
        [|  0x1UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x0Cuy; |];         |]  // WRITE AND VERIFY(32)
        [|  0x1UL;  9; [| 0x3Fuy; |];         |]  // WRITE LONG(10)
        [|  0x1UL; 15; [| 0x9Fuy; |];         |]  // WRITE LONG(16)
        [|  0x1UL;  9; [| 0x41uy; |];         |]  // WRITE SAME(10)
        [|  0x1UL; 15; [| 0x93uy; |];         |]  // WRITE SAME(16)
        [|  0x1UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x0Duy; |];         |]  // WRITE SAME(32)
        [|  0x1UL;  9; [| 0x52uy; |];         |]  // XDREAD(10)
        [|  0x1UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x03uy; |];         |]  // XDREAD(32)
        [|  0x1UL;  9; [| 0x50uy; |];         |]  // XDWRITE(10)
        [|  0x1UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x04uy; |];         |]  // XDWRITE(32)
        [|  0x1UL;  9; [| 0x53uy; |];         |]  // XDWRITEREAD(10)
        [|  0x1UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x07uy; |];         |]  // XDWRITEREAD(32)
        [|  0x1UL;  9; [| 0x51uy; |];         |]  // XPWRITE(10)
        [|  0x1UL;  1; [| 0x7Fuy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x18uy; 0x00uy; 0x06uy; |];         |]  // XPWRITE(32)
    |]

    // Check the operation codes not supported
    [<Theory>]
    [<MemberData( "NotSupportedOperationCode_001_data" )>]
    member _.NotSupportedOperationCode_001 ( argLUN : uint64 ) ( ctrlpos : int ) ( cdbparam : byte[] ) =
        task {
            let! r1 = SCSI_Initiator.Create m_defaultSessParam1 m_defaultConnParam1

            // create unuported CDB bytes
            let lun = lun_me.fromPrim argLUN
            let cdb = Array.zeroCreate<byte> 16
            Array.blit cdbparam 0 cdb 0 ( min 16 cdbparam.Length )
            cdb.[ctrlpos] <- 0x04uy

            // send unsupported CDB.
            let! itt = r1.SendSCSICommand TaskATTRCd.SIMPLE_TASK lun cdb PooledBuffer.Empty 0u
            let! res = r1.WaitSCSIResponse itt
            Assert.True(( res.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( res.Sense.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( res.Sense.Value.ASC = ASCCd.INVALID_COMMAND_OPERATION_CODE ))

            // Errors with unknown operation codes are always treated as CA, regardless of the value of the control byte.
            let! itt_i = r1.Send_Inquiry TaskATTRCd.SIMPLE_TASK lun EVPD.T 0uy 256us NACA.T
            let! res_i = r1.Wait_Inquiry_Standerd itt_i

            do! r1.Close()
        }
