//=============================================================================
// Haruka Software Storage.
// GenConfRW.fs : Configuration XML document reader/writer program generator.
// 

open System
open System.IO
open System.Text
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open System.Reflection
open Haruka.Constants

/// Validator for the definition file of the XML document to be input and output.
let inputXSD = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='root'>
    <xsd:complexType>
      <xsd:sequence>
        <xsd:element name='form' type='T_Form' minOccurs='1' maxOccurs='1'/>
        <xsd:element name='typedef' type='T_Typedef' minOccurs='0' maxOccurs='65535'/>
      </xsd:sequence>
      <xsd:attribute name='namespace' type='xsd:string' use='required'/>
      <xsd:attribute name='class' type='xsd:string' use='required'/>
    </xsd:complexType>
  </xsd:element>
  <xsd:complexType name='T_Item'>
    <xsd:sequence>
      <xsd:element name='item' type='T_Item' minOccurs='0' maxOccurs='65535'/>
    </xsd:sequence>
    <xsd:attribute name='name' type='xsd:string' use='required'/>
    <xsd:attribute name='constraint' type='xsd:string'/>
    <xsd:attribute name='selection' type='xsd:boolean'/>
    <xsd:attribute name='mincount' type='xsd:string'/>
    <xsd:attribute name='maxcount' type='xsd:string'/>
    <xsd:attribute name='minvalue' type='xsd:string'/>
    <xsd:attribute name='maxvalue' type='xsd:string'/>
    <xsd:attribute name='pattern' type='xsd:string'/>
    <xsd:attribute name='hidden' type='xsd:boolean'/>
    <xsd:attribute name='default' type='xsd:string'/>
    <xsd:attribute name='defaultref' type='xsd:string'/>
  </xsd:complexType>
  <xsd:complexType name='T_Form'>
    <xsd:sequence>
      <xsd:element name='item' type='T_Item' minOccurs='1' maxOccurs='65535'/>
    </xsd:sequence>
    <xsd:attribute name='name' type='xsd:string' use='required'/>
  </xsd:complexType>
  <xsd:complexType name='T_Typedef'>
    <xsd:sequence>
      <xsd:element name='item' type='T_Item' minOccurs='1' maxOccurs='65535'/>
    </xsd:sequence>
    <xsd:attribute name='name' type='xsd:string' use='required'/>
    <xsd:attribute name='selection' type='xsd:boolean'/>
  </xsd:complexType>
</xsd:schema>"

// ----------------------------------------------------------------------------
// Output teble structure codes

/// <summary>
///  Get constant value from Haruka.Constants.Constants class member by specified value.
/// </summary>
/// <param name="strName">
///  string name.
/// </param>
/// <returns>
///  Constant value.
/// </returns>
let getConstantValue ( strName : string ) : string =
    Constants.constants_type_maker.GetRuntimeMethods()
    |> Seq.tryFind ( fun itr -> String.Equals( itr.Name, "get_" + strName, StringComparison.Ordinal ) )
    |>  function
        | None ->
            raise <| System.Exception( strName + " is not valid constant name." )
        | Some( i ) ->
            i.Invoke( null, Array.empty ).ToString()

/// <summary>
///  Get specified attribute value. If attribute is missing, returns None.
/// </summary>
/// <param name="elem">
///  XML element.
/// </param>
/// <param name="name">
///  attribute name.
/// </param>
/// <returns>
///  attribute value or None.
/// </returns>
let getAttributeStr ( elem : XElement ) ( name : string ) : string option =
    let e = elem.Attribute( XName.Get name )
    if e <> null then
        Some e.Value
    else
        None

/// <summary>
///  Get mincount attribute value.
///  If the above value is not specified, it is assumed that 1 is specified.
///  If that value is not uint value, it is assumed that constant value defined in Haruka.Constants.Constants class.
/// </summary>
/// <param name="elem">
///  XML element that will have mincount.
/// </param>
/// <returns>
///  mincount value.
/// </returns>
let getMinCountAttbValue ( elem : XElement ) : uint =
    match getAttributeStr elem "mincount" with
    | Some( x ) ->
        match System.UInt32.TryParse x with
        | true, x2 -> x2
        | false, _ ->
            x
            |> getConstantValue
            |> System.UInt32.Parse
    | None ->
        1u

/// <summary>
///  Get maxcount attribute value.
///  If the above value is not specified, it is assumed that 1 is specified.
///  If that value is not uint value, it is assumed that constant value defined in Haruka.Constants.Constants class.
/// </summary>
/// <param name="elem">
///  XML element that will have maxcount.
/// </param>
/// <returns>
///  maxcount value.
/// </returns>
let getMaxCountAttbValue ( elem : XElement ) : uint =
    match getAttributeStr elem "maxcount" with
    | Some( x ) ->
        match System.UInt32.TryParse x with
        | true, x2 -> x2
        | false, _ ->
            x
            |> getConstantValue
            |> System.UInt32.Parse
    | None ->
        1u

/// <summary>
///  Get minvalue attribute value.
///  If the above value is not specified, it returns None.
///  If that value is not uint value, it is assumed that constant value defined in Haruka.Constants.Constants class.
/// </summary>
/// <param name="elem">
///  XML element that will have minvalue.
/// </param>
/// <returns>
///  minvalue value.
/// </returns>
let getMinValueAttbValue ( elem : XElement ) : int64 option =
    match getAttributeStr elem "minvalue" with
    | Some( x ) ->
        match System.Int64.TryParse x with
        | true, x2 -> Some x2
        | false, _ ->
            x
            |> getConstantValue
            |> System.Int64.Parse
            |> Some
    | None ->
        None

/// <summary>
///  Get maxvalue attribute value.
///  If the above value is not specified, it is assumed that 1 is specified.
///  If that value is not uint value, it is assumed that constant value defined in Haruka.Constants.Constants class.
/// </summary>
/// <param name="elem">
///  XML element that will have maxvalue.
/// </param>
/// <returns>
///  maxvalue value.
/// </returns>
let getMaxValueAttbValue ( elem : XElement ) : int64 option =
    match getAttributeStr elem "maxvalue" with
    | Some( x ) ->
        match System.Int64.TryParse x with
        | true, x2 -> Some x2
        | false, _ ->
            x
            |> getConstantValue
            |> System.Int64.Parse
            |> Some
    | None ->
        None

/// <summary>
///  Get mincount and maxcount attribute value.
///  If the above value is not specified, it is assumed that 1 is specified.
/// </summary>
/// <param name="elem">
///  XML element that will have mincount and maxcount.
/// </param>
/// <returns>
///  pair of mincount and maxcount value.
/// </returns>
let getMinMaxCount ( elem : XElement ) : ( uint * uint ) =
    let minCount = getMinCountAttbValue elem
    let maxCount = getMaxCountAttbValue elem
    if minCount > maxCount then
        let name = match getAttributeStr elem "name" with | Some( x ) -> x | None -> ""
        raise <| System.Exception( "mincount or maxcount consistent error. Name=" + name )
    ( minCount, maxCount )

/// <summary>
///  Get primary type name.
/// </summary>
/// <param name="tName">
///  constraint attribute value.
/// </param>
let GetPrimeTypeName( tName : string ) : string =
    match tName.Trim() with
    | "string" -> "string"
    | "byte" -> "sbyte"
    | "unsignedByte" -> "uint8"
    | "int" -> "int"
    | "unsignedInt" -> "uint32"
    | "long" -> "int64"
    | "unsignedLong" -> "uint64"
    | "short" -> "int16"
    | "unsignedShort" -> "uint16"
    | "double" -> "float"
    | "boolean" -> "bool"
    | "NETPORTIDX_T" -> "NETPORTIDX_T"
    | "TNODEIDX_T" -> "TNODEIDX_T"
    | "MEDIAIDX_T" -> "MEDIAIDX_T"
    | "TPGT_T" -> "TPGT_T"
    | "LUN_T" -> "LUN_T"
    | "RESVKEY_T" -> "RESVKEY_T"
    | "AuthMethodCandidateValue" -> "AuthMethodCandidateValue"
    | "PR_TYPE" -> "PR_TYPE"
    | "LogLevel" -> "LogLevel"
    | "iSCSIName" -> "string"
    | "GUID" -> "System.Guid"
    | "TargetDeviceID" -> "TDID_T"
    | "TargetGroupID" -> "TGID_T"
    | "CtrlSessionID" -> "CtrlSessionID"
    | "DateTime" -> "DateTime"
    | "TSIH_T" -> "TSIH_T"
    | "ISID" -> "ISID_T"
    | "CID_T" -> "CID_T"
    | "CONCNT_T" -> "CONCNT_T"
    | "unit" -> "unit"
    | "IPCondition" -> "IPCondition"
    | _ -> sprintf "T_%s" tName

