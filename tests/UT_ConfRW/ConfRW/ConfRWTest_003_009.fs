//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

namespace Haruka.Test.UT.ConfRW_003_009

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_Test = {
    R027_1 : T_R027_1;
}

and [<NoComparison>]T_R027_1 = 
    | U_D1 of int
    | U_R027_2 of T_R027_2
    | U_D2 of int

and [<NoComparison>]T_R027_2 = 
    | U_D1_1 of int
    | U_D1_2 of int

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='Test' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='R027_1' >
        <xsd:complexType><xsd:choice>
          <xsd:element name='D1' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:int'>
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='R027_2' >
            <xsd:complexType><xsd:choice>
              <xsd:element name='D1_1' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:int'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='D1_2' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:int'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:choice></xsd:complexType>
          </xsd:element>
          <xsd:element name='D2' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:int'>
              </xsd:restriction>
            </xsd:simpleType>
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
        fname |> File.ReadAllText |> ReaderWriter.LoadString

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
        "Test" |> xdoc.Element |> ReaderWriter.Read_T_Test

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
            R027_1 =
                ReaderWriter.Read_T_R027_1( elem.Element( XName.Get "R027_1" ) );
        }

    /// <summary>
    ///  Read T_R027_1 data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_R027_1 data structure.
    /// </returns>
    static member private Read_T_R027_1 ( elem : XElement ) : T_R027_1 = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "D1" ->
            U_D1( Int32.Parse( firstChild.Value ) )
        | "R027_2" ->
            U_R027_2( ReaderWriter.Read_T_R027_2 firstChild )
        | "D2" ->
            U_D2( Int32.Parse( firstChild.Value ) )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_R027_2 data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_R027_2 data structure.
    /// </returns>
    static member private Read_T_R027_2 ( elem : XElement ) : T_R027_2 = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "D1_1" ->
            U_D1_1( Int32.Parse( firstChild.Value ) )
        | "D1_2" ->
            U_D1_2( Int32.Parse( firstChild.Value ) )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

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
        let s = ReaderWriter.T_Test_toString 0 2 d "Test"
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
        ReaderWriter.T_Test_toString 0 0 d "Test"
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
            yield! ReaderWriter.T_R027_1_toString ( indent + 1 ) indentStep ( elem.R027_1 ) "R027_1"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_R027_1 data structure to configuration file.
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
    static member private T_R027_1_toString ( indent : int ) ( indentStep : int ) ( elem : T_R027_1 ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_D1( x ) ->
                yield sprintf "%s%s<D1>%d</D1>" singleIndent indentStr (x)
            | U_R027_2( x ) ->
                yield! ReaderWriter.T_R027_2_toString ( indent + 1 ) indentStep ( x ) "R027_2"
            | U_D2( x ) ->
                yield sprintf "%s%s<D2>%d</D2>" singleIndent indentStr (x)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_R027_2 data structure to configuration file.
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
    static member private T_R027_2_toString ( indent : int ) ( indentStep : int ) ( elem : T_R027_2 ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_D1_1( x ) ->
                yield sprintf "%s%s<D1_1>%d</D1_1>" singleIndent indentStr (x)
            | U_D1_2( x ) ->
                yield sprintf "%s%s<D1_2>%d</D1_2>" singleIndent indentStr (x)
            yield sprintf "%s</%s>" indentStr elemName
        }


