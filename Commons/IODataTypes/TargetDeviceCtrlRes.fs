//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

namespace Haruka.IODataTypes.TargetDeviceCtrlRes

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_TargetDeviceCtrlRes = {
    Response : T_Response;
}

and [<NoComparison>]T_Response = 
    | U_ActiveTargetGroups of T_ActiveTargetGroups
    | U_LoadedTargetGroups of T_LoadedTargetGroups
    | U_InactivateTargetGroupResult of T_InactivateTargetGroupResult
    | U_ActivateTargetGroupResult of T_ActivateTargetGroupResult
    | U_UnloadTargetGroupResult of T_UnloadTargetGroupResult
    | U_LoadTargetGroupResult of T_LoadTargetGroupResult
    | U_SetLogParametersResult of bool
    | U_LogParameters of T_LogParameters
    | U_DeviceName of string
    | U_SessionList of T_SessionList
    | U_DestructSessionResult of T_DestructSessionResult
    | U_ConnectionList of T_ConnectionList
    | U_LUStatus of T_LUStatus
    | U_LUResetResult of T_LUResetResult
    | U_MediaStatus of T_MediaStatus
    | U_MediaControlResponse of T_MediaControlResponse
    | U_UnexpectedError of string

and [<NoComparison>]T_ActiveTargetGroups = {
    ActiveTGInfo : T_ActiveTGInfo list;
}

and [<NoComparison>]T_ActiveTGInfo = {
    ID : TGID_T;
    Name : string;
}

and [<NoComparison>]T_LoadedTargetGroups = {
    LoadedTGInfo : T_LoadedTGInfo list;
}

and [<NoComparison>]T_LoadedTGInfo = {
    ID : TGID_T;
    Name : string;
}

