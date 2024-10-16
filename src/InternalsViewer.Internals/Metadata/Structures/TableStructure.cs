﻿namespace InternalsViewer.Internals.Metadata.Structures;

public record TableStructure(long AllocationUnitId)
    : Structure<ColumnStructure>(AllocationUnitId)
{
    public int ColumnCount => Columns.Count;
}