/// <summary>
///  Generate code for type declarations.
/// </summary>
/// <param name="elem">
///  XML "item" node.
/// </param>
/// <param name="ignoreCount">
///  Ignore mincount and maxcount attribution value or not.
/// </param>
/// <returns>
///  Generated string.
/// </returns>
let convSimpleValueTypeName ( elem : XElement ) ( ignoreCount : bool ) : string =
    let subName = elem.Attribute( XName.Get "name" ).Value
    let minCount, maxCount = getMinMaxCount elem
    if elem.HasElements then
        // It is a record type or a selection type.
        if minCount = 1u && maxCount = 1u || ignoreCount then
            sprintf "T_%s" subName
        elif minCount = 0u && maxCount = 1u then
            sprintf "T_%s option" subName
        else
            sprintf "T_%s list" subName
    else
        let tName = elem.Attribute( XName.Get "constraint" ).Value
        let primeTypeName = GetPrimeTypeName tName
        if minCount = 1u && maxCount = 1u || ignoreCount then
            sprintf "%s" primeTypeName
        elif minCount = 0u && maxCount = 1u then
            let da = elem.Attribute( XName.Get "default" )
            let dar = elem.Attribute( XName.Get "defaultref" )
            if da <> null || dar <> null then
                sprintf "%s" primeTypeName
            else
                sprintf "%s option" primeTypeName
        else
            sprintf "%s list" primeTypeName

/// <summary>
///  Recursively generate type declarations for the specified node and all child nodes.
/// </summary>
/// <param name="isFirst">
///  elem is first node or not.
/// </param>
/// <param name="elem">
///  XML "item" node.
/// </param>
let rec convXMLtoSCode ( outfile : TextWriter ) ( isFirst : bool ) ( elem : XElement ) : unit =
    if elem.HasElements then
        let itemName = elem.Attribute( XName.Get "name" ).Value

        // It is a record type or a selection type.
        let attbSelection = elem.Attribute( XName.Get "selection" )
        if attbSelection <> null && String.Equals( attbSelection.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase ) then
            // selection type
            if isFirst then
                fprintfn outfile "type [<NoComparison>]T_%s = " itemName
            else
                fprintfn outfile "and [<NoComparison>]T_%s = " itemName
            for itr in elem.Elements() do
                let subName = itr.Attribute( XName.Get "name" ).Value
                fprintfn outfile "    | U_%s of %s" subName ( convSimpleValueTypeName itr true )
            fprintfn outfile ""
        else
            // record type
            if isFirst then
                fprintfn outfile "type [<NoComparison>]T_%s = {" itemName
            else
                fprintfn outfile "and [<NoComparison>]T_%s = {" itemName
            for itr in elem.Elements() do
                let subName = itr.Attribute( XName.Get "name" ).Value
                fprintfn outfile "    %s : %s;" subName ( convSimpleValueTypeName itr false )
            fprintfn outfile "}"
            fprintfn outfile ""
    else
        // Single value(Nothing to do)
        ()

    // Output type declarations for child nodes.
    for itr in elem.Elements() do
        convXMLtoSCode outfile false itr

// ----------------------------------------------------------------------------
// Output XSD codes

/// <summary>
///  Generate element tag string in XSD string.
/// </summary>
/// <param name="refType">
///  The type name when referring to other types. 
///  If you do not want to refer to another type, specify a zero-length string.
/// </param>
/// <param name="elem">
///   XML "item" node.
/// </param>
/// <param name="ignoreCount">
///   Ignore mincount and maxcount attribution value or not..
/// </param>
/// <returns>
///  Generated string.
/// </returns>
let GenElementTagStr ( refType : string ) ( elem : XElement ) ( ignoreCount : bool ) : string =
    let nameStr = elem.Attribute( XName.Get "name" ).Value
    let minCountAttbObj = "mincount" |> XName.Get |> elem.Attribute
    let maxCountAttbObj = "maxcount" |> XName.Get |> elem.Attribute
    let minCount = getMinCountAttbValue elem
    let maxCount = getMaxCountAttbValue elem

    sprintf
        "<xsd:element name='%s'%s%s%s >"
            nameStr
            (
                if refType.Length > 0 then
                    " type='" + refType + "'"
                else
                    ""
            )
            (
                if minCountAttbObj <> null && not ignoreCount then
                    sprintf " minOccurs='%d'" minCount
                else
                    ""
            )
            (
                if maxCountAttbObj <> null && not ignoreCount then
                    sprintf " maxOccurs='%d'" maxCount
                else
                    ""
            )

