﻿namespace InternalsViewer.Internals.Engine.Annotations;

public enum DataStructureItemType
{
    ColumnOffsetArray,
    StatusBitsA,
    StatusBitsB,
    ColumnCount,
    ColumnCountOffset,
    VariableLengthColumnCount,
    NullBitmap,
    ForwardingStub,
    DownPagePointer,
    Rid,
    SparseColumns,
    SparseColumnOffsets,
    SparseColumnCount,
    ComplexHeader,
    BlobData,
    BlobId,
    BlobLength,
    BlobType,
    MaxLinks,
    CurrentLinks,
    Level,
    BlobSize,
    CompressedValue,
    SlotOffset,
    Value,
    BlobChildOffset,
    BlobChildLength,
    PageModCount,
    CiSize,
    CiLength,
    Timestamp,
    PointerType,
    EntryCount,
    OverflowLength,
    Unused,
    UpdateSeq,
    CdArrayItem,
    SlotCount,
    Uniqueifier,
    PageAddress,

    // Header
    HeaderPageAddress,
    PageType,
    NextPage,
    PreviousPage,
    InternalObjectId,
    InternalIndexId,
    IndexLevel,
    FreeCount,
    FreeData,
    FixedLengthSize,
    PageSlotCount,
    ReservedCount,
    TransactionReservedCount,
    TornBits,
    FlagBits,
    Lsn,
    HeaderVersion,
    GhostRecordCount,
    TypeFlagBits,
    InternalTransactionId,

    AllocationUnitId,
    AnchorRecord,
    CompressionDictionary,
    CiHeader
}