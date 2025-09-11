//=============================================================================
// Haruka Software Storage.
// PRManagerTest3.fs : Test cases for PRManager class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.BlockDeviceLU

//=============================================================================
// Import declaration

open System
open System.IO
open System.Text
open System.Threading

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.BlockDeviceLU
open Haruka.IODataTypes
open Haruka.Test

//=============================================================================
// Class implementation

type PRManager_Test3 () =

    ///////////////////////////////////////////////////////////////////////////
    // Common definition

    let defaultSource =  {
        I_TNexus = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        CID = cid_me.zero;
        ConCounter = concnt_me.zero;
        TSIH = tsih_me.zero;
        ProtocolService = new CProtocolService_Stub();
        SessionKiller = new HKiller()
    }

    do
        let lock = GlbFunc.LogParamUpdateLock()
        HLogger.SetLogParameters( 100u, 100u, 0u, LogLevel.LOGLEVEL_OFF, stderr )
        lock.Release() |> ignore

    member _.CreateTestDir() =
        let w1 = Functions.AppendPathName ( Path.GetTempPath() ) "PRManager_Test3"
        GlbFunc.CreateDir w1 |> ignore
        w1

    static member GetPRInfoRec( pm : PRManager ) =
        let pc = new PrivateCaller( pm )
        ( pc.GetField( "m_Locker" ) :?> OptimisticLock< PRInfoRec > ).obj

    member _.CreateDefaultPM ( fname : string ) ( expType : PR_TYPE ) ( expCnt : int ) =
        let k = new HKiller() :> IKiller
        let statStub = new CStatus_Stub(
            p_GetTargetFromLUN = ( fun lun ->
                Assert.True(( lun = lun_me.zero || lun = lun_me.fromPrim 1UL || lun = lun_me.fromPrim 2UL ))
                [
                    if lun = lun_me.zero || lun = lun_me.fromPrim 1UL then
                        yield {
                            IdentNumber = tnodeidx_me.fromPrim 0u;
                            TargetName = "target000";
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                            LUN = [ lun_me.zero; lun_me.fromPrim 1UL; ];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        };
                    if lun = lun_me.zero || lun = lun_me.fromPrim 1UL then
                        yield {
                            IdentNumber = tnodeidx_me.fromPrim 1u;
                            TargetName = "target001";
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.fromPrim 1us;
                            LUN = [ lun_me.zero; lun_me.fromPrim 1UL; ];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        };
                    if lun = lun_me.fromPrim 2UL then
                        yield {
                            IdentNumber = tnodeidx_me.fromPrim 2u;
                            TargetName = "target002";
                            TargetAlias = "";
                            TargetPortalGroupTag = tpgt_me.fromPrim 0us;
                            LUN = [ lun_me.fromPrim 2UL ];
                            Auth = TargetGroupConf.T_Auth.U_None();
                        };
                ]
            ),
            p_GetITNexusFromLUN = ( fun lun ->
                [|
                    new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
                    new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us );
                    new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 3uy 3us 3uy 3us, "target001", tpgt_me.fromPrim 1us );
                    new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 4uy 4us 4uy 4us, "target001", tpgt_me.fromPrim 1us );
                |]
            )
        )
        let luStub = new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero )
        let pm = new PRManager( statStub, luStub, lun_me.zero, fname, k )

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = expType ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = expCnt ))

        k, luStub, pm

    member _.WaitForFileDelete ( fname : string ) =
        let mutable cnt = 0
        while ( File.Exists fname ) && cnt < 200 do
            Thread.Sleep 5
            cnt <- cnt + 1

    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_001.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, true;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_002.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, true;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x08uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute

            0x00uy; 0x00uy; 0x00uy; 0x48uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID 1
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator333"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "830004050006"
            0x00uy; 0x00uy; 0x00uy;

            // TransportID 2
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator444"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "830004050006"
            0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_003.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, true;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0xEEuy; 0xFFuy; 0xEEuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let sourceITN = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let source = {
            defaultSource with
                I_TNexus = sourceITN
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 2 ))
        Assert.True(( prinfo.m_Registrations.Item( sourceITN ) = resvkey_me.fromPrim 0xEEFFEEFF11223344UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_003_2() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_003_2.txt"
        let initITN1 = new ITNexus( "initiator00001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                yield ( initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, true );
                for i = 2 to Constants.PRDATA_MAX_REGISTRATION_COUNT do
                    let initITN_n = new ITNexus( sprintf "initiator%05d" i, isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
                    yield ( initITN_n, resvkey_me.fromPrim ( uint64 i ), false );

            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY Constants.PRDATA_MAX_REGISTRATION_COUNT
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0xEEuy; 0xFFuy; 0xEEuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let sourceITN = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let source = {
            defaultSource with
                I_TNexus = sourceITN
        }

        try
            let _ = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INSUFFICIENT_REGISTRATION_RESOURCES ))

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_004.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, true;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0xEEuy; 0xFFuy; 0xEEuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x08uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute

            0x00uy; 0x00uy; 0x00uy; 0x48uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID 1
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator333"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "830004050006"
            0x00uy; 0x00uy; 0x00uy;

            // TransportID 2
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator444"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "830004050006"
            0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        }
        try
            let _ = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        k.NoticeTerminate()
        Assert.True( File.Exists fname )
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_005.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, false;  // Delete target
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x01uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 0 ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime

        Assert.True( File.Exists fname )
        let pm2 = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let prinfo2 = PRManager_Test2.GetPRInfoRec pm2
        Assert.True(( prinfo2.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo2.m_PRGeneration = 0u ))
        Assert.True(( prinfo2.m_Registrations.Count = 0 ))

        k.NoticeTerminate()
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_006() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_006.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        let initITN2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, false;  // Delete target
                initITN2, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 2
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_007() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_007.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        let initITN2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, true;   // Reservation holder & Delete target
                initITN2, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 2
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_008() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_008.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        let initITN2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, false;  // Delete target
                initITN2, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // Reservation holder
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 2
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_009() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_009.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, true;  // Reservation holder && Delete target
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 0 ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_010() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_010.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        let initITN2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target003", tpgt_me.fromPrim 1us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target004", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, true;   // Reservation holder & Delete target
                initITN2, resvkey_me.fromPrim 0xA1B2C3D4E5F60719UL, false;
                initITN3, resvkey_me.fromPrim 0xA1B2C3D4E5F6071AUL, false;
                initITN4, resvkey_me.fromPrim 0xA1B2C3D4E5F6071BUL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 4
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let mutable cnt = 0
        luStub.p_EstablishUnitAttention <- ( fun itr acaEX ->
            cnt <- cnt + 1
            match cnt with
            | 1 ->
                Assert.True(( itr = "initiator002,i,0x420003040005" ))
            | 2 ->
                Assert.True(( itr = "initiator003,i,0x420003040005" ))
            | _ ->
                Assert.Fail __LINE__
        )
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 3 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xA1B2C3D4E5F60719UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0xA1B2C3D4E5F6071AUL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0xA1B2C3D4E5F6071BUL ))
        Assert.True(( prinfo.m_Holder.IsNone ))
        Assert.True(( cnt = 2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_011() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_011.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        let initITN2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target003", tpgt_me.fromPrim 1us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target004", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, true;   // Reservation holder
                initITN2, resvkey_me.fromPrim 0xA1B2C3D4E5F60719UL, false;  // Delete target
                initITN3, resvkey_me.fromPrim 0xA1B2C3D4E5F6071AUL, false;
                initITN4, resvkey_me.fromPrim 0xA1B2C3D4E5F6071BUL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 4
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 3 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0xA1B2C3D4E5F6071AUL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0xA1B2C3D4E5F6071BUL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_012() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_012.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, true;   // Reservation holder & Delete target
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 1
        let param = [|
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // RESERVATION KEY 
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 0 ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_013() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_013.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1122334455667788UL, false;   // Delete target
                initITN2, resvkey_me.fromPrim 0x2233445566778899UL, true;    // Reservation holder
                initITN3, resvkey_me.fromPrim 0x33445566778899AAUL, false;
                initITN4, resvkey_me.fromPrim 0x445566778899AABBUL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 4
        let param = [|
            0x11uy; 0x22uy; 0x33uy; 0x44uy; // RESERVATION KEY 
            0x55uy; 0x66uy; 0x77uy; 0x88uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x04uy;                         // SPEC_I_PT(0), ALL_TG_PT(1), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let mutable cnt = 0
        luStub.p_EstablishUnitAttention <- ( fun itr acaEX ->
            cnt <- cnt + 1
            match cnt with
            | 1 ->
                // The initiator000 is the source of the command.
                // However, the I_TNexus of initITN2 was removed with collateral damage, so a UA is established.
                Assert.True(( itr = "initiator000,i,0x410001010001" ))
            | 2 ->
                Assert.True(( itr = "initiator001,i,0x420002020002" ))
            | _ ->
                Assert.Fail __LINE__
        )
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 2 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x33445566778899AAUL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x445566778899AABBUL ))
        Assert.True(( prinfo.m_Holder.IsNone ))
        Assert.True(( cnt = 2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_014() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_014.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // Reservation holder & Delete target
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 1
        let param = [|
            0x11uy; 0x22uy; 0x33uy; 0x44uy; // RESERVATION KEY 
            0x55uy; 0x66uy; 0x77uy; 0x88uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x08uy;                         // SPEC_I_PT(1), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute

            0x00uy; 0x00uy; 0x00uy; 0x48uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID 1
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy; 0x00uy;

            // TransportID 2
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator444"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "830004050006"
            0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        try
            let _ = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        k.NoticeTerminate()
        Assert.True( File.Exists fname )
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_015() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_015.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        let initITN2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // reservation key update target
                initITN2, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS 2
        let param = [|
            0xBBuy; 0xBBuy; 0xBBuy; 0xBBuy; // RESERVATION KEY 
            0xBBuy; 0xBBuy; 0xBBuy; 0xBBuy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY
            0xEEuy; 0xEEuy; 0xEEuy; 0xEEuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 2 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xFFFFFFFFEEEEEEEEUL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_016() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_016.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // reservation key update target
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS 1
        let param = [|
            0xBBuy; 0xBBuy; 0xBBuy; 0xBBuy; // RESERVATION KEY 
            0xBBuy; 0xBBuy; 0xBBuy; 0xBBuy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY
            0xEEuy; 0xEEuy; 0xEEuy; 0xEEuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xFFFFFFFFEEEEEEEEUL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_017() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_017.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // reservation key update target
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 1
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY
            0xEEuy; 0xEEuy; 0xEEuy; 0xEEuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x08uy;                         // SPEC_I_PT(1), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute

            0x00uy; 0x00uy; 0x00uy; 0x48uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID 1
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy; 0x00uy;

            // TransportID 2
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator444"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "830004050006"
            0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        try
            let _ = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        k.NoticeTerminate()
        Assert.True( File.Exists fname )
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_018() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_018.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;   // reservation key update target
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, true;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 4
        let param = [|
            0x55uy; 0x55uy; 0x55uy; 0x55uy; // RESERVATION KEY 
            0x55uy; 0x55uy; 0x55uy; 0x55uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY
            0xEEuy; 0xEEuy; 0xEEuy; 0xEEuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x04uy;                         // SPEC_I_PT(0), ALL_TG_PT(1), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xFFFFFFFFEEEEEEEEUL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xFFFFFFFFEEEEEEEEUL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndIgnoreExistingKey_019() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndIgnoreExistingKey_019.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL, true;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0xEEuy; 0xFFuy; 0xEEuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // dummy buffer
        |]
        let sourceITN = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let source = {
            defaultSource with
                I_TNexus = sourceITN
        }
        let v = pm.RegisterAndIgnoreExistingKey source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length - 4u ) ( PooledBuffer( param, param.Length - 4 ) )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 2 ))
        Assert.True(( prinfo.m_Registrations.Item( sourceITN ) = resvkey_me.fromPrim 0xEEFFEEFF11223344UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xA1B2C3D4E5F60718UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_001() =
        let k, luStub, pm = this.CreateDefaultPM "" NO_RESERVATION 0
        let param = [|
            0x55uy; 0x55uy; 0x55uy; 0x55uy; // RESERVATION KEY 
            0x55uy; 0x55uy; 0x55uy; 0x55uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY
            0xEEuy; 0xEEuy; 0xEEuy; 0xEEuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy; 0x00uy;
        |]
        try
            let _ = pm.RegisterAndMove defaultSource ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 0 ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        k.NoticeTerminate()

    [<Fact>]
    member this.RegisterAndMove_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_002.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.NO_RESERVATION
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname NO_RESERVATION 4
        let param = [|
            0x55uy; 0x55uy; 0x55uy; 0x55uy; // RESERVATION KEY 
            0x55uy; 0x55uy; 0x55uy; 0x55uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY
            0xEEuy; 0xEEuy; 0xEEuy; 0xEEuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy; 0x00uy;
        |]
        try
            let _ = pm.RegisterAndMove defaultSource ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_003.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, true;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0x55uy; 0x55uy; 0x55uy; 0x55uy; // RESERVATION KEY 
            0x55uy; 0x55uy; 0x55uy; 0x55uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY
            0xEEuy; 0xEEuy; 0xEEuy; 0xEEuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy; 0x00uy;
        |]
        let v = pm.RegisterAndMove defaultSource ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_004.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 4
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY
            0xEEuy; 0xEEuy; 0xEEuy; 0xEEuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Dummy buffer
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndMove source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length - 4u ) ( PooledBuffer( param, param.Length - 4 ) )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_005.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY
            0xEEuy; 0xEEuy; 0xEEuy; 0xEEuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let v = pm.RegisterAndMove source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_006() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_006.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY
            0xEEuy; 0xEEuy; 0xEEuy; 0xEEuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndMove source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_007() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_007.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420003040005"
            0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        try
            let _ = pm.RegisterAndMove source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        k.NoticeTerminate()
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_008() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_008.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator000"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "410001010001"
            0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        try
            let _ = pm.RegisterAndMove source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_009() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_009.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x18uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator000"
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        try
            let _ = pm.RegisterAndMove source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_010() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_010.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, true;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0x33uy; 0x33uy; 0x33uy; 0x33uy; // RESERVATION KEY 
            0x33uy; 0x33uy; 0x33uy; 0x33uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0x00uy; 0x00uy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x18uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator000"
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN3;
        }
        try
            let _ = pm.RegisterAndMove source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN3 ))

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_011() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_011.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0xFFuy; 0xFFuy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator000"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "440004040004"
            0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndMove source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        let ansITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 4uy 4us 4uy 4us, "target000", tpgt_me.fromPrim 0xFFFFus )
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 5 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN1 ) =  resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Holder.Value = ansITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_012() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_012.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0xAAus )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0x00uy; 0xAAuy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420002020002"
            0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndMove source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN3 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_013() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_013.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0xFFuy; 0xFFuy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x18uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndMove source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        let ansITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0xFFFFus )
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 5 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN1 ) =  resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Holder.Value = ansITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_014() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_014.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0xAAus )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy;                         // Reserved
            0x03uy;                         // UNREG(1), APTPL(1)
            0x00uy; 0xAAuy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x24uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x45uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x20uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            yield! Encoding.UTF8.GetBytes ",i,0x"
            yield! Encoding.UTF8.GetBytes "420002020002"
            0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.RegisterAndMove source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 3 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN3 ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime

        Assert.True( File.Exists fname )
        let pm2 = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let prinfo2 = PRManager_Test2.GetPRInfoRec pm2
        Assert.True(( prinfo2.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo2.m_PRGeneration = 0u ))
        Assert.True(( prinfo2.m_Registrations.Count = 3 ))
        Assert.True(( prinfo2.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo2.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo2.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo2.m_Holder.Value = initITN3 ))

        k.NoticeTerminate()
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.RegisterAndMove_015() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "RegisterAndMove_015.txt"
        let initITN1 = new ITNexus( "initiator00001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                yield ( initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true );
                for i = 2 to Constants.PRDATA_MAX_REGISTRATION_COUNT do
                    let initITN_n = new ITNexus( sprintf "initiator%05d" i, isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
                    yield ( initITN_n, resvkey_me.fromPrim ( uint64 i ), false );
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY Constants.PRDATA_MAX_REGISTRATION_COUNT
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY(0)
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy;                         // Reserved
            0x00uy;                         // UNREG(0), APTPL(0)
            0xFFuy; 0xFFuy;                 // RELATIVE TARGET PORT IDENTIFIER
            0x00uy; 0x00uy; 0x00uy; 0x18uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID
            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator001"
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }

        try
            let _ = pm.RegisterAndMove source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INSUFFICIENT_REGISTRATION_RESOURCES ))

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Reserve_001() =
        let k, luStub, pm = this.CreateDefaultPM "" NO_RESERVATION 0
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let v = pm.Reserve defaultSource ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 0 ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        k.NoticeTerminate()

    [<Fact>]
    member this.Reserve_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Reserve_002.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 4
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let v = pm.Reserve defaultSource ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Reserve_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Reserve_003.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 4
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.Reserve source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Reserve_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Reserve_004.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.NO_RESERVATION
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let k, luStub, pm = this.CreateDefaultPM fname NO_RESERVATION 4
        let param = [|
            0x44uy; 0x44uy; 0x44uy; 0x44uy; // RESERVATION KEY 
            0x44uy; 0x44uy; 0x44uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x01uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN4;
        }
        let v = pm.Reserve source ( itt_me.fromPrim 0u ) WRITE_EXCLUSIVE ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN4 ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime

        Assert.True( File.Exists fname )
        let pm2 = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let prinfo2 = PRManager_Test2.GetPRInfoRec pm2
        Assert.True(( prinfo2.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN4 ))

        k.NoticeTerminate()
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Reserve_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Reserve_005.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.NO_RESERVATION
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname NO_RESERVATION 4
        let param = [|
            0x44uy; 0x44uy; 0x44uy; 0x44uy; // RESERVATION KEY 
            0x44uy; 0x44uy; 0x44uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN4;
        }
        let v = pm.Reserve source ( itt_me.fromPrim 0u ) PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Reserve_006() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Reserve_006.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 4
        let param = [|
            0x44uy; 0x44uy; 0x44uy; 0x44uy; // RESERVATION KEY 
            0x44uy; 0x44uy; 0x44uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN4;
        }
        let v = pm.Reserve source ( itt_me.fromPrim 0u ) PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Reserve_007() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Reserve_007.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_ALL_REGISTRANTS 4
        let param = [|
            0x44uy; 0x44uy; 0x44uy; 0x44uy; // RESERVATION KEY 
            0x44uy; 0x44uy; 0x44uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN4;
        }
        let v = pm.Reserve source ( itt_me.fromPrim 0u ) PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Reserve_008() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Reserve_008.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, true;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS 4
        let param = [|
            0x33uy; 0x33uy; 0x33uy; 0x33uy; // RESERVATION KEY 
            0x33uy; 0x33uy; 0x33uy; 0x33uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN3;
        }
        let v = pm.Reserve source ( itt_me.fromPrim 0u ) PR_TYPE.EXCLUSIVE_ACCESS ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Reserve_009() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Reserve_009.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, true;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let v = pm.Reserve source ( itt_me.fromPrim 0u ) PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Reserve_010() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Reserve_010.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, true;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // dummy buffer
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let v = pm.Reserve source ( itt_me.fromPrim 0u ) PR_TYPE.WRITE_EXCLUSIVE ( uint32 param.Length - 4u ) ( PooledBuffer( param, param.Length - 4 ) )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Release_001() =
        let k, luStub, pm = this.CreateDefaultPM "" NO_RESERVATION 0
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let v = pm.Release defaultSource ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))
        k.NoticeTerminate()

    [<Fact>]
    member this.Release_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Release_002.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, true;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let v = pm.Release defaultSource ( itt_me.fromPrim 0u ) PR_TYPE.WRITE_EXCLUSIVE ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Release_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Release_003.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, true;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 4
        let param = [|
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // RESERVATION KEY 
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let v = pm.Release source ( itt_me.fromPrim 0u ) PR_TYPE.WRITE_EXCLUSIVE ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Release_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Release_004.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.NO_RESERVATION
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname NO_RESERVATION 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let v = pm.Release source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Release_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Release_005.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, true;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS 4
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.Release source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName


    [<Fact>]
    member this.Release_006() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Release_006.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, true;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        try
            let _ = pm.Release source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_RELEASE_OF_PERSISTENT_RESERVATION ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Release_007() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Release_007.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, true;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 4
        let param = [|
            0x33uy; 0x33uy; 0x33uy; 0x33uy; // RESERVATION KEY 
            0x33uy; 0x33uy; 0x33uy; 0x33uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN3;
        }
        let v = pm.Release source ( itt_me.fromPrim 0u ) PR_TYPE.WRITE_EXCLUSIVE ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Release_008() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Release_008.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, true;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0x44uy; 0x44uy; 0x44uy; 0x44uy; // RESERVATION KEY 
            0x44uy; 0x44uy; 0x44uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN4;
        }
        let mutable cnt = 0
        luStub.p_EstablishUnitAttention <- ( fun itr acaEX ->
            cnt <- cnt + 1
            match cnt with
            | 1 -> Assert.True(( itr = "initiator000,i,0x410001010001" ))
            | 2 -> Assert.True(( itr = "initiator001,i,0x420002020002" ))
            | _ -> Assert.Fail __LINE__
        )
        let v = pm.Release source ( itt_me.fromPrim 0u ) PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))
        Assert.True(( cnt = 2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Release_009() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Release_009.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_ALL_REGISTRANTS 4
        let param = [|
            0x44uy; 0x44uy; 0x44uy; 0x44uy; // RESERVATION KEY 
            0x44uy; 0x44uy; 0x44uy; 0x44uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN4;
        }
        try
            let _ = pm.Release source ( itt_me.fromPrim 0u ) PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ( uint32 param.Length ) ( PooledBuffer.Rent param )
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_RELEASE_OF_PERSISTENT_RESERVATION ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Release_010() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Release_010.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_ALL_REGISTRANTS 1
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x01uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // dummy buffer
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.Release source ( itt_me.fromPrim 0u ) PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ( uint32 param.Length - 4u ) ( PooledBuffer( param, param.Length - 4 ) )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime

        Assert.True( File.Exists fname )
        let pm2 = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let prinfo2 = PRManager_Test2.GetPRInfoRec pm2
        Assert.True(( prinfo2.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo2.m_PRGeneration = 0u ))
        Assert.True(( prinfo2.m_Registrations.Count = 1 ))
        Assert.True(( prinfo2.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo2.m_Holder.IsNone ))

        k.NoticeTerminate()
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName
