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



