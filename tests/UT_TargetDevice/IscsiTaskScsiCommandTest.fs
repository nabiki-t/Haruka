//=============================================================================
// Haruka Software Storage.
// IscsiTaskScsiCommandTest.fs : Test cases for IscsiTaskScsiCommand class.
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

type IscsiTaskScsiCommand_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    static member defaultSessionParam = {
            MaxConnections = Constants.NEGOPARAM_MaxConnections;
            TargetGroupID = tgid_me.Zero;
            TargetConf = {
                IdentNumber = tnodeidx_me.fromPrim 0u;
                TargetName = "abcT";
                TargetAlias = "";
                TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                LUN = [];
                Auth = TargetGroupConf.T_Auth.U_None();
            };
            InitiatorName = "abcI";
            InitiatorAlias = "abcIA";
            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
            InitialR2T = false;
            ImmediateData = true;
            MaxBurstLength = 4096u;
            FirstBurstLength = 4096u;
            DefaultTime2Wait = 1us;
            DefaultTime2Retain = 1us;
            MaxOutstandingR2T = 1us;
            DataPDUInOrder = true;
            DataSequenceInOrder = true;
            ErrorRecoveryLevel = 0uy;
    }

    static member defaultScsiCommandPDUValues = {
        I = true;
        F = true;
        R = false;
        W = true;
        ATTR = TaskATTRCd.SIMPLE_TASK;
        LUN = lun_me.zero;
        InitiatorTaskTag = itt_me.fromPrim 1u;
        ExpectedDataTransferLength = 256u;
        CmdSN = cmdsn_me.zero;
        ExpStatSN = statsn_me.fromPrim 1u;
        ScsiCDB = [| 0uy .. 15uy |];
        DataSegment = PooledBuffer.Rent [| 0uy .. 255uy |];
        BidirectionalExpectedReadDataLength = 0u;
        ByteCount = 0u;
    }

    static member defaultScisDataOutPDUValues = {
        F = true;
        LUN = lun_me.zero;
        InitiatorTaskTag = itt_me.fromPrim 1u;
        TargetTransferTag = ttt_me.fromPrim 0u;
        ExpStatSN = statsn_me.zero;
        DataSN = datasn_me.zero;
        BufferOffset = 0u;
        DataSegment = PooledBuffer.Rent [| 0uy .. 255uy |];
        ByteCount = 0u;
    }

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.Constractor_001() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let defscmd = IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues
        let r =
            new IscsiTaskScsiCommand(
                objidx_me.NewID(),
                sessStub,
                cid_me.fromPrim 0us,
                concnt_me.fromPrim 0,
                ValueSome( defscmd ),
                [],
                Array.empty,
                DATARECVSTAT.SOLICITED,
                0u,
                false
            )
        Assert.False(( ( r :> IIscsiTask ).IsRemovable ))
        Assert.True(( ( r :> IIscsiTask ).IsExecutable ))
        Assert.True(( ( r :> IIscsiTask ).TaskType = SCSICommand ))
        Assert.True(( ( r :> IIscsiTask ).TaskTypeName = "SCSI Command request" ))
        Assert.True(( ( r :> IIscsiTask ).InitiatorTaskTag = ValueSome( itt_me.fromPrim 1u ) ))
        Assert.True(( ( r :> IIscsiTask ).AllegiantConnection = ( cid_me.fromPrim 0us, concnt_me.fromPrim 0 ) ))
        Assert.True(( r.Session = sessStub ))
        let scmd = r.SCSICommandPDU
        Assert.True(( scmd.IsSome ))
        Assert.True(( Object.ReferenceEquals( scmd.Value, defscmd ) ))
        Assert.True(( r.SCSIDataOutPDUs = [] ))
        Assert.True(( r.R2TPDU = Array.empty ))
        Assert.True(( r.Status = DATARECVSTAT.SOLICITED ))
        Assert.True(( r.NextR2TSNValue = 0u ))

    [<Fact>]
    member _.Constractor_002() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r =
            new IscsiTaskScsiCommand(
                objidx_me.NewID(),
                sessStub :> ISession,
                cid_me.fromPrim 0us,
                concnt_me.fromPrim 0,
                ValueNone,
                [],
                Array.empty,
                DATARECVSTAT.UNSOLICITED,
                0u,
                false
            )
        Assert.False(( ( r :> IIscsiTask ).IsRemovable ))
        Assert.False(( ( r :> IIscsiTask ).IsExecutable ))

    [<Fact>]
    member _.Constractor_003() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r2t = { offset = 0u; length = 0u; ttt = ttt_me.fromPrim 0u; sn = datasn_me.zero; isOutstanding = true; sendTime = new DateTime() }
        let r =
            new IscsiTaskScsiCommand(
                objidx_me.NewID(),
                sessStub :> ISession,
                cid_me.fromPrim 0us,
                concnt_me.fromPrim 0,
                ValueNone,
                [],
                [| r2t |],
                DATARECVSTAT.SOLICITED,
                0u,
                false
            )
        Assert.False(( ( r :> IIscsiTask ).IsRemovable ))
        Assert.True(( ( r :> IIscsiTask ).IsExecutable ))
        Assert.True(( r.R2TPDU = [| r2t |] ))

    [<Fact>]
    member _.InitiatorTaskTag_001() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r =
            new IscsiTaskScsiCommand(
                objidx_me.NewID(),
                sessStub,
                cid_me.fromPrim 0us,
                concnt_me.fromPrim 0,
                ValueSome( IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues ),
                [],
                Array.empty,
                DATARECVSTAT.SOLICITED,
                0u,
                false
            ) :> IIscsiTask
        Assert.True(( ValueSome( itt_me.fromPrim 1u ) = r.InitiatorTaskTag ))
        Assert.True(( ValueSome( cmdsn_me.zero ) = r.CmdSN ))

    member _.InitiatorTaskTag_002() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r =
            new IscsiTaskScsiCommand(
                objidx_me.NewID(),
                sessStub,
                cid_me.fromPrim 0us,
                concnt_me.fromPrim 0,
                ValueNone,
                [],
                Array.empty,
                DATARECVSTAT.SOLICITED,
                0u,
                false
            ) :> IIscsiTask
        Assert.True(( ValueNone = r.InitiatorTaskTag ))
        Assert.True(( ValueNone = r.CmdSN ))


    member _.InitiatorTaskTag_003() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = fun () -> tsih_me.zero
        )
        let r =
            new IscsiTaskScsiCommand(
                objidx_me.NewID(),
                sessStub,
                cid_me.fromPrim 0us,
                concnt_me.fromPrim 0,
                ValueNone,
                [ IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues ],
                Array.empty,
                DATARECVSTAT.SOLICITED,
                0u,
                false
            ) :> IIscsiTask
        Assert.True(( ValueSome( itt_me.fromPrim 1u ) = r.InitiatorTaskTag ))
        Assert.True(( ValueNone = r.CmdSN ))

    [<Fact>]
    member _.genR2TInfoForGap_001() =
        let result = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "genR2TInfoForGap", 0u, 19u, 10u, 0u ) :?> r2tinfo list
        Assert.True((
            result = [
                { offset = 0u; length = 10u; ttt = ttt_me.fromPrim 0u; sn = datasn_me.zero; isOutstanding = false; sendTime = new DateTime() };
                { offset = 10u; length = 10u; ttt = ttt_me.fromPrim 1u; sn = datasn_me.fromPrim 1u; isOutstanding = false; sendTime = new DateTime() };
            ]
        ))

    [<Fact>]
    member _.genR2TInfoForGap_002() =
        let result = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "genR2TInfoForGap", 0u, 15u, 10u, 100u ) :?> r2tinfo list
        Assert.True(( 2 = result.Length ))
        Assert.True(( 0u = ( result.Item 0 ).offset ))
        Assert.True(( 10u = ( result.Item 0 ).length ))
        Assert.True(( ttt_me.fromPrim 100u = ( result.Item 0 ).ttt ))
        Assert.True(( datasn_me.fromPrim 100u = ( result.Item 0 ).sn ))
        Assert.True(( 10u = ( result.Item 1 ).offset ))
        Assert.True(( 6u = ( result.Item 1 ).length ))
        Assert.True(( ttt_me.fromPrim 101u = ( result.Item 1 ).ttt ))
        Assert.True(( datasn_me.fromPrim 101u = ( result.Item 1 ).sn ))

    [<Fact>]
    member _.genR2TInfoForGap_003() =
        let result = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "genR2TInfoForGap", 0u, 5u, 10u, 0xFFFFFFFEu ) :?> r2tinfo list
        Assert.True(( 1 = result.Length ))
        Assert.True(( 0u = ( result.Item 0 ).offset ))
        Assert.True(( 6u = ( result.Item 0 ).length ))
        Assert.True(( ttt_me.fromPrim 0xFFFFFFFEu = ( result.Item 0 ).ttt ))
        Assert.True(( datasn_me.fromPrim 0xFFFFFFFEu = ( result.Item 0 ).sn ))

    [<Fact>]
    member _.genR2TInfoForGap_004() =
        let result = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "genR2TInfoForGap", 10u, 9u, 10u, 0u ) :?> r2tinfo list
        Assert.True(( 0 = result.Length ))

    [<Fact>]
    member _.genR2TInfoForGap_005() =
        let result = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "genR2TInfoForGap", 0u, 29u, 10u, 0xFFFFFFFEu ) :?> r2tinfo list
        Assert.True(( 3 = result.Length ))
        Assert.True(( 0u = ( result.Item 0 ).offset ))
        Assert.True(( 10u = ( result.Item 0 ).length ))
        Assert.True(( ttt_me.fromPrim 0xFFFFFFFEu = ( result.Item 0 ).ttt ))
        Assert.True(( datasn_me.fromPrim 0xFFFFFFFEu = ( result.Item 0 ).sn ))
        Assert.True(( 10u = ( result.Item 1 ).offset ))
        Assert.True(( 10u = ( result.Item 1 ).length ))
        Assert.True(( ttt_me.fromPrim 0u = ( result.Item 1 ).ttt ))
        Assert.True(( datasn_me.zero = ( result.Item 1 ).sn ))
        Assert.True(( 20u = ( result.Item 2 ).offset ))
        Assert.True(( 10u = ( result.Item 2 ).length ))
        Assert.True(( ttt_me.fromPrim 1u = ( result.Item 2 ).ttt ))
        Assert.True(( datasn_me.fromPrim 1u = ( result.Item 2 ).sn ))
 
    [<Fact>]
    member _.generateR2TInfo_001() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = false;
        }
        let data : SCSIDataOutPDU list = []
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateR2TInfo", cmd, data, 10u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 0 = r2t.Length ))
        Assert.True(( 0u = nsn ))

    [<Fact>]
    member _.generateR2TInfo_002() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 5;
        }
        let data = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                BufferOffset = 5u;
                DataSegment = PooledBuffer.RentAndInit 5;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
        }
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateR2TInfo", cmd, [ data ], 10u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 2 = r2t.Length ))
        Assert.True(( 2u = nsn ))
        Assert.True(( 10u = r2t.[0].offset ))
        Assert.True(( 10u = r2t.[0].length ))
        Assert.True(( 20u = r2t.[1].offset ))
        Assert.True(( 10u = r2t.[1].length ))

    [<Fact>]
    member _.generateR2TInfo_003() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 5;
        }
        let data = [
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 10u;
                    DataSegment = PooledBuffer.RentAndInit 10;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 25u;
                    DataSegment = PooledBuffer.RentAndInit 5;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
        ]
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateR2TInfo", cmd, data, 10u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 0 = r2t.Length ))

    [<Fact>]
    member _.generateR2TInfo_004() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 35;
        }
        let data = [
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 5u;
                    DataSegment = PooledBuffer.RentAndInit 10;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 15u;
                    DataSegment = PooledBuffer.RentAndInit 5;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
        ]
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateR2TInfo", cmd, data, 10u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 0 = r2t.Length ))

    [<Fact>]
    member _.generateR2TInfo_005() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.Empty;
        }
        let data : SCSIDataOutPDU list = []
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateR2TInfo", cmd, data, 10u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 3 = r2t.Length ))
        Assert.True(( 3u = nsn ))
        Assert.True(( 00u = r2t.[0].offset ))
        Assert.True(( 10u = r2t.[0].length ))
        Assert.True(( 10u = r2t.[1].offset ))
        Assert.True(( 10u = r2t.[1].length ))
        Assert.True(( 20u = r2t.[2].offset ))
        Assert.True(( 10u = r2t.[2].length ))

    [<Fact>]
    member _.generateR2TInfo_006() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 29;
        }
        let data : SCSIDataOutPDU list = []
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateR2TInfo", cmd, data, 10u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 1 = r2t.Length ))
        Assert.True(( 1u = nsn ))
        Assert.True(( 29u = r2t.[0].offset ))
        Assert.True(( 1u = r2t.[0].length ))

    [<Fact>]
    member _.generateR2TInfo_007() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 23;
        }
        let data = [
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 5u;
                    DataSegment = PooledBuffer.RentAndInit 10;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
        ]
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateR2TInfo", cmd, data, 10u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 1 = r2t.Length ))
        Assert.True(( 1u = nsn ))
        Assert.True(( 23u = r2t.[0].offset ))
        Assert.True(( 7u = r2t.[0].length ))

    [<Fact>]
    member _.generateR2TInfo_008() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 30;
        }
        let data : SCSIDataOutPDU list = []
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateR2TInfo", cmd, data, 10u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 0 = r2t.Length ))
        Assert.True(( 0u = nsn ))

    [<Fact>]
    member _.generateR2TInfo_009() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 0u;
                DataSegment = PooledBuffer.Empty;
        }
        let data : SCSIDataOutPDU list = []
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateR2TInfo", cmd, data, 10u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 0 = r2t.Length ))
        Assert.True(( 0u = nsn ))

    [<Fact>]
    member _.generateR2TInfo_010() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 1u;
                DataSegment = PooledBuffer.Empty;
        }
        let data : SCSIDataOutPDU list = []
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateR2TInfo", cmd, data, 10u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 1 = r2t.Length ))
        Assert.True(( r2t.[0].offset = 0u ))
        Assert.True(( r2t.[0].length = 1u ))
        Assert.True(( 1u = nsn ))

    [<Fact>]
    member _.generateRecoveryR2TInfo_001() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 100u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let data = [
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 20u;
                    DataSegment = PooledBuffer.RentAndInit 12;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 55u;
                    DataSegment = PooledBuffer.RentAndInit 7;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 60u;
                    DataSegment = PooledBuffer.RentAndInit 24;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
        ]
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateRecoveryR2TInfo", cmd, data, 20u, 5u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 4 = r2t.Length ))
        Assert.True(( 9u = nsn ))
        Assert.True(( 10u = r2t.[0].offset ))
        Assert.True(( 10u = r2t.[0].length ))
        Assert.True(( datasn_me.fromPrim 5u = r2t.[0].sn ))
        Assert.True(( 32u = r2t.[1].offset ))
        Assert.True(( 20u = r2t.[1].length ))
        Assert.True(( datasn_me.fromPrim 6u = r2t.[1].sn ))
        Assert.True(( 52u = r2t.[2].offset ))
        Assert.True(( 3u = r2t.[2].length ))
        Assert.True(( datasn_me.fromPrim 7u = r2t.[2].sn ))
        Assert.True(( 84u = r2t.[3].offset ))
        Assert.True(( 16u = r2t.[3].length ))
        Assert.True(( datasn_me.fromPrim 8u = r2t.[3].sn ))

    [<Fact>]
    member _.generateRecoveryR2TInfo_002() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = false;
        }
        let data : SCSIDataOutPDU list = []
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateRecoveryR2TInfo", cmd, data, 10u, 0u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 0 = r2t.Length ))
        Assert.True(( 0u = nsn ))

    [<Fact>]
    member _.generateRecoveryR2TInfo_003() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 30;
        }
        let data = [
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 20u;
                    DataSegment = PooledBuffer.RentAndInit 12;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
        ]
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateRecoveryR2TInfo", cmd, data, 20u, 5u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 0 = r2t.Length ))
        Assert.True(( 5u = nsn ))

    [<Fact>]
    member _.generateRecoveryR2TInfo_004() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 20;
        }
        let data = [
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 20u;
                    DataSegment = PooledBuffer.RentAndInit 10;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
        ]
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateRecoveryR2TInfo", cmd, data, 20u, 5u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 0 = r2t.Length ))
        Assert.True(( 5u = nsn ))

    [<Fact>]
    member _.generateRecoveryR2TInfo_005() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let data = [
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 20u;
                    DataSegment = PooledBuffer.RentAndInit 10;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
        ]
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateRecoveryR2TInfo", cmd, data, 20u, 5u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 1 = r2t.Length ))
        Assert.True(( 6u = nsn ))
        Assert.True(( 10u = r2t.[0].offset ))
        Assert.True(( 10u = r2t.[0].length ))
        Assert.True(( datasn_me.fromPrim 5u = r2t.[0].sn ))

    [<Fact>]
    member _.generateRecoveryR2TInfo_006() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 20;
        }
        let data = [
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 5u;
                    DataSegment = PooledBuffer.RentAndInit 5;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
        ]
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateRecoveryR2TInfo", cmd, data, 20u, 5u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 1 = r2t.Length ))
        Assert.True(( 6u = nsn ))
        Assert.True(( 20u = r2t.[0].offset ))
        Assert.True(( 10u = r2t.[0].length ))
        Assert.True(( datasn_me.fromPrim 5u = r2t.[0].sn ))

    [<Fact>]
    member _.generateRecoveryR2TInfo_007() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 5;
        }
        let data = [
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 5u;
                    DataSegment = PooledBuffer.RentAndInit 20;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 10u;
                    DataSegment = PooledBuffer.RentAndInit 5;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
        ]
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateRecoveryR2TInfo", cmd, data, 20u, 5u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 1 = r2t.Length ))
        Assert.True(( 6u = nsn ))
        Assert.True(( 25u = r2t.[0].offset ))
        Assert.True(( 5u = r2t.[0].length ))
        Assert.True(( datasn_me.fromPrim 5u = r2t.[0].sn ))

    [<Fact>]
    member _.generateRecoveryR2TInfo_008() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 5;
        }
        let data = [
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with  // empty Data-Out PDU
                    BufferOffset = 5u;
                    DataSegment = PooledBuffer.Empty;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 10u;
                    DataSegment = PooledBuffer.RentAndInit 5;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
        ]
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateRecoveryR2TInfo", cmd, data, 20u, 5u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 2 = r2t.Length ))
        Assert.True(( 7u = nsn ))
        Assert.True(( 5u = r2t.[0].offset ))
        Assert.True(( 5u = r2t.[0].length ))
        Assert.True(( datasn_me.fromPrim 5u = r2t.[0].sn ))
        Assert.True(( 15u = r2t.[1].offset ))
        Assert.True(( 15u = r2t.[1].length ))
        Assert.True(( datasn_me.fromPrim 6u = r2t.[1].sn ))

    [<Fact>]
    member _.generateRecoveryR2TInfo_009() =
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 5;
        }
        let data = [
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with  // Out of range Data-Out PDU
                    BufferOffset = 30u;
                    DataSegment = PooledBuffer.Rent 1;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with  // Out of range Data-Out PDU
                    BufferOffset = 31u;
                    DataSegment = PooledBuffer.Rent 1;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
            {
                IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                    BufferOffset = 10u;
                    DataSegment = PooledBuffer.RentAndInit 5;
                    TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
            };
        ]
        let r2t, nsn = PrivateCaller.Invoke< IscsiTaskScsiCommand >( "generateRecoveryR2TInfo", cmd, data, 20u, 5u ) :?> ( r2tinfo[] * uint32 )
        Assert.True(( 2 = r2t.Length ))
        Assert.True(( 7u = nsn ))
        Assert.True(( 5u = r2t.[0].offset ))
        Assert.True(( 5u = r2t.[0].length ))
        Assert.True(( datasn_me.fromPrim 5u = r2t.[0].sn ))
        Assert.True(( 15u = r2t.[1].offset ))
        Assert.True(( 15u = r2t.[1].length ))
        Assert.True(( datasn_me.fromPrim 6u = r2t.[1].sn ))

    [<Fact>]
    member _.ReceivedNewSCSICommandPDU_001() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
            } )
        )
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                F = false;
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let p = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True(( DATARECVSTAT.SOLICITED = p.Status ))
        Assert.True(( 2 = p.R2TPDU.Length ))
        Assert.True(( 10u = p.R2TPDU.[0].offset ))
        Assert.True(( 10u = p.R2TPDU.[0].length ))
        Assert.True(( ttt_me.fromPrim 0u = p.R2TPDU.[0].ttt ))
        Assert.True(( datasn_me.zero = p.R2TPDU.[0].sn ))
        Assert.True(( 20u = p.R2TPDU.[1].offset ))
        Assert.True(( 10u = p.R2TPDU.[1].length ))
        Assert.True(( ttt_me.fromPrim 1u = p.R2TPDU.[1].ttt ))
        Assert.True(( datasn_me.fromPrim 1u = p.R2TPDU.[1].sn ))
        Assert.True(( 2u = p.NextR2TSNValue ))

    [<Fact>]
    member _.ReceivedNewSCSICommandPDU_002() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                F = false;
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 30;
        }
        let p = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True(( DATARECVSTAT.SOLICITED = p.Status ))
        Assert.True(( 0 = p.R2TPDU.Length ))
        Assert.True(( 0u = p.NextR2TSNValue ))

    [<Fact>]
    member _.ReceivedNewSCSICommandPDU_003() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
            } )
        )
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                F = false;
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 31;
        }
        let p = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True(( DATARECVSTAT.SOLICITED = p.Status ))
        Assert.True(( 0 = p.R2TPDU.Length ))
        Assert.True(( 0u = p.NextR2TSNValue ))

    [<Fact>]
    member _.ReceivedNewSCSICommandPDU_004() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                F = false;
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 29;
        }
        let p = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True(( DATARECVSTAT.UNSOLICITED = p.Status ))
        Assert.True(( 0 = p.R2TPDU.Length ))
        Assert.True(( 0u = p.NextR2TSNValue ))

    [<Fact>]
    member _.ReceivedNewSCSICommandPDU_005() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
            } )
        )
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = false;
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let p = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True(( DATARECVSTAT.SOLICITED = p.Status ))
        Assert.True(( 0 = p.R2TPDU.Length ))
        Assert.True(( 0u = p.NextR2TSNValue ))

    [<Fact>]
    member _.ReceivedNewSCSICommandPDU_006() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 30u;
            } )
        )
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                F = true;
                W = true;
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let p = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True(( DATARECVSTAT.SOLICITED = p.Status ))
        Assert.True(( 1 = p.R2TPDU.Length ))
        Assert.True(( 10u = p.R2TPDU.[0].offset ))
        Assert.True(( 20u = p.R2TPDU.[0].length ))

    [<Fact>]
    member _.ReceivedNewSCSIDataOutPDU_001() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
            } )
        )
        let data = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu
        }
        let task = IscsiTaskScsiCommand.ReceivedNewSCSIDataOutPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, data )
        Assert.True( task.IsNone )

    [<Fact>]
    member _.ReceivedNewSCSIDataOutPDU_002() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )
        let data = IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues
        let task = IscsiTaskScsiCommand.ReceivedNewSCSIDataOutPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, data )
        Assert.True( task.IsNone )

    [<Fact>]
    member _.ReceivedNewSCSIDataOutPDU_003() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )
        let data = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu
        }
        let task = IscsiTaskScsiCommand.ReceivedNewSCSIDataOutPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, data )
        Assert.True( task.IsSome )
        Assert.True( ValueOption.isNone task.Value.SCSICommandPDU )
        Assert.True(( 1 = task.Value.SCSIDataOutPDUs.Length ))
        Assert.Same( data, task.Value.SCSIDataOutPDUs.Item( 0 ) )
        Assert.True(( 0 = task.Value.R2TPDU.Length ))
        Assert.True(( DATARECVSTAT.UNSOLICITED = task.Value.Status ))
        Assert.True(( 0u = task.Value.NextR2TSNValue ))
        Assert.False( ( task.Value :> IIscsiTask ).IsExecutable )
        Assert.False( ( task.Value :> IIscsiTask ).IsRemovable )

    [<Fact>]
    member _.ReceivedContinuationSCSIDataOutPDU_001() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )
        let cmd = {
            // All data is already received but F flag is false.
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                F = false;
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 30;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True(( 0 = task1.SCSIDataOutPDUs.Length ))
        Assert.True(( DATARECVSTAT.SOLICITED = task1.Status ))
        Assert.False( ( task1 :> IIscsiTask ).IsRemovable )
        Assert.True( ValueOption.isSome task1.SCSICommandPDU )

        let data = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu
        }

        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( task1, data )
        Assert.Same( task1, task2 )

    [<Fact>]
    member _.ReceivedContinuationSCSIDataOutPDU_002() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )
        let cmd = {
            // Unsolicited data PDU follows.
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                F = false;
                ExpectedDataTransferLength = 30u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True(( 0 = task1.SCSIDataOutPDUs.Length ))
        Assert.True(( DATARECVSTAT.UNSOLICITED = task1.Status ))
        Assert.True( ValueOption.isSome task1.SCSICommandPDU )

        let data = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                TargetTransferTag = ttt_me.fromPrim 0x0u                // TTT of unsolicited data pdu must be 0xFFFFFFFF
        }

        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( task1, data )
        Assert.Same( task1, task2 )

    [<Fact>]
    member _.ReceivedContinuationSCSIDataOutPDU_003() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )
        let cmd = {
            // Unsolicited data PDU follows.
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                F = false;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 20;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True(( 0 = task1.SCSIDataOutPDUs.Length ))
        Assert.True(( DATARECVSTAT.UNSOLICITED = task1.Status ))
        Assert.True( ValueOption.isSome task1.SCSICommandPDU )

        // The last unsolicited data is the PDU. However, not all data has been received.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = true;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 20u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }

        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( task1, data1 )
        Assert.True(( 1 = task2.SCSIDataOutPDUs.Length ))
        Assert.Same( data1, task2.SCSIDataOutPDUs.Item( 0 ) )
        Assert.True(( DATARECVSTAT.SOLICITED = task2.Status ))
        Assert.True(( 1 = task2.R2TPDU.Length ))
        Assert.True(( 30u = task2.R2TPDU.[0].offset ))
        Assert.True(( 10u = task2.R2TPDU.[0].length ))
        Assert.True(( 1u = task2.NextR2TSNValue ))

    [<Fact>]
    member _.ReceivedContinuationSCSIDataOutPDU_004() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )
        // Unsolicited data PDU follows.
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                F = false;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 20;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True(( 0 = task1.SCSIDataOutPDUs.Length ))
        Assert.True(( DATARECVSTAT.UNSOLICITED = task1.Status ))
        Assert.True( ValueOption.isSome task1.SCSICommandPDU )

        // Unsolicited Data PDU. More Unsolicited Data PDUs follow.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = false;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 20u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }

        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( task1, data1 )
        Assert.True(( 1 = task2.SCSIDataOutPDUs.Length ))
        Assert.Same( data1, task2.SCSIDataOutPDUs.Item( 0 ) )
        Assert.True(( DATARECVSTAT.UNSOLICITED = task2.Status ))
        Assert.True(( 0 = task2.R2TPDU.Length ))
        Assert.True(( 0u = task2.NextR2TSNValue ))

    [<Fact>]
    member _.ReceivedContinuationSCSIDataOutPDU_005() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )
        // Unsolicited data PDU. Not SCSI command PDU.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = false;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSIDataOutPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, data1 )
        Assert.True( task1.IsSome )
        Assert.True( ValueOption.isNone task1.Value.SCSICommandPDU )

        // The last unsolicited data PDU.
        let data2 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = true;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.fromPrim 1u;
                BufferOffset = 10u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }

        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( task1.Value, data2 )
        Assert.True( ValueOption.isNone task2.SCSICommandPDU )
        Assert.True(( 2 = task2.SCSIDataOutPDUs.Length ))
        Assert.True( List.exists ( (=) data1 ) task2.SCSIDataOutPDUs )
        Assert.True( List.exists ( (=) data2 ) task2.SCSIDataOutPDUs )
        Assert.True(( DATARECVSTAT.UNSOLICITED = task2.Status ))
        Assert.True(( 0 = task2.R2TPDU.Length ))
        Assert.True(( 0u = task2.NextR2TSNValue ))

    [<Fact>]
    member _.ReceivedContinuationSCSIDataOutPDU_006() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
            } )
        )
        // No unsolicited data PDUs follow.
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True( ValueOption.isSome task1.SCSICommandPDU )
        Assert.True(( 0 = task1.SCSIDataOutPDUs.Length ))
        Assert.True(( 3 = task1.R2TPDU.Length ))
        Assert.True(( DATARECVSTAT.SOLICITED = task1.Status ))

        // Unexpected Unsolicited Data PDU.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = false;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 20u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }

        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( task1, data1 )
        Assert.True(( 1 = task2.SCSIDataOutPDUs.Length ))
        Assert.Same( task2.SCSIDataOutPDUs.Item( 0 ), data1 )
        Assert.True(( 3 = task1.R2TPDU.Length ))
        Assert.True(( DATARECVSTAT.SOLICITED = task1.Status ))

    [<Fact>]
    member _.ReceivedContinuationSCSIDataOutPDU_007() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
            } )
        )
        // No unsolicited data PDUs follow.
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True( ValueOption.isSome task1.SCSICommandPDU )
        Assert.True(( 0 = task1.SCSIDataOutPDUs.Length ))
        Assert.True(( 3 = task1.R2TPDU.Length ))
        Assert.True(( ttt_me.fromPrim 0u = task1.R2TPDU.[0].ttt ))
        Assert.True(( datasn_me.zero = task1.R2TPDU.[0].sn ))
        Assert.True(( ttt_me.fromPrim 1u = task1.R2TPDU.[1].ttt ))
        Assert.True(( datasn_me.fromPrim 1u = task1.R2TPDU.[1].sn ))
        Assert.True(( ttt_me.fromPrim 2u = task1.R2TPDU.[2].ttt ))
        Assert.True(( datasn_me.fromPrim 2u = task1.R2TPDU.[2].sn ))
        Assert.True(( DATARECVSTAT.SOLICITED = task1.Status ))

        // Solicited Data PDU with unexpected TTT.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = false;
                TargetTransferTag = ttt_me.fromPrim 10u;
                DataSN = datasn_me.zero;
                BufferOffset = 20u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }

        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( task1, data1 )
        Assert.True(( 0 = task2.SCSIDataOutPDUs.Length ))
        Assert.True(( 3 = task1.R2TPDU.Length ))
        Assert.True(( DATARECVSTAT.SOLICITED = task1.Status ))


    [<Fact>]
    member _.ReceivedContinuationSCSIDataOutPDU_008() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
            } )
        )
        // No unsolicited data PDUs follow.
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True( ValueOption.isSome task1.SCSICommandPDU )
        Assert.True(( 0 = task1.SCSIDataOutPDUs.Length ))
        Assert.True(( 3 = task1.R2TPDU.Length ))
        Assert.True(( ttt_me.fromPrim 0u = task1.R2TPDU.[0].ttt ))
        Assert.True(( datasn_me.zero = task1.R2TPDU.[0].sn ))
        Assert.True(( ttt_me.fromPrim 1u = task1.R2TPDU.[1].ttt ))
        Assert.True(( datasn_me.fromPrim 1u = task1.R2TPDU.[1].sn ))
        Assert.True(( ttt_me.fromPrim 2u = task1.R2TPDU.[2].ttt ))
        Assert.True(( datasn_me.fromPrim 2u = task1.R2TPDU.[2].sn ))
        Assert.True(( DATARECVSTAT.SOLICITED = task1.Status ))

        // Respond to R2T PDU. No R2T is cleared.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = false;
                TargetTransferTag = ttt_me.fromPrim 0u;
                DataSN = datasn_me.zero;
                BufferOffset = 10u;
                DataSegment = [| 0uy .. 5uy |] |> PooledBuffer.Rent;
        }

        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( task1, data1 )
        Assert.True(( 1 = task2.SCSIDataOutPDUs.Length ))
        Assert.Same( task2.SCSIDataOutPDUs.Item( 0 ), data1 )
        Assert.True(( 3 = task1.R2TPDU.Length ))
        Assert.True(( DATARECVSTAT.SOLICITED = task1.Status ))

    [<Fact>]
    member _.ReceivedContinuationSCSIDataOutPDU_009() =
        let mutable cnt2 = 0
        let connStub = new CConnection_Stub(
            p_NotifyR2TSatisfied = ( fun itt r2tsn ->
                Assert.True(( itt = itt_me.fromPrim 1u ))
                Assert.True(( r2tsn = datasn_me.zero ))
                cnt2 <- 1
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
            } ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub ) )
        )

        // No unsolicited data PDUs follow.
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True( ValueOption.isSome task1.SCSICommandPDU )
        Assert.True(( 0 = task1.SCSIDataOutPDUs.Length ))
        Assert.True(( 3 = task1.R2TPDU.Length ))
        Assert.True(( ttt_me.fromPrim 0u = task1.R2TPDU.[0].ttt ))
        Assert.True(( datasn_me.zero = task1.R2TPDU.[0].sn ))
        Assert.True(( ttt_me.fromPrim 1u = task1.R2TPDU.[1].ttt ))
        Assert.True(( datasn_me.fromPrim 1u = task1.R2TPDU.[1].sn ))
        Assert.True(( ttt_me.fromPrim 2u = task1.R2TPDU.[2].ttt ))
        Assert.True(( datasn_me.fromPrim 2u = task1.R2TPDU.[2].sn ))
        Assert.True(( DATARECVSTAT.SOLICITED = task1.Status ))

        // Respond to R2T PDU. One R2T is cleared.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = true;
                TargetTransferTag = ttt_me.fromPrim 0u;
                DataSN = datasn_me.zero;
                BufferOffset = 10u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }

        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( task1, data1 )
        Assert.True(( 1 = task2.SCSIDataOutPDUs.Length ))
        Assert.Same( task2.SCSIDataOutPDUs.Item( 0 ), data1 )
        Assert.True(( 2 = task2.R2TPDU.Length ))
        Assert.True(( ttt_me.fromPrim 1u = task2.R2TPDU.[0].ttt ))
        Assert.True(( datasn_me.fromPrim 1u = task2.R2TPDU.[0].sn ))
        Assert.True(( 20u = task2.R2TPDU.[0].offset ))
        Assert.True(( 10u = task2.R2TPDU.[0].length ))
        Assert.True(( ttt_me.fromPrim 2u = task2.R2TPDU.[1].ttt ))
        Assert.True(( datasn_me.fromPrim 2u = task2.R2TPDU.[1].sn ))
        Assert.True(( 30u = task2.R2TPDU.[1].offset ))
        Assert.True(( 10u = task2.R2TPDU.[1].length ))
        Assert.True(( DATARECVSTAT.SOLICITED = task2.Status ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReceivedContinuationSCSIDataOutPDU_010() =
        let mutable cnt2 = 0
        let connStub = new CConnection_Stub(
            p_NotifyR2TSatisfied = ( fun itt r2tsn ->
                Assert.True(( itt = itt_me.fromPrim 1u ))
                Assert.True(( r2tsn = datasn_me.zero ))
                cnt2 <- 1
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
                    ErrorRecoveryLevel = 1uy;
            } ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub ) )
        )

        // No unsolicited data PDUs follow.
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 30;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True( ValueOption.isSome task1.SCSICommandPDU )
        Assert.True(( 0 = task1.SCSIDataOutPDUs.Length ))
        Assert.True(( 1 = task1.R2TPDU.Length ))
        Assert.True(( ttt_me.fromPrim 0u = task1.R2TPDU.[0].ttt ))
        Assert.True(( datasn_me.zero = task1.R2TPDU.[0].sn ))
        Assert.True(( 30u = task1.R2TPDU.[0].offset ))
        Assert.True(( 10u = task1.R2TPDU.[0].length ))
        Assert.True(( DATARECVSTAT.SOLICITED = task1.Status ))

        // Respond to R2T PDU. All R2Ts are cleared.
        // However, recovery R2T is generated.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = true;
                TargetTransferTag = ttt_me.fromPrim 0u;
                DataSN = datasn_me.zero;
                BufferOffset = 35u;
                DataSegment = [| 0uy .. 4uy |] |> PooledBuffer.Rent;
        }

        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( task1, data1 )
        Assert.True(( 1 = task2.SCSIDataOutPDUs.Length ))
        Assert.Same( task2.SCSIDataOutPDUs.Item( 0 ), data1 )
        Assert.True(( 1 = task2.R2TPDU.Length ))
        Assert.True(( ttt_me.fromPrim 1u = task2.R2TPDU.[0].ttt ))
        Assert.True(( datasn_me.fromPrim 1u = task2.R2TPDU.[0].sn ))
        Assert.True(( 30u = task2.R2TPDU.[0].offset ))
        Assert.True(( 5u = task2.R2TPDU.[0].length ))
        Assert.True(( DATARECVSTAT.SOLICITED = task2.Status ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member _.ReceivedContinuationSCSIDataOutPDU_011() =
        let mutable cnt = 0
        let mutable cnt2 = 0
        let connStub = new CConnection_Stub(
            p_NotifyR2TSatisfied = ( fun itt r2tsn ->
                Assert.True(( itt = itt_me.fromPrim 1u ))
                Assert.True(( r2tsn = datasn_me.zero ))
                cnt2 <- 1
            )
        )
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
                    ErrorRecoveryLevel = 0uy;   // ErrorRecoveryLevel is 0
            } ),
            p_DestroySession = ( fun () ->
                cnt <- 1                        // session is destroyed
            ),
            p_GetConnection = ( fun _ _ -> ValueSome( connStub ) )
        )

        // No unsolicited data PDUs follow.
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 30;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True( ValueOption.isSome task1.SCSICommandPDU )
        Assert.True(( 0 = task1.SCSIDataOutPDUs.Length ))
        Assert.True(( 1 = task1.R2TPDU.Length ))
        Assert.True(( ttt_me.fromPrim 0u = task1.R2TPDU.[0].ttt ))
        Assert.True(( datasn_me.zero = task1.R2TPDU.[0].sn ))
        Assert.True(( 30u = task1.R2TPDU.[0].offset ))
        Assert.True(( 10u = task1.R2TPDU.[0].length ))
        Assert.True(( DATARECVSTAT.SOLICITED = task1.Status ))

        // Respond to R2T PDU. All R2Ts are cleared.
        // However, recovery R2T is generated.
        // ErrorRecoveryLevel is 0, so the session is destroyed.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = true;
                TargetTransferTag = ttt_me.fromPrim 0u;
                DataSN = datasn_me.zero;
                BufferOffset = 35u;
                DataSegment = [| 0uy .. 4uy |] |> PooledBuffer.Rent;
        }

        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( task1, data1 )
        Assert.True(( cnt = 1 ))
        Assert.True(( cnt2 = 1 ))


    [<Fact>]
    member _.ReceivedContinuationSCSICommandPDU_001() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
            } )
        )
        // First SCSI command PDU.
        let cmd1 = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                I = true;
                W = true;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 30;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd1 )
        Assert.True( ValueOption.isSome task1.SCSICommandPDU )

        // The second SCSI Command PDU was received.
        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSICommandPDU( task1, cmd1 )
        Assert.Same( task1, task2 )

    [<Fact>]
    member _.ReceivedContinuationSCSICommandPDU_002() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;      // InitialR2T is true.
                    MaxBurstLength = 10u;
            } )
        )

        // First SCSI Data-Out PDU.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = false;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }
        let task1 =
            new IscsiTaskScsiCommand(
                    objidx_me.NewID(),
                    sessStub,
                    cid_me.fromPrim 0us,
                    concnt_me.fromPrim 0,
                    ValueNone,
                    [ data1 ],
                    Array.empty,
                    DATARECVSTAT.UNSOLICITED,
                    0u,
                    false
                )

        // A SCSI Command PDU was received later.
        // It is not expected to receive a SCSI Data-Out PDU before sending a R2T PDU.
        let cmd1 = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 30;
        }
        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSICommandPDU( task1, cmd1 )
        Assert.True( ValueOption.isSome task2.SCSICommandPDU )
        Assert.True(( 0 = task2.SCSIDataOutPDUs.Length ))

    [<Fact>]
    member _.ReceivedContinuationSCSICommandPDU_003() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;     // InitialR2T is false.
                    MaxBurstLength = 10u;
            } )
        )

        // First SCSI Data-Out PDU.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = false;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSIDataOutPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, data1 )
        Assert.True( task1.IsSome )
        Assert.True( ValueOption.isNone task1.Value.SCSICommandPDU )
        Assert.True(( 1 = task1.Value.SCSIDataOutPDUs.Length ))
        Assert.Same( data1, task1.Value.SCSIDataOutPDUs.[0] )

        // A SCSI Command PDU with W bit is true, no SCSI Data-Out PDUs are expected
        let cmd1 = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = false;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 30;
        }
        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSICommandPDU( task1.Value, cmd1 )
        Assert.True( ValueOption.isSome task2.SCSICommandPDU )
        Assert.True(( 0 = task2.SCSIDataOutPDUs.Length ))

    [<Fact>]
    member _.ReceivedContinuationSCSICommandPDU_004() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )

        // First unsolicited SCSI Data-Out PDU received.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = true;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 15u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSIDataOutPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, data1 )
        Assert.True( task1.IsSome )
        Assert.True( ValueOption.isNone task1.Value.SCSICommandPDU )
        Assert.True(( 1 = task1.Value.SCSIDataOutPDUs.Length ))
        Assert.Same( data1, task1.Value.SCSIDataOutPDUs.[0] )
        Assert.True(( DATARECVSTAT.UNSOLICITED = task1.Value.Status ))

        // A SCSI Command PDU is received and an R2T is generated.
        let cmd1 = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSICommandPDU( task1.Value, cmd1 )
        Assert.True( ValueOption.isSome task2.SCSICommandPDU )
        Assert.True(( 1 = task2.SCSIDataOutPDUs.Length ))
        Assert.True(( DATARECVSTAT.SOLICITED = task2.Status ))
        Assert.True(( 2 = task2.R2TPDU.Length ))
        Assert.True(( ttt_me.fromPrim 0u = task2.R2TPDU.[0].ttt ))
        Assert.True(( datasn_me.zero = task2.R2TPDU.[0].sn ))
        Assert.True(( 25u = task2.R2TPDU.[0].offset ))
        Assert.True(( 10u = task2.R2TPDU.[0].length ))
        Assert.True(( ttt_me.fromPrim 1u = task2.R2TPDU.[1].ttt ))
        Assert.True(( datasn_me.fromPrim 1u = task2.R2TPDU.[1].sn ))
        Assert.True(( 35u = task2.R2TPDU.[1].offset ))
        Assert.True(( 5u = task2.R2TPDU.[1].length ))

    [<Fact>]
    member _.ReceivedContinuationSCSICommandPDU_005() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )

        // First unsolicited SCSI Data-Out PDU received.
        // However, the F bit is false.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = false;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 15u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSIDataOutPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, data1 )
        Assert.True( task1.IsSome )
        Assert.True( ValueOption.isNone task1.Value.SCSICommandPDU )
        Assert.True(( 1 = task1.Value.SCSIDataOutPDUs.Length ))
        Assert.Same( data1, task1.Value.SCSIDataOutPDUs.[0] )
        Assert.True(( DATARECVSTAT.UNSOLICITED = task1.Value.Status ))

        // A SCSI Command PDU is received and an R2T is generated.
        // The F bit of the subsequently received SCSI Command PDU is true.
        let cmd1 = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                F = true;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSICommandPDU( task1.Value, cmd1 )
        Assert.True( ValueOption.isSome task2.SCSICommandPDU )
        Assert.True(( 1 = task2.SCSIDataOutPDUs.Length ))
        Assert.True(( DATARECVSTAT.SOLICITED = task2.Status ))
        Assert.True(( 2 = task2.R2TPDU.Length ))
        Assert.True(( ttt_me.fromPrim 0u = task2.R2TPDU.[0].ttt ))
        Assert.True(( datasn_me.zero = task2.R2TPDU.[0].sn ))
        Assert.True(( 25u = task2.R2TPDU.[0].offset ))
        Assert.True(( 10u = task2.R2TPDU.[0].length ))
        Assert.True(( ttt_me.fromPrim 1u = task2.R2TPDU.[1].ttt ))
        Assert.True(( datasn_me.fromPrim 1u = task2.R2TPDU.[1].sn ))
        Assert.True(( 35u = task2.R2TPDU.[1].offset ))
        Assert.True(( 5u = task2.R2TPDU.[1].length ))

    [<Fact>]
    member _.ReceivedContinuationSCSICommandPDU_006() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )

        // First unsolicited SCSI Data-Out PDU received.
        // However, the F bit is false.
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = false;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 15u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSIDataOutPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, data1 )
        Assert.True( task1.IsSome )
        Assert.True( ValueOption.isNone task1.Value.SCSICommandPDU )
        Assert.True(( 1 = task1.Value.SCSIDataOutPDUs.Length ))
        Assert.Same( data1, task1.Value.SCSIDataOutPDUs.[0] )
        Assert.True(( DATARECVSTAT.UNSOLICITED = task1.Value.Status ))

        // A SCSI Command PDU with the F bit set to false was received.
        let cmd1 = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                F = false;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSICommandPDU( task1.Value, cmd1 )
        Assert.True( ValueOption.isSome task2.SCSICommandPDU )
        Assert.True(( 1 = task2.SCSIDataOutPDUs.Length ))
        Assert.True(( DATARECVSTAT.UNSOLICITED = task2.Status ))
        Assert.True(( 0 = task2.R2TPDU.Length ))

    [<Fact>]
    member _.GetExecuteTask_001() =
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                F = false;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.False( ( task1 :> IIscsiTask ).IsExecutable )
        Assert.False( ( task1 :> IIscsiTask ).IsRemovable )

        let struct( ext, nxt ) = ( task1 :> IIscsiTask ).GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.False( ( task1 :> IIscsiTask ).IsExecutable )
        Assert.False( ( task1 :> IIscsiTask ).IsRemovable )

        Assert.True(( DATARECVSTAT.UNSOLICITED = task1.Status ))

    [<Fact>]
    member _.GetExecuteTask_002() =
        let mutable cnt = 0
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = false;
                    MaxBurstLength = 10u;
            } )
        )
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                F = false;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 20;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = true;
                TargetTransferTag = ttt_me.fromPrim 0xFFFFFFFFu;
                DataSN = datasn_me.zero;
                BufferOffset = 20u;
                DataSegment = [| 0uy .. 19uy |] |> PooledBuffer.Rent;
        }
        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( task1, data1 )
        Assert.True(( DATARECVSTAT.SOLICITED = task2.Status ))
        Assert.True( ( task2 :> IIscsiTask ).IsExecutable )
        Assert.False( ( task2 :> IIscsiTask ).IsRemovable )

        sessStub.p_GetSCSITaskRouter <- (
            fun () -> new CProtocolService_Stub(
                p_SCSICommand = ( fun cid counter command data ->
                    cnt <- 1
                    Assert.True(( cid_me.fromPrim 0us = cid ))
                    Assert.True(( concnt_me.fromPrim 0 = counter ))
                    Assert.Same( command, cmd )
                    Assert.True(( 1 = data.Length ))
                    Assert.Same( data1, data.Item( 0 ) )
                )
            )
        )

        let struct( ext, nxt ) = ( task2 :> IIscsiTask ).GetExecuteTask()
        Assert.True( nxt.Executed )
        ext()

        Assert.False( nxt.IsExecutable )
        Assert.True( nxt.IsRemovable )
        Assert.True(( 1 = cnt ))

    [<Fact>]
    member _.GetExecuteTask_003() =
        let mutable cnt = 0
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
                    MaxOutstandingR2T = 2us;
            } )
        )
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True(( DATARECVSTAT.SOLICITED = task1.Status ))
        Assert.True(( 3 = task1.R2TPDU.Length ))

        sessStub.p_GetConnection <- ( fun cid counter ->
            Assert.True(( cid_me.fromPrim 0us = cid ))
            Assert.True(( concnt_me.fromPrim 0 = counter ))
            new CConnection_Stub(
                p_SendPDU = ( fun pdu ->
                    cnt <- cnt + 1
                    Assert.True(( OpcodeCd.R2T = pdu.Opcode ))
                    Assert.True(( itt_me.fromPrim 1u = pdu.InitiatorTaskTag ))
                    let r2t = pdu :?> R2TPDU
                    if cnt = 1 then
                        Assert.True(( 10u = r2t.BufferOffset ))
                        Assert.True(( 10u = r2t.DesiredDataTransferLength ))
                        Assert.True(( datasn_me.zero = r2t.R2TSN ))
                    else
                        Assert.True(( 20u = r2t.BufferOffset ))
                        Assert.True(( 10u = r2t.DesiredDataTransferLength ))
                        Assert.True(( datasn_me.fromPrim 1u = r2t.R2TSN ))
                )
            ) :> IConnection
            |> ValueSome
        )

        let struct( ext, task1_1 ) = ( task1 :> IIscsiTask ).GetExecuteTask()
        Assert.True( task1_1.Executed )
        ext()

        Assert.True(( 2 = cnt ))
        Assert.True( ( task1_1 :?> IscsiTaskScsiCommand ).R2TPDU.[0].isOutstanding )
        Assert.True( ( task1_1 :?> IscsiTaskScsiCommand ).R2TPDU.[1].isOutstanding )
        Assert.False( ( task1_1 :?> IscsiTaskScsiCommand ).R2TPDU.[2].isOutstanding )

        let mutable cnt2 = 0
        sessStub.p_GetConnection <- ( fun cid counter ->
            Assert.True(( cid_me.fromPrim 0us = cid ))
            Assert.True(( concnt_me.fromPrim 0 = counter ))
            new CConnection_Stub(
                p_SendPDU = ( fun pdu ->
                    Assert.Fail __LINE__
                    cnt <- cnt + 1
                ),
                p_NotifyR2TSatisfied = ( fun itt r2tsn ->
                    cnt2 <- 1
                    Assert.True(( itt = itt_me.fromPrim 1u ))
                    Assert.True(( r2tsn = datasn_me.zero ))
                )
            ) :> IConnection
            |> ValueSome
        )

        let struct( ext, task1_2 ) = task1_1.GetExecuteTask()
        Assert.True( task1_2.Executed )
        ext()

        Assert.True(( 2 = cnt ))

        let data1 = {
            IscsiTaskScsiCommand_Test.defaultScisDataOutPDUValues with
                F = true;
                TargetTransferTag = ttt_me.fromPrim 0x0u;
                DataSN = datasn_me.zero;
                BufferOffset = 10u;
                DataSegment = [| 0uy .. 9uy |] |> PooledBuffer.Rent;
        }
        let task2 = IscsiTaskScsiCommand.ReceivedContinuationSCSIDataOutPDU( ( task1_2 :?> IscsiTaskScsiCommand ), data1 )
        Assert.True(( DATARECVSTAT.SOLICITED = task2.Status ))
        Assert.True(( 2 = task2.R2TPDU.Length ))
        Assert.True(( 1 = cnt2 ))

        sessStub.p_GetConnection <- ( fun cid counter ->
            Assert.True(( cid_me.fromPrim 0us = cid ))
            Assert.True(( concnt_me.fromPrim 0 = counter ))
            new CConnection_Stub(
                p_SendPDU = ( fun pdu ->
                    cnt <- cnt + 1
                    Assert.True(( OpcodeCd.R2T = pdu.Opcode ))
                    Assert.True(( itt_me.fromPrim 1u = pdu.InitiatorTaskTag ))
                    let r2t = pdu :?> R2TPDU
                    if cnt = 3 then
                        Assert.True(( 30u = r2t.BufferOffset ))
                        Assert.True(( 10u = r2t.DesiredDataTransferLength ))
                        Assert.True(( datasn_me.fromPrim 2u = r2t.R2TSN ))
                )
            ) :> IConnection
            |> ValueSome
        )

        let struct( ext, task2_1 ) = ( task2 :> IIscsiTask ).GetExecuteTask()
        Assert.True( task2_1.Executed )
        ext()

        Assert.True(( 3 = cnt ))
        Assert.True( ( task2_1 :?> IscsiTaskScsiCommand ).R2TPDU.[0].isOutstanding )
        Assert.True( ( task2_1 :?> IscsiTaskScsiCommand ).R2TPDU.[1].isOutstanding )

        sessStub.p_GetConnection <- ( fun cid counter ->
            Assert.True(( cid_me.fromPrim 0us = cid ))
            Assert.True(( concnt_me.fromPrim 0 = counter ))
            new CConnection_Stub(
                p_SendPDU = ( fun pdu ->
                    Assert.Fail __LINE__
                    cnt <- cnt + 1
                )
            ) :> IConnection
            |> ValueSome
        )

        let struct( ext, task2_2 ) = task2_1.GetExecuteTask()
        Assert.True( task2_2.Executed )
        ext()

        Assert.True(( 3 = cnt ))

    [<Fact>]
    member _.GetExecuteTask_004() =
        let mutable cnt = 0
        let sessStub = new CSession_Stub(
            p_GetTSIH = ( fun () -> tsih_me.zero ),
            p_GetSessionParameter = ( fun () -> {
                IscsiTaskScsiCommand_Test.defaultSessionParam with
                    InitialR2T = true;
                    MaxBurstLength = 10u;
            } ),
            p_GetConnection = ( fun cid counter ->
                Assert.True(( cid_me.fromPrim 0us = cid ))
                Assert.True(( concnt_me.fromPrim 0 = counter ))
                ValueNone
            )
        )
        let cmd = {
            IscsiTaskScsiCommand_Test.defaultScsiCommandPDUValues with
                W = true;
                ExpectedDataTransferLength = 40u;
                DataSegment = PooledBuffer.RentAndInit 10;
        }
        let task1 = IscsiTaskScsiCommand.ReceivedNewSCSICommandPDU( sessStub, cid_me.fromPrim 0us, concnt_me.fromPrim 0, cmd )
        Assert.True(( DATARECVSTAT.SOLICITED = task1.Status ))
        Assert.True(( 3 = task1.R2TPDU.Length ))

        let struct( ext, task1_1 ) = ( task1 :> IIscsiTask ).GetExecuteTask()
        Assert.True( task1_1.Executed )
        ext()

        Assert.True(( DATARECVSTAT.SOLICITED = ( task1_1 :?> IscsiTaskScsiCommand ).Status ))
        Assert.True(( 3 = ( task1_1 :?> IscsiTaskScsiCommand ).R2TPDU.Length ))
        Assert.False( ( task1_1 :?> IscsiTaskScsiCommand ).R2TPDU.[0].isOutstanding )
        Assert.False( ( task1_1 :?> IscsiTaskScsiCommand ).R2TPDU.[1].isOutstanding )
        Assert.False( ( task1_1 :?> IscsiTaskScsiCommand ).R2TPDU.[2].isOutstanding )

