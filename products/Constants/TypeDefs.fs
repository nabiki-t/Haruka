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

//=============================================================================
// Type definition

/// Log message level
[<Struct; IsReadOnly>]
type LogLevel =
    | LOGLEVEL_VERBOSE
    | LOGLEVEL_INFO
    | LOGLEVEL_WARNING
    | LOGLEVEL_ERROR
    | LOGLEVEL_FAILED
    | LOGLEVEL_OFF

    /// convert LogLevel value to string
    static member toString : ( LogLevel -> string ) =
        function
        | LOGLEVEL_VERBOSE  -> "VERBOSE"
        | LOGLEVEL_INFO     -> "INFO"
        | LOGLEVEL_WARNING  -> "WARNING"
        | LOGLEVEL_ERROR    -> "ERROR"
        | LOGLEVEL_FAILED   -> "FAILED"
        | LOGLEVEL_OFF      -> "OFF"

    /// convert string to LogLevel value
    static member fromString : ( string -> LogLevel ) =
        function
        | "VERBOSE" -> LOGLEVEL_VERBOSE
        | "INFO"    -> LOGLEVEL_INFO
        | "WARNING" -> LOGLEVEL_WARNING
        | "ERROR"   -> LOGLEVEL_ERROR
        | "FAILED"  -> LOGLEVEL_FAILED
        | "OFF"     -> LOGLEVEL_OFF
        | _         -> LOGLEVEL_INFO

    /// convert string to LogLevel value
    static member tryFromString : ( string -> ( bool * LogLevel ) ) =
        function
        | "VERBOSE" -> true, LOGLEVEL_VERBOSE
        | "INFO"    -> true, LOGLEVEL_INFO
        | "WARNING" -> true, LOGLEVEL_WARNING
        | "ERROR"   -> true, LOGLEVEL_ERROR
        | "FAILED"  -> true, LOGLEVEL_FAILED
        | "OFF"     -> true, LOGLEVEL_OFF
        | _         -> false, LOGLEVEL_INFO

    /// convert LogLevel value to interger value
    static member toInt : ( LogLevel -> int ) =
        function
        | LOGLEVEL_VERBOSE  -> 1
        | LOGLEVEL_INFO     -> 2
        | LOGLEVEL_WARNING  -> 3
        | LOGLEVEL_ERROR    -> 4
        | LOGLEVEL_FAILED   -> 5
        | LOGLEVEL_OFF      -> 99

    /// convert interger value to LogLevel value
    static member fromInt : ( int -> LogLevel ) =
        function
        | 1     -> LOGLEVEL_VERBOSE
        | 2     -> LOGLEVEL_INFO
        | 3     -> LOGLEVEL_WARNING
        | 4     -> LOGLEVEL_ERROR
        | 5     -> LOGLEVEL_FAILED
        | 99    -> LOGLEVEL_OFF
        | _     -> LOGLEVEL_INFO

    /// All of values
    static member Values = [|
        LOGLEVEL_VERBOSE;
        LOGLEVEL_INFO;
        LOGLEVEL_WARNING;
        LOGLEVEL_ERROR;
        LOGLEVEL_FAILED;
        LOGLEVEL_OFF;
    |]

/// <summary>
/// used in ISCSINegoParam.HeaderDigest, DataDigest value
/// </summary>
[<Struct; IsReadOnly>]
type DigestType =
    | DST_None
    | DST_CRC32C
    | DST_NotUnderstood

    /// <summary>
    /// Get string name value corresponging to DigestType value.
    /// </summary>
    static member toStringName : ( DigestType -> string ) =
        function
        | DigestType.DST_None  -> "None"
        | DigestType.DST_CRC32C  -> "CRC32C"
        | DigestType.DST_NotUnderstood   -> "NotUnderstood"

    /// <summary>
    /// Get DigestType value corresponging to specified string value. If argument is unexpected string, NotUnderstood is returned.
    /// </summary>
    static member fromStringValue : ( string -> DigestType ) =
        function
        | "None"  -> DigestType.DST_None
        | "CRC32C"  -> DigestType.DST_CRC32C
        | "NotUnderstood" -> DigestType.DST_NotUnderstood
        | _ -> DigestType.DST_NotUnderstood


/// <summary>
/// used in ISCSINegoParam.AuthMethod value( Login phase, security negotiation stage only )
/// </summary>
[<Struct; IsReadOnly>]
type AuthMethodCandidateValue =
    | AMC_None
    | AMC_CHAP
    | AMC_SRP
    | AMC_KRB5
    | AMC_SPKM1
    | AMC_SPKM2
    | AMC_NotUnderstood

    /// <summary>
    /// Get string name value corresponging to AuthMethodCandidateValue value.
    /// </summary>
    static member toStringName : ( AuthMethodCandidateValue -> string ) =
        function
        | AMC_None  -> "None"
        | AMC_CHAP  -> "CHAP"
        | AMC_SRP   -> "SRP"
        | AMC_KRB5  -> "KRB5"
        | AMC_SPKM1 -> "SPKM1"
        | AMC_SPKM2 -> "SPKM2"
        | AMC_NotUnderstood -> "NotUnderstood"

    /// <summary>
    /// Get AuthMethodCandidateValue value corresponging to specified string value. If argument is unexpected string, NotUnderstood is returned.
    /// </summary>
    static member fromStringValue : ( string -> AuthMethodCandidateValue ) =
        function
        | "None"  -> AMC_None
        | "CHAP"  -> AMC_CHAP
        | "SRP"   -> AMC_SRP
        | "KRB5"  -> AMC_KRB5
        | "SPKM1" -> AMC_SPKM1
        | "SPKM2" -> AMC_SPKM2
        | "NotUnderstood" -> AMC_NotUnderstood
        | _ -> AMC_NotUnderstood

    /// <summary>
    /// Get integer value corresponging to specified AuthMethodCandidateValue value.
    /// </summary>
    static member toNumericValue : ( AuthMethodCandidateValue -> int ) =
        function
        | AMC_None  -> 0
        | AMC_CHAP  -> 1
        | AMC_SRP   -> 2
        | AMC_KRB5  -> 3
        | AMC_SPKM1 -> 4
        | AMC_SPKM2 -> 5
        | AMC_NotUnderstood -> 0xFF

    /// <summary>
    ///  All of Values.
    /// </summary>
    static member Values : AuthMethodCandidateValue[] = [|
        AMC_None;
        AMC_CHAP;
        AMC_SRP;
        AMC_KRB5;
        AMC_SPKM1;
        AMC_SPKM2;
        AMC_NotUnderstood;
    |]

