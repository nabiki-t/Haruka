//=============================================================================
// Haruka Software Storage.
// VHDXLogManager.fs : Defines VhdxLogManager class.
// VHDXLogManager class implement VHDX file block device functionality.
// 

namespace Haruka.Media.VhdxUtil

open System
open System.Threading.Tasks

open Haruka.Commons

type private PendingLogEntry = {
    // Update data
    Updates : struct( SEC4K_T * byte[] )[];

    // File size.
    RequiredFileSize : uint64;
}

type VhdxLogManager ( m_FA : FileAccessor, m_ImmHeader : VhdxHeader, loadedVarHeader : VhdxMutableHeader ) =

    // Latest header values
    let mutable m_VarHeader = {
        loadedVarHeader with
            LogGuid = Guid.Empty;
    }

    // Log sequence numer to be used next.
    let mutable nextLogSequenceNumber = Random.Shared.NextInt64( 100000000L, 0x3FFFFFFFFFFFFFFFL ) |> uint64

    // following log output position. Byte position within the log buffer.
    let mutable nextLogWriteOffset = 0u

    // log output content. Used for flushing.
    let pendingEntries = ResizeArray<PendingLogEntry>()

    // Byte length of the log buffer.
    let m_LogLength = m_ImmHeader.LogLength

    /// property of m_VarHeader
    member _.VarHeader = m_VarHeader

    // Update DataWriteGuid value
    member _.UpdateDataWriteGuid() : Task =
        task {
            let newhd = {
                m_VarHeader with
                    DataWriteGuid = Guid.NewGuid()
            }
            let! wverhd = VhdxCommons.UpdateHeader m_FA m_ImmHeader newhd
            m_VarHeader <- wverhd
        }

    // Update BAT entries.
    member this.UpdateBATEntries ( structures : VhdxStructures ) ( sec4Ks : SEC4K_T[] ) ( requiredFileSizeAfterCommit : uint64 ) : Task =
        task {
            let mutable index = 0u
            let sec4KsCnt = uint32 sec4Ks.Length

            while index < sec4KsCnt do
                // At least two 4K sectors are required to output logs.
                if m_LogLength - nextLogWriteOffset < 8192u then
                    do! this.Flush()

                // Calculate the number of entries that can be output from the current log output position.
                let count = 
                    VhdxWriter.Max4KSectorCountFromLogCapacity ( m_LogLength - nextLogWriteOffset )
                    |> min ( sec4KsCnt - index )

                let updates = Array.zeroCreate< struct( SEC4K_T * byte[] ) >( int count )
                for i in 0u .. count - 1u do
                    let sectorNumber = sec4Ks.[ int ( index + i ) ]
                    let data = VhdxWriter.CreateBATEntryTableFrom4KSectorNumber structures.BAT sectorNumber
                    updates.[ int i ] <- struct ( sectorNumber, data )

                // Output log entries.
                do! this.WriteLogEntries structures updates requiredFileSizeAfterCommit
                index <- index + count
        }

    // Update file size only.
    member this.UpdateFileSize ( structures : VhdxStructures ) ( requiredFileSizeAfterCommit : uint64 ) : Task =
        task {
            // One 4K sector is required to output a log entry consisting solely of an entry header.
            if m_LogLength - nextLogWriteOffset < 4096u then
                do! this.Flush()

            // Output log entries.
            do! this.WriteLogEntries structures [||] requiredFileSizeAfterCommit
        }

    // Update structures data.
    member this.UpdateGenericStructesData ( structures : VhdxStructures ) ( sec4Ks : struct( SEC4K_T * ArraySegment<byte> )[] ) : Task =
        task {
            let requiredFileSizeAfterCommit = m_FA.FileSize
            let mutable index = 0u
            let sec4KsCnt = uint32 sec4Ks.Length

            while index < sec4KsCnt do

                // At least two 4K sectors are required to output logs.
                if m_LogLength - nextLogWriteOffset < 8192u then
                    do! this.Flush()

                // Calculate the number of entries that can be output from the current log output position.
                let count = 
                    VhdxWriter.Max4KSectorCountFromLogCapacity ( m_LogLength - nextLogWriteOffset )
                    |> min ( sec4KsCnt - index )

                let updates = Array.zeroCreate< struct( SEC4K_T * byte[] ) >( int count )
                for i in 0u .. count - 1u do
                    let struct ( sectorNumber, dataSegment ) = sec4Ks.[ int ( index + i ) ]
                    let data = dataSegment.ToArray()
                    updates.[ int i ] <- struct ( sectorNumber, data )

                // Output log entries
                do! this.WriteLogEntries structures updates requiredFileSizeAfterCommit
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
            // Commit the actual BAT / Sector Bitmap sectors.
            for pendingEntry in pendingEntries do
                // update file size if needed.
                if m_FA.FileSize <> pendingEntry.RequiredFileSize then
                    do! m_FA.SetFileSize pendingEntry.RequiredFileSize

                for struct ( sectorNumber, data ) in pendingEntry.Updates do
                    let fileOffset = uint64 sectorNumber * 4096UL
                    do! m_FA.Write fileOffset ( ArraySegment data )

            // Set log GUID to zero to represent that log is cleared.
            let! wverhd = VhdxCommons.UpdateHeader m_FA m_ImmHeader { m_VarHeader with LogGuid = Guid.Empty }
            m_VarHeader <- wverhd

            // Clear log cache data.
            nextLogSequenceNumber <- nextLogSequenceNumber + 1UL
            nextLogWriteOffset <- 0u
            pendingEntries.Clear()
        }

    /// Append one already-created logical group of metadata sectors.
    ///
    /// If the active log does not have enough remaining capacity, it is
    /// flushed before the supplied update is appended.
    member private _.WriteLogEntries ( structures : VhdxStructures ) ( updates : struct( SEC4K_T * byte[] )[] ) ( fileSizeAfterCommit : uint64 ) : Task =
        task {
            // A new transaction is required.
            if m_VarHeader.LogGuid = Guid.Empty then
                m_VarHeader <- { m_VarHeader with LogGuid = Guid.NewGuid() }
                nextLogSequenceNumber <- nextLogSequenceNumber + 1UL
                nextLogWriteOffset <- 0u
                pendingEntries.Clear()

            let fileSizeBeforeCommit = m_FA.FileSize
            // Notice that tail value is always zero.
            let logEntry =
                VhdxCorrupter.CreateLogEntry updates 0u nextLogSequenceNumber m_VarHeader.LogGuid fileSizeBeforeCommit fileSizeAfterCommit

            do! VhdxCorrupter.WriteLogEntry m_FA structures nextLogWriteOffset [] logEntry

            let pendingEntry = {
                Updates = updates
                RequiredFileSize = fileSizeAfterCommit
            }
            pendingEntries.Add pendingEntry

            nextLogWriteOffset <- nextLogWriteOffset + VhdxWriter.LogCapacityFrom4KSecCount ( uint32 updates.Length )
            nextLogSequenceNumber <- nextLogSequenceNumber + 1UL

            // Only the first entry of a transaction activates LogGuid.
            if pendingEntries.Count = 1 then
                let! wverhd = VhdxCommons.UpdateHeader m_FA m_ImmHeader m_VarHeader
                m_VarHeader <- wverhd
        }