/// <summary>
///  Generate XSD string for the specified node.
/// </summary>
/// <param name="elem">
///   XML "item" node.
/// </param>
/// <param name="param">
///   Indent count.
/// </param>
/// <returns>
///   Close tag string that must be appended after generating the XSD string for the child node.
/// </returns>
let OutputOwnNode ( outfile : TextWriter ) ( elem : XElement ) ( indent : int ) ( parentIsSelection : bool ) : ( string * bool ) =
    let indentStr = String.replicate indent " "
    let nameStr = elem.Attribute( XName.Get "name" ).Value

    // this element(=argument "elem") has child nodes, or not.
    if elem.HasElements then

        // If there are child nodes, output complexType tag.
        if elem.Name <> XName.Get "typedef" then
            let selectionAttb = elem.Attribute( XName.Get "selection" )
            if selectionAttb <> null && String.Equals( selectionAttb.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase ) then
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:complexType><xsd:choice>" indentStr
                ( indentStr + "  </xsd:choice></xsd:complexType>\r\n" + indentStr + "</xsd:element>\r\n", true )
            else
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:complexType><xsd:sequence>" indentStr
                ( indentStr + "  </xsd:sequence></xsd:complexType>\r\n" + indentStr + "</xsd:element>\r\n", false )
        else
            // If this node defines a type, it must not output element tag.
            fprintfn outfile "%s<xsd:complexType name='%s'>" indentStr nameStr
            let selectionAttb = elem.Attribute( XName.Get "selection" )
            let closeTag1 =
                if selectionAttb <> null && String.Equals( selectionAttb.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase ) then
                    fprintfn outfile "%s  <xsd:choice>" indentStr
                    indentStr + "  </xsd:choice>\r\n"
                else
                    fprintfn outfile "%s  <xsd:sequence>" indentStr
                    indentStr + "  </xsd:sequence>\r\n"
            ( closeTag1 + indentStr + "</xsd:complexType>\r\n", false )
    else
        let constraintStr = elem.Attribute( XName.Get "constraint" ).Value
        let hiddenStr = getAttributeStr elem "hidden"
        // If there are no child nodes, output simpleType tag.
        
        if hiddenStr.IsNone || ( String.Equals( hiddenStr.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase ) |> not ) then
            match constraintStr with
            | "string" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:%s'>" indentStr constraintStr
                let minValue = getMinValueAttbValue elem
                if minValue.IsSome then
                    fprintfn outfile "%s      <xsd:minLength value='%d' />" indentStr ( minValue.Value )
                let maxValue = getMaxValueAttbValue elem
                if maxValue.IsSome then
                    fprintfn outfile "%s      <xsd:maxLength value='%d' />" indentStr ( maxValue.Value )
                let patternValue = elem.Attribute( XName.Get "pattern" )
                if patternValue <> null then
                    fprintfn outfile "%s      <xsd:pattern value='%s' />" indentStr ( patternValue.Value )

                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "byte"
            | "unsignedByte"
            | "int"
            | "unsignedInt"
            | "long"
            | "unsignedLong"
            | "short"
            | "unsignedShort"
            | "double" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                let minValue = getMinValueAttbValue elem
                let maxValue = getMaxValueAttbValue elem
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:%s'>" indentStr constraintStr
                if minValue.IsSome then
                    fprintfn outfile "%s      <xsd:minInclusive value='%d' />" indentStr ( minValue.Value )
                if maxValue.IsSome then
                    fprintfn outfile "%s      <xsd:maxInclusive value='%d' />" indentStr ( maxValue.Value )
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "NETPORTIDX_T"
            | "TNODEIDX_T"
            | "MEDIAIDX_T" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "TPGT_T" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:unsignedShort'>" indentStr
                let minValue = getMinValueAttbValue elem
                if minValue.IsSome then
                    fprintfn outfile "%s      <xsd:minInclusive value='%d' />" indentStr ( minValue.Value )
                let maxValue = getMaxValueAttbValue elem
                if maxValue.IsSome then
                    fprintfn outfile "%s      <xsd:maxInclusive value='%d' />" indentStr ( maxValue.Value )
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr

                fprintfn outfile "%s</xsd:element>" indentStr

            | "LUN_T" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:unsignedLong'>" indentStr
                fprintfn outfile "%s      <xsd:minInclusive value='%d' />" indentStr ( Constants.MIN_LUN_VALUE )
                fprintfn outfile "%s      <xsd:maxInclusive value='%d' />" indentStr ( Constants.MAX_LUN_VALUE )
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "RESVKEY_T" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType><xsd:restriction base='xsd:unsignedLong' /></xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "AuthMethodCandidateValue" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:string'>" indentStr
                for itr in AuthMethodCandidateValue.Values do
                    fprintfn outfile "%s      <xsd:enumeration value='%s' />" indentStr ( AuthMethodCandidateValue.toStringName itr )
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "PR_TYPE" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:string'>" indentStr
                for itr in PR_TYPE.Values do
                    fprintfn outfile "%s      <xsd:enumeration value='%s' />" indentStr ( PR_TYPE.toStringName itr )
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "LogLevel" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:string'>" indentStr
                for itr in LogLevel.Values do
                    fprintfn outfile "%s      <xsd:enumeration value='%s' />" indentStr ( LogLevel.toString itr )
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "iSCSIName" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:string'>" indentStr
                fprintfn outfile "%s      <xsd:pattern value='%s' />" indentStr Constants.ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_STR
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "boolean" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType><xsd:restriction base='xsd:%s' /></xsd:simpleType>" indentStr constraintStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "GUID" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:string'>" indentStr
                fprintfn outfile "%s      <xsd:pattern value='^%s$' />" indentStr Constants.GUID_STRING_FORMAT_REGEX
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "TargetDeviceID" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:string'>" indentStr
                fprintfn outfile "%s      <xsd:pattern value='%s' />" indentStr Constants.TARGET_DEVICE_DIR_NAME_REGEX
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "TargetGroupID" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:string'>" indentStr
                fprintfn outfile "%s      <xsd:pattern value='%s' />" indentStr Constants.TARGET_GRP_CONFIG_FILE_NAME_REGEX
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "CtrlSessionID" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:string'>" indentStr
                fprintfn outfile "%s      <xsd:pattern value='^%s%s$' />" indentStr Constants.CTRL_SESS_ID_PREFIX Constants.GUID_STRING_FORMAT_REGEX
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "DateTime" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:long' >" indentStr
                fprintfn outfile "%s      <xsd:minInclusive value='%d' />" indentStr System.DateTime.MinValue.Ticks
                fprintfn outfile "%s      <xsd:maxInclusive value='%d' />" indentStr System.DateTime.MaxValue.Ticks
                fprintfn outfile "%s    </xsd:restriction>" indentStr

                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "CID_T"
            | "TSIH_T" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:unsignedShort' />" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "ISID" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:string'>" indentStr
                fprintfn outfile "%s      <xsd:pattern value='^0(x|X)[0-9a-fA-F]{12}$' />" indentStr
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "CONCNT_T" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:int' />" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "unit" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:int'>" indentStr
                fprintfn outfile "%s      <xsd:minInclusive value='0' />" indentStr
                fprintfn outfile "%s      <xsd:maxInclusive value='0' />" indentStr
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | "IPCondition" ->
                fprintfn outfile "%s%s" indentStr ( GenElementTagStr "" elem parentIsSelection )
                fprintfn outfile "%s  <xsd:simpleType>" indentStr
                fprintfn outfile "%s    <xsd:restriction base='xsd:string'>" indentStr
                fprintfn outfile "%s      <xsd:pattern value='^Any|Loopback|Linklocal|Private|Multicast|Global|IPv4Any|IPv4Loopback|IPv4Linklocal|IPv4Private|IPv4Multicast|IPv4Global|IPv6Any|IPv6Loopback|IPv6Linklocal|IPv6Private|IPv6Multicast|IPv6Global|IPFilter\( *[^ ,\)]{1,} *, *[^ ,\)]{1,} *\)$' />" indentStr
                fprintfn outfile "%s    </xsd:restriction>" indentStr
                fprintfn outfile "%s  </xsd:simpleType>" indentStr
                fprintfn outfile "%s</xsd:element>" indentStr

            | _ ->
                // If the constraint specifies a type name other than the default type,
                // it is a reference to a type defined separately.
                fprintfn outfile "%s%s</xsd:element>" indentStr ( GenElementTagStr constraintStr elem parentIsSelection )

        ( "", false )

/// <summary>
///  Recursively generate XSD string for the specified node and all child nodes.
/// </summary>
/// <param name="isroot">
///   If true, this node is the root node.
/// </param>
/// <param name="elem">
///   XML "item" node.
/// </param>
/// <param name="indent">
///   Indent count.
/// </param>
let rec OutputXSD ( outfile : TextWriter ) ( isroot : bool ) ( elem : XElement ) ( indent : int ) ( parentIsSelection : bool ) : unit =
    let closeTag, currentIsSelection =
        if isroot then
            // If specified node is the root node, output XML header string
            fprintfn outfile "<?xml version='1.0' encoding='UTF-8'?>"
            fprintfn outfile "<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>"
            "</xsd:schema>", false
        else
            // If specified node is not the root node, output XSD definition corresponding current node.
            OutputOwnNode outfile elem indent parentIsSelection

    for itr in elem.Elements() do
        OutputXSD outfile false itr ( indent + ( if isroot then 2 else 4 ) ) currentIsSelection

    fprintf outfile "%s" closeTag


// ----------------------------------------------------------------------------
// Output Read XML codes

