//=============================================================================
// Haruka Software Storage.
// VhdxTestFunc.fs : Common functions for VHDX media test cases.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.VHDXMedia

//=============================================================================
// Import declaration


open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Text
open System.Net
open System.Runtime.InteropServices

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Media.VhdxUtil
open Haruka.Test


//=============================================================================
// Type definitions

type TestLogDesc =
    | Zero of ( uint64 * uint64 )   // ZeroLength and FileOffset
    | Data of byte[] * uint64       // byte data and FileOffset. The byte data must be 4KB length.

type TestLogEntry = {
    PatchPosition : int;
    PatchData : byte[];
    Descriptor : TestLogDesc[];
}

//=============================================================================
// Class implementation

type VhdxTestFunc() =

    [<DllImport( "kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode )>]
    static extern bool GetVolumeNameForVolumeMountPoint(
        string lpszVolumeMountPoint, 
        StringBuilder lpszVolumeName, 
        uint32 cchBufferLength
    )

    /// <summary>
    ///  Generate log entry header bytes.
    /// </summary>
    /// <param name="entryLength">
    ///  The EntryLength value at log entry header.
    /// </param>
    /// <param name="tail">
    ///  The Tail value at log entry header.
    /// </param>
    /// <param name="sequenceNumber">
    ///  The SequenceNumber value at log entry header.
    /// </param>
    /// <param name="descriptorCount">
    ///  The DescriptorCount value at log entry header.
    /// </param>
    /// <param name="logGuid">
    ///  The LogGuid value at log entry header.
    /// </param>
    /// <param name="flushedFileOffset">
    ///  The FlushedFileOffset value at log entry header.
    /// </param>
    /// <param name="lastFileOffset">
    ///  The LastFileOffset value at log entry header.
    /// </param>
    /// <returns>
    ///  Log entry header bytes.
    /// </returns>
    static member GenLogEntryHeader
        ( entryLength : uint32 )
        ( tail : uint32 )
        ( sequenceNumber : uint64 )
        ( descriptorCount : uint32 )
        ( logGuid : Guid )
        ( flushedFileOffset : uint64 )
        ( lastFileOffset : uint64 ) =
        [|
            yield! ( "loge" |> Encoding.UTF8.GetBytes )     // ZeroSignature
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                 // checksum
            yield! BitConverter.GetBytes entryLength        // EntryLength
            yield! BitConverter.GetBytes tail               // tail
            yield! BitConverter.GetBytes sequenceNumber     // SequenceNumber
            yield! BitConverter.GetBytes descriptorCount    // DescriptorCount
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                 // Reserved
            yield! logGuid.ToByteArray()                    // LogGuid
            yield! BitConverter.GetBytes flushedFileOffset  // FlushedFileOffset
            yield! BitConverter.GetBytes lastFileOffset     // LastFileOffset
        |]

    /// <summary>
    ///  Generate zero descriptor bytes of log entry.
    /// </summary>
    /// <param name="zeroLength">
    ///  The ZeroLength value at zero descriptor.
    /// </param>
    /// <param name="fileOffset">
    ///  The FileOffset value at zero descriptor.
    /// </param>
    /// <param name="sequenceNumber">
    ///  The SequenceNumber value at zero descriptor.
    /// </param>
    /// <returns>
    ///  Zero descriptor bytes.
    /// </returns>
    static member GenZeroDiscriptor
        ( zeroLength : uint64 )
        ( fileOffset : uint64 )
        ( sequenceNumber : uint64 ) =
        [|
            yield! ( "zero" |> Encoding.UTF8.GetBytes )     // Signature
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                 // Reserved
            yield! BitConverter.GetBytes zeroLength         // ZeroLength
            yield! BitConverter.GetBytes fileOffset         // FileOffset
            yield! BitConverter.GetBytes sequenceNumber     // SequenceNumber
        |]

    /// <summary>
    ///  Generate data descriptor bytes of log entry.
    /// </summary>
    /// <param name="trailingBytes">
    ///  The TrailingBytes value at data descriptor. It must be 4 bytes.
    /// </param>
    /// <param name="leadingBytes">
    ///  The LeadingBytes value at data descriptor. It must be 8 bytes.
    /// </param>
    /// <param name="fileOffset">
    ///  The FileOffset value at data descriptor.
    /// </param>
    /// <param name="sequenceNumber">
    ///  The SequenceNumber value at data descriptor.
    /// </param>
    /// <returns>
    ///  Data descriptor bytes.
    /// </returns>
    static member GenDataDiscriptor
        ( trailingBytes : byte[] )
        ( leadingBytes : byte[] )
        ( fileOffset : uint64 )
        ( sequenceNumber : uint64 ) =
        [|
            yield! ( "desc" |> Encoding.UTF8.GetBytes )     // DataSignature
            yield! trailingBytes                            // TrailingBytes
            yield! leadingBytes                             // LeadingBytes
            yield! BitConverter.GetBytes fileOffset         // FileOffset
            yield! BitConverter.GetBytes sequenceNumber     // SequenceNumber
        |]

    /// <summary>
    ///  Generate log data bytes.
    /// </summary>
    /// <param name="logLength">
    ///  Log area bytes count. It must be multiple of 1MB.
    /// </param>
    /// <param name="startPos">
    ///  Specify the offset from the beginning of the log buffer to determine where the log will be recorded.
    ///  It must be multiple of 4KB.
    /// </param>
    /// <param name="logEntries">
    ///  Specify the log entries that should be recorded in the log.
    /// </param>
    /// <param name="logGuid">
    ///  The log GUID value of log entry header.
    /// </param>
    /// <param name="sequenceNumber">
    ///  The SequenceNumber value of log entry header.
    /// </param>
    /// <param name="flushedFileOffset">
    ///  The FlushedFileOffset value of log entry header.
    /// </param>
    /// <param name="lastFileOffset">
    ///  The LastFileOffset value of log entry header.
    /// </param>
    /// <returns>
    ///  Log data bytes
    /// </returns>
    static member GenLogData
        ( logLength : int32 )
        ( startPos : int32 )    // 4KB unit
        ( logEntries : TestLogEntry[] )
        ( logGuid : Guid )
        ( sequenceNumber : uint64 )
        ( flushedFileOffset : uint64 )
        ( lastFileOffset : uint64 ) =

        let v = [|
            for i = 0 to logEntries.Length - 1 do
                let logents = logEntries.[i].Descriptor
                let entryBytes = [|
                    let dataCount = Array.fold ( fun cnt j -> cnt + ( match j with | Data _ -> 1 | _ -> 0 ) ) 0 logents
                    let headerlength = Functions.CeilUnit ( 64 + logents.Length * 32 ) 4096
                    let entryLength = headerlength + dataCount * 4096
                    let effSN = ( sequenceNumber + uint64 i )
                    // log entry header
                    yield! VhdxTestFunc.GenLogEntryHeader ( uint32 entryLength ) ( uint32 startPos ) effSN ( uint32 logents.Length ) logGuid flushedFileOffset lastFileOffset
                    // descriptor
                    for itr2 in logents do
                        match itr2 with
                        | Zero( x, y ) ->
                            yield! VhdxTestFunc.GenZeroDiscriptor x y effSN
                        | Data( x, y ) ->
                            let trailingBytes = if x.Length > 0 then x.[ 4092 .. 4095 ] else Array.zeroCreate<byte> 4
                            let leadingBytes = if x.Length > 0 then x.[ 0 .. 7 ] else Array.zeroCreate<byte> 8
                            yield! VhdxTestFunc.GenDataDiscriptor trailingBytes leadingBytes y effSN
                    // padding
                    let padlength = headerlength - ( 64 + logents.Length * 32 )
                    yield! Array.zeroCreate<byte> padlength
                    // data sector
                    for itr2 in logents do
                        match itr2 with
                        | Data( x, y ) ->
                            yield! ( "data" |> Encoding.UTF8.GetBytes )     // DataSignature
                            yield! BitConverter.GetBytes ( uint32 ( effSN >>> 32 ) )   // SequenceHigh
                            yield! if x.Length > 0 then x.[ 8 .. 4091 ] else Array.zeroCreate<byte> 4084
                            yield! BitConverter.GetBytes ( uint32 effSN )   // SequenceLow
                        | _ ->
                            ()
                |]
                Array.blit logEntries.[i].PatchData 0 entryBytes logEntries.[i].PatchPosition logEntries.[i].PatchData.Length
                let crc = Crc32C.Compute entryBytes
                ByteFunc.WriteU32LE entryBytes 4u crc
                yield! entryBytes
        |]
        let logBuffer = Array.zeroCreate<byte> logLength
        let bufferOffset = startPos % logLength
        if bufferOffset + v.Length <= logLength then
            Array.blit v 0 logBuffer bufferOffset v.Length
        else
            let firstChunkSize = logLength - bufferOffset
            let secondChunkSize = v.Length - firstChunkSize
            Array.blit v 0 logBuffer bufferOffset firstChunkSize
            Array.blit v firstChunkSize logBuffer 0 secondChunkSize
        logBuffer

    /// <summary>
    ///  Generate log entry data.
    /// </summary>
    /// <param name="descriptor">
    ///  Specifies the content to be output as a log descriptor.
    /// </param>
    /// <returns>
    ///  LogEntry data.
    /// </returns>
    static member GenLogEntry ( descriptor : TestLogDesc[] ) : LogEntry =
        let ddindex =
            descriptor
            |> Array.mapFold ( fun s itr ->
                if itr.IsData then
                    ( s, s + 1u )
                else
                    ( 0u, s )
            ) 0u
            |> fst
        {
            Signature = 0u;
            Checksum = 0u;
            EntryLength = 0u;
            Tail = 0u;
            SequenceNumber = 0UL;
            DescriptorCount = 1u;
            LogGuid = Guid();
            FlushedFileOffset = 0UL;
            LastFileOffset = 0UL;
            Descriptors = [
                for i = 0 to descriptor.Length - 1 do
                    match descriptor.[i] with
                    | Zero( x, y ) ->
                        LogDescriptor.Zero({
                            ZeroSignature = 0u;
                            ZeroLength = x;
                            FileOffset = y;
                            SequenceNumber = 0UL;
                        });
                    | Data( x, y ) ->
                        LogDescriptor.Data({
                            DataSignature = 0u;
                            TrailingBytes = x.[ 4092 .. 4095 ];
                            LeadingBytes = x.[ 0 .. 7 ]
                            FileOffset = y;
                            SequenceNumber = 0UL;
                            ddIndex = ddindex.[i];
                        });
            ]
            DataSectors = [
                for i = 0 to descriptor.Length - 1 do
                    match descriptor.[i] with
                    | Data( x, _ ) ->
                        yield x.[ 8 .. 4091 ];
                    | _ -> ()
            ];
        };

    /// <summary>
    ///  Generate region table bytes.
    /// </summary>
    /// <param name="t">
    ///  Values of the structured Region table.
    /// </param>
    /// <param name="pos">
    ///  Specify where the patch should be applied.
    /// </param>
    /// <param name="patch">
    ///  Patch data.
    /// </param>
    /// <returns>
    ///  Region table bytes. 
    ///  CRC is calculated based on data after patch application.
    /// </returns>
    static member GenRegionTable ( t : RegionTable ) ( pos : int ) ( patch : byte[] ) : byte[] =
        let v = Array.zeroCreate<byte> 65536
        ByteFunc.WriteU32BE v 0u t.Signature
        ByteFunc.WriteS32LE v 8u t.Entries.Length
        t.Entries
        |> List.iteri ( fun idx itr ->
            let pos = uint32 ( 16 + idx * 32 )
            if pos <= 65504u then
                ByteFunc.WriteGuid v ( pos + 0u ) itr.Guid
                ByteFunc.WriteU64LE v ( pos + 16u ) itr.FileOffset
                ByteFunc.WriteU32LE v ( pos + 24u ) itr.Length
                ByteFunc.WriteU32LE v ( pos + 28u ) ( if itr.Required then 1u else 0u )
        )
        Array.blit patch 0 v pos patch.Length
        let crc = Crc32C.Compute v
        ByteFunc.WriteU32LE v 4u crc
        v

    // defReginTable
    /// <summary>
    ///  Get default region table bytes.
    /// </summary>
    /// <param name="pos">
    ///  Specify where the patch should be applied.
    /// </param>
    /// <param name="patch">
    ///  Patch data.
    /// </param>
    /// <returns>
    ///  Bytes array from the region table generated using default values.
    ///  CRC is calculated based on data after patch application.
    /// </returns>
    static member GetDefaultRegionTable ( pos : int ) ( patch : byte[] ) : byte[] =
        VhdxTestFunc.GenRegionTable {
            Signature = 0x72656769u
            Checksum = 0u;
            EntryCount = 0u;
            Entries = [
                {
                    Guid = VhdxCommons.REGENT_TYPE_BAT;
                    FileOffset = 1048576UL;
                    Length = 1048576u;
                    Required = true;
                };
                {
                    Guid = VhdxCommons.REGENT_TYPE_METADATA;
                    FileOffset = 2097152UL;
                    Length = 1048576u;
                    Required = true;
                };
            ];
        } pos patch

    /// <summary>
    ///  Update the offset value of the Metadata Table Entry to a valid value.
    /// </summary>
    /// <param name="mdi">
    ///  Metadata Table Entry values ​​set without considering offset values.
    /// </param>
    /// <returns>
    ///  Metadata Table Entry with updated offset value.
    /// </returns>
    static member UpdateMTEOffset ( mdi : MetadataTableEntry[] ) : MetadataTableEntry[] =
        mdi
        |> Array.mapFold
            ( fun pos itr ->
                let ne = if itr.Length > 0u then { itr with Offset = pos } else itr
                let npos = pos + itr.Length
                ( ne, npos )
            ) 65536u
        |> fst

    /// <summary>
    ///  Generate metadata table bytes.
    /// </summary>
    /// <param name="len">
    ///  Specifies the length of the metadata area. It must be a multiple of 1MB.
    /// </param>
    /// <param name="mdi">
    ///  Specify the data for the entries that should be output to the metadata area.
    /// </param>
    /// <returns>
    ///  Metadata table bytes.
    /// </returns>
    static member GenMetadataTable ( len : int ) ( mdi : MetadataTableEntry[] ) : byte[] =
        let v = Array.zeroCreate<byte> len
        ByteFunc.WriteU64BE v 0u 0x6D65746164617461UL
        ByteFunc.WriteU16LE v 10u ( uint16 mdi.Length )
        mdi
        |> Array.iteri( fun idx itr ->
            let pos = 32 + idx * 32
            ByteFunc.WriteGuid v ( uint32 pos ) itr.ItemId
            ByteFunc.WriteU32LE v ( uint32 pos + 16u ) itr.Offset
            ByteFunc.WriteU32LE v ( uint32 pos + 20u ) itr.Length
            let f =  ( if itr.IsUser then 1uy else 0uy ) ||| ( if itr.IsVirtualDisk then 2uy else 0uy ) ||| ( if itr.IsRequired then 4uy else 0uy )
            v.[ pos + 24 ] <- f
            Array.blit itr.Data 0 v ( int32 itr.Offset ) itr.Data.Length
        )
        v

    /// <summary>
    ///  Generate file parameter metadata item bytes for the metadata.
    /// </summary>
    /// <param name="blockSize">
    ///  The BlockSize value for file parameter metadata item.
    /// </param>
    /// <param name="leaveBlockAllocated">
    ///  The LeaveBlockAllocated value for file parameter metadata item.
    /// </param>
    /// <param name="hasParent">
    ///  The HasParent value for file parameter metadata item.
    /// </param>
    /// <returns>
    ///  File parameter metadata item bytes.
    /// </returns>
    static member GenFileParameter ( blockSize : uint32 ) ( leaveBlockAllocated : bool ) ( hasParent : bool ) : byte[] =
        let v = Array.zeroCreate<byte> 8
        ByteFunc.WriteU32LE v 0u blockSize
        v.[4] <- ( if leaveBlockAllocated then 1uy else 0uy ) ||| ( if hasParent then 2uy else 0uy )
        v

    /// <summary>
    ///  Generate virtual disk size metadata item bytes for the metadata.
    /// </summary>
    /// <param name="virtualDiskSize">
    ///  The VirtualDiskSize value for virtual disk size metadata item.
    /// </param>
    /// <returns>
    ///  Virtual disk size metadata item bytes.
    /// </returns>
    static member GenVirtualDiskSize ( virtualDiskSize : uint64 ) : byte[] =
        let v = Array.zeroCreate<byte> 8
        ByteFunc.WriteU64LE v 0u virtualDiskSize
        v

    /// <summary>
    ///  Generate virtual disk ID metadata item bytes for the metadata.
    /// </summary>
    /// <param name="virtualDiskId">
    ///  The VirtualDiskSize value for virtual disk ID metadata item.
    /// </param>
    /// <returns>
    ///  Virtual disk ID metadata item bytes.
    /// </returns>
    static member GenVirtualDiskId ( virtualDiskId : Guid ) : byte[] =
        let v = Array.zeroCreate<byte> 16
        ByteFunc.WriteGuid v 0u virtualDiskId
        v

    /// <summary>
    ///  Generate Logical Sector Size metadata item bytes for the metadata.
    /// </summary>
    /// <param name="logicalSectorSize">
    ///  The LogicalSectorSize value for Logical Sector Size metadata item.
    /// </param>
    /// <returns>
    ///  Logical Sector Size metadata item bytes.
    /// </returns>
    static member GenLogicalSectorSize ( logicalSectorSize : uint32 ) : byte[] =
        let v = Array.zeroCreate<byte> 4
        ByteFunc.WriteU32LE v 0u logicalSectorSize
        v

    /// <summary>
    ///  Generate Physical Sector Size metadata item bytes for the metadata.
    /// </summary>
    /// <param name="physicalSectorSize">
    ///  The PhysicalSectorSize value for Physical Sector Size metadata item.
    /// </param>
    /// <returns>
    ///  Physical Sector Size metadata item bytes.
    /// </returns>
    static member GenPhysicalSectorSize ( physicalSectorSize : uint32 ) : byte[] =
        let v = Array.zeroCreate<byte> 4
        ByteFunc.WriteU32LE v 0u physicalSectorSize
        v

    /// <summary>
    ///  Generate Parent Locator metadata item bytes for the metadata.
    /// </summary>
    /// <param name="parentLocator">
    ///  The ParentLocator value for Parent Locator metadata item.
    /// </param>
    /// <returns>
    ///  Parent Locator metadata item bytes.
    /// </returns>
    static member GenParentLocator ( parentLocator : ( string * string ) [] ) : byte[] =
        let parentLocator_u16 =
            parentLocator
            |> Array.map ( fun ( e, v ) -> ( Encoding.Unicode.GetBytes e, Encoding.Unicode.GetBytes v ) )
        let offset, valueLen =
            parentLocator_u16
            |> Array.mapFold
                ( fun pos ( e, v ) ->
                    ( uint32 pos, uint32( pos + e.Length ) ), pos + e.Length + v.Length
                ) 0
        let buflen = 20 + 12 * parentLocator.Length + valueLen
        let v = Array.zeroCreate<byte> buflen

        // parent locator header
        ByteFunc.WriteGuid v 0u ( Guid "B04AEFB7-D19E-4A81-B789-25B8E9445913" )
        ByteFunc.WriteU16LE v 18u ( uint16 parentLocator.Length )

        // parent locator entry, entry and value
        offset
        |> Array.iteri ( fun idx ( eo, vo ) ->
            // parent locator entry
            let pos = 20 + 12 * idx |> uint32
            let headerlen = 20 + 12 * parentLocator.Length |> uint32
            let eoh = eo + headerlen
            let voh = vo + headerlen
            ByteFunc.WriteU32LE v ( pos + 0u ) eoh
            ByteFunc.WriteU32LE v ( pos + 4u ) voh
            let e16, v16 = parentLocator_u16.[idx]
            ByteFunc.WriteU16LE v ( pos + 8u ) ( uint16 e16.Length )
            ByteFunc.WriteU16LE v ( pos + 10u ) ( uint16 v16.Length )
            // entry
            Array.blit e16 0 v ( int eoh ) e16.Length
            // value
            Array.blit v16 0 v ( int voh ) v16.Length
        )
        v

    /// <summary>
    ///  Generate File Parameter item as MetadataTableEntry.
    /// </summary>
    /// <param name="data">
    ///  File parameter metadata item bytes.
    /// </param>
    /// <returns>
    ///  MetadataTableEntry data.
    /// </returns>
    static member GenFileParameterMTE ( data : byte[] ) : MetadataTableEntry =
        {
            ItemId = Guid( "CAA16737-FA36-4D43-B3B6-33F0AA44E76B" );
            Offset = 0u;
            Length = uint32 data.Length;
            IsUser = false;
            IsVirtualDisk = false;
            IsRequired = true;
            Data = data;
        };

    /// <summary>
    ///  Generate Virtual Disk Size item as MetadataTableEntry.
    /// </summary>
    /// <param name="data">
    ///  Virtual Disk Size metadata item bytes.
    /// </param>
    /// <returns>
    ///  MetadataTableEntry data.
    /// </returns>
    static member GenVirtualDiskSizeMTE ( data : byte[] ) : MetadataTableEntry =
        {
            ItemId = Guid( "2FA54224-CD1B-4876-B211-5DBED83BF4B8" );
            Offset = 0u;
            Length = uint32 data.Length;
            IsUser = false;
            IsVirtualDisk = true;
            IsRequired = true;
            Data = data;
        };

    /// <summary>
    ///  Generate Virtual Disk ID item as MetadataTableEntry.
    /// </summary>
    /// <param name="data">
    ///  Virtual Disk ID metadata item bytes.
    /// </param>
    /// <returns>
    ///  MetadataTableEntry data.
    /// </returns>
    static member GenVirtualDiskIdMTE ( data : byte[] ) : MetadataTableEntry =
        {
            ItemId = Guid( "BECA12AB-B2E6-4523-93EF-C309E000C746" );
            Offset = 0u;
            Length = uint32 data.Length;
            IsUser = false;
            IsVirtualDisk = true;
            IsRequired = true;
            Data = data;
        };

    /// <summary>
    ///  Generate Logical Sector Size item as MetadataTableEntry.
    /// </summary>
    /// <param name="data">
    ///  Logical Sector Size metadata item bytes.
    /// </param>
    /// <returns>
    ///  MetadataTableEntry data.
    /// </returns>
    static member GenLogicalSectorSizeMTE ( data : byte[] ) : MetadataTableEntry =
        {
            ItemId = Guid( "8141BF1D-A96F-4709-BA47-F233A8FAAB5F" );
            Offset = 0u;
            Length = uint32 data.Length;
            IsUser = false;
            IsVirtualDisk = true;
            IsRequired = true;
            Data = data;
        };

    /// <summary>
    ///  Generate Physical Sector Size item as MetadataTableEntry.
    /// </summary>
    /// <param name="data">
    ///  Physical Sector Size metadata item bytes.
    /// </param>
    /// <returns>
    ///  MetadataTableEntry data.
    /// </returns>
    static member GenPhysicalSectorSizeMTE ( data : byte[] ) : MetadataTableEntry =
        {
            ItemId = Guid( "CDA348C7-445D-4471-9CC9-E9885251C556" );
            Offset = 0u;
            Length = uint32 data.Length;
            IsUser = false;
            IsVirtualDisk = true;
            IsRequired = true;
            Data = data;
        };

    /// <summary>
    ///  Generate Parent Locator item as MetadataTableEntry.
    /// </summary>
    /// <param name="data">
    ///  Parent Locator metadata item bytes.
    /// </param>
    /// <returns>
    ///  MetadataTableEntry data.
    /// </returns>
    static member GenParentLocatorMTE ( data : byte[] ) : MetadataTableEntry =
        {
            ItemId = Guid( "A8D35F2D-B30B-454D-ABF7-D3D84834AB0C" );
            Offset = 0u;
            Length = uint32 data.Length;
            IsUser = false;
            IsVirtualDisk = false;
            IsRequired = true;
            Data = data;
        };

    /// <summary>
    ///  Generate a byte array for the Metadata Table based on the VirtualDiskInfo structured data.
    /// </summary>
    /// <param name="vdi">
    ///  VirtualDiskInfo data.
    /// </param>
    /// <returns>
    ///  Metadata Table bytes.
    /// </returns>
    static member VDItoMetadataTable ( vdi : VirtualDiskInfo ) : byte[] =
        [|
            VhdxTestFunc.GenFileParameterMTE( VhdxTestFunc.GenFileParameter vdi.PayloadBlockSize vdi.LeaveBlockAllocated vdi.HasParent );
            VhdxTestFunc.GenVirtualDiskSizeMTE( VhdxTestFunc.GenVirtualDiskSize vdi.VirtualDiskSize );
            VhdxTestFunc.GenVirtualDiskIdMTE( VhdxTestFunc.GenVirtualDiskId vdi.VirtualDiskId );
            VhdxTestFunc.GenLogicalSectorSizeMTE( VhdxTestFunc.GenLogicalSectorSize ( Blocksize.toUInt32 vdi.LogicalSectorSize ) );
            VhdxTestFunc.GenPhysicalSectorSizeMTE( VhdxTestFunc.GenPhysicalSectorSize ( Blocksize.toUInt32 vdi.PhysicalSectorSize ) );
            VhdxTestFunc.GenParentLocatorMTE( VhdxTestFunc.GenParentLocator ( vdi.ParentLocator |> Seq.map ( fun kv -> kv.Key, kv.Value ) |> Seq.toArray ) );
        |]
        |> VhdxTestFunc.UpdateMTEOffset
        |> VhdxTestFunc.GenMetadataTable 1048576

    /// <summary>
    ///  Check the exception message when metadata loading fails.
    /// </summary>
    /// <param name="expmsg">
    ///  Expected error message.
    /// </param>
    /// <param name="v">
    ///  Metadata table bytes.
    /// </param>
    static member CheckReadMetadataFailResult ( expmsg : string ) ( v : byte[] ) : unit =
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.ReadMetadata v |> ignore
            )
        Assert.StartsWith( expmsg, r.Message )

    /// <summary>
    ///  Convert the filename to Volume path format using the volume GUID value.
    /// </summary>
    /// <param name="fullPath">
    ///  File name.
    /// </param>
    /// <returns>
    ///  Volume path format filename.
    /// </returns>
    static member GetVolumePath ( fullPath: string ) : string =
        let driveName, relativePath =
            let w = Path.GetFullPath fullPath
            let r = Path.GetPathRoot w
            ( if r.EndsWith "\\" then r else r + "\\" ), ( w.Substring r.Length )
    
        if String.IsNullOrEmpty driveName || not ( driveName.Contains ":" ) then
            fullPath
        else
            let sb = StringBuilder 256
            if GetVolumeNameForVolumeMountPoint( driveName, sb, uint32 sb.Capacity ) then
                Path.Combine( sb.ToString(), relativePath )
            else
                fullPath

    /// <summary>
    ///  Generate BAT entries bytes.
    /// </summary>
    /// <param name="ent">
    ///  BAT entries.
    /// </param>
    /// <returns>
    ///  BAT entries bytes.
    /// </returns>
    static member GenBATEntries ( ent : ( byte * uint64 )[] ) : byte[] =
        let v = Array.zeroCreate<byte> ( ent.Length * 8 )
        ent
        |> Array.iteri ( fun idx ( s, f ) ->
            let pos = idx * 8
            ByteFunc.WriteU64LE v ( uint32 pos ) f
            v.[pos] <- ( s &&& 0x07uy )
        )
        v

    /// <summary>
    ///  Generate BAT entry bytes with default balues.
    /// </summary>
    /// <param name="pbsize">
    ///  Payload block size.
    /// </param>
    /// <param name="chunkRatio">
    ///  Chunk ratio.
    /// </param>
    /// <param name="batEntryCount">
    ///  BAT entry count.
    /// </param>
    /// <param name="hasParent">
    ///  Specify whether it is a differential VHDX file or not.
    /// </param>
    static member DefaultBatEntries ( pbsize : uint32 ) ( chunkRatio : uint32 ) ( batEntryCount : uint32 ) ( hasParent : bool ) : byte[] =
        let sectorBitmapCount =
            if hasParent then
                ( uint64 batEntryCount ) / ( uint64 chunkRatio + 1UL )
            else
                0UL
        let v = [|
            for i in 0UL .. uint64 batEntryCount - 1UL do
                let bitmapIndex = i / ( uint64 chunkRatio + 1UL )
                if i % ( uint64 chunkRatio + 1UL ) = uint64 chunkRatio then
                    if hasParent then
                        ( 6uy, ( bitmapIndex + 1UL ) * 1048576UL )
                    else
                        ( 0uy, 0UL )
                else
                    let pos = ( sectorBitmapCount + 1UL ) * 1048576UL + ( i - bitmapIndex ) * ( uint64 pbsize )
                    ( 6uy, pos )

        |]
        VhdxTestFunc.GenBATEntries v

    /// <summary>
    ///  Generate a dynamic VHDX file and run the test cases.
    /// </summary>
    /// <param name="f">
    ///  Specify the function of the test case to be executed.
    /// </param>
    static member CreateDynamicVHDXFileAndTest ( f : ( FileAccessor -> Task ) ) : Task =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! VhdxCreator.Create None fa 0x100000u 0x100000u false 0x4000000UL Blocksize.BS_512
                do! f fa
            finally
                fa.Close()
                GlbFunc.DeleteFile fname
        }

    /// <summary>
    ///  Generate differencing VHDX files and run the test cases.
    /// </summary>
    /// <param name="f">
    ///  Specify the function of the test case to be executed.
    /// </param>
    static member CreateDifferencingVHDXFileAndTest ( f : ( FileAccessor -> FileAccessor -> Task ) ) : Task =
        task {
            let pfname = Path.GetTempFileName()
            let pfa = FileAccessor( pfname, 1u, false )
            let cfname = Path.GetTempFileName()
            let cfa = FileAccessor( cfname, 1u, false )
            try
                do! VhdxCreator.Create None pfa 0x100000u 0x100000u false 0x4000000UL Blocksize.BS_512
                do! VhdxCreator.Create ( Some pfa ) cfa 0x100000u 0x100000u false 0x4000000UL Blocksize.BS_512
                do! f pfa cfa
            finally
                pfa.Close()
                cfa.Close()
                GlbFunc.DeleteFile pfname
                GlbFunc.DeleteFile cfname
        }

    /// <summary>
    ///  Generate VHDX header bytes.
    /// </summary>
    /// <param name="imhd">
    ///  Immutable VHDX Header data.
    /// </param>
    /// <param name="mhd">
    ///  Mutable VHDX Header data.
    /// </param>
    /// <returns>
    ///  VHDX header bytes.
    /// </returns>
    static member GenHeaderBytes ( imhd : VhdxHeader ) ( mhd : VhdxMutableHeader ) : byte[] =
        let v = Array.zeroCreate<byte> 4096
        Array.blit ( "head" |> Encoding.UTF8.GetBytes ) 0 v 0 4
        ByteFunc.WriteU64LE v 8u mhd.SequenceNumber
        ByteFunc.WriteGuid v 16u mhd.FileWriteGuid
        ByteFunc.WriteGuid v 32u mhd.DataWriteGuid
        ByteFunc.WriteGuid v 48u mhd.LogGuid
        ByteFunc.WriteU16LE v 64u imhd.LogVersion
        ByteFunc.WriteU16LE v 66u imhd.Version
        ByteFunc.WriteU32LE v 68u imhd.LogLength
        ByteFunc.WriteU64LE v 72u imhd.LogOffset
        Crc32C.Compute v
        |> ByteFunc.WriteU32LE v 4u
        v