and [<NoComparison>]T_InactivateTargetGroupResult = {
    ID : TGID_T;
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_ActivateTargetGroupResult = {
    ID : TGID_T;
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_UnloadTargetGroupResult = {
    ID : TGID_T;
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_LoadTargetGroupResult = {
    ID : TGID_T;
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_LogParameters = {
    SoftLimit : uint32;
    HardLimit : uint32;
    LogLevel : LogLevel;
}

and [<NoComparison>]T_SessionList = {
    Session : T_Session list;
}

and [<NoComparison>]T_Session = {
    TargetGroupID : TGID_T;
    TargetNodeID : TNODEIDX_T;
    TSIH : TSIH_T;
    ITNexus : T_ITNEXUS;
    SessionParameters : T_SessionParameters;
    EstablishTime : DateTime;
}

and [<NoComparison>]T_SessionParameters = {
    MaxConnections : uint16;
    InitiatorAlias : string;
    InitialR2T : bool;
    ImmediateData : bool;
    MaxBurstLength : uint32;
    FirstBurstLength : uint32;
    DefaultTime2Wait : uint16;
    DefaultTime2Retain : uint16;
    MaxOutstandingR2T : uint16;
    DataPDUInOrder : bool;
    DataSequenceInOrder : bool;
    ErrorRecoveryLevel : uint8;
}

and [<NoComparison>]T_DestructSessionResult = {
    TSIH : TSIH_T;
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_ConnectionList = {
    Connection : T_Connection list;
}

and [<NoComparison>]T_Connection = {
    TSIH : TSIH_T;
    ConnectionID : CID_T;
    ConnectionCount : CONCNT_T;
    ReceiveBytesCount : T_RESCOUNTER list;
    SentBytesCount : T_RESCOUNTER list;
    ConnectionParameters : T_ConnectionParameters;
    EstablishTime : DateTime;
}

and [<NoComparison>]T_ConnectionParameters = {
    AuthMethod : string;
    HeaderDigest : string;
    DataDigest : string;
    MaxRecvDataSegmentLength_I : uint32;
    MaxRecvDataSegmentLength_T : uint32;
}

and [<NoComparison>]T_LUStatus = {
    LUN : LUN_T;
    ErrorMessage : string;
    LUStatus_Success : T_LUStatus_Success option;
}

and [<NoComparison>]T_LUStatus_Success = {
    ReadBytesCount : T_RESCOUNTER list;
    WrittenBytesCount : T_RESCOUNTER list;
    ReadTickCount : T_RESCOUNTER list;
    WriteTickCount : T_RESCOUNTER list;
    ACAStatus : T_ACAStatus option;
}

and [<NoComparison>]T_ACAStatus = {
    ITNexus : T_ITNEXUS;
    StatusCode : uint8;
    SenseKey : uint8;
    AdditionalSenseCode : uint16;
    IsCurrent : bool;
}

and [<NoComparison>]T_LUResetResult = {
    LUN : LUN_T;
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_MediaStatus = {
    LUN : LUN_T;
    ID : MEDIAIDX_T;
    ErrorMessage : string;
    MediaStatus_Success : T_MediaStatus_Success option;
}

and [<NoComparison>]T_MediaStatus_Success = {
    ReadBytesCount : T_RESCOUNTER list;
    WrittenBytesCount : T_RESCOUNTER list;
    ReadTickCount : T_RESCOUNTER list;
    WriteTickCount : T_RESCOUNTER list;
}

and [<NoComparison>]T_MediaControlResponse = {
    LUN : LUN_T;
    ID : MEDIAIDX_T;
    ErrorMessage : string;
    Response : string;
}

and [<NoComparison>]T_RESCOUNTER = {
    Time : DateTime;
    Value : int64;
    Count : int64;
}

and [<NoComparison>]T_ITNEXUS = {
    InitiatorName : string;
    ISID : ISID_T;
    TargetName : string;
    TPGT : TPGT_T;
}

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='TargetDeviceCtrlRes' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='Response' >
        <xsd:complexType><xsd:choice>
          <xsd:element name='ActiveTargetGroups' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='ActiveTGInfo' minOccurs='0' maxOccurs='255' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='ID' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:string'>
                        <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='Name' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:string'>
                        <xsd:minLength value='0' />
                        <xsd:maxLength value='256' />
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='LoadedTargetGroups' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='LoadedTGInfo' minOccurs='0' maxOccurs='255' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='ID' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:string'>
                        <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='Name' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:string'>
                        <xsd:minLength value='0' />
                        <xsd:maxLength value='256' />
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='InactivateTargetGroupResult' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='ID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
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
          <xsd:element name='ActivateTargetGroupResult' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='ID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
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
          <xsd:element name='UnloadTargetGroupResult' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='ID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
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
          <xsd:element name='LoadTargetGroupResult' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='ID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
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
          <xsd:element name='SetLogParametersResult' >
            <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
          </xsd:element>
          <xsd:element name='LogParameters' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SoftLimit' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedInt'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='10000000' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='HardLimit' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedInt'>
                    <xsd:minInclusive value='100' />
                    <xsd:maxInclusive value='20000000' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='LogLevel' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:enumeration value='VERBOSE' />
                    <xsd:enumeration value='INFO' />
                    <xsd:enumeration value='WARNING' />
                    <xsd:enumeration value='ERROR' />
                    <xsd:enumeration value='FAILED' />
                    <xsd:enumeration value='OFF' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='DeviceName' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:minLength value='0' />
                <xsd:maxLength value='512' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='SessionList' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='Session' minOccurs='0' maxOccurs='510' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='TargetGroupID' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:string'>
                        <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='TargetNodeID' >
                    <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='TSIH' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:unsignedShort' />
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='ITNexus' type='ITNEXUS' ></xsd:element>
                  <xsd:element name='SessionParameters' >
                    <xsd:complexType><xsd:sequence>
                      <xsd:element name='MaxConnections' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedShort'>
                            <xsd:minInclusive value='0' />
                            <xsd:maxInclusive value='16' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='InitiatorAlias' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:string'>
                            <xsd:minLength value='0' />
                            <xsd:maxLength value='256' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='InitialR2T' >
                        <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='ImmediateData' >
                        <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='MaxBurstLength' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedInt'>
                            <xsd:minInclusive value='512' />
                            <xsd:maxInclusive value='16777215' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='FirstBurstLength' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedInt'>
                            <xsd:minInclusive value='512' />
                            <xsd:maxInclusive value='16777215' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='DefaultTime2Wait' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedShort'>
                            <xsd:minInclusive value='0' />
                            <xsd:maxInclusive value='3600' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='DefaultTime2Retain' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedShort'>
                            <xsd:minInclusive value='0' />
                            <xsd:maxInclusive value='3600' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='MaxOutstandingR2T' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedShort'>
                            <xsd:minInclusive value='1' />
                            <xsd:maxInclusive value='65535' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='DataPDUInOrder' >
                        <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='DataSequenceInOrder' >
                        <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='ErrorRecoveryLevel' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedByte'>
                            <xsd:minInclusive value='0' />
                            <xsd:maxInclusive value='2' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                    </xsd:sequence></xsd:complexType>
                  </xsd:element>
                  <xsd:element name='EstablishTime' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:long' >
                        <xsd:minInclusive value='0' />
                        <xsd:maxInclusive value='3155378975999999999' />
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='DestructSessionResult' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='TSIH' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedShort' />
                </xsd:simpleType>
              </xsd:element>
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
          <xsd:element name='ConnectionList' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='Connection' minOccurs='0' maxOccurs='8160' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='TSIH' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:unsignedShort' />
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='ConnectionID' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:unsignedShort' />
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='ConnectionCount' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:int' />
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='ReceiveBytesCount' type='RESCOUNTER' minOccurs='0' maxOccurs='62' ></xsd:element>
                  <xsd:element name='SentBytesCount' type='RESCOUNTER' minOccurs='0' maxOccurs='62' ></xsd:element>
                  <xsd:element name='ConnectionParameters' >
                    <xsd:complexType><xsd:sequence>
                      <xsd:element name='AuthMethod' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:string'>
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='HeaderDigest' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:string'>
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='DataDigest' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:string'>
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='MaxRecvDataSegmentLength_I' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedInt'>
                            <xsd:minInclusive value='512' />
                            <xsd:maxInclusive value='16777215' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='MaxRecvDataSegmentLength_T' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedInt'>
                            <xsd:minInclusive value='512' />
                            <xsd:maxInclusive value='16777215' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                    </xsd:sequence></xsd:complexType>
                  </xsd:element>
                  <xsd:element name='EstablishTime' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:long' >
                        <xsd:minInclusive value='0' />
                        <xsd:maxInclusive value='3155378975999999999' />
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='LUStatus' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='LUN' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedLong'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='255' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='LUStatus_Success' minOccurs='0' maxOccurs='1' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='ReadBytesCount' type='RESCOUNTER' minOccurs='0' maxOccurs='62' ></xsd:element>
                  <xsd:element name='WrittenBytesCount' type='RESCOUNTER' minOccurs='0' maxOccurs='62' ></xsd:element>
                  <xsd:element name='ReadTickCount' type='RESCOUNTER' minOccurs='0' maxOccurs='62' ></xsd:element>
                  <xsd:element name='WriteTickCount' type='RESCOUNTER' minOccurs='0' maxOccurs='62' ></xsd:element>
                  <xsd:element name='ACAStatus' minOccurs='0' maxOccurs='1' >
                    <xsd:complexType><xsd:sequence>
                      <xsd:element name='ITNexus' type='ITNEXUS' ></xsd:element>
                      <xsd:element name='StatusCode' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedByte'>
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='SenseKey' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedByte'>
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='AdditionalSenseCode' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedShort'>
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='IsCurrent' >
                        <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
                      </xsd:element>
                    </xsd:sequence></xsd:complexType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='LUResetResult' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='LUN' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedLong'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='255' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
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
          <xsd:element name='MediaStatus' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='LUN' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedLong'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='255' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ID' >
                <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
              </xsd:element>
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='MediaStatus_Success' minOccurs='0' maxOccurs='1' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='ReadBytesCount' type='RESCOUNTER' minOccurs='0' maxOccurs='62' ></xsd:element>
                  <xsd:element name='WrittenBytesCount' type='RESCOUNTER' minOccurs='0' maxOccurs='62' ></xsd:element>
                  <xsd:element name='ReadTickCount' type='RESCOUNTER' minOccurs='0' maxOccurs='62' ></xsd:element>
                  <xsd:element name='WriteTickCount' type='RESCOUNTER' minOccurs='0' maxOccurs='62' ></xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='MediaControlResponse' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='LUN' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedLong'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='255' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ID' >
                <xsd:simpleType><xsd:restriction base='xsd:unsignedInt' /></xsd:simpleType>
              </xsd:element>
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='Response' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='UnexpectedError' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
        </xsd:choice></xsd:complexType>
      </xsd:element>
    </xsd:sequence></xsd:complexType>
  </xsd:element>
  <xsd:complexType name='RESCOUNTER'>
    <xsd:sequence>
      <xsd:element name='Time' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:long' >
            <xsd:minInclusive value='0' />
            <xsd:maxInclusive value='3155378975999999999' />
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='Value' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:long'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
      <xsd:element name='Count' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:long'>
          </xsd:restriction>
        </xsd:simpleType>
      </xsd:element>
    </xsd:sequence>
  </xsd:complexType>
  <xsd:complexType name='ITNEXUS'>
    <xsd:sequence>
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
    </xsd:sequence>
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
    ///  Load TargetDeviceCtrlRes data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded TargetDeviceCtrlRes data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_TargetDeviceCtrlRes =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load TargetDeviceCtrlRes data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded TargetDeviceCtrlRes data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_TargetDeviceCtrlRes =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "TargetDeviceCtrlRes" |> xdoc.Element |> ReaderWriter.Read_T_TargetDeviceCtrlRes

    /// <summary>
    ///  Read T_TargetDeviceCtrlRes data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TargetDeviceCtrlRes data structure.
    /// </returns>
    static member private Read_T_TargetDeviceCtrlRes ( elem : XElement ) : T_TargetDeviceCtrlRes = 
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
        | "ActiveTargetGroups" ->
            U_ActiveTargetGroups( ReaderWriter.Read_T_ActiveTargetGroups firstChild )
        | "LoadedTargetGroups" ->
            U_LoadedTargetGroups( ReaderWriter.Read_T_LoadedTargetGroups firstChild )
        | "InactivateTargetGroupResult" ->
            U_InactivateTargetGroupResult( ReaderWriter.Read_T_InactivateTargetGroupResult firstChild )
        | "ActivateTargetGroupResult" ->
            U_ActivateTargetGroupResult( ReaderWriter.Read_T_ActivateTargetGroupResult firstChild )
        | "UnloadTargetGroupResult" ->
            U_UnloadTargetGroupResult( ReaderWriter.Read_T_UnloadTargetGroupResult firstChild )
        | "LoadTargetGroupResult" ->
            U_LoadTargetGroupResult( ReaderWriter.Read_T_LoadTargetGroupResult firstChild )
        | "SetLogParametersResult" ->
            U_SetLogParametersResult( Boolean.Parse( firstChild.Value ) )
        | "LogParameters" ->
            U_LogParameters( ReaderWriter.Read_T_LogParameters firstChild )
        | "DeviceName" ->
            U_DeviceName( firstChild.Value )
        | "SessionList" ->
            U_SessionList( ReaderWriter.Read_T_SessionList firstChild )
        | "DestructSessionResult" ->
            U_DestructSessionResult( ReaderWriter.Read_T_DestructSessionResult firstChild )
        | "ConnectionList" ->
            U_ConnectionList( ReaderWriter.Read_T_ConnectionList firstChild )
        | "LUStatus" ->
            U_LUStatus( ReaderWriter.Read_T_LUStatus firstChild )
        | "LUResetResult" ->
            U_LUResetResult( ReaderWriter.Read_T_LUResetResult firstChild )
        | "MediaStatus" ->
            U_MediaStatus( ReaderWriter.Read_T_MediaStatus firstChild )
        | "MediaControlResponse" ->
            U_MediaControlResponse( ReaderWriter.Read_T_MediaControlResponse firstChild )
        | "UnexpectedError" ->
            U_UnexpectedError( firstChild.Value )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_ActiveTargetGroups data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_ActiveTargetGroups data structure.
    /// </returns>
    static member private Read_T_ActiveTargetGroups ( elem : XElement ) : T_ActiveTargetGroups = 
        {
            ActiveTGInfo =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "ActiveTGInfo" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_ActiveTGInfo itr )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_ActiveTGInfo data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_ActiveTGInfo data structure.
    /// </returns>
    static member private Read_T_ActiveTGInfo ( elem : XElement ) : T_ActiveTGInfo = 
        {
            ID =
                tgid_me.fromString( elem.Element( XName.Get "ID" ).Value );
            Name =
                elem.Element( XName.Get "Name" ).Value;
        }

    /// <summary>
    ///  Read T_LoadedTargetGroups data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LoadedTargetGroups data structure.
    /// </returns>
    static member private Read_T_LoadedTargetGroups ( elem : XElement ) : T_LoadedTargetGroups = 
        {
            LoadedTGInfo =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "LoadedTGInfo" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_LoadedTGInfo itr )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_LoadedTGInfo data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LoadedTGInfo data structure.
    /// </returns>
    static member private Read_T_LoadedTGInfo ( elem : XElement ) : T_LoadedTGInfo = 
        {
            ID =
                tgid_me.fromString( elem.Element( XName.Get "ID" ).Value );
            Name =
                elem.Element( XName.Get "Name" ).Value;
        }

    /// <summary>
    ///  Read T_InactivateTargetGroupResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_InactivateTargetGroupResult data structure.
    /// </returns>
    static member private Read_T_InactivateTargetGroupResult ( elem : XElement ) : T_InactivateTargetGroupResult = 
        {
            ID =
                tgid_me.fromString( elem.Element( XName.Get "ID" ).Value );
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_ActivateTargetGroupResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_ActivateTargetGroupResult data structure.
    /// </returns>
    static member private Read_T_ActivateTargetGroupResult ( elem : XElement ) : T_ActivateTargetGroupResult = 
        {
            ID =
                tgid_me.fromString( elem.Element( XName.Get "ID" ).Value );
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_UnloadTargetGroupResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_UnloadTargetGroupResult data structure.
    /// </returns>
    static member private Read_T_UnloadTargetGroupResult ( elem : XElement ) : T_UnloadTargetGroupResult = 
        {
            ID =
                tgid_me.fromString( elem.Element( XName.Get "ID" ).Value );
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_LoadTargetGroupResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LoadTargetGroupResult data structure.
    /// </returns>
    static member private Read_T_LoadTargetGroupResult ( elem : XElement ) : T_LoadTargetGroupResult = 
        {
            ID =
                tgid_me.fromString( elem.Element( XName.Get "ID" ).Value );
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_LogParameters data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LogParameters data structure.
    /// </returns>
    static member private Read_T_LogParameters ( elem : XElement ) : T_LogParameters = 
        {
            SoftLimit =
                UInt32.Parse( elem.Element( XName.Get "SoftLimit" ).Value );
            HardLimit =
                UInt32.Parse( elem.Element( XName.Get "HardLimit" ).Value );
            LogLevel =
                LogLevel.fromString( elem.Element( XName.Get "LogLevel" ).Value );
        }

    /// <summary>
    ///  Read T_SessionList data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_SessionList data structure.
    /// </returns>
    static member private Read_T_SessionList ( elem : XElement ) : T_SessionList = 
        {
            Session =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Session" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_Session itr )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_Session data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Session data structure.
    /// </returns>
    static member private Read_T_Session ( elem : XElement ) : T_Session = 
        {
            TargetGroupID =
                tgid_me.fromString( elem.Element( XName.Get "TargetGroupID" ).Value );
            TargetNodeID =
                tnodeidx_me.fromPrim( UInt32.Parse( elem.Element( XName.Get "TargetNodeID" ).Value ) );
            TSIH =
                tsih_me.fromPrim( UInt16.Parse( elem.Element( XName.Get "TSIH" ).Value ) );
            ITNexus =
                ReaderWriter.Read_T_ITNEXUS( elem.Element( XName.Get "ITNexus" ) );
            SessionParameters =
                ReaderWriter.Read_T_SessionParameters( elem.Element( XName.Get "SessionParameters" ) );
            EstablishTime =
                DateTime.SpecifyKind( DateTime( Int64.Parse( elem.Element( XName.Get "EstablishTime" ).Value ) ), DateTimeKind.Utc );
        }

    /// <summary>
    ///  Read T_SessionParameters data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_SessionParameters data structure.
    /// </returns>
    static member private Read_T_SessionParameters ( elem : XElement ) : T_SessionParameters = 
        {
            MaxConnections =
                UInt16.Parse( elem.Element( XName.Get "MaxConnections" ).Value );
            InitiatorAlias =
                elem.Element( XName.Get "InitiatorAlias" ).Value;
            InitialR2T =
                Boolean.Parse( elem.Element( XName.Get "InitialR2T" ).Value );
            ImmediateData =
                Boolean.Parse( elem.Element( XName.Get "ImmediateData" ).Value );
            MaxBurstLength =
                UInt32.Parse( elem.Element( XName.Get "MaxBurstLength" ).Value );
            FirstBurstLength =
                UInt32.Parse( elem.Element( XName.Get "FirstBurstLength" ).Value );
            DefaultTime2Wait =
                UInt16.Parse( elem.Element( XName.Get "DefaultTime2Wait" ).Value );
            DefaultTime2Retain =
                UInt16.Parse( elem.Element( XName.Get "DefaultTime2Retain" ).Value );
            MaxOutstandingR2T =
                UInt16.Parse( elem.Element( XName.Get "MaxOutstandingR2T" ).Value );
            DataPDUInOrder =
                Boolean.Parse( elem.Element( XName.Get "DataPDUInOrder" ).Value );
            DataSequenceInOrder =
                Boolean.Parse( elem.Element( XName.Get "DataSequenceInOrder" ).Value );
            ErrorRecoveryLevel =
                Byte.Parse( elem.Element( XName.Get "ErrorRecoveryLevel" ).Value );
        }

    /// <summary>
    ///  Read T_DestructSessionResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_DestructSessionResult data structure.
    /// </returns>
    static member private Read_T_DestructSessionResult ( elem : XElement ) : T_DestructSessionResult = 
        {
            TSIH =
                tsih_me.fromPrim( UInt16.Parse( elem.Element( XName.Get "TSIH" ).Value ) );
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_ConnectionList data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_ConnectionList data structure.
    /// </returns>
    static member private Read_T_ConnectionList ( elem : XElement ) : T_ConnectionList = 
        {
            Connection =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Connection" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_Connection itr )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_Connection data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Connection data structure.
    /// </returns>
    static member private Read_T_Connection ( elem : XElement ) : T_Connection = 
        {
            TSIH =
                tsih_me.fromPrim( UInt16.Parse( elem.Element( XName.Get "TSIH" ).Value ) );
            ConnectionID =
                cid_me.fromPrim( UInt16.Parse( elem.Element( XName.Get "ConnectionID" ).Value ) );
            ConnectionCount =
                concnt_me.fromPrim( Int32.Parse( elem.Element( XName.Get "ConnectionCount" ).Value ) );
            ReceiveBytesCount =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "ReceiveBytesCount" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_RESCOUNTER( itr ) )
                |> Seq.toList
            SentBytesCount =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "SentBytesCount" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_RESCOUNTER( itr ) )
                |> Seq.toList
            ConnectionParameters =
                ReaderWriter.Read_T_ConnectionParameters( elem.Element( XName.Get "ConnectionParameters" ) );
            EstablishTime =
                DateTime.SpecifyKind( DateTime( Int64.Parse( elem.Element( XName.Get "EstablishTime" ).Value ) ), DateTimeKind.Utc );
        }

    /// <summary>
    ///  Read T_ConnectionParameters data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_ConnectionParameters data structure.
    /// </returns>
    static member private Read_T_ConnectionParameters ( elem : XElement ) : T_ConnectionParameters = 
        {
            AuthMethod =
                elem.Element( XName.Get "AuthMethod" ).Value;
            HeaderDigest =
                elem.Element( XName.Get "HeaderDigest" ).Value;
            DataDigest =
                elem.Element( XName.Get "DataDigest" ).Value;
            MaxRecvDataSegmentLength_I =
                UInt32.Parse( elem.Element( XName.Get "MaxRecvDataSegmentLength_I" ).Value );
            MaxRecvDataSegmentLength_T =
                UInt32.Parse( elem.Element( XName.Get "MaxRecvDataSegmentLength_T" ).Value );
        }

    /// <summary>
    ///  Read T_LUStatus data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LUStatus data structure.
    /// </returns>
    static member private Read_T_LUStatus ( elem : XElement ) : T_LUStatus = 
        {
            LUN =
                lun_me.fromStringValue( elem.Element( XName.Get "LUN" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
            LUStatus_Success = 
                let subElem = elem.Element( XName.Get "LUStatus_Success" )
                if subElem = null then
                    None
                else
                    Some( ReaderWriter.Read_T_LUStatus_Success subElem );
        }

    /// <summary>
    ///  Read T_LUStatus_Success data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LUStatus_Success data structure.
    /// </returns>
    static member private Read_T_LUStatus_Success ( elem : XElement ) : T_LUStatus_Success = 
        {
            ReadBytesCount =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "ReadBytesCount" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_RESCOUNTER( itr ) )
                |> Seq.toList
            WrittenBytesCount =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "WrittenBytesCount" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_RESCOUNTER( itr ) )
                |> Seq.toList
            ReadTickCount =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "ReadTickCount" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_RESCOUNTER( itr ) )
                |> Seq.toList
            WriteTickCount =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "WriteTickCount" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_RESCOUNTER( itr ) )
                |> Seq.toList
            ACAStatus = 
                let subElem = elem.Element( XName.Get "ACAStatus" )
                if subElem = null then
                    None
                else
                    Some( ReaderWriter.Read_T_ACAStatus subElem );
        }

    /// <summary>
    ///  Read T_ACAStatus data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_ACAStatus data structure.
    /// </returns>
    static member private Read_T_ACAStatus ( elem : XElement ) : T_ACAStatus = 
        {
            ITNexus =
                ReaderWriter.Read_T_ITNEXUS( elem.Element( XName.Get "ITNexus" ) );
            StatusCode =
                Byte.Parse( elem.Element( XName.Get "StatusCode" ).Value );
            SenseKey =
                Byte.Parse( elem.Element( XName.Get "SenseKey" ).Value );
            AdditionalSenseCode =
                UInt16.Parse( elem.Element( XName.Get "AdditionalSenseCode" ).Value );
            IsCurrent =
                Boolean.Parse( elem.Element( XName.Get "IsCurrent" ).Value );
        }

    /// <summary>
    ///  Read T_LUResetResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LUResetResult data structure.
    /// </returns>
    static member private Read_T_LUResetResult ( elem : XElement ) : T_LUResetResult = 
        {
            LUN =
                lun_me.fromStringValue( elem.Element( XName.Get "LUN" ).Value );
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_MediaStatus data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_MediaStatus data structure.
    /// </returns>
    static member private Read_T_MediaStatus ( elem : XElement ) : T_MediaStatus = 
        {
            LUN =
                lun_me.fromStringValue( elem.Element( XName.Get "LUN" ).Value );
            ID =
                mediaidx_me.fromPrim( UInt32.Parse( elem.Element( XName.Get "ID" ).Value ) );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
            MediaStatus_Success = 
                let subElem = elem.Element( XName.Get "MediaStatus_Success" )
                if subElem = null then
                    None
                else
                    Some( ReaderWriter.Read_T_MediaStatus_Success subElem );
        }

    /// <summary>
    ///  Read T_MediaStatus_Success data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_MediaStatus_Success data structure.
    /// </returns>
    static member private Read_T_MediaStatus_Success ( elem : XElement ) : T_MediaStatus_Success = 
        {
            ReadBytesCount =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "ReadBytesCount" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_RESCOUNTER( itr ) )
                |> Seq.toList
            WrittenBytesCount =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "WrittenBytesCount" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_RESCOUNTER( itr ) )
                |> Seq.toList
            ReadTickCount =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "ReadTickCount" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_RESCOUNTER( itr ) )
                |> Seq.toList
            WriteTickCount =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "WriteTickCount" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_RESCOUNTER( itr ) )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_MediaControlResponse data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_MediaControlResponse data structure.
    /// </returns>
    static member private Read_T_MediaControlResponse ( elem : XElement ) : T_MediaControlResponse = 
        {
            LUN =
                lun_me.fromStringValue( elem.Element( XName.Get "LUN" ).Value );
            ID =
                mediaidx_me.fromPrim( UInt32.Parse( elem.Element( XName.Get "ID" ).Value ) );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
            Response =
                elem.Element( XName.Get "Response" ).Value;
        }

    /// <summary>
    ///  Read T_RESCOUNTER data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_RESCOUNTER data structure.
    /// </returns>
    static member private Read_T_RESCOUNTER ( elem : XElement ) : T_RESCOUNTER = 
        {
            Time =
                DateTime.SpecifyKind( DateTime( Int64.Parse( elem.Element( XName.Get "Time" ).Value ) ), DateTimeKind.Utc );
            Value =
                Int64.Parse( elem.Element( XName.Get "Value" ).Value );
            Count =
                Int64.Parse( elem.Element( XName.Get "Count" ).Value );
        }

    /// <summary>
    ///  Read T_ITNEXUS data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_ITNEXUS data structure.
    /// </returns>
    static member private Read_T_ITNEXUS ( elem : XElement ) : T_ITNEXUS = 
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
    ///  Write TargetDeviceCtrlRes data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_TargetDeviceCtrlRes ) : unit =
        let s = ReaderWriter.T_TargetDeviceCtrlRes_toString 0 2 d "TargetDeviceCtrlRes"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert TargetDeviceCtrlRes data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_TargetDeviceCtrlRes ) : string =
        ReaderWriter.T_TargetDeviceCtrlRes_toString 0 0 d "TargetDeviceCtrlRes"
        |> String.Concat

    /// <summary>
    ///  Write T_TargetDeviceCtrlRes data structure to configuration file.
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
    static member private T_TargetDeviceCtrlRes_toString ( indent : int ) ( indentStep : int ) ( elem : T_TargetDeviceCtrlRes ) ( elemName : string ) : seq<string> = 
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
            | U_ActiveTargetGroups( x ) ->
                yield! ReaderWriter.T_ActiveTargetGroups_toString ( indent + 1 ) indentStep ( x ) "ActiveTargetGroups"
            | U_LoadedTargetGroups( x ) ->
                yield! ReaderWriter.T_LoadedTargetGroups_toString ( indent + 1 ) indentStep ( x ) "LoadedTargetGroups"
            | U_InactivateTargetGroupResult( x ) ->
                yield! ReaderWriter.T_InactivateTargetGroupResult_toString ( indent + 1 ) indentStep ( x ) "InactivateTargetGroupResult"
            | U_ActivateTargetGroupResult( x ) ->
                yield! ReaderWriter.T_ActivateTargetGroupResult_toString ( indent + 1 ) indentStep ( x ) "ActivateTargetGroupResult"
            | U_UnloadTargetGroupResult( x ) ->
                yield! ReaderWriter.T_UnloadTargetGroupResult_toString ( indent + 1 ) indentStep ( x ) "UnloadTargetGroupResult"
            | U_LoadTargetGroupResult( x ) ->
                yield! ReaderWriter.T_LoadTargetGroupResult_toString ( indent + 1 ) indentStep ( x ) "LoadTargetGroupResult"
            | U_SetLogParametersResult( x ) ->
                yield sprintf "%s%s<SetLogParametersResult>%b</SetLogParametersResult>" singleIndent indentStr (x)
            | U_LogParameters( x ) ->
                yield! ReaderWriter.T_LogParameters_toString ( indent + 1 ) indentStep ( x ) "LogParameters"
            | U_DeviceName( x ) ->
                if (x).Length < 0 then
                    raise <| ConfRWException( "Min value(string) restriction error. DeviceName" )
                if (x).Length > 512 then
                    raise <| ConfRWException( "Max value(string) restriction error. DeviceName" )
                yield sprintf "%s%s<DeviceName>%s</DeviceName>" singleIndent indentStr ( ReaderWriter.xmlEncode(x) )
            | U_SessionList( x ) ->
                yield! ReaderWriter.T_SessionList_toString ( indent + 1 ) indentStep ( x ) "SessionList"
            | U_DestructSessionResult( x ) ->
                yield! ReaderWriter.T_DestructSessionResult_toString ( indent + 1 ) indentStep ( x ) "DestructSessionResult"
            | U_ConnectionList( x ) ->
                yield! ReaderWriter.T_ConnectionList_toString ( indent + 1 ) indentStep ( x ) "ConnectionList"
            | U_LUStatus( x ) ->
                yield! ReaderWriter.T_LUStatus_toString ( indent + 1 ) indentStep ( x ) "LUStatus"
            | U_LUResetResult( x ) ->
                yield! ReaderWriter.T_LUResetResult_toString ( indent + 1 ) indentStep ( x ) "LUResetResult"
            | U_MediaStatus( x ) ->
                yield! ReaderWriter.T_MediaStatus_toString ( indent + 1 ) indentStep ( x ) "MediaStatus"
            | U_MediaControlResponse( x ) ->
                yield! ReaderWriter.T_MediaControlResponse_toString ( indent + 1 ) indentStep ( x ) "MediaControlResponse"
            | U_UnexpectedError( x ) ->
                yield sprintf "%s%s<UnexpectedError>%s</UnexpectedError>" singleIndent indentStr ( ReaderWriter.xmlEncode(x) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_ActiveTargetGroups data structure to configuration file.
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
    static member private T_ActiveTargetGroups_toString ( indent : int ) ( indentStep : int ) ( elem : T_ActiveTargetGroups ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.ActiveTGInfo.Length < 0 || elem.ActiveTGInfo.Length > 255 then 
                raise <| ConfRWException( "Element count restriction error. ActiveTGInfo" )
            for itr in elem.ActiveTGInfo do
                yield! ReaderWriter.T_ActiveTGInfo_toString ( indent + 1 ) indentStep itr "ActiveTGInfo"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_ActiveTGInfo data structure to configuration file.
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
    static member private T_ActiveTGInfo_toString ( indent : int ) ( indentStep : int ) ( elem : T_ActiveTGInfo ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<ID>%s</ID>" singleIndent indentStr ( tgid_me.toString (elem.ID) )
            if (elem.Name).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. Name" )
            if (elem.Name).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. Name" )
            yield sprintf "%s%s<Name>%s</Name>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Name) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LoadedTargetGroups data structure to configuration file.
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
    static member private T_LoadedTargetGroups_toString ( indent : int ) ( indentStep : int ) ( elem : T_LoadedTargetGroups ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.LoadedTGInfo.Length < 0 || elem.LoadedTGInfo.Length > 255 then 
                raise <| ConfRWException( "Element count restriction error. LoadedTGInfo" )
            for itr in elem.LoadedTGInfo do
                yield! ReaderWriter.T_LoadedTGInfo_toString ( indent + 1 ) indentStep itr "LoadedTGInfo"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LoadedTGInfo data structure to configuration file.
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
    static member private T_LoadedTGInfo_toString ( indent : int ) ( indentStep : int ) ( elem : T_LoadedTGInfo ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<ID>%s</ID>" singleIndent indentStr ( tgid_me.toString (elem.ID) )
            if (elem.Name).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. Name" )
            if (elem.Name).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. Name" )
            yield sprintf "%s%s<Name>%s</Name>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Name) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_InactivateTargetGroupResult data structure to configuration file.
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
    static member private T_InactivateTargetGroupResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_InactivateTargetGroupResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<ID>%s</ID>" singleIndent indentStr ( tgid_me.toString (elem.ID) )
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_ActivateTargetGroupResult data structure to configuration file.
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
    static member private T_ActivateTargetGroupResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_ActivateTargetGroupResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<ID>%s</ID>" singleIndent indentStr ( tgid_me.toString (elem.ID) )
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_UnloadTargetGroupResult data structure to configuration file.
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
    static member private T_UnloadTargetGroupResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_UnloadTargetGroupResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<ID>%s</ID>" singleIndent indentStr ( tgid_me.toString (elem.ID) )
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LoadTargetGroupResult data structure to configuration file.
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
    static member private T_LoadTargetGroupResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_LoadTargetGroupResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<ID>%s</ID>" singleIndent indentStr ( tgid_me.toString (elem.ID) )
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LogParameters data structure to configuration file.
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
    static member private T_LogParameters_toString ( indent : int ) ( indentStep : int ) ( elem : T_LogParameters ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if (elem.SoftLimit) < 0u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. SoftLimit" )
            if (elem.SoftLimit) > 10000000u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. SoftLimit" )
            yield sprintf "%s%s<SoftLimit>%d</SoftLimit>" singleIndent indentStr (elem.SoftLimit)
            if (elem.HardLimit) < 100u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. HardLimit" )
            if (elem.HardLimit) > 20000000u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. HardLimit" )
            yield sprintf "%s%s<HardLimit>%d</HardLimit>" singleIndent indentStr (elem.HardLimit)
            yield sprintf "%s%s<LogLevel>%s</LogLevel>" singleIndent indentStr ( LogLevel.toString (elem.LogLevel) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_SessionList data structure to configuration file.
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
    static member private T_SessionList_toString ( indent : int ) ( indentStep : int ) ( elem : T_SessionList ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.Session.Length < 0 || elem.Session.Length > 510 then 
                raise <| ConfRWException( "Element count restriction error. Session" )
            for itr in elem.Session do
                yield! ReaderWriter.T_Session_toString ( indent + 1 ) indentStep itr "Session"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Session data structure to configuration file.
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
    static member private T_Session_toString ( indent : int ) ( indentStep : int ) ( elem : T_Session ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetGroupID>%s</TargetGroupID>" singleIndent indentStr ( tgid_me.toString (elem.TargetGroupID) )
            yield sprintf "%s%s<TargetNodeID>%d</TargetNodeID>" singleIndent indentStr ( tnodeidx_me.toPrim (elem.TargetNodeID) )
            yield sprintf "%s%s<TSIH>%d</TSIH>" singleIndent indentStr ( tsih_me.toPrim (elem.TSIH) )
            yield! ReaderWriter.T_ITNEXUS_toString ( indent + 1 ) indentStep ( elem.ITNexus ) "ITNexus"
            yield! ReaderWriter.T_SessionParameters_toString ( indent + 1 ) indentStep ( elem.SessionParameters ) "SessionParameters"
            yield sprintf "%s%s<EstablishTime>%d</EstablishTime>" singleIndent indentStr ( (elem.EstablishTime).Ticks )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_SessionParameters data structure to configuration file.
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
    static member private T_SessionParameters_toString ( indent : int ) ( indentStep : int ) ( elem : T_SessionParameters ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if (elem.MaxConnections) < 0us then
                raise <| ConfRWException( "Min value(unsignedShort) restriction error. MaxConnections" )
            if (elem.MaxConnections) > 16us then
                raise <| ConfRWException( "Max value(unsignedShort) restriction error. MaxConnections" )
            yield sprintf "%s%s<MaxConnections>%d</MaxConnections>" singleIndent indentStr (elem.MaxConnections)
            if (elem.InitiatorAlias).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. InitiatorAlias" )
            if (elem.InitiatorAlias).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. InitiatorAlias" )
            yield sprintf "%s%s<InitiatorAlias>%s</InitiatorAlias>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.InitiatorAlias) )
            yield sprintf "%s%s<InitialR2T>%b</InitialR2T>" singleIndent indentStr (elem.InitialR2T)
            yield sprintf "%s%s<ImmediateData>%b</ImmediateData>" singleIndent indentStr (elem.ImmediateData)
            if (elem.MaxBurstLength) < 512u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. MaxBurstLength" )
            if (elem.MaxBurstLength) > 16777215u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. MaxBurstLength" )
            yield sprintf "%s%s<MaxBurstLength>%d</MaxBurstLength>" singleIndent indentStr (elem.MaxBurstLength)
            if (elem.FirstBurstLength) < 512u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. FirstBurstLength" )
            if (elem.FirstBurstLength) > 16777215u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. FirstBurstLength" )
            yield sprintf "%s%s<FirstBurstLength>%d</FirstBurstLength>" singleIndent indentStr (elem.FirstBurstLength)
            if (elem.DefaultTime2Wait) < 0us then
                raise <| ConfRWException( "Min value(unsignedShort) restriction error. DefaultTime2Wait" )
            if (elem.DefaultTime2Wait) > 3600us then
                raise <| ConfRWException( "Max value(unsignedShort) restriction error. DefaultTime2Wait" )
            yield sprintf "%s%s<DefaultTime2Wait>%d</DefaultTime2Wait>" singleIndent indentStr (elem.DefaultTime2Wait)
            if (elem.DefaultTime2Retain) < 0us then
                raise <| ConfRWException( "Min value(unsignedShort) restriction error. DefaultTime2Retain" )
            if (elem.DefaultTime2Retain) > 3600us then
                raise <| ConfRWException( "Max value(unsignedShort) restriction error. DefaultTime2Retain" )
            yield sprintf "%s%s<DefaultTime2Retain>%d</DefaultTime2Retain>" singleIndent indentStr (elem.DefaultTime2Retain)
            if (elem.MaxOutstandingR2T) < 1us then
                raise <| ConfRWException( "Min value(unsignedShort) restriction error. MaxOutstandingR2T" )
            if (elem.MaxOutstandingR2T) > 65535us then
                raise <| ConfRWException( "Max value(unsignedShort) restriction error. MaxOutstandingR2T" )
            yield sprintf "%s%s<MaxOutstandingR2T>%d</MaxOutstandingR2T>" singleIndent indentStr (elem.MaxOutstandingR2T)
            yield sprintf "%s%s<DataPDUInOrder>%b</DataPDUInOrder>" singleIndent indentStr (elem.DataPDUInOrder)
            yield sprintf "%s%s<DataSequenceInOrder>%b</DataSequenceInOrder>" singleIndent indentStr (elem.DataSequenceInOrder)
            if (elem.ErrorRecoveryLevel) < 0uy then
                raise <| ConfRWException( "Min value(unsignedByte) restriction error. ErrorRecoveryLevel" )
            if (elem.ErrorRecoveryLevel) > 2uy then
                raise <| ConfRWException( "Max value(unsignedByte) restriction error. ErrorRecoveryLevel" )
            yield sprintf "%s%s<ErrorRecoveryLevel>%d</ErrorRecoveryLevel>" singleIndent indentStr (elem.ErrorRecoveryLevel)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_DestructSessionResult data structure to configuration file.
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
    static member private T_DestructSessionResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_DestructSessionResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TSIH>%d</TSIH>" singleIndent indentStr ( tsih_me.toPrim (elem.TSIH) )
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_ConnectionList data structure to configuration file.
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
    static member private T_ConnectionList_toString ( indent : int ) ( indentStep : int ) ( elem : T_ConnectionList ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.Connection.Length < 0 || elem.Connection.Length > 8160 then 
                raise <| ConfRWException( "Element count restriction error. Connection" )
            for itr in elem.Connection do
                yield! ReaderWriter.T_Connection_toString ( indent + 1 ) indentStep itr "Connection"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Connection data structure to configuration file.
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
    static member private T_Connection_toString ( indent : int ) ( indentStep : int ) ( elem : T_Connection ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TSIH>%d</TSIH>" singleIndent indentStr ( tsih_me.toPrim (elem.TSIH) )
            yield sprintf "%s%s<ConnectionID>%d</ConnectionID>" singleIndent indentStr ( cid_me.toPrim (elem.ConnectionID) )
            yield sprintf "%s%s<ConnectionCount>%d</ConnectionCount>" singleIndent indentStr ( concnt_me.toPrim (elem.ConnectionCount) )
            if elem.ReceiveBytesCount.Length < 0 || elem.ReceiveBytesCount.Length > 62 then 
                raise <| ConfRWException( "Element count restriction error. ReceiveBytesCount" )
            for itr in elem.ReceiveBytesCount do
                yield! ReaderWriter.T_RESCOUNTER_toString ( indent + 1 ) indentStep ( itr ) "ReceiveBytesCount"
            if elem.SentBytesCount.Length < 0 || elem.SentBytesCount.Length > 62 then 
                raise <| ConfRWException( "Element count restriction error. SentBytesCount" )
            for itr in elem.SentBytesCount do
                yield! ReaderWriter.T_RESCOUNTER_toString ( indent + 1 ) indentStep ( itr ) "SentBytesCount"
            yield! ReaderWriter.T_ConnectionParameters_toString ( indent + 1 ) indentStep ( elem.ConnectionParameters ) "ConnectionParameters"
            yield sprintf "%s%s<EstablishTime>%d</EstablishTime>" singleIndent indentStr ( (elem.EstablishTime).Ticks )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_ConnectionParameters data structure to configuration file.
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
    static member private T_ConnectionParameters_toString ( indent : int ) ( indentStep : int ) ( elem : T_ConnectionParameters ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<AuthMethod>%s</AuthMethod>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.AuthMethod) )
            yield sprintf "%s%s<HeaderDigest>%s</HeaderDigest>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.HeaderDigest) )
            yield sprintf "%s%s<DataDigest>%s</DataDigest>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.DataDigest) )
            if (elem.MaxRecvDataSegmentLength_I) < 512u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. MaxRecvDataSegmentLength_I" )
            if (elem.MaxRecvDataSegmentLength_I) > 16777215u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. MaxRecvDataSegmentLength_I" )
            yield sprintf "%s%s<MaxRecvDataSegmentLength_I>%d</MaxRecvDataSegmentLength_I>" singleIndent indentStr (elem.MaxRecvDataSegmentLength_I)
            if (elem.MaxRecvDataSegmentLength_T) < 512u then
                raise <| ConfRWException( "Min value(unsignedInt) restriction error. MaxRecvDataSegmentLength_T" )
            if (elem.MaxRecvDataSegmentLength_T) > 16777215u then
                raise <| ConfRWException( "Max value(unsignedInt) restriction error. MaxRecvDataSegmentLength_T" )
            yield sprintf "%s%s<MaxRecvDataSegmentLength_T>%d</MaxRecvDataSegmentLength_T>" singleIndent indentStr (elem.MaxRecvDataSegmentLength_T)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LUStatus data structure to configuration file.
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
    static member private T_LUStatus_toString ( indent : int ) ( indentStep : int ) ( elem : T_LUStatus ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if lun_me.toPrim (elem.LUN) < 0UL then
                raise <| ConfRWException( "Min value(LUN_T) restriction error. LUN" )
            if lun_me.toPrim (elem.LUN) > 255UL then
                raise <| ConfRWException( "Max value(LUN_T) restriction error. LUN" )
            yield sprintf "%s%s<LUN>%s</LUN>" singleIndent indentStr ( lun_me.toString (elem.LUN) )
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            if elem.LUStatus_Success.IsSome then
                yield! ReaderWriter.T_LUStatus_Success_toString ( indent + 1 ) indentStep ( elem.LUStatus_Success.Value ) "LUStatus_Success"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LUStatus_Success data structure to configuration file.
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
    static member private T_LUStatus_Success_toString ( indent : int ) ( indentStep : int ) ( elem : T_LUStatus_Success ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.ReadBytesCount.Length < 0 || elem.ReadBytesCount.Length > 62 then 
                raise <| ConfRWException( "Element count restriction error. ReadBytesCount" )
            for itr in elem.ReadBytesCount do
                yield! ReaderWriter.T_RESCOUNTER_toString ( indent + 1 ) indentStep ( itr ) "ReadBytesCount"
            if elem.WrittenBytesCount.Length < 0 || elem.WrittenBytesCount.Length > 62 then 
                raise <| ConfRWException( "Element count restriction error. WrittenBytesCount" )
            for itr in elem.WrittenBytesCount do
                yield! ReaderWriter.T_RESCOUNTER_toString ( indent + 1 ) indentStep ( itr ) "WrittenBytesCount"
            if elem.ReadTickCount.Length < 0 || elem.ReadTickCount.Length > 62 then 
                raise <| ConfRWException( "Element count restriction error. ReadTickCount" )
            for itr in elem.ReadTickCount do
                yield! ReaderWriter.T_RESCOUNTER_toString ( indent + 1 ) indentStep ( itr ) "ReadTickCount"
            if elem.WriteTickCount.Length < 0 || elem.WriteTickCount.Length > 62 then 
                raise <| ConfRWException( "Element count restriction error. WriteTickCount" )
            for itr in elem.WriteTickCount do
                yield! ReaderWriter.T_RESCOUNTER_toString ( indent + 1 ) indentStep ( itr ) "WriteTickCount"
            if elem.ACAStatus.IsSome then
                yield! ReaderWriter.T_ACAStatus_toString ( indent + 1 ) indentStep ( elem.ACAStatus.Value ) "ACAStatus"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_ACAStatus data structure to configuration file.
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
    static member private T_ACAStatus_toString ( indent : int ) ( indentStep : int ) ( elem : T_ACAStatus ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield! ReaderWriter.T_ITNEXUS_toString ( indent + 1 ) indentStep ( elem.ITNexus ) "ITNexus"
            yield sprintf "%s%s<StatusCode>%d</StatusCode>" singleIndent indentStr (elem.StatusCode)
            yield sprintf "%s%s<SenseKey>%d</SenseKey>" singleIndent indentStr (elem.SenseKey)
            yield sprintf "%s%s<AdditionalSenseCode>%d</AdditionalSenseCode>" singleIndent indentStr (elem.AdditionalSenseCode)
            yield sprintf "%s%s<IsCurrent>%b</IsCurrent>" singleIndent indentStr (elem.IsCurrent)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LUResetResult data structure to configuration file.
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
    static member private T_LUResetResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_LUResetResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if lun_me.toPrim (elem.LUN) < 0UL then
                raise <| ConfRWException( "Min value(LUN_T) restriction error. LUN" )
            if lun_me.toPrim (elem.LUN) > 255UL then
                raise <| ConfRWException( "Max value(LUN_T) restriction error. LUN" )
            yield sprintf "%s%s<LUN>%s</LUN>" singleIndent indentStr ( lun_me.toString (elem.LUN) )
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_MediaStatus data structure to configuration file.
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
    static member private T_MediaStatus_toString ( indent : int ) ( indentStep : int ) ( elem : T_MediaStatus ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if lun_me.toPrim (elem.LUN) < 0UL then
                raise <| ConfRWException( "Min value(LUN_T) restriction error. LUN" )
            if lun_me.toPrim (elem.LUN) > 255UL then
                raise <| ConfRWException( "Max value(LUN_T) restriction error. LUN" )
            yield sprintf "%s%s<LUN>%s</LUN>" singleIndent indentStr ( lun_me.toString (elem.LUN) )
            yield sprintf "%s%s<ID>%d</ID>" singleIndent indentStr ( mediaidx_me.toPrim (elem.ID) )
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            if elem.MediaStatus_Success.IsSome then
                yield! ReaderWriter.T_MediaStatus_Success_toString ( indent + 1 ) indentStep ( elem.MediaStatus_Success.Value ) "MediaStatus_Success"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_MediaStatus_Success data structure to configuration file.
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
    static member private T_MediaStatus_Success_toString ( indent : int ) ( indentStep : int ) ( elem : T_MediaStatus_Success ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.ReadBytesCount.Length < 0 || elem.ReadBytesCount.Length > 62 then 
                raise <| ConfRWException( "Element count restriction error. ReadBytesCount" )
            for itr in elem.ReadBytesCount do
                yield! ReaderWriter.T_RESCOUNTER_toString ( indent + 1 ) indentStep ( itr ) "ReadBytesCount"
            if elem.WrittenBytesCount.Length < 0 || elem.WrittenBytesCount.Length > 62 then 
                raise <| ConfRWException( "Element count restriction error. WrittenBytesCount" )
            for itr in elem.WrittenBytesCount do
                yield! ReaderWriter.T_RESCOUNTER_toString ( indent + 1 ) indentStep ( itr ) "WrittenBytesCount"
            if elem.ReadTickCount.Length < 0 || elem.ReadTickCount.Length > 62 then 
                raise <| ConfRWException( "Element count restriction error. ReadTickCount" )
            for itr in elem.ReadTickCount do
                yield! ReaderWriter.T_RESCOUNTER_toString ( indent + 1 ) indentStep ( itr ) "ReadTickCount"
            if elem.WriteTickCount.Length < 0 || elem.WriteTickCount.Length > 62 then 
                raise <| ConfRWException( "Element count restriction error. WriteTickCount" )
            for itr in elem.WriteTickCount do
                yield! ReaderWriter.T_RESCOUNTER_toString ( indent + 1 ) indentStep ( itr ) "WriteTickCount"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_MediaControlResponse data structure to configuration file.
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
    static member private T_MediaControlResponse_toString ( indent : int ) ( indentStep : int ) ( elem : T_MediaControlResponse ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if lun_me.toPrim (elem.LUN) < 0UL then
                raise <| ConfRWException( "Min value(LUN_T) restriction error. LUN" )
            if lun_me.toPrim (elem.LUN) > 255UL then
                raise <| ConfRWException( "Max value(LUN_T) restriction error. LUN" )
            yield sprintf "%s%s<LUN>%s</LUN>" singleIndent indentStr ( lun_me.toString (elem.LUN) )
            yield sprintf "%s%s<ID>%d</ID>" singleIndent indentStr ( mediaidx_me.toPrim (elem.ID) )
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s%s<Response>%s</Response>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Response) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_RESCOUNTER data structure to configuration file.
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
    static member private T_RESCOUNTER_toString ( indent : int ) ( indentStep : int ) ( elem : T_RESCOUNTER ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Time>%d</Time>" singleIndent indentStr ( (elem.Time).Ticks )
            yield sprintf "%s%s<Value>%d</Value>" singleIndent indentStr (elem.Value)
            yield sprintf "%s%s<Count>%d</Count>" singleIndent indentStr (elem.Count)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_ITNEXUS data structure to configuration file.
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
    static member private T_ITNEXUS_toString ( indent : int ) ( indentStep : int ) ( elem : T_ITNEXUS ) ( elemName : string ) : seq<string> = 
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


