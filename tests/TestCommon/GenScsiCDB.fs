//=============================================================================
// Haruka Software Storage.
// GenScsiCDB.fs : Implement a function to generate a byte array for the SCSI CDB
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test

//=============================================================================
// Import declaration

open System
open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Type definition

/// Represents EVPD bit in CDB is 1 or 0.
type EVPD =
    | T     // EVPD flag is 1
    | F     // EVPD flag is 0
    /// Convert EVPD to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to EVPD
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents PF(PageFormat) bit in CDB is 1 or 0.
type PF =
    | T     // PF flag is 1
    | F     // PF flag is 0
    /// Convert PF to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to PF
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents SP(SavePages) bit in CDB is 1 or 0.
type SP =
    | T     // SP flag is 1
    | F     // SP flag is 0
    /// Convert SP to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to SP
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents DBD bit in CDB is 1 or 0.
type DBD =
    | T     // DBD flag is 1
    | F     // DBD flag is 0
    /// Convert DBD to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to DBD
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents LLBAA bit in CDB is 1 or 0.
type LLBAA =
    | T     // LLBAA flag is 1
    | F     // LLBAA flag is 0
    /// Convert LLBAA to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to LLBAA
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents IMMED bit in CDB is 1 or 0.
type IMMED =
    | T     // IMMED flag is 1
    | F     // IMMED flag is 0
    /// Convert IMMED to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to IMMED
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents DESC bit in CDB is 1 or 0.
type DESC =
    | T     // DESC flag is 1
    | F     // DESC flag is 0
    /// Convert DESC to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to DESC
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents FMTPINFO bit in CDB is 1 or 0.
type FMTPINFO =
    | T     // FMTPINFO flag is 1
    | F     // FMTPINFO flag is 0
    /// Convert FMTPINFO to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to FMTPINFO
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents RTO_REQ bit in CDB is 1 or 0.
type RTO_REQ =
    | T     // RTO_REQ flag is 1
    | F     // RTO_REQ flag is 0
    /// Convert RTO_REQ to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to RTO_REQ
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents LONGLIST bit in CDB is 1 or 0.
type LONGLIST =
    | T     // LONGLIST flag is 1
    | F     // LONGLIST flag is 0
    /// Convert LONGLIST to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to LONGLIST
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents FMTDATA bit in CDB is 1 or 0.
type FMTDATA =
    | T     // FMTDATA flag is 1
    | F     // FMTDATA flag is 0
    /// Convert FMTDATA to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to FMTDATA
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents CMPLIST bit in CDB is 1 or 0.
type CMPLIST =
    | T     // CMPLIST flag is 1
    | F     // CMPLIST flag is 0
    /// Convert CMPLIST to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to CMPLIST
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents DPO bit in CDB is 1 or 0.
type DPO =
    | T     // DPO flag is 1
    | F     // DPO flag is 0
    /// Convert DPO to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to DPO
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents FUA bit in CDB is 1 or 0.
type FUA =
    | T     // FUA flag is 1
    | F     // FUA flag is 0
    /// Convert FUA to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to FUA
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents FUA_NV bit in CDB is 1 or 0.
type FUA_NV =
    | T     // FUA_NV flag is 1
    | F     // FUA_NV flag is 0
    /// Convert FUA_NV to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to FUA_NV
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents PMI bit in CDB is 1 or 0.
type PMI =
    | T     // PMI flag is 1
    | F     // PMI flag is 0
    /// Convert PMI to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to PMI
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents SYNC_NV bit in CDB is 1 or 0.
type SYNC_NV =
    | T     // SYNC_NV flag is 1
    | F     // SYNC_NV flag is 0
    /// Convert SYNC_NV to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to SYNC_NV
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents NACA bit in CDB is 1 or 0.
type NACA =
    | T     // NACA flag is 1
    | F     // NACA flag is 0
    /// Convert NACA to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to NACA
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents LINK bit in CDB is 1 or 0.
type LINK =
    | T     // LINK flag is 1
    | F     // LINK flag is 0
    /// Convert LINK to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to LINK
    static member ofBool =
        function
        | true -> T
        | false -> F

//=============================================================================
// Class implementation

