﻿namespace InternalsViewer.Internals.Metadata.Structures;

public record IndexColumnStructure : ColumnStructure
{
    public bool IsIncludeColumn { get; set; }

    public bool IsIndexKey { get; set; }

    public int IndexColumnId { get; set; }
}