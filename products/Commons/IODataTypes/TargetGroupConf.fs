//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

namespace Haruka.IODataTypes.TargetGroupConf

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_TargetGroup = {
    TargetGroupID : TGID_T;
    TargetGroupName : string;
    EnabledAtStart : bool;
    Target : T_Target list;
    LogicalUnit : T_LogicalUnit list;
}

and [<NoComparison>]T_Target = {
    IdentNumber : TNODEIDX_T;
    TargetPortalGroupTag : TPGT_T;
    TargetName : string;
    TargetAlias : string;
    LUN : LUN_T list;
    Auth : T_Auth;
}

and [<NoComparison>]T_Auth = 
    | U_CHAP of T_CHAP
    | U_None of unit

and [<NoComparison>]T_CHAP = {
    InitiatorAuth : T_InitiatorAuth;
    TargetAuth : T_TargetAuth;
}

and [<NoComparison>]T_InitiatorAuth = {
    UserName : string;
    Password : string;
}

and [<NoComparison>]T_TargetAuth = {
    UserName : string;
    Password : string;
}

and [<NoComparison>]T_LogicalUnit = {
    LUN : LUN_T;
    LUName : string;
    WorkPath : string;
    LUDevice : T_DEVICE;
}

and [<NoComparison>]T_DEVICE = 
    | U_BlockDevice of T_BlockDevice
    | U_DummyDevice of unit

and [<NoComparison>]T_BlockDevice = {
    Peripheral : T_MEDIA;
}

and [<NoComparison>]T_MEDIA = 
    | U_PlainFile of T_PlainFile
    | U_MemBuffer of T_MemBuffer
    | U_DummyMedia of T_DummyMedia
    | U_DebugMedia of T_DebugMedia

and [<NoComparison>]T_PlainFile = {
    IdentNumber : MEDIAIDX_T;
    MediaName : string;
    FileName : string;
    MaxMultiplicity : uint32;
    QueueWaitTimeOut : int;
    WriteProtect : bool;
}

and [<NoComparison>]T_MemBuffer = {
    IdentNumber : MEDIAIDX_T;
    MediaName : string;
    BytesCount : uint64;
}

and [<NoComparison>]T_DummyMedia = {
    IdentNumber : MEDIAIDX_T;
    MediaName : string;
}

