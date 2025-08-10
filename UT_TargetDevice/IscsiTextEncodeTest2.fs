namespace Haruka.Test.UT.TargetDevice

open System.Text

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.TargetDevice
open Haruka.Test


type IscsiTextEncode2_Test () =

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_AuthMethod_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSV_Negotiated;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_Missing )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSV_Negotiated )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_AuthMethod_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_Missing )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_AuthMethod_003() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_NotUnderstood )
        Assert.True( s.NegoStat_AuthMethod = ( NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_AuthMethod_004() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_NotUnderstood )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_AuthMethod_005() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_NotUnderstood )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_AuthMethod_006() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_NotUnderstood )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_AuthMethod_007() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_NotUnderstood )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_AuthMethod_008() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Reject;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_Reject )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_AuthMethod_009() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Reject;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_Reject )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_AuthMethod_010() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Irrelevant;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_Irrelevant )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_AuthMethod_011() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Irrelevant;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_Irrelevant )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_AuthMethod_012() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_SPKM1; AuthMethodCandidateValue.AMC_CHAP;  |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_SPKM2; AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_CHAP_A_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_A = TextValueType.Value( [| 1us; 2us; 4us; 5us; 6us; |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_A = TextValueType.Value( [| 2us; 5us; 7us; 8us; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_A = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_A = TextValueType.Value( [| 2us; |] ) )
        Assert.True( s.NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_CHAP_I_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_I = TextValueType.Value( 128us );
                }
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_I = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_I = TextValueType.Value( 128us ) )
        Assert.True( s.NegoStat_CHAP_I = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_CHAP_I_0102() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_I = TextValueType.Value( 128us );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_I = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_I = TextValueType.Value( 128us ) )
        Assert.True( s.NegoStat_CHAP_I = ( NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_CHAP_C_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_C = TextValueType.Value( [| 0uy .. 255uy |] );
                }
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_C = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_C = TextValueType.Value( [| 0uy .. 255uy |] ) )
        Assert.True( s.NegoStat_CHAP_C = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_CHAP_C_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_C = TextValueType.Value( [| 0uy .. 255uy |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_C = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_C = TextValueType.Value( [| 0uy .. 255uy |] ) )
        Assert.True( s.NegoStat_CHAP_C = ( NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_CHAP_N_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_N = TextValueType.Value( "AAABBBCCC" );
                }
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_N = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_N = TextValueType.Value( "AAABBBCCC" ) )
        Assert.True( s.NegoStat_CHAP_N = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_CHAP_N_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_N = TextValueType.Value( "XXXYYYZZ" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_N = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_N = TextValueType.Value( "XXXYYYZZ" ) )
        Assert.True( s.NegoStat_CHAP_N = ( NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_CHAP_R_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_R = TextValueType.Value( [| 0uy .. 255uy |] );
                }
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_R = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_R = TextValueType.Value( [| 0uy .. 255uy |] ) )
        Assert.True( s.NegoStat_CHAP_R = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_CHAP_R_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_R = TextValueType.Value( [| 0uy .. 255uy |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_R = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_R = TextValueType.Value( [| 0uy .. 255uy |] ) )
        Assert.True( s.NegoStat_CHAP_R = ( NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_HeaderDigest_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        HeaderDigest = TextValueType.Value( [| DigestType.DST_None; DigestType.DST_CRC32C;  |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_NotUnderstood; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_HeaderDigest = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; |] ) )
        Assert.True( s.NegoStat_HeaderDigest = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_DataDigest_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataDigest = TextValueType.Value( [| DigestType.DST_None; DigestType.DST_CRC32C;  |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataDigest = TextValueType.Value( [| DigestType.DST_None; DigestType.DST_NotUnderstood; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_DataDigest = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.DataDigest = TextValueType.Value( [| DigestType.DST_None; |] ) )
        Assert.True( s.NegoStat_DataDigest = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_MaxConnections_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxConnections = TextValueType.Value( 10us );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxConnections = TextValueType.Value( 9us );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_MaxConnections = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.MaxConnections = TextValueType.Value( 9us ) )
        Assert.True( s.NegoStat_MaxConnections = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_SendTargets_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        SendTargets = TextValueType.Value( "aaaabbbbcccc" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        SendTargets = TextValueType.Value( "XXXXYYYYZZZZ" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_SendTargets = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.SendTargets = TextValueType.Value( "aaaabbbbcccc" ) )
        Assert.True( s.NegoStat_SendTargets = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_TargetName_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetName = TextValueType.Value( "aaaabbbbcccc111" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetName = TextValueType.Value( "XXXXYYYYZZZZ222" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_TargetName = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.TargetName = TextValueType.Value( "aaaabbbbcccc111" ) )
        Assert.True( s.NegoStat_TargetName = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_InitiatorName_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorName = TextValueType.Value( "aaaabbbbcccc333" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorName = TextValueType.Value( "XXXXYYYYZZZZ444" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_InitiatorName = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.InitiatorName = TextValueType.Value( "aaaabbbbcccc333" ) )
        Assert.True( s.NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_TargetAlias_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAlias = TextValueType.Value( "aaaa111bbbbcccc" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAlias = TextValueType.Value( "XXXX222YYYYZZZZ" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_TargetAlias = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.TargetAlias = TextValueType.Value( "XXXX222YYYYZZZZ" ) )
        Assert.True( s.NegoStat_TargetAlias = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_InitiatorAlias_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorAlias = TextValueType.Value( "aaaa333bbbbcccc" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorAlias = TextValueType.Value( "XXXX444YYYYZZZZ" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.InitiatorAlias = TextValueType.Value( "aaaa333bbbbcccc" ) )
        Assert.True( s.NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_TargetAddress_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAddress = TextValueType.Value( "aaaa555bbbbcccc" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAddress = TextValueType.Value( "XXXX666YYYYZZZZ" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_TargetAddress = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.TargetAddress = TextValueType.Value( "XXXX666YYYYZZZZ" ) )
        Assert.True( s.NegoStat_TargetAddress = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_TargetPortalGroupTag_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetPortalGroupTag = TextValueType.Value( 12345us );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetPortalGroupTag = TextValueType.Value( 23456us );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_TargetPortalGroupTag = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.TargetPortalGroupTag = TextValueType.Value( 23456us ) )
        Assert.True( s.NegoStat_TargetPortalGroupTag = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_InitialR2T_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( true );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( false );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_InitialR2T = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.InitialR2T = TextValueType.Value( true ) )
        Assert.True( s.NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_InitialR2T_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( false );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( false );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_InitialR2T = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.InitialR2T = TextValueType.Value( false ) )
        Assert.True( s.NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_InitialR2T_003() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( true );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( true );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_InitialR2T = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.InitialR2T = TextValueType.Value( true ) )
        Assert.True( s.NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_InitialR2T_004() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( false );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( true );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_InitialR2T = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.InitialR2T = TextValueType.Value( true ) )
        Assert.True( s.NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_ImmediateData_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( false );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( false );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_ImmediateData = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.ImmediateData = TextValueType.Value( false ) )
        Assert.True( s.NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_ImmediateData_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( true );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( false );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_ImmediateData = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.ImmediateData = TextValueType.Value( false ) )
        Assert.True( s.NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_ImmediateData_003() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( false );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( true );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_ImmediateData = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.ImmediateData = TextValueType.Value( false ) )
        Assert.True( s.NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_ImmediateData_004() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( true );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( true );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_ImmediateData = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.ImmediateData = TextValueType.Value( true ) )
        Assert.True( s.NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_MaxRecvDataSegmentLength_I_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.Value( 12345u );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.Value( 23456u );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.MaxRecvDataSegmentLength_I = TextValueType.Value( 12345u ) )
        Assert.True( s.NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_MaxRecvDataSegmentLength_T_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_T = TextValueType.Value( 12345u );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_T = TextValueType.Value( 23456u );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.MaxRecvDataSegmentLength_T = TextValueType.Value( 23456u ) )
        Assert.True( s.NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_MaxBurstLength_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxBurstLength = TextValueType.Value( 12345u );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxBurstLength = TextValueType.Value( 23456u );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_MaxBurstLength = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.MaxBurstLength = TextValueType.Value( 12345u ) )
        Assert.True( s.NegoStat_MaxBurstLength = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_FirstBurstLength_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        FirstBurstLength = TextValueType.Value( 12345u );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        FirstBurstLength = TextValueType.Value( 23456u );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_FirstBurstLength = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.FirstBurstLength = TextValueType.Value( 12345u ) )
        Assert.True( s.NegoStat_FirstBurstLength = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_DefaultTime2Wait_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Wait = TextValueType.Value( 10us );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Wait = TextValueType.Value( 20us );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_DefaultTime2Wait = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.DefaultTime2Wait = TextValueType.Value( 10us ) )
        Assert.True( s.NegoStat_DefaultTime2Wait = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_DefaultTime2Retain_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Retain = TextValueType.Value( 30us );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Retain = TextValueType.Value( 20us );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_DefaultTime2Retain = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.DefaultTime2Retain = TextValueType.Value( 20us ) )
        Assert.True( s.NegoStat_DefaultTime2Retain = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_MaxOutstandingR2T_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxOutstandingR2T = TextValueType.Value( 100us );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxOutstandingR2T = TextValueType.Value( 200us );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_MaxOutstandingR2T = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.MaxOutstandingR2T = TextValueType.Value( 100us ) )
        Assert.True( s.NegoStat_MaxOutstandingR2T = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_DataPDUInOrder_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataPDUInOrder = TextValueType.Value( true );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataPDUInOrder = TextValueType.Value( false );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_DataPDUInOrder = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.DataPDUInOrder = TextValueType.Value( true ) )
        Assert.True( s.NegoStat_DataPDUInOrder = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_DataSequenceInOrder_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataSequenceInOrder = TextValueType.Value( false );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataSequenceInOrder = TextValueType.Value( true );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_DataSequenceInOrder = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.DataSequenceInOrder = TextValueType.Value( true ) )
        Assert.True( s.NegoStat_DataSequenceInOrder = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_ErrorRecoveryLevel_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        ErrorRecoveryLevel = TextValueType.Value( 0uy );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        ErrorRecoveryLevel = TextValueType.Value( 1uy );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_ErrorRecoveryLevel = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.ErrorRecoveryLevel = TextValueType.Value( 0uy ) )
        Assert.True( s.NegoStat_ErrorRecoveryLevel = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_SessionType_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        SessionType = TextValueType.Value( "abcdefg" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        SessionType = TextValueType.Value( "OPQRSTU" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_SessionType = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.SessionType = TextValueType.Value( "abcdefg"  ) )
        Assert.True( s.NegoStat_SessionType = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_UnknownKeys_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "A"; "B"; "C"; |];
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "X"; "Y"; "Z"; |];
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.UnknownKeys = [| "X"; "Y"; "Z"; |] )
        Assert.True( s.NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_UnknownKeys_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "X"; "Y"; "Z"; |];
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.UnknownKeys = [| "X"; "Y"; "Z"; |] )
        Assert.True( s.NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_UnknownKeys_003() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "A"; "B"; "C"; |];
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [||];
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.UnknownKeys = [||] )
        Assert.True( s.NegoStat_UnknownKeys = NegoStatusValue.NSV_Negotiated )

    [<Fact>]
    member _.margeTextKeyValue_TargetSide_UnknownKeys_004() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Target
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "A"; "B"; "C"; |];
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "" |];
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.UnknownKeys = [||] )
        Assert.True( s.NegoStat_UnknownKeys = NegoStatusValue.NSV_Negotiated )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_AuthMethod_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSV_Negotiated;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_Missing )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSV_Negotiated )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_AuthMethod_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_Missing )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_AuthMethod_003() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_NotUnderstood )
        Assert.True( s.NegoStat_AuthMethod = ( NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_AuthMethod_004() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Missing;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_NotUnderstood )
        Assert.True( s.NegoStat_AuthMethod = ( NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_AuthMethod_005() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_NotUnderstood )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_AuthMethod_006() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_NotUnderstood )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_AuthMethod_007() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_NotUnderstood;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_NotUnderstood )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_AuthMethod_008() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Reject;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_Reject )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_AuthMethod_009() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Reject;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_Reject )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_AuthMethod_010() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Irrelevant;
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_Irrelevant )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_AuthMethod_011() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.ISV_Irrelevant;
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.ISV_Irrelevant )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_AuthMethod_012() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_SPKM1; AuthMethodCandidateValue.AMC_CHAP;  |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_SPKM2; AuthMethodCandidateValue.AMC_CHAP; AuthMethodCandidateValue.AMC_None; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_AuthMethod = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.AuthMethod = TextValueType.Value( [| AuthMethodCandidateValue.AMC_CHAP; |] ) )
        Assert.True( s.NegoStat_AuthMethod = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_CHAP_A_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_A = TextValueType.Value( [| 1us; 2us; 4us; 5us; 6us; |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_A = TextValueType.Value( [| 2us; 5us; 7us; 8us; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_A = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_A = TextValueType.Value( [| 2us; |] ) )
        Assert.True( s.NegoStat_CHAP_A = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_CHAP_I_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_I = TextValueType.Value( 128us );
                }
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_I = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_I = TextValueType.Value( 128us ) )
        Assert.True( s.NegoStat_CHAP_I = ( NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_CHAP_I_0102() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_I = TextValueType.Value( 128us );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_I = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_I = TextValueType.Value( 128us ) )
        Assert.True( s.NegoStat_CHAP_I = ( NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_CHAP_C_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_C = TextValueType.Value( [| 0uy .. 255uy |] );
                }
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_C = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_C = TextValueType.Value( [| 0uy .. 255uy |] ) )
        Assert.True( s.NegoStat_CHAP_C = ( NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_CHAP_C_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_C = TextValueType.Value( [| 0uy .. 255uy |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_C = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_C = TextValueType.Value( [| 0uy .. 255uy |] ) )
        Assert.True( s.NegoStat_CHAP_C = ( NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_CHAP_N_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_N = TextValueType.Value( "AAABBBCCC" );
                }
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_N = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_N = TextValueType.Value( "AAABBBCCC" ) )
        Assert.True( s.NegoStat_CHAP_N = ( NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_CHAP_N_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_N = TextValueType.Value( "XXXYYYZZ" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_N = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_N = TextValueType.Value( "XXXYYYZZ" ) )
        Assert.True( s.NegoStat_CHAP_N = ( NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_CHAP_R_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_R = TextValueType.Value( [| 0uy .. 255uy |] );
                }
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_R = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_R = TextValueType.Value( [| 0uy .. 255uy |] ) )
        Assert.True( s.NegoStat_CHAP_R = ( NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_CHAP_R_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValues.defaultTextKeyValues with
                        CHAP_R = TextValueType.Value( [| 0uy .. 255uy |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_CHAP_R = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.CHAP_R = TextValueType.Value( [| 0uy .. 255uy |] ) )
        Assert.True( s.NegoStat_CHAP_R = ( NegoStatusValue.NSG_WaitSend ) )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_HeaderDigest_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        HeaderDigest = TextValueType.Value( [| DigestType.DST_None; DigestType.DST_CRC32C;  |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; DigestType.DST_NotUnderstood; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_HeaderDigest = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.HeaderDigest = TextValueType.Value( [| DigestType.DST_CRC32C; |] ) )
        Assert.True( s.NegoStat_HeaderDigest = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_DataDigest_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataDigest = TextValueType.Value( [| DigestType.DST_None; DigestType.DST_CRC32C;  |] );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataDigest = TextValueType.Value( [| DigestType.DST_None; DigestType.DST_NotUnderstood; |] );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_DataDigest = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.DataDigest = TextValueType.Value( [| DigestType.DST_None; |] ) )
        Assert.True( s.NegoStat_DataDigest = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_MaxConnections_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxConnections = TextValueType.Value( 10us );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxConnections = TextValueType.Value( 9us );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_MaxConnections = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.MaxConnections = TextValueType.Value( 9us ) )
        Assert.True( s.NegoStat_MaxConnections = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_SendTargets_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        SendTargets = TextValueType.Value( "aaaabbbbcccc" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        SendTargets = TextValueType.Value( "XXXXYYYYZZZZ" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_SendTargets = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.SendTargets = TextValueType.Value( "aaaabbbbcccc" ) )
        Assert.True( s.NegoStat_SendTargets = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_TargetName_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetName = TextValueType.Value( "aaaabbbbcccc111" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetName = TextValueType.Value( "XXXXYYYYZZZZ222" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_TargetName = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.TargetName = TextValueType.Value( "aaaabbbbcccc111" ) )
        Assert.True( s.NegoStat_TargetName = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_InitiatorName_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorName = TextValueType.Value( "aaaabbbbcccc333" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorName = TextValueType.Value( "XXXXYYYYZZZZ444" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_InitiatorName = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.InitiatorName = TextValueType.Value( "aaaabbbbcccc333" ) )
        Assert.True( s.NegoStat_InitiatorName = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_TargetAlias_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAlias = TextValueType.Value( "aaaa111bbbbcccc" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAlias = TextValueType.Value( "XXXX222YYYYZZZZ" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_TargetAlias = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.TargetAlias = TextValueType.Value( "XXXX222YYYYZZZZ" ) )
        Assert.True( s.NegoStat_TargetAlias = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_InitiatorAlias_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorAlias = TextValueType.Value( "aaaa333bbbbcccc" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitiatorAlias = TextValueType.Value( "XXXX444YYYYZZZZ" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.InitiatorAlias = TextValueType.Value( "aaaa333bbbbcccc" ) )
        Assert.True( s.NegoStat_InitiatorAlias = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_TargetAddress_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAddress = TextValueType.Value( "aaaa555bbbbcccc" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetAddress = TextValueType.Value( "XXXX666YYYYZZZZ" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_TargetAddress = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.TargetAddress = TextValueType.Value( "XXXX666YYYYZZZZ" ) )
        Assert.True( s.NegoStat_TargetAddress = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_TargetPortalGroupTag_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetPortalGroupTag = TextValueType.Value( 12345us );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        TargetPortalGroupTag = TextValueType.Value( 23456us );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_TargetPortalGroupTag = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.TargetPortalGroupTag = TextValueType.Value( 23456us ) )
        Assert.True( s.NegoStat_TargetPortalGroupTag = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_InitialR2T_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( true );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( false );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_InitialR2T = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.InitialR2T = TextValueType.Value( true ) )
        Assert.True( s.NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_InitialR2T_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( false );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( false );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_InitialR2T = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.InitialR2T = TextValueType.Value( false ) )
        Assert.True( s.NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_InitialR2T_003() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( true );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( true );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_InitialR2T = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.InitialR2T = TextValueType.Value( true ) )
        Assert.True( s.NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_InitialR2T_004() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( false );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        InitialR2T = TextValueType.Value( true );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_InitialR2T = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.InitialR2T = TextValueType.Value( true ) )
        Assert.True( s.NegoStat_InitialR2T = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_ImmediateData_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( false );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( false );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_ImmediateData = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.ImmediateData = TextValueType.Value( false ) )
        Assert.True( s.NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_ImmediateData_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( true );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( false );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_ImmediateData = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.ImmediateData = TextValueType.Value( false ) )
        Assert.True( s.NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_ImmediateData_003() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( false );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( true );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_ImmediateData = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.ImmediateData = TextValueType.Value( false ) )
        Assert.True( s.NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_ImmediateData_004() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( true );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        ImmediateData = TextValueType.Value( true );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_ImmediateData = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.ImmediateData = TextValueType.Value( true ) )
        Assert.True( s.NegoStat_ImmediateData = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_MaxRecvDataSegmentLength_I_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.Value( 12345u );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_I = TextValueType.Value( 23456u );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.MaxRecvDataSegmentLength_I = TextValueType.Value( 12345u ) )
        Assert.True( s.NegoStat_MaxRecvDataSegmentLength_I = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_MaxRecvDataSegmentLength_T_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_T = TextValueType.Value( 12345u );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxRecvDataSegmentLength_T = TextValueType.Value( 23456u );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.MaxRecvDataSegmentLength_T = TextValueType.Value( 23456u ) )
        Assert.True( s.NegoStat_MaxRecvDataSegmentLength_T = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_MaxBurstLength_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxBurstLength = TextValueType.Value( 12345u );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxBurstLength = TextValueType.Value( 23456u );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_MaxBurstLength = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.MaxBurstLength = TextValueType.Value( 12345u ) )
        Assert.True( s.NegoStat_MaxBurstLength = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_FirstBurstLength_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        FirstBurstLength = TextValueType.Value( 12345u );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        FirstBurstLength = TextValueType.Value( 23456u );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_FirstBurstLength = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.FirstBurstLength = TextValueType.Value( 12345u ) )
        Assert.True( s.NegoStat_FirstBurstLength = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_DefaultTime2Wait_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Wait = TextValueType.Value( 10us );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Wait = TextValueType.Value( 20us );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_DefaultTime2Wait = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.DefaultTime2Wait = TextValueType.Value( 10us ) )
        Assert.True( s.NegoStat_DefaultTime2Wait = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_DefaultTime2Retain_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Retain = TextValueType.Value( 30us );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        DefaultTime2Retain = TextValueType.Value( 20us );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_DefaultTime2Retain = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.DefaultTime2Retain = TextValueType.Value( 20us ) )
        Assert.True( s.NegoStat_DefaultTime2Retain = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_MaxOutstandingR2T_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxOutstandingR2T = TextValueType.Value( 100us );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        MaxOutstandingR2T = TextValueType.Value( 200us );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_MaxOutstandingR2T = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.MaxOutstandingR2T = TextValueType.Value( 100us ) )
        Assert.True( s.NegoStat_MaxOutstandingR2T = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_DataPDUInOrder_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataPDUInOrder = TextValueType.Value( true );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataPDUInOrder = TextValueType.Value( false );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_DataPDUInOrder = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.DataPDUInOrder = TextValueType.Value( true ) )
        Assert.True( s.NegoStat_DataPDUInOrder = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_DataSequenceInOrder_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataSequenceInOrder = TextValueType.Value( false );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        DataSequenceInOrder = TextValueType.Value( true );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_DataSequenceInOrder = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.DataSequenceInOrder = TextValueType.Value( true ) )
        Assert.True( s.NegoStat_DataSequenceInOrder = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_ErrorRecoveryLevel_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        ErrorRecoveryLevel = TextValueType.Value( 0uy );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        ErrorRecoveryLevel = TextValueType.Value( 1uy );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_ErrorRecoveryLevel = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.ErrorRecoveryLevel = TextValueType.Value( 0uy ) )
        Assert.True( s.NegoStat_ErrorRecoveryLevel = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_SessionType_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        SessionType = TextValueType.Value( "abcdefg" );
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        SessionType = TextValueType.Value( "OPQRSTU" );
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_SessionType = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.SessionType = TextValueType.Value( "abcdefg"  ) )
        Assert.True( s.NegoStat_SessionType = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_UnknownKeys_001() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "A"; "B"; "C"; |];
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "X"; "Y"; "Z"; |];
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.UnknownKeys = [| "A"; "B"; "C"; |] )
        Assert.True( s.NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_UnknownKeys_002() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "X"; "Y"; "Z"; |];
                }
                TextKeyValues.defaultTextKeyValues
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.UnknownKeys = [| "X"; "Y"; "Z"; |] )
        Assert.True( s.NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitSend )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_UnknownKeys_003() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [||];
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "A"; "B"; "C"; |];
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.UnknownKeys = [||] )
        Assert.True( s.NegoStat_UnknownKeys = NegoStatusValue.NSV_Negotiated )

    [<Fact>]
    member _.margeTextKeyValue_InitiatorSide_UnknownKeys_004() =
        let r, s =
            IscsiTextEncode.margeTextKeyValue
                Standpoint.Initiator
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "" |];
                }
                {
                    TextKeyValues.defaultTextKeyValues with
                        UnknownKeys = [| "A"; "B"; "C"; |];
                }
                {
                    TextKeyValuesStatus.defaultTextKeyValuesStatus with
                        NegoStat_UnknownKeys = NegoStatusValue.NSG_WaitReceive ||| NegoStatusValue.NSG_WaitSend;
                }
        Assert.True( r.UnknownKeys = [||] )
        Assert.True( s.NegoStat_UnknownKeys = NegoStatusValue.NSV_Negotiated )

