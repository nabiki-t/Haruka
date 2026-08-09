//=============================================================================
// Haruka Software Storage.
// ByteFunc.fs : Define a set of basic functions for handling byte arrays.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Buffers.Binary

open Haruka.Constants

//=============================================================================
// Class implementation

/// <summary>
///   Define a set of basic functions for handling byte arrays.
/// </summary>
type ByteFunc() =

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
    static member inline ReadGuid ( bytes : byte[] ) ( offset : uint32 ) : Guid =
        Guid( ReadOnlySpan( bytes, int32 offset, 16 ) )

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
    static member inline WriteGuid ( bytes : byte[] ) ( offset : uint32 ) ( v : Guid ) : unit =
        if not <| v.TryWriteBytes( Span( bytes, int32 offset, 16 ) ) then
            failwith "Unexpected error. Failed to write GUID to byte array. In WriteGuid function."

    /// <summary>
    ///  Read a int16 value from byte array in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  Byte array containing the int16 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the int16 value in the byte array is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved int16 value.
    /// </returns>
    static member inline ReadS16LE( bytes : byte[] ) ( offset : uint32 ) : int16 =
        BinaryPrimitives.ReadInt16LittleEndian( ReadOnlySpan( bytes, int32 offset, 2 ) )

    /// <summary>
    ///  Read a int16 value from PooledBuffer in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  PooledBuffer containing the int16 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the int16 value in the PooledBuffer is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved int16 value.
    /// </returns>
    static member inline ReadS16LEPB( bytes : PooledBuffer ) ( offset : uint32 ) : int16 =
        if offset + 2u > bytes.uLength || offset + 2u < offset then
            failwith "Argument exception. Out of range. In ReadS16LEPB function."
        BinaryPrimitives.ReadInt16LittleEndian( ReadOnlySpan( bytes.Array, int32 offset, 2 ) )

    /// <summary>
    ///  Read a int16 value from byte array in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  Byte array containing the int16 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the int16 value in the byte array is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved int16 value.
    /// </returns>
    static member inline ReadS16BE ( bytes : byte[] ) ( offset : uint32 ) : int16 =
        BinaryPrimitives.ReadInt16BigEndian( ReadOnlySpan( bytes, int32 offset, 2 ) )

    /// <summary>
    ///  Read a int16 value from PooledBuffer in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  PooledBuffer containing the int16 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the int16 value in the PooledBuffer is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved int16 value.
    /// </returns>
    static member inline ReadS16BEPB ( bytes : PooledBuffer ) ( offset : uint32 ) : int16 =
        if offset + 2u > bytes.uLength || offset + 2u < offset then
            failwith "Argument exception. Out of range. In ReadS16BEPB function."
        BinaryPrimitives.ReadInt16BigEndian( ReadOnlySpan( bytes.Array, int32 offset, 2 ) )

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
    static member inline ReadU16LE( bytes : byte[] ) ( offset : uint32 ) : uint16 =
        BinaryPrimitives.ReadUInt16LittleEndian( ReadOnlySpan( bytes, int32 offset, 2 ) )

    /// <summary>
    ///  Read a uint16 value from PooledBuffer in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  PooledBuffer containing the uint16 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the uint16 value in the PooledBuffer is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved uint16 value.
    /// </returns>
    static member inline ReadU16LEPB( bytes : PooledBuffer ) ( offset : uint32 ) : uint16 =
        if offset + 2u > bytes.uLength || offset + 2u < offset then
            failwith "Argument exception. Out of range. In ReadU16LEPB function."
        BinaryPrimitives.ReadUInt16LittleEndian( ReadOnlySpan( bytes.Array, int32 offset, 2 ) )

    /// <summary>
    ///  Read a uint16 value from byte array in big-endian format.
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
    static member inline ReadU16BE ( bytes : byte[] ) ( offset : uint32 ) : uint16 =
        BinaryPrimitives.ReadUInt16BigEndian( ReadOnlySpan( bytes, int32 offset, 2 ) )

    /// <summary>
    ///  Read a uint16 value from PooledBuffer in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  PooledBuffer containing the uint16 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the uint16 value in the PooledBuffer is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved uint16 value.
    /// </returns>
    static member inline ReadU16BEPB ( bytes : PooledBuffer ) ( offset : uint32 ) : uint16 =
        if offset + 2u > bytes.uLength || offset + 2u < offset then
            failwith "Argument exception. Out of range. In ReadU16BEPB function."
        BinaryPrimitives.ReadUInt16BigEndian( ReadOnlySpan( bytes.Array, int32 offset, 2 ) )

    /// <summary>
    ///  Read a int32 value from byte array in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  Byte array containing the int32 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the int32 value in the byte array is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved int32 value.
    /// </returns>
    static member inline ReadS32LE( bytes : byte[] ) ( offset : uint32 ) : int32 =
        BinaryPrimitives.ReadInt32LittleEndian( ReadOnlySpan( bytes, int32 offset, 4 ) )

    /// <summary>
    ///  Read a int32 value from PooledBuffer in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  PooledBuffer containing the int32 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the int32 value in the PooledBuffer is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved int32 value.
    /// </returns>
    static member inline ReadS32LEPB( bytes : PooledBuffer ) ( offset : uint32 ) : int32 =
        if offset + 4u > bytes.uLength || offset + 4u < offset then
            failwith "Argument exception. Out of range. In ReadS32LEPB function."
        BinaryPrimitives.ReadInt32LittleEndian( ReadOnlySpan( bytes.Array, int32 offset, 4 ) )

    /// <summary>
    ///  Read a int32 value from byte array in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  Byte array containing the int32 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the int32 value in the byte array is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved int32 value.
    /// </returns>
    static member inline ReadS32BE ( bytes : byte[] ) ( offset : uint32 ) : int32 =
        BinaryPrimitives.ReadInt32BigEndian( ReadOnlySpan( bytes, int32 offset, 4 ) )

    /// <summary>
    ///  Read a int32 value from PooledBuffer in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  PooledBuffer containing the int32 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the int32 value in the PooledBuffer is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved int32 value.
    /// </returns>
    static member inline ReadS32BEPB ( bytes : PooledBuffer ) ( offset : uint32 ) : int32 =
        if offset + 4u > bytes.uLength || offset + 4u < offset then
            failwith "Argument exception. Out of range. In ReadS32BEPB function."
        BinaryPrimitives.ReadInt32BigEndian( ReadOnlySpan( bytes.Array, int32 offset, 4 ) )

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
    static member inline ReadU32LE( bytes : byte[] ) ( offset : uint32 ) : uint32 =
        BinaryPrimitives.ReadUInt32LittleEndian( ReadOnlySpan( bytes, int32 offset, 4 ) )

    /// <summary>
    ///  Read a uint32 value from PooledBuffer in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  PooledBuffer containing the uint32 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the uint32 value in the PooledBuffer is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved uint32 value.
    /// </returns>
    static member inline ReadU32LEPB( bytes : PooledBuffer ) ( offset : uint32 ) : uint32 =
        if offset + 4u > bytes.uLength || offset + 4u < offset then
            failwith "Argument exception. Out of range. In ReadU32LEPB function."
        BinaryPrimitives.ReadUInt32LittleEndian( ReadOnlySpan( bytes.Array, int32 offset, 4 ) )

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
    static member inline ReadU32BE ( bytes : byte[] ) ( offset : uint32 ) : uint32 =
        BinaryPrimitives.ReadUInt32BigEndian( ReadOnlySpan( bytes, int32 offset, 4 ) )

    /// <summary>
    ///  Read a uint32 value from PooledBuffer in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  PooledBuffer containing the uint32 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the uint32 value in the PooledBuffer is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved uint32 value.
    /// </returns>
    static member inline ReadU32BEPB ( bytes : PooledBuffer ) ( offset : uint32 ) : uint32 =
        if offset + 4u > bytes.uLength || offset + 4u < offset then
            failwith "Argument exception. Out of range. In ReadU32BEPB function."
        BinaryPrimitives.ReadUInt32BigEndian( ReadOnlySpan( bytes.Array, int32 offset, 4 ) )

    /// <summary>
    ///  Read a int64 value from byte array in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  Byte array containing the int64 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the int64 value in the byte array is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved int64 value.
    /// </returns>
    static member inline ReadS64LE ( bytes : byte[] ) ( offset : uint32 ) : int64 =
        BinaryPrimitives.ReadInt64LittleEndian( ReadOnlySpan( bytes, int32 offset, 8 ) )

    /// <summary>
    ///  Read a int64 value from PooledBuffer in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  PooledBuffer containing the int64 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the int64 value in the PooledBuffer is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved int64 value.
    /// </returns>
    static member inline ReadS64LEPB ( bytes : PooledBuffer ) ( offset : uint32 ) : int64 =
        if offset + 8u > bytes.uLength || offset + 8u < offset then
            failwith "Argument exception. Out of range. In ReadS64LEPB function."
        BinaryPrimitives.ReadInt64LittleEndian( ReadOnlySpan( bytes.Array, int32 offset, 8 ) )

    /// <summary>
    ///  Read a int64 value from byte array in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  Byte array containing the int64 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the int64 value in the byte array is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved int64 value.
    /// </returns>
    static member inline ReadS64BE ( bytes : byte[] ) ( offset : uint32 ) : int64 =
        BinaryPrimitives.ReadInt64BigEndian( ReadOnlySpan( bytes, int32 offset, 8 ) )

    /// <summary>
    ///  Read a int64 value from PooledBuffer in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  PooledBuffer containing the int64 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the int64 value in the PooledBuffer is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved int64 value.
    /// </returns>
    static member inline ReadS64BEPB ( bytes : PooledBuffer ) ( offset : uint32 ) : int64 =
        if offset + 8u > bytes.uLength || offset + 8u < offset then
            failwith "Argument exception. Out of range. In ReadS64BEPB function."
        BinaryPrimitives.ReadInt64BigEndian( ReadOnlySpan( bytes.Array, int32 offset, 8 ) )

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
    static member inline ReadU64LE ( bytes : byte[] ) ( offset : uint32 ) : uint64 =
        BinaryPrimitives.ReadUInt64LittleEndian( ReadOnlySpan( bytes, int32 offset, 8 ) )

    /// <summary>
    ///  Read a uint64 value from PooledBuffer in little-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  PooledBuffer containing the uint64 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the uint64 value in the PooledBuffer is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved uint64 value.
    /// </returns>
    static member inline ReadU64LEPB ( bytes : PooledBuffer ) ( offset : uint32 ) : uint64 =
        if offset + 8u > bytes.uLength || offset + 8u < offset then
            failwith "Argument exception. Out of range. In ReadU64LEPB function."
        BinaryPrimitives.ReadUInt64LittleEndian( ReadOnlySpan( bytes.Array, int32 offset, 8 ) )

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
    static member inline ReadU64BE ( bytes : byte[] ) ( offset : uint32 ) : uint64 =
        BinaryPrimitives.ReadUInt64BigEndian( ReadOnlySpan( bytes, int32 offset, 8 ) )

    /// <summary>
    ///  Read a uint64 value from PooledBuffer in big-endian format.
    /// </summary>
    /// <param name="bytes">
    ///  PooledBuffer containing the uint64 value.
    /// </param>
    /// <param name="offset">
    ///  The index in which the uint64 value in the PooledBuffer is recorded.
    /// </param>
    /// <returns>
    ///  Retrieved uint64 value.
    /// </returns>
    static member inline ReadU64BEPB ( bytes : PooledBuffer ) ( offset : uint32 ) : uint64 =
        if offset + 8u > bytes.uLength || offset + 8u < offset then
            failwith "Argument exception. Out of range. In ReadU64BEPB function."
        BinaryPrimitives.ReadUInt64BigEndian( ReadOnlySpan( bytes.Array, int32 offset, 8 ) )

    /// <summary>
    ///  Write a int16 value to byte array in little-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The byte array to which the int16 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the int16 value will be written.
    /// </param>
    /// <param name="v">
    ///  The int16 value to be written.
    /// </param>
    static member inline WriteS16LE ( buffer : byte[] ) ( offset : uint32 ) ( v : int16 ) : unit =
        BinaryPrimitives.WriteInt16LittleEndian( Span( buffer, int32 offset, 2 ), v )

    /// <summary>
    ///  Write a int16 value to the PooledBuffer in little-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The PooledBuffer to which the int16 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the PooledBuffer where the int16 value will be written.
    /// </param>
    /// <param name="v">
    ///  The int16 value to be written.
    /// </param>
    static member inline WriteS16LEPB ( buffer : PooledBuffer ) ( offset : uint32 ) ( v : int16 ) : unit =
        if offset + 2u > buffer.uLength || offset + 2u < offset then
            failwith "Argument exception. Out of range. In WriteS16LEPB function."
        BinaryPrimitives.WriteInt16LittleEndian( Span( buffer.Array, int32 offset, 2 ), v )

    /// <summary>
    ///  Write a int16 value to byte array in big-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The byte array to which the int16 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the int16 value will be written.
    /// </param>
    /// <param name="v">
    ///  The int16 value to be written.
    /// </param>
    static member inline WriteS16BE ( buffer : byte[] ) ( offset : uint32 ) ( v : int16 ) : unit =
        BinaryPrimitives.WriteInt16BigEndian( Span( buffer, int32 offset, 2 ), v )

    /// <summary>
    ///  Write a int16 value to the PooledBuffer in big-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The PooledBuffer to which the int16 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the PooledBuffer where the int16 value will be written.
    /// </param>
    /// <param name="v">
    ///  The int16 value to be written.
    /// </param>
    static member inline WriteS16BEPB ( buffer : PooledBuffer ) ( offset : uint32 ) ( v : int16 ) : unit =
        if offset + 2u > buffer.uLength || offset + 2u < offset then
            failwith "Argument exception. Out of range. In WriteS16BEPB function."
        BinaryPrimitives.WriteInt16BigEndian( Span( buffer.Array, int32 offset, 2 ), v )

    /// <summary>
    ///  Write a uint16 value to byte array in little-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The byte array to which the uint16 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the uint16 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint16 value to be written.
    /// </param>
    static member inline WriteU16LE ( buffer : byte[] ) ( offset : uint32 ) ( v : uint16 ) : unit =
        BinaryPrimitives.WriteUInt16LittleEndian( Span( buffer, int32 offset, 2 ), v )

    /// <summary>
    ///  Write a uint16 value to the PooledBuffer in little-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The PooledBuffer to which the uint16 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the PooledBuffer where the uint16 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint16 value to be written.
    /// </param>
    static member inline WriteU16LEPB ( buffer : PooledBuffer ) ( offset : uint32 ) ( v : uint16 ) : unit =
        if offset + 2u > buffer.uLength || offset + 2u < offset then
            failwith "Argument exception. Out of range. In WriteU16LEPB function."
        BinaryPrimitives.WriteUInt16LittleEndian( Span( buffer.Array, int32 offset, 2 ), v )

    /// <summary>
    ///  Write a uint16 value to byte array in big-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The byte array to which the uint16 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the uint16 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint16 value to be written.
    /// </param>
    static member inline WriteU16BE ( buffer : byte[] ) ( offset : uint32 ) ( v : uint16 ) : unit =
        BinaryPrimitives.WriteUInt16BigEndian( Span( buffer, int32 offset, 2 ), v )

    /// <summary>
    ///  Write a uint16 value to the PooledBuffer in big-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The PooledBuffer to which the uint16 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the PooledBuffer where the uint16 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint16 value to be written.
    /// </param>
    static member inline WriteU16BEPB ( buffer : PooledBuffer ) ( offset : uint32 ) ( v : uint16 ) : unit =
        if offset + 2u > buffer.uLength || offset + 2u < offset then
            failwith "Argument exception. Out of range. In WriteU16BEPB function."
        BinaryPrimitives.WriteUInt16BigEndian( Span( buffer.Array, int32 offset, 2 ), v )

    /// <summary>
    ///  Write a int32 value to byte array in little-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The byte array to which the int32 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the int32 value will be written.
    /// </param>
    /// <param name="v">
    ///  The int32 value to be written.
    /// </param>
    static member inline WriteS32LE ( buffer : byte[] ) ( offset : uint32 ) ( v : int32 ) : unit =
        BinaryPrimitives.WriteInt32LittleEndian( Span( buffer, int32 offset, 4 ), v )

    /// <summary>
    ///  Write a int32 value to the PooledBuffer in little-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The PooledBuffer to which the int32 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the PooledBuffer where the int32 value will be written.
    /// </param>
    /// <param name="v">
    ///  The int32 value to be written.
    /// </param>
    static member inline WriteS32LEPB ( buffer : PooledBuffer ) ( offset : uint32 ) ( v : int32 ) : unit =
        if offset + 4u > buffer.uLength || offset + 4u < offset then
            failwith "Argument exception. Out of range. In WriteS32LEPB function."
        BinaryPrimitives.WriteInt32LittleEndian( Span( buffer.Array, int32 offset, 4 ), v )

    /// <summary>
    ///  Write a int32 value to byte array in big-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The byte array to which the int32 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the int32 value will be written.
    /// </param>
    /// <param name="v">
    ///  The int32 value to be written.
    /// </param>
    static member inline WriteS32BE ( buffer : byte[] ) ( offset : uint32 ) ( v : int32 ) : unit =
        BinaryPrimitives.WriteInt32BigEndian( Span( buffer, int32 offset, 4 ), v )

    /// <summary>
    ///  Write a int32 value to the PooledBuffer in big-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The PooledBuffer to which the int32 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the PooledBuffer where the int32 value will be written.
    /// </param>
    /// <param name="v">
    ///  The int32 value to be written.
    /// </param>
    static member inline WriteS32BEPB ( buffer : PooledBuffer ) ( offset : uint32 ) ( v : int32 ) : unit =
        if offset + 4u > buffer.uLength || offset + 4u < offset then
            failwith "Argument exception. Out of range. In WriteS32BEPB function."
        BinaryPrimitives.WriteInt32BigEndian( Span( buffer.Array, int32 offset, 4 ), v )

    /// <summary>
    ///  Write a uint32 value to byte array in little-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The byte array to which the uint32 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the uint32 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint32 value to be written.
    /// </param>
    static member inline WriteU32LE ( buffer : byte[] ) ( offset : uint32 ) ( v : uint32 ) : unit =
        BinaryPrimitives.WriteUInt32LittleEndian( Span( buffer, int32 offset, 4 ), v )

    /// <summary>
    ///  Write a uint32 value to the PooledBuffer in little-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The PooledBuffer to which the uint32 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the PooledBuffer where the uint32 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint32 value to be written.
    /// </param>
    static member inline WriteU32LEPB ( buffer : PooledBuffer ) ( offset : uint32 ) ( v : uint32 ) : unit =
        if offset + 4u > buffer.uLength || offset + 4u < offset then
            failwith "Argument exception. Out of range. In WriteU32LEPB function."
        BinaryPrimitives.WriteUInt32LittleEndian( Span( buffer.Array, int32 offset, 4 ), v )

    /// <summary>
    ///  Write a uint32 value to byte array in big-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The byte array to which the uint32 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the uint32 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint32 value to be written.
    /// </param>
    static member inline WriteU32BE ( buffer : byte[] ) ( offset : uint32 ) ( v : uint32 ) : unit =
        BinaryPrimitives.WriteUInt32BigEndian( Span( buffer, int32 offset, 4 ), v )

    /// <summary>
    ///  Write a uint32 value to the PooledBuffer in big-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The PooledBuffer to which the uint32 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the PooledBuffer where the uint32 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint32 value to be written.
    /// </param>
    static member inline WriteU32BEPB ( buffer : PooledBuffer ) ( offset : uint32 ) ( v : uint32 ) : unit =
        if offset + 4u > buffer.uLength || offset + 4u < offset then
            failwith "Argument exception. Out of range. In WriteU32BEPB function."
        BinaryPrimitives.WriteUInt32BigEndian( Span( buffer.Array, int32 offset, 4 ), v )

    /// <summary>
    ///  Write a int64 value to byte array in little-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The byte array to which the int64 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the int64 value will be written.
    /// </param>
    /// <param name="v">
    ///  The int64 value to be written.
    /// </param>
    static member inline WriteS64LE ( buffer : byte[] ) ( offset : uint32 ) ( v : int64 ) : unit =
        BinaryPrimitives.WriteInt64LittleEndian( Span( buffer, int32 offset, 8 ), v )

    /// <summary>
    ///  Write a int64 value to the PooledBuffer in little-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The PooledBuffer to which the int64 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the PooledBuffer where the int64 value will be written.
    /// </param>
    /// <param name="v">
    ///  The int64 value to be written.
    /// </param>
    static member inline WriteS64LEPB ( buffer : PooledBuffer ) ( offset : uint32 ) ( v : int64 ) : unit =
        if offset + 8u > buffer.uLength || offset + 8u < offset then
            failwith "Argument exception. Out of range. In WriteS64LEPB function."
        BinaryPrimitives.WriteInt64LittleEndian( Span( buffer.Array, int32 offset, 8 ), v )

    /// <summary>
    ///  Write a int64 value to byte array in big-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The byte array to which the int64 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the int64 value will be written.
    /// </param>
    /// <param name="v">
    ///  The int64 value to be written.
    /// </param>
    static member inline WriteS64BE ( buffer : byte[] ) ( offset : uint32 ) ( v : int64 ) : unit =
        BinaryPrimitives.WriteInt64BigEndian( Span( buffer, int32 offset, 8 ), v )

    /// <summary>
    ///  Write a int64 value to the PooledBuffer in big-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The PooledBuffer to which the int64 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the PooledBuffer where the int64 value will be written.
    /// </param>
    /// <param name="v">
    ///  The int64 value to be written.
    /// </param>
    static member inline WriteS64BEPB ( buffer : PooledBuffer ) ( offset : uint32 ) ( v : int64 ) : unit =
        if offset + 8u > buffer.uLength || offset + 8u < offset then
            failwith "Argument exception. Out of range. In WriteS64BEPB function."
        BinaryPrimitives.WriteInt64BigEndian( Span( buffer.Array, int32 offset, 8 ), v )

    /// <summary>
    ///  Write a uint64 value to byte array in little-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The byte array to which the uint64 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the uint64 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint64 value to be written.
    /// </param>
    static member inline WriteU64LE ( buffer : byte[] ) ( offset : uint32 ) ( v : uint64 ) : unit =
        BinaryPrimitives.WriteUInt64LittleEndian( Span( buffer, int32 offset, 8 ), v )

    /// <summary>
    ///  Write a uint64 value to the PooledBuffer in little-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The PooledBuffer to which the uint64 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the PooledBuffer where the uint64 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint64 value to be written.
    /// </param>
    static member inline WriteU64LEPB ( buffer : PooledBuffer ) ( offset : uint32 ) ( v : uint64 ) : unit =
        if offset + 8u > buffer.uLength || offset + 8u < offset then
            failwith "Argument exception. Out of range. In WriteU64LEPB function."
        BinaryPrimitives.WriteUInt64LittleEndian( Span( buffer.Array, int32 offset, 8 ), v )

    /// <summary>
    ///  Write a uint64 value to byte array in big-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The byte array to which the uint64 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the byte array where the uint64 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint64 value to be written.
    /// </param>
    static member inline WriteU64BE ( buffer : byte[] ) ( offset : uint32 ) ( v : uint64 ) : unit =
        BinaryPrimitives.WriteUInt64BigEndian( Span( buffer, int32 offset, 8 ), v )

    /// <summary>
    ///  Write a uint64 value to the PooledBuffer in big-endian format.
    /// </summary>
    /// <param name="buffer">
    ///  The PooledBuffer to which the uint64 value will be written.
    /// </param>
    /// <param name="offset">
    ///  The offset in the PooledBuffer where the uint64 value will be written.
    /// </param>
    /// <param name="v">
    ///  The uint64 value to be written.
    /// </param>
    static member inline WriteU64BEPB ( buffer : PooledBuffer ) ( offset : uint32 ) ( v : uint64 ) : unit =
        if offset + 8u > buffer.uLength || offset + 8u < offset then
            failwith "Argument exception. Out of range. In WriteU64BEPB function."
        BinaryPrimitives.WriteUInt64BigEndian( Span( buffer.Array, int32 offset, 8 ), v )

    /// <summary>
    ///  Convert int16 value to byte array in little-endian format.
    ///  It allocates a new buffer that holds converted result.
    /// </summary>
    /// <param name="v">
    ///  int16 value.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member inline S16ToNVLE ( v : int16 ) : byte[] =
        let r = Array.zeroCreate<byte> 2
        ByteFunc.WriteS16LE r 0u v
        r

    /// <summary>
    ///  Convert int16 value to byte array in big-endian format.
    ///  It allocates a new buffer that holds converted result.
    /// </summary>
    /// <param name="v">
    ///  int16 value.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member inline S16ToNVBE ( v : int16 ) : byte[] =
        let r = Array.zeroCreate<byte> 2
        ByteFunc.WriteS16BE r 0u v
        r

    /// <summary>
    ///  Convert uint16 value to byte array in little-endian format.
    ///  It allocates a new buffer that holds converted result.
    /// </summary>
    /// <param name="v">
    ///  uint16 value.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member inline U16ToNVLE ( v : uint16 ) : byte[] =
        let r = Array.zeroCreate<byte> 2
        ByteFunc.WriteU16LE r 0u v
        r

    /// <summary>
    ///  Convert uint16 value to byte array in big-endian format.
    ///  It allocates a new buffer that holds converted result.
    /// </summary>
    /// <param name="v">
    ///  uint16 value.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member inline U16ToNVBE ( v : uint16 ) : byte[] =
        let r = Array.zeroCreate<byte> 2
        ByteFunc.WriteU16BE r 0u v
        r

    /// <summary>
    ///  Convert int32 value to byte array in little-endian format.
    ///  It allocates a new buffer that holds converted result.
    /// </summary>
    /// <param name="v">
    ///  int32 value.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member inline S32ToNVLE ( v : int32 ) : byte[] =
        let r = Array.zeroCreate<byte> 4
        ByteFunc.WriteS32LE r 0u v
        r

    /// <summary>
    ///  Convert int32 value to byte array in big-endian format.
    ///  It allocates a new buffer that holds converted result.
    /// </summary>
    /// <param name="v">
    ///  int32 value.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member inline S32ToNVBE ( v : int32 ) : byte[] =
        let r = Array.zeroCreate<byte> 4
        ByteFunc.WriteS32BE r 0u v
        r

    /// <summary>
    ///  Convert uint32 value to byte array in little-endian format.
    ///  It allocates a new buffer that holds converted result.
    /// </summary>
    /// <param name="v">
    ///  uint32 value.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member inline U32ToNVLE ( v : uint32 ) : byte[] =
        let r = Array.zeroCreate<byte> 4
        ByteFunc.WriteU32LE r 0u v
        r

    /// <summary>
    ///  Convert uint32 value to byte array in big-endian format.
    ///  It allocates a new buffer that holds converted result.
    /// </summary>
    /// <param name="v">
    ///  uint32 value.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member inline U32ToNVBE ( v : uint32 ) : byte[] =
        let r = Array.zeroCreate<byte> 4
        ByteFunc.WriteU32BE r 0u v
        r

    /// <summary>
    ///  Convert int64 value to byte array in little-endian format.
    ///  It allocates a new buffer that holds converted result.
    /// </summary>
    /// <param name="v">
    ///  int64 value.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member inline S64ToNVLE ( v : int64 ) : byte[] =
        let r = Array.zeroCreate<byte> 8
        ByteFunc.WriteS64LE r 0u v
        r

    /// <summary>
    ///  Convert int64 value to byte array in big-endian format.
    ///  It allocates a new buffer that holds converted result.
    /// </summary>
    /// <param name="v">
    ///  int64 value.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member inline S64ToNVBE ( v : int64 ) : byte[] =
        let r = Array.zeroCreate<byte> 8
        ByteFunc.WriteS64BE r 0u v
        r

    /// <summary>
    ///  Convert uint64 value to byte array in little-endian format.
    ///  It allocates a new buffer that holds converted result.
    /// </summary>
    /// <param name="v">
    ///  uint64 value.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member inline U64ToNVLE ( v : uint64 ) : byte[] =
        let r = Array.zeroCreate<byte> 8
        ByteFunc.WriteU64LE r 0u v
        r

    /// <summary>
    ///  Convert uint64 value to byte array in big-endian format.
    ///  It allocates a new buffer that holds converted result.
    /// </summary>
    /// <param name="v">
    ///  uint64 value.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member inline U64ToNVBE ( v : uint64 ) : byte[] =
        let r = Array.zeroCreate<byte> 8
        ByteFunc.WriteU64BE r 0u v
        r