/// <summary>
/// Constants value that represents type of reservation.
/// </summary>
[<Struct; IsReadOnly>]
type PR_TYPE =
    /// Reservation is not established
    | NO_RESERVATION
    /// Write Exclusive
    | WRITE_EXCLUSIVE
    /// Exclusive Access
    | EXCLUSIVE_ACCESS
    /// Write Exclusive – Registrants Only
    | WRITE_EXCLUSIVE_REGISTRANTS_ONLY
    /// Write Exclusive – All Registrants
    | WRITE_EXCLUSIVE_ALL_REGISTRANTS
    /// Exclusive Access – Registrants Only
    | EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
    /// Exclusive Access – All Registrants
    | EXCLUSIVE_ACCESS_ALL_REGISTRANTS

    /// <summary>
    /// Get string name value corresponging to PR_TYPE value.
    /// </summary>
    static member toStringName : ( PR_TYPE -> string ) =
        function
        | NO_RESERVATION -> "NO_RESERVATION"
        | WRITE_EXCLUSIVE -> "WRITE_EXCLUSIVE"
        | EXCLUSIVE_ACCESS -> "EXCLUSIVE_ACCESS"
        | WRITE_EXCLUSIVE_REGISTRANTS_ONLY -> "WRITE_EXCLUSIVE_REGISTRANTS_ONLY"
        | WRITE_EXCLUSIVE_ALL_REGISTRANTS -> "WRITE_EXCLUSIVE_ALL_REGISTRANTS"
        | EXCLUSIVE_ACCESS_REGISTRANTS_ONLY -> "EXCLUSIVE_ACCESS_REGISTRANTS_ONLY"
        | EXCLUSIVE_ACCESS_ALL_REGISTRANTS -> "EXCLUSIVE_ACCESS_ALL_REGISTRANTS"

    /// <summary>
    /// Get PR_TYPE value corresponging to specified string value. If argument is unexpected string, NO_RESERVATION is returned.
    /// </summary>
    static member fromStringValue : ( string -> PR_TYPE ) =
        function
        | "NO_RESERVATION" -> NO_RESERVATION
        | "WRITE_EXCLUSIVE" -> WRITE_EXCLUSIVE
        | "EXCLUSIVE_ACCESS" -> EXCLUSIVE_ACCESS
        | "WRITE_EXCLUSIVE_REGISTRANTS_ONLY" -> WRITE_EXCLUSIVE_REGISTRANTS_ONLY
        | "WRITE_EXCLUSIVE_ALL_REGISTRANTS" -> WRITE_EXCLUSIVE_ALL_REGISTRANTS
        | "EXCLUSIVE_ACCESS_REGISTRANTS_ONLY" -> EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
        | "EXCLUSIVE_ACCESS_ALL_REGISTRANTS" -> EXCLUSIVE_ACCESS_ALL_REGISTRANTS
        | _ -> NO_RESERVATION


    /// <summary>
    /// Get integer value corresponging to specified PR_TYPE value.
    /// Returned integer value is defined in SPC-3 6.11.3.4.
    /// </summary>
    static member toNumericValue : ( PR_TYPE -> byte ) =
        function
        | NO_RESERVATION -> 0uy
        | WRITE_EXCLUSIVE -> 1uy
        | EXCLUSIVE_ACCESS -> 3uy
        | WRITE_EXCLUSIVE_REGISTRANTS_ONLY -> 5uy
        | WRITE_EXCLUSIVE_ALL_REGISTRANTS -> 7uy
        | EXCLUSIVE_ACCESS_REGISTRANTS_ONLY -> 6uy
        | EXCLUSIVE_ACCESS_ALL_REGISTRANTS -> 8uy

    /// <summary>
    /// If specified argument is All Registrants type, true is returned.
    /// </summary>
    /// <param name="v">
    ///  PR_TYPE value.
    /// </param>
    static member isAllRegistrants ( v : PR_TYPE ) : bool =
        v = WRITE_EXCLUSIVE_ALL_REGISTRANTS || v = EXCLUSIVE_ACCESS_ALL_REGISTRANTS

    /// <summary>
    /// If specified argument is Registrants Only type, true is returned.
    /// </summary>
    /// <param name="v">
    ///  PR_TYPE value.
    /// </param>
    static member isRegistrantsOnly ( v : PR_TYPE ) : bool =
        v = WRITE_EXCLUSIVE_REGISTRANTS_ONLY || v = EXCLUSIVE_ACCESS_REGISTRANTS_ONLY

    /// <summary>
    /// If specified argument is WRITE EXCLUSIVE or EXCLUSIVE ACCESS type, true is returned.
    /// </summary>
    /// <param name="v">
    ///  PR_TYPE value.
    /// </param>
    static member isOtherReservation ( v : PR_TYPE ) : bool =
        v = WRITE_EXCLUSIVE || v = EXCLUSIVE_ACCESS

    /// <summary>
    ///  All of values.
    /// </summary>
    static member Values : PR_TYPE [] = [|
        NO_RESERVATION;
        WRITE_EXCLUSIVE;
        EXCLUSIVE_ACCESS;
        WRITE_EXCLUSIVE_REGISTRANTS_ONLY;
        WRITE_EXCLUSIVE_ALL_REGISTRANTS;
        EXCLUSIVE_ACCESS_REGISTRANTS_ONLY;
        EXCLUSIVE_ACCESS_ALL_REGISTRANTS;
    |]

/// iSCSI type value.
[<Struct; IsReadOnly>]
type iSCSITaskType =
    | NOPOut
    | SCSICommand
    | SCSITaskManagement
    | TextNegociation
    | Logout
    | SNACK

/// Initiator-side or target-side processing
[<Struct; IsReadOnly>]
type Standpoint =
    | Target
    | Initiator

//=============================================================================
// Measure type definitions.

[<Measure>]
type objidx_me =

    /// <summary>Generate new object ID.</summary>
    static member inline NewID () : uint<objidx_me> =
        let g = Guid.NewGuid()
        let v = g.ToByteArray()
        let u1 = BitConverter.ToUInt32( v, 0 )
        let u2 = BitConverter.ToUInt32( v, 4 )
        let u3 = BitConverter.ToUInt32( v, 8 )
        let u4 = BitConverter.ToUInt32( v, 12 )
        ( u1 ^^^ u2 ^^^ u3 ^^^ u4 ) * 1u<objidx_me>

    static member inline ToString( v : uint<objidx_me> ) : string =
        String.Format( "0x{0:X8}", v )

/// Data types of objidx_me
type OBJIDX_T = uint<objidx_me>


