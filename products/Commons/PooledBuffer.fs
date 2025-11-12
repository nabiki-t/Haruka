//=============================================================================
// Haruka Software Storage.
// PooledBuffer.fs : Holds buffers allocated by ArrayPool.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Constants

//=============================================================================
// Import declaration

open System
open System.Buffers
open System.Threading

//=============================================================================
// Class implementation

/// <summary>
///  An alias to indicate that this is a buffer allocated from an ArrayPool.
/// </summary>
/// <param name="argBuffer">
///  The buffer allocated from the ArrayPool.
/// </param>
/// <param name="argLength">
///  The size requested when allocating the buffer, which may differ from the size actually allocated.
/// </param>
type PooledBuffer private ( argBuffer : byte[], argLength : int ) =

    /// Allocated buffer. After release, it references an array of length 0.
    let mutable m_Buffer = argBuffer

    /// Requested buffer length.
    let mutable m_Length = argLength

    //=========================================================================
    // Public method

    /// <summary>
    ///  Release buffer.
    /// </summary>
    member _.Return() : unit =
        let v = Interlocked.Exchange( &m_Buffer, Array.Empty<byte>() )
        if v.Length > 0 then
            ArrayPool<byte>.Shared.Return v
        m_Length <- 0

    /// <summary>
    ///  Duplicating part of the buffer.
    /// </summary>
    /// <param name="s">
    ///  The start position of the range to be duplicated.</param>
    /// <param name="e">
    ///  End position of the range to be duplicated.
    /// </param>
    /// <returns>
    ///  Duplicated array. This array is not allocated by ArrayBuffer.
    /// </returns>
    member _.GetPartialBytes ( s : int ) ( e : int ) : byte[] =
        m_Buffer.[ s .. e ]

    /// <summary>
    ///  Returns an ArraySegment that references a portion of the buffer.
    /// </summary>
    /// <param name="s">
    ///  The start position of the buffer to reference.
    /// </param>
    /// <param name="len">
    ///  The length of the range to reference.
    /// </param>
    /// <returns>
    ///  An ArraySegment that references the specified range of the buffer.
    /// </returns>
    member _.GetArraySegment ( s : int ) ( len : int ) : ArraySegment<byte> =
        ArraySegment<byte>( m_Buffer, s, len )

    /// Returns a reference to the buffer.
    /// Do not return a referenced array using ArrayPoo.Return,
    /// as doing so may result in the array being returned twice.
    member _.Array = m_Buffer

    /// Returns the length requested when allocating the buffer. Same as Count property.
    member _.Length = m_Length

    /// Returns the length requested when allocating the buffer. Same as Length property.
    member _.Count = m_Length

    /// Returns an ArraySegment that references the entire buffer.
    member _.ArraySegment = ArraySegment<byte>( m_Buffer, 0, m_Length )

    /// Accessing elements of a buffer.
    member _.Item with get ( idx : int ) = m_Buffer.[ idx ]

    /// Buffer is allocated of not.
    member _.IsEmpty() = ( Array.isEmpty m_Buffer )

    /// <summary>
    ///  Truncate buffer size.
    /// </summary>
    /// <param name="s">
    ///  Specifies the number of bytes to truncate the buffer length to.
    /// </param>
    /// <remarks>
    ///  If the value of s is greater than the buffer length (request size), nothing is done.
    ///  If it is less than 0, the buffer length is truncated to 0.
    /// </remarks>
    member this.Truncate ( s : int ) : unit =
        let o = m_Length    // old value
        let n =
            min o s     // new value
            |> max 0
        let r = Interlocked.CompareExchange( &m_Length, n, o )
        if r <> o then this.Truncate s

    //=========================================================================
    // static method

    /// <summary>
    ///  Returns the length requested when allocating the buffer.
    /// </summary>
    /// <param name="v">
    ///  The buffer for which the requested length is to be obtained.
    /// </param>
    /// <returns>
    ///  The length requested when allocating the buffer.
    /// </returns>
    static member length ( v : PooledBuffer ) : int =
        v.Length

    /// <summary>
    ///  Returns the length requested when allocating the buffer.
    /// </summary>
    /// <param name="v">
    ///  The buffer for which the requested length is to be obtained.
    /// </param>
    /// <returns>
    ///  the length requested when allocating the buffer.
    /// </returns>
    static member ulength ( v : PooledBuffer ) : uint32 =
        uint32 v.Length

    /// <summary>
    ///  Whether the individual values ​​in the buffer match.
    /// </summary>
    /// <param name="v1">
    ///  The targets for comparison 1.
    /// </param>
    /// <param name="v2">
    ///  The targets for comparison 2.
    /// </param>
    /// <returns>
    ///  Returns true if all values ​​in the buffer match, false otherwise.
    /// </returns>
    static member ValueEquals( v1 : PooledBuffer ) ( v2 : PooledBuffer ) : bool =
        if v1.Length <> v2.Length then
            false
        else
            let s1 = Seq.truncate v1.Length v1.Array
            let s2 = Seq.truncate v2.Length v2.Array
            0 = Seq.compareWith ( fun a1 a2 -> int ( a1 - a2 ) ) s1 s2

    /// <summary>
    ///  Whether the individual values ​​in the buffer match.
    /// </summary>
    /// <param name="v1">
    ///  The targets for comparison 1.
    /// </param>
    /// <param name="v2">
    ///  The targets for comparison 2.
    /// </param>
    /// <returns>
    ///  Returns true if all values ​​in the buffer match, false otherwise.
    /// </returns>
    static member ValueEqualsWithArray( v1 : PooledBuffer ) ( v2 : byte[] ) : bool =
        if v1.Length <> v2.Length then
            false
        else
            let s1 = Seq.truncate v1.Length v1.Array
            0 = Seq.compareWith ( fun a1 a2 -> int ( a1 - a2 ) ) s1 v2

    /// <summary>
    ///  Get buffer from ArrayPool
    /// </summary>
    /// <param name="s">
    ///  Required buffer size.
    /// </param>
    /// <returns>
    ///  Allocated buffer. This buffer is not initialized.
    /// </returns>
    /// <remarks>
    ///  If the requested buffer length is less than 0, a zero-length buffer is allocated.
    /// </remarks>
    static member Rent ( s : int ) : PooledBuffer =
        if s <= 0 then
            PooledBuffer.Empty
        else
            PooledBuffer( ArrayPool<byte>.Shared.Rent s, s )

    /// <summary>
    ///  Get buffer from ArrayPool
    /// </summary>
    /// <param name="s">
    ///  Required buffer size.
    /// </param>
    /// <returns>
    ///  Allocated and zero cleared buffer.
    ///  If the requested buffer length is less than 0, a zero-length buffer is allocated.
    /// </returns>
    static member RentAndInit ( s : int ) : PooledBuffer =
        if s <= 0 then
            PooledBuffer.Empty
        else
            let r = PooledBuffer.Rent s
            for i = 0 to s - 1 do
                r.Array.[i] <- 0uy
            r

    /// <summary>
    ///  Allocate buffer from ArrayPool and initialize contents from specified array.
    /// </summary>
    /// <param name="v">
    ///  array that holds initial contents.
    /// </param>
    /// <returns>
    ///  Allocated buffer.
    /// </returns>
    static member Rent ( v : byte[] ) : PooledBuffer =
        if v.Length = 0 then
            PooledBuffer.Empty
        else
            let b = ArrayPool<byte>.Shared.Rent v.Length
            Array.blit v 0 b 0 v.Length
            PooledBuffer( b, v.Length )

    /// <summary>
    ///  Allocate buffer from ArrayPool and initialize contents from specified array.
    /// </summary>
    /// <param name="v">
    ///  array that holds initial contents.
    /// </param>
    /// <param name="nlen">
    ///  The new length of the array.
    /// </param>
    /// <returns>
    ///  Allocated buffer.
    /// </returns>
    /// <remarks>
    ///  If the value of nlen is greater than the length of the original array v, an expanded array is allocated.
    ///  The expanded part is not initialized.
    ///  If the requested buffer length is less than 0, a zero-length buffer is allocated.
    /// </remarks>
    static member Rent ( v : byte[], nlen : int ) : PooledBuffer =
        if nlen <= 0 then
            PooledBuffer.Empty
        else
            let b = ArrayPool<byte>.Shared.Rent nlen
            Array.blit v 0 b 0 ( min v.Length nlen )
            PooledBuffer( b, nlen )

    /// <summary>
    ///  Allocate buffer from ArrayPool and initialize contents from specified array.
    /// </summary>
    /// <param name="v">
    ///  array that holds initial contents.
    /// </param>
    /// <param name="nlen">
    ///  The new length of the array.
    /// </param>
    /// <returns>
    ///  Allocated buffer.
    /// </returns>
    /// <remarks>
    ///  If the value of nlen is greater than the length of the original array v, an expanded array is allocated.
    ///  The expanded part is not initialized.
    ///  If the requested buffer length is less than 0, a zero-length buffer is allocated.
    /// </remarks>
    static member Rent ( v : PooledBuffer, nlen : int ) : PooledBuffer =
        if nlen <= 0 then
            PooledBuffer.Empty
        else
            let b = ArrayPool<byte>.Shared.Rent nlen
            Array.blit v.Array 0 b 0 ( min v.Length nlen )
            PooledBuffer( b, nlen )

    /// <summary>
    ///  Release a buffer.
    /// </summary>
    /// <param name="v">
    ///  PooledBuffer.
    /// </param>
    static member Return( v : PooledBuffer ) : unit =
        v.Return()

    /// <summary>
    ///  Release buffers.
    /// </summary>
    /// <param name="s">
    ///  Sequense of PooledBuffers
    /// </param>
    static member Return( s : PooledBuffer seq ) : unit =
        s
        |> Seq.filter ( fun itr -> itr.Array.Length > 0 )
        |> Seq.iter PooledBuffer.Return

    /// Empty
    static member Empty : PooledBuffer =
        PooledBuffer( Array.Empty<byte>(), 0 )

    /// Buffer is allocated or not.
    static member IsEmpty ( v : PooledBuffer ) : bool =
        v.IsEmpty()



