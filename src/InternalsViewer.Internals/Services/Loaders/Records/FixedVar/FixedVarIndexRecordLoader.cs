﻿using System.Collections;
using System.Diagnostics;
using System.Xml.Linq;
using InternalsViewer.Internals.Annotations;
using InternalsViewer.Internals.Engine.Address;
using InternalsViewer.Internals.Engine.Database.Enums;
using InternalsViewer.Internals.Engine.Pages;
using InternalsViewer.Internals.Engine.Parsers;
using InternalsViewer.Internals.Engine.Records;
using InternalsViewer.Internals.Engine.Records.FixedVarRecordType;
using InternalsViewer.Internals.Engine.Records.Index;
using InternalsViewer.Internals.Metadata.Helpers;
using InternalsViewer.Internals.Metadata.Structures;

namespace InternalsViewer.Internals.Services.Loaders.Records.FixedVar;

/// <summary>
/// Loads an Index Record using a combination of the table structure and the record structure
/// </summary>
/// <remarks>
/// Microsoft SQL Server 2012 Internals by Kalen Delaney et al. has a good description of the index record structure in Chapter 7.
/// 
/// There are several different types of index records this loader has to parse:
/// 
/// - Clustered Index
///     - Node records
///         Note - not Leaf records - these are the data pages themselves
///     - Unique / Non-Unique (Uniqueifier)
///     
/// - Non-Clustered Index
///     - Based on a Heap or based on a Clustered Index
///     - Node records
///     - Leaf records
///     - Unique / Non-Unique
///     - Includes columns
///     
///         ┌───────────────┬────────┬───────────┬──────────┐
///         │     Type      │ Unique │ Node Type │ Includes │
///         ├───────────────┼────────┼───────────┼──────────┤
///         │ Clustered     │ Yes    │ Root      │ No       │
///         │ Clustered     │ Yes    │ Node      │ No       │
///         │ Clustered     │ Yes    │ Leaf      │ No       │
///         │ Clustered     │ No     │ Root      │ No       │
///         │ Clustered     │ No     │ Node      │ No       │
///         │ Clustered     │ No     │ Leaf      │ No       │
///         │ Clustered     │ Yes    │ Root      │ Yes      │
///         │ Clustered     │ Yes    │ Node      │ Yes      │
///         │ Clustered     │ Yes    │ Leaf      │ Yes      │
///         │ Clustered     │ No     │ Root      │ Yes      │
///         │ Clustered     │ No     │ Node      │ Yes      │
///         │ Clustered     │ No     │ Leaf      │ Yes      │
///         │ Non-Clustered │ Yes    │ Root      │ No       │
///         │ Non-Clustered │ Yes    │ Node      │ No       │
///         │ Non-Clustered │ Yes    │ Leaf      │ No       │
///         │ Non-Clustered │ No     │ Root      │ No       │
///         │ Non-Clustered │ No     │ Node      │ No       │
///         │ Non-Clustered │ No     │ Leaf      │ No       │
///         │ Non-Clustered │ Yes    │ Root      │ Yes      │
///         │ Non-Clustered │ Yes    │ Node      │ Yes      │
///         │ Non-Clustered │ Yes    │ Leaf      │ Yes      │
///         │ Non-Clustered │ No     │ Root      │ Yes      │
///         │ Non-Clustered │ No     │ Node      │ Yes      │
///         │ Non-Clustered │ No     │ Leaf      │ Yes      │
///         └───────────────┴────────┴───────────┴──────────┘
///     
/// This is in addition to the variable/fixed length record fields.
/// </remarks>
public class FixedVarIndexRecordLoader(ILogger<FixedVarIndexRecordLoader> logger) : FixedVarRecordLoader
{
    private ILogger<FixedVarIndexRecordLoader> Logger { get; } = logger;

