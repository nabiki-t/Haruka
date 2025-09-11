//=============================================================================
// Haruka Software Storage.
// PRManagerTest2.fs : Test cases for PRManager class.
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

type PRManager_Test2 () =

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
        let w1 = Functions.AppendPathName ( Path.GetTempPath() ) "PRManager_Test2"
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
    member this.Register_001() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_001.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY  
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.ContainsKey initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_002.txt"

        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY  
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x01uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // dummy buffer
        |]
        let source = {
            defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        }
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length - 4u ) ( PooledBuffer( param, param.Length - 4 ) )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 2 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Registrations.Item( source.I_TNexus ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime

        Assert.True( File.Exists fname )
        let pm2 = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let prinfo2 = PRManager_Test2.GetPRInfoRec pm2
        Assert.True(( prinfo2.m_PRGeneration = 0u ))
        Assert.True(( prinfo2.m_Registrations.Count = 2 ))
        Assert.True(( prinfo2.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo2.m_Registrations.Item( source.I_TNexus ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))

        k.NoticeTerminate()
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_003.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY  
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x08uy;                         // SPEC_I_PT(1), ALL_TG_PT(0), APTPL(0)
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))

        let ansITN1 = new ITNexus( "initiator333", isid_me.fromElem ( 2uy <<< 6 ) 3uy 4us 5uy 6us, "target001", tpgt_me.fromPrim 1us );
        let ansITN2 = new ITNexus( "initiator444", isid_me.fromElem ( 2uy <<< 6 ) 3uy 4us 5uy 6us, "target001", tpgt_me.fromPrim 1us );
        Assert.True(( prinfo.m_Registrations.Item( source.I_TNexus ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN1 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN2 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_004.txt"

        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY  
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x0Cuy;                         // SPEC_I_PT(1), ALL_TG_PT(1), APTPL(0)
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 7 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))

        let ansITN1 = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
        let ansITN2 = new ITNexus( "initiator333", isid_me.fromElem ( 2uy <<< 6 ) 3uy 4us 5uy 6us, "target000", tpgt_me.fromPrim 0us );
        let ansITN3 = new ITNexus( "initiator444", isid_me.fromElem ( 2uy <<< 6 ) 3uy 4us 5uy 6us, "target000", tpgt_me.fromPrim 0us );
        let ansITN4 = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let ansITN5 = new ITNexus( "initiator333", isid_me.fromElem ( 2uy <<< 6 ) 3uy 4us 5uy 6us, "target001", tpgt_me.fromPrim 1us );
        let ansITN6 = new ITNexus( "initiator444", isid_me.fromElem ( 2uy <<< 6 ) 3uy 4us 5uy 6us, "target001", tpgt_me.fromPrim 1us );
        Assert.True(( prinfo.m_Registrations.Item( ansITN1 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN2 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN3 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN4 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN5 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN6 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_005.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY  
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x0Cuy;                         // SPEC_I_PT(1), ALL_TG_PT(1), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute

            0x00uy; 0x00uy; 0x00uy; 0x30uy; // TRANSPORTID PARAMETER DATA LENGTH

            // TransportID 1
            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator000"
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy;

            // TransportID 2
            0x05uy;                         // FORMAT CODE, PROTOCOL IDENTIFIER
            0x00uy;                         // Reserved
            0x00uy; 0x14uy;                 // ADDITIONAL LENGTH
            yield! Encoding.UTF8.GetBytes "initiator002"
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy;

        |]
        let source = {
            defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        }
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 9 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))

        let ansITN1 = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
        let ansITN2 = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let ansITN3 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
        let ansITN4 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 3uy 3us 3uy 3us, "target000", tpgt_me.fromPrim 0us );
        let ansITN5 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        let ansITN6 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 3uy 3us 3uy 3us, "target001", tpgt_me.fromPrim 1us );
        let ansITN7 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 4uy 4us 4uy 4us, "target000", tpgt_me.fromPrim 0us );
        let ansITN8 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 4uy 4us 4uy 4us, "target001", tpgt_me.fromPrim 1us );
        Assert.True(( prinfo.m_Registrations.Item( ansITN1 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN2 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN3 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN4 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN5 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN6 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN7 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN8 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_006() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_006.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY  
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x04uy;                         // SPEC_I_PT(0), ALL_TG_PT(1), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        }
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 3 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))

        let ansITN1 = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us );
        let ansITN4 = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        Assert.True(( prinfo.m_Registrations.Item( ansITN1 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))
        Assert.True(( prinfo.m_Registrations.Item( ansITN4 ) = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_007() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_007.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY  
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x04uy;                         // SPEC_I_PT(0), ALL_TG_PT(1), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target001", tpgt_me.fromPrim 1us );
        }
        try
            pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))

        k.NoticeTerminate()
        Assert.True( File.Exists fname )

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_008() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_008.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY  
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x0Cuy;                         // SPEC_I_PT(1), ALL_TG_PT(1), APTPL(0)
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
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        }

        try
            pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param ) |> ignore
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_CDB ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))

        k.NoticeTerminate()
        Assert.True( File.Exists fname )

        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_009() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_009.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 1
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY  
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x04uy;                         // SPEC_I_PT(0), ALL_TG_PT(1), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = new ITNexus( "initiator111", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us );
        }
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_010() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_010.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 1
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY  
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x04uy;                         // SPEC_I_PT(0), ALL_TG_PT(1), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()

        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_011() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_011.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;  // Delete target
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 1
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
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
    member this.Register_012() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_012.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        let initITN2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;  // Delete target
                initITN2, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 2
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime

        Assert.True( File.Exists fname )
        let pm2 = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let prinfo2 = PRManager_Test2.GetPRInfoRec pm2
        Assert.True(( prinfo2.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS ))
        Assert.True(( prinfo2.m_PRGeneration = 0u ))
        Assert.True(( prinfo2.m_Registrations.Count = 1 ))
        Assert.True(( prinfo2.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))

        k.NoticeTerminate()
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_013() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_013.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        let initITN2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // Reservation holder & Delete target
                initITN2, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 2
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime

        Assert.True( File.Exists fname )
        let pm2 = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let prinfo2 = PRManager_Test2.GetPRInfoRec pm2
        Assert.True(( prinfo2.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo2.m_PRGeneration = 0u ))
        Assert.True(( prinfo2.m_Registrations.Count = 1 ))
        Assert.True(( prinfo2.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        k.NoticeTerminate()
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_014() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_014.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        let initITN2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;  // Delete target
                initITN2, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // Reservation holder
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 2
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime

        Assert.True( File.Exists fname )
        let pm2 = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let prinfo2 = PRManager_Test2.GetPRInfoRec pm2
        Assert.True(( prinfo2.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( prinfo2.m_PRGeneration = 0u ))
        Assert.True(( prinfo2.m_Registrations.Count = 1 ))
        Assert.True(( prinfo2.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        k.NoticeTerminate()
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_015() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_015.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;  // Reservation holder & Delete target
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 1
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
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
    member this.Register_016() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_016.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        let initITN2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target003", tpgt_me.fromPrim 1us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target004", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // Reservation holder & Delete target
                initITN2, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
                initITN3, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
                initITN4, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 3 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))
        Assert.True(( cnt = 2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_017() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_017.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        let initITN2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target003", tpgt_me.fromPrim 1us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target004", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // Reservation holder
                initITN2, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;  // Delete target
                initITN3, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
                initITN4, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 3 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_018() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_018.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // Reservation holder & Delete target
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 1
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
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
    member this.Register_019() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_019.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // Reservation holder & Delete target
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 1
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
            let _ = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
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
    member this.Register_020() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_020.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;   // Delete target
                initITN2, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;    // Reservation holder
                initITN3, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
                initITN4, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 2 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))
        Assert.True(( cnt = 2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()

    [<Fact>]
    member this.Register_021() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_021.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        let initITN2 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target002", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // reservation key update target
                initITN2, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 2
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
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
    member this.Register_022() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_022.txt"
        let initITN1 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 3us 4uy 5us, "target000", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;   // reservation key update target
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 1
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xFFFFFFFFEEEEEEEEUL ))
        Assert.True(( prinfo.m_Holder.Value = initITN1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Register_023() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_023.txt"
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
            let _ = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
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
    member this.Register_024() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Register_024.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 1us )
        let initITN3 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target000", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 2uy 2us 2uy 2us, "target001", tpgt_me.fromPrim 1us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;   // reservation key update target
                initITN2, resvkey_me.fromPrim 0xAABBCCDD11223344UL, true;
                initITN3, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
                initITN4, resvkey_me.fromPrim 0xAABBCCDD11223344UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_REGISTRANTS_ONLY 4
        let param = [|
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; // RESERVATION KEY 
            0x11uy; 0x22uy; 0x33uy; 0x44uy;
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
        let v = pm.Register source ( itt_me.fromPrim 0u ) NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0xFFFFFFFFEEEEEEEEUL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0xFFFFFFFFEEEEEEEEUL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0xAABBCCDD11223344UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName


    [<Fact>]
    member this.Clear_001() =
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
        let v = pm.Clear defaultSource ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 0 ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        k.NoticeTerminate()

    [<Fact>]
    member this.Clear_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Clear_002.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.NO_RESERVATION
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
            |]
            fname
        let initPRFileTime = File.GetLastWriteTimeUtc fname
        let k, luStub, pm = this.CreateDefaultPM fname NO_RESERVATION 1
        let param = [|
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x01uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(1)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // Dummy buffer
        |]
        let source = {
            defaultSource with
                I_TNexus = initITN1;
        }
        let v = pm.Clear source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 param.Length - 4u ) ( PooledBuffer( param, param.Length - 4 ) )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 1  ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        GlbFunc.WaitForFileUpdate fname initPRFileTime

        Assert.True( File.Exists fname )
        let pm2 = new PRManager( new CStatus_Stub(), new CInternalLU_Stub( p_LUN = fun () -> lun_me.zero ), lun_me.zero, fname, k )
        let prinfo2 = PRManager_Test2.GetPRInfoRec pm2
        Assert.True(( prinfo2.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo2.m_PRGeneration = 0u ))
        Assert.True(( prinfo2.m_Registrations.Count = 1  ))
        Assert.True(( prinfo2.m_Holder.IsNone ))

        k.NoticeTerminate()
        GlbFunc.DeleteFile fname
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Clear_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Clear_003.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, true;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 3
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
        let v = pm.Clear source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.RESERVATION_CONFLICT ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 3 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Clear_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Clear_004.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, true;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE 3
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
        let mutable cnt = 0
        luStub.p_EstablishUnitAttention <- ( fun itr acaEX ->
            cnt <- cnt + 1
            match cnt with
            | 1 -> Assert.True(( itr = "initiator001,i,0x410001010001" ))
            | 2 -> Assert.True(( itr = "initiator002,i,0x410001010001" ))
            | _ -> Assert.Fail __LINE__
        )
        let v = pm.Clear source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 0 ))
        Assert.True(( prinfo.m_Holder.IsNone ))
        Assert.True(( cnt = 2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Clear_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Clear_005.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS_ALL_REGISTRANTS 3
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
        let mutable cnt = 0
        luStub.p_EstablishUnitAttention <- ( fun itr acaEX ->
            cnt <- cnt + 1
            match cnt with
            | 1 -> Assert.True(( itr = "initiator000,i,0x410001010001" ))
            | 2 -> Assert.True(( itr = "initiator002,i,0x410001010001" ))
            | _ -> Assert.Fail __LINE__
        )
        let v = pm.Clear source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 param.Length ) ( PooledBuffer.Rent param )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 0 ))
        Assert.True(( prinfo.m_Holder.IsNone ))
        Assert.True(( cnt = 2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.Clear_006() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "Clear_006.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
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
            0xAAuy; 0xAAuy; 0xAAuy; 0xAAuy; // Dummy buffer
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
            | 2 -> Assert.True(( itr = "initiator001,i,0x410001010001" ))
            | 3 -> Assert.True(( itr = "initiator002,i,0x410001010001" ))
            | _ -> Assert.Fail __LINE__
        )
        let v = pm.Clear source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 param.Length - 4u ) ( PooledBuffer( param, param.Length - 4 ) )
        Assert.True(( v = ScsiCmdStatCd.GOOD ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 0 ))
        Assert.True(( prinfo.m_Holder.IsNone ))
        Assert.True(( cnt = 3 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PreemptAndAbort_001() =
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
        let parambuf = PooledBuffer.Rent param

        let struct( stat, rITN, prType, saResvKey ) = pm.PreemptAndAbort defaultSource ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 parambuf.Count ) parambuf
        Assert.True(( stat = ScsiCmdStatCd.RESERVATION_CONFLICT ))
        Assert.True(( rITN.Length = 0 ))
        Assert.True(( prType = NO_RESERVATION ))
        Assert.True(( saResvKey = resvkey_me.fromPrim 0UL ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 0 ))
        Assert.True(( prinfo.m_Holder.IsNone ))

        k.NoticeTerminate()

    [<Fact>]
    member this.PreemptAndAbort_002() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "PreemptAndAbort_002.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
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
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // RESERVATION KEY 
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let parambuf = PooledBuffer.Rent param

        let struct( stat, rITN, prType, saResvKey ) = pm.PreemptAndAbort defaultSource ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 parambuf.Count ) parambuf
        Assert.True(( stat = ScsiCmdStatCd.RESERVATION_CONFLICT ))
        Assert.True(( rITN.Length = 0 ))
        Assert.True(( prType = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( saResvKey = resvkey_me.fromPrim 0UL ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN4 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PreemptAndAbort_003() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "PreemptAndAbort_003.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
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
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // RESERVATION KEY 
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let parambuf = PooledBuffer.Rent param
        let source = {
            defaultSource with
                I_TNexus = initITN4;
        }

        let struct( stat, rITN, prType, saResvKey ) = pm.PreemptAndAbort source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 parambuf.Count ) parambuf
        Assert.True(( stat = ScsiCmdStatCd.RESERVATION_CONFLICT ))
        Assert.True(( rITN.Length = 0 ))
        Assert.True(( prType = PR_TYPE.EXCLUSIVE_ACCESS_REGISTRANTS_ONLY ))
        Assert.True(( saResvKey = resvkey_me.fromPrim 0UL ))

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
    member this.PreemptAndAbort_004() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "PreemptAndAbort_004.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
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
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY(0)    // Delete target missing
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let parambuf = PooledBuffer.Rent param
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }

        let struct( stat, rITN, prType, saResvKey ) = pm.PreemptAndAbort source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 parambuf.Count ) parambuf
        Assert.True(( stat = ScsiCmdStatCd.RESERVATION_CONFLICT ))
        Assert.True(( rITN.Length = 0 ))
        Assert.True(( prType = PR_TYPE.NO_RESERVATION ))
        Assert.True(( saResvKey = resvkey_me.fromPrim 0UL ))

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
    member this.PreemptAndAbort_005() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "PreemptAndAbort_005.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.NO_RESERVATION
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;  // Delete target
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname NO_RESERVATION 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // SERVICE ACTION RESERVATION KEY
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
            0xCCuy; 0xCCuy; 0xCCuy; 0xCCuy; // Dummy buffer
        |]
        let parambuf = PooledBuffer( param, param.Length - 4 )
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let mutable cnt = 0
        luStub.p_EstablishUnitAttention <- ( fun itr acaEX ->
            cnt <- cnt + 1
            match cnt with
            | 1 -> Assert.True(( itr = "initiator000,i,0x410001010001" ))
            | _ -> Assert.Fail __LINE__
        )

        let struct( stat, rITN, prType, saResvKey ) = pm.PreemptAndAbort source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 parambuf.Count ) parambuf
        Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        Assert.True(( rITN.Length = 1 ))
        Assert.True(( rITN.[0] = initITN1 ))
        Assert.True(( prType = PR_TYPE.NO_RESERVATION ))
        Assert.True(( saResvKey = resvkey_me.fromPrim 0x1111111111111111UL ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 3 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))
        Assert.True(( cnt = 1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PreemptAndAbort_006() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "PreemptAndAbort_006.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;  // Delete target
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;  // Delete target
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;  // Delete target
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_ALL_REGISTRANTS 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let parambuf = PooledBuffer.Rent param
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let mutable cnt = 0
        luStub.p_EstablishUnitAttention <- ( fun itr acaEX ->
            cnt <- cnt + 1
            match cnt with
            | 1 -> Assert.True(( itr = "initiator000,i,0x410001010001" ))
            | 2 -> Assert.True(( itr = "initiator002,i,0x410001010001" ))
            | 3 -> Assert.True(( itr = "initiator003,i,0x410001010001" ))
            | _ -> Assert.Fail __LINE__
        )

        let struct( stat, rITN, prType, saResvKey ) = pm.PreemptAndAbort source ( itt_me.fromPrim 0u ) PR_TYPE.EXCLUSIVE_ACCESS ( uint32 parambuf.Count ) parambuf
        Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        Assert.True(( rITN.Length = 3 ))
        Assert.True(( rITN.[0] = initITN1 ))
        Assert.True(( rITN.[1] = initITN3 ))
        Assert.True(( rITN.[2] = initITN4 ))
        Assert.True(( prType = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ))
        Assert.True(( saResvKey = resvkey_me.fromPrim 0x0000000000000000UL ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))
        Assert.True(( cnt = 3 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PreemptAndAbort_007() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "PreemptAndAbort_007.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;  // Delete target
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_ALL_REGISTRANTS 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x33uy; 0x33uy; 0x33uy; 0x33uy; // SERVICE ACTION RESERVATION KEY
            0x33uy; 0x33uy; 0x33uy; 0x33uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let parambuf = PooledBuffer.Rent param
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let mutable cnt = 0
        luStub.p_EstablishUnitAttention <- ( fun itr acaEX ->
            cnt <- cnt + 1
            match cnt with
            | 1 -> Assert.True(( itr = "initiator002,i,0x410001010001" ))
            | _ -> Assert.Fail __LINE__
        )

        let struct( stat, rITN, prType, saResvKey ) = pm.PreemptAndAbort source ( itt_me.fromPrim 0u ) PR_TYPE.EXCLUSIVE_ACCESS ( uint32 parambuf.Count ) parambuf
        Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        Assert.True(( rITN.Length = 1 ))
        Assert.True(( rITN.[0] = initITN3 ))
        Assert.True(( prType = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ))
        Assert.True(( saResvKey = resvkey_me.fromPrim 0x3333333333333333UL ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 3 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))
        Assert.True(( cnt = 1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PreemptAndAbort_008() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "PreemptAndAbort_008.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // SERVICE ACTION RESERVATION KEY
            0x00uy; 0x00uy; 0x00uy; 0x00uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let parambuf = PooledBuffer.Rent param
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }

        try
            let _ = pm.PreemptAndAbort source ( itt_me.fromPrim 0u ) PR_TYPE.EXCLUSIVE_ACCESS ( uint32 parambuf.Count ) parambuf
            Assert.Fail __LINE__
        with
        | :? SCSIACAException as x ->
            Assert.True(( x.Status = ScsiCmdStatCd.CHECK_CONDITION ))
            Assert.True(( x.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
            Assert.True(( x.ASC = ASCCd.INVALID_FIELD_IN_PARAMETER_LIST ))

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PreemptAndAbort_009() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "PreemptAndAbort_009.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;   // delete target
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, false;
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // SERVICE ACTION RESERVATION KEY
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let parambuf = PooledBuffer.Rent param
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let mutable cnt = 0
        luStub.p_EstablishUnitAttention <- ( fun itr acaEX ->
            cnt <- cnt + 1
            match cnt with
            | 1 -> Assert.True(( itr = "initiator000,i,0x410001010001" ))
            | _ -> Assert.Fail __LINE__
        )

        let struct( stat, rITN, prType, saResvKey ) = pm.PreemptAndAbort source ( itt_me.fromPrim 0u ) PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ( uint32 parambuf.Count ) parambuf
        Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        Assert.True(( rITN.Length = 1 ))
        Assert.True(( rITN.[0] = initITN1 ))
        Assert.True(( prType = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( saResvKey = resvkey_me.fromPrim 0x1111111111111111UL ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_ALL_REGISTRANTS ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 3 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))
        Assert.True(( cnt = 1 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PreemptAndAbort_010() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "PreemptAndAbort_010.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;   // delete target
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x1111111111111111UL, false;   // delete target
                initITN4, resvkey_me.fromPrim 0x4444444444444444UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // SERVICE ACTION RESERVATION KEY
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let parambuf = PooledBuffer.Rent param
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let mutable cnt = 0
        luStub.p_EstablishUnitAttention <- ( fun itr acaEX ->
            cnt <- cnt + 1
            match cnt with
            | 1 -> Assert.True(( itr = "initiator000,i,0x410001010001" ))
            | 2 -> Assert.True(( itr = "initiator002,i,0x410001010001" ))
            | _ -> Assert.Fail __LINE__
        )

        let struct( stat, rITN, prType, saResvKey ) = pm.PreemptAndAbort source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 parambuf.Count ) parambuf
        Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        Assert.True(( rITN.Length = 2 ))
        Assert.True(( rITN.[0] = initITN1 ))
        Assert.True(( rITN.[1] = initITN3 ))
        Assert.True(( prType = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( saResvKey = resvkey_me.fromPrim 0x1111111111111111UL ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.NO_RESERVATION ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 2 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x4444444444444444UL ))
        Assert.True(( prinfo.m_Holder.IsNone ))
        Assert.True(( cnt = 2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PreemptAndAbort_011() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "PreemptAndAbort_011.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, true;   // delete target
                initITN2, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN3, resvkey_me.fromPrim 0x1111111111111111UL, false;   // delete target
                initITN4, resvkey_me.fromPrim 0x1111111111111111UL, false;   // delete target
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS 4
        let param = [|
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // RESERVATION KEY 
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // SERVICE ACTION RESERVATION KEY
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let parambuf = PooledBuffer.Rent param
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let mutable cnt = 0
        luStub.p_EstablishUnitAttention <- ( fun itr acaEX ->
            cnt <- cnt + 1
            match cnt with
            | 1 -> Assert.True(( itr = "initiator000,i,0x410001010001" ))
            | 2 -> Assert.True(( itr = "initiator002,i,0x410001010001" ))
            | 3 -> Assert.True(( itr = "initiator003,i,0x410001010001" ))
            | _ -> Assert.Fail __LINE__
        )

        let struct( stat, rITN, prType, saResvKey ) = pm.PreemptAndAbort source ( itt_me.fromPrim 0u ) PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ( uint32 parambuf.Count ) parambuf
        Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        Assert.True(( rITN.Length = 3 ))
        Assert.True(( rITN.[0] = initITN1 ))
        Assert.True(( rITN.[1] = initITN3 ))
        Assert.True(( rITN.[2] = initITN4 ))
        Assert.True(( prType = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( saResvKey = resvkey_me.fromPrim 0x1111111111111111UL ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 1 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN2 ))
        Assert.True(( cnt = 3 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PreemptAndAbort_012() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "PreemptAndAbort_012.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.EXCLUSIVE_ACCESS
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;   // delete target
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, true;
                initITN4, resvkey_me.fromPrim 0x1111111111111111UL, false;   // delete target
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname EXCLUSIVE_ACCESS 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0x11uy; 0x11uy; 0x11uy; 0x11uy; // SERVICE ACTION RESERVATION KEY
            0x11uy; 0x11uy; 0x11uy; 0x11uy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let parambuf = PooledBuffer.Rent param
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }
        let mutable cnt = 0
        luStub.p_EstablishUnitAttention <- ( fun itr acaEX ->
            cnt <- cnt + 1
            match cnt with
            | 1 -> Assert.True(( itr = "initiator000,i,0x410001010001" ))
            | 2 -> Assert.True(( itr = "initiator003,i,0x410001010001" ))
            | _ -> Assert.Fail __LINE__
        )

        let struct( stat, rITN, prType, saResvKey ) = pm.PreemptAndAbort source ( itt_me.fromPrim 0u ) PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ( uint32 parambuf.Count ) parambuf
        Assert.True(( stat = ScsiCmdStatCd.GOOD ))
        Assert.True(( rITN.Length = 2 ))
        Assert.True(( rITN.[0] = initITN1 ))
        Assert.True(( rITN.[1] = initITN4 ))
        Assert.True(( prType = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( saResvKey = resvkey_me.fromPrim 0x1111111111111111UL ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.EXCLUSIVE_ACCESS ))
        Assert.True(( prinfo.m_PRGeneration = 1u ))
        Assert.True(( prinfo.m_Registrations.Count = 2 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN3 ))
        Assert.True(( cnt = 2 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName

    [<Fact>]
    member this.PreemptAndAbort_013() =
        let pDirName = this.CreateTestDir()
        let fname = Functions.AppendPathName pDirName "PreemptAndAbort_013.txt"
        let initITN1 = new ITNexus( "initiator000", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target000", tpgt_me.fromPrim 0us )
        let initITN2 = new ITNexus( "initiator001", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target001", tpgt_me.fromPrim 0us )
        let initITN3 = new ITNexus( "initiator002", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target002", tpgt_me.fromPrim 0us )
        let initITN4 = new ITNexus( "initiator003", isid_me.fromElem ( 1uy <<< 6 ) 1uy 1us 1uy 1us, "target003", tpgt_me.fromPrim 0us )
        GlbFunc.writeDefaultPRFile
            PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY
            [|
                initITN1, resvkey_me.fromPrim 0x1111111111111111UL, false;
                initITN2, resvkey_me.fromPrim 0x2222222222222222UL, false;
                initITN3, resvkey_me.fromPrim 0x3333333333333333UL, true;
                initITN4, resvkey_me.fromPrim 0x1111111111111111UL, false;
            |]
            fname
        let k, luStub, pm = this.CreateDefaultPM fname WRITE_EXCLUSIVE_REGISTRANTS_ONLY 4
        let param = [|
            0x22uy; 0x22uy; 0x22uy; 0x22uy; // RESERVATION KEY 
            0x22uy; 0x22uy; 0x22uy; 0x22uy;
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; // SERVICE ACTION RESERVATION KEY
            0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy;
            0x00uy; 0x00uy; 0x00uy; 0x00uy; // Obsolute
            0x00uy;                         // SPEC_I_PT(0), ALL_TG_PT(0), APTPL(0)
            0x00uy;                         // Reserved
            0x00uy; 0x00uy;                 // Obsolute
        |]
        let parambuf = PooledBuffer.Rent param
        let source = {
            defaultSource with
                I_TNexus = initITN2;
        }

        let struct( stat, rITN, prType, saResvKey ) = pm.PreemptAndAbort source ( itt_me.fromPrim 0u ) PR_TYPE.NO_RESERVATION ( uint32 parambuf.Count ) parambuf
        Assert.True(( stat = ScsiCmdStatCd.RESERVATION_CONFLICT ))
        Assert.True(( rITN.Length = 0 ))
        Assert.True(( prType = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( saResvKey = resvkey_me.fromPrim 0xFFFFFFFFFFFFFFFFUL ))

        let prinfo = PRManager_Test2.GetPRInfoRec pm
        Assert.True(( prinfo.m_Type = PR_TYPE.WRITE_EXCLUSIVE_REGISTRANTS_ONLY ))
        Assert.True(( prinfo.m_PRGeneration = 0u ))
        Assert.True(( prinfo.m_Registrations.Count = 4 ))
        Assert.True(( prinfo.m_Registrations.Item( initITN1 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN2 ) = resvkey_me.fromPrim 0x2222222222222222UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN3 ) = resvkey_me.fromPrim 0x3333333333333333UL ))
        Assert.True(( prinfo.m_Registrations.Item( initITN4 ) = resvkey_me.fromPrim 0x1111111111111111UL ))
        Assert.True(( prinfo.m_Holder.Value = initITN3 ))

        this.WaitForFileDelete fname
        Assert.False( File.Exists fname )
        k.NoticeTerminate()

        GlbFunc.DeleteFile fname
        k.NoticeTerminate()
        GlbFunc.DeleteDir pDirName
