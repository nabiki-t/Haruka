//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

namespace Haruka.Test.UT.ConfRW_003_012

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_Test = {
    D7 : T_T030_1;
    D9 : T_D9;
}

and [<NoComparison>]T_D9 = 
    | U_D9_1 of T_T030_2
    | U_D9_2 of T_T030_3

and [<NoComparison>]T_T030_1 = {
    D1 : int list;
    D2 : int list;
}

and [<NoComparison>]T_T030_2 = {
    D3 : int;
    D4 : int;
}

and [<NoComparison>]T_T030_3 = 
    | U_D5 of int
    | U_D6 of int

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='Test' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='D7' type='T030_1' ></xsd:element>
      <xsd:element name='D9' >
        <xsd:complexType><xsd:choice>
          <xsd:element name='D9_1' type='T030_2' ></xsd:element>
          <xsd:element name='D9_2' type='T030_3' ></xsd:element>
        </xsd:choice></xsd:complexType>
      </xsd:element>
    </xsd:sequence></xsd:complexType>
  </xsd:element>
  <xsd:complexType name='T030_1'>
    <xsd:sequence>
      <xsd:element name='D1' minOccurs='2' maxOccurs='3' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:int'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D2' minOccurs='2' maxOccurs='3' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:int'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>
  <xsd:complexType name='T030_2'>
    <xsd:sequence>
      <xsd:element name='D3' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:int'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D4' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:int'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>
  <xsd:complexType name='T030_3'>
    <xsd:choice>
      <xsd:element name='D5' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:int'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='D6' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:int'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
    </xsd:choice>
  </xsd:complexType>
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
            D7 =
                ReaderWriter.Read_T_T030_1( elem.Element( XName.Get "D7" ) );
            D9 =
                ReaderWriter.Read_T_D9( elem.Element( XName.Get "D9" ) );
        }

    /// <summary>
    ///  Read T_D9 data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_D9 data structure.
    /// </returns>
    static member private Read_T_D9 ( elem : XElement ) : T_D9 = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "D9_1" ->
            U_D9_1( ReaderWriter.Read_T_T030_2( firstChild ) )
        | "D9_2" ->
            U_D9_2( ReaderWriter.Read_T_T030_3( firstChild ) )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_T030_1 data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_T030_1 data structure.
    /// </returns>
    static member private Read_T_T030_1 ( elem : XElement ) : T_T030_1 = 
        {
            D1 =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "D1" )
                |> Seq.map ( fun itr -> Int32.Parse( itr.Value ) )
                |> Seq.toList
            D2 =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "D2" )
                |> Seq.map ( fun itr -> Int32.Parse( itr.Value ) )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_T030_2 data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_T030_2 data structure.
    /// </returns>
    static member private Read_T_T030_2 ( elem : XElement ) : T_T030_2 = 
        {
            D3 =
                Int32.Parse( elem.Element( XName.Get "D3" ).Value );
            D4 =
                Int32.Parse( elem.Element( XName.Get "D4" ).Value );
        }

    /// <summary>
    ///  Read T_T030_3 data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_T030_3 data structure.
    /// </returns>
    static member private Read_T_T030_3 ( elem : XElement ) : T_T030_3 = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "D5" ->
            U_D5( Int32.Parse( firstChild.Value ) )
        | "D6" ->
            U_D6( Int32.Parse( firstChild.Value ) )
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
            yield! ReaderWriter.T_T030_1_toString ( indent + 1 ) indentStep ( elem.D7 ) "D7"
            yield! ReaderWriter.T_D9_toString ( indent + 1 ) indentStep ( elem.D9 ) "D9"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_D9 data structure to configuration file.
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
    static member private T_D9_toString ( indent : int ) ( indentStep : int ) ( elem : T_D9 ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_D9_1( x ) ->
                yield! ReaderWriter.T_T030_2_toString ( indent + 1 ) indentStep ( x ) "D9_1"
            | U_D9_2( x ) ->
                yield! ReaderWriter.T_T030_3_toString ( indent + 1 ) indentStep ( x ) "D9_2"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_T030_1 data structure to configuration file.
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
    static member private T_T030_1_toString ( indent : int ) ( indentStep : int ) ( elem : T_T030_1 ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.D1.Length < 2 || elem.D1.Length > 3 then 
                raise <| ConfRWException( "Element count restriction error. D1" )
            for itr in elem.D1 do
                yield sprintf "%s%s<D1>%d</D1>" singleIndent indentStr (itr)
            if elem.D2.Length < 2 || elem.D2.Length > 3 then 
                raise <| ConfRWException( "Element count restriction error. D2" )
            for itr in elem.D2 do
                yield sprintf "%s%s<D2>%d</D2>" singleIndent indentStr (itr)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_T030_2 data structure to configuration file.
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
    static member private T_T030_2_toString ( indent : int ) ( indentStep : int ) ( elem : T_T030_2 ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<D3>%d</D3>" singleIndent indentStr (elem.D3)
            yield sprintf "%s%s<D4>%d</D4>" singleIndent indentStr (elem.D4)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_T030_3 data structure to configuration file.
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
    static member private T_T030_3_toString ( indent : int ) ( indentStep : int ) ( elem : T_T030_3 ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_D5( x ) ->
                yield sprintf "%s%s<D5>%d</D5>" singleIndent indentStr (x)
            | U_D6( x ) ->
                yield sprintf "%s%s<D6>%d</D6>" singleIndent indentStr (x)
            yield sprintf "%s</%s>" indentStr elemName
        }


