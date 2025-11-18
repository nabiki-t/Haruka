//=============================================================================
// Haruka Software Storage.
// BlockDeviceLUTest.fs : Test cases for BlockDeviceLU class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent
open System.Collections.Immutable

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.BlockDeviceLU
open Haruka.IODataTypes
open Haruka.Test

//=============================================================================
// Class implementation

type public CBlockDeviceTask_Stub() =

    let mutable f_GetTaskType : ( unit -> BlockDeviceTaskType ) option = None
    let mutable f_GetSource : ( unit -> CommandSourceInfo ) option = None
    let mutable f_GetInitiatorTaskTag : ( unit -> ITT_T ) option = None
    let mutable f_GetSCSICommand : ( unit -> SCSICommandPDU ) option = None
    let mutable f_GetReceivedDataLength : ( unit -> uint ) option = None
    let mutable f_GetCDB : ( unit -> ICDB voption ) option = None
    let mutable f_Execute : ( unit -> ( unit -> Task<unit> ) ) option = None
    let mutable f_GetDescString : ( unit -> string ) option = None
    let mutable f_NotifyTerminate : ( bool -> unit ) option = None
    let mutable f_GetACANoncompliant : ( unit -> bool ) option = None
    let mutable f_ReleasePooledBuffer : ( unit -> unit ) option = Some( id )

    member val dummy : obj = box () with get, set
    member _.p_GetTaskType with set v = f_GetTaskType <- Some( v )
    member _.p_GetSource with set v = f_GetSource <- Some v
    member _.p_GetInitiatorTaskTag with set v = f_GetInitiatorTaskTag <- Some v
    member _.p_GetSCSICommand with set v = f_GetSCSICommand <- Some v
    member _.p_GetReceivedDataLength with set v = f_GetReceivedDataLength <- Some v
    member _.p_GetCDB with set v = f_GetCDB <- Some v
    member _.p_Execute with set v = f_Execute <- Some v
    member _.p_GetDescString with set v = f_GetDescString <- Some v
    member _.p_NotifyTerminate with set v = f_NotifyTerminate <- Some v
    member _.p_GetACANoncompliant with set v = f_GetACANoncompliant <- Some v
    member _.p_ReleasePooledBuffer with set v = f_ReleasePooledBuffer <- Some v

    interface IBlockDeviceTask with
        override _.TaskType : BlockDeviceTaskType = f_GetTaskType.Value()
        override _.Source : CommandSourceInfo = f_GetSource.Value()
        override _.InitiatorTaskTag : ITT_T = f_GetInitiatorTaskTag.Value()
        override _.SCSICommand : SCSICommandPDU = f_GetSCSICommand.Value()
        override _.ReceivedDataLength : uint = f_GetReceivedDataLength.Value()
        override _.CDB : ICDB voption = f_GetCDB.Value()
        override _.Execute() : ( unit -> Task<unit> ) = f_Execute.Value()
        override _.DescString : string =
            if f_GetDescString.IsSome then
                f_GetDescString.Value()
            else
                ""
        override _.NotifyTerminate ( v : bool ) : unit = f_NotifyTerminate.Value( v )
        override _.ACANoncompliant : bool = f_GetACANoncompliant.Value()
        override _.ReleasePooledBuffer() : unit = f_ReleasePooledBuffer.Value()

