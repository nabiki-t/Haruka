//=============================================================================
// Haruka Software Storage.
// PDUInterface.fs : Defines the interfaces of the PDU data object.
// PDU data is generated in connection component and passed to session component,
// to process iSCSI task in session or more deep elements.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Runtime.CompilerServices

open Haruka.Constants

//=============================================================================
// Interface definition

/// Common interface of logical PDU records
type ILogicalPDU =
    /// Get immidiate flag
    abstract Immidiate : bool

    /// Get Opcode value
    abstract Opcode : OpcodeCd

    /// Get terminate flag
    abstract Final : bool

    /// Get InitiatorTaskTag property
    abstract InitiatorTaskTag : ITT_T

    /// This PDU has CmdSN field or not
    abstract HasCmdSN : bool

    /// Get CmdSN property
    abstract CmdSN : CMDSN_T

    /// This PDU has ExpStatSN field or not
    abstract HasExpStatSN : bool

    /// Get ExpStatSN property
    abstract ExpStatSN : STATSN_T

    /// Update target variable values
    abstract UpdateTargetValues : STATSN_T -> CMDSN_T -> CMDSN_T -> ILogicalPDU

    /// Update target variable values for resend
    abstract UpdateTargetValuesForResend : STATSN_T -> CMDSN_T -> CMDSN_T -> ILogicalPDU

    /// After this PDU is sent to the initiator, the target must increment StatSN value, or not.
    /// If this PDU is output data, this method always return false.
    abstract NeedIncrementStatSN : unit -> bool

    /// Whether response fence is required
    abstract NeedResponseFence : ResponseFenceNeedsFlag

    /// Number of bytes when this PDU will be sent over the network.
    /// If this PDU created for sending, this value has None.
    abstract ByteCount : uint32 voption


