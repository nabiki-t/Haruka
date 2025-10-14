//=============================================================================
// Haruka Software Storage.
// InterfaceDef.fs : Defines the interfaces that is published by application
// domains in Harula project.

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Threading.Tasks

open Haruka.Constants
open Haruka.IODataTypes

//=============================================================================
// Type definition

/// iSCSI Negotiable parameters( Connection only parameter )
[<NoComparison>]
type IscsiNegoParamCO = {
    AuthMethod : AuthMethodCandidateValue[];    // IO( Security negotiation stage only ), Initiator/Target
    HeaderDigest : DigestType[];                // IO, Initiator/Target
    DataDigest : DigestType[];                  // IO, Initiator/Target
    MaxRecvDataSegmentLength_I : uint32         // ALL, Initiator( MaxRecvDataSegmentLength value of Initiator->Target value )
    MaxRecvDataSegmentLength_T : uint32         // ALL, Target( MaxRecvDataSegmentLength value of Target->Initiator value )
}

/// iSCSI Negotiable parameters( Session wide parameter )
[<NoComparison>]
type IscsiNegoParamSW = {
    MaxConnections : uint16                     // LO, Initiator/Target
    TargetGroupID : TGID_T                      // IO, Initiator( This value specify target group )
    TargetConf : TargetGroupConf.T_Target       // IO, Initiator( This value reference to target configuration in configuration master )
    InitiatorName : string                      // IO, Initiator
    InitiatorAlias : string                     // All, Initiator
    TargetPortalGroupTag : TPGT_T               // IO, Target
    InitialR2T : bool                           // LO, Initiator/Target
    ImmediateData : bool                        // LO, Initiator/Target
    MaxBurstLength : uint32                     // LO, Initiator/Target
    FirstBurstLength : uint32                   // LO, Initiator/Target
    DefaultTime2Wait : uint16                   // LO, Initiator/Target
    DefaultTime2Retain : uint16                 // LO, Initiator/Target
    MaxOutstandingR2T : uint16                  // LO, Initiator/Target
    DataPDUInOrder : bool                       // LO, Initiator/Target
    DataSequenceInOrder : bool                  // LO, Initiator/Target
    ErrorRecoveryLevel : byte                   // LO, Initiator/Target
}


//=============================================================================
// Interface definition

/// <summary>
///   Common interface of component.
///   All components must implements this interface.
/// </summary>
type IComponent =

    // ------------------------------------------------------------------------
    /// <summary>
    ///  Notince terminate request.
    /// </summary>
    /// <remarks>
    ///  May be called multiple times for the same object.
    /// </remarks>
    abstract Terminate : unit -> unit


/// <summary>
///   Interface of killer object that notices terminate request to some objects.
/// </summary>
type IKiller =

    // ----------------------------------------------------------------------------
    /// <summary>
    ///  Add an object that is noticed terminate request.
    /// </summary>
    /// <param name="o">
    ///   Request target object.
    /// </param>
    abstract Add : o : IComponent -> unit

    // ----------------------------------------------------------------------------
    /// <summary>
    ///  Notice terminate request to all of objects.
    /// </summary>
    abstract NoticeTerminate : unit -> unit

    // ----------------------------------------------------------------------------
    /// <summary>
    ///  Get already noticed terminate request.
    /// </summary>
    abstract IsNoticed : bool


/// <summary>
///   Interface of iSCSI Task object.
///   iSCSI Task object represents an iSCSI Task, that has an Initiator Task Tag.
/// </summary>
type IIscsiTask =

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the type of this iSCSI task.
    /// </summary>
    /// <returns>
    ///   Type of iSCSI task.
    /// </returns>
    abstract TaskType : iSCSITaskType

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the type string value of this iSCSI task for log output.
    /// </summary>
    abstract TaskTypeName : string

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the Initiator Task Tag.
    /// </summary>
    /// <returns>
    ///   The value of initiator task tag.
    /// </returns>
    abstract InitiatorTaskTag : ITT_T voption

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the CmdSN value.
    /// </summary>
    abstract CmdSN : CMDSN_T voption

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the Immidiate flag.
    /// </summary>
    abstract Immidiate : bool voption

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Whether this command can be executable.
    /// </summary>
    /// <returns>
    ///   If this command lady to execute, return true.
    /// </returns>
    abstract IsExecutable : bool

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get CID and counter of allegiant connection.
    /// </summary>
    /// <returns>
    ///   Pair of CID and counter, representing the connection that this task was received.
    /// </returns>
    abstract AllegiantConnection : struct ( CID_T * CONCNT_T )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Execute this command.
    /// </summary>
    abstract GetExecuteTask : unit -> struct( ( unit -> unit ) * IIscsiTask )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   This task already compleated and removale or not.
    /// </summary>
    abstract IsRemovable : bool

    // ------------------------------------------------------------------------
    /// <summary>
    ///   GetExecuteTask method had been called or not.
    /// </summary>
    abstract Executed : bool


