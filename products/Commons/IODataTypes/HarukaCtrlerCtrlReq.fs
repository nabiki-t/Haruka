//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

namespace Haruka.IODataTypes.HarukaCtrlerCtrlReq

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_HarukaCtrlerCtrlReq = {
    Request : T_Request;
}

and [<NoComparison>]T_Request = 
    | U_Login of bool
    | U_Logout of CtrlSessionID
    | U_NoOperation of CtrlSessionID
    | U_GetControllerConfig of CtrlSessionID
    | U_SetControllerConfig of T_SetControllerConfig
    | U_GetTargetDeviceDir of CtrlSessionID
    | U_CreateTargetDeviceDir of T_CreateTargetDeviceDir
    | U_DeleteTargetDeviceDir of T_DeleteTargetDeviceDir
    | U_GetTargetDeviceConfig of T_GetTargetDeviceConfig
    | U_CreateTargetDeviceConfig of T_CreateTargetDeviceConfig
    | U_GetTargetGroupID of T_GetTargetGroupID
    | U_GetTargetGroupConfig of T_GetTargetGroupConfig
    | U_GetAllTargetGroupConfig of T_GetAllTargetGroupConfig
    | U_CreateTargetGroupConfig of T_CreateTargetGroupConfig
    | U_DeleteTargetGroupConfig of T_DeleteTargetGroupConfig
    | U_GetLUWorkDir of T_GetLUWorkDir
    | U_CreateLUWorkDir of T_CreateLUWorkDir
    | U_DeleteLUWorkDir of T_DeleteLUWorkDir
    | U_GetTargetDeviceProcs of CtrlSessionID
    | U_KillTargetDeviceProc of T_KillTargetDeviceProc
    | U_StartTargetDeviceProc of T_StartTargetDeviceProc
    | U_TargetDeviceCtrlRequest of T_TargetDeviceCtrlRequest
    | U_CreateMediaFile of T_CreateMediaFile
    | U_GetInitMediaStatus of CtrlSessionID
    | U_KillInitMediaProc of T_KillInitMediaProc

and [<NoComparison>]T_SetControllerConfig = {
    SessionID : CtrlSessionID;
    Config : string;
}

and [<NoComparison>]T_CreateTargetDeviceDir = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
}

and [<NoComparison>]T_DeleteTargetDeviceDir = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
}

and [<NoComparison>]T_GetTargetDeviceConfig = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
}

and [<NoComparison>]T_CreateTargetDeviceConfig = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
    Config : string;
}

and [<NoComparison>]T_GetTargetGroupID = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
}

and [<NoComparison>]T_GetTargetGroupConfig = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
    TargetGroupID : TGID_T;
}

and [<NoComparison>]T_GetAllTargetGroupConfig = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
}

and [<NoComparison>]T_CreateTargetGroupConfig = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
    TargetGroupID : TGID_T;
    Config : string;
}

and [<NoComparison>]T_DeleteTargetGroupConfig = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
    TargetGroupID : TGID_T;
}

and [<NoComparison>]T_GetLUWorkDir = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
}

and [<NoComparison>]T_CreateLUWorkDir = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
    LUN : LUN_T;
}

and [<NoComparison>]T_DeleteLUWorkDir = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
    LUN : LUN_T;
}

and [<NoComparison>]T_KillTargetDeviceProc = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
}

and [<NoComparison>]T_StartTargetDeviceProc = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
}

and [<NoComparison>]T_TargetDeviceCtrlRequest = {
    SessionID : CtrlSessionID;
    TargetDeviceID : TDID_T;
    Request : string;
}

and [<NoComparison>]T_CreateMediaFile = {
    SessionID : CtrlSessionID;
    MediaType : T_MediaType;
}

and [<NoComparison>]T_MediaType = 
    | U_PlainFile of T_PlainFile

and [<NoComparison>]T_PlainFile = {
    FileName : string;
    FileSize : int64;
}

