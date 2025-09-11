//=============================================================================
// Haruka Software Storage.
// Exceptions.fs : Define Exception classes using in Haruka.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Text
open Haruka.Constants

//=============================================================================
// Type definition

/// <summary>
/// It defines data type of argument that is passed to SendErrorStatusTask class
/// constructor as information sense data descriptor. 
/// </summary>
/// <see>SPC-3 4.5.2.2</see>
type informationSenseDataDesc = {
    /// Device-type or command specific.
    Information : byte[]
}

/// <summary>
/// It defines data type of argument that is passed to SendErrorStatusTask class
/// constructor as command-specific information sense data descriptor. 
/// </summary>
/// <see>SPC-3 4.5.2.3</see>
type commandSpecificSenseDataDesc = {
    /// Information that depends on the command on which the exception condition occurred.
    CommandSpecific : byte[]
}

/// <summary>
///  Field pointer sense key specific data type used in senseKeySpecificSenseDataDesc type.
/// </summary>
/// <see>SPC-3 4.5.2.4.2</see>
type fieldPointerSenseKeySpecificData = {
    /// A command data bit
    CommandData : bool;
    /// bit pointer valid bit
    BPV : bool;
    /// Bit Pointer field
    BitPointer : byte;
    /// field pointer field
    FieldPointer : uint16;
}

/// <summary>
///  Actual retry count sense key specific data type used in senseKeySpecificSenseDataDesc type.
/// </summary>
/// <see>SPC-3 4.5.2.4.3</see>
type actualRetryCountSenseKeySpecificData = {
    /// Actual retry count field.
    ActualRetryCount : uint16;
}

/// <summary>
///  Progress indication sense key specific data type used in senseKeySpecificSenseDataDesc type.
/// </summary>
/// <see>SPC-3 4.5.2.4.4</see>
type progressIndicationSenseKeySpecificData = {
    /// Progress indication field.
    ProgressIndication : uint16;
}

/// <summary>
///  Segment pointer sense key specific data type used in senseKeySpecificSenseDataDesc type.
/// </summary>
/// <see>SPC-3 4.5.2.4.5</see>
type segmentPointerSenseKeySpecificData = {
    /// Ssegment descriptor bit
    SD : bool;
    /// bit pointer valid bit
    BPV : bool;
    /// Bit Pointer field
    BitPointer : byte;
    /// field pointer field
    FieldPointer : uint16;
}

/// <summary>
/// It defines data type of argument that is passed to SenseData class
/// constructor as Sense key specific sense data descriptor. 
/// </summary>
/// <see>SPC-3 4.5.2.4</see>
/// <remarks>
/// To set a value for one of the fields.
/// </remarks>
type senseKeySpecificSenseDataDesc = {
    /// Field pointer sense key specific data ( Sense key is ILLEGAL REQUEST )
    FieldPointer : fieldPointerSenseKeySpecificData option;
    /// Actual retry count sense key specific data ( Sense key is HARDWARE ERROR, MEDIUM ERROR, RECOVERED ERROR )
    ActualRetryCount : actualRetryCountSenseKeySpecificData option;
    /// Progress indication sense key specific data ( Sense key is NO SENSE, NOT READY )
    ProgressIndication : progressIndicationSenseKeySpecificData option;
    /// Segment pointer sense key specific data ( Sense key is COPY ABORTED )
    SegmentPointer : segmentPointerSenseKeySpecificData option;
}

/// <summary>
/// It defines data type of argument that is passed to SenseData class
/// constructor as field replaceable unit sense data descriptor. 
/// </summary>
/// <see>SPC-3 4.5.2.5</see>
type fieldReplaceableUnitSenseDataDesc = {
    /// field replaceable unit code field
    FieldReplaceableUnitCode: byte;
}

/// <summary>
/// Descriptor type values that used in vendor specific sense data.
/// </summary>
type VendorSpecificSenseDataDescType =
    | TEXT_MESSAGE = 0x80uy

/// <summary>
/// It defines data type of argument that is passed to SenseData class
/// constructor as vendor specific sense data descriptors. 
/// </summary>
/// <see>SPC-3 4.5.2.6</see>
type vendorSpecificSenseDataDesc = {
    /// DESCRIPTOR TYPE
    DescriptorType : VendorSpecificSenseDataDescType;
    /// Vendor specific
    VendorSpecific: byte[];
}

/// <summary>
/// It defines data type of argument that is passed to SenseData class
/// constructor as block command sense data descriptors.
/// </summary>
/// <see>SBC-2 4.13.2</see>
type blockCommandSenseDataDesc = {
    /// INCORRECT LENGTH INDICATION
    ILI : bool;
}

