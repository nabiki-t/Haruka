namespace VhdxLibrary

open System
open System.IO
open System.Text
open System.Xml

/// Output VHDX metadata as XML data.
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
    ///  Convert VHDX metadata to XML string.
    /// </summary>
    /// <param name="metadata">
    ///  VHDX metadata.
    /// </param>
    /// <returns>
    ///  Converted XML string.
    /// </returns>
    static member Serialize ( metadata : VhdxMetadata ) : string =

        let sb = StringBuilder()
        use writer = XmlWriter.Create( sb, XmlWriterSettings( Indent = true, Encoding = Encoding.UTF8 ) )
        writer.WriteStartDocument()
        writer.WriteStartElement( "VhdxMetadata" )

        // File type identifier
        writer.WriteStartElement( "FileTypeIdentifier" )
        writer.WriteString( metadata.Creator )
        writer.WriteEndElement()
            
        // Header
        writer.WriteStartElement( "Header" )
        writer.WriteElementString( "Checksum", "0x" + metadata.Header.Checksum.ToString( "X8" ) )
        writer.WriteElementString( "SequenceNumber", string metadata.Header.SequenceNumber )
        writer.WriteElementString( "FileWriteGuid", metadata.Header.FileWriteGuid.ToString "D" )
        writer.WriteElementString( "DataWriteGuid", metadata.Header.DataWriteGuid.ToString "D" )
        writer.WriteElementString( "LogGuid", metadata.Header.LogGuid.ToString "D" )
        writer.WriteElementString( "LogVersion", string metadata.Header.LogVersion )
        writer.WriteElementString( "Version", string metadata.Header.Version )
        writer.WriteElementString( "LogLength", string metadata.Header.LogLength )
        writer.WriteElementString( "LogOffset", string metadata.Header.LogOffset )
        writer.WriteElementString( "Offset", string metadata.Header.Offset )
        writer.WriteElementString( "Index", string metadata.Header.Index )
        writer.WriteEndElement()

        // Log
        writer.WriteStartElement( "LogEntries" )
        metadata.LogInfo
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
            writer.WriteElementString( "DataSectors.Length", e.DataSectors.Length.ToString() )
            writer.WriteEndElement()
        )
        writer.WriteEndElement()

        // Region table
        writer.WriteStartElement( "RegionTable" )
        writer.WriteElementString( "Checksum", "0x" + metadata.RegionTables.Checksum.ToString("X8") )
        writer.WriteElementString( "EntryCount", string metadata.RegionTables.EntryCount )
        writer.WriteStartElement( "Entries" )
        List.iteri ( fun j ( e : RegionEntry ) ->
            let regionName =
                if e.Guid = GlbFunc.REGENT_TYPE_BAT then
                    "BAT"
                elif e.Guid = GlbFunc.REGENT_TYPE_METADATA then
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
        ) metadata.RegionTables.Entries
        writer.WriteEndElement()
        writer.WriteEndElement()
            
        // Virtual disk info
        writer.WriteStartElement( "VirtualDiskInfo" )
        writer.WriteElementString( "PayloadBlockSize", string metadata.VirtualDiskInfo.PayloadBlockSize )
        writer.WriteElementString( "LeaveBlockAllocated", string metadata.VirtualDiskInfo.LeaveBlockAllocated )
        writer.WriteElementString( "HasParent", string metadata.VirtualDiskInfo.HasParent )
        writer.WriteElementString( "VirtualDiskSize", string metadata.VirtualDiskInfo.VirtualDiskSize )
        writer.WriteElementString( "VirtualDiskId", string metadata.VirtualDiskInfo.VirtualDiskId )
        writer.WriteElementString( "LogicalSectorSize", string metadata.VirtualDiskInfo.LogicalSectorSize )
        writer.WriteElementString( "PhysicalSectorSize", string metadata.VirtualDiskInfo.PhysicalSectorSize )
        writer.WriteStartElement( "ParentLocators" )
        for itr in metadata.VirtualDiskInfo.ParentLocator do
            writer.WriteStartElement( "ParentLocator" )
            writer.WriteElementString( "Key", itr.Key )
            writer.WriteElementString( "Value", itr.Value )
            writer.WriteEndElement()
        writer.WriteEndElement()
        writer.WriteEndElement()

        // BAT entry
        writer.WriteStartElement( "BatEntries" )
        writer.WriteElementString( "BATRegionOffset", string metadata.BatEntries.BATRegionOffset )
        writer.WriteElementString( "BATRegionLength", string metadata.BatEntries.BATRegionLength )
        writer.WriteElementString( "ChunkSize", string metadata.BatEntries.ChunkSize )
        writer.WriteElementString( "ChunkRatio", string metadata.BatEntries.ChunkRatio )
        writer.WriteElementString( "PayloadBlockCount", string metadata.BatEntries.PayloadBlockCount )
        writer.WriteElementString( "SectorBitmapBlockCount", string metadata.BatEntries.SectorBitmapBlockCount )
        writer.WriteElementString( "BatEntryCount", string metadata.BatEntries.BatEntryCount )
        writer.WriteStartElement( "Payloads" )
        for itr in metadata.BatEntries.Payloads do
            writer.WriteStartElement( "Payload" )
            writer.WriteElementString( "BatEntryIndex", string itr.BatEntryIndex )
            writer.WriteElementString( "State", string itr.State )
            writer.WriteElementString( "FileOffset", string itr.FileOffset )
            writer.WriteEndElement()
        writer.WriteEndElement()
        writer.WriteStartElement( "SectorBitmaps" )
        for itr in metadata.BatEntries.SectorBitmap do
            writer.WriteStartElement( "SectorBitmap" )
            writer.WriteElementString( "BatEntryIndex", string itr.BatEntryIndex )
            writer.WriteElementString( "SBState", string itr.SBState )
            writer.WriteElementString( "FileOffset", string itr.FileOffset )
            writer.WriteEndElement()
        writer.WriteEndElement()
        writer.WriteEndElement()

        writer.WriteEndElement()
        writer.WriteEndDocument()
        writer.Flush()
        
        sb.ToString()
    
    /// <summary>
    ///  Write VHDX metadata to file as XML.
    /// </summary>
    /// <param name="metadata">
    ///  VHDX metadata.
    /// </param>
    /// <param name="filePath">
    ///  Output file name.
    /// </param>
    static member SerializeToFile ( metadata : VhdxMetadata ) ( filePath : string ) : unit =
        let xml = VhdxXmlSerializer.Serialize( metadata )
        File.WriteAllText( filePath, xml, Encoding.UTF8 )