/// Measure for network portal index number in configuration data.
[<Measure>]
type netportidx_me =

    /// <summary>Convert netportidx_me value to primitive value.</summary>
    /// <param name="v">netportidx_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint32<netportidx_me> ) : uint32 =
        uint32 v

    /// <summary>Convert primitive value to netportidx_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint32 ) : uint32<netportidx_me> =
        ( uint32 v ) * 1u<netportidx_me>

    /// zero value fo cid_me
    static member zero = 0u<netportidx_me>


/// Data types of netportidx_me
type NETPORTIDX_T = uint32<netportidx_me>

/// Measure for target node index number in configuration data
[<Measure>]
type tnodeidx_me =

    /// <summary>Convert tnodeidx_me value to primitive value.</summary>
    /// <param name="v">tnodeidx_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint32<tnodeidx_me> ) : uint32 =
        uint32 v

    /// <summary>Convert primitive value to tnodeidx_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint32 ) : uint32<tnodeidx_me> =
        ( uint32 v ) * 1u<tnodeidx_me>


/// Data types of tnodeidx_me
type TNODEIDX_T = uint32<tnodeidx_me>

/// Measure for media index number in configuration data.
[<Measure>]
type mediaidx_me =

    /// <summary>Convert mediaidx_me value to primitive value.</summary>
    /// <param name="v">mediaidx_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint32<mediaidx_me> ) : uint32 =
        uint32 v

    /// <summary>Convert primitive value to mediaidx_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint32 ) : uint32<mediaidx_me> =
        ( uint32 v ) * 1u<mediaidx_me>

    /// zero value fo mediaidx_me
    static member zero = 0u<mediaidx_me>

/// Data types of mediaidx_me
type MEDIAIDX_T = uint32<mediaidx_me>

/// Measure for TPGT
[<Measure>]
type tpgt_me =
    /// <summary>Convert tpgt_me value to primitive value.</summary>
    /// <param name="v">tpgt_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint16<tpgt_me> ) : uint16 =
        uint16 v

    /// <summary>Convert primitive value to tpgt_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint16 ) : uint16<tpgt_me> =
        v * 1us<tpgt_me>

    /// zero value fo tpgt_me
    static member zero = 0us<tpgt_me>

    /// <summary>Get the value from optional data or default value if speficied optional value is None.</summary>
    /// <param name="def">Default value that is returned, when optv is None.</param>
    /// <param name="optv">Optional value.</param>
    /// <returns>Value of optv, or def.</returns>
    static member inline fromOpt ( def : uint16 ) ( optv : uint16<tpgt_me> option ) : uint16<tpgt_me> =
        match optv with
        | None -> tpgt_me.fromPrim def
        | Some( x ) -> x

/// Data types of TPGT
type TPGT_T = uint16<tpgt_me>

/// <summary>
///  Measure for LUN.
/// </summary>
/// <remarks>
///  Internally, lun is represented by uint64.
///  However, legal LUN values ​​are between 0 and 255.
///  LUN value is defined as "FIRST LEVEL(0xXXXX) - SECOND LEVEL(0xXXXX) - THIRD LEVEL(0xXXXX) - FOURTH LEVEL(0xXXXX)" in SAM-2.
///  The internal representation is handled as 0xFFFFTTTTSSSSIIII.
///  Where FFFF is FOURTH LEVEL, TTTT is THIRD LEVEL, SSSS is SECOND LEVEL and IIII is FIRST LEVEL".
///  The values ​​at each level are in network byte order.
/// </remarks>
[<Measure>]
type lun_me =

    /// <summary>Convert lun_me value to primitive value.</summary>
    /// <param name="v">lun_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint64<lun_me> ) : uint64 =
        uint64 v

    /// <summary>Convert primitive value to lun_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint64 ) : uint64<lun_me> =
        v * 1UL<lun_me>

    /// zero value fo lun_me
    static member inline zero = lun_me.fromPrim 0UL

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Translate a uint64 value representing the LUN to string value for using log output messages.
    /// </summary>
    /// <param name="lun">LUN value</param>
    /// <returns>formatted string LUN value</returns>
    /// <example><code>
    /// let msg = sprintf "Specified LUN = %s" ( lun_me.toString lun )
    /// </code></example>
    static member inline toString ( lun : uint64<lun_me> ) : string =
        String.Format( "{0}", lun_me.toPrim lun )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Translate a LUN_value to working directory name for LU.
    /// </summary>
    /// <param name="lun">LUN value</param>
    /// <returns>working directory name</returns>
    static member inline WorkDirName ( lun : uint64<lun_me> ) : string =
        Constants.LU_WORK_DIR_PREFIX + ( lun_me.toString lun )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Translate a string represents LUN value to LUN_T value.
    /// </summary>
    /// <param name="s">string value resresenting LUN value</param>
    /// <returns>LUN_T value</returns>
    static member inline fromStringValue ( s : string ) : uint64<lun_me> =
        if s.StartsWith "0x" || s.StartsWith "0X" then
            Convert.ToUInt64( s, 16 )
        else
            Convert.ToUInt64( s, 10 )
        |> lun_me.fromPrim

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Translate a LUN value to binary LUN. The result is written into specified array.
    /// </summary>
    /// <param name="v">Bytes array to which written converted result.</param>
    /// <param name="p">The position in array v where the conversion result is written.</param>
    /// <param name="lun">LUN value</param>
    static member inline toBytes ( v : byte[] ) ( p : int ) ( lun : uint64<lun_me> ) : unit =
        let lval = lun_me.toPrim lun
        v.[ p + 1 ] <- ( lval        ) |> byte
        v.[ p + 0 ] <- ( lval >>> 8  ) |> byte
        v.[ p + 3 ] <- ( lval >>> 16 ) |> byte
        v.[ p + 2 ] <- ( lval >>> 24 ) |> byte
        v.[ p + 5 ] <- ( lval >>> 32 ) |> byte
        v.[ p + 4 ] <- ( lval >>> 40 ) |> byte
        v.[ p + 7 ] <- ( lval >>> 48 ) |> byte
        v.[ p + 6 ] <- ( lval >>> 56 ) |> byte

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Translate a LUN value to binary LUN. The result is written into newly created array.
    /// </summary>
    /// <param name="lun">LUN value</param>
    /// <returns>converted value</returns>
    static member inline toBytes_NewVec ( lun : uint64<lun_me> ) : byte[] =
        let v = Array.zeroCreate<byte> 8
        lun_me.toBytes v 0 lun
        v

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Translate a bytes array to LUN value.
    /// </summary>
    /// <param name="v">Bytes array/</param>
    /// <param name="p">The position in array v where the LUN value has been written.</param>
    /// <returns>Translated LUN value</returns>
    static member inline fromBytes ( v : byte[] ) ( p : int ) : uint64<lun_me> =
        (   uint64 v.[ p + 1 ]          ) |||
        ( ( uint64 v.[ p + 0 ] ) <<< 8  ) |||
        ( ( uint64 v.[ p + 3 ] ) <<< 16 ) |||
        ( ( uint64 v.[ p + 2 ] ) <<< 24 ) ||| 
        ( ( uint64 v.[ p + 5 ] ) <<< 32 ) |||
        ( ( uint64 v.[ p + 4 ] ) <<< 40 ) |||
        ( ( uint64 v.[ p + 7 ] ) <<< 48 ) |||
        ( ( uint64 v.[ p + 6 ] ) <<< 56 ) 
        |> lun_me.fromPrim


