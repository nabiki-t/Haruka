namespace VhdxLibrary

open System
open System.IO

// ============================================================================
// File header

/// VHDX Header
type VhdxHeader = {
    /// Signature "head"
    Signature : uint32
    /// CRC-32C Checksum
    Checksum : uint32
    /// Sequence number
    SequenceNumber : uint64
    /// File write GUID
    FileWriteGuid : Guid
    /// Data write GUID
    DataWriteGuid : Guid
    /// Log GUID
    LogGuid : Guid
    /// Log version. Allways zero.
    LogVersion : uint16
    /// VHDX format version. Always 1.
    Version : uint16
    /// Log area size in bytes.
    LogLength : uint32
    /// File offset in bytes where log area to be written.
    LogOffset : uint64
    /// File offset in bytes where this header to be written.
    Offset : uint64
    /// Index of this header ( 0 or 1 )
    Index : int;
}

// ============================================================================
// Log

/// Zero descriptor
type ZeroDescriptor = {
    /// Signature "zero"
    ZeroSignature : uint32;
    /// The length in bytes of the range where zero should be set.
    /// It must be a multiple of 4KB.
    ZeroLength : uint64;
    /// The file offset in bytes of the range where zero should be set.
    /// It must be a multiple of 4KB.
    FileOffset : uint64;
    /// Sequence number, it must equal the sequence number in the log entry.
    SequenceNumber : uint64;
}

/// Data descriptor
type DataDescriptor = {
    /// Signature "desc"
    DataSignature : uint32;
    /// Trailing 4 bytes, in update data.
    TrailingBytes : byte[];
    /// Leading 8 bytes, in update data.
    LeadingBytes : byte[];
    /// The file offset in bytes where the update data should be write.
    FileOffset : uint64;
    /// Sequence number, it must equal the sequence number in the log entry.
    SequenceNumber : uint64;
    /// Index of data descriptors, excluding zero descriptors.
    ddIndex : uint32;
}

/// Type of descriptor
type LogDescriptor =
    | Data of DataDescriptor
    | Zero of ZeroDescriptor

/// Log entry
type LogEntry = {
    /// Signature "loge"
    Signature : uint32;
    /// Checksum
    Checksum : uint32;
    /// Size in bytes of log entry.
    /// It must be a multiple of 4KB.
    EntryLength : uint32;
    /// Tail
    Tail : uint32;
    /// Sequence number
    SequenceNumber : uint32;
    /// Number of descriptors in this log entry.
    DescriptorCount : uint32;
    /// Log GUID
    LogGuid : Guid;
    /// Flushed file offset
    FlushedFileOffset : uint64;
    /// Last file offset
    LastFileOffset : uint64;
    /// List of descriptores
    Descriptors : LogDescriptor list;
    /// Data sectores（1 data sector has 4084 bytes）
    DataSectors : byte[] list;
}

// ============================================================================
// Region table

/// Region table entry
type RegionEntry = {
    /// GUID representing the object type
    Guid : Guid
    /// The offset in bytes within the file where the object was written.
    FileOffset : uint64
    /// Number of bytes of the region.
    Length : uint32
    /// Required or not.
    Required : bool
}

/// Region table
type RegionTable = {
    /// Signature "regi"
    Signature : uint32
    /// CRC-32C Checksum
    Checksum : uint32
    /// Entry count
    EntryCount : uint32
    /// List of entries
    Entries : RegionEntry list
}

// ============================================================================
// Metadata

/// Metadata table entry
type MetadataTableEntry = {
    /// Metadata item ID.
    ItemId : Guid
    /// Offset within the metadata area. In bytes.
    /// Relative position from the beginning of the metadata area.
    Offset : uint32
    /// Bytes count of metadata item.
    Length : uint32
    /// A-IsUser flag.
    IsUser : bool
    /// B-IsVirtualDisk flag.
    IsVirtualDisk : bool
    /// C-IsRequired flag.
    IsRequired : bool
    /// Contains of metadata.
    Data : byte[]
}

/// Virtual disk information.
type VirtualDiskInfo = {
    /// Payload block size. (In bytes, 1MB to 256MB, power of 2)
    PayloadBlockSize : uint32;
    /// Is it possible to deallocate blocks from a file?
    LeaveBlockAllocated : bool;
    /// Does this file contain a parent VHDX file?
    HasParent : bool;
    /// Virtual disk size.(In bytes, Maxmum 64TB)
    VirtualDiskSize : uint64;
    /// Virtual disk ID
    VirtualDiskId : Guid;
    /// Logical sector size(In bytes, 512 or 4096)
    LogicalSectorSize : Blocksize;
    /// Physical sector size(In bytes, 512 or 4096)
    PhysicalSectorSize : Blocksize;
    /// Parent locator. Maxmum 65,535.
    ParentLocator : Map<string,string>;
}

// ============================================================================
// Block Allocation Table(BAT)

/// Status of Payload BAT entry
[<Struct>]
type BatEntryStatePB =
    | PayloadNotPresent
    | PayloadUndefined
    | PayloadZero
    | PayloadUnapped
    | PayloadFullyPresent
    | PayloadPartiallyPresent

/// Payload BAT entry
type PayloadBATEntry = {
    // index number of this BAT entry within BAT table.
    BatEntryIndex : uint64;
    // Status ot this BAT entry
    State : BatEntryStatePB;
    // File offset where the payload block is recorded
    FileOffset : uint64;
}

/// Status of sector bitmap BAT entry
[<Struct>]
type BatEntryStateSB =
    | SectorBitmapNotPresent
    | SectorBitmapPresent

type SectorBitmapBATEntry = {
    // index number of this BAT entry within BAT table.
    BatEntryIndex : uint64;
    // Status ot this BAT entry
    SBState : BatEntryStateSB;
    // File offset where the sector bitmap is recorded
    FileOffset : uint64;
    // sector bitmap
    Bitmap : byte[];
}

/// BAT entry
type BatEntries = {
    /// Bat region file offset in bytes
    BATRegionOffset : uint64;
    /// BAT region length in bytes
    BATRegionLength : uint32;
    /// Chunk size (Max.32GB : 4096bytes*(2^23))
    ChunkSize : uint64;
    /// Chunk ratio (Max.32,768 : Chunk Size 32GB / Payload block size 1MB)
    ChunkRatio : uint64;
    /// Payload block count (Maxi.67,108,864 : 64TB / Payload block size 1MB)
    PayloadBlockCount : uint64;
    /// Sector bitmap block count (Maximum 16,384 : 64TB / Logical sector size 512 bytes / (2^23))
    SectorBitmapBlockCount : uint64;
    /// BAT entry count (Max.67,125,248 : If Logical sector size=512 then 67,125,248. If 4096 then 67,110,912.)
    BatEntryCount : uint64;
    /// Payload BAT entries. （Max.67,108,864 : 64TB/Payload block size 1MB)
    Payloads : PayloadBATEntry[];
    /// Sector bitmap blocks （Max.16,384 : 64TB / Logical sector size 512 / (2^23))
    SectorBitmap : SectorBitmapBATEntry[];
}

// ============================================================================
// All of VHDX metadata

/// VHDX file metadata
type VhdxMetadata = {
    /// creator string
    Creator : string;
    /// Effective header
    Header : VhdxHeader;
    /// Log
    LogInfo : LogEntry list;
    /// Expected file size
    LastFileSize : uint64;
    /// Regio table
    RegionTables : RegionTable;
    /// Virtual disk infomation.
    VirtualDiskInfo : VirtualDiskInfo;
    /// BAT entries
    BatEntries : BatEntries;
}