    /// <summary>
    /// Load an Index record at the specified offset
    /// </summary>
    public IndexRecord Load(IndexPage page, ushort offset, int slot, IndexStructure structure)
    {
        Logger.BeginScope("Index Record Loader: {FileId}:{PageId}:{Offset}", page.PageAddress.FileId, page.PageAddress.PageId, offset);

        Logger.LogDebug(structure.ToDetailString());

        var nodeType = page.PageHeader.Level switch
        {
            0 => NodeType.Leaf,
            1 => NodeType.Root,
            _ => NodeType.Node,
        };

        var record = new IndexRecord
        {
            Offset = offset,
            Slot = slot,
            NodeType = nodeType
        };

        Logger.LogDebug("Loading Index Record ({nodeType}) at offset {offset}", nodeType, offset);

        // Indexes should always have a Status Bits A
        LoadStatusBitsA(record, page.Data);

        Logger.LogTrace("Status Bits A: {StatusBitsA}", record.StatusBitsA);

        // Load the null bitmap if necessary
        if (record.HasNullBitmap)
        {
            Logger.LogTrace("Has Null Bitmap flag set, loading null bitmap");

            LoadNullBitmap(record, page, structure);
        }

        // Load the variable length column offset array if necessary
        if (record.HasVariableLengthColumns)
        {
            Logger.LogTrace("Has Variable Length Columns flag set, loading offset array");

            var startIndex = record.HasNullBitmap ? 2 + record.NullBitmapSize : 0;

            LoadColumnOffsetArray(record, startIndex, page);

            // Calculate the offset of the variable length data
            record.VariableLengthDataOffset = (ushort)(page.PageHeader.FixedLengthSize
                                                       + sizeof(short)
                                                       + startIndex
                                                       + sizeof(short) * record.VariableLengthColumnCount);
        }

        Logger.LogDebug("Node Type: {NodeType}, Index Type: {IndexType}, Underlying Index Type: {ParentIndexType}",
                        record.NodeType,
                        page.AllocationUnit.IndexType,
                        structure.TableStructure?.IndexType ?? structure.IndexType);

        switch (nodeType)
        {
            case NodeType.Root:
                Logger.LogDebug("Loading Root Record");

                LoadDownPagePointer(record, page);

                if (structure.IndexType == IndexType.Clustered)
                {
                    LoadClusteredNode(record, page, structure);
                }
                else
                {
                    LoadNonClusteredRoot(record, page, structure);
                }

                break;
            case NodeType.Node:
                {
                    Logger.LogDebug("Loading Node Record");

                    // A node will have a down page pointer to the next level in the b-tree
                    LoadDownPagePointer(record, page);

                    if (structure.IndexType == IndexType.Clustered)
                    {
                        LoadClusteredNode(record, page, structure);
                    }
                    else
                    {
                        LoadNonClusteredNode(record, page, structure);
                    }

                    break;
                }
            case NodeType.Leaf:
                Logger.LogDebug("Loading Leaf Record");

                Debug.Assert(structure.IndexType == IndexType.NonClustered, "Leaf level on Index type pages should always be non-clustered");

                LoadNonClusteredLeaf(record, page, structure);
                break;
        }

        return record;
    }

    /// <summary>
    /// Load a clustered index node record
    /// </summary>
    /// <remarks>
    /// A clustered index node will contain the clustered key columns and a down page pointer
    /// </remarks>
    private void LoadClusteredNode(IndexRecord record, PageData page, IndexStructure structure)
    {
        var columns = structure.Columns.Where(c => c.IsKey || c.IsUniqueifier).ToList();

        LoadColumnValues(record, page, columns, NodeType.Node, structure.IsUnique);
    }

    private void LoadNonClusteredNode(IndexRecord record, PageData page, IndexStructure structure)
    {
        List<IndexColumnStructure> columns;

        if (structure.TableStructure?.IndexType == IndexType.Clustered)
        {
            columns = structure.Columns.Where(c => c.IsKey || c.IsUniqueifier).ToList();
        }
        else
        {
            columns = structure.Columns;
        }

        LoadColumnValues(record, page, columns, NodeType.Node, structure.IsUnique);
    }

    private void LoadNonClusteredRoot(IndexRecord record, PageData page, IndexStructure structure)
    {
        List<IndexColumnStructure> columns;

        if (structure.TableStructure?.IndexType == IndexType.Clustered && !structure.IsUnique)
        {
            columns = structure.Columns.Where(c => c.IsKey || c.IsUniqueifier).ToList();
        }
        else
        {
            // Unique non-clustered indexes do not contain the clustered index keys in the root
            columns = structure.Columns.Where(c => c.IsIndexKey).ToList();
        }

        LoadColumnValues(record, page, columns, NodeType.Root, structure.IsUnique);
    }

    private void LoadNonClusteredLeaf(IndexRecord record, PageData page, IndexStructure structure)
    {
        var columns = structure.Columns;

        LoadColumnValues(record, page, columns, NodeType.Leaf, structure.IsUnique);
    }

