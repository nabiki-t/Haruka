//=============================================================================
// Haruka Software Storage.
// ScsiCDB.fs : Define SCSI CDB data types, and translate binary data to 
// SCSI CDB data types function.
// 

//=============================================================================
// Namespace declaration

/// <summary>
///   This component defines data types that represent SCSI CDB data structure.
/// </summary>
namespace Haruka.BlockDeviceLU

//=============================================================================
// Import declaration

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Type definition

/// <summary>
/// CDBTypes shows the typed of SCSI CDB.
/// </summary>
type CDBTypes =
    /// SPC-3 6.2 CHANGE ALIASES command
    | ChangeAliases

    /// SPC-3 6.3 EXTENDED COPY command
    | ExtendedCopy

    /// SPC-3 6.4 INQUIRY command
    | Inquiry

    /// SPC-3 6.5 LOG SELECT command
    | LogSelect

    /// SPC-3 6.6 LOG SENSE command
    | LogSense

    /// SPC-3 6.7 MODE SELECT(6), 6.8 MODE SELECT(10) command
    | ModeSelect

    /// SPC-3 6.9 MODE SENSE(6), 6.10 MODE SENSE(10) command
    | ModeSense

    /// SPC-3 6.11 PERSISTENT RESERVE IN command
    | PersistentReserveIn

    /// SPC-3 6.12 PERSISTENT RESERVE OUT command
    | PersistentReserveOut

    /// SPC-3 6.13 PREVENT ALLOW MEDIUM REMOVAL command
    | PreventAllowMediumRemoval

    /// SPC-3 6.14 READ ATTRIBUTE command
    | ReadAttribute

    /// SPC-3 6.15 READ BUFFER command
    | ReadBuffer

    /// SPC-3 6.16 READ MEDIA SERIAL NUMBER command
    | ReadMediaSerialNumber

    /// SPC-3 6.17 RECEIVE COPY RESULTS command
    | ReceiveCopyResults

    /// SPC-3 6.18 RECEIVE DIAGNOSTIC RESULTS command
    | ReceiveDiagnosticResults

    /// SPC-3 6.19 REPORT ALIASES command
    | ReportAliases

    /// SPC-3 6.20 REPORT DEVICE IDENTIFIER command
    | ReportDeviceIdentifier

    /// SPC-3 6.21 REPORT LUNS command
    | ReportLUNs

    /// SPC-3 6.22 REPORT PRIORITY command
    | ReportPriority

    /// SPC-3 6.23 REPORT SUPPORTED OPERATION CODES command
    | ReportSupportedOperationCodes

    /// SPC-3 6.24 REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS command
    | ReportSupportedTaskManagementFunctions

    /// SPC-3 6.25 REPORT TARGET PORT GROUPS command
    | ReportTargetPortGroups

    /// SPC-3 6.26 REPORT TIMESTAMP command
    | ReportTimestamp

    /// SPC-3 6.27 REQUEST SENSE command
    | RequestSense

    /// SPC-3 6.28 SEND DIAGNOSTIC command
    | SendDiagnostic

    /// SPC-3 6.29 SET DEVICE IDENTIFIER command
    | SetDeviceIdentifier

    /// SPC-3 6.30 SET PRIORITY command
    | SetPriority

    /// SPC-3 6.31 SET TARGET PORT GROUPS command
    | SetTargetPortGroups

    /// SPC-3 6.32 SET TIMESTAMP command
    | SetTimestamp

    /// SPC-3 6.33 TEST UNIT READY command
    | TestUnitReady

    /// SPC-3 6.34 WRITE ATTRIBUTE command
    | WriteAttribute

    /// SPC-3 6.35 WRITE BUFFER command
    | WriteBuffer

    /// SPC-3 8.3.2 ACCESS CONTROL IN command
    | AccessControlIn

    /// SPC-3 8.3.3 ACCESS CONTROL OUT command
    | AccessControlOut

    /// SBC-2 5.2 FORMAT UNIT command
    | FormatUnit

    /// SBC-2 5.3 PRE-FETCH(10), 5.4 PRE-FETCH(16) command
    | PreFetch

    /// SBC-2 5.5 READ(6), 5.6 READ(10), 5.7 READ(12), 5.8 READ(16), 5.9 READ(32) command
    | Read

    /// SBC-2 5.10 READ CAPACITY(10), 5.11 READ CAPACITY(16) command
    | ReadCapacity

    /// SBC-2 5.12 READ DEFECT DATA(10), 5.13 READ DEFECT DATA(12) command
    | ReadDefectData

    /// SBC-2 5.14 READ LONG(10), 5.15 READ LONG(16) command
    | ReadLong

    /// SBC-2 5.16 REASSIGN BLOCKS command
    | ReassignBlocks

    /// SBC-2 5.17 START STOP UNIT command
    | StartStopUnit

    /// SBC-2 5.18 SYNCHRONIZE CACHE(10), 5.19 SYNCHRONIZE CACHE(16) command
    | SynchronizeCache

    /// SBC-2 5.20 VERIFY(10), 5.21 VERIFY(12), 5.22 VERIFY(16), 5.23 VERIFY(32) command
    | Verify

    /// SBC-2 5.24 WRITE(6), 5.25 WRITE(10), 5.26 WRITE(12), 5.27 WRITE(16), 5.28 WRITE(32) command
    | Write

    /// SBC-2 5.29 WRITE AND VERIFY(10), 5.30 WRITE AND VERIFY(12), 5.31 WRITE AND VERIFY(16), 5.32 WRITE AND VERIFY(32) command
    | WriteAndVerify

    /// SBC-2 5.33 WRITE LONG(10), 5.34 WRITE LONG(16) command
    | WriteLong

    /// SBC-2 5.35 WRITE SAME(10), 5.36 WRITE SAME(16), 5.37 WRITE SAME(32) command
    | WriteSame

    /// SBC-2 5.38 XDREAD(10), 5.39 XDREAD(32) command
    | XDRead

    /// SBC-2 5.40 XDWRITE(10), 5.41 XDWRITE(32) command
    | XDWrite

    /// SBC-2 5.42 XDWRITEREAD(10), 5.43 XDWRITEREAD(32) command
    | XDWriteRead

    /// SBC-2 5.44 XPWRITE(10), 5.45 XPWRITE(32) command
    | XPWrite

    /// <summary>
    ///  CDBTypes value to string value.
    /// </summary>
    static member getName =
        function
        | ChangeAliases -> "ChangeAliases"
        | ExtendedCopy -> "ExtendedCopy"
        | Inquiry -> "Inquiry"
        | LogSelect -> "LogSelect"
        | LogSense -> "LogSense"
        | ModeSelect -> "ModeSelect"
        | ModeSense -> "ModeSense"
        | PersistentReserveIn -> "PersistentReserveIn"
        | PersistentReserveOut -> "PersistentReserveOut"
        | PreventAllowMediumRemoval -> "PreventAllowMediumRemoval"
        | ReadAttribute -> "ReadAttribute"
        | ReadBuffer -> "ReadBuffer"
        | ReadMediaSerialNumber -> "ReadMediaSerialNumber"
        | ReceiveCopyResults -> "ReceiveCopyResults"
        | ReceiveDiagnosticResults -> "ReceiveDiagnosticResults"
        | ReportAliases -> "ReportAliases"
        | ReportDeviceIdentifier -> "ReportDeviceIdentifier"
        | ReportLUNs -> "ReportLUNs"
        | ReportPriority -> "ReportPriority"
        | ReportSupportedOperationCodes -> "ReportSupportedOperationCodes"
        | ReportSupportedTaskManagementFunctions -> "ReportSupportedTaskManagementFunctions"
        | ReportTargetPortGroups -> "ReportTargetPortGroups"
        | ReportTimestamp -> "ReportTimestamp"
        | RequestSense -> "RequestSense"
        | SendDiagnostic -> "SendDiagnostic"
        | SetDeviceIdentifier -> "SetDeviceIdentifier"
        | SetPriority -> "SetPriority"
        | SetTargetPortGroups -> "SetTargetPortGroups"
        | SetTimestamp -> "SetTimestamp"
        | TestUnitReady -> "TestUnitReady"
        | WriteAttribute -> "WriteAttribute"
        | WriteBuffer -> "WriteBuffer"
        | AccessControlIn -> "AccessControlIn"
        | AccessControlOut -> "AccessControlOut"
        | FormatUnit -> "FormatUnit"
        | PreFetch -> "PreFetch"
        | Read -> "Read"
        | ReadCapacity -> "ReadCapacity"
        | ReadDefectData -> "ReadDefectData"
        | ReadLong -> "ReadLong"
        | ReassignBlocks -> "ReassignBlocks"
        | StartStopUnit -> "StartStopUnit"
        | SynchronizeCache -> "SynchronizeCache"
        | Verify -> "Verify"
        | Write -> "Write"
        | WriteAndVerify -> "WriteAndVerify"
        | WriteLong -> "WriteLong"
        | WriteSame -> "WriteSame"
        | XDRead -> "XDRead"
        | XDWrite -> "XDWrite"
        | XDWriteRead -> "XDWriteRead"
        | XPWrite -> "XPWrite"

