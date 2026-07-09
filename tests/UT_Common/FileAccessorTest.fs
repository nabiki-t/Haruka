//=============================================================================
// Haruka Software Storage.
// FileAccessorTest.fs : Test cases for FileAccessor class.
//


//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.Commons

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading
open System.Threading.Tasks

open Xunit

open Haruka.Commons
open Haruka.Constants

//=============================================================================
// Class implementation

type Stream_Stub() =
    inherit Stream() with
        let mutable f_get_CanRead : ( unit -> bool ) option = None
        let mutable f_get_CanSeek : ( unit -> bool ) option = None
        let mutable f_get_CanTimeout : ( unit -> bool ) option = None
        let mutable f_get_CanWrite : ( unit -> bool ) option = None
        let mutable f_get_Length : ( unit -> int64 ) option = None
        let mutable f_get_Position : ( unit -> int64 ) option = None
        let mutable f_set_Position : ( int64 -> unit ) option = None
        let mutable f_get_ReadTimeout : ( unit -> int32 ) option = None
        let mutable f_set_ReadTimeout : ( int32 -> unit ) option = None
        let mutable f_get_WriteTimeout : ( unit -> int32 ) option = None
        let mutable f_set_WriteTimeout : ( int32 -> unit ) option = None
        let mutable f_Close  : ( unit -> unit ) option = None
        let mutable f_Flush  : ( unit -> unit ) option = None
        let mutable f_FlushAsync  : ( CancellationToken -> Task ) option = None
        let mutable f_ReadAsync  : ( ( byte[] * int32 * int32 * CancellationToken ) -> Task<int32> ) option = None
        let mutable f_Read  : ( ( byte[] * int32 * int32 ) -> int32 ) option = None
        let mutable f_Seek : ( ( int64 * SeekOrigin ) -> int64 ) option = None
        let mutable f_SetLength  : ( int64 -> unit ) option = None
        let mutable f_Write  : ( byte[] * int32 * int32 -> unit ) option = None
        let mutable f_WriteAsync  : ( byte[] * int32 * int32 * CancellationToken -> Task ) option = None
    
        member _.p_get_CanRead with set v = f_get_CanRead <- Some( v )
        member _.p_get_CanSeek with set v = f_get_CanSeek <- Some( v )
        member _.p_get_CanTimeout with set v = f_get_CanTimeout <- Some( v )
        member _.p_get_CanWrite with set v = f_get_CanWrite <- Some( v )
        member _.p_get_Length with set v = f_get_Length <- Some( v )
        member _.p_get_Position with set v = f_get_Position <- Some( v )
        member _.p_set_Position with set v = f_set_Position <- Some( v )
        member _.p_get_ReadTimeout with set v = f_get_ReadTimeout <- Some( v )
        member _.p_set_ReadTimeout with set v = f_set_ReadTimeout <- Some( v )
        member _.p_get_WriteTimeout with set v = f_get_WriteTimeout <- Some( v )
        member _.p_set_WriteTimeout with set v = f_set_WriteTimeout <- Some( v )
        member _.p_Close with set v = f_Close <- Some( v )
        member _.p_Flush with set v = f_Flush <- Some( v )
        member _.p_FlushAsync with set v = f_FlushAsync <- Some( v )
        member _.p_Read with set v = f_Read <- Some( v )
        member _.p_ReadAsync with set v = f_ReadAsync <- Some( v )
        member _.p_Seek with set v = f_Seek <- Some( v )
        member _.p_SetLength with set v = f_SetLength <- Some( v )
        member _.p_Write with set v = f_Write <- Some( v )
        member _.p_WriteAsync with set v = f_WriteAsync <- Some( v )

        override _.CanRead with get() =
            match f_get_CanRead with
            | Some x -> x()
            | None -> true
        override _.CanSeek with get() =
            match f_get_CanSeek with
            | Some x -> x()
            | None -> true
        override _.CanTimeout with get() =
            match f_get_CanTimeout with
            | Some x -> x()
            | None -> true
        override _.CanWrite with get() =
            match f_get_CanWrite with
            | Some x -> x()
            | None -> true
        override _.Length with get() = f_get_Length.Value()
        override _.Position with get() = f_get_Position.Value()
                            and  set v = f_set_Position.Value v
        override _.ReadTimeout with get() = f_get_ReadTimeout.Value()
                               and  set v = f_set_ReadTimeout.Value v
        override _.WriteTimeout with get() = f_get_WriteTimeout.Value()
                                and  set v = f_set_WriteTimeout.Value v

        override _.Close() : unit =
            f_Close.Value()
        override _.Flush() : unit =
            f_Flush.Value()
        override _.FlushAsync( c : CancellationToken ) : Task =
            f_FlushAsync.Value( c )
        override _.Read ( buffer : byte[], offset : int32, count : int32 ) : int32 =
            f_Read.Value( buffer, offset, count )
        override _.ReadAsync ( buffer : byte[], offset : int32, count : int32, c : CancellationToken ) : Task<int32> =
            f_ReadAsync.Value( buffer, offset, count, c )
        override _.Seek( offset : int64, origin : SeekOrigin ) : int64 =
            f_Seek.Value( offset, origin )
        override _.SetLength( value : int64 ) : unit =
            f_SetLength.Value( value )
        override _.Write ( buffer : byte[], offset : int32, count : int32 ) : unit =
            f_Write.Value( buffer, offset, count )
        override _.WriteAsync ( buffer : byte[], offset : int32, count : int32, c : CancellationToken ) : Task =
            f_WriteAsync.Value( buffer, offset, count, c )


