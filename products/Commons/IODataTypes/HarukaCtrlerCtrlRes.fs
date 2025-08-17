//=============================================================================
// Haruka Software Storage.
// Definition of ReaderWriter configuration reader/writer function.

namespace Haruka.IODataTypes.HarukaCtrlerCtrlRes

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Xml
open System.Xml.Schema
open System.Xml.Linq
open Haruka.Constants

type [<NoComparison>]T_HarukaCtrlerCtrlRes = {
    Response : T_Response;
}

and [<NoComparison>]T_Response = 
    | U_LoginResult of T_LoginResult
    | U_LogoutResult of T_LogoutResult
    | U_NoOperationResult of T_NoOperationResult
    | U_ControllerConfig of T_ControllerConfig
    | U_SetControllerConfigResult of T_SetControllerConfigResult
    | U_TargetDeviceDirs of T_TargetDeviceDirs
    | U_CreateTargetDeviceDirResult of T_TDReqResult
    | U_DeleteTargetDeviceDirResult of T_TDReqResult
    | U_TargetDeviceConfig of T_TargetDeviceConfig
    | U_CreateTargetDeviceConfigResult of T_TDReqResult
    | U_TargetGroupID of T_TargetGroupID
    | U_TargetGroupConfig of T_TargetGroupConfig
    | U_AllTargetGroupConfig of T_AllTargetGroupConfig
    | U_CreateTargetGroupConfigResult of T_CreateTargetGroupConfigResult
    | U_DeleteTargetGroupConfigResult of T_DeleteTargetGroupConfigResult
    | U_LUWorkDirs of T_LUWorkDirs
    | U_CreateLUWorkDirResult of T_CreateLUWorkDirResult
    | U_DeleteLUWorkDirResult of T_DeleteLUWorkDirResult
    | U_TargetDeviceProcs of T_TargetDeviceProcs
    | U_KillTargetDeviceProcResult of T_TDReqResult
    | U_StartTargetDeviceProcResult of T_TDReqResult
    | U_TargetDeviceCtrlResponse of T_TargetDeviceCtrlResponse
    | U_CreateMediaFileResult of T_CreateMediaFileResult
    | U_InitMediaStatus of T_InitMediaStatus
    | U_KillInitMediaProcResult of T_KillInitMediaProcResult
    | U_UnexpectedError of string

and [<NoComparison>]T_LoginResult = {
    Result : bool;
    SessionID : CtrlSessionID;
}

and [<NoComparison>]T_LogoutResult = {
    Result : bool;
    SessionID : CtrlSessionID;
}

and [<NoComparison>]T_NoOperationResult = {
    Result : bool;
    SessionID : CtrlSessionID;
}

and [<NoComparison>]T_ControllerConfig = {
    Config : string;
    ErrorMessage : string;
}

