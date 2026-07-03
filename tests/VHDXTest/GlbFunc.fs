namespace VhdxLibrary

open System
open System.IO
open System.Buffers.Binary
open System.Runtime.Intrinsics.X86
open System.Runtime.CompilerServices

/// Define possible block sizes
[<Struct; IsReadOnly>]
type Blocksize =
    | BS_512
    | BS_4096

    /// <summary>
    /// Get string name value corresponging to Blocksize value.
    /// </summary>
    static member toStringName : ( Blocksize -> string ) =
        function
        | Blocksize.BS_512 -> "512"
        | Blocksize.BS_4096 -> "4096"

    /// <summary>
    /// Get Blocksize value corresponging to specified string value. If argument is unexpected string, 512 bytes is returned.
    /// </summary>
    static member fromStringValue : ( string -> Blocksize ) =
        function
        | "512" -> Blocksize.BS_512
        | "4096" -> Blocksize.BS_4096
        | _ -> Blocksize.BS_512

    /// <summary>
    /// Get uint16 valuee corresponging to Blocksize value.
    /// </summary>
    static member toUInt16 ( v : Blocksize ) : uint16 =
        match v with
        | Blocksize.BS_512 -> 512us
        | Blocksize.BS_4096 -> 4096us

    /// <summary>
    /// Get uint32 valuee corresponging to Blocksize value.
    /// </summary>
    static member toUInt32 ( v : Blocksize ) : uint32 =
        match v with
        | Blocksize.BS_512 -> 512u
        | Blocksize.BS_4096 -> 4096u

    /// <summary>
    /// Get uint32 valuee corresponging to Blocksize value.
    /// </summary>
    static member toUInt64 ( v : Blocksize ) : uint64 =
        match v with
        | Blocksize.BS_512 -> 512UL
        | Blocksize.BS_4096 -> 4096UL

    /// <summary>
    ///  All of values.
    /// </summary>
    static member Values : Blocksize [] = [|
        Blocksize.BS_512;
        Blocksize.BS_4096;
    |]


/// Data type that represents block count
[<Measure>]
type blkcnt_me =

    /// <summary>
    ///  Convert to a primitive uint64 value.
    /// </summary>
    /// <param name="v">
    ///  Block count value by blkcnt_me.
    /// </param>
    /// <returns>
    ///  Converted uint64 value.
    /// </returns>
    static member inline toUInt64( v : uint64<blkcnt_me> ) : uint64 =
        uint64 v

    /// <summary>
    ///  Convert a primitive uint64 value to a Block count value by blkcnt_me.
    /// </summary>
    /// <param name="v">
    ///  primitive uint64 value.
    /// </param>
    /// <returns>
    ///  Converted blkcnt_me value.
    /// </returns>
    static member inline ofUInt64( v : uint64 ) : uint64<blkcnt_me> =
        v * 1UL<blkcnt_me>

    /// <summary>
    ///  Convert to a primitive uint32 value.
    /// </summary>
    /// <param name="v">
    ///  Block count value by blkcnt_me.
    /// </param>
    /// <returns>
    ///  Converted uint32 value.
    /// </returns>
    static member inline toUInt32( v : uint32<blkcnt_me> ) : uint32 =
        uint32 v

    /// <summary>
    ///  Convert a primitive uint32 value to a Block count value by blkcnt_me.
    /// </summary>
    /// <param name="v">
    ///  primitive uint32 value.
    /// </param>
    /// <returns>
    ///  Converted blkcnt_me value.
    /// </returns>
    static member inline ofUInt32( v : uint32 ) : uint32<blkcnt_me> =
        v * 1u<blkcnt_me>

    /// <summary>
    ///  Convert to a primitive uint16 value.
    /// </summary>
    /// <param name="v">
    ///  Block count value by blkcnt_me.
    /// </param>
    /// <returns>
    ///  Converted uint16 value.
    /// </returns>
    static member inline toUInt16( v : uint16<blkcnt_me> ) : uint16 =
        uint16 v

    /// <summary>
    ///  Convert a primitive uint16 value to a Block count value by blkcnt_me.
    /// </summary>
    /// <param name="v">
    ///  primitive uint16 value.
    /// </param>
    /// <returns>
    ///  Converted blkcnt_me value.
    /// </returns>
    static member inline ofUInt16( v : uint16 ) : uint16<blkcnt_me> =
        v * 1us<blkcnt_me>

    /// <summary>
    ///  Convert to a primitive uint8 value.
    /// </summary>
    /// <param name="v">
    ///  Block count value by blkcnt_me.
    /// </param>
    /// <returns>
    ///  Converted uint16 value.
    /// </returns>
    static member inline toUInt8( v : uint8<blkcnt_me> ) : uint8 =
        uint8 v

    /// <summary>
    ///  Convert a primitive uint8 value to a Block count value by blkcnt_me.
    /// </summary>
    /// <param name="v">
    ///  primitive uint8 value.
    /// </param>
    /// <returns>
    ///  Converted blkcnt_me value.
    /// </returns>
    static member inline ofUInt8( v : uint8 ) : uint8<blkcnt_me> =
        v * 1uy<blkcnt_me>

    /// zero value by uint8
    static member zero8 = 0uy<blkcnt_me>

    /// zero value by uint16
    static member zero16 = 0us<blkcnt_me>

    /// zero value by uint32
    static member zero32 = 0u<blkcnt_me>

    /// zero value by uint64
    static member zero64 = 0UL<blkcnt_me>

