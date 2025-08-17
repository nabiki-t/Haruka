//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

namespace Haruka.IODataTypes.MediaCtrlRes

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_MediaCtrlRes = {
    Response : T_Response;
}

and [<NoComparison>]T_Response = 
    | U_Debug of T_Debug
    | U_Unexpected of string

and [<NoComparison>]T_Debug = 
    | U_AllTraps of T_AllTraps
    | U_AddTrapResult of T_AddTrapResult
    | U_ClearTrapsResult of T_ClearTrapsResult
    | U_CounterValue of int

and [<NoComparison>]T_AllTraps = {
    Trap : T_Trap list;
}

and [<NoComparison>]T_Trap = {
    Event : T_Event;
    Action : T_Action;
}

and [<NoComparison>]T_Event = 
    | U_TestUnitReady of unit
    | U_ReadCapacity of unit
    | U_Read of T_Read
    | U_Write of T_Write
    | U_Format of unit

and [<NoComparison>]T_Read = {
    StartLBA : uint64;
    EndLBA : uint64;
}

and [<NoComparison>]T_Write = {
    StartLBA : uint64;
    EndLBA : uint64;
}

and [<NoComparison>]T_Action = 
    | U_ACA of string
    | U_LUReset of string
    | U_Count of T_Count
    | U_Delay of int

and [<NoComparison>]T_Count = {
    Index : int;
    Value : int;
}

