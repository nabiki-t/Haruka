//=============================================================================
// Haruka Software Storage.
// InterProcessCounter.fs : Implement a global counter across the entire system.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.IO
open System.Threading

//=============================================================================
// Class implementation

/// <summary>
///  A class that implements a global counter that spans across processes.
/// </summary>
/// <param name="m_Name">
///  Name of counter.
/// </param>
type InterProcessCounter( m_Name : string ) =

    /// File name.
    let m_FileName =
        Path.Combine( Path.GetTempPath(), m_Name )

    //// The mutex object.
    let m_Mutex =
        let mutable createdNew = false
        let mutexName = sprintf @"Global\%s_mutex" m_Name
        let m = new Mutex( true, mutexName, &createdNew )
        if createdNew then
          try
              File.Delete( m_FileName )
          finally
              m.ReleaseMutex()
        m

    /// <summary>
    ///  Get the lock with a mutex
    /// </summary>
    /// <param name="m">
    ///  The mutext object.
    /// </param>
    let safeWait ( m: Mutex ) =
        try
            m.WaitOne() |> ignore
        with :? AbandonedMutexException ->
            ()

    /// <summary>
    ///  Get next counter value.
    /// </summary>
    member _.Next() : uint64 =
        safeWait( m_Mutex )
        try
            use f = File.Open( m_FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None )
            let buf = Array.zeroCreate<byte> 8
            let currentVal = 
                try
                    f.ReadExactly( buf )
                    BitConverter.ToUInt64 buf
                with
                | _ ->
                    0UL
            let nextVal = currentVal + 1UL
            f.Seek( 0L, SeekOrigin.Begin ) |> ignore
            BitConverter.TryWriteBytes( buf, nextVal ) |> ignore
            f.Write( buf )
            nextVal
        finally
            m_Mutex.ReleaseMutex()