/// <summary>
///   Interface of SCSI task router component.
///   IProtocolService interface defines boundary of iSCSI and SCSI.
/// </summary>
type IProtocolService =
    inherit IComponent

    // ------------------------------------------------------------------------
    /// <summary>
    ///   ABORT TASK task management function request.
    ///   It aborts the task specified by referencedTaskTag.
    /// </summary>
    /// <param name="iScsiTask">
    ///   Received the task management request PDUs.
    /// </param>
    /// <param name="lun">
    ///   LUN of the objective logical unit.
    /// </param>
    /// <param name="referencedTaskTag">
    ///   The task tag value that should be aborted.
    /// </param>
    abstract AbortTask : iScsiTask:IIscsiTask -> lun:LUN_T -> referencedTaskTag:ITT_T -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   ABORT TASK SET task management function request.
    ///   It aborts all of the task that established by the session in specified logical unit.
    /// </summary>
    /// <param name="iScsiTask">
    ///   Received the task management request PDUs.
    /// </param>
    /// <param name="lun">
    ///   LUN of the objective logical unit.
    /// </param>
    abstract AbortTaskSet : iScsiTask:IIscsiTask -> lun:LUN_T -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   CLEAR ACA task management function request.
    ///   It clears the ACA state in specified logical unit.
    /// </summary>
    /// <param name="iScsiTask">
    ///   Received the task management request PDUs.
    /// </param>
    /// <param name="lun">
    ///   LUN of the objective logical unit.
    /// </param>
    abstract ClearACA : iScsiTask:IIscsiTask -> lun:LUN_T -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   CLEAR TASK SET task management function request.
    ///   It aborts all of the task in specified logical unit.
    /// </summary>
    /// <param name="iScsiTask">
    ///   Received the task management request PDUs.
    /// </param>
    /// <param name="lun">
    ///   LUN of the objective logical unit.
    /// </param>
    abstract ClearTaskSet : iScsiTask:IIscsiTask -> lun:LUN_T -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   LOGICAL UNIT RESET task management function request.
    ///   It resets specified logical unit.
    /// </summary>
    /// <param name="iScsiTask">
    ///   Received the task management request PDUs.
    /// </param>
    /// <param name="lun">
    ///   LUN of the objective logical unit.
    /// </param>
    abstract LogicalUnitReset : iScsiTask:IIscsiTask -> lun:LUN_T -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   SCSI Command request.
    ///   It request processing of SCSI Command request PDU.
    /// </summary>
    /// <param name="cid">
    ///   CID of connection which received the SCSI command PDU.
    /// </param>
    /// <param name="counter">
    ///   Connection counter value of connection which received the SCSI command PDU.
    /// </param>
    /// <param name="command">
    ///   The SCSI Command PDU.
    /// </param>
    /// <param name="data">
    ///   A list of SCSI Data-Out PDUs.
    /// </param>
    abstract SCSICommand : cid:CID_T -> counter:CONCNT_T -> command:SCSICommandPDU -> data:SCSIDataOutPDU list -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send response data to the initiator. 
    /// </summary>
    /// <param name="reqCmdPDU">
    ///   SCSI Command PDU that requests SCSI command.
    /// </param>
    /// <param name="cid">
    ///   CID of connection that connecting to the initiator.
    /// </param>
    /// <param name="counter">
    ///   Connection counter value of connection that connecting to the initiator.
    /// </param>
    /// <param name="recvDataLength">
    ///   Received output data length, transfered by the SCSI command specified by reqCmdPDU.
    /// </param>
    /// <param name="argRespCode">
    ///   iSCSI responce code that will be transferd to the initiator.
    /// </param>
    /// <param name="argStatCode">
    ///   SCSI status code that will be transferd to the initiator.
    /// </param>
    /// <param name="senseData">
    ///   Sense data that will be transfered to the initiator.
    /// </param>
    /// <param name="resData">
    ///   All of response data.
    ///   This data may include mode than the receive buffer in the initiator
    ///   specified by allocationLength parameter.
    /// </param>
    /// <param name="allocationLength">
    ///   Receive buffer size in the initiator, that is specify by SCSI command.
    /// </param>
    /// <param name="needResponseFence">
    ///   Specify the type of lock based on ResponseFence.
    /// </param>
    /// <remarks>
    ///   This function run in asyncnously.
    ///   In sending process, if unknown error is occurred, session recovery is executed.
    /// </remarks>
    abstract SendSCSIResponse :
            reqCmdPDU:SCSICommandPDU ->
            cid:CID_T ->
            counter:CONCNT_T ->
            recvDataLength:uint32 ->
            argRespCode:iScsiSvcRespCd ->
            argStatCode:ScsiCmdStatCd ->
            senseData:PooledBuffer ->
            resData:PooledBuffer ->
            allocationLength:uint32 ->
            needResponseFence:ResponseFenceNeedsFlag ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send response PDU other than SCSI response.
    /// </summary>
    /// <param name="cid">
    ///   CID of connection that connecting to the initiator.
    /// </param>
    /// <param name="counter">
    ///   Connection counter value of connection that connecting to the initiator.
    /// </param>
    /// <param name="pdu">
    ///   The response PDU.
    /// </param>
    /// <param name="lun">
    ///   LUN value of logical unit where call this method.
    /// </param>
    abstract SendOtherResponse :
            cid:CID_T ->
            counter:CONCNT_T ->
            pdu:ILogicalPDU ->
            lun:LUN_T ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the TSIH value of the session, that hosts this protocol service component.
    /// </summary>
    /// <returns>
    ///   The TSIH value.
    /// </returns>
    abstract TSIH : TSIH_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Notice the session recovery to this session.
    /// </summary>
    /// <param name="msg">
    ///   A message string that descripts cource of this session recovery.
    /// </param>
    abstract NoticeSessionRecovery : msg:string -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get session wide parameter value, that is used to create this task router object.
    /// </summary>
    /// <returns>
    ///   Current effective session wide parameter values.
    /// </returns>
    abstract SessionParameter : IscsiNegoParamSW

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get LUNs which is accessable from same target.
    /// </summary>
    abstract GetLUNs : unit -> LUN_T[]

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get used count of the task queue.
    /// </summary>
    /// <remarks>
    ///   If there are multiple accessible LU, this function returns the maximum value of task count.
    ///   Task count in the task queue may be over BDLU_MAX_TASKSET_SIZE.
    /// </remarks>
    abstract GetTaskQueueUsage : unit -> int



/// <summary>
///  This structure descripts 
/// </summary>
[<NoComparison>]
type CommandSourceInfo =
    {
        /// I_T Nexus information where command was received on.
        I_TNexus : ITNexus

        /// ConnectionID of the connection where command was received on.
        CID : CID_T

        /// Connection counter value of the connection where command was received on.
        ConCounter : CONCNT_T

        /// TSIH value of session where command was received.
        TSIH : TSIH_T

        /// IProtocolService interface of TaskRouter that relates the session object where command was received.
        ProtocolService : IProtocolService

        /// Killer object of session object.
        SessionKiller : IKiller
    }

