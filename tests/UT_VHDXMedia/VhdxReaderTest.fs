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

type TestLogEntry =
    | Zero of ( uint64 * uint64 )
    | Data of byte[] * uint64   // The byte array must be 4KB

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
        ( logEntries : TestLogEntry[][] )
        ( logGuid : Guid )
        ( sequenceNumber : uint64 )
        ( flushedFileOffset : uint64 )
        ( lastFileOffset : uint64 ) =

        let v = [|
            for i = 0 to logEntries.Length - 1 do
                let logents = logEntries.[i]
                let entryBytes = [|
                    let dataCount = Array.fold ( fun cnt j -> cnt + ( match j with | Data _ -> 1 | _ -> 0 ) ) 0 logents
                    let headerlength = Functions.AddPaddingLengthInt32 ( 64 + logents.Length * 32 ) 4096
                    let entryLength = headerlength + dataCount * 4096
                    // log entry header
                    yield! logEntryHeader ( uint32 entryLength ) ( uint32 startPos ) ( sequenceNumber + uint64 i ) ( uint32 logents.Length ) logGuid flushedFileOffset lastFileOffset
                    // descriptor
                    for itr2 in logents do
                        match itr2 with
                        | Zero( x, y ) ->
                            yield! zeroDiscriptor x y sequenceNumber
                        | Data( x, y ) ->
                            let trailingBytes = if x.Length > 0 then x.[ 4092 .. 4095 ] else Array.zeroCreate<byte> 4
                            let leadingBytes = if x.Length > 0 then x.[ 0 .. 7 ] else Array.zeroCreate<byte> 8
                            yield! dataDiscriptor trailingBytes leadingBytes y sequenceNumber
                    // padding
                    let padlength = headerlength - ( 64 + logents.Length * 32 )
                    yield! Array.zeroCreate<byte> padlength
                    // data sector
                    for itr2 in logents do
                        match itr2 with
                        | Data( x, y ) ->
                            yield! ( "data" |> Encoding.UTF8.GetBytes )     // DataSignature
                            yield! BitConverter.GetBytes ( uint32 ( sequenceNumber >>> 32 ) )   // SequenceHigh
                            yield! if x.Length > 0 then x.[ 8 .. 4091 ] else Array.zeroCreate<byte> 4084
                            yield! BitConverter.GetBytes ( uint32 sequenceNumber )   // SequenceLow
                        | _ ->
                            ()
                |]
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

    [<Fact>]
    member _.ReadLogEntry_001 () =
        let entry = [|
            [| Data( [||], 0UL ); Data( [||], 4096UL ); Data( [||], 8192UL ); Zero( 4096UL, 12288UL ) |]
            [| Data( [||], 12288UL ); Data( [||], 16384UL ); |]
            [| Data( [||], 20480UL ); |]
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