/// <summary>
///  Generate parse method for single value at specified node.
/// </summary>
/// <param name="className">
///   Class name specified in the root node.
/// </param>
/// <param name="elemCallName">
///   XML "item" node name.
/// </param>
/// <param name="constraintStr">
///   Data type name.
/// </param>
/// <returns>
///   Generated string.
/// </returns>
let callReadFuncStr ( className : string ) ( elemCallName : string ) ( constraintStr : string ) : string =
    match constraintStr with
    | "string" ->
        sprintf "%s.Value" elemCallName
    | "byte" ->
        sprintf "SByte.Parse( %s.Value )" elemCallName
    | "unsignedByte" ->
        sprintf "Byte.Parse( %s.Value )" elemCallName
    | "int" ->
        sprintf "Int32.Parse( %s.Value )" elemCallName
    | "unsignedInt" ->
        sprintf "UInt32.Parse( %s.Value )" elemCallName
    | "long" ->
        sprintf "Int64.Parse( %s.Value )" elemCallName
    | "unsignedLong" ->
        sprintf "UInt64.Parse( %s.Value )" elemCallName
    | "short" ->
        sprintf "Int16.Parse( %s.Value )" elemCallName
    | "unsignedShort" ->
        sprintf "UInt16.Parse( %s.Value )" elemCallName
    | "double" ->
        sprintf "Double.Parse( %s.Value )" elemCallName
    | "boolean" ->
        sprintf "Boolean.Parse( %s.Value )" elemCallName
    | "NETPORTIDX_T" ->
        sprintf "netportidx_me.fromPrim( UInt32.Parse( %s.Value ) )" elemCallName
    | "TNODEIDX_T" ->
        sprintf "tnodeidx_me.fromPrim( UInt32.Parse( %s.Value ) )" elemCallName
    | "MEDIAIDX_T" ->
        sprintf "mediaidx_me.fromPrim( UInt32.Parse( %s.Value ) )" elemCallName
    | "TPGT_T" ->
        sprintf "tpgt_me.fromPrim( UInt16.Parse( %s.Value ) )" elemCallName
    | "LUN_T" ->
        sprintf "lun_me.fromStringValue( %s.Value )" elemCallName
    | "RESVKEY_T" ->
        sprintf "resvkey_me.fromPrim( UInt64.Parse( %s.Value ) )" elemCallName
    | "AuthMethodCandidateValue" ->
        sprintf "AuthMethodCandidateValue.fromStringValue( %s.Value )" elemCallName
    | "PR_TYPE" ->
        sprintf "PR_TYPE.fromStringValue( %s.Value )" elemCallName
    | "LogLevel" ->
        sprintf "LogLevel.fromString( %s.Value )" elemCallName
    | "iSCSIName" ->
        sprintf "%s.Check223Length( %s.Value )" className elemCallName
    | "GUID" ->
        sprintf "System.Guid.Parse( %s.Value )" elemCallName
    | "TargetDeviceID" ->
        sprintf "tdid_me.fromString( %s.Value )" elemCallName
    | "TargetGroupID" ->
        sprintf "tgid_me.fromString( %s.Value )" elemCallName
    | "CtrlSessionID" ->
        sprintf "new CtrlSessionID( %s.Value )" elemCallName
    | "DateTime" ->
        sprintf "DateTime.SpecifyKind( DateTime( Int64.Parse( %s.Value ) ), DateTimeKind.Utc )" elemCallName
    | "TSIH_T" ->
        sprintf "tsih_me.fromPrim( UInt16.Parse( %s.Value ) )" elemCallName
    | "ISID" ->
        sprintf "isid_me.HexStringToISID( %s.Value )" elemCallName
    | "CID_T" ->
        sprintf "cid_me.fromPrim( UInt16.Parse( %s.Value ) )" elemCallName
    | "CONCNT_T" ->
        sprintf "concnt_me.fromPrim( Int32.Parse( %s.Value ) )" elemCallName
    | "unit" ->
        sprintf "()"
    | "IPCondition" ->
        sprintf "IPCondition.Parse( %s.Value )" elemCallName
    | _ ->
        sprintf "%s.Read_T_%s( %s )" className constraintStr elemCallName

/// <summary>
///  Generate default value for single value at specified node.
/// </summary>
/// <param name="defValue">
///   Default value specified at attribute.
/// </param>
/// <param name="constraintStr">
///   Data type name.
/// </param>
/// <returns>
///   Generated string.
/// </returns>
let genSetDefaultValueStr ( defValue : string ) ( constraintStr : string ) : string =
    match constraintStr with
    | "string" ->
        sprintf "\"%s\"" defValue
    | "byte" ->
        sprintf "%sy" defValue
    | "unsignedByte" ->
        sprintf "%suy" defValue
    | "int" ->
        sprintf "%s" defValue
    | "unsignedInt" ->
        sprintf "%su" defValue
    | "long" ->
        sprintf "%sL" defValue
    | "unsignedLong" ->
        sprintf "%sUL" defValue
    | "short" ->
        sprintf "%ss" defValue
    | "unsignedShort" ->
        sprintf "%sus" defValue
    | "double" ->
        sprintf "%s" defValue
    | "boolean" ->
        if String.Equals( defValue.Trim(), "true", StringComparison.OrdinalIgnoreCase ) then
            "true"
        else
            "false"
    | "NETPORTIDX_T" ->
        sprintf "netportidx_me.fromPrim( %su )" defValue
    | "TNODEIDX_T" ->
        sprintf "tnodeidx_me.fromPrim( %su )" defValue
    | "MEDIAIDX_T" ->
        sprintf "mediaidx_me.fromPrim( %su )" defValue
    | "TPGT_T" ->
        sprintf "tpgt_me.fromPrim( %sus )" defValue
    | "LUN_T" ->
        sprintf "lun_me.fromPrim( %sUL )" defValue
    | "RESVKEY_T" ->
        sprintf "resvkey_me.fromPrim( %sUL )" defValue
    | "AuthMethodCandidateValue" ->
        sprintf "AuthMethodCandidateValue.fromStringValue( \"%s\" )" defValue
    | "PR_TYPE" ->
        sprintf "PR_TYPE.fromStringValue( \"%s\" )" defValue
    | "LogLevel" ->
        sprintf "LogLevel.fromString( \"%s\" )" defValue
    | "iSCSIName" ->
        sprintf "\"%s\"" defValue
    | "GUID" ->
        sprintf "System.Guid.Parse( \"%s\" )" defValue
    | "TargetDeviceID" ->
        sprintf "tdid_me.fromString( \"%s\" )" defValue
    | "TargetGroupID" ->
        sprintf "tgid_me.fromString( \"%s\" )" defValue
    | "CtrlSessionID" ->
        sprintf "new CtrlSessionID( \"%s\" )" defValue
    | "DateTime" ->
        sprintf "DateTime.SpecifyKind( DateTime.Parse( \"%s\" ), DateTimeKind.Utc )" defValue
    | "TSIH_T" ->
        sprintf "tsih_me.fromPrim( %sus )" defValue
    | "ISID" ->
        sprintf "isid_me.HexStringToISID( \"%s\" )" defValue
    | "CID_T" ->
        sprintf "cid_me.fromPrim( %sus )" defValue
    | "CONCNT_T" ->
        sprintf "concnt_me.fromPrim( %s )" defValue
    | "unit" ->
        sprintf "()"
    | "IPCondition" ->
        sprintf "IPCondition.Parse( \"%s\" )" defValue
    | _ ->
        raise <| new System.Exception( "Unknown type name. " + constraintStr )

/// <summary>
///  Generate default value for single value at specified node.
/// </summary>
/// <param name="constantName">
///   Constant name that is used to be default value.
/// </param>
/// <param name="constraintStr">
///   Data type name.
/// </param>
/// <returns>
///   Generated string.
/// </returns>
let genSetDefaultValueStr_RefConstant ( constantName : string ) ( constraintStr : string ) : string =
    let v = getConstantValue constantName
    genSetDefaultValueStr v constraintStr


/// <summary>
///  Generate default value for single value at specified node.
/// </summary>
/// <param name="constraintStr">
///   Data type name.
/// </param>
/// <returns>
///   Generated string.
/// </returns>
let genSetDefaultValueStr_Default ( constraintStr : string ) : string =
    match constraintStr with
    | "string" ->
        "\"\""
    | "byte" ->
        "0y"
    | "unsignedByte" ->
        "0uy"
    | "int" ->
        "0"
    | "unsignedInt" ->
        "0u"
    | "long" ->
        "0L"
    | "unsignedLong" ->
        "0UL"
    | "short" ->
        "0s"
    | "unsignedShort" ->
        "0us"
    | "double" ->
        "0.0"
    | "boolean" ->
        "false"
    | "NETPORTIDX_T" ->
        "netportidx_me.fromPrim( 0u )"
    | "TNODEIDX_T" ->
        "tnodeidx_me.fromPrim( 0u )"
    | "MEDIAIDX_T" ->
        "mediaidx_me.fromPrim( 0u )"
    | "TPGT_T" ->
        "tpgt_me.zero"
    | "LUN_T" ->
        "lun_me.zero"
    | "RESVKEY_T" ->
        "resvkey_me.zero"
    | "AuthMethodCandidateValue" ->
        "AuthMethodCandidateValue.AMC_None"
    | "PR_TYPE" ->
        "PR_TYPE.NO_RESERVATION"
    | "LogLevel" ->
        "LogLevel.LOGLEVEL_INFO"
    | "iSCSIName" ->
        "\"\""
    | "GUID" ->
        "System.Guid()"
    | "TargetDeviceID" ->
        "tdid_me.Zero"
    | "TargetGroupID" ->
        "tgid_me.Zero"
    | "CtrlSessionID" ->
        "CtrlSessionID.Zero"
    | "DateTime" ->
        "DateTime( 0L, DateTimeKind.Utc )"
    | "TSIH_T" ->
        "tsih_me.zero"
    | "ISID" ->
        "isid_me.zero"
    | "CID_T" ->
        "cid_me.zero"
    | "CONCNT_T" ->
        "concnt_me.zero"
    | "unit" ->
        "()"
    | "IPCondition" ->
        "IPCondition.Loopback"
    | _ ->
        raise <| new System.Exception( "Unknown type name. " + constraintStr )

