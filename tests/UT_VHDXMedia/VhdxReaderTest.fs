//=============================================================================
// Haruka Software Storage.
// VhdxReaderTest.fs : Test cases for VhdxReader class.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.VHDXMedia

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Text
open System.Net

open Xunit

open Haruka.Constants
open Haruka.Commons
open Haruka.Media.VhdxUtil
open Haruka.Test


//=============================================================================
// Type definitions

type TestLogDesc =
    | Zero of ( uint64 * uint64 )   // ZeroLength and FileOffset
    | Data of byte[] * uint64       // byte data and FileOffset. The byte data must be 4KB length.

type TestLogEntry = {
    PatchPosition : int;
    PatchData : byte[];
    Descriptor : TestLogDesc[];
}

//=============================================================================
// Class implementation

type VhdxReaderTest_Test () =

    let logEntryHeader
        ( entryLength : uint32 )
        ( tail : uint32 )
        ( sequenceNumber : uint64 )
        ( descriptorCount : uint32 )
        ( logGuid : Guid )
        ( flushedFileOffset : uint64 )
        ( lastFileOffset : uint64 ) =
        [|
            yield! ( "loge" |> Encoding.UTF8.GetBytes )     // ZeroSignature
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                 // checksum
            yield! BitConverter.GetBytes entryLength        // EntryLength
            yield! BitConverter.GetBytes tail               // tail
            yield! BitConverter.GetBytes sequenceNumber     // SequenceNumber
            yield! BitConverter.GetBytes descriptorCount    // DescriptorCount
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                 // Reserved
            yield! logGuid.ToByteArray()                    // LogGuid
            yield! BitConverter.GetBytes flushedFileOffset  // FlushedFileOffset
            yield! BitConverter.GetBytes lastFileOffset     // lastFileOffset
        |]

    let zeroDiscriptor
        ( zeroLength : uint64 )
        ( fileOffset : uint64 )
        ( sequenceNumber : uint64 ) =
        [|
            yield! ( "zero" |> Encoding.UTF8.GetBytes )     // Signature
            0x00uy; 0x00uy; 0x00uy; 0x00uy;                 // Reserved
            yield! BitConverter.GetBytes zeroLength         // ZeroLength
            yield! BitConverter.GetBytes fileOffset         // FileOffset
            yield! BitConverter.GetBytes sequenceNumber     // SequenceNumber
        |]

    let dataDiscriptor
        ( trailingBytes : byte[] )
        ( leadingBytes : byte[] )
        ( fileOffset : uint64 )
        ( sequenceNumber : uint64 ) =
        [|
            yield! ( "desc" |> Encoding.UTF8.GetBytes )     // DataSignature
            yield! trailingBytes                            // TrailingBytes
            yield! leadingBytes                             // LeadingBytes
            yield! BitConverter.GetBytes fileOffset         // FileOffset
            yield! BitConverter.GetBytes sequenceNumber     // SequenceNumber
        |]

    let genLogData
        ( logLength : int32 )
        ( startPos : int32 )    // 4KB unit
        ( logEntries : TestLogEntry[] )
        ( logGuid : Guid )
        ( sequenceNumber : uint64 )
        ( flushedFileOffset : uint64 )
        ( lastFileOffset : uint64 ) =

        let v = [|
            for i = 0 to logEntries.Length - 1 do
                let logents = logEntries.[i].Descriptor
                let entryBytes = [|
                    let dataCount = Array.fold ( fun cnt j -> cnt + ( match j with | Data _ -> 1 | _ -> 0 ) ) 0 logents
                    let headerlength = Functions.AddPaddingLengthInt32 ( 64 + logents.Length * 32 ) 4096
                    let entryLength = headerlength + dataCount * 4096
                    let effSN = ( sequenceNumber + uint64 i )
                    // log entry header
                    yield! logEntryHeader ( uint32 entryLength ) ( uint32 startPos ) effSN ( uint32 logents.Length ) logGuid flushedFileOffset lastFileOffset
                    // descriptor
                    for itr2 in logents do
                        match itr2 with
                        | Zero( x, y ) ->
                            yield! zeroDiscriptor x y effSN
                        | Data( x, y ) ->
                            let trailingBytes = if x.Length > 0 then x.[ 4092 .. 4095 ] else Array.zeroCreate<byte> 4
                            let leadingBytes = if x.Length > 0 then x.[ 0 .. 7 ] else Array.zeroCreate<byte> 8
                            yield! dataDiscriptor trailingBytes leadingBytes y effSN
                    // padding
                    let padlength = headerlength - ( 64 + logents.Length * 32 )
                    yield! Array.zeroCreate<byte> padlength
                    // data sector
                    for itr2 in logents do
                        match itr2 with
                        | Data( x, y ) ->
                            yield! ( "data" |> Encoding.UTF8.GetBytes )     // DataSignature
                            yield! BitConverter.GetBytes ( uint32 ( effSN >>> 32 ) )   // SequenceHigh
                            yield! if x.Length > 0 then x.[ 8 .. 4091 ] else Array.zeroCreate<byte> 4084
                            yield! BitConverter.GetBytes ( uint32 effSN )   // SequenceLow
                        | _ ->
                            ()
                |]
                Array.blit logEntries.[i].PatchData 0 entryBytes logEntries.[i].PatchPosition logEntries.[i].PatchData.Length
                let crc = Crc32C.Compute entryBytes
                ByteFunc.WriteU32LE entryBytes 4u crc
                yield! entryBytes
        |]
        let logBuffer = Array.zeroCreate<byte> logLength
        let bufferOffset = startPos % logLength
        if bufferOffset + v.Length <= logLength then
            Array.blit v 0 logBuffer bufferOffset v.Length
        else
            let firstChunkSize = logLength - bufferOffset
            let secondChunkSize = v.Length - firstChunkSize
            Array.blit v 0 logBuffer bufferOffset firstChunkSize
            Array.blit v firstChunkSize logBuffer 0 secondChunkSize
        logBuffer

    let genLogEntry ( descriptor : TestLogDesc[] ) =
        let ddindex =
            descriptor
            |> Array.mapFold ( fun s itr ->
                if itr.IsData then
                    ( s, s + 1u )
                else
                    ( 0u, s )
            ) 0u
            |> fst
        {
            Signature = 0u;
            Checksum = 0u;
            EntryLength = 0u;
            Tail = 0u;
            SequenceNumber = 0UL;
            DescriptorCount = 1u;
            LogGuid = Guid();
            FlushedFileOffset = 0UL;
            LastFileOffset = 0UL;
            Descriptors = [
                for i = 0 to descriptor.Length - 1 do
                    match descriptor.[i] with
                    | Zero( x, y ) ->
                        LogDescriptor.Zero({
                            ZeroSignature = 0u;
                            ZeroLength = x;
                            FileOffset = y;
                            SequenceNumber = 0UL;
                        });
                    | Data( x, y ) ->
                        LogDescriptor.Data({
                            DataSignature = 0u;
                            TrailingBytes = x.[ 4092 .. 4095 ];
                            LeadingBytes = x.[ 0 .. 7 ]
                            FileOffset = y;
                            SequenceNumber = 0UL;
                            ddIndex = ddindex.[i];
                        });
            ]
            DataSectors = [
                for i = 0 to descriptor.Length - 1 do
                    match descriptor.[i] with
                    | Data( x, _ ) ->
                        yield x.[ 8 .. 4091 ];
                    | _ -> ()
            ];
        };


    ///////////////////////////////////////////////////////////////////////////
    // Test cases

    [<Fact>]
    member _.ReadFileTypeIdentifier_Fail_001() =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            do! fa.SetFileSize 519UL
            let! _ =
                Assert.ThrowsAnyAsync<Exception>( fun () -> task {
                    let! _ = VhdxReader.ReadFileTypeIdentifier fa
                    ()
                } )
            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.ReadFileTypeIdentifier_Fail_002() =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            do! fa.SetFileSize 520UL
            let! r =
                Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                    let! _ = VhdxReader.ReadFileTypeIdentifier fa
                    ()
                } )
            Assert.StartsWith( "File type identifier", r.Message )
            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( 255 )>]
    [<InlineData( 256 )>]
    member _.ReadFileTypeIdentifier_001 ( len : int32 ) =
        task {
            let fname = Path.GetTempFileName()
            let creatorStr = String.replicate len "a"

            let fa = FileAccessor( fname, 1u, false )
            do! fa.SetFileSize 520UL

            do! fa.Write 0UL ( ArraySegment( Encoding.UTF8.GetBytes "vhdxfile" ) )
            do! fa.Write 8UL ( ArraySegment( Encoding.Unicode.GetBytes creatorStr ) )

            let! r = VhdxReader.ReadFileTypeIdentifier fa
            Assert.StrictEqual( creatorStr, r )

            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.ReadHeaders_Fail_001 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            do! fa.SetFileSize 135167UL // 128K + 4096 - 1
            let! _ =
                Assert.ThrowsAnyAsync<Exception>( fun () -> task {
                    let! _ = VhdxReader.ReadFileTypeIdentifier fa
                    ()
                } )
            fa.Close()
            File.Delete fname
        }

    static member m_ReadHeaders_001_Data : obj[][] = [|
        [|
            // No Error
            [||]; true; true; true; 0x10000UL; 0UL;
        |];
        [|
            // header0 CRC check error
            [||]; false; true; true; 0x20000UL; 0UL;
        |];
        [|
            [|
                // incollect header0 Signature
                ( 0, 0, [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] );
            |]; true; true; true; 0x20000UL; 0UL;
        |];
        [|
            [|
                // incollect header0 LogVersion
                ( 0, 64, [| 0xFFuy; 0xFFuy; |] );
            |]; true; true; true; 0x20000UL; 0UL;
        |];
        [|
            [|
                // incollect header0 Version
                ( 0, 66, [| 0xFFuy; 0xFFuy; |] );
            |]; true; true; true; 0x20000UL; 0UL;
        |];
        [|
            [|
                // header0 LogLength is zero.
                ( 0, 68, [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] );
            |]; true; true; true; 0x20000UL; 0UL;
        |];
        [|
            [|
                // header0 LogLength is not multiple of 1MB.
                ( 0, 68, [| 0x00uy; 0x00uy; 0x08uy; 0x00uy; |] );
            |]; true; true; true; 0x20000UL; 0UL;
        |];
        [|
            [|
                // header0 LogOffset is zero.
                ( 0, 72, [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] );
            |]; true; true; true; 0x20000UL; 0UL;
        |];
        [|
            [|
                // header0 LogOffset is not multiple of 1MB.
                ( 0, 72, [| 0x00uy; 0x00uy; 0x08uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] );
            |]; true; true; true; 0x20000UL; 0UL;
        |];
        [|
            [|
                // header0 LogLength is excessive.
                ( 0, 68, [| 0x00uy; 0x00uy; 0x00uy; 0x80uy |] );
            |]; true; true; true; 0x20000UL; 0UL;
        |];
        [|
            [|
                // header0 LogOffset is excessive.
                ( 0, 72, [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x80uy; |] );
            |]; true; true; true; 0x20000UL; 0UL;
        |];
        [|
            [|
                // header0 LogLength + LogOffset exceeds the file size.
                ( 0, 68, [| 0x00uy; 0x00uy; 0x20uy; 0x80uy; |] );
            |]; true; true; true; 0x20000UL; 0UL;
        |];
        [|
            // header1 CRC check error
            [||]; true; false; true; 0x10000UL; 0UL;
        |];
        [|
            [|
                // incollect header1 Signature
                ( 1, 0, [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] );
            |]; true; true; true; 0x10000UL; 0UL;
        |];
        [|
            [|
                // incollect header1 LogVersion
                ( 1, 64, [| 0xFFuy; 0xFFuy; |] );
            |]; true; true; true; 0x10000UL; 0UL;
        |];
        [|
            [|
                // incollect header1 Version
                ( 1, 66, [| 0xFFuy; 0xFFuy; |] );
            |]; true; true; true; 0x10000UL; 0UL;
        |];
        [|
            [|
                // header1 LogLength is zero.
                ( 1, 68, [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] );
            |]; true; true; true; 0x10000UL; 0UL;
        |];
        [|
            [|
                // header1 LogLength is not multiple of 1MB.
                ( 1, 68, [| 0x00uy; 0x00uy; 0x08uy; 0x00uy; |] );
            |]; true; true; true; 0x10000UL; 0UL;
        |];
        [|
            [|
                // header1 LogOffset is zero.
                ( 1, 72, [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] );
            |]; true; true; true; 0x10000UL; 0UL;
        |];
        [|
            [|
                // header1 LogOffset is not multiple of 1MB.
                ( 1, 72, [| 0x00uy; 0x00uy; 0x08uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] );
            |]; true; true; true; 0x10000UL; 0UL;
        |];
        [|
            [|
                // header1 LogLength is excessive.
                ( 1, 68, [| 0x00uy; 0x00uy; 0x00uy; 0x80uy |] );
            |]; true; true; true; 0x10000UL; 0UL;
        |];
        [|
            [|
                // header1 LogOffset is excessive.
                ( 1, 72, [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x80uy; |] );
            |]; true; true; true; 0x10000UL; 0UL;
        |];
        [|
            [|
                // header1 LogLength + LogOffset exceeds the file size.
                ( 1, 68, [| 0x00uy; 0x00uy; 0x20uy; 0x80uy; |] );
            |]; true; true; true; 0x10000UL; 0UL;
        |];
        [|
            [|
                // header0 SequenceNumber
                ( 0, 8, [| 0x01uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] );
                // header1 SequenceNumber
                ( 1, 8, [| 0x02uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] );
            |]; true; true; true; 0x20000UL; 2UL;
        |];
        [|
            [|
                // header0 SequenceNumber
                ( 0, 8, [| 0x05uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] );
                // header1 SequenceNumber
                ( 1, 8, [| 0x04uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |] );
            |]; true; true; true; 0x10000UL; 5UL;
        |];
        [|
            // CRC of both header0 and header1 is incollect.
            [||]; false; false; false; 0UL; 0UL;
        |];
    |]

    [<Theory>]
    [<MemberData( "m_ReadHeaders_001_Data" )>]
    member _.ReadHeaders_001 ( v : ( int32 * int32 * byte[] )[] ) ( crc0 : bool ) ( crc1 : bool ) ( success : bool ) ( extoffset : uint64 ) ( extseq : uint64 ) =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            do! fa.SetFileSize 2097152UL // 2MB

            let hddata0 = [|
                yield! ( "head" |> Encoding.UTF8.GetBytes ) // signature ( 0 .. 3 )
                0x00uy; 0x00uy; 0x00uy; 0x00uy;             // checksum ( 4 .. 7 )
                0x00uy; 0x00uy; 0x00uy; 0x00uy;             // SequenceNumber ( 8 .. 15 )
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                yield! ( Guid() ).ToByteArray();            // FileWriteGuid ( 16 .. 31 )
                yield! ( Guid() ).ToByteArray();            // DataWriteGuid ( 32 .. 47 )
                yield! ( Guid() ).ToByteArray();            // LogGuid ( 48 .. 63 )
                0x00uy; 0x00uy;                             // LogVersion ( 64 .. 65 )
                0x01uy; 0x00uy;                             // Version (1) ( 66 .. 67 )
                0x00uy; 0x00uy; 0x10uy; 0x00uy;             // LogLength (1MB) ( 68 .. 71 )
                0x00uy; 0x00uy; 0x10uy; 0x00uy;             // LogOffset (1MB) ( 72 .. 79 )
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                yield! Array.zeroCreate<byte> 4016;         // Reserved ( 80 .. 4095 )
            |]
            let hddata1 = Array.copy hddata0

            for ( hdidx, pos, data ) in v do
                let v = if hdidx = 0 then hddata0 else hddata1
                Array.blit data 0 v pos data.Length

            if crc0 then
                Crc32C.Compute hddata0
                |> ByteFunc.WriteU32LE hddata0 4u

            if crc1 then
                Crc32C.Compute hddata1
                |> ByteFunc.WriteU32LE hddata1 4u

            do! fa.Write 65536UL ( ArraySegment hddata0 )
            do! fa.Write 131072UL ( ArraySegment hddata1 )

            if success then
                let! rih, rvh = VhdxReader.ReadHeaders fa
                Assert.StrictEqual( extoffset, rih.Offset )
                Assert.StrictEqual( extseq, rvh.SequenceNumber )
            else
                let! r =
                    Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                        let! _ = VhdxReader.ReadHeaders fa
                        ()
                    } )
                Assert.StartsWith( "No valid header exists", r.Message )

            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( 0 ) >]
    [<InlineData( 1 ) >]
    member _.ReadHeaders_002 ( hdindex : int32 ) =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            do! fa.SetFileSize 4194304UL // 4MB

            let fileWriteGuid = Guid.NewGuid()
            let dataWriteGuid = Guid.NewGuid()
            let logGuid = Guid.NewGuid()
            let hddata0 = [|
                yield! ( "head" |> Encoding.UTF8.GetBytes ) // signature
                0x00uy; 0x00uy; 0x00uy; 0x00uy;             // checksum
                0x11uy; 0x22uy; 0x33uy; 0x44uy;             // SequenceNumber
                0x55uy; 0x66uy; 0x77uy; 0x88uy;
                yield! fileWriteGuid.ToByteArray();         // FileWriteGuid
                yield! dataWriteGuid.ToByteArray();         // DataWriteGuid
                yield! logGuid.ToByteArray();               // LogGuid
                0x00uy; 0x00uy;                             // LogVersion
                0x01uy; 0x00uy;                             // Version
                0x00uy; 0x00uy; 0x20uy; 0x00uy;             // LogLength
                0x00uy; 0x00uy; 0x20uy; 0x00uy;             // LogOffset
                0x00uy; 0x00uy; 0x00uy; 0x00uy;
                yield! Array.zeroCreate<byte> 4016;         // Reserved ( 80 .. 4095 )
            |]
            let hddata1 = Array.copy hddata0

            if hdindex = 0 then
                Crc32C.Compute hddata0
                |> ByteFunc.WriteU32LE hddata0 4u

            if hdindex = 1 then
                Crc32C.Compute hddata1
                |> ByteFunc.WriteU32LE hddata1 4u

            do! fa.Write 65536UL ( ArraySegment hddata0 )
            do! fa.Write 131072UL ( ArraySegment hddata1 )

            let! rih, rvh = VhdxReader.ReadHeaders fa
            let sigstring = rih.Signature |> int32 |> IPAddress.NetworkToHostOrder |> BitConverter.GetBytes |> Encoding.UTF8.GetString
            Assert.StrictEqual( "head", sigstring )
            Assert.StrictEqual( 0x8877665544332211UL, rvh.SequenceNumber )
            Assert.StrictEqual( fileWriteGuid, rvh.FileWriteGuid )
            Assert.StrictEqual( dataWriteGuid, rvh.DataWriteGuid )
            Assert.StrictEqual( logGuid, rvh.LogGuid )
            Assert.StrictEqual( 0us, rih.LogVersion )
            Assert.StrictEqual( 1us, rih.Version )
            Assert.StrictEqual( 2097152u, rih.LogLength )
            Assert.StrictEqual( 2097152UL, rih.LogOffset )
            Assert.StrictEqual( hdindex, rih.Index )

            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.ReadLogDataSector_001 () =
        let data = Array.zeroCreate<byte> 4084
        Random.Shared.NextBytes data
        let v = [|
            yield! ( "data" |> Encoding.UTF8.GetBytes ) // DataSignature
            0x11uy; 0x22uy; 0x33uy; 0x44uy;             // SequenceHigh
            yield! data;                                // Data
            0x55uy; 0x66uy; 0x77uy; 0x88uy;             // SequenceLow
        |]
        let v2 = VhdxReader.ReadLogDataSector v 0u 0x4433221188776655UL
        Assert.True(( v2 = data ))

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 4 )>]
    [<InlineData( 4092 )>]
    member _.ReadLogDataSector_Fail_001 ( dpos : int32 ) =
        let data = Array.zeroCreate<byte> 4084
        Random.Shared.NextBytes data
        let v = [|
            yield! ( "data" |> Encoding.UTF8.GetBytes ) // DataSignature
            0x11uy; 0x22uy; 0x33uy; 0x44uy;             // SequenceHigh
            yield! data;                                // Data
            0x55uy; 0x66uy; 0x77uy; 0x88uy;             // SequenceLow
        |]
        for i = dpos to dpos + 3 do
            v.[i] <- 0xFFuy;
        let v2 = VhdxReader.ReadLogDataSector v 0u 0x4433221188776655UL
        Assert.Empty v2

    [<Theory>]
    [<InlineData( 4095, 0u )>]
    [<InlineData( 8191, 4096u )>]
    member _.ReadLogDataSector_Fail_002 ( len : int32 ) ( pos : uint32 ) =
        let v = Array.zeroCreate<byte> len
        Array.blit ( "data" |> Encoding.UTF8.GetBytes ) 0 v 0 4
        let r = VhdxReader.ReadLogDataSector v pos 0UL
        Assert.Empty r

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 16 )>]
    member _.ReadLogDescriptor_ZeroDescriptor_001 ( dummyDataLen : int32 ) =
        let v = [|
            yield! Array.zeroCreate<byte> dummyDataLen
            yield! ( "zero" |> Encoding.UTF8.GetBytes ) // ZeroSignature
            0x00uy; 0x00uy; 0x00uy; 0x00uy;             // Reserved
            0x00uy; 0x10uy; 0x66uy; 0x55uy;             // ZeroLength
            0x44uy; 0x33uy; 0x22uy; 0x11uy;
            0x00uy; 0x10uy; 0xDDuy; 0xCCuy;             // FileOffset
            0xBBuy; 0xAAuy; 0x99uy; 0x88uy;
            0x88uy; 0x77uy; 0x66uy; 0x55uy;             // SequenceNumber
            0x44uy; 0x33uy; 0x22uy; 0x11uy;
        |]
        match VhdxReader.ReadLogDescriptor v ( uint32 dummyDataLen ) 0u 0x1122334455667788UL with
        | Some( LogDescriptor.Zero( x ) ) ->
            let signature = x.ZeroSignature |> int32 |> IPAddress.NetworkToHostOrder |> BitConverter.GetBytes |> Encoding.UTF8.GetString
            Assert.StrictEqual( "zero", signature )
            Assert.StrictEqual( 0x1122334455661000UL, x.ZeroLength )
            Assert.StrictEqual( 0x8899AABBCCDD1000UL, x.FileOffset )
            Assert.StrictEqual( 0x1122334455667788UL, x.SequenceNumber )
        | _ ->
            Assert.Fail __LINE__

    static member m_ReadLogDescriptor_ZeroDescriptor_Fail_001_Data : obj[][] = [|
        [| 24; [| 0xFFuy; |] |];  // ZeroLength is not multiple of 4KB
        [| 32; [| 0xFFuy; |] |];  // FileOffset is not multiple of 4KB
        [| 40; [| 0xFFuy; |] |];  // SequenceNumber Unmatch
    |]

    [<Theory>]
    [<MemberData( "m_ReadLogDescriptor_ZeroDescriptor_Fail_001_Data" )>]
    member _.ReadLogDescriptor_ZeroDescriptor_Fail_001 ( pos : int32 ) ( dd : byte[] ) =
        let v = [|
            yield! Array.zeroCreate<byte> 16
            yield! ( "zero" |> Encoding.UTF8.GetBytes ) // ZeroSignature
            0x00uy; 0x00uy; 0x00uy; 0x00uy;             // Reserved
            0x00uy; 0x10uy; 0x66uy; 0x55uy;             // ZeroLength
            0x44uy; 0x33uy; 0x22uy; 0x11uy;
            0x00uy; 0x10uy; 0xDDuy; 0xCCuy;             // FileOffset
            0xBBuy; 0xAAuy; 0x99uy; 0x88uy;
            0x88uy; 0x77uy; 0x66uy; 0x55uy;             // SequenceNumber
            0x44uy; 0x33uy; 0x22uy; 0x11uy;
        |]
        Array.blit dd 0 v pos dd.Length
        let r = VhdxReader.ReadLogDescriptor v 16u 0u 0x1122334455667788UL
        Assert.StrictEqual( None, r )

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 16 )>]
    member _.ReadLogDescriptor_DataDescriptor_001 ( dummyDataLen : int32 ) =
        let v = [|
            yield! Array.zeroCreate<byte> dummyDataLen
            yield! ( "desc" |> Encoding.UTF8.GetBytes ) // DataSignature
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy;             // TrailingBytes
            0x11uy; 0x22uy; 0x33uy; 0x44uy;             // LeadingBytes
            0x55uy; 0x66uy; 0x77uy; 0x88uy;
            0x00uy; 0x10uy; 0xDDuy; 0xCCuy;             // FileOffset
            0xBBuy; 0xAAuy; 0x99uy; 0x88uy;
            0x88uy; 0x77uy; 0x66uy; 0x55uy;             // SequenceNumber
            0x44uy; 0x33uy; 0x22uy; 0x11uy;
        |]
        match VhdxReader.ReadLogDescriptor v ( uint32 dummyDataLen ) 99u 0x1122334455667788UL with
        | Some( LogDescriptor.Data( x ) ) ->
            let signature = x.DataSignature |> int32 |> IPAddress.NetworkToHostOrder |> BitConverter.GetBytes |> Encoding.UTF8.GetString
            Assert.StrictEqual( "desc", signature )
            Assert.True(( [| 0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy; |] = x.TrailingBytes ))
            Assert.True(( [| 0x11uy; 0x22uy; 0x33uy; 0x44uy; 0x55uy; 0x66uy; 0x77uy; 0x88uy; |] = x.LeadingBytes ))
            Assert.StrictEqual( 0x8899AABBCCDD1000UL, x.FileOffset )
            Assert.StrictEqual( 0x1122334455667788UL, x.SequenceNumber )
        | _ ->
            Assert.Fail __LINE__

    static member m_ReadLogDescriptor_DataDescriptor_Fail_001_Data : obj[][] = [|
        [| 32; [| 0xFFuy; |] |];  // FileOffset is not multiple of 4KB
        [| 40; [| 0xFFuy; |] |];  // SequenceNumber Unmatch
    |]

    [<Theory>]
    [<MemberData( "m_ReadLogDescriptor_DataDescriptor_Fail_001_Data" )>]
    member _.ReadLogDescriptor_DataDescriptor_Fail_001 ( pos : int32 ) ( dd : byte[] ) =
        let v = [|
            yield! Array.zeroCreate<byte> 16
            yield! ( "desc" |> Encoding.UTF8.GetBytes ) // DataSignature
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy;             // TrailingBytes
            0x11uy; 0x22uy; 0x33uy; 0x44uy;             // LeadingBytes
            0x55uy; 0x66uy; 0x77uy; 0x88uy;
            0x00uy; 0x10uy; 0xDDuy; 0xCCuy;             // FileOffset
            0xBBuy; 0xAAuy; 0x99uy; 0x88uy;
            0x88uy; 0x77uy; 0x66uy; 0x55uy;             // SequenceNumber
            0x44uy; 0x33uy; 0x22uy; 0x11uy;
        |]
        Array.blit dd 0 v pos dd.Length
        let r = VhdxReader.ReadLogDescriptor v 16u 99u 0x1122334455667788UL
        Assert.StrictEqual( None, r )

    [<Fact>]
    member _.ReadLogDescriptor_Fail_001 () =
        let v = Array.zeroCreate<byte> 32
        let r = VhdxReader.ReadLogDescriptor v 0u 0u 0UL
        Assert.StrictEqual( None, r )

    [<Theory>]
    [<InlineData( 31, 0u )>]
    [<InlineData( 32, 1u )>]
    [<InlineData( 32, 0x80000000u )>]
    [<InlineData( 32, 0xFFFFFFFFu )>]
    member _.ReadLogDescriptor_Fail_002 ( dataLen : int32 ) ( offset : uint32 ) =
        let v1 = Array.zeroCreate<byte> dataLen
        let v2 = [|
            yield! ( "desc" |> Encoding.UTF8.GetBytes ) // DataSignature
            0xAAuy; 0xBBuy; 0xCCuy; 0xDDuy;             // TrailingBytes
            0x11uy; 0x22uy; 0x33uy; 0x44uy;             // LeadingBytes
            0x55uy; 0x66uy; 0x77uy; 0x88uy;
            0x00uy; 0x10uy; 0xDDuy; 0xCCuy;             // FileOffset
            0xBBuy; 0xAAuy; 0x99uy; 0x88uy;
            0x88uy; 0x77uy; 0x66uy; 0x55uy;             // SequenceNumber
            0x44uy; 0x33uy; 0x22uy; 0x11uy;
        |]
        Array.blit v2 0 v1 0 dataLen
        let r = VhdxReader.ReadLogDescriptor v1 offset 0u 0x1122334455667788UL
        Assert.StrictEqual( None, r )

    [<Theory>]
    [<InlineData( 0, 0u, "The log data length must not be empty" )>]
    [<InlineData( 1048575, 0u, "The log data length must be in units of 1MB" )>]
    [<InlineData( 1048576, 4095u, "The log sequence start position msut be in 4KB units" )>]
    member _.ReadLogEntry_Fail_001 ( dataLen : int32 ) ( offset : uint32 ) ( exmsg : string ) =
        let v1 = Array.zeroCreate<byte> dataLen
        let r = Assert.Throws<VhdxMediaException>( fun () ->
            VhdxReader.ReadLogEntry v1 offset ( Guid() ) |> ignore
        )
        Assert.StartsWith( exmsg, r.Message )

    static member m_ReadLogEntry_Fail_002_Data : obj[][] = [|
        [|    0; [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |]; 0; [||] |];   // Signature
        [|    0; [||]; 4; [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |] |];   // Checksum
        [|    8; [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |]; 0; [||] |];   // EntryLength = -1
        [|    8; [| 0x00uy; 0x00uy; 0x00uy; 0x80uy; |]; 0; [||] |];   // EntryLength = -2147483648
        [|    8; [| 0x00uy; 0x00uy; 0x00uy; 0x00uy; |]; 0; [||] |];   // EntryLength = 0
        [|    8; [| 0x00uy; 0x10uy; 0x10uy; 0x00uy; |]; 0; [||] |];   // EntryLength exceeds the log length.
        [|    8; [| 0x00uy; 0x08uy; 0x00uy; 0x00uy; |]; 0; [||] |];   // EntryLength is not a multiple of 4K.
        [|   12; [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |]; 0; [||] |];   // Tail = -1
        [|   12; [| 0x00uy; 0x00uy; 0x00uy; 0x80uy; |]; 0; [||] |];   // Tail = -2147483648
        [|   12; [| 0x00uy; 0x10uy; 0x10uy; 0x00uy; |]; 0; [||] |];   // Tail exceeds the log length.
        [|   12; [| 0x00uy; 0x08uy; 0x00uy; 0x00uy; |]; 0; [||] |];   // Tail is not a multiple of 4K.
        [|   16; [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |]; 0; [||] |];   // SequenceNumber
        [|   24; [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |]; 0; [||] |];   // DescriptorCount = -1
        [|   24; [| 0x00uy; 0x00uy; 0x00uy; 0x80uy; |]; 0; [||] |];   // DescriptorCount = -2147483648
        [|   24; [| 0xFFuy; 0x00uy; 0x00uy; 0x00uy; |]; 0; [||] |];   // DescriptorCount exceeds the EntryLength
        [|   32; [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |]; 0; [||] |];   // LogGuid
        [|   48; [| 0x00uy; 0x00uy; 0x08uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |]; 0; [||] |];   // FlushedFileOffset is not a multiple of 1M.
        [|   48; [| 0x00uy; 0x00uy; 0x10uy; 0x00uy; 0x00uy; 0x40uy; 0x00uy; 0x00uy; |]; 0; [||] |];   // FlushedFileOffset exceeds the EntryLength 64T.
        [|   56; [| 0x00uy; 0x00uy; 0x08uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; |]; 0; [||] |];   // LastFileOffset is not a multiple of 1M.
        [|   56; [| 0x00uy; 0x00uy; 0x10uy; 0x00uy; 0x00uy; 0x40uy; 0x00uy; 0x00uy; |]; 0; [||] |];   // LastFileOffset exceeds the EntryLength 64T.
        [|   64; [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |]; 0; [||] |];   // DataSignature
        // The number of DataSignatures differs from the number of DataSectors.
        [|   64; [|
                    0x7Auy; 0x65uy; 0x72uy; 0x6Fuy; // zero
                    0x00uy; 0x00uy; 0x00uy; 0x00uy; // Reserved
                    0x00uy; 0x10uy; 0x00uy; 0x00uy; // ZeroLength
                    0x00uy; 0x00uy; 0x00uy; 0x00uy;
                    0x00uy; 0x00uy; 0x00uy; 0x00uy; // FileOffset
                    0x00uy; 0x00uy; 0x00uy; 0x00uy;
                    0x63uy; 0x00uy; 0x00uy; 0x00uy; // SequenceNumber
                    0x00uy; 0x00uy; 0x00uy; 0x00uy;
                |]; 0; [||]
        |];
        [| 4096; [| 0xFFuy; 0xFFuy; 0xFFuy; 0xFFuy; |]; 0; [||] |];   // DataSignature
    |]

    [<Theory>]
    [<MemberData( "m_ReadLogEntry_Fail_002_Data" )>]
    member _.ReadLogEntry_Fail_002 ( patch1Pos : int ) ( patch1Data : byte[] ) ( patch2Pos : int ) ( patch2Data : byte[] ) =
        let entry = [|
            {
                PatchPosition = patch1Pos;
                PatchData = patch1Data;
                Descriptor = [| Data( [||], 0UL ); Data( [||], 4096UL ); |];
            };
        |]
        let logGuid = Guid()
        let logData = genLogData 1048576 0 entry logGuid 99UL 2097152UL 3145728UL
        Array.blit patch2Data 0 logData patch2Pos patch2Data.Length
        let r = VhdxReader.ReadLogEntry logData 0u logGuid
        Assert.True( r.IsNone )

    [<Fact>]
    member _.ReadLogEntry_001 () =
        let entry = [|
            {
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 0UL ); Data( [||], 4096UL ); Data( [||], 8192UL ); Zero( 4096UL, 12288UL ) |];
            };
            {
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 12288UL ); Data( [||], 16384UL ); |];
            };
            {
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 20480UL ); |];
            };
        |]
        let logGuid = Guid.NewGuid()
        let logData = genLogData 1048576 0 entry logGuid 99UL 2097152UL 3145728UL 
        let r = VhdxReader.ReadLogEntry logData 0u logGuid
        Assert.True( r.IsSome )
        let signature = r.Value.Signature |> int32 |> IPAddress.NetworkToHostOrder |> BitConverter.GetBytes |> Encoding.UTF8.GetString
        Assert.StrictEqual( "loge", signature )
        Assert.StrictEqual( 4u * 4u * 1024u, r.Value.EntryLength )
        Assert.StrictEqual( 0u, r.Value.Tail )
        Assert.StrictEqual( 99UL, r.Value.SequenceNumber )
        Assert.StrictEqual( 4u, r.Value.DescriptorCount )
        Assert.StrictEqual( logGuid, r.Value.LogGuid )
        Assert.StrictEqual( 2097152UL, r.Value.FlushedFileOffset )
        Assert.StrictEqual( 3145728UL, r.Value.LastFileOffset )
        Assert.StrictEqual( 4, r.Value.Descriptors.Length )
        for i = 0 to 2 do
            match r.Value.Descriptors.[i] with
            | LogDescriptor.Data( x ) ->
                let signature = x.DataSignature |> int32 |> IPAddress.NetworkToHostOrder |> BitConverter.GetBytes |> Encoding.UTF8.GetString
                Assert.StrictEqual( "desc", signature )
                Assert.True(( [| 0uy; 0uy; 0uy; 0uy; |] = x.TrailingBytes ))
                Assert.True(( [| 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; |] = x.LeadingBytes ))
                Assert.True(( 4096UL * uint64 i = x.FileOffset ))
                Assert.True(( 99UL = x.SequenceNumber ))
                Assert.True(( uint32 i = x.ddIndex ))
            | _ ->
                Assert.Fail __LINE__

        match r.Value.Descriptors.[3] with
        | LogDescriptor.Zero( x ) ->
            let signature = x.ZeroSignature |> int32 |> IPAddress.NetworkToHostOrder |> BitConverter.GetBytes |> Encoding.UTF8.GetString
            Assert.StrictEqual( "zero", signature )
            Assert.True(( 4096UL = x.ZeroLength ))
            Assert.True(( 12288UL = x.FileOffset ))
            Assert.True(( 99UL = x.SequenceNumber ))
        | _ ->
            Assert.Fail __LINE__

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 4096 )>]
    [<InlineData( 1032192 )>]
    [<InlineData( 1036288 )>]
    [<InlineData( 1044480 )>]
    member _.ReadLogEntry_002 ( pos : int32 ) =
        let entry = [|
            {
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 0UL ); Data( [||], 4096UL ); Data( [||], 8192UL ); Zero( 4096UL, 12288UL ) |];
            };
        |]
        let logGuid = Guid.NewGuid()
        let logData = genLogData 1048576 pos entry logGuid 99UL 2097152UL 3145728UL 
        let r = VhdxReader.ReadLogEntry logData ( uint32 pos ) logGuid
        Assert.True( r.IsSome )
        Assert.StrictEqual( 4, r.Value.Descriptors.Length )
 
    [<Theory>]
    [<InlineData( 1, 1 )>]
    [<InlineData( 125, 1 )>]
    [<InlineData( 126, 1 )>]
    [<InlineData( 127, 1 )>]
    [<InlineData( 0, 254 )>]
    [<InlineData( 0, 0 )>]
    member _.ReadLogEntry_003 ( zcount : int32 ) ( dcount : int32 ) =
        let entry = [|
            {
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [|
                    for i = 1 to zcount do
                        yield Zero( 4096UL, 12288UL )
                    for i = 1 to dcount do
                        yield Data( [||], 0UL );
                |];
            };
        |]
        let logGuid = Guid.NewGuid()
        let logData = genLogData 1048576 0 entry logGuid 99UL 2097152UL 3145728UL 
        let r = VhdxReader.ReadLogEntry logData 0u logGuid
        Assert.True( r.IsSome )
        Assert.StrictEqual( zcount + dcount, r.Value.Descriptors.Length )

    [<Theory>]
    [<InlineData( 0 )>]
    [<InlineData( 1011712 )>]
    [<InlineData( 1019904 )>]
    [<InlineData( 1044480 )>]
    member _.ReadActiveLogSequense_001 ( pos : int32 ) =
        let entry = [|
            {   // Entry length = 16384
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 0UL ); Data( [||], 4096UL ); Data( [||], 8192UL ); Zero( 4096UL, 12288UL ) |];
            };
            {   // Entry length = 12288
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 12288UL ); Data( [||], 16384UL ); |];
            };
            {   // Entry length = 8192
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 20480UL ); |];
            };
        |]
        let logGuid = Guid.NewGuid()
        let logData = genLogData 1048576 pos entry logGuid 99UL 2097152UL 3145728UL 
        let r = VhdxReader.ReadActiveLogSequense logData logGuid
        Assert.StrictEqual( 3, r.Length )
        Assert.StrictEqual( 4u, r.[0].DescriptorCount )
        Assert.True( r.[0].Descriptors.[0].IsData )
        Assert.True( r.[0].Descriptors.[1].IsData )
        Assert.True( r.[0].Descriptors.[2].IsData )
        Assert.True( r.[0].Descriptors.[3].IsZero )
        Assert.StrictEqual( 2u, r.[1].DescriptorCount )
        Assert.True( r.[1].Descriptors.[0].IsData )
        Assert.True( r.[1].Descriptors.[1].IsData )
        Assert.StrictEqual( 1u, r.[2].DescriptorCount )
        Assert.True( r.[2].Descriptors.[0].IsData )

    // If multiple valid active sequences exist, the one with the higher sequence number is adopted.
    [<Theory>]
    [<InlineData( 1UL, 2UL, false )>]
    [<InlineData( 2UL, 1UL, true )>]
    [<InlineData( 2UL, 2UL, true )>]
    member _.ReadActiveLogSequense_002 ( seq1 : uint64 ) ( seq2 : uint64 ) ( flg : bool ) =
        let entry1 = [|
            {   // Entry length = 16384
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 0UL ); Data( [||], 4096UL ); Data( [||], 8192UL ); Zero( 4096UL, 12288UL ) |];
            };
            {   // Entry length = 12288
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 12288UL ); Data( [||], 16384UL ); |];
            };
        |]
        let entry2 = [|
            {   // Entry length = 8192
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 20480UL ); |];
            };
        |]

        let logGuid = Guid.NewGuid()
        let logData1 = genLogData 1048576 0 entry1 logGuid seq1 2097152UL 3145728UL
        let logData2 = genLogData 1048576 65536 entry2 logGuid seq2 2097152UL 3145728UL
        Array.blit logData2 65536 logData1 65536 8192

        let r = VhdxReader.ReadActiveLogSequense logData1 logGuid
        if flg then
            Assert.StrictEqual( 2, r.Length )
            Assert.StrictEqual( 4u, r.[0].DescriptorCount )
            Assert.True( r.[0].Descriptors.[0].IsData )
            Assert.True( r.[0].Descriptors.[1].IsData )
            Assert.True( r.[0].Descriptors.[2].IsData )
            Assert.True( r.[0].Descriptors.[3].IsZero )
            Assert.StrictEqual( 2u, r.[1].DescriptorCount )
            Assert.True( r.[1].Descriptors.[0].IsData )
            Assert.True( r.[1].Descriptors.[1].IsData )
        else
            Assert.StrictEqual( 1, r.Length )
            Assert.StrictEqual( 1u, r.[0].DescriptorCount )
            Assert.True( r.[0].Descriptors.[0].IsData )

    // Even if multiple valid sequences exist consecutively, each sequence is evaluated separately.
    [<Fact>]
    member _.ReadActiveLogSequense_003 () =
        let entry1 = [|
            {   // Entry length = 16384
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 0UL ); Data( [||], 4096UL ); Data( [||], 8192UL ); Zero( 4096UL, 12288UL ) |];
            };
            {   // Entry length = 12288
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 12288UL ); Data( [||], 16384UL ); |];
            };
        |]
        let entry2 = [|
            {   // Entry length = 8192
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 20480UL ); |];
            };
        |]

        let logGuid = Guid.NewGuid()
        let logData1 = genLogData 1048576 0 entry1 logGuid 1UL 2097152UL 3145728UL
        let logData2 = genLogData 1048576 28672 entry2 logGuid 3UL 2097152UL 3145728UL
        Array.blit logData2 28672 logData1 28672 8192

        let r = VhdxReader.ReadActiveLogSequense logData1 logGuid
        Assert.StrictEqual( 1, r.Length )
        Assert.StrictEqual( 1u, r.[0].DescriptorCount )
        Assert.True( r.[0].Descriptors.[0].IsData )

    [<Fact>]
    member _.ReadActiveLogSequense_004 () =
        let entry = [|
            {
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 20480UL ); |];
            };
        |]
        let logGuid = Guid.NewGuid()
        let logData1 = genLogData 1048576 0 entry logGuid 0UL 2097152UL 3145728UL
        let r = VhdxReader.ReadActiveLogSequense logData1 logGuid
        Assert.StrictEqual( 1, r.Length )
        Assert.StrictEqual( 1u, r.[0].DescriptorCount )
        Assert.True( r.[0].Descriptors.[0].IsData )

    [<Fact>]
    member _.ReadActiveLogSequense_005 () =
        let entry1 = [|
            {   // Entry length = 16384
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 0UL ); Data( [||], 4096UL ); Data( [||], 8192UL ); Zero( 4096UL, 12288UL ) |];
            };
        |]
        let entry2 = [|
            {   // Entry length = 8192
                PatchPosition = 0;
                PatchData = Array.Empty();
                Descriptor = [| Data( [||], 20480UL ); |];
            };
        |]

        let logGuid = Guid.NewGuid()
        let logData1 = genLogData 1048576 8192 entry1 logGuid 50UL 2097152UL 3145728UL
        let logData2 = genLogData 1048576 65536 entry2 logGuid 100UL 2097152UL 3145728UL
        Array.blit logData2 65536 logData1 32768 8192

        let r = VhdxReader.ReadActiveLogSequense logData1 logGuid
        Assert.StrictEqual( 1, r.Length )
        Assert.StrictEqual( 4u, r.[0].DescriptorCount )
        Assert.True( r.[0].Descriptors.[0].IsData )
        Assert.True( r.[0].Descriptors.[1].IsData )
        Assert.True( r.[0].Descriptors.[2].IsData )
        Assert.True( r.[0].Descriptors.[3].IsZero )

    [<Theory>]
    [<InlineData( 0, "The log data length must not be empty" )>]
    [<InlineData( 524288, "The log data length must be in units of 1MB" )>]
    member _.ReadActiveLogSequense_Fail_001 ( len : int32 ) ( exmsg : string ) =
        let r =
            Assert.Throws<VhdxMediaException>( fun () ->
                VhdxReader.ReadActiveLogSequense ( Array.zeroCreate<byte> len ) ( Guid() ) |> ignore
            )
        Assert.StartsWith( exmsg, r.Message )

    [<Theory>]
    [<InlineData( 1UL, 4096u, "The offset(" )>]
    [<InlineData( 4095UL, 4096u, "The offset(" )>]
    [<InlineData( 4096UL, 1u, "The length(" )>]
    [<InlineData( 4096UL, 4095u, "The length(" )>]
    member _.ReadBytesWithLog_Fail_001 ( offset : uint64 ) ( len : uint32 ) ( exmsg : string ) =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u,false )
            try
                let! r =
                    Assert.ThrowsAsync<VhdxMediaException>( fun () -> task {
                        let! _ = VhdxReader.ReadBytesWithLog [] 1048576UL fa offset len
                        ()
                    } )
                Assert.StartsWith( exmsg, r.Message )
            finally
                fa.Close()
                File.Delete fname
        }

    [<Fact>]
    member _.ReadBytesWithLog_Fail_002 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u,false )
            try
                let! r =
                    Assert.ThrowsAsync<ArgumentOutOfRangeException>( fun () -> task {
                        let! _ = VhdxReader.ReadBytesWithLog [] 1048576UL fa 1048576UL 4096u
                        ()
                    } )
                ()
            finally
                fa.Close()
                File.Delete fname
        }

    [<Fact>]
    member _.ReadBytesWithLog_Length0_001 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u,false )
            try
                let! r = VhdxReader.ReadBytesWithLog [] 1048576UL fa 0UL 0u
                Assert.Empty r
            finally
                fa.Close()
                File.Delete fname
        }

    [<Fact>]
    member _.ReadBytesWithLog_Empty_001 () =
        task {
            let fname = Path.GetTempFileName()
            let firstBytes = Array.zeroCreate<byte> 4096
            Random.Shared.NextBytes firstBytes
            File.WriteAllBytes( fname, firstBytes )
            let fa = FileAccessor( fname, 1u,false )
            Assert.StrictEqual( 4096UL, fa.FileSize )

            try
                let! r = VhdxReader.ReadBytesWithLog [] 1048576UL fa 0UL 4096u
                Assert.StrictEqual( 4096, r.Length )
                Assert.True(( firstBytes = r ))
            finally
                fa.Close()
                File.Delete fname
        }

    [<Fact>]
    member _.ReadBytesWithLog_Empty_002 () =
        task {
            let fname = Path.GetTempFileName()
            let firstBytes = Array.zeroCreate<byte> 4096
            Random.Shared.NextBytes firstBytes
            File.WriteAllBytes( fname, firstBytes )
            let fa = FileAccessor( fname, 1u,false )
            Assert.StrictEqual( 4096UL, fa.FileSize )

            try
                let! r = VhdxReader.ReadBytesWithLog [] 1048576UL fa 4096UL 4096u
                for itr in r do
                    Assert.StrictEqual( 0uy, itr )
            finally
                fa.Close()
                File.Delete fname
        }

