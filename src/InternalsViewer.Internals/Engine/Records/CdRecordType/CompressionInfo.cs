﻿using InternalsViewer.Internals.Annotations;

namespace InternalsViewer.Internals.Engine.Records.CdRecordType;

/// <summary>
/// Compression Info record
/// </summary>
/// <remarks>
/// The CI record is a record added for pages compressed using the PAGE compression type.
/// 
/// It has the following structure:
/// 
///         - Header
///         - Page Modification Count
///         - Offsets (Length = CI Record Size, Size = CI Record Size)
///         - Anchor Record
///         - Dictionary
/// </remarks>
public class CompressionInfo(int offset) : DataStructure
{
    [DataStructureItem(ItemType.PageModificationCount)]
    public short PageModificationCount { get; set; }

    [DataStructureItem(ItemType.Size)]
    public short Size { get; set; }

    [DataStructureItem(ItemType.Length)]
    public ushort Length { get; set; }

    [DataStructureItem(ItemType.Header)]
    public byte Header { get; set; }

    public static ushort SlotOffset => 96;

    [DataStructureItem(ItemType.AnchorRecord)]
    public CompressedDataRecord? AnchorRecord { get; set; }

    [DataStructureItem(ItemType.CompressionDictionary)]
    public Dictionary? CompressionDictionary { get; set; }

    public bool HasAnchorRecord { get; set; }

    public bool HasDictionary { get; set; }

    public int Offset { get; set; } = offset;
}