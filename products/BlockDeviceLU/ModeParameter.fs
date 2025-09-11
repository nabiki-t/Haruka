//=============================================================================
// Haruka Software Storage.
// ModeParameter.fs : Defines ModeParameter class.
// ModeParameter class manage mode parameter values belongings to block device LU.
// 
// Following mode pages are not supported in Haruka.
//  * Disconnect-Reconnect mode page (SPC-3 7.4.8)
//  * Extended mode page(SPC-3 7.4.9)
//  * Extended Device-Type Specific mode page(SPC-3 7.4.10)
//  * Informational Exceptions Control mode page(SPC-3 7.4.11)
//  * Power Condition mode page(SPC-3 7.4.12)
//  * Protocol Specific Logical Unit mode page(SPC-3 7.4.13)
//  * Protocol Specific Port mode page(SPC-3 7.4.14)
//  *  Caching mode page(SBC-2 6.3.3)
//  * Verify Error Recovery mode page(SBC-2 6.3.5)
//  * XOR Control mode page(SBC-2 6.3.6)

//=============================================================================
// Namespace declaration

namespace Haruka.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

/// <summary>
///  ModeParameter class. This class holds and management mode parameter values.
/// </summary>
/// <param name="m_Media">
///  Refelense of the Media object.
/// </param>
/// <param name="m_LUN">
///  LUN of the logical unit which this mode parameter belongings to.
/// </param>
type ModeParameter
    (
        m_Media : IMedia,
        m_LUN : LUN_T
    ) =

    // ========================================================================
    // Mode parameter header values

    /// DPOFUA
    static let m_DPOFUA = false

    // ========================================================================
    // Mode parameter block descriptors

    /// BLOCK LENGTH
    let mutable m_BlockLength = Constants.MEDIA_BLOCK_SIZE

    // ========================================================================
    // Cache mode page
    // PS : false ( Not savable )
    // SPF : false
    // PAGE CODE : 0x08uy
    // mode page policy : shared

    /// IC(initiator control)
    static let m_IC = false

    /// ABPF(abort pre-fetch)
    static let m_ABPF = false

    /// CAP(caching analysis permitted)
    static let m_CAP = false

    /// DISC(discontinuity)
    static let m_DISC = false

    /// SIZE(size enable)
    static let m_SIZE = false

    /// WCE(writeback cache enable)
    static let m_WCE = false

    /// MF(multiplication factor)
    static let m_MF = false

    /// RCD(read cache disable)
    static let m_RCD = false

    /// DEMAND READ RETENTION PRIORITY
    static let m_DemandReadRetentionPriority = 0uy

    /// WRITE RETENTION PRIORITY
    static let m_WriteRetentionPriority = 0uy

    /// DISABLE PRE-FETCH TRANSFER LENGTH
    static let m_DisablePreFetchTransferLength = 0us

    /// MINIMUM PRE-FETCH
    static let m_MinimumPreFetch = 0us

    ///  MAXIMUM PRE-FETCH
    static let m_MaximumPreFetch = 0us

    ///  MAXIMUM PRE-FETCH CEILING
    static let m_MaximumPreFetchCeiling = 0us

    /// FSW(force sequential write)
    static let m_FSW = false

    /// LBCSS(logical block cache segment size)
    static let m_LBCSS = false

    /// DRA(disable read-ahead)
    static let m_DRA = false

    /// NV_DIS
    static let m_NV_DIS = false

    /// NUMBER OF CACHE SEGMENTS
    static let m_NumberOfCacheSegments = 0uy

    /// CACHE SEGMENT SIZE
    static let m_CacheSegmentSize = 0us

    // ========================================================================
    // Control mode page
    // PS : false ( Not savable )
    // SPF : false
    // PAGE CODE : 0x0Auy
    // mode page policy : shared
    
    /// TST(task set type)
    static let m_TST = 0x00uy

    /// TMF_ONLY(allow task management function only)
    static let m_TMF_ONLY = false

    /// D_SENSE(descriptor format sense data) default value
    static let m_D_SENSE_Default = true

    /// D_SENSE
    let mutable m_D_SENSE = m_D_SENSE_Default

    /// GLTSD(global logging target save disable)
    static let m_GLTSD = false

    /// RLEC(report log exception condition)
    static let m_RLEC = false

    /// QUEUE ALGORITHM MODIFIER
    static let m_QueueAlgorithmModifier = 0x01uy

    /// QERR(queue error management)
    static let m_QERR = 0x00uy

    /// RAC(report a check)
    static let m_RAC = false

    /// UA_INTLCK_CTRL(unit attention interlocks control)
    static let m_UA_INTLCK_CTRL = 0x00uy

    /// SWP(software write protect) default value
    static let m_SWP_Default = false

    /// SWP
    let mutable m_SWP = m_SWP_Default

    /// ATO(application tag owner)
    static let m_ATO = false

    /// TAS(task aborted status)
    static let m_TAS = true

    /// BUSY TIMEOUT PERIOD
    static let m_BusyTimePeriod = 0x0000us

    /// EXTENDED SELF-TEST COMPLETION TIME
    static let m_ExtendedSelfTestCompletionTime = 0x00us

    // ========================================================================
    // Informational Exceptions Control mode page
    // PS : false ( Not savable )
    // SPF : false
    // PAGE CODE : 0x1Cuy
    // mode page policy : shared

    /// PERF(performance)
    static let m_PERF = false

    /// EBF(Enable Background Function)
    static let m_EBF = true

    /// EWASC(enable warning)
    static let m_EWASC = true

    /// DEXCPT(disable exception control)
    static let m_DEXCPT = true

    /// TEST(test)
    static let m_TEST = false

    /// LOGERR(log error)
    static let m_LOGERR = true

    /// MRIE(method of reporting informational exceptions)
    static let m_MRIE = 0x2uy

    /// INTERVAL TIMER
    static let m_IntervalTimer = 0x00u

    /// REPORT COUNT
    static let m_ReportCount = 0x00u

    /// Hash value identify this instance
    static let m_ObjID = objidx_me.NewID()

    do
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g -> g.Gen2( m_ObjID, ValueNone, ValueNone, ValueSome m_LUN, "ModeParameter", "" ) )

    // ========================================================================
    // Properties
    // * All propaties are read only.

    member _.BlockLength : uint64 =
        m_BlockLength

    /// DPOFUA property
    member _.DPOFUA : bool =
        m_DPOFUA

    /// IC property
    member _.IC : bool =
        m_IC

    /// ABPF property
    member _.ABPF : bool =
        m_ABPF

    /// CAP property
    member _.CAP : bool =
        m_CAP

    /// DISC property
    member _.DISC : bool =
        m_DISC

    /// SIZE property
    member _.SIZE : bool =
        m_SIZE

    /// WCE property
    member _.WCE : bool =
        m_WCE

    /// MF property
    member _.MF : bool =
        m_MF

    /// RCD property
    member _.RCD : bool =
        m_RCD

    /// DEMAND READ RETENTION PRIORITY property
    member _.DemandReadRetentionPriority : byte =
        m_DemandReadRetentionPriority

    /// WRITE RETENTION PRIORITY property
    member _.WriteRetentionPriority : byte =
        m_WriteRetentionPriority

    /// DISABLE PRE-FETCH TRANSFER LENGTH property
    member _.DisablePreFetchTransferLength : uint16 =
        m_DisablePreFetchTransferLength

    /// MINIMUM PRE-FETCH property
    member _.MinimumPreFetch : uint16 =
        m_MinimumPreFetch

    ///  MAXIMUM PRE-FETCH property
    member _.MaximumPreFetch : uint16 =
        m_MaximumPreFetch

    ///  MAXIMUM PRE-FETCH CEILING property
    member _.MaximumPreFetchCeiling : uint16 =
        m_MaximumPreFetchCeiling

    /// FSW property
    member _.FSW : bool =
        m_FSW

    /// LBCSS property
    member _.LBCSS : bool =
        m_LBCSS

    /// DRA property
    member _.DRA : bool =
        m_DRA

    /// NV_DIS
    member _.NV_DIS : bool =
        m_NV_DIS

    /// NUMBER OF CACHE SEGMENTS
    member _.NumberOfCacheSegments : byte =
        m_NumberOfCacheSegments

    /// CACHE SEGMENT SIZE
    member _.CacheSegmentSize : uint16 =
        m_CacheSegmentSize

    
    /// TST property
    member _.TST : byte =
        m_TST

    /// TMF_ONLY property
    member _.TMF_ONLY : bool =
        m_TMF_ONLY

    /// D_SENSE property
    member _.D_SENSE : bool =
        m_D_SENSE

    /// GLTSD property
    member _.GLTSD : bool =
        m_GLTSD

    /// RLEC property
    member _.RLEC : bool =
        m_RLEC

    /// QUEUE ALGORITHM MODIFIER property
    member _.QueueAlgorithmModifier : byte =
        m_QueueAlgorithmModifier

    /// QERR property
    member _.QERR : byte =
        m_QERR

    /// TAS property
    member _.TAS : bool =
        m_TAS

    /// RAC property
    member _.RAC : bool =
        m_RAC

    /// UA_INTLCK_CTRL property
    member _.UA_INTLCK_CTRL : byte =
        0x2uy

    /// SWP property ( updated from the initiator )
    member _.SWP : bool =
        m_SWP

    /// ATO property
    member _.ATO : bool =
        m_ATO

    /// BUSY TIMEOUT PERIOD property
    member _.BusyTimePeriod : uint16 =
        m_BusyTimePeriod

    /// EXTENDED SELF-TEST COMPLETION TIME property
    member _.ExtendedSelfTestCompletionTime : uint16 =
        m_ExtendedSelfTestCompletionTime

    /// PERF property
    member _.PERF : bool =
        m_PERF

    /// EBF property
    member _.EBF : bool =
        m_EBF

    /// EWASC property
    member _.EWASC : bool =
        m_EWASC

    /// DEXCPT property
    member _.DEXCPT : bool =
        m_DEXCPT

    /// TEST property
    member _.TEST : bool =
        m_TEST

    /// LOGERR property
    member _.LOGERR : bool =
        m_LOGERR

    /// MRIE property
    member _.MRIE : byte =
        m_MRIE

    /// INTERVAL TIMER property
    member _.IntervalTimer : uint32 =
        m_IntervalTimer

    /// REPORT COUNT property
    member _.ReportCount : uint32 =
        m_ReportCount

    /// <summary>
    /// Set mode parameter values by MODE SELECT(6) command.
    /// </summary>
    /// <param name="v">
    /// Received bytes array by MODE SELECT(6) command.
    /// </param>
    /// <param name="parameterLength">
    /// PARAMETER LENGTH value of MODE SELECT(6) CDB.
    /// </param>
    /// <param name="pf">
    /// PF(Page Format) value of MODE SELECT(6) CDB.
    /// </param>
    /// <param name="sp">
    /// SP(Save Pages) value of MODE SELECT(6) CDB.
    /// </param>
    /// <param name="source">
    /// Command source information.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag value of SCSI command.
    /// </param>
    member this.Select6 ( v : PooledBuffer ) ( parameterLength : int ) ( pf : bool ) ( sp : bool ) ( source : CommandSourceInfo ) ( itt : ITT_T ) : unit =
        let loginfo = struct ( m_ObjID, ValueSome source, ValueSome itt, ValueSome m_LUN )

        if v.Count < 4 || parameterLength < 4 || v.Count < parameterLength then
            // Parameter list length error
            let errmsg = sprintf "Invalie parameter list length(%d), in MODE SELECT(6) command CDB." v.Count
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 4us },
                errmsg
            )

        let mediumType = int v.[1]
        let blockDescriptorLength = int v.[3]

        if mediumType <> 0 then
            let errmsg = sprintf "Invalid MEDIUM TYPE value(%d), in MODE SELECT(6) parameter list." mediumType
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = 1us },
                errmsg
            )

        if blockDescriptorLength > parameterLength - 4 || ( blockDescriptorLength <> 0 && blockDescriptorLength <> 8 ) then
            let errmsg = sprintf "Invalid BLOCK DESCRIPTOR LENGTH value(%d), in MODE SELECT(6) parameter list." blockDescriptorLength
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = 3us },
                errmsg
            )

        if blockDescriptorLength > 0 then
            let blockLength = ( Functions.NetworkBytesToUInt32_InPooledBuffer v 8 ) &&& 0x00FFFFFFu
            m_BlockLength <- uint64 blockLength

        // If PF bit is 0( following data is vendor specific ), following data is ignored.
        if pf && blockDescriptorLength + 4 < parameterLength then
            let rec loop s =
                let next =
                    match v.[s] &&& 0x3Fuy with
                    | 0x08uy -> // Cache mode page
                        this.ReadCacheModePageByteData v s ( int parameterLength ) source itt
                    | 0x0Auy -> // Control mode page
                        this.ReadControlModePageByteData v s ( int parameterLength ) source itt
                    | 0x1Cuy -> // Informational Exceptions Control mode page
                        this.ReadInformationalExceptionsControlModePageByteData v s ( int parameterLength ) source itt
                    | _ ->      // Unknown
                        let errmsg = sprintf "Unsupported page code value(0x%02X), in MODE SELECT(6) parameter list." ( v.[s] &&& 0x3Fuy )
                        HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, errmsg )
                        raise <| SCSIACAException (
                            source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                            { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = ( uint16 s ) },
                            errmsg
                        )

                if next < parameterLength then
                    loop next
            loop ( int blockDescriptorLength + 4 )


    /// <summary>
    /// Get mode parameter values by MODE SENSE(6) command.
    /// </summary>
    /// <param name="dbd">
    /// DBD value of MODE SENSE(6) CDB.
    /// </param>
    /// <param name="pageCode">
    /// PAGE CODE value of MODE SENSE(6) CDB.
    /// </param>
    /// <param name="subPageCode">
    /// SUB PAGE CODE value of MODE SENSE(6) CDB.
    /// </param>
    /// <param name="pc">
    /// PC value of MODE SENSE(6) CDB.
    /// </param>
    /// <param name="source">
    /// Command source information.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag value of SCSI command.
    /// </param>
    /// <returns>
    /// Bytes array to return for MODE SENSE(6) command.
    /// </returns>
    member this.Sense6 ( dbd : bool ) ( pageCode : byte ) ( subPageCode : byte ) ( pc : byte ) ( source : CommandSourceInfo ) ( itt : ITT_T ) : byte[] =
        let loginfo = struct ( m_ObjID, ValueSome source, ValueSome itt, ValueSome m_LUN )

        let mediumType = 0uy         // Block Device(0h)
        let deviceSpecificParameter = this.GetDeviceSpecificParameter()
        let modeParameterBlockDescriptor : byte[] =
            if not dbd then
                this.GetShortLBAModeParamterBlockDescriptor()
            else
                Array.empty

        let modePage =
            if ( pc = 0x00uy || pc = 0x03uy ) && pageCode = 0x08uy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetCacheModePage_Current()
            elif ( pc = 0x00uy || pc = 0x03uy ) && pageCode = 0x0Auy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetControlModePage_Current()
            elif ( pc = 0x00uy || pc = 0x03uy ) && pageCode = 0x1Cuy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetInformationalExceptionsControlModePage_Current()
            elif ( pc = 0x00uy || pc = 0x03uy ) && pageCode = 0x3Fuy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                [|
                    yield! this.GetCacheModePage_Current();
                    yield! this.GetControlModePage_Current();
                    yield! this.GetInformationalExceptionsControlModePage_Current();
                |]
            elif pc = 0x01uy && pageCode = 0x08uy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetCacheModePage_Changeable()
            elif pc = 0x01uy && pageCode = 0x0Auy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetControlModePage_Changeable()
            elif pc = 0x01uy && pageCode = 0x1Cuy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetInformationalExceptionsControlModePage_Changeable()
            elif pc = 0x01uy && pageCode = 0x3Fuy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                [|
                    yield! this.GetCacheModePage_Changeable();
                    yield! this.GetControlModePage_Changeable();
                    yield! this.GetInformationalExceptionsControlModePage_Changeable();
                |]
            elif pc = 0x02uy && pageCode = 0x08uy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetCacheModePage_Default()
            elif pc = 0x02uy && pageCode = 0x0Auy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetControlModePage_Default()
            elif pc = 0x02uy && pageCode = 0x1Cuy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetInformationalExceptionsControlModePage_Default()
            elif pc = 0x02uy && pageCode = 0x3Fuy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                [|
                    yield! this.GetCacheModePage_Default();
                    yield! this.GetControlModePage_Default();
                    yield! this.GetInformationalExceptionsControlModePage_Default();
                |]
            else
                // Unsupported page code or PC field value
                let errmsg = sprintf "In MODE SENSE(6) CDB, unsupported PAGE CODE(0x%02X) and SUB PAGE CODE(0x%02X) is specified." pageCode subPageCode
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 5uy; FieldPointer = 2us },
                    errmsg
                )

        let modeDataLength = modeParameterBlockDescriptor.Length + modePage.Length + 3
        if modeDataLength >= 256 then
            HLogger.Trace( LogID.F_ERROR_EXIT, fun g ->
                g.Gen1( loginfo, "Mode parameter data length is over than 256 bytes." )
            )
            exit( 1 )

        let modeParameterHeader = [|
            byte modeDataLength;
            mediumType;
            deviceSpecificParameter;
            byte ( modeParameterBlockDescriptor.Length );
        |]

        [|
            yield! modeParameterHeader;
            yield! modeParameterBlockDescriptor;
            yield! modePage;
        |]


    /// <summary>
    /// Set mode parameter values by MODE SELECT(10) command.
    /// </summary>
    /// <param name="v">
    /// Received bytes array by MODE SELECT(10) command.
    /// </param>
    /// <param name="parameterLength">
    /// PARAMETER LENGTH value of MODE SELECT(10) CDB.
    /// </param>
    /// <param name="pf">
    /// PF(Page Format) value of MODE SELECT(10) CDB.
    /// </param>
    /// <param name="sp">
    /// SP(Save Pages) value of MODE SELECT(10) CDB.
    /// </param>
    /// <param name="source">
    /// Command source information.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag value of SCSI command.
    /// </param>
    member this.Select10 ( v: PooledBuffer ) ( parameterLength : int ) ( pf : bool ) ( sp : bool ) ( source : CommandSourceInfo ) ( itt : ITT_T ) : unit =
        let loginfo = struct ( m_ObjID, ValueSome source, ValueSome itt, ValueSome m_LUN )

        if v.Count < 8 || parameterLength < 8 || v.Count < parameterLength then
            // Parameter list length error
            let errmsg = sprintf "Invalie parameter list length(%d), in MODE SELECT(10) command CDB." v.Count
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 4us },
                errmsg
            )

        let mediumType = int v.[2]
        let blockDescriptorLength = Functions.NetworkBytesToUInt16_InPooledBuffer v 6 |> int
        let longLBA = Functions.CheckBitflag v.[4] 0x01uy

        if mediumType <> 0 then
            let errmsg = sprintf "Invalid MEDIUM TYPE value(%d), in MODE SELECT(10) parameter list." mediumType
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = 1us },
                errmsg
            )

        if not ( blockDescriptorLength = 0 || ( not longLBA && blockDescriptorLength = 8 ) || ( longLBA && blockDescriptorLength = 16 ) ) ||
            blockDescriptorLength > parameterLength - 8 then

            let errmsg = sprintf "Invalid BLOCK DESCRIPTOR LENGTH value(%d), in MODE SELECT(10) parameter list." blockDescriptorLength
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = 6us },
                errmsg
            )

        if blockDescriptorLength > 0 then
            m_BlockLength <-
                if blockDescriptorLength = 8 then
                    uint64 ( ( Functions.NetworkBytesToUInt32_InPooledBuffer v 12 ) &&& 0x00FFFFFFu )
                else
                    uint64 ( Functions.NetworkBytesToUInt32_InPooledBuffer v 20 )

        // If PF bit is 0( following data is vendor specific ), following data is ignored.
        if pf && blockDescriptorLength + 8 < parameterLength then
            let rec loop s =
                let next =
                    match v.[s] &&& 0x3Fuy with
                    | 0x08uy -> // Cache mode page
                        this.ReadCacheModePageByteData v s ( int parameterLength ) source itt
                    | 0x0Auy -> // Control mode page
                        this.ReadControlModePageByteData v s ( int parameterLength ) source itt
                    | 0x1Cuy -> // Informational Exceptions Control mode page
                        this.ReadInformationalExceptionsControlModePageByteData v s ( int parameterLength ) source itt
                    | _ ->      // Unknown
                        let errmsg = sprintf "Unsupported page code value(0x%02X), in MODE SELECT(10) parameter list." ( v.[s] &&& 0x3Fuy )
                        HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, errmsg )
                        raise <| SCSIACAException (
                            source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                            { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = ( uint16 s ) },
                            errmsg
                        )

                if next < parameterLength then
                    loop next
            loop ( int blockDescriptorLength + 8 )

    /// <summary>
    /// Get mode parameter values by MODE SENSE(10) command.
    /// </summary>
    /// <param name="llbaa">
    /// LLBAA value of MODE SENSE(10) CDB.
    /// </param>
    /// <param name="dbd">
    /// DBD value of MODE SENSE(10) CDB.
    /// </param>
    /// <param name="pageCode">
    /// PAGE CODE value of MODE SENSE(10) CDB.
    /// </param>
    /// <param name="subPageCode">
    /// SUB PAGE CODE value of MODE SENSE(10) CDB.
    /// </param>
    /// <param name="pc">
    /// PC value of MODE SENSE(10) CDB.
    /// </param>
    /// <param name="source">
    /// Command source information.
    /// </param>
    /// <param name="itt">
    /// Initiator Task Tag value of SCSI command.
    /// </param>
    /// <returns>
    /// Bytes array to return for mode sense(10) command.
    /// </returns>
    member this.Sense10 ( llbaa : bool ) ( dbd : bool ) ( pageCode : byte ) ( subPageCode : byte ) ( pc : byte ) ( source : CommandSourceInfo ) ( itt : ITT_T ) : byte[] =
        let loginfo = struct ( m_ObjID, ValueSome source, ValueSome itt, ValueSome m_LUN )

        let mediumType = 0uy         // Block Device(0h)
        let deviceSpecificParameter = this.GetDeviceSpecificParameter()
        let modeParameterBlockDescriptor : byte[] =
            if not dbd then
                if llbaa then
                    this.GetLongLBAModeParamterBlockDescriptor()
                else
                    this.GetShortLBAModeParamterBlockDescriptor()
            else
                Array.empty

        let modePage =
            if ( pc = 0x00uy || pc = 0x03uy ) && pageCode = 0x08uy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetCacheModePage_Current()
            elif ( pc = 0x00uy || pc = 0x03uy ) && pageCode = 0x0Auy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetControlModePage_Current()
            elif ( pc = 0x00uy || pc = 0x03uy ) && pageCode = 0x1Cuy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetInformationalExceptionsControlModePage_Current()
            elif ( pc = 0x00uy || pc = 0x03uy ) && pageCode = 0x3Fuy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                [|
                    yield! this.GetCacheModePage_Current();
                    yield! this.GetControlModePage_Current();
                    yield! this.GetInformationalExceptionsControlModePage_Current();
                |]
            elif pc = 0x01uy && pageCode = 0x08uy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetCacheModePage_Changeable()
            elif pc = 0x01uy && pageCode = 0x0Auy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetControlModePage_Changeable()
            elif pc = 0x01uy && pageCode = 0x1Cuy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetInformationalExceptionsControlModePage_Changeable()
            elif pc = 0x01uy && pageCode = 0x3Fuy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                [|
                    yield! this.GetCacheModePage_Changeable();
                    yield! this.GetControlModePage_Changeable();
                    yield! this.GetInformationalExceptionsControlModePage_Changeable();
                |]
            elif pc = 0x02uy && pageCode = 0x08uy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetCacheModePage_Default()
            elif pc = 0x02uy && pageCode = 0x0Auy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetControlModePage_Default()
            elif pc = 0x02uy && pageCode = 0x1Cuy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                this.GetInformationalExceptionsControlModePage_Default()
            elif pc = 0x02uy && pageCode = 0x3Fuy && ( subPageCode = 0x00uy || subPageCode = 0xFFuy ) then
                [|
                    yield! this.GetCacheModePage_Default();
                    yield! this.GetControlModePage_Default();
                    yield! this.GetInformationalExceptionsControlModePage_Default();
                |]
            else
                // Unsupported page code or PC field value
                let errmsg = sprintf "In MODE SENSE(10) CDB, unsupported PAGE CODE(0x%02X) and SUB PAGE CODE(0x%02X) is specified." pageCode subPageCode
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 5uy; FieldPointer = 2us },
                    errmsg
                )

        let modeDataLength = modeParameterBlockDescriptor.Length + modePage.Length + 6
        if modeDataLength >= 65536 then
            HLogger.Trace( LogID.F_ERROR_EXIT, fun g ->
                g.Gen1( loginfo, "Mode parameter data length is over than 65535 bytes." )
            )
            exit( 1 )

        let modeParameterHeader = [|
            yield! Functions.Int16ToNetworkBytes_NewVec ( int16 modeDataLength );
            mediumType;
            deviceSpecificParameter;
            if llbaa then 0x01uy else 0x00uy;
            0x00uy;
            yield! Functions.Int16ToNetworkBytes_NewVec ( int16 modeParameterBlockDescriptor.Length );
        |]

        [|
            yield! modeParameterHeader;
            yield! modeParameterBlockDescriptor;
            yield! modePage;
        |]

    /// <summary>
    ///  Get device specific parameter bytes array.
    /// </summary>
    member private _.GetDeviceSpecificParameter() : byte =
        // WP : （write protect)
        let wp =
            if m_Media.WriteProtect || m_SWP then
                0x80uy
            else
                0x00uy
        let dpofua = Functions.SetBitflag m_DPOFUA 0x10uy
        wp ||| dpofua

    /// <summary>
    ///  Get short LBA mode parameter block descriptor bytes array.
    /// </summary>
    member private _.GetShortLBAModeParamterBlockDescriptor() : byte[] =
        let bc =
            let w = m_Media.BlockCount
            if w > 0xFFFFFFFFUL then
                0xFFFFFFFFu
            else
                uint32 w
        let bl = uint32 m_BlockLength
        [|
            yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 bc );
            yield! Functions.UInt32ToNetworkBytes_NewVec ( bl &&& 0x00FFFFFFu );
        |]

    /// <summary>
    ///  Get long LBA mode parameter block descriptor bytes array.
    /// </summary>
    member private _.GetLongLBAModeParamterBlockDescriptor() : byte[] =
        let bc = m_Media.BlockCount
        let bl = uint32 m_BlockLength
        [|
            yield! Functions.UInt64ToNetworkBytes_NewVec bc;
            0x00uy;
            0x00uy;
            0x00uy;
            0x00uy;
            yield! Functions.UInt32ToNetworkBytes_NewVec bl;
        |]

    /// <summary>
    ///  Get current value of cache mode page bytes array.
    /// </summary>
    member private _.GetCacheModePage_Current() : byte[] =
        [|
            0x08uy; // PS(false), SPF(0b), PAGE CODE(0x08)
            0x12uy;                                             // PAGE LENGTH(0x12)
            ( Functions.SetBitflag m_IC   0x80uy ) |||
                ( Functions.SetBitflag m_ABPF 0x40uy ) |||
                ( Functions.SetBitflag m_CAP  0x20uy ) |||
                ( Functions.SetBitflag m_DISC 0x10uy ) |||
                ( Functions.SetBitflag m_SIZE 0x08uy ) |||
                ( Functions.SetBitflag m_WCE  0x04uy ) |||
                ( Functions.SetBitflag m_MF   0x02uy ) |||
                ( Functions.SetBitflag m_RCD  0x01uy );
            ( m_DemandReadRetentionPriority <<< 4 ) |||
                ( m_WriteRetentionPriority &&& 0x0Fuy );
            yield! Functions.UInt16ToNetworkBytes_NewVec m_DisablePreFetchTransferLength;
            yield! Functions.UInt16ToNetworkBytes_NewVec m_MinimumPreFetch;
            yield! Functions.UInt16ToNetworkBytes_NewVec m_MaximumPreFetch;
            yield! Functions.UInt16ToNetworkBytes_NewVec m_MaximumPreFetchCeiling;
            ( Functions.SetBitflag m_FSW   0x80uy ) |||
                ( Functions.SetBitflag m_LBCSS  0x40uy ) |||
                ( Functions.SetBitflag m_DRA    0x20uy ) |||
                ( Functions.SetBitflag m_NV_DIS 0x01uy );
            m_NumberOfCacheSegments;
            yield! Functions.UInt16ToNetworkBytes_NewVec m_CacheSegmentSize;
            0x00uy;
            0x00uy;
            0x00uy;
            0x00uy;
        |]

    /// <summary>
    ///  Get default value of cache mode page bytes array.
    /// </summary>
    member private this.GetCacheModePage_Default() : byte[] =
        this.GetCacheModePage_Current()

    /// <summary>
    ///  Get cache mode page changeable values mask bytes array.
    /// </summary>
    member private _.GetCacheModePage_Changeable() : byte[] =
        [|
            0x08uy;         // PS(false), SPF(0b), PAGE CODE(0x08)
            0x12uy;         // PAGE LENGTH(0x12)
            0x00uy;         // IC,ABPF,CAP,DISC,SIZE,WCE,MF,RCD
            0x00uy;         // DEMAND READ RETENTION PRIORITY, WRITE RETENTION PRIORITY
            0x00uy; 0x00uy; // DISABLE PRE-FETCH TRANSFER LENGTH
            0x00uy; 0x00uy; // MINIMUM PRE-FETCH
            0x00uy; 0x00uy; // MAXIMUM PRE-FETCH
            0x00uy; 0x00uy; // MAXIMUM PRE-FETCH CEILING
            0x00uy;         // FSW,LBCSS,DRA,NV_DIS
            0x00uy;         // NUMBER OF CACHE SEGMENTS
            0x00uy; 0x00uy; // CACHE SEGMENT SIZE
            0x00uy;         // Reserved
            0x00uy;         // Obsolete
            0x00uy;         // Obsolete 
            0x00uy;         // Obsolete
        |]

    /// <summary>
    ///  Get current value of control mode page bytes array.
    /// </summary>
    member private _.GetControlModePage_Current() : byte[] =
        [|
            0x0Auy; // PS(false), SPF(0b), PAGE CODE(0x0A)
            0x0Auy;                                             // PAGE LENGTH(0x0A)
            ( m_TST <<< 5 ) |||                                 // TST
                ( Functions.SetBitflag m_TMF_ONLY 0x10uy ) |||  // TMF_ONLY
                ( Functions.SetBitflag m_D_SENSE 0x04uy ) |||   // D_SENSE
                ( Functions.SetBitflag m_GLTSD 0x02uy ) |||     // GLTSD
                ( Functions.SetBitflag m_RLEC 0x02uy );         // RLEC
            ( m_QueueAlgorithmModifier <<< 4 ) |||              // QUEUE ALGORITHM MODIFIER
                ( ( m_QERR &&& 0x03uy ) <<< 1 );                // QERR
            ( Functions.SetBitflag m_RAC 0x40uy ) |||           // RAC
                ( ( m_UA_INTLCK_CTRL &&& 0x03uy ) <<< 4 ) |||   // m_UA_INTLCK_CTRL
                ( Functions.SetBitflag m_SWP 0x08uy )           // SWP
            ( Functions.SetBitflag m_ATO 0x80uy ) |||           // ATO
                ( Functions.SetBitflag m_TAS 0x40uy )           // TAS
            0x00uy; // Obsolete
            0x00uy; // Obsolete
            byte ( m_BusyTimePeriod >>> 8 );                    // BUSY TIMEOUT PERIOD
            byte ( m_BusyTimePeriod &&& 0x00FFus );
            byte ( m_ExtendedSelfTestCompletionTime >>> 8 );    // EXTENDED SELF-TEST COMPLETION TIME
            byte ( m_ExtendedSelfTestCompletionTime &&& 0x00FFus );
        |]

    /// <summary>
    ///  Get default value of control mode page bytes array.
    /// </summary>
    member private _.GetControlModePage_Default() : byte[] =
        [|
            0x0Auy; // PS(false), SPF(0b), PAGE CODE(0x0A)
            0x0Auy;                                                     // PAGE LENGTH(0x0A)
            ( m_TST <<< 5 ) |||                                         // TST
                ( Functions.SetBitflag m_TMF_ONLY 0x10uy ) |||          // TMF_ONLY
                ( Functions.SetBitflag m_D_SENSE_Default 0x04uy ) |||   // D_SENSE(default value)
                ( Functions.SetBitflag m_GLTSD 0x02uy ) |||             // GLTSD
                ( Functions.SetBitflag m_RLEC 0x02uy );                 // RLEC
            ( m_QueueAlgorithmModifier <<< 4 ) |||                      // QUEUE ALGORITHM MODIFIER
                ( ( m_QERR &&& 0x03uy ) <<< 1 );                        // QERR
            ( Functions.SetBitflag m_RAC 0x40uy ) |||                   // RAC
                ( ( m_UA_INTLCK_CTRL &&& 0x03uy ) <<< 4 ) |||           // m_UA_INTLCK_CTRL
                ( Functions.SetBitflag m_SWP_Default 0x08uy )           // SWP(default value)
            ( Functions.SetBitflag m_ATO 0x80uy ) |||                   // ATO
                ( Functions.SetBitflag m_TAS 0x40uy )                   // TAS
            0x00uy; // Obsolete
            0x00uy; // Obsolete
            byte ( m_BusyTimePeriod >>> 8 );                    // BUSY TIMEOUT PERIOD
            byte ( m_BusyTimePeriod &&& 0x00FFus );
            byte ( m_ExtendedSelfTestCompletionTime >>> 8 );    // EXTENDED SELF-TEST COMPLETION TIME
            byte ( m_ExtendedSelfTestCompletionTime &&& 0x00FFus );
        |]

    /// <summary>
    ///  Get control mode page changeable values mask bytes array.
    /// </summary>
    member private _.GetControlModePage_Changeable() : byte[] =
        [|
            0x0Auy; // PS(false), SPF(0b), PAGE CODE(0x0A)
            0x0Auy; // PAGE LENGTH(0x0A)
            0x04uy; // TST, TMF_ONLY, D_SENSE, GLTSD, RLEC
            0x00uy; // QUEUE ALGORITHM MODIFIER, m_QERR
            0x08uy; // RAC, UA_INTLCK_CTRL, SWP
            0x00uy; // ATO, TAS
            0x00uy; // Obsolete
            0x00uy; // Obsolete
            0x00uy; // BUSY TIMEOUT PERIOD
            0x00uy; // BUSY TIMEOUT PERIOD
            0x00uy; // EXTENDED SELF-TEST COMPLETION TIME
            0x00uy; // EXTENDED SELF-TEST COMPLETION TIME
        |]

    /// <summary>
    /// Get current value of informational exceptions control mode page bytes array.
    /// </summary>
    member private _.GetInformationalExceptionsControlModePage_Current() : byte[] =
        [|
            0x1Cuy; // PS(false), SPF(0b), PAGE CODE(0x1C)
            0x0Auy; // PAGE LENGTH(0x0A)
            ( Functions.SetBitflag m_PERF 0x80uy ) |||          // PERF
                ( Functions.SetBitflag m_EBF 0x20uy ) |||       // EBF
                ( Functions.SetBitflag m_EWASC 0x10uy ) |||     // EWASC
                ( Functions.SetBitflag m_DEXCPT 0x08uy ) |||    // DEXCPT
                ( Functions.SetBitflag m_TEST 0x04uy ) |||      // TEST
                ( Functions.SetBitflag m_LOGERR 0x01uy );       // LOGERR
            ( 0x0Fuy &&& m_MRIE );                              // MRIE
            yield! Functions.UInt32ToNetworkBytes_NewVec m_IntervalTimer;   // INTERVAL TIMER
            yield! Functions.UInt32ToNetworkBytes_NewVec m_ReportCount;     // REPORT COUNT
        |]

    /// <summary>
    /// Get default value of informational exceptions control mode page bytes array.
    /// </summary>
    member private this.GetInformationalExceptionsControlModePage_Default() : byte[] =
        this.GetInformationalExceptionsControlModePage_Current()

    /// <summary>
    /// Get informational exceptions control mode page changeable values mask bytes array.
    /// </summary>
    member private _.GetInformationalExceptionsControlModePage_Changeable() : byte[] =
        [|
            0x1Cuy; // PS(false), SPF(0b), PAGE CODE(0x1C)
            0x0Auy; // PAGE LENGTH(0x0A)
            0x00uy; // PERF, EBF, EWASC, DEXCPT, TEST, LOGERR
            0x00uy; // MRIE
            0x00uy; 0x00uy; 0x00uy; 0x00uy;// INTERVAL TIMER
            0x00uy; 0x00uy; 0x00uy; 0x00uy;// REPORT COUNT
        |]

    /// <summary>
    ///  Read values from cache mode page received from initiator by parameter list.
    /// </summary>
    /// <param name="v">
    ///  Received bytes array by mode select command.
    /// </param>
    /// <param name="s">
    ///  Start position of control mode page in argument v.
    /// </param>
    /// <param name="len">
    ///  Length of parameter list.
    /// </param>
    /// <param name="source">
    ///  Command source information.
    /// </param>
    /// <param name="itt">
    ///  Initiator task tag of SCSI command.
    /// </param>
    /// <returns>
    ///  Next data position.
    /// </returns>
    member private this.ReadCacheModePageByteData ( v : PooledBuffer ) ( s : int ) ( len : int ) ( source : CommandSourceInfo ) ( itt:ITT_T ) : int =
        let loginfo = struct ( m_ObjID, ValueSome source, ValueSome itt, ValueSome m_LUN )

        if s + 20 > len then
            let errmsg = sprintf "Invalie parameter list length(%d), in MODE SELECT command CDB." len
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 4us },
                errmsg
            )

        // check PAGE LENGTH value
        if v.[ s + 1 ] <> 0x12uy then
            let errmsg = sprintf "Invalie parameter value in cache mode page. PAGE LENGTH is %d, but must be 18." v.[ s + 1 ]
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = uint16 ( s + 1 ) },
                errmsg
            )

        let cv = this.GetCacheModePage_Changeable()
        let dv = this.GetCacheModePage_Default()

        for i = 2 to 19 do
            if ( dv.[i] &&& ~~~ cv.[i] ) <> ( v.[ s + i ] &&& ~~~ cv.[i] ) then
                let errmsg = "Invalie parameter value in cache mode page. An attempt was made to change constant value."
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, errmsg )
                raise <| SCSIACAException (
                    source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                    { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = uint16 ( s + i ) },
                    errmsg
                )

        // There are no changeable field in informational exceptions control mode page.

        // return next byte position
        s + 20

    /// <summary>
    ///  Read values from control mode page received from initiator by parameter list.
    /// </summary>
    /// <param name="v">
    ///  Received bytes array by mode select command.
    /// </param>
    /// <param name="s">
    ///  Start position of control mode page in argument v.
    /// </param>
    /// <param name="len">
    ///  Length of parameter list.
    /// </param>
    /// <param name="source">
    ///  Command source information.
    /// </param>
    /// <param name="itt">
    ///  Initiator task tag of SCSI command.
    /// </param>
    /// <returns>
    ///  Next data position.
    /// </returns>
    member private this.ReadControlModePageByteData ( v : PooledBuffer ) ( s : int ) ( len : int ) ( source : CommandSourceInfo ) ( itt:ITT_T ) : int =
        let loginfo = struct ( m_ObjID, ValueSome source, ValueSome itt, ValueSome m_LUN )

        if s + 12 > len then
            let errmsg = sprintf "Invalie parameter list length(%d), in MODE SELECT command CDB." len
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 4us },
                errmsg
            )

        // check PAGE LENGTH value
        if v.[ s + 1 ] <> 0x0Auy then
            let errmsg = sprintf "Invalie parameter value in control mode page. PAGE LENGTH is %d, but must be 10." v.[ s + 1 ]
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = uint16 ( s + 1 ) },
                errmsg
            )

        let cv = this.GetControlModePage_Changeable()
        let dv = this.GetControlModePage_Default()

        for i = 2 to 11 do
            if ( dv.[i] &&& ~~~ cv.[i] ) <> ( v.[ s + i ] &&& ~~~ cv.[i] ) then
                let errmsg = "Invalie parameter value in control mode page. An attempt was made to change constant value."
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, errmsg )
                raise <| SCSIACAException (
                    source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                    { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = uint16 ( s + i ) },
                    errmsg
                )

        // get D_SENSE value
        let afterDSENSE = Functions.CheckBitflag v.[ s + 2 ] 0x04uy
        if m_D_SENSE <> afterDSENSE then
            HLogger.Trace( LogID.I_DSENSE_CHANGED, fun g ->
                g.Gen2( loginfo,
                    ( if m_D_SENSE then "Descriptor format" else "Fixed format" ),
                    ( if afterDSENSE then "descriptor format" else "Fixed format" )
                )
            )
            m_D_SENSE <- afterDSENSE

        // get SWP value
        let afterSWP = Functions.CheckBitflag v.[ s + 4 ] 0x08uy
        if m_SWP <> afterSWP then
            HLogger.Trace( LogID.I_SWP_CHANGED, fun g ->
                g.Gen2( loginfo,
                    ( if m_SWP then "Enabled" else "Disabled" ),
                    ( if afterSWP then "Enabled" else "Disabled" )
                )
            )
            m_SWP <- afterSWP

        // return next byte position
        s + 12

    /// <summary>
    ///  Read values from informational exceptions control mode page received from initiator by parameter list.
    /// </summary>
    /// <param name="v">
    ///  Received bytes array by mode select command.
    /// </param>
    /// <param name="s">
    ///  Start position of control mode page in argument v.
    /// </param>
    /// <param name="len">
    ///  Length of parameter list.
    /// </param>
    /// <param name="source">
    ///  Command source information.
    /// </param>
    /// <param name="itt">
    ///  Initiator task tag of SCSI command.
    /// </param>
    /// <returns>
    ///  Next data position.
    /// </returns>
    member private this.ReadInformationalExceptionsControlModePageByteData ( v : PooledBuffer ) ( s : int ) ( len : int ) ( source : CommandSourceInfo ) ( itt:ITT_T ) : int =
        let loginfo = struct ( m_ObjID, ValueSome source, ValueSome itt, ValueSome m_LUN )

        if s + 12 > len then
            let errmsg = sprintf "Invalie parameter list length(%d), in MODE SELECT command CDB." len
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.PARAMETER_LIST_LENGTH_ERROR,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 4us },
                errmsg
            )

        // check PAGE LENGTH value
        if v.[ s + 1 ] <> 0x0Auy then
            let errmsg = sprintf "Invalie parameter value in informational exceptions control mode page. PAGE LENGTH is %d, but must be 10." v.[ s + 1 ]
            HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, errmsg )
            raise <| SCSIACAException (
                source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = uint16 ( s + 1 ) },
                errmsg
            )

        let cv = this.GetInformationalExceptionsControlModePage_Changeable()
        let dv = this.GetInformationalExceptionsControlModePage_Default()

        for i = 2 to 11 do
            if ( dv.[i] &&& ~~~ cv.[i] ) <> ( v.[ s + i ] &&& ~~~ cv.[i] ) then
                let errmsg = "Invalie parameter value in informational exceptions control mode page. An attempt was made to change constant value."
                HLogger.ACAException( loginfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST, errmsg )
                raise <| SCSIACAException (
                    source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_PARAMETER_LIST,
                    { CommandData = false; BPV = true; BitPointer = 7uy; FieldPointer = uint16 ( s + i ) },
                    errmsg
                )

        // There are no changeable field in informational exceptions control mode page.

        // return next byte position
        s + 12
