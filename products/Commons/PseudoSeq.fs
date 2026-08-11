//=============================================================================
// Haruka Software Storage.
// PseudoSeq.fs : Implement a pseudo-sequence where the next value is specified each time.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Constants

//=============================================================================
// Import declaration

open System.Collections.Generic

//=============================================================================
// Class implementation

/// <summary>
///  Implement a pseudo-sequence where the next value is specified each time.
/// </summary>
/// <param name="initVal">
///  Specify the initial value.
/// </param>
type PseudoSeq<'T>( initVal : 'T voption ) =

    /// next value
    let mutable m_NextValue : 'T voption = initVal

    /// Construct a PseudoSeq without specifying the following values.
    /// Repetition is not performed unless a value is explicitly specified.
    new() = PseudoSeq<'T>( ValueNone )

    /// Construct a PseudoSeq by specifying initial values.
    new( v :'T ) = PseudoSeq<'T>( ValueSome v )

    /// Get next value property
    member _.NextValue with set ( v : 'T voption ) : unit = m_NextValue <- v
                       and  get () : 'T voption = m_NextValue

    interface IEnumerable<'T> with
        /// Get pseudo enumerator object
        override this.GetEnumerator() : IEnumerator<'T> =
            new PseudoEnumerator<'T>( this )

        /// Get pseudo enumerator object
        override this.GetEnumerator() : System.Collections.IEnumerator =
            new PseudoEnumerator<'T>( this )

    /// Instruct to interrupt the repetition.
    member _.Break() : unit =
        m_NextValue <- ValueNone

    /// <summary>
    ///  Specify the following value to indicate that the repetition should continue.
    /// </summary>
    /// <param name="v">
    ///  Next value.
    /// </param>
    member _.Continue ( v : 'T ) : unit =
        m_NextValue <- ValueSome v

/// <summary>
///  Implement the functionality of the pseudo-Enumerator used in PseudoSeq.
/// </summary>
/// <param name="m_Seq">
///  Specify a PseudoSeq instance.
/// </param>
and PseudoEnumerator<'T>( m_Seq : PseudoSeq<'T> ) =

    /// current value
    let mutable m_CurrentValue : 'T voption = ValueNone

    interface IEnumerator<'T> with

        /// Get the current value.
        override _.Current : 'T =
            m_CurrentValue
            |> ValueOption.get

        /// Get the current value.
        override _.Current : obj =
            m_CurrentValue
            |> ValueOption.get
            :> obj

        /// Retrieve the presence or absence of the following value.
        override _.MoveNext() =
            m_CurrentValue <- m_Seq.NextValue
            m_CurrentValue
            |> ValueOption.isSome

        /// Nothing to do.
        override _.Reset() = ()

        /// Nothing to do.
        override _.Dispose() = ()

