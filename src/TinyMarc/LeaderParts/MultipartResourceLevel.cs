namespace TinyMarc;

/// <summary>
/// MARC-21 multipart resource record level codes (Leader position 19).
/// </summary>
public enum MultipartResourceLevel
{
    /// <summary>Not specified or not applicable (space)</summary>
    NotSpecified = ' ',

    /// <summary>Set (a)</summary>
    Set = 'a',

    /// <summary>Part with independent title (b)</summary>
    PartWithIndependentTitle = 'b',

    /// <summary>Part with dependent title (c)</summary>
    PartWithDependentTitle = 'c'
}