/// <summary>
/// It holds sense data.
/// If some data is missing, set None in the corresponding field.
/// </summary>
/// <param name="m_IsCurrent">
/// Current error or not. If this field set to true, this erro is treated as current error
/// </param>
/// <param name="m_SenseKey">
///   A sense key value of established ACA.
/// </param>
/// <param name="m_ASC">
/// Additional sense code and Additional sense code qualifier
/// </param>
/// <param name="m_Information">
/// Information sense data
/// </param>
/// <param name="m_CommandSpecific">
/// Command-specific information sense data
/// </param>
/// <param name="m_SenseKeySpecific">
/// Sense key specific sense data
/// </param>
/// <param name="m_FieldReplaceableUnit">
/// Field replaceable unit sense data
/// </param>
/// <param name="m_VendorSpecific">
/// Vendor specific sense data
/// </param>
/// <param name="m_BlockCommand">
/// Block command sense data
/// </param>
type SenseData
    (
        m_IsCurrent : bool,
        m_SenseKey : SenseKeyCd,
        m_ASC : ASCCd,
        m_Information : informationSenseDataDesc option,
        m_CommandSpecific : commandSpecificSenseDataDesc option,
        m_SenseKeySpecific : senseKeySpecificSenseDataDesc option,
        m_FieldReplaceableUnit : fieldReplaceableUnitSenseDataDesc option,
        m_VendorSpecific : vendorSpecificSenseDataDesc option,
        m_BlockCommand : blockCommandSenseDataDesc option
    ) =

        /// Arguments of m_SenseKeySpecific and m_SenseKey is valid or not.
        /// If above args has an error, Sense key specific sense data is omitted.
        let m_SenseKeySpecificArgsValid : bool =
            if m_SenseKeySpecific.IsNone then
                true
            else
                match m_SenseKey with
                | SenseKeyCd.ILLEGAL_REQUEST ->
                    m_SenseKeySpecific.Value.FieldPointer.IsSome && 
                    m_SenseKeySpecific.Value.ActualRetryCount.IsNone &&
                    m_SenseKeySpecific.Value.ProgressIndication.IsNone &&
                    m_SenseKeySpecific.Value.SegmentPointer.IsNone
                | SenseKeyCd.HARDWARE_ERROR
                | SenseKeyCd.MEDIUM_ERROR
                | SenseKeyCd.RECOVERED_ERROR ->
                    m_SenseKeySpecific.Value.FieldPointer.IsNone && 
                    m_SenseKeySpecific.Value.ActualRetryCount.IsSome &&
                    m_SenseKeySpecific.Value.ProgressIndication.IsNone &&
                    m_SenseKeySpecific.Value.SegmentPointer.IsNone
                | SenseKeyCd.NO_SENSE
                | SenseKeyCd.NOT_READY ->
                    m_SenseKeySpecific.Value.FieldPointer.IsNone && 
                    m_SenseKeySpecific.Value.ActualRetryCount.IsNone &&
                    m_SenseKeySpecific.Value.ProgressIndication.IsSome &&
                    m_SenseKeySpecific.Value.SegmentPointer.IsNone
                | SenseKeyCd.COPY_ABORTED ->
                    m_SenseKeySpecific.Value.FieldPointer.IsNone && 
                    m_SenseKeySpecific.Value.ActualRetryCount.IsNone &&
                    m_SenseKeySpecific.Value.ProgressIndication.IsNone &&
                    m_SenseKeySpecific.Value.SegmentPointer.IsSome
                | _ ->
                    false
        do
            ()
            //assert( m_SenseKeySpecificArgsValid )

        /// <summary>
        ///  SenseData class constractor with Information sense data descriptor.
        /// </summary>
        /// <param name="argIsCurrent">
        ///  Current error or not. If this field set to true, this erro is treated as current error
        /// </param>
        /// <param name="argSenseKey">
        ///  A sense key value of established ACA.
        /// </param>
        /// <param name="argASC">
        ///  Additional sense code and Additional sense code qualifier
        /// </param>
        /// <param name="argInformation">
        ///  Information sense data.
        /// </param>
        /// <param name="argDescType">
        ///   Descriptor type value in vendor specific sense data descriptor.
        /// </param>
        /// <param name="argMessage">
        ///  Message string that is treated as vendor specific field bytes array in vendor specific sense data descriptor.
        /// </param>
        new (
            argIsCurrent : bool,
            argSenseKey : SenseKeyCd,
            argASC : ASCCd,
            argInformation : informationSenseDataDesc,
            ?argDescType : VendorSpecificSenseDataDescType,
            ?argMessage : string
            ) =
                new SenseData(
                    argIsCurrent,
                    argSenseKey,
                    argASC,
                    Some argInformation,
                    None,
                    None,
                    None,
                    SenseData.GetOptionalVendorSpecificDesc( argDescType, argMessage ),
                    None
                )

        /// <summary>
        ///  SenseData class constractor with Command-specific sense data descriptor.
        /// </summary>
        /// <param name="argIsCurrent">
        ///  Current error or not. If this field set to true, this erro is treated as current error
        /// </param>
        /// <param name="argSenseKey">
        ///  A sense key value of established ACA.
        /// </param>
        /// <param name="argASC">
        ///  Additional sense code and Additional sense code qualifier
        /// </param>
        /// <param name="argCommandSpecific">
        /// Command-specific information sense data
        /// </param>
        /// <param name="argDescType">
        ///  Descriptor type value in vendor specific sense data descriptor.
        /// </param>
        /// <param name="argMessage">
        ///  Message string that is treated as vendor specific field bytes array in vendor specific sense data descriptor.
        /// </param>
        new (
            argIsCurrent : bool,
            argSenseKey : SenseKeyCd,
            argASC : ASCCd,
            argCommandSpecific : commandSpecificSenseDataDesc,
            ?argDescType : VendorSpecificSenseDataDescType,
            ?argMessage : string
            ) =
                new SenseData(
                    argIsCurrent,
                    argSenseKey,
                    argASC,
                    None,
                    Some argCommandSpecific,
                    None,
                    None,
                    SenseData.GetOptionalVendorSpecificDesc( argDescType, argMessage ),
                    None
                )

        /// <summary>
        ///  SenseData class constractor with Sense key specific sense data descriptor.
        /// </summary>
        /// <param name="argIsCurrent">
        ///  Current error or not. If this field set to true, this erro is treated as current error
        /// </param>
        /// <param name="argSenseKey">
        ///  A sense key value of established ACA.
        /// </param>
        /// <param name="argASC">
        ///  Additional sense code and Additional sense code qualifier
        /// </param>
        /// <param name="argFieldPointer">
        ///  Field pointer sense key specific data in Sense key specific sense data descriptor.
        /// </param>
        /// <param name="argDescType">
        ///  Descriptor type value in vendor specific sense data descriptor.
        /// </param>
        /// <param name="argMessage">
        ///  Message string that is treated as vendor specific field bytes array in vendor specific sense data descriptor.
        /// </param>
        new (
            argIsCurrent : bool,
            argSenseKey : SenseKeyCd,
            argASC : ASCCd,
            argFieldPointer : fieldPointerSenseKeySpecificData,
            ?argDescType : VendorSpecificSenseDataDescType,
            ?argMessage : string
            ) =
                new SenseData(
                    argIsCurrent,
                    argSenseKey,
                    argASC,
                    None,
                    None,
                    Some {
                        FieldPointer = Some argFieldPointer;
                        ActualRetryCount = None;
                        ProgressIndication = None;
                        SegmentPointer = None;
                    },
                    None,
                    SenseData.GetOptionalVendorSpecificDesc( argDescType, argMessage ),
                    None
                )

        /// <summary>
        ///  SenseData class constractor with Sense key specific sense data descriptor.
        /// </summary>
        /// <param name="argIsCurrent">
        ///  Current error or not. If this field set to true, this erro is treated as current error
        /// </param>
        /// <param name="argSenseKey">
        ///  A sense key value of established ACA.
        /// </param>
        /// <param name="argASC">
        ///  Additional sense code and Additional sense code qualifier
        /// </param>
        /// <param name="argActualRetryCount">
        ///  Actual retry count sense key specific data in Sense key specific sense data descriptor.
        /// </param>
        /// <param name="argDescType">
        ///  Descriptor type value in vendor specific sense data descriptor.
        /// </param>
        /// <param name="argMessage">
        ///  Message string that is treated as vendor specific field bytes array in vendor specific sense data descriptor.
        /// </param>
        new (
            argIsCurrent : bool,
            argSenseKey : SenseKeyCd,
            argASC : ASCCd,
            argActualRetryCount : actualRetryCountSenseKeySpecificData,
            ?argDescType : VendorSpecificSenseDataDescType,
            ?argMessage : string
            ) =
                new SenseData(
                    argIsCurrent,
                    argSenseKey,
                    argASC,
                    None,
                    None,
                    Some {
                        FieldPointer = None;
                        ActualRetryCount = Some argActualRetryCount;
                        ProgressIndication = None;
                        SegmentPointer = None;
                    },
                    None,
                    SenseData.GetOptionalVendorSpecificDesc( argDescType, argMessage ),
                    None
                )

        /// <summary>
        ///  SenseData class constractor with Sense key specific sense data descriptor.
        /// </summary>
        /// <param name="argIsCurrent">
        ///  Current error or not. If this field set to true, this erro is treated as current error
        /// </param>
        /// <param name="argSenseKey">
        ///  A sense key value of established ACA.
        /// </param>
        /// <param name="argASC">
        ///  Additional sense code and Additional sense code qualifier
        /// </param>
        /// <param name="argProgressIndication">
        ///  Progress indication sense key specific data in Sense key specific sense data descriptor.
        /// </param>
        /// <param name="argDescType">
        ///  Descriptor type value in vendor specific sense data descriptor.
        /// </param>
        /// <param name="argMessage">
        ///  Message string that is treated as vendor specific field bytes array in vendor specific sense data descriptor.
        /// </param>
        new (
            argIsCurrent : bool,
            argSenseKey : SenseKeyCd,
            argASC : ASCCd,
            argProgressIndication : progressIndicationSenseKeySpecificData,
            ?argDescType : VendorSpecificSenseDataDescType,
            ?argMessage : string
            ) =
                new SenseData(
                    argIsCurrent,
                    argSenseKey,
                    argASC,
                    None,
                    None,
                    Some {
                        FieldPointer = None;
                        ActualRetryCount = None;
                        ProgressIndication = Some argProgressIndication;
                        SegmentPointer = None;
                    },
                    None,
                    SenseData.GetOptionalVendorSpecificDesc( argDescType, argMessage ),
                    None
                )

        /// <summary>
        ///  SenseData class constractor with Sense key specific sense data descriptor.
        /// </summary>
        /// <param name="argIsCurrent">
        ///  Current error or not. If this field set to true, this erro is treated as current error
        /// </param>
        /// <param name="argSenseKey">
        ///  A sense key value of established ACA.
        /// </param>
        /// <param name="argASC">
        ///  Additional sense code and Additional sense code qualifier
        /// </param>
        /// <param name="argSegmentPointer">
        ///  Segment pointer sense key specific data in Sense key specific sense data descriptor.
        /// </param>
        /// <param name="argDescType">
        ///  Descriptor type value in vendor specific sense data descriptor.
        /// </param>
        /// <param name="argMessage">
        ///  Message string that is treated as vendor specific field bytes array in vendor specific sense data descriptor.
        /// </param>
        new (
            argIsCurrent : bool,
            argSenseKey : SenseKeyCd,
            argASC : ASCCd,
            argSegmentPointer : segmentPointerSenseKeySpecificData,
            ?argDescType : VendorSpecificSenseDataDescType,
            ?argMessage : string
            ) =
                new SenseData(
                    argIsCurrent,
                    argSenseKey,
                    argASC,
                    None,
                    None,
                    Some {
                        FieldPointer = None;
                        ActualRetryCount = None;
                        ProgressIndication = None;
                        SegmentPointer = Some argSegmentPointer;
                    },
                    None,
                    SenseData.GetOptionalVendorSpecificDesc( argDescType, argMessage ),
                    None
                )

        /// <summary>
        ///  SenseData class constractor with Field replaceable unit sense data descriptor.
        /// </summary>
        /// <param name="argIsCurrent">
        ///  Current error or not. If this field set to true, this erro is treated as current error
        /// </param>
        /// <param name="argSenseKey">
        ///  A sense key value of established ACA.
        /// </param>
        /// <param name="argASC">
        ///  Additional sense code and Additional sense code qualifier
        /// </param>
        /// <param name="argFieldReplaceableUnit">
        /// Field replaceable unit sense data
        /// </param>
        /// <param name="argDescType">
        ///  Descriptor type value in vendor specific sense data descriptor.
        /// </param>
        /// <param name="argMessage">
        ///  Message string that is treated as vendor specific field bytes array in vendor specific sense data descriptor.
        /// </param>
        new (
            argIsCurrent : bool,
            argSenseKey : SenseKeyCd,
            argASC : ASCCd,
            argFieldReplaceableUnit : fieldReplaceableUnitSenseDataDesc,
            ?argDescType : VendorSpecificSenseDataDescType,
            ?argMessage : string
            ) =
                new SenseData(
                    argIsCurrent,
                    argSenseKey,
                    argASC,
                    None,
                    None,
                    None,
                    Some argFieldReplaceableUnit,
                    SenseData.GetOptionalVendorSpecificDesc( argDescType, argMessage ),
                    None
                )

        /// <summary>
        ///  SenseData class constractor with Vendor specific sense data descriptor.
        /// </summary>
        /// <param name="argIsCurrent">
        ///  Current error or not. If this field set to true, this erro is treated as current error
        /// </param>
        /// <param name="argSenseKey">
        ///  A sense key value of established ACA.
        /// </param>
        /// <param name="argASC">
        ///  Additional sense code and Additional sense code qualifier
        /// </param>
        /// <param name="argVendorSpecific">
        /// Vendor specific sense data
        /// </param>
        new (
            argIsCurrent : bool,
            argSenseKey : SenseKeyCd,
            argASC : ASCCd,
            argVendorSpecific : vendorSpecificSenseDataDesc
            ) =
                new SenseData( argIsCurrent, argSenseKey, argASC, None, None, None, None, Some argVendorSpecific, None )

        /// <summary>
        ///  SenseData class constractor with Block command sense data descriptor.
        /// </summary>
        /// <param name="argIsCurrent">
        ///  Current error or not. If this field set to true, this erro is treated as current error
        /// </param>
        /// <param name="argSenseKey">
        ///  A sense key value of established ACA.
        /// </param>
        /// <param name="argASC">
        ///  Additional sense code and Additional sense code qualifier
        /// </param>
        /// <param name="argBlockCommand">
        /// Block command sense data
        /// </param>
        /// <param name="argDescType">
        ///  Descriptor type value in vendor specific sense data descriptor.
        /// </param>
        /// <param name="argMessage">
        ///  Message string that is treated as vendor specific field bytes array in vendor specific sense data descriptor.
        /// </param>
        new (
            argIsCurrent : bool,
            argSenseKey : SenseKeyCd,
            argASC : ASCCd,
            argBlockCommand : blockCommandSenseDataDesc,
            ?argDescType : VendorSpecificSenseDataDescType,
            ?argMessage : string
            ) =
                new SenseData(
                    argIsCurrent,
                    argSenseKey,
                    argASC,
                    None,
                    None,
                    None,
                    None,
                    SenseData.GetOptionalVendorSpecificDesc( argDescType, argMessage ),
                    Some argBlockCommand
                )

        /// <summary>
        ///  SenseData class constractor with Block command sense data descriptor.
        /// </summary>
        /// <param name="argIsCurrent">
        ///  Current error or not. If this field set to true, this erro is treated as current error
        /// </param>
        /// <param name="argSenseKey">
        ///  A sense key value of established ACA.
        /// </param>
        /// <param name="argASC">
        ///  Additional sense code and Additional sense code qualifier
        /// </param>
        /// <param name="argMessage">
        ///  Message string that is treated as vendor specific field bytes array in vendor specific sense data descriptor.
        /// </param>
        new (
            argIsCurrent : bool,
            argSenseKey : SenseKeyCd,
            argASC : ASCCd,
            argMessage : string
            ) =
                new SenseData(
                    argIsCurrent,
                    argSenseKey,
                    argASC,
                    None,
                    None,
                    None,
                    None,
                    SenseData.GetOptionalVendorSpecificDesc( Some VendorSpecificSenseDataDescType.TEXT_MESSAGE, Some argMessage ),
                    None
                )

        /// <summary>
        ///  In simplified constractor, create vendor specific sense data descriptor corresponding optional argument value.
        /// </summary>
        /// <param name="argDescType">
        ///  Descriptor type value in vendor specific sense data descriptor.
        /// </param>
        /// <param name="argMessage">
        ///  Message string that is treated as vendor specific field bytes array in vendor specific sense data descriptor.
        /// </param>
        /// <returns>
        ///  If both argument argDescType and argMessage is specified, it returns vendor specific sense data descriptor stracture.
        ///  If not, returns None.
        /// </returns>
        static member private GetOptionalVendorSpecificDesc( argDescType : VendorSpecificSenseDataDescType option, argMessage : string option ) : vendorSpecificSenseDataDesc option =
            if box argMessage <> null && box argDescType <> null then
                Some {
                    DescriptorType = argDescType.Value;
                    VendorSpecific = Encoding.GetEncoding( "utf-8" ).GetBytes( argMessage.Value )
                }
            else
                None

        /// <summary>
        ///  Get sense data byte array for sending response to initiator.
        /// </summary>
        /// <param name="desc">
        ///  If true, get descriptor format data, or not, get fixed format data.
        /// </param>
        /// <returns>
        ///  Bytes array of sense data.
        /// </returns>
        member this.GetSenseData( desc : bool ) : byte[] =

            if desc then
                // Additional sense data
                let asd = [|
                    // Information
                    if m_Information.IsSome then
                        // DESCRIPTOR TYPE
                        yield 0x00uy;
                        // ADDITIONAL LENGTH
                        yield 0x0Auy;
                        // VALID, Reserved
                        yield 0x80uy;
                        // Reserved
                        yield 0x00uy;
                        // INFORMATION
                        if m_Information.Value.Information.Length >= 8 then
                            yield! m_Information.Value.Information.[ 0 .. 7 ]
                        else
                            yield! Array.zeroCreate( 8 - m_Information.Value.Information.Length )
                            yield! m_Information.Value.Information

                    // Command Specific
                    if m_CommandSpecific.IsSome then
                        // DESCRIPTOR TYPE
                        yield 0x01uy;
                        // ADDITIONAL LENGTH
                        yield 0x0Auy;
                        // Reserved
                        yield 0x00uy;
                        // Reserved
                        yield 0x00uy;
                        // INFORMATION
                        if m_CommandSpecific.Value.CommandSpecific.Length >= 8 then
                            yield! m_CommandSpecific.Value.CommandSpecific.[ 0 .. 7 ]
                        else
                            yield! Array.zeroCreate( 8 - m_CommandSpecific.Value.CommandSpecific.Length )
                            yield! m_CommandSpecific.Value.CommandSpecific

                    // Sense key specific
                    if m_SenseKeySpecific.IsSome && m_SenseKeySpecificArgsValid then
                        // DESCRIPTOR TYPE
                        yield 0x02uy;
                        // ADDITIONAL LENGTH
                        yield 0x06uy;
                        // Reserved
                        yield 0x00uy;
                        // Reserved
                        yield 0x00uy;
                        // sense key specific bytes data
                        yield! this.GetSenseKeySpecificSenseDataBytes( m_SenseKeySpecific.Value )
                        // Reserved
                        yield 0x00uy;

                    // Field replaceable unit sense data
                    if m_FieldReplaceableUnit.IsSome then
                        // DESCRIPTOR TYPE
                        yield 0x03uy;
                        // ADDITIONAL LENGTH
                        yield 0x02uy;
                        // Reserved
                        yield 0x00uy;
                        // FIELD REPLACEABLE UNIT CODE
                        yield m_FieldReplaceableUnit.Value.FieldReplaceableUnitCode

                    // Block command sense data
                    if m_BlockCommand.IsSome then
                        // DESCRIPTOR TYPE
                        yield 0x05uy;
                        // ADDITIONAL LENGTH
                        yield 0x02uy;
                        // Reserved
                        yield 0x00uy;
                        // ILI
                        yield ( Functions.SetBitflag m_BlockCommand.Value.ILI 0x20uy )

                    let wlen =
                        ( if m_Information.IsSome then 12 else 0  ) +
                        ( if m_CommandSpecific.IsSome then 12 else 0 ) +
                        ( if m_SenseKeySpecific.IsSome && m_SenseKeySpecificArgsValid then 8 else 0 ) +
                        ( if m_FieldReplaceableUnit.IsSome then 4 else 0 ) +
                        ( if m_BlockCommand.IsSome then 4 else 0 )

                    // Vendor specific sense data
                    if m_VendorSpecific.IsSome &&
                        ( m_VendorSpecific.Value.DescriptorType |> byte ) >= 0x80uy &&
                        ( m_VendorSpecific.Value.DescriptorType |> byte ) <= 0xFFuy then

                        // DESCRIPTOR TYPE
                        yield m_VendorSpecific.Value.DescriptorType |> byte;
                        // ADDITIONAL LENGTH, Vendor specific
                        if m_VendorSpecific.Value.VendorSpecific.Length <= 242 - wlen then
                            yield m_VendorSpecific.Value.VendorSpecific.Length |> byte;
                            yield! m_VendorSpecific.Value.VendorSpecific;
                        else
                            yield ( 242 - wlen ) |> byte;
                            yield! m_VendorSpecific.Value.VendorSpecific.[ 0 .. 241 - wlen ];
                |]

                assert( asd.Length <= 244 )

                // Descriptor format
                [|
                    // RESPONSE CODE
                    yield if m_IsCurrent then 0x72uy else 0x73uy;
                    //  SENSE KEY
                    yield ( byte m_SenseKey >>> 0 ) &&& 0x0Fuy;
                    // ADDITIONAL SENSE CODE
                    yield ( uint16 m_ASC ) >>> 8 |> byte;
                    //  ADDITIONAL SENSE CODE QUALIFIER
                    yield ( uint16 m_ASC ) &&& 0x00FFus |> byte;
                    //  Reserved
                    yield 0uy;
                    yield 0uy;
                    yield 0uy;
                    // ADDITIONAL SENSE LENGTH
                    yield asd.Length |> byte
                    // SENSE DATA DESCRIPTORS
                    yield! asd
                |]
            else
                // Fixed format
                [|
                    // VALID, RESPONSE CODE
                    yield
                        (
                            if ( m_Information.IsSome && m_Information.Value.Information.Length <= 4 ) then
                                0x80uy
                            else
                                0x00uy
                        ) |||
                        ( if m_IsCurrent then 0x70uy else 0x71uy );
                    // Obsolete
                    yield 0x00uy;
                    // FILEMARK, EOM, ILI, SENSE KEY
                    yield
                        ( if m_BlockCommand.IsSome then Functions.SetBitflag m_BlockCommand.Value.ILI 0x20uy else 0x00uy ) |||
                        ( ( byte m_SenseKey ) &&& 0x0Fuy )
                    // INFORMATION
                    if m_Information.IsSome && m_Information.Value.Information.Length <= 4 then
                        yield! Array.zeroCreate( 4 - m_Information.Value.Information.Length )
                        yield! m_Information.Value.Information
                    else
                        yield! [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; |]
                    // ADDITIONAL SENSE LENGTH
                    if m_VendorSpecific.IsSome then
                        if m_VendorSpecific.Value.VendorSpecific.Length <= 234 then
                            yield ( m_VendorSpecific.Value.VendorSpecific.Length + 10 ) |> byte;
                        else
                            yield 244uy;
                    else
                        yield 0x0Auy;
                    // COMMAND-SPECIFIC INFORMATION
                    if m_CommandSpecific.IsSome && m_CommandSpecific.Value.CommandSpecific.Length <= 4 then
                        yield! Array.zeroCreate( 4 - m_CommandSpecific.Value.CommandSpecific.Length )
                        yield! m_CommandSpecific.Value.CommandSpecific
                    else
                        yield! [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; |]
                    // ADDITIONAL SENSE CODE
                    yield ( uint16 m_ASC ) >>> 8 |> byte;
                    //  ADDITIONAL SENSE CODE QUALIFIER
                    yield ( uint16 m_ASC ) &&& 0x00FFus |> byte;
                    // FIELD REPLACEABLE UNIT CODE
                    if m_FieldReplaceableUnit.IsSome then
                        yield m_FieldReplaceableUnit.Value.FieldReplaceableUnitCode
                    else
                        yield 0x00uy;
                    // SKSV, SENSE KEY SPECIFIC
                    if m_SenseKeySpecific.IsSome && m_SenseKeySpecificArgsValid then
                        // sense key specific bytes data
                        yield! this.GetSenseKeySpecificSenseDataBytes( m_SenseKeySpecific.Value )
                    else
                        yield! [| 0x00uy; 0x00uy; 0x00uy |]
                    // Additional sense bytes
                    if m_VendorSpecific.IsSome then
                        if m_VendorSpecific.Value.VendorSpecific.Length <= 234 then
                            yield! m_VendorSpecific.Value.VendorSpecific;
                        else
                            yield! m_VendorSpecific.Value.VendorSpecific.[ 0 .. 233 ];
                |]

        /// <summary>
        /// Create Sense key specific sense data in descriptore format and fixed format.
        /// </summary>
        /// <param name="argSKS">
        /// Sense key specific sense data descriptor in constractor of SenseData.
        /// </param>
        member private _.GetSenseKeySpecificSenseDataBytes( argSKS ) : byte[] =
            [|
                if argSKS.FieldPointer.IsSome then
                    // Field pointer sense key specific data
                    yield
                        0x80uy |||
                        ( Functions.SetBitflag argSKS.FieldPointer.Value.CommandData 0x40uy ) |||
                        ( Functions.SetBitflag argSKS.FieldPointer.Value.BPV 0x08uy ) |||
                        ( argSKS.FieldPointer.Value.BitPointer &&& 0x07uy );
                    yield ( argSKS.FieldPointer.Value.FieldPointer >>> 8 ) |> byte;
                    yield argSKS.FieldPointer.Value.FieldPointer |> byte;
                elif argSKS.ActualRetryCount.IsSome then
                    // Actual retry count sense key specific data
                    yield 0x80uy;
                    yield ( argSKS.ActualRetryCount.Value.ActualRetryCount >>> 8 ) |> byte;
                    yield argSKS.ActualRetryCount.Value.ActualRetryCount |> byte;
                elif argSKS.ProgressIndication.IsSome then
                    // Progress indication sense key specific data
                    yield 0x80uy;
                    yield ( argSKS.ProgressIndication.Value.ProgressIndication >>> 8 ) |> byte;
                    yield argSKS.ProgressIndication.Value.ProgressIndication |> byte;
                elif argSKS.SegmentPointer.IsSome then
                    // Segment pointer sense key specific data
                    yield
                        0x80uy |||
                        ( Functions.SetBitflag argSKS.SegmentPointer.Value.SD 0x20uy ) |||
                        ( Functions.SetBitflag argSKS.SegmentPointer.Value.BPV 0x08uy ) |||
                        ( argSKS.SegmentPointer.Value.BitPointer &&& 0x07uy );
                    yield ( argSKS.SegmentPointer.Value.FieldPointer >>> 8 ) |> byte;
                    yield argSKS.SegmentPointer.Value.FieldPointer |> byte;
                else
                    yield! [| 0x00uy; 0x00uy; 0x00uy |];
                    assert( false )
            |]

        /// Current error or not.
        member _.IsCurrent : bool =
            m_IsCurrent

        /// Get sense key value.
        member _.SenseKey : SenseKeyCd =
            m_SenseKey

        /// Get additional sense code and additional sense code qualifier
        member _.ASC : ASCCd =
            m_ASC

        /// Get Information sense data
        member _.Information : informationSenseDataDesc option =
            m_Information

        /// Get Command-specific information sense data
        member _.CommandSpecific : commandSpecificSenseDataDesc option =
            m_CommandSpecific

        /// Get Field pointer sense key specific data
        member _.FieldPointer : fieldPointerSenseKeySpecificData option =
            if m_SenseKeySpecific.IsSome then m_SenseKeySpecific.Value.FieldPointer else None

        /// Get Actual retry count sense key specific data
        member _.ActualRetryCount : actualRetryCountSenseKeySpecificData option =
            if m_SenseKeySpecific.IsSome then m_SenseKeySpecific.Value.ActualRetryCount else None

        /// Get Progress indication sense key specific data
        member _.ProgressIndication : progressIndicationSenseKeySpecificData option =
            if m_SenseKeySpecific.IsSome then m_SenseKeySpecific.Value.ProgressIndication else None

        /// Get Segment pointer sense key specific data
        member _.SegmentPointer : segmentPointerSenseKeySpecificData option =
            if m_SenseKeySpecific.IsSome then m_SenseKeySpecific.Value.SegmentPointer else None

        /// Get Field replaceable unit sense data
        member _.FieldReplaceableUnit : fieldReplaceableUnitSenseDataDesc option =
            m_FieldReplaceableUnit

        /// Get Vendor specific sense data
        member _.VendorSpecific : vendorSpecificSenseDataDesc option =
            m_VendorSpecific

        /// Get Block command sense data
        member _.BlockCommand : blockCommandSenseDataDesc option =
            m_BlockCommand

        /// Get sense data description string for log output.
        member _.DescString : string =
            ""