/// Data types of LUN
type LUN_T = uint64<lun_me>


/// Measure for InitiatorTaskTag or ReferencedTaskTag
[<Measure>]
type itt_me =
    /// <summary>Convert itt_me value to primitive value.</summary>
    /// <param name="v">itt_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint<itt_me> ) : uint =
        uint32 v

    /// <summary>Convert primitive value to itt_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint ) : uint<itt_me> =
        v * 1u<itt_me>

/// Data types of InitiatorTaskTag
type ITT_T = uint<itt_me>


/// Measure for TargetTransferTag
[<Measure>]
type ttt_me =
    /// <summary>Convert ttt_me value to primitive value.</summary>
    /// <param name="v">ttt_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint<ttt_me> ) : uint =
        uint32 v

    /// <summary>Convert primitive value to ttt_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint ) : uint<ttt_me> =
        v * 1u<ttt_me>

/// Data types of TargetTransferTag
type TTT_T = uint<ttt_me>


/// Measure for TSIH
[<Measure>]
type tsih_me =

    /// <summary>Convert tsih_me value to primitive value.</summary>
    /// <param name="v">tsih_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint16<tsih_me> ) : uint16 =
        uint16 v

    /// <summary>Convert primitive value to tsih_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint16 ) : uint16<tsih_me> =
        v * 1us<tsih_me>

    /// zero value fo tsih_me
    static member zero = 0us<tsih_me>

    /// <summary>Get the value from optional data or default value if speficied optional value is None.</summary>
    /// <param name="def">Default value that is returned, when optv is None.</param>
    /// <param name="optv">Optional value.</param>
    /// <returns>Value of optv, or def.</returns>
    static member inline fromOpt ( def : uint16 ) ( optv : uint16<tsih_me> option ) : uint16<tsih_me> =
        match optv with
        | None -> tsih_me.fromPrim def
        | Some( x ) -> x

    /// <summary>Get the value from optional data or default value if speficied optional value is None.</summary>
    /// <param name="def">Default value that is returned, when optv is None.</param>
    /// <param name="optv">Optional value.</param>
    /// <returns>Value of optv, or def.</returns>
    static member inline fromValOpt ( def : uint16 ) ( optv : uint16<tsih_me> ValueOption ) : uint16<tsih_me> =
        match optv with
        | ValueNone -> tsih_me.fromPrim def
        | ValueSome( x ) -> x

    /// <summary>Compare 2 TSIH values</summary>
    /// <param name="a">TSIH value 1.</param>
    /// <param name="b">TSIH Value 2.</param>
    /// <returns>
    ///  If a less than b, -1 is returned. If a greater than b, 1 is returned. Otherwise 0 is returned.
    /// </returns>
    static member inline Compare ( a : uint16<tsih_me> ) ( b : uint16<tsih_me> ) : int =
        if a < b then -1
        elif a > b then 1
        else 0

/// Data types of TSIH
type TSIH_T = uint16<tsih_me>


/// Measure for ConnectionID
[<Measure>]
type cid_me =

    /// <summary>Convert cid_me value to primitive value.</summary>
    /// <param name="v">cid_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint16<cid_me> ) : uint16 =
        uint16 v

    /// <summary>Convert primitive value to cid_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint16 ) : uint16<cid_me> =
        ( uint16 v ) * 1us<cid_me>

    /// zero value fo cid_me
    static member zero = 0us<cid_me>

    /// <summary>Get the value from optional data or default value if speficied optional value is None.</summary>
    /// <param name="def">Default value that is returned, when optv is None.</param>
    /// <param name="optv">Optional value.</param>
    /// <returns>Value of optv, or def.</returns>
    static member inline fromOpt ( def : uint16 ) ( optv : uint16<cid_me> option ) : uint16<cid_me> =
        match optv with
        | None -> cid_me.fromPrim def
        | Some( x ) -> x

    /// <summary>Get the value from optional data or default value if speficied optional value is None.</summary>
    /// <param name="def">Default value that is returned, when optv is None.</param>
    /// <param name="optv">Optional value.</param>
    /// <returns>Value of optv, or def.</returns>
    static member inline fromValOpt ( def : uint16 ) ( optv : uint16<cid_me> ValueOption ) : uint16<cid_me> =
        match optv with
        | ValueNone -> cid_me.fromPrim def
        | ValueSome( x ) -> x

/// Data types of ConnectionID
type CID_T = uint16<cid_me>


/// Measure for Connection Counter
[<Measure>]
type concnt_me =
    /// <summary>Convert concnt_me value to primitive value.</summary>
    /// <param name="v">concnt_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : int<concnt_me> ) : int =
        int v

    /// <summary>Convert primitive value to concnt_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : int ) : int<concnt_me> =
        ( int v ) * 1<concnt_me>

    /// zero value fo cid_me
    static member zero = 0<concnt_me>

    /// <summary>Get the value from optional data or default value if speficied optional value is None.</summary>
    /// <param name="def">Default value that is returned, when optv is None.</param>
    /// <param name="optv">Optional value.</param>
    /// <returns>Value of optv, or def.</returns>
    static member inline fromOpt ( def : int ) ( optv : int<concnt_me> option ) : int<concnt_me> =
        match optv with
        | None -> concnt_me.fromPrim def
        | Some( x ) -> x

    /// <summary>Compare CID_T and CONCNT_T pair.</summary>
    /// <param name="acid">CID value 1 for compare</param>
    /// <param name="aconcnt">ConCnt value 1 for compare</param>
    /// <param name="bcid">CID value 2 for compare</param>
    /// <param name="bconcnt">ConCnt value 1 for compare</param>
    /// <returns>
    ///  If ( acid, aconcnt ) less than ( bcid, bconcnt ), -1 will be returned.
    ///  If ( acid, aconcnt ) greater than ( bcid, bconcnt ), 1 will be returned.
    ///  Otherwise, 0 will be returned.
    /// </returns>
    static member inline Compare ( acid : CID_T ) ( aconcnt : int<concnt_me> ) ( bcid : CID_T ) ( bconcnt : int<concnt_me> ) : int =
        if acid < bcid then -1
        elif acid > bcid then 1
        elif aconcnt < bconcnt then -1
        elif aconcnt > bconcnt then 1
        else 0

