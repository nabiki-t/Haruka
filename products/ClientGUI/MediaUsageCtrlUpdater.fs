//=============================================================================
// Haruka Software Storage.
// MediaUsageCtrlUpdater.fs : Implement the function to update media usege grephs.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.Windows.Controls

open Haruka.Constants
open Haruka.IODataTypes
open Haruka.Commons
open Haruka.Client

//=============================================================================
// Class implementation

/// <summary>
///  MediaUsageCtrlUpdater class.
/// </summary>
/// <param name="m_ReadBytesHeightTextBlock">
///  Text box for highest value of the read bytes graph.
/// </param>
/// <param name="m_ReadBytesGraphWriter">
///  GraphWriter object of the read bytes graph.
/// </param>
/// <param name="m_WrittenBytesHeightTextBlock">
///  Text box for highest value of the written bytes graph.
/// </param>
/// <param name="m_WrittenBytesGraphWriter">
///  GraphWriter object of the written bytes graph.
/// </param>
/// <param name="m_ReadTickCountHeightTextBlock">
///  Text box for highest value of the read tick count graph.
/// </param>
/// <param name="m_ReadTickCountGraphWriter">
///  GraphWriter object of the read tick count graph.
/// </param>
/// <param name="m_WriteTickCountHeightTextBlock">
///  Text box for highest value of the write tick count graph.
/// </param>
/// <param name="m_WriteTickCountGraphWriter">
///  GraphWriter object of the write tick count graph.
/// </param>
/// <param name="m_ReadCountHeightTextBlock">
///  Text box for highest value of the read count graph.
/// </param>
/// <param name="m_ReadCountGraphWriter">
///  GraphWriter object of the read count graph.
/// </param>
/// <param name="m_WrittenCountHeightTextBlock">
///  Text box for highest value of the write count graph.
/// </param>
/// <param name="m_WrittenCountGraphWriter">
///  GraphWriter object of the write count graph.
/// </param>
type MediaUsageCtrlUpdater (
        m_ReadBytesHeightTextBlock : TextBlock,
        m_ReadBytesGraphWriter : GraphWriter,
        m_WrittenBytesHeightTextBlock : TextBlock,
        m_WrittenBytesGraphWriter : GraphWriter,
        m_ReadTickCountHeightTextBlock : TextBlock,
        m_ReadTickCountGraphWriter : GraphWriter,
        m_WriteTickCountHeightTextBlock : TextBlock,
        m_WriteTickCountGraphWriter : GraphWriter,
        m_ReadCountHeightTextBlock : TextBlock,
        m_ReadCountGraphWriter : GraphWriter,
        m_WrittenCountHeightTextBlock : TextBlock,
        m_WrittenCountGraphWriter : GraphWriter
    ) =

    /// <summary>
    ///  Clear all graphs.
    /// </summary>
    member _.Clear() : unit =
        m_ReadBytesHeightTextBlock.Text <- ""
        m_ReadBytesGraphWriter.SetValue Array.empty 1.0
        m_WrittenBytesHeightTextBlock.Text <- ""
        m_WrittenBytesGraphWriter.SetValue Array.empty 1.0
        m_ReadTickCountHeightTextBlock.Text <- ""
        m_ReadTickCountGraphWriter.SetValue Array.empty 1.0
        m_WriteTickCountHeightTextBlock.Text <- ""
        m_WriteTickCountGraphWriter.SetValue Array.empty 1.0
        m_ReadCountHeightTextBlock.Text <- ""
        m_ReadCountGraphWriter.SetValue Array.empty 1.0
        m_WrittenCountHeightTextBlock.Text <- ""
        m_WrittenCountGraphWriter.SetValue Array.empty 1.0

    /// <summary>
    ///  Update session tree.
    /// </summary>
    /// <param name="readBytesCount">
    ///  Statistics about bytes read.
    /// </param>
    /// <param name="writtenBytesCount">
    ///  Statistics about bytes written.
    /// </param>
    /// <param name="readTickCount">
    ///  Statistics about the tick time it takes to read.
    /// </param>
    /// <param name="writeTickCount">
    ///  Statistics about the tick time it takes to written.
    /// </param>
    member _.Update 
            ( readBytesCount : TargetDeviceCtrlRes.T_RESCOUNTER list ) 
            ( writtenBytesCount : TargetDeviceCtrlRes.T_RESCOUNTER list ) 
            ( readTickCount : TargetDeviceCtrlRes.T_RESCOUNTER list ) 
            ( writeTickCount : TargetDeviceCtrlRes.T_RESCOUNTER list ) : unit =

        let norReadBytesCount =
            m_ReadBytesGraphWriter.NormalizeValue [| readBytesCount |]
        let norWrittenBytesCount =
            m_WrittenBytesGraphWriter.NormalizeValue [| writtenBytesCount |]
        let norReadTickCount =
            m_ReadTickCountGraphWriter.NormalizevaluePerCount [| readTickCount |]
        let norWriteTickCount =
            m_WriteTickCountGraphWriter.NormalizevaluePerCount [| writeTickCount |]
        let norReadCount =
            m_ReadCountGraphWriter.NormalizeValue [| readBytesCount |]
        let norWrittenCount =
            m_WrittenCountGraphWriter.NormalizeValue [| writtenBytesCount |]

        let bytesCountScale, bytesCountScaleLabel =
            seq{ norReadBytesCount; norWrittenBytesCount }
            |> Seq.concat
            |> GraphWriter.CalcScale_BytesPerSec

        let tickCountScale, tickCountScaleLabel =
            seq{ norReadTickCount; norWriteTickCount }
            |> Seq.concat
            |> GraphWriter.CalcScale_MilisecPerAccess

        let countScale, countScaleLabel =
            seq{ norReadCount; norWrittenCount }
            |> Seq.concat
            |> GraphWriter.CalcScale_CountPerSec

        m_ReadBytesHeightTextBlock.Text <- bytesCountScaleLabel
        m_ReadBytesGraphWriter.SetValue norReadBytesCount bytesCountScale
        m_ReadBytesGraphWriter.UpdateGraph()

        m_WrittenBytesHeightTextBlock.Text <- bytesCountScaleLabel
        m_WrittenBytesGraphWriter.SetValue norWrittenBytesCount bytesCountScale
        m_WrittenBytesGraphWriter.UpdateGraph()

        m_ReadTickCountHeightTextBlock.Text <- tickCountScaleLabel
        m_ReadTickCountGraphWriter.SetValue norReadTickCount tickCountScale
        m_ReadTickCountGraphWriter.UpdateGraph()

        m_WriteTickCountHeightTextBlock.Text <- tickCountScaleLabel
        m_WriteTickCountGraphWriter.SetValue norWriteTickCount tickCountScale
        m_WriteTickCountGraphWriter.UpdateGraph()

        m_ReadCountHeightTextBlock.Text <- countScaleLabel
        m_ReadCountGraphWriter.SetValue norReadCount countScale
        m_ReadCountGraphWriter.UpdateGraph()

        m_WrittenCountHeightTextBlock.Text <- countScaleLabel
        m_WrittenCountGraphWriter.SetValue norWrittenCount countScale
        m_WrittenCountGraphWriter.UpdateGraph()

