namespace Haruka.Test.IT.ISCSI

open System
open System.Threading
open System.Threading.Tasks
open System.IO
open System.Diagnostics
open System.Net.Sockets

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.Test
open System.Net

type ClientConfigTest() =

    let workPath =
        let tempPath = Path.GetTempPath()
        Functions.AppendPathName tempPath ( Guid.NewGuid().ToString( "N" ) )

    let controllPortNo = GlbFunc.nextTcpPortNo()
    let iscsiPortNo = GlbFunc.nextTcpPortNo()

    // default session parameters
    let m_defaultSessParam = {
        InitiatorName = "2020-05.example.com:initiator";
        InitiatorAlias = "aaa";
        TargetName = "2020-05.example.com:target1";
        TargetAlias = "";
        ISID = isid_me.fromPrim 1UL;
        TSIH = tsih_me.fromPrim 0us;
        MaxConnections = 16us;
        InitialR2T = false;
        ImmediateData = true;
        MaxBurstLength = 262144u;
        FirstBurstLength = 262144u;
        DefaultTime2Wait = 2us;
        DefaultTime2Retain = 20us;
        MaxOutstandingR2T = 16us;
        DataPDUInOrder = false;
        DataSequenceInOrder = false;
        ErrorRecoveryLevel = 0uy;
    }

    // default connection parameters
    let m_defaultConnParam = {
        PortNo = iscsiPortNo;
        CID = cid_me.zero;
        Initiator_UserName = "";
        Initiator_Password = "";
        Target_UserName = "";
        Target_Password = "";
        HeaderDigest = DigestType.DST_CRC32C;
        DataDigest = DigestType.DST_CRC32C;
        MaxRecvDataSegmentLength_I = 262144u;
        MaxRecvDataSegmentLength_T = 262144u;
    }

    let SendReadCapacityCDB ( sess : iSCSI_Initiator ) ( lun : LUN_T ) : Task< struct( uint64 * uint32 ) > =
        task {
            // Read Capacity(16) CDB
            let cdb = [|
                0x9Euy; // Operation code
                0x10uy; // Service action
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LBA
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LBA
                0x00uy; 0x00uy; 0x00uy; 0x20uy; // Allocation length
                0x00uy; // PMI
                0x00uy; // Controll
            |]
            let cid = sess.CID.[0]
            let! struct( itt1, cmdsn1 ) = sess.SendSCSICommandPDU cid false true true false TaskATTRCd.SIMPLE_TASK lun 32u cdb PooledBuffer.Empty 0u
            let! resp1 = sess.ReceiveSpecific<SCSIDataInPDU>( cid )
            let! resp2 = sess.ReceiveSpecific<SCSIResponsePDU>( cid )

            Assert.True(( resp2.Response = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( resp2.Status = ScsiCmdStatCd.GOOD ))
            Assert.True(( resp2.ExpCmdSN = cmdsn1 + cmdsn_me.fromPrim 1u))

            let blockcount =
                BitConverter.ToInt64( resp1.DataSegment.Array, resp1.DataSegment.Offset + 0 )
                |> IPAddress.NetworkToHostOrder
                |> uint64
                |> (+) 1UL

            let blockSize =
                BitConverter.ToInt32( resp1.DataSegment.Array, resp1.DataSegment.Offset + 8 )
                |> IPAddress.NetworkToHostOrder
                |> uint32

            return struct( blockcount, blockSize )
        }

    // Test the interaction between configuration changes by the client and access by the initiator.
    [<Fact>]
    member _.Test() =
        task {
            // Initialize working folder, Start controller and client process
            let controller, client = TestFunctions.StartHarukaController workPath controllPortNo

            // Add and start the target device.
            client.RunCommand "create" "Created" "CR> "
            client.RunCommand "select 0" "" "TD> "
            client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" iscsiPortNo ) "Created" "TD> "
            client.RunCommand "create targetgroup" "Created" "TD> "
            client.RunCommand "set LogParameters.LogLevel VERBOSE" "" "TD> "
            client.RunCommand "select 1" "" "TG> "
        
            client.RunCommand "create /n 2020-05.example.com:target1" "Created" "TG> "      // target1, LU=1, No auth required, 64KB
            client.RunCommand "select 0" "" "T > "
            client.RunCommand "create /l 1" "Created" "T > "
            client.RunCommand "select 0" "" "LU> "
            client.RunCommand "create debug" "Created" "LU> "
            client.RunCommand "select 0" "" "MD> "
            client.RunCommand "create membuffer /s 65536" "Created" "MD> "
            client.RunCommand "unselect" "" "LU> "
            client.RunCommand "unselect" "" "T > "
            client.RunCommand "unselect" "" "TG> "

            client.RunCommand "validate" "All configurations are vlidated" "TG> "
            client.RunCommand "publish" "All configurations are uploaded to the controller" "TG> "
            client.RunCommand "start" "Started" "TG> "

            // Access configured media. target1, LU=1
            let sessParam1 = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
                    TargetName = "2020-05.example.com:target1";
            }
            let! r1 = iSCSI_Initiator.CreateInitialSession sessParam1 m_defaultConnParam
            let! expBlockCount1, expBlockSize1 = SendReadCapacityCDB r1 ( lun_me.fromPrim 1UL )
            Assert.True(( expBlockCount1 * ( uint64 expBlockSize1 ) = 65536UL ))
            let! _ = r1.SendLogoutRequestPDU r1.CID.[0] false LogoutReqReasonCd.CLOSE_SESS r1.CID.[0]
            let! rpdu1 = r1.ReceiveSpecific<LogoutResponsePDU> r1.CID.[0]
            Assert.True(( rpdu1.Response = LogoutResCd.SUCCESS ))

            // Configuration change during TargetDevice running. failed.
            client.RunCommand "unselect" "" "TD> "
            client.RunCommand "select 0" "" "NP> "
            client.RunCommand "set RECEIVEBUFFERSIZE 16384" "Unexpected error." "NP> "
            client.RunCommand "reload" "" "CR> "

            // Stop TargetDevice and then change the configuration. success.
            client.RunCommand "select 0" "" "TD> "
            client.RunCommand "kill" "Killed : Target Device" "TD> "
            client.RunCommand "select 0" "" "NP> "
            client.RunCommand "set RECEIVEBUFFERSIZE 16384" "" "NP> "
            client.RunCommand "validate" "All configurations are vlidated" "NP> "
            client.RunCommand "publish" "All configurations are uploaded to the controller" "NP> "
            client.RunCommand "start" "Started" "NP> "

            // Access configured media. target1, LU=1
            let sessParam2 = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
                    TargetName = "2020-05.example.com:target1";
            }
            let! r2 = iSCSI_Initiator.CreateInitialSession sessParam2 m_defaultConnParam
            let! expBlockCount2, expBlockSize2 = SendReadCapacityCDB r2 ( lun_me.fromPrim 1UL )
            Assert.True(( expBlockCount2 * ( uint64 expBlockSize2 ) = 65536UL ))

            // Change the configuration of an Active TargetGroup. failed
            client.RunCommand "unselect" "" "TD> "
            client.RunCommand "select 1" "" "TG> "
            client.RunCommand "create /n 2020-05.example.com:target2" "Unexpected error." "TG> "

            // Add a TargetGroup while TargetDevice is running.
            client.RunCommand "unselect" "" "TD> "
            client.RunCommand "create targetgroup" "Created" "TD> "
            client.RunCommand "select 2" "" "TG> "
            client.RunCommand "create /n 2020-05.example.com:target2" "Created" "TG> "
            client.RunCommand "select 0" "" "T > "
            client.RunCommand "create /l 2" "Created" "T > "
            client.RunCommand "select 0" "" "LU> "
            client.RunCommand "create debug" "Created" "LU> "
            client.RunCommand "select 0" "" "MD> "
            client.RunCommand "create membuffer /s 32768" "Created" "MD> "
            client.RunCommand "validate" "All configurations are vlidated" "MD> "
            client.RunCommand "publish" "All configurations are uploaded to the controller" "MD> "
            client.RunCommand "unselect" "" "LU> "
            client.RunCommand "unselect" "" "T > "
            client.RunCommand "unselect" "" "TG> "

            // Start the added TargetGroup.
            client.RunCommand "load" "Loaded" "TG> "
            client.RunCommand "activate" "Activated" "TG> "

            // Access configured media. target2, LU=2
            let sessParam3 = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
                    TargetName = "2020-05.example.com:target2";
            }
            let! r3 = iSCSI_Initiator.CreateInitialSession sessParam3 m_defaultConnParam
            let! expBlockCount3, expBlockSize3 = SendReadCapacityCDB r3 ( lun_me.fromPrim 2UL )
            Assert.True(( expBlockCount3 * ( uint64 expBlockSize3 ) = 32768UL ))

            // Inactivate the TargetGroup.
            client.RunCommand "inactivate" "Inactivated" "TG> "

            // Access configured media. target2, LU=2. failed.
            try
                let sessParam4 = {
                    m_defaultSessParam with
                        ISID = GlbFunc.newISID();
                        TargetName = "2020-05.example.com:target2";
                }
                let! _ = iSCSI_Initiator.CreateInitialSession sessParam4 m_defaultConnParam
                Assert.Fail __LINE__
            with
            | :? SessionRecoveryException as x ->
                ()

            // unload the inactivated TargetGroup. failed.
            client.RunCommand "unload" "Unexpected request" "TG> "

            // logout
            let! _ = r3.SendLogoutRequestPDU r3.CID.[0] false LogoutReqReasonCd.CLOSE_SESS r3.CID.[0]
            let! rpdu3 = r3.ReceiveSpecific<LogoutResponsePDU> r3.CID.[0]
            Assert.True(( rpdu3.Response = LogoutResCd.SUCCESS ))

            // unload the inactivated TargetGroup. success.
            do! Task.Delay 500
            let mutable cnt = 0
            while cnt < 20 do
                try
                    client.RunCommand "unload" "Unloaded" "TG> "
                    do! Task.Delay 500
                    cnt <- 999
                with
                | :? TestException ->
                    cnt <- cnt + 1
            Assert.True(( cnt = 999 ))

            // Update the configuration of the unloaded TargetGroup.
            client.RunCommand "select 0" "" "T > "
            client.RunCommand "create /l 3" "Created" "T > "
            client.RunCommand "select 1" "" "LU> "
            client.RunCommand "create debug" "Created" "LU> "
            client.RunCommand "select 0" "" "MD> "
            client.RunCommand "create membuffer /s 16384" "Created" "MD> "
            client.RunCommand "validate" "All configurations are vlidated" "MD> "
            client.RunCommand "publish" "All configurations are uploaded to the controller" "MD> "
            client.RunCommand "load" "Loaded" "MD> "
            client.RunCommand "activate" "Activated" "MD> "

            // Access configured media. target2, LU=2 and 3. success.
            let sessParam5 = {
                m_defaultSessParam with
                    ISID = GlbFunc.newISID();
                    TargetName = "2020-05.example.com:target2";
            }
            let! r5 = iSCSI_Initiator.CreateInitialSession sessParam5 m_defaultConnParam
            let! expBlockCount5_2, expBlockSize5_2 = SendReadCapacityCDB r5 ( lun_me.fromPrim 3UL )
            Assert.True(( expBlockCount5_2 * ( uint64 expBlockSize5_2 ) = 16384UL ))
            let! expBlockCount5_1, expBlockSize5_1 = SendReadCapacityCDB r5 ( lun_me.fromPrim 2UL )
            Assert.True(( expBlockCount5_1 * ( uint64 expBlockSize5_1 ) = 32768UL ))

            client.RunCommand "logout" "" "--> "
            client.Kill()

        }