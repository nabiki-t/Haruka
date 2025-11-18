//=============================================================================
// Haruka Software Storage.
// iSCSI_Initiator.fs : Implement the iSCSI Initiator function used in the integration test.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test

//=============================================================================
// Import declaration

open System
open System.Diagnostics
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Net.Sockets
open System.Collections.Generic

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open System.Security.Cryptography
open System.Text

//=============================================================================
// Type definition

type SessParams = {
    InitiatorName : string;
    InitiatorAlias : string;
    TargetName : string;
    TargetAlias : string;
    ISID : ISID_T;
    TSIH : TSIH_T;
    MaxConnections : uint16;
    InitialR2T : bool;
    ImmediateData : bool;
    MaxBurstLength : uint32;
    FirstBurstLength : uint32;
    DefaultTime2Wait : uint16;
    DefaultTime2Retain : uint16;
    MaxOutstandingR2T : uint16;
    DataPDUInOrder : bool;
    DataSequenceInOrder : bool;
    ErrorRecoveryLevel : byte;
}

type ConnParams = {
    PortNo : int;
    CID : CID_T;
    Initiator_UserName : string;
    Initiator_Password : string;
    Target_UserName : string;
    Target_Password : string;
    HeaderDigest : DigestType;
    DataDigest : DigestType;
    MaxRecvDataSegmentLength_I : uint32;
    MaxRecvDataSegmentLength_T : uint32;
}

/// Represents I bit in PDU is 1 or 0.
type BitI =
    | T     // Immidiate flag is 1
    | F     // Immidiate flag os 0
    /// Convert BitI to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to BitI
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents F bit in PDU is 1 or 0.
type BitF =
    | T     // Final flag is 1
    | F     // Final flag is 0
    /// Convert BitF to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to BitF
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents R bit in PDU is 1 or 0.
type BitR =
    | T     // Read flag is 1
    | F     // Read flag is 0
    /// Convert BitR to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to BitR
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents W bit in PDU is 1 or 0.
type BitW =
    | T     // Write flag is 1
    | F     // Write flag is 0
    /// Convert BitW to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to BitW
    static member ofBool =
        function
        | true -> T
        | false -> F

/// Represents C bit in PDU is 1 or 0.
type BitC =
    | T     // Continue flag is 1
    | F     // Continue flag is 0
    /// Convert BitC to bool
    static member toBool =
        function
        | T -> true
        | F -> false
    /// Convert bool to BitC
    static member ofBool =
        function
        | true -> T
        | false -> F

///////////////////////////////////////////////////////////////////////////////
// Definition of iSCSI_Connection class

type iSCSI_Connection(
    m_ConParams : ConnParams,
    m_Connection : NetworkStream,
    initialExpStatSN : STATSN_T
) =

    let mutable m_ExpStatSN = initialExpStatSN

    let m_SendLock = new SemaphoreSlim( 1 )
    let m_ReceiveLock = new SemaphoreSlim( 1 )

    /// Get connection parameters property
    member _.Params =  m_ConParams

    /// Get connection property
    member _.Connection = m_Connection

    member _.ExpStatSN = m_ExpStatSN

    member _.IncrementExpStatSN() =
        m_ExpStatSN <- statsn_me.next m_ExpStatSN

    /// <summary>
    ///  Skip ExtStatSN Value.
    /// </summary>
    /// <param name="v">
    ///  Number of ExtStatSN value to skip.
    /// </param>
    member _.SkipExtStatSN ( v : STATSN_T ) : unit =
        m_ExpStatSN <- m_ExpStatSN + v

    /// <summary>
    ///  Rewind ExtStatSN Value.
    /// </summary>
    /// <param name="v">
    ///  Number of ExtStatSN value to rewind.
    /// </param>
    member _.RewindExtStatSN ( v : STATSN_T ) : unit =
        m_ExpStatSN <- m_ExpStatSN - v

    /// <summary>
    ///  Set next ExtStatSN Value
    /// </summary>
    /// <param name="v">
    ///  ExtStatSN value to be used.
    /// </param>
    member _.SetNextExtStatSN ( v : STATSN_T ) : unit =
        m_ExpStatSN <- v


    /// Wait to prevent conflicts between sending processes by multiple threads on the same connection.
    member _.WaitSend() =
        m_SendLock.WaitAsync()

    /// Wait to prevent conflicts between receiving processes by multiple threads on the same connection
    member _.WaitReceive() =
        m_ReceiveLock.WaitAsync()

    /// Release the send lock.
    member _.ReleaseSend() =
        m_SendLock.Release() |> ignore

    /// Release the receive lock.
    member _.ReleaseReceive() =
        m_ReceiveLock.Release() |> ignore

///////////////////////////////////////////////////////////////////////////////
// Definition of iSCSI_Initiator class


