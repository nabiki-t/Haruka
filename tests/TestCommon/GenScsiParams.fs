//=============================================================================
// Haruka Software Storage.
// GenScsiParams.fs : Implement a function to generate a byte array for the SCSI parameter data.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test

//=============================================================================
// Import declaration

open System
open System.Text
open Haruka.Commons
open Haruka.Constants

//=============================================================================
// Type definition

/// Parameter data format that is returned for READ CAPACITY(16) command.
type ReadCapacity16Param = {
    ReturnedLogicalUnitAddress : uint64;
    BlockLengthInBytes : uint32;
    ReferenceTagOwnEnable : bool;
    ProtectionEnable : bool;
}

/// command descriptor format in SOCParam_All.
type SOCDesc_All = {
    OperationCode : byte;
    ServiceAction : uint16;
    ServiceActionValid : bool;
    CDBLength : uint16;
}

/// all_commands parameter data format that is returned for REPORT SUPPORTED OPERATION CODES.
type SOCParam_All = {
    CommandDataLength : uint32;
    Descs : SOCDesc_All[];
}

/// one_command  parameter data format that is returned for REPORT SUPPORTED OPERATION CODES.
type SOCParam_One = {
    Support : byte;
    CDBSize : uint16;
    CDBUsageData : byte[];
}

/// Parameter data format that is returned for REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS.
type RSTMFParam = {
    AbortTaskSupported : bool;
    AbortTaskSetSupported : bool;
    ClearACASupported : bool;
    ClearTaskSetSupported : bool;
    LUResetSupported : bool;
    QueryTaskSupported : bool;
    TargetResetSupported : bool;
    WakeupSupported : bool;
}

/// Standerd inquiry data format that is returned for INQUIRY.
type StanderdInquiry = {
    PeripheraQualifier : byte;
    PeripheralDeviceType : byte;
    RemovableMedium : bool;
    Version : byte;
    NormalACASupported : bool;
    HierarchicalSupported : bool;
    RsponseDataFormat : byte;
    AdditionalLength : byte;
    SCCSupported : bool;
    AccessControlsCoordinator : bool;
    TargetPortGroupSupport : byte;
    ThirdPartyCopy : bool;
    Protect : bool;
    BQueue : bool;
    EnclosureServices : bool;
    MultiPort : bool;
    MediumChanger : bool;
    LinkedCommand : bool;
    CmdQueue : bool;
    T10VendorIdentification : string;
    ProduceIdentification : string;
    ProductRevisionLevel : string;
    VersionDescriptor : uint16[];
}

/// Unit Serial Number VPD page data format that is returned for INQUIRY.
type UnitSerialNumberVPD = {
    PeripheraQualifier : byte;
    PeripheralDeviceType : byte;
    PageCode : byte;
    PageLength : byte;
    ProductSerialNumber : string;
}

/// Identifier descriptor format in DeviceIdentifierVPD.
type DeviceIdentifierDesc = {
    ProtocolIdentifier : byte;
    CodeSet : byte;
    ProtocolIdentifierValid : bool;
    Association : byte;
    IdentifierType : byte;
    IdentifierLength : byte;
    Identifier : string;
}

/// Device Identifier VPD page data format that is returned for INQUIRY.
type DeviceIdentifierVPD = {
    PeripheraQualifier : byte;
    PeripheralDeviceType : byte;
    PageCode : byte;
    PageLength : byte;
    IdentifierDescriptor : DeviceIdentifierDesc[];
}

/// Extended Inquiry Data VPD page data format that is returned for INQUIRY.
type ExtendedInquiryDataVPD = {
    PeripheraQualifier : byte;
    PeripheralDeviceType : byte;
    PageCode : byte;
    PageLength : byte;
    ReferenceTagOwnership : bool;
    GuardCheck : bool;
    ApplicationTagCheck : bool;
    ReferenceTagCheck : bool;
    GroupingFunctionSupported : bool;
    PrioritySupported : bool;
    HeadOfQueueSupported : bool;
    OrderedSupported : bool;
    SimpleSupported : bool;
    NonVolatileSupported : bool;
    VolatileSupported : bool;
}

/// Block Limit VPD page data format that is returned for INQUIRY.
type BlockLimitVPD = {
    PeripheraQualifier : byte;
    PeripheralDeviceType : byte;
    PageCode : byte;
    PageLength : byte;
    OptimalTransferLengthGramularity : uint16;
    MaximumTransferLength : uint32;
    OptimalTransferLength : uint32;
}

/// Block Device Characteristics VPD page data format that is returned for INQUIRY.
type BlockDeviceCharacteristicsVPD = {
    PeripheraQualifier : byte;
    PeripheralDeviceType : byte;
    PageCode : byte;
    PageLength : uint16;
    MediumRotationRate : uint16;
    ProductType : byte;
    WriteAfterBlockEraseRequired : byte;
    WriteAfterCryptographicEraseRequired : byte;
    NominalFormFactor : byte;
    ForceUnitAccessBehavior : bool;
    VerifyByteCheckUnmappedLBASupported : bool;
}

/// Supported VPD page data format that is returned for INQUIRY.
type SupportedVPD = {
    PeripheraQualifier : byte;
    PeripheralDeviceType : byte;
    PageCode : byte;
    PageLength : byte;
    SupportedVPGPages : byte[];
}

/// Block descriptor format in mode parameter.
type BlockDescriptor = {
    BlockCount : uint64;                    // In LONGLBA=0, only the lower 32 bits are used.
    BlockLength : uint32;
}

/// Control mode page data format that is used for MODE SELECT/MODE SENSE command.
type ControlModePage = {
    ParametersSavable : bool;
    PageLength : byte;
    TaskSetType : byte;
    AllowTaskManagementFunctionOnly : bool;
    DescriptorFormatSenseData : bool;
    GlobalLoggingTargetSaveDisable : bool;
    ReportLogExceptionCondition : bool;
    QueueAlgorithmModifier : byte;
    QueueErrorManagement : byte;
    ReportACheck : bool;
    UnitAttentionInterlocksControl : byte;
    SoftwareWriteProtect : bool;
    ApplicationTagOwner : bool;
    TaskAbortedStatus : bool;
    AutoLoadMode : byte;
    BusyTimeOutPeriod : uint16;
    ExtendedSelfTestCompletionTime : uint16;
}

/// Cache mode page data format that is used for MODE SELECT/MODE SENSE command.
type CacheModePage = {
    ParametersSavable : bool;
    PageLength : byte;
    InitiatorControl : bool;
    AbortPreFetch : bool;
    CachingAnalysisPermitted : bool;
    Discontinuity : bool;
    SizeEnable : bool;
    WritebackCacheEnable : bool;
    MultiplicationFactor : bool;
    ReadCacheDisable : bool;
    DemandReadRetentionPriority : byte;
    WriteRetentionPriority : byte;
    DisablePreFetchTransferLength : uint16;
    MinimumPreFetch : uint16;
    MaximumPreFetch : uint16;
    MaximumPreFetchCeiling : uint16;
    ForceSequentialWrite : bool;
    LogicalBlockCacheSegmentSize : bool;
    DisableReadAhead : bool;
    NonVolatileDisabled : bool;
    NumberOfCacheSegments : byte;
    CacheSegmentSize : uint16;
}

/// Informational Exceptions Control mode page data format that is used for MODE SELECT/MODE SENSE command.
type InformationalExceptionsControlModePage = {
    ParametersSavable : bool;
    PageLength : byte;
    Performance : bool;
    EnableBackgroundFunction : bool;
    EnableWarning : bool;
    DisableExceptionControl : bool;
    Test : bool;
    LogError : bool;
    MethodOfReportingInformationalExceptions : byte;
    IntervalTimer : uint32;
    ReportCount : uint32;
}

/// Mode parameter data format that is used for MODE SELECT(6)/MODE SENSE(6) command.
type ModeParameter6 = {
    ModeDataLength : byte;                  // ignored in mode select
    MediumType : byte;
    WriteProtect : bool;                    // ignored in mode select
    DisablePageOut_ForceUnitAccess : bool;  // ignored in mode select
    BlockDescriptorLength : byte;           // ignored in mode select
    Block : BlockDescriptor option;
    Control : ControlModePage option;
    Cache : CacheModePage option;
    InformationalExceptionsControl : InformationalExceptionsControlModePage option;
}

