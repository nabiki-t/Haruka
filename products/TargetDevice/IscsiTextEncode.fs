//=============================================================================
// Haruka Software Storage.
// IscsiTextEncode.fs : Defines the functions that used for iSCSI text format encoding.
//

//=============================================================================
// Namespace declaration

/// <summary>
///   Definitions of functions, that used for processing iSCSI text key-value data.
/// </summary>
namespace Haruka.TargetDevice

//=============================================================================
// Import declaration

open System
open System.Text

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Data type definition

/// <summary>
///   Represent receiving or sending value in text key.
/// </summary>
type TextValueType<'a> =
    /// Text key is missing ( not receiving or sending )
    | ISV_Missing
    /// The value of NotUnderstood
    | ISV_NotUnderstood
    /// The value of Irrelevant
    | ISV_Irrelevant
    /// The value of Reject
    | ISV_Reject
    /// Other effective value.
    | Value of 'a

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get holding value.
    ///   If the value is Missing, NotUnderstood, Irrelevant or Reject, a exception is thrown.
    /// </summary>
    /// <returns>
    ///   Holding value.
    /// </returns>
    member this.GetValue : 'a =
        match this with
        | Value ( x ) -> x
        | _ -> raise ( System.InvalidOperationException() )
    
    // ------------------------------------------------------------------------
    /// <summary>
    ///   Check effective value is existing or not.
    /// </summary>
    /// <returns>
    ///   If the value is not Missing, NotUnderstood, Irrelevant or Reject, result is true.
    /// </returns>
    member this.HasValue : bool =
        match this with
        | Value ( x ) -> true
        | _ -> false

//=============================================================================
/// <summary>
///   This record type has all of supported text keys.
///   It used in login or text key negotiation. 
///   Initially, this holds the target's candidate value, and updated when received initiator requests.
///   When all of keys's candidate value squeezed to only one, the negotiation can be finished.
/// </summary>
type TextKeyValues =
    {
        /// AuthMethod. Vector of None or CHAP or SRP or KRB5 or SPKM1 or SPKM2 or NotUnderstood.
        /// Haruka supports None and CHAP only.
        /// See RFC 3720 11.1
        AuthMethod : TextValueType<AuthMethodCandidateValue[]>;

        /// CHAP algorithm. Haruka suppotrts only 5 ( CHAP with MD5 )
        CHAP_A : TextValueType<uint16[]>;

        /// CHAP Identifier. See RFC 3720 11.1.4 and RFC 1994 4
        CHAP_I : TextValueType<uint16>;

        /// CHAP challenge ( binary data )
        CHAP_C : TextValueType<byte[]>;

        /// CHAP Name
        CHAP_N : TextValueType<string>;

        /// CHAP response ( binary data )
        CHAP_R : TextValueType<byte[]>;

        /// HeaderDigest. See RFC 3720 12.1
        HeaderDigest : TextValueType<DigestType[]>;

        /// DataDigest. See RFC 3720 12.1
        DataDigest : TextValueType<DigestType[]>;

        /// MaxConnections. See RFC 3720 12.2
        MaxConnections : TextValueType<uint16>;

        /// SendTargets. See RFC 3720 12.3
        SendTargets : TextValueType<string>;

        /// TargetName. See RFC 3720 12.4
        TargetName : TextValueType<string>;

        /// InitiatorName. See RFC 3720 12.5
        InitiatorName : TextValueType<string>;

        /// TargetAlias. See RFC 3720 12.6
        TargetAlias : TextValueType<string>;

        /// InitiatorAlias. See RFC 3720 12.7
        InitiatorAlias : TextValueType<string>;

        /// TargetAddress. See RFC 3720 12.8
        TargetAddress : TextValueType<string>;

        /// TargetPortalGroupTag. See RFC 3720 12.9
        TargetPortalGroupTag : TextValueType<uint16>;

        /// InitialR2T. See RFC 3720 12.10
        InitialR2T : TextValueType<bool>;

        /// ImmediateData. See RFC 3720 12.11
        ImmediateData : TextValueType<bool>;

        /// MaxRecvDataSegmentLength. See RFC 3720 12.12.
        /// MaxRecvDataSegmentLength_I is holds initiator side value of MaxRecvDataSegmentLength.
        MaxRecvDataSegmentLength_I : TextValueType<uint32>;

        /// MaxRecvDataSegmentLength. See RFC 3720 12.12.
        /// MaxRecvDataSegmentLength_T is holds target side value of MaxRecvDataSegmentLength.
        MaxRecvDataSegmentLength_T : TextValueType<uint32>;

        /// MaxBurstLength. See RFC 3720 12.13
        MaxBurstLength : TextValueType<uint32>;

        /// FirstBurstLength. See RFC 3720 12.14
        FirstBurstLength : TextValueType<uint32>;

        /// DefaultTime2Wait. See RFC 3720 12.15
        DefaultTime2Wait : TextValueType<uint16>;

        /// DefaultTime2Retain. See RFC 3720 12.16
        DefaultTime2Retain : TextValueType<uint16>;

        /// MaxOutstandingR2T. See RFC 3720 12.17
        MaxOutstandingR2T : TextValueType<uint16>;

        /// DataPDUInOrder. See RFC 3720 12.18
        DataPDUInOrder : TextValueType<bool>;

        /// DataSequenceInOrder. See RFC 3720 12.19
        DataSequenceInOrder : TextValueType<bool>;

        /// ErrorRecoveryLevel. See RFC 3720 12.20.
        /// Haruka supports ErrorRecoveryLevel in 0 only.
        ErrorRecoveryLevel : TextValueType<byte>;

        /// SessionType. See RFC 3720 12.21
        SessionType : TextValueType<string>;

        /// UnknownKeys holds unknown key name received from the initiator.
        /// It is used to responde "NotUnderstood" values.
        UnknownKeys : string[];
    }

    //=============================================================================
    /// <summary>
    ///   Default values of TextKeyValues record.
    ///   It is used to shorten the assignment of the initial value.
    /// </summary>
    static member defaultTextKeyValues : TextKeyValues =
        {
            AuthMethod = ISV_Missing;
            CHAP_A = ISV_Missing;
            CHAP_I = ISV_Missing;
            CHAP_C = ISV_Missing;
            CHAP_N = ISV_Missing;
            CHAP_R = ISV_Missing;
            HeaderDigest = ISV_Missing;
            DataDigest = ISV_Missing;
            MaxConnections = ISV_Missing;
            SendTargets = ISV_Missing;
            TargetName = ISV_Missing;
            InitiatorName = ISV_Missing;
            TargetAlias = ISV_Missing;
            InitiatorAlias = ISV_Missing;
            TargetAddress = ISV_Missing;
            TargetPortalGroupTag = ISV_Missing;
            InitialR2T = ISV_Missing;
            ImmediateData = ISV_Missing;
            MaxRecvDataSegmentLength_I = ISV_Missing;
            MaxRecvDataSegmentLength_T = ISV_Missing;
            MaxBurstLength = ISV_Missing;
            FirstBurstLength = ISV_Missing;
            DefaultTime2Wait = ISV_Missing;
            DefaultTime2Retain = ISV_Missing;
            MaxOutstandingR2T = ISV_Missing;
            DataPDUInOrder = ISV_Missing;
            DataSequenceInOrder = ISV_Missing;
            ErrorRecoveryLevel = ISV_Missing;
            SessionType = ISV_Missing;
            UnknownKeys = Array.empty;
        }

//=============================================================================
/// Text key item negotiation status
type NegoStatusValue =
    /// The text key value is negotiated, and it does not have to send or receive text key value.
    | NSV_Negotiated  = 0x00000000

    /// Initiator should send the value.
    | NSG_WaitReceive = 0x00000001

    /// Target should send the value.
    | NSG_WaitSend    = 0x00000002

