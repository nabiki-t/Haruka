//=============================================================================
// Haruka Software Storage.
// MemBufferMedia.fs : Defines MemBufferMedia class.
// MemBuffer class implement RAM disk functionality.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.Media

//=============================================================================
// Import declaration

open System
open System.Threading.Tasks
open System.Diagnostics

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

//=============================================================================
// Class implementation

/// <summary>
///  MemBufferMedia class definition.
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
/// <param name="argBufferLineBlocks">
///  The block count of one segment when segmenting the buffer.
/// </param>
/// <param name="m_BlockSize">
///  Media block size in bytes.
/// </param>
type MemBufferMedia
    (
        m_StatusMaster : IStatus,
        m_Config : TargetGroupConf.T_MemBuffer,
        m_Killer : IKiller,
        m_LUN : LUN_T,
        argBufferLineBlocks : uint64,
        m_BlockSize : uint64
    ) as this =

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    /// Buffer line size in bytes. It must be multiple of the block size.
    let m_BufferLineSize =
        argBufferLineBlocks * m_BlockSize

    /// Block count
    let m_BlockCount =
        // If the configured size is not multiple of the blocksize, it truncated to multiple of the blocksize.
        let w1 = m_Config.BytesCount / m_BlockSize
        // If the configured size is too large to be stored in single array, it truncated to the maximum size.
        let struct( d, r ) = Math.DivRem( w1 * m_BlockSize, m_BufferLineSize )
        let d2 = if r > 0UL then d + 1UL else d
        if ( uint64 Array.MaxLength ) < d2 then
            ( uint64 Array.MaxLength ) * m_BufferLineSize / m_BlockSize
        else
            w1

    /// Memory buffer
    let m_Buffer =
        let struct( d, r ) = Math.DivRem( m_BlockCount * m_BlockSize, m_BufferLineSize )
        let d2 = if r > 0UL then d + 1UL else d
        // d2 never exceeds Array.MaxLength.
        Array.zeroCreate< byte[] >( int d2 )

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
            g.Gen2( loginfo, "MemBufferMedia", "" )
        )

    interface IMedia with

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override _.Terminate() : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "MemBufferMedia.Terminate." )
                )
            // release all of buffers
            for i = 0 to m_Buffer.Length - 1 do
                m_Buffer.[i] <- null
    
        // ------------------------------------------------------------------------
        // Implementation of Initialize method
        override _.Initialize() : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "MemBufferMedia.Initialize." )
                )

            // allocate all of buffers
            MemBufferMedia.InitializeBuffer m_Buffer m_BlockCount m_BlockSize m_BufferLineSize

        // ------------------------------------------------------------------------
        // Implementation of Finalize method
        override _.Closing() : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "MemBufferMedia.Closing." )
                )

            // release all of buffers
            for i = 0 to m_Buffer.Length - 1 do
                m_Buffer.[i] <- null

        // ------------------------------------------------------------------------
        // Implementation of TestUnitReady method
        override _.TestUnitReady( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : ASCCd voption =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "MemBufferMedia.TestUnitReady." )
                )
            ValueNone    // Always returns true

        // ------------------------------------------------------------------------
        // Implementation of ReadCapacity method
        override _.ReadCapacity( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : uint64 =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "MemBufferMedia.ReadCapacity." )
                )
            m_BlockCount

        // ------------------------------------------------------------------------
        // Implementation of Read method
        override this.Read
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( argLBA : uint64 )
            ( buffer : ArraySegment<byte> )
            : Task<int> =

            let sw = new Stopwatch()
            sw.Start()
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )

            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "MemBufferMedia.Read." ) )

            let readBytesLength_u64 = uint64 buffer.Count
            let readpos_u64 = argLBA * m_BlockSize
            //let mediaSize = m_BlockCount * m_BlockSize

            // Check specified range is in media file.
            if Functions.CheckAccessRange argLBA readBytesLength_u64 m_BlockCount m_BlockSize |> not then
                let errmsg =
                    sprintf
                        "Out of media capacity. BlockSize=%d, TotalBlockCount=%d, RequestedLBA=%d, RequestedBytesCount=%d"
                        m_BlockSize
                        m_BlockCount
                        argLBA
                        buffer.Count
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )
                raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )

            task {
                let struct( startLinePos, startLineOffset ) =
                    Math.DivRem( readpos_u64, m_BufferLineSize )
                let struct( endLinePos, endLineOffset ) =
                    let struct( d, r ) =
                        Math.DivRem( readpos_u64 + readBytesLength_u64, m_BufferLineSize )
                    if r > 0UL then
                        struct( d, r )
                    elif d > 0UL then
                        struct( d - 1UL, m_BufferLineSize )
                    else struct( 0UL, 0UL )

                // startLinePos and endLinePos never exceeds Array.MaxLength.
                assert( startLinePos < uint64 Array.MaxLength )
                assert( endLinePos < uint64 Array.MaxLength )

                if startLinePos = endLinePos then
                    this.RefBuffer startLinePos startLineOffset buffer.Array ( uint64 buffer.Offset ) ( uint64 buffer.Count )
                else
                    this.RefBuffer startLinePos startLineOffset buffer.Array ( uint64 buffer.Offset ) ( m_BufferLineSize - startLineOffset )
                    for i = startLinePos + 1UL to endLinePos do
                        let s = ( i - startLinePos ) * m_BufferLineSize - startLineOffset
                        let wl = if i < endLinePos then m_BufferLineSize else endLineOffset
                        this.RefBuffer i 0UL buffer.Array ( uint64 buffer.Offset + s ) wl

                sw.Stop()
                let d = DateTime.UtcNow
                m_ReadBytesCounter.AddCount d ( int64 buffer.Count )
                m_ReadTickCounter.AddCount d sw.ElapsedTicks 
                return buffer.Count
            }

        // ------------------------------------------------------------------------
        // Implementation of Write method
        override this.Write
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( argLBA : uint64 )
            ( offset : uint64 )
            ( data : ArraySegment<byte> )
            : Task<int> =

            assert( offset < m_BlockSize )
            let sw = new Stopwatch()
            sw.Start()
            let loginfo = struct ( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
    
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "MemBufferMedia.Write." ) )

            let writeBytesLength_u64 = uint64 data.Count
            let writepos_u64 = argLBA * m_BlockSize + offset
            let mediaSize = m_BlockCount * m_BlockSize

            // Check specified range is in media file.
            if Functions.CheckAccessRange argLBA ( writeBytesLength_u64 + offset ) m_BlockCount m_BlockSize |> not then
                let errmsg = 
                    sprintf
                        "Out of media capacity. BlockSize=%d, TotalBlockCount=%d, RequestedLBA=%d, RequestedOffset=%d, RequestedBytesCount=%d"
                        m_BlockSize
                        m_BlockCount
                        argLBA
                        offset
                        writeBytesLength_u64
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )
                raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )

            task {
                let struct( startLinePos, startLineOffset ) =
                    Math.DivRem( writepos_u64, m_BufferLineSize )
                let struct( endLinePos, endLineOffset ) =
                    let struct( d, r ) =
                        Math.DivRem( writepos_u64 + writeBytesLength_u64, m_BufferLineSize )
                    if r > 0UL then
                        struct( d, r )
                    elif d > 0UL then
                        struct( d - 1UL, m_BufferLineSize )
                    else struct( 0UL, 0UL )

                // startLinePos and endLinePos never exceeds Array.MaxLength.
                assert( startLinePos < uint64 Array.MaxLength )
                assert( endLinePos < uint64 Array.MaxLength )

                try
                    if startLinePos = endLinePos then
                        this.WriteBuffer data.Array ( uint64 data.Offset ) startLinePos startLineOffset ( uint64 data.Count )
                    else
                        this.WriteBuffer data.Array ( uint64 data.Offset ) startLinePos startLineOffset ( m_BufferLineSize - startLineOffset )
                        for i = startLinePos + 1UL to endLinePos do
                            let s = ( i - startLinePos ) * m_BufferLineSize - startLineOffset
                            let wl = if i < endLinePos then m_BufferLineSize else endLineOffset
                            this.WriteBuffer data.Array ( uint64 data.Offset + s ) i 0UL wl
                with
                | :? OutOfMemoryException ->
                    let errmsg = sprintf "Out of memory. Requested media size = 0x%016X" mediaSize
                    HLogger.ACAException( loginfo, SenseKeyCd.MEDIUM_ERROR, ASCCd.INSUFFICIENT_RESOURCES, errmsg )
                    raise <| SCSIACAException ( source, true, SenseKeyCd.MEDIUM_ERROR, ASCCd.INSUFFICIENT_RESOURCES, errmsg )

                sw.Stop()
                let d = DateTime.UtcNow
                m_WrittenBytesCounter.AddCount d ( int64 data.Count )
                m_WriteTickCounter.AddCount d sw.ElapsedTicks
                return data.Count
            }

        // ------------------------------------------------------------------------
        // Implementation of Format method
        override _.Format( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : Task<unit> =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "MemBufferMedia.Format." )
                )
            task {
                // release all of buffers
                for i = 0 to m_Buffer.Length - 1 do
                    m_Buffer.[i] <- null

                // GC
                GC.Collect()

                // re-allocate buffers
                MemBufferMedia.InitializeBuffer m_Buffer m_BlockCount m_BlockSize m_BufferLineSize
            }

        // ------------------------------------------------------------------------
        // Notify logical unit reset.
        override _.NotifyLUReset ( initiatorTaskTag : ITT_T voption ) ( source : CommandSourceInfo voption ) : unit =
            // Nothing to do
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, source, initiatorTaskTag, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "MemBufferMedia.NotifyLUReset." )
                )

        // ------------------------------------------------------------------------
        // Media control request.
        override _.MediaControl ( request : MediaCtrlReq.T_Request ) : Task<MediaCtrlRes.T_Response> =
            task {
                return MediaCtrlRes.U_Unexpected( "MemBuffer media does not support media controls." )
            }

        // ------------------------------------------------------------------------
        // Get block count
        override _.BlockCount = m_BlockCount

        // ------------------------------------------------------------------------
        // Get write protect
        override _.WriteProtect =
            false

        // ------------------------------------------------------------------------
        // Media index ID
        override _.MediaIndex = m_Config.IdentNumber

        // ------------------------------------------------------------------------
        // String that descripts this media.
        override _.DescriptString =
            sprintf "Memory buffer Media(Configured Size=%d, Allocated Size=%d)" m_Config.BytesCount ( m_BlockCount * m_BlockSize )

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

    /// <summary>
    ///  Copy m_Buffer containts to specified bytes array.
    /// </summary>
    /// <param name="lineIdx">
    ///  Index number of m_Buffer.
    /// </param>
    /// <param name="srcPos">
    ///  Copy source index of m_Buffer.[ lineIdx ]
    /// </param>
    /// <param name="dstBuf">
    ///  Copy destination buffer.
    /// </param>
    /// <param name="dstPos">
    ///  Copy destination index of dstBuf.
    /// </param>
    /// <param name="length">
    ///  Byte count to be copied.
    /// </param>
    /// <remarks>
    ///  If specified m_Buffer.[ lineIdx ] is no allocated, zeros are written to the destination bytes array.
    /// </remarks>
    member private _.RefBuffer ( lineIdx : uint64 ) ( srcPos : uint64 ) ( dstBuf : byte[] ) ( dstPos : uint64 ) ( length : uint64 ) : unit =
        if m_Buffer.[ int lineIdx ] = null then
            Array.Clear( dstBuf, int dstPos, int length )
        else
            Array.blit m_Buffer.[ int lineIdx ]  ( int srcPos ) dstBuf ( int dstPos ) ( int length )

    /// <summary>
    ///  Copy bytes array containts to m_Buffer.
    /// </summary>
    /// <param name="srcBuf">
    ///  Copy source bytes array.
    /// </param>
    /// <param name="srcPos">
    ///  Copy source index of srcBuf.
    /// </param>
    /// <param name="lineIdx">
    ///  Index number of m_Buffer.
    /// </param>
    /// <param name="dstPos">
    ///  Copy destination index of m_Buffer.[ lineIdx ].
    /// </param>
    /// <param name="length">
    ///  Byte count to be copied.
    /// </param>
    /// <remarks>
    ///  If specified m_Buffer.[ lineIdx ] is no allocated, newly allocated memory buffer.
    /// </remarks>
    member private _.WriteBuffer ( srcBuf : byte[] ) ( srcPos : uint64 ) ( lineIdx : uint64 ) ( dstPos : uint64 ) ( length : uint64 ) : unit =
        Array.blit srcBuf ( int srcPos ) m_Buffer.[ int lineIdx ] ( int dstPos ) ( int length )

    /// Reffer buffer line size
    member _.BufferLineSize = m_BufferLineSize

    static member InitializeBuffer ( buffer : byte[][] ) ( blockCount : uint64 ) ( blockSize : uint64 ) ( bufferLineSize : uint64 ) : unit =
        for i = 0 to buffer.Length - 1 do
            let reqBufSize =
                if i = buffer.Length - 1 then
                    let r = ( blockCount * blockSize ) % bufferLineSize
                    if r > 0UL then
                        r
                    else
                        bufferLineSize
                else
                    bufferLineSize
            buffer.[i] <- Array.zeroCreate< byte > ( int reqBufSize )