type BlockDeviceLU_Test () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    member _.GetTestFileName( fn : string ) =
        sprintf "%s%c%s" ( Path.GetTempPath() ) Path.DirectorySeparatorChar fn

    member private this.createBlockDevice() =
        let media = new CMedia_Stub(
            p_GetBlockCount = ( fun () -> 512UL )
        )
        let sm = new CStatus_Stub(
            p_CreateMedia = ( fun _ _ _ -> media )
        )
        let info : TargetGroupConf.T_BlockDevice = {
            Peripheral = TargetGroupConf.T_MEDIA.U_PlainFile(
                {
                    IdentNumber = mediaidx_me.fromPrim 0u;
                    MediaName = "";
                    FileName = "";
                    MaxMultiplicity = 0u;
                    QueueWaitTimeOut = 0;
                    WriteProtect = false;
                }
            );
        }
        media, sm, new BlockDeviceLU( BlockDeviceType.BDT_Normal, sm, lun_me.zero, info, Path.GetTempPath(), new HKiller() )

    
    static member private cmdSource() =
        {
            I_TNexus = new ITNexus( "INIT", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
            CID = cid_me.fromPrim 0us;
            ConCounter = concnt_me.fromPrim 0;
            TSIH = tsih_me.fromPrim 0us;
            ProtocolService = new CProtocolService_Stub();
            SessionKiller = new HKiller()
        }

    static member private defaultSCSICommand ( attr : TaskATTRCd )  =
        {
            I = false;
            F = true;
            R = false;
            W = false;
            ATTR = attr;
            LUN = lun_me.zero
            InitiatorTaskTag = itt_me.fromPrim 0u;
            ExpectedDataTransferLength = 0u;
            CmdSN = cmdsn_me.zero;
            ExpStatSN = statsn_me.zero;
            ScsiCDB = Array.empty;
            DataSegment = PooledBuffer.Empty;
            BidirectionalExpectedReadDataLength = 0u;
            ByteCount = 0u;
        }

    static member private defaultDataOutPDU =
        {
            F = false;
            LUN = lun_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 0u;
            TargetTransferTag = ttt_me.fromPrim 0u;
            ExpStatSN = statsn_me.zero;
            DataSN = datasn_me.zero;
            BufferOffset = 0u;
            DataSegment = PooledBuffer.Empty;
            ByteCount = 0u;
        }

    static member private defSCSIACAException
        ( stat : ScsiCmdStatCd )
        ( senseKey : SenseKeyCd )
        ( asc : ASCCd ) =
            new SCSIACAException(
                BlockDeviceLU_Test.cmdSource(),
                stat,
                new SenseData( true, senseKey, asc, "" ),
                ""
            )

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member this.AbortTask_001() =
        let mutable cnt = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <-
            ( fun _ _ _ _ ->
                Assert.Fail __LINE__
                cnt <- cnt + 1
            )

        sm.p_NotifyLUReset <- ( fun _ _ -> () )
        media.p_Terminate <- ( fun () -> () )

        pc.Invoke( "NotifyLUReset", source, itt_me.fromPrim 0u ) :?> unit

        ( lu :> ILU ).AbortTask source ( itt_me.fromPrim 0u ) ( itt_me.fromPrim 0u )

        Assert.True(( cnt = 0 ))


    [<Fact>]
    member this.AbortTask_002() =
        let mutable cnt1 = 0
        let _, _, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let br = new Barrier( 2 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.True(( pdu.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ))
            let tmpdu = pdu :?> TaskManagementFunctionResponsePDU
            Assert.True(( source = cid_me.fromPrim 0us ))
            Assert.True(( cnd = concnt_me.fromPrim 0 ))
            Assert.True(( tmpdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))
            Assert.True(( tmpdu.InitiatorTaskTag = itt_me.fromPrim 0u ))
            Assert.True(( lun = lun_me.zero ))
            cnt1 <- cnt1 + 1
            br.SignalAndWait()
        )

        let cnt = Array.zeroCreate<int> 10
        let tasks = [|
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt.[0] <- cnt.[0] + 1
                ),
                p_GetSource = ( fun () -> source )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun _ ->
                    Assert.Fail __LINE__
                ),
                p_GetSource = ( fun () -> source )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt.[2] <- cnt.[2] + 1
                ),
                p_GetSource = ( fun () -> source )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                p_NotifyTerminate = ( fun _ ->
                    Assert.Fail __LINE__
                ),
                p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK ),
                p_GetSource = ( fun () -> source )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt.[4] <- cnt.[4] + 1
                ),
                p_GetSource = ( fun () -> source )
            );
        |]
        let queue1 = {
            Queue =
                let w =
                    tasks
                    |> Seq.map ( fun itr -> TaskStatus.TASK_STAT_Running( itr :> IBlockDeviceTask ) )
                w.ToImmutableArray()
            ACA = ValueNone
        }
        pc.SetField( "m_TaskSet", queue1 )

        ( lu :> ILU ).AbortTask source ( itt_me.fromPrim 0u ) ( itt_me.fromPrim 0u )

        Assert.True(( br.SignalAndWait 100000 ))
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt.[0] = 1 ))
        Assert.True(( cnt.[1] = 0 ))
        Assert.True(( cnt.[2] = 1 ))
        Assert.True(( cnt.[3] = 0 ))
        Assert.True(( cnt.[4] = 1 ))

        let queue2 = pc.GetField( "m_TaskSet" ) :?> TaskSet

        Assert.True(( ( TaskStatus.getTask queue2.Queue.[0] ) = tasks.[1] ))
        Assert.True(( ( TaskStatus.getTask queue2.Queue.[1] ) = tasks.[3] ))
        Assert.True(( queue2.Queue.Length = 2 ))

    [<Fact>]
    member this.AbortTask_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.Fail __LINE__
        )

        let task1 =
            TaskStatus.TASK_STAT_Running (
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt1 <- cnt1 + 1
                        if cnt1 = 1 then
                            raise <| Exception( "" )
                        if cnt1 = 2 then
                            sema1.Release() |> ignore
                    ),
                    p_GetSource = ( fun () ->
                        BlockDeviceLU_Test.cmdSource()
                    )
                )
            )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty.Add task1;
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt2 <- cnt2 + 1
            sema2.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt3 <- cnt3 + 1
            sema3.Release() |> ignore
        )

        ( lu :> ILU ).AbortTask source ( itt_me.fromPrim 0u ) ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 2 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

        Assert.True(( sema3.Wait 100000 ))
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member this.AbortTask_005() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )

        let testTask =
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt1 <- cnt1 + 1
                        if cnt1 = 1 then
                            Assert.False( flg )
                            raise <| Exception( "" )
                        else
                            Assert.True( flg )
                        if cnt1 = 2 then
                            sema1.Release() |> ignore
                    ),
                    p_GetSource = ( fun () ->
                        {
                            I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                            CID = cid_me.fromPrim 1us;
                            ConCounter = concnt_me.fromPrim 1;
                            TSIH = tsih_me.fromPrim 0us;
                            ProtocolService = new CProtocolService_Stub();
                            SessionKiller = new HKiller()
                        }
                    )
                )
            )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty.Add testTask;
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt2 <- cnt2 + 1
            sema2.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt3 <- cnt3 + 1
            sema3.Release() |> ignore
        )

        ( lu :> ILU ).AbortTask source ( itt_me.fromPrim 0u ) ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 2 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

        Assert.True(( sema3.Wait 100000 ))
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member this.AbortTask_006() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let _, _, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.True(( pdu.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ))
            let tmpdu = pdu :?> TaskManagementFunctionResponsePDU
            Assert.True(( source = cid_me.fromPrim 0us ))
            Assert.True(( cnd = concnt_me.fromPrim 0 ))
            Assert.True(( tmpdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))
            Assert.True(( tmpdu.InitiatorTaskTag = itt_me.fromPrim 0u ))
            Assert.True(( lun = lun_me.zero ))
            cnt1 <- cnt1 + 1
            sema1.Release() |> ignore
        )

        let testTask =
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt2 <- cnt2 + 1
                    if cnt2 = 16 then
                        sema2.Release() |> ignore
                )
            )
        let testTasks = [| 
            for i = 0 to 15 do
                yield TaskStatus.TASK_STAT_Running( testTask )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        ( lu :> ILU ).AbortTask source ( itt_me.fromPrim 0u ) ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 16 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.AbortTaskSet_001() =
        let mutable cnt = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun _ _ _ _ ->
            Assert.Fail __LINE__
            cnt <- cnt + 1
        )

        sm.p_NotifyLUReset <- ( fun _ _ -> () )
        media.p_Terminate <- ( fun () -> () )

        pc.Invoke( "NotifyLUReset", source, itt_me.fromPrim 0u ) :?> unit

        ( lu :> ILU ).AbortTaskSet source ( itt_me.fromPrim 0u )

        Assert.True(( cnt = 0 ))


    [<Fact>]
    member this.AbortTaskSet_002() =
        let mutable cnt1 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.True(( pdu.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ))
            let tmpdu = pdu :?> TaskManagementFunctionResponsePDU
            Assert.True(( source = cid_me.fromPrim 0us ))
            Assert.True(( cnd = concnt_me.fromPrim 0 ))
            Assert.True(( tmpdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))
            Assert.True(( tmpdu.InitiatorTaskTag = itt_me.fromPrim 0u ))
            Assert.True(( lun = lun_me.zero ))
            cnt1 <- cnt1 + 1
            sema1.Release() |> ignore
        )

        let cnt = Array.zeroCreate<int> 10
        let tasks = [|
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun _ ->
                        Assert.Fail __LINE__
                    ),
                    p_GetSource = ( fun () -> 
                        {
                            I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                            CID = cid_me.fromPrim 1us;
                            ConCounter = concnt_me.fromPrim 1;
                            TSIH = tsih_me.fromPrim 0us;
                            ProtocolService = new CProtocolService_Stub();
                            SessionKiller = new HKiller()
                        }
                    ),
                    p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
                )
            );
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        Assert.False( flg )
                        cnt.[1] <- cnt.[1] + 1
                    ),
                    p_GetSource = ( fun () -> source )
                )
            );
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        Assert.Fail __LINE__
                    ),
                    p_GetSource = ( fun () -> source )
                )
            );
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        Assert.False( flg )
                        cnt.[3] <- cnt.[3] + 1
                    ),
                    p_GetSource = ( fun () -> source )
                )
            );
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        Assert.Fail __LINE__
                    ),
                    p_GetSource = ( fun () -> 
                        {
                            I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                            CID = cid_me.fromPrim 1us;
                            ConCounter = concnt_me.fromPrim 1;
                            TSIH = tsih_me.fromPrim 0us;
                            ProtocolService = new CProtocolService_Stub();
                            SessionKiller = new HKiller()
                        }
                    )
                )
            );
        |]

        let queue1 = {
            Queue = tasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        ( lu :> ILU ).AbortTaskSet source ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt.[0] = 0 ))
        Assert.True(( cnt.[1] = 1 ))
        Assert.True(( cnt.[2] = 0 ))
        Assert.True(( cnt.[3] = 1 ))
        Assert.True(( cnt.[4] = 0 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( ( TaskStatus.getTask m_TaskSet.Queue.[0] ) = ( TaskStatus.getTask tasks.[0] ) ))
        Assert.True(( ( TaskStatus.getTask m_TaskSet.Queue.[1] ) = ( TaskStatus.getTask tasks.[2] ) ))
        Assert.True(( ( TaskStatus.getTask m_TaskSet.Queue.[2] ) = ( TaskStatus.getTask tasks.[4] ) ))
        Assert.True(( m_TaskSet.Queue.Length = 3 ))

    [<Fact>]
    member this.AbortTaskSet_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.Fail __LINE__
        )

        let testTask =
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt1 <- cnt1 + 1
                        if cnt1 = 1 then
                            raise <| Exception( "" )
                        if cnt1 = 2 then
                            sema1.Release() |> ignore
                    ),
                    p_GetSource = ( fun () -> source )
                )
            )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty.Add testTask;
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )
        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt2 <- cnt2 + 1
            sema2.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt3 <- cnt3 + 1
            sema3.Release() |> ignore
        )

        ( lu :> ILU ).AbortTaskSet source ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 2 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

        Assert.True(( sema3.Wait 100000 ))
        Assert.True(( cnt3 = 1 ))


    [<Fact>]
    member this.AbortTaskSet_005() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let mutable cnt4 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )
        let sema4 = new SemaphoreSlim( 0 )

        let testTasks = [|
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt1 <- cnt1 + 1
                        Assert.False( flg )
                        if cnt1 = 1 then
                            raise <| Exception( "" )
                        if cnt1 = 2 then
                            sema1.Release() |> ignore
                    ),
                    p_GetSource = ( fun () -> source )
                )
            );
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt2 <- cnt2 + 1
                        Assert.True( flg )
                        sema2.Release() |> ignore
                    ),
                    p_GetSource = ( fun () ->
                        {
                            I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                            CID = cid_me.fromPrim 1us;
                            ConCounter = concnt_me.fromPrim 1;
                            TSIH = tsih_me.fromPrim 0us;
                            ProtocolService = new CProtocolService_Stub();
                            SessionKiller = new HKiller()
                        }
                    )
                )
            );
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt3 <- cnt3 + 1
            sema3.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt4 <- cnt4 + 1
            sema4.Release() |> ignore
        )

        ( lu :> ILU ).AbortTaskSet source ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 2 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

        Assert.True(( sema3.Wait 100000 ))
        Assert.True(( cnt3 = 1 ))

        Assert.True(( sema4.Wait 100000 ))
        Assert.True(( cnt4 = 1 ))

    [<Fact>]
    member this.AbortTaskSet_006() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.True(( pdu.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ))
            let tmpdu = pdu :?> TaskManagementFunctionResponsePDU
            Assert.True(( source = cid_me.fromPrim 0us ))
            Assert.True(( cnd = concnt_me.fromPrim 0 ))
            Assert.True(( tmpdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))
            Assert.True(( tmpdu.InitiatorTaskTag = itt_me.fromPrim 0u ))
            Assert.True(( lun = lun_me.zero ))
            cnt1 <- cnt1 + 1
            sema1.Release() |> ignore
        )

        let testTask =
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt2 <- cnt2 + 1
                    if cnt2 = 16 then
                        sema2.Release() |> ignore
                ),
                p_GetSource = ( fun () -> source )
            )
        let testTasks = [|
            for i = 0 to 15 do
                yield TaskStatus.TASK_STAT_Running( testTask )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        ( lu :> ILU ).AbortTaskSet source ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 16 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.ClearACA_001() =
        let mutable cnt = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun _ _ _ _ ->
            Assert.Fail __LINE__
            cnt <- cnt + 1
        )

        sm.p_NotifyLUReset <- ( fun _ _ -> () )
        media.p_Terminate <- ( fun () -> () )

        pc.Invoke( "NotifyLUReset", source, itt_me.fromPrim 0u ) :?> unit

        ( lu :> ILU ).ClearACA source ( itt_me.fromPrim 0u )

        Assert.True(( cnt = 0 ))

    [<Fact>]
    member this.ClearACA_002() =
        let mutable cnt1 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.True(( pdu.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ))
            let tmpdu = pdu :?> TaskManagementFunctionResponsePDU
            Assert.True(( source = cid_me.fromPrim 0us ))
            Assert.True(( cnd = concnt_me.fromPrim 0 ))
            Assert.True(( tmpdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))
            Assert.True(( tmpdu.InitiatorTaskTag = itt_me.fromPrim 0u ))
            Assert.True(( lun = lun_me.zero ))
            cnt1 <- cnt1 + 1
            sema1.Release() |> ignore
        )

        let cnt = Array.zeroCreate<int> 10
        let testTasks1 = [|
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun _ ->
                    Assert.Fail __LINE__
                ),
                p_GetSource = ( fun () -> 
                    {
                        I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                        CID = cid_me.fromPrim 1us;
                        ConCounter = concnt_me.fromPrim 1;
                        TSIH = tsih_me.fromPrim 0us;
                        ProtocolService = new CProtocolService_Stub();
                        SessionKiller = new HKiller()
                    }
                ),
                p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt.[1] <- cnt.[1] + 1
                ),
                p_GetSource = ( fun () -> source ),
                p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.Fail __LINE__
                ),
                p_GetSource = ( fun () -> source )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun _ ->
                    Assert.Fail __LINE__
                ),
                p_GetSource = ( fun () -> source ),
                p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.HEAD_OF_QUEUE_TASK )
            );
        |]
        let testTasks2 = [|
            for itr in testTasks1 -> TaskStatus.TASK_STAT_Running( itr )
        |]
        let queue1 = {
            Queue = testTasks2.ToImmutableArray();
            ACA = ValueSome( 
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }
        pc.SetField( "m_TaskSet", queue1 )

        ( lu :> ILU ).ClearACA source ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt.[0] = 0 ))
        Assert.True(( cnt.[1] = 1 ))
        Assert.True(( cnt.[2] = 0 ))
        Assert.True(( cnt.[3] = 0 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( ( TaskStatus.getTask m_TaskSet.Queue.[0] ) = testTasks1.[0] ))
        Assert.True(( ( TaskStatus.getTask m_TaskSet.Queue.[1] ) = testTasks1.[2] ))
        Assert.True(( ( TaskStatus.getTask m_TaskSet.Queue.[2] ) = testTasks1.[3] ))
        Assert.True(( m_TaskSet.Queue.Length = 3 ))
        let waca = m_TaskSet.ACA
        Assert.True( waca.IsNone )


    [<Fact>]
    member this.ClearACA_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.Fail __LINE__
        )

        let testTask =
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt1 <- cnt1 + 1
                        if cnt1 = 1 then
                            raise <| Exception( "" )
                        if cnt1 = 2 then
                            sema1.Release() |> ignore
                    ),
                    p_GetSource = ( fun () -> source ),
                    p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK )
                )
            )
        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty.Add testTask;
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt2 <- cnt2 + 1
            sema2.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt3 <- cnt3 + 1
            sema3.Release() |> ignore
        )

        ( lu :> ILU ).ClearACA source ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 2 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

        Assert.True(( sema3.Wait 100000 ))
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member this.ClearACA_005() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let mutable cnt4 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )
        let sema4 = new SemaphoreSlim( 0 )

        let testTasks = [|
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt1 <- cnt1 + 1
                        Assert.False( flg )
                        if cnt1 = 1 then
                            raise <| Exception( "" )
                        if cnt1 = 2 then
                            sema1.Release() |> ignore
                    ),
                    p_GetSource = ( fun () -> source ),
                    p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK )
                )
            );
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt2 <- cnt2 + 1
                        Assert.True( flg )
                        sema2.Release() |> ignore
                    ),
                    p_GetSource = ( fun () ->
                        {
                            I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                            CID = cid_me.fromPrim 1us;
                            ConCounter = concnt_me.fromPrim 1;
                            TSIH = tsih_me.fromPrim 0us;
                            ProtocolService = new CProtocolService_Stub();
                            SessionKiller = new HKiller()
                        }
                    )
                )
            );
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt3 <- cnt3 + 1
            sema3.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt4 <- cnt4 + 1
            sema4.Release() |> ignore
        )

        ( lu :> ILU ).ClearACA source ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 2 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

        Assert.True(( sema3.Wait 100000 ))
        Assert.True(( cnt3 = 1 ))

        Assert.True(( sema4.Wait 100000 ))
        Assert.True(( cnt4 = 1 ))


    [<Fact>]
    member this.ClearACA_006() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.True(( pdu.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ))
            let tmpdu = pdu :?> TaskManagementFunctionResponsePDU
            Assert.True(( source = cid_me.fromPrim 0us ))
            Assert.True(( cnd = concnt_me.fromPrim 0 ))
            Assert.True(( tmpdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))
            Assert.True(( tmpdu.InitiatorTaskTag = itt_me.fromPrim 0u ))
            Assert.True(( lun = lun_me.zero ))
            cnt1 <- cnt1 + 1
            sema1.Release() |> ignore
        )

        let testTask =
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt2 <- cnt2 + 1
                    if cnt2 = 16 then
                        sema2.Release() |> ignore
                ),
                p_GetSource = ( fun () -> source ),
                p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK )
            )
        let testTasks = [|
            for i = 0 to 15 do
                yield TaskStatus.TASK_STAT_Running( testTask )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )
        
        ( lu :> ILU ).ClearACA source ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 16 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.ClearTaskSet_001() =
        let mutable cnt = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun _ _ _ _ ->
            Assert.Fail __LINE__
            cnt <- cnt + 1
        )

        sm.p_NotifyLUReset <- ( fun _ _ -> () )
        media.p_Terminate <- ( fun () -> () )

        pc.Invoke( "NotifyLUReset", source, itt_me.fromPrim 0u ) :?> unit

        ( lu :> ILU ).ClearTaskSet source ( itt_me.fromPrim 0u )

        Assert.True(( cnt = 0 ))

    [<Fact>]
    member this.ClearTaskSet_002() =
        let mutable cnt1 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.True(( pdu.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ))
            let tmpdu = pdu :?> TaskManagementFunctionResponsePDU
            Assert.True(( source = cid_me.fromPrim 0us ))
            Assert.True(( cnd = concnt_me.fromPrim 0 ))
            Assert.True(( tmpdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))
            Assert.True(( tmpdu.InitiatorTaskTag = itt_me.fromPrim 0u ))
            Assert.True(( lun = lun_me.zero ))
            cnt1 <- cnt1 + 1
            sema1.Release() |> ignore
        )

        let cnt = Array.zeroCreate<int> 10
        let testTasks1 = [|
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.True( flg )
                    cnt.[0] <- cnt.[0] + 1
                ),
                p_GetSource = ( fun () -> 
                    {
                        I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                        CID = cid_me.fromPrim 1us;
                        ConCounter = concnt_me.fromPrim 1;
                        TSIH = tsih_me.fromPrim 0us;
                        ProtocolService = new CProtocolService_Stub();
                        SessionKiller = new HKiller()
                    }
                )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt.[1] <- cnt.[1] + 1
                ),
                p_GetSource = ( fun () -> source )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt.[2] <- cnt.[2] + 1
                ),
                p_GetSource = ( fun () -> source )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.True( flg )
                    cnt.[3] <- cnt.[3] + 1
                ),
                p_GetSource = ( fun () -> 
                    {
                        I_TNexus = new ITNexus( "INIT_3", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                        CID = cid_me.fromPrim 1us;
                        ConCounter = concnt_me.fromPrim 1;
                        TSIH = tsih_me.fromPrim 0us;
                        ProtocolService = new CProtocolService_Stub();
                        SessionKiller = new HKiller()
                    }
                )
            );
        |]
        let testTasks2 = [|
            TaskStatus.TASK_STAT_Running( testTasks1.[0] )
            TaskStatus.TASK_STAT_Dormant( testTasks1.[1] )
            TaskStatus.TASK_STAT_Running( testTasks1.[2] )
            TaskStatus.TASK_STAT_Dormant( testTasks1.[3] )
        |]
        let queue1 = {
            Queue = testTasks2.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        ( lu :> ILU ).ClearTaskSet source ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt.[0] = 1 ))
        Assert.True(( cnt.[1] = 1 ))
        Assert.True(( cnt.[2] = 1 ))
        Assert.True(( cnt.[3] = 1 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.ClearTaskSet_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.Fail __LINE__
        )

        let testTask =
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt1 <- cnt1 + 1
                        if cnt1 = 1 then
                            raise <| Exception( "" )
                        if cnt1 = 2 then
                            sema1.Release() |> ignore
                    ),
                    p_GetSource = ( fun () -> source )
                )
            )
        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty.Add testTask;
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt2 <- cnt2 + 1
            sema2.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt3 <- cnt3 + 1
            sema3.Release() |> ignore
        )

        ( lu :> ILU ).ClearTaskSet source ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 2 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

        Assert.True(( sema3.Wait 100000 ))
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member this.ClearTaskSet_005() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let mutable cnt4 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )
        let sema4 = new SemaphoreSlim( 0 )

        let testTask = [|
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt1 <- cnt1 + 1
                        Assert.False( flg )
                        if cnt1 = 1 then
                            raise <| Exception( "" )
                        if cnt1 = 2 then
                            sema1.Release() |> ignore
                    ),
                    p_GetSource = ( fun () -> source )
                )
            );
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt2 <- cnt2 + 1
                        Assert.True( flg )
                        sema2.Release() |> ignore
                    ),
                    p_GetSource = ( fun () ->
                        {
                            I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                            CID = cid_me.fromPrim 1us;
                            ConCounter = concnt_me.fromPrim 1;
                            TSIH = tsih_me.fromPrim 0us;
                            ProtocolService = new CProtocolService_Stub();
                            SessionKiller = new HKiller()
                        }
                    )
                )
            );
        |]
        let queue1 = {
            Queue = testTask.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt3 <- cnt3 + 1
            sema3.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt4 <- cnt4 + 1
            sema4.Release() |> ignore
        )

        ( lu :> ILU ).ClearTaskSet source ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 2 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

        Assert.True(( sema3.Wait 100000 ))
        Assert.True(( cnt3 = 1 ))

        Assert.True(( sema4.Wait 100000 ))
        Assert.True(( cnt4 = 1 ))

    [<Fact>]
    member this.ClearTaskSet_006() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.True(( pdu.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ))
            let tmpdu = pdu :?> TaskManagementFunctionResponsePDU
            Assert.True(( source = cid_me.fromPrim 0us ))
            Assert.True(( cnd = concnt_me.fromPrim 0 ))
            Assert.True(( tmpdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))
            Assert.True(( tmpdu.InitiatorTaskTag = itt_me.fromPrim 0u ))
            Assert.True(( lun = lun_me.zero ))
            cnt1 <- cnt1 + 1
            sema1.Release() |> ignore
        )

        let testTask =
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt2 <- cnt2 + 1
                    if cnt2 = 16 then
                        sema2.Release() |> ignore
                ),
                p_GetSource = ( fun () -> source )
            )
        let testTasks = [|
            for i = 0 to 15 do
                yield TaskStatus.TASK_STAT_Running( testTask )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        ( lu :> ILU ).ClearTaskSet source ( itt_me.fromPrim 0u )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 16 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.LogicalUnitReset_001() =
        let mutable cnt = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun _ _ _ _ ->
            Assert.Fail __LINE__
            cnt <- cnt + 1
        )

        sm.p_NotifyLUReset <- ( fun _ _ -> () )
        media.p_Terminate <- ( fun () -> () )

        pc.Invoke( "NotifyLUReset", source, itt_me.fromPrim 0u ) :?> unit

        ( lu :> ILU ).LogicalUnitReset ( ValueSome source ) ( ValueSome( itt_me.fromPrim 0u ) )

        Assert.True(( cnt = 0 ))


    [<Fact>]
    member this.LogicalUnitReset_002() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.True(( pdu.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ))
            let tmpdu = pdu :?> TaskManagementFunctionResponsePDU
            Assert.True(( source = cid_me.fromPrim 0us ))
            Assert.True(( cnd = concnt_me.fromPrim 0 ))
            Assert.True(( tmpdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))
            Assert.True(( tmpdu.InitiatorTaskTag = itt_me.fromPrim 0u ))
            Assert.True(( lun = lun_me.zero ))
            cnt1 <- cnt1 + 1
            sema1.Release() |> ignore
        )

        let cnt = Array.zeroCreate<int> 10
        let testTasks1 = [|
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.True( flg )
                    cnt.[0] <- cnt.[0] + 1
                ),
                p_GetSource = ( fun () -> 
                    {
                        I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                        CID = cid_me.fromPrim 1us;
                        ConCounter = concnt_me.fromPrim 1;
                        TSIH = tsih_me.fromPrim 0us;
                        ProtocolService = new CProtocolService_Stub();
                        SessionKiller = new HKiller()
                    }
                )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt.[1] <- cnt.[1] + 1
                ),
                p_GetSource = ( fun () -> source )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt.[2] <- cnt.[2] + 1
                ),
                p_GetSource = ( fun () -> source )
            );
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.True( flg )
                    cnt.[3] <- cnt.[3] + 1
                ),
                p_GetSource = ( fun () -> 
                    {
                        I_TNexus = new ITNexus( "INIT_3", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                        CID = cid_me.fromPrim 1us;
                        ConCounter = concnt_me.fromPrim 1;
                        TSIH = tsih_me.fromPrim 0us;
                        ProtocolService = new CProtocolService_Stub();
                        SessionKiller = new HKiller()
                    }
                )
            );
        |]
        let testTasks1 = [|
            TaskStatus.TASK_STAT_Running( testTasks1.[0] )
            TaskStatus.TASK_STAT_Dormant( testTasks1.[1] )
            TaskStatus.TASK_STAT_Running( testTasks1.[2] )
            TaskStatus.TASK_STAT_Dormant( testTasks1.[3] )
        |]
        let queue1 = {
            Queue = testTasks1.ToImmutableArray();
            ACA = ValueSome( 
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }
        pc.SetField( "m_TaskSet", queue1 )

        sm.p_NotifyLUReset <- ( fun lun arglu ->
            Assert.True(( lun = lun_me.zero ))
            Assert.True(( arglu = ( lu :> ILU ) ))
            cnt2 <- cnt2 + 1
            sema2.Release() |> ignore
        )
        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt3 <- cnt3 + 1
            sema3.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )

        ( lu :> ILU ).LogicalUnitReset ( ValueSome source ) ( ValueSome ( itt_me.fromPrim 0u ) )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

        Assert.True(( sema3.Wait 100000 ))
        Assert.True(( cnt3 = 1 ))

        Assert.True(( cnt.[0] = 1 ))
        Assert.True(( cnt.[1] = 1 ))
        Assert.True(( cnt.[2] = 1 ))
        Assert.True(( cnt.[3] = 1 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))
        let waca = m_TaskSet.ACA
        Assert.True( waca.IsNone )

    [<Fact>]
    member this.LogicalUnitReset_004() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.Fail __LINE__
        )

        let testTask =
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt1 <- cnt1 + 1
                        sema1.Release() |> ignore
                        raise <| Exception( "" )
                    ),
                    p_GetSource = ( fun () -> source )
                )
            )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty.Add testTask;
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt2 <- cnt2 + 1
            sema2.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt3 <- cnt3 + 1
            sema3.Release() |> ignore
        )

        ( lu :> ILU ).LogicalUnitReset ( ValueSome source ) ( ValueSome ( itt_me.fromPrim 0u ) )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

        Assert.True(( sema3.Wait 100000 ))
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member this.LogicalUnitReset_005() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let mutable cnt4 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )
        let sema4 = new SemaphoreSlim( 0 )

        let testTasks = [|
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt1 <- cnt1 + 1
                        Assert.False( flg )
                        sema1.Release() |> ignore
                    ),
                    p_GetSource = ( fun () -> source ),
                    p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
                )
            );
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_NotifyTerminate = ( fun flg ->
                        cnt2 <- cnt2 + 1
                        Assert.True( flg )
                        sema2.Release() |> ignore
                        raise <| Exception( "" )
                    ),
                    p_GetSource = ( fun () ->
                        {
                            I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                            CID = cid_me.fromPrim 1us;
                            ConCounter = concnt_me.fromPrim 1;
                            TSIH = tsih_me.fromPrim 0us;
                            ProtocolService = new CProtocolService_Stub();
                            SessionKiller = new HKiller()
                        }
                    )
                )
            );
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt3 <- cnt3 + 1
            sema3.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt4 <- cnt4 + 1
            sema4.Release() |> ignore
        )

        ( lu :> ILU ).LogicalUnitReset ( ValueSome source ) ( ValueSome ( itt_me.fromPrim 0u ) )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

        Assert.True(( sema3.Wait 100000 ))
        Assert.True(( cnt3 = 1 ))

        Assert.True(( sema4.Wait 100000 ))
        Assert.True(( cnt4 = 1 ))

    [<Fact>]
    member this.LogicalUnitReset_006() =
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let mutable cnt4 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )
        let sema4 = new SemaphoreSlim( 0 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun source cnd pdu lun ->
            Assert.True(( pdu.Opcode = OpcodeCd.SCSI_TASK_MGR_RES ))
            let tmpdu = pdu :?> TaskManagementFunctionResponsePDU
            Assert.True(( source = cid_me.fromPrim 0us ))
            Assert.True(( cnd = concnt_me.fromPrim 0 ))
            Assert.True(( tmpdu.Response = TaskMgrResCd.FUNCTION_COMPLETE ))
            Assert.True(( tmpdu.InitiatorTaskTag = itt_me.fromPrim 0u ))
            Assert.True(( lun = lun_me.zero ))
            cnt1 <- cnt1 + 1
            sema1.Release() |> ignore
        )

        let testTask =
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt2 <- cnt2 + 1
                    if cnt2 = 16 then
                        sema2.Release() |> ignore
                ),
                p_GetSource = ( fun () -> source )
            )
        let testTasks = [|
            for i = 0 to 15 do
                yield TaskStatus.TASK_STAT_Running( testTask )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt3 <- cnt3 + 1
            sema3.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt4 <- cnt4 + 1
            sema4.Release() |> ignore
        )

        ( lu :> ILU ).LogicalUnitReset ( ValueSome source ) ( ValueSome ( itt_me.fromPrim 0u ) )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 16 ))

        Assert.True(( sema3.Wait 100000 ))
        Assert.True(( cnt3 = 1 ))

        Assert.True(( sema4.Wait 100000 ))
        Assert.True(( cnt4 = 1 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.NotifyLUReset_001() =
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let mutable cnt4 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        let testTask =
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt2 <- cnt2 + 1
                ),
                p_GetSource = ( fun () -> source )
            )
        let testTasks = [|
            for i = 0 to 15 do
                yield TaskStatus.TASK_STAT_Running( testTask )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt3 <- cnt3 + 1
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt4 <- cnt4 + 1
        )

        pc.Invoke( "NotifyLUReset", source, itt_me.fromPrim 0u ) :?> unit

        Assert.True(( cnt2 = 16 ))
        Assert.True(( cnt3 = 1 ))
        Assert.True(( cnt4 = 1 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.NotifyLUReset_002() =
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let mutable cnt4 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        let testTask =
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.True( flg )
                    cnt2 <- cnt2 + 1
                ),
                p_GetSource = ( fun () ->
                    {
                        I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                        CID = cid_me.fromPrim 1us;
                        ConCounter = concnt_me.fromPrim 1;
                        TSIH = tsih_me.fromPrim 0us;
                        ProtocolService = new CProtocolService_Stub();
                        SessionKiller = new HKiller()
                    }
                )
            )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty.Add( TaskStatus.TASK_STAT_Running( testTask ) );
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt3 <- cnt3 + 1
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt4 <- cnt4 + 1
        )

        pc.Invoke( "NotifyLUReset", source, itt_me.fromPrim 0u ) :?> unit

        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt3 = 1 ))
        Assert.True(( cnt4 = 1 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.NotifyLUReset_003() =
        let mutable cnt3 = 0
        let mutable cnt4 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt3 <- cnt3 + 1
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt4 <- cnt4 + 1
        )

        pc.Invoke( "NotifyLUReset", source, itt_me.fromPrim 0u ) :?> unit

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

        Assert.True(( cnt3 = 1 ))
        Assert.True(( cnt4 = 1 ))

        pc.Invoke( "NotifyLUReset", source, itt_me.fromPrim 0u ) :?> unit

        Assert.True(( cnt3 = 1 ))
        Assert.True(( cnt4 = 1 ))

    [<Fact>]
    member this.NotifyLUReset_004() =
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let mutable cnt4 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        let testTask =
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_NotifyTerminate = ( fun flg ->
                    Assert.False( flg )
                    cnt2 <- cnt2 + 1
                    raise <| Exception( "" )
                ),
                p_GetSource = ( fun () -> source )
            )
        let testTasks = [|
            for i = 0 to 15 do
                TaskStatus.TASK_STAT_Running( testTask )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt3 <- cnt3 + 1
            raise <| Exception( "" )
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt4 <- cnt4 + 1
        )

        pc.Invoke( "NotifyLUReset", source, itt_me.fromPrim 0u ) :?> unit

        Assert.True(( cnt2 = 16 ))
        Assert.True(( cnt3 = 1 ))
        Assert.True(( cnt4 = 1 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.DeleteTask_001() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        let testTasks1 = [|
            for i = 0 to 15 do
                yield new CBlockDeviceTask_Stub(
                    dummy = box i,
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim ( uint i ) ),
                    p_GetSource = ( fun () -> source )
                );
        |]
        let testTasks2 = [|
            for i = 0 to 15 do
                yield TaskStatus.TASK_STAT_Running( testTasks1.[i] )
        |]

        for j = 0 to 15 do
            let queue1 = {
                Queue = testTasks2.ToImmutableArray();
                ACA = ValueNone;
            }

            let queue2 =
                pc.Invoke( "DeleteTask", testTasks1.[j], queue1 ) :?> TaskSet

            Assert.True(( queue2.Queue.Length = 15 ))
            for i = 0 to 14 do
                if i < j then
                    let witt = ( TaskStatus.getTask  queue2.Queue.[i] ).InitiatorTaskTag
                    Assert.True(( witt = itt_me.fromPrim ( uint i ) ))
                if i >= j then
                    let witt = ( TaskStatus.getTask queue2.Queue.[i] ).InitiatorTaskTag
                    Assert.True(( witt = itt_me.fromPrim ( uint i + 1u ) ))

    [<Fact>]
    member this.DeleteTask_002() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        let testTasks1 = [|
            for i = 0 to 15 do
                yield new CBlockDeviceTask_Stub();
        |]
        let testTasks2 = [|
            for i = 0 to 15 do
                yield TaskStatus.TASK_STAT_Running( testTasks1.[i] )
        |]
        let queue1 = {
            Queue = testTasks2.ToImmutableArray();
            ACA = ValueNone;
        }

        let wtask =
            new CBlockDeviceTask_Stub(
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_GetSource = ( fun () -> source )
            );

        let queue2 =
            pc.Invoke( "DeleteTask", wtask, queue1 ) :?> TaskSet

        Assert.True(( queue2.Queue.Length = 16 ))
        for i = 0 to 15 do
            Assert.True(( ( TaskStatus.getTask queue2.Queue.[i] ) = testTasks1.[i] ))

    [<Fact>]
    member this.DeleteTask_003() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueNone;
        }
        let wtask =
            new CBlockDeviceTask_Stub(
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                p_GetSource = ( fun () -> source )
            );
        let queue2 =
            pc.Invoke( "DeleteTask", wtask, queue1 ) :?> TaskSet

        Assert.True(( queue2.Queue.Length = 0 ))

    [<Fact>]
    member this.CheckUnitAttentionStatus_001() =
        let media, sm, lu = this.createBlockDevice()
        let pc = new PrivateCaller( lu )
        let ua = pc.GetField( "m_UnitAttention" ) :?> ConcurrentDictionary< string, SCSIACAException >

        let wtask = 
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetCDB = ( fun () ->
                    {    // CDBTypes.Inquiry
                        OperationCode = 0x12uy;
                        EVPD = false;
                        PageCode = 0uy;
                        AllocationLength = 0us;
                        Control = 0uy;
                    } :> ICDB |> ValueSome
                ),
                p_GetSource = ( fun () -> BlockDeviceLU_Test.cmdSource() ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u )
            );

        ua.Clear()
        let r = pc.Invoke( "CheckUnitAttentionStatus", wtask ) :?> SCSIACAException voption
        Assert.True( r.IsNone )

    [<Fact>]
    member this.CheckUnitAttentionStatus_002() =
        let media, sm, lu = this.createBlockDevice()
        let pc = new PrivateCaller( lu )
        let ua = pc.GetField( "m_UnitAttention" ) :?> ConcurrentDictionary< string, SCSIACAException >

        let wtask = 
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetCDB = ( fun () ->
                    {    // CDBTypes.ReportLUNs
                        OperationCode = 0xA0uy;
                        SelectReport = 0uy;
                        AllocationLength = 0u;
                        Control = 0uy;
                    } :> ICDB |> ValueSome
                ),
                p_GetSource = ( fun () -> BlockDeviceLU_Test.cmdSource() ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u )
            );

        ua.Clear()
        let r = pc.Invoke( "CheckUnitAttentionStatus", wtask ) :?> SCSIACAException voption
        Assert.True( r.IsNone )

    [<Fact>]
    member this.CheckUnitAttentionStatus_003() =
        let media, sm, lu = this.createBlockDevice()
        let pc = new PrivateCaller( lu )
        let ua = pc.GetField( "m_UnitAttention" ) :?> ConcurrentDictionary< string, SCSIACAException >

        let wtask = 
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetCDB = ( fun () ->
                    {    // CDBTypes.RequestSense
                        OperationCode = 0x03uy;
                        DESC = false;
                        AllocationLength = 0uy;
                        Control = 0uy;
                    } :> ICDB |> ValueSome
                ),
                p_GetSource = ( fun () -> BlockDeviceLU_Test.cmdSource() ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u )
            );

        ua.Clear()
        let r = pc.Invoke( "CheckUnitAttentionStatus", wtask ) :?> SCSIACAException voption
        Assert.True( r.IsNone )

    [<Fact>]
    member this.CheckUnitAttentionStatus_004() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let ua = pc.GetField( "m_UnitAttention" ) :?> ConcurrentDictionary< string, SCSIACAException >

        let wtask = 
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetCDB = ( fun () ->
                    { // TestUnitReadyCDB
                        OperationCode = 0uy;
                        Control = 0uy;
                    } :> ICDB |> ValueSome
                ),
                p_GetSource = ( fun () ->
                    {
                        source with
                            I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                    }
                ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u )
            );

        ua.Clear()
        let r = pc.Invoke( "CheckUnitAttentionStatus", wtask ) :?> SCSIACAException voption
        Assert.True( r.IsNone )

    [<Fact>]
    member this.CheckUnitAttentionStatus_005() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let ua = pc.GetField( "m_UnitAttention" ) :?> ConcurrentDictionary< string, SCSIACAException >

        let wtask = 
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetCDB = ( fun () ->
                    { // TestUnitReadyCDB
                        OperationCode = 0uy;
                        Control = 0uy;
                    } :> ICDB |> ValueSome
                ),
                p_GetSource = ( fun () ->
                    {
                        source with
                            I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                    }
                ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u )
            );

        ua.Clear()
        ua.TryAdd(
            source.I_TNexus.InitiatorPortName,
            new SCSIACAException(
                source,
                ScsiCmdStatCd.CHECK_CONDITION,
                new SenseData(
                    true,
                    SenseKeyCd.ILLEGAL_REQUEST,
                    ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                    ""
                ),
                ""
            )
        )
        |> ignore
        let r = pc.Invoke( "CheckUnitAttentionStatus", wtask ) :?> SCSIACAException voption
        Assert.True( r.IsNone )

    [<Fact>]
    member this.CheckUnitAttentionStatus_006() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let ua = pc.GetField( "m_UnitAttention" ) :?> ConcurrentDictionary< string, SCSIACAException >

        let wtask = 
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetCDB = ( fun () ->
                    { // TestUnitReadyCDB
                        OperationCode = 0uy;
                        Control = 0uy;
                    } :> ICDB |> ValueSome
                ),
                p_GetSource = ( fun () -> source ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u )
            );

        ua.Clear()
        ua.TryAdd(
            ( new ITNexus( "INIT", isid_me.zero, "TARG", tpgt_me.fromPrim 0us ) ).InitiatorPortName,
            new SCSIACAException(
                source,
                ScsiCmdStatCd.CHECK_CONDITION,
                new SenseData(
                    true,
                    SenseKeyCd.ILLEGAL_REQUEST,
                    ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                    ""
                ),
                ""
            )
        )
        |> ignore
        let r = pc.Invoke( "CheckUnitAttentionStatus", wtask ) :?> SCSIACAException voption
        Assert.True( r.Value.Status = ScsiCmdStatCd.CHECK_CONDITION )

    [<Fact>]
    member this.CheckUnitAttentionStatus_007() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let ua = pc.GetField( "m_UnitAttention" ) :?> ConcurrentDictionary< string, SCSIACAException >

        let wtask = 
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetCDB = ( fun () ->
                    { // TestUnitReadyCDB
                        OperationCode = 0uy;
                        Control = 0uy;
                    } :> ICDB |> ValueSome
                ),
                p_GetSource = ( fun () -> {
                        source with
                            I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                    }
                ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u )
            );

        ua.Clear()
        ua.TryAdd(
            source.I_TNexus.InitiatorPortName,
            BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
        )
        |> ignore
        ua.TryAdd(
            ( new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us ) ).InitiatorPortName,
            new SCSIACAException(
                {
                    source with
                        I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                },
                ScsiCmdStatCd.TASK_ABORTED,
                new SenseData(
                    true,
                    SenseKeyCd.ILLEGAL_REQUEST,
                    ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                    ""
                ),
                ""
            )
        )
        |> ignore
        let r = pc.Invoke( "CheckUnitAttentionStatus", wtask ) :?> SCSIACAException voption
        Assert.True( r.Value.Status = ScsiCmdStatCd.TASK_ABORTED )


    [<Fact>]
    member this.LUResetStatus_001() =
        let mutable cnt = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        sm.p_NotifyLUReset <- ( fun _ _ -> () )
        media.p_Terminate <- ( fun () -> () )

        Assert.False(( ( lu :> ILU ).LUResetStatus ))
        pc.Invoke( "NotifyLUReset", source, itt_me.fromPrim 0u ) :?> unit
        Assert.True(( ( lu :> ILU ).LUResetStatus ))

    [<Fact>]
    member this.GetUnitAttention_001() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let ua = pc.GetField( "m_UnitAttention" ) :?> ConcurrentDictionary< string, SCSIACAException >

        ua.Clear()
        ua.TryAdd(
            source.I_TNexus.InitiatorPortName,
            BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
        )
        |> ignore
        ua.TryAdd(
            ( new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us ) ).InitiatorPortName,
            new SCSIACAException(
                {
                    source with
                        I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                },
                ScsiCmdStatCd.TASK_ABORTED,
                new SenseData(
                    true,
                    SenseKeyCd.ILLEGAL_REQUEST,
                    ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                    ""
                ),
                ""
            )
        )
        |> ignore

        let inlu = lu :> IInternalLU
        let r1 =
            inlu.GetUnitAttention( ( new ITNexus( "INIT", isid_me.zero, "TARG", tpgt_me.fromPrim 0us ) ) )
        Assert.True( r1.IsSome )
        Assert.True(( r1.Value.Status = ScsiCmdStatCd.CHECK_CONDITION ))

        let r2 =
            inlu.GetUnitAttention( ( new ITNexus( "INIT", isid_me.fromElem 1uy 1uy 2us 3uy 4us, "TARG", tpgt_me.fromPrim 5us ) ) )
        Assert.True( r2.IsNone )

    [<Fact>]
    member this.GetUnitAttention_002() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let ua = pc.GetField( "m_UnitAttention" ) :?> ConcurrentDictionary< string, SCSIACAException >

        ua.Clear()
        let inlu = lu :> IInternalLU
        let r =
            inlu.GetUnitAttention( ( new ITNexus( "INIT", isid_me.zero, "TARG", tpgt_me.fromPrim 0us ) ) )
        Assert.True( r.IsNone )

    [<Fact>]
    member this.ClearUnitAttention_001() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let ua = pc.GetField( "m_UnitAttention" ) :?> ConcurrentDictionary< string, SCSIACAException >
        let inlu = lu :> IInternalLU

        ua.Clear()

        inlu.ClearUnitAttention( ( new ITNexus( "INIT", isid_me.zero, "TARG", tpgt_me.fromPrim 0us ) ) )
        Assert.True(( ua.Count = 0 ))

        ua.TryAdd(
            source.I_TNexus.InitiatorPortName,
            BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
        )
        |> ignore
        ua.TryAdd(
            ( new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us ) ).InitiatorPortName,
            new SCSIACAException(
                {
                    source with
                        I_TNexus = new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us );
                },
                ScsiCmdStatCd.TASK_ABORTED,
                new SenseData(
                    true,
                    SenseKeyCd.ILLEGAL_REQUEST,
                    ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                    ""
                ),
                ""
            )
        )
        |> ignore

        Assert.True(( ua.Count = 2 ))
        inlu.ClearUnitAttention( ( new ITNexus( "INIT", isid_me.zero, "TARG", tpgt_me.fromPrim 0us ) ) )
        Assert.True(( ua.Count = 1 ))

        inlu.ClearUnitAttention( ( new ITNexus( "INIT", isid_me.zero, "TARG_ABC", tpgt_me.fromPrim 0us ) ) )
        Assert.True(( ua.Count = 1 ))


    [<Fact>]
    member this.EstablishUnitAttention_001() =
        let media, sm, lu = this.createBlockDevice()
        let pc = new PrivateCaller( lu )
        let ua = pc.GetField( "m_UnitAttention" ) :?> ConcurrentDictionary< string, SCSIACAException >
        let inlu = lu :> IInternalLU

        ua.Clear()

        let iportn_1 = ( new ITNexus( "INIT_1", isid_me.zero, "TARG", tpgt_me.fromPrim 0us ) ).InitiatorPortName
        let iportn_2 = ( new ITNexus( "INIT_2", isid_me.zero, "TARG", tpgt_me.fromPrim 0us ) ).InitiatorPortName
        let ex_1 = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
        let ex_2 = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
        inlu.EstablishUnitAttention iportn_1 ex_1
        Assert.True(( ua.Count = 1 ))

        inlu.EstablishUnitAttention iportn_2 ex_2
        Assert.True(( ua.Count = 2 ))

        inlu.EstablishUnitAttention iportn_1 ex_1
        Assert.True(( ua.Count = 2 ))

    [<Fact>]
    member this.NotifyTerminateTask_001() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let cdb = { OperationCode = 0uy; Control = 0x00uy }
        let w = new SemaphoreSlim( 0 )
        let mutable cnt = 0
        let inlu = lu :> IInternalLU

        let taskStub_1 = new CBlockDeviceTask_Stub()
        taskStub_1.p_GetSource <- ( fun () -> source )
        taskStub_1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        taskStub_1.p_ReleasePooledBuffer <- id

        let taskStub_2 = new CBlockDeviceTask_Stub()
        taskStub_2.p_GetSource <- ( fun () -> source )
        taskStub_2.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        taskStub_2.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        taskStub_2.p_GetCDB <- ( fun () -> ValueSome cdb )
        taskStub_2.p_Execute <- ( fun () ->
            fun () -> task {
                cnt <- cnt + 1
                w.Release() |> ignore
            }
        )
        taskStub_2.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
        taskStub_2.p_ReleasePooledBuffer <- id

        let testTask = [|
            yield TaskStatus.TASK_STAT_Dormant( taskStub_1 :> IBlockDeviceTask );
            yield TaskStatus.TASK_STAT_Dormant( taskStub_2 :> IBlockDeviceTask );
        |]
        let queue1 = {
            Queue = testTask.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        inlu.NotifyTerminateTask taskStub_1

        w.Wait()
        Assert.True(( cnt = 1 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 1 ))

    [<Fact>]
    member this.NotifyTerminateTaskWithException_001() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let inlu = lu :> IInternalLU
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )

        sm.p_NotifyLUReset <- ( fun _ _ ->
            cnt1 <- cnt1 + 1
            sema1.Release() |> ignore
        )
        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt2 <- cnt2 + 1
            sema2.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )

        let testTasks =[|
            for i = 0 to 15 do
                let dt = new CBlockDeviceTask_Stub()
                dt.p_GetSource <- ( fun () -> source )
                dt.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
                yield TaskStatus.TASK_STAT_Dormant( dt :> IBlockDeviceTask )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        inlu.NotifyTerminateTaskWithException ( TaskStatus.getTask testTasks.[0] ) ( new Exception() )

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.NotifyTerminateTaskWithException_002() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let inlu = lu :> IInternalLU
        let cdb = { OperationCode = 0uy; Control = 0x00uy }
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let w = new SemaphoreSlim(1)

        let testTasks = [|
            for i = 0 to 15 do
                let dt = new CBlockDeviceTask_Stub()
                dt.p_GetSource <- ( fun () -> source )
                dt.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
                dt.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
                dt.p_GetCDB <- ( fun () -> ValueSome cdb )
                dt.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
                dt.p_GetReceivedDataLength <- ( fun () -> 0u )
                dt.p_Execute <- ( fun () ->
                    cnt1 <- cnt1 + 1
                    fun () -> Task.FromResult()
                )
                dt.p_ReleasePooledBuffer <- id
                yield TaskStatus.TASK_STAT_Dormant( dt :> IBlockDeviceTask )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            cnt2 <- cnt2 + 1
            w.Release() |> ignore
        )

        w.Wait()

        let ex = new SCSIACAException ( source, true, SenseKeyCd.ABORTED_COMMAND, ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT, "" )
        inlu.NotifyTerminateTaskWithException ( TaskStatus.getTask testTasks.[0] ) ex

        w.Wait()

        Assert.True(( cnt1 = 15 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member this.NotifyTerminateTaskWithException_003() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let inlu = lu :> IInternalLU
        let cdb = { OperationCode = 0uy; Control = 0x00uy }
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let sema1 = new SemaphoreSlim( 0 )
        let sema2 = new SemaphoreSlim( 0 )

        let testTasks = [|
            let dt1 = new CBlockDeviceTask_Stub()
            dt1.p_GetSource <- ( fun () -> source )
            dt1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
            dt1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
            dt1.p_GetCDB <- ( fun () -> ValueSome cdb )
            dt1.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
            yield TaskStatus.TASK_STAT_Running( dt1 :> IBlockDeviceTask )

            let dt2 = new CBlockDeviceTask_Stub()
            dt2.p_GetSource <- ( fun () -> source )
            dt2.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
            dt2.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
            dt2.p_GetCDB <- ( fun () -> ValueSome cdb )
            dt2.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
            dt2.p_Execute <- ( fun () ->
                raise <| SCSIACAException ( source, true, SenseKeyCd.ABORTED_COMMAND, ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT, "" )
                fun () -> Task.FromResult()
            )
            yield TaskStatus.TASK_STAT_Dormant( dt2 :> IBlockDeviceTask )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        sm.p_NotifyLUReset <- ( fun _ _ ->
            cnt1 <- cnt1 + 1
            sema1.Release() |> ignore
        )
        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt2 <- cnt2 + 1
            sema2.Release() |> ignore
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ -> () )

        let ex = new SCSIACAException ( source, true, SenseKeyCd.ABORTED_COMMAND, ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT, "" )
        inlu.NotifyTerminateTaskWithException ( TaskStatus.getTask testTasks.[0] ) ex

        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))

        Assert.True(( sema2.Wait 100000 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member this.AbortTasksFromSpecifiedITNexus_001() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let inlu = lu :> IInternalLU

        let testTask =
            let dt1 = new CBlockDeviceTask_Stub()
            dt1.p_GetSource <- ( fun () -> source )
            dt1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
            TaskStatus.TASK_STAT_Running( dt1 :> IBlockDeviceTask )

        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty.Add( testTask );
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        inlu.AbortTasksFromSpecifiedITNexus ( TaskStatus.getTask testTask ) [| source.I_TNexus |] true

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 1 ))


    [<Fact>]
    member this.AbortTasksFromSpecifiedITNexus_002() =
        let media, sm, lu = this.createBlockDevice()
        let pc = new PrivateCaller( lu )
        let inlu = lu :> IInternalLU
        let itn1 = ITNexus( "INIT1", isid_me.zero, "TARG1", tpgt_me.fromPrim 0us );
        let itn2 = ITNexus( "INIT2", isid_me.zero, "TARG2", tpgt_me.fromPrim 0us );
        let itn3 = ITNexus( "INIT3", isid_me.zero, "TARG3", tpgt_me.fromPrim 0us );
        let itn4 = ITNexus( "INIT4", isid_me.zero, "TARG4", tpgt_me.fromPrim 0us );
        let source1 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn1 }
        let source2 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn2 }
        let source3 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn3 }
        let source4 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn4 }
        let vcnt = Array.zeroCreate<int> 6

        let self = new CBlockDeviceTask_Stub(
            p_GetSource = ( fun () -> source1 ),
            p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
            p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
            p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
        )

        let testTasks = [|
            yield TaskStatus.TASK_STAT_Running( self :> IBlockDeviceTask )

            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source1 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[1] <- vcnt.[1] + 1 )
                    )
                )

            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source2 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[2] <- vcnt.[2] + 1 )
                    )
                )

            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source3 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[3] <- vcnt.[3] + 1 )
                    )
                )

            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source4 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[4] <- vcnt.[4] + 1 )
                    )
                )

            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source4 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[5] <- vcnt.[5] + 1 )
                    )
                )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        inlu.AbortTasksFromSpecifiedITNexus self [| itn1; itn2; itn3 |] false

        Assert.True(( vcnt.[0] = 0 ))
        Assert.True(( vcnt.[1] = 0 ))
        Assert.True(( vcnt.[2] = 1 ))
        Assert.True(( vcnt.[3] = 1 ))
        Assert.True(( vcnt.[4] = 0 ))
        Assert.True(( vcnt.[5] = 0 ))

    [<Fact>]
    member this.AbortTasksFromSpecifiedITNexus_003() =
        let media, sm, lu = this.createBlockDevice()
        let pc = new PrivateCaller( lu )
        let inlu = lu :> IInternalLU
        let itn1 = ITNexus( "INIT1", isid_me.zero, "TARG1", tpgt_me.fromPrim 0us );
        let itn2 = ITNexus( "INIT2", isid_me.zero, "TARG2", tpgt_me.fromPrim 0us );
        let itn3 = ITNexus( "INIT3", isid_me.zero, "TARG3", tpgt_me.fromPrim 0us );
        let itn4 = ITNexus( "INIT4", isid_me.zero, "TARG4", tpgt_me.fromPrim 0us );
        let source1 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn1 }
        let source2 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn2 }
        let source3 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn3 }
        let source4 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn4 }
        let vcnt = Array.zeroCreate<int> 6

        let self = new CBlockDeviceTask_Stub(
            p_GetSource = ( fun () -> source1 ),
            p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
            p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
            p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
        )
        let testTasks = [|
            yield TaskStatus.TASK_STAT_Running( self :> IBlockDeviceTask )

            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source1 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[1] <- vcnt.[1] + 1 )
                    )
                )

            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source2 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[2] <- vcnt.[2] + 1 )
                    )
                )

            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source3 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[3] <- vcnt.[3] + 1 )
                    )
                )

            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source4 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[4] <- vcnt.[4] + 1 )
                    )
                )

            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source4 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[5] <- vcnt.[5] + 1 )
                    )
                )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        inlu.AbortTasksFromSpecifiedITNexus self [| itn1; itn2; itn3 |] true

        Assert.True(( vcnt.[0] = 0 ))
        Assert.True(( vcnt.[1] = 0 ))
        Assert.True(( vcnt.[2] = 1 ))
        Assert.True(( vcnt.[3] = 1 ))
        Assert.True(( vcnt.[4] = 0 ))
        Assert.True(( vcnt.[5] = 1 ))

    [<Fact>]
    member this.AbortTasksFromSpecifiedITNexus_004() =
        let media, sm, lu = this.createBlockDevice()
        let pc = new PrivateCaller( lu )
        let inlu = lu :> IInternalLU
        let itn1 = ITNexus( "INIT1", isid_me.zero, "TARG1", tpgt_me.fromPrim 0us );
        let itn2 = ITNexus( "INIT2", isid_me.zero, "TARG2", tpgt_me.fromPrim 0us );
        let itn3 = ITNexus( "INIT3", isid_me.zero, "TARG3", tpgt_me.fromPrim 0us );
        let itn4 = ITNexus( "INIT4", isid_me.zero, "TARG4", tpgt_me.fromPrim 0us );
        let source1 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn1 }
        let source2 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn2 }
        let source3 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn3 }
        let source4 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn4 }
        let vcnt = Array.zeroCreate<int> 6

        let self = new CBlockDeviceTask_Stub(
            p_GetSource = ( fun () -> source1 ),
            p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
            p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
            p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
        )
        let testTasks = [|
            yield TaskStatus.TASK_STAT_Running( self :> IBlockDeviceTask )
            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source1 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[1] <- vcnt.[1] + 1 )
                    )
                )
            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source2 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[2] <- vcnt.[2] + 1 )
                    )
                )
            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source3 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[3] <- vcnt.[3] + 1 )
                    )
                )
            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source4 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[4] <- vcnt.[4] + 1 )
                    )
                )
            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetSource = ( fun () -> source4 ),
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK ),
                        p_NotifyTerminate = ( fun _ -> vcnt.[5] <- vcnt.[5] + 1 )
                    )
                )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        inlu.AbortTasksFromSpecifiedITNexus self Array.empty false

        Assert.True(( vcnt.[0] = 0 ))
        Assert.True(( vcnt.[1] = 0 ))
        Assert.True(( vcnt.[2] = 0 ))
        Assert.True(( vcnt.[3] = 0 ))
        Assert.True(( vcnt.[4] = 0 ))
        Assert.True(( vcnt.[5] = 0 ))

    [<Fact>]
    member _.FindQueueByITT_001() =
        let itn1 = ITNexus( "INIT1", isid_me.zero, "TARG1", tpgt_me.fromPrim 0us );
        let itn2 = ITNexus( "INIT2", isid_me.zero, "TARG2", tpgt_me.fromPrim 0us );
        let src1 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn1 }
        let src2 = { BlockDeviceLU_Test.cmdSource() with I_TNexus = itn2 }
        let testTasks =
            [|
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                        p_GetSource = ( fun () -> src1 )
                    ) :> IBlockDeviceTask
                );
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                        p_GetSource = ( fun () -> src1 )
                    ) :> IBlockDeviceTask
                );
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 2u ),
                        p_GetSource = ( fun () -> src1 )
                    ) :> IBlockDeviceTask
                );
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 3u ),
                        p_GetSource = ( fun () -> src1 )
                    ) :> IBlockDeviceTask
                );
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 4u ),
                        p_GetSource = ( fun () -> src1 )
                    ) :> IBlockDeviceTask
                );
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 5u ),
                        p_GetSource = ( fun () -> src2 )
                    ) :> IBlockDeviceTask
                );
            |]
        let immTestTasks = testTasks.ToImmutableArray()

        let r1 =
            PrivateCaller.Invoke< BlockDeviceLU >( "FindQueueByITT", [| immTestTasks :> obj; src1 :> obj; ( itt_me.fromPrim 0u ) :> obj; |] ) :?> int
        Assert.True(( r1 = 0 ))

        let r2 =
            PrivateCaller.Invoke< BlockDeviceLU >( "FindQueueByITT", [| immTestTasks :> obj; src1 :> obj; ( itt_me.fromPrim 4u ) :> obj; |] ) :?> int
        Assert.True(( r2 = 4 ))

        let r3 =
            PrivateCaller.Invoke< BlockDeviceLU >( "FindQueueByITT", [| immTestTasks :> obj; src1 :> obj; ( itt_me.fromPrim 5u ) :> obj; |] ) :?> int
        Assert.True(( r3 = -1 ))

        let r4 =
            PrivateCaller.Invoke< BlockDeviceLU >( "FindQueueByITT", [| immTestTasks :> obj; src2 :> obj; ( itt_me.fromPrim 5u ) :> obj; |] ) :?> int
        Assert.True(( r4 = 5 ))

    [<Fact>]
    member _.FindQueueByITT_002() =
        let tasks = ImmutableArray< TaskStatus >.Empty
        let source = BlockDeviceLU_Test.cmdSource()
        let r1 =
            PrivateCaller.Invoke< BlockDeviceLU >( "FindQueueByITT", [| tasks :> obj; source :> obj; ( itt_me.fromPrim 0u ) :> obj; |] ) :?> int
        Assert.True(( r1 = -1 ))

    [<Fact>]
    member this.CheckOverlappedTask_001() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        pc.Invoke( "CheckOverlappedTask", ImmutableArray< TaskStatus >.Empty, source, itt_me.fromPrim 0u ) |> ignore

    [<Fact>]
    member this.CheckOverlappedTask_002() =
        let mutable cnt = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        let testTasks = [|
            for i = 0 to 15 do
                yield
                    TaskStatus.TASK_STAT_Running(
                        new CBlockDeviceTask_Stub(
                            p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim ( uint i ) ),
                            p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                            p_GetSource = ( fun () -> source ),
                            p_NotifyTerminate = ( fun flg ->
                                Assert.False( flg )
                                cnt <- cnt + 1
                            )
                        ) :> IBlockDeviceTask
                    )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }

        try
            pc.Invoke( "CheckOverlappedTask", testTasks.ToImmutableArray(), source, itt_me.fromPrim 0u ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))

    [<Fact>]
    member this.CheckOverlappedTask_003() =
        let mutable cnt = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        let testTasks = [|
            for i = 0 to 15 do
                yield
                    TaskStatus.TASK_STAT_Running(
                        new CBlockDeviceTask_Stub(
                            p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim ( uint i ) ),
                            p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                            p_GetSource = ( fun () -> source ),
                            p_NotifyTerminate = ( fun flg ->
                                Assert.False( flg )
                                cnt <- cnt + 1
                            )
                        ) :> IBlockDeviceTask
                    )
        |]
        pc.Invoke( "CheckOverlappedTask", testTasks.ToImmutableArray(), source, itt_me.fromPrim 0xFFFFFFFFu ) |> ignore


    [<Fact>]
    member this.CheckDuplicateACATask_001() =
        let media, sm, lu = this.createBlockDevice()
        let pc = new PrivateCaller( lu )
        Assert.False( pc.Invoke( "CheckDuplicateACATask", ImmutableArray< TaskStatus >.Empty ) :?> bool )

    [<Fact>]
    member this.CheckDuplicateACATask_002() =
        let media, sm, lu = this.createBlockDevice()
        let pc = new PrivateCaller( lu )
        let testTasks = [|
            for i = 0 to 15 do
                yield
                    TaskStatus.TASK_STAT_Running(
                        new CBlockDeviceTask_Stub(
                            p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                            p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
                        ) :> IBlockDeviceTask
                    )
            yield
                TaskStatus.TASK_STAT_Running(
                    new CBlockDeviceTask_Stub(
                        p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                        p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK )
                    ) :> IBlockDeviceTask
                )
        |]
        Assert.True( pc.Invoke( "CheckDuplicateACATask", testTasks.ToImmutableArray() ) :?> bool )

    [<Fact>]
    member this.CheckDuplicateACATask_003() =
        let media, sm, lu = this.createBlockDevice()
        let pc = new PrivateCaller( lu )
        let testTasks = [|
            for i = 0 to 15 do
                yield
                    TaskStatus.TASK_STAT_Running(
                        new CBlockDeviceTask_Stub(
                            p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask )
                        ) :> IBlockDeviceTask
                    )
        |]
        Assert.False( pc.Invoke( "CheckDuplicateACATask", testTasks.ToImmutableArray() ) :?> bool )


    [<Fact>]
    member this.CheckDuplicateACATask_004() =
        let media, sm, lu = this.createBlockDevice()
        let pc = new PrivateCaller( lu )
        let testTasks = [|
            for i = 0 to 15 do
                yield
                    TaskStatus.TASK_STAT_Running(
                        new CBlockDeviceTask_Stub(
                            p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                            p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
                        ) :> IBlockDeviceTask
                    )
        |]
        Assert.False( pc.Invoke( "CheckDuplicateACATask", testTasks.ToImmutableArray() ) :?> bool )

    [<Fact>]
    member this.AddNewScsiTaskToQueue_001() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK
        let cdb = { OperationCode = 0uy; Control = 0uy }
        let scsiData : SCSIDataOutPDU list = [
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 10 };
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 20 };
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 30 };
        ]
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueSome( 
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }
        let queue2 =
            pc.Invoke( "AddNewScsiTaskToQueue", source :> obj, scsiCmd :> obj, cdb :> obj, scsiData :> obj, queue1 ) :?> TaskSet

        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True(( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask ))
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 60u ))

        let pc_task = new PrivateCaller( wt )
        let status = pc_task.GetField( "m_StatCode" ) :?> ScsiCmdStatCd
        Assert.True(( status = ScsiCmdStatCd.ACA_ACTIVE ))
        let respCode = pc_task.GetField( "m_RespCode" ) :?> iScsiSvcRespCd
        Assert.True(( respCode = iScsiSvcRespCd.COMMAND_COMPLETE ))

    [<Fact>]
    member this.AddNewScsiTaskToQueue_002() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK
        let cdb = { OperationCode = 0uy; Control = 0uy }
        let scsiData : SCSIDataOutPDU list = [
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 10 };
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 20 };
        ]

        let testTask =
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                    p_GetSource = ( fun () -> source )
                ) :> IBlockDeviceTask
            )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty.Add testTask;
            ACA = ValueSome( 
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }

        let queue2 =
            pc.Invoke( "AddNewScsiTaskToQueue", source :> obj, scsiCmd :> obj, cdb :> obj, scsiData :> obj, queue1 ) :?> TaskSet

        Assert.True(( queue2.Queue.Length = 2 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[1] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[1] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 30u ))

        let pc_task = new PrivateCaller( wt )
        let status = pc_task.GetField( "m_StatCode" ) :?> ScsiCmdStatCd
        Assert.True(( status = ScsiCmdStatCd.ACA_ACTIVE ))
        let respCode = pc_task.GetField( "m_RespCode" ) :?> iScsiSvcRespCd
        Assert.True(( respCode = iScsiSvcRespCd.COMMAND_COMPLETE ))

    [<Fact>]
    member this.AddNewScsiTaskToQueue_003() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let scsiCmd = {
            BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK with
                InitiatorTaskTag = itt_me.fromPrim 0xAABBCCDDu;
        }
        let cdb = { OperationCode = 0uy; Control = 0uy }
        let scsiData : SCSIDataOutPDU list = [
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 10 };
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 20 };
        ]

        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueSome( 
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }

        let queue2 =
            pc.Invoke( "AddNewScsiTaskToQueue", source :> obj, scsiCmd :> obj, cdb :> obj, scsiData :> obj, queue1 ) :?> TaskSet

        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.ScsiTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> ScsiTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 30u ))
        let pc_task = new PrivateCaller( wt )
        let resultpdu = pc_task.GetField( "m_Command" ) :?> SCSICommandPDU
        Assert.True(( resultpdu.InitiatorTaskTag = itt_me.fromPrim 0xAABBCCDDu ))

    [<Fact>]
    member this.AddNewScsiTaskToQueue_004() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK
        let cdb = { OperationCode = 0uy; Control = 0uy }
        let scsiData : SCSIDataOutPDU list = [
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 15 };
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 20 };
        ]

        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueSome( 
                new ITNexus( "INIT2", isid_me.fromElem 1uy 2uy 3us 4uy 5us, "TARG6", tpgt_me.fromPrim 7us ),
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }

        let queue2 =
            pc.Invoke( "AddNewScsiTaskToQueue", source :> obj, scsiCmd :> obj, cdb :> obj, scsiData :> obj, queue1 ) :?> TaskSet

        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 35u ))
        let pc_task = new PrivateCaller( wt )
        let status = pc_task.GetField( "m_StatCode" ) :?> ScsiCmdStatCd
        Assert.True(( status = ScsiCmdStatCd.ACA_ACTIVE ))
        let respCode = pc_task.GetField( "m_RespCode" ) :?> iScsiSvcRespCd
        Assert.True(( respCode = iScsiSvcRespCd.COMMAND_COMPLETE ))

    [<Fact>]
    member this.AddNewScsiTaskToQueue_005() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let scsiCmd = { BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK with DataSegment = PooledBuffer.Rent 5 }
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let scsiData : SCSIDataOutPDU list = [
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 20 };
        ]

        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueSome( 
                new ITNexus( "INIT2", isid_me.fromElem 1uy 2uy 3us 4uy 5us, "TARG6", tpgt_me.fromPrim 7us ),
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }

        let queue2 =
            pc.Invoke( "AddNewScsiTaskToQueue", source :> obj, scsiCmd :> obj, cdb :> obj, scsiData :> obj, queue1 ) :?> TaskSet

        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 25u ))
        let pc_task = new PrivateCaller( wt )
        let status = pc_task.GetField( "m_StatCode" ) :?> ScsiCmdStatCd
        Assert.True(( status = ScsiCmdStatCd.ACA_ACTIVE ))
        let respCode = pc_task.GetField( "m_RespCode" ) :?> iScsiSvcRespCd
        Assert.True(( respCode = iScsiSvcRespCd.COMMAND_COMPLETE ))

    [<Fact>]
    member this.AddNewScsiTaskToQueue_006() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK
        let cdb = { OperationCode = 0uy; Control = 0x00uy }
        let scsiData : SCSIDataOutPDU list = [
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 10 };
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 20 };
        ]

        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueSome( 
                new ITNexus( "INIT2", isid_me.fromElem 1uy 2uy 3us 4uy 5us, "TARG6", tpgt_me.fromPrim 7us ),
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }

        let queue2 =
            pc.Invoke( "AddNewScsiTaskToQueue", source :> obj, scsiCmd :> obj, cdb :> obj, scsiData :> obj, queue1 ) :?> TaskSet

        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 30u ))
        let pc_task = new PrivateCaller( wt )
        let status = pc_task.GetField( "m_StatCode" ) :?> ScsiCmdStatCd
        Assert.True(( status = ScsiCmdStatCd.BUSY ))
        let respCode = pc_task.GetField( "m_RespCode" ) :?> iScsiSvcRespCd
        Assert.True(( respCode = iScsiSvcRespCd.COMMAND_COMPLETE ))

    [<Fact>]
    member this.AddNewScsiTaskToQueue_007() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK
        let cdb = { OperationCode = 0uy; Control = 0x00uy }
        let scsiData : SCSIDataOutPDU list = []
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueNone;
        }

        try
            pc.Invoke( "AddNewScsiTaskToQueue", source :> obj, scsiCmd :> obj, cdb :> obj, scsiData :> obj, queue1 ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_MESSAGE_ERROR ))

    [<Fact>]
    member this.AddNewScsiTaskToQueue_008() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let scsiCmd = {
            BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK with
                InitiatorTaskTag = itt_me.fromPrim 0xAABBCCDDu;
        }
        let cdb = { OperationCode = 0uy; Control = 0uy }
        let scsiData : SCSIDataOutPDU list = [
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 10 };
            { BlockDeviceLU_Test.defaultDataOutPDU with DataSegment = PooledBuffer.Rent 20 };
        ]
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueNone;
        }

        let queue2 =
            pc.Invoke( "AddNewScsiTaskToQueue", source :> obj, scsiCmd :> obj, cdb :> obj, scsiData :> obj, queue1 ) :?> TaskSet

        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.ScsiTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> ScsiTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 30u ))
        let pc_task = new PrivateCaller( wt )
        let resultpdu = pc_task.GetField( "m_Command" ) :?> SCSICommandPDU
        Assert.True(( resultpdu.InitiatorTaskTag = itt_me.fromPrim 0xAABBCCDDu ))

    [<Fact>]
    member this.AddNewScsiTaskToQueue_009() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let testTask =
            TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 0u ),
                    p_GetSource = ( fun () -> source )
                )
            )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty.Add testTask;
            ACA = ValueNone;
        }
        let scsiCmd = {
            BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK with
                InitiatorTaskTag = itt_me.fromPrim 0u;
        }
        let cdb = { OperationCode = 0uy; Control = 0uy }
        let scsiData : SCSIDataOutPDU list = []
        
        try
            pc.Invoke( "AddNewScsiTaskToQueue", source :> obj, scsiCmd :> obj, cdb :> obj, scsiData :> obj, queue1 ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( x.SenseKey = SenseKeyCd.ABORTED_COMMAND ))
            Assert.True(( x.ASC = ASCCd.OVERLAPPED_COMMANDS_ATTEMPTED ))

    [<Fact>]
    member this.EstablishNewACAStatus_001() =
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let exp = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK

        media.p_Terminate <- ( fun () ->
            cnt2 <- cnt2 + 1
        )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.True(( lun = lun_me.zero ))
            cnt3 <- cnt3 + 1
        )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueNone;
        }        
        try
            pc.Invoke( "EstablishNewACAStatus", source, exp, scsiCmd, 0u, true, BlockDeviceTaskType.InternalTask, queue1 ) |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ as x ->
            ()


    [<Fact>]
    member this.EstablishNewACAStatus_002() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let exp = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.BUSY SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK

        media.p_Terminate <- ( fun () ->
            Assert.Fail __LINE__
        )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.Fail __LINE__
        )

        let w = new SemaphoreSlim(1)
        w.Wait()
        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            w.Wait()
        )
        
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueNone;
        }
        let queue2 =
            pc.Invoke( "EstablishNewACAStatus", source, exp, scsiCmd, 99u, true, BlockDeviceTaskType.ScsiTask, queue1 ) :?> TaskSet

        // send raised exception to the initiator
        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 99u ))
        let pcwt = new PrivateCaller( wt )
        Assert.True(( ( pcwt.GetField( "m_StatCode" ) :?> ScsiCmdStatCd ) = ScsiCmdStatCd.BUSY ))

        w.Release() |> ignore

    [<Fact>]
    member this.EstablishNewACAStatus_003() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let exp = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK       // ACA task failed

        media.p_Terminate <- ( fun () ->
            Assert.Fail __LINE__
        )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.Fail __LINE__
        )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueNone;
        }

        let w = new SemaphoreSlim(1)
        w.Wait()
        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            w.Wait()
        )
        
        let queue2 =
            pc.Invoke( "EstablishNewACAStatus", source, exp, scsiCmd, 98u, true, BlockDeviceTaskType.ScsiTask, queue1 ) :?> TaskSet

        let waca = queue2.ACA
        Assert.True( waca.IsSome )
        Assert.True( fst( waca.Value ) = source.I_TNexus )
        Assert.True( snd( waca.Value ) = exp )

        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 98u ))
        let pcwt = new PrivateCaller( wt )
        Assert.True(( ( pcwt.GetField( "m_StatCode" ) :?> ScsiCmdStatCd ) = ScsiCmdStatCd.CHECK_CONDITION ))
        let senseData = pcwt.GetField( "m_SenseData" ) :?> SenseData option
        Assert.True(( senseData.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
        Assert.True(( senseData.Value.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))

        w.Release() |> ignore


    [<Fact>]
    member this.EstablishNewACAStatus_004() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let exp = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK       // ACA task failed

        media.p_Terminate <- ( fun () ->
            Assert.Fail __LINE__
        )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.Fail __LINE__
        )

        let w = new SemaphoreSlim(1)
        w.Wait()
        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            w.Wait()
        )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueNone;
        }

        let queue2 =
            pc.Invoke( "EstablishNewACAStatus", source, exp, scsiCmd, 7u, false, BlockDeviceTaskType.ScsiTask, queue1 ) :?> TaskSet

        let waca = queue2.ACA
        Assert.True( waca.IsNone )
        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 7u ))
        let pcwt = new PrivateCaller( wt )
        Assert.True(( ( pcwt.GetField( "m_StatCode" ) :?> ScsiCmdStatCd ) = ScsiCmdStatCd.CHECK_CONDITION ))
        let senseData = pcwt.GetField( "m_SenseData" ) :?> SenseData option
        Assert.True(( senseData.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
        Assert.True(( senseData.Value.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))

        w.Release() |> ignore


    [<Fact>]
    member this.EstablishNewACAStatus_005() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let exp = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK    // Normal task failed

        media.p_Terminate <- ( fun () ->
            Assert.Fail __LINE__
        )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.Fail __LINE__
        )

        let w = new SemaphoreSlim(1)
        w.Wait()
        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            w.Wait()
        )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueNone;
        }
        
        let queue2 =
            pc.Invoke( "EstablishNewACAStatus", source, exp, scsiCmd, 50u, true, BlockDeviceTaskType.ScsiTask, queue1 ) :?> TaskSet

        let waca = queue2.ACA
        Assert.True( waca.IsSome )
        Assert.True( fst( waca.Value ) = source.I_TNexus )
        Assert.True( snd( waca.Value ) = exp )

        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 50u ))
        let pcwt = new PrivateCaller( wt )
        Assert.True(( ( pcwt.GetField( "m_StatCode" ) :?> ScsiCmdStatCd ) = ScsiCmdStatCd.CHECK_CONDITION ))
        let senseData = pcwt.GetField( "m_SenseData" ) :?> SenseData option
        Assert.True(( senseData.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
        Assert.True(( senseData.Value.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))

        w.Release() |> ignore

    [<Fact>]
    member this.EstablishNewACAStatus_006() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let exp = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK    // Normal task failed

        media.p_Terminate <- ( fun () ->
            Assert.Fail __LINE__
        )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.Fail __LINE__
        )

        let w = new SemaphoreSlim(1)
        w.Wait()
        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            w.Wait()
        )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueNone;
        }

        let queue2 =
            pc.Invoke( "EstablishNewACAStatus", source, exp, scsiCmd, 40u, false, BlockDeviceTaskType.ScsiTask, queue1) :?> TaskSet

        let waca = queue2.ACA
        Assert.True( waca.IsNone )
        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 40u ))
        let pcwt = new PrivateCaller( wt )
        Assert.True(( ( pcwt.GetField( "m_StatCode" ) :?> ScsiCmdStatCd ) = ScsiCmdStatCd.CHECK_CONDITION ))
        let senseData = pcwt.GetField( "m_SenseData" ) :?> SenseData option
        Assert.True(( senseData.Value.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
        Assert.True(( senseData.Value.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))

        w.Release() |> ignore

    [<Fact>]
    member this.EstablishNewACAStatus_007() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let exp = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.MEDIUM_ERROR ASCCd.AUXILIARY_MEMORY_READ_ERROR
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK     // ACA task failed

        media.p_Terminate <- ( fun () ->
            Assert.Fail __LINE__
        )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.Fail __LINE__
        )
        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueSome( 
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ABORTED_COMMAND ASCCd.ACK_NAK_TIMEOUT
            );
        }

        let w = new SemaphoreSlim(1)
        w.Wait()
        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            w.Wait()
        )
        
        let queue2 =
            pc.Invoke( "EstablishNewACAStatus", source, exp, scsiCmd, 30u, true, BlockDeviceTaskType.ScsiTask, queue1 ) :?> TaskSet

        let waca = queue2.ACA
        Assert.True( waca.IsSome )
        Assert.True( fst( waca.Value ) = source.I_TNexus )
        Assert.True( snd( waca.Value ) = exp )

        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 30u ))
        let pcwt = new PrivateCaller( wt )
        Assert.True(( ( pcwt.GetField( "m_StatCode" ) :?> ScsiCmdStatCd ) = ScsiCmdStatCd.CHECK_CONDITION ))
        let senseData = pcwt.GetField( "m_SenseData" ) :?> SenseData option
        Assert.True(( senseData.Value.SenseKey = SenseKeyCd.MEDIUM_ERROR ))
        Assert.True(( senseData.Value.ASC = ASCCd.AUXILIARY_MEMORY_READ_ERROR ))

        w.Release() |> ignore

    [<Fact>]
    member this.EstablishNewACAStatus_008() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let exp = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.MEDIUM_ERROR ASCCd.AUXILIARY_MEMORY_READ_ERROR
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK     // ACA task failed

        media.p_Terminate <- ( fun () ->
            Assert.Fail __LINE__
        )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.Fail __LINE__
        )

        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueSome( 
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ABORTED_COMMAND ASCCd.ACK_NAK_TIMEOUT
            );
        }

        let w = new SemaphoreSlim(1)
        w.Wait()
        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            w.Wait()
        )
        
        let queue2 =
            pc.Invoke( "EstablishNewACAStatus", source, exp, scsiCmd, 20u, false, BlockDeviceTaskType.ScsiTask, queue1 ) :?> TaskSet

        let waca = queue2.ACA
        Assert.True( waca.IsNone )
        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 20u ))
        let pcwt = new PrivateCaller( wt )
        Assert.True(( ( pcwt.GetField( "m_StatCode" ) :?> ScsiCmdStatCd ) = ScsiCmdStatCd.CHECK_CONDITION ))
        let senseData = pcwt.GetField( "m_SenseData" ) :?> SenseData option
        Assert.True(( senseData.Value.SenseKey = SenseKeyCd.MEDIUM_ERROR ))
        Assert.True(( senseData.Value.ASC = ASCCd.AUXILIARY_MEMORY_READ_ERROR ))
        
        w.Release() |> ignore

    [<Fact>]
    member this.EstablishNewACAStatus_009() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let exp = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.MEDIUM_ERROR ASCCd.AUXILIARY_MEMORY_READ_ERROR
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.HEAD_OF_QUEUE_TASK     // Normal task failed

        media.p_Terminate <- ( fun () ->
            Assert.Fail __LINE__
        )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.Fail __LINE__
        )

        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueSome( 
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ABORTED_COMMAND ASCCd.ACK_NAK_TIMEOUT
            );
        }

        let w = new SemaphoreSlim(1)
        w.Wait()
        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            w.Wait()
        )
        
        let queue2 =
            pc.Invoke( "EstablishNewACAStatus", source, exp, scsiCmd, 10u, false, BlockDeviceTaskType.ScsiTask, queue1 ) :?> TaskSet

        let waca = queue2.ACA
        Assert.True( waca.IsSome )
        Assert.True(( queue2.ACA = queue1.ACA ))
        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 10u ))
        let pcwt = new PrivateCaller( wt )
        Assert.True(( ( pcwt.GetField( "m_StatCode" ) :?> ScsiCmdStatCd ) = ScsiCmdStatCd.TASK_ABORTED ))
        let senseData = pcwt.GetField( "m_SenseData" ) :?> SenseData option
        Assert.True( senseData.IsNone )
        
        w.Release() |> ignore

    [<Fact>]
    member this.EstablishNewACAStatus_010() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let exp = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.MEDIUM_ERROR ASCCd.AUXILIARY_MEMORY_READ_ERROR
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK    // ACA task failed

        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueSome( 
                new ITNexus( "INIT_2", isid_me.fromElem 1uy 2uy 3us 4uy 5us, "TARG_6", tpgt_me.fromPrim 7us ),
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ABORTED_COMMAND ASCCd.ACK_NAK_TIMEOUT
            );
        }

        try
            pc.Invoke( "EstablishNewACAStatus", source, exp, scsiCmd, 0u, false, BlockDeviceTaskType.ScsiTask, queue1 ) |> ignore
            Assert.Fail __LINE__
        with
        | :? Xunit.Sdk.FailException -> reraise();
        | _ ->
            ()

    [<Fact>]
    member this.EstablishNewACAStatus_011() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let exp = BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.MEDIUM_ERROR ASCCd.AUXILIARY_MEMORY_READ_ERROR
        let scsiCmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.HEAD_OF_QUEUE_TASK    // Normal task failed

        media.p_Terminate <- ( fun () ->
            Assert.Fail __LINE__
        )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            Assert.Fail __LINE__
        )

        let queue1 = {
            Queue = ImmutableArray< TaskStatus >.Empty;
            ACA = ValueSome( 
                new ITNexus( "INIT_2", isid_me.fromElem 1uy 2uy 3us 4uy 5us, "TARG_6", tpgt_me.fromPrim 7us ),
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ABORTED_COMMAND ASCCd.ACK_NAK_TIMEOUT
            );
        }

        let w = new SemaphoreSlim(1)
        w.Wait()
        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            w.Wait()
        )
        
        let queue2 =
            pc.Invoke( "EstablishNewACAStatus", source, exp, scsiCmd, 0u, false, BlockDeviceTaskType.ScsiTask , queue1) :?> TaskSet

        let waca = queue2.ACA
        Assert.True( waca.IsSome )
        Assert.True(( queue2.ACA = queue1.ACA ))
        Assert.True(( queue2.Queue.Length = 1 ))
        Assert.True( ( TaskStatus.getTask queue2.Queue.[0] ).TaskType = BlockDeviceTaskType.InternalTask )
        let wt = ( TaskStatus.getTask queue2.Queue.[0] ) :?> SendErrorStatusTask
        Assert.True(( ( wt :> IBlockDeviceTask ).ReceivedDataLength = 0u ))
        let pcwt = new PrivateCaller( wt )
        Assert.True(( ( pcwt.GetField( "m_StatCode" ) :?> ScsiCmdStatCd ) = ScsiCmdStatCd.TASK_ABORTED ))
        let senseData = pcwt.GetField( "m_SenseData" ) :?> SenseData option
        Assert.True( senseData.IsNone )
        
        w.Release() |> ignore

    [<Fact>]
    member this.RunSCSITask_001() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x00uy }
        let w = new SemaphoreSlim( 0 )
        let mutable cnt = 0

        let taskStub = new CBlockDeviceTask_Stub()
        taskStub.p_GetSource <- ( fun () -> source )
        taskStub.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        taskStub.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        taskStub.p_GetCDB <- ( fun () -> ValueSome cdb )
        taskStub.p_Execute <- ( fun () ->
            fun () -> task{
                cnt <- cnt + 1
                w.Release() |> ignore
            }
        )

        m_TaskSetQueue.Enqueue( fun () ->
            let nextStat =
                pc.Invoke( "RunSCSITask", taskStub ) :?> TaskStatus
            match nextStat with
            | TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | _ -> ()
        )

        Assert.True(( w.Wait 100000 ))
        Assert.True(( cnt = 1 ))

    [<Fact>]
    member this.RunSCSITask_002() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let w = new SemaphoreSlim( 0 )
        let mutable cnt = 0

        let taskStub = new CBlockDeviceTask_Stub()
        taskStub.p_GetSource <- ( fun () -> source )
        taskStub.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        taskStub.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.InternalTask )
        taskStub.p_GetCDB <- ( fun () -> ValueNone )
        taskStub.p_Execute <- ( fun () ->
            fun () -> task{
                cnt <- cnt + 1
                w.Release() |> ignore
            }
        )

        m_TaskSetQueue.Enqueue( fun () ->
            let nextStat = pc.Invoke( "RunSCSITask", taskStub ) :?> TaskStatus
            match nextStat with
            | TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | _ -> ()
        )

        Assert.True(( w.Wait 100000 ))
        Assert.True(( cnt = 1 ))

    [<Fact>]
    member this.RunSCSITask_003() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let ua = pc.GetField( "m_UnitAttention" ) :?> ConcurrentDictionary< string, SCSIACAException >
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let w1 = new SemaphoreSlim( 0 )
        let mutable cnt = 0

        let taskStub = new CBlockDeviceTask_Stub()
        taskStub.p_GetSource <- ( fun () -> source )
        taskStub.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        taskStub.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        taskStub.p_GetCDB <- ( fun () -> ValueSome cdb )
        taskStub.p_Execute <- ( fun () ->
            Assert.Fail __LINE__
            fun () -> Task.FromResult()
        )
        taskStub.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
        taskStub.p_GetReceivedDataLength <- ( fun () -> 0u )

        // Unit Attention is exist
        ua.TryAdd(
            source.I_TNexus.InitiatorPortName,
            BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
        )
        |> ignore

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ _ _ _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.CHECK_CONDITION ))
            cnt <- cnt + 1
            w1.Release() |> ignore
        )

        m_TaskSetQueue.Enqueue( fun () ->
            let nextStat = pc.Invoke( "RunSCSITask", taskStub ) :?> TaskStatus

            match nextStat with
            | TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | TASK_STAT_Running( x ) ->
                Assert.True(( x.TaskType = BlockDeviceTaskType.InternalTask ))

        )

        Assert.True(( w1.Wait 100000 ))
        Assert.True(( cnt = 1 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        let waca = m_TaskSet.ACA
        Assert.True( waca.IsNone )
        Assert.True(( ua.Count = 0 ))

    [<Fact>]
    member this.RunSCSITask_004() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let w1 = new SemaphoreSlim( 0 )
        let w2 = new SemaphoreSlim( 0 )
        let w3 = new SemaphoreSlim( 0 )
        let mutable cnt1 = 0
        let mutable cnt2 = 0

        let taskStub = new CBlockDeviceTask_Stub()
        taskStub.p_GetSource <- ( fun () -> source )
        taskStub.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        taskStub.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        taskStub.p_GetCDB <- ( fun () -> ValueSome cdb )
        taskStub.p_Execute <- ( fun () ->
            fun () -> task {
                cnt1 <- cnt1 + 1
                // ACA Exception
                let ex =
                    BlockDeviceLU_Test.defSCSIACAException
                        ScsiCmdStatCd.CHECK_CONDITION
                        SenseKeyCd.ILLEGAL_REQUEST
                        ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
                ( lu :> IInternalLU ).NotifyTerminateTaskWithException taskStub ex
                w1.Release() |> ignore
            }
        )
        taskStub.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
        taskStub.p_GetReceivedDataLength <- ( fun () -> 0u )

        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty.Add( TaskStatus.TASK_STAT_Dormant( taskStub :> IBlockDeviceTask ) );
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ resp stat _ _ _ _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.CHECK_CONDITION ))
            cnt2 <- cnt2 + 1
            w2.Release() |> ignore
            w3.Wait()
        )

        task {
            m_TaskSetQueue.Enqueue( fun () ->
                let nextStat = pc.Invoke( "RunSCSITask", taskStub ) :?> TaskStatus

                match nextStat with
                | TASK_STAT_Dormant( _ ) ->
                    Assert.Fail __LINE__
                | TASK_STAT_Running( x ) ->
                    Assert.True(( x.TaskType = BlockDeviceTaskType.ScsiTask ))

                let queue2 = {
                    Queue = ImmutableArray<TaskStatus>.Empty.Add( nextStat );
                    ACA = ValueNone;
                }
                pc.SetField( "m_TaskSet", queue2 )
            )

            do! w1.WaitAsync()
            Assert.True(( cnt1 = 1 ))

            do! w2.WaitAsync()
            Assert.True(( cnt2 = 1 ))

            let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
            let waca = m_TaskSet.ACA
            Assert.True( waca.IsSome )

            Assert.True(( m_TaskSet.Queue.Length > 0 ))
            match m_TaskSet.Queue.[0] with
            | TaskStatus.TASK_STAT_Running( x ) ->
                Assert.True(( x.TaskType = BlockDeviceTaskType.InternalTask ))
            | _ ->
                Assert.Fail __LINE__

            w3.Release() |> ignore
        }
        |> Functions.RunTaskSynchronously

    [<Fact>]
    member this.RunSCSITask_005() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let w1 = new SemaphoreSlim( 0 )
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0

        let taskStub = new CBlockDeviceTask_Stub()
        taskStub.p_GetSource <- ( fun () -> source )
        taskStub.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        taskStub.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        taskStub.p_GetCDB <- ( fun () -> ValueSome cdb )
        taskStub.p_Execute <- ( fun () ->
            fun () -> task {
                cnt1 <- cnt1 + 1
                let ex = new Exception( "" )    // raise other exception( = LU Reset )
                ( lu :> IInternalLU ).NotifyTerminateTaskWithException taskStub ex
            }
        )
        taskStub.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
        taskStub.p_NotifyTerminate <- ( fun flg ->
            Assert.False( flg )
        )
        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty.Add( TaskStatus.TASK_STAT_Dormant( taskStub :> IBlockDeviceTask ) );
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt2 <- cnt2 + 1
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            cnt3 <- cnt3 + 1
            w1.Release() |> ignore
        )

        m_TaskSetQueue.Enqueue( fun () ->
            let nextStat = pc.Invoke( "RunSCSITask", taskStub ) :?> TaskStatus
            match nextStat with
            | TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | TASK_STAT_Running( x ) ->
                Assert.True(( x.TaskType = BlockDeviceTaskType.ScsiTask ))

            let queue2 = {
                Queue = ImmutableArray<TaskStatus>.Empty.Add( nextStat );
                ACA = ValueNone;
            }
            pc.SetField( "m_TaskSet", queue2 )
        )

        Assert.True(( w1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member this.RunSCSITask_006() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
//        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let w1 = new SemaphoreSlim( 0 )
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0

        let taskStub = new CBlockDeviceTask_Stub()
        taskStub.p_GetSource <- ( fun () -> source )
        taskStub.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        taskStub.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.InternalTask )
        taskStub.p_GetCDB <- ( fun () -> ValueNone )
        taskStub.p_Execute <- ( fun () ->
            fun () -> task {
                cnt1 <- cnt1 + 1
                // raise ACA exception in internal task ( it will occur LU reset )
                let ex =
                    BlockDeviceLU_Test.defSCSIACAException
                        ScsiCmdStatCd.CHECK_CONDITION
                        SenseKeyCd.ILLEGAL_REQUEST
                        ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
                ( lu :> IInternalLU ).NotifyTerminateTaskWithException taskStub ex
            }
        )
        taskStub.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
        taskStub.p_NotifyTerminate <- ( fun flg ->
            Assert.False( flg )
        )

        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty.Add( TaskStatus.TASK_STAT_Dormant( taskStub :> IBlockDeviceTask ) );
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt2 <- cnt2 + 1
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            cnt3 <- cnt3 + 1
            w1.Release() |> ignore
        )

        m_TaskSetQueue.Enqueue( fun () ->
            let nextStat = pc.Invoke( "RunSCSITask", taskStub ) :?> TaskStatus
            match nextStat with
            | TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | TASK_STAT_Running( x ) ->
                Assert.True(( x.TaskType = BlockDeviceTaskType.InternalTask ))

            let queue2 = {
                Queue = ImmutableArray<TaskStatus>.Empty.Add( nextStat );
                ACA = ValueNone;
            }
            pc.SetField( "m_TaskSet", queue2 )
        )

        Assert.True(( w1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member this.RunSCSITask_007() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let w1 = new SemaphoreSlim( 0 )

        let task2 = new CBlockDeviceTask_Stub()
        task2.p_GetSource <- ( fun () -> source )
        task2.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task2.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task2.p_GetCDB <- ( fun () -> ValueSome cdb )
        task2.p_Execute <- ( fun () ->
            fun () -> task {
                cnt2 <- cnt2 + 1
                ( lu :> IInternalLU ).NotifyTerminateTask task2
                w1.Release() |> ignore
            }
        )
        task2.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )

        let task1 = new CBlockDeviceTask_Stub()
        task1.p_GetSource <- ( fun () -> source )
        task1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task1.p_GetCDB <- ( fun () -> ValueSome cdb )
        task1.p_Execute <- ( fun () ->
            fun () -> task {
                cnt1 <- cnt1 + 1

                m_TaskSetQueue.Enqueue( fun () ->
                    let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
                    let queue2 = {
                        Queue = m_TaskSet.Queue.Add( TaskStatus.TASK_STAT_Dormant( task2 ) );
                        ACA = ValueNone;
                    }
                    pc.SetField( "m_TaskSet", queue2 )
                )

                ( lu :> IInternalLU ).NotifyTerminateTask task1
            }
        )
        task1.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )

        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty.Add( TaskStatus.TASK_STAT_Dormant( task1 :> IBlockDeviceTask ) );
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let nextStat = pc.Invoke( "RunSCSITask", task1 ) :?> TaskStatus
            match nextStat with
            | TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | TASK_STAT_Running( x ) ->
                Assert.True(( x = task1 ))

            let queue3 = {
                Queue = ImmutableArray<TaskStatus>.Empty.Add( nextStat );
                ACA = ValueNone;
            }
            pc.SetField( "m_TaskSet", queue3 )
        )

        Assert.True(( w1.Wait 100000 ))
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 100 do
            Thread.Sleep 10
        Assert.True(( wcnt < 100 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.RunSCSITask_008() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let sema1 = new SemaphoreSlim( 0 )
        let sema3 = new SemaphoreSlim( 0 )

        let taskdummy =
            new CBlockDeviceTask_Stub(
                p_GetSource = ( fun () -> source ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetCDB = ( fun () -> ValueSome cdb ),
                p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
            ) :> IBlockDeviceTask

        let taskDummyV = [|
            for i = 0 to 15 do
                yield TaskStatus.TASK_STAT_Running( taskdummy )
        |]

        let task1 = new CBlockDeviceTask_Stub()
        task1.p_GetSource <- ( fun () -> source )
        task1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task1.p_GetCDB <- ( fun () -> ValueSome cdb )
        task1.p_Execute <- ( fun () ->
            fun () -> task {
                cnt1 <- cnt1 + 1
                ( lu :> IInternalLU ).NotifyTerminateTask task1
                sema3.Release() |> ignore
            }
        )
        task1.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )

        let queue1 = {
            Queue = taskDummyV.ToImmutableArray().Add( TaskStatus.TASK_STAT_Dormant( task1 ) );
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let nextStat = pc.Invoke( "RunSCSITask", task1 ) :?> TaskStatus

            match nextStat with
            | TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | TASK_STAT_Running( x ) ->
                Assert.True(( x = task1 ))

            let queue2 = {
                Queue = queue1.Queue.SetItem( 16, nextStat )
                ACA = ValueNone;
            }
            pc.SetField( "m_TaskSet", queue2 )
            sema1.Release() |> ignore
        )
        Assert.True(( sema1.Wait 100000 ))
        Assert.True(( sema3.Wait 100000 ))

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 100 do
            Thread.Sleep 10
        Assert.True(( wcnt < 100 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 16 ))
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member this.RunSCSITask_009() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let w = new SemaphoreSlim( 0 )

        let rec task1() =
            let t = new CBlockDeviceTask_Stub()
            t.p_GetSource <- ( fun () -> source )
            t.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
            t.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
            t.p_GetCDB <- ( fun () -> ValueSome cdb )
            t.p_Execute <- ( fun () ->
                fun () -> task {
                    if cnt1 < 10000 then
                        cnt1 <- cnt1 + 1

                        m_TaskSetQueue.Enqueue( fun () ->
                            let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
                            let queue2 = {
                                Queue = m_TaskSet.Queue.Add( TaskStatus.TASK_STAT_Dormant( task1() ) )
                                ACA = ValueNone;
                            }
                            pc.SetField( "m_TaskSet", queue2 )
                        )

                        ( lu :> IInternalLU ).NotifyTerminateTask t
                    else
                        ( lu :> IInternalLU ).NotifyTerminateTask t
                        w.Release() |> ignore
                }
            )
            t.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK )
            t

        let testTask1 = task1()
        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty.Add( TaskStatus.TASK_STAT_Dormant( testTask1 ) );
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let nextStat = pc.Invoke( "RunSCSITask", testTask1 ) :?> TaskStatus
            match nextStat with
            | TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | TASK_STAT_Running( x ) ->
                Assert.True(( x.TaskType = BlockDeviceTaskType.ScsiTask ))

            let queue1 = {
                Queue = ImmutableArray<TaskStatus>.Empty.Add( nextStat );
                ACA = ValueNone;
            }
            pc.SetField( "m_TaskSet", queue1 )
        )
        Assert.True(( w.Wait 100000 ))

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 1000 do
            Thread.Sleep 10
        Assert.True(( wcnt < 1000 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))
        Assert.True(( cnt1 = 10000 ))

    [<Fact>]
    member this.StartExecutableSCSITasks_001() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let sema1 = new SemaphoreSlim( 0 )

        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty;
            ACA = ValueSome( 
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 =  pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet
            Assert.True(( queue2.Queue.Length = 0 ))
            let waca = queue2.ACA
            Assert.True(( waca.IsSome ))
            pc.SetField( "m_TaskSet", queue2 )
            sema1.Release() |> ignore
        )
        Assert.True(( sema1.Wait 100000 ))

    [<Fact>]
    member this.StartExecutableSCSITasks_002() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let mutable cnt1 = 0
        let w = new SemaphoreSlim(1)

        w.Wait()

        let task1 = new CBlockDeviceTask_Stub()
        task1.p_GetSource <- ( fun () -> source )
        task1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.InternalTask )
        task1.p_GetCDB <- ( fun () -> ValueNone )
        task1.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task1
                cnt1 <- cnt1 + 1
                w.Release() |> ignore
            }
        )
        task1.p_GetACANoncompliant <- ( fun () -> true )   // InternalTask always must return true.

        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty.Add( TaskStatus.TASK_STAT_Dormant( task1 ) );
            ACA = ValueSome(
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet
            Assert.True(( queue2.Queue.Length = 1 ))
            match queue2.Queue.[0] with
            | TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | TASK_STAT_Running( x ) ->
                Assert.True(( x.TaskType = BlockDeviceTaskType.InternalTask ))
            pc.SetField( "m_TaskSet", queue2 )
        )

        w.Wait()
        Assert.True(( cnt1 = 1 ))


    [<Fact>]
    member this.StartExecutableSCSITasks_003() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let w = new SemaphoreSlim( 0 )

        let task1 = new CBlockDeviceTask_Stub()
        task1.p_GetSource <- ( fun () -> source )
        task1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task1.p_GetCDB <- ( fun () -> ValueSome cdb )
        task1.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task1
                cnt1 <- cnt1 + 1
                w.Release() |> ignore
            }
        )
        task1.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK )
        task1.p_GetACANoncompliant <- ( fun () -> false )

        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty.Add( TaskStatus.TASK_STAT_Dormant( task1 ) );
            ACA = ValueSome(
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet
            Assert.True(( queue2.Queue.Length = 1 ))
            match queue2.Queue.[0] with
            | TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | TASK_STAT_Running( x ) ->
                Assert.True(( x.TaskType = BlockDeviceTaskType.ScsiTask ))
            pc.SetField( "m_TaskSet", queue2 )
        )

        w.Wait()
        Assert.True(( cnt1 = 1 ))


    [<Fact>]
    member this.StartExecutableSCSITasks_004() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let w = new SemaphoreSlim( 0 )

        let testTasks = [|
            yield TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetSource = ( fun () -> source ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetCDB = ( fun () -> ValueSome cdb ),
                    p_Execute = ( fun () ->
                        Assert.Fail __LINE__
                        fun () -> Task.FromResult()
                    ),
                    p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK ),
                    p_GetACANoncompliant = ( fun () -> false )
                ) :> IBlockDeviceTask
            );
            yield TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetSource = ( fun () -> source ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                    p_GetCDB = ( fun () -> ValueNone ),
                    p_Execute = ( fun () ->
                        Assert.Fail __LINE__
                        fun () -> Task.FromResult()
                    ),
                    p_GetACANoncompliant = ( fun () -> true )   // InternalTask always must return true.
                ) :> IBlockDeviceTask
            );
            for i = 2 to 15 do
                let task3 = new CBlockDeviceTask_Stub()
                task3.p_GetSource <- ( fun () -> source )
                task3.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
                task3.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.InternalTask )
                task3.p_GetCDB <- ( fun () -> ValueNone )
                task3.p_Execute <- ( fun () ->
                    fun () -> task {
                        ( lu :> IInternalLU ).NotifyTerminateTask task3
                        let wcnt = Interlocked.Increment( &cnt1 )
                        if wcnt >= 14 then
                            w.Release() |> ignore
                    }
                )
                task3.p_GetACANoncompliant <- ( fun () -> true )   // InternalTask always must return true.
                yield TaskStatus.TASK_STAT_Dormant( task3 )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueSome(
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet
            Assert.True(( queue2.Queue.Length = 16 ))
            for i = 2 to 15 do
                match queue2.Queue.[i] with
                | TASK_STAT_Dormant( _ ) ->
                    Assert.Fail __LINE__
                | TASK_STAT_Running( x ) ->
                    Assert.True(( x.TaskType = BlockDeviceTaskType.InternalTask ))
            pc.SetField( "m_TaskSet", queue2 )
        )

        w.Wait()
        Assert.True(( cnt1 = 14 ))

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 100 do
            Thread.Sleep 10
        Assert.True(( wcnt < 100 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.Equal( m_TaskSet.Queue.Length, 2 )

    [<Fact>]
    member this.StartExecutableSCSITasks_005() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let w = new SemaphoreSlim( 0 )

        let testTasks = [|
            yield TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetSource = ( fun () -> source ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetCDB = ( fun () -> ValueSome cdb ),
                    p_Execute = ( fun () ->
                        Assert.Fail __LINE__
                        fun () -> Task.FromResult()
                    ),
                    p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK ),
                    p_GetACANoncompliant = ( fun () -> false )
                ) :> IBlockDeviceTask
            );
            yield TaskStatus.TASK_STAT_Running(
                new CBlockDeviceTask_Stub(
                    p_GetSource = ( fun () -> source ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.InternalTask ),
                    p_GetCDB = ( fun () -> ValueNone ),
                    p_Execute = ( fun () ->
                        Assert.Fail __LINE__
                        fun () -> Task.FromResult()
                    ),
                    p_GetACANoncompliant = ( fun () -> true )   // InternalTask always must return true.
                ) :> IBlockDeviceTask
            );

            for i = 2 to 15 do
                let task3 = new CBlockDeviceTask_Stub()
                task3.p_GetSource <- ( fun () -> source )
                task3.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
                task3.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
                task3.p_GetCDB <- ( fun () -> ValueSome cdb )
                task3.p_Execute <- ( fun () ->
                    fun () -> task {
                        ( lu :> IInternalLU ).NotifyTerminateTask task3
                        let wcnt = Interlocked.Increment( &cnt1 )
                        if wcnt >= 14 then
                            w.Release() |> ignore
                    }
                )
                task3.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK )
                task3.p_GetACANoncompliant <- ( fun () -> false )
                yield TaskStatus.TASK_STAT_Dormant( task3 )
        |]

        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueSome(
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet

            Assert.True(( queue2.Queue.Length = 16 ))
            for i = 2 to 15 do
                match queue2.Queue.[i] with
                | TASK_STAT_Dormant( _ ) ->
                    Assert.Fail __LINE__
                | TASK_STAT_Running( x ) ->
                    Assert.True(( x.TaskType = BlockDeviceTaskType.ScsiTask ))
            pc.SetField( "m_TaskSet", queue2 )
        )

        w.Wait()
        Assert.Equal( 14, cnt1 )

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 100 do
            Thread.Sleep 10
        Assert.True(( wcnt < 100 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.Equal( 2, m_TaskSet.Queue.Length )

    [<Fact>]
    member this.StartExecutableSCSITasks_006() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let w = new SemaphoreSlim( 0 )

        let testTasks = [|
            yield TaskStatus.TASK_STAT_Dormant(
                new CBlockDeviceTask_Stub(
                    p_GetSource = ( fun () -> source ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetCDB = ( fun () -> ValueSome cdb ),
                    p_Execute = ( fun () ->
                        Assert.Fail __LINE__
                        fun () -> Task.FromResult()
                    ),
                    p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.HEAD_OF_QUEUE_TASK ),
                    p_GetACANoncompliant = ( fun () -> false )
                ) :> IBlockDeviceTask
            );
            let task2 =
                new CBlockDeviceTask_Stub(
                    p_GetSource = ( fun () -> source ),
                    p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                    p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                    p_GetCDB = ( fun () -> ValueSome cdb ),
                    p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK ),
                    p_GetACANoncompliant = ( fun () -> false )
                )
            task2.p_Execute <- ( fun () ->
                fun () -> task {
                    ( lu :> IInternalLU ).NotifyTerminateTask task2
                    cnt1 <- cnt1 + 1
                    w.Release() |> ignore
                }
            )
            yield TaskStatus.TASK_STAT_Dormant( task2 )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueSome(
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet
            pc.SetField( "m_TaskSet", queue2 )
        )

        w.Wait()
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member this.StartExecutableSCSITasks_007() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let w = new SemaphoreSlim( 0 )

        let task1 =
            new CBlockDeviceTask_Stub(
                p_GetSource = ( fun () -> source ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetCDB = ( fun () -> ValueSome cdb ),
                p_Execute = ( fun () ->
                    Assert.Fail __LINE__
                    fun () -> Task.FromResult()
                ),
                p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ORDERED_TASK ),
                p_GetACANoncompliant = ( fun () -> false )
            ) :> IBlockDeviceTask
        let task2 = new CBlockDeviceTask_Stub()
        task2.p_GetSource <- ( fun () -> source )
        task2.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task2.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task2.p_GetCDB <- ( fun () -> ValueSome cdb )
        task2.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task2
                cnt1 <- cnt1 + 1
                w.Release() |> ignore
            }
        )
        task2.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK )
        task2.p_GetACANoncompliant <- ( fun () -> false )

        let testTasks = [|
            yield TaskStatus.TASK_STAT_Dormant( task1 )
            yield TaskStatus.TASK_STAT_Dormant( task2 )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueSome(
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet
            pc.SetField( "m_TaskSet", queue2 )
        )

        w.Wait()
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member this.StartExecutableSCSITasks_008() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let w = new SemaphoreSlim( 0 )

        let task1 =
            new CBlockDeviceTask_Stub(
                p_GetSource = ( fun () -> source ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetCDB = ( fun () -> ValueSome cdb ),
                p_Execute = ( fun () ->
                    Assert.Fail __LINE__
                    fun () -> Task.FromResult()
                ),
                p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK ),
                p_GetACANoncompliant = ( fun () -> false )
            ) :> IBlockDeviceTask
        let task2 = new CBlockDeviceTask_Stub()
        task2.p_GetSource <- ( fun () -> source )
        task2.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task2.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task2.p_GetCDB <- ( fun () -> ValueSome cdb )
        task2.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task2
                cnt1 <- cnt1 + 1
                w.Release() |> ignore
            }
        )
        task2.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK )
        task2.p_GetACANoncompliant <- ( fun () -> false )

        let testTasks = [|
            yield TaskStatus.TASK_STAT_Dormant( task1 )
            yield TaskStatus.TASK_STAT_Dormant( task2 )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueSome(
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet
            pc.SetField( "m_TaskSet", queue2 )
        )

        w.Wait()
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member this.StartExecutableSCSITasks_009() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let w = new SemaphoreSlim( 0 )

        let task1 =
            new CBlockDeviceTask_Stub(
                p_GetSource = ( fun () -> source ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u ),
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetCDB = ( fun () -> ValueSome cdb ),
                p_Execute = ( fun () ->
                    Assert.Fail __LINE__
                    fun () -> Task.FromResult()
                ),
                p_GetSCSICommand = ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.TAGLESS_TASK ),
                p_GetACANoncompliant = ( fun () -> false )
            ) :> IBlockDeviceTask
        let task2 = new CBlockDeviceTask_Stub()
        task2.p_GetSource <- ( fun () -> source )
        task2.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task2.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task2.p_GetCDB <- ( fun () -> ValueSome cdb )
        task2.p_Execute <- ( fun () ->
            fun () -> task {
                cnt1 <- cnt1 + 1
                w.Release() |> ignore
                ( lu :> IInternalLU ).NotifyTerminateTask task2
            }
        )
        task2.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ACA_TASK )
        task2.p_GetACANoncompliant <- ( fun () -> false )

        let testTasks = [|
            yield TaskStatus.TASK_STAT_Dormant( task1 )
            yield TaskStatus.TASK_STAT_Dormant( task2 )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueSome(
                source.I_TNexus,
                BlockDeviceLU_Test.defSCSIACAException ScsiCmdStatCd.CHECK_CONDITION SenseKeyCd.ILLEGAL_REQUEST ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT
            );
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet
            pc.SetField( "m_TaskSet", queue2 )
        )

        w.Wait()
        Assert.True(( cnt1 = 1 ))

    [<Fact>]
    member this.StartExecutableSCSITasks_010() =
        let _, _, lu = this.createBlockDevice()
        let pc = new PrivateCaller( lu )
        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty;
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )
        let queue2 =
            pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet
        Assert.True(( queue2.Queue.Length = 0 ))

    [<Theory>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK )>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.ORDERED_TASK )>]
    [<InlineData( TaskATTRCd.HEAD_OF_QUEUE_TASK )>]
    [<InlineData( TaskATTRCd.ACA_TASK )>]
    member this.StartExecutableSCSITasks_011( attr : TaskATTRCd ) =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let w = new SemaphoreSlim( 0 )

        let task1 = new CBlockDeviceTask_Stub()
        task1.p_GetSource <- ( fun () -> source )
        task1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task1.p_GetCDB <- ( fun () -> ValueSome cdb )
        task1.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task1
                cnt1 <- cnt1 + 1
                w.Release() |> ignore
            }
        )
        task1.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand attr )
        task1.p_GetACANoncompliant <- ( fun () -> false )

        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty.Add( TaskStatus.TASK_STAT_Dormant( task1 ) )
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet
            pc.SetField( "m_TaskSet", queue2 )
        )

        w.Wait()
        Assert.True(( cnt1 = 1 ))

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 100 do
            Thread.Sleep 10
        Assert.True(( wcnt < 100 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))
        
    [<Fact>]
    member this.StartExecutableSCSITasks_012() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let mutable cnt1 = 0
        let w = new SemaphoreSlim( 0 )

        let task1 = new CBlockDeviceTask_Stub()
        task1.p_GetSource <- ( fun () -> source )
        task1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.InternalTask )
        task1.p_GetCDB <- ( fun () -> ValueNone )
        task1.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task1
                cnt1 <- cnt1 + 1
                w.Release() |> ignore
            }
        )
        task1.p_GetACANoncompliant <- ( fun () -> true )

        let queue1 = {
            Queue = ImmutableArray<TaskStatus>.Empty.Add( TaskStatus.TASK_STAT_Dormant( task1 ) )
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet
            pc.SetField( "m_TaskSet", queue2 )
        )

        w.Wait()
        Assert.True(( cnt1 = 1 ))

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 100 do
            Thread.Sleep 10
        Assert.True(( wcnt < 100 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Theory>]
    [<InlineData(   0, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  true,  true )>]
    [<InlineData(   1, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  true,  true )>]
    [<InlineData(   2, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(   3, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(   4, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(   5, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      true,  true,  true )>]
    [<InlineData(   6, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       true,  true,  true )>]
    [<InlineData(   7, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(   8, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(   9, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  10, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  11, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  12, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  13, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  14, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  15, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  16, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  17, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  18, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  19, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  20, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  21, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  22, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  23, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  24, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  25, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  true,  true )>]
    [<InlineData(  26, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  true,  true )>]
    [<InlineData(  27, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  28, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  29, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  30, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      true,  true,  true )>]
    [<InlineData(  31, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       true,  true,  true )>]
    [<InlineData(  32, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  33, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  34, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  35, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  36, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  37, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  38, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  39, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  40, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  41, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  42, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  43, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  44, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  45, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  46, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  47, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  48, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  49, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  50, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  51, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  52, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  53, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  54, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  55, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  56, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  57, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  58, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  59, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  60, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  61, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  62, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  63, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  64, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  65, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  66, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  67, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  68, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  69, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  70, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  71, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  72, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  73, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  74, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  75, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  76, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  77, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  78, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  79, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  80, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  81, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  82, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  83, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  84, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  85, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  86, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  87, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  88, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  89, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  90, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  91, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  92, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  93, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  94, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  95, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  96, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  97, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  98, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  99, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData( 100, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData( 101, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData( 102, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData( 103, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData( 104, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData( 105, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData( 106, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData( 107, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData( 108, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData( 109, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData( 110, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData( 111, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData( 112, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData( 113, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData( 114, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData( 115, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData( 116, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData( 117, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData( 118, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData( 119, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData( 120, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData( 121, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData( 122, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData( 123, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData( 124, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    member this.StartExecutableSCSITasks_013
                ( caseIdx : int )
                ( attr1 : TaskATTRCd )
                ( attr2 : TaskATTRCd )
                ( attr3 : TaskATTRCd )
                ( task1flg : bool )
                ( task2flg : bool )
                ( task3flg : bool ) =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let w1 = new SemaphoreSlim( 0 )
        let w2 = new SemaphoreSlim( 0 )
        let w3 = new SemaphoreSlim( 0 )

        let task1 = new CBlockDeviceTask_Stub()
        task1.p_GetSource <- ( fun () -> source )
        task1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task1.p_GetCDB <- ( fun () -> ValueSome cdb )
        task1.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task1
                cnt1 <- cnt1 + 1
                if task1flg then
                    w1.Release() |> ignore
                else
                    Assert.Fail __LINE__
            }
        )
        task1.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand attr1 )
        task1.p_GetACANoncompliant <- ( fun () -> false )

        let task2 = new CBlockDeviceTask_Stub()
        task2.p_GetSource <- ( fun () -> source )
        task2.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task2.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task2.p_GetCDB <- ( fun () -> ValueSome cdb )
        task2.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task2
                cnt2 <- cnt2 + 1
                if task2flg then
                    w2.Release() |> ignore
                else
                    Assert.Fail __LINE__
            }
        )
        task2.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand attr2 )
        task2.p_GetACANoncompliant <- ( fun () -> false )

        let task3 = new CBlockDeviceTask_Stub()
        task3.p_GetSource <- ( fun () -> source )
        task3.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task3.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task3.p_GetCDB <- ( fun () -> ValueSome cdb )
        task3.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task3
                cnt3 <- cnt3 + 1
                if task3flg then
                    w3.Release() |> ignore
                else
                    Assert.Fail __LINE__
            }
        )
        task3.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand attr3 )
        task3.p_GetACANoncompliant <- ( fun () -> false )

        let testTasks = [|
            yield TaskStatus.TASK_STAT_Dormant( task1 )
            yield TaskStatus.TASK_STAT_Dormant( task2 )
            yield TaskStatus.TASK_STAT_Dormant( task3 )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet

            let queue3 =
                if task1flg then
                    match queue2.Queue.[0] with
                    | TaskStatus.TASK_STAT_Dormant( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    queue2
                else
                    match queue2.Queue.[0] with
                    | TaskStatus.TASK_STAT_Running( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    {
                        queue2 with
                            Queue = queue2.Queue.SetItem ( 0, TaskStatus.TASK_STAT_Running( task1 ) )
                    }

            let queue4 =
                if task2flg then
                    match queue3.Queue.[1] with
                    | TaskStatus.TASK_STAT_Dormant( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    queue3
                else
                    match queue3.Queue.[1] with
                    | TaskStatus.TASK_STAT_Running( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    {
                        queue3 with
                            Queue = queue3.Queue.SetItem( 1, TaskStatus.TASK_STAT_Running( task2 ) )
                    }

            let queue5 =
                if task3flg then
                    match queue4.Queue.[2] with
                    | TaskStatus.TASK_STAT_Dormant( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    queue4
                else
                    match queue4.Queue.[2] with
                    | TaskStatus.TASK_STAT_Running( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    {
                        queue4 with
                            Queue = queue4.Queue.SetItem( 2, TaskStatus.TASK_STAT_Running( task3 ) )
                    }

            pc.SetField( "m_TaskSet", queue5 )
        )


        if task1flg then
            w1.Wait()
            Assert.True(( cnt1 = 1 ))

        if task2flg then
            w2.Wait()
            Assert.True(( cnt2 = 1 ))

        if task3flg then
            w3.Wait()
            Assert.True(( cnt3 = 1 ))

        let widx =
            ( if task1flg then 0 else 1 ) +
            ( if task2flg then 0 else 1 ) +
            ( if task3flg then 0 else 1 )

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 100 do
            Thread.Sleep 10
        Assert.True(( wcnt < 100 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = widx ))

    [<Theory>]
    [<InlineData(   0, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  true )>]
    [<InlineData(   1, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  true )>]
    [<InlineData(   2, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData(   3, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData(   4, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData(   5, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      true,  true )>]
    [<InlineData(   6, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       true,  true )>]
    [<InlineData(   7, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData(   8, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData(   9, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData(  10, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      false, false )>]
    [<InlineData(  11, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       false, false )>]
    [<InlineData(  12, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      false, false )>]
    [<InlineData(  13, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,false, true )>]
    [<InlineData(  14, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          false, true )>]
    [<InlineData(  15, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  false )>]
    [<InlineData(  16, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  false )>]
    [<InlineData(  17, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData(  18, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData(  19, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData(  20, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  false )>]
    [<InlineData(  21, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  false )>]
    [<InlineData(  22, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData(  23, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData(  24, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData(  25, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  true )>]
    [<InlineData(  26, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  true )>]
    [<InlineData(  27, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData(  28, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData(  29, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData(  30, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      true,  true )>]
    [<InlineData(  31, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       true,  true )>]
    [<InlineData(  32, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData(  33, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData(  34, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData(  35, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      false, false )>]
    [<InlineData(  36, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       false, false )>]
    [<InlineData(  37, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      false, false )>]
    [<InlineData(  38, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,false, true )>]
    [<InlineData(  39, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          false, true )>]
    [<InlineData(  40, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  false )>]
    [<InlineData(  41, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  false )>]
    [<InlineData(  42, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData(  43, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData(  44, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData(  45, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  false )>]
    [<InlineData(  46, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  false )>]
    [<InlineData(  47, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData(  48, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData(  49, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData(  50, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      false, false )>]
    [<InlineData(  51, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       false, false )>]
    [<InlineData(  52, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      false, false )>]
    [<InlineData(  53, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,false, true )>]
    [<InlineData(  54, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          false, true )>]
    [<InlineData(  55, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      false, false )>]
    [<InlineData(  56, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       false, false )>]
    [<InlineData(  57, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      false, false )>]
    [<InlineData(  58, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,false, true )>]
    [<InlineData(  59, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          false, true )>]
    [<InlineData(  60, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      false, false )>]
    [<InlineData(  61, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       false, false )>]
    [<InlineData(  62, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      false, false )>]
    [<InlineData(  63, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,false, true )>]
    [<InlineData(  64, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          false, true )>]
    [<InlineData(  65, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  false )>]
    [<InlineData(  66, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  false )>]
    [<InlineData(  67, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData(  68, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData(  69, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData(  70, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  false )>]
    [<InlineData(  71, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  false )>]
    [<InlineData(  72, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData(  73, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData(  74, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData(  75, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      false, false )>]
    [<InlineData(  76, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       false, false )>]
    [<InlineData(  77, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      false, false )>]
    [<InlineData(  78, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,false, true )>]
    [<InlineData(  79, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          false, true )>]
    [<InlineData(  80, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      false, false )>]
    [<InlineData(  81, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       false, false )>]
    [<InlineData(  82, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      false, false )>]
    [<InlineData(  83, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,false, true )>]
    [<InlineData(  84, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          false, true )>]
    [<InlineData(  85, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      false, false )>]
    [<InlineData(  86, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       false, false )>]
    [<InlineData(  87, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      false, false )>]
    [<InlineData(  88, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,false, true )>]
    [<InlineData(  89, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          false, true )>]
    [<InlineData(  90, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  false )>]
    [<InlineData(  91, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  false )>]
    [<InlineData(  92, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData(  93, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData(  94, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData(  95, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  false )>]
    [<InlineData(  96, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  false )>]
    [<InlineData(  97, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData(  98, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData(  99, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData( 100, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      false, false )>]
    [<InlineData( 101, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       false, false )>]
    [<InlineData( 102, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      false, false )>]
    [<InlineData( 103, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,false, true )>]
    [<InlineData( 104, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          false, true )>]
    [<InlineData( 105, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      false, false )>]
    [<InlineData( 106, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       false, false )>]
    [<InlineData( 107, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      false, false )>]
    [<InlineData( 108, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,false, true )>]
    [<InlineData( 109, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          false, true )>]
    [<InlineData( 110, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      false, false )>]
    [<InlineData( 111, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       false, false )>]
    [<InlineData( 112, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      false, false )>]
    [<InlineData( 113, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,false, true )>]
    [<InlineData( 114, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          false, true )>]
    [<InlineData( 115, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  false )>]
    [<InlineData( 116, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  false )>]
    [<InlineData( 117, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData( 118, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData( 119, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true )>]
    [<InlineData( 120, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  false )>]
    [<InlineData( 121, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  false )>]
    [<InlineData( 122, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  false )>]
    [<InlineData( 123, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true )>]
    [<InlineData( 124, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true )>]
    member this.StartExecutableSCSITasks_014
                ( caseIdx : int )
                ( attr1 : TaskATTRCd )
                ( attr2 : TaskATTRCd )
                ( attr3 : TaskATTRCd )
                ( task2flg : bool )
                ( task3flg : bool ) =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let w2 = new SemaphoreSlim( 0 )
        let w3 = new SemaphoreSlim( 0 )

        let task1 = new CBlockDeviceTask_Stub()
        task1.p_GetSource <- ( fun () -> source )
        task1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task1.p_GetCDB <- ( fun () -> ValueSome cdb )
        task1.p_Execute <- ( fun () ->
            Assert.Fail __LINE__
            fun () -> Task.FromResult()
        )
        task1.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand attr1 )
        task1.p_GetACANoncompliant <- ( fun () -> false )

        let task2 = new CBlockDeviceTask_Stub()
        task2.p_GetSource <- ( fun () -> source )
        task2.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task2.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task2.p_GetCDB <- ( fun () -> ValueSome cdb )
        task2.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task2
                cnt2 <- cnt2 + 1
                if task2flg then
                    w2.Release() |> ignore
                else
                    Assert.Fail __LINE__
            }
        )
        task2.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand attr2 )
        task2.p_GetACANoncompliant <- ( fun () -> false )

        let task3 = new CBlockDeviceTask_Stub()
        task3.p_GetSource <- ( fun () -> source )
        task3.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task3.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task3.p_GetCDB <- ( fun () -> ValueSome cdb )
        task3.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task3
                cnt3 <- cnt3 + 1
                if task3flg then
                    w3.Release() |> ignore
                else
                    Assert.Fail __LINE__
            }
        )
        task3.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand attr3 )
        task3.p_GetACANoncompliant <- ( fun () -> false )

        let testTasks = [|
            yield TaskStatus.TASK_STAT_Running( task1 )
            yield TaskStatus.TASK_STAT_Dormant( task2 )
            yield TaskStatus.TASK_STAT_Dormant( task3 )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet
            
            let queue3 =
                if task2flg then
                    match queue2.Queue.[1] with
                    | TaskStatus.TASK_STAT_Dormant( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    queue2
                else
                    match queue2.Queue.[1] with
                    | TaskStatus.TASK_STAT_Running( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    {
                        queue2 with
                            Queue = queue2.Queue.SetItem ( 1, TaskStatus.TASK_STAT_Running( task2 ) )
                    }

            let queue4 =
                if task3flg then
                    match queue3.Queue.[2] with
                    | TaskStatus.TASK_STAT_Dormant( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    queue3
                else
                    match queue3.Queue.[2] with
                    | TaskStatus.TASK_STAT_Running( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    {
                        queue3 with
                            Queue = queue3.Queue.SetItem ( 2, TaskStatus.TASK_STAT_Running( task3 ) )
                    }

            pc.SetField( "m_TaskSet", queue4 )
        )

        if task2flg then
            w2.Wait()
            Assert.True(( cnt2 = 1 ))

        if task3flg then
            w3.Wait()
            Assert.True(( cnt3 = 1 ))

        let widx =
            ( if task2flg then 0 else 1 ) +
            ( if task3flg then 0 else 1 ) + 1

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 100 do
            Thread.Sleep 10
        Assert.True(( wcnt < 100 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = widx ))

    [<Theory>]
    [<InlineData(   0, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  true,  true )>]
    [<InlineData(   1, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  true,  true )>]
    [<InlineData(   2, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(   3, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(   4, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(   5, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      true,  true,  true )>]
    [<InlineData(   6, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       true,  true,  true )>]
    [<InlineData(   7, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(   8, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(   9, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  10, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  11, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  12, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  13, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  14, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  15, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  16, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  17, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  18, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  19, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  20, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  21, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  22, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  23, TaskATTRCd.TAGLESS_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  24, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  25, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  true,  true )>]
    [<InlineData(  26, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  true,  true )>]
    [<InlineData(  27, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  28, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  29, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  30, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      true,  true,  true )>]
    [<InlineData(  31, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       true,  true,  true )>]
    [<InlineData(  32, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  33, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  34, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  35, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  36, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  37, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  38, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  39, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  40, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  41, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  42, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  43, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  44, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  45, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  46, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  47, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  48, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  49, TaskATTRCd.SIMPLE_TASK,        TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  50, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  51, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  52, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  53, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  54, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  55, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  56, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  57, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  58, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  59, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  60, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  61, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  62, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  63, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  64, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  65, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  66, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  67, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  68, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  69, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  70, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  71, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  72, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  73, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  74, TaskATTRCd.ORDERED_TASK,       TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  75, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  76, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  77, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  78, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  79, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  80, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  81, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  82, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  83, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  84, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  85, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData(  86, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData(  87, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData(  88, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData(  89, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData(  90, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  91, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  92, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  93, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  94, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData(  95, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData(  96, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData(  97, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData(  98, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData(  99, TaskATTRCd.HEAD_OF_QUEUE_TASK, TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData( 100, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData( 101, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData( 102, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData( 103, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData( 104, TaskATTRCd.ACA_TASK,           TaskATTRCd.TAGLESS_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData( 105, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData( 106, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData( 107, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData( 108, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData( 109, TaskATTRCd.ACA_TASK,           TaskATTRCd.SIMPLE_TASK,       TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData( 110, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.TAGLESS_TASK,      true,  false, false )>]
    [<InlineData( 111, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.SIMPLE_TASK,       true,  false, false )>]
    [<InlineData( 112, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ORDERED_TASK,      true,  false, false )>]
    [<InlineData( 113, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  false, true )>]
    [<InlineData( 114, TaskATTRCd.ACA_TASK,           TaskATTRCd.ORDERED_TASK,      TaskATTRCd.ACA_TASK,          true,  false, true )>]
    [<InlineData( 115, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData( 116, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData( 117, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData( 118, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData( 119, TaskATTRCd.ACA_TASK,           TaskATTRCd.HEAD_OF_QUEUE_TASK,TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    [<InlineData( 120, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.TAGLESS_TASK,      true,  true,  false )>]
    [<InlineData( 121, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.SIMPLE_TASK,       true,  true,  false )>]
    [<InlineData( 122, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.ORDERED_TASK,      true,  true,  false )>]
    [<InlineData( 123, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.HEAD_OF_QUEUE_TASK,true,  true,  true )>]
    [<InlineData( 124, TaskATTRCd.ACA_TASK,           TaskATTRCd.ACA_TASK,          TaskATTRCd.ACA_TASK,          true,  true,  true )>]
    member this.StartExecutableSCSITasks_015
                ( caseIdx : int )
                ( attr1 : TaskATTRCd )
                ( attr2 : TaskATTRCd )
                ( attr3 : TaskATTRCd )
                ( task1flg : bool )
                ( task2flg : bool )
                ( task3flg : bool ) =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let mutable cnt2 = 0
        let mutable cnt3 = 0
        let mutable cnt_int_1 = 0
        let mutable cnt_int_2 = 0
        let mutable cnt_int_3 = 0
        let w1 = new SemaphoreSlim( 0 )
        let w2 = new SemaphoreSlim( 0 )
        let w3 = new SemaphoreSlim( 0 )
        let w_int_1 = new SemaphoreSlim( 0 )
        let w_int_2 = new SemaphoreSlim( 0 )
        let w_int_3 = new SemaphoreSlim( 0 )

        let task1 = new CBlockDeviceTask_Stub()
        task1.p_GetSource <- ( fun () -> source )
        task1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task1.p_GetCDB <- ( fun () -> ValueSome cdb )
        task1.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task1
                cnt1 <- cnt1 + 1
                if task1flg then
                    w1.Release() |> ignore
                else
                    Assert.Fail __LINE__
            }
        )
        task1.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand attr1 )
        task1.p_GetACANoncompliant <- ( fun () -> false )

        let task_int_1 = new CBlockDeviceTask_Stub()
        task_int_1.p_GetSource <- ( fun () -> source )
        task_int_1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task_int_1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.InternalTask )
        task_int_1.p_GetCDB <- ( fun () -> ValueNone )
        task_int_1.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task_int_1
                cnt_int_1 <- cnt_int_1 + 1
                w_int_1.Release() |> ignore
            }
        )

        let task2 = new CBlockDeviceTask_Stub()
        task2.p_GetSource <- ( fun () -> source )
        task2.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task2.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task2.p_GetCDB <- ( fun () -> ValueSome cdb )
        task2.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task2
                cnt2 <- cnt2 + 1
                if task2flg then
                    w2.Release() |> ignore
                else
                    Assert.Fail __LINE__
            }
        )
        task2.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand attr2 )
        task2.p_GetACANoncompliant <- ( fun () -> false )

        let task_int_2 = new CBlockDeviceTask_Stub()
        task_int_2.p_GetSource <- ( fun () -> source )
        task_int_2.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task_int_2.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.InternalTask )
        task_int_2.p_GetCDB <- ( fun () -> ValueNone )
        task_int_2.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task_int_2
                cnt_int_2 <- cnt_int_2 + 1
                w_int_2.Release() |> ignore
            }
        )

        let task3 = new CBlockDeviceTask_Stub()
        task3.p_GetSource <- ( fun () -> source )
        task3.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task3.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
        task3.p_GetCDB <- ( fun () -> ValueSome cdb )
        task3.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task3
                cnt3 <- cnt3 + 1
                if task3flg then
                    w3.Release() |> ignore
                else
                    Assert.Fail __LINE__
            }
        )
        task3.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand attr3 )
        task3.p_GetACANoncompliant <- ( fun () -> false )

        let task_int_3 = new CBlockDeviceTask_Stub()
        task_int_3.p_GetSource <- ( fun () -> source )
        task_int_3.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
        task_int_3.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.InternalTask )
        task_int_3.p_GetCDB <- ( fun () -> ValueNone )
        task_int_3.p_Execute <- ( fun () ->
            fun () -> task {
                ( lu :> IInternalLU ).NotifyTerminateTask task_int_3
                cnt_int_3 <- cnt_int_3 + 1
                w_int_3.Release() |> ignore
            }
        )

        let testTasks = [|
            yield TaskStatus.TASK_STAT_Dormant( task1 )
            yield TaskStatus.TASK_STAT_Dormant( task_int_1 )
            yield TaskStatus.TASK_STAT_Dormant( task2 )
            yield TaskStatus.TASK_STAT_Dormant( task_int_2 )
            yield TaskStatus.TASK_STAT_Dormant( task3 )
            yield TaskStatus.TASK_STAT_Dormant( task_int_3 )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet

            let queue3 =
                if task1flg then
                    match queue2.Queue.[0] with
                    | TaskStatus.TASK_STAT_Dormant( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    queue2
                else
                    match queue2.Queue.[0] with
                    | TaskStatus.TASK_STAT_Running( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    {
                        queue2 with
                            Queue = queue2.Queue.SetItem ( 0, TaskStatus.TASK_STAT_Running( task1 ) )
                    }

            match queue3.Queue.[1] with
            | TaskStatus.TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | _ -> ()

            let queue4 =
                if task2flg then
                    match queue3.Queue.[2] with
                    | TaskStatus.TASK_STAT_Dormant( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    queue3
                else
                    match queue3.Queue.[2] with
                    | TaskStatus.TASK_STAT_Running( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    {
                        queue3 with
                            Queue = queue3.Queue.SetItem ( 2, TaskStatus.TASK_STAT_Running( task2 ) )
                    }

            match queue4.Queue.[3] with
            | TaskStatus.TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | _ -> ()

            let queue5 =
                if task3flg then
                    match queue4.Queue.[4] with
                    | TaskStatus.TASK_STAT_Dormant( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    queue4
                else
                    match queue4.Queue.[4] with
                    | TaskStatus.TASK_STAT_Running( _ ) ->
                        Assert.Fail __LINE__
                    | _ -> ()
                    {
                        queue4 with
                            Queue = queue4.Queue.SetItem ( 4, TaskStatus.TASK_STAT_Running( task3 ) )
                    }

            match queue5.Queue.[5] with
            | TaskStatus.TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | _ -> ()

            pc.SetField( "m_TaskSet", queue5 )
        )


        if task1flg then
            w1.Wait()
            Assert.True(( cnt1 = 1 ))

        if task2flg then
            w2.Wait()
            Assert.True(( cnt2 = 1 ))

        if task3flg then
            w3.Wait()
            Assert.True(( cnt3 = 1 ))

        w_int_1.Wait()
        w_int_2.Wait()
        w_int_3.Wait()
        Assert.True(( cnt_int_1 = 1 ))
        Assert.True(( cnt_int_2 = 1 ))
        Assert.True(( cnt_int_3 = 1 ))

        let widx =
            ( if task1flg then 0 else 1 ) +
            ( if task2flg then 0 else 1 ) +
            ( if task3flg then 0 else 1 )

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 100 do
            Thread.Sleep 10
        Assert.True(( wcnt < 100 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = widx ))

    [<Fact>]
    member this.StartExecutableSCSITasks_016() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let w1 = new SemaphoreSlim(1)
        w1.Wait()

        let testTasks = [|
            for i = 0 to 15 do
                let task1 = new CBlockDeviceTask_Stub()
                task1.p_GetSource <- ( fun () -> source )
                task1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
                task1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.InternalTask )
                task1.p_GetCDB <- ( fun () -> ValueNone )
                task1.p_Execute <- ( fun () ->
                    fun () -> task {
                        ( lu :> IInternalLU ).NotifyTerminateTask task1
                        let wcnt = Interlocked.Increment( &cnt1 )
                        if wcnt >= 16 then
                            w1.Release() |> ignore
                    }
                )
                yield TaskStatus.TASK_STAT_Dormant( task1 )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet

            for i = 0 to 15 do
                match queue2.Queue.[i] with
                | TaskStatus.TASK_STAT_Dormant( _ ) ->
                    Assert.Fail __LINE__
                | _ -> ()

            pc.SetField( "m_TaskSet", queue2 )
        )

        w1.Wait()
        Assert.True(( cnt1 = 16 ))

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 100 do
            Thread.Sleep 10
        Assert.True(( wcnt < 100 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))
            
    [<Theory>]
    [<InlineData( TaskATTRCd.TAGLESS_TASK )>]
    [<InlineData( TaskATTRCd.SIMPLE_TASK )>]
    [<InlineData( TaskATTRCd.HEAD_OF_QUEUE_TASK )>]
    [<InlineData( TaskATTRCd.ACA_TASK )>]
    member this.StartExecutableSCSITasks_017( attr : TaskATTRCd ) =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let w1 = new SemaphoreSlim( 0 )

        let testTasks = [|
            for i = 0 to 15 do
                let task1 = new CBlockDeviceTask_Stub()
                task1.p_GetSource <- ( fun () -> source )
                task1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
                task1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
                task1.p_GetCDB <- ( fun () -> ValueSome cdb )
                task1.p_Execute <- ( fun () ->
                    fun () -> task {
                        ( lu :> IInternalLU ).NotifyTerminateTask task1
                        let wcnt = Interlocked.Increment( &cnt1 )
                        if wcnt >= 16 then
                            w1.Release() |> ignore
                    }
                )
                task1.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand attr )
                task1.p_GetACANoncompliant <- ( fun () -> false )
                yield TaskStatus.TASK_STAT_Dormant( task1 )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet

            for i = 0 to 15 do
                match queue2.Queue.[i] with
                | TaskStatus.TASK_STAT_Dormant( _ ) ->
                    Assert.Fail __LINE__
                | _ -> ()

            pc.SetField( "m_TaskSet", queue2 )
        )

        w1.Wait()
        Assert.True(( cnt1 = 16 ))

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 100 do
            Thread.Sleep 10
        Assert.True(( wcnt < 100 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.StartExecutableSCSITasks_018() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let m_TaskSetQueue = pc.GetField( "m_TaskSetQueue" ) :?> LambdaQueue
        let cdb = { OperationCode = 0uy; Control = 0x04uy }
        let mutable cnt1 = 0
        let taskwait = [|
            for i = 1 to 16 do
                yield new SemaphoreSlim( 0 )
        |]
        let w1 = new SemaphoreSlim( 0 )

        let testTasks = [|
            for i = 0 to 15 do
                let task1 = new CBlockDeviceTask_Stub()
                task1.p_GetSource <- ( fun () -> source )
                task1.p_GetInitiatorTaskTag <- ( fun () -> itt_me.fromPrim 1u )
                task1.p_GetTaskType <- ( fun () -> BlockDeviceTaskType.ScsiTask )
                task1.p_GetCDB <- ( fun () -> ValueSome cdb )
                task1.p_Execute <- ( fun () ->
                    fun () -> task {
                        taskwait.[cnt1].Wait()
                        cnt1 <- cnt1 + 1
                        if cnt1 < 16 then
                            taskwait.[cnt1].Release() |> ignore
                        ( lu :> IInternalLU ).NotifyTerminateTask task1
                        if cnt1 >= 16 then
                            w1.Release() |> ignore
                    }
                )
                task1.p_GetSCSICommand <- ( fun () -> BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.ORDERED_TASK )
                task1.p_GetACANoncompliant <- ( fun () -> false )
                yield TaskStatus.TASK_STAT_Dormant( task1 )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        taskwait.[0].Release() |> ignore

        m_TaskSetQueue.Enqueue( fun () ->
            let queue2 = pc.Invoke( "StartExecutableSCSITasks", queue1 ) :?> TaskSet

            match queue2.Queue.[0] with
            | TaskStatus.TASK_STAT_Dormant( _ ) ->
                Assert.Fail __LINE__
            | _ -> ()

            for i = 1 to 15 do
                match queue2.Queue.[i] with
                | TaskStatus.TASK_STAT_Running( _ ) ->
                    Assert.Fail __LINE__
                | _ -> ()

            pc.SetField( "m_TaskSet", queue2 )
        )

        w1.Wait()
        Assert.True(( cnt1 = 16 ))

        let mutable wcnt = 0
        while m_TaskSetQueue.RunWaitCount > 0u && wcnt < 100 do
            Thread.Sleep 10
        Assert.True(( wcnt < 100 ))

        let m_TaskSet = pc.GetField( "m_TaskSet" ) :?> TaskSet
        Assert.True(( m_TaskSet.Queue.Length = 0 ))

    [<Fact>]
    member this.SCSICommand_001() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let scsicmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK
        let scsidata : SCSIDataOutPDU list = []

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ _ _ _ _ _ _ ->
            Assert.Fail __LINE__
        )

        pc.SetField( "m_LUResetFlag", true )
        ( lu :> ILU ).SCSICommand source scsicmd scsidata

    [<Fact>]
    member this.SCSICommand_002() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let scsicmd = BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK
        let scsidata : SCSIDataOutPDU list = []
        let w = new SemaphoreSlim( 0 )
        let mutable cnt = 0

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ resp stat senseData _ _ _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( senseData.Length > 0 ))
            cnt <- cnt + 1
            w.Release() |> ignore
        )

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendOtherResponse <- ( fun _ _ _ _ ->
            Assert.Fail __LINE__
        )

        ( lu :> ILU ).SCSICommand source scsicmd scsidata

        w.Wait()
        Assert.True(( cnt = 1 ))

    [<Fact>]
    member this.SCSICommand_003() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )
        let scsicmd = {
            BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK with
                ScsiCDB = [| 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; |];
        }
        let scsidata : SCSIDataOutPDU list = []
        let w = new SemaphoreSlim( 0 )
        let mutable cnt2 = 0
        let mutable cnt3 = 0

        media.p_NotifyLUReset <- ( fun _ _ ->
            cnt2 <- cnt2 + 1
        )
        media.p_GetMediaIndex <- ( fun () -> mediaidx_me.zero )
        media.p_GetDescriptString <- ( fun () -> "" )
        sm.p_NotifyLUReset <- ( fun lun lu ->
            cnt3 <- cnt3 + 1
            w.Release() |> ignore
        )

        let task1 =
            new CBlockDeviceTask_Stub(
                p_GetTaskType = ( fun () -> BlockDeviceTaskType.ScsiTask ),
                p_GetSource = ( fun () -> source ),
                p_NotifyTerminate = ( fun _ -> () ),
                p_GetInitiatorTaskTag = ( fun () -> itt_me.fromPrim 1u )
            )

        let testTasks = [|
            for i = 0 to 15 do
                yield TaskStatus.TASK_STAT_Running( task1 )
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )

        ( lu :> ILU ).SCSICommand source scsicmd scsidata

        w.Wait()
        Assert.True(( cnt2 = 1 ))
        Assert.True(( cnt3 = 1 ))

    [<Fact>]
    member this.SCSICommand_004() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let scsicmd = {
            BlockDeviceLU_Test.defaultSCSICommand TaskATTRCd.SIMPLE_TASK with
                ScsiCDB = [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x04uy; |]
        }
        let scsidata : SCSIDataOutPDU list = []
        let w = new SemaphoreSlim( 0 )
        let mutable cnt1 = 0
        let mutable cnt2 = 0

        ( source.ProtocolService :?> CProtocolService_Stub ).p_SendSCSIResponse <- ( fun _ _ _ _ resp stat senseData resData _ _ ->
            Assert.True(( resp = iScsiSvcRespCd.COMMAND_COMPLETE ))
            Assert.True(( stat = ScsiCmdStatCd.GOOD ))
            cnt1 <- cnt1 + 1
            w.Release() |> ignore
        )

        media.p_TestUnitReady <- ( fun itt source ->
            cnt2 <- cnt2 + 1
            ValueNone
        )

        ( lu :> ILU ).SCSICommand source scsicmd scsidata

        w.Wait()
        Assert.True(( cnt1 = 1 ))
        Assert.True(( cnt2 = 1 ))

    [<Fact>]
    member this.GetTaskQueueUsage_001() =
        let media, sm, lu = this.createBlockDevice()
        Assert.True(( 0 = ( lu :> ILU ).GetTaskQueueUsage( tsih_me.fromPrim 0us ) ))

    [<Fact>]
    member this.GetTaskQueueUsage_002() =
        let media, sm, lu = this.createBlockDevice()
        let source = BlockDeviceLU_Test.cmdSource()
        let pc = new PrivateCaller( lu )

        let testTasks = [|
            yield TaskStatus.TASK_STAT_Dormant(
                new CBlockDeviceTask_Stub(
                    p_GetSource = ( fun () -> { source with TSIH = tsih_me.fromPrim 0us } )
                )
            );
            yield TaskStatus.TASK_STAT_Dormant(
                new CBlockDeviceTask_Stub(
                    p_GetSource = ( fun () -> { source with TSIH = tsih_me.fromPrim 1us } )
                )
            );
        |]
        let queue1 = {
            Queue = testTasks.ToImmutableArray();
            ACA = ValueNone;
        }
        pc.SetField( "m_TaskSet", queue1 )
        Assert.True(( 1 = ( lu :> ILU ).GetTaskQueueUsage( tsih_me.fromPrim 0us ) ))
        Assert.True(( 1 = ( lu :> ILU ).GetTaskQueueUsage( tsih_me.fromPrim 1us ) ))
        Assert.True(( 0 = ( lu :> ILU ).GetTaskQueueUsage( tsih_me.fromPrim 2us ) ))
