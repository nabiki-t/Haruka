//=============================================================================
// Haruka Software Storage.
// VHDXFileMedia.fs : Defines VHDXMedia class.
// VHDXFileMedia class implement VHDX file block device functionality.
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
open Haruka.Media.VhdxUtil

//=============================================================================
// Class implementation

/// <summary>
///  VHDXFileMedia class definition.
/// </summary>
/// <param name="m_StatusMaster">
///  Interface of StatusMaster instance.
/// </param>
/// <param name="m_Config">
///  Configuration of this VHDX media
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
type VHDXFileMedia
    (
        m_StatusMaster : IStatus,
        m_Config : TargetGroupConf.T_VHDXFile,
        m_Killer : IKiller,
        m_LUN : LUN_T,
        m_Multiplicity : uint32
    ) as this =

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    /// Reader-Writer lock object
    let m_Lock = RWLock()

    let mutable m_FileAccessors : FileAccessor [] = [||]
    let mutable m_Structures : VhdxStructures [] = [||]

    /// VHDX log manager object
    let m_LogManager =
        let fa = FileAccessor( m_Config.FileName, m_Multiplicity, m_Config.WriteProtect )
        task {
            // load al of VHDX files and flush logs.
            let! all = VhdxReader.ReadAllStructures fa
            let lastidx = all.Length - 1
            let ffa, fvs = all.[ lastidx ]
            let! verhd1 =
                if fvs.Log.Length > 0 && not m_Config.WriteProtect then
                    VhdxChecker.FlushLog ffa fvs
                else
                    Task.FromResult fvs.LoadedVarHeader

            let lm = VhdxLogManager( m_LUN, ffa, fvs.ImmHeader, verhd1 )

            // Although this is earlier than the intended timing,
            // io opend in read/write mode, data write GUID is updated at this point.
            if m_Config.WriteProtect |> not then
                do! lm.UpdateDataWriteGuid()

            let allfa, allvs = all |> Array.unzip
            m_FileAccessors <- allfa
            m_Structures <- allvs
            return lm
        }
        |> Functions.RunTaskSynchronously   // Due to implementation constraints, the threads here must be synchronized.



    /// The block size and the virtual disk size
    /// Since the constructor executes synchronously, there is no need to acquire a lock here.
    /// Therefore, information that does not change during operation is retrieved in advance.
    let m_BlockSize, m_VirtualDiskSize =
        let vdi = m_Structures.[0].VDI
        ( vdi.LogicalSectorSize, vdi.VirtualDiskSize )

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
            let msg = ""
            g.Gen2( loginfo, "VHDXFileMedia", msg )
        )

    interface IMedia with

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override this.Terminate() : unit =
            let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "VHDXFileMedia.Terminate." ) )

            this.CloseAllFiles()
            |> Functions.RunTaskSynchronously   // Due to implementation constraints, the threads here must be synchronized.

            HLogger.Trace( LogID.I_FILE_CLOSED, fun g -> g.Gen1( loginfo, "" ) )

    
        // ------------------------------------------------------------------------
        // Implementation of Initialize method
        override _.Initialize() : unit =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "VHDXFileMedia.Initialize." )
                )
            // Nothing to do

        // ------------------------------------------------------------------------
        // Implementation of Finalize method
        override _.Closing() : unit =
            let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "VHDXFileMedia.Closing." ) )

            this.CloseAllFiles()
            |> Functions.RunTaskSynchronously   // Due to implementation constraints, the threads here must be synchronized.

            HLogger.Trace( LogID.I_FILE_CLOSED, fun g -> g.Gen1( loginfo, "" ) )

        // ------------------------------------------------------------------------
        // Implementation of TestUnitReady method
        override _.TestUnitReady( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : ASCCd voption =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueSome source, ValueSome initiatorTaskTag, ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "VHDXFileMedia.TestUnitReady." )
                )
            ValueNone    // Always returns true

        // ------------------------------------------------------------------------
        // Implementation of ReadCapacity method
        override _.ReadCapacity( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : uint64 =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueSome source, ValueSome initiatorTaskTag, ValueSome m_LUN )
                    g.Gen1( loginfo, "VHDXFileMedia.ReadCapacity." )
                )
            m_VirtualDiskSize / ( Blocksize.toUInt64 m_BlockSize )

        // ------------------------------------------------------------------------
        // Implementation of Read method
        override _.Read
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( argLBA : BLKCNT64_T )
            ( buffer : ArraySegment<byte> )
            : Task<int32> =

            let loginfo = struct( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "VHDXFileMedia.Read." ) )

            task {
                do! m_Lock.WLock()
                try
                    let sw = new Stopwatch()
                    sw.Start()

                    let allStructures = m_Structures
                    let allFileAccessors = m_FileAccessors
                    let lastIdx = allStructures.Length - 1
                    let curFA = allFileAccessors.[ lastIdx ]
                    let curStr = allStructures.[ lastIdx ]

                    let readBytesLength_u64 = uint64 buffer.Count
                    let blockSize_u64 = Blocksize.toUInt64 m_BlockSize
                    let mediaBlockCount = m_VirtualDiskSize / blockSize_u64

                    // Check specified range is in media.
                    if Functions.CheckAccessRange argLBA readBytesLength_u64 mediaBlockCount blockSize_u64 |> not then
                        let errmsg =
                            sprintf
                                "Out of media capacity. BlockSize=%d, TotalBlockCount=%d, RequestedLBA=%d, RequestedBytesCount=%d"
                                blockSize_u64 mediaBlockCount argLBA buffer.Count
                        HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )
                        raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )

                    // Sector count (number of logical blocks to read)
                    let totalSectors = ( readBytesLength_u64 + ( blockSize_u64 - 1UL ) ) / blockSize_u64

                    // payload block size and logical sector size for the last (child) file
                    let childPBSize = uint64 curStr.VDI.PayloadBlockSize
                    let blockSize_u64 = Blocksize.toUInt64 curStr.VDI.LogicalSectorSize
                    let secCntInPB = childPBSize / blockSize_u64

                    let loop ( sectorsRead : uint64 ) : Task<struct( bool * uint64 )> = task {

                        if sectorsRead < totalSectors then
                            let curLBA = blkcnt_me.ofUInt64 ( blkcnt_me.toUInt64 argLBA + uint64 sectorsRead )

                            // Identify payload block index within child file and sector index inside it
                            let struct( pbIdx, secIdxInPB ) = VhdxCommons.LBAtoPayloadBlockIndex curLBA curStr
                            let secIdx = blkcnt_me.toUInt32 secIdxInPB |> uint64

                            // Determine how many sectors remain in this payload block
                            let remainInPB = secCntInPB - secIdx
                            let remainTotal = totalSectors - sectorsRead
                            let takeSectors = min remainInPB remainTotal

                            match curStr.BAT.Payloads.[ int pbIdx ].State with
                            | PayloadUndefined
                            | PayloadZero
                            | PayloadUnapped ->
                                // Entire payload block range can be zero-filled
                                let bytesToZero = takeSectors * blockSize_u64
                                Array.Clear( buffer.Array, buffer.Offset + int ( sectorsRead * blockSize_u64 ), int bytesToZero )
                                return struct( true, sectorsRead + takeSectors )

                            | PayloadFullyPresent ->
                                // All data for this payload exists in child file; read as a single request
                                let pbEntry = curStr.BAT.Payloads.[ int pbIdx ]
                                let posInFile = pbEntry.FileOffset + secIdx * blockSize_u64
                                let bytesToRead = takeSectors * blockSize_u64
                                let dstOffset = buffer.Offset + int ( sectorsRead * blockSize_u64 )
                                do! curFA.ReadWithPseudoLimit curStr.LastFileSize posInFile ( ArraySegment( buffer.Array, dstOffset, int bytesToRead ) )
                                return struct( true, sectorsRead + takeSectors )

                            | PayloadNotPresent
                            | PayloadPartiallyPresent ->
                                // Must resolve per logical block (may involve parent files)
                                for i in 0UL .. takeSectors - 1UL do
                                    let lba = blkcnt_me.ofUInt64 ( blkcnt_me.toUInt64 argLBA + sectorsRead + i )
                                    match VhdxCommons.ResolvLBA lba allStructures with
                                    | ValueSome( struct( fsidx, fpos ) ) ->
                                        let dstOffset = buffer.Offset + int ( ( sectorsRead + i ) * blockSize_u64 )
                                        let bytesAvail = buffer.Count - dstOffset
                                        let readCount = min ( int blockSize_u64 ) bytesAvail
                                        do! allFileAccessors.[fsidx].ReadWithPseudoLimit curStr.LastFileSize fpos ( ArraySegment( buffer.Array, dstOffset, readCount ) )
                                    | _ ->
                                        let dstOffset = buffer.Offset + int ( ( sectorsRead + i ) * blockSize_u64 )
                                        let bytesAvail = buffer.Count - dstOffset
                                        let zeroCount = min ( int blockSize_u64 ) bytesAvail
                                        Array.Clear( buffer.Array, dstOffset, zeroCount )
                                return struct( true, sectorsRead + takeSectors )
                        else
                            return struct( false, 0UL )
                    }
                    let! _ = Functions.loopAsyncWithState loop 0UL

                    sw.Stop()
                    let d = DateTime.UtcNow
                    m_ReadBytesCounter.AddCount d ( int64 buffer.Count )
                    m_ReadTickCounter.AddCount d sw.ElapsedTicks

                    do! m_Lock.Release()
                with
                | _ ->
                    do! m_Lock.Release()

                return 0
            }

        // ------------------------------------------------------------------------
        // Implementation of Write method
        override this.Write
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( argLBA : BLKCNT64_T )
            ( offset : uint64 )
            ( data : ArraySegment<byte> )
            : Task<int32> =

            let loginfo = struct( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "VHDXFileMedia.Write." ) )

            // Check read only or not
            if m_Config.WriteProtect then
                let errmsg = "Write protected."
                HLogger.ACAException( loginfo, SenseKeyCd.DATA_PROTECT, ASCCd.WRITE_PROTECTED, errmsg )
                raise <| SCSIACAException ( source, true, SenseKeyCd.DATA_PROTECT, ASCCd.WRITE_PROTECTED, errmsg )

            task {
                do! m_Lock.RLock()
                try
                    let sw = new Stopwatch()
                    sw.Start()

                    let allStructures = m_Structures
                    let allFileAccessors = m_FileAccessors
                    let lastIdx = allStructures.Length - 1
                    let curFA = allFileAccessors.[ lastIdx ]
                    let curStr = allStructures.[ lastIdx ]

                    let writeBytesLength_u64 = uint64 data.Count
                    let blockSize_u64 = Blocksize.toUInt64 m_BlockSize
                    let writeStartBytePos = uint64 argLBA * blockSize_u64 + offset
                    let writeStartBlockPos = writeStartBytePos / blockSize_u64 |> blkcnt_me.ofUInt64
                    let writeEndBytePos = writeStartBytePos + writeBytesLength_u64
                    let writeEndBlockPos = ( writeEndBytePos + blockSize_u64 - 1UL ) / blockSize_u64 |> blkcnt_me.ofUInt64
                    let writeBlockLength = writeEndBlockPos - writeStartBlockPos

                    // Check specified range is in media file.
                    if VHDXFileMedia.CheckWriteRange m_VirtualDiskSize blockSize_u64 argLBA offset writeBytesLength_u64 then
                        let errmsg = 
                            sprintf
                                "Out of media capacity. BlockSize=%d, VirtualDiskSize=%d, RequestedLBA=%d, RequestedOffset=%d, RequestedBytesCount=%d"
                                blockSize_u64 m_VirtualDiskSize argLBA offset writeBytesLength_u64
                        HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )
                        raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )

                    if this.CheckAlreadyAllocated curStr writeStartBlockPos writeBlockLength |> not then
                        // the payload blocks to be write should be allocate.
                        do! m_Lock.Release()
                        do! m_Lock.WLock()

                        let struct( updatedBAT4K, requiredFileSize ) =
                            VhdxWriter.AllocatePayloadBlock curStr writeStartBlockPos writeBlockLength

                        let updatedSB4K =
                            if curStr.VDI.HasParent then
                                VhdxWriter.UpdateSectorBitmap curStr updatedBAT4K writeStartBlockPos writeBlockLength
                                |> Seq.map ( fun itr -> struct( itr.Key, itr.Value ) )
                                |> Seq.toArray
                            else
                                Array.empty

                        do! m_LogManager.UpdateBATEntries curStr ( updatedBAT4K |> Seq.toArray ) requiredFileSize
                        do! m_LogManager.UpdateGenericStructesData curStr updatedSB4K

                    // Write the data to allocated payload blocks.
                    do! VHDXFileMedia.WriteData curFA curStr writeStartBytePos data

                    sw.Stop()
                    let d = DateTime.UtcNow
                    m_WrittenBytesCounter.AddCount d ( int64 data.Count )
                    m_WriteTickCounter.AddCount d sw.ElapsedTicks

                    do! m_Lock.Release()
                with
                | _ ->
                    do! m_Lock.Release()

                return data.Count
            }

        // ------------------------------------------------------------------------
        // Implementation of Format method
        override _.Format( initiatorTaskTag : ITT_T ) ( source : CommandSourceInfo ) : Task<unit> =
            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueSome( source ), ValueSome( initiatorTaskTag ), ValueSome( m_LUN ) )
                    g.Gen1( loginfo, "VHDXFileMedia.Format." )
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
                    g.Gen1( loginfo, "VHDXFileMedia.NotifyLUReset." )
                )
            ( this :> IMedia ).Closing()

        // ------------------------------------------------------------------------
        // Media control request.
        override _.MediaControl ( request : MediaCtrlReq.T_Request ) : Task<MediaCtrlRes.T_Response> =
            task {
                let loginfo = struct( m_ObjID, ValueNone, ValueNone, ValueSome m_LUN )
                if HLogger.IsVerbose then
                    HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g -> g.Gen1( loginfo, "VHDXFileMedia.MediaControl." ) )

                return MediaCtrlRes.U_Unexpected( "Plain file media does not support media controls." )
            }

        // ------------------------------------------------------------------------
        // Get block count
        override _.BlockCount = m_VirtualDiskSize / ( Blocksize.toUInt64 m_BlockSize )

        // ------------------------------------------------------------------------
        // Get block size
        override _.BlockSize = m_BlockSize

        // ------------------------------------------------------------------------
        // Get write protect
        override _.WriteProtect = m_Config.WriteProtect

        // ------------------------------------------------------------------------
        // Media index ID
        override _.MediaIndex = m_Config.IdentNumber

        // ------------------------------------------------------------------------
        // String that descripts this media.
        override _.DescriptString =
            sprintf "VHDX File Media(File Name=%s)" m_Config.FileName

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
        override _.GetSubMedia() : IMedia list =
            // A VHDX file is a peripheral medium; no further child entities can exist beyond it.
            []


    /// <summary>
    ///  Close all of VHDX files.
    /// </summary>
    member private _.CloseAllFiles() : Task =
        task {
            do! m_Lock.WLock()
            try
                for fs in m_FileAccessors do
                    fs.Close()
                m_FileAccessors <- [||]
                m_Structures <- [||]
                do! m_Lock.Release()
            with
            | _ ->
                do! m_Lock.Release()
        }

    /// <summary>
    ///  Check whether the necessary area for writing user data has been allocated.
    /// </summary>
    /// <param name="structures">
    ///  The VHDX structure data.
    /// </param>
    /// <param name="lba">
    ///  The starting position for writing data.
    /// </param>
    /// <param name="blkCnt">
    ///  Length of data to write.
    /// </param>
    /// <returns>
    ///  Returns true if no additional memory allocation is required.
    /// </returns>
    member private _.CheckAlreadyAllocated ( structures : VhdxStructures ) ( lba : BLKCNT64_T ) ( blkCnt : BLKCNT64_T ) : bool =
        
        let blockSize = Blocksize.toUInt32 structures.VDI.LogicalSectorSize
        let pbBlkCnt = structures.VDI.PayloadBlockSize / blockSize |> uint64 |> blkcnt_me.ofUInt64

        let rec loop ( wpos : BLKCNT64_T ) : bool =
            if wpos < lba + blkCnt then
                
                let struct( pbidx, secidb ) = VhdxCommons.LBAtoPayloadBlockIndex wpos structures
                let pb = structures.BAT.Payloads.[ int32 pbidx ]
                match pb.State with
                | PayloadNotPresent
                | PayloadUndefined
                | PayloadZero
                | PayloadUnapped ->
                    // Need to allocate
                    false
                | PayloadFullyPresent ->
                    // allocated
                    loop ( wpos + pbBlkCnt - ( secidb |> uint64 |> blkcnt_me.ofUInt64 ) )
                | PayloadPartiallyPresent ->
                    // must be check sector bitmap
                    let struct( sbIdx, bytePos, bitPos ) = VhdxCommons.LBAtoSectorBitmapIndex lba structures
                    let sbEntry = structures.BAT.SectorBitmap.[ int32 sbIdx ]
                    let sb = sbEntry.Bitmap
                    let bitValue = ( sb.[ int32 bytePos ] >>> ( int32 bitPos ) ) &&& 1uy
                    if bitValue <> 0uy then
                        // it is already marked as in use
                        loop ( wpos + blkcnt_me.ofUInt64 1UL )
                    else
                        // must be update sector bitmap
                        false
            else
                // all range had been allocated
                true
        loop lba

    /// <summary>
    ///  Check whether the area to be written to the data is within the media size limits.
    /// </summary>
    /// <param name="mediaSize">
    ///  Specify the media size in bytes.
    /// </param>
    /// <param name="blockSize">
    ///  Specify the block length in bytes.
    /// </param>
    /// <param name="lba">
    ///  LBA requested as the write start position.
    /// </param>
    /// <param name="offset">
    ///  The offset for the start position of data writing. Writing is performed to ( LBA * blockSize ) + offset.
    /// </param>
    /// <param name="len">
    ///  Specifies the number of bytes of data to be written.
    /// </param>
    /// <returns>
    ///  Returns true if the area to be updated is within the media length limit.
    /// </returns>
    static member private CheckWriteRange ( mediaSize : uint64 ) ( blockSize : uint64 ) ( lba : BLKCNT64_T ) ( offset : uint64 ) ( len : uint64 ) : bool =
        let posa = uint64 lba * blockSize
        let posb = posa + offset
        let posc = posb + len
        ( posb <= mediaSize && posa < posb && offset < posb && posc <= mediaSize && posb < posc && len < posc )

    /// <summary>
    ///  Write data to the media. It is assumed that all necessary areas have already been allocated.
    /// </summary>
    /// <param name="vhdxfs">
    ///  The FileAccessor object for the CHDX file.
    /// </param>
    /// <param name="structures">
    ///  The VHDX structures data/
    /// </param>
    /// <param name="pos">
    ///  Starting position for writing data. Specifies the byte location on the virtual disk image.
    /// </param>
    /// <param name="data">
    ///  Data to write.
    /// </param>
    static member private WriteData ( vhdxfs : FileAccessor ) ( structures : VhdxStructures ) ( pos : uint64 ) ( data : ArraySegment<byte> ) : Task =
        task {

            let blockSize = structures.VDI.LogicalSectorSize |> Blocksize.toUInt64
            let writeBytesLength_u64 = uint64 data.Count

            // Write only the requested bytes. No payload-block-sized buffer is needed.
            let mutable bytesWritten = 0UL
            while bytesWritten < writeBytesLength_u64 do
                let currentByte = pos + bytesWritten
                let struct( currentLBA, offsetInSector ) = Math.DivRem( currentByte, blockSize )
                let struct( payloadIndex, sectorIndex ) = VhdxCommons.LBAtoPayloadBlockIndex ( blkcnt_me.ofUInt64 currentLBA ) structures
                let payload = structures.BAT.Payloads.[ int32 payloadIndex ]

                let offsetInPayload = ( uint64 sectorIndex * blockSize ) + offsetInSector
                let bytesToWrite = min ( uint64 structures.VDI.PayloadBlockSize - offsetInPayload ) ( writeBytesLength_u64 - bytesWritten )
                let fileOffset = payload.FileOffset + offsetInPayload
                let sourceOffset = data.Offset + int32 bytesWritten
                do! vhdxfs.Write fileOffset ( ArraySegment( data.Array, sourceOffset, int bytesToWrite ) )
                bytesWritten <- bytesWritten + bytesToWrite
        }
