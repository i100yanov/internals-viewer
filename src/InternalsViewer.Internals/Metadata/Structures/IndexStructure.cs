﻿namespace InternalsViewer.Internals.Metadata.Structures;

public record IndexStructure(long AllocationUnitId)
    : Structure<IndexColumnStructure>(AllocationUnitId)
{
    public bool IsUnique { get; set; }

    public bool HasFilter { get; set; }

    public TableStructure? TableStructure { get; set; }
}