/// <summary>
///   This interface defines component that implements functionality of media device.
/// </summary>
type IMedia =
    inherit IComponent

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Initialize the Media object.
    ///   This methos is called after media object is cleared.
    /// </summary>
    abstract Initialize : unit -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Finalize the media object.
    ///   This methos is called before media object is destroyed.
    ///   If the process is terminated unexpectedly, this function is not called.
    /// </summary>
    abstract Closing : unit -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   SCSI TEST UNIT READY command.
    ///   If media can be accessed, this function returns true, or if not, returns false or throws SCSIStatusException.
    ///   If this function returns false, SCSI command is terminated with CHECK CONDITION status / NOT READY sense key.
    /// </summary>
    /// <param name="initiatorTaskTag">
    ///   Initiator task tag value of requested SCSI command.
    /// </param>
    /// <param name="source">
    ///   Source information of received SCSI command PDU.
    /// </param>
    /// <returns>
    ///   If media can be accessed, returns None.
    ///   If media cannot be accessed, it returns additional sense code that represents the cource.
    /// </returns>
    abstract TestUnitReady : initiatorTaskTag:ITT_T -> source:CommandSourceInfo -> ASCCd voption

    // ------------------------------------------------------------------------
    /// <summary>
    ///   SCSI READ CAPACITY command.
    ///   This method returns logical block count in the media.
    /// </summary>
    /// <param name="initiatorTaskTag">
    ///   Initiator task tag value of requested SCSI command.
    /// </param>
    /// <param name="source">
    ///   Source information of received SCSI command PDU.
    /// </param>
    /// <returns>
    ///   Logical block count.
    /// </returns>
    abstract ReadCapacity : initiatorTaskTag:ITT_T -> source:CommandSourceInfo -> uint64

    // ------------------------------------------------------------------------
    /// <summary>
    ///   SCSI READ command.
    ///   This method returns specified logical block data.
    /// </summary>
    /// <param name="initiatorTaskTag">
    ///   Initiator task tag value of requested SCSI command.
    /// </param>
    /// <param name="source">
    ///   Source information of received SCSI command PDU.
    /// </param>
    /// <param name="argLBA">
    ///   Start block address of block sequence to be read.
    /// </param>
    /// <param name="buffer">
    ///   Array segment that specify a part of the buffer.
    ///   Read data will be written into specified range of the bytes array.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been read. If the read is successful, buffer.Count is returned.
    /// </returns>
    /// <remarks>
    /// <code>
    ///     Media |-======================================================================================-| ...... (A)
    ///           |-...... argLBA*(Block Size) ......-|-========== Data to be read =========-| .....................(B)
    ///                                                               ||
    ///                                                               \/
    ///                      |-... buffer.Offset ....-|-=========== buffer.Count ===========-| .................... (C)
    ///     In memory buffer |-=====================================================================-|
    ///  Data to be read in media is range (B), and that is represented by argLBA and buffer.Count.
    ///  Read data is must be written to a part of in memory buffer, range (C), that is represented by buffer.Offset and buffer.Count.
    /// </code>
    /// </remarks>
    abstract Read : initiatorTaskTag:ITT_T -> source:CommandSourceInfo -> argLBA:uint64 -> buffer:ArraySegment<byte> -> Task<int>

    // ------------------------------------------------------------------------
    /// <summary>
    ///   SCSI WRITE command.
    ///   This method write specified data to media.
    /// </summary>
    /// <param name="initiatorTaskTag">
    ///   Initiator task tag value of requested SCSI command.
    /// </param>
    /// <param name="source">
    ///   Source information of received SCSI command PDU.
    /// </param>
    /// <param name="argLBA">
    ///   Start block address of block sequence to be wrote. This value is the relative LBA with the first LBA of the media being zero.
    /// </param>
    /// <param name="offset">
    ///   Indicates the write start position within the block indicated by the argLBA. 
    ///   In short, it is the remainder when the write start position is divided by the block size.
    /// </param>
    /// <param name="data">
    ///   Bytes sequence to write.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been written. If the write is successful, buffer.Count is returned.
    /// </returns>
    /// <remarks>
    ///   The bytes sequense to written is data.Array.[ data.Offset ] to data.Array.[ data.Offset + data.Count - 1 ].
    ///   Above data have to be written to argLBA * (Block Size) in the media. 
    ///   <code>
    ///                      |-... buffer.Offset ....-|-=========== buffer.Count ===========-| .................... (A)
    ///     In memory buffer |-=====================================================================-|
    ///                                                               ||
    ///                                                               \/
    ///     Media |-======================================================================================-| ...... (B)
    ///           |-.. argLBA*(Block Size)+offset ...-|-========== Overwritten range =======-| .....................(C)

    ///     Data to be written is range of (A). That is the area represents by data.Offset and data.Count in data.Array.
    ///     Range (B) is all of the media.
    ///     Range (C) is the part of (B), and that starts at argLBA*(Block Size) and length is buffer.Count bytes.
    ///   </code>
    /// </remarks>
    abstract Write : initiatorTaskTag:ITT_T -> source:CommandSourceInfo -> argLBA:uint64 -> offset:uint64 -> data:ArraySegment<byte> -> Task<int>

    // ------------------------------------------------------------------------
    /// <summary>
    ///   SCSI FORMAT command.
    /// </summary>
    /// <param name="initiatorTaskTag">
    ///   Initiator task tag value of requested SCSI command.
    /// </param>
    /// <param name="source">
    ///   Source information of received SCSI command PDU.
    /// </param>
    abstract Format : initiatorTaskTag:ITT_T -> source:CommandSourceInfo -> Task<unit>

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Notify logical unit reset.
    ///   This method called when logical unit reset was occured in the LU.
    /// </summary>
    /// <param name="initiatorTaskTag">
    ///   Initiator task tag value of requested SCSI command.
    ///   If LU reset is occured with internal reason, initiatorTaskTag can be None.
    /// </param>
    /// <param name="source">
    ///   Source information of received SCSI command PDU.
    ///   If LU reset is occured with internal reason, source can be None.
    /// </param>
    abstract NotifyLUReset : initiatorTaskTag:ITT_T voption -> source:CommandSourceInfo voption -> unit


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Media control request.
    /// </summary>
    /// <param name="request">
    ///   Request message
    /// </param>
    /// <returns>
    ///   responce message.
    /// </returns>
    abstract MediaControl : request:MediaCtrlReq.T_Request -> Task<MediaCtrlRes.T_Response>


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Blocks count property.
    /// </summary>
    abstract BlockCount : uint64

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Write protect property.
    /// </summary>
    abstract WriteProtect : bool

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Media index ID
    /// </summary>
    abstract MediaIndex : MEDIAIDX_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///   String that descripts this media.
    /// </summary>
    abstract DescriptString : string

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Obtain the total number of read bytes.
    /// </summary>
    /// <returns>
    ///   Aggregated results.
    /// </returns>
    abstract GetReadBytesCount : unit -> ResCountResult array

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Obtain the total number of written bytes.
    /// </summary>
    /// <returns>
    ///   Aggregated results.
    /// </returns>
    abstract GetWrittenBytesCount : unit -> ResCountResult array

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Obtain the tick count of read operation.
    /// </summary>
    /// <returns>
    ///   Aggregated results.
    /// </returns>
    abstract GetReadTickCount : unit -> ResCountResult array

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Obtain the tick count of write operation.
    /// </summary>
    /// <returns>
    ///   Aggregated results.
    /// </returns>
    abstract GetWriteTickCount : unit -> ResCountResult array

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get sub media object.
    /// </summary>
    abstract GetSubMedia : unit -> IMedia list