//=============================================================================
/// <summary>
///   This record holds the negotiation status of all of the text key.
/// </summary>
type TextKeyValuesStatus =
    {
        NegoStat_AuthMethod : NegoStatusValue;
        NegoStat_CHAP_A : NegoStatusValue;
        NegoStat_CHAP_I : NegoStatusValue;
        NegoStat_CHAP_C : NegoStatusValue;
        NegoStat_CHAP_N : NegoStatusValue;
        NegoStat_CHAP_R : NegoStatusValue;
        NegoStat_HeaderDigest : NegoStatusValue;
        NegoStat_DataDigest : NegoStatusValue;
        NegoStat_MaxConnections : NegoStatusValue;
        NegoStat_SendTargets : NegoStatusValue;
        NegoStat_TargetName : NegoStatusValue;
        NegoStat_InitiatorName : NegoStatusValue;
        NegoStat_TargetAlias : NegoStatusValue;
        NegoStat_InitiatorAlias : NegoStatusValue;
        NegoStat_TargetAddress : NegoStatusValue;
        NegoStat_TargetPortalGroupTag : NegoStatusValue;
        NegoStat_InitialR2T : NegoStatusValue;
        NegoStat_ImmediateData : NegoStatusValue;
        NegoStat_MaxRecvDataSegmentLength_I : NegoStatusValue;
        NegoStat_MaxRecvDataSegmentLength_T : NegoStatusValue;
        NegoStat_MaxBurstLength : NegoStatusValue;
        NegoStat_FirstBurstLength : NegoStatusValue;
        NegoStat_DefaultTime2Wait : NegoStatusValue;
        NegoStat_DefaultTime2Retain : NegoStatusValue;
        NegoStat_MaxOutstandingR2T : NegoStatusValue;
        NegoStat_DataPDUInOrder : NegoStatusValue;
        NegoStat_DataSequenceInOrder : NegoStatusValue;
        NegoStat_ErrorRecoveryLevel : NegoStatusValue;
        NegoStat_SessionType : NegoStatusValue;
        NegoStat_UnknownKeys : NegoStatusValue;
    }

    //=============================================================================
    /// <summary>
    ///   The default value of TextKeyValuesStatus record.
    ///   It is used to shorten the assignment of the initial value.
    /// </summary>
    static member defaultTextKeyValuesStatus : TextKeyValuesStatus =
        {
            NegoStat_AuthMethod = NegoStatusValue.NSV_Negotiated;
            NegoStat_CHAP_A = NegoStatusValue.NSV_Negotiated;
            NegoStat_CHAP_I = NegoStatusValue.NSV_Negotiated;
            NegoStat_CHAP_C = NegoStatusValue.NSV_Negotiated;
            NegoStat_CHAP_N = NegoStatusValue.NSV_Negotiated;
            NegoStat_CHAP_R = NegoStatusValue.NSV_Negotiated;
            NegoStat_HeaderDigest = NegoStatusValue.NSV_Negotiated;
            NegoStat_DataDigest = NegoStatusValue.NSV_Negotiated;
            NegoStat_MaxConnections = NegoStatusValue.NSV_Negotiated;
            NegoStat_SendTargets = NegoStatusValue.NSV_Negotiated;
            NegoStat_TargetName = NegoStatusValue.NSV_Negotiated;
            NegoStat_InitiatorName = NegoStatusValue.NSV_Negotiated;
            NegoStat_TargetAlias = NegoStatusValue.NSV_Negotiated;
            NegoStat_InitiatorAlias = NegoStatusValue.NSV_Negotiated;
            NegoStat_TargetAddress = NegoStatusValue.NSV_Negotiated;
            NegoStat_TargetPortalGroupTag = NegoStatusValue.NSV_Negotiated;
            NegoStat_InitialR2T = NegoStatusValue.NSV_Negotiated;
            NegoStat_ImmediateData = NegoStatusValue.NSV_Negotiated;
            NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSV_Negotiated;
            NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSV_Negotiated;
            NegoStat_MaxBurstLength = NegoStatusValue.NSV_Negotiated;
            NegoStat_FirstBurstLength = NegoStatusValue.NSV_Negotiated;
            NegoStat_DefaultTime2Wait = NegoStatusValue.NSV_Negotiated;
            NegoStat_DefaultTime2Retain = NegoStatusValue.NSV_Negotiated;
            NegoStat_MaxOutstandingR2T = NegoStatusValue.NSV_Negotiated;
            NegoStat_DataPDUInOrder = NegoStatusValue.NSV_Negotiated;
            NegoStat_DataSequenceInOrder = NegoStatusValue.NSV_Negotiated;
            NegoStat_ErrorRecoveryLevel = NegoStatusValue.NSV_Negotiated;
            NegoStat_SessionType = NegoStatusValue.NSV_Negotiated;
            NegoStat_UnknownKeys = NegoStatusValue.NSV_Negotiated;
        }

