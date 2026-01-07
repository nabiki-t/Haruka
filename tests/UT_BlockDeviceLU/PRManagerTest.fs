//=============================================================================
// Haruka Software Storage.
// PRManagerTest.fs : Test cases for PRManager class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading.Tasks
open System.Collections.Immutable
open System.Collections.Generic
open System.Text

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.BlockDeviceLU
open Haruka.Test

//=============================================================================
// Class implementation

type PRManager_Test1 () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let cdesForIsBlockedByPersistentReservation : ( bool * bool * bool * bool * bool * ICDB )[] = 
        [|
            true,  true,  false, true,  true,  ( { OperationCode = 0xA4uy; ServiceAction = 0x0Bus; ParameterListLength = 0u; Control = 0uy; } : ChangeAliasesCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x83uy; ParameterListLength = 0u; Control = 0uy; } : ExtendedCopyCDB );
            false, false, false, false, false, ( { OperationCode = 0x12uy; EVPD = false; PageCode = 0uy; AllocationLength = 0us; Control = 0uy; } : InquiryCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x4Cuy; PCR = false; SP = false; PC = 0uy; ParameterListLength = 0us; Control = 0uy; } : LogSelectCDB );
            false, false, false, false, false, ( { OperationCode = 0x4Duy; PPC = false; SP = false; PC = 0uy; ParameterPointer = 0us; AllocationLength = 0us; Control = 0uy; } : LogSenseCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x15uy; PF = false; SP = false; ParameterListLength = 0us; Control = 0uy; } : ModeSelectCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x55uy; PF = false; SP = false; ParameterListLength = 0us; Control = 0uy; } : ModeSelectCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x1Auy; LLBAA = false; DBD = false; PC = 0uy; PageCode = 0uy; SubPageCode = 0uy; AllocationLength = 0us; Control = 0uy; } : ModeSenseCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x15uy; LLBAA = false; DBD = false; PC = 0uy; PageCode = 0uy; SubPageCode = 0uy; AllocationLength = 0us; Control = 0uy; } : ModeSenseCDB );
            false, false, false, false, false, ( { OperationCode = 0x5Euy; ServiceAction = 0uy; AllocationLength = 0us; Control = 0uy; } : PersistentReserveInCDB );
            false, false, false, false, false, ( { OperationCode = 0x1Euy; Prevent = 0uy; Control = 0uy; } : PreventAllowMediumRemovalCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x1Euy; Prevent = 1uy; Control = 0uy; } : PreventAllowMediumRemovalCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x8Cuy; ServiceAction = 0uy; VolumeNumber = 0uy; PartitionNumber = 0uy; FirstAttributeIdentifier = 0us; AllocationLength = 0u; Control = 0uy; } : ReadAttributeCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x8Cuy; Mode = 0uy; BufferID = 0uy; BufferOffset = 0u; AllocationLength = 0u; Control = 0uy; } : ReadBufferCDB );
            false, false, false, false, false, ( { OperationCode = 0xABuy; ServiceAction = 0uy; AllocationLength = 0u; Control = 0uy; } : ReadMediaSerialNumberCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x84uy; ServiceAction = 0uy; ListIdentifier = 0uy; AllocationLength = 0u; Control = 0uy; } : ReceiveCopyResultsCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x1Cuy; PageCode = 0uy; AllocationLength = 0us; Control = 0uy; } : ReceiveDiagnosticResultsCDB );
            false, false, false, false, false, ( { OperationCode = 0xA3uy; ServiceAction = 0x0Buy; AllocationLength = 0u; Control = 0uy; } : ReportAliasesCDB );
            false, false, false, false, false, ( { OperationCode = 0xA3uy; ServiceAction = 0x05uy; AllocationLength = 0u; Control = 0uy; } : ReportDeviceIdentifierCDB );
            false, false, false, false, false, ( { OperationCode = 0xA0uy; SelectReport = 0uy; AllocationLength = 0u; Control = 0uy; } : ReportLUNsCDB );
            false, false, false, false, false, ( { OperationCode = 0xA3uy; ServiceAction = 0x0Euy; PriorityReported = 0uy; AllocationLength = 0u; Control = 0uy; } : ReportPriorityCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0xA3uy; ServiceAction = 0x0Cuy; ReportingOptions = 0uy; RequestedOperationCode = 0uy; RequestedServiceAction = 0us; AllocationLength = 0u; Control = 0uy; } : ReportSupportedOperationCodesCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0xA3uy; ServiceAction = 0x0Duy; AllocationLength = 0u; Control = 0uy; } : ReportSupportedTaskManagementFunctionsCDB );
            false, false, false, false, false, ( { OperationCode = 0xA3uy; ServiceAction = 0x0Auy; AllocationLength = 0u; Control = 0uy; } : ReportTargetPortGroupsCDB );
            false, false, false, false, false, ( { OperationCode = 0xA3uy; ServiceAction = 0x0Fuy; AllocationLength = 0u; Control = 0uy; } : ReportTimestampCDB );
            false, false, false, false, false, ( { OperationCode = 0x03uy; DESC = false; AllocationLength = 0uy; Control = 0uy; } : RequestSenseCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x1Duy; SelfTestCode = 0uy; PF = false; SelfTest = false; DevOffL = false; UnitOffL = false; ParameterListLength = 0us; Control = 0uy; } : SendDiagnosticCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0xA4uy; ServiceAction = 0x06uy; ParameterListLength = 0u; Control = 0uy; } : SetDeviceIdentifierCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0xA4uy; ServiceAction = 0x0Euy; I_T_LNexusToSet = 0uy; ParameterListLength = 0u; Control = 0uy; } : SetPriorityCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0xA4uy; ServiceAction = 0x0Auy; ParameterListLength = 0u; Control = 0uy; } : SetTargetPortGroupsCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0xA4uy; ServiceAction = 0x0Fuy; ParameterListLength = 0u; Control = 0uy; } : SetTimestampCDB );
            false, false, false, false, false, ( { OperationCode = 0x00uy; Control = 0uy; } : TestUnitReadyCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x80uy; VolumeNumber = 0uy; PartitionNumber = 0uy; ParameterListLength = 0u; Control = 0uy; } : WriteAttributeCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x3Buy; Mode = 0uy; BufferID = 0uy; BufferOffset = 0u; ParameterListLength = 0u; Control = 0uy; } : WriteBufferCDB );
            false, false, false, false, false, ( { OperationCode = 0x86uy; ServiceAction = 0x00uy; ManagementIdentifierKey = 0UL; AllocationLength = 0u; Control = 0uy; } : AccessControlIn_ReportAclCDB );
            false, false, false, false, false, ( { OperationCode = 0x86uy; ServiceAction = 0x01uy; ManagementIdentifierKey = 0UL; AllocationLength = 0u; Control = 0uy; } : AccessControlIn_ReportLUDescriptorsCDB );
            false, false, false, false, false, ( { OperationCode = 0x86uy; ServiceAction = 0x02uy; ManagementIdentifierKey = 0UL; LogPortion = 0uy; AllocationLength = 0us; Control = 0uy; } : AccessControlIn_ReportAccessControlsLogCDB );
            false, false, false, false, false, ( { OperationCode = 0x86uy; ServiceAction = 0x03uy; ManagementIdentifierKey = 0UL; AllocationLength = 0u; Control = 0uy; } : AccessControlIn_ReportOverrideLockoutTimerCDB );
            false, false, false, false, false, ( { OperationCode = 0x86uy; ServiceAction = 0x04uy; LUNValue = lun_me.zero; AllocationLength = 0u; Control = 0uy; } : AccessControlIn_RequestProxyTokenCDB );
            false, false, false, false, false, ( { OperationCode = 0x87uy; ServiceAction = 0x00uy; ParameterListLength = 0u; Control = 0uy; } : AccessControlOutCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x04uy; FMTPINFO = false; RTO_REQ = false; LONGLIST = false; FMTDATA = false; CMPLIST = false; DefectListFormat = 0uy; Control = 0uy; } : FormatUnitCDB );
            false, true,  false, false, true,  ( { OperationCode = 0x34uy; IMMED = false; LogicalBlockAddress = blkcnt_me.zero64; PrefetchLength = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : PreFetchCDB );
            false, true,  false, false, true,  ( { OperationCode = 0x90uy; IMMED = false; LogicalBlockAddress = blkcnt_me.zero64; PrefetchLength = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : PreFetchCDB );
            false, true,  false, false, true,  ( { OperationCode = 0x08uy; RdProtect = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : ReadCDB );
            false, true,  false, false, true,  ( { OperationCode = 0x28uy; RdProtect = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : ReadCDB );
            false, true,  false, false, true,  ( { OperationCode = 0xA8uy; RdProtect = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : ReadCDB );
            false, true,  false, false, true,  ( { OperationCode = 0x88uy; RdProtect = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : ReadCDB );
            false, true,  false, false, true,  ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x0009us; RDPROTECT = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; ExpectedInitialLogicalBlockReferenceTag = 0u; ExpectedLogicalBlockApplicationTag = 0us; LogicalBlockApplicationTagMask = 0us; TransferLength = blkcnt_me.zero32; } : Read32CDB );
            false, false, false, false, false, ( { OperationCode = 0x25uy; ServiceAction = 0x00uy; LogicalBlockAddress = blkcnt_me.zero64; PMI = false; AllocationLength = 0u; Control = 0uy; } : ReadCapacityCDB );
            false, false, false, false, false, ( { OperationCode = 0x9Euy; ServiceAction = 0x10uy; LogicalBlockAddress = blkcnt_me.zero64; PMI = false; AllocationLength = 0u; Control = 0uy; } : ReadCapacityCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x37uy; REQ_PLIST = false; REQ_GLIST = false; DefectListFormat = 0uy; AllocationLength = 0u; Control = 0uy; } : ReadDefectDataCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0xB7uy; REQ_PLIST = false; REQ_GLIST = false; DefectListFormat = 0uy; AllocationLength = 0u; Control = 0uy; } : ReadDefectDataCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x3Euy; ServiceAction = 0x00uy; LogicalBlockAddress = blkcnt_me.zero64; ByteTransferLength = 0us; CORRT = false; Control = 0uy; } : ReadLongCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x9Euy; ServiceAction = 0x11uy; LogicalBlockAddress = blkcnt_me.zero64; ByteTransferLength = 0us; CORRT = false; Control = 0uy; } : ReadLongCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x07uy; LONGLBA = false; LONGLIST = false; Control = 0uy; } : ReassignBlocksCDB );
            false, false, false, false, false, ( { OperationCode = 0x1Buy; IMMED = false; PowerCondition = 0uy; LOEJ = false; Start = true; Control = 0uy; } : StartStopUnitCDB );
            true,  true,  false, true,  true , ( { OperationCode = 0x1Buy; IMMED = false; PowerCondition = 0uy; LOEJ = false; Start = false; Control = 0uy; } : StartStopUnitCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x35uy; SyncNV = false; IMMED = false; LogicalBlockAddress = blkcnt_me.zero64; NumberOfBlocks = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : SynchronizeCacheCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x91uy; SyncNV = false; IMMED = false; LogicalBlockAddress = blkcnt_me.zero64; NumberOfBlocks = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : SynchronizeCacheCDB );
            false, true,  false, false, true,  ( { OperationCode = 0x2Fuy; VRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; VerificationLength = blkcnt_me.zero32; Control = 0uy; } : VerifyCDB );
            false, true,  false, false, true,  ( { OperationCode = 0xAFuy; VRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; VerificationLength = blkcnt_me.zero32; Control = 0uy; } : VerifyCDB );
            false, true,  false, false, true,  ( { OperationCode = 0x8Fuy; VRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; VerificationLength = blkcnt_me.zero32; Control = 0uy; } : VerifyCDB );
            false, true,  false, false, true,  ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x000Aus; VRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; ExpectedInitialLogicalBlockReferenceTag = 0u; ExpectedLogicalBlockApplicationTag = 0us; LogicalBlockApplicationTagMask = 0us; VerificationLength = blkcnt_me.zero32; } : Verify32CDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x0Auy; WRPROTECT = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x2Auy; WRPROTECT = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0xAAuy; WRPROTECT = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x8Auy; WRPROTECT = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x000Bus; WRPROTECT = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; ExpectedInitialLogicalBlockReferenceTag = 0u; ExpectedLogicalBlockApplicationTag = 0us; LogicalBlockApplicationTagMask = 0us; TransferLength = blkcnt_me.zero32; } : Write32CDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x2Euy; WRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteAndVerifyCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0xAEuy; WRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteAndVerifyCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x8Euy; WRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteAndVerifyCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x000Cus; WRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; ExpectedInitialLogicalBlockReferenceTag = 0u; ExpectedLogicalBlockApplicationTag = 0us; LogicalBlockApplicationTagMask = 0us; TransferLength = blkcnt_me.zero32; } : WriteAndVerify32CDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x3Fuy; ServiceAction = 0x00uy; LogicalBlockAddress = blkcnt_me.zero64; ByteTransferLength = 0us; Control = 0uy; } : WriteLongCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x9Fuy; ServiceAction = 0x11uy; LogicalBlockAddress = blkcnt_me.zero64; ByteTransferLength = 0us; Control = 0uy; } : WriteLongCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x41uy; WRPROTECT = 0uy; PBDATA = false; LBDATA = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; NumberOfBlocks = blkcnt_me.zero32; Control = 0uy; } : WriteSameCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x93uy; WRPROTECT = 0uy; PBDATA = false; LBDATA = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; NumberOfBlocks = blkcnt_me.zero32; Control = 0uy; } : WriteSameCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x000Dus; WRPROTECT = 0uy; PBDATA = false; LBDATA = false; LogicalBlockAddress = blkcnt_me.zero64; ExpectedInitialLogicalBlockReferenceTag = 0u; ExpectedLogicalBlockApplicationTag = 0us; LogicalBlockApplicationTagMask = 0us; NumberOfBlocks = blkcnt_me.zero32; } : WriteSame32CDB );
            false, true,  false, false, true,  ( { OperationCode = 0x52uy; XORPINFO = false; LogicalBlockAddress = blkcnt_me.zero32; GroupNumber = 0uy; TransferLength = blkcnt_me.zero16; Control = 0uy; } : XDReadCDB );
            false, true,  false, false, true,  ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x0003us; XORPINFO = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; } : XDRead32CDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x50uy; WRPROTECT = 0uy; DPO = false; FUA = false; DisableWrite = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero32; GroupNumber = 0uy; TransferLength = blkcnt_me.zero16; Control = 0uy; } : XDWriteCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x0004us; WRPROTECT = 0uy; DPO = false; FUA = false; DisableWrite = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; } : XDWrite32CDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x53uy; WRPROTECT = 0uy; DPO = false; FUA = false; DisableWrite = false; FUA_NV = false; XORPINFO = false; LogicalBlockAddress = blkcnt_me.zero32; GroupNumber = 0uy; TransferLength = blkcnt_me.zero16; Control = 0uy; } : XDWriteReadCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x0007us; WRPROTECT = 0uy; DPO = false; FUA = false; DisableWrite = false; FUA_NV = false; XORPINFO = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; } : XDWriteRead32CDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x51uy; DPO = false; FUA = false; FUA_NV = false; XORPINFO = false; LogicalBlockAddress = blkcnt_me.zero32; GroupNumber = 0uy; TransferLength = blkcnt_me.zero16; Control = 0uy; } : XPWriteCDB );
            true,  true,  false, true,  true,  ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x0006us; DPO = false; FUA = false; FUA_NV = false; XORPINFO = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; } : XPWrite32CDB );
        |]

    let prOutCdesForIsBlockedByPersistentReservation : ( bool * bool * ICDB )[] = [|
        false, true,  ( { OperationCode = 0x5fuy; ServiceAction = 0x03uy; Scope = 0x00uy; PRType = PR_TYPE.WRITE_EXCLUSIVE; ParameterListLength = 0x00u; Control = 0x00uy; } : PersistentReserveOutCDB );
        false, true,  ( { OperationCode = 0x5fuy; ServiceAction = 0x04uy; Scope = 0x00uy; PRType = PR_TYPE.WRITE_EXCLUSIVE; ParameterListLength = 0x00u; Control = 0x00uy; } : PersistentReserveOutCDB );
        false, true,  ( { OperationCode = 0x5fuy; ServiceAction = 0x05uy; Scope = 0x00uy; PRType = PR_TYPE.WRITE_EXCLUSIVE; ParameterListLength = 0x00u; Control = 0x00uy; } : PersistentReserveOutCDB );
        false, false, ( { OperationCode = 0x5fuy; ServiceAction = 0x00uy; Scope = 0x00uy; PRType = PR_TYPE.WRITE_EXCLUSIVE; ParameterListLength = 0x00u; Control = 0x00uy; } : PersistentReserveOutCDB );
        false, false, ( { OperationCode = 0x5fuy; ServiceAction = 0x06uy; Scope = 0x00uy; PRType = PR_TYPE.WRITE_EXCLUSIVE; ParameterListLength = 0x00u; Control = 0x00uy; } : PersistentReserveOutCDB );
        true,  true,  ( { OperationCode = 0x5fuy; ServiceAction = 0x07uy; Scope = 0x00uy; PRType = PR_TYPE.WRITE_EXCLUSIVE; ParameterListLength = 0x00u; Control = 0x00uy; } : PersistentReserveOutCDB );
        false, true,  ( { OperationCode = 0x5fuy; ServiceAction = 0x02uy; Scope = 0x00uy; PRType = PR_TYPE.WRITE_EXCLUSIVE; ParameterListLength = 0x00u; Control = 0x00uy; } : PersistentReserveOutCDB );
        true,  true,  ( { OperationCode = 0x5fuy; ServiceAction = 0x01uy; Scope = 0x00uy; PRType = PR_TYPE.WRITE_EXCLUSIVE; ParameterListLength = 0x00u; Control = 0x00uy; } : PersistentReserveOutCDB );
    |]

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    static member defaultSource =  {
        I_TNexus = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        CID = cid_me.zero;
        ConCounter = concnt_me.zero;
        TSIH = tsih_me.zero;
        ProtocolService = new CProtocolService_Stub();
        SessionKiller = new HKiller()
    }

    static member defaultTaskObj ( cdb : ICDB ) ( pm : PRManager ) =
        new ScsiTask(
            new CStatus_Stub(),
            PRManager_Test1.defaultSource,
            {
                I = false;
                F = false;
                R = false;
                W = false;
                ATTR = TaskATTRCd.SIMPLE_TASK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                ExpectedDataTransferLength = 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                ScsiCDB = Array.empty;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
                ByteCount = 0u;
            },
            cdb,
            [],
            new CInternalLU_Stub(),
            new CMedia_Stub(),
            new ModeParameter(
                new CMedia_Stub(),
                lun_me.zero
            ),
            pm,
            false
        )

    member _.CreateTestDir() =
        let w1 = Functions.AppendPathName ( Path.GetTempPath() ) "PRManager_Test1"
        GlbFunc.CreateDir w1 |> ignore
        w1

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member this.LoadPRFile_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_001.txt"
        GlbFunc.DeleteFile fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_002.txt"

        File.WriteAllText( fname, "" )
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_003.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, false;
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 70UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_NO_RESERVATION_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_NO_RESERVATION_001.txt"

        GlbFunc.writeDefaultPRFile NO_RESERVATION Array.empty fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_NO_RESERVATION_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_NO_RESERVATION_002.txt"

        GlbFunc.writeDefaultPRFile
            NO_RESERVATION
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration = 0u ))
        Assert.True(( r.m_Registrations.Count = 1 ))
        let itn = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        Assert.True(( r.m_Registrations.Item( itn ) = resvkey_me.fromPrim 7UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_NO_RESERVATION_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_NO_RESERVATION_003.txt"

        GlbFunc.writeDefaultPRFile
            NO_RESERVATION
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, false;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 2 ))
        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us )
        Assert.True(( r.m_Registrations.Item( itn1 ) = resvkey_me.fromPrim 7UL ))
        Assert.True(( r.m_Registrations.Item( itn2 ) = resvkey_me.fromPrim 70UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_NO_RESERVATION_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_NO_RESERVATION_004.txt" 

        GlbFunc.writeDefaultPRFile
            NO_RESERVATION
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_001.txt" 
        
        GlbFunc.writeDefaultPRFile WRITE_EXCLUSIVE Array.empty fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_002() =

        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_002.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_003() =

        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_003.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        let itn = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        Assert.True(( r.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( r.m_Holder.IsSome ))
        Assert.True(( r.m_Holder.Value = itn ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 1 ))
        Assert.True(( r.m_Registrations.Item( itn ) = resvkey_me.fromPrim 7UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_004.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us )
        Assert.True(( r.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( r.m_Holder.IsSome ))
        Assert.True(( r.m_Holder.Value = itn1 ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 2 ))
        Assert.True(( r.m_Registrations.Item( itn1 ) = resvkey_me.fromPrim 7UL ))
        Assert.True(( r.m_Registrations.Item( itn2 ) = resvkey_me.fromPrim 70UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_005.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, true;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_001.txt" 

        GlbFunc.writeDefaultPRFile EXCLUSIVE_ACCESS Array.empty fname
        File.WriteAllText( fname, "
            <Haruka>
              <Ver100>
                <Reservation>
                  <Type>EXCLUSIVE_ACCESS</Type>
                </Reservation>
                <Registrations />
              </Ver100>
            </Haruka>
        " )
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_002.txt" 

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_003.txt" 

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        let itn = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        Assert.True(( r.m_Type = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( r.m_Holder.IsSome ))
        Assert.True(( r.m_Holder.Value = itn ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 1 ))
        Assert.True(( r.m_Registrations.Item( itn ) = resvkey_me.fromPrim 7UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_004.txt" 

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us )
        Assert.True(( r.m_Type = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( r.m_Holder.IsSome ))
        Assert.True(( r.m_Holder.Value = itn1 ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 2 ))
        Assert.True(( r.m_Registrations.Item( itn1 ) = resvkey_me.fromPrim 7UL ))
        Assert.True(( r.m_Registrations.Item( itn2 ) = resvkey_me.fromPrim 70UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_005.txt" 

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, true;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_001.txt" 

        GlbFunc.writeDefaultPRFile WRITE_EXCLUSIVE_REGISTRANTS_ONLY Array.empty fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_002.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_003.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        let itn = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        Assert.True(( r.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( r.m_Holder.IsSome ))
        Assert.True(( r.m_Holder.Value = itn ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 1 ))
        Assert.True(( r.m_Registrations.Item( itn ) = resvkey_me.fromPrim 7UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_004.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us )
        Assert.True(( r.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( r.m_Holder.IsSome ))
        Assert.True(( r.m_Holder.Value = itn1 ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 2 ))
        Assert.True(( r.m_Registrations.Item( itn1 ) = resvkey_me.fromPrim 7UL ))
        Assert.True(( r.m_Registrations.Item( itn2 ) = resvkey_me.fromPrim 70UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_005.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, true;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_ALL_REGISTRANTS_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_ALL_REGISTRANTS_001.txt" 

        GlbFunc.writeDefaultPRFile WRITE_EXCLUSIVE_ALL_REGISTRANTS Array.empty fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_ALL_REGISTRANTS_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_ALL_REGISTRANTS_002.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        let itn = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        Assert.True(( r.m_Type = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 1 ))
        Assert.True(( r.m_Registrations.Item( itn ) = resvkey_me.fromPrim 7UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_ALL_REGISTRANTS_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_ALL_REGISTRANTS_003.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_ALL_REGISTRANTS_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_ALL_REGISTRANTS_004.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_ALL_REGISTRANTS_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_ALL_REGISTRANTS_005.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, true;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_WRITE_EXCLUSIVE_ALL_REGISTRANTS_006() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_WRITE_EXCLUSIVE_ALL_REGISTRANTS_006.txt" 

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, false;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us )
        Assert.True(( r.m_Type = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 2 ))
        Assert.True(( r.m_Registrations.Item( itn1 ) = resvkey_me.fromPrim 7UL ))
        Assert.True(( r.m_Registrations.Item( itn2 ) = resvkey_me.fromPrim 70UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_001.txt" 

        GlbFunc.writeDefaultPRFile EXCLUSIVE_ACCESS_REGISTRANTS_ONLY Array.empty fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_002.txt" 

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_003.txt" 

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        let itn = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        Assert.True(( r.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( r.m_Holder.IsSome ))
        Assert.True(( r.m_Holder.Value = itn ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 1 ))
        Assert.True(( r.m_Registrations.Item( itn ) = resvkey_me.fromPrim 7UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_004.txt" 

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us )
        Assert.True(( r.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( r.m_Holder.IsSome ))
        Assert.True(( r.m_Holder.Value = itn1 ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 2 ))
        Assert.True(( r.m_Registrations.Item( itn1 ) = resvkey_me.fromPrim 7UL ))
        Assert.True(( r.m_Registrations.Item( itn2 ) = resvkey_me.fromPrim 70UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_005.txt" 

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, true;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_001.txt" 

        GlbFunc.writeDefaultPRFile EXCLUSIVE_ACCESS_ALL_REGISTRANTS Array.empty fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_002.txt" 

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        let itn = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        Assert.True(( r.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 1 ))
        Assert.True(( r.m_Registrations.Item( itn ) = resvkey_me.fromPrim 7UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_003.txt" 

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_004.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_005.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, true;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.LoadPRFile_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_006() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "LoadPRFile_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_006.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, false;
                new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us ), resvkey_me.fromPrim 70UL, false;
            |]
            fname
        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us )
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 3uy <<< 6 ) 63uy 65535us 255uy 65535us, "target002", tpgt_me.fromPrim 65535us )
        Assert.True(( r.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 2 ))
        Assert.True(( r.m_Registrations.Item( itn1 ) = resvkey_me.fromPrim 7UL ))
        Assert.True(( r.m_Registrations.Item( itn2 ) = resvkey_me.fromPrim 70UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.SavePRFile_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "SavePRFile_001.txt"

        let pr = {
            m_Type = PR_TYPE.NO_RESERVATION;
            m_Holder = None;
            m_PRGeneration = 0u;
            m_Registrations = ImmutableDictionary< ITNexus, RESVKEY_T >.Empty;
        }

        PrivateCaller.Invoke< PRManager >( "SavePRFile", objidx_me.NewID(), lun_me.zero, fname, pr, true ) :?> Task
        |> Functions.RunTaskSynchronously

        Assert.True( File.Exists fname )

        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 0 ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.SavePRFile_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "SavePRFile_002.txt"

        let pr = {
            m_Type = PR_TYPE.NO_RESERVATION;
            m_Holder = None;
            m_PRGeneration = 0u;
            m_Registrations = ImmutableDictionary< ITNexus, RESVKEY_T >.Empty;
        }
        File.WriteAllText( fname, "" )

        PrivateCaller.Invoke< PRManager >( "SavePRFile", objidx_me.NewID(), lun_me.zero, fname, pr, false ) :?> Task
        |> Functions.RunTaskSynchronously

        Assert.False( File.Exists fname )

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.SavePRFile_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "SavePRFile_003.txt"

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 2uy <<< 6 ) 2uy 2us 2uy 2us, "target002", tpgt_me.fromPrim 2us );
        let itn3 = new ITNexus( "initiator003", isid_me.fromElem ( 3uy <<< 6 ) 3uy 3us 3uy 3us, "target003", tpgt_me.fromPrim 3us );
        let itn4 = new ITNexus( "initiator004", isid_me.fromElem ( 0uy <<< 6 ) 4uy 4us 4uy 4us, "target004", tpgt_me.fromPrim 4us );
        let pr = {
            m_Type = PR_TYPE.NO_RESERVATION;
            m_Holder = None;
            m_PRGeneration = 1u;
            m_Registrations = 
                let r = new Dictionary< ITNexus, RESVKEY_T >();
                r.Add( itn1, resvkey_me.fromPrim 1UL )
                r.Add( itn2, resvkey_me.fromPrim 2UL )
                r.Add( itn3, resvkey_me.fromPrim 3UL )
                r.Add( itn4, resvkey_me.fromPrim 4UL )
                r.ToImmutableDictionary()
        }

        PrivateCaller.Invoke< PRManager >( "SavePRFile", objidx_me.NewID(), lun_me.zero, fname, pr, true ) :?> Task
        |> Functions.RunTaskSynchronously

        Assert.True( File.Exists fname )

        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 4 ))
        Assert.True(( r.m_Registrations.Item itn1 = resvkey_me.fromPrim 1UL ))
        Assert.True(( r.m_Registrations.Item itn2 = resvkey_me.fromPrim 2UL ))
        Assert.True(( r.m_Registrations.Item itn3 = resvkey_me.fromPrim 3UL ))
        Assert.True(( r.m_Registrations.Item itn4 = resvkey_me.fromPrim 4UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.SavePRFile_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "SavePRFile_004.txt"

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 2uy <<< 6 ) 2uy 2us 2uy 2us, "target002", tpgt_me.fromPrim 2us );
        let itn3 = new ITNexus( "initiator003", isid_me.fromElem ( 3uy <<< 6 ) 3uy 3us 3uy 3us, "target003", tpgt_me.fromPrim 3us );
        let itn4 = new ITNexus( "initiator004", isid_me.fromElem ( 0uy <<< 6 ) 4uy 4us 4uy 4us, "target004", tpgt_me.fromPrim 4us );
        let itn5 = new ITNexus( "initiator005", isid_me.fromElem ( 1uy <<< 6 ) 5uy 5us 5uy 5us, "target005", tpgt_me.fromPrim 5us );
        let pr = {
            m_Type = PR_TYPE.WRITE_EXCLUSIVE;
            m_Holder = Some itn2;
            m_PRGeneration = 1u;
            m_Registrations = 
                let r = new Dictionary< ITNexus, RESVKEY_T >();
                r.Add( itn1, resvkey_me.fromPrim 1UL )
                r.Add( itn2, resvkey_me.fromPrim 2UL )
                r.Add( itn3, resvkey_me.fromPrim 3UL )
                r.Add( itn4, resvkey_me.fromPrim 4UL )
                r.Add( itn5, resvkey_me.fromPrim 5UL )
                r.ToImmutableDictionary()
        }

        PrivateCaller.Invoke< PRManager >( "SavePRFile", objidx_me.NewID(), lun_me.zero, fname, pr, true ) :?> Task
        |> Functions.RunTaskSynchronously

        Assert.True( File.Exists fname )

        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( r.m_Holder.IsSome ))
        Assert.True(( r.m_Holder.Value = itn2 ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 5 ))
        Assert.True(( r.m_Registrations.Item itn1 = resvkey_me.fromPrim 1UL ))
        Assert.True(( r.m_Registrations.Item itn2 = resvkey_me.fromPrim 2UL ))
        Assert.True(( r.m_Registrations.Item itn3 = resvkey_me.fromPrim 3UL ))
        Assert.True(( r.m_Registrations.Item itn4 = resvkey_me.fromPrim 4UL ))
        Assert.True(( r.m_Registrations.Item itn5 = resvkey_me.fromPrim 5UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.SavePRFile_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "SavePRFile_005.txt"

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 2uy <<< 6 ) 2uy 2us 2uy 2us, "target002", tpgt_me.fromPrim 2us );
        let itn3 = new ITNexus( "initiator003", isid_me.fromElem ( 3uy <<< 6 ) 3uy 3us 3uy 3us, "target003", tpgt_me.fromPrim 3us );
        let itn4 = new ITNexus( "initiator004", isid_me.fromElem ( 0uy <<< 6 ) 4uy 4us 4uy 4us, "target004", tpgt_me.fromPrim 4us );
        let itn5 = new ITNexus( "initiator005", isid_me.fromElem ( 1uy <<< 6 ) 5uy 5us 5uy 5us, "target005", tpgt_me.fromPrim 5us );
        let itn6 = new ITNexus( "initiator006", isid_me.fromElem ( 2uy <<< 6 ) 6uy 6us 6uy 6us, "target006", tpgt_me.fromPrim 6us );
        let pr = {
            m_Type = PR_TYPE.EXCLUSIVE_ACCESS;
            m_Holder = Some itn3;
            m_PRGeneration = 1u;
            m_Registrations = 
                let r = new Dictionary< ITNexus, RESVKEY_T >();
                r.Add( itn1, resvkey_me.fromPrim 1UL )
                r.Add( itn2, resvkey_me.fromPrim 2UL )
                r.Add( itn3, resvkey_me.fromPrim 3UL )
                r.Add( itn4, resvkey_me.fromPrim 4UL )
                r.Add( itn5, resvkey_me.fromPrim 5UL )
                r.Add( itn6, resvkey_me.fromPrim 6UL )
                r.ToImmutableDictionary()
        }

        PrivateCaller.Invoke< PRManager >( "SavePRFile", objidx_me.NewID(), lun_me.zero, fname, pr, true ) :?> Task
        |> Functions.RunTaskSynchronously

        Assert.True( File.Exists fname )

        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( r.m_Holder.IsSome ))
        Assert.True(( r.m_Holder.Value = itn3 ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 6 ))
        Assert.True(( r.m_Registrations.Item itn1 = resvkey_me.fromPrim 1UL ))
        Assert.True(( r.m_Registrations.Item itn2 = resvkey_me.fromPrim 2UL ))
        Assert.True(( r.m_Registrations.Item itn3 = resvkey_me.fromPrim 3UL ))
        Assert.True(( r.m_Registrations.Item itn4 = resvkey_me.fromPrim 4UL ))
        Assert.True(( r.m_Registrations.Item itn5 = resvkey_me.fromPrim 5UL ))
        Assert.True(( r.m_Registrations.Item itn6 = resvkey_me.fromPrim 6UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.SavePRFile_006() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "SavePRFile_006.txt"

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 2uy <<< 6 ) 2uy 2us 2uy 2us, "target002", tpgt_me.fromPrim 2us );
        let itn3 = new ITNexus( "initiator003", isid_me.fromElem ( 3uy <<< 6 ) 3uy 3us 3uy 3us, "target003", tpgt_me.fromPrim 3us );
        let itn4 = new ITNexus( "initiator004", isid_me.fromElem ( 0uy <<< 6 ) 4uy 4us 4uy 4us, "target004", tpgt_me.fromPrim 4us );
        let itn5 = new ITNexus( "initiator005", isid_me.fromElem ( 1uy <<< 6 ) 5uy 5us 5uy 5us, "target005", tpgt_me.fromPrim 5us );
        let itn6 = new ITNexus( "initiator006", isid_me.fromElem ( 2uy <<< 6 ) 6uy 6us 6uy 6us, "target006", tpgt_me.fromPrim 6us );
        let itn7 = new ITNexus( "initiator007", isid_me.fromElem ( 3uy <<< 6 ) 7uy 7us 7uy 7us, "target007", tpgt_me.fromPrim 7us );
        let pr = {
            m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY;
            m_Holder = Some itn4;
            m_PRGeneration = 1u;
            m_Registrations = 
                let r = new Dictionary< ITNexus, RESVKEY_T >();
                r.Add( itn1, resvkey_me.fromPrim 1UL )
                r.Add( itn2, resvkey_me.fromPrim 2UL )
                r.Add( itn3, resvkey_me.fromPrim 3UL )
                r.Add( itn4, resvkey_me.fromPrim 4UL )
                r.Add( itn5, resvkey_me.fromPrim 5UL )
                r.Add( itn6, resvkey_me.fromPrim 6UL )
                r.Add( itn7, resvkey_me.fromPrim 7UL )
                r.ToImmutableDictionary()
        }

        PrivateCaller.Invoke< PRManager >( "SavePRFile", objidx_me.NewID(), lun_me.zero, fname, pr, true ) :?> Task
        |> Functions.RunTaskSynchronously

        Assert.True( File.Exists fname )

        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( r.m_Holder.IsSome ))
        Assert.True(( r.m_Holder.Value = itn4 ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 7 ))
        Assert.True(( r.m_Registrations.Item itn1 = resvkey_me.fromPrim 1UL ))
        Assert.True(( r.m_Registrations.Item itn2 = resvkey_me.fromPrim 2UL ))
        Assert.True(( r.m_Registrations.Item itn3 = resvkey_me.fromPrim 3UL ))
        Assert.True(( r.m_Registrations.Item itn4 = resvkey_me.fromPrim 4UL ))
        Assert.True(( r.m_Registrations.Item itn5 = resvkey_me.fromPrim 5UL ))
        Assert.True(( r.m_Registrations.Item itn6 = resvkey_me.fromPrim 6UL ))
        Assert.True(( r.m_Registrations.Item itn7 = resvkey_me.fromPrim 7UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.SavePRFile_007() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "SavePRFile_007.txt"
        
        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 2uy <<< 6 ) 2uy 2us 2uy 2us, "target002", tpgt_me.fromPrim 2us );
        let itn3 = new ITNexus( "initiator003", isid_me.fromElem ( 3uy <<< 6 ) 3uy 3us 3uy 3us, "target003", tpgt_me.fromPrim 3us );
        let itn4 = new ITNexus( "initiator004", isid_me.fromElem ( 0uy <<< 6 ) 4uy 4us 4uy 4us, "target004", tpgt_me.fromPrim 4us );
        let itn5 = new ITNexus( "initiator005", isid_me.fromElem ( 1uy <<< 6 ) 5uy 5us 5uy 5us, "target005", tpgt_me.fromPrim 5us );
        let itn6 = new ITNexus( "initiator006", isid_me.fromElem ( 2uy <<< 6 ) 6uy 6us 6uy 6us, "target006", tpgt_me.fromPrim 6us );
        let itn7 = new ITNexus( "initiator007", isid_me.fromElem ( 3uy <<< 6 ) 7uy 7us 7uy 7us, "target007", tpgt_me.fromPrim 7us );
        let itn8 = new ITNexus( "initiator008", isid_me.fromElem ( 0uy <<< 6 ) 8uy 8us 8uy 8us, "target008", tpgt_me.fromPrim 8us );
        let pr = {
            m_Type = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS;
            m_Holder = None;
            m_PRGeneration = 5u;
            m_Registrations = 
                let r = new Dictionary< ITNexus, RESVKEY_T >();
                r.Add( itn1, resvkey_me.fromPrim 1UL )
                r.Add( itn2, resvkey_me.fromPrim 2UL )
                r.Add( itn3, resvkey_me.fromPrim 3UL )
                r.Add( itn4, resvkey_me.fromPrim 4UL )
                r.Add( itn5, resvkey_me.fromPrim 5UL )
                r.Add( itn6, resvkey_me.fromPrim 6UL )
                r.Add( itn7, resvkey_me.fromPrim 7UL )
                r.Add( itn8, resvkey_me.fromPrim 8UL )
                r.ToImmutableDictionary()
        }

        PrivateCaller.Invoke< PRManager >( "SavePRFile", objidx_me.NewID(), lun_me.zero, fname, pr, true ) :?> Task
        |> Functions.RunTaskSynchronously

        Assert.True( File.Exists fname )

        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 8 ))
        Assert.True(( r.m_Registrations.Item itn1 = resvkey_me.fromPrim 1UL ))
        Assert.True(( r.m_Registrations.Item itn2 = resvkey_me.fromPrim 2UL ))
        Assert.True(( r.m_Registrations.Item itn3 = resvkey_me.fromPrim 3UL ))
        Assert.True(( r.m_Registrations.Item itn4 = resvkey_me.fromPrim 4UL ))
        Assert.True(( r.m_Registrations.Item itn5 = resvkey_me.fromPrim 5UL ))
        Assert.True(( r.m_Registrations.Item itn6 = resvkey_me.fromPrim 6UL ))
        Assert.True(( r.m_Registrations.Item itn7 = resvkey_me.fromPrim 7UL ))
        Assert.True(( r.m_Registrations.Item itn8 = resvkey_me.fromPrim 8UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.SavePRFile_008() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "SavePRFile_008.txt"
        
        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 2uy <<< 6 ) 2uy 2us 2uy 2us, "target002", tpgt_me.fromPrim 2us );
        let itn3 = new ITNexus( "initiator003", isid_me.fromElem ( 3uy <<< 6 ) 3uy 3us 3uy 3us, "target003", tpgt_me.fromPrim 3us );
        let itn4 = new ITNexus( "initiator004", isid_me.fromElem ( 0uy <<< 6 ) 4uy 4us 4uy 4us, "target004", tpgt_me.fromPrim 4us );
        let itn5 = new ITNexus( "initiator005", isid_me.fromElem ( 1uy <<< 6 ) 5uy 5us 5uy 5us, "target005", tpgt_me.fromPrim 5us );
        let itn6 = new ITNexus( "initiator006", isid_me.fromElem ( 2uy <<< 6 ) 6uy 6us 6uy 6us, "target006", tpgt_me.fromPrim 6us );
        let itn7 = new ITNexus( "initiator007", isid_me.fromElem ( 3uy <<< 6 ) 7uy 7us 7uy 7us, "target007", tpgt_me.fromPrim 7us );
        let itn8 = new ITNexus( "initiator008", isid_me.fromElem ( 0uy <<< 6 ) 8uy 8us 8uy 8us, "target008", tpgt_me.fromPrim 8us );
        let itn9 = new ITNexus( "initiator009", isid_me.fromElem ( 1uy <<< 6 ) 9uy 9us 9uy 9us, "target009", tpgt_me.fromPrim 9us );
        let pr = {
            m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY;
            m_Holder = Some itn8;
            m_PRGeneration = 5u;
            m_Registrations = 
                let r = new Dictionary< ITNexus, RESVKEY_T >();
                r.Add( itn1, resvkey_me.fromPrim 1UL )
                r.Add( itn2, resvkey_me.fromPrim 2UL )
                r.Add( itn3, resvkey_me.fromPrim 3UL )
                r.Add( itn4, resvkey_me.fromPrim 4UL )
                r.Add( itn5, resvkey_me.fromPrim 5UL )
                r.Add( itn6, resvkey_me.fromPrim 6UL )
                r.Add( itn7, resvkey_me.fromPrim 7UL )
                r.Add( itn8, resvkey_me.fromPrim 8UL )
                r.Add( itn9, resvkey_me.fromPrim 9UL )
                r.ToImmutableDictionary()
        }

        PrivateCaller.Invoke< PRManager >( "SavePRFile", objidx_me.NewID(), lun_me.zero, fname, pr, true ) :?> Task
        |> Functions.RunTaskSynchronously

        Assert.True( File.Exists fname )

        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( r.m_Holder.IsSome ))
        Assert.True(( r.m_Holder.Value = itn8 ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 9 ))
        Assert.True(( r.m_Registrations.Item itn1 = resvkey_me.fromPrim 1UL ))
        Assert.True(( r.m_Registrations.Item itn2 = resvkey_me.fromPrim 2UL ))
        Assert.True(( r.m_Registrations.Item itn3 = resvkey_me.fromPrim 3UL ))
        Assert.True(( r.m_Registrations.Item itn4 = resvkey_me.fromPrim 4UL ))
        Assert.True(( r.m_Registrations.Item itn5 = resvkey_me.fromPrim 5UL ))
        Assert.True(( r.m_Registrations.Item itn6 = resvkey_me.fromPrim 6UL ))
        Assert.True(( r.m_Registrations.Item itn7 = resvkey_me.fromPrim 7UL ))
        Assert.True(( r.m_Registrations.Item itn8 = resvkey_me.fromPrim 8UL ))
        Assert.True(( r.m_Registrations.Item itn9 = resvkey_me.fromPrim 9UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.SavePRFile_009() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "SavePRFile_009.txt"
        
        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let itn2 = new ITNexus( "initiator002", isid_me.fromElem ( 2uy <<< 6 ) 2uy 2us 2uy 2us, "target002", tpgt_me.fromPrim 2us );
        let itn3 = new ITNexus( "initiator003", isid_me.fromElem ( 3uy <<< 6 ) 3uy 3us 3uy 3us, "target003", tpgt_me.fromPrim 3us );
        let itn4 = new ITNexus( "initiator004", isid_me.fromElem ( 0uy <<< 6 ) 4uy 4us 4uy 4us, "target004", tpgt_me.fromPrim 4us );
        let itn5 = new ITNexus( "initiator005", isid_me.fromElem ( 1uy <<< 6 ) 5uy 5us 5uy 5us, "target005", tpgt_me.fromPrim 5us );
        let itn6 = new ITNexus( "initiator006", isid_me.fromElem ( 2uy <<< 6 ) 6uy 6us 6uy 6us, "target006", tpgt_me.fromPrim 6us );
        let itn7 = new ITNexus( "initiator007", isid_me.fromElem ( 3uy <<< 6 ) 7uy 7us 7uy 7us, "target007", tpgt_me.fromPrim 7us );
        let itn8 = new ITNexus( "initiator008", isid_me.fromElem ( 0uy <<< 6 ) 8uy 8us 8uy 8us, "target008", tpgt_me.fromPrim 8us );
        let itn9 = new ITNexus( "initiator009", isid_me.fromElem ( 1uy <<< 6 ) 9uy 9us 9uy 9us, "target009", tpgt_me.fromPrim 9us );
        let itn10 = new ITNexus( "initiator010", isid_me.fromElem ( 2uy <<< 6 ) 10uy 10us 10uy 10us, "target010", tpgt_me.fromPrim 10us );
        let pr = {
            m_Type = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS;
            m_Holder = None;
            m_PRGeneration = 5u;
            m_Registrations = 
                let r = new Dictionary< ITNexus, RESVKEY_T >();
                r.Add( itn1, resvkey_me.fromPrim 1UL )
                r.Add( itn2, resvkey_me.fromPrim 2UL )
                r.Add( itn3, resvkey_me.fromPrim 3UL )
                r.Add( itn4, resvkey_me.fromPrim 4UL )
                r.Add( itn5, resvkey_me.fromPrim 5UL )
                r.Add( itn6, resvkey_me.fromPrim 6UL )
                r.Add( itn7, resvkey_me.fromPrim 7UL )
                r.Add( itn8, resvkey_me.fromPrim 8UL )
                r.Add( itn9, resvkey_me.fromPrim 9UL )
                r.Add( itn10, resvkey_me.fromPrim 10UL )
                r.ToImmutableDictionary()
        }

        PrivateCaller.Invoke< PRManager >( "SavePRFile", objidx_me.NewID(), lun_me.zero, fname, pr, true ) :?> Task
        |> Functions.RunTaskSynchronously

        Assert.True( File.Exists fname )

        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 10 ))
        Assert.True(( r.m_Registrations.Item itn1 = resvkey_me.fromPrim 1UL ))
        Assert.True(( r.m_Registrations.Item itn2 = resvkey_me.fromPrim 2UL ))
        Assert.True(( r.m_Registrations.Item itn3 = resvkey_me.fromPrim 3UL ))
        Assert.True(( r.m_Registrations.Item itn4 = resvkey_me.fromPrim 4UL ))
        Assert.True(( r.m_Registrations.Item itn5 = resvkey_me.fromPrim 5UL ))
        Assert.True(( r.m_Registrations.Item itn6 = resvkey_me.fromPrim 6UL ))
        Assert.True(( r.m_Registrations.Item itn7 = resvkey_me.fromPrim 7UL ))
        Assert.True(( r.m_Registrations.Item itn8 = resvkey_me.fromPrim 8UL ))
        Assert.True(( r.m_Registrations.Item itn9 = resvkey_me.fromPrim 9UL ))
        Assert.True(( r.m_Registrations.Item itn10 = resvkey_me.fromPrim 10UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.SavePRFile_010() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "SavePRFile_010.txt"

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );

        let pr = {
            m_Type = PR_TYPE.NO_RESERVATION;
            m_Holder = None;
            m_PRGeneration = 0u;
            m_Registrations = 
                let r = new Dictionary< ITNexus, RESVKEY_T >();
                r.Add( itn1, resvkey_me.fromPrim 1UL )
                r.ToImmutableDictionary()
        }

        use s = new FileStream( fname, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 512, true )

        let t1 =
            PrivateCaller.Invoke< PRManager >( "SavePRFile", objidx_me.NewID(), lun_me.zero, fname, pr, true ) :?> Task;
        let t2 =
            task {
                do! Task.Delay 70
                s.Close()
                s.Dispose()
            }
        Task.WaitAll( t1, t2 )

        Assert.True( File.Exists fname )

        let r = PrivateCaller.Invoke< PRManager >( "LoadPRFile", objidx_me.NewID(), lun_me.zero, fname ) :?> PRInfoRec
        Assert.True(( r.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( r.m_Holder.IsNone ))
        Assert.True(( r.m_PRGeneration= 0u ))
        Assert.True(( r.m_Registrations.Count = 1 ))
        Assert.True(( r.m_Registrations.Item itn1 = resvkey_me.fromPrim 1UL ))

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.SavePRFile_011() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "SavePRFile_011.txt"

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )

        let pr = {
            m_Type = PR_TYPE.NO_RESERVATION;
            m_Holder = None;
            m_PRGeneration = 0u;
            m_Registrations = 
                let r = new Dictionary< ITNexus, RESVKEY_T >();
                r.Add( itn1, resvkey_me.fromPrim 1UL )
                r.ToImmutableDictionary()
        }

        use s = new FileStream( fname, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 512, true )

        PrivateCaller.Invoke< PRManager >( "SavePRFile", objidx_me.NewID(), lun_me.zero, fname, pr, true ) :?> Task
        |> Functions.RunTaskSynchronously

        s.Close()
        s.Dispose()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.SavePRFile_012() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "SavePRFile_012.txt"

        let itn1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );

        let pr = {
            m_Type = PR_TYPE.NO_RESERVATION;
            m_Holder = None;
            m_PRGeneration = 0u;
            m_Registrations = 
                let r = new Dictionary< ITNexus, RESVKEY_T >();
                r.Add( itn1, resvkey_me.fromPrim 1UL )
                r.ToImmutableDictionary()
        }

        GlbFunc.CreateDir fname |> ignore

        PrivateCaller.Invoke< PRManager >( "SavePRFile", objidx_me.NewID(), lun_me.zero, fname, pr, true ) :?> Task
        |> Functions.RunTaskSynchronously

        GlbFunc.DeleteDir fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member _.paramDataToBasicParameterList_001() =
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                23u,
                PooledBuffer.RentAndInit 23
            ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            ()

    [<Fact>]
    member _.paramDataToBasicParameterList_002() =
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                25u,
                PooledBuffer.RentAndInit 24
            ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException ->
            ()

    [<Fact>]
    member _.paramDataToBasicParameterList_003() =
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                0xFFFFFFFFu,
                PooledBuffer.RentAndInit 24
            ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException ->
            ()

    [<Fact>]
    member _.paramDataToBasicParameterList_004() =
        let paramData = [|
            // RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // SERVICE ACTION RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;
            // Reserved
            0x00uy;
            // Obsoleted
            0x00uy; 0x00uy;
            // Additional parameter data
            0x00uy;
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException ->
            ()

    [<Fact>]
    member _.paramDataToBasicParameterList_005() =
        let paramData = [|
            // RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // SERVICE ACTION RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;
            // Reserved
            0x00uy;
            // Obsoleted
            0x00uy; 0x00uy;
            // Additional parameter data
            0x00uy; 0x00uy; 0x00uy; 0x01uy;
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException ->
            ()

    [<Fact>]
    member _.paramDataToBasicParameterList_006() =
        let paramData = [|
            // RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // SERVICE ACTION RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // SPEC_I_PT, ALL_TG_PT, APTPL
            0x08uy;
            // Reserved
            0x00uy;
            // Obsoleted
            0x00uy; 0x00uy;
            // Additional parameter data
            0x00uy; 0x00uy; 0x00uy;
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException ->
            ()

    [<Fact>]
    member _.paramDataToBasicParameterList_007() =
        let paramData = [|
            // RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // SERVICE ACTION RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // SPEC_I_PT, ALL_TG_PT, APTPL
            0x08uy;
            // Reserved
            0x00uy;
            // Obsoleted
            0x00uy; 0x00uy;
            // Additional parameter data
            0x00uy; 0x00uy; 0x00uy; 0x01uy;
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException ->
            ()

    [<Fact>]
    member _.paramDataToBasicParameterList_008() =
        let paramData = [|
            // RESERVATION KEY
            0x01uy; 0x02uy; 0x03uy; 0x04uy; 0x05uy; 0x06uy; 0x07uy; 0x08uy;
            // SERVICE ACTION RESERVATION KEY
            0x11uy; 0x12uy; 0x13uy; 0x14uy; 0x15uy; 0x16uy; 0x17uy; 0x18uy;
            // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // SPEC_I_PT, ALL_TG_PT, APTPL
            0x05uy;
            // Reserved
            0x00uy;
            // Obsoleted
            0x00uy; 0x00uy;
        |]
        let r = 
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) :?> BasicParameterList
        Assert.True(( r.ReservationKey = resvkey_me.fromPrim 0x0102030405060708UL ))
        Assert.True(( r.ServiceActionReservationKey = resvkey_me.fromPrim 0x1112131415161718UL ))
        Assert.False(( r.SPEC_I_PT ))
        Assert.True(( r.ALL_TG_PT ))
        Assert.True(( r.APTPL ))
        Assert.True(( r.TransportID.Length = 0 ))

    [<Fact>]
    member _.paramDataToBasicParameterList_009() =
        let paramData = [|
            // RESERVATION KEY
            0x01uy; 0x02uy; 0x03uy; 0x04uy; 0x05uy; 0x06uy; 0x07uy; 0x08uy;
            // SERVICE ACTION RESERVATION KEY
            0x11uy; 0x12uy; 0x13uy; 0x14uy; 0x15uy; 0x16uy; 0x17uy; 0x18uy;
            // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // SPEC_I_PT, ALL_TG_PT, APTPL
            0x0Duy;
            // Reserved
            0x00uy;
            // Obsoleted
            0x00uy; 0x00uy;
            // Additional parameter data
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        let r = 
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) :?> BasicParameterList
        Assert.True(( r.ReservationKey = resvkey_me.fromPrim 0x0102030405060708UL ))
        Assert.True(( r.ServiceActionReservationKey = resvkey_me.fromPrim 0x1112131415161718UL ))
        Assert.True(( r.SPEC_I_PT ))
        Assert.True(( r.ALL_TG_PT ))
        Assert.True(( r.APTPL ))
        Assert.True(( r.TransportID.Length = 0 ))

    [<Fact>]
    member _.paramDataToBasicParameterList_010() =
        let paramData = [|
            // RESERVATION KEY
            0x01uy; 0x02uy; 0x03uy; 0x04uy; 0x05uy; 0x06uy; 0x07uy; 0x08uy;
            // SERVICE ACTION RESERVATION KEY
            0x11uy; 0x12uy; 0x13uy; 0x14uy; 0x15uy; 0x16uy; 0x17uy; 0x18uy;
            // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            // SPEC_I_PT, ALL_TG_PT, APTPL
            0x05uy;
            // Reserved
            0x00uy;
            // Obsoleted
            0x00uy; 0x00uy;
            // Additional parameter data
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        let r = 
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) :?> BasicParameterList
        Assert.True(( r.ReservationKey = resvkey_me.fromPrim 0x0102030405060708UL ))
        Assert.True(( r.ServiceActionReservationKey = resvkey_me.fromPrim 0x1112131415161718UL ))
        Assert.False(( r.SPEC_I_PT ))
        Assert.True(( r.ALL_TG_PT ))
        Assert.True(( r.APTPL ))
        Assert.True(( r.TransportID.Length = 0 ))

    [<Fact>]
    member _.paramDataToBasicParameterList_011() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x01uy; // TRANSPORTID PARAMETER DATA LENGTH
            0x85uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "Invalid FORMAT CODE value" = 0 ))

    [<Fact>]
    member _.paramDataToBasicParameterList_012() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x01uy; // TRANSPORTID PARAMETER DATA LENGTH
            0x40uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "Invalid PROTOCOL IDENTIFIER value" = 0 ))

    [<Fact>]
    member _.paramDataToBasicParameterList_013() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x14uy; // TRANSPORTID PARAMETER DATA LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x10uy;                 // ADDITIONAL LENGTH(16)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "Invalid TransportID length" = 0 ))

    [<Fact>]
    member _.paramDataToBasicParameterList_014() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x18uy; // TRANSPORTID PARAMETER DATA LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x18uy;                 // ADDITIONAL LENGTH(24)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "Invalid  ADDITIONAL LENGTH value" = 0 ))

    [<Fact>]
    member _.paramDataToBasicParameterList_015() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x18uy; // TRANSPORTID PARAMETER DATA LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x10uy;                 // ADDITIONAL LENGTH(16)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "Invalid  ADDITIONAL LENGTH value" = 0 ))

    [<Fact>]
    member _.paramDataToBasicParameterList_016() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x19uy; // TRANSPORTID PARAMETER DATA LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x15uy;                 // ADDITIONAL LENGTH(21)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy;
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "Invalid  ADDITIONAL LENGTH value" = 0 ))

    [<Fact>]
    member _.paramDataToBasicParameterList_017() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x18uy; // TRANSPORTID PARAMETER DATA LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH(20)
            0x41uy; 0x2Cuy; 0x69uy; 0x2Cuy; // A,i,0x000001
            0x30uy; 0x78uy; 0x30uy; 0x30uy; 
            0x30uy; 0x30uy; 0x30uy; 0x30uy; 
            0x30uy; 0x30uy; 0x30uy; 0x30uy; 
            0x30uy; 0x31uy; 0x00uy; 0x00uy;
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "Invalid TransportID format" = 0 ))

    [<Fact>]
    member _.paramDataToBasicParameterList_018() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x18uy; // TRANSPORTID PARAMETER DATA LENGTH
            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH(20)
            0x41uy; 0x00uy; 0x00uy; 0x00uy; // A
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "Invalid TransportID format" = 0 ))

    [<Fact>]
    member _.paramDataToBasicParameterList_019() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x18uy; // TRANSPORTID PARAMETER DATA LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH(20)
            0x61uy; 0x2Cuy; 0x69uy; 0x2Cuy; // a,i,0xFF1111223333
            0x30uy; 0x78uy; 0x46uy; 0x46uy; 
            0x31uy; 0x31uy; 0x31uy; 0x31uy; 
            0x32uy; 0x32uy; 0x33uy; 0x33uy; 
            0x33uy; 0x33uy; 0x00uy; 0x00uy;
        |]
        try
            let r =
                PrivateCaller.Invoke< PRManager >(
                    "paramDataToBasicParameterList",
                    PRManager_Test1.defaultSource,
                    objidx_me.NewID(),
                    lun_me.zero,
                    itt_me.fromPrim 0u,
                    uint32 paramData.Length,
                    ( PooledBuffer.Rent paramData )
                ) :?> BasicParameterList
            Assert.True(( r.ReservationKey = resvkey_me.fromPrim 0x0102030405060708UL ))
            Assert.True(( r.ServiceActionReservationKey = resvkey_me.fromPrim 0x1112131415161718UL ))
            Assert.True(( r.SPEC_I_PT ))
            Assert.True(( r.ALL_TG_PT ))
            Assert.True(( r.APTPL ))
            Assert.True(( r.TransportID.Length = 1 ))
            Assert.True(( ( fst r.TransportID.[0] ) = "a" ))
            Assert.True(( ( snd r.TransportID.[0] ).IsSome ))
            Assert.True(( ( snd r.TransportID.[0] ).Value |> isid_me.get_T = 0xC0uy ))
            Assert.True(( ( snd r.TransportID.[0] ).Value |> isid_me.get_A = 0x3Fuy ))
            Assert.True(( ( snd r.TransportID.[0] ).Value |> isid_me.get_B = 0x1111us ))
            Assert.True(( ( snd r.TransportID.[0] ).Value |> isid_me.get_C = 0x22uy ))
            Assert.True(( ( snd r.TransportID.[0] ).Value |> isid_me.get_D = 0x3333us ))
        with
        | :? SCSIACAException ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.paramDataToBasicParameterList_020() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x18uy; // TRANSPORTID PARAMETER DATA LENGTH
            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH(20)
            0x61uy; 0x00uy; 0x00uy; 0x00uy; // a
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
        |]
        try
            let r =
                PrivateCaller.Invoke< PRManager >(
                    "paramDataToBasicParameterList",
                    PRManager_Test1.defaultSource,
                    objidx_me.NewID(),
                    lun_me.zero,
                    itt_me.fromPrim 0u,
                    uint32 paramData.Length,
                    ( PooledBuffer.Rent paramData )
                ) :?> BasicParameterList
            Assert.True(( r.ReservationKey = resvkey_me.fromPrim 0x0102030405060708UL ))
            Assert.True(( r.ServiceActionReservationKey = resvkey_me.fromPrim 0x1112131415161718UL ))
            Assert.True(( r.SPEC_I_PT ))
            Assert.True(( r.ALL_TG_PT ))
            Assert.True(( r.APTPL ))
            Assert.True(( r.TransportID.Length = 1 ))
            Assert.True(( ( fst r.TransportID.[0] ) = "a" ))
            Assert.True(( ( snd r.TransportID.[0] ).IsNone ))
        with
        | :? SCSIACAException ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.paramDataToBasicParameterList_021() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0xFCuy; // TRANSPORTID PARAMETER DATA LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0xF8uy;                 // ADDITIONAL LENGTH(248)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "TransportID is too long. " = 0 ))

    [<Fact>]
    member _.paramDataToBasicParameterList_022() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0xE8uy; // TRANSPORTID PARAMETER DATA LENGTH
            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0xE4uy;                 // ADDITIONAL LENGTH(228)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToBasicParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Message.Contains "TransportID is too long. " ))

    [<Fact>]
    member _.paramDataToBasicParameterList_023() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x01uy; 0xDCuy; // TRANSPORTID PARAMETER DATA LENGTH

            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0xF4uy;                 // ADDITIONAL LENGTH(244)
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy;
            0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x62uy; 0x2Cuy;
            0x69uy; 0x2Cuy; 0x30uy; 0x78uy; 0x46uy; 0x46uy; 0x36uy; 0x36uy;
            0x36uy; 0x36uy; 0x35uy; 0x35uy; 0x34uy; 0x34uy; 0x34uy; 0x34uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 

            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0xE0uy;                 // ADDITIONAL LENGTH(224)
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy;
            0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x63uy; 0x00uy;
        |]
        try
            let r =
                PrivateCaller.Invoke< PRManager >(
                    "paramDataToBasicParameterList",
                    PRManager_Test1.defaultSource,
                    objidx_me.NewID(),
                    lun_me.zero,
                    itt_me.fromPrim 0u,
                    uint32 paramData.Length,
                    ( PooledBuffer.Rent paramData )
                ) :?> BasicParameterList
            Assert.True(( r.ReservationKey = resvkey_me.fromPrim 0x0102030405060708UL ))
            Assert.True(( r.ServiceActionReservationKey = resvkey_me.fromPrim 0x1112131415161718UL ))
            Assert.True(( r.SPEC_I_PT ))
            Assert.True(( r.ALL_TG_PT ))
            Assert.True(( r.APTPL ))
            Assert.True(( r.TransportID.Length = 2 ))
            Assert.True(( ( fst r.TransportID.[0] ) = String.replicate 223 "b" ))
            Assert.True(( ( snd r.TransportID.[0] ).IsSome ))
            Assert.True(( ( snd r.TransportID.[0] ).Value |> isid_me.get_T = 0xC0uy ))
            Assert.True(( ( snd r.TransportID.[0] ).Value |> isid_me.get_A = 0x3Fuy ))
            Assert.True(( ( snd r.TransportID.[0] ).Value |> isid_me.get_B = 0x6666us ))
            Assert.True(( ( snd r.TransportID.[0] ).Value |> isid_me.get_C = 0x55uy ))
            Assert.True(( ( snd r.TransportID.[0] ).Value |> isid_me.get_D = 0x4444us ))
            Assert.True(( ( fst r.TransportID.[1] ) = String.replicate 223 "c" ))
            Assert.True(( ( snd r.TransportID.[1] ).IsNone ))
        with
        | :? SCSIACAException ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.paramDataToBasicParameterList_024() =
        let paramData = [|
            0x01uy; 0x02uy; 0x03uy; 0x04uy; // RESERVATION KEY
            0x05uy; 0x06uy; 0x07uy; 0x08uy;
            0x11uy; 0x12uy; 0x13uy; 0x14uy; // SERVICE ACTION RESERVATION KEY
            0x15uy; 0x16uy; 0x17uy; 0x18uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsoleted
            0x0Duy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsoleted
            0x00uy; 0x00uy; 0x00uy; 0x30uy; // TRANSPORTID PARAMETER DATA LENGTH

            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH(20)
            0x61uy; 0x00uy; 0x00uy; 0x00uy; // a
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 

            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH(20)
            0x62uy; 0x00uy; 0x00uy; 0x00uy; // b
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 

            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH(20)
            0x63uy; 0x00uy; 0x00uy; 0x00uy; // c
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
        |]
        try
            let r =
                PrivateCaller.Invoke< PRManager >(
                    "paramDataToBasicParameterList",
                    PRManager_Test1.defaultSource,
                    objidx_me.NewID(),
                    lun_me.zero,
                    itt_me.fromPrim 0u,
                    uint32 paramData.Length,
                    ( PooledBuffer.Rent paramData )
                ) :?> BasicParameterList
            Assert.True(( r.ReservationKey = resvkey_me.fromPrim 0x0102030405060708UL ))
            Assert.True(( r.ServiceActionReservationKey = resvkey_me.fromPrim 0x1112131415161718UL ))
            Assert.True(( r.SPEC_I_PT ))
            Assert.True(( r.ALL_TG_PT ))
            Assert.True(( r.APTPL ))
            Assert.True(( r.TransportID.Length = 2 ))
            Assert.True(( ( fst r.TransportID.[0] ) = "a" ))
            Assert.True(( ( snd r.TransportID.[0] ).IsNone ))
            Assert.True(( ( fst r.TransportID.[1] ) = "b" ))
            Assert.True(( ( snd r.TransportID.[1] ).IsNone ))
        with
        | :? SCSIACAException ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.paramDataToMoveParameterList_001() =
        let paramData = [| 1uy .. 47uy |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToMoveParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "Parameter length in" = 0 ))

    [<Fact>]
    member _.paramDataToMoveParameterList_002() =
        let paramData = [| 1uy .. 48uy |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToMoveParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 ( paramData.Length + 1 ),
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "Parameter length in" = 0 ))

    [<Fact>]
    member _.paramDataToMoveParameterList_003() =
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToMoveParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                0xFFFFFFFFu,
                PooledBuffer.Empty
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "Parameter length in" = 0 ))

    [<Fact>]
    member _.paramDataToMoveParameterList_004() =
        let paramData = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy;                         // Reserved
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER 
            0x00uy; 0x00uy; 0x00uy; 0x01uy; // TRANSPORTID PARAMETER DATA LENGTH

            0x00uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x15uy;                 // ADDITIONAL LENGTH(21)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy;
        |]
        try
            PrivateCaller.Invoke< PRManager >(
                "paramDataToMoveParameterList",
                PRManager_Test1.defaultSource,
                objidx_me.NewID(),
                lun_me.zero,
                itt_me.fromPrim 0u,
                uint32 paramData.Length,
                ( PooledBuffer.Rent paramData )
            ) |> ignore
        with
        | :? SCSIACAException as x ->
            Assert.True(( Functions.CompareStringHeader x.Message "Invalid TRANSPORTID PARAMETER DATA LENGTH" = 0 ))

    [<Fact>]
    member _.paramDataToMoveParameterList_005() =
        let paramData = [|
            0x04uy; 0x03uy; 0x02uy; 0x01uy; // RESERVATION KEY
            0x08uy; 0x07uy; 0x06uy; 0x05uy;
            0x14uy; 0x13uy; 0x12uy; 0x11uy; // SERVICE ACTION RESERVATION KEY 
            0x18uy; 0x17uy; 0x16uy; 0x15uy;
            0x00uy;                         // Reserved
            0x00uy;                         // Reserved
            0x21uy; 0x22uy;                 // RELATIVE TARGET PORT IDENTIFIER 
            0x00uy; 0x00uy; 0x00uy; 0x18uy; // TRANSPORTID PARAMETER DATA LENGTH

            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH(20)
            0x61uy; 0x00uy; 0x00uy; 0x00uy; // a
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
        |]
        try
            let r =
                PrivateCaller.Invoke< PRManager >(
                    "paramDataToMoveParameterList",
                    PRManager_Test1.defaultSource,
                    objidx_me.NewID(),
                    lun_me.zero,
                    itt_me.fromPrim 0u,
                    uint32 paramData.Length,
                    ( PooledBuffer.Rent paramData )
                ) :?> MoveParameterList
            Assert.True(( r.ReservationKey = resvkey_me.fromPrim 0x0403020108070605UL ))
            Assert.True(( r.ServiceActionReservationKey = resvkey_me.fromPrim 0x1413121118171615UL ))
            Assert.True(( r.RelativeTargetPortIdentifier = 0x2122us ))
            Assert.True(( fst r.TransportID = "a" ))
            Assert.True(( ( snd r.TransportID ).IsNone ))
        with
        | :? SCSIACAException as x ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.paramDataToMoveParameterList_006() =
        let paramData = [|
            0x04uy; 0x03uy; 0x02uy; 0x01uy; // RESERVATION KEY
            0x08uy; 0x07uy; 0x06uy; 0x05uy;
            0x14uy; 0x13uy; 0x12uy; 0x11uy; // SERVICE ACTION RESERVATION KEY 
            0x18uy; 0x17uy; 0x16uy; 0x15uy;
            0x00uy;                         // Reserved
            0x00uy;                         // Reserved
            0x21uy; 0x22uy;                 // RELATIVE TARGET PORT IDENTIFIER 
            0x00uy; 0x00uy; 0x00uy; 0x30uy; // TRANSPORTID PARAMETER DATA LENGTH

            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH(20)
            0x61uy; 0x00uy; 0x00uy; 0x00uy; // a
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 

            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH(20)
            0x62uy; 0x00uy; 0x00uy; 0x00uy; // b
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
            0x00uy; 0x00uy; 0x00uy; 0x00uy; 
        |]
        try
            let r =
                PrivateCaller.Invoke< PRManager >(
                    "paramDataToMoveParameterList",
                    PRManager_Test1.defaultSource,
                    objidx_me.NewID(),
                    lun_me.zero,
                    itt_me.fromPrim 0u,
                    uint32 paramData.Length,
                    ( PooledBuffer.Rent paramData )
                ) :?> MoveParameterList
            Assert.True(( r.ReservationKey = resvkey_me.fromPrim 0x0403020108070605UL ))
            Assert.True(( r.ServiceActionReservationKey = resvkey_me.fromPrim 0x1413121118171615UL ))
            Assert.True(( r.RelativeTargetPortIdentifier = 0x2122us ))
            Assert.True(( fst r.TransportID = "a" ))
            Assert.True(( ( snd r.TransportID ).IsNone ))
        with
        | :? SCSIACAException as x ->
            Assert.Fail __LINE__

    static member m_decideACANoncompliant_001_data = [|
            [| ( { OperationCode = 0xA4uy; ServiceAction = 0x0Bus; ParameterListLength = 0u; Control = 0uy; } : ChangeAliasesCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x83uy; ParameterListLength = 0u; Control = 0uy; } : ExtendedCopyCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x12uy; EVPD = false; PageCode = 0uy; AllocationLength = 0us; Control = 0uy; } : InquiryCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x4Cuy; PCR = false; SP = false; PC = 0uy; ParameterListLength = 0us; Control = 0uy; } : LogSelectCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x4Duy; PPC = false; SP = false; PC = 0uy; ParameterPointer = 0us; AllocationLength = 0us; Control = 0uy; } : LogSenseCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x15uy; PF = false; SP = false; ParameterListLength = 0us; Control = 0uy; } : ModeSelectCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x55uy; PF = false; SP = false; ParameterListLength = 0us; Control = 0uy; } : ModeSelectCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x1Auy; LLBAA = false; DBD = false; PC = 0uy; PageCode = 0uy; SubPageCode = 0uy; AllocationLength = 0us; Control = 0uy; } : ModeSenseCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x15uy; LLBAA = false; DBD = false; PC = 0uy; PageCode = 0uy; SubPageCode = 0uy; AllocationLength = 0us; Control = 0uy; } : ModeSenseCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x5Euy; ServiceAction = 0uy; AllocationLength = 0us; Control = 0uy; } : PersistentReserveInCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x1Euy; Prevent = 0uy; Control = 0uy; } : PreventAllowMediumRemovalCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x8Cuy; ServiceAction = 0uy; VolumeNumber = 0uy; PartitionNumber = 0uy; FirstAttributeIdentifier = 0us; AllocationLength = 0u; Control = 0uy; } : ReadAttributeCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x8Cuy; Mode = 0uy; BufferID = 0uy; BufferOffset = 0u; AllocationLength = 0u; Control = 0uy; } : ReadBufferCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xABuy; ServiceAction = 0uy; AllocationLength = 0u; Control = 0uy; } : ReadMediaSerialNumberCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x84uy; ServiceAction = 0uy; ListIdentifier = 0uy; AllocationLength = 0u; Control = 0uy; } : ReceiveCopyResultsCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x1Cuy; PageCode = 0uy; AllocationLength = 0us; Control = 0uy; } : ReceiveDiagnosticResultsCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA3uy; ServiceAction = 0x0Buy; AllocationLength = 0u; Control = 0uy; } : ReportAliasesCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA3uy; ServiceAction = 0x05uy; AllocationLength = 0u; Control = 0uy; } : ReportDeviceIdentifierCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA0uy; SelectReport = 0uy; AllocationLength = 0u; Control = 0uy; } : ReportLUNsCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA3uy; ServiceAction = 0x0Euy; PriorityReported = 0uy; AllocationLength = 0u; Control = 0uy; } : ReportPriorityCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA3uy; ServiceAction = 0x0Cuy; ReportingOptions = 0uy; RequestedOperationCode = 0uy; RequestedServiceAction = 0us; AllocationLength = 0u; Control = 0uy; } : ReportSupportedOperationCodesCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA3uy; ServiceAction = 0x0Duy; AllocationLength = 0u; Control = 0uy; } : ReportSupportedTaskManagementFunctionsCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA3uy; ServiceAction = 0x0Auy; AllocationLength = 0u; Control = 0uy; } : ReportTargetPortGroupsCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA3uy; ServiceAction = 0x0Fuy; AllocationLength = 0u; Control = 0uy; } : ReportTimestampCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x03uy; DESC = false; AllocationLength = 0uy; Control = 0uy; } : RequestSenseCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x1Duy; SelfTestCode = 0uy; PF = false; SelfTest = false; DevOffL = false; UnitOffL = false; ParameterListLength = 0us; Control = 0uy; } : SendDiagnosticCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA4uy; ServiceAction = 0x06uy; ParameterListLength = 0u; Control = 0uy; } : SetDeviceIdentifierCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA4uy; ServiceAction = 0x0Euy; I_T_LNexusToSet = 0uy; ParameterListLength = 0u; Control = 0uy; } : SetPriorityCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA4uy; ServiceAction = 0x0Auy; ParameterListLength = 0u; Control = 0uy; } : SetTargetPortGroupsCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA4uy; ServiceAction = 0x0Fuy; ParameterListLength = 0u; Control = 0uy; } : SetTimestampCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x00uy; Control = 0uy; } : TestUnitReadyCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x80uy; VolumeNumber = 0uy; PartitionNumber = 0uy; ParameterListLength = 0u; Control = 0uy; } : WriteAttributeCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x3Buy; Mode = 0uy; BufferID = 0uy; BufferOffset = 0u; ParameterListLength = 0u; Control = 0uy; } : WriteBufferCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x86uy; ServiceAction = 0x00uy; ManagementIdentifierKey = 0UL; AllocationLength = 0u; Control = 0uy; } : AccessControlIn_ReportAclCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x86uy; ServiceAction = 0x01uy; ManagementIdentifierKey = 0UL; AllocationLength = 0u; Control = 0uy; } : AccessControlIn_ReportLUDescriptorsCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x86uy; ServiceAction = 0x02uy; ManagementIdentifierKey = 0UL; LogPortion = 0uy; AllocationLength = 0us; Control = 0uy; } : AccessControlIn_ReportAccessControlsLogCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x86uy; ServiceAction = 0x03uy; ManagementIdentifierKey = 0UL; AllocationLength = 0u; Control = 0uy; } : AccessControlIn_ReportOverrideLockoutTimerCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x86uy; ServiceAction = 0x04uy; LUNValue = lun_me.zero; AllocationLength = 0u; Control = 0uy; } : AccessControlIn_RequestProxyTokenCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x87uy; ServiceAction = 0x00uy; ParameterListLength = 0u; Control = 0uy; } : AccessControlOutCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x04uy; FMTPINFO = false; RTO_REQ = false; LONGLIST = false; FMTDATA = false; CMPLIST = false; DefectListFormat = 0uy; Control = 0uy; } : FormatUnitCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x34uy; IMMED = false; LogicalBlockAddress = blkcnt_me.zero64; PrefetchLength = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : PreFetchCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x90uy; IMMED = false; LogicalBlockAddress = blkcnt_me.zero64; PrefetchLength = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : PreFetchCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x08uy; RdProtect = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : ReadCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x28uy; RdProtect = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : ReadCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xA8uy; RdProtect = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : ReadCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x88uy; RdProtect = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : ReadCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x0009us; RDPROTECT = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; ExpectedInitialLogicalBlockReferenceTag = 0u; ExpectedLogicalBlockApplicationTag = 0us; LogicalBlockApplicationTagMask = 0us; TransferLength = blkcnt_me.zero32; } : Read32CDB ) :> ICDB; |];
            [| ( { OperationCode = 0x25uy; ServiceAction = 0x00uy; LogicalBlockAddress = blkcnt_me.zero64; PMI = false; AllocationLength = 0u; Control = 0uy; } : ReadCapacityCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x9Euy; ServiceAction = 0x10uy; LogicalBlockAddress = blkcnt_me.zero64; PMI = false; AllocationLength = 0u; Control = 0uy; } : ReadCapacityCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x37uy; REQ_PLIST = false; REQ_GLIST = false; DefectListFormat = 0uy; AllocationLength = 0u; Control = 0uy; } : ReadDefectDataCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xB7uy; REQ_PLIST = false; REQ_GLIST = false; DefectListFormat = 0uy; AllocationLength = 0u; Control = 0uy; } : ReadDefectDataCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x3Euy; ServiceAction = 0x00uy; LogicalBlockAddress = blkcnt_me.zero64; ByteTransferLength = 0us; CORRT = false; Control = 0uy; } : ReadLongCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x9Euy; ServiceAction = 0x11uy; LogicalBlockAddress = blkcnt_me.zero64; ByteTransferLength = 0us; CORRT = false; Control = 0uy; } : ReadLongCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x07uy; LONGLBA = false; LONGLIST = false; Control = 0uy; } : ReassignBlocksCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x1Buy; IMMED = false; PowerCondition = 0uy; LOEJ = false; Start = false; Control = 0uy; } : StartStopUnitCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x35uy; SyncNV = false; IMMED = false; LogicalBlockAddress = blkcnt_me.zero64; NumberOfBlocks = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : SynchronizeCacheCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x91uy; SyncNV = false; IMMED = false; LogicalBlockAddress = blkcnt_me.zero64; NumberOfBlocks = blkcnt_me.zero32; GroupNumber = 0uy; Control = 0uy; } : SynchronizeCacheCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x2Fuy; VRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; VerificationLength = blkcnt_me.zero32; Control = 0uy; } : VerifyCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xAFuy; VRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; VerificationLength = blkcnt_me.zero32; Control = 0uy; } : VerifyCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x8Fuy; VRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; VerificationLength = blkcnt_me.zero32; Control = 0uy; } : VerifyCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x000Aus; VRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; ExpectedInitialLogicalBlockReferenceTag = 0u; ExpectedLogicalBlockApplicationTag = 0us; LogicalBlockApplicationTagMask = 0us; VerificationLength = blkcnt_me.zero32; } : Verify32CDB ) :> ICDB; |];
            [| ( { OperationCode = 0x0Auy; WRPROTECT = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x2Auy; WRPROTECT = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xAAuy; WRPROTECT = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x8Auy; WRPROTECT = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x000Bus; WRPROTECT = 0uy; DPO = false; FUA = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; ExpectedInitialLogicalBlockReferenceTag = 0u; ExpectedLogicalBlockApplicationTag = 0us; LogicalBlockApplicationTagMask = 0us; TransferLength = blkcnt_me.zero32; } : Write32CDB ) :> ICDB; |];
            [| ( { OperationCode = 0x2Euy; WRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteAndVerifyCDB ) :> ICDB; |];
            [| ( { OperationCode = 0xAEuy; WRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteAndVerifyCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x8Euy; WRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; TransferLength = blkcnt_me.zero32; Control = 0uy; } : WriteAndVerifyCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x000Cus; WRPROTECT = 0uy; DPO = false; BYTCHK = false; LogicalBlockAddress = blkcnt_me.zero64; ExpectedInitialLogicalBlockReferenceTag = 0u; ExpectedLogicalBlockApplicationTag = 0us; LogicalBlockApplicationTagMask = 0us; TransferLength = blkcnt_me.zero32; } : WriteAndVerify32CDB ) :> ICDB; |];
            [| ( { OperationCode = 0x3Fuy; ServiceAction = 0x00uy; LogicalBlockAddress = blkcnt_me.zero64; ByteTransferLength = 0us; Control = 0uy; } : WriteLongCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x9Fuy; ServiceAction = 0x11uy; LogicalBlockAddress = blkcnt_me.zero64; ByteTransferLength = 0us; Control = 0uy; } : WriteLongCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x41uy; WRPROTECT = 0uy; PBDATA = false; LBDATA = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; NumberOfBlocks = blkcnt_me.zero32; Control = 0uy; } : WriteSameCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x93uy; WRPROTECT = 0uy; PBDATA = false; LBDATA = false; LogicalBlockAddress = blkcnt_me.zero64; GroupNumber = 0uy; NumberOfBlocks = blkcnt_me.zero32; Control = 0uy; } : WriteSameCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x000Dus; WRPROTECT = 0uy; PBDATA = false; LBDATA = false; LogicalBlockAddress = blkcnt_me.zero64; ExpectedInitialLogicalBlockReferenceTag = 0u; ExpectedLogicalBlockApplicationTag = 0us; LogicalBlockApplicationTagMask = 0us; NumberOfBlocks = blkcnt_me.zero32; } : WriteSame32CDB ) :> ICDB; |];
            [| ( { OperationCode = 0x52uy; XORPINFO = false; LogicalBlockAddress = blkcnt_me.zero32; GroupNumber = 0uy; TransferLength = blkcnt_me.zero16; Control = 0uy; } : XDReadCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x0003us; XORPINFO = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; } : XDRead32CDB ) :> ICDB; |];
            [| ( { OperationCode = 0x50uy; WRPROTECT = 0uy; DPO = false; FUA = false; DisableWrite = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero32; GroupNumber = 0uy; TransferLength = blkcnt_me.zero16; Control = 0uy; } : XDWriteCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x0004us; WRPROTECT = 0uy; DPO = false; FUA = false; DisableWrite = false; FUA_NV = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; } : XDWrite32CDB ) :> ICDB; |];
            [| ( { OperationCode = 0x53uy; WRPROTECT = 0uy; DPO = false; FUA = false; DisableWrite = false; FUA_NV = false; XORPINFO = false; LogicalBlockAddress = blkcnt_me.zero32; GroupNumber = 0uy; TransferLength = blkcnt_me.zero16; Control = 0uy; } : XDWriteReadCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x0007us; WRPROTECT = 0uy; DPO = false; FUA = false; DisableWrite = false; FUA_NV = false; XORPINFO = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; } : XDWriteRead32CDB ) :> ICDB; |];
            [| ( { OperationCode = 0x51uy; DPO = false; FUA = false; FUA_NV = false; XORPINFO = false; LogicalBlockAddress = blkcnt_me.zero32; GroupNumber = 0uy; TransferLength = blkcnt_me.zero16; Control = 0uy; } : XPWriteCDB ) :> ICDB; |];
            [| ( { OperationCode = 0x7Fuy; Control = 0uy; GroupNumber = 0uy; AdditionalCDBLength = 18uy; ServiceAction = 0x0006us; DPO = false; FUA = false; FUA_NV = false; XORPINFO = false; LogicalBlockAddress = blkcnt_me.zero64; TransferLength = blkcnt_me.zero32; } : XPWrite32CDB ) :> ICDB; |];
        |]

    [<Theory>]
    [<MemberData( "m_decideACANoncompliant_001_data" )>]
    member _.decideACANoncompliant_001 ( vcdb : ICDB ) =
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub(), lun_me.zero, "", k )
        let faultI_TNexus = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        Assert.False( pm.decideACANoncompliant PRManager_Test1.defaultSource lun_me.zero ( itt_me.fromPrim 0u ) vcdb PooledBuffer.Empty [] faultI_TNexus )
        k.NoticeTerminate()


    [<Fact>]
    member _.decideACANoncompliant_002() =
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub(), lun_me.zero, "", k )
        let faultI_TNexus = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let cdb = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0x05uy;   // PREEMPT AND ABORT
            Scope = 0uy;
            PRType = PR_TYPE.WRITE_EXCLUSIVE;
            ParameterListLength = 0u;
            Control = 0uy;
        }
        Assert.False( pm.decideACANoncompliant PRManager_Test1.defaultSource lun_me.zero ( itt_me.fromPrim 0u ) cdb PooledBuffer.Empty [] faultI_TNexus )
        k.NoticeTerminate()

    [<Fact>]
    member this.decideACANoncompliant_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "decideACANoncompliant_003.txt"

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub(), lun_me.zero, fname, k )
        let faultI_TNexus = new ITNexus( "initiator999", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us );
        let cdb = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0x05uy;   // PREEMPT AND ABORT
            Scope = 0uy;
            PRType = PR_TYPE.WRITE_EXCLUSIVE;
            ParameterListLength = 0u;
            Control = 0uy;
        }
        Assert.False( pm.decideACANoncompliant PRManager_Test1.defaultSource lun_me.zero ( itt_me.fromPrim 0u ) cdb PooledBuffer.Empty [] faultI_TNexus )
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName
            
    [<Fact>]
    member this.decideACANoncompliant_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "decideACANoncompliant_004.txt"

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 7UL, true;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub(), lun_me.zero, fname, k )
        let faultI_TNexus = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us );
        let cdb = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0x05uy;   // PREEMPT AND ABORT
            Scope = 0uy;
            PRType = PR_TYPE.WRITE_EXCLUSIVE;
            ParameterListLength = 24u;
            Control = 0uy;
        }
        let data = {
            F = true;
            LUN = lun_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 0u;
            TargetTransferTag = ttt_me.fromPrim 0u;
            ExpStatSN = statsn_me.zero;
            DataSN = datasn_me.zero;
            BufferOffset = 0u;
            DataSegment = [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY
                0x00uy; 0x00uy; 0x00uy; 0x00uy;                                 // Obsolute
                0x00uy; // SPEC_I_PT, ALL_TG_PT, APTPL
                0x00uy; // Reserved
                0x00uy; 0x00uy; // Obsolute
            |] |> PooledBuffer.Rent;
            ByteCount = 0u;
        }
        Assert.False( pm.decideACANoncompliant PRManager_Test1.defaultSource lun_me.zero ( itt_me.fromPrim 0u ) cdb PooledBuffer.Empty [ data ] faultI_TNexus )
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.decideACANoncompliant_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "decideACANoncompliant_005.txt"

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 99UL, true;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub(), lun_me.zero, fname, k )
        let faultI_TNexus = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us );
        let cdb = {
            OperationCode = 0x5Fuy;
            ServiceAction = 0x05uy;   // PREEMPT AND ABORT
            Scope = 0uy;
            PRType = PR_TYPE.WRITE_EXCLUSIVE;
            ParameterListLength = 24u;
            Control = 0uy;
        }
        let data = {
            F = true;
            LUN = lun_me.zero;
            InitiatorTaskTag = itt_me.fromPrim 0u;
            TargetTransferTag = ttt_me.fromPrim 0u;
            ExpStatSN = statsn_me.zero;
            DataSN = datasn_me.zero;
            BufferOffset = 0u;
            DataSegment = [|
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY
                0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x63uy; // SERVICE ACTION RESERVATION KEY
                0x00uy; 0x00uy; 0x00uy; 0x00uy;                                 // Obsolute
                0x00uy; // SPEC_I_PT, ALL_TG_PT, APTPL
                0x00uy; // Reserved
                0x00uy; 0x00uy; // Obsolute
            |] |> PooledBuffer.Rent;
            ByteCount = 0u;
        }
        Assert.True( pm.decideACANoncompliant PRManager_Test1.defaultSource lun_me.zero ( itt_me.fromPrim 0u ) cdb PooledBuffer.Empty [ data ] faultI_TNexus )
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IsBlockedByPersistentReservation_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "IsBlockedByPersistentReservation_001.txt"

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 6us ), resvkey_me.fromPrim 99UL, true;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let task = new SendErrorStatusTask(
            new CStatus_Stub(),
            PRManager_Test1.defaultSource,
            {
                I = false;
                F = false;
                R = false;
                W = false;
                ATTR = TaskATTRCd.SIMPLE_TASK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                ExpectedDataTransferLength = 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                ScsiCDB = Array.empty;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
                ByteCount = 0u;
            },
            0u,
            new CInternalLU_Stub(),
            false,
            iScsiSvcRespCd.COMMAND_COMPLETE,
            ScsiCmdStatCd.CHECK_CONDITION
        )
        Assert.False( pm.IsBlockedByPersistentReservation PRManager_Test1.defaultSource task )
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member _.IsBlockedByPersistentReservation_002() =
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, "", k )
        let task = PRManager_Test1.defaultTaskObj( { OperationCode = 0x00uy; Control = 0uy; } : TestUnitReadyCDB ) pm
        Assert.False( pm.IsBlockedByPersistentReservation PRManager_Test1.defaultSource task )
        k.NoticeTerminate()

    [<Fact>]
    member this.IsBlockedByPersistentReservation_003_WRITE_EXCLUSIVE() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "IsBlockedByPersistentReservation_003_WRITE_EXCLUSIVE.txt"

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, true;
                new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let source_NotRegistered = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator999", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let source_Registered = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let source_Holder = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let pc = PrivateCaller( pm )
        let prInfo = ( pc.GetField( "m_Locker" ) :?> OptimisticLock<PRInfoRec> ).obj
        Assert.True(( prInfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( prInfo.m_Registrations.Count = 2 ))

        cdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( itrRes, _, _, _, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_NotRegistered task ) = itrRes )
            Assert.False( pm.IsBlockedByPersistentReservation source_Holder task )
            Assert.True(( pm.IsBlockedByPersistentReservation source_Registered task ) = itrRes )
        )

        prOutCdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( itrRes, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.False( pm.IsBlockedByPersistentReservation source_Holder task )
            Assert.True(( pm.IsBlockedByPersistentReservation source_Registered task ) = itrRes )
        )

        prOutCdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, itrRes, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_NotRegistered task ) = itrRes )
        )

        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IsBlockedByPersistentReservation_004_EXCLUSIVE_ACCESS() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "IsBlockedByPersistentReservation_004_EXCLUSIVE_ACCESS.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, true;
                new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let source_NotRegistered = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator999", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let source_Registered = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let source_Holder = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let pc = PrivateCaller( pm )
        let prInfo = ( pc.GetField( "m_Locker" ) :?> OptimisticLock<PRInfoRec> ).obj
        Assert.True(( prInfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( prInfo.m_Registrations.Count = 2 ))

        cdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, itrRes, _, _, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_NotRegistered task ) = itrRes )
            Assert.False( pm.IsBlockedByPersistentReservation source_Holder task )
            Assert.True(( pm.IsBlockedByPersistentReservation source_Registered task ) = itrRes )
        )

        prOutCdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( itrRes, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.False( pm.IsBlockedByPersistentReservation source_Holder task )
            Assert.True(( pm.IsBlockedByPersistentReservation source_Registered task ) = itrRes )
        )

        prOutCdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, itrRes, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_NotRegistered task ) = itrRes )
        )

        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IsBlockedByPersistentReservation_005_WRITE_EXCLUSIVE_ALL_REGISTRANTS_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "IsBlockedByPersistentReservation_005_WRITE_EXCLUSIVE_ALL_REGISTRANTS_001.txt"

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let source_Registered = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let pc = PrivateCaller( pm )
        let prInfo = ( pc.GetField( "m_Locker" ) :?> OptimisticLock<PRInfoRec> ).obj
        Assert.True(( prInfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ))
        Assert.True(( prInfo.m_Registrations.Count = 1 ))

        cdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, _, itrRes, _, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_Registered task ) = itrRes )
        )

        prOutCdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( itrRes, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_Registered task ) = itrRes )
        )

        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IsBlockedByPersistentReservation_006_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "IsBlockedByPersistentReservation_006_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_001.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let source_Registered = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let pc = PrivateCaller( pm )
        let prInfo = ( pc.GetField( "m_Locker" ) :?> OptimisticLock<PRInfoRec> ).obj
        Assert.True(( prInfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ))
        Assert.True(( prInfo.m_Registrations.Count = 1 ))

        cdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, _, itrRes, _, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_Registered task ) = itrRes )
        )

        prOutCdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( itrRes, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_Registered task ) = itrRes )
        )

        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IsBlockedByPersistentReservation_007_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "IsBlockedByPersistentReservation_007_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_001.txt"

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, true;
                new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let source_Registered = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let source_Holder = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let pc = PrivateCaller( pm )
        let prInfo = ( pc.GetField( "m_Locker" ) :?> OptimisticLock<PRInfoRec> ).obj
        Assert.True(( prInfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prInfo.m_Registrations.Count = 2 ))

        cdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, _, itrRes, _, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.False( pm.IsBlockedByPersistentReservation source_Holder task )
            Assert.True(( pm.IsBlockedByPersistentReservation source_Registered task ) = itrRes )
        )

        prOutCdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( itrRes, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.False( pm.IsBlockedByPersistentReservation source_Holder task )
            Assert.True(( pm.IsBlockedByPersistentReservation source_Registered task ) = itrRes )
        )

        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IsBlockedByPersistentReservation_008_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "IsBlockedByPersistentReservation_008_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_001.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, true;
                new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let source_Registered = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let source_Holder = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let pc = PrivateCaller( pm )
        let prInfo = ( pc.GetField( "m_Locker" ) :?> OptimisticLock<PRInfoRec> ).obj
        Assert.True(( prInfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prInfo.m_Registrations.Count = 2 ))

        cdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, _, itrRes, _, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.False( pm.IsBlockedByPersistentReservation source_Holder task )
            Assert.True(( pm.IsBlockedByPersistentReservation source_Registered task ) = itrRes )
        )

        prOutCdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( itrRes, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.False( pm.IsBlockedByPersistentReservation source_Holder task )
            Assert.True(( pm.IsBlockedByPersistentReservation source_Registered task ) = itrRes )
        )

        k.NoticeTerminate()
 
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IsBlockedByPersistentReservation_009_WRITE_EXCLUSIVE_ALL_REGISTRANTS_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "IsBlockedByPersistentReservation_009_WRITE_EXCLUSIVE_ALL_REGISTRANTS_002.txt"

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let source_NotRegistered = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator999", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let pc = PrivateCaller( pm )
        let prInfo = ( pc.GetField( "m_Locker" ) :?> OptimisticLock<PRInfoRec> ).obj
        Assert.True(( prInfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ))
        Assert.True(( prInfo.m_Registrations.Count = 1 ))

        cdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, _, _, itrRes, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_NotRegistered task ) = itrRes )
        )

        prOutCdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, itrRes, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_NotRegistered task ) = itrRes )
        )

        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IsBlockedByPersistentReservation_010_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "IsBlockedByPersistentReservation_010_WRITE_EXCLUSIVE_REGISTRANTS_ONLY_002.txt"

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, true;
                new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let source_NotRegistered = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator999", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let source_Holder = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let pc = PrivateCaller( pm )
        let prInfo = ( pc.GetField( "m_Locker" ) :?> OptimisticLock<PRInfoRec> ).obj
        Assert.True(( prInfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prInfo.m_Registrations.Count = 2 ))

        cdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, _, _, itrRes, _, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.False( pm.IsBlockedByPersistentReservation source_Holder task )
            Assert.True(( pm.IsBlockedByPersistentReservation source_NotRegistered task ) = itrRes )
        )

        prOutCdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, itrRes, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_NotRegistered task ) = itrRes )
        )

        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IsBlockedByPersistentReservation_011_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "IsBlockedByPersistentReservation_011_EXCLUSIVE_ACCESS_ALL_REGISTRANTS_002.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let source_NotRegistered = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator999", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let pc = PrivateCaller( pm )
        let prInfo = ( pc.GetField( "m_Locker" ) :?> OptimisticLock<PRInfoRec> ).obj
        Assert.True(( prInfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ))
        Assert.True(( prInfo.m_Registrations.Count = 1 ))

        cdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, _, _, _, itrRes, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_NotRegistered task ) = itrRes )
        )

        prOutCdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, itrRes, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_NotRegistered task ) = itrRes )
        )

        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.IsBlockedByPersistentReservation_012_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "IsBlockedByPersistentReservation_012_EXCLUSIVE_ACCESS_REGISTRANTS_ONLY_002.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, true;
                new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let source_NotRegistered = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator999", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let source_Holder = {
            PRManager_Test1.defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us );
        }
        let pc = PrivateCaller( pm )
        let prInfo = ( pc.GetField( "m_Locker" ) :?> OptimisticLock<PRInfoRec> ).obj
        Assert.True(( prInfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prInfo.m_Registrations.Count = 2 ))

        cdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, _, _, _, itrRes, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.False( pm.IsBlockedByPersistentReservation source_Holder task )
            Assert.True(( pm.IsBlockedByPersistentReservation source_NotRegistered task ) = itrRes )
        )

        prOutCdesForIsBlockedByPersistentReservation
        |> Array.iter ( fun ( _, itrRes, itrCDB ) ->
            let task = PRManager_Test1.defaultTaskObj itrCDB pm
            Assert.True(( pm.IsBlockedByPersistentReservation source_NotRegistered task ) = itrRes )
        )

        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ReadKey_001() =
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, "", k )
        let v = pm.ReadKey PRManager_Test1.defaultSource ( itt_me.fromPrim 0u )
        let ans = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // PRGENERATION
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // ADDITIONAL LENGTH
        |]
        Assert.True(( v = ans ))
        k.NoticeTerminate()

    [<Fact>]
    member this.ReadKey_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "ReadKey_002.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 99UL, true;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let v = pm.ReadKey PRManager_Test1.defaultSource ( itt_me.fromPrim 0u )
        let ans = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // PRGENERATION
            0x00uy; 0x00uy; 0x00uy; 0x08uy; // ADDITIONAL LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 1
            0x00uy; 0x00uy; 0x00uy; 0x63uy;
        |]
        Assert.True(( v = ans ))
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ReadKey_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "ReadKey_003.txt"

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                PRManager_Test1.defaultSource.I_TNexus, resvkey_me.fromPrim 99UL, true;
                new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0xAABBCCDD01020304UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let ilustub = new CInternalLU_Stub(
            p_LUN = ( fun () -> lun_me.zero ),
            p_EstablishUnitAttention = ( fun itr _ -> () )
        )
        let pm = new PRManager( new CStatus_Stub(), ilustub, lun_me.zero, fname, k )
        let v = pm.ReadKey PRManager_Test1.defaultSource ( itt_me.fromPrim 0u )
        let ans = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // PRGENERATION
            0x00uy; 0x00uy; 0x00uy; 0x10uy; // ADDITIONAL LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 1
            0x00uy; 0x00uy; 0x00uy; 0x63uy;
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 2
            0x01uy; 0x02uy; 0x03uy; 0x04uy;
        |]
        Assert.True(( v = ans ))

        let clearParam = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; //  RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x63uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; //  SERVICE ACTION RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT, ALL_TG_PT, APTPL
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        pm.Clear PRManager_Test1.defaultSource ( itt_me.fromPrim 0u ) ( PR_TYPE.NO_RESERVATION ) 24u ( PooledBuffer.Rent clearParam ) |> ignore

        let v2 = pm.ReadKey PRManager_Test1.defaultSource ( itt_me.fromPrim 0u )
        let ans2 = [|
            0x00uy; 0x00uy; 0x00uy; 0x01uy; // PRGENERATION
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // ADDITIONAL LENGTH
        |]
        Assert.True(( v2 = ans2 ))

        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ReadReservation_001() =
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, "", k )
        let v = pm.ReadReservation PRManager_Test1.defaultSource ( itt_me.fromPrim 0u )
        let ans = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // PRGENERATION
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // ADDITIONAL LENGTH
        |]
        Assert.True(( v = ans ))
        k.NoticeTerminate()

    [<Fact>]
    member this.ReadReservation_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "ReadReservation_002.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let v = pm.ReadReservation PRManager_Test1.defaultSource ( itt_me.fromPrim 0u )
        let ans = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // PRGENERATION
            0x00uy; 0x00uy; 0x00uy; 0x10uy; // ADDITIONAL LENGTH
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // Reserved
            0x06uy;                         // SCOPE, TYPE
            0x00uy; 0x00uy;                 // Obsolute
        |]
        Assert.True(( v = ans ))
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ReadReservation_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "ReadReservation_003.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
                new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target003", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0x1UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let v = pm.ReadReservation PRManager_Test1.defaultSource ( itt_me.fromPrim 0u )
        let ans = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // PRGENERATION
            0x00uy; 0x00uy; 0x00uy; 0x10uy; // ADDITIONAL LENGTH
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // Reserved
            0x08uy;                         // SCOPE, TYPE
            0x00uy; 0x00uy;                 // Obsolute
        |]
        Assert.True(( v = ans ))
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ReportCapabilities_001() =
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, "", k )
        let v = pm.ReportCapabilities PRManager_Test1.defaultSource ( itt_me.fromPrim 0u )
        let ans = [|
            0x00uy; 0x08uy; // LENGTH
            0x0Duy;         // CRH, SPI_C, ATP_, PTPL_C
            0x80uy;         // TMV, PTPL_A
            0xEAuy; 0x01uy; // PERSISTENT RESERVATION TYPE MASK
            0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v = ans ))
        k.NoticeTerminate()

    [<Fact>]
    member this.ReportCapabilities_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "ReportCapabilities_002.txt"

        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let v = pm.ReportCapabilities PRManager_Test1.defaultSource ( itt_me.fromPrim 0u )
        let ans = [|
            0x00uy; 0x08uy; // LENGTH
            0x0Duy;         // CRH, SPI_C, ATP_, PTPL_C
            0x81uy;         // TMV, PTPL_A
            0xEAuy; 0x01uy; // PERSISTENT RESERVATION TYPE MASK
            0x00uy; 0x00uy; // Reserved
        |]
        Assert.True(( v = ans ))
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ReadFullStatus_001() =
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, "", k )
        let v = pm.ReadFullStatus PRManager_Test1.defaultSource ( itt_me.fromPrim 0u )
        let ans = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // PRGENERATION
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // ADDITIONAL LENGTH
        |]
        Assert.True(( v = ans ))
        k.NoticeTerminate()

    [<Fact>]
    member this.ReadFullStatus_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "ReadFullStatus_002.txt"

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
                new ITNexus( "i", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us ), resvkey_me.fromPrim 1UL, false;
                new ITNexus( "initiator333", isid_me.fromElem ( 2uy <<< 6 ) 3uy 4us 5uy 6us, "target003", tpgt_me.fromPrim 2us ), resvkey_me.fromPrim 2UL, false;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let v = pm.ReadFullStatus PRManager_Test1.defaultSource ( itt_me.fromPrim 0u )
        let ans = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // PRGENERATION
            0x00uy; 0x00uy; 0x00uy; 0xA8uy; // ADDITIONAL LENGTH

            // descriptor 1(48bytes)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x01uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x01uy;                         // ALL_TG_PT(0), R_HOLDER(1)
            0x07uy;                         // SCOPE, TYPE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x00uy; 0x01uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x18uy; // ADDITIONAL DESCRIPTOR LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "i"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy;

            // descriptor 2(60bytes)
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x01uy;                         // ALL_TG_PT(0), R_HOLDER(1)
            0x07uy;                         // SCOPE, TYPE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // ADDITIONAL DESCRIPTOR LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator111"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy; 0x00uy;

            // descriptor 3(60bytes)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x02uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x01uy;                         // ALL_TG_PT(0), R_HOLDER(1)
            0x07uy;                         // SCOPE, TYPE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x00uy; 0x02uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // ADDITIONAL DESCRIPTOR LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator333"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "830004050006"
            0x00uy; 0x00uy; 0x00uy;

        |]
        Assert.True(( v = ans ))
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.ReadFullStatus_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "ReadFullStatus_003.txt"

        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                new ITNexus( "initiator333", isid_me.fromElem ( 2uy <<< 6 ) 3uy 4us 5uy 6us, "target003", tpgt_me.fromPrim 2us ), resvkey_me.fromPrim 2UL, false;
                new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 0us ), resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
                new ITNexus( "initiator222", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us ), resvkey_me.fromPrim 1UL, true;
            |]
            fname
        let k = new HKiller() :> IKiller
        let pm = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let v = pm.ReadFullStatus PRManager_Test1.defaultSource ( itt_me.fromPrim 0u )
        let ans = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // PRGENERATION
            0x00uy; 0x00uy; 0x00uy; 0xB4uy; // ADDITIONAL LENGTH

            // descriptor 2(60bytes)
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x00uy;                         // ALL_TG_PT(0), R_HOLDER(0)
            0x00uy;                         // SCOPE, TYPE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // ADDITIONAL DESCRIPTOR LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator111"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy; 0x00uy;

            // descriptor 2(60bytes)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x01uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x01uy;                         // ALL_TG_PT(0), R_HOLDER(1)
            0x05uy;                         // SCOPE, TYPE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x00uy; 0x01uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // ADDITIONAL DESCRIPTOR LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator222"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy; 0x00uy;

            // descriptor 3(60bytes)
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x02uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x00uy;                         // ALL_TG_PT(0), R_HOLDER(0)
            0x00uy;                         // SCOPE, TYPE
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
            0x00uy; 0x02uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // ADDITIONAL DESCRIPTOR LENGTH
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator333"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "830004050006"
            0x00uy; 0x00uy; 0x00uy;

        |]
        Assert.True(( v = ans ))
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName
