//=============================================================================
// Haruka Software Storage.
// GraphWriter.fs : Load icon images that used for Haruka client GUI.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.Windows
open System.Windows.Markup
open System.Windows.Media
open System.Windows.Shapes
open System.Windows.Controls
open System.Collections.Generic

open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes

/// Constant values that specify the color used as graph drawing.
type GraphColor =
    | GC_RED = 0
    | GC_YELLOW = 1
    | GC_GREEN = 2
    | GC_CYAN = 3
    | GC_BLUE = 4
    | GC_PURPLE = 5

/// <summary>
///  This class imprements functions to write a graph at a specified canvas control.
/// </summary>
/// <param name="m_GraphCanvas">
///  The canvas control where is written the graph.
/// </param>
/// <param name="m_ColorIndex">
///  Graph color.
/// </param>
/// <param name="m_PointCnt">
///  Graph data count.
/// </param>
type GraphWriter(
    m_GraphCanvas : Canvas,
    m_ColorIndex : GraphColor,
    m_PointCnt : int
) as this =

    let m_AllColors = [|
        SolidColorBrush( Color.FromRgb( 0xF0uy, 0x60uy, 0x60uy ) );
        SolidColorBrush( Color.FromRgb( 0xE0uy, 0xE0uy, 0x00uy ) );
        SolidColorBrush( Color.FromRgb( 0x60uy, 0xF0uy, 0x60uy ) );
        SolidColorBrush( Color.FromRgb( 0x00uy, 0xE0uy, 0xE0uy ) );
        SolidColorBrush( Color.FromRgb( 0x60uy, 0x60uy, 0xF0uy ) );
        SolidColorBrush( Color.FromRgb( 0xE0uy, 0x00uy, 0xE0uy ) );
    |]

    let m_AllLightColors = [|
        SolidColorBrush( Color.FromRgb( 0xF0uy, 0xE0uy, 0xE0uy ) );
        SolidColorBrush( Color.FromRgb( 0xF0uy, 0xF0uy, 0xD0uy ) );
        SolidColorBrush( Color.FromRgb( 0xE0uy, 0xF0uy, 0xE0uy ) );
        SolidColorBrush( Color.FromRgb( 0xD0uy, 0xF0uy, 0xF0uy ) );
        SolidColorBrush( Color.FromRgb( 0xE0uy, 0xE0uy, 0xF0uy ) );
        SolidColorBrush( Color.FromRgb( 0xF0uy, 0xD0uy, 0xF0uy ) );
    |]

    let m_GraphColor_Dark = m_AllColors.[ int m_ColorIndex ]
    let m_GraphColor_Light = m_AllLightColors.[ int m_ColorIndex ]
    let m_GraphLeftBorder = new Line( Stroke = m_GraphColor_Dark, StrokeThickness = 0.8 )
    let m_GraphTopBorder = new Line( Stroke = m_GraphColor_Dark, StrokeThickness = 0.8 )
    let m_GraphRightBorder = new Line( Stroke = m_GraphColor_Dark, StrokeThickness = 0.8 )
    let m_GraphBottomBorder = new Line( Stroke = m_GraphColor_Dark, StrokeThickness = 0.8 )
    let m_HInsideLine = [|
        for j = 0 to 8 do
            yield new Line( Stroke = m_GraphColor_Light, StrokeThickness = 0.4 )
    |]
    let m_GraphPolygon =
        new Polygon(
            Stroke = m_GraphColor_Dark,
            Fill = new SolidColorBrush( Color = m_GraphColor_Light.Color, Opacity = 0.3 ),
            StrokeThickness = 0.8
        )
    let m_GraphPolygon_PointCol = new PointCollection( m_PointCnt - 1 )

    let m_Values = Array.zeroCreate< float >( m_PointCnt )

    do
        m_GraphCanvas.Children.Add m_GraphLeftBorder |> ignore
        m_GraphCanvas.Children.Add m_GraphTopBorder |> ignore
        m_GraphCanvas.Children.Add m_GraphRightBorder |> ignore
        m_GraphCanvas.Children.Add m_GraphBottomBorder |> ignore
        for itr in m_HInsideLine do
            m_GraphCanvas.Children.Add itr |> ignore
        m_GraphCanvas.Children.Add m_GraphPolygon |> ignore
        for j = 0 to m_PointCnt - 1 + 3 do
            m_GraphPolygon_PointCol.Add( Point( 0.0, 0.0 ) )
        m_GraphPolygon.Points <- m_GraphPolygon_PointCol

        m_GraphCanvas.SizeChanged.AddHandler ( fun _ _ ->
            this.GraphCanvas_SizeChanges()
            this.UpdateGraph()
        )

    ///////////////////////////////////////////////////////////////////////////
    // Static public method

    /// <summary>
    ///  Calculate scale and label for "Bytes/Sec" usage data.
    /// </summary>
    /// <param name="v">
    ///  usage data
    /// </param>
    /// <returns>
    ///  Pair of scale and labal text.
    /// </returns>
    static member CalcScale_BytesPerSec ( v : float seq ) : ( float * string ) =
        let vScales = [|
            ( 262144.0, "256K Bytes/s" )
            ( 1048576.0, "1M Bytes/s" )
            ( 4194304.0, "4M Bytes/s" )
            ( 16777216.0, "16M Bytes/s" )
            ( 67108864.0, "64M Bytes/s" )
            ( 268435456.0, "256M Bytes/s" )
            ( 1073741824.0, "1G Bytes/s" )
            ( 4294967296.0, "4G Bytes/s" )
            ( 17179869184.0, "16G Bytes/s" )
            ( 68719476736.0, "64G Bytes/s" )
            ( 274877906944.0, "256G Bytes/s" )
            ( 1099511627776.0, "1T Bytes/s" )
        |]
        GraphWriter.CalcScale vScales v

    /// <summary>
    ///  Calculate scale and label for "Counts/Sec" usage data.
    /// </summary>
    /// <param name="v">
    ///  usage data
    /// </param>
    /// <returns>
    ///  Pair of scale and labal text.
    /// </returns>
    static member CalcScale_CountPerSec ( v : float seq ) : ( float * string ) =
        let vScales = [|
            ( 16.0, "16 Counts/s" )
            ( 64.0, "64 Counts/s" )
            ( 256.0, "256 Counts/s" )
            ( 1024.0, "1K Counts/s" )
            ( 4096.0, "4K Counts/s" )
            ( 16384.0, "16K Counts/s" )
            ( 65536.0, "16K Counts/s" )
            ( 262144.0, "256K Counts/s" )
            ( 1048576.0, "1M Counts/s" )
            ( 4194304.0, "4M Counts/s" )
            ( 16777216.0, "16M Counts/s" )
            ( 67108864.0, "64M Counts/s" )
        |]
        GraphWriter.CalcScale vScales v

    /// <summary>
    ///  Calculate scale and label for "milisec/Access" usage data.
    /// </summary>
    /// <param name="v">
    ///  Statistics measured in milliseconds.
    /// </param>
    /// <returns>
    ///  Pair of scale and labal text.
    /// </returns>
    static member CalcScale_MilisecPerAccess ( v : float seq ) : ( float * string ) =
        let vScales = [|
            ( 0.0001, "100 nano sec/access" )
            ( 0.001, "1 micro sec/access" )
            ( 0.010, "10 micro sec/access" )
            ( 0.100, "100 micro sec/access" )
            ( 1.0, "1 mili sec/access" )
            ( 10.0, "10 mili sec/access" )
            ( 100.0, "100 mili sec/access" )
            ( 1000.0, "1 sec/access" )
            ( 10000.0, "10 sec/access" )
            ( 100000.0, "100 sec/access" )
        |]
        GraphWriter.CalcScale vScales v

    ///////////////////////////////////////////////////////////////////////////
    // Static private method

    /// <summary>
    ///  Calculate scale and label.
    /// </summary>
    /// <param name="vScale">
    ///  The scale and label array to use.
    /// </param>
    /// <param name="v">
    ///  Statistics measured in milliseconds.
    /// </param>
    /// <returns>
    ///  Pair of scale and labal text.
    /// </returns>
    static member private CalcScale ( vScale : ( float * string ) [] ) ( v : float seq ) : ( float * string ) =
        if Seq.isEmpty v then
            vScale.[0]
        else
            v
            |> Seq.max
            |> fun m -> Seq.tryFind ( fst >> (<) m ) vScale
            |> Option.defaultValue vScale.[ vScale.Length - 1 ]

    ///////////////////////////////////////////////////////////////////////////
    // Public method

    /// <summary>
    ///  This method is called when size of the canvas is changed.
    /// </summary>
    member _.GraphCanvas_SizeChanges() : unit =
        let h = m_GraphCanvas.ActualHeight
        let w = m_GraphCanvas.ActualWidth

        m_GraphLeftBorder.X1 <- 0.0
        m_GraphLeftBorder.X2 <- 0.0
        m_GraphLeftBorder.Y1 <- 0.0
        m_GraphLeftBorder.Y2 <- h

        m_GraphRightBorder.X1 <- w
        m_GraphRightBorder.X2 <- w
        m_GraphRightBorder.Y1 <- 0.0
        m_GraphRightBorder.Y2 <- h

        m_GraphTopBorder.X1 <- 0.0
        m_GraphTopBorder.X2 <- w
        m_GraphTopBorder.Y1 <- 0.0
        m_GraphTopBorder.Y2 <- 0.0

        m_GraphBottomBorder.X1 <- 0.0
        m_GraphBottomBorder.X2 <- w
        m_GraphBottomBorder.Y1 <- h
        m_GraphBottomBorder.Y2 <- h

        for i = 1 to 9 do
            let r = m_HInsideLine. [ i - 1 ]
            r.X1 <- 0.0
            r.X2 <- w
            r.Y1 <- ( h / 10.0 ) * ( float i )
            r.Y2 <- ( h / 10.0 ) * ( float i )


    /// <summary>
    ///  Redraw the graph.
    /// </summary>
    member _.UpdateGraph() : unit =
        let h = m_GraphCanvas.ActualHeight
        let w = m_GraphCanvas.ActualWidth
        for i = 0 to m_PointCnt - 1 do
            m_GraphPolygon_PointCol.Item( i ) <-
                Point( w * float i / float( m_PointCnt - 1 ), h * ( 1.0 - m_Values.[i] ) )
        m_GraphPolygon_PointCol.Item( m_PointCnt - 1 + 1 ) <- Point( w, h )
        m_GraphPolygon_PointCol.Item( m_PointCnt - 1 + 2 ) <- Point( 0.0, h )
        m_GraphPolygon_PointCol.Item( m_PointCnt - 1 + 3 ) <- m_GraphPolygon_PointCol.Item( 0 )


    /// <summary>
    ///  Set usage data and scale factor.
    /// </summary>
    /// <param name="argv">
    ///  The usage data.
    /// </param>
    /// <param name="scale">
    ///  Scale factor.
    /// </param>
    member _.SetValue ( argv : float[] ) ( scale : float ) : unit =
        let wv =
            argv
            |> Array.map ( fun itr -> min 1.0 ( itr / scale ) )
            |> Array.rev
        Array.fill m_Values 0 m_Values.Length 0.0
        if wv.Length > m_Values.Length then
            let s = wv.Length - m_Values.Length
            Array.blit wv s m_Values 0 m_Values.Length
        else
            let s = m_Values.Length - wv.Length
            Array.blit wv 0 m_Values s wv.Length


    /// <summary>
    ///  Calculate measurements value of usage data for a certain period of time going back from the current time based on the measured statistical information.
    /// </summary>
    /// <param name="v">
    ///  statistical information.
    /// </param>
    /// <returns>
    ///  Calculated usage data.
    /// </returns>
    member this.NormalizeValue ( v : TargetDeviceCtrlRes.T_RESCOUNTER list seq ) : float array =
        this.Normalize v ( fun itr -> float itr.Value )

    /// <summary>
    ///  Calculate count of usage data for a certain period of time going back from the current time based on the measured statistical information.
    /// </summary>
    /// <param name="v">
    ///  statistical information.
    /// </param>
    /// <returns>
    ///  Calculated usage data.
    /// </returns>
    member this.NormalizeCount ( v : TargetDeviceCtrlRes.T_RESCOUNTER list seq ) : float array =
        this.Normalize v ( fun itr -> float itr.Count )

    /// <summary>
    ///  Calculate value per count of usage data for a certain period of time going back from the current time based on the measured statistical information.
    /// </summary>
    /// <param name="v">
    ///  statistical information.
    /// </param>
    /// <returns>
    ///  Calculated usage data.
    /// </returns>
    member this.NormalizevaluePerCount ( v : TargetDeviceCtrlRes.T_RESCOUNTER list seq ) : float array =
        this.Normalize v ( fun itr -> ( float itr.Value ) / ( float itr.Count ) )

    ///////////////////////////////////////////////////////////////////////////
    // Private method

    /// <summary>
    ///  Calculate usage data for a certain period of time going back from the current time based on the measured statistical information.
    /// </summary>
    /// <param name="v">
    ///  statistical information.
    /// </param>
    /// <param name="f">
    ///  value getter function.
    /// </param>
    /// <returns>
    ///  Calculated usage data.
    /// </returns>
    member private _.Normalize ( v : TargetDeviceCtrlRes.T_RESCOUNTER list seq ) ( f : TargetDeviceCtrlRes.T_RESCOUNTER -> float ) : float array =
        let cd = DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond / Constants.RECOUNTER_SPAN_SEC
        let d =
            seq {
                for i = 1 to m_PointCnt do
                    yield ( cd - int64 i, 0.0 )
            }
            |> Seq.map KeyValuePair
            |> Dictionary
        v
        |> Seq.iter ( fun i1 ->
            i1
            |> List.iter ( fun i2 ->
                let k = i2.Time.Ticks / TimeSpan.TicksPerSecond / Constants.RECOUNTER_SPAN_SEC
                let r, wval = d.TryGetValue k
                if r then
                    d.Item( k ) <- wval + ( ( f i2 ) / ( float Constants.RECOUNTER_SPAN_SEC ) )
            )
        )
        d
        |> Seq.sortByDescending _.Key
        |> Seq.map _.Value
        |> Seq.map float
        |> Seq.toArray
