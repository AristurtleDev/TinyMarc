
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace TinyMarc;

// Reference: https://www.loc.gov/marc/specifications/speccharintro.html
public sealed class Marc8Encoding : Encoding
{
    #region Constants

    // Control bytes that indicate combining diacritics or special character sets
    private const byte CombiningHookAbove = 0xE0;
    private const byte CombiningGraveAccent = 0xE1;
    private const byte CombiningAcuteAccent = 0xE2;
    private const byte CombiningCircumflexAccent = 0xE3;
    private const byte CombiningTilde = 0xE4;
    private const byte CombiningMacron = 0xE5;
    private const byte CombiningBreve = 0xE6;
    private const byte CombiningDotAbove = 0xE7;
    private const byte CombiningDiaeresis = 0xE8;
    private const byte CombiningCaron = 0xE9;
    private const byte CombiningRingAbove = 0xEA;
    private const byte CombiningDoubleAcuteAccent = 0xEE;
    private const byte CombiningCedilla = 0xF0;
    private const byte CombiningOgonek = 0xF1;
    private const byte CombiningDotBelow = 0xF2;
    private const byte CombiningDoubleUnderscore = 0xF3;
    private const byte CombiningUnderscore = 0xF4;
    private const byte CombiningCommaBelow = 0xF7;
    private const byte CombiningBreveBelow = 0xF9;

    // Threshold for special multi-byte sequences
    private const byte SpecialSequenceThreshold = 0xE0;

    #endregion Constants

    #region Lookup Tables

    private static readonly FrozenDictionary<byte, char> SingleByteToUnicode;
    private static readonly FrozenDictionary<(byte, byte), char> TwoByteToUnicode;
    private static readonly FrozenDictionary<(byte, byte, byte), char> ThreeByteToUnicode;
    private static readonly FrozenDictionary<char, Marc8Bytes> UnicodeToMarc8;

    #endregion Lookup Tables

    public override string EncodingName => "MARC8";

    static Marc8Encoding()
    {
        // Single-byte MARC-8 to Unicode mappings (for bytes < 0xE0)
        SingleByteToUnicode = new Dictionary<byte, char>()
        {
            [0x88] = '\u0098',
            [0x89] = '\u009C',
            [0xA1] = '\u0141',  // Ł
            [0xA2] = '\u00D8',
            [0xA3] = '\u0110',  // Đ
            [0xA4] = '\u00DE',
            [0xA5] = '\u00C6',
            [0xA6] = '\u0152',  // Œ
            [0xA7] = '\u02B9',  // ʹ soft sign
            [0xA9] = '\u266D',  // ♭ flat
            [0xAE] = '\u02BC',  // ʼ alif
            [0xA8] = '\u00B7',
            [0xAA] = '\u00AE',
            [0xAB] = '\u00B1',
            [0xB0] = '\u02BB',  // ʻ ayn
            [0xB1] = '\u0142',  // ł
            [0xB3] = '\u0111',  // đ
            [0xB6] = '\u0153',  // œ
            [0xB7] = '\u02BA',  // ʺ hard sign
            [0xB8] = '\u0131',  // ı
            [0xB9] = '\u00A3',
            [0xBA] = '\u00F0',  // ð
            [0xC0] = '\u00B0',
            [0xC1] = '\u2113',  // ℓ script l
            [0xC2] = '\u2117',  // ℗ sound recording copyright
            [0xC4] = '\u266F',  // ♯ sharp
            [0xC3] = '\u00A9',
            [0xC5] = '\u00BF',
            [0xC6] = '\u00A1',
            [0xC7] = '\u00DF',
            [0xC8] = '\u20AC',  // € euro
        }.ToFrozenDictionary();

        // Type-byte sequences: combining diacritic + base character
        TwoByteToUnicode = BuildTwoByteTable();

        // Three-byte sequences: two combining diacritics + base character
        ThreeByteToUnicode = BuildThreeByteTable();

        // Unicode to MARC-8 mappings
        UnicodeToMarc8 = BuildUnicodeToMarc8Table();
    }

    #region Encoding Methods

    public override int GetByteCount(char[] chars, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(chars);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index + count, chars.Length);

