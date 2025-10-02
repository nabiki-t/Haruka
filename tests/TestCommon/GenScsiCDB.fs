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
open Haruka.Commons

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
    static member Inquiry ( argEVPD : bool ) ( argPageCode : byte ) ( argAllocationLength : uint16 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x12uy;                                                                             // OPERATION CODE
            Functions.SetBitflag argEVPD 0x01uy;                                                // EVPD
            argPageCode;                                                                        // PAGE CODE
            yield! Functions.UInt16ToNetworkBytes_NewVec argAllocationLength                    // ALLOCATION LENGTH
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member ModeSelect6 ( argPageFormat : bool ) ( argSavePages : bool ) ( argParameterListLength : byte ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x15uy;                                                                             // OPERATION CODE
            ( Functions.SetBitflag argPageFormat 0x10uy ) |||                                   // PF
                ( Functions.SetBitflag argSavePages  0x01uy );                                  // SP
            0x00uy; 0x00uy;                                                                     // Reserved
            argParameterListLength;                                                             // PARAMETER LIST LENGTH
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member ModeSelect10 ( argPageFormat : bool ) ( argSavePages : bool ) ( argParameterListLength : uint16 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x55uy;                                                                             // OPERATION CODE
            ( Functions.SetBitflag argPageFormat 0x10uy ) |||                                   // PF
                ( Functions.SetBitflag argSavePages  0x01uy );                                  // SP
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // Reserved
            0x00uy;                                                                             // Reserved
            yield! Functions.UInt16ToNetworkBytes_NewVec argParameterListLength;                // PARAMETER LIST LENGTH
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member ModeSense6 ( argDBD : bool ) ( argPC : byte ) ( argPageCode : byte ) ( argSubPageCode : byte ) ( argAllocationLength : byte ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x1Auy;                                                                             // OPERATION CODE
            Functions.SetBitflag argDBD 0x08uy;                                                 // DBD
            ( ( argPC &&& 0x03uy ) <<< 6 ) |||                                                  // PC
                ( argPageCode &&& 0x3Fuy );                                                     // PAGE CODE
            argSubPageCode;                                                                     // SUB PAGE CODE
            argAllocationLength;                                                                // ALLOCATION LENGTH
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member ModeSense10 ( argLLBAA : bool ) ( argDBD : bool ) ( argPC : byte ) ( argPageCode : byte ) ( argSubPageCode : byte ) ( argAllocationLength : uint16 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x5Auy;                                                                             // OPERATION CODE
            ( Functions.SetBitflag argLLBAA 0x01uy ) |||                                        // LLBAA
                ( Functions.SetBitflag argDBD 0x08uy );                                         // DBD
            ( ( argPC &&& 0x03uy ) <<< 6 ) |||                                                  // PC
                ( argPageCode &&& 0x3Fuy );                                                     // PAGE CODE
            argSubPageCode;                                                                     // SUB PAGE CODE
            0x00uy; 0x00uy; 0x00uy;                                                             // Reserved
            yield! Functions.UInt16ToNetworkBytes_NewVec argAllocationLength                    // ALLOCATION LENGTH 
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member PersistentReserveIn ( argServiceAction : byte ) ( argAllocationLength : uint16 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x5Euy;                                                                             // OPERATION CODE
            ( argServiceAction &&& 0x1Fuy );                                                    // SERVICE ACTION
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // Reserved
            0x00uy;                                                                             // Reserved
            yield! Functions.UInt16ToNetworkBytes_NewVec argAllocationLength                    // ALLOCATION LENGTH 
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member PersistentReserveOut ( argServiceAction : byte ) ( argScope : byte ) ( argType : byte ) ( argParameterListLength : uint32 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x5Fuy;                                                                             // OPERATION CODE
            argServiceAction &&& 0x1Fuy;                                                        // SERVICE ACTION
            ( ( argScope &&& 0x0Fuy ) <<< 4 ) |||                                               // SCOPE
                ( argType &&& 0x0Fuy );                                                         // TYPE
            0x00uy; 0x00uy;                                                                     // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec argParameterListLength;                // PARAMETER LIST LENGTH 
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member PreFetch10 ( argIMMED : bool ) ( argLBA : uint32 ) ( argGroupNumber : byte ) ( argPreFetchLength : uint16 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x34uy;                                                                             // OPERATION CODE
            ( Functions.SetBitflag argIMMED 0x02uy );                                           // IMMED
            yield! Functions.UInt32ToNetworkBytes_NewVec argLBA;                                // LOGICAL BLOCK ADDRESS
            ( argGroupNumber &&& 0x1Fuy );                                                      // GROUP NUMBER
            yield! Functions.UInt16ToNetworkBytes_NewVec argPreFetchLength;                     // PREFETCH LENGTH
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
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
    static member PreFetch16 ( argIMMED : bool ) ( argLBA : uint64 ) ( argGroupNumber : byte ) ( argPreFetchLength : uint32 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x90uy;                                                                             // OPERATION CODE
            Functions.SetBitflag argIMMED 0x02uy;                                               // IMMED
            yield! Functions.UInt64ToNetworkBytes_NewVec argLBA;                                // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec argPreFetchLength;                     // PREFETCH LENGTH
            argGroupNumber &&& 0x1Fuy;                                                          // GROUP NUMBER
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member ReportLUNs ( argSelectReport : byte ) ( argAllocationLength : uint32 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0xA0uy;                                                                             // OPERATION CODE
            0x00uy;                                                                             // Reserved
            argSelectReport;                                                                    // SELECT REPORT
            0x00uy; 0x00uy; 0x00uy;                                                             // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec argAllocationLength;                   // ALLOCATION LENGTH
            0x00uy;                                                                             // Reserved
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member RequestSense ( argDESC : bool ) ( argAllocationLength : byte ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0xA0uy;                                                                             // OPERATION CODE
            Functions.SetBitflag argDESC 0x01uy;                                                // DESC
            0x00uy; 0x00uy;                                                                     // Reserved
            argAllocationLength;                                                                // ALLOCATION LENGTH
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member TestUnitReady ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x00uy;                                                                             // OPERATION CODE
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // Reserved
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member FormatUnit ( argFMTPINFO : bool ) ( argRTO_REQ : bool ) ( argLONGLIST : bool ) ( argFMTDATA : bool ) ( argCMPLIST : bool ) ( argDefectListFormat : byte ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x04uy;                                                                             // OPERATION CODE
            ( Functions.SetBitflag argFMTPINFO 0x80uy ) |||                                     // FMTPINFO
                ( Functions.SetBitflag argRTO_REQ  0x40uy ) |||                                 // RTO_REQ
                ( Functions.SetBitflag argLONGLIST 0x20uy ) |||                                 // LONGLIST
                ( Functions.SetBitflag argFMTDATA  0x10uy ) |||                                 // FMTDATA
                ( Functions.SetBitflag argCMPLIST  0x08uy ) |||                                 // CMPLIST
                ( argDefectListFormat &&& 0x07uy );                                             // DEFECT LIST FORMAT
            0x00uy;                                                                             // Vendor specific
            0x00uy; 0x00uy;                                                                     // Obsoleted
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member Read6 ( argLBA : uint32 ) ( argTransferLength : byte ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x08uy;                                                                             // OPERATION CODE
            byte( ( argLBA &&& 0x001F0000u ) >>> 16 );                                          // LOGICAL BLOCK ADDRESS
            byte( ( argLBA &&& 0x0000FF00u ) >>> 8 );                                           // LOGICAL BLOCK ADDRESS
            byte(   argLBA &&& 0x000000FFu );                                                   // LOGICAL BLOCK ADDRESS
            argTransferLength;                                                                  // TRANSFER LENGTH
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member Read10 ( argRDPROTECT : byte ) ( argDPO : bool ) ( argFUA : bool ) ( argFUA_NV : bool ) ( argLBA : uint32 ) ( argGroupNumber : byte ) ( argTransferLength : uint16 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x28uy;                                                                             // OPERATION CODE
            ( ( argRDPROTECT &&& 0x07uy ) <<< 5 ) |||                                           // RDPROTECT
                ( Functions.SetBitflag argDPO 0x10uy ) |||                                      // DPO
                ( Functions.SetBitflag argFUA 0x08uy ) |||                                      // FUA
                ( Functions.SetBitflag argFUA_NV 0x02uy );                                      // FUA_NV
            yield! Functions.UInt32ToNetworkBytes_NewVec argLBA;                                // LOGICAL BLOCK ADDRESS
            argGroupNumber &&& 0x1Fuy;                                                          // GROUP NUMBER
            yield! Functions.UInt16ToNetworkBytes_NewVec argTransferLength;                     // TRANSFER LENGTH
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;                                                                     // padding
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
    static member Read12 ( argRDPROTECT : byte ) ( argDPO : bool ) ( argFUA : bool ) ( argFUA_NV : bool ) ( argLBA : uint32 ) ( argGroupNumber : byte ) ( argTransferLength : uint32 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0xA8uy;                                                                             // OPERATION CODE
            ( ( argRDPROTECT &&& 0x07uy ) <<< 5 ) |||                                           // RDPROTECT
                ( Functions.SetBitflag argDPO 0x10uy ) |||                                      // DPO
                ( Functions.SetBitflag argFUA 0x08uy ) |||                                      // FUA
                ( Functions.SetBitflag argFUA_NV 0x02uy );                                      // FUA_NV
            yield! Functions.UInt32ToNetworkBytes_NewVec argLBA;                                // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec argTransferLength;                     // TRANSFER LENGTH
            argGroupNumber &&& 0x1Fuy;                                                          // GROUP NUMBER
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
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
    static member Read16 ( argRDPROTECT : byte ) ( argDPO : bool ) ( argFUA : bool ) ( argFUA_NV : bool ) ( argLBA : uint64 ) ( argGroupNumber : byte ) ( argTransferLength : uint32 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x88uy;                                                                             // OPERATION CODE
            ( ( argRDPROTECT &&& 0x07uy ) <<< 5 ) |||                                           // RDPROTECT
                ( Functions.SetBitflag argDPO 0x10uy ) |||                                      // DPO
                ( Functions.SetBitflag argFUA 0x08uy ) |||                                      // FUA
                ( Functions.SetBitflag argFUA_NV 0x02uy );                                      // FUA_NV
            yield! Functions.UInt64ToNetworkBytes_NewVec argLBA;                                // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec argTransferLength;                     // TRANSFER LENGTH
            argGroupNumber &&& 0x1Fuy;                                                          // GROUP NUMBER
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member ReadCapacity10 ( argLBA : uint32 ) ( argPMI : bool ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x25uy;                                                                             // OPERATION CODE
            0x00uy;                                                                             // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec argLBA;                                // LOGICAL BLOCK ADDRESS
            0x00uy; 0x00uy;                                                                     // Reserved
            Functions.SetBitflag argPMI 0x01uy;                                                 // PMI
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member ReadCapacity16 ( argLBA : uint64 ) ( argAllocationLength : uint32 ) ( argPMI : bool ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x9Euy;                                                                             // OPERATION CODE
            0x10uy;                                                                             // SERVICE ACTION
            yield! Functions.UInt64ToNetworkBytes_NewVec argLBA;                                // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec argAllocationLength;                   // ALLOCATION LENGTH
            Functions.SetBitflag argPMI 0x01uy;                                                 // PMI
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member SynchronizeCache10 ( argSYNC_NV : bool ) ( argIMMED : bool ) ( argLBA : uint32 ) ( argGroupNumber : byte ) ( argNumberOfBlockes : uint16 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x35uy;                                                                             // OPERATION CODE
            ( Functions.SetBitflag argSYNC_NV 0x04uy ) |||                                      // SYNC_NV
                ( Functions.SetBitflag argIMMED 0x02uy );                                       // IMMED
            yield! Functions.UInt32ToNetworkBytes_NewVec argLBA;                                // LOGICAL BLOCK ADDRESS
            argGroupNumber &&& 0x1Fuy;                                                          // GROUP NUMBER
            yield! Functions.UInt16ToNetworkBytes_NewVec argNumberOfBlockes;                    // NUMBER OF BLOCKS
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;                                                                     // padding
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
    static member SynchronizeCache16 ( argSYNC_NV : bool ) ( argIMMED : bool ) ( argLBA : uint64 ) ( argGroupNumber : byte ) ( argNumberOfBlockes : uint32 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x91uy;                                                                             // OPERATION CODE
            ( Functions.SetBitflag argSYNC_NV 0x04uy ) |||                                      // SYNC_NV
                ( Functions.SetBitflag argIMMED 0x02uy );                                       // IMMED
            yield! Functions.UInt64ToNetworkBytes_NewVec argLBA;                                // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec argNumberOfBlockes;                    // NUMBER OF BLOCKS
            argGroupNumber &&& 0x1Fuy;                                                          // GROUP NUMBER
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member Write6 ( argLBA : uint32 ) ( argTransferLength : byte )( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x0Auy;                                                                             // OPERATION CODE
            byte( ( argLBA &&& 0x001F0000u ) >>> 16 );                                          // LOGICAL BLOCK ADDRESS
            byte( ( argLBA &&& 0x0000FF00u ) >>> 8 );                                           // LOGICAL BLOCK ADDRESS
            byte(   argLBA &&& 0x000000FFu );                                                   // LOGICAL BLOCK ADDRESS
            argTransferLength;                                                                  // TRANSFER LENGTH
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member Write10 ( argWRPROTECT : byte ) ( argDPO : bool ) ( argFUA : bool ) ( argFUA_NV : bool ) ( argLBA : uint32 ) ( argGroupNumber : byte ) ( argTransferLength : uint16 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x2Auy;                                                                             // OPERATION CODE
            ( ( argWRPROTECT &&& 0x07uy ) <<< 5 ) |||                                           // WRPROTECT
                ( Functions.SetBitflag argDPO 0x10uy ) |||                                      // DPO
                ( Functions.SetBitflag argFUA 0x08uy ) |||                                      // FUA
                ( Functions.SetBitflag argFUA_NV 0x02uy );                                      // FUA_NV
            yield! Functions.UInt32ToNetworkBytes_NewVec argLBA;                                // LOGICAL BLOCK ADDRESS
            argGroupNumber &&& 0x1Fuy;                                                          // GROUP NUMBER
            yield! Functions.UInt16ToNetworkBytes_NewVec argTransferLength;                     // TRANSFER LENGTH
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
            0x00uy; 0x00uy;                                                                     // padding
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
    static member Write12 ( argWRPROTECT : byte ) ( argDPO : bool ) ( argFUA : bool ) ( argFUA_NV : bool ) ( argLBA : uint32 ) ( argGroupNumber : byte ) ( argTransferLength : uint32 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0xAAuy;                                                                             // OPERATION CODE
            ( ( argWRPROTECT &&& 0x07uy ) <<< 5 ) |||                                           // WRPROTECT
                ( Functions.SetBitflag argDPO 0x10uy ) |||                                      // DPO
                ( Functions.SetBitflag argFUA 0x08uy ) |||                                      // FUA
                ( Functions.SetBitflag argFUA_NV 0x02uy );                                      // FUA_NV
            yield! Functions.UInt32ToNetworkBytes_NewVec argLBA;                                // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec argTransferLength;                     // TRANSFER LENGTH
            argGroupNumber &&& 0x1Fuy;                                                          // GROUP NUMBER
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
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
    static member Write16 ( argWRPROTECT : byte ) ( argDPO : bool ) ( argFUA : bool ) ( argFUA_NV : bool ) ( argLBA : uint64 ) ( argGroupNumber : byte ) ( argTransferLength : uint32 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0x8Auy;                                                                             // OPERATION CODE
            ( ( argWRPROTECT &&& 0x07uy ) <<< 5 ) |||                                           // WRPROTECT
                ( Functions.SetBitflag argDPO 0x10uy ) |||                                      // DPO
                ( Functions.SetBitflag argFUA 0x08uy ) |||                                      // FUA
                ( Functions.SetBitflag argFUA_NV 0x02uy );                                      // FUA_NV
            yield! Functions.UInt64ToNetworkBytes_NewVec argLBA;                                // LOGICAL BLOCK ADDRESS
            yield! Functions.UInt32ToNetworkBytes_NewVec argTransferLength;                     // TRANSFER LENGTH
            argGroupNumber &&& 0x1Fuy;                                                          // GROUP NUMBER
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member ReportSupportedOperationCodes ( argReportingOptions : byte ) ( argRequestedOperationCode : byte ) ( argRequestedServiceAction : uint16 ) ( argAllocationLength : uint32 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0xA3uy;                                                                             // OPERATION CODE
            0x0Cuy;                                                                             // SERVICE ACTION
            argReportingOptions &&& 0x07uy;                                                     // REPORTING OPTIONS
            argRequestedOperationCode;                                                          // REQUESTED OPERATION CODE
            yield! Functions.UInt16ToNetworkBytes_NewVec argRequestedServiceAction;             // REQUESTED SERVICE ACTION
            yield! Functions.UInt32ToNetworkBytes_NewVec argAllocationLength;                   // ALLOCATION LENGTH
            0x00uy;                                                                             // Reserved
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
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
    static member ReportSupportedTaskManagementFunctions ( argAllocationLength : uint32 ) ( argNACA : bool ) ( argLINK : bool ) : byte[] =
        [|
            0xA3uy;                                                                             // OPERATION CODE
            0x0Duy;                                                                             // SERVICE ACTION
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // Reserved
            yield! Functions.UInt32ToNetworkBytes_NewVec argAllocationLength;                   // ALLOCATION LENGTH
            0x00uy;                                                                             // Reserved
            ( Functions.SetBitflag argNACA 0x04uy ) ||| ( Functions.SetBitflag argLINK 0x01uy ) // CONTROL
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                                                     // padding
        |]