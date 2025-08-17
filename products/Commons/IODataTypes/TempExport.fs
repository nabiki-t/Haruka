//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

namespace Haruka.IODataTypes.TempExport

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_ExportData = {
    AppVersion : T_AppVersion;
    RootNode : uint64;
    Relationship : T_Relationship list;
    Node : T_Node list;
}

and [<NoComparison>]T_AppVersion = {
    Major : uint32;
    Minor : uint32;
    Rivision : uint64;
}

and [<NoComparison>]T_Relationship = {
    NodeID : uint64;
    Child : uint64 list;
}

and [<NoComparison>]T_Node = {
    TypeName : string;
    NodeID : uint64;
    Values : T_Values list;
}

and [<NoComparison>]T_Values = {
    Name : string;
    Value : string;
}

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='ExportData' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='AppVersion' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='Major' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedInt'>
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='Minor' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedInt'>
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='Rivision' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedLong'>
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
        </xsd:sequence></xsd:complexType>
      </xsd:element>
      <xsd:element name='RootNode' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:unsignedLong'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='Relationship' minOccurs='1' maxOccurs='200000' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='NodeID' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedLong'>
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='Child' minOccurs='0' maxOccurs='1024' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedLong'>
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
        </xsd:sequence></xsd:complexType>
      </xsd:element>
      <xsd:element name='Node' minOccurs='1' maxOccurs='200000' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='TypeName' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='NodeID' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedLong'>
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='Values' minOccurs='0' maxOccurs='256' >
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
    ///  Load ExportData data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded ExportData data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_ExportData =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load ExportData data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded ExportData data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_ExportData =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "ExportData" |> xdoc.Element |> ReaderWriter.Read_T_ExportData

    /// <summary>
    ///  Read T_ExportData data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_ExportData data structure.
    /// </returns>
    static member private Read_T_ExportData ( elem : XElement ) : T_ExportData = 
        {
            AppVersion =
                ReaderWriter.Read_T_AppVersion( elem.Element( XName.Get "AppVersion" ) );
            RootNode =
                UInt64.Parse( elem.Element( XName.Get "RootNode" ).Value );
            Relationship =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Relationship" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_Relationship itr )
                |> Seq.toList
            Node =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Node" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_Node itr )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_AppVersion data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_AppVersion data structure.
    /// </returns>
    static member private Read_T_AppVersion ( elem : XElement ) : T_AppVersion = 
        {
            Major =
                UInt32.Parse( elem.Element( XName.Get "Major" ).Value );
            Minor =
                UInt32.Parse( elem.Element( XName.Get "Minor" ).Value );
            Rivision =
                UInt64.Parse( elem.Element( XName.Get "Rivision" ).Value );
        }

    /// <summary>
    ///  Read T_Relationship data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Relationship data structure.
    /// </returns>
    static member private Read_T_Relationship ( elem : XElement ) : T_Relationship = 
        {
            NodeID =
                UInt64.Parse( elem.Element( XName.Get "NodeID" ).Value );
            Child =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Child" )
                |> Seq.map ( fun itr -> UInt64.Parse( itr.Value ) )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_Node data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Node data structure.
    /// </returns>
    static member private Read_T_Node ( elem : XElement ) : T_Node = 
        {
            TypeName =
                elem.Element( XName.Get "TypeName" ).Value;
            NodeID =
                UInt64.Parse( elem.Element( XName.Get "NodeID" ).Value );
            Values =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Values" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_Values itr )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_Values data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Values data structure.
    /// </returns>
    static member private Read_T_Values ( elem : XElement ) : T_Values = 
        {
            Name =
                elem.Element( XName.Get "Name" ).Value;
            Value =
                elem.Element( XName.Get "Value" ).Value;
        }

    /// <summary>
    ///  Write ExportData data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_ExportData ) : unit =
        let s = ReaderWriter.T_ExportData_toString 0 2 d "ExportData"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert ExportData data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_ExportData ) : string =
        ReaderWriter.T_ExportData_toString 0 0 d "ExportData"
        |> String.Concat

    /// <summary>
    ///  Write T_ExportData data structure to configuration file.
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
    static member private T_ExportData_toString ( indent : int ) ( indentStep : int ) ( elem : T_ExportData ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield! ReaderWriter.T_AppVersion_toString ( indent + 1 ) indentStep ( elem.AppVersion ) "AppVersion"
            yield sprintf "%s%s<RootNode>%d</RootNode>" singleIndent indentStr (elem.RootNode)
            if elem.Relationship.Length < 1 || elem.Relationship.Length > 200000 then 
                raise <| ConfRWException( "Element count restriction error. Relationship" )
            for itr in elem.Relationship do
                yield! ReaderWriter.T_Relationship_toString ( indent + 1 ) indentStep itr "Relationship"
            if elem.Node.Length < 1 || elem.Node.Length > 200000 then 
                raise <| ConfRWException( "Element count restriction error. Node" )
            for itr in elem.Node do
                yield! ReaderWriter.T_Node_toString ( indent + 1 ) indentStep itr "Node"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_AppVersion data structure to configuration file.
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
    static member private T_AppVersion_toString ( indent : int ) ( indentStep : int ) ( elem : T_AppVersion ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Major>%d</Major>" singleIndent indentStr (elem.Major)
            yield sprintf "%s%s<Minor>%d</Minor>" singleIndent indentStr (elem.Minor)
            yield sprintf "%s%s<Rivision>%d</Rivision>" singleIndent indentStr (elem.Rivision)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Relationship data structure to configuration file.
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
    static member private T_Relationship_toString ( indent : int ) ( indentStep : int ) ( elem : T_Relationship ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<NodeID>%d</NodeID>" singleIndent indentStr (elem.NodeID)
            if elem.Child.Length < 0 || elem.Child.Length > 1024 then 
                raise <| ConfRWException( "Element count restriction error. Child" )
            for itr in elem.Child do
                yield sprintf "%s%s<Child>%d</Child>" singleIndent indentStr (itr)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Node data structure to configuration file.
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
    static member private T_Node_toString ( indent : int ) ( indentStep : int ) ( elem : T_Node ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TypeName>%s</TypeName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.TypeName) )
            yield sprintf "%s%s<NodeID>%d</NodeID>" singleIndent indentStr (elem.NodeID)
            if elem.Values.Length < 0 || elem.Values.Length > 256 then 
                raise <| ConfRWException( "Element count restriction error. Values" )
            for itr in elem.Values do
                yield! ReaderWriter.T_Values_toString ( indent + 1 ) indentStep itr "Values"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Values data structure to configuration file.
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
    static member private T_Values_toString ( indent : int ) ( indentStep : int ) ( elem : T_Values ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Name>%s</Name>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Name) )
            yield sprintf "%s%s<Value>%s</Value>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Value) )
            yield sprintf "%s</%s>" indentStr elemName
        }


