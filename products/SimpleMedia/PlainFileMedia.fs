//=============================================================================
// Haruka Software Storage.
// PlainFileMedia.fs : Defines PlainFileMedia class.
// PlainFileMedia class implement file I-O block device functionality.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.Media

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Diagnostics

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Class implementation

/// <summary>
///  PlainFileMedia class definition.
/// </summary>
/// <param name="m_StatusMaster">
///  Interface of StatusMaster instance.
/// </param>
/// <param name="m_Config">
///  Configuration information of this media object.
/// </param>
/// <param name="m_Killer">
///  Killer object that notice terminate request to this object.
/// </param>
/// <param name="m_LUN">
///  LUN of LU which access to this media.
/// </param>
/// <param name="m_Multiplicity">
///   Maximum number of simultaneous accesses.
/// </param>
type PlainFileMedia
    (
        m_StatusMaster : IStatus,
        m_Config : TargetGroupConf.T_PlainFile,
        m_Killer : IKiller,
        m_LUN : LUN_T,
        m_Multiplicity : uint32
    ) as this =

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    /// file, file size in bytes
    /// With Plain file media, the file size will not change while it is running.
    let m_File, m_FileSize = 
        try
            let fa = FileAccessor( m_Config.FileName, m_Multiplicity, m_Config.WriteProtect )
            fa, fa.GetFileSize()
        with
        | _ as x ->
            HLogger.Trace( LogID.E_FILE_OPEN_ERROR, fun g ->
                let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                g.Gen3( loginfo, m_Config.FileName, x.GetType().FullName, "" )
            )
            reraise()

    let m_BlockSize = Blocksize.toUInt64 m_Config.BlockSize

    /// Resource counter for read data
    let m_ReadBytesCounter = new ResCounter( Constants.RECOUNTER_SPAN_SEC, Constants.RESCOUNTER_LENGTH_SEC )

    /// Resource counter for written data
    let m_WrittenBytesCounter = new ResCounter( Constants.RECOUNTER_SPAN_SEC, Constants.RESCOUNTER_LENGTH_SEC )

    /// Resource counter for read response time
    let m_ReadTickCounter = new ResCounter( Constants.RECOUNTER_SPAN_SEC, Constants.RESCOUNTER_LENGTH_SEC )

    /// Resource counter for write response time
    let m_WriteTickCounter = new ResCounter( Constants.RECOUNTER_SPAN_SEC, Constants.RESCOUNTER_LENGTH_SEC )

    do
        m_Killer.Add this
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g ->
            let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
            let msg = "FileName=" + m_Config.FileName
            g.Gen2( loginfo, "PlainFileMedia", msg )
        )

    interface IMedia with

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override _.Terminate() : unit =
            let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "PlainFileMedia.Terminate." ) )
            m_File.Close()
            HLogger.Trace( LogID.I_FILE_CLOSED, fun g -> g.Gen1( loginfo, m_Config.FileName ) )

    
        // ------------------------------------------------------------------------
        // Implementation of Initialize method
        override _.Initialize() : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "PlainFileMedia.Initialize." )
                )
            // Nothing to do

        // ------------------------------------------------------------------------
        // Implementation of Finalize method
        override _.Closing() : unit =
            let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "PlainFileMedia.Closing." ) )
            m_File.Close()
            HLogger.Trace( LogID.I_FILE_CLOSED, fun g -> g.Gen1( loginfo, m_Config.FileName ) )

        // ------------------------------------------------------------------------
        // Implementation of TestUnitReady method
        override _.TestUnitReady( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : ASCCd voption =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "PlainFileMedia.TestUnitReady." )
                )
            ValueNone    // Always returns true

        // ------------------------------------------------------------------------
        // Implementation of ReadCapacity method
        override _.ReadCapacity( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : uint64 =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "PlainFileMedia.ReadCapacity." )
                )
            uint64 m_FileSize / m_BlockSize

        // ------------------------------------------------------------------------
        // Implementation of Read method
        override _.Read
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( argLBA : BLKCNT64_T )
            ( buffer : ArraySegment<byte> )
            : Task<int> =

            let sw = new Stopwatch()
            sw.Start()
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )

            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "PlainFileMedia.Read." ) )

            let readBytesLength_u64 = uint64 buffer.Count
            let readpos_u64 : uint64 = ( blkcnt_me.toUInt64 argLBA ) * m_BlockSize
            let mediaBlockCount = ( ( uint64 m_FileSize ) / m_BlockSize )

            // Check specified range is in media file.
            if Functions.CheckAccessRange argLBA readBytesLength_u64 mediaBlockCount m_BlockSize |> not then
                let errmsg =
                    sprintf
                        "Out of media capacity. BlockSize=%d, TotalBlockCount=%d, RequestedLBA=%d, RequestedBytesCount=%d"
                        m_BlockSize
                        ( ( uint64 m_FileSize ) / m_BlockSize )
                        argLBA
                        buffer.Count
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )
                raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )

            // Check limit of this module
            if readpos_u64 >= 0x8000000000000000UL || readBytesLength_u64 >= 0x0000000080000000UL then
                let errmsg =
                    sprintf
                        "Out of module limits. BlockSize=%d, TotalBlockCount=0x%016X, RequestedLBA=0x%016X, RequestedBytesCount=%d"
                        m_BlockSize
                        ( ( uint64 m_FileSize ) / m_BlockSize )
                        argLBA
                        buffer.Count
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.CONFIGURATION_OF_INCAPABLE_LOGICAL_UNITS_FAILED, errmsg )
                raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.CONFIGURATION_OF_INCAPABLE_LOGICAL_UNITS_FAILED, errmsg )

            task {
                try
                    do! m_File.Read readpos_u64 buffer
                    let d = DateTime.UtcNow
                    m_ReadBytesCounter.AddCount d ( int64 buffer.Count )
                    m_ReadTickCounter.AddCount d sw.ElapsedTicks 
                    return buffer.Count
                with
                | _ as x ->
                    // Retry count over, ACA will be established.
                    let errmsg = "I/O retry count overed. Read request was failed. Last error message=" + x.Message
                    HLogger.ACAException( loginfo, SenseKeyCd.MEDIUM_ERROR, ASCCd.UNRECOVERED_READ_ERROR, errmsg )
                    return ( raise <| SCSIACAException ( source, true, SenseKeyCd.MEDIUM_ERROR, ASCCd.UNRECOVERED_READ_ERROR, errmsg ) )
            }

        // ------------------------------------------------------------------------
        // Implementation of Write method
        override _.Write
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( argLBA : BLKCNT64_T )
            ( offset : uint64 )
            ( data : ArraySegment<byte> )
            : Task<int> =

            assert( offset < m_BlockSize )
            let sw = new Stopwatch()
            sw.Start()
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "PlainFileMedia.Write." ) )

            let writeBytesLength_u64 = uint64 data.Count
            let writepos_u64 = ( blkcnt_me.toUInt64 argLBA ) * m_BlockSize + offset
            let mediaBlockCount = ( ( uint64 m_FileSize ) / m_BlockSize )

            // Check specified range is in media file.
            if Functions.CheckAccessRange argLBA ( writeBytesLength_u64 + offset ) mediaBlockCount m_BlockSize |> not then
                let errmsg = 
                    sprintf
                        "Out of media capacity. BlockSize=%d, TotalBlockCount=%d, RequestedLBA=%d, RequestedOffset=%d, RequestedBytesCount=%d"
                        m_BlockSize
                        ( ( uint64 m_FileSize ) / m_BlockSize )
                        argLBA
                        offset
                        writeBytesLength_u64
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )
                raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )

            // Check read only or not
            if m_Config.WriteProtect then
                let errmsg = "Write protected."
                HLogger.ACAException( loginfo, SenseKeyCd.DATA_PROTECT, ASCCd.WRITE_PROTECTED, errmsg )
                raise <| SCSIACAException ( source, true, SenseKeyCd.DATA_PROTECT, ASCCd.WRITE_PROTECTED, errmsg )

            task {
                try
                    do! m_File.Write writepos_u64 data
                    sw.Stop()
                    let d = DateTime.UtcNow
                    m_WrittenBytesCounter.AddCount d ( int64 data.Count )
                    m_WriteTickCounter.AddCount d sw.ElapsedTicks
                    return data.Count
                with
                | _ as x ->
                    let errmsg = "I/O retry count overed. Write request is failed. Last error message=" + x.Message
                    HLogger.ACAException( loginfo, SenseKeyCd.MEDIUM_ERROR, ASCCd.WRITE_ERROR, errmsg )
                    return( raise <| SCSIACAException ( source, true, SenseKeyCd.MEDIUM_ERROR, ASCCd.WRITE_ERROR, errmsg ) )
            }

        // ------------------------------------------------------------------------
        // Implementation of Format method
        override _.Format( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : Task<unit> =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "PlainFileMedia.Format." )
                )
            // Nothig to do
            Task.FromResult ()

        // ------------------------------------------------------------------------
        // Notify logical unit reset.
        override _.NotifyLUReset ( initiatorTaskTag : ITT_T voption ) ( source : CommandSourceInfo voption ) : unit =
            // to close all of file handle, redirect to Finalize method.
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, source, initiatorTaskTag, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "PlainFileMedia.NotifyLUReset." )
                )
            ( this :> IMedia ).Closing()

        // ------------------------------------------------------------------------
        // Media control request.
        override _.MediaControl ( request : MediaCtrlReq.T_Request ) : Task<MediaCtrlRes.T_Response> =
            task {
                return MediaCtrlRes.U_Unexpected( "Plain file media does not support media controls." )
            }

        // ------------------------------------------------------------------------
        // Get block count
        override _.BlockCount = uint64( m_FileSize ) / m_BlockSize

        // ------------------------------------------------------------------------
        // Get block size
        override _.BlockSize = m_Config.BlockSize

        // ------------------------------------------------------------------------
        // Get write protect
        override _.WriteProtect =
            m_Config.WriteProtect

        // ------------------------------------------------------------------------
        // Media index ID
        override _.MediaIndex = m_Config.IdentNumber

        // ------------------------------------------------------------------------
        // String that descripts this media.
        override _.DescriptString =
            sprintf "Plain File Media(File Name=%s)" m_Config.FileName

        // ------------------------------------------------------------------------
        // Obtain the total number of read bytes.
        override _.GetReadBytesCount() : ResCountResult[] =
            m_ReadBytesCounter.Get DateTime.UtcNow

        // ------------------------------------------------------------------------
        // Obtain the total number of written bytes.
        override _.GetWrittenBytesCount() : ResCountResult[] =
            m_WrittenBytesCounter.Get DateTime.UtcNow

        // ------------------------------------------------------------------------
        // Obtain the tick count of read operation.
        override _.GetReadTickCount() : ResCountResult[] =
            // Tick ​​counts are calculated in Stopwatch.Frequency units, so they are converted to milliseconds.
            m_ReadTickCounter.Get DateTime.UtcNow
            |> Array.map ( fun itr -> {
                itr with
                    Value = itr.Value / ( Stopwatch.Frequency / 1000L )
            })

        // ------------------------------------------------------------------------
        // Obtain the tick count of write operation.
        override _.GetWriteTickCount() : ResCountResult[] =
            // Tick ​​counts are calculated in Stopwatch.Frequency units, so they are converted to milliseconds.
            m_WriteTickCounter.Get DateTime.UtcNow
            |> Array.map ( fun itr -> {
                itr with
                    Value = itr.Value / ( Stopwatch.Frequency / 1000L )
            })

        // ------------------------------------------------------------------------
        // Get sub media object.
        override _.GetSubMedia() : IMedia list = []

