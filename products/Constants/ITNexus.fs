//=============================================================================
// Haruka Software Storage.
// ITNexus.fs : Defines the data type that holds I_T Nexus string value.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Constants

open System
open System.Collections.Generic

/// <summary>
/// The data type of I_T Nexus identifier.
/// </summary>
/// <param name="initiatorName">
///  Initiator name string.
/// </param>
/// <param name="isid">
///  Initiator Session ID.
/// </param>
/// <param name="targetName">
///  Target name string.
/// </param>
/// <param name="tpgt">
///  Target portal group tag.
/// </param>
type ITNexus(
    initiatorName : string,
    isid : ISID_T,
    targetName : string,
    tpgt : TPGT_T ) =

    ///////////////////////////////////////////////////////////////////////////
    // member values
    // 

    /// SCSI initiator port name string
    let m_InitiatorPortName =
        String.Format( "{0},i,{1}", initiatorName, isid_me.toString isid )

    /// SCSI target port name string
    let m_TargetPortName =
        String.Format( "{0},t,0x{1:X4}", targetName, tpgt )

    /// I_T Nexus string
    let m_I_T_Nexus =
        String.Format( "( {0}, {1} )", m_InitiatorPortName, m_TargetPortName )


    ///////////////////////////////////////////////////////////////////////////
    // Implementation of interface method
    // 

    // Imprementation of IEquatable interface
    interface IEquatable<ITNexus> with

        /// <summary>
        ///  Compare two I_T nexus values.
        /// </summary>
        /// <param name="v">
        ///  The targets for comparison.
        /// </param>
        /// <returns>
        ///  True if this I_T Nexus value and 'v' can be considered equal, false otherwise.
        /// </returns>
        member _.Equals( v : ITNexus ) : bool =
            String.Equals( m_I_T_Nexus, v.I_TNexusStr, StringComparison.Ordinal )

    // Imprementation of IEqualityComparer interface
    interface  IEqualityComparer<ITNexus> with

        /// <summary>
        ///  Compare two I_T nexus values.
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
        member _.Equals ( v1 : ITNexus, v2 : ITNexus ) : bool =
            String.Equals( v1.I_TNexusStr, v2.I_TNexusStr, StringComparison.Ordinal )

        /// <summary>
        ///  Returns hash value.
        /// </summary>
        /// <param name="v">
        ///  The object for which the hash value is to be calculated.
        /// </param>
        /// <returns>
        ///  Calculated hash value.
        /// </returns>
        member _.GetHashCode ( v : ITNexus ): int = 
            v.I_TNexusStr.GetHashCode()


    ///////////////////////////////////////////////////////////////////////////
    // override method
    // 

    /// Compare ITNexus values
    override _.Equals( v : obj ) : bool =
        match v with
        | :? ITNexus as x ->
            String.Equals( m_I_T_Nexus, x.I_TNexusStr, StringComparison.Ordinal )
        | _ -> false

    /// Get hash code
    override _.GetHashCode() : int =
        m_I_T_Nexus.GetHashCode()

    /// Convert to string
    override this.ToString() : string =
        this.I_TNexusStr

    ///////////////////////////////////////////////////////////////////////////
    // public method
    // 

    /// <summary>
    ///  Compare two I_T nexus values.
    /// </summary>
    /// <param name="v">
    ///  The targets for comparison.
    /// </param>
    /// <returns>
    ///  True if this I_T Nexus value and 'v' can be considered equal, false otherwise.
    /// </returns>
    member _.Equals ( v : ITNexus ) : bool =
        String.Equals( m_I_T_Nexus, v.I_TNexusStr, StringComparison.Ordinal )


    ///////////////////////////////////////////////////////////////////////////
    // properties
    // 

    /// Get SCSI initiator port name string.
    member _.InitiatorPortName : string =
        m_InitiatorPortName

    /// Get SCSI target port name.
    member _.TargetPortName : string =
        m_TargetPortName


    /// Get I_T Nexus string.
    member _.I_TNexusStr : string =
        m_I_T_Nexus


    /// Get initiator name string
    member _.InitiatorName : string =
        initiatorName

    /// Get ISID value.
    member _.ISID : ISID_T =
        isid

    /// Get target name string
    member _.TargetName : string =
        targetName


    /// get TPGT value.
    member _.TPGT : TPGT_T =
        tpgt


    ///////////////////////////////////////////////////////////////////////////
    // static method
    // 

    /// <summary>
    ///  Get SCSI initiator port name string.
    /// </summary>
    /// <param name="i">
    ///  ITNexus value
    /// </param>
    /// <returns>
    ///  Initiator port name.
    /// </returns>
    static member getInitiatorPortName ( i : ITNexus ) : string =
        i.InitiatorPortName

    /// <summary>
    ///  Get SCSI target port name.
    /// </summary>
    /// <param name="i">
    ///  ITNexus value
    /// </param>
    /// <returns>
    ///  Target port name.
    /// </returns>
    static member getTargetPortName ( i : ITNexus ) : string =
        i.TargetPortName

    /// <summary>
    ///  Get I_T Nexus string.
    /// </summary>
    /// <param name="i">
    ///  ITNexus value
    /// </param>
    /// <returns>
    ///  I_T Nexus string.
    /// </returns>
    static member getI_TNexusStr ( i : ITNexus ) : string =
        i.I_TNexusStr

    /// <summary>
    ///  Get initiator name string
    /// </summary>
    /// <param name="i">
    ///  ITNexus value
    /// </param>
    /// <returns>
    ///  Initiator name string.
    /// </returns>
    static member getInitiatorName ( i : ITNexus ) : string =
        i.InitiatorName

    /// <summary>
    ///  Get ISID value.
    /// </summary>
    /// <param name="i">
    ///  ITNexus value
    /// </param>
    /// <returns>
    ///  ISID value.
    /// </returns>
    static member getISID ( i : ITNexus ) : ISID_T =
        i.ISID

    /// <summary>
    ///  Get target name string.
    /// </summary>
    /// <param name="i">
    ///  ITNexus value
    /// </param>
    /// <returns>
    ///  Target name string.
    /// </returns>
    static member getTargetName ( i : ITNexus ) : string =
        i.TargetName

    /// <summary>
    ///  Get TPGT value.
    /// </summary>
    /// <param name="i">
    ///  ITNexus value
    /// </param>
    /// <returns>
    ///  TPGT value.
    /// </returns>
    static member getTPGT ( i : ITNexus ) : TPGT_T =
        i.TPGT

    /// <summary>
    ///  Compare two I_T Nexus values.
    /// </summary>
    /// <param name="a">
    ///  I_T Nexus values 1.
    /// </param>
    /// <param name="b">
    ///  I_T Nexus values 2.
    /// </param>
    /// <returns>
    ///  True if I_T Nexus value 'a' and 'b' can be considered equal, false otherwise.
    /// </returns>
    static member Equals( a : ITNexus, b : ITNexus ) : bool =
        String.Equals( a.I_TNexusStr, b.I_TNexusStr, StringComparison.Ordinal )

    /// <summary>
    ///  Compare two I_T Nexus values.
    /// </summary>
    /// <param name="a">
    ///  I_T Nexus values 1.
    /// </param>
    /// <param name="b">
    ///  I_T Nexus values 2.
    /// </param>
    /// <returns>
    ///  If a is greater than b, it returns positive value.
    ///  If a is less than b, it returns negative value.
    ///  Otherwise, it returns 0.
    /// </returns>
    static member Compare ( a : ITNexus ) ( b : ITNexus ) : int =
        String.Compare( a.I_TNexusStr, b.I_TNexusStr, StringComparison.Ordinal )
