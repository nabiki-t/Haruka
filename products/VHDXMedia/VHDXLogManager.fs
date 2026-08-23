
namespace Haruka.Media.VhdxUtil

open System
open System.Threading.Tasks

open Haruka.Commons



/// <summary>
/// Pending VHDX log transaction.
///
/// The updated sector data is retained here until Flush is called.
/// The data is therefore not required to be reconstructed by reading the
/// log area when the normal in-process Flush operation is performed.
///
/// Recovery after an unexpected process termination remains the
/// responsibility of the normal VHDX log recovery path.
/// </summary>
type private PendingLogEntry = {
    /// Offset of this entry within the VHDX log area.
    LogOffset : uint32;

    /// Serialized VHDX log entry.
    LogEntry : byte[];

    /// Actual metadata updates represented by this log entry.
    Updates : struct( SEC4K_T * byte[] )[];

    /// File size required before the updates represented by this entry are
    /// committed.
    RequiredFileSize : uint64;
}


/// <summary>
/// Manages VHDX metadata logging and the effective VHDX header.
///
/// This class is not thread-safe. The caller must serialize all VHDX
/// metadata update operations.
///
/// The VHDX header held by this class is the authoritative in-memory header.
/// Other components that require the current header must obtain it through
/// the Header property.
///
/// This class separates the following operations:
///
///   * append metadata updates to the VHDX log;
///   * automatically flush the active log when there is insufficient space;
///   * explicitly flush the active log;
///   * update the actual BAT or Sector Bitmap sectors during Flush.
/// </summary>
type VhdxLogManager ( m_FA : FileAccessor, initialHeader : VhdxHeader ) =

    // ------------------------------------------------------------------------
    // Current header state
    // ------------------------------------------------------------------------

    /// The authoritative current VHDX header.
    let mutable currentHeader = initialHeader

    let m_Log4KSecCount = VhdxWriter.Max4KSectorCountFromLogCapacity initialHeader.LogLength

    // ------------------------------------------------------------------------
    // Current log transaction state
    // ------------------------------------------------------------------------

    /// GUID of the currently active log.
    ///
    /// Guid.Empty means that no active log transaction exists.
    let mutable activeLogGuid = Guid.Empty


    /// Sequence number assigned to the next log entry.
    ///
    /// The current implementation starts a new active log sequence at 1 and
    /// increments this value for every log entry appended to that sequence.
    let mutable nextLogSequenceNumber = 1UL


    /// Next write position within the VHDX log area.
    let mutable nextLogWriteOffset = 0u

    /// Pending entries that have been written to the VHDX log but have not
    /// yet been reflected into the actual metadata sectors.
    let pendingEntries = ResizeArray<PendingLogEntry>()

    /// Largest file size required by the active transaction.
    let mutable requiredFileSize = m_FA.GetFileSize()


    // ------------------------------------------------------------------------
    // Public BAT append
    // ------------------------------------------------------------------------

    /// <summary>
    /// Append updated BAT sectors to the active VHDX log.
    ///
    /// The BAT sectors are not written to their actual locations by this
    /// method. They are committed by Flush.
    ///
    /// If the current log does not have enough space, it is flushed
    /// automatically before the remaining BAT updates are appended.
    /// </summary>
    member this.AppendBAT ( structures : VhdxStructures ) ( sec4Ks : SEC4K_T[] ) ( requiredFileSizeAfterCommit : uint64 ) : Task =
        task {
            let mutable index = 0u

            while index < uint32 sec4Ks.Length do
                let count = min m_Log4KSecCount ( uint32 sec4Ks.Length - index )

                let updates = Array.zeroCreate< struct( SEC4K_T * byte[] ) >( int count )
                for i in 0u .. count - 1u do
                    let sectorNumber = sec4Ks.[ int ( index + i ) ]
                    let data = VhdxWriter.CreateBATEntryTableFrom4KSectorNumber structures.BAT sectorNumber
                    updates.[ int i ] <- struct ( sectorNumber, data )

                do! this.appendEntry structures updates requiredFileSizeAfterCommit
                index <- index + count
        }


    // ------------------------------------------------------------------------
    // Public Sector Bitmap append
    // ------------------------------------------------------------------------

    /// <summary>
    /// Append updated Sector Bitmap sectors to the active VHDX log.
    ///
    /// The actual Sector Bitmap sectors are not written by this method.
    /// They are committed by Flush.
    /// </summary>
    member this.AppendSectorBitmap ( structures : VhdxStructures ) ( sec4Ks : struct( SEC4K_T * ArraySegment<byte> )[] ) : Task =
        task {
            let requiredFileSizeAfterCommit = max structures.LastFileSize ( m_FA.GetFileSize() )
            let mutable index = 0u

            while index < uint32 sec4Ks.Length do
                let count = min m_Log4KSecCount ( uint32 sec4Ks.Length - index )
                let updates = Array.zeroCreate< struct( SEC4K_T * byte[] ) >( int count )

                for i in 0u .. count - 1u do
                    let struct ( sectorNumber, dataSegment ) = sec4Ks.[ int ( index + i ) ]
                    let data = dataSegment.ToArray()
                    updates.[ int i ] <- struct ( sectorNumber, data )

                do! this.appendEntry structures updates requiredFileSizeAfterCommit
                index <- index + count
        }


    /// <summary>
    /// Commit every metadata update represented by the active VHDX log and
    /// invalidate the log.
    ///
    /// If no active transaction exists, this method performs no operation.
    /// </summary>
    member _.Flush () : Task =
        task {
            // The file must have sufficient length before actual metadata
            // sectors beyond the previous end of file are committed.
            let currentFileSize = m_FA.GetFileSize()

            if currentFileSize <> requiredFileSize then
                do! m_FA.SetFileSize requiredFileSize

            // Commit the actual BAT / Sector Bitmap sectors.
            for pendingEntry in pendingEntries do
                for struct ( sectorNumber, data ) in pendingEntry.Updates do
                    let fileOffset = uint64 sectorNumber * 4096UL
                    do! m_FA.Write fileOffset ( ArraySegment data )

            // The log must be invalidated only after every logged metadata
            // update has completed successfully.
            let newHeader = {
                currentHeader with
                    LogGuid = Guid.Empty
            }
            let! nextSequenceNumber = VhdxCommons.UpdateHeader m_FA newHeader
            currentHeader <- {
                newHeader with
                    SequenceNumber = nextSequenceNumber
            }

            // Reset the transaction state only after LogGuid has been
            // successfully cleared in the header.
            activeLogGuid <- Guid.Empty
            nextLogSequenceNumber <- 1UL
            nextLogWriteOffset <- 0u

            pendingEntries.Clear()
        }

    /// Return the size in bytes of a VHDX log entry containing sectorCount
    /// data descriptors.
    ///
    /// This is intentionally calculated in the same manner as
    /// VhdxCorrupter.CreateLogEntry.
    member private _.getLogEntryLength ( sectorCount : uint32 ) : uint32 =
        let descriptorBytes = sectorCount * 32u + 64u
        let descriptorSectorLength = ( ( descriptorBytes + 4095u ) / 4096u ) * 4096u
        descriptorSectorLength + sectorCount * 4096u

    /// Determine whether a log entry of the specified size can be appended
    /// without wrapping around the end of the log area.
    ///
    /// This implementation deliberately does not split a log entry across the
    /// end of the active transaction. If the remaining log area is too small,
    /// the active transaction is flushed before a new transaction is started.
    member private _.canAppendEntry ( entryLength : uint32 ) =
        ( nextLogWriteOffset <= currentHeader.LogLength ) &&
            ( entryLength <= currentHeader.LogLength - nextLogWriteOffset )


    // ------------------------------------------------------------------------
    // Log append
    // ------------------------------------------------------------------------

    /// Append one already-created logical group of metadata sectors.
    ///
    /// If the active log does not have enough remaining capacity, it is
    /// flushed before the supplied update is appended.
    member private this.appendEntry ( structures : VhdxStructures ) ( updates : struct( SEC4K_T * byte[] )[] ) ( fileSizeAfterCommit : uint64 ) : Task =
        task {
            let entryLength = this.getLogEntryLength( uint32 updates.Length )

            // If an active transaction exists but the next entry cannot fit,
            // commit the current transaction first.
            if activeLogGuid <> Guid.Empty && not ( this.canAppendEntry entryLength ) then
                do! this.Flush()

            // A new transaction is required.
            if activeLogGuid = Guid.Empty then
                activeLogGuid <- Guid.NewGuid()
                nextLogSequenceNumber <- 1UL
                nextLogWriteOffset <- 0u
                pendingEntries.Clear()
                requiredFileSize <- m_FA.GetFileSize()

            let fileSizeBeforeCommit = m_FA.GetFileSize()
            let logEntry =
                VhdxCorrupter.CreateLogEntry updates nextLogWriteOffset nextLogSequenceNumber activeLogGuid fileSizeBeforeCommit fileSizeAfterCommit

            let currentStructures = {
                structures with
                    Header = currentHeader
            }

            // The log entry must become durable before the header advertises
            // the LogGuid.
            do! VhdxCorrupter.WriteLogEntry m_FA currentStructures nextLogWriteOffset [] logEntry

            let pendingEntry = {
                LogOffset = nextLogWriteOffset
                LogEntry = logEntry
                Updates = updates
                RequiredFileSize = fileSizeAfterCommit
            }

            pendingEntries.Add pendingEntry

            nextLogWriteOffset <- nextLogWriteOffset + entryLength
            nextLogSequenceNumber <- nextLogSequenceNumber + 1UL

            if fileSizeAfterCommit > requiredFileSize then
                requiredFileSize <- fileSizeAfterCommit

            // Only the first entry of a transaction activates LogGuid.
            if pendingEntries.Count = 1 then
                let newHeader = {
                    currentHeader with
                        LogGuid = activeLogGuid
                }
                let! nextSequenceNumber = VhdxCommons.UpdateHeader m_FA newHeader
                currentHeader <- {
                    newHeader with
                        SequenceNumber = nextSequenceNumber
                }
        }