/// Mode parameter data format that is used for MODE SELECT(10)/MODE SENSE(10) command.
type ModeParameter10 = {
    ModeDataLength : uint16;                // ignored in mode select
    MediumType : byte;
    WriteProtect : bool;                    // ignored in mode select
    DisablePageOut_ForceUnitAccess : bool;  // ignored in mode select
    LongLBA : bool;
    BlockDescriptorLength : uint16;         // ignored in mode select
    Block : BlockDescriptor option;
    Control : ControlModePage option;
    Cache : CacheModePage option;
    InformationalExceptionsControl : InformationalExceptionsControlModePage option;
}

/// Parameter data format that is used for PERSISTENT RESERVE IN command READ KEYS service action.
type PR_ReadKey = {
    PersistentReservationsGeneration : uint32;
    AdditionalLength : uint32;
    ReservationKey : RESVKEY_T[];
}

/// Parameter data format that is used for PERSISTENT RESERVE IN command READ RESERVATION service action.
type PR_ReadReservation = {
    PersistentReservationsGeneration : uint32;
    AdditionalLength : uint32;
    ReservationKey : RESVKEY_T;
    Scope : byte;
    Type : byte;
}

/// Parameter data format that is used for PERSISTENT RESERVE IN command REPORT CAPABILITIES service action.
type PR_ReportCapabilities = {
    Length : uint16;
    CompatibleReservationHandling : bool;
    SpecifyInitiatorPortCapable : bool;
    AllTargetPortsCapable : bool;
    PersistThroughPowerLossCapable : bool;
    TypeMaskValid : bool;
    PersistThroughPowerLossActivated : bool;
    WriteExclusive_AllRegistrants : bool;
    ExclusiveAccess_RegistrantsOnly : bool;
    WriteExclusive_RegistrantsOnly : bool;
    ExclusiveAccess : bool;
    WriteExclusive : bool;
    ExclusiveAccess_AllRegistrants : bool;
}

/// Full status descriptor format in PERSISTENT RESERVE IN command READ FULL STATUS service action.
type PR_ReadFullStatus_FullStatusDesc = {
    ReservationKey : RESVKEY_T;
    AllTargetPorts : bool;
    ReservationHolder : bool;
    Scope : byte;
    Type : byte;
    RelativeTargetPortIdentifier : uint16;
    AdditionalDescriptorLength : uint32;
    FormatCode: byte;
    ProtocalIdentifier : byte;
    AdditionalLength : uint16;
    iSCSIName : string;
}

/// Parameter data format that is used for PERSISTENT RESERVE IN command READ FULL STATUS service action.
type PR_ReadFullStatus = {
    PersistentReservationsGeneration : uint32;
    AdditionalLength : uint32;
    FullStatusDescriptor : PR_ReadFullStatus_FullStatusDesc[];
}

//=============================================================================
// Class implementation

