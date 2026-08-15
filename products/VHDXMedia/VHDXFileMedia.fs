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

    let mutable m_FileAccessors, m_Structures = 
        let fa = FileAccessor( m_Config.FileName, m_Multiplicity, m_Config.WriteProtect )
        VhdxReader.ReadAllStructures fa
        |> Functions.RunTaskSynchronously   // Due to implementation constraints, the threads here must be synchronized.
        |> Array.unzip

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
                do! m_Lock.RLock()
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
                    let readpos_u64 = ( blkcnt_me.toUInt64 argLBA ) * blockSize_u64
                    let mediaBlockCount = m_VirtualDiskSize / blockSize_u64

                    // Check specified range is in media.
                    if Functions.CheckAccessRange argLBA readBytesLength_u64 mediaBlockCount blockSize_u64 |> not then
                        let errmsg =
                            sprintf
                                "Out of media capacity. BlockSize=%d, TotalBlockCount=%d, RequestedLBA=%d, RequestedBytesCount=%d"
                                blockSize_u64 mediaBlockCount argLBA buffer.Count
                        HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )
                        raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )

                    // Check limit of this module
                    if readpos_u64 >= 0x8000000000000000UL || readBytesLength_u64 >= 0x0000000080000000UL then
                        let errmsg =
                            sprintf
                                "Out of module limits. BlockSize=%d, TotalBlockCount=0x%016X, RequestedLBA=0x%016X, RequestedBytesCount=%d"
                                blockSize_u64 mediaBlockCount argLBA buffer.Count
                        HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.CONFIGURATION_OF_INCAPABLE_LOGICAL_UNITS_FAILED, errmsg )
                        raise <| SCSIACAException ( source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.CONFIGURATION_OF_INCAPABLE_LOGICAL_UNITS_FAILED, errmsg )

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
        override _.Write
            ( initiatorTaskTag : ITT_T )
            ( source : CommandSourceInfo )
            ( argLBA : BLKCNT64_T )
            ( offset : uint64 )
            ( data : ArraySegment<byte> )
            : Task<int32> =

            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_INTERFACE_CALLED, fun g ->
                    let loginfo = struct( m_ObjID, ValueSome source, ValueSome initiatorTaskTag, ValueSome m_LUN )
                    g.Gen1( loginfo, "VHDXFileMedia.Read." )
                )

            task {
                return 0
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