/// <summary>
///   Interface of device server component.
///   This interface defines behavior of device server should implement as LU.
/// </summary>
type ILU =
    inherit IComponent

    // ------------------------------------------------------------------------
    /// <summary>
    ///   ABORT TASK task management function request.
    ///   It aborts the task specified by referencedTaskTag.
    /// </summary>
    /// <param name="source">
    ///   Source information of received task management request PDU.
    /// </param>
    /// <param name="initiatorTaskTag">
    ///   Initiator Task Tag value of received task managedmt requst.
    /// </param>
    /// <param name="referencedTaskTag">
    ///   The task tag value that should be aborted.
    /// </param>
    abstract AbortTask : source:CommandSourceInfo -> initiatorTaskTag:ITT_T -> referencedTaskTag:ITT_T -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   ABORT TASK SET task management function request.
    ///   It aborts all of the task that established by the session in specified logical unit.
    /// </summary>
    /// <param name="source">
    ///   Source information of received task management request PDU.
    /// </param>
    /// <param name="initiatorTaskTag">
    ///   Initiator Task Tag value of received task managedmt requst.
    /// </param>
    abstract AbortTaskSet : source:CommandSourceInfo -> initiatorTaskTag:ITT_T -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   CLEAR ACA task management function request.
    ///   It clears the ACA state in specified logical unit.
    /// </summary>
    /// <param name="source">
    ///   Source information of received task management request PDU.
    /// </param>
    /// <param name="initiatorTaskTag">
    ///   Initiator Task Tag value of received task managedmt requst.
    /// </param>
    abstract ClearACA : source:CommandSourceInfo -> initiatorTaskTag:ITT_T -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   CLEAR TASK SET task management function request.
    ///   It aborts all of the task in specified logical unit.
    /// </summary>
    /// <param name="source">
    ///   Source information of received task management request PDU.
    /// </param>
    /// <param name="initiatorTaskTag">
    ///   Initiator Task Tag value of received task managedmt requst.
    /// </param>
    abstract ClearTaskSet : source:CommandSourceInfo -> initiatorTaskTag:ITT_T -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   LOGICAL UNIT RESET task management function request.
    ///   It resets specified logical unit.
    /// </summary>
    /// <param name="source">
    ///   Source information of received task management request PDU.
    /// </param>
    /// <param name="initiatorTaskTag">
    ///   Initiator Task Tag value of received task managedmt requst.
    /// </param>
    abstract LogicalUnitReset : source:CommandSourceInfo voption -> initiatorTaskTag:ITT_T voption -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   SCSI Command request.
    ///   It request processing of SCSI Command request PDU.
    /// </summary>
    /// <param name="source">
    ///   Source information of received SCSI command PDU.
    /// </param>
    /// <param name="command">
    ///   The SCSI Command PDU.
    /// </param>
    /// <param name="data">
    ///   A list of SCSI Data-Out PDUs.
    /// </param>
    abstract SCSICommand : source:CommandSourceInfo -> command:SCSICommandPDU -> data:SCSIDataOutPDU list -> unit


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get Logical Unit Reset status flag value.
    /// </summary>
    /// <returns>
    ///   If Logical Unit Reset is undergoing, returns true, otherwise returns false.
    /// </returns>
    abstract LUResetStatus : bool

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Obtain the total number of read bytes.
    /// </summary>
    /// <returns>
    ///   Aggregated results.
    /// </returns>
    abstract GetReadBytesCount : unit -> ResCountResult array

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Obtain the total number of written bytes.
    /// </summary>
    /// <returns>
    ///   Aggregated results.
    /// </returns>
    abstract GetWrittenBytesCount : unit -> ResCountResult array

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Obtain the tick count of read operation.
    /// </summary>
    /// <returns>
    ///   Aggregated results.
    /// </returns>
    abstract GetReadTickCount : unit -> ResCountResult array

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Obtain the tick count of write operation.
    /// </summary>
    /// <returns>
    ///   Aggregated results.
    /// </returns>
    abstract GetWriteTickCount : unit -> ResCountResult array

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Obtain current ACA status.
    /// </summary>
    abstract ACAStatus : struct( ITNexus * ScsiCmdStatCd * SenseKeyCd * ASCCd * bool ) voption

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get media object.
    /// </summary>
    abstract GetMedia : unit -> IMedia

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get used count of the task queue.
    /// </summary>
    /// <remarks>
    ///   Task count in the task queue may be over BDLU_MAX_TASKSET_SIZE.
    /// </remarks>
    abstract GetTaskQueueUsage : TSIH_T -> int



/// <summary>
///   Interface of login negociator object.
///   Login negociator is created by StatusMaster component when the initiator establish a new connection.
///   And sends/receives login request/response PDUs for negociating session or connection parameters.
///   If login sequence is successfy finished, login negociator creates Connection object and regists to the session component.
/// </summary>
type ILoginNegociator =
    inherit IComponent
    
    // ------------------------------------------------------------------------
    /// <summary>
    ///   Start to process iSCSI request
    ///   In this method, iSCSI login phase is started.   
    ///   ( connection status is transition to S3:XPT_UP state ).
    /// </summary>
    /// <param name="runSync">
    ///   If runSync is true, connection processing perform syncronously.
    ///   And all exceptions are thrown to caller method.
    ///   This mode are used in unit test only.
    ///   If runSync is false, connection processing is perfomed asyncronously.
    ///   Exceptions are not thrown caller method and in this case the connection AppDomain is unloaded.
    /// </param>
    /// <returns>
    ///   Always true.
    /// </returns>
    abstract Start : runSync : bool -> bool

