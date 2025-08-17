//=============================================================================
// Haruka Software Storage.
// Definition of ConfRW_UT002_string configuration reader/writer function.

namespace Haruka.Test.UT.ConfRW_002_string

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_Test = {
    D1 : string;
    D2 : string;
    D3 : string list;
    D4 : string option;
    D5 : string;
    D6 : string;
    D7 : string;
    D8 : string;
    D9 : string;
}

///  ConfRW_UT002_string class imprements read and write function of configuration.
type ConfRW_UT002_string() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='Test' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='D1' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
            <xsd:minLength value='2' />
            <xsd:maxLength value='5' />
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D2' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
            <xsd:pattern value='^a.*c$' />
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D3' minOccurs='2' maxOccurs='3' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D4' minOccurs='0' maxOccurs='1' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D8' minOccurs='0' maxOccurs='1' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D9' minOccurs='0' maxOccurs='1' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
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
        fname |> File.ReadAllText |> ConfRW_UT002_string.LoadString

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
        "Test" |> xdoc.Element |> ConfRW_UT002_string.Read_T_Test

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
                elem.Element( XName.Get "D1" ).Value;
            D2 =
                elem.Element( XName.Get "D2" ).Value;
            D3 =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "D3" )
                |> Seq.map ( fun itr -> itr.Value )
                |> Seq.toList
            D4 = 
                let subElem = elem.Element( XName.Get "D4" )
                if subElem = null then
                    None
                else
                    Some( subElem.Value );
            D5 = "";
            D6 = "a01";
            D7 = "Haruka";
            D8 = 
                let subElem = elem.Element( XName.Get "D8" )
                if subElem = null then
                    "b01";
                else
                    subElem.Value;
            D9 = 
                let subElem = elem.Element( XName.Get "D9" )
                if subElem = null then
                    "Haruka";
                else
                    subElem.Value;
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
        let s = ConfRW_UT002_string.T_Test_toString 0 2 d "Test"
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
        ConfRW_UT002_string.T_Test_toString 0 0 d "Test"
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
            if (elem.D1).Length < 2 then
                raise <| ConfRWException( "Min value(string) restriction error. D1" )
            if (elem.D1).Length > 5 then
                raise <| ConfRWException( "Max value(string) restriction error. D1" )
            yield sprintf "%s%s<D1>%s</D1>" singleIndent indentStr ( ConfRW_UT002_string.xmlEncode(elem.D1) )
            if not( System.Text.RegularExpressions.Regex.IsMatch( elem.D2, "^a.*c$" ) ) then
                raise <| ConfRWException( "Pattern restriction error. D2" )
            yield sprintf "%s%s<D2>%s</D2>" singleIndent indentStr ( ConfRW_UT002_string.xmlEncode(elem.D2) )
            if elem.D3.Length < 2 || elem.D3.Length > 3 then 
                raise <| ConfRWException( "Element count restriction error. D3" )
            for itr in elem.D3 do
                yield sprintf "%s%s<D3>%s</D3>" singleIndent indentStr ( ConfRW_UT002_string.xmlEncode(itr) )
            if elem.D4.IsSome then
                yield sprintf "%s%s<D4>%s</D4>" singleIndent indentStr ( ConfRW_UT002_string.xmlEncode(elem.D4.Value) )
            yield sprintf "%s%s<D8>%s</D8>" singleIndent indentStr ( ConfRW_UT002_string.xmlEncode(elem.D8) )
            yield sprintf "%s%s<D9>%s</D9>" singleIndent indentStr ( ConfRW_UT002_string.xmlEncode(elem.D9) )
            yield sprintf "%s</%s>" indentStr elemName
        }


