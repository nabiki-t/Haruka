//=============================================================================
// Haruka Software Storage.
// Typedefs.fs : Defines miscellaneous data types that is used in SCSI commonly.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Constants

//=============================================================================
// Import declaration

open System
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Buffers

//=============================================================================
// Type definition

/// <summary>
///  An alias to indicate that this is a buffer allocated from an ArrayPool.
/// </summary>
/// <param name="m_Buffer">
///  The buffer allocated from the ArrayPool.
/// </param>
/// <param name="m_Length">
///  The size requested when allocating the buffer, which may differ from the size actually allocated.
/// </param>
[<CustomEquality; NoComparison; Struct; IsReadOnly;>]
type PooledBuffer(
    m_Buffer : byte[],
    m_Length : int ) =


    //=========================================================================
    // Interface method

    // Imprementation of IEquatable interface
    interface IEquatable<PooledBuffer> with

        /// <summary>
        ///  Equals method. 
        ///  If they refer to the same buffer and have the same requested size, they are determined to be the same.
        /// </summary>
        /// <param name="v">
        ///  The targets for comparison.
        /// </param>
        /// <returns>
        ///  True if this object and 'v' can be considered equal, false otherwise.
        /// </returns>
        member _.Equals( v : PooledBuffer ) : bool =
            Object.ReferenceEquals( m_Buffer, v.Array ) && m_Length = v.Length

    // Imprementation of IEqualityComparer interface
    interface IEqualityComparer<PooledBuffer> with

        /// <summary>
        ///  Equals method. 
        ///  If they refer to the same buffer and have the same requested size, they are determined to be the same.
        /// </summary>
        /// <param name="v1">
        ///  The targets for comparison 1.
        /// </param>
        /// <param name="v2">
        ///  The targets for comparison 2.
        /// </param>
        /// <returns>
        ///  True if 'v1' and 'v2' can be considered equal, false otherwise.
        /// </returns>
        member _.Equals ( v1 : PooledBuffer, v2 : PooledBuffer ) : bool =
            Object.ReferenceEquals( v1.Array, v2.Array ) && v1.Length = v2.Length

        /// <summary>
        ///  Returns hash value.
        /// </summary>
        /// <param name="v">
        ///  The object for which the hash value is to be calculated.
        /// </param>
        /// <returns>
        ///  Equal to the GetHashCode value of the buffer.
        /// </returns>
        member _.GetHashCode ( v : PooledBuffer ): int = 
            v.Array.GetHashCode()

    //=========================================================================
    // override method

    /// <summary>
    ///  Equals method. 
    ///  If they refer to the same buffer and have the same requested size, they are determined to be the same.
    /// </summary>
    /// <param name="v">
    ///  The targets for comparison.
    /// </param>
    /// <returns>
    ///  True if this object and 'v' can be considered equal, false otherwise.
    /// </returns>
    override _.Equals( v : obj ) : bool =
        match v with
        | :? PooledBuffer as x ->
            Object.ReferenceEquals( m_Buffer, x.Array ) && m_Length = x.Length
        | _ -> false

    /// <summary>
    ///  Returns hash value.
    /// </summary>
    /// <returns>
    ///  Equal to the GetHashCode value of the buffer.
    /// </returns>
    override _.GetHashCode() : int =
        m_Buffer.GetHashCode()

    /// <summary>
    ///  Convert to string value.
    /// </summary>
    /// <returns>
    ///  Returns a fixed value "PooledBuffer".
    /// </returns>
    override _.ToString() : string =
        "PooledBuffer"


    //=========================================================================
    // Public method


    /// <summary>
    ///  Equals method. 
    ///  If they refer to the same buffer and have the same requested size, they are determined to be the same.
    /// </summary>
    /// <param name="v">
    ///  The targets for comparison.
    /// </param>
    /// <returns>
    ///  True if this object and 'v' can be considered equal, false otherwise.
    /// </returns>
    member _.Equals ( v : PooledBuffer ) : bool =
        Object.ReferenceEquals( m_Buffer, v.Array ) && m_Length = v.Length

    /// <summary>
    ///  Release buffer.
    /// </summary>
    member _.Return() : unit =
        if m_Buffer.Length > 0 then
            ArrayPool<byte>.Shared.Return m_Buffer

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
    member _.Array = m_Buffer

    /// Returns the length requested when allocating the buffer. Same as Count property.
    member _.Length = m_Length

    /// Returns the length requested when allocating the buffer. Same as Length property.
    member _.Count = m_Length

    /// Returns an ArraySegment that references the entire buffer.
    member _.ArraySegment = ArraySegment<byte>( m_Buffer, 0, m_Length )

    /// Accessing elements of a buffer.
    member _.Item with get ( idx : int ) = m_Buffer.[ idx ]

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
    static member Rent ( s : int ) : PooledBuffer =
        PooledBuffer( ArrayPool<byte>.Shared.Rent s, s )

    /// <summary>
    ///  Get buffer from ArrayPool
    /// </summary>
    /// <param name="s">
    ///  Required buffer size.
    /// </param>
    /// <returns>
    ///  Allocated and zero cleared buffer.
    /// </returns>
    static member RentAndInit ( s : int ) : PooledBuffer =
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
        let b = ArrayPool<byte>.Shared.Rent v.Length
        Array.blit v 0 b 0 v.Length
        PooledBuffer( b, v.Length )


    /// <summary>
    ///  Release a buffer.
    /// </summary>
    /// <param name="s">
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
        |> Seq.filter ( fun itr -> itr.Length > 0 )
        |> Seq.fold ( fun ( acc : PooledBuffer list ) x ->
            if not ( acc |> List.exists ( fun y -> Object.ReferenceEquals( x.Array, y.Array ) ) ) then
                x :: acc
            else
                acc
        ) []
        |> Seq.iter PooledBuffer.Return

    /// <summary>
    ///  Truncate buffer length.
    /// </summary>
    /// <param name="count">
    ///  The new buffer length.
    ///  If this value greater than or equals length of old buffer, returned buffer length will be equals to old buffer length.
    /// </param>
    /// <param name="b">
    ///  The buffer that will be truncated.
    /// </param>
    /// <returns>
    ///  Truncated buffer.
    /// </returns>
    /// <remarks>
    ///  Returned buffer refers to same array of old buffer. It not create new buffer.
    ///  Note that if Return method is called on both the old and new buffers, a double release of the buffers will occur.
    /// </remarks>
    static member Truncate ( count : int ) ( b : PooledBuffer ) : PooledBuffer =
        PooledBuffer( b.Array, min count b.Length )

    /// Empty
    static member Empty : PooledBuffer =
        PooledBuffer( Array.Empty<byte>(), 0 )

