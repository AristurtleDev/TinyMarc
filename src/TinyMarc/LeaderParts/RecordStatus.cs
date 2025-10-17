namespace TinyMarc;

/// <summary>
/// MARC-21 record status codes (Leader position 5).
/// </summary>
public enum RecordStatus
{
    /// <summary>Increase in encoding level (a)</summary>
    IncreaseInEncodingLevel = 'a',

    /// <summary>Corrected or revised (c)</summary>
    Corrected = 'c',

    /// <summary>Deleted (d)</summary>
    Deleted = 'd',

    /// <summary>New (n)</summary>
    New = 'n',

    /// <summary>Increase in encoding level from prepublication (p)</summary>
    IncreaseFromPrepublication = 'p'
}