/// <summary>
///  Recursively generate reader code for the specified node and all child nodes.
/// </summary>
/// <param name="className">
///   Class name specified in the root node.
/// </param>
/// <param name="elem">
///   XML "item" node.
/// </param>
let rec OutputReaderCode ( outfile : TextWriter ) ( className : string ) ( elem : XElement ) : unit =

    if elem.HasElements then
        // It is a record type or a selection type.
        let itemName = elem.Attribute( XName.Get "name" ).Value
        let attbSelection = elem.Attribute( XName.Get "selection" )

        fprintfn outfile "    /// <summary>"
        fprintfn outfile "    ///  Read T_%s data from XML document." itemName
        fprintfn outfile "    /// </summary>"
        fprintfn outfile "    /// <param name=\"elem\">"
        fprintfn outfile "    ///  Loaded XML document." 
        fprintfn outfile "    /// </param>"
        fprintfn outfile "    /// <returns>"
        fprintfn outfile "    ///  parsed T_%s data structure." itemName
        fprintfn outfile "    /// </returns>"
        fprintfn outfile "    static member private Read_T_%s ( elem : XElement ) : T_%s = " itemName itemName

        if attbSelection <> null && String.Equals( attbSelection.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase ) then
            // selection type
            fprintfn outfile "        let firstChild = elem.Elements() |> Seq.head "
            fprintfn outfile "        let firstChildName = firstChild.Name"
            fprintfn outfile "        match firstChildName.LocalName with"
            for itr in elem.Elements() do
                let subName = itr.Attribute( XName.Get "name" ).Value
                if not itr.HasElements then
                    // chile node has single value.
                    let constraintStr = itr.Attribute( XName.Get "constraint" ).Value
                    fprintfn outfile "        | \"%s\" ->" subName
                    fprintfn outfile "            U_%s( %s )" subName ( callReadFuncStr className "firstChild" constraintStr )
                else
                    // child node is a record type or a selection type.
                    fprintfn outfile "        | \"%s\" ->" subName
                    fprintfn outfile "            U_%s( %s.Read_T_%s firstChild )" subName className subName
            fprintfn outfile "        | _ -> raise <| ConfRWException( \"Unexpected tag name.\" )"
            fprintfn outfile ""
        else
            // record type
            fprintfn outfile "        {"
            for itr in elem.Elements() do
                let subName = itr.Attribute( XName.Get "name" ).Value
                if not itr.HasElements then
                    // chile node has single value.
                    let constraintStr = itr.Attribute( XName.Get "constraint" ).Value
                    let hiddenStr = getAttributeStr itr "hidden"
                    let defaultStr = getAttributeStr itr "default"
                    let defaultRefStr = getAttributeStr itr "defaultref"
                    let minCount, maxCount = getMinMaxCount itr

                    if hiddenStr.IsNone || ( String.Equals( hiddenStr.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase ) |> not ) then
                        if minCount = 1u && maxCount = 1u then
                            fprintfn outfile "            %s =" subName
                            fprintfn outfile "                %s;" ( callReadFuncStr className ( sprintf "elem.Element( XName.Get \"%s\" )" subName ) constraintStr )
                        elif minCount = 0u && maxCount = 1u then
                            if defaultStr.IsSome then
                                fprintfn outfile "            %s = " subName
                                fprintfn outfile "                let subElem = elem.Element( XName.Get \"%s\" )" subName
                                fprintfn outfile "                if subElem = null then"
                                fprintfn outfile "                    %s;" ( genSetDefaultValueStr defaultStr.Value constraintStr )
                                fprintfn outfile "                else"
                                fprintfn outfile "                    %s;" ( callReadFuncStr className "subElem" constraintStr )
                            elif defaultRefStr.IsSome then
                                fprintfn outfile "            %s = " subName
                                fprintfn outfile "                let subElem = elem.Element( XName.Get \"%s\" )" subName
                                fprintfn outfile "                if subElem = null then"
                                fprintfn outfile "                    %s;" ( genSetDefaultValueStr_RefConstant defaultRefStr.Value constraintStr )
                                fprintfn outfile "                else"
                                fprintfn outfile "                    %s;" ( callReadFuncStr className "subElem" constraintStr )
                            else
                                fprintfn outfile "            %s = " subName
                                fprintfn outfile "                let subElem = elem.Element( XName.Get \"%s\" )" subName
                                fprintfn outfile "                if subElem = null then"
                                fprintfn outfile "                    None"
                                fprintfn outfile "                else"
                                fprintfn outfile "                    Some( %s );" ( callReadFuncStr className "subElem" constraintStr )

                        else
                            fprintfn outfile "            %s =" subName
                            fprintfn outfile "                elem.Elements()"
                            fprintfn outfile "                |> Seq.filter ( fun itr -> itr.Name = XName.Get \"%s\" )" subName
                            fprintfn outfile "                |> Seq.map ( fun itr -> %s )" ( callReadFuncStr className "itr" constraintStr )
                            fprintfn outfile "                |> Seq.toList"
                    else
                        if defaultStr.IsSome then
                            fprintfn outfile "            %s = %s;" subName ( genSetDefaultValueStr defaultStr.Value constraintStr )
                        elif defaultRefStr.IsSome then
                            fprintfn outfile "            %s = %s;" subName ( genSetDefaultValueStr_RefConstant defaultRefStr.Value constraintStr )
                        else
                            fprintfn outfile "            %s = %s;" subName ( genSetDefaultValueStr_Default constraintStr )
                else
                    // child node is a record type or a selection type.
                    let minCount, maxCount = getMinMaxCount itr
                    if minCount = 1u && maxCount = 1u then
                        fprintfn outfile "            %s =" subName
                        fprintfn outfile "                %s.Read_T_%s( elem.Element( XName.Get \"%s\" ) );" className subName subName
                    elif minCount = 0u && maxCount = 1u then
                        fprintfn outfile "            %s = " subName
                        fprintfn outfile "                let subElem = elem.Element( XName.Get \"%s\" )" subName
                        fprintfn outfile "                if subElem = null then"
                        fprintfn outfile "                    None"
                        fprintfn outfile "                else"
                        fprintfn outfile "                    Some( %s.Read_T_%s subElem );" className subName
                    else
                        fprintfn outfile "            %s =" subName
                        fprintfn outfile "                elem.Elements()"
                        fprintfn outfile "                |> Seq.filter ( fun itr -> itr.Name = XName.Get \"%s\" )" subName
                        fprintfn outfile "                |> Seq.map ( fun itr -> %s.Read_T_%s itr )" className subName
                        fprintfn outfile "                |> Seq.toList"
            fprintfn outfile "        }"
            fprintfn outfile ""
    else
        // Single value.(Nothing to do)
        ()

    // Output reader code for all of child nodes.
    for itr in elem.Elements() do
        OutputReaderCode outfile className itr



// ----------------------------------------------------------------------------
// Output Write XML codes


