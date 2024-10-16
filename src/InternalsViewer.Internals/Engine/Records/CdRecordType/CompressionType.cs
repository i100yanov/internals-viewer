﻿namespace InternalsViewer.Internals.Engine.Records.CdRecordType;

/// <summary>
/// 2008 Page compression type
/// </summary>
[Flags]
public enum CompressionType : byte
{
    /// <summary>
    /// No compression (default)
    /// </summary>
    None = 0,
    /// <summary>
    /// Row level compressions
    /// </summary>
    Row = 1,
    /// <summary>
    /// Page level compression
    /// </summary>
    Page = 2,

    Columnstore = 3,

    ColumnstoreArchive = 4,
}