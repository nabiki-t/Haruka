//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

//=============================================================================
// Namespace declaration

namespace Haruka.IODataTypes.MediaCtrlReq

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

type [<NoComparison>]T_MediaCtrlReq = {
    Request : T_Request;
}

and [<NoComparison>]T_Request = 
    | U_Debug of T_Debug

and [<NoComparison>]T_Debug = 
    | U_GetAllTraps of unit
    | U_AddTrap of T_AddTrap
    | U_ClearTraps of unit
    | U_GetCounterValue of int
    | U_GetTaskWaitStatus of unit
    | U_Resume of T_Resume

and [<NoComparison>]T_AddTrap = {
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
    | U_Count of int
    | U_Delay of int
    | U_Wait of unit

and [<NoComparison>]T_Resume = {
    TSIH : TSIH_T;
    ITT : ITT_T;
}

//=============================================================================
// Class implementation

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='MediaCtrlReq' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='Request' >
        <xsd:complexType><xsd:choice>
          <xsd:element name='Debug' >
            <xsd:complexType><xsd:choice>
              <xsd:element name='GetAllTraps' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:int'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='0' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='AddTrap' >
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
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:int'>
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='Delay' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:int'>
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='Wait' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:int'>
                            <xsd:minInclusive value='0' />
                            <xsd:maxInclusive value='0' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                    </xsd:choice></xsd:complexType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
              <xsd:element name='ClearTraps' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:int'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='0' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='GetCounterValue' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:int'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='GetTaskWaitStatus' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:int'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='0' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='Resume' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='TSIH' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:unsignedShort' />
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='ITT' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:unsignedInt' />
                    </xsd:simpleType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
            </xsd:choice></xsd:complexType>
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
    ///  Load MediaCtrlReq data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded MediaCtrlReq data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_MediaCtrlReq =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load MediaCtrlReq data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded MediaCtrlReq data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_MediaCtrlReq =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "MediaCtrlReq" |> xdoc.Element |> ReaderWriter.Read_T_MediaCtrlReq

    /// <summary>
    ///  Read T_MediaCtrlReq data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_MediaCtrlReq data structure.
    /// </returns>
    static member private Read_T_MediaCtrlReq ( elem : XElement ) : T_MediaCtrlReq = 
        {
            Request =
                ReaderWriter.Read_T_Request( elem.Element( XName.Get "Request" ) );
        }

    /// <summary>
    ///  Read T_Request data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Request data structure.
    /// </returns>
    static member private Read_T_Request ( elem : XElement ) : T_Request = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "Debug" ->
            U_Debug( ReaderWriter.Read_T_Debug firstChild )
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
        | "GetAllTraps" ->
            U_GetAllTraps( () )
        | "AddTrap" ->
            U_AddTrap( ReaderWriter.Read_T_AddTrap firstChild )
        | "ClearTraps" ->
            U_ClearTraps( () )
        | "GetCounterValue" ->
            U_GetCounterValue( Int32.Parse( firstChild.Value ) )
        | "GetTaskWaitStatus" ->
            U_GetTaskWaitStatus( () )
        | "Resume" ->
            U_Resume( ReaderWriter.Read_T_Resume firstChild )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_AddTrap data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_AddTrap data structure.
    /// </returns>
    static member private Read_T_AddTrap ( elem : XElement ) : T_AddTrap = 
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
            U_Count( Int32.Parse( firstChild.Value ) )
        | "Delay" ->
            U_Delay( Int32.Parse( firstChild.Value ) )
        | "Wait" ->
            U_Wait( () )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_Resume data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Resume data structure.
    /// </returns>
    static member private Read_T_Resume ( elem : XElement ) : T_Resume = 
        {
            TSIH =
                tsih_me.fromPrim( UInt16.Parse( elem.Element( XName.Get "TSIH" ).Value ) );
            ITT =
                itt_me.fromPrim( UInt32.Parse( elem.Element( XName.Get "ITT" ).Value ) );
        }

    /// <summary>
    ///  Write MediaCtrlReq data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_MediaCtrlReq ) : unit =
        let s = ReaderWriter.T_MediaCtrlReq_toString 0 2 d "MediaCtrlReq"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert MediaCtrlReq data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_MediaCtrlReq ) : string =
        ReaderWriter.T_MediaCtrlReq_toString 0 0 d "MediaCtrlReq"
        |> String.Concat

    /// <summary>
    ///  Write T_MediaCtrlReq data structure to configuration file.
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
    static member private T_MediaCtrlReq_toString ( indent : int ) ( indentStep : int ) ( elem : T_MediaCtrlReq ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield! ReaderWriter.T_Request_toString ( indent + 1 ) indentStep ( elem.Request ) "Request"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Request data structure to configuration file.
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
    static member private T_Request_toString ( indent : int ) ( indentStep : int ) ( elem : T_Request ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_Debug( x ) ->
                yield! ReaderWriter.T_Debug_toString ( indent + 1 ) indentStep ( x ) "Debug"
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
            | U_GetAllTraps( x ) ->
                yield sprintf "%s%s<GetAllTraps>0</GetAllTraps>" singleIndent indentStr
            | U_AddTrap( x ) ->
                yield! ReaderWriter.T_AddTrap_toString ( indent + 1 ) indentStep ( x ) "AddTrap"
            | U_ClearTraps( x ) ->
                yield sprintf "%s%s<ClearTraps>0</ClearTraps>" singleIndent indentStr
            | U_GetCounterValue( x ) ->
                yield sprintf "%s%s<GetCounterValue>%d</GetCounterValue>" singleIndent indentStr (x)
            | U_GetTaskWaitStatus( x ) ->
                yield sprintf "%s%s<GetTaskWaitStatus>0</GetTaskWaitStatus>" singleIndent indentStr
            | U_Resume( x ) ->
                yield! ReaderWriter.T_Resume_toString ( indent + 1 ) indentStep ( x ) "Resume"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_AddTrap data structure to configuration file.
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
    static member private T_AddTrap_toString ( indent : int ) ( indentStep : int ) ( elem : T_AddTrap ) ( elemName : string ) : seq<string> = 
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
                yield sprintf "%s%s<Count>%d</Count>" singleIndent indentStr (x)
            | U_Delay( x ) ->
                yield sprintf "%s%s<Delay>%d</Delay>" singleIndent indentStr (x)
            | U_Wait( x ) ->
                yield sprintf "%s%s<Wait>0</Wait>" singleIndent indentStr
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Resume data structure to configuration file.
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
    static member private T_Resume_toString ( indent : int ) ( indentStep : int ) ( elem : T_Resume ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TSIH>%d</TSIH>" singleIndent indentStr ( tsih_me.toPrim (elem.TSIH) )
            yield sprintf "%s%s<ITT>%d</ITT>" singleIndent indentStr ( itt_me.toPrim (elem.ITT) )
            yield sprintf "%s</%s>" indentStr elemName
        }


