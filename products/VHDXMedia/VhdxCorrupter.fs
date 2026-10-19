//=============================================================================
// Haruka Software Storage.
// VhdxCorrupter.fs : Write unprocessed log data to the VHDX file.
// This is a test function that simulates a power loss during the update process.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Media.VhdxUtil

//=============================================================================
// Import declaration

open System
open System.Text
open System.Threading.Tasks
open System.Collections.Generic

open Haruka.Commons

//=============================================================================
// Class implementation

/// A class that updates an existing VHDX file,
/// filling the data in the specified sectors
/// with random numbers while recording the original data as an unprocessed log.
type VhdxCorrupter() =

    /// <summary>
    ///  Generate data descriptors for log entries
    /// </summary>
    /// <param name="offset">
    /// The location in the file where the data to be updated is recorded.
    /// </param>
    /// <param name="data">
    ///  Updated data. Must be 4096 bytes.
    /// </param>
    /// <param name="sequenceNumber">
    ///  Sequence number of the log entry.
    /// </param>
    /// <returns>
    ///  Bytes array of the data sector.
    /// </returns>
    static member CreateLogEntry_DataSector ( offset : SEC4K_T ) ( data : byte[] ) ( sequenceNumber : uint64 ) : byte[] =

        if data.Length <> 4096 then
            raise <| ArgumentException( "data must be 4096 bytes." )

        let v = Array.zeroCreate<byte> 32
        Array.blit ( Encoding.UTF8.GetBytes "desc" ) 0 v 0 4
        Array.blit data ( data.Length - 4 ) v 4 4
        Array.blit data 0 v 8 8
        ByteFunc.WriteU64LE v 16u ( uint64 offset * 4096UL )
        ByteFunc.WriteU64LE v 24u sequenceNumber
        v

    /// <summary>
    ///  Calculate the log entry header size based on the number of 4K sectores.
    /// </summary>
    /// <param name="entries">
    ///  Number of 4K sectors where update information should be recorded
    /// </param>
    /// <returns>
    ///  Log entry header size.
    /// </returns>
    static member LogEntryHeaderSizeFrom4KSecCount ( entries : uint32 ) : uint32 =
        let headerRaw = 64u + entries * 32u
        ( ( headerRaw + 4095u ) / 4096u ) * 4096u

    /// <summary>
    ///  Calculate the required log region size based on the number of 4K sectores.
    /// </summary>
    /// <param name="entries">
    ///  Number of 4K sectors where update information should be recorded
    /// </param>
    /// <returns>
    ///  Required log region size.
    /// </returns>
    static member LogEntrySizeFrom4KSecCount ( entries : uint32 ) : uint32 =
        let headerSize = VhdxCorrupter.LogEntryHeaderSizeFrom4KSecCount entries
        let dataSize = entries * 4096u
        headerSize + dataSize

    /// <summary>
    ///  Determine the upper limit of the 4K sectors that can be recorded
    ///  based on the size of the log area.
    /// </summary>
    /// <param name="capacity">
    ///  Log region size. It msut be greater than 1MB and multiple of 1MB.
    /// </param>
    /// <returns>
    ///  The number of 4K entries that can be recorded using a single log entry.
    /// </returns>
    static member Max4KSectorCountFromLogCapacity ( capacity : uint32 ) : uint32 =
        let nApprox = ( capacity - 64u ) / 4128u
        let k =
            let headerRaw = 64u + 32u * nApprox
            ( headerRaw + 4095u ) / 4096u
        let n1 = ( 4096u * k - 64u ) / 32u
        let n2 =
            let s = VhdxCorrupter.LogEntrySizeFrom4KSecCount nApprox
            s / 4096u - k
        ( min n1 n2 )

    /// <summary>
    ///  Create log entry.
    /// </summary>
    /// <param name="data">
    ///  The pair of the recording location in the file and the updated data.
    /// </param>
    /// <param name="fail">
    ///  Value of tail in log entry.
    /// </param>
    /// <param name="secnum">
    ///  Sequence number value of the log entry.
    /// </param>
    /// <param name="logGuid">
    ///  Log GUID value.
    /// </param>
    /// <param name="argFFO">
    ///  Flashed file offset value of the log entry.
    /// </param>
    /// <param name="argLFO">
    ///  Last file offset value of the log entry.
    /// </param>
    /// <returns>
    ///  Created bytes array of the log log entry.
    /// </returns>
    static member CreateLogEntry
            ( data : struct ( SEC4K_T * byte[] ) seq )
            ( tail : uint32 )
            ( secnum : uint64 )
            ( logGuid : Guid )
            ( argFFO : uint64 )
            ( argLFO : uint64 ) : byte[] =

        let descNum = Seq.length data |> uint32                             // Number of descriptor
        let headerSize = VhdxCorrupter.LogEntryHeaderSizeFrom4KSecCount descNum
        let entryLength = VhdxCorrupter.LogEntrySizeFrom4KSecCount descNum   // Bytes length of the log entry.
        let logEntry = Array.zeroCreate<byte> ( int32 entryLength )

        // Entry header
        Array.blit ( Encoding.UTF8.GetBytes "loge" ) 0 logEntry 0 4         // Signature
        ByteFunc.WriteU32LE logEntry 4u 0u            // Checksum
        ByteFunc.WriteU32LE logEntry 8u entryLength   // Entry length
        ByteFunc.WriteU32LE logEntry 12u tail         // tail
        ByteFunc.WriteU64LE logEntry 16u secnum       // Sequence number
        ByteFunc.WriteU32LE logEntry 24u descNum      // Number of descriptors
        ByteFunc.WriteU32LE logEntry 28u 0u           // Reserved
        ByteFunc.WriteGuid logEntry 32u logGuid       // Log GUID
        ByteFunc.WriteU64LE logEntry 48u argFFO       // Flashed file offset
        ByteFunc.WriteU64LE logEntry 56u argLFO       // ast file offset

        // descriptors
        data
        |> Seq.map ( fun struct ( o, d ) -> VhdxCorrupter.CreateLogEntry_DataSector o d secnum )
        |> Seq.iteri ( fun idx itr -> Array.blit itr 0 logEntry ( 64 + idx * 32 ) 32 )

        // Data sectores
        data
        |> Seq.iteri ( fun idx struct ( _, itr ) ->
            let pos = headerSize + uint32( idx * 4096 )
            Array.blit ( Encoding.UTF8.GetBytes "data" ) 0 logEntry ( int32 pos ) 4
            ByteFunc.WriteU32LE logEntry ( pos + 4u ) ( uint32 ( secnum >>> 32 ) )
            Array.blit itr 8 logEntry ( int32 pos + 8 ) 4084
            ByteFunc.WriteU32LE logEntry ( pos + 4092u ) ( uint32 secnum )
        )

        // Update checksum
        let checkSum = Crc32C.Compute logEntry
        ByteFunc.WriteU32LE logEntry 4u checkSum

        logEntry

    /// <summary>
    ///  Output log entry
    /// </summary>
    /// <param name="fa">
    ///  File accessor for VHDX file.
    /// </param>
    /// <param name="structures">
    ///  The VHDX file structures data.
    /// </param>
    /// <param name="offset">
    ///  Offset of log output position within the log area.
    /// </param>
    /// <param name="dummyLogEntry">
    ///  Log entry that writes dummy data.
    /// </param>
    /// <param name="rightLogEntry">
    ///  Log entries that write correct data.
    /// </param>
    static member WriteLogEntry ( fa : FileAccessor ) ( header : VhdxHeader ) ( offset : uint32 ) ( dummyLogEntry : byte[] list ) ( rightLogEntry: byte[] ) : Task =
        let rec loop ( idx : int32 ) ( pos : int32 ) ( v : byte[] ) : int32 =
            if idx < dummyLogEntry.Length then
                Array.blit dummyLogEntry.[idx] 0 v pos dummyLogEntry.[idx].Length
                loop ( idx + 1 ) ( pos + dummyLogEntry.[idx].Length ) v
            else
                pos

        task {
            if ( rightLogEntry.Length &&& 0x00000FFF ) <> 0 then
                raise <| ArgumentException( "Argument rightLogEntry must be multiple of 4KB." )
            if rightLogEntry.Length < 4096 then
                raise <| ArgumentException( "Argument rightLogEntry must be 4 KB or larger." )
            dummyLogEntry
            |> List.iter ( fun itr ->
                if ( itr.Length &&& 0x00000FFF ) <> 0 then
                    raise <| ArgumentException( "Argument dummyLogEntry must be multiple of 4KB." )
                if itr.Length < 4096 then
                    raise <| ArgumentException( "Argument dummyLogEntry must be 4 KB or larger." )
            )
            if ( offset &&& 0x00000FFFu ) <> 0u then
                raise <| ArgumentException( "Argument offset must be multiple of 4KB." )

            let totalLen =
                dummyLogEntry
                |> List.sumBy ( fun itr -> itr.Length )
                |> (+) rightLogEntry.Length
            if uint32 totalLen > header.LogLength then
                raise <| ArgumentException( "The log exceeds the log area length." )

            // Combine the output data into a single array.
            let v =
                if dummyLogEntry.IsEmpty then
                    rightLogEntry
                else
                    let v = Array.zeroCreate<byte> totalLen
                    Array.blit rightLogEntry 0 v ( loop 0 0 v ) rightLogEntry.Length
                    v

            let roffset = offset % header.LogLength

            if roffset + ( uint32 totalLen ) <= header.LogLength then
                do! fa.Write ( header.LogOffset + uint64 roffset ) ( ArraySegment v )
            else
                let len1 = header.LogLength - roffset   // Length of the first half
                let len2 = ( uint32 v.Length ) - len1   // Length of the second half

                // first half
                do! fa.Write ( header.LogOffset + uint64 roffset ) ( ArraySegment( v, 0, int32 len1 ) )

                // second half
                do! fa.Write ( header.LogOffset ) ( ArraySegment( v, int32 len1, int32 len2 ) )
        }


    /// <summary>
    ///  Update the VHDX file, writing random numbers to the specified sectors
    ///  while recording the original data as an unprocessed log.
    /// </summary>
    /// <param name="outputFile">
    ///  File accessor for the VHDX file.
    /// </param>
    /// <param name="sectorIndices">
    ///  The index number of the 4K sector where random data should be written.
    /// </param>
    static member Inject ( outputFile : FileAccessor ) ( sectorIndices : SEC4K_T list ) : Task =
        task {
            let! structures = VhdxReader.ReadVhdx outputFile
            let fileSize = outputFile.GetFileSize()

            if sectorIndices.IsEmpty then
                raise <| ArgumentException( "Injected sectores must be at least 1." )

            let reqLogSizse = ( VhdxCorrupter.LogEntrySizeFrom4KSecCount ( uint32 sectorIndices.Length ) ) * 5u
            if reqLogSizse > structures.ImmHeader.LogLength then
                raise <| ArgumentException( "The required log capacity exceeds the log area length." )

            // Update header
            let newLogGuid = Guid.NewGuid()
            let newHeader = {
                SequenceNumber = structures.LoadedVarHeader.SequenceNumber + 1UL;
                FileWriteGuid = Guid.NewGuid();
                DataWriteGuid = structures.LoadedVarHeader.DataWriteGuid;
                LogGuid = newLogGuid;
            }
            let! _ = VhdxCommons.UpdateHeader outputFile structures.ImmHeader newHeader

            // Fill the log area with random numbers.
            let logSecCnt = structures.ImmHeader.LogLength / 4096u
            let randbuf = Array.zeroCreate<byte> 4096
            for i in 1UL .. uint64 logSecCnt - 1UL do
                Random.Shared.NextBytes randbuf
                do! outputFile.WriteArray ( structures.ImmHeader.LogOffset + i * 4096UL ) randbuf

            // Generate dummy update data
            let dummyUpdateData = [
                for i = 0 to 3 do
                    yield
                        sectorIndices
                        |> List.map ( fun itr ->
                            let rnddata = Array.zeroCreate<byte> 4096
                            Random.Shared.NextBytes rnddata
                            struct ( itr, rnddata )
                        )
            ]

            // Retrieve data from the specified 4K sectors and
            // fill in the remaining data with random numbers.
            let sectorInfo = List< struct( SEC4K_T * byte[] ) >( sectorIndices.Length )
            let rnddata = Array.zeroCreate<byte> 4096
            Random.Shared.NextBytes rnddata
            for itr in sectorIndices do
                let offset = uint64 itr * 4096UL
                let readdata = Array.zeroCreate<byte> 4096
                do! outputFile.ReadArray offset readdata
                do! outputFile.WriteArray offset rnddata
                sectorInfo.Add ( struct ( itr, readdata ) )

            // Determine the log writing location.
            //let logOutputPos = Random.Shared.Next ( int32 logSecCnt ) * 4096 |> uint32
            let logOutputPos = 0u

            // Generate four dummy log entries.
            let dummyLogEntry =
                dummyUpdateData
                |> Seq.mapi ( fun idx itr ->
                    let secn = uint64 idx + 1UL
                    VhdxCorrupter.CreateLogEntry itr logOutputPos secn newLogGuid fileSize fileSize
                )
                |> Seq.toList

            // Generate a correct log entry (1 item)
            let rightLogEntry = VhdxCorrupter.CreateLogEntry sectorInfo logOutputPos 5UL newLogGuid fileSize fileSize

            do! VhdxCorrupter.WriteLogEntry outputFile structures.ImmHeader logOutputPos dummyLogEntry rightLogEntry

        }
