//=============================================================================
// Haruka Software Storage.
// MemBufferMediaTest.fs : Test cases for MemBufferMedia class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Media

//=============================================================================
// Import declaration

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test
open Haruka.Media
open Haruka.IODataTypes

//=============================================================================
// Class implementation

type MemBufferMedia_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let wCmdSrc ( k1 : IKiller ) : CommandSourceInfo =
        {
            I_TNexus = new ITNexus( "Initiator", isid_me.zero, "Target", tpgt_me.zero )
            CID = cid_me.zero;
            ConCounter = concnt_me.zero;
            TSIH = tsih_me.zero;
            ProtocolService = new CProtocolService_Stub() :> IProtocolService
            SessionKiller = k1
        }

    let fillBuffer ( b : byte[] ) =
        let r = new Random()
        r.NextBytes b
        b

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore
        
    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constructor_001() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 0UL;
        }
        let k1 = new HKiller() :> IKiller

        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        Assert.True(( mb.BufferLineSize = 4UL * 16UL ))
        Assert.True(( ( mb :> IMedia ).BlockCount = 0UL ))

    [<Fact>]
    member _.Constructor_002() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            BytesCount = 16UL - 1UL;
            MediaName = "";
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        Assert.True(( ( mb :> IMedia ).BlockCount = 0UL ))
        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 0 ))

    [<Fact>]
    member _.Constructor_003() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL;
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        Assert.True(( ( mb :> IMedia ).BlockCount = 1UL ))
        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 1 ))

    [<Fact>]
    member _.Constructor_004() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL;
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        Assert.True(( ( mb :> IMedia ).BlockCount = 4UL ))
        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 1 ))

    [<Fact>]
    member _.Constructor_005() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL + 16UL;
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        Assert.True(( ( mb :> IMedia ).BlockCount = 5UL ))
        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 2 ))

    [<Fact>]
    member _.Constructor_006() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * ( uint64 Array.MaxLength );
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        Assert.True(( ( mb :> IMedia ).BlockCount = 4UL * ( uint64 Array.MaxLength ) ))
        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = Array.MaxLength ))

    [<Fact>]
    member _.Constructor_007() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * ( uint64 Array.MaxLength ) + 16UL;
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        Assert.True(( ( mb :> IMedia ).BlockCount = 4UL * ( uint64 Array.MaxLength ) ))
        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = Array.MaxLength ))

    [<Fact>]
    member _.Terminate_001() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 2UL;
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 2 ))
        ( mb :> IMedia ).Initialize()

        Assert.True(( m_Buffer.[0] <> null ))
        Assert.True(( m_Buffer.[1] <> null ))

        ( mb :> IMedia ).Terminate()

        Assert.True(( m_Buffer.[0] = null ))
        Assert.True(( m_Buffer.[1] = null ))

    [<Fact>]
    member _.Initialize_001() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 2UL;
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 2 ))
        Assert.True(( m_Buffer.[0] = null ))
        Assert.True(( m_Buffer.[1] = null ))

        ( mb :> IMedia ).Initialize()

        Assert.True(( m_Buffer.[0] <> null ))
        Assert.True(( m_Buffer.[1] <> null ))

    [<Fact>]
    member _.Closing_001() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 2UL;
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 2 ))
        ( mb :> IMedia ).Initialize()

        Assert.True(( m_Buffer.[0] <> null ))
        Assert.True(( m_Buffer.[1] <> null ))

        ( mb :> IMedia ).Closing()

        Assert.True(( m_Buffer.[0] = null ))
        Assert.True(( m_Buffer.[1] = null ))

    [<Fact>]
    member _.ReadCapacity_001() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL;
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        let r = ( mb :> IMedia ).ReadCapacity( itt_me.fromPrim 0u ) ( wCmdSrc k1 )
        Assert.True(( r = 4UL ))

    [<Fact>]
    member _.TestUnitReady_001() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        Assert.True(( ( ( mb :> IMedia ).TestUnitReady ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ) = ValueNone ))

    [<Fact>]
    member _.Read_001() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte > 16
        let wr =
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( wr = 16 ))

    [<Fact>]
    member _.Read_002() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte > 17
        try
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Message.StartsWith "Out of media capacity" ))

    [<Fact>]
    member _.Read_003() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL * 10UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 0 )
        let wr =
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 10UL ) ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( wr = 0 ))

    [<Fact>]
    member _.Read_004() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL * 10UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 1 )
        try
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 10UL ) ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Message.StartsWith "Out of media capacity" ))

    [<Fact>]
    member _.Read_005() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL * 10UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 0 )
        try
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 11UL ) ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Message.StartsWith "Out of media capacity" ))

    [<Fact>]
    member _.Read_006() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL * 10UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte > 160
        let wr =
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( wr = 160 ))

    [<Fact>]
    member _.Read_007() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL * 10UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 16 * 10 + 1 )
        try
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Message.StartsWith "Out of media capacity" ))

    [<Fact>]
    member _.Read_008() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 2UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 2 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let rbuf = Array.zeroCreate< byte >( 4 * 16 - 1 )
        let rr =
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment rbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( rr = rbuf.Length ))
        Assert.True(( GlbFunc.Compare wbuf 0 rbuf 0 rbuf.Length ))

    [<Fact>]
    member _.Read_009() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 2UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 2 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let rbuf = Array.zeroCreate< byte >( 4 * 16 )
        let rr =
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment rbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( rr = rbuf.Length ))
        Assert.True(( GlbFunc.Compare wbuf 0 rbuf 0 rbuf.Length ))

    [<Fact>]
    member _.Read_010() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 2UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 2 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let rbuf = Array.zeroCreate< byte >( 4 * 16 + 1 )
        let rr =
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment rbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( rr = rbuf.Length ))
        Assert.True(( GlbFunc.Compare wbuf 0 rbuf 0 rbuf.Length ))

    [<Fact>]
    member _.Read_011() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 3 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let rbuf = Array.zeroCreate< byte >( 4 * 16 * 2 - 1 )
        let rr =
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment rbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( rr = rbuf.Length ))
        Assert.True(( GlbFunc.Compare wbuf 0 rbuf 0 rbuf.Length ))

    [<Fact>]
    member _.Read_012() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 3 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let rbuf = Array.zeroCreate< byte >( 4 * 16 * 2 )
        let rr =
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment rbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( rr = rbuf.Length ))
        Assert.True(( GlbFunc.Compare wbuf 0 rbuf 0 rbuf.Length ))

    [<Fact>]
    member _.Read_013() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 3 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let rbuf = Array.zeroCreate< byte >( 4 * 16 * 2 + 1 )
        let rr =
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) ( ArraySegment rbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( rr = rbuf.Length ))
        Assert.True(( GlbFunc.Compare wbuf 0 rbuf 0 rbuf.Length ))

    [<Fact>]
    member _.Read_014() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 3 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let rbuf = Array.zeroCreate< byte >( 4 * 16 * 2 )
        let rr =
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 1UL ) ( ArraySegment rbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( rr = rbuf.Length ))
        Assert.True(( GlbFunc.Compare wbuf 16 rbuf 0 rbuf.Length ))

    [<Fact>]
    member _.Read_015() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 3 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let rbuf = Array.zeroCreate< byte >( 4 * 16 * 2 )
        let rr =
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 4UL ) ( ArraySegment rbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( rr = rbuf.Length ))
        Assert.True(( GlbFunc.Compare wbuf ( 4 * 16 ) rbuf 0 rbuf.Length ))

    [<Fact>]
    member _.Read_016() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 3 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let rbuf = Array.zeroCreate< byte >( 4 * 16 * 2 )
        let rr =
            ( mb :> IMedia ).Read ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 1UL ) ( ArraySegment( rbuf, 10, rbuf.Length - 10 ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( rr = rbuf.Length - 10 ))
        Assert.True(( GlbFunc.Compare wbuf 16 rbuf 10 ( rbuf.Length - 10 ) ))

    [<Fact>]
    member _.Write_001() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte > 16
        let wr =
            ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( wr = 16 ))

    [<Fact>]
    member _.Write_002() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte > 17
        try
            ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Message.StartsWith "Out of media capacity" ))

    [<Fact>]
    member _.Write_003() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte > 16
        try
            ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 1UL ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Message.StartsWith "Out of media capacity" ))

    [<Fact>]
    member _.Write_004() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL * 10UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 0 )
        let wr =
            ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 10UL ) 0UL ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( wr = 0 ))

    [<Fact>]
    member _.Write_005() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL * 10UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 1 )
        try
            ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 10UL ) 0UL ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Message.StartsWith "Out of media capacity" ))

    [<Fact>]
    member _.Write_006() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL * 10UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 0 )
        try
            ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 11UL ) 0UL ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Message.StartsWith "Out of media capacity" ))

    [<Fact>]
    member _.Write_007() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL * 10UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte > 160
        let wr =
            ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
        Assert.True(( wr = 160 ))

    [<Fact>]
    member _.Write_008() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 16UL * 10UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 16 * 10 + 1 )
        try
            ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL )0UL ( ArraySegment wbuf )
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Message.StartsWith "Out of media capacity" ))

    [<Fact>]
    member _.Write_009() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 2UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 - 1 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 2 ))

        Assert.True(( GlbFunc.Compare wbuf 0 m_Buffer.[0] 0 wbuf.Length ))

    [<Fact>]
    member _.Write_010() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 2UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 2 ))

        Assert.True(( GlbFunc.Compare wbuf 0 m_Buffer.[0] 0 wbuf.Length ))

    [<Fact>]
    member _.Write_011() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 2UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 + 1 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 2 ))

        Assert.True(( GlbFunc.Compare wbuf 0 m_Buffer.[0] 0 ( 4 * 16 ) ))
        Assert.True(( m_Buffer.[1][0] = wbuf.[ 4 * 16 ] ))

    [<Fact>]
    member _.Write_012() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 2 - 1 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 3 ))

        Assert.True(( GlbFunc.Compare wbuf 0 m_Buffer.[0] 0 ( 4 * 16 ) ))
        Assert.True(( GlbFunc.Compare wbuf ( 4 * 16 ) m_Buffer.[1] 0 ( 4 * 16 - 1 ) ))

    [<Fact>]
    member _.Write_013() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 2 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 3 ))

        Assert.True(( GlbFunc.Compare wbuf 0 m_Buffer.[0] 0 ( 4 * 16 ) ))
        Assert.True(( GlbFunc.Compare wbuf ( 4 * 16 ) m_Buffer.[1] 0 ( 4 * 16 ) ))

    [<Fact>]
    member _.Write_014() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 2 + 1 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 3 ))

        Assert.True(( GlbFunc.Compare wbuf 0 m_Buffer.[0] 0 ( 4 * 16 ) ))
        Assert.True(( GlbFunc.Compare wbuf ( 4 * 16 ) m_Buffer.[1] 0 ( 4 * 16 ) ))
        Assert.True(( GlbFunc.Compare wbuf ( 4 * 16 * 2 ) m_Buffer.[2] 0 1 ))

    [<Fact>]
    member _.Write_015() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 2 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 1UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 3 ))

        Assert.True(( GlbFunc.Compare wbuf 0 m_Buffer.[0] 16 ( 4 * 16 - 16 ) ))
        Assert.True(( GlbFunc.Compare wbuf ( 4 * 16 - 16 ) m_Buffer.[1] 0 ( 4 * 16 ) ))
        Assert.True(( GlbFunc.Compare wbuf ( 4 * 16 * 2 - 16 ) m_Buffer.[2] 0 16 ))

    [<Fact>]
    member _.Write_016() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 2 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 7UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 3 ))

        Assert.True(( GlbFunc.Compare wbuf 0 m_Buffer.[0] 7 ( 4 * 16 - 7 ) ))
        Assert.True(( GlbFunc.Compare wbuf ( 4 * 16 - 7 ) m_Buffer.[1] 0 ( 4 * 16 ) ))
        Assert.True(( GlbFunc.Compare wbuf ( 4 * 16 * 2 - 7 ) m_Buffer.[2] 0 7 ))

    [<Fact>]
    member _.Write_017() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 2 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 4UL ) 0UL ( ArraySegment wbuf )
        |> Functions.RunTaskSynchronously
        |> ignore

        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 3 ))

        Assert.True(( GlbFunc.Compare wbuf 0 m_Buffer.[1] 0 ( 4 * 16 ) ))
        Assert.True(( GlbFunc.Compare wbuf ( 4 * 16 ) m_Buffer.[2] 0 ( 4 * 16 ) ))

    [<Fact>]
    member _.Write_018() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf = Array.zeroCreate< byte >( 4 * 16 * 2 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 1UL ) 1UL ( ArraySegment( wbuf, 10, wbuf.Length - 10 ) )
        |> Functions.RunTaskSynchronously
        |> ignore

        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 3 ))

        Assert.True(( GlbFunc.Compare wbuf 10 m_Buffer.[0] 17 ( 4 * 16 - 17 ) ))
        Assert.True(( GlbFunc.Compare wbuf ( 10 + 4 * 16 - 17 ) m_Buffer.[1] 0 ( 4 * 16 ) ))
        Assert.True(( GlbFunc.Compare wbuf ( 10 + 4 * 16 * 2 - 17 ) m_Buffer.[2] 0 7 ))

    [<Fact>]
    member _.Write_019() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 3UL;
        }
        let k1 = new HKiller()
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let wbuf1 = Array.zeroCreate< byte >( 4 * 16 * 3 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 0UL ) 0UL ( ArraySegment wbuf1 )
        |> Functions.RunTaskSynchronously
        |> ignore

        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 3 ))

        Assert.True(( GlbFunc.Compare wbuf1 0 m_Buffer.[0] 0 ( 4 * 16 ) ))
        Assert.True(( GlbFunc.Compare wbuf1 ( 4 * 16 ) m_Buffer.[1] 0 ( 4 * 16 ) ))
        Assert.True(( GlbFunc.Compare wbuf1 ( 4 * 16 * 2 ) m_Buffer.[2] 0 ( 4 * 16 ) ))

        let wbuf2 = Array.zeroCreate< byte >( 4 * 16 ) |> fillBuffer
        ( mb :> IMedia ).Write ( itt_me.fromPrim 0u ) ( wCmdSrc k1 ) ( blkcnt_me.ofUInt64 2UL ) 0UL ( ArraySegment wbuf2 )
        |> Functions.RunTaskSynchronously
        |> ignore

        Assert.True(( GlbFunc.Compare wbuf2 0 m_Buffer.[0] ( 2 * 16 ) ( 2 * 16 ) ))
        Assert.True(( GlbFunc.Compare wbuf2 ( 2 * 16 ) m_Buffer.[1] 0 ( 2 * 16 ) ))

    [<Fact>]
    member _.Format_001() =
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 2UL;
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL )
        ( mb :> IMedia ).Initialize()

        let pr = new PrivateCaller( mb )
        let m_Buffer = pr.GetField( "m_Buffer" ) :?> byte[][]
        Assert.True(( m_Buffer.Length = 2 ))
        Assert.True(( m_Buffer.[0] <> null ))
        Assert.True(( m_Buffer.[1] <> null ))

        let wb0 = m_Buffer.[0]
        let wb1 = m_Buffer.[1]

        ( mb :> IMedia ).Format ( itt_me.fromPrim 0u ) ( wCmdSrc k1 )
        |> Functions.RunTaskSynchronously
        |> ignore

        Assert.True(( m_Buffer.[0] <> null ))
        Assert.True(( m_Buffer.[1] <> null ))
        Assert.False(( Functions.IsSame wb0 m_Buffer.[0] ))
        Assert.False(( Functions.IsSame wb1 m_Buffer.[1] ))
        
    [<Fact>]
    member _.MediaControl_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 2UL;
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL ) :> IMedia

        let r =
            mb.MediaControl( MediaCtrlReq.U_Debug( MediaCtrlReq.U_GetAllTraps() ) )
            |> Functions.RunTaskSynchronously
        match r with
        | MediaCtrlRes.U_Unexpected( x ) ->
            Assert.True(( x.StartsWith "MemBuffer media does not support media controls" ))
        | _ ->
            Assert.Fail __LINE__
        
    [<Fact>]
    member _.GetSubMedia_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let conf : TargetGroupConf.T_MemBuffer = {
            IdentNumber = mediaidx_me.fromPrim 0u;
            MediaName = "";
            BytesCount = 4UL * 16UL * 2UL;
        }
        let k1 = new HKiller() :> IKiller
        let mb = new MemBufferMedia( stat_stub, conf, k1, lun_me.fromPrim 1UL, 4UL, 16UL ) :> IMedia
        Assert.True(( mb.GetSubMedia() = [] ))

