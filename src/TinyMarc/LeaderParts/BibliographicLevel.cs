namespace TinyMarc;

/// <summary>
/// MARC-21 bibliographic level codes (Leader position 7).
/// </summary>
public enum BibliographicLevel
{
    /// <summary>Monographic component part (a)</summary>
    MonographicComponentPart = 'a',

    /// <summary>Serial component part (b)</summary>
    SerialComponentPart = 'b',

    /// <summary>Collection (c)</summary>
    Collection = 'c',

    /// <summary>Subunit (d)</summary>
    Subunit = 'd',

    /// <summary>Integrating resource (i)</summary>
    IntegratingResource = 'i',

    /// <summary>Monograph/Item (m)</summary>
    Monograph = 'm',

    /// <summary>Serial (s)</summary>
    Serial = 's'
}