//=============================================================================
// Class declaration

/// <summary>
///   Request for in command recovery.
///   Target send recovery R2T PDU to the initiator.
///   *ErrorRecoverLevel=1
/// </summary>
/// <param name="argMsg">
///   A message string that descripts cource of exception.
/// </param>
type InCommandRecoveryException( argMsg : string ) =
    inherit Exception( argMsg )


/// <summary>
///   Request for in connection recovery.
///   Target send NOP-In PDU to the initiator.
///   *ErrorRecoverLevel=1
/// </summary>
/// <param name="argMsg">
///   A message string that descripts cource of exception.
/// </param>
type InConnectionRecoveryException( argMsg : string ) =
    inherit Exception( argMsg )

/// <summary>
///   Exceptions that indicate that a communication error has occurred.
/// </summary>
/// <param name="argMsg">
///   A message string that descripts cource of exception.
/// </param>
/// <param name="m_TSIH">
///   TSIH identifying the session which connection that is source of exception belongings to.
/// </param>
/// <param name="m_CID">
///   CID identifying the connection that is source of the exception.
///   Connection represented by this CID should be dropped.
/// </param>
type ConnectionErrorException( argMsg : string, m_TSIH : TSIH_T, m_CID : CID_T ) =
    inherit Exception( argMsg )

    /// Get CID property
    member _.CID : CID_T =
        m_CID

    /// Get TSIH property
    member _.TSIH : TSIH_T =
        m_TSIH


