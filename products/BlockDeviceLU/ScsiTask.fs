//=============================================================================
// Haruka Software Storage.
// ScsiTask.fs : Defines ScsiTask structure.
// ScsiTask structure holds tha data needed SAM-2 SCSI task.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open System.Text
open System.Threading
open System.Threading.Tasks
open System.Diagnostics
open System.Buffers
open System.Collections.Generic

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

/// <summary>
///  ScsiTask class represents one SCSI task in task manager. 
/// </summary>
/// <param name="m_StatusMaster">
///  Refelence of the status master object.
/// </param>
/// <param name="m_Source">
///  Source of this SCSI task.
/// </param>
/// <param name="m_Command">
///  Received SCSI task command.
/// </param>
/// <param name="m_CDB">
///  Converted SCSI CBD.
/// </param>
/// <param name="m_DataOut">
///  Received Data-Out PDUs.
/// </param>
/// <param name="m_LU">
///  Interface of Logical Unit object where this task is created.
/// </param>
/// <param name="m_Media">
///  Interface of Media object that is accessed by this task.
/// </param>
/// <param name="m_ModeParameter">
///  Mode parameter object that is belongings to the LU.
/// </param>
/// <param name="m_PRManager">
///  Persistent reservation object.
/// </param>
/// <param name="m_ACANoncompliant">
///  If this parameter is true, this task can also be run when ACA is established.
/// </param>
[<NoComparison>]
type ScsiTask
    (
        m_StatusMaster : IStatus,
        m_Source : CommandSourceInfo,
        m_Command : SCSICommandPDU,
        m_CDB : ICDB,
        m_DataOut : SCSIDataOutPDU list,
        m_LU : IInternalLU,
        m_Media : IMedia,
        m_ModeParameter : ModeParameter,
        m_PRManager : PRManager,
        m_ACANoncompliant : bool
    ) =

    /// Hash value identify this instance
    let m_ObjID = objidx_me.NewID()

    /// Initiator task tag
    let m_ITT = m_Command.InitiatorTaskTag

    /// LUN
    let m_LUN = m_Command.LUN

    /// Log Information
    let m_LogInfo = struct ( m_ObjID, ValueSome m_Source, ValueSome m_ITT, ValueSome m_LUN )

    /// Terminate request flag.
    /// If this flag is 2, this task must abort quickly.
    /// ( 0:response is not returned yet, 1:task is complete, 2:task is aborted)
    let mutable m_TerminateFlag = 0

    /// stop watch for read/write time
    let m_RWStopWatch =
        let s = new Stopwatch()
        s.Start()
        s

    do
        if HLogger.IsVerbose then
            HLogger.Trace( LogID.V_TRACE, fun g ->
                let msg = sprintf "ScsiTask instance was created. Operation code=%s" ( CDBTypes.getName m_CDB.Type )
                g.Gen1( m_LogInfo, msg )
            )

    //=========================================================================
    // Interface method

    interface IBlockDeviceTask with

        /// Return task type.
        override _.TaskType : BlockDeviceTaskType =
            BlockDeviceTaskType.ScsiTask

        /// Return source information of this task.
        override _.Source : CommandSourceInfo =
            m_Source
    
        /// Return  Initiator task tag.
        override _.InitiatorTaskTag : ITT_T =
            m_ITT

        /// Return SCSI Command object of this object.
        override _.SCSICommand : SCSICommandPDU =
            m_Command

        /// Return CDB of this object
        override _.CDB : ICDB voption =
            ValueSome m_CDB

        /// Execute this SCSI task.
        override this.Execute() : unit -> Task<unit> =

            // ****************************************************************
            // This method is called in critical section of BlockDeviceLU task set lock.
            // And returned task workflow is executed in asyncnously.
            // ****************************************************************

            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_TRACE, fun g ->
                    let msg = sprintf "Scsi task has been started. Operation code=%s" ( CDBTypes.getName m_CDB.Type )
                    g.Gen1( m_LogInfo, msg )
                )

            match m_CDB.Type with
            | ChangeAliases                             // SPC-3 6.2 CHANGE ALIASES command
            | ExtendedCopy                              // SPC-3 6.3 EXTENDED COPY command
            | LogSelect                                 // SPC-3 6.5 LOG SELECT command
            | LogSense                                  // SPC-3 6.6 LOG SENSE command
            | PreventAllowMediumRemoval                 // SPC-3 6.13 PREVENT ALLOW MEDIUM REMOVAL command
            | ReadAttribute                             // SPC-3 6.14 READ ATTRIBUTE command
            | ReadBuffer                                // SPC-3 6.15 READ BUFFER command
            | ReadMediaSerialNumber                     // SPC-3 6.16 READ MEDIA SERIAL NUMBER command
            | ReceiveCopyResults                        // SPC-3 6.17 RECEIVE COPY RESULTS command
            | ReceiveDiagnosticResults                  // SPC-3 6.18 RECEIVE DIAGNOSTIC RESULTS command
            | ReportAliases                             // SPC-3 6.19 REPORT ALIASES command
            | ReportDeviceIdentifier                    // SPC-3 6.20 REPORT DEVICE IDENTIFIER command
            | ReportPriority                            // SPC-3 6.22 REPORT PRIORITY command
            | ReportTargetPortGroups                    // SPC-3 6.25 REPORT TARGET PORT GROUPS command
            | ReportTimestamp                           // SPC-3 6.26 REPORT TIMESTAMP command
            | SendDiagnostic                            // SPC-3 6.28 SEND DIAGNOSTIC command
            | SetDeviceIdentifier                       // SPC-3 6.29 SET DEVICE IDENTIFIER command
            | SetPriority                               // SPC-3 6.30 SET PRIORITY command
            | SetTargetPortGroups                       // SPC-3 6.31 SET TARGET PORT GROUPS command
            | SetTimestamp                              // SPC-3 6.32 SET TIMESTAMP command
            | WriteAttribute                            // SPC-3 6.34 WRITE ATTRIBUTE command
            | WriteBuffer                               // SPC-3 6.35 WRITE BUFFER command
            | AccessControlIn                           // SPC-3 8.3.2 ACCESS CONTROL IN command
            | AccessControlOut                          // SPC-3 8.3.3 ACCESS CONTROL OUT command
            | ReadDefectData                            // SBC-2 5.12 READ DEFECT DATA(10), 5.13 READ DEFECT DATA(12) command
            | ReadLong                                  // SBC-2 5.14 READ LONG(10), 5.15 READ LONG(16) command
            | ReassignBlocks                            // SBC-2 5.16 REASSIGN BLOCKS command
            | StartStopUnit                             // SBC-2 5.17 START STOP UNIT command
            | Verify                                    // SBC-2 5.20 VERIFY(10), 5.21 VERIFY(12), 5.22 VERIFY(16), 5.23 VERIFY(32) command
            | WriteAndVerify                            // SBC-2 5.29 WRITE AND VERIFY(10), 5.30 WRITE AND VERIFY(12), 5.31 WRITE AND VERIFY(16), 5.32 WRITE AND VERIFY(32) command
            | WriteLong                                 // SBC-2 5.33 WRITE LONG(10), 5.34 WRITE LONG(16) command
            | WriteSame                                 // SBC-2 5.35 WRITE SAME(10), 5.36 WRITE SAME(16), 5.37 WRITE SAME(32) command
            | XDRead                                    // SBC-2 5.38 XDREAD(10), 5.39 XDREAD(32) command
            | XDWrite                                   // SBC-2 5.40 XDWRITE(10), 5.41 XDWRITE(32) command
            | XDWriteRead                               // SBC-2 5.42 XDWRITEREAD(10), 5.43 XDWRITEREAD(32) command
            | XPWrite                                   // SBC-2 5.44 XPWRITE(10), 5.45 XPWRITE(32) command
                ->
                    fun () -> task {
                        let errmsg =
                            sprintf
                                "Specified operation code (%s) is not supported."
                                ( CDBTypes.getName m_CDB.Type )
                        HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_COMMAND_OPERATION_CODE, errmsg )
                        let ex =
                            new SCSIACAException (
                                m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_COMMAND_OPERATION_CODE,
                                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 0us },
                                errmsg
                            )
                        m_LU.NotifyTerminateTaskWithException this ex
                    }

            | Inquiry                                   // SPC-3 6.4 INQUIRY command
                ->
                    fun () -> task {
                        try
                            this.Execute_Inquiry()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | ModeSelect                                // SPC-3 6.7 MODE SELECT(6), 6.8 MODE SELECT(10) command
                ->
                    fun () -> task {
                        try
                            this.Execute_ModeSelect()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | ModeSense                                 // SPC-3 6.9 MODE SENSE(6), 6.10 MODE SENSE(10) command
                ->
                    fun () -> task {
                        try
                            this.Execute_ModeSense()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | PersistentReserveIn                       // SPC-3 6.11 PERSISTENT RESERVE IN command
                ->
                    fun () -> task {
                        try
                            this.Execute_PersistentReserveIn()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | PersistentReserveOut                      // SPC-3 6.12 PERSISTENT RESERVE OUT command
                -> 
                    // Note that the method is being called directly here.
                    this.Execute_PersistentReserveOut()

            | PreFetch                                  // SBC-2 5.3 PRE-FETCH(10), 5.4 PRE-FETCH(16) command
                ->
                    fun () -> task {
                        try
                            this.Execute_PreFetch()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | ReportLUNs                                // SPC-3 6.21 REPORT LUNS command
                ->
                    fun () -> task {
                        try
                            this.Execute_ReportLUNs()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | ReportSupportedOperationCodes             // SPC-3 6.23 REPORT SUPPORTED OPERATION CODES command
                ->
                    fun () -> task {
                        try
                            this.Execute_ReportSupportedOperationCodes()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | ReportSupportedTaskManagementFunctions    // SPC-3 6.24 REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS command
                ->
                    fun () -> task {
                        try
                            this.Execute_ReportSupportedTaskManagementFunctions()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | RequestSense                              // SPC-3 6.27 REQUEST SENSE command
                ->
                    fun () -> task {
                        try
                            this.Execute_RequestSense()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | TestUnitReady                             // SPC-3 6.33 TEST UNIT READY command
                ->
                    fun () -> task {
                        try
                            this.Execute_TestUnitReady()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | FormatUnit                                // SBC-2 5.2 FORMAT UNIT command
                ->
                    fun () -> task {
                        try
                            do! this.Execute_FormatUnit()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | Read                                      // SBC-2 5.5 READ(6), 5.6 READ(10), 5.7 READ(12), 5.8 READ(16), 5.9 READ(32) command
                ->
                    fun () -> task {
                        try
                            do! this.Execute_Read()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | ReadCapacity                              // SBC-2 5.10 READ CAPACITY(10), 5.11 READ CAPACITY(16) command
                ->
                    fun () -> task {
                        try
                            this.Execute_ReadCapacity()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | SynchronizeCache                          // SBC-2 5.18 SYNCHRONIZE CACHE(10), 5.19 SYNCHRONIZE CACHE(16) command
                ->
                    fun () -> task {
                        try
                            this.Execute_SynchronizeCache()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

            | Write                                     // SBC-2 5.24 WRITE(6), 5.25 WRITE(10), 5.26 WRITE(12), 5.27 WRITE(16), 5.28 WRITE(32) command
                ->
                    fun () -> task {
                        try
                            do! this.Execute_Write()
                            m_LU.NotifyTerminateTask this
                        with
                        | _ as x ->
                            m_LU.NotifyTerminateTaskWithException this x
                    }

        /// Get task description string.
        override _.DescString : string =
            "SCSI task. Command=" + m_CDB.DescriptString

        /// <summary>
        ///   Notify task terminate request
        /// </summary>
        /// <param name="needResp">
        ///   If task is terminated from the other I_T Nexus, set true to this value.
        /// </param>
        override this.NotifyTerminate( needResp : bool ) : unit =

            let init, current = this.SetTerminateFlag 2
            if init = 0 && current = 2 then
                // If this task is aborted by the other I_T Nexus, returns TASK ABORTED response.
                if needResp then
                    m_Source.ProtocolService.SendSCSIResponse
                        m_Command
                        m_Source.CID
                        m_Source.ConCounter
                        0u
                        iScsiSvcRespCd.COMMAND_COMPLETE
                        ScsiCmdStatCd.TASK_ABORTED
                        PooledBuffer.Empty
                        PooledBuffer.Empty
                        0u
                        ResponseFenceNeedsFlag.R_Mode

        /// Return ACANoncompliant flag value
        override _.ACANoncompliant : bool =
            m_ACANoncompliant

        /// Release PooledBuffer
        override _.ReleasePooledBuffer() =
            m_DataOut
            |> List.map _.DataSegment
            |> List.insertAt 0 m_Command.DataSegment
            |> List.iter ( fun itr ->
                itr.Return()
            )

    //=========================================================================
    // Public method

    /// Get media object of this SCSI task
    member _.Media : IMedia =
        m_Media

    /// Get reference of LU object
    member _.LU : IInternalLU =
        m_LU

    member _.SetTerminateFlag ( flg : int ) : int * int =
        Interlocked.CompareExchange( &m_TerminateFlag, flg, 0 ), m_TerminateFlag

    //=========================================================================
    // Private method

    /// <summary>
    ///  Execute INQUIRY SCSI command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be InquiryCDB.
    /// </remarks>
    member private this.Execute_Inquiry() : unit =
        assert( m_CDB.Type = Inquiry )
        assert( match m_CDB with | :? InquiryCDB -> true | _ -> false )
        let cdb = m_CDB :?> InquiryCDB

        let inData =
            if cdb.EVPD then
                match cdb.PageCode with
                | 0x80uy ->
                    //  Unit Serial Number VPD page
                    [|
                        yield 0x00uy;   // PERIPHERAL QUALIFIER(0) PERIPHERAL DEVICE TYPE(0)
                        yield 0x80uy;   // PAGE CODE(0x80)
                        yield 0x00uy;   // Reserved
                        yield 0x04uy;   // PAGE LENGTH(4)
                        yield 0x20uy;   // PRODUCT SERIAL NUMBER
                        yield 0x20uy;   // PRODUCT SERIAL NUMBER
                        yield 0x20uy;   // PRODUCT SERIAL NUMBER
                        yield 0x20uy;   // PRODUCT SERIAL NUMBER
                    |]
                | 0x83uy ->
                    // Device Identification
                    [|
                        yield 0x00uy;   // PERIPHERAL QUALIFIER(0) PERIPHERAL DEVICE TYPE(0)
                        yield 0x83uy;   // PAGE CODE(0x83)

                        let sessParam = m_Source.ProtocolService.SessionParameter

                        // DISCRIPTOR 1 ( logical unit )
                        let dec1 = [|
                            yield 0x03uy;   // PROTOCOL IDENTIFIER(0h)  CODE SET(3h)
                            yield 0x08uy;   // PIV(0) ASSOCIATION(00b) IDENTIFIER TYPE(8h)
                            yield 0x00uy;   // Reserved
                            let luNameBytesData = 
                                sprintf "%s,L,0x%016X" ( sessParam.TargetConf.TargetName ) ( lun_me.toPrim m_LUN )
                                |> Encoding.UTF8.GetBytes
                                |> Functions.PadBytesArray 4 256
                            yield byte( luNameBytesData.Length );   // IDENTIFIER LENGTH
                            yield! luNameBytesData;   // IDENTIFIER
                        |]

                        // DISCRIPTOR 2 ( target port )
                        let dec2 = [|
                            yield 0x53uy;   // PROTOCOL IDENTIFIER(5h)  CODE SET(3h)
                            yield 0x98uy;   // PIV(1) ASSOCIATION(01b) IDENTIFIER TYPE(8h)
                            yield 0x00uy;   // Reserved
                            let targetPortNameBytesData = 
                                sprintf "%s,t,0x%04X" sessParam.TargetConf.TargetName sessParam.TargetPortalGroupTag
                                |> Encoding.UTF8.GetBytes
                                |> Functions.PadBytesArray 4 256
                            yield byte( targetPortNameBytesData.Length );   // IDENTIFIER LENGTH
                            yield! targetPortNameBytesData;   // IDENTIFIER
                        |]

                        // DISCRIPTOR 2 ( SCSI target device )
                        let dec3 = [|
                            yield 0x53uy;   // PROTOCOL IDENTIFIER(5h)  CODE SET(3h)
                            yield 0xA8uy;   // PIV(1) ASSOCIATION(10b) IDENTIFIER TYPE(8h)
                            yield 0x00uy;   // Reserved
                            let targetNameBytesData = 
                                sessParam.TargetConf.TargetName
                                |> Encoding.UTF8.GetBytes
                                |> Functions.PadBytesArray 4 256
                            yield byte( targetNameBytesData.Length );   // IDENTIFIER LENGTH
                            yield! targetNameBytesData;   // IDENTIFIER
                        |]

                        // PAGE LENGTH
                        yield! int16( dec1.Length + dec2.Length + dec3.Length )
                                |> Functions.Int16ToNetworkBytes_NewVec

                        yield! dec1;    // DISCRIPTOR 1
                        yield! dec2;    // DISCRIPTOR 2
                        yield! dec3;    // DISCRIPTOR 3
                    |]
                | 0x86uy ->
                    // Extended INQUIRY Data
                    [|
                        yield 0x00uy;   // PERIPHERAL QUALIFIER(0) PERIPHERAL DEVICE TYPE(0)
                        yield 0x86uy;   // PAGE CODE(0x86)
                        yield 0x00uy;   // Reserved
                        yield 0x3Cuy;   // PAGE LENGTH
                        yield 0x00uy;   // RTO(0) GRD_CHK(0) APP_CHK REF_CHK(0)
                        yield 0x07uy;   // GROUP_SUP(0) PRIOR_SUP(0) HEADSUP(1) ORDSUP(1) SIMPSUP(1)
                        yield 0x00uy;   // NV_SUP(0) V_SUP(0)
                        for _ = 7 to 63 do yield 0uy;
                    |]
                | 0xB0uy ->
                    //  Block Limits VPD page
                    [|
                        yield 0x00uy;   // PERIPHERAL QUALIFIER(0) PERIPHERAL DEVICE TYPE(0)
                        yield 0xB0uy;   // PAGE CODE(0xB0)
                        yield 0x00uy;   // Reserved
                        yield 0x0Cuy;   // PAGE LENGTH
                        yield 0x00uy;   // Reserved
                        yield 0x00uy;   // Reserved
                        yield 0x00uy;   // OPTIMAL TRANSFER LENGTH GRAMULARITY
                        yield 0x01uy;
                        yield 0x00uy;   // MAXIMUM TRANSFER LENGTH
                        yield 0x00uy;
                        yield 0x00uy;
                        yield 0x00uy;
                        yield 0x00uy;   // OPTIMAL TRANSFER LENGTH
                        yield 0x00uy;
                        yield 0x00uy;
                        yield 0x08uy;
                    |]
                | 0xB1uy ->
                    //  Block Device Characteristics VPD page
                    [|
                        yield 0x0Cuy;   // PERIPHERAL QUALIFIER(0b) PERIPHERAL DEVICE TYPE(0Ch)
                        yield 0xB1uy;   // PAGE CODE(0xB1)
                        yield 0x00uy;   // PAGE LENGTH(0x003C)
                        yield 0x3Cuy;   // PAGE LENGTH
                        yield 0x00uy;   // MEDIUM ROTATION RATE(0x0000)
                        yield 0x00uy;   // MEDIUM ROTATION RATE
                        yield 0x00uy;   // PRODUCT TYPE(0x00)
                        yield 0x00uy;   // WABEREQ(0)/WACEREQ(0)/NOMINAL FORM FACTOR(0)
                        yield 0x00uy;   // FUAB(0)/VBULS(0)
                        for _ = 9 to 63 do yield 0uy;
                    |]
                | 0x00uy ->
                    // Supported VPD Pages
                    [|
                        yield 0x00uy;   // PERIPHERAL QUALIFIER(0) PERIPHERAL DEVICE TYPE(0)
                        yield 0x00uy;   // PAGE CODE(0x00)
                        yield 0x00uy;   // Reserved
                        yield 0x06uy;   // PAGE LENGTH
                        yield! [| 0x00uy; 0x80uy; 0x83uy; 0x86uy; 0xB0uy; 0xB1uy |] // supported VPD pages list
                    |]
                | _ ->
                    // Unsupported page code
                    let errmsg = sprintf "In INQUIRY CDB, unsupported PAGE CODE(0x%02X) is specified." cdb.PageCode
                    HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                    raise <| SCSIACAException (
                        m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                        { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 2us },
                        errmsg
                    )

            else
                // Return standerd inquiry date
                if cdb.PageCode <> 0uy then
                    let errmsg = sprintf "In INQUIRY CDB, EVPD bit is 0 and PageCode field is not 0(PageCode=0x%02X)." cdb.PageCode
                    HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                    raise <| SCSIACAException (
                        m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                        { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 2us },
                        errmsg
                    )

                [|
                    yield 0x00uy;   // PERIPHERAL QUALIFIER(0) PERIPHERAL DEVICE TYPE(0)
                    yield 0x00uy;   // RMB(0)
                    yield 0x05uy;   // VERSION(05h,SPC-3)
                    yield 0x22uy;   // NORMACA(1) HISUP(0) RESPONSE DATA FORMAT(2)
                    yield 0x5Buy;     // ADDITIONAL LENGTH( 95 bytes length - 4 )
                    yield 0x00uy;   // SCCS(0) ACC(0) TPGS(00b) 3PC(0) PROTECT(0)
                    yield 0x10uy;   // BQUE(0) ENCSERV(0) VS(0) MULTIP(1) MCHNGR(0) ADDR16(0)
                    yield 0x02uy;   // WBUS16(0) SYNC(0) LINKED(0) CMDQUE(1) VS(0)

                    // T10 VENDOR IDENTIFICATION
                    yield! [| 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy |]

                    // PRODUCT IDENTIFICATION
                    yield! [| byte 'H'; byte 'A'; byte 'R'; byte 'U'; byte 'K'; byte 'A'; byte ' '; byte 'S'; |]
                    yield! [| byte '.'; byte 'S'; byte '.'; 0uy; 0uy; 0uy; 0uy; 0uy; |]

                    // PRODUCT REVISION LEVEL
                    yield! [| byte '1'; byte '0'; byte '0'; 0uy; |]

                    // Vendor Specific
                    for _ = 36 to 55 do yield 0uy;

                    yield 0uy;      // CLOCKING(0) QAS(0) IUS(0)
                    yield 0uy;      // Reserved
                    yield! [| 0x00uy; 0x40uy; |] // VERSION DESCRIPTOR 1(SAM-2)
                    yield! [| 0x09uy; 0x60uy; |] // VERSION DESCRIPTOR 2(iSCSI)
                    yield! [| 0x09uy; 0x60uy; |] // VERSION DESCRIPTOR 3(iSCSI)
                    yield! [| 0x03uy; 0x00uy; |] // VERSION DESCRIPTOR 4(SPC-3)
                    yield! [| 0x03uy; 0x20uy; |] // VERSION DESCRIPTOR 5(SBC-2)
                    yield! [| 0x00uy; 0x00uy; |] // VERSION DESCRIPTOR 6
                    yield! [| 0x00uy; 0x00uy; |] // VERSION DESCRIPTOR 7
                    yield! [| 0x00uy; 0x00uy; |] // VERSION DESCRIPTOR 8

                    // Reserved
                    for _ = 74 to 95 do yield 0uy;
                |]

        let init, current = this.SetTerminateFlag 1
        if init = 0 && current = 1 then
            // Send response data to the initiator
            m_Source.ProtocolService.SendSCSIResponse
                m_Command
                m_Source.CID
                m_Source.ConCounter
                0u
                iScsiSvcRespCd.COMMAND_COMPLETE
                ScsiCmdStatCd.GOOD
                PooledBuffer.Empty
                ( PooledBuffer.Rent inData )
                ( uint32 cdb.AllocationLength )
                ResponseFenceNeedsFlag.R_Mode

    /// <summary>
    ///  Execute MODE SELECT SCSI command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be ModeSelectCDB.
    /// </remarks>
    member private this.Execute_ModeSelect() : unit =
        assert( m_CDB.Type = ModeSelect )
        assert( match m_CDB with | :? ModeSelectCDB -> true | _ -> false )
        let cdb = m_CDB :?> ModeSelectCDB
        let parameterList = SCSIDataOutPDU.AppendParamList m_Command.DataSegment m_DataOut ( int cdb.ParameterListLength )

        match cdb.OperationCode with
        | 0x15uy -> // MODE SELECT(6)
            m_ModeParameter.Select6 parameterList ( int cdb.ParameterListLength ) cdb.PF cdb.SP m_Source m_ITT
        | 0x55uy -> // MODE SELECT(10)
            m_ModeParameter.Select10 parameterList ( int cdb.ParameterListLength ) cdb.PF cdb.SP m_Source m_ITT
        | _ ->
            HLogger.Trace( LogID.F_ERROR_EXIT, fun g -> g.Gen1( m_LogInfo, sprintf "Invalid OPERATION CODE(0x%02X)" cdb.OperationCode ) )
            exit( 1 )

        parameterList.Return()

        let recvDataLen =
            ( PooledBuffer.length m_Command.DataSegment ) +
            ( m_DataOut |> Seq.map _.DataSegment |> Seq.map PooledBuffer.length |> Seq.sum )

        let init, current = this.SetTerminateFlag 1
        if init = 0 && current = 1 then
            // Send response data to the initiator
            m_Source.ProtocolService.SendSCSIResponse
                m_Command
                m_Source.CID
                m_Source.ConCounter
                ( uint32 recvDataLen )
                iScsiSvcRespCd.COMMAND_COMPLETE
                ScsiCmdStatCd.GOOD
                PooledBuffer.Empty
                PooledBuffer.Empty
                0u
                ResponseFenceNeedsFlag.R_Mode


    /// <summary>
    ///  Execute MODE SENSE SCSI command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be ModeSenseCDB.
    /// </remarks>
    member private this.Execute_ModeSense() : unit =
        assert( m_CDB.Type = ModeSense )
        assert( match m_CDB with | :? ModeSenseCDB -> true | _ -> false )
        let cdb = m_CDB :?> ModeSenseCDB

        let result =
            match cdb.OperationCode with
            | 0x1Auy -> // MODE SENSE(6)
                m_ModeParameter.Sense6 cdb.DBD cdb.PageCode cdb.SubPageCode cdb.PC m_Source m_ITT
            | 0x5Auy -> // MODE SENSE(10)
                m_ModeParameter.Sense10 cdb.LLBAA cdb.DBD cdb.PageCode cdb.SubPageCode cdb.PC m_Source m_ITT
            | _ ->
                HLogger.Trace( LogID.F_ERROR_EXIT, fun g -> g.Gen1( m_LogInfo, sprintf "Invalid OPERATION CODE(0x%02X)" cdb.OperationCode ) )
                exit( 1 )

        let init, current = this.SetTerminateFlag 1
        if init = 0 && current = 1 then
            // Send response data to the initiator
            m_Source.ProtocolService.SendSCSIResponse
                m_Command
                m_Source.CID
                m_Source.ConCounter
                0u
                iScsiSvcRespCd.COMMAND_COMPLETE
                ScsiCmdStatCd.GOOD
                PooledBuffer.Empty
                ( PooledBuffer.Rent result )
                ( uint32 cdb.AllocationLength )
                ResponseFenceNeedsFlag.R_Mode

    /// <summary>
    ///  Execute PERSISTENT RESERVE IN SCSI command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be PersistentReserveInCDB.
    /// </remarks>
    member private this.Execute_PersistentReserveIn() : unit =
        assert( m_CDB.Type = PersistentReserveIn )
        assert( match m_CDB with | :? PersistentReserveInCDB -> true | _ -> false )
        let cdb = m_CDB :?> PersistentReserveInCDB

        let paramdata =
            match cdb.ServiceAction with
            | 0x00uy -> // READ KEYS
                m_PRManager.ReadKey m_Source m_ITT
            | 0x01uy -> // READ RESERVATION
                m_PRManager.ReadReservation m_Source m_ITT
            | 0x02uy -> // REPORT CAPABILITIES
                m_PRManager.ReportCapabilities m_Source m_ITT
            | 0x03uy -> // READ FULL STATUS
                m_PRManager.ReadFullStatus m_Source m_ITT
            | _ ->
                let errmsg = sprintf "Invalie SERVICE ACTION value(%d), in PERSISTENT RESERVE IN command CDB." cdb.ServiceAction
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 4uy; FieldPointer = 1us },
                    errmsg
                )

        let init, current = this.SetTerminateFlag 1
        if init = 0 && current = 1 then
            // Send response data to the initiator
            m_Source.ProtocolService.SendSCSIResponse
                m_Command
                m_Source.CID
                m_Source.ConCounter
                0u
                iScsiSvcRespCd.COMMAND_COMPLETE
                ScsiCmdStatCd.GOOD
                PooledBuffer.Empty
                ( PooledBuffer.Rent paramdata )
                ( uint32 cdb.AllocationLength )
                ResponseFenceNeedsFlag.R_Mode
        
    /// <summary>
    ///  Execute PERSISTENT RESERVE OUT SCSI command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be PersistentReserveOutCDB.
    /// </remarks>
    member private this.Execute_PersistentReserveOut() : unit -> Task<unit> =
        assert( m_CDB.Type = PersistentReserveOut )
        assert( match m_CDB with | :? PersistentReserveOutCDB -> true | _ -> false )
        let cdb = m_CDB :?> PersistentReserveOutCDB
        let parameterList = SCSIDataOutPDU.AppendParamList m_Command.DataSegment m_DataOut ( int cdb.ParameterListLength )

        // ****************************************************************
        // This method is called in critical section of BlockDeviceLU task set lock.
        // And returned task workflow is executed in asyncnously.
        // ****************************************************************

        // return result function.
        // following procedure will be run in asynchronously.
        let retresultfunc ( resultval : ScsiCmdStatCd ) _ =
            task {
                try
                    let recvDataLen =
                        ( PooledBuffer.length m_Command.DataSegment ) +
                        ( m_DataOut |> Seq.map _.DataSegment |> Seq.map PooledBuffer.length |> Seq.sum )

                    let init, current = this.SetTerminateFlag 1
                    if init = 0 && current = 1 then
                        // Send response data to the initiator
                        m_Source.ProtocolService.SendSCSIResponse
                            m_Command
                            m_Source.CID
                            m_Source.ConCounter
                            ( uint32 recvDataLen )
                            iScsiSvcRespCd.COMMAND_COMPLETE
                            resultval
                            PooledBuffer.Empty
                            PooledBuffer.Empty
                            0u
                            ResponseFenceNeedsFlag.W_Mode
                    else
                        // Response is already returned.
                        ()

                    m_LU.NotifyTerminateTask this
                with
                | _ as x ->
                    m_LU.NotifyTerminateTaskWithException this x
            }

        try
            let statcd =
                match cdb.ServiceAction with
                | 0x00uy -> // REGISTER
                    m_PRManager.Register m_Source m_ITT cdb.PRType cdb.ParameterListLength parameterList
                | 0x01uy -> // RESERVE
                    m_PRManager.Reserve m_Source m_ITT cdb.PRType cdb.ParameterListLength parameterList
                | 0x02uy -> // RELEASE
                    m_PRManager.Release m_Source m_ITT cdb.PRType cdb.ParameterListLength parameterList
                | 0x03uy -> // CLEAR
                    m_PRManager.Clear m_Source m_ITT cdb.PRType cdb.ParameterListLength parameterList
                | 0x04uy -> // PREEMPT
                    m_PRManager.Preempt m_Source m_ITT cdb.PRType cdb.ParameterListLength parameterList
                | 0x05uy -> // PREEMPT AND ABORT
                    let struct( statCD, itn, prType, resvKey ) = m_PRManager.PreemptAndAbort m_Source m_ITT cdb.PRType cdb.ParameterListLength parameterList
                    let abortAllACATasks = PR_TYPE.isAllRegistrants prType && resvKey = resvkey_me.zero
                    m_LU.AbortTasksFromSpecifiedITNexus this itn abortAllACATasks
                    statCD
                | 0x06uy -> // REGISTER AND IGNORE EXISTING KEY
                    m_PRManager.RegisterAndIgnoreExistingKey m_Source m_ITT cdb.PRType cdb.ParameterListLength parameterList
                | 0x07uy -> // REGISTER AND MOVE
                    m_PRManager.RegisterAndMove m_Source m_ITT cdb.PRType cdb.ParameterListLength parameterList
                | _ ->
                    let errmsg = sprintf "Invalie SERVICE ACTION value(%d), in PERSISTENT RESERVE OUT command CDB." cdb.ServiceAction
                    HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                    raise <| SCSIACAException (
                        m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                        { CommandData = true; BPV = true; BitPointer = 4uy; FieldPointer = 1us },
                        errmsg
                    )
            parameterList.Return()
            retresultfunc statcd
        with
        | _ as x ->
            fun () -> task {
                m_LU.NotifyTerminateTaskWithException this x
            }

    /// <summary>
    ///  Execute PRE-FETCH command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be WriteCDB.
    /// </remarks>
    member private this.Execute_PreFetch() : unit =
        assert( m_CDB.Type = PreFetch )
        assert( match m_CDB with | :? PreFetchCDB -> true | _ -> false )
        let cdb = m_CDB :?> PreFetchCDB
        let wMediaBlockCount = m_Media.BlockCount

        if cdb.LogicalBlockAddress > wMediaBlockCount ||
            cdb.LogicalBlockAddress + ( uint64 cdb.PrefetchLength ) > wMediaBlockCount || 
            cdb.LogicalBlockAddress + ( uint64 cdb.PrefetchLength ) < cdb.LogicalBlockAddress then
            let errmsg = sprintf "Invalid LBA(0x%16X) in PRE-FETCH command CDB" cdb.LogicalBlockAddress
            HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )
            raise <| SCSIACAException ( m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )

        let s =
            if m_Media.BlockCount < cdb.LogicalBlockAddress + ( uint64 cdb.PrefetchLength ) then
                ScsiCmdStatCd.GOOD
            else
                ScsiCmdStatCd.CONDITION_MET

        // Nothig to do

        let init, current = this.SetTerminateFlag 1
        if init = 0 && current = 1 then
            // Send response data to the initiator
            m_Source.ProtocolService.SendSCSIResponse
                m_Command
                m_Source.CID
                m_Source.ConCounter
                0u
                iScsiSvcRespCd.COMMAND_COMPLETE
                s
                PooledBuffer.Empty
                PooledBuffer.Empty
                0u
                ResponseFenceNeedsFlag.R_Mode


    /// <summary>
    ///  Execute REPORT LUNS SCSI command.
    /// </summary>
    /// <remarks>
    ///  m_CDB must be ReportLUNsCDB.
    /// </remarks>
    member private this.Execute_ReportLUNs() : unit =
        assert( m_CDB.Type = ReportLUNs )
        assert( match m_CDB with | :? ReportLUNsCDB -> true | _ -> false )
        let cdb = m_CDB :?> ReportLUNsCDB

        // Check allocation length value
        if cdb.AllocationLength < 16u then
            let errmsg = sprintf "Invalie allocation length value(%d), in REPORT LUNS command CDB, must be over than or equals 16 bytes." cdb.AllocationLength
            HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 6us },
                errmsg
            )

        let luns =
            // Check specified LU is well known LU or not.
            match cdb.SelectReport with
            | 0x00uy ->
                m_Source.ProtocolService.GetLUNs()
            | 0x01uy ->
                Array.empty
            | 0x02uy ->
                m_Source.ProtocolService.GetLUNs()
            | _ ->
                let errmsg = sprintf "Invalie  SELECT REPORT field value(%d), in REPORT LUNS command CDB" cdb.SelectReport
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 2us },
                    errmsg
                )

        if HLogger.IsVerbose then
            luns
            |> Array.iter ( fun itrlun -> 
                HLogger.Trace( LogID.V_TRACE, fun g ->
                    let msg = sprintf "Reported LUN=%s" ( lun_me.toString itrlun )
                    g.Gen1( m_ObjID, msg )
                )
            )

        let bufLen = 4 + 4 + ( 8 * luns.Length )
        let paramdata = PooledBuffer.Rent bufLen
        Functions.Int32ToNetworkBytes paramdata.Array 0 ( luns.Length * 8 )
        paramdata.Array.[4] <- 0x00uy;
        paramdata.Array.[5] <- 0x00uy;
        paramdata.Array.[6] <- 0x00uy;
        paramdata.Array.[7] <- 0x00uy;
        for i = 0 to luns.Length - 1 do
            lun_me.toBytes paramdata.Array ( 8 + i * 8 ) luns.[i]

        let init, current = this.SetTerminateFlag 1
        if init = 0 && current = 1 then
            // Send response data to the initiator
            m_Source.ProtocolService.SendSCSIResponse
                m_Command
                m_Source.CID
                m_Source.ConCounter
                0u
                iScsiSvcRespCd.COMMAND_COMPLETE
                ScsiCmdStatCd.GOOD
                PooledBuffer.Empty
                paramdata
                cdb.AllocationLength
                ResponseFenceNeedsFlag.R_Mode

    /// <summary>
    ///  Execute REPORT SUPPORTED OPERATION CODES SCSI command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be ReportSupportedOperationCodesCDB.
    /// </remarks>
    member private this.Execute_ReportSupportedOperationCodes() : unit =
        assert( m_CDB.Type = ReportSupportedOperationCodes )
        assert( match m_CDB with | :? ReportSupportedOperationCodesCDB -> true | _ -> false )
        let cdb = m_CDB :?> ReportSupportedOperationCodesCDB

        let result =
            match cdb.ReportingOptions with
            | 0x00uy -> SupportedOperationCodeConst.SupportedAllOperationCommands
            | 0x01uy ->
                match cdb.RequestedOperationCode with
                | 0x12uy -> SupportedOperationCodeConst.CdbUsageData_INQUIRY
                | 0x15uy -> SupportedOperationCodeConst.CdbUsageData_MODE_SELECT_6
                | 0x55uy -> SupportedOperationCodeConst.CdbUsageData_MODE_SELECT_10
                | 0x1Auy -> SupportedOperationCodeConst.CdbUsageData_MODE_SENSE_6
                | 0x5Auy -> SupportedOperationCodeConst.CdbUsageData_MODE_SENSE_10
                | 0xA0uy -> SupportedOperationCodeConst.CdbUsageData_REPORT_LUNS
                | 0x03uy -> SupportedOperationCodeConst.CdbUsageData_REQUEST_SENSE
                | 0x00uy -> SupportedOperationCodeConst.CdbUsageData_TEST_UNIT_READY
                | 0x04uy -> SupportedOperationCodeConst.CdbUsageData_FORMAT_UNIT
                | 0x34uy -> SupportedOperationCodeConst.CdbUsageData_PRE_FETCH_10
                | 0x90uy -> SupportedOperationCodeConst.CdbUsageData_PRE_FETCH_16
                | 0x08uy -> SupportedOperationCodeConst.CdbUsageData_READ_6
                | 0x28uy -> SupportedOperationCodeConst.CdbUsageData_READ_10
                | 0xA8uy -> SupportedOperationCodeConst.CdbUsageData_READ_12
                | 0x88uy -> SupportedOperationCodeConst.CdbUsageData_READ_16
                | 0x25uy -> SupportedOperationCodeConst.CdbUsageData_READ_CAPACITY_10
                | 0x35uy -> SupportedOperationCodeConst.CdbUsageData_SYNCHRONIZE_CACHE_10
                | 0x91uy -> SupportedOperationCodeConst.CdbUsageData_SYNCHRONIZE_CACHE_16
                | 0x0Auy -> SupportedOperationCodeConst.CdbUsageData_WRITE_6
                | 0x2Auy -> SupportedOperationCodeConst.CdbUsageData_WRITE_10
                | 0xAAuy -> SupportedOperationCodeConst.CdbUsageData_WRITE_12
                | 0x8Auy -> SupportedOperationCodeConst.CdbUsageData_WRITE_16
                | 0x5Euy    // PERSISTENT RESERVE IN
                | 0x5Fuy    // PERSISTENT RESERVE OUT
                | 0x9Euy    // READ CAPACITY(16)
                | 0xA3uy    // REPORT SUPPORTED OPERATION CODES / REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS
                | 0x7Fuy -> // READ(32) / WRITE(32)
                    let errmsg = sprintf "REQUESTED OPERATION CODE 0x%02X has a SERVICE ACTION field." cdb.RequestedOperationCode
                    HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                    raise <| SCSIACAException (
                        m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                        { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 3us },
                        errmsg
                    )

                | _ ->
                    // unknown operation code.
                    HLogger.Trace( LogID.W_INVALID_CDB_VALUE, fun g ->
                        let errmsg =
                            sprintf "In REPORT SUPPORTED OPERATION CODES CDB, REQUESTED OPERATION CODE 0x%02X is not supported." cdb.RequestedOperationCode
                        g.Gen1( m_LogInfo, errmsg )
                    )
                    [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |]

            | 0x02uy ->
                let sa_errmsg =
                    sprintf "In REPORT SUPPORTED OPERATION CODES CDB, REQUESTED SERVICE ACTION CODE 0x%02X is not supported." cdb.RequestedServiceAction
                match cdb.RequestedOperationCode with
                | 0x5Euy -> // PERSISTENT RESERVE IN
                    if 0us <= cdb.RequestedServiceAction && cdb.RequestedServiceAction <= 3us then
                        SupportedOperationCodeConst.CdbUsageData_PERSISTENT_RESERVE_IN ( byte cdb.RequestedServiceAction )
                    else
                        HLogger.Trace( LogID.W_INVALID_CDB_VALUE, fun g -> g.Gen1( m_LogInfo, sa_errmsg ) )
                        [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |]
                | 0x5Fuy -> // PERSISTENT RESERVE OUT
                    if 0us <= cdb.RequestedServiceAction && cdb.RequestedServiceAction <= 7us then
                        SupportedOperationCodeConst.CdbUsageData_PERSISTENT_RESERVE_OUT ( byte cdb.RequestedServiceAction )
                    else
                        HLogger.Trace( LogID.W_INVALID_CDB_VALUE, fun g -> g.Gen1( m_LogInfo, sa_errmsg ) )
                        [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |]
                | 0x9Euy -> // READ CAPACITY(16)
                    if 0x10us = cdb.RequestedServiceAction then
                        SupportedOperationCodeConst.CdbUsageData_READ_CAPACITY_16
                    else
                        HLogger.Trace( LogID.W_INVALID_CDB_VALUE, fun g -> g.Gen1( m_LogInfo, sa_errmsg ) )
                        [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |]
                | 0xA3uy -> // REPORT SUPPORTED OPERATION CODES / REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS
                    if cdb.RequestedServiceAction = 0x0Cus then
                        SupportedOperationCodeConst.CdbUsageData_REPORT_SUPPORTED_OPERATION_CODES
                    elif cdb.RequestedServiceAction = 0x0Dus then
                        SupportedOperationCodeConst.CdbUsageData_REPORT_SUPPORTED_TASK_MANAGEMENT_FUNCTIONS
                    else
                        HLogger.Trace( LogID.W_INVALID_CDB_VALUE, fun g -> g.Gen1( m_LogInfo, sa_errmsg ) )
                        [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |]
                | 0x7Fuy -> // READ(32) / WRITE(32)
                    if cdb.RequestedServiceAction = 0x0009us then
                        SupportedOperationCodeConst.CdbUsageData_READ_32
                    elif cdb.RequestedServiceAction = 0x000Bus then
                        SupportedOperationCodeConst.CdbUsageData_WRITE_32
                    else
                        HLogger.Trace( LogID.W_INVALID_CDB_VALUE, fun g -> g.Gen1( m_LogInfo, sa_errmsg ) )
                        [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |]
                | 0x12uy    // INQUIRY
                | 0x15uy    // MODE SELECT(6)
                | 0x55uy    // MODE SELECT(10)
                | 0x1Auy    // MODE SENSE(6)
                | 0x5Auy    // MODE SENSE(10)
                | 0xA0uy    // REPORT LUNS
                | 0x03uy    // REQUEST SENSE
                | 0x00uy    // TEST UNIT READY
                | 0x04uy    // FORMAT UNIT
                | 0x34uy    // PRE-FETCH(10)
                | 0x90uy    // PRE-FETCH(16)
                | 0x08uy    // READ(6)
                | 0x28uy    // READ(10)
                | 0xA8uy    // READ(12)
                | 0x88uy    // READ(16)
                | 0x25uy    // READ CAPACITY(10)
                | 0x9Euy    // READ CAPACITY(16)
                | 0x35uy    // SYNCHRONIZE CACHE(10)
                | 0x91uy    // SYNCHRONIZE CACHE(16)
                | 0x0Auy    // WRITE(6)
                | 0x2Auy    // WRITE(10)
                | 0xAAuy    // WRITE(12)
                | 0x8Auy -> // WRITE(16)
                    let errmsg = sprintf "REQUESTED OPERATION CODE 0x%02X has not a SERVICE ACTION field." cdb.RequestedOperationCode
                    HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                    raise <| SCSIACAException (
                        m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                        { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 3us },
                        errmsg
                    )

                | _ ->
                    // unknown operation code.
                    HLogger.Trace( LogID.W_INVALID_CDB_VALUE, fun g ->
                        let errmsg =
                            sprintf "In REPORT SUPPORTED OPERATION CODES CDB, REQUESTED OPERATION CODE 0x%02X is not supported." cdb.RequestedOperationCode
                        g.Gen1( m_LogInfo, errmsg )
                    )
                    [| 0x00uy; 0x01uy; 0x00uy; 0x00uy; |]

            | _ ->
                let errmsg = sprintf "Invalie  REPORTING OPTIONS field value(%d), in REPORT SUPPORTED OPERATION CODES command CDB" cdb.ReportingOptions
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 2us },
                    errmsg
                )

        let init, current = this.SetTerminateFlag 1
        if init = 0 && current = 1 then
            // Send response data to the initiator
            m_Source.ProtocolService.SendSCSIResponse
                m_Command
                m_Source.CID
                m_Source.ConCounter
                0u
                iScsiSvcRespCd.COMMAND_COMPLETE
                ScsiCmdStatCd.GOOD
                PooledBuffer.Empty
                ( PooledBuffer.Rent result )
                cdb.AllocationLength
                ResponseFenceNeedsFlag.R_Mode

    /// <summary>
    ///  Execute REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS SCSI command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be ReportSupportedTaskManagementFunctionsCDB.
    /// </remarks>
    member private this.Execute_ReportSupportedTaskManagementFunctions() : unit =
        assert( m_CDB.Type = ReportSupportedTaskManagementFunctions )
        assert( match m_CDB with | :? ReportSupportedTaskManagementFunctionsCDB -> true | _ -> false )
        let cdb = m_CDB :?> ReportSupportedTaskManagementFunctionsCDB

        // Check allocation length value
        if cdb.AllocationLength < 4u then
            let errmsg = sprintf "Invalie allocation length value(%d), in REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS command CDB, must be over than or equals 4 bytes." cdb.AllocationLength
            HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
            raise <| SCSIACAException (
                m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 6us },
                errmsg
            )

        let result = PooledBuffer.Rent 4
        result.Array.[0] <- 0xF8uy; // ATS(1), ATSS(1), CACAS(1), CTSS(1), LURS(1), QTS(0), TRS(0), WAKES(0)
        result.Array.[1] <- 0x00uy; // Reserved
        result.Array.[2] <- 0x00uy; // Reserved
        result.Array.[3] <- 0x00uy; // Reserved
(*        let result = [|
            0xF8uy; // ATS(1), ATSS(1), CACAS(1), CTSS(1), LURS(1), QTS(0), TRS(0), WAKES(0)
            0x00uy; 0x00uy; 0x00uy; // Reserved
        |]*)

        let init, current = this.SetTerminateFlag 1
        if init = 0 && current = 1 then
            // Send response data to the initiator
            m_Source.ProtocolService.SendSCSIResponse
                m_Command
                m_Source.CID
                m_Source.ConCounter
                0u
                iScsiSvcRespCd.COMMAND_COMPLETE
                ScsiCmdStatCd.GOOD
                PooledBuffer.Empty
                result
                cdb.AllocationLength
                ResponseFenceNeedsFlag.R_Mode

    /// <summary>
    ///  Execute REQUEST SENSE SCSI command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be RequestSenseCDB.
    /// </remarks>
    member private this.Execute_RequestSense() : unit =
        assert( m_CDB.Type = RequestSense )
        assert( match m_CDB with | :? RequestSenseCDB -> true | _ -> false )
        let cdb = m_CDB :?> RequestSenseCDB

        let result =
            match m_LU.GetUnitAttention m_Source.I_TNexus with
            | ValueNone ->
                let s = new SenseData(
                    true,
                    SenseKeyCd.NO_SENSE,
                    ASCCd.NO_ADDITIONAL_SENSE_INFORMATION,
                    None,
                    None,
                    None,
                    None,
                    None,
                    None
                )
                s.GetSenseData cdb.DESC
            | ValueSome( x ) ->
                // Remove established Unit Attention
                m_LU.ClearUnitAttention m_Source.I_TNexus
                x.SenseData.GetSenseData cdb.DESC


        let init, current = this.SetTerminateFlag 1
        if init = 0 && current = 1 then
            // Send response data to the initiator
            m_Source.ProtocolService.SendSCSIResponse
                m_Command
                m_Source.CID
                m_Source.ConCounter
                0u
                iScsiSvcRespCd.COMMAND_COMPLETE
                ScsiCmdStatCd.GOOD
                PooledBuffer.Empty
                ( PooledBuffer.Rent result )
                ( uint32 cdb.AllocationLength )
                ResponseFenceNeedsFlag.R_Mode

    /// <summary>
    ///  Execute TEST UNIT READY command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be TestUnitReadyCDB.
    /// </remarks>
    member private this.Execute_TestUnitReady() : unit =
        assert( m_CDB.Type = TestUnitReady )
        assert( match m_CDB with | :? TestUnitReadyCDB -> true | _ -> false )
        let _ = m_CDB :?> TestUnitReadyCDB

        let r = m_Media.TestUnitReady m_ITT m_Source

        let init, current = this.SetTerminateFlag 1
        if init = 0 && current = 1 then
            // Send response data to the initiator
            match r with
            | ValueNone ->
                // Return GOOD status if media is accessible.
                m_Source.ProtocolService.SendSCSIResponse
                    m_Command
                    m_Source.CID
                    m_Source.ConCounter
                    0u
                    iScsiSvcRespCd.COMMAND_COMPLETE
                    ScsiCmdStatCd.GOOD
                    PooledBuffer.Empty
                    PooledBuffer.Empty
                    0u
                    ResponseFenceNeedsFlag.R_Mode
            | ValueSome( v ) ->
                // If media is not accessible, return NOT READY status and sense data.
                let s = new SenseData(
                    true,
                    SenseKeyCd.NOT_READY,
                    v,
                    None,
                    None,
                    None,
                    None,
                    None,
                    None
                )

                let result = ( s.GetSenseData true )
                m_Source.ProtocolService.SendSCSIResponse
                    m_Command
                    m_Source.CID
                    m_Source.ConCounter
                    0u
                    iScsiSvcRespCd.TARGET_FAILURE
                    ScsiCmdStatCd.CHECK_CONDITION
                    ( PooledBuffer.Rent result )
                    PooledBuffer.Empty
                    0u
                    ResponseFenceNeedsFlag.R_Mode

    /// <summary>
    ///  Execute FORMAT UNIT READY command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be FormatUnitCDB.
    /// </remarks>
    member private this.Execute_FormatUnit() : Task<unit> =
        assert( m_CDB.Type = FormatUnit )
        assert( match m_CDB with | :? FormatUnitCDB -> true | _ -> false )
        let _ = m_CDB :?> FormatUnitCDB // unused
        task {
            // Format
            if m_ModeParameter.SWP then
                let errmsg = "Write protected."
                HLogger.ACAException( m_LogInfo, SenseKeyCd.DATA_PROTECT, ASCCd.WRITE_PROTECTED, errmsg )
                raise <| SCSIACAException ( m_Source, true, SenseKeyCd.DATA_PROTECT, ASCCd.WRITE_PROTECTED, errmsg )

            try
                do! m_Media.Format m_ITT m_Source
            with
            | :? AggregateException as x -> raise <| x.InnerException

            let init, current = this.SetTerminateFlag 1
            if init = 0 && current = 1 then
                // Send response data to the initiator
                m_Source.ProtocolService.SendSCSIResponse
                    m_Command
                    m_Source.CID
                    m_Source.ConCounter
                    0u
                    iScsiSvcRespCd.COMMAND_COMPLETE
                    ScsiCmdStatCd.GOOD
                    PooledBuffer.Empty
                    PooledBuffer.Empty
                    0u
                    ResponseFenceNeedsFlag.R_Mode
        }


    /// <summary>
    ///  Execute READ command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be ReadCDB.
    /// </remarks>
    member private this.Execute_Read() : Task<unit> =
        assert( m_CDB.Type = Read )
        assert( match m_CDB with | :? ReadCDB -> true | _ -> false )
        let cdb = m_CDB :?> ReadCDB
        let wMediaBlockCount = m_Media.BlockCount
        let wBlkSize = Constants.MEDIA_BLOCK_SIZE

        task {
            if cdb.DPO then
                let errmsg = "DPO(disable page out) is not supported."
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 4uy; FieldPointer = 1us }, errmsg )

            if cdb.FUA then
                let errmsg = "FUA(force unit access) is not supported."
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 3uy; FieldPointer = 1us }, errmsg )

            if cdb.FUA_NV then
                let errmsg = "FUA_NV(force unit access non-volatile cache) is not supported."
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 1uy; FieldPointer = 1us }, errmsg )

            if cdb.RdProtect <> 0uy then
                let errmsg = "RDPROTECT is not supported."
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 1us }, errmsg )

            if Functions.CheckAccessRange cdb.LogicalBlockAddress ( uint64 cdb.TransferLength * wBlkSize ) wMediaBlockCount wBlkSize |> not then
                let errmsg = 
                    sprintf
                        "Out of media capacity. BlockSize=%d, TotalBlockCount=%d, LBA=%d, TransferLength=%d"
                        wBlkSize
                        wMediaBlockCount
                        cdb.LogicalBlockAddress
                        cdb.TransferLength
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )
                raise <| SCSIACAException ( m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )

            let bufLen = ( uint64 cdb.TransferLength ) * wBlkSize
            let buf = PooledBuffer.Rent ( int bufLen )
            try
                // read from media
                let! readSize =
                    if bufLen > 0UL then
                        m_Media.Read m_ITT m_Source cdb.LogicalBlockAddress buf.ArraySegment
                    else
                        task{ return 0 }

                // Notify read bytes count to LU for usage counter.
                m_LU.NotifyReadBytesCount DateTime.UtcNow ( int64 readSize )
            with
            | :? AggregateException as x -> raise <| x.InnerException

            let init, current = this.SetTerminateFlag 1
            if init = 0 && current = 1 then
                m_RWStopWatch.Stop()
                m_LU.NotifyReadTickCount DateTime.UtcNow m_RWStopWatch.ElapsedTicks

                // Send response data to the initiator
                m_Source.ProtocolService.SendSCSIResponse
                    m_Command
                    m_Source.CID
                    m_Source.ConCounter
                    0u
                    iScsiSvcRespCd.COMMAND_COMPLETE
                    ScsiCmdStatCd.GOOD
                    PooledBuffer.Empty
                    buf
                    ( uint32 bufLen )
                    ResponseFenceNeedsFlag.R_Mode
        }

    /// <summary>
    ///  Execute READ CAPACITY command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be ReadCapacityCDB.
    /// </remarks>
    member private this.Execute_ReadCapacity() : unit =
        assert( m_CDB.Type = ReadCapacity )
        assert( match m_CDB with | :? ReadCapacityCDB -> true | _ -> false )
        let cdb = m_CDB :?> ReadCapacityCDB

        let wBlockCount = m_Media.ReadCapacity m_ITT m_Source
        let blockCount = wBlockCount - 1UL

        let result =
            if cdb.OperationCode = 0x25uy then
                //  READ CAPACITY(10)
                let r = PooledBuffer.Rent 8
                if blockCount < 0xFFFFFFFFUL then
                    Functions.UInt32ToNetworkBytes r.Array 0 ( uint32 blockCount )
                else
                    Functions.UInt32ToNetworkBytes r.Array 0 0xFFFFFFFFu
                Functions.UInt32ToNetworkBytes r.Array 4 ( uint32 Constants.MEDIA_BLOCK_SIZE )
                r
