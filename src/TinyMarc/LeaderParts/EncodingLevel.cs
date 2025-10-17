namespace TinyMarc;

/// <summary>
/// MARC-21 encoding level codes (Leader position 17).
/// </summary>
public enum EncodingLevel
{
    /// <summary>Full level (space)</summary>
    FullLevel = ' ',

    /// <summary>Full level, material not examined (1)</summary>
    FullLevelNotExamined = '1',

    /// <summary>Less-than-full level, material not examined (2)</summary>
    LessThanFullNotExamined = '2',

    /// <summary>Abbreviated level (3)</summary>
    AbbreviatedLevel = '3',

    /// <summary>Core level (4)</summary>
    CoreLevel = '4',

    /// <summary>Partial (preliminary) level (5)</summary>
    PartialLevel = '5',

    /// <summary>Minimal level (7)</summary>
    MinimalLevel = '7',

    /// <summary>Prepublication level (8)</summary>
    PrepublicationLevel = '8',

    /// <summary>Unknown (u)</summary>
    Unknown = 'u',

    /// <summary>Not applicable (z)</summary>
    NotApplicable = 'z'
}