/// <summary>
///   Common interface of CDB data structure.
/// </summary>
type ICDB =

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the type of CDB.
    /// </summary>
    /// <returns>
    ///   The CDBTypes value corresponds this CDB.
    /// </returns>
    /// <remarks>
    ///   CDBTypes value represents functionality of the CDB, size of CDB is ignored.
    /// </remarks>
    abstract Type : CDBTypes

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get OPERATION CODE value of the CDB.
    /// </summary>
    /// <returns>
    ///   The OPERATION CODE field value that is set in the CDB.
    /// </returns>
    abstract OperationCode : byte

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get SERVICE ACTION value of the CDB.
    /// </summary>
    /// <returns>
    ///   The SERVICE ACTION field value that is set in the CDB.
    ///   If the CDB does not have SERVICE ACTION field, it returns 0.
    /// </returns>
    abstract ServiceAction : uint16

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get NACA(Normal ACA) bit in CONTROL field of the CDB.
    /// </summary>
    /// <returns>
    ///   The NACA value that is set in the CDB.
    /// </returns>
    abstract NACA : bool

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get LINK bit in CONTROL field of the CDB.
    /// </summary>
    /// <returns>
    ///   The LINK value that is set in the CDB.
    /// </returns>
    abstract LINK : bool

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get description string.
    /// </summary>
    /// <returns>
    ///   Strings that descript this CDB for output log message.
    /// </returns>
    abstract DescriptString : string