/// Data types of Connection Counter
type CONCNT_T = int<concnt_me>

/// Measure for CmdSN
[<Measure>]
type cmdsn_me =
    /// <summary>Convert cmdsn_me value to primitive value.</summary>
    /// <param name="v">cmdsn_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint<cmdsn_me> ) : uint =
        uint32 v

    /// <summary>Convert primitive value to cmdsn_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint ) : uint<cmdsn_me> =
        v * 1u<cmdsn_me>

    /// zero value fo cmdsn_me
    static member zero = 0u<cmdsn_me>

    /// <summary>Get the value from optional data or default value if speficied optional value is None.</summary>
    /// <param name="def">Default value that is returned, when optv is None.</param>
    /// <param name="optv">Optional value.</param>
    /// <returns>Value of optv, or def.</returns>
    static member inline fromOpt ( def : uint32 ) ( optv : uint<cmdsn_me> option ) : uint<cmdsn_me> =
        match optv with
        | None -> cmdsn_me.fromPrim def
        | Some( x ) -> x

    /// <summary>Compare cmdsn_me value in Serial Number Arithmetic.</summary>
    /// <param name="s1">left value.</param>
    /// <param name="s2">right value.</param>
    /// <returns>If s1 is less than s2 in serial number arithmetic, it returns true.</returns>
    static member inline lessThan ( s1 : uint<cmdsn_me> ) ( s2 : uint<cmdsn_me> ) : bool =
        let i1 = cmdsn_me.toPrim s1
        let i2 = cmdsn_me.toPrim s2
        ( ( i1 < i2 ) && ( ( i2 - i1 ) <  0x80000000u ) ) || ( ( i1 > i2 ) && ( ( i1 - i2 ) > 0x80000000u ) )

    /// <summary>Compare cmdsn_me value in Serial Number Arithmetic.</summary>
    /// <param name="s1">left value.</param>
    /// <param name="s2">right value.</param>
    /// <returns>
    ///   If s1 is less than s2 in serial number arithmetic, it returns -1,
    ///   if s2 is less than s1 in serial number arithmetic, it returns 1,
    ///   otherwise it returns zero.
    /// </returns>
    static member inline compare ( s1 : uint<cmdsn_me> ) ( s2 : uint<cmdsn_me> ) : int =
        if cmdsn_me.lessThan s1 s2 then
            -1
        elif cmdsn_me.lessThan s2 s1 then
            1
        elif s1 = s2 then
            0
        // If s1 and s2 are opposite, the result is undefined,
        // in that case, it is simply decided by the comparison of s1 and s2.
        elif s1 < s2 then
           -1
        else
            1

    /// <summary>
    ///  Get next CmdSN value.
    /// </summary>
    /// <param name="s1">
    ///  Current CmdSN value.
    /// </param>
    /// <returns>
    ///  Current CmdSN + 1.
    /// </returns>
    static member inline next ( s1 : uint<cmdsn_me> ) : uint<cmdsn_me> =
        s1 + ( cmdsn_me.fromPrim 1u )

    /// <summary>
    ///  Increment CmdSN value.
    /// </summary>
    /// <param name="v">
    ///  Number to add
    /// </param>
    /// <param name="s1">
    ///  CmdSN value to be added.
    /// </param>
    /// <returns>
    ///  s1 + v
    /// </returns>
    static member inline incr ( v : uint ) ( s1 : uint<cmdsn_me> ) : uint<cmdsn_me> =
        s1 + ( cmdsn_me.fromPrim v )

    /// <summary>
    ///  Decrement CmdSN value.
    /// </summary>
    /// <param name="v">
    ///  Number to subtract
    /// </param>
    /// <param name="s1">
    ///  CmdSN value to be subtracted.
    /// </param>
    /// <returns>
    ///  s1 + v
    /// </returns>
    static member inline decr ( v : uint ) ( s1 : uint<cmdsn_me> ) : uint<cmdsn_me> =
        s1 - ( cmdsn_me.fromPrim v )


/// Data types of CmdSN
type CMDSN_T = uint<cmdsn_me>


/// Measure for StatSN
[<Measure>]
type statsn_me =
    /// <summary>Convert statsn_me value to primitive value.</summary>
    /// <param name="v">statsn_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint<statsn_me> ) : uint =
        uint32 v

    /// <summary>Convert primitive value to statsn_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint ) : uint<statsn_me> =
        v * 1u<statsn_me>

    /// zero value fo statsn_me
    static member zero = 0u<statsn_me>

    /// <summary>Get the value from optional data or default value if speficied optional value is None.</summary>
    /// <param name="def">Default value that is returned, when optv is None.</param>
    /// <param name="optv">Optional value.</param>
    /// <returns>Value of optv, or def.</returns>
    static member inline fromOpt ( def : uint32 ) ( optv : uint<statsn_me> option ) : uint<statsn_me> =
        match optv with
        | None -> statsn_me.fromPrim def
        | Some( x ) -> x

    /// <summary>Compare statsn_me value in Serial Number Arithmetic.</summary>
    /// <param name="s1">left value.</param>
    /// <param name="s2">right value.</param>
    /// <returns>If s1 is less than s2 in serial number arithmetic, it returns true.</returns>
    static member inline lessThan ( s1 : uint<statsn_me> ) ( s2 : uint<statsn_me> ) : bool =
        let i1 = statsn_me.toPrim s1
        let i2 = statsn_me.toPrim s2
        ( ( i1 < i2 ) && ( ( i2 - i1 ) <  0x80000000u ) ) || ( ( i1 > i2 ) && ( ( i1 - i2 ) > 0x80000000u ) )

    /// <summary>Compare statsn_me value in Serial Number Arithmetic.</summary>
    /// <param name="s1">left value.</param>
    /// <param name="s2">right value.</param>
    /// <returns>
    ///   If s1 is less than s2 in serial number arithmetic, it returns -1,
    ///   if s2 is less than s1 in serial number arithmetic, it returns 1,
    ///   otherwise it returns zero.
    /// </returns>
    static member inline compare ( s1 : uint<statsn_me> ) ( s2 : uint<statsn_me> ) : int =
        if statsn_me.lessThan s1 s2 then
            -1
        elif statsn_me.lessThan s2 s1 then
            1
        elif s1 = s2 then
            0
        // If s1 and s2 are opposite, the result is undefined,
        // in that case, it is simply decided by the comparison of s1 and s2.
        elif s1 < s2 then
           -1
        else
            1

    /// <summary>
    ///  Get next StatSN value.
    /// </summary>
    /// <param name="s1">
    ///  Current StatSN value.
    /// </param>
    /// <returns>
    ///  Current StatSN + 1.
    /// </returns>
    static member inline next ( s1 : uint<statsn_me> ) : uint<statsn_me> =
        s1 + ( statsn_me.fromPrim 1u )

    /// <summary>
    ///  Increment StatSN value.
    /// </summary>
    /// <param name="v">
    ///  Number to add
    /// </param>
    /// <param name="s1">
    ///  StatSN value to be added.
    /// </param>
    /// <returns>
    ///  s1 + v
    /// </returns>
    static member inline incr ( v : uint ) ( s1 : uint<statsn_me> ) : uint<statsn_me> =
        s1 + ( statsn_me.fromPrim v )

    /// <summary>
    ///  Decrement StatSN value.
    /// </summary>
    /// <param name="v">
    ///  Number to subtract
    /// </param>
    /// <param name="s1">
    ///  StatSN value to be subtracted.
    /// </param>
    /// <returns>
    ///  s1 + v
    /// </returns>
    static member inline decr ( v : uint ) ( s1 : uint<statsn_me> ) : uint<statsn_me> =
        s1 - ( statsn_me.fromPrim v )

