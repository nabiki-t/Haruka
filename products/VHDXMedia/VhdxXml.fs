//=============================================================================
// Haruka Software Storage.
// VhdxXml.fs : Output the VHDX structures data of the VHDX file in XML format.
// 

//=============================================================================
// Namespace declaration

namespace Haruka.Media.VhdxUtil

//=============================================================================
// Import declaration

open System
open System.IO
open System.Text
open System.Xml


//=============================================================================
// Class implementation

/// Output VHDX file structures as XML data.
type VhdxXmlSerializer() =

    /// <summary>
    ///  Convert bytes array to HEX string.
    /// </summary>
    /// <param name="bytes">
    ///  Bytes array data.
    /// </param>
    /// <returns>
    ///  Converted HEX string.
    /// </returns>
    static member bytesToHex ( bytes : byte[] ) : string =
        if bytes = null || bytes.Length = 0 then
            ""
        else
            System.BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant()
    
    /// <summary>
    ///  Convert VHDX structures to XML string.
    /// </summary>
    /// <param name="structures">
    ///  VHDX structures data.
    /// </param>
    /// <returns>
    ///  Converted XML string.
    /// </returns>
    static member Serialize ( structures : VhdxStructures ) : string =

        let sb = StringBuilder()
        use writer = XmlWriter.Create( sb, XmlWriterSettings( Indent = true, Encoding = Encoding.UTF8 ) )
        writer.WriteStartDocument()
        writer.WriteStartElement( "VhdxStructures" )

        // File type identifier
        writer.WriteStartElement( "FileTypeIdentifier" )
        writer.WriteString( structures.Creator )
        writer.WriteEndElement()
            
        // Header
        writer.WriteStartElement( "Header" )
        writer.WriteElementString( "Checksum", "0x" + structures.ImmHeader.Checksum.ToString( "X8" ) )
        writer.WriteElementString( "SequenceNumber", string structures.LoadedVarHeader.SequenceNumber )
        writer.WriteElementString( "FileWriteGuid", structures.LoadedVarHeader.FileWriteGuid.ToString "D" )
        writer.WriteElementString( "DataWriteGuid", structures.LoadedVarHeader.DataWriteGuid.ToString "D" )
        writer.WriteElementString( "LogGuid", structures.LoadedVarHeader.LogGuid.ToString "D" )
        writer.WriteElementString( "LogVersion", string structures.ImmHeader.LogVersion )
        writer.WriteElementString( "Version", string structures.ImmHeader.Version )
        writer.WriteElementString( "LogLength", string structures.ImmHeader.LogLength )
        writer.WriteElementString( "LogOffset", string structures.ImmHeader.LogOffset )
        writer.WriteElementString( "Offset", string structures.ImmHeader.Offset )
        writer.WriteElementString( "Index", string structures.ImmHeader.Index )
        writer.WriteEndElement()

        // Log
        writer.WriteStartElement( "LogEntries" )
        structures.Log
        |> List.iter ( fun ( e : LogEntry ) ->
            writer.WriteStartElement( "LogEntry" )
            writer.WriteElementString( "Checksum", "0x" + e.Checksum.ToString("X8") )
            writer.WriteElementString( "EntryLength", e.EntryLength.ToString() )
            writer.WriteElementString( "Tail", "0x" + e.Tail.ToString("X8")  )
            writer.WriteElementString( "SequenceNumber", e.SequenceNumber.ToString() )
            writer.WriteElementString( "DescriptorCount", e.DescriptorCount.ToString() )
            writer.WriteElementString( "LogGuid", e.LogGuid.ToString "D" )
            writer.WriteElementString( "FlushedFileOffset", e.FlushedFileOffset.ToString() )
            writer.WriteElementString( "LastFileOffset", e.LastFileOffset.ToString() )
            writer.WriteStartElement( "Descriptors" )
            e.Descriptors
            |> List.iter ( fun ( d : LogDescriptor ) ->
                match d with
                | LogDescriptor.Data( x ) ->
                    writer.WriteStartElement( "DataDescriptor" )
                    writer.WriteElementString( "TrailingBytes", VhdxXmlSerializer.bytesToHex x.TrailingBytes )
                    writer.WriteElementString( "LeadingBytes", VhdxXmlSerializer.bytesToHex x.LeadingBytes )
                    writer.WriteElementString( "FileOffset", x.FileOffset.ToString() )
                    writer.WriteElementString( "SequenceNumber", x.SequenceNumber.ToString() )
                    writer.WriteElementString( "DataDescriptorIndex", x.ddIndex.ToString() )
                    writer.WriteEndElement()
                | LogDescriptor.Zero( x ) ->
                    writer.WriteStartElement( "ZeroDescriptor" )
                    writer.WriteElementString( "ZeroLength", x.ZeroLength.ToString() )
                    writer.WriteElementString( "FileOffset", x.FileOffset.ToString() )
                    writer.WriteElementString( "SequenceNumber", x.SequenceNumber.ToString() )
                    writer.WriteEndElement()
            )
            writer.WriteEndElement()
            writer.WriteElementString( "DataSectors.Length", e.DataSectors.Length |> _.ToString() )
            writer.WriteEndElement()
        )
        writer.WriteEndElement()

        // Region table
        writer.WriteStartElement( "RegionTable" )
        writer.WriteElementString( "Checksum", "0x" + structures.Region.Checksum.ToString("X8") )
        writer.WriteElementString( "EntryCount", string structures.Region.EntryCount )
        writer.WriteStartElement( "Entries" )
        List.iteri ( fun j ( e : RegionEntry ) ->
            let regionName =
                if e.Guid = VhdxCommons.REGENT_TYPE_BAT then
                    "BAT"
                elif e.Guid = VhdxCommons.REGENT_TYPE_METADATA then
                    "Metadata"
                else
                    sprintf "Region_%d" j
            writer.WriteStartElement( "Entry" )
            writer.WriteElementString( "Index", string j )
            writer.WriteElementString( "Name", regionName )
            writer.WriteElementString( "IsRequired", string e.Required )
            writer.WriteElementString( "Guid", e.Guid.ToString "D" )
            writer.WriteElementString( "FileOffset", string e.FileOffset )
            writer.WriteElementString( "Length", string e.Length )
            writer.WriteEndElement()
        ) structures.Region.Entries
        writer.WriteEndElement()
        writer.WriteEndElement()
            
        // Virtual disk info
        writer.WriteStartElement( "VirtualDiskInfo" )
        writer.WriteElementString( "PayloadBlockSize", string structures.VDI.PayloadBlockSize )
        writer.WriteElementString( "LeaveBlockAllocated", string structures.VDI.LeaveBlockAllocated )
        writer.WriteElementString( "HasParent", string structures.VDI.HasParent )
        writer.WriteElementString( "VirtualDiskSize", string structures.VDI.VirtualDiskSize )
        writer.WriteElementString( "VirtualDiskId", string structures.VDI.VirtualDiskId )
        writer.WriteElementString( "LogicalSectorSize", string structures.VDI.LogicalSectorSize )
        writer.WriteElementString( "PhysicalSectorSize", string structures.VDI.PhysicalSectorSize )
        writer.WriteStartElement( "ParentLocators" )
        for itr in structures.VDI.ParentLocator do
            writer.WriteStartElement( "ParentLocator" )
            writer.WriteElementString( "Key", itr.Key )
            writer.WriteElementString( "Value", itr.Value )
            writer.WriteEndElement()
        writer.WriteEndElement()
        writer.WriteEndElement()

        // BAT entry
        writer.WriteStartElement( "BatEntries" )
        writer.WriteElementString( "BATRegionOffset", string structures.BAT.BATRegionOffset )
        writer.WriteElementString( "BATRegionLength", string structures.BAT.BATRegionLength )
        writer.WriteElementString( "ChunkSize", string structures.BAT.ChunkSize )
        writer.WriteElementString( "ChunkRatio", string structures.BAT.ChunkRatio )
        writer.WriteElementString( "PayloadBlockCount", string structures.BAT.PayloadBlockCount )
        writer.WriteElementString( "SectorBitmapBlockCount", string structures.BAT.SectorBitmapBlockCount )
        writer.WriteElementString( "BatEntryCount", string structures.BAT.BatEntryCount )
        writer.WriteStartElement( "Payloads" )
        for i = 0 to structures.BAT.Payloads.Length - 1 do
            let itr = structures.BAT.Payloads[i]
            writer.WriteStartElement( "Payload" )
            writer.WriteElementString( "Index", string i )
            writer.WriteElementString( "BatEntryIndex", string itr.BatEntryIndex )
            writer.WriteElementString( "State", string itr.State )
            writer.WriteElementString( "FileOffset", string itr.FileOffset )
            writer.WriteEndElement()
        writer.WriteEndElement()
        writer.WriteStartElement( "SectorBitmaps" )
        for i = 0 to structures.BAT.SectorBitmap.Length - 1 do
            let itr = structures.BAT.SectorBitmap.[i]
            writer.WriteStartElement( "SectorBitmap" )
            writer.WriteElementString( "Index", string i )
            writer.WriteElementString( "BatEntryIndex", string itr.BatEntryIndex )
            writer.WriteElementString( "SBState", string itr.SBState )
            writer.WriteElementString( "FileOffset", string itr.FileOffset )
            writer.WriteElementString( "BitmapLength", string itr.Bitmap.Length )
            if itr.SBState.IsSectorBitmapPresent then
                writer.WriteStartElement( "Bitmap" )
                let bmpChunkLength = itr.Bitmap.Length / int structures.BAT.ChunkRatio
                for j = 0 to int structures.BAT.ChunkRatio - 1 do
                    let spos = j * bmpChunkLength
                    writer.WriteElementString( "CorrespondingPayloadIndex", string ( i * int structures.BAT.ChunkRatio + j ) )
                    let targetSpan = ReadOnlySpan<byte>( itr.Bitmap, spos, bmpChunkLength )
                    for k in 0 .. 32 .. ( bmpChunkLength - 1 ) do
                        let sb = StringBuilder()
                        for b in targetSpan.Slice( k, 32 ) do
                            sb.AppendFormat( "{0:x2} ", b ) |> ignore
                        writer.WriteElementString( sprintf "Bitmap%05d" k, sb.ToString() )
                writer.WriteEndElement()
            writer.WriteEndElement()
        writer.WriteEndElement()
        writer.WriteEndElement()

        writer.WriteEndElement()
        writer.WriteEndDocument()
        writer.Flush()
        
        sb.ToString()
    
    /// <summary>
    ///  Write VHDX structures to file as XML.
    /// </summary>
    /// <param name="structures">
    ///  VHDX structures data.
    /// </param>
    /// <param name="filePath">
    ///  Output file name.
    /// </param>
    static member SerializeToFile ( structures : VhdxStructures ) ( filePath : string ) : unit =
        let xml = VhdxXmlSerializer.Serialize( structures )
        File.WriteAllText( filePath, xml, Encoding.UTF8 )
