//=============================================================================
// Haruka Software Storage.
// PDU.fs : Implementation of PDU class.
// PDU class send/receive iSCSI PDU from/to TCP socket.
// And unpack/pack a PDU into/from the byte stream.
// 

//=============================================================================
// Namespace declaration

/// <summary>
///   Definitions of PDU class.
/// </summary>
namespace Haruka.TargetDevice

//=============================================================================
// Import declaration

open System
open System.IO
open System.Runtime.CompilerServices
open System.Net.Sockets
open System.Threading.Tasks

open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Type definition

/// <summary>
///  Internal record type that used to receive PDU.
/// </summary>
/// <param name="m_I">
///  Immidiate flag. If I bit is 1, this value is set to true.
/// </param>
/// <param name="m_Opcode">
///   Opcode value.
/// </param>
/// <param name="m_F">
///   Final flag. If F bit is 1, this value is set to true.
/// </param>
/// <param name="m_OpcodeSpecific0">
///   Opcode specific( 3 bytes )
/// </param>
/// <param name="m_LUNorOpcodeSpecific1">
///   LUN or Opcode specific( 8 bytes )
/// </param>
/// <param name="m_InitiatorTaskTag">
///   Initiator task tag
/// </param>
/// <param name="m_OpcodeSpecific2">
///   Opcode specific( 28 bytes )
/// </param>
/// <param name="m_AHS">
///   Additional header segment
/// </param>
/// <param name="m_DataSegment">
///   Data segment
/// </param>
/// <param name="m_TSIH">
///   Session ID of Receiving this PDU 
/// </param>
/// <param name="m_CID">
///   Connection ID of Receiving this PDU
/// </param>
/// <param name="m_ConCounter">
///   Connection counter value of Receiving this PDU
/// </param>
/// <param name="m_IsTargetSide">
///   If a PDU send/receive at target side, set to true
/// </param>
/// <param name="m_ReceivedByteCount">
///   NUmber of bytes that received from the initiator.
/// </param>
/// <param name="m_ObjID">
///   Object Identifier
/// </param>
[<Struct; IsReadOnly; IsByRefLike>]
type private internalPDUInfo = {
    m_I : bool;
    m_Opcode : OpcodeCd;
    m_F : bool;
    m_OpcodeSpecific0 : byte[];
    m_LUNorOpcodeSpecific1 : byte[];
    m_InitiatorTaskTag : ITT_T;
    m_OpcodeSpecific2 : byte[];
    m_AHS : AHS[];
    m_DataSegment : PooledBuffer;
    m_TSIH : TSIH_T ValueOption;
    m_CID : CID_T ValueOption;
    m_ConCounter : CONCNT_T ValueOption;
    m_Standpoint : Standpoint;
    m_ReceivedByteCount : uint32;
    m_ObjID : OBJIDX_T;
}

//=============================================================================
// Class definition