/// Data types of StatSN
type STATSN_T = uint<statsn_me>


/// Measure for DataSN / R2TSN
[<Measure>]
type datasn_me =
    /// <summary>Convert datasn_me value to primitive value.</summary>
    /// <param name="v">datasn_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint<datasn_me> ) : uint =
        uint32 v

    /// <summary>Convert primitive value to datasn_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint ) : uint<datasn_me> =
        v * 1u<datasn_me>

    /// zero value fo datasn_me
    static member zero = 0u<datasn_me>

    /// <summary>Get the value from optional data or default value if speficied optional value is None.</summary>
    /// <param name="def">Default value that is returned, when optv is None.</param>
    /// <param name="optv">Optional value.</param>
    /// <returns>Value of optv, or def.</returns>
    static member inline fromOpt ( def : uint32 ) ( optv : uint<datasn_me> option ) : uint<datasn_me> =
        match optv with
        | None -> datasn_me.fromPrim def
        | Some( x ) -> x

    /// <summary>Compare datasn_me value in Serial Number Arithmetic.</summary>
    /// <param name="s1">left value.</param>
    /// <param name="s2">right value.</param>
    /// <returns>If s1 is less than s2 in serial number arithmetic, it returns true.</returns>
    static member inline lessThan ( s1 : uint<datasn_me> ) ( s2 : uint<datasn_me> ) : bool =
        let i1 = datasn_me.toPrim s1
        let i2 = datasn_me.toPrim s2
        ( ( i1 < i2 ) && ( ( i2 - i1 ) <  0x80000000u ) ) || ( ( i1 > i2 ) && ( ( i1 - i2 ) > 0x80000000u ) )

    /// <summary>Compare datasn_me value in Serial Number Arithmetic.</summary>
    /// <param name="s1">left value.</param>
    /// <param name="s2">right value.</param>
    /// <returns>
    ///   If s1 is less than s2 in serial number arithmetic, it returns -1,
    ///   if s2 is less than s1 in serial number arithmetic, it returns 1,
    ///   otherwise it returns zero.
    /// </returns>
    static member inline compare ( s1 : uint<datasn_me> ) ( s2 : uint<datasn_me> ) : int =
        if datasn_me.lessThan s1 s2 then
            -1
        elif datasn_me.lessThan s2 s1 then
            1
        elif s1 = s2 then
            0
        // If s1 and s2 are opposite, the result is undefined,
        // in that case, it is simply decided by the comparison of s1 and s2.
        elif s1 < s2 then
           -1
        else
            1

    /// <summary>
    ///  Get next DataSN value.
    /// </summary>
    /// <param name="s1">
    ///  Current DataSN value.
    /// </param>
    /// <returns>
    ///  Current DataSN + 1.
    /// </returns>
    static member inline next ( s1 : uint<datasn_me> ) : uint<datasn_me> =
        s1 + ( datasn_me.fromPrim 1u )

    /// <summary>
    ///  Increment DataSN value.
    /// </summary>
    /// <param name="v">
    ///  Number to add
    /// </param>
    /// <param name="s1">
    ///  DataSN value to be added.
    /// </param>
    /// <returns>
    ///  s1 + v
    /// </returns>
    static member inline incr ( v : uint ) ( s1 : uint<datasn_me> ) : uint<datasn_me> =
        s1 + ( datasn_me.fromPrim v )

    /// <summary>
    ///  Decrement DataSN value.
    /// </summary>
    /// <param name="v">
    ///  Number to subtract
    /// </param>
    /// <param name="s1">
    ///  DataSN value to be subtracted.
    /// </param>
    /// <returns>
    ///  s1 + v
    /// </returns>
    static member inline decr ( v : uint ) ( s1 : uint<datasn_me> ) : uint<datasn_me> =
        s1 - ( datasn_me.fromPrim v )

/// Data types of DataSN / R2TSN
type DATASN_T = uint<datasn_me>

/// Measure for SNACKTag
[<Measure>]
type snacktag_me =
    /// <summary>Convert snacktag_me value to primitive value.</summary>
    /// <param name="v">snacktag_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint<snacktag_me> ) : uint =
        uint32 v

    /// <summary>Convert primitive value to snacktag_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint ) : uint<snacktag_me> =
        v * 1u<snacktag_me>

    /// zero value fo snacktag_me
    static member zero = 0u<snacktag_me>

/// Data types of SNACKTag
type SNACKTAG_T = uint<snacktag_me>

/// Measure for reservation key number in PRManager.
[<Measure>]
type resvkey_me =

    /// <summary>Convert netportidx_me value to primitive value.</summary>
    /// <param name="v">netportidx_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint64<resvkey_me> ) : uint64 =
        uint64 v

    /// <summary>Convert primitive value to resvkey_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint64 ) : uint64<resvkey_me> =
        v * 1UL<resvkey_me>

    /// zero value of resvkey_me
    static member zero = 0UL<resvkey_me>

    /// <summary>Convert to string value</summary>
    /// <param name="v">resvkey_me value to be converted.</param>
    /// <returns>Converted string value.</returns>
    static member toString( v : uint64<resvkey_me> ) : string =
        String.Format( "0x{0:X16}", v )