/// <summary>
///   Interface of connection object.
///   Connection holds the TCP connection and sends/receives iSCSI PDUs.
///   The instance of this class is created by Login negociator object when login phase was successfly finished.
/// </summary>
type IConnection =
    inherit IComponent

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get connected date time
    /// </summary>
    /// <returns>
    ///   DateTime object that represent establishment time of the connection.
    /// </returns>
    abstract ConnectedDate : DateTime

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the current effective connection only parameters.
    /// </summary>
    /// <returns>
    ///   The current effective connection only parameters.
    /// </returns>
    abstract CurrentParams : IscsiNegoParamCO

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get TSIH value of the session which this connection belongs.
    /// </summary>
    abstract TSIH : TSIH_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get CID value of this connection
    /// </summary>
    abstract CID : CID_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the next StatSN value to be used.
    /// </summary>
    abstract NextStatSN : STATSN_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get connection counter value of this connection
    /// </summary>
    abstract ConCounter : CONCNT_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get network portal index number of this connection.
    /// </summary>
    abstract NetPortIdx : NETPORTIDX_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get TCP connection local endpoint information.
    /// </summary>
    abstract LocalAddress : System.Net.IPEndPoint voption

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Request to close connection.
    /// </summary>
    abstract Close : unit -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Process all of the full feature phase requests.
    /// </summary>
    abstract StartFullFeaturePhase : unit -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send PDU to the initiator.
    /// </summary>
    /// <param name="pdu">
    ///   PDU that should be sent to the initiator.
    /// </param>
    abstract SendPDU : pdu : ILogicalPDU -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Resend PDU to the initiator.
    /// </summary>
    /// <param name="pdu">
    ///   PDU that should be resent to the initiator.
    /// </param>
    abstract ReSendPDU : pdu : ILogicalPDU -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Resend PDU for R-SNACK request to the initiator.
    /// </summary>
    /// <param name="pdu">
    ///   SCSI Response or SCSI Data-In PDU that generated for R-SNACK request.
    /// </param>
    abstract ReSendPDUForRSnack : pdu : ILogicalPDU -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Notify that connection parameter values are changed.
    /// </summary>
    /// <param name="argCOParams">
    ///   Updated parameter values.
    /// </param>
    /// <remarks>
    ///   If values are not changed, this notify is ignored.
    ///   There is no guarantee when the changes will take effect.
    ///   If the value can not be updated is changed, it is silently ignored.
    /// </remarks>
    abstract NotifyUpdateConnectionParameter :
            argCOParams:IscsiNegoParamCO ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Notify to delete R2T PDU.
    /// </summary>
    /// <param name="itt">
    ///   ITT of R2T PDU which all of Data-Out PDU were received.
    /// </param>
    /// <param name="datasn">
    ///   DataSN of R2T PDU which all of Data-Out PDU were received.
    /// </param>
    abstract NotifyR2TSatisfied :
            itt:ITT_T ->
            datasn:DATASN_T ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Notify to delete acknowledged Data-In PDU by Data-Ack SNACK request.
    /// </summary>
    /// <param name="ttt">
    ///   TTT value that is transfered to the initiator in SCSI Data-In PDU that has 1 in A bit.
    /// </param>
    /// <param name="lun">
    ///   LUN value that is transfered to the initiator in SCSI Data-In PDU that has 1 in A bit.
    /// </param>
    /// <param name="begrun">
    ///   next expected DataSN value.
    /// </param>
    abstract NotifyDataAck :
            ttt:TTT_T ->
            lun:LUN_T ->
            begrun:DATASN_T ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get Data-In PDUs or R2T PDUs for resend.
    /// </summary>
    /// <param name="itt">
    ///   ITT of SCSI command that needs data resending.
    /// </param>
    /// <param name="begrun">
    ///   DataSN or R2TSN value.
    /// </param>
    /// <param name="runlength">
    ///   Requested PDUs count for resent.
    /// </param>
    abstract GetSentDataInPDUForSNACK :
            itt:ITT_T ->
            begrun:DATASN_T ->
            runlength : uint32 ->
            ILogicalPDU[]

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get SCSI Response PDUs for resend.
    /// </summary>
    /// <param name="begrun">
    ///   Specified BegRun value by SNACK PDU.
    /// </param>
    /// <param name="runlength">
    ///   Specified RunLength value by SNACK PDU.
    /// </param>
    abstract GetSentResponsePDUForSNACK :
            begrun:STATSN_T ->
            runlength:uint32 ->
            ILogicalPDU[]

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get SCSI Response PDU and Data-In PDUs for R-SNACK
    /// </summary>
    /// <param name="itt">
    ///   Initiator Task Tag
    /// </param>
    abstract GetSentSCSIResponsePDUForR_SNACK :
            itt:ITT_T ->
            ( SCSIDataInPDU[] * SCSIResponsePDU )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   R-SNACK request. If specified itt is missing returns false.
    /// </summary>
    /// <param name="itt">
    ///   Initiator Task Tag
    /// </param>
    /// <param name="cont">
    ///   Next procedure.
    /// </param>
    abstract R_SNACKRequest :
            itt:ITT_T ->
            cont:( unit -> unit ) ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Obtain the total number of received bytes.
    /// </summary>
    /// <returns>
    ///   Aggregated results.
    /// </returns>
    abstract GetReceiveBytesCount : unit -> ResCountResult array

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Obtain the total number of sent bytes.
    /// </summary>
    /// <returns>
    ///   Aggregated results.
    /// </returns>
    abstract GetSentBytesCount : unit -> ResCountResult array