    /// <summary>
    /// Load a down page pointer (page address) pointing to the next level in the b-tree
    /// </summary>
    private static void LoadDownPagePointer(IndexRecord record, PageData page)
    {
        //Last 6 bytes of the fixed slot
        var address = new byte[PageAddress.Size];

        var downPagePointerOffset = record.Offset + page.PageHeader.FixedLengthSize - PageAddress.Size;

        Array.Copy(page.Data, downPagePointerOffset, address, 0, PageAddress.Size);

        record.DownPagePointer = PageAddressParser.Parse(address);

        record.MarkProperty(nameof(IndexRecord.DownPagePointer), downPagePointerOffset, PageAddress.Size);
    }

    /// <summary>
    /// Check if a column is a Row Identifier by looking for a specific data type/length and offset
    /// </summary>
    private static bool IsRowIdentifier(ColumnStructure indexColumn, NodeType nodeType, int fixedLengthSize)
    {
        var isBinaryDataType = indexColumn.DataType == System.Data.SqlDbType.Binary;
        var hasCorrectDataLength = indexColumn.DataLength == RowIdentifier.Size;
        var hasCorrectLeafOffset = nodeType == NodeType.Leaf && indexColumn.LeafOffset == fixedLengthSize - 8;
        var hasCorrectNodeOffset = nodeType == NodeType.Node && indexColumn.NodeOffset == fixedLengthSize - 14;

        return isBinaryDataType && hasCorrectDataLength && (hasCorrectLeafOffset || hasCorrectNodeOffset);
    }

    private static void LoadColumnOffsetArray(IndexRecord record, int varColStartIndex, Page page)
    {
        var variableColumnCountOffset = record.Offset + page.PageHeader.FixedLengthSize + varColStartIndex;

        record.VariableLengthColumnCount = BitConverter.ToUInt16(page.Data, variableColumnCountOffset);

        record.MarkProperty(nameof(IndexRecord.VariableLengthColumnCount), variableColumnCountOffset, sizeof(short));

        var offset =
            record.Offset + page.PageHeader.FixedLengthSize + sizeof(short) + varColStartIndex;

        // Load offset array of 2-byte integers indicating the end offset of each variable length field
        record.VariableLengthColumnOffsetArray = RecordHelpers.GetOffsetArray(page.Data,
                                                             record.VariableLengthColumnCount,
                                                             offset);

        record.MarkProperty(nameof(IndexRecord.VariableLengthColumnOffsetArray),
                            variableColumnCountOffset + sizeof(short),
                            record.VariableLengthColumnCount * sizeof(short));
    }

    private void LoadColumnValues(IndexRecord record,
                                  PageData page,
                                  List<IndexColumnStructure> columns,
                                  NodeType nodeType,
                                  bool isUniqueIndex)
    {
        var columnValues = new List<FixedVarRecordField>();

        foreach (var column in columns)
        {
            var columnOffset = nodeType == NodeType.Leaf ? column.LeafOffset : column.NodeOffset;

            var field = new FixedVarRecordField(column);

            if (record.HasNullBitmap && record.IsNullBitmapSet(column, 0))
            {
                // Null bitmap is set
                field = LoadNullField(column);
            }
            else if ((nodeType == NodeType.Root || nodeType == NodeType.Node) && record.Slot == 0)
            {
                // The first slot in a root (level 1) index page is always null for non-unique indexes
                field = LoadNullField(column);
            }
            else if (columnOffset >= 0)
            {
                if (IsRowIdentifier(column, nodeType, page.PageHeader.FixedLengthSize))
                {
                    LoadRidField(columnOffset, record, page.Data);
                }
                else
                {
                    // Fixed length field
                    field = LoadFixedLengthField(columnOffset, column, record, page.Data);
                }
            }
            else if (column.IsUniqueifier)
            {
                field = LoadUniqueifier(columnOffset, column, record, page.Data);
            }
            else if (record.HasVariableLengthColumns)
            {
                // Variable length field
                field = LoadVariableLengthField(columnOffset, column, record, page.Data);
            }

            columnValues.Add(field);
        }

        record.Fields.AddRange(columnValues);
    }

    private static FixedVarRecordField LoadNullField(IndexColumnStructure column)
    {
        var nullField = new FixedVarRecordField(column);

        nullField.MarkProperty(nameof(FixedVarRecordField.Value));

        return nullField;
    }