/// <summary>
///  Generate converter code for single value at specified node.
/// </summary>
/// <param name="indent">
///   Indent level on the F# source code.
/// </param>
/// <param name="className">
///   Class name specified in the root node.
/// </param>
/// <param name="elemName">
///   XML "item" node name used to output XML tag.
/// </param>
/// <param name="elemCallName">
///   XML "item" node name used to reffer the value.
/// </param>
/// <param name="elem">
///   XML element.
/// </param>
/// <returns>
///   Generated string.
/// </returns>
let callWriteFuncStr ( outfile : TextWriter ) ( indent : int ) ( className : string ) ( elemName : string ) ( elemCallName : string ) ( elem : XElement ) : unit =
    let constraintStr = elem.Attribute( XName.Get "constraint" ).Value
    let indentStr = String.replicate indent " "
    match constraintStr with
    | "string" ->
        match getMinValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s).Length < %d then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Min value(string) restriction error. %s\" )" indentStr elemName
        | None -> ()
        match getMaxValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s).Length > %d then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Max value(string) restriction error. %s\" )" indentStr elemName
        | None -> ()
        match getAttributeStr elem "pattern" with
        | Some( x ) ->
            fprintfn outfile "%sif not( System.Text.RegularExpressions.Regex.IsMatch( %s, \"%s\" ) ) then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Pattern restriction error. %s\" )" indentStr elemName
        | None -> ()
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%s</%s>\" singleIndent indentStr ( %s.xmlEncode(%s) )" indentStr elemName elemName className elemCallName

    | "byte" ->
        match getMinValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) < %dy then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Min value(byte) restriction error. %s\" )" indentStr elemName
        | None -> ()
        match getMaxValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) > %dy then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Max value(byte) restriction error. %s\" )" indentStr elemName
        | None -> ()
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr (%s)" indentStr elemName elemName elemCallName

    | "unsignedByte" ->
        match getMinValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) < %duy then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Min value(unsignedByte) restriction error. %s\" )" indentStr elemName
        | None -> ()
        match getMaxValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) > %duy then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Max value(unsignedByte) restriction error. %s\" )" indentStr elemName
        | None -> ()
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr (%s)" indentStr elemName elemName elemCallName

    | "int" ->
        match getMinValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) < %d then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Min value(int) restriction error. %s\" )" indentStr elemName
        | None -> ()
        match getMaxValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) > %d then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Max value(int) restriction error. %s\" )" indentStr elemName
        | None -> ()
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr (%s)" indentStr elemName elemName elemCallName

    | "unsignedInt" ->
        match getMinValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) < %du then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Min value(unsignedInt) restriction error. %s\" )" indentStr elemName
        | None -> ()
        match getMaxValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) > %du then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Max value(unsignedInt) restriction error. %s\" )" indentStr elemName
        | None -> ()
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr (%s)" indentStr elemName elemName elemCallName

    | "long" ->
        match getMinValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) < %dL then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Min value(long) restriction error. %s\" )" indentStr elemName
        | None -> ()
        match getMaxValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) > %dL then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Max value(long) restriction error. %s\" )" indentStr elemName
        | None -> ()
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr (%s)" indentStr elemName elemName elemCallName

    | "unsignedLong" ->
        match getMinValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) < %dUL then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Min value(unsignedLong) restriction error. %s\" )" indentStr elemName
        | None -> ()
        match getMaxValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) > %dUL then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Max value(unsignedLong) restriction error. %s\" )" indentStr elemName
        | None -> ()
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr (%s)" indentStr elemName elemName elemCallName

    | "short" ->
        match getMinValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) < %ds then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Min value(short) restriction error. %s\" )" indentStr elemName
        | None -> ()
        match getMaxValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) > %ds then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Max value(short) restriction error. %s\" )" indentStr elemName
        | None -> ()
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr (%s)" indentStr elemName elemName elemCallName

    | "unsignedShort" ->
        match getMinValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) < %dus then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Min value(unsignedShort) restriction error. %s\" )" indentStr elemName
        | None -> ()
        match getMaxValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) > %dus then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Max value(unsignedShort) restriction error. %s\" )" indentStr elemName
        | None -> ()
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr (%s)" indentStr elemName elemName elemCallName

    | "double" ->
        match getMinValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) < %d then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Min value(double) restriction error. %s\" )" indentStr elemName
        | None -> ()
        match getMaxValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) > %d then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Max value(double) restriction error. %s\" )" indentStr elemName
        | None -> ()
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%f</%s>\" singleIndent indentStr (%s)" indentStr elemName elemName elemCallName

    | "boolean" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%b</%s>\" singleIndent indentStr (%s)" indentStr elemName elemName elemCallName

    | "NETPORTIDX_T" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr ( netportidx_me.toPrim (%s) )" indentStr elemName elemName elemCallName

    | "TNODEIDX_T" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr ( tnodeidx_me.toPrim (%s) )" indentStr elemName elemName elemCallName

    | "MEDIAIDX_T" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr ( mediaidx_me.toPrim (%s) )" indentStr elemName elemName elemCallName

    | "TPGT_T" ->
        match getMinValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) < ( tpgt_me.fromPrim %dus ) then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Min value(TPGT_T) restriction error. %s\" )" indentStr elemName
        | None -> ()
        match getMaxValueAttbValue elem with
        | Some( x ) ->
            fprintfn outfile "%sif (%s) > ( tpgt_me.fromPrim %dus ) then" indentStr elemCallName x
            fprintfn outfile "%s    raise <| ConfRWException( \"Max value(TPGT_T) restriction error. %s\" )" indentStr elemName
        | None -> ()
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr ( tpgt_me.toPrim (%s) )" indentStr elemName elemName elemCallName

    | "LUN_T" ->
        fprintfn outfile "%sif lun_me.toPrim (%s) < %dUL then" indentStr elemCallName Constants.MIN_LUN_VALUE
        fprintfn outfile "%s    raise <| ConfRWException( \"Min value(LUN_T) restriction error. %s\" )" indentStr elemName
        fprintfn outfile "%sif lun_me.toPrim (%s) > %dUL then" indentStr elemCallName Constants.MAX_LUN_VALUE
        fprintfn outfile "%s    raise <| ConfRWException( \"Max value(LUN_T) restriction error. %s\" )" indentStr elemName
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%s</%s>\" singleIndent indentStr ( lun_me.toString (%s) )" indentStr elemName elemName elemCallName

    | "RESVKEY_T" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr ( resvkey_me.toPrim (%s) )" indentStr elemName elemName elemCallName

    | "AuthMethodCandidateValue" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%s</%s>\" singleIndent indentStr ( AuthMethodCandidateValue.toStringName (%s) )" indentStr elemName elemName elemCallName

    | "PR_TYPE" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%s</%s>\" singleIndent indentStr ( PR_TYPE.toStringName (%s) )" indentStr elemName elemName elemCallName

    | "LogLevel" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%s</%s>\" singleIndent indentStr ( LogLevel.toString (%s) )" indentStr elemName elemName elemCallName

    | "iSCSIName" ->
        fprintfn outfile "%sif not( Regex.IsMatch( %s, Constants.ISCSI_TEXT_ISCSI_NAME_VALUE_REGEX_STR ) ) then" indentStr elemCallName
        fprintfn outfile "%s    raise <| ConfRWException( \"iSCSI name pattern restriction error. %s\" )" indentStr elemName
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%s</%s>\" singleIndent indentStr (%s) " indentStr elemName elemName elemCallName

    | "GUID" ->
        fprintfn outfile "%slet work = %s" indentStr elemCallName
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%s</%s>\" singleIndent indentStr (work.ToString()) " indentStr elemName elemName 

    | "TargetDeviceID" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%s</%s>\" singleIndent indentStr ( tdid_me.toString (%s) )" indentStr elemName elemName elemCallName

    | "TargetGroupID" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%s</%s>\" singleIndent indentStr ( tgid_me.toString (%s) )" indentStr elemName elemName elemCallName

    | "CtrlSessionID" ->
        fprintfn outfile "%slet work = %s" indentStr elemCallName
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%s</%s>\" singleIndent indentStr (work.ToString()) " indentStr elemName elemName 

    | "DateTime" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr ( (%s).Ticks )" indentStr elemName elemName elemCallName

    | "TSIH_T" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr ( tsih_me.toPrim (%s) )" indentStr elemName elemName elemCallName

    | "ISID" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%s</%s>\" singleIndent indentStr ( isid_me.toString (%s) )" indentStr elemName elemName elemCallName

    | "CID_T" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr ( cid_me.toPrim (%s) )" indentStr elemName elemName elemCallName

    | "CONCNT_T" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%d</%s>\" singleIndent indentStr ( concnt_me.toPrim (%s) )" indentStr elemName elemName elemCallName

    | "unit" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>0</%s>\" singleIndent indentStr" indentStr elemName elemName

    | "IPCondition" ->
        fprintfn outfile "%syield sprintf \"%%s%%s<%s>%%s</%s>\" singleIndent indentStr ( IPCondition.ToString(%s) )" indentStr elemName elemName elemCallName

    | _ ->
        fprintfn outfile "%syield! %s.T_%s_toString ( indent + 1 ) indentStep ( %s ) \"%s\"" indentStr className constraintStr elemCallName elemName

