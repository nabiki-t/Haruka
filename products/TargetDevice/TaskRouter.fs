//=============================================================================
// Haruka Software Storage.
// TaskRouter.fs : Defines TaskRouter class
// TaskRouter class imprements the IProtocolService interface.

//=============================================================================
// Namespace declaration

namespace Haruka.TargetDevice

//=============================================================================
// Import declaration

open System
open System.Collections.Concurrent

open Haruka
open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

/// <summary>
///  TaskRouter class definition.
///  TaskRouter implements protocol service interface, it exists in boundary of SCSI and iSCSI layer.
///  One TaskRouter object is created when one session object is created.
///  Session object and TaskRouter object have one-to-one relationship.
///  TaskRouter object loads the LU library accessible at the target to which the session logs in.
/// </summary>
/// <param name="m_Status">
///  The interface of the StatusMaster object.
/// </param>
/// <param name="m_Session">
///  The interface of session object which this instance belongings to.
/// </param>
/// <param name="m_I_T_Nexus">
///  I_T Nexus information that is represented by session object referred by m_Session.
/// </param>
/// <param name="m_TSIH">
///  TSIH value of this session.
/// </param>
/// <param name="m_SessionParameter">
///  Effective session parameter values.
/// </param>
/// <param name="m_Killer">
///  Killer object of this object.
/// </param>
type TaskRouter
    (
        m_Status : IStatus,
        m_Session : ISession,
        m_I_T_Nexus : ITNexus,
        m_TSIH : TSIH_T,
        m_SessionParameter : IscsiNegoParamSW,
        m_Killer : IKiller
    ) as this =

    /// Object identifier
    let m_ObjID = objidx_me.NewID()

    /// Interface of Logical unit component, indexed by LUN
    let m_LU =
        [|
            yield lun_me.zero;
            for i in m_SessionParameter.TargetConf.LUN -> i
        |]
        |> Array.map ( fun itr ->
            match m_Status.GetLU itr with
            | ValueSome( lu ) ->
                HLogger.Trace( LogID.I_ASSIGN_LU_TO_SESSION, fun g ->
                    g.Gen0( m_ObjID, ValueNone, ValueNone, ValueSome m_TSIH, ValueNone, ValueSome itr )
                )
                ( itr, lu )
            | ValueNone ->
                HLogger.Trace( LogID.E_MISSING_LU, fun g ->
                    g.Gen0( m_ObjID, ValueNone, ValueNone, ValueSome m_TSIH, ValueNone, ValueSome itr )
                )
                let msg = "This LU is not assigned to session."
                raise <| SessionRecoveryException( msg, m_TSIH )
        )
        |> Functions.ToFrozenDictionary

    let m_CommandSourcePool =
        ConcurrentDictionary< uint64, CommandSourceInfo >()

    do
        m_Killer.Add this
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g ->
            let loginfo = struct ( m_ObjID, ValueNone, ValueNone, ValueSome( m_TSIH ), ValueNone, ValueNone )
            g.Gen2( loginfo, "TaskRouter", "" )
        )

    //=========================================================================
    // Interface method

    interface IProtocolService with

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override _.Terminate() : unit = ()  // Nothig to do

        // ------------------------------------------------------------------------
        // ABORT TASK task management function request.
        // It aborts the task specified by referencedTaskTag.
        override this.AbortTask ( iScsiTask : IIscsiTask ) ( lun : LUN_T ) ( referencedTaskTag : ITT_T ) : unit =
            let msg = "TaskRouter.AbortTask. ReferencedTaskTag=" + ( referencedTaskTag.ToString() )
            let struct( cid, counter ) = iScsiTask.AllegiantConnection
            let cmdSource = this.GetCommandSourceObject cid counter
            let itt = iScsiTask.InitiatorTaskTag
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, ValueSome cmdSource, itt, ValueSome lun, msg ) )

            match m_LU.TryGetValue( lun ) with
            | false, _ ->
                let msg = sprintf "Unknown LU target(LUN=%s)." ( lun_me.toString lun )
                HLogger.Trace( LogID.E_MISSING_LU, fun g -> g.Gen1( m_ObjID, ValueSome cmdSource, itt, ValueSome lun, msg ) )
                raise <| SessionRecoveryException( msg, m_TSIH )
            | true, lu ->
                lu.AbortTask cmdSource itt.Value referencedTaskTag

        // ------------------------------------------------------------------------
        // ABORT TASK SET task management function request.
        // It aborts all of the task that established by the session in specified logical unit.
        override this.AbortTaskSet ( iScsiTask : IIscsiTask ) ( lun : LUN_T ) : unit =
            let struct( cid, counter ) = iScsiTask.AllegiantConnection
            let cmdSource = this.GetCommandSourceObject cid counter
            let itt = iScsiTask.InitiatorTaskTag
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, ValueSome cmdSource, itt, ValueSome lun, "TaskRouter.AbortTask." ) )

            match m_LU.TryGetValue( lun ) with
            | false, _ ->
                let msg = sprintf "Unknown LU target(LUN=%s)." ( lun_me.toString lun )
                HLogger.Trace( LogID.E_MISSING_LU, fun g -> g.Gen1( m_ObjID, ValueSome cmdSource, itt, ValueSome lun, msg ) )
                raise <| SessionRecoveryException( msg, m_TSIH )
            | true, lu ->
                lu.AbortTaskSet cmdSource itt.Value

        // ------------------------------------------------------------------------
        // CLEAR ACA task management function request.
        // It clears the ACA state in specified logical unit.
        override this.ClearACA ( iScsiTask : IIscsiTask ) ( lun : LUN_T ) : unit =
            let struct( cid, counter ) = iScsiTask.AllegiantConnection
            let cmdSource = this.GetCommandSourceObject cid counter
            let itt = iScsiTask.InitiatorTaskTag
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, ValueSome cmdSource, itt, ValueSome lun, "TaskRouter.AbortTaskSet." ) )

            match m_LU.TryGetValue( lun ) with
            | false, _ ->
                let msg = sprintf "Unknown LU target(LUN=%s)." ( lun_me.toString lun )
                HLogger.Trace( LogID.E_MISSING_LU, fun g -> g.Gen1( m_ObjID, ValueSome cmdSource, itt, ValueSome lun, msg ) )
                raise <| SessionRecoveryException( msg, m_TSIH )
            | true, lu ->
                lu.ClearACA cmdSource itt.Value

        // ------------------------------------------------------------------------
        // CLEAR TASK SET task management function request.
        // It aborts all of the task in specified logical unit.
        override this.ClearTaskSet ( iScsiTask : IIscsiTask ) ( lun : LUN_T ) : unit =
            let struct( cid, counter ) = iScsiTask.AllegiantConnection
            let cmdSource = this.GetCommandSourceObject cid counter
            let itt = iScsiTask.InitiatorTaskTag
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, ValueSome cmdSource, itt, ValueSome lun, "TaskRouter.ClearTaskSet." ) )

            match m_LU.TryGetValue( lun ) with
            | false, _ ->
                let msg = sprintf "Unknown LU target(LUN=%s)." ( lun_me.toString lun )
                HLogger.Trace( LogID.E_MISSING_LU, fun g -> g.Gen1( m_ObjID, ValueSome cmdSource, itt, ValueSome lun, msg ) )
                raise <| SessionRecoveryException( msg, m_TSIH )
            | true, lu ->
                lu.ClearTaskSet cmdSource itt.Value

        // ------------------------------------------------------------------------
        // LOGICAL UNIT RESET task management function request.
        // It resets specified logical unit.
        override this.LogicalUnitReset ( iScsiTask : IIscsiTask ) ( lun : LUN_T ) : unit =
            let struct( cid, counter ) = iScsiTask.AllegiantConnection
            let cmdSource = this.GetCommandSourceObject cid counter
            let itt = iScsiTask.InitiatorTaskTag
            let loginfo = struct( m_ObjID, ValueSome cmdSource, itt, ValueSome lun )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "TaskRouter.LogicalUnitReset." ) )

            match m_LU.TryGetValue( lun ) with
            | false, _ ->
                let msg = sprintf "Unknown LU target(LUN=%s)." ( lun_me.toString lun )
                HLogger.Trace( LogID.E_MISSING_LU, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException( msg, m_TSIH )
            | true, lu ->
                lu.LogicalUnitReset ( ValueSome cmdSource ) itt

        // ------------------------------------------------------------------------
        // SCSI Command request.
        override this.SCSICommand ( cid : CID_T ) ( counter : CONCNT_T ) ( command : SCSICommandPDU ) ( data : SCSIDataOutPDU list ) : unit =
            let itt = command.InitiatorTaskTag
            let lun = command.LUN
            let cmdSource = this.GetCommandSourceObject cid counter
            let loginfo = struct( m_ObjID, ValueSome cmdSource, ValueSome itt, ValueSome lun )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "TaskRouter.SCSICommand." ) )

            match m_LU.TryGetValue( lun ) with
            | false, _ ->
                let msg = sprintf "Unknown LU target(LUN=%s)." ( lun_me.toString lun )
                HLogger.Trace( LogID.E_MISSING_LU, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException( msg, m_TSIH )
            | true, lu ->
               lu.SCSICommand cmdSource command data

        // ------------------------------------------------------------------------
        // Send response data to the initiator.
        override _.SendSCSIResponse
                ( reqCmdPDU : SCSICommandPDU )
                ( cid : CID_T )
                ( counter : CONCNT_T )
                ( recvDataLength : uint32 )
                ( argRespCode : iScsiSvcRespCd )
                ( argStatCode : ScsiCmdStatCd )
                ( senseData : PooledBuffer )
                ( resData : PooledBuffer )
                ( allocationLength : uint32 )
                ( needResponseFence : ResponseFenceNeedsFlag ) : unit =

            // Warning!!!
            // By the time this is called, reqCmdPDU.DataSegment may have already been recycled and should not be accessed.

            let itt = reqCmdPDU.InitiatorTaskTag
            let lun = reqCmdPDU.LUN
            let loginfo = struct ( m_ObjID, ValueSome( cid ), ValueSome( counter ), ValueSome( m_TSIH ), ValueSome( itt ), ValueSome( lun ) )
            if HLogger.IsVerbose then
                let tracemsg = sprintf "TaskRouter.SendSCSIResponse.RespFence=%s" ( ResponseFenceNeedsFlag.toString needResponseFence )
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, tracemsg ) )

            if m_Killer.IsNoticed then
                // If session is already dropped, it ommit sending response data.
                let msg = "Session is already dropped. CmdSN=" + ( string reqCmdPDU.CmdSN )
                HLogger.Trace( LogID.W_RETURN_DATA_DROPPED, fun g -> g.Gen1( loginfo, msg ) )
            else
                m_Session.SendSCSIResponse reqCmdPDU cid counter recvDataLength argRespCode argStatCode senseData resData allocationLength needResponseFence

        // ------------------------------------------------------------------------
        // Send response PDU other than SCSI Responce DPU.
        override _.SendOtherResponse ( cid : CID_T ) ( counter : CONCNT_T ) ( pdu : ILogicalPDU ) ( lun : LUN_T ) : unit =
            let loginfo = struct ( m_ObjID, ValueSome( cid ), ValueSome( counter ), ValueSome( m_TSIH ), ValueSome( pdu.InitiatorTaskTag ), ValueSome( lun ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "TaskRouter.SendOtherResponse" ) )

            if m_Killer.IsNoticed then
                // If session is already dropped, it ommit sending response data.
                let msg = sprintf "Session is already dropped. Opcode=%s CmdSN=%s" ( Constants.getOpcodeNameFromValue pdu.Opcode ) ( string pdu.CmdSN )
                HLogger.Trace( LogID.W_RETURN_DATA_DROPPED, fun g -> g.Gen1( loginfo, msg ) )
            else
                m_Session.SendOtherResponsePDU cid counter pdu


        // ------------------------------------------------------------------------
        // Get the TSIH value of the session, that hosts this protocol service component.
        override _.TSIH : TSIH_T =
            m_TSIH

        // ------------------------------------------------------------------------
        // Notice the session recovery to this session.
        override _.NoticeSessionRecovery ( msg : string ) : unit =
            // Notice destroy command to session object
            HLogger.Trace( LogID.E_SESSION_RECOVERY, fun g -> g.Gen1( m_ObjID, ValueNone, ValueNone, ValueSome( m_TSIH ), ValueNone, ValueNone, msg ) )
            m_Session.DestroySession()

        // ------------------------------------------------------------------------
        // Notice the session recovery to this session.
        override _.SessionParameter =
            m_SessionParameter

        // ------------------------------------------------------------------------
        // Get LUNs which is accessable from same target.
        override _.GetLUNs() : LUN_T[] =
            [|
                for itr in m_LU -> itr.Key
            |]

        // ------------------------------------------------------------------------
        // Get used count of the task queue.
        override _.GetTaskQueueUsage() : int =
            let wusage = [|
                for itr in m_LU do
                    if itr.Key <> lun_me.zero then
                        yield itr.Value.GetTaskQueueUsage( m_TSIH )
            |]

            if wusage.Length <= 0 then
                0
            else
                wusage |> Array.max

    //=========================================================================
    // Private method

    member private this.GetCommandSourceObject ( cid : CID_T ) ( conCounter : CONCNT_T ) : CommandSourceInfo =
        let idx = ( cid_me.toPrim cid |> uint64 ) <<< 32 ||| ( concnt_me.toPrim conCounter |> uint64 )
        m_CommandSourcePool.GetOrAdd( idx, fun _ -> {
            I_TNexus = m_I_T_Nexus;
            CID = cid;
            ConCounter = conCounter;
            TSIH = m_TSIH;
            ProtocolService = this;
            SessionKiller = m_Killer;
        })

