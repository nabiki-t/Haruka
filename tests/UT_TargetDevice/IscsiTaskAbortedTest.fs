//=============================================================================
// Haruka Software Storage.
// IscsiTaskAbortedTest.fs : Test cases for IscsiTaskAborted class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.TargetDevice

//=============================================================================
// Import declaration

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes
open Haruka.TargetDevice
open Haruka.Test

//=============================================================================
// Class implementation

type IscsiTaskAborted_Test () =


    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.TaskType_001() =
        let t =
            IscsiTaskAborted(
                new CSession_Stub(),
                new CISCSITask_Stub()
            ) :> IIscsiTask
        Assert.True(( t.TaskType = iSCSITaskType.Aborted ))

    [<Fact>]
    member _.TaskTypeName_001() =
        let t =
            IscsiTaskAborted(
                new CSession_Stub(),
                new CISCSITask_Stub()
            ) :> IIscsiTask
        Assert.True(( t.TaskTypeName = "Aborted task" ))

    [<Fact>]
    member _.InitiatorTaskTag_001() =
        let t =
            IscsiTaskAborted(
                new CSession_Stub(),
                new CISCSITask_Stub(
                    p_GetInitiatorTaskTag = fun () -> ValueSome( itt_me.fromPrim 123u )
                )
            ) :> IIscsiTask
        Assert.True(( t.InitiatorTaskTag = ValueSome( itt_me.fromPrim 123u ) ))

    [<Fact>]
    member _.CmdSN_001() =
        let t =
            IscsiTaskAborted(
                new CSession_Stub(),
                new CISCSITask_Stub(
                    p_GetCmdSN = fun () -> ValueSome( cmdsn_me.fromPrim 456u )
                )
            ) :> IIscsiTask
        Assert.True(( t.CmdSN  = ValueSome( cmdsn_me.fromPrim 456u ) ))

    [<Fact>]
    member _.Immidiate_001() =
        let t =
            IscsiTaskAborted(
                new CSession_Stub(),
                new CISCSITask_Stub(
                    p_GetImmidiate = fun () -> ValueSome true
                )
            ) :> IIscsiTask
        Assert.True(( t.Immidiate  = ValueSome true ))

    [<Fact>]
    member _.IsExecutable_001() =
        let t =
            IscsiTaskAborted(
                new CSession_Stub(),
                new CISCSITask_Stub()
            ) :> IIscsiTask
        Assert.False(( t.IsExecutable ))

    [<Fact>]
    member _.AllegiantConnection_001() =
        let t =
            IscsiTaskAborted(
                new CSession_Stub(),
                new CISCSITask_Stub(
                    p_GetAllegiantConnection = fun () -> struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 )
                )
            ) :> IIscsiTask
        Assert.True(( t.AllegiantConnection = struct( cid_me.fromPrim 1us, concnt_me.fromPrim 2 ) ))

    [<Fact>]
    member _.GetExecuteTask_001() =
        let t =
            IscsiTaskAborted(
                new CSession_Stub(),
                new CISCSITask_Stub()
            ) :> IIscsiTask
        let struct( rf, rt ) = t.GetExecuteTask()
        Assert.Same( t, rt )

    [<Fact>]
    member _.IsRemovable_001() =
        let t =
            IscsiTaskAborted(
                new CSession_Stub(),
                new CISCSITask_Stub()
            ) :> IIscsiTask
        Assert.True(( t.IsRemovable ))

    [<Fact>]
    member _.Executed_001() =
        let t =
            IscsiTaskAborted(
                new CSession_Stub(),
                new CISCSITask_Stub()
            ) :> IIscsiTask
        Assert.True(( t.Executed ))