and [<NoComparison>]T_DebugMedia = {
    IdentNumber : MEDIAIDX_T;
    MediaName : string;
    Peripheral : T_MEDIA;
}

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='TargetGroup' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='TargetGroupID' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
            <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='TargetGroupName' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
            <xsd:minLength value='0' />
            <xsd:maxLength value='256' />
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='EnabledAtStart' minOccurs='0' maxOccurs='1' >
        <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
      </xsd:element>
      <xsd:element name='Target' minOccurs='1' maxOccurs='255' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='IdentNumber' >
            <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
          </xsd:element>
          <xsd:element name='TargetPortalGroupTag' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedShort'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='0' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='TargetName' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:pattern value='^[\-\.\:a-z0-9]{1,223}$' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='TargetAlias' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='0' />
                <xsd:maxLength value='256' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='LUN' minOccurs='1' maxOccurs='255' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedLong'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='255' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='Auth' >
            <xsd:complexType><xsd:choice>
              <xsd:element name='CHAP' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='InitiatorAuth' >
                    <xsd:complexType><xsd:sequence>
                      <xsd:element name='UserName' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:string'>
                            <xsd:minLength value='1' />
                            <xsd:maxLength value='256' />
                            <xsd:pattern value='^[a-zA-Z0-9\.\-+@_/\[\]\:]+$' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='Password' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:string'>
                            <xsd:minLength value='1' />
                            <xsd:maxLength value='256' />
                            <xsd:pattern value='^[a-zA-Z0-9\.\-+@_/\[\]\:]+$' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                    </xsd:sequence></xsd:complexType>
                  </xsd:element>
                  <xsd:element name='TargetAuth' >
                    <xsd:complexType><xsd:sequence>
                      <xsd:element name='UserName' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:string'>
                            <xsd:minLength value='0' />
                            <xsd:maxLength value='256' />
                            <xsd:pattern value='^[a-zA-Z0-9\.\-+@_/\[\]\:]*$' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='Password' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:string'>
                            <xsd:minLength value='0' />
                            <xsd:maxLength value='256' />
                            <xsd:pattern value='^[a-zA-Z0-9\.\-+@_/\[\]\:]*$' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                    </xsd:sequence></xsd:complexType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
              <xsd:element name='None' >
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
      <xsd:element name='LogicalUnit' minOccurs='1' maxOccurs='255' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='LUN' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedLong'>
                <xsd:minInclusive value='0' />
                <xsd:maxInclusive value='255' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='LUName' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='0' />
                <xsd:maxLength value='256' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='LUDevice' type='DEVICE' ></xsd:element>
        </xsd:sequence></xsd:complexType>
      </xsd:element>
    </xsd:sequence></xsd:complexType>
  </xsd:element>
  <xsd:complexType name='DEVICE'>
    <xsd:choice>
      <xsd:element name='BlockDevice' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='Peripheral' type='MEDIA' ></xsd:element>
        </xsd:sequence></xsd:complexType>
      </xsd:element>
      <xsd:element name='DummyDevice' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:int'>
            <xsd:minInclusive value='0' />
            <xsd:maxInclusive value='0' />
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
    </xsd:choice>
  </xsd:complexType>
  <xsd:complexType name='MEDIA'>
    <xsd:choice>
      <xsd:element name='PlainFile' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='IdentNumber' >
            <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
          </xsd:element>
          <xsd:element name='MediaName' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='0' />
                <xsd:maxLength value='256' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='FileName' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='0' />
                <xsd:maxLength value='256' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='MaxMultiplicity' minOccurs='0' maxOccurs='1' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedInt'>
                <xsd:minInclusive value='1' />
                <xsd:maxInclusive value='32' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='QueueWaitTimeOut' minOccurs='0' maxOccurs='1' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:int'>
                <xsd:minInclusive value='50' />
                <xsd:maxInclusive value='3000000' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='WriteProtect' >
            <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
          </xsd:element>
        </xsd:sequence></xsd:complexType>
      </xsd:element>
      <xsd:element name='MemBuffer' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='IdentNumber' >
            <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
          </xsd:element>
          <xsd:element name='MediaName' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='0' />
                <xsd:maxLength value='256' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='BytesCount' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedLong'>
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
        </xsd:sequence></xsd:complexType>
      </xsd:element>
      <xsd:element name='DummyMedia' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='IdentNumber' >
            <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
          </xsd:element>
          <xsd:element name='MediaName' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='0' />
                <xsd:maxLength value='256' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
        </xsd:sequence></xsd:complexType>
      </xsd:element>
      <xsd:element name='DebugMedia' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='IdentNumber' >
            <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
          </xsd:element>
          <xsd:element name='MediaName' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='0' />
                <xsd:maxLength value='256' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='Peripheral' type='MEDIA' ></xsd:element>
        </xsd:sequence></xsd:complexType>
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
    ///  Load TargetGroup data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded TargetGroup data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_TargetGroup =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load TargetGroup data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded TargetGroup data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_TargetGroup =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "TargetGroup" |> xdoc.Element |> ReaderWriter.Read_T_TargetGroup

    /// <summary>
    ///  Read T_TargetGroup data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TargetGroup data structure.
    /// </returns>
    static member private Read_T_TargetGroup ( elem : XElement ) : T_TargetGroup = 
        {
            TargetGroupID =
                tgid_me.fromString( elem.Element( XName.Get "TargetGroupID" ).Value );
            TargetGroupName =
                elem.Element( XName.Get "TargetGroupName" ).Value;
            EnabledAtStart = 
                let subElem = elem.Element( XName.Get "EnabledAtStart" )
                if subElem = null then
                    true;
                else
                    Boolean.Parse( subElem.Value );
            Target =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Target" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_Target itr )
                |> Seq.toList
            LogicalUnit =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "LogicalUnit" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_LogicalUnit itr )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_Target data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Target data structure.
    /// </returns>
    static member private Read_T_Target ( elem : XElement ) : T_Target = 
        {
            IdentNumber =
                tnodeidx_me.fromPrim( UInt32.Parse( elem.Element( XName.Get "IdentNumber" ).Value ) );
            TargetPortalGroupTag =
                tpgt_me.fromPrim( UInt16.Parse( elem.Element( XName.Get "TargetPortalGroupTag" ).Value ) );
            TargetName =
                ReaderWriter.Check223Length( elem.Element( XName.Get "TargetName" ).Value );
            TargetAlias =
                elem.Element( XName.Get "TargetAlias" ).Value;
            LUN =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "LUN" )
                |> Seq.map ( fun itr -> lun_me.fromStringValue( itr.Value ) )
                |> Seq.toList
            Auth =
                ReaderWriter.Read_T_Auth( elem.Element( XName.Get "Auth" ) );
        }

    /// <summary>
    ///  Read T_Auth data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Auth data structure.
    /// </returns>
    static member private Read_T_Auth ( elem : XElement ) : T_Auth = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "CHAP" ->
            U_CHAP( ReaderWriter.Read_T_CHAP firstChild )
        | "None" ->
            U_None( () )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_CHAP data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_CHAP data structure.
    /// </returns>
    static member private Read_T_CHAP ( elem : XElement ) : T_CHAP = 
        {
            InitiatorAuth =
                ReaderWriter.Read_T_InitiatorAuth( elem.Element( XName.Get "InitiatorAuth" ) );
            TargetAuth =
                ReaderWriter.Read_T_TargetAuth( elem.Element( XName.Get "TargetAuth" ) );
        }

    /// <summary>
    ///  Read T_InitiatorAuth data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_InitiatorAuth data structure.
    /// </returns>
    static member private Read_T_InitiatorAuth ( elem : XElement ) : T_InitiatorAuth = 
        {
            UserName =
                elem.Element( XName.Get "UserName" ).Value;
            Password =
                elem.Element( XName.Get "Password" ).Value;
        }

    /// <summary>
    ///  Read T_TargetAuth data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TargetAuth data structure.
    /// </returns>
    static member private Read_T_TargetAuth ( elem : XElement ) : T_TargetAuth = 
        {
            UserName =
                elem.Element( XName.Get "UserName" ).Value;
            Password =
                elem.Element( XName.Get "Password" ).Value;
        }

    /// <summary>
    ///  Read T_LogicalUnit data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LogicalUnit data structure.
    /// </returns>
    static member private Read_T_LogicalUnit ( elem : XElement ) : T_LogicalUnit = 
        {
            LUN =
                lun_me.fromStringValue( elem.Element( XName.Get "LUN" ).Value );
            LUName =
                elem.Element( XName.Get "LUName" ).Value;
            WorkPath = "";
            LUDevice =
                ReaderWriter.Read_T_DEVICE( elem.Element( XName.Get "LUDevice" ) );
        }

    /// <summary>
    ///  Read T_DEVICE data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_DEVICE data structure.
    /// </returns>
    static member private Read_T_DEVICE ( elem : XElement ) : T_DEVICE = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "BlockDevice" ->
            U_BlockDevice( ReaderWriter.Read_T_BlockDevice firstChild )
        | "DummyDevice" ->
            U_DummyDevice( () )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_BlockDevice data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_BlockDevice data structure.
    /// </returns>
    static member private Read_T_BlockDevice ( elem : XElement ) : T_BlockDevice = 
        {
            Peripheral =
                ReaderWriter.Read_T_MEDIA( elem.Element( XName.Get "Peripheral" ) );
        }

    /// <summary>
    ///  Read T_MEDIA data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_MEDIA data structure.
    /// </returns>
    static member private Read_T_MEDIA ( elem : XElement ) : T_MEDIA = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "PlainFile" ->
            U_PlainFile( ReaderWriter.Read_T_PlainFile firstChild )
        | "MemBuffer" ->
            U_MemBuffer( ReaderWriter.Read_T_MemBuffer firstChild )
        | "DummyMedia" ->
            U_DummyMedia( ReaderWriter.Read_T_DummyMedia firstChild )
        | "DebugMedia" ->
            U_DebugMedia( ReaderWriter.Read_T_DebugMedia firstChild )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_PlainFile data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_PlainFile data structure.
    /// </returns>
    static member private Read_T_PlainFile ( elem : XElement ) : T_PlainFile = 
        {
            IdentNumber =
                mediaidx_me.fromPrim( UInt32.Parse( elem.Element( XName.Get "IdentNumber" ).Value ) );
            MediaName =
                elem.Element( XName.Get "MediaName" ).Value;
            FileName =
                elem.Element( XName.Get "FileName" ).Value;
            MaxMultiplicity = 
                let subElem = elem.Element( XName.Get "MaxMultiplicity" )
                if subElem = null then
                    10u;
                else
                    UInt32.Parse( subElem.Value );
            QueueWaitTimeOut = 
                let subElem = elem.Element( XName.Get "QueueWaitTimeOut" )
                if subElem = null then
                    10000;
                else
                    Int32.Parse( subElem.Value );
            WriteProtect =
                Boolean.Parse( elem.Element( XName.Get "WriteProtect" ).Value );
        }

    /// <summary>
    ///  Read T_MemBuffer data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_MemBuffer data structure.
    /// </returns>
    static member private Read_T_MemBuffer ( elem : XElement ) : T_MemBuffer = 
        {
            IdentNumber =
                mediaidx_me.fromPrim( UInt32.Parse( elem.Element( XName.Get "IdentNumber" ).Value ) );
            MediaName =
                elem.Element( XName.Get "MediaName" ).Value;
            BytesCount =
                UInt64.Parse( elem.Element( XName.Get "BytesCount" ).Value );
        }

    /// <summary>
    ///  Read T_DummyMedia data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_DummyMedia data structure.
    /// </returns>
    static member private Read_T_DummyMedia ( elem : XElement ) : T_DummyMedia = 
        {
            IdentNumber =
                mediaidx_me.fromPrim( UInt32.Parse( elem.Element( XName.Get "IdentNumber" ).Value ) );
            MediaName =
                elem.Element( XName.Get "MediaName" ).Value;
        }

    /// <summary>
    ///  Read T_DebugMedia data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_DebugMedia data structure.
    /// </returns>
    static member private Read_T_DebugMedia ( elem : XElement ) : T_DebugMedia = 
        {
            IdentNumber =
                mediaidx_me.fromPrim( UInt32.Parse( elem.Element( XName.Get "IdentNumber" ).Value ) );
            MediaName =
                elem.Element( XName.Get "MediaName" ).Value;
            Peripheral =
                ReaderWriter.Read_T_MEDIA( elem.Element( XName.Get "Peripheral" ) );
        }

    /// <summary>
    ///  Write TargetGroup data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_TargetGroup ) : unit =
        let s = ReaderWriter.T_TargetGroup_toString 0 2 d "TargetGroup"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert TargetGroup data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_TargetGroup ) : string =
        ReaderWriter.T_TargetGroup_toString 0 0 d "TargetGroup"
        |> String.Concat

    /// <summary>
    ///  Write T_TargetGroup data structure to configuration file.
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
    static member private T_TargetGroup_toString ( indent : int ) ( indentStep : int ) ( elem : T_TargetGroup ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetGroupID>%s</TargetGroupID>" singleIndent indentStr ( tgid_me.toString (elem.TargetGroupID) )
            if (elem.TargetGroupName).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. TargetGroupName" )
            if (elem.TargetGroupName).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. TargetGroupName" )
            yield sprintf "%s%s<TargetGroupName>%s</TargetGroupName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.TargetGroupName) )
            yield sprintf "%s%s<EnabledAtStart>%b</EnabledAtStart>" singleIndent indentStr (elem.EnabledAtStart)
            if elem.Target.Length < 1 || elem.Target.Length > 255 then 
                raise <| ConfRWException( "Element count restriction error. Target" )
            for itr in elem.Target do
                yield! ReaderWriter.T_Target_toString ( indent + 1 ) indentStep itr "Target"
            if elem.LogicalUnit.Length < 1 || elem.LogicalUnit.Length > 255 then 
                raise <| ConfRWException( "Element count restriction error. LogicalUnit" )
            for itr in elem.LogicalUnit do
                yield! ReaderWriter.T_LogicalUnit_toString ( indent + 1 ) indentStep itr "LogicalUnit"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Target data structure to configuration file.
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
    static member private T_Target_toString ( indent : int ) ( indentStep : int ) ( elem : T_Target ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<IdentNumber>%d</IdentNumber>" singleIndent indentStr ( tnodeidx_me.toPrim (elem.IdentNumber) )
            if (elem.TargetPortalGroupTag) < ( tpgt_me.fromPrim 0us ) then
                raise <| ConfRWException( "Min value(TPGT_T) restriction error. TargetPortalGroupTag" )
            if (elem.TargetPortalGroupTag) > ( tpgt_me.fromPrim 0us ) then
                raise <| ConfRWException( "Max value(TPGT_T) restriction error. TargetPortalGroupTag" )
            yield sprintf "%s%s<TargetPortalGroupTag>%d</TargetPortalGroupTag>" singleIndent indentStr ( tpgt_me.toPrim (elem.TargetPortalGroupTag) )
            if not( Regex.IsMatch( elem.TargetName, Constants.ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_STR ) ) then
                raise <| ConfRWException( "iSCSI name pattern restriction error. TargetName" )
            yield sprintf "%s%s<TargetName>%s</TargetName>" singleIndent indentStr (elem.TargetName) 
            if (elem.TargetAlias).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. TargetAlias" )
            if (elem.TargetAlias).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. TargetAlias" )
            yield sprintf "%s%s<TargetAlias>%s</TargetAlias>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.TargetAlias) )
            if elem.LUN.Length < 1 || elem.LUN.Length > 255 then 
                raise <| ConfRWException( "Element count restriction error. LUN" )
            for itr in elem.LUN do
                if lun_me.toPrim (itr) < 0UL then
                    raise <| ConfRWException( "Min value(LUN_T) restriction error. LUN" )
                if lun_me.toPrim (itr) > 255UL then
                    raise <| ConfRWException( "Max value(LUN_T) restriction error. LUN" )
                yield sprintf "%s%s<LUN>%s</LUN>" singleIndent indentStr ( lun_me.toString (itr) )
            yield! ReaderWriter.T_Auth_toString ( indent + 1 ) indentStep ( elem.Auth ) "Auth"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Auth data structure to configuration file.
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
    static member private T_Auth_toString ( indent : int ) ( indentStep : int ) ( elem : T_Auth ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_CHAP( x ) ->
                yield! ReaderWriter.T_CHAP_toString ( indent + 1 ) indentStep ( x ) "CHAP"
            | U_None( x ) ->
                yield sprintf "%s%s<None>0</None>" singleIndent indentStr
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_CHAP data structure to configuration file.
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
    static member private T_CHAP_toString ( indent : int ) ( indentStep : int ) ( elem : T_CHAP ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield! ReaderWriter.T_InitiatorAuth_toString ( indent + 1 ) indentStep ( elem.InitiatorAuth ) "InitiatorAuth"
            yield! ReaderWriter.T_TargetAuth_toString ( indent + 1 ) indentStep ( elem.TargetAuth ) "TargetAuth"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_InitiatorAuth data structure to configuration file.
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
    static member private T_InitiatorAuth_toString ( indent : int ) ( indentStep : int ) ( elem : T_InitiatorAuth ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if (elem.UserName).Length < 1 then
                raise <| ConfRWException( "Min value(string) restriction error. UserName" )
            if (elem.UserName).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. UserName" )
            if not( System.Text.RegularExpressions.Regex.IsMatch( elem.UserName, "^[a-zA-Z0-9\.\-+@_/\[\]\:]+$" ) ) then
                raise <| ConfRWException( "Pattern restriction error. UserName" )
            yield sprintf "%s%s<UserName>%s</UserName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.UserName) )
            if (elem.Password).Length < 1 then
                raise <| ConfRWException( "Min value(string) restriction error. Password" )
            if (elem.Password).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. Password" )
            if not( System.Text.RegularExpressions.Regex.IsMatch( elem.Password, "^[a-zA-Z0-9\.\-+@_/\[\]\:]+$" ) ) then
                raise <| ConfRWException( "Pattern restriction error. Password" )
            yield sprintf "%s%s<Password>%s</Password>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Password) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_TargetAuth data structure to configuration file.
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
    static member private T_TargetAuth_toString ( indent : int ) ( indentStep : int ) ( elem : T_TargetAuth ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if (elem.UserName).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. UserName" )
            if (elem.UserName).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. UserName" )
            if not( System.Text.RegularExpressions.Regex.IsMatch( elem.UserName, "^[a-zA-Z0-9\.\-+@_/\[\]\:]*$" ) ) then
                raise <| ConfRWException( "Pattern restriction error. UserName" )
            yield sprintf "%s%s<UserName>%s</UserName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.UserName) )
            if (elem.Password).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. Password" )
            if (elem.Password).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. Password" )
            if not( System.Text.RegularExpressions.Regex.IsMatch( elem.Password, "^[a-zA-Z0-9\.\-+@_/\[\]\:]*$" ) ) then
                raise <| ConfRWException( "Pattern restriction error. Password" )
            yield sprintf "%s%s<Password>%s</Password>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Password) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LogicalUnit data structure to configuration file.
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
    static member private T_LogicalUnit_toString ( indent : int ) ( indentStep : int ) ( elem : T_LogicalUnit ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if lun_me.toPrim (elem.LUN) < 0UL then
                raise <| ConfRWException( "Min value(LUN_T) restriction error. LUN" )
            if lun_me.toPrim (elem.LUN) > 255UL then
                raise <| ConfRWException( "Max value(LUN_T) restriction error. LUN" )
            yield sprintf "%s%s<LUN>%s</LUN>" singleIndent indentStr ( lun_me.toString (elem.LUN) )
            if (elem.LUName).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. LUName" )
            if (elem.LUName).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. LUName" )
            yield sprintf "%s%s<LUName>%s</LUName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.LUName) )
            yield! ReaderWriter.T_DEVICE_toString ( indent + 1 ) indentStep ( elem.LUDevice ) "LUDevice"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_DEVICE data structure to configuration file.
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
    static member private T_DEVICE_toString ( indent : int ) ( indentStep : int ) ( elem : T_DEVICE ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_BlockDevice( x ) ->
                yield! ReaderWriter.T_BlockDevice_toString ( indent + 1 ) indentStep ( x ) "BlockDevice"
            | U_DummyDevice( x ) ->
                yield sprintf "%s%s<DummyDevice>0</DummyDevice>" singleIndent indentStr
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_BlockDevice data structure to configuration file.
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
    static member private T_BlockDevice_toString ( indent : int ) ( indentStep : int ) ( elem : T_BlockDevice ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield! ReaderWriter.T_MEDIA_toString ( indent + 1 ) indentStep ( elem.Peripheral ) "Peripheral"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_MEDIA data structure to configuration file.
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
    static member private T_MEDIA_toString ( indent : int ) ( indentStep : int ) ( elem : T_MEDIA ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_PlainFile( x ) ->
                yield! ReaderWriter.T_PlainFile_toString ( indent + 1 ) indentStep ( x ) "PlainFile"
            | U_MemBuffer( x ) ->
                yield! ReaderWriter.T_MemBuffer_toString ( indent + 1 ) indentStep ( x ) "MemBuffer"
            | U_DummyMedia( x ) ->
                yield! ReaderWriter.T_DummyMedia_toString ( indent + 1 ) indentStep ( x ) "DummyMedia"
            | U_DebugMedia( x ) ->
                yield! ReaderWriter.T_DebugMedia_toString ( indent + 1 ) indentStep ( x ) "DebugMedia"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_PlainFile data structure to configuration file.
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
    static member private T_PlainFile_toString ( indent : int ) ( indentStep : int ) ( elem : T_PlainFile ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<IdentNumber>%d</IdentNumber>" singleIndent indentStr ( mediaidx_me.toPrim (elem.IdentNumber) )
            if (elem.MediaName).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. MediaName" )
            if (elem.MediaName).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. MediaName" )
            yield sprintf "%s%s<MediaName>%s</MediaName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.MediaName) )
            if (elem.FileName).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. FileName" )
            if (elem.FileName).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. FileName" )
            yield sprintf "%s%s<FileName>%s</FileName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.FileName) )
            if (elem.MaxMultiplicity) < 1u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. MaxMultiplicity" )
            if (elem.MaxMultiplicity) > 32u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. MaxMultiplicity" )
            yield sprintf "%s%s<MaxMultiplicity>%d</MaxMultiplicity>" singleIndent indentStr (elem.MaxMultiplicity)
            if (elem.QueueWaitTimeOut) < 50 then
                raise <| ConfRWException( "Min value(int) restriction error. QueueWaitTimeOut" )
            if (elem.QueueWaitTimeOut) > 3000000 then
                raise <| ConfRWException( "Max value(int) restriction error. QueueWaitTimeOut" )
            yield sprintf "%s%s<QueueWaitTimeOut>%d</QueueWaitTimeOut>" singleIndent indentStr (elem.QueueWaitTimeOut)
            yield sprintf "%s%s<WriteProtect>%b</WriteProtect>" singleIndent indentStr (elem.WriteProtect)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_MemBuffer data structure to configuration file.
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
    static member private T_MemBuffer_toString ( indent : int ) ( indentStep : int ) ( elem : T_MemBuffer ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<IdentNumber>%d</IdentNumber>" singleIndent indentStr ( mediaidx_me.toPrim (elem.IdentNumber) )
            if (elem.MediaName).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. MediaName" )
            if (elem.MediaName).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. MediaName" )
            yield sprintf "%s%s<MediaName>%s</MediaName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.MediaName) )
            yield sprintf "%s%s<BytesCount>%d</BytesCount>" singleIndent indentStr (elem.BytesCount)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_DummyMedia data structure to configuration file.
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
    static member private T_DummyMedia_toString ( indent : int ) ( indentStep : int ) ( elem : T_DummyMedia ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<IdentNumber>%d</IdentNumber>" singleIndent indentStr ( mediaidx_me.toPrim (elem.IdentNumber) )
            if (elem.MediaName).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. MediaName" )
            if (elem.MediaName).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. MediaName" )
            yield sprintf "%s%s<MediaName>%s</MediaName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.MediaName) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_DebugMedia data structure to configuration file.
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
    static member private T_DebugMedia_toString ( indent : int ) ( indentStep : int ) ( elem : T_DebugMedia ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<IdentNumber>%d</IdentNumber>" singleIndent indentStr ( mediaidx_me.toPrim (elem.IdentNumber) )
            if (elem.MediaName).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. MediaName" )
            if (elem.MediaName).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. MediaName" )
            yield sprintf "%s%s<MediaName>%s</MediaName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.MediaName) )
            yield! ReaderWriter.T_MEDIA_toString ( indent + 1 ) indentStep ( elem.Peripheral ) "Peripheral"
            yield sprintf "%s</%s>" indentStr elemName
        }