/// <summary>
///  Recursively generate converter code for the specified node and all child nodes.
/// </summary>
/// <param name="className">
///   Class name specified in the root node.
/// </param>
/// <param name="elem">
///   XML "item" node.
/// </param>
let rec OutputWriterCode ( outfile : TextWriter ) ( className : string ) ( elem : XElement ) : unit =

    if elem.HasElements then
        // It is a record type or a selection type.
        let itemName = elem.Attribute( XName.Get "name" ).Value
        fprintfn outfile "    /// <summary>"
        fprintfn outfile "    ///  Write T_%s data structure to configuration file." itemName
        fprintfn outfile "    /// </summary>"
        fprintfn outfile "    /// <param name=\"indent\">"
        fprintfn outfile "    ///  Indent space count." 
        fprintfn outfile "    /// </param>"
        fprintfn outfile "    /// <param name=\"indentStep\">"
        fprintfn outfile "    ///  Indent step count." 
        fprintfn outfile "    /// </param>"
        fprintfn outfile "    /// <param name=\"elem\">"
        fprintfn outfile "    ///  Data structure for output." 
        fprintfn outfile "    /// </param>"
        fprintfn outfile "    /// <param name=\"elemName\">"
        fprintfn outfile "    ///  XML tag name for the data." 
        fprintfn outfile "    /// </param>"
        fprintfn outfile "    /// <returns>"
        fprintfn outfile "    ///  Array of the generated string." 
        fprintfn outfile "    /// </returns>"
        fprintfn outfile "    static member private T_%s_toString ( indent : int ) ( indentStep : int ) ( elem : T_%s ) ( elemName : string ) : seq<string> = " itemName itemName
        fprintfn outfile "        let indentStr = String.replicate ( indent * indentStep ) \" \""
        fprintfn outfile "        let singleIndent = String.replicate ( indentStep ) \" \""
        fprintfn outfile "        seq {"
        fprintfn outfile "            yield sprintf \"%%s<%%s>\" indentStr elemName"
        let attbSelection = elem.Attribute( XName.Get "selection" )
        if attbSelection <> null && String.Equals( attbSelection.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase ) then
            // selection type
            fprintfn outfile "            match elem with"
            for itr in elem.Elements() do
                let subName = itr.Attribute( XName.Get "name" ).Value
                if not itr.HasElements then
                    // childe node has single value.
                    fprintfn outfile "            | U_%s( x ) ->" subName
                    callWriteFuncStr outfile 16 className subName "x" itr
                else
                    // childe node is a record type or a selection type.
                    fprintfn outfile "            | U_%s( x ) ->" subName
                    fprintfn outfile "                yield! %s.T_%s_toString ( indent + 1 ) indentStep ( x ) \"%s\"" className subName subName
        else
            // record type
            for itr in elem.Elements() do
                let subName = itr.Attribute( XName.Get "name" ).Value
                if not itr.HasElements then
                    // chile node has single value.
                    //let constraintStr = itr.Attribute( XName.Get "constraint" ).Value
                    let hiddenStr = getAttributeStr itr "hidden"
                    let defaultStr = getAttributeStr itr "default"
                    let defaultRefStr = getAttributeStr itr "defaultref"
                    let minCount, maxCount = getMinMaxCount itr

                    if hiddenStr.IsNone || ( String.Equals( hiddenStr.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase ) |> not ) then
                        if minCount = 1u && maxCount = 1u then
                            callWriteFuncStr outfile 12 className subName ( sprintf "elem.%s" subName ) itr
                        elif minCount = 0u && maxCount = 1u then
                            if defaultStr.IsNone && defaultRefStr.IsNone then
                                fprintfn outfile "            if elem.%s.IsSome then" subName
                                callWriteFuncStr outfile 16 className subName ( sprintf "elem.%s.Value" subName ) itr
                            else
                                callWriteFuncStr outfile 12 className subName ( sprintf "elem.%s" subName ) itr
                        else
                            fprintfn outfile "            if elem.%s.Length < %d || elem.%s.Length > %d then " subName minCount subName maxCount
                            fprintfn outfile "                raise <| ConfRWException( \"Element count restriction error. %s\" )" subName
                            fprintfn outfile "            for itr in elem.%s do" subName
                            callWriteFuncStr outfile 16 className subName "itr" itr

                else
                    // child node is a record type or a selection type.
                    let minCount, maxCount = getMinMaxCount itr
                    if minCount = 1u && maxCount = 1u then
                        fprintfn outfile "            yield! %s.T_%s_toString ( indent + 1 ) indentStep ( elem.%s ) \"%s\"" className subName subName subName
                    elif minCount = 0u && maxCount = 1u then
                        fprintfn outfile "            if elem.%s.IsSome then" subName
                        fprintfn outfile "                yield! %s.T_%s_toString ( indent + 1 ) indentStep ( elem.%s.Value ) \"%s\"" className subName subName subName
                    else
                        fprintfn outfile "            if elem.%s.Length < %d || elem.%s.Length > %d then " subName minCount subName maxCount
                        fprintfn outfile "                raise <| ConfRWException( \"Element count restriction error. %s\" )" subName
                        fprintfn outfile "            for itr in elem.%s do" subName
                        fprintfn outfile "                yield! %s.T_%s_toString ( indent + 1 ) indentStep itr \"%s\"" className subName subName
        fprintfn outfile "            yield sprintf \"%%s</%%s>\" indentStr elemName"
        fprintfn outfile "        }"
        fprintfn outfile ""

    else
        // Single value.(Nothing to do)
        ()

    // Output converter code for all of child nodes.
    for itr in elem.Elements() do
        OutputWriterCode outfile className itr


