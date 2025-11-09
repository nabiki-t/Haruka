//=============================================================================
// Haruka Software Storage.
// IscsiTaskAborted.fs : Defines IscsiTaskAborted class
// IscsiTaskAborted class represents aborted IIscsiTask.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.TargetDevice

//=============================================================================
// Import declaration

open System

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

/// <summary>
///  Represents an iSCSI task that was interrupted by TMF etc.
/// </summary>
/// <param name="m_Session">
///   The interface of the session object which this task belongings to.
/// </param>
/// <param name="m_AbortedTask">
///   Aborted task object.
/// </param>
type IscsiTaskAborted
    (
        m_Session : ISession,
        m_AbortedTask : IIscsiTask
    ) =

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    do
        HLogger.Trace( LogID.V_TRACE, fun g ->
            let msg = sprintf "IscsiTaskCanceled instance created." 
            let logInfo =
                let struct( cid, concnt ) = m_AbortedTask.AllegiantConnection
                struct ( m_ObjID, ValueSome cid, ValueSome concnt, ValueSome m_Session.TSIH, m_AbortedTask.InitiatorTaskTag, ValueNone )
            g.Gen1( logInfo, msg )
        )

    //=========================================================================
    // Interface method

    interface IIscsiTask with

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.TaskType
        override _.TaskType : iSCSITaskType =
            iSCSITaskType.Aborted

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.TaskTypeName
        override _.TaskTypeName : string =
            "Aborted task"

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.InitiatorTaskTag
        override _.InitiatorTaskTag : ITT_T voption =
            m_AbortedTask.InitiatorTaskTag

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.CmdSN
        override _.CmdSN : CMDSN_T voption =
            m_AbortedTask.CmdSN

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.LUN
        override _.LUN : LUN_T voption =
            m_AbortedTask.LUN

        // ------------------------------------------------------------------------
        // Implementation of IIscsiTask.Immidiate
        override _.Immidiate : bool voption =
            m_AbortedTask.Immidiate

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.IsExecutable
        override _.IsExecutable : bool =
            false

        // --------------------------------------------------------------------
        // Implementation of IIscsiTask.AllegiantConnection
        override _.AllegiantConnection : struct( CID_T * CONCNT_T ) =
            m_AbortedTask.AllegiantConnection

        // --------------------------------------------------------------------
        // Execute this command.
        override this.GetExecuteTask () : struct( ( unit -> unit ) * IIscsiTask ) =
            // Nothig to do.
            struct( ( fun () -> () ), this )

        // --------------------------------------------------------------------
        //   This task already compleated and removale or not.
        override _.IsRemovable : bool =
            // this task is always compleated and removable.
            true

        // ------------------------------------------------------------------------
        // GetExecuteTask method had been called or not.
        override _.Executed : bool =
            // this task is always considered as executed.
            true