(*                [|
                    if blockCount < 0xFFFFFFFFUL then
                        yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 blockCount )
                    else
                        yield! Functions.UInt32ToNetworkBytes_NewVec 0xFFFFFFFFu
                    yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )
                |]*)
            else
                //  READ CAPACITY(16)
                let r = PooledBuffer.Rent 32
                Functions.UInt64ToNetworkBytes r.Array 0 blockCount
                Functions.UInt32ToNetworkBytes r.Array 8 ( uint32 Constants.MEDIA_BLOCK_SIZE )
                r.Array.[12] <- 0x00uy; // RTO_EN, PROT_EN
                for i = 13 to 31 do
                    r.Array.[i] <- 0x00uy;
                r
(*                [|
                    yield! Functions.UInt64ToNetworkBytes_NewVec blockCount
                    yield! Functions.UInt32ToNetworkBytes_NewVec ( uint32 Constants.MEDIA_BLOCK_SIZE )
                    yield 0x00uy; // RTO_EN, PROT_EN
                    for _ = 13 to 31 do
                        yield 0x00uy;
                |]*)

        let init, current = this.SetTerminateFlag 1
        if init = 0 && current = 1 then
            // Send response data to the initiator
            m_Source.ProtocolService.SendSCSIResponse
                m_Command
                m_Source.CID
                m_Source.ConCounter
                0u
                iScsiSvcRespCd.COMMAND_COMPLETE
                ScsiCmdStatCd.GOOD
                PooledBuffer.Empty
                result
                cdb.AllocationLength
                ResponseFenceNeedsFlag.R_Mode

    /// <summary>
    ///  Execute SYNCHRONIZE CACHE command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be WriteCDB.
    /// </remarks>
    member private this.Execute_SynchronizeCache() : unit =
        assert( m_CDB.Type = SynchronizeCache )
        assert( match m_CDB with | :? SynchronizeCacheCDB -> true | _ -> false )
        let cdb = m_CDB :?> SynchronizeCacheCDB

        if m_Media.BlockCount <= cdb.LogicalBlockAddress + ( uint64 cdb.NumberOfBlocks ) then
            let errmsg = sprintf "Invalid LBA(0x%16X) and NumberOfBlocks(%d), in SYNCHRONIZE CACHE command CDB" cdb.LogicalBlockAddress cdb.NumberOfBlocks
            HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )
            raise <| SCSIACAException ( m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )

        // Nothig to do

        let init, current = this.SetTerminateFlag 1
        if init = 0 && current = 1 then
            // Send response data to the initiator
            m_Source.ProtocolService.SendSCSIResponse
                m_Command
                m_Source.CID
                m_Source.ConCounter
                0u
                iScsiSvcRespCd.COMMAND_COMPLETE
                ScsiCmdStatCd.GOOD
                PooledBuffer.Empty
                PooledBuffer.Empty
                0u
                ResponseFenceNeedsFlag.R_Mode

    /// <summary>
    ///  Execute WRITE command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be WriteCDB.
    /// </remarks>
    member private this.Execute_Write() : Task<unit> =
        assert( m_CDB.Type = Write )
        assert( match m_CDB with | :? WriteCDB -> true | _ -> false )
        let cdb = m_CDB :?> WriteCDB
        let wBlkSize = Constants.MEDIA_BLOCK_SIZE
        let wTransBytesLen = ( uint64 cdb.TransferLength ) * wBlkSize
        let wMediaBlockCount = m_Media.BlockCount

        task {
            if cdb.DPO then
                let errmsg = "DPO(disable page out) is not supported."
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 4uy; FieldPointer = 1us }, errmsg )

            if cdb.FUA then
                let errmsg = "FUA(force unit access) is not supported."
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 3uy; FieldPointer = 1us }, errmsg )

            if cdb.FUA_NV then
                let errmsg = "FUA_NV(force unit access non-volatile cache) is not supported."
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 1uy; FieldPointer = 1us }, errmsg )

            if cdb.WRPROTECT <> 0uy then
                let errmsg = "WRPROTECT is not supported."
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB, errmsg )
                raise <| SCSIACAException (
                    m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.INVALID_FIELD_IN_CDB,
                    { CommandData = true; BPV = true; BitPointer = 7uy; FieldPointer = 1us }, errmsg )

            if m_ModeParameter.SWP then
                let errmsg = "Write protected."
                HLogger.ACAException( m_LogInfo, SenseKeyCd.DATA_PROTECT, ASCCd.WRITE_PROTECTED, errmsg )
                raise <| SCSIACAException ( m_Source, true, SenseKeyCd.DATA_PROTECT, ASCCd.WRITE_PROTECTED, errmsg )

            if Functions.CheckAccessRange cdb.LogicalBlockAddress ( uint64 cdb.TransferLength * wBlkSize ) wMediaBlockCount  wBlkSize |> not then
                let errmsg = 
                    sprintf
                        "Out of media capacity. BlockSize=%d, TotalBlockCount=%d, LBA=%d, TransferLength=%d"
                        wBlkSize
                        wMediaBlockCount
                        cdb.LogicalBlockAddress
                        cdb.TransferLength
                HLogger.ACAException( m_LogInfo, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )
                raise <| SCSIACAException ( m_Source, true, SenseKeyCd.ILLEGAL_REQUEST, ASCCd.LOGICAL_BLOCK_ADDRESS_OUT_OF_RANGE, errmsg )

            let wv = 
                let v2 = List< struct( uint64 * PooledBuffer ) >( m_DataOut.Length + 1 )
                if PooledBuffer.length m_Command.DataSegment > 0 then
                    v2.Add( struct ( 0UL, m_Command.DataSegment ) )
                for itr in m_DataOut do
                    if PooledBuffer.length itr.DataSegment > 0 then
                        v2.Add( struct ( uint64 itr.BufferOffset, itr.DataSegment ) )
                v2.Sort( Comparer<struct( uint64 * PooledBuffer )>.Create( fun struct( a, _ ) struct( b, _ ) -> a.CompareTo( b ) ) )
                v2

            try
                for struct ( wBufferOffset, wDataSegment ) in wv do
                    if wBufferOffset < wTransBytesLen then
                        let wCount =
                            ( min wTransBytesLen ( wBufferOffset + ( uint64 wDataSegment.Count ) ) ) -
                            ( wBufferOffset )

                        // Write to media
                        let! writtenCount =
                            let struct( lbaOffset, remainder ) = UInt64.DivRem( wBufferOffset, wBlkSize )
                            m_Media.Write m_ITT m_Source ( cdb.LogicalBlockAddress + lbaOffset ) remainder ( wDataSegment.GetArraySegment 0 ( int wCount ) )

                        // Notify written bytes count to LU for usage counter
                        m_LU.NotifyWrittenBytesCount DateTime.UtcNow ( int64 writtenCount )
            with
            | :? AggregateException as x -> raise <| x.InnerException

            let recvDataLength =
                let rec loop ( cnt : int ) ( s : int ) =
                    if cnt < wv.Count then
                        let struct ( _, seg ) = wv.[ cnt ]
                        loop ( cnt + 1 ) ( s + seg.Count )
                    else
                        s
                loop 0 0

            let init, current = this.SetTerminateFlag 1
            if init = 0 && current = 1 then
                m_RWStopWatch.Stop()
                m_LU.NotifyWriteTickCount DateTime.UtcNow m_RWStopWatch.ElapsedTicks

                // Send response data to the initiator
                m_Source.ProtocolService.SendSCSIResponse
                    m_Command
                    m_Source.CID
                    m_Source.ConCounter
                    ( uint32 recvDataLength )
                    iScsiSvcRespCd.COMMAND_COMPLETE
                    ScsiCmdStatCd.GOOD
                    PooledBuffer.Empty
                    PooledBuffer.Empty
                    0u
                    ResponseFenceNeedsFlag.R_Mode
        }