/// <summary>
///  iSCSI PDU data stracture. PDU class supports receiving bytes stream and marshalling to internal data structure,
///  or marshalling PDU data to bytes stream and sending this.
/// </summary>
type PDU() =
   
    // ------------------------------------------------------------------------
    /// <summary>
    ///   Receive PDU data
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Negotiated MaxRecvDataSegmentLength value. This valued used to check received data segment length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Header digest is used or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Data digest is used or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH of session which the connection of 'sock' is belonging to.
    ///   This argument is used to output to log file and thrown as exception.
    /// </param>
    /// <param name="argCID">
    ///   Connection ID of the connection of 'sock'.
    ///   This argument is used to output to log file and thrown as exception.
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the connection of 'sock'.
    ///   This argument is used to output to log file and thrown as exception.
    /// </param>
    /// <param name="sock">
    ///   Socket that is used to receive the PDU.
    /// </param>
    /// <param name="isTargetSide">
    ///   The PDU is received at target side or not.
    ///   This argument is used to check opcode value.
    /// </param>
    static member Receive
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            sock : Stream,
            standpoint : Standpoint
        ) : Task<ILogicalPDU> =

            task {
                // Generate object ID value for this object
                let objid = objidx_me.NewID()
                let loginfo = struct ( objid, argCID, argCounter, argTSIH, ValueNone, ValueNone )

                // receive BHS data
                let wbufBHS = PooledBuffer.Rent 48
                do! PDU.ReceiveBytes( sock, wbufBHS.Array, 48, argTSIH, argCID, argCounter, objid )

                // Check the TotalAHSLength
                if ( wbufBHS.[4] &&& 0x03uy ) > 0uy then
                    let msg = sprintf "Invalid TotalAHSLength(%d). TotalAHSLength should be padded in 4 bytes word." ( int wbufBHS.[4] )
                    HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                    raise <| SessionRecoveryException ( msg, tsih_me.fromValOpt 0us argTSIH )

                // receive all of AHS data
                let wbufAllAHS = PooledBuffer.Rent( int wbufBHS.[4] )
                do! PDU.ReceiveBytes( sock, wbufAllAHS.Array, ( int wbufBHS.[4] ), argTSIH, argCID, argCounter, objid )

                // receive HeaderDigest if digest is suppied
                let headerDigestLen = if argHeaderDigest = DigestType.DST_CRC32C then 4 else 0
                if headerDigestLen > 0 then
                    let wbufHeaderDigest = Array.zeroCreate<byte> headerDigestLen
                    do! PDU.ReceiveBytes( sock, wbufHeaderDigest, headerDigestLen, argTSIH, argCID, argCounter, objid )

                    // Check header digest
                    let crc = Functions.CRC32_AS [| wbufBHS.ArraySegment; wbufAllAHS.ArraySegment; |]
                    if crc <> Functions.NetworkBytesToUInt32 wbufHeaderDigest 0 then
                        HLogger.Trace( LogID.E_HEADER_DIGEST_ERROR, fun g -> g.Gen0 loginfo )
                        raise <| ConnectionErrorException( "Header digest error.", tsih_me.fromValOpt 0us argTSIH, cid_me.fromValOpt 0us argCID )

                // calculate data segment length  
                let wDataSegmentLength = ( uint32( wbufBHS.[5] ) <<< 16 ) + ( uint32( wbufBHS.[6] ) <<< 8 ) + uint32( wbufBHS.[7] )

                // Add padding count to DataSegmentLength
                let wDataSegmentLengthWithPadd = Functions.AddPaddingLengthUInt32 wDataSegmentLength 4u

                // if the data segment length over MaxRecvDataSegmentLength,
                if wDataSegmentLength > argMaxRecvDataSegmentLength then
                    let msg = sprintf "Data segment length(%d) over MaxRecvDataSegmentLength(%d). " wDataSegmentLength argMaxRecvDataSegmentLength
                    HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                    raise <| SessionRecoveryException ( msg, tsih_me.fromValOpt 0us argTSIH )

                // receive DataSegment
                let wbufDataSegment = PooledBuffer.Rent ( int wDataSegmentLength )
                do! PDU.ReceiveBytes( sock, wbufDataSegment.Array, ( int wDataSegmentLength ), argTSIH, argCID, argCounter, objid )

                // receive DataSegment padding bytes
                let dataSegmentPaddingLen = int wDataSegmentLengthWithPadd - int wDataSegmentLength
                let wbufDataSegmentPadding = Array.zeroCreate<byte> dataSegmentPaddingLen
                if dataSegmentPaddingLen > 0 then
                    do! PDU.ReceiveBytes( sock, wbufDataSegmentPadding, dataSegmentPaddingLen, argTSIH, argCID, argCounter, objid )

                // receive DataDigest if digest is suppied and DataSegment length is non zero.
                let dataDigestLen = if argDataDigest = DigestType.DST_CRC32C && wbufDataSegment.Count > 0 then 4 else 0
                if dataDigestLen > 0 then
                    let wbufDataDigest = Array.zeroCreate<byte> dataDigestLen
                    do! PDU.ReceiveBytes( sock, wbufDataDigest, dataDigestLen, argTSIH, argCID, argCounter, objid )
 
                    // Check data digest
                    let crc = Functions.CRC32_AS [| wbufDataSegment.ArraySegment; ArraySegment wbufDataSegmentPadding |]
                    if crc <> Functions.NetworkBytesToUInt32 wbufDataDigest 0 then
                        HLogger.Trace( LogID.W_DATA_DIGEST_ERROR, fun g -> g.Gen0 loginfo )
                        match standpoint with
                        | Standpoint.Target ->
                            // On target side, data digest error is handled by responding a reject PDU.
                            let header = Seq.append wbufBHS.ArraySegment wbufAllAHS.ArraySegment |> Seq.toArray
                            raise <| RejectPDUException( "Data digest error. ", RejectReasonCd.DATA_DIGEST_ERR, header )
                        | _ ->
                            // On initiator side, data digest error is handled by discarding received PDU.
                            raise <| DiscardPDUException( "Data digest error. Received PDU is discarded." )

                let wOpcode = Constants.byteToOpcodeCd ( wbufBHS.[0] &&& 0x3Fuy ) ( fun v ->
                    let msg = sprintf "Invalid Opcode(0x%02X)." v
                    HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                    let header = Seq.append wbufBHS.ArraySegment wbufAllAHS.ArraySegment |> Seq.toArray
                    raise <| RejectPDUException( msg, RejectReasonCd.COM_NOT_SUPPORT, header )
                )

                // Check opcode
                match standpoint with
                | Standpoint.Target ->
                    if wOpcode <> OpcodeCd.NOP_OUT && 
                        wOpcode <> OpcodeCd.SCSI_COMMAND &&
                        wOpcode <> OpcodeCd.SCSI_TASK_MGR_REQ &&
                        wOpcode <> OpcodeCd.LOGIN_REQ &&
                        wOpcode <> OpcodeCd.TEXT_REQ &&
                        wOpcode <> OpcodeCd.SCSI_DATA_OUT &&
                        wOpcode <> OpcodeCd.LOGOUT_REQ &&
                        wOpcode <> OpcodeCd.SNACK then

                        let msg = sprintf "Invalid Opcode(0x%02X). iSCSI target node expects receiving initiator Opcode only in Opcode field." ( byte wOpcode )
                        HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                        // If Opcode is invalid, it must return Reject PDU.
                        let header = Seq.append wbufBHS.ArraySegment wbufAllAHS.ArraySegment |> Seq.toArray
                        raise <| RejectPDUException( msg, RejectReasonCd.COM_NOT_SUPPORT, header )
                | _ ->
                    if wOpcode <> OpcodeCd.NOP_IN && 
                        wOpcode <> OpcodeCd.SCSI_RES &&
                        wOpcode <> OpcodeCd.SCSI_TASK_MGR_RES &&
                        wOpcode <> OpcodeCd.LOGIN_RES &&
                        wOpcode <> OpcodeCd.TEXT_RES &&
                        wOpcode <> OpcodeCd.SCSI_DATA_IN &&
                        wOpcode <> OpcodeCd.LOGOUT_RES &&
                        wOpcode <> OpcodeCd.R2T &&
                        wOpcode <> OpcodeCd.ASYNC &&
                        wOpcode <> OpcodeCd.REJECT then

                        let msg = sprintf "Invalid Opcode(0x%02X). iSCSI initiator node expects receiving target Opcode only in Opcode field." ( byte wOpcode )
                        HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                        // If Opcode is invalid, initiator discards received PDU
                        raise <| DiscardPDUException( msg )

                let w = {
                    m_I = Functions.CheckBitflag wbufBHS.[0] Constants.IMMIDIATE_BIT;
                    m_Opcode = wOpcode;    // 0x3F mask reserved and Immidiate bit
                    m_F = Functions.CheckBitflag wbufBHS.[1] Constants.FINAL_BIT;
                    m_OpcodeSpecific0 = [| wbufBHS.[1] &&& ~~~ Constants.FINAL_BIT;  wbufBHS.[2]; wbufBHS.[3] |];
                    m_LUNorOpcodeSpecific1 = wbufBHS.GetPartialBytes 8 15;
                    m_InitiatorTaskTag = Functions.NetworkBytesToUInt32_InPooledBuffer wbufBHS 16 |> itt_me.fromPrim;
                    m_OpcodeSpecific2 = wbufBHS.GetPartialBytes 20 47;
                    m_AHS = PDU.ParseAHSData wbufAllAHS argTSIH argCID argCounter objid;
                    m_DataSegment = wbufDataSegment;
                    m_TSIH = argTSIH;
                    m_CID = argCID;
                    m_ConCounter = argCounter;
                    m_Standpoint = standpoint;
                    m_ReceivedByteCount =
                        wbufBHS.Count + wbufAllAHS.Length + headerDigestLen +
                        wbufDataSegment.Count + dataSegmentPaddingLen + dataDigestLen
                        |> uint32
                    m_ObjID = objid;
                }
                wbufBHS.Return()
                wbufAllAHS.Return()
                return PDU.toLogicalPDU w
            }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Receive bytes sequense from the network peer.
    /// </summary>
    /// <param name="s">
    ///   The stream that used to receive specified bytes.
    /// </param>
    /// <param name="buf">
    ///   The buffer which the received data are written to.
    /// </param>
    /// <param name="count">
    ///   Byte count that should be received.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH of session which the connection of 's' is belonging to.
    ///   This argument is used to output to log file and thrown as exception.
    /// </param>
    /// <param name="argCID">
    ///   Connection ID of the connection of 's'.
    ///   This argument is used to output to log file and thrown as exception.
    /// </param>
    /// <param name="argConCounter">
    ///   Connection counter value of the connection of 's'.
    ///   This argument is used to output to log file and thrown as exception.
    /// </param>
    /// <param name="argObjID">
    ///   Object ID of the object that calls this method.
    /// </param>
    /// <param name="fnname">
    ///   Function name of the function that calls this method. This argument is used to output to log file.
    /// </param>
    /// <param name="source">
    ///   The source code file name. This argument is used to output to log file.
    /// </param>
    /// <param name="line">
    ///   The source code line number of caller function.
    ///   This argument is used to output to log file.
    /// </param>
    static member private ReceiveBytes
            (
                ( s : Stream ),
                ( buf : byte[] ),
                ( count : int ),
                ( argTSIH : TSIH_T ValueOption ),
                ( argCID : CID_T ValueOption ) ,
                ( argConCounter : CONCNT_T ValueOption ),
                ( argObjID : OBJIDX_T ) ,
                [<CallerMemberName>] ?fnname : string,
                [<CallerFilePath>] ?source : string,
                [<CallerLineNumber>] ?line: int
            ) : Task<unit> =

        task {
            let wtsih = tsih_me.fromValOpt 0us argTSIH
            let wcid = cid_me.fromValOpt 0us argCID
            try
                do! s.ReadExactlyAsync( buf, 0, count )
            with
            | :? EndOfStreamException as x ->
                HLogger.Trace( LogID.E_CONNECTION_CLOSED, fun g ->
                    g.Gen0( argObjID, argCID, argConCounter, argTSIH, ValueNone, ValueNone, (), fnname.Value, source.Value, line.Value )
                )
                raise <| ConnectionErrorException( "Connection closed.", wtsih, wcid )
            | :? IOException
            | :? ObjectDisposedException as x ->
                HLogger.Trace( LogID.E_PDU_RECEIVE_ERROR, fun g ->
                    g.Gen1( argObjID, argCID, argConCounter, argTSIH, ValueNone, ValueNone, x.Message, (), fnname.Value, source.Value, line.Value )
                )
                raise <| ConnectionErrorException( "Connection closed.", wtsih, wcid )
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Parse the received AHS byte sequence to AHS structure.
    /// </summary>
    /// <param name="argAHSBuf">
    ///   Received AHS data bytes.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH of session which the connection receiving the data is belonging to.
    ///   This argument is used to output to log file and thrown as exception.
    /// </param>
    /// <param name="argCID">
    ///   Connection ID of the connection which AHS data is received.
    ///   This argument is used to output to log file and thrown as exception.
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the connection of 'sock'.
    ///   This argument is used to output to log file and thrown as exception.
    /// </param>
    /// <param name="argObjID">
    ///   Object ID of the object that calls this method.
    /// </param>
    /// <returns>
    ///   Structured AHS data.
    /// </returns>
    static member private ParseAHSData
        ( argAHSBuf : PooledBuffer )
        ( argTSIH : TSIH_T ValueOption )
        ( argCID : CID_T ValueOption ) 
        ( argCounter : CONCNT_T ValueOption )
        ( argObjID : OBJIDX_T ) : AHS[] =

        let loginfo = struct( argObjID, argCID, argCounter, argTSIH, ValueNone, ValueNone )
        let rec loop ( pos : int ) ( cont : AHS list -> AHS list )  =
            // get AHSLength value
            let wAHSLength = ( uint16 argAHSBuf.[ pos + 0 ] <<< 8 ) + uint16 argAHSBuf.[ pos + 1 ]

            // Add padding count and other field length to AHSLength
            let wAHSLengthWithPadd : int = int ( Functions.AddPaddingLengthUInt16 ( wAHSLength + 3us ) 4us )
                
            let wAHSType = Constants.byteToAHSTypeCd argAHSBuf.[ pos + 2 ] ( fun v ->
                let msg = sprintf "Invalid AHSType(%d) value in AHS." v
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, tsih_me.fromValOpt 0us argTSIH )
            )

            // Check AHSType value
            if wAHSType <> AHSTypeCd.EXTENDED_CDB && wAHSType <> AHSTypeCd.EXPECTED_LENGTH then
                let msg = sprintf "Invalid AHSType(%d) value in AHS." ( byte wAHSType )
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, tsih_me.fromValOpt 0us argTSIH )

            // Check AHSLength value
            if pos + wAHSLengthWithPadd > argAHSBuf.Length then
                let msg = sprintf "AHSLength(%d) and TotalAHSLength(%d) in AHS are mismatch." wAHSLength argAHSBuf.Length
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, tsih_me.fromValOpt 0us argTSIH )

            if wAHSType = AHSTypeCd.EXTENDED_CDB && wAHSLength < 2us then
                let msg = sprintf "In extended CDB AHS, AHSLength(%d) must be greater than 1." wAHSLength
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, tsih_me.fromValOpt 0us argTSIH )

            if wAHSType = AHSTypeCd.EXPECTED_LENGTH && wAHSLength <> 5us then
                let msg = sprintf "In Expected Bidirectional Read Data Length AHS, AHSLength(%d) must be 5." wAHSLength
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, tsih_me.fromValOpt 0us argTSIH )

            let ahs = {
                AHSLength = wAHSLength;
                AHSType = wAHSType;
                AHSSpecific1 = argAHSBuf.[ pos + 3 ];
                AHSSpecific2 = argAHSBuf.Array.[ pos + 4 .. pos + 3 + int wAHSLength - 1 ];
            }
            if pos + wAHSLengthWithPadd < argAHSBuf.Length then
                loop ( pos + wAHSLengthWithPadd ) ( fun wl -> cont( ahs :: wl ) )
            else
                cont [ ahs ]
        if argAHSBuf.Length > 0 then
            List.toArray ( loop 0 id )
        else
            Array.empty

    // --------------------------------------------------------------------
    // Get Logical PDU.
    static member private toLogicalPDU( a : internalPDUInfo ) : ILogicalPDU =
        match a.m_Opcode with
        | OpcodeCd.NOP_IN ->
            PDU.toNOPInPDU a
        | OpcodeCd.SCSI_RES ->
            PDU.toSCSIResponsePDU a
        | OpcodeCd.SCSI_TASK_MGR_RES ->
            PDU.toTaskManagementFunctionResponsePDU a
        | OpcodeCd.LOGIN_RES ->
            PDU.toLoginResponsePDU a
        | OpcodeCd.TEXT_RES ->
            PDU.toTextResponsePDU a
        | OpcodeCd.SCSI_DATA_IN ->
            PDU.toSCSIDataInPDU a
        | OpcodeCd.LOGOUT_RES ->
            PDU.toLogoutResponsePDU a
        | OpcodeCd.R2T ->
            PDU.toR2TPDU a
        | OpcodeCd.ASYNC ->
            PDU.toAsyncronousMessagePDU a
        | OpcodeCd.REJECT ->
            PDU.toRejectPDU a
        | OpcodeCd.NOP_OUT ->
            PDU.toNOPOutPDU a
        | OpcodeCd.SCSI_COMMAND ->
            PDU.toSCSICommandPDU a
        | OpcodeCd.SCSI_TASK_MGR_REQ ->
            PDU.toTaskManagementFunctionRequestPDU a
        | OpcodeCd.LOGIN_REQ ->
            PDU.toLoginRequestPDU a
        | OpcodeCd.TEXT_REQ ->
            PDU.toTextRequestPDU a
        | OpcodeCd.SCSI_DATA_OUT ->
            PDU.toSCSIDataOutPDU a
        | OpcodeCd.LOGOUT_REQ ->
            PDU.toLogoutRequestPDU a
        | OpcodeCd.SNACK ->
            PDU.toSNACKRequestPDU a
        | _ ->
            let msg = sprintf "Unknown Opcode value(0x%02X)." ( byte a.m_Opcode )
            let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, tsih_me.fromValOpt 0us a.m_TSIH )

    // --------------------------------------------------------------------
    // Get SCSI Command PDU
    static member private toSCSICommandPDU ( a : internalPDUInfo ) : SCSICommandPDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not SCSI Command, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.SCSI_COMMAND )

        // Receiving SCSI Command PDU is occured on target only.
        assert( a.m_Standpoint.IsTarget )

        // Search AHS
        let extendedCDBAHS =
            if Array.exists ( AHS.getAHSType >> (=) AHSTypeCd.EXTENDED_CDB ) a.m_AHS then
                Array.findIndex( AHS.getAHSType >> (=) AHSTypeCd.EXTENDED_CDB ) a.m_AHS
            else
                -1
        let expectedLengthAHS =
            if Array.exists ( AHS.getAHSType >> (=) AHSTypeCd.EXPECTED_LENGTH ) a.m_AHS then
                Array.findIndex( AHS.getAHSType >> (=) AHSTypeCd.EXPECTED_LENGTH ) a.m_AHS
            else
                -1

        // Create returing data structure.
        let retvalue = {
            I = a.m_I;
            F = a.m_F;
            R = Functions.CheckBitflag a.m_OpcodeSpecific0.[0] Constants.READ_BIT;
            W = Functions.CheckBitflag a.m_OpcodeSpecific0.[0] Constants.WRITE_BIT;
            ATTR = Constants.byteToTaskATTRCdCd ( a.m_OpcodeSpecific0.[0] &&& 0x07uy ) ( fun x ->
                // It is considered a format error.
                let msg = sprintf "Invalid ATTR(0x%02X) field value in SCSI Command PDU." x
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
            );
            LUN = lun_me.fromBytes a.m_LUNorOpcodeSpecific1 0;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            ExpectedDataTransferLength = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 0;
            CmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> cmdsn_me.fromPrim;
            ExpStatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> statsn_me.fromPrim;
            ScsiCDB =
                if extendedCDBAHS = -1 then
                    a.m_OpcodeSpecific2.[12 .. 27]
                else
                    Array.append a.m_OpcodeSpecific2.[12 .. 27] a.m_AHS.[extendedCDBAHS].AHSSpecific2
            DataSegment = a.m_DataSegment;
            BidirectionalExpectedReadDataLength = 
                if expectedLengthAHS = -1 then
                    0u
                else
                    Functions.NetworkBytesToUInt32 a.m_AHS.[expectedLengthAHS].AHSSpecific2 0
            ByteCount = a.m_ReceivedByteCount;
        }

        // Check W, F bit value
        if not retvalue.F && not retvalue.W then
            let msg = "Both W and F bit in SCSI command PDU are 0. It is considered a PDU format error."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )
       
        // If Expected data transfer length or Bidirectional Expected Read-Data Length is non 0,
        // at least one of R and W are must be 1.
        if not retvalue.R && not retvalue.W &&
            ( retvalue.ExpectedDataTransferLength > 0u || retvalue.BidirectionalExpectedReadDataLength > 0u ) then
                let msg = "Both W and R bit in SCSI command PDU are 0, but Expected Data Transfer Length or Bidirectional Expected Read-Data Length is not 0."
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )

        // Check R, W flags and AHS
        if retvalue.R && retvalue.W && expectedLengthAHS = -1 then
            let msg = "In bidirectional operation, expected length AHS is needed."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )
        
        // Check F, W flag and expected data length
        if retvalue.W && retvalue.ExpectedDataTransferLength = uint32 a.m_DataSegment.Count then
            // If write expected data transfer length equals following immidiate data length, F bit is must be 1.
            if not retvalue.F then
                let msg = "If there are no following data PDU, F bit is must be 1."
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
        
        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get SCSI Response PDU
    static member private toSCSIResponsePDU ( a : internalPDUInfo ) : SCSIResponsePDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not SCSI Response, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.SCSI_RES )

        // Receiving SCSI Command PDU is occured on initiator only.
        assert( a.m_Standpoint.IsInitiator )

        // In SCSI Response PDU, if DataSegment is not empty, length of DataSegment is greater than or equal 2.
        if a.m_DataSegment.Count <> 0 && a.m_DataSegment.Count < 2 then
            let msg = "In SCSI Response PDU, DataSegment length must be 0 or greator than 1."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )
            
        // Parse sense data in DataSegment.
        let wSenseLength =
            if a.m_DataSegment.Count > 0 then
                Functions.NetworkBytesToUInt16_InPooledBuffer a.m_DataSegment 0
            else
                0us

        if a.m_DataSegment.Count > 0 && ( int wSenseLength > a.m_DataSegment.Count - 2 ) then
            let msg =
                sprintf
                    "In SCSI Response PDU, SenseLength(%d) must be less than data segment length(%d) - 2 bytes."
                    wSenseLength
                    ( a.m_DataSegment.Count )
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        let wSenseData =
            if wSenseLength > 0us then
                a.m_DataSegment.GetArraySegment 2 ( int wSenseLength )
                //ArraySegment( a.m_DataSegment.Array, 2, int wSenseLength )
            else
                ArraySegment.Empty
        let wResponseData =
            if int wSenseLength + 2 < a.m_DataSegment.Count then
                a.m_DataSegment.GetArraySegment ( int wSenseLength + 2 ) ( a.m_DataSegment.Count - ( int wSenseLength + 2 ) )
                //ArraySegment( a.m_DataSegment.Array, int wSenseLength + 2, a.m_DataSegment.Count - ( int wSenseLength + 2 ) )
            else
                ArraySegment.Empty

        // Create returing data structure.
        let wFlagsByte = a.m_OpcodeSpecific0.[0]
        let wResponseByte = a.m_OpcodeSpecific0.[1]
        let wStatusByte = a.m_OpcodeSpecific0.[2]
        let retvalue = {
            o = Functions.CheckBitflag wFlagsByte Constants.BI_READ_RESIDUAL_OV_BIT;
            u = Functions.CheckBitflag wFlagsByte Constants.BI_READ_RESIDUAL_UND_BIT;
            O = Functions.CheckBitflag wFlagsByte Constants.RESIDUAL_OVERFLOW_BIT;
            U = Functions.CheckBitflag wFlagsByte Constants.RESIDUAL_UNDERFLOW_BIT;
            Response = Constants.byteToiScsiSvcRespCd wResponseByte ( fun _ ->
                let msg = sprintf "In SCSI response PDU, Response(0x%02X) field value is invalid." wResponseByte
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih );
            );
            Status = 
                if wResponseByte = byte iScsiSvcRespCd.COMMAND_COMPLETE then
                    Constants.byteToScsiCmdStatCd wStatusByte ( fun _ ->
                        let msg = sprintf "In SCSI response PDU, Status(0x%02X) field value is invalid." wStatusByte
                        HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                        raise <| SessionRecoveryException ( msg, wtsih )
                    )
                else
                    ScsiCmdStatCd.GOOD;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            SNACKTag = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 0 |> snacktag_me.fromPrim;
            StatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> statsn_me.fromPrim;
            ExpCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> cmdsn_me.fromPrim;
            MaxCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 12 |> cmdsn_me.fromPrim;
            ExpDataSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 16 |> datasn_me.fromPrim;
            BidirectionalReadResidualCount = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 20;
            ResidualCount = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 24;
            SenseLength = wSenseLength;
            SenseData = wSenseData;
            ResponseData = wResponseData;
            ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
            DataInBuffer = a.m_DataSegment;
        }

        // o, u flag are mutually exclusive.
        if retvalue.o && retvalue.u then
            let msg = "o and u bit in SCSI response PDU are mutually exclusive."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // O, U flag are mutually exclusive.
        if retvalue.O && retvalue.U then
            let msg = "O and U bit in SCSI response PDU are mutually exclusive."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // if Respons is not "Commanf Complete at Target", o, u, O, U is must be 0.
        if retvalue.Response <> iScsiSvcRespCd.COMMAND_COMPLETE then
            if retvalue.o || retvalue.u || retvalue.O || retvalue.U then
                let msg = "In SCSI response PDU, if Response field is not \"Command Complete at Target\", o, u, O and U bit are must be 0."
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
        
        // return created data
        retvalue

    // --------------------------------------------------------------------
    // Get Task management function request PDU
    static member private toTaskManagementFunctionRequestPDU ( a : internalPDUInfo ) : TaskManagementFunctionRequestPDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not Task management function request, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.SCSI_TASK_MGR_REQ )

        // Receiving Task management function request PDU is occured on target only.
        assert( a.m_Standpoint.IsTarget );
        
        // AHS must be empty
        if a.m_AHS.Length > 0 then
            let msg = "In Task management function request PDU, TotalAHSLength must be 0 and AHS must be empty."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // DataSegment must be empty.
        if a.m_DataSegment.Count > 0 then
            let msg = "In Task management function request PDU, DataSegmentLength must be 0 and DataSegment must be empty."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Create returing data structure.
        let wFunctionByte = a.m_OpcodeSpecific0.[0]
        let retvalue = {
            I = a.m_I;
            Function = Constants.byteToTaskMgrReqCd ( wFunctionByte &&& 0x7Fuy ) ( fun _ ->
                let msg = sprintf "In Task management function request PDU, Function(0x%02X) fileld value is invalid." wFunctionByte
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
            );
            LUN = lun_me.fromBytes a.m_LUNorOpcodeSpecific1 0;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            ReferencedTaskTag = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 0 |> itt_me.fromPrim;
            CmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> cmdsn_me.fromPrim;
            ExpStatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> statsn_me.fromPrim;
            RefCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 12 |> cmdsn_me.fromPrim;
            ExpDataSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 16 |> datasn_me.fromPrim;
            ByteCount = a.m_ReceivedByteCount;
        }

        // Check ReferencedTaskTag field value
        if retvalue.Function <> TaskMgrReqCd.ABORT_TASK &&
            retvalue.Function <> TaskMgrReqCd.TASK_REASSIGN &&
            retvalue.ReferencedTaskTag <> itt_me.fromPrim 0xFFFFFFFFu then
            let msg =
                sprintf
                    "In Task management function request PDU, If Function field is not \"ABORT TASK\" and \"TASK REASSIGN\", ReferencedTaskTag(0x%08X) fileld must be set 0xFFFFFFFF."
                    ( byte retvalue.Function )
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )
        
        // Return created data
        retvalue
  
    // --------------------------------------------------------------------
    // Get Task management function response PDU
    static member private toTaskManagementFunctionResponsePDU ( a : internalPDUInfo ) : TaskManagementFunctionResponsePDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not Task management function response, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.SCSI_TASK_MGR_RES )

        // Receiving Task management function response PDU is occured on initiator only.
        assert( a.m_Standpoint.IsInitiator );

        // AHS must be empty
        if a.m_AHS.Length > 0 then
            let msg = "In Task management function response PDU, TotalAHSLength must be 0 and AHS must be empty."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // DataSegment must be empty.
        if a.m_DataSegment.Count > 0 then
            let msg = "In Task management function response PDU, DataSegmentLength must be 0 and DataSegment must be empty."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Create returing data structure.
        let wResponseByte = a.m_OpcodeSpecific0.[1]
        let retvalue = {
            Response = Constants.byteToTaskMgrResCd wResponseByte ( fun _ ->
                let msg = sprintf "In Task management function response PDU, Response(0x%02X) field value is invalid." wResponseByte
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
            );
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            StatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> statsn_me.fromPrim;
            ExpCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> cmdsn_me.fromPrim;
            MaxCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 12 |> cmdsn_me.fromPrim;
            ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
        }

        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get SCSI Data-Out PDU
    static member private toSCSIDataOutPDU ( a : internalPDUInfo ) : SCSIDataOutPDU =

        // Check Opcode value ( If Opcode is not SCSI Data-Out, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.SCSI_DATA_OUT )

        // Receiving SCSI Data-Out PDU is occured on target only.
        assert( a.m_Standpoint.IsTarget );

        // Create returing data structure.
        let retvalue = {
            F = a.m_F;
            LUN = lun_me.fromBytes a.m_LUNorOpcodeSpecific1 0;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            TargetTransferTag = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 0 |> ttt_me.fromPrim;
            ExpStatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> statsn_me.fromPrim;
            DataSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 16 |> datasn_me.fromPrim;
            BufferOffset = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 20;
            DataSegment = a.m_DataSegment;
            ByteCount = a.m_ReceivedByteCount;
        }

        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get SCSI Data-In PDU
    static member private toSCSIDataInPDU ( a : internalPDUInfo ) : SCSIDataInPDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not SCSI Data-In, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.SCSI_DATA_IN )

        // Receiving SCSI Data-In PDU is occured on initiator only.
        assert( a.m_Standpoint.IsInitiator );

        // Create returing data structure.
        let wFlagsByte = a.m_OpcodeSpecific0.[0]
        let wStatusByte = a.m_OpcodeSpecific0.[2]
        let retvalue = {
            F = a.m_F;
            A = Functions.CheckBitflag wFlagsByte Constants.ACKNOWLEDGE_BIT;
            O = Functions.CheckBitflag wFlagsByte Constants.RESIDUAL_OVERFLOW_BIT;
            U = Functions.CheckBitflag wFlagsByte Constants.RESIDUAL_UNDERFLOW_BIT;
            S = Functions.CheckBitflag wFlagsByte Constants.STATUS_BIT;
            Status = 
                if Functions.CheckBitflag wFlagsByte Constants.STATUS_BIT then
                    Constants.byteToScsiCmdStatCd wStatusByte ( fun _ ->
                        let msg = sprintf "In SCSI Data-In PDU, Status(0x%02X) field value is invalid." wStatusByte
                        HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                        raise <| SessionRecoveryException ( msg, wtsih )
                    )
                else
                    ScsiCmdStatCd.GOOD;
            LUN = lun_me.fromBytes a.m_LUNorOpcodeSpecific1 0;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            TargetTransferTag = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 0 |> ttt_me.fromPrim;
            StatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> statsn_me.fromPrim;
            ExpCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> cmdsn_me.fromPrim;
            MaxCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 12 |> cmdsn_me.fromPrim;
            DataSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 16 |> datasn_me.fromPrim;
            BufferOffset = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 20;
            ResidualCount = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 24;
            DataSegment = a.m_DataSegment.ArraySegment;
            ResponseFence = ResponseFenceNeedsFlag.Irrelevant;
        }

        // Check F bit & S bit value
        if  retvalue.S && not retvalue.F then
            let msg = "In SCSI Data-In PDU, if S bit set to 1, F bit must be 1."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )
        
        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get R2T PDU
    static member private toR2TPDU ( a : internalPDUInfo ) : R2TPDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not R2T, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.R2T )

        // Receiving R2T PDU is occured on initiator only.
        assert( a.m_Standpoint.IsInitiator );

        // AHS must be empty
        if a.m_AHS.Length > 0 then
            let msg = "In R2T PDU, TotalAHSLength must be 0 and AHS must be empty."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // DataSegment must be empty.
        if a.m_DataSegment.Count > 0 then
            let msg = "In R2T PDU, DataSegmentLength must be 0 and DataSegment must be empty."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Create returing data structure.
        let retvalue = {
            LUN = lun_me.fromBytes a.m_LUNorOpcodeSpecific1 0;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            TargetTransferTag = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 0 |> ttt_me.fromPrim;
            StatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> statsn_me.fromPrim;
            ExpCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> cmdsn_me.fromPrim;
            MaxCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 12 |> cmdsn_me.fromPrim;
            R2TSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 16 |> datasn_me.fromPrim;
            BufferOffset = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 20;
            DesiredDataTransferLength = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 24;
        }

        // Check DesiredDataTransferLength field value
        if retvalue.DesiredDataTransferLength = 0u then
            let msg = "In R2T PDU, DesiredDataTransferLength must not be 0."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Check TargetTransferTag field value
        if ttt_me.toPrim retvalue.TargetTransferTag = 0xFFFFFFFFu then
            let msg = "In R2T PDU, TargetTransferTag must not be 0xFFFFFFFF."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get Asyncronous Message PDU
    static member private toAsyncronousMessagePDU ( a : internalPDUInfo ) : AsyncronousMessagePDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not Asyncronous Message, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.ASYNC )

        // Receiving Asyncronous Message PDU is occured on initiator only.
        assert( a.m_Standpoint.IsInitiator );

        // Parse sense data in DataSegment.
        let wSenseLength =
            if a.m_DataSegment.Count > 0 then
                Functions.NetworkBytesToUInt16_InPooledBuffer a.m_DataSegment 0
            else
                0us
        let wSenseData =
            if wSenseLength > 0us then
                a.m_DataSegment.GetPartialBytes 2 ( int wSenseLength + 1 ) 
            else
                Array.empty
        let wISCSIEventData =
            if int wSenseLength + 2 < a.m_DataSegment.Count then
                a.m_DataSegment.GetPartialBytes ( int wSenseLength + 2 ) ( a.m_DataSegment.Count - 1 ) 
                //a.m_DataSegment.[ int wSenseLength + 2 .. ]
            else
                Array.empty

        // Create returing data structure.
        let wAsyncEventByte = a.m_OpcodeSpecific2.[16]
        let retvalue = {
            LUN = lun_me.fromBytes a.m_LUNorOpcodeSpecific1 0;
            StatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> statsn_me.fromPrim;
            ExpCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> cmdsn_me.fromPrim;
            MaxCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 12 |> cmdsn_me.fromPrim;
            AsyncEvent = Constants.byteToAsyncEventCd wAsyncEventByte ( fun _ -> 
                let msg = sprintf "In Asyncronous message PDU, AsyncEvent(0x%02X) field value is invalid." wAsyncEventByte
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
            );
            AsyncVCode = a.m_OpcodeSpecific2.[17];
            Parameter1 = Functions.NetworkBytesToUInt16 a.m_OpcodeSpecific2 18;
            Parameter2 = Functions.NetworkBytesToUInt16 a.m_OpcodeSpecific2 20;
            Parameter3 = Functions.NetworkBytesToUInt16 a.m_OpcodeSpecific2 22;
            SenseLength = wSenseLength;
            SenseData = wSenseData;
            ISCSIEventData = wISCSIEventData;
        }

        // Return created data
        retvalue
      
    // --------------------------------------------------------------------
    // Get Text request PDU
    static member private toTextRequestPDU ( a : internalPDUInfo ) : TextRequestPDU =
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not Text request, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.TEXT_REQ )

        // Receiving Text request PDU is occured on target only.
        assert( a.m_Standpoint.IsTarget );

        // Create returing data structure.
        let retvalue = {
            I = a.m_I;
            F = a.m_F;
            C = Functions.CheckBitflag a.m_OpcodeSpecific0.[0] Constants.CONTINUE_BIT;
            LUN = lun_me.fromBytes a.m_LUNorOpcodeSpecific1 0;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            TargetTransferTag = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 0 |> ttt_me.fromPrim;
            CmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> cmdsn_me.fromPrim;
            ExpStatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> statsn_me.fromPrim;
            TextRequest = a.m_DataSegment.ArraySegment.ToArray();    // Since it is troublesome and occurs infrequently, it is allocated from the heap.
            ByteCount = a.m_ReceivedByteCount;
        }

        // The buffer allocated from the ArrayPool is no longer needed, so it is returned.
        a.m_DataSegment.Return()

        // Check F and C bit value
        if retvalue.F && retvalue.C then
            let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
            let msg = "In Text request PDU, if C bit set to 1, F bit must be 0."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get Text response PDU
    static member private toTextResponsePDU ( a : internalPDUInfo ) : TextResponsePDU =
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not Text response, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.TEXT_RES )

        // Receiving Text response PDU is occured on initiator only.
        assert( a.m_Standpoint.IsInitiator );

        // Create returing data structure.
        let retvalue = {
            F = a.m_F;
            C = Functions.CheckBitflag a.m_OpcodeSpecific0.[0] Constants.CONTINUE_BIT;
            LUN = lun_me.fromBytes a.m_LUNorOpcodeSpecific1 0;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            TargetTransferTag = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 0 |> ttt_me.fromPrim;
            StatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> statsn_me.fromPrim;
            ExpCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> cmdsn_me.fromPrim;
            MaxCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 12 |> cmdsn_me.fromPrim;
            TextResponse = a.m_DataSegment.ArraySegment.ToArray();    // Since it is troublesome and occurs infrequently, it is allocated from the heap.
        }

        // The buffer allocated from the ArrayPool is no longer needed, so it is returned.
        a.m_DataSegment.Return()

        // Check F and C bit value
        if retvalue.F && retvalue.C then
            let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
            let msg = "In Text response PDU, if C bit set to 1, F bit must be 0."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get Login request PDU
    static member private toLoginRequestPDU ( a : internalPDUInfo ) : LoginRequestPDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not Login request, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.LOGIN_REQ )

        // Receiving Login request PDU is occured on target only.
        assert( a.m_Standpoint.IsTarget );

        // Create returing data structure.
        let retvalue = {
            T = a.m_F;
            C = Functions.CheckBitflag a.m_OpcodeSpecific0.[0] Constants.CONTINUE_BIT;
            CSG = Constants.byteToLoginReqStateCd ( ( a.m_OpcodeSpecific0.[0] &&& 0x0Cuy ) >>> 2 ) ( fun c ->
                let msg = sprintf "In Login request PDU, CSG(0x%02X) field value is invalid." c
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
            );
            NSG = 
                if a.m_F then
                    Constants.byteToLoginReqStateCd( a.m_OpcodeSpecific0.[0] &&& 0x03uy ) ( fun c -> 
                        let msg = sprintf "In Login request PDU, NSG(0x%02X) field value is invalid." c
                        HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                        raise <| SessionRecoveryException ( msg, wtsih )
                    )
                else
                    LoginReqStateCd.SEQURITY;   // ignored

            VersionMax = a.m_OpcodeSpecific0.[1];
            VersionMin = a.m_OpcodeSpecific0.[2];
            ISID =
                isid_me.fromElem
                    ( a.m_LUNorOpcodeSpecific1.[0] &&& 0xC0uy )
                    ( a.m_LUNorOpcodeSpecific1.[0] &&& 0x3Fuy )
                    ( Functions.NetworkBytesToUInt16 a.m_LUNorOpcodeSpecific1 1 )
                    ( a.m_LUNorOpcodeSpecific1.[3] )
                    ( Functions.NetworkBytesToUInt16 a.m_LUNorOpcodeSpecific1 4 );
            TSIH = Functions.NetworkBytesToUInt16 a.m_LUNorOpcodeSpecific1 6 |> tsih_me.fromPrim;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            CID = Functions.NetworkBytesToUInt16 a.m_OpcodeSpecific2 0 |> cid_me.fromPrim;
            CmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> cmdsn_me.fromPrim;
            ExpStatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> statsn_me.fromPrim;
            TextRequest = a.m_DataSegment.ArraySegment.ToArray();    // Since it is troublesome and occurs infrequently, it is allocated from the heap.
            ByteCount = a.m_ReceivedByteCount;
        }

        // The buffer allocated from the ArrayPool is no longer needed, so it is returned.
        a.m_DataSegment.Return()

        // Check C and T bit value
        if retvalue.T && retvalue.C then
            let msg = "In Login request PDU, if C bit set to 1, T bit must be 0."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Check CSG field value
        if retvalue.CSG <> LoginReqStateCd.SEQURITY &&
            retvalue.CSG <> LoginReqStateCd.OPERATIONAL then
            let msg = sprintf "In Login request PDU, CSG(0x%02X) field value is invalid." ( byte retvalue.CSG )
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Check NSG field value
        if retvalue.T then
            if retvalue.CSG = LoginReqStateCd.SEQURITY && retvalue.NSG = LoginReqStateCd.SEQURITY then
                let msg = sprintf "In Login request PDU, CSG(0x%02X) and NSG(0x%02X) fields value combination is invalid." ( byte retvalue.CSG ) ( byte retvalue.NSG )
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )

            if retvalue.CSG = LoginReqStateCd.OPERATIONAL && retvalue.NSG <> LoginReqStateCd.FULL then
                let msg = sprintf "In Login request PDU, CSG(0x%02X) and NSG(0x%02X) fields value combination is invalid." ( byte retvalue.CSG ) ( byte retvalue.NSG )
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )

        // Check T field in ISID value
        if isid_me.get_T retvalue.ISID = 0xC0uy then
            let msg = sprintf "In Login request PDU, T(0x%02X) field in ISID value is invalid." ( isid_me.get_T retvalue.ISID )
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get Login response PDU
    static member private toLoginResponsePDU ( a : internalPDUInfo ) : LoginResponsePDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not Login response, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.LOGIN_RES )

        // Receiving Login request PDU is occured on initiator only.
        assert( a.m_Standpoint.IsInitiator );

        // Create returing data structure.
        let retvalue = {
            T = a.m_F;
            C = Functions.CheckBitflag a.m_OpcodeSpecific0.[0] Constants.CONTINUE_BIT;
            CSG = Constants.byteToLoginReqStateCd( ( a.m_OpcodeSpecific0.[0] &&& 0x0Cuy ) >>> 2 ) ( fun c ->
                let msg = sprintf "In Login response PDU, CSG(0x%02X) field value is invalid." c
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
            );
            NSG =
                if a.m_F then
                    Constants.byteToLoginReqStateCd( a.m_OpcodeSpecific0.[0] &&& 0x03uy ) ( fun c ->
                        let msg = sprintf "In Login response PDU, NSG(0x%02X) field value is invalid." c
                        HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                        raise <| SessionRecoveryException ( msg, wtsih )
                    )
                else
                    LoginReqStateCd.SEQURITY;   // ignored
            VersionMax = a.m_OpcodeSpecific0.[1];
            VersionActive = a.m_OpcodeSpecific0.[2];
            ISID =
                isid_me.fromElem
                    ( a.m_LUNorOpcodeSpecific1.[0] &&& 0xC0uy )
                    ( a.m_LUNorOpcodeSpecific1.[0] &&& 0x3Fuy )
                    ( Functions.NetworkBytesToUInt16 a.m_LUNorOpcodeSpecific1 1 )
                    ( a.m_LUNorOpcodeSpecific1.[3] )
                    ( Functions.NetworkBytesToUInt16 a.m_LUNorOpcodeSpecific1 4 );
            TSIH = Functions.NetworkBytesToUInt16 a.m_LUNorOpcodeSpecific1 6 |> tsih_me.fromPrim;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            StatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> statsn_me.fromPrim;
            ExpCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> cmdsn_me.fromPrim;
            MaxCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 12 |> cmdsn_me.fromPrim;
            Status = Constants.shortToLoginResStatCd ( Functions.NetworkBytesToUInt16 a.m_OpcodeSpecific2 16 ) ( fun c ->
                let msg = sprintf "In Login response PDU, Status-Class and Status-Detail(0x%04X) field value is invalid." c
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
            );
            TextResponse = a.m_DataSegment.ArraySegment.ToArray();    // Since it is troublesome and occurs infrequently, it is allocated from the heap.
        }

        // The buffer allocated from the ArrayPool is no longer needed, so it is returned.
        a.m_DataSegment.Return()

        // Check C and T bit value
        if retvalue.T && retvalue.C then
            let msg = "In Login response PDU, if C bit set to 1, T bit must be 0."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Check CSG field value
        if retvalue.CSG <> LoginReqStateCd.SEQURITY &&
            retvalue.CSG <> LoginReqStateCd.OPERATIONAL then
            let msg = sprintf "In Login response PDU, CSG(0x%02X) field value is invalid." ( byte retvalue.CSG )
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Check NSG field value
        if retvalue.T then
            if retvalue.CSG = LoginReqStateCd.SEQURITY && retvalue.NSG = LoginReqStateCd.SEQURITY then
                let msg = sprintf "In Login response PDU, CSG(0x%02X) and NSG(0x%02X) fields value combination is invalid." ( byte retvalue.CSG ) ( byte retvalue.NSG )
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )

            if retvalue.CSG = LoginReqStateCd.OPERATIONAL && retvalue.NSG <> LoginReqStateCd.FULL then
                let msg = sprintf "In Login response PDU, CSG(0x%02X) and NSG(0x%02X) fields value combination is invalid." ( byte retvalue.CSG ) ( byte retvalue.NSG )
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )


        // Check T field in ISID value
        if isid_me.get_T retvalue.ISID = 0xC0uy then
            let msg = sprintf "In Login response PDU, T(0x%02X) field in ISID value is invalid." ( isid_me.get_T retvalue.ISID )
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get Logout request PDU
    static member private toLogoutRequestPDU ( a : internalPDUInfo ) : LogoutRequestPDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not Logout request, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.LOGOUT_REQ )

        // Receiving Logout request PDU is occured on target only.
        assert( a.m_Standpoint.IsTarget );

        // AHS must be empty
        if a.m_AHS.Length > 0 then
            let msg = "In Logout request PDU, TotalAHSLength must be 0 and AHS must be empty."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, tsih_me.fromValOpt 0us a.m_TSIH )

        // DataSegment must be empty.
        if a.m_DataSegment.Count > 0 then
            let msg = "In Logout request PDU, DataSegmentLength must be 0 and DataSegment must be empty."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, tsih_me.fromValOpt 0us a.m_TSIH )

        // Create returing data structure.
        let retvalue = {
            I = a.m_I;
            ReasonCode = Constants.byteToLogoutReqReasonCd( a.m_OpcodeSpecific0.[0] &&& 0x7Fuy ) ( fun c ->
                let msg = sprintf "In Logout request PDU, ReasonCode(0x%02X) field value is invalid." c
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
            );
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            CID = Functions.NetworkBytesToUInt16 a.m_OpcodeSpecific2 0 |> cid_me.fromPrim;
            CmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> cmdsn_me.fromPrim;
            ExpStatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> statsn_me.fromPrim;
            ByteCount = a.m_ReceivedByteCount;
        }

        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get Logout response PDU
    static member private toLogoutResponsePDU ( a : internalPDUInfo ) : LogoutResponsePDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not Logout response, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.LOGOUT_RES )

        // Receiving Logout request PDU is occured on initiator only.
        assert( a.m_Standpoint.IsInitiator );

        // AHS must be empty
        if a.m_AHS.Length > 0 then
            let msg = "In Logout response PDU, TotalAHSLength must be 0 and AHS must be empty."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // DataSegment must be empty.
        if a.m_DataSegment.Count > 0 then
            let msg = "In Logout response PDU, DataSegmentLength must be 0 and DataSegment must be empty."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Create returing data structure.
        let retvalue = {
            Response = Constants.byteToLogoutResCd( a.m_OpcodeSpecific0.[1] ) ( fun c -> 
                let msg = sprintf "In Logout response PDU, Response(0x%02X) field value is invalid." c
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
            );
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            StatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> statsn_me.fromPrim;
            ExpCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> cmdsn_me.fromPrim;
            MaxCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 12 |> cmdsn_me.fromPrim;
            Time2Wait = Functions.NetworkBytesToUInt16 a.m_OpcodeSpecific2 20;
            Time2Retain = Functions.NetworkBytesToUInt16 a.m_OpcodeSpecific2 22;

            // This value is an internally used flag, so there is no corresponding value in the received data.
            CloseAllegiantConnection = true;
        }

        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get SNACK request PDU
    static member private toSNACKRequestPDU ( a : internalPDUInfo ) : SNACKRequestPDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not SNACK request, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.SNACK )

        // Receiving SNACK request PDU is occured on target only.
        assert( a.m_Standpoint.IsTarget );

        // Create returing data structure.
        let retvalue = {
            Type = Constants.byteToSnackReqTypeCd( a.m_OpcodeSpecific0.[0] &&& 0x0Fuy ) ( fun c ->
                let msg = sprintf "In SNACK request PDU, Type(0x%02X) field value is invalid." c
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
            );
            LUN = lun_me.fromBytes a.m_LUNorOpcodeSpecific1 0;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            TargetTransferTag = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 0 |> ttt_me.fromPrim;
            ExpStatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> statsn_me.fromPrim;
            BegRun = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 20;
            RunLength = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 24;
            ByteCount = a.m_ReceivedByteCount;
        }

        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get Reject PDU
    static member private toRejectPDU ( a : internalPDUInfo ) : RejectPDU =
        let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not Reject, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.REJECT )

        // Receiving Reject PDU is occured on initiator only.
        assert( a.m_Standpoint.IsInitiator );

        // Create returing data structure.
        let wReasonByte = a.m_OpcodeSpecific0.[1]
        let retvalue = {
            Reason = Constants.byteToRejectReasonCd wReasonByte ( fun _ ->
                let msg = sprintf "In Reject PDU, Reason(0x%02X) field value is invalid." wReasonByte
                HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
                raise <| SessionRecoveryException ( msg, wtsih )
            );
            StatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> statsn_me.fromPrim;
            ExpCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> cmdsn_me.fromPrim;
            MaxCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 12 |> cmdsn_me.fromPrim;
            DataSN_or_R2TSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 16 |> datasn_me.fromPrim;
            HeaderData = a.m_DataSegment.ArraySegment.ToArray();    // Since it is troublesome and occurs infrequently, it is allocated from the heap.
        }

        // The buffer allocated from the ArrayPool is no longer needed, so it is returned.
        a.m_DataSegment.Return()

        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get NOP-Out PDU
    static member private toNOPOutPDU ( a : internalPDUInfo ) : NOPOutPDU =
        let wtsih = tsih_me.fromValOpt 0us a.m_TSIH

        // Check Opcode value ( If Opcode is not NOP-Out, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.NOP_OUT )

        // Receiving NOP-Out PDU is occured on target only.
        assert( a.m_Standpoint.IsTarget );

        // Create returing data structure.
        let retvalue = {
            I = a.m_I;
            LUN = lun_me.fromBytes a.m_LUNorOpcodeSpecific1 0;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            TargetTransferTag = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 0 |> ttt_me.fromPrim;
            CmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> cmdsn_me.fromPrim;
            ExpStatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> statsn_me.fromPrim;
            PingData = a.m_DataSegment;
            ByteCount = a.m_ReceivedByteCount;
        }

        // Check InitiatorTaskTag field and I bit value
        if retvalue.InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu && not retvalue.I then
            let loginfo = struct( a.m_ObjID, a.m_CID, a.m_ConCounter, a.m_TSIH, ValueNone, ValueNone )
            let msg = "In NOP-Out PDU, if InitiatorTaskTag field is 0xFFFFFFFF, I bit must be set 1."
            HLogger.Trace( LogID.E_ISCSI_FORMAT_ERROR, fun g -> g.Gen1( loginfo, msg ) )
            raise <| SessionRecoveryException ( msg, wtsih )

        // Return created data
        retvalue

    // --------------------------------------------------------------------
    // Get NOP-In PDU
    static member private toNOPInPDU ( a : internalPDUInfo ) : NOPInPDU =
        // Check Opcode value ( If Opcode is not NOP-In, this function must not called.
        // So, In case of conflict with this condition, it is considered a defect of Haruka. )
        assert ( a.m_Opcode = OpcodeCd.NOP_IN )

        // Receiving NOP-In PDU is occured on initiator only.
        assert( a.m_Standpoint.IsInitiator );

        // Create returing data structure.
        let retvalue = {
            LUN = lun_me.fromBytes a.m_LUNorOpcodeSpecific1 0;
            InitiatorTaskTag = a.m_InitiatorTaskTag;
            TargetTransferTag = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 0 |> ttt_me.fromPrim;
            StatSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 4 |> statsn_me.fromPrim;
            ExpCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 8 |> cmdsn_me.fromPrim;
            MaxCmdSN = Functions.NetworkBytesToUInt32 a.m_OpcodeSpecific2 12 |> cmdsn_me.fromPrim;
            PingData = a.m_DataSegment;
        }

        // Return created data
        retvalue

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Write AHS data to specified buffer.
    /// </summary>
    /// <param name="argAHS">
    ///   AHS data that is written to buffer.
    /// </param>
    /// <param name="argBuf">
    ///   The buffer which is written AHS data to.
    /// </param>
    /// <param name="s">
    ///   Offset in the buffer. It specify the position which AHS data is written.
    /// </param>
    /// <returns>
    ///   Written bytes count.
    /// </returns>
    static member private WriteAHSDataToBuffer ( argAHS : AHS ) ( argBuf : byte[] ) ( s : int ) : int =
        Functions.UInt16ToNetworkBytes argBuf ( s + 0 ) argAHS.AHSLength
        argBuf.[ s + 2 ] <- (byte argAHS.AHSType )
        argBuf.[ s + 3 ] <- argAHS.AHSSpecific1
        Array.blit argAHS.AHSSpecific2 0 argBuf ( s + 4 ) argAHS.AHSSpecific2.Length
        argAHS.AHSSpecific2.Length + 4
    
    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send bytes sequence via specified socket.
    /// </summary>
    /// <param name="s">
    ///   Stream used to send data.
    /// </param>
    /// <param name="v">
    ///   Bytes sequence that is sent to. All of bytes in this array are sent.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH of the current session. It is used in exception and to write log message only.
    /// </param>
    /// <param name="argCID">
    ///   CID of the current connection. It is used in exception and to write log message only.
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection. It is used in exception and to write log message only.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="fnname">
    ///   Function name of the function that calls this method. This argument is used to output to log file.
    /// </param>
    /// <param name="source">
    ///   The source code file name. This argument is used to output to log file.
    /// </param>
    /// <param name="line">
    ///   The source code line number of caller function.
    ///   This argument is used to output to log file.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendBytes
        (
            s : Stream,
            v : byte[],
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            [<CallerMemberName>] ?fnname : string,
            [<CallerFilePath>] ?source : string,
            [<CallerLineNumber>] ?line: int
         ) : Task<uint32> =
        PDU.SendBytes( s, ArraySegment( v, 0, v.Length ), argTSIH, argCID, argCounter, objid, fnname.Value, source.Value, line.Value )

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send bytes sequence via specified socket.
    /// </summary>
    /// <param name="s">
    ///   Stream used to send data.
    /// </param>
    /// <param name="v">
    ///   Bytes sequence that is sent to.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH of the current session. It is used in exception and to write log message only.
    /// </param>
    /// <param name="argCID">
    ///   CID of the current connection. It is used in exception and to write log message only.
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection. It is used in exception and to write log message only.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="fnname">
    ///   Function name of the function that calls this method. This argument is used to output to log file.
    /// </param>
    /// <param name="source">
    ///   The source code file name. This argument is used to output to log file.
    /// </param>
    /// <param name="line">
    ///   The source code line number of caller function.
    ///   This argument is used to output to log file.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendBytes
        (
            s : Stream,
            v : ArraySegment<byte>,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            [<CallerMemberName>] ?fnname : string,
            [<CallerFilePath>] ?source : string,
            [<CallerLineNumber>] ?line: int
         ) : Task<uint32> =
        task {
            if v.Count > 0 then
                try
                    do! s.WriteAsync( v.Array, v.Offset, v.Count )
                with
                | :? IOException
                | :? SocketException
                | :? ObjectDisposedException as x ->
                    let loginfo = struct( objid, argCID, argCounter, argTSIH, ValueNone, ValueNone )
                    HLogger.Trace( LogID.E_PDU_SEND_ERROR, fun g -> g.Gen1( loginfo, x.Message, (), fnname.Value, source.Value, line.Value ) )
                    raise <| ConnectionErrorException( "Connection closed.", tsih_me.fromValOpt 0us argTSIH, cid_me.fromValOpt 0us argCID )
                return ( uint32 v.Count )
            else
                return 0u
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send data segment bytes. 
    ///   It add padding bytes to send bytes sequence in necessary.
    ///   And calculates data digest and send it to the peer.
    /// </summary>
    /// <param name="s">
    ///   Stream used to send data.
    /// </param>
    /// <param name="v">
    ///   Bytes sequence that is sent to. All of bytes in this array are sent.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH of the current session. It is used in exception and to write log message only.
    /// </param>
    /// <param name="argCID">
    ///   CID of the current connection. It is used in exception and to write log message only.
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection. It is used in exception and to write log message only.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="argDataDigest">
    ///   Algorithm used to calculate the data digest.
    /// </param>
    /// <param name="fnname">
    ///   Function name of the function that calls this method. This argument is used to output to log file.
    /// </param>
    /// <param name="source">
    ///   The source code file name. This argument is used to output to log file.
    /// </param>
    /// <param name="line">
    ///   The source code line number of caller function.
    ///   This argument is used to output to log file.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendDataSegmentBytes
        (
            s : Stream,
            v : ArraySegment<byte>,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            argDataDigest : DigestType,
            [<CallerMemberName>] ?fnname : string,
            [<CallerFilePath>] ?source : string,
            [<CallerLineNumber>] ?line: int
        ) : Task<uint32> =

        task {
            if v.Count > 0 then
                let paddBytesCount = ( Functions.AddPaddingLengthUInt32 ( uint32 v.Count ) 4u ) - uint32 v.Count
                let paddBytes : byte[] = Array.zeroCreate( int paddBytesCount )

                // Start sending data segment
                let! dataSegSentLen =
                    PDU.SendBytes( s, v, argTSIH, argCID, argCounter, objid, fnname.Value, source.Value, line.Value )

                // Calc Data Digest
                let vDigest = 
                    if argDataDigest = DigestType.DST_CRC32C then
                        Functions.CRC32_AS( [| v; ArraySegment( paddBytes ); |] )
                        |> Functions.UInt32ToNetworkBytes_NewVec
                        |> Some
                    else
                        None

                // Send padding bytes
                let! padSentLen = PDU.SendBytes( s, paddBytes, argTSIH, argCID, argCounter, objid, fnname.Value, source.Value, line.Value )
        
                if argDataDigest = DigestType.DST_CRC32C then
                    // Send Data Digest
                    let! digestSentLen = PDU.SendBytes( s, vDigest.Value, argTSIH, argCID, argCounter, objid, fnname.Value, source.Value, line.Value )
                    return dataSegSentLen + padSentLen + digestSentLen
                else
                    return dataSegSentLen + padSentLen
            else
                return 0u
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send PDU data to peer.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session, or if TSIH is not dicided, specify 0 in this argument.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection, or 0.
    ///   If TSIH is not 0, CID must also not 0.
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection. It is used in exception and to write log message only.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   Any type of PDU data structure.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   This function recognize specified PDU data type and calls correspond method.
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member
        SendPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : ILogicalPDU ) : Task<uint32> =

        match argPDU.Opcode with
        | OpcodeCd.NOP_IN ->
            PDU.SendNOPInPDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> NOPInPDU,
                false
            )
        | OpcodeCd.SCSI_RES ->
            PDU.SendSCSIResponsePDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> SCSIResponsePDU,
                false
            )
        | OpcodeCd.SCSI_TASK_MGR_RES ->
            PDU.SendTaskManagementFunctionResponsePDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> TaskManagementFunctionResponsePDU,
                false
            )
        | OpcodeCd.LOGIN_RES ->
            PDU.SendLoginResponsePDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> LoginResponsePDU,
                false
            )
        | OpcodeCd.TEXT_RES ->
            PDU.SendTextResponsePDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> TextResponsePDU,
                false
            )
        | OpcodeCd.SCSI_DATA_IN ->
            PDU.SendSCSIDataInPDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> SCSIDataInPDU,
                false
            )
        | OpcodeCd.LOGOUT_RES ->
            PDU.SendLogoutResponsePDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> LogoutResponsePDU,
                false
            )
        | OpcodeCd.R2T ->
            PDU.SendR2TPDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> R2TPDU,
                false
            )
        | OpcodeCd.ASYNC ->
            PDU.SendAsyncronousMessagePDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> AsyncronousMessagePDU,
                false
            )
        | OpcodeCd.REJECT ->
            PDU.SendRejectPDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> RejectPDU,
                false
            )
        | OpcodeCd.NOP_OUT ->
            PDU.SendNOPOutPDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> NOPOutPDU,
                false
            )
        | OpcodeCd.SCSI_COMMAND ->
            PDU.SendSCSICommandPDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> SCSICommandPDU,
                false
            )
        | OpcodeCd.SCSI_TASK_MGR_REQ ->
            PDU.SendTaskManagementFunctionRequestPDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> TaskManagementFunctionRequestPDU,
                false
            )
        | OpcodeCd.LOGIN_REQ ->
            PDU.SendLoginRequestPDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> LoginRequestPDU,
                false
            )
        | OpcodeCd.TEXT_REQ ->
            PDU.SendTextRequestPDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> TextRequestPDU,
                false
            )
        | OpcodeCd.SCSI_DATA_OUT ->
            PDU.SendSCSIDataOutPDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> SCSIDataOutPDU,
                false
            )
        | OpcodeCd.LOGOUT_REQ ->
            PDU.SendLogoutRequestPDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> LogoutRequestPDU,
                false
            )
        | OpcodeCd.SNACK ->
            PDU.SendSNACKRequestPDU(
                argMaxRecvDataSegmentLength,
                argHeaderDigest,
                argDataDigest,
                argTSIH,
                argCID,
                argCounter,
                objid,
                sock,
                argPDU :?> SNACKRequestPDU,
                false
            )
        | _ ->
            task {
                return 0u
            }


    // ------------------------------------------------------------------------
    /// <summary>
    ///   Get BHS bytes data of specified PDU.
    /// </summary>
    /// <param name="argTSIH">
    ///   TSIH value of current session, or if TSIH is not dicided, specify 0 in this argument.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection, or 0.
    ///   If TSIH is not 0, CID must also not 0.
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection. It is used in exception and to write log message only.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="argPDU">
    ///   Any type of PDU data structure.
    /// </param>
    /// <returns>
    ///   BHS bytes data. 
    /// </returns>
    static member GetHeader ( argPDU : ILogicalPDU ) : byte[] =

        use s = new MemoryStream()
        let dummy = objidx_me.NewID()

        let w =
            match argPDU.Opcode with
            | OpcodeCd.NOP_IN ->
                PDU.SendNOPInPDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> NOPInPDU, true )
            | OpcodeCd.SCSI_RES ->
                PDU.SendSCSIResponsePDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> SCSIResponsePDU, true )
            | OpcodeCd.SCSI_TASK_MGR_RES ->
                PDU.SendTaskManagementFunctionResponsePDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> TaskManagementFunctionResponsePDU, true )
            | OpcodeCd.LOGIN_RES ->
                PDU.SendLoginResponsePDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> LoginResponsePDU, true )
            | OpcodeCd.TEXT_RES ->
                PDU.SendTextResponsePDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> TextResponsePDU, true )
            | OpcodeCd.SCSI_DATA_IN ->
                PDU.SendSCSIDataInPDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> SCSIDataInPDU, true )
            | OpcodeCd.LOGOUT_RES ->
                PDU.SendLogoutResponsePDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> LogoutResponsePDU, true )
            | OpcodeCd.R2T ->
                PDU.SendR2TPDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> R2TPDU, true )
            | OpcodeCd.ASYNC ->
                PDU.SendAsyncronousMessagePDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> AsyncronousMessagePDU, true )
            | OpcodeCd.REJECT ->
                PDU.SendRejectPDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> RejectPDU, true )
            | OpcodeCd.NOP_OUT ->
                PDU.SendNOPOutPDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> NOPOutPDU, true )
            | OpcodeCd.SCSI_COMMAND ->
                PDU.SendSCSICommandPDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> SCSICommandPDU, true )
            | OpcodeCd.SCSI_TASK_MGR_REQ ->
                PDU.SendTaskManagementFunctionRequestPDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> TaskManagementFunctionRequestPDU, true )
            | OpcodeCd.LOGIN_REQ ->
                PDU.SendLoginRequestPDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> LoginRequestPDU, true )
            | OpcodeCd.TEXT_REQ ->
                PDU.SendTextRequestPDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> TextRequestPDU, true )
            | OpcodeCd.SCSI_DATA_OUT ->
                PDU.SendSCSIDataOutPDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> SCSIDataOutPDU, true )
            | OpcodeCd.LOGOUT_REQ ->
                PDU.SendLogoutRequestPDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> LogoutRequestPDU, true )
            | OpcodeCd.SNACK ->
                PDU.SendSNACKRequestPDU( 0u, DigestType.DST_None, DigestType.DST_None, ValueNone, ValueNone, ValueNone, dummy, s, argPDU :?> SNACKRequestPDU, true )
            | _ ->
                Task.FromResult 0u
        w
        |> Functions.RunTaskSynchronously
        |> ignore

        s.Seek( 0L, SeekOrigin.Begin ) |> ignore
        let buf = Array.zeroCreate( 48 )
        s.Read( buf, 0, 48 ) |> ignore
        buf

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send SCSI Command PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A SCSI Command PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If this argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendSCSICommandPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : SCSICommandPDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            // If SCSI CDB exists, CDB data must include padding bytes
            assert( argPDU.ScsiCDB.Length >= 16 && argPDU.ScsiCDB.Length % 4 = 0 )
            let dataSegment = argPDU.DataSegment

            // Create AHS data
            let vAHS =
                [|
                    if argPDU.ScsiCDB.Length >= 17 then
                        yield {
                            AHSLength = uint16( argPDU.ScsiCDB.Length - 15 );
                            AHSType = AHSTypeCd.EXTENDED_CDB;
                            AHSSpecific1 = 0uy;
                            AHSSpecific2 = argPDU.ScsiCDB.[16..];   // AHSSpecific2.Length + 1(AHSSpecific1 length) = AHSLength
                        }
                    if argPDU.R && argPDU.W then
                        yield {
                            AHSLength = 5us;
                            AHSType = AHSTypeCd.EXPECTED_LENGTH;
                            AHSSpecific1 = 0uy;
                            AHSSpecific2 = Functions.UInt32ToNetworkBytes_NewVec argPDU.BidirectionalExpectedReadDataLength;
                        }
                |]
            let AHSLength = Array.fold ( fun acc i -> acc + 4 + i.AHSSpecific2.Length ) 0 vAHS
            let wDataSegmentLength = uint32 dataSegment.Count

            // AHS Length must less than or equal 255.
            assert( AHSLength <= 255 )
            assert( argPDU.ScsiCDB.Length % 4 = 0 )

            let wbuf = PooledBuffer.RentAndInit ( 48 + AHSLength )

            // Create BHS & AHS data.
            wbuf.Array.[0] <- ( Functions.SetBitflag argPDU.I Constants.IMMIDIATE_BIT ) ||| ( byte OpcodeCd.SCSI_COMMAND )
            wbuf.Array.[1] <- ( Functions.SetBitflag argPDU.F Constants.FINAL_BIT ) ||| ( Functions.SetBitflag argPDU.R Constants.READ_BIT ) ||| ( Functions.SetBitflag argPDU.W Constants.WRITE_BIT ) ||| ( byte argPDU.ATTR )
            wbuf.Array.[2] <- 0uy
            wbuf.Array.[3] <- 0uy
            wbuf.Array.[4] <- byte AHSLength
            wbuf.Array.[5] <- byte( wDataSegmentLength >>> 16 )
            wbuf.Array.[6] <- byte( wDataSegmentLength >>> 8 )
            wbuf.Array.[7] <- byte( wDataSegmentLength )
            lun_me.toBytes wbuf.Array 8 argPDU.LUN
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 20 argPDU.ExpectedDataTransferLength
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( cmdsn_me.toPrim argPDU.CmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( statsn_me.toPrim argPDU.ExpStatSN )
            Array.blit argPDU.ScsiCDB 0 wbuf.Array 32 16
            if vAHS.Length > 0 then
                let w = PDU.WriteAHSDataToBuffer vAHS.[0] wbuf.Array 48
                if vAHS.Length > 1 then
                    PDU.WriteAHSDataToBuffer vAHS.[1] wbuf.Array ( 48 + w ) |> ignore
 
            // send BHS & AHS Data
            let! headerSendLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            if not headerOnly then
                // Data segment length must less than or equal 0x00FFFFFF and argMaxRecvDataSegmentLength
                assert( wDataSegmentLength <= 0x00FFFFFFu && wDataSegmentLength <= argMaxRecvDataSegmentLength )

                // Send header digest
                let headerDigestLen =
                    if argHeaderDigest = DigestType.DST_CRC32C then 4u else 0u
                if headerDigestLen > 0u then
                    let vDigest = Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                    let! _ = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
                    ()

                // Send DataSegment
                let! dataSendLen =
                    PDU.SendDataSegmentBytes( sock, dataSegment.ArraySegment, argTSIH, argCID, argCounter, objid, argDataDigest )

                wbuf.Return()
                return headerSendLen + headerDigestLen + dataSendLen
            else
                wbuf.Return()
                return headerSendLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send SCSI Response PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A SCSI Response PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendSCSIResponsePDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : SCSIResponsePDU ,
            headerOnly : bool ) : Task<uint32> =

        task {
            // Calcurate DataSegment Length
            let wDataSegmentLength = 
                let wRespData = argPDU.ResponseData
                let wSenseData = argPDU.SenseData
                if wSenseData.Count + wRespData.Count > 0 then
                    uint32 ( wSenseData.Count + wRespData.Count + 2 )
                else
                    0u

            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data.
            wbuf.Array.[0] <- ( byte OpcodeCd.SCSI_RES )
            wbuf.Array.[1] <- Constants.FINAL_BIT |||
                        ( Functions.SetBitflag argPDU.o Constants.BI_READ_RESIDUAL_OV_BIT ) |||
                        ( Functions.SetBitflag argPDU.u Constants.BI_READ_RESIDUAL_UND_BIT ) |||
                        ( Functions.SetBitflag argPDU.O Constants.RESIDUAL_OVERFLOW_BIT ) |||
                        ( Functions.SetBitflag argPDU.U Constants.RESIDUAL_UNDERFLOW_BIT )
            wbuf.Array.[2] <- byte argPDU.Response
            wbuf.Array.[3] <- byte argPDU.Status
            wbuf.Array.[4] <- 0uy
            wbuf.Array.[5] <- byte( wDataSegmentLength >>> 16 )
            wbuf.Array.[6] <- byte( wDataSegmentLength >>> 8 )
            wbuf.Array.[7] <- byte( wDataSegmentLength )
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 20 ( snacktag_me.toPrim argPDU.SNACKTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( statsn_me.toPrim argPDU.StatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( cmdsn_me.toPrim argPDU.ExpCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 32 ( cmdsn_me.toPrim argPDU.MaxCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 36 ( datasn_me.toPrim argPDU.ExpDataSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 40 argPDU.BidirectionalReadResidualCount
            Functions.UInt32ToNetworkBytes wbuf.Array 44 argPDU.ResidualCount

            // Send BHS data
            let! bhsLen =
                PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            if headerOnly then
                wbuf.Return()
                return bhsLen
            else
                // Data segment length must less than or equal 0x00FFFFFF and argMaxRecvDataSegmentLength
                assert( wDataSegmentLength <= 0x00FFFFFFu && wDataSegmentLength <= argMaxRecvDataSegmentLength )

                // send Header Digest
                let headerDigestLen =
                    if argHeaderDigest = DigestType.DST_CRC32C then 4u else 0u
                if headerDigestLen > 0u then
                    let vDigest = Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                    let! _ = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
                    ()
        
                if wDataSegmentLength = 0u then
                    wbuf.Return()
                    return bhsLen + headerDigestLen
                else
                    // Send DataSegment
                    let wSenseDataBytes = argPDU.SenseData
                    let SenseDataLengthBytes = Functions.UInt16ToNetworkBytes_NewVec( uint16 wSenseDataBytes.Count )

                    let! senseLenLen = PDU.SendBytes( sock, SenseDataLengthBytes, argTSIH, argCID, argCounter, objid )
                    let! senseLen = PDU.SendBytes( sock, argPDU.SenseData, argTSIH, argCID, argCounter, objid )

                    // Send response data
                    let! respLen = PDU.SendBytes( sock, argPDU.ResponseData, argTSIH, argCID, argCounter, objid )

                    // Send padding bytes
                    let paddBytesCount = ( Functions.AddPaddingLengthUInt32 wDataSegmentLength 4u ) - wDataSegmentLength
                    let paddBytes : byte[] = Array.zeroCreate( int paddBytesCount )
                    let! padLen = PDU.SendBytes( sock, paddBytes, argTSIH, argCID, argCounter, objid )

                    // Calc data digest
                    let vDigest =
                        if argDataDigest = DigestType.DST_CRC32C then
                            Functions.CRC32_AS [|
                                ArraySegment( SenseDataLengthBytes );
                                argPDU.SenseData;
                                argPDU.ResponseData;
                                ArraySegment( paddBytes );
                            |]
                            |> Functions.UInt32ToNetworkBytes_NewVec
                        else
                            Array.empty
        
                    // Send Data Digest
                    let! dataDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )

                    wbuf.Return()
                    return bhsLen + headerDigestLen + senseLenLen + senseLen + respLen + padLen + dataDigestLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Task management function request PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Task management function request PDU doen not have data segment.
    ///   So, this argument is not used.
    ///   It exists to unify the types of function.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Task management function request PDU doen not have data segment.
    ///   So, this argument is not used.
    ///   It exists to unify the types of function.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A Task management function request PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendTaskManagementFunctionRequestPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : TaskManagementFunctionRequestPDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data.
            wbuf.Array.[0] <- ( Functions.SetBitflag argPDU.I Constants.IMMIDIATE_BIT ) ||| ( byte OpcodeCd.SCSI_TASK_MGR_REQ )
            wbuf.Array.[1] <- Constants.FINAL_BIT ||| ( byte argPDU.Function )
            lun_me.toBytes wbuf.Array 8 argPDU.LUN
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 20 ( itt_me.toPrim argPDU.ReferencedTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( cmdsn_me.toPrim argPDU.CmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( statsn_me.toPrim argPDU.ExpStatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 32 ( cmdsn_me.toPrim argPDU.RefCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 36 ( datasn_me.toPrim argPDU.ExpDataSN )

            // Send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            // send Header Digest
            if ( not headerOnly ) && argHeaderDigest = DigestType.DST_CRC32C then
                let vDigest = Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
                wbuf.Return()
                return bhsLen + headerDigestLen
            else
                wbuf.Return()
                return bhsLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Task management function response PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Task management function response PDU doen not have data segment.
    ///   So, this argument is not used.
    ///   It exists to unify the types of function.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Task management function response PDU doen not have data segment.
    ///   So, this argument is not used.
    ///   It exists to unify the types of function.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A Task management function response PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendTaskManagementFunctionResponsePDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : TaskManagementFunctionResponsePDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data.
            wbuf.Array.[0] <- ( byte OpcodeCd.SCSI_TASK_MGR_RES )
            wbuf.Array.[1] <- Constants.FINAL_BIT
            wbuf.Array.[2] <- byte argPDU.Response
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( statsn_me.toPrim argPDU.StatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( cmdsn_me.toPrim argPDU.ExpCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 32 ( cmdsn_me.toPrim argPDU.MaxCmdSN )

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            // send Header Digest
            if ( not headerOnly ) && argHeaderDigest = DigestType.DST_CRC32C then
                let vDigest = Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
                wbuf.Return()
                return bhsLen + headerDigestLen
            else
                wbuf.Return()
                return bhsLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send SCSI Data-Out PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A Send SCSI Data-Out PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendSCSIDataOutPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : SCSIDataOutPDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let dataSegment = argPDU.DataSegment
            let wDataSegmentLength = uint32 dataSegment.Count
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- ( byte OpcodeCd.SCSI_DATA_OUT )
            wbuf.Array.[1] <- Functions.SetBitflag argPDU.F Constants.FINAL_BIT
            wbuf.Array.[2] <- 0uy
            wbuf.Array.[3] <- 0uy
            wbuf.Array.[4] <- 0uy
            wbuf.Array.[5] <- byte( wDataSegmentLength >>> 16 )
            wbuf.Array.[6] <- byte( wDataSegmentLength >>> 8 )
            wbuf.Array.[7] <- byte( wDataSegmentLength )
            lun_me.toBytes wbuf.Array 8 argPDU.LUN
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 20 ( ttt_me.toPrim argPDU.TargetTransferTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( statsn_me.toPrim argPDU.ExpStatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 36 ( datasn_me.toPrim argPDU.DataSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 40 argPDU.BufferOffset

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            if headerOnly then
                wbuf.Return()
                return bhsLen
            else
                // Data segment length must less than or equal 0x00FFFFFF and argMaxRecvDataSegmentLength
                assert( wDataSegmentLength <= 0x00FFFFFFu && wDataSegmentLength <= argMaxRecvDataSegmentLength )

                // Create and send Header Digest
                let vDigest =
                    if argHeaderDigest = DigestType.DST_CRC32C then
                        Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                    else
                        Array.empty
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
        
                // Send DataSegment
                let! dataSegLen = PDU.SendDataSegmentBytes( sock, dataSegment.ArraySegment, argTSIH, argCID, argCounter, objid, argDataDigest )

                wbuf.Return()
                return bhsLen + headerDigestLen + dataSegLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send SCSI Data-In PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A SCSI Data-In PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendSCSIDataInPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : SCSIDataInPDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wSendDataSeg = argPDU.DataSegment
            let wDataSegmentLength = uint32 wSendDataSeg.Count

            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- ( byte OpcodeCd.SCSI_DATA_IN )
            wbuf.Array.[1] <- ( Functions.SetBitflag argPDU.F Constants.FINAL_BIT ) |||
                        ( Functions.SetBitflag argPDU.A Constants.ACKNOWLEDGE_BIT ) |||
                        ( Functions.SetBitflag argPDU.O Constants.RESIDUAL_OVERFLOW_BIT ) |||
                        ( Functions.SetBitflag argPDU.U Constants.RESIDUAL_UNDERFLOW_BIT ) |||
                        ( Functions.SetBitflag argPDU.S Constants.STATUS_BIT )
            wbuf.Array.[2] <- 0uy
            wbuf.Array.[3] <- byte argPDU.Status
            wbuf.Array.[4] <- 0uy
            wbuf.Array.[5] <- byte( wDataSegmentLength >>> 16 )
            wbuf.Array.[6] <- byte( wDataSegmentLength >>> 8 )
            wbuf.Array.[7] <- byte( wDataSegmentLength )
            lun_me.toBytes wbuf.Array 8 argPDU.LUN
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 20 ( ttt_me.toPrim argPDU.TargetTransferTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( statsn_me.toPrim argPDU.StatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( cmdsn_me.toPrim argPDU.ExpCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 32 ( cmdsn_me.toPrim argPDU.MaxCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 36 ( datasn_me.toPrim argPDU.DataSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 40 argPDU.BufferOffset
            Functions.UInt32ToNetworkBytes wbuf.Array 44 argPDU.ResidualCount

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            if headerOnly then
                wbuf.Return()
                return bhsLen
            else
                // Data segment length must less than or equal 0x00FFFFFF and argMaxRecvDataSegmentLength
                assert( wDataSegmentLength <= 0x00FFFFFFu && wDataSegmentLength <= argMaxRecvDataSegmentLength )

                // Create and send Header Digest
                let vDigest =
                    if argHeaderDigest = DigestType.DST_CRC32C then
                        Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                    else
                        Array.empty
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
        
                // Send DataSegment
                let! dataSegLen = PDU.SendDataSegmentBytes( sock, wSendDataSeg, argTSIH, argCID, argCounter, objid, argDataDigest )

                wbuf.Return()
                return bhsLen + headerDigestLen + dataSegLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send R2T PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   R2T PDU doen not have data segment.
    ///   So, this argument is not used.
    ///   It exists to unify the types of function.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   R2T PDU doen not have data segment.
    ///   So, this argument is not used.
    ///   It exists to unify the types of function.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A R2T PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendR2TPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : R2TPDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- ( byte OpcodeCd.R2T )
            wbuf.Array.[1] <- Constants.FINAL_BIT
            lun_me.toBytes wbuf.Array 8 argPDU.LUN
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 20 ( ttt_me.toPrim argPDU.TargetTransferTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( statsn_me.toPrim argPDU.StatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( cmdsn_me.toPrim argPDU.ExpCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 32 ( cmdsn_me.toPrim argPDU.MaxCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 36 ( datasn_me.toPrim argPDU.R2TSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 40 argPDU.BufferOffset
            Functions.UInt32ToNetworkBytes wbuf.Array 44 argPDU.DesiredDataTransferLength

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            // Create and send Header Digest
            if ( not headerOnly ) && argHeaderDigest = DigestType.DST_CRC32C then
                let vDigest = Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )

                wbuf.Return()
                return bhsLen + headerDigestLen
            else

                wbuf.Return()
                return bhsLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Asyncronous message PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A Asyncronous message PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendAsyncronousMessagePDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : AsyncronousMessagePDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            // Calcurate DataSegment Length
            let wDataSegmentLength = 
                if argPDU.SenseData.Length + argPDU.ISCSIEventData.Length > 0 then
                    uint32 ( argPDU.SenseData.Length + argPDU.ISCSIEventData.Length + 2 )
                else
                    0u

            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- ( byte OpcodeCd.ASYNC )
            wbuf.Array.[1] <- Constants.FINAL_BIT
            wbuf.Array.[5] <- byte( wDataSegmentLength >>> 16 )
            wbuf.Array.[6] <- byte( wDataSegmentLength >>> 8 )
            wbuf.Array.[7] <- byte( wDataSegmentLength )
            lun_me.toBytes wbuf.Array 8 argPDU.LUN
            Functions.UInt32ToNetworkBytes wbuf.Array 16 0xFFFFFFFFu
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( statsn_me.toPrim argPDU.StatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( cmdsn_me.toPrim argPDU.ExpCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 32 ( cmdsn_me.toPrim argPDU.MaxCmdSN )
            wbuf.Array.[36] <- byte argPDU.AsyncEvent
            wbuf.Array.[37] <- argPDU.AsyncVCode
            Functions.UInt16ToNetworkBytes wbuf.Array 38 argPDU.Parameter1
            Functions.UInt16ToNetworkBytes wbuf.Array 40 argPDU.Parameter2
            Functions.UInt16ToNetworkBytes wbuf.Array 42 argPDU.Parameter3

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            if headerOnly then
                wbuf.Return()
                return bhsLen
            else
                // Data segment length must less than or equal 0x00FFFFFF and argMaxRecvDataSegmentLength
                assert( wDataSegmentLength <= 0x00FFFFFFu && wDataSegmentLength <= argMaxRecvDataSegmentLength )

                // Create and send Header Digest
                let vDigest =
                    if argHeaderDigest = DigestType.DST_CRC32C then
                        Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                    else
                        Array.empty
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )

                wbuf.Return()
        
                if wDataSegmentLength = 0u then
                    return bhsLen + headerDigestLen
                else
                    // Send DataSegment
                    let paddBytesCount = ( Functions.AddPaddingLengthUInt32 wDataSegmentLength 4u ) - wDataSegmentLength
                    let paddBytes : byte[] = Array.zeroCreate( int paddBytesCount )
                    let SenseDataLengthBytes = Functions.UInt16ToNetworkBytes_NewVec( uint16 argPDU.SenseData.Length )

                    let! senseDataLengthLen = PDU.SendBytes( sock, SenseDataLengthBytes, argTSIH, argCID, argCounter, objid )
                    let! senseDataLen = PDU.SendBytes( sock, argPDU.SenseData, argTSIH, argCID, argCounter, objid )
                    let! eventDataLen = PDU.SendBytes( sock, argPDU.ISCSIEventData, argTSIH, argCID, argCounter, objid )
                    let! padLen = PDU.SendBytes( sock, paddBytes, argTSIH, argCID, argCounter, objid )
        
                    // Send Data Digest
                    let vDigest =
                        if argDataDigest = DigestType.DST_CRC32C then
                            Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32v( [| SenseDataLengthBytes; argPDU.SenseData; argPDU.ISCSIEventData; paddBytes; |] )
                        else
                            Array.empty
                    let! dataDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
                    return bhsLen + headerDigestLen + senseDataLengthLen + senseDataLen + eventDataLen + padLen + dataDigestLen
        }
    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Text request PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A Text request PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendTextRequestPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : TextRequestPDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wDataSegmentLength = uint32 argPDU.TextRequest.Length
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- ( Functions.SetBitflag argPDU.I Constants.IMMIDIATE_BIT ) ||| ( byte OpcodeCd.TEXT_REQ )
            wbuf.Array.[1] <- ( Functions.SetBitflag argPDU.F Constants.FINAL_BIT ) ||| ( Functions.SetBitflag argPDU.C Constants.CONTINUE_BIT )
            wbuf.Array.[5] <- byte( wDataSegmentLength >>> 16 )
            wbuf.Array.[6] <- byte( wDataSegmentLength >>> 8 )
            wbuf.Array.[7] <- byte( wDataSegmentLength )
            lun_me.toBytes wbuf.Array 8 argPDU.LUN
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 20 ( ttt_me.toPrim argPDU.TargetTransferTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( cmdsn_me.toPrim argPDU.CmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( statsn_me.toPrim argPDU.ExpStatSN )

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            if headerOnly then
                wbuf.Return()
                return bhsLen
            else
                // Data segment length must less than or equal 0x00FFFFFF and argMaxRecvDataSegmentLength
                assert( wDataSegmentLength <= 0x00FFFFFFu && wDataSegmentLength <= argMaxRecvDataSegmentLength )

                // Create and send Header Digest
                let vDigest =
                    if argHeaderDigest = DigestType.DST_CRC32C then
                        Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                    else
                        Array.empty
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
        
                // Send DataSegment
                let! dataSegLen = PDU.SendDataSegmentBytes( sock, ArraySegment( argPDU.TextRequest ), argTSIH, argCID, argCounter, objid, argDataDigest )

                wbuf.Return()
                return bhsLen + headerDigestLen + dataSegLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Text response PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A Text response PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendTextResponsePDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : TextResponsePDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wDataSegmentLength = uint32 argPDU.TextResponse.Length
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- ( byte OpcodeCd.TEXT_RES )
            wbuf.Array.[1] <- ( Functions.SetBitflag argPDU.F Constants.FINAL_BIT ) ||| ( Functions.SetBitflag argPDU.C Constants.CONTINUE_BIT )
            wbuf.Array.[5] <- byte( wDataSegmentLength >>> 16 )
            wbuf.Array.[6] <- byte( wDataSegmentLength >>> 8 )
            wbuf.Array.[7] <- byte( wDataSegmentLength )
            lun_me.toBytes wbuf.Array 8 argPDU.LUN
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 20 ( ttt_me.toPrim argPDU.TargetTransferTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( statsn_me.toPrim argPDU.StatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( cmdsn_me.toPrim argPDU.ExpCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 32 ( cmdsn_me.toPrim argPDU.MaxCmdSN )

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            if headerOnly then
                wbuf.Return()
                return bhsLen
            else
                // Data segment length must less than or equal 0x00FFFFFF and argMaxRecvDataSegmentLength
                assert( wDataSegmentLength <= 0x00FFFFFFu && wDataSegmentLength <= argMaxRecvDataSegmentLength )

                // Create and send Header Digest
                let vDigest =
                    if argHeaderDigest = DigestType.DST_CRC32C then
                        Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                    else
                        Array.empty
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
        
                // Send DataSegment
                let! dataSegLen =
                    PDU.SendDataSegmentBytes( sock, ArraySegment( argPDU.TextResponse ), argTSIH, argCID, argCounter, objid, argDataDigest )

                wbuf.Return()
                return bhsLen + headerDigestLen + dataSegLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Login request PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A Login request PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendLoginRequestPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : LoginRequestPDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wDataSegmentLength = uint32 argPDU.TextRequest.Length
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- Constants.IMMIDIATE_BIT ||| ( byte OpcodeCd.LOGIN_REQ )
            wbuf.Array.[1] <- ( Functions.SetBitflag argPDU.T Constants.TRANSIT_BIT ) |||
                        ( Functions.SetBitflag argPDU.C Constants.CONTINUE_BIT ) |||
                        ( ( byte argPDU.CSG ) <<< 2 ) ||| ( byte argPDU.NSG )
            wbuf.Array.[2] <- argPDU.VersionMax
            wbuf.Array.[3] <- argPDU.VersionMin
            wbuf.Array.[5] <- byte( wDataSegmentLength >>> 16 )
            wbuf.Array.[6] <- byte( wDataSegmentLength >>> 8 )
            wbuf.Array.[7] <- byte( wDataSegmentLength )
            wbuf.Array.[8] <- ( isid_me.get_T argPDU.ISID ) ||| ( isid_me.get_A argPDU.ISID )
            Functions.UInt16ToNetworkBytes wbuf.Array 9 ( isid_me.get_B argPDU.ISID )
            wbuf.Array.[11] <- ( isid_me.get_C argPDU.ISID )
            Functions.UInt16ToNetworkBytes wbuf.Array 12 ( isid_me.get_D argPDU.ISID )
            Functions.UInt16ToNetworkBytes wbuf.Array 14 ( tsih_me.toPrim argPDU.TSIH )
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt16ToNetworkBytes wbuf.Array 20 ( cid_me.toPrim argPDU.CID )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( cmdsn_me.toPrim argPDU.CmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( statsn_me.toPrim argPDU.ExpStatSN )

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            if headerOnly then
                wbuf.Return()
                return bhsLen
            else
                // Data segment length must less than or equal 0x00FFFFFF and argMaxRecvDataSegmentLength
                assert( wDataSegmentLength <= 0x00FFFFFFu && wDataSegmentLength <= argMaxRecvDataSegmentLength )

                // Create and send Header Digest
                let vDigest =
                    if argHeaderDigest = DigestType.DST_CRC32C then
                        Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                    else
                        Array.empty
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
        
                // Send DataSegment
                let! dataSegLen =
                    PDU.SendDataSegmentBytes( sock, ArraySegment( argPDU.TextRequest ), argTSIH, argCID, argCounter, objid, argDataDigest )

                wbuf.Return()
                return bhsLen + headerDigestLen + dataSegLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Login response PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A Login response PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendLoginResponsePDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : LoginResponsePDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wDataSegmentLength = uint32 argPDU.TextResponse.Length
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- byte OpcodeCd.LOGIN_RES
            wbuf.Array.[1] <- ( Functions.SetBitflag argPDU.T Constants.TRANSIT_BIT ) |||
                        ( Functions.SetBitflag argPDU.C Constants.CONTINUE_BIT ) |||
                        ( ( byte argPDU.CSG ) <<< 2 ) ||| ( byte argPDU.NSG )
            wbuf.Array.[2] <- argPDU.VersionMax
            wbuf.Array.[3] <- argPDU.VersionActive
            wbuf.Array.[5] <- byte( wDataSegmentLength >>> 16 )
            wbuf.Array.[6] <- byte( wDataSegmentLength >>> 8 )
            wbuf.Array.[7] <- byte( wDataSegmentLength )
            wbuf.Array.[8] <- ( isid_me.get_T argPDU.ISID ) ||| ( isid_me.get_A argPDU.ISID )
            Functions.UInt16ToNetworkBytes wbuf.Array 9 ( isid_me.get_B argPDU.ISID )
            wbuf.Array.[11] <- ( isid_me.get_C argPDU.ISID )
            Functions.UInt16ToNetworkBytes wbuf.Array 12 ( isid_me.get_D argPDU.ISID )
            Functions.UInt16ToNetworkBytes wbuf.Array 14 ( tsih_me.toPrim argPDU.TSIH )
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( statsn_me.toPrim argPDU.StatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( cmdsn_me.toPrim argPDU.ExpCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 32 ( cmdsn_me.toPrim argPDU.MaxCmdSN )
            Functions.UInt16ToNetworkBytes wbuf.Array 36 ( uint16 argPDU.Status )

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            if headerOnly then
                wbuf.Return()
                return bhsLen
            else
                // Data segment length must less than or equal 0x00FFFFFF and argMaxRecvDataSegmentLength
                assert( wDataSegmentLength <= 0x00FFFFFFu && wDataSegmentLength <= argMaxRecvDataSegmentLength )

                // Create and send Header Digest
                let vDigest =
                    if argHeaderDigest = DigestType.DST_CRC32C then
                        Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                    else
                        Array.empty
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
        
                // Send DataSegment
                let! dataSegLen =
                    PDU.SendDataSegmentBytes( sock, ArraySegment( argPDU.TextResponse ), argTSIH, argCID, argCounter, objid, argDataDigest )

                wbuf.Return()
                return bhsLen + headerDigestLen + dataSegLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Logout request request PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Logout request PDU doen not have data segment.
    ///   So, this argument is not used.
    ///   It exists to unify the types of function.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Logout request PDU doen not have data segment.
    ///   So, this argument is not used.
    ///   It exists to unify the types of function.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A Logout request PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendLogoutRequestPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : LogoutRequestPDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- ( Functions.SetBitflag argPDU.I Constants.IMMIDIATE_BIT ) ||| ( byte OpcodeCd.LOGOUT_REQ )
            wbuf.Array.[1] <- Constants.FINAL_BIT ||| ( byte argPDU.ReasonCode )
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt16ToNetworkBytes wbuf.Array 20 ( cid_me.toPrim argPDU.CID )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( cmdsn_me.toPrim argPDU.CmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( statsn_me.toPrim argPDU.ExpStatSN )

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            // Create and send Header Digest
            if ( not headerOnly ) && argHeaderDigest = DigestType.DST_CRC32C then
                let vDigest = Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )

                wbuf.Return()
                return bhsLen + headerDigestLen
            else
                wbuf.Return()
                return bhsLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Logout response PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Logout response PDU doen not have data segment.
    ///   So, this argument is not used.
    ///   It exists to unify the types of function.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Logout response PDU doen not have data segment.
    ///   So, this argument is not used.
    ///   It exists to unify the types of function.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A Logout response PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendLogoutResponsePDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : LogoutResponsePDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- ( byte OpcodeCd.LOGOUT_RES )
            wbuf.Array.[1] <- Constants.FINAL_BIT
            wbuf.Array.[2] <- byte argPDU.Response
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( statsn_me.toPrim argPDU.StatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( cmdsn_me.toPrim argPDU.ExpCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 32 ( cmdsn_me.toPrim argPDU.MaxCmdSN )
            Functions.UInt16ToNetworkBytes wbuf.Array 40 argPDU.Time2Wait
            Functions.UInt16ToNetworkBytes wbuf.Array 42 argPDU.Time2Retain

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            // Create and send Header Digest
            if ( not headerOnly ) && argHeaderDigest = DigestType.DST_CRC32C then
                let vDigest = Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )

                wbuf.Return()
                return bhsLen + headerDigestLen
            else
                wbuf.Return()
                return bhsLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send SNACK request PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   SNACK request PDU doen not have data segment.
    ///   So, this argument is not used.
    ///   It exists to unify the types of function.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   SNACK request PDU doen not have data segment.
    ///   So, this argument is not used.
    ///   It exists to unify the types of function.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A SNACK request PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendSNACKRequestPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : SNACKRequestPDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- ( byte OpcodeCd.SNACK )
            wbuf.Array.[1] <- Constants.FINAL_BIT ||| ( byte argPDU.Type )
            lun_me.toBytes wbuf.Array 8 argPDU.LUN
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 20 ( ttt_me.toPrim argPDU.TargetTransferTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( statsn_me.toPrim argPDU.ExpStatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 40 argPDU.BegRun
            Functions.UInt32ToNetworkBytes wbuf.Array 44 argPDU.RunLength

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            // Create and send Header Digest
            if ( not headerOnly ) && argHeaderDigest = DigestType.DST_CRC32C then
                let vDigest = Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )

                wbuf.Return()
                return bhsLen + headerDigestLen
            else
                wbuf.Return()
                return bhsLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send Reject PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A Reject PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendRejectPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : RejectPDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wDataSegmentLength = uint32 argPDU.HeaderData.Length
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- ( byte OpcodeCd.REJECT )
            wbuf.Array.[1] <- Constants.FINAL_BIT
            wbuf.Array.[2] <- byte argPDU.Reason
            wbuf.Array.[5] <- byte( wDataSegmentLength >>> 16 )
            wbuf.Array.[6] <- byte( wDataSegmentLength >>> 8 )
            wbuf.Array.[7] <- byte( wDataSegmentLength )
            Functions.UInt32ToNetworkBytes wbuf.Array 16 0xFFFFFFFFu
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( statsn_me.toPrim argPDU.StatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( cmdsn_me.toPrim argPDU.ExpCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 32 ( cmdsn_me.toPrim argPDU.MaxCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 36 ( datasn_me.toPrim argPDU.DataSN_or_R2TSN )

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            if headerOnly then
                wbuf.Return()
                return bhsLen
            else
                // Data segment length must less than or equal 0x00FFFFFF and argMaxRecvDataSegmentLength
                assert( wDataSegmentLength <= 0x00FFFFFFu && wDataSegmentLength <= argMaxRecvDataSegmentLength )

                // Create and send Header Digest
                let vDigest =
                    if argHeaderDigest = DigestType.DST_CRC32C then
                        Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                    else
                        Array.empty
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
        
                // Send DataSegment
                let! dataSegLen =
                    PDU.SendDataSegmentBytes( sock, ArraySegment( argPDU.HeaderData ), argTSIH, argCID, argCounter, objid, argDataDigest )

                wbuf.Return()
                return bhsLen + headerDigestLen + dataSegLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send NOP-Out PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A NOP-Out PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendNOPOutPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : NOPOutPDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wDataSegment = argPDU.PingData
            let wDataSegmentLength = uint32 wDataSegment.Count
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- ( Functions.SetBitflag argPDU.I Constants.IMMIDIATE_BIT ) ||| ( byte OpcodeCd.NOP_OUT )
            wbuf.Array.[1] <- Constants.FINAL_BIT
            wbuf.Array.[5] <- byte( wDataSegmentLength >>> 16 )
            wbuf.Array.[6] <- byte( wDataSegmentLength >>> 8 )
            wbuf.Array.[7] <- byte( wDataSegmentLength )
            lun_me.toBytes wbuf.Array 8 argPDU.LUN
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 20 ( ttt_me.toPrim argPDU.TargetTransferTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( cmdsn_me.toPrim argPDU.CmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( statsn_me.toPrim argPDU.ExpStatSN )

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            if headerOnly then
                wbuf.Return()
                return bhsLen
            else
                // Data segment length must less than or equal 0x00FFFFFF and argMaxRecvDataSegmentLength
                assert( wDataSegmentLength <= 0x00FFFFFFu && wDataSegmentLength <= argMaxRecvDataSegmentLength )

                // Create and send Header Digest
                let vDigest =
                    if argHeaderDigest = DigestType.DST_CRC32C then
                        Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                    else
                        Array.empty
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
        
                // Send DataSegment
                let! dataSegLen =
                    let wPingData = argPDU.PingData
                    PDU.SendDataSegmentBytes( sock, wPingData.ArraySegment, argTSIH, argCID, argCounter, objid, argDataDigest )

                wbuf.Return()
                return bhsLen + headerDigestLen + dataSegLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Send NOP-In PDU.
    /// </summary>
    /// <param name="argMaxRecvDataSegmentLength">
    ///   Effective MaxRecvDataSegmentLength value of peer.
    ///   In this function, data segment bytes in the given PDU is devided in this length.
    /// </param>
    /// <param name="argHeaderDigest">
    ///   Effective HeaderDigest parameter value. It specify header digest is present or not.
    /// </param>
    /// <param name="argDataDigest">
    ///   Effective DataDigest parameter value. It specify data digest is present or not.
    /// </param>
    /// <param name="argTSIH">
    ///   TSIH value of current session.
    /// </param>
    /// <param name="argCID">
    ///   CID value of current connection
    /// </param>
    /// <param name="argCounter">
    ///   Connection counter value of the current connection.
    /// </param>
    /// <param name="objid">
    ///   Identifier value of PDU object.
    /// </param>
    /// <param name="sock">
    ///   Interface of the stream to peer.
    /// </param>
    /// <param name="argPDU">
    ///   A NOP-In PDU that is sent to.
    /// </param>
    /// <param name="headerOnly">
    ///   If thia argument is true, this function write only header data to stream,
    ///   and argMaxRecvDataSegmentLength, argHeaderDigest and argDataDigest values are ignored.
    /// </param>
    /// <returns>
    ///   Number of bytes that has been sent.
    /// </returns>
    /// <remarks>
    ///   Write operation is ran in asynchronously.
    /// </remarks>
    static member private SendNOPInPDU
        (
            argMaxRecvDataSegmentLength : uint32,
            argHeaderDigest : DigestType,
            argDataDigest : DigestType,
            argTSIH : TSIH_T ValueOption,
            argCID : CID_T ValueOption,
            argCounter : CONCNT_T ValueOption,
            objid : OBJIDX_T,
            sock : Stream,
            argPDU : NOPInPDU,
            headerOnly : bool ) : Task<uint32> =

        task {
            let wDataSegment = argPDU.PingData
            let wDataSegmentLength = uint32 wDataSegment.Count
            let wbuf = PooledBuffer.RentAndInit 48

            // Create BHS data
            wbuf.Array.[0] <- ( byte OpcodeCd.NOP_IN )
            wbuf.Array.[1] <- Constants.FINAL_BIT
            wbuf.Array.[5] <- byte( wDataSegmentLength >>> 16 )
            wbuf.Array.[6] <- byte( wDataSegmentLength >>> 8 )
            wbuf.Array.[7] <- byte( wDataSegmentLength )
            lun_me.toBytes wbuf.Array 8 argPDU.LUN
            Functions.UInt32ToNetworkBytes wbuf.Array 16 ( itt_me.toPrim argPDU.InitiatorTaskTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 20 ( ttt_me.toPrim argPDU.TargetTransferTag )
            Functions.UInt32ToNetworkBytes wbuf.Array 24 ( statsn_me.toPrim argPDU.StatSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 28 ( cmdsn_me.toPrim argPDU.ExpCmdSN )
            Functions.UInt32ToNetworkBytes wbuf.Array 32 ( cmdsn_me.toPrim argPDU.MaxCmdSN )

            // send BHS data
            let! bhsLen = PDU.SendBytes( sock, wbuf.ArraySegment, argTSIH, argCID, argCounter, objid )

            if headerOnly then
                wbuf.Return()
                return bhsLen
            else
                // Data segment length must less than or equal 0x00FFFFFF and argMaxRecvDataSegmentLength
                assert( wDataSegmentLength <= 0x00FFFFFFu && wDataSegmentLength <= argMaxRecvDataSegmentLength )

                // Create and send Header Digest
                let vDigest =
                    if argHeaderDigest = DigestType.DST_CRC32C then
                        Functions.UInt32ToNetworkBytes_NewVec <| Functions.CRC32_A( wbuf.ArraySegment )
                    else
                        Array.empty
                let! headerDigestLen = PDU.SendBytes( sock, vDigest, argTSIH, argCID, argCounter, objid )
        
                // Send DataSegment
                let! dataSegLen =
                    PDU.SendDataSegmentBytes( sock, wDataSegment.ArraySegment, argTSIH, argCID, argCounter, objid, argDataDigest )

                wbuf.Return()
                return bhsLen + headerDigestLen + dataSegLen
        }

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Create Login response PDU from Login request PDU to uging for login reject.
    /// </summary>
    /// <param name="reqPDU">
    ///   Login request PDU that is cause occurring reject.
    /// </param>
    /// <returns>
    ///   Created Login response PDU.
    /// </returns>
    static member CreateLoginResponsePDUfromLoginRequestPDU ( reqPDU : LoginRequestPDU ) : LoginResponsePDU =
        {
            T = false;
            C = false;
            CSG = reqPDU.CSG;
            NSG = reqPDU.NSG;
            VersionMax = 0uy;       // Haruka supports iSCSI protocol version 0 only.
            VersionActive = 0uy;
            ISID = reqPDU.ISID;
            TSIH = reqPDU.TSIH;
            InitiatorTaskTag = reqPDU.InitiatorTaskTag;
            StatSN = reqPDU.ExpStatSN;
            ExpCmdSN = reqPDU.CmdSN;
            MaxCmdSN = reqPDU.CmdSN;
            Status = LoginResStatCd.SUCCESS;
            TextResponse = Array.empty;
        }