type IscsiTextEncode() =

    //=============================================================================
    // function definition

    // ----------------------------------------------------------------------------
    /// Byte encoded "NotUnderstood" value.
    static member byteData_NotUnderstood : byte[] =
        Encoding.UTF8.GetBytes "NotUnderstood"

    // ----------------------------------------------------------------------------
    /// Byte encoded "Irrelevant" value.
    static member byteData_Irrelevant : byte[] =
        Encoding.UTF8.GetBytes "Irrelevant"

    // ----------------------------------------------------------------------------
    /// Byte encoded "Reject" value.
    static member byteData_Reject : byte[] =
        Encoding.UTF8.GetBytes "Reject"

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert standerd-label bytes to string.
    /// </summary>
    /// <param name="v">Bytes array that conforms standerd-label format.</param>
    /// <returns>Converted string value, or None if input bytes does not conform standerd-label format.</returns>
    static member StandardLabelBytes2String ( v : byte[] ) : string voption =
        if v.Length <= 0 || v.Length > 63 then
            ValueNone
        else
            let str = Encoding.UTF8.GetString( v )
            if Constants.ISCSI_TEXT_STANDERD_LABE_REGEX_OBJ.IsMatch( str ) then
                ValueSome( str )
            else
                ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert string to text-value bytes.
    /// </summary>
    /// <param name="s">String value that conforms standerd-label format.</param>
    /// <returns>Converted bytes array.</returns>
    /// <remarks>If input string does not conform standerd-label format, it generates an assertion.</remarks>
    static member String2StandardLabelBytes ( s : string ) : byte[] =
        assert( Constants.ISCSI_TEXT_STANDERD_LABE_REGEX_OBJ.IsMatch( s ) )
        Encoding.UTF8.GetBytes s

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert text-value bytes to string.
    /// </summary>
    /// <param name="v">Bytes array that conforms text-value format.</param>
    /// <returns>Converted string value, or None if input bytes does not conform text-value format.</returns>
    static member TextValueBytes2String ( v : byte[] ) : string voption =
        let str = Encoding.UTF8.GetString( v )
        if Constants.ISCSI_TEXT_TEXT_VALUE_REGEX_OBJ.IsMatch( str ) then
            ValueSome( str )
        else
            ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert string to text-value bytes.
    /// </summary>
    /// <param name="s">String value that conforms text-value format.</param>
    /// <returns>Converted bytes array.</returns>
    /// <remarks>If input string does not conform text-value format, it generates an assertion.</remarks>
    static member String2TextValueBytes ( s : string ) : byte[] =
        assert( Constants.ISCSI_TEXT_TEXT_VALUE_REGEX_OBJ.IsMatch( s ) )
        Encoding.UTF8.GetBytes s

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert iSCSI-name-value bytes to string.
    /// </summary>
    /// <param name="v">Bytes array that conforms iSCSI-name-value format.</param>
    /// <returns>Converted string value, or None if input bytes does not conform iSCSI-name-value format.</returns>
    static member ISCSINameValueBytes2String ( v : byte[] ) : string voption =
        let str = Encoding.UTF8.GetString( v )
        if Constants.ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_OBJ.IsMatch( str ) then
            ValueSome( str )
        else
            ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert string to iSCSI-name-value bytes.
    /// </summary>
    /// <param name="s">String value that conforms iSCSI-name-value format.</param>
    /// <returns>Converted bytes array.</returns>
    /// <remarks>If input string does not conform iSCSI-name-value format, it generates an assertion.</remarks>
    static member String2ISCSINameValueBytes ( s : string ) : byte[] =
        assert( Constants.ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_OBJ.IsMatch( s ) )
        Encoding.UTF8.GetBytes s
 
    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert iSCSI-local-name-value bytes to string.
    /// </summary>
    /// <param name="v">Bytes array that conforms iSCSI-local-name-value format.</param>
    /// <returns>Converted string value, or None if input bytes does not conform iSCSI-local-name-value.</returns>
    static member ISCSILocalNameValueBytes2String ( v : byte[] ) : string voption =
        ValueSome( Encoding.UTF8.GetString( v ) )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert string to iSCSI-local-name-value bytes.
    /// </summary>
    /// <param name="s">String value that conforms iSCSI-local-name-value format.</param>
    /// <returns>Converted bytes array.</returns>
    static member String2ISCSILocalNameValueBytes ( s : string ) : byte[] =
        Encoding.UTF8.GetBytes s

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert boolean-value bytes to bool value.
    /// </summary>
    /// <param name="v">Bytes array that forms "Yes" or "No", in string encoded.</param>
    /// <returns>boolean value, or None if input bytes does not specified value.</returns>
    static member BooleanValueBytes2Bool ( v : byte[] ) : bool voption =
        if v = [| byte 'Y'; byte 'e'; byte 's' |] then
            ValueSome( true )
        elif v = [| byte 'N'; byte 'o'; |] then
            ValueSome( false )
        else
            ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert bool to boolean-value bytes.
    /// </summary>
    /// <param name="b">boolean value.</param>
    /// <returns>Converted bytes array.</returns>
    static member Bool2BooleanValueBytes ( b : bool ) : byte[] =
        if b then [| byte 'Y'; byte 'e'; byte 's' |] else [| byte 'N'; byte 'o'; |]

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert numerical-value bytes to a byte value.
    /// </summary>
    /// <param name="v">Bytes array that conforms numerical-value format.</param>
    /// <param name="min">Minimal value allowed as input value.</param>
    /// <param name="max">Maximum value allowed as input value.</param>
    /// <returns>
    ///   Converted a byte value, or if v does not conform numerical-value format or out of range values, it returns None.
    /// </returns>
    static member NumericalValueBytes2byte ( v : byte[] ) ( min : byte ) ( max : byte ) : byte voption =
        let str = Encoding.UTF8.GetString( v )
        if v.Length > 2 && v.[0] = byte '0' && ( v.[1] = byte 'x' || v.[1] = byte 'X' ) then
            try
                let r = System.Convert.ToByte( str, 16 )
                if r < min || r > max then
                    ValueNone
                else
                    ValueSome( r )
            with
            | _ ->
                ValueNone
        else
            let ( r, v ) = System.Byte.TryParse( str )
            if r && v >= min && v <= max then
                ValueSome( v )
            else
                ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert a byte value to numerical-value bytes
    /// </summary>
    /// <param name="v">A byte value.</param>
    /// <returns>Converted bytes array. It conforms numerical-value format.</returns>
    static member bytetoNumericalValueBytes ( v : byte ) : byte[] =
        String.Format( "{0}", v )
        |> Encoding.UTF8.GetBytes

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert numerical-value bytes to a uint16 value.
    /// </summary>
    /// <param name="v">Bytes array that conforms numerical-value format.</param>
    /// <param name="min">Minimal value allowed as input value.</param>
    /// <param name="max">Maximum value allowed as input value.</param>
    /// <returns>
    ///   Converted a uint16 value, or if v does not conform numerical-value format or out of range values, it returns None.
    /// </returns>
    static member NumericalValueBytes2uint16 ( v : byte[] ) ( min : uint16 ) ( max : uint16 ) : uint16 voption =
        let str = Encoding.UTF8.GetString( v )
        if v.Length > 2 && v.[0] = byte '0' && ( v.[1] = byte 'x' || v.[1] = byte 'X' ) then
            try
                let r = System.Convert.ToUInt16( str, 16 )
                if r < min || r > max then
                    ValueNone
                else
                    ValueSome( r )
            with
            | _ ->
                ValueNone
        else
            let ( r, v ) = System.UInt16.TryParse( str )
            if r && v >= min && v <= max then
                ValueSome( v )
            else
                ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert a uint16 value to numerical-value bytes
    /// </summary>
    /// <param name="v">A uint16 value.</param>
    /// <returns>Converted bytes array. It conforms numerical-value format.</returns>
    static member uint16toNumericalValueBytes ( v : uint16 ) : byte[] =
        String.Format( "{0}", v )
        |> Encoding.UTF8.GetBytes

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert numerical-value bytes to a uint32 value.
    /// </summary>
    /// <param name="v">Bytes array that conforms numerical-value format.</param>
    /// <param name="min">Minimal value allowed as input value.</param>
    /// <param name="max">Maximum value allowed as input value.</param>
    /// <returns>
    ///   Converted a uint32 value, or if v does not conform numerical-value format or out of range values, it returns None.
    /// </returns>
    static member NumericalValueBytes2uint32 ( v : byte[] ) ( min : uint32 ) ( max : uint32 ) : uint32 voption =
        let str = Encoding.UTF8.GetString( v )
        if v.Length > 2 && v.[0] = byte '0' && ( v.[1] = byte 'x' || v.[1] = byte 'X' ) then
            try
                let r = System.Convert.ToUInt32( str, 16 )
                if r < min || r > max then
                    ValueNone
                else
                    ValueSome( r )
            with
            | _ ->
                ValueNone
        else
            let ( r, v ) = System.UInt32.TryParse( str )
            if r && v >= min && v <= max then
                ValueSome( v )
            else
                ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert a uint32 value to numerical-value bytes
    /// </summary>
    /// <param name="v">A uint32 value.</param>
    /// <returns>Converted bytes array. It conforms numerical-value format.</returns>
    static member uint32toNumericalValueBytes ( v : uint32 ) : byte[] =
        String.Format( "{0}", v )
        |> Encoding.UTF8.GetBytes

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert numerical-value bytes to a uint64 value.
    /// </summary>
    /// <param name="v">Bytes array that conforms numerical-value format.</param>
    /// <param name="min">Minimal value allowed as input value.</param>
    /// <param name="max">Maximum value allowed as input value.</param>
    /// <returns>
    ///   Converted a uint64 value, or if v does not conform numerical-value format or out of range values, it returns None.
    /// </returns>
    static member NumericalValueBytes2uint64 ( v : byte[] ) ( min : uint64 ) ( max : uint64 ) : uint64 voption =
        let str = Encoding.UTF8.GetString( v )
        if v.Length > 2 && v.[0] = byte '0' && ( v.[1] = byte 'x' || v.[1] = byte 'X' ) then
            try
                let r = System.Convert.ToUInt64( str, 16 )
                if r < min || r > max then
                    ValueNone
                else
                    ValueSome( r )

            with
            | _ ->
                ValueNone
        else
            let ( r, v ) = System.UInt64.TryParse( str )
            if r && v >= min && v <= max then
                ValueSome( v )
            else
                ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert a uint64 value to numerical-value bytes
    /// </summary>
    /// <param name="v">A uint64 value.</param>
    /// <returns>Converted bytes array. It conforms numerical-value format.</returns>
    static member uint64toNumericalValueBytes ( v : uint64 ) : byte[] =
        String.Format( "{0}", v )
        |> Encoding.UTF8.GetBytes

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert base64-constant bytes to binary
    /// </summary>
    /// <param name="v">Bytes array that conforms base64-constant format.</param>
    /// <returns>
    ///   Decoded bytes array of input base64 string, or if the input value does not conform base64-constant, it returns None.
    /// </returns>
    static member Base64ConstantBytes2Binary ( v : byte[] ) : byte[] voption =
        if not( v.Length > 2 && v.[0] = byte '0' && ( v.[1] = byte 'b' || v.[1] = byte 'B' ) ) then
            // Consider specified data is not base64 encoded value.
            ValueNone
        else
            let wp = Array.findIndexBack ( (<>) ( byte '=' ) ) v
            let b64CharCount = wp - 1
            if wp < 2 || ( ( b64CharCount - 1 ) % 4 ) = 0 then
                // encoded string is invalid length.
                ValueNone
            else
                // Decode base64 string to bytes array.
                let outbuf : byte[] = Array.zeroCreate( b64CharCount * 3 / 4 )
                let trans = new System.Security.Cryptography.FromBase64Transform()
                let inputBlockSize = 4
                let outputBlockSize = trans.OutputBlockSize
                let rec floop inpos outpos =
                    if v.Length - inpos > inputBlockSize then
                        trans.TransformBlock( v, inpos, v.Length - inpos, outbuf, outpos ) |> ignore
                        floop ( inpos + inputBlockSize ) ( outpos + outputBlockSize )
                    else
                        ( inpos, outpos )
                let lastinpos, lastoutput = floop 2 0
                let lastblock = trans.TransformFinalBlock( v, lastinpos, v.Length - lastinpos )
                if lastblock.Length > outbuf.Length - lastoutput then
                    ValueNone
                else
                    Array.blit lastblock 0 outbuf lastoutput lastblock.Length
                    ValueSome( outbuf )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert binary data value to base64-constant bytes.
    /// </summary>
    /// <param name="v">Bytes array that holds any binary data.</param>
    /// <returns>Encoded bytes array that conforms base64-constant format.</returns>
    static member Binary2Base64ConstantBytes ( v : byte[] ) : byte[] =
        // Encode bytes array to base64 string.
        let EncodedLength = Functions.AddPaddingLengthInt32 ( v.Length * 4 / 3 ) 4

        if EncodedLength <= 0 then
            Array.empty
        else
            let outbuf : byte[] = Array.zeroCreate( EncodedLength + 2 )
            let trans = new System.Security.Cryptography.ToBase64Transform()
            outbuf.[0] <- byte '0'
            outbuf.[1] <- byte 'b'

            let rec floop inpos outpos =
                if v.Length - inpos > trans.InputBlockSize then
                    trans.TransformBlock( v, inpos, trans.InputBlockSize, outbuf, outpos ) |> ignore
                    floop ( inpos + trans.InputBlockSize ) ( outpos + trans.OutputBlockSize )
                else
                    ( inpos, outpos )
            let lastinpos, lastoutput = floop 0 2
            let lastblock = trans.TransformFinalBlock( v, lastinpos, v.Length - lastinpos )
            Array.blit lastblock 0 outbuf lastoutput lastblock.Length
            outbuf

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert hex-constant bytes to binary
    /// </summary>
    /// <param name="v">Bytes array that conforms hex-constant format.</param>
    /// <returns>
    ///   Decoded bytes array of input string, or if the input value does not conform hex-constant, it returns None.
    /// </returns>
    static member HexConstantBytes2Binary ( v : byte[] ) : byte[] voption =
        if not( v.Length > 2 && v.[0] = byte '0' && ( v.[1] = byte 'x' || v.[1] = byte 'X' ) ) then
            // Consider specified data is not hex-constant value.
            ValueNone
        else
            if v.Length % 2 <> 0 then
                // encoded string is invalid length
                ValueNone
            else
                let h2d c =
                    if c >= byte '0' && c <= byte '9' then
                        c - byte '0'
                    elif c >= byte 'A' && c <= byte 'F' then    
                        ( c - byte 'A' ) + 10uy
                    elif c >= byte 'a' && c <= byte 'f' then
                        ( c - byte 'a' ) + 10uy
                    else
                        0uy
                ValueSome( [|
                        for i = 1 to ( v.Length - 1 ) / 2 do
                            yield ( ( h2d v.[ i * 2 ] ) <<< 4 ) ||| ( h2d v.[ i * 2 + 1 ] )
                |] )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert binary data value to hex-constant bytes.
    /// </summary>
    /// <param name="v">Bytes array that holds any binary data.</param>
    /// <returns>Encoded bytes array that conforms hex-constant format.</returns>
    static member Binary2HexConstantBytes ( v : byte[] ) : byte[] =
        if v.Length = 0 then
            [||]
        else
            let outbuf : byte[] = Array.zeroCreate( v.Length * 2 + 2 )
            outbuf.[0] <- byte '0'
            outbuf.[1] <- byte 'x'
            let d2h ( b : byte ) = if b <= 9uy then b + byte '0' else ( b - 10uy ) + byte 'A'
            for i = 0 to v.Length - 1 do
                outbuf.[ i * 2 + 2 ] <- d2h ( v.[i] >>> 4 )
                outbuf.[ i * 2 + 3 ] <- d2h ( v.[i] &&& 0x0Fuy )
            outbuf

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert decimal-constant bytes to binary
    /// </summary>
    /// <param name="v">Bytes array that conforms decimal-constant format.</param>
    /// <returns>
    ///   Decoded bytes array of input string, or if the input value does not conform decimal-constant, it returns None.
    /// </returns>
    static member DecimalConstantBytes2Binary ( v : byte[] ) : byte[] voption =
        let ( r, dv ) = Int64.TryParse( Encoding.UTF8.GetString( v ) )
        if r then
            if dv = 0L then
                ValueSome( [| 0uy |] )
            else
                let decbuf : byte[] =
                    BitConverter.GetBytes(
                        uint64( Net.IPAddress.NetworkToHostOrder( dv ) )
                    )
                let wpos = Array.findIndex ( (<>) 0uy ) decbuf
                ValueSome( decbuf.[ wpos .. ] )
        else
            ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert binary-value encoded bytes to binary data.
    /// </summary>
    /// <param name="v">Bytes array that conforms binary-value format.</param>
    /// <returns>
    ///   Decoded bytes array of input binary-value data, or if the input value does not conform binary-value, it returns None.
    /// </returns>
    static member BinaryValueBytes2Binary ( v : byte[] ) : byte[] voption =
        if v.Length > 2 && v.[0] = byte '0' && ( v.[1] = byte 'b' || v.[1] = byte 'B' ) then
            IscsiTextEncode.Base64ConstantBytes2Binary v
        elif v.Length > 2 && v.[0] = byte '0' && ( v.[1] = byte 'x' || v.[1] = byte 'X' ) then
            IscsiTextEncode.HexConstantBytes2Binary v
        else
            IscsiTextEncode.DecimalConstantBytes2Binary v

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert large-binary-value encoded bytes to binary data.
    /// </summary>
    /// <param name="v">Bytes array that conforms large-binary-value format.</param>
    /// <returns>
    ///   Decoded bytes array of input large-binary-value data,
    ///   or if the input value does not conform large-binary-value, it returns None.
    /// </returns>
    static member LargeBinaryValueBytes2Binary ( v : byte[] ) : byte[] voption =
        if v.Length > 2 && v.[0] = byte '0' && ( v.[1] = byte 'b' || v.[1] = byte 'B' ) then
            IscsiTextEncode.Base64ConstantBytes2Binary v
        elif v.Length > 2 && v.[0] = byte '0' && ( v.[1] = byte 'x' || v.[1] = byte 'X' ) then
            IscsiTextEncode.HexConstantBytes2Binary v
        else
            ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert numeric-range bytes to a uint16 pair.
    /// </summary>
    /// <param name="v">Bytes array that conforms numeric-range format.</param>
    /// <param name="min">Minimal value allowed as input value.</param>
    /// <param name="max">Maximum value allowed as input value.</param>
    /// <returns>
    ///   Converted pair of uint16 values , or if v does not conform numeric-range format or out of range values, it returns None.
    /// </returns>
    static member NumericRangeBytes2uint16Pair ( v : byte[] ) ( min : uint16 ) ( max : uint16 )  : struct ( uint16 * uint16 ) voption =
        try
            let pos = Array.findIndex ( (=) ( byte '~' ) ) v
            let minV = IscsiTextEncode.NumericalValueBytes2uint16 v.[ 0 .. pos - 1 ] min max
            let maxV = IscsiTextEncode.NumericalValueBytes2uint16 v.[ pos + 1 .. ] min max
            if minV = ValueNone || maxV = ValueNone || minV.Value > maxV.Value then
                ValueNone
            else
                ValueSome( minV.Value, maxV.Value )
        with
        | _ -> ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert numeric-range bytes to uint32 pair.
    /// </summary>
    /// <param name="v">Bytes array that conforms numeric-range format.</param>
    /// <param name="min">Minimal value allowed as input value.</param>
    /// <param name="max">Maximum value allowed as input value.</param>
    /// <returns>
    ///   Converted pair of uint32 values , or if v does not conform numeric-range format or out of range values, it returns None.
    /// </returns>
    static member NumericRangeBytes2uint32Pair ( v : byte[] ) ( min : uint32 ) ( max : uint32 ) : struct ( uint32 * uint32 ) voption =
        try
            let pos = Array.findIndex ( (=) ( byte '~' ) ) v
            let minV = IscsiTextEncode.NumericalValueBytes2uint32 v.[ 0 .. pos - 1 ] min max
            let maxV = IscsiTextEncode.NumericalValueBytes2uint32 v.[ pos + 1 .. ] min max
            if minV = ValueNone || maxV = ValueNone || minV.Value > maxV.Value then
                ValueNone
            else
                ValueSome( minV.Value, maxV.Value )
        with
        | _ -> ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert numeric-range bytes to uint64 pair.
    /// </summary>
    /// <param name="v">Bytes array that conforms numeric-range format.</param>
    /// <param name="min">Minimal value allowed as input value.</param>
    /// <param name="max">Maximum value allowed as input value.</param>
    /// <returns>
    ///   Converted pair of uint64 values , or if v does not conform numeric-range format or out of range values, it returns None.
    /// </returns>
    static member NumericRangeBytes2uint64Pair ( v : byte[] ) ( min : uint64 ) ( max : uint64 ) : struct ( uint64 * uint64 ) voption =
        try
            let pos = Array.findIndex ( (=) ( byte '~' ) ) v
            let minV = IscsiTextEncode.NumericalValueBytes2uint64 v.[ 0 .. pos - 1 ] min max
            let maxV = IscsiTextEncode.NumericalValueBytes2uint64 v.[ pos + 1 .. ] min max
            if minV = ValueNone || maxV = ValueNone || minV.Value > maxV.Value then
                ValueNone
            else
                ValueSome( minV.Value, maxV.Value )
        with
        | _ -> ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert list-of-values bytes to strings array.
    /// </summary>
    /// <param name="v">Bytes array that conforms list-of-values format.</param>
    /// <returns>
    ///   Converted array of strings , or if v does not conform list-of-values format, it returns None.
    /// </returns>
    static member ListOfValuesBytes2Strings ( v : byte[] ) : ( string[] ) voption =
        if v.Length <= 0 then
            ValueNone
        else
            let str = Encoding.UTF8.GetString( v )
            if Constants.ISCSI_TEXT_LIST_OF_VALUES_REGEX_OBJ.IsMatch( str ) then
                ValueSome( str.Split( ',' ) )
            else
                ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert string vector to list-of-values bytes
    /// </summary>
    /// <param name="v">Array of strings</param>
    /// <returns>Converted bytes array that conforms list-of-values format.</returns>
    static member Strings2ListOfValuesBytes ( v : string[] ) : ( byte[] ) =
        let w =
            Array.collect ( fun ( x :string ) ->
                [|
                    yield! ( Encoding.UTF8.GetBytes x )
                    yield ','B;
                |]
            ) v
        w.[ 0 .. w.Length - 2 ]

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert list-of-values bytes to uint16 array.
    /// </summary>
    /// <param name="v">Bytes array that conforms list-of-values format.</param>
    /// <returns>
    ///   Converted array of uint16 values , or if v does not conform list-of-values format, it returns None.
    /// </returns>
    static member ListOfValuesBytes2uint16 ( v : byte[] ) : ( uint16[] ) voption =
        try
            v
            |> IscsiTextEncode.ListOfValuesBytes2Strings
            |> ValueOption.get
            |> Array.map UInt16.Parse 
            |> ValueSome
        with
        | _ -> ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert array of uint16 values to list-of-values bytes.
    /// </summary>
    /// <param name="v">Array of uint16 values</param>
    /// <returns>Converted bytes array that conforms list-of-values format.</returns>
    static member uint16ToListOfValuesBytes ( v : uint16[] ) : ( byte[] ) =
        let w =
            Array.collect ( fun ( x :uint16 ) ->
                [|
                    yield! ( String.Format( "{0}", x ) |> Encoding.UTF8.GetBytes )
                    yield ','B;
                |]
            ) v
        w.[ 0 .. w.Length - 2 ]

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert list-of-values bytes to uint32 array.
    /// </summary>
    /// <param name="v">Bytes array that conforms list-of-values format.</param>
    /// <returns>
    ///   Converted array of uint32 values , or if v does not conform list-of-values format, it returns None.
    /// </returns>
    static member ListOfValuesBytes2uint32 ( v : byte[] ) : ( uint32[] ) voption =
        try
            v
            |> IscsiTextEncode.ListOfValuesBytes2Strings
            |> ValueOption.get
            |> Array.map UInt32.Parse 
            |> ValueSome
        with
        | _ -> ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert array of uint32 values to list-of-values bytes.
    /// </summary>
    /// <param name="v">Array of uint32 values</param>
    /// <returns>Converted bytes array that conforms list-of-values format.</returns>
    static member uint32ToListOfValuesBytes ( v : uint32[] ) : ( byte[] ) =
        let w =
            Array.collect ( fun ( x :uint32 ) ->
                [|
                    yield! ( String.Format( "{0}", x ) |> Encoding.UTF8.GetBytes )
                    yield ','B;
                |]
            ) v
        w.[ 0 .. w.Length - 2 ]

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert list-of-values bytes to uint64 array.
    /// </summary>
    /// <param name="v">Bytes array that conforms list-of-values format.</param>
    /// <returns>
    ///   Converted array of uint64 values , or if v does not conform list-of-values format, it returns None.
    /// </returns>
    static member ListOfValuesBytes2uint64 ( v : byte[] ) : ( uint64[] ) voption =
        try
            v
            |> IscsiTextEncode.ListOfValuesBytes2Strings
            |> ValueOption.get
            |> Array.map UInt64.Parse 
            |> ValueSome
        with
        | _ -> ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert array of uint64 values to list-of-values bytes.
    /// </summary>
    /// <param name="v">Array of uint64 values</param>
    /// <returns>Converted bytes array that conforms list-of-values format.</returns>
    static member uint64ToListOfValuesBytes ( v : uint64[] ) : ( byte[] ) =
        let w =
            Array.collect ( fun ( x :uint64 ) ->
                [|
                    yield! ( String.Format( "{0}", x ) |> Encoding.UTF8.GetBytes )
                    yield ','B;
                |]
            ) v
        w.[ 0 .. w.Length - 2 ]

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Convert bytes to key-value pairs.
    ///   This function converts a bytes array to generic key-value pair value.
    ///   It does not recognize the key name or data type specified in iSCSI.
    /// </summary>
    /// <param name="v">Bytes array that forms key-value pair strings.</param>
    /// <returns>
    ///   An array of key-value pairs. Data type of value is bytes array.
    ///   The value is not casted to specific data type correspond to the key name.
    ///   If input bytes array does not conform iSCSI specified format, it returns None.
    /// </returns>
    /// <remarks>
    ///   Received text-key string in Login request/response or text key negotiation PDUs are converted by this function.
    ///   One text-key string may be divited one or more PDUs, thus this function accepts an array of bytes arrays.
    /// </remarks>
    static member TextKeyData2KeyValues ( v : byte[][] ) : ( struct ( string * TextValueType<byte[]> )[] ) voption =
        try
            let w =
                [| for itr in v do yield! itr |]
                |> Functions.SplitByteArray 0x00uy
                |> List.filter ( fun x -> x.Length <> 0 )
            if w.Length = 0 then
                // If received text key-value data is empty,
                // it is handled normal case.
                // But others ( length > 0 and contain null chars only ), it consider error pattern.
                if 0 = Array.sumBy ( fun ( itr : byte[] ) -> itr.Length ) v then
                    ValueSome( Array.empty )
                else
                    ValueNone
            else
                List.map
                    (
                        fun itr ->
                            let idx = Array.findIndex ( (=) ( byte '=' ) ) itr
                            let keyName = IscsiTextEncode.StandardLabelBytes2String itr.[ 0 .. idx - 1 ]
                            let value = itr.[ idx + 1 .. ]
                            if value = IscsiTextEncode.byteData_NotUnderstood then
                                struct ( keyName.Value, TextValueType.ISV_NotUnderstood )
                            elif value = IscsiTextEncode.byteData_Irrelevant then
                                struct ( keyName.Value, TextValueType.ISV_Irrelevant )
                            elif value = IscsiTextEncode.byteData_Reject then
                                struct ( keyName.Value, TextValueType.ISV_Reject )
                            else
                                struct ( keyName.Value, TextValueType.Value( value ) )
                    ) w
                |> List.toArray
                |> ValueSome
        with
        | _ -> ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Search a value from a generic key value pairs array correspongind speficied key name.
    /// </summary>
    /// <param name="keyName">
    ///   Specify the key name you want to search.
    /// </param>
    /// <param name="argKeyValue">
    ///   Key value pairs array.
    /// </param>
    /// <returns>
    ///   Searched value, or if specified key is missing, it returns TextValueType.ISV_Missing.
    /// </returns>
    static member SearchTextKeyValue ( keyName : string ) ( argKeyValue : struct ( string * TextValueType<byte[]> ) [] ) : TextValueType<byte[]> =
        let findResult =
            argKeyValue
            |> Array.tryFindBack ( fun itr ->
                let struct( wname, _ ) = itr
                wname = keyName
            )
        match findResult  with
        | None ->
            ISV_Missing
        | Some( x ) ->
            let struct( _, kv ) = x
            kv

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Update a TextKeyValues record value by received a key value pair.
    /// </summary>
    /// <param name="isTargetVal">
    ///   Specified key value pair is received from the target or not.
    ///   If isTargetVal is true, it updates MaxRecvDataSegmentLength_T value, and if not, it updates MaxRecvDataSegmentLength_I.
    /// </param>
    /// <param name="recTKV">
    ///   A TextKeyValues record before update.
    /// </param>
    /// <param name="keyName">
    ///   Key name string of received key-value pair.
    /// </param>
    /// <param name="valDat">
    ///   Value of received key-value pair.
    /// </param>
    /// <returns>
    ///   Updated a TextKeyValues record.
    /// </returns>
    /// <remarks>
    ///   It is expected that the value created by TextKeyData2KeyValues function is specified for valDat argument.
    /// </remarks>
    static member UpdateTextKeyValuesRecord ( isTargetVal : bool ) ( recTKV : TextKeyValues ) struct ( keyName : string, valDat : TextValueType<byte[]> ) =
        let tranceDataType ( f : byte[] -> 'a ) : TextValueType<'a> =
            match valDat with
            | ISV_Missing ->
                ISV_Missing
            | ISV_NotUnderstood ->
                ISV_NotUnderstood
            | ISV_Irrelevant ->
                ISV_Irrelevant
            | ISV_Reject ->
                ISV_Reject
            | Value( x ) ->
                TextValueType.Value( f x )

        if String.Equals( keyName, "AuthMethod", StringComparison.Ordinal ) then {
            recTKV with
                AuthMethod = 
                    tranceDataType ( 
                        IscsiTextEncode.ListOfValuesBytes2Strings
                        >> ValueOption.get
                        >> Array.map AuthMethodCandidateValue.fromStringValue
                    )
            }
        elif String.Equals( keyName, "CHAP_A", StringComparison.Ordinal ) then {
            recTKV with
                CHAP_A = tranceDataType ( IscsiTextEncode.ListOfValuesBytes2uint16 >> ValueOption.get )
            }
        elif String.Equals( keyName, "CHAP_I", StringComparison.Ordinal ) then {
            recTKV with
                CHAP_I = tranceDataType ( fun x -> ( IscsiTextEncode.NumericalValueBytes2uint16 x 0us 255us ) |> ValueOption.get )
            }
        elif String.Equals( keyName, "CHAP_C", StringComparison.Ordinal ) then {
            recTKV with
                CHAP_C = tranceDataType ( IscsiTextEncode.LargeBinaryValueBytes2Binary >> ValueOption.get )
            }
        elif String.Equals( keyName, "CHAP_N", StringComparison.Ordinal ) then {
            recTKV with
                CHAP_N = tranceDataType ( IscsiTextEncode.TextValueBytes2String >> ValueOption.get )
            }
        elif String.Equals( keyName, "CHAP_R", StringComparison.Ordinal ) then {
            recTKV with
                CHAP_R = tranceDataType ( IscsiTextEncode.LargeBinaryValueBytes2Binary >> ValueOption.get )
            }
        elif String.Equals( keyName, "HeaderDigest", StringComparison.Ordinal ) then {
            recTKV with
                HeaderDigest =
                    tranceDataType (
                        IscsiTextEncode.ListOfValuesBytes2Strings
                        >> ValueOption.get
                        >> Array.map (
                            fun v ->
                                if String.Equals( v, "CRC32C", StringComparison.Ordinal ) then DigestType.DST_CRC32C
                                elif String.Equals( v, "None", StringComparison.Ordinal ) then DigestType.DST_None
                                else DigestType.DST_NotUnderstood
                        ) 
                    )
            }
        elif String.Equals( keyName, "DataDigest", StringComparison.Ordinal ) then {
            recTKV with
                DataDigest = 
                    tranceDataType (
                        IscsiTextEncode.ListOfValuesBytes2Strings
                        >> ValueOption.get
                        >> Array.map (
                            fun v ->
                                if v = "CRC32C" then DigestType.DST_CRC32C
                                elif v = "None" then DigestType.DST_None
                                else DigestType.DST_NotUnderstood
                        ) 
                    )
            }
        elif String.Equals( keyName, "MaxConnections", StringComparison.Ordinal ) then {
            recTKV with
                MaxConnections = tranceDataType ( fun x -> ( IscsiTextEncode.NumericalValueBytes2uint16 x 1us 65535us ) |> ValueOption.get )
            }
        elif String.Equals( keyName, "SendTargets", StringComparison.Ordinal ) then {
            recTKV with
                SendTargets = tranceDataType ( IscsiTextEncode.TextValueBytes2String >> ValueOption.get )
            }
        elif String.Equals( keyName, "TargetName", StringComparison.Ordinal ) then {
            recTKV with
                TargetName = tranceDataType ( IscsiTextEncode.ISCSINameValueBytes2String >> ValueOption.get )
            }
        elif String.Equals( keyName, "InitiatorName", StringComparison.Ordinal ) then {
            recTKV with
                InitiatorName = tranceDataType ( IscsiTextEncode.ISCSINameValueBytes2String >> ValueOption.get )
            }
        elif String.Equals( keyName, "TargetAlias", StringComparison.Ordinal ) then {
            recTKV with
                TargetAlias = tranceDataType ( IscsiTextEncode.ISCSILocalNameValueBytes2String >> ValueOption.get )
            }
        elif String.Equals( keyName, "InitiatorAlias", StringComparison.Ordinal ) then {
            recTKV with
                InitiatorAlias = tranceDataType ( IscsiTextEncode.ISCSILocalNameValueBytes2String >> ValueOption.get )
            }
        elif String.Equals( keyName, "TargetAddress", StringComparison.Ordinal ) then {
            recTKV with
                TargetAddress = tranceDataType ( IscsiTextEncode.ISCSILocalNameValueBytes2String >> ValueOption.get )
            }
        elif String.Equals( keyName, "TargetPortalGroupTag", StringComparison.Ordinal ) then {
            recTKV with
                TargetPortalGroupTag = tranceDataType ( fun x -> ( IscsiTextEncode.NumericalValueBytes2uint16 x 0us 65535us ) |> ValueOption.get )
            }
        elif String.Equals( keyName, "InitialR2T", StringComparison.Ordinal ) then {
            recTKV with
                InitialR2T = tranceDataType ( IscsiTextEncode.BooleanValueBytes2Bool >> ValueOption.get )
            }
        elif String.Equals( keyName, "ImmediateData", StringComparison.Ordinal ) then {
            recTKV with
                ImmediateData = tranceDataType ( IscsiTextEncode.BooleanValueBytes2Bool >> ValueOption.get )
            }
        elif String.Equals( keyName, "MaxRecvDataSegmentLength", StringComparison.Ordinal ) then
            if isTargetVal then {
                recTKV with
                    MaxRecvDataSegmentLength_T = tranceDataType ( fun x -> ( IscsiTextEncode.NumericalValueBytes2uint32 x 512u 0x00FFFFFFu ) |> ValueOption.get )
                }
            else {
                recTKV with
                    MaxRecvDataSegmentLength_I = tranceDataType ( fun x -> ( IscsiTextEncode.NumericalValueBytes2uint32 x 512u 0xFFFFFFu ) |> ValueOption.get )
                }
        elif String.Equals( keyName, "MaxBurstLength", StringComparison.Ordinal ) then {
            recTKV with
                MaxBurstLength = tranceDataType ( fun x -> ( IscsiTextEncode.NumericalValueBytes2uint32 x 512u 0xFFFFFFu ) |> ValueOption.get )
            }
        elif String.Equals( keyName, "FirstBurstLength", StringComparison.Ordinal ) then {
            recTKV with
                FirstBurstLength = tranceDataType ( fun x -> ( IscsiTextEncode.NumericalValueBytes2uint32 x 512u 0xFFFFFFu ) |> ValueOption.get )
            }
        elif String.Equals( keyName, "DefaultTime2Wait", StringComparison.Ordinal ) then {
            recTKV with
                DefaultTime2Wait = tranceDataType ( fun x -> ( IscsiTextEncode.NumericalValueBytes2uint16 x 0us 3600us ) |> ValueOption.get )
            }
        elif String.Equals( keyName, "DefaultTime2Retain", StringComparison.Ordinal ) then {
            recTKV with
                DefaultTime2Retain = tranceDataType ( fun x -> ( IscsiTextEncode.NumericalValueBytes2uint16 x 0us 3600us ) |> ValueOption.get )
            }
        elif String.Equals( keyName, "MaxOutstandingR2T", StringComparison.Ordinal ) then {
            recTKV with
                MaxOutstandingR2T = tranceDataType ( fun x -> ( IscsiTextEncode.NumericalValueBytes2uint16 x 1us 65535us ) |> ValueOption.get )
            }
        elif String.Equals( keyName, "DataPDUInOrder", StringComparison.Ordinal ) then {
            recTKV with
                DataPDUInOrder = tranceDataType ( IscsiTextEncode.BooleanValueBytes2Bool >> ValueOption.get )
            }
        elif String.Equals( keyName, "DataSequenceInOrder", StringComparison.Ordinal ) then {
            recTKV with
                DataSequenceInOrder = tranceDataType ( IscsiTextEncode.BooleanValueBytes2Bool >> ValueOption.get )
            }
        elif String.Equals( keyName, "ErrorRecoveryLevel", StringComparison.Ordinal ) then {
            recTKV with
                ErrorRecoveryLevel = tranceDataType ( fun x -> ( IscsiTextEncode.NumericalValueBytes2byte x 0uy 2uy ) |> ValueOption.get )
            }
        elif String.Equals( keyName, "SessionType", StringComparison.Ordinal ) then
            let ws = tranceDataType ( IscsiTextEncode.TextValueBytes2String >> ValueOption.get )
            if ws <> TextValueType.ISV_Missing &&
                ws <> TextValueType.ISV_Irrelevant &&
                ws <> TextValueType.ISV_NotUnderstood &&
                ws <> TextValueType.ISV_Reject &&
                ( String.Equals( ws.GetValue, "Discovery", StringComparison.Ordinal ) |> not ) &&
                ( String.Equals( ws.GetValue, "Normal", StringComparison.Ordinal ) |> not ) then
                    raise <| new System.NullReferenceException();
            else
                {
                    recTKV with
                        SessionType = ws;
                }
        else {
            recTKV with
                UnknownKeys = Array.append recTKV.UnknownKeys [| keyName |]
        }

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Recognize received text key data and create a TextKeyValues record.
    /// </summary>
    /// <param name="isTargetVal">
    ///   Received text key data is received from the target or not.
    ///   If isTargetVal is true, received MaxRecvDataSegmentLength value is set to MaxRecvDataSegmentLength_T.
    /// </param>
    /// <param name="v">
    ///   Received text key data. It is an array of bytes arrays that forms key-value pair strings.
    /// </param>
    /// <returns>
    ///   Created TextKeyValues record that holds recoved text-key values.
    /// </returns>
    static member RecognizeTextKeyData ( isTargetVal : bool ) ( v : byte[][] ) : TextKeyValues voption =
        try
            match IscsiTextEncode.TextKeyData2KeyValues v with
            | ValueNone -> ValueNone
            | ValueSome( v2 ) ->
                v2
                |> Array.fold ( IscsiTextEncode.UpdateTextKeyValuesRecord isTargetVal ) TextKeyValues.defaultTextKeyValues
                |> ValueSome
        with
        | _ -> ValueNone

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Create bytes array that holds text key values.
    ///   This bytes array used to send initiator or target on Login request/response PDUs or text request/response PDUs.
    /// </summary>
    /// <param name="v">
    ///   TextKeyValues record that holds the key values.
    /// </param>
    /// <param name="s">
    ///   Negotiation status. Returned bytes array is generated based on this status.
    /// </param>
    /// <returns>
    ///   Create bytes array that holds text key values.
    /// </returns>
    static member CreateTextKeyValueString ( v : TextKeyValues ) ( s : TextKeyValuesStatus ) =
        let tvtToByte tvt ( keyName : string ) f =
            let valB = 
                match tvt with
                | TextValueType.ISV_Missing ->
                    Array.empty
                | TextValueType.ISV_NotUnderstood ->
                    IscsiTextEncode.byteData_NotUnderstood
                | TextValueType.ISV_Irrelevant ->
                    IscsiTextEncode.byteData_Irrelevant
                | TextValueType.ISV_Reject ->
                    IscsiTextEncode.byteData_Reject
                | Value( x ) ->
                    f x
            if valB.Length > 0 then
                [|
                    yield! ( Encoding.UTF8.GetBytes keyName )
                    yield '='B
                    yield! valB
                    yield '\u0000'B
                |]
            else
                Array.empty

        [|
            if s.NegoStat_AuthMethod &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.AuthMethod "AuthMethod" ( fun x -> 
                    let w =
                        x |>
                        Array.collect (
                            AuthMethodCandidateValue.toStringName
                            >> sprintf "%s,"
                            >> Encoding.GetEncoding( "utf-8" ).GetBytes
                        )
                    w.[ 0 .. w.Length - 2 ]
                )
            if s.NegoStat_CHAP_A &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.CHAP_A "CHAP_A" IscsiTextEncode.uint16ToListOfValuesBytes

            if s.NegoStat_CHAP_I &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.CHAP_I "CHAP_I" IscsiTextEncode.uint16toNumericalValueBytes

            if s.NegoStat_CHAP_C &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.CHAP_C "CHAP_C" IscsiTextEncode.Binary2Base64ConstantBytes

            if s.NegoStat_CHAP_N &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.CHAP_N "CHAP_N" IscsiTextEncode.String2TextValueBytes

            if s.NegoStat_CHAP_R &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.CHAP_R "CHAP_R" IscsiTextEncode.Binary2Base64ConstantBytes

            if s.NegoStat_HeaderDigest &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.HeaderDigest "HeaderDigest" ( fun x -> 
                    let w =
                        Array.collect ( fun y ->
                            if y = DigestType.DST_None then
                                [| 'N'B; 'o'B; 'n'B; 'e'B; ','B; |]
                            elif y = DigestType.DST_CRC32C then
                                [| 'C'B; 'R'B; 'C'B; '3'B; '2'B; 'C'B; ','B; |]
                            else
                                assert( false );
                                Array.empty
                        ) x
                    if w.Length < 2 then
                        [| 'N'B; 'o'B; 'n'B; 'e'B; ','B; |]
                    else
                        w.[ 0 .. w.Length - 2 ]
                )

            if s.NegoStat_DataDigest &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.DataDigest "DataDigest" ( fun x -> 
                    let w =
                        Array.collect ( fun y ->
                            if y = DigestType.DST_None then
                                [| 'N'B; 'o'B; 'n'B; 'e'B; ','B; |]
                            elif y = DigestType.DST_CRC32C then
                                [| 'C'B; 'R'B; 'C'B; '3'B; '2'B; 'C'B; ','B; |]
                            else
                                assert( false );
                                Array.empty
                        ) x
                    if w.Length < 2 then
                        [| 'N'B; 'o'B; 'n'B; 'e'B; ','B; |]
                    else
                        w.[ 0 .. w.Length - 2 ]
                )

            if s.NegoStat_MaxConnections &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.MaxConnections "MaxConnections" IscsiTextEncode.uint16toNumericalValueBytes

            if s.NegoStat_SendTargets &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.SendTargets "SendTargets" IscsiTextEncode.String2TextValueBytes

            if s.NegoStat_TargetName &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.TargetName "TargetName" IscsiTextEncode.String2TextValueBytes

            if s.NegoStat_InitiatorName &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.InitiatorName "InitiatorName" IscsiTextEncode.String2TextValueBytes

            if s.NegoStat_TargetAlias &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.TargetAlias "TargetAlias" IscsiTextEncode.String2TextValueBytes

            if s.NegoStat_InitiatorAlias &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.InitiatorAlias "InitiatorAlias" IscsiTextEncode.String2TextValueBytes

            if s.NegoStat_TargetAddress &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.TargetAddress "TargetAddress" IscsiTextEncode.String2TextValueBytes

            if s.NegoStat_TargetPortalGroupTag &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.TargetPortalGroupTag "TargetPortalGroupTag" IscsiTextEncode.uint16toNumericalValueBytes

            if s.NegoStat_InitialR2T &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.InitialR2T "InitialR2T" IscsiTextEncode.Bool2BooleanValueBytes

            if s.NegoStat_ImmediateData &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.ImmediateData "ImmediateData" IscsiTextEncode.Bool2BooleanValueBytes

            if s.NegoStat_MaxRecvDataSegmentLength_T &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.MaxRecvDataSegmentLength_T "MaxRecvDataSegmentLength" IscsiTextEncode.uint32toNumericalValueBytes
            elif s.NegoStat_MaxRecvDataSegmentLength_I &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.MaxRecvDataSegmentLength_I "MaxRecvDataSegmentLength" IscsiTextEncode.uint32toNumericalValueBytes

            if s.NegoStat_MaxBurstLength &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.MaxBurstLength "MaxBurstLength" IscsiTextEncode.uint32toNumericalValueBytes

            if s.NegoStat_FirstBurstLength &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.FirstBurstLength "FirstBurstLength" IscsiTextEncode.uint32toNumericalValueBytes

            if s.NegoStat_DefaultTime2Wait &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.DefaultTime2Wait "DefaultTime2Wait" IscsiTextEncode.uint16toNumericalValueBytes

            if s.NegoStat_DefaultTime2Retain &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.DefaultTime2Retain "DefaultTime2Retain" IscsiTextEncode.uint16toNumericalValueBytes

            if s.NegoStat_MaxOutstandingR2T &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.MaxOutstandingR2T "MaxOutstandingR2T" IscsiTextEncode.uint16toNumericalValueBytes

            if s.NegoStat_DataPDUInOrder &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.DataPDUInOrder "DataPDUInOrder" IscsiTextEncode.Bool2BooleanValueBytes

            if s.NegoStat_DataSequenceInOrder &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.DataSequenceInOrder "DataSequenceInOrder" IscsiTextEncode.Bool2BooleanValueBytes

            if s.NegoStat_ErrorRecoveryLevel &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.ErrorRecoveryLevel "ErrorRecoveryLevel" IscsiTextEncode.bytetoNumericalValueBytes

            if s.NegoStat_SessionType &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                yield! tvtToByte v.SessionType "SessionType" IscsiTextEncode.String2TextValueBytes
        
            if s.NegoStat_UnknownKeys &&& NegoStatusValue.NSG_WaitSend = NegoStatusValue.NSG_WaitSend then
                for i = 0 to v.UnknownKeys.Length - 1 do
                    yield! ( Encoding.UTF8.GetBytes v.UnknownKeys.[i] )
                    yield '='B;
                    yield! IscsiTextEncode.byteData_NotUnderstood
                    yield '\u0000'B;

        |]

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   It applies specified function to all of keys status and calculate the logical product of all results.
    /// </summary>
    /// <param name="k">
    ///   A TextKeyValuesStatus record that holds negotiation status of keys.
    /// </param>
    /// <param name="f">
    ///   The function applied to key status.
    /// </param>
    /// <returns>
    ///   If all results of specified function is true, it returns true.
    /// </returns>
    static member CheckAllKeyStatus ( k : TextKeyValuesStatus ) ( f : NegoStatusValue -> bool ) : bool =
        f k.NegoStat_AuthMethod &&
        f k.NegoStat_CHAP_A &&
        f k.NegoStat_CHAP_I &&
        f k.NegoStat_CHAP_C &&
        f k.NegoStat_CHAP_N &&
        f k.NegoStat_CHAP_R &&
        f k.NegoStat_HeaderDigest &&
        f k.NegoStat_DataDigest &&
        f k.NegoStat_MaxConnections &&
        f k.NegoStat_SendTargets &&
        f k.NegoStat_TargetName &&
        f k.NegoStat_InitiatorName &&
        f k.NegoStat_TargetAlias &&
        f k.NegoStat_InitiatorAlias &&
        f k.NegoStat_TargetAddress &&
        f k.NegoStat_TargetPortalGroupTag &&
        f k.NegoStat_InitialR2T &&
        f k.NegoStat_ImmediateData &&
        f k.NegoStat_MaxRecvDataSegmentLength_I &&
        f k.NegoStat_MaxRecvDataSegmentLength_T &&
        f k.NegoStat_MaxBurstLength &&
        f k.NegoStat_FirstBurstLength &&
        f k.NegoStat_DefaultTime2Wait &&
        f k.NegoStat_DefaultTime2Retain &&
        f k.NegoStat_MaxOutstandingR2T &&
        f k.NegoStat_DataPDUInOrder &&
        f k.NegoStat_DataSequenceInOrder &&
        f k.NegoStat_ErrorRecoveryLevel &&
        f k.NegoStat_SessionType &&
        f k.NegoStat_UnknownKeys

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Negotiation of all of the key is finished or not.
    /// </summary>
    /// <param name="k">
    ///   Status of negotiation.
    /// </param>
    /// <returns>
    ///   If all keies status is nedotiated, it returns true.
    /// </returns>
    static member IsAllKeyNegotiated ( k : TextKeyValuesStatus ) : bool =
        IscsiTextEncode.CheckAllKeyStatus k ( (=) NegoStatusValue.NSV_Negotiated )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Get common element in specified arrays. If results has two or more elements, it returns first one element.
    /// </summary>
    /// <param name="a1">The array 1.</param>
    /// <param name="a2">The array 1.</param>
    /// <returns>
    ///   Common element of specified arrays.
    ///   If specified two arrays hos two or more common elements, it returns first one element.
    ///   If has no common elements, it return empty array.
    /// </returns>
    static member private arrayAnd ( a1 : 'a[] ) ( a2 : 'a[] ) =
        Array.filter ( fun x -> Array.exists ( (=) x ) a2 ) a1
        |> ( fun x -> if x.Length = 0 then x else x.[0..0] )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Marge a initiator value and a target value by specified result function.
    /// </summary>
    /// <param name="standpoint">
    ///   A value that indicates from which position the processing will be performed.
    /// </param>
    /// <param name="ikv1">
    ///   The initiator side value.
    /// </param>
    /// <param name="tkv2">
    ///   The target side value.
    /// </param>
    /// <param name="s">
    ///   Negotiation status.
    /// </param>
    /// <param name="f">
    ///   Result function.
    /// </param>
    /// <returns>
    ///   A pair of marged value and next status of negotiation.
    /// </returns>
    static member private margeTextValueType ( standpoint : Standpoint ) ( ikv1 : TextValueType<'a> ) ( tkv2 : TextValueType<'a> ) ( s : NegoStatusValue ) ( f : 'a -> 'a -> 'a ) : ( TextValueType<'a> * NegoStatusValue ) =
        if ikv1 = ISV_Missing && tkv2 = ISV_Missing then
            // If both value is "missing", result value is "missing" and status is not changed.
            ( ISV_Missing, s )
        elif ikv1 = ISV_Missing then
            match standpoint with
            | Standpoint.Target ->
                // If initiator value is "missing" and target is not, 
                // no change in status because no corresponding value was received from the initiator.
                ( tkv2, s )
            | Standpoint.Initiator ->
                // If initiator value is "missing" and target is not, 
                // the corresponding value was received from the target, so the WaitReceive state is cleared.
                ( tkv2, s &&& ~~~ NegoStatusValue.NSG_WaitReceive )
        elif tkv2 = ISV_Missing then
            match standpoint with
            | Standpoint.Target ->
                // If target value is "missing" and initiator is not,
                // the corresponding value was received from the initiator, so the WaitReceive state is cleared.
                ( ikv1, s &&& ~~~ NegoStatusValue.NSG_WaitReceive )
            | Standpoint.Initiator ->
                // If target value is "missing" and initiator is not,
                // no change in status because no corresponding value was received from the target.
                ( ikv1, s )
        elif ikv1 = ISV_NotUnderstood || tkv2 = ISV_NotUnderstood then
            // If one side value is "NotUnderstood", result value is "NotUnderstood" and status is cleared
            ( ISV_NotUnderstood, s &&& ~~~ NegoStatusValue.NSG_WaitReceive )
        elif ikv1 = ISV_Reject || tkv2 = ISV_Reject then
            // If one side value is "Reject", result value is "Reject" and status is cleared
            ( ISV_Reject, s &&& ~~~ NegoStatusValue.NSG_WaitReceive  )
        elif ikv1 = ISV_Irrelevant || tkv2 = ISV_Irrelevant then
            // If one side value is "Irrelevant", result value is "Irrelevant" and status is cleared
            ( ISV_Irrelevant, s &&& ~~~ NegoStatusValue.NSG_WaitReceive )
        else
            // both target and initiator send values, marge all values and status is cleared
            ( TextValueType.Value( f ikv1.GetValue tkv2.GetValue ), s &&& ~~~ NegoStatusValue.NSG_WaitReceive )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Marge text-key value received from initiator and target value.
    /// </summary>
    /// <param name="standpoint">
    ///   A value that indicates from which position the processing will be performed.
    /// </param>
    /// <param name="ikv">
    ///   TextKeyValues record that holds the initiator side values.
    /// </param>
    /// <param name="tkv">
    ///   TextKeyValues record that holds the target side values.
    /// </param>
    /// <param name="stat">
    ///   Negotiation status.
    /// </param>
    /// <returns>
    ///   A pair of marged key-value pairs and next status of negotiation.
    /// </returns>
    static member margeTextKeyValue ( standpoint : Standpoint ) ( ikv : TextKeyValues ) ( tkv : TextKeyValues ) ( stat : TextKeyValuesStatus ) : TextKeyValues * TextKeyValuesStatus =

        let next_AuthMethod, next_AuthMethodStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.AuthMethod tkv.AuthMethod stat.NegoStat_AuthMethod IscsiTextEncode.arrayAnd
        let next_CHAP_A, next_CHAP_AStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.CHAP_A tkv.CHAP_A stat.NegoStat_CHAP_A IscsiTextEncode.arrayAnd
        let next_CHAP_I, next_CHAP_IStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.CHAP_I tkv.CHAP_I stat.NegoStat_CHAP_I ( fun i t -> assert( false ); i )    // function is dummy
        let next_CHAP_C, next_CHAP_CStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.CHAP_C tkv.CHAP_C stat.NegoStat_CHAP_C ( fun i t -> assert( false ); i )    // function is dummy
        let next_CHAP_N, next_CHAP_NStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.CHAP_N tkv.CHAP_N stat.NegoStat_CHAP_N ( fun i t -> assert( false ); i )    // function is dummy
        let next_CHAP_R, next_CHAP_RStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.CHAP_R tkv.CHAP_R stat.NegoStat_CHAP_R ( fun i t -> assert( false ); i )    // function is dummy
        let next_HeaderDigest, next_HeaderDigestStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.HeaderDigest tkv.HeaderDigest stat.NegoStat_HeaderDigest IscsiTextEncode.arrayAnd
        let next_DataDigest, next_DataDigestStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.DataDigest tkv.DataDigest stat.NegoStat_DataDigest IscsiTextEncode.arrayAnd
        let next_MaxConnections, next_MaxConnectionsStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.MaxConnections tkv.MaxConnections stat.NegoStat_MaxConnections min
        let next_SendTargets, next_SendTargetsStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.SendTargets tkv.SendTargets stat.NegoStat_SendTargets ( fun i t -> i ) // always initiator value is used
        let next_TargetName, next_TargetNameStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.TargetName tkv.TargetName stat.NegoStat_TargetName ( fun i t -> i ) // always initiator value is used
        let next_InitiatorName, next_InitiatorNameStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.InitiatorName tkv.InitiatorName stat.NegoStat_InitiatorName ( fun i t -> i ) // always initiator value is used
        let next_TargetAlias, next_TargetAliasStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.TargetAlias tkv.TargetAlias stat.NegoStat_TargetAlias ( fun i t -> t ) // always target value is used
        let next_InitiatorAlias, next_InitiatorAliasStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.InitiatorAlias tkv.InitiatorAlias stat.NegoStat_InitiatorAlias ( fun i t -> i ) // always initiator value is used
        let next_TargetAddress, next_TargetAddressStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.TargetAddress tkv.TargetAddress stat.NegoStat_TargetAddress ( fun i t -> t ) // always target value is used
        let next_TargetPortalGroupTag, next_TargetPortalGroupTagStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.TargetPortalGroupTag tkv.TargetPortalGroupTag stat.NegoStat_TargetPortalGroupTag ( fun i t -> t ) // always target value is used
        let next_InitialR2T, next_InitialR2TStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.InitialR2T tkv.InitialR2T stat.NegoStat_InitialR2T (||)
        let next_ImmediateData, next_ImmediateDataStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.ImmediateData tkv.ImmediateData stat.NegoStat_ImmediateData (&&)
        let next_MaxRecvDataSegmentLength_I, next_MaxRecvDataSegmentLength_IStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.MaxRecvDataSegmentLength_I tkv.MaxRecvDataSegmentLength_I stat.NegoStat_MaxRecvDataSegmentLength_I ( fun i t -> i ) // always initiator value is used
        let next_MaxRecvDataSegmentLength_T, next_MaxRecvDataSegmentLength_TStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.MaxRecvDataSegmentLength_T tkv.MaxRecvDataSegmentLength_T stat.NegoStat_MaxRecvDataSegmentLength_T ( fun i t -> t ) // always target value is used
        let next_MaxBurstLength, next_MaxBurstLengthStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.MaxBurstLength tkv.MaxBurstLength stat.NegoStat_MaxBurstLength min
        let next_FirstBurstLength, next_FirstBurstLengthStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.FirstBurstLength tkv.FirstBurstLength stat.NegoStat_FirstBurstLength min
        let next_DefaultTime2Wait, next_DefaultTime2WaitStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.DefaultTime2Wait tkv.DefaultTime2Wait stat.NegoStat_DefaultTime2Wait min
        let next_DefaultTime2Retain, next_DefaultTime2RetainStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.DefaultTime2Retain tkv.DefaultTime2Retain stat.NegoStat_DefaultTime2Retain min
        let next_MaxOutstandingR2T, next_MaxOutstandingR2TStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.MaxOutstandingR2T tkv.MaxOutstandingR2T stat.NegoStat_MaxOutstandingR2T min
        let next_DataPDUInOrder, next_DataPDUInOrderStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.DataPDUInOrder tkv.DataPDUInOrder stat.NegoStat_DataPDUInOrder (||)
        let next_DataSequenceInOrder, next_DataSequenceInOrderStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.DataSequenceInOrder tkv.DataSequenceInOrder stat.NegoStat_DataSequenceInOrder (||)
        let next_ErrorRecoveryLevel, next_ErrorRecoveryLevelStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.ErrorRecoveryLevel tkv.ErrorRecoveryLevel stat.NegoStat_ErrorRecoveryLevel min
        let next_SessionType, next_SessionTypeStat =
            IscsiTextEncode.margeTextValueType standpoint ikv.SessionType tkv.SessionType stat.NegoStat_SessionType ( fun i t -> i ) // always initiator value is used
        let next_UnknownKeys, next_UnknownKeysStat =
            match standpoint with
            | Standpoint.Target ->
                if tkv.UnknownKeys.Length = 0 || ( tkv.UnknownKeys.Length = 1 && tkv.UnknownKeys.[0] = "" ) then
                    // If key names should be send is not exist, negotiation status is "Negotiated"
                    Array.Empty(), NegoStatusValue.NSV_Negotiated
                else
                    tkv.UnknownKeys, NegoStatusValue.NSG_WaitSend
            | Standpoint.Initiator ->
                if ikv.UnknownKeys.Length = 0 || ( ikv.UnknownKeys.Length = 1 && ikv.UnknownKeys.[0] = "" ) then
                    // If key names should be send is not exist, negotiation status is "Negotiated"
                    Array.Empty(), NegoStatusValue.NSV_Negotiated
                else
                    ikv.UnknownKeys, NegoStatusValue.NSG_WaitSend
        (
            {
                AuthMethod = next_AuthMethod;
                CHAP_A = next_CHAP_A;
                CHAP_I = next_CHAP_I;
                CHAP_C = next_CHAP_C;
                CHAP_N = next_CHAP_N;
                CHAP_R = next_CHAP_R;
                HeaderDigest = next_HeaderDigest;
                DataDigest = next_DataDigest;
                MaxConnections = next_MaxConnections;
                SendTargets = next_SendTargets;
                TargetName = next_TargetName;
                InitiatorName = next_InitiatorName;
                TargetAlias = next_TargetAlias;
                InitiatorAlias = next_InitiatorAlias;
                TargetAddress = next_TargetAddress;
                TargetPortalGroupTag = next_TargetPortalGroupTag;
                InitialR2T = next_InitialR2T;
                ImmediateData = next_ImmediateData;
                MaxRecvDataSegmentLength_I = next_MaxRecvDataSegmentLength_I;
                MaxRecvDataSegmentLength_T = next_MaxRecvDataSegmentLength_T;
                MaxBurstLength = next_MaxBurstLength;
                FirstBurstLength = next_FirstBurstLength;
                DefaultTime2Wait = next_DefaultTime2Wait;
                DefaultTime2Retain = next_DefaultTime2Retain;
                MaxOutstandingR2T = next_MaxOutstandingR2T;
                DataPDUInOrder = next_DataPDUInOrder;
                DataSequenceInOrder = next_DataSequenceInOrder;
                ErrorRecoveryLevel = next_ErrorRecoveryLevel;
                SessionType = next_SessionType;
                UnknownKeys = next_UnknownKeys;
            },
            {
                NegoStat_AuthMethod = next_AuthMethodStat;
                NegoStat_CHAP_A = next_CHAP_AStat;
                NegoStat_CHAP_I = next_CHAP_IStat;
                NegoStat_CHAP_C = next_CHAP_CStat;
                NegoStat_CHAP_N = next_CHAP_NStat;
                NegoStat_CHAP_R = next_CHAP_RStat;
                NegoStat_HeaderDigest = next_HeaderDigestStat;
                NegoStat_DataDigest = next_DataDigestStat;
                NegoStat_MaxConnections = next_MaxConnectionsStat;
                NegoStat_SendTargets = next_SendTargetsStat;
                NegoStat_TargetName = next_TargetNameStat;
                NegoStat_InitiatorName = next_InitiatorNameStat;
                NegoStat_TargetAlias = next_TargetAliasStat;
                NegoStat_InitiatorAlias = next_InitiatorAliasStat;
                NegoStat_TargetAddress = next_TargetAddressStat;
                NegoStat_TargetPortalGroupTag = next_TargetPortalGroupTagStat;
                NegoStat_InitialR2T = next_InitialR2TStat;
                NegoStat_ImmediateData = next_ImmediateDataStat;
                NegoStat_MaxRecvDataSegmentLength_I = next_MaxRecvDataSegmentLength_IStat;
                NegoStat_MaxRecvDataSegmentLength_T = next_MaxRecvDataSegmentLength_TStat;
                NegoStat_MaxBurstLength = next_MaxBurstLengthStat;
                NegoStat_FirstBurstLength = next_FirstBurstLengthStat;
                NegoStat_DefaultTime2Wait = next_DefaultTime2WaitStat;
                NegoStat_DefaultTime2Retain = next_DefaultTime2RetainStat;
                NegoStat_MaxOutstandingR2T = next_MaxOutstandingR2TStat;
                NegoStat_DataPDUInOrder = next_DataPDUInOrderStat;
                NegoStat_DataSequenceInOrder = next_DataSequenceInOrderStat;
                NegoStat_ErrorRecoveryLevel = next_ErrorRecoveryLevelStat;
                NegoStat_SessionType = next_SessionTypeStat;
                NegoStat_UnknownKeys = next_UnknownKeysStat;
            }
        )

    // ----------------------------------------------------------------------------
    /// <summary>
    ///   Clear wait send status.
    /// </summary>
    /// <param name="s">
    ///   Negotiation status.
    /// </param>
    /// <returns>
    ///   Negotiation status that cleared the wait send status.
    /// </returns>
    static member ClearSendWaitStatus ( s : TextKeyValuesStatus ) : TextKeyValuesStatus =
        {
            NegoStat_AuthMethod = s.NegoStat_AuthMethod &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_CHAP_A = s.NegoStat_CHAP_A &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_CHAP_I = s.NegoStat_CHAP_I &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_CHAP_C = s.NegoStat_CHAP_C &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_CHAP_N = s.NegoStat_CHAP_N &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_CHAP_R = s.NegoStat_CHAP_R &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_HeaderDigest = s.NegoStat_HeaderDigest &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_DataDigest = s.NegoStat_DataDigest &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_MaxConnections = s.NegoStat_MaxConnections &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_SendTargets = s.NegoStat_SendTargets &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_TargetName = s.NegoStat_TargetName &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_InitiatorName = s.NegoStat_InitiatorName &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_TargetAlias = s.NegoStat_TargetAlias &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_InitiatorAlias = s.NegoStat_InitiatorAlias &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_TargetAddress = s.NegoStat_TargetAddress &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_TargetPortalGroupTag = s.NegoStat_TargetPortalGroupTag &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_InitialR2T = s.NegoStat_InitialR2T &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_ImmediateData = s.NegoStat_ImmediateData &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_MaxRecvDataSegmentLength_I = s.NegoStat_MaxRecvDataSegmentLength_I &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_MaxRecvDataSegmentLength_T = s.NegoStat_MaxRecvDataSegmentLength_T &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_MaxBurstLength = s.NegoStat_MaxBurstLength &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_FirstBurstLength = s.NegoStat_FirstBurstLength &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_DefaultTime2Wait = s.NegoStat_DefaultTime2Wait &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_DefaultTime2Retain = s.NegoStat_DefaultTime2Retain &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_MaxOutstandingR2T = s.NegoStat_MaxOutstandingR2T &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_DataPDUInOrder = s.NegoStat_DataPDUInOrder &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_DataSequenceInOrder = s.NegoStat_DataSequenceInOrder &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_ErrorRecoveryLevel = s.NegoStat_ErrorRecoveryLevel &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_SessionType = s.NegoStat_SessionType &&& ~~~ NegoStatusValue.NSG_WaitSend;
            NegoStat_UnknownKeys = s.NegoStat_UnknownKeys &&& ~~~ NegoStatusValue.NSG_WaitSend;
        }
