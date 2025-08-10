//=============================================================================
// Haruka Software Storage.
// Definition of ConfRW_UT002_IPCondition configuration reader/writer function.

namespace Haruka.Test.UT.ConfRW_002_IPCondition

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_Test = {
    D1 : IPCondition;
    D2 : IPCondition list;
    D3 : IPCondition option;
    D4 : IPCondition;
    D5 : IPCondition;
    D7 : IPCondition;
}

///  ConfRW_UT002_IPCondition class imprements read and write function of configuration.
type ConfRW_UT002_IPCondition() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='Test' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='D1' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
            <xsd:pattern value='^Any|Loopback|Linklocal|Private|Multicast|Global|IPv4Any|IPv4Loopback|IPv4Linklocal|IPv4Private|IPv4Multicast|IPv4Global|IPv6Any|IPv6Loopback|IPv6Linklocal|IPv6Private|IPv6Multicast|IPv6Global|IPFilter\( *[^ ,\)]{1,} *, *[^ ,\)]{1,} *\)$' />
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D2' minOccurs='2' maxOccurs='3' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
            <xsd:pattern value='^Any|Loopback|Linklocal|Private|Multicast|Global|IPv4Any|IPv4Loopback|IPv4Linklocal|IPv4Private|IPv4Multicast|IPv4Global|IPv6Any|IPv6Loopback|IPv6Linklocal|IPv6Private|IPv6Multicast|IPv6Global|IPFilter\( *[^ ,\)]{1,} *, *[^ ,\)]{1,} *\)$' />
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D3' minOccurs='0' maxOccurs='1' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
            <xsd:pattern value='^Any|Loopback|Linklocal|Private|Multicast|Global|IPv4Any|IPv4Loopback|IPv4Linklocal|IPv4Private|IPv4Multicast|IPv4Global|IPv6Any|IPv6Loopback|IPv6Linklocal|IPv6Private|IPv6Multicast|IPv6Global|IPFilter\( *[^ ,\)]{1,} *, *[^ ,\)]{1,} *\)$' />
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D7' minOccurs='0' maxOccurs='1' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
            <xsd:pattern value='^Any|Loopback|Linklocal|Private|Multicast|Global|IPv4Any|IPv4Loopback|IPv4Linklocal|IPv4Private|IPv4Multicast|IPv4Global|IPv6Any|IPv6Loopback|IPv6Linklocal|IPv6Private|IPv6Multicast|IPv6Global|IPFilter\( *[^ ,\)]{1,} *, *[^ ,\)]{1,} *\)$' />
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
    ///  Load Test data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded Test data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_Test =
        fname |> File.ReadAllText |> ConfRW_UT002_IPCondition.LoadString

    /// <summary>
    ///  Load Test data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded Test data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_Test =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "Test" |> xdoc.Element |> ConfRW_UT002_IPCondition.Read_T_Test

    /// <summary>
    ///  Read T_Test data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Test data structure.
    /// </returns>
    static member private Read_T_Test ( elem : XElement ) : T_Test = 
        {
            D1 =
                IPCondition.Parse( elem.Element( XName.Get "D1" ).Value );
            D2 =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "D2" )
                |> Seq.map ( fun itr -> IPCondition.Parse( itr.Value ) )
                |> Seq.toList
            D3 = 
                let subElem = elem.Element( XName.Get "D3" )
                if subElem = null then
                    None
                else
                    Some( IPCondition.Parse( subElem.Value ) );
            D4 = IPCondition.Loopback;
            D5 = IPCondition.Parse( "Multicast" );
            D7 = 
                let subElem = elem.Element( XName.Get "D7" )
                if subElem = null then
                    IPCondition.Parse( "IPv6Linklocal" );
                else
                    IPCondition.Parse( subElem.Value );
        }

    /// <summary>
    ///  Write Test data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_Test ) : unit =
        let s = ConfRW_UT002_IPCondition.T_Test_toString 0 2 d "Test"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert Test data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_Test ) : string =
        ConfRW_UT002_IPCondition.T_Test_toString 0 0 d "Test"
        |> String.Concat

    /// <summary>
    ///  Write T_Test data structure to configuration file.
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
    static member private T_Test_toString ( indent : int ) ( indentStep : int ) ( elem : T_Test ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<D1>%s</D1>" singleIndent indentStr ( IPCondition.ToString(elem.D1) )
            if elem.D2.Length < 2 || elem.D2.Length > 3 then 
                raise <| ConfRWException( "Element count restriction error. D2" )
            for itr in elem.D2 do
                yield sprintf "%s%s<D2>%s</D2>" singleIndent indentStr ( IPCondition.ToString(itr) )
            if elem.D3.IsSome then
                yield sprintf "%s%s<D3>%s</D3>" singleIndent indentStr ( IPCondition.ToString(elem.D3.Value) )
            yield sprintf "%s%s<D7>%s</D7>" singleIndent indentStr ( IPCondition.ToString(elem.D7) )
            yield sprintf "%s</%s>" indentStr elemName
        }