    private static void LoadRidField(int offset, IndexRecord record, byte[] pageData)
    {
        var ridAddress = new byte[8];

        Array.Copy(pageData, record.Offset + offset, ridAddress, 0, RowIdentifier.Size);

        record.Rid = new RowIdentifier(ridAddress);

        record.MarkProperty(nameof(IndexRecord.Rid), record.Offset + offset, RowIdentifier.Size);
    }

    private static FixedVarRecordField LoadVariableLengthField(short columnOffset, ColumnStructure column, IndexRecord record, byte[] pageData)
    {
        int length;

        var field = new FixedVarRecordField(column);

        var variableIndex = Math.Abs(columnOffset) - 1;

        var offset = GetVariableLengthOffset(record, variableIndex);

        if (variableIndex >= record.VariableLengthColumnOffsetArray.Length)
        {
            length = 0;
        }
        else
        {
            length = record.VariableLengthColumnOffsetArray[variableIndex] - offset;
        }

        var data = new byte[length];

        Array.Copy(pageData, offset + record.Offset, data, 0, length);

        field.Offset = offset;
        field.Length = length;
        field.Data = data;
        field.VariableOffset = variableIndex;

        record.MarkValue(ItemType.VariableLengthValue, column.ColumnName, field, record.Offset + field.Offset, field.Length);

        return field;
    }

    private static ushort GetVariableLengthOffset(IndexRecord record, int variableIndex)
    {
        ushort offset;

        if (variableIndex == 0)
        {
            // If position 0 the start of the data will be at the variable length data offset...
            offset = record.VariableLengthDataOffset;
        }
        else
        {
            // ...else use the end offset of the previous column as the start of this one
            offset = record.VariableLengthColumnOffsetArray[variableIndex - 1];
        }

        return offset;
    }

    private static FixedVarRecordField LoadUniqueifier(short columnOffset, IndexColumnStructure column, IndexRecord record, byte[] pageData)
    {
        var field = new FixedVarRecordField(column);

        var uniqueifierIndex = Math.Abs(columnOffset) - 1;

        if (uniqueifierIndex >= record.VariableLengthColumnCount)
        {
            // If there is no slot for the uniqueifier it can be taken as zero
            return field;
        }

        var offset = GetVariableLengthOffset(record, uniqueifierIndex);

        // Uniqueifier is always a 4-byte integer
        var length = sizeof(int);

        var data = new byte[length];

        Array.Copy(pageData, offset + record.Offset, data, 0, length);

        field.Offset = offset;
        field.Length = length;
        field.Data = data;

        record.MarkValue(ItemType.UniquifierIndex, "Uniquifier", field, record.Offset + field.Offset, field.Length);

        return field;
    }

    /// <summary>
    /// Loads Fixed Length Fields into a new Record Field
    /// </summary>
    /// <remarks>
    /// Fixed length fields are based on the length of the field defined in the table structure.
    /// </remarks>
    private static FixedVarRecordField LoadFixedLengthField(short offset, ColumnStructure column, Record record, byte[] pageData)
    {
        var field = new FixedVarRecordField(column);

        // Length fixed from data type/data length
        var length = column.DataLength;

        var data = new byte[length];

        field.Offset = offset;
        field.Length = length;

        Array.Copy(pageData, record.Offset + field.Offset, data, 0, length);

        field.Data = data;

        record.MarkValue(ItemType.FixedLengthValue,
                         column.ColumnName,
                         field,
                         record.Offset + field.Offset,
                         field.Length);

        return field;
    }

    private static void LoadNullBitmap(IndexRecord record, PageData page, IndexStructure structure)
    {
        record.NullBitmapSize = (short)((structure.Columns.Count - 1) / 8 + 1);

        var columnCountPosition = record.Offset + page.PageHeader.FixedLengthSize;

        record.ColumnCount = BitConverter.ToInt16(page.Data, columnCountPosition);

        record.MarkProperty(nameof(IndexRecord.ColumnCount), columnCountPosition, sizeof(short));

        var nullBitmapBytes = new byte[record.NullBitmapSize];

        var nullBitmapPosition = record.Offset + page.PageHeader.FixedLengthSize + sizeof(short);

        Array.Copy(page.Data,
                   nullBitmapPosition,
                   nullBitmapBytes,
                   0,
                   record.NullBitmapSize);

        record.NullBitmap = new BitArray(nullBitmapBytes);

        record.MarkProperty(nameof(IndexRecord.NullBitmap), nullBitmapPosition, record.NullBitmapSize);
    }
}