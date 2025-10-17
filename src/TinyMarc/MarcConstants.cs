// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace TinyMarc;

/// <summary>
/// Defines constants used in MARC-21 record processing.
/// </summary>
public static class MarcConstants
{
    /// <summary>
    /// Subfield indicator character (ASCII 0x1F).
    /// </summary>
    public const char SubfieldIndicator = '\x1F';

    /// <summary>
    /// End of field character (ASCII 0x1E).
    /// </summary>
    public const char EndOfField = '\x1E';

    /// <summary>
    /// End of record character (ASCII 0x1D).
    /// </summary>
    public const char EndOfRecord = '\x1D';

    /// <summary>
    /// Length of a directory entry in bytes.
    /// </summary>
    public const int DirectoryEntryLength = 12;

    /// <summary>
    /// Length of the leader in bytes.
    /// </summary>
    public const int LeaderLength = 24;

    /// <summary>
    /// Maximum allowed record length in bytes.
    /// </summary>
    public const int MaxRecordLength = 99999;
}
