﻿using InternalsViewer.Internals.Engine.Database.Enums;
using InternalsViewer.Internals.Metadata.Internals;
using InternalsViewer.Internals.Metadata.Structures;

namespace InternalsViewer.Internals.Providers.Metadata;

/// <summary>
/// Provider responsible for providing table structure information from the metadata collection
/// </summary>
public class TableStructureProvider
{
    /// <summary>
    /// Gets the table structure for the specified allocation unit
    /// </summary>
    public static TableStructure GetTableStructure(InternalMetadata metadata, long allocationUnitId)
    {
        var structure = new TableStructure(allocationUnitId);

        var allocationUnit = metadata.AllocationUnits
                                     .FirstOrDefault(a => a.AllocationUnitId == allocationUnitId);

        if(allocationUnit == null)
        {
            throw new ArgumentException($"Allocation unit {allocationUnitId} not found");
        }

        var rowSet = metadata.RowSets
                             .FirstOrDefault(p => p.RowSetId == allocationUnit.ContainerId);

        if(rowSet == null)
        {
            throw new ArgumentException($"Row set {allocationUnit.ContainerId} not found");
        }

        structure.IndexType = rowSet.IndexId == 0 ? IndexType.Heap : IndexType.Clustered;

        structure.CompressionType = rowSet.CompressionType;

        var columnLayouts = metadata.ColumnLayouts.Where(c => c.PartitionId == rowSet.RowSetId).ToList();

        var columns = metadata.Columns.Where(c => c.ObjectId == rowSet.ObjectId).ToList();

        var indexColumns = metadata.IndexColumns
                                   .Where(c => c.ObjectId == rowSet.ObjectId
                                               && c.IndexId == rowSet.IndexId)
                                   .ToList();

        structure.Columns.AddRange(columnLayouts.Select(s =>
        {
            var column = columns.FirstOrDefault(c => c.ColumnId == s.ColumnId);

            var isDropped = Convert.ToBoolean(s.Status & 2);
            var isUniqueifer = Convert.ToBoolean(s.Status & 16);   
            var isKey = indexColumns.Any(c => c.ColumnId == s.ColumnId);

            structure.ObjectId = rowSet.ObjectId;
            structure.IndexId = rowSet.IndexId;
            structure.PartitionId = rowSet.RowSetId;

            /*
                The Offset field is a 4 byte integer, the first 2 bytes represent the leaf offset (offset in a leaf index page), the second
                2 bytes represent the node offset (offset in a node/non-leaf index page).
            */
            var leafOffset = (short)(s.Offset & 0xffff);
            var nodeOffset = (short)(s.Offset >> 16);

            string name;

            if(isDropped)
            {
                name = "(Dropped)";
            }
            else if(isUniqueifer)
            {
                name = "UNIQUIFIER";
            }
            else
            {
                name = column?.Name ?? "Unknown";
            }

            var typeInfo = s.TypeInfo.ToTypeInfo();

            var result = new ColumnStructure
            {
                ColumnId = s.ColumnId,
                ColumnName = name,
                DataType = typeInfo.DataType,
                LeafOffset = leafOffset,
                NodeOffset = nodeOffset,
                Precision = typeInfo.Precision,
                DataLength = typeInfo.MaxLength,
                Scale = typeInfo.Scale,
                IsDropped = isDropped,
                IsUniqueifier = isUniqueifer,
                IsSparse = (s.Status & 256) != 0,
                NullBitIndex = (short)(s.NullBit & 0xffff),
                BitPosition = s.BitPosition,
                IsKey = isKey
            };

            return result;
        }));

        return structure;
    }
}