let Convert ( infile : TextReader ) ( outfile : TextWriter ) : int =
    let confSchemaSet =
        use xsdStream =
            let bt = Encoding.GetEncoding( "utf-8" ).GetBytes inputXSD
            let ms = new MemoryStream()
            ms.Write( bt, 0, bt.Length )
            ms.Seek( 0L, SeekOrigin.Begin ) |> ignore
            ms
        use xsdReader = XmlReader.Create xsdStream
        let wSS = new XmlSchemaSet ()
        wSS.Add( null, xsdReader ) |> ignore
        wSS
    let xdoc = XDocument.Load infile

    xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )

    let elemRoot = xdoc.Element( XName.Get "root" )
    let firstElem = elemRoot.Element( XName.Get "form" )
    let firstElemName = firstElem.Attribute( XName.Get "name"  ).Value

    // Output file header
    let namespaceStr = elemRoot.Attribute( XName.Get "namespace" ).Value
    let classStr = elemRoot.Attribute( XName.Get "class" ).Value
    fprintfn outfile "//============================================================================="
    fprintfn outfile "// Haruka Software Storage."
    fprintfn outfile "// Definition of %s configuration reader/writer function." classStr
    fprintfn outfile ""
    fprintfn outfile "namespace %s" namespaceStr
    fprintfn outfile ""
    fprintfn outfile "open System"
    fprintfn outfile "open System.IO"
    fprintfn outfile "open System.Text"
    fprintfn outfile "open System.Text.RegularExpressions"
    fprintfn outfile "open System.Xml"
    fprintfn outfile "open System.Xml.Schema"
    fprintfn outfile "open System.Xml.Linq"
    fprintfn outfile "open Haruka.Constants"
    fprintfn outfile ""

    // Output data type defifnition
    for itr in elemRoot.Elements() do
        convXMLtoSCode outfile ( itr = firstElem ) itr

    // Outpu class name
    fprintfn outfile "///  %s class imprements read and write function of configuration." classStr
    fprintfn outfile "type %s() =" classStr

    // Output XSD data
    fprintfn outfile ""
    fprintfn outfile "/// XSD data for validate input XML document."
    fprintf  outfile "    static let xsd = \""
    OutputXSD outfile true elemRoot 0 false
    fprintfn outfile "\""
    fprintfn outfile ""

    // Output Read method imprementation
    fprintfn outfile "    /// <summary>"
    fprintfn outfile "    ///  Get XmlSchemaSet for validate input XML document."
    fprintfn outfile "    /// </summary>"
    fprintfn outfile "    static let schemaSet ="
    fprintfn outfile "        lazy"
    fprintfn outfile "            use xsdStream = new MemoryStream( Encoding.GetEncoding( \"utf-8\" ).GetBytes xsd, false )"
    fprintfn outfile "            use xsdReader = XmlReader.Create xsdStream"
    fprintfn outfile "            let wSS = new XmlSchemaSet ()"
    fprintfn outfile "            wSS.Add( null, xsdReader ) |> ignore"
    fprintfn outfile "            xsdStream.Dispose()"
    fprintfn outfile "            xsdReader.Dispose()"
    fprintfn outfile "            wSS"
    fprintfn outfile ""
    fprintfn outfile "    /// <summary>"
    fprintfn outfile "    ///  Check iSCSI Name string length."
    fprintfn outfile "    /// </summary>"
    fprintfn outfile "    static member private Check223Length ( str : string ) : string ="
    fprintfn outfile "        let encStr = Encoding.GetEncoding( \"utf-8\" ).GetBytes( str )"
    fprintfn outfile "        if encStr.Length > Constants.ISCSI_TEXT_MAX_ISCSI_NAME_LENGTH then"
    fprintfn outfile "            raise( ConfRWException( \"iSCSI name too long.\" ) )"
    fprintfn outfile "        else"
    fprintfn outfile "            str"
    fprintfn outfile ""
    fprintfn outfile "    /// <summary>"
    fprintfn outfile "    ///  Encode string value for output XML data."
    fprintfn outfile "    /// </summary>"
    fprintfn outfile "    static member private xmlEncode : string -> string ="
    fprintfn outfile "        String.collect ("
    fprintfn outfile "            function"
    fprintfn outfile "            | '<' -> \"&lt;\""
    fprintfn outfile "            | '>' -> \"&gt;\""
    fprintfn outfile "            | '&' -> \"&amp;\""
    fprintfn outfile "            | '\\\"' -> \"&quot;\""
    fprintfn outfile "            | '\\\'' -> \"&apos;\""
    fprintfn outfile "            | '\\r' -> \"&#013;\""
    fprintfn outfile "            | '\\n' -> \"&#010;\""
    fprintfn outfile "            | _ as c -> c.ToString()"
    fprintfn outfile "        )"
    fprintfn outfile ""
    fprintfn outfile "    /// <summary>"
    fprintfn outfile "    ///  Load %s data from specified file." firstElemName
    fprintfn outfile "    /// </summary>"
    fprintfn outfile "    /// <param name=\"fname\">"
    fprintfn outfile "    ///  Configuration file name."
    fprintfn outfile "    /// </param>"
    fprintfn outfile "    /// <returns>"
    fprintfn outfile "    ///  Loaded %s data structures." firstElemName
    fprintfn outfile "    /// </returns>"
    fprintfn outfile "    /// <remarks>"
    fprintfn outfile "    ///  If it failed to load configuration, an exception will be raised."
    fprintfn outfile "    /// </remarks>"
    fprintfn outfile "    static member LoadFile ( fname : string ) : T_%s =" firstElemName
    fprintfn outfile "        fname |> File.ReadAllText |> %s.LoadString" classStr
    fprintfn outfile ""
    fprintfn outfile "    /// <summary>"
    fprintfn outfile "    ///  Load %s data from specified string." firstElemName
    fprintfn outfile "    /// </summary>"
    fprintfn outfile "    /// <param name=\"s\">"
    fprintfn outfile "    ///  XML string"
    fprintfn outfile "    /// </param>"
    fprintfn outfile "    /// <returns>"
    fprintfn outfile "    ///  Loaded %s data structures." firstElemName
    fprintfn outfile "    /// </returns>"
    fprintfn outfile "    /// <remarks>"
    fprintfn outfile "    ///  If it failed to load configuration, an exception will be raised."
    fprintfn outfile "    /// </remarks>"
    fprintfn outfile "    static member LoadString ( s : string ) : T_%s =" firstElemName
    fprintfn outfile "        let confSchemaSet = schemaSet.Value"
    fprintfn outfile "        let xdoc ="
    fprintfn outfile "            use ms = new MemoryStream( Encoding.GetEncoding( \"utf-8\" ).GetBytes s, false )"
    fprintfn outfile "            XDocument.Load ms"
    fprintfn outfile "        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )"
    fprintfn outfile "        \"%s\" |> xdoc.Element |> %s.Read_T_%s" firstElemName classStr firstElemName 
    fprintfn outfile ""
    

    for itr in elemRoot.Elements() do
        OutputReaderCode outfile classStr itr

    // Output Write method imprementation
    fprintfn outfile "    /// <summary>"
    fprintfn outfile "    ///  Write %s data to specified file." firstElemName
    fprintfn outfile "    /// </summary>"
    fprintfn outfile "    /// <param name=\"fname\">"
    fprintfn outfile "    ///  Configuration file name."
    fprintfn outfile "    /// </param>"
    fprintfn outfile "    /// <param name=\"d\">"
    fprintfn outfile "    ///  Data to output."
    fprintfn outfile "    /// </param>"
    fprintfn outfile "    /// <remarks>"
    fprintfn outfile "    ///  If it failed to write configuration, an exception will be raised."
    fprintfn outfile "    /// </remarks>"
    fprintfn outfile "    static member WriteFile ( fname : string ) ( d : T_%s ) : unit =" firstElemName
    fprintfn outfile "        let s = %s.T_%s_toString 0 2 d \"%s\"" classStr firstElemName firstElemName
    fprintfn outfile "        File.WriteAllLines( fname, s )"
    fprintfn outfile ""
    fprintfn outfile "    /// <summary>"
    fprintfn outfile "    ///  Convert %s data to string." firstElemName
    fprintfn outfile "    /// </summary>"
    fprintfn outfile "    /// <param name=\"d\">"
    fprintfn outfile "    ///  Data to output."
    fprintfn outfile "    /// </param>"
    fprintfn outfile "    /// <returns>"
    fprintfn outfile "    ///  Converted string"
    fprintfn outfile "    /// </returns>"
    fprintfn outfile "    static member ToString ( d : T_%s ) : string =" firstElemName
    fprintfn outfile "        %s.T_%s_toString 0 0 d \"%s\"" classStr firstElemName firstElemName
    fprintfn outfile "        |> String.Concat"
    fprintfn outfile ""
    
    for itr in elemRoot.Elements() do
        OutputWriterCode outfile classStr itr
    fprintfn outfile ""

    0

let ConvertFiles ( inDir : string ) ( outDir : string ) : int =
    let c = Path.DirectorySeparatorChar
    let rep = new RegularExpressions.Regex( sprintf @"^(.*)\%c([^\%c][^\%c]*)\.xml$" c c c )
    Directory.GetFiles inDir
    |> Array.map rep.Match
    |> Array.filter _.Success
    |> Array.map ( fun itr -> ( itr.Groups.[1].Value, itr.Groups.[2].Value ) )
    |> Array.iter ( fun ( path, body ) ->
        let infname = sprintf "%s%c%s.xml" path c body
        let outfname = sprintf "%s%c%s.fs" outDir c body
        let cr =
            if File.Exists outfname then
                let xmlTime = File.GetLastWriteTimeUtc infname
                let fsTime =  File.GetLastWriteTimeUtc outfname
                fsTime.CompareTo xmlTime < 0
            else
                true
        if cr then
            use infile = new StreamReader( File.OpenRead( infname ) )
            use outfile = new StreamWriter( new FileStream( outfname, FileMode.Create, FileAccess.Write, FileShare.None ) )
            Convert infile outfile |> ignore
    )
    0

/// entry point.
[<EntryPoint>]
let main : ( string[] -> int  )=
    function
    | [||] ->
        Convert stdin stdout
    | [| x |] ->
        ConvertFiles x x
    | [| x; y; |] ->
        ConvertFiles x y
    | _ ->
        printf "Instructions :"
        printf "    GenConfRW [ InputPath [ OutputPath ] ]"
        0
