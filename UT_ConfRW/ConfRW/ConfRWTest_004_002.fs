//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

namespace Haruka.Test.UT.ConfRW_004_002

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_Test = {
    C : T_C;
}

and [<NoComparison>]T_C = 
    | U_D_string_min of string
    | U_D_string_max of string
    | U_D_byte_min of sbyte
    | U_D_byte_max of sbyte
    | U_D_unsignedByte_min of uint8
    | U_D_unsignedByte_max of uint8
    | U_D_int_min of int
    | U_D_int_max of int
    | U_D_unsignedInt_min of uint32
    | U_D_unsignedInt_max of uint32
    | U_D_long_min of int64
    | U_D_long_max of int64
    | U_D_unsignedLong_min of uint64
    | U_D_unsignedLong_max of uint64
    | U_D_short_min of int16
    | U_D_short_max of int16
    | U_D_unsignedShort_min of uint16
    | U_D_unsignedShort_max of uint16
    | U_D_double_min of float
    | U_D_double_max of float
    | U_D_TPGT_T_min of TPGT_T
    | U_D_TPGT_T_max of TPGT_T

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='Test' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='C' >
        <xsd:complexType><xsd:choice>
          <xsd:element name='D_string_min' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_string_max' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:maxLength value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_byte_min' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:byte'>
                <xsd:minInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_byte_max' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:byte'>
                <xsd:maxInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_unsignedByte_min' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedByte'>
                <xsd:minInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_unsignedByte_max' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedByte'>
                <xsd:maxInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_int_min' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:int'>
                <xsd:minInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_int_max' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:int'>
                <xsd:maxInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_unsignedInt_min' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedInt'>
                <xsd:minInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_unsignedInt_max' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedInt'>
                <xsd:maxInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_long_min' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:long'>
                <xsd:minInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_long_max' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:long'>
                <xsd:maxInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_unsignedLong_min' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedLong'>
                <xsd:minInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_unsignedLong_max' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedLong'>
                <xsd:maxInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_short_min' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:short'>
                <xsd:minInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_short_max' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:short'>
                <xsd:maxInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_unsignedShort_min' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedShort'>
                <xsd:minInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_unsignedShort_max' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedShort'>
                <xsd:maxInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_double_min' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:double'>
                <xsd:minInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_double_max' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:double'>
                <xsd:maxInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_TPGT_T_min' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedShort'>
                <xsd:minInclusive value='16' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='D_TPGT_T_max' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:unsignedShort'>
                <xsd:maxInclusive value='16' />
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
            C =
                ReaderWriter.Read_T_C( elem.Element( XName.Get "C" ) );
        }

    /// <summary>
    ///  Read T_C data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_C data structure.
    /// </returns>
    static member private Read_T_C ( elem : XElement ) : T_C = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "D_string_min" ->
            U_D_string_min( firstChild.Value )
        | "D_string_max" ->
            U_D_string_max( firstChild.Value )
        | "D_byte_min" ->
            U_D_byte_min( SByte.Parse( firstChild.Value ) )
        | "D_byte_max" ->
            U_D_byte_max( SByte.Parse( firstChild.Value ) )
        | "D_unsignedByte_min" ->
            U_D_unsignedByte_min( Byte.Parse( firstChild.Value ) )
        | "D_unsignedByte_max" ->
            U_D_unsignedByte_max( Byte.Parse( firstChild.Value ) )
        | "D_int_min" ->
            U_D_int_min( Int32.Parse( firstChild.Value ) )
        | "D_int_max" ->
            U_D_int_max( Int32.Parse( firstChild.Value ) )
        | "D_unsignedInt_min" ->
            U_D_unsignedInt_min( UInt32.Parse( firstChild.Value ) )
        | "D_unsignedInt_max" ->
            U_D_unsignedInt_max( UInt32.Parse( firstChild.Value ) )
        | "D_long_min" ->
            U_D_long_min( Int64.Parse( firstChild.Value ) )
        | "D_long_max" ->
            U_D_long_max( Int64.Parse( firstChild.Value ) )
        | "D_unsignedLong_min" ->
            U_D_unsignedLong_min( UInt64.Parse( firstChild.Value ) )
        | "D_unsignedLong_max" ->
            U_D_unsignedLong_max( UInt64.Parse( firstChild.Value ) )
        | "D_short_min" ->
            U_D_short_min( Int16.Parse( firstChild.Value ) )
        | "D_short_max" ->
            U_D_short_max( Int16.Parse( firstChild.Value ) )
        | "D_unsignedShort_min" ->
            U_D_unsignedShort_min( UInt16.Parse( firstChild.Value ) )
        | "D_unsignedShort_max" ->
            U_D_unsignedShort_max( UInt16.Parse( firstChild.Value ) )
        | "D_double_min" ->
            U_D_double_min( Double.Parse( firstChild.Value ) )
        | "D_double_max" ->
            U_D_double_max( Double.Parse( firstChild.Value ) )
        | "D_TPGT_T_min" ->
            U_D_TPGT_T_min( tpgt_me.fromPrim( UInt16.Parse( firstChild.Value ) ) )
        | "D_TPGT_T_max" ->
            U_D_TPGT_T_max( tpgt_me.fromPrim( UInt16.Parse( firstChild.Value ) ) )
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
            yield! ReaderWriter.T_C_toString ( indent + 1 ) indentStep ( elem.C ) "C"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_C data structure to configuration file.
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
    static member private T_C_toString ( indent : int ) ( indentStep : int ) ( elem : T_C ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_D_string_min( x ) ->
                if (x).Length < 16 then
                    raise <| ConfRWException( "Min value(string) restriction error. D_string_min" )
                yield sprintf "%s%s<D_string_min>%s</D_string_min>" singleIndent indentStr ( ReaderWriter.xmlEncode(x) )
            | U_D_string_max( x ) ->
                if (x).Length > 16 then
                    raise <| ConfRWException( "Max value(string) restriction error. D_string_max" )
                yield sprintf "%s%s<D_string_max>%s</D_string_max>" singleIndent indentStr ( ReaderWriter.xmlEncode(x) )
            | U_D_byte_min( x ) ->
                if (x) < 16y then
                    raise <| ConfRWException( "Min value(byte) restriction error. D_byte_min" )
                yield sprintf "%s%s<D_byte_min>%d</D_byte_min>" singleIndent indentStr (x)
            | U_D_byte_max( x ) ->
                if (x) > 16y then
                    raise <| ConfRWException( "Max value(byte) restriction error. D_byte_max" )
                yield sprintf "%s%s<D_byte_max>%d</D_byte_max>" singleIndent indentStr (x)
            | U_D_unsignedByte_min( x ) ->
                if (x) < 16uy then
                    raise <| ConfRWException( "Min value(unsignedByte) restriction error. D_unsignedByte_min" )
                yield sprintf "%s%s<D_unsignedByte_min>%d</D_unsignedByte_min>" singleIndent indentStr (x)
            | U_D_unsignedByte_max( x ) ->
                if (x) > 16uy then
                    raise <| ConfRWException( "Max value(unsignedByte) restriction error. D_unsignedByte_max" )
                yield sprintf "%s%s<D_unsignedByte_max>%d</D_unsignedByte_max>" singleIndent indentStr (x)
            | U_D_int_min( x ) ->
                if (x) < 16 then
                    raise <| ConfRWException( "Min value(int) restriction error. D_int_min" )
                yield sprintf "%s%s<D_int_min>%d</D_int_min>" singleIndent indentStr (x)
            | U_D_int_max( x ) ->
                if (x) > 16 then
                    raise <| ConfRWException( "Max value(int) restriction error. D_int_max" )
                yield sprintf "%s%s<D_int_max>%d</D_int_max>" singleIndent indentStr (x)
            | U_D_unsignedInt_min( x ) ->
                if (x) < 16u then
                    raise <| ConfRWException( "Min value(unsignedInt) restriction error. D_unsignedInt_min" )
                yield sprintf "%s%s<D_unsignedInt_min>%d</D_unsignedInt_min>" singleIndent indentStr (x)
            | U_D_unsignedInt_max( x ) ->
                if (x) > 16u then
                    raise <| ConfRWException( "Max value(unsignedInt) restriction error. D_unsignedInt_max" )
                yield sprintf "%s%s<D_unsignedInt_max>%d</D_unsignedInt_max>" singleIndent indentStr (x)
            | U_D_long_min( x ) ->
                if (x) < 16L then
                    raise <| ConfRWException( "Min value(long) restriction error. D_long_min" )
                yield sprintf "%s%s<D_long_min>%d</D_long_min>" singleIndent indentStr (x)
            | U_D_long_max( x ) ->
                if (x) > 16L then
                    raise <| ConfRWException( "Max value(long) restriction error. D_long_max" )
                yield sprintf "%s%s<D_long_max>%d</D_long_max>" singleIndent indentStr (x)
            | U_D_unsignedLong_min( x ) ->
                if (x) < 16UL then
                    raise <| ConfRWException( "Min value(unsignedLong) restriction error. D_unsignedLong_min" )
                yield sprintf "%s%s<D_unsignedLong_min>%d</D_unsignedLong_min>" singleIndent indentStr (x)
            | U_D_unsignedLong_max( x ) ->
                if (x) > 16UL then
                    raise <| ConfRWException( "Max value(unsignedLong) restriction error. D_unsignedLong_max" )
                yield sprintf "%s%s<D_unsignedLong_max>%d</D_unsignedLong_max>" singleIndent indentStr (x)
            | U_D_short_min( x ) ->
                if (x) < 16s then
                    raise <| ConfRWException( "Min value(short) restriction error. D_short_min" )
                yield sprintf "%s%s<D_short_min>%d</D_short_min>" singleIndent indentStr (x)
            | U_D_short_max( x ) ->
                if (x) > 16s then
                    raise <| ConfRWException( "Max value(short) restriction error. D_short_max" )
                yield sprintf "%s%s<D_short_max>%d</D_short_max>" singleIndent indentStr (x)
            | U_D_unsignedShort_min( x ) ->
                if (x) < 16us then
                    raise <| ConfRWException( "Min value(unsignedShort) restriction error. D_unsignedShort_min" )
                yield sprintf "%s%s<D_unsignedShort_min>%d</D_unsignedShort_min>" singleIndent indentStr (x)
            | U_D_unsignedShort_max( x ) ->
                if (x) > 16us then
                    raise <| ConfRWException( "Max value(unsignedShort) restriction error. D_unsignedShort_max" )
                yield sprintf "%s%s<D_unsignedShort_max>%d</D_unsignedShort_max>" singleIndent indentStr (x)
            | U_D_double_min( x ) ->
                if (x) < 16 then
                    raise <| ConfRWException( "Min value(double) restriction error. D_double_min" )
                yield sprintf "%s%s<D_double_min>%f</D_double_min>" singleIndent indentStr (x)
            | U_D_double_max( x ) ->
                if (x) > 16 then
                    raise <| ConfRWException( "Max value(double) restriction error. D_double_max" )
                yield sprintf "%s%s<D_double_max>%f</D_double_max>" singleIndent indentStr (x)
            | U_D_TPGT_T_min( x ) ->
                if (x) < ( tpgt_me.fromPrim 16us ) then
                    raise <| ConfRWException( "Min value(TPGT_T) restriction error. D_TPGT_T_min" )
                yield sprintf "%s%s<D_TPGT_T_min>%d</D_TPGT_T_min>" singleIndent indentStr ( tpgt_me.toPrim (x) )
            | U_D_TPGT_T_max( x ) ->
                if (x) > ( tpgt_me.fromPrim 16us ) then
                    raise <| ConfRWException( "Max value(TPGT_T) restriction error. D_TPGT_T_max" )
                yield sprintf "%s%s<D_TPGT_T_max>%d</D_TPGT_T_max>" singleIndent indentStr ( tpgt_me.toPrim (x) )
            yield sprintf "%s</%s>" indentStr elemName
        }


