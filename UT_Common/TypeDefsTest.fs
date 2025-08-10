namespace Haruka.Test.UT.Commons

open System
open System.Net

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test


type TypeDefs_Test () =
   
    [<Fact>]
    member _.lun_me_fromPrim_001() =
        Assert.True(( lun_me.zero = 0UL<lun_me> ))
        Assert.True(( lun_me.fromPrim 1UL = 1UL<lun_me> ))
        Assert.True(( lun_me.fromPrim 0xFFFFFFFFFFFFFFFFUL = 18446744073709551615UL<lun_me> ))

    [<Fact>]
    member _.lun_me_toPrim_001() =
        Assert.True(( lun_me.toPrim( 0UL<lun_me> ) = 0x0000000000000000UL ))
        Assert.True(( lun_me.toPrim( 1UL<lun_me> ) = 0x0000000000000001UL ))
        Assert.True(( lun_me.toPrim( 18446744073709551615UL<lun_me> ) = 0xFFFFFFFFFFFFFFFFUL ))

    [<Fact>]
    member _.lun_me_toString_001() =
        Assert.True( ( lun_me.toString ( lun_me.fromPrim 0x1122334455667788UL ) ) = "1234605616436508552" )
        Assert.True( ( lun_me.toString ( lun_me.fromPrim 0xAABBCCDDEEFFAABBUL ) ) = "12302652060662213307" )

    [<Fact>]
    member _.lun_me_fromStringValue_001() =
        Assert.True( ( lun_me.fromStringValue "1234605616436508552" ) = lun_me.fromPrim 1234605616436508552UL )
        Assert.True( ( lun_me.fromStringValue "12302652060662213307" ) = lun_me.fromPrim 12302652060662213307UL )
        Assert.True( ( lun_me.fromStringValue "0xABCDEF0123456789" ) = lun_me.fromPrim 0xABCDEF0123456789UL )
        Assert.True( ( lun_me.fromStringValue "0XFFFF" ) = lun_me.fromPrim 0xFFFFUL )

    [<Fact>]
    member _.itt_me_fromPrim_001() =
        Assert.True(( itt_me.fromPrim 0u = 0u<itt_me> ))
        Assert.True(( itt_me.fromPrim 1u = 1u<itt_me> ))
        Assert.True(( itt_me.fromPrim 0xFFFFFFFFu = 4294967295u<itt_me> ))

    [<Fact>]
    member _.itt_me_toPrim_001() =
        Assert.True(( itt_me.toPrim( 0u<itt_me> ) = 0x00000000u ))
        Assert.True(( itt_me.toPrim( 1u<itt_me> ) = 0x00000001u ))
        Assert.True(( itt_me.toPrim( 4294967295u<itt_me> ) = 0xFFFFFFFFu ))

    [<Fact>]
    member _.ttt_me_fromPrim_001() =
        Assert.True(( ttt_me.fromPrim 0u = 0u<ttt_me> ))
        Assert.True(( ttt_me.fromPrim 1u = 1u<ttt_me> ))
        Assert.True(( ttt_me.fromPrim 0xFFFFFFFFu = 4294967295u<ttt_me> ))

    [<Fact>]
    member _.ttt_me_toPrim_001() =
        Assert.True(( ttt_me.toPrim( 0u<ttt_me> ) = 0x00000000u ))
        Assert.True(( ttt_me.toPrim( 1u<ttt_me> ) = 0x00000001u ))
        Assert.True(( ttt_me.toPrim( 4294967295u<ttt_me> ) = 0xFFFFFFFFu ))

    [<Fact>]
    member _.tsih_me_fromPrim_001() =
        Assert.True(( tsih_me.fromPrim 0us = 0us<tsih_me> ))
        Assert.True(( tsih_me.fromPrim 1us = 1us<tsih_me> ))
        Assert.True(( tsih_me.fromPrim 0xFFFFus = 65535us<tsih_me> ))

    [<Fact>]
    member _.tsih_me_toPrim_001() =
        Assert.True(( tsih_me.toPrim( 0us<tsih_me> ) = 0x00000000us ))
        Assert.True(( tsih_me.toPrim( 1us<tsih_me> ) = 0x00000001us ))
        Assert.True(( tsih_me.toPrim( 65535us<tsih_me> ) = 0xFFFFus ))

    [<Fact>]
    member _.tsih_me_zero_001() =
        Assert.True(( tsih_me.zero = 0us<tsih_me> ))

    [<Fact>]
    member _.tsih_me_fromOpt_001() =
        Assert.True(( ( tsih_me.fromOpt 1us None ) = tsih_me.fromPrim 1us ))
        Assert.True(( ( tsih_me.fromOpt 1us ( Some ( tsih_me.fromPrim 2us ) ) ) = tsih_me.fromPrim 2us ))

    [<Fact>]
    member _.cid_me_fromPrim_001() =
        Assert.True(( cid_me.fromPrim 0us = 0us<cid_me> ))
        Assert.True(( cid_me.fromPrim 1us = 1us<cid_me> ))
        Assert.True(( cid_me.fromPrim 0xFFFFus = 65535us<cid_me> ))

    [<Fact>]
    member _.cid_me_toPrim_001() =
        Assert.True(( cid_me.toPrim( 0us<cid_me> ) = 0x00000000us ))
        Assert.True(( cid_me.toPrim( 1us<cid_me> ) = 0x00000001us ))
        Assert.True(( cid_me.toPrim( 65535us<cid_me> ) = 0xFFFFus ))

    [<Fact>]
    member _.cid_me_zero_001() =
        Assert.True(( cid_me.zero = 0us<cid_me> ))

    [<Fact>]
    member _.cid_me_fromOpt_001() =
        Assert.True(( ( cid_me.fromOpt 1us None ) = cid_me.fromPrim 1us ))
        Assert.True(( ( cid_me.fromOpt 1us ( Some ( cid_me.fromPrim 2us ) ) ) = cid_me.fromPrim 2us ))

    [<Fact>]
    member _.concnt_me_fromPrim_001() =
        Assert.True(( concnt_me.fromPrim 0 = 0<concnt_me> ))
        Assert.True(( concnt_me.fromPrim 1 = 1<concnt_me> ))
        Assert.True(( concnt_me.fromPrim 0xFFFFFFFF = -1<concnt_me> ))

    [<Fact>]
    member _.concnt_me_toPrim_001() =
        Assert.True(( concnt_me.toPrim( 0<concnt_me> ) = 0x00000000 ))
        Assert.True(( concnt_me.toPrim( 1<concnt_me> ) = 0x00000001 ))
        Assert.True(( concnt_me.toPrim( -1<concnt_me> ) = 0xFFFFFFFF ))

    [<Fact>]
    member _.concnt_me_zero_001() =
        Assert.True(( concnt_me.zero = 0<concnt_me> ))

    [<Fact>]
    member _.concnt_me_fromOpt_001() =
        Assert.True(( ( concnt_me.fromOpt 1 None ) = concnt_me.fromPrim 1 ))
        Assert.True(( ( concnt_me.fromOpt 1 ( Some ( concnt_me.fromPrim 2 ) ) ) = concnt_me.fromPrim 2 ))

    [<Fact>]
    member _.cmdsn_me_fromPrim_001() =
        Assert.True(( cmdsn_me.fromPrim 0u = 0u<cmdsn_me> ))
        Assert.True(( cmdsn_me.fromPrim 1u = 1u<cmdsn_me> ))
        Assert.True(( cmdsn_me.fromPrim 0xFFFFFFFFu = 4294967295u<cmdsn_me> ))

    [<Fact>]
    member _.cmdsn_me_toPrim_001() =
        Assert.True(( cmdsn_me.toPrim( 0u<cmdsn_me> ) = 0x00000000u ))
        Assert.True(( cmdsn_me.toPrim( 1u<cmdsn_me> ) = 0x00000001u ))
        Assert.True(( cmdsn_me.toPrim( 4294967295u<cmdsn_me> ) = 0xFFFFFFFFu ))

    [<Fact>]
    member _.cmdsn_me_zero_001() =
        Assert.True(( cmdsn_me.zero = 0u<cmdsn_me> ))

    [<Fact>]
    member _.cmdsn_me_fromOpt_001() =
        Assert.True(( ( cmdsn_me.fromOpt 1u None ) = cmdsn_me.fromPrim 1u ))
        Assert.True(( ( cmdsn_me.fromOpt 1u ( Some ( cmdsn_me.fromPrim 2u ) ) ) = cmdsn_me.fromPrim 2u ))

    [<Fact>]
    member _.cmdsn_me_lessThan_001() =
        Assert.True(( cmdsn_me.lessThan ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u ) ))
        Assert.False(( cmdsn_me.lessThan ( cmdsn_me.fromPrim 2u ) ( cmdsn_me.fromPrim 1u ) ))
        Assert.False(( cmdsn_me.lessThan ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ))
        Assert.True(( cmdsn_me.lessThan ( cmdsn_me.fromPrim 0xFFFFFFFFu ) ( cmdsn_me.fromPrim 1u ) ))
        Assert.True(( cmdsn_me.lessThan ( cmdsn_me.fromPrim 0xFFFFFFFFu ) ( cmdsn_me.fromPrim 0x7FFFFFFEu ) ))
        Assert.False(( cmdsn_me.lessThan ( cmdsn_me.fromPrim 0xFFFFFFFFu ) ( cmdsn_me.fromPrim 0x7FFFFFFFu ) ))
        Assert.False(( cmdsn_me.lessThan ( cmdsn_me.fromPrim 0xFFFFFFFFu ) ( cmdsn_me.fromPrim 0x80000000u ) ))
        Assert.True(( cmdsn_me.lessThan ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 0x80000000u ) ))
        Assert.False(( cmdsn_me.lessThan ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 0x80000001u ) ))
        Assert.False(( cmdsn_me.lessThan ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 0x80000002u ) ))

    [<Fact>]
    member _.cmdsn_me_compare_001() =
        Assert.True(( ( cmdsn_me.compare ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 2u ) ) < 0 ))
        Assert.True(( ( cmdsn_me.compare ( cmdsn_me.fromPrim 2u ) ( cmdsn_me.fromPrim 1u ) ) > 0 ))
        Assert.True(( ( cmdsn_me.compare ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 1u ) ) = 0 ))
        Assert.True(( ( cmdsn_me.compare ( cmdsn_me.fromPrim 0xFFFFFFFFu ) ( cmdsn_me.fromPrim 1u ) ) < 0 ))
        Assert.True(( ( cmdsn_me.compare ( cmdsn_me.fromPrim 0xFFFFFFFFu ) ( cmdsn_me.fromPrim 0x7FFFFFFEu ) ) < 0 ))
        Assert.True(( ( cmdsn_me.compare ( cmdsn_me.fromPrim 0xFFFFFFFFu ) ( cmdsn_me.fromPrim 0x7FFFFFFFu ) ) > 0 ))   // undefined
        Assert.True(( ( cmdsn_me.compare ( cmdsn_me.fromPrim 0xFFFFFFFFu ) ( cmdsn_me.fromPrim 0x80000000u ) ) > 0 ))
        Assert.True(( ( cmdsn_me.compare ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 0x80000000u ) ) < 0 ))
        Assert.True(( ( cmdsn_me.compare ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 0x80000001u ) ) < 0 ))    // undefined
        Assert.True(( ( cmdsn_me.compare ( cmdsn_me.fromPrim 1u ) ( cmdsn_me.fromPrim 0x80000002u ) ) > 0 ))

    [<Fact>]
    member _.statsn_me_fromPrim_001() =
        Assert.True(( statsn_me.fromPrim 0u = 0u<statsn_me> ))
        Assert.True(( statsn_me.fromPrim 1u = 1u<statsn_me> ))
        Assert.True(( statsn_me.fromPrim 0xFFFFFFFFu = 4294967295u<statsn_me> ))

    [<Fact>]
    member _.statsn_me_toPrim_001() =
        Assert.True(( statsn_me.toPrim( 0u<statsn_me> ) = 0x00000000u ))
        Assert.True(( statsn_me.toPrim( 1u<statsn_me> ) = 0x00000001u ))
        Assert.True(( statsn_me.toPrim( 4294967295u<statsn_me> ) = 0xFFFFFFFFu ))

    [<Fact>]
    member _.statsn_me_zero_001() =
        Assert.True(( statsn_me.zero = 0u<statsn_me> ))

    [<Fact>]
    member _.statsn_me_fromOpt_001() =
        Assert.True(( ( statsn_me.fromOpt 1u None ) = statsn_me.fromPrim 1u ))
        Assert.True(( ( statsn_me.fromOpt 1u ( Some ( statsn_me.fromPrim 2u ) ) ) = statsn_me.fromPrim 2u ))

    [<Fact>]
    member _.statsn_me_lessThan_001() =
        Assert.True(( statsn_me.lessThan ( statsn_me.fromPrim 1u ) ( statsn_me.fromPrim 2u ) ))
        Assert.False(( statsn_me.lessThan ( statsn_me.fromPrim 2u ) ( statsn_me.fromPrim 1u ) ))
        Assert.False(( statsn_me.lessThan ( statsn_me.fromPrim 1u ) ( statsn_me.fromPrim 1u ) ))
        Assert.True(( statsn_me.lessThan ( statsn_me.fromPrim 0xFFFFFFFFu ) ( statsn_me.fromPrim 1u ) ))
        Assert.True(( statsn_me.lessThan ( statsn_me.fromPrim 0xFFFFFFFFu ) ( statsn_me.fromPrim 0x7FFFFFFEu ) ))
        Assert.False(( statsn_me.lessThan ( statsn_me.fromPrim 0xFFFFFFFFu ) ( statsn_me.fromPrim 0x7FFFFFFFu ) ))
        Assert.False(( statsn_me.lessThan ( statsn_me.fromPrim 0xFFFFFFFFu ) ( statsn_me.fromPrim 0x80000000u ) ))
        Assert.True(( statsn_me.lessThan ( statsn_me.fromPrim 1u ) ( statsn_me.fromPrim 0x80000000u ) ))
        Assert.False(( statsn_me.lessThan ( statsn_me.fromPrim 1u ) ( statsn_me.fromPrim 0x80000001u ) ))
        Assert.False(( statsn_me.lessThan ( statsn_me.fromPrim 1u ) ( statsn_me.fromPrim 0x80000002u ) ))

    [<Fact>]
    member _.statsn_me_compare_001() =
        Assert.True(( ( statsn_me.compare ( statsn_me.fromPrim 1u ) ( statsn_me.fromPrim 2u ) ) < 0 ))
        Assert.True(( ( statsn_me.compare ( statsn_me.fromPrim 2u ) ( statsn_me.fromPrim 1u ) ) > 0 ))
        Assert.True(( ( statsn_me.compare ( statsn_me.fromPrim 1u ) ( statsn_me.fromPrim 1u ) ) = 0 ))
        Assert.True(( ( statsn_me.compare ( statsn_me.fromPrim 0xFFFFFFFFu ) ( statsn_me.fromPrim 1u ) ) < 0 ))
        Assert.True(( ( statsn_me.compare ( statsn_me.fromPrim 0xFFFFFFFFu ) ( statsn_me.fromPrim 0x7FFFFFFEu ) ) < 0 ))
        Assert.True(( ( statsn_me.compare ( statsn_me.fromPrim 0xFFFFFFFFu ) ( statsn_me.fromPrim 0x7FFFFFFFu ) ) > 0 ))    // undefined
        Assert.True(( ( statsn_me.compare ( statsn_me.fromPrim 0xFFFFFFFFu ) ( statsn_me.fromPrim 0x80000000u ) ) > 0 ))
        Assert.True(( ( statsn_me.compare ( statsn_me.fromPrim 1u ) ( statsn_me.fromPrim 0x80000000u ) ) < 0 ))
        Assert.True(( ( statsn_me.compare ( statsn_me.fromPrim 1u ) ( statsn_me.fromPrim 0x80000001u ) ) < 0 )) // undefined
        Assert.True(( ( statsn_me.compare ( statsn_me.fromPrim 1u ) ( statsn_me.fromPrim 0x80000002u ) ) > 0 ))

    [<Fact>]
    member _.datasn_me_fromPrim_001() =
        Assert.True(( datasn_me.fromPrim 0u = 0u<datasn_me> ))
        Assert.True(( datasn_me.fromPrim 1u = 1u<datasn_me> ))
        Assert.True(( datasn_me.fromPrim 0xFFFFFFFFu = 4294967295u<datasn_me> ))

    [<Fact>]
    member _.datasn_me_toPrim_001() =
        Assert.True(( datasn_me.toPrim( 0u<datasn_me> ) = 0x00000000u ))
        Assert.True(( datasn_me.toPrim( 1u<datasn_me> ) = 0x00000001u ))
        Assert.True(( datasn_me.toPrim( 4294967295u<datasn_me> ) = 0xFFFFFFFFu ))

    [<Fact>]
    member _.datasn_me_zero_001() =
        Assert.True(( datasn_me.zero = 0u<datasn_me> ))

    [<Fact>]
    member _.datasn_me_fromOpt_001() =
        Assert.True(( ( datasn_me.fromOpt 1u None ) = datasn_me.fromPrim 1u ))
        Assert.True(( ( datasn_me.fromOpt 1u ( Some ( datasn_me.fromPrim 2u ) ) ) = datasn_me.fromPrim 2u ))

    [<Fact>]
    member _.datasn_me_lessThan_001() =
        Assert.True(( datasn_me.lessThan ( datasn_me.fromPrim 1u ) ( datasn_me.fromPrim 2u ) ))
        Assert.False(( datasn_me.lessThan ( datasn_me.fromPrim 2u ) ( datasn_me.fromPrim 1u ) ))
        Assert.False(( datasn_me.lessThan ( datasn_me.fromPrim 1u ) ( datasn_me.fromPrim 1u ) ))
        Assert.True(( datasn_me.lessThan ( datasn_me.fromPrim 0xFFFFFFFFu ) ( datasn_me.fromPrim 1u ) ))
        Assert.True(( datasn_me.lessThan ( datasn_me.fromPrim 0xFFFFFFFFu ) ( datasn_me.fromPrim 0x7FFFFFFEu ) ))
        Assert.False(( datasn_me.lessThan ( datasn_me.fromPrim 0xFFFFFFFFu ) ( datasn_me.fromPrim 0x7FFFFFFFu ) ))
        Assert.False(( datasn_me.lessThan ( datasn_me.fromPrim 0xFFFFFFFFu ) ( datasn_me.fromPrim 0x80000000u ) ))
        Assert.True(( datasn_me.lessThan ( datasn_me.fromPrim 1u ) ( datasn_me.fromPrim 0x80000000u ) ))
        Assert.False(( datasn_me.lessThan ( datasn_me.fromPrim 1u ) ( datasn_me.fromPrim 0x80000001u ) ))
        Assert.False(( datasn_me.lessThan ( datasn_me.fromPrim 1u ) ( datasn_me.fromPrim 0x80000002u ) ))

    [<Fact>]
    member _.datasn_me_compare_001() =
        Assert.True(( ( datasn_me.compare ( datasn_me.fromPrim 1u ) ( datasn_me.fromPrim 2u ) ) < 0 ))
        Assert.True(( ( datasn_me.compare ( datasn_me.fromPrim 2u ) ( datasn_me.fromPrim 1u ) ) > 0 ))
        Assert.True(( ( datasn_me.compare ( datasn_me.fromPrim 1u ) ( datasn_me.fromPrim 1u ) ) = 0 ))
        Assert.True(( ( datasn_me.compare ( datasn_me.fromPrim 0xFFFFFFFFu ) ( datasn_me.fromPrim 1u ) ) < 0 ))
        Assert.True(( ( datasn_me.compare ( datasn_me.fromPrim 0xFFFFFFFFu ) ( datasn_me.fromPrim 0x7FFFFFFEu ) ) < 0 ))
        Assert.True(( ( datasn_me.compare ( datasn_me.fromPrim 0xFFFFFFFFu ) ( datasn_me.fromPrim 0x7FFFFFFFu ) ) > 0 ))    // undefined
        Assert.True(( ( datasn_me.compare ( datasn_me.fromPrim 0xFFFFFFFFu ) ( datasn_me.fromPrim 0x80000000u ) ) > 0 ))
        Assert.True(( ( datasn_me.compare ( datasn_me.fromPrim 1u ) ( datasn_me.fromPrim 0x80000000u ) ) < 0 ))
        Assert.True(( ( datasn_me.compare ( datasn_me.fromPrim 1u ) ( datasn_me.fromPrim 0x80000001u ) ) < 0 ))    // undefined
        Assert.True(( ( datasn_me.compare ( datasn_me.fromPrim 1u ) ( datasn_me.fromPrim 0x80000002u ) ) > 0 ))


    [<Fact>]
    member _.snacktag_me_fromPrim_001() =
        Assert.True(( snacktag_me.fromPrim 0u = 0u<snacktag_me> ))
        Assert.True(( snacktag_me.fromPrim 1u = 1u<snacktag_me> ))
        Assert.True(( snacktag_me.fromPrim 0xFFFFFFFFu = 4294967295u<snacktag_me> ))

    [<Fact>]
    member _.snacktag_me_toPrim_001() =
        Assert.True(( snacktag_me.toPrim( 0u<snacktag_me> ) = 0x00000000u ))
        Assert.True(( snacktag_me.toPrim( 1u<snacktag_me> ) = 0x00000001u ))
        Assert.True(( snacktag_me.toPrim( 4294967295u<snacktag_me> ) = 0xFFFFFFFFu ))

    [<Fact>]
    member _.snacktag_me_zero_001() =
        Assert.True(( snacktag_me.zero = 0u<snacktag_me> ))

    [<Fact>]
    member _.TargetDeviceID_001() =
        let t1 = tdid_me.Zero
        Assert.True(( tdid_me.toPrim t1 = 0u ))
        Assert.True(( tdid_me.toString t1 = "TD_00000000" ))

    [<Fact>]
    member _.TargetDeviceID_002() =
        let t1 = tdid_me.fromPrim( 0x11223344u )
        Assert.True(( tdid_me.toPrim t1 = 0x11223344u ))
        Assert.True(( tdid_me.toString t1 = "TD_11223344" ))

    [<Fact>]
    member _.TargetDeviceID_003() =
        let t1 = tdid_me.fromPrim( 0x11223344u )
        let t2 = tdid_me.fromPrim( 0x11223344u )
        let t3 = tdid_me.Zero
        Assert.True(( t1 = t2 ))
        Assert.False(( t1 = t3 ))

    [<Fact>]
    member _.TargetDeviceID_004() =
        let t1 = tdid_me.Zero
        Assert.True(( tdid_me.toPrim t1 = 0u ))

    [<Fact>]
    member _.TargetDeviceID_005() =
        let v = [|
            tdid_me.fromPrim( 1u );
            tdid_me.fromPrim( 2u );
        |]
        let t1 = tdid_me.NewID v
        Assert.True(( tdid_me.toPrim t1 = 3u ))

    [<Fact>]
    member _.TargetDeviceID_006() =
        let v = Array.empty
        let t1 = tdid_me.NewID v
        Assert.True(( tdid_me.toPrim t1 = 1u ))

    [<Fact>]
    member _.TargetDeviceID_007() =
        let v = [|
            tdid_me.fromPrim( UInt32.MaxValue );
        |]
        let t1 = tdid_me.NewID v
        Assert.True(( tdid_me.toPrim t1 = 1u ))

    [<Fact>]
    member _.TargetDeviceID_008() =
        let v = [|
            tdid_me.fromPrim( 1u );
            tdid_me.fromPrim( UInt32.MaxValue );
        |]
        let t1 = tdid_me.NewID v
        Assert.True(( tdid_me.toPrim t1 = 2u ))

    [<Fact>]
    member _.TargetDeviceID_009() =
        let t1 = tdid_me.fromString( "TD_00112233" )
        Assert.True(( tdid_me.toPrim t1 = 0x00112233u ))

    [<Fact>]
    member _.TargetDeviceID_010() =
        try
            let _ = tdid_me.fromString( "TD_aaa" )
            Assert.Fail __LINE__
        with
        | :? FormatException ->
            ()
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TargetDeviceID_011() =
        try
            let _ = tdid_me.fromString( "00112233" )
            Assert.Fail __LINE__
        with
        | :? FormatException ->
            ()
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TargetGroupID_001() =
        let t1 = tgid_me.Zero
        Assert.True(( tgid_me.toPrim t1 = 0u ))
        Assert.True(( tgid_me.toString t1 = "TG_00000000" ))

    [<Fact>]
    member _.TargetGroupID_002() =
        let t1 = tgid_me.fromPrim( 0x11223344u )
        Assert.True(( tgid_me.toPrim t1 = 0x11223344u ))
        Assert.True(( tgid_me.toString t1 = "TG_11223344" ))

    [<Fact>]
    member _.TargetGroupID_003() =
        let t1 = tgid_me.fromPrim( 0x11223344u )
        let t2 = tgid_me.fromPrim( 0x11223344u )
        let t3 = tgid_me.Zero
        Assert.True(( t1 = t2 ))
        Assert.False(( t1 = t3 ))

    [<Fact>]
    member _.TargetGroupID_004() =
        let t1 = tgid_me.Zero
        Assert.True(( tgid_me.toPrim t1 = 0u ))

    [<Fact>]
    member _.TargetGroupID_005() =
        let v = [|
            tgid_me.fromPrim( 1u );
            tgid_me.fromPrim( 2u );
        |]
        let t1 = tgid_me.NewID v
        Assert.True(( tgid_me.toPrim t1 = 3u ))

    [<Fact>]
    member _.TargetGroupID_006() =
        let v = Array.empty
        let t1 = tgid_me.NewID v
        Assert.True(( tgid_me.toPrim t1 = 1u ))

    [<Fact>]
    member _.TargetGroupID_007() =
        let v = [|
            tgid_me.fromPrim( UInt32.MaxValue );
        |]
        let t1 = tgid_me.NewID v
        Assert.True(( tgid_me.toPrim t1 = 1u ))

    [<Fact>]
    member _.TargetGroupID_008() =
        let v = [|
            tgid_me.fromPrim( 1u );
            tgid_me.fromPrim( UInt32.MaxValue );
        |]
        let t1 = tgid_me.NewID v
        Assert.True(( tgid_me.toPrim t1 = 2u ))

    [<Fact>]
    member _.TargetGroupID_009() =
        let t1 = tgid_me.fromString( "TG_00112233" )
        Assert.True(( tgid_me.toPrim t1 = 0x00112233u ))

    [<Fact>]
    member _.TargetGroupID_010() =
        try
            let _ = tgid_me.fromString( "TG_aaa" )
            Assert.Fail __LINE__
        with
        | :? FormatException ->
            ()
        | _ ->
            Assert.Fail __LINE__

    [<Fact>]
    member _.TargetGroupID_011() =
        try
            let _ = tgid_me.fromString( "00112233" )
            Assert.Fail __LINE__
        with
        | :? FormatException ->
            ()
        | _ ->
            Assert.Fail __LINE__