type GenScsiCDB() =

    /// <summary>
    ///  Generate INQUIRY CDB
    /// </summary>
    /// <param name="argEVPD">
    ///  EVPD bit
    /// </param>
    /// <param name="argPageCode">
    ///  PAGE CODE field
    /// </param>
    /// <param name="argAllocationLength">
    ///  ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated INQUIRY CDB with padding to 16 byte.
    /// </returns>
    static member Inquiry ( argEVPD : EVPD ) ( argPageCode : byte ) ( argAllocationLength : uint16 ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x12uy;                                                                             // OPERATION CODE
            Functions.SetBitflag ( EVPD.toBool argEVPD ) 0x01uy;                                // EVPD
            argPageCode;                                                                        // PAGE CODE
            yield! Functions.UInt16ToNetworkBytes_NewVec argAllocationLength                    // ALLOCATION LENGTH
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy;
        |]

    /// <summary>
    ///  Generate MODE SELECT(6) CDB
    /// </summary>
    /// <param name="argPageFormat">
    ///  PF bit
    /// </param>
    /// <param name="argSavePages">
    ///  SP bit
    /// </param>
    /// <param name="argParameterListLength">
    ///  PARAMETER LIST LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated MODE SELECT(6) CDB with padding to 16 byte.
    /// </returns>
    static member ModeSelect6 ( argPageFormat : PF ) ( argSavePages : SP ) ( argParameterListLength : byte ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x15uy;                                                                             // OPERATION CODE
            ( Functions.SetBitflag ( PF.toBool argPageFormat ) 0x10uy ) |||                     // PF
                ( Functions.SetBitflag ( SP.toBool argSavePages  ) 0x01uy );                    // SP
            0x00uy; 0x00uy;                                                                     // Reserved
            argParameterListLength;                                                             // PARAMETER LIST LENGTH
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy;
        |]

    /// <summary>
    ///  Generate MODE SELECT(10) CDB
    /// </summary>
    /// <param name="argPageFormat">
    ///  PF bit
    /// </param>
    /// <param name="argSavePages">
    ///  SP bit
    /// </param>
    /// <param name="argParameterListLength">
    ///  PARAMETER LIST LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated MODE SELECT(10) CDB with padding to 16 byte.
    /// </returns>
    static member ModeSelect10 ( argPageFormat : PF ) ( argSavePages : SP ) ( argParameterListLength : uint16 ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x55uy;                                                                             // OPERATION CODE
            ( Functions.SetBitflag ( PF.toBool argPageFormat ) 0x10uy ) |||                     // PF
                ( Functions.SetBitflag ( SP.toBool argSavePages  ) 0x01uy );                    // SP
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // Reserved
            0x00uy;                                                                             // Reserved
            yield! Functions.UInt16ToNetworkBytes_NewVec argParameterListLength;                // PARAMETER LIST LENGTH
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;
        |]

    /// <summary>
    ///  Generate MODE SENSE(6) CDB
    /// </summary>
    /// <param name="argDBD">
    ///  DBD bit
    /// </param>
    /// <param name="argPC">
    ///  PC field
    /// </param>
    /// <param name="argPageCode">
    ///  PAGE CODE field
    /// </param>
    /// <param name="argSubPageCode">
    ///  SUB PAGE CODE field
    /// </param>
    /// <param name="argAllocationLength">
    ///  ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated MODE SENSE(6) CDB with padding to 16 byte.
    /// </returns>
    static member ModeSense6 ( argDBD : DBD ) ( argPC : byte ) ( argPageCode : byte ) ( argSubPageCode : byte ) ( argAllocationLength : byte ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x1Auy;                                                                             // OPERATION CODE
            Functions.SetBitflag ( DBD.toBool argDBD ) 0x08uy;                                  // DBD
            ( ( argPC &&& 0x03uy ) <<< 6 ) |||                                                  // PC
                ( argPageCode &&& 0x3Fuy );                                                     // PAGE CODE
            argSubPageCode;                                                                     // SUB PAGE CODE
            argAllocationLength;                                                                // ALLOCATION LENGTH
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy;
        |]

    /// <summary>
    ///  Generate MODE SENSE(10) CDB
    /// </summary>
    /// <param name="argLLBAA">
    ///  LLBAA bit
    /// </param>
    /// <param name="argDBD">
    ///  DBD bit
    /// </param>
    /// <param name="argPC">
    ///  PC field
    /// </param>
    /// <param name="argPageCode">
    ///  PAGE CODE field
    /// </param>
    /// <param name="argSubPageCode">
    ///  SUB PAGE CODE field
    /// </param>
    /// <param name="argAllocationLength">
    ///  ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated MODE SENSE(10) CDB with padding to 16 byte.
    /// </returns>
    static member ModeSense10 ( argLLBAA : LLBAA ) ( argDBD : DBD ) ( argPC : byte ) ( argPageCode : byte ) ( argSubPageCode : byte ) ( argAllocationLength : uint16 ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x5Auy;                                                                             // OPERATION CODE
            ( Functions.SetBitflag ( LLBAA.toBool argLLBAA ) 0x01uy ) |||                       // LLBAA
                ( Functions.SetBitflag ( DBD.toBool argDBD ) 0x08uy );                          // DBD
            ( ( argPC &&& 0x03uy ) <<< 6 ) |||                                                  // PC
                ( argPageCode &&& 0x3Fuy );                                                     // PAGE CODE
            argSubPageCode;                                                                     // SUB PAGE CODE
            0x00uy; 0x00uy; 0x00uy;                                                             // Reserved
            yield! Functions.UInt16ToNetworkBytes_NewVec argAllocationLength                    // ALLOCATION LENGTH 
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;
        |]

    /// <summary>
    ///  Generate PERSISTENT RESERVE IN CDB
    /// </summary>
    /// <param name="argServiceAction">
    ///   SERVICE ACTION field
    /// </param>
    /// <param name="argAllocationLength">
    ///  ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated PERSISTENT RESERVE IN CDB with padding to 16 byte.
    /// </returns>
    static member PersistentReserveIn ( argServiceAction : byte ) ( argAllocationLength : uint16 ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x5Euy;                                                                             // OPERATION CODE
            ( argServiceAction &&& 0x1Fuy );                                                    // SERVICE ACTION
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // Reserved
            0x00uy;                                                                             // Reserved
            yield! Functions.UInt16ToNetworkBytes_NewVec argAllocationLength                    // ALLOCATION LENGTH 
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;
        |]

    /// <summary>
    ///  Generate PERSISTENT RESERVE OUT CDB
    /// </summary>
    /// <param name="argServiceAction">
    ///   SERVICE ACTION field
    /// </param>
    /// <param name="argScope">
    ///   SCOPE field
    /// </param>
    /// <param name="argType">
    ///   TYPE field
    /// </param>
    /// <param name="argParameterListLength">
    ///  PARAMETER LIST LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated PERSISTENT RESERVE OUT CDB with padding to 16 byte.
    /// </returns>
    static member PersistentReserveOut ( argServiceAction : byte ) ( argScope : byte ) ( argType : byte ) ( argParameterListLength : uint32 ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x5Fuy;                                                                             // OPERATION CODE
            argServiceAction &&& 0x1Fuy;                                                        // SERVICE ACTION
            ( ( argScope &&& 0x0Fuy ) <<< 4 ) |||                                               // SCOPE
                ( argType &&& 0x0Fuy );                                                         // TYPE
            0x00uy; 0x00uy;                                                                     // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec argParameterListLength;                // PARAMETER LIST LENGTH 
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;
        |]

    /// <summary>
    ///  Generate PRE-FETCH(10) CDB
    /// </summary>
    /// <param name="argIMMED">
    ///   IMMED bit
    /// </param>
    /// <param name="argLBA">
    ///   LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argGroupNumber">
    ///   GROUP NUMBER field
    /// </param>
    /// <param name="argPreFetchLength">
    ///  PREFETCH LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated PRE-FETCH(10) CDB with padding to 16 byte.
    /// </returns>
    static member PreFetch10 ( argIMMED : IMMED ) ( argLBA : BLKCNT32_T ) ( argGroupNumber : byte ) ( argPreFetchLength : BLKCNT16_T ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x34uy;                                                                                     // OPERATION CODE
            ( Functions.SetBitflag ( IMMED.toBool argIMMED ) 0x02uy );                                  // IMMED
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argLBA );                 // LOGICAL BLOCK ADDRESS
            ( argGroupNumber &&& 0x1Fuy );                                                              // GROUP NUMBER
            yield! Functions.UInt16ToNetworkBytes_NewVec ( blkcnt_me.toUInt16 argPreFetchLength );   // PREFETCH LENGTH
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                                 // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                             // padding
            0x00uy; 0x00uy;
        |]

    /// <summary>
    ///  Generate PRE-FETCH(16) CDB
    /// </summary>
    /// <param name="argIMMED">
    ///   IMMED bit
    /// </param>
    /// <param name="argLBA">
    ///   LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argGroupNumber">
    ///   GROUP NUMBER field
    /// </param>
    /// <param name="argPreFetchLength">
    ///  PREFETCH LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated PRE-FETCH(16) CDB.
    /// </returns>
    static member PreFetch16 ( argIMMED : IMMED ) ( argLBA : BLKCNT64_T ) ( argGroupNumber : byte ) ( argPreFetchLength : BLKCNT32_T ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x90uy;                                                                                     // OPERATION CODE
            Functions.SetBitflag ( IMMED.toBool argIMMED ) 0x02uy;                                      // IMMED
            yield! Functions.UInt64ToNetworkBytes_NewVec ( blkcnt_me.toUInt64 argLBA );                 // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argPreFetchLength );      // PREFETCH LENGTH
            argGroupNumber &&& 0x1Fuy;                                                                  // GROUP NUMBER
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                                 // CONTROL
        |]

    /// <summary>
    ///  Generate REPORT LUNS CDB
    /// </summary>
    /// <param name="argSelectReport">
    ///   SELECT REPORT field
    /// </param>
    /// <param name="argAllocationLength">
    ///   ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated REPORT LUNS CDB with padding to 16 byte.
    /// </returns>
    static member ReportLUNs ( argSelectReport : byte ) ( argAllocationLength : uint32 ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0xA0uy;                                                                             // OPERATION CODE
            0x00uy;                                                                             // Reserved
            argSelectReport;                                                                    // SELECT REPORT
            0x00uy; 0x00uy; 0x00uy;                                                             // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec argAllocationLength;                   // ALLOCATION LENGTH
            0x00uy;                                                                             // Reserved
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
        |]

    /// <summary>
    ///  Generate REQUEST SENSE CDB
    /// </summary>
    /// <param name="argDESC">
    ///   DESC bit
    /// </param>
    /// <param name="argAllocationLength">
    ///   ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated REQUEST SENSE CDB with padding to 16 byte.
    /// </returns>
    static member RequestSense ( argDESC : DESC ) ( argAllocationLength : byte ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0xA0uy;                                                                             // OPERATION CODE
            Functions.SetBitflag ( DESC.toBool argDESC ) 0x01uy;                                // DESC
            0x00uy; 0x00uy;                                                                     // Reserved
            argAllocationLength;                                                                // ALLOCATION LENGTH
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;                                                                     // padding
        |]

    /// <summary>
    ///  Generate TEST UNIT READY CDB
    /// </summary>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated TEST UNIT READY CDB with padding to 16 byte.
    /// </returns>
    static member TestUnitReady ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x00uy;                                                                             // OPERATION CODE
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // Reserved
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;                                                                     // padding
        |]

    /// <summary>
    ///  Generate FORMAT UNIT CDB
    /// </summary>
    /// <param name="argFMTPINFO">
    ///  FMTPINFO bit
    /// </param>
    /// <param name="argRTO_REQ">
    ///  RTO_REQ bit
    /// </param>
    /// <param name="argLONGLIST">
    ///  LONGLIST bit
    /// </param>
    /// <param name="argFMTDATA">
    ///  FMTDATA bit
    /// </param>
    /// <param name="argCMPLIST">
    ///  CMPLIST bit
    /// </param>
    /// <param name="argDefectListFormat">
    ///  DEFECT LIST FORMAT field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated FORMAT UNIT CDB with padding to 16 byte.
    /// </returns>
    static member FormatUnit ( argFMTPINFO : FMTPINFO ) ( argRTO_REQ : RTO_REQ ) ( argLONGLIST : LONGLIST ) ( argFMTDATA : FMTDATA ) ( argCMPLIST : CMPLIST ) ( argDefectListFormat : byte ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x04uy;                                                                             // OPERATION CODE
            ( Functions.SetBitflag ( FMTPINFO.toBool argFMTPINFO ) 0x80uy ) |||                 // FMTPINFO
                ( Functions.SetBitflag ( RTO_REQ.toBool argRTO_REQ ) 0x40uy ) |||               // RTO_REQ
                ( Functions.SetBitflag ( LONGLIST.toBool argLONGLIST ) 0x20uy ) |||             // LONGLIST
                ( Functions.SetBitflag ( FMTDATA.toBool argFMTDATA ) 0x10uy ) |||               // FMTDATA
                ( Functions.SetBitflag ( CMPLIST.toBool argCMPLIST ) 0x08uy ) |||               // CMPLIST
                ( argDefectListFormat &&& 0x07uy );                                             // DEFECT LIST FORMAT
            0x00uy;                                                                             // Vendor specific
            0x00uy; 0x00uy;                                                                     // Obsoleted
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;                                                                     // padding
        |]

    /// <summary>
    ///  Generate READ(6) CDB
    /// </summary>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argTransferLength">
    ///  TRANSFER LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated READ(6) CDB with padding to 16 byte.
    /// </returns>
    static member Read6 ( argLBA : BLKCNT32_T ) ( argTransferLength : BLKCNT8_T ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        let lbau32 = blkcnt_me.toUInt32 argLBA
        [|
            0x08uy;                                                                             // OPERATION CODE
            byte( ( lbau32 &&& 0x001F0000u ) >>> 16 );                                          // LOGICAL BLOCK ADDRESS
            byte( ( lbau32 &&& 0x0000FF00u ) >>> 8 );                                           // LOGICAL BLOCK ADDRESS
            byte(   lbau32 &&& 0x000000FFu );                                                   // LOGICAL BLOCK ADDRESS
            argTransferLength |> blkcnt_me.toUInt8;                                             // TRANSFER LENGTH
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;                                                                     // padding
        |]

    /// <summary>
    ///  Generate READ(10) CDB
    /// </summary>
    /// <param name="argRDPROTECT">
    ///  RDPROTECT field
    /// </param>
    /// <param name="argDPO">
    ///  DPO bit
    /// </param>
    /// <param name="argFUA">
    ///  FUA bit
    /// </param>
    /// <param name="argFUA_NV">
    ///  FUA_NV bit
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argGroupNumber">
    ///  GROUP NUMBER field
    /// </param>
    /// <param name="argTransferLength">
    ///  TRANSFER LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated READ(10) CDB with padding to 16 byte.
    /// </returns>
    static member Read10 ( argRDPROTECT : byte ) ( argDPO : DPO ) ( argFUA : FUA ) ( argFUA_NV : FUA_NV ) ( argLBA : BLKCNT32_T ) ( argGroupNumber : byte ) ( argTransferLength : BLKCNT16_T ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x28uy;                                                                                     // OPERATION CODE
            ( ( argRDPROTECT &&& 0x07uy ) <<< 5 ) |||                                                   // RDPROTECT
                ( Functions.SetBitflag ( DPO.toBool argDPO ) 0x10uy ) |||                               // DPO
                ( Functions.SetBitflag ( FUA.toBool argFUA ) 0x08uy ) |||                               // FUA
                ( Functions.SetBitflag ( FUA_NV.toBool argFUA_NV ) 0x02uy );                            // FUA_NV
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argLBA );                 // LOGICAL BLOCK ADDRESS
            argGroupNumber &&& 0x1Fuy;                                                                  // GROUP NUMBER
            yield! Functions.UInt16ToNetworkBytes_NewVec ( blkcnt_me.toUInt16 argTransferLength );      // TRANSFER LENGTH
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                                 // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                             // padding
            0x00uy; 0x00uy;                                                                             // padding
        |]

    /// <summary>
    ///  Generate READ(12) CDB
    /// </summary>
    /// <param name="argRDPROTECT">
    ///  RDPROTECT field
    /// </param>
    /// <param name="argDPO">
    ///  DPO bit
    /// </param>
    /// <param name="argFUA">
    ///  FUA bit
    /// </param>
    /// <param name="argFUA_NV">
    ///  FUA_NV bit
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argGroupNumber">
    ///  GROUP NUMBER field
    /// </param>
    /// <param name="argTransferLength">
    ///  TRANSFER LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated READ(12) CDB with padding to 16 byte.
    /// </returns>
    static member Read12 ( argRDPROTECT : byte ) ( argDPO : DPO ) ( argFUA : FUA ) ( argFUA_NV : FUA_NV ) ( argLBA : BLKCNT32_T ) ( argGroupNumber : byte ) ( argTransferLength : BLKCNT32_T ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0xA8uy;                                                                                     // OPERATION CODE
            ( ( argRDPROTECT &&& 0x07uy ) <<< 5 ) |||                                                   // RDPROTECT
                ( Functions.SetBitflag ( DPO.toBool argDPO ) 0x10uy ) |||                               // DPO
                ( Functions.SetBitflag ( FUA.toBool argFUA ) 0x08uy ) |||                               // FUA
                ( Functions.SetBitflag ( FUA_NV.toBool argFUA_NV ) 0x02uy );                            // FUA_NV
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argLBA );                 // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argTransferLength );      // TRANSFER LENGTH
            argGroupNumber &&& 0x1Fuy;                                                                  // GROUP NUMBER
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                                 // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                             // padding
        |]

    /// <summary>
    ///  Generate READ(16) CDB
    /// </summary>
    /// <param name="argRDPROTECT">
    ///  RDPROTECT field
    /// </param>
    /// <param name="argDPO">
    ///  DPO bit
    /// </param>
    /// <param name="argFUA">
    ///  FUA bit
    /// </param>
    /// <param name="argFUA_NV">
    ///  FUA_NV bit
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argGroupNumber">
    ///  GROUP NUMBER field
    /// </param>
    /// <param name="argTransferLength">
    ///  TRANSFER LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated READ(16) CDB.
    /// </returns>
    static member Read16 ( argRDPROTECT : byte ) ( argDPO : DPO ) ( argFUA : FUA ) ( argFUA_NV : FUA_NV ) ( argLBA : BLKCNT64_T ) ( argGroupNumber : byte ) ( argTransferLength : BLKCNT32_T ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x88uy;                                                                                     // OPERATION CODE
            ( ( argRDPROTECT &&& 0x07uy ) <<< 5 ) |||                                                   // RDPROTECT
                ( Functions.SetBitflag ( DPO.toBool argDPO ) 0x10uy ) |||                               // DPO
                ( Functions.SetBitflag ( FUA.toBool argFUA ) 0x08uy ) |||                               // FUA
                ( Functions.SetBitflag ( FUA_NV.toBool argFUA_NV ) 0x02uy );                            // FUA_NV
            yield! Functions.UInt64ToNetworkBytes_NewVec ( blkcnt_me.toUInt64 argLBA );                 // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argTransferLength );      // TRANSFER LENGTH
            argGroupNumber &&& 0x1Fuy;                                                                  // GROUP NUMBER
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                                 // CONTROL
        |]

    /// <summary>
    ///  Generate READ CAPACITY(10) CDB
    /// </summary>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argPMI">
    ///  PMI bit
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated READ CAPACITY(10) CDB with padding to 16 byte.
    /// </returns>
    static member ReadCapacity10 ( argLBA : BLKCNT32_T ) ( argPMI : PMI ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x25uy;                                                                             // OPERATION CODE
            0x00uy;                                                                             // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argLBA );         // LOGICAL BLOCK ADDRESS
            0x00uy; 0x00uy;                                                                     // Reserved
            Functions.SetBitflag ( PMI.toBool argPMI ) 0x01uy;                                  // PMI
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;                                                                     // padding
        |]

    /// <summary>
    ///  Generate READ CAPACITY(16) CDB
    /// </summary>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argAllocationLength">
    ///  ALLOCATION LENGTH field
    /// </param>
    /// <param name="argPMI">
    ///  PMI bit
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated READ CAPACITY(16) CDB.
    /// </returns>
    static member ReadCapacity16 ( argLBA : BLKCNT64_T ) ( argAllocationLength : uint32 ) ( argPMI : PMI ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x9Euy;                                                                             // OPERATION CODE
            0x10uy;                                                                             // SERVICE ACTION
            yield! Functions.UInt64ToNetworkBytes_NewVec ( blkcnt_me.toUInt64 argLBA );         // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec argAllocationLength;                   // ALLOCATION LENGTH
            Functions.SetBitflag ( PMI.toBool argPMI ) 0x01uy;                                  // PMI
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
        |]

    /// <summary>
    ///  Generate SYNCHRONIZE CACHE(10) CDB
    /// </summary>
    /// <param name="argSYNC_NV">
    ///  SYNC_NV bit
    /// </param>
    /// <param name="argIMMED">
    ///  IMMED bit
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS  field
    /// </param>
    /// <param name="argGroupNumber">
    ///  GROUP NUMBER field
    /// </param>
    /// <param name="argNumberOfBlockes">
    ///   NUMBER OF BLOCKS field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated SYNCHRONIZE CACHE(10) CDB with padding to 16 byte.
    /// </returns>
    static member SynchronizeCache10 ( argSYNC_NV : SYNC_NV ) ( argIMMED : IMMED ) ( argLBA : BLKCNT32_T ) ( argGroupNumber : byte ) ( argNumberOfBlockes : BLKCNT16_T ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x35uy;                                                                                     // OPERATION CODE
            ( Functions.SetBitflag ( SYNC_NV.toBool argSYNC_NV ) 0x04uy ) |||                           // SYNC_NV
                ( Functions.SetBitflag ( IMMED.toBool argIMMED ) 0x02uy );                              // IMMED
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argLBA );                 // LOGICAL BLOCK ADDRESS
            argGroupNumber &&& 0x1Fuy;                                                                  // GROUP NUMBER
            yield! Functions.UInt16ToNetworkBytes_NewVec ( blkcnt_me.toUInt16 argNumberOfBlockes );     // NUMBER OF BLOCKS
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                                 // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                             // padding
            0x00uy; 0x00uy;                                                                             // padding
        |]

    /// <summary>
    ///  Generate SYNCHRONIZE CACHE(16) CDB
    /// </summary>
    /// <param name="argSYNC_NV">
    ///  SYNC_NV bit
    /// </param>
    /// <param name="argIMMED">
    ///  IMMED bit
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS  field
    /// </param>
    /// <param name="argGroupNumber">
    ///  GROUP NUMBER field
    /// </param>
    /// <param name="argNumberOfBlockes">
    ///   NUMBER OF BLOCKS field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated SYNCHRONIZE CACHE(16) CDB.
    /// </returns>
    static member SynchronizeCache16 ( argSYNC_NV : SYNC_NV ) ( argIMMED : IMMED ) ( argLBA : BLKCNT64_T ) ( argGroupNumber : byte ) ( argNumberOfBlockes : BLKCNT32_T ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x91uy;                                                                                     // OPERATION CODE
            ( Functions.SetBitflag ( SYNC_NV.toBool argSYNC_NV ) 0x04uy ) |||                           // SYNC_NV
                ( Functions.SetBitflag ( IMMED.toBool argIMMED ) 0x02uy );                              // IMMED
            yield! Functions.UInt64ToNetworkBytes_NewVec ( blkcnt_me.toUInt64 argLBA );                 // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argNumberOfBlockes );     // NUMBER OF BLOCKS
            argGroupNumber &&& 0x1Fuy;                                                                  // GROUP NUMBER
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                                 // CONTROL
        |]

    /// <summary>
    ///  Generate WRITE(6) CDB
    /// </summary>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS  field
    /// </param>
    /// <param name="argTransferLength">
    ///  TRANSFER LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated WRITE(6) CDB with padding to 16 byte.
    /// </returns>
    static member Write6 ( argLBA : BLKCNT32_T ) ( argTransferLength : BLKCNT8_T ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        let lbau32 = blkcnt_me.toUInt32 argLBA
        [|
            0x0Auy;                                                                             // OPERATION CODE
            byte( ( lbau32 &&& 0x001F0000u ) >>> 16 );                                          // LOGICAL BLOCK ADDRESS
            byte( ( lbau32 &&& 0x0000FF00u ) >>> 8 );                                           // LOGICAL BLOCK ADDRESS
            byte(   lbau32 &&& 0x000000FFu );                                                   // LOGICAL BLOCK ADDRESS
            argTransferLength |> blkcnt_me.toUInt8;                                             // TRANSFER LENGTH
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;                                                                     // padding
        |]

    /// <summary>
    ///  Generate WRITE(10) CDB
    /// </summary>
    /// <param name="argWRPROTECT">
    ///  WRPROTECT field
    /// </param>
    /// <param name="argDPO">
    ///  DPO bit
    /// </param>
    /// <param name="argFUA">
    ///  FUA bit
    /// </param>
    /// <param name="argFUA_NV">
    ///  FUA_NV bit
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argGroupNumber">
    ///  GROUP NUMBER field
    /// </param>
    /// <param name="argTransferLength">
    ///  TRANSFER LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated WRITE(10) CDB with padding to 16 byte.
    /// </returns>
    static member Write10 ( argWRPROTECT : byte ) ( argDPO : DPO ) ( argFUA : FUA ) ( argFUA_NV : FUA_NV ) ( argLBA : BLKCNT32_T ) ( argGroupNumber : byte ) ( argTransferLength : BLKCNT16_T ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x2Auy;                                                                                     // OPERATION CODE
            ( ( argWRPROTECT &&& 0x07uy ) <<< 5 ) |||                                                   // WRPROTECT
                ( Functions.SetBitflag ( DPO.toBool argDPO ) 0x10uy ) |||                               // DPO
                ( Functions.SetBitflag ( FUA.toBool argFUA ) 0x08uy ) |||                               // FUA
                ( Functions.SetBitflag ( FUA_NV.toBool argFUA_NV ) 0x02uy );                            // FUA_NV
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argLBA );                 // LOGICAL BLOCK ADDRESS
            argGroupNumber &&& 0x1Fuy;                                                                  // GROUP NUMBER
            yield! Functions.UInt16ToNetworkBytes_NewVec ( blkcnt_me.toUInt16 argTransferLength );      // TRANSFER LENGTH
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                                 // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                             // padding
            0x00uy; 0x00uy;                                                                             // padding
        |]

    /// <summary>
    ///  Generate WRITE(12) CDB
    /// </summary>
    /// <param name="argWRPROTECT">
    ///  WRPROTECT field
    /// </param>
    /// <param name="argDPO">
    ///  DPO bit
    /// </param>
    /// <param name="argFUA">
    ///  FUA bit
    /// </param>
    /// <param name="argFUA_NV">
    ///  FUA_NV bit
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argGroupNumber">
    ///  GROUP NUMBER field
    /// </param>
    /// <param name="argTransferLength">
    ///  TRANSFER LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated WRITE(12) CDB with padding to 16 byte.
    /// </returns>
    static member Write12 ( argWRPROTECT : byte ) ( argDPO : DPO ) ( argFUA : FUA ) ( argFUA_NV : FUA_NV ) ( argLBA : BLKCNT32_T ) ( argGroupNumber : byte ) ( argTransferLength : BLKCNT32_T ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0xAAuy;                                                                                     // OPERATION CODE
            ( ( argWRPROTECT &&& 0x07uy ) <<< 5 ) |||                                                   // WRPROTECT
                ( Functions.SetBitflag ( DPO.toBool argDPO ) 0x10uy ) |||                               // DPO
                ( Functions.SetBitflag ( FUA.toBool argFUA ) 0x08uy ) |||                               // FUA
                ( Functions.SetBitflag ( FUA_NV.toBool argFUA_NV ) 0x02uy );                            // FUA_NV
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argLBA );                 // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argTransferLength );      // TRANSFER LENGTH
            argGroupNumber &&& 0x1Fuy;                                                                  // GROUP NUMBER
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                                 // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                             // padding
        |]

    /// <summary>
    ///  Generate WRITE(16) CDB
    /// </summary>
    /// <param name="argWRPROTECT">
    ///  WRPROTECT field
    /// </param>
    /// <param name="argDPO">
    ///  DPO bit
    /// </param>
    /// <param name="argFUA">
    ///  FUA bit
    /// </param>
    /// <param name="argFUA_NV">
    ///  FUA_NV bit
    /// </param>
    /// <param name="argLBA">
    ///  LOGICAL BLOCK ADDRESS field
    /// </param>
    /// <param name="argGroupNumber">
    ///  GROUP NUMBER field
    /// </param>
    /// <param name="argTransferLength">
    ///  TRANSFER LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated WRITE(16) CDB.
    /// </returns>
    static member Write16 ( argWRPROTECT : byte ) ( argDPO : DPO ) ( argFUA : FUA ) ( argFUA_NV : FUA_NV ) ( argLBA : BLKCNT64_T ) ( argGroupNumber : byte ) ( argTransferLength : BLKCNT32_T ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0x8Auy;                                                                                     // OPERATION CODE
            ( ( argWRPROTECT &&& 0x07uy ) <<< 5 ) |||                                                   // WRPROTECT
                ( Functions.SetBitflag ( DPO.toBool argDPO ) 0x10uy ) |||                               // DPO
                ( Functions.SetBitflag ( FUA.toBool argFUA ) 0x08uy ) |||                               // FUA
                ( Functions.SetBitflag ( FUA_NV.toBool argFUA_NV ) 0x02uy );                            // FUA_NV
            yield! Functions.UInt64ToNetworkBytes_NewVec ( blkcnt_me.toUInt64 argLBA );                 // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec ( blkcnt_me.toUInt32 argTransferLength );      // TRANSFER LENGTH
            argGroupNumber &&& 0x1Fuy;                                                                  // GROUP NUMBER
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                                 // CONTROL
        |]

    /// <summary>
    ///  Generate REPORT SUPPORTED OPERATION CODES CDB
    /// </summary>
    /// <param name="argReportingOptions">
    ///  REPORTING OPTIONS field
    /// </param>
    /// <param name="argRequestedOperationCode">
    ///  REQUESTED OPERATION CODE field
    /// </param>
    /// <param name="argRequestedServiceAction">
    ///  REQUESTED SERVICE ACTION field
    /// </param>
    /// <param name="argAllocationLength">
    ///   ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated REPORT SUPPORTED OPERATION CODES CDB with padding to 16 byte.
    /// </returns>
    static member ReportSupportedOperationCodes ( argReportingOptions : byte ) ( argRequestedOperationCode : byte ) ( argRequestedServiceAction : uint16 ) ( argAllocationLength : uint32 ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0xA3uy;                                                                             // OPERATION CODE
            0x0Cuy;                                                                             // SERVICE ACTION
            argReportingOptions &&& 0x07uy;                                                     // REPORTING OPTIONS
            argRequestedOperationCode;                                                          // REQUESTED OPERATION CODE
            yield! Functions.UInt16ToNetworkBytes_NewVec argRequestedServiceAction;             // REQUESTED SERVICE ACTION
            yield! Functions.UInt32ToNetworkBytes_NewVec argAllocationLength;                   // ALLOCATION LENGTH
            0x00uy;                                                                             // Reserved
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
        |]

    /// <summary>
    ///  Generate REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS CDB
    /// </summary>
    /// <param name="argAllocationLength">
    ///   ALLOCATION LENGTH field
    /// </param>
    /// <param name="argNACA">
    ///  NACA bit in CONTROL field
    /// </param>
    /// <param name="argLINK">
    ///  LINK bit in CONTROL field
    /// </param>
    /// <returns>
    ///  Generated REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS CDB with padding to 16 byte.
    /// </returns>
    static member ReportSupportedTaskManagementFunctions ( argAllocationLength : uint32 ) ( argNACA : NACA ) ( argLINK : LINK ) : byte[] =
        [|
            0xA3uy;                                                                             // OPERATION CODE
            0x0Duy;                                                                             // SERVICE ACTION
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec argAllocationLength;                   // ALLOCATION LENGTH
            0x00uy;                                                                             // Reserved
            ( Functions.SetBitflag ( NACA.toBool argNACA ) 0x04uy ) |||
                ( Functions.SetBitflag ( LINK.toBool argLINK ) 0x01uy )                         // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
        |]
