//=============================================================================
// Haruka Software Storage.
// SessionsCtrlUpdater.fs : Implement the function to update controlls in "Sessions" expander.
//

//=============================================================================
// Namespace declaration

namespace Haruka.ClientGUI

//=============================================================================
// Import declaration

open System
open System.Windows
open System.Windows.Controls
open System.Windows.Threading
open System.ComponentModel
open System.Collections.Generic

open Haruka.Constants
open Haruka.IODataTypes
open Haruka.Commons
open Haruka.Client

/// <summary>
///  SessionsCtrlUpdater class.
/// </summary>
type SessionsCtrlUpdater(
        m_SessionTree : TreeView,
        m_DestroySessionButton : Button,
        m_SessionParametersListView : ListView,
        m_ReceiveBytesHeightTextBlock : TextBlock,
        m_ReceiveBytesGraphWriter : GraphWriter,
        m_SendBytesHeightTextBlock : TextBlock,
        m_SendBytesGraphWriter : GraphWriter
    ) =

    member _.Clear() : unit =
        m_SessionTree.Items.Clear()
        m_DestroySessionButton.IsEnabled <- false
        m_SessionParametersListView.Items.Clear()
        m_ReceiveBytesHeightTextBlock.Text <- ""
        m_SendBytesHeightTextBlock.Text <- ""
        m_ReceiveBytesGraphWriter.SetValue Array.empty 1.0
        m_SendBytesGraphWriter.SetValue Array.empty 1.0

    member _.GetSelectedItemInfo() : SessionTreeItemType option =
        let selTVI = m_SessionTree.SelectedItem :?> TreeViewItem
        if selTVI = null then
            None
        else
            Some( selTVI.Tag :?> SessionTreeItemType )

    /// <summary>
    ///  Update session tree.
    /// </summary>
    /// <param name="sess">
    ///  Loaded session information.
    /// </param>
    /// <param name="conn">
    ///  Loaded connection information.
    /// </param>
    member this.InitialShow ( sess : TargetDeviceCtrlRes.T_Session list ) ( conn : TargetDeviceCtrlRes.T_Connection list ) : unit =
        this.Update sess conn
        if m_SessionTree.Items.Count > 0 then
            ( m_SessionTree.Items.Item( 0 ) :?> TreeViewItem ).IsSelected <- true

    /// <summary>
    ///  Update session tree.
    /// </summary>
    /// <param name="sess">
    ///  Loaded session information.
    /// </param>
    /// <param name="conn">
    ///  Loaded connection information.
    /// </param>
    member this.Update ( sess : TargetDeviceCtrlRes.T_Session list ) ( conn : TargetDeviceCtrlRes.T_Connection list ) : unit =
        let wConn =
            conn
            |> Seq.groupBy ( fun itr -> itr.TSIH )
            |> Seq.map ( fun ( a, b ) ->
                let v =
                    b
                    |> Seq.sortWith ( fun c d -> concnt_me.Compare c.ConnectionID c.ConnectionCount d.ConnectionID d.ConnectionCount )
                    |> Seq.toArray
                ( a, v )
            )
            |> Seq.map KeyValuePair
            |> Dictionary

        sess
        |> Seq.sortWith ( fun a b -> tsih_me.Compare a.TSIH b.TSIH )
        |> Seq.fold ( fun ( idx : int ) sinfo ->
            let wc =
                match wConn.TryGetValue sinfo.TSIH with
                | true, w -> w
                | false, _ -> Array.empty
            let c =
                if m_SessionTree.Items.Count <= idx then
                    1
                else
                    let stvi = m_SessionTree.Items.Item( idx ) :?> TreeViewItem
                    match stvi.Tag :?> SessionTreeItemType with
                    | SessionTreeItemType.Session( _, tviTSIH ) ->
                        tsih_me.Compare tviTSIH sinfo.TSIH
                    | _ -> 0    // Unexpected

            if c = 0 then
                // update session tree node
                let stvi = m_SessionTree.Items.Item( idx ) :?> TreeViewItem
                this.UpdateSessionTree_Conn stvi sinfo wc 
                ( idx + 1 )
            elif c < 0 then
                m_SessionTree.Items.RemoveAt idx
                idx
            else
                // insert new session tree node
                let stvi = new TreeViewItem( Header = sprintf "TSIH=%d" ( tsih_me.toPrim sinfo.TSIH ) )
                stvi.Tag <- SessionTreeItemType.Session( null, sinfo.TSIH )
                m_SessionTree.Items.Insert( idx, stvi )
                this.UpdateSessionTree_Conn stvi sinfo wc 
                ( idx + 1 )
        ) 0
        |> ( fun idx -> while m_SessionTree.Items.Count > idx do m_SessionTree.Items.RemoveAt idx )

    /// <summary>
    ///  Update one tree item in session tree.
    /// </summary>
    /// <param name="sess">
    ///  Loaded session information.
    /// </param>
    /// <param name="conn">
    ///  Loaded connection information.
    /// </param>
    member private this.UpdateSessionTree_Conn
            ( stvi : TreeViewItem )
            ( sess : TargetDeviceCtrlRes.T_Session )
            ( conn : TargetDeviceCtrlRes.T_Connection [] )
            : unit =

        // Normalize session usage data
        let sessReceiveUsage =
            conn
            |> Seq.map ( fun itr -> itr.ReceiveBytesCount )
            |> m_ReceiveBytesGraphWriter.NormalizeValue
        let sessSentUsage =
            conn
            |> Seq.map ( fun itr -> itr.SentBytesCount )
            |> m_SendBytesGraphWriter.NormalizeValue

        // calc session graph scale
        let sessSace, sessScaleLabel =
            seq{ sessReceiveUsage; sessSentUsage }
            |> Seq.concat
            |> GraphWriter.CalcScale_BytesPerSec

        // Update handler for session tree item node
        let sessHandler = new RoutedEventHandler(
            fun ( sender : obj ) _ ->
                if ( sender :?> TreeViewItem ).IsSelected then
                    m_ReceiveBytesHeightTextBlock.Text <- sessScaleLabel
                    m_ReceiveBytesGraphWriter.SetValue sessReceiveUsage sessSace
                    m_ReceiveBytesGraphWriter.UpdateGraph()
                    m_SendBytesHeightTextBlock.Text <- sessScaleLabel
                    m_SendBytesGraphWriter.SetValue sessSentUsage sessSace
                    m_SendBytesGraphWriter.UpdateGraph()
                    this.ShowSessionParameters sess
                    m_DestroySessionButton.IsEnabled <- true
        )

        match stvi.Tag :?> SessionTreeItemType with
        | SessionTreeItemType.Session( e, _ ) ->
            if e <> null then
                stvi.Selected.RemoveHandler e
        | _ -> ()
        stvi.Tag <- SessionTreeItemType.Session( sessHandler, sess.TSIH )
        stvi.Selected.AddHandler sessHandler

        if stvi.IsSelected then
            m_ReceiveBytesHeightTextBlock.Text <- sessScaleLabel
            m_ReceiveBytesGraphWriter.SetValue sessReceiveUsage sessSace
            m_ReceiveBytesGraphWriter.UpdateGraph()
            m_SendBytesHeightTextBlock.Text <- sessScaleLabel
            m_SendBytesGraphWriter.SetValue sessSentUsage sessSace
            m_SendBytesGraphWriter.UpdateGraph()


        conn
        |> Seq.fold ( fun ( idx : int ) ( itr : TargetDeviceCtrlRes.T_Connection ) ->
            

            // Normalize connection usage data
            let connReceiveUsage =
                m_ReceiveBytesGraphWriter.NormalizeValue [| itr.ReceiveBytesCount |]
            let connSentUsage =
                m_SendBytesGraphWriter.NormalizeValue [| itr.SentBytesCount |]

            // calc connection graph scale
            let connSace, connScaleLabel =
                seq{ connReceiveUsage; connSentUsage }
                |> Seq.concat
                |> GraphWriter.CalcScale_BytesPerSec 

            let conHandler = new RoutedEventHandler(
                fun ( sender : obj ) _ ->
                    if ( sender :?> TreeViewItem ).IsSelected then
                        m_ReceiveBytesHeightTextBlock.Text <- connScaleLabel
                        m_ReceiveBytesGraphWriter.SetValue connReceiveUsage connSace
                        m_ReceiveBytesGraphWriter.UpdateGraph()
                        m_SendBytesHeightTextBlock.Text <- connScaleLabel
                        m_SendBytesGraphWriter.SetValue connSentUsage connSace
                        m_SendBytesGraphWriter.UpdateGraph()
                        this.ShowConnectionParameters itr
                        m_DestroySessionButton.IsEnabled <- false
            )

            let c =
                if stvi.Items.Count <= idx then
                    1
                else
                    let ctvi = stvi.Items.Item( idx ) :?> TreeViewItem
                    match ctvi.Tag :?> SessionTreeItemType with
                    | SessionTreeItemType.Connection( _, tviCID, tviConCnt ) ->
                        concnt_me.Compare tviCID tviConCnt itr.ConnectionID itr.ConnectionCount
                    | _ -> 0    // Unexpected

            if c = 0 then
                // update connection tree node
                let ctvi = stvi.Items.Item( idx ) :?> TreeViewItem
                match ctvi.Tag :?> SessionTreeItemType with
                | SessionTreeItemType.Connection( e, _, _ ) ->
                    ctvi.Selected.RemoveHandler e
                | _ -> ()
                ctvi.Tag <- SessionTreeItemType.Connection( conHandler, itr.ConnectionID, itr.ConnectionCount )
                ctvi.Selected.AddHandler conHandler

                if ctvi.IsSelected then
                    m_ReceiveBytesHeightTextBlock.Text <- connScaleLabel
                    m_ReceiveBytesGraphWriter.SetValue connReceiveUsage connSace
                    m_ReceiveBytesGraphWriter.UpdateGraph()
                    m_SendBytesHeightTextBlock.Text <- connScaleLabel
                    m_SendBytesGraphWriter.SetValue connSentUsage connSace
                    m_SendBytesGraphWriter.UpdateGraph()

                idx + 1
            elif c < 0 then
                stvi.Items.RemoveAt idx
                idx
            else
                // insert new connection tree node
                let connTVStr = sprintf "CID=%d, CountNo=%d" itr.ConnectionID itr.ConnectionCount
                let ctvi = new TreeViewItem( Header = connTVStr )
                stvi.Items.Insert( idx, ctvi )
                ctvi.Tag <- SessionTreeItemType.Connection( conHandler, itr.ConnectionID, itr.ConnectionCount )
                ctvi.Selected.AddHandler conHandler
                idx + 1
        ) 0
        |> ( fun idx -> while stvi.Items.Count > idx do stvi.Items.RemoveAt idx )


    /// <summary>
    ///  Set the session paramter values to the parameter list controll.
    /// </summary>
    /// <param name="conn">
    ///  Selected session parameter values.
    /// </param>
    member private _.ShowSessionParameters ( sess : TargetDeviceCtrlRes.T_Session ) : unit =
        m_SessionParametersListView.Items.Clear()
        let itn = ITNexus( sess.ITNexus.InitiatorName, sess.ITNexus.ISID, sess.ITNexus.TargetName, sess.ITNexus.TPGT )
        m_SessionParametersListView.Items.Add { Name = "I_T Nexsus"; Value = itn.ToString() } |> ignore
        m_SessionParametersListView.Items.Add { Name = "TSIH"; Value = sprintf "%d" sess.TSIH } |> ignore
        m_SessionParametersListView.Items.Add { Name = "Target Group ID"; Value = tgid_me.toString sess.TargetGroupID } |> ignore
        m_SessionParametersListView.Items.Add { Name = "MaxConnections"; Value = sprintf "%d" sess.SessionParameters.MaxConnections } |> ignore
        m_SessionParametersListView.Items.Add { Name = "InitiatorAlias"; Value = sess.SessionParameters.InitiatorAlias } |> ignore
        m_SessionParametersListView.Items.Add { Name = "InitialR2T"; Value = sprintf "%b" sess.SessionParameters.InitialR2T } |> ignore
        m_SessionParametersListView.Items.Add { Name = "ImmediateData"; Value = sprintf "%b" sess.SessionParameters.ImmediateData } |> ignore
        m_SessionParametersListView.Items.Add { Name = "MaxBurstLength"; Value = sprintf "%d" sess.SessionParameters.MaxBurstLength } |> ignore
        m_SessionParametersListView.Items.Add { Name = "FirstBurstLength"; Value = sprintf "%d" sess.SessionParameters.FirstBurstLength } |> ignore
        m_SessionParametersListView.Items.Add { Name = "DefaultTime2Wait"; Value = sprintf "%d" sess.SessionParameters.DefaultTime2Wait } |> ignore
        m_SessionParametersListView.Items.Add { Name = "DefaultTime2Retain"; Value = sprintf "%d" sess.SessionParameters.DefaultTime2Retain } |> ignore
        m_SessionParametersListView.Items.Add { Name = "MaxOutstandingR2T"; Value = sprintf "%d" sess.SessionParameters.MaxOutstandingR2T } |> ignore
        m_SessionParametersListView.Items.Add { Name = "DataPDUInOrder"; Value = sprintf "%b" sess.SessionParameters.DataPDUInOrder } |> ignore
        m_SessionParametersListView.Items.Add { Name = "DataSequenceInOrder"; Value = sprintf "%b" sess.SessionParameters.DataSequenceInOrder } |> ignore
        m_SessionParametersListView.Items.Add { Name = "ErrorRecoveryLevel"; Value = sprintf "%d" sess.SessionParameters.ErrorRecoveryLevel } |> ignore
        m_SessionParametersListView.Items.Add { Name = "Establish Time"; Value = sess.EstablishTime.ToString "o" } |> ignore

    /// <summary>
    ///  Set the connection paramter values to the parameter list controll.
    /// </summary>
    /// <param name="conn">
    ///  Selected connection parameter values.
    /// </param>
    member private _.ShowConnectionParameters ( conn : TargetDeviceCtrlRes.T_Connection ) : unit =
        m_SessionParametersListView.Items.Clear()
        m_SessionParametersListView.Items.Add { Name = "Connection ID"; Value = sprintf "%d" conn.ConnectionID } |> ignore
        m_SessionParametersListView.Items.Add { Name = "Connection Count"; Value = sprintf "%d" conn.ConnectionCount } |> ignore
        m_SessionParametersListView.Items.Add { Name = "AuthMethod"; Value = conn.ConnectionParameters.AuthMethod } |> ignore
        m_SessionParametersListView.Items.Add { Name = "HeaderDigest"; Value = conn.ConnectionParameters.HeaderDigest } |> ignore
        m_SessionParametersListView.Items.Add { Name = "DataDigest"; Value = conn.ConnectionParameters.DataDigest } |> ignore
        m_SessionParametersListView.Items.Add { Name = "MaxRecvDataSegmentLength(Initiator)"; Value = sprintf "%d" conn.ConnectionParameters.MaxRecvDataSegmentLength_I } |> ignore
        m_SessionParametersListView.Items.Add { Name = "MaxRecvDataSegmentLength(Target)"; Value = sprintf "%d" conn.ConnectionParameters.MaxRecvDataSegmentLength_T } |> ignore
        m_SessionParametersListView.Items.Add { Name = "Establish Time"; Value = conn.EstablishTime.ToString "o" } |> ignore

