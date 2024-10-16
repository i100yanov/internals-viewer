﻿using InternalsViewer.Internals.Annotations;
using InternalsViewer.Internals.Engine.Records;
using InternalsViewer.Internals.Metadata.Structures;
using InternalsViewer.Internals.Services.Loaders.Records;

namespace InternalsViewer.Internals.Engine.Records.Data;

public class SparseVector : DataStructure
{
    public const int ColCountOffset = 2;
    public const int ColumnsOffset = 4;

    internal SparseVector(byte[] sparseRecord, TableStructure structure, DataRecord parentRecord, short recordOffset)
    {
        Data = sparseRecord;
        Structure = structure;
        ParentRecord = parentRecord;
        RecordOffset = recordOffset;
    }

    private static string GetComplexHeaderDescription(short complexVector)
    {
        switch (complexVector)
        {
            case 5:
                return "In row sparse vector";
            default:
                return "Unknown";
        }
    }

    internal TableStructure Structure { get; set; }

    public byte[] Data { get; set; }

    internal DataRecord ParentRecord { get; set; }

    public ushort[] Columns { get; set; } = Array.Empty<ushort>();

    [DataStructureItem(ItemType.SparseColumns)]
    public string ColumnsDescription => RecordHelpers.GetArrayString(Columns);

    [DataStructureItem(ItemType.SparseColumnOffsets)]
    public string OffsetsDescription => RecordHelpers.GetArrayString(Offset);

    public ushort[] Offset { get; set; } = Array.Empty<ushort>();

    [DataStructureItem(ItemType.SparseColumnCount)]
    public short ColCount { get; set; }

    public short RecordOffset { get; set; }

    public short ComplexHeader { get; set; }

    [DataStructureItem(ItemType.ComplexHeader)]
    public string ComplexHeaderDescription => GetComplexHeaderDescription(ComplexHeader);
}