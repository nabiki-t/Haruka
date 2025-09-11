//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

//=============================================================================
// Namespace declaration

namespace Haruka.IODataTypes.TargetDeviceConf

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

type [<NoComparison>]T_TargetDevice = {
    NetworkPortal : T_NetworkPortal list;
    NegotiableParameters : T_NegotiableParameters option;
    LogParameters : T_LogParameters option;
    DeviceName : string;
}

and [<NoComparison>]T_NetworkPortal = {
    IdentNumber : NETPORTIDX_T;
    TargetPortalGroupTag : TPGT_T;
    TargetAddress : string;
    PortNumber : uint16;
    DisableNagle : bool;
    ReceiveBufferSize : int;
    SendBufferSize : int;
    WhiteList : IPCondition list;
}

and [<NoComparison>]T_NegotiableParameters = {
    MaxRecvDataSegmentLength : uint32;
    MaxBurstLength : uint32;
    FirstBurstLength : uint32;
    DefaultTime2Wait : uint16;
    DefaultTime2Retain : uint16;
    MaxOutstandingR2T : uint16;
}

and [<NoComparison>]T_LogParameters = {
    SoftLimit : uint32;
    HardLimit : uint32;
    LogLevel : LogLevel;
}

//=============================================================================
// Class implementation

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='TargetDevice' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='NetworkPortal' minOccurs='0' maxOccurs='16' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='IdentNumber' >
            <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
          </xsd:element>
          <xsd:element name='TargetPortalGroupTag' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedShort'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='0' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='TargetAddress' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='0' />
                <xsd:maxLength value='32768' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='PortNumber' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedShort'>
                <xsd:minInclusive value='1' />
                <xsd:maxInclusive value='65535' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='DisableNagle' >
            <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
          </xsd:element>
          <xsd:element name='ReceiveBufferSize' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:int'>
                <xsd:minInclusive value='0' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='SendBufferSize' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:int'>
                <xsd:minInclusive value='0' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='WhiteList' minOccurs='0' maxOccurs='16' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:pattern value='^Any|Loopback|Linklocal|Private|Multicast|Global|IPv4Any|IPv4Loopback|IPv4Linklocal|IPv4Private|IPv4Multicast|IPv4Global|IPv6Any|IPv6Loopback|IPv6Linklocal|IPv6Private|IPv6Multicast|IPv6Global|IPFilter\( *[^ ,\)]{1,} *, *[^ ,\)]{1,} *\)$' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
        </xsd:sequence></xsd:complexType>
      </xsd:element>
      <xsd:element name='NegotiableParameters' minOccurs='0' maxOccurs='1' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='MaxRecvDataSegmentLength' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedInt'>
                <xsd:minInclusive value='512' />
                <xsd:maxInclusive value='16777215' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='MaxBurstLength' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedInt'>
                <xsd:minInclusive value='512' />
                <xsd:maxInclusive value='16777215' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='FirstBurstLength' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedInt'>
                <xsd:minInclusive value='512' />
                <xsd:maxInclusive value='16777215' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='DefaultTime2Wait' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedShort'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='3600' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='DefaultTime2Retain' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedShort'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='3600' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='MaxOutstandingR2T' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedShort'>
                <xsd:minInclusive value='1' />
                <xsd:maxInclusive value='65535' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
        </xsd:sequence></xsd:complexType>
      </xsd:element>
      <xsd:element name='LogParameters' minOccurs='0' maxOccurs='1' >
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
      <xsd:element name='DeviceName' minOccurs='0' maxOccurs='1' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
            <xsd:minLength value='0' />
            <xsd:maxLength value='512' />
          </xsd:restriction>
        </xsd:simpleType>
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
    ///  Load TargetDevice data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded TargetDevice data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_TargetDevice =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load TargetDevice data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded TargetDevice data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_TargetDevice =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "TargetDevice" |> xdoc.Element |> ReaderWriter.Read_T_TargetDevice

    /// <summary>
    ///  Read T_TargetDevice data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TargetDevice data structure.
    /// </returns>
    static member private Read_T_TargetDevice ( elem : XElement ) : T_TargetDevice = 
        {
            NetworkPortal =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "NetworkPortal" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_NetworkPortal itr )
                |> Seq.toList
            NegotiableParameters = 
                let subElem = elem.Element( XName.Get "NegotiableParameters" )
                if subElem = null then
                    None
                else
                    Some( ReaderWriter.Read_T_NegotiableParameters subElem );
            LogParameters = 
                let subElem = elem.Element( XName.Get "LogParameters" )
                if subElem = null then
                    None
                else
                    Some( ReaderWriter.Read_T_LogParameters subElem );
            DeviceName = 
                let subElem = elem.Element( XName.Get "DeviceName" )
                if subElem = null then
                    "";
                else
                    subElem.Value;
        }

    /// <summary>
    ///  Read T_NetworkPortal data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_NetworkPortal data structure.
    /// </returns>
    static member private Read_T_NetworkPortal ( elem : XElement ) : T_NetworkPortal = 
        {
            IdentNumber =
                netportidx_me.fromPrim( UInt32.Parse( elem.Element( XName.Get "IdentNumber" ).Value ) );
            TargetPortalGroupTag =
                tpgt_me.fromPrim( UInt16.Parse( elem.Element( XName.Get "TargetPortalGroupTag" ).Value ) );
            TargetAddress =
                elem.Element( XName.Get "TargetAddress" ).Value;
            PortNumber =
                UInt16.Parse( elem.Element( XName.Get "PortNumber" ).Value );
            DisableNagle =
                Boolean.Parse( elem.Element( XName.Get "DisableNagle" ).Value );
            ReceiveBufferSize =
                Int32.Parse( elem.Element( XName.Get "ReceiveBufferSize" ).Value );
            SendBufferSize =
                Int32.Parse( elem.Element( XName.Get "SendBufferSize" ).Value );
            WhiteList =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "WhiteList" )
                |> Seq.map ( fun itr -> IPCondition.Parse( itr.Value ) )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_NegotiableParameters data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_NegotiableParameters data structure.
    /// </returns>
    static member private Read_T_NegotiableParameters ( elem : XElement ) : T_NegotiableParameters = 
        {
            MaxRecvDataSegmentLength =
                UInt32.Parse( elem.Element( XName.Get "MaxRecvDataSegmentLength" ).Value );
            MaxBurstLength =
                UInt32.Parse( elem.Element( XName.Get "MaxBurstLength" ).Value );
            FirstBurstLength =
                UInt32.Parse( elem.Element( XName.Get "FirstBurstLength" ).Value );
            DefaultTime2Wait =
                UInt16.Parse( elem.Element( XName.Get "DefaultTime2Wait" ).Value );
            DefaultTime2Retain =
                UInt16.Parse( elem.Element( XName.Get "DefaultTime2Retain" ).Value );
            MaxOutstandingR2T =
                UInt16.Parse( elem.Element( XName.Get "MaxOutstandingR2T" ).Value );
        }

    /// <summary>
    ///  Read T_LogParameters data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LogParameters data structure.
    /// </returns>
    static member private Read_T_LogParameters ( elem : XElement ) : T_LogParameters = 
        {
            SoftLimit =
                UInt32.Parse( elem.Element( XName.Get "SoftLimit" ).Value );
            HardLimit =
                UInt32.Parse( elem.Element( XName.Get "HardLimit" ).Value );
            LogLevel =
                LogLevel.fromString( elem.Element( XName.Get "LogLevel" ).Value );
        }

    /// <summary>
    ///  Write TargetDevice data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_TargetDevice ) : unit =
        let s = ReaderWriter.T_TargetDevice_toString 0 2 d "TargetDevice"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert TargetDevice data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_TargetDevice ) : string =
        ReaderWriter.T_TargetDevice_toString 0 0 d "TargetDevice"
        |> String.Concat

    /// <summary>
    ///  Write T_TargetDevice data structure to configuration file.
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
    static member private T_TargetDevice_toString ( indent : int ) ( indentStep : int ) ( elem : T_TargetDevice ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.NetworkPortal.Length < 0 || elem.NetworkPortal.Length > 16 then 
                raise <| ConfRWException( "Element count restriction error. NetworkPortal" )
            for itr in elem.NetworkPortal do
                yield! ReaderWriter.T_NetworkPortal_toString ( indent + 1 ) indentStep itr "NetworkPortal"
            if elem.NegotiableParameters.IsSome then
                yield! ReaderWriter.T_NegotiableParameters_toString ( indent + 1 ) indentStep ( elem.NegotiableParameters.Value ) "NegotiableParameters"
            if elem.LogParameters.IsSome then
                yield! ReaderWriter.T_LogParameters_toString ( indent + 1 ) indentStep ( elem.LogParameters.Value ) "LogParameters"
            if (elem.DeviceName).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. DeviceName" )
            if (elem.DeviceName).Length > 512 then
                raise <| ConfRWException( "Max value(string) restriction error. DeviceName" )
            yield sprintf "%s%s<DeviceName>%s</DeviceName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.DeviceName) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_NetworkPortal data structure to configuration file.
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
    static member private T_NetworkPortal_toString ( indent : int ) ( indentStep : int ) ( elem : T_NetworkPortal ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<IdentNumber>%d</IdentNumber>" singleIndent indentStr ( netportidx_me.toPrim (elem.IdentNumber) )
            if (elem.TargetPortalGroupTag) < ( tpgt_me.fromPrim 0us ) then
                raise <| ConfRWException( "Min value(TPGT_T) restriction error. TargetPortalGroupTag" )
            if (elem.TargetPortalGroupTag) > ( tpgt_me.fromPrim 0us ) then
                raise <| ConfRWException( "Max value(TPGT_T) restriction error. TargetPortalGroupTag" )
            yield sprintf "%s%s<TargetPortalGroupTag>%d</TargetPortalGroupTag>" singleIndent indentStr ( tpgt_me.toPrim (elem.TargetPortalGroupTag) )
            if (elem.TargetAddress).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. TargetAddress" )
            if (elem.TargetAddress).Length > 32768 then
                raise <| ConfRWException( "Max value(string) restriction error. TargetAddress" )
            yield sprintf "%s%s<TargetAddress>%s</TargetAddress>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.TargetAddress) )
            if (elem.PortNumber) < 1us then
                raise <| ConfRWException( "Min value(unsignedShort) restriction error. PortNumber" )
            if (elem.PortNumber) > 65535us then
                raise <| ConfRWException( "Max value(unsignedShort) restriction error. PortNumber" )
            yield sprintf "%s%s<PortNumber>%d</PortNumber>" singleIndent indentStr (elem.PortNumber)
            yield sprintf "%s%s<DisableNagle>%b</DisableNagle>" singleIndent indentStr (elem.DisableNagle)
            if (elem.ReceiveBufferSize) < 0 then
                raise <| ConfRWException( "Min value(int) restriction error. ReceiveBufferSize" )
            yield sprintf "%s%s<ReceiveBufferSize>%d</ReceiveBufferSize>" singleIndent indentStr (elem.ReceiveBufferSize)
            if (elem.SendBufferSize) < 0 then
                raise <| ConfRWException( "Min value(int) restriction error. SendBufferSize" )
            yield sprintf "%s%s<SendBufferSize>%d</SendBufferSize>" singleIndent indentStr (elem.SendBufferSize)
            if elem.WhiteList.Length < 0 || elem.WhiteList.Length > 16 then 
                raise <| ConfRWException( "Element count restriction error. WhiteList" )
            for itr in elem.WhiteList do
                yield sprintf "%s%s<WhiteList>%s</WhiteList>" singleIndent indentStr ( IPCondition.ToString(itr) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_NegotiableParameters data structure to configuration file.
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
    static member private T_NegotiableParameters_toString ( indent : int ) ( indentStep : int ) ( elem : T_NegotiableParameters ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if (elem.MaxRecvDataSegmentLength) < 512u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. MaxRecvDataSegmentLength" )
            if (elem.MaxRecvDataSegmentLength) > 16777215u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. MaxRecvDataSegmentLength" )
            yield sprintf "%s%s<MaxRecvDataSegmentLength>%d</MaxRecvDataSegmentLength>" singleIndent indentStr (elem.MaxRecvDataSegmentLength)
            if (elem.MaxBurstLength) < 512u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. MaxBurstLength" )
            if (elem.MaxBurstLength) > 16777215u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. MaxBurstLength" )
            yield sprintf "%s%s<MaxBurstLength>%d</MaxBurstLength>" singleIndent indentStr (elem.MaxBurstLength)
            if (elem.FirstBurstLength) < 512u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. FirstBurstLength" )
            if (elem.FirstBurstLength) > 16777215u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. FirstBurstLength" )
            yield sprintf "%s%s<FirstBurstLength>%d</FirstBurstLength>" singleIndent indentStr (elem.FirstBurstLength)
            if (elem.DefaultTime2Wait) < 0us then
                raise <| ConfRWException( "Min value(unsignedShort) restriction error. DefaultTime2Wait" )
            if (elem.DefaultTime2Wait) > 3600us then
                raise <| ConfRWException( "Max value(unsignedShort) restriction error. DefaultTime2Wait" )
            yield sprintf "%s%s<DefaultTime2Wait>%d</DefaultTime2Wait>" singleIndent indentStr (elem.DefaultTime2Wait)
            if (elem.DefaultTime2Retain) < 0us then
                raise <| ConfRWException( "Min value(unsignedShort) restriction error. DefaultTime2Retain" )
            if (elem.DefaultTime2Retain) > 3600us then
                raise <| ConfRWException( "Max value(unsignedShort) restriction error. DefaultTime2Retain" )
            yield sprintf "%s%s<DefaultTime2Retain>%d</DefaultTime2Retain>" singleIndent indentStr (elem.DefaultTime2Retain)
            if (elem.MaxOutstandingR2T) < 1us then
                raise <| ConfRWException( "Min value(unsignedShort) restriction error. MaxOutstandingR2T" )
            if (elem.MaxOutstandingR2T) > 65535us then
                raise <| ConfRWException( "Max value(unsignedShort) restriction error. MaxOutstandingR2T" )
            yield sprintf "%s%s<MaxOutstandingR2T>%d</MaxOutstandingR2T>" singleIndent indentStr (elem.MaxOutstandingR2T)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LogParameters data structure to configuration file.
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
    static member private T_LogParameters_toString ( indent : int ) ( indentStep : int ) ( elem : T_LogParameters ) ( elemName : string ) : seq<string> = 
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


