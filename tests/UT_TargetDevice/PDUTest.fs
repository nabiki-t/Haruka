//=============================================================================
// Haruka Software Storage.
// PDUTest.fs : Unit test code for Connection.PDU
// 

namespace Haruka.Test.UT.TargetDevice

open System
open System.IO

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.Test


type PDU_Test () =

    let tsih1o = ValueSome( tsih_me.fromPrim 1us )
    let cid1o = ValueSome( cid_me.fromPrim 1us )
    let cnt1o = ValueSome( concnt_me.fromPrim 1 )

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    [<Fact>]
    member _.SCSICommandPDU_001() =
        use ms = new MemoryStream()

        let pduw =
            {
                I = true;
                F = true;
                R = true;
                W = true;
                ATTR = TaskATTRCd.ACA_TASK;
                LUN = lun_me.fromPrim 1UL;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 2u;
                CmdSN = cmdsn_me.fromPrim 3u;
                ExpStatSN = statsn_me.fromPrim 4u;
                ScsiCDB = Array.zeroCreate(16);
                DataSegment = PooledBuffer.RentAndInit 3;
                BidirectionalExpectedReadDataLength = 15u;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                pduw
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 68u ))

        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        let logPDU =
            PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
            |> Functions.RunTaskSynchronously

        Assert.True( logPDU.Opcode = OpcodeCd.SCSI_COMMAND )
        let pdu = logPDU :?> SCSICommandPDU

        Assert.True( pdu.I )
        Assert.True( pdu.F )
        Assert.True( pdu.R )
        Assert.True( pdu.W )
        Assert.True( ( pdu.ATTR = TaskATTRCd.ACA_TASK ) )
        Assert.True( ( pdu.LUN = lun_me.fromPrim 1UL ) )
        Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 1u ) )
        Assert.True( ( pdu.ExpectedDataTransferLength = 2u ) )
        Assert.True( ( pdu.CmdSN = cmdsn_me.fromPrim 3u ) )
        Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 4u ) )
        Assert.True( ( pdu.ScsiCDB.Length = 16 ) )
        Assert.True( ( Array.zeroCreate<byte>(16) = pdu.ScsiCDB ) )
        Assert.True( ( PooledBuffer.ValueEqualsWithArray pdu.DataSegment ( Array.zeroCreate 3 ) ) )
        Assert.True( ( pdu.BidirectionalExpectedReadDataLength = 15u ) )
        Assert.True( ( pdu.ByteCount = 68u ) )

    [<Fact>]
    member _.SCSICommandPDU_002() =
        use ms = new MemoryStream()

        let buf : byte[] = Array.zeroCreate( 15 )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ =
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? AggregateException as x ->
            match x.InnerExceptions.Item(0) with
            | :? ConnectionErrorException as y ->
                Assert.True( ( Functions.CompareStringHeader y.Message "Connection closed."  ) = 0 ) |> ignore
            | _ ->
            Assert.Fail __LINE__
        | :? ConnectionErrorException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Connection closed."  ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_003() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x01uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength  
                0x01uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x01uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x01uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x01uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x01uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
            |]
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ =
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Invalid TotalAHSLength" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_004() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x08uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy;                         // AHS
            |]
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ =
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? ConnectionErrorException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Connection closed" ) = 0 ) |> ignore
               
    [<Fact>]
    member _.SCSICommandPDU_005() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x10uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x08uy; 0x01uy; 0x00uy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy;                         // Header digest
            |]
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? ConnectionErrorException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Connection closed" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_006() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x10uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x08uy; 0x01uy; 0x00uy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__  
        with
        | :? ConnectionErrorException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Header digest error" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_007() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x03uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Invalid AHSType" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_008() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x06uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "AHSLength(" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_009() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x0Cuy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x01uy; 0x01uy; 0xFFuy; // AHS1
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 60 ( Functions.CRC32 buf.[0..59] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In extended CDB AHS, AHSLength(" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_010() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x04uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Expected Bidirectional Read Data Length AHS, AHSLength" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_011() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x08uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x04uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Data Segment
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Data digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 7u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Data segment length(8) over MaxRecvDataSegmentLength(7). " ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_012() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x08uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x04uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Data Segment
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? ConnectionErrorException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Connection closed" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_013() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x05uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x04uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? ConnectionErrorException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Connection closed" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_014() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x05uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x03uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy;
                0xAAuy; 0x00uy; 0x00uy;         // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? ConnectionErrorException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Connection closed" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_015() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x05uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x02uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy;
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? RejectPDUException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Data digest error" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_016() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x05uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy;
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? DiscardPDUException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Data digest error. Received PDU is discarded." ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_017() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0xFFuy; 0x00uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x05uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy;
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 80 ( Functions.CRC32 buf.[72..79] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? RejectPDUException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Invalid Opcode(0x3F). iSCSI target node" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_018() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x05uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy;
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 80 ( Functions.CRC32 buf.[72..79] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? DiscardPDUException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Invalid Opcode(0x00). iSCSI initiator node" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_019() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x00uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x05uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS2
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy;
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 80 ( Functions.CRC32 buf.[72..79] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Both W and F bit in SCSI" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_020() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x80uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x05uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x01uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS2
                0x00uy; 0x00uy; 0x00uy; 0x01uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy;
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 80 ( Functions.CRC32 buf.[72..79] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Both W and R bit in SCSI" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_021() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0xE5uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x05uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x01uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS2
                0x00uy; 0x00uy; 0x00uy; 0x01uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy;
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 80 ( Functions.CRC32 buf.[72..79] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "Invalid ATTR(0x05)" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_022() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0xE0uy; 0x00uy; 0x00uy; 
                0x0Cuy; 0x00uy; 0x00uy; 0x05uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x05uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy;
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 60 ( Functions.CRC32 buf.[0..59] )
        Functions.UInt32ToNetworkBytes buf 72 ( Functions.CRC32 buf.[64..71] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In bidirectional operation" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_023() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x21uy; 0x00uy; 0x00uy; 
                0x0Cuy; 0x00uy; 0x00uy; 0x05uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x05uy; // Expected Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // CDB
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;  
                0x00uy; 0x09uy; 0x01uy; 0xFFuy; // AHS1
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy;
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 60 ( Functions.CRC32 buf.[0..59] )
        Functions.UInt32ToNetworkBytes buf 72 ( Functions.CRC32 buf.[64..71] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "If there are no following data PDU," ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSICommandPDU_024() =
        use ms = new MemoryStream()
        let buf : byte[] =
            [|
                0x01uy; 0x62uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x05uy; // TotalAHSLength , DataSegmentLength 
                0x06uy; 0x07uy; 0x04uy; 0x05uy; // LUN
                0x02uy; 0x03uy; 0x00uy; 0x01uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Expected Data Transfer Length
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // CmdSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpStatSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // CDB
                0x04uy; 0x05uy; 0x06uy; 0x07uy;
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy;
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy;  
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS1
                0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 
                0x00uy; 0x08uy; 0x01uy; 0xFFuy; // AHS2
                0x10uy; 0x11uy; 0x12uy; 0x13uy; 
                0x14uy; 0x15uy; 0x16uy; 0x17uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0xBBuy; 0xBBuy; 0xBBuy;
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 80 ( Functions.CRC32 buf.[72..79] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore


        let recvPDU_logi = 
            PDU.Receive( 8u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
            |> Functions.RunTaskSynchronously
        Assert.True( recvPDU_logi.Opcode = OpcodeCd.SCSI_COMMAND )

        let pdu = recvPDU_logi :?> SCSICommandPDU

        Assert.True( ( pdu.F = false ) )
        Assert.True( ( pdu.R = true ) )
        Assert.True( ( pdu.W = true ) )
        Assert.True( ( pdu.ATTR = TaskATTRCd.ORDERED_TASK ) )
        Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
        Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ) )
        Assert.True( ( pdu.ExpectedDataTransferLength = 0x00000010u ) )
        Assert.True( ( pdu.CmdSN = cmdsn_me.fromPrim 0x10203040u ) )
        Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 0x0F0E0D0Cu ) )
        Assert.True( ( pdu.ScsiCDB = [| 0x00uy .. 0x16uy |] ) )
        Assert.True( ( PooledBuffer.ValueEqualsWithArray pdu.DataSegment [| 0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; |] ) )
        Assert.True( ( pdu.BidirectionalExpectedReadDataLength = 0xDEADBEEFu ) )
        Assert.True( ( pdu.ByteCount = 84u ) )
            
    [<Fact>]
    member _.SCSICommandPDU_025() =
        use ms = new MemoryStream()
        let pdu =
            {
                I = true;
                F = true;
                R = false;
                W = true;
                ATTR = TaskATTRCd.HEAD_OF_QUEUE_TASK;
                LUN = lun_me.fromPrim 0xF0E1D2C3B4A59687UL;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 2u;
                CmdSN = cmdsn_me.fromPrim 3u;
                ExpStatSN = statsn_me.fromPrim 4u;
                ScsiCDB = Array.zeroCreate(20);
                DataSegment = PooledBuffer.Rent [| 0x00uy .. 0x50uy |];
                BidirectionalExpectedReadDataLength = 0u;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_None,
                DigestType.DST_None,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                pdu
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 140u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi =
                PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.SCSI_COMMAND )

            let pdu = recvPDU_logi :?> SCSICommandPDU

            Assert.True( pdu.I )
            Assert.True( pdu.F )
            Assert.False( pdu.R )
            Assert.True( pdu.W )
            Assert.True( ( pdu.ATTR = TaskATTRCd.HEAD_OF_QUEUE_TASK ) )
            Assert.True( ( pdu.LUN = lun_me.fromPrim 0xF0E1D2C3B4A59687UL ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 1u ) )
            Assert.True( ( pdu.ExpectedDataTransferLength = 2u ) )
            Assert.True( ( pdu.CmdSN = cmdsn_me.fromPrim 3u ) )
            Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 4u ) )
            Assert.True( ( pdu.ScsiCDB = Array.zeroCreate(20) ) )
            Assert.True( ( PooledBuffer.ValueEqualsWithArray pdu.DataSegment [| 0x00uy .. 0x50uy |] ) )
            Assert.True( ( pdu.BidirectionalExpectedReadDataLength = 0u ) )
            Assert.True( ( pdu.ByteCount = 140u ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

            
    [<Fact>]
    member _.SCSICommandPDU_026() =
        use ms = new MemoryStream()
        let pduw =
            {
                I = true;
                F = true;
                R = true;
                W = true;
                ATTR = TaskATTRCd.HEAD_OF_QUEUE_TASK;
                LUN = lun_me.fromPrim 0xF0E1D2C3B4A59687UL;
                InitiatorTaskTag = itt_me.fromPrim 1u;
                ExpectedDataTransferLength = 2u;
                CmdSN = cmdsn_me.fromPrim 3u;
                ExpStatSN = statsn_me.fromPrim 4u;
                ScsiCDB = Array.zeroCreate(20);
                DataSegment = PooledBuffer.Rent [| 0x00uy .. 0x50uy |];
                BidirectionalExpectedReadDataLength = 0u;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_None,
                DigestType.DST_None,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                pduw
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 148u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        let recvPDU_logi = 
            PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
            |> Functions.RunTaskSynchronously
        Assert.True( recvPDU_logi.Opcode = OpcodeCd.SCSI_COMMAND )

        let pdu = recvPDU_logi :?> SCSICommandPDU

        Assert.True( pdu.I )
        Assert.True( pdu.F )
        Assert.True( pdu.R )
        Assert.True( pdu.W )
        Assert.True( ( pdu.ATTR = TaskATTRCd.HEAD_OF_QUEUE_TASK ) )
        Assert.True( ( pdu.LUN = lun_me.fromPrim 0xF0E1D2C3B4A59687UL ) )
        Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 1u ) )
        Assert.True( ( pdu.ExpectedDataTransferLength = 2u ) )
        Assert.True( ( pdu.CmdSN = cmdsn_me.fromPrim 3u ) )
        Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 4u ) )
        Assert.True( ( pdu.ScsiCDB = Array.zeroCreate(20) ) )
        Assert.True( ( PooledBuffer.ValueEqualsWithArray pdu.DataSegment [| 0x00uy .. 0x50uy |] ) )
        Assert.True( ( pdu.BidirectionalExpectedReadDataLength = 0u ) )
        Assert.True( ( pdu.ByteCount = 148u ) )
         
    [<Fact>]
    member _.SCSIResponsePDU_001() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x21uy; 0x80uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x01uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // SNACK tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // ExpDataSN
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy; // Bidirectional Read Residual Count
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy;  // Residual Count
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS1
                0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 
                0x00uy; 0x08uy; 0x01uy; 0xFFuy; // AHS2
                0x10uy; 0x11uy; 0x12uy; 0x13uy; 
                0x14uy; 0x15uy; 0x16uy; 0x17uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 76 ( Functions.CRC32 buf.[72..75] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In SCSI Response PDU, DataSegment" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSIResponsePDU_002() =
        use ms = new MemoryStream()
        let pduw =
            {
                o = true;
                u = true;
                O = true;
                U = true;
                Response = iScsiSvcRespCd.COMMAND_COMPLETE;
                Status = ScsiCmdStatCd.GOOD;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                SNACKTag = snacktag_me.fromPrim 1u;
                StatSN = statsn_me.fromPrim 2u;
                ExpCmdSN = cmdsn_me.fromPrim 3u;
                MaxCmdSN = cmdsn_me.fromPrim 4u;
                ExpDataSN = datasn_me.fromPrim 5u;
                BidirectionalReadResidualCount = 6u;
                ResidualCount = 7u;
                SenseLength = 0us;
                SenseData = ArraySegment.Empty;
                ResponseData = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
                DataInBuffer = PooledBuffer.Empty;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                1u,
                DigestType.DST_CRC32C,
                DigestType.DST_None,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                pduw
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "o and u bit in SCSI response PDU" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSIResponsePDU_003() =
        use ms = new MemoryStream()
        let pdu =
            {
                o = false;
                u = true;
                O = true;
                U = true;
                Response = iScsiSvcRespCd.COMMAND_COMPLETE;
                Status = ScsiCmdStatCd.GOOD;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                SNACKTag = snacktag_me.fromPrim 1u;
                StatSN = statsn_me.fromPrim 2u;
                ExpCmdSN = cmdsn_me.fromPrim 3u;
                MaxCmdSN = cmdsn_me.fromPrim 4u;
                ExpDataSN = datasn_me.fromPrim 5u;
                BidirectionalReadResidualCount = 6u;
                ResidualCount = 7u;
                SenseLength = 0us;
                SenseData = ArraySegment.Empty
                ResponseData = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
                DataInBuffer = PooledBuffer.Empty;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                1u,
                DigestType.DST_CRC32C,
                DigestType.DST_None,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                pdu
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "O and U bit in SCSI response PDU" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSIResponsePDU_004() =
        use ms = new MemoryStream()
        let pduw =
            {
                o = false;
                u = true;
                O = true;
                U = false;
                Response = iScsiSvcRespCd.TARGET_FAILURE;
                Status = ScsiCmdStatCd.GOOD;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                SNACKTag = snacktag_me.fromPrim 1u;
                StatSN = statsn_me.fromPrim 2u;
                ExpCmdSN = cmdsn_me.fromPrim 3u;
                MaxCmdSN = cmdsn_me.fromPrim 4u;
                ExpDataSN = datasn_me.fromPrim 5u;
                BidirectionalReadResidualCount = 6u;
                ResidualCount = 7u;
                SenseLength = 0us;
                SenseData = ArraySegment.Empty;
                ResponseData = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
                DataInBuffer = PooledBuffer.Empty;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                1u,
                DigestType.DST_CRC32C,
                DigestType.DST_None,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                pduw
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In SCSI response PDU, if Response field is not " ) = 0 ) |> ignore
            
    [<Fact>]
    member _.SCSIResponsePDU_005() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x21uy; 0x80uy; 0x00uy; 0x00uy; // Response
                0x14uy; 0x00uy; 0x00uy; 0x04uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // SNACK tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // ExpDataSN
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy; // Bidirectional Read Residual Count
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; // Residual Count
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS1
                0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 
                0x00uy; 0x08uy; 0x01uy; 0xFFuy; // AHS2
                0x10uy; 0x11uy; 0x12uy; 0x13uy; 
                0x14uy; 0x15uy; 0x16uy; 0x17uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment( SenseLength is error )
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 76 ( Functions.CRC32 buf.[72..75] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In SCSI Response PDU, SenseLength(43690) must be" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSIResponsePDU_006() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x21uy; 0x80uy; 0xFFuy; 0x00uy; // Response is error
                0x14uy; 0x00uy; 0x00uy; 0x04uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // SNACK tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // ExpDataSN
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy; // Bidirectional Read Residual Count
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; // Residual Count
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS1
                0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 
                0x00uy; 0x08uy; 0x01uy; 0xFFuy; // AHS2
                0x10uy; 0x11uy; 0x12uy; 0x13uy; 
                0x14uy; 0x15uy; 0x16uy; 0x17uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0x00uy; 0x02uy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 76 ( Functions.CRC32 buf.[72..75] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In SCSI response PDU, Response(0xFF) field " ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSIResponsePDU_007() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x21uy; 0x98uy; 0x00uy; 0xFFuy; // Status is error
                0x14uy; 0x00uy; 0x00uy; 0x04uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // SNACK tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // ExpDataSN
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy; // Bidirectional Read Residual Count
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; // Residual Count
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS1
                0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 
                0x00uy; 0x08uy; 0x01uy; 0xFFuy; // AHS2
                0x10uy; 0x11uy; 0x12uy; 0x13uy; 
                0x14uy; 0x15uy; 0x16uy; 0x17uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0x00uy; 0x02uy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 76 ( Functions.CRC32 buf.[72..75] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In SCSI response PDU, Status(0xFF) field" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSIResponsePDU_008() =
        use ms = new MemoryStream()
        let pdus =
            {
                o = false;
                u = true;
                O = true;
                U = false;
                Response = iScsiSvcRespCd.COMMAND_COMPLETE;
                Status = ScsiCmdStatCd.CHECK_CONDITION;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                SNACKTag = snacktag_me.fromPrim 0x02040608u;
                StatSN = statsn_me.fromPrim 0x0306090Bu;
                ExpCmdSN = cmdsn_me.fromPrim 3u;
                MaxCmdSN = cmdsn_me.fromPrim 0x04080C10u;
                ExpDataSN = datasn_me.fromPrim 0x10111213u;
                BidirectionalReadResidualCount = 0x12131415u;
                ResidualCount = 0x21222324u;
                SenseLength = 5us;
                SenseData = ArraySegment<byte>( [| 0xF0uy; 0xF1uy; 0xF2uy; 0xF3uy; 0xF4uy; |] );
                ResponseData = ArraySegment<byte>( [| 0x00uy .. 0x18uy |], 0, 25 );
                ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
                DataInBuffer = PooledBuffer.Empty;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_None,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                pdus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 84u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_None, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.SCSI_RES )

            let pdu = recvPDU_logi :?> SCSIResponsePDU

            Assert.True( ( pdu.o = false ) )
            Assert.True( ( pdu.u = true ) )
            Assert.True( ( pdu.O = true ) )
            Assert.True( ( pdu.U = false ) )
            Assert.True( ( pdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ) )
            Assert.True( ( pdu.Status = ScsiCmdStatCd.CHECK_CONDITION ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ) )
            Assert.True( ( pdu.SNACKTag = snacktag_me.fromPrim 0x02040608u ) )
            Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0x0306090Bu ) )
            Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x04080C10u ) )
            Assert.True( ( pdu.ExpDataSN = datasn_me.fromPrim 0x10111213u ) )
            Assert.True( ( pdu.BidirectionalReadResidualCount = 0x12131415u ) )
            Assert.True( ( pdu.ResidualCount = 0x21222324u ) )
            Assert.True( ( pdu.SenseLength = 5us ) )
            let arSenseData = pdu.SenseData
            let arResponseData = pdu.ResponseData
            Assert.True( ( arSenseData.ToArray() = [| 0xF0uy; 0xF1uy; 0xF2uy; 0xF3uy; 0xF4uy; |] ) )
            for i = 0 to arResponseData.Count - 1 do
                Assert.True( ( arResponseData.Item(i) = byte i ) )
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In SCSI response PDU, Status(0xFF) field" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSIResponsePDU_009() =
        use ms = new MemoryStream()
        let pdus =
            {
                o = true;
                u = false;
                O = false;
                U = true;
                Response = iScsiSvcRespCd.COMMAND_COMPLETE;
                Status = ScsiCmdStatCd.CHECK_CONDITION;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                SNACKTag = snacktag_me.fromPrim 0x02040608u;
                StatSN = statsn_me.fromPrim 0x0306090Bu;
                ExpCmdSN = cmdsn_me.fromPrim 3u;
                MaxCmdSN = cmdsn_me.fromPrim 0x04080C10u;
                ExpDataSN = datasn_me.fromPrim 0x10111213u;
                BidirectionalReadResidualCount = 0x12131415u;
                ResidualCount = 0x21222324u;
                SenseLength = 9us;
                SenseData = ArraySegment<byte>( [| 0uy; 1uy; 2uy; 3uy; 4uy; 4uy; 4uy; 4uy; 4uy; |] );
                ResponseData = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
                DataInBuffer = PooledBuffer.Empty;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_None,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                pdus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 64u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.SCSI_RES )

            let pdu = recvPDU_logi :?> SCSIResponsePDU

            Assert.True( ( pdu.o = true ) )
            Assert.True( ( pdu.u = false ) )
            Assert.True( ( pdu.O = false ) )
            Assert.True( ( pdu.U = true ) )
            Assert.True( ( pdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ) )
            Assert.True( ( pdu.Status = ScsiCmdStatCd.CHECK_CONDITION ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ) )
            Assert.True( ( pdu.SNACKTag = snacktag_me.fromPrim 0x02040608u ) )
            Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0x0306090Bu ) )
            Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x04080C10u ) )
            Assert.True( ( pdu.ExpDataSN = datasn_me.fromPrim 0x10111213u ) )
            Assert.True( ( pdu.BidirectionalReadResidualCount = 0x12131415u ) )
            Assert.True( ( pdu.ResidualCount = 0x21222324u ) )
            Assert.True( ( pdu.SenseLength = 9us ) )
            let arSenseData = pdu.SenseData
            let arResponseData = pdu.ResponseData
            Assert.True( ( arSenseData.ToArray() = [| 0uy; 1uy; 2uy; 3uy; 4uy; 4uy; 4uy; 4uy; 4uy; |] ) )
            Assert.True( ( arResponseData.ToArray() = Array.empty ) )
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In SCSI response PDU, Status(0xFF) field" ) = 0 ) |> ignore

    [<Fact>]
    member _.SCSIResponsePDU_010() =
        use ms = new MemoryStream()
        let pduw =
            {
                o = true;
                u = false;
                O = false;
                U = true;
                Response = iScsiSvcRespCd.COMMAND_COMPLETE;
                Status = ScsiCmdStatCd.CHECK_CONDITION;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                SNACKTag = snacktag_me.fromPrim 0x02040608u;
                StatSN = statsn_me.fromPrim 0x0306090Bu;
                ExpCmdSN = cmdsn_me.fromPrim 3u;
                MaxCmdSN = cmdsn_me.fromPrim 0x04080C10u;
                ExpDataSN = datasn_me.fromPrim 0x10111213u;
                BidirectionalReadResidualCount = 0x12131415u;
                ResidualCount = 0x21222324u;
                SenseLength = 0us;
                SenseData = ArraySegment.Empty;
                ResponseData = ArraySegment( [| 0uy; 1uy; 2uy; 3uy; 4uy; 4uy; 4uy; 4uy; 4uy; |], 0, 9 );
                ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
                DataInBuffer = PooledBuffer.Empty;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                pduw
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 68u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        let recvPDU_logi = 
            try
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            with
            | :? SessionRecoveryException as x ->
                Assert.True( ( Functions.CompareStringHeader x.Message "In SCSI response PDU, Status(0xFF) field" ) = 0 )
                reraise()
            | _ as x ->
                Assert.Fail __LINE__
                reraise()

        Assert.True( recvPDU_logi.Opcode = OpcodeCd.SCSI_RES )

        let pdu = recvPDU_logi :?> SCSIResponsePDU

        Assert.True( ( pdu.o = true ) )
        Assert.True( ( pdu.u = false ) )
        Assert.True( ( pdu.O = false ) )
        Assert.True( ( pdu.U = true ) )
        Assert.True( ( pdu.Response = iScsiSvcRespCd.COMMAND_COMPLETE ) )
        Assert.True( ( pdu.Status = ScsiCmdStatCd.CHECK_CONDITION ) )
        Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ) )
        Assert.True( ( pdu.SNACKTag = snacktag_me.fromPrim 0x02040608u ) )
        Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0x0306090Bu ) )
        Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x04080C10u ) )
        Assert.True( ( pdu.ExpDataSN = datasn_me.fromPrim 0x10111213u ) )
        Assert.True( ( pdu.BidirectionalReadResidualCount = 0x12131415u ) )
        Assert.True( ( pdu.ResidualCount = 0x21222324u ) )
        Assert.True( ( pdu.SenseLength = 0us ) )
        let arSenseData = pdu.SenseData
        let arResponseData = pdu.ResponseData
        Assert.True( ( arSenseData.Count = 0 ) )
        for i = 0 to arResponseData.Count - 1 do
            Assert.True( ( arResponseData.Item(i) = [| 0uy; 1uy; 2uy; 3uy; 4uy; 4uy; 4uy; 4uy; 4uy; |].[i] ) )

    [<Fact>]
    member _.TaskManagementFunctionRequestPDU_001() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x02uy; 0x81uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x01uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Referenced Task Tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // CmdSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpStatSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // RefCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // ExpDataSN
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy; // Reserved
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; 
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS1
                0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 
                0x00uy; 0x08uy; 0x01uy; 0xFFuy; // AHS2
                0x10uy; 0x11uy; 0x12uy; 0x13uy; 
                0x14uy; 0x15uy; 0x16uy; 0x17uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 76 ( Functions.CRC32 buf.[72..75] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Task management function request PDU, TotalAHSLength" ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TaskManagementFunctionRequestPDU_002() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x02uy; 0x81uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x01uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Referenced Task Tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // CmdSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpStatSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // RefCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // ExpDataSN
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy; // Reserved
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        Functions.UInt32ToNetworkBytes buf 56 ( Functions.CRC32 buf.[52..55] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Task management function request PDU, DataSegmentLength must" ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TaskManagementFunctionRequestPDU_003() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x02uy; 0x80uy; 0x00uy; 0x00uy; // Function is error
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Referenced Task Tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // CmdSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpStatSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // RefCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // ExpDataSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            |]
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Task management function request PDU, Function(" ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TaskManagementFunctionRequestPDU_004() =
        use ms = new MemoryStream()
        let psus =
            {
                I = true;
                Function = TaskMgrReqCd.CLEAR_ACA;
                LUN = lun_me.fromPrim 0x030405060708090AUL;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                ReferencedTaskTag = itt_me.fromPrim 0x02040608u;
                CmdSN = cmdsn_me.fromPrim 0xDEADBEEFu;
                ExpStatSN = statsn_me.fromPrim 0xFEEEFEEEu;
                RefCmdSN = cmdsn_me.fromPrim 0x11111111u;
                ExpDataSN = datasn_me.fromPrim 0x22222222u;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_None,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_None, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Task management function request PDU, If Function field is not" ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TaskManagementFunctionRequestPDU_005() =
        use ms = new MemoryStream()
        let psus =
            {
                I = true;
                Function = TaskMgrReqCd.CLEAR_TASK_SET;
                LUN = lun_me.fromPrim 0x030405060708090AUL;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                ReferencedTaskTag = itt_me.fromPrim 0xFFFFFFFFu;
                CmdSN = cmdsn_me.fromPrim 0xDEADBEEFu;
                ExpStatSN = statsn_me.fromPrim 0xFEEEFEEEu;
                RefCmdSN = cmdsn_me.fromPrim 0x11111111u;
                ExpDataSN = datasn_me.fromPrim 0x22222222u;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_None,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 48u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.SCSI_TASK_MGR_REQ )

            let pdu = recvPDU_logi :?> TaskManagementFunctionRequestPDU

            Assert.True( ( pdu.I = true ) )
            Assert.True( ( pdu.Function = TaskMgrReqCd.CLEAR_TASK_SET ) )
            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x030405060708090AUL ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ) )
            Assert.True( ( pdu.ReferencedTaskTag = itt_me.fromPrim 0xFFFFFFFFu ) )
            Assert.True( ( pdu.CmdSN = cmdsn_me.fromPrim 0xDEADBEEFu ) )
            Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.RefCmdSN = cmdsn_me.fromPrim 0x11111111u ) )
            Assert.True( ( pdu.ExpDataSN = datasn_me.fromPrim 0x22222222u ) )
            Assert.True( ( pdu.ByteCount = 48u ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TaskManagementFunctionResponsePDU_001() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x22uy; 0x81uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x01uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Reserved
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // Reserved
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy;
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; 
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS1
                0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 
                0x00uy; 0x08uy; 0x01uy; 0xFFuy; // AHS2
                0x10uy; 0x11uy; 0x12uy; 0x13uy; 
                0x14uy; 0x15uy; 0x16uy; 0x17uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 76 ( Functions.CRC32 buf.[72..75] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Task management function response PDU, TotalAHSLength" ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TaskManagementFunctionResponsePDU_002() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x22uy; 0x81uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x01uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Reserved
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // Reserved
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy;
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        Functions.UInt32ToNetworkBytes buf 56 ( Functions.CRC32 buf.[52..55] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Task management function response PDU, DataSegmentLength" ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TaskManagementFunctionResponsePDU_003() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x22uy; 0x80uy; 0x0Auy; 0x00uy; // Response is error
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Reserved
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // Reserved
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy;
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Task management function response PDU, Response(0x" ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TaskManagementFunctionResponsePDU_004() =
        use ms = new MemoryStream()
        let psus =
            {
                Response = TaskMgrResCd.FUCTION_COMPLETE;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                StatSN = statsn_me.fromPrim 0xDEADBEEFu;
                ExpCmdSN = cmdsn_me.fromPrim 0xFEEEFEEEu;
                MaxCmdSN = cmdsn_me.fromPrim 0x11111111u;
                ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.SCSI_TASK_MGR_RES )

            let pdu = recvPDU_logi :?> TaskManagementFunctionResponsePDU

            Assert.True( ( pdu.Response = TaskMgrResCd.FUCTION_COMPLETE ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0x01020304u ) )
            Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0xDEADBEEFu ) )
            Assert.True( ( pdu.ExpCmdSN = cmdsn_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x11111111u ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SCSIDataOutPDU_001() =
        use ms = new MemoryStream()
        let psus =
            {
                F = true;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                TargetTransferTag = ttt_me.fromPrim 0xDEADBEEFu;
                ExpStatSN = statsn_me.fromPrim 0xFEEEFEEEu;
                DataSN = datasn_me.fromPrim 0x11111111u;
                BufferOffset = 0x22222222u;
                DataSegment = PooledBuffer.Rent [| 0x00uy .. 0xFEuy |];
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                255u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 255u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.SCSI_DATA_OUT )

            let pdu = recvPDU_logi :?> SCSIDataOutPDU

            Assert.True( ( pdu.F = true ) )
            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
            Assert.True( ( pdu.TargetTransferTag = ttt_me.fromPrim 0xDEADBEEFu ) )
            Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.DataSN = datasn_me.fromPrim 0x11111111u ) )
            Assert.True( ( pdu.BufferOffset = 0x22222222u ) )
            Assert.True( ( PooledBuffer.ValueEqualsWithArray pdu.DataSegment [| 0x00uy .. 0xFEuy |] ) )
            Assert.True( ( pdu.ByteCount = 312u ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SCSIDataOutPDU_002() =
        use ms = new MemoryStream()
        let psus =
            {
                F = false;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                TargetTransferTag = ttt_me.fromPrim 0xDEADBEEFu;
                ExpStatSN = statsn_me.fromPrim 0xFEEEFEEEu;
                DataSN = datasn_me.fromPrim 0x11111111u;
                BufferOffset = 0x22222222u;
                DataSegment = PooledBuffer.Empty;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                255u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 255u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.SCSI_DATA_OUT )

            let pdu = recvPDU_logi :?> SCSIDataOutPDU

            Assert.True( ( pdu.F = false ) )
            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
            Assert.True( ( pdu.TargetTransferTag = ttt_me.fromPrim 0xDEADBEEFu ) )
            Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.DataSN = datasn_me.fromPrim 0x11111111u ) )
            Assert.True( ( pdu.BufferOffset = 0x22222222u ) )
            Assert.True( ( PooledBuffer.length pdu.DataSegment = 0 ) )
            Assert.True( ( pdu.ByteCount = 52u ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SCSIDataInPDU_001() =
        use ms = new MemoryStream()
        let psus =
            {
                F = false;
                A = true;
                O = true;
                U = true;
                S = true;
                Status = ScsiCmdStatCd.GOOD;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                TargetTransferTag = ttt_me.fromPrim 0xDEADBEEFu;
                StatSN = statsn_me.fromPrim 0xFEEEFEEEu;
                ExpCmdSN = cmdsn_me.fromPrim 0x11111111u;
                MaxCmdSN = cmdsn_me.fromPrim 0x22222222u;
                DataSN = datasn_me.fromPrim 0x33333333u;
                BufferOffset = 0x44444444u;
                ResidualCount = 0x55555555u;
                DataSegment = ArraySegment( [| 0x00uy .. 0xFEuy |], 0, 255 );
                ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In SCSI Data-In PDU, if S bit set to 1, F bit must be 1." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__
            
    [<Fact>]
    member _.SCSIDataInPDU_002() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x25uy; 0xC7uy; 0x00uy; 0xBBuy; // Status is error
                0x00uy; 0x00uy; 0x00uy; 0x04uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0xEFuy; 0xBEuy; 0xADuy; 0xDEuy; // TargetTransferTag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // DataSN
                0x44uy; 0x44uy; 0x44uy; 0x44uy; // BufferOffset
                0x55uy; 0x55uy; 0x55uy; 0x55uy; // ResidualCount
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0x00uy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        Functions.UInt32ToNetworkBytes buf 56 ( Functions.CRC32 buf.[52..55] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In SCSI Data-In PDU, Status(0xBB" ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SCSIDataInPDU_003() =
        use ms = new MemoryStream()
        let psus =
            {
                F = true;
                A = true;
                O = true;
                U = true;
                S = true;
                Status = ScsiCmdStatCd.GOOD;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                TargetTransferTag = ttt_me.fromPrim 0xDEADBEEFu;
                StatSN = statsn_me.fromPrim 0xFEEEFEEEu;
                ExpCmdSN = cmdsn_me.fromPrim 0x11111111u;
                MaxCmdSN = cmdsn_me.fromPrim 0x22222222u;
                DataSN = datasn_me.fromPrim 0x33333333u;
                BufferOffset = 0x44444444u;
                ResidualCount = 0x55555555u;
                DataSegment = ArraySegment( [| 0x00uy .. 0xFEuy |], 0, 255 );
                ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.SCSI_DATA_IN )

            let pdu = recvPDU_logi :?> SCSIDataInPDU

            Assert.True( ( pdu.F = true ) )
            Assert.True( ( pdu.A = true ) )
            Assert.True( ( pdu.O = true ) )
            Assert.True( ( pdu.U = true ) )
            Assert.True( ( pdu.S = true ) )
            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
            Assert.True( ( pdu.TargetTransferTag = ttt_me.fromPrim 0xDEADBEEFu ) )
            Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.ExpCmdSN = cmdsn_me.fromPrim 0x11111111u ) )
            Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x22222222u ) )
            Assert.True( ( pdu.DataSN = datasn_me.fromPrim 0x33333333u ) )
            Assert.True( ( pdu.BufferOffset = 0x44444444u ) )
            Assert.True( ( pdu.ResidualCount = 0x55555555u ) )
            let arDataSegment = pdu.DataSegment
            Assert.True( ( arDataSegment.ToArray() = [| 0x00uy .. 0xFEuy |] ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.SCSIDataInPDU_004() =
        use ms = new MemoryStream()
        let psus =
            {
                F = false;
                A = true;
                O = false;
                U = true;
                S = false;
                Status = ScsiCmdStatCd.GOOD;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                TargetTransferTag = ttt_me.fromPrim 0xDEADBEEFu;
                StatSN = statsn_me.fromPrim 0xFEEEFEEEu;
                ExpCmdSN = cmdsn_me.fromPrim 0x11111111u;
                MaxCmdSN = cmdsn_me.fromPrim 0x22222222u;
                DataSN = datasn_me.fromPrim 0x33333333u;
                BufferOffset = 0x44444444u;
                ResidualCount = 0x55555555u;
                DataSegment = ArraySegment( [| 0x00uy .. 0xFEuy |], 0, 255 );
                ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.SCSI_DATA_IN )

            let pdu = recvPDU_logi :?> SCSIDataInPDU

            Assert.True( ( pdu.F = false ) )
            Assert.True( ( pdu.A = true ) )
            Assert.True( ( pdu.O = false ) )
            Assert.True( ( pdu.U = true ) )
            Assert.True( ( pdu.S = false ) )
            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
            Assert.True( ( pdu.TargetTransferTag = ttt_me.fromPrim 0xDEADBEEFu ) )
            Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.ExpCmdSN = cmdsn_me.fromPrim 0x11111111u ) )
            Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x22222222u ) )
            Assert.True( ( pdu.DataSN = datasn_me.fromPrim 0x33333333u ) )
            Assert.True( ( pdu.BufferOffset = 0x44444444u ) )
            Assert.True( ( pdu.ResidualCount = 0x55555555u ) )
            let arDataSegment = pdu.DataSegment
            Assert.True( ( arDataSegment.ToArray() = [| 0x00uy .. 0xFEuy |] ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.R2TPDU_001() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x31uy; 0x81uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x01uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Target Transfer Tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // R2TSN
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy; // Buffer Offset
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; // Desired Data Transfer Length
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS1
                0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 
                0x00uy; 0x08uy; 0x01uy; 0xFFuy; // AHS2
                0x10uy; 0x11uy; 0x12uy; 0x13uy; 
                0x14uy; 0x15uy; 0x16uy; 0x17uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 76 ( Functions.CRC32 buf.[72..75] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In R2T PDU, TotalAHSLength must be 0" ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.R2TPDU_002() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x31uy; 0x81uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x01uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Target Transfer Tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // R2TSN
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy; // Buffer Offset
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; // Desired Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        Functions.UInt32ToNetworkBytes buf 56 ( Functions.CRC32 buf.[52..55] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In R2T PDU, DataSegmentLength must be 0" ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.R2TPDU_003() =
        use ms = new MemoryStream()
        let psus =
            {
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                TargetTransferTag = ttt_me.fromPrim 0xDEADBEEFu;
                StatSN = statsn_me.fromPrim 0xFEEEFEEEu;
                ExpCmdSN = cmdsn_me.fromPrim 0x11111111u;
                MaxCmdSN = cmdsn_me.fromPrim 0x22222222u;
                R2TSN = datasn_me.fromPrim 0x33333333u;
                BufferOffset = 0x44444444u;
                DesiredDataTransferLength = 0x00000000u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In R2T PDU, DesiredDataTransferLength must not be 0" ) = 0 ) |> ignore

    [<Fact>]
    member _.R2TPDU_004() =
        use ms = new MemoryStream()
        let psus =
            {
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                StatSN = statsn_me.fromPrim 0xFEEEFEEEu;
                ExpCmdSN = cmdsn_me.fromPrim 0x11111111u;
                MaxCmdSN = cmdsn_me.fromPrim 0x22222222u;
                R2TSN = datasn_me.fromPrim 0x33333333u;
                BufferOffset = 0x44444444u;
                DesiredDataTransferLength = 0x55555555u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In R2T PDU, TargetTransferTag must not be 0xFFFFFFFF." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.R2TPDU_005() =
        use ms = new MemoryStream()
        let psus =
            {
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                TargetTransferTag = ttt_me.fromPrim 0xDEADBEEFu;
                StatSN = statsn_me.fromPrim 0xFEEEFEEEu;
                ExpCmdSN = cmdsn_me.fromPrim 0x11111111u;
                MaxCmdSN = cmdsn_me.fromPrim 0x22222222u;
                R2TSN = datasn_me.fromPrim 0x33333333u;
                BufferOffset = 0x44444444u;
                DesiredDataTransferLength = 0x55555555u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.R2T )

            let pdu = recvPDU_logi :?> R2TPDU

            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
            Assert.True( ( pdu.TargetTransferTag = ttt_me.fromPrim 0xDEADBEEFu ) )
            Assert.True( ( pdu.StatSN =  statsn_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.ExpCmdSN = cmdsn_me.fromPrim 0x11111111u ) )
            Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x22222222u ) )
            Assert.True( ( pdu.R2TSN = datasn_me.fromPrim 0x33333333u ) )
            Assert.True( ( pdu.BufferOffset = 0x44444444u ) )
            Assert.True( ( pdu.DesiredDataTransferLength = 0x55555555u ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.AsyncronousMessagePDU_001() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x32uy; 0x80uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
                0x00uy; 0x00uy; 0x00uy; 0x10uy;
                0xFEuy; 0xEEuy; 0xFEuy; 0xEEuy; // StatSN
                0x11uy; 0x11uy; 0x11uy; 0x11uy; // ExpCmdSN
                0x22uy; 0x22uy; 0x22uy; 0x22uy; // MaxCmdSN
                0x05uy; 0x00uy; 0x11uy; 0x11uy; // AsyncEvent, AsyncVCode, Parameter1
                0x22uy; 0x22uy; 0x33uy; 0x33uy; // Parameter2, Parameter3
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True(( x.Message.StartsWith "In Asyncronous message PDU, AsyncEvent(0x05)" ))
            
    [<Fact>]
    member _.AsyncronousMessagePDU_002() =
        use ms = new MemoryStream()
        let psus =
            {
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                StatSN = statsn_me.fromPrim 0xFEEEFEEEu;
                ExpCmdSN = cmdsn_me.fromPrim 0x11111111u;
                MaxCmdSN = cmdsn_me.fromPrim 0x22222222u;
                AsyncEvent = AsyncEventCd.SENCE_DATA;
                AsyncVCode = 0uy;
                Parameter1 = 0x1111us;
                Parameter2 = 0x2222us;
                Parameter3 = 0x3333us;
                SenseLength = 0us;
                SenseData = Array.empty;
                ISCSIEventData = [| 0x00uy .. 0xFFuy |];
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 316u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

//        try
        let recvPDU_logi = 
            PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
            |> Functions.RunTaskSynchronously
        Assert.True( recvPDU_logi.Opcode = OpcodeCd.ASYNC )

        let pdu = recvPDU_logi :?> AsyncronousMessagePDU

        Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
        Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0xFEEEFEEEu ) )
        Assert.True( ( pdu.ExpCmdSN = cmdsn_me.fromPrim 0x11111111u ) )
        Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x22222222u ) )
        Assert.True( ( pdu.AsyncEvent = AsyncEventCd.SENCE_DATA ) )
        Assert.True( ( pdu.AsyncVCode = 0uy ) )
        Assert.True( ( pdu.Parameter1 = 0x1111us ) )
        Assert.True( ( pdu.Parameter2 = 0x2222us ) )
        Assert.True( ( pdu.Parameter3 = 0x3333us ) )
        Assert.True( ( pdu.SenseLength = 0us ) )
        Assert.True( ( pdu.SenseData = Array.empty ) )
        Assert.True( ( pdu.ISCSIEventData = [| 0x00uy .. 0xFFuy |] ) )
//        with
//        | _ as x ->
//            Assert.Fail __LINE__
            
    [<Fact>]
    member _.AsyncronousMessagePDU_003() =
        use ms = new MemoryStream()
        let psus =
            {
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                StatSN = statsn_me.fromPrim 0xFEEEFEEEu;
                ExpCmdSN = cmdsn_me.fromPrim 0x11111111u;
                MaxCmdSN = cmdsn_me.fromPrim 0x22222222u;
                AsyncEvent = AsyncEventCd.SENCE_DATA;
                AsyncVCode = 0uy;
                Parameter1 = 0x1111us;
                Parameter2 = 0x2222us;
                Parameter3 = 0x3333us;
                SenseLength = 254us;
                SenseData = [| 0x00uy .. 0xFDuy |];
                ISCSIEventData = [| 0x00uy .. 0xFEuy |];
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 568u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.ASYNC )

            let pdu = recvPDU_logi :?> AsyncronousMessagePDU

            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
            Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.ExpCmdSN = cmdsn_me.fromPrim 0x11111111u ) )
            Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x22222222u ) )
            Assert.True( ( pdu.AsyncEvent = AsyncEventCd.SENCE_DATA ) )
            Assert.True( ( pdu.AsyncVCode = 0uy ) )
            Assert.True( ( pdu.Parameter1 = 0x1111us ) )
            Assert.True( ( pdu.Parameter2 = 0x2222us ) )
            Assert.True( ( pdu.Parameter3 = 0x3333us ) )
            Assert.True( ( pdu.SenseLength = 254us ) )
            Assert.True( ( pdu.SenseData = [| 0x00uy .. 0xFDuy |] ) )
            Assert.True( ( pdu.ISCSIEventData = [| 0x00uy .. 0xFEuy |] ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TextRequestPDU_001() =
        use ms = new MemoryStream()
        let psus =
            {
                I = true;
                F = true;
                C = true;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                TargetTransferTag = ttt_me.fromPrim 0x11111111u;
                CmdSN = cmdsn_me.fromPrim 0x22222222u;
                ExpStatSN = statsn_me.fromPrim 0x33333333u;
                TextRequest = Array.empty;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Text request PDU, if C bit set to 1, F bit must be 0." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TextRequestPDU_002() =
        use ms = new MemoryStream()
        let psus =
            {
                I = true;
                F = false;
                C = true;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                TargetTransferTag = ttt_me.fromPrim 0x11111111u;
                CmdSN = cmdsn_me.fromPrim 0x22222222u;
                ExpStatSN = statsn_me.fromPrim 0x33333333u;
                TextRequest = Array.empty;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.TEXT_REQ )

            let pdu = recvPDU_logi :?> TextRequestPDU

            Assert.True( ( pdu.I = true ) )
            Assert.True( ( pdu.F = false ) )
            Assert.True( ( pdu.C = true ) )
            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.TargetTransferTag = ttt_me.fromPrim 0x11111111u ) )
            Assert.True( ( pdu.CmdSN = cmdsn_me.fromPrim 0x22222222u ) )
            Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 0x33333333u ) )
            Assert.True( ( pdu.TextRequest = Array.empty ) )
            Assert.True( ( pdu.ByteCount = 52u ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TextRequestPDU_003() =
        use ms = new MemoryStream()
        let psus =
            {
                I = false;
                F = true;
                C = false;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                TargetTransferTag = ttt_me.fromPrim 0x11111111u;
                CmdSN = cmdsn_me.fromPrim 0x22222222u;
                ExpStatSN = statsn_me.fromPrim 0x33333333u;
                TextRequest = [| 0x00uy .. 0xF1uy |];
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 300u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.TEXT_REQ )

            let pdu = recvPDU_logi :?> TextRequestPDU

            Assert.True( ( pdu.I = false ) )
            Assert.True( ( pdu.F = true ) )
            Assert.True( ( pdu.C = false ) )
            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.TargetTransferTag = ttt_me.fromPrim 0x11111111u ) )
            Assert.True( ( pdu.CmdSN = cmdsn_me.fromPrim 0x22222222u ) )
            Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 0x33333333u ) )
            Assert.True( ( pdu.TextRequest = [| 0x00uy .. 0xF1uy |] ) )
            Assert.True( ( pdu.ByteCount = 300u ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TextResponsePDU_001() =
        use ms = new MemoryStream()
        let psus =
            {
                F = true;
                C = true;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                TargetTransferTag = ttt_me.fromPrim 0x11111111u;
                StatSN = statsn_me.fromPrim 0x22222222u;
                ExpCmdSN = cmdsn_me.fromPrim 0x33333333u;
                MaxCmdSN = cmdsn_me.fromPrim 0x33333333u;
                TextResponse = Array.empty
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Text response PDU, if C bit set to 1, F bit must be 0." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TextResponsePDU_002() =
        use ms = new MemoryStream()
        let psus =
            {
                F = false;
                C = true;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                TargetTransferTag = ttt_me.fromPrim 0x11111111u;
                StatSN = statsn_me.fromPrim 0x22222222u;
                ExpCmdSN = cmdsn_me.fromPrim 0x33333333u;
                MaxCmdSN = cmdsn_me.fromPrim 0x44444444u;
                TextResponse = Array.empty
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.TEXT_RES )

            let pdu = recvPDU_logi :?> TextResponsePDU

            Assert.True( ( pdu.F = false ) )
            Assert.True( ( pdu.C = true ) )
            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.TargetTransferTag = ttt_me.fromPrim 0x11111111u ) )
            Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0x22222222u ) )
            Assert.True( ( pdu.ExpCmdSN = cmdsn_me.fromPrim 0x33333333u ) )
            Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x44444444u ) )
            Assert.True( ( pdu.TextResponse = Array.empty ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TextResponsePDU_003() =
        use ms = new MemoryStream()
        let psus =
            {
                F = true;
                C = false;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                TargetTransferTag = ttt_me.fromPrim 0x11111111u;
                StatSN = statsn_me.fromPrim 0x22222222u;
                ExpCmdSN = cmdsn_me.fromPrim 0x33333333u;
                MaxCmdSN = cmdsn_me.fromPrim 0x44444444u;
                TextResponse = [| 0x00uy .. 0xFFuy |]
            }
        let sendBytesCnt =
            PDU.SendPDU(
                256u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 256u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.TEXT_RES )

            let pdu = recvPDU_logi :?> TextResponsePDU

            Assert.True( ( pdu.F = true ) )
            Assert.True( ( pdu.C = false ) )
            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.TargetTransferTag = ttt_me.fromPrim 0x11111111u ) )
            Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0x22222222u ) )
            Assert.True( ( pdu.ExpCmdSN = cmdsn_me.fromPrim 0x33333333u ) )
            Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x44444444u ) )
            Assert.True( ( pdu.TextResponse = [| 0x00uy .. 0xFFuy |] ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginRequestPDU_001() =
        use ms = new MemoryStream()
        let psus =
            {
                T = true;
                C = true;
                CSG = LoginReqStateCd.SEQURITY;
                NSG = LoginReqStateCd.SEQURITY;
                VersionMax = 0x00uy;
                VersionMin = 0x00uy;
                ISID = isid_me.fromElem 0xC0uy 0x00uy 0x00us 0x00uy 0x00us;
                TSIH = tsih_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                CID = cid_me.fromPrim 0x1111us;
                CmdSN = cmdsn_me.fromPrim 0x22222222u;
                ExpStatSN = statsn_me.fromPrim 0x33333333u;
                TextRequest = Array.empty;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login request PDU, if C bit set to 1, T bit must be 0." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginRequestPDU_002() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x43uy; 0x48uy; 0x00uy; 0x00uy; // CSG is error
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ISID, TSIH
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0xFEuy; 0xEEuy; 0xFEuy; 0xEEuy; // Initiator task tag
                0x11uy; 0x11uy; 0x00uy; 0x00uy; // CID
                0x22uy; 0x22uy; 0x22uy; 0x22uy; // CmdSN
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_None, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login request PDU, CSG(0x02) field value is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginRequestPDU_003() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x43uy; 0xC2uy; 0x00uy; 0x00uy; // NSG is error
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // ISID, TSIH
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0xFEuy; 0xEEuy; 0xFEuy; 0xEEuy; // Initiator task tag
                0x11uy; 0x11uy; 0x00uy; 0x00uy; // CID
                0x22uy; 0x22uy; 0x22uy; 0x22uy; // CmdSN
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login request PDU, NSG(0x02) field value is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginRequestPDU_004() =
        use ms = new MemoryStream()
        let psus =
            {
                T = true;
                C = false;
                CSG = LoginReqStateCd.FULL;
                NSG = LoginReqStateCd.OPERATIONAL;
                VersionMax = 0x00uy;
                VersionMin = 0x00uy;
                ISID = isid_me.fromElem 0xC0uy 0x00uy 0x00us 0x00uy 0x00us;
                TSIH = tsih_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                CID = cid_me.fromPrim 0x1111us;
                CmdSN = cmdsn_me.fromPrim 0x22222222u;
                ExpStatSN = statsn_me.fromPrim 0x33333333u;
                TextRequest = Array.empty;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login request PDU, CSG(0x03) field value is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginRequestPDU_005() =
        use ms = new MemoryStream()
        let psus =
            {
                T = true;
                C = false;
                CSG = LoginReqStateCd.SEQURITY;
                NSG = LoginReqStateCd.SEQURITY;
                VersionMax = 0x00uy;
                VersionMin = 0x00uy;
                ISID = isid_me.fromElem 0xC0uy 0x00uy 0x00us 0x00uy 0x00us;
                TSIH = tsih_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                CID = cid_me.fromPrim 0x1111us;
                CmdSN = cmdsn_me.fromPrim 0x22222222u;
                ExpStatSN = statsn_me.fromPrim 0x33333333u;
                TextRequest = Array.empty;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login request PDU, CSG(0x00) and NSG(0x00) fields value combination is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginRequestPDU_006() =
        use ms = new MemoryStream()
        let psus =
            {
                T = true;
                C = false;
                CSG = LoginReqStateCd.OPERATIONAL;
                NSG = LoginReqStateCd.OPERATIONAL;
                VersionMax = 0x00uy;
                VersionMin = 0x00uy;
                ISID = isid_me.fromElem 0xC0uy 0x00uy 0x00us 0x00uy 0x00us;
                TSIH = tsih_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                CID = cid_me.fromPrim 0x1111us;
                CmdSN = cmdsn_me.fromPrim 0x22222222u;
                ExpStatSN = statsn_me.fromPrim 0x33333333u;
                TextRequest = Array.empty;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login request PDU, CSG(0x01) and NSG(0x01) fields value combination is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginRequestPDU_007() =
        use ms = new MemoryStream()
        let psus =
            {
                T = false;
                C = true;
                CSG = LoginReqStateCd.SEQURITY;
                NSG = LoginReqStateCd.SEQURITY;
                VersionMax = 0x00uy;
                VersionMin = 0x00uy;
                ISID = isid_me.fromElem 0xC0uy 0x00uy 0x00us 0x00uy 0x00us;
                TSIH = tsih_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                CID = cid_me.fromPrim 0x1111us;
                CmdSN = cmdsn_me.fromPrim 0x22222222u;
                ExpStatSN = statsn_me.fromPrim 0x33333333u;
                TextRequest = Array.empty;
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 52u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login request PDU, T(0xC0) field in ISID value is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginRequestPDU_008() =
        use ms = new MemoryStream()
        let psus =
            {
                T = false;
                C = true;
                CSG = LoginReqStateCd.SEQURITY;
                NSG = LoginReqStateCd.SEQURITY;
                VersionMax = 0x11uy;
                VersionMin = 0x22uy;
                ISID = isid_me.fromElem 0x00uy 0x11uy 0x2222us 0x44uy 0x5555us;
                TSIH = tsih_me.fromPrim 0x6666us;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                CID = cid_me.fromPrim 0x1111us;
                CmdSN = cmdsn_me.fromPrim 0x22222222u;
                ExpStatSN = statsn_me.fromPrim 0x33333333u;
                TextRequest = [| 0x00uy .. 0xFFuy |];
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.LOGIN_REQ )

            let pdu = recvPDU_logi :?> LoginRequestPDU
            Assert.True( ( pdu.T = false ) )
            Assert.True( ( pdu.C = true ) )
            Assert.True( ( pdu.CSG = LoginReqStateCd.SEQURITY ) )
            Assert.True( ( pdu.NSG = LoginReqStateCd.SEQURITY ) )
            Assert.True( ( pdu.VersionMax = 0x11uy ) )
            Assert.True( ( pdu.VersionMin = 0x22uy ) )
            Assert.True( ( pdu.VersionMin = 0x22uy ) )
            Assert.True( ( pdu.ISID |> isid_me.get_T = 0x00uy ) )
            Assert.True( ( pdu.ISID |> isid_me.get_A = 0x11uy ) )
            Assert.True( ( pdu.ISID |> isid_me.get_B = 0x2222us ) )
            Assert.True( ( pdu.ISID |> isid_me.get_C = 0x44uy ) )
            Assert.True( ( pdu.ISID |> isid_me.get_D = 0x5555us ) )
            Assert.True( ( pdu.TSIH = tsih_me.fromPrim 0x6666us ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.CID = cid_me.fromPrim 0x1111us ) )
            Assert.True( ( pdu.CmdSN = cmdsn_me.fromPrim 0x22222222u ) )
            Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 0x33333333u ) )
            Assert.True( ( pdu.TextRequest = [| 0x00uy .. 0xFFuy |] ) )
            Assert.True( ( pdu.ByteCount = 312u ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginResponsePDU_001() =
        use ms = new MemoryStream()
        let psus =
            {
                T = true;
                C = true;
                CSG = LoginReqStateCd.SEQURITY;
                NSG = LoginReqStateCd.SEQURITY;
                VersionMax = 0x11uy;
                VersionActive = 0x22uy;
                ISID = isid_me.fromElem 0x00uy 0x11uy 0x2222us 0x44uy 0x5555us;
                TSIH = tsih_me.fromPrim 0x6666us;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                StatSN = statsn_me.fromPrim 0x22222222u;
                ExpCmdSN = cmdsn_me.fromPrim 0x33333333u;
                MaxCmdSN = cmdsn_me.fromPrim 0x44444444u;
                Status = LoginResStatCd.SUCCESS;
                TextResponse = [| 0x00uy .. 0xFFuy |]
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login response PDU, if C bit set to 1, T bit must be 0." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginResponsePDU_002() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x23uy; 0x48uy; 0x00uy; 0x00uy; // CSG is error
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0xC0uy; 0x00uy; 0x00uy; 0x00uy; // ISID, TSIH
                0x00uy; 0x00uy; 0x66uy; 0x66uy;
                0xFEuy; 0xEEuy; 0xFEuy; 0xEEuy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x22uy; 0x22uy; 0x22uy; 0x22uy; // StatSN
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // ExpCmdSN
                0x44uy; 0x44uy; 0x44uy; 0x44uy; // MaxCmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Status-Class, Status-Detail
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_None, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login response PDU, CSG(0x02) field value is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginResponsePDU_003() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x23uy; 0xC2uy; 0x00uy; 0x00uy; // NSG is error
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0xC0uy; 0x00uy; 0x00uy; 0x00uy; // ISID, TSIH
                0x00uy; 0x00uy; 0x66uy; 0x66uy;
                0xFEuy; 0xEEuy; 0xFEuy; 0xEEuy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x22uy; 0x22uy; 0x22uy; 0x22uy; // StatSN
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // ExpCmdSN
                0x44uy; 0x44uy; 0x44uy; 0x44uy; // MaxCmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Status-Class, Status-Detail
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_None, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login response PDU, NSG(0x02) field value is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginResponsePDU_004() =
        use ms = new MemoryStream()
        let psus =
            {
                T = false;
                C = true;
                CSG = LoginReqStateCd.FULL;
                NSG = LoginReqStateCd.OPERATIONAL;
                VersionMax = 0x11uy;
                VersionActive = 0x22uy;
                ISID = isid_me.fromElem 0x00uy 0x11uy 0x2222us 0x44uy 0x5555us;
                TSIH = tsih_me.fromPrim 0x6666us;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                StatSN = statsn_me.fromPrim 0x22222222u;
                ExpCmdSN = cmdsn_me.fromPrim 0x33333333u;
                MaxCmdSN = cmdsn_me.fromPrim 0x44444444u;
                Status = LoginResStatCd.SUCCESS;
                TextResponse = [| 0x00uy .. 0xFFuy |]
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login response PDU, CSG(0x03) field value is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginResponsePDU_005() =
        use ms = new MemoryStream()
        let psus =
            {
                T = true;
                C = false;
                CSG = LoginReqStateCd.SEQURITY;
                NSG = LoginReqStateCd.SEQURITY;
                VersionMax = 0x11uy;
                VersionActive = 0x22uy;
                ISID = isid_me.fromElem 0x00uy 0x11uy 0x2222us 0x44uy 0x5555us;
                TSIH = tsih_me.fromPrim 0x6666us;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                StatSN = statsn_me.fromPrim 0x22222222u;
                ExpCmdSN = cmdsn_me.fromPrim 0x33333333u;
                MaxCmdSN = cmdsn_me.fromPrim 0x44444444u;
                Status = LoginResStatCd.SUCCESS;
                TextResponse = [| 0x00uy .. 0xFFuy |]
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login response PDU, CSG(0x00) and NSG(0x00) fields value combination is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginResponsePDU_006() =
        use ms = new MemoryStream()
        let psus =
            {
                T = true;
                C = false;
                CSG = LoginReqStateCd.OPERATIONAL;
                NSG = LoginReqStateCd.OPERATIONAL;
                VersionMax = 0x11uy;
                VersionActive = 0x22uy;
                ISID = isid_me.fromElem 0x00uy 0x11uy 0x2222us 0x44uy 0x5555us;
                TSIH = tsih_me.fromPrim 0x6666us;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                StatSN = statsn_me.fromPrim 0x22222222u;
                ExpCmdSN = cmdsn_me.fromPrim 0x33333333u;
                MaxCmdSN = cmdsn_me.fromPrim 0x44444444u;
                Status = LoginResStatCd.SUCCESS;
                TextResponse = [| 0x00uy .. 0xFFuy |]
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login response PDU, CSG(0x01) and NSG(0x01) fields value combination is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginResponsePDU_007() =
        use ms = new MemoryStream()
        let psus =
            {
                T = false;
                C = true;
                CSG = LoginReqStateCd.SEQURITY;
                NSG = LoginReqStateCd.SEQURITY;
                VersionMax = 0x11uy;
                VersionActive = 0x22uy;
                ISID = isid_me.fromElem 0xC0uy 0x11uy 0x2222us 0x44uy 0x5555us;
                TSIH = tsih_me.fromPrim 0x6666us;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                StatSN = statsn_me.fromPrim 0x22222222u;
                ExpCmdSN = cmdsn_me.fromPrim 0x33333333u;
                MaxCmdSN = cmdsn_me.fromPrim 0x44444444u;
                Status = LoginResStatCd.SUCCESS;
                TextResponse = [| 0x00uy .. 0xFFuy |]
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login response PDU, T(0xC0) field in ISID value is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( LoginResStatCd.SUCCESS )>]
    [<InlineData( LoginResStatCd.REDIRECT_TMP )>]
    [<InlineData( LoginResStatCd.REDIRECT_PERM )>]
    [<InlineData( LoginResStatCd.INITIATOR_ERR )>]
    [<InlineData( LoginResStatCd.AUTH_FAILURE )>]
    [<InlineData( LoginResStatCd.NOT_ALLOWED )>]
    [<InlineData( LoginResStatCd.NOT_FOUND )>]
    [<InlineData( LoginResStatCd.TARGET_REMOVED )>]
    [<InlineData( LoginResStatCd.UNSUPPORTED_VERSION )>]
    [<InlineData( LoginResStatCd.TOO_MANY_CONS )>]
    [<InlineData( LoginResStatCd.MISSING_PARAMS )>]
    [<InlineData( LoginResStatCd.UNSUPPORT_MCS )>]
    [<InlineData( LoginResStatCd.UNSUPPORT_SESS_TYPE )>]
    [<InlineData( LoginResStatCd.SESS_NOT_EXIST )>]
    [<InlineData( LoginResStatCd.INVALID_LOGIN )>]
    [<InlineData( LoginResStatCd.TARGET_ERROR )>]
    [<InlineData( LoginResStatCd.SERVICE_UNAVAILABLE )>]
    [<InlineData( LoginResStatCd.OUT_OF_RESOURCE )>]
    member _.LoginResponsePDU_008 ( wstat : LoginResStatCd ) =
        use ms = new MemoryStream()
        let psus =
            {
                T = false;
                C = true;
                CSG = LoginReqStateCd.SEQURITY;
                NSG = LoginReqStateCd.SEQURITY;
                VersionMax = 0x11uy;
                VersionActive = 0x22uy;
                ISID = isid_me.fromElem 0x00uy 0x11uy 0x2222us 0x44uy 0x5555us;
                TSIH = tsih_me.fromPrim 0x6666us;
                InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu;
                StatSN = statsn_me.fromPrim 0x22222222u;
                ExpCmdSN = cmdsn_me.fromPrim 0x33333333u;
                MaxCmdSN = cmdsn_me.fromPrim 0x44444444u;
                Status = wstat;
                TextResponse = [| 0x00uy .. 0xFFuy |]
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_None,
                DigestType.DST_None,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 304u ))
        ms.Seek( 0L, SeekOrigin.Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.LOGIN_RES )
    
            let pdu = recvPDU_logi :?> LoginResponsePDU

            Assert.True( ( pdu.T = false ) )
            Assert.True( ( pdu.C = true ) )
            Assert.True( ( pdu.CSG = LoginReqStateCd.SEQURITY ) )
            Assert.True( ( pdu.NSG = LoginReqStateCd.SEQURITY ) )
            Assert.True( ( pdu.VersionMax = 0x11uy ) )
            Assert.True( ( pdu.VersionActive = 0x22uy ) )
            Assert.True( ( pdu.ISID |> isid_me.get_T = 0x00uy ) )
            Assert.True( ( pdu.ISID |> isid_me.get_A = 0x11uy ) )
            Assert.True( ( pdu.ISID |> isid_me.get_B = 0x2222us ) )
            Assert.True( ( pdu.ISID |> isid_me.get_C = 0x44uy ) )
            Assert.True( ( pdu.ISID |> isid_me.get_D = 0x5555us ) )
            Assert.True( ( pdu.TSIH = tsih_me.fromPrim 0x6666us ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0xFEEEFEEEu ) )
            Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0x22222222u ) )
            Assert.True( ( pdu.ExpCmdSN = cmdsn_me.fromPrim 0x33333333u ) )
            Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x44444444u ) )
            Assert.True( ( pdu.Status = wstat ) )
            Assert.True( ( pdu.TextResponse = [| 0x00uy .. 0xFFuy |] ) )
            ()
        with
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LoginResponsePDU_009() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x23uy; 0x40uy; 0x00uy; 0x00uy; // CSG, NSG
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0xC0uy; 0x00uy; 0x00uy; 0x00uy; // ISID, TSIH
                0x00uy; 0x00uy; 0x66uy; 0x66uy;
                0xFEuy; 0xEEuy; 0xFEuy; 0xEEuy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x22uy; 0x22uy; 0x22uy; 0x22uy; // StatSN
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // ExpCmdSN
                0x44uy; 0x44uy; 0x44uy; 0x44uy; // MaxCmdSN
                0xFFuy; 0xFEuy; 0x00uy; 0x00uy; // Status-Class, Status-Detail is error
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_None, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Login response PDU, Status-Class and Status-Detail(0xFFFE) field value is invalid." ) =0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LogoutRequestPDU_001() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x06uy; 0x81uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x01uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Target Transfer Tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // R2TSN
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy; // Buffer Offset
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; // Desired Data Transfer Length
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS1
                0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 
                0x00uy; 0x08uy; 0x01uy; 0xFFuy; // AHS2
                0x10uy; 0x11uy; 0x12uy; 0x13uy; 
                0x14uy; 0x15uy; 0x16uy; 0x17uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 76 ( Functions.CRC32 buf.[72..75] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Logout request PDU, TotalAHSLength must be 0 and AHS must be empty." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LogoutRequestPDU_002() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x06uy; 0x81uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x01uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Target Transfer Tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // R2TSN
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy; // Buffer Offset
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; // Desired Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        Functions.UInt32ToNetworkBytes buf 56 ( Functions.CRC32 buf.[52..55] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Logout request PDU, DataSegmentLength must be 0 and DataSegment must be empty." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LogoutRequestPDU_003() =
        for wreason in [| LogoutReqReasonCd.CLOSE_SESS; LogoutReqReasonCd.CLOSE_CONN; LogoutReqReasonCd.RECOVERY; |] do
            use ms = new MemoryStream()
            let psus =
                {
                    I = true;
                    ReasonCode = wreason;
                    InitiatorTaskTag = itt_me.fromPrim 0x11111111u;
                    CID = cid_me.fromPrim 0x2222us;
                    CmdSN = cmdsn_me.fromPrim 0x33333333u;
                    ExpStatSN = statsn_me.fromPrim 0x44444444u;
                    ByteCount = 0u;
                }
            let sendBytesCnt =
                PDU.SendPDU(
                    8192u,
                    DigestType.DST_CRC32C,
                    DigestType.DST_CRC32C,
                    tsih1o,
                    cid1o,
                    cnt1o,
                    objidx_me.NewID(),
                    ms,
                    psus
                )
                |> Functions.RunTaskSynchronously
            Assert.True(( sendBytesCnt = 52u ))
            ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

            try
                let recvPDU_logi = 
                    PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                    |> Functions.RunTaskSynchronously
                Assert.True( recvPDU_logi.Opcode = OpcodeCd.LOGOUT_REQ )
    
                let pdu = recvPDU_logi :?> LogoutRequestPDU

                Assert.True( ( pdu.I = true ) )
                Assert.True( ( pdu.ReasonCode = wreason ) )
                Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0x11111111u ) )
                Assert.True( ( pdu.CID = cid_me.fromPrim 0x2222us ) )
                Assert.True( ( pdu.CmdSN = cmdsn_me.fromPrim 0x33333333u ) )
                Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 0x44444444u ) )
                Assert.True( ( pdu.ByteCount = 52u ) )
            with
            | _ as x ->
                Assert.Fail __LINE__

    [<Fact>]
    member _.LogoutRequestPDU_004() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x46uy; 0xFFuy; 0x00uy; 0x00uy; // ReasonCode is error
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x22uy; 0x22uy; 0x00uy; 0x00uy; // CID
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // CmdSN
                0x44uy; 0x44uy; 0x44uy; 0x44uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore
        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Logout request PDU, ReasonCode(0x7F) field value is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LogoutResponsePDU_001() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x26uy; 0x81uy; 0x00uy; 0x00uy; 
                0x14uy; 0x00uy; 0x00uy; 0x01uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Target Transfer Tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // R2TSN
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy; // Buffer Offset
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; // Desired Data Transfer Length
                0x00uy; 0x05uy; 0x02uy; 0x00uy; // AHS1
                0xDEuy; 0xADuy; 0xBEuy; 0xEFuy; 
                0x00uy; 0x08uy; 0x01uy; 0xFFuy; // AHS2
                0x10uy; 0x11uy; 0x12uy; 0x13uy; 
                0x14uy; 0x15uy; 0x16uy; 0x17uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 68 ( Functions.CRC32 buf.[0..67] )
        Functions.UInt32ToNetworkBytes buf 76 ( Functions.CRC32 buf.[72..75] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Logout response PDU, TotalAHSLength must be 0 and AHS must be empty." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.LogoutResponsePDU_002() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x26uy; 0x81uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x01uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // LUN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // Target Transfer Tag
                0x10uy; 0x20uy; 0x30uy; 0x40uy; // StatSN
                0x0Fuy; 0x0Euy; 0x0Duy; 0x0Cuy; // ExpCmdSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // MaxCmdSN
                0x04uy; 0x05uy; 0x06uy; 0x07uy; // R2TSN
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy; // Buffer Offset
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy; // Desired Data Transfer Length
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Data Segment
                0xAAuy; 0x00uy; 0x00uy; 0x00uy  // Data Digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        Functions.UInt32ToNetworkBytes buf 56 ( Functions.CRC32 buf.[52..55] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Logout response PDU, DataSegmentLength must be 0 and DataSegment must be empty." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__
 
     [<Fact>]
     member _.LogoutResponsePDU_003() =
        for wreason in [| LogoutResCd.SUCCESS; LogoutResCd.CID_NOT_FOUND; LogoutResCd.RECOVERY_NOT_SUPPORT; LogoutResCd.CLEANUP_FAILED; |] do
            use ms = new MemoryStream()
            let psus =
                {
                    Response = wreason;
                    InitiatorTaskTag = itt_me.fromPrim 0x11111111u;
                    StatSN = statsn_me.fromPrim 0x22222222u;
                    ExpCmdSN = cmdsn_me.fromPrim 0x33333333u;
                    MaxCmdSN = cmdsn_me.fromPrim 0x44444444u;
                    Time2Wait = 0x5555us;
                    Time2Retain = 0x6666us;
                    CloseAllegiantConnection = true;
                }
            let sendBytesCnt =
                PDU.SendPDU(
                    8192u,
                    DigestType.DST_CRC32C,
                    DigestType.DST_CRC32C,
                    tsih1o,
                    cid1o,
                    cnt1o,
                    objidx_me.NewID(),
                    ms,
                    psus
                )
                |> Functions.RunTaskSynchronously
            Assert.True(( sendBytesCnt = 52u ))
            ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

            try
                let recvPDU_logi = 
                    PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                    |> Functions.RunTaskSynchronously
                Assert.True( recvPDU_logi.Opcode = OpcodeCd.LOGOUT_RES )
    
                let pdu = recvPDU_logi :?> LogoutResponsePDU

                Assert.True( ( pdu.Response = wreason ) )
                Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0x11111111u ) )
                Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0x22222222u ) )
                Assert.True( ( pdu.ExpCmdSN = cmdsn_me.fromPrim 0x33333333u ) )
                Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x44444444u ) )
                Assert.True( ( pdu.Time2Wait = 0x5555us ) )
                Assert.True( ( pdu.Time2Retain = 0x6666us ) )
            with
            | _ as x ->
                Assert.Fail __LINE__
 
     [<Fact>]
     member _.LogoutResponsePDU_004() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x26uy; 0x80uy; 0xFFuy; 0x00uy; // Response is error
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x11uy; 0x11uy; 0x11uy; 0x11uy; // Initiator task tag
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x22uy; 0x22uy; 0x22uy; 0x22uy; // StatSN
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // ExpCmdSN
                0x44uy; 0x44uy; 0x44uy; 0x44uy; // MaxCmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x55uy; 0x55uy; 0x66uy; 0x66uy; // Time2Wait, Time2Retain
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Logout response PDU, Response(0xFF) field value is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

     [<Fact>]
     member _.SNACKRequestPDU_001() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x10uy; 0xFFuy; 0x00uy; 0x00uy; // Type is error
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x11uy; 0x11uy; 0x11uy; 0x11uy; // Initiator task tag
                0x22uy; 0x22uy; 0x22uy; 0x22uy; // TargetTransferTag
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x44uy; 0x44uy; 0x44uy; 0x44uy; // BegRun
                0x55uy; 0x55uy; 0x55uy; 0x55uy; // RunLength
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In SNACK request PDU, Type(0x0F) field value is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

     [<Fact>]
     member _.SNACKRequestPDU_002() =
        for wtype in [| SnackReqTypeCd.DATA_R2T; SnackReqTypeCd.STATUS; SnackReqTypeCd.DATA_ACK; SnackReqTypeCd.RDATA_SNACK |] do
            use ms = new MemoryStream()
            let psus =
                {
                    Type = wtype;
                    LUN = lun_me.fromPrim 0x0001020304050607UL;
                    InitiatorTaskTag = itt_me.fromPrim 0x11111111u;
                    TargetTransferTag = ttt_me.fromPrim 0x22222222u;
                    ExpStatSN = statsn_me.fromPrim 0x33333333u;
                    BegRun = 0x44444444u;
                    RunLength = 0x55555555u;
                    ByteCount = 0u;
                }
            let sendBytesCnt =
                PDU.SendPDU(
                    8192u,
                    DigestType.DST_CRC32C,
                    DigestType.DST_CRC32C,
                    tsih1o,
                    cid1o,
                    cnt1o,
                    objidx_me.NewID(),
                    ms,
                    psus
                )
                |> Functions.RunTaskSynchronously
            Assert.True(( sendBytesCnt = 52u ))
            ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

            try
                let recvPDU_logi = 
                    PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                    |> Functions.RunTaskSynchronously
                Assert.True( recvPDU_logi.Opcode = OpcodeCd.SNACK )
    
                let pdu = recvPDU_logi :?> SNACKRequestPDU

                Assert.True( ( pdu.Type = wtype ) )
                Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
                Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0x11111111u ) )
                Assert.True( ( pdu.TargetTransferTag = ttt_me.fromPrim 0x22222222u ) )
                Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 0x33333333u ) )
                Assert.True( ( pdu.BegRun = 0x44444444u ) )
                Assert.True( ( pdu.RunLength = 0x55555555u ) )
                Assert.True( ( pdu.ByteCount = 52u ) )
            with
            | _ as x ->
                Assert.Fail __LINE__

     [<Fact>]
     member _.RejectPDU_001() =
        use ms = new MemoryStream()
        let buf =
             [|
                0x3Fuy; 0x80uy; 0x00uy; 0x00uy; // Reason is error
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength , DataSegmentLength 
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x11uy; 0x11uy; 0x11uy; 0x11uy; // StatSN
                0x22uy; 0x22uy; 0x22uy; 0x22uy; // ExpCmdSN
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // MaxCmdSN
                0x44uy; 0x44uy; 0x44uy; 0x44uy; // DataSN_or_R2TSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Header digest
            |]
        Functions.UInt32ToNetworkBytes buf 48 ( Functions.CRC32 buf.[0..47] )
        ms.Write( buf, 0, buf.Length )
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In Reject PDU, Reason(0x00) field value is invalid." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

     [<Fact>]
     member _.RejectPDU_002() =
        for wreason in [| RejectResonCd.DATA_DIGEST_ERR; RejectResonCd.SNACK_REJECT; RejectResonCd.PROTOCOL_ERR; RejectResonCd.COM_NOT_SUPPORT;
                        RejectResonCd.IMMIDIATE_COM_REJECT; RejectResonCd.TASK_IN_PROGRESS; RejectResonCd.INVALID_DATA_ACK; RejectResonCd.INVALID_PDU_FIELD;
                        RejectResonCd.LONG_OPE_REJECT; RejectResonCd.NEGOTIATION_RESET; RejectResonCd.WAIT_FOR_LOGOUT |] do
            use ms = new MemoryStream()
            let psus =
                {
                    Reason = wreason;
                    StatSN = statsn_me.fromPrim 0x11111111u;
                    ExpCmdSN = cmdsn_me.fromPrim 0x22222222u;
                    MaxCmdSN = cmdsn_me.fromPrim 0x33333333u;
                    DataSN_or_R2TSN = datasn_me.fromPrim 0x44444444u;
                    HeaderData = [| 0x00uy .. 0xFFuy |];
                }
            let sendBytesCnt =
                PDU.SendPDU(
                    8192u,
                    DigestType.DST_CRC32C,
                    DigestType.DST_CRC32C,
                    tsih1o,
                    cid1o,
                    cnt1o,
                    objidx_me.NewID(),
                    ms,
                    psus
                )
                |> Functions.RunTaskSynchronously
            Assert.True(( sendBytesCnt = 312u ))
            ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

            try
                let recvPDU_logi = 
                    PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                    |> Functions.RunTaskSynchronously
                Assert.True( recvPDU_logi.Opcode = OpcodeCd.REJECT )
    
                let pdu = recvPDU_logi :?> RejectPDU

                Assert.True( ( pdu.Reason = wreason ) )
                Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0x11111111u ) )
                Assert.True( ( pdu.ExpCmdSN = cmdsn_me.fromPrim 0x22222222u ) )
                Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0x33333333u ) )
                Assert.True( ( pdu.DataSN_or_R2TSN = datasn_me.fromPrim 0x44444444u ) )
                Assert.True( ( pdu.HeaderData = [| 0x00uy .. 0xFFuy |] ) )
            with
            | _ as x ->
                Assert.Fail __LINE__

     [<Fact>]
     member _.NOPOutPDU_001() =
        use ms = new MemoryStream()
        let psus =
            {
                I = false;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu;
                TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                CmdSN = cmdsn_me.fromPrim 0xDDDDDDDDu;
                ExpStatSN = statsn_me.fromPrim 0xCCCCCCCCu;
                PingData = PooledBuffer.Rent [| 0x00uy .. 0xFFuy |];
                ByteCount = 0u;
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let _ =
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.Fail __LINE__
        with
        | :? SessionRecoveryException as x ->
            Assert.True( ( Functions.CompareStringHeader x.Message "In NOP-Out PDU, if InitiatorTaskTag field is 0xFFFFFFFF, I bit must be set 1." ) = 0 ) |> ignore
        | _ as x ->
            Assert.Fail __LINE__

     [<Fact>]
     member _.NOPOutPDU_002() =
        use ms = new MemoryStream()
        let psus =
            {
                I = true;
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0xF0F0F0F0u;
                TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                CmdSN = cmdsn_me.fromPrim 0xDDDDDDDDu;
                ExpStatSN = statsn_me.fromPrim 0xCCCCCCCCu;
                PingData = PooledBuffer.Rent [| 0x00uy .. 0xFFuy |];
                ByteCount = 0u;
            }

        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Target )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.NOP_OUT )
    
            let pdu = recvPDU_logi :?> NOPOutPDU

            Assert.True( ( pdu.I = true ) )
            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0xF0F0F0F0u ) )
            Assert.True( ( pdu.TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu ) )
            Assert.True( ( pdu.CmdSN = cmdsn_me.fromPrim 0xDDDDDDDDu ) )
            Assert.True( ( pdu.ExpStatSN = statsn_me.fromPrim 0xCCCCCCCCu ) )
            Assert.True( ( PooledBuffer.ValueEqualsWithArray pdu.PingData [| 0x00uy .. 0xFFuy |] ) )
            Assert.True( ( pdu.ByteCount = 312u ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

     [<Fact>]
     member _.NOPInPDU_001() =
        use ms = new MemoryStream()
        let psus =
            {
                LUN = lun_me.fromPrim 0x0001020304050607UL;
                InitiatorTaskTag = itt_me.fromPrim 0xF0F0F0F0u;
                TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu;
                StatSN = statsn_me.fromPrim 0xDDDDDDDDu;
                ExpCmdSN = cmdsn_me.fromPrim 0xCCCCCCCCu;
                MaxCmdSN = cmdsn_me.fromPrim 0xBBBBBBBBu;
                PingData = PooledBuffer.Rent [| 0x00uy .. 0xFFuy |];
            }
        let sendBytesCnt =
            PDU.SendPDU(
                8192u,
                DigestType.DST_CRC32C,
                DigestType.DST_CRC32C,
                tsih1o,
                cid1o,
                cnt1o,
                objidx_me.NewID(),
                ms,
                psus
            )
            |> Functions.RunTaskSynchronously
        Assert.True(( sendBytesCnt = 312u ))
        ms.Seek( 0L, SeekOrigin .Begin ) |> ignore

        try
            let recvPDU_logi = 
                PDU.Receive( 8192u, DigestType.DST_CRC32C, DigestType.DST_CRC32C, tsih1o, cid1o, cnt1o, ms, Standpoint.Initiator )
                |> Functions.RunTaskSynchronously
            Assert.True( recvPDU_logi.Opcode = OpcodeCd.NOP_IN )
    
            let pdu = recvPDU_logi :?> NOPInPDU

            Assert.True( ( pdu.LUN = lun_me.fromPrim 0x0001020304050607UL ) )
            Assert.True( ( pdu.InitiatorTaskTag = itt_me.fromPrim 0xF0F0F0F0u ) )
            Assert.True( ( pdu.TargetTransferTag = ttt_me.fromPrim 0xEEEEEEEEu ) )
            Assert.True( ( pdu.StatSN = statsn_me.fromPrim 0xDDDDDDDDu ) )
            Assert.True( ( pdu.ExpCmdSN = cmdsn_me.fromPrim 0xCCCCCCCCu ) )
            Assert.True( ( pdu.MaxCmdSN = cmdsn_me.fromPrim 0xBBBBBBBBu ) )
            Assert.True( ( PooledBuffer.ValueEqualsWithArray pdu.PingData [| 0x00uy .. 0xFFuy |] ) )
        with
        | _ as x ->
            Assert.Fail __LINE__

     [<Fact>]
     member _.GetHeader_NOP_IN() =
        let pdu =
            {
                LUN = lun_me.fromPrim 0x0102030405060708UL;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                TargetTransferTag = ttt_me.fromPrim 0x01020304u;
                StatSN = statsn_me.fromPrim 0x01020304u;
                ExpCmdSN = cmdsn_me.fromPrim 0x01020304u;
                MaxCmdSN = cmdsn_me.fromPrim 0x01020304u;
                PingData = PooledBuffer.RentAndInit 256;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x20uy; 0x80uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x01uy; 0x00uy; // TotalAHSLength, DataSegmentLength
                0x07uy; 0x08uy; 0x05uy; 0x06uy; // LUN
                0x03uy; 0x04uy; 0x01uy; 0x02uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // ITT
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // TTT
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // StatSN
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // ExpCmdSN
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // MaxCmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
        ))

     [<Fact>]
     member _.GetHeader_SCSI_RES() =
        let pdu =
            {
                o = true;
                u = false;
                O = true;
                U = false;
                Response = iScsiSvcRespCd.TARGET_FAILURE;
                Status = ScsiCmdStatCd.ACA_ACTIVE;
                InitiatorTaskTag = itt_me.fromPrim 0x02040608u;
                SNACKTag = snacktag_me.fromPrim 0x01030507u;
                StatSN = statsn_me.fromPrim 0x01020304u;
                ExpCmdSN = cmdsn_me.fromPrim 0x01020304u;
                MaxCmdSN = cmdsn_me.fromPrim 0x01020304u;
                ExpDataSN = datasn_me.fromPrim 0x11223344u;
                BidirectionalReadResidualCount = 0x22446688u;
                ResidualCount = 0x33557799u;
                SenseLength = 0x1122us;
                SenseData = ArraySegment<byte>( [| 0uy .. 200uy |], 0, 200 );
                ResponseData = ArraySegment<byte>( [| 0uy .. 200uy |], 0, 200 );
                ResponseFence = ResponseFenceNeedsFlag.Immediately;
                DataInBuffer = PooledBuffer.Empty;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x21uy; 0x94uy; 0x01uy; 0x30uy; // o,u,O,U,Response,Status
                0x00uy; 0x00uy; 0x01uy; 0x92uy; // TotalAHSLength, DataSegmentLength
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x02uy; 0x04uy; 0x06uy; 0x08uy; // ITT
                0x01uy; 0x03uy; 0x05uy; 0x07uy; // SNACKTag
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // StatSN
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // ExpCmdSN
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // MaxCmdSN
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // ExpDataSN
                0x22uy; 0x44uy; 0x66uy; 0x88uy; // BidirectionalReadResidualCount
                0x33uy; 0x55uy; 0x77uy; 0x99uy; // ResidualCount
            |]
        ))

     [<Fact>]
     member _.GetHeader_SCSI_TASK_MGR_RES() =
        let pdu =
            {
                Response = TaskMgrResCd.FUCTION_COMPLETE;
                InitiatorTaskTag = itt_me.fromPrim 0x01020304u;
                StatSN = statsn_me.fromPrim 0x01020304u;
                ExpCmdSN = cmdsn_me.fromPrim 0x01020304u;
                MaxCmdSN = cmdsn_me.fromPrim 0x01020304u;
                ResponseFence = ResponseFenceNeedsFlag.Immediately;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x22uy; 0x80uy; 0x00uy; 0x00uy; // Response
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength, DataSegmentLength
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // ITT
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // StatSN
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // ExpCmdSN
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // MaxCmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
        ))

     [<Fact>]
     member _.GetHeader_LOGIN_RES() =
        let pdu =
            {
                T = true;
                C = true;
                CSG = LoginReqStateCd.FULL;
                NSG = LoginReqStateCd.FULL;
                VersionMax = 0xAAuy;
                VersionActive = 0xBBuy;
                ISID = isid_me.fromElem 0xC0uy 0x3Auy 0xBBBBus 0xCCuy 0xDDDDus;
                TSIH = tsih_me.fromPrim 0xEEEEus;
                InitiatorTaskTag = itt_me.fromPrim 0x11111111u;
                StatSN = statsn_me.fromPrim 0x22222222u;
                ExpCmdSN = cmdsn_me.fromPrim 0x33333333u;
                MaxCmdSN = cmdsn_me.fromPrim 0x44444444u;
                Status = LoginResStatCd.INITIATOR_ERR;
                TextResponse = [| 0uy .. 255uy |];
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x23uy; 0xCFuy; 0xAAuy; 0xBBuy; // T,C,CSG,NSG,Version-max,Version-active
                0x00uy; 0x00uy; 0x01uy; 0x00uy; // TotalAHSLength, DataSegmentLength
                0xFAuy; 0xBBuy; 0xBBuy; 0xCCuy; // ISID
                0xDDuy; 0xDDuy; 0xEEuy; 0xEEuy; // ISID,TSIH
                0x11uy; 0x11uy; 0x11uy; 0x11uy; // ITT
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x22uy; 0x22uy; 0x22uy; 0x22uy; // StatSN
                0x33uy; 0x33uy; 0x33uy; 0x33uy; // ExpCmdSN
                0x44uy; 0x44uy; 0x44uy; 0x44uy; // MaxCmdSN
                0x02uy; 0x00uy; 0x00uy; 0x00uy; // Status-Class,Status-Detail
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
        ))

     [<Fact>]
     member _.GetHeader_TEXT_RES() =
        let pdu =
            {
                F = true;
                C = true;
                LUN = lun_me.fromPrim 0x1122334455667788UL;
                InitiatorTaskTag = itt_me.fromPrim 0x21222324u;
                TargetTransferTag = ttt_me.fromPrim 0x31323334u;
                StatSN = statsn_me.fromPrim 0x41424344u;
                ExpCmdSN = cmdsn_me.fromPrim 0x51525354u;
                MaxCmdSN = cmdsn_me.fromPrim 0x61626364u;
                TextResponse = [| 0uy .. 127uy |];
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x24uy; 0xC0uy; 0x00uy; 0x00uy; // F,C
                0x00uy; 0x00uy; 0x00uy; 0x80uy; // TotalAHSLength, DataSegmentLength
                0x77uy; 0x88uy; 0x55uy; 0x66uy; // LUN
                0x33uy; 0x44uy; 0x11uy; 0x22uy;
                0x21uy; 0x22uy; 0x23uy; 0x24uy; // ITT
                0x31uy; 0x32uy; 0x33uy; 0x34uy; // TTT
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // StatSN
                0x51uy; 0x52uy; 0x53uy; 0x54uy; // ExpCmdSN
                0x61uy; 0x62uy; 0x63uy; 0x64uy; // MaxCmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
        ))

     [<Fact>]
     member _.GetHeader_SCSI_DATA_IN() =
        let pdu =
            {
                F = true;
                A = true;
                O = true;
                U = true;
                S = true;
                Status = ScsiCmdStatCd.RESERVATION_CONFLICT;
                LUN = lun_me.fromPrim 0x1122334455667788UL;
                InitiatorTaskTag = itt_me.fromPrim 0x21222324u;
                TargetTransferTag = ttt_me.fromPrim 0x31323334u;
                StatSN = statsn_me.fromPrim 0x41424344u;
                ExpCmdSN = cmdsn_me.fromPrim 0x51525354u;
                MaxCmdSN = cmdsn_me.fromPrim 0x61626364u;
                DataSN = datasn_me.fromPrim 0x71727374u;
                BufferOffset = 0x81828384u;
                ResidualCount = 0x91929394u;
                DataSegment = ArraySegment<byte>( [| 0uy .. 199uy |], 0, 200 );
                ResponseFence = ResponseFenceNeedsFlag.Immediately;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x25uy; 0xC7uy; 0x00uy; 0x18uy; // F,A,O,U,S,Status
                0x00uy; 0x00uy; 0x00uy; 0xC8uy; // TotalAHSLength, DataSegmentLength
                0x77uy; 0x88uy; 0x55uy; 0x66uy; // LUN
                0x33uy; 0x44uy; 0x11uy; 0x22uy;
                0x21uy; 0x22uy; 0x23uy; 0x24uy; // ITT
                0x31uy; 0x32uy; 0x33uy; 0x34uy; // TTT
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // StatSN
                0x51uy; 0x52uy; 0x53uy; 0x54uy; // ExpCmdSN
                0x61uy; 0x62uy; 0x63uy; 0x64uy; // MaxCmdSN
                0x71uy; 0x72uy; 0x73uy; 0x74uy; // DataSN
                0x81uy; 0x82uy; 0x83uy; 0x84uy; // BufferOffset
                0x91uy; 0x92uy; 0x93uy; 0x94uy; // ResidualCount
            |]
        ))

     [<Fact>]
     member _.GetHeader_LOGOUT_RES() =
        let pdu =
            {
                Response = LogoutResCd.CLEANUP_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0x21222324u;
                StatSN = statsn_me.fromPrim 0x41424344u;
                ExpCmdSN = cmdsn_me.fromPrim 0x51525354u;
                MaxCmdSN = cmdsn_me.fromPrim 0x61626364u;
                Time2Wait = 0x7172us;
                Time2Retain = 0x8182us;
                CloseAllegiantConnection = true;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x26uy; 0x80uy; 0x03uy; 0x00uy; // Response
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength, DataSegmentLength
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reseerved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x21uy; 0x22uy; 0x23uy; 0x24uy; // ITT
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reseerved
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // StatSN
                0x51uy; 0x52uy; 0x53uy; 0x54uy; // ExpCmdSN
                0x61uy; 0x62uy; 0x63uy; 0x64uy; // MaxCmdSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reseerved
                0x71uy; 0x72uy; 0x81uy; 0x82uy; // Time2Wait,Time2Retain
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reseerved
            |]
        ))

     [<Fact>]
     member _.GetHeader_R2T() =
        let pdu =
            {
                LUN = lun_me.fromPrim 0x1122334455667788UL;
                InitiatorTaskTag = itt_me.fromPrim 0x21222324u;
                TargetTransferTag = ttt_me.fromPrim 0x31323334u;
                StatSN = statsn_me.fromPrim 0x41424344u;
                ExpCmdSN = cmdsn_me.fromPrim 0x51525354u;
                MaxCmdSN = cmdsn_me.fromPrim 0x61626364u;
                R2TSN = datasn_me.fromPrim 0x71727374u;
                BufferOffset = 0x81828384u;
                DesiredDataTransferLength = 0x91929394u;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x31uy; 0x80uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength, DataSegmentLength
                0x77uy; 0x88uy; 0x55uy; 0x66uy; // LUN
                0x33uy; 0x44uy; 0x11uy; 0x22uy;
                0x21uy; 0x22uy; 0x23uy; 0x24uy; // ITT
                0x31uy; 0x32uy; 0x33uy; 0x34uy; // TTT
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // StatSN
                0x51uy; 0x52uy; 0x53uy; 0x54uy; // ExpCmdSN
                0x61uy; 0x62uy; 0x63uy; 0x64uy; // MaxCmdSN
                0x71uy; 0x72uy; 0x73uy; 0x74uy; // R2TSN
                0x81uy; 0x82uy; 0x83uy; 0x84uy; // BufferOffset
                0x91uy; 0x92uy; 0x93uy; 0x94uy; // DesiredDataTransferLength
            |]
        ))

     [<Fact>]
     member _.GetHeader_ASYNC() =
        let pdu =
            {
                LUN = lun_me.fromPrim 0x1122334455667788UL;
                StatSN = statsn_me.fromPrim 0x41424344u;
                ExpCmdSN = cmdsn_me.fromPrim 0x51525354u;
                MaxCmdSN = cmdsn_me.fromPrim 0x61626364u;
                AsyncEvent = AsyncEventCd.SESSION_CLOSE;
                AsyncVCode = 0xAAuy;
                Parameter1 = 0xBBBBus;
                Parameter2 = 0xCCCCus;
                Parameter3 = 0xDDDDus;
                SenseLength = 200us;
                SenseData = [| 0uy .. 199uy |];
                ISCSIEventData = [| 0uy .. 128uy |];
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x32uy; 0x80uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x01uy; 0x4Buy; // TotalAHSLength, DataSegmentLength
                0x77uy; 0x88uy; 0x55uy; 0x66uy; // LUN
                0x33uy; 0x44uy; 0x11uy; 0x22uy;
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // StatSN
                0x51uy; 0x52uy; 0x53uy; 0x54uy; // ExpCmdSN
                0x61uy; 0x62uy; 0x63uy; 0x64uy; // MaxCmdSN
                0x03uy; 0xAAuy; 0xBBuy; 0xBBuy; // AsyncEvent,AsyncVCode,Parameter1
                0xCCuy; 0xCCuy; 0xDDuy; 0xDDuy; // Parameter2,Parameter3
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            |]
        ))

     [<Fact>]
     member _.GetHeader_REJECT() =
        let pdu =
            {
                Reason = RejectResonCd.IMMIDIATE_COM_REJECT;
                StatSN = statsn_me.fromPrim 0x41424344u;
                ExpCmdSN = cmdsn_me.fromPrim 0x51525354u;
                MaxCmdSN = cmdsn_me.fromPrim 0x61626364u;
                DataSN_or_R2TSN = datasn_me.fromPrim 0x71727374u;
                HeaderData = [| 0uy .. 47uy |];
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x3Fuy; 0x80uy; 0x06uy; 0x00uy; // Reason
                0x00uy; 0x00uy; 0x00uy; 0x30uy; // TotalAHSLength, DataSegmentLength
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // StatSN
                0x51uy; 0x52uy; 0x53uy; 0x54uy; // ExpCmdSN
                0x61uy; 0x62uy; 0x63uy; 0x64uy; // MaxCmdSN
                0x71uy; 0x72uy; 0x73uy; 0x74uy; // DataSN/R2TSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            |]
        ))

     [<Fact>]
     member _.GetHeader_NOP_OUT() =
        let pdu =
            {
                I = true;
                LUN = lun_me.fromPrim 0x1122334455667788UL;
                InitiatorTaskTag = itt_me.fromPrim 0x11121314u;
                TargetTransferTag = ttt_me.fromPrim 0x21222324u;
                CmdSN = cmdsn_me.fromPrim 0x31323334u;
                ExpStatSN = statsn_me.fromPrim 0x41424344u;
                PingData = PooledBuffer.Rent [| 0uy .. 255uy |];
                ByteCount = 0u;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x40uy; 0x80uy; 0x00uy; 0x00uy; // I
                0x00uy; 0x00uy; 0x01uy; 0x00uy; // TotalAHSLength, DataSegmentLength
                0x77uy; 0x88uy; 0x55uy; 0x66uy; // LUN
                0x33uy; 0x44uy; 0x11uy; 0x22uy;
                0x11uy; 0x12uy; 0x13uy; 0x14uy; // ITT
                0x21uy; 0x22uy; 0x23uy; 0x24uy; // TTT
                0x31uy; 0x32uy; 0x33uy; 0x34uy; // CmdSN
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            |]
        ))

     [<Fact>]
     member _.GetHeader_SCSI_COMMAND() =
        let pdu =
            {
                I = true;
                F = true;
                R = true;
                W = true;
                ATTR = TaskATTRCd.ACA_TASK;
                LUN = lun_me.fromPrim 0x1122334455667788UL;
                InitiatorTaskTag = itt_me.fromPrim 0x11121314u;
                ExpectedDataTransferLength = 0x21222324u;
                CmdSN = cmdsn_me.fromPrim 0x31323334u;
                ExpStatSN = statsn_me.fromPrim 0x41424344u;
                ScsiCDB = [| 0uy .. 15uy |];
                DataSegment = PooledBuffer.Rent [| 0uy .. 255uy |];
                BidirectionalExpectedReadDataLength = 0x51525354u;
                ByteCount = 0u;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x41uy; 0xE4uy; 0x00uy; 0x00uy; // I,F,R,W,ATTR
                0x08uy; 0x00uy; 0x01uy; 0x00uy; // TotalAHSLength, DataSegmentLength
                0x77uy; 0x88uy; 0x55uy; 0x66uy; // LUN
                0x33uy; 0x44uy; 0x11uy; 0x22uy;
                0x11uy; 0x12uy; 0x13uy; 0x14uy; // ITT
                0x21uy; 0x22uy; 0x23uy; 0x24uy; // ExpectedDataTransferLength
                0x31uy; 0x32uy; 0x33uy; 0x34uy; // CmdSN
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // ExpStatSN
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // ScsiCDB
                0x04uy; 0x05uy; 0x06uy; 0x07uy;
                0x08uy; 0x09uy; 0x0Auy; 0x0Buy;
                0x0Cuy; 0x0Duy; 0x0Euy; 0x0Fuy;
            |]
        ))

     [<Fact>]
     member _.GetHeader_SCSI_TASK_MGR_REQ() =
        let pdu =
            {
                I = true;
                Function = TaskMgrReqCd.ABORT_TASK_SET;
                LUN = lun_me.fromPrim 0x1122334455667788UL;
                InitiatorTaskTag = itt_me.fromPrim 0x11121314u;
                ReferencedTaskTag = itt_me.fromPrim 0x21222324u;
                CmdSN = cmdsn_me.fromPrim 0x31323334u;
                ExpStatSN = statsn_me.fromPrim 0x41424344u;
                RefCmdSN = cmdsn_me.fromPrim 0x51525354u;
                ExpDataSN = datasn_me.fromPrim 0x61626364u;
                ByteCount = 0u;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x42uy; 0x82uy; 0x00uy; 0x00uy; // I,Function
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength, DataSegmentLength
                0x77uy; 0x88uy; 0x55uy; 0x66uy; // LUN
                0x33uy; 0x44uy; 0x11uy; 0x22uy;
                0x11uy; 0x12uy; 0x13uy; 0x14uy; // ITT
                0x21uy; 0x22uy; 0x23uy; 0x24uy; // ReferencedTaskTag
                0x31uy; 0x32uy; 0x33uy; 0x34uy; // CmdSN
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // ExpStatSN
                0x51uy; 0x52uy; 0x53uy; 0x54uy; // RefCmdSN
                0x61uy; 0x62uy; 0x63uy; 0x64uy; // ExpDataSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
        ))

     [<Fact>]
     member _.GetHeader_LOGIN_REQ() =
        let pdu =
            {
                T = true;
                C = true;
                CSG = LoginReqStateCd.FULL;
                NSG = LoginReqStateCd.FULL;
                VersionMax = 0xAAuy;
                VersionMin = 0xBBuy;
                ISID = isid_me.fromElem 0xC0uy 0x3Auy 0xBBBBus 0xCCuy 0xDDDDus;
                TSIH = tsih_me.fromPrim 0xEEEEus;
                InitiatorTaskTag = itt_me.fromPrim 0x11121314u;
                CID = cid_me.fromPrim 0x2222us;
                CmdSN = cmdsn_me.fromPrim 0x31323334u;
                ExpStatSN = statsn_me.fromPrim 0x41424344u;
                TextRequest = [| 0uy .. 16uy |];
                ByteCount = 0u;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x43uy; 0xCFuy; 0xAAuy; 0xBBuy; // T,C,CSG,NSG,Version-max,Version-min
                0x00uy; 0x00uy; 0x00uy; 0x11uy; // TotalAHSLength, DataSegmentLength
                0xFAuy; 0xBBuy; 0xBBuy; 0xCCuy; // ISID
                0xDDuy; 0xDDuy; 0xEEuy; 0xEEuy; // ISID,TSIH
                0x11uy; 0x12uy; 0x13uy; 0x14uy; // ITT
                0x22uy; 0x22uy; 0x00uy; 0x00uy; // CID
                0x31uy; 0x32uy; 0x33uy; 0x34uy; // CmdSN
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
        ))

     [<Fact>]
     member _.GetHeader_TEXT_REQ() =
        let pdu =
            {
                I = true;
                F = true;
                C = true;
                LUN = lun_me.fromPrim 0x1122334455667788UL;
                InitiatorTaskTag = itt_me.fromPrim 0x11121314u;
                TargetTransferTag = ttt_me.fromPrim 0x21222324u;
                CmdSN = cmdsn_me.fromPrim 0x31323334u;
                ExpStatSN = statsn_me.fromPrim 0x41424344u;
                TextRequest = [| 0uy .. 15uy |];
                ByteCount = 0u;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x44uy; 0xC0uy; 0x00uy; 0x00uy; // F,C
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // TotalAHSLength, DataSegmentLength
                0x77uy; 0x88uy; 0x55uy; 0x66uy; // LUN
                0x33uy; 0x44uy; 0x11uy; 0x22uy;
                0x11uy; 0x12uy; 0x13uy; 0x14uy; // ITT
                0x21uy; 0x22uy; 0x23uy; 0x24uy; // TTT
                0x31uy; 0x32uy; 0x33uy; 0x34uy; // CmdSN
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
        ))

     [<Fact>]
     member _.GetHeader_SCSI_DATA_OUT() =
        let pdu =
            {
                F = true;
                LUN = lun_me.fromPrim 0x1122334455667788UL;
                InitiatorTaskTag = itt_me.fromPrim 0x11121314u;
                TargetTransferTag = ttt_me.fromPrim 0x21222324u;
                ExpStatSN = statsn_me.fromPrim 0x41424344u;
                DataSN = datasn_me.fromPrim 0x51525354u;
                BufferOffset = 0x61626364u;
                DataSegment = PooledBuffer.Rent [| 0uy .. 31uy |];
                ByteCount = 0u;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x05uy; 0x80uy; 0x00uy; 0x00uy; // F
                0x00uy; 0x00uy; 0x00uy; 0x20uy; // TotalAHSLength, DataSegmentLength
                0x77uy; 0x88uy; 0x55uy; 0x66uy; // LUN
                0x33uy; 0x44uy; 0x11uy; 0x22uy;
                0x11uy; 0x12uy; 0x13uy; 0x14uy; // ITT
                0x21uy; 0x22uy; 0x23uy; 0x24uy; // TTT
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x51uy; 0x52uy; 0x53uy; 0x54uy; // DataSN
                0x61uy; 0x62uy; 0x63uy; 0x64uy; // BufferOffset
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
        ))

     [<Fact>]
     member _.GetHeader_LOGOUT_REQ() =
        let pdu =
            {
                I = true;
                ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
                InitiatorTaskTag = itt_me.fromPrim 0x11121314u;
                CID = cid_me.fromPrim 0x2122us;
                CmdSN = cmdsn_me.fromPrim 0x31323334u;
                ExpStatSN = statsn_me.fromPrim 0x41424344u;
                ByteCount = 0u;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x46uy; 0x81uy; 0x00uy; 0x00uy; // I
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength, DataSegmentLength
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x11uy; 0x12uy; 0x13uy; 0x14uy; // ITT
                0x21uy; 0x22uy; 0x00uy; 0x00uy; // CID
                0x31uy; 0x32uy; 0x33uy; 0x34uy; // CmdSN
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
        ))

     [<Fact>]
     member _.GetHeader_SNACK() =
        let pdu =
            {
                Type = SnackReqTypeCd.RDATA_SNACK;
                LUN = lun_me.fromPrim 0x1122334455667788UL;
                InitiatorTaskTag = itt_me.fromPrim 0x11121314u;
                TargetTransferTag = ttt_me.fromPrim 0x21222324u;
                ExpStatSN = statsn_me.fromPrim 0x41424344u;
                BegRun = 0x51525354u;
                RunLength = 0x61626364u;
                ByteCount = 0u;
            }
        let b = PDU.GetHeader( pdu )
        Assert.True(( b =
            [|
                0x10uy; 0x83uy; 0x00uy; 0x00uy; // I,Type
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // TotalAHSLength, DataSegmentLength
                0x77uy; 0x88uy; 0x55uy; 0x66uy; // LUN
                0x33uy; 0x44uy; 0x11uy; 0x22uy;
                0x11uy; 0x12uy; 0x13uy; 0x14uy; // ITT
                0x21uy; 0x22uy; 0x23uy; 0x24uy; // TTT
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x41uy; 0x42uy; 0x43uy; 0x44uy; // ExpStatSN
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x51uy; 0x52uy; 0x53uy; 0x54uy; // BegRun
                0x61uy; 0x62uy; 0x63uy; 0x64uy; // RunLength
            |]
        ))
