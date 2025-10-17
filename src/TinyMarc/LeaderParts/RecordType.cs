namespace TinyMarc;

/// <summary>
/// MARC-21 record type codes (Leader position 6).
/// </summary>
public enum RecordType
{
    /// <summary>Language material (a)</summary>
    LanguageMaterial = 'a',

    /// <summary>Notated music (c)</summary>
    NotatedMusic = 'c',

    /// <summary>Manuscript notated music (d)</summary>
    ManuscriptNotatedMusic = 'd',

    /// <summary>Cartographic material (e)</summary>
    CartographicMaterial = 'e',

    /// <summary>Manuscript cartographic material (f)</summary>
    ManuscriptCartographicMaterial = 'f',

    /// <summary>Projected medium (g)</summary>
    ProjectedMedium = 'g',

    /// <summary>Nonmusical sound recording (i)</summary>
    NonmusicalSoundRecording = 'i',

    /// <summary>Musical sound recording (j)</summary>
    MusicalSoundRecording = 'j',

    /// <summary>Two-dimensional nonprojectable graphic (k)</summary>
    TwoDimensionalGraphic = 'k',

    /// <summary>Computer file (m)</summary>
    ComputerFile = 'm',

    /// <summary>Kit (o)</summary>
    Kit = 'o',

    /// <summary>Mixed materials (p)</summary>
    MixedMaterials = 'p',

    /// <summary>Three-dimensional artifact or naturally occurring object (r)</summary>
    ThreeDimensionalArtifact = 'r',

    /// <summary>Manuscript language material (t)</summary>
    ManuscriptLanguageMaterial = 't'
}