/// <summary>
///   Request session recovery.
///   target drop all of connections in the session.
///   *ErrorRecoverLevel=0
/// </summary>
/// <param name="argMsg">
///   A message string that descripts cource of exception.
/// </param>
/// <param name="m_TSIH">
///   TSIH identifying the session that is source of exception.
///   Session represented by the TSIH should be dropped.
/// </param>
type SessionRecoveryException( argMsg : string, m_TSIH : TSIH_T ) =
    inherit Exception( argMsg )

    /// Get TSIH property
    member _.TSIH : TSIH_T =
        m_TSIH

/// <summary>
///   Request send Reject to initiator.
/// </summary>
/// <param name="argMsg">
///   A message string that descripts cource of exception.
/// </param>
/// <param name="m_Reason">
///   The value that is set to Reason field in Reject PDU.
/// </param>
/// <param name="m_HeaderData">
///   The value that is set to HeaderData field in Reject PDU.
/// </param>
type RejectPDUException( argMsg : string, m_Reason : RejectReasonCd, m_HeaderData : byte[] ) =
    inherit Exception( argMsg )
    
    /// Get Reason code property
    member _.Reason : RejectReasonCd =
        m_Reason

    /// Get header data property
    member _.Header : byte[] =
        m_HeaderData

/// <summary>
///   Discard received PDU.
/// </summary>
/// <param name="argMsg">
///   A message string that descripts cource of exception.
/// </param>
type DiscardPDUException( argMsg : string ) =
    inherit Exception( argMsg )