/// <summary>
///  SCSI Command PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the initiator to the target.
/// </remarks>
[<NoComparison>]
type SCSICommandPDU =
    {
        /// Immidiate flag. This field is set to true, if I bit is 1.
        I : bool;
        /// Final flag. This field is set to true, if F bit is 1.
        F : bool;
        /// Read flag. This field is set to true, if R bit is 1.
        R : bool;
        /// Write flag. This field is set to true, if W bit is 1.
        W : bool;
        /// Attribute field value.
        ATTR : TaskATTRCd;
        /// LUN field value.
        LUN : LUN_T;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// Expected Data Transfer Length field value.
        ExpectedDataTransferLength : uint32;
        /// CmdSN field value.
        CmdSN : CMDSN_T;
        /// ExpStatSN field value.
        ExpStatSN : STATSN_T;
        /// SCSI Command Descriptor Block(CDB) field value.
        /// This value includes padding data.
        ScsiCDB : byte[];
        /// Data segment value. This value doues not include padding bytes.
        /// In PDU.Receive, this buffer is allocated by ArrayPool.
        /// In BlockDeviceLU.NotifyTerminateTask or BlockDeviceLU.NotifyTerminateTaskWithException, 
        /// this buffer will be returned to the ArrayPool when it is no longer needed.
        DataSegment : PooledBuffer;
        /// Bidirectional Expected Read Data Length field value in AHS.
        BidirectionalExpectedReadDataLength : uint32;
        /// Received byte count
        ByteCount : uint32;
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member this.Immidiate : bool =
            this.I

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.SCSI_COMMAND

        // Get terminate flag
        member this.Final : bool =
            this.F

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            true

        // Get CmdSN property
        member this.CmdSN : CMDSN_T =
            this.CmdSN

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            true

        // Get ExpStatSN property
        member this.ExpStatSN : STATSN_T =
            this.ExpStatSN

        // Update target variable values
        // (There are no target values, so it's return the same SCSICommandPDU as is. )
        member this.UpdateTargetValues ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Update target variable values for resend
        // (There are no target values, so it's return the same SCSICommandPDU as is. )
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU


        // Target does not sent this PDU to the initiator.
        member _.NeedIncrementStatSN() =
            false

        // Target does not sent this PDU to the initiator.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.Irrelevant

        // Received byte count
        member this.ByteCount : uint32 voption =
            ValueSome this.ByteCount

/// <summary>
///  SCSI Response PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the target to the initiator.
/// </remarks>
[<NoComparison>]
type SCSIResponsePDU =
    {
        /// Bidirectional Read Residual Overflow. This field is set to true, if o bit is 1.
        o : bool;
        /// Bidirectional Read Residual Underflow. This field is set to true, if u bit is 1.
        u : bool;
        /// Residual Overflow. This field is set to true, if O bit is 1.
        O : bool;
        /// Residual Underflow. This field is set to true, if U bit is 1.
        U : bool;
        /// Response field value. iSCSI service response code.
        Response : iScsiSvcRespCd;
        /// Status field value. SAM2 SCSI command status.
        Status : ScsiCmdStatCd;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// SNACKTag field value.
        SNACKTag : SNACKTAG_T;
        /// StatSN field value.
        StatSN : STATSN_T;
        /// ExpCmdSN field value.
        ExpCmdSN : CMDSN_T;
        /// MaxCmdSN field value.
        MaxCmdSN : CMDSN_T;
        /// ExpDataSN field value. 
        ExpDataSN : DATASN_T;
        /// Bidirectional Expected Read Data Length field value.
        BidirectionalReadResidualCount : uint32;
        /// Residual Count field value.
        ResidualCount : uint32;
        /// Sense Length field in data segment value.
        SenseLength : uint16;
        /// Sense Data field in data segment value.
        /// Referencing a part of DataInBuffer.
        SenseData : ArraySegment<byte>;
        /// Response Data field in data segment value.
        /// Referencing a part of DataInBuffer.
        ResponseData : ArraySegment<byte>;
        /// Whether response fence is required
        ResponseFence : ResponseFenceNeedsFlag

        /// A buffer that holds Sense Data or Response Data.
        /// The buffer allocated by ArrayPool is given as an argument to IProtocolService.SendSCSIResponse.
        /// The same instance of a buffer may be referenced by multiple PDUs. 
        /// When returning a buffer to the ArrayPool, the same buffer must never be returned more than once.
        DataInBuffer : PooledBuffer;

        /// The LUN value set in the LUN field of the SCSI Request PDU that caused the SCSI Response PDU to be generated.
        /// The SCSI Response PDU does not have a field for setting the LUN.
        /// However, since the LUN value is required when re-dividing the Data-In PDU in response to an R-Data SNACK request, the value is retained.
        /// If not used, set to 0.
        LUN : LUN_T;
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member _.Immidiate : bool =
            false

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.SCSI_RES

        // Get terminate flag
        member _.Final : bool =
            true

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            false

        // Get CmdSN property
        member _.CmdSN : CMDSN_T =
            cmdsn_me.zero

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            false

        // Get ExpStatSN property
        member _.ExpStatSN : STATSN_T =
            statsn_me.zero

        // Update target variable values
        member this.UpdateTargetValues ( argStatSN : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            {
                this with
                    StatSN = argStatSN;
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // Update target variable values for resend
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            // To resend SCSI response PDU, StatSN value must not be not updated from original value.
            {
                this with
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // After this PDU is sent to the initiator, the target must increment StatSN value.
        member _.NeedIncrementStatSN() =
            true

        // Whether response fence is required
        member this.NeedResponseFence : ResponseFenceNeedsFlag =
            this.ResponseFence

        // Target does not receive this PDU from the initiator.
        member _.ByteCount : uint32 voption =
            ValueNone

/// <summary>
///  Task Management Function Request PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the initiator to the target.
/// </remarks>
type TaskManagementFunctionRequestPDU =
    {
        /// Immidiate flag. This field is set to true, if I bit is 1.
        I : bool;
        /// Function field value.
        Function : TaskMgrReqCd;
        /// LUN field value.
        LUN : LUN_T;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// Referenced Task Tag field value.
        ReferencedTaskTag : ITT_T;
        /// CmdSN field value.
        CmdSN : CMDSN_T;
        /// ExpStatSN field value.
        ExpStatSN : STATSN_T;
        /// RefCmdSN field value.
        RefCmdSN : CMDSN_T;
        /// ExpDataSN field value.
        ExpDataSN : DATASN_T;
        /// Received byte count
        ByteCount : uint32;
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member this.Immidiate : bool =
            this.I

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.SCSI_TASK_MGR_REQ

        // Get terminate flag
        member _.Final : bool =
            true

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            true

        // Get CmdSN property
        member this.CmdSN : CMDSN_T =
            this.CmdSN

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            true

        // Get ExpStatSN property
        member this.ExpStatSN : STATSN_T =
            this.ExpStatSN

        // Update target variable values
        // (There are no target values, so it's return the same Task Management Function Request PDU as is. )
        member this.UpdateTargetValues ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Update target variable values for resend
        // (There are no target values, so it's return the same Task Management Function Request PDU as is. )
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Target does not sent this PDU to the initiator.
        member _.NeedIncrementStatSN() = false

        // Target does not sent this PDU to the initiator.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.Irrelevant

        // Received byte count
        member this.ByteCount : uint32 voption =
            ValueSome this.ByteCount

/// <summary>
///  Task Management Function Response PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the target to the initiator.
/// </remarks>
type TaskManagementFunctionResponsePDU =
    {
        /// Response field value.
        Response : TaskMgrResCd;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// StatSN field value.
        StatSN : STATSN_T;
        /// ExpCmdSN field value.
        ExpCmdSN : CMDSN_T;
        /// MaxCmdSN field value.
        MaxCmdSN : CMDSN_T;
        /// Whether response fence is required
        ResponseFence : ResponseFenceNeedsFlag
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member _.Immidiate : bool =
            false

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.SCSI_TASK_MGR_RES

        // Get terminate flag
        member _.Final : bool =
            true

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            false

        // Get CmdSN property
        member _.CmdSN : CMDSN_T =
            cmdsn_me.zero

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            false

        // Get ExpStatSN property
        member _.ExpStatSN : STATSN_T =
            statsn_me.zero

        // Update target variable values
        member this.UpdateTargetValues ( argStatSN : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            {
                this with
                    StatSN = argStatSN;
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // Update target variable values for resend
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            // To resend Task Management Function Response PDU, StatSN value must not be not updated from original value.
            {
                this with
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // After this PDU is sent to the initiator, the target must increment StatSN value.
        member _.NeedIncrementStatSN() =
            true

        // Whether response fence is required
        member this.NeedResponseFence : ResponseFenceNeedsFlag =
            this.ResponseFence

        // Target does not receive this PDU from the initiator.
        member _.ByteCount : uint32 voption =
            ValueNone

/// <summary>
///  SCSI Data-Out PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the initiator to the target.
/// </remarks>
[<NoComparison>]
type SCSIDataOutPDU =
    {
        /// Final flag. This field is set to true, if F bit is 1.
        F : bool;
        /// LUN field value.
        LUN : LUN_T;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// Target Transfer Tag field value.
        TargetTransferTag : TTT_T
        /// ExpStatSN field value.
        ExpStatSN : STATSN_T;
        /// DataSN field value.
        DataSN : DATASN_T;
        /// BufferOffset field value.
        BufferOffset : uint32;
        /// DataSegment value. Padding bytes is not included.
        /// In PDU.Receive, this buffer is allocated by ArrayPool.
        /// In BlockDeviceLU.NotifyTerminateTask or BlockDeviceLU.NotifyTerminateTaskWithException, 
        /// this buffer will be returned to the ArrayPool when it is no longer needed.
        DataSegment : PooledBuffer;
        /// Received byte count
        ByteCount : uint32;
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member _.Immidiate : bool =
            false

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.SCSI_DATA_OUT

        // Get terminate flag
        member this.Final : bool =
            this.F

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            false

        // Get CmdSN property
        member _.CmdSN : CMDSN_T =
            cmdsn_me.zero

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            true

        // Get ExpStatSN property
        member this.ExpStatSN : STATSN_T =
            this.ExpStatSN

        // Update target variable values
        // (There are no target values, so it's return the same SCSI Data-Out PDU as is. )
        member this.UpdateTargetValues ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Update target variable values for resend
        // (There are no target values, so it's return the same SCSI Data-Out PDU as is. )
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Target does not sent this PDU to the initiator.
        member _.NeedIncrementStatSN() =
            false

        // Target does not sent this PDU to the initiator.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.Irrelevant

        // Received byte count
        member this.ByteCount : uint32 voption =
            ValueSome this.ByteCount


    /// <summary>
    ///  Append parameter data in some SCSI Data-Out PDUs to one bytes array.
    /// </summary>
    /// <param name="cmdPduData">
    ///  Data segment bytes array in SCSI Command PDU.
    /// </param>
    /// <param name="dout">
    ///  SCSI Data-Out PDUs list
    /// </param>
    /// <param name="maxlen">
    ///  Maximum length of result array.
    /// </param>
    /// <returns>
    ///  parameter data bytes array. The buffer is allocaled by ArrayPool. It must be returned to ArrayPool.
    /// </returns>
    static member AppendParamList ( cmdPduData : PooledBuffer ) ( dout : SCSIDataOutPDU list ) ( maxlen : int ) : PooledBuffer =
        if maxlen <= 0 then
            PooledBuffer.Empty
        else
            let seglist = 
                dout
                |> Seq.map ( fun itr -> struct ( itr.BufferOffset, itr.DataSegment ) )
                |> Seq.insertAt 0 struct ( 0u, cmdPduData )
                |> Seq.filter ( fun struct ( _, seg ) -> seg.Count > 0 )
                |> Seq.toArray

            if seglist.Length <= 0 then
                PooledBuffer.Empty
            else
                let paramlength =
                    let struct( wOffset, wDataSeg ) =
                        seglist
                        |> Array.maxBy( fun struct ( bOffset, dataSeg ) -> bOffset + (uint)dataSeg.Count )
                    min ( wOffset + ( uint wDataSeg.Count ) ) ( uint maxlen )
                let wv = PooledBuffer.Rent( int paramlength )

                for struct ( bOffset, dataSeg ) in seglist do
                    if bOffset < paramlength && dataSeg.Count > 0 then
                        let wcnt =  ( min ( bOffset + ( uint dataSeg.Count ) ) paramlength ) - bOffset
                        Array.blit dataSeg.Array 0 wv.Array ( int bOffset ) ( int wcnt )
                wv

/// <summary>
///  SCSI Data-In PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the target to the initiator.
/// </remarks>
[<NoComparison>]
type SCSIDataInPDU =
    {
        /// Final flag. This field is set to true, if F bit is 1.
        F : bool;
        /// Ackowledge flag. This field is set to true, if A bit is 1.
        A : bool;
        /// Residual Overflow. This field is set to true, if O bit is 1.
        O : bool;
        /// Residual Underflow. This field is set to true, if U bit is 1.
        U : bool;
        /// Status flag. This field is set to true, if S bit is 1.
        /// *** In Haruka, Data-In PDU with status is not supported. So, S bit must always be 0. ***
        S : bool;
        /// Status field value.
        Status : ScsiCmdStatCd;
        /// LUN field value.
        LUN : LUN_T;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// Target Transfer Tag field value.
        TargetTransferTag : TTT_T;
        /// StatSN field value.
        StatSN : STATSN_T;
        /// ExpCmdSN field value.
        ExpCmdSN : CMDSN_T;
        /// MaxCmdSN field value.
        MaxCmdSN : CMDSN_T;
        /// DataSN field value.
        DataSN : DATASN_T;
        /// BufferOffset field value.
        BufferOffset : uint32;
        /// ResidualCount field value.
        ResidualCount : uint32;
        /// DataSegment field value. Padding bytes is not included.
        /// The buffer allocated by ArrayPool is given as an argument to IProtocolService.SendSCSIResponse.
        /// The same instance of a buffer may be referenced by multiple PDUs. 
        /// When returning a buffer to the ArrayPool, the same buffer must never be returned more than once.
        DataSegment : ArraySegment<byte>;
        /// Whether response fence is required
        ResponseFence : ResponseFenceNeedsFlag
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member _.Immidiate : bool =
            false

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.SCSI_DATA_IN

        // Get terminate flag
        member this.Final : bool =
            this.F

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            false

        // Get CmdSN property
        member _.CmdSN : CMDSN_T =
            cmdsn_me.zero

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            false

        // Get ExpStatSN property
        member _.ExpStatSN : STATSN_T =
            statsn_me.zero

        // Update target variable values
        member this.UpdateTargetValues ( argStatSN : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            {
                this with
                    // StatSN field is enable when only S bit has 1.
                    StatSN = if this.S then argStatSN else statsn_me.zero;
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // Update target variable values for resend
        member this.UpdateTargetValuesForResend ( argStatSN : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            // To resend SCSI Data-In PDU, StatSN value must not be not updated from original value.
            {
                this with
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // If status values are in this PDU, target must increment StatSN value.
        member this.NeedIncrementStatSN() =
            this.S

        // Whether response fence is required
        member this.NeedResponseFence : ResponseFenceNeedsFlag =
            // If this PDU does not have status ( S bit equals 0 )
            // Target sends this PDU immediately independently of the response fence.
            if this.S then
                this.ResponseFence
            else
                ResponseFenceNeedsFlag.Immediately

        // Target does not receive this PDU from the initiator.
        member _.ByteCount : uint32 voption =
            ValueNone


/// <summary>
/// Ready To Transfer(R2T) PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the target to the initiator.
/// </remarks>
type R2TPDU =
    {
        /// LUN field value.
        LUN : LUN_T;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// Target Transfer Tag field value.
        TargetTransferTag : TTT_T;
        /// StatSN field value.
        StatSN : STATSN_T;
        /// ExpCmdSN field value.
        ExpCmdSN : CMDSN_T;
        /// MaxCmdSN field value.
        MaxCmdSN : CMDSN_T;
        /// R2TSN field value.
        R2TSN : DATASN_T;
        /// BufferOffset field value.
        BufferOffset : uint32;
        /// DesiredDataTransferLength field value.
        DesiredDataTransferLength : uint32;
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member _.Immidiate : bool =
            false

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.R2T

        // Get terminate flag
        member _.Final : bool =
            true

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            false

        // Get CmdSN property
        member _.CmdSN : CMDSN_T =
            cmdsn_me.zero

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            false

        // Get ExpStatSN property
        member _.ExpStatSN : STATSN_T =
            statsn_me.zero

        // Update target variable values
        member this.UpdateTargetValues ( argStatSN : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            {
                this with
                    StatSN = argStatSN;
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // Update target variable values for resend
        member this.UpdateTargetValuesForResend ( argStatSN : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            // To resend R2P PDU, StatSN must transfer current value.
            {
                this with
                    StatSN = argStatSN;
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // After this PDU is sent to initiator, target does not increment StatSN value.
        member _.NeedIncrementStatSN() =
            false

        // Target sends this PDU immediately independently of the response fence.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.Immediately

        // Target does not receive this PDU from the initiator.
        member _.ByteCount : uint32 voption =
            ValueNone


/// <summary>
/// Asyncronous message PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the target to the initiator.
/// </remarks>
type AsyncronousMessagePDU =
    {
        /// LUN field value.
        LUN : LUN_T;
        /// StatSN field value.
        StatSN : STATSN_T;
        /// ExpCmdSN field value.
        ExpCmdSN : CMDSN_T;
        /// MaxCmdSN field value.
        MaxCmdSN : CMDSN_T;
        /// AsyncEvent field value.
        AsyncEvent : AsyncEventCd;
        /// AsyncVCode field value.
        AsyncVCode : byte;
        /// Parameter1 field value. Usage of this field is specified by AsyncEvent field value.
        Parameter1 : uint16;
        /// Parameter2 field value. Usage of this field is specified by AsyncEvent field value.
        Parameter2 : uint16;
        /// Parameter3 field value. Usage of this field is specified by AsyncEvent field value.
        Parameter3 : uint16;
        /// SenseLength field value in data segment.
        SenseLength : uint16;
        /// SenseData field value in data segment.
        SenseData : byte[];
        /// ISCSIEventData field value in data segment.
        ISCSIEventData : byte[];
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member _.Immidiate : bool =
            false

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.ASYNC

        // Get terminate flag
        member _.Final : bool =
            true

        // Get InitiatorTaskTag property
        member _.InitiatorTaskTag : ITT_T =
            itt_me.fromPrim 0xFFFFFFFFu

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            false

        // Get CmdSN property
        member _.CmdSN : CMDSN_T =
            cmdsn_me.zero

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            false

        // Get ExpStatSN property
        member _.ExpStatSN : STATSN_T =
            statsn_me.zero

        // Update target variable values
        member this.UpdateTargetValues ( argStatSN : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            {
                this with
                    StatSN = argStatSN;
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // Update target variable values for resend
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            // To resend Asyncronous message PDU, StatSN value must not be not updated from original value.
            {
                this with
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // After this PDU is sent to the initiator, the target must increment StatSN value.
        member _.NeedIncrementStatSN() =
            true

        // Always response fence is not required.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.R_Mode

        // Target does not receive this PDU from the initiator.
        member _.ByteCount : uint32 voption =
            ValueNone


/// <summary>
///  Text request PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the initiator to the target.
/// </remarks>
[<NoComparison>]
type TextRequestPDU =
    {
        /// Immidiate flag. This field is set to true, if I bit is 1.
        I : bool;
        /// Final flag. This field is set to true, if F bit is 1.
        F : bool;
        /// Continue flag. This field is set to true, if C bit is 1.
        C : bool;
        /// LUN field value.
        LUN : LUN_T;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// Target Transfer Tag field value.
        TargetTransferTag : TTT_T;
        /// CmdSN field value.
        CmdSN : CMDSN_T;
        /// ExpStatSN field value.
        ExpStatSN : STATSN_T;
        /// Text Request data in data segment.
        TextRequest : byte[];
        /// Received byte count
        ByteCount : uint32;
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member this.Immidiate : bool =
            this.I

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.TEXT_REQ

        // Get terminate flag
        member this.Final : bool =
            this.F

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            true

        // Get CmdSN property
        member this.CmdSN : CMDSN_T =
            this.CmdSN

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            true

        // Get ExpStatSN property
        member this.ExpStatSN : STATSN_T =
            this.ExpStatSN

        // Update target variable values
        // (There are no target values, so it's return the same Text request PDU as is. )
        member this.UpdateTargetValues ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Update target variable values for resend
        // (There are no target values, so it's return the same Text request PDU as is. )
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Target does not sent this PDU to the initiator.
        member _.NeedIncrementStatSN() =
            false

        // Target does not sent this PDU to the initiator.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.Irrelevant

        // Received byte count
        member this.ByteCount : uint32 voption =
            ValueSome this.ByteCount


/// <summary>
/// Text response PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the target to the initiator.
/// </remarks>
type TextResponsePDU =
    {
        /// Final flag. This field is set to true, if F bit is 1.
        F : bool;
        /// Continue flag. This field is set to true, if C bit is 1.
        C : bool;
        /// LUN field value.
        LUN : LUN_T;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// Target Transfer Tag field value.
        TargetTransferTag : TTT_T;
        /// StatSN field value.
        StatSN : STATSN_T;
        /// ExpCmdSN field value.
        ExpCmdSN : CMDSN_T;
        /// MaxCmdSN field value.
        MaxCmdSN : CMDSN_T;
        /// Text Response data in data segment.
        TextResponse : byte[];
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member _.Immidiate : bool =
            false

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.TEXT_RES

        // Get terminate flag
        member this.Final : bool =
            this.F

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            false

        // Get CmdSN property
        member _.CmdSN : CMDSN_T =
            cmdsn_me.zero

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            false

        // Get ExpStatSN property
        member _.ExpStatSN : STATSN_T =
            statsn_me.zero

        // Update target variable values
        member this.UpdateTargetValues ( argStatSN : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            {
                this with
                    StatSN = argStatSN;
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // Update target variable values for resend
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            // To resend text response PDU, StatSN value must not be not updated from original value.
            {
                this with
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // After this PDU is sent to the initiator, the target must increment StatSN value.
        member _.NeedIncrementStatSN() =
            true

        // Always response fence is not required.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.R_Mode

        // Target does not receive this PDU from the initiator.
        member _.ByteCount : uint32 voption =
            ValueNone


/// <summary>
///  Login request PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the initiator to the target.
/// </remarks>
[<NoComparison>]
type LoginRequestPDU =
    {
        /// Transit flag. This field is set to true, if T bit is 1.
        T : bool;
        /// Continue flag. This field is set to true, if T bit is 1.
        C : bool;
        /// Current stage field value.
        CSG : LoginReqStateCd;
        /// Next stage field value.
        NSG : LoginReqStateCd;
        /// VersionMax field value.
        VersionMax : byte;
        /// VersionMin field value.
        VersionMin : byte;
        /// ISID field value.
        ISID : ISID_T;
        /// TSIH field value.
        TSIH : TSIH_T;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// CID ( Connection ID ) field value.
        CID : CID_T;
        /// CmdSN field value.
        CmdSN : CMDSN_T;
        /// ExpStatSN field value.
        ExpStatSN : STATSN_T;
        /// Text Request data in data segment.
        TextRequest : byte[];
        /// Received byte count
        ByteCount : uint32;
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member _.Immidiate : bool =
            true

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.LOGIN_REQ

        // Get terminate flag
        member this.Final : bool =
            this.T    /// Transit

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            true

        // Get CmdSN property
        member this.CmdSN : CMDSN_T =
            this.CmdSN

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            true

        // Get ExpStatSN property
        member this.ExpStatSN : STATSN_T =
            this.ExpStatSN

        // Update target variable values
        // (There are no target values, so it's return the same Login request PDU as is. )
        member this.UpdateTargetValues ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Update target variable values for resend
        // (There are no target values, so it's return the same Login request PDU as is. )
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Target does not sent this PDU to the initiator.
        member _.NeedIncrementStatSN() =
            false

        // Target does not sent this PDU to the initiator.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.Irrelevant

        // Received byte count
        member this.ByteCount : uint32 voption =
            ValueSome this.ByteCount

/// <summary>
/// Login response PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the target to the initiator.
/// </remarks>
type LoginResponsePDU =
    {
        /// Transit flag. This field is set to true, if T bit is 1.
        T : bool;   // Transit flag
        /// Continue flag. This field is set to true, if T bit is 1.
        C : bool;
        /// Current stage field value.
        CSG : LoginReqStateCd;
        /// Next stage field value.
        NSG : LoginReqStateCd;
        /// VersionMax field value.
        VersionMax : byte;
        /// VersionActive field value.
        VersionActive : byte;
        /// ISID field value.
        ISID : ISID_T;
        /// TSIH field value.
        TSIH : TSIH_T;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// StatSN field value.
        StatSN : STATSN_T;
        /// ExpCmdSN field value.
        ExpCmdSN : CMDSN_T;
        /// MaxCmdSN field value.
        MaxCmdSN : CMDSN_T;
        /// Status-Class and Status-Detail field value.
        Status : LoginResStatCd;
        /// Text Response data in data segment.
        TextResponse : byte[];
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member _.Immidiate : bool =
            false

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.LOGIN_RES

        // Get terminate flag
        member this.Final : bool =
            this.T  // Transit

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            false

        // Get CmdSN property
        member _.CmdSN : CMDSN_T =
            cmdsn_me.zero

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            false

        // Get ExpStatSN property
        member _.ExpStatSN : STATSN_T =
            statsn_me.zero

        // Update target variable values
        member this.UpdateTargetValues ( argStatSN : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            {
                this with
                    StatSN = argStatSN;
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // Update target variable values for resend
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            // To resend Login response PDU, StatSN value must not be not updated from original value.
            {
                this with
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // After this PDU is sent to the initiator, the target must increment StatSN value.
        member _.NeedIncrementStatSN() =
            true

        // Always response fence is not required.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.R_Mode

        // Target does not receive this PDU from the initiator.
        member _.ByteCount : uint32 voption =
            ValueNone

/// <summary>
///  Logout request PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the initiator to the target.
/// </remarks>
type LogoutRequestPDU =
    {
        /// Immidiate flag. This field is set to true, if I bit is 1.
        I : bool;
        /// ReasonCode field value.
        ReasonCode : LogoutReqReasonCd;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// CID ( Connection ID ) field value.
        CID : CID_T;
        /// CmdSN field value.
        CmdSN : CMDSN_T;
        /// ExpStatSN field value.
        ExpStatSN : STATSN_T;
        /// Received byte count
        ByteCount : uint32;
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member this.Immidiate : bool =
            this.I

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.LOGOUT_REQ

        // Get terminate flag
        member _.Final : bool =
            true

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            true

        // Get CmdSN property
        member this.CmdSN : CMDSN_T =
            this.CmdSN

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            true

        // Get ExpStatSN property
        member this.ExpStatSN : STATSN_T =
            this.ExpStatSN

        // Update target variable values
        // (There are no target values, so it's return the same Logout request PDU as is. )
        member this.UpdateTargetValues ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Update target variable values for resend
        // (There are no target values, so it's return the same Logout request PDU as is. )
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Target does not sent this PDU to the initiator.
        member _.NeedIncrementStatSN() =
            false

        // Target does not sent this PDU to the initiator.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.Irrelevant

        // Received byte count
        member this.ByteCount : uint32 voption =
            ValueSome this.ByteCount

/// <summary>
///  Logout response PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the target to the initiator.
/// </remarks>
type LogoutResponsePDU =
    {
        /// Response field value.
        Response : LogoutResCd;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// StatSN field value.
        StatSN : STATSN_T;
        /// ExpCmdSN field value.
        ExpCmdSN : CMDSN_T;
        /// MaxCmdSN field value.
        MaxCmdSN : CMDSN_T;
        /// Time2Wait field value.
        Time2Wait : uint16;
        /// Time2Retain field value.
        Time2Retain : uint16;
        /// Close this connection or not.
        CloseAllegiantConnection : bool;
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member _.Immidiate : bool =
            false

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.LOGOUT_RES

        // Get terminate flag
        member _.Final : bool =
            true

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            false

        // Get CmdSN property
        member _.CmdSN : CMDSN_T =
            cmdsn_me.zero

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            false

        // Get ExpStatSN property
        member _.ExpStatSN : STATSN_T =
            statsn_me.zero

        // Update target variable values
        member this.UpdateTargetValues ( argStatSN : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            {
                this with
                    StatSN = argStatSN;
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // Update target variable values for resend
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            // To resend Logout response PDU, StatSN value must not be not updated from original value.
            {
                this with
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // After this PDU is sent to the initiator, the target must increment StatSN value.
        member _.NeedIncrementStatSN() =
            true

        // Always response fence is not required.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.R_Mode

        // Target does not receive this PDU from the initiator.
        member _.ByteCount : uint32 voption =
            ValueNone

/// <summary>
///  SNACK request PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the initiator to the target.
/// </remarks>
type SNACKRequestPDU =
    {
        /// Type field value.
        Type : SnackReqTypeCd;
        /// LUN field value.
        LUN : LUN_T;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// Target Transfer Tag field value.
        TargetTransferTag : TTT_T;
        /// ExpStatSN field value.
        ExpStatSN : STATSN_T;
        /// BegRun field value.
        BegRun : uint32;
        /// RunLength field value.
        RunLength : uint32;
        /// Received byte count
        ByteCount : uint32;
    }

    interface ILogicalPDU with
        // The I bit of the SNACK request PDU is always 0.
        // But There is no CmdSN field in the SNACK request PDU, so
        // the SNACK request PDUs are always as treated immediate PDU. 
        member _.Immidiate : bool =
            true

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.SNACK

        // Get terminate flag
        member _.Final : bool =
            true

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            false

        // Get CmdSN property
        member _.CmdSN : CMDSN_T =
            cmdsn_me.zero

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            true

        // Get ExpStatSN property
        member this.ExpStatSN : STATSN_T =
            this.ExpStatSN

        // Update target variable values
        // (There are no target values, so it's return the same SNACK request PDU as is. )
        member this.UpdateTargetValues ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Update target variable values for resend
        // (There are no target values, so it's return the same SNACK request PDU as is. )
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Target does not sent this PDU to the initiator.
        member _.NeedIncrementStatSN() =
            false

        // Target does not sent this PDU to the initiator.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.Irrelevant

        // Received byte count
        member this.ByteCount : uint32 voption =
            ValueSome this.ByteCount

/// <summary>
/// Reject PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the target to the initiator.
/// </remarks>
type RejectPDU =
    {
        /// Reason field value.
        Reason : RejectReasonCd;
        /// StatSN field value.
        StatSN : STATSN_T;
        /// ExpCmdSN field value.
        ExpCmdSN : CMDSN_T;
        /// MaxCmdSN field value.
        MaxCmdSN : CMDSN_T;
        /// DataSN or R2TSN field value.
        DataSN_or_R2TSN : DATASN_T;
        /// Header Data.
        HeaderData : byte[];
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member _.Immidiate : bool =
            false

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.REJECT

        // Get terminate flag
        member _.Final : bool =
            true

        // Get InitiatorTaskTag property
        member _.InitiatorTaskTag : ITT_T =
            itt_me.fromPrim 0xFFFFFFFFu

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            false

        // Get CmdSN property
        member _.CmdSN : CMDSN_T =
            cmdsn_me.zero

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            false

        // Get ExpStatSN property
        member _.ExpStatSN : STATSN_T =
            statsn_me.zero

        // Update target variable values
        member this.UpdateTargetValues ( argStatSN : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            {
                this with
                    StatSN = argStatSN;
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // Update target variable values for resend
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            // To resend Reject PDU, StatSN value must not be not updated from original value.
            {
                this with
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // After this PDU is sent to the initiator, the target must increment StatSN value.
        member _.NeedIncrementStatSN() =
            true

        // Always response fence is not required.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.R_Mode

        // Target does not receive this PDU from the initiator.
        member _.ByteCount : uint32 voption =
            ValueNone

/// <summary>
///  NOP-Out PDU
/// </summary>
/// <remarks>
///  This PDU is sent by the initiator to the target.
/// </remarks>
[<NoComparison>]
type NOPOutPDU =
    {
        /// Immidiate flag. This field is set to true, if I bit is 1.
        I : bool;
        /// LUN field value.
        LUN : LUN_T;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// Target Transfer Tag field value.
        TargetTransferTag : TTT_T;
        /// CmdSN field value.
        CmdSN : CMDSN_T;
        /// ExpStatSN field value.
        ExpStatSN : STATSN_T;
        /// PingData field value.
        /// In PDU.Receive, this buffer is allocated by ArrayPool.
        /// The allocated buffer is passed directly to NOPInPDU.
        PingData : PooledBuffer;
        /// Received byte count
        ByteCount : uint32;
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member this.Immidiate : bool =
            this.I

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.NOP_OUT

        // Get terminate flag
        member _.Final : bool =
            true

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            true

        // Get CmdSN property
        member this.CmdSN : CMDSN_T =
            this.CmdSN

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            true

        // Get ExpStatSN property
        member this.ExpStatSN : STATSN_T =
            this.ExpStatSN

        // Update target variable values
        // (There are no target values, so it's return the same NOP-Out PDU as is. )
        member this.UpdateTargetValues ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Update target variable values for resend
        // (There are no target values, so it's return the same NOP-Out PDU as is. )
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( _ : CMDSN_T ) ( _ : CMDSN_T ) : ILogicalPDU =
            this :> ILogicalPDU

        // Target does not sent this PDU to the initiator.
        member _.NeedIncrementStatSN() =
            false

        // Target does not sent this PDU to the initiator.
        member _.NeedResponseFence : ResponseFenceNeedsFlag =
            ResponseFenceNeedsFlag.Irrelevant

        // Received byte count
        member this.ByteCount : uint32 voption =
            ValueSome this.ByteCount

/// NOP-In
[<NoComparison;>]
type NOPInPDU =
    {
        /// LUN field value.
        LUN : LUN_T;
        /// Initiator Task Tag field value.
        InitiatorTaskTag : ITT_T;
        /// Target Transfer Tag field value.
        TargetTransferTag : TTT_T;
        /// StatSN field value.
        StatSN : STATSN_T;
        /// ExpCmdSN field value.
        ExpCmdSN : CMDSN_T;
        /// MaxCmdSN field value.
        MaxCmdSN : CMDSN_T;
        /// PingData field value.
        /// It is allocated when a new NOPInPDU is constructed, or it is received from a NOPOutPDU.
        /// After the PDU is sent, or when an acknowledgment is received from the initiator, the PDU is returned to the ArrayPool.
        [<IsReadOnly>] PingData : PooledBuffer;
    }

    interface ILogicalPDU with
        // Get immidiate flag
        member _.Immidiate : bool =
            false

        // Get Opcode value
        member _.Opcode : OpcodeCd =
            OpcodeCd.NOP_IN

        // Get terminate flag
        member _.Final : bool =
            true

        // Get InitiatorTaskTag property
        member this.InitiatorTaskTag : ITT_T =
            this.InitiatorTaskTag

        // This PDU has CmdSN field or not
        member _.HasCmdSN : bool =
            false

        // Get CmdSN property
        member _.CmdSN : CMDSN_T =
            cmdsn_me.zero

        // This PDU has ExpStatSN field or not
        member _.HasExpStatSN : bool =
            false

        // Get ExpStatSN property
        member _.ExpStatSN : STATSN_T =
            statsn_me.zero

        // Update target variable values
        member this.UpdateTargetValues ( argStatSN : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            {
                this with
                    StatSN = argStatSN;
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // Update target variable values for resend
        member this.UpdateTargetValuesForResend ( _ : STATSN_T ) ( argExpCmdSN : CMDSN_T ) ( argMaxCmdSN : CMDSN_T ) : ILogicalPDU =
            // To resend NOP-In PDU, StatSN value must not be not updated from original value.
            // ( If ITT is reserved value ( = ping request by target ), that PDU is not saved for resend.
            //   So that PDU is not resent to initiator. Thus, following function has not to think this pattern. )
            {
                this with
                    ExpCmdSN = argExpCmdSN;
                    MaxCmdSN = argMaxCmdSN;
            } :> ILogicalPDU

        // If ITT is reserved value ( = ping request by target ), after send this PDU, target does not increment StatSN.
        member this.NeedIncrementStatSN() =
            ( this.InitiatorTaskTag <> itt_me.fromPrim 0xFFFFFFFFu )

        // If StatSN will not be incremented, this PDU must be send immediately.
        member this.NeedResponseFence : ResponseFenceNeedsFlag =
            if this.InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu then
                ResponseFenceNeedsFlag.Immediately
            else
                ResponseFenceNeedsFlag.R_Mode

        // Target does not receive this PDU from the initiator.
        member _.ByteCount : uint32 voption =
            ValueNone