and [<NoComparison>]T_KillInitMediaProc = {
    SessionID : CtrlSessionID;
    ProcID : uint64;
}

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='HarukaCtrlerCtrlReq' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='Request' >
        <xsd:complexType><xsd:choice>
          <xsd:element name='Login' >
            <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
          </xsd:element>
          <xsd:element name='Logout' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='NoOperation' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='GetControllerConfig' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='SetControllerConfig' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='Config' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='GetTargetDeviceDir' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='CreateTargetDeviceDir' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='DeleteTargetDeviceDir' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='GetTargetDeviceConfig' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='CreateTargetDeviceConfig' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='Config' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='GetTargetGroupID' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='GetTargetGroupConfig' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetGroupID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='GetAllTargetGroupConfig' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='CreateTargetGroupConfig' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetGroupID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='Config' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='DeleteTargetGroupConfig' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetGroupID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='GetLUWorkDir' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='CreateLUWorkDir' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='LUN' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedLong'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='255' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='DeleteLUWorkDir' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='LUN' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedLong'>
                    <xsd:minInclusive value='0' />
                    <xsd:maxInclusive value='255' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='GetTargetDeviceProcs' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='KillTargetDeviceProc' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='StartTargetDeviceProc' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='TargetDeviceCtrlRequest' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='Request' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='CreateMediaFile' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='MediaType' >
                <xsd:complexType><xsd:choice>
                  <xsd:element name='PlainFile' >
                    <xsd:complexType><xsd:sequence>
                      <xsd:element name='FileName' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:string'>
                            <xsd:minLength value='1' />
                            <xsd:maxLength value='256' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='FileSize' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:long'>
                            <xsd:minInclusive value='1' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                    </xsd:sequence></xsd:complexType>
                  </xsd:element>
                </xsd:choice></xsd:complexType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='GetInitMediaStatus' >
            <xsd:simpleType>
              <xsd:restriction base='xsd:string'>
                <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
              </xsd:restriction>
            </xsd:simpleType>
          </xsd:element>
          <xsd:element name='KillInitMediaProc' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ProcID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedLong'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
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
    ///  Load HarukaCtrlerCtrlReq data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded HarukaCtrlerCtrlReq data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_HarukaCtrlerCtrlReq =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load HarukaCtrlerCtrlReq data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded HarukaCtrlerCtrlReq data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_HarukaCtrlerCtrlReq =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "HarukaCtrlerCtrlReq" |> xdoc.Element |> ReaderWriter.Read_T_HarukaCtrlerCtrlReq

    /// <summary>
    ///  Read T_HarukaCtrlerCtrlReq data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_HarukaCtrlerCtrlReq data structure.
    /// </returns>
    static member private Read_T_HarukaCtrlerCtrlReq ( elem : XElement ) : T_HarukaCtrlerCtrlReq = 
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
        | "Login" ->
            U_Login( Boolean.Parse( firstChild.Value ) )
        | "Logout" ->
            U_Logout( new CtrlSessionID( firstChild.Value ) )
        | "NoOperation" ->
            U_NoOperation( new CtrlSessionID( firstChild.Value ) )
        | "GetControllerConfig" ->
            U_GetControllerConfig( new CtrlSessionID( firstChild.Value ) )
        | "SetControllerConfig" ->
            U_SetControllerConfig( ReaderWriter.Read_T_SetControllerConfig firstChild )
        | "GetTargetDeviceDir" ->
            U_GetTargetDeviceDir( new CtrlSessionID( firstChild.Value ) )
        | "CreateTargetDeviceDir" ->
            U_CreateTargetDeviceDir( ReaderWriter.Read_T_CreateTargetDeviceDir firstChild )
        | "DeleteTargetDeviceDir" ->
            U_DeleteTargetDeviceDir( ReaderWriter.Read_T_DeleteTargetDeviceDir firstChild )
        | "GetTargetDeviceConfig" ->
            U_GetTargetDeviceConfig( ReaderWriter.Read_T_GetTargetDeviceConfig firstChild )
        | "CreateTargetDeviceConfig" ->
            U_CreateTargetDeviceConfig( ReaderWriter.Read_T_CreateTargetDeviceConfig firstChild )
        | "GetTargetGroupID" ->
            U_GetTargetGroupID( ReaderWriter.Read_T_GetTargetGroupID firstChild )
        | "GetTargetGroupConfig" ->
            U_GetTargetGroupConfig( ReaderWriter.Read_T_GetTargetGroupConfig firstChild )
        | "GetAllTargetGroupConfig" ->
            U_GetAllTargetGroupConfig( ReaderWriter.Read_T_GetAllTargetGroupConfig firstChild )
        | "CreateTargetGroupConfig" ->
            U_CreateTargetGroupConfig( ReaderWriter.Read_T_CreateTargetGroupConfig firstChild )
        | "DeleteTargetGroupConfig" ->
            U_DeleteTargetGroupConfig( ReaderWriter.Read_T_DeleteTargetGroupConfig firstChild )
        | "GetLUWorkDir" ->
            U_GetLUWorkDir( ReaderWriter.Read_T_GetLUWorkDir firstChild )
        | "CreateLUWorkDir" ->
            U_CreateLUWorkDir( ReaderWriter.Read_T_CreateLUWorkDir firstChild )
        | "DeleteLUWorkDir" ->
            U_DeleteLUWorkDir( ReaderWriter.Read_T_DeleteLUWorkDir firstChild )
        | "GetTargetDeviceProcs" ->
            U_GetTargetDeviceProcs( new CtrlSessionID( firstChild.Value ) )
        | "KillTargetDeviceProc" ->
            U_KillTargetDeviceProc( ReaderWriter.Read_T_KillTargetDeviceProc firstChild )
        | "StartTargetDeviceProc" ->
            U_StartTargetDeviceProc( ReaderWriter.Read_T_StartTargetDeviceProc firstChild )
        | "TargetDeviceCtrlRequest" ->
            U_TargetDeviceCtrlRequest( ReaderWriter.Read_T_TargetDeviceCtrlRequest firstChild )
        | "CreateMediaFile" ->
            U_CreateMediaFile( ReaderWriter.Read_T_CreateMediaFile firstChild )
        | "GetInitMediaStatus" ->
            U_GetInitMediaStatus( new CtrlSessionID( firstChild.Value ) )
        | "KillInitMediaProc" ->
            U_KillInitMediaProc( ReaderWriter.Read_T_KillInitMediaProc firstChild )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_SetControllerConfig data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_SetControllerConfig data structure.
    /// </returns>
    static member private Read_T_SetControllerConfig ( elem : XElement ) : T_SetControllerConfig = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            Config =
                elem.Element( XName.Get "Config" ).Value;
        }

    /// <summary>
    ///  Read T_CreateTargetDeviceDir data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_CreateTargetDeviceDir data structure.
    /// </returns>
    static member private Read_T_CreateTargetDeviceDir ( elem : XElement ) : T_CreateTargetDeviceDir = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
        }

    /// <summary>
    ///  Read T_DeleteTargetDeviceDir data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_DeleteTargetDeviceDir data structure.
    /// </returns>
    static member private Read_T_DeleteTargetDeviceDir ( elem : XElement ) : T_DeleteTargetDeviceDir = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
        }

    /// <summary>
    ///  Read T_GetTargetDeviceConfig data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_GetTargetDeviceConfig data structure.
    /// </returns>
    static member private Read_T_GetTargetDeviceConfig ( elem : XElement ) : T_GetTargetDeviceConfig = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
        }

    /// <summary>
    ///  Read T_CreateTargetDeviceConfig data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_CreateTargetDeviceConfig data structure.
    /// </returns>
    static member private Read_T_CreateTargetDeviceConfig ( elem : XElement ) : T_CreateTargetDeviceConfig = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            Config =
                elem.Element( XName.Get "Config" ).Value;
        }

    /// <summary>
    ///  Read T_GetTargetGroupID data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_GetTargetGroupID data structure.
    /// </returns>
    static member private Read_T_GetTargetGroupID ( elem : XElement ) : T_GetTargetGroupID = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
        }

    /// <summary>
    ///  Read T_GetTargetGroupConfig data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_GetTargetGroupConfig data structure.
    /// </returns>
    static member private Read_T_GetTargetGroupConfig ( elem : XElement ) : T_GetTargetGroupConfig = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            TargetGroupID =
                tgid_me.fromString( elem.Element( XName.Get "TargetGroupID" ).Value );
        }

    /// <summary>
    ///  Read T_GetAllTargetGroupConfig data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_GetAllTargetGroupConfig data structure.
    /// </returns>
    static member private Read_T_GetAllTargetGroupConfig ( elem : XElement ) : T_GetAllTargetGroupConfig = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
        }

    /// <summary>
    ///  Read T_CreateTargetGroupConfig data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_CreateTargetGroupConfig data structure.
    /// </returns>
    static member private Read_T_CreateTargetGroupConfig ( elem : XElement ) : T_CreateTargetGroupConfig = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            TargetGroupID =
                tgid_me.fromString( elem.Element( XName.Get "TargetGroupID" ).Value );
            Config =
                elem.Element( XName.Get "Config" ).Value;
        }

    /// <summary>
    ///  Read T_DeleteTargetGroupConfig data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_DeleteTargetGroupConfig data structure.
    /// </returns>
    static member private Read_T_DeleteTargetGroupConfig ( elem : XElement ) : T_DeleteTargetGroupConfig = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            TargetGroupID =
                tgid_me.fromString( elem.Element( XName.Get "TargetGroupID" ).Value );
        }

    /// <summary>
    ///  Read T_GetLUWorkDir data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_GetLUWorkDir data structure.
    /// </returns>
    static member private Read_T_GetLUWorkDir ( elem : XElement ) : T_GetLUWorkDir = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
        }

    /// <summary>
    ///  Read T_CreateLUWorkDir data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_CreateLUWorkDir data structure.
    /// </returns>
    static member private Read_T_CreateLUWorkDir ( elem : XElement ) : T_CreateLUWorkDir = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            LUN =
                lun_me.fromStringValue( elem.Element( XName.Get "LUN" ).Value );
        }

    /// <summary>
    ///  Read T_DeleteLUWorkDir data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_DeleteLUWorkDir data structure.
    /// </returns>
    static member private Read_T_DeleteLUWorkDir ( elem : XElement ) : T_DeleteLUWorkDir = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            LUN =
                lun_me.fromStringValue( elem.Element( XName.Get "LUN" ).Value );
        }

    /// <summary>
    ///  Read T_KillTargetDeviceProc data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_KillTargetDeviceProc data structure.
    /// </returns>
    static member private Read_T_KillTargetDeviceProc ( elem : XElement ) : T_KillTargetDeviceProc = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
        }

    /// <summary>
    ///  Read T_StartTargetDeviceProc data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_StartTargetDeviceProc data structure.
    /// </returns>
    static member private Read_T_StartTargetDeviceProc ( elem : XElement ) : T_StartTargetDeviceProc = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
        }

    /// <summary>
    ///  Read T_TargetDeviceCtrlRequest data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TargetDeviceCtrlRequest data structure.
    /// </returns>
    static member private Read_T_TargetDeviceCtrlRequest ( elem : XElement ) : T_TargetDeviceCtrlRequest = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            Request =
                elem.Element( XName.Get "Request" ).Value;
        }

    /// <summary>
    ///  Read T_CreateMediaFile data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_CreateMediaFile data structure.
    /// </returns>
    static member private Read_T_CreateMediaFile ( elem : XElement ) : T_CreateMediaFile = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            MediaType =
                ReaderWriter.Read_T_MediaType( elem.Element( XName.Get "MediaType" ) );
        }

    /// <summary>
    ///  Read T_MediaType data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_MediaType data structure.
    /// </returns>
    static member private Read_T_MediaType ( elem : XElement ) : T_MediaType = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "PlainFile" ->
            U_PlainFile( ReaderWriter.Read_T_PlainFile firstChild )
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
            FileName =
                elem.Element( XName.Get "FileName" ).Value;
            FileSize =
                Int64.Parse( elem.Element( XName.Get "FileSize" ).Value );
        }

    /// <summary>
    ///  Read T_KillInitMediaProc data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_KillInitMediaProc data structure.
    /// </returns>
    static member private Read_T_KillInitMediaProc ( elem : XElement ) : T_KillInitMediaProc = 
        {
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
            ProcID =
                UInt64.Parse( elem.Element( XName.Get "ProcID" ).Value );
        }

    /// <summary>
    ///  Write HarukaCtrlerCtrlReq data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_HarukaCtrlerCtrlReq ) : unit =
        let s = ReaderWriter.T_HarukaCtrlerCtrlReq_toString 0 2 d "HarukaCtrlerCtrlReq"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert HarukaCtrlerCtrlReq data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_HarukaCtrlerCtrlReq ) : string =
        ReaderWriter.T_HarukaCtrlerCtrlReq_toString 0 0 d "HarukaCtrlerCtrlReq"
        |> String.Concat

    /// <summary>
    ///  Write T_HarukaCtrlerCtrlReq data structure to configuration file.
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
    static member private T_HarukaCtrlerCtrlReq_toString ( indent : int ) ( indentStep : int ) ( elem : T_HarukaCtrlerCtrlReq ) ( elemName : string ) : seq<string> = 
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
            | U_Login( x ) ->
                yield sprintf "%s%s<Login>%b</Login>" singleIndent indentStr (x)
            | U_Logout( x ) ->
                let work = x
                yield sprintf "%s%s<Logout>%s</Logout>" singleIndent indentStr (work.ToString()) 
            | U_NoOperation( x ) ->
                let work = x
                yield sprintf "%s%s<NoOperation>%s</NoOperation>" singleIndent indentStr (work.ToString()) 
            | U_GetControllerConfig( x ) ->
                let work = x
                yield sprintf "%s%s<GetControllerConfig>%s</GetControllerConfig>" singleIndent indentStr (work.ToString()) 
            | U_SetControllerConfig( x ) ->
                yield! ReaderWriter.T_SetControllerConfig_toString ( indent + 1 ) indentStep ( x ) "SetControllerConfig"
            | U_GetTargetDeviceDir( x ) ->
                let work = x
                yield sprintf "%s%s<GetTargetDeviceDir>%s</GetTargetDeviceDir>" singleIndent indentStr (work.ToString()) 
            | U_CreateTargetDeviceDir( x ) ->
                yield! ReaderWriter.T_CreateTargetDeviceDir_toString ( indent + 1 ) indentStep ( x ) "CreateTargetDeviceDir"
            | U_DeleteTargetDeviceDir( x ) ->
                yield! ReaderWriter.T_DeleteTargetDeviceDir_toString ( indent + 1 ) indentStep ( x ) "DeleteTargetDeviceDir"
            | U_GetTargetDeviceConfig( x ) ->
                yield! ReaderWriter.T_GetTargetDeviceConfig_toString ( indent + 1 ) indentStep ( x ) "GetTargetDeviceConfig"
            | U_CreateTargetDeviceConfig( x ) ->
                yield! ReaderWriter.T_CreateTargetDeviceConfig_toString ( indent + 1 ) indentStep ( x ) "CreateTargetDeviceConfig"
            | U_GetTargetGroupID( x ) ->
                yield! ReaderWriter.T_GetTargetGroupID_toString ( indent + 1 ) indentStep ( x ) "GetTargetGroupID"
            | U_GetTargetGroupConfig( x ) ->
                yield! ReaderWriter.T_GetTargetGroupConfig_toString ( indent + 1 ) indentStep ( x ) "GetTargetGroupConfig"
            | U_GetAllTargetGroupConfig( x ) ->
                yield! ReaderWriter.T_GetAllTargetGroupConfig_toString ( indent + 1 ) indentStep ( x ) "GetAllTargetGroupConfig"
            | U_CreateTargetGroupConfig( x ) ->
                yield! ReaderWriter.T_CreateTargetGroupConfig_toString ( indent + 1 ) indentStep ( x ) "CreateTargetGroupConfig"
            | U_DeleteTargetGroupConfig( x ) ->
                yield! ReaderWriter.T_DeleteTargetGroupConfig_toString ( indent + 1 ) indentStep ( x ) "DeleteTargetGroupConfig"
            | U_GetLUWorkDir( x ) ->
                yield! ReaderWriter.T_GetLUWorkDir_toString ( indent + 1 ) indentStep ( x ) "GetLUWorkDir"
            | U_CreateLUWorkDir( x ) ->
                yield! ReaderWriter.T_CreateLUWorkDir_toString ( indent + 1 ) indentStep ( x ) "CreateLUWorkDir"
            | U_DeleteLUWorkDir( x ) ->
                yield! ReaderWriter.T_DeleteLUWorkDir_toString ( indent + 1 ) indentStep ( x ) "DeleteLUWorkDir"
            | U_GetTargetDeviceProcs( x ) ->
                let work = x
                yield sprintf "%s%s<GetTargetDeviceProcs>%s</GetTargetDeviceProcs>" singleIndent indentStr (work.ToString()) 
            | U_KillTargetDeviceProc( x ) ->
                yield! ReaderWriter.T_KillTargetDeviceProc_toString ( indent + 1 ) indentStep ( x ) "KillTargetDeviceProc"
            | U_StartTargetDeviceProc( x ) ->
                yield! ReaderWriter.T_StartTargetDeviceProc_toString ( indent + 1 ) indentStep ( x ) "StartTargetDeviceProc"
            | U_TargetDeviceCtrlRequest( x ) ->
                yield! ReaderWriter.T_TargetDeviceCtrlRequest_toString ( indent + 1 ) indentStep ( x ) "TargetDeviceCtrlRequest"
            | U_CreateMediaFile( x ) ->
                yield! ReaderWriter.T_CreateMediaFile_toString ( indent + 1 ) indentStep ( x ) "CreateMediaFile"
            | U_GetInitMediaStatus( x ) ->
                let work = x
                yield sprintf "%s%s<GetInitMediaStatus>%s</GetInitMediaStatus>" singleIndent indentStr (work.ToString()) 
            | U_KillInitMediaProc( x ) ->
                yield! ReaderWriter.T_KillInitMediaProc_toString ( indent + 1 ) indentStep ( x ) "KillInitMediaProc"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_SetControllerConfig data structure to configuration file.
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
    static member private T_SetControllerConfig_toString ( indent : int ) ( indentStep : int ) ( elem : T_SetControllerConfig ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<Config>%s</Config>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Config) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_CreateTargetDeviceDir data structure to configuration file.
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
    static member private T_CreateTargetDeviceDir_toString ( indent : int ) ( indentStep : int ) ( elem : T_CreateTargetDeviceDir ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_DeleteTargetDeviceDir data structure to configuration file.
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
    static member private T_DeleteTargetDeviceDir_toString ( indent : int ) ( indentStep : int ) ( elem : T_DeleteTargetDeviceDir ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_GetTargetDeviceConfig data structure to configuration file.
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
    static member private T_GetTargetDeviceConfig_toString ( indent : int ) ( indentStep : int ) ( elem : T_GetTargetDeviceConfig ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_CreateTargetDeviceConfig data structure to configuration file.
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
    static member private T_CreateTargetDeviceConfig_toString ( indent : int ) ( indentStep : int ) ( elem : T_CreateTargetDeviceConfig ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s%s<Config>%s</Config>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Config) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_GetTargetGroupID data structure to configuration file.
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
    static member private T_GetTargetGroupID_toString ( indent : int ) ( indentStep : int ) ( elem : T_GetTargetGroupID ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_GetTargetGroupConfig data structure to configuration file.
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
    static member private T_GetTargetGroupConfig_toString ( indent : int ) ( indentStep : int ) ( elem : T_GetTargetGroupConfig ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s%s<TargetGroupID>%s</TargetGroupID>" singleIndent indentStr ( tgid_me.toString (elem.TargetGroupID) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_GetAllTargetGroupConfig data structure to configuration file.
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
    static member private T_GetAllTargetGroupConfig_toString ( indent : int ) ( indentStep : int ) ( elem : T_GetAllTargetGroupConfig ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_CreateTargetGroupConfig data structure to configuration file.
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
    static member private T_CreateTargetGroupConfig_toString ( indent : int ) ( indentStep : int ) ( elem : T_CreateTargetGroupConfig ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s%s<TargetGroupID>%s</TargetGroupID>" singleIndent indentStr ( tgid_me.toString (elem.TargetGroupID) )
            yield sprintf "%s%s<Config>%s</Config>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Config) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_DeleteTargetGroupConfig data structure to configuration file.
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
    static member private T_DeleteTargetGroupConfig_toString ( indent : int ) ( indentStep : int ) ( elem : T_DeleteTargetGroupConfig ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s%s<TargetGroupID>%s</TargetGroupID>" singleIndent indentStr ( tgid_me.toString (elem.TargetGroupID) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_GetLUWorkDir data structure to configuration file.
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
    static member private T_GetLUWorkDir_toString ( indent : int ) ( indentStep : int ) ( elem : T_GetLUWorkDir ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_CreateLUWorkDir data structure to configuration file.
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
    static member private T_CreateLUWorkDir_toString ( indent : int ) ( indentStep : int ) ( elem : T_CreateLUWorkDir ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            if lun_me.toPrim (elem.LUN) < 0UL then
                raise <| ConfRWException( "Min value(LUN_T) restriction error. LUN" )
            if lun_me.toPrim (elem.LUN) > 255UL then
                raise <| ConfRWException( "Max value(LUN_T) restriction error. LUN" )
            yield sprintf "%s%s<LUN>%s</LUN>" singleIndent indentStr ( lun_me.toString (elem.LUN) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_DeleteLUWorkDir data structure to configuration file.
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
    static member private T_DeleteLUWorkDir_toString ( indent : int ) ( indentStep : int ) ( elem : T_DeleteLUWorkDir ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            if lun_me.toPrim (elem.LUN) < 0UL then
                raise <| ConfRWException( "Min value(LUN_T) restriction error. LUN" )
            if lun_me.toPrim (elem.LUN) > 255UL then
                raise <| ConfRWException( "Max value(LUN_T) restriction error. LUN" )
            yield sprintf "%s%s<LUN>%s</LUN>" singleIndent indentStr ( lun_me.toString (elem.LUN) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_KillTargetDeviceProc data structure to configuration file.
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
    static member private T_KillTargetDeviceProc_toString ( indent : int ) ( indentStep : int ) ( elem : T_KillTargetDeviceProc ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_StartTargetDeviceProc data structure to configuration file.
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
    static member private T_StartTargetDeviceProc_toString ( indent : int ) ( indentStep : int ) ( elem : T_StartTargetDeviceProc ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_TargetDeviceCtrlRequest data structure to configuration file.
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
    static member private T_TargetDeviceCtrlRequest_toString ( indent : int ) ( indentStep : int ) ( elem : T_TargetDeviceCtrlRequest ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s%s<Request>%s</Request>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Request) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_CreateMediaFile data structure to configuration file.
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
    static member private T_CreateMediaFile_toString ( indent : int ) ( indentStep : int ) ( elem : T_CreateMediaFile ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield! ReaderWriter.T_MediaType_toString ( indent + 1 ) indentStep ( elem.MediaType ) "MediaType"
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_MediaType data structure to configuration file.
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
    static member private T_MediaType_toString ( indent : int ) ( indentStep : int ) ( elem : T_MediaType ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_PlainFile( x ) ->
                yield! ReaderWriter.T_PlainFile_toString ( indent + 1 ) indentStep ( x ) "PlainFile"
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
            if (elem.FileName).Length < 1 then
                raise <| ConfRWException( "Min value(string) restriction error. FileName" )
            if (elem.FileName).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. FileName" )
            yield sprintf "%s%s<FileName>%s</FileName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.FileName) )
            if (elem.FileSize) < 1L then
                raise <| ConfRWException( "Min value(long) restriction error. FileSize" )
            yield sprintf "%s%s<FileSize>%d</FileSize>" singleIndent indentStr (elem.FileSize)
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_KillInitMediaProc data structure to configuration file.
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
    static member private T_KillInitMediaProc_toString ( indent : int ) ( indentStep : int ) ( elem : T_KillInitMediaProc ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s%s<ProcID>%d</ProcID>" singleIndent indentStr (elem.ProcID)
            yield sprintf "%s</%s>" indentStr elemName
        }


