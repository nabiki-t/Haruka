//=============================================================================
// Haruka Software Storage.
// PRManager.fs : Defines PRManager class.
// PRManager class manage persistent reservation belongings to block device LU.

//=============================================================================
// Namespace declaration

namespace Haruka.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open System.Threading
open System.Threading.Tasks
open System.Threading.Tasks.Dataflow
open System.Collections.Immutable
open System.Text
open System.Text.RegularExpressions
open System.IO

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes
open System.Buffers

//=============================================================================
// Type definition

/// <summary>
/// Data type that represents basic PERSISTENT RESERVE OUT parameter list.
/// </summary>
type BasicParameterList = {
    /// RESERVATION KEY field value.
    ReservationKey : RESVKEY_T;

    /// SERVICE ACTION RESERVATION KEY field value.
    ServiceActionReservationKey : RESVKEY_T;

    /// SPEC_I_PT(Specify Initiator Port) bit value.
    SPEC_I_PT : bool;

    /// ALL_TG_PT(All Target Ports) bit value.
    ALL_TG_PT : bool;

    /// APTPL(Activate Persist Through Power Loss).
    APTPL : bool;

    /// array of TransportID.
    TransportID : ( string * ISID_T option )[];
}

/// <summary>
/// Data type that represents parameter list used for PERSISTENT RESERVE OUT command REGISTER AND MOVE service action.
/// </summary>
type MoveParameterList = {
    /// RESERVATION KEY field value.
    ReservationKey : RESVKEY_T;

    /// SERVICE ACTION RESERVATION KEY field value.
    ServiceActionReservationKey : RESVKEY_T;

    /// UNREG(unregister) bit value.
    UNREG : bool;

    /// APTPL(Activate Persist Through Power Loss).
    APTPL : bool;

    /// RELATIVE TARGET PORT IDENTIFIER field value.
    RelativeTargetPortIdentifier : uint16;

    /// list of TransportID.
    TransportID : ( string * ISID_T option );
}

/// A record type that holds information related to PR. 
/// Exclusive control is performed on this record.
[<NoComparison>]
type PRInfoRec =
    {
        /// Reservation type which is currentry established.
        /// If reservation is not established , m_Type is NO_RESERVATION.
        m_Type : PR_TYPE;

        /// Reservation holder I_T Nexus
        /// If m_Type is NO_RESERVATION, WRITE_EXCLUSIVE_ALL_REGISTRANTS, or EXCLUSIVE_ACCESS_ALL_REGISTRANTS, m_Holder must be None.
        /// Otherwise, m_Holder is not be None.
        m_Holder : ITNexus option;

        /// Counter of Persistent Reservations Generation.
        /// This value is incremented when PERSISTENT RESERVE OUT command that has
        /// REGISTER, REGISTER AND IGNORE EXISTING KEY, REGISTER AND MOVE, CLEAR, PREEMPT, PREEMPT AND ABORT service action is performed.
        /// This value is zero cleared when LU object is newly cleated.
        m_PRGeneration : uint32;

        /// Registration information
        /// If m_Holder is not None, m_Holder must be content in m_Registrations.
        m_Registrations : ImmutableDictionary< ITNexus, RESVKEY_T >;
    }

// ============================================================================
// Class definition of PRManager.
//

