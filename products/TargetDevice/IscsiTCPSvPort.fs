//=============================================================================
// Haruka Software Storage.
// IscsiTCPSvPort.fs : Defines IscsiTCPSvPort class.
// IscsiTCPSvPort class imprements TCP server port for iSCSI.
// This class wait for connect a connection from iSCSI initiator, and create 
// Connection object.

//=============================================================================
// Namespace declaration

namespace Haruka.TargetDevice

//=============================================================================
// Import declaration

open System
open System.Net
open System.Net.Sockets 

open Haruka
open Haruka.Constants
open Haruka.Commons
open Haruka.IODataTypes
open System.IO

//=============================================================================
// Class implementation

/// <summary>
///   Instance of TCP server port.
///   Wait for establishment of TCP connection and ask Status Master to create connection component.
/// </summary>
/// <param name="m_StatusMaster">
///   Interface of Status Master component.
/// </param>
/// <param name="m_NetworkPortal">
///   Configuration information of TCP connection.
/// </param>
/// <param name="m_Killer">
///   An object that notice terminate request to this object.
/// </param>
type IscsiTCPSvPort
    (
        m_StatusMaster : IStatus,
        m_NetworkPortal : TargetDeviceConf.T_NetworkPortal,
        m_Killer : IKiller
    ) as this = 

    /// Object identifier
    let m_ObjID = objidx_me.NewID()

    /// TCPPort
    let m_Listener : TcpListener[] =
        IscsiTCPSvPort.CreateListener m_NetworkPortal m_ObjID

    do
        m_Killer.Add this
        HLogger.Trace( LogID.I_OBJ_INSTANCE_CREATED, fun g ->
            let msg = "IdentNumber=" + ( string m_NetworkPortal.IdentNumber ) + "PortNumber=" + ( string m_NetworkPortal.PortNumber )
            g.Gen2( m_ObjID, "IscsiTCPSvPort", msg )
        )

    //=========================================================================
    // Interface method

    interface IPort with

        // --------------------------------------------------------------------
        // Implementation of IComponent.Terminate
        override _.Terminate() : unit =
            for itr in m_Listener do
                itr.Stop()

        // Imprementation of IPort.Start
        override _.Start () : bool =
            if m_Killer.IsNoticed then
                raise <| Exception("Terminated requested.")

            m_Listener
            |> Array.map ( fun itr ->
                try
                    itr.Start()

                    // successfully starting to wait connection.
                    HLogger.Trace( LogID.I_START_WAITING_CONNECTION, fun g ->
                        g.Gen2( m_ObjID, m_NetworkPortal.IdentNumber, m_NetworkPortal.PortNumber )
                    )

                    fun () -> task {
                        try
                            while true do
                                // Start to wait connection establishment
                                let! s = itr.AcceptSocketAsync()
                                let remoteEndpoint = ( s.RemoteEndPoint :?> IPEndPoint )
                                let remoteAddress = remoteEndpoint.Address
                                let remotePort = remoteEndpoint.Port

                                let filterResult =
                                    if m_NetworkPortal.WhiteList.IsEmpty then
                                        // If IP conditions are not specified, it consider accept all connections.
                                        true
                                    else
                                        IPCondition.Match ( s.RemoteEndPoint :?> IPEndPoint ).Address m_NetworkPortal.WhiteList
                                if not filterResult then
                                    HLogger.Trace( LogID.W_CONN_REJECTED_DUE_TO_WHITELIST, fun g ->
                                        let endPointStr = remoteAddress.ToString ()
                                        let sourcePortStr = sprintf "%d" remotePort
                                        g.Gen2( m_ObjID, endPointStr, sourcePortStr )
                                    )
                                    s.Close()
                                else
                                    HLogger.Trace( LogID.I_ACCEPT_CONNECTION, fun g ->
                                        let endPointStr = remoteAddress.ToString ()
                                        let sourcePortStr = sprintf "%d" remotePort
                                        g.Gen2( m_ObjID, endPointStr, sourcePortStr )
                                    )

                                    s.ReceiveBufferSize <- m_NetworkPortal.ReceiveBufferSize
                                    s.SendBufferSize <- m_NetworkPortal.SendBufferSize
                                    s.NoDelay <- m_NetworkPortal.DisableNagle
                                    //s.SetSocketOption( SocketOptionLevel.IP, SocketOptionName.UseLoopback, true )
                                    let netStream = new NetworkStream( s )


                                    let connectedTime = DateTime.UtcNow

                                    // Start login negociation in asynchronously( TPGT is always zero. )
                                    let ln = m_StatusMaster.CreateLoginNegociator netStream connectedTime tpgt_me.zero m_NetworkPortal.IdentNumber
                                    ln.Start false |> ignore    // Start method returns always true
                        with
                        | _ as x ->
                            // If unknown exception is occured, notice this event to Status Master.
                            // And, I hope to StatusMaster rebuilds a Network Portal.
                            HLogger.UnexpectedException( fun g -> g.GenExp( m_ObjID, x ) )
                            // Close TCP port. And exit wait loop
                            itr.Stop()
                    }
                    |> Functions.StartTask
                    true
                with
                | _ as x ->
                    // failed to starting to wait connection.
                    HLogger.UnexpectedException( fun g -> g.GenExp( m_ObjID, x ) )
                    false
            )
            |> Array.exists not
            |> not

        // Imprementation of IPort.NetworkPortalInfo
        override _.NetworkPortal : TargetDeviceConf.T_NetworkPortal =
            m_NetworkPortal

    //=========================================================================
    // Static method

    // ------------------------------------------------------------------------
    /// <summary>
    ///   Create TCP port listener
    /// </summary>
    /// <param name="argNetworkPortalInfo">
    ///   Configuration information of TCP server port.
    /// </param>
    /// <param name="objid">
    ///   Object ID of the object that calls this method.
    /// </param>
    /// <returns>
    ///   TcpListener object.
    /// </returns>
    static member private CreateListener ( argNetworkPortalInfo : TargetDeviceConf.T_NetworkPortal ) ( objid : OBJIDX_T ) : TcpListener[] =
        // resolv local host name to network assress.
        let confAddr = argNetworkPortalInfo.TargetAddress
        let portNum = argNetworkPortalInfo.PortNumber
        let addr =
            if confAddr.Length > 0 then
                try
                    match IPAddress.TryParse confAddr with
                    | ( true, x ) -> [| x |]
                    | ( false, _ ) ->
                        let r = Dns.GetHostEntry confAddr
                        r.AddressList
                with
                | :? ArgumentNullException
                | :? ArgumentOutOfRangeException
                | :? SocketException
                | :? ArgumentException as x ->
                    // crush this application domain.
                    HLogger.Trace( LogID.E_FAILED_RESOLV_ADDRESS, fun g -> g.Gen2( objid, confAddr, x.Message ) )
                    reraise()
            else
                [| IPAddress.IPv6Any |]

        // Create TCP server port.
        try
            addr
            |> Array.map( fun itr ->
                let l = new TcpListener( itr, int portNum )
                if confAddr.Length <= 0 then
                    l.Server.SetSocketOption( SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0 )
                HLogger.Trace( LogID.I_CREATE_TCP_SERVER_PORT, fun g -> g.Gen2( objid, confAddr, portNum ) )
                l
            )
        with
        | :? ArgumentNullException
        | :? ArgumentOutOfRangeException as x ->
            HLogger.Trace( LogID.E_FAILED_CREATE_TCP_SERVER_PORT, fun g -> g.Gen3( objid, confAddr, portNum, x.Message ) )
            reraise()
        
