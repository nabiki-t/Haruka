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
open System.Collections.Frozen

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
    let m_LUN =
        let v = [|
            yield lun_me.zero;
            for i in m_SessionParameter.TargetConf.LUN -> i
        |]
        v.ToFrozenSet()

    /// <summary>
    ///  A cache for reusing CommandSource objects.
    ///  In the current implementation, the constructed CommandSource object is not deleted until the session is destroyed.
    ///  If the connection is repeatedly reconnected while the session is maintained, garbage will accumulate, but we will not worry about it.
    /// </summary>
    let m_CommandSourcePool =
        ConcurrentDictionary< uint64, CommandSourceInfo >()

    do
        m_Killer.Add this
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g ->
            let loginfo = struct ( m_ObjID, ValueNone, ValueNone, ValueSome( m_TSIH ), ValueNone, ValueNone )
            g.Gen2( loginfo, "TaskRouter", "" )
        )

        // Let StatusMaster instantiate LU objects.
        for itr in m_LUN do
            match m_Status.GetLU itr with
            | ValueSome( _ ) ->
                HLogger.Trace( LogID.I_ASSIGN_LU_TO_SESSION, fun g ->
                    g.Gen0( m_ObjID, ValueNone, ValueNone, ValueSome m_TSIH, ValueNone, ValueSome itr )
                )
            | ValueNone ->
                HLogger.Trace( LogID.E_MISSING_LU, fun g ->
                    g.Gen0( m_ObjID, ValueNone, ValueNone, ValueSome m_TSIH, ValueNone, ValueSome itr )
                )
                let msg = "This LU is not assigned to session."
                raise <| SessionRecoveryException( msg, m_TSIH )


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

            let lu = this.GetLU lun cmdSource itt
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

            let lu = this.GetLU lun cmdSource itt
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

            let lu = this.GetLU lun cmdSource itt
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

            let lu = this.GetLU lun cmdSource itt
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

            let lu = this.GetLU lun cmdSource itt
            lu.LogicalUnitReset ( ValueSome cmdSource ) itt true

        // ------------------------------------------------------------------------
        // TARGET WARM RESET or TARGET COLD RESET task management function request.
        // It resets all logical unit which can be accessed from the session.
        override this.TargetReset ( iScsiTask : IIscsiTask ) ( lun : LUN_T ) : unit =
            let struct( cid, counter ) = iScsiTask.AllegiantConnection
            let cmdSource = this.GetCommandSourceObject cid counter
            let itt = iScsiTask.InitiatorTaskTag
            let loginfo = struct( m_ObjID, ValueSome cmdSource, itt, ValueSome lun )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "TaskRouter.TargetReset." ) )

            if m_LUN.Contains lun |> not then
                let msg = "Unknown LU target"
                HLogger.Trace( LogID.E_MISSING_LU, fun g -> g.Gen1( m_ObjID, ValueSome cmdSource, itt, ValueSome lun, msg ) )
                raise <| SessionRecoveryException( msg, m_TSIH )

            if lun = lun_me.zero then
                let lu = this.GetLU lun cmdSource itt
                lu.LogicalUnitReset ( ValueSome cmdSource ) itt true
            else
                m_LUN
                |> Seq.filter ( (<>) lun_me.zero )
                |> Seq.iter ( fun itrLUN ->
                    let lu = this.GetLU itrLUN cmdSource itt
                    lu.LogicalUnitReset ( ValueSome cmdSource ) itt ( itrLUN = lun )
                )

        // ------------------------------------------------------------------------
        // SCSI Command request.
        override this.SCSICommand ( cid : CID_T ) ( counter : CONCNT_T ) ( command : SCSICommandPDU ) ( data : SCSIDataOutPDU list ) : unit =
            let itt = command.InitiatorTaskTag
            let lun = command.LUN
            let cmdSource = this.GetCommandSourceObject cid counter
            let loginfo = struct( m_ObjID, ValueSome cmdSource, ValueSome itt, ValueSome lun )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "TaskRouter.SCSICommand." ) )

            let lu = this.GetLU lun cmdSource ( ValueSome itt )
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
                for itr in m_LUN -> itr
            |]

        // ------------------------------------------------------------------------
        // Get used count of the task queue.
        override _.GetTaskQueueUsage() : int =
            m_LUN
            |> Seq.fold ( fun ( m : int ) lun ->
                if lun = lun_me.zero then
                    m
                else
                    match m_Status.GetLU lun with
                    | ValueSome x ->
                        let r = x.GetTaskQueueUsage( m_TSIH )
                        max r m
                    | _ ->
                        m
            ) 0

    //=========================================================================
    // Private method

    /// <summary>
    ///  Gets a CommandSource object from the cache, or constructs a new one if it does not exist.
    /// </summary>
    /// <param name="cid">
    ///  Connection ID.
    /// </param>
    /// <param name="conCounter">
    ///  Connection counter.
    /// </param>
    /// <returns>
    ///  CommandSource object.
    /// </returns>
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

    /// <summary>
    ///  Obtains the LU object held by the Status Master.
    /// </summary>
    /// <param name="lun">
    ///  LUN.
    /// </param>
    /// <param name="cmdSource">
    ///  Information indicating the source of the command. This is used for logging.
    /// </param>
    /// <param name="itt">
    ///  ITT of the command. This is used for logging.
    /// </param>
    /// <returns>
    ///  LU object.
    /// </returns>
    /// <remarks>
    ///  If an inaccessible LUN is specified from the session, session recovery is performed.
    /// </remarks>
    member private _.GetLU ( lun : LUN_T ) ( cmdSource : CommandSourceInfo ) ( itt : ITT_T voption ) : ILU =
        if m_LUN.Contains lun |> not then
            let msg = "Unknown LU target"
            HLogger.Trace( LogID.E_MISSING_LU, fun g -> g.Gen1( m_ObjID, ValueSome cmdSource, itt, ValueSome lun, msg ) )
            raise <| SessionRecoveryException( msg, m_TSIH )

        match m_Status.GetLU lun with
        | ValueSome x ->
            x
        | ValueNone ->
            let msg = sprintf "Missing LU object"
            HLogger.Trace( LogID.E_MISSING_LU, fun g -> g.Gen1( m_ObjID, ValueSome cmdSource, itt, ValueSome lun, msg ) )
            raise <| SessionRecoveryException( msg, m_TSIH )
