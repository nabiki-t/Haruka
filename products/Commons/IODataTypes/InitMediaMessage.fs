//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

namespace Haruka.IODataTypes.InitMediaMessage

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_OutputLine = {
    LineType : T_LineType;
}

and [<NoComparison>]T_LineType = 
    | U_Start of unit
    | U_CreateFile of string
    | U_Progress of uint8
    | U_End of string
    | U_ErrorMessage of string

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='OutputLine' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='LineType' >
        <xsd:complexType><xsd:choice>
          <xsd:element name='Start' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:int'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='0' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='CreateFile' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='0' />
                <xsd:maxLength value='256' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='Progress' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedByte'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='100' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='End' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='0' />
                <xsd:maxLength value='256' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='ErrorMessage' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='0' />
                <xsd:maxLength value='256' />
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
    ///  Load OutputLine data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded OutputLine data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_OutputLine =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load OutputLine data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded OutputLine data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_OutputLine =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "OutputLine" |> xdoc.Element |> ReaderWriter.Read_T_OutputLine

    /// <summary>
    ///  Read T_OutputLine data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_OutputLine data structure.
    /// </returns>
    static member private Read_T_OutputLine ( elem : XElement ) : T_OutputLine = 
        {
            LineType =
                ReaderWriter.Read_T_LineType( elem.Element( XName.Get "LineType" ) );
        }

    /// <summary>
    ///  Read T_LineType data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LineType data structure.
    /// </returns>
    static member private Read_T_LineType ( elem : XElement ) : T_LineType = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "Start" ->
            U_Start( () )
        | "CreateFile" ->
            U_CreateFile( firstChild.Value )
        | "Progress" ->
            U_Progress( Byte.Parse( firstChild.Value ) )
        | "End" ->
            U_End( firstChild.Value )
        | "ErrorMessage" ->
            U_ErrorMessage( firstChild.Value )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Write OutputLine data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_OutputLine ) : unit =
        let s = ReaderWriter.T_OutputLine_toString 0 2 d "OutputLine"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert OutputLine data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_OutputLine ) : string =
        ReaderWriter.T_OutputLine_toString 0 0 d "OutputLine"
        |> String.Concat

    /// <summary>
    ///  Write T_OutputLine data structure to configuration file.
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
    static member private T_OutputLine_toString ( indent : int ) ( indentStep : int ) ( elem : T_OutputLine ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield! ReaderWriter.T_LineType_toString ( indent + 1 ) indentStep ( elem.LineType ) "LineType"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LineType data structure to configuration file.
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
    static member private T_LineType_toString ( indent : int ) ( indentStep : int ) ( elem : T_LineType ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_Start( x ) ->
                yield sprintf "%s%s<Start>0</Start>" singleIndent indentStr
            | U_CreateFile( x ) ->
                if (x).Length < 0 then
                    raise <| ConfRWException( "Min value(string) restriction error. CreateFile" )
                if (x).Length > 256 then
                    raise <| ConfRWException( "Max value(string) restriction error. CreateFile" )
                yield sprintf "%s%s<CreateFile>%s</CreateFile>" singleIndent indentStr ( ReaderWriter.xmlEncode(x) )
            | U_Progress( x ) ->
                if (x) < 0uy then
                    raise <| ConfRWException( "Min value(unsignedByte) restriction error. Progress" )
                if (x) > 100uy then
                    raise <| ConfRWException( "Max value(unsignedByte) restriction error. Progress" )
                yield sprintf "%s%s<Progress>%d</Progress>" singleIndent indentStr (x)
            | U_End( x ) ->
                if (x).Length < 0 then
                    raise <| ConfRWException( "Min value(string) restriction error. End" )
                if (x).Length > 256 then
                    raise <| ConfRWException( "Max value(string) restriction error. End" )
                yield sprintf "%s%s<End>%s</End>" singleIndent indentStr ( ReaderWriter.xmlEncode(x) )
            | U_ErrorMessage( x ) ->
                if (x).Length < 0 then
                    raise <| ConfRWException( "Min value(string) restriction error. ErrorMessage" )
                if (x).Length > 256 then
                    raise <| ConfRWException( "Max value(string) restriction error. ErrorMessage" )
                yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(x) )
            yield sprintf "%s</%s>" indentStr elemName
        }