//          | 0 .. 4095 | 4096 .. 8191 | 8192 .. 12287 | 12288 .. 16383 | 16384 .. 20479 | 20480 .. 24575 | 24576 .. 28671 |
// file     |------------------------Random 16KB------------------------|                :                :                :
// zero     :           |-----4KB------|               :                :                :                :                :
// data     :           :              |--Random 4KB---|                :                |---Random 4KB---|                :
//          :           :              :               :                :                :                :                :
// test_001 |Raed target|              :               :                :                :                :                :
// test_002 :           :              :               :                |---Raed target--|                :                :
// test_003 |--------Raed target-------|               :                :                :                :                :
// test_004 :           :              |-----------Raed target----------|                :                :                :
// test_005 :           :              :               :                :                |-----------Raed target-----------|

    [<Fact>]
    member _.ReadBytesWithLog_001 () =
        task {
            let fname = Path.GetTempFileName()
            let firstBytes = Array.zeroCreate<byte>( 4096 * 4 )
            Random.Shared.NextBytes firstBytes
            File.WriteAllBytes( fname, firstBytes )
            let fa = FileAccessor( fname, 1u,false )
            Assert.StrictEqual( uint64 firstBytes.Length, fa.FileSize )

            let data0 = Array.zeroCreate<byte>( 4096 )
            let data1 = Array.zeroCreate<byte>( 4096 )
            Random.Shared.NextBytes data0
            Random.Shared.NextBytes data1
            let entry = [
                genLogEntry( [| Zero( 4096UL, 4096UL ); |] );
                genLogEntry( [| Data( data0, 8192UL ); Data( data1, 20480UL ); |] );
            ]

            try
                let! r = VhdxReader.ReadBytesWithLog entry 1048576UL fa 0UL 4096u
                Assert.StrictEqual( 4096, r.Length )
                Assert.True(( firstBytes.[ 0 .. 4095 ] = r.[ 0 .. 4095 ] ))
            finally
                fa.Close()
                File.Delete fname
        }

    [<Fact>]
    member _.ReadBytesWithLog_002 () =
        task {
            let fname = Path.GetTempFileName()
            let firstBytes = Array.zeroCreate<byte>( 4096 * 4 )
            Random.Shared.NextBytes firstBytes
            File.WriteAllBytes( fname, firstBytes )
            let fa = FileAccessor( fname, 1u,false )
            Assert.StrictEqual( uint64 firstBytes.Length, fa.FileSize )

            let data0 = Array.zeroCreate<byte>( 4096 )
            let data1 = Array.zeroCreate<byte>( 4096 )
            Random.Shared.NextBytes data0
            Random.Shared.NextBytes data1
            let entry = [
                genLogEntry( [| Zero( 4096UL, 4096UL ); |] );
                genLogEntry( [| Data( data0, 8192UL ); Data( data1, 20480UL ); |] );
            ]

            try
                let! r = VhdxReader.ReadBytesWithLog entry 1048576UL fa 16384UL 4096u
                Assert.StrictEqual( 4096, r.Length )
                for i = 0 to 4095 do
                    Assert.StrictEqual( 0uy, r.[i] )
            finally
                fa.Close()
                File.Delete fname
        }

    [<Fact>]
    member _.ReadBytesWithLog_003 () =
        task {
            let fname = Path.GetTempFileName()
            let firstBytes = Array.zeroCreate<byte>( 4096 * 4 )
            Random.Shared.NextBytes firstBytes
            File.WriteAllBytes( fname, firstBytes )
            let fa = FileAccessor( fname, 1u,false )
            Assert.StrictEqual( uint64 firstBytes.Length, fa.FileSize )

            let data0 = Array.zeroCreate<byte>( 4096 )
            let data1 = Array.zeroCreate<byte>( 4096 )
            Random.Shared.NextBytes data0
            Random.Shared.NextBytes data1
            let entry = [
                genLogEntry( [| Zero( 4096UL, 4096UL ); |] );
                genLogEntry( [| Data( data0, 8192UL ); Data( data1, 20480UL ); |] );
            ]

            try
                let! r = VhdxReader.ReadBytesWithLog entry 1048576UL fa 0UL 8192u
                Assert.StrictEqual( 8192, r.Length )
                Assert.True(( firstBytes.[ 0 .. 4095 ] = r.[ 0 .. 4095 ] ))
                for i = 4096 to 8191 do
                    Assert.StrictEqual( 0uy, r.[i] )
            finally
                fa.Close()
                File.Delete fname
        }

    [<Fact>]
    member _.ReadBytesWithLog_004 () =
        task {
            let fname = Path.GetTempFileName()
            let firstBytes = Array.zeroCreate<byte>( 4096 * 4 )
            Random.Shared.NextBytes firstBytes
            File.WriteAllBytes( fname, firstBytes )
            let fa = FileAccessor( fname, 1u,false )
            Assert.StrictEqual( uint64 firstBytes.Length, fa.FileSize )

            let data0 = Array.zeroCreate<byte>( 4096 )
            let data1 = Array.zeroCreate<byte>( 4096 )
            Random.Shared.NextBytes data0
            Random.Shared.NextBytes data1
            let entry = [
                genLogEntry( [| Zero( 4096UL, 4096UL ); |] );
                genLogEntry( [| Data( data0, 8192UL ); Data( data1, 20480UL ); |] );
            ]

            try
                let! r = VhdxReader.ReadBytesWithLog entry 1048576UL fa 8192UL 8192u
                Assert.StrictEqual( 8192, r.Length )
                Assert.True(( data0 = r.[ 0 .. 4095 ] ))
                Assert.True(( firstBytes.[ 12288 .. 16383 ] = r.[ 4096 .. 8191 ] ))
            finally
                fa.Close()
                File.Delete fname
        }

    [<Fact>]
    member _.ReadBytesWithLog_005 () =
        task {
            let fname = Path.GetTempFileName()
            let firstBytes = Array.zeroCreate<byte>( 4096 * 4 )
            Random.Shared.NextBytes firstBytes
            File.WriteAllBytes( fname, firstBytes )
            let fa = FileAccessor( fname, 1u,false )
            Assert.StrictEqual( uint64 firstBytes.Length, fa.FileSize )

            let data0 = Array.zeroCreate<byte>( 4096 )
            let data1 = Array.zeroCreate<byte>( 4096 )
            Random.Shared.NextBytes data0
            Random.Shared.NextBytes data1
            let entry = [
                genLogEntry( [| Zero( 4096UL, 4096UL ); |] );
                genLogEntry( [| Data( data0, 8192UL ); Data( data1, 20480UL ); |] );
            ]

            try
                let! r = VhdxReader.ReadBytesWithLog entry 1048576UL fa 20480UL 8192u
                Assert.StrictEqual( 8192, r.Length )
                Assert.True(( data1 = r.[ 0 .. 4095 ] ))
                for i = 4096 to 8191 do
                    Assert.StrictEqual( 0uy, r.[i] )
            finally
                fa.Close()
                File.Delete fname
        }