/// <summary>
///   PRManager class definition. 
/// </summary>
/// <param name="m_StatusMaster">
///   Interface of status master object.
/// </param>
/// <param name="m_LU">
///   Interface of Logical Unit object that hold this class object.
/// </param>
/// <param name="m_FileName">
///   File name that stores reservation information.
///   If file name is empty, the reservation information is not saved.
/// </param>
type PRManager(
        m_StatusMaster : IStatus,
        m_LU : IInternalLU,
        m_LUN : LUN_T,
        m_FileName : string,
        m_Killer : IKiller
    ) as this =

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    /// persistent reservation information.
    let m_Locker =
        let r = PRManager.LoadPRFile m_ObjID m_LUN m_FileName
        new OptimisticLock< PRInfoRec >( r )

    /// Queue to hold APTPL flag that requested by PERSISTENT RESERVE OUT command.
    let m_SavePRReqQueue = new WorkingTaskQueue< bool >( fun flg -> task {
        do! PRManager.SavePRFile m_ObjID m_LUN m_FileName m_Locker.obj flg
    })

    do
        m_Killer.Add this

    //=========================================================================
    // Interface

    interface IComponent with

        // Terminate method
        member _.Terminate() : unit =
            HLogger.Trace( LogID.V_TRACE, fun g ->
                let loginfo = struct ( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                g.Gen1( loginfo, "Notify terminate" )
            )
            // Post dummy value to the queue
            //( m_SavePRReqQueue :> ITargetBlock< int > ).Post( 2 ) |> ignore
            //m_SavePRReqQueue.Complete()
            m_SavePRReqQueue.Stop()


    //=========================================================================
    // Public method


    /// <summary>
    ///  Decide specified task can be run or not when ACA is established.
    /// </summary>
    /// <param name="source">
    ///  Command source information.
    /// </param>
    /// <param name="lun">
    ///  Logical Unit Number of LU where this object belongings to.
    /// </param>
    /// <param name="itt">
    ///  ITT of SCSI command.
    /// </param>
    /// <param name="cdb">
    ///  Received SCSI CDB.
    /// </param>
    /// <param name="cmdPduData">
    ///  Data segment bytes array in received SCSI Command PDU.
    /// </param>
    /// <param name="data">
    ///  Received SCSI Data-Out PDUs list.
    /// </param>
    /// <param name="faultITNexus">
    ///  fault I_T nexus.
    /// </param>
    /// <returns>
    /// If received task is PERSISTENT RESERVE OUT, and fault I_T nexus is associated with the persistent reservation or registration being preempted.
    /// this function is returns true, and this task must be run eaven if ACA established.
    /// Otherwise it returns false.
    /// </returns>
    /// <remarks>
    /// This method is called in critical section of BlockDeviceLU task set lock.
    /// </remarks>
    member _.decideACANoncompliant
        ( source : CommandSourceInfo )
        ( lun : LUN_T )
        ( itt : ITT_T )
        ( cdb : ICDB )
        ( cmdPduData : PooledBuffer )
        ( data : SCSIDataOutPDU list )
        ( faultITNexus : ITNexus ) : bool =

        if cdb.OperationCode <> 0x5Fuy || cdb.ServiceAction <> 0x0005us then
            // It Operation Code is not PERSISTENT RESERVE OUT, Service Action is not PREEMPT AND ABORT,
            // the task is must executed in ACA compliant.
            false
        else
            let pr = m_Locker.obj
            let fitnRegved, fitnResvKey = pr.m_Registrations.TryGetValue faultITNexus
            if pr.m_Holder.IsNone || not fitnRegved then
                // If there are no reservation, reservation type is all registrants, or fault I_T Nexus is not registered,
                // the task is must executed in ACA compliant.
                false
            else
                let wcdb = cdb :?> PersistentReserveOutCDB
                let parameterList = SCSIDataOutPDU.AppendParamList cmdPduData data ( int wcdb.ParameterListLength )
                let basicParam = PRManager.paramDataToBasicParameterList source m_ObjID lun itt wcdb.ParameterListLength parameterList
                parameterList.Return()
                ( fitnResvKey = basicParam.ServiceActionReservationKey )

    /// <summary>
    ///  Before task is performed, check this task can be run or not by persistent reservation.
    /// </summary>
    /// <param name="source">
    ///  Information that represents source of the task specified in the argument "task".
    /// </param>
    /// <param name="bdtask">
    ///  Task that will be performed.
    /// </param>
    /// <returns>
    ///  If specified task is blocked by reservation, this function returns true.
    /// </returns>
    member _.IsBlockedByPersistentReservation ( source : CommandSourceInfo ) ( bdtask : IBlockDeviceTask ) : bool =
        let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( bdtask.InitiatorTaskTag ), ValueSome( m_LU.LUN ) )
        let pr = m_Locker.obj
        let srcIsRegistered = pr.m_Registrations.ContainsKey( source.I_TNexus )

        let bdtask_cdb = bdtask.CDB
        if bdtask.TaskType = BlockDeviceTaskType.InternalTask || bdtask_cdb.IsNone then
            // If specified task is internal task, it can be executed independently of the reservation.
            false
        elif pr.m_Type = PR_TYPE.NO_RESERVATION then
            // Nothing to do
            false
        elif pr.m_Holder.IsSome && source.I_TNexus = pr.m_Holder.Value then
            false
        else
            let confFunc() =
                // Reservation conflict
                HLogger.Trace(
                    LogID.I_RESERVATION_CONFLICT,
                    fun g -> g.Gen5(
                        loginfo,
                        ( PR_TYPE.toStringName pr.m_Type ),
                        "",
                        "",
                        source.I_TNexus.I_TNexusStr,
                        bdtask.DescString
                    )
                )
                true

            let alloFunc() = false

            let f ( a1 : unit -> bool ) ( a2 : unit -> bool ) ( a3 : unit -> bool ) ( a4 : unit -> bool ) ( a5 : unit -> bool ) : bool =
                if pr.m_Type = WRITE_EXCLUSIVE then
                    a1()
                elif pr.m_Type = EXCLUSIVE_ACCESS then
                    a2()
                elif srcIsRegistered then
                    a3()
                elif pr.m_Type = WRITE_EXCLUSIVE_REGISTRANTS_ONLY || pr.m_Type = WRITE_EXCLUSIVE_ALL_REGISTRANTS then
                    a4()
                elif pr.m_Type = EXCLUSIVE_ACCESS_REGISTRANTS_ONLY || pr.m_Type = EXCLUSIVE_ACCESS_ALL_REGISTRANTS then
                    a5()
                else
                    // error
                    assert( false )
                    false

            match bdtask_cdb.Value.Type with
            | AccessControlIn -> // SPC-3 8.3.2 ACCESS CONTROL IN command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | AccessControlOut -> // SPC-3 8.3.3 ACCESS CONTROL OUT command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | ChangeAliases -> // SPC-3 6.2 CHANGE ALIASES command
                f confFunc confFunc alloFunc confFunc confFunc
            | ExtendedCopy -> // SPC-3 6.3 EXTENDED COPY command
                f confFunc confFunc alloFunc confFunc confFunc
            | Inquiry -> // SPC-3 6.4 INQUIRY command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | LogSelect -> // SPC-3 6.5 LOG SELECT command
                f confFunc confFunc alloFunc confFunc confFunc
            | LogSense -> // SPC-3 6.6 LOG SENSE command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | ModeSelect -> // SPC-3 6.7 MODE SELECT(6), 6.8 MODE SELECT(10) command
                f confFunc confFunc alloFunc confFunc confFunc
            | ModeSense -> // SPC-3 6.9 MODE SENSE(6), 6.10 MODE SENSE(10) command
                f confFunc confFunc alloFunc confFunc confFunc
            | PersistentReserveIn -> // SPC-3 6.11 PERSISTENT RESERVE IN command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | PersistentReserveOut -> // SPC-3 6.12 PERSISTENT RESERVE OUT command

                match bdtask_cdb.Value.ServiceAction with
                | 0x0000us ->   // REGISTER
                    alloFunc()
                | 0x0001us ->   // RESERVE
                    confFunc()
                | 0x0002us ->   // RELEASE
                    if srcIsRegistered then alloFunc() else confFunc()
                | 0x0003us ->   // CLEAR
                    if srcIsRegistered then alloFunc() else confFunc()
                | 0x0004us ->   // PREEMPT
                    if srcIsRegistered then alloFunc() else confFunc()
                | 0x0005us ->   // REEMPT AND ABORT
                    if srcIsRegistered then alloFunc() else confFunc()
                | 0x0006us ->   // REGISTER AND IGNORE EXISTING KEY
                    alloFunc()
                | 0x0007us ->   // REGISTER AND MOVE
                    confFunc()
                | _ ->          // error
                    assert( false )
                    false

            | PreventAllowMediumRemoval -> // SPC-3 6.13 PREVENT ALLOW MEDIUM REMOVAL command
                let wcdb = bdtask_cdb.Value :?> PreventAllowMediumRemovalCDB
                if wcdb.Prevent = 0uy then
                    f alloFunc alloFunc alloFunc alloFunc alloFunc
                else
                    f confFunc confFunc alloFunc confFunc confFunc
            | ReadAttribute -> // SPC-3 6.14 READ ATTRIBUTE command
                f confFunc confFunc alloFunc confFunc confFunc
            | ReadBuffer -> // SPC-3 6.15 READ BUFFER command
                f confFunc confFunc alloFunc confFunc confFunc
            | ReadMediaSerialNumber -> // SPC-3 6.16 READ MEDIA SERIAL NUMBER command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | ReceiveCopyResults -> // SPC-3 6.17 RECEIVE COPY RESULTS command
                f confFunc confFunc alloFunc confFunc confFunc
            | ReceiveDiagnosticResults -> // SPC-3 6.18 RECEIVE DIAGNOSTIC RESULTS command
                f confFunc confFunc alloFunc confFunc confFunc
            | ReportAliases -> // SPC-3 6.19 REPORT ALIASES command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | ReportDeviceIdentifier -> // SPC-3 6.20 REPORT DEVICE IDENTIFIER command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | ReportLUNs -> // SPC-3 6.21 REPORT LUNS command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | ReportPriority -> // SPC-3 6.22 REPORT PRIORITY command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | ReportSupportedOperationCodes -> // SPC-3 6.23 REPORT SUPPORTED OPERATION CODES command
                f confFunc confFunc alloFunc confFunc confFunc
            | ReportSupportedTaskManagementFunctions -> // SPC-3 6.24 REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS command
                f confFunc confFunc alloFunc confFunc confFunc
            | ReportTargetPortGroups -> // SPC-3 6.25 REPORT TARGET PORT GROUPS command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | ReportTimestamp -> // SPC-3 6.26 REPORT TIMESTAMP command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | RequestSense -> // SPC-3 6.27 REQUEST SENSE command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | SendDiagnostic -> // SPC-3 6.28 SEND DIAGNOSTIC command
                f confFunc confFunc alloFunc confFunc confFunc
            | SetDeviceIdentifier -> // SPC-3 6.29 SET DEVICE IDENTIFIER command
                f confFunc confFunc alloFunc confFunc confFunc
            | SetPriority -> // SPC-3 6.30 SET PRIORITY command
                f confFunc confFunc alloFunc confFunc confFunc
            | SetTargetPortGroups -> // SPC-3 6.31 SET TARGET PORT GROUPS command
                f confFunc confFunc alloFunc confFunc confFunc
            | SetTimestamp -> // SPC-3 6.32 SET TIMESTAMP command
                f confFunc confFunc alloFunc confFunc confFunc
            | TestUnitReady -> // SPC-3 6.33 TEST UNIT READY command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | WriteAttribute -> // SPC-3 6.34 WRITE ATTRIBUTE command
                f confFunc confFunc alloFunc confFunc confFunc
            | WriteBuffer -> // SPC-3 6.35 WRITE BUFFER command
                f confFunc confFunc alloFunc confFunc confFunc
            | FormatUnit -> // SBC-2 5.2 FORMAT UNIT command
                f confFunc confFunc alloFunc confFunc confFunc
            | PreFetch -> // SBC-2 5.3 PRE-FETCH(10), 5.4 PRE-FETCH(16) command
                f alloFunc confFunc alloFunc alloFunc confFunc
            | Read -> // SBC-2 5.5 READ(6), 5.6 READ(10), 5.7 READ(12), 5.8 READ(16), 5.9 READ(32) command
                f alloFunc confFunc alloFunc alloFunc confFunc
            | ReadCapacity -> // SBC-2 5.10 READ CAPACITY(10), 5.11 READ CAPACITY(16) command
                f alloFunc alloFunc alloFunc alloFunc alloFunc
            | ReadDefectData -> // SBC-2 5.12 READ DEFECT DATA(10), 5.13 READ DEFECT DATA(12) command
                f confFunc confFunc alloFunc confFunc confFunc
            | ReadLong -> 
                f confFunc confFunc alloFunc confFunc confFunc
            | ReassignBlocks -> // SBC-2 5.16 REASSIGN BLOCKS command
                f confFunc confFunc alloFunc confFunc confFunc
            | StartStopUnit -> // SBC-2 5.17 START STOP UNIT command
                let wcdb = bdtask_cdb.Value :?> StartStopUnitCDB
                if wcdb.Start = true && wcdb.PowerCondition = 0uy then
                    f alloFunc alloFunc alloFunc alloFunc alloFunc
                else
                    f confFunc confFunc alloFunc confFunc confFunc
            | SynchronizeCache -> // SBC-2 5.18 SYNCHRONIZE CACHE(10), 5.19 SYNCHRONIZE CACHE(16) command
                f confFunc confFunc alloFunc confFunc confFunc
            | Verify -> // SBC-2 5.20 VERIFY(10), 5.21 VERIFY(12), 5.22 VERIFY(16), 5.23 VERIFY(32) command
                f alloFunc confFunc alloFunc alloFunc confFunc
            | Write -> // SBC-2 5.24 WRITE(6), 5.25 WRITE(10), 5.26 WRITE(12), 5.27 WRITE(16), 5.28 WRITE(32) command
                f confFunc confFunc alloFunc confFunc confFunc
            | WriteAndVerify -> // SBC-2 5.29 WRITE AND VERIFY(10), 5.30 WRITE AND VERIFY(12), 5.31 WRITE AND VERIFY(16), 5.32 WRITE AND VERIFY(32) command
                f confFunc confFunc alloFunc confFunc confFunc
            | WriteLong -> // SBC-2 5.33 WRITE LONG(10), 5.34 WRITE LONG(16) command
                f confFunc confFunc alloFunc confFunc confFunc
            | WriteSame -> // SBC-2 5.35 WRITE SAME(10), 5.36 WRITE SAME(16), 5.37 WRITE SAME(32) command
                f confFunc confFunc alloFunc confFunc confFunc
            | XDRead -> // SBC-2 5.38 XDREAD(10), 5.39 XDREAD(32) command
                f alloFunc confFunc alloFunc alloFunc confFunc
            | XDWrite -> // SBC-2 5.40 XDWRITE(10), 5.41 XDWRITE(32) command
                f confFunc confFunc alloFunc confFunc confFunc
            | XDWriteRead -> // SBC-2 5.42 XDWRITEREAD(10), 5.43 XDWRITEREAD(32) command
                f confFunc confFunc alloFunc confFunc confFunc
            | XPWrite -> // SBC-2 5.44 XPWRITE(10), 5.45 XPWRITE(32) command
                f confFunc confFunc alloFunc confFunc confFunc

    // ========================================================================
    // Public member function for PERSISTENT RESERVE IN command.


    /// <summary>
    /// READ KEY service action of PERSISTENT RESERVE IN command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE IN command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE IN command.
    /// </param>
    /// <returns>
    /// Returned parameter data for READ KEY service action of PERSISTENT RESERVE IN command.
    /// </returns>
    member _.ReadKey ( source : CommandSourceInfo ) ( itt : ITT_T ) : byte[] =
        let pr = m_Locker.obj
        let v =
            pr.m_Registrations
            |> Seq.sortWith ( fun a b -> ITNexus.Compare a.Key b.Key )
            |> Seq.toArray
        [|
            // PRGENERATION
            yield! Functions.UInt32ToNetworkBytes_NewVec pr.m_PRGeneration;
            // ADDITIONAL LENGTH
            yield! Functions.Int32ToNetworkBytes_NewVec ( pr.m_Registrations.Count * 8 );
            // RESERVATION KEYs
            for itr in v do
                yield! Functions.UInt64ToNetworkBytes_NewVec ( resvkey_me.toPrim itr.Value );
        |]

    /// <summary>
    /// READ RESERVATION service action of PERSISTENT RESERVE IN command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE IN command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE IN command.
    /// </param>
    /// <returns>
    /// Returned parameter data for READ RESERVATION service action of PERSISTENT RESERVE IN command.
    /// </returns>
    member _.ReadReservation ( source : CommandSourceInfo ) ( itt : ITT_T ) : byte[] =
        let pr = m_Locker.obj
        if pr.m_Type = PR_TYPE.NO_RESERVATION then
            [|
                // PRGENERATION
                yield! Functions.UInt32ToNetworkBytes_NewVec pr.m_PRGeneration;
                // ADDITIONAL LENGTH
                yield! Functions.Int32ToNetworkBytes_NewVec 0;
            |]
        else
            [|
                // PRGENERATION
                yield! Functions.UInt32ToNetworkBytes_NewVec pr.m_PRGeneration;
                // ADDITIONAL LENGTH
                yield! Functions.Int32ToNetworkBytes_NewVec 0x10;
                // RESERVATION KEY
                if PR_TYPE.isAllRegistrants pr.m_Type then
                    yield! Functions.UInt64ToNetworkBytes_NewVec ( 0UL );
                else
                    let _, holderKey = pr.m_Registrations.TryGetValue( pr.m_Holder.Value )
                    yield! Functions.UInt64ToNetworkBytes_NewVec ( resvkey_me.toPrim holderKey );
                // Obsoluted
                yield 0uy;
                yield 0uy;
                yield 0uy;
                yield 0uy;
                // Reserved
                yield 0uy;
                // SCOPE(LU_SCOPE:0), TYPE
                yield PR_TYPE.toNumericValue pr.m_Type;
                // Obsoluted
                yield 0uy;
                yield 0uy;
            |]

    /// <summary>
    /// REPORT CAPABILITIES service action of PERSISTENT RESERVE IN command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE IN command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE IN command.
    /// </param>
    /// <returns>
    /// Returned parameter data for REPORT CAPABILITIES service action of PERSISTENT RESERVE IN command.
    /// </returns>
    member _.ReportCapabilities ( source : CommandSourceInfo ) ( itt : ITT_T ) : byte[] =
        [|
            // LENGTH
            yield 0x00uy;
            yield 0x08uy;
            // CRH(0), SPI_C(1), ATP_C(1), PTPL_C(1)
            yield 0x0Duy;
            // TMV(1), PTPL_A
            if File.Exists m_FileName then
                yield 0x81uy;
            else
                yield 0x80uy;
            // PERSISTENT RESERVATION TYPE MASK
            yield 0xEAuy;   // WR_EX_AR(1), EX_AC_RO(1), WR_EX_RO(1), EX_AC(1) WR_EX(1)
            yield 0x01uy;   // EX_AC_AR(1)
            // Reserved
            yield 0x00uy;
            yield 0x00uy;
        |]

    /// <summary>
    /// READ FULL STATUS service action of PERSISTENT RESERVE IN command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE IN command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE IN command.
    /// </param>
    /// <returns>
    /// Returned parameter data for READ FULL STATUS service action of PERSISTENT RESERVE IN command.
    /// </returns>
    member _.ReadFullStatus ( source : CommandSourceInfo ) ( itt : ITT_T ) : byte[] =
        let pr = m_Locker.obj
        let desc = [|
            let v =
                pr.m_Registrations
                |> Seq.sortWith ( fun a b -> ITNexus.Compare a.Key b.Key )
            for itr in v do
                let iITN = itr.Key
                let iKey = itr.Value

                // RESERVATION KEY
                yield! Functions.UInt64ToNetworkBytes_NewVec ( resvkey_me.toPrim iKey );
                // Reserved
                yield 0x00uy;
                yield 0x00uy;
                yield 0x00uy;
                yield 0x00uy;
                // ALL_TG_PT, R_HOLDER
                let holderFlg, typeVal =
                    if pr.m_Type = PR_TYPE.NO_RESERVATION then
                        0x00uy, 0x00uy
                    elif PR_TYPE.isAllRegistrants pr.m_Type || pr.m_Holder.Value = iITN then
                        0x01uy, PR_TYPE.toNumericValue pr.m_Type
                    else
                        0x00uy, 0x00uy
                yield holderFlg;
                // SCOPE(0), TYPE
                yield typeVal;
                // Reserved
                yield 0x00uy;
                yield 0x00uy;
                yield 0x00uy;
                yield 0x00uy;
                // RELATIVE TARGET PORT IDENTIFIER
                yield! Functions.UInt16ToNetworkBytes_NewVec ( tpgt_me.toPrim iITN.TPGT )

                // prepare initiator name bytes array for TransportID
                let initiatorPortNameStr = Encoding.UTF8.GetBytes iITN.InitiatorPortName
                let initiatorPortNameBytesLen = Functions.AddPaddingLengthUInt32 ( uint32 initiatorPortNameStr.Length + 1u ) 4u
                let buf = Array.zeroCreate<byte> ( int initiatorPortNameBytesLen )
                Array.blit initiatorPortNameStr 0 buf 0 initiatorPortNameStr.Length

                // ADDITIONAL DESCRIPTOR LENGTH
                yield! Functions.UInt32ToNetworkBytes_NewVec ( initiatorPortNameBytesLen + 4u )

                // FORMAT CODE(01b),  PROTOCOL IDENTIFIER(5)
                yield 0x45uy;
                // Reserved
                yield 0x00uy;
                // ADDITIONAL LENGTH
                yield! Functions.UInt16ToNetworkBytes_NewVec ( uint16 initiatorPortNameBytesLen )
                // ISCSI NAME
                yield! buf
            |]
        [|
            // PRGENERATION
            yield! Functions.UInt32ToNetworkBytes_NewVec pr.m_PRGeneration;
            // ADDITIONAL LENGTH
            yield! Functions.Int32ToNetworkBytes_NewVec desc.Length;
            // Full status descriptors
            yield! desc
        |]

                                    
    // ========================================================================
    // Public member function for PERSISTENT RESERVE OUT command.


    /// <summary>
    /// REGISTER service action of PERSISTENT RESERVE OUT command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="prType">
    /// TYPE field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// Parameter length field value of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    member this.Register ( source : CommandSourceInfo ) ( itt : ITT_T ) ( prType : PR_TYPE ) ( paramLen : uint32 ) ( param : PooledBuffer ) : ScsiCmdStatCd =
        this.Register_sub source itt prType paramLen param false

    /// <summary>
    /// RESERVE service action of PERSISTENT RESERVE OUT command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="prType">
    /// TYPE field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// Parameter list bytes count value PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    member _.Reserve ( source : CommandSourceInfo ) ( itt : ITT_T ) ( prType : PR_TYPE ) ( paramLen : uint32 ) ( param : PooledBuffer ) : ScsiCmdStatCd =
        let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( itt ), ValueSome( m_LU.LUN ) )
        let basicParam = PRManager.paramDataToBasicParameterList source m_ObjID m_LU.LUN itt paramLen param

        let struct( statCode, msg ) =
            m_Locker.Update ( fun oldVal ->
                // Check source I_T Nexus is registered or not.
                let srcRegved, srcResvKey = oldVal.m_Registrations.TryGetValue source.I_TNexus

                if not srcRegved then
                    // If RESERVE service action was received from unregistered I_T Nexus, return CHECK CONDITION status
                    let msg = "PERSISTENT RESERVE OUT command with RESERVE service action was received from unregistered I_T Nexus."
                    struct( oldVal, struct ( ScsiCmdStatCd.RESERVATION_CONFLICT, msg ) )

                elif srcResvKey <> basicParam.ReservationKey then
                    oldVal, ( ScsiCmdStatCd.RESERVATION_CONFLICT, "Reservation key unmatched." )

                elif oldVal.m_Type = PR_TYPE.NO_RESERVATION then
                    // establish new reservation
                    struct(
                        {
                            oldVal with
                                m_Type = prType;
                                m_Holder =
                                    if PR_TYPE.isAllRegistrants prType then
                                        None
                                    else
                                        Some source.I_TNexus;
                        },
                        struct( ScsiCmdStatCd.GOOD, "" )
                    )

                elif PR_TYPE.isAllRegistrants oldVal.m_Type then
                    if prType = oldVal.m_Type then
                        // Nothing to do
                        struct( oldVal, struct( ScsiCmdStatCd.GOOD, "" ) )
                    else
                        // PR TYPE unmatched
                        struct( oldVal, struct( ScsiCmdStatCd.RESERVATION_CONFLICT, "Reservation type unmatched." ) )

                elif oldVal.m_Holder.Value <> source.I_TNexus then
                    let msg = "Not the holder of the persistent reservation I_T Nexus requested that the reservation be established."
                    struct( oldVal, struct( ScsiCmdStatCd.RESERVATION_CONFLICT, msg ) )

                elif prType = oldVal.m_Type then
                    // Nothing to do
                    struct( oldVal, struct( ScsiCmdStatCd.GOOD, "" ) )

                else
                    // PR TYPE unmatched
                    struct( oldVal, struct( ScsiCmdStatCd.RESERVATION_CONFLICT, "Reservation type unmatched." ) )
            )

        // Request saving PR info to file.
        //( m_SavePRReqQueue :> ITargetBlock< int > ).Post( if basicParam.APTPL then 1 else 0 ) |> ignore
        m_SavePRReqQueue.Enqueue basicParam.APTPL

        if statCode = ScsiCmdStatCd.RESERVATION_CONFLICT then
            HLogger.Trace( LogID.W_RESERVATION_CONFLICT, fun g -> g.Gen2( loginfo, "PERSISTENT RESERVE OUT(RESERVE service action)", msg ) )
            ScsiCmdStatCd.RESERVATION_CONFLICT
        else
            statCode

    /// <summary>
    /// RELEASE service action of PERSISTENT RESERVE OUT command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="prType">
    /// TYPE field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// Parameter list bytes count value PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    member _.Release ( source : CommandSourceInfo ) ( itt : ITT_T ) ( prType : PR_TYPE ) ( paramLen : uint32 ) ( param : PooledBuffer ) : ScsiCmdStatCd =
        let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( itt ), ValueSome( m_LU.LUN ) )
        let basicParam = PRManager.paramDataToBasicParameterList source m_ObjID m_LU.LUN itt paramLen param

        let struct( rStatus, rMessage, uaList ) =
            m_Locker.Update ( fun oldVal ->
                // Check source I_T Nexus is registered or not.
                let srcRegved, srcResvKey = oldVal.m_Registrations.TryGetValue source.I_TNexus

                let r =
                    if not srcRegved then
                        // If RELEASE service action was received from unregistered I_T Nexus, return RESERVATION_CONFLICT status
                        let msg = "Command was received from unregistered I_T Nexus."
                        ValueSome( struct( ScsiCmdStatCd.RESERVATION_CONFLICT, msg, [] ) )

                    elif srcResvKey <> basicParam.ReservationKey then
                        // Reservation key unmatched
                        ValueSome( struct( ScsiCmdStatCd.RESERVATION_CONFLICT, "Reservation key unmatched.", [] ) )

                    elif oldVal.m_Type = PR_TYPE.NO_RESERVATION then
                        // Nothig to do
                        ValueSome( struct( ScsiCmdStatCd.GOOD, "", [] ) )

                    elif not ( PR_TYPE.isAllRegistrants oldVal.m_Type ) then
                        if not ( ITNexus.Equals( oldVal.m_Holder.Value, source.I_TNexus ) ) then
                            // If source I_T Nexus is not reservation holder, nothing to do
                            ValueSome( struct( ScsiCmdStatCd.GOOD, "", [] ) )

                        elif oldVal.m_Type <> prType then
                            // specified TYPE value unmatched.
                            let msg =
                                sprintf
                                    "Specified TYPE value(%s) is unmatched with established reservation type(%s)"
                                    ( PR_TYPE.toStringName prType )
                                    ( PR_TYPE.toStringName oldVal.m_Type )
                            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                            raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_RELEASE_OF_PERSISTENT_RESERVATION, msg )
                            ValueSome( struct( ScsiCmdStatCd.CHECK_CONDITION, "", [] ) )

                        else
                            // Release reservation
                            ValueNone
                    else
                        if oldVal.m_Type <> prType then
                            // specified TYPE value unmatched.
                            let msg =
                                sprintf
                                    "Specified TYPE value(%s) is unmatched with established reservation type(%s)"
                                    ( PR_TYPE.toStringName prType )
                                    ( PR_TYPE.toStringName oldVal.m_Type )
                            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                            raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_RELEASE_OF_PERSISTENT_RESERVATION, msg )
                            ValueSome( struct( ScsiCmdStatCd.CHECK_CONDITION, "", [] ) )
                        else
                            // Release reservation
                            ValueNone

                if r.IsSome then
                    struct( oldVal, r.Value )
                else
                    if ( not <| PR_TYPE.isAllRegistrants oldVal.m_Type ) || oldVal.m_Registrations.Count <= 1 then
                        let uaList =
                            if PR_TYPE.isAllRegistrants oldVal.m_Type || PR_TYPE.isRegistrantsOnly oldVal.m_Type then
                                // establish Unit Attention
                                oldVal.m_Registrations
                                |> Seq.map _.Key
                                |> Seq.sortWith ITNexus.Compare
                                |> Seq.filter ( fun itr -> ITNexus.Equals( itr, source.I_TNexus ) |> not )
                                |> Seq.map ITNexus.getInitiatorPortName
                                |> Seq.distinct
                                |> Seq.toList
                            else
                                []

                        // Release reservation
                        struct(
                            {
                                oldVal with
                                    m_Type = PR_TYPE.NO_RESERVATION;
                                    m_Holder = None;
                            },
                            struct( ScsiCmdStatCd.GOOD, "", uaList )
                        )
                    else
                        struct( oldVal, struct( ScsiCmdStatCd.GOOD, "", [] ) )
            )

        // Request saving PR info to file.
        //( m_SavePRReqQueue :> ITargetBlock< int > ).Post( if basicParam.APTPL then 1 else 0 ) |> ignore
        m_SavePRReqQueue.Enqueue basicParam.APTPL

        if rStatus = ScsiCmdStatCd.RESERVATION_CONFLICT then
            HLogger.Trace( LogID.W_RESERVATION_CONFLICT, fun g -> g.Gen2( loginfo, "PERSISTENT RESERVE OUT(RELEASE service action)", rMessage ) )
            ScsiCmdStatCd.RESERVATION_CONFLICT
        else
            // Establish Unit Attention
            let ex =
                new SCSIACAException (
                    source,
                    true,
                    SenseKeyCd.UNIT_ATTENTION,
                    ASCCd.RESERVATIONS_PREEMPTED,
                    sprintf "Persistent reservation established. source=%s" source.I_TNexus.I_TNexusStr
                )
            uaList
            |> List.iter ( fun itr -> m_LU.EstablishUnitAttention itr ex )
            rStatus


    /// <summary>
    /// CLEAR service action of PERSISTENT RESERVE OUT command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="prType">
    /// TYPE field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// Parameter list bytes count value PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    member _.Clear ( source : CommandSourceInfo ) ( itt : ITT_T ) ( prType : PR_TYPE ) ( paramLen : uint32 ) ( param : PooledBuffer ) : ScsiCmdStatCd =
        let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( itt ), ValueSome( m_LU.LUN ) )
        let basicParam = PRManager.paramDataToBasicParameterList source m_ObjID m_LU.LUN itt paramLen param

        let struct( rStatus, rMessage, uaList ) =
            m_Locker.Update ( fun oldVal ->
                // Check source I_T Nexus is registered or not.
                let srcRegved, srcResvKey = oldVal.m_Registrations.TryGetValue source.I_TNexus

                if not srcRegved then
                    // If CLEAR service action was received from unregistered I_T Nexus, return RESERVATION_CONFLICT status
                    struct( oldVal, struct ( ScsiCmdStatCd.RESERVATION_CONFLICT, "Command was received from unregistered I_T Nexus.", [] ) )

                elif srcResvKey <> basicParam.ReservationKey then
                    // Reservation key unmatched
                    struct( oldVal, struct ( ScsiCmdStatCd.RESERVATION_CONFLICT, "Reservation key unmatched.", [] ) )

                else
                    // Prepare initiator names for establish unit attention.
                    let uaList =
                        oldVal.m_Registrations
                        |> Seq.map _.Key
                        |> Seq.sortWith ITNexus.Compare
                        |> Seq.filter ( fun itr -> ITNexus.Equals( itr, source.I_TNexus ) |> not )
                        |> Seq.map ITNexus.getInitiatorPortName
                        |> Seq.distinct
                        |> Seq.toList
    
                    struct (
                        {
                                m_Type = PR_TYPE.NO_RESERVATION;
                                m_Holder = None;
                                m_PRGeneration = oldVal.m_PRGeneration + 1u;
                                m_Registrations = ImmutableDictionary.Empty;
                        },
                        struct ( ScsiCmdStatCd.GOOD, "", uaList )
                    )
            )

        // Request saving PR info to file.
        //( m_SavePRReqQueue :> ITargetBlock< int > ).Post( if basicParam.APTPL then 1 else 0 ) |> ignore
        m_SavePRReqQueue.Enqueue basicParam.APTPL

        if rStatus = ScsiCmdStatCd.RESERVATION_CONFLICT then
            HLogger.Trace( LogID.W_RESERVATION_CONFLICT, fun g -> g.Gen2( loginfo, "PERSISTENT RESERVE OUT(RELEASE service action)", rMessage ) )
            ScsiCmdStatCd.RESERVATION_CONFLICT
        else
            // establish unit attention
            let ex =
                new SCSIACAException (
                    source,
                    true,
                    SenseKeyCd.UNIT_ATTENTION,
                    ASCCd.RESERVATIONS_PREEMPTED,
                    sprintf "Persistent reservation established. source=%s" source.I_TNexus.I_TNexusStr
                )
            uaList
            |> List.iter ( fun itr -> m_LU.EstablishUnitAttention itr ex )

            rStatus


    /// <summary>
    /// PREEMPT service action of PERSISTENT RESERVE OUT command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="prType">
    /// TYPE field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// Parameter list bytes count value PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    member this.Preempt ( source : CommandSourceInfo ) ( itt : ITT_T ) ( prType : PR_TYPE ) ( paramLen : uint32 ) ( param : PooledBuffer ) : ScsiCmdStatCd =
        let struct( rStatus, _, _, _ ) = this.Preempt_sub source itt prType paramLen param
        rStatus

    /// <summary>
    /// PREEMPT AND ABORT service action of PERSISTENT RESERVE OUT command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="prType">
    /// TYPE field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// Parameter list bytes count value PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <returns>
    /// Pair of status, preempted I_T Nesus, preempted reservation type nad specified service action reservation key value.
    /// </returns>
    member this.PreemptAndAbort ( source : CommandSourceInfo ) ( itt : ITT_T ) ( prType : PR_TYPE ) ( paramLen : uint32 ) ( param : PooledBuffer ) :
            struct ( ScsiCmdStatCd * ITNexus[] * PR_TYPE * RESVKEY_T ) =
        this.Preempt_sub source itt prType paramLen param

    /// <summary>
    /// REGISTER AND IGNORE EXISTING KEY service action of PERSISTENT RESERVE OUT command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="prType">
    /// TYPE field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// Parameter length field value of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    member this.RegisterAndIgnoreExistingKey ( source : CommandSourceInfo ) ( itt : ITT_T ) ( prType : PR_TYPE ) ( paramLen : uint32 ) ( param : PooledBuffer ) : ScsiCmdStatCd =
        this.Register_sub source itt prType paramLen param true


    /// <summary>
    /// REGISTER AND MOVE service action of PERSISTENT RESERVE OUT command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="prType">
    /// TYPE field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// PARAMETER LIST LENGTH field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    member _.RegisterAndMove ( source : CommandSourceInfo ) ( itt : ITT_T ) ( prType : PR_TYPE ) ( paramLen : uint32 ) ( param : PooledBuffer ) : ScsiCmdStatCd =
        let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( itt ), ValueSome( m_LU.LUN ) )
        let moveParam = PRManager.paramDataToMoveParameterList source m_ObjID m_LU.LUN itt paramLen param

        let struct( rStatus, rMessage ) =
            m_Locker.Update ( fun oldVal ->
                // Check source I_T Nexus is registered or not.
                let srcRegved, srcResvKey = oldVal.m_Registrations.TryGetValue source.I_TNexus

                // If There is no reservation, this command terminated in CHECK CONDITION
                if oldVal.m_Type = PR_TYPE.NO_RESERVATION then
                    // return CHECK CONDITION status
                    let msg = "PERSISTENT RESERVE OUT command with REGISTER AND MOVE service action was received, and there is no reservation."
                    HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                    raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                    struct( oldVal, struct( ScsiCmdStatCd.CHECK_CONDITION, "" ) )

                elif not srcRegved then
                    let msg = "Request was received from unregistered I_T Nexus, but reservation is already existed."
                    struct( oldVal, struct( ScsiCmdStatCd.RESERVATION_CONFLICT, msg ) )

                elif PR_TYPE.isAllRegistrants oldVal.m_Type then
                    let msg = sprintf "It will be established reservation is %s." ( PR_TYPE.toStringName oldVal.m_Type )
                    struct( oldVal, struct( ScsiCmdStatCd.RESERVATION_CONFLICT, msg ) )

                elif not( ITNexus.Equals( source.I_TNexus, oldVal.m_Holder.Value ) ) then
                    // Source I_T Nexus was registered, but it does not have reservation.
                    let msg = "Request was received from registered I_T Nexus, but it does not have the reservation."
                    struct( oldVal, struct( ScsiCmdStatCd.RESERVATION_CONFLICT, msg ) )

                elif moveParam.ReservationKey <> srcResvKey then
                    // Reservation Key mismatch
                    let msg = "Request was received from the I_T Nexus that holds reservation, but reservation key mismatch."
                    struct( oldVal, struct( ScsiCmdStatCd.RESERVATION_CONFLICT, msg ) )

                elif moveParam.ServiceActionReservationKey = resvkey_me.zero then
                    // return CHECK CONDITION status
                    let msg = "Request was received from the I_T Nexus that holds reservation, but SERVICE ACTION RESERVATION KEY is 0."
                    HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                    raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                    struct( oldVal, struct( ScsiCmdStatCd.CHECK_CONDITION, "" ) )

                else
                    // check TransportID specifies source I_T Nexus or not
                    let ( iname, isid_o ) = moveParam.TransportID
                    if String.Equals( source.I_TNexus.InitiatorName, iname, StringComparison.Ordinal ) &&
                            ( isid_o.IsNone || source.I_TNexus.ISID = isid_o.Value ) then
                        // CHECK CONDITION
                        let msg = sprintf "TransportID specifies initiator port same as source I_T Nexus. Initiator Name=%s" iname
                        HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, msg )
                        raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, msg )

                    let allITNexus = m_StatusMaster.GetITNexusFromLUN m_LU.LUN
                    let itNexuses =
                        [|
                            // Register I_T Nexus specified in TransportID.
                            let ( iname, isid_o ) = moveParam.TransportID
                            if isid_o.IsNone then
                                // TransportID is only initiator name.
                                for j in allITNexus do
                                    if String.Equals( j.InitiatorName, iname, StringComparison.Ordinal ) then
                                        yield new ITNexus( iname, j.ISID, source.I_TNexus.TargetName, tpgt_me.fromPrim moveParam.RelativeTargetPortIdentifier )
                            else
                                // TransportID is initiator name + ISID.
                                yield new ITNexus( iname, isid_o.Value, source.I_TNexus.TargetName, tpgt_me.fromPrim moveParam.RelativeTargetPortIdentifier );
                        |]

                    if itNexuses.Length <> 1 then
                        // CHECK CONDITION
                        let msg = "Move target TransportID is not unique. "
                        HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, msg )
                        raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, msg )

                    // Update reservation
                    let nextRegist1 =
                        if oldVal.m_Registrations.ContainsKey( itNexuses.[0] ) then
                            oldVal.m_Registrations.SetItem( itNexuses.[0], moveParam.ServiceActionReservationKey )
                        else
                            oldVal.m_Registrations.Add( itNexuses.[0], moveParam.ServiceActionReservationKey )

                    // Delete source I_T Nexus if nesserly.
                    let nextRegist2 =
                        if moveParam.UNREG then
                            nextRegist1.Remove( source.I_TNexus )
                        else
                            nextRegist1

                    if nextRegist2.Count > Constants.PRDATA_MAX_REGISTRATION_COUNT then
                        // CHECK CONDITION
                        let msg = "Number of reservations exceeded the limit."
                        HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INSUFFICIENT_REGISTRATION_RESOURCES, msg )
                        raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INSUFFICIENT_REGISTRATION_RESOURCES, msg )

                    struct(
                        {
                            oldVal with
                                m_Holder = Some itNexuses.[0];
                                m_PRGeneration = oldVal.m_PRGeneration + 1u;
                                m_Registrations = nextRegist2;
                        },
                        struct ( ScsiCmdStatCd.GOOD, "" )
                    )
            )

        // Request saving PR info to file.
        //( m_SavePRReqQueue :> ITargetBlock< int > ).Post( if moveParam.APTPL then 1 else 0 ) |> ignore
        m_SavePRReqQueue.Enqueue moveParam.APTPL

        if rStatus = ScsiCmdStatCd.RESERVATION_CONFLICT then
            HLogger.Trace( LogID.W_RESERVATION_CONFLICT, fun g -> g.Gen2( loginfo, "PERSISTENT RESERVE OUT(REGISTER AND MOVE service action)", rMessage ) )
            ScsiCmdStatCd.RESERVATION_CONFLICT
        else
            ScsiCmdStatCd.GOOD

    // ========================================================================
    // Private member definition


    /// <summary>
    /// REGISTER service action of PERSISTENT RESERVE OUT command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="prType">
    /// TYPE field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// Parameter length field value of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="saIgnore">
    /// If false, service action is REGISTER. Otherwise, service action is REGISTER AND IGNORE EXISTING KEY.
    /// </param>
    member private _.Register_sub ( source : CommandSourceInfo ) ( itt : ITT_T ) ( prType : PR_TYPE ) ( paramLen : uint32 ) ( param : PooledBuffer ) ( saIgnore : bool ) : ScsiCmdStatCd =
        let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( itt ), ValueSome( m_LU.LUN ) )
        let basicParam = PRManager.paramDataToBasicParameterList source m_ObjID m_LU.LUN itt paramLen param
        let serviceActionName = if saIgnore then "REGISTER AND IGNORE EXISTING KEY" else "REGISTER"

        let struct( rStatus, rMessage, uaList ) =
            m_Locker.Update ( fun oldVal ->
                // Check source I_T Nexus is registered or not.
                let srcRegved, srcResvKey = oldVal.m_Registrations.TryGetValue source.I_TNexus

                // Get all target name and TPGT that can access this LU.
                let allTargetPort =
                    [|
                        for i in m_StatusMaster.GetTargetFromLUN m_LU.LUN ->
                            ( i.TargetName, i.TargetPortalGroupTag )
                    |]

                // Get I_T Nexus accessible to this LU.
                let allITNexus = m_StatusMaster.GetITNexusFromLUN m_LU.LUN

                let vSourceITNexus =
                    if not basicParam.ALL_TG_PT then
                        // If ALL_TG_PT is false, source I_T Nexus is only real source I_T Nexus.
                        [| source.I_TNexus |]
                    else
                        // If ALL_TG_PT is true, The PERSISTENT RESERVE OUT command is treated as if it were received from all target ports.
                        [|
                            for tn, tgpt in allTargetPort ->
                                new ITNexus( source.I_TNexus.InitiatorName, source.I_TNexus.ISID, tn, tgpt )
                        |]

                let vProcInst =
                    [|
                        // next : 0=Nothig to do, 1=Register, 2=Delete, 3=Update
                        for sourIT in vSourceITNexus do
                            if not srcRegved then  // source I_T Nexus is not registered.
                                if saIgnore || basicParam.ReservationKey = resvkey_me.zero then
                                    if basicParam.ServiceActionReservationKey = resvkey_me.zero then
                                        // Nothing to do( return GOOD status ).
                                        let msg = "Command was reseived from unregistered I_T Nexus, and RESERVATION KEY and SERVICE ACTION RESERVATION KEY is both 0"
                                        HLogger.Trace( LogID.I_REQUEST_IGNORED, fun g ->
                                            g.Gen2( loginfo, sprintf "PERSISTENT RESERVE OUT(%s service action)" serviceActionName, msg )
                                        )
                                        struct (
                                            ScsiCmdStatCd.GOOD,
                                            "",
                                            0,  // Nothing to do
                                            Array.empty
                                        )
                                    elif not basicParam.SPEC_I_PT then
                                        // Register source I_T Nexus only
                                        struct (
                                            ScsiCmdStatCd.GOOD,
                                            "",
                                            1,  // register
                                            [| sourIT |]
                                        )
                                    else
                                        if saIgnore then
                                            // If service action is REGISTER AND IGNORE EXISTING KEY and SPEC_I_PT equals 1, return CHECK CONDITION.
                                            let msg =
                                                sprintf
                                                    "Invalid parameter value. SPEC_I_PT=1. (RESERVATION KEY=%d, SERVICE ACTION RESERVATION KEY=%d)"
                                                    ( resvkey_me.toPrim basicParam.ReservationKey )
                                                    ( resvkey_me.toPrim basicParam.ServiceActionReservationKey )
                                            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                                            raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                                        else
                                            struct (
                                                ScsiCmdStatCd.GOOD,
                                                "",
                                                1,  // register
                                                [|
                                                    // Register source I_T Nexus.
                                                    yield sourIT;

                                                    // Register I_T Nexus specified in TransportID.
                                                    for ( iname, isid_o ) in basicParam.TransportID do
                                                        if isid_o.IsNone then
                                                            // TransportID is only initiator name.
                                                            for j in allITNexus do
                                                                if String.Equals( j.InitiatorName, iname, StringComparison.Ordinal ) then
                                                                    yield new ITNexus( iname, j.ISID, sourIT.TargetName, sourIT.TPGT )
                                                        else
                                                            // TransportID is initiator name + ISID.
                                                            yield new ITNexus( iname, isid_o.Value, sourIT.TargetName, sourIT.TPGT );
                                                |]
                                            )
                                else
                                    // return RESERVATION CONFLICT status
                                    let msg = sprintf "Request was received from unregistered I_T Nexus, but RESERVATION KEY(%s) is not 0" ( resvkey_me.toString basicParam.ReservationKey )
                                    struct (
                                        ScsiCmdStatCd.RESERVATION_CONFLICT,
                                        msg,
                                        0,  // Nothing to do
                                        Array.empty
                                    )

                            // source I_T Nexus is already registered.
                            elif ( not saIgnore ) && ( srcResvKey <> basicParam.ReservationKey ) then
                                // return RESERVATION CONFLICT status
                                let msg =
                                    sprintf
                                        "Requested RESERVATION KEY(%s) is unmatch. Registered value is %s"
                                        ( resvkey_me.toString basicParam.ReservationKey )
                                        ( resvkey_me.toString srcResvKey )
                                struct (
                                    ScsiCmdStatCd.RESERVATION_CONFLICT,
                                    msg,
                                    0,  // Nothing to do
                                    Array.empty
                                )

                            elif basicParam.ServiceActionReservationKey = resvkey_me.zero then
                                if not basicParam.SPEC_I_PT then
                                    // Delete source I_T Nexus
                                    struct (
                                        ScsiCmdStatCd.GOOD,
                                        "",
                                        2,  // Delete
                                        [| sourIT |]
                                    )
                                else
                                    // return CHECK CONDITION status
                                    let msg =
                                        sprintf
                                            "Invalid parameter value. SPEC_I_PT=1. (RESERVATION KEY=%d, SERVICE ACTION RESERVATION KEY=%d)"
                                            ( resvkey_me.toPrim basicParam.ReservationKey )
                                            ( resvkey_me.toPrim basicParam.ServiceActionReservationKey )
                                    HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                                    raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                            elif not basicParam.SPEC_I_PT then
                                // Update source I_T Nexus
                                struct (
                                    ScsiCmdStatCd.GOOD,
                                    "",
                                    3,  // Update
                                    [| sourIT |]
                                )
                            else
                                // return CHECK CONDITION status
                                let msg =
                                    sprintf
                                        "Invalid parameter value. SPEC_I_PT=1. (RESERVATION KEY=%d, SERVICE ACTION RESERVATION KEY=%d)"
                                        ( resvkey_me.toPrim basicParam.ReservationKey )
                                        ( resvkey_me.toPrim basicParam.ServiceActionReservationKey )
                                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                                raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                    |]

                // If the status is mixed with something other than GOOD ( equals RESERVATION_CONFRICT ), do nothing and return the RESERVATION_CONFRICT.
                let rc = vProcInst |> Array.tryFind ( fun struct ( stat, _, _, _ ) -> stat <> ScsiCmdStatCd.GOOD )
                if rc.IsSome then
                    let struct( stat, msg, _, _ ) = rc.Value
                    struct( oldVal, struct( stat, msg, [] ) )
                else
                    // At this point, there shouldn't be a mixture of multiple values of next, but if it were, return a CHECK CONDITION.
                    let vNext =
                        vProcInst
                        |> Array.map ( fun struct ( _, _, n, _ ) -> n )
                        |> Array.distinct

                    if vNext.Length > 1 then
                        // return CHECK CONDITION status
                        let msg = "Unexpected reservation status."
                        HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                        raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )

                    if vNext.[0] = 1 then
                        // Register I_T Nexuses

                        // Remove duplicates
                        let vITN =
                            [|
                                for ( _, _, _, itr ) in vProcInst do
                                    yield! itr
                            |]
                            |> Array.distinct

                        // Check to see if there are any registered I_T Nexus.
                        vITN
                        |> Array.iter ( fun itr ->
                            if oldVal.m_Registrations.ContainsKey itr then
                                let msg = sprintf "I_T Nexus(%s) is already registered." itr.I_TNexusStr
                                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                                raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                        )

                        // Register specified I_T Nexus
                        let nextRegist : ImmutableDictionary< ITNexus, RESVKEY_T > =
                            vITN
                            |> Array.fold ( fun ( s ) itr ->
                                s.SetItem( itr, basicParam.ServiceActionReservationKey )
                            ) oldVal.m_Registrations

                        if nextRegist.Count > Constants.PRDATA_MAX_REGISTRATION_COUNT then
                            // CHECK CONDITION
                            let msg = "Number of reservations exceeded the limit."
                            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INSUFFICIENT_REGISTRATION_RESOURCES, msg )
                            raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INSUFFICIENT_REGISTRATION_RESOURCES, msg )

                        struct(
                            {
                                oldVal with
                                    m_PRGeneration = oldVal.m_PRGeneration + 1u;
                                    m_Registrations = nextRegist
                            },
                            struct( ScsiCmdStatCd.GOOD, "", [] )
                        )

                    elif vNext.[0] = 2 then
                        // Delete I_T Nexuses
                        let struct ( nextRegist, wUAList ) =
                            vProcInst
                            |> Array.fold ( fun s struct ( _, _, _, vITN ) ->
                                vITN
                                |> Array.fold ( fun struct( wr, wl ) itNexus ->
                                    let ws : ImmutableDictionary< ITNexus, RESVKEY_T > = wr
                                    let next = ws.Remove( itNexus )
                                    let nextUAL =
                                        if PR_TYPE.isRegistrantsOnly oldVal.m_Type && ( ITNexus.Equals( itNexus, oldVal.m_Holder.Value ) ) then
                                            // If deleted I_T Nexus is reservation holder that is registrants only type,
                                            // unit attention must be established for all registered initiators.
                                            oldVal.m_Registrations
                                            |> Seq.filter( fun itr -> not( ITNexus.Equals( itr.Key, source.I_TNexus ) ) )
                                            |> Seq.map ( _.Key >> ITNexus.getInitiatorPortName )
                                            |> Seq.distinct
                                            |> Seq.sort
                                            |> Seq.toList
                                        else
                                            // Nothing to do
                                            wl
                                    struct ( next, nextUAL )
                                ) s
                            ) struct ( oldVal.m_Registrations, [] )

                        // All of registration is deleted, or reservation holder is unregistered,
                        // Reservation information must be cleared.
                        let reservationIsDeleted =
                            nextRegist.Count = 0 ||
                            (
                                ( PR_TYPE.isRegistrantsOnly oldVal.m_Type || PR_TYPE.isOtherReservation oldVal.m_Type ) &&
                                not ( nextRegist.ContainsKey( oldVal.m_Holder.Value ) )
                            )

                        if nextRegist.Count > Constants.PRDATA_MAX_REGISTRATION_COUNT then
                            // CHECK CONDITION ( Originally, this route is not executed )
                            let msg = "Number of reservations exceeded the limit."
                            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INSUFFICIENT_REGISTRATION_RESOURCES, msg )
                            raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INSUFFICIENT_REGISTRATION_RESOURCES, msg )

                        struct (
                            {
                                m_Type = if reservationIsDeleted then PR_TYPE.NO_RESERVATION else oldVal.m_Type;
                                m_Holder = if reservationIsDeleted then None else oldVal.m_Holder;
                                m_PRGeneration = oldVal.m_PRGeneration + 1u;
                                m_Registrations = nextRegist;
                            },
                            struct ( ScsiCmdStatCd.GOOD, "", wUAList )
                        )

                    elif vNext.[0] = 3 then
                        // Update I_T Nexuses
                        let nextRegist =
                            vProcInst
                            |> Array.fold ( fun s struct ( _, _, _, vITN ) ->
                                vITN |> Array.fold ( fun ( s2 : ImmutableDictionary< ITNexus, RESVKEY_T > ) itNexus ->
                                    s2.SetItem( itNexus, basicParam.ServiceActionReservationKey )
                                ) s
                            ) oldVal.m_Registrations

                        if nextRegist.Count > Constants.PRDATA_MAX_REGISTRATION_COUNT then
                            // CHECK CONDITION ( Originally, this route is not executed )
                            let msg = "Number of reservations exceeded the limit."
                            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INSUFFICIENT_REGISTRATION_RESOURCES, msg )
                            raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INSUFFICIENT_REGISTRATION_RESOURCES, msg )

                        struct (
                            {
                                oldVal with
                                    m_PRGeneration = oldVal.m_PRGeneration + 1u;
                                    m_Registrations = nextRegist;
                            },
                            struct ( ScsiCmdStatCd.GOOD, "", [] )
                        )
                    else
                        struct( oldVal, struct ( ScsiCmdStatCd.GOOD, "", [] ) )
            )

        // Request saving PR info to file.
        //( m_SavePRReqQueue :> ITargetBlock< int > ).Post( if basicParam.APTPL then 1 else 0 ) |> ignore
        m_SavePRReqQueue.Enqueue basicParam.APTPL

        if rStatus = ScsiCmdStatCd.RESERVATION_CONFLICT then
            HLogger.Trace( LogID.W_RESERVATION_CONFLICT, fun g ->
                g.Gen2( loginfo, sprintf "PERSISTENT RESERVE OUT(%s service action)" serviceActionName, rMessage )
            )
            ScsiCmdStatCd.RESERVATION_CONFLICT
        else
            // establish unit attention
            let ex =
                new SCSIACAException (
                    source,
                    true,
                    SenseKeyCd.UNIT_ATTENTION,
                    ASCCd.RESERVATIONS_RELEASED,
                    sprintf "Persistent reservation released. I_T Nexus=%s" source.I_TNexus.I_TNexusStr
                )
            uaList |> List.iter ( fun itr -> m_LU.EstablishUnitAttention itr ex )
            ScsiCmdStatCd.GOOD

    /// <summary>
    /// PREEMPT AND ABORT service action of PERSISTENT RESERVE OUT command.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="prType">
    /// TYPE field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// Parameter list bytes count value PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <returns>
    ///  Pair of Preempted I_T Nexuses, reservation type and SERVICE ACTION RESERVATION KEY value.
    /// </returns>
    member _.Preempt_sub ( source : CommandSourceInfo ) ( itt : ITT_T ) ( prType : PR_TYPE ) ( paramLen : uint32 ) ( param : PooledBuffer ) :
            struct ( ScsiCmdStatCd * ITNexus[] * PR_TYPE * RESVKEY_T ) =
        let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( itt ), ValueSome( m_LU.LUN ) )
        let basicParam = PRManager.paramDataToBasicParameterList source m_ObjID m_LU.LUN itt paramLen param

        let struct( rStatus, rMessage, rNexus, currentPRType, saRESVKEY, ( uaList : string[] ) ) =
            m_Locker.Update ( fun oldVal ->
                // Check source I_T Nexus is registered or not.
                let srcRegved, srcResvKey  = oldVal.m_Registrations.TryGetValue source.I_TNexus

                let struct( next, rstat, rmessage ) =
                    if not srcRegved then
                        // If RELEASE service action was received from unregistered I_T Nexus, return CHECK CONDITION status
                        let msg = "Command was received from unregistered I_T Nexus."
                        struct( 0, ScsiCmdStatCd.RESERVATION_CONFLICT, msg )

                    elif srcResvKey <> basicParam.ReservationKey then
                        // Reservation key unmatched
                        struct( 0, ScsiCmdStatCd.RESERVATION_CONFLICT, "Reservation key unmatched." )

                    elif oldVal.m_Type = PR_TYPE.NO_RESERVATION then
                        // Reservation missing
                        struct( 1, ScsiCmdStatCd.GOOD, "" )

                    elif PR_TYPE.isAllRegistrants oldVal.m_Type then
                        if basicParam.ServiceActionReservationKey = resvkey_me.zero then
                            struct( 3, ScsiCmdStatCd.GOOD, "" )
                        else
                            // In this case, it will be treated the same as if there was no reservation.
                            struct( 2, ScsiCmdStatCd.GOOD, "" )

                    elif basicParam.ServiceActionReservationKey = resvkey_me.zero then
                        // return CHECK CONDITION status
                        let msg = "PERSISTENT RESERVATION is exist, but SERVICE ACTION RESERVATION KEY is 0."
                        HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, msg )
                        raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, msg )
                        struct( 0, ScsiCmdStatCd.CHECK_CONDITION, "" )

                    else
                        let _, holderKey = oldVal.m_Registrations.TryGetValue oldVal.m_Holder.Value
                        if basicParam.ServiceActionReservationKey = holderKey then
                            struct( 4, ScsiCmdStatCd.GOOD, "" )
                        else
                            // In this case, it will be treated the same as if there was no reservation.
                            struct( 1, ScsiCmdStatCd.GOOD, "" )

                match next with
                | 1
                | 2 ->
                    // SPC-3 5.6.10.4.4
                    // SERVICE ACTION RESERVATION KEYで指定された全ての登録を削除する。
                    // PERSISTENT RESERVE OUT コマンドを受信した I_T ネクサスを除く、
                    // 登録が削除された全ての I_T ネクサスに関するイニシエータポートに対して、
                    // REGISTRATION PREEMPTED の追加センスコードを設定したユニット警告状態を確立する。
                    // SERVICE ACTION RESERVATION KEY フィールドに設定された値がいかなる予約キーとも一致しなかった場合は、
                    // デバイスサーバは RESERVATION CONFLICT ステータスを返さなければならない。

                    // Search delete target entry
                    let delentry =
                        oldVal.m_Registrations
                        |> Seq.filter ( _.Value >> (=) basicParam.ServiceActionReservationKey )
                        |> Seq.map _.Key
                        |> Seq.sortWith ITNexus.Compare
                        |> Seq.toArray
                    
                    if delentry.Length <= 0 then
                        let msg = "Missing reservation key that match SERVICE ACTION RESERVATION KEY."
                        struct(
                            oldVal,
                            struct ( ScsiCmdStatCd.RESERVATION_CONFLICT, msg, Array.empty, oldVal.m_Type, basicParam.ServiceActionReservationKey, Array.empty )
                        )
                    else
                        // Delete entry specified by SERVICE ACTION RESERVATION KEY
                        let nextRegist =
                            delentry
                            |> Seq.fold ( fun ( s : ImmutableDictionary< ITNexus, RESVKEY_T > ) itr ->
                                s.Remove( itr )
                            ) oldVal.m_Registrations

                        // Establish Unit Attention
                        let uaList =
                            delentry
                            |> Array.filter ( fun itr -> ITNexus.Equals( itr, source.I_TNexus ) |> not )
                            |> Array.map ITNexus.getInitiatorPortName
                            |> Array.distinct

                        // All of registration is deleted, or reservation holder is unregistered,
                        // Reservation information must be cleared.
                        let reservationIsDeleted =
                            nextRegist.Count = 0 ||
                            (
                                ( PR_TYPE.isRegistrantsOnly oldVal.m_Type || PR_TYPE.isOtherReservation oldVal.m_Type ) &&
                                ( not ( nextRegist.ContainsKey oldVal.m_Holder.Value ) )
                            )

                        struct (
                            {
                                m_Type = if reservationIsDeleted then PR_TYPE.NO_RESERVATION else oldVal.m_Type;
                                m_Holder = if reservationIsDeleted then None else oldVal.m_Holder;
                                m_PRGeneration = oldVal.m_PRGeneration + 1u;
                                m_Registrations = nextRegist;
                            },
                            struct ( ScsiCmdStatCd.GOOD, "", delentry, oldVal.m_Type, basicParam.ServiceActionReservationKey, uaList )
                        )

                | 3 ->
                    // 5.6.10.4.3
                    // PERSISTENT RESERVE OUT コマンドで使用された I_T ネクサスを除く、全ての内容が削除されなければならない。
                    // 新しい TYPE と SCOPE を使用し、剥奪を試みる I_T ネクサスのために永続予約を確立する。
                    // 永続予約もしくは登録を喪失した全ての I_T ネクサスに関係するイニシエータポートに対して、
                    // 追加センスコード REGISTRATIONS PREEMPTED を設定したユニット警告状態を確立する。

                    // Prepare UA target initiator port name
                    let delentry =
                        oldVal.m_Registrations
                        |> Seq.map _.Key
                        |> Seq.filter ( fun itr -> ITNexus.Equals( itr, source.I_TNexus ) |> not )
                        |> Seq.sortWith ITNexus.Compare
                        |> Seq.toArray

                    let uaList =
                        delentry
                        |> Array.map ITNexus.getInitiatorPortName
                        |> Array.distinct

                    struct (
                        {
                            m_Type = prType;
                            m_Holder = Some source.I_TNexus;
                            m_PRGeneration = oldVal.m_PRGeneration + 1u;
                            m_Registrations = ImmutableDictionary< ITNexus, RESVKEY_T >.Empty.Add( source.I_TNexus, basicParam.ReservationKey );
                        },
                        struct ( ScsiCmdStatCd.GOOD, "", delentry, oldVal.m_Type, basicParam.ServiceActionReservationKey, uaList )
                    )

                | 4 ->
                    // 5.6.10.4.3
                    // PERSISTENT RESERVE OUT コマンドで使用されている I_T ネクサスを除く、
                    // SERVICE ACTION RESERVATION KEY フィールドにより識別される全ての I_T ネクサスについて登録を削除する。
                    // SCOPE と TYPE フィールドの内容を使用し、剥奪を試みる I_T ネクサスのために永続予約を確立する。
                    // 永続予約もしくは登録を喪失した全ての I_T ネクサスに関係するイニシエータポートに対
                    // して、追加センスコード REGISTRATIONS PREEMPTED を設定したユニット警告状態を確立する。

                    // Search delete target entry
                    let delentry =
                        oldVal.m_Registrations
                        |> Seq.filter ( fun itr -> ITNexus.Equals( itr.Key, source.I_TNexus ) |> not )
                        |> Seq.filter ( _.Value >> (=) basicParam.ServiceActionReservationKey )
                        |> Seq.map _.Key
                        |> Seq.sortWith ITNexus.Compare
                        |> Seq.toArray

                    // Delete entry ( the entry that holds reservation is deleted in this point )
                    let nextRegist =
                        delentry
                        |> Seq.fold ( fun ( s : ImmutableDictionary< ITNexus, RESVKEY_T > ) itr ->
                            s.Remove( itr )
                        ) oldVal.m_Registrations

                    // Establish Unit Attention
                    let uaList =
                        delentry
                        |> Array.map ITNexus.getInitiatorPortName
                        |> Array.distinct

                    struct (
                        {
                            m_Type =
                                if nextRegist.Count <= 0 then
                                    PR_TYPE.NO_RESERVATION
                                else
                                    prType;
                            m_Holder =
                                if nextRegist.Count <= 0 || prType = PR_TYPE.NO_RESERVATION || PR_TYPE.isAllRegistrants prType then
                                    None
                                else
                                    Some source.I_TNexus;
                            m_PRGeneration = oldVal.m_PRGeneration + 1u;
                            m_Registrations = nextRegist;
                        },
                        struct ( ScsiCmdStatCd.GOOD, "", delentry, oldVal.m_Type, basicParam.ServiceActionReservationKey, uaList )
                    )

                | _ ->
                    struct( oldVal, struct ( rstat, rmessage, Array.empty, oldVal.m_Type, basicParam.ServiceActionReservationKey, Array.empty ) )
            )

        // Request saving PR info to file.
        //( m_SavePRReqQueue :> ITargetBlock< int > ).Post( if basicParam.APTPL then 1 else 0 ) |> ignore
        m_SavePRReqQueue.Enqueue basicParam.APTPL

        if rStatus = ScsiCmdStatCd.RESERVATION_CONFLICT then
            HLogger.Trace( LogID.W_RESERVATION_CONFLICT, fun g -> g.Gen2( loginfo, "PERSISTENT RESERVE OUT(PREEMPT service action)", rMessage ) )
            struct( ScsiCmdStatCd.RESERVATION_CONFLICT, rNexus, currentPRType, saRESVKEY )
        else
            // establish unit attention
            let uaExp =
                new SCSIACAException (
                    source,
                    true,
                    SenseKeyCd.UNIT_ATTENTION,
                    ASCCd.RESERVATIONS_RELEASED,
                    sprintf "Persistent reservation released. I_T Nexus=%s" source.I_TNexus.I_TNexusStr
                )
            uaList
            |> Array.iter ( fun itr -> m_LU.EstablishUnitAttention itr uaExp )
            struct( ScsiCmdStatCd.GOOD, rNexus, currentPRType, saRESVKEY )

    /// <summary>
    /// Load persistent reservation information from saved file.
    /// </summary>
    /// <param name="objID">
    /// Object ID of PRManager instance.
    /// </param>
    /// <param name="lun">
    /// LUN of LU that holds PRManager instance.
    /// </param>
    /// <param name="fname">
    /// Saved file name.
    /// </param>
    /// <remarks>
    /// If load failed or fname is empty, it return empty object.
    /// </remarks>
    static member private LoadPRFile ( objID : OBJIDX_T ) ( lun : LUN_T ) ( fname : string ) : PRInfoRec =
        let loginfo = struct( objID, ValueNone, ValueNone, ValueSome lun )
        try
            let p = PersistentReservation.ReaderWriter.LoadFile fname
            let r =
                p.Registration
                |> Seq.fold
                    ( fun s itr ->
                        let itn = new ITNexus( itr.ITNexus.InitiatorName, itr.ITNexus.ISID, itr.ITNexus.TargetName, itr.ITNexus.TPGT )
                        if s.m_Holder.IsSome && itr.Holder then
                            let msg = "Reservation holder specified multiple time."
                            HLogger.Trace( LogID.I_PR_FILE_VALIDATE_ERROR, fun g -> g.Gen2( loginfo, fname, msg ) )
                            raise <| Exception( msg )

                        {
                            s with
                                m_Holder = if itr.Holder then Some itn else s.m_Holder
                                m_Registrations = s.m_Registrations.Add( itn, itr.ReservationKey )
                        }
                    )
                    {
                        m_Type = p.Type;
                        m_Holder = None;
                        m_PRGeneration = 0u;
                        m_Registrations = ImmutableDictionary.Empty;
                    }

            if ( r.m_Type <> PR_TYPE.NO_RESERVATION ) && r.m_Registrations.IsEmpty then
                let msg = sprintf "Reservation Type is %s, but registration is missing." ( PR_TYPE.toStringName r.m_Type )
                HLogger.Trace( LogID.I_PR_FILE_VALIDATE_ERROR, fun g -> g.Gen2( loginfo, fname, msg ) )
                raise <| Exception( msg )

            if ( r.m_Type = PR_TYPE.NO_RESERVATION || PR_TYPE.isAllRegistrants r.m_Type ) && r.m_Holder.IsSome then
                let msg = sprintf "Reservation holder is specified, but Type is %s" ( PR_TYPE.toStringName r.m_Type )
                HLogger.Trace( LogID.I_PR_FILE_VALIDATE_ERROR, fun g -> g.Gen2( loginfo, fname, msg ) )
                raise <| Exception( msg )

            if ( r.m_Type = PR_TYPE.WRITE_EXCLUSIVE || r.m_Type = EXCLUSIVE_ACCESS || PR_TYPE.isRegistrantsOnly r.m_Type ) && r.m_Holder.IsNone then
                let msg = sprintf "Reservation Type is %s, but reservation holder is missing." ( PR_TYPE.toStringName r.m_Type )
                HLogger.Trace( LogID.I_PR_FILE_VALIDATE_ERROR, fun g -> g.Gen2( loginfo, fname, msg ) )
                raise <| Exception( msg )
            r

        with
        | _ as x ->
            HLogger.Trace( LogID.I_FAILED_LOAD_PR_FILE, fun g -> g.Gen2( loginfo, fname, x.Message ) )
            {
                m_Type = PR_TYPE.NO_RESERVATION;
                m_Holder = None;
                m_PRGeneration = 0u;
                m_Registrations = ImmutableDictionary< ITNexus, RESVKEY_T >.Empty;
            }

    /// <summary>
    /// Save persistent reservation information to specified file.
    /// </summary>
    /// <param name="objID">
    /// Object ID of PRManager instance.
    /// </param>
    /// <param name="source">
    /// source info of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="itt">
    /// Initiator task tag value of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="lun">
    /// LUN of LU that holds PRManager instance.
    /// </param>
    /// <param name="fname">
    /// File name to save.
    /// </param>
    /// <param name="pr">
    /// Persistent reservation info to save specified file.
    /// </param>
    /// <param name="aptpl">
    /// APTPL value in parameter list of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <remarks>
    /// If save failed or fname is empty, it will give up to save PR info to file.
    /// </remarks>
    static member private SavePRFile
            ( objID : OBJIDX_T )
            ( lun : LUN_T )
            ( fname : string )
            ( pr : PRInfoRec )
            ( aptpl : bool )
            : Task<unit> =

        let loginfo = struct ( objID, ValueNone, ValueNone, ValueSome lun )
        let ws : PersistentReservation.T_PRInfo = 
            {
                Type = pr.m_Type;
                Registration =
                    pr.m_Registrations
                    |> Seq.map ( fun itr -> itr.Key, itr.Value )
                    |> Seq.map ( fun ( nexus, resvk ) ->
                        {
                            ITNexus = {
                                InitiatorName = nexus.InitiatorName;
                                ISID = nexus.ISID;
                                TargetName = nexus.TargetName;
                                TPGT = nexus.TPGT;
                            };
                            ReservationKey = resvk;
                            Holder = ( pr.m_Holder.IsSome && nexus = pr.m_Holder.Value );
                        } : PersistentReservation.T_Registration
                       )
                    |> Seq.toList
            }

        task {
            let mutable cnt = if fname.Length > 0 then 0 else 20
            while ( cnt < 10 ) do
                try
                    if aptpl then
                        PersistentReservation.ReaderWriter.WriteFile fname ws
                        HLogger.Trace( LogID.I_SUCCEED_TO_SAVE_PR_FILE, fun g -> g.Gen0 loginfo )
                    else
                        File.Delete fname
                        HLogger.Trace( LogID.I_SUCCEED_TO_DELETE_PR_FILE, fun g -> g.Gen0 loginfo )
                    cnt <- 20
                with
                | _ as x ->
                    if cnt < 9 then
                        do! Task.Delay 10
                    else
                        HLogger.Trace( LogID.W_FAILED_SAVE_PR_FILE, fun g -> g.Gen2( loginfo, fname, x.Message ) )
                cnt <- cnt + 1
        }


    /// <summary>
    /// Resognize parameter list bytes, convert to the BasicParameterList record.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="objID">
    /// Object identifier of the PRManager instance.
    /// </param>
    /// <param name="lun">
    /// LUN of the LU which receive the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// PARAMETER LIST LENGTH field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <returns>
    /// Recognized BasicParameterList record.
    /// </returns>
    /// <exception cref="SCSIACAException">
    /// If one or more errors exists in parameter list, it throws SCSIACAException.
    /// </exception>
    static member private paramDataToBasicParameterList
            ( source : CommandSourceInfo )
            ( objID : OBJIDX_T )
            ( lun : LUN_T )
            ( itt : ITT_T )
            ( paramLen : uint32 )
            ( param : PooledBuffer ) : BasicParameterList =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( itt ), ValueSome( lun ) )

        if paramLen <  24u || param.Count < (int)paramLen || (int)paramLen < 0 then
            let msg = "Parameter length in PERSISTENT RESERVE OUT is too short."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, msg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 5us },
                msg
            )

        // get SPEC_I_PT value
        let specI_PT = Functions.CheckBitflag param.[20] 0x08uy
        if not specI_PT && paramLen <> 24u then
            let msg = "SPEC_I_PT bit = 0, but parameter length in PERSISTENT RESERVE OUT is not 24 bytes."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, msg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 5us },
                msg
            )

        let reservationKey = resvkey_me.fromPrim( Functions.NetworkBytesToUInt64_InPooledBuffer param 0 )
        let serviceActionReservationKey = resvkey_me.fromPrim( Functions.NetworkBytesToUInt64_InPooledBuffer param 8 )
        let allTG_PT = Functions.CheckBitflag param.[20] 0x04uy
        let aptpl = Functions.CheckBitflag param.[20] 0x01uy

        let transportID =
            if specI_PT && paramLen > 24u then    // If exist addisional params
                if paramLen < 28u then
                    let msg = "Additional parameter data length in PERSISTENT RESERVE OUT is too short."
                    HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, msg )
                    raise <| SCSIACAException (
                        source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                        { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 5us },
                        msg
                    )

                let transportParameterDataLength = Functions.NetworkBytesToUInt32_InPooledBuffer param 24
                if transportParameterDataLength + 28u > paramLen then
                    let msg = "Invalid TRANSPORTID PARAMETER DATA LENGTH in PERSISTENT RESERVE OUT."
                    HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, msg )
                    raise <| SCSIACAException (
                        source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                        { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = 24us },
                        msg
                    )

                PRManager.RecognizeTransportIDsInParameterData source objID lun itt ( transportParameterDataLength + 28u ) param 28
            else
                Array.empty
            
        {
            ReservationKey = reservationKey;
            ServiceActionReservationKey = serviceActionReservationKey;
            SPEC_I_PT = specI_PT;
            ALL_TG_PT = allTG_PT;
            APTPL = aptpl;
            TransportID = transportID;
        }

    /// <summary>
    /// Resognize parameter list bytes, convert to the MoveParameterList record.
    /// </summary>
    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="objID">
    /// Object identifier of the PRManager instance.
    /// </param>
    /// <param name="lun">
    /// LUN of the LU which receive the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// PARAMETER LIST LENGTH field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <returns>
    /// Recognized BasicParameterList record.
    /// </returns>
    /// <exception cref="SCSIACAException">
    /// If one or more errors exists in parameter list, it throws SCSIACAException.
    /// </exception>
    static member private paramDataToMoveParameterList
            ( source : CommandSourceInfo )
            ( objID : OBJIDX_T )
            ( lun : LUN_T )
            ( itt : ITT_T )
            ( paramLen : uint32 )
            ( param : PooledBuffer ) : MoveParameterList =
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( itt ), ValueSome( lun ) )

        if paramLen < 48u || param.Count < (int)paramLen || (int)paramLen < 0 then
            let msg = "Parameter length in PERSISTENT RESERVE OUT is too short."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, msg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 5us },
                msg
            )

        let reservationKey = resvkey_me.fromPrim( Functions.NetworkBytesToUInt64_InPooledBuffer param 0 )
        let serviceActionReservationKey = resvkey_me.fromPrim( Functions.NetworkBytesToUInt64_InPooledBuffer param 8 )
        let unreg = Functions.CheckBitflag param.[17] 0x02uy
        let aptpl = Functions.CheckBitflag param.[17] 0x01uy
        let relativeTargetPortIdentifier = Functions.NetworkBytesToUInt16_InPooledBuffer param 18
        let transportIDLength = Functions.NetworkBytesToUInt32_InPooledBuffer param 20

        if transportIDLength + 24u <> paramLen then
            let msg = "Invalid TRANSPORTID PARAMETER DATA LENGTH in PERSISTENT RESERVE OUT."
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, msg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = 20us },
                msg
            )

        let transportID = PRManager.RecognizeTransportIDsInParameterData source objID lun itt ( transportIDLength + 24u ) param 24

        {
            ReservationKey = reservationKey;
            ServiceActionReservationKey = serviceActionReservationKey;
            UNREG = unreg;
            APTPL = aptpl;
            RelativeTargetPortIdentifier = relativeTargetPortIdentifier;
            TransportID = transportID.[0];
        }


    /// Regex object of iSCSI-name-values + \0 + etc.
    static member private m_IName_Match =
        new Regex( "^([\-\.\:a-z0-9]{1,223})\0.*$", RegexOptions.Compiled )

    ///Regex object for  iSCSI-name-values + ",i," + 12 digits hex-constant + \0 + etc.
    static member private m_IName_ISID_match =
        new Regex( "^([\-\.\:a-z0-9]{1,223}),i,0[xX]([0-9a-fA-F]{12})\0.*$", RegexOptions.Compiled )


    /// <param name="source">
    /// Command source information which the PERSISTENT RESERVE OUT command was reseived from.
    /// </param>
    /// <param name="objID">
    /// Object identifier of the PRManager instance.
    /// </param>
    /// <param name="lun">
    /// LUN of the LU which receive the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag of the PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="paramLen">
    /// PARAMETER LIST LENGTH field value in the CDB of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="param">
    /// Parameter list bytes array of PERSISTENT RESERVE OUT command.
    /// </param>
    /// <param name="startPos">
    /// First byte number of the TransportID on param.
    /// </param>
    /// <returns>
    /// Recognized TransportID array. TransportID is ( InitiatorName * ISID option ).
    /// If FORMAT CODE is 0, TransportID contents the initiator name only. 
    /// If FORMAT CODE is 1, TransportID contents the initiator name and ISID.
    /// </returns>
    /// <exception cref="SCSIACAException">
    /// If one or more errors exists in parameter list, it throws SCSIACAException.
    /// </exception>
    static member RecognizeTransportIDsInParameterData
            ( source : CommandSourceInfo )
            ( objID : OBJIDX_T )
            ( lun : LUN_T )
            ( itt : ITT_T )
            ( paramLen : uint32 )
            ( param : PooledBuffer )
            ( startPos : int ) : ( string * ISID_T option ) [] = 

        assert( ( int paramLen ) >= 0 )
        assert( paramLen >=  24u )
        assert( param.Count >= ( int paramLen ) )
        let loginfo = struct ( objID, ValueSome( source ), ValueSome( itt ), ValueSome( lun ) )

        let rec loop ( pos : int ) cont =
            if pos = ( int paramLen ) then
                cont []
            else
                let formatCode = param.[pos] >>> 6
                let protocolIdentifier = param.[pos] &&& 0x0Fuy

                if formatCode <> 0uy && formatCode <> 1uy then
                    let msg = sprintf "Invalid FORMAT CODE value(0x%02X) in PERSISTENT RESERVE OUT command parameter list." formatCode
                    HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, msg )
                    raise <| SCSIACAException (
                        source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                        { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = (uint16)pos },
                        msg
                    )

                if protocolIdentifier <> 5uy then
                    let msg = sprintf "Invalid PROTOCOL IDENTIFIER value(0x%02X) in PERSISTENT RESERVE OUT command parameter list." protocolIdentifier
                    HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, msg )
                    raise <| SCSIACAException (
                        source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                        { CommandData = false; BPV = true; BitPointer = 3uy; FieldPointer = (uint16)pos },
                        msg
                    )

                if pos + 24 > ( int paramLen ) then
                    let msg = "Invalid TransportID length in PERSISTENT RESERVE OUT command parameter list."
                    HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, msg )
                    raise <| SCSIACAException (
                        source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                        { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 5us },
                        msg
                    )

                let additionalLength = int( Functions.NetworkBytesToUInt16_InPooledBuffer param ( pos + 2 ) )
                if pos + 4 + additionalLength > ( int paramLen ) || additionalLength < 20 || additionalLength % 4 <> 0 then
                    let msg = sprintf "Invalid  ADDITIONAL LENGTH value(%d) in PERSISTENT RESERVE OUT command parameter list." additionalLength
                    HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, msg )
                    raise <| SCSIACAException (
                        source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                        { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = (uint16)pos + 2us },
                        msg
                    )

                if ( formatCode = 0uy && additionalLength > 224 ) || ( formatCode = 1uy && additionalLength > 244 ) then
                    let msg = sprintf "TransportID is too long. ADDITIONAL LENGTH value is %d in PERSISTENT RESERVE OUT command parameter list." additionalLength
                    HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, msg )
                    raise <| SCSIACAException (
                        source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                        { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = (uint16)pos + 2us },
                        msg
                    )

                let transportID = Encoding.UTF8.GetString( param.Array.[ pos + 4 .. pos + 3 + additionalLength ] )
                let iSCSIName, isid =
                    if formatCode = 0uy then
                        // TransportID contents iSCSI Name only
                        let m = PRManager.m_IName_Match.Match( transportID )
                        if not m.Success then
                            let msg = "Invalid TransportID format in PERSISTENT RESERVE OUT command parameter list."
                            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_VALUE_INVALID, msg )
                            raise <| SCSIACAException (
                                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_VALUE_INVALID,
                                { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = (uint16)pos + 2us },
                                msg
                            )
                        m.Groups.[1].Value, None
                    else
                        // TransportID contents iSCSI Name and ISID
                        let m = PRManager.m_IName_ISID_match.Match( transportID )
                        if not m.Success then
                            let msg = "Invalid TransportID format in PERSISTENT RESERVE OUT command parameter list."
                            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_VALUE_INVALID, msg )
                            raise <| SCSIACAException (
                                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_VALUE_INVALID,
                                { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = (uint16)pos + 2us },
                                msg
                            )
                        m.Groups.[1].Value, Some( isid_me.HexStringToISID m.Groups.[2].Value )
                loop ( pos + 4 + additionalLength ) ( fun arglist -> cont ( ( iSCSIName, isid ) :: arglist ) )

        loop startPos id
        |> List.toArray
