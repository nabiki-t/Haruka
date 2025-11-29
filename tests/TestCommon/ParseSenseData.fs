//=============================================================================
// Haruka Software Storage.
// ParseSenseData.fs : Implementing functionality to interpret the sense data byte array.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test

//=============================================================================
// Import declaration

open System
open Haruka.Commons
open Haruka.Constants

//=============================================================================
// Type definition

type ParseSenseData() =

    static member Parse ( sd : byte[] ) : SenseData voption =
        if sd.Length <= 0 then
            ValueNone
        else
            match ( sd.[0] &&& 0x7Fuy ) with
            | 0x70uy -> // Current and Fixed format
                ParseSenseData.ParseFixed sd true
            | 0x71uy -> // Delay and Fixed format
                ParseSenseData.ParseFixed sd false
            | 0x72uy -> // Current and Descriptor format
                ParseSenseData.ParseDescriptor sd true
            | 0x73uy -> // Delay and Descriptor format
                ParseSenseData.ParseDescriptor sd false
            | _ ->      // Unexpected
                ValueNone

    static member private ParseDescriptor ( sd : byte[] ) ( isCurrent : bool ) : SenseData voption =
        if sd.Length < 8 && ( int sd.[7] ) + 8 > sd.Length then
            ValueNone
        else
            let senseKey = Enum.ToObject( typeof<SenseKeyCd>, sd.[1] ) :?> SenseKeyCd
            let additionalSenseCode = Enum.ToObject( typeof<ASCCd>, Functions.NetworkBytesToUInt16 sd 2 ) :?> ASCCd

            let rec loop (
                i : informationSenseDataDesc voption,
                c : commandSpecificSenseDataDesc voption,
                s : senseKeySpecificSenseDataDesc voption,
                f : fieldReplaceableUnitSenseDataDesc voption,
                v : vendorSpecificSenseDataDesc voption,
                b : blockCommandSenseDataDesc voption,
                pos : int
            ) =

                if pos = sd.Length then
                    ValueSome ( i, c, s, f, v, b )
                elif pos + 2 > sd.Length || pos + ( int sd.[pos+1] ) + 2 > sd.Length then
                    ValueNone
                else
                    match sd.[pos] with
                    | 0uy ->
                        match ParseSenseData.Parse_informationSenseDataDesc sd pos with
                        | ValueNone ->
                            ValueNone
                        | ValueSome( x ) ->
                            loop( ValueSome x, c, s, f, v, b, pos + ( int sd.[pos+1] ) + 2 )
                    | 1uy ->
                        match ParseSenseData.Parse_commandSpecificSenseDataDesc sd pos with
                        | ValueNone ->
                            ValueNone
                        | ValueSome( x ) ->
                            loop( i, ValueSome x, s, f, v, b, pos + ( int sd.[pos+1] ) + 2 )
                    | 2uy ->
                        match ParseSenseData.Parse_senseKeySpecificSenseDataDesc sd pos senseKey with
                        | ValueNone ->
                            ValueNone
                        | ValueSome( x ) ->
                            loop( i, c, ValueSome x, f, v, b, pos + ( int sd.[pos+1] ) + 2 )
                    | 3uy ->
                        match ParseSenseData.Parse_fieldReplaceableUnitSenseDataDesc sd pos with
                        | ValueNone ->
                            ValueNone
                        | ValueSome( x ) ->
                            loop( i, c, s, ValueSome x, v, b, pos + ( int sd.[pos+1] ) + 2 )
                    | 5uy ->
                        match ParseSenseData.Parse_blockCommandSenseDataDesc sd pos with
                        | ValueNone ->
                            ValueNone
                        | ValueSome( x ) ->
                            loop( i, c, s, f, v, ValueSome x, pos + ( int sd.[pos+1] ) + 2 )
                    | r when r >= 0x80uy && r <= 0xFFuy ->
                        match ParseSenseData.Parse_vendorSpecificSenseDataDesc sd pos with
                        | ValueNone ->
                            ValueNone
                        | ValueSome( x ) ->
                            loop( i, c, s, f, ValueSome x, b, pos + ( int sd.[pos+1] ) + 2 )
                    | _ ->
                        ValueNone
            let r = loop( ValueNone, ValueNone, ValueNone, ValueNone, ValueNone, ValueNone, 8 )
            if r.IsNone then
                ValueNone
            else
                let ( i, c, s, f, v, b ) = r.Value
                SenseData( isCurrent, senseKey, additionalSenseCode, i, c, s, f, v, b ) |> ValueSome


    static member private Parse_informationSenseDataDesc ( sd : byte[] ) ( pos : int ) : informationSenseDataDesc voption =
        if pos + 12 > sd.Length || sd.[ pos + 0 ] <> 0x00uy || sd.[ pos + 1 ] <> 0x0Auy || sd.[ pos + 2 ] <> 0x80uy then
            ValueNone
        else
            {
                Information = sd.[ pos + 4 .. pos + 11 ]
            }
            |> ValueSome

    static member private Parse_commandSpecificSenseDataDesc ( sd : byte[] ) ( pos : int ) : commandSpecificSenseDataDesc voption =
        if pos + 12 > sd.Length || sd.[ pos + 0 ] <> 0x01uy || sd.[ pos + 1 ] <> 0x0Auy then
            ValueNone
        else
            {
                CommandSpecific = sd.[ pos + 4 .. pos + 11 ]
            }
            |> ValueSome

    static member private Parse_senseKeySpecificSenseDataDesc ( sd : byte[] ) ( pos : int ) ( senseKey : SenseKeyCd ) : senseKeySpecificSenseDataDesc voption =
        if pos + 8 > sd.Length || sd.[ pos + 0 ] <> 0x02uy || sd.[ pos + 1 ] <> 0x06uy || sd.[ pos + 4 ] &&& 0x80uy <> 0x80uy then
            ValueNone
        else
            match senseKey with
            | SenseKeyCd.ILLEGAL_REQUEST ->
                senseKeySpecificSenseDataDesc.FieldPointer ( {
                    CommandData = Functions.CheckBitflag sd.[ pos + 4 ] 0x40uy;
                    BPV = Functions.CheckBitflag sd.[ pos + 4 ] 0x08uy;
                    BitPointer = sd.[ pos + 4 ] &&& 0x07uy;
                    FieldPointer = Functions.NetworkBytesToUInt16 sd ( pos + 5 );
                } ) |> ValueSome
            | SenseKeyCd.RECOVERED_ERROR
            | SenseKeyCd.MEDIUM_ERROR
            | SenseKeyCd.HARDWARE_ERROR ->
                senseKeySpecificSenseDataDesc.ActualRetryCount ( {
                    ActualRetryCount = Functions.NetworkBytesToUInt16 sd ( pos + 5 );
                } ) |> ValueSome
            | SenseKeyCd.NO_SENSE
            | SenseKeyCd.NOT_READY ->
                senseKeySpecificSenseDataDesc.ProgressIndication ( {
                    ProgressIndication = Functions.NetworkBytesToUInt16 sd ( pos + 5 );
                } ) |> ValueSome
            | SenseKeyCd.COPY_ABORTED ->
                senseKeySpecificSenseDataDesc.SegmentPointer ( {
                    SD = Functions.CheckBitflag sd.[ pos + 4 ] 0x20uy;
                    BPV = Functions.CheckBitflag sd.[ pos + 4 ] 0x08uy;
                    BitPointer = sd.[ pos + 4 ] &&& 0x07uy;
                    FieldPointer = Functions.NetworkBytesToUInt16 sd ( pos + 5 );
                } ) |> ValueSome
            | _ ->
                ValueNone

    static member private Parse_fieldReplaceableUnitSenseDataDesc ( sd : byte[] ) ( pos : int ) : fieldReplaceableUnitSenseDataDesc voption =
        if pos + 4 > sd.Length || sd.[ pos + 0 ] <> 0x03uy || sd.[ pos + 1 ] <> 0x02uy then
            ValueNone
        else
            {
                FieldReplaceableUnitCode = sd.[ pos + 3];
            }
            |> ValueSome

    static member private Parse_blockCommandSenseDataDesc ( sd : byte[] ) ( pos : int ) : blockCommandSenseDataDesc voption =
        if pos + 4 > sd.Length || sd.[ pos + 0 ] <> 0x05uy || sd.[ pos + 1 ] <> 0x02uy then
            ValueNone
        else
            {
                ILI = Functions.CheckBitflag sd.[ pos + 3 ] 0x20uy;
            }
            |> ValueSome

    static member private Parse_vendorSpecificSenseDataDesc ( sd : byte[] ) ( pos : int ) : vendorSpecificSenseDataDesc voption =
        if pos + 2 > sd.Length || sd.[ pos ] &&& 0x80uy <> 0x80uy || pos + ( int sd.[ pos + 1 ] ) + 2 > sd.Length then
            ValueNone
        else
            {
                DescriptorType = Enum.ToObject( typeof<VendorSpecificSenseDataDescType>, sd.[ pos ] ) :?> VendorSpecificSenseDataDescType;
                VendorSpecific = sd.[ pos + 2 .. pos + ( int sd.[ pos + 1 ] ) + 1 ];
            }
            |> ValueSome

    static member private ParseFixed ( sd : byte[] ) ( isCurrent : bool ) : SenseData voption =
        if sd.Length < 18 || ( int sd.[7] ) + 8 > sd.Length then
            ValueNone
        else
            let senseKey = Enum.ToObject( typeof<SenseKeyCd>, sd.[2] &&& 0x0Fuy ) :?> SenseKeyCd
            let additionalSenseCode = Enum.ToObject( typeof<ASCCd>, Functions.NetworkBytesToUInt16 sd 12 ) :?> ASCCd

            let b =
                {
                    ILI = Functions.CheckBitflag sd.[2] 0x20uy;
                }
                |> ValueSome
            let i =
                if Functions.CheckBitflag sd.[0] 0x80uy then
                    {
                        Information = sd.[ 3 .. 6 ]
                    }
                    |> ValueSome
                else
                    ValueNone

            let c =
                {
                    CommandSpecific = sd.[ 8 .. 11 ];
                }
                |> ValueSome
            let f =
                {
                    FieldReplaceableUnitCode = sd.[14];
                }
                |> ValueSome
            let s =
                if Functions.CheckBitflag sd.[15] 0x80uy then
                    match senseKey with
                    | SenseKeyCd.ILLEGAL_REQUEST ->
                        senseKeySpecificSenseDataDesc.FieldPointer( {
                                CommandData = Functions.CheckBitflag sd.[15] 0x40uy;
                                BPV = Functions.CheckBitflag sd.[15] 0x08uy;
                                BitPointer = sd.[15] &&& 0x07uy;
                                FieldPointer = Functions.NetworkBytesToUInt16 sd 16;
                        } ) |> ValueSome
                    | SenseKeyCd.RECOVERED_ERROR
                    | SenseKeyCd.MEDIUM_ERROR
                    | SenseKeyCd.HARDWARE_ERROR ->
                        senseKeySpecificSenseDataDesc.ActualRetryCount( {
                                ActualRetryCount = Functions.NetworkBytesToUInt16 sd 16;
                        } ) |> ValueSome
                    | SenseKeyCd.NO_SENSE
                    | SenseKeyCd.NOT_READY ->
                        senseKeySpecificSenseDataDesc.ProgressIndication( {
                                ProgressIndication = Functions.NetworkBytesToUInt16 sd 16;
                        } ) |> ValueSome
                    | SenseKeyCd.COPY_ABORTED ->
                        senseKeySpecificSenseDataDesc.SegmentPointer( {
                                SD = Functions.CheckBitflag sd.[15] 0x20uy;
                                BPV = Functions.CheckBitflag sd.[15] 0x08uy;
                                BitPointer = sd.[15] &&& 0x07uy;
                                FieldPointer = Functions.NetworkBytesToUInt16 sd 16;
                        } ) |> ValueSome
                    | _ ->
                        ValueNone
                else
                    ValueNone
            let v = 
                if sd.Length > 18 then
                    {
                        DescriptorType = VendorSpecificSenseDataDescType.TEXT_MESSAGE;
                        VendorSpecific = sd.[ 18 .. ];
                    }
                    |> ValueSome
                else
                    ValueNone
            SenseData( isCurrent, senseKey, additionalSenseCode, i, c, s, f, v, b )
            |> ValueSome