/// Data types of resvkey_me
type RESVKEY_T = uint64<resvkey_me>

[<Measure>]
type isid_me =

    /// <summary>Convert isid_me value to primitive value.</summary>
    /// <param name="v">isid_me value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline toPrim( v : uint64<isid_me> ) : uint64 =
        uint64 v

    /// <summary>Convert primitive value to isid_me value.</summary>
    /// <param name="v">Primitive value to be converted.</param>
    /// <returns>Converted value.</returns>
    static member inline fromPrim( v : uint64 ) : uint64<isid_me> =
        v * 1UL<isid_me>

    /// zero value of isid_me
    static member zero = 0UL<isid_me>

    /// <summary>
    ///  Get T field value of ISID.
    /// </summary>
    /// <param name="v">
    ///  isid_me value
    /// </param>
    /// <returns>
    ///  T field value of ISID.
    /// </returns>
    static member get_T ( v : uint64<isid_me> ) : byte   =
        byte   ( ( ( isid_me.toPrim v ) &&& 0x0000C00000000000UL ) >>> 40 )

    /// <summary>
    ///  Get A field value of ISID.
    /// </summary>
    /// <param name="v">
    ///  isid_me value
    /// </param>
    /// <returns>
    ///  A field value of ISID.
    /// </returns>
    static member get_A ( v : uint64<isid_me> ) : byte   =
        byte   ( ( ( isid_me.toPrim v ) &&& 0x00003F0000000000UL ) >>> 40 )

    /// <summary>
    ///  Get B field value of ISID.
    /// </summary>
    /// <param name="v">
    ///  isid_me value
    /// </param>
    /// <returns>
    ///  B field value of ISID.
    /// </returns>
    static member get_B ( v : uint64<isid_me> ) : uint16 =
        uint16 ( ( ( isid_me.toPrim v ) &&& 0x000000FFFF000000UL ) >>> 24 )

    /// <summary>
    ///  Get C field value of ISID.
    /// </summary>
    /// <param name="v">
    ///  isid_me value
    /// </param>
    /// <returns>
    ///  C field value of ISID.
    /// </returns>
    static member get_C ( v : uint64<isid_me> ) : byte   =
        byte   ( ( ( isid_me.toPrim v ) &&& 0x0000000000FF0000UL ) >>> 16 )

    /// <summary>
    ///  Get D field value of ISID.
    /// </summary>
    /// <param name="v">
    ///  isid_me value
    /// </param>
    /// <returns>
    ///  D field value of ISID.
    /// </returns>
    static member get_D ( v : uint64<isid_me> ) : uint16 =
        uint16   ( ( isid_me.toPrim v ) &&& 0x000000000000FFFFUL )

    /// <summary>
    ///  Converts to hex string value.
    /// </summary>
    /// <param name="v">
    ///  isid_me value
    /// </param>
    /// <returns>
    ///  Converted string value.
    /// </returns>
    static member toString( v : uint64<isid_me> ) : string =
        String.Format( "0x{0:X12}", isid_me.toPrim v )

    /// <summary>
    ///  Converts hex string value to isid_me.
    /// </summary>
    /// <param name="v">
    ///  String value.
    /// </param>
    /// <returns>
    ///  Converted isid_me value, if it failed to get ISID values, returns zero.
    /// </returns>
    static member HexStringToISID( v : string ) : uint64<isid_me> =
        try
            System.Convert.ToUInt64( v, 16 )
            |> isid_me.fromPrim
        with
        | _ -> isid_me.zero

    /// <summary>
    ///  Generate an ISID value by specifying individual element values.
    /// </summary>
    /// <param name="t">
    ///  T value od ISID.
    /// </param>
    /// <param name="a">
    ///  A value od ISID.
    /// </param>
    /// <param name="b">
    ///  B value od ISID.
    /// </param>
    /// <param name="c">
    ///  C value od ISID.
    /// </param>
    /// <param name="d">
    ///  D value od ISID.
    /// </param>
    /// <returns>
    ///  Created ISID value.
    /// </returns>
    static member fromElem ( t : byte ) ( a : byte ) ( b : uint16 ) ( c : byte ) ( d : uint16 ) : uint64<isid_me> =
        ( ( ( uint64 t ) <<< 40 ) &&& 0x0000C00000000000UL ) |||
        ( ( ( uint64 a ) <<< 40 ) &&& 0x00003F0000000000UL ) |||
        ( ( ( uint64 b ) <<< 24 ) &&& 0x000000FFFF000000UL ) |||
        ( ( ( uint64 c ) <<< 16 ) &&& 0x0000000000FF0000UL ) |||
        ( (   uint64 d          ) &&& 0x000000000000FFFFUL )
        |> isid_me.fromPrim

/// Data types of isid_me
type ISID_T = uint64<isid_me>


[<Measure>]
type tdid_me =
    static member inline toPrim( v : uint32<tdid_me> ) : uint32 =
        uint32 v
    static member inline fromPrim( v : uint32 ) : uint32<tdid_me> =
        v * 1u<tdid_me>
    static member Zero = 0u<tdid_me>

    /// <summary>
    ///  Generate new TargetDeviceID.
    /// </summary>
    /// <param name="oldtds">
    ///  Used TargetDeviceIDs.
    /// </param>
    /// <returns>
    ///  Generated new TargetDeviceID.
    /// </returns>
    static member NewID ( oldtds : uint32<tdid_me> seq  ) : uint32<tdid_me> =
        if Seq.isEmpty oldtds then
            1u<tdid_me>
        else
            let oldTDIDs = oldtds |> Seq.map tdid_me.toPrim |> Seq.toArray
            let maxTDIDs = Array.max oldTDIDs
            let rec loop ( cnt : int ) ( vn : uint32 ) =
                if cnt > oldTDIDs.Length then
                    tdid_me.Zero
                else
                    let vn2 = if vn = 0u then 1u else vn
                    if Array.contains vn2 oldTDIDs then
                        loop ( cnt + 1 ) ( vn2 + 1u )
                    else
                        tdid_me.fromPrim vn2
            loop 0 ( maxTDIDs + 1u )

    static member fromString ( v : string ) : uint32<tdid_me> = 
        if not( Constants.TARGET_DEVICE_DIR_NAME_REGOBJ.IsMatch v ) then
            raise <| FormatException( "TargetDeviceID format error. Unexpected value \"" + v + "\"." )
        Convert.ToUInt32( v.[ Constants.TARGET_DEVICE_DIR_PREFIX.Length .. ], 16 )
        |> tdid_me.fromPrim

    static member toString ( v : uint32<tdid_me> ) : string =
        String.Format( "{0}{1:X8}", Constants.TARGET_DEVICE_DIR_PREFIX, tdid_me.toPrim v )

