//=============================================================================
// Haruka Software Storage.
// ScsiTaskForDummyDevice.fs : Defines ScsiTaskForDummyDevice structure.
// ScsiTaskForDummyDevice imprements simplified ScsiTak for DummyDevice.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open System.Text
open System.Threading.Tasks

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
type ScsiTaskForDummyDevice
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

    /// original ScsiTask object
    let m_ScsiTask = new ScsiTask( m_StatusMaster, m_Source, m_Command, m_CDB, m_DataOut, m_LU, m_Media, m_ModeParameter, m_PRManager, m_ACANoncompliant )

    /// Log Information
    let m_LogInfo = struct ( m_ObjID, ValueSome m_Source, ValueSome m_Command.InitiatorTaskTag, ValueSome m_Command.LUN )

    do
        if HLogger.IsVerbose then
            HLogger.Trace( LogID.V_TRACE, fun g ->
                let msg = sprintf "ScsiTaskForDummyDevice instance was created. Operation code=%s" ( CDBTypes.getName m_CDB.Type )
                g.Gen1( m_ObjID, msg )
            )

    interface IBlockDeviceTask with

        /// Return task type.
        override _.TaskType : BlockDeviceTaskType =
            ( m_ScsiTask :> IBlockDeviceTask ).TaskType

        /// Return source information of this task.
        override _.Source : CommandSourceInfo =
            ( m_ScsiTask :> IBlockDeviceTask ).Source
    
        /// Return  Initiator task tag.
        override _.InitiatorTaskTag : ITT_T =
            ( m_ScsiTask :> IBlockDeviceTask ).InitiatorTaskTag

        /// Return SCSI Command object of this object.
        override _.SCSICommand : SCSICommandPDU =
            ( m_ScsiTask :> IBlockDeviceTask ).SCSICommand

        /// Return total received data length in bytes.
        override _.ReceivedDataLength : uint =
            ( m_ScsiTask :> IBlockDeviceTask ).ReceivedDataLength

        /// Return CDB of this object
        override _.CDB : ICDB voption =
            ( m_ScsiTask :> IBlockDeviceTask ).CDB

        /// Execute this SCSI task.
        override this.Execute() : unit -> Task<unit> =

            // ****************************************************************
            // This method is called in critical section of BlockDeviceLU task set lock.
            // And returned task workflow is executed in asyncnously.
            // ****************************************************************

            if HLogger.IsVerbose then
                HLogger.Trace( LogID.V_TRACE, fun g ->
                    let msg = sprintf "Scsi task has been executed. Operation code=%s" ( CDBTypes.getName m_CDB.Type )
                    g.Gen1(  m_ObjID, msg )
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
            | FormatUnit                                // SBC-2 5.2 FORMAT UNIT command
            | PreFetch                                  // SBC-2 5.3 PRE-FETCH(10), 5.4 PRE-FETCH(16) command
            | Read                                      // SBC-2 5.5 READ(6), 5.6 READ(10), 5.7 READ(12), 5.8 READ(16), 5.9 READ(32) command
            | ReadDefectData                            // SBC-2 5.12 READ DEFECT DATA(10), 5.13 READ DEFECT DATA(12) command
            | ReadLong                                  // SBC-2 5.14 READ LONG(10), 5.15 READ LONG(16) command
            | ReassignBlocks                            // SBC-2 5.16 REASSIGN BLOCKS command
            | StartStopUnit                             // SBC-2 5.17 START STOP UNIT command
            | Verify                                    // SBC-2 5.20 VERIFY(10), 5.21 VERIFY(12), 5.22 VERIFY(16), 5.23 VERIFY(32) command
            | Write                                     // SBC-2 5.24 WRITE(6), 5.25 WRITE(10), 5.26 WRITE(12), 5.27 WRITE(16), 5.28 WRITE(32) command
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
            | ModeSense                                 // SPC-3 6.9 MODE SENSE(6), 6.10 MODE SENSE(10) command
            | PersistentReserveIn                       // SPC-3 6.11 PERSISTENT RESERVE IN command
            | PersistentReserveOut                      // SPC-3 6.12 PERSISTENT RESERVE OUT command
            | ReportLUNs                                // SPC-3 6.21 REPORT LUNS command
            | RequestSense                              // SPC-3 6.27 REQUEST SENSE command
            | ReadCapacity                              // SBC-2 5.10 READ CAPACITY(10), 5.11 READ CAPACITY(16) command
                ->
                    ( m_ScsiTask :> IBlockDeviceTask ).Execute()


            | TestUnitReady                             // SPC-3 6.33 TEST UNIT READY command
                ->
                // If media is not accessible, return NOT READY status and sense data.
                let msg = "TestUnitReady command has been requested to the dummy device."
                HLogger.ACAException( m_LogInfo, SenseKeyCd.NOT_READY, ASCCd.MEDIUM_NOT_PRESENT, msg )
                let ex = SCSIACAException ( m_Source, true, SenseKeyCd.NOT_READY, ASCCd.MEDIUM_NOT_PRESENT, msg )
                m_LU.NotifyTerminateTaskWithException this ex
                fun () -> Task.FromResult()

            | SynchronizeCache  ->                      // SBC-2 5.18 SYNCHRONIZE CACHE(10), 5.19 SYNCHRONIZE CACHE(16) command

                // Nothig to do

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
                m_LU.NotifyTerminateTask this
                fun () -> task{ () }

            | ReportSupportedOperationCodes             // SPC-3 6.23 REPORT SUPPORTED OPERATION CODES command
                ->
                    ( m_ScsiTask :> IBlockDeviceTask ).Execute()
            | ReportSupportedTaskManagementFunctions    // SPC-3 6.24 REPORT SUPPORTED TASK MANAGEMENT FUNCTIONS command
                ->
                    ( m_ScsiTask :> IBlockDeviceTask ).Execute()


        /// Get task description string.
        override _.DescString : string =
            "SCSI task for dummy device. Command=" + m_CDB.DescriptString

        /// <summary>
        ///   Notify task terminate request
        /// </summary>
        /// <param name="needResp">
        ///   If task is terminated from the other I_T Nexus, set true to this value.
        /// </param>
        override _.NotifyTerminate( needResp : bool ) : unit =
            ( m_ScsiTask :> IBlockDeviceTask ).NotifyTerminate needResp

        /// Return ACANoncompliant flag value
        override _.ACANoncompliant : bool =
            m_ACANoncompliant

        /// Release PooledBuffer
        override _.ReleasePooledBuffer() =
            m_DataOut
            |> Seq.map _.DataSegment
            |> Seq.insertAt 0 m_Command.DataSegment
            |> PooledBuffer.Return

    //=========================================================================
    // Private method

    /// <summary>
    ///  Execute INQUIRY SCSI command.
    /// </summary>
    /// <remarks>
    ///  m_CDB muast be InquiryCDB.
    /// </remarks>
    member private _.Execute_Inquiry() : unit =
        assert( m_CDB.Type = Inquiry )
        assert( match m_CDB with | :? InquiryCDB -> true | _ -> false )
        let cdb = m_CDB :?> InquiryCDB

        let inData =
            if cdb.EVPD then
                match cdb.PageCode with
                | 0x80uy ->
                    //  Unit Serial Number VPD page
                    [|
                        yield 0x0Cuy;   // PERIPHERAL QUALIFIER(0b) PERIPHERAL DEVICE TYPE(0Ch)
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
                        yield 0x0Cuy;   // PERIPHERAL QUALIFIER(0b) PERIPHERAL DEVICE TYPE(0Ch)
                        yield 0x83uy;   // PAGE CODE(0x83)

                        let sessParam = m_Source.ProtocolService.SessionParameter

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
                        yield! int16( dec2.Length + dec3.Length )
                                |> Functions.Int16ToNetworkBytes_NewVec

                        yield! dec2;    // DISCRIPTOR 2
                        yield! dec3;    // DISCRIPTOR 3
                    |]
                | 0x86uy ->
                    // Extended INQUIRY Data
                    [|
                        yield 0x0Cuy;   // PERIPHERAL QUALIFIER(0b) PERIPHERAL DEVICE TYPE(0Ch)
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
                        yield 0x0Cuy;   // PERIPHERAL QUALIFIER(0b) PERIPHERAL DEVICE TYPE(0Ch)
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
                        yield 0x0Cuy;   // PERIPHERAL QUALIFIER(001b) PERIPHERAL DEVICE TYPE(0)
                        yield 0x00uy;   // PAGE CODE(0x00)
                        yield 0x00uy;   // Reserved
                        yield 0x06uy;   // PAGE LENGTH
                        yield! [| 0x00uy; 0x80uy; 0x83uy; 0x86uy; 0xB0uy; 0xB1uy; |] // supported VPD pages list
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
                    yield 0x0Cuy;   // PERIPHERAL QUALIFIER(0b) PERIPHERAL DEVICE TYPE(0Ch)
                    yield 0x00uy;   // RMB(0)
                    yield 0x05uy;   // VERSION(05h,SPC-3)
                    yield 0x22uy;   // NORMACA(1) HISUP(0) RESPONSE DATA FORMAT(2)
                    yield 92uy;     // ADDITIONAL LENGTH( 96 bytes length - 4 )
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
                    yield! [| 0x01uy; 0xE0uy; |] // VERSION DESCRIPTOR 5(SCC-2)
                    yield! [| 0x00uy; 0x00uy; |] // VERSION DESCRIPTOR 6
                    yield! [| 0x00uy; 0x00uy; |] // VERSION DESCRIPTOR 7
                    yield! [| 0x00uy; 0x00uy; |] // VERSION DESCRIPTOR 8

                    // Reserved
                    for _ = 74 to 95 do yield 0uy;
                |]

        let init, current = m_ScsiTask.SetTerminateFlag 1
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