and [<NoComparison>]T_SetControllerConfigResult = {
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_TargetDeviceDirs = {
    TargetDeviceID : TDID_T list;
    ErrorMessage : string;
}

and [<NoComparison>]T_TargetDeviceConfig = {
    TargetDeviceID : TDID_T;
    Config : string;
    ErrorMessage : string;
}

and [<NoComparison>]T_TargetGroupID = {
    TargetDeviceID : TDID_T;
    TargetGroupID : TGID_T list;
    ErrorMessage : string;
}

and [<NoComparison>]T_TargetGroupConfig = {
    TargetDeviceID : TDID_T;
    TargetGroupID : TGID_T;
    Config : string;
    ErrorMessage : string;
}

and [<NoComparison>]T_AllTargetGroupConfig = {
    TargetDeviceID : TDID_T;
    TargetGroup : T_TargetGroup list;
    ErrorMessage : string;
}

and [<NoComparison>]T_TargetGroup = {
    TargetGroupID : TGID_T;
    Config : string;
}

and [<NoComparison>]T_CreateTargetGroupConfigResult = {
    TargetDeviceID : TDID_T;
    TargetGroupID : TGID_T;
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_DeleteTargetGroupConfigResult = {
    TargetDeviceID : TDID_T;
    TargetGroupID : TGID_T;
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_LUWorkDirs = {
    TargetDeviceID : TDID_T;
    Name : LUN_T list;
    ErrorMessage : string;
}

and [<NoComparison>]T_CreateLUWorkDirResult = {
    TargetDeviceID : TDID_T;
    LUN : LUN_T;
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_DeleteLUWorkDirResult = {
    TargetDeviceID : TDID_T;
    LUN : LUN_T;
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_TargetDeviceProcs = {
    TargetDeviceID : TDID_T list;
    ErrorMessage : string;
}

and [<NoComparison>]T_TargetDeviceCtrlResponse = {
    TargetDeviceID : TDID_T;
    Response : string;
    ErrorMessage : string;
}

and [<NoComparison>]T_CreateMediaFileResult = {
    Result : bool;
    ProcID : uint64;
    ErrorMessage : string;
}

and [<NoComparison>]T_InitMediaStatus = {
    Procs : T_Procs list;
    ErrorMessage : string;
}

and [<NoComparison>]T_Procs = {
    ProcID : uint64;
    PathName : string;
    FileType : string;
    Status : T_Status;
    ErrorMessage : string list;
}

and [<NoComparison>]T_Status = 
    | U_NotStarted of unit
    | U_ProgressCreation of uint8
    | U_Recovery of uint8
    | U_NormalEnd of unit
    | U_AbnormalEnd of unit

and [<NoComparison>]T_KillInitMediaProcResult = {
    Result : bool;
    ErrorMessage : string;
}

and [<NoComparison>]T_TDReqResult = {
    TargetDeviceID : TDID_T;
    Result : bool;
    ErrorMessage : string;
}

///  ReaderWriter class imprements read and write function of configuration.
type ReaderWriter() =

/// XSD data for validate input XML document.
    static let xsd = "<?xml version='1.0' encoding='UTF-8'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:element name='HarukaCtrlerCtrlRes' >
    <xsd:complexType><xsd:sequence>
      <xsd:element name='Response' >
        <xsd:complexType><xsd:choice>
          <xsd:element name='LoginResult' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='Result' >
                <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
              </xsd:element>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='LogoutResult' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='Result' >
                <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
              </xsd:element>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='NoOperationResult' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='Result' >
                <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
              </xsd:element>
              <xsd:element name='SessionID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^CSI_[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='ControllerConfig' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='Config' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='SetControllerConfigResult' >
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
          <xsd:element name='TargetDeviceDirs' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='TargetDeviceID' minOccurs='0' maxOccurs='16' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='CreateTargetDeviceDirResult' type='TDReqResult' ></xsd:element>
          <xsd:element name='DeleteTargetDeviceDirResult' type='TDReqResult' ></xsd:element>
          <xsd:element name='TargetDeviceConfig' >
            <xsd:complexType><xsd:sequence>
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
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='CreateTargetDeviceConfigResult' type='TDReqResult' ></xsd:element>
          <xsd:element name='TargetGroupID' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetGroupID' minOccurs='0' maxOccurs='255' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TG_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='TargetGroupConfig' >
            <xsd:complexType><xsd:sequence>
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
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='AllTargetGroupConfig' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='TargetGroup' minOccurs='0' maxOccurs='255' >
                <xsd:complexType><xsd:sequence>
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
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='CreateTargetGroupConfigResult' >
            <xsd:complexType><xsd:sequence>
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
          <xsd:element name='DeleteTargetGroupConfigResult' >
            <xsd:complexType><xsd:sequence>
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
          <xsd:element name='LUWorkDirs' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='Name' minOccurs='0' maxOccurs='255' >
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
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='CreateLUWorkDirResult' >
            <xsd:complexType><xsd:sequence>
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
          <xsd:element name='DeleteLUWorkDirResult' >
            <xsd:complexType><xsd:sequence>
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
          <xsd:element name='TargetDeviceProcs' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='TargetDeviceID' minOccurs='0' maxOccurs='16' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='KillTargetDeviceProcResult' type='TDReqResult' ></xsd:element>
          <xsd:element name='StartTargetDeviceProcResult' type='TDReqResult' ></xsd:element>
          <xsd:element name='TargetDeviceCtrlResponse' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='TargetDeviceID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                    <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='Response' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='CreateMediaFileResult' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='Result' >
                <xsd:simpleType><xsd:restriction base='xsd:boolean' /></xsd:simpleType>
              </xsd:element>
              <xsd:element name='ProcID' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:unsignedLong'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='InitMediaStatus' >
            <xsd:complexType><xsd:sequence>
              <xsd:element name='Procs' minOccurs='0' maxOccurs='4' >
                <xsd:complexType><xsd:sequence>
                  <xsd:element name='ProcID' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:unsignedLong'>
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='PathName' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:string'>
                        <xsd:minLength value='0' />
                        <xsd:maxLength value='256' />
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='FileType' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:string'>
                        <xsd:minLength value='0' />
                        <xsd:maxLength value='32' />
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                  <xsd:element name='Status' >
                    <xsd:complexType><xsd:choice>
                      <xsd:element name='NotStarted' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:int'>
                            <xsd:minInclusive value='0' />
                            <xsd:maxInclusive value='0' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='ProgressCreation' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedByte'>
                            <xsd:minInclusive value='0' />
                            <xsd:maxInclusive value='100' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='Recovery' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:unsignedByte'>
                            <xsd:minInclusive value='0' />
                            <xsd:maxInclusive value='100' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='NormalEnd' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:int'>
                            <xsd:minInclusive value='0' />
                            <xsd:maxInclusive value='0' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                      <xsd:element name='AbnormalEnd' >
                        <xsd:simpleType>
                          <xsd:restriction base='xsd:int'>
                            <xsd:minInclusive value='0' />
                            <xsd:maxInclusive value='0' />
                          </xsd:restriction>
                        </xsd:simpleType>
                      </xsd:element>
                    </xsd:choice></xsd:complexType>
                  </xsd:element>
                  <xsd:element name='ErrorMessage' minOccurs='0' maxOccurs='16' >
                    <xsd:simpleType>
                      <xsd:restriction base='xsd:string'>
                        <xsd:minLength value='0' />
                        <xsd:maxLength value='256' />
                      </xsd:restriction>
                    </xsd:simpleType>
                  </xsd:element>
                </xsd:sequence></xsd:complexType>
              </xsd:element>
              <xsd:element name='ErrorMessage' >
                <xsd:simpleType>
                  <xsd:restriction base='xsd:string'>
                  </xsd:restriction>
                </xsd:simpleType>
              </xsd:element>
            </xsd:sequence></xsd:complexType>
          </xsd:element>
          <xsd:element name='KillInitMediaProcResult' >
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
  <xsd:complexType name='TDReqResult'>
    <xsd:sequence>
      <xsd:element name='TargetDeviceID' >
        <xsd:simpleType>
          <xsd:restriction base='xsd:string'>
            <xsd:pattern value='^TD_[0-9a-fA-F]{8}$' />
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
    ///  Load HarukaCtrlerCtrlRes data from specified file.
    /// </summary>
    /// <param name="fname">
    ///  Configuration file name.
    /// </param>
    /// <returns>
    ///  Loaded HarukaCtrlerCtrlRes data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadFile ( fname : string ) : T_HarukaCtrlerCtrlRes =
        fname |> File.ReadAllText |> ReaderWriter.LoadString

    /// <summary>
    ///  Load HarukaCtrlerCtrlRes data from specified string.
    /// </summary>
    /// <param name="s">
    ///  XML string
    /// </param>
    /// <returns>
    ///  Loaded HarukaCtrlerCtrlRes data structures.
    /// </returns>
    /// <remarks>
    ///  If it failed to load configuration, an exception will be raised.
    /// </remarks>
    static member LoadString ( s : string ) : T_HarukaCtrlerCtrlRes =
        let confSchemaSet = schemaSet.Value
        let xdoc =
            use ms = new MemoryStream( Encoding.GetEncoding( "utf-8" ).GetBytes s, false )
            XDocument.Load ms
        xdoc.Validate( confSchemaSet, fun _ argEx -> raise argEx.Exception )
        "HarukaCtrlerCtrlRes" |> xdoc.Element |> ReaderWriter.Read_T_HarukaCtrlerCtrlRes

    /// <summary>
    ///  Read T_HarukaCtrlerCtrlRes data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_HarukaCtrlerCtrlRes data structure.
    /// </returns>
    static member private Read_T_HarukaCtrlerCtrlRes ( elem : XElement ) : T_HarukaCtrlerCtrlRes = 
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
        | "LoginResult" ->
            U_LoginResult( ReaderWriter.Read_T_LoginResult firstChild )
        | "LogoutResult" ->
            U_LogoutResult( ReaderWriter.Read_T_LogoutResult firstChild )
        | "NoOperationResult" ->
            U_NoOperationResult( ReaderWriter.Read_T_NoOperationResult firstChild )
        | "ControllerConfig" ->
            U_ControllerConfig( ReaderWriter.Read_T_ControllerConfig firstChild )
        | "SetControllerConfigResult" ->
            U_SetControllerConfigResult( ReaderWriter.Read_T_SetControllerConfigResult firstChild )
        | "TargetDeviceDirs" ->
            U_TargetDeviceDirs( ReaderWriter.Read_T_TargetDeviceDirs firstChild )
        | "CreateTargetDeviceDirResult" ->
            U_CreateTargetDeviceDirResult( ReaderWriter.Read_T_TDReqResult( firstChild ) )
        | "DeleteTargetDeviceDirResult" ->
            U_DeleteTargetDeviceDirResult( ReaderWriter.Read_T_TDReqResult( firstChild ) )
        | "TargetDeviceConfig" ->
            U_TargetDeviceConfig( ReaderWriter.Read_T_TargetDeviceConfig firstChild )
        | "CreateTargetDeviceConfigResult" ->
            U_CreateTargetDeviceConfigResult( ReaderWriter.Read_T_TDReqResult( firstChild ) )
        | "TargetGroupID" ->
            U_TargetGroupID( ReaderWriter.Read_T_TargetGroupID firstChild )
        | "TargetGroupConfig" ->
            U_TargetGroupConfig( ReaderWriter.Read_T_TargetGroupConfig firstChild )
        | "AllTargetGroupConfig" ->
            U_AllTargetGroupConfig( ReaderWriter.Read_T_AllTargetGroupConfig firstChild )
        | "CreateTargetGroupConfigResult" ->
            U_CreateTargetGroupConfigResult( ReaderWriter.Read_T_CreateTargetGroupConfigResult firstChild )
        | "DeleteTargetGroupConfigResult" ->
            U_DeleteTargetGroupConfigResult( ReaderWriter.Read_T_DeleteTargetGroupConfigResult firstChild )
        | "LUWorkDirs" ->
            U_LUWorkDirs( ReaderWriter.Read_T_LUWorkDirs firstChild )
        | "CreateLUWorkDirResult" ->
            U_CreateLUWorkDirResult( ReaderWriter.Read_T_CreateLUWorkDirResult firstChild )
        | "DeleteLUWorkDirResult" ->
            U_DeleteLUWorkDirResult( ReaderWriter.Read_T_DeleteLUWorkDirResult firstChild )
        | "TargetDeviceProcs" ->
            U_TargetDeviceProcs( ReaderWriter.Read_T_TargetDeviceProcs firstChild )
        | "KillTargetDeviceProcResult" ->
            U_KillTargetDeviceProcResult( ReaderWriter.Read_T_TDReqResult( firstChild ) )
        | "StartTargetDeviceProcResult" ->
            U_StartTargetDeviceProcResult( ReaderWriter.Read_T_TDReqResult( firstChild ) )
        | "TargetDeviceCtrlResponse" ->
            U_TargetDeviceCtrlResponse( ReaderWriter.Read_T_TargetDeviceCtrlResponse firstChild )
        | "CreateMediaFileResult" ->
            U_CreateMediaFileResult( ReaderWriter.Read_T_CreateMediaFileResult firstChild )
        | "InitMediaStatus" ->
            U_InitMediaStatus( ReaderWriter.Read_T_InitMediaStatus firstChild )
        | "KillInitMediaProcResult" ->
            U_KillInitMediaProcResult( ReaderWriter.Read_T_KillInitMediaProcResult firstChild )
        | "UnexpectedError" ->
            U_UnexpectedError( firstChild.Value )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_LoginResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LoginResult data structure.
    /// </returns>
    static member private Read_T_LoginResult ( elem : XElement ) : T_LoginResult = 
        {
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
        }

    /// <summary>
    ///  Read T_LogoutResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LogoutResult data structure.
    /// </returns>
    static member private Read_T_LogoutResult ( elem : XElement ) : T_LogoutResult = 
        {
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
        }

    /// <summary>
    ///  Read T_NoOperationResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_NoOperationResult data structure.
    /// </returns>
    static member private Read_T_NoOperationResult ( elem : XElement ) : T_NoOperationResult = 
        {
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            SessionID =
                new CtrlSessionID( elem.Element( XName.Get "SessionID" ).Value );
        }

    /// <summary>
    ///  Read T_ControllerConfig data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_ControllerConfig data structure.
    /// </returns>
    static member private Read_T_ControllerConfig ( elem : XElement ) : T_ControllerConfig = 
        {
            Config =
                elem.Element( XName.Get "Config" ).Value;
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_SetControllerConfigResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_SetControllerConfigResult data structure.
    /// </returns>
    static member private Read_T_SetControllerConfigResult ( elem : XElement ) : T_SetControllerConfigResult = 
        {
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_TargetDeviceDirs data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TargetDeviceDirs data structure.
    /// </returns>
    static member private Read_T_TargetDeviceDirs ( elem : XElement ) : T_TargetDeviceDirs = 
        {
            TargetDeviceID =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "TargetDeviceID" )
                |> Seq.map ( fun itr -> tdid_me.fromString( itr.Value ) )
                |> Seq.toList
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_TargetDeviceConfig data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TargetDeviceConfig data structure.
    /// </returns>
    static member private Read_T_TargetDeviceConfig ( elem : XElement ) : T_TargetDeviceConfig = 
        {
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            Config =
                elem.Element( XName.Get "Config" ).Value;
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_TargetGroupID data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TargetGroupID data structure.
    /// </returns>
    static member private Read_T_TargetGroupID ( elem : XElement ) : T_TargetGroupID = 
        {
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            TargetGroupID =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "TargetGroupID" )
                |> Seq.map ( fun itr -> tgid_me.fromString( itr.Value ) )
                |> Seq.toList
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_TargetGroupConfig data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TargetGroupConfig data structure.
    /// </returns>
    static member private Read_T_TargetGroupConfig ( elem : XElement ) : T_TargetGroupConfig = 
        {
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            TargetGroupID =
                tgid_me.fromString( elem.Element( XName.Get "TargetGroupID" ).Value );
            Config =
                elem.Element( XName.Get "Config" ).Value;
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_AllTargetGroupConfig data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_AllTargetGroupConfig data structure.
    /// </returns>
    static member private Read_T_AllTargetGroupConfig ( elem : XElement ) : T_AllTargetGroupConfig = 
        {
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            TargetGroup =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "TargetGroup" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_TargetGroup itr )
                |> Seq.toList
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

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
            Config =
                elem.Element( XName.Get "Config" ).Value;
        }

    /// <summary>
    ///  Read T_CreateTargetGroupConfigResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_CreateTargetGroupConfigResult data structure.
    /// </returns>
    static member private Read_T_CreateTargetGroupConfigResult ( elem : XElement ) : T_CreateTargetGroupConfigResult = 
        {
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            TargetGroupID =
                tgid_me.fromString( elem.Element( XName.Get "TargetGroupID" ).Value );
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_DeleteTargetGroupConfigResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_DeleteTargetGroupConfigResult data structure.
    /// </returns>
    static member private Read_T_DeleteTargetGroupConfigResult ( elem : XElement ) : T_DeleteTargetGroupConfigResult = 
        {
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            TargetGroupID =
                tgid_me.fromString( elem.Element( XName.Get "TargetGroupID" ).Value );
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_LUWorkDirs data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_LUWorkDirs data structure.
    /// </returns>
    static member private Read_T_LUWorkDirs ( elem : XElement ) : T_LUWorkDirs = 
        {
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            Name =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Name" )
                |> Seq.map ( fun itr -> lun_me.fromStringValue( itr.Value ) )
                |> Seq.toList
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_CreateLUWorkDirResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_CreateLUWorkDirResult data structure.
    /// </returns>
    static member private Read_T_CreateLUWorkDirResult ( elem : XElement ) : T_CreateLUWorkDirResult = 
        {
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            LUN =
                lun_me.fromStringValue( elem.Element( XName.Get "LUN" ).Value );
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_DeleteLUWorkDirResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_DeleteLUWorkDirResult data structure.
    /// </returns>
    static member private Read_T_DeleteLUWorkDirResult ( elem : XElement ) : T_DeleteLUWorkDirResult = 
        {
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            LUN =
                lun_me.fromStringValue( elem.Element( XName.Get "LUN" ).Value );
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_TargetDeviceProcs data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TargetDeviceProcs data structure.
    /// </returns>
    static member private Read_T_TargetDeviceProcs ( elem : XElement ) : T_TargetDeviceProcs = 
        {
            TargetDeviceID =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "TargetDeviceID" )
                |> Seq.map ( fun itr -> tdid_me.fromString( itr.Value ) )
                |> Seq.toList
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_TargetDeviceCtrlResponse data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TargetDeviceCtrlResponse data structure.
    /// </returns>
    static member private Read_T_TargetDeviceCtrlResponse ( elem : XElement ) : T_TargetDeviceCtrlResponse = 
        {
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            Response =
                elem.Element( XName.Get "Response" ).Value;
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_CreateMediaFileResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_CreateMediaFileResult data structure.
    /// </returns>
    static member private Read_T_CreateMediaFileResult ( elem : XElement ) : T_CreateMediaFileResult = 
        {
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ProcID =
                UInt64.Parse( elem.Element( XName.Get "ProcID" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_InitMediaStatus data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_InitMediaStatus data structure.
    /// </returns>
    static member private Read_T_InitMediaStatus ( elem : XElement ) : T_InitMediaStatus = 
        {
            Procs =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "Procs" )
                |> Seq.map ( fun itr -> ReaderWriter.Read_T_Procs itr )
                |> Seq.toList
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_Procs data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Procs data structure.
    /// </returns>
    static member private Read_T_Procs ( elem : XElement ) : T_Procs = 
        {
            ProcID =
                UInt64.Parse( elem.Element( XName.Get "ProcID" ).Value );
            PathName =
                elem.Element( XName.Get "PathName" ).Value;
            FileType =
                elem.Element( XName.Get "FileType" ).Value;
            Status =
                ReaderWriter.Read_T_Status( elem.Element( XName.Get "Status" ) );
            ErrorMessage =
                elem.Elements()
                |> Seq.filter ( fun itr -> itr.Name = XName.Get "ErrorMessage" )
                |> Seq.map ( fun itr -> itr.Value )
                |> Seq.toList
        }

    /// <summary>
    ///  Read T_Status data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_Status data structure.
    /// </returns>
    static member private Read_T_Status ( elem : XElement ) : T_Status = 
        let firstChild = elem.Elements() |> Seq.head 
        let firstChildName = firstChild.Name
        match firstChildName.LocalName with
        | "NotStarted" ->
            U_NotStarted( () )
        | "ProgressCreation" ->
            U_ProgressCreation( Byte.Parse( firstChild.Value ) )
        | "Recovery" ->
            U_Recovery( Byte.Parse( firstChild.Value ) )
        | "NormalEnd" ->
            U_NormalEnd( () )
        | "AbnormalEnd" ->
            U_AbnormalEnd( () )
        | _ -> raise <| ConfRWException( "Unexpected tag name." )

    /// <summary>
    ///  Read T_KillInitMediaProcResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_KillInitMediaProcResult data structure.
    /// </returns>
    static member private Read_T_KillInitMediaProcResult ( elem : XElement ) : T_KillInitMediaProcResult = 
        {
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Read T_TDReqResult data from XML document.
    /// </summary>
    /// <param name="elem">
    ///  Loaded XML document.
    /// </param>
    /// <returns>
    ///  parsed T_TDReqResult data structure.
    /// </returns>
    static member private Read_T_TDReqResult ( elem : XElement ) : T_TDReqResult = 
        {
            TargetDeviceID =
                tdid_me.fromString( elem.Element( XName.Get "TargetDeviceID" ).Value );
            Result =
                Boolean.Parse( elem.Element( XName.Get "Result" ).Value );
            ErrorMessage =
                elem.Element( XName.Get "ErrorMessage" ).Value;
        }

    /// <summary>
    ///  Write HarukaCtrlerCtrlRes data to specified file.
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
    static member WriteFile ( fname : string ) ( d : T_HarukaCtrlerCtrlRes ) : unit =
        let s = ReaderWriter.T_HarukaCtrlerCtrlRes_toString 0 2 d "HarukaCtrlerCtrlRes"
        File.WriteAllLines( fname, s )

    /// <summary>
    ///  Convert HarukaCtrlerCtrlRes data to string.
    /// </summary>
    /// <param name="d">
    ///  Data to output.
    /// </param>
    /// <returns>
    ///  Converted string
    /// </returns>
    static member ToString ( d : T_HarukaCtrlerCtrlRes ) : string =
        ReaderWriter.T_HarukaCtrlerCtrlRes_toString 0 0 d "HarukaCtrlerCtrlRes"
        |> String.Concat

    /// <summary>
    ///  Write T_HarukaCtrlerCtrlRes data structure to configuration file.
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
    static member private T_HarukaCtrlerCtrlRes_toString ( indent : int ) ( indentStep : int ) ( elem : T_HarukaCtrlerCtrlRes ) ( elemName : string ) : seq<string> = 
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
            | U_LoginResult( x ) ->
                yield! ReaderWriter.T_LoginResult_toString ( indent + 1 ) indentStep ( x ) "LoginResult"
            | U_LogoutResult( x ) ->
                yield! ReaderWriter.T_LogoutResult_toString ( indent + 1 ) indentStep ( x ) "LogoutResult"
            | U_NoOperationResult( x ) ->
                yield! ReaderWriter.T_NoOperationResult_toString ( indent + 1 ) indentStep ( x ) "NoOperationResult"
            | U_ControllerConfig( x ) ->
                yield! ReaderWriter.T_ControllerConfig_toString ( indent + 1 ) indentStep ( x ) "ControllerConfig"
            | U_SetControllerConfigResult( x ) ->
                yield! ReaderWriter.T_SetControllerConfigResult_toString ( indent + 1 ) indentStep ( x ) "SetControllerConfigResult"
            | U_TargetDeviceDirs( x ) ->
                yield! ReaderWriter.T_TargetDeviceDirs_toString ( indent + 1 ) indentStep ( x ) "TargetDeviceDirs"
            | U_CreateTargetDeviceDirResult( x ) ->
                yield! ReaderWriter.T_TDReqResult_toString ( indent + 1 ) indentStep ( x ) "CreateTargetDeviceDirResult"
            | U_DeleteTargetDeviceDirResult( x ) ->
                yield! ReaderWriter.T_TDReqResult_toString ( indent + 1 ) indentStep ( x ) "DeleteTargetDeviceDirResult"
            | U_TargetDeviceConfig( x ) ->
                yield! ReaderWriter.T_TargetDeviceConfig_toString ( indent + 1 ) indentStep ( x ) "TargetDeviceConfig"
            | U_CreateTargetDeviceConfigResult( x ) ->
                yield! ReaderWriter.T_TDReqResult_toString ( indent + 1 ) indentStep ( x ) "CreateTargetDeviceConfigResult"
            | U_TargetGroupID( x ) ->
                yield! ReaderWriter.T_TargetGroupID_toString ( indent + 1 ) indentStep ( x ) "TargetGroupID"
            | U_TargetGroupConfig( x ) ->
                yield! ReaderWriter.T_TargetGroupConfig_toString ( indent + 1 ) indentStep ( x ) "TargetGroupConfig"
            | U_AllTargetGroupConfig( x ) ->
                yield! ReaderWriter.T_AllTargetGroupConfig_toString ( indent + 1 ) indentStep ( x ) "AllTargetGroupConfig"
            | U_CreateTargetGroupConfigResult( x ) ->
                yield! ReaderWriter.T_CreateTargetGroupConfigResult_toString ( indent + 1 ) indentStep ( x ) "CreateTargetGroupConfigResult"
            | U_DeleteTargetGroupConfigResult( x ) ->
                yield! ReaderWriter.T_DeleteTargetGroupConfigResult_toString ( indent + 1 ) indentStep ( x ) "DeleteTargetGroupConfigResult"
            | U_LUWorkDirs( x ) ->
                yield! ReaderWriter.T_LUWorkDirs_toString ( indent + 1 ) indentStep ( x ) "LUWorkDirs"
            | U_CreateLUWorkDirResult( x ) ->
                yield! ReaderWriter.T_CreateLUWorkDirResult_toString ( indent + 1 ) indentStep ( x ) "CreateLUWorkDirResult"
            | U_DeleteLUWorkDirResult( x ) ->
                yield! ReaderWriter.T_DeleteLUWorkDirResult_toString ( indent + 1 ) indentStep ( x ) "DeleteLUWorkDirResult"
            | U_TargetDeviceProcs( x ) ->
                yield! ReaderWriter.T_TargetDeviceProcs_toString ( indent + 1 ) indentStep ( x ) "TargetDeviceProcs"
            | U_KillTargetDeviceProcResult( x ) ->
                yield! ReaderWriter.T_TDReqResult_toString ( indent + 1 ) indentStep ( x ) "KillTargetDeviceProcResult"
            | U_StartTargetDeviceProcResult( x ) ->
                yield! ReaderWriter.T_TDReqResult_toString ( indent + 1 ) indentStep ( x ) "StartTargetDeviceProcResult"
            | U_TargetDeviceCtrlResponse( x ) ->
                yield! ReaderWriter.T_TargetDeviceCtrlResponse_toString ( indent + 1 ) indentStep ( x ) "TargetDeviceCtrlResponse"
            | U_CreateMediaFileResult( x ) ->
                yield! ReaderWriter.T_CreateMediaFileResult_toString ( indent + 1 ) indentStep ( x ) "CreateMediaFileResult"
            | U_InitMediaStatus( x ) ->
                yield! ReaderWriter.T_InitMediaStatus_toString ( indent + 1 ) indentStep ( x ) "InitMediaStatus"
            | U_KillInitMediaProcResult( x ) ->
                yield! ReaderWriter.T_KillInitMediaProcResult_toString ( indent + 1 ) indentStep ( x ) "KillInitMediaProcResult"
            | U_UnexpectedError( x ) ->
                yield sprintf "%s%s<UnexpectedError>%s</UnexpectedError>" singleIndent indentStr ( ReaderWriter.xmlEncode(x) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LoginResult data structure to configuration file.
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
    static member private T_LoginResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_LoginResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LogoutResult data structure to configuration file.
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
    static member private T_LogoutResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_LogoutResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_NoOperationResult data structure to configuration file.
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
    static member private T_NoOperationResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_NoOperationResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            let work = elem.SessionID
            yield sprintf "%s%s<SessionID>%s</SessionID>" singleIndent indentStr (work.ToString()) 
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_ControllerConfig data structure to configuration file.
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
    static member private T_ControllerConfig_toString ( indent : int ) ( indentStep : int ) ( elem : T_ControllerConfig ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Config>%s</Config>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Config) )
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_SetControllerConfigResult data structure to configuration file.
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
    static member private T_SetControllerConfigResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_SetControllerConfigResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_TargetDeviceDirs data structure to configuration file.
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
    static member private T_TargetDeviceDirs_toString ( indent : int ) ( indentStep : int ) ( elem : T_TargetDeviceDirs ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.TargetDeviceID.Length < 0 || elem.TargetDeviceID.Length > 16 then 
                raise <| ConfRWException( "Element count restriction error. TargetDeviceID" )
            for itr in elem.TargetDeviceID do
                yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (itr) )
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_TargetDeviceConfig data structure to configuration file.
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
    static member private T_TargetDeviceConfig_toString ( indent : int ) ( indentStep : int ) ( elem : T_TargetDeviceConfig ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s%s<Config>%s</Config>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Config) )
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_TargetGroupID data structure to configuration file.
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
    static member private T_TargetGroupID_toString ( indent : int ) ( indentStep : int ) ( elem : T_TargetGroupID ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            if elem.TargetGroupID.Length < 0 || elem.TargetGroupID.Length > 255 then 
                raise <| ConfRWException( "Element count restriction error. TargetGroupID" )
            for itr in elem.TargetGroupID do
                yield sprintf "%s%s<TargetGroupID>%s</TargetGroupID>" singleIndent indentStr ( tgid_me.toString (itr) )
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_TargetGroupConfig data structure to configuration file.
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
    static member private T_TargetGroupConfig_toString ( indent : int ) ( indentStep : int ) ( elem : T_TargetGroupConfig ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s%s<TargetGroupID>%s</TargetGroupID>" singleIndent indentStr ( tgid_me.toString (elem.TargetGroupID) )
            yield sprintf "%s%s<Config>%s</Config>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Config) )
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_AllTargetGroupConfig data structure to configuration file.
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
    static member private T_AllTargetGroupConfig_toString ( indent : int ) ( indentStep : int ) ( elem : T_AllTargetGroupConfig ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            if elem.TargetGroup.Length < 0 || elem.TargetGroup.Length > 255 then 
                raise <| ConfRWException( "Element count restriction error. TargetGroup" )
            for itr in elem.TargetGroup do
                yield! ReaderWriter.T_TargetGroup_toString ( indent + 1 ) indentStep itr "TargetGroup"
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

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
            yield sprintf "%s%s<Config>%s</Config>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Config) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_CreateTargetGroupConfigResult data structure to configuration file.
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
    static member private T_CreateTargetGroupConfigResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_CreateTargetGroupConfigResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s%s<TargetGroupID>%s</TargetGroupID>" singleIndent indentStr ( tgid_me.toString (elem.TargetGroupID) )
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_DeleteTargetGroupConfigResult data structure to configuration file.
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
    static member private T_DeleteTargetGroupConfigResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_DeleteTargetGroupConfigResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s%s<TargetGroupID>%s</TargetGroupID>" singleIndent indentStr ( tgid_me.toString (elem.TargetGroupID) )
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_LUWorkDirs data structure to configuration file.
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
    static member private T_LUWorkDirs_toString ( indent : int ) ( indentStep : int ) ( elem : T_LUWorkDirs ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            if elem.Name.Length < 0 || elem.Name.Length > 255 then 
                raise <| ConfRWException( "Element count restriction error. Name" )
            for itr in elem.Name do
                if lun_me.toPrim (itr) < 0UL then
                    raise <| ConfRWException( "Min value(LUN_T) restriction error. Name" )
                if lun_me.toPrim (itr) > 255UL then
                    raise <| ConfRWException( "Max value(LUN_T) restriction error. Name" )
                yield sprintf "%s%s<Name>%s</Name>" singleIndent indentStr ( lun_me.toString (itr) )
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_CreateLUWorkDirResult data structure to configuration file.
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
    static member private T_CreateLUWorkDirResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_CreateLUWorkDirResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
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
    ///  Write T_DeleteLUWorkDirResult data structure to configuration file.
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
    static member private T_DeleteLUWorkDirResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_DeleteLUWorkDirResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
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
    ///  Write T_TargetDeviceProcs data structure to configuration file.
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
    static member private T_TargetDeviceProcs_toString ( indent : int ) ( indentStep : int ) ( elem : T_TargetDeviceProcs ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.TargetDeviceID.Length < 0 || elem.TargetDeviceID.Length > 16 then 
                raise <| ConfRWException( "Element count restriction error. TargetDeviceID" )
            for itr in elem.TargetDeviceID do
                yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (itr) )
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_TargetDeviceCtrlResponse data structure to configuration file.
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
    static member private T_TargetDeviceCtrlResponse_toString ( indent : int ) ( indentStep : int ) ( elem : T_TargetDeviceCtrlResponse ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s%s<Response>%s</Response>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.Response) )
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_CreateMediaFileResult data structure to configuration file.
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
    static member private T_CreateMediaFileResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_CreateMediaFileResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ProcID>%d</ProcID>" singleIndent indentStr (elem.ProcID)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_InitMediaStatus data structure to configuration file.
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
    static member private T_InitMediaStatus_toString ( indent : int ) ( indentStep : int ) ( elem : T_InitMediaStatus ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            if elem.Procs.Length < 0 || elem.Procs.Length > 4 then 
                raise <| ConfRWException( "Element count restriction error. Procs" )
            for itr in elem.Procs do
                yield! ReaderWriter.T_Procs_toString ( indent + 1 ) indentStep itr "Procs"
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Procs data structure to configuration file.
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
    static member private T_Procs_toString ( indent : int ) ( indentStep : int ) ( elem : T_Procs ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<ProcID>%d</ProcID>" singleIndent indentStr (elem.ProcID)
            if (elem.PathName).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. PathName" )
            if (elem.PathName).Length > 256 then
                raise <| ConfRWException( "Max value(string) restriction error. PathName" )
            yield sprintf "%s%s<PathName>%s</PathName>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.PathName) )
            if (elem.FileType).Length < 0 then
                raise <| ConfRWException( "Min value(string) restriction error. FileType" )
            if (elem.FileType).Length > 32 then
                raise <| ConfRWException( "Max value(string) restriction error. FileType" )
            yield sprintf "%s%s<FileType>%s</FileType>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.FileType) )
            yield! ReaderWriter.T_Status_toString ( indent + 1 ) indentStep ( elem.Status ) "Status"
            if elem.ErrorMessage.Length < 0 || elem.ErrorMessage.Length > 16 then 
                raise <| ConfRWException( "Element count restriction error. ErrorMessage" )
            for itr in elem.ErrorMessage do
                if (itr).Length < 0 then
                    raise <| ConfRWException( "Min value(string) restriction error. ErrorMessage" )
                if (itr).Length > 256 then
                    raise <| ConfRWException( "Max value(string) restriction error. ErrorMessage" )
                yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(itr) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_Status data structure to configuration file.
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
    static member private T_Status_toString ( indent : int ) ( indentStep : int ) ( elem : T_Status ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            match elem with
            | U_NotStarted( x ) ->
                yield sprintf "%s%s<NotStarted>0</NotStarted>" singleIndent indentStr
            | U_ProgressCreation( x ) ->
                if (x) < 0uy then
                    raise <| ConfRWException( "Min value(unsignedByte) restriction error. ProgressCreation" )
                if (x) > 100uy then
                    raise <| ConfRWException( "Max value(unsignedByte) restriction error. ProgressCreation" )
                yield sprintf "%s%s<ProgressCreation>%d</ProgressCreation>" singleIndent indentStr (x)
            | U_Recovery( x ) ->
                if (x) < 0uy then
                    raise <| ConfRWException( "Min value(unsignedByte) restriction error. Recovery" )
                if (x) > 100uy then
                    raise <| ConfRWException( "Max value(unsignedByte) restriction error. Recovery" )
                yield sprintf "%s%s<Recovery>%d</Recovery>" singleIndent indentStr (x)
            | U_NormalEnd( x ) ->
                yield sprintf "%s%s<NormalEnd>0</NormalEnd>" singleIndent indentStr
            | U_AbnormalEnd( x ) ->
                yield sprintf "%s%s<AbnormalEnd>0</AbnormalEnd>" singleIndent indentStr
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_KillInitMediaProcResult data structure to configuration file.
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
    static member private T_KillInitMediaProcResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_KillInitMediaProcResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }

    /// <summary>
    ///  Write T_TDReqResult data structure to configuration file.
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
    static member private T_TDReqResult_toString ( indent : int ) ( indentStep : int ) ( elem : T_TDReqResult ) ( elemName : string ) : seq<string> = 
        let indentStr = String.replicate ( indent * indentStep ) " "
        let singleIndent = String.replicate ( indentStep ) " "
        seq {
            yield sprintf "%s<%s>" indentStr elemName
            yield sprintf "%s%s<TargetDeviceID>%s</TargetDeviceID>" singleIndent indentStr ( tdid_me.toString (elem.TargetDeviceID) )
            yield sprintf "%s%s<Result>%b</Result>" singleIndent indentStr (elem.Result)
            yield sprintf "%s%s<ErrorMessage>%s</ErrorMessage>" singleIndent indentStr ( ReaderWriter.xmlEncode(elem.ErrorMessage) )
            yield sprintf "%s</%s>" indentStr elemName
        }


