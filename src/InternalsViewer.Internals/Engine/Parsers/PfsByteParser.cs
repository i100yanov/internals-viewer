﻿using System.Collections;
using InternalsViewer.Internals.Engine.Allocation.Enums;

namespace InternalsViewer.Internals.Engine.Parsers;

public class PfsByteParser
{
    /// <summary>
    /// Parses a PFS byte
    /// </summary>
    /// <remarks>
    ///  Byte is expressed as MSb - Most Significant Bit first so smaller bits are on the right
    ///  
    ///     PFS bits are as follows:
    ///     
    ///     Bits 87654 321
    ///          00000 000
    ///     
    ///     Bits 1-3 - Space Free value
    ///         
    ///         321
    ///         ---
    ///         000 - Empty
    ///         001 - 50%
    ///         010 - 80%
    ///         011 - 95%
    ///         100 - 100%
    ///         
    ///     Bit 4 - Is Ghost record
    ///     Bit 5 - Is IAM page
    ///     Bit 6 - Is Mixed Extent
    ///     Bit 7 - Is Allocated
    ///     
    ///     Bit 8 - Unused
    /// </remarks>
    public static PfsByte Parse(byte pageByte)
    {
        var bitArray = new BitArray(new[] { pageByte });

        var pfsByte = new PfsByte
        {
            Value = pageByte,
            GhostRecords = bitArray[3],
            IsIam = bitArray[4],
            IsMixed = bitArray[5],
            IsAllocated = bitArray[6],
            PageSpaceFree = (SpaceFree)(pageByte & 7)
        };

        return pfsByte;
    }

    public static byte Parse(PfsByte pfsByte)
    {
        var bitArray = new BitArray(new [] { (byte)pfsByte.PageSpaceFree });

        bitArray[3] = pfsByte.GhostRecords;
        bitArray[4] = pfsByte.IsIam;
        bitArray[5] = pfsByte.IsMixed;
        bitArray[6] = pfsByte.IsAllocated;

        var returnByte = new byte[1];

        bitArray.CopyTo(returnByte, 0);

        return returnByte[0];
    }
}
