﻿using InternalsViewer.Internals.Engine.Address;

namespace InternalsViewer.Internals.Engine.Pages;

/// <summary>
/// Boot Page
/// </summary>
public class BootPage : Page
{
    public static PageAddress BootPageAddress = new(1, 9);

    public string DatabaseName { get; set; } = string.Empty;

    public int DatabaseId { get; set; }

    public short CreatedVersion { get; set; }

    public short CurrentVersion { get; set; }

    /// <summary>
    /// First page for the Allocation Units (sys.sysallocunits) table
    /// </summary>
    /// <remarks>
    /// dbi_firstSysIndexes in the DBCC PAGE output
    /// </remarks>
    public PageAddress FirstAllocationUnitsPage { get; set; }

    public int Status { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public int CompatibilityLevel { get; set; }

    /// <summary>
    /// Last checkpoint LSN.
    /// </summary>
    public LogSequenceNumber CheckpointLsn { get; set; }

    public long MaxLogSpaceUsed { get; set; }

    public int Collation { get; set; }

    public long NextAllocationUnitId { get; set; }
}