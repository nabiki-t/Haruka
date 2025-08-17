//=============================================================================
// Haruka Software Storage.
// Functions.fs : Generic functions, commonly used in Haruca project.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Net
open System.IO
open System.Text
open System.Runtime.Intrinsics.X86
open System.Threading
open System.Threading.Tasks
open System.Collections.Generic
open System.Collections.Frozen
open System.Buffers

open Haruka.Constants

//=============================================================================
// Function definition

/// <summary>
///   Definitions of type that is used at Functions.loopAsyncWithArgs function.
/// </summary>
type LoopState< 'a, 'b > =
    /// Specifying repeated calls to a function
    | Continue of 'a
    /// Specifies that a function call is terminated
    | Terminate of 'b

/// <summary>
///   Definitions of global functions globaly used in Haruka project.
/// </summary>
type Functions() =

    /// Generate new guid and get as string value
    static member GetNewGuid () : string =
        let wguid = Guid.NewGuid()
        wguid.ToString()

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Add length number to padding count ( int32 )
    /// </summary>
    /// <param name="length">
    ///   The number of data length.
    /// </param>
    /// <param name="unitLength">
    ///   Unit length of data. Data length is incremented until multiple of unit length.
    /// </param>
    /// <returns>
    ///   Data length include padding length ( int32 ).
    /// </returns>
    static member public AddPaddingLengthInt32 ( length : int32 ) ( unitLength : int32 ) : int32 =
        if length % unitLength = 0 then
            length
        else
            length + unitLength - ( length % unitLength )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Add length number to padding count ( uint32 )
    /// </summary>
    /// <param name="length">
    ///   The number of data length.
    /// </param>
    /// <param name="unitLength">
    ///   Unit length of data. Data length is incremented until multiple of unit length.
    /// </param>
    /// <returns>
    ///   Data length include padding length ( uint32 ).
    /// </returns>
    static member public AddPaddingLengthUInt32 ( length : uint32 ) ( unitLength : uint32 ) : uint32 =
        if length % unitLength = 0u then
            length
        else
            length + unitLength - ( length % unitLength )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Add length number to padding count ( int16 )
    /// </summary>
    /// <param name="length">
    ///   The number of data length.
    /// </param>
    /// <param name="unitLength">
    ///   Unit length of data. Data length is incremented until multiple of unit length.
    /// </param>
    /// <returns>
    ///   Data length include padding length ( int16 ).
    /// </returns>
    static member public AddPaddingLengthInt16 ( length : int16 ) ( unitLength : int16 ) : int16 =
        if length % unitLength = 0s then
            length
        else
            length + unitLength - ( length % unitLength )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Add length number to padding count ( uint16 )
    /// </summary>
    /// <param name="length">
    ///   The number of data length.
    /// </param>
    /// <param name="unitLength">
    ///   Unit length of data. Data length is incremented until multiple of unit length.
    /// </param>
    /// <returns>
    ///   Data length include padding length ( uint16 ).
    /// </returns>
    static member public AddPaddingLengthUInt16 ( length : uint16 ) ( unitLength : uint16 ) : uint16 =
        if length % unitLength = 0us then
            length
        else
            length + unitLength - ( length % unitLength )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Add length number to padding count ( sbyte )
    /// </summary>
    /// <param name="length">
    ///   The number of data length.
    /// </param>
    /// <param name="unitLength">
    ///   Unit length of data. Data length is incremented until multiple of unit length.
    /// </param>
    /// <returns>
    ///   Data length include padding length ( sbyte ).
    /// </returns>
    static member public AddPaddingLengthInt8 ( length : sbyte ) ( unitLength : sbyte ) : sbyte =
        if length % unitLength = 0y then
            length
        else
            length + unitLength - ( length % unitLength )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Add length number to padding count ( byte )
    /// </summary>
    /// <param name="length">
    ///   The number of data length.
    /// </param>
    /// <param name="unitLength">
    ///   Unit length of data. Data length is incremented until multiple of unit length.
    /// </param>
    /// <returns>
    ///   Data length include padding length ( byte ).
    /// </returns>
    static member public AddPaddingLengthUInt8 ( length : byte ) ( unitLength : byte ) : byte =
        if length % unitLength = 0uy then
            length
        else
            length + unitLength - ( length % unitLength )

    // ----------------------------------------------------------------------------
    /// Create crc32 table 
    static member private C32Tbl = 
        [|
            for i in 0u .. 255u ->
                Seq.fold
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
    /// </param name="v">
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
    static member private CRC32_soft ( init : uint32 ) ( v : byte[] ) ( s : int ) ( cnt : int ) : uint32 =
        let rec loop idx a =
            if idx < cnt then
                loop ( idx + 1 ) ( ( a >>> 8 ) ^^^ Functions.C32Tbl.[ int( a &&& 0xFFu ) ^^^ int ( v.[ s + idx ] ) ] )
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
    /// </param name="v">
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
    static member private CRC32_x64 ( init : uint32 ) ( v : byte[] ) ( s : int ) ( cnt : int ) : uint32 =
        let mutable r2 = init
        let lcnt1 = cnt / 8
        for i = 0 to lcnt1 - 1 do
            r2 <- uint <| Sse42.X64.Crc32( ( uint64 r2 ), BitConverter.ToUInt64( v, i * 8 + s ) )
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
    /// </param name="v">
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
    static member private CRC32_x86 ( init : uint32 ) ( v : byte[] ) ( s : int ) ( cnt : int ) : uint32 =
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
    static member private CRC32_Auto : uint32 -> byte[] -> int -> int -> uint32 =
        if Sse42.IsSupported then
            if Sse42.X64.IsSupported then
                Functions.CRC32_x64
            else
                Functions.CRC32_x86
        else
            Functions.CRC32_soft

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
    static member public CRC32 ( v : byte[] ) : uint32 =
        Functions.CRC32_Auto 0xFFFFFFFFu v 0 v.Length
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
    static member public CRC32_A ( v : ArraySegment<byte> ) : uint32 =
        Functions.CRC32_Auto 0xFFFFFFFFu v.Array v.Offset v.Count
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
    static member public CRC32v ( v : byte[][] ) : uint32 =
        v
        |> Seq.fold ( fun init itr -> Functions.CRC32_Auto init itr 0 itr.Length ) 0xFFFFFFFFu
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
    static member public CRC32_AS ( v : ArraySegment<byte>[] ) : uint32 =
        v
        |> Seq.fold ( fun init itr -> Functions.CRC32_Auto init itr.Array itr.Offset itr.Count ) 0xFFFFFFFFu
        |> (~~~)

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get multi-bytes number(int16) from network byte sequence.
    /// </summary>
    /// <param name="buf">
    ///   Bytes sequence that include multi-byte number data.
    ///   It has that value at network byte order.
    /// </param>
    /// <param name="s">
    ///   Start index of buf data.
    /// </param>
    /// <returns>
    ///   Obtainded number data.
    /// </returns>
    static member NetworkBytesToInt16 ( buf : byte[] ) ( s : int ) : int16 =
        BitConverter.ToInt16( buf, s )
        |> IPAddress.NetworkToHostOrder
        |> int16

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get multi-bytes number(int16) from network byte sequence.
    /// </summary>
    /// <param name="buf">
    ///   Bytes sequence that include multi-byte number data.
    ///   It has that value at network byte order.
    /// </param>
    /// <param name="s">
    ///   Start index of buf data.
    /// </param>
    /// <returns>
    ///   Obtainded number data.
    /// </returns>
    static member NetworkBytesToInt16_InPooledBuffer ( buf : PooledBuffer ) ( s : int ) : int16 =
        Functions.NetworkBytesToInt16 buf.Array s

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get multi-bytes number(uint16) from network byte sequence.
    /// </summary>
    /// <param name="buf">
    ///   Bytes sequence that include multi-byte number data.
    ///   It has that value at network byte order.
    /// </param>
    /// <param name="s">
    ///   Start index of buf data.
    /// </param>
    /// <returns>
    ///   Obtainded number data.
    /// </returns>
    static member NetworkBytesToUInt16 ( buf : byte[] ) ( s : int ) : uint16 =
        BitConverter.ToInt16( buf, s )
        |> IPAddress.NetworkToHostOrder
        |> uint16

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get multi-bytes number(uint16) from network byte sequence.
    /// </summary>
    /// <param name="buf">
    ///   Bytes sequence that include multi-byte number data.
    ///   It has that value at network byte order.
    /// </param>
    /// <param name="s">
    ///   Start index of buf data.
    /// </param>
    /// <returns>
    ///   Obtainded number data.
    /// </returns>
    static member NetworkBytesToUInt16_InPooledBuffer ( buf : PooledBuffer ) ( s : int ) : uint16 =
        Functions.NetworkBytesToUInt16 buf.Array s

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get multi-bytes number(int32) from network byte sequence.
    /// </summary>
    /// <param name="buf">
    ///   Bytes sequence that include multi-byte number data.
    ///   It has that value at network byte order.
    /// </param>
    /// <param name="s">
    ///   Start index of buf data.
    /// </param>
    /// <returns>
    ///   Obtainded number data.
    /// </returns>
    static member NetworkBytesToInt32 ( buf : byte[] ) ( s : int ) : int32 =
        BitConverter.ToInt32( buf, s )
        |> IPAddress.NetworkToHostOrder
        |> int32

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get multi-bytes number(int32) from network byte sequence.
    /// </summary>
    /// <param name="buf">
    ///   Bytes sequence that include multi-byte number data.
    ///   It has that value at network byte order.
    /// </param>
    /// <param name="s">
    ///   Start index of buf data.
    /// </param>
    /// <returns>
    ///   Obtainded number data.
    /// </returns>
    static member NetworkBytesToInt32_InPooledBuffer ( buf : PooledBuffer ) ( s : int ) : int32 =
        Functions.NetworkBytesToInt32 buf.Array s

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get multi-bytes number(uint32) from network byte sequence.
    /// </summary>
    /// <param name="buf">
    ///   Bytes sequence that include multi-byte number data.
    ///   It has that value at network byte order.
    /// </param>
    /// <param name="s">
    ///   Start index of buf data.
    /// </param>
    /// <returns>
    ///   Obtainded number data.
    /// </returns>
    static member NetworkBytesToUInt32 ( buf : byte[] ) ( s : int ) : uint32 =
        BitConverter.ToInt32( buf, s )
        |> IPAddress.NetworkToHostOrder
        |> uint32

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get multi-bytes number(uint32) from network byte sequence.
    /// </summary>
    /// <param name="buf">
    ///   Bytes sequence that include multi-byte number data.
    ///   It has that value at network byte order.
    /// </param>
    /// <param name="s">
    ///   Start index of buf data.
    /// </param>
    /// <returns>
    ///   Obtainded number data.
    /// </returns>
    static member NetworkBytesToUInt32_InPooledBuffer ( buf : PooledBuffer ) ( s : int ) : uint32 =
        Functions.NetworkBytesToUInt32 buf.Array s

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get multi-bytes number(int64) from network byte sequence.
    /// </summary>
    /// <param name="buf">
    ///   Bytes sequence that include multi-byte number data.
    ///   It has that value at network byte order.
    /// </param>
    /// <param name="s">
    ///   Start index of buf data.
    /// </param>
    /// <returns>
    ///   Obtainded number data.
    /// </returns>
    static member NetworkBytesToInt64 ( buf : byte[] ) ( s : int ) : int64 =
        BitConverter.ToInt64( buf, s )
        |> IPAddress.NetworkToHostOrder
        |> int64

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get multi-bytes number(int64) from network byte sequence.
    /// </summary>
    /// <param name="buf">
    ///   Bytes sequence that include multi-byte number data.
    ///   It has that value at network byte order.
    /// </param>
    /// <param name="s">
    ///   Start index of buf data.
    /// </param>
    /// <returns>
    ///   Obtainded number data.
    /// </returns>
    static member NetworkBytesToInt64_InPooledBuffer ( buf : PooledBuffer ) ( s : int ) : int64 =
        Functions.NetworkBytesToInt64 buf.Array s

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get multi-bytes number(uint64) from network byte sequence.
    /// </summary>
    /// <param name="buf">
    ///   Bytes sequence that include multi-byte number data.
    ///   It has that value at network byte order.
    /// </param>
    /// <param name="s">
    ///   Start index of buf data.
    /// </param>
    /// <returns>
    ///   Obtainded number data.
    /// </returns>
    static member NetworkBytesToUInt64 ( buf : byte[] ) ( s : int ) : uint64 =
        BitConverter.ToInt64( buf, s )
        |> IPAddress.NetworkToHostOrder
        |> uint64

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get multi-bytes number(uint64) from network byte sequence.
    /// </summary>
    /// <param name="buf">
    ///   Bytes sequence that include multi-byte number data.
    ///   It has that value at network byte order.
    /// </param>
    /// <param name="s">
    ///   Start index of buf data.
    /// </param>
    /// <returns>
    ///   Obtainded number data.
    /// </returns>
    static member NetworkBytesToUInt64_InPooledBuffer ( buf : PooledBuffer ) ( s : int ) : uint64 =
        Functions.NetworkBytesToUInt64 buf.Array s

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert from multi-bytes number to network bytes sequence ( int16 ).
    ///   It allocates a new buffer that holds converted result.
    /// </summary>
    static member Int16ToNetworkBytes_NewVec : int16 -> byte[] =
       IPAddress.HostToNetworkOrder >> BitConverter.GetBytes

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert from multi-bytes number to network bytes sequence ( uint16 ).
    ///   It allocates a new buffer that holds converted result.
    /// </summary>
    static member UInt16ToNetworkBytes_NewVec : uint16 -> byte[]  =
        int16 >> IPAddress.HostToNetworkOrder >> BitConverter.GetBytes

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert from multi-bytes number to network bytes sequence ( int32 ).
    ///   It allocates a new buffer that holds converted result.
    /// </summary>
    static member Int32ToNetworkBytes_NewVec : int32 -> byte[] =
       IPAddress.HostToNetworkOrder >> BitConverter.GetBytes

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert from multi-bytes number to network bytes sequence ( uint32 ).
    ///   It allocates a new buffer that holds converted result.
    /// </summary>
    static member UInt32ToNetworkBytes_NewVec : uint32 -> byte[] =
        int32 >> IPAddress.HostToNetworkOrder >> BitConverter.GetBytes

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert from multi-bytes number to network bytes sequence ( int64 ).
    ///   It allocates a new buffer that holds converted result.
    /// </summary>
    static member Int64ToNetworkBytes_NewVec : int64 -> byte[]  =
        IPAddress.HostToNetworkOrder >> BitConverter.GetBytes

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert from multi-bytes number to network bytes sequence ( unit64 ).
    ///   It allocates a new buffer that holds converted result.
    /// </summary>
    static member UInt64ToNetworkBytes_NewVec : uint64 -> byte[]  =
        int64 >> IPAddress.HostToNetworkOrder >> BitConverter.GetBytes

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert from multi-bytes number to network bytes sequence ( int16 ).
    ///   It writes result to specified buffer.
    /// </summary>
    /// <param name="buf">
    ///   Buffer receives converted result.
    /// </param>
    /// <param name="s">
    ///   Position in buffer that converted result is written.
    /// </param>
    /// <param name="v">
    ///   An integer value that will be converted.
    /// </param>
    static member Int16ToNetworkBytes ( buf : byte[] ) ( s : int ) ( v : int16 ) : unit =
        let v2 = v |> IPAddress.HostToNetworkOrder
        BitConverter.TryWriteBytes( Span( buf, s, 2 ), v2 ) |> ignore

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert from multi-bytes number to network bytes sequence ( uint16 )
    ///   It writes result to specified buffer.
    /// </summary>
    /// <param name="buf">
    ///   Buffer receives converted result.
    /// </param>
    /// <param name="s">
    ///   Position in buffer that converted result is written.
    /// </param>
    /// <param name="v">
    ///   An integer value that will be converted.
    /// </param>
    static member UInt16ToNetworkBytes ( buf : byte[] ) ( s : int ) ( v : uint16 ) : unit =
        let v2 = v |> int16 |> IPAddress.HostToNetworkOrder
        BitConverter.TryWriteBytes( Span( buf, s, 2 ), v2 ) |> ignore

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert from multi-bytes number to network bytes sequence ( int32 )
    ///   It writes result to specified buffer.
    /// </summary>
    /// <param name="buf">
    ///   Buffer receives converted result.
    /// </param>
    /// <param name="s">
    ///   Position in buffer that converted result is written.
    /// </param>
    /// <param name="v">
    ///   An integer value that will be converted.
    /// </param>
    static member Int32ToNetworkBytes ( buf : byte[] ) ( s : int ) ( v : int32 ) : unit =
        let v2 = v |> IPAddress.HostToNetworkOrder
        BitConverter.TryWriteBytes( Span( buf, s, 4 ), v2 ) |> ignore

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert from multi-bytes number to network bytes sequence ( uint32 )
    ///   It writes result to specified buffer.
    /// </summary>
    /// <param name="buf">
    ///   Buffer receives converted result.
    /// </param>
    /// <param name="s">
    ///   Position in buffer that converted result is written.
    /// </param>
    /// <param name="v">
    ///   An integer value that will be converted.
    /// </param>
    static member UInt32ToNetworkBytes ( buf : byte[] ) ( s : int ) ( v : uint32 ) : unit =
        let v2 = v |> int32 |> IPAddress.HostToNetworkOrder
        BitConverter.TryWriteBytes( Span( buf, s, 4 ), v2 ) |> ignore

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert from multi-bytes number to network bytes sequence ( int64 )
    ///   It writes result to specified buffer.
    /// </summary>
    /// <param name="buf">
    ///   Buffer receives converted result.
    /// </param>
    /// <param name="s">
    ///   Position in buffer that converted result is written.
    /// </param>
    /// <param name="v">
    ///   An integer value that will be converted.
    /// </param>
    static member Int64ToNetworkBytes ( buf : byte[] ) ( s : int ) ( v : int64 ) : unit =
        let v2 = v |> IPAddress.HostToNetworkOrder
        BitConverter.TryWriteBytes( Span( buf, s, 8 ), v2 ) |> ignore

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert from multi-bytes number to network bytes sequence ( uint64 )
    ///   It writes result to specified buffer.
    /// </summary>
    /// <param name="buf">
    ///   Buffer receives converted result.
    /// </param>
    /// <param name="s">
    ///   Position in buffer that converted result is written.
    /// </param>
    /// <param name="v">
    ///   An integer value that will be converted.
    /// </param>
    static member UInt64ToNetworkBytes ( buf : byte[] ) ( s : int ) ( v : uint64 ) : unit =
        let v2 = v |> int64 |> IPAddress.HostToNetworkOrder
        BitConverter.TryWriteBytes( Span( buf, s, 8 ), v2 ) |> ignore

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Test the first argument byte by a bitmask in second argument.
    /// </summary>
    /// <param name="d">
    ///   A byte value that tested by bitmask.
    /// </param>
    /// <param name="f">
    ///   A bitmask value.
    /// </param>
    /// <returns>
    ///   If all of bit in d value specified by bitmask of f has 1, this function returns true. Otherwise, It returns false.
    /// </returns>
    static member CheckBitflag ( d: byte ) ( f : byte ) : bool =
        d &&& f = f

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   If flag value is true, it returns bitvalue. Otherwise it returns 0.
    /// </summary>
    /// <param name="flag">
    ///   true, or false.
    /// </param>
    /// <param name="bitvalue">
    ///   A value that returns when flag is true.
    /// </param>
    /// <returns>
    ///   bitvalue, or 0.
    /// </returns>
    static member SetBitflag ( flag: bool ) ( bitvalue : byte ) : byte =
        if flag then bitvalue else 0uy

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Test the first argument uint16 value by a bitmask in second argument.
    /// </summary>
    /// <param name="d">
    ///   A uint16 value that tested by bitmask.
    /// </param>
    /// <param name="f">
    ///   A bitmak value.
    /// </param>
    /// <returns>
    ///   If all of bit in d value specified by bitmask of f has 1, this function returns true. Otherwise, It returns false.
    /// </returns>
    static member CheckBitflag16 ( d: uint16 ) ( f : uint16 ) : bool =
        d &&& f = f

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   If flag value is true, it returns bitvalue. Otherwise it returns 0.
    /// </summary>
    /// <param name="flag">
    ///   true, or false.
    /// </param>
    /// <param name="bitvalue">
    ///   A value that returns when flag is true.
    /// </param>
    /// <returns>
    ///   bitvalue, or 0.
    /// </returns>
    static member SetBitflag16 ( flag: bool ) ( bitvalue : uint16 ) : uint16 =
        if flag then bitvalue else 0us

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Test the first argument uint32 value by a bitmask in second argument.
    /// </summary>
    /// <param name="d">
    ///   A uint32 value that tested by bitmask.
    /// </param>
    /// <param name="f">
    ///   A bitmak value.
    /// </param>
    /// <returns>
    ///   If all of bit in d value specified by bitmask of f has 1, this function returns true. Otherwise, It returns false.
    /// </returns>
    static member CheckBitflag32 ( d: uint32 ) ( f : uint32 ) : bool =
        d &&& f = f

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   If flag value is true, it returns bitvalue. Otherwise it returns 0.
    /// </summary>
    /// <param name="flag">
    ///   true, or false.
    /// </param>
    /// <param name="bitvalue">
    ///   A value that returns when flag is true.
    /// </param>
    /// <returns>
    ///   bitvalue, or 0.
    /// </returns>
    static member SetBitflag32 ( flag: bool ) ( bitvalue : uint32 ) : uint32 =
        if flag then bitvalue else 0u

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Test the first argument uint64 value by a bitmask in second argument.
    /// </summary>
    /// <param name="d">
    ///   A uint64 value that tested by bitmask.
    /// </param>
    /// <param name="f">
    ///   A bitmak value.
    /// </param>
    /// <returns>
    ///   If all of bit in d value specified by bitmask of f has 1, this function returns true. Otherwise, It returns false.
    /// </returns>
    static member CheckBitflag64 ( d: uint64 ) ( f : uint64 ) : bool =
        d &&& f = f

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   If flag value is true, it returns bitvalue. Otherwise it returns 0.
    /// </summary>
    /// <param name="flag">
    ///   true, or false.
    /// </param>
    /// <param name="bitvalue">
    ///   A value that returns when flag is true.
    /// </param>
    /// <returns>
    ///   bitvalue, or 0.
    /// </returns>
    static member SetBitflag64 ( flag: bool ) ( bitvalue : uint64 ) : uint64 =
        if flag then bitvalue else 0UL

    // ----------------------------------------------------------------------------
    /// Compare string header
    static member CompareStringHeader ( s1 : string ) ( s2 : string ) =
        System.String.Compare( s1, 0, s2, 0, ( min s1.Length s2.Length ), StringComparison.Ordinal )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   split bytes array by char
    /// </summary>
    /// <param name="c">
    ///   A byte value that used to delimiter.
    /// </param>
    /// <param name="v">
    ///   A bytes array that separated into multiple arrays by byte value specified in c argument.
    /// </param>
    /// <returns>
    ///   An array that holds multiple bytes arrays.
    /// </returns>
    /// <example><code>
    ///   > SplitByteArray 0uy [| 1uy; 2uy; 0uy; 3uy; 4uy; 0uy; 9uy |];;
    ///   val it : byte [] list = [[|1uy; 2uy|]; [|3uy; 4uy|]; [|9uy|]]
    ///   > SplitByteArray 0uy [| 0uy; 2uy; 0uy; 0uy; 4uy; 0uy; 9uy; 0uy |];;
    ///   val it : byte [] list = [[||]; [|2uy|]; [||]; [|4uy|]; [|9uy|]; [||]]
    /// </code></example>
    static member SplitByteArray ( c : byte ) ( v : byte[] ) : byte [] list =
        let rec f s e ( cont : ( byte[] ) list -> ( byte[] ) list ) =
            if e < v.Length then
                if v.[e] = c then
                    f ( e + 1 ) ( e + 1 ) ( fun li -> ( cont ( v.[ s .. e - 1 ] :: li ) ) )
                else
                    f s ( e + 1 ) cont
            else
                cont [ v.[ s .. ] ]
        f 0 0 ( fun a -> a )


    // ----------------------------------------------------------------------------
    /// <summary>
    ///   create new bytes array, added padding bytes.
    /// </summary>
    /// <param name="u">
    ///   unit length
    /// </param>
    /// <param name="m">
    ///   max length
    /// </param>
    /// <param name="v">
    ///   A bytes array.
    /// </param>
    static member PadBytesArray ( u : int ) ( m : int ) ( v : byte[] ) : byte [] =
        let wl = Functions.AddPaddingLengthInt32 v.Length u
        let wa : byte[] = Array.zeroCreate( min wl m )
        Array.blit v 0 wa 0 ( min wa.Length v.Length )
        wa

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Compare optional value with specified value.
    /// </summary>
    /// <param name="d">
    ///   Default value, if v is None.
    /// </param>
    /// <param name="c">
    ///   Compared value 1.
    /// </param>
    /// <param name="v">
    ///   Compared optional value 1.
    /// </param>
    /// <returns>
    ///   if v is not None, returns compared result with c and v value. Otherwise returns d.
    /// </returns>
    static member CompareOptValueWithDefault ( d : bool ) ( c : 'a ) ( v : 'a option ) : bool =
        match v with
        | option.None -> d
        | option.Some( x ) -> c = x

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Compare optional value with specified value.
    /// </summary>
    /// <param name="c">
    ///   Compared value 1.
    /// </param>
    /// <param name="v">
    ///   Compared optional value 1.
    /// </param>
    /// <returns>
    ///   if v is not None, returns compared result with c and v value. Otherwise returns false.
    /// </returns>
    static member CompareOptValue ( c : 'a ) ( v : 'a option ) : bool =
        Functions.CompareOptValueWithDefault false c v

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   get sequence first n'th item.
    /// </summary>
    /// <param name="cnt">
    ///   muximum count of element want to take.
    /// </param>
    /// <param name="s">
    ///   sequence
    /// </param>
    static member TruncateSeq_uint32 ( cnt : uint32 ) ( s : seq<'T> ) : 'T seq =
        let i = s.GetEnumerator()
        let rec loop ( wcnt : uint32 ) =
            seq {
                if i.MoveNext() && wcnt < cnt then
                    yield i.Current
                    yield! loop ( wcnt + 1u )
            }
        loop 0u

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Append path name.
    /// </summary>
    /// <param name="p1">
    ///   parent directory name.
    /// </param>
    /// <param name="p2">
    ///   sub folder name, or file name.
    /// </param>
    /// <returns>
    ///   Appended path name, separated \ or / character.
    /// </param>
    static member AppendPathName ( p1 : string ) ( p2 : string ) : string =
        Functions.OptimizePathName( Path.Join( p1, p2 ) )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get parent directory name
    /// </summary>
    /// <param name="p1">
    ///   Directory name.
    /// </param>
    /// <returns>
    ///   Parent directory name of specified path.
    /// </param>
    static member GetParentName ( p1 : string ) : string =
        Functions.AppendPathName p1 ".."

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Optimize path name.
    /// </summary>
    /// <param name="p1">
    ///   Directory name that may contain ".." or "." or empty directory name( "\\" )
    /// </param>
    /// <returns>
    ///   Optimized path name.
    /// </param>
    static member OptimizePathName ( p1 : string ) : string =
        if p1.Length <= 0 then
            ""
        else
            let r1 = Path.GetPathRoot p1
            let p2 = string( Path.DirectorySeparatorChar ) + p1.[ r1.Length .. ]
            let p3 = Path.GetFullPath p2
            let r2 = Path.GetPathRoot p3
            Path.Join( r1, p3.[ r2.Length .. ] )

    /// <summary>
    ///  Do specified function at f, if an exception is raised, retry max 10 times.
    /// </summary>
    /// <param name="f">
    ///  Function have to do. If this functions returns Error("...") or raise an exception, retry will continue.
    ///  Otherwise, exit retry loop.
    /// </param>
    /// <param name="e">
    ///  Function to check if an exception has occurred that should abort the retry immediately.
    ///  If this function returns true, retry will be aborted.
    /// </param>
    /// <returns>
    ///  Execution result of function f, or error message.
    /// </returns>
    static member RetryAsync1 ( f : unit -> Task<Result<'a, string>> ) ( e : Exception -> bool ) : Task<Result<'a, string>> =
        let rec loop ( cnt : int ) =
            task {
                try
                    match! f() with
                    | Ok( x ) ->
                        return Ok( x )
                    | Error( x ) ->
                        if cnt = 9 then
                            return Error( x )
                        else
                            do! Task.Delay 10
                            return! loop ( cnt + 1 )
                with
                | _ as x ->
                    if cnt = 9 || e x then
                        return Error( x.Message )
                    else
                        do! Task.Delay 10
                        return! loop ( cnt + 1 )
            }
        loop 0

    /// <summary>
    ///  Do specified function at f, if an exception is raised, retry max 10 times.
    /// </summary>
    /// <param name="f">
    ///  Function have to do. If this functions raise an exception, retry will continue.
    /// </param>
    /// <param name="e">
    ///  Function to check if an exception has occurred that should abort the retry immediately.
    ///  If this function returns true, retry will be aborted.
    /// </param>
    /// <returns>
    ///  Execution result of function f, or error message.
    /// </returns>
    static member RetryAsync2 ( f : unit -> Task<'a> ) ( e : Exception -> bool ) : Task<Result<'a, string>> =
        let wf() = task {
            let! wr = f()
            return Ok( wr )
        }
        Functions.RetryAsync1 wf e

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get OK value of Result type value. If specified value is error, it returns default value.
    /// </summary>
    /// <param name="d">
    ///   Default value.
    /// </param>
    /// <param name="r">
    ///   Result type value.
    /// </param>
    /// <returns>
    ///   Ok value, or default value.
    /// </returns>
    static member GetOkValue ( d : 'a ) ( r : Result<'a,'b> ) : 'a =
        match r with
        | Ok( x ) -> x
        | Error( _ ) -> d

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get Error value of Result type value. If specified value is Ok, it returns default value.
    /// </summary>
    /// <param name="d">
    ///   Default value.
    /// </param>
    /// <param name="r">
    ///   Result type value.
    /// </param>
    /// <returns>
    ///   Error value, or default value.
    /// </returns>
    static member GetErrorValue ( d : 'b ) ( r : Result<'a,'b> ) : 'b =
        match r with
        | Ok( x_ ) -> d
        | Error( x ) -> x

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Create directory. If failed to create, retry max 10 times.
    /// </summary>
    /// <param name="p">
    ///   Directory name.
    /// </param>
    /// <returns>
    ///   If failed, it returns error message. Otherwise None.
    /// </returns>
    static member CreateDirectoryAsync ( p : string ) : Task< Result<unit, string> > =
        Functions.RetryAsync2
            ( fun () -> task { Directory.CreateDirectory p |> ignore } )
            ( function
                | :? UnauthorizedAccessException
                | :? ArgumentException
                | :? PathTooLongException
                | :? NotSupportedException -> true
                | _ -> false )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Delete directory. If failed to delete, retry max 10 times.
    /// </summary>
    /// <param name="p">
    ///   Directory name.
    /// </param>
    /// <returns>
    ///   If failed, it returns error message. Otherwise None.
    /// </returns>
    static member DeleteDirectoryAsync ( p : string ) : Task< Result<unit, string> > =
        Functions.RetryAsync2
            ( fun () -> task {
                try
                    Directory.Delete p
                with
                | :? DirectoryNotFoundException -> ()
                | _ as x -> raise x
            } )
            ( function
                | :? Security.SecurityException
                | :? UnauthorizedAccessException
                | :? ArgumentException
                | :? PathTooLongException
                | :? NotSupportedException -> true
                | _ -> false )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Delete all of sub directories and files in specified directory. If failed to delete, retry max 10 times.
    /// </summary>
    /// <param name="p">
    ///   Directory name.
    /// </param>
    /// <returns>
    ///   If failed, it returns error message. Otherwise Result.Ok.
    /// </returns>
    static member DeleteDirectoryRecursvelyAsync ( p : string ) : Task< Result<unit, string> > =
        task {
            try
                let fexist = File.Exists p
                let dexist = Directory.Exists p
                if ( not fexist ) && ( not dexist ) then
                    // If specified directory or file is not exist,
                    // it is assumed that specified one had been deleted in normaly.
                    return Result.Ok()
                elif dexist then
                    // Delete sub files
                    let subfiles = Directory.GetFiles p
                    let rv1 = Array.zeroCreate< Result<unit, string> >( subfiles.Length )
                    for i = 0 to  subfiles.Length - 1 do
                        let! wr = Functions.DeleteFileAsync ( subfiles.[i] )
                        rv1.[i] <- wr

                    // If one or more error was occurred, it failed to delete directory.
                    match Array.tryFind Result.isError rv1 with
                    | Some( x ) ->
                        // Return first error information, ignore others.
                        return x
                    | None ->
                        // Delete sub directories
                        let subdirs = Directory.GetDirectories p
                        let rv2 = Array.zeroCreate< Result<unit, string> >( subdirs.Length )
                        for i = 0 to  subdirs.Length - 1 do
                            let! wr = Functions.DeleteDirectoryRecursvelyAsync ( subdirs.[i] )
                            rv2.[i] <- wr
                        match Array.tryFind Result.isError rv2 with
                        | Some x ->
                            // Return first error information, ignore others.
                            return x
                        | None ->
                            // All of subdirectory or files were deleted in normaly.
                            // Delete specified directory.
                            return! Functions.DeleteDirectoryAsync p
                else
                    // If specified object is exist but directory, it is considered an error.
                    return Result.Error( sprintf "Specified name (%s) is not directory." p )
            with
            | :? Security.SecurityException
            | :? UnauthorizedAccessException
            | :? PathTooLongException
            | :? ArgumentException
            | :? ArgumentNullException as x ->
                return Result.Error( x.Message )
        }


    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Read file containts. If failed to read, retry max 10 times.
    /// </summary>
    /// <param name="p">
    ///   file name.
    /// </param>
    /// <returns>
    ///   If failed, it returns error message. Otherwise byte arrayy.
    /// </returns>
    static member ReadAllBytesAsync ( p : string ) : Task<Result<byte[], string>> =
        Functions.RetryAsync2
            ( fun () -> File.ReadAllBytesAsync p )
            ( function
                | :? DirectoryNotFoundException
                | :? FileNotFoundException
                | :? UnauthorizedAccessException
                | :? ArgumentException
                | :? PathTooLongException
                | :? NotSupportedException -> true
                | _ -> false )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Read file containts. If failed to read, retry max 10 times.
    /// </summary>
    /// <param name="p">
    ///   file name.
    /// </param>
    /// <returns>
    ///   If failed, it returns error message. Otherwise read string.
    /// </returns>
    static member ReadAllTextAsync ( p : string ) : Task<Result<string, string>> =
        Functions.RetryAsync2
            ( fun () -> File.ReadAllTextAsync p )
            ( function
                | :? DirectoryNotFoundException
                | :? FileNotFoundException
                | :? UnauthorizedAccessException
                | :? ArgumentException
                | :? PathTooLongException
                | :? NotSupportedException -> true
                | _ -> false )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Write file containts. If failed to write, retry max 10 times.
    /// </summary>
    /// <param name="p">
    ///   file name.
    /// </param>
    /// <param name="v">
    ///   Bytes data, it will be written.
    /// </param>
    /// <returns>
    ///   If failed, it returns error message. Otherwise None.
    /// </returns>
    static member WriteAllBytesAsync ( p : string ) ( v : byte[] ) : Task<Result<unit, string>> =
        Functions.RetryAsync2
            ( fun () -> task {
                do! File.WriteAllBytesAsync( p, v )
                return ()
            })
            ( function
                | :? DirectoryNotFoundException
                | :? UnauthorizedAccessException
                | :? ArgumentException
                | :? PathTooLongException
                | :? NotSupportedException -> true
                | _ -> false )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Write file containts. If failed to write, retry max 10 times.
    /// </summary>
    /// <param name="p">
    ///   file name.
    /// </param>
    /// <param name="v">
    ///   String data, it will be written.
    /// </param>
    /// <returns>
    ///   If failed, it returns error message. Otherwise None.
    /// </returns>
    static member WriteAllTextAsync ( p : string ) ( v : string ) : Task<Result<unit, string>> =
        Functions.RetryAsync2
            ( fun () -> task {
                do! File.WriteAllTextAsync( p, v )
                return ()
            })
            ( function
                | :? DirectoryNotFoundException
                | :? UnauthorizedAccessException
                | :? ArgumentException
                | :? PathTooLongException
                | :? NotSupportedException -> true
                | _ -> false )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Delete file. If failed to delete, retry max 10 times.
    /// </summary>
    /// <param name="p">
    ///   file name.
    /// </param>
    /// <returns>
    ///   If failed, it returns error message. Otherwise None.
    /// </returns>
    static member DeleteFileAsync ( p : string ) : Task<Result<unit, string>> =
        Functions.RetryAsync2
            ( fun () -> task{ File.Delete p } )
            ( function
                | :? DirectoryNotFoundException
                | :? UnauthorizedAccessException
                | :? ArgumentException
                | :? PathTooLongException
                | :? NotSupportedException -> true
                | _ -> false )
    
    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Repeat the specified function until the condition is met.
    /// </summary>
    /// <param name="f">
    ///   Function. It accept one argument thet holds current status.
    ///   This function returns pair of boolean value and next status,
    ///   If this function returns false at boolean value, terminate repeat.
    /// </param>
    /// <param name="init">
    ///   Initial status value.
    /// </param>
    /// <returns>
    ///   Last status value, it returns with false at boolean value.
    /// </returns>
    /// <code>
    ///   > let f2 ( a : int ) =
    ///       task {
    ///         do! Task.Delay 100
    ///         printfn "a=%d" a
    ///         return struct ( not( a = 3 ), a + 1 )
    ///       }
    ///   > ( loopAsyncWithState f2 0 ).Result;;
    ///   a=0
    ///   a=1
    ///   a=2
    ///   a=3
    ///   val it: int = 4
    /// </code>
    static member loopAsyncWithState ( f : ( 'a -> Task<struct( bool * 'a )> ) ) ( init : 'a ) : Task<'a> =
        task {
            let mutable state = init
            let mutable flg = true
            while flg do
                let! struct( wflg, wstate ) = f state
                flg <- wflg
                state <- wstate
            return state
        }

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Repeat the specified function until the function f returns false.
    /// </summary>
    /// <param name="f">
    ///   Function that is called in repeatedly. If this function returns false, terminate caling.
    /// </param>
    /// <returns>
    ///   Task.
    /// </returns>
    /// <code>
    ///   > let r = Random()
    ///     let f1 () =
    ///       task {
    ///         do! Task.Delay 100
    ///         let w = r.Next() % 3
    ///         printfn "%d" w
    ///         return w = 0
    ///       }
    ///   > Task.WaitAll( loopAsync f1 );;
    ///   0
    ///   1
    ///   val it: unit = ()
    /// </code>
    static member loopAsync ( f : unit -> Task<bool> ) : Task =
        task {
            let mutable flg = true
            while flg do
                let! w = f()
                flg <- w
        }

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Repeat the specified function until the function return LoopState.Terminate value.
    ///   Function arguments are given by LoopState.Continue value. And Function return values are given by LoopState.Terminate.
    /// </summary>
    /// <param name="f">
    ///   Function. It accept one argument.
    ///   This function returns LoopState.Continue or LoopState.Terminate value.
    /// </param>
    /// <param name="init">
    ///   Initial argument value.
    /// </param>
    /// <returns>
    ///   Given value of LoopState.Terminate
    /// </returns>
    /// <code>
    ///   let loop1 ( s : int ) : Task<LoopState<int, string>> =
    ///     task {
    ///       if s < 10 then
    ///         return Continue( s + 1 )
    ///       else
    ///         return Terminate( sprintf "%d" s )
    ///     }
    ///   let! r = loopAsyncWithArgs loop1 0
    ///   printfn "r = %s" r
    /// </code>
    static member loopAsyncWithArgs ( f : ( 'a -> Task<LoopState<'a,'b>> ) ) ( init : 'a ) : Task<'b> =
        task {
            let mutable args = init
            let mutable flg = true
            let mutable r : 'b voption = ValueNone
            while flg do
                match! f args with
                | Continue( a ) ->
                    flg <- true
                    args <- a
                | Terminate( b ) ->
                    flg <- false
                    r <- ValueSome b
            return r.Value
        }

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Start task in synchronously. And wait for the task termination.
    /// </summary>
    /// <param name="t">
    ///   Task
    /// </param>
    /// <returns>
    ///   Task result.
    /// </returns>
    /// <remarks>
    ///   If the task raise an exception, that exception will be reraised.
    /// </remarks>
    static member RunTaskSynchronously ( t : Task<'a> ) : 'a =
        try
            t.Result
        with
        | :? AggregateException as x ->
            raise <| x.InnerException

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Start task in synchronously. And wait for the task termination.
    /// </summary>
    /// <param name="t">
    ///   Task
    /// </param>
    /// <remarks>
    ///   If the task raise an exception, that exception will be reraised.
    /// </remarks>
    static member RunTaskSynchronously ( t : Task ) : unit =
        try
            Task.WaitAll( t )
        with
        | :? AggregateException as x ->
            raise <| x.InnerException

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Start task in asynchronously, not wait for the task termination.
    /// </summary>
    /// <param name="t">
    ///   Task
    /// </param>
    /// <remarks>
    ///   The Task "t" must not raise any exceptions.
    ///   This case is not expected so calling process will be aborted.
    /// </remarks>
    static member StartTask ( t : ( unit -> Task<unit> ) ) : unit =
        task {
            try
                do! Task.Yield()
                do! t()
            with
            | _ ->
                exit( 1 )
        }
        |> ignore

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Start task in asynchronously, not wait for the task termination.
    /// </summary>
    /// <param name="t">
    ///   Task
    /// </param>
    static member StartTask ( t : ( unit -> Task ) ) : unit =
        task {
            try
                do! Task.Yield()
                do! t()
            with
            | _ ->
                exit( 1 )
        }
        |> ignore

    // ----------------------------------------------------------------------------
    /// <summary>
    ///  Receive data which byte count is specified in argument, from specified network connection.
    /// </summary>
    /// <param name="s">
    ///  Network connection
    /// </param>
    /// <param name="len">
    ///  Bytes count that should be received.
    /// </param>
    /// <returns>
    ///  Received bytes array.
    /// </returns>
    static member ReceiveBytesFromNetwork ( s : Stream ) ( len : int ) : Task< byte array > =
        let buf = Array.zeroCreate<byte>( len )
        let readPartData ( stat : int ) =
            task {
                let! ww = s.ReadAsync( buf, stat, len - stat )
                if ww = 0 then
                    raise <| IOException( "Connection closed." )
                return struct ( ( ww <> 0 && stat + ww < len ), stat + ww )
            }
        task {
            if len > 0 then
                let! _ = Functions.loopAsyncWithState readPartData 0
                ()
            return buf
        }

    // ----------------------------------------------------------------------------
    /// <summary>
    ///  Run one or more tasks in sequencialy.
    /// </summary>
    /// <param name="v">
    ///  Tasks. ( array of functions that returns a task. )
    /// </param>
    /// <returns>
    ///  Array of task's results.
    /// </returns>
    /// <remarks>
    ///   If the task raise an exception, that exception will be reraised.
    /// </remarks>
    static member RunTaskInSequencial ( v : ( unit -> Task<'T> )[] ) : Task<'T []> =
        task {
            let rv = Array.zeroCreate<'T>( v.Length )
            for i = 0 to v.Length - 1 do
                let wt = v.[i]()
                let! w = wt.WaitAsync( CancellationToken.None )
                rv.[i] <- w
            return rv
        }

    // ----------------------------------------------------------------------------
    /// <summary>
    ///  Ignore the results of task.
    /// </summary>
    /// <param name="v">
    ///  A task.
    /// </param>
    /// <returns>
    ///  A task, that results is unit.
    /// </returns>
    static member TaskIgnore ( v : Task<'T> ) : Task<unit> =
        task {
            let! _ = v
            ()
        }

    // ----------------------------------------------------------------------------
    /// <summary>
    ///  Run one or more tasks in pallalel.
    /// </summary>
    /// <param name="v">
    ///  Tasks. ( array of functions that returns a task. )
    /// </param>
    /// <returns>
    ///  Array of task's results.
    /// </returns>
    static member RunTaskInPallalel ( v : ( unit -> Task<'T> ) [] ) : Task<'T []> =
        task {
            let rv = Array.zeroCreate<'T>( v.Length )
            let wtv = v |> Array.map ( fun itr -> itr() )
            for i = 0 to v.Length - 1 do
                let! w = wtv.[i].WaitAsync( CancellationToken.None )
                rv.[i] <- w
            return rv
        }

    // ----------------------------------------------------------------------------
    /// <summary>
    ///  Convert multiple sequences indexed by key to single sequence.
    /// </summary>
    /// <param name="v">
    ///  Array of sequences. The sequence must be indexed by some value.
    /// </param>
    /// <param name="getIdx">
    ///  Function that produces the index from element of sequence.
    /// </param>
    /// <param name="getVal">
    ///  Function that produces the value from element of sequence.
    /// </param>
    /// <returns>
    ///  Integrated sequence.
    /// </returns>
    /// <code>
    ///  let s1 = seq { ( 1, 10 ); ( 3, 30 ); }
    ///  let s2 = seq { ( 2, -20 ); ( 1, -10 ); }
    ///  let s3 = seq { ( 8, 800 ); ( 2, 200 ); }
    ///  let sr = PairByIndex [| s1; s2; s3 |] fst snd
    ///  sr = seq [
    ///    ( 1, [| Some 10; Some -10; None     |] );
    ///    ( 2, [| None;    Some -20; Some 200 |] );
    ///    ( 3, [| Some 30; None;     None     |]);
    ///    ( 8, [| None;    None;     Some 800 |]);
    ///  ]
    /// </code>
    static member PairByIndex ( v : 'a seq [] ) ( getIdx : ( 'a -> 'b ) ) ( getVal : ( 'a -> 'c ) ) : ( 'b * 'c option [] ) seq =
        let d = Dictionary< 'b, 'c option [] >()
        v
        |> Array.iteri ( fun idx itr ->
            itr
            |> Seq.iter ( fun itr2 ->
                let widx = getIdx itr2
                let wval = getVal itr2
                let f, r = d.TryGetValue widx
                if f then
                    r.[idx] <- Some wval
                else
                    let wv = Array.zeroCreate< 'c option >( v.Length )
                    wv.[idx] <- Some wval
                    d.Add( widx, wv )
            )
        )
        d
        |> Seq.sortBy _.Key
        |> Seq.map ( fun i -> i.Key, i.Value )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///  Generate unique number.
    /// </summary>
    /// <param name="addr">
    ///  The function that is used for adding a number.
    /// </param>
    /// <param name="d">
    ///  Default value, that is returned if it failed to determine unique number.
    /// </param>
    /// <param name="v">
    ///  The sequence of already used numbers.
    /// </param>
    /// <returns>
    ///  Determined unique number, or default value specified at argument 'd'.
    /// </returns>
    /// <code>
    ///  > Functions.GenUniqueNumber ( (+) 1 ) 0 [| 0; 1; 2 |];;
    ///  val it: int = 3
    ///  > GenUniqueNumber ( (+) 1uy ) 10uy [| 0uy .. 255uy |];;
    ///  val it: byte = 10uy
    /// </code>
    static member GenUniqueNumber ( addr : 'a -> 'a ) ( d : 'a ) ( v : 'a seq ) : 'a =
        let vlen = Seq.length v
        let rec loop ( cnt : int ) ( i : 'a ) =
            if cnt >= vlen then
                d
            elif Seq.contains i v then
                loop ( cnt + 1 ) ( addr i ) 
            else
                i
        if Seq.isEmpty v then
            d
        else
            v
            |> Seq.max
            |> addr
            |> loop 0

    // ----------------------------------------------------------------------------
    /// <summary>
    ///  Write byte count and string data to the stream.
    /// </summary>
    /// <param name="s">
    ///  The stream to which data is written.
    /// </param>
    /// <param name="d">
    ///  The string data to be written.
    /// </param>
    static member FramingSender ( s : Stream ) ( d : string ) : Task =
        task {
            let plainData = Encoding.UTF8.GetBytes d
            let headerBytes =
                plainData.Length
                |> IPAddress.HostToNetworkOrder
                |> BitConverter.GetBytes 
            do! s.WriteAsync( headerBytes, 0, headerBytes.Length )
            do! s.WriteAsync( plainData, 0, plainData.Length )
        }

    // ----------------------------------------------------------------------------
    /// <summary>
    ///  Receive string data written by FramingSender function from the stream.
    /// </summary>
    /// <param name="s">
    ///  The stream to which data is read.
    /// </param>
    /// <returns>
    ///  Received string.
    /// </returns>
    static member FramingReceiver ( s : Stream ) : Task<string> =
        task {
            let! headerBytes = Functions.ReceiveBytesFromNetwork s ( sizeof<int> )
            let dataLen =
                BitConverter.ToInt32( headerBytes, 0 )
                |> IPAddress.NetworkToHostOrder
            let! plainData = Functions.ReceiveBytesFromNetwork s dataLen
            return Encoding.UTF8.GetString( plainData )
        }

    /// <summary>
    ///  Check media access range.
    /// </summary>
    /// <param name="sBlkPos">
    ///  Access start position in block.
    /// </param>
    /// <param name="trBytes">
    ///  Access length in bytes.
    /// </param>
    /// <param name="mediaBlks">
    ///  Usable media block count.
    /// </param>
    /// <param name="blkSize">
    ///  Block size in bytes.
    /// </param>
    /// <returns>
    ///  If all of access range is in the media, it returns true, otherwise false.
    /// </returns>
    static member CheckAccessRange ( sBlkPos : uint64 ) ( trBytes : uint64 ) ( mediaBlks : uint64 ) ( blkSize : uint64 ) : bool =
        let struct ( d, r ) = Math.DivRem( trBytes, blkSize )
        let trBlks = if r > 0UL then d + 1UL else d
        let eBlkPos = sBlkPos + trBlks
        ( sBlkPos <= mediaBlks && trBlks <= mediaBlks && eBlkPos <= mediaBlks && eBlkPos >= sBlkPos && eBlkPos >= trBlks )


    /// <summary>
    ///  Convert option type value to ValueOption.
    /// </summary>
    /// <param name="v">
    ///  option type value
    /// </param>
    /// <returns>
    ///  Converted ValueOption type value.
    /// </returns>
    static member OptToValOpt ( v : 'T option ) : 'T ValueOption =
        match v with
        | Some x -> ValueSome x
        | None -> ValueNone

    /// <summary>
    ///  Convert ValueOption type value to option.
    /// </summary>
    /// <param name="v">
    ///  ValueOption type value.
    /// </param>
    /// <returns>
    ///  Converted option type value.
    /// </returns>
    static member ValOptToOpt ( v : 'T ValueOption ) : 'T option =
        match v with
        | ValueSome x -> Some x
        | ValueNone -> None

    /// <summary>
    ///  Generate FrozenDictionary object.
    /// </summary>
    /// <param name="v">
    ///  The sequence of pair of key and value.
    /// </param>
    /// <returns>
    ///  Created FrozenDictionary object that holds key-value pairs specified at argument 'v'. 
    /// </returns>
    static member ToFrozenDictionary< 'T1, 'T2 > ( v : ( 'T1 * 'T2 ) seq ) : FrozenDictionary< 'T1, 'T2 > =
        let w = v |> Seq.map KeyValuePair
        w.ToFrozenDictionary()

    /// <summary>
    ///  Convert Haruka.IODataTypes.TargetDeviceCtrlRes.T_ITNEXUS to ITNexus type.
    /// </summary>
    /// <param name="a">
    ///  Haruka.IODataTypes.TargetDeviceCtrlRes.T_ITNEXUS value should be convert to ITNexus.
    /// </param>
    /// <returns>
    ///  Converted ITNexus value.
    /// </returns>
    static member ConvertITNexus ( a : Haruka.IODataTypes.TargetDeviceCtrlRes.T_ITNEXUS ) : ITNexus =
        new ITNexus( a.InitiatorName, a.ISID, a.TargetName, a.TPGT )

    /// <summary>
    ///  Compare string sequence.
    /// </summary>
    /// <param name="a">
    ///  string sequence 1
    /// </param>
    /// <param name="b">
    ///  string sequence 2
    /// </param>
    /// <returns>
    ///  If a < b, it returns negative value. If a > b, it returns positive value. Otherwise 0.
    /// </returns>
    /// <code>
    ///  > CompareMultiLevelKey [| "a" |] [| "a" |];;
    ///  val it: int = 0
    ///  > CompareMultiLevelKey [| "b" |] [| "a" |];;
    ///  val it: int = 1
    ///  > CompareMultiLevelKey [| "a"; "c" |] [| "a" |];;
    ///  val it: int = 1
    ///  > CompareMultiLevelKey [| "a" |] [| "a"; "c" |];;
    ///  val it: int = -1
    /// </code>
    static member CompareMultiLevelKey ( a : string seq ) ( b : string seq ) : int =
        let rec loop ( ae : IEnumerator<string> ) ( be : IEnumerator<string> ) : int =
            match ae.MoveNext(), be.MoveNext() with
            | false, false ->
                0
            | true, false ->
                1
            | false, true ->
                -1
            | _ ->
                let w = String.Compare( ae.Current, be.Current, StringComparison.Ordinal )
                if w = 0 then
                    loop ae be
                elif w < 0 then
                    -1
                else
                    1
        loop ( a.GetEnumerator() ) ( b.GetEnumerator() )


    /// <summary>
    ///  Searches for a value in the dictionary and applies a transformation function to the found value.
    /// </summary>
    /// <param name="d">
    ///  Dictionary object
    /// </param>
    /// <param name="s">
    ///  Search key.
    /// </param>
    /// <param name="pf">
    ///  Transformation function.
    /// </param>
    /// <param name="dv">
    ///  Default value, it will be returned if an error is occurred.
    /// </param>
    /// <returns>
    ///  Converted value, or default value.
    /// </returns>
    static member SearchAndConvert ( d : IReadOnlyDictionary< 'a, 'b > ) ( s : 'a ) ( pf : ( 'b -> 'c ) ) ( dv : 'c ) : 'c =
        try
            d.Item s
            |> pf
        with
        | _ -> dv