/// <summary>
///  Implementation of iSCSI initiator fanctions. Instance of this object represents one iSCSI session.
/// </summary>
/// <param name="m_SessParams">
///  Negotiated session parameters.
/// </param>
/// <param name="leadingConParams">
///  Negotiated leading connection parameters.
/// </param>
/// <param name="firstExpStatSN">
///  ExpStatSN value that is used for ExpStatSN of first PDU in the leading connection.
/// </param>
/// <param name="leadingConn">
///  Connected and logged in leading connection.
/// </param>
/// <param name="initialCmdSNValue">
///  CmdSN value that is to be used as initial value.
/// </param>
type iSCSI_Initiator(
    m_SessParams : SessParams,
    leadingConParams : ConnParams,
    firstExpStatSN : STATSN_T,
    leadingConn : NetworkStream,
    initialCmdSNValue : CMDSN_T
) =

    let m_ObjID = objidx_me.NewID()

    let m_Connections =
        [|
            ( leadingConParams.CID, iSCSI_Connection( leadingConParams, leadingConn, firstExpStatSN ) )
        |]
        |> Array.map KeyValuePair
        |> Dictionary

    let mutable m_CmdSN : uint32 =
        cmdsn_me.toPrim initialCmdSNValue - 1u
    let mutable m_ITT : uint32 = 0xFFFFFFFFu
    

    ///////////////////////////////////////////////////////////////////////////
    // public member

    /// <summary>
    ///  Add additional connection to the session.
    /// </summary>
    /// <param name="exp_ConnParams">
    ///  Desired connection parameters.
    /// </param>
    member _.AddConnection ( exp_ConnParams :  ConnParams ) : Task<unit> =
        task {
            let getCmdSN = fun () -> m_CmdSN |> cmdsn_me.fromPrim

            let r, v = m_Connections.TryGetValue exp_ConnParams.CID
            let wStatSN =
                if r then
                    m_Connections.Remove exp_ConnParams.CID |> ignore
                    v.ExpStatSN
                else
                    statsn_me.zero
            let! _, connParams, lastStatSN, conn =
                iSCSI_Initiator.Login m_SessParams exp_ConnParams m_ObjID getCmdSN ( itt_me.fromPrim m_ITT ) wStatSN false false
            m_Connections.Add( exp_ConnParams.CID, iSCSI_Connection( connParams, conn, lastStatSN ) )
        }

    /// Get connection count property
    member _.ConCount = m_Connections.Count

    /// Get connection property
    member _.Connection with get ( cid : CID_T ) =
        m_Connections.[cid]

    /// Get CID array property
    member _.CID = [| for i in m_Connections.Keys -> i |]

    /// Get session parameters
    member _.Params = m_SessParams

    /// Get CmdSN property
    member _.CmdSN with get() = cmdsn_me.fromPrim m_CmdSN
                   and  set( v : CMDSN_T ) =  m_CmdSN <- cmdsn_me.toPrim v

    /// Get ITT property
    member _.ITT with get() = itt_me.fromPrim m_ITT
                 and  set( v : ITT_T ) = m_ITT <- itt_me.toPrim v

    /// <summary>
    ///  Skip CmdSN Value.
    /// </summary>
    /// <param name="v">
    ///  Number of CmdSN value to skip.
    /// </param>
    member _.SkipCmdSN ( v : CMDSN_T ) : unit =
        m_CmdSN <- m_CmdSN + ( cmdsn_me.toPrim v )

    /// <summary>
    ///  Rewind CmdSN Value.
    /// </summary>
    /// <param name="v">
    ///  Number of CmdSN value to rewind.
    /// </param>
    member _.RewindCmdSN ( v : CMDSN_T ) : unit =
        m_CmdSN <- m_CmdSN - ( cmdsn_me.toPrim v )

    /// <summary>
    ///  Set next CmdSN Value
    /// </summary>
    /// <param name="v">
    ///  CmdSN value to be used.
    /// </param>
    member _.SetNextCmdSN ( v : CMDSN_T ) : unit =
        m_CmdSN <- ( cmdsn_me.toPrim v ) - 1u


    /// <summary>
    ///  Send SCSI command PDU to the target with test function.
    /// </summary>
    /// <param name="updater">Function to modify the PDU before sending.</param>
    /// <param name="fuz">
    ///  The offset and length of the range to be destroyed.
    ///  If this value is ValueNone, the PDU is sent without modification.
    /// </param>
    /// <param name="cid">
    ///  CID of the connection that is used to send the PDU.
    /// </param>
    /// <param name="argI">
    ///  SCSICommandPDU I field value.
    /// </param>
    /// <param name="argF">
    ///  SCSICommandPDU F field value.
    /// </param>
    /// <param name="argR">
    ///  SCSICommandPDU R field value.
    /// </param>
    /// <param name="argW">
    ///  SCSICommandPDU W field value.
    /// </param>
    /// <param name="argATTR">
    ///  SCSICommandPDU ATTR field value.
    /// </param>
    /// <param name="argLUN">
    ///  SCSICommandPDU LUN field value.
    /// </param>
    /// <param name="argExpectedDataTransferLength">
    ///  SCSICommandPDU ExpectedDataTransferLength field value.
    /// </param>
    /// <param name="argScsiCDB">
    ///  SCSICommandPDU ScsiCDB field value.
    /// </param>
    /// <param name="argDataSegment">
    ///  SCSICommandPDU DataSegment field value.
    /// </param>
    /// <param name="argBidirectionalExpectedReadDataLength">
    ///  SCSICommandPDU BidirectionalExpectedReadDataLength field value.
    /// </param>
    /// <returns>
    ///  The pair of ITT and CmdSN in sent PDU.
    /// </returns>
    member this.SendSCSICommandPDU_Test
        ( updater : SCSICommandPDU -> SCSICommandPDU )
        ( fuz : ( uint * uint ) voption )
        ( cid : CID_T )
        ( argI : BitI )
        ( argF : BitF )
        ( argR : BitR )
        ( argW : BitW )
        ( argATTR : TaskATTRCd )
        ( argLUN : LUN_T )
        ( argExpectedDataTransferLength : uint32 )
        ( argScsiCDB : byte[] )
        ( argDataSegment : PooledBuffer )
        ( argBidirectionalExpectedReadDataLength : uint32 )
        : Task< struct( ITT_T * CMDSN_T ) > =
        task {
            let con = m_Connections.[cid]
            do! con.WaitSend()
            try
                let itt = Interlocked.Increment( &m_ITT ) |> itt_me.fromPrim
                let cmdsn =
                    if BitI.toBool argI then m_CmdSN else Interlocked.Increment( &m_CmdSN )
                    |> cmdsn_me.fromPrim
                let pdu = {
                    I = BitI.toBool argI;
                    F = BitF.toBool argF;
                    R = BitR.toBool argR;
                    W = BitW.toBool argW;
                    ATTR = argATTR;
                    LUN = argLUN;
                    InitiatorTaskTag = itt;
                    ExpectedDataTransferLength = argExpectedDataTransferLength;
                    CmdSN = cmdsn;
                    ExpStatSN = con.ExpStatSN;
                    ScsiCDB = argScsiCDB;
                    DataSegment = argDataSegment;
                    BidirectionalExpectedReadDataLength = argBidirectionalExpectedReadDataLength;
                    ByteCount = 0u;
                }
                let mrdslt = con.Params.MaxRecvDataSegmentLength_T
                let headerDigest = con.Params.HeaderDigest
                let dataDigest = con.Params.DataDigest
                let pdu2 = updater pdu
                let! _ = this.SendPDUWithFazzer mrdslt headerDigest dataDigest con.Connection pdu2 fuz
                return struct( itt, cmdsn )
            finally
                con.ReleaseSend()
        }

    /// <summary>
    ///  Send SCSI command PDU to the target without test function.
    /// </summary>
    member this.SendSCSICommandPDU = this.SendSCSICommandPDU_Test id ValueNone

    /// <summary>
    ///  Send Task management function request PDU to the target with test function.
    /// </summary>
    /// <param name="updater">
    ///  Function to modify the PDU before sending.
    /// </param>
    /// <param name="fuz">
    ///  The offset and length of the range to be destroyed.
    ///  If this value is ValueNone, the PDU is sent without modification.
    /// </param>
    /// <param name="cid">
    ///  CID of the connection that is used to send the PDU.
    /// </param>
    /// <param name="argI">
    ///  TaskManagementFunctionRequestPDU I field value.
    /// </param>
    /// <param name="argFunction">
    ///  TaskManagementFunctionRequestPDU Function field value.
    /// </param>
    /// <param name="argLUN">
    ///  TaskManagementFunctionRequestPDU LUN field value.
    /// </param>
    /// <param name="argReferencedTaskTag">
    ///  TaskManagementFunctionRequestPDU ReferencedTaskTag field value.
    /// </param>
    /// <param name="argRefCmdSN">
    ///  TaskManagementFunctionRequestPDU RefCmdSN field value.
    ///  If ValueNone is specified, RefCmdSN is set to CmdSN value of this TMF command.
    /// </param>
    /// <param name="argExpDataSN">
    ///  TaskManagementFunctionRequestPDU ExpDataSN field value.
    /// </param>
    /// <returns>
    ///  The pair of ITT and CmdSN in sent PDU.
    /// </returns>
    member this.SendTaskManagementFunctionRequestPDU_Test
        ( updater : TaskManagementFunctionRequestPDU -> TaskManagementFunctionRequestPDU )
        ( fuz : ( uint * uint ) voption )
        ( cid : CID_T )
        ( argI : BitI )
        ( argFunction : TaskMgrReqCd )
        ( argLUN : LUN_T )
        ( argReferencedTaskTag : ITT_T )
        ( argRefCmdSN : CMDSN_T voption )
        ( argExpDataSN : DATASN_T )
        : Task< struct( ITT_T * CMDSN_T ) > =
        task {
            let con = m_Connections.[cid]
            do! con.WaitSend()
            try
                let itt = Interlocked.Increment( &m_ITT ) |> itt_me.fromPrim
                let cmdsn =
                    if BitI.toBool argI then m_CmdSN else Interlocked.Increment( &m_CmdSN )
                    |> cmdsn_me.fromPrim
                let pdu = {
                    I = BitI.toBool argI;
                    Function = argFunction;
                    LUN = argLUN;
                    InitiatorTaskTag = itt;
                    ReferencedTaskTag = argReferencedTaskTag;
                    CmdSN = cmdsn;
                    ExpStatSN = con.ExpStatSN;
                    RefCmdSN = ValueOption.defaultValue cmdsn argRefCmdSN
                    ExpDataSN = argExpDataSN;
                    ByteCount = 0u;
                }
                let mrdslt = con.Params.MaxRecvDataSegmentLength_T
                let headerDigest = con.Params.HeaderDigest
                let dataDigest = con.Params.DataDigest
                let pdu2 = updater pdu
                let! _ = this.SendPDUWithFazzer mrdslt headerDigest dataDigest con.Connection pdu2 fuz
                return struct( itt, cmdsn );
            finally
                con.ReleaseSend()
        }

    /// <summary>
    ///  Send Task management function request PDU to the target without test function.
    /// </summary>
    member this.SendTaskManagementFunctionRequestPDU = this.SendTaskManagementFunctionRequestPDU_Test id ValueNone

    /// <summary>
    ///  Send SCSI data out PDU to the target with test function.
    /// </summary>
    /// <param name="updater">
    ///  Function to modify the PDU before sending.
    /// </param>
    /// <param name="fuz">
    ///  The offset and length of the range to be destroyed.
    ///  If this value is ValueNone, the PDU is sent without modification.
    /// </param>
    /// <param name="cid">
    ///  CID of the connection that is used to send the PDU.
    /// </param>
    /// <param name="argF">
    ///  SCSIDataOutPDU F field value.
    /// </param>
    /// <param name="itt">
    ///  Initiator task tag field value.
    /// </param>
    /// <param name="argLUN">
    ///  SCSIDataOutPDU LUN field value.
    /// </param>
    /// <param name="argTargetTransferTag">
    ///  SCSIDataOutPDU TargetTransferTag field value.
    /// </param>
    /// <param name="argDataSN">
    ///  SCSIDataOutPDU DataSN field value.
    /// </param>
    /// <param name="argBufferOffset">
    ///  SCSIDataOutPDU BufferOffset field value.
    /// </param>
    /// <param name="argDataSegment">
    ///  SCSIDataOutPDU DataSegment field value.
    /// </param>
    /// <returns>
    ///  The pair of ITT and CmdSN in sent PDU.
    /// </returns>
    member this.SendSCSIDataOutPDU_Test
        ( updater : SCSIDataOutPDU -> SCSIDataOutPDU )
        ( fuz : ( uint * uint ) voption )
        ( cid : CID_T )
        ( argF : BitF )
        ( itt : ITT_T )
        ( argLUN : LUN_T )
        ( argTargetTransferTag : TTT_T )
        ( argDataSN : DATASN_T )
        ( argBufferOffset : uint32 )
        ( argDataSegment : PooledBuffer )
        : Task<unit> =
        task {
            let con = m_Connections.[cid]
            do! con.WaitSend()
            try
                let pdu = {
                    F = BitF.toBool argF;
                    LUN = argLUN;
                    InitiatorTaskTag = itt;
                    TargetTransferTag = argTargetTransferTag;
                    ExpStatSN = con.ExpStatSN;
                    DataSN = argDataSN;
                    BufferOffset = argBufferOffset;
                    DataSegment = argDataSegment;
                    ByteCount = 0u;
                }
                let mrdslt = con.Params.MaxRecvDataSegmentLength_T
                let headerDigest = con.Params.HeaderDigest
                let dataDigest = con.Params.DataDigest
                let pdu2 = updater pdu
                let! _ = this.SendPDUWithFazzer mrdslt headerDigest dataDigest con.Connection pdu2 fuz
                ()
            finally
                con.ReleaseSend()
        }

    /// <summary>
    ///  Send SCSI data out PDU to the target with test function.
    /// </summary>
    member this.SendSCSIDataOutPDU = this.SendSCSIDataOutPDU_Test id ValueNone

    /// <summary>
    ///  Send text request PDU to the target with test function.
    /// </summary>
    /// <param name="updater">
    ///  Function to modify the PDU before sending.
    /// </param>
    /// <param name="fuz">
    ///  The offset and length of the range to be destroyed.
    ///  If this value is ValueNone, the PDU is sent without modification.
    /// </param>
    /// <param name="cid">
    ///  CID of the connection that is used to send the PDU.
    /// </param>
    /// <param name="argI">
    ///  TextRequestPDU I field value.
    /// </param>
    /// <param name="argF">
    ///  TextRequestPDU F field value.
    /// </param>
    /// <param name="argC">
    ///  TextRequestPDU C field value.
    /// </param>
    /// <param name="argITT">
    ///  Initiator Task Tag field value.
    /// </param>
    /// <param name="argLUN">
    ///  TextRequestPDU LUN field value.
    /// </param>
    /// <param name="argTargetTransferTag">
    ///  TextRequestPDU TargetTransferTag field value.
    /// </param>
    /// <param name="argTextRequest">
    ///  TextRequestPDU TextRequest field value.
    /// </param>
    /// <returns>
    ///  The pair of ITT and CmdSN in sent PDU.
    /// </returns>
    member this.SendTextRequestPDU_Test
        ( updater : TextRequestPDU -> TextRequestPDU )
        ( fuz : ( uint * uint ) voption )
        ( cid : CID_T )
        ( argI : BitI )
        ( argF : BitF )
        ( argC : BitC )
        ( argITT : ITT_T voption )
        ( argLUN : LUN_T )
        ( argTargetTransferTag : TTT_T )
        ( argTextRequest : byte[] )
        : Task< struct( ITT_T * CMDSN_T ) > =
        task {
            let con = m_Connections.[cid]
            do! con.WaitSend()
            try
                let itt =
                    match argITT with
                    | ValueSome( x ) ->
                        x
                    | ValueNone ->
                        Interlocked.Increment( &m_ITT ) |> itt_me.fromPrim
                let cmdsn =
                    if BitI.toBool argI then m_CmdSN else Interlocked.Increment( &m_CmdSN )
                    |> cmdsn_me.fromPrim
                let pdu = {
                    I = BitI.toBool argI;
                    F = BitF.toBool argF;
                    C = BitC.toBool argC;
                    LUN = argLUN;
                    InitiatorTaskTag = itt;
                    TargetTransferTag = argTargetTransferTag;
                    CmdSN = cmdsn;
                    ExpStatSN = con.ExpStatSN;
                    TextRequest = argTextRequest;
                    ByteCount = 0u;
                }
                let mrdslt = con.Params.MaxRecvDataSegmentLength_T
                let headerDigest = con.Params.HeaderDigest
                let dataDigest = con.Params.DataDigest
                let pdu2 = updater pdu
                let! _ = this.SendPDUWithFazzer mrdslt headerDigest dataDigest con.Connection pdu2 fuz
                return struct( itt, cmdsn );
            finally
                con.ReleaseSend()
        }

    /// <summary>
    ///  Send text request PDU to the target without test function.
    /// </summary>
    member this.SendTextRequestPDU = this.SendTextRequestPDU_Test id ValueNone

    /// <summary>
    ///  Send logout request PDU to the target with test function.
    /// </summary>
    /// <param name="updater">
    ///  Function to modify the PDU before sending.
    /// </param>
    /// <param name="fuz">
    ///  The offset and length of the range to be destroyed.
    ///  If this value is ValueNone, the PDU is sent without modification.
    /// </param>
    /// <param name="cid">
    ///  CID of the connection that is used to send the PDU.
    /// </param>
    /// <param name="argI">
    ///  LogoutRequestPDU I field value.
    /// </param>
    /// <param name="argReasonCode">
    ///  LogoutRequestPDU ReasonCode field value.
    /// </param>
    /// <param name="argCID">
    ///  LogoutRequestPDU CID field value.
    /// </param>
    /// <returns>
    ///  The pair of ITT and CmdSN in sent PDU.
    /// </returns>
    member this.SendLogoutRequestPDU_Test
        ( updater : LogoutRequestPDU -> LogoutRequestPDU )
        ( fuz : ( uint * uint ) voption )
        ( cid : CID_T )
        ( argI : BitI )
        ( argReasonCode : LogoutReqReasonCd )
        ( argCID : CID_T )
        : Task< struct( ITT_T * CMDSN_T ) > =
        task {
            let con = m_Connections.[cid]
            do! con.WaitSend()
            try
                let itt = Interlocked.Increment( &m_ITT ) |> itt_me.fromPrim
                let cmdsn =
                    if BitI.toBool argI then m_CmdSN else Interlocked.Increment( &m_CmdSN )
                    |> cmdsn_me.fromPrim
                let pdu = {
                    I = BitI.toBool argI;
                    ReasonCode = argReasonCode;
                    InitiatorTaskTag = itt;
                    CID = argCID;
                    CmdSN = cmdsn;
                    ExpStatSN = con.ExpStatSN;
                    ByteCount = 0u;
                }
                let mrdslt = con.Params.MaxRecvDataSegmentLength_T
                let headerDigest = con.Params.HeaderDigest
                let dataDigest = con.Params.DataDigest
                let pdu2 = updater pdu
                let! _ = this.SendPDUWithFazzer mrdslt headerDigest dataDigest con.Connection pdu2 fuz
                return struct( itt, cmdsn );
            finally
                con.ReleaseSend()
        }

    /// <summary>
    ///  Send logout request PDU to the target without test function.
    /// </summary>
    member this.SendLogoutRequestPDU = this.SendLogoutRequestPDU_Test id ValueNone

    /// <summary>
    ///  Send SNACK request PDU to the target with test function.
    /// </summary>
    /// <param name="updater">
    ///  Function to modify the PDU before sending.
    /// </param>
    /// <param name="fuz">
    ///  The offset and length of the range to be destroyed.
    ///  If this value is ValueNone, the PDU is sent without modification.
    /// </param>
    /// <param name="cid">
    ///  CID of the connection that is used to send the PDU.
    /// </param>
    /// <param name="argType">
    ///  SNACKRequestPDU Type field value.
    /// </param>
    /// <param name="argLUN">
    ///  SNACKRequestPDU LUN field value.
    /// </param>
    /// <param name="argInitiatorTaskTag">
    ///  SNACKRequestPDU InitiatorTaskTag field value.
    /// </param>
    /// <param name="argTargetTransferTag">
    ///  SNACKRequestPDU TargetTransferTag field value.
    /// </param>
    /// <param name="argBegRun">
    ///  SNACKRequestPDU BegRun field value.
    /// </param>
    /// <param name="argRunLength">
    ///  SNACKRequestPDU RunLength field value.
    /// </param>
    /// <returns>
    ///  The pair of ITT and CmdSN in sent PDU.
    /// </returns>
    member this.SendSNACKRequestPDU_Test
        ( updater : SNACKRequestPDU -> SNACKRequestPDU )
        ( fuz : ( uint * uint ) voption )
        ( cid : CID_T )
        ( argType : SnackReqTypeCd )
        ( argLUN : LUN_T )
        ( argInitiatorTaskTag : ITT_T )
        ( argTargetTransferTag : TTT_T )
        ( argBegRun : uint32 )
        ( argRunLength : uint32 )
        : Task< unit > =
        task {
            let con = m_Connections.[cid]
            do! con.WaitSend()
            try
                let pdu = {
                    Type = argType;
                    LUN = argLUN;
                    InitiatorTaskTag = argInitiatorTaskTag;
                    TargetTransferTag = argTargetTransferTag;
                    ExpStatSN = con.ExpStatSN;
                    BegRun = argBegRun;
                    RunLength = argRunLength;
                    ByteCount = 0u;
                }
                let mrdslt = con.Params.MaxRecvDataSegmentLength_T
                let headerDigest = con.Params.HeaderDigest
                let dataDigest = con.Params.DataDigest
                let pdu2 = updater pdu
                let! _ = this.SendPDUWithFazzer mrdslt headerDigest dataDigest con.Connection pdu2 fuz
                ()
            finally
                con.ReleaseSend()
        }

    /// <summary>
    ///  Send SNACK request PDU to the target without test function.
    /// </summary>
    member this.SendSNACKRequestPDU = this.SendSNACKRequestPDU_Test id ValueNone

    /// <summary>
    ///  Send NOP out PDU to the target with test function.
    /// </summary>
    /// <param name="updater">
    ///  Function to modify the PDU before sending.
    /// </param>
    /// <param name="fuz">
    ///  The offset and length of the range to be destroyed.
    ///  If this value is ValueNone, the PDU is sent without modification.
    /// </param>
    /// <param name="cid">
    ///  CID of the connection that is used to send the PDU.
    /// </param>
    /// <param name="argI">
    ///  NOPOutPDU I field value.
    /// </param>
    /// <param name="argLUN">
    ///  NOPOutPDU LUN field value.
    /// </param>
    /// <param name="argTargetTransferTag">
    ///  NOPOutPDU TargetTransferTag field value.
    /// </param>
    /// <param name="argPingData">
    ///  NOPOutPDU PingData field value.
    /// </param>
    /// <returns>
    ///  The pair of ITT and CmdSN in sent PDU.
    /// </returns>
    member this.SendNOPOutPDU_Test
        ( updater : NOPOutPDU -> NOPOutPDU )
        ( fuz : ( uint * uint ) voption )
        ( cid : CID_T )
        ( argI : BitI )
        ( argLUN : LUN_T )
        ( argTargetTransferTag : TTT_T )
        ( argPingData : PooledBuffer )
        : Task< struct( ITT_T * CMDSN_T ) > =
        task {
            let con = m_Connections.[cid]
            do! con.WaitSend()
            try
                let itt = Interlocked.Increment( &m_ITT ) |> itt_me.fromPrim
                let cmdsn =
                    if BitI.toBool argI then m_CmdSN else Interlocked.Increment( &m_CmdSN )
                    |> cmdsn_me.fromPrim
                let pdu = {
                    I = BitI.toBool argI;
                    LUN = argLUN;
                    InitiatorTaskTag = itt;
                    TargetTransferTag = argTargetTransferTag;
                    CmdSN = cmdsn;
                    ExpStatSN = con.ExpStatSN;
                    PingData = argPingData;
                    ByteCount = 0u;
                }
                let mrdslt = con.Params.MaxRecvDataSegmentLength_T
                let headerDigest = con.Params.HeaderDigest
                let dataDigest = con.Params.DataDigest
                let pdu2 = updater pdu
                let! _ = this.SendPDUWithFazzer mrdslt headerDigest dataDigest con.Connection pdu2 fuz
                return struct( itt, cmdsn );
            finally
                con.ReleaseSend()
        }

    /// <summary>
    ///  Send NOP out PDU to the target without test function.
    /// </summary>
    member this.SendNOPOutPDU = this.SendNOPOutPDU_Test id ValueNone

    /// <summary>
    ///  Receive response PDU.
    /// </summary>
    /// <param name="cid">
    ///  CID of the connection that is used to receive the PDU.
    /// </param>
    /// <returns>
    ///  Received PDU.
    /// </returns>
    member _.Receive ( cid : CID_T ) : Task<ILogicalPDU> =
        task {
            let con = m_Connections.[cid]
            do! con.WaitReceive()
            try
                let mrdsli = con.Params.MaxRecvDataSegmentLength_I
                let headerDigest = con.Params.HeaderDigest
                let dataDigest = con.Params.DataDigest
                let! pdu = PDU.Receive( mrdsli, headerDigest, dataDigest, ValueNone, ValueNone, ValueNone, con.Connection, Standpoint.Initiator )

                // If a PDU with the expected StatSN is received, increment ExpStatSN.
                match pdu with
                | :? SCSIResponsePDU as x ->
                    if x.StatSN = con.ExpStatSN then
                        con.IncrementExpStatSN()
                | :? TaskManagementFunctionResponsePDU as x ->
                    if x.StatSN = con.ExpStatSN then
                        con.IncrementExpStatSN()
                | :? AsyncronousMessagePDU as x ->
                    if x.StatSN = con.ExpStatSN then
                        con.IncrementExpStatSN()
                | :? TextResponsePDU as x ->
                    if x.StatSN = con.ExpStatSN then
                        con.IncrementExpStatSN()
                | :? LogoutResponsePDU as x ->
                    if x.StatSN = con.ExpStatSN then
                        con.IncrementExpStatSN()
                | :? NOPInPDU as x ->
                    if x.StatSN = con.ExpStatSN then
                        con.IncrementExpStatSN()
                | :? SCSIDataInPDU as x ->
                    if x.S && x.StatSN = con.ExpStatSN then
                        con.IncrementExpStatSN()
                | _ -> ()

                return pdu
            finally
                con.ReleaseReceive()
        }

    /// <summary>
    ///  Receive a response PDU of specific type.
    /// </summary>
    /// <param name="cid">
    ///  CID of the connection that is used to receive the PDU.
    /// </param>
    /// <returns>
    ///  Received PDU.
    /// </returns>
    member this.ReceiveSpecific< 'T when 'T :> ILogicalPDU > ( cid : CID_T ) : Task<'T> =
        task {
            let! r = this.Receive cid
            return r :?> 'T
        }

    /// <summary>
    ///  close and remove connection from the session..
    /// </summary>
    /// <param name="cid">
    ///  Connection ID
    /// </param>
    member _.CloseConnection ( cid : CID_T ) : unit =
        let con = m_Connections.[cid]
        con.Connection.Flush()
        con.Connection.Close()
        con.Connection.Dispose()
        m_Connections.Remove( cid ) |> ignore

    /// <summary>
    ///  Close session graaseflly.
    /// </summary>
    /// <param name="cid">
    ///  Specifies the connection to use to send the logout request.
    /// </param>
    /// <param name="immidiate">
    ///  Specify whether to deliver immediately or not.
    /// </param>
    member this.CloseSession ( cid : CID_T ) ( immidiate : BitI ) : Task<unit> =
        task {
            let! itt, _ = this.SendLogoutRequestPDU cid immidiate LogoutReqReasonCd.CLOSE_SESS cid
            let! rpdu5 = this.ReceiveSpecific<LogoutResponsePDU> cid
            if rpdu5.InitiatorTaskTag <> itt then
                raise <| SessionRecoveryException( "Unexpedted ITT", m_SessParams.TSIH )
            if rpdu5.Response <> LogoutResCd.SUCCESS then
                raise <| SessionRecoveryException( "Unexpedted Response", m_SessParams.TSIH )
            this.CloseConnection cid
        }

    /// <summary>
    ///  Remove connection entry and return old connection object.
    ///  Connection will not be closed.
    /// </summary>
    /// <param name="cid">
    ///  Connection ID of removed connection.
    /// </param>
    /// <returns>
    ///  Removed connection.
    /// </returns>
    member _.RemoveConnectionEntry ( cid : CID_T ) : iSCSI_Connection =
        let r = m_Connections.[cid]
        m_Connections.Remove( cid ) |> ignore
        r

    /// <summary>
    ///  Forces an update of the negotiated connection parameter values.
    /// </summary>
    /// <param name="cid">
    ///  Connection ID of which connection should be updated.
    /// </param>
    /// <param name="conParams">
    ///  New parameter values.
    /// </param>
    member this.FakeConnectionParameter ( cid : CID_T ) ( conParams : ConnParams ) : unit =
        let r = this.RemoveConnectionEntry cid
        m_Connections.Add( cid, iSCSI_Connection( conParams, r.Connection, r.ExpStatSN ) )

    /// <summary>
    ///  Read a specified range of data from the media.
    /// </summary>
    /// <param name="cid">
    ///  Specifies the connection to be used for sending and receiving PDUs.
    /// </param>
    /// <param name="lun">
    ///  Specify the LU to be read.
    /// </param>
    /// <param name="lba">
    ///  Specifiy the start position to be read.
    /// </param>
    /// <param name="blockCount">
    ///  Specifiy the length to be read.
    /// </param>
    /// <param name="blockLength">
    ///  Media block size.
    /// </param>
    /// <returns>
    ///  Return the loaded data.
    /// </returns>
    member this.ReadMediaData ( cid : CID_T ) ( lun : LUN_T ) ( lba : uint32 ) ( blockCount : uint32 ) ( blockLength : uint32 ) : Task<byte[]> =
        task {
            let accessLength = blockCount * blockLength
            let rBuffer = Array.zeroCreate<byte>( int accessLength )
            let readCDB = GenScsiCDB.Read10 0uy false false false lba 0uy ( uint16 blockCount ) false false
            let! itt, _ = this.SendSCSICommandPDU cid BitI.F BitF.T BitR.T BitW.F TaskATTRCd.SIMPLE_TASK lun accessLength readCDB PooledBuffer.Empty 0u

            do! Functions.loopAsync ( fun () ->
                task {
                    let! pdu = this.Receive cid
                    match pdu with
                    | :? SCSIDataInPDU as x ->
                        if x.InitiatorTaskTag <> itt then
                            raise <| SessionRecoveryException( "Unexpedted ITT", m_SessParams.TSIH )
                        x.DataSegment.CopyTo( rBuffer, int x.BufferOffset )
                        return true
                    | :? SCSIResponsePDU as x ->
                        if x.InitiatorTaskTag <> itt then
                            raise <| SessionRecoveryException( "Unexpedted ITT", m_SessParams.TSIH )
                        if x.Response <> iScsiSvcRespCd.COMMAND_COMPLETE then
                            raise <| SessionRecoveryException( "Unexpedted Response", m_SessParams.TSIH )
                        if x.Status <> ScsiCmdStatCd.GOOD then
                            raise <| SessionRecoveryException( "Unexpedted Status", m_SessParams.TSIH )
                        return false
                    | _ ->
                        raise <| SessionRecoveryException( "Unexpedted PDU", m_SessParams.TSIH )
                        return false
                }
            )
            return rBuffer
        }

    /// <summary>
    ///  Write data to specified range of the media.
    /// </summary>
    /// <param name="cid">
    ///  Specifies the connection to be used for sending and receiving PDUs.
    /// </param>
    /// <param name="lun">
    ///  Specify the LU to be read.
    /// </param>
    /// <param name="lba">
    ///  Specifiy the start position to be read.
    /// </param>
    /// <param name="blockLength">
    ///  Media block size.
    /// </param>
    /// <param name="bytesData">
    ///  Data to be written.
    /// </param>
    member this.WriteMediaData ( cid : CID_T ) ( lun : LUN_T ) ( lba : uint32 ) ( blockLength : uint32 ) ( bytesData : byte[] ) : Task<unit> =
        task {
            let blockCount = uint16 ( bytesData.Length / int blockLength )
            let mbl = m_SessParams.MaxBurstLength
            let mrdsl = m_Connections.[cid].Params.MaxRecvDataSegmentLength_T
            let writeCDB = GenScsiCDB.Write10 0uy false false false lba 0uy blockCount false false
            let! itt, _ = this.SendSCSICommandPDU cid BitI.F BitF.T BitR.F BitW.T TaskATTRCd.SIMPLE_TASK lun ( uint32 bytesData.Length ) writeCDB PooledBuffer.Empty 0u

            let loop () : Task<bool> =
                task {
                    let! pdu = this.Receive cid
                    match pdu with
                    | :? R2TPDU as x ->
                        if x.InitiatorTaskTag <> itt then
                            raise <| SessionRecoveryException( "Unexpedted ITT", m_SessParams.TSIH )

                        let segs =
                            Functions.DivideRespDataSegment x.BufferOffset x.DesiredDataTransferLength mbl mrdsl
                            |> List.indexed
                            |> List.map ( fun ( idx, struct( s, l , f ) ) -> struct( idx, s, l, f ) )

                        for struct( idx, s, l, f ) in segs do
                            let sendData = PooledBuffer.Rent( bytesData.[ ( int s ) .. ( int s + int l - 1 ) ] )
                            let datasn = datasn_me.fromPrim ( uint32 idx )
                            do! this.SendSCSIDataOutPDU cid ( BitF.ofBool f ) itt lun x.TargetTransferTag datasn s sendData
                            sendData.Return()

                        return true
                    | :? SCSIResponsePDU as x ->
                        if x.InitiatorTaskTag <> itt then
                            raise <| SessionRecoveryException( "Unexpedted ITT", m_SessParams.TSIH )
                        if x.Response <> iScsiSvcRespCd.COMMAND_COMPLETE then
                            raise <| SessionRecoveryException( "Unexpedted Response", m_SessParams.TSIH )
                        if x.Status <> ScsiCmdStatCd.GOOD then
                            raise <| SessionRecoveryException( "Unexpedted Status", m_SessParams.TSIH )
                        return false
                    | _ ->
                        raise <| SessionRecoveryException( "Unexpedted PDU", m_SessParams.TSIH )
                        return false
                }

            do! Functions.loopAsync loop
        }

    /// <summary>
    ///  Send SendTargets text request and receive responces.
    /// </summary>
    /// <param name="cid">
    ///  Specifies the connection to be used for sending and receiving PDUs.
    /// </param>
    /// <param name="param">
    ///  Parameter data that will be send as SendTargets text key.
    /// </param>
    /// <returns>
    ///  Dictionary of the target name and target addresses.
    /// </returns>
    member this.SendTargetsTextRequest ( cid : CID_T ) ( param : string ) : Task< Dictionary< string, string[] > > =

        let buildResult ( respKeyVal : string[][] ) : Dictionary< string, string[] > =
            let rd = Dictionary< string, List<string> >()
            let rec loop ( targetName : string ) ( idx : int ) =
                if idx < respKeyVal.Length then
                    if respKeyVal.[idx].[0] = "TargetName" then
                        loop respKeyVal.[idx].[1] ( idx + 1 )
                    else
                        let r, v = rd.TryGetValue targetName
                        if r then
                            v.Add respKeyVal.[idx].[1]
                        else
                            let nv = List<string>( [| respKeyVal.[idx].[1] |] )
                            rd.Add( targetName, nv )
                        loop targetName ( idx + 1 )
            loop "" 0
            rd
            |> Seq.map ( fun itr -> itr.Key, itr.Value |> Seq.toArray )
            |> Seq.map KeyValuePair
            |> Dictionary
        task {
            // Send SendTargets text request
            let rv = List<TextResponsePDU>()
            while rv.Count = 0 || rv.[ rv.Count - 1 ].C do
                let textReq =
                    if rv.Count = 0 then
                        [|
                            yield! ( Encoding.UTF8.GetBytes "SendTargets=" )
                            yield! ( Encoding.UTF8.GetBytes param )
                            yield '\u0000'B
                        |]
                    else
                        Array.Empty()
                let! _ = this.SendTextRequestPDU cid BitI.T BitF.F BitC.F ValueNone lun_me.zero ( ttt_me.fromPrim 0xFFFFFFFFu ) textReq
                let! wresppdu = this.ReceiveSpecific<TextResponsePDU> cid
                rv.Add wresppdu

            let keyValues =
                rv
                |> Seq.map _.TextResponse
                |> Seq.concat
                |> Seq.toArray
                |> Functions.SplitByteArray 0uy
                |> List.filter ( fun x -> x.Length <> 0 )
                |> List.map Encoding.UTF8.GetString
            let keyValues2 =
                keyValues
                |> List.map ( fun itr -> itr.Split '=' )
                |> List.filter ( fun itr -> itr.Length = 2 )
                |> List.filter ( fun itr -> itr.[0] = "TargetName" || itr.[0] = "TargetAddress" )
                |> List.toArray

            return buildResult keyValues2
        }
    ///////////////////////////////////////////////////////////////////////////
    // private member

    /// <summary>
    ///  Sends a PDU while destroying data in the specified range.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///  The target's MaxRecvDataSegmentLength value reported by the target.
    /// </param>
    /// <param name="argHeaderDigest">
    ///  Whether to use header digest.
    /// </param>
    /// <param name="argDataDigest">
    ///  Whether to use data digest.
    /// </param>
    /// <param name="sock">
    ///  The network stream to send the PDU.
    /// </param>
    /// <param name="argPDU">
    ///  The PDU to send.
    /// </param>
    /// <param name="fuz">
    ///  The offset and length of the range to be destroyed.
    ///  If this value is ValueNone, the PDU is sent without modification.
    /// </param>
    member private _.SendPDUWithFazzer
            ( argMaxRecvDataSegmentLength : uint32 )
            ( argHeaderDigest : DigestType )
            ( argDataDigest : DigestType )
            ( sock : Stream )
            ( argPDU : ILogicalPDU )
            ( fuz : ( uint * uint ) voption ) : Task<unit> =
        task {
            match fuz with
            | ValueSome( off, len ) ->
                let off64 = int64 off
                let len64 = int64 len

                // Output the PDU to a MemoryStream in the form of sending it over the network.
                use mb = new MemoryStream()
                let! _ = PDU.SendPDU( argMaxRecvDataSegmentLength, argHeaderDigest, argDataDigest, ValueNone, ValueNone, ValueNone, m_ObjID, mb, argPDU )

                // Overwrites the data in the specified range with random numbers.
                if off64 < mb.Length then
                    let wcnt = ( min mb.Length ( off64 + len64 ) ) - off64
                    mb.Seek( off64, SeekOrigin.Begin ) |> ignore
                    for _ = 0L to wcnt - 1L do
                        mb.WriteByte( byte <| Random.Shared.Next() )

                // Send the modified data to the network.
                mb.Seek( 0L, SeekOrigin.Begin ) |> ignore
                do! mb.CopyToAsync( sock )

            | ValueNone ->
                let! _ = PDU.SendPDU( argMaxRecvDataSegmentLength, argHeaderDigest, argDataDigest, ValueNone, ValueNone, ValueNone, m_ObjID, sock, argPDU )
                ()
        }

    ///////////////////////////////////////////////////////////////////////////
    // static public member

    /// <summary>
    ///  Initialize Haruka configuration directory
    /// </summary>
    /// <param name="workPath">
    ///  Path name of the working folder.
    /// </param>
    /// <param name="controllPortNo">
    ///  The TCP port number used for client connections to the controller.
    /// </param>
    static member InitializeConfigDir ( workPath : string ) ( controllPortNo : int ) : unit =
        let curdir = Path.GetDirectoryName workPath

        // Initialize Haruka configuration directory
        let ctrlProc1 = new Process(
            StartInfo = ProcessStartInfo(
                FileName = GlbFunc.controllerExePath,
                Arguments = sprintf "ID \"%s\" /p %d /a ::1 /o" workPath controllPortNo,
                CreateNoWindow = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                WorkingDirectory = curdir
            ),
            EnableRaisingEvents = true
        )
        if ctrlProc1.Start() |> not then
            raise <| TestException( "Failed to start controller proc." )

        if ctrlProc1.WaitForExit( 5000 ) |> not then
            raise <| TestException( "The controller process does not terminate." )

        if ctrlProc1.ExitCode <> 0 then
            raise <| TestException( "The controller process terminated abnormally." )

    /// <summary>
    ///  Create session instance and login the leading connection. ISID will be newly issued.
    /// </summary>
    /// <param name="exp_SessParams">
    ///  Desired session parameters. ISID field value is ignored.
    /// </param>
    /// <param name="exp_ConnParams">
    ///  Desired connection parameters.
    /// </param>
    /// <returns>
    ///  Created iSCSI initiator object.
    /// </returns>
    /// <remarks>
    ///  Regardless of the value of the ISID field in exp_SessParams, a new ISID will be generated.
    ///  If the ISID needs to be explicitly specified, use the CreateInitialSessionWithInitialCmdSN method.
    /// </remarks>
    static member CreateInitialSession ( exp_SessParams : SessParams ) ( exp_ConnParams : ConnParams ) : Task<iSCSI_Initiator> =
        let sesParam = {
            exp_SessParams with
                ISID = GlbFunc.newISID();
        }
        iSCSI_Initiator.CreateInitialSessionWithInitialCmdSN sesParam exp_ConnParams cmdsn_me.zero

    /// <summary>
    ///  Create session instance and login the leading connection.
    /// </summary>
    /// <param name="exp_SessParams">
    ///  Desired session parameters.
    /// </param>
    /// <param name="exp_ConnParams">
    ///  Desired connection parameters.
    /// </param>
    /// <param name="initCmdSN">
    ///  CmdSN value that is to be used as initial value..
    /// </param>
    /// <returns>
    ///  Created iSCSI initiator object.
    /// </returns>
    static member CreateInitialSessionWithInitialCmdSN ( exp_SessParams : SessParams ) ( exp_ConnParams : ConnParams ) ( initCmdSN : CMDSN_T ) : Task<iSCSI_Initiator> =
        task {
            let objID = objidx_me.NewID()
            let getCmdSN = fun () -> initCmdSN
            let! sessParams, connParams, lastStatSN, conn =
                iSCSI_Initiator.Login exp_SessParams exp_ConnParams objID getCmdSN ( itt_me.fromPrim 0u ) statsn_me.zero true false
            return new iSCSI_Initiator( sessParams, connParams, statsn_me.next lastStatSN, conn, initCmdSN )
        }

    /// <summary>
    ///  Create session instance and login for discovery session..
    /// </summary>
    /// <param name="exp_SessParams">
    ///  Desired session parameters.
    /// </param>
    /// <param name="exp_ConnParams">
    ///  Desired connection parameters.
    /// </param>
    /// <returns>
    ///  Created iSCSI initiator object.
    /// </returns>
    static member LoginForDiscoverySession ( exp_SessParams : SessParams ) ( exp_ConnParams : ConnParams ) : Task<iSCSI_Initiator> =
        task {
            let objID = objidx_me.NewID()
            let getCmdSN = fun () -> cmdsn_me.zero
            let! sessParams, connParams, lastStatSN, conn =
                iSCSI_Initiator.Login exp_SessParams exp_ConnParams objID getCmdSN ( itt_me.fromPrim 0u ) statsn_me.zero true true
            return new iSCSI_Initiator( sessParams, connParams, statsn_me.next lastStatSN, conn, cmdsn_me.zero )
        }

    /// <summary>
    ///  Login for discovery session and get target name list.
    /// </summary>
    /// <param name="exp_ConnParams">
    ///  Desired connection parameters.
    /// </param>
    /// <param name="param">
    ///  Parameter data that will be send as SendTargets text key.
    /// </param>
    /// <returns>
    ///  Dictionary of the target name and target addresses.
    /// </returns>
    static member QueryTargetNames ( exp_ConnParams : ConnParams ) ( param : string ) : Task< Dictionary< string, string[] > > =
        task {
            let swp = {
                InitiatorName = "iqn.2020-05.example.com:initiator";
                InitiatorAlias = "";
                TargetName = "";
                TargetAlias = "";
                ISID = isid_me.zero;
                TSIH = tsih_me.zero;
                MaxConnections = 1us;
                InitialR2T = false;
                ImmediateData = true;
                MaxBurstLength = 8192u;
                FirstBurstLength = 8192u;
                DefaultTime2Wait = 2us;
                DefaultTime2Retain = 20us;
                MaxOutstandingR2T = 1us;
                DataPDUInOrder = true;
                DataSequenceInOrder = true;
                ErrorRecoveryLevel = 0uy;
            }
            let! sess = iSCSI_Initiator.LoginForDiscoverySession swp exp_ConnParams
            let cid = exp_ConnParams.CID
            let! rd = sess.SendTargetsTextRequest cid param

            // Logout
            let! _ = sess.SendLogoutRequestPDU cid BitI.F LogoutReqReasonCd.CLOSE_SESS cid
            let! _ = sess.ReceiveSpecific<LogoutResponsePDU> cid

            return rd
        }

    ///////////////////////////////////////////////////////////////////////////
    // static private member

    /// <summary>
    ///  Login to the initiator.
    /// </summary>
    /// <param name="exp_SessParams">
    ///  Desired session parameters.
    /// </param>
    /// <param name="exp_ConnParams">
    ///  Desired connection parameters.
    /// </param>
    /// <param name="objID">
    ///  Object ID for log output.
    /// </param>
    /// <param name="getCmdSN">
    ///  The function to get current CmdSN value.
    /// </param>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <param name="expStatSN">
    ///  Next ExpStatSN value.
    /// </param>
    /// <param name="isLeadingCon">
    ///  Leading connection or not.
    /// </param>
    /// <param name="isDiscoverySession">
    ///  Discovery session or normal session.
    /// </param>
    /// <returns>
    ///  Negotiated session params,
    /// </returns>
    static member private Login
            ( exp_SessParams : SessParams )
            ( exp_ConnParams : ConnParams )
            ( objID : OBJIDX_T )
            ( getCmdSN : unit -> CMDSN_T )
            ( itt : ITT_T )
            ( expStatSN : STATSN_T )
            ( isLeadingCon : bool )
            ( isDiscoverySession : bool )
            : Task< SessParams * ConnParams * STATSN_T * NetworkStream > =
        task {
            let conn = GlbFunc.ConnectToServer( exp_ConnParams.PortNo )

            let negoValue1 =
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetName =
                            if isDiscoverySession then
                                TextValueType.ISV_Missing
                            else
                                TextValueType.Value( exp_SessParams.TargetName );
                        InitiatorName = TextValueType.Value( exp_SessParams.InitiatorName );
                        SessionType =
                            if isDiscoverySession then
                                TextValueType.Value( "Discovery" )
                            else
                                TextValueType.Value( "Normal" );
                        AuthMethod =
                            if exp_ConnParams.Initiator_UserName.Length = 0 && exp_ConnParams.Target_UserName.Length = 0 then
                                TextValueType.Value( [| AuthMethodCandidateValue.AMC_None |] )
                            else
                                TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP |] )
                }
            let negoStat1 =
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_TargetName =
                            if isDiscoverySession then NegoStatusValue.NSV_Negotiated else NegoStatusValue.NSG_WaitSend;
                        NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend;
                        NegoStat_SessionType = NegoStatusValue.NSG_WaitSend;
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend;
                }

            // Send initial login request
            let textReq = IscsiTextEncode.CreateTextKeyValueString negoValue1 negoStat1
            let loginRequest =
                {
                    T = false;
                    C = false;
                    CSG = LoginReqStateCd.SEQURITY;
                    NSG = LoginReqStateCd.SEQURITY;
                    VersionMax = 0x00uy;
                    VersionMin = 0x00uy;
                    ISID = exp_SessParams.ISID; // Specifies a value that identifies the initiator port.
                    TSIH = exp_SessParams.TSIH; // 0 for the leading connection. Notified by the target in the last login response.
                                                // For connections other than the leading connection, use the TSIH for that session.
                    InitiatorTaskTag = itt;     // Always 0 for leading connection. Does not change during negotiation.
                                                // For non-leading connections, use a valid ITT at the start of the login phase.
                    CID = exp_ConnParams.CID;
                    CmdSN = getCmdSN();         // Always 0 for leading connections. No addition.
                                                // For connections other than the leading connection, the CmdSN value at the time of PDU transmission is sent. No increments are made. (Same as for immediate commands)
                    ExpStatSN = expStatSN;      // It starts with 0. It is incremented by 1 for each PDU. If the StatSN value is not as expected, an error occurs.
                                                // When a connection is re-established, the original ExpStatSN value is retained.
                    TextRequest = textReq;
                    ByteCount = 0u;             // not used
                }
            let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objID, conn, loginRequest )
            let negoStat2 = IscsiTextEncode.ClearSendWaitStatus negoStat1

            // Receive response
            let! respTKVs, lastPDU = iSCSI_Initiator.ReceiveLoginResponse objID exp_ConnParams.CID conn getCmdSN None
            let negoValue3, negoStat3 = IscsiTextEncode.margeTextKeyValue Standpoint.Initiator negoValue1 respTKVs negoStat2

            if negoValue3.AuthMethod = ISV_Missing || negoValue3.AuthMethod = ISV_NotUnderstood ||
                negoValue3.AuthMethod = ISV_Irrelevant || negoValue3.AuthMethod = ISV_Reject then
                raise <| SessionRecoveryException ( "Unknown negotiation error.", tsih_me.zero )

            let v = negoValue3.AuthMethod.GetValue
            if v.Length = 0 then
                raise <| SessionRecoveryException ( "Unknown negotiation error.", tsih_me.zero )

            if v.[0] = AuthMethodCandidateValue.AMC_CHAP then
                // Need to authenticate
                let! lastPDU2 =
                    iSCSI_Initiator.SecurityNegotiation exp_SessParams exp_ConnParams ( statsn_me.next lastPDU.StatSN ) objID getCmdSN itt conn
                let! negoVal, negoStat, lastTSIH, lastStatSN =
                    iSCSI_Initiator.OperationalNegotiation exp_SessParams exp_ConnParams negoValue3 lastPDU2 objID getCmdSN itt conn isLeadingCon true
                let sessParams, conParams =
                    iSCSI_Initiator.DecideParameters exp_SessParams exp_ConnParams lastTSIH isLeadingCon negoVal negoStat
                return sessParams, conParams, lastStatSN, conn
            else
                let! negoVal, negoStat, lastTSIH, lastStatSN =
                    iSCSI_Initiator.OperationalNegotiation exp_SessParams exp_ConnParams negoValue3 lastPDU objID getCmdSN itt conn isLeadingCon false
                let sessParams, conParams =
                    iSCSI_Initiator.DecideParameters exp_SessParams exp_ConnParams lastTSIH isLeadingCon negoVal negoStat
                return sessParams, conParams, lastStatSN, conn
        }

    /// <summary>
    ///  Perform security negotiation.
    /// </summary>
    /// <param name="exp_SessParams">
    ///  Desired session parameters.
    /// </param>
    /// <param name="exp_ConnParams">
    ///  Desired connection parameters.
    /// </param>
    /// <param name="expStatSN">
    ///  Next ExpStatSN value.
    /// </param>
    /// <param name="objID">
    ///  Object ID for log output.
    /// </param>
    /// <param name="getCmdSN">
    ///  The function to get current CmdSN value.
    /// </param>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <param name="conn">
    ///  Network connection object.
    /// </param>
    /// <returns>
    ///  Last received login response PDU.
    /// </returns>
    static member private SecurityNegotiation
            ( exp_SessParams : SessParams )
            ( exp_ConnParams : ConnParams )
            ( expStatSN : STATSN_T )
            ( objID : OBJIDX_T )
            ( getCmdSN : unit -> CMDSN_T )
            ( itt : ITT_T )
            ( conn : NetworkStream )
            : Task<LoginResponsePDU> =
        task {
            let defaultLoginRequest =
                {
                    T = false;
                    C = false;
                    CSG = LoginReqStateCd.SEQURITY;
                    NSG = LoginReqStateCd.SEQURITY;
                    VersionMax = 0x00uy;
                    VersionMin = 0x00uy;
                    ISID = exp_SessParams.ISID;
                    TSIH = exp_SessParams.TSIH;
                    InitiatorTaskTag = itt;
                    CID = exp_ConnParams.CID;
                    CmdSN = cmdsn_me.zero;
                    ExpStatSN = statsn_me.zero;
                    TextRequest = [||];
                    ByteCount = 0u; // not used
                }

            // Send CHAP_A, and wait receive CHAP_A, CHAP_I, CHAP_C
            let negoValue1 =
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_A = TextValueType.Value( [| 5us |] );
                }
            let negoStat1 =
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive;
                        NegoStat_CHAP_I = NegoStatusValue.NSG_WaitReceive;
                        NegoStat_CHAP_C = NegoStatusValue.NSG_WaitReceive;
                }
            let textReq = IscsiTextEncode.CreateTextKeyValueString negoValue1 negoStat1
            let loginRequest1 =
                {
                    defaultLoginRequest with
                        CmdSN = getCmdSN();
                        ExpStatSN = expStatSN;
                        TextRequest = textReq;
                }
            let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objID, conn, loginRequest1 )

            // receive CHAP_A, CHAP_I, CHAP_C
            let! textResp1, lastPDU1 = iSCSI_Initiator.ReceiveLoginResponse objID exp_ConnParams.CID conn getCmdSN ( Some loginRequest1.ExpStatSN )
            let negoValue2, negoStat2 =
                negoStat1
                |> IscsiTextEncode.ClearSendWaitStatus
                |> IscsiTextEncode.margeTextKeyValue Standpoint.Initiator negoValue1 textResp1

            // CHAP_A, CHAP_I, CHAP_C must be received
            if ( negoStat2.NegoStat_CHAP_A &&& NegoStatusValue.NSG_WaitReceive = NegoStatusValue.NSG_WaitReceive ) ||
                ( negoStat2.NegoStat_CHAP_I &&& NegoStatusValue.NSG_WaitReceive = NegoStatusValue.NSG_WaitReceive ) ||
                ( negoStat2.NegoStat_CHAP_C &&& NegoStatusValue.NSG_WaitReceive = NegoStatusValue.NSG_WaitReceive ) then
                    raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )

            // CHAP_A must contain 5us
            let r = Array.exists ( (=) 5us ) negoValue2.CHAP_A.GetValue
            if not r then
                raise <| SessionRecoveryException ( "CHAP algorithm not supported.", tsih_me.zero )

            // Calculate the value of CHAP_R to respond with
            let responseHashValue =
                ( MD5.Create() ).ComputeHash
                    [|
                        yield byte negoValue2.CHAP_I.GetValue;
                        yield! Encoding.UTF8.GetBytes exp_ConnParams.Initiator_Password;
                        yield! negoValue2.CHAP_C.GetValue;
                    |]

            let! result =
                if exp_ConnParams.Target_UserName = "" then
                    task {
                        // No authentication of the target is required
                        // send CHAP_N, CHAP_R
                        let negoValue3 =
                            {
                                TextKeyValues.defaultTextKeyValues with
                                    CHAP_N = TextValueType.Value( exp_ConnParams.Initiator_UserName );
                                    CHAP_R = TextValueType.Value( responseHashValue );
                            }
                        let negoStat3 =
                            {
                                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                    NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend;
                                    NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend;
                            }

                        let textReq = IscsiTextEncode.CreateTextKeyValueString negoValue3 negoStat3
                        let loginRequest3 =
                            {
                                defaultLoginRequest with
                                    T = true;
                                    NSG = LoginReqStateCd.OPERATIONAL;
                                    CmdSN = getCmdSN();
                                    ExpStatSN = statsn_me.next lastPDU1.StatSN;
                                    TextRequest = textReq;
                            }
                        let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objID, conn, loginRequest3 )

                        let! _, lastPDU3 = iSCSI_Initiator.ReceiveLoginResponse objID exp_ConnParams.CID conn getCmdSN ( Some loginRequest3.ExpStatSN )
                        //let negoValue4, negoStat4 = IscsiTextEncode.margeTextKeyValue textResp3 negoValue3 negoStat3

                        // Login successful
                        return lastPDU3
                    }
                else
                    task {
                        // Target authentication required

                        let rnd1 = new Random()
                        let sendIdentVal = uint16( rnd1.Next() % 0xFF )
                        let cspRand = RandomNumberGenerator.Create()
                        let challangeBuffer : byte[] = Array.zeroCreate 1024
                        cspRand.GetBytes challangeBuffer
                        let negoValue3 =
                            {
                                TextKeyValues.defaultTextKeyValues with
                                    CHAP_N = TextValueType.Value( exp_ConnParams.Initiator_UserName );
                                    CHAP_R = TextValueType.Value( responseHashValue );
                                    CHAP_I = TextValueType.Value( sendIdentVal );
                                    CHAP_C = TextValueType.Value( challangeBuffer );
                            }
                        let negoStat3 =
                            {
                                TextKeyValuesStatus.defaultTextKeyValuesStatus with
                                    NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive;
                                    NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive;
                                    NegoStat_CHAP_I = NegoStatusValue.NSG_WaitSend;
                                    NegoStat_CHAP_C = NegoStatusValue.NSG_WaitSend;
                            }

                        let textReq = IscsiTextEncode.CreateTextKeyValueString negoValue3 negoStat3
                        let loginRequest3 =
                            {
                                defaultLoginRequest with
                                    T = true;
                                    NSG = LoginReqStateCd.OPERATIONAL;
                                    CmdSN = getCmdSN();
                                    ExpStatSN = statsn_me.next lastPDU1.StatSN;
                                    TextRequest = textReq;
                            }
                        let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objID, conn, loginRequest3 )

                        let negoStat4 = IscsiTextEncode.ClearSendWaitStatus negoStat3
                        let negoValue4 = {
                            negoValue3 with
                                // drop CHAP_N and CHAP_R value.
                                // Both the initiator and the target send N and R values.
                                // But N and R values are not merged afterwards. 
                                // Only the target's values ​​are used, so N and R values ​​sent by the initiator must be dropped.
                                CHAP_N = TextValueType.ISV_Missing;
                                CHAP_R = TextValueType.ISV_Missing;
                        }

                        let! textResp3, lastPDU3 = iSCSI_Initiator.ReceiveLoginResponse objID exp_ConnParams.CID conn getCmdSN ( Some loginRequest3.ExpStatSN )
                        let negoValue5, negoStat5 = IscsiTextEncode.margeTextKeyValue Standpoint.Initiator negoValue4 textResp3 negoStat4

                        // CHAP_N, CHAP_R must be received
                        if ( negoStat5.NegoStat_CHAP_N &&& NegoStatusValue.NSG_WaitReceive = NegoStatusValue.NSG_WaitReceive ) ||
                            ( negoStat5.NegoStat_CHAP_R &&& NegoStatusValue.NSG_WaitReceive = NegoStatusValue.NSG_WaitReceive ) then
                                raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )

                        // check target name
                        if negoValue5.CHAP_N.GetValue <> exp_ConnParams.Target_UserName then
                            raise <| SessionRecoveryException ( "Authenticate failed. Invalid target user name.", tsih_me.zero )

                        // calc target response value
                        let expHashValue =
                            ( MD5.Create() ).ComputeHash
                                [|
                                    yield byte sendIdentVal;
                                    yield! Encoding.UTF8.GetBytes exp_ConnParams.Target_Password;
                                    yield! challangeBuffer;
                                |]
                        if expHashValue <> negoValue5.CHAP_R.GetValue then
                            raise <| SessionRecoveryException ( "Authenticate failed. Invalid target response value.", tsih_me.zero )

                        // Login successful
                        return lastPDU3
                    }
            return result
        }


    /// <summary>
    ///  Perform operational negotiation.
    /// </summary>
    /// <param name="exp_SessParams">
    ///  Desired session parameters.
    /// </param>
    /// <param name="exp_ConnParams">
    ///  Desired connection parameters.
    /// </param>
    /// <param name="currentNegoValues">
    ///  Current negotiation values.
    /// </param>
    /// <param name="lastPDU">
    ///  Last received login response PDU.
    /// </param>
    /// <param name="objID">
    ///  Object ID for log output.
    /// </param>
    /// <param name="getCmdSN">
    ///  The function to get current CmdSN value.
    /// </param>
    /// <param name="itt">
    ///  Initiator task tag.
    /// </param>
    /// <param name="conn">
    ///  Network connection object.
    /// </param>
    /// <param name="isLeadingCon">
    ///  Login for leagind connection or not.
    /// </param>
    /// <param name="isAuthentified">
    ///  Security negotiation is performed or not.
    /// </param>
    /// <returns>
    ///  Negotiated text key values and status. 
    ///  TSIH value received from the target.
    ///  StatSN value received from the target.
    /// </returns>
    static member private OperationalNegotiation
            ( exp_SessParams : SessParams )
            ( exp_ConnParams : ConnParams )
            ( currentNegoValues : TextKeyValues )
            ( lastPDU : LoginResponsePDU )
            ( objID : OBJIDX_T )
            ( getCmdSN : unit -> CMDSN_T )
            ( itt : ITT_T )
            ( conn : NetworkStream )
            ( isLeadingCon : bool )
            ( isAuthentified : bool )
            : Task< TextKeyValues * TextKeyValuesStatus * TSIH_T * STATSN_T > =
        task {
            let defaultLoginRequest =
                {
                    T = false;
                    C = false;
                    CSG = LoginReqStateCd.OPERATIONAL;
                    NSG = LoginReqStateCd.OPERATIONAL;
                    VersionMax = 0x00uy;
                    VersionMin = 0x00uy;
                    ISID = exp_SessParams.ISID;
                    TSIH = exp_SessParams.TSIH;
                    InitiatorTaskTag = itt;
                    CID = exp_ConnParams.CID;
                    CmdSN = cmdsn_me.zero;
                    ExpStatSN = statsn_me.next lastPDU.StatSN;
                    TextRequest = [||];
                    ByteCount = 0u; // not used
                }

            // Continue sending empty LoginRequestPDUs until you receive a PDU with T=1 from the target.
            let waitTrance ( expStatSN : STATSN_T ): Task<LoopState< STATSN_T, LoginResponsePDU >> =
                task {
                    // send empty login request pdu
                    let loginRequest =
                        {
                            defaultLoginRequest with
                                T = true;
                                CSG = LoginReqStateCd.SEQURITY;
                                CmdSN = getCmdSN();
                                ExpStatSN = expStatSN;
                        }
                    let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objID, conn, loginRequest )

                    // receive login response pdu
                    let! _, loginResponse = iSCSI_Initiator.ReceiveLoginResponse objID exp_ConnParams.CID conn getCmdSN ( Some expStatSN )

                    // If T bit is not set, try to next one PDU
                    if not loginResponse.T then
                        return LoopState.Continue( statsn_me.next loginResponse.StatSN )
                    else
                        return LoopState.Terminate( loginResponse )
                }
        
            // Perform operation negotiation
            let negoloop ( negoValue, negoStat, initiatorTvalue, expStatSN ) : 
                    Task< LoopState< ( TextKeyValues * TextKeyValuesStatus * bool * STATSN_T ), ( TextKeyValues * TextKeyValuesStatus * TSIH_T * STATSN_T ) > > =
                task {
                    // Send login request PDU
                    let textReq = IscsiTextEncode.CreateTextKeyValueString negoValue negoStat
                    let loginRequest =
                        {
                            defaultLoginRequest with
                                T = initiatorTvalue;
                                NSG = if initiatorTvalue then LoginReqStateCd.FULL else LoginReqStateCd.OPERATIONAL;
                                CmdSN = getCmdSN();
                                ExpStatSN = expStatSN;
                                TextRequest = textReq;
                        }

                    let! lastSentExpStatSN = iSCSI_Initiator.SendNegotiationRequest_InBytes objID conn loginRequest initiatorTvalue getCmdSN expStatSN textReq
                    let negoStat2 = IscsiTextEncode.ClearSendWaitStatus negoStat

                    // Receive Login response PDU
                    let! textKey, recvPDU = iSCSI_Initiator.ReceiveLoginResponse objID exp_ConnParams.CID conn getCmdSN ( Some expStatSN )

                    // check StatSN value
                    if lastSentExpStatSN <> recvPDU.StatSN then
                        let msg = "Unexpected StatSN value."
                        raise <| SessionRecoveryException ( msg, tsih_me.zero )

                    // if keys not allowed in operation negotiation stage is used, drop this connection
                    if textKey.AuthMethod <> TextValueType.ISV_Missing ||
                        textKey.CHAP_A <> TextValueType.ISV_Missing ||
                        textKey.CHAP_I <> TextValueType.ISV_Missing ||
                        textKey.CHAP_C <> TextValueType.ISV_Missing ||
                        textKey.CHAP_N <> TextValueType.ISV_Missing ||
                        textKey.CHAP_R <> TextValueType.ISV_Missing ||
                        textKey.SendTargets <> TextValueType.ISV_Missing ||
                        textKey.TargetAddress <> TextValueType.ISV_Missing then
                        let msg = "Invalid text key was received."
                        raise <| SessionRecoveryException ( msg, tsih_me.zero )

                    // Check reject value
                    // if use existing session, LO parameters must not be handled.
                    if ( not isLeadingCon ) && (
                        textKey.MaxConnections <> TextValueType.ISV_Missing ||
                        textKey.InitialR2T <> TextValueType.ISV_Missing ||
                        textKey.ImmediateData <> TextValueType.ISV_Missing ||
                        textKey.MaxBurstLength <> TextValueType.ISV_Missing ||
                        textKey.FirstBurstLength <> TextValueType.ISV_Missing ||
                        textKey.DefaultTime2Wait <> TextValueType.ISV_Missing ||
                        textKey.DefaultTime2Retain <> TextValueType.ISV_Missing ||
                        textKey.MaxOutstandingR2T <> TextValueType.ISV_Missing ||
                        textKey.DataPDUInOrder <> TextValueType.ISV_Missing ||
                        textKey.DataSequenceInOrder <> TextValueType.ISV_Missing ||
                        textKey.ErrorRecoveryLevel <> TextValueType.ISV_Missing ) then
                            let msg = "Invalid text key was received."
                            raise <| SessionRecoveryException ( msg, tsih_me.zero )

                    // If sequrity negotiation is performed, some text keys are not allowed to use in operational stage.
                    if isAuthentified then
                        if textKey.SessionType <> TextValueType.ISV_Missing ||
                            textKey.InitiatorName <> TextValueType.ISV_Missing ||
                            textKey.TargetName <> TextValueType.ISV_Missing ||
                            textKey.TargetPortalGroupTag <> TextValueType.ISV_Missing then
                            let msg = "Invalid text key was received."
                            raise <| SessionRecoveryException ( msg, tsih_me.zero )

                    // marge parameters
                    let next_negoValue, next_negoStat =
                        IscsiTextEncode.margeTextKeyValue Standpoint.Initiator negoValue textKey negoStat2 
            
                    // decide transit flg
                    // ( If all of initiator value is sended and target says 'T', transit to next stage )
                    let nextInitiatorTvalue =
                        IscsiTextEncode.CheckAllKeyStatus next_negoStat ( fun v -> v &&& NegoStatusValue.NSG_WaitSend <> NegoStatusValue.NSG_WaitSend )

                    // try to next
                    if not ( nextInitiatorTvalue && recvPDU.T ) then
                        return LoopState.Continue( next_negoValue, next_negoStat, nextInitiatorTvalue, statsn_me.next recvPDU.StatSN )
                    else
                        return LoopState.Terminate( next_negoValue, next_negoStat, recvPDU.TSIH, recvPDU.StatSN )
                }

            let! lastPDU2 =
                if not lastPDU.T then
                    Functions.loopAsyncWithArgs waitTrance ( statsn_me.next lastPDU.StatSN )
                else
                    Task.FromResult lastPDU

            let negoValue1 =
                {
                    TextKeyValues.defaultTextKeyValues with
                        HeaderDigest = TextValueType.Value( [| exp_ConnParams.HeaderDigest; DigestType.DST_None; |] );
                        DataDigest = TextValueType.Value( [| exp_ConnParams.DataDigest; DigestType.DST_None; |] );
                        MaxConnections = if isLeadingCon then TextValueType.Value( exp_SessParams.MaxConnections ) else ISV_Missing;
                        InitiatorAlias = TextValueType.Value( exp_SessParams.InitiatorAlias );
                        TargetAlias = ISV_Missing;
                        TargetPortalGroupTag = ISV_Missing;
                        InitialR2T = if isLeadingCon then TextValueType.Value( exp_SessParams.InitialR2T ) else ISV_Missing;
                        ImmediateData = if isLeadingCon then TextValueType.Value( exp_SessParams.ImmediateData ) else ISV_Missing;
                        MaxRecvDataSegmentLength_I = TextValueType.Value( exp_ConnParams.MaxRecvDataSegmentLength_I );
                        MaxRecvDataSegmentLength_T = TextValueType.Value( exp_ConnParams.MaxRecvDataSegmentLength_T );
                        MaxBurstLength = if isLeadingCon then TextValueType.Value( exp_SessParams.MaxBurstLength ) else ISV_Missing;
                        FirstBurstLength = if isLeadingCon then TextValueType.Value( exp_SessParams.FirstBurstLength ) else ISV_Missing;
                        DefaultTime2Wait = if isLeadingCon then TextValueType.Value( exp_SessParams.DefaultTime2Wait ) else ISV_Missing;
                        DefaultTime2Retain = if isLeadingCon then TextValueType.Value( exp_SessParams.DefaultTime2Retain ) else ISV_Missing;
                        MaxOutstandingR2T = if isLeadingCon then TextValueType.Value( exp_SessParams.MaxOutstandingR2T ) else ISV_Missing;
                        DataPDUInOrder = if isLeadingCon then TextValueType.Value( exp_SessParams.DataPDUInOrder ) else ISV_Missing;
                        DataSequenceInOrder = if isLeadingCon then TextValueType.Value( exp_SessParams.DataSequenceInOrder ) else ISV_Missing;
                        ErrorRecoveryLevel = if isLeadingCon then TextValueType.Value( exp_SessParams.ErrorRecoveryLevel ) else ISV_Missing;
                }
            let negoStat1 =
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_HeaderDigest = NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive;
                        NegoStat_DataDigest = NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive;
                        NegoStat_MaxConnections = if isLeadingCon then NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive else NegoStatusValue.NSV_Negotiated;
                        NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitSend;
                        NegoStat_TargetAlias = NegoStatusValue.NSV_Negotiated;
                        NegoStat_TargetPortalGroupTag = if not isAuthentified then NegoStatusValue.NSG_WaitReceive else NegoStatusValue.NSV_Negotiated;
                        NegoStat_InitialR2T = if isLeadingCon then NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive else NegoStatusValue.NSV_Negotiated;
                        NegoStat_ImmediateData = if isLeadingCon then NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive else NegoStatusValue.NSV_Negotiated;
                        NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitSend;
                        NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSG_WaitReceive;
                        NegoStat_MaxBurstLength = if isLeadingCon then NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive else NegoStatusValue.NSV_Negotiated;
                        NegoStat_FirstBurstLength = if isLeadingCon then NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive else NegoStatusValue.NSV_Negotiated;
                        NegoStat_DefaultTime2Wait = if isLeadingCon then NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive else NegoStatusValue.NSV_Negotiated;
                        NegoStat_DefaultTime2Retain = if isLeadingCon then NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive else NegoStatusValue.NSV_Negotiated;
                        NegoStat_MaxOutstandingR2T = if isLeadingCon then NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive else NegoStatusValue.NSV_Negotiated;
                        NegoStat_DataPDUInOrder = if isLeadingCon then NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive else NegoStatusValue.NSV_Negotiated;
                        NegoStat_DataSequenceInOrder = if isLeadingCon then NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive else NegoStatusValue.NSV_Negotiated;
                        NegoStat_ErrorRecoveryLevel = if isLeadingCon then NegoStatusValue.NSG_WaitSend ||| NegoStatusValue.NSG_WaitReceive else NegoStatusValue.NSV_Negotiated;
                }

            let negoValue2, negoStat2 =
                IscsiTextEncode.margeTextKeyValue Standpoint.Initiator currentNegoValues negoValue1 negoStat1 


            return! Functions.loopAsyncWithArgs negoloop ( negoValue2, negoStat2, false, statsn_me.next lastPDU2.StatSN )
        }

    /// <summary>
    ///  Receives a sequence of LoginResponse PDUs with the C bit set to 1.
    /// </summary>
    /// <param name="objID">
    ///  Object ID for log output.
    /// </param>
    /// <param name="argCID">
    ///  CID for connection to used receive the PDU.
    /// </param>
    /// <param name="conn">
    ///  Network connection object. 
    /// </param>
    /// <param name="getCmdSN">
    ///  The function to get current CmdSN value.
    /// </param>
    /// <param name="expStatSN">
    ///  Next ExpStatSN value.
    /// </param>
    /// <returns>
    ///  Received text key values.
    /// </returns>
    static member private ReceiveLoginResponse
        ( objID : OBJIDX_T )
        ( argCID : CID_T )
        ( conn : NetworkStream )
        ( getCmdSN : unit -> CMDSN_T )
        ( expStatSN : STATSN_T option ) // The ExpStatSN in the initial Login Request PDU is meaningless.
                                        // The counter starts from the StatSN value included in the response to the first login request.
                                        // (The initial value is determined by the target.)
        : Task< struct ( TextKeyValues * LoginResponsePDU ) > =
        task {
            // receive login response pdu sequence with c bit.
            let cbitLoop ( expStatSN : STATSN_T option, rv : List< LoginResponsePDU > ) :
                    Task< LoopState< STATSN_T option * List< LoginResponsePDU >, unit > > =
                task {
                    // receive next login response PDU
                    let! wnextLogiPDU = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, conn, Standpoint.Initiator )
                    if wnextLogiPDU.Opcode <> OpcodeCd.LOGIN_RES then
                        raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )
                    let recvPDU = wnextLogiPDU :?> LoginResponsePDU

                    match expStatSN with
                    | Some( x ) ->
                        if recvPDU.StatSN <> x then
                            raise <| SessionRecoveryException ( "Unexpected StatSN was received.", tsih_me.zero )
                    | _ -> ()

                    if recvPDU.Status <> LoginResStatCd.SUCCESS then
                        let msg = sprintf "Unexpected login response status(%s) was received." ( recvPDU.Status.ToString() )
                        raise <| SessionRecoveryException ( msg, tsih_me.zero )

                    rv.Add recvPDU

                    if recvPDU.C then
                        // send to empty login request PDU
                        let emptyPDU = iSCSI_Initiator.CreateLoginRequestPDUfromLoginResponsePDU argCID recvPDU getCmdSN
                        let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objID, conn, emptyPDU )
                        return LoopState.Continue( Some emptyPDU.ExpStatSN, rv )
                    else
                        return LoopState.Terminate()
                }

            let rv = new List<LoginResponsePDU>()

            do! Functions.loopAsyncWithArgs cbitLoop ( expStatSN, rv )
            let pduList = [| for itr in rv -> itr |]

            let reqs_opt, pdu =
                (
                    pduList
                    |> Array.map ( fun x -> x.TextResponse )
                    |> IscsiTextEncode.RecognizeTextKeyData true,
                    Array.last pduList
                )

            if reqs_opt.IsNone then
                // format error
                let msg = "In iSCSI Login response PDU, Text response data is invalid."
                raise <| SessionRecoveryException ( msg, tsih_me.zero )

            // return value( text key-value and last pdu )
            return struct ( reqs_opt.Value, pdu )
        }

    /// <summary>
    ///  Send login request PDU sequence with C bit set to 1.
    /// </summary>
    /// <param name="objID">
    ///  Object ID for log output.
    /// </param>
    /// <param name="conn">
    ///  Network connection object.
    /// </param>
    /// <param name="defLoginReqPDU">
    ///  Default LoginRequestPDU value.
    /// </param>
    /// <param name="argT">
    ///  T bit value.
    /// </param>
    /// <param name="getCmdSN">
    ///  The function to get current CmdSN value.
    /// </param>
    /// <param name="nextExpStatSN">
    ///  Next ExpStatSN value.
    /// </param>
    /// <param name="textReq">
    ///  Text request data to be sent.
    /// </param>
    /// <returns>
    ///  Last sent ExpStatSN value.
    /// </returns>
    static member private SendNegotiationRequest_InBytes
            ( objID : OBJIDX_T )
            ( conn : NetworkStream )
            ( defLoginReqPDU : LoginRequestPDU )
            ( argT : bool )
            ( getCmdSN : unit -> CMDSN_T )
            ( nextExpStatSN : STATSN_T )
            ( textReq : byte[] ) : Task<STATSN_T> =
        task {
            // Divite bytes array into 8192 bytes unit.
            let sendTextResponses =
                let v = Array.chunkBySize 8192 textReq
                if v.Length > 0 then v else [| Array.empty |]

            for i = 0 to sendTextResponses.Length - 1 do
                // Decide C bit value
                let cBitValue = ( i < sendTextResponses.Length - 1 )

                let loginRequest =
                    {
                        defLoginReqPDU with
                            C = cBitValue;
                            T = if cBitValue then false else argT;
                            NSG = if cBitValue || not argT then LoginReqStateCd.SEQURITY else defLoginReqPDU.NSG;   // If T is 0, NSG is reserved
                            CmdSN = getCmdSN();
                            ExpStatSN = statsn_me.incr ( uint i ) nextExpStatSN;
                            TextRequest = sendTextResponses.[i];
                    }
                let! _ = PDU.SendPDU( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, objID, conn, loginRequest )

                if cBitValue then
                    let! wnextLogiPDU = PDU.Receive( 8192u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, conn, Standpoint.Initiator )

                    // Check received PDU
                    if wnextLogiPDU.Opcode <> OpcodeCd.LOGIN_RES then
                        raise <| SessionRecoveryException ( "Unexpected PDU was received.", tsih_me.zero )
                    let emptyLoginResponsePDU = wnextLogiPDU :?> LoginResponsePDU
                    if emptyLoginResponsePDU.TextResponse.Length > 0 || emptyLoginResponsePDU.C then
                        let msg = "Response for Login request PDU with C bit set to 1, TextResponse is not empty."
                        raise <| SessionRecoveryException ( msg, tsih_me.zero )
                    if emptyLoginResponsePDU.StatSN <> loginRequest.ExpStatSN then
                        raise <| SessionRecoveryException ( "Unexpected StatSN value.", tsih_me.zero )
                    if emptyLoginResponsePDU.Status <> LoginResStatCd.SUCCESS then
                        let msg = sprintf "Unexpected login response status(%s) was received." ( emptyLoginResponsePDU.Status.ToString() )
                        raise <| SessionRecoveryException ( msg, tsih_me.zero )

            let lastSendExpStatSN = statsn_me.incr ( uint sendTextResponses.Length - 1u ) nextExpStatSN
            return lastSendExpStatSN
        }

    /// <summary>
    ///  Determine the parameter value from the negotiation result.
    /// </summary>
    /// <param name="defSessParams">
    ///  Desired session parameters.
    /// </param>
    /// <param name="defConnParams">
    ///  Desired connection parameters.
    /// </param>
    /// <param name="lastPDU_TSIH">
    ///  TSIH value received from the target.
    /// </param>
    /// <param name="isLeadingCon">
    ///  Login for leagind connection or not.
    /// </param>
    /// <param name="negoVal">
    ///  Negotiated text key values.
    /// </param>
    /// <param name="negoStat">
    ///  Negotiated text key values status.
    /// </param>
    /// <returns>
    ///  Decided session parameters and connection parameters.
    /// </returns>
    static member private DecideParameters
        ( defSessParams : SessParams )
        ( defConnParams : ConnParams )
        ( lastPDU_TSIH : TSIH_T )
        ( isLeadingCon : bool )
        ( negoVal : TextKeyValues )
        ( negoStat : TextKeyValuesStatus )
            : ( SessParams * ConnParams ) =

        let resultSessParams = {
            InitiatorName = defSessParams.InitiatorName;
            InitiatorAlias = defSessParams.InitiatorAlias;
            TargetName = defSessParams.TargetName;
            TargetAlias = 
                if isLeadingCon && negoVal.TargetAlias.HasValue then
                    negoVal.TargetAlias.GetValue
                else
                    defSessParams.TargetAlias;
            ISID = defSessParams.ISID;
            TSIH = if isLeadingCon then lastPDU_TSIH else defSessParams.TSIH;
            MaxConnections =
                if isLeadingCon && negoStat.NegoStat_MaxConnections = NegoStatusValue.NSV_Negotiated then
                    negoVal.MaxConnections.GetValue
                else
                    defSessParams.MaxConnections;
            InitialR2T =
                if isLeadingCon && negoStat.NegoStat_InitialR2T = NegoStatusValue.NSV_Negotiated then
                    negoVal.InitialR2T.GetValue
                else
                    defSessParams.InitialR2T;
            ImmediateData =
                if isLeadingCon && negoStat.NegoStat_ImmediateData = NegoStatusValue.NSV_Negotiated then
                    negoVal.ImmediateData.GetValue
                else
                    defSessParams.ImmediateData;
            MaxBurstLength =
                if isLeadingCon && negoStat.NegoStat_MaxBurstLength = NegoStatusValue.NSV_Negotiated then
                    negoVal.MaxBurstLength.GetValue
                else
                    defSessParams.MaxBurstLength;
            FirstBurstLength =
                if isLeadingCon && negoStat.NegoStat_FirstBurstLength = NegoStatusValue.NSV_Negotiated then
                    negoVal.FirstBurstLength.GetValue
                else
                    defSessParams.FirstBurstLength;
            DefaultTime2Wait =
                if isLeadingCon && negoStat.NegoStat_DefaultTime2Wait = NegoStatusValue.NSV_Negotiated then
                    negoVal.DefaultTime2Wait.GetValue
                else
                    defSessParams.DefaultTime2Wait;
            DefaultTime2Retain =
                if isLeadingCon && negoStat.NegoStat_DefaultTime2Retain = NegoStatusValue.NSV_Negotiated then
                    negoVal.DefaultTime2Retain.GetValue
                else
                    defSessParams.DefaultTime2Retain;
            MaxOutstandingR2T =
                if isLeadingCon && negoStat.NegoStat_MaxOutstandingR2T = NegoStatusValue.NSV_Negotiated then
                    negoVal.MaxOutstandingR2T.GetValue
                else
                    defSessParams.MaxOutstandingR2T;
            DataPDUInOrder =
                if isLeadingCon && negoStat.NegoStat_DataPDUInOrder = NegoStatusValue.NSV_Negotiated then
                    negoVal.DataPDUInOrder.GetValue
                else
                    defSessParams.DataPDUInOrder;
            DataSequenceInOrder =
                if isLeadingCon && negoStat.NegoStat_DataSequenceInOrder = NegoStatusValue.NSV_Negotiated then
                    negoVal.DataSequenceInOrder.GetValue
                else
                    defSessParams.DataSequenceInOrder;
            ErrorRecoveryLevel =
                if isLeadingCon && negoStat.NegoStat_ErrorRecoveryLevel = NegoStatusValue.NSV_Negotiated then
                    negoVal.ErrorRecoveryLevel.GetValue
                else
                    defSessParams.ErrorRecoveryLevel;
        }
        let resultConnParams = {
            PortNo = defConnParams.PortNo;
            CID = defConnParams.CID;
            Initiator_UserName = defConnParams.Initiator_UserName;
            Initiator_Password = defConnParams.Initiator_Password;
            Target_UserName = defConnParams.Target_UserName;
            Target_Password = defConnParams.Target_Password;
            HeaderDigest = 
                if negoStat.NegoStat_HeaderDigest = NegoStatusValue.NSV_Negotiated then
                    negoVal.HeaderDigest.GetValue.[0]
                else
                    defConnParams.HeaderDigest;
            DataDigest = 
                if negoStat.NegoStat_DataDigest = NegoStatusValue.NSV_Negotiated then
                    negoVal.DataDigest.GetValue.[0]
                else
                    defConnParams.DataDigest;
            MaxRecvDataSegmentLength_I = 
                if negoStat.NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSV_Negotiated then
                    negoVal.MaxRecvDataSegmentLength_I.GetValue
                else
                    defConnParams.MaxRecvDataSegmentLength_I;
            MaxRecvDataSegmentLength_T = 
                if negoStat.NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSV_Negotiated then
                    negoVal.MaxRecvDataSegmentLength_T.GetValue
                else
                    defConnParams.MaxRecvDataSegmentLength_T;
        }
        ( resultSessParams, resultConnParams )

    /// <summary>
    ///  Creates an empty LoginRequest PDU that can be used to request the next LoginResponse PDU in response to a LoginResponse PDU with the C bit set to 1.
    /// </summary>
    /// <param name="argCID">
    ///  CID for connection to used receive the PDU.
    /// </param>
    /// <param name="resPDU">
    ///  LoginResponse PDU with the C bit set to 1.
    /// </param>
    /// <param name="getCmdSN">
    ///  The function to get current CmdSN value.
    /// </param>
    /// <returns>
    ///  Created LoginRequest PDU.
    /// </returns>
    static member private CreateLoginRequestPDUfromLoginResponsePDU ( argCID : CID_T ) ( resPDU : LoginResponsePDU ) ( getCmdSN : unit -> CMDSN_T ) : LoginRequestPDU =
        {
            T = false;
            C = false;
            CSG = resPDU.CSG;
            NSG = resPDU.NSG;
            VersionMax = 0uy;
            VersionMin = 0uy;
            ISID = resPDU.ISID;
            TSIH = resPDU.TSIH;
            InitiatorTaskTag = resPDU.InitiatorTaskTag;
            CID = argCID;
            CmdSN = getCmdSN();
            ExpStatSN = statsn_me.next resPDU.StatSN;
            TextRequest = Array.empty;
            ByteCount = 0u; // not used
        }

    