type TDID_T = uint32<tdid_me>

[<Measure>]
type tgid_me =
    static member inline toPrim( v : uint32<tgid_me> ) : uint32 =
        uint32 v
    static member inline fromPrim( v : uint32 ) : uint32<tgid_me> =
        v * 1u<tgid_me>
    static member Zero = 0u<tgid_me>

    /// <summary>
    ///  Generate new TargetGroupID.
    /// </summary>
    /// <param name="oldtds">
    ///  Used TargetGroupIDs.
    /// </param>
    /// <returns>
    ///  Generated new TargetGroupID.
    /// </returns>
    static member NewID ( oldtds : uint32<tgid_me> seq  ) : uint32<tgid_me> =
        if Seq.isEmpty oldtds then
            1u<tgid_me>
        else
            let oldTDIDs = oldtds |> Seq.map tgid_me.toPrim |> Seq.toArray
            let maxTDIDs = Array.max oldTDIDs
            let rec loop ( cnt : int ) ( vn : uint32 ) : uint32<tgid_me> =
                if cnt > oldTDIDs.Length then
                    tgid_me.Zero
                else
                    let vn2 = if vn = 0u then 1u else vn
                    if Array.contains vn2 oldTDIDs then
                        loop ( cnt + 1 ) ( vn2 + 1u )
                    else
                        tgid_me.fromPrim vn2
            loop 0 ( maxTDIDs + 1u )

    static member fromString ( v : string ) : uint32<tgid_me> = 
        if not( Constants.TARGET_GRP_CONFIG_FILE_NAME_REGOBJ.IsMatch v ) then
            raise <| FormatException( "TargetGroupID format error. Unexpected value \"" + v + "\"." )
        Convert.ToUInt32( v.[ Constants.TARGET_GRP_CONFIG_FILE_PREFIX.Length .. ], 16 )
        |> tgid_me.fromPrim

    static member toString ( v : uint32<tgid_me> ) : string =
        String.Format( "{0}{1:X8}", Constants.TARGET_GRP_CONFIG_FILE_PREFIX, tgid_me.toPrim v )

type TGID_T = uint32<tgid_me>

//=============================================================================
// Record definition

/// Data structure of AHS fields in PDU.
[<Struct; IsReadOnly;>]
type AHS =
    {
        /// AHSLength field.
        AHSLength : uint16;
        /// AHSType field. AHSType field is identifies the type of this AHS entry.
        AHSType : AHSTypeCd;
        /// AHS byte 3. in currently unused.
        AHSSpecific1 : byte;
        /// Extended CDB or Expected bidirectional read data length
        AHSSpecific2 : byte[];
    }

    /// <summary>get AHSLength field value</summary>
    /// <param name="i">instance value</param>
    /// <returns>AHSLength field value</returns>
    static member getAHSLength ( i : AHS ) : uint16 = i.AHSLength

    /// <summary>get AHSType field value</summary>
    /// <param name="i">instance value</param>
    /// <returns>AHSType field value</returns>
    static member getAHSType ( i : AHS ) : AHSTypeCd = i.AHSType

    /// <summary>get AHSSpecific1 field value</summary>
    /// <param name="i">instance value</param>
    /// <returns>AHSSpecific1 field value</returns>
    static member getAHSSpecific1 ( i : AHS ) : byte = i.AHSSpecific1

    /// <summary>get AHSSpecific2 field value</summary>
    /// <param name="i">instance value</param>
    /// <returns>AHSSpecific2 field value</returns>
    static member getAHSSpecific2 ( i : AHS ) : byte[] = i.AHSSpecific2

/// <summary>
///   Type of session ID
/// </summary>
/// <param name="m_ID">
///   Identifier value.
/// </param>
[<CustomEquality; CustomComparison; Struct; IsReadOnly;>]
type CtrlSessionID( m_ID : Guid ) =

    // imprementation of IComparable interface
    interface System.IComparable<CtrlSessionID> with
        /// CompareTo method
        member _.CompareTo( v : CtrlSessionID ) : int =
            m_ID.CompareTo v.Value   

    // imprementation of IEquatable interface
    interface IEquatable<CtrlSessionID> with
        /// Equals method
        member _.Equals( v : CtrlSessionID ) : bool =
            m_ID = v.Value

    interface  IEqualityComparer<CtrlSessionID> with
        member _.Equals ( v1 : CtrlSessionID, v2 : CtrlSessionID ) : bool =
            v1.Value.Equals v2.Value

        member _.GetHashCode ( v : CtrlSessionID ): int = 
            v.GetHashCode()

    interface IComparer<CtrlSessionID> with
        member _.Compare( v1 : CtrlSessionID, v2 : CtrlSessionID ) : int =
            v1.Value.CompareTo v2.Value

    /// Compare
    override _.Equals( v : obj ) : bool =
        match v with
        | :? CtrlSessionID as x ->
            m_ID = x.Value
        | _ -> false

    /// Hash code
    override _.GetHashCode() : int =
        m_ID.GetHashCode()

    /// Return string value of session ID.
    override _.ToString() : string =
        Constants.CTRL_SESS_ID_PREFIX + m_ID.ToString()

    /// Compare
    member _.Equals ( v : CtrlSessionID ) : bool =
        m_ID = v.Value

    /// Return internal guid value
    member _.Value = m_ID

    /// Return internal guid value
    static member getValue ( i : CtrlSessionID ) : Guid = i.Value

    /// Create new identifier.
    static member NewID() : CtrlSessionID =
        new CtrlSessionID( Guid.NewGuid() )

    /// Zero identifier
    static member Zero =
        new CtrlSessionID( Guid() )

    /// Parse
    new( v : string ) =
        if not ( v.StartsWith Constants.CTRL_SESS_ID_PREFIX ) then
            raise <| FormatException( "Contoroller session ID should start \"" + Constants.CTRL_SESS_ID_PREFIX + "\" prefix." )
        CtrlSessionID( System.Guid.Parse v.[ Constants.CTRL_SESS_ID_PREFIX.Length .. ] )


//=============================================================================
// Simple Exceptions

/// <summary>
///   Failed to read / write configuration exception.
/// </summary>
/// <param name="argMsg">
///   A message string that descripts cource of exception.
/// </param>
type ConfRWException( argMsg : string ) =
    inherit System.Exception( argMsg )

