//=============================================================================
// Haruka Software Storage.
// Crc32C.fs : CRC-32C Checksum Calculation
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Runtime.Intrinsics.X86
open System.Runtime.CompilerServices


//=============================================================================
// Class implementation


/// CRC-32C Checksum Calculation
type Crc32C =

    /// Create crc32 table 
    static let Table : uint32[] =
        [|
            for i in 0u .. 255u ->
                Array.fold
                    ( fun a d -> if a &&& 1u = 1u then 0x82F63B78u ^^^ ( a >>> 1 ) else a >>> 1 )
                    i [| 0 .. 7 |]
        |]
        
    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Calculate invert CRC32 from initial value and byte sequence.
    /// </summary>
    /// <param name="init">
    ///   CRC32 initial value.
    /// </param>
    /// <param name="v">
    ///   Byte array
    /// </param>
    /// <param name="s">
    ///   Start index of bytes array s.
    /// </param>
    /// <param name="cnt">
    ///   Bytes count.
    /// </param>
    /// <remarks>
    ///   Software imprementation of CRC32 algorithm.
    /// </remarks>
    static let CRC32_soft ( init : uint32 ) ( v : byte[] ) ( s : int32 ) ( cnt : int32 ) : uint32 =
        let rec loop idx a =
            if idx < cnt then
                loop ( idx + 1 ) ( ( a >>> 8 ) ^^^ Table.[ int32( a &&& 0xFFu ) ^^^ int32 ( v.[ s + idx ] ) ] )
            else
                a
        loop 0 init

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Calculate invert CRC32 from initial value and byte sequence.
    /// </summary>
    /// <param name="init">
    ///   CRC32 initial value.
    /// </param>
    /// <param name="v">
    ///   Byte array
    /// </param>
    /// <param name="s">
    ///   Start index of bytes array s.
    /// </param>
    /// <param name="cnt">
    ///   Bytes count.
    /// </param>
    /// <remarks>
    ///   This function must be used at x86-64 processor that support SSE4.2.
    /// </remarks>
    static let CRC32_x64 ( init : uint32 ) ( v : byte[] ) ( s : int32 ) ( cnt : int32 ) : uint32 =
        let mutable r2 = init
        let lcnt1 = cnt / 8
        for i = 0 to lcnt1 - 1 do
            r2 <- uint32 <| Sse42.X64.Crc32( ( uint64 r2 ), BitConverter.ToUInt64( v, i * 8 + s ) )
        for i = lcnt1 * 8 to cnt - 1 do
            r2 <- Sse42.Crc32( r2, v.[ i + s ] )
        r2

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Calculate invert CRC32 from initial value and byte sequence.
    /// </summary>
    /// <param name="init">
    ///   CRC32 initial value.
    /// </param>
    /// <param name="v">
    ///   Byte array
    /// </param>
    /// <param name="s">
    ///   Start index of bytes array s.
    /// </param>
    /// <param name="cnt">
    ///   Bytes count.
    /// </param>
    /// <remarks>
    ///   This function must be used at x86 processor that support SSE4.2.
    /// </remarks>
    static let CRC32_x86 ( init : uint32 ) ( v : byte[] ) ( s : int32 ) ( cnt : int32 ) : uint32 =
        let mutable r2 = init
        let lcnt1 = cnt / 4
        for i = 0 to lcnt1 - 1 do
            r2 <- Sse42.Crc32( r2, BitConverter.ToUInt32( v, i * 4 + s ) )
        for i = lcnt1 * 4 to cnt - 1 do
            r2 <- Sse42.Crc32( r2, v.[ i + s ] )
        r2

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   CRC32 internal function.
    /// </summary>
    static let CRC32_Auto : uint32 -> byte[] -> int32 -> int32 -> uint32 =
        if Sse42.IsSupported then
            if Sse42.X64.IsSupported then
                CRC32_x64
            else
                CRC32_x86
        else
            CRC32_soft

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Calculate CRC32 value from byte sequence.
    /// </summary>
    /// <param name="v">
    ///   Bytes array.
    /// </param>
    /// <returns>
    ///   Calclated CRC32 value.
    /// </returns>
    static member Compute ( v : byte[] ) : uint32 =
        CRC32_Auto 0xFFFFFFFFu v 0 v.Length
        |> (~~~)

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Calculate CRC32 value from byte array segment.
    /// </summary>
    /// <param name="v">
    ///   Array segment of byte array.
    /// </param>
    /// <returns>
    ///   Calclated CRC32 value.
    /// </returns>
    static member Compute ( v : ArraySegment<byte> ) : uint32 =
        CRC32_Auto 0xFFFFFFFFu v.Array v.Offset v.Count
        |> (~~~)

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Calculate CRC32 value from multiple byte sequences
    /// </summary>
    /// <param name="v">
    ///   Byte arrays.
    /// </param>
    /// <returns>
    ///   Calclated CRC32 value.
    /// </returns>
    static member Compute ( v : byte[][] ) : uint32 =
        v
        |> Array.fold ( fun init itr -> CRC32_Auto init itr 0 itr.Length ) 0xFFFFFFFFu
        |> (~~~)

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Calculate CRC32 value from multiple byte array segments.
    /// </summary>
    /// <param name="v">
    ///   Array segments of byte array.
    /// </param>
    /// <returns>
    ///   Calclated CRC32 value.
    /// </returns>
    static member Compute ( v : ArraySegment<byte>[] ) : uint32 =
        v
        |> Array.fold ( fun init itr -> CRC32_Auto init itr.Array itr.Offset itr.Count ) 0xFFFFFFFFu
        |> (~~~)
