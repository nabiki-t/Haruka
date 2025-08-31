namespace Haruka.Test.UT.Commons

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test


type PDUInterface_Test () =
   
    [<Fact>]
    member _.SCSICommandPDU_001() =
        let pdu =
            {
                I = false
                F = false;
                R = false;
                W = false;
                ATTR = TaskATTRCd.ACA_TASK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                ExpectedDataTransferLength = 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                ScsiCDB = Array.empty;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r1 = pdu ))
   
    [<Fact>]
    member _.SCSICommandPDU_002() =
        let pdu =
            {
                I = false
                F = false;
                R = false;
                W = false;
                ATTR = TaskATTRCd.ACA_TASK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                ExpectedDataTransferLength = 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                ScsiCDB = Array.empty;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r2 = pdu ))
   
    [<Fact>]
    member _.SCSICommandPDU_003() =
        let pdu =
            {
                I = false
                F = false;
                R = false;
                W = false;
                ATTR = TaskATTRCd.ACA_TASK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                ExpectedDataTransferLength = 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                ScsiCDB = Array.empty;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.False ( pdu.NeedIncrementStatSN() )
   
    [<Fact>]
    member _.SCSICommandPDU_004() =
        let pdu =
            {
                I = false
                F = false;
                R = false;
                W = false;
                ATTR = TaskATTRCd.ACA_TASK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                ExpectedDataTransferLength = 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                ScsiCDB = Array.empty;
                DataSegment = PooledBuffer.Empty;
                BidirectionalExpectedReadDataLength = 0u;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.Irrelevant ))

    [<Fact>]
    member _.SCSIResponsePDU_001() =
        let pdu =
            {
                o = false;
                u = false;
                O = false;
                U = false;
                Response = iScsiSvcRespCd.COMMAND_COMPLETE;
                Status = ScsiCmdStatCd.ACA_ACTIVE;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                SNACKTag = snacktag_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                ExpDataSN = datasn_me.zero;
                BidirectionalReadResidualCount = 0u;
                ResidualCount = 0u;
                SenseLength = 0us;
                SenseData = ArraySegment.Empty;
                ResponseData = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                DataInBuffer = PooledBuffer.Empty;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r1 = {
                o = false;
                u = false;
                O = false;
                U = false;
                Response = iScsiSvcRespCd.COMMAND_COMPLETE;
                Status = ScsiCmdStatCd.ACA_ACTIVE;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                SNACKTag = snacktag_me.fromPrim 0u;
                StatSN = statsn_me.fromPrim 1u;
                ExpCmdSN = cmdsn_me.fromPrim 1u;
                MaxCmdSN = cmdsn_me.fromPrim 2u;
                ExpDataSN = datasn_me.zero;
                BidirectionalReadResidualCount = 0u;
                ResidualCount = 0u;
                SenseLength = 0us;
                SenseData = ArraySegment.Empty;
                ResponseData = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                DataInBuffer = PooledBuffer.Empty;
        } ))

    [<Fact>]
    member _.SCSIResponsePDU_002() =
        let pdu =
            {
                o = false;
                u = false;
                O = false;
                U = false;
                Response = iScsiSvcRespCd.COMMAND_COMPLETE;
                Status = ScsiCmdStatCd.ACA_ACTIVE;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                SNACKTag = snacktag_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                ExpDataSN = datasn_me.zero;
                BidirectionalReadResidualCount = 0u;
                ResidualCount = 0u;
                SenseLength = 0us;
                SenseData = ArraySegment.Empty;
                ResponseData = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                DataInBuffer = PooledBuffer.Empty;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 4u ) ( cmdsn_me.fromPrim 5u ) ( cmdsn_me.fromPrim 6u )
        Assert.True(( r2 = {
                o = false;
                u = false;
                O = false;
                U = false;
                Response = iScsiSvcRespCd.COMMAND_COMPLETE;
                Status = ScsiCmdStatCd.ACA_ACTIVE;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                SNACKTag = snacktag_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.fromPrim 5u;
                MaxCmdSN = cmdsn_me.fromPrim 6u;
                ExpDataSN = datasn_me.zero;
                BidirectionalReadResidualCount = 0u;
                ResidualCount = 0u;
                SenseLength = 0us;
                SenseData = ArraySegment.Empty;
                ResponseData = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                DataInBuffer = PooledBuffer.Empty;
        } ))

    [<Fact>]
    member _.SCSIResponsePDU_003() =
        let pdu =
            {
                o = false;
                u = false;
                O = false;
                U = false;
                Response = iScsiSvcRespCd.COMMAND_COMPLETE;
                Status = ScsiCmdStatCd.ACA_ACTIVE;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                SNACKTag = snacktag_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                ExpDataSN = datasn_me.zero;
                BidirectionalReadResidualCount = 0u;
                ResidualCount = 0u;
                SenseLength = 0us;
                SenseData = ArraySegment.Empty;
                ResponseData = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                DataInBuffer = PooledBuffer.Empty;
            } :> ILogicalPDU
        Assert.True ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.SCSIResponsePDU_004() =
        let pdu =
            {
                o = false;
                u = false;
                O = false;
                U = false;
                Response = iScsiSvcRespCd.COMMAND_COMPLETE;
                Status = ScsiCmdStatCd.ACA_ACTIVE;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                SNACKTag = snacktag_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                ExpDataSN = datasn_me.zero;
                BidirectionalReadResidualCount = 0u;
                ResidualCount = 0u;
                SenseLength = 0us;
                SenseData = ArraySegment.Empty;
                ResponseData = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
                DataInBuffer = PooledBuffer.Empty;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.W_Mode ))

    [<Fact>]
    member _.TaskManagementFunctionRequestPDU_001() =
        let pdu =
            {
                I = true;
                Function = TaskMgrReqCd.ABORT_TASK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                ReferencedTaskTag = itt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                RefCmdSN = cmdsn_me.zero;
                ExpDataSN = datasn_me.zero;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r1 = pdu ))

    [<Fact>]
    member _.TaskManagementFunctionRequestPDU_002() =
        let pdu =
            {
                I = true;
                Function = TaskMgrReqCd.ABORT_TASK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                ReferencedTaskTag = itt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                RefCmdSN = cmdsn_me.zero;
                ExpDataSN = datasn_me.zero;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r2 = pdu ))

    [<Fact>]
    member _.TaskManagementFunctionRequestPDU_003() =
        let pdu =
            {
                I = true;
                Function = TaskMgrReqCd.ABORT_TASK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                ReferencedTaskTag = itt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                RefCmdSN = cmdsn_me.zero;
                ExpDataSN = datasn_me.zero;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.False ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.TaskManagementFunctionRequestPDU_004() =
        let pdu =
            {
                I = true;
                Function = TaskMgrReqCd.ABORT_TASK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                ReferencedTaskTag = itt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                RefCmdSN = cmdsn_me.zero;
                ExpDataSN = datasn_me.zero;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.Irrelevant ))
        
    [<Fact>]
    member _.TaskManagementFunctionResponsePDU_001() =
        let pdu =
            {
                Response = TaskMgrResCd.AUTH_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                ResponseFence = ResponseFenceNeedsFlag.R_Mode;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r1 = {
                Response = TaskMgrResCd.AUTH_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.fromPrim 1u;
                ExpCmdSN = cmdsn_me.fromPrim 1u;
                MaxCmdSN = cmdsn_me.fromPrim 2u;
                ResponseFence = ResponseFenceNeedsFlag.R_Mode;
        } ))
        
    [<Fact>]
    member _.TaskManagementFunctionResponsePDU_002() =
        let pdu =
            {
                Response = TaskMgrResCd.AUTH_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                ResponseFence = ResponseFenceNeedsFlag.R_Mode;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 4u ) ( cmdsn_me.fromPrim 5u ) ( cmdsn_me.fromPrim 6u )
        Assert.True(( r2 = {
                Response = TaskMgrResCd.AUTH_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.fromPrim 5u;
                MaxCmdSN = cmdsn_me.fromPrim 6u;
                ResponseFence = ResponseFenceNeedsFlag.R_Mode;
        } ))
        
    [<Fact>]
    member _.TaskManagementFunctionResponsePDU_003() =
        let pdu =
            {
                Response = TaskMgrResCd.AUTH_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                ResponseFence = ResponseFenceNeedsFlag.R_Mode;
            } :> ILogicalPDU
        Assert.True ( pdu.NeedIncrementStatSN() )
        
    [<Fact>]
    member _.TaskManagementFunctionResponsePDU_004() =
        let pdu =
            {
                Response = TaskMgrResCd.AUTH_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                ResponseFence = ResponseFenceNeedsFlag.R_Mode;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.R_Mode ))

    [<Fact>]
    member _.SCSIDataOutPDU_001() =
        let pdu =
            {
                F = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                ExpStatSN = statsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                DataSegment = PooledBuffer.Empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r1 = pdu ))

    [<Fact>]
    member _.SCSIDataOutPDU_002() =
        let pdu =
            {
                F = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                ExpStatSN = statsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                DataSegment = PooledBuffer.Empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r2 = pdu ))

    [<Fact>]
    member _.SCSIDataOutPDU_003() =
        let pdu =
            {
                F = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                ExpStatSN = statsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                DataSegment = PooledBuffer.Empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.False ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.SCSIDataOutPDU_004() =
        let pdu =
            {
                F = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                ExpStatSN = statsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                DataSegment = PooledBuffer.Empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.Irrelevant ))

    [<Fact>]
    member _.SCSIDataOutPDU_005() =
        let defaultSCSIDataOutPDU =
            {
                F = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                ExpStatSN = statsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                DataSegment = PooledBuffer.Empty;
                ByteCount = 0u;
            }
        let cmdData = PooledBuffer.Rent [| 1uy; |]
        let data = [
            { 
                defaultSCSIDataOutPDU with
                    BufferOffset = 3u;
                    DataSegment = PooledBuffer.Empty;
            };
            { 
                defaultSCSIDataOutPDU with
                    BufferOffset = 15u;
                    DataSegment = PooledBuffer.Rent [| 15uy; 16uy; 17uy; 18uy; 19uy; |];
            };
            { 
                defaultSCSIDataOutPDU with
                    BufferOffset = 5u;
                    DataSegment = PooledBuffer.Rent [| 5uy; 6uy; 7uy; 8uy; 9uy; |];
            };
            { 
                defaultSCSIDataOutPDU with
                    BufferOffset = 19u;
                    DataSegment = PooledBuffer.Rent [| 219uy; 220uy; |];
            };
            { 
                defaultSCSIDataOutPDU with
                    BufferOffset = 8u;
                    DataSegment = PooledBuffer.Rent [| 108uy; 109uy; 110uy; 111uy; 112uy; 113uy; |];
            };
        ]

        let v = SCSIDataOutPDU.AppendParamList cmdData data 18
        Assert.True((
            PooledBuffer.ValueEqualsWithArray v [|
                1uy;   0uy;   0uy;   0uy;   0uy;
                5uy;   6uy;   7uy;   108uy; 109uy;
                110uy; 111uy; 112uy; 113uy; 0uy;
                15uy;  16uy;  17uy;
            |]
        ))

    [<Fact>]
    member _.SCSIDataOutPDU_006() =
        let v = SCSIDataOutPDU.AppendParamList PooledBuffer.Empty [] 10
        Assert.True(( PooledBuffer.ValueEqualsWithArray v Array.empty ))

    [<Fact>]
    member _.SCSIDataOutPDU_007() =
        let v = SCSIDataOutPDU.AppendParamList ( PooledBuffer.Rent [| 1uy; 2uy |] ) [] 10
        Assert.True(( PooledBuffer.ValueEqualsWithArray v [| 1uy; 2uy |] ))

    [<Fact>]
    member _.SCSIDataOutPDU_008() =
        let v = SCSIDataOutPDU.AppendParamList ( PooledBuffer( [| 0uy; 1uy; 2uy; 0uy; |], 2 ) ) [] 10
        Assert.True(( PooledBuffer.ValueEqualsWithArray v [| 0uy; 1uy |] ))

    [<Fact>]
    member _.SCSIDataOutPDU_009() =
        let cmdData = PooledBuffer( [| 1uy; 99uy; |], 1 )
        let data = [
            { 
                F = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                ExpStatSN = statsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 15u;
                DataSegment = PooledBuffer( [| 15uy; 16uy; 17uy; 18uy; 19uy; 0uy; 0uy; |], 5 );
                ByteCount = 0u;
            };
        ]

        let v = SCSIDataOutPDU.AppendParamList cmdData data 18
        Assert.True((
            PooledBuffer.ValueEqualsWithArray v [|
                1uy;  0uy;  0uy;  0uy; 0uy;
                0uy;  0uy;  0uy;  0uy; 0uy;
                0uy;  0uy;  0uy;  0uy; 0uy;
                15uy; 16uy; 17uy;
            |]
        ))

    [<Fact>]
    member _.SCSIDataInPDU_001() =
        let pdu =
            {
                F = false;
                A = false;
                O = false;
                U = false;
                S = false;
                Status = ScsiCmdStatCd.BUSY;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                ResidualCount = 0u;
                DataSegment = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 10u ) ( cmdsn_me.fromPrim 11u ) ( cmdsn_me.fromPrim 12u )
        Assert.True(( r1 = {
                F = false;
                A = false;
                O = false;
                U = false;
                S = false;
                Status = ScsiCmdStatCd.BUSY;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.fromPrim 11u;
                MaxCmdSN = cmdsn_me.fromPrim 12u;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                ResidualCount = 0u;
                DataSegment = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
        } ))

    [<Fact>]
    member _.SCSIDataInPDU_002() =
        let pdu =
            {
                F = false;
                A = false;
                O = false;
                U = false;
                S = false;
                Status = ScsiCmdStatCd.BUSY;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                ResidualCount = 0u;
                DataSegment = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 13u ) ( cmdsn_me.fromPrim 14u ) ( cmdsn_me.fromPrim 15u )
        Assert.True(( r2 = {
                F = false;
                A = false;
                O = false;
                U = false;
                S = false;
                Status = ScsiCmdStatCd.BUSY;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.fromPrim 14u;
                MaxCmdSN = cmdsn_me.fromPrim 15u;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                ResidualCount = 0u;
                DataSegment = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
        } ))

    [<Fact>]
    member _.SCSIDataInPDU_003() =
        let pdu =
            {
                F = false;
                A = false;
                O = false;
                U = false;
                S = false;
                Status = ScsiCmdStatCd.BUSY;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                ResidualCount = 0u;
                DataSegment = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
            } :> ILogicalPDU
        Assert.False ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.SCSIDataInPDU_004() =
        let pdu =
            {
                F = false;
                A = false;
                O = false;
                U = false;
                S = false;
                Status = ScsiCmdStatCd.BUSY;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                ResidualCount = 0u;
                DataSegment = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.Immediately ))

    [<Fact>]
    member _.SCSIDataInPDU_005() =
        let pdu =
            {
                F = false;
                A = false;
                O = false;
                U = false;
                S = true;
                Status = ScsiCmdStatCd.BUSY;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                ResidualCount = 0u;
                DataSegment = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 10u ) ( cmdsn_me.fromPrim 11u ) ( cmdsn_me.fromPrim 12u )
        Assert.True(( r1 = {
                F = false;
                A = false;
                O = false;
                U = false;
                S = true;
                Status = ScsiCmdStatCd.BUSY;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.fromPrim 10u;
                ExpCmdSN = cmdsn_me.fromPrim 11u;
                MaxCmdSN = cmdsn_me.fromPrim 12u;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                ResidualCount = 0u;
                DataSegment = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
        } ))

    [<Fact>]
    member _.SCSIDataInPDU_006() =
        let pdu =
            {
                F = false;
                A = false;
                O = false;
                U = false;
                S = true;
                Status = ScsiCmdStatCd.BUSY;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                ResidualCount = 0u;
                DataSegment = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 13u ) ( cmdsn_me.fromPrim 14u ) ( cmdsn_me.fromPrim 15u )
        Assert.True(( r2 = {
                F = false;
                A = false;
                O = false;
                U = false;
                S = true;
                Status = ScsiCmdStatCd.BUSY;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.fromPrim 14u;
                MaxCmdSN = cmdsn_me.fromPrim 15u;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                ResidualCount = 0u;
                DataSegment = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
        } ))

    [<Fact>]
    member _.SCSIDataInPDU_007() =
        let pdu =
            {
                F = false;
                A = false;
                O = false;
                U = false;
                S = true;
                Status = ScsiCmdStatCd.BUSY;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                ResidualCount = 0u;
                DataSegment = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
            } :> ILogicalPDU
        Assert.True ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.SCSIDataInPDU_008() =
        let pdu =
            {
                F = false;
                A = false;
                O = false;
                U = false;
                S = true;
                Status = ScsiCmdStatCd.BUSY;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                DataSN = datasn_me.zero;
                BufferOffset = 0u;
                ResidualCount = 0u;
                DataSegment = ArraySegment.Empty;
                ResponseFence = ResponseFenceNeedsFlag.W_Mode;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.W_Mode ))

    [<Fact>]
    member _.R2TPDU_001() =
        let pdu =
            {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                R2TSN = datasn_me.zero;
                BufferOffset = 0u;
                DesiredDataTransferLength = 0u;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r1 = {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.fromPrim 1u;
                ExpCmdSN = cmdsn_me.fromPrim 1u;
                MaxCmdSN = cmdsn_me.fromPrim 2u;
                R2TSN = datasn_me.zero;
                BufferOffset = 0u;
                DesiredDataTransferLength = 0u;
        } ))

    [<Fact>]
    member _.R2TPDU_002() =
        let pdu =
            {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                R2TSN = datasn_me.zero;
                BufferOffset = 0u;
                DesiredDataTransferLength = 0u;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 4u ) ( cmdsn_me.fromPrim 5u ) ( cmdsn_me.fromPrim 6u )
        Assert.True(( r2 = {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.fromPrim 4u;
                ExpCmdSN = cmdsn_me.fromPrim 5u;
                MaxCmdSN = cmdsn_me.fromPrim 6u;
                R2TSN = datasn_me.zero;
                BufferOffset = 0u;
                DesiredDataTransferLength = 0u;
        } ))

    [<Fact>]
    member _.R2TPDU_003() =
        let pdu =
            {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                R2TSN = datasn_me.zero;
                BufferOffset = 0u;
                DesiredDataTransferLength = 0u;
            } :> ILogicalPDU
        Assert.False ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.R2TPDU_004() =
        let pdu =
            {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                R2TSN = datasn_me.zero;
                BufferOffset = 0u;
                DesiredDataTransferLength = 0u;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.Immediately ))

    [<Fact>]
    member _.AsyncronousMessagePDU_001() =
        let pdu =
            {
                LUN = lun_me.zero;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                AsyncEvent = AsyncEventCd.CONNECTION_CLOSE;
                AsyncVCode = 0uy;
                Parameter1 = 0us;
                Parameter2 = 0us;
                Parameter3 = 0us;
                SenseLength = 0us;
                SenseData = Array.empty;
                ISCSIEventData = Array.empty;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u ) ( cmdsn_me.fromPrim 3u )
        Assert.True(( r1 = {
                LUN = lun_me.zero;
                StatSN = statsn_me.fromPrim 1u;
                ExpCmdSN = cmdsn_me.fromPrim 2u;
                MaxCmdSN = cmdsn_me.fromPrim 3u;
                AsyncEvent = AsyncEventCd.CONNECTION_CLOSE;
                AsyncVCode = 0uy;
                Parameter1 = 0us;
                Parameter2 = 0us;
                Parameter3 = 0us;
                SenseLength = 0us;
                SenseData = Array.empty;
                ISCSIEventData = Array.empty;
        } ))

    [<Fact>]
    member _.AsyncronousMessagePDU_002() =
        let pdu =
            {
                LUN = lun_me.zero;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                AsyncEvent = AsyncEventCd.CONNECTION_CLOSE;
                AsyncVCode = 0uy;
                Parameter1 = 0us;
                Parameter2 = 0us;
                Parameter3 = 0us;
                SenseLength = 0us;
                SenseData = Array.empty;
                ISCSIEventData = Array.empty;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 4u ) ( cmdsn_me.fromPrim 5u ) ( cmdsn_me.fromPrim 6u )
        Assert.True(( r2 = {
                LUN = lun_me.zero;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.fromPrim 5u;
                MaxCmdSN = cmdsn_me.fromPrim 6u;
                AsyncEvent = AsyncEventCd.CONNECTION_CLOSE;
                AsyncVCode = 0uy;
                Parameter1 = 0us;
                Parameter2 = 0us;
                Parameter3 = 0us;
                SenseLength = 0us;
                SenseData = Array.empty;
                ISCSIEventData = Array.empty;
        } ))

    [<Fact>]
    member _.AsyncronousMessagePDU_003() =
        let pdu =
            {
                LUN = lun_me.zero;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                AsyncEvent = AsyncEventCd.CONNECTION_CLOSE;
                AsyncVCode = 0uy;
                Parameter1 = 0us;
                Parameter2 = 0us;
                Parameter3 = 0us;
                SenseLength = 0us;
                SenseData = Array.empty;
                ISCSIEventData = Array.empty;
            } :> ILogicalPDU
        Assert.True ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.AsyncronousMessagePDU_004() =
        let pdu =
            {
                LUN = lun_me.zero;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                AsyncEvent = AsyncEventCd.CONNECTION_CLOSE;
                AsyncVCode = 0uy;
                Parameter1 = 0us;
                Parameter2 = 0us;
                Parameter3 = 0us;
                SenseLength = 0us;
                SenseData = Array.empty;
                ISCSIEventData = Array.empty;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.R_Mode ))

    [<Fact>]
    member _.TextRequestPDU_001() =
        let pdu =
            {
                I = false;
                F = false;
                C = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                TextRequest = Array.empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r1 = pdu ))

    [<Fact>]
    member _.TextRequestPDU_002() =
        let pdu =
            {
                I = false;
                F = false;
                C = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                TextRequest = Array.empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r2 = pdu ))

    [<Fact>]
    member _.TextRequestPDU_003() =
        let pdu =
            {
                I = false;
                F = false;
                C = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                TextRequest = Array.empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.False ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.TextRequestPDU_004() =
        let pdu =
            {
                I = false;
                F = false;
                C = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                TextRequest = Array.empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.Irrelevant ))

    [<Fact>]
    member _.TextResponsePDU_001() =
        let pdu =
            {
                F = false;
                C = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                TextResponse = Array.empty;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u ) ( cmdsn_me.fromPrim 3u )
        Assert.True(( r1 = {
                F = false;
                C = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.fromPrim 1u;
                ExpCmdSN = cmdsn_me.fromPrim 2u;
                MaxCmdSN = cmdsn_me.fromPrim 3u;
                TextResponse = Array.empty;
        } ))

    [<Fact>]
    member _.TextResponsePDU_002() =
        let pdu =
            {
                F = false;
                C = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                TextResponse = Array.empty;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 4u ) ( cmdsn_me.fromPrim 5u ) ( cmdsn_me.fromPrim 6u )
        Assert.True(( r2 = {
                F = false;
                C = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.fromPrim 5u;
                MaxCmdSN = cmdsn_me.fromPrim 6u;
                TextResponse = Array.empty;
        } ))

    [<Fact>]
    member _.TextResponsePDU_003() =
        let pdu =
            {
                F = false;
                C = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                TextResponse = Array.empty;
            } :> ILogicalPDU
        Assert.True ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.TextResponsePDU_004() =
        let pdu =
            {
                F = false;
                C = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                TextResponse = Array.empty;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.R_Mode ))

    [<Fact>]
    member _.LoginRequestPDU_001() =
        let pdu =
            {
                T = false;
                C = false;
                CSG = LoginReqStateCd.FULL;
                NSG = LoginReqStateCd.FULL;
                VersionMax = 0uy;
                VersionMin = 0uy;
                ISID = isid_me.zero;
                TSIH = tsih_me.fromPrim 0us;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                CID = cid_me.fromPrim 0us;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                TextRequest = Array.empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r1 = pdu ))

    [<Fact>]
    member _.LoginRequestPDU_002() =
        let pdu =
            {
                T = false;
                C = false;
                CSG = LoginReqStateCd.FULL;
                NSG = LoginReqStateCd.FULL;
                VersionMax = 0uy;
                VersionMin = 0uy;
                ISID = isid_me.zero;
                TSIH = tsih_me.fromPrim 0us;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                CID = cid_me.fromPrim 0us;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                TextRequest = Array.empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r2 = pdu ))

    [<Fact>]
    member _.LoginRequestPDU_003() =
        let pdu =
            {
                T = false;
                C = false;
                CSG = LoginReqStateCd.FULL;
                NSG = LoginReqStateCd.FULL;
                VersionMax = 0uy;
                VersionMin = 0uy;
                ISID = isid_me.zero;
                TSIH = tsih_me.fromPrim 0us;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                CID = cid_me.fromPrim 0us;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                TextRequest = Array.empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.False ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.LoginRequestPDU_004() =
        let pdu =
            {
                T = false;
                C = false;
                CSG = LoginReqStateCd.FULL;
                NSG = LoginReqStateCd.FULL;
                VersionMax = 0uy;
                VersionMin = 0uy;
                ISID = isid_me.zero;
                TSIH = tsih_me.fromPrim 0us;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                CID = cid_me.fromPrim 0us;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                TextRequest = Array.empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.Irrelevant ))

    [<Fact>]
    member _.LoginResponsePDU_001() =
        let pdu =
            {
                T = false;
                C = false;
                CSG = LoginReqStateCd.OPERATIONAL; 
                NSG = LoginReqStateCd.OPERATIONAL;
                VersionMax = 0uy;
                VersionActive = 0uy;
                ISID = isid_me.zero;
                TSIH = tsih_me.fromPrim 0us;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                Status = LoginResStatCd.AUTH_FAILURE;
                TextResponse = Array.empty;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u ) ( cmdsn_me.fromPrim 3u )
        Assert.True(( r1 = {
                T = false;
                C = false;
                CSG = LoginReqStateCd.OPERATIONAL; 
                NSG = LoginReqStateCd.OPERATIONAL;
                VersionMax = 0uy;
                VersionActive = 0uy;
                ISID = isid_me.zero;
                TSIH = tsih_me.fromPrim 0us;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.fromPrim 1u;
                ExpCmdSN = cmdsn_me.fromPrim 2u;
                MaxCmdSN = cmdsn_me.fromPrim 3u;
                Status = LoginResStatCd.AUTH_FAILURE;
                TextResponse = Array.empty;
        } ))

    [<Fact>]
    member _.LoginResponsePDU_002() =
        let pdu =
            {
                T = false;
                C = false;
                CSG = LoginReqStateCd.OPERATIONAL; 
                NSG = LoginReqStateCd.OPERATIONAL;
                VersionMax = 0uy;
                VersionActive = 0uy;
                ISID = isid_me.zero;
                TSIH = tsih_me.fromPrim 0us;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                Status = LoginResStatCd.AUTH_FAILURE;
                TextResponse = Array.empty;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 4u ) ( cmdsn_me.fromPrim 5u ) ( cmdsn_me.fromPrim 6u )
        Assert.True(( r2 = {
                T = false;
                C = false;
                CSG = LoginReqStateCd.OPERATIONAL; 
                NSG = LoginReqStateCd.OPERATIONAL;
                VersionMax = 0uy;
                VersionActive = 0uy;
                ISID = isid_me.zero;
                TSIH = tsih_me.fromPrim 0us;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.fromPrim 5u;
                MaxCmdSN = cmdsn_me.fromPrim 6u;
                Status = LoginResStatCd.AUTH_FAILURE;
                TextResponse = Array.empty;
        } ))

    [<Fact>]
    member _.LoginResponsePDU_003() =
        let pdu =
            {
                T = false;
                C = false;
                CSG = LoginReqStateCd.OPERATIONAL; 
                NSG = LoginReqStateCd.OPERATIONAL;
                VersionMax = 0uy;
                VersionActive = 0uy;
                ISID = isid_me.zero;
                TSIH = tsih_me.fromPrim 0us;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                Status = LoginResStatCd.AUTH_FAILURE;
                TextResponse = Array.empty;
            } :> ILogicalPDU
        Assert.True ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.LoginResponsePDU_004() =
        let pdu =
            {
                T = false;
                C = false;
                CSG = LoginReqStateCd.OPERATIONAL; 
                NSG = LoginReqStateCd.OPERATIONAL;
                VersionMax = 0uy;
                VersionActive = 0uy;
                ISID = isid_me.zero;
                TSIH = tsih_me.fromPrim 0us;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                Status = LoginResStatCd.AUTH_FAILURE;
                TextResponse = Array.empty;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.R_Mode ))

    [<Fact>]
    member _.LogoutRequestPDU_001() =
        let pdu =
            {
                I = false;
                ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                CID = cid_me.fromPrim 0us;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r1 = pdu ))

    [<Fact>]
    member _.LogoutRequestPDU_002() =
        let pdu =
            {
                I = false;
                ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                CID = cid_me.fromPrim 0us;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r2 = pdu ))

    [<Fact>]
    member _.LogoutRequestPDU_003() =
        let pdu =
            {
                I = false;
                ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                CID = cid_me.fromPrim 0us;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.False ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.LogoutRequestPDU_004() =
        let pdu =
            {
                I = false;
                ReasonCode = LogoutReqReasonCd.CLOSE_CONN;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                CID = cid_me.fromPrim 0us;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.Irrelevant ))

    [<Fact>]
    member _.LogoutResponsePDU_001() =
        let pdu =
            {
                Response = LogoutResCd.CLEANUP_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                Time2Wait = 0us;
                Time2Retain = 0us;
                CloseAllegiantConnection = true;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u ) ( cmdsn_me.fromPrim 3u )
        Assert.True(( r1 = {
                Response = LogoutResCd.CLEANUP_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.fromPrim 1u;
                ExpCmdSN = cmdsn_me.fromPrim 2u;
                MaxCmdSN = cmdsn_me.fromPrim 3u;
                Time2Wait = 0us;
                Time2Retain = 0us;
                CloseAllegiantConnection = true;
        } ))

    [<Fact>]
    member _.LogoutResponsePDU_002() =
        let pdu =
            {
                Response = LogoutResCd.CLEANUP_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                Time2Wait = 0us;
                Time2Retain = 0us;
                CloseAllegiantConnection = true;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 4u ) ( cmdsn_me.fromPrim 5u ) ( cmdsn_me.fromPrim 6u )
        Assert.True(( r2 = {
                Response = LogoutResCd.CLEANUP_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.fromPrim 5u;
                MaxCmdSN = cmdsn_me.fromPrim 6u;
                Time2Wait = 0us;
                Time2Retain = 0us;
                CloseAllegiantConnection = true;
        } ))

    [<Fact>]
    member _.LogoutResponsePDU_003() =
        let pdu =
            {
                Response = LogoutResCd.CLEANUP_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                Time2Wait = 0us;
                Time2Retain = 0us;
                CloseAllegiantConnection = true;
            } :> ILogicalPDU
        Assert.True ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.LogoutResponsePDU_004() =
        let pdu =
            {
                Response = LogoutResCd.CLEANUP_FAILED;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                Time2Wait = 0us;
                Time2Retain = 0us;
                CloseAllegiantConnection = true;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.R_Mode ))

    [<Fact>]
    member _.SNACKRequestPDU_001() =
        let pdu =
            {
                Type = SnackReqTypeCd.DATA_ACK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                ExpStatSN = statsn_me.zero;
                BegRun = 0u;
                RunLength = 0u;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r1 = pdu ))

    [<Fact>]
    member _.SNACKRequestPDU_002() =
        let pdu =
            {
                Type = SnackReqTypeCd.DATA_ACK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                ExpStatSN = statsn_me.zero;
                BegRun = 0u;
                RunLength = 0u;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r2 = pdu ))

    [<Fact>]
    member _.SNACKRequestPDU_003() =
        let pdu =
            {
                Type = SnackReqTypeCd.DATA_ACK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                ExpStatSN = statsn_me.zero;
                BegRun = 0u;
                RunLength = 0u;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.False ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.SNACKRequestPDU_004() =
        let pdu =
            {
                Type = SnackReqTypeCd.DATA_ACK;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                ExpStatSN = statsn_me.zero;
                BegRun = 0u;
                RunLength = 0u;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.Irrelevant ))

    [<Fact>]
    member _.RejectPDU_001() =
        let pdu =
            {
                Reason = RejectReasonCd.COM_NOT_SUPPORT;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                DataSN_or_R2TSN = datasn_me.zero;
                HeaderData = Array.empty;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u ) ( cmdsn_me.fromPrim 3u )
        Assert.True(( r1 = {
                Reason = RejectReasonCd.COM_NOT_SUPPORT;
                StatSN = statsn_me.fromPrim 1u;
                ExpCmdSN = cmdsn_me.fromPrim 2u;
                MaxCmdSN = cmdsn_me.fromPrim 3u;
                DataSN_or_R2TSN = datasn_me.zero;
                HeaderData = Array.empty;
        } ))

    [<Fact>]
    member _.RejectPDU_002() =
        let pdu =
            {
                Reason = RejectReasonCd.COM_NOT_SUPPORT;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                DataSN_or_R2TSN = datasn_me.zero;
                HeaderData = Array.empty;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 4u ) ( cmdsn_me.fromPrim 5u ) ( cmdsn_me.fromPrim 6u )
        Assert.True(( r2 = {
                Reason = RejectReasonCd.COM_NOT_SUPPORT;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.fromPrim 5u;
                MaxCmdSN = cmdsn_me.fromPrim 6u;
                DataSN_or_R2TSN = datasn_me.zero;
                HeaderData = Array.empty;
        } ))

    [<Fact>]
    member _.RejectPDU_003() =
        let pdu =
            {
                Reason = RejectReasonCd.COM_NOT_SUPPORT;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                DataSN_or_R2TSN = datasn_me.zero;
                HeaderData = Array.empty;
            } :> ILogicalPDU
        Assert.True ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.RejectPDU_004() =
        let pdu =
            {
                Reason = RejectReasonCd.COM_NOT_SUPPORT;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                DataSN_or_R2TSN = datasn_me.zero;
                HeaderData = Array.empty;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.R_Mode ))

    [<Fact>]
    member _.NOPOutPDU_001() =
        let pdu =
            {
                I = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                PingData = PooledBuffer.Empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r1 = pdu ))

    [<Fact>]
    member _.NOPOutPDU_002() =
        let pdu =
            {
                I = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                PingData = PooledBuffer.Empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u )
        Assert.True(( r2 = pdu ))

    [<Fact>]
    member _.NOPOutPDU_003() =
        let pdu =
            {
                I = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                PingData = PooledBuffer.Empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.False ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.NOPOutPDU_004() =
        let pdu =
            {
                I = false;
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                CmdSN = cmdsn_me.zero;
                ExpStatSN = statsn_me.zero;
                PingData = PooledBuffer.Empty;
                ByteCount = 0u;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.Irrelevant ))

    [<Fact>]
    member _.NOPInPDU_001() =
        let pdu =
            {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                PingData = PooledBuffer.Empty;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u ) ( cmdsn_me.fromPrim 3u )
        Assert.True(( r1 = {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.fromPrim 1u;
                ExpCmdSN = cmdsn_me.fromPrim 2u;
                MaxCmdSN = cmdsn_me.fromPrim 3u;
                PingData = PooledBuffer.Empty;
        } ))

    [<Fact>]
    member _.NOPInPDU_002() =
        let pdu =
            {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                PingData = PooledBuffer.Empty;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 4u ) ( cmdsn_me.fromPrim 5u ) ( cmdsn_me.fromPrim 6u )
        Assert.True(( r2 = {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.fromPrim 5u;
                MaxCmdSN = cmdsn_me.fromPrim 6u;
                PingData = PooledBuffer.Empty;
        } ))

    [<Fact>]
    member _.NOPInPDU_003() =
        let pdu =
            {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                PingData = PooledBuffer.Empty;
            } :> ILogicalPDU
        Assert.False ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.NOPInPDU_004() =
        let pdu =
            {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0xFFFFFFFFu;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                PingData = PooledBuffer.Empty;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.Immediately ))

    [<Fact>]
    member _.NOPInPDU_005() =
        let pdu =
            {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                PingData = PooledBuffer.Empty;
            } :> ILogicalPDU
        let r1 = pdu.UpdateTargetValues ( statsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u ) ( cmdsn_me.fromPrim 3u )
        Assert.True(( r1 = {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.fromPrim 1u;
                ExpCmdSN = cmdsn_me.fromPrim 2u;
                MaxCmdSN = cmdsn_me.fromPrim 3u;
                PingData = PooledBuffer.Empty;
        } ))

    [<Fact>]
    member _.NOPInPDU_006() =
        let pdu =
            {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                PingData = PooledBuffer.Empty;
            } :> ILogicalPDU
        let r2 = pdu.UpdateTargetValuesForResend ( statsn_me.fromPrim 4u ) ( cmdsn_me.fromPrim 5u ) ( cmdsn_me.fromPrim 6u )
        Assert.True(( r2 = {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.fromPrim 5u;
                MaxCmdSN = cmdsn_me.fromPrim 6u;
                PingData = PooledBuffer.Empty;
        } ))

    [<Fact>]
    member _.NOPInPDU_007() =
        let pdu =
            {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                PingData = PooledBuffer.Empty;
            } :> ILogicalPDU
        Assert.True ( pdu.NeedIncrementStatSN() )

    [<Fact>]
    member _.NOPInPDU_008() =
        let pdu =
            {
                LUN = lun_me.zero;
                InitiatorTaskTag = itt_me.fromPrim 0u;
                TargetTransferTag = ttt_me.fromPrim 0u;
                StatSN = statsn_me.zero;
                ExpCmdSN = cmdsn_me.zero;
                MaxCmdSN = cmdsn_me.zero;
                PingData = PooledBuffer.Empty;
            } :> ILogicalPDU
        Assert.True(( pdu.NeedResponseFence = ResponseFenceNeedsFlag.R_Mode ))
