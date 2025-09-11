//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

//=============================================================================
// Namespace declaration

namespace Haruka.IODataTypes.TargetDeviceCtrlReq

//=============================================================================
// Import declaration

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

//=============================================================================
// Type definition

type [<NoComparison>]T_TargetDeviceCtrlReq = {
    Request : T_Request;
}

and [<NoComparison>]T_Request = 
    | U_GetActiveTargetGroups of unit
    | U_GetLoadedTargetGroups of unit
    | U_InactivateTargetGroup of TGID_T
    | U_ActivateTargetGroup of TGID_T
    | U_UnloadTargetGroup of TGID_T
    | U_LoadTargetGroup of TGID_T
    | U_SetLogParameters of T_SetLogParameters
    | U_GetLogParameters of unit
    | U_GetDeviceName of unit
    | U_GetSession of T_GetSession
    | U_DestructSession of TSIH_T
    | U_GetConnection of T_GetConnection
    | U_GetLUStatus of LUN_T
    | U_LUReset of LUN_T
    | U_GetMediaStatus of T_GetMediaStatus
    | U_MediaControlRequest of T_MediaControlRequest

and [<NoComparison>]T_SetLogParameters = {
    SoftLimit : uint32;
    HardLimit : uint32;
    LogLevel : LogLevel;
}

and [<NoComparison>]T_GetSession = 
    | U_SessInTargetDevice of unit
    | U_SessInTargetGroup of TGID_T
    | U_SessInTarget of TNODEIDX_T

and [<NoComparison>]T_GetConnection = 
    | U_ConInTargetDevice of unit
    | U_ConInNetworkPortal of NETPORTIDX_T
    | U_ConInTargetGroup of TGID_T
    | U_ConInTarget of TNODEIDX_T
    | U_ConInSession of TSIH_T

and [<NoComparison>]T_GetMediaStatus = {
    LUN : LUN_T;
    ID : MEDIAIDX_T;
}

and [<NoComparison>]T_MediaControlRequest = {
    LUN : LUN_T;
    ID : MEDIAIDX_T;
    Request : string;
}

//=============================================================================
// Class implementation

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='TargetDeviceCtrlReq' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='Request' >
        <xsd:complexType><xsd:choice>
          <xsd:element name='GetActiveTargetGroups' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:int'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='0' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='GetLoadedTargetGroups' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:int'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='0' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='InactivateTargetGroup' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='ActivateTargetGroup' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='UnloadTargetGroup' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='LoadTargetGroup' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='SetLogParameters' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SoftLimit' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedInt'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='10000000' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='HardLimit' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedInt'>
                    <xsd:minInclusive value='100' />
                    <xsd:maxInclusive value='20000000' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='LogLevel' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:enumeration value='VERBOSE' />
                    <xsd:enumeration value='INFO' />
                    <xsd:enumeration value='WARNING' />
                    <xsd:enumeration value='ERROR' />
                    <xsd:enumeration value='FAILED' />
                    <xsd:enumeration value='OFF' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='GetLogParameters' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:int'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='0' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='GetDeviceName' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:int'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='0' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='GetSession' >
            <xsd:complexType><xsd:choice>
              <xsd:element name='SessInTargetDevice' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:int'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='0' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='SessInTargetGroup' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='SessInTarget' >
                <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
              </xsd:element>
            </xsd:choice></xsd:complexType>
          </xsd:element>
          <xsd:element name='DestructSession' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedShort' />
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='GetConnection' >
            <xsd:complexType><xsd:choice>
              <xsd:element name='ConInTargetDevice' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:int'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='0' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ConInNetworkPortal' >
                <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
              </xsd:element>
              <xsd:element name='ConInTargetGroup' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ConInTarget' >
                <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
              </xsd:element>
              <xsd:element name='ConInSession' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedShort' />
                </xsd:simpleType>
              </xsd:element>
            </xsd:choice></xsd:complexType>
          </xsd:element>
          <xsd:element name='GetLUStatus' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedLong'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='255' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='LUReset' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedLong'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='255' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='GetMediaStatus' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='LUN' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedLong'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='255' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ID' >
                <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='MediaControlRequest' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='LUN' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedLong'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='255' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ID' >
                <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
              </xsd:element>
              <xsd:element name='Request' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
        </xsd:choice></xsd:complexType>
      </xsd:element>
    </xsd:sequence></xsd:complexType>
  </xsd:element>