type GenScsiParams() =

    /// <summary>
    ///  Convert the byte array to the standerd inquiry data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by INQUIRY command.
    /// </param>
    /// <returns>
    ///  Converted standerd inquiry data.
    /// </returns>
    static member Inquiry_Standerd ( pv : PooledBuffer ) : StanderdInquiry =
        {
            PeripheraQualifier =
                if pv.Length > 0 then
                    ( pv.Array.[0] >>> 5 ) &&& 0x07uy
                else
                    0uy;
            PeripheralDeviceType = 
                if pv.Length > 0 then
                    pv.Array.[0] &&& 0x1Fuy
                else
                    0uy;
            RemovableMedium =
                if pv.Length > 1 then
                    Functions.CheckBitflag pv.Array.[1] 0x80uy
                else
                    false;
            Version = 
                if pv.Length > 2 then
                    pv.Array.[2]
                else
                    0uy;
            NormalACASupported =
                if pv.Length > 3 then
                    Functions.CheckBitflag pv.Array.[3] 0x20uy
                else
                    false;
            HierarchicalSupported =
                if pv.Length > 3 then
                    Functions.CheckBitflag pv.Array.[3] 0x10uy
                else
                    false;
            RsponseDataFormat =
                if pv.Length > 3 then
                    pv.Array.[3] &&& 0x0Fuy
                else
                    0uy;
            AdditionalLength =
                if pv.Length > 4 then
                    pv.Array.[4];
                else
                    0uy;
            SCCSupported =
                if pv.Length > 5 then
                    Functions.CheckBitflag pv.Array.[5] 0x80uy
                else
                    false;
            AccessControlsCoordinator =
                if pv.Length > 5 then
                    Functions.CheckBitflag pv.Array.[5] 0x40uy
                else
                    false;
            TargetPortGroupSupport =
                if pv.Length > 5 then
                    ( pv.Array.[5] >>> 4 ) &&& 0x03uy
                else
                    0uy;
            ThirdPartyCopy =
                if pv.Length > 5 then
                    Functions.CheckBitflag pv.Array.[5] 0x08uy
                else
                    false;
            Protect =
                if pv.Length > 5 then
                    Functions.CheckBitflag pv.Array.[5] 0x01uy
                else
                    false;
            BQueue =
                if pv.Length > 6 then
                    Functions.CheckBitflag pv.Array.[6] 0x80uy
                else
                    false;
            EnclosureServices =
                if pv.Length > 6 then
                    Functions.CheckBitflag pv.Array.[6] 0x40uy
                else
                    false;
            MultiPort =
                if pv.Length > 6 then
                    Functions.CheckBitflag pv.Array.[6] 0x10uy
                else
                    false;
            MediumChanger =
                if pv.Length > 6 then
                    Functions.CheckBitflag pv.Array.[6] 0x08uy
                else
                    false;
            LinkedCommand =
                if pv.Length > 7 then
                    Functions.CheckBitflag pv.Array.[7] 0x08uy
                else
                    false;
            CmdQueue =
                if pv.Length > 7 then
                    Functions.CheckBitflag pv.Array.[7] 0x02uy
                else
                    false;
            T10VendorIdentification =
                if pv.Length >= 16 then
                    pv.Array.[ 8 .. 15 ]
                    |> Array.takeWhile ( (<) 0uy )
                    |> Encoding.UTF8.GetString
                else
                    "";
            ProduceIdentification =
                if pv.Length >= 32 then
                    pv.Array.[ 16 .. 31 ]
                    |> Array.takeWhile ( (<) 0uy )
                    |> Encoding.UTF8.GetString
                else
                    "";
            ProductRevisionLevel =
                if pv.Length >= 36 then
                    pv.Array.[ 32 .. 35 ]
                    |> Array.takeWhile ( (<) 0uy )
                    |> Encoding.UTF8.GetString
                else
                    "";
            VersionDescriptor =
                [|
                    for i = 0 to 7 do
                        if pv.Length > ( i * 2 ) + 58 then
                            yield Functions.NetworkBytesToUInt16_InPooledBuffer pv ( ( i * 2 ) + 58 )
                |];
        }

    /// <summary>
    ///  Convert the byte array to the Unit Serial Number VPD page data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by INQUIRY command.
    /// </param>
    /// <returns>
    ///  Converted Unit Serial Number VPD page data.
    /// </returns>
    static member Inquiry_UnitSerialNumberVPD ( pv : PooledBuffer ) : UnitSerialNumberVPD =
        {
            PeripheraQualifier =
                if pv.Length > 0 then
                    ( pv.Array.[0] >>> 5 ) &&& 0x07uy
                else
                    0uy;
            PeripheralDeviceType = 
                if pv.Length > 0 then
                    pv.Array.[0] &&& 0x1Fuy
                else
                    0uy;
            PageCode =
                if pv.Length > 1 then
                    pv.Array.[1]
                else
                    0uy;
            PageLength =
                if pv.Length > 3 then
                    pv.Array.[3]
                else
                    0uy;
            ProductSerialNumber = 
                if pv.Length > 4 then
                    pv.Array.[ 4 .. ]
                    |> Array.takeWhile ( (<) 0uy )
                    |> Encoding.UTF8.GetString
                else
                    "";
        }

    /// <summary>
    ///  Convert the byte array to the Device Identifier VPD page data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by INQUIRY command.
    /// </param>
    /// <returns>
    ///  Converted Device Identifier VPD page data.
    /// </returns>
    static member Inquiry_DeviceIdentifierVPD ( pv : PooledBuffer ) : DeviceIdentifierVPD =
        {
            PeripheraQualifier =
                if pv.Length > 0 then
                    ( pv.Array.[0] >>> 5 ) &&& 0x07uy
                else
                    0uy;
            PeripheralDeviceType = 
                if pv.Length > 0 then
                    pv.Array.[0] &&& 0x1Fuy
                else
                    0uy;
            PageCode =
                if pv.Length > 1 then
                    pv.Array.[1]
                else
                    0uy;
            PageLength =
                if pv.Length > 3 then
                    pv.Array.[3]
                else
                    0uy;
            IdentifierDescriptor =
                let rec loop ( s : int ) ( acc : DeviceIdentifierDesc list ) =
                    if pv.Length >= s + 4 then
                        let wIdentifierLength = int ( pv.Array.[ s + 3 ] )
                        let r = {
                            ProtocolIdentifier = ( pv.Array.[ s ] >>> 4 ) &&& 0x0fuy;
                            CodeSet = ( pv.Array.[ s ] >>> 4 ) &&& 0x0Fuy;
                            ProtocolIdentifierValid = Functions.CheckBitflag pv.Array.[ s + 1 ] 0x80uy;
                            Association = ( pv.Array.[ s + 1 ] >>> 4 ) &&& 0x03uy;
                            IdentifierType = pv.Array.[ s + 1 ] &&& 0x0Fuy;
                            IdentifierLength = byte wIdentifierLength;
                            Identifier = 
                                if pv.Length > s + 4 && wIdentifierLength > 0 then
                                    let e = min ( pv.Length - 1 ) ( s + 4 + wIdentifierLength - 1 )
                                    pv.Array.[ s + 4 .. e ]
                                    |> Array.takeWhile ( (<) 0uy )
                                    |> Encoding.UTF8.GetString
                                else
                                    "";
                        }
                        if wIdentifierLength > 0 then
                            loop ( s + wIdentifierLength ) ( r :: acc )
                        else
                            r :: acc
                    else
                        acc
                if pv.Length > 4 then
                    loop 4 []
                    |> List.rev
                    |> List.toArray
                else
                    [||];
        }

    /// <summary>
    ///  Convert the byte array to the Extended Inquiry Data VPD page data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by INQUIRY command.
    /// </param>
    /// <returns>
    ///  Converted Extended Inquiry Data VPD page data.
    /// </returns>
    static member Inquiry_ExtendedInquiryDataVPD ( pv : PooledBuffer ) : ExtendedInquiryDataVPD =
        {
            PeripheraQualifier =
                if pv.Length > 0 then
                    ( pv.Array.[0] >>> 5 ) &&& 0x07uy
                else
                    0uy;
            PeripheralDeviceType = 
                if pv.Length > 0 then
                    pv.Array.[0] &&& 0x1Fuy
                else
                    0uy;
            PageCode =
                if pv.Length > 1 then
                    pv.Array.[1]
                else
                    0uy;
            PageLength =
                if pv.Length > 3 then
                    pv.Array.[3]
                else
                    0uy;
            ReferenceTagOwnership =
                if pv.Length > 4 then
                    Functions.CheckBitflag pv.Array.[4] 0x08uy;
                else
                    false;
            GuardCheck =
                if pv.Length > 4 then
                    Functions.CheckBitflag pv.Array.[4] 0x04uy;
                else
                    false;
            ApplicationTagCheck =
                if pv.Length > 4 then
                    Functions.CheckBitflag pv.Array.[4] 0x02uy;
                else
                    false;
            ReferenceTagCheck =
                if pv.Length > 4 then
                    Functions.CheckBitflag pv.Array.[4] 0x01uy;
                else
                    false;
            GroupingFunctionSupported =
                if pv.Length > 5 then
                    Functions.CheckBitflag pv.Array.[5] 0x10uy;
                else
                    false;
            PrioritySupported =
                if pv.Length > 5 then
                    Functions.CheckBitflag pv.Array.[5] 0x08uy;
                else
                    false;
            HeadOfQueueSupported =
                if pv.Length > 5 then
                    Functions.CheckBitflag pv.Array.[5] 0x04uy;
                else
                    false;
            OrderedSupported =
                if pv.Length > 5 then
                    Functions.CheckBitflag pv.Array.[5] 0x02uy;
                else
                    false;
            SimpleSupported =
                if pv.Length > 5 then
                    Functions.CheckBitflag pv.Array.[5] 0x01uy;
                else
                    false;
            NonVolatileSupported =
                if pv.Length > 6 then
                    Functions.CheckBitflag pv.Array.[6] 0x02uy;
                else
                    false;
            VolatileSupported =
                if pv.Length > 6 then
                    Functions.CheckBitflag pv.Array.[6] 0x01uy;
                else
                    false;
        }

    /// <summary>
    ///  Convert the byte array to the Block Limit VPD page data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by INQUIRY command.
    /// </param>
    /// <returns>
    ///  Converted Block Limit VPD page data.
    /// </returns>
    static member Inquiry_BlockLimitVPD ( pv : PooledBuffer ) : BlockLimitVPD =
        {
            PeripheraQualifier =
                if pv.Length > 0 then
                    ( pv.Array.[0] >>> 5 ) &&& 0x07uy
                else
                    0uy;
            PeripheralDeviceType = 
                if pv.Length > 0 then
                    pv.Array.[0] &&& 0x1Fuy
                else
                    0uy;
            PageCode =
                if pv.Length > 1 then
                    pv.Array.[1]
                else
                    0uy;
            PageLength =
                if pv.Length > 3 then
                    pv.Array.[3]
                else
                    0uy;
            OptimalTransferLengthGramularity =
                if pv.Length >= 8 then
                    Functions.NetworkBytesToUInt16_InPooledBuffer pv 6
                else
                    0us;
            MaximumTransferLength =
                if pv.Length >= 12 then
                    Functions.NetworkBytesToUInt32_InPooledBuffer pv 8
                else
                    0u;
            OptimalTransferLength =
                if pv.Length >= 16 then
                    Functions.NetworkBytesToUInt32_InPooledBuffer pv 12
                else
                    0u;
        }

    /// <summary>
    ///  Convert the byte array to the Block Device Characteristics VPD page data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by INQUIRY command.
    /// </param>
    /// <returns>
    ///  Converted Block Device Characteristics VPD page data.
    /// </returns>
    static member Inquiry_BlockDeviceCharacteristicsVPD ( pv : PooledBuffer ) : BlockDeviceCharacteristicsVPD =
        {
            PeripheraQualifier =
                if pv.Length > 0 then
                    ( pv.Array.[0] >>> 5 ) &&& 0x07uy
                else
                    0uy;
            PeripheralDeviceType = 
                if pv.Length > 0 then
                    pv.Array.[0] &&& 0x1Fuy
                else
                    0uy;
            PageCode =
                if pv.Length > 1 then
                    pv.Array.[1]
                else
                    0uy;
            PageLength =
                if pv.Length >= 4 then
                    Functions.NetworkBytesToUInt16_InPooledBuffer pv 2
                else
                    0us;
            MediumRotationRate =
                if pv.Length >= 6 then
                    Functions.NetworkBytesToUInt16_InPooledBuffer pv 4
                else
                    0us;
            ProductType =
                if pv.Length > 6 then
                    pv.Array.[6]
                else
                    0uy;
            WriteAfterBlockEraseRequired = 
                if pv.Length > 7 then
                    ( pv.Array.[7] >>> 6 ) &&& 0x03uy
                else
                    0uy;
            WriteAfterCryptographicEraseRequired = 
                if pv.Length > 7 then
                    ( pv.Array.[7] >>> 4 ) &&& 0x03uy
                else
                    0uy;
            NominalFormFactor = 
                if pv.Length > 7 then
                    pv.Array.[7] &&& 0x0Fuy
                else
                    0uy;
            ForceUnitAccessBehavior =
                if pv.Length > 8 then
                    Functions.CheckBitflag pv.Array.[8] 0x02uy;
                else
                    false;
            VerifyByteCheckUnmappedLBASupported =
                if pv.Length > 8 then
                    Functions.CheckBitflag pv.Array.[8] 0x01uy;
                else
                    false;
        }

    /// <summary>
    ///  Convert the byte array to the Supported VPD page data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by INQUIRY command.
    /// </param>
    /// <returns>
    ///  Converted Supported VPD page data.
    /// </returns>
    static member Inquiry_SupportedVPD ( pv : PooledBuffer ) : SupportedVPD =
        {
            PeripheraQualifier =
                if pv.Length > 0 then
                    ( pv.Array.[0] >>> 5 ) &&& 0x07uy
                else
                    0uy;
            PeripheralDeviceType = 
                if pv.Length > 0 then
                    pv.Array.[0] &&& 0x1Fuy
                else
                    0uy;
            PageCode =
                if pv.Length > 1 then
                    pv.Array.[1]
                else
                    0uy;
            PageLength =
                if pv.Length > 3 then
                    pv.Array.[3]
                else
                    0uy;
            SupportedVPGPages =
                if pv.Length > 4 then
                    pv.Array.[ 4 .. ]
                else
                    [||];
        }

    /// <summary>
    ///  Convert the mode parameter data to the byte array.
    /// </summary>
    /// <param name="m">
    ///  Mode parameter data that will be send to the target by MODE SELECT(6) command.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member ModeSelect6 ( m : ModeParameter6 ) : PooledBuffer =
        let blockDescriptor =
            if m.Block.IsSome then
                [|
                    yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 m.Block.Value.BlockCount )
                    yield! Functions.UInt32ToNetworkBytes_NewVec ( m.Block.Value.BlockLength &&& 0x00FFFFFFu )
                |]
            else
                Array.Empty()

        let modePages = [|
            if m.Control.IsSome then
                yield! GenScsiParams.GenControlModePageBytes m.Control.Value
            if m.Cache.IsSome then
                yield! GenScsiParams.GenCacheModePageBytes m.Cache.Value
            if m.InformationalExceptionsControl.IsSome then
                yield! GenScsiParams.GenInformationalExceptionsControlModePageBytes m.InformationalExceptionsControl.Value
        |]

        let modeDataLength = 3uy + ( byte blockDescriptor.Length ) + ( byte modePages.Length );
        let header = [|
            yield modeDataLength;
            yield m.MediumType;
            yield ( Functions.SetBitflag m.WriteProtect 0x80uy ) |||
                    ( Functions.SetBitflag m.DisablePageOut_ForceUnitAccess 0x10uy );
            yield blockDescriptor.Length |> byte
        |]
        let modePageData = [|
            yield! header;
            yield! blockDescriptor;
            yield! modePages;
        |]
        PooledBuffer.Rent modePageData

    /// <summary>
    ///  Convert the mode parameter data to the byte array.
    /// </summary>
    /// <param name="m">
    ///  Mode parameter data that will be send to the target by MODE SELECT(10) command.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member ModeSelect10 ( m : ModeParameter10 ) : PooledBuffer =
        let blockDescriptor =
            if m.Block.IsSome then
                if not m.LongLBA then
                    [|
                        yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 m.Block.Value.BlockCount );
                        yield! Functions.UInt32ToNetworkBytes_NewVec ( m.Block.Value.BlockLength &&& 0x00FFFFFFu );
                    |]
                else
                    [|
                        yield! Functions.UInt64ToNetworkBytes_NewVec m.Block.Value.BlockCount;
                        yield 0x00uy;
                        yield 0x00uy;
                        yield 0x00uy;
                        yield 0x00uy;
                        yield! Functions.UInt32ToNetworkBytes_NewVec m.Block.Value.BlockLength;
                    |]
            else
                Array.Empty()

        let modePages = [|
            if m.Control.IsSome then
                yield! GenScsiParams.GenControlModePageBytes m.Control.Value
            if m.Cache.IsSome then
                yield! GenScsiParams.GenCacheModePageBytes m.Cache.Value
            if m.InformationalExceptionsControl.IsSome then
                yield! GenScsiParams.GenInformationalExceptionsControlModePageBytes m.InformationalExceptionsControl.Value
        |]

        let modeDataLength = 3us + ( uint16 blockDescriptor.Length ) + ( uint16 modePages.Length );
        let header = [|
            yield! Functions.UInt16ToNetworkBytes_NewVec modeDataLength;
            yield m.MediumType;
            yield ( Functions.SetBitflag m.WriteProtect 0x80uy ) |||
                    ( Functions.SetBitflag m.DisablePageOut_ForceUnitAccess 0x10uy );
            yield Functions.SetBitflag m.LongLBA 0x01uy;
            yield 0x000uy;
            yield! Functions.UInt16ToNetworkBytes_NewVec ( uint16 blockDescriptor.Length );
        |]
        let modePageData = [|
            yield! header;
            yield! blockDescriptor;
            yield! modePages;
        |]
        PooledBuffer.Rent modePageData

    /// <summary>
    ///  Convert the Control Mode Page data to the byte array.
    /// </summary>
    /// <param name="c">
    ///  Control Mode Page in the mode select parameter data.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member private GenControlModePageBytes ( c : ControlModePage ) : byte[] =
        [|
            yield ( Functions.SetBitflag c.ParametersSavable 0x80uy ) ||| 0x0Auy;
            yield 0x0Auy;
            yield ( ( c.TaskSetType &&& 0x07uy ) <<< 5 ) |||
                    ( Functions.SetBitflag c.AllowTaskManagementFunctionOnly 0x10uy ) |||
                    ( Functions.SetBitflag c.DescriptorFormatSenseData 0x04uy ) |||
                    ( Functions.SetBitflag c.GlobalLoggingTargetSaveDisable 0x02uy ) |||
                    ( Functions.SetBitflag c.ReportLogExceptionCondition 0x01uy );
            yield ( ( c.QueueAlgorithmModifier &&& 0x0Fuy ) <<< 4 ) |||
                    ( ( c.QueueErrorManagement &&& 0x03uy ) <<< 1 );
            yield ( Functions.SetBitflag c.ReportACheck 0x40uy ) |||
                    ( ( c.UnitAttentionInterlocksControl &&& 0x03uy ) <<< 4 ) |||
                    ( Functions.SetBitflag c.SoftwareWriteProtect 0x08uy );
            yield ( Functions.SetBitflag c.ApplicationTagOwner 0x80uy ) |||
                    ( Functions.SetBitflag c.TaskAbortedStatus 0x40uy ) |||
                    ( c.AutoLoadMode &&& 0x07uy );
            yield 0uy;
            yield 0uy;
            yield! Functions.UInt16ToNetworkBytes_NewVec c.BusyTimeOutPeriod;
            yield! Functions.UInt16ToNetworkBytes_NewVec c.ExtendedSelfTestCompletionTime;
        |]

    /// <summary>
    ///  Convert the Cache Mode Page data to the byte array.
    /// </summary>
    /// <param name="c">
    ///  Cache Mode Page in the mode select parameter data.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member private GenCacheModePageBytes ( c : CacheModePage ) : byte[] =
        [|
            yield ( Functions.SetBitflag c.ParametersSavable 0x80uy ) ||| 0x08uy;
            yield 0x12uy;
            yield ( Functions.SetBitflag c.InitiatorControl 0x80uy ) |||
                    ( Functions.SetBitflag c.AbortPreFetch 0x40uy ) |||
                    ( Functions.SetBitflag c.CachingAnalysisPermitted 0x20uy ) |||
                    ( Functions.SetBitflag c.Discontinuity 0x10uy ) |||
                    ( Functions.SetBitflag c.SizeEnable 0x08uy ) |||
                    ( Functions.SetBitflag c.WritebackCacheEnable 0x04uy ) |||
                    ( Functions.SetBitflag c.MultiplicationFactor 0x02uy ) |||
                    ( Functions.SetBitflag c.ReadCacheDisable 0x01uy );
            yield ( ( c.DemandReadRetentionPriority &&& 0x0Fuy ) <<< 4 ) |||
                    ( c.WriteRetentionPriority &&& 0x0Fuy );
            yield! Functions.UInt16ToNetworkBytes_NewVec c.DisablePreFetchTransferLength;
            yield! Functions.UInt16ToNetworkBytes_NewVec c.MinimumPreFetch;
            yield! Functions.UInt16ToNetworkBytes_NewVec c.MaximumPreFetch;
            yield! Functions.UInt16ToNetworkBytes_NewVec c.MaximumPreFetchCeiling;
            yield ( Functions.SetBitflag c.ForceSequentialWrite 0x80uy ) |||
                    ( Functions.SetBitflag c.LogicalBlockCacheSegmentSize 0x40uy ) |||
                    ( Functions.SetBitflag c.DisableReadAhead 0x20uy ) |||
                    ( Functions.SetBitflag c.NonVolatileDisabled 0x01uy );
            yield c.NumberOfCacheSegments;
            yield! Functions.UInt16ToNetworkBytes_NewVec c.CacheSegmentSize;
            yield 0x00uy;
            yield 0x00uy;
            yield 0x00uy;
        |]

    /// <summary>
    ///  Convert the Informational Exceptions Control Mode Page data to the byte array.
    /// </summary>
    /// <param name="c">
    ///  Informational Exceptions Control Mode Page in the mode select parameter data.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member private GenInformationalExceptionsControlModePageBytes ( c : InformationalExceptionsControlModePage ) : byte[] =
        [|
            yield ( Functions.SetBitflag c.ParametersSavable 0x80uy ) ||| 0x1Cuy;
            yield 0x0Auy;
            yield ( Functions.SetBitflag c.Performance 0x80uy ) |||
                    ( Functions.SetBitflag c.EnableBackgroundFunction 0x20uy ) |||
                    ( Functions.SetBitflag c.EnableWarning 0x10uy ) |||
                    ( Functions.SetBitflag c.DisableExceptionControl 0x08uy ) |||
                    ( Functions.SetBitflag c.Test 0x04uy ) |||
                    ( Functions.SetBitflag c.LogError 0x01uy );
            yield ( c.MethodOfReportingInformationalExceptions &&& 0x0Fuy );
            yield! Functions.UInt32ToNetworkBytes_NewVec c.IntervalTimer;
            yield! Functions.UInt32ToNetworkBytes_NewVec c.ReportCount;
        |]

    /// <summary>
    ///  Convert the byte array to the mode sense 6 parameter data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by MODE SENSE(6) command.
    /// </param>
    /// <returns>
    ///  Converted mode sense 6 parameter data.
    /// </returns>
    static member ModeSense6 ( pv : PooledBuffer ) : ModeParameter6 =
        let blockDescLength =
            if pv.Length > 3 then pv.Array.[3] else 0uy
        {
            ModeDataLength =
                if pv.Length > 0 then
                    pv.Array.[0]
                else
                    0uy;
            MediumType =
                if pv.Length > 1 then
                    pv.Array.[1]
                else
                    0uy;
            WriteProtect =
                if pv.Length > 2 then
                    Functions.CheckBitflag pv.Array.[2] 0x80uy
                else
                    false;
            DisablePageOut_ForceUnitAccess =
                if pv.Length > 2 then
                    Functions.CheckBitflag pv.Array.[2] 0x10uy
                else
                    false;
            BlockDescriptorLength = blockDescLength;
            Block = 
                if pv.Length >= 12 && blockDescLength >= 8uy then
                    Some {
                        BlockCount = Functions.NetworkBytesToUInt32_InPooledBuffer pv 4 |> uint64;
                        BlockLength = ( Functions.NetworkBytesToUInt32_InPooledBuffer pv 8 ) &&& 0x00FFFFFFu;
                    }
                else
                    None;
            Control = GenScsiParams.BytesToControlModePage pv ( 4 + int blockDescLength );
            Cache = GenScsiParams.BytesToCacheModePage pv ( 4 + int blockDescLength );
            InformationalExceptionsControl = GenScsiParams.BytesToInformationalExceptionsControlModePage pv ( 4 + int blockDescLength );
        }

    /// <summary>
    ///  Convert the byte array to the mode sense 10 parameter data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by MODE SENSE(10) command.
    /// </param>
    /// <returns>
    ///  Converted mode sense 6 parameter data.
    /// </returns>
    static member ModeSense10 ( pv : PooledBuffer ) : ModeParameter10 =
        let blockDescLength =
            if pv.Length >= 8 then
                Functions.NetworkBytesToUInt16_InPooledBuffer pv 6
            else
                0us
        let longlba =
            if pv.Length > 4 then
                Functions.CheckBitflag pv.Array.[4] 0x01uy
            else
                false;
        {
            ModeDataLength =
                if pv.Length >= 2 then
                    Functions.NetworkBytesToUInt16_InPooledBuffer pv 0
                else
                    0us;
            MediumType =
                if pv.Length > 2 then
                    pv.Array.[2]
                else
                    0uy;
            WriteProtect =
                if pv.Length > 3 then
                    Functions.CheckBitflag pv.Array.[3] 0x80uy
                else
                    false;
            DisablePageOut_ForceUnitAccess =
                if pv.Length > 3 then
                    Functions.CheckBitflag pv.Array.[3] 0x10uy
                else
                    false;
            LongLBA = longlba;
            BlockDescriptorLength = blockDescLength;
            Block =
                if longlba then
                    if pv.Length >= 24 && blockDescLength >= 16us then
                        Some {
                            BlockCount = Functions.NetworkBytesToUInt64_InPooledBuffer pv 8;
                            BlockLength = Functions.NetworkBytesToUInt32_InPooledBuffer pv 20;
                        }
                    else
                        None;
                else
                    if pv.Length >= 16 && blockDescLength >= 8us then
                        Some {
                            BlockCount = Functions.NetworkBytesToUInt32_InPooledBuffer pv 8 |> uint64;
                            BlockLength = ( Functions.NetworkBytesToUInt32_InPooledBuffer pv 12 ) &&& 0x00FFFFFFu;
                        }
                    else
                        None;
            Control = GenScsiParams.BytesToControlModePage pv ( 8 + int blockDescLength );
            Cache = GenScsiParams.BytesToCacheModePage pv ( 8 + int blockDescLength );
            InformationalExceptionsControl = GenScsiParams.BytesToInformationalExceptionsControlModePage pv ( 8 + int blockDescLength );
        }

    /// <summary>
    ///  Convert the byte array to the Control Mode Page data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by MODE SENSE(6) command.
    /// </param>
    /// <param name="s">
    ///  Start position of the mode pages.
    /// </param>
    /// <returns>
    ///  Converted Control Mode Page data or, if missing it returns None.
    /// </returns>
    static member private BytesToControlModePage ( pv : PooledBuffer ) ( s : int ) : ControlModePage option =
        let rec loop ( p : int ) : ControlModePage option =
            if pv.Length < p + 12 then
                None
            elif ( pv.Array.[ p + 0 ] &&& 0x7Fuy ) = 0x0Auy then
                {
                    ParametersSavable = Functions.CheckBitflag pv.Array.[ p ] 0x80uy;
                    PageLength = pv.Array.[ p + 1 ];
                    TaskSetType = ( pv.Array.[ p + 2 ] >>> 5 ) &&& 0x07uy;
                    AllowTaskManagementFunctionOnly = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x10uy;
                    DescriptorFormatSenseData = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x04uy;
                    GlobalLoggingTargetSaveDisable = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x02uy;
                    ReportLogExceptionCondition = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x01uy;
                    QueueAlgorithmModifier = ( pv.Array.[ p + 3 ] >>> 4 ) &&& 0x0Fuy;
                    QueueErrorManagement = ( pv.Array.[ p + 3 ] >>> 1 ) &&& 0x03uy;
                    ReportACheck = Functions.CheckBitflag pv.Array.[ p + 4 ] 0x40uy;
                    UnitAttentionInterlocksControl = ( pv.Array.[ p + 4 ] >>> 4 ) &&& 0x03uy;
                    SoftwareWriteProtect = Functions.CheckBitflag pv.Array.[ p + 4 ] 0x08uy;
                    ApplicationTagOwner = Functions.CheckBitflag pv.Array.[ p + 5 ] 0x80uy;
                    TaskAbortedStatus = Functions.CheckBitflag pv.Array.[ p + 5 ] 0x40uy;
                    AutoLoadMode = pv.Array.[ p + 5 ] &&& 0x07uy;
                    BusyTimeOutPeriod = Functions.NetworkBytesToUInt16_InPooledBuffer pv ( p + 8 );
                    ExtendedSelfTestCompletionTime = Functions.NetworkBytesToUInt16_InPooledBuffer pv ( p + 10 );
                }
                |> Some
            else
                let wPageLength = pv.Array.[ p + 1 ]
                if wPageLength > 0uy then
                    loop ( p + int wPageLength )
                else
                    None
        loop s

    /// <summary>
    ///  Convert the byte array to the Cache Mode Page data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by MODE SENSE(6) command.
    /// </param>
    /// <param name="s">
    ///  Start position of the mode pages.
    /// </param>
    /// <returns>
    ///  Converted Cache Mode Page data or, if missing it returns None.
    /// </returns>
    static member private BytesToCacheModePage ( pv : PooledBuffer ) ( s : int ) : CacheModePage option =
        let rec loop ( p : int ) : CacheModePage option =
            if pv.Length < p + 20 then
                None
            elif ( pv.Array.[ p + 0 ] &&& 0x7Fuy ) = 0x08uy then
                {
                    ParametersSavable = Functions.CheckBitflag pv.Array.[ p ] 0x80uy;
                    PageLength = pv.Array.[ p + 1 ];
                    InitiatorControl = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x80uy;
                    AbortPreFetch = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x40uy;
                    CachingAnalysisPermitted = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x20uy;
                    Discontinuity = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x10uy;
                    SizeEnable = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x08uy;
                    WritebackCacheEnable = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x04uy;
                    MultiplicationFactor = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x02uy;
                    ReadCacheDisable = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x01uy;
                    DemandReadRetentionPriority = ( pv.Array.[ p + 3 ] >>> 4 ) &&& 0x0Fuy;
                    WriteRetentionPriority = pv.Array.[ p + 3 ] &&& 0x0Fuy;
                    DisablePreFetchTransferLength = Functions.NetworkBytesToUInt16_InPooledBuffer pv ( p + 4 );
                    MinimumPreFetch = Functions.NetworkBytesToUInt16_InPooledBuffer pv ( p + 6 );
                    MaximumPreFetch = Functions.NetworkBytesToUInt16_InPooledBuffer pv ( p + 8 );
                    MaximumPreFetchCeiling = Functions.NetworkBytesToUInt16_InPooledBuffer pv ( p + 10 );
                    ForceSequentialWrite = Functions.CheckBitflag pv.Array.[ p + 12 ] 0x80uy;
                    LogicalBlockCacheSegmentSize = Functions.CheckBitflag pv.Array.[ p + 12 ] 0x40uy;
                    DisableReadAhead = Functions.CheckBitflag pv.Array.[ p + 12 ] 0x20uy;
                    NonVolatileDisabled = Functions.CheckBitflag pv.Array.[ p + 12 ] 0x01uy;
                    NumberOfCacheSegments = pv.Array.[ p + 13 ];
                    CacheSegmentSize = Functions.NetworkBytesToUInt16_InPooledBuffer pv ( p + 14 );
                }
                |> Some
            else
                let wPageLength = pv.Array.[ p + 1 ]
                if wPageLength > 0uy then
                    loop ( p + int wPageLength )
                else
                    None
        loop s

    /// <summary>
    ///  Convert the byte array to the Informational Exceptions Control Mode Page data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by MODE SENSE(6) command.
    /// </param>
    /// <param name="s">
    ///  Start position of the mode pages.
    /// </param>
    /// <returns>
    ///  Converted Informational Exceptions Control Mode Page data or, if missing it returns None.
    /// </returns>
    static member private BytesToInformationalExceptionsControlModePage ( pv : PooledBuffer ) ( s : int ) : InformationalExceptionsControlModePage option =
        let rec loop ( p : int ) : InformationalExceptionsControlModePage option =
            if pv.Length < p + 12 then
                None
            elif ( pv.Array.[ p + 0 ] &&& 0x7Fuy ) = 0x1Cuy then
                {
                    ParametersSavable = Functions.CheckBitflag pv.Array.[ p ] 0x80uy;
                    PageLength = pv.Array.[ p + 1 ];
                    Performance = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x80uy;
                    EnableBackgroundFunction = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x20uy;
                    EnableWarning = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x10uy;
                    DisableExceptionControl = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x08uy;
                    Test = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x04uy;
                    LogError = Functions.CheckBitflag pv.Array.[ p + 2 ] 0x01uy;
                    MethodOfReportingInformationalExceptions = pv.Array.[ p + 3 ] &&& 0x0Fuy;
                    IntervalTimer = Functions.NetworkBytesToUInt32_InPooledBuffer pv ( p + 4 );
                    ReportCount = Functions.NetworkBytesToUInt32_InPooledBuffer pv ( p + 8 );
                }
                |> Some
            else
                let wPageLength = pv.Array.[ p + 1 ]
                if wPageLength > 0uy then
                    loop ( p + int wPageLength )
                else
                    None
        loop s

    /// <summary>
    ///  Convert the byte array to the PERSISTENT RESERVE IN command READ KEYS service action parameter data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by PERSISTENT RESERVE IN command.
    /// </param>
    /// <returns>
    ///  Converted PR_ReadKey data.
    /// </returns>
    static member PersistentReserveIn_ReadKey ( pv : PooledBuffer ) : PR_ReadKey =
        let wAdditionalLength = 
            if pv.Length >= 8 then
                Functions.NetworkBytesToUInt32_InPooledBuffer pv 4
            else
                0u;
        {
            PersistentReservationsGeneration =
                if pv.Length >= 4 then
                    Functions.NetworkBytesToUInt32_InPooledBuffer pv 0
                else
                    0u;
            AdditionalLength = wAdditionalLength;
            ReservationKey =
                if pv.Length >= 16 then
                    [|
                        let cnt = ( min ( pv.Length - 8 ) ( int wAdditionalLength ) ) / 8;
                        for i = 0 to cnt - 1 do
                            Functions.NetworkBytesToUInt64_InPooledBuffer pv ( i * 8 + 8 ) |> resvkey_me.fromPrim
                    |];
                else
                    [||];
        }

    /// <summary>
    ///  Convert the byte array to the PERSISTENT RESERVE IN  command READ RESERVATION service action parameter data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by PERSISTENT RESERVE IN command.
    /// </param>
    /// <returns>
    ///  Converted PR_ReadReservation data.
    /// </returns>
    static member PersistentReserveIn_ReadReservation ( pv : PooledBuffer ) : PR_ReadReservation =
        {
            PersistentReservationsGeneration =
                if pv.Length >= 4 then
                    Functions.NetworkBytesToUInt32_InPooledBuffer pv 0
                else
                    0u;
            AdditionalLength =
                if pv.Length >= 8 then
                    Functions.NetworkBytesToUInt32_InPooledBuffer pv 4
                else
                    0u;
            ReservationKey =
                if pv.Length >= 16 then
                    Functions.NetworkBytesToUInt64_InPooledBuffer pv 8 |> resvkey_me.fromPrim
                else
                    resvkey_me.zero;
            Scope =
                if pv.Length > 21 then
                    ( pv.Array.[ 21 ] >>> 4 ) &&& 0x0Fuy;
                else
                    0uy;
            Type =
                if pv.Length > 21 then
                    pv.Array.[ 21 ] &&& 0x0Fuy;
                else
                    0uy;
        }

    /// <summary>
    ///  Convert the byte array to the PERSISTENT RESERVE IN  command REPORT CAPABILITIES service action parameter data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by PERSISTENT RESERVE IN command.
    /// </param>
    /// <returns>
    ///  Converted PR_ReportCapabilities data.
    /// </returns>
    static member PersistentReserveIn_ReportCapabilities ( pv : PooledBuffer ) : PR_ReportCapabilities =
        {
            Length =
                if pv.Length >= 2 then
                    Functions.NetworkBytesToUInt16_InPooledBuffer pv 0
                else
                    0us;
            CompatibleReservationHandling =
                if pv.Length > 2 then
                    Functions.CheckBitflag pv.Array.[2] 0x10uy
                else
                    false;
            SpecifyInitiatorPortCapable =
                if pv.Length > 2 then
                    Functions.CheckBitflag pv.Array.[2] 0x08uy
                else
                    false;
            AllTargetPortsCapable =
                if pv.Length > 2 then
                    Functions.CheckBitflag pv.Array.[2] 0x04uy
                else
                    false;
            PersistThroughPowerLossCapable =
                if pv.Length > 2 then
                    Functions.CheckBitflag pv.Array.[2] 0x01uy
                else
                    false;
            TypeMaskValid =
                if pv.Length > 3 then
                    Functions.CheckBitflag pv.Array.[3] 0x80uy
                else
                    false;
            PersistThroughPowerLossActivated =
                if pv.Length > 3 then
                    Functions.CheckBitflag pv.Array.[3] 0x01uy
                else
                    false;
            WriteExclusive_AllRegistrants =
                if pv.Length > 4 then
                    Functions.CheckBitflag pv.Array.[4] 0x80uy
                else
                    false;
            ExclusiveAccess_RegistrantsOnly =
                if pv.Length > 4 then
                    Functions.CheckBitflag pv.Array.[4] 0x40uy
                else
                    false;
            WriteExclusive_RegistrantsOnly =
                if pv.Length > 4 then
                    Functions.CheckBitflag pv.Array.[4] 0x20uy
                else
                    false;
            ExclusiveAccess =
                if pv.Length > 4 then
                    Functions.CheckBitflag pv.Array.[4] 0x08uy
                else
                    false;
            WriteExclusive =
                if pv.Length > 4 then
                    Functions.CheckBitflag pv.Array.[4] 0x02uy
                else
                    false;
            ExclusiveAccess_AllRegistrants =
                if pv.Length > 5 then
                    Functions.CheckBitflag pv.Array.[5] 0x01uy
                else
                    false;
        }

    /// <summary>
    ///  Convert the byte array to the PERSISTENT RESERVE IN  command READ FULL STATUS service action parameter data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by PERSISTENT RESERVE IN command.
    /// </param>
    /// <returns>
    ///  Converted PR_ReadFullStatus data.
    /// </returns>
    static member PersistentReserveIn_ReadFullStatus ( pv : PooledBuffer ) : PR_ReadFullStatus =
        let wAdditionalLength =
            if pv.Length >= 8 then
                Functions.NetworkBytesToUInt32_InPooledBuffer pv 4
            else
                0u;
        {
            PersistentReservationsGeneration =
                if pv.Length >= 4 then
                    Functions.NetworkBytesToUInt32_InPooledBuffer pv 0
                else
                    0u;
            AdditionalLength = wAdditionalLength;
            FullStatusDescriptor =
                let wlen = min ( int wAdditionalLength + 7 ) pv.Length
                let rec loop ( s : int ) ( acc : PR_ReadFullStatus_FullStatusDesc list ) =
                    if s < wlen then
                        let wAdditionalDescriptorLength =
                                if wlen >= s + 24  then
                                    Functions.NetworkBytesToUInt32_InPooledBuffer pv ( s + 20 )
                                else
                                    0u;
                        let wAdditionalLength =
                                if wlen >= s + 28 then
                                    Functions.NetworkBytesToUInt16_InPooledBuffer pv ( s + 26 )
                                else
                                    0us;
                        let desc = {
                            ReservationKey =
                                if wlen >= s + 8  then
                                    Functions.NetworkBytesToUInt64_InPooledBuffer pv s |> resvkey_me.fromPrim
                                else
                                    resvkey_me.zero;
                            AllTargetPorts =
                                if wlen > s + 12 then
                                    Functions.CheckBitflag pv.Array.[ s + 12 ] 0x02uy
                                else
                                    false;
                            ReservationHolder =
                                if wlen > s + 12 then
                                    Functions.CheckBitflag pv.Array.[ s + 12 ] 0x01uy
                                else
                                    false;
                            Scope =
                                if wlen > s + 13 then
                                    ( pv.Array.[ s + 13 ] >>> 4 ) &&& 0x0Fuy
                                else
                                    0uy;
                            Type =
                                if wlen > s + 13 then
                                    pv.Array.[ s + 13 ] &&& 0x0Fuy
                                else
                                    0uy;
                            RelativeTargetPortIdentifier =
                                if wlen >= s + 20  then
                                    Functions.NetworkBytesToUInt16_InPooledBuffer pv ( s + 18 )
                                else
                                    0us;
                            AdditionalDescriptorLength = wAdditionalDescriptorLength;
                            FormatCode =
                                if wlen > s + 24 then
                                    ( pv.Array.[ s + 24 ] >>> 6 ) &&& 0x03uy
                                else
                                    0uy;
                            ProtocalIdentifier =
                                if wlen > s + 24 then
                                    pv.Array.[ s + 24 ] &&& 0x0Fuy
                                else
                                    0uy;
                            AdditionalLength =
                                if wlen >= s + 28 then
                                    Functions.NetworkBytesToUInt16_InPooledBuffer pv ( s + 26 )
                                else
                                    0us;
                            iSCSIName =
                                if wlen > s + 28 && wAdditionalLength > 0us then
                                    let wnextpos1 = s + int wAdditionalDescriptorLength
                                    let wnextpos2 = s + 28 + int wAdditionalLength
                                    let epos = ( min wnextpos1 wnextpos2 ) - 1
                                    pv.Array.[ s + 28 .. epos ]
                                    |> Array.takeWhile ( (<) 0uy )
                                    |> Encoding.UTF8.GetString
                                else
                                    "";
                        }
                        if wAdditionalDescriptorLength > 0u then
                            loop ( s + int wAdditionalDescriptorLength ) ( desc :: acc )
                        else
                            desc :: acc
                    else
                        acc
                loop 8 []
                |> List.rev
                |> List.toArray
        }

    /// <summary>
    ///  Convert the PERSISTENT RESERVE OUT parameter data to the byte array.
    /// </summary>
    /// <param name="c">
    ///  Parameter data that will be send to the target by PERSISTENT RESERVE OUT command without REGISTER AND MOVE service action.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member PersistentReserveOut_BasicParameterList ( c : Haruka.BlockDeviceLU.BasicParameterList ) : PooledBuffer =
        let transportIDList =
            c.TransportID
            |> Array.map GenScsiParams.iSCSINameToTransportID
            |> Array.concat
        [|
            yield! Functions.UInt64ToNetworkBytes_NewVec ( c.ReservationKey |> resvkey_me.toPrim );
            yield! Functions.UInt64ToNetworkBytes_NewVec ( c.ServiceActionReservationKey |> resvkey_me.toPrim );
            yield 0x00uy;
            yield 0x00uy;
            yield 0x00uy;
            yield 0x00uy;
            yield ( Functions.SetBitflag c.SPEC_I_PT 0x08uy ) |||
                    ( Functions.SetBitflag c.ALL_TG_PT 0x04uy ) |||
                    ( Functions.SetBitflag c.APTPL 0x01uy );
            yield 0x00uy;
            yield 0x00uy;
            yield 0x00uy;
            yield! Functions.Int32ToNetworkBytes_NewVec transportIDList.Length;
            yield! transportIDList;
        |]
        |> PooledBuffer.Rent

    /// <summary>
    ///  Convert the PERSISTENT RESERVE OUT parameter data to the byte array.
    /// </summary>
    /// <param name="c">
    ///  Parameter data that will be send to the target by PERSISTENT RESERVE OUT command with REGISTER AND MOVE service action.
    /// </param>
    /// <returns>
    ///  Converted byte array.
    /// </returns>
    static member PersistentReserveOut_MoveParameterList ( c : Haruka.BlockDeviceLU.MoveParameterList ) : PooledBuffer =
        let transportID = GenScsiParams.iSCSINameToTransportID c.TransportID
        [|
            yield! Functions.UInt64ToNetworkBytes_NewVec ( c.ReservationKey |> resvkey_me.toPrim );
            yield! Functions.UInt64ToNetworkBytes_NewVec ( c.ServiceActionReservationKey |> resvkey_me.toPrim );
            yield 0x00uy;
            yield ( Functions.SetBitflag c.UNREG 0x02uy ) ||| ( Functions.SetBitflag c.APTPL 0x01uy );
            yield! Functions.UInt16ToNetworkBytes_NewVec c.RelativeTargetPortIdentifier;
            yield! Functions.Int32ToNetworkBytes_NewVec transportID.Length;
            yield! transportID;
        |]
        |> PooledBuffer.Rent

    /// <summary>
    ///  Convert iSCSI name string and ISID value to TransportID bytes array.
    /// </summary>
    /// <param name="name">
    ///  iSCSI Name string
    /// </param>
    /// <param name="isid">
    ///  ISID value, or None.
    /// </param>
    /// <returns>
    ///  Converted TransportID value.
    /// </returns>
    static member private iSCSINameToTransportID ( name : string, isid : ISID_T option ) : byte[] =
        let iScsiName =
            match isid with
            | Some x ->
                sprintf "%s,i,%s" name ( isid_me.toString x )
            | None ->
                name
        let iScsiNameBytes = Encoding.UTF8.GetBytes iScsiName
        let additionalLength =
            Functions.AddPaddingLengthInt32 ( max 20 ( iScsiNameBytes.Length + 1 ) ) 4
        let v = Array.zeroCreate<byte> additionalLength
        Array.blit iScsiNameBytes 0 v 0 iScsiNameBytes.Length
        [|
            yield ( Functions.SetBitflag isid.IsSome 0x40uy ) ||| 0x05uy;
            yield 0x00uy;
            yield! Functions.UInt16ToNetworkBytes_NewVec ( uint16 v.Length )
            yield! v
        |]


    /// <summary>
    ///  Convert the byte array to the report LUNs parameter data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by REPORT LUNS command.
    /// </param>
    /// <returns>
    ///  Converted report LUNs parameter data.
    /// </returns>
    static member ReportLUNs ( pv : PooledBuffer ) : struct( uint32 * LUN_T[] ) =
        if pv.Length < 4 then
            struct( 0u, [||] )
        else
            let luncount = Functions.NetworkBytesToUInt32_InPooledBuffer pv 0
            let cnt = ( pv.Length - 4 ) / 8
            let rv = Array.zeroCreate<LUN_T> cnt
            for i = 0 to cnt - 1 do
                rv.[i] <-
                    Functions.NetworkBytesToUInt64_InPooledBuffer pv ( i * 8 + 4 )
                    |> lun_me.fromPrim
            struct( luncount, rv )

    /// <summary>
    ///  Convert the byte array to the read capacity 10 parameter data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by READ CAPACITY(10) command.
    /// </param>
    /// <returns>
    ///  Converted read capacity 10 parameter data.
    /// </returns>
    static member ReadCapacity10 ( pv : PooledBuffer ) : struct( uint32 * uint32 ) =
        if pv.Length < 8 then
            struct( 0u, 0u )
        else
            let a = Functions.NetworkBytesToUInt32_InPooledBuffer pv 0
            let b = Functions.NetworkBytesToUInt32_InPooledBuffer pv 4
            struct( a, b )

    /// <summary>
    ///  Convert the byte array to the read capacity 16 parameter data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by READ CAPACITY(16) command.
    /// </param>
    /// <returns>
    ///  Converted read capacity 16 parameter data.
    /// </returns>
    static member ReadCapacity16 ( pv : PooledBuffer ) : ReadCapacity16Param =
        {
            ReturnedLogicalUnitAddress =
                if pv.Length >= 8 then
                    Functions.NetworkBytesToUInt64_InPooledBuffer pv 0
                else
                    0UL;
            BlockLengthInBytes =
                if pv.Length >= 12 then
                    Functions.NetworkBytesToUInt32_InPooledBuffer pv 8
                else
                    0u;
            ReferenceTagOwnEnable =
                if pv.Length > 12 then
                    Functions.CheckBitflag pv.Array.[12] 0x02uy
                else
                    false;
            ProtectionEnable =
                if pv.Length > 12 then
                    Functions.CheckBitflag pv.Array.[12] 0x01uy
                else
                    false;
        }

    /// <summary>
    ///  Convert the byte array to all_commands format of the report supported operationn codes parameter data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by REPORT SUPPORTED OPERATION CODES command.
    /// </param>
    /// <returns>
    ///  Converted report supported operationn codes parameter data.
    /// </returns>
    static member ReportSupportedOperationCodes_AllCommand ( pv : PooledBuffer ) : SOCParam_All = 
        {
            CommandDataLength =
                if pv.Length >= 4 then
                    Functions.NetworkBytesToUInt32_InPooledBuffer pv 0 
                else
                    0u;
            Descs =
                [|
                    let cnt = ( pv.Length - 4 ) / 8
                    for i = 0 to cnt - 1 do
                        let idx = 4 + i * 8 
                        yield {
                            OperationCode = pv.Array.[ idx + 0 ];
                            ServiceAction = Functions.NetworkBytesToUInt16_InPooledBuffer pv ( idx + 2 );
                            ServiceActionValid = Functions.CheckBitflag pv.Array.[ idx + 5 ] 0x01uy;
                            CDBLength = Functions.NetworkBytesToUInt16_InPooledBuffer pv ( idx + 6 );
                        }
                |];
        }

    /// <summary>
    ///  Convert the byte array to one_command format of the report supported operationn codes parameter data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by REPORT SUPPORTED OPERATION CODES command.
    /// </param>
    /// <returns>
    ///  Converted report supported operationn codes parameter data.
    /// </returns>
    static member ReportSupportedOperationCodes_OneCommand ( pv : PooledBuffer ) : SOCParam_One = 
        {
            Support = if pv.Length > 1 then pv.[1] &&& 0x07uy else 0uy;
            CDBSize = 
                if pv.Length >= 4 then
                    Functions.NetworkBytesToUInt16_InPooledBuffer pv 2
                else
                    0us;
            CDBUsageData =
                if pv.Length >= 4 then
                    pv.GetPartialBytes 4 ( pv.Length - 1 )
                else
                    Array.Empty();
        }

    /// <summary>
    ///  Convert the byte array to the report supported task management functions parameter data.
    /// </summary>
    /// <param name="pv">
    ///  Received parameter data by REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS command.
    /// </param>
    /// <returns>
    ///  Converted report supported task management functions parameter data.
    /// </returns>
    static member ReportSupportedTaskManagementFunctions ( pv : PooledBuffer ) : RSTMFParam = 
        let b = if pv.Length > 0 then pv.Array.[0] else 0uy;
        {
            AbortTaskSupported = Functions.CheckBitflag b 0x80uy;
            AbortTaskSetSupported = Functions.CheckBitflag b 0x40uy;
            ClearACASupported = Functions.CheckBitflag b 0x20uy;
            ClearTaskSetSupported = Functions.CheckBitflag b 0x10uy;
            LUResetSupported = Functions.CheckBitflag b 0x08uy;
            QueryTaskSupported = Functions.CheckBitflag b 0x04uy;
            TargetResetSupported = Functions.CheckBitflag b 0x02uy;
            WakeupSupported = Functions.CheckBitflag b 0x01uy;
        }
