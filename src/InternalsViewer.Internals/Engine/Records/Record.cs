﻿using InternalsViewer.Internals.Engine.Annotations;

namespace InternalsViewer.Internals.Engine.Records;

/// <summary>
/// Database Record Structure
/// </summary>
public abstract class Record : DataStructure
{
    public ushort SlotOffset { get; set; }

    public List<RecordField> Fields { get; } = new();

    public RecordField[] FieldsArray => Fields.ToArray();

    [DataStructureItem(ItemType.ColumnCount)]
    public short ColumnCount { get; set; }
}