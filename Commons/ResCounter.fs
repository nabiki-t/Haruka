//=============================================================================
// Haruka Software Storage.
// ResCounter.fs : Resource counter function.
// Aggregate resource usage at regular intervals.

//=============================================================================
// Namespace declaration

namespace Haruka.Commons

//=============================================================================
// Import declaration

open System
open System.Collections.Concurrent
open System.Threading

//=============================================================================
// declaration

/// <summary>
///  Definition of record which used in ResCounter. One ResCntRec has a time stamp, a total value, a number of value.
/// </summary>
/// <param name="timeStamp">
///  Time stamp value.
/// </param>
/// <param name="initVal">
///  Initial usage value.
/// </param>
type ResCntRec( timeStamp : int64, initVal : int64 ) =

    /// Multiplicity. Indicates the degree to which addition processing is parallelized.
    /// The larger this number, the better the performance, but it will consume more memory.
    let m_ParaCnt = 8

    /// Number of elements used for one counter.
    /// Within the array, m_PadSize-1 elements always contain 0 and are not used.
    /// Specify a value greater than the number of cache line bytes divided by 8.
    let m_PadSize = 8

    /// array of time stamp. Only the first element is used.
    let m_TimeStamp : int64[] =
        let v = Array.zeroCreate< int64 > ( m_PadSize + 1 )
        v.[0] <- timeStamp
        v

    /// Array to calculate total value.
    let m_ValCounter : int64[] =
        let v = Array.zeroCreate< int64 > ( ( m_PadSize * m_ParaCnt ) + 1 )
        v.[0] <- initVal
        v

    /// Array to find the number of times values are added.
    let m_NumCounter : int64[] = 
        let v = Array.zeroCreate< int64 > ( ( m_PadSize * m_ParaCnt ) + 1 )
        v.[0] <- 1L
        v

    /// <summary>
    ///  Add resource usage value.
    /// </summary>
    /// <param name="v">
    ///  usage value
    /// </param>
    member _.Add( v : int64 ) : unit =
        let pos = ( Threading.Thread.CurrentThread.ManagedThreadId % m_ParaCnt ) * m_PadSize
        Interlocked.Add( &( m_ValCounter.[pos] ), v ) |> ignore
        Interlocked.Increment( &( m_NumCounter.[pos] ) ) |> ignore

    /// <summary>
    ///  Get time stamp value.
    /// </summary>
    member _.TimeStamp : int64 =
        m_TimeStamp.[0]

    /// <summary>
    ///  Get aggregated usage values. 
    /// </summary>
    member _.Value : int64 =
        m_ValCounter |> Array.sum

    /// <summary>
    ///  Get number of added usage values.
    /// </summary>
    member _.Count : int64 =
        m_NumCounter |> Array.sum

/// <summary>
///  Structure that represents aggregated results.
/// </summary>
[<Struct>]
type ResCountResult = {
    /// Date and time of measurement
    Time: DateTime;

    /// Aggregated value
    Value : int64;

    /// Aggregated count
    Count : int64;
}

/// <summary>
///  Aggregate recource usage value.
/// </summary>
/// <param name="spanSec">
///  Specify the unit time for aggregation in seconds.
///  If the number that is lower than 1 is specified, it is considered 1.
/// </param>
/// <param name="length">
///  Specify the period for aggregation in seconds.
///  If the number that is lower than spanSec*8 is specified, it is considered spanSec*8.
///  If the number that is upper than spanSec*1024 is specified, it is considered spanSec*1024.
/// </param>
type ResCounter( spanSec : int64, length : int64 ) =

    /// the unit time for aggregation in seconds.
    let m_SpanSec = max 1L spanSec

    /// the period for aggregation in seconds.
    let m_Length = max ( m_SpanSec * 8L ) length |> min ( m_SpanSec * 1024L )

    /// current aggregation recorde
    let m_CurrentCounter = new OptimisticLock<ResCntRec>( ResCntRec( 0L, 0L ) )

    /// aggregate result
    let m_CntHist = new ConcurrentDictionary< int64, ResCntRec >()

    /// <summary>
    ///  Add resource usage value.
    /// </summary>
    /// <param name="n">
    ///  Date time value of which resource usage was measurerd.
    /// </param>
    /// <param name="v">
    ///  usage value.
    /// </param>
    member _.AddCount ( n : DateTime ) ( v : int64 ) : unit =
        let ct = n.Ticks / TimeSpan.TicksPerSecond / m_SpanSec
        let struct( oldRec, newRec ) =
            m_CurrentCounter.Update( fun oldRec ->
                if oldRec.TimeStamp = ct then
                    oldRec.Add v
                    oldRec
                elif oldRec.TimeStamp > ct then
                    oldRec
                else
                    new ResCntRec( ct, v )
            )
        if Object.ReferenceEquals( oldRec, newRec ) |> not then
            let tv = oldRec.TimeStamp * m_SpanSec
            m_CntHist.TryAdd( tv, oldRec ) |> ignore
            let thre = tv - m_Length
            for itr in m_CntHist.Keys do
                if itr < thre then
                    m_CntHist.TryRemove( itr ) |> ignore

    /// <summary>
    ///  Get aggregate result.
    /// </summary>
    /// <param name="n">
    ///  Current time.
    /// </param>
    /// <returns>
    ///  Returns the aggregation results after current time - m_Length.
    ///  The maximum number of elements in the array is m_Length / m_SpanSec.
    ///  The number may change depending on the timing when the value was obtained.
    /// </returns>
    member _.Get ( n : DateTime ) : ResCountResult array =
        let et = n.Ticks / TimeSpan.TicksPerSecond
        let st = et - m_Length
        [|
            for itr in m_CntHist do
                if itr.Key >= st && itr.Key < et then
                    { Time= DateTime( itr.Key * TimeSpan.TicksPerSecond ); Value = itr.Value.Value; Count = itr.Value.Count }
        |]
