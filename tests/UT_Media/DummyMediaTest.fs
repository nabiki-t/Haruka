//=============================================================================
// Haruka Software Storage.
// DummyMediaTest.fs : Test cases for DummyMedia class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Media

//=============================================================================
// Import declaration

open System

open Xunit

open Haruka.Constants
open Haruka.IODataTypes
open Haruka.Commons
open Haruka.Test
open Haruka.Media

//=============================================================================
// Class implementation

type DummyMedia_Test () =

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

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constructor_001() =
        let k1 = new HKiller() :> IKiller
        new DummyMedia( k1, lun_me.fromPrim 1UL ) |> ignore

    [<Fact>]
    member _.Terminate_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia
        m.Terminate()

    [<Fact>]
    member _.Initialize_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia
        m.Initialize()

    [<Fact>]
    member _.Closing_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia
        m.Closing()

    [<Fact>]
    member _.TestUnitReady_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia
        try
            m.TestUnitReady ( itt_me.fromPrim 0u ) defaultCommandSource |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_COMMAND_OPERATION_CODE ))

    [<Fact>]
    member _.ReadCapacity_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia
        Assert.True(( m.ReadCapacity ( itt_me.fromPrim 0u ) defaultCommandSource = 0UL ))

    [<Fact>]
    member _.Read_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia
        try
            m.Read ( itt_me.fromPrim 0u ) defaultCommandSource 0UL ArraySegment.Empty
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_COMMAND_OPERATION_CODE ))

    [<Fact>]
    member _.Write_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia
        try
            m.Write ( itt_me.fromPrim 0u ) defaultCommandSource 0UL 0UL ArraySegment.Empty
            |> Functions.RunTaskSynchronously
            |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_COMMAND_OPERATION_CODE ))

    [<Fact>]
    member _.Format_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia
        try
            m.Format ( itt_me.fromPrim 0u ) defaultCommandSource
            |> GlbFunc.RunSync
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_COMMAND_OPERATION_CODE ))

    [<Fact>]
    member _.NotifyLUReset_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia
        m.NotifyLUReset ValueNone ValueNone
        
    [<Fact>]
    member _.MediaControl_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia

        let r =
            m.MediaControl( MediaCtrlReq.U_Debug( MediaCtrlReq.U_GetAllTraps() ) )
            |> Functions.RunTaskSynchronously
        match r with
        | MediaCtrlRes.U_Unexpected( x ) ->
            Assert.True(( x.StartsWith "Dummy media does not support media controls" ))
        | _ ->
            Assert.Fail __LINE__
        
    [<Fact>]
    member _.GetSubMedia_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia
        Assert.True(( m.GetSubMedia() = [] ))

    [<Fact>]
    member _.BlockCount_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia
        Assert.True( m.BlockCount = 0UL )

    [<Fact>]
    member _.WriteProtect_001() =
        let k1 = new HKiller() :> IKiller
        let m = new DummyMedia( k1, lun_me.fromPrim 1UL ) :> IMedia
        Assert.True( m.WriteProtect )