/// <summary>
///   Interface of Session component.
///   Session component represents iSCSI session instance.
///   Session component manages connections belongings to the session and send/received PDUs.
/// </summary>
type ISession =
    inherit IComponent

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get created date time
    /// </summary>
    /// <returns>
    ///   DateTime object that represent establishment time of the session.
    /// </returns>
    abstract CreateDate : DateTime

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get parameters of this session.
    /// </summary>
    /// <returns>
    ///   Currentry effective session wide parameters in this session.
    /// </returns>
    abstract SessionParameter : IscsiNegoParamSW

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get TSIH value of this session.
    /// </summary>
    /// <returns>
    ///   The TSIH value.
    /// </returns>
    abstract TSIH : TSIH_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get I_T Nexus value of this session.
    /// </summary>
    /// <returns>
    ///   The I_T Nexus value.
    /// </returns>
    abstract I_TNexus : ITNexus


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get next TTT value.
    /// </summary>
    abstract NextTTT : TTT_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Check specified CID is exist or not.
    /// </summary>
    /// <param name="cid">
    ///   Connection ID that should be confirmed.
    /// </param>
    /// <returns>
    ///   If specified CID is in active, true is returned.
    /// </returns>
    abstract IsExistCID : cid : CID_T -> bool

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Add a new connection to a existing session.
    ///   If specified CID is already exist, this function failed and return false.
    /// </summary>
    /// <param name="sock">
    ///   Socket of the newly established connection.
    /// </param>
    /// <param name="conTime">
    ///   Connected date time.
    /// </param>
    /// <param name="newCID">
    ///   Connection ID of the new connection.
    /// </param>
    /// <param name="netPortIdx">
    ///   Network portal index number where this connection was established.
    /// </param>
    /// <param name="tpgt">
    ///   Target Portal Group Tag
    /// </param>
    /// <param name="iSCSIParamsCO">
    ///   Negociated connection only parameters.
    /// </param>
    /// <returns>
    ///   If succeed to add new connection, true is returned.
    ///   If specified CID is already existed, existing connection is not modified,
    ///   and new connection is not add to this session. This function return false.
    /// </returns>
    abstract AddNewConnection :
                sock:System.IO.Stream ->
                conTime:DateTime ->
                newCID:CID_T ->
                netPortIdx:NETPORTIDX_T ->
                tpgt:TPGT_T ->
                iSCSIParamsCO:IscsiNegoParamCO ->
                bool

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Drop existing connection and add a new connection in the session.
    ///   If specified CID is not exist, this function failed and return false.
    /// </summary>
    /// <param name="sock">
    ///   Socket of the newly established connection.
    /// </param>
    /// <param name="conTime">
    ///   Connected date time.
    /// </param>
    /// <param name="newCID">
    ///   Connection ID of the new connection.
    /// </param>
    /// <param name="netPortIdx">
    ///   Network portal index number where this connection was established.
    /// </param>
    /// <param name="tpgt">
    ///   Target Portal Group Tag
    /// </param>
    /// <param name="iSCSIParamsCO">
    ///   Negociated connection only parameters.
    /// </param>
    /// <returns>
    ///   If succeed to add new connection, true is returned.
    ///   If specified CID is not existed, new connection is not add to this session.
    ///   And this function return false.
    /// </returns>
    abstract ReinstateConnection :
                sock:System.IO.Stream ->
                conTime:DateTime ->
                newCID:CID_T ->
                netPortIdx:NETPORTIDX_T ->
                tpgt:TPGT_T ->
                iSCSIParamsCO:IscsiNegoParamCO ->
                bool

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Remove closed connection from this session.
    /// </summary>
    /// <param name="cid">
    ///   CID value of closed connection.
    /// </param>
    /// <param name="concnt">
    ///   Connection counter value of closed connection.
    /// </param>
    abstract RemoveConnection : cid:CID_T -> concnt:CONCNT_T -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Push received PDUs to session component.
    ///   In the full feature phase, connection component received meny of PDUs,
    ///   and connection component pours received PDUs into session component,
    ///   instead of processing in connection component.
    /// </summary>
    /// <param name="conn">
    ///   The connection witch the PDU was received.
    /// </param>
    /// <param name="pdu">
    ///   Received PDU.
    /// </param>
    abstract PushReceivedPDU : conn:IConnection -> pdu:ILogicalPDU -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Update MaxCmdSN and get current ExpCmdSN and MaxCmdSN value.
    /// </summary>
    /// <returns>
    ///   Pair of ExpCmdSN and MaxCmdSN values.
    /// </returns>
    abstract UpdateMaxCmdSN : unit -> struct( CMDSN_T * CMDSN_T )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the interface of specified connection.
    /// </summary>
    /// <param name="cid">
    ///   CID of connection wanted to search.
    /// </param>
    /// <param name="counter">
    ///   Counter of connection wanted to search.
    /// </param>
    /// <returns>
    ///   interface of the connection.
    ///   If specified connection is not already lived, returned None.
    /// </returns>
    abstract GetConnection : cid:CID_T -> counter:CONCNT_T -> IConnection voption

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get an array of the connections.
    /// </summary>
    /// <returns>
    ///   An array of interface of the connections.
    /// </returns>
    abstract GetAllConnections : unit -> IConnection array

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the protocol service interface of SCSI Task Router.
    /// </summary>
    /// <returns>
    ///   The SCSI Task router object in this session..
    /// </returns>
    abstract SCSITaskRouter : IProtocolService

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Check if the session is still alive.
    /// </summary>
    /// <returns>
    ///   If this session is alive, it returns true.
    /// </returns>
    abstract IsAlive : bool

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Destroy all of the object in this session.
    /// </summary>
    abstract DestroySession : unit -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send response data to the initiator. 
    /// </summary>
    /// <param name="reqCmdPDU">
    ///   SCSI Command PDU that requests SCSI command.
    /// </param>
    /// <param name="cid">
    ///   CID of connection that connecting to the initiator.
    /// </param>
    /// <param name="counter">
    ///   Connection counter value of connection that connecting to the initiator.
    /// </param>
    /// <param name="recvDataLength">
    ///   Received output data length, transfered by the SCSI command specified by reqCmdPDU.
    /// </param>
    /// <param name="argRespCode">
    ///   iSCSI responce code that will be transferd to the initiator.
    /// </param>
    /// <param name="argStatCode">
    ///   SCSI status code that will be transferd to the initiator.
    /// </param>
    /// <param name="resData">
    ///   All of response data.
    ///   This data may include mode than the receive buffer in the initiator
    ///   specified by allocationLength parameter.
    /// </param>
    /// <param name="senseData">
    ///   Sense data that will be transfered to the initiator.
    /// </param>
    /// <param name="allocationLength">
    ///   Receive buffer size in the initiator, that is specify by SCSI command.
    /// </param>
    /// <param name="needResponseFence">
    ///   Specify the type of lock based on ResponseFence.
    /// </param>
    /// <remarks>
    ///   This function run in asyncnously.
    ///   In sending process, if unknown error is occurred, session recovery is executed.
    /// </remarks>
    abstract SendSCSIResponse :
            reqCmdPDU:SCSICommandPDU ->
            cid:CID_T ->
            counter:CONCNT_T ->
            recvDataLength:uint32 ->
            argRespCode:iScsiSvcRespCd ->
            argStatCode:ScsiCmdStatCd ->
            senseData:PooledBuffer ->
            resData:PooledBuffer ->
            allocationLength:uint32 ->
            needResponseFence:ResponseFenceNeedsFlag ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send reject PDU to the initiator with ILogicalPDU data.
    /// </summary>
    /// <param name="cid">
    ///   CID of connection that connecting to the initiator.
    /// </param>
    /// <param name="counter">
    ///   Connection counter value of connection that connecting to the initiator.
    /// </param>
    /// <param name="pdu">
    ///   The received PDU that is rejected.
    /// </param>
    /// <param name="argReason">
    ///   Reason code.
    /// </param>
    abstract RejectPDUByLogi :
            cid:CID_T ->
            counter:CONCNT_T ->
            pdu:ILogicalPDU ->
            argReason:RejectReasonCd ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send reject PDU to the initiator with header bytes data..
    /// </summary>
    /// <param name="cid">
    ///   CID of connection that connecting to the initiator.
    /// </param>
    /// <param name="counter">
    ///   Connection counter value of connection that connecting to the initiator.
    /// </param>
    /// <param name="header">
    ///   Header bytes data of rejected PDU that is sent to the initiator.
    /// </param>
    /// <param name="argReason">
    ///   Reason code.
    /// </param>
    abstract RejectPDUByHeader :
            cid:CID_T ->
            counter:CONCNT_T ->
            header:byte[] ->
            argReason:RejectReasonCd ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send response PDU other than SCSI response, TMF or reject PDU.
    /// </summary>
    /// <param name="cid">
    ///   CID of connection that connecting to the initiator.
    /// </param>
    /// <param name="counter">
    ///   Connection counter value of connection that connecting to the initiator.
    /// </param>
    /// <param name="pdu">
    ///   Response PDU that is sent to initiator.
    /// </param>
    abstract SendOtherResponsePDU :
            cid:CID_T ->
            counter:CONCNT_T ->
            pdu:ILogicalPDU ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Resend PDU with response fence.
    /// </summary>
    /// <param name="cid">
    ///   CID of connection that connecting to the initiator.
    /// </param>
    /// <param name="counter">
    ///   Connection counter value of connection that connecting to the initiator.
    /// </param>
    /// <param name="pdu">
    ///   Response PDU that is resent to initiator.
    /// </param>
    abstract ResendPDU :
            cid:CID_T ->
            counter:CONCNT_T ->
            pdu:ILogicalPDU ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Resend PDU for R-SNACK request with response fence.
    /// </summary>
    /// <param name="cid">
    ///   CID of connection that connecting to the initiator.
    /// </param>
    /// <param name="counter">
    ///   Connection counter value of connection that connecting to the initiator.
    /// </param>
    /// <param name="pdu">
    ///   SCSI Response or SCSI Data-In PDU that generated by R-SNACK request.
    /// </param>
    abstract ResendPDUForRSnack :
            cid:CID_T ->
            counter:CONCNT_T ->
            pdu:ILogicalPDU ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Notice that session parameter values are changed.
    /// </summary>
    /// <param name="argSWParams">
    ///   Updated parameter values.
    /// </param>
    /// <remarks>
    ///   If values are not changed, this notify is ignored.
    ///   There is no guarantee when the changes will take effect.
    ///   If the value can not be updated is changed, it is silently ignored.
    /// </remarks>
    abstract NoticeUpdateSessionParameter :
            argSWParams:IscsiNegoParamSW ->
            unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Unlock response fence.
    /// </summary>
    /// <param name="mode">
    ///   lock mode.
    /// </param>
    abstract NoticeUnlockResponseFence :
            mode:ResponseFenceNeedsFlag ->
            unit


