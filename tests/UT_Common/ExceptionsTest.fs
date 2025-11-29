//=============================================================================
// Haruka Software Storage.
// ExceptionsTest.fs : Test cases for Exceptions class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Test

//=============================================================================
// Class implementation

type Exceptions_Test () =

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_001() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.ABORTED_COMMAND,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                ValueNone,   // m_SenseKeySpecific
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.True( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_003() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.ILLEGAL_REQUEST,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.FieldPointer(
                    { CommandData = true; BPV = true; BitPointer = 0uy; FieldPointer = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.True( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_004() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.ILLEGAL_REQUEST,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome ( senseKeySpecificSenseDataDesc.ActualRetryCount (
                    { ActualRetryCount = 0us; }
                ) ) ,
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_005() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.ILLEGAL_REQUEST,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome ( senseKeySpecificSenseDataDesc.ProgressIndication (
                    { ProgressIndication = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_006() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.ILLEGAL_REQUEST,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome ( senseKeySpecificSenseDataDesc.SegmentPointer (
                    { SD = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us; }
                ) ) ,
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_008() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.HARDWARE_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome ( senseKeySpecificSenseDataDesc.FieldPointer( 
                    { CommandData = true; BPV = true; BitPointer = 0uy; FieldPointer = 0us };
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_009() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.HARDWARE_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome ( senseKeySpecificSenseDataDesc.ActualRetryCount(
                    { ActualRetryCount = 0us; }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.True( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_010() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.HARDWARE_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome ( senseKeySpecificSenseDataDesc.ProgressIndication(
                    { ProgressIndication = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_011() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.HARDWARE_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome ( senseKeySpecificSenseDataDesc.SegmentPointer(
                    { SD = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us; }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_013() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.MEDIUM_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.FieldPointer(
                    { CommandData = true; BPV = true; BitPointer = 0uy; FieldPointer = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_014() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.MEDIUM_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ActualRetryCount(
                    { ActualRetryCount = 0us; }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.True( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_015() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.MEDIUM_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ProgressIndication(
                    { ProgressIndication = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_016() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.MEDIUM_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.SegmentPointer(
                    { SD = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us; }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_018() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.RECOVERED_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.FieldPointer(
                    { CommandData = true; BPV = true; BitPointer = 0uy; FieldPointer = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_019() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.RECOVERED_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ActualRetryCount(
                    { ActualRetryCount = 0us; }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.True( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_020() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.RECOVERED_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ProgressIndication(
                    { ProgressIndication = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_021() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.RECOVERED_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.SegmentPointer(
                    { SD = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us; }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_023() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NO_SENSE,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.FieldPointer(
                    { CommandData = true; BPV = true; BitPointer = 0uy; FieldPointer = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_024() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NO_SENSE,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ActualRetryCount(
                    { ActualRetryCount = 0us; }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_025() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NO_SENSE,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ProgressIndication(
                    { ProgressIndication = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.True( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_026() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NO_SENSE,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.SegmentPointer(
                    { SD = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us; }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_028() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NOT_READY,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.FieldPointer(
                    { CommandData = true; BPV = true; BitPointer = 0uy; FieldPointer = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_029() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NOT_READY,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ActualRetryCount(
                    { ActualRetryCount = 0us; }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_030() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NOT_READY,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ProgressIndication(
                    { ProgressIndication = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.True( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_031() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NOT_READY,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.SegmentPointer(
                    { SD = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us; }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_033() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.FieldPointer(
                    { CommandData = true; BPV = true; BitPointer = 0uy; FieldPointer = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_034() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ActualRetryCount(
                    { ActualRetryCount = 0us; }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_035() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ProgressIndication(
                    { ProgressIndication = 0us }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.False( result )

    [<Fact>]
    member _.Test_m_SenseKeySpecificArgsValid_036() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                ValueNone,   // m_Information
                ValueNone,   // m_CommandSpecific
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.SegmentPointer(
                    { SD = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us; }
                ) ),
                ValueNone,   // m_FieldReplaceableUnit
                ValueNone,   // m_VendorSpecific
                ValueNone    // m_BlockCommand
            )
        let pc = new PrivateCaller( sd )
        let result = pc.GetField( "m_SenseKeySpecificArgsValid" ) :?> bool
        Assert.True( result )

    [<Fact>]
    member _.Test_Constractor_001() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_Information
                ValueSome { Information = [| 0uy .. 5uy |] },
                // m_CommandSpecific
                ValueSome { CommandSpecific = [| 1uy .. 6uy |] },
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.SegmentPointer(
                    { SD = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us; }
                ) ),
                // m_FieldReplaceableUnit
                ValueSome { FieldReplaceableUnitCode = 1uy },
                // m_VendorSpecific
                ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 2uy .. 7uy |] },
                // m_BlockCommand
                ValueSome { ILI = true }
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( ( sd.Information |> ValueOption.get |> _.Information = [| 0uy .. 5uy |] ) )
        Assert.True( ( sd.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 1uy .. 6uy |] ) )
        Assert.True( ( sd.FieldPointer = ValueNone ) )
        Assert.True( ( sd.ActualRetryCount = ValueNone ) )
        Assert.True( ( sd.ProgressIndication = ValueNone ) )
        Assert.True( ( sd.SegmentPointer |> ValueOption.get |> _.SD =  true ) )
        Assert.True( ( sd.SegmentPointer |> ValueOption.get |> _.BPV = false ) )
        Assert.True( ( sd.SegmentPointer |> ValueOption.get |> _.BitPointer = 0uy ) )
        Assert.True( ( sd.SegmentPointer |> ValueOption.get |> _.FieldPointer = 0us ) )
        Assert.True( ( sd.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 1uy ) )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ) )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.VendorSpecific = [| 2uy .. 7uy |] ) )
        Assert.True( ( sd.BlockCommand |> ValueOption.get |> _.ILI = true ) )

    [<Fact>]
    member _.Test_Constractor_002() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_Information
                { Information = [| 0uy .. 5uy |] },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE,
                "abcdefg"
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( ( sd.Information |> ValueOption.get |> _.Information = [| 0uy .. 5uy |] ) )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer  |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount  |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication  |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer  |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ) )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.VendorSpecific = System.Text.Encoding.GetEncoding( "utf-8" ).GetBytes( "abcdefg" ) ) )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_003() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_Information
                { Information = [| 0uy .. 5uy |] },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( ( sd.Information |> ValueOption.get |> _.Information = [| 0uy .. 5uy |] ) )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_004() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_Information
                { Information = [| 0uy .. 5uy |] }
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( ( sd.Information |> ValueOption.get |> _.Information = [| 0uy .. 5uy |] ) )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_005() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_CommandSpecific
                { CommandSpecific = [| 1uy .. 6uy |] },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE,
                "abcdefg"
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( ( sd.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 1uy .. 6uy |] ) )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ) )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.VendorSpecific = System.Text.Encoding.GetEncoding( "utf-8" ).GetBytes( "abcdefg" ) ) )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_006() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_CommandSpecific
                { CommandSpecific = [| 1uy .. 6uy |] },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( ( sd.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 1uy .. 6uy |] ) )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_007() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_CommandSpecific
                { CommandSpecific = [| 1uy .. 6uy |] }
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( ( sd.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 1uy .. 6uy |] ) )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_008() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // FieldPointer
                { CommandData = true; BPV = true; BitPointer = 0uy; FieldPointer = 0us },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE,
                "abcdefg"
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.get |> _.CommandData )
        Assert.True( sd.FieldPointer |> ValueOption.get |> _.BPV )
        Assert.True( ( sd.FieldPointer |> ValueOption.get |> _.BitPointer = 0uy ) )
        Assert.True( ( sd.FieldPointer |> ValueOption.get |> _.FieldPointer = 0us ) )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ) )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.VendorSpecific = System.Text.Encoding.GetEncoding( "utf-8" ).GetBytes( "abcdefg" ) ) )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_009() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // FieldPointer
                { CommandData = true; BPV = true; BitPointer = 0uy; FieldPointer = 0us },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.get |> _.CommandData )
        Assert.True( sd.FieldPointer |> ValueOption.get |> _.BPV )
        Assert.True( ( sd.FieldPointer |> ValueOption.get |> _.BitPointer = 0uy ) )
        Assert.True( ( sd.FieldPointer |> ValueOption.get |> _.FieldPointer = 0us ) )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_010() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // FieldPointer
                { CommandData = true; BPV = true; BitPointer = 0uy; FieldPointer = 0us }
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.get |> _.CommandData )
        Assert.True( sd.FieldPointer |> ValueOption.get |> _.BPV )
        Assert.True( ( sd.FieldPointer |> ValueOption.get |> _.BitPointer = 0uy ) )
        Assert.True( ( sd.FieldPointer |> ValueOption.get |> _.FieldPointer = 0us ) )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_011() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // ActualRetryCount
                { ActualRetryCount = 0us; },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE,
                "OPQRSTUVWX"
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( ( sd.ActualRetryCount |> ValueOption.get |> _.ActualRetryCount = 0us ) )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ) )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.VendorSpecific = System.Text.Encoding.GetEncoding( "utf-8" ).GetBytes( "OPQRSTUVWX" ) ) )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_012() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // ActualRetryCount
                { ActualRetryCount = 0us; },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( ( sd.ActualRetryCount |> ValueOption.get |> _.ActualRetryCount = 0us ) )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_013() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // ActualRetryCount
                { ActualRetryCount = 0us; }
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( ( sd.ActualRetryCount |> ValueOption.get |> _.ActualRetryCount = 0us ) )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_014() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.BLOCK_SEQUENCE_ERROR,
                // ProgressIndication
                { ProgressIndication = 0us; },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE,
                "abcdefg"
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.BLOCK_SEQUENCE_ERROR ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( ( sd.ProgressIndication |> ValueOption.get |> _.ProgressIndication = 0us ) )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ) )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.VendorSpecific = System.Text.Encoding.GetEncoding( "utf-8" ).GetBytes( "abcdefg" ) ) )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_015() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // ProgressIndication
                { ProgressIndication = 0us; },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( ( sd.ProgressIndication |> ValueOption.get |> _.ProgressIndication = 0us ) )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_016() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.BLANK_CHECK,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // ProgressIndication
                { ProgressIndication = 0us; }
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.BLANK_CHECK ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( ( sd.ProgressIndication |> ValueOption.get |> _.ProgressIndication = 0us ) )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_017() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // SegmentPointer
                { SD = true; BPV = false; BitPointer = 0uy; FieldPointer = 0us; },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE,
                "abcdefg"
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.get |> _.SD )
        Assert.False( sd.SegmentPointer |> ValueOption.get |> _.BPV )
        Assert.True( ( sd.SegmentPointer |> ValueOption.get |> _.BitPointer = 0uy ) )
        Assert.True( ( sd.SegmentPointer |> ValueOption.get |> _.FieldPointer = 0us ) )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ) )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.VendorSpecific = System.Text.Encoding.GetEncoding( "utf-8" ).GetBytes( "abcdefg" ) ) )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_018() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // SegmentPointer
                { SD = true; BPV = true; BitPointer = 0uy; FieldPointer = 0us; },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.get |> _.SD )
        Assert.True( sd.SegmentPointer |> ValueOption.get |> _.BPV )
        Assert.True( ( sd.SegmentPointer |> ValueOption.get |> _.BitPointer = 0uy ) )
        Assert.True( ( sd.SegmentPointer |> ValueOption.get |> _.FieldPointer = 0us ) )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_019() =
        let sd =
            new SenseData(
                false,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // SegmentPointer
                { SD = false; BPV = true; BitPointer = 0uy; FieldPointer = 0us; }
            )
        Assert.False( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.False( sd.SegmentPointer |> ValueOption.get |> _.SD )
        Assert.True( sd.SegmentPointer |> ValueOption.get |> _.BPV )
        Assert.True( ( sd.SegmentPointer |> ValueOption.get |> _.BitPointer = 0uy ) )
        Assert.True( ( sd.SegmentPointer |> ValueOption.get |> _.FieldPointer = 0us ) )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_020() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_FieldReplaceableUnit
                { FieldReplaceableUnitCode = 1uy },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE,
                "abcdefg"
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( ( sd.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 1uy ) )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ) )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.VendorSpecific = System.Text.Encoding.GetEncoding( "utf-8" ).GetBytes( "abcdefg" ) ) )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_021() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_FieldReplaceableUnit
                { FieldReplaceableUnitCode = 1uy },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( ( sd.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 1uy ) )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_022() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_FieldReplaceableUnit
                { FieldReplaceableUnitCode = 1uy }
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( ( sd.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 1uy ) )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_023() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_FieldReplaceableUnit
                { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 2uy .. 7uy |] }
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ) )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.VendorSpecific = [| 2uy .. 7uy |] ) )
        Assert.True( sd.BlockCommand |> ValueOption.isNone )

    [<Fact>]
    member _.Test_Constractor_024() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_BlockCommand
                { ILI = true },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE,
                "abcdefg"
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ) )
        Assert.True( ( sd.VendorSpecific |> ValueOption.get |> _.VendorSpecific = System.Text.Encoding.GetEncoding( "utf-8" ).GetBytes( "abcdefg" ) ) )
        Assert.True( sd.BlockCommand |> ValueOption.get |> _.ILI )

    [<Fact>]
    member _.Test_Constractor_025() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_BlockCommand
                { ILI = true },
                VendorSpecificSenseDataDescType.TEXT_MESSAGE
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.get |> _.ILI )

    [<Fact>]
    member _.Test_Constractor_026() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_BlockCommand
                { ILI = true }
            )
        Assert.True( sd.IsCurrent )
        Assert.True(( sd.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True( sd.Information |> ValueOption.isNone )
        Assert.True( sd.CommandSpecific |> ValueOption.isNone )
        Assert.True( sd.FieldPointer |> ValueOption.isNone )
        Assert.True( sd.ActualRetryCount |> ValueOption.isNone )
        Assert.True( sd.ProgressIndication |> ValueOption.isNone )
        Assert.True( sd.SegmentPointer |> ValueOption.isNone )
        Assert.True( sd.FieldReplaceableUnit |> ValueOption.isNone )
        Assert.True( sd.VendorSpecific |> ValueOption.isNone )
        Assert.True( sd.BlockCommand |> ValueOption.get |> _.ILI )

    [<Fact>]
    member _.Test_GetSenseData_Information_001() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_Information
                { Information = [| 0uy .. 3uy |] }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x0Cuy; // ADDITIONAL SENSE LENGTH

                // Information
                0x00uy; // DESCRIPTOR TYPE ( Information )
                0x0Auy; // ADDITIONAL LENGTH
                0x80uy; // VALID, Reserved
                0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x00uy; 0x01uy; 0x02uy; 0x03uy;
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_Information_002() =
        let sd =
            new SenseData(
                false,
                SenseKeyCd.MISCOMPARE,
                ASCCd.MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE,
                // m_Information
                { Information = [| 0uy .. 7uy |] }
            )
        let exp =
            [|
                0x73uy; // RESPONSE CODE ( deferred )
                0x0Euy; // SENSE KEY ( MISCOMPARE )
                0x3Auy; // ADDITIONAL SENSE CODE ( MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE )
                0x04uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x0Cuy; // ADDITIONAL SENSE LENGTH

                // Information
                0x00uy; // DESCRIPTOR TYPE ( Information )
                0x0Auy; // ADDITIONAL LENGTH
                0x80uy; // VALID, Reserved
                0x00uy; // Reserved
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // INFORMATION
                0x04uy; 0x05uy; 0x06uy; 0x07uy;
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_Information_003() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_Information
                { Information = Array.empty }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x0Cuy; // ADDITIONAL SENSE LENGTH

                // Information
                0x00uy; // DESCRIPTOR TYPE ( Information )
                0x0Auy; // ADDITIONAL LENGTH
                0x80uy; // VALID, Reserved
                0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_Information_004() =
        let sd =
            new SenseData(
                false,
                SenseKeyCd.MISCOMPARE,
                ASCCd.MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE,
                // m_Information
                { Information = [| 0uy .. 8uy |] }
            )
        let exp =
            [|
                0x73uy; // RESPONSE CODE ( deferred )
                0x0Euy; // SENSE KEY ( MISCOMPARE )
                0x3Auy; // ADDITIONAL SENSE CODE ( MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE )
                0x04uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x0Cuy; // ADDITIONAL SENSE LENGTH

                // Information
                0x00uy; // DESCRIPTOR TYPE ( Information )
                0x0Auy; // ADDITIONAL LENGTH
                0x80uy; // VALID, Reserved
                0x00uy; // Reserved
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // INFORMATION
                0x04uy; 0x05uy; 0x06uy; 0x07uy;
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_Information_005() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.MISCOMPARE,
                ASCCd.MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE,
                // m_Information
                { Information = [| 0uy .. 3uy |] }
            )
        let exp =
            [|
                0xF0uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Euy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( MISCOMPARE )
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x3Auy; // ADDITIONAL SENSE CODE ( MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE )
                0x04uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_Information_006() =
        let sd =
            new SenseData(
                false,
                SenseKeyCd.MISCOMPARE,
                ASCCd.MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE,
                // m_Information
                { Information = [| 0uy .. 4uy |] }
            )
        let exp =
            [|
                0x71uy; // VALID,RESPONSE CODE ( deferred )
                0x00uy; // Obsolete
                0x0Euy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( MISCOMPARE )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x3Auy; // ADDITIONAL SENSE CODE ( MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE )
                0x04uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_Information_007() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.MISCOMPARE,
                ASCCd.MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE,
                // m_Information
                { Information = [| 0xAAuy; 0xBBuy; 0xCCuy; |] }
            )
        let exp =
            [|
                0xF0uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Euy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( MISCOMPARE )
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x3Auy; // ADDITIONAL SENSE CODE ( MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE )
                0x04uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_Information_008() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.MISCOMPARE,
                ASCCd.MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE,
                // m_Information
                { Information = Array.empty }
            )
        let exp =
            [|
                0xF0uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Euy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( MISCOMPARE )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x3Auy; // ADDITIONAL SENSE CODE ( MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE )
                0x04uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_CommandSpecific_001() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_CommandSpecific
                { CommandSpecific = [| 0uy .. 3uy |] }
            )     
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x0Cuy; // ADDITIONAL SENSE LENGTH

                // CommandSpecific
                0x01uy; // DESCRIPTOR TYPE ( CommandSpecific )
                0x0Auy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; //  COMMAND-SPECIFIC INFORMATION
                0x00uy; 0x01uy; 0x02uy; 0x03uy;
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_CommandSpecific_002() =
        let sd =
            new SenseData(
                false,
                SenseKeyCd.MISCOMPARE,
                ASCCd.MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE,
                // m_CommandSpecific
                { CommandSpecific = [| 0uy .. 7uy |] }
            )     
        let exp =
            [|
                0x73uy; // RESPONSE CODE ( deferred )
                0x0Euy; // SENSE KEY ( MISCOMPARE )
                0x3Auy; // ADDITIONAL SENSE CODE ( MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE )
                0x04uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x0Cuy; // ADDITIONAL SENSE LENGTH

                // CommandSpecific
                0x01uy; // DESCRIPTOR TYPE ( CommandSpecific )
                0x0Auy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0x00uy; 0x01uy; 0x02uy; 0x03uy; //  COMMAND-SPECIFIC INFORMATION
                0x04uy; 0x05uy; 0x06uy; 0x07uy;
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_CommandSpecific_003() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_CommandSpecific
                { CommandSpecific = Array.empty }
            )     
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x0Cuy; // ADDITIONAL SENSE LENGTH

                // CommandSpecific
                0x01uy; // DESCRIPTOR TYPE ( CommandSpecific )
                0x0Auy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; //  COMMAND-SPECIFIC INFORMATION
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_CommandSpecific_004() =
        let sd =
            new SenseData(
                false,
                SenseKeyCd.MISCOMPARE,
                ASCCd.MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE,
                // m_CommandSpecific
                { CommandSpecific = [| 0uy .. 8uy |] }
            )     
        let exp =
            [|
                0x73uy; // RESPONSE CODE ( deferred )
                0x0Euy; // SENSE KEY ( MISCOMPARE )
                0x3Auy; // ADDITIONAL SENSE CODE ( MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE )
                0x04uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x0Cuy; // ADDITIONAL SENSE LENGTH

                // CommandSpecific
                0x01uy; // DESCRIPTOR TYPE ( CommandSpecific )
                0x0Auy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0x00uy; 0x01uy; 0x02uy; 0x03uy; //  COMMAND-SPECIFIC INFORMATION
                0x04uy; 0x05uy; 0x06uy; 0x07uy;
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_CommandSpecific_005() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_CommandSpecific
                { CommandSpecific = [| 0uy .. 3uy |] }
            )     
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Auy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( COPY_ABORTED )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x01uy; 0x02uy; 0x03uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_CommandSpecific_006() =
        let sd =
            new SenseData(
                false,
                SenseKeyCd.MISCOMPARE,
                ASCCd.MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE,
                // m_CommandSpecific
                { CommandSpecific = [| 0uy .. 4uy |] }
            )     
        let exp =
            [|
                0x71uy; // VALID,RESPONSE CODE ( deferred )
                0x00uy; // Obsolete
                0x0Euy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( MISCOMPARE )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x3Auy; // ADDITIONAL SENSE CODE ( MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE )
                0x04uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_CommandSpecific_007() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.MISCOMPARE,
                ASCCd.MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE,
                // m_CommandSpecific
                { CommandSpecific = [| 0xAAuy; 0xBBuy; 0xCCuy; |] }
            )     
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Euy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( MISCOMPARE )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0xAAuy; 0xBBuy; 0xCCuy; // COMMAND-SPECIFIC INFORMATION
                0x3Auy; // ADDITIONAL SENSE CODE ( MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE )
                0x04uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_CommandSpecific_008() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.MISCOMPARE,
                ASCCd.MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE,
                // m_CommandSpecific
                { CommandSpecific = Array.empty }
            )     
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Euy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( MISCOMPARE )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x3Auy; // ADDITIONAL SENSE CODE ( MEDIUM_NOT_PRESENT_MEDIUM_AUXILIARY_MEMORY_ACCESSIBLE )
                0x04uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
                0x00uy; // SENSE KEY SPECIFIC
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_FieldPointer_001() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.ILLEGAL_REQUEST,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // FieldPointer
                { CommandData = true; BPV = true; BitPointer = 1uy; FieldPointer = 2us }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x05uy; // SENSE KEY ( ILLEGAL_REQUEST )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x08uy; // ADDITIONAL SENSE LENGTH

                // Information
                0x02uy; // DESCRIPTOR TYPE ( SenseKeySpecific )
                0x06uy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0xC9uy; // SKSV, CommandData, BPV, BitPointer
                0x00uy; 0x02uy; // FieldPointer
                0x00uy; // Reserved
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_FieldPointer_002() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.MISCOMPARE,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // FieldPointer
                { CommandData = true; BPV = true; BitPointer = 1uy; FieldPointer = 2us }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Euy; // SENSE KEY ( MISCOMPARE )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; // ADDITIONAL SENSE LENGTH
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_FieldPointer_003() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.ILLEGAL_REQUEST,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // FieldPointer
                { CommandData = true; BPV = true; BitPointer = 1uy; FieldPointer = 2us }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x05uy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( ILLEGAL_REQUEST )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0xC9uy; // SKSV, CommandData, BPV, BitPointer
                0x00uy; 0x02uy; // FieldPointer
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_FieldPointer_004() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.MISCOMPARE,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // FieldPointer
                { CommandData = true; BPV = true; BitPointer = 1uy; FieldPointer = 2us }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Euy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( MISCOMPARE )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, CommandData, BPV, BitPointer
                0x00uy; 0x00uy; // FieldPointer
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_ActualRetryCount_001() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.HARDWARE_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // ActualRetryCount
                { ActualRetryCount = 0xFFEEus; }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x04uy; // SENSE KEY ( HARDWARE_ERROR )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x08uy; // ADDITIONAL SENSE LENGTH

                // ActualRetryCount
                0x02uy; // DESCRIPTOR TYPE ( SenseKeySpecific )
                0x06uy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0x80uy; // SKSV
                0xFFuy; 0xEEuy; // ActualRetryCount
                0x00uy; // Reserved
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_ActualRetryCount_002() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // ActualRetryCount
                { ActualRetryCount = 0xFFEEus; }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; // ADDITIONAL SENSE LENGTH
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_ActualRetryCount_003() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.HARDWARE_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // ActualRetryCount
                { ActualRetryCount = 0xFFEEus; }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x04uy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( HARDWARE_ERROR )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x80uy; // SKSV
                0xFFuy; 0xEEuy; // ActualRetryCount
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_ActualRetryCount_004() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // ActualRetryCount
                { ActualRetryCount = 0xFFEEus; }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Auy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( COPY_ABORTED )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV
                0x00uy; 0x00uy; // ActualRetryCount
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_ProgressIndication_001() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NO_SENSE,
                ASCCd.OPERATION_IN_PROGRESS,
                // ProgressIndication
                { ProgressIndication = 0xFFEEus; }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x00uy; // SENSE KEY ( NO_SENSE )
                0x00uy; // ADDITIONAL SENSE CODE ( OPERATION_IN_PROGRESS )
                0x16uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x08uy; // ADDITIONAL SENSE LENGTH

                // ProgressIndication
                0x02uy; // DESCRIPTOR TYPE ( SenseKeySpecific )
                0x06uy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0x80uy; // SKSV
                0xFFuy; 0xEEuy; // ProgressIndication
                0x00uy; // Reserved
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_ProgressIndication_002() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // ProgressIndication
                { ProgressIndication = 0xFFEEus; }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; // ADDITIONAL SENSE LENGTH
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_ProgressIndication_003() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NO_SENSE,
                ASCCd.OPERATION_IN_PROGRESS,
                // ProgressIndication
                { ProgressIndication = 0xFFEEus; }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x00uy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( NO_SENSE )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x00uy; // ADDITIONAL SENSE CODE ( OPERATION_IN_PROGRESS )
                0x16uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x80uy; // SKSV
                0xFFuy; 0xEEuy; // ProgressIndication
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_ProgressIndication_004() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // ProgressIndication
                { ProgressIndication = 0xFFEEus; }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Auy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( COPY_ABORTED )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV
                0x00uy; 0x00uy; // ProgressIndication
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_SegmentPointer_001() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // SegmentPointer
                { SD = true; BPV = true; BitPointer = 0x01uy; FieldPointer = 0xFFEEus; }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x08uy; // ADDITIONAL SENSE LENGTH

                // SegmentPointer
                0x02uy; // DESCRIPTOR TYPE ( SenseKeySpecific )
                0x06uy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0xA9uy; // SKSV, SD, BPV, BitPointer
                0xFFuy; 0xEEuy; // FieldPointer
                0x00uy; // Reserved
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_SegmentPointer_002() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NO_SENSE,
                ASCCd.OPERATION_IN_PROGRESS,
                // SegmentPointer
                { SD = true; BPV = true; BitPointer = 0x01uy; FieldPointer = 0xFFEEus; }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x00uy; // SENSE KEY ( NO_SENSE )
                0x00uy; // ADDITIONAL SENSE CODE ( OPERATION_IN_PROGRESS )
                0x16uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x00uy; // ADDITIONAL SENSE LENGTH
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_SegmentPointer_003() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // SegmentPointer
                { SD = true; BPV = true; BitPointer = 0x01uy; FieldPointer = 0xFFEEus; }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Auy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( COPY_ABORTED )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0xA9uy; // SKSV, SD, BPV, BitPointer
                0xFFuy; 0xEEuy; // FieldPointer
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_SegmentPointer_004() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NO_SENSE,
                ASCCd.OPERATION_IN_PROGRESS,
                // SegmentPointer
                { SD = true; BPV = true; BitPointer = 0x01uy; FieldPointer = 0xFFEEus; }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x00uy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( NO_SENSE )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x00uy; // ADDITIONAL SENSE CODE ( OPERATION_IN_PROGRESS )
                0x16uy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SD, BPV, BitPointer
                0x00uy; 0x00uy; // FieldPointer
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_FieldReplaceableUnit_001() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // FieldReplaceableUnit
                { FieldReplaceableUnitCode = 0xEFuy; }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x04uy; // ADDITIONAL SENSE LENGTH

                // FieldReplaceableUnit
                0x03uy; // DESCRIPTOR TYPE ( FieldReplaceableUnit )
                0x02uy; // ADDITIONAL LENGTH
                0x00uy; // Reserved
                0xEFuy; // FieldReplaceableUnitCode
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_FieldReplaceableUnit_002() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // FieldReplaceableUnit
                { FieldReplaceableUnitCode = 0xEFuy; }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Auy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( COPY_ABORTED )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0xEFuy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SD, BPV, BitPointer
                0x00uy; 0x00uy; // FieldPointer
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_VendorSpecific_001() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // VendorSpecific
                { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 0x01uy; 0x02uy; |] }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x04uy; // ADDITIONAL SENSE LENGTH

                // VendorSpecific
                0x80uy; // DESCRIPTOR TYPE ( VendorSpecificSenseDataDescType.TEXT_MESSAGE )
                0x02uy; // ADDITIONAL LENGTH
                0x01uy; 0x02uy; // VendorSpecific
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_VendorSpecific_002() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // VendorSpecific
                { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = Array.empty }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x02uy; // ADDITIONAL SENSE LENGTH

                // VendorSpecific
                0x80uy; // DESCRIPTOR TYPE ( VendorSpecificSenseDataDescType.TEXT_MESSAGE )
                0x00uy; // ADDITIONAL LENGTH
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_VendorSpecific_003() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // VendorSpecific
                {
                    DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE;
                    VendorSpecific = [| 0x01uy .. 0xF2uy |]
                }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0xF4uy; // ADDITIONAL SENSE LENGTH

                // FieldReplaceableUnit
                0x80uy; // DESCRIPTOR TYPE ( VendorSpecificSenseDataDescType.TEXT_MESSAGE )
                0xF2uy; // ADDITIONAL LENGTH
                yield! [| 0x01uy .. 0xF2uy |];  // VendorSpecific
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_VendorSpecific_004() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // VendorSpecific
                {
                    DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE;
                    VendorSpecific = [| 0x01uy .. 0xF3uy |]
                }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0xF4uy; // ADDITIONAL SENSE LENGTH

                // FieldReplaceableUnit
                0x80uy; // DESCRIPTOR TYPE ( VendorSpecificSenseDataDescType.TEXT_MESSAGE )
                0xF2uy; // ADDITIONAL LENGTH
                yield! [| 0x01uy .. 0xF2uy |];  // VendorSpecific
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_VendorSpecific_005() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // VendorSpecific
                { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 0x01uy; 0x02uy; |] }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Auy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( COPY_ABORTED )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Cuy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SD, BPV, BitPointer
                0x00uy; 0x00uy; // FieldPointer
                yield! [| 0x01uy; 0x02uy; |] // Additional sense bytes
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )
        Assert.True( ( d.[7] <= 244uy ) )
        Assert.True( ( d.Length <= 252 ) )

    [<Fact>]
    member _.Test_GetSenseData_VendorSpecific_006() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // VendorSpecific
                { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = Array.empty }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Auy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( COPY_ABORTED )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SD, BPV, BitPointer
                0x00uy; 0x00uy; // FieldPointer
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )
        Assert.True( ( d.[7] <= 244uy ) )
        Assert.True( ( d.Length <= 252 ) )

    [<Fact>]
    member _.Test_GetSenseData_VendorSpecific_007() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // VendorSpecific
                { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 0x01uy .. 0xEAuy |] }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Auy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( COPY_ABORTED )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0xF4uy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SD, BPV, BitPointer
                0x00uy; 0x00uy; // FieldPointer
                yield! [| 0x01uy .. 0xEAuy |] // Additional sense bytes
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )
        Assert.True( ( d.[7] <= 244uy ) )
        Assert.True( ( d.Length <= 252 ) )

    [<Fact>]
    member _.Test_GetSenseData_VendorSpecific_008() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // VendorSpecific
                { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 0x01uy .. 0xEBuy |] }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x0Auy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( COPY_ABORTED )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0xF4uy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SD, BPV, BitPointer
                0x00uy; 0x00uy; // FieldPointer
                yield! [| 0x01uy .. 0xEAuy |] // Additional sense bytes
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )
        Assert.True( ( d.[7] <= 244uy ) )
        Assert.True( ( d.Length <= 252 ) )

    [<Fact>]
    member _.Test_GetSenseData_BlockCommand_001() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // BlockCommand
                { ILI = true; }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x04uy; // ADDITIONAL SENSE LENGTH

                // BloclCommand
                0x05uy; // DESCRIPTOR TYPE ( BlockCommand )
                0x02uy; // ADDITIONAL LENGTH
                0x00uy; // Reserved
                0x20uy; // ILI
            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_BlockCommand_008() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // BlockCommand
                { ILI = true; }
            )
        let exp =
            [|
                0x70uy; // VALID,RESPONSE CODE ( current )
                0x00uy; // Obsolete
                0x2Auy; // FILEMARK, EOM, ILI, Reserved, SENSE KEY ( COPY_ABORTED )
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x0Auy; // ADDITIONAL SENSE LENGTH
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; // FIELD REPLACEABLE UNIT CODE
                0x00uy; // SKSV, SD, BPV, BitPointer
                0x00uy; 0x00uy; // FieldPointer
            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_All_001() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_Information
                ValueSome { Information = [| 1uy .. 8uy |] },
                // m_CommandSpecific
                ValueSome { CommandSpecific = [| 2uy .. 9uy |] },
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.SegmentPointer(
                    { SD = true; BPV = false; BitPointer = 1uy; FieldPointer = 0xFFEEus; }
                ) ),
                // m_FieldReplaceableUnit
                ValueSome { FieldReplaceableUnitCode = 0xEFuy },
                // m_VendorSpecific
                ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 0x01uy; 0x02uy; |] },
                // m_BlockCommand
                ValueSome { ILI = true }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x0Auy; // SENSE KEY ( COPY_ABORTED )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0x2Cuy; // ADDITIONAL SENSE LENGTH

                // Information
                0x00uy; // DESCRIPTOR TYPE ( Information )
                0x0Auy; // ADDITIONAL LENGTH
                0x80uy; // VALID, Reserved
                0x00uy; // Reserved
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // INFORMATION
                0x05uy; 0x06uy; 0x07uy; 0x08uy;

                // CommandSpecific
                0x01uy; // DESCRIPTOR TYPE ( CommandSpecific )
                0x0Auy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0x02uy; 0x03uy; 0x04uy; 0x05uy; //  COMMAND-SPECIFIC INFORMATION
                0x06uy; 0x07uy; 0x08uy; 0x09uy;

                // SenseKeySpecific
                0x02uy; // DESCRIPTOR TYPE ( SenseKeySpecific )
                0x06uy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0xA1uy; // SKSV, SD, BPV, BitPointer
                0xFFuy; 0xEEuy; // SegmentPointer
                0x00uy; // Reserved

                // FieldReplaceableUnit
                0x03uy; // DESCRIPTOR TYPE ( FieldReplaceableUnit )
                0x02uy; // ADDITIONAL LENGTH
                0x00uy; // Reserved
                0xEFuy; // FieldReplaceableUnitCode

                // BloclCommand
                0x05uy; // DESCRIPTOR TYPE ( BlockCommand )
                0x02uy; // ADDITIONAL LENGTH
                0x00uy; // Reserved
                0x20uy; // ILI

                // VendorSpecific
                0x80uy; // DESCRIPTOR TYPE ( VendorSpecificSenseDataDescType.TEXT_MESSAGE )
                0x02uy; // ADDITIONAL LENGTH
                0x01uy; 0x02uy; // VendorSpecific

            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_All_002() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NO_SENSE,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_Information
                ValueSome { Information = [| 1uy .. 9uy |] },
                // m_CommandSpecific
                ValueSome { CommandSpecific = [| 2uy .. 0xAuy |] },
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ProgressIndication(
                    { ProgressIndication = 0xAABBus; }
                ) ),
                // m_FieldReplaceableUnit
                ValueSome { FieldReplaceableUnitCode = 0xEFuy },
                // m_VendorSpecific
                ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 202uy |] },
                // m_BlockCommand
                ValueSome { ILI = true }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x00uy; // SENSE KEY ( NO_SENSE )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0xF4uy; // ADDITIONAL SENSE LENGTH

                // Information
                0x00uy; // DESCRIPTOR TYPE ( Information )
                0x0Auy; // ADDITIONAL LENGTH
                0x80uy; // VALID, Reserved
                0x00uy; // Reserved
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // INFORMATION
                0x05uy; 0x06uy; 0x07uy; 0x08uy;

                // CommandSpecific
                0x01uy; // DESCRIPTOR TYPE ( CommandSpecific )
                0x0Auy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0x02uy; 0x03uy; 0x04uy; 0x05uy; //  COMMAND-SPECIFIC INFORMATION
                0x06uy; 0x07uy; 0x08uy; 0x09uy;

                // SenseKeySpecific
                0x02uy; // DESCRIPTOR TYPE ( SenseKeySpecific )
                0x06uy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0x80uy; // SKSV
                0xAAuy; 0xBBuy; // ProgressIndication
                0x00uy; // Reserved

                // FieldReplaceableUnit
                0x03uy; // DESCRIPTOR TYPE ( FieldReplaceableUnit )
                0x02uy; // ADDITIONAL LENGTH
                0x00uy; // Reserved
                0xEFuy; // FieldReplaceableUnitCode

                // BloclCommand
                0x05uy; // DESCRIPTOR TYPE ( BlockCommand )
                0x02uy; // ADDITIONAL LENGTH
                0x00uy; // Reserved
                0x20uy; // ILI

                // VendorSpecific
                0x80uy; // DESCRIPTOR TYPE ( VendorSpecificSenseDataDescType.TEXT_MESSAGE )
                0xCAuy; // ADDITIONAL LENGTH
                yield! [| 1uy .. 202uy |];   // VendorSpecific

            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_All_003() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.HARDWARE_ERROR,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_Information
                ValueSome { Information = Array.empty },
                // m_CommandSpecific
                ValueSome { CommandSpecific = Array.empty },
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ActualRetryCount(
                    { ActualRetryCount = 0xACBDus; }
                ) ),
                // m_FieldReplaceableUnit
                ValueSome { FieldReplaceableUnitCode = 0xEFuy },
                // m_VendorSpecific
                ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 203uy |] },
                // m_BlockCommand
                ValueSome { ILI = true }
            )
        let exp =
            [|
                0x72uy; // RESPONSE CODE ( current )
                0x04uy; // SENSE KEY ( HARDWARE_ERROR )
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0x00uy; 0x00uy; 0x00uy; // Reserved
                0xF4uy; // ADDITIONAL SENSE LENGTH

                // Information
                0x00uy; // DESCRIPTOR TYPE ( Information )
                0x0Auy; // ADDITIONAL LENGTH
                0x80uy; // VALID, Reserved
                0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; // INFORMATION
                0x00uy; 0x00uy; 0x00uy; 0x00uy;

                // CommandSpecific
                0x01uy; // DESCRIPTOR TYPE ( CommandSpecific )
                0x0Auy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0x00uy; 0x00uy; 0x00uy; 0x00uy; //  COMMAND-SPECIFIC INFORMATION
                0x00uy; 0x00uy; 0x00uy; 0x00uy;

                // SenseKeySpecific
                0x02uy; // DESCRIPTOR TYPE ( SenseKeySpecific )
                0x06uy; // ADDITIONAL LENGTH
                0x00uy; 0x00uy; // Reserved
                0x80uy; // SKSV
                0xACuy; 0xBDuy; // ActualRetryCount
                0x00uy; // Reserved

                // FieldReplaceableUnit
                0x03uy; // DESCRIPTOR TYPE ( FieldReplaceableUnit )
                0x02uy; // ADDITIONAL LENGTH
                0x00uy; // Reserved
                0xEFuy; // FieldReplaceableUnitCode

                // BloclCommand
                0x05uy; // DESCRIPTOR TYPE ( BlockCommand )
                0x02uy; // ADDITIONAL LENGTH
                0x00uy; // Reserved
                0x20uy; // ILI

                // VendorSpecific
                0x80uy; // DESCRIPTOR TYPE ( VendorSpecificSenseDataDescType.TEXT_MESSAGE )
                0xCAuy; // ADDITIONAL LENGTH
                yield! [| 1uy .. 202uy |];   // VendorSpecific

            |]
        let d = sd.GetSenseData( true )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_All_004() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.COPY_ABORTED,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_Information
                ValueSome { Information = [| 1uy .. 4uy |] },
                // m_CommandSpecific
                ValueSome { CommandSpecific = [| 2uy .. 5uy |] },
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.SegmentPointer(
                    { SD = true; BPV = false; BitPointer = 1uy; FieldPointer = 0xFFEEus; }
                ) ),
                // m_FieldReplaceableUnit
                ValueSome { FieldReplaceableUnitCode = 0xEFuy },
                // m_VendorSpecific
                ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 0x01uy; 0x02uy; |] },
                // m_BlockCommand
                ValueSome { ILI = true }
            )
        let exp =
            [|
                0xF0uy; // VALID, RESPONSE CODE ( current )
                0x00uy; // Obsolute
                0x2Auy; // FILEMARK, EOM, ILI, SENSE KEY ( COPY_ABORTED )
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // INFORMATION
                0x0Cuy; // ADDITIONAL SENSE LENGTH
                0x02uy; 0x03uy; 0x04uy; 0x05uy; //  COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0xEFuy; // FieldReplaceableUnitCode
                0xA1uy; // SKSV, SD, BPV, BitPointer
                0xFFuy; 0xEEuy; // SegmentPointer
                0x01uy; 0x02uy; // VendorSpecific

            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_All_005() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NO_SENSE,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_Information
                ValueSome { Information = [| 1uy .. 4uy |] },
                // m_CommandSpecific
                ValueSome { CommandSpecific = [| 2uy .. 5uy |] },
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ProgressIndication(
                    { ProgressIndication = 0xAABBus; }
                ) ),
                // m_FieldReplaceableUnit
                ValueSome { FieldReplaceableUnitCode = 0xEFuy },
                // m_VendorSpecific
                ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 234uy |] },
                // m_BlockCommand
                ValueSome { ILI = true }
            )
        let exp =
            [|
                0xF0uy; // VALID, RESPONSE CODE ( current )
                0x00uy; // Obsolute
                0x20uy; // FILEMARK, EOM, ILI, SENSE KEY ( NO_SENSE )
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // INFORMATION
                0xF4uy; // ADDITIONAL SENSE LENGTH
                0x02uy; 0x03uy; 0x04uy; 0x05uy; //  COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0xEFuy; // FieldReplaceableUnitCode
                0x80uy; // SKSV
                0xAAuy; 0xBBuy; // ProgressIndication
                yield! [| 1uy .. 234uy |] // VendorSpecific

            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Test_GetSenseData_All_006() =
        let sd =
            new SenseData(
                true,
                SenseKeyCd.NO_SENSE,
                ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
                // m_Information
                ValueSome { Information = [| 1uy .. 4uy |] },
                // m_CommandSpecific
                ValueSome { CommandSpecific = [| 2uy .. 5uy |] },
                // m_SenseKeySpecific
                ValueSome( senseKeySpecificSenseDataDesc.ProgressIndication(
                    { ProgressIndication = 0xAABBus; }
                ) ),
                // m_FieldReplaceableUnit
                ValueSome { FieldReplaceableUnitCode = 0xEFuy },
                // m_VendorSpecific
                ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 235uy |] },
                // m_BlockCommand
                ValueSome { ILI = true }
            )
        let exp =
            [|
                0xF0uy; // VALID, RESPONSE CODE ( current )
                0x00uy; // Obsolute
                0x20uy; // FILEMARK, EOM, ILI, SENSE KEY ( NO_SENSE )
                0x01uy; 0x02uy; 0x03uy; 0x04uy; // INFORMATION
                0xF4uy; // ADDITIONAL SENSE LENGTH
                0x02uy; 0x03uy; 0x04uy; 0x05uy; //  COMMAND-SPECIFIC INFORMATION
                0x20uy; // ADDITIONAL SENSE CODE ( ACCESS_DENIED_ACL_LUN_CONFLICT )
                0x0Buy; // ADDITIONAL SENSE CODE QUALIFIER
                0xEFuy; // FieldReplaceableUnitCode
                0x80uy; // SKSV
                0xAAuy; 0xBBuy; // ProgressIndication
                yield! [| 1uy .. 234uy |] // VendorSpecific

            |]
        let d = sd.GetSenseData( false )
        Assert.True( ( d = exp ) )

    [<Fact>]
    member _.Parse_Desc_001() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.ILLEGAL_REQUEST,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueSome { Information = [| 1uy .. 4uy |] },
            // m_CommandSpecific
            ValueSome { CommandSpecific = [| 2uy .. 5uy |] },
            // m_SenseKeySpecific
            ValueSome( senseKeySpecificSenseDataDesc.FieldPointer(
                { CommandData = true; BPV = true; BitPointer = 1uy; FieldPointer = 2us }
            ) ),
            // m_FieldReplaceableUnit
            ValueSome { FieldReplaceableUnitCode = 0xEFuy },
            // m_VendorSpecific
            ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 255uy |] },
            // m_BlockCommand
            ValueSome { ILI = true }
        )
        let v = sd1.GetSenseData true
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isSome ))
        Assert.True(( sd2.Information |> ValueOption.get |> _.Information = [| 0uy; 0uy; 0uy; 0uy; 1uy; 2uy; 3uy; 4uy |] ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 0uy; 0uy; 0uy; 0uy; 2uy; 3uy; 4uy; 5uy |] ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isSome ))
        Assert.True(( sd2.FieldPointer |> ValueOption.get |> _.CommandData ))
        Assert.True(( sd2.FieldPointer |> ValueOption.get |> _.BPV ))
        Assert.True(( sd2.FieldPointer |> ValueOption.get |> _.BitPointer = 1uy ))
        Assert.True(( sd2.FieldPointer |> ValueOption.get |> _.FieldPointer = 2us ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isSome ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 0xEFuy ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.VendorSpecific = [| 1uy .. 202uy |] ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isSome ))
        Assert.True(( sd2.BlockCommand |> ValueOption.get |> _.ILI ))

    [<Fact>]
    member _.Parse_Desc_002() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.HARDWARE_ERROR,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueSome { Information = [| 1uy .. 8uy |] },
            // m_CommandSpecific
            ValueSome { CommandSpecific = [| 2uy .. 9uy |] },
            // m_SenseKeySpecific
            ValueSome( senseKeySpecificSenseDataDesc.ActualRetryCount(
                { ActualRetryCount = 8us; }
            ) ),
            // m_FieldReplaceableUnit
            ValueSome { FieldReplaceableUnitCode = 0xEFuy },
            // m_VendorSpecific
            ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 201uy |] },
            // m_BlockCommand
            ValueSome { ILI = true }
        )
        let v = sd1.GetSenseData true
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.HARDWARE_ERROR ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isSome ))
        Assert.True(( sd2.Information |> ValueOption.get |> _.Information = [| 1uy .. 8uy |] ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 2uy .. 9uy |] ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isSome ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.get |> _.ActualRetryCount = 8us ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isSome ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 0xEFuy ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.VendorSpecific = [| 1uy .. 201uy |] ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isSome ))
        Assert.True(( sd2.BlockCommand |> ValueOption.get |> _.ILI ))

    [<Fact>]
    member _.Parse_Desc_003() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.NO_SENSE,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueSome { Information = [| 1uy .. 10uy |] },
            // m_CommandSpecific
            ValueSome { CommandSpecific = [| 2uy .. 11uy |] },
            // m_SenseKeySpecific
            ValueSome( senseKeySpecificSenseDataDesc.ProgressIndication(
                { ProgressIndication = 0xAABBus; }
            ) ),
            // m_FieldReplaceableUnit
            ValueSome { FieldReplaceableUnitCode = 0xEFuy },
            // m_VendorSpecific
            ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 202uy |] },
            // m_BlockCommand
            ValueSome { ILI = true }
        )
        let v = sd1.GetSenseData true
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.NO_SENSE ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isSome ))
        Assert.True(( sd2.Information |> ValueOption.get |> _.Information = [| 1uy .. 8uy |] ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 2uy .. 9uy |] ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isSome ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.get |> _.ProgressIndication = 0xAABBus ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isSome ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 0xEFuy ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.VendorSpecific = [| 1uy .. 202uy |] ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isSome ))
        Assert.True(( sd2.BlockCommand |> ValueOption.get |> _.ILI ))

    [<Fact>]
    member _.Parse_Desc_004() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.COPY_ABORTED,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueSome { Information = [| 1uy .. 4uy |] },
            // m_CommandSpecific
            ValueSome { CommandSpecific = [| 2uy .. 5uy |] },
            // m_SenseKeySpecific
            ValueSome( senseKeySpecificSenseDataDesc.SegmentPointer(
                { SD = true; BPV = true; BitPointer = 6uy; FieldPointer = 7us; }
            ) ),
            // m_FieldReplaceableUnit
            ValueSome { FieldReplaceableUnitCode = 0xEFuy },
            // m_VendorSpecific
            ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 255uy |] },
            // m_BlockCommand
            ValueSome { ILI = true }
        )
        let v = sd1.GetSenseData true
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isSome ))
        Assert.True(( sd2.Information |> ValueOption.get |> _.Information = [| 0uy; 0uy; 0uy; 0uy; 1uy; 2uy; 3uy; 4uy |] ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 0uy; 0uy; 0uy; 0uy; 2uy; 3uy; 4uy; 5uy |] ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isSome ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.get |> _.SD ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.get |> _.BPV ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.get |> _.BitPointer = 6uy ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.get |> _.FieldPointer = 7us ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isSome ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 0xEFuy ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.VendorSpecific = [| 1uy .. 202uy |] ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isSome ))
        Assert.True(( sd2.BlockCommand |> ValueOption.get |> _.ILI ))

    [<Fact>]
    member _.Parse_Desc_005() =
        let sd1 = new SenseData(
            false,
            SenseKeyCd.MISCOMPARE,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueNone,
            // m_CommandSpecific
            ValueNone,
            // m_SenseKeySpecific
            ValueNone,
            // m_FieldReplaceableUnit
            ValueNone,
            // m_VendorSpecific
            ValueNone,
            // m_BlockCommand
            ValueNone
        )
        let v = sd1.GetSenseData true
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.False(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.MISCOMPARE ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isNone ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isNone ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isNone ))

    [<Fact>]
    member _.Parse_Desc_006() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.COPY_ABORTED,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueSome { Information = [| 1uy .. 4uy |] },
            // m_CommandSpecific
            ValueNone,
            // m_SenseKeySpecific
            ValueNone,
            // m_FieldReplaceableUnit
            ValueNone,
            // m_VendorSpecific
            ValueNone,
            // m_BlockCommand
            ValueNone
        )
        let v = sd1.GetSenseData true
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isSome ))
        Assert.True(( sd2.Information |> ValueOption.get |> _.Information = [| 0uy; 0uy; 0uy; 0uy; 1uy; 2uy; 3uy; 4uy |] ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isNone ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isNone ))

    [<Fact>]
    member _.Parse_Desc_007() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.COPY_ABORTED,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueNone,
            // m_CommandSpecific
            ValueSome { CommandSpecific = [| 2uy .. 5uy |] },
            // m_SenseKeySpecific
            ValueNone,
            // m_FieldReplaceableUnit
            ValueNone,
            // m_VendorSpecific
            ValueNone,
            // m_BlockCommand
            ValueNone
        )
        let v = sd1.GetSenseData true
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isNone ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 0uy; 0uy; 0uy; 0uy; 2uy; 3uy; 4uy; 5uy |] ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isNone ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isNone ))

    [<Fact>]
    member _.Parse_Desc_008() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.COPY_ABORTED,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueNone,
            // m_CommandSpecific
            ValueNone,
            // m_SenseKeySpecific
            ValueSome( senseKeySpecificSenseDataDesc.SegmentPointer(
                { SD = true; BPV = true; BitPointer = 6uy; FieldPointer = 7us; }
            ) ),
            // m_FieldReplaceableUnit
            ValueNone,
            // m_VendorSpecific
            ValueNone,
            // m_BlockCommand
            ValueNone
        )
        let v = sd1.GetSenseData true
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isNone ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isSome ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.get |> _.SD ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.get |> _.BPV ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.get |> _.BitPointer = 6uy ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.get |> _.FieldPointer = 7us ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isNone ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isNone ))

    [<Fact>]
    member _.Parse_Desc_009() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.COPY_ABORTED,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueNone,
            // m_CommandSpecific
            ValueNone,
            // m_SenseKeySpecific
            ValueNone,
            // m_FieldReplaceableUnit
            ValueSome { FieldReplaceableUnitCode = 0xEFuy },
            // m_VendorSpecific
            ValueNone,
            // m_BlockCommand
            ValueNone
        )
        let v = sd1.GetSenseData true
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isNone ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isSome ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 0xEFuy ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isNone ))

    [<Fact>]
    member _.Parse_Desc_010() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.COPY_ABORTED,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueNone,
            // m_CommandSpecific
            ValueNone,
            // m_SenseKeySpecific
            ValueNone,
            // m_FieldReplaceableUnit
            ValueNone,
            // m_VendorSpecific
            ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 255uy |] },
            // m_BlockCommand
            ValueNone
        )
        let v = sd1.GetSenseData true
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isNone ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isNone ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.VendorSpecific = [| 1uy .. 242uy |] ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isNone ))

    [<Fact>]
    member _.Parse_Desc_011() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.COPY_ABORTED,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueNone,
            // m_CommandSpecific
            ValueNone,
            // m_SenseKeySpecific
            ValueNone,
            // m_FieldReplaceableUnit
            ValueNone,
            // m_VendorSpecific
            ValueNone,
            // m_BlockCommand
            ValueSome { ILI = true }
        )
        let v = sd1.GetSenseData true
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isNone ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isNone ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isSome ))
        Assert.True(( sd2.BlockCommand |> ValueOption.get |> _.ILI ))

    [<Fact>]
    member _.Parse_Fixed_001() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.ILLEGAL_REQUEST,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueSome { Information = [| 1uy .. 4uy |] },
            // m_CommandSpecific
            ValueSome { CommandSpecific = [| 2uy .. 5uy |] },
            // m_SenseKeySpecific
            ValueSome( senseKeySpecificSenseDataDesc.FieldPointer(
                { CommandData = true; BPV = true; BitPointer = 1uy; FieldPointer = 2us }
            ) ),
            // m_FieldReplaceableUnit
            ValueSome { FieldReplaceableUnitCode = 0xEFuy },
            // m_VendorSpecific
            ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 255uy |] },
            // m_BlockCommand
            ValueSome { ILI = true }
        )
        let v = sd1.GetSenseData false
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.ILLEGAL_REQUEST ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isSome ))
        Assert.True(( sd2.Information |> ValueOption.get |> _.Information = [| 1uy .. 4uy |] ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 2uy .. 5uy |] ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isSome ))
        Assert.True(( sd2.FieldPointer |> ValueOption.get |> _.CommandData ))
        Assert.True(( sd2.FieldPointer |> ValueOption.get |> _.BPV ))
        Assert.True(( sd2.FieldPointer |> ValueOption.get |> _.BitPointer = 1uy ))
        Assert.True(( sd2.FieldPointer |> ValueOption.get |> _.FieldPointer = 2us ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isSome ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 0xEFuy ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.VendorSpecific = [| 1uy .. 234uy |] ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isSome ))
        Assert.True(( sd2.BlockCommand |> ValueOption.get |> _.ILI ))

    [<Fact>]
    member _.Parse_Fixed_002() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.HARDWARE_ERROR,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueSome { Information = [| 1uy; 2uy |] },
            // m_CommandSpecific
            ValueSome { CommandSpecific = [| 2uy; 3uy |] },
            // m_SenseKeySpecific
            ValueSome( senseKeySpecificSenseDataDesc.ActualRetryCount(
                { ActualRetryCount = 8us; }
            ) ),
            // m_FieldReplaceableUnit
            ValueSome { FieldReplaceableUnitCode = 0xEFuy },
            // m_VendorSpecific
            ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 233uy |] },
            // m_BlockCommand
            ValueSome { ILI = true }
        )
        let v = sd1.GetSenseData false
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.HARDWARE_ERROR ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isSome ))
        Assert.True(( sd2.Information |> ValueOption.get |> _.Information = [| 0uy; 0uy; 1uy; 2uy |] ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 0uy; 0uy; 2uy; 3uy |] ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isSome ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.get |> _.ActualRetryCount = 8us ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isSome ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 0xEFuy ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.VendorSpecific = [| 1uy .. 233uy |] ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isSome ))
        Assert.True(( sd2.BlockCommand |> ValueOption.get |> _.ILI ))

    [<Fact>]
    member _.Parse_Fixed_003() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.NO_SENSE,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueSome { Information = [| 1uy .. 5uy |] },
            // m_CommandSpecific
            ValueSome { CommandSpecific = [| 2uy .. 6uy |] },
            // m_SenseKeySpecific
            ValueSome( senseKeySpecificSenseDataDesc.ProgressIndication(
                { ProgressIndication = 0xAABBus; }
            ) ),
            // m_FieldReplaceableUnit
            ValueSome { FieldReplaceableUnitCode = 0xEFuy },
            // m_VendorSpecific
            ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 234uy |] },
            // m_BlockCommand
            ValueSome { ILI = true }
        )
        let v = sd1.GetSenseData false
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.NO_SENSE ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isNone ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 0uy; 0uy; 0uy; 0uy |] ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isSome ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.get |> _.ProgressIndication = 0xAABBus ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isSome ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 0xEFuy ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.VendorSpecific = [| 1uy .. 234uy |] ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isSome ))
        Assert.True(( sd2.BlockCommand |> ValueOption.get |> _.ILI ))

    [<Fact>]
    member _.Parse_Fixed_004() =
        let sd1 = new SenseData(
            true,
            SenseKeyCd.COPY_ABORTED,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueSome { Information = [| 1uy .. 4uy |] },
            // m_CommandSpecific
            ValueSome { CommandSpecific = [| 2uy .. 5uy |] },
            // m_SenseKeySpecific
            ValueSome( senseKeySpecificSenseDataDesc.SegmentPointer(
                { SD = true; BPV = true; BitPointer = 6uy; FieldPointer = 7us; }
            ) ),
            // m_FieldReplaceableUnit
            ValueSome { FieldReplaceableUnitCode = 0xEFuy },
            // m_VendorSpecific
            ValueSome { DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE; VendorSpecific = [| 1uy .. 255uy |] },
            // m_BlockCommand
            ValueSome { ILI = true }
        )
        let v = sd1.GetSenseData false
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.True(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.COPY_ABORTED ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isSome ))
        Assert.True(( sd2.Information |> ValueOption.get |> _.Information = [| 1uy .. 4uy |] ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 2uy .. 5uy |] ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isSome ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.get |> _.SD ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.get |> _.BPV ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.get |> _.BitPointer = 6uy ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.get |> _.FieldPointer = 7us ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isSome ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 0xEFuy ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.get |> _.VendorSpecific = [| 1uy .. 234uy |] ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isSome ))
        Assert.True(( sd2.BlockCommand |> ValueOption.get |> _.ILI ))

    [<Fact>]
    member _.Parse_Fixed_005() =
        let sd1 = new SenseData(
            false,
            SenseKeyCd.MISCOMPARE,
            ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT,
            // m_Information
            ValueNone,
            // m_CommandSpecific
            ValueNone,
            // m_SenseKeySpecific
            ValueNone,
            // m_FieldReplaceableUnit
            ValueNone,
            // m_VendorSpecific
            ValueNone,
            // m_BlockCommand
            ValueNone
        )
        let v = sd1.GetSenseData false
        let sd2_o = ParseSenseData.Parse v
        Assert.True(( sd2_o.IsSome ))
        let sd2 = sd2_o.Value
        Assert.False(( sd2.IsCurrent ))
        Assert.True(( sd2.SenseKey = SenseKeyCd.MISCOMPARE ))
        Assert.True(( sd2.ASC = ASCCd.ACCESS_DENIED_ACL_LUN_CONFLICT ))
        Assert.True(( sd2.Information |> ValueOption.isNone ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.isSome ))
        Assert.True(( sd2.CommandSpecific |> ValueOption.get |> _.CommandSpecific = [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] ))
        Assert.True(( sd2.FieldPointer |> ValueOption.isNone ))
        Assert.True(( sd2.ActualRetryCount |> ValueOption.isNone ))
        Assert.True(( sd2.ProgressIndication |> ValueOption.isNone ))
        Assert.True(( sd2.SegmentPointer |> ValueOption.isNone ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.isSome ))
        Assert.True(( sd2.FieldReplaceableUnit |> ValueOption.get |> _.FieldReplaceableUnitCode = 0uy ))
        Assert.True(( sd2.VendorSpecific |> ValueOption.isNone ))
        Assert.True(( sd2.BlockCommand |> ValueOption.isSome ))
        Assert.False(( sd2.BlockCommand |> ValueOption.get |> _.ILI ))