/// <summary>
///   CHANGE ALIASES CDB data structure.
/// </summary>
type ChangeAliasesCDB =
    {
        /// OPERATION CODE (A4h)
        OperationCode : byte;

        /// SERVICE ACTION (0Bh)
        ServiceAction : uint16;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ChangeAliases

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // Get SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            this.ServiceAction

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "CHANGE ALIASES,NACA=%b,LINK=%b,ServiceAction=0x%04X,ParameterListLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ServiceAction
                this.ParameterListLength

/// <summary>
///   EXTENDED COPY CDB data structure.
/// </summary>
type ExtendedCopyCDB =
    {
        /// OPERATION CODE (83h)
        OperationCode : byte;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes = ExtendedCopy

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist.
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "EXTENDED COPY,NACA=%b,LINK=%b,ParameterListLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ParameterListLength

/// <summary>
///   INQUIRY CDB data structure.
/// </summary>
type InquiryCDB =
    {
        /// OPERATION CODE (12h)
        OperationCode : byte;

        /// EVPD bit
        EVPD : bool

        /// PAGE CODE
        PageCode : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            Inquiry

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist.
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "INQUIRY,NACA=%b,LINK=%b,EVPD=%b,PageCode=%d,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.EVPD
                this.PageCode
                this.AllocationLength

/// <summary>
///   LOG SELECT data structure.
/// </summary>
type LogSelectCDB =
    {
        /// OPERATION CODE (4Ch)
        OperationCode : byte;

        /// PCR(Parameter Code Reset) bit
        PCR : bool

        /// SP(Save Parameter) bit
        SP : bool;

        /// PC(Page Control) field
        PC : byte;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            LogSelect

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist.
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "LOG SELECT,NACA=%b,LINK=%b,PCR=%b,SP=%b,PC=0x%02X,ParameterListLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.PCR
                this.SP
                this.PC
                this.ParameterListLength

/// <summary>
///   LOG SENSE data structure.
/// </summary>
type LogSenseCDB =
    {
        /// OPERATION CODE (4Dh)
        OperationCode : byte;

        /// PPC(Parameter Pointer Control) bit
        PPC : bool

        /// SP(Save Parameter) bit
        SP : bool;

        /// PC(Page Control) field
        PC : byte;

         /// PARAMETER POINTER
        ParameterPointer : uint16;

        /// ALLOCATION LENGTH
        AllocationLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            LogSense

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist.
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "LOG SENSE,NAC=%b,LINK=%b,PPC=%b,SP=%b,PC=0x%02X,ParameterPointer=0x%04X,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.PPC
                this.SP
                this.PC
                this.ParameterPointer
                this.AllocationLength

/// <summary>
///   MODE SELECT data structure.
/// </summary>
type ModeSelectCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (15h) : MODE SELECT(6)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (55h) : MODE SELECT(10)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// PF(Page Format) bit
        PF : bool

        /// SP(Save Pages) bit
        SP : bool;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ModeSelect

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist.
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "%s,NACA=%b,LINK=%b,PF=%b,SP=%b,ParameterListLength=%d"
                (
                    match this.OperationCode with
                    | 0x15uy -> "MODE SELECT(6)"
                    | 0x55uy -> "MODE SELECT(10)"
                    | _ -> "MODE SELECT(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.PF
                this.SP
                this.ParameterListLength

/// <summary>
///   MODE SENSE data structure.
/// </summary>
type ModeSenseCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (1Ah)    : MODE SENSE(6)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (15h)    : MODE SENSE(10)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// LLBAA(Long LBA Accepted) bit
        LLBAA : bool;

        /// DBD(Disable Block Descriptors) bit
        DBD : bool

        /// PC(Page Control) bit
        PC : byte;

        /// PAGE CODE
        PageCode : byte;

        // SUBPAGE CODE
        SubPageCode : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ModeSense

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist.
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "%s,NACA=%b,LINK=%b,LLBAA=%b,DBD=%b,PC=0x%02X,PageCode=%d,SubPageCode=%d,AllocationLength=%d"
                (
                    match this.OperationCode with
                    | 0x1Auy -> "MODE SENSE(6)"
                    | 0x5Auy -> "MODE SENSE(10)"
                    | _ -> "MODE SENSE(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.LLBAA
                this.DBD
                this.PC
                this.PageCode
                this.SubPageCode
                this.AllocationLength

/// <summary>
///   PERSISTENT RESERVE IN data structure.
/// </summary>
type PersistentReserveInCDB =
    {
        /// OPERATION CODE (5Eh)
        OperationCode : byte;

        /// SERVICE ACTION
        ServiceAction : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            PersistentReserveIn

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            let saStr =
                match this.ServiceAction with
                | 0x00uy -> "READ KEYS"
                | 0x01uy -> "READ RESERVATION"
                | 0x02uy -> "REPORT CAPABILITIES"
                | 0x03uy -> "READ FULL STATUS"
                | _ -> sprintf "Unknown(0x%02X)" this.ServiceAction
            sprintf
                "PERSISTENT RESERVE IN,NACA=%b,LINK=%b,ServiceAction=%s,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                saStr
                this.AllocationLength

/// <summary>
///   PERSISTENT RESERVE OUT data structure.
/// </summary>
type PersistentReserveOutCDB =
    {
        /// OPERATION CODE (5Fh)
        OperationCode : byte;

        /// SERVICE ACTION
        ServiceAction : byte;

        /// SCOPE
        Scope : byte;

        // TYPE
        PRType : PR_TYPE;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =PersistentReserveOut

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy
 
        // Get description string.
        member this.DescriptString : string =
            let saStr =
                match this.ServiceAction with
                | 0x00uy -> "REGISTER"
                | 0x01uy -> "RESERVE"
                | 0x02uy -> "RELEASE"
                | 0x03uy -> "CLEAR"
                | 0x04uy -> "PREEMPT"
                | 0x05uy -> "PREEMPT AND ABORT"
                | 0x06uy -> "REGISTER AND IGNORE EXISTING KEY"
                | 0x07uy -> "REGISTER AND MOVE"
                | _ -> sprintf "Unknown(0x%02X)" this.ServiceAction
            sprintf
                "PERSISTENT RESERVE OUT,NACA=%b,LINK=%b,ServiceAction=%s,Scope=0x%02X,Type=%s,ParameterListLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                saStr
                this.Scope
                ( PR_TYPE.toStringName this.PRType )
                this.ParameterListLength

/// <summary>
///   PREVENT ALLOW MEDIUM REMOVAL data structure.
/// </summary>
type PreventAllowMediumRemovalCDB =
    {
        /// OPERATION CODE (1Eh)
        OperationCode : byte;

        /// PREVENT
        Prevent : byte;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            PreventAllowMediumRemoval

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist.
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "PREVENT ALLOW MEDIUM REMOVAL,NACA=%b,LINK=%b,Prevent=0x%02X"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.Prevent

/// <summary>
///   READ ATTRIBUTE data structure.
/// </summary>
type ReadAttributeCDB =
    {
        /// OPERATION CODE (8Ch)
        OperationCode : byte;

        /// SERVICE ACTION
        ServiceAction : byte;

        /// VOLUME NUMBER
        VolumeNumber : byte;

        /// PARTITION NUMBER
        PartitionNumber : byte;

        /// FIRST ATTRIBUTE IDENTIFIER
        FirstAttributeIdentifier : uint16;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReadAttribute

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            let saStr =
                match this.ServiceAction with
                | 0x00uy -> "ATTRIBUTE VALUES"
                | 0x01uy -> "ATTRIBUTE LIST"
                | 0x02uy -> "VOLUME LIST"
                | 0x03uy -> "PARTITION LIST"
                | _ -> sprintf "Unknown(0x%02X)" this.ServiceAction
            sprintf
                "READ ATTRIBUTE,NAC=%b,LINK=%b,ServiceAction=%s,VolumeNumber=%d,PartitionNumber=%d,FirstAttributeIdentifier=%d,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                saStr
                this.VolumeNumber
                this.PartitionNumber
                this.FirstAttributeIdentifier
                this.AllocationLength

/// <summary>
///   READ BUFFER data structure.
/// </summary>
type ReadBufferCDB =
    {
        /// OPERATION CODE (3Ch)
        OperationCode : byte;

        /// MODE
        Mode : byte;

        /// BUFFER ID
        BufferID : byte;

        /// BUFFER OFFSET
        BufferOffset : uint32

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReadBuffer

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "READ BUFFER,NACA=%b,LINK=%b,Mode=0x%02X,BufferID=%d,BufferOffset=%d,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.Mode
                this.BufferID
                this.BufferOffset
                this.AllocationLength

/// <summary>
///   READ MEDIA SERIAL NUMBER data structure.
/// </summary>
type ReadMediaSerialNumberCDB =
    {
        /// OPERATION CODE (ABh)
        OperationCode : byte;

        /// SERVICE ACTION(01h)
        ServiceAction : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReadMediaSerialNumber

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "READ MEDIA SERIAL NUMBER,NACA=%b,LINK=%b,ServiceAction=0x%02X,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ServiceAction
                this.AllocationLength

/// <summary>
///   RECEIVE COPY RESULTS data structure.
/// </summary>
type ReceiveCopyResultsCDB =
    {
        /// OPERATION CODE (84h)
        OperationCode : byte;

        /// SERVICE ACTION
        ServiceAction : byte;

        /// LIST IDENTIFIER
        ListIdentifier : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReceiveCopyResults

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            let saStr =
                match this.ServiceAction with
                | 0x00uy -> "COPY RESULTS"
                | 0x01uy -> "RECEIVE DATA"
                | 0x03uy -> "OPERATING PARAMETERS"
                | 0x04uy -> "FAILED SEGMENT DETAILS"
                | _ -> sprintf "Unknown(0x%02X)" this.ServiceAction
            sprintf
                "RECEIVE COPY RESULTS,NACA=%b,LINK=%b,ServiceAction=%s,ListIdentifier=%d,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                saStr
                this.ListIdentifier
                this.AllocationLength

/// <summary>
///   RECEIVE DIAGNOSTIC RESULTS data structure.
/// </summary>
type ReceiveDiagnosticResultsCDB =
    {
        /// OPERATION CODE (1Ch)
        OperationCode : byte;

        /// PAGE CODE
        PageCode : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReceiveDiagnosticResults

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist.
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy
        
        // Get description string.
        member this.DescriptString : string =
            sprintf
                "RECEIVE DIAGNOSTIC RESULTS,NACA=%b,LINK=%b,PageCode=0x%02X,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.PageCode
                this.AllocationLength

/// <summary>
///   REPORT ALIASES data structure.
/// </summary>
type ReportAliasesCDB =
    {
        /// OPERATION CODE (A3h)
        OperationCode : byte;

        /// SERVICE ACTION(0Bh)
        ServiceAction : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReportAliases

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "REPORT ALIASES,NACA=%b,LINK=%b,ServiceAction=0x%02X,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ServiceAction
                this.AllocationLength

/// <summary>
///   REPORT DEVICE IDENTIFIER data structure.
/// </summary>
type ReportDeviceIdentifierCDB =
    {
        /// OPERATION CODE (A3h)
        OperationCode : byte;

        /// SERVICE ACTION(05h)
        ServiceAction : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReportDeviceIdentifier

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "REPORT DEVICE IDENTIFIER,NACA=%b,LINK=%b,ServiceAction=0x%02X,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ServiceAction
                this.AllocationLength

/// <summary>
///   REPORT LUNS data structure.
/// </summary>
type ReportLUNsCDB =
    {
        /// OPERATION CODE (A0h)
        OperationCode : byte;

        /// SELECT REPORT
        SelectReport : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint32

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReportLUNs

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist.
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy
 
        // Get description string.
        member this.DescriptString : string =
            sprintf
                "REPORT LUNS,NACA=%b,LINK=%b,SelectReport=%d,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.SelectReport
                this.AllocationLength

/// <summary>
///   REPORT PRIORITY data structure.
/// </summary>
type ReportPriorityCDB =
    {
        /// OPERATION CODE (A3h)
        OperationCode : byte;

        /// SERVICE ACTION(0Eh)
        ServiceAction : byte;

        /// PRIORITY REPORTED
        PriorityReported : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReportPriority

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "REPORT PRIORITY,NACA=%b,LINK=%b,ServiceAction=0x%02X,PriorityReported=0x%02X,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ServiceAction
                this.PriorityReported
                this.AllocationLength

/// <summary>
///   REPORT SUPPORTED OPERATION CODES data structure.
/// </summary>
type ReportSupportedOperationCodesCDB =
    {
        /// OPERATION CODE (A3h)
        OperationCode : byte;

        /// SERVICE ACTION(0Ch)
        ServiceAction : byte;

        /// REPORTIONG OPTIONS
        ReportingOptions : byte;

        /// REQUESTED OPERATION CODE
        RequestedOperationCode : byte;

        /// REQUESTED SERVICE ACTION
        RequestedServiceAction : uint16

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReportSupportedOperationCodes

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "REPORT SUPPORTED OPERATION CODES,NACA=%b,LINK=%b,ReportingOptions=%d,RequestedOperationCode=%d,RequestedServiceAction=%d,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ReportingOptions
                this.RequestedOperationCode
                this.RequestedServiceAction
                this.AllocationLength

/// <summary>
///   REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS data structure.
/// </summary>
type ReportSupportedTaskManagementFunctionsCDB =
    {
        /// OPERATION CODE (A3h)
        OperationCode : byte;

        /// SERVICE ACTION(0Dh)
        ServiceAction : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReportSupportedTaskManagementFunctions

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS,NACA=%b,LINK=%b,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.AllocationLength

/// <summary>
///   REPORT TARGET PORT GROUPS data structure.
/// </summary>
type ReportTargetPortGroupsCDB =
    {
        /// OPERATION CODE (A3h)
        OperationCode : byte;

        /// SERVICE ACTION(0Ah)
        ServiceAction : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReportTargetPortGroups

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "REPORT TARGET PORT GROUPS,NACA=%b,LINK=%b,ServiceAction=0x%02X,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ServiceAction
                this.AllocationLength

/// <summary>
///   REPORT TIMESTAMP data structure.
/// </summary>
type ReportTimestampCDB =
    {
        /// OPERATION CODE (A3h)
        OperationCode : byte;

        /// SERVICE ACTION(0Fh)
        ServiceAction : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReportTimestamp

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "REPORT TIMESTAMP,NACA=%b,LINK=%b,ServiceAction=0x%02X,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ServiceAction
                this.AllocationLength

/// <summary>
///   REQUEST SENSE data structure.
/// </summary>
type RequestSenseCDB =
    {
        /// OPERATION CODE (03h)
        OperationCode : byte;

        /// DESC(Descriptor Format) bit
        DESC : bool;

        /// ALLOCATION LENGTH
        AllocationLength : byte;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            RequestSense

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "REQUEST SENSE,NACA=%b,LINK=%b,DESC=%b,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.DESC
                this.AllocationLength

/// <summary>
///   SEND DIAGNOSTIC data structure.
/// </summary>
type SendDiagnosticCDB =
    {
        /// OPERATION CODE (1Dh)
        OperationCode : byte;

        /// SELF-TEST CODE
        SelfTestCode : byte;

        /// PF(Page Format) bit
        PF : bool;

        /// SELF TEST bit
        SelfTest : bool;

        /// DEV OFF L( SCSI Target Device Offline ) bit
        DevOffL : bool;

        /// UNIT OFF L( Unit Offline ) bit
        UnitOffL : bool;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            SendDiagnostic

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "SEND DIAGNOSTIC,NACA=%b,LINK=%b,SelfTestCode=0x%02X,PF=%b,SelfTest=%b,DevOffL=%b,UnitOffL=%b,ParameterListLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.SelfTestCode
                this.PF
                this.SelfTest
                this.DevOffL
                this.UnitOffL
                this.ParameterListLength

/// <summary>
///   SET DEVICE IDENTIFIER data structure.
/// </summary>
type SetDeviceIdentifierCDB =
    {
        /// OPERATION CODE (A4h)
        OperationCode : byte;

        /// SERVICE ACTION (06h)
        ServiceAction : byte;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            SetDeviceIdentifier

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "SET DEVICE IDENTIFIER,NACA=%b,LINK=%b,ParameterListLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ParameterListLength

/// <summary>
///   SET PRIORITY data structure.
/// </summary>
type SetPriorityCDB =
    {
        /// OPERATION CODE (A4h)
        OperationCode : byte;

        /// SERVICE ACTION (0Eh)
        ServiceAction : byte;

        /// I_T_L NEXUS TO SET
        I_T_LNexusToSet : byte;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            SetPriority

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "SET PRIORITY,NACA=%b,LINK=%b,ServiceAction=0x%02X,I_T_LNexusToSet=0x%02X,ParameterListLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ServiceAction
                this.I_T_LNexusToSet
                this.ParameterListLength

/// <summary>
///   SET TARGET PORT GROUPS data structure.
/// </summary>
type SetTargetPortGroupsCDB =
    {
        /// OPERATION CODE (A4h)
        OperationCode : byte;

        /// SERVICE ACTION (0Ah)
        ServiceAction : byte;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            SetTargetPortGroups

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "SET TARGET PORT GROUPS,NACA=%b,LINK=%b,ServiceAction=0x%02X,ParameterListLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ServiceAction
                this.ParameterListLength

/// <summary>
///   SET TIMESTAMP data structure.
/// </summary>
type SetTimestampCDB =
    {
        /// OPERATION CODE (A4h)
        OperationCode : byte;

        /// SERVICE ACTION (0Fh)
        ServiceAction : byte;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            SetTimestamp

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "SET TIMESTAMP,NACA=%b,LINK=%b,ServiceAction=0x%02X,ParameterListLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ServiceAction
                this.ParameterListLength

/// <summary>
///   TEST UNIT READY data structure.
/// </summary>
type TestUnitReadyCDB =
    {
        /// OPERATION CODE (00h)
        OperationCode : byte;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            TestUnitReady

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "TEST UNIT READY,NACA=%b,LINK=%b"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK

/// <summary>
///   WRITE ATTRIBUTE data structure.
/// </summary>
type WriteAttributeCDB =
    {
        /// OPERATION CODE (80h)
        OperationCode : byte;

        /// VALUME NUMBER
        VolumeNumber : byte;

        /// PARTITION NUMBER
        PartitionNumber : byte;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            WriteAttribute

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "WRITE ATTRIBUTE,NACA=%b,LINK=%b,VolumeNumber=%d,PartitionNumber=%d,ParameterListLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.VolumeNumber
                this.PartitionNumber
                this.ParameterListLength

/// <summary>
///   WRITE BUFFER data structure.
/// </summary>
type WriteBufferCDB =
    {
        /// OPERATION CODE (3Bh)
        OperationCode : byte;

        /// MODE
        Mode : byte;

        /// BUFFER ID
        BufferID : byte;

        /// BUFFER OFFSET
        BufferOffset : uint32;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            WriteBuffer

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "WRITE BUFFER,NACA=%b,LINK=%b,Mode=0x%02X,BufferID=0x%02X,BufferOffset=%d,ParameterListLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.Mode
                this.BufferID
                this.BufferOffset
                this.ParameterListLength

/// <summary>
///   ACCESS CONTROL IN command REPORT ACL service action data structure.
/// </summary>
type AccessControlIn_ReportAclCDB =
    {
        /// OPERATION CODE (86h)
        OperationCode : byte;

        /// SERVICE ACTION(00h)
        ServiceAction : byte;

        /// MANAGEMTN IDENTIFIER KEY
        ManagementIdentifierKey : uint64;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            AccessControlIn

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "ACCESS CONTROL IN(ReportAcl),NACA=%b,LINK=%b,ManagementIdentifierKey=0x%016X,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ManagementIdentifierKey
                this.AllocationLength

/// <summary>
///   ACCESS CONTROL IN command REPORT LU DESCRIPTORS service action data structure.
/// </summary>
type AccessControlIn_ReportLUDescriptorsCDB =
    {
        /// OPERATION CODE (86h)
        OperationCode : byte;

        /// SERVICE ACTION(01h)
        ServiceAction : byte;

        /// MANAGEMTN IDENTIFIER KEY
        ManagementIdentifierKey : uint64;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            AccessControlIn

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "ACCESS CONTROL IN(ReportLUDescriptors),NACA=%b,LINK=%b,ManagementIdentifierKey=0x%016X,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ManagementIdentifierKey
                this.AllocationLength


/// <summary>
///   ACCESS CONTROL IN command REPORT ACCESS CONTROLS LOG service action data structure.
/// </summary>
type AccessControlIn_ReportAccessControlsLogCDB =
    {
        /// OPERATION CODE (86h)
        OperationCode : byte;

        /// SERVICE ACTION(02h)
        ServiceAction : byte;

        /// MANAGEMTN IDENTIFIER KEY
        ManagementIdentifierKey : uint64;

        /// LOG PORTION
        LogPortion : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            AccessControlIn

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "ACCESS CONTROL IN(ReportAccessControlsLog),NACA=%b,LINK=%b,ManagementIdentifierKey=0x%016X,LogPortion=%d,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ManagementIdentifierKey
                this.LogPortion
                this.AllocationLength

/// <summary>
///   ACCESS CONTROL IN command REPORT OVERRIDE LOCKOUT TIMER service action data structure.
/// </summary>
type AccessControlIn_ReportOverrideLockoutTimerCDB =
    {
        /// OPERATION CODE (86h)
        OperationCode : byte;

        /// SERVICE ACTION(03h)
        ServiceAction : byte;

        /// MANAGEMTN IDENTIFIER KEY
        ManagementIdentifierKey : uint64;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            AccessControlIn

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "ACCESS CONTROL IN(ReportOverrideLockoutTimer),NACA=%b,LINK=%b,ManagementIdentifierKey=0x%016X,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ManagementIdentifierKey
                this.AllocationLength

/// <summary>
///   ACCESS CONTROL IN command REPORT REQUEST PROXY TOKEN service action data structure.
/// </summary>
type AccessControlIn_RequestProxyTokenCDB =
    {
        /// OPERATION CODE (86h)
        OperationCode : byte;

        /// SERVICE ACTION(04h)
        ServiceAction : byte;

        /// LUN VALUE
        LUNValue : LUN_T;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            AccessControlIn

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "ACCESS CONTROL IN(ReportAccessControlsLog),NACA=%b,LINK=%b,LUNValue=%s,AllocationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                ( lun_me.toString this.LUNValue )
                this.AllocationLength

/// <summary>
///   ACCESS CONTROL OUT command data structure.
/// </summary>
type AccessControlOutCDB =
    {
        /// OPERATION CODE (87h)
        OperationCode : byte;

        /// SERVICE ACTION
        ServiceAction : byte;

        /// PARAMETER LIST LENGTH
        ParameterListLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            AccessControlIn

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "ACCESS CONTROL OUT,NACA=%b,LINK=%b,ServiceAction=0x%02X,ParameterListLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.ServiceAction
                this.ParameterListLength

/// <summary>
///   FORMAT UNIT command data structure.
/// </summary>
type FormatUnitCDB =
    {
        /// OPERATION CODE (04h)
        OperationCode : byte;

        /// FMTPINFO( Format Protection Information ) bit
        FMTPINFO : bool;

        /// RTO_REQ( Reference Tag Own Request ) bit
        RTO_REQ : bool;

        /// LONGLIST bit
        LONGLIST : bool;

        /// FMTDATA( Format Data ) bit
        FMTDATA : bool;

        /// CMPLIST( Complete List ) bit
        CMPLIST : bool;

        /// DEFECT LIST FORMAT
        DefectListFormat : byte;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            FormatUnit

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "FORMAT UNIT,NACA=%b,LINK=%b,FMTPINFO=%b,RTO_REQ=%b,LONGLIST=%b,FMTDATA=%b,CMPLIST=%b,DefectListFormat=0x%02X"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.FMTPINFO
                this.RTO_REQ
                this.LONGLIST
                this.FMTDATA
                this.CMPLIST
                this.DefectListFormat

/// <summary>
///   PRE-FETCH command data structure.
/// </summary>
type PreFetchCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (34h) : PRE-FETCH(10)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (90h) : PRE-FETCH(16)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// IMMED(Immidiate) bit
        IMMED : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// PREFETCH LENGTH
        PrefetchLength : uint32;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            PreFetch

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "%s,NACA=%b,LINK=%b,IMMED=%b,LogicalBlockAddress=0x%016X,PrefetchLength=%d,GroupNumber=%d"
                (
                    match this.OperationCode with
                    | 0x34uy -> "PRE-FETCH(10)"
                    | 0x90uy -> "PRE-FETCH(16)"
                    | _ -> "PRE-FETCH(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.IMMED
                this.LogicalBlockAddress
                this.PrefetchLength
                this.GroupNumber

/// <summary>
///   READ command data structure.
/// </summary>
type ReadCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (08h) : READ(6)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (28h) : READ(10)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (A8h) : READ(12)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (88h) : READ(16)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// RDPROTECT
        RdProtect : byte;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// FUA( Force Unit Access ) bit
        FUA : bool;

        /// FUA_NV( Force Unit Access Non-volatile ) bit
        FUA_NV : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// TRANSFER LENGTH
        TransferLength : uint32;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            Read

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "%s,NACA=%b,LINK=%b,RdProtect=0x%02X,DPO=%b,FUA=%b,FUA_NV=%b,LogicalBlockAddress=0x%016X,TransferLength=%d,GroupNumber=%d"
                (
                    match this.OperationCode with
                    | 0x08uy -> "READ(6)"
                    | 0x28uy -> "READ(10)"
                    | 0xA8uy -> "READ(12)"
                    | 0x88uy -> "READ(16)"
                    | _ -> "READ(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.RdProtect
                this.DPO
                this.FUA
                this.FUA_NV
                this.LogicalBlockAddress
                this.TransferLength
                this.GroupNumber   

/// <summary>
///   READ(32) command data structure.
/// </summary>
type Read32CDB =
    {
        /// OPERATION CODE (7Fh)
        OperationCode : byte;

        /// CONTROL
        Control : byte;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// ADDITIONAL CDB LENGTH(18h)
        AdditionalCDBLength : byte;

        /// SERVICE ACTION(0009h)
        ServiceAction : uint16;

        /// RDPROTECT
        RDPROTECT : byte;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// FUA( Force Unit Access ) bit
        FUA : bool;

        /// FUA_NV( Force Unit Access Non-volatile ) bit
        FUA_NV : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// EXPECTED INITIAL LOGICAL BLOCK REFERENCE TAG
        ExpectedInitialLogicalBlockReferenceTag : uint32;

        /// EXPECTED LOGICAL BLOCK APPLICATION TAG
        ExpectedLogicalBlockApplicationTag : uint16;

        /// LOGICAL BLOCK APPLICATION TAG MASK
        LogicalBlockApplicationTagMask : uint16;

        /// TRANSFER LENGTH
        TransferLength : uint32;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            Read

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            this.ServiceAction

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "READ(32),NACA=%b,LINK=%b,GroupNumber=%d,AdditionalCDBLength=%d,RDPROTECT=0x%02X,DPO=%b,FUA=%b,FUA_NV=%b,LogicalBlockAddress=0x%016X,\
                ExpectedInitialLogicalBlockReferenceTag=0x%08X,ExpectedLogicalBlockApplicationTag=0x%04X,LogicalBlockApplicationTagMask=0x%04X"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.GroupNumber
                this.AdditionalCDBLength
                this.RDPROTECT
                this.DPO
                this.FUA
                this.FUA_NV
                this.LogicalBlockAddress  
                this.ExpectedInitialLogicalBlockReferenceTag  
                this.ExpectedLogicalBlockApplicationTag  
                this.LogicalBlockApplicationTagMask  

/// <summary>
///   READ CAPACITY command data structure.
/// </summary>
type ReadCapacityCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (25h) : READ CAPACITY(10)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (9Eh) : READ CAPACITY(16)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// <list type="bullet">
        /// <item><description>
        ///   Unused : READ CAPACITY(10)
        /// </description></item>
        /// <item><description>
        ///   SERVICE ACTION(10h) : READ CAPACITY(16)
        /// </description></item>
        /// </list>
        ServiceAction : byte;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// PMI(partial medium indicator) bit
        PMI : bool

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReadCapacity

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "%s,NACA=%b,LINK=%b,LogicalBlockAddress=0x%016X,PMI=%b,AllocationLength=%d"
                (
                    match this.OperationCode with
                    | 0x25uy -> "READ CAPACITY(10)"
                    | 0x9Euy when this.ServiceAction = 0x10uy -> "READ CAPACITY(16)"
                    | _ -> "READ CAPACITY(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.LogicalBlockAddress
                this.PMI
                this.AllocationLength

/// <summary>
///   READ DEFECT DATA command data structure.
/// </summary>
type ReadDefectDataCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (37h) : READ DEFECT DATA(10)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (B7h) : READ DEFECT DATA(12)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// REQ_PLIST( Request Primary Defect List ) bit
        REQ_PLIST : bool;

        /// REQ_GLIST( Request Grown Defect List ) bit
        REQ_GLIST : bool;

        /// DEFECT LIST FORMAT
        DefectListFormat : byte;

        /// ALLOCATION LENGTH
        AllocationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReadDefectData

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "%s,NACA=%b,LINK=%b,REQ_PLIST=%b,REQ_GLIST=%b,DefectListFormat=0x%02X,AllocationLength=%d"
                (
                        match this.OperationCode with
                    | 0x37uy -> "READ DEFECT DATA(10)"
                    | 0xB7uy -> "READ DEFECT DATA(12)"
                    | _ -> "READ DEFECT DATA(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.REQ_PLIST
                this.REQ_GLIST
                this.DefectListFormat
                this.AllocationLength

/// <summary>
///   READ LONG command data structure.
/// </summary>
type ReadLongCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (3Eh) : READ LONG(10)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (9Eh) : READ LONG(16)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// <list type="bullet">
        /// <item><description>
        ///   Unused : READ LONG(10)
        /// </description></item>
        /// <item><description>
        ///   SERVICE ACTION(11h) : READ LONG(16)
        /// </description></item>
        /// </list>
        ServiceAction : byte;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// BYTE TRANSFER LENGTH
        ByteTransferLength : uint16

        /// CORRCT( correct ) bit
        CORRT : bool;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReadLong

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "%s,NACA=%b,LINK=%b,LogicalBlockAddress=0x%016X,ByteTransferLength=%d,CORRT=%b"
                (
                    match this.OperationCode with
                    | 0x3Euy -> "READ LONG(10)"
                    | 0x9Euy when this.ServiceAction = 0x11uy -> "READ LONG(16)"
                    | _ -> "READ LONG(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.LogicalBlockAddress
                this.ByteTransferLength
                this.CORRT

/// <summary>
///   REASSIGN BLOCK command data structure.
/// </summary>
type ReassignBlocksCDB =
    {
        /// OPERATION CODE (07h)
        OperationCode : byte;

        /// LONGLBA( Long LBA ) bit
        LONGLBA : bool;

        /// LONGLIST bit
        LONGLIST : bool;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            ReassignBlocks

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "REASSIGN BLOCKS,NACA=%b,LINK=%b,LONGLBA=%b,LONGLIST=%b"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.LONGLBA
                this.LONGLIST

/// <summary>
///   START STOP UINT command data structure.
/// </summary>
type StartStopUnitCDB =
    {
        /// OPERATION CODE (1Bh)
        OperationCode : byte;

        /// IMMED( Immediate ) bit
        IMMED : bool;

        /// POWER CONDITION
        PowerCondition : byte;

        /// LOEJ( Load Eject ) bit
        LOEJ : bool;

        /// START bit
        Start : bool;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            StartStopUnit

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "START STOP UNIT,NACA=%b,LINK=%b,IMMED=%b,PowerCondition=0x%02X,LOEJ=%b,Start=%b"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.IMMED
                this.PowerCondition
                this.LOEJ
                this.Start

/// <summary>
///   SYNCHRONIZE CACHE command data structure.
/// </summary>
type SynchronizeCacheCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (35h) : SYNCHRONIZE CACHE(10)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (91h) : SYNCHRONIZE CACHE(16)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// SYNC_NV bit
        SyncNV : bool;

        // IMMED( Immediate ) : bit
        IMMED : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// NUMBER OF BLOCKS
        NumberOfBlocks : uint32;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            SynchronizeCache

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "%s,NACA=%b,LINK=%b,SyncNV=%b,IMMED=%b,LogicalBlockAddress=0x%016X,NumberOfBlocks=%d,GroupNumber=%d"
                (
                    match this.OperationCode with
                    | 0x35uy -> "SYNCHRONIZE CACHE(10)"
                    | 0x91uy -> "SYNCHRONIZE CACHE(16)"
                    | _ -> "SYNCHRONIZE CACHE(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.SyncNV
                this.IMMED
                this.LogicalBlockAddress
                this.NumberOfBlocks
                this.GroupNumber

/// <summary>
///   VERIFY command data structure.
/// </summary>
type VerifyCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (2Fh) : VERIFY(10)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (AFh) : VERIFY(12)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (8Fh) : VERIFY(16)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// VRRPROTECT
        VRPROTECT : byte;

        /// DPO bit
        DPO : bool

        /// BYTCHK( Byte Check ) bit
        BYTCHK : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// VERIFICATION LENGTH
        VerificationLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            Verify

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "%s,NACA=%b,LINK=%b,VRPROTECT=0x%02X,DPO=%b,BYTCHK=%b,LogicalBlockAddress=0x%016X,GroupNumber=%d,VerificationLength=%d"
                (
                    match this.OperationCode with
                    | 0x2Fuy -> "VERIFY(10)"
                    | 0xAFuy -> "VERIFY(12)"
                    | 0x8Fuy -> "VERIFY(16)"
                    | _ -> "VERIFY(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.VRPROTECT
                this.DPO
                this.BYTCHK
                this.LogicalBlockAddress
                this.GroupNumber
                this.VerificationLength

/// <summary>
///   VERIFY(32) command data structure.
/// </summary>
type Verify32CDB =
    {
        /// OPERATION CODE (7Fh)
        OperationCode : byte;

        /// CONTROL
        Control : byte;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// ADDITIONAL CDB LENGTH(18h)
        AdditionalCDBLength : byte;

        /// SERVICE ACTION(000Ah)
        ServiceAction : uint16;

        /// VRPROTECT
        VRPROTECT : byte;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// BYTCHK( Byte Check ) bit
        BYTCHK : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// EXPECTED INITIAL LOGICAL BLOCK REFERENCE TAG
        ExpectedInitialLogicalBlockReferenceTag : uint32;

        /// EXPECTED LOGICAL BLOCK APPLICATION TAG
        ExpectedLogicalBlockApplicationTag : uint16;

        /// LOGICAL BLOCK APPLICATION TAG MASK
        LogicalBlockApplicationTagMask : uint16;

        /// VERIFICATION LENGTH
        VerificationLength : uint32;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            Verify

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            this.ServiceAction

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "VERIFY(32),NACA=%b,LINK=%b,GroupNumber=%d,AdditionalCDBLength=%d,VRPROTECT=0x%02X,DPO=%b,BYTCHK=%b,\
                LogicalBlockAddress=0x%016X,ExpectedInitialLogicalBlockReferenceTag=0x%08X,\
                ExpectedLogicalBlockApplicationTag=0x&04X,LogicalBlockApplicationTagMask=0x%04X,VerificationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.GroupNumber
                this.AdditionalCDBLength
                this.VRPROTECT
                this.DPO
                this.BYTCHK
                this.LogicalBlockAddress
                this.ExpectedLogicalBlockApplicationTag
                this.LogicalBlockApplicationTagMask
                this.VerificationLength
                
/// <summary>
///   WRITE command data structure.
/// </summary>
type WriteCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (0Ah) : WRITE(6)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (2Ah) : WRITE(10)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (AAh) : WRITE(12)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (8Ah) : WRITE(16)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// WRPROTECT
        WRPROTECT : byte;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// FUA( Force Unit Access ) bit
        FUA : bool;

        /// FUA_NV( Force Unit Access Non-Volatile ) bit
        FUA_NV : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// TRANSFER LENGTH
        TransferLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            Write

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf 
                "%s,NACA=%b,LINK=%b,WRPROTECT=0x%02X,DPO=%b,FUA=%b,FUA_NV=%b,LogicalBlockAddress=0x%016X,\
                GroupNumber=%d,TransferLength=%d"
                (
                    match this.OperationCode with
                    | 0x0Auy -> "WRITE(6)"
                    | 0x2Auy -> "WRITE(10)"
                    | 0xAAuy -> "WRITE(12)"
                    | 0x8Auy -> "WRITE(16)"
                    | _ -> "WRITE(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.WRPROTECT
                this.DPO
                this.FUA
                this.FUA_NV
                this.LogicalBlockAddress
                this.GroupNumber
                this.TransferLength

/// <summary>
///   WRITE(32) command data structure.
/// </summary>
type Write32CDB =
    {
        /// OPERATION CODE (7Fh)
        OperationCode : byte;

        /// CONTROL
        Control : byte;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// ADDITIONAL CDB LENGTH(18h)
        AdditionalCDBLength : byte;

        /// SERVICE ACTION(000Bh)
        ServiceAction : uint16;

        /// WRPROTECT
        WRPROTECT : byte;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// FUA( Force Unit Access ) bit
        FUA : bool;

        /// FUA_NV( Force Unit Access Non-Volatile ) bit
        FUA_NV : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// EXPECTED INITIAL LOGICAL BLOCK REFERENCE TAG
        ExpectedInitialLogicalBlockReferenceTag : uint32;

        /// EXPECTED LOGICAL BLOCK APPLICATION TAG
        ExpectedLogicalBlockApplicationTag : uint16;

        /// LOGICAL BLOCK APPLICATION TAG MASK
        LogicalBlockApplicationTagMask : uint16;

        /// VERIFICATION LENGTH
        VerificationLength : uint32;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            Write

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            this.ServiceAction

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf 
                "WRITE(32),NACA=%b,LINK=%b,GroupNumber=%d,AdditionalCDBLength=%d,WRPROTECT=0x%02X,DPO=%b,FUA=%b,FUA_NV=%b,\
                LogicalBlockAddress=0x%016X,ExpectedInitialLogicalBlockReferenceTag=0x%08X,ExpectedLogicalBlockApplicationTag=0x%04X,\
                LogicalBlockApplicationTagMask=0x%04X,VerificationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.GroupNumber
                this.AdditionalCDBLength
                this.WRPROTECT
                this.DPO
                this.FUA
                this.FUA_NV
                this.LogicalBlockAddress
                this.ExpectedInitialLogicalBlockReferenceTag
                this.ExpectedLogicalBlockApplicationTag
                this.LogicalBlockApplicationTagMask
                this.VerificationLength

/// <summary>
///   WRITE AND VERIFY command data structure.
/// </summary>
type WriteAndVerifyCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (2Eh) : WRITE AND VERIFY(10)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (AEh) : WRITE AND VERIFY(12)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (8Eh) : WRITE AND VERIFY(16)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// WRPROTECT
        WRPROTECT : byte;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// BYTCHK( Byte Check ) bit
        BYTCHK : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// TRANSFER LENGTH
        TransferLength : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            WriteAndVerify

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "%s,NACA=%b,LINK=%b,WRPROTECT=0x%02X,DPO=%b,BYTCHK=%b,LogicalBlockAddress=0x%016X,GroupNumber=%d,TransferLength=%d"
                (
                    match this.OperationCode with
                    | 0x2Euy -> "WRITE AND VERIFY(10)"
                    | 0xAEuy -> "WRITE AND VERIFY(12)"
                    | 0x8Euy -> "WRITE AND VERIFY(16)"
                    | _ -> "WRITE AND VERIFY(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.WRPROTECT
                this.DPO
                this.BYTCHK
                this.LogicalBlockAddress
                this.GroupNumber
                this.TransferLength

/// <summary>
///   WRITE AND VERIFY(32) command data structure.
/// </summary>
type WriteAndVerify32CDB =
    {
        /// OPERATION CODE (7Fh)
        OperationCode : byte;

        /// CONTROL
        Control : byte;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// ADDITIONAL CDB LENGTH(18h)
        AdditionalCDBLength : byte;

        /// SERVICE ACTION(000Ch)
        ServiceAction : uint16;

        /// WRPROTECT
        WRPROTECT : byte;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// BYTCHK( Byte Check ) bit
        BYTCHK : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// EXPECTED INITIAL LOGICAL BLOCK REFERENCE TAG
        ExpectedInitialLogicalBlockReferenceTag : uint32;

        /// EXPECTED LOGICAL BLOCK APPLICATION TAG
        ExpectedLogicalBlockApplicationTag : uint16;

        /// LOGICAL BLOCK APPLICATION TAG MASK
        LogicalBlockApplicationTagMask : uint16;

        /// VERIFICATION LENGTH
        VerificationLength : uint32;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            WriteAndVerify

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            this.ServiceAction

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "WRITE AND VERIFY(32),NACA=%b,LINK=%b,GroupNumber=%d,AdditionalCDBLength=%d,WRPROTECT=0x%02X,DPO=%b,BYTCHK=%b,\
                LogicalBlockAddress=0x%016X,ExpectedInitialLogicalBlockReferenceTag=0x%08X,ExpectedLogicalBlockApplicationTag=0x%04X\
                LogicalBlockApplicationTagMask=0x%04X,VerificationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.GroupNumber
                this.AdditionalCDBLength
                this.WRPROTECT
                this.DPO
                this.BYTCHK
                this.LogicalBlockAddress
                this.ExpectedInitialLogicalBlockReferenceTag
                this.ExpectedLogicalBlockApplicationTag
                this.LogicalBlockApplicationTagMask
                this.VerificationLength

/// <summary>
///   WRITE LONG command data structure.
/// </summary>
type WriteLongCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (3Fh) : WRITE LONG(10)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (9Fh) : WRITE LONG(16)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// unused : WRITE LONG(10)
        /// SERVICE ACTION(11h) : WRITE LONG(16)
        ServiceAction : byte;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// TRANSFER LENGTH
        TransferLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            WriteLong

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            uint16( this.ServiceAction )

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "%s,NACA=%b,LINK=%b,LogicalBlockAddress=0x%016X,TransferLength=%d"
                (
                    match this.OperationCode with
                    | 0x3Fuy -> "WRITE LONG(10)"
                    | 0x9Fuy when this.ServiceAction = 0x11uy -> "WRITE LONG(16)"
                    | _ -> "WRITE LONG(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.LogicalBlockAddress
                this.TransferLength

/// <summary>
///   WRITE SAME command data structure.
/// </summary>
type WriteSameCDB =
    {
        /// <list type="bullet">
        /// <item><description>
        ///   OPERATION CODE (41h) : WRITE SAME(10)
        /// </description></item>
        /// <item><description>
        ///   OPERATION CODE (93h) : WRITE SAME(16)
        /// </description></item>
        /// </list>
        OperationCode : byte;

        /// WRPROTECT
        WRPROTECT : byte;

        /// PBDATA bit
        PBDATA : bool;

        /// LBDATA bit
        LBDATA : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// NUMBER OF BLOCKS
        NumberOfBlocks : uint32;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            WriteLong

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "%s,NACA=%b,LINK=%b,WRPROTECT=0x%02X,PBDATA=%b,LBDATA=%b,LogicalBlockAddress=0x%016X,GroupNumber=%d,NumberOfBlocks=%d"
                (
                    match this.OperationCode with
                    | 0x41uy -> "WRITE SAME(10)"
                    | 0x93uy -> "WRITE SAME(16)"
                    | _ -> "WRITE SAME(Unknown)"
                )
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.WRPROTECT
                this.PBDATA
                this.LBDATA
                this.LogicalBlockAddress
                this.GroupNumber
                this.NumberOfBlocks

/// <summary>
///   WRITE SAME(32) command data structure.
/// </summary>
type WriteSame32CDB =
    {
        /// OPERATION CODE (7Fh)
        OperationCode : byte;

        /// CONTROL
        Control : byte;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// ADDITIONAL CDB LENGTH(18h)
        AdditionalCDBLength : byte;

        /// SERVICE ACTION(000Dh)
        ServiceAction : uint16;

        /// WRPROTECT
        WRPROTECT : byte;

        /// PBDATA bit
        PBDATA : bool;

        /// LBDATA bit
        LBDATA : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// EXPECTED INITIAL LOGICAL BLOCK REFERENCE TAG
        ExpectedInitialLogicalBlockReferenceTag : uint32;

        /// EXPECTED LOGICAL BLOCK APPLICATION TAG
        ExpectedLogicalBlockApplicationTag : uint16;

        /// LOGICAL BLOCK APPLICATION TAG MASK
        LogicalBlockApplicationTagMask : uint16;

        /// VERIFICATION LENGTH
        VerificationLength : uint32;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            WriteSame

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            this.ServiceAction

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "WRITE SAME(32),NACA=%b,LINK=%b,GroupNumber=%d,AdditionalCDBLength=%d,WRPROTECT=0x%02X,PBDATA=%b,LBDATA=%b,\
                LogicalBlockAddress=0x%016X,ExpectedInitialLogicalBlockReferenceTag=0x%08X,ExpectedLogicalBlockApplicationTag=0x%04X\
                LogicalBlockApplicationTagMask=0x%04X,VerificationLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.GroupNumber
                this.AdditionalCDBLength
                this.WRPROTECT
                this.PBDATA
                this.LBDATA
                this.LogicalBlockAddress
                this.ExpectedInitialLogicalBlockReferenceTag
                this.ExpectedLogicalBlockApplicationTag
                this.LogicalBlockApplicationTagMask
                this.VerificationLength

/// <summary>
///   XDREAD(10) command data structure.
/// </summary>
type XDReadCDB =
    {
        /// OPERATION CODE (52h)
        OperationCode : byte;

        /// XORPINFO( XOR Protection Information ) bit
        XORPINFO : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint32;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// NUMBER OF BLOCKS
        NumberOfBlocks : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            XDRead

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "XDREAD(10),NACA=%b,LINK=%b,XORPINFO=%b,LogicalBlockAddress=0x%08X,GroupNumber=%d,NumberOfBlocks=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.XORPINFO
                this.LogicalBlockAddress
                this.GroupNumber
                this.NumberOfBlocks

/// <summary>
///   XDREAD(32) command data structure.
/// </summary>
type XDRead32CDB =
    {
        /// OPERATION CODE (7Fh)
        OperationCode : byte;

        /// CONTROL
        Control : byte;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// ADDITIONAL CDB LENGTH(18h)
        AdditionalCDBLength : byte;

        /// SERVICE ACTION(0003h)
        ServiceAction : uint16;

        /// XORPINFO( XOR Protection Information ) bit
        XORPINFO : bool;
        
        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// TRANSFER LENGTH
        TransferLength : uint32;

    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            XDRead

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            this.ServiceAction

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "XDREAD(32),NACA=%b,LINK=%b,GroupNumber=%d,AdditionalCDBLength=%d,XORPINFO=%b,LogicalBlockAddress=0x%016X,TransferLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.GroupNumber
                this.AdditionalCDBLength
                this.XORPINFO
                this.LogicalBlockAddress
                this.TransferLength

/// <summary>
///   XDWRITE(10) command data structure.
/// </summary>
type XDWriteCDB =
    {
        /// OPERATION CODE (50h)
        OperationCode : byte;

        /// WRPROTECT
        WRPROTECT : byte;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// FUA( Force Unit Access ) bit
        FUA : bool;

        /// DISABLE WRITE
        DisableWrite : bool;

        /// FUA_NV( Force Unit Access Non-Volatile ) bit
        FUA_NV : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint32;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// TRANSFER LENGTH
        TransferLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            XDWrite

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "XDWRITE(10),NACA=%b,LINK=%b,WRPROTECT=0x%02X,DPO=%b,FUA=%b,DisableWrite=%b,FUA_NV=%b,LogicalBlockAddress=0x%08X,GroupNumber=%d,TransferLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.WRPROTECT
                this.DPO
                this.FUA
                this.DisableWrite
                this.FUA_NV
                this.LogicalBlockAddress
                this.GroupNumber
                this.TransferLength

/// <summary>
///   XDWRITE(32) command data structure.
/// </summary>
type XDWrite32CDB =
    {
        /// OPERATION CODE (7Fh)
        OperationCode : byte;

        /// CONTROL
        Control : byte;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// ADDITIONAL CDB LENGTH(18h)
        AdditionalCDBLength : byte;

        /// SERVICE ACTION(0004h)
        ServiceAction : uint16;

        /// WRPROTECT
        WRPROTECT : byte;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// FUA( Force Unit Access ) bit
        FUA : bool;

        /// DISABLE WRITE
        DisableWrite : bool;

        /// FUA_NV( Force Unit Access Non-Volatile ) bit
        FUA_NV : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// TRANSFER LENGTH
        TransferLength : uint32;

    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            XDWrite

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            this.ServiceAction

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "XDWRITE(32),NACA=%b,LINK=%b,GroupNumber=%d,AdditionalCDBLength=%d,WRPROTECT=0x%02X,\
                DPO=%b,FUA=%b,DisableWrite=%b,FUA_NV=%b,LogicalBlockAddress=0x%016X,TransferLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.GroupNumber
                this.AdditionalCDBLength
                this.WRPROTECT
                this.DPO
                this.FUA
                this.DisableWrite
                this.FUA_NV
                this.LogicalBlockAddress
                this.TransferLength

/// <summary>
///   XDWRITEREAD(10) command data structure.
/// </summary>
type XDWriteReadCDB =
    {
        /// OPERATION CODE (53h)
        OperationCode : byte;

        /// WRPROTECT
        WRPROTECT : byte;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// FUA( Force Unit Access ) bit
        FUA : bool;

        /// DISABLE WRITE
        DisableWrite : bool;

        /// FUA_NV( Force Unit Access Non-Volatile ) bit
        FUA_NV : bool;

        /// XORPINFO( XOR Protection Information ) bit
        XORPINFO : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint32;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// TRANSFER LENGTH
        TransferLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            XDWriteRead

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "XDWRITEREAD(10),NACA=%b,LINK=%b,WRPROTECT=0x%02X,DPO=%b,FUA=%b,DisableWrite=%b,FUA_NV=%b,XORPINFO=%b,LogicalBlockAddress=0x%08X,GroupNumber=%d,TransferLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.WRPROTECT
                this.DPO
                this.FUA
                this.DisableWrite
                this.FUA_NV
                this.XORPINFO
                this.LogicalBlockAddress
                this.GroupNumber
                this.TransferLength

/// <summary>
///   XDWRITEREAD(32) command data structure.
/// </summary>
type XDWriteRead32CDB =
    {
        /// OPERATION CODE (7Fh)
        OperationCode : byte;

        /// CONTROL
        Control : byte;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// ADDITIONAL CDB LENGTH(18h)
        AdditionalCDBLength : byte;

        /// SERVICE ACTION(0007h)
        ServiceAction : uint16;

        /// WRPROTECT
        WRPROTECT : byte;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// FUA( Force Unit Access ) bit
        FUA : bool;

        /// DISABLE WRITE
        DisableWrite : bool;

        /// FUA_NV( Force Unit Access Non-Volatile ) bit
        FUA_NV : bool;

        /// XORPINFO( XOR Protection Information ) bit
        XORPINFO : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// TRANSFER LENGTH
        TransferLength : uint32;

    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            XDWriteRead

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            this.ServiceAction

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "XDWRITEREAD(32),NACA=%b,LINK=%b,GroupNumber=%d,AdditionalCDBLength=%d,WRPROTECT=0x%02X,\
                DPO=%b,FUA=%b,DisableWrite=%b,FUA_NV=%b,XORPINFO=%b,LogicalBlockAddress=0x%016X,TransferLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.GroupNumber
                this.AdditionalCDBLength
                this.WRPROTECT
                this.DPO
                this.FUA
                this.DisableWrite
                this.FUA_NV
                this.XORPINFO
                this.LogicalBlockAddress
                this.TransferLength

/// <summary>
///   XPWRITE(10) command data structure.
/// </summary>
type XPWriteCDB =
    {
        /// OPERATION CODE (51h)
        OperationCode : byte;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// FUA( Force Unit Access ) bit
        FUA : bool;

        /// FUA_NV( Force Unit Access Non-Volatile ) bit
        FUA_NV : bool;

        /// XORPINFO( XOR Protection Information ) bit
        XORPINFO : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint32;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// TRANSFER LENGTH
        TransferLength : uint16;

        /// CONTROL
        Control : byte;
    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            XPWrite

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION field is not exist
        member this.ServiceAction : uint16 =
            0us

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "XPWRITE(10),NACA=%b,LINK=%b,DPO=%b,FUA=%b,FUA_NV=%b,XORPINFO=%b,LogicalBlockAddress=0x%08X,GroupNumber=%d,TransferLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.DPO
                this.FUA
                this.FUA_NV
                this.XORPINFO
                this.LogicalBlockAddress
                this.GroupNumber
                this.TransferLength

/// <summary>
///   XPWRITE(32) command data structure.
/// </summary>
type XPWrite32CDB =
    {
        /// OPERATION CODE (7Fh)
        OperationCode : byte;

        /// CONTROL
        Control : byte;

        /// GROUP NUMBER
        GroupNumber : byte;

        /// ADDITIONAL CDB LENGTH(18h)
        AdditionalCDBLength : byte;

        /// SERVICE ACTION(0006h)
        ServiceAction : uint16;

        /// DPO( Disable Page Out ) bit
        DPO : bool;

        /// FUA( Force Unit Access ) bit
        FUA : bool;

        /// FUA_NV( Force Unit Access Non-Volatile ) bit
        FUA_NV : bool;

        /// XORPINFO( XOR Protection Information ) bit
        XORPINFO : bool;

        /// LOGICAL BLOCK ADDRESS
        LogicalBlockAddress : uint64;

        /// TRANSFER LENGTH
        TransferLength : uint32;

    }

    /// <inheritdoc />
    interface ICDB with
        // Get the type of CDB.
        member this.Type : CDBTypes =
            XPWrite

        // Get OPERATION CODE value of the CDB.
        member this.OperationCode : byte =
            this.OperationCode

        // SERVICE ACTION value of the CDB.
        member this.ServiceAction : uint16 =
            this.ServiceAction

        // Get NACA(Normal ACA) bit in CONTROL field of the CDB.
        member this.NACA : bool =
            Functions.CheckBitflag this.Control 0x04uy

        // Get LINK bit in CONTROL field of the CDB.
        member this.LINK : bool =
            Functions.CheckBitflag this.Control 0x01uy

        // Get description string.
        member this.DescriptString : string =
            sprintf
                "XPWRITE(32),NACA=%b,LINK=%b,GroupNumber=%d,AdditionalCDBLength=%d,DPO=%b,FUA=%b,FUA_NV=%b,XORPINFO=%b,LogicalBlockAddress=0x%016X,TransferLength=%d"
                ( this :> ICDB ).NACA
                ( this :> ICDB ).LINK
                this.GroupNumber
                this.AdditionalCDBLength
                this.DPO
                this.FUA
                this.FUA_NV
                this.XORPINFO
                this.LogicalBlockAddress
                this.TransferLength

