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
type PlainFileMedia
    (
        m_StatusMaster : IStatus,
        m_Config : TargetGroupConf.T_PlainFile,
        m_Killer : IKiller,
        m_LUN : LUN_T
    ) as this =

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    /// file
    let m_vfile, m_FileSize = 
        try
            let v = [|
                for i = 0u to m_Config.MaxMultiplicity - 1u do
                    yield new FileStream( m_Config.FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 512, true )
            |]
            ( v, v.[0].Length )
        with
        | _ as x ->
            HLogger.Trace( LogID.E_FILE_OPEN_ERROR, fun g ->
                let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                g.Gen3( loginfo, m_Config.FileName, x.GetType().FullName, "" )
            )
            reraise()

    /// Lock object of control multiplicity
    let m_MulSema = new SemaphoreSlim( int( m_Config.MaxMultiplicity ) )

    /// m_vfile index counter
    let mutable m_FileIdx = 0

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
            // Close file handle
            m_vfile |> Array.iter ( fun i ->
                if i.CanWrite then
                    try
                        i.Flush()
                    with
                    | :? ObjectDisposedException as x ->
                        // Stream is already closed, so ignore this exception.
                        ()
                    | :? IOException as x ->
                        // At this point if flush error is occurred, there is nothing we can do.
                        // So only output a log message and ignore this error.
                        HLogger.Trace(
                            LogID.E_FILE_FLUSH_ERROR, fun g ->
                                g.Gen3(
                                    loginfo, m_Config.FileName, x.GetType().FullName,
                                    "In terminate process, failed to flush file stream, so some data may be lost."
                                )
                        )
                i.Close()
                i.Dispose()
            )
            for i = 0 to m_vfile.Length - 1 do
                m_vfile.[i] <- null
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
            // Close file handle
            m_vfile |> Array.iter ( fun i ->
                if i.CanWrite then
                    try
                        i.Flush()
                    with
                    | :? ObjectDisposedException as x ->
                        // Stream is already closed, so ignore this exception.
                        ()
                    | :? IOException as x ->
                        // At this point if flush error is occurred, there is nothing we can do.
                        // So only output a log message.
                        HLogger.Trace(
                            LogID.E_FILE_FLUSH_ERROR, fun g ->
                                g.Gen3(
                                    loginfo, m_Config.FileName, x.GetType().FullName,
                                    "In terminate process, failed to flush file stream, so some data may be lost."
                                )
                        )
                i.Close()
                i.Dispose()
            )
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
            uint64 m_FileSize / Constants.MEDIA_BLOCK_SIZE

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
            let readpos_u64 : uint64 = ( blkcnt_me.toUInt64 argLBA ) * Constants.MEDIA_BLOCK_SIZE
            let mediaBlockCount = ( ( uint64 m_FileSize ) / Constants.MEDIA_BLOCK_SIZE )

            // Check specified range is in media file.
            if Functions.CheckAccessRange argLBA readBytesLength_u64 mediaBlockCount Constants.MEDIA_BLOCK_SIZE |> not then
                let errmsg =
                    sprintf
                        "Out of media capacity. BlockSize=%d, TotalBlockCount=%d, RequestedLBA=%d, RequestedBytesCount=%d"
                        Constants.MEDIA_BLOCK_SIZE
                        ( ( uint64 m_FileSize ) / Constants.MEDIA_BLOCK_SIZE )
                        argLBA
                        buffer.Count
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )
                raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )

            // Check limit of this module
            if readpos_u64 >= 0x8000000000000000UL || readBytesLength_u64 >= 0x0000000080000000UL then
                let errmsg =
                    sprintf
                        "Out of module limits. BlockSize=%d, TotalBlockCount=0x%016X, RequestedLBA=0x%016X, RequestedBytesCount=%d"
                        Constants.MEDIA_BLOCK_SIZE
                        ( ( uint64 m_FileSize ) / Constants.MEDIA_BLOCK_SIZE )
                        argLBA
                        buffer.Count
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.CONFIGURATION_OF_INCAPABLE_LOGICAL_UNITS_FAILED, errmsg )
                raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.CONFIGURATION_OF_INCAPABLE_LOGICAL_UNITS_FAILED, errmsg )

            // Check buffer length is equal to read bytes count.
            assert( uint64 buffer.Count = readBytesLength_u64 )

            let readBytesLength32 = int32 readBytesLength_u64
            let readpos64 = int64 readpos_u64
            let mutable wrpos : int = 0

            let wReadFunc() = task {
                // Wait for access
                let! wresult = m_MulSema.WaitAsync( m_Config.QueueWaitTimeOut )
                if not wresult then
                    let errmsg = "Media access timed out. Read request was failed."
                    HLogger.ACAException( loginfo, SenseKeyCd.MEDIUM_ERROR, ASCCd.TIMEOUT_ON_LOGICAL_UNIT, errmsg )
                    raise <| SCSIACAException ( source, true, SenseKeyCd.MEDIUM_ERROR, ASCCd.TIMEOUT_ON_LOGICAL_UNIT, errmsg )

                // get stream index
                let wsidx = ( Interlocked.Increment &m_FileIdx ) - 1
                let! readcnt =
                    try
                        // seek
                        m_vfile.[wsidx].Seek( readpos64 + int64 wrpos, SeekOrigin.Begin ) |> ignore
                        // read
                        m_vfile.[wsidx].ReadAsync( buffer.Array, buffer.Offset + wrpos, readBytesLength32 - wrpos, CancellationToken.None )
                    finally
                        Interlocked.Decrement &m_FileIdx |> ignore
                        m_MulSema.Release() |> ignore

                if readBytesLength32 <= readcnt + wrpos then
                    // succeed
                    return Ok()
                else
                    wrpos <- wrpos + readcnt
                    return Error( sprintf "Partial read %d / %d" wrpos readBytesLength32 )
            }

            let errorCheckFunc : Exception -> bool =
                function
                | :? SCSIACAException as x ->
                    raise x
                | :? IOException as x ->
                    HLogger.Trace( LogID.E_IO_ERROR_RETRY, fun g -> g.Gen1( loginfo, m_Config.FileName ) )
                    false    // retry
                | _ as x ->
                    // Unexpected I/O error
                    let errmsg = "Unexpected I/O error was occured. " + x.Message
                    HLogger.ACAException( loginfo, SenseKeyCd.MEDIUM_ERROR, ASCCd.UNRECOVERED_READ_ERROR, errmsg )
                    raise <| SCSIACAException ( source, true, SenseKeyCd.MEDIUM_ERROR, ASCCd.UNRECOVERED_READ_ERROR, errmsg )

            // do read loop
            task {
                match! Functions.RetryAsync1 wReadFunc errorCheckFunc with
                | Ok() ->
                    sw.Stop()
                    let d = DateTime.UtcNow
                    m_ReadBytesCounter.AddCount d ( int64 buffer.Count )
                    m_ReadTickCounter.AddCount d sw.ElapsedTicks 
                    return buffer.Count
                | Error( x ) ->
                    // Retry count over, ACA will be established.
                    let errmsg = "I/O retry count overed. Read request was failed. Last error message=" + x
                    HLogger.ACAException( loginfo, SenseKeyCd.MEDIUM_ERROR, ASCCd.UNRECOVERED_READ_ERROR, errmsg )
                    raise <| SCSIACAException ( source, true, SenseKeyCd.MEDIUM_ERROR, ASCCd.UNRECOVERED_READ_ERROR, errmsg )
                    return 0
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

            assert( offset < Constants.MEDIA_BLOCK_SIZE )
            let sw = new Stopwatch()
            sw.Start()
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "PlainFileMedia.Write." ) )

            //let ivrand = new Random()
            let writeBytesLength_u64 = uint64 data.Count
            let writepos_u64 = ( blkcnt_me.toUInt64 argLBA ) * Constants.MEDIA_BLOCK_SIZE + offset
            let mediaBlockCount = ( ( uint64 m_FileSize ) / Constants.MEDIA_BLOCK_SIZE )

            // Check specified range is in media file.
            if Functions.CheckAccessRange argLBA writeBytesLength_u64 mediaBlockCount Constants.MEDIA_BLOCK_SIZE |> not then
                let errmsg = 
                    sprintf
                        "Out of media capacity. BlockSize=%d, TotalBlockCount=%d, RequestedLBA=%d, RequestedOffset=%d, RequestedBytesCount=%d"
                        Constants.MEDIA_BLOCK_SIZE
                        ( ( uint64 m_FileSize ) / Constants.MEDIA_BLOCK_SIZE )
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

            let writeBytesLength32 = int32 writeBytesLength_u64
            let writepos64 = int64 writepos_u64

            let wWriteFunc() = task {
                // Wait for access
                let! wresult = m_MulSema.WaitAsync( m_Config.QueueWaitTimeOut )
                if not wresult then
                    let errmsg = "Media access timed out. Write request was failed."
                    HLogger.ACAException( loginfo, SenseKeyCd.MEDIUM_ERROR, ASCCd.TIMEOUT_ON_LOGICAL_UNIT, errmsg )
                    raise <| SCSIACAException ( source, true, SenseKeyCd.MEDIUM_ERROR, ASCCd.TIMEOUT_ON_LOGICAL_UNIT, errmsg )

                // get stream index
                let wsidx = ( Interlocked.Increment &m_FileIdx ) - 1

                // Write
                try
                    // seek
                    m_vfile.[wsidx].Seek( writepos64, SeekOrigin.Begin ) |> ignore
                    // Write
                    do! m_vfile.[wsidx].WriteAsync( data.Array, data.Offset, writeBytesLength32, CancellationToken.None )
                    // Flush
                    do! m_vfile.[wsidx].FlushAsync()
                finally
                    Interlocked.Decrement &m_FileIdx |> ignore
                    m_MulSema.Release() |> ignore
            }

            let errorCheckFunc : Exception -> bool =
                function
                | :? SCSIACAException as x ->
                    raise x
                | :? IOException as x ->
                    HLogger.Trace( LogID.E_IO_ERROR_RETRY, fun g -> g.Gen1( loginfo, m_Config.FileName ) )
                    false    // retry continue
                | _ as x ->
                    // Unexpected I/O error
                    let errmsg = "Unexpected I/O error was occured. " + x.Message
                    HLogger.ACAException( loginfo, SenseKeyCd.MEDIUM_ERROR, ASCCd.WRITE_ERROR, errmsg )
                    raise <| SCSIACAException ( source, true, SenseKeyCd.MEDIUM_ERROR, ASCCd.WRITE_ERROR, errmsg )

            // Do write loop
            task {
                match! Functions.RetryAsync2 wWriteFunc errorCheckFunc with
                | Ok() ->
                    sw.Stop()
                    let d = DateTime.UtcNow
                    m_WrittenBytesCounter.AddCount d ( int64 data.Count )
                    m_WriteTickCounter.AddCount d sw.ElapsedTicks
                    return data.Count
                | Error( x ) ->
                    // Retry count over, ACA will be established.
                    let errmsg = "I/O retry count overed. Write request is failed."
                    HLogger.ACAException( loginfo, SenseKeyCd.MEDIUM_ERROR, ASCCd.WRITE_ERROR, errmsg )
                    raise <| SCSIACAException ( source, true, SenseKeyCd.MEDIUM_ERROR, ASCCd.WRITE_ERROR, errmsg )
                    return 0
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
        override _.BlockCount = uint64( m_FileSize ) / Constants.MEDIA_BLOCK_SIZE

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
            sprintf "Simple File Media(File Name=%s)" m_Config.FileName

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