        return GetByteCountCore(chars.AsSpan(index, count));
    }

    private int GetByteCountCore(ReadOnlySpan<char> chars)
    {
        int byteCount = 0;

        foreach (char c in chars)
        {
            if (UnicodeToMarc8.TryGetValue(c, out Marc8Bytes marc8Bytes))
            {
                byteCount += marc8Bytes.Length;
            }
            else if (c < 127)
            {
                // Direct ASCII mapping
                byteCount++;
            }
            else
            {
                // Fallback: will use '?' replacement character
                byteCount++;
            }
        }

        return byteCount;
    }

    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        ArgumentNullException.ThrowIfNull(chars);
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentOutOfRangeException.ThrowIfNegative(charIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(charCount);
        ArgumentOutOfRangeException.ThrowIfNegative(byteIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(charIndex + charCount, chars.Length);

        // Special case for counting only (byteIndex = -1)
        if (byteIndex < 0)
        {
            return GetByteCountCore(chars.AsSpan(charIndex, charCount));
        }

        return GetBytesCore(chars.AsSpan(charIndex, charCount), bytes.AsSpan(byteIndex));
    }

    private int GetBytesCore(ReadOnlySpan<char> chars, Span<byte> bytes)
    {
        int bytesWritten = 0;

        foreach (char c in chars)
        {
            if (UnicodeToMarc8.TryGetValue(c, out Marc8Bytes marc8Bytes))
            {
                int length = marc8Bytes.Length;
                if (bytesWritten + length > bytes.Length)
                {
                    throw new ArgumentException("Insufficient buffer space for encoding.", nameof(bytes));
                }

                marc8Bytes.CopyTo(bytes.Slice(bytesWritten));
                bytesWritten += length;
            }
            else if (c < 127)
            {
                if (bytesWritten >= bytes.Length)
                {
                    throw new ArgumentException("Insufficient buffer space for encoding.", nameof(bytes));
                }
                bytes[bytesWritten++] = (byte)c;
            }
            else
            {
                // Replacement character for unmapped characters
                if (bytesWritten >= bytes.Length)
                {
                    throw new ArgumentException("Insufficient buffer space for encoding.", nameof(bytes));
                }
                bytes[bytesWritten++] = (byte)'?';
            }
        }

        return bytesWritten;
    }

    #endregion Encoding Methods

    #region Decoding Methods

    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index + count, bytes.Length);

        return GetCharCountCore(bytes.AsSpan(index, count));
    }

    private int GetCharCountCore(ReadOnlySpan<byte> bytes)
    {
        int charCount = 0;
        int i = 0;

        while (i < bytes.Length)
        {
            byte currentByte = bytes[i];

            if (currentByte < SpecialSequenceThreshold)
            {
                // Single-byte character
                charCount++;
                i++;
            }
            else
            {
                // Multi-byte sequence - need to look ahead
                int consumed = TryDecodeMultiByte(bytes.Slice(i), out _);
                if (consumed > 0)
                {
                    charCount++;
                    i += consumed;
                }
                else
                {
                    // Couldn't decode - skip this byte
                    i++;
                }
            }
        }

        return charCount;
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(chars);
        ArgumentOutOfRangeException.ThrowIfNegative(byteIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        ArgumentOutOfRangeException.ThrowIfNegative(charIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(byteIndex + byteCount, bytes.Length);

        // Special case for counting only (charIndex = -1)
        if (charIndex < 0)
        {
            return GetCharCountCore(bytes.AsSpan(byteIndex, byteCount));
        }

        return GetCharsCore(bytes.AsSpan(byteIndex, byteCount), chars.AsSpan(charIndex));
    }

    private int GetCharsCore(ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        int charsWritten = 0;
        int i = 0;

        while (i < bytes.Length)
        {
            byte currentByte = bytes[i];

            if (currentByte < SpecialSequenceThreshold)
            {
                // Single-byte character
                char decoded = DecodeSingleByte(currentByte);
                if (charsWritten >= chars.Length)
                {
                    throw new ArgumentException("Insufficient buffer space for decoding.", nameof(chars));
                }
                chars[charsWritten++] = decoded;
                i++;
            }
            else
            {
                // Multi-byte sequence
                int consumed = TryDecodeMultiByte(bytes.Slice(i), out char decodedChar);
                if (consumed > 0)
                {
                    if (charsWritten >= chars.Length)
                    {
                        throw new ArgumentException("Insufficient buffer space for decoding.", nameof(chars));
                    }
                    chars[charsWritten++] = decodedChar;
                    i += consumed;
                }
                else
                {
                    // Couldn't decode - skip this byte and use replacement
                    if (charsWritten >= chars.Length)
                    {
                        throw new ArgumentException("Insufficient buffer space for decoding.", nameof(chars));
                    }
                    chars[charsWritten++] = '?';
                    i++;
                }
            }
        }

        return charsWritten;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char DecodeSingleByte(byte b)
    {
        return SingleByteToUnicode.TryGetValue(b, out char c) ? c : (char)b;
    }

    private static int TryDecodeMultiByte(ReadOnlySpan<byte> bytes, out char result)
    {
        if (bytes.Length == 0)
        {
            result = default;
            return 0;
        }

        byte first = bytes[0];

        // Not a special sequence
        if (first < SpecialSequenceThreshold)
        {
            result = default;
            return 0;
        }

        // Try three-byte sequence first
        if (bytes.Length >= 3)
        {
            var key = (bytes[0], bytes[1], bytes[2]);
            if (ThreeByteToUnicode.TryGetValue(key, out result))
            {
                return 3;
            }
        }

        // Try two-byte sequence
        if (bytes.Length >= 2)
        {
            var key = (bytes[0], bytes[1]);
            if (TwoByteToUnicode.TryGetValue(key, out result))
            {
                return 2;
            }
        }

        // No valid multi-byte sequence found
        result = default;
        return 0;
    }

    #endregion Decoding Methods

    #region Max Count Methods

    public override int GetMaxByteCount(int charCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(charCount);

        // Each char can require up to 3 bytes in MARC-8
        long maxBytes = (long)charCount * 3;
        if (maxBytes > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(charCount), "Character count too large.");
        }

        return (int)maxBytes;
    }

    public override int GetMaxCharCount(int byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);

        // Each byte can produce at most 1 character
        return byteCount;
    }

    #endregion Max Count Methods

    #region Table Building

    private static FrozenDictionary<(byte, byte), char> BuildTwoByteTable()
    {
        var table = new Dictionary<(byte, byte), char>(400);

        // Hook Above (0xE0)
        AddTwoByteMapping(table, CombiningHookAbove, 0x41, '\u1EA2'); // A
        AddTwoByteMapping(table, CombiningHookAbove, 0x45, '\u1EBA'); // E
        AddTwoByteMapping(table, CombiningHookAbove, 0x49, '\u1EC8'); // I
        AddTwoByteMapping(table, CombiningHookAbove, 0x4F, '\u1ECE'); // O
        AddTwoByteMapping(table, CombiningHookAbove, 0x55, '\u1EE6'); // U
        AddTwoByteMapping(table, CombiningHookAbove, 0x59, '\u1EF6'); // Y
        AddTwoByteMapping(table, CombiningHookAbove, 0x61, '\u1EA3'); // a
        AddTwoByteMapping(table, CombiningHookAbove, 0x65, '\u1EBB'); // e
        AddTwoByteMapping(table, CombiningHookAbove, 0x69, '\u1EC9'); // i
        AddTwoByteMapping(table, CombiningHookAbove, 0x6F, '\u1ECF'); // o
        AddTwoByteMapping(table, CombiningHookAbove, 0x75, '\u1EE7'); // u
        AddTwoByteMapping(table, CombiningHookAbove, 0x79, '\u1EF7'); // y
        AddTwoByteMapping(table, CombiningHookAbove, 0xAC, '\u1EDE'); // Ơ
        AddTwoByteMapping(table, CombiningHookAbove, 0xAD, '\u1EEC'); // Ư
        AddTwoByteMapping(table, CombiningHookAbove, 0xBC, '\u1EDF'); // ơ
        AddTwoByteMapping(table, CombiningHookAbove, 0xBD, '\u1EED'); // ư

        // Grave Accent (0xE1)
        AddTwoByteMapping(table, CombiningGraveAccent, 0x41, '\u00C0'); // À
        AddTwoByteMapping(table, CombiningGraveAccent, 0x45, '\u00C8'); // È
        AddTwoByteMapping(table, CombiningGraveAccent, 0x49, '\u00CC'); // Ì
        AddTwoByteMapping(table, CombiningGraveAccent, 0x4E, '\u01F8'); // Ǹ
        AddTwoByteMapping(table, CombiningGraveAccent, 0x4F, '\u00D2'); // Ò
        AddTwoByteMapping(table, CombiningGraveAccent, 0x55, '\u00D9'); // Ù
        AddTwoByteMapping(table, CombiningGraveAccent, 0x57, '\u1E80'); // Ẁ
        AddTwoByteMapping(table, CombiningGraveAccent, 0x59, '\u1EF2'); // Ỳ
        AddTwoByteMapping(table, CombiningGraveAccent, 0x61, '\u00E0'); // à
        AddTwoByteMapping(table, CombiningGraveAccent, 0x65, '\u00E8'); // è
        AddTwoByteMapping(table, CombiningGraveAccent, 0x69, '\u00EC'); // ì
        AddTwoByteMapping(table, CombiningGraveAccent, 0x6E, '\u01F9'); // ǹ
        AddTwoByteMapping(table, CombiningGraveAccent, 0x6F, '\u00F2'); // ò
        AddTwoByteMapping(table, CombiningGraveAccent, 0x75, '\u00F9'); // ù
        AddTwoByteMapping(table, CombiningGraveAccent, 0x77, '\u1E81'); // ẁ
        AddTwoByteMapping(table, CombiningGraveAccent, 0x79, '\u1EF3'); // ỳ
        AddTwoByteMapping(table, CombiningGraveAccent, 0xAC, '\u1EDC'); // Ờ
        AddTwoByteMapping(table, CombiningGraveAccent, 0xAD, '\u1EEA'); // Ừ
        AddTwoByteMapping(table, CombiningGraveAccent, 0xBC, '\u1EDD'); // ờ
        AddTwoByteMapping(table, CombiningGraveAccent, 0xBD, '\u1EEB'); // ừ

        // Acute Accent (0xE2) - extensive mappings
        AddAcuteAccentMappings(table);

        // Circumflex Accent (0xE3)
        AddCircumflexAccentMappings(table);

        // Tilde (0xE4)
        AddTildeMappings(table);

        // Macron (0xE5)
        AddMacronMappings(table);

        // Breve (0xE6)
        AddBreveMappings(table);

        // Dot Above (0xE7)
        AddDotAboveMappings(table);

        // Diaeresis (0xE8)
        AddDiaeresisMappings(table);

        // Caron (0xE9)
        AddCaronMappings(table);

        // Ring Above (0xEA)
        AddRingAboveMappings(table);

        // Double Acute (0xEE)
        AddDoubleAcuteMappings(table);

        // Cedilla (0xF0)
        AddCedillaMappings(table);

        // Ogonek (0xF1)
        AddOgonekMappings(table);

        // Dot Below (0xF2)
        AddDotBelowMappings(table);

        // Double Underscore (0xF3)
        AddTwoByteMapping(table, CombiningDoubleUnderscore, 0x55, '\u1E72'); // Ṳ
        AddTwoByteMapping(table, CombiningDoubleUnderscore, 0x75, '\u1E73'); // ṳ

        // Underscore (0xF4)
        AddTwoByteMapping(table, CombiningUnderscore, 0x41, '\u1E00'); // Ḁ
        AddTwoByteMapping(table, CombiningUnderscore, 0x61, '\u1E01'); // ḁ

        // Comma Below (0xF7)
        AddTwoByteMapping(table, CombiningCommaBelow, 0x53, '\u0218'); // Ș
        AddTwoByteMapping(table, CombiningCommaBelow, 0x54, '\u021A'); // Ț
        AddTwoByteMapping(table, CombiningCommaBelow, 0x73, '\u0219'); // ș
        AddTwoByteMapping(table, CombiningCommaBelow, 0x74, '\u021B'); // ț

        // Breve Below (0xF9)
        AddTwoByteMapping(table, CombiningBreveBelow, 0x48, '\u1E2A'); // Ḫ
        AddTwoByteMapping(table, CombiningBreveBelow, 0x68, '\u1E2B'); // ḫ

        return table.ToFrozenDictionary();
    }

    private static void AddTwoByteMapping(Dictionary<(byte, byte), char> table, byte combining, byte baseChar, char unicode)
    {
        table[(combining, baseChar)] = unicode;
    }

    private static void AddAcuteAccentMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x41, '\u00C1'); // Á
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x43, '\u0106'); // Ć
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x45, '\u00C9'); // É
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x47, '\u01F4'); // Ǵ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x49, '\u00CD'); // Í
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x4B, '\u1E30'); // Ḱ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x4C, '\u0139'); // Ĺ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x4D, '\u1E3E'); // Ḿ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x4E, '\u0143'); // Ń
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x4F, '\u00D3'); // Ó
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x50, '\u1E54'); // Ṕ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x52, '\u0154'); // Ŕ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x53, '\u015A'); // Ś
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x55, '\u00DA'); // Ú
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x57, '\u1E82'); // Ẃ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x59, '\u00DD'); // Ý
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x5A, '\u0179'); // Ź
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x61, '\u00E1'); // á
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x63, '\u0107'); // ć
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x65, '\u00E9'); // é
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x67, '\u01F5'); // ǵ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x69, '\u00ED'); // í
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x6B, '\u1E31'); // ḱ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x6C, '\u013A'); // ĺ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x6D, '\u1E3F'); // ḿ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x6E, '\u0144'); // ń
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x6F, '\u00F3'); // ó
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x70, '\u1E55'); // ṕ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x72, '\u0155'); // ŕ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x73, '\u015B'); // ś
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x75, '\u00FA'); // ú
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x77, '\u1E83'); // ẃ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x79, '\u00FD'); // ý
        AddTwoByteMapping(table, CombiningAcuteAccent, 0x7A, '\u017A'); // ź
        AddTwoByteMapping(table, CombiningAcuteAccent, 0xA2, '\u01FE'); // Ǿ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0xA5, '\u01FC'); // Ǽ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0xAC, '\u1EDA'); // Ớ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0xAD, '\u1EE8'); // Ứ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0xB2, '\u01FF'); // ǿ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0xB5, '\u01FD'); // ǽ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0xBC, '\u1EDB'); // ớ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0xBD, '\u1EE9'); // ứ
        AddTwoByteMapping(table, CombiningAcuteAccent, 0xE8, '\u0344'); // Combining Greek dialytika tonos
    }

    private static void AddCircumflexAccentMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x41, '\u00C2'); // Â
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x43, '\u0108'); // Ĉ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x45, '\u00CA'); // Ê
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x47, '\u011C'); // Ĝ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x48, '\u0124'); // Ĥ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x49, '\u00CE'); // Î
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x4A, '\u0134'); // Ĵ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x4F, '\u00D4'); // Ô
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x53, '\u015C'); // Ŝ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x55, '\u00DB'); // Û
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x57, '\u0174'); // Ŵ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x59, '\u0176'); // Ŷ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x5A, '\u1E90'); // Ẑ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x61, '\u00E2'); // â
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x63, '\u0109'); // ĉ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x65, '\u00EA'); // ê
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x67, '\u011D'); // ĝ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x68, '\u0125'); // ĥ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x69, '\u00EE'); // î
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x6A, '\u0135'); // ĵ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x6F, '\u00F4'); // ô
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x73, '\u015D'); // ŝ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x75, '\u00FB'); // û
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x77, '\u0175'); // ŵ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x79, '\u0177'); // ŷ
        AddTwoByteMapping(table, CombiningCircumflexAccent, 0x7A, '\u1E91'); // ẑ
    }

    private static void AddTildeMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningTilde, 0x41, '\u00C3'); // Ã
        AddTwoByteMapping(table, CombiningTilde, 0x45, '\u1EBC'); // Ẽ
        AddTwoByteMapping(table, CombiningTilde, 0x49, '\u0128'); // Ĩ
        AddTwoByteMapping(table, CombiningTilde, 0x4E, '\u00D1'); // Ñ
        AddTwoByteMapping(table, CombiningTilde, 0x4F, '\u00D5'); // Õ
        AddTwoByteMapping(table, CombiningTilde, 0x55, '\u0168'); // Ũ
        AddTwoByteMapping(table, CombiningTilde, 0x56, '\u1E7C'); // Ṽ
        AddTwoByteMapping(table, CombiningTilde, 0x59, '\u1EF8'); // Ỹ
        AddTwoByteMapping(table, CombiningTilde, 0x61, '\u00E3'); // ã
        AddTwoByteMapping(table, CombiningTilde, 0x65, '\u1EBD'); // ẽ
        AddTwoByteMapping(table, CombiningTilde, 0x69, '\u0129'); // ĩ
        AddTwoByteMapping(table, CombiningTilde, 0x6E, '\u00F1'); // ñ
        AddTwoByteMapping(table, CombiningTilde, 0x6F, '\u00F5'); // õ
        AddTwoByteMapping(table, CombiningTilde, 0x75, '\u0169'); // ũ
        AddTwoByteMapping(table, CombiningTilde, 0x76, '\u1E7D'); // ṽ
        AddTwoByteMapping(table, CombiningTilde, 0x79, '\u1EF9'); // ỹ
        AddTwoByteMapping(table, CombiningTilde, 0xAC, '\u1EE0'); // Ỡ
        AddTwoByteMapping(table, CombiningTilde, 0xAD, '\u1EEE'); // Ữ
        AddTwoByteMapping(table, CombiningTilde, 0xBC, '\u1EE1'); // ỡ
        AddTwoByteMapping(table, CombiningTilde, 0xBD, '\u1EEF'); // ữ
    }

    private static void AddMacronMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningMacron, 0x41, '\u0100'); // Ā
        AddTwoByteMapping(table, CombiningMacron, 0x45, '\u0112'); // Ē
        AddTwoByteMapping(table, CombiningMacron, 0x47, '\u1E20'); // Ḡ
        AddTwoByteMapping(table, CombiningMacron, 0x49, '\u012A'); // Ī
        AddTwoByteMapping(table, CombiningMacron, 0x4F, '\u014C'); // Ō
        AddTwoByteMapping(table, CombiningMacron, 0x55, '\u016A'); // Ū
        AddTwoByteMapping(table, CombiningMacron, 0x59, '\u0232'); // Ȳ
        AddTwoByteMapping(table, CombiningMacron, 0x61, '\u0101'); // ā
        AddTwoByteMapping(table, CombiningMacron, 0x65, '\u0113'); // ē
        AddTwoByteMapping(table, CombiningMacron, 0x67, '\u1E21'); // ḡ
        AddTwoByteMapping(table, CombiningMacron, 0x69, '\u012B'); // ī
        AddTwoByteMapping(table, CombiningMacron, 0x6F, '\u014D'); // ō
        AddTwoByteMapping(table, CombiningMacron, 0x75, '\u016B'); // ū
        AddTwoByteMapping(table, CombiningMacron, 0x79, '\u0233'); // ȳ
        AddTwoByteMapping(table, CombiningMacron, 0xA5, '\u01E2'); // Ǣ
        AddTwoByteMapping(table, CombiningMacron, 0xB5, '\u01E3'); // ǣ
    }

    private static void AddBreveMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningBreve, 0x41, '\u0102'); // Ă
        AddTwoByteMapping(table, CombiningBreve, 0x45, '\u0114'); // Ĕ
        AddTwoByteMapping(table, CombiningBreve, 0x47, '\u011E'); // Ğ
        AddTwoByteMapping(table, CombiningBreve, 0x49, '\u012C'); // Ĭ
        AddTwoByteMapping(table, CombiningBreve, 0x4F, '\u014E'); // Ŏ
        AddTwoByteMapping(table, CombiningBreve, 0x55, '\u016C'); // Ŭ
        AddTwoByteMapping(table, CombiningBreve, 0x61, '\u0103'); // ă
        AddTwoByteMapping(table, CombiningBreve, 0x65, '\u0115'); // ĕ
        AddTwoByteMapping(table, CombiningBreve, 0x67, '\u011F'); // ğ
        AddTwoByteMapping(table, CombiningBreve, 0x69, '\u012D'); // ĭ
        AddTwoByteMapping(table, CombiningBreve, 0x6F, '\u014F'); // ŏ
        AddTwoByteMapping(table, CombiningBreve, 0x75, '\u016D'); // ŭ
    }

    private static void AddDotAboveMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningDotAbove, 0x41, '\u0226'); // Ȧ
        AddTwoByteMapping(table, CombiningDotAbove, 0x42, '\u1E02'); // Ḃ
        AddTwoByteMapping(table, CombiningDotAbove, 0x43, '\u010A'); // Ċ
        AddTwoByteMapping(table, CombiningDotAbove, 0x44, '\u1E0A'); // Ḋ
        AddTwoByteMapping(table, CombiningDotAbove, 0x45, '\u0116'); // Ė
        AddTwoByteMapping(table, CombiningDotAbove, 0x46, '\u1E1E'); // Ḟ
        AddTwoByteMapping(table, CombiningDotAbove, 0x47, '\u0120'); // Ġ
        AddTwoByteMapping(table, CombiningDotAbove, 0x48, '\u1E22'); // Ḣ
        AddTwoByteMapping(table, CombiningDotAbove, 0x49, '\u0130'); // İ
        AddTwoByteMapping(table, CombiningDotAbove, 0x4D, '\u1E40'); // Ṁ
        AddTwoByteMapping(table, CombiningDotAbove, 0x4E, '\u1E44'); // Ṅ
        AddTwoByteMapping(table, CombiningDotAbove, 0x4F, '\u022E'); // Ȯ
        AddTwoByteMapping(table, CombiningDotAbove, 0x50, '\u1E56'); // Ṗ
        AddTwoByteMapping(table, CombiningDotAbove, 0x52, '\u1E58'); // Ṙ
        AddTwoByteMapping(table, CombiningDotAbove, 0x53, '\u1E60'); // Ṡ
        AddTwoByteMapping(table, CombiningDotAbove, 0x54, '\u1E6A'); // Ṫ
        AddTwoByteMapping(table, CombiningDotAbove, 0x57, '\u1E86'); // Ẇ
        AddTwoByteMapping(table, CombiningDotAbove, 0x58, '\u1E8A'); // Ẋ
        AddTwoByteMapping(table, CombiningDotAbove, 0x59, '\u1E8E'); // Ẏ
        AddTwoByteMapping(table, CombiningDotAbove, 0x5A, '\u017B'); // Ż
        AddTwoByteMapping(table, CombiningDotAbove, 0x61, '\u0227'); // ȧ
        AddTwoByteMapping(table, CombiningDotAbove, 0x62, '\u1E03'); // ḃ
        AddTwoByteMapping(table, CombiningDotAbove, 0x63, '\u010B'); // ċ
        AddTwoByteMapping(table, CombiningDotAbove, 0x64, '\u1E0B'); // ḋ
        AddTwoByteMapping(table, CombiningDotAbove, 0x65, '\u0117'); // ė
        AddTwoByteMapping(table, CombiningDotAbove, 0x66, '\u1E1F'); // ḟ
        AddTwoByteMapping(table, CombiningDotAbove, 0x67, '\u0121'); // ġ
        AddTwoByteMapping(table, CombiningDotAbove, 0x68, '\u1E23'); // ḣ
        AddTwoByteMapping(table, CombiningDotAbove, 0x6D, '\u1E41'); // ṁ
        AddTwoByteMapping(table, CombiningDotAbove, 0x6E, '\u1E45'); // ṅ
        AddTwoByteMapping(table, CombiningDotAbove, 0x6F, '\u022F'); // ȯ
        AddTwoByteMapping(table, CombiningDotAbove, 0x70, '\u1E57'); // ṗ
        AddTwoByteMapping(table, CombiningDotAbove, 0x72, '\u1E59'); // ṙ
        AddTwoByteMapping(table, CombiningDotAbove, 0x73, '\u1E61'); // ṡ
        AddTwoByteMapping(table, CombiningDotAbove, 0x74, '\u1E6B'); // ṫ
        AddTwoByteMapping(table, CombiningDotAbove, 0x77, '\u1E87'); // ẇ
        AddTwoByteMapping(table, CombiningDotAbove, 0x78, '\u1E8B'); // ẋ
        AddTwoByteMapping(table, CombiningDotAbove, 0x79, '\u1E8F'); // ẏ
        AddTwoByteMapping(table, CombiningDotAbove, 0x7A, '\u017C'); // ż
    }

    private static void AddDiaeresisMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningDiaeresis, 0x41, '\u00C4'); // Ä
        AddTwoByteMapping(table, CombiningDiaeresis, 0x45, '\u00CB'); // Ë
        AddTwoByteMapping(table, CombiningDiaeresis, 0x48, '\u1E26'); // Ḧ
        AddTwoByteMapping(table, CombiningDiaeresis, 0x49, '\u00CF'); // Ï
        AddTwoByteMapping(table, CombiningDiaeresis, 0x4F, '\u00D6'); // Ö
        AddTwoByteMapping(table, CombiningDiaeresis, 0x55, '\u00DC'); // Ü
        AddTwoByteMapping(table, CombiningDiaeresis, 0x57, '\u1E84'); // Ẅ
        AddTwoByteMapping(table, CombiningDiaeresis, 0x58, '\u1E8C'); // Ẍ
        AddTwoByteMapping(table, CombiningDiaeresis, 0x59, '\u0178'); // Ÿ
        AddTwoByteMapping(table, CombiningDiaeresis, 0x61, '\u00E4'); // ä
        AddTwoByteMapping(table, CombiningDiaeresis, 0x65, '\u00EB'); // ë
        AddTwoByteMapping(table, CombiningDiaeresis, 0x68, '\u1E27'); // ḧ
        AddTwoByteMapping(table, CombiningDiaeresis, 0x69, '\u00EF'); // ï
        AddTwoByteMapping(table, CombiningDiaeresis, 0x6F, '\u00F6'); // ö
        AddTwoByteMapping(table, CombiningDiaeresis, 0x74, '\u1E97'); // ẗ
        AddTwoByteMapping(table, CombiningDiaeresis, 0x75, '\u00FC'); // ü
        AddTwoByteMapping(table, CombiningDiaeresis, 0x77, '\u1E85'); // ẅ
        AddTwoByteMapping(table, CombiningDiaeresis, 0x78, '\u1E8D'); // ẍ
        AddTwoByteMapping(table, CombiningDiaeresis, 0x79, '\u00FF'); // ÿ
    }

    private static void AddCaronMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningCaron, 0x41, '\u01CD'); // Ǎ
        AddTwoByteMapping(table, CombiningCaron, 0x43, '\u010C'); // Č
        AddTwoByteMapping(table, CombiningCaron, 0x44, '\u010E'); // Ď
        AddTwoByteMapping(table, CombiningCaron, 0x45, '\u011A'); // Ě
        AddTwoByteMapping(table, CombiningCaron, 0x47, '\u01E6'); // Ǧ
        AddTwoByteMapping(table, CombiningCaron, 0x48, '\u021E'); // Ȟ
        AddTwoByteMapping(table, CombiningCaron, 0x49, '\u01CF'); // Ǐ
        AddTwoByteMapping(table, CombiningCaron, 0x4B, '\u01E8'); // Ǩ
        AddTwoByteMapping(table, CombiningCaron, 0x4C, '\u013D'); // Ľ
        AddTwoByteMapping(table, CombiningCaron, 0x4E, '\u0147'); // Ň
        AddTwoByteMapping(table, CombiningCaron, 0x4F, '\u01D1'); // Ǒ
        AddTwoByteMapping(table, CombiningCaron, 0x52, '\u0158'); // Ř
        AddTwoByteMapping(table, CombiningCaron, 0x53, '\u0160'); // Š
        AddTwoByteMapping(table, CombiningCaron, 0x54, '\u0164'); // Ť
        AddTwoByteMapping(table, CombiningCaron, 0x55, '\u01D3'); // Ǔ
        AddTwoByteMapping(table, CombiningCaron, 0x5A, '\u017D'); // Ž
        AddTwoByteMapping(table, CombiningCaron, 0x61, '\u01CE'); // ǎ
        AddTwoByteMapping(table, CombiningCaron, 0x63, '\u010D'); // č
        AddTwoByteMapping(table, CombiningCaron, 0x64, '\u010F'); // ď
        AddTwoByteMapping(table, CombiningCaron, 0x65, '\u011B'); // ě
        AddTwoByteMapping(table, CombiningCaron, 0x67, '\u01E7'); // ǧ
        AddTwoByteMapping(table, CombiningCaron, 0x68, '\u021F'); // ȟ
        AddTwoByteMapping(table, CombiningCaron, 0x69, '\u01D0'); // ǐ
        AddTwoByteMapping(table, CombiningCaron, 0x6A, '\u01F0'); // ǰ
        AddTwoByteMapping(table, CombiningCaron, 0x6B, '\u01E9'); // ǩ
        AddTwoByteMapping(table, CombiningCaron, 0x6C, '\u013E'); // ľ
        AddTwoByteMapping(table, CombiningCaron, 0x6E, '\u0148'); // ň
        AddTwoByteMapping(table, CombiningCaron, 0x6F, '\u01D2'); // ǒ
        AddTwoByteMapping(table, CombiningCaron, 0x72, '\u0159'); // ř
        AddTwoByteMapping(table, CombiningCaron, 0x73, '\u0161'); // š
        AddTwoByteMapping(table, CombiningCaron, 0x74, '\u0165'); // ť
        AddTwoByteMapping(table, CombiningCaron, 0x75, '\u01D4'); // ǔ
        AddTwoByteMapping(table, CombiningCaron, 0x7A, '\u017E'); // ž
    }

    private static void AddRingAboveMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningRingAbove, 0x41, '\u00C5'); // Å
        AddTwoByteMapping(table, CombiningRingAbove, 0x55, '\u016E'); // Ů
        AddTwoByteMapping(table, CombiningRingAbove, 0x61, '\u00E5'); // å
        AddTwoByteMapping(table, CombiningRingAbove, 0x75, '\u016F'); // ů
        AddTwoByteMapping(table, CombiningRingAbove, 0x77, '\u1E98'); // ẘ
        AddTwoByteMapping(table, CombiningRingAbove, 0x79, '\u1E99'); // ẙ
    }

    private static void AddDoubleAcuteMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningDoubleAcuteAccent, 0x4F, '\u0150'); // Ő
        AddTwoByteMapping(table, CombiningDoubleAcuteAccent, 0x55, '\u0170'); // Ű
        AddTwoByteMapping(table, CombiningDoubleAcuteAccent, 0x6F, '\u0151'); // ő
        AddTwoByteMapping(table, CombiningDoubleAcuteAccent, 0x75, '\u0171'); // ű
    }

    private static void AddCedillaMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningCedilla, 0x43, '\u00C7'); // Ç
        AddTwoByteMapping(table, CombiningCedilla, 0x44, '\u1E10'); // Ḑ
        AddTwoByteMapping(table, CombiningCedilla, 0x45, '\u0228'); // Ȩ
        AddTwoByteMapping(table, CombiningCedilla, 0x47, '\u0122'); // Ģ
        AddTwoByteMapping(table, CombiningCedilla, 0x48, '\u1E28'); // Ḩ
        AddTwoByteMapping(table, CombiningCedilla, 0x4B, '\u0136'); // Ķ
        AddTwoByteMapping(table, CombiningCedilla, 0x4C, '\u013B'); // Ļ
        AddTwoByteMapping(table, CombiningCedilla, 0x4E, '\u0145'); // Ņ
        AddTwoByteMapping(table, CombiningCedilla, 0x52, '\u0156'); // Ŗ
        AddTwoByteMapping(table, CombiningCedilla, 0x53, '\u015E'); // Ş
        AddTwoByteMapping(table, CombiningCedilla, 0x54, '\u0162'); // Ţ
        AddTwoByteMapping(table, CombiningCedilla, 0x63, '\u00E7'); // ç
        AddTwoByteMapping(table, CombiningCedilla, 0x64, '\u1E11'); // ḑ
        AddTwoByteMapping(table, CombiningCedilla, 0x65, '\u0229'); // ȩ
        AddTwoByteMapping(table, CombiningCedilla, 0x67, '\u0123'); // ģ
        AddTwoByteMapping(table, CombiningCedilla, 0x68, '\u1E29'); // ḩ
        AddTwoByteMapping(table, CombiningCedilla, 0x6B, '\u0137'); // ķ
        AddTwoByteMapping(table, CombiningCedilla, 0x6C, '\u013C'); // ļ
        AddTwoByteMapping(table, CombiningCedilla, 0x6E, '\u0146'); // ņ
        AddTwoByteMapping(table, CombiningCedilla, 0x72, '\u0157'); // ŗ
        AddTwoByteMapping(table, CombiningCedilla, 0x73, '\u015F'); // ş
        AddTwoByteMapping(table, CombiningCedilla, 0x74, '\u0163'); // ţ
    }

    private static void AddOgonekMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningOgonek, 0x41, '\u0104'); // Ą
        AddTwoByteMapping(table, CombiningOgonek, 0x45, '\u0118'); // Ę
        AddTwoByteMapping(table, CombiningOgonek, 0x49, '\u012E'); // Į
        AddTwoByteMapping(table, CombiningOgonek, 0x4F, '\u01EA'); // Ǫ
        AddTwoByteMapping(table, CombiningOgonek, 0x55, '\u0172'); // Ų
        AddTwoByteMapping(table, CombiningOgonek, 0x61, '\u0105'); // ą
        AddTwoByteMapping(table, CombiningOgonek, 0x65, '\u0119'); // ę
        AddTwoByteMapping(table, CombiningOgonek, 0x69, '\u012F'); // į
        AddTwoByteMapping(table, CombiningOgonek, 0x6F, '\u01EB'); // ǫ
        AddTwoByteMapping(table, CombiningOgonek, 0x75, '\u0173'); // ų
    }

    private static void AddDotBelowMappings(Dictionary<(byte, byte), char> table)
    {
        AddTwoByteMapping(table, CombiningDotBelow, 0x41, '\u1EA0'); // Ạ
        AddTwoByteMapping(table, CombiningDotBelow, 0x42, '\u1E04'); // Ḅ
        AddTwoByteMapping(table, CombiningDotBelow, 0x44, '\u1E0C'); // Ḍ
        AddTwoByteMapping(table, CombiningDotBelow, 0x45, '\u1EB8'); // Ẹ
        AddTwoByteMapping(table, CombiningDotBelow, 0x48, '\u1E24'); // Ḥ
        AddTwoByteMapping(table, CombiningDotBelow, 0x49, '\u1ECA'); // Ị
        AddTwoByteMapping(table, CombiningDotBelow, 0x4B, '\u1E32'); // Ḳ
        AddTwoByteMapping(table, CombiningDotBelow, 0x4C, '\u1E36'); // Ḷ
        AddTwoByteMapping(table, CombiningDotBelow, 0x4D, '\u1E42'); // Ṃ
        AddTwoByteMapping(table, CombiningDotBelow, 0x4E, '\u1E46'); // Ṇ
        AddTwoByteMapping(table, CombiningDotBelow, 0x4F, '\u1ECC'); // Ọ
        AddTwoByteMapping(table, CombiningDotBelow, 0x52, '\u1E5A'); // Ṛ
        AddTwoByteMapping(table, CombiningDotBelow, 0x53, '\u1E62'); // Ṣ
        AddTwoByteMapping(table, CombiningDotBelow, 0x54, '\u1E6C'); // Ṭ
        AddTwoByteMapping(table, CombiningDotBelow, 0x55, '\u1EE4'); // Ụ
        AddTwoByteMapping(table, CombiningDotBelow, 0x56, '\u1E7E'); // Ṿ
        AddTwoByteMapping(table, CombiningDotBelow, 0x57, '\u1E88'); // Ẉ
        AddTwoByteMapping(table, CombiningDotBelow, 0x59, '\u1EF4'); // Ỵ
        AddTwoByteMapping(table, CombiningDotBelow, 0x5A, '\u1E92'); // Ẓ
        AddTwoByteMapping(table, CombiningDotBelow, 0x61, '\u1EA1'); // ạ
        AddTwoByteMapping(table, CombiningDotBelow, 0x62, '\u1E05'); // ḅ
        AddTwoByteMapping(table, CombiningDotBelow, 0x64, '\u1E0D'); // ḍ
        AddTwoByteMapping(table, CombiningDotBelow, 0x65, '\u1EB9'); // ẹ
        AddTwoByteMapping(table, CombiningDotBelow, 0x68, '\u1E25'); // ḥ
        AddTwoByteMapping(table, CombiningDotBelow, 0x69, '\u1ECB'); // ị
        AddTwoByteMapping(table, CombiningDotBelow, 0x6B, '\u1E33'); // ḳ
        AddTwoByteMapping(table, CombiningDotBelow, 0x6C, '\u1E37'); // ḷ
        AddTwoByteMapping(table, CombiningDotBelow, 0x6D, '\u1E43'); // ṃ
        AddTwoByteMapping(table, CombiningDotBelow, 0x6E, '\u1E47'); // ṇ
        AddTwoByteMapping(table, CombiningDotBelow, 0x6F, '\u1ECD'); // ọ
        AddTwoByteMapping(table, CombiningDotBelow, 0x72, '\u1E5B'); // ṛ
        AddTwoByteMapping(table, CombiningDotBelow, 0x73, '\u1E63'); // ṣ
        AddTwoByteMapping(table, CombiningDotBelow, 0x74, '\u1E6D'); // ṭ
        AddTwoByteMapping(table, CombiningDotBelow, 0x75, '\u1EE5'); // ụ
        AddTwoByteMapping(table, CombiningDotBelow, 0x76, '\u1E7F'); // ṿ
        AddTwoByteMapping(table, CombiningDotBelow, 0x77, '\u1E89'); // ẉ
        AddTwoByteMapping(table, CombiningDotBelow, 0x79, '\u1EF5'); // ỵ
        AddTwoByteMapping(table, CombiningDotBelow, 0x7A, '\u1E93'); // ẓ
        AddTwoByteMapping(table, CombiningDotBelow, 0xAC, '\u1EE2'); // Ợ
        AddTwoByteMapping(table, CombiningDotBelow, 0xAD, '\u1EF0'); // Ự
        AddTwoByteMapping(table, CombiningDotBelow, 0xBC, '\u1EE3'); // ợ
        AddTwoByteMapping(table, CombiningDotBelow, 0xBD, '\u1EF1'); // ự
    }

    private static FrozenDictionary<(byte, byte, byte), char> BuildThreeByteTable()
    {
        var table = new Dictionary<(byte, byte, byte), char>(200);

        // Hook Above + Circumflex combinations
        Add3Mapping(table, CombiningHookAbove, CombiningCircumflexAccent, 0x41, '\u1EA8'); // Ẩ
        Add3Mapping(table, CombiningHookAbove, CombiningCircumflexAccent, 0x45, '\u1EC2'); // Ể
        Add3Mapping(table, CombiningHookAbove, CombiningCircumflexAccent, 0x4F, '\u1ED4'); // Ổ
        Add3Mapping(table, CombiningHookAbove, CombiningCircumflexAccent, 0x61, '\u1EA9'); // ẩ
        Add3Mapping(table, CombiningHookAbove, CombiningCircumflexAccent, 0x65, '\u1EC3'); // ể
        Add3Mapping(table, CombiningHookAbove, CombiningCircumflexAccent, 0x6F, '\u1ED5'); // ổ

        // Hook Above + Breve combinations
        Add3Mapping(table, CombiningHookAbove, CombiningBreve, 0x41, '\u1EB2'); // Ẳ
        Add3Mapping(table, CombiningHookAbove, CombiningBreve, 0x61, '\u1EB3'); // ẳ

        // Grave + Circumflex combinations
        Add3Mapping(table, CombiningGraveAccent, CombiningCircumflexAccent, 0x41, '\u1EA6'); // Ầ
        Add3Mapping(table, CombiningGraveAccent, CombiningCircumflexAccent, 0x45, '\u1EC0'); // Ề
        Add3Mapping(table, CombiningGraveAccent, CombiningCircumflexAccent, 0x4F, '\u1ED2'); // Ồ
        Add3Mapping(table, CombiningGraveAccent, CombiningCircumflexAccent, 0x61, '\u1EA7'); // ầ
        Add3Mapping(table, CombiningGraveAccent, CombiningCircumflexAccent, 0x65, '\u1EC1'); // ề
        Add3Mapping(table, CombiningGraveAccent, CombiningCircumflexAccent, 0x6F, '\u1ED3'); // ồ

        // Grave + Macron combinations
        Add3Mapping(table, CombiningGraveAccent, CombiningMacron, 0x45, '\u1E14'); // Ḕ
        Add3Mapping(table, CombiningGraveAccent, CombiningMacron, 0x4F, '\u1E50'); // Ṑ
        Add3Mapping(table, CombiningGraveAccent, CombiningMacron, 0x65, '\u1E15'); // ḕ
        Add3Mapping(table, CombiningGraveAccent, CombiningMacron, 0x6F, '\u1E51'); // ṑ

        // Grave + Breve combinations
        Add3Mapping(table, CombiningGraveAccent, CombiningBreve, 0x41, '\u1EB0'); // Ằ
        Add3Mapping(table, CombiningGraveAccent, CombiningBreve, 0x61, '\u1EB1'); // ằ

        // Grave + Diaeresis combinations
        Add3Mapping(table, CombiningGraveAccent, CombiningDiaeresis, 0x55, '\u01DB'); // Ǜ
        Add3Mapping(table, CombiningGraveAccent, CombiningDiaeresis, 0x75, '\u01DC'); // ǜ

        // Acute + Circumflex combinations
        Add3Mapping(table, CombiningAcuteAccent, CombiningCircumflexAccent, 0x41, '\u1EA4'); // Ấ
        Add3Mapping(table, CombiningAcuteAccent, CombiningCircumflexAccent, 0x45, '\u1EBE'); // Ế
        Add3Mapping(table, CombiningAcuteAccent, CombiningCircumflexAccent, 0x4F, '\u1ED0'); // Ố
        Add3Mapping(table, CombiningAcuteAccent, CombiningCircumflexAccent, 0x61, '\u1EA5'); // ấ
        Add3Mapping(table, CombiningAcuteAccent, CombiningCircumflexAccent, 0x65, '\u1EBF'); // ế
        Add3Mapping(table, CombiningAcuteAccent, CombiningCircumflexAccent, 0x6F, '\u1ED1'); // ố

        // Acute + Tilde combinations
        Add3Mapping(table, CombiningAcuteAccent, CombiningTilde, 0x4F, '\u1E4C'); // Ṍ
        Add3Mapping(table, CombiningAcuteAccent, CombiningTilde, 0x55, '\u1E78'); // Ṹ
        Add3Mapping(table, CombiningAcuteAccent, CombiningTilde, 0x6F, '\u1E4D'); // ṍ
        Add3Mapping(table, CombiningAcuteAccent, CombiningTilde, 0x75, '\u1E79'); // ṹ

        // Acute + Macron combinations
        Add3Mapping(table, CombiningAcuteAccent, CombiningMacron, 0x45, '\u1E16'); // Ḗ
        Add3Mapping(table, CombiningAcuteAccent, CombiningMacron, 0x4F, '\u1E52'); // Ṓ
        Add3Mapping(table, CombiningAcuteAccent, CombiningMacron, 0x65, '\u1E17'); // ḗ
        Add3Mapping(table, CombiningAcuteAccent, CombiningMacron, 0x6F, '\u1E53'); // ṓ

        // Acute + Breve combinations
        Add3Mapping(table, CombiningAcuteAccent, CombiningBreve, 0x41, '\u1EAE'); // Ắ
        Add3Mapping(table, CombiningAcuteAccent, CombiningBreve, 0x61, '\u1EAF'); // ắ

        // Acute + Diaeresis combinations
        Add3Mapping(table, CombiningAcuteAccent, CombiningDiaeresis, 0x49, '\u1E2E'); // Ḯ
        Add3Mapping(table, CombiningAcuteAccent, CombiningDiaeresis, 0x55, '\u01D7'); // Ǘ
        Add3Mapping(table, CombiningAcuteAccent, CombiningDiaeresis, 0x69, '\u1E2F'); // ḯ
        Add3Mapping(table, CombiningAcuteAccent, CombiningDiaeresis, 0x75, '\u01D8'); // ǘ

        // Acute + Ring Above combinations
        Add3Mapping(table, CombiningAcuteAccent, CombiningRingAbove, 0x41, '\u01FA'); // Ǻ
        Add3Mapping(table, CombiningAcuteAccent, CombiningRingAbove, 0x61, '\u01FB'); // ǻ

        // Acute + Cedilla combinations
        Add3Mapping(table, CombiningAcuteAccent, CombiningCedilla, 0x43, '\u1E08'); // Ḉ
        Add3Mapping(table, CombiningAcuteAccent, CombiningCedilla, 0x63, '\u1E09'); // ḉ

        // Circumflex + Dot Below combinations
        Add3Mapping(table, CombiningCircumflexAccent, CombiningDotBelow, 0x41, '\u1EAC'); // Ậ
        Add3Mapping(table, CombiningCircumflexAccent, CombiningDotBelow, 0x45, '\u1EC6'); // Ệ
        Add3Mapping(table, CombiningCircumflexAccent, CombiningDotBelow, 0x4F, '\u1ED8'); // Ộ
        Add3Mapping(table, CombiningCircumflexAccent, CombiningDotBelow, 0x61, '\u1EAD'); // ậ
        Add3Mapping(table, CombiningCircumflexAccent, CombiningDotBelow, 0x65, '\u1EC7'); // ệ
        Add3Mapping(table, CombiningCircumflexAccent, CombiningDotBelow, 0x6F, '\u1ED9'); // ộ

        // Tilde + Circumflex combinations
        Add3Mapping(table, CombiningTilde, CombiningCircumflexAccent, 0x41, '\u1EAA'); // Ẫ
        Add3Mapping(table, CombiningTilde, CombiningCircumflexAccent, 0x45, '\u1EC4'); // Ễ
        Add3Mapping(table, CombiningTilde, CombiningCircumflexAccent, 0x4F, '\u1ED6'); // Ỗ
        Add3Mapping(table, CombiningTilde, CombiningCircumflexAccent, 0x61, '\u1EAB'); // ẫ
        Add3Mapping(table, CombiningTilde, CombiningCircumflexAccent, 0x65, '\u1EC5'); // ễ
        Add3Mapping(table, CombiningTilde, CombiningCircumflexAccent, 0x6F, '\u1ED7'); // ỗ

        // Tilde + Breve combinations
        Add3Mapping(table, CombiningTilde, CombiningBreve, 0x41, '\u1EB4'); // Ẵ
        Add3Mapping(table, CombiningTilde, CombiningBreve, 0x61, '\u1EB5'); // ẵ

        // Macron + Tilde combinations
        Add3Mapping(table, CombiningMacron, CombiningTilde, 0x4F, '\u022C'); // Ȭ
        Add3Mapping(table, CombiningMacron, CombiningTilde, 0x6F, '\u022D'); // ȭ

        // Macron + Dot Above combinations
        Add3Mapping(table, CombiningMacron, CombiningDotAbove, 0x41, '\u01E0'); // Ǡ
        Add3Mapping(table, CombiningMacron, CombiningDotAbove, 0x4F, '\u0230'); // Ȱ
        Add3Mapping(table, CombiningMacron, CombiningDotAbove, 0x61, '\u01E1'); // ǡ
        Add3Mapping(table, CombiningMacron, CombiningDotAbove, 0x6F, '\u0231'); // ȱ

        // Macron + Diaeresis combinations
        Add3Mapping(table, CombiningMacron, CombiningDiaeresis, 0x41, '\u01DE'); // Ǟ
        Add3Mapping(table, CombiningMacron, CombiningDiaeresis, 0x4F, '\u022A'); // Ȫ
        Add3Mapping(table, CombiningMacron, CombiningDiaeresis, 0x55, '\u01D5'); // Ǖ
        Add3Mapping(table, CombiningMacron, CombiningDiaeresis, 0x61, '\u01DF'); // ǟ
        Add3Mapping(table, CombiningMacron, CombiningDiaeresis, 0x6F, '\u022B'); // ȫ
        Add3Mapping(table, CombiningMacron, CombiningDiaeresis, 0x75, '\u01D6'); // ǖ

        // Macron + Ogonek combinations
        Add3Mapping(table, CombiningMacron, CombiningOgonek, 0x4F, '\u01EC'); // Ǭ
        Add3Mapping(table, CombiningMacron, CombiningOgonek, 0x6F, '\u01ED'); // ǭ

        // Macron + Dot Below combinations
        Add3Mapping(table, CombiningMacron, CombiningDotBelow, 0x4C, '\u1E38'); // Ḹ
        Add3Mapping(table, CombiningMacron, CombiningDotBelow, 0x52, '\u1E5C'); // Ṝ
        Add3Mapping(table, CombiningMacron, CombiningDotBelow, 0x6C, '\u1E39'); // ḹ
        Add3Mapping(table, CombiningMacron, CombiningDotBelow, 0x72, '\u1E5D'); // ṝ

        // Breve + Cedilla combinations
        Add3Mapping(table, CombiningBreve, CombiningCedilla, 0x45, '\u1E1C'); // Ḝ
        Add3Mapping(table, CombiningBreve, CombiningCedilla, 0x65, '\u1E1D'); // ḝ

        // Breve + Dot Below combinations
        Add3Mapping(table, CombiningBreve, CombiningDotBelow, 0x41, '\u1EB6'); // Ặ
        Add3Mapping(table, CombiningBreve, CombiningDotBelow, 0x61, '\u1EB7'); // ặ

        // Dot Above + Acute combinations
        Add3Mapping(table, CombiningDotAbove, CombiningAcuteAccent, 0x53, '\u1E64'); // Ṥ
        Add3Mapping(table, CombiningDotAbove, CombiningAcuteAccent, 0x73, '\u1E65'); // ṥ

        // Dot Above + Caron combinations
        Add3Mapping(table, CombiningDotAbove, CombiningCaron, 0x53, '\u1E66'); // Ṧ
        Add3Mapping(table, CombiningDotAbove, CombiningCaron, 0x73, '\u1E67'); // ṧ

        // Dot Above + Dot Below combinations
        Add3Mapping(table, CombiningDotAbove, CombiningDotBelow, 0x53, '\u1E68'); // Ṩ
        Add3Mapping(table, CombiningDotAbove, CombiningDotBelow, 0x73, '\u1E69'); // ṩ

        // Diaeresis + Tilde combinations
        Add3Mapping(table, CombiningDiaeresis, CombiningTilde, 0x4F, '\u1E4E'); // Ṏ
        Add3Mapping(table, CombiningDiaeresis, CombiningTilde, 0x6F, '\u1E4F'); // ṏ

        // Diaeresis + Macron combinations
        Add3Mapping(table, CombiningDiaeresis, CombiningMacron, 0x55, '\u1E7A'); // Ṻ
        Add3Mapping(table, CombiningDiaeresis, CombiningMacron, 0x75, '\u1E7B'); // ṻ

        // Caron + Diaeresis combinations
        Add3Mapping(table, CombiningCaron, CombiningDiaeresis, 0x55, '\u01D9'); // Ǚ
        Add3Mapping(table, CombiningCaron, CombiningDiaeresis, 0x75, '\u01DA'); // ǚ

        return table.ToFrozenDictionary();
    }

    private static void Add3Mapping(Dictionary<(byte, byte, byte), char> table, byte first, byte second, byte third, char unicode)
    {
        table[(first, second, third)] = unicode;
    }

    private static FrozenDictionary<char, Marc8Bytes> BuildUnicodeToMarc8Table()
    {
        var table = new Dictionary<char, Marc8Bytes>(2000);

        // Build reverse mappings from the forward tables
        foreach (var kvp in SingleByteToUnicode)
        {
            table[kvp.Value] = new Marc8Bytes(kvp.Key);
        }

        foreach (var kvp in TwoByteToUnicode)
        {
            table[kvp.Value] = new Marc8Bytes(kvp.Key.Item1, kvp.Key.Item2);
        }

        foreach (var kvp in ThreeByteToUnicode)
        {
            table[kvp.Value] = new Marc8Bytes(kvp.Key.Item1, kvp.Key.Item2, kvp.Key.Item3);
        }

        return table.ToFrozenDictionary();
    }

    #endregion

    #region Helper Structs

    /// <summary>
    /// Represents a MARC-8 encoded character sequence.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Marc8Bytes
    {
        private readonly byte _byte1;
        private readonly byte _byte2;
        private readonly byte _byte3;
        private readonly byte _length;

        public int Length => _length;

        public Marc8Bytes(byte b1)
        {
            _byte1 = b1;
            _byte2 = 0;
            _byte3 = 0;
            _length = 1;
        }

        public Marc8Bytes(byte b1, byte b2)
        {
            _byte1 = b1;
            _byte2 = b2;
            _byte3 = 0;
            _length = 2;
        }

        public Marc8Bytes(byte b1, byte b2, byte b3)
        {
            _byte1 = b1;
            _byte2 = b2;
            _byte3 = b3;
            _length = 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<byte> destination)
        {
            switch (_length)
            {
                case 1:
                    destination[0] = _byte1;
                    break;
                case 2:
                    destination[0] = _byte1;
                    destination[1] = _byte2;
                    break;
                case 3:
                    destination[0] = _byte1;
                    destination[1] = _byte2;
                    destination[2] = _byte3;
                    break;
            }
        }
    }

    #endregion Helper Structs
}