/// <summary>
///   Interface of iSCSI Network Portal component.
///   iSCSI Network Portal component has one TCP server port, and create TCP connection
///   when initiator established a connection.
/// </summary>
type IPort =
    inherit IComponent

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Start to wait connection.
    ///   This method creates a thread and start to wait incoming TCP connection, 
    ///   and return to calling procedure immediatly.
    /// </summary>
    /// <returns>
    ///   It returns true, if creation of TCP server port is succeed.
    /// </returns>
    abstract Start : unit -> bool

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get NetworkPortalInfo that is used to create this IscsiTCPSvPort object.
    /// </summary>
    /// <returns>
    ///   NetworkPortalInfo
    /// </returns>
    abstract NetworkPortal : TargetDeviceConf.T_NetworkPortal


/// <summary>
///   Interface of Configuration Master component.
///   Configuration Master has the information of static system configuration.
///   Instance of this component is always only one. 
///   All of the other components must refer to this unique instance.
///   If an exception or error occurs in this component and unloaded Master AppDomain,
///   Haruka process must be terminated.
/// </summary>
type IConfiguration =
    inherit IComponent

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Export network portal information.
    /// </summary>
    /// <returns>
    ///   All of information of network portal.
    /// </returns>
    abstract GetNetworkPortal : unit -> TargetDeviceConf.T_NetworkPortal list

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Export target group IDs
    /// </summary>
    /// <returns>
    ///   All of loaded target group IDs.
    /// </returns>
    abstract GetTargetGroupID : unit -> TGID_T[]

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Export target group IDs
    /// </summary>
    /// <param name="id">
    ///   Target group ID.
    /// </param>
    /// <returns>
    ///   target group configuration.
    /// </returns>
    abstract GetTargetGroupConf : id:TGID_T -> TargetGroupConf.T_TargetGroup option

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Export all of target group configuration
    /// </summary>
    /// <returns>
    ///   target group configuration.
    /// </returns>
    abstract GetAllTargetGroupConf : unit -> ( TargetGroupConf.T_TargetGroup * IKiller ) []

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Unload specified target group configuration.
    /// </summary>
    /// <param name="id">
    ///   Target group ID.
    /// </param>
    /// <remarks>
    ///   If specified target group ID is missing, unload will success.
    /// </remarks>
    abstract UnloadTargetGroup : id:TGID_T -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Load specified target group configuration.
    /// </summary>
    /// <param name="id">
    ///   Target group ID.
    /// </param>
    /// <remarks>
    ///   Configuration file to load is selected by file name.
    ///   If failed to load, current loaded configuration is not changed.
    /// </remarks>
    abstract LoadTargetGroup : id:TGID_T -> bool

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get default log parameter values.
    /// </summary>
    /// <remarks>
    ///   Combination of soft limit, hard limit and log level values.
    /// </remarks>
    abstract GetDefaultLogParameters : unit -> struct ( uint32 * uint32 * LogLevel )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Export iSCSI connection only negotiable parameters.
    ///   This is default value that used as source of negotiation.
    /// </summary>
    /// <returns>
    ///   iSCSI connection only parameters configured in system configuration.
    /// </returns>
    abstract IscsiNegoParamCO : IscsiNegoParamCO

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Export iSCSI session wide negotiable parameters.
    ///   This is default value that used as source of negotiation.
    /// </summary>
    /// <returns>
    ///   iSCSI session wide parameters configured in system configuration.
    /// </returns>
    abstract IscsiNegoParamSW : IscsiNegoParamSW

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Export configuered target device name.
    /// </summary>
    abstract DeviceName : string

