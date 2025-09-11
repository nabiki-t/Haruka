//=============================================================================
// Haruka Software Storage.
// Constants.fs : It defines constant values used in client.
//

//=============================================================================
// Namespace declaration

namespace Haruka.Client

//=============================================================================
// Import declaration

open System

//=============================================================================
// Class implementation

type ClientConst() =
    let dummy = ()

    /// IConfigureNode.NodeTypeName value for ConfNode_Controller
    static member NODE_TYPE_NAME_Controller = "Controller"

    /// IConfigureNode.NodeTypeName value for ConfNode_TargetDevice
    static member NODE_TYPE_NAME_TargetDevice = "Target Device"

    /// IConfigureNode.NodeTypeName value for ConfNode_NetworkPortal
    static member NODE_TYPE_NAME_NetworkPortal = "Network Portal"

    /// IConfigureNode.NodeTypeName value for ConfNode_TargetGroup
    static member NODE_TYPE_NAME_TargetGroup = "Target Group"

    /// IConfigureNode.NodeTypeName value for ConfNode_Target
    static member NODE_TYPE_NAME_Target = "Target"

    /// IConfigureNode.NodeTypeName value for ConfNode_DummyDeviceLU
    static member NODE_TYPE_NAME_DummyDeviceLU = "Dummy Device LU"

    /// IConfigureNode.NodeTypeName value for ConfNode_BlockDeviceLU
    static member NODE_TYPE_NAME_BlockDeviceLU = "Block Device LU"

    /// IConfigureNode.NodeTypeName value for ConfNode_PlainFileMedia
    static member NODE_TYPE_NAME_PlainFileMedia = "Plain File Media"

    /// IConfigureNode.NodeTypeName value for ConfNode_MemBufferMedia
    static member NODE_TYPE_NAME_MemBufferMedia = "Memory Buffer Media"

    /// IConfigureNode.NodeTypeName value for ConfNode_DummyMedia
    static member NODE_TYPE_NAME_DummyMedia = "Dummy Media"

    /// IConfigureNode.NodeTypeName value for ConfNode_DummyMedia
    static member NODE_TYPE_NAME_DebugMedia = "Debug Media"

    /// Node type name used at IConfigureNode.SortKey value of ConfNode_Controller
    static member SORT_KEY_TYPE_Controller = "000_Controller"

    /// Node type name used at IConfigureNode.SortKey value of ConfNode_TargetDevice
    static member SORT_KEY_TYPE_TargetDevice = "001_TargetDevice"

    /// Node type name used at IConfigureNode.SortKey value of ConfNode_NetworkPortal
    static member SORT_KEY_TYPE_NetworkPortal = "002_NetworkPortal"

    /// Node type name used at IConfigureNode.SortKey value of ConfNode_TargetGroup
    static member SORT_KEY_TYPE_TargetGroup = "003_TargetGroup"

    /// Node type name used at IConfigureNode.SortKey value of ConfNode_Target
    static member SORT_KEY_TYPE_Target = "004_Target"

    /// Node type name used at IConfigureNode.SortKey value of ConfNode_DummyDeviceLU
    static member SORT_KEY_TYPE_DummyDeviceLU = "005_DummyDeviceLU"

    /// Node type name used at IConfigureNode.SortKey value of ConfNode_BlockDeviceLU
    static member SORT_KEY_TYPE_BlockDeviceLU = "006_BlockDeviceLU"

    /// Node type name used at IConfigureNode.SortKey value of ConfNode_DummyMedia
    static member SORT_KEY_TYPE_DummyMedia = "007_DummyMedia"

    /// Node type name used at IConfigureNode.SortKey value of ConfNode_DummyMedia
    static member SORT_KEY_TYPE_DebugMedia = "008_DebugMedia"

    /// Node type name used at IConfigureNode.SortKey value of ConfNode_PlainFileMedia
    static member SORT_KEY_TYPE_PlainFileMedia = "009_PlainFileMedia"

    /// Node type name used at IConfigureNode.SortKey value of ConfNode_MemBufferMedia
    static member SORT_KEY_TYPE_MemBufferMedia = "010_MemBufferMedia"

    /// Clipboard format string for ConfNode_TargetDevice node
    static member CB_FORMAT_TargetDevice = "Haruka-TargetDevice-fdc379a7-ee2f-4de8-8fca-a500f1a767b3"

    /// Clipboard format string for ConfNode_NetworkPortal node
    static member CB_FORMAT_NetworkPortal = "Haruka-NetworkPortal-fdc379a7-ee2f-4de8-8fca-a500f1a767b3"

    /// Clipboard format string for ConfNode_TargetGroup node
    static member CB_FORMAT_TargetGroup = "Haruka-TargetGroup-fdc379a7-ee2f-4de8-8fca-a500f1a767b3"

    /// Clipboard format string for ConfNode_Target node
    static member CB_FORMAT_Target = "Haruka-Target-fdc379a7-ee2f-4de8-8fca-a500f1a767b3"

    /// Clipboard format string for ConfNode_BlockDeviceLU node
    static member CB_FORMAT_BlockDeviceLU = "Haruka-BlockDeviceLU-fdc379a7-ee2f-4de8-8fca-a500f1a767b3"

    /// Clipboard format string for ConfNode_PlainFileMedia node
    static member CB_FORMAT_PlainFileMedia = "Haruka-PlainFileMedia-fdc379a7-ee2f-4de8-8fca-a500f1a767b3"

    /// Clipboard format string for ConfNode_MemBufferMedia node
    static member CB_FORMAT_MemBufferMedia = "Haruka-MemBufferMedia-fdc379a7-ee2f-4de8-8fca-a500f1a767b3"

    /// Clipboard format string for ConfNode_DebugMedia node
    static member CB_FORMAT_DebugMedia = "Haruka-DebugMedia-fdc379a7-ee2f-4de8-8fca-a500f1a767b3"

    /// ConfNode_Controller type name string used for temp export data.
    static member TEMPEXP_NN_Controller = "ConfNode_Controller"

    /// ConfNode_TargetDevice type name string used for temp export data.
    static member TEMPEXP_NN_TargetDevice = "ConfNode_TargetDevice"

    /// ConfNode_NetworkPortal type name string used for temp export data.
    static member TEMPEXP_NN_NetworkPortal = "ConfNode_NetworkPortal"

    /// ConfNode_TargetGroup type name string used for temp export data.
    static member TEMPEXP_NN_TargetGroup = "ConfNode_TargetGroup"

    /// ConfNode_Target type name string used for temp export data.
    static member TEMPEXP_NN_Target = "ConfNode_Target"

    /// ConfNode_DummyDeviceLU type name string used for temp export data.
    static member TEMPEXP_NN_DummyDeviceLU = "ConfNode_DummyDeviceLU"

    /// ConfNode_BlockDeviceLU type name string used for temp export data.
    static member TEMPEXP_NN_BlockDeviceLU = "ConfNode_BlockDeviceLU"

    /// ConfNode_PlainFileMedia type name string used for temp export data.
    static member TEMPEXP_NN_PlainFileMedia = "ConfNode_PlainFileMedia"

    /// ConfNode_MemBufferMedia type name string used for temp export data.
    static member TEMPEXP_NN_MemBufferMedia = "ConfNode_MemBufferMedia"

    /// ConfNode_DummyMedia type name string used for temp export data.
    static member TEMPEXP_NN_DummyMedia = "ConfNode_DummyMedia"

    /// ConfNode_DummyMedia type name string used for temp export data.
    static member TEMPEXP_NN_DebugMedia = "ConfNode_DebugMedia"

    /// Maximum count of child nodes.
    static member MAX_CHILD_NODE_COUNT = 1024

/// Exception that is raised when contradiction is occurred in the configuration.
type ConfigurationError( m_Message : string ) =
    inherit Exception( m_Message )

/// Exception that is raised when a conflicting change was attempted.
type EditError( m_Message : string ) =
    inherit Exception( m_Message )

