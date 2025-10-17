namespace TinyMarc;

/// <summary>
/// MARC-21 descriptive cataloging form codes (Leader position 18).
/// </summary>
public enum DescriptiveCatalogingForm
{
    /// <summary>Non-ISBD (space)</summary>
    NonISBD = ' ',

    /// <summary>AACR 2 (a)</summary>
    AACR2 = 'a',

    /// <summary>ISBD punctuation omitted (c)</summary>
    ISBDPunctuationOmitted = 'c',

    /// <summary>ISBD punctuation included (i)</summary>
    ISBDPunctuationIncluded = 'i',

    /// <summary>Non-ISBD punctuation omitted (n)</summary>
    NonISBDPunctuationOmitted = 'n',

    /// <summary>Unknown (u)</summary>
    Unknown = 'u'
}
