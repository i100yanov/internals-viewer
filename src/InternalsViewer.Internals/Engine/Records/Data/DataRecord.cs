﻿using InternalsViewer.Internals.Annotations;
using InternalsViewer.Internals.Converters;
using InternalsViewer.Internals.Engine.Address;
using InternalsViewer.Internals.Engine.Records.FixedVarRecordType;

namespace InternalsViewer.Internals.Engine.Records.Data;

public class DataRecord : FixedVarRecord
{
    public SparseVector? SparseVector { get; set; }

    [DataStructureItem(ItemType.StatusBitsB)]
    public string StatusBitsBDescription => "";

    [DataStructureItem(ItemType.ForwardingStub)]
    public RowIdentifier? ForwardingStub { get; set; }

    public RowIdentifier? RowIdentifier { get; set; }

    public T? GetValue<T>(string columnName)
    {
        var field = Fields.FirstOrDefault(f => f.Name.ToLower() == columnName.ToLower());

        if (field == null)
        {
            throw new ArgumentException($"Column {columnName} not found");
        }

        if (field.Data.Length == 0)
        {
            return default;
        }

        return DataConverter.GetValue<T>(field.Data,
                                         field.ColumnStructure.DataType,
                                         field.ColumnStructure.Precision,
                                         field.ColumnStructure.Scale);
    }
}