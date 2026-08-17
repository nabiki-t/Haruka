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
type PseudoSeqStat<'T1, 'T2>( initVal : 'T1 voption ) =

    /// next value
    let mutable m_NextValue : 'T1 voption = initVal

    /// If a value is specified in the Break method, that specified value is retained.
    let mutable m_LastValue : 'T2 voption = ValueNone

    /// Construct a PseudoSeqStat without specifying the following values.
    /// Repetition is not performed unless a value is explicitly specified.
    new() = PseudoSeqStat<'T1, 'T2>( ValueNone )

    /// Construct a PseudoSeqStat by specifying initial values.
    new( v :'T1 ) = PseudoSeqStat<'T1, 'T2>( ValueSome v )

    /// Get next value property
    member _.NextValue = m_NextValue

    /// Get last value property
    member _.LastValue = m_LastValue

    interface IEnumerable<'T1> with
        /// Get pseudo enumerator object
        override this.GetEnumerator() : IEnumerator<'T1> =
            new PseudoEnumerator<'T1, 'T2>( this )

        /// Get pseudo enumerator object
        override this.GetEnumerator() : System.Collections.IEnumerator =
            new PseudoEnumerator<'T1, 'T2>( this )

    /// Instruct to interrupt the repetition.
    member _.Break() : unit =
        m_NextValue <- ValueNone
        m_LastValue <- ValueNone

    /// <summary>
    ///  Instruct to interrupt the repetition.
    /// </summary>
    /// <param name="v">
    ///  Specify the final value to be retained after the loop terminates.
    /// </param>
    member _.Break( v : 'T2 ) : unit =
        m_NextValue <- ValueNone
        m_LastValue <- ValueSome v

    /// <summary>
    ///  Specify the following value to indicate that the repetition should continue.
    /// </summary>
    /// <param name="v">
    ///  Next value.
    /// </param>
    member _.Continue ( v : 'T1 ) : unit =
        m_NextValue <- ValueSome v
        m_LastValue <- ValueNone

/// <summary>
///  Implement the functionality of the pseudo-Enumerator used in PseudoSeq.
/// </summary>
/// <param name="m_Seq">
///  Specify a PseudoSeq instance.
/// </param>
and PseudoEnumerator<'T1, 'T2>( m_Seq : PseudoSeqStat<'T1, 'T2> ) =

    /// current value
    let mutable m_CurrentValue : 'T1 voption = ValueNone

    interface IEnumerator<'T1> with

        /// Get the current value.
        override _.Current : 'T1 =
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


/// <summary>
///  Specify the same type name in PseudoSeqStat.
/// </summary>
/// <param name="initVal">
///  Specify the initial value.
/// </param>
type PseudoSeq<'T>( initVal : 'T voption ) =
    inherit PseudoSeqStat<'T, 'T>( initVal )

    /// Construct a PseudoSeq without specifying the following values.
    /// Repetition is not performed unless a value is explicitly specified.
    new() = PseudoSeq<'T>( ValueNone )

    /// Construct a PseudoSeq by specifying initial values.
    new( v :'T ) = PseudoSeq<'T>( ValueSome v )


/// <summary>
///  A pseudo-sequence object that specifies repetition conditions in advance.
/// </summary>
/// <param name="initVal">
///  Specify the initial value. 
///  If the specified initial value does not satisfy the iteration condition, 
///  the iteration is not executed unless a value that satisfies the condition is explicitly specified.
/// </param>
/// <param name="m_Condition">
///  Specify the repetition condition.
///  The loop executes as long as this function returns true.
/// </param>
type PseudoSeqCond<'T>( initVal : 'T voption, m_Condition : 'T -> bool ) =
    inherit PseudoSeq<'T>( ValueOption.filter m_Condition initVal )

    /// Construct a PseudoSeq without specifying the following values.
    /// Repetition is not performed unless a value is explicitly specified.
    new( m_Condition : 'T -> bool ) =
        PseudoSeqCond<'T>( ValueNone, m_Condition )

    /// Construct a PseudoSeq by specifying initial values.
    new( v :'T, m_Condition : 'T -> bool ) =
        PseudoSeqCond<'T>( ValueSome v, m_Condition )

    /// <summary>
    ///  Specify the following values.
    /// </summary>
    /// <param name="v">
    ///  Next value.
    /// </param>
    /// <remarks>
    ///  If the specified next value satisfies the iteration condition, 
    ///  the loop continues (equivalent to calling the Continue method). 
    ///  If it does not satisfy the iteration condition,
    ///  the loop terminates (equivalent to calling the Break method).
    ///  Note that if the parent class's `Continue` method is called directly,
    ///  the loop continues without evaluating the repetition condition.
    ///  Similarly, if the parent class's Break method is called,
    ///  the loop is interrupted regardless of the repetition condition.
    /// </remarks>
    member _.Next ( v : 'T ) : unit =
        if m_Condition v then
            base.Continue v
        else
            base.Break( v )
