//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

namespace Haruka.IODataTypes.StringTableFormat

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_Messages = {
    Section : T_Section list;
}

and [<NoComparison>]T_Section = {
    Name : string option;
    Message : T_Message list;
}

and [<NoComparison>]T_Message = {
    Name : string;
    Value : string;
}

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='Messages' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='Section' minOccurs='0' maxOccurs='65535' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='Name' minOccurs='0' maxOccurs='1' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='Message' minOccurs='0' maxOccurs='65535' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='Name' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='Value' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
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
    ///  Load Messages data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded Messages data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_Messages =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load Messages data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded Messages data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_Messages =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "Messages" |> xdoc.Element |> ReaderWriter.Read_T_Messages

    /// <summary>
    ///  Read T_Messages data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Messages data structure.
    /// </returns>
    static member private Read_T_Messages ( elem : XElement ) : T_Messages = 
        {
            Section =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Section" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_Section itr )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_Section data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Section data structure.
    /// </returns>
    static member private Read_T_Section ( elem : XElement ) : T_Section = 
        {
            Name = 
                let subElem = elem.Element( XName.Get "Name" )
                if subElem = null then
                    None
                else
                    Some( subElem.Value );
            Message =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Message" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_Message itr )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_Message data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Message data structure.
    /// </returns>
    static member private Read_T_Message ( elem : XElement ) : T_Message = 
        {
            Name =
                elem.Element( XName.Get "Name" ).Value;
            Value =
                elem.Element( XName.Get "Value" ).Value;
        }

    /// <summary>
    ///  Write Messages data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_Messages ) : unit =
        let s = ReaderWriter.T_Messages_toString 0 2 d "Messages"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert Messages data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_Messages ) : string =
        ReaderWriter.T_Messages_toString 0 0 d "Messages"
        |> String.Concat

    /// <summary>
    ///  Write T_Messages data structure to configuration file.
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
    static member private T_Messages_toString ( indent : int ) ( indentStep : int ) ( elem : T_Messages ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.Section.Length < 0 || elem.Section.Length > 65535 then 
                raise <| ConfRWException( "Element count restriction error. Section" )
            for itr in elem.Section do
                yield! ReaderWriter.T_Section_toString ( indent + 1 ) indentStep itr "Section"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Section data structure to configuration file.
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
    static member private T_Section_toString ( indent : int ) ( indentStep : int ) ( elem : T_Section ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.Name.IsSome then
                yield sprintf "%s%s<Name>%s</Name>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Name.Value) )
            if elem.Message.Length < 0 || elem.Message.Length > 65535 then 
                raise <| ConfRWException( "Element count restriction error. Message" )
            for itr in elem.Message do
                yield! ReaderWriter.T_Message_toString ( indent + 1 ) indentStep itr "Message"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Message data structure to configuration file.
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
    static member private T_Message_toString ( indent : int ) ( indentStep : int ) ( elem : T_Message ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Name>%s</Name>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Name) )
            yield sprintf "%s%s<Value>%s</Value>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Value) )
            yield sprintf "%s</%s>" indentStr elemName
        }


