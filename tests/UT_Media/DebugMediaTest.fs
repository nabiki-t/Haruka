//=============================================================================
// Haruka Software Storage.
// DebugMediaTest.fs : Test cases for DebugMedia class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Media

//=============================================================================
// Import declaration

open System
open System.Threading.Tasks

open Xunit

open Haruka.Constants
open Haruka.IODataTypes
open Haruka.Commons
open Haruka.Test
open Haruka.Media

//=============================================================================
// Class implementation

type DebugMedia_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let defaultCommandSource = {
        I_TNexus = new ITNexus( "initiator", isid_me.zero, "target", tpgt_me.zero )
        CID = cid_me.zero;
        ConCounter = concnt_me.zero;
        TSIH = tsih_me.zero;
        ProtocolService = new CProtocolService_Stub() :> IProtocolService
        SessionKiller = new HKiller()
    }

    let defaultConf : TargetGroupConf.T_DebugMedia = {
        IdentNumber = mediaidx_me.fromPrim 1u;
        MediaName = "";
        Peripheral = TargetGroupConf.U_DummyMedia({
            IdentNumber = mediaidx_me.fromPrim 2u;
            MediaName = "";
        });
    }

    let AddCounterAction ( num : int ) ( a : MediaCtrlReq.T_Event ) ( media : IMedia ) =
        let r1 =
            media.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = a;
                        Action = MediaCtrlReq.U_Count( num );
                    })
                )
            )
            |> Functions.RunTaskSynchronously
        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__


    let GetAllTraps( media : IMedia ) =
        let r1 =
            media.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_GetAllTraps()
                )
            )
            |> Functions.RunTaskSynchronously

        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AllTraps( y ) ->
                y.Trap
            | _ ->
                Assert.Fail __LINE__
                []
        | _ ->
            Assert.Fail __LINE__
            []

    let GetCounterValue ( num : int ) ( media : IMedia ) =
        let r1 =
            media.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_GetCounterValue ( num )
                )
            )
            |> Functions.RunTaskSynchronously

        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_CounterValue( y ) ->
                y
            | _ ->
                Assert.Fail __LINE__
                -2
        | _ ->
            Assert.Fail __LINE__
            -2


    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constructor_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k ->
            match c with
            | TargetGroupConf.U_DummyMedia( x ) ->
                Assert.True(( x.IdentNumber = mediaidx_me.fromPrim 2u ))
            | _ ->
                Assert.Fail __LINE__
            Assert.True(( lun = lun_me.fromPrim 1UL ))
            stub_media
        )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let subMedia = dm.GetSubMedia()
        Assert.True(( subMedia.Length = 1 ))
        Assert.True(( Functions.IsSame subMedia.Head stub_media ))

    [<Fact>]
    member _.Terminate_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> new CMedia_Stub() )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        dm.Terminate()

    [<Fact>]
    member _.Initialize_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Initialize <- ( fun _ ->
            cnt <- cnt + 1
        )

        AddCounterAction 1 ( MediaCtrlReq.U_TestUnitReady() ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        dm.Initialize()
        Assert.True(( cnt = 1 ))

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 0 ))

    [<Fact>]
    member _.Closing_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Closing <- ( fun _ ->
            cnt <- cnt + 1
        )

        AddCounterAction 1 ( MediaCtrlReq.U_TestUnitReady() ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        dm.Closing()
        Assert.True(( cnt = 1 ))

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 0 ))

    [<Fact>]
    member _.TestUnitReady_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_TestUnitReady <- ( fun itt src ->
            Assert.True(( itt = itt_me.fromPrim 99u ))
            Assert.True(( Functions.IsSame src defaultCommandSource ))
            cnt <- cnt + 1
            ValueNone
        )

        AddCounterAction 1 ( MediaCtrlReq.U_TestUnitReady() ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 = dm.TestUnitReady ( itt_me.fromPrim 99u ) defaultCommandSource
        Assert.True(( r2 = ValueNone ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 1 ))

    [<Fact>]
    member _.TestUnitReady_002() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_TestUnitReady <- ( fun itt src ->
            Assert.True(( itt = itt_me.fromPrim 99u ))
            Assert.True(( Functions.IsSame src defaultCommandSource ))
            cnt <- cnt + 1
            ValueNone
        )

        AddCounterAction 1 ( MediaCtrlReq.U_Format() ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 = dm.TestUnitReady ( itt_me.fromPrim 99u ) defaultCommandSource
        Assert.True(( r2 = ValueNone ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 0 ))

    [<Fact>]
    member _.TestUnitReady_003() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_TestUnitReady <- ( fun itt src ->
            Assert.True(( itt = itt_me.fromPrim 99u ))
            Assert.True(( Functions.IsSame src defaultCommandSource ))
            cnt <- cnt + 1
            ValueNone
        )

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            AddCounterAction i ( MediaCtrlReq.U_TestUnitReady() ) dm

        let r2 = dm.TestUnitReady ( itt_me.fromPrim 99u ) defaultCommandSource
        Assert.True(( r2 = ValueNone ))
        Assert.True(( cnt = 1 ))

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            let r4 = GetCounterValue i dm
            Assert.True(( r4 = 1 ))

    [<Fact>]
    member _.TestUnitReady_004() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_TestUnitReady <- ( fun itt src ->
            Assert.True(( itt = itt_me.fromPrim 99u ))
            Assert.True(( Functions.IsSame src defaultCommandSource ))
            cnt <- cnt + 1
            ValueNone
        )

        // Add delay action
        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_TestUnitReady();
                        Action = MediaCtrlReq.U_Delay( 10000 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously
        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let startTime = Environment.TickCount64
        let r2 = dm.TestUnitReady ( itt_me.fromPrim 99u ) defaultCommandSource
        let endTime = Environment.TickCount64
        Assert.True(( endTime - startTime < 5000 ))

        Assert.True(( r2 = ValueNone ))
        Assert.True(( cnt = 1 ))


    [<Fact>]
    member _.ReadCapacity_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_ReadCapacity <- ( fun itt src ->
            Assert.True(( itt = itt_me.fromPrim 99u ))
            Assert.True(( Functions.IsSame src defaultCommandSource ))
            cnt <- cnt + 1
            123UL
        )

        AddCounterAction 1 ( MediaCtrlReq.U_ReadCapacity() ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 = dm.ReadCapacity ( itt_me.fromPrim 99u ) defaultCommandSource
        Assert.True(( r2 = 123UL ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 1 ))

    [<Fact>]
    member _.ReadCapacity_002() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_ReadCapacity <- ( fun itt src ->
            Assert.True(( itt = itt_me.fromPrim 99u ))
            Assert.True(( Functions.IsSame src defaultCommandSource ))
            cnt <- cnt + 1
            123UL
        )

        AddCounterAction 1 ( MediaCtrlReq.U_TestUnitReady() ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 = dm.ReadCapacity ( itt_me.fromPrim 99u ) defaultCommandSource
        Assert.True(( r2 = 123UL ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 0 ))

    [<Fact>]
    member _.ReadCapacity_003() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_ReadCapacity <- ( fun itt src ->
            Assert.True(( itt = itt_me.fromPrim 99u ))
            Assert.True(( Functions.IsSame src defaultCommandSource ))
            cnt <- cnt + 1
            123UL
        )

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            AddCounterAction i ( MediaCtrlReq.U_ReadCapacity() ) dm

        let r2 = dm.ReadCapacity ( itt_me.fromPrim 99u ) defaultCommandSource
        Assert.True(( r2 = 123UL ))
        Assert.True(( cnt = 1 ))

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            let r4 = GetCounterValue i dm
            Assert.True(( r4 = 1 ))

    [<Fact>]
    member _.ReadCapacity_004() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_ReadCapacity <- ( fun itt src ->
            Assert.True(( itt = itt_me.fromPrim 99u ))
            Assert.True(( Functions.IsSame src defaultCommandSource ))
            cnt <- cnt + 1
            123UL
        )

        // Add delay action
        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_ReadCapacity();
                        Action = MediaCtrlReq.U_Delay( 10000 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously
        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let startTime = Environment.TickCount64
        let r2 = dm.ReadCapacity ( itt_me.fromPrim 99u ) defaultCommandSource
        let endTime = Environment.TickCount64
        Assert.True(( endTime - startTime < 5000 ))

        Assert.True(( r2 = 123UL ))
        Assert.True(( cnt = 1 ))

    [<Fact>]
    member _.Read_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Read <- ( fun itt src lba buf ->
            Assert.True(( itt = itt_me.fromPrim 99u ))
            Assert.True(( Functions.IsSame src defaultCommandSource ))
            cnt <- cnt + 1
            Task.FromResult 112
        )

        AddCounterAction 1 ( MediaCtrlReq.U_Read({ StartLBA = 10UL; EndLBA = 20UL; }) ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 =
            let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 10 )
            dm.Read ( itt_me.fromPrim 99u ) defaultCommandSource 0UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 0 ))

    [<Fact>]
    member _.Read_002() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Read <- ( fun itt src lba buf ->
            cnt <- cnt + 1
            Task.FromResult 112
        )

        AddCounterAction 1 ( MediaCtrlReq.U_Read({ StartLBA = 10UL; EndLBA = 20UL; }) ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 =
            let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 11 )
            dm.Read ( itt_me.fromPrim 99u ) defaultCommandSource 10UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 1 ))

    [<Fact>]
    member _.Read_003() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Read <- ( fun itt src lba buf ->
            cnt <- cnt + 1
            Task.FromResult 112
        )

        AddCounterAction 1 ( MediaCtrlReq.U_Read({ StartLBA = 10UL; EndLBA = 20UL; }) ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 =
            let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 1 )
            dm.Read ( itt_me.fromPrim 99u ) defaultCommandSource 20UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 1 ))

    [<Fact>]
    member _.Read_004() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Read <- ( fun itt src lba buf ->
            cnt <- cnt + 1
            Task.FromResult 112
        )

        AddCounterAction 1 ( MediaCtrlReq.U_Read({ StartLBA = 10UL; EndLBA = 20UL; }) ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 =
            let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 1 )
            dm.Read ( itt_me.fromPrim 99u ) defaultCommandSource 21UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 0 ))

    [<Fact>]
    member _.Read_005() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Read <- ( fun itt src lba buf ->
            cnt <- cnt + 1
            Task.FromResult 112
        )

        AddCounterAction 1 ( MediaCtrlReq.U_TestUnitReady() ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 =
            let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 1 )
            dm.Read ( itt_me.fromPrim 99u ) defaultCommandSource 0UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 0 ))

    [<Fact>]
    member _.Read_006() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Read <- ( fun itt src lba buf ->
            cnt <- cnt + 1
            Task.FromResult 112
        )

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            AddCounterAction i ( MediaCtrlReq.U_Read({ StartLBA = 10UL; EndLBA = 20UL; }) ) dm

        let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 1 )
        let r2 =
            dm.Read ( itt_me.fromPrim 99u ) defaultCommandSource 10UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            let r4 = GetCounterValue i dm
            Assert.True(( r4 = 1 ))

    [<Fact>]
    member _.Read_007() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Read <- ( fun itt src lba buf ->
            cnt <- cnt + 1
            Task.FromResult 112
        )

        // Add delay action
        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Read( { StartLBA = 0UL; EndLBA = UInt64.MaxValue; } );
                        Action = MediaCtrlReq.U_Delay( 1000 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously
        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let startTime = Environment.TickCount64
        let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 1 )
        let r2 =
            dm.Read ( itt_me.fromPrim 99u ) defaultCommandSource 10UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        let endTime = Environment.TickCount64
        Assert.True(( endTime - startTime >= 1000 ))

        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

    [<Fact>]
    member _.Write_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Write <- ( fun itt src lba off buf ->
            Assert.True(( itt = itt_me.fromPrim 99u ))
            Assert.True(( Functions.IsSame src defaultCommandSource ))
            cnt <- cnt + 1
            Task.FromResult 112
        )

        AddCounterAction 1 ( MediaCtrlReq.U_Write({ StartLBA = 10UL; EndLBA = 20UL; }) ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 =
            let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 10 )
            dm.Write ( itt_me.fromPrim 99u ) defaultCommandSource 0UL 0UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 0 ))

    [<Fact>]
    member _.Write_002() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Write <- ( fun itt src lba off buf ->
            cnt <- cnt + 1
            Task.FromResult 112
        )

        AddCounterAction 1 ( MediaCtrlReq.U_Write({ StartLBA = 10UL; EndLBA = 20UL; }) ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 =
            let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 11 )
            dm.Write ( itt_me.fromPrim 99u ) defaultCommandSource 0UL 0UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 1 ))

    [<Fact>]
    member _.Write_003() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Write <- ( fun itt src lba off buf ->
            cnt <- cnt + 1
            Task.FromResult 112
        )

        AddCounterAction 1 ( MediaCtrlReq.U_Write({ StartLBA = 10UL; EndLBA = 20UL; }) ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 =
            let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 1 )
            dm.Write ( itt_me.fromPrim 99u ) defaultCommandSource 20UL 0UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 1 ))

    [<Fact>]
    member _.Write_004() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Write <- ( fun itt src lba off buf ->
            cnt <- cnt + 1
            Task.FromResult 112
        )

        AddCounterAction 1 ( MediaCtrlReq.U_Write({ StartLBA = 10UL; EndLBA = 20UL; }) ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 =
            let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 1 )
            dm.Write ( itt_me.fromPrim 99u ) defaultCommandSource 21UL 0UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 0 ))

    [<Fact>]
    member _.Write_005() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Write <- ( fun itt src lba off buf ->
            cnt <- cnt + 1
            Task.FromResult 112
        )

        AddCounterAction 1 ( MediaCtrlReq.U_TestUnitReady() ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 =
            let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 1 )
            dm.Write ( itt_me.fromPrim 99u ) defaultCommandSource 0UL 0UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 0 ))

    [<Fact>]
    member _.Write_006() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Write <- ( fun itt src lba off buf ->
            cnt <- cnt + 1
            Task.FromResult 112
        )

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            AddCounterAction i ( MediaCtrlReq.U_Write({ StartLBA = 10UL; EndLBA = 20UL; }) ) dm

        let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 10 )
        let r2 =
            dm.Write ( itt_me.fromPrim 99u ) defaultCommandSource 10UL 0UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously
        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            let r4 = GetCounterValue i dm
            Assert.True(( r4 = 1 ))

    [<Fact>]
    member _.Write_007() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Write <- ( fun itt src lba off buf ->
            cnt <- cnt + 1
            Task.FromResult 112
        )

        // Add delay action
        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Write( { StartLBA = 0UL; EndLBA = UInt64.MaxValue; } );
                        Action = MediaCtrlReq.U_Delay( 1000 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously
        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let startTime = Environment.TickCount64
        let buf = Array.zeroCreate< byte >( int Constants.MEDIA_BLOCK_SIZE * 10 )
        let r2 =
            dm.Write ( itt_me.fromPrim 99u ) defaultCommandSource 10UL 0UL ( ArraySegment( buf, 0, buf.Length ) )
            |> Functions.RunTaskSynchronously

        let endTime = Environment.TickCount64
        Assert.True(( endTime - startTime >= 1000 ))

        Assert.True(( r2 = 112 ))
        Assert.True(( cnt = 1 ))

    [<Fact>]
    member _.Format_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Format<- ( fun itt src ->
            Assert.True(( itt = itt_me.fromPrim 99u ))
            Assert.True(( Functions.IsSame src defaultCommandSource ))
            cnt <- cnt + 1
            Task.FromResult ()
        )

        AddCounterAction 1 ( MediaCtrlReq.U_Format() ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        dm.Format ( itt_me.fromPrim 99u ) defaultCommandSource
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 1 ))

    [<Fact>]
    member _.Format_002() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Format<- ( fun itt src ->
            cnt <- cnt + 1
            Task.FromResult ()
        )

        AddCounterAction 1 ( MediaCtrlReq.U_TestUnitReady() ) dm
        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        dm.Format ( itt_me.fromPrim 99u ) defaultCommandSource
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt = 1 ))

        let r3 = GetAllTraps dm
        Assert.True(( r3.Length = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 0 ))

    [<Fact>]
    member _.Format_003() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Format<- ( fun itt src ->
            cnt <- cnt + 1
            Task.FromResult ()
        )

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            AddCounterAction i ( MediaCtrlReq.U_Format() ) dm

        dm.Format ( itt_me.fromPrim 99u ) defaultCommandSource
        |> Functions.RunTaskSynchronously
        Assert.True(( cnt = 1 ))

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            let r4 = GetCounterValue i dm
            Assert.True(( r4 = 1 ))

    [<Fact>]
    member _.Format_004() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Format<- ( fun itt src ->
            cnt <- cnt + 1
            Task.FromResult ()
        )

        // Add delay action
        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Format();
                        Action = MediaCtrlReq.U_Delay( 1000 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously
        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let startTime = Environment.TickCount64
        dm.Format ( itt_me.fromPrim 99u ) defaultCommandSource
        |> Functions.RunTaskSynchronously
        let endTime = Environment.TickCount64
        Assert.True(( endTime - startTime >= 1000 ))

        Assert.True(( cnt = 1 ))

    [<Fact>]
    member _.NotifyLUReset_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_NotifyLUReset <- ( fun itt src ->
            cnt <- cnt + 1
        )

        AddCounterAction 1 ( MediaCtrlReq.U_Format() ) dm

        dm.NotifyLUReset ValueNone ValueNone
        Assert.True(( cnt = 1 ))

        let r4 = GetCounterValue 1 dm
        Assert.True(( r4 = 0 ))

    [<Fact>]
    member _.MediaControl_GetAllTraps_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 0 ))


    [<Fact>]
    member _.MediaControl_GetAllTraps_002() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        AddCounterAction 1 ( MediaCtrlReq.U_Format() ) dm

        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))
        Assert.True(( r1.[0].Event.IsU_Format ))
        match r1.[0].Action with
        | MediaCtrlRes.U_Count( z ) ->
            Assert.True(( z.Index = 1 ))
            Assert.True(( z.Value = 0 ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_GetAllTraps_003() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            AddCounterAction i ( MediaCtrlReq.U_Format() ) dm

        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = Constants.DEBUG_MEDIA_MAX_TRAP_COUNT ))

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            Assert.True(( r1.[ i - 1 ].Event.IsU_Format ))
            match r1.[ i - 1 ].Action with
            | MediaCtrlRes.U_Count( z ) ->
                Assert.True(( z.Index = i ))
                Assert.True(( z.Value = 0 ))
            | _ ->
                Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_GetAllTraps_Event_TestUnitReady() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        AddCounterAction 1 ( MediaCtrlReq.U_TestUnitReady() ) dm
            
        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))
        Assert.True (( r2.[0].Event.IsU_TestUnitReady ))

    [<Fact>]
    member _.MediaControl_GetAllTraps_Event_ReadCapacity() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        AddCounterAction 1 ( MediaCtrlReq.U_ReadCapacity() ) dm

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))
        Assert.True (( r2.[0].Event.IsU_ReadCapacity ))

    [<Fact>]
    member _.MediaControl_GetAllTraps_Event_Read() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        AddCounterAction 1 ( MediaCtrlReq.U_Read({ StartLBA = 0UL; EndLBA = 1UL; }) ) dm

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))

        match r2.[0].Event with
        | MediaCtrlRes.U_Read( z ) ->
            Assert.True(( z.StartLBA = 0UL ))
            Assert.True(( z.EndLBA = 1UL ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_GetAllTraps_Event_Write() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        AddCounterAction 1 ( MediaCtrlReq.U_Write({ StartLBA = 0UL; EndLBA = 1UL; }) ) dm

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))
        match r2.[0].Event with
        | MediaCtrlRes.U_Write( z ) ->
            Assert.True(( z.StartLBA = 0UL ))
            Assert.True(( z.EndLBA = 1UL ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_GetAllTraps_Event_Format() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        AddCounterAction 1 ( MediaCtrlReq.U_Format() ) dm

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))
        Assert.True(( r2.[0].Event.IsU_Format ))

    [<Fact>]
    member _.MediaControl_GetAllTraps_Action_ACA() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Format();
                        Action = MediaCtrlReq.U_ACA( "zzzzzz" );
                    })
                )
            )
            |> Functions.RunTaskSynchronously
        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))
        match r2.[0].Action with
        | MediaCtrlRes.U_ACA( z ) ->
            Assert.True(( z = "zzzzzz" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_GetAllTraps_Action_LUReset() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Format();
                        Action = MediaCtrlReq.U_LUReset( "yyyyy" );
                    })
                )
            )
            |> Functions.RunTaskSynchronously
        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))
        match r2.[0].Action with
        | MediaCtrlRes.U_LUReset( z ) ->
            Assert.True(( z = "yyyyy" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_GetAllTraps_Action_Count() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        AddCounterAction 5 ( MediaCtrlReq.U_Format() ) dm

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))
        match r2.[0].Action with
        | MediaCtrlRes.U_Count( z ) ->
            Assert.True(( z.Index = 5 ))
            Assert.True(( z.Value = 0 ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_GetAllTraps_Action_Delay() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Format();
                        Action = MediaCtrlReq.U_Delay( 99 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously
        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))
        match r2.[0].Action with
        | MediaCtrlRes.U_Delay( z ) ->
            Assert.True(( z = 99 ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_AddTrap_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        for i = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            AddCounterAction i ( MediaCtrlReq.U_Format() ) dm

        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Format();
                        Action = MediaCtrlReq.U_Count( 1 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously

        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.False(( y.Result ))
                Assert.True(( y.ErrorMessage.StartsWith "The number of registered traps has exceeded the limit" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = Constants.DEBUG_MEDIA_MAX_TRAP_COUNT ))

    [<Fact>]
    member _.MediaControl_AddTrap_002() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Read({
                            StartLBA = 1UL;
                            EndLBA = 2UL;
                        });
                        Action = MediaCtrlReq.U_Count( 1 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously

        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))

        match r2.[0].Event with
        | MediaCtrlRes.U_Read( x ) ->
            Assert.True(( x.StartLBA = 1UL ))
            Assert.True(( x.EndLBA = 2UL ))
        | _ ->
            Assert.Fail __LINE__

        match r2.[0].Action with
        | MediaCtrlRes.U_Count( x ) ->
            Assert.True(( x.Index = 1 ))
            Assert.True(( x.Value = 0 ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_AddTrap_003() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Read({
                            StartLBA = 1UL;
                            EndLBA = 1UL;
                        });
                        Action = MediaCtrlReq.U_Count( 1 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously

        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))

        match r2.[0].Event with
        | MediaCtrlRes.U_Read( x ) ->
            Assert.True(( x.StartLBA = 1UL ))
            Assert.True(( x.EndLBA = 1UL ))
        | _ ->
            Assert.Fail __LINE__

        match r2.[0].Action with
        | MediaCtrlRes.U_Count( x ) ->
            Assert.True(( x.Index = 1 ))
            Assert.True(( x.Value = 0 ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_AddTrap_004() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Read({
                            StartLBA = 2UL;
                            EndLBA = 1UL;
                        });
                        Action = MediaCtrlReq.U_Count( 1 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously

        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.False(( y.Result ))
                Assert.True(( y.ErrorMessage.StartsWith "Invalid value" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 0 ))

    [<Fact>]
    member _.MediaControl_AddTrap_005() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Write({
                            StartLBA = 1UL;
                            EndLBA = 2UL;
                        });
                        Action = MediaCtrlReq.U_Count( 1 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously

        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))

        match r2.[0].Event with
        | MediaCtrlRes.U_Write( x ) ->
            Assert.True(( x.StartLBA = 1UL ))
            Assert.True(( x.EndLBA = 2UL ))
        | _ ->
            Assert.Fail __LINE__

        match r2.[0].Action with
        | MediaCtrlRes.U_Count( x ) ->
            Assert.True(( x.Index = 1 ))
            Assert.True(( x.Value = 0 ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_AddTrap_006() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Write({
                            StartLBA = 1UL;
                            EndLBA = 1UL;
                        });
                        Action = MediaCtrlReq.U_Count( 1 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously

        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))

        match r2.[0].Event with
        | MediaCtrlRes.U_Write( x ) ->
            Assert.True(( x.StartLBA = 1UL ))
            Assert.True(( x.EndLBA = 1UL ))
        | _ ->
            Assert.Fail __LINE__

        match r2.[0].Action with
        | MediaCtrlRes.U_Count( x ) ->
            Assert.True(( x.Index = 1 ))
            Assert.True(( x.Value = 0 ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_AddTrap_007() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Write({
                            StartLBA = 2UL;
                            EndLBA = 1UL;
                        });
                        Action = MediaCtrlReq.U_Count( 1 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously

        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.False(( y.Result ))
                Assert.True(( y.ErrorMessage.StartsWith "Invalid value" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 0 ))

    [<Fact>]
    member _.MediaControl_AddTrap_008() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Format();
                        Action = MediaCtrlReq.U_Count( 1 );
                    })
                )
            )
            |> Functions.RunTaskSynchronously

        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))

        match r2.[0].Event with
        | MediaCtrlRes.U_Format() ->
            ()
        | _ ->
            Assert.Fail __LINE__

        match r2.[0].Action with
        | MediaCtrlRes.U_Count( x ) ->
            Assert.True(( x.Index = 1 ))
            Assert.True(( x.Value = 0 ))
        | _ ->
            Assert.Fail __LINE__

        Assert.True(( 0 = GetCounterValue 1 dm ))

    [<Fact>]
    member _.MediaControl_AddTrap_009() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_AddTrap({
                        Event = MediaCtrlReq.U_Format();
                        Action = MediaCtrlReq.U_ACA( "aaa" );
                    })
                )
            )
            |> Functions.RunTaskSynchronously

        match r1 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_AddTrapResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))

        match r2.[0].Event with
        | MediaCtrlRes.U_Format() ->
            ()
        | _ ->
            Assert.Fail __LINE__

        match r2.[0].Action with
        | MediaCtrlRes.U_ACA( x ) ->
            Assert.True(( x = "aaa" ))
        | _ ->
            Assert.Fail __LINE__

        Assert.True(( -1 = GetCounterValue 1 dm ))

    [<Fact>]
    member _.MediaControl_ClearTraps_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 0 ))

        let r2 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_ClearTraps()
                )
            )
            |> Functions.RunTaskSynchronously

        match r2 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_ClearTrapsResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 0 ))

    [<Fact>]
    member _.MediaControl_ClearTraps_002() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        AddCounterAction 1 ( MediaCtrlReq.U_Format() ) dm

        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        Assert.True(( 0 = GetCounterValue 1 dm ))

        let r2 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_ClearTraps()
                )
            )
            |> Functions.RunTaskSynchronously

        match r2 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_ClearTrapsResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 0 ))
        Assert.True(( -1 = GetCounterValue 1 dm ))

    [<Fact>]
    member _.MediaControl_ClearTraps_003() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        for _ = 1 to Constants.DEBUG_MEDIA_MAX_TRAP_COUNT do
            AddCounterAction 1 ( MediaCtrlReq.U_Format() ) dm

        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = Constants.DEBUG_MEDIA_MAX_TRAP_COUNT ))

        let r2 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_ClearTraps()
                )
            )
            |> Functions.RunTaskSynchronously

        match r2 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_ClearTrapsResult( y ) ->
                Assert.True(( y.Result ))
                Assert.True(( y.ErrorMessage = "" ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 0 ))

    [<Fact>]
    member _.MediaControl_GetCounterValue_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 0 ))

        let r2 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_GetCounterValue( 99 )
                )
            )
            |> Functions.RunTaskSynchronously

        match r2 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_CounterValue( y ) ->
                Assert.True(( y = -1 ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_GetCounterValue_002() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        AddCounterAction 98 ( MediaCtrlReq.U_TestUnitReady() ) dm

        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_GetCounterValue( 99 )
                )
            )
            |> Functions.RunTaskSynchronously

        match r2 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_CounterValue( y ) ->
                Assert.True(( y = -1 ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_GetCounterValue_003() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia

        AddCounterAction 88 ( MediaCtrlReq.U_TestUnitReady() ) dm

        let r1 = GetAllTraps dm
        Assert.True(( r1.Length = 1 ))

        let r2 =
            dm.MediaControl(
                MediaCtrlReq.U_Debug(
                    MediaCtrlReq.U_GetCounterValue( 88 )
                )
            )
            |> Functions.RunTaskSynchronously

        match r2 with
        | MediaCtrlRes.U_Debug( x ) ->
            match x with
            | MediaCtrlRes.U_CounterValue( y ) ->
                Assert.True(( y = 0 ))
            | _ ->
                Assert.Fail __LINE__
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_ACA_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Format<- ( fun itt src -> task {
            cnt <- cnt + 1
        })

        dm.MediaControl(
            MediaCtrlReq.U_Debug(
                MediaCtrlReq.U_AddTrap({
                    Event = MediaCtrlReq.U_Format();
                    Action = MediaCtrlReq.U_ACA( "abc" );
                })
            )
        )
        |> Functions.RunTaskSynchronously
        |> ignore

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))

        try
            dm.Format ( itt_me.fromPrim 99u ) defaultCommandSource
            |> Functions.RunTaskSynchronously
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Message = "abc" ))
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.MediaControl_LUReset_001() =
        let k1 = new HKiller() :> IKiller
        let stat_stub = new CStatus_Stub()
        let stub_media = new CMedia_Stub()
        stat_stub.p_CreateMedia <- ( fun c lun k -> stub_media )
        let dm = new DebugMedia( stat_stub, defaultConf, k1, lun_me.fromPrim 1UL ) :> IMedia
        let mutable cnt = 0
        stub_media.p_Format<- ( fun itt src -> task {
            cnt <- cnt + 1
        })

        dm.MediaControl(
            MediaCtrlReq.U_Debug(
                MediaCtrlReq.U_AddTrap({
                    Event = MediaCtrlReq.U_Format();
                    Action = MediaCtrlReq.U_LUReset( "abc" );
                })
            )
        )
        |> Functions.RunTaskSynchronously
        |> ignore

        let r2 = GetAllTraps dm
        Assert.True(( r2.Length = 1 ))

        try
            dm.Format ( itt_me.fromPrim 99u ) defaultCommandSource
            |> Functions.RunTaskSynchronously
        with
        | :? SCSIACAException as x ->
            Assert.Fail __LINE__
        | _ as x ->
            Assert.True(( x.Message = "abc" ))


