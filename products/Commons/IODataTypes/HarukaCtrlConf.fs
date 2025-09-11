//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

//=============================================================================
// Namespace declaration

namespace Haruka.IODataTypes.HarukaCtrlConf

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

type [<NoComparison>]T_HarukaCtrl = {
    RemoteCtrl : T_RemoteCtrl option;
    LogMaintenance : T_LogMaintenance option;
    LogParameters : T_LogParameters option;
}

and [<NoComparison>]T_RemoteCtrl = {
    PortNum : uint16;
    Address : string;
    WhiteList : IPCondition list;
}

and [<NoComparison>]T_LogMaintenance = {
    OutputDest : T_OutputDest;
}

and [<NoComparison>]T_OutputDest = 
    | U_ToFile of T_ToFile
    | U_ToStdout of uint32

and [<NoComparison>]T_ToFile = {
    TotalLimit : uint32;
    MaxFileCount : uint32;
    ForceSync : bool;
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
  <xsd:element name='HarukaCtrl' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='RemoteCtrl' minOccurs='0' maxOccurs='1' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='PortNum' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedShort'>
                <xsd:minInclusive value='1' />
                <xsd:maxInclusive value='65535' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='Address' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='1' />
                <xsd:maxLength value='256' />
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
      <xsd:element name='LogMaintenance' minOccurs='0' maxOccurs='1' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='OutputDest' >
            <xsd:complexType><xsd:choice>
              <xsd:element name='ToFile' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='TotalLimit' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:unsignedInt'>
                        <xsd:minInclusive value='1' />
                        <xsd:maxInclusive value='10000000' />
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='MaxFileCount' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:unsignedInt'>
                        <xsd:minInclusive value='1' />
                        <xsd:maxInclusive value='1024' />
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='ForceSync' >
                    <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
              <xsd:element name='ToStdout' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedInt'>
                    <xsd:minInclusive value='1' />
                    <xsd:maxInclusive value='10000000' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:choice></xsd:complexType>
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
    ///  Load HarukaCtrl data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded HarukaCtrl data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_HarukaCtrl =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load HarukaCtrl data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded HarukaCtrl data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_HarukaCtrl =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "HarukaCtrl" |> xdoc.Element |> ReaderWriter.Read_T_HarukaCtrl

    /// <summary>
    ///  Read T_HarukaCtrl data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_HarukaCtrl data structure.
    /// </returns>
    static member private Read_T_HarukaCtrl ( elem : XElement ) : T_HarukaCtrl = 
        {
            RemoteCtrl = 
                let subElem = elem.Element( XName.Get "RemoteCtrl" )
                if subElem = null then
                    None
                else
                    Some( ReaderWriter.Read_T_RemoteCtrl subElem );
            LogMaintenance = 
                let subElem = elem.Element( XName.Get "LogMaintenance" )
                if subElem = null then
                    None
                else
                    Some( ReaderWriter.Read_T_LogMaintenance subElem );
            LogParameters = 
                let subElem = elem.Element( XName.Get "LogParameters" )
                if subElem = null then
                    None
                else
                    Some( ReaderWriter.Read_T_LogParameters subElem );
        }

    /// <summary>
    ///  Read T_RemoteCtrl data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_RemoteCtrl data structure.
    /// </returns>
    static member private Read_T_RemoteCtrl ( elem : XElement ) : T_RemoteCtrl = 
        {
            PortNum =
                UInt16.Parse( elem.Element( XName.Get "PortNum" ).Value );
            Address =
                elem.Element( XName.Get "Address" ).Value;
            WhiteList =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "WhiteList" )
                |> Seq.map ( fun itr -> IPCondition.Parse( itr.Value ) )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_LogMaintenance data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LogMaintenance data structure.
    /// </returns>
    static member private Read_T_LogMaintenance ( elem : XElement ) : T_LogMaintenance = 
        {
            OutputDest =
                ReaderWriter.Read_T_OutputDest( elem.Element( XName.Get "OutputDest" ) );
        }

    /// <summary>
    ///  Read T_OutputDest data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_OutputDest data structure.
    /// </returns>
    static member private Read_T_OutputDest ( elem : XElement ) : T_OutputDest = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "ToFile" ->
            U_ToFile( ReaderWriter.Read_T_ToFile firstChild )
        | "ToStdout" ->
            U_ToStdout( UInt32.Parse( firstChild.Value ) )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_ToFile data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_ToFile data structure.
    /// </returns>
    static member private Read_T_ToFile ( elem : XElement ) : T_ToFile = 
        {
            TotalLimit =
                UInt32.Parse( elem.Element( XName.Get "TotalLimit" ).Value );
            MaxFileCount =
                UInt32.Parse( elem.Element( XName.Get "MaxFileCount" ).Value );
            ForceSync =
                Boolean.Parse( elem.Element( XName.Get "ForceSync" ).Value );
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
    ///  Write HarukaCtrl data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_HarukaCtrl ) : unit =
        let s = ReaderWriter.T_HarukaCtrl_toString 0 2 d "HarukaCtrl"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert HarukaCtrl data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_HarukaCtrl ) : string =
        ReaderWriter.T_HarukaCtrl_toString 0 0 d "HarukaCtrl"
        |> String.Concat

    /// <summary>
    ///  Write T_HarukaCtrl data structure to configuration file.
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
    static member private T_HarukaCtrl_toString ( indent : int ) ( indentStep : int ) ( elem : T_HarukaCtrl ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.RemoteCtrl.IsSome then
                yield! ReaderWriter.T_RemoteCtrl_toString ( indent + 1 ) indentStep ( elem.RemoteCtrl.Value ) "RemoteCtrl"
            if elem.LogMaintenance.IsSome then
                yield! ReaderWriter.T_LogMaintenance_toString ( indent + 1 ) indentStep ( elem.LogMaintenance.Value ) "LogMaintenance"
            if elem.LogParameters.IsSome then
                yield! ReaderWriter.T_LogParameters_toString ( indent + 1 ) indentStep ( elem.LogParameters.Value ) "LogParameters"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_RemoteCtrl data structure to configuration file.
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
    static member private T_RemoteCtrl_toString ( indent : int ) ( indentStep : int ) ( elem : T_RemoteCtrl ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if (elem.PortNum) < 1us then
                raise <| ConfRWException( "Min value(unsignedShort) restriction error. PortNum" )
            if (elem.PortNum) > 65535us then
                raise <| ConfRWException( "Max value(unsignedShort) restriction error. PortNum" )
            yield sprintf "%s%s<PortNum>%d</PortNum>" singleIndent indentStr (elem.PortNum)
            if (elem.Address).Length < 1 then
                raise <| ConfRWException( "Min value(string) restriction error. Address" )
            if (elem.Address).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. Address" )
            yield sprintf "%s%s<Address>%s</Address>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Address) )
            if elem.WhiteList.Length < 0 || elem.WhiteList.Length > 16 then 
                raise <| ConfRWException( "Element count restriction error. WhiteList" )
            for itr in elem.WhiteList do
                yield sprintf "%s%s<WhiteList>%s</WhiteList>" singleIndent indentStr ( IPCondition.ToString(itr) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LogMaintenance data structure to configuration file.
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
    static member private T_LogMaintenance_toString ( indent : int ) ( indentStep : int ) ( elem : T_LogMaintenance ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield! ReaderWriter.T_OutputDest_toString ( indent + 1 ) indentStep ( elem.OutputDest ) "OutputDest"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_OutputDest data structure to configuration file.
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
    static member private T_OutputDest_toString ( indent : int ) ( indentStep : int ) ( elem : T_OutputDest ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_ToFile( x ) ->
                yield! ReaderWriter.T_ToFile_toString ( indent + 1 ) indentStep ( x ) "ToFile"
            | U_ToStdout( x ) ->
                if (x) < 1u then
                    raise <| ConfRWException( "Min value(unsignedInt) restriction error. ToStdout" )
                if (x) > 10000000u then
                    raise <| ConfRWException( "Max value(unsignedInt) restriction error. ToStdout" )
                yield sprintf "%s%s<ToStdout>%d</ToStdout>" singleIndent indentStr (x)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_ToFile data structure to configuration file.
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
    static member private T_ToFile_toString ( indent : int ) ( indentStep : int ) ( elem : T_ToFile ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if (elem.TotalLimit) < 1u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. TotalLimit" )
            if (elem.TotalLimit) > 10000000u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. TotalLimit" )
            yield sprintf "%s%s<TotalLimit>%d</TotalLimit>" singleIndent indentStr (elem.TotalLimit)
            if (elem.MaxFileCount) < 1u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. MaxFileCount" )
            if (elem.MaxFileCount) > 1024u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. MaxFileCount" )
            yield sprintf "%s%s<MaxFileCount>%d</MaxFileCount>" singleIndent indentStr (elem.MaxFileCount)
            yield sprintf "%s%s<ForceSync>%b</ForceSync>" singleIndent indentStr (elem.ForceSync)
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