and [<NoComparison>]T_AddTrapResult = {
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_ClearTrapsResult = {
    Result : bool;
    ErrorMessage : string;
}

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='MediaCtrlRes' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='Response' >
        <xsd:complexType><xsd:choice>
          <xsd:element name='Debug' >
            <xsd:complexType><xsd:choice>
              <xsd:element name='AllTraps' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='Trap' minOccurs='0' maxOccurs='16' >
                    <xsd:complexType><xsd:sequence>
                      <xsd:element name='Event' >
                        <xsd:complexType><xsd:choice>
                          <xsd:element name='TestUnitReady' >
                            <xsd:simpleType>
                              <xsd:restriction base='xsd:int'>
                                <xsd:minInclusive value='0' />
                                <xsd:maxInclusive value='0' />
                              </xsd:restriction>
                            </xsd:simpleType>
                          </xsd:element>
                          <xsd:element name='ReadCapacity' >
                            <xsd:simpleType>
                              <xsd:restriction base='xsd:int'>
                                <xsd:minInclusive value='0' />
                                <xsd:maxInclusive value='0' />
                              </xsd:restriction>
                            </xsd:simpleType>
                          </xsd:element>
                          <xsd:element name='Read' >
                            <xsd:complexType><xsd:sequence>
                              <xsd:element name='StartLBA' >
                                <xsd:simpleType>
                                  <xsd:restriction base='xsd:unsignedLong'>
                                  </xsd:restriction>
                                </xsd:simpleType>
                              </xsd:element>
                              <xsd:element name='EndLBA' >
                                <xsd:simpleType>
                                  <xsd:restriction base='xsd:unsignedLong'>
                                  </xsd:restriction>
                                </xsd:simpleType>
                              </xsd:element>
                            </xsd:sequence></xsd:complexType>
                          </xsd:element>
                          <xsd:element name='Write' >
                            <xsd:complexType><xsd:sequence>
                              <xsd:element name='StartLBA' >
                                <xsd:simpleType>
                                  <xsd:restriction base='xsd:unsignedLong'>
                                  </xsd:restriction>
                                </xsd:simpleType>
                              </xsd:element>
                              <xsd:element name='EndLBA' >
                                <xsd:simpleType>
                                  <xsd:restriction base='xsd:unsignedLong'>
                                  </xsd:restriction>
                                </xsd:simpleType>
                              </xsd:element>
                            </xsd:sequence></xsd:complexType>
                          </xsd:element>
                          <xsd:element name='Format' >
                            <xsd:simpleType>
                              <xsd:restriction base='xsd:int'>
                                <xsd:minInclusive value='0' />
                                <xsd:maxInclusive value='0' />
                              </xsd:restriction>
                            </xsd:simpleType>
                          </xsd:element>
                        </xsd:choice></xsd:complexType>
                      </xsd:element>
                      <xsd:element name='Action' >
                        <xsd:complexType><xsd:choice>
                          <xsd:element name='ACA' >
                            <xsd:simpleType>
                              <xsd:restriction base='xsd:string'>
                              </xsd:restriction>
                            </xsd:simpleType>
                          </xsd:element>
                          <xsd:element name='LUReset' >
                            <xsd:simpleType>
                              <xsd:restriction base='xsd:string'>
                              </xsd:restriction>
                            </xsd:simpleType>
                          </xsd:element>
                          <xsd:element name='Count' >
                            <xsd:complexType><xsd:sequence>
                              <xsd:element name='Index' >
                                <xsd:simpleType>
                                  <xsd:restriction base='xsd:int'>
                                  </xsd:restriction>
                                </xsd:simpleType>
                              </xsd:element>
                              <xsd:element name='Value' >
                                <xsd:simpleType>
                                  <xsd:restriction base='xsd:int'>
                                  </xsd:restriction>
                                </xsd:simpleType>
                              </xsd:element>
                            </xsd:sequence></xsd:complexType>
                          </xsd:element>
                          <xsd:element name='Delay' >
                            <xsd:simpleType>
                              <xsd:restriction base='xsd:int'>
                              </xsd:restriction>
                            </xsd:simpleType>
                          </xsd:element>
                        </xsd:choice></xsd:complexType>
                      </xsd:element>
                    </xsd:sequence></xsd:complexType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
              <xsd:element name='AddTrapResult' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='Result' >
                    <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='ErrorMessage' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:string'>
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
              <xsd:element name='ClearTrapsResult' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='Result' >
                    <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='ErrorMessage' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:string'>
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
              <xsd:element name='CounterValue' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:int'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:choice></xsd:complexType>
          </xsd:element>
          <xsd:element name='Unexpected' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
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
    ///  Load MediaCtrlRes data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded MediaCtrlRes data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_MediaCtrlRes =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load MediaCtrlRes data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded MediaCtrlRes data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_MediaCtrlRes =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "MediaCtrlRes" |> xdoc.Element |> ReaderWriter.Read_T_MediaCtrlRes

    /// <summary>
    ///  Read T_MediaCtrlRes data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_MediaCtrlRes data structure.
    /// </returns>
    static member private Read_T_MediaCtrlRes ( elem : XElement ) : T_MediaCtrlRes = 
        {
            Response =
                ReaderWriter.Read_T_Response( elem.Element( XName.Get "Response" ) );
        }

    /// <summary>
    ///  Read T_Response data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Response data structure.
    /// </returns>
    static member private Read_T_Response ( elem : XElement ) : T_Response = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "Debug" ->
            U_Debug( ReaderWriter.Read_T_Debug firstChild )
        | "Unexpected" ->
            U_Unexpected( firstChild.Value )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_Debug data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Debug data structure.
    /// </returns>
    static member private Read_T_Debug ( elem : XElement ) : T_Debug = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "AllTraps" ->
            U_AllTraps( ReaderWriter.Read_T_AllTraps firstChild )
        | "AddTrapResult" ->
            U_AddTrapResult( ReaderWriter.Read_T_AddTrapResult firstChild )
        | "ClearTrapsResult" ->
            U_ClearTrapsResult( ReaderWriter.Read_T_ClearTrapsResult firstChild )
        | "CounterValue" ->
            U_CounterValue( Int32.Parse( firstChild.Value ) )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_AllTraps data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_AllTraps data structure.
    /// </returns>
    static member private Read_T_AllTraps ( elem : XElement ) : T_AllTraps = 
        {
            Trap =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Trap" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_Trap itr )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_Trap data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Trap data structure.
    /// </returns>
    static member private Read_T_Trap ( elem : XElement ) : T_Trap = 
        {
            Event =
                ReaderWriter.Read_T_Event( elem.Element( XName.Get "Event" ) );
            Action =
                ReaderWriter.Read_T_Action( elem.Element( XName.Get "Action" ) );
        }

    /// <summary>
    ///  Read T_Event data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Event data structure.
    /// </returns>
    static member private Read_T_Event ( elem : XElement ) : T_Event = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "TestUnitReady" ->
            U_TestUnitReady( () )
        | "ReadCapacity" ->
            U_ReadCapacity( () )
        | "Read" ->
            U_Read( ReaderWriter.Read_T_Read firstChild )
        | "Write" ->
            U_Write( ReaderWriter.Read_T_Write firstChild )
        | "Format" ->
            U_Format( () )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_Read data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Read data structure.
    /// </returns>
    static member private Read_T_Read ( elem : XElement ) : T_Read = 
        {
            StartLBA =
                UInt64.Parse( elem.Element( XName.Get "StartLBA" ).Value );
            EndLBA =
                UInt64.Parse( elem.Element( XName.Get "EndLBA" ).Value );
        }

    /// <summary>
    ///  Read T_Write data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Write data structure.
    /// </returns>
    static member private Read_T_Write ( elem : XElement ) : T_Write = 
        {
            StartLBA =
                UInt64.Parse( elem.Element( XName.Get "StartLBA" ).Value );
            EndLBA =
                UInt64.Parse( elem.Element( XName.Get "EndLBA" ).Value );
        }

    /// <summary>
    ///  Read T_Action data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Action data structure.
    /// </returns>
    static member private Read_T_Action ( elem : XElement ) : T_Action = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "ACA" ->
            U_ACA( firstChild.Value )
        | "LUReset" ->
            U_LUReset( firstChild.Value )
        | "Count" ->
            U_Count( ReaderWriter.Read_T_Count firstChild )
        | "Delay" ->
            U_Delay( Int32.Parse( firstChild.Value ) )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_Count data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Count data structure.
    /// </returns>
    static member private Read_T_Count ( elem : XElement ) : T_Count = 
        {
            Index =
                Int32.Parse( elem.Element( XName.Get "Index" ).Value );
            Value =
                Int32.Parse( elem.Element( XName.Get "Value" ).Value );
        }

    /// <summary>
    ///  Read T_AddTrapResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_AddTrapResult data structure.
    /// </returns>
    static member private Read_T_AddTrapResult ( elem : XElement ) : T_AddTrapResult = 
        {
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_ClearTrapsResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_ClearTrapsResult data structure.
    /// </returns>
    static member private Read_T_ClearTrapsResult ( elem : XElement ) : T_ClearTrapsResult = 
        {
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Write MediaCtrlRes data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_MediaCtrlRes ) : unit =
        let s = ReaderWriter.T_MediaCtrlRes_toString 0 2 d "MediaCtrlRes"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert MediaCtrlRes data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_MediaCtrlRes ) : string =
        ReaderWriter.T_MediaCtrlRes_toString 0 0 d "MediaCtrlRes"
        |> String.Concat

    /// <summary>
    ///  Write T_MediaCtrlRes data structure to configuration file.
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
    static member private T_MediaCtrlRes_toString ( indent : int ) ( indentStep : int ) ( elem : T_MediaCtrlRes ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield! ReaderWriter.T_Response_toString ( indent + 1 ) indentStep ( elem.Response ) "Response"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Response data structure to configuration file.
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
    static member private T_Response_toString ( indent : int ) ( indentStep : int ) ( elem : T_Response ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_Debug( x ) ->
                yield! ReaderWriter.T_Debug_toString ( indent + 1 ) indentStep ( x ) "Debug"
            | U_Unexpected( x ) ->
                yield sprintf "%s%s<Unexpected>%s</Unexpected>" singleIndent indentStr ( ReaderWriter.xmlEncode(x) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Debug data structure to configuration file.
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
    static member private T_Debug_toString ( indent : int ) ( indentStep : int ) ( elem : T_Debug ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_AllTraps( x ) ->
                yield! ReaderWriter.T_AllTraps_toString ( indent + 1 ) indentStep ( x ) "AllTraps"
            | U_AddTrapResult( x ) ->
                yield! ReaderWriter.T_AddTrapResult_toString ( indent + 1 ) indentStep ( x ) "AddTrapResult"
            | U_ClearTrapsResult( x ) ->
                yield! ReaderWriter.T_ClearTrapsResult_toString ( indent + 1 ) indentStep ( x ) "ClearTrapsResult"
            | U_CounterValue( x ) ->
                yield sprintf "%s%s<CounterValue>%d</CounterValue>" singleIndent indentStr (x)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_AllTraps data structure to configuration file.
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
    static member private T_AllTraps_toString ( indent : int ) ( indentStep : int ) ( elem : T_AllTraps ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.Trap.Length < 0 || elem.Trap.Length > 16 then 
                raise <| ConfRWException( "Element count restriction error. Trap" )
            for itr in elem.Trap do
                yield! ReaderWriter.T_Trap_toString ( indent + 1 ) indentStep itr "Trap"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Trap data structure to configuration file.
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
    static member private T_Trap_toString ( indent : int ) ( indentStep : int ) ( elem : T_Trap ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield! ReaderWriter.T_Event_toString ( indent + 1 ) indentStep ( elem.Event ) "Event"
            yield! ReaderWriter.T_Action_toString ( indent + 1 ) indentStep ( elem.Action ) "Action"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Event data structure to configuration file.
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
    static member private T_Event_toString ( indent : int ) ( indentStep : int ) ( elem : T_Event ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_TestUnitReady( x ) ->
                yield sprintf "%s%s<TestUnitReady>0</TestUnitReady>" singleIndent indentStr
            | U_ReadCapacity( x ) ->
                yield sprintf "%s%s<ReadCapacity>0</ReadCapacity>" singleIndent indentStr
            | U_Read( x ) ->
                yield! ReaderWriter.T_Read_toString ( indent + 1 ) indentStep ( x ) "Read"
            | U_Write( x ) ->
                yield! ReaderWriter.T_Write_toString ( indent + 1 ) indentStep ( x ) "Write"
            | U_Format( x ) ->
                yield sprintf "%s%s<Format>0</Format>" singleIndent indentStr
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Read data structure to configuration file.
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
    static member private T_Read_toString ( indent : int ) ( indentStep : int ) ( elem : T_Read ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<StartLBA>%d</StartLBA>" singleIndent indentStr (elem.StartLBA)
            yield sprintf "%s%s<EndLBA>%d</EndLBA>" singleIndent indentStr (elem.EndLBA)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Write data structure to configuration file.
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
    static member private T_Write_toString ( indent : int ) ( indentStep : int ) ( elem : T_Write ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<StartLBA>%d</StartLBA>" singleIndent indentStr (elem.StartLBA)
            yield sprintf "%s%s<EndLBA>%d</EndLBA>" singleIndent indentStr (elem.EndLBA)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Action data structure to configuration file.
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
    static member private T_Action_toString ( indent : int ) ( indentStep : int ) ( elem : T_Action ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_ACA( x ) ->
                yield sprintf "%s%s<ACA>%s</ACA>" singleIndent indentStr ( ReaderWriter.xmlEncode(x) )
            | U_LUReset( x ) ->
                yield sprintf "%s%s<LUReset>%s</LUReset>" singleIndent indentStr ( ReaderWriter.xmlEncode(x) )
            | U_Count( x ) ->
                yield! ReaderWriter.T_Count_toString ( indent + 1 ) indentStep ( x ) "Count"
            | U_Delay( x ) ->
                yield sprintf "%s%s<Delay>%d</Delay>" singleIndent indentStr (x)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Count data structure to configuration file.
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
    static member private T_Count_toString ( indent : int ) ( indentStep : int ) ( elem : T_Count ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Index>%d</Index>" singleIndent indentStr (elem.Index)
            yield sprintf "%s%s<Value>%d</Value>" singleIndent indentStr (elem.Value)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_AddTrapResult data structure to configuration file.
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
    static member private T_AddTrapResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_AddTrapResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_ClearTrapsResult data structure to configuration file.
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
    static member private T_ClearTrapsResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_ClearTrapsResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }


