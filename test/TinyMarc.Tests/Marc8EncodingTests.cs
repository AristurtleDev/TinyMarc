namespace TinyMarc.Tests;

/// <summary>
/// Comprehensive tests for MARC-8 to Unicode encoding and decoding.
/// </summary>
public class Marc8EncodingTests
{
    private readonly Marc8Encoding _encoding = new Marc8Encoding();

    #region Basic ASCII Tests

    [Theory]
    [InlineData("Hello World", new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64 })]
    [InlineData("ABC123", new byte[] { 0x41, 0x42, 0x43, 0x31, 0x32, 0x33 })]
    [InlineData("", new byte[] { })]
    [InlineData(" ", new byte[] { 0x20 })]
    public void GetBytes_WithAscii_ReturnsExpectedBytes(string input, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, "Hello")]
    [InlineData(new byte[] { 0x41, 0x42, 0x43 }, "ABC")]
    [InlineData(new byte[] { }, "")]
    [InlineData(new byte[] { 0x20 }, " ")]
    public void GetChars_WithAsciiBytes_ReturnsExpectedString(byte[] input, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(input, 0, input.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    #endregion

    #region Single-Byte Special Characters

    [Theory]
    [InlineData(new byte[] { 0x88 }, "\u0098")]  // START OF STRING
    [InlineData(new byte[] { 0x89 }, "\u009C")]  // STRING TERMINATOR
    [InlineData(new byte[] { 0xA2 }, "\u00D8")]  // Ø
    [InlineData(new byte[] { 0xA4 }, "\u00DE")]  // Þ
    [InlineData(new byte[] { 0xA5 }, "\u00C6")]  // Æ
    [InlineData(new byte[] { 0xA8 }, "\u00B7")]  // ·
    [InlineData(new byte[] { 0xAA }, "\u00AE")]  // ®
    [InlineData(new byte[] { 0xAB }, "\u00B1")]  // ±
    [InlineData(new byte[] { 0xB9 }, "\u00A3")]  // £
    [InlineData(new byte[] { 0xC0 }, "\u00B0")]  // °
    [InlineData(new byte[] { 0xC3 }, "\u00A9")]  // ©
    [InlineData(new byte[] { 0xC5 }, "\u00BF")]  // ¿
    [InlineData(new byte[] { 0xC6 }, "\u00A1")]  // ¡
    [InlineData(new byte[] { 0xC7 }, "\u00DF")]  // ß
    public void GetChars_WithSingleByteSpecialCharacters_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    [Theory]
    [InlineData("\u00D8", new byte[] { 0xA2 })]  // Ø
    [InlineData("\u00DE", new byte[] { 0xA4 })]  // Þ
    [InlineData("\u00C6", new byte[] { 0xA5 })]  // Æ
    [InlineData("\u00A9", new byte[] { 0xC3 })]  // ©
    [InlineData("\u00DF", new byte[] { 0xC7 })]  // ß
    public void GetBytes_WithSingleByteSpecialCharacters_ReturnsExpectedMarc8(string unicode, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(unicode);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Hook Above (0xE0)

    [Theory]
    [InlineData(new byte[] { 0xE0, 0x41 }, "\u1EA2")]  // Ả
    [InlineData(new byte[] { 0xE0, 0x45 }, "\u1EBA")]  // Ẻ
    [InlineData(new byte[] { 0xE0, 0x49 }, "\u1EC8")]  // Ỉ
    [InlineData(new byte[] { 0xE0, 0x4F }, "\u1ECE")]  // Ỏ
    [InlineData(new byte[] { 0xE0, 0x55 }, "\u1EE6")]  // Ủ
    [InlineData(new byte[] { 0xE0, 0x59 }, "\u1EF6")]  // Ỷ
    [InlineData(new byte[] { 0xE0, 0x61 }, "\u1EA3")]  // ả
    [InlineData(new byte[] { 0xE0, 0x65 }, "\u1EBB")]  // ẻ
    [InlineData(new byte[] { 0xE0, 0x69 }, "\u1EC9")]  // ỉ
    [InlineData(new byte[] { 0xE0, 0x6F }, "\u1ECF")]  // ỏ
    [InlineData(new byte[] { 0xE0, 0x75 }, "\u1EE7")]  // ủ
    [InlineData(new byte[] { 0xE0, 0x79 }, "\u1EF7")]  // ỷ
    public void GetChars_WithHookAbove_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    #endregion

    #region Grave Accent (0xE1)

    [Theory]
    [InlineData(new byte[] { 0xE1, 0x41 }, "\u00C0")]  // À
    [InlineData(new byte[] { 0xE1, 0x45 }, "\u00C8")]  // È
    [InlineData(new byte[] { 0xE1, 0x49 }, "\u00CC")]  // Ì
    [InlineData(new byte[] { 0xE1, 0x4F }, "\u00D2")]  // Ò
    [InlineData(new byte[] { 0xE1, 0x55 }, "\u00D9")]  // Ù
    [InlineData(new byte[] { 0xE1, 0x57 }, "\u1E80")]  // Ẁ
    [InlineData(new byte[] { 0xE1, 0x59 }, "\u1EF2")]  // Ỳ
    [InlineData(new byte[] { 0xE1, 0x61 }, "\u00E0")]  // à
    [InlineData(new byte[] { 0xE1, 0x65 }, "\u00E8")]  // è
    [InlineData(new byte[] { 0xE1, 0x69 }, "\u00EC")]  // ì
    [InlineData(new byte[] { 0xE1, 0x6F }, "\u00F2")]  // ò
    [InlineData(new byte[] { 0xE1, 0x75 }, "\u00F9")]  // ù
    [InlineData(new byte[] { 0xE1, 0x77 }, "\u1E81")]  // ẁ
    [InlineData(new byte[] { 0xE1, 0x79 }, "\u1EF3")]  // ỳ
    public void GetChars_WithGraveAccent_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    [Theory]
    [InlineData("\u00C0", new byte[] { 0xE1, 0x41 })]  // À
    [InlineData("\u00E8", new byte[] { 0xE1, 0x65 })]  // è
    [InlineData("\u00F9", new byte[] { 0xE1, 0x75 })]  // ù
    public void GetBytes_WithGraveAccent_ReturnsExpectedMarc8(string unicode, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(unicode);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Acute Accent (0xE2)

    [Theory]
    [InlineData(new byte[] { 0xE2, 0x41 }, "\u00C1")]  // Á
    [InlineData(new byte[] { 0xE2, 0x43 }, "\u0106")]  // Ć
    [InlineData(new byte[] { 0xE2, 0x45 }, "\u00C9")]  // É
    [InlineData(new byte[] { 0xE2, 0x49 }, "\u00CD")]  // Í
    [InlineData(new byte[] { 0xE2, 0x4C }, "\u0139")]  // Ĺ
    [InlineData(new byte[] { 0xE2, 0x4E }, "\u0143")]  // Ń
    [InlineData(new byte[] { 0xE2, 0x4F }, "\u00D3")]  // Ó
    [InlineData(new byte[] { 0xE2, 0x53 }, "\u015A")]  // Ś
    [InlineData(new byte[] { 0xE2, 0x55 }, "\u00DA")]  // Ú
    [InlineData(new byte[] { 0xE2, 0x59 }, "\u00DD")]  // Ý
    [InlineData(new byte[] { 0xE2, 0x5A }, "\u0179")]  // Ź
    [InlineData(new byte[] { 0xE2, 0x61 }, "\u00E1")]  // á
    [InlineData(new byte[] { 0xE2, 0x63 }, "\u0107")]  // ć
    [InlineData(new byte[] { 0xE2, 0x65 }, "\u00E9")]  // é
    [InlineData(new byte[] { 0xE2, 0x69 }, "\u00ED")]  // í
    [InlineData(new byte[] { 0xE2, 0x6F }, "\u00F3")]  // ó
    [InlineData(new byte[] { 0xE2, 0x75 }, "\u00FA")]  // ú
    [InlineData(new byte[] { 0xE2, 0x79 }, "\u00FD")]  // ý
    public void GetChars_WithAcuteAccent_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    [Theory]
    [InlineData("\u00C1", new byte[] { 0xE2, 0x41 })]  // Á
    [InlineData("\u00E9", new byte[] { 0xE2, 0x65 })]  // é
    [InlineData("\u00FA", new byte[] { 0xE2, 0x75 })]  // ú
    public void GetBytes_WithAcuteAccent_ReturnsExpectedMarc8(string unicode, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(unicode);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Circumflex Accent (0xE3)

    [Theory]
    [InlineData(new byte[] { 0xE3, 0x41 }, "\u00C2")]  // Â
    [InlineData(new byte[] { 0xE3, 0x43 }, "\u0108")]  // Ĉ
    [InlineData(new byte[] { 0xE3, 0x45 }, "\u00CA")]  // Ê
    [InlineData(new byte[] { 0xE3, 0x47 }, "\u011C")]  // Ĝ
    [InlineData(new byte[] { 0xE3, 0x48 }, "\u0124")]  // Ĥ
    [InlineData(new byte[] { 0xE3, 0x49 }, "\u00CE")]  // Î
    [InlineData(new byte[] { 0xE3, 0x4A }, "\u0134")]  // Ĵ
    [InlineData(new byte[] { 0xE3, 0x4F }, "\u00D4")]  // Ô
    [InlineData(new byte[] { 0xE3, 0x53 }, "\u015C")]  // Ŝ
    [InlineData(new byte[] { 0xE3, 0x55 }, "\u00DB")]  // Û
    [InlineData(new byte[] { 0xE3, 0x57 }, "\u0174")]  // Ŵ
    [InlineData(new byte[] { 0xE3, 0x59 }, "\u0176")]  // Ŷ
    [InlineData(new byte[] { 0xE3, 0x61 }, "\u00E2")]  // â
    [InlineData(new byte[] { 0xE3, 0x65 }, "\u00EA")]  // ê
    [InlineData(new byte[] { 0xE3, 0x69 }, "\u00EE")]  // î
    [InlineData(new byte[] { 0xE3, 0x6F }, "\u00F4")]  // ô
    [InlineData(new byte[] { 0xE3, 0x75 }, "\u00FB")]  // û
    public void GetChars_WithCircumflexAccent_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    #endregion

    #region Tilde (0xE4)

    [Theory]
    [InlineData(new byte[] { 0xE4, 0x41 }, "\u00C3")]  // Ã
    [InlineData(new byte[] { 0xE4, 0x4E }, "\u00D1")]  // Ñ
    [InlineData(new byte[] { 0xE4, 0x4F }, "\u00D5")]  // Õ
    [InlineData(new byte[] { 0xE4, 0x61 }, "\u00E3")]  // ã
    [InlineData(new byte[] { 0xE4, 0x6E }, "\u00F1")]  // ñ
    [InlineData(new byte[] { 0xE4, 0x6F }, "\u00F5")]  // õ
    public void GetChars_WithTilde_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    [Theory]
    [InlineData("\u00D1", new byte[] { 0xE4, 0x4E })]  // Ñ
    [InlineData("\u00F1", new byte[] { 0xE4, 0x6E })]  // ñ
    public void GetBytes_WithTilde_ReturnsExpectedMarc8(string unicode, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(unicode);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Macron (0xE5)

    [Theory]
    [InlineData(new byte[] { 0xE5, 0x41 }, "\u0100")]  // Ā
    [InlineData(new byte[] { 0xE5, 0x45 }, "\u0112")]  // Ē
    [InlineData(new byte[] { 0xE5, 0x49 }, "\u012A")]  // Ī
    [InlineData(new byte[] { 0xE5, 0x4F }, "\u014C")]  // Ō
    [InlineData(new byte[] { 0xE5, 0x55 }, "\u016A")]  // Ū
    [InlineData(new byte[] { 0xE5, 0x61 }, "\u0101")]  // ā
    [InlineData(new byte[] { 0xE5, 0x65 }, "\u0113")]  // ē
    [InlineData(new byte[] { 0xE5, 0x69 }, "\u012B")]  // ī
    [InlineData(new byte[] { 0xE5, 0x6F }, "\u014D")]  // ō
    [InlineData(new byte[] { 0xE5, 0x75 }, "\u016B")]  // ū
    public void GetChars_WithMacron_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    #endregion

    #region Breve (0xE6)

    [Theory]
    [InlineData(new byte[] { 0xE6, 0x41 }, "\u0102")]  // Ă
    [InlineData(new byte[] { 0xE6, 0x47 }, "\u011E")]  // Ğ
    [InlineData(new byte[] { 0xE6, 0x55 }, "\u016C")]  // Ŭ
    [InlineData(new byte[] { 0xE6, 0x61 }, "\u0103")]  // ă
    [InlineData(new byte[] { 0xE6, 0x67 }, "\u011F")]  // ğ
    [InlineData(new byte[] { 0xE6, 0x75 }, "\u016D")]  // ŭ
    public void GetChars_WithBreve_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    #endregion

    #region Dot Above (0xE7)

    [Theory]
    [InlineData(new byte[] { 0xE7, 0x43 }, "\u010A")]  // Ċ
    [InlineData(new byte[] { 0xE7, 0x45 }, "\u0116")]  // Ė
    [InlineData(new byte[] { 0xE7, 0x47 }, "\u0120")]  // Ġ
    [InlineData(new byte[] { 0xE7, 0x49 }, "\u0130")]  // İ
    [InlineData(new byte[] { 0xE7, 0x5A }, "\u017B")]  // Ż
    [InlineData(new byte[] { 0xE7, 0x63 }, "\u010B")]  // ċ
    [InlineData(new byte[] { 0xE7, 0x65 }, "\u0117")]  // ė
    [InlineData(new byte[] { 0xE7, 0x67 }, "\u0121")]  // ġ
    [InlineData(new byte[] { 0xE7, 0x7A }, "\u017C")]  // ż
    public void GetChars_WithDotAbove_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    #endregion

    #region Diaeresis (0xE8)

    [Theory]
    [InlineData(new byte[] { 0xE8, 0x41 }, "\u00C4")]  // Ä
    [InlineData(new byte[] { 0xE8, 0x45 }, "\u00CB")]  // Ë
    [InlineData(new byte[] { 0xE8, 0x49 }, "\u00CF")]  // Ï
    [InlineData(new byte[] { 0xE8, 0x4F }, "\u00D6")]  // Ö
    [InlineData(new byte[] { 0xE8, 0x55 }, "\u00DC")]  // Ü
    [InlineData(new byte[] { 0xE8, 0x59 }, "\u0178")]  // Ÿ
    [InlineData(new byte[] { 0xE8, 0x61 }, "\u00E4")]  // ä
    [InlineData(new byte[] { 0xE8, 0x65 }, "\u00EB")]  // ë
    [InlineData(new byte[] { 0xE8, 0x69 }, "\u00EF")]  // ï
    [InlineData(new byte[] { 0xE8, 0x6F }, "\u00F6")]  // ö
    [InlineData(new byte[] { 0xE8, 0x75 }, "\u00FC")]  // ü
    [InlineData(new byte[] { 0xE8, 0x79 }, "\u00FF")]  // ÿ
    public void GetChars_WithDiaeresis_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    [Theory]
    [InlineData("\u00C4", new byte[] { 0xE8, 0x41 })]  // Ä
    [InlineData("\u00F6", new byte[] { 0xE8, 0x6F })]  // ö
    [InlineData("\u00FC", new byte[] { 0xE8, 0x75 })]  // ü
    public void GetBytes_WithDiaeresis_ReturnsExpectedMarc8(string unicode, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(unicode);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Caron (0xE9)

    [Theory]
    [InlineData(new byte[] { 0xE9, 0x43 }, "\u010C")]  // Č
    [InlineData(new byte[] { 0xE9, 0x44 }, "\u010E")]  // Ď
    [InlineData(new byte[] { 0xE9, 0x45 }, "\u011A")]  // Ě
    [InlineData(new byte[] { 0xE9, 0x4E }, "\u0147")]  // Ň
    [InlineData(new byte[] { 0xE9, 0x52 }, "\u0158")]  // Ř
    [InlineData(new byte[] { 0xE9, 0x53 }, "\u0160")]  // Š
    [InlineData(new byte[] { 0xE9, 0x54 }, "\u0164")]  // Ť
    [InlineData(new byte[] { 0xE9, 0x5A }, "\u017D")]  // Ž
    [InlineData(new byte[] { 0xE9, 0x63 }, "\u010D")]  // č
    [InlineData(new byte[] { 0xE9, 0x64 }, "\u010F")]  // ď
    [InlineData(new byte[] { 0xE9, 0x65 }, "\u011B")]  // ě
    [InlineData(new byte[] { 0xE9, 0x6E }, "\u0148")]  // ň
    [InlineData(new byte[] { 0xE9, 0x72 }, "\u0159")]  // ř
    [InlineData(new byte[] { 0xE9, 0x73 }, "\u0161")]  // š
    [InlineData(new byte[] { 0xE9, 0x74 }, "\u0165")]  // ť
    [InlineData(new byte[] { 0xE9, 0x7A }, "\u017E")]  // ž
    public void GetChars_WithCaron_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    [Theory]
    [InlineData("\u010C", new byte[] { 0xE9, 0x43 })]  // Č
    [InlineData("\u0161", new byte[] { 0xE9, 0x73 })]  // š
    [InlineData("\u017E", new byte[] { 0xE9, 0x7A })]  // ž
    public void GetBytes_WithCaron_ReturnsExpectedMarc8(string unicode, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(unicode);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Ring Above (0xEA)

    [Theory]
    [InlineData(new byte[] { 0xEA, 0x41 }, "\u00C5")]  // Å
    [InlineData(new byte[] { 0xEA, 0x55 }, "\u016E")]  // Ů
    [InlineData(new byte[] { 0xEA, 0x61 }, "\u00E5")]  // å
    [InlineData(new byte[] { 0xEA, 0x75 }, "\u016F")]  // ů
    public void GetChars_WithRingAbove_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    #endregion

    #region Double Acute (0xEE)

    [Theory]
    [InlineData(new byte[] { 0xEE, 0x4F }, "\u0150")]  // Ő
    [InlineData(new byte[] { 0xEE, 0x55 }, "\u0170")]  // Ű
    [InlineData(new byte[] { 0xEE, 0x6F }, "\u0151")]  // ő
    [InlineData(new byte[] { 0xEE, 0x75 }, "\u0171")]  // ű
    public void GetChars_WithDoubleAcute_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    #endregion

    #region Cedilla (0xF0)

    [Theory]
    [InlineData(new byte[] { 0xF0, 0x43 }, "\u00C7")]  // Ç
    [InlineData(new byte[] { 0xF0, 0x47 }, "\u0122")]  // Ģ
    [InlineData(new byte[] { 0xF0, 0x4B }, "\u0136")]  // Ķ
    [InlineData(new byte[] { 0xF0, 0x4C }, "\u013B")]  // Ļ
    [InlineData(new byte[] { 0xF0, 0x4E }, "\u0145")]  // Ņ
    [InlineData(new byte[] { 0xF0, 0x52 }, "\u0156")]  // Ŗ
    [InlineData(new byte[] { 0xF0, 0x53 }, "\u015E")]  // Ş
    [InlineData(new byte[] { 0xF0, 0x54 }, "\u0162")]  // Ţ
    [InlineData(new byte[] { 0xF0, 0x63 }, "\u00E7")]  // ç
    [InlineData(new byte[] { 0xF0, 0x67 }, "\u0123")]  // ģ
    [InlineData(new byte[] { 0xF0, 0x6B }, "\u0137")]  // ķ
    [InlineData(new byte[] { 0xF0, 0x6C }, "\u013C")]  // ļ
    [InlineData(new byte[] { 0xF0, 0x6E }, "\u0146")]  // ņ
    [InlineData(new byte[] { 0xF0, 0x72 }, "\u0157")]  // ŗ
    [InlineData(new byte[] { 0xF0, 0x73 }, "\u015F")]  // ş
    [InlineData(new byte[] { 0xF0, 0x74 }, "\u0163")]  // ţ
    public void GetChars_WithCedilla_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    [Theory]
    [InlineData("\u00C7", new byte[] { 0xF0, 0x43 })]  // Ç
    [InlineData("\u00E7", new byte[] { 0xF0, 0x63 })]  // ç
    public void GetBytes_WithCedilla_ReturnsExpectedMarc8(string unicode, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(unicode);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Ogonek (0xF1)

    [Theory]
    [InlineData(new byte[] { 0xF1, 0x41 }, "\u0104")]  // Ą
    [InlineData(new byte[] { 0xF1, 0x45 }, "\u0118")]  // Ę
    [InlineData(new byte[] { 0xF1, 0x49 }, "\u012E")]  // Į
    [InlineData(new byte[] { 0xF1, 0x55 }, "\u0172")]  // Ų
    [InlineData(new byte[] { 0xF1, 0x61 }, "\u0105")]  // ą
    [InlineData(new byte[] { 0xF1, 0x65 }, "\u0119")]  // ę
    [InlineData(new byte[] { 0xF1, 0x69 }, "\u012F")]  // į
    [InlineData(new byte[] { 0xF1, 0x75 }, "\u0173")]  // ų
    public void GetChars_WithOgonek_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    [Theory]
    [InlineData("\u0104", new byte[] { 0xF1, 0x41 })]  // Ą
    [InlineData("\u0119", new byte[] { 0xF1, 0x65 })]  // ę
    public void GetBytes_WithOgonek_ReturnsExpectedMarc8(string unicode, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(unicode);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Dot Below (0xF2)

    [Theory]
    [InlineData(new byte[] { 0xF2, 0x41 }, "\u1EA0")]  // Ạ
    [InlineData(new byte[] { 0xF2, 0x42 }, "\u1E04")]  // Ḅ
    [InlineData(new byte[] { 0xF2, 0x44 }, "\u1E0C")]  // Ḍ
    [InlineData(new byte[] { 0xF2, 0x45 }, "\u1EB8")]  // Ẹ
    [InlineData(new byte[] { 0xF2, 0x48 }, "\u1E24")]  // Ḥ
    [InlineData(new byte[] { 0xF2, 0x49 }, "\u1ECA")]  // Ị
    [InlineData(new byte[] { 0xF2, 0x4B }, "\u1E32")]  // Ḳ
    [InlineData(new byte[] { 0xF2, 0x4C }, "\u1E36")]  // Ḷ
    [InlineData(new byte[] { 0xF2, 0x4D }, "\u1E42")]  // Ṃ
    [InlineData(new byte[] { 0xF2, 0x4E }, "\u1E46")]  // Ṇ
    [InlineData(new byte[] { 0xF2, 0x4F }, "\u1ECC")]  // Ọ
    [InlineData(new byte[] { 0xF2, 0x52 }, "\u1E5A")]  // Ṛ
    [InlineData(new byte[] { 0xF2, 0x53 }, "\u1E62")]  // Ṣ
    [InlineData(new byte[] { 0xF2, 0x54 }, "\u1E6C")]  // Ṭ
    [InlineData(new byte[] { 0xF2, 0x55 }, "\u1EE4")]  // Ụ
    [InlineData(new byte[] { 0xF2, 0x56 }, "\u1E7E")]  // Ṿ
    [InlineData(new byte[] { 0xF2, 0x57 }, "\u1E88")]  // Ẉ
    [InlineData(new byte[] { 0xF2, 0x59 }, "\u1EF4")]  // Ỵ
    [InlineData(new byte[] { 0xF2, 0x5A }, "\u1E92")]  // Ẓ
    [InlineData(new byte[] { 0xF2, 0x61 }, "\u1EA1")]  // ạ
    [InlineData(new byte[] { 0xF2, 0x62 }, "\u1E05")]  // ḅ
    [InlineData(new byte[] { 0xF2, 0x64 }, "\u1E0D")]  // ḍ
    [InlineData(new byte[] { 0xF2, 0x65 }, "\u1EB9")]  // ẹ
    [InlineData(new byte[] { 0xF2, 0x68 }, "\u1E25")]  // ḥ
    [InlineData(new byte[] { 0xF2, 0x69 }, "\u1ECB")]  // ị
    [InlineData(new byte[] { 0xF2, 0x6B }, "\u1E33")]  // ḳ
    [InlineData(new byte[] { 0xF2, 0x6C }, "\u1E37")]  // ḷ
    [InlineData(new byte[] { 0xF2, 0x6D }, "\u1E43")]  // ṃ
    [InlineData(new byte[] { 0xF2, 0x6E }, "\u1E47")]  // ṇ
    [InlineData(new byte[] { 0xF2, 0x6F }, "\u1ECD")]  // ọ
    [InlineData(new byte[] { 0xF2, 0x72 }, "\u1E5B")]  // ṛ
    [InlineData(new byte[] { 0xF2, 0x73 }, "\u1E63")]  // ṣ
    [InlineData(new byte[] { 0xF2, 0x74 }, "\u1E6D")]  // ṭ
    [InlineData(new byte[] { 0xF2, 0x75 }, "\u1EE5")]  // ụ
    [InlineData(new byte[] { 0xF2, 0x76 }, "\u1E7F")]  // ṿ
    [InlineData(new byte[] { 0xF2, 0x77 }, "\u1E89")]  // ẉ
    [InlineData(new byte[] { 0xF2, 0x79 }, "\u1EF5")]  // ỵ
    [InlineData(new byte[] { 0xF2, 0x7A }, "\u1E93")]  // ẓ
    public void GetChars_WithDotBelow_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    #endregion

    #region Three-Byte Sequences

    [Theory]
    // Hook Above + Circumflex
    [InlineData(new byte[] { 0xE0, 0xE3, 0x41 }, "\u1EA8")]  // Ẩ
    [InlineData(new byte[] { 0xE0, 0xE3, 0x45 }, "\u1EC2")]  // Ể
    [InlineData(new byte[] { 0xE0, 0xE3, 0x4F }, "\u1ED4")]  // Ổ
    [InlineData(new byte[] { 0xE0, 0xE3, 0x61 }, "\u1EA9")]  // ẩ
    [InlineData(new byte[] { 0xE0, 0xE3, 0x65 }, "\u1EC3")]  // ể
    [InlineData(new byte[] { 0xE0, 0xE3, 0x6F }, "\u1ED5")]  // ổ
    // Hook Above + Breve
    [InlineData(new byte[] { 0xE0, 0xE6, 0x41 }, "\u1EB2")]  // Ẳ
    [InlineData(new byte[] { 0xE0, 0xE6, 0x61 }, "\u1EB3")]  // ẳ
    // Grave + Circumflex
    [InlineData(new byte[] { 0xE1, 0xE3, 0x41 }, "\u1EA6")]  // Ầ
    [InlineData(new byte[] { 0xE1, 0xE3, 0x45 }, "\u1EC0")]  // Ề
    [InlineData(new byte[] { 0xE1, 0xE3, 0x4F }, "\u1ED2")]  // Ồ
    [InlineData(new byte[] { 0xE1, 0xE3, 0x61 }, "\u1EA7")]  // ầ
    [InlineData(new byte[] { 0xE1, 0xE3, 0x65 }, "\u1EC1")]  // ề
    [InlineData(new byte[] { 0xE1, 0xE3, 0x6F }, "\u1ED3")]  // ồ
    // Grave + Macron
    [InlineData(new byte[] { 0xE1, 0xE5, 0x45 }, "\u1E14")]  // Ḕ
    [InlineData(new byte[] { 0xE1, 0xE5, 0x4F }, "\u1E50")]  // Ṑ
    [InlineData(new byte[] { 0xE1, 0xE5, 0x65 }, "\u1E15")]  // ḕ
    [InlineData(new byte[] { 0xE1, 0xE5, 0x6F }, "\u1E51")]  // ṑ
    // Grave + Breve
    [InlineData(new byte[] { 0xE1, 0xE6, 0x41 }, "\u1EB0")]  // Ằ
    [InlineData(new byte[] { 0xE1, 0xE6, 0x61 }, "\u1EB1")]  // ằ
    // Acute + Circumflex
    [InlineData(new byte[] { 0xE2, 0xE3, 0x41 }, "\u1EA4")]  // Ấ
    [InlineData(new byte[] { 0xE2, 0xE3, 0x45 }, "\u1EBE")]  // Ế
    [InlineData(new byte[] { 0xE2, 0xE3, 0x4F }, "\u1ED0")]  // Ố
    [InlineData(new byte[] { 0xE2, 0xE3, 0x61 }, "\u1EA5")]  // ấ
    [InlineData(new byte[] { 0xE2, 0xE3, 0x65 }, "\u1EBF")]  // ế
    [InlineData(new byte[] { 0xE2, 0xE3, 0x6F }, "\u1ED1")]  // ố
    // Acute + Tilde
    [InlineData(new byte[] { 0xE2, 0xE4, 0x4F }, "\u1E4C")]  // Ṍ
    [InlineData(new byte[] { 0xE2, 0xE4, 0x55 }, "\u1E78")]  // Ṹ
    [InlineData(new byte[] { 0xE2, 0xE4, 0x6F }, "\u1E4D")]  // ṍ
    [InlineData(new byte[] { 0xE2, 0xE4, 0x75 }, "\u1E79")]  // ṹ
    // Acute + Macron
    [InlineData(new byte[] { 0xE2, 0xE5, 0x45 }, "\u1E16")]  // Ḗ
    [InlineData(new byte[] { 0xE2, 0xE5, 0x4F }, "\u1E52")]  // Ṓ
    [InlineData(new byte[] { 0xE2, 0xE5, 0x65 }, "\u1E17")]  // ḗ
    [InlineData(new byte[] { 0xE2, 0xE5, 0x6F }, "\u1E53")]  // ṓ
    // Acute + Breve
    [InlineData(new byte[] { 0xE2, 0xE6, 0x41 }, "\u1EAE")]  // Ắ
    [InlineData(new byte[] { 0xE2, 0xE6, 0x61 }, "\u1EAF")]  // ắ
    // Acute + Diaeresis
    [InlineData(new byte[] { 0xE2, 0xE8, 0x49 }, "\u1E2E")]  // Ḯ
    [InlineData(new byte[] { 0xE2, 0xE8, 0x55 }, "\u01D7")]  // Ǘ
    [InlineData(new byte[] { 0xE2, 0xE8, 0x69 }, "\u1E2F")]  // ḯ
    [InlineData(new byte[] { 0xE2, 0xE8, 0x75 }, "\u01D8")]  // ǘ
    // Acute + Ring Above
    [InlineData(new byte[] { 0xE2, 0xEA, 0x41 }, "\u01FA")]  // Ǻ
    [InlineData(new byte[] { 0xE2, 0xEA, 0x61 }, "\u01FB")]  // ǻ
    // Circumflex + Dot Below
    [InlineData(new byte[] { 0xE3, 0xF2, 0x41 }, "\u1EAC")]  // Ậ
    [InlineData(new byte[] { 0xE3, 0xF2, 0x45 }, "\u1EC6")]  // Ệ
    [InlineData(new byte[] { 0xE3, 0xF2, 0x4F }, "\u1ED8")]  // Ộ
    [InlineData(new byte[] { 0xE3, 0xF2, 0x61 }, "\u1EAD")]  // ậ
    [InlineData(new byte[] { 0xE3, 0xF2, 0x65 }, "\u1EC7")]  // ệ
    [InlineData(new byte[] { 0xE3, 0xF2, 0x6F }, "\u1ED9")]  // ộ
    // Tilde + Circumflex
    [InlineData(new byte[] { 0xE4, 0xE3, 0x41 }, "\u1EAA")]  // Ẫ
    [InlineData(new byte[] { 0xE4, 0xE3, 0x45 }, "\u1EC4")]  // Ễ
    [InlineData(new byte[] { 0xE4, 0xE3, 0x4F }, "\u1ED6")]  // Ỗ
    [InlineData(new byte[] { 0xE4, 0xE3, 0x61 }, "\u1EAB")]  // ẫ
    [InlineData(new byte[] { 0xE4, 0xE3, 0x65 }, "\u1EC5")]  // ễ
    [InlineData(new byte[] { 0xE4, 0xE3, 0x6F }, "\u1ED7")]  // ỗ
    // Tilde + Breve
    [InlineData(new byte[] { 0xE4, 0xE6, 0x41 }, "\u1EB4")]  // Ẵ
    [InlineData(new byte[] { 0xE4, 0xE6, 0x61 }, "\u1EB5")]  // ẵ
    // Macron + Dot Above
    [InlineData(new byte[] { 0xE5, 0xE7, 0x41 }, "\u01E0")]  // Ǡ
    [InlineData(new byte[] { 0xE5, 0xE7, 0x4F }, "\u0230")]  // Ȱ
    [InlineData(new byte[] { 0xE5, 0xE7, 0x61 }, "\u01E1")]  // ǡ
    [InlineData(new byte[] { 0xE5, 0xE7, 0x6F }, "\u0231")]  // ȱ
    // Macron + Diaeresis
    [InlineData(new byte[] { 0xE5, 0xE8, 0x41 }, "\u01DE")]  // Ǟ
    [InlineData(new byte[] { 0xE5, 0xE8, 0x4F }, "\u022A")]  // Ȫ
    [InlineData(new byte[] { 0xE5, 0xE8, 0x55 }, "\u01D5")]  // Ǖ
    [InlineData(new byte[] { 0xE5, 0xE8, 0x61 }, "\u01DF")]  // ǟ
    [InlineData(new byte[] { 0xE5, 0xE8, 0x6F }, "\u022B")]  // ȫ
    [InlineData(new byte[] { 0xE5, 0xE8, 0x75 }, "\u01D6")]  // ǖ
    // Breve + Dot Below
    [InlineData(new byte[] { 0xE6, 0xF2, 0x41 }, "\u1EB6")]  // Ặ
    [InlineData(new byte[] { 0xE6, 0xF2, 0x61 }, "\u1EB7")]  // ặ
    // Dot Above + Acute
    [InlineData(new byte[] { 0xE7, 0xE2, 0x53 }, "\u1E64")]  // Ṥ
    [InlineData(new byte[] { 0xE7, 0xE2, 0x73 }, "\u1E65")]  // ṥ
    // Dot Above + Caron
    [InlineData(new byte[] { 0xE7, 0xE9, 0x53 }, "\u1E66")]  // Ṧ
    [InlineData(new byte[] { 0xE7, 0xE9, 0x73 }, "\u1E67")]  // ṧ
    // Dot Above + Dot Below
    [InlineData(new byte[] { 0xE7, 0xF2, 0x53 }, "\u1E68")]  // Ṩ
    [InlineData(new byte[] { 0xE7, 0xF2, 0x73 }, "\u1E69")]  // ṩ
    public void GetChars_WithThreeByteSequences_ReturnsExpectedUnicode(byte[] marc8, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(marc8, 0, marc8.Length, result, 0);

        Assert.Equal(expected.Length, charsWritten);
        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    [Theory]
    [InlineData("\u1EA8", new byte[] { 0xE0, 0xE3, 0x41 })]  // Ẩ
    [InlineData("\u1EC2", new byte[] { 0xE0, 0xE3, 0x45 })]  // Ể
    [InlineData("\u1EA6", new byte[] { 0xE1, 0xE3, 0x41 })]  // Ầ
    [InlineData("\u1EAF", new byte[] { 0xE2, 0xE6, 0x61 })]  // ắ
    public void GetBytes_WithThreeByteSequences_ReturnsExpectedMarc8(string unicode, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(unicode);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Mixed Content Tests

    [Theory]
    [InlineData("Café", new byte[] { 0x43, 0x61, 0x66, 0xE2, 0x65 })]
    [InlineData("Tübingen", new byte[] { 0x54, 0xE8, 0x75, 0x62, 0x69, 0x6E, 0x67, 0x65, 0x6E })]
    [InlineData("Øresund", new byte[] { 0xA2, 0x72, 0x65, 0x73, 0x75, 0x6E, 0x64 })]
    [InlineData("Québec", new byte[] { 0x51, 0x75, 0xE2, 0x65, 0x62, 0x65, 0x63 })]
    [InlineData("São Paulo", new byte[] { 0x53, 0xE4, 0x61, 0x6F, 0x20, 0x50, 0x61, 0x75, 0x6C, 0x6F })]
    public void GetBytes_WithMixedContent_ReturnsExpectedMarc8(string input, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new byte[] { 0x43, 0x61, 0x66, 0xE2, 0x65 }, "Café")]
    [InlineData(new byte[] { 0x54, 0xE8, 0x75, 0x62, 0x69, 0x6E, 0x67, 0x65, 0x6E }, "Tübingen")]
    [InlineData(new byte[] { 0xA2, 0x72, 0x65, 0x73, 0x75, 0x6E, 0x64 }, "Øresund")]
    public void GetChars_WithMixedContent_ReturnsExpectedUnicode(byte[] input, string expected)
    {
        char[] result = new char[expected.Length];
        int charsWritten = _encoding.GetChars(input, 0, input.Length, result, 0);

        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    #endregion

    #region Round-Trip Tests

    [Theory]
    [InlineData("Hello World")]
    [InlineData("Café")]
    [InlineData("Tübingen")]
    [InlineData("Øresund")]
    [InlineData("Zürich")]
    [InlineData("São Paulo")]
    [InlineData("Québec")]
    [InlineData("Łódź")]
    [InlineData("Москва")]  // Will use replacement character
    public void RoundTrip_EncodeThenDecode_ReturnsOriginalOrReplacement(string original)
    {
        byte[] encoded = _encoding.GetBytes(original);
        string decoded = _encoding.GetString(encoded);

        // For characters that can't be encoded, they become '?'
        // So we check that either:
        // 1. We get the original back (successful round-trip)
        // 2. We get a string with '?' for unsupported characters
        Assert.True(decoded == original || decoded.All(c => c == '?' || original.Contains(c)));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void GetBytes_WithNullArray_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _encoding.GetBytes((string)null!, 0, 0, new byte[10], 0));
    }

    [Fact]
    public void GetChars_WithNullArray_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _encoding.GetChars(null!, 0, 0, new char[10], 0));
    }

    [Fact]
    public void GetBytes_WithInsufficientBuffer_ThrowsArgumentException()
    {
        string input = "Hello";
        byte[] buffer = new byte[2];  // Too small

        Assert.Throws<ArgumentException>(() => _encoding.GetBytes(input.ToCharArray(), 0, input.Length, buffer, 0));
    }

    [Fact]
    public void GetChars_WithInsufficientBuffer_ThrowsArgumentException()
    {
        byte[] input = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        char[] buffer = new char[2];  // Too small

        Assert.Throws<ArgumentException>(() => _encoding.GetChars(input, 0, input.Length, buffer, 0));
    }

    #endregion

    #region Byte Count Tests

    [Theory]
    [InlineData("Hello", 5)]
    [InlineData("Café", 5)]  // C a f é(2 bytes)
    [InlineData("Tübingen", 9)]  // T ü(2) b i n g e n
    [InlineData("Ẩ", 3)]  // Three-byte sequence
    [InlineData("", 0)]
    public void GetByteCount_ReturnsExpectedCount(string input, int expectedCount)
    {
        int count = _encoding.GetByteCount(input.ToCharArray(), 0, input.Length);
        Assert.Equal(expectedCount, count);
    }

    #endregion

    #region Char Count Tests

    [Theory]
    [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, 5)]  // Hello
    [InlineData(new byte[] { 0xE2, 0x65 }, 1)]  // é (two bytes, one char)
    [InlineData(new byte[] { 0xE0, 0xE3, 0x41 }, 1)]  // Ẩ (three bytes, one char)
    [InlineData(new byte[] { }, 0)]
    public void GetCharCount_ReturnsExpectedCount(byte[] input, int expectedCount)
    {
        int count = _encoding.GetCharCount(input, 0, input.Length);
        Assert.Equal(expectedCount, count);
    }

    #endregion

    #region Max Count Tests

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 3)]
    [InlineData(10, 30)]
    [InlineData(100, 300)]
    public void GetMaxByteCount_ReturnsExpectedMaximum(int charCount, int expectedMax)
    {
        int maxBytes = _encoding.GetMaxByteCount(charCount);
        Assert.Equal(expectedMax, maxBytes);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    public void GetMaxCharCount_ReturnsExpectedMaximum(int byteCount, int expectedMax)
    {
        int maxChars = _encoding.GetMaxCharCount(byteCount);
        Assert.Equal(expectedMax, maxChars);
    }

    [Fact]
    public void GetMaxByteCount_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _encoding.GetMaxByteCount(-1));
    }

    [Fact]
    public void GetMaxCharCount_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _encoding.GetMaxCharCount(-1));
    }

    #endregion

    #region Encoding Name Tests

    [Fact]
    public void EncodingName_ReturnsMARCEight()
    {
        Assert.Equal("MARC8", _encoding.EncodingName);
    }

    #endregion

    #region Unmapped Character Tests

    [Theory]
    [InlineData("Hello 世界")]  // Chinese characters not in MARC-8
    [InlineData("Привет")]  // Cyrillic not fully in MARC-8
    [InlineData("こんにちは")]  // Japanese not in MARC-8
    [InlineData("🎉")]  // Emoji
    public void GetBytes_WithUnmappedCharacters_UsesReplacementCharacter(string input)
    {
        byte[] result = _encoding.GetBytes(input);

        // Should contain at least one '?' (0x3F) for unmapped characters
        Assert.Contains<byte>(0x3F, result);
    }

    [Theory]
    [InlineData(new byte[] { 0xE0 })]  // Incomplete combining sequence
    [InlineData(new byte[] { 0xE1 })]  // Incomplete combining sequence
    [InlineData(new byte[] { 0xE2 })]  // Incomplete combining sequence
    [InlineData(new byte[] { 0xE0, 0xE0 })]  // Invalid combination
    public void GetChars_WithInvalidSequences_HandlesGracefully(byte[] input)
    {
        // Should not throw, but may produce replacement characters or skip bytes
        char[] result = new char[10];
        int charsWritten = _encoding.GetChars(input, 0, input.Length, result, 0);

        Assert.True(charsWritten >= 0);
    }

    #endregion

    #region Vietnamese Text Tests

    [Theory]
    [InlineData("Việt Nam", new byte[] { 0x56, 0x69, 0xE3, 0xF2, 0x65, 0x74, 0x20, 0x4E, 0x61, 0x6D })]  // ệ = circumflex + dot below + e (3 bytes!)
    [InlineData("Hà Nội", new byte[] { 0x48, 0xE1, 0x61, 0x20, 0x4E, 0xE3, 0xF2, 0x6F, 0x69 })]  // à = grave + a, ộ = circumflex + dot below + o (3 bytes!)
    [InlineData("Sài Gòn", new byte[] { 0x53, 0xE1, 0x61, 0x69, 0x20, 0x47, 0xE1, 0x6F, 0x6E })]
    public void GetBytes_WithVietnameseText_ReturnsExpectedMarc8(string input, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new byte[] { 0x56, 0x69, 0xE3, 0xF2, 0x65, 0x74, 0x20, 0x4E, 0x61, 0x6D }, "Việt Nam")]  // ệ = circumflex + dot below + e
    [InlineData(new byte[] { 0x48, 0xE1, 0x61, 0x20, 0x4E, 0xE3, 0xF2, 0x6F, 0x69 }, "Hà Nội")]  // ộ needs all 3 bytes: circumflex + dot below + o
    public void GetChars_WithVietnameseBytes_ReturnsExpectedUnicode(byte[] input, string expected)
    {
        int charCount = _encoding.GetCharCount(input, 0, input.Length);
        char[] result = new char[charCount];
        int charsWritten = _encoding.GetChars(input, 0, input.Length, result, 0);

        Assert.Equal(expected, new string(result, 0, charsWritten));
    }

    #endregion

    #region Polish Text Tests

    [Theory]
    [InlineData("Kraków", new byte[] { 0x4B, 0x72, 0x61, 0x6B, 0xE2, 0x6F, 0x77 })]  // ó = acute + o
    [InlineData("Gdańsk", new byte[] { 0x47, 0x64, 0x61, 0xE2, 0x6E, 0x73, 0x6B })]  // ń = acute + n
    [InlineData("Żółw", new byte[] { 0xE7, 0x5A, 0xE2, 0x6F, 0xB1, 0x77 })]  // Ż = dot above + Z, ó = acute + o, ł = 0xB1
    [InlineData("Łódź", new byte[] { 0xA1, 0xE2, 0x6F, 0x64, 0xE2, 0x7A })]  // Ł = 0xA1, ó = acute + o, ź = acute + z
    [InlineData("Wrocław", new byte[] { 0x57, 0x72, 0x6F, 0x63, 0xB1, 0x61, 0x77 })]  // ł = 0xB1
    public void GetBytes_WithPolishText_ReturnsExpectedMarc8(string input, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(input);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Czech Text Tests

    [Theory]
    [InlineData("Praha", new byte[] { 0x50, 0x72, 0x61, 0x68, 0x61 })]
    [InlineData("Brno", new byte[] { 0x42, 0x72, 0x6E, 0x6F })]
    [InlineData("Ostrava", new byte[] { 0x4F, 0x73, 0x74, 0x72, 0x61, 0x76, 0x61 })]
    public void GetBytes_WithCzechText_ReturnsExpectedMarc8(string input, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(input);
        Assert.Equal(expected, result);
    }

    #endregion

    #region German Text Tests

    [Theory]
    [InlineData("München", new byte[] { 0x4D, 0xE8, 0x75, 0x6E, 0x63, 0x68, 0x65, 0x6E })]
    [InlineData("Köln", new byte[] { 0x4B, 0xE8, 0x6F, 0x6C, 0x6E })]
    [InlineData("Zürich", new byte[] { 0x5A, 0xE8, 0x75, 0x72, 0x69, 0x63, 0x68 })]
    [InlineData("Straße", new byte[] { 0x53, 0x74, 0x72, 0x61, 0xC7, 0x65 })]
    public void GetBytes_WithGermanText_ReturnsExpectedMarc8(string input, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(input);
        Assert.Equal(expected, result);
    }

    #endregion

    #region French Text Tests

    [Theory]
    [InlineData("École", new byte[] { 0xE2, 0x45, 0x63, 0x6F, 0x6C, 0x65 })]  // É = acute + E (combining comes FIRST!)
    [InlineData("Français", new byte[] { 0x46, 0x72, 0x61, 0x6E, 0xF0, 0x63, 0x61, 0x69, 0x73 })]
    [InlineData("Montréal", new byte[] { 0x4D, 0x6F, 0x6E, 0x74, 0x72, 0xE2, 0x65, 0x61, 0x6C })]
    [InlineData("Côte", new byte[] { 0x43, 0xE3, 0x6F, 0x74, 0x65 })]
    public void GetBytes_WithFrenchText_ReturnsExpectedMarc8(string input, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(input);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Spanish Text Tests

    [Theory]
    [InlineData("España", new byte[] { 0x45, 0x73, 0x70, 0x61, 0xE4, 0x6E, 0x61 })]
    [InlineData("México", new byte[] { 0x4D, 0xE2, 0x65, 0x78, 0x69, 0x63, 0x6F })]
    [InlineData("Bogotá", new byte[] { 0x42, 0x6F, 0x67, 0x6F, 0x74, 0xE2, 0x61 })]
    [InlineData("Málaga", new byte[] { 0x4D, 0xE2, 0x61, 0x6C, 0x61, 0x67, 0x61 })]
    public void GetBytes_WithSpanishText_ReturnsExpectedMarc8(string input, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(input);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Turkish Text Tests

    [Theory]
    [InlineData("İstanbul", new byte[] { 0xE7, 0x49, 0x73, 0x74, 0x61, 0x6E, 0x62, 0x75, 0x6C })]
    [InlineData("Ankara", new byte[] { 0x41, 0x6E, 0x6B, 0x61, 0x72, 0x61 })]
    [InlineData("Çanakkale", new byte[] { 0xF0, 0x43, 0x61, 0x6E, 0x61, 0x6B, 0x6B, 0x61, 0x6C, 0x65 })]
    public void GetBytes_WithTurkishText_ReturnsExpectedMarc8(string input, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(input);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Boundary Tests

    [Fact]
    public void GetBytes_WithEmptyString_ReturnsEmptyArray()
    {
        byte[] result = _encoding.GetBytes("");
        Assert.Empty(result);
    }

    [Fact]
    public void GetChars_WithEmptyArray_ReturnsEmptyString()
    {
        char[] result = new char[0];
        int charsWritten = _encoding.GetChars(Array.Empty<byte>(), 0, 0, result, 0);
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void GetBytes_WithVeryLongString_CompletesSuccessfully()
    {
        string longString = new string('A', 10000);
        byte[] result = _encoding.GetBytes(longString);
        Assert.Equal(10000, result.Length);
    }

    [Fact]
    public void GetChars_WithVeryLongByteArray_CompletesSuccessfully()
    {
        byte[] longArray = Enumerable.Repeat((byte)0x41, 10000).ToArray();
        char[] result = new char[10000];
        int charsWritten = _encoding.GetChars(longArray, 0, longArray.Length, result, 0);
        Assert.Equal(10000, charsWritten);
    }

    #endregion

    #region Special Character Sequences Tests

    [Theory]
    [InlineData("test\u0098test", new byte[] { 0x74, 0x65, 0x73, 0x74, 0x88, 0x74, 0x65, 0x73, 0x74 })]
    [InlineData("©2024", new byte[] { 0xC3, 0x32, 0x30, 0x32, 0x34 })]
    [InlineData("±5°", new byte[] { 0xAB, 0x35, 0xC0 })]
    public void GetBytes_WithSpecialCharacters_ReturnsExpectedMarc8(string input, byte[] expected)
    {
        byte[] result = _encoding.GetBytes(input);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Encoding API Consistency Tests

    [Fact]
    public void GetBytes_StringOverload_MatchesArrayOverload()
    {
        string test = "Café";
        byte[] result1 = _encoding.GetBytes(test);
        byte[] result2 = _encoding.GetBytes(test.ToCharArray(), 0, test.Length);

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void GetString_MatchesGetChars()
    {
        byte[] input = new byte[] { 0x43, 0x61, 0x66, 0xE2, 0x65 };
        string result1 = _encoding.GetString(input);

        char[] buffer = new char[5];
        int charsWritten = _encoding.GetChars(input, 0, input.Length, buffer, 0);
        string result2 = new string(buffer, 0, charsWritten);

        Assert.Equal(result1, result2);
    }

    #endregion

    #region Index and Count Parameter Tests

    [Fact]
    public void GetBytes_WithOffsetAndCount_EncodesCorrectSubstring()
    {
        char[] input = "Hello World".ToCharArray();
        byte[] result = _encoding.GetBytes(input, 6, 5);

        Assert.Equal(5, result.Length);
        Assert.Equal("World", _encoding.GetString(result));
    }

    [Fact]
    public void GetChars_WithOffsetAndCount_DecodesCorrectSubarray()
    {
        byte[] input = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64 };
        char[] result = new char[5];
        int charsWritten = _encoding.GetChars(input, 6, 5, result, 0);

        Assert.Equal(5, charsWritten);
        Assert.Equal("World", new string(result, 0, charsWritten));
    }

    [Fact]
    public void GetByteCount_WithOffsetAndCount_ReturnsCorrectCount()
    {
        char[] input = "Café Zürich".ToCharArray();
        int count = _encoding.GetByteCount(input, 0, 4);  // "Café"

        Assert.Equal(5, count);  // C a f é(2 bytes)
    }

    [Fact]
    public void GetCharCount_WithOffsetAndCount_ReturnsCorrectCount()
    {
        byte[] input = new byte[] { 0x43, 0x61, 0x66, 0xE2, 0x65, 0x20, 0x41 };
        int count = _encoding.GetCharCount(input, 0, 5);  // "Café"

        Assert.Equal(4, count);  // 4 characters
    }

    #endregion
}
