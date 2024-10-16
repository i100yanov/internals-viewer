﻿using System.Diagnostics;
using InternalsViewer.Internals.Engine.Address;
using InternalsViewer.Internals.Engine.Records.Blob.BlobPointers;

namespace InternalsViewer.Internals.Services.Loaders.Records.Fields;

/// <summary>
/// Loads a LOB Overflow pointer field
/// </summary>
/// <remarks>
/// 
/// </remarks>
public class LobOverflowFieldLoader
{
    private const int ChildOffset = 12;
    private const int LevelOffset = 1;
    private const int TimestampOffset = 6;
    private const int UnusedOffset = 3;
    private const int UpdateSeqOffset = 4;

    public static OverflowField Load(byte[] data, int offset)
    {
        Debug.Assert(data.Length == 24, "Invalid Overflow Field Length");

        var field = new OverflowField();

        field.MarkProperty("PointerType", offset, sizeof(byte));

        field.PointerType = (BlobFieldType)data[0];

        field.MarkProperty("Unused", offset + UnusedOffset, sizeof(byte));

        field.Unused = data[UnusedOffset];

        field.MarkProperty("Level", offset + LevelOffset, sizeof(byte));

        field.Level = data[LevelOffset];

        field.MarkProperty("Timestamp", offset + TimestampOffset, sizeof(int));

        field.Timestamp = BitConverter.ToUInt32(data, TimestampOffset);

        field.MarkProperty("UpdateSeq", offset + UpdateSeqOffset, sizeof(short));

        field.UpdateSeq = BitConverter.ToInt16(data, UpdateSeqOffset);

        LoadLinks(field, data, offset);

        return field;
    }

    protected static void LoadLinks(OverflowField field, byte[] data, int offset)
    {
        field.Links = new List<BlobChildLink>();

        field.MarkProperty("Length", offset + ChildOffset, sizeof(int));

        field.Length = BitConverter.ToInt32(data, ChildOffset);

        var rowIdData = new byte[8];

        Array.Copy(data, ChildOffset + 4, rowIdData, 0, 8);

        field.MarkProperty("LinksArray", string.Empty, 0);

        var rowId = new RowIdentifier(rowIdData);

        var link = new BlobChildLink(rowId, field.Length, 0);

        link.MarkProperty("RowIdentifier", offset + ChildOffset + 4, 8);

        field.Links.Add(link);
    }
}