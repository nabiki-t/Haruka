//=============================================================================
// Haruka Software Storage.
// ModeParameterTest.fs : Test cases for ModeParameter class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.BlockDeviceLU

//=============================================================================
// Import declaration

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.BlockDeviceLU
open Haruka.Test

//=============================================================================
// Class implementation

type ModeParameter_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let cmdSource = {
        I_TNexus = new ITNexus( "INIT", isid_me.zero, "TARG", tpgt_me.zero );
        CID = cid_me.fromPrim 0us;
        ConCounter = concnt_me.fromPrim 0;
        TSIH = tsih_me.fromPrim 0us;
        ProtocolService = new CProtocolService_Stub();
        SessionKiller = new HKiller();
    }

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    static member initialMP ( initWP : bool ) =
        new ModeParameter(
            new CMedia_Stub(
                p_GetBlockCount = ( fun _ -> 1024UL ),
                p_GetWriteProtect = ( fun _ -> initWP )
            ),
            lun_me.zero
        )

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Select6_001() =
        let mp = ModeParameter_Test.initialMP false
        try
            mp.Select6 PooledBuffer.Empty 0 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.Select6_002() =
        let mp = ModeParameter_Test.initialMP false
        try
            let v = PooledBuffer.RentAndInit 3
            mp.Select6 v 3 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.Select6_003() =
        let mp = ModeParameter_Test.initialMP false
        try
            let v = PooledBuffer.RentAndInit 4
            mp.Select6 v 3 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.Select6_004() =
        let mp = ModeParameter_Test.initialMP false
        try
            let v = PooledBuffer.RentAndInit 4
            mp.Select6 v 5 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.Select6_004_1() =
        let mp = ModeParameter_Test.initialMP false
        try
            let v = PooledBuffer( Array.zeroCreate 5, 4 )
            mp.Select6 v 5 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.Select6_005() =
        let mp = ModeParameter_Test.initialMP false
        let v = [| 0uy; 1uy; 0uy; 0uy; |] |> PooledBuffer.Rent
        try
            mp.Select6 v v.Count true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.Select6_006() =
        let mp = ModeParameter_Test.initialMP false
        let v = [| 0uy; 0uy; 0uy; 1uy; |] |> PooledBuffer.Rent
        try
            mp.Select6 v v.Count true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.Select6_007() =
        let mp = ModeParameter_Test.initialMP false
        let v = [| 0uy; 0uy; 0uy; 1uy; 0uy; 0uy; |] |> PooledBuffer.Rent
        try
            mp.Select6 v v.Count true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.Select6_008() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0uy; 0uy; 0uy; 8uy;
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
            |]
            |> PooledBuffer.Rent
        Assert.True(( mp.BlockLength = Constants.MEDIA_BLOCK_SIZE ))
        mp.Select6 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

        Assert.True(( mp.BlockLength = 0x0000000000AABBCCUL ))

    [<Fact>]
    member _.Select6_009() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0uy; 0uy; 0uy; 0uy;
            |]
            |> PooledBuffer.Rent
        mp.Select6 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select6_010() =
        let mp = ModeParameter_Test.initialMP false
        let v = 
            [|
                0uy; 0uy; 0uy; 0uy; 1uy;
            |]
            |> PooledBuffer.Rent
        try
            mp.Select6 v v.Count true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.Select6_011() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0uy; 0uy; 0uy; 0uy;
                yield! ( pc.Invoke( "GetCacheModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select6 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select6_012() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0uy; 0uy; 0uy; 0uy;
                yield! ( pc.Invoke( "GetControlModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select6 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select6_013() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0uy; 0uy; 0uy; 0uy;
                yield! ( pc.Invoke( "GetInformationalExceptionsControlModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select6 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select6_014() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0uy; 0uy; 0uy; 0uy;
                yield! ( pc.Invoke( "GetCacheModePage_Current" ) :?> byte[] )
                yield! ( pc.Invoke( "GetControlModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select6 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select6_015() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0uy; 0uy; 0uy; 0uy;
                yield! ( pc.Invoke( "GetControlModePage_Current" ) :?> byte[] )
                yield! ( pc.Invoke( "GetInformationalExceptionsControlModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select6 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select6_016() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0uy; 0uy; 0uy; 0uy;
                yield! ( pc.Invoke( "GetCacheModePage_Current" ) :?> byte[] )
                yield! ( pc.Invoke( "GetControlModePage_Current" ) :?> byte[] )
                yield! ( pc.Invoke( "GetInformationalExceptionsControlModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select6 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select6_017() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0uy; 0uy; 0uy; 8uy;
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
                yield! ( pc.Invoke( "GetInformationalExceptionsControlModePage_Current" ) :?> byte[] )
                yield! ( pc.Invoke( "GetControlModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select6 v 36 true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select6_018() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0uy; 0uy; 0uy; 8uy;
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
                0x00uy; 0x00uy; 0x00uy;
            |]
            |> PooledBuffer.Rent
        mp.Select6 v 15 false true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select6_019() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x08uy;
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // dummy buffer
            |]
        let v2 = PooledBuffer( v, v.Length - 4 )
        Assert.True(( mp.BlockLength = Constants.MEDIA_BLOCK_SIZE ))
        mp.Select6 v2 v2.Count true true cmdSource ( itt_me.fromPrim 0u )
        Assert.True(( mp.BlockLength = 0x0000000000AABBCCUL ))

    [<Fact>]
    member _.ReadCacheModePageByteData_001() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x08uy; 0x12uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy;
            |]
            |> PooledBuffer.Rent
        try
            mp.Select6 v 23 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.ReadCacheModePageByteData_002() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x08uy; 0x11uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            |]
            |> PooledBuffer.Rent
        try
            mp.Select6 v 24 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.ReadCacheModePageByteData_003() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x08uy; 0x12uy; 0x01uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            |]
            |> PooledBuffer.Rent
        try
            mp.Select6 v 24 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.ReadCacheModePageByteData_004() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x08uy; 0x12uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            |]
            |> PooledBuffer.Rent
        mp.Select6 v 24 true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.ReadCacheModePageByteData_005() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x08uy; 0x11uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // dummy buffer
            |]
        let v2 = PooledBuffer( v, v.Length - 4 )
        try
            mp.Select6 v2 24 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.ReadControlModePageByteData_001() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x0Auy; 0x0Auy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy;
            |]
            |> PooledBuffer.Rent
        try
            mp.Select6 v 15 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.ReadControlModePageByteData_002() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x0Auy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            |]
            |> PooledBuffer.Rent
        try
            mp.Select6 v 16 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.ReadControlModePageByteData_003() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x0Auy; 0x0Auy; 0x00uy; 0x10uy; 
                0x08uy; 0x40uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            |]
            |> PooledBuffer.Rent
        Assert.True( mp.D_SENSE )
        Assert.False( mp.SWP )
        mp.Select6 v 16 true true cmdSource ( itt_me.fromPrim 0u )

        Assert.False( mp.D_SENSE )
        Assert.True( mp.SWP )

    [<Fact>]
    member _.ReadControlModePageByteData_004() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x0Auy; 0x0Auy; 0x10uy; 0x10uy; 
                0x28uy; 0x40uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            |]
            |> PooledBuffer.Rent
        Assert.True( mp.D_SENSE )
        Assert.False( mp.SWP )
        try
            mp.Select6 v 16 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

        Assert.True( mp.D_SENSE )
        Assert.False( mp.SWP )

    [<Fact>]
    member _.ReadControlModePageByteData_005() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x0Auy; 0x0Auy; 0x00uy; 0x10uy; 
                0x08uy; 0x40uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // dummy buffer
            |]
        let v2 = PooledBuffer( v, v.Length - 4 )
        Assert.True( mp.D_SENSE )
        Assert.False( mp.SWP )
        mp.Select6 v2 16 true true cmdSource ( itt_me.fromPrim 0u )
        Assert.False( mp.D_SENSE )
        Assert.True( mp.SWP )

    [<Fact>]
    member _.ReadInformationalExceptionsControlModePageByteData_001() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [| 
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x1Cuy; 0x0Auy; 0x39uy; 0x02uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy;
            |]
            |> PooledBuffer.Rent
        try
            mp.Select6 v 15 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.ReadInformationalExceptionsControlModePageByteData_002() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x1Cuy; 0x0Auy; 0x39uy; 0x02uy; 
                0x10uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
            |> PooledBuffer.Rent
        try
            mp.Select6 v 16 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.ReadInformationalExceptionsControlModePageByteData_003() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x1Cuy; 0x0Auy; 0x39uy; 0x02uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
            |> PooledBuffer.Rent
        mp.Select6 v 16 true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.ReadInformationalExceptionsControlModePageByteData_004() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [| 
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x1Cuy; 0x0Auy; 0x39uy; 0x02uy; 
                0x10uy; 0x00uy; 0x00uy; 0x00uy; 
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // dummy buffer
            |]
        let v2 = PooledBuffer( v, v.Length - 4 )
        try
            mp.Select6 v2 16 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.Select10_001() =
        let mp = ModeParameter_Test.initialMP false
        try
            mp.Select10 PooledBuffer.Empty 0 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.Select10_002() =
        let mp = ModeParameter_Test.initialMP false
        try
            let v = PooledBuffer.RentAndInit 7
            mp.Select10 v 7 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.Select10_003() =
        let mp = ModeParameter_Test.initialMP false
        try
            let v = PooledBuffer.RentAndInit 8
            mp.Select10 v 7 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.Select10_004() =
        let mp = ModeParameter_Test.initialMP false
        try
            let v = PooledBuffer.RentAndInit 8
            mp.Select10 v 9 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.Select10_004_1() =
        let mp = ModeParameter_Test.initialMP false
        try
            let v = PooledBuffer( Array.zeroCreate 10, 8 )
            mp.Select10 v 10 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.PARAMETER_LIST_LENGTH_ERROR ))

    [<Fact>]
    member _.Select10_005() =
        let mp = ModeParameter_Test.initialMP false
        let v = [| 0uy; 0uy; 1uy; 0uy; 0uy; 0uy; 0uy; 0uy; |] |> PooledBuffer.Rent
        try
            mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.Select10_006() =
        let mp = ModeParameter_Test.initialMP false
        let v = [| 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 1uy; |] |> PooledBuffer.Rent
        try
            mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.Select10_007() =
        let mp = ModeParameter_Test.initialMP false
        let v = [| 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 1uy; 0uy; 0uy; |] |> PooledBuffer.Rent
        try
            mp.Select10 v 9 true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.Select10_008() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
            |]
            |> PooledBuffer.Rent
        try
            mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.Select10_009() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                0x11uy; 0x22uy; 0x33uy; 0x44uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
            |]
            |> PooledBuffer.Rent
        try
            mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.Select10_010() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
            |]
            |> PooledBuffer.Rent
        Assert.True(( mp.BlockLength = Constants.MEDIA_BLOCK_SIZE ))
        mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )
        Assert.True(( mp.BlockLength = 0x0000000000AABBCCUL ))

    [<Fact>]
    member _.Select10_011() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                0x11uy; 0x22uy; 0x33uy; 0x44uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                0xFFuy; 0xEEuy; 0xDDuy; 0xCCuy; // BLOCK LENGTH
            |]
            |> PooledBuffer.Rent
        Assert.True(( mp.BlockLength = Constants.MEDIA_BLOCK_SIZE ))
        mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )
        Assert.True(( mp.BlockLength = 0x00000000FFEEDDCCUL ))

    [<Fact>]
    member _.Select10_012() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
            |> PooledBuffer.Rent
        mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select10_013() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x01uy;
            |]
            |> PooledBuffer.Rent
        try
            mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

    [<Fact>]
    member _.Select10_014() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                yield! ( pc.Invoke( "GetCacheModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select10_015() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                yield! ( pc.Invoke( "GetControlModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select10_016() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                yield! ( pc.Invoke( "GetInformationalExceptionsControlModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select10_017() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                yield! ( pc.Invoke( "GetCacheModePage_Current" ) :?> byte[] )
                yield! ( pc.Invoke( "GetControlModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select10_018() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                yield! ( pc.Invoke( "GetControlModePage_Current" ) :?> byte[] )
                yield! ( pc.Invoke( "GetInformationalExceptionsControlModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select10_019() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x08uy;
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
                yield! ( pc.Invoke( "GetInformationalExceptionsControlModePage_Current" ) :?> byte[] )
                yield! ( pc.Invoke( "GetControlModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select10_020() =
        let mp = ModeParameter_Test.initialMP false
        let pc = new PrivateCaller( mp )
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x08uy;
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
                yield! ( pc.Invoke( "GetCacheModePage_Current" ) :?> byte[] )
                yield! ( pc.Invoke( "GetInformationalExceptionsControlModePage_Current" ) :?> byte[] )
                yield! ( pc.Invoke( "GetControlModePage_Current" ) :?> byte[] )
            |]
            |> PooledBuffer.Rent
        mp.Select10 v v.Count true true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select10_021() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x08uy;
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
                0x00uy; 0x00uy; 0x00uy;
            |]
            |> PooledBuffer.Rent
        mp.Select10 v v.Count false true cmdSource ( itt_me.fromPrim 0u )

    [<Fact>]
    member _.Select10_022() =
        let mp = ModeParameter_Test.initialMP false
        let v =
            [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
                0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH
                0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // dummy buffer
            |]
        let v2 = PooledBuffer( v, v.Length - 4 )
        Assert.True(( mp.BlockLength = Constants.MEDIA_BLOCK_SIZE ))
        mp.Select10 v2 v2.Count true true cmdSource ( itt_me.fromPrim 0u )
        Assert.True(( mp.BlockLength = 0x0000000000AABBCCUL ))

    [<Fact>]
    member _.Sense6_001() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 false 0x08uy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x1Fuy; 0x00uy; 0x00uy; 0x08uy;
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_002() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 true 0x08uy 0xFFuy 0x03uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x17uy; 0x00uy; 0x00uy; 0x00uy;

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_003() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 false 0x0Auy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x17uy; 0x00uy; 0x00uy; 0x08uy;
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_004() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 true 0x0Auy 0xFFuy 0x03uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x0Fuy; 0x00uy; 0x00uy; 0x00uy;
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_005() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 false 0x1Cuy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x17uy; 0x00uy; 0x00uy; 0x08uy;
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_006() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 true 0x1Cuy 0xFFuy 0x03uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x0Fuy; 0x00uy; 0x00uy; 0x00uy;
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_007() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 false 0x3Fuy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x37uy; 0x00uy; 0x00uy; 0x08uy;
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_008() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 true 0x3Fuy 0xFFuy 0x03uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x2Fuy; 0x00uy; 0x00uy; 0x00uy;

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_009() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 false 0x08uy 0x00uy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x1Fuy; 0x00uy; 0x00uy; 0x08uy;
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_010() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 true 0x08uy 0xFFuy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x17uy; 0x00uy; 0x00uy; 0x00uy;
            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_011() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 false 0x0Auy 0x00uy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x17uy; 0x00uy; 0x00uy; 0x08uy;
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x00uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x00uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_012() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 true 0x0Auy 0xFFuy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x0Fuy; 0x00uy; 0x00uy; 0x00uy;
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x00uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x00uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_013() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 false 0x1Cuy 0x00uy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x17uy; 0x00uy; 0x00uy; 0x08uy;
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x00uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_014() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 true 0x1Cuy 0xFFuy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x0Fuy; 0x00uy; 0x00uy; 0x00uy;
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x00uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_015() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 false 0x3Fuy 0x00uy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x37uy; 0x00uy; 0x00uy; 0x08uy;
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x00uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x00uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x00uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_016() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 true 0x3Fuy 0xFFuy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x2Fuy; 0x00uy; 0x00uy; 0x00uy;

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x00uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x00uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x00uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_017() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense6 false 0x08uy 0x00uy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x1Fuy; 0x00uy; 0x80uy; 0x08uy;
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_018() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 true 0x08uy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x17uy; 0x00uy; 0x00uy; 0x00uy;
            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_019() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense6 false 0x0Auy 0x00uy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x17uy; 0x00uy; 0x80uy; 0x08uy;
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_020() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 true 0x0Auy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x0Fuy; 0x00uy; 0x00uy; 0x00uy;
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_021() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 false 0x1Cuy 0x00uy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x17uy; 0x00uy; 0x00uy; 0x08uy;
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_022() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 true 0x1Cuy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x0Fuy; 0x00uy; 0x00uy; 0x00uy;
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_023() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 false 0x3Fuy 0x00uy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x37uy; 0x00uy; 0x00uy; 0x08uy;
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_024() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense6 true 0x3Fuy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x2Fuy; 0x00uy; 0x00uy; 0x00uy;

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense6_025() =
        let mp = ModeParameter_Test.initialMP false
        try
            let _ = mp.Sense6 true 0xFFuy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

    [<Fact>]
    member _.Sense6_026() =
        let mp = ModeParameter_Test.initialMP false
        try
            let _ = mp.Sense6 true 0x0Auy 0x11uy 0x02uy cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

    [<Fact>]
    member _.Sense6_027() =
        let mp = ModeParameter_Test.initialMP false
        try
            let _ = mp.Sense6 true 0x0Auy 0x00uy 0x04uy cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

    [<Fact>]
    member _.Sense10_001() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false false 0x08uy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x22uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_002() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false true 0x08uy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x1Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_003() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true false 0x08uy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x2Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_004() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true true 0x08uy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x1Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_005() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense10 true false 0x08uy 0xFFuy 0x03uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x2Auy; 0x00uy; 0x00uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_006() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false false 0x0Auy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x1Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_007() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false true 0x0Auy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x12uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_008() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true false 0x0Auy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x22uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_009() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true true 0x0Auy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x12uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_010() =
        let mp = ModeParameter_Test.initialMP false
        let v1 = mp.Sense10 true false 0x0Auy 0xFFuy 0x03uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x22uy; 0x00uy; 0x00uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_011() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true false 0x1Cuy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x22uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_012() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false false 0x1Cuy 0xFFuy 0x03uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x1Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_013() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false true 0x1Cuy 0xFFuy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x12uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_014() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true false 0x3Fuy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x42uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_015() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false false 0x3Fuy 0xFFuy 0x03uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x3Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_016() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false true 0x3Fuy 0xFFuy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x32uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_017() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true false 0x08uy 0x00uy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x2Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_018() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false false 0x08uy 0xFFuy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x22uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_019() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false true 0x08uy 0xFFuy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x1Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_020() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true false 0x0Auy 0x00uy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x22uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x00uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x00uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_021() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false false 0x0Auy 0xFFuy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x1Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x00uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x00uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_022() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false true 0x0Auy 0xFFuy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x12uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x00uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x00uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_023() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true false 0x1Cuy 0x00uy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x22uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x00uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_024() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false false 0x1Cuy 0xFFuy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x1Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x00uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_025() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false true 0x1Cuy 0xFFuy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x12uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x00uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_026() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true false 0x3Fuy 0x00uy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x42uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x00uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x00uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x00uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_027() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false false 0x3Fuy 0xFFuy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x3Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x00uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x00uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x00uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_028() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false true 0x3Fuy 0xFFuy 0x01uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x32uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x00uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x00uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x00uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_029() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true false 0x08uy 0x00uy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x2Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_030() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false false 0x08uy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x22uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_031() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false true 0x08uy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x1Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_032() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true false 0x0Auy 0x00uy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x22uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_033() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false false 0x0Auy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x1Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_034() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false true 0x0Auy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x12uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_035() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true false 0x1Cuy 0x00uy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x22uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_036() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false false 0x1Cuy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x1Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_037() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false true 0x1Cuy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x12uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_038() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 true false 0x3Fuy 0x00uy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x42uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_039() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false false 0x3Fuy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x3Auy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )  // BLOCK SIZE

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_040() =
        let mp = ModeParameter_Test.initialMP true
        let v1 = mp.Sense10 false true 0x3Fuy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x32uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x04uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x00uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Sense10_041() =
        let mp = ModeParameter_Test.initialMP false
        try
            let v1 = mp.Sense10 true true 0xFFuy 0xFFuy 0x02uy cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

    [<Fact>]
    member _.Sense10_042() =
        let mp = ModeParameter_Test.initialMP false
        try
            let v1 = mp.Sense10 true true 0x0Auy 0x11uy 0x02uy cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

    [<Fact>]
    member _.Sense10_043() =
        let mp = ModeParameter_Test.initialMP false
        try
            let v1 = mp.Sense10 true true 0x0Auy 0x00uy 0x04uy cmdSource ( itt_me.fromPrim 0u )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseData.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

    [<Fact>]
    member _.Update_6_001() =
        let mp = ModeParameter_Test.initialMP false
        Assert.True(( mp.BlockLength = Constants.MEDIA_BLOCK_SIZE ))
        Assert.True(( mp.D_SENSE ))
        Assert.False(( mp.SWP ))
        let v = [|
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER, BLOCK DESCRIPTOR LENGTH
            0x11uy; 0x22uy; 0x33uy; 0x44uy; // NUMBER OF BLOCKS
            0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // dummy buffer
        |]
        let v2 = PooledBuffer( v, v.Length - 4 )
        mp.Select6 v2 v2.Count true true cmdSource ( itt_me.fromPrim 0u )

        Assert.True(( mp.BlockLength = 0x00AABBCCUL ))
        Assert.False(( mp.D_SENSE ))
        Assert.True(( mp.SWP ))

        let v1 = mp.Sense6 false 0x3Fuy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x37uy; 0x00uy; 0x80uy; 0x08uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x04uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // BLOCK LENGTH

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))

    [<Fact>]
    member _.Update_10_002() =
        let mp = ModeParameter_Test.initialMP false
        Assert.True(( mp.BlockLength = Constants.MEDIA_BLOCK_SIZE ))
        Assert.True(( mp.D_SENSE ))
        Assert.False(( mp.SWP ))
        let v = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // NUMBER OF BLOCKS
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x12uy; 0x34uy; 0x56uy; 0x78uy; // BLOCK LENGTH

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // dummy buffer
        |]
        let v2 = PooledBuffer( v, v.Length - 4 )
        mp.Select10 v2 v2.Count true true cmdSource ( itt_me.fromPrim 0u )

        Assert.True(( mp.BlockLength = 0x12345678UL ))
        Assert.False(( mp.D_SENSE ))
        Assert.True(( mp.SWP ))

        let v1 = mp.Sense10 true false 0x3Fuy 0x00uy 0x00uy cmdSource ( itt_me.fromPrim 0u )
        let v2 = [|
            0x00uy; 0x42uy; 0x00uy; 0x80uy; // MODE DATA LENGTH, MEDIUM TYPE, DEVICE-SPECIFIC PARAMETER
            0x01uy; 0x00uy; 0x00uy; 0x10uy; // LONGLBA, BLOCK DESCRIPTOR LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // BLOCK COUNT
            0x00uy; 0x00uy; 0x04uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x12uy; 0x34uy; 0x56uy; 0x78uy; // BLOCK LENGTH

            0x08uy;                         // PS, SPF, PAGE CODE
            0x12uy;                         // PAGE LENGTH
            0x00uy;                         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;                         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy;                 // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy;                 // MINIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy;                 // MAXIMUM PRE-FETCH CEILING
            0x00uy;                         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;                         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy;                 // CACHE SEGMENT SIZE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved

            0x0Auy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x00uy;                         // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x10uy;                         // QUEUE ALGORITHM MODIFIER, QERR
            0x08uy;                         // RAC, UA_INTLCK_CTRL, SWP
            0x40uy;                         // ATO, TAS, AUTOLOAD MODE(ignore)
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy;                 // BUSY TIMEOUT PERIOD
            0x00uy; 0x00uy;                 // EXTENDED SELF-TEST COMPLETION TIME

            0x1Cuy;                         // PS, SPF, PAGE CODE
            0x0Auy;                         // PAGE LENGTH
            0x39uy;                         // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x02uy;                         // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // REPORT COUNT
        |]
        Assert.True(( v1 = v2 ))
