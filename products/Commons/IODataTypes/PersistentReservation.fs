//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

//=============================================================================
// Namespace declaration

namespace Haruka.IODataTypes.PersistentReservation

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

type [<NoComparison>]T_PRInfo = {
    Type : PR_TYPE;
    Registration : T_Registration list;
}

and [<NoComparison>]T_Registration = {
    ITNexus : T_ITNexus;
    ReservationKey : RESVKEY_T;
    Holder : bool;
}

and [<NoComparison>]T_ITNexus = {
    InitiatorName : string;
    ISID : ISID_T;
    TargetName : string;
    TPGT : TPGT_T;
}

//=============================================================================
// Class implementation

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='PRInfo' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='Type' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
            <xsd:enumeration value='NO_RESERVATION' />
            <xsd:enumeration value='WRITE_EXCLUSIVE' />
            <xsd:enumeration value='EXCLUSIVE_ACCESS' />
            <xsd:enumeration value='WRITE_EXCLUSIVE_REGISTRANTS_ONLY' />
            <xsd:enumeration value='WRITE_EXCLUSIVE_ALL_REGISTRANTS' />
            <xsd:enumeration value='EXCLUSIVE_ACCESS_REGISTRANTS_ONLY' />
            <xsd:enumeration value='EXCLUSIVE_ACCESS_ALL_REGISTRANTS' />
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='Registration' minOccurs='0' maxOccurs='65535' >
        <xsd:complexType><xsd:sequence>
          <xsd:element name='ITNexus' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='InitiatorName' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^[\-\.\:a-z0-9]{1,223}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ISID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^0(x|X)[0-9a-fA-F]{12}$' />
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
              <xsd:element name='TPGT' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedShort'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='ReservationKey' >
            <xsd:simpleType><xsd:restriction base='xsd:unsignedLong' /></xsd:simpleType>
          </xsd:element>
          <xsd:element name='Holder' >
            <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
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
    ///  Load PRInfo data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded PRInfo data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_PRInfo =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load PRInfo data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded PRInfo data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_PRInfo =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "PRInfo" |> xdoc.Element |> ReaderWriter.Read_T_PRInfo

    /// <summary>
    ///  Read T_PRInfo data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_PRInfo data structure.
    /// </returns>
    static member private Read_T_PRInfo ( elem : XElement ) : T_PRInfo = 
        {
            Type =
                PR_TYPE.fromStringValue( elem.Element( XName.Get "Type" ).Value );
            Registration =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Registration" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_Registration itr )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_Registration data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Registration data structure.
    /// </returns>
    static member private Read_T_Registration ( elem : XElement ) : T_Registration = 
        {
            ITNexus =
                ReaderWriter.Read_T_ITNexus( elem.Element( XName.Get "ITNexus" ) );
            ReservationKey =
                resvkey_me.fromPrim( UInt64.Parse( elem.Element( XName.Get "ReservationKey" ).Value ) );
            Holder =
                Boolean.Parse( elem.Element( XName.Get "Holder" ).Value );
        }

    /// <summary>
    ///  Read T_ITNexus data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_ITNexus data structure.
    /// </returns>
    static member private Read_T_ITNexus ( elem : XElement ) : T_ITNexus = 
        {
            InitiatorName =
                ReaderWriter.Check223Length( elem.Element( XName.Get "InitiatorName" ).Value );
            ISID =
                isid_me.HexStringToISID( elem.Element( XName.Get "ISID" ).Value );
            TargetName =
                ReaderWriter.Check223Length( elem.Element( XName.Get "TargetName" ).Value );
            TPGT =
                tpgt_me.fromPrim( UInt16.Parse( elem.Element( XName.Get "TPGT" ).Value ) );
        }

    /// <summary>
    ///  Write PRInfo data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_PRInfo ) : unit =
        let s = ReaderWriter.T_PRInfo_toString 0 2 d "PRInfo"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert PRInfo data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_PRInfo ) : string =
        ReaderWriter.T_PRInfo_toString 0 0 d "PRInfo"
        |> String.Concat

    /// <summary>
    ///  Write T_PRInfo data structure to configuration file.
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
    static member private T_PRInfo_toString ( indent : int ) ( indentStep : int ) ( elem : T_PRInfo ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Type>%s</Type>" singleIndent indentStr ( PR_TYPE.toStringName (elem.Type) )
            if elem.Registration.Length < 0 || elem.Registration.Length > 65535 then 
                raise <| ConfRWException( "Element count restriction error. Registration" )
            for itr in elem.Registration do
                yield! ReaderWriter.T_Registration_toString ( indent + 1 ) indentStep itr "Registration"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Registration data structure to configuration file.
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
    static member private T_Registration_toString ( indent : int ) ( indentStep : int ) ( elem : T_Registration ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield! ReaderWriter.T_ITNexus_toString ( indent + 1 ) indentStep ( elem.ITNexus ) "ITNexus"
            yield sprintf "%s%s<ReservationKey>%d</ReservationKey>" singleIndent indentStr ( resvkey_me.toPrim (elem.ReservationKey) )
            yield sprintf "%s%s<Holder>%b</Holder>" singleIndent indentStr (elem.Holder)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_ITNexus data structure to configuration file.
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
    static member private T_ITNexus_toString ( indent : int ) ( indentStep : int ) ( elem : T_ITNexus ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if not( Regex.IsMatch( elem.InitiatorName, Constants.ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_STR ) ) then
                raise <| ConfRWException( "iSCSI name pattern restriction error. InitiatorName" )
            yield sprintf "%s%s<InitiatorName>%s</InitiatorName>" singleIndent indentStr (elem.InitiatorName) 
            yield sprintf "%s%s<ISID>%s</ISID>" singleIndent indentStr ( isid_me.toString (elem.ISID) )
            if not( Regex.IsMatch( elem.TargetName, Constants.ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_STR ) ) then
                raise <| ConfRWException( "iSCSI name pattern restriction error. TargetName" )
            yield sprintf "%s%s<TargetName>%s</TargetName>" singleIndent indentStr (elem.TargetName) 
            yield sprintf "%s%s<TPGT>%d</TPGT>" singleIndent indentStr ( tpgt_me.toPrim (elem.TPGT) )
            yield sprintf "%s</%s>" indentStr elemName
        }