/// Data type that uses uint8 to represent the number of blocks
type BLKCNT8_T = uint8<blkcnt_me>

/// Data type that uses uint16 to represent the number of blocks
type BLKCNT16_T = uint16<blkcnt_me>

/// Data type that uses uint32 to represent the number of blocks
type BLKCNT32_T = uint32<blkcnt_me>

/// Data type that uses uint64 to represent the number of blocks
type BLKCNT64_T = uint64<blkcnt_me>


/// Data type that represents 4K sector number.
[<Measure>]
type sec4k_me =

    /// <summary>
    ///  Convert to a primitive uint64 value.
    /// </summary>
    /// <param name="v">
    ///  Block count value by sec4k_me.
    /// </param>
    /// <returns>
    ///  Converted uint64 value.
    /// </returns>
    static member inline toUInt64( v : uint64<sec4k_me> ) : uint64 =
        uint64 v

    /// <summary>
    ///  Convert a primitive uint64 value to a Block count value by sec4k_me.
    /// </summary>
    /// <param name="v">
    ///  primitive uint64 value.
    /// </param>
    /// <returns>
    ///  Converted sec4k_me value.
    /// </returns>
    static member inline ofUInt64( v : uint64 ) : uint64<sec4k_me> =
        v * 1UL<sec4k_me>

    /// zero value by uint64
    static member zero64 = 0UL<sec4k_me>


/// Data type that uses uint64 to represent the 4K sector number
type SEC4K_T = uint64<sec4k_me>



/// CRC-32C Checksum Calculation
type Crc32C =
    static let Table : uint32[] =
        [|
            for i in 0u .. 255u ->
                Seq.fold
                    ( fun a d -> if a &&& 1u = 1u then 0x82F63B78u ^^^ ( a >>> 1 ) else a >>> 1 )
                    i [| 0 .. 7 |]
        |]
        
    static let CRC32_soft ( init : uint32 ) ( v : byte[] ) ( s : int32 ) ( cnt : int32 ) : uint32 =
        let rec loop idx a =
            if idx < cnt then
                loop ( idx + 1 ) ( ( a >>> 8 ) ^^^ Table.[ int( a &&& 0xFFu ) ^^^ int32 ( v.[ s + idx ] ) ] )
            else
                a
        loop 0 init

    static let CRC32_x64 ( init : uint32 ) ( v : byte[] ) ( s : int32 ) ( cnt : int32 ) : uint32 =
        let mutable r2 = init
        let lcnt1 = cnt / 8
        for i = 0 to lcnt1 - 1 do
            r2 <- uint32 <| Sse42.X64.Crc32( ( uint64 r2 ), BitConverter.ToUInt64( v, i * 8 + s ) )
        for i = lcnt1 * 8 to cnt - 1 do
            r2 <- Sse42.Crc32( r2, v.[ i + s ] )
        r2

    static let CRC32_x86 ( init : uint32 ) ( v : byte[] ) ( s : int32 ) ( cnt : int32 ) : uint32 =
        let mutable r2 = init
        let lcnt1 = cnt / 4
        for i = 0 to lcnt1 - 1 do
            r2 <- Sse42.Crc32( r2, BitConverter.ToUInt32( v, i * 4 + s ) )
        for i = lcnt1 * 4 to cnt - 1 do
            r2 <- Sse42.Crc32( r2, v.[ i + s ] )
        r2

    static let CRC32_Auto : uint32 -> byte[] -> int32 -> int32 -> uint32 =
        if Sse42.IsSupported then
            if Sse42.X64.IsSupported then
                CRC32_x64
            else
                CRC32_x86
        else
            CRC32_soft

    static member Compute ( v : byte[] ) : uint32 =
        CRC32_Auto 0xFFFFFFFFu v 0 v.Length
        |> (~~~)


type GlbFunc() =

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

    /// <summary>
    ///  Calculate an index that wraps around over a specified length.
    /// </summary>
    /// <param name="pos">
    ///  The virtual index value before wrap-around.
    /// </param>
    /// <param name="len">
    ///  Length of accessible range.
    /// </param>
    /// <returns>
    ///  Wrap-arounded value.
    /// </returns>
    static member RapUInt32 ( pos : uint32 ) ( len : uint32 ) : uint32 =
        let struct( _, r ) = Math.DivRem( pos, len )
        r

    /// <summary>
    ///  Calculate an index that wraps around over a specified length.
    /// </summary>
    /// <param name="pos">
    ///  The virtual index value before wrap-around.
    /// </param>
    /// <param name="len">
    ///  Length of accessible range.
    /// </param>
    /// <returns>
    ///  Wrap-arounded value.
    /// </returns>
    static member RapInt32 ( pos : int32 ) ( len : int32 ) : int32 =
        let struct( _, r ) = Math.DivRem( pos, len )
        r

    /// <summary>
    ///  Read data from a specified region in the stream.
    /// </summary>
    /// <param name="fs">
    ///  Stream.
    /// </param>
    /// <param name="offset">
    ///  Position where data is read.
    /// </param>
    /// <param name="length">
    ///  Data length to be read.
    /// </param>
    /// <returns>
    ///  Loaded data.
    /// </returns>
    static member ReadBytes ( fs : FileStream ) ( offset : uint64 ) ( length : uint32 ) : byte[] =
        let b = Array.zeroCreate<byte>( int32 length )
        fs.Seek( int64 offset, SeekOrigin.Begin ) |> ignore
        fs.ReadExactly( b, 0, int32 length )
        b