</xsd:schema>"

    /// <summary>
    ///  Get XmlSchemaSet for validate input XML document.
    /// </summary>
    static let schemaSet =
        lazy
            use xsdStream = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes xsd, false )
            use xsdReader = XmlReader.Create xsdStream
            let wSS = new XmlSchemaSet ()
            wSS.Add( null, xsdReader ) |> ignore
            xsdStream.Dispose()
            xsdReader.Dispose()
            wSS

    /// <summary>
    ///  Check iSCSI Name string length.
    /// </summary>
    static member private Check223Length ( str : string ) : string =
        let encStr = Encoding.GetEncoding( "utf-8" ).GetBytes( str )
        if encStr.Length > Constants.ISCSI_TEXT_MAX_ISCSI_NAME_LENGTH then
            raise( ConfRWException( "iSCSI name too long." ) )
        else
            str

    /// <summary>
    ///  Encode string value for output XML data.
    /// </summary>
    static member private xmlEncode : string -> string =
        String.collect (
            function
            | '<' -> "&lt;"
            | '>' -> "&gt;"
            | '&' -> "&amp;"
            | '\"' -> "&quot;"
            | '\'' -> "&apos;"
            | '\r' -> "&#013;"
            | '\n' -> "&#010;"
            | _ as c -> c.ToString()
        )

    /// <summary>
    ///  Load TargetDeviceCtrlReq data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded TargetDeviceCtrlReq data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_TargetDeviceCtrlReq =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load TargetDeviceCtrlReq data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded TargetDeviceCtrlReq data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_TargetDeviceCtrlReq =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "TargetDeviceCtrlReq" |> xdoc.Element |> ReaderWriter.Read_T_TargetDeviceCtrlReq

    /// <summary>
    ///  Read T_TargetDeviceCtrlReq data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TargetDeviceCtrlReq data structure.
    /// </returns>
    static member private Read_T_TargetDeviceCtrlReq ( elem : XElement ) : T_TargetDeviceCtrlReq = 
        {
            Request =
                ReaderWriter.Read_T_Request( elem.Element( XName.Get "Request" ) );
        }

    /// <summary>
    ///  Read T_Request data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Request data structure.
    /// </returns>
    static member private Read_T_Request ( elem : XElement ) : T_Request = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "GetActiveTargetGroups" ->
            U_GetActiveTargetGroups( () )
        | "GetLoadedTargetGroups" ->
            U_GetLoadedTargetGroups( () )
        | "InactivateTargetGroup" ->
            U_InactivateTargetGroup( tgid_me.fromString( firstChild.Value ) )
        | "ActivateTargetGroup" ->
            U_ActivateTargetGroup( tgid_me.fromString( firstChild.Value ) )
        | "UnloadTargetGroup" ->
            U_UnloadTargetGroup( tgid_me.fromString( firstChild.Value ) )
        | "LoadTargetGroup" ->
            U_LoadTargetGroup( tgid_me.fromString( firstChild.Value ) )
        | "SetLogParameters" ->
            U_SetLogParameters( ReaderWriter.Read_T_SetLogParameters firstChild )
        | "GetLogParameters" ->
            U_GetLogParameters( () )
        | "GetDeviceName" ->
            U_GetDeviceName( () )
        | "GetSession" ->
            U_GetSession( ReaderWriter.Read_T_GetSession firstChild )
        | "DestructSession" ->
            U_DestructSession( tsih_me.fromPrim( UInt16.Parse( firstChild.Value ) ) )
        | "GetConnection" ->
            U_GetConnection( ReaderWriter.Read_T_GetConnection firstChild )
        | "GetLUStatus" ->
            U_GetLUStatus( lun_me.fromStringValue( firstChild.Value ) )
        | "LUReset" ->
            U_LUReset( lun_me.fromStringValue( firstChild.Value ) )
        | "GetMediaStatus" ->
            U_GetMediaStatus( ReaderWriter.Read_T_GetMediaStatus firstChild )
        | "MediaControlRequest" ->
            U_MediaControlRequest( ReaderWriter.Read_T_MediaControlRequest firstChild )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_SetLogParameters data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_SetLogParameters data structure.
    /// </returns>
    static member private Read_T_SetLogParameters ( elem : XElement ) : T_SetLogParameters = 
        {
            SoftLimit =
                UInt32.Parse( elem.Element( XName.Get "SoftLimit" ).Value );
            HardLimit =
                UInt32.Parse( elem.Element( XName.Get "HardLimit" ).Value );
            LogLevel =
                LogLevel.fromString( elem.Element( XName.Get "LogLevel" ).Value );
        }

    /// <summary>
    ///  Read T_GetSession data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_GetSession data structure.
    /// </returns>
    static member private Read_T_GetSession ( elem : XElement ) : T_GetSession = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "SessInTargetDevice" ->
            U_SessInTargetDevice( () )
        | "SessInTargetGroup" ->
            U_SessInTargetGroup( tgid_me.fromString( firstChild.Value ) )
        | "SessInTarget" ->
            U_SessInTarget( tnodeidx_me.fromPrim( UInt32.Parse( firstChild.Value ) ) )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_GetConnection data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_GetConnection data structure.
    /// </returns>
    static member private Read_T_GetConnection ( elem : XElement ) : T_GetConnection = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "ConInTargetDevice" ->
            U_ConInTargetDevice( () )
        | "ConInNetworkPortal" ->
            U_ConInNetworkPortal( netportidx_me.fromPrim( UInt32.Parse( firstChild.Value ) ) )
        | "ConInTargetGroup" ->
            U_ConInTargetGroup( tgid_me.fromString( firstChild.Value ) )
        | "ConInTarget" ->
            U_ConInTarget( tnodeidx_me.fromPrim( UInt32.Parse( firstChild.Value ) ) )
        | "ConInSession" ->
            U_ConInSession( tsih_me.fromPrim( UInt16.Parse( firstChild.Value ) ) )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_GetMediaStatus data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_GetMediaStatus data structure.
    /// </returns>
    static member private Read_T_GetMediaStatus ( elem : XElement ) : T_GetMediaStatus = 
        {
            LUN =
                lun_me.fromStringValue( elem.Element( XName.Get "LUN" ).Value );
            ID =
                mediaidx_me.fromPrim( UInt32.Parse( elem.Element( XName.Get "ID" ).Value ) );
        }

    /// <summary>
    ///  Read T_MediaControlRequest data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_MediaControlRequest data structure.
    /// </returns>
    static member private Read_T_MediaControlRequest ( elem : XElement ) : T_MediaControlRequest = 
        {
            LUN =
                lun_me.fromStringValue( elem.Element( XName.Get "LUN" ).Value );
            ID =
                mediaidx_me.fromPrim( UInt32.Parse( elem.Element( XName.Get "ID" ).Value ) );
            Request =
                elem.Element( XName.Get "Request" ).Value;
        }

    /// <summary>
    ///  Write TargetDeviceCtrlReq data to specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <remarks>
    ///  If it failed to write configuration, an exception will be raised.
    /// </remarks>
    static member WriteFile ( fname : string ) ( d : T_TargetDeviceCtrlReq ) : unit =
        let s = ReaderWriter.T_TargetDeviceCtrlReq_toString 0 2 d "TargetDeviceCtrlReq"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert TargetDeviceCtrlReq data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_TargetDeviceCtrlReq ) : string =
        ReaderWriter.T_TargetDeviceCtrlReq_toString 0 0 d "TargetDeviceCtrlReq"
        |> String.Concat

    /// <summary>
    ///  Write T_TargetDeviceCtrlReq data structure to configuration file.
    /// </summary>
    /// <param name="indent">
    ///  Indent space count.
    /// </param>
    /// <param name="indentStep">
    ///  Indent step count.
    /// </param>
    /// <param name="elem">
    ///  Data structure for output.
    /// </param>
    /// <param name="elemName">
    ///  XML tag name for the data.
    /// </param>
    /// <returns>
    ///  Array of the generated string.
    /// </returns>
    static member private T_TargetDeviceCtrlReq_toString ( indent : int ) ( indentStep : int ) ( elem : T_TargetDeviceCtrlReq ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield! ReaderWriter.T_Request_toString ( indent + 1 ) indentStep ( elem.Request ) "Request"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Request data structure to configuration file.
    /// </summary>
    /// <param name="indent">
    ///  Indent space count.
    /// </param>
    /// <param name="indentStep">
    ///  Indent step count.
    /// </param>
    /// <param name="elem">
    ///  Data structure for output.
    /// </param>
    /// <param name="elemName">
    ///  XML tag name for the data.
    /// </param>
    /// <returns>
    ///  Array of the generated string.
    /// </returns>
    static member private T_Request_toString ( indent : int ) ( indentStep : int ) ( elem : T_Request ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_GetActiveTargetGroups( x ) ->
                yield sprintf "%s%s<GetActiveTargetGroups>0</GetActiveTargetGroups>" singleIndent indentStr
            | U_GetLoadedTargetGroups( x ) ->
                yield sprintf "%s%s<GetLoadedTargetGroups>0</GetLoadedTargetGroups>" singleIndent indentStr
            | U_InactivateTargetGroup( x ) ->
                yield sprintf "%s%s<InactivateTargetGroup>%s</InactivateTargetGroup>" singleIndent indentStr ( tgid_me.toString (x) )
            | U_ActivateTargetGroup( x ) ->
                yield sprintf "%s%s<ActivateTargetGroup>%s</ActivateTargetGroup>" singleIndent indentStr ( tgid_me.toString (x) )
            | U_UnloadTargetGroup( x ) ->
                yield sprintf "%s%s<UnloadTargetGroup>%s</UnloadTargetGroup>" singleIndent indentStr ( tgid_me.toString (x) )
            | U_LoadTargetGroup( x ) ->
                yield sprintf "%s%s<LoadTargetGroup>%s</LoadTargetGroup>" singleIndent indentStr ( tgid_me.toString (x) )
            | U_SetLogParameters( x ) ->
                yield! ReaderWriter.T_SetLogParameters_toString ( indent + 1 ) indentStep ( x ) "SetLogParameters"
            | U_GetLogParameters( x ) ->
                yield sprintf "%s%s<GetLogParameters>0</GetLogParameters>" singleIndent indentStr
            | U_GetDeviceName( x ) ->
                yield sprintf "%s%s<GetDeviceName>0</GetDeviceName>" singleIndent indentStr
            | U_GetSession( x ) ->
                yield! ReaderWriter.T_GetSession_toString ( indent + 1 ) indentStep ( x ) "GetSession"
            | U_DestructSession( x ) ->
                yield sprintf "%s%s<DestructSession>%d</DestructSession>" singleIndent indentStr ( tsih_me.toPrim (x) )
            | U_GetConnection( x ) ->
                yield! ReaderWriter.T_GetConnection_toString ( indent + 1 ) indentStep ( x ) "GetConnection"
            | U_GetLUStatus( x ) ->
                if lun_me.toPrim (x) < 0UL then
                    raise <| ConfRWException( "Min value(LUN_T) restriction error. GetLUStatus" )
                if lun_me.toPrim (x) > 255UL then
                    raise <| ConfRWException( "Max value(LUN_T) restriction error. GetLUStatus" )
                yield sprintf "%s%s<GetLUStatus>%s</GetLUStatus>" singleIndent indentStr ( lun_me.toString (x) )
            | U_LUReset( x ) ->
                if lun_me.toPrim (x) < 0UL then
                    raise <| ConfRWException( "Min value(LUN_T) restriction error. LUReset" )
                if lun_me.toPrim (x) > 255UL then
                    raise <| ConfRWException( "Max value(LUN_T) restriction error. LUReset" )
                yield sprintf "%s%s<LUReset>%s</LUReset>" singleIndent indentStr ( lun_me.toString (x) )
            | U_GetMediaStatus( x ) ->
                yield! ReaderWriter.T_GetMediaStatus_toString ( indent + 1 ) indentStep ( x ) "GetMediaStatus"
            | U_MediaControlRequest( x ) ->
                yield! ReaderWriter.T_MediaControlRequest_toString ( indent + 1 ) indentStep ( x ) "MediaControlRequest"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_SetLogParameters data structure to configuration file.
    /// </summary>
    /// <param name="indent">
    ///  Indent space count.
    /// </param>
    /// <param name="indentStep">
    ///  Indent step count.
    /// </param>
    /// <param name="elem">
    ///  Data structure for output.
    /// </param>
    /// <param name="elemName">
    ///  XML tag name for the data.
    /// </param>
    /// <returns>
    ///  Array of the generated string.
    /// </returns>
    static member private T_SetLogParameters_toString ( indent : int ) ( indentStep : int ) ( elem : T_SetLogParameters ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if (elem.SoftLimit) < 0u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. SoftLimit" )
            if (elem.SoftLimit) > 10000000u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. SoftLimit" )
            yield sprintf "%s%s<SoftLimit>%d</SoftLimit>" singleIndent indentStr (elem.SoftLimit)
            if (elem.HardLimit) < 100u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. HardLimit" )
            if (elem.HardLimit) > 20000000u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. HardLimit" )
            yield sprintf "%s%s<HardLimit>%d</HardLimit>" singleIndent indentStr (elem.HardLimit)
            yield sprintf "%s%s<LogLevel>%s</LogLevel>" singleIndent indentStr ( LogLevel.toString (elem.LogLevel) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_GetSession data structure to configuration file.
    /// </summary>
    /// <param name="indent">
    ///  Indent space count.
    /// </param>
    /// <param name="indentStep">
    ///  Indent step count.
    /// </param>
    /// <param name="elem">
    ///  Data structure for output.
    /// </param>
    /// <param name="elemName">
    ///  XML tag name for the data.
    /// </param>
    /// <returns>
    ///  Array of the generated string.
    /// </returns>
    static member private T_GetSession_toString ( indent : int ) ( indentStep : int ) ( elem : T_GetSession ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_SessInTargetDevice( x ) ->
                yield sprintf "%s%s<SessInTargetDevice>0</SessInTargetDevice>" singleIndent indentStr
            | U_SessInTargetGroup( x ) ->
                yield sprintf "%s%s<SessInTargetGroup>%s</SessInTargetGroup>" singleIndent indentStr ( tgid_me.toString (x) )
            | U_SessInTarget( x ) ->
                yield sprintf "%s%s<SessInTarget>%d</SessInTarget>" singleIndent indentStr ( tnodeidx_me.toPrim (x) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_GetConnection data structure to configuration file.
    /// </summary>
    /// <param name="indent">
    ///  Indent space count.
    /// </param>
    /// <param name="indentStep">
    ///  Indent step count.
    /// </param>
    /// <param name="elem">
    ///  Data structure for output.
    /// </param>
    /// <param name="elemName">
    ///  XML tag name for the data.
    /// </param>
    /// <returns>
    ///  Array of the generated string.
    /// </returns>
    static member private T_GetConnection_toString ( indent : int ) ( indentStep : int ) ( elem : T_GetConnection ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_ConInTargetDevice( x ) ->
                yield sprintf "%s%s<ConInTargetDevice>0</ConInTargetDevice>" singleIndent indentStr
            | U_ConInNetworkPortal( x ) ->
                yield sprintf "%s%s<ConInNetworkPortal>%d</ConInNetworkPortal>" singleIndent indentStr ( netportidx_me.toPrim (x) )
            | U_ConInTargetGroup( x ) ->
                yield sprintf "%s%s<ConInTargetGroup>%s</ConInTargetGroup>" singleIndent indentStr ( tgid_me.toString (x) )
            | U_ConInTarget( x ) ->
                yield sprintf "%s%s<ConInTarget>%d</ConInTarget>" singleIndent indentStr ( tnodeidx_me.toPrim (x) )
            | U_ConInSession( x ) ->
                yield sprintf "%s%s<ConInSession>%d</ConInSession>" singleIndent indentStr ( tsih_me.toPrim (x) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_GetMediaStatus data structure to configuration file.
    /// </summary>
    /// <param name="indent">
    ///  Indent space count.
    /// </param>
    /// <param name="indentStep">
    ///  Indent step count.
    /// </param>
    /// <param name="elem">
    ///  Data structure for output.
    /// </param>
    /// <param name="elemName">
    ///  XML tag name for the data.
    /// </param>
    /// <returns>
    ///  Array of the generated string.
    /// </returns>
    static member private T_GetMediaStatus_toString ( indent : int ) ( indentStep : int ) ( elem : T_GetMediaStatus ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if lun_me.toPrim (elem.LUN) < 0UL then
                raise <| ConfRWException( "Min value(LUN_T) restriction error. LUN" )
            if lun_me.toPrim (elem.LUN) > 255UL then
                raise <| ConfRWException( "Max value(LUN_T) restriction error. LUN" )
            yield sprintf "%s%s<LUN>%s</LUN>" singleIndent indentStr ( lun_me.toString (elem.LUN) )
            yield sprintf "%s%s<ID>%d</ID>" singleIndent indentStr ( mediaidx_me.toPrim (elem.ID) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_MediaControlRequest data structure to configuration file.
    /// </summary>
    /// <param name="indent">
    ///  Indent space count.
    /// </param>
    /// <param name="indentStep">
    ///  Indent step count.
    /// </param>
    /// <param name="elem">
    ///  Data structure for output.
    /// </param>
    /// <param name="elemName">
    ///  XML tag name for the data.
    /// </param>
    /// <returns>
    ///  Array of the generated string.
    /// </returns>
    static member private T_MediaControlRequest_toString ( indent : int ) ( indentStep : int ) ( elem : T_MediaControlRequest ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if lun_me.toPrim (elem.LUN) < 0UL then
                raise <| ConfRWException( "Min value(LUN_T) restriction error. LUN" )
            if lun_me.toPrim (elem.LUN) > 255UL then
                raise <| ConfRWException( "Max value(LUN_T) restriction error. LUN" )
            yield sprintf "%s%s<LUN>%s</LUN>" singleIndent indentStr ( lun_me.toString (elem.LUN) )
            yield sprintf "%s%s<ID>%d</ID>" singleIndent indentStr ( mediaidx_me.toPrim (elem.ID) )
            yield sprintf "%s%s<Request>%s</Request>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Request) )
            yield sprintf "%s</%s>" indentStr elemName
        }