type FileAccessorTest() =

    [<Fact>]
    member _.Constractor_InvalidFileName_001() =
        Assert.Throws< FileNotFoundException >( fun () ->
            FileAccessor( "", 1u, true ) |> ignore
        ) |> ignore

    [<Fact>]
    member _.Constractor_InvalidFileName_002() =
        let fname = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString( "N" ) )
        if File.Exists fname then
            File.Delete fname

        Assert.Throws< FileNotFoundException >( fun () ->
            FileAccessor( fname, 1u, true ) |> ignore
        ) |> ignore

    [<Fact>]
    member _.Constractor_InvalidMultiplicity_001() =
        let fname = Path.GetTempFileName()
        Assert.Throws< ArgumentException >( fun () ->
            FileAccessor( fname, 0u, true ) |> ignore
        ) |> ignore
        File.Delete fname

    [<Fact>]
    member _.Constractor_InvalidMultiplicity_002() =
        let fname = Path.GetTempFileName()
        Assert.Throws< ArgumentException >( fun () ->
            FileAccessor( fname, Constants.LU_MAX_MULTIPLICITY + 1u, true ) |> ignore
        ) |> ignore
        File.Delete fname

    [<Theory>]
    [<InlineData( true, FileAccess.Read )>]
    [<InlineData( false, FileAccess.ReadWrite )>]
    member _.Constractor_OpenFailed_001 ( flg : bool ) ( exfa : FileAccess ) =
        let fname = Path.GetTempFileName()
        let f ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
            Assert.StrictEqual( fn, fname )
            Assert.StrictEqual( fa, exfa )
            raise <| IOException "xxx" 

        let e =
            Assert.Throws< IOException >( fun () ->
                FileAccessor( fname, 1u, flg, f ) |> ignore
            )
        Assert.StartsWith( "xxx", e.Message )
        File.Delete fname

    [<Fact>]
    member _.Constractor_Open_001() =
        let fname = Path.GetTempFileName()
        let mutable cnt = 0
        let f ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
            cnt <- cnt + 1
            new Stream_Stub()

        let _ = FileAccessor( fname, Constants.LU_MAX_MULTIPLICITY, true, f )
        Assert.StrictEqual( int32 Constants.LU_MAX_MULTIPLICITY, cnt )
        File.Delete fname

    [<Theory>]
    [<InlineData( 0UL, 0UL, 1 )>]
    [<InlineData( 0UL, 1UL, 0 )>]
    [<InlineData( 0UL, 0xFFFFFFFFFFFFFFFFUL, 1 )>]
    [<InlineData( 16UL, 0UL, 17 )>]
    [<InlineData( 16UL, 16UL, 1 )>]
    [<InlineData( 16UL, 17UL, 0 )>]
    member _.Read_InvalidArg_001 ( fsize : uint64 ) ( pos : uint64 ) ( len : int32 ) =
        task {
            let fname = Path.GetTempFileName()
            let f = File.OpenWrite fname
            f.SetLength( int64 fsize )
            f.Close()
            f.Dispose()

            let fa = FileAccessor( fname, 1u, true )
            let buf = Array.zeroCreate<byte> len
            let! _ = Assert.ThrowsAsync< ArgumentOutOfRangeException >( fun () -> task {
                do! fa.Read pos ( ArraySegment buf )
            } )
            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( 0UL, 0UL, 0 )>]
    [<InlineData( 4096UL, 0UL, 16 )>]
    [<InlineData( 4096UL, 4080UL, 16 )>]
    [<InlineData( 4096UL, 0UL, 4096 )>]
    member _.Read_Success_001 ( fsize : uint64 ) ( pos : uint64 ) ( len : int32 ) =
        task {
            let fname = Path.GetTempFileName()
            let wbuf = Array.zeroCreate<byte>( int32 fsize )
            Random.Shared.NextBytes wbuf
            File.WriteAllBytes( fname, wbuf )

            let fa = FileAccessor( fname, 1u, true )

            let wbuf2 = Array.zeroCreate<byte>( len + 20 )
            do! fa.Read pos ( ArraySegment( wbuf2, 10, len ) )

            for i = 0 to len - 1 do
                Assert.StrictEqual( wbuf2.[ i + 10 ], wbuf.[ i + int32 pos ] )

            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( true )>]
    [<InlineData( false )>]
    member _.Read_TooManyDuplicate_001 ( flg : bool ) =
        task {
            let sm = new SemaphoreSlim( 1 )
            sm.Wait()
            let mutable cnt = 0

            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_ReadAsync = ( fun ( _, _, _, _ ) -> task {
                        Interlocked.Increment &cnt |> ignore
                        do! sm.WaitAsync()
                        return 1
                    }),
                    p_get_Length = ( fun () -> 1L ),
                    p_Seek = ( fun ( _, _ ) -> 0L ),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            File.WriteAllBytes( fname, [| 0uy |] )
            let fa = FileAccessor( fname, Constants.LU_MAX_MULTIPLICITY, true, fc )

            for _ = 1 to int32 Constants.LU_MAX_MULTIPLICITY do
                fun () ->
                    task {
                        let buf = Array.zeroCreate<byte> 1
                        if flg then
                            do! fa.Read 0UL ( ArraySegment buf )
                        else
                            do! fa.ReadWithPseudoLimit 4096UL 0UL ( ArraySegment buf )
                    }
                |> Functions.StartTask

            do! Task.Delay 5
            while cnt < int32 Constants.LU_MAX_MULTIPLICITY do
                do! Task.Delay 5

            let! e =
                Assert.ThrowsAsync< InvalidOperationException >( fun () -> task {
                    let buf = Array.zeroCreate<byte> 1
                    do! fa.Read 0UL ( ArraySegment buf )
                } )
            Assert.StartsWith( "No available stream", e.Message )

            for _ = 1 to int32 Constants.LU_MAX_MULTIPLICITY do
                sm.Release() |> ignore
            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( true )>]
    [<InlineData( false )>]
    member _.Read_GetLengthError_001 ( flg : bool ) =
        task {
            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_get_Length = ( fun () -> raise <| IOException "aaa" ),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, true, fc )
            let buf = Array.zeroCreate<byte> 1

            let! e =
                Assert.ThrowsAsync< IOException >( fun () -> task {
                    if flg then
                        do! fa.Read 0UL ( ArraySegment buf )
                    else
                        do! fa.ReadWithPseudoLimit 4096UL 0UL ( ArraySegment buf )

                } )
            Assert.StartsWith( "aaa", e.Message )

            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( true )>]
    [<InlineData( false )>]
    member _.Read_SeekError_001 ( flg : bool ) =
        task {
            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_get_Length = ( fun () -> 1L ),
                    p_Seek = ( fun ( _, _ ) -> raise <| IOException "bbb" ),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, true, fc )
            let buf = Array.zeroCreate<byte> 1

            let! e =
                Assert.ThrowsAsync< IOException >( fun () -> task {
                    if flg then
                        do! fa.Read 0UL ( ArraySegment buf )
                    else
                        do! fa.ReadWithPseudoLimit 4096UL 0UL ( ArraySegment buf )
                } )
            Assert.StartsWith( "bbb", e.Message )

            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( true )>]
    [<InlineData( false )>]
    member _.Read_ReadError_001 ( flg : bool ) =
        task {
            let mutable cnt = 0
            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_get_Length = ( fun () -> 1L ),
                    p_Seek = ( fun ( _, _ ) -> 0L ),
                    p_ReadAsync = ( fun ( _, _, _, _ ) -> task {
                        cnt <- cnt + 1
                        raise <| Exception( "gggg" )
                        return 1
                    }),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, true, fc )

            let! e =
                Assert.ThrowsAsync< IOException >( fun () -> task {
                    let buf = Array.zeroCreate<byte> 1
                    if flg then
                        do! fa.Read 0UL ( ArraySegment buf )
                    else
                        do! fa.ReadWithPseudoLimit 4096UL 0UL ( ArraySegment buf )
                } )
            Assert.StartsWith( "gggg", e.Message )
            Assert.StrictEqual( 1, cnt )

            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( true )>]
    [<InlineData( false )>]
    member _.Read_ReadError_002 ( flg : bool ) =
        task {
            let mutable cnt = 0
            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_get_Length = ( fun () -> 1L ),
                    p_Seek = ( fun ( _, _ ) -> 0L ),
                    p_ReadAsync = ( fun ( _, _, _, _ ) -> task {
                        cnt <- cnt + 1
                        if cnt < 3 then
                            raise <| IOException( "aaa" )
                        else
                            raise <| Exception( "bbb" )
                        return 1
                    }),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, true, fc )

            let! e =
                Assert.ThrowsAsync< IOException >( fun () -> task {
                    let buf = Array.zeroCreate<byte> 1
                    if flg then
                        do! fa.Read 0UL ( ArraySegment buf )
                    else
                        do! fa.ReadWithPseudoLimit 4096UL 0UL ( ArraySegment buf )
                } )
            Assert.StartsWith( "bbb", e.Message )
            Assert.StrictEqual( 3, cnt )

            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( true )>]
    [<InlineData( false )>]
    member _.Read_ReadAfterClose_001 ( flg : bool ) =
        task {
            let fname = Path.GetTempFileName()
            File.WriteAllBytes( fname, [| 0uy |] )
            let fa = FileAccessor( fname, 1u, true )
            fa.Close()
            let! _ =
                Assert.ThrowsAsync< InvalidOperationException >( fun () -> task {
                    let buf = Array.zeroCreate<byte> 1
                    if flg then
                        do! fa.Read 0UL ( ArraySegment buf )
                    else
                        do! fa.ReadWithPseudoLimit 4096UL 0UL ( ArraySegment buf )
                } )
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( 0x8000000000000000UL, 0UL, 0 )>]
    [<InlineData( 0x7FFFFFFFFFFFFFFFUL, 0x8000000000000000UL, 0 )>]
    [<InlineData( 0x7FFFFFFFFFFFFFFFUL, 0x7FFFFFFFFFFFFFFFUL, 1 )>]
    [<InlineData( 0UL, 1UL, 0 )>]
    [<InlineData( 0UL, 0UL, 1 )>]
    [<InlineData( 10UL, 0UL, 11 )>]
    [<InlineData( 10UL, 1UL, 10 )>]
    [<InlineData( 10UL, 10UL, 1 )>]
    [<InlineData( 10UL, 11UL, 0 )>]
    member _.ReadWithPseudoLimit_InvalidArg_001 ( pssize : uint64 ) ( pos : uint64 ) ( len : int32 ) =
        task {
            let fname = Path.GetTempFileName()
            let f = File.OpenWrite fname
            f.Close()
            f.Dispose()

            let fa = FileAccessor( fname, 1u, true )
            let buf = Array.zeroCreate<byte> len
            let! _ = Assert.ThrowsAsync< ArgumentOutOfRangeException >( fun () -> task {
                do! fa.ReadWithPseudoLimit pssize pos ( ArraySegment buf )
            } )
            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( 0x7FFFFFFFFFFFFFFFUL, 0x7FFFFFFFFFFFFFFFUL, 0 )>]
    [<InlineData( 0x7FFFFFFFFFFFFFFFUL, 0x7FFFFFFFFFFFFFFEUL, 1 )>]
    [<InlineData( 0UL, 0UL, 0 )>]
    [<InlineData( 10UL, 0UL, 10 )>]
    [<InlineData( 10UL, 1UL, 9 )>]
    [<InlineData( 10UL, 10UL, 0 )>]
    member _.ReadWithPseudoLimit_Success_001 ( pssize : uint64 ) ( pos : uint64 ) ( len : int32 ) =
        task {
            let fname = Path.GetTempFileName()
            File.WriteAllBytes( fname, [||] )

            let fa = FileAccessor( fname, 1u, true )

            let wbuf2 = Array.zeroCreate<byte>( len + 20 )
            do! fa.ReadWithPseudoLimit pssize pos ( ArraySegment( wbuf2, 10, len ) )

            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( 0UL, 0UL, 0 )>]
    [<InlineData( 4096UL, 0UL, 16 )>]
    [<InlineData( 4096UL, 4080UL, 16 )>]
    [<InlineData( 4096UL, 0UL, 4096 )>]
    member _.ReadWithPseudoLimit_Success_002 ( fsize : uint64 ) ( pos : uint64 ) ( len : int32 ) =
        task {
            let fname = Path.GetTempFileName()
            let wbuf = Array.zeroCreate<byte>( int32 fsize )
            Random.Shared.NextBytes wbuf
            File.WriteAllBytes( fname, wbuf )

            let fa = FileAccessor( fname, 1u, true )

            let wbuf2 = Array.zeroCreate<byte>( len + 20 )
            do! fa.ReadWithPseudoLimit 65536UL pos ( ArraySegment( wbuf2, 10, len ) )

            for i = 0 to len - 1 do
                Assert.StrictEqual( wbuf2.[ i + 10 ], wbuf.[ i + int32 pos ] )

            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.ReadWithPseudoLimit_Success_003 () =
        task {
            let fname = Path.GetTempFileName()
            let wbuf = Array.zeroCreate<byte>( 4096 )
            Random.Shared.NextBytes wbuf
            File.WriteAllBytes( fname, wbuf )

            let fa = FileAccessor( fname, 1u, true )
            let wbuf2 = Array.zeroCreate<byte>( 32 )
            do! fa.ReadWithPseudoLimit 65536UL 4080UL ( ArraySegment( wbuf2, 0, 32 ) )

            for i = 0 to 15 do
                Assert.StrictEqual( wbuf.[ 4080 + i ], wbuf2.[i] )
            for i = 16 to 31 do
                Assert.StrictEqual( 0uy, wbuf2.[i] )

            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.ReadWithPseudoLimit_Success_004 () =
        task {
            let fname = Path.GetTempFileName()
            let wbuf = Array.zeroCreate<byte>( 4096 )
            Random.Shared.NextBytes wbuf
            File.WriteAllBytes( fname, wbuf )

            let fa = FileAccessor( fname, 1u, true )
            let wbuf2 = Array.zeroCreate<byte>( 32 )
            do! fa.ReadWithPseudoLimit 65536UL 4096UL ( ArraySegment( wbuf2, 0, 32 ) )

            for i = 0 to 31 do
                Assert.StrictEqual( 0uy, wbuf2.[i] )

            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( 0UL, 0UL, 1 )>]
    [<InlineData( 0UL, 1UL, 0 )>]
    [<InlineData( 0UL, 0xFFFFFFFFFFFFFFFFUL, 1 )>]
    [<InlineData( 16UL, 0UL, 17 )>]
    [<InlineData( 16UL, 16UL, 1 )>]
    [<InlineData( 16UL, 17UL, 0 )>]
    member _.Write_InvalidArg_001 ( fsize : uint64 ) ( pos : uint64 ) ( len : int32 ) =
        task {
            let fname = Path.GetTempFileName()
            let f = File.OpenWrite fname
            f.SetLength( int64 fsize )
            f.Close()
            f.Dispose()

            let fa = FileAccessor( fname, 1u, false )
            let buf = Array.zeroCreate<byte> len
            let! _ =
                Assert.ThrowsAsync< ArgumentOutOfRangeException >( fun () -> task {
                    do! fa.Write pos ( ArraySegment buf )
                } )
            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( 0UL, 0UL, 0 )>]
    [<InlineData( 4096UL, 0UL, 16 )>]
    [<InlineData( 4096UL, 4080UL, 16 )>]
    [<InlineData( 4096UL, 0UL, 4096 )>]
    member _.Write_Success_001 ( fsize : uint64 ) ( pos : uint64 ) ( len : int32 ) =
        task {
            let fname = Path.GetTempFileName()

            let wbuf = Array.zeroCreate<byte>( int32 fsize )
            Random.Shared.NextBytes wbuf
            File.WriteAllBytes( fname, wbuf )

            let wbuf2 = Array.zeroCreate<byte>( len + 20 )
            Random.Shared.NextBytes wbuf2

            let fa = FileAccessor( fname, 1u, false )
            do! fa.Write pos ( ArraySegment( wbuf2, 10, len ) )
            fa.Close()

            for i = 0 to len - 1 do
                wbuf.[ i + int32 pos ] <- wbuf2.[ i + 10 ]
            let wbuf3 = File.ReadAllBytes( fname )
            Assert.True(( wbuf = wbuf3 ))

            File.Delete fname
        }

    [<Fact>]
    member _.Write_ReadOnly_001 () =
        task {
            let fname = Path.GetTempFileName()

            let fa = FileAccessor( fname, 1u, true )
            let! e =
                Assert.ThrowsAsync< InvalidOperationException >( fun () -> task {
                    do! fa.Write 0UL ( ArraySegment() )
                } )
            Assert.StartsWith( "File opened read-only", e.Message )
            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.Write_TooManyDuplicate_001 () =
        task {
            let sm = new SemaphoreSlim( 1 )
            sm.Wait()
            let mutable cnt = 0

            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_WriteAsync = ( fun ( _, _, _, _ ) -> task {
                        Interlocked.Increment &cnt |> ignore
                        do! sm.WaitAsync()
                        return 1
                    }),
                    p_FlushAsync = ( fun _ -> Task.FromResult() ),
                    p_get_Length = ( fun () -> 1L ),
                    p_Seek = ( fun ( _, _ ) -> 0L ),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            File.WriteAllBytes( fname, [| 0uy |] )
            let fa = FileAccessor( fname, Constants.LU_MAX_MULTIPLICITY, false, fc )

            for _ = 1 to int32 Constants.LU_MAX_MULTIPLICITY do
                fun () ->
                    task {
                        let buf = Array.zeroCreate<byte> 1
                        do! fa.Write 0UL ( ArraySegment buf )
                    }
                |> Functions.StartTask

            do! Task.Delay 5
            while cnt < int32 Constants.LU_MAX_MULTIPLICITY do
                do! Task.Delay 5

            let! e =
                Assert.ThrowsAsync< InvalidOperationException >( fun () -> task {
                    let buf = Array.zeroCreate<byte> 1
                    do! fa.Write 0UL ( ArraySegment buf )
                } )
            Assert.StartsWith( "No available stream", e.Message )

            for _ = 1 to int32 Constants.LU_MAX_MULTIPLICITY do
                sm.Release() |> ignore
            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.Write_GetLengthError_001 () =
        task {
            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_get_Length = ( fun () -> raise <| IOException "aaa" ),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false, fc )
            let buf = Array.zeroCreate<byte> 1

            let! e =
                Assert.ThrowsAsync< IOException >( fun () -> task {
                    let! _ = fa.Write 0UL ( ArraySegment buf )
                    ()
                } )
            Assert.StartsWith( "aaa", e.Message )

            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.Write_SeekError_001 () =
        task {
            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_get_Length = ( fun () -> 1L ),
                    p_Seek = ( fun ( _, _ ) -> raise <| IOException "bbb" ),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false, fc )
            let buf = Array.zeroCreate<byte> 1

            let! e =
                Assert.ThrowsAsync< IOException >( fun () -> task {
                    do! fa.Write 0UL ( ArraySegment buf )
                } )
            Assert.StartsWith( "bbb", e.Message )

            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.Write_WriteError_001 () =
        task {
            let mutable cnt = 0
            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_get_Length = ( fun () -> 1L ),
                    p_Seek = ( fun ( _, _ ) -> 0L ),
                    p_WriteAsync = ( fun ( _, _, _, _ ) -> task {
                        cnt <- cnt + 1
                        raise <| Exception( "gggg" )
                        return 1
                    }),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false, fc )
            let! e =
                Assert.ThrowsAsync< IOException >( fun () -> task {
                    let buf = Array.zeroCreate<byte> 1
                    do! fa.Write 0UL ( ArraySegment buf )
                } )
            Assert.StartsWith( "gggg", e.Message )
            Assert.StrictEqual( 1, cnt )

            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.Write_WriteError_002 () =
        task {
            let mutable cnt = 0
            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_get_Length = ( fun () -> 1L ),
                    p_Seek = ( fun ( _, _ ) -> 0L ),
                    p_WriteAsync = ( fun ( _, _, _, _ ) -> task {
                        cnt <- cnt + 1
                        if cnt < 3 then
                            raise <| IOException( "aaa" )
                        else
                            raise <| Exception( "bbb" )
                        return 1
                    }),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false, fc )

            let! e =
                Assert.ThrowsAsync< IOException >( fun () -> task {
                    let buf = Array.zeroCreate<byte> 1
                    do! fa.Write 0UL ( ArraySegment buf )
                } )
            Assert.StartsWith( "bbb", e.Message )
            Assert.StrictEqual( 3, cnt )

            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.Write_WriteAfterClose_001 () =
        task {
            let fname = Path.GetTempFileName()
            File.WriteAllBytes( fname, [| 0uy |] )
            let fa = FileAccessor( fname, 1u, false )
            fa.Close()
            let! _ =
                Assert.ThrowsAsync< InvalidOperationException >( fun () -> task {
                    let buf = Array.zeroCreate<byte> 1
                    do! fa.Write 0UL ( ArraySegment buf )
                } )
            File.Delete fname
        }

    [<Fact>]
    member _.SetFileSize_InvalidArg_001 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            try
                do! fa.SetFileSize 0x8000000000000000UL
                Assert.Fail __LINE__
            with
            | :? ArgumentOutOfRangeException ->
                ()
            | _ ->
                Assert.Fail __LINE__
            fa.Close()
            File.Delete fname
        }

    [<Theory>]
    [<InlineData( 0UL, 0UL )>]
    [<InlineData( 0UL, 4096UL )>]
    [<InlineData( 4096UL, 0UL )>]
    member _.SetFileSize_Success_001 ( fsizea : uint64 ) ( fsizeb : uint64 ) =
        task {
            let fname = Path.GetTempFileName()
            let f = File.OpenWrite fname
            f.SetLength( int64 fsizea )
            f.Close()
            f.Dispose()
            Assert.StrictEqual( int64 fsizea, ( FileInfo fname ).Length )

            let fa = FileAccessor( fname, 1u, false )
            do! fa.SetFileSize fsizeb
            fa.Close()

            Assert.StrictEqual( int64 fsizeb, ( FileInfo fname ).Length )
            File.Delete fname
        }

    [<Fact>]
    member _.SetFileSize_ReadOnly_001 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, true )
            let! _ =
                Assert.ThrowsAsync< InvalidOperationException >( fun () -> task {
                    do! fa.SetFileSize 1UL
                } )
            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.SetFileSize_TooManyDuplicate_001 () =
        task {
            let sm = new SemaphoreSlim( 1 )
            sm.Wait()
            let mutable cnt = 0

            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_SetLength = ( fun ( _ ) -> 
                        Interlocked.Increment &cnt |> ignore
                        sm.Wait()
                    ),
                    p_FlushAsync = ( fun _ -> Task.FromResult() ),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, Constants.LU_MAX_MULTIPLICITY, false, fc )

            for _ = 1 to int32 Constants.LU_MAX_MULTIPLICITY do
                fun () ->
                    task {
                        do! fa.SetFileSize 1UL
                    }
                |> Functions.StartTask

            do! Task.Delay 5
            while cnt < int32 Constants.LU_MAX_MULTIPLICITY do
                do! Task.Delay 5

            let! e =
                Assert.ThrowsAsync< InvalidOperationException >( fun () -> task {
                    do! fa.SetFileSize 1UL
                } )
            Assert.StartsWith( "No available stream", e.Message )

            for _ = 1 to int32 Constants.LU_MAX_MULTIPLICITY do
                sm.Release() |> ignore
            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.SetFileSize_SetLengthError_001 () =
        task {
            let mutable cnt = 0
            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_SetLength = ( fun _ ->
                        cnt <- cnt + 1
                        raise <| Exception( "gggg" )
                    ),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false, fc )

            let! e =
                Assert.ThrowsAsync< IOException >( fun () -> task {
                    do! fa.SetFileSize 1UL
                } )
            Assert.StartsWith( "gggg", e.Message )
            Assert.StrictEqual( 1, cnt )

            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.SetFileSize_SetLengthError_002 () =
        task {
            let mutable cnt = 0
            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_SetLength = ( fun _ ->
                        cnt <- cnt + 1
                        if cnt < 3 then
                            raise <| IOException( "aaa" )
                        else
                            raise <| Exception( "bbb" )
                    ),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false, fc )
            let! e =
                Assert.ThrowsAsync< IOException >( fun () -> task {
                    do! fa.SetFileSize 0UL
                } )
            Assert.StartsWith( "bbb", e.Message )
            Assert.StrictEqual( 3, cnt )

            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.SetFileSize_WriteAfterClose_001 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, false )
            fa.Close()

            let! _ =
                Assert.ThrowsAsync< InvalidOperationException >( fun () -> task {
                    do! fa.SetFileSize 0UL
                } )

            File.Delete fname
        }

    [<Theory>]
    [<InlineData( 0UL )>]
    [<InlineData( 4096UL )>]
    member _.GetFileSize_Success_001 ( fsizea : uint64 ) =
        task {
            let fname = Path.GetTempFileName()
            let f = File.OpenWrite fname
            f.SetLength( int64 fsizea )
            f.Close()
            f.Dispose()
            Assert.StrictEqual( int64 fsizea, ( FileInfo fname ).Length )

            let fa = FileAccessor( fname, 1u, true )
            let r = fa.GetFileSize()
            Assert.StrictEqual( fsizea, r )

            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.GetFileSize_TooManyDuplicate_001 () =
        task {
            ThreadPool.SetMinThreads( int32 Constants.LU_MAX_MULTIPLICITY + 1, int32 Constants.LU_MAX_MULTIPLICITY + 1 ) |> ignore
            let sm = new SemaphoreSlim( 1 )
            sm.Wait()
            let mutable cnt = 0

            let fc ( fn : string ) ( fm : FileMode ) ( fa : FileAccess ) ( fs : FileShare ) : Stream =
                new Stream_Stub(
                    p_get_Length = ( fun ( _ ) -> 
                        Interlocked.Increment &cnt |> ignore
                        sm.Wait()
                        0L
                    ),
                    p_Close = ( fun _ -> () )
                )

            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, Constants.LU_MAX_MULTIPLICITY, false, fc )

            for _ = 1 to int32 Constants.LU_MAX_MULTIPLICITY do
                fun () ->
                    task {
                        do! Task.Yield()
                        let _ = fa.GetFileSize()
                        ()
                    }
                |> Functions.StartTask

            do! Task.Delay 5
            while cnt < int32 Constants.LU_MAX_MULTIPLICITY do
                do! Task.Delay 5

            let e =
                Assert.Throws< InvalidOperationException >( fun () ->
                    let _ = fa.GetFileSize()
                    ()
                )
            Assert.StartsWith( "No available stream", e.Message )

            for _ = 1 to int32 Constants.LU_MAX_MULTIPLICITY do
                sm.Release() |> ignore
            fa.Close()
            File.Delete fname
        }

    [<Fact>]
    member _.Close_001 () =
        task {
            let fname = Path.GetTempFileName()
            let fa = FileAccessor( fname, 1u, true )
            fa.Close()
            fa.Close()
            File.Delete fname
        }
