//=============================================================================
// Haruka Software Storage.
// TaskQueue.fs : Test cases for task queue in the LU.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.IT.SCSI

//=============================================================================
// Import declaration

open System
open System.IO

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.Client
open Haruka.Test

//=============================================================================
// Class implementation


[<CollectionDefinition( "SCSI_TaskQueueCases" )>]
type SCSI_TaskQueueCases_Fixture() =

    let m_iSCSIPortNo = GlbFunc.nextTcpPortNo()
    let m_MediaSize = 65536u

    // Add default configurations
    let AddDefaultConf( client : ClientProc ): unit =

        ///////////////////////////////
        // Target Device 0

        // Target device, Target group
        client.RunCommand "create" "Created" "CR> "
        client.RunCommand "select 0" "" "TD> "
        client.RunCommand ( sprintf "set MAXRECVDATASEGMENTLENGTH %d" 4096 ) "" "TD> "
        client.RunCommand ( sprintf "set MAXBURSTLENGTH %d" 16384 ) "" "TD> "
        client.RunCommand ( sprintf "set FIRSTBURSTLENGTH %d" 16384 ) "" "TD> "
        client.RunCommand "create targetgroup" "Created" "TD> "
        client.RunCommand ( sprintf "create networkportal /a ::1 /p %d" m_iSCSIPortNo ) "Created" "TD> "
        client.RunCommand "select 0" "" "TG> "

        // Target, LU
        client.RunCommand "create /n iqn.2020-05.example.com:target1" "Created" "TG> "
        client.RunCommand "select 0" "" "T > "
        client.RunCommand "create /l 1" "Created" "T > "
        client.RunCommand "select 0" "" "LU> "
        client.RunCommand ( sprintf "create membuffer /s %d" m_MediaSize ) "Created" "LU> "

        client.RunCommand "validate" "All configurations are vlidated" "LU> "
        client.RunCommand "publish" "All configurations are uploaded to the controller" "LU> "
        client.RunCommand "start" "Started" "LU> "

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

    interface ICollectionFixture<SCSI_TaskQueueCases_Fixture>

    member _.controllerProc = m_Controller
    member _.clientProc = m_Client
    member _.iSCSIPortNo = m_iSCSIPortNo
    member _.MediaSize = m_MediaSize
    member _.MediaBlockSize = uint Constants.MEDIA_BLOCK_SIZE   // 4096 or 512 bytes


[<Collection( "SCSI_TaskQueueCases" )>]
type SCSI_TaskQueueCases( fx : SCSI_TaskQueueCases_Fixture ) =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let g_CID0 = cid_me.zero
    let g_LUN0 = lun_me.fromPrim 0UL
    let g_LUN1 = lun_me.fromPrim 1UL
    let iSCSIPortNo = fx.iSCSIPortNo
    let m_MediaSize = fx.MediaSize
    let m_MediaBlockSize = fx.MediaBlockSize
    let m_ClientProc = fx.clientProc

    // default session parameters
    let m_defaultSessParam = {
        InitiatorName = "iqn.2020-05.example.com:initiator";
        InitiatorAlias = "aaa";
        TargetName = "iqn.2020-05.example.com:target1";
        TargetAlias = "";
        ISID = isid_me.fromPrim 1UL;
        TSIH = tsih_me.fromPrim 0us;
        MaxConnections = Constants.NEGOPARAM_MaxConnections;
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
        PortNo = iSCSIPortNo;
        CID = g_CID0;
        Initiator_UserName = "";
        Initiator_Password = "";
        Target_UserName = "";
        Target_Password = "";
        HeaderDigest = DigestType.DST_CRC32C;
        DataDigest = DigestType.DST_CRC32C;
        MaxRecvDataSegmentLength_I = 4096u;
        MaxRecvDataSegmentLength_T = 4096u;
    }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.SingleTask_001() =
        task {
            let! r = SCSI_Initiator.Create m_defaultSessParam m_defaultConnParam
            let! itt = r.Send_Inquiry TaskATTRCd.SIMPLE_TASK g_LUN1 EVPD.F 0uy 128us NACA.T
            let! result = r.Wait_Inquiry_Standerd itt
            Assert.True(( result.ProduceIdentification = "HARUKA S.S." ))
            do! r.Close()
        }

