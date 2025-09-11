//=============================================================================
// Haruka Software Storage.
// Definition of ConfRW_UT002_unsignedShort configuration reader/writer function.

//=============================================================================
// Namespace declaration

namespace Haruka.Test.UT.ConfRW_002_unsignedShort

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

type [<NoComparison>]T_Test = {
    D1 : uint16;
    D2 : uint16 list;
    D3 : uint16 option;
    D4 : uint16;
    D5 : uint16;
    D6 : uint16;
    D7 : uint16;
    D8 : uint16;
}

//=============================================================================
// Class implementation

///  ConfRW_UT002_unsignedShort class imprements read and write function of configuration.
type ConfRW_UT002_unsignedShort() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='Test' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='D1' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:unsignedShort'>
            <xsd:minInclusive value='1' />
            <xsd:maxInclusive value='2' />
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D2' minOccurs='2' maxOccurs='3' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:unsignedShort'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D3' minOccurs='0' maxOccurs='1' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:unsignedShort'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D7' minOccurs='0' maxOccurs='1' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:unsignedShort'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D8' minOccurs='0' maxOccurs='1' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:unsignedShort'>
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
        fname |> File.ReadAllText |> ConfRW_UT002_unsignedShort.LoadString

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
        "Test" |> xdoc.Element |> ConfRW_UT002_unsignedShort.Read_T_Test

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
                UInt16.Parse( elem.Element( XName.Get "D1" ).Value );
            D2 =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "D2" )
                |> Seq.map ( fun itr -> UInt16.Parse( itr.Value ) )
                |> Seq.toList
            D3 = 
                let subElem = elem.Element( XName.Get "D3" )
                if subElem = null then
                    None
                else
                    Some( UInt16.Parse( subElem.Value ) );
            D4 = 0us;
            D5 = 99us;
            D6 = 16us;
            D7 = 
                let subElem = elem.Element( XName.Get "D7" )
                if subElem = null then
                    98us;
                else
                    UInt16.Parse( subElem.Value );
            D8 = 
                let subElem = elem.Element( XName.Get "D8" )
                if subElem = null then
                    16us;
                else
                    UInt16.Parse( subElem.Value );
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
        let s = ConfRW_UT002_unsignedShort.T_Test_toString 0 2 d "Test"
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
        ConfRW_UT002_unsignedShort.T_Test_toString 0 0 d "Test"
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
            if (elem.D1) < 1us then
                raise <| ConfRWException( "Min value(unsignedShort) restriction error. D1" )
            if (elem.D1) > 2us then
                raise <| ConfRWException( "Max value(unsignedShort) restriction error. D1" )
            yield sprintf "%s%s<D1>%d</D1>" singleIndent indentStr (elem.D1)
            if elem.D2.Length < 2 || elem.D2.Length > 3 then 
                raise <| ConfRWException( "Element count restriction error. D2" )
            for itr in elem.D2 do
                yield sprintf "%s%s<D2>%d</D2>" singleIndent indentStr (itr)
            if elem.D3.IsSome then
                yield sprintf "%s%s<D3>%d</D3>" singleIndent indentStr (elem.D3.Value)
            yield sprintf "%s%s<D7>%d</D7>" singleIndent indentStr (elem.D7)
            yield sprintf "%s%s<D8>%d</D8>" singleIndent indentStr (elem.D8)
            yield sprintf "%s</%s>" indentStr elemName
        }


