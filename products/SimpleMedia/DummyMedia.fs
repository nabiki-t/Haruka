//=============================================================================
// Haruka Software Storage.
// DummyMedia.fs : Defines DummyMedia class.
// DummyMedia is used in LUN 0 block device.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.Media

//=============================================================================
// Import declaration

open System
open System.Threading.Tasks

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Class implementation

/// <summary>
///  DummyMedia class definition. The length of the DummyMedia is always 0 and all processing requests fail.
/// </summary>
/// <param name="m_Killer">
///  Killer object that notice terminate request to this object.
/// </param>
/// <param name="m_LUN">
///  LUN of LU which access to this media.
/// </param>
type DummyMedia
    (
        m_Killer : IKiller,
        m_LUN : LUN_T
    ) as this =

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    do
        m_Killer.Add this
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g -> g.Gen2( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ), "DummyMedia", "" ) )

    interface IMedia with

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override _.Terminate() : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ), "DummyMedia.Terminate." ) )
            // Nothig to do
            ()
    
        // ------------------------------------------------------------------------
        // Implementation of Initialize method
        override _.Initialize() : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ), "DummyMedia.Initialize." ) )
            // Nothing to do
            ()

        // ------------------------------------------------------------------------
        // Implementation of Finalize method
        override _.Closing() : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ), "DummyMedia.Closing." ) )
            // Nothing to do
            ()

        // ------------------------------------------------------------------------
        // Implementation of TestUnitReady method
        override _.TestUnitReady( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : ASCCd voption =
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "DummyMedia.TestUnitReady." ) )
            let msg = "DummyMedia was accessed"
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_COMMAND_OPERATION_CODE, msg )
            raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_COMMAND_OPERATION_CODE, msg )
            ValueNone    // Always returns true

        // ------------------------------------------------------------------------
        // Implementation of ReadCapacity method
        override _.ReadCapacity( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : uint64 =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ), "DummyMedia.ReadCapacity." ) )
            0UL

        // ------------------------------------------------------------------------
        // Implementation of Read method
        override _.Read
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( _ : uint64 )
            ( _ : ArraySegment<byte> )
            : Task<int> =

            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "DummyMedia.Read." ) )
            let msg = "DummyMedia was accessed"
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_COMMAND_OPERATION_CODE, msg )
            raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_COMMAND_OPERATION_CODE, msg )

            Task.FromResult 0


        // ------------------------------------------------------------------------
        // Implementation of Write method
        override _.Write
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( _ : uint64 )
            ( _ : uint64 )
            ( _ : ArraySegment<byte> )
            : Task<int> =

            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "DummyMedia.Write." ) )
            let msg = "DummyMedia was accessed"
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_COMMAND_OPERATION_CODE, msg )
            raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_COMMAND_OPERATION_CODE, msg )

            task{ return 0 }

        // ------------------------------------------------------------------------
        // Implementation of Format method
        override _.Format( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : Task<unit> =
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "DummyMedia.Format." ) )
            let msg = "DummyMedia was accessed"
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_COMMAND_OPERATION_CODE, msg )
            raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_COMMAND_OPERATION_CODE, msg )
            Task.FromResult ()

        // ------------------------------------------------------------------------
        // Notify logical unit reset.
        override _.NotifyLUReset ( initiatorTaskTag : ITT_T voption ) ( source : CommandSourceInfo voption ) : unit =
            // to close all of file handle, redirect to Finalize method.
            if HLogger.IsVerbose then
                let loginfo = struct( m_ObjID, source, initiatorTaskTag, ValueSome m_LUN )
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "DummyMedia.NotifyLUReset." ) )
            ( this :> IMedia ).Closing()

        // ------------------------------------------------------------------------
        // Media control request.
        override _.MediaControl ( request : MediaCtrlReq.T_Request ) : Task<MediaCtrlRes.T_Response> =
            task {
                return MediaCtrlRes.U_Unexpected( "Dummy media does not support media controls." )
            }

        // ------------------------------------------------------------------------
        // Get block count
        override _.BlockCount = 0UL

        // ------------------------------------------------------------------------
        // Get write protect
        override _.WriteProtect =
            true

        // ------------------------------------------------------------------------
        // Media index ID
        override _.MediaIndex = mediaidx_me.zero

        // ------------------------------------------------------------------------
        // String that descripts this media.
        override _.DescriptString = "Dummy media"

        // ------------------------------------------------------------------------
        // Obtain the total number of read bytes.
        override _.GetReadBytesCount() : ResCountResult[] =
            // All of access to the dummy media are treated as an error. 
            // Therefore, there is no statistical information that can be provided.
            Array.empty

        // ------------------------------------------------------------------------
        // Obtain the total number of written bytes.
        override _.GetWrittenBytesCount() : ResCountResult[] =
            // All of access to the dummy media are treated as an error. 
            // Therefore, there is no statistical information that can be provided.
            Array.empty

        // ------------------------------------------------------------------------
        // Obtain the tick count of read operation.
        override _.GetReadTickCount() : ResCountResult[] =
            // All of access to the dummy media are treated as an error. 
            // Therefore, there is no statistical information that can be provided.
            Array.empty

        // ------------------------------------------------------------------------
        // Obtain the tick count of write operation.
        override _.GetWriteTickCount() : ResCountResult[] =
            // All of access to the dummy media are treated as an error. 
            // Therefore, there is no statistical information that can be provided.
            Array.empty

        // ------------------------------------------------------------------------
        // Get sub media object.
        override _.GetSubMedia() : IMedia list = []