/// <summary>
///   Interface of Status Master object.
///   Status Master manages all of the other object and dynamically changed
///   status information.
///   Instance of this component is always only one. 
///   All of the other components must refer to this unique instance.
///   If an exception or error occurs in this object, Haruka process must be terminated.
/// </summary>
type IStatus =
    inherit IComponent

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Export network portal information.
    /// </summary>
    /// <returns>
    ///   All of information of network portal.
    /// </returns>
    abstract GetNetworkPortal : unit -> TargetDeviceConf.T_NetworkPortal list

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the target group configurations that are currently available for login.
    /// </summary>
    /// <returns>
    ///   Currently available target group configuration.
    /// </returns>
    abstract GetActiveTargetGroup : unit -> TargetGroupConf.T_TargetGroup list

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the target configurations that are currently available for login.
    /// </summary>
    /// <returns>
    ///   Currently available target node configuration.
    /// </returns>
    abstract GetActiveTarget : unit -> TargetGroupConf.T_Target list

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get the target configurations that can access specified LUN.
    /// </summary>
    /// <param name="lun">
    ///   LUN.
    /// </param>
    /// <returns>
    ///   Currently available target node configuration.
    /// </returns>
    /// <remarks>
    ///   This method searches from the currently loaded target.
    ///   Loaded but inactive targets are also included in search results.
    /// </remarks>
    abstract GetTargetFromLUN : lun:LUN_T -> TargetGroupConf.T_Target list


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Export iSCSI connection only negotiable parameters.
    ///   This is default value that used as source of negotiation.
    /// </summary>
    /// <returns>
    ///   iSCSI connection only parameters configured in system configuration.
    /// </returns>
    abstract IscsiNegoParamCO : IscsiNegoParamCO

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Export iSCSI session wide negotiable parameters.
    ///   This is default value that used as source of negotiation.
    /// </summary>
    /// <returns>
    ///   iSCSI session wide parameters configured in system configuration.
    /// </returns>
    abstract IscsiNegoParamSW : IscsiNegoParamSW


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Create LoginNegociator AppDomain and LoginNegociator component.
    ///   When a TCP connection is established, port components requests to create 
    ///   a LoginNegociator component.
    /// </summary>
    /// <param name="sock">
    ///   Socket that passed to created LoginNegociator object.
    /// </param>
    /// <param name="conTime">
    ///   Connected date time.
    /// </param>
    /// <param name="targetPortalGroupTag">
    ///   Target portal group tag of TCP server port that is TCP connection is established.
    ///   Haruka not support multiple target portal group, so this value must be always 0.
    /// </param>
    /// <param name="netPortIdx">
    ///   Network portal index number where the connection was established.
    /// </param>
    abstract CreateLoginNegociator : sock:System.Net.Sockets.NetworkStream -> conTime:DateTime -> targetPortalGroupTag:TPGT_T -> netPortIdx:NETPORTIDX_T -> ILoginNegociator

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Search a lived session (TSIH) from I_T Nexus Identifier
    ///   (If specified I_T Nexus Identifier is not exist, returned TSIH is 0.)
    /// </summary>
    /// <param name="argI_TNexus">
    ///   I_T Next Identifier
    /// </param>
    /// <returns>
    ///   TSIH, if not cursponding to specified I_t Next identifier, 0.
    /// </returns>
    abstract GetTSIH : argI_TNexus:ITNexus -> TSIH_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Generate new TSIH value.
    /// </summary>
    /// <returns>
    ///   New TSIH value.
    /// </returns>
    abstract GenNewTSIH : unit -> TSIH_T

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get Session interface from TSIH
    /// </summary>
    /// <param name="tsih">
    ///   TSIH
    /// </param>
    /// <returns>
    ///   Interface of session component, that has specified TSIH.
    /// </returns>
    abstract GetSession : tsih:TSIH_T -> ISession voption

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get I_T Nexus that can access to specified LUN.
    /// </summary>
    /// <param name="lun">
    ///   LUN.
    /// </param>
    abstract GetITNexusFromLUN : lun:LUN_T -> ITNexus[]

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Create a new session, return a new TSIH
    ///   If specified I_T Nexus Identifier is already exist, this function failed and return 0.
    /// </summary>
    /// <param name="argTSIH">
    ///   TSIH value of new session.
    /// </param>
    /// <param name="argI_TNexus">
    ///   I_T Nexus value of new session.
    /// </param>
    /// <param name="sessionParameter">
    ///   Negotiated parameter, that used in newly created session.
    /// </param>
    /// <param name="newCmdSN">
    ///   CmdSN that used in newly created session.
    ///   This value is specified in Login request PDU sended from initiator.
    /// </param>
    /// <returns>
    ///   TSIH of newly created session. Or, if it failed to create session, return 0.
    /// </returns>
    abstract CreateNewSession :
        argI_TNexus:ITNexus ->
        argTSIH : TSIH_T ->
        sessionParameter:IscsiNegoParamSW ->
        newCmdSN : CMDSN_T ->
        ISession voption
(*
    // ------------------------------------------------------------------------
    /// <summary>
    ///   Drop existing session and create a new session, return a new TSIH
    ///   If specified I_T Nexus Identifier is not exist, this function failed and return 0.
    /// </summary>
    /// <param name="argI_TNexus">
    ///   I_T Next identifier of drop and create session.
    /// </param>
    /// <param name="tsih">
    ///   TSIH of drop and create session.
    /// </param>
    /// <param name="sessionParameter">
    ///   Negotiated parameter, that used in newly created session.
    /// </param>
    /// <param name="newCmdSN">
    ///   CmdSN that used in newly created session.
    ///   This value is specified in Login request PDU sended from initiator.
    /// </param>
    /// <returns>
    ///   TSIH of newly create session. Or, if it failed to create session, return 0.
    /// </returns>
    abstract ReinstateSession :
        argI_TNexus:ITNexus ->
        tsih:TSIH_T ->
        sessionParameter:IscsiNegoParamSW ->
        newCmdSN : CMDSN_T ->
        TSIH_T
*)
    // ------------------------------------------------------------------------
    /// <summary>
    ///  Remove terminated session object.
    /// </summary>
    /// <param name="tsih">
    ///  TSIH that will be removed.
    /// </param>
    abstract RemoveSession : tsih:TSIH_T -> unit


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get LU object that imprements ILU interface if LU object specified by LUN is exist,
    ///   or creates new LU object and returns that LU object.
    /// </summary>
    /// <param name="argLUN">
    ///   LUN of LU.
    /// </param>
    /// <returns>
    ///   LU object. If unknown LUN is specified, returns None.
    /// </returns>
    abstract GetLU :
        argLUN:LUN_T ->
        ILU voption

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Create Media object.
    /// </summary>
    /// <param name="confInfo">
    ///   Configuration information that pass to created media object.
    /// </param>
    /// <param name="lun">
    ///   LUN value of LU that host created media object.
    /// </param>
    /// <param name="killer">
    ///   Killer object that apply create media object.
    /// </param>
    /// <returns>
    ///   Created media object.
    /// </returns>
    abstract CreateMedia :
        confInfo : TargetGroupConf.T_MEDIA ->
        lun : LUN_T ->
        killer : IKiller ->
        IMedia

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Notify Logical Unit Reset.
    ///   When LUReset is occured, resetted LU calls this method to notify this reset.
    /// </summary>
    /// <param name="lun">
    ///   LUN of LU that is occured LUReset.
    /// </param>
    /// <param name="lu">
    ///   Interface of LU object that is occured LUReset.
    /// </param>
    abstract NotifyLUReset : lun : LUN_T -> lu : ILU -> unit

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Process parent control request.
    /// </summary>
    /// <remarks>
    ///   This method would not respond where continuing iSCSI services.
    /// </remarks>
    abstract ProcessControlRequest : unit -> System.Threading.Tasks.Task

    // ------------------------------------------------------------------------
    /// Start iSCSI service
    abstract Start : unit -> unit 