/// <summary>
///   Internal assertion exception
/// </summary>
/// <param name="argMsg">
///   A message string that descripts cource of exception.
/// </param>
type InternalAssertionException( argMsg : string ) =
    inherit Exception( argMsg )

/// <summary>
///   Request for in command recovery.
///   Target send recovery R2T PDU to the initiator.
///   *ErrorRecoverLevel=1
/// </summary>
/// <param name="m_CID">
///   Connection ID that received PDUs.
/// </param>
/// <param name="m_Counter">
///   Connection ID counter that received PDUs.
/// </param>
/// <param name="m_Status">
///   SCSI result status.
/// </param>
/// <param name="m_SenseKey">
///   A sense key value of established ACA.
/// </param>
/// <param name="m_ASC">
///   An additional sense code value of established ACA.
/// </param>
/// <param name="argMsg">
///   A message string that descripts cource of exception.
/// </param>
type SCSIStatusException
    (
        m_CID : CID_T,
        m_Counter : int32,
        m_Status : byte,
        m_SenseKey : byte,
        m_ASC : uint16,
        argMsg : string
    ) =
    inherit Exception( argMsg )


/// <summary>
///   SCSI error is occurred and ACA is established.
/// </summary>
/// <param name="m_source">
///   Command received source information.
/// </param>
/// <param name="m_Status">
///   SCSI result status.
/// </param>
/// <param name="m_SenseData">
///   Sense data.
/// </param>
/// <param name="m_Message">
///   Message string.
/// </param>
type SCSIACAException
    (
        m_source : CommandSourceInfo,
        m_Status : ScsiCmdStatCd,
        m_SenseData : SenseData,
        m_Message : string
    ) =
    inherit Exception( m_Message )

    new (
        source : CommandSourceInfo,
        isCurrent : bool,
        senseKey : SenseKeyCd,
        asc : ASCCd,
        fieldPointer : fieldPointerSenseKeySpecificData,
        message : string ) =
            new SCSIACAException (
                source,
                ScsiCmdStatCd.CHECK_CONDITION,
                new SenseData (
                    isCurrent,
                    senseKey,
                    asc,
                    fieldPointer,
                    VendorSpecificSenseDataDescType.TEXT_MESSAGE,
                    message
                ),
                message
            )

    new (
        source : CommandSourceInfo,
        isCurrent : bool,
        senseKey : SenseKeyCd,
        asc : ASCCd,
        message : string ) =
            new SCSIACAException (
                source,
                ScsiCmdStatCd.CHECK_CONDITION,
                new SenseData (
                    isCurrent,
                    senseKey,
                    asc,
                    message
                ),
                message
            )



    /// Get command that raise this exception source information.
    member _.CommandSource : CommandSourceInfo =
        m_source

    /// Get status value that has to be send to initiator.
    member _.Status : ScsiCmdStatCd =
        m_Status

    /// Get sense key value that has to be established at ACA.
    member _.SenseKey : SenseKeyCd =
        m_SenseData.SenseKey

    /// Get sense key value that has to be established at ACA.
    member _.ASC : ASCCd =
        m_SenseData.ASC

    /// Get sense data that has to be established at ACA.
    member _.IsCurrent : bool =
        m_SenseData.IsCurrent

    /// Get sense data that has to be established at ACA.
    member _.SenseData : SenseData =
        m_SenseData


