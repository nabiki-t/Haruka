namespace VhdxLibrary

open System
open System.IO
open System.Buffers.Binary

open Haruka.Commons


type VhdxCommon() =

    /// GUID representing BAT in the region table.
    static member REGENT_TYPE_BAT = Guid( "2dc27766-f623-4200-9d64-115e9bfd4a08" )

    /// GUID representing the metadata in the region table.
    static member REGENT_TYPE_METADATA = Guid( "8b7ca206-4790-4b9a-b8fe-575f050f886e" )

    /// GUID representing the file parameters in the metadata item.
    static member METADATA_FILE_PARAM = Guid( "CAA16737-FA36-4D43-B3B6-33F0AA44E76B" )

    /// GUID representing the virtual disk size in the metadata item.
    static member METADATA_VIRT_DISK_SIZE = Guid( "2FA54224-CD1B-4876-B211-5DBED83BF4B8" )

    /// GUID representing the virtual disk ID in the metadata item.
    static member METADATA_VIRT_DISK_ID = Guid( "BECA12AB-B2E6-4523-93EF-C309E000C746" )

    /// GUID representing the logical sector size in the metadata item.
    static member METADATA_LOGI_SECTOR_SIZE = Guid( "8141BF1D-A96F-4709-BA47-F233A8FAAB5F" )

    /// GUID representing the physical sector size in the metadata item.
    static member METADATA_PHY_SECTOR_SIZE = Guid( "CDA348C7-445D-4471-9CC9-E9885251C556" )

    /// GUID representing the parent locator in the metadata item.
    static member METADATA_PARENT_LOC = Guid( "A8D35F2D-B30B-454D-ABF7-D3D84834AB0C" )

    /// GUID representing the type of parent locator in the metadata item.
    static member METADATA_PARENT_LOC_VHDX = Guid( "B04AEFB7-D19E-4A81-B789-25B8E9445913" )

    /// <summary>
    ///  Read a GUID value from byte array.
    /// </summary>
    /// <param name="bytes">
    ///  Byte array containing a GUID value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the GUID value in the byte array is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved GUID value.
    /// </returns>
    static member ReadGuid( bytes : byte[] ) ( offset : uint32 ) : Guid =
        System.Guid( bytes.[ int32 offset .. int32 offset + 15 ] )

    /// <summary>
    ///  Read a uint32 value from byte array in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  Byte array containing the uint32 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the uint32 value in the byte array is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved uint32 value.
    /// </returns>
    static member ReadUInt32LE( bytes : byte[] ) ( offset : uint32 ) : uint32 =
        BinaryPrimitives.ReadUInt32LittleEndian( ReadOnlySpan( bytes, int32 offset, 4 ) )

    /// <summary>
    ///  Read a uint32 value from byte array in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  Byte array containing the uint32 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the uint32 value in the byte array is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved uint32 value.
    /// </returns>
    static member ReadUInt32BE( bytes : byte[] ) ( offset : uint32 ) : uint32 =
        BinaryPrimitives.ReadUInt32BigEndian( ReadOnlySpan( bytes, int32 offset, 4 ) )

    /// <summary>
    ///  Read a uint16 value from byte array in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  Byte array containing the uint16 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the uint16 value in the byte array is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved uint16 value.
    /// </returns>
    static member ReadUInt16LE( bytes : byte[] ) ( offset : uint32 ) : uint16 =
        BinaryPrimitives.ReadUInt16LittleEndian( ReadOnlySpan( bytes, int32 offset, 2 ) )

    /// <summary>
    ///  Read a uint64 value from byte array in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  Byte array containing the uint64 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the uint64 value in the byte array is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved uint64 value.
    /// </returns>
    static member ReadUInt64LE( bytes : byte[] ) ( offset : uint32 ) : uint64 =
        BinaryPrimitives.ReadUInt64LittleEndian( ReadOnlySpan( bytes, int32 offset, 8 ) )

    /// <summary>
    ///  Read a uint64 value from byte array in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  Byte array containing the uint64 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the uint64 value in the byte array is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved uint64 value.
    /// </returns>
    static member ReadUInt64BE( bytes : byte[] ) ( offset : uint32 ) : uint64 =
        BinaryPrimitives.ReadUInt64BigEndian( ReadOnlySpan( bytes, int32 offset, 8 ) )

    /// <summary>
    ///  Write a GUID value to byte array.
    /// </summary>
    /// <param name="bytes">
    ///  The byte array to which the GUID value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the GUID value will be written.
    /// </param>
    /// <param name="v">
    ///  The GUID value to be written.
    /// </param>
    static member WriteGuid( bytes : byte[] ) ( offset : uint32 ) ( v : Guid ) : unit =
        Array.blit ( v.ToByteArray() ) 0 bytes ( int32 offset ) 16

    /// <summary>
    ///  Write a uint32 value to byte array in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  The byte array to which the uint32 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the uint32 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint32 value to be written.
    /// </param>
    static member WriteUInt32LE( buffer : byte[] ) ( offset : uint32 ) ( v : uint32 ) : unit =
        BinaryPrimitives.WriteUInt32LittleEndian( Span( buffer, int32 offset, 4 ), v )

    /// <summary>
    ///  Write a uint32 value to byte array in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  The byte array to which the uint32 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the uint32 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint32 value to be written.
    /// </param>
    static member WriteUInt32BE( buffer : byte[] ) ( offset : uint32 ) ( v : uint32 ) : unit =
        BinaryPrimitives.WriteUInt32BigEndian( Span( buffer, int32 offset, 4 ), v )

    /// <summary>
    ///  Write a uint16 value to byte array in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  The byte array to which the uint16 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the uint16 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint16 value to be written.
    /// </param>
    static member WriteUInt16LE( buffer : byte[] ) ( offset : uint32 ) ( v : uint16 ) : unit =
        BinaryPrimitives.WriteUInt16LittleEndian( Span( buffer, int32 offset, 2 ), v )

    /// <summary>
    ///  Write a uint64 value to byte array in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  The byte array to which the uint64 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the uint64 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint64 value to be written.
    /// </param>
    static member WriteUInt64LE( buffer : byte[] ) ( offset : uint32 ) ( v : uint64 ) : unit =
        BinaryPrimitives.WriteUInt64LittleEndian( Span( buffer, int32 offset, 8 ), v )

    /// <summary>
    ///  Write a uint64 value to byte array in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  The byte array to which the uint64 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the uint64 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint64 value to be written.
    /// </param>
    static member WriteUInt64BE( buffer : byte[] ) ( offset : uint32 ) ( v : uint64 ) : unit =
        BinaryPrimitives.WriteUInt64BigEndian( Span( buffer, int32 offset, 8 ), v )

    /// <summary>
    ///  Verify the checksum of the header.
    /// </summary>
    /// <param name="data">
    ///  The data to be verified.
    /// </param>
    /// <param name="checksum">
    ///  Checksum value.
    /// </param>
    /// <returns>
    ///  if the data is valid, it returns true.
    /// </returns>
    static member CheckHeaderChecksum( data : byte[] ) ( checksum : uint32 ) : bool =
        let wbuf = Array.zeroCreate<byte> data.Length
        Array.blit data 0 wbuf 0 data.Length
        for i = 4 to 7 do
            wbuf.[ i ] <- 0uy;
        Crc32C.Compute wbuf = checksum



