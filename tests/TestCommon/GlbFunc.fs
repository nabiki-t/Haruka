//=============================================================================
// Haruka Software Storage.
// Glbfunc.fs : Global functions for test.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Test

//=============================================================================
// Import declaration

open System
open System.Net
open System.Net.Sockets
open System.IO
open System.IO.Pipes
open System.IO.MemoryMappedFiles
open System.Threading
open System.Threading.Tasks
open System.Security.Cryptography

open Haruka
open Haruka.Constants
open Haruka.Commons

//=============================================================================
// Class implementation

/// <summary>
///  Exception classes used in test cases.
/// </summary>
/// <param name="argMsg">
///  Error message.
/// </param>
type TestException( argMsg : string ) =
    inherit System.Exception( argMsg )

type GlbFunc() =

    /// MemoryMappedFile used to generate unique TCP port numbers.
    static let portCounter = InterProcessCounter( "12999bce-b69b-4072-9852-52dbf49a879b" )

    /// MemoryMappedFile used to generate unique Target Device IDs.
    static let dgIdenCounter = InterProcessCounter( "224cd616-b225-485c-b20a-8150ae7def37" )

    /// MemoryMappedFile used to generate unique Initiator ISID.
    static let isidCounter = InterProcessCounter( "4c816d62-8c97-4b31-a867-3c17af26ce4b" )

    /// Gets the name of the directory in which the currently running program is stored.
    static let currentExeDir =
        let curExeName = System.Reflection.Assembly.GetEntryAssembly()
        Path.GetDirectoryName curExeName.Location

    /// The full pathname of the TargetDevice.exe file.
    static member tdExePath : string = 
        Functions.AppendPathName currentExeDir Constants.TARGET_DEVICE_EXE_NAME

    /// The full path name of the program that handles the creation of media files.
    static member imExePath : string = 
        Functions.AppendPathName currentExeDir Constants.MEDIA_CREATION_EXE_NAME

    /// The full pathname of the Controller.exe file.
    static member controllerExePath : string = 
        Functions.AppendPathName currentExeDir "Controller.exe"

    /// The full pathname of the Client.exe file.
    static member clientExePath : string = 
        Functions.AppendPathName currentExeDir "Client.exe"

    /// The environment variable name to be given when starting TestCommon.exe.
    /// Specify what processing it should act as a stub for.
    /// Possible values are "MediaCreateProcStub" or "ControllerStarter".
    static member STUB_PROC_TYPE : string = "STUB_PROC_TYPE"


    /// The environment variable name to be given when starting TestCommon.exe.
    /// Specifies the file name to which arguments given when starting the child process should be output.
    static member ARGS_DEBUG_FILE : string = "ARGS_DEBUG_FILE"


    /// The environment variable name to be given when starting TestCommon.exe.
    /// Specifies the name of the named pipe that should be connected to the standard input of the child process.
    static member STDIN_DEBUG_PIPE : string = "STDIN_DEBUG_PIPE"

    /// The environment variable name to be given when starting TestCommon.exe.
    /// Specifies the name of the named pipe that should be connected to the standard output of the child process.
    static member STDOUT_DEBUG_PIPE : string = "STDOUT_DEBUG_PIPE"

    /// The environment variable name to be given when starting TestCommon.exe.
    /// Specifies the name of the named pipe that should be connected to the standard error output of the child process.
    static member STDERR_DEBUG_PIPE : string = "STDERR_DEBUG_PIPE"

    /// The environment variable name to be given when starting TestCommon.exe.
    /// Specifies the name of the named pipe used to wait for the termination of the stub process.
    /// The stub process will terminate by writing a line of numbers to the pipe.
    /// The value written to the pipe will be used as the exit status.
    static member WAIT_DEBUG_PIPE : string = "WAIT_DEBUG_PIPE"

    /// Get the next TCP port number to use.
    static member nextTcpPortNo() : int =
        let v = portCounter.Next()
        let portNum = v % 22768UL + 10000UL
        portNum |> int


    /// Get the next Target Device ID to use.    
    static member newTargetDeviceID() : TDID_T =
        let v = dgIdenCounter.Next()
        let v2 = if v = 0UL then dgIdenCounter.Next() else v
        tdid_me.fromPrim ( uint32 v2 )


    /// Get the next Target Group ID to use.
    static member newTargetGroupID() : TGID_T =
        GlbFunc.newTargetDeviceID()
        |> tdid_me.toPrim
        |> tgid_me.fromPrim

    ///Get the next ISID to use.
    static member newISID() : ISID_T =
        let v = isidCounter.Next()
        let v2 = if v = 0UL then dgIdenCounter.Next() else v
        isid_me.fromPrim v2

    /// <summary>
    ///  Wait for a TCP connection on the specified port and return the NetworkStream.
    /// </summary>
    /// <param name="port">
    ///   The port number to listen on.
    /// </param>
    /// <returns>
    ///   The NetworkStream connected to the client.
    /// </returns>
    /// <remarks>
    ///   The address used for listening is IPv6 loopback.
    /// </remarks>
    static member WaitConnect ( port : int ) : NetworkStream =
        let m_Listener = new TcpListener( IPAddress.IPv6Loopback, port )
        m_Listener.Start ()
        let r = new NetworkStream( m_Listener.AcceptSocket () )
        m_Listener.Stop()
        r

    /// <summary>
    ///  Connect to the TCP server on the specified port and return the NetworkStream.
    /// </summary>
    /// <param name="port">
    ///   The port number to connect to.
    /// </param>
    /// <returns>
    ///   The NetworkStream connected to the server.
    /// </returns>
    /// <remarks>
    ///   The destination address is IPv6 loopback.
    /// </remarks>
    static member ConnectToServer ( port : int ) : NetworkStream =
        let rec loop cnt =
            try
                ( new TcpClient( "::1", port ) ).GetStream()
            with
            | _ as x ->
                if cnt < 100 then
                    Thread.Sleep 20
                    loop ( cnt + 1 )
                else
                    reraise ()
        loop 0

    /// <summary>
    ///  Creates a single, connected TCP connection.
    /// </summary>
    /// <returns>
    ///   A tuple containing two NetworkStreams, one for the server and one for the client.
    /// </returns>
    static member GetNetConn() : ( NetworkStream * NetworkStream ) =
        let portNo = GlbFunc.nextTcpPortNo()
        let listener = new TcpListener( IPAddress.IPv6Loopback, portNo )
        listener.Start ()
        let t1 = 
            task{
                let! s = listener.AcceptSocketAsync()
                return new NetworkStream( s )
            }
        let t2 =
            task {
                return GlbFunc.ConnectToServer( portNo )
            };
        Task.WaitAll( t1, t2 )
        listener.Stop()
        t1.Result, t2.Result

    /// <summary>
    ///  Builds multiple connected TCP connections.
    /// </summary>
    /// <param name="cnt">
    ///   The number of connections to create.
    /// </param>
    /// <returns>
    ///   A tuple containing two arrays of NetworkStreams, one for the server and one for the client.
    /// </returns>
    static member GetNetConnV( cnt : int ) : ( NetworkStream[] * NetworkStream[] ) =
        [|
            for i = 1 to cnt do
                yield GlbFunc.GetNetConn()
        |]
        |> Array.unzip

    /// <summary>
    ///  Closes the specified NetworkStreams.
    /// </summary>
    /// <param name="v">
    ///   An array of NetworkStreams to close.
    /// </param>
    static member ClosePorts( v : NetworkStream[] ) : unit =
        v
        |> Array.iter ( fun itr ->
            try
                itr.Socket.Disconnect false
                itr.Close()
                itr.Dispose()
            with
            | :? System.Net.Sockets.SocketException ->
                ()  // ignore
            | :? System.ObjectDisposedException ->
                ()  // ignore
        )

    /// <summary>
    ///  Writes the default PR file with the specified type and registrations.
    /// </summary>
    /// <param name="t">
    ///   The type of the PR file.
    /// </param>
    /// <param name="regs">
    ///   Specify the content that should be registered as RP.
    /// </param>
    /// <param name="fname">
    ///   The file name to write the PR file to.
    /// </param>
    static member writeDefaultPRFile ( t : PR_TYPE ) ( regs : ( ITNexus * RESVKEY_T * bool )[] ) ( fname : string ) : unit =
        let vstr = [|
            yield "<PRInfo>";
            yield "<Type>" + PR_TYPE.toStringName t + "</Type>";
            for itn, reskey, holder in regs do
                yield "<Registration>";
                yield "<ITNexus>";
                yield "<InitiatorName>" + itn.InitiatorName + "</InitiatorName>";
                let wisid = itn.ISID
                yield "<ISID>" + ( isid_me.toString wisid ) + "</ISID>"
                yield "<TargetName>" + itn.TargetName + "</TargetName>"
                yield "<TPGT>" + ( sprintf "%d" ( tpgt_me.toPrim itn.TPGT ) ) + "</TPGT>"
                yield "</ITNexus>";
                yield "<ReservationKey>" + ( sprintf "%d" ( resvkey_me.toPrim reskey ) ) + "</ReservationKey>"
                yield "<Holder>" + ( sprintf "%b" holder ) + "</Holder>"
                yield "</Registration>";
            yield "</PRInfo>";
        |]
        File.WriteAllLines( fname, vstr )

    /// <summary>
    ///  Delete specified file.
    /// </summary>
    /// <param name="fn">
    ///   The file name to delete.
    /// </param>
    static member DeleteFile ( fn : string ) : unit =
        let mutable cnt = 0
        while cnt < 30 do
            try
                File.Delete fn
                cnt <- 99
            with
            | :? DirectoryNotFoundException ->
                cnt <- 99
            | _ ->
                if cnt < 29 then
                    Thread.Sleep 10
                cnt <- cnt + 1

    /// <summary>
    ///  Delete specified directory and all its contents.
    /// </summary>
    /// <param name="pn">
    ///   The path name of the directory to delete.
    /// </param>
    static member DeleteDir ( pn : string ) : unit =

        let rec loop ( wpath : string ) =
            Directory.GetFiles wpath
            |> Array.iter File.Delete
            Directory.GetDirectories wpath
            |> Array.iter loop
            Directory.Delete wpath

        if Directory.Exists pn then
            let mutable cnt = 0
            while cnt < 30 do
                try
                    loop pn
                    cnt <- 999
                with
                | :? DirectoryNotFoundException ->
                    cnt <- 999
                | _ ->
                    if cnt < 999 then
                        Thread.Sleep 10
                    cnt <- cnt + 1

    /// <summary>
    ///  Create a directory with the specified path name.
    /// </summary>
    /// <param name="pn">
    ///   The path name of the directory to create.
    /// </param>
    /// <returns>
    ///   The DirectoryInfo object representing the created directory.
    /// </returns>
    /// <remarks>
    ///   If the directory already exists, it will be deleted and recreated.
    /// </remarks>
    static member CreateDir ( pn : string ) : DirectoryInfo =
        GlbFunc.DeleteDir pn
        Directory.CreateDirectory pn

    /// <summary>
    ///  Wait for the specified file to be deleted.
    /// </summary>
    /// <param name="fname">
    ///   The file name to wait for deletion.
    /// </param>
    /// <remarks>
    ///   This method will wait for up to 3 second for the file to be deleted.
    /// </remarks>
    static member WaitForFileDelete ( fname : string ) : unit =
        let mutable cnt = 0
        while ( File.Exists fname ) && cnt < 300 do
            Thread.Sleep 10
            cnt <- cnt + 1
        if File.Exists fname then
            raise <| TestException( sprintf "Timeout waiting for file deletion. %s" fname )

    /// <summary>
    ///  Wait for the specified file to be created.
    /// </summary>
    /// <param name="fname">
    ///   The file name to wait for creation.
    /// </param>
    /// <remarks>
    ///   This method will wait for up to 3 second for the file to be created.
    /// </remarks>
    static member WaitForFileCreate ( fname : string ) : unit =
        let mutable cnt = 0
        while ( fname |> File.Exists |> not ) && cnt < 300 do
            Thread.Sleep 10
            cnt <- cnt + 1
        if not ( File.Exists fname ) then
            raise <| TestException( sprintf "Timeout waiting for file creation. %s" fname )

    /// <summary>
    ///  Wait for the specified file to be updated.
    /// </summary>
    /// <param name="fname">
    ///   The file name to wait for update.
    /// </param>
    /// <param name="initFileTime">
    ///   The initial last write time of the file.
    /// </param>
    /// <remarks>
    ///   This method will wait for up to 3 second for the file to be updated.
    ///   File updates are determined by monitoring the last update date and time.
    /// </remarks>
    static member WaitForFileUpdate ( fname : string ) ( initFileTime : DateTime ) : unit =
        let mutable cnt = 0
        while ( initFileTime = File.GetLastWriteTimeUtc fname ) && cnt < 300 do
            Thread.Sleep 10
            cnt <- cnt + 1
        if cnt >= 300 then
            raise <| TestException( sprintf "Timeout waiting for file update 1. %s %s" ( initFileTime.ToString "o" ) fname )
        cnt <- 0
        while cnt >= 0 && cnt < 300 do
            try
                use s = File.OpenRead( fname )
                cnt <- -1
                s.Close()
            with
            | _ ->
                cnt <- cnt + 1
                Thread.Sleep 10
        if cnt >= 300 then
            raise <| TestException( sprintf "Timeout waiting for file update 2. %s %s" ( initFileTime.ToString "o" ) fname )

    /// <summary>
    ///  Wait until the file's hash value changes.. 
    /// </summary>
    /// <param name="fname">
    ///   The file name to wait for update.
    /// </param>
    /// <param name="initHash">
    ///   The initial hash value of the file.
    /// </param>
    /// <remarks>
    ///   This method will wait for up to 3 second for the file to be updated.
    /// </remarks>
    static member WaitForFileUpdateByHash ( fname : string ) ( initHash : byte[] ) : unit =
        let mutable cnt = 0
        while ( initHash = GlbFunc.GetFileHash fname ) && cnt < 300 do
            Thread.Sleep 10
            cnt <- cnt + 1
        if cnt >= 300 then
            raise <| TestException( sprintf "Timeout waiting for file update. %s" fname )

    /// <summary>
    ///  Construct a pair of anonymous pipes.
    /// </summary>
    /// <returns>
    ///  A tuple of the server and client streams.
    /// </returns>
    static member CreateAnonymousPipe() : ( AnonymousPipeServerStream * AnonymousPipeClientStream ) =
        let pout = new AnonymousPipeServerStream( PipeDirection.Out )
        let pin = new AnonymousPipeClientStream( PipeDirection.In, pout.ClientSafePipeHandle )
        pout, pin

    /// <summary>
    ///  Run a Task synchronously and return the result.
    /// </summary>
    /// <param name="t">
    ///   The Task to run.
    /// </param>
    /// <returns>
    ///   The result of the Task.
    /// </returns>
    static member RunSync ( t : Task<'a> ) : 'a =
        t.Result

    /// <summary>
    ///  Constructs a lock object to wait for updates to the log parameters.
    /// </summary>
    /// <returns>
    ///   A Semaphore that can be used to synchronize access to the log parameters.
    /// </returns>
    static member LogParamUpdateLock() : Mutex =
        let lock = new Mutex( false, "12bd8dc3-0be8-4803-bb8e-9bd27bd00c35" )
        try
            lock.WaitOne() |> ignore
        with :? AbandonedMutexException ->
            ()
        lock

    /// <summary>
    ///  Dispose all IDisposable objects in the specified sequence.
    /// </summary>
    /// <param name="v">
    ///   A sequence of IDisposable objects to dispose.
    /// </param>
    static member inline AllDispose ( v : IDisposable seq ) : unit =
        v |> Seq.iter ( fun itr -> itr.Dispose() )

    /// <summary>
    ///  Compare two byte arrays for equality.
    /// </summary>
    /// <param name="v1">
    ///   The first byte array.
    /// </param>
    /// <param name="s1">
    ///   The starting index in the first byte array.
    /// </param>
    /// <param name="v2">
    ///   The second byte array.
    /// </param>
    /// <param name="s2">
    ///   The starting index in the second byte array.
    /// </param>
    /// <param name="len">
    ///   The number of bytes to compare.
    /// </param>
    /// <returns>
    ///   True if the byte arrays are equal, false otherwise.
    /// </returns>
    static member Compare ( v1 : byte[] ) ( s1 : int ) ( v2 : byte[] ) ( s2 : int ) ( len : int ) : bool =
        let d = len / 8
        let mutable flg = true
        for i = 0 to d - 1 do
            let vv1 = BitConverter.ToUInt64( v1, s1 + i * 8 )
            let vv2 = BitConverter.ToUInt64( v2, s2 + i * 8 )
            flg <- flg && ( vv1 = vv2 )
        for i = d * 8 to len - 1 do
            flg <- flg && ( v1.[ s1 + i ] = v2.[ s2 + i ] )
        flg

    /// <summary>
    ///  Read a string of the specified length from the StreamReader.
    /// </summary>
    /// <param name="s">
    ///   The StreamReader to read from.
    /// </param>
    /// <param name="len">
    ///   The length of the string to read.
    /// </param>
    /// <returns>
    ///   The string read from the StreamReader.
    /// </returns>
    static member ReadString ( s : StreamReader ) ( len : int ) : string =
        let buf = Array.zeroCreate< char > len
        let mutable cnt = 0
        while cnt < len do
            let r = s.Read( buf, cnt, ( len - cnt ) )
            cnt <- cnt + r
        String( buf )

    /// <summary>
    ///  Get the hash value of the file
    /// </summary>
    /// <param name="fname">
    ///  File nmae.
    /// </param>
    /// <returns>
    ///  File hash value. If failed to read specified file, it raise TestException.
    /// </returns>
    static member GetFileHash ( fname : string ) : byte array =
        let tryReadBytes () : byte[] option =
            try
                File.ReadAllBytes fname |> Some
            with
            | _ ->
                None
        let rec loop ( cnt : int ) : byte[] option =
            if cnt >= 30 then
                None
            else
                let r = tryReadBytes()
                if r.IsSome then
                    r
                else
                    Thread.Sleep 100
                    loop ( cnt + 1 )
        match loop 0 with
        | None ->
            raise <| TestException( sprintf "Failed to read %s." fname )
        | Some( fd ) ->
            use m = MD5.Create()
            m.ComputeHash fd

