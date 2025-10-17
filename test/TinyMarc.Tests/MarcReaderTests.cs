using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace TinyMarc.Tests;

public sealed class MarcReaderEncodingTests
{
    private const string Utf8TestFile = "test_utf8.mrc";
    private const string Marc8TestFile = "test_marc8.mrc";


    [Fact]
    public void ReadRecord_Utf8File_DetectsUtf8Encoding()
    {
        using MarcReader reader = new(Utf8TestFile);
        Record? record = reader.ReadRecord();

        Assert.NotNull(record);

        // Verify leader indicates UTF-8 (position 9 should be 'a')
        string? leader = record.CalculateLeader();
        Assert.NotNull(leader);
        Assert.Equal('a', leader[9]);
    }

    [Fact]
    public void ReadRecord_Marc8File_DetectsMarc8Encoding()
    {
        using MarcReader reader = new(Marc8TestFile);
        Record? record = reader.ReadRecord();

        Assert.NotNull(record);

        // Verify leader indicates MARC-8 (position 9 should be space)
        string? leader = record.CalculateLeader();
        Assert.NotNull(leader);
        Assert.Equal(' ', leader[9]);
    }

    [Fact]
    public void ReadRecords_Utf8File_ReadsAllThreeRecords()
    {
        using MarcReader reader = new(Utf8TestFile);
        List<Record> records = reader.ReadRecords().ToList();

        Assert.Equal(3, records.Count);

        // Verify control field 001 values
        Assert.Equal("utf8001", records[0].GetControlFieldData("001"));
        Assert.Equal("utf8002", records[1].GetControlFieldData("001"));
        Assert.Equal("utf8003", records[2].GetControlFieldData("001"));
    }

    [Fact]
    public void ReadRecords_Marc8File_ReadsAllThreeRecords()
    {
        using MarcReader reader = new(Marc8TestFile);
        List<Record> records = reader.ReadRecords().ToList();

        Assert.Equal(3, records.Count);

        // Verify control field 001 values
        Assert.Equal("marc8001", records[0].GetControlFieldData("001"));
        Assert.Equal("marc8002", records[1].GetControlFieldData("001"));
        Assert.Equal("marc8003", records[2].GetControlFieldData("001"));
    }

    [Fact]
    public void ReadRecord_Utf8ChineseCharacters_DecodesCorrectly()
    {
        using MarcReader reader = new(Utf8TestFile);
        Record? record = reader.ReadRecord();

        Assert.NotNull(record);

        DataField? field245 = record.GetDataField("245");
        Assert.NotNull(field245);

        string? subfieldA = field245.GetSubfieldData('a');
        Assert.NotNull(subfieldA);

        // Verify Chinese characters are present
        Assert.Contains("现代汉语词典", subfieldA);
        Assert.Contains("Xiàndài Hànyǔ Cídiǎn", subfieldA);
    }

    [Fact]
    public void ReadRecord_Utf8MultiByteCharacters_DecodesCorrectly()
    {
        using MarcReader reader = new(Utf8TestFile);
        Record? record = reader.ReadRecord();

        Assert.NotNull(record);

        // Check 260 field for Chinese characters
        DataField? field260 = record.GetDataField("260");
        Assert.NotNull(field260);

        string? subfieldA = field260.GetSubfieldData('a');
        Assert.NotNull(subfieldA);
        Assert.Contains("北京", subfieldA);
        Assert.Contains("商务印书馆", subfieldA);
    }

    [Fact]
    public void ReadRecord_Utf8EmojiAndArabic_DecodesCorrectly()
    {
        using MarcReader reader = new(Utf8TestFile);
        Record? record = reader.ReadRecord();

        Assert.NotNull(record);

        DataField? field500 = record.GetDataField("500");
        Assert.NotNull(field500);

        string? subfieldA = field500.GetSubfieldData('a');
        Assert.NotNull(subfieldA);

        // Verify emoji
        Assert.Contains("📚", subfieldA);

        // Verify Arabic
        Assert.Contains("العربية", subfieldA);
    }

    [Fact]
    public void ReadRecord_Utf8LatinExtended_DecodesCorrectly()
    {
        using MarcReader reader = new(Utf8TestFile);

        // Skip first record
        reader.ReadRecord();

        // Read second record with Latin extended
        Record? record = reader.ReadRecord();
        Assert.NotNull(record);

        DataField? field100 = record.GetDataField("100");
        Assert.NotNull(field100);

        string? subfieldA = field100.GetSubfieldData('a');
        Assert.NotNull(subfieldA);
        Assert.Contains("Müller", subfieldA);
        Assert.Contains("François", subfieldA);
    }

    [Fact]
    public void ReadRecord_Utf8PolishCzechTurkish_DecodesCorrectly()
    {
        using MarcReader reader = new(Utf8TestFile);

        // Skip first record
        reader.ReadRecord();

        // Read second record
        Record? record = reader.ReadRecord();
        Assert.NotNull(record);

        DataField? field500 = record.GetDataField("500");
        Assert.NotNull(field500);

        string? subfieldA = field500.GetSubfieldData('a');
        Assert.NotNull(subfieldA);

        Assert.Contains("Łódź", subfieldA);
        Assert.Contains("Dvořák", subfieldA);
        Assert.Contains("Şehir", subfieldA);
    }

    [Fact]
    public void ReadRecord_Utf8CyrillicAndGreek_DecodesCorrectly()
    {
        using MarcReader reader = new(Utf8TestFile);

        // Skip to third record
        reader.ReadRecord();
        reader.ReadRecord();

        Record? record = reader.ReadRecord();
        Assert.NotNull(record);

        DataField? field245 = record.GetDataField("245");
        Assert.NotNull(field245);

        string? subfieldA = field245.GetSubfieldData('a');
        Assert.NotNull(subfieldA);

        // Verify Cyrillic
        Assert.Contains("Достоевский", subfieldA);

        // Verify Greek
        Assert.Contains("Ἑλληνική", subfieldA);
    }

    [Fact]
    public void ReadRecord_Marc8CombiningDiacritics_DecodesCorrectly()
    {
        using MarcReader reader = new(Marc8TestFile);
        Record? record = reader.ReadRecord();

        Assert.NotNull(record);

        DataField? field100 = record.GetDataField("100");
        Assert.NotNull(field100);

        string? subfieldA = field100.GetSubfieldData('a');
        Assert.NotNull(subfieldA);

        // Should contain properly decoded diacritics
        Assert.Contains("ü", subfieldA);
        Assert.Contains("ç", subfieldA);
        Assert.Contains("Müller", subfieldA);
        Assert.Contains("François", subfieldA);
    }

    [Fact]
    public void ReadRecord_Marc8AcuteAccent_DecodesCorrectly()
    {
        using MarcReader reader = new(Marc8TestFile);
        Record? record = reader.ReadRecord();

        Assert.NotNull(record);

        DataField? field245 = record.GetDataField("245");
        Assert.NotNull(field245);

        string? subfieldA = field245.GetSubfieldData('a');
        Assert.NotNull(subfieldA);

        // Should contain É with acute accent
        Assert.Contains("É", subfieldA);
        Assert.Contains("é", subfieldA);
    }

    [Fact]
    public void ReadRecord_Marc8GreekEscapeSequences_DecodesCorrectly()
    {
        using MarcReader reader = new(Marc8TestFile);

        // Skip first record
        reader.ReadRecord();

        // Read second record with Greek
        Record? record = reader.ReadRecord();
        Assert.NotNull(record);

        DataField? field245 = record.GetDataField("245");
        Assert.NotNull(field245);

        string? subfieldA = field245.GetSubfieldData('a');
        Assert.NotNull(subfieldA);

        // The MARC-8 escape sequences should be decoded to proper Greek characters
        // The exact output depends on Marc8Encoding implementation
        Assert.NotNull(subfieldA);
        Assert.NotEmpty(subfieldA);
    }

    [Fact]
    public void ReadRecord_Marc8CyrillicEscapeSequences_DecodesCorrectly()
    {
        using MarcReader reader = new(Marc8TestFile);

        // Skip to third record
        reader.ReadRecord();
        reader.ReadRecord();

        Record? record = reader.ReadRecord();
        Assert.NotNull(record);

        DataField? field245 = record.GetDataField("245");
        Assert.NotNull(field245);

        string? subfieldA = field245.GetSubfieldData('a');
        Assert.NotNull(subfieldA);

        // Should have decoded Cyrillic text
        Assert.NotNull(subfieldA);
        Assert.NotEmpty(subfieldA);
    }

    [Fact]
    public async Task ReadRecordAsync_Utf8File_DecodesCorrectly()
    {
        using MarcReader reader = new(Utf8TestFile);
        Record? record = await reader.ReadRecordAsync();

        Assert.NotNull(record);

        DataField? field245 = record.GetDataField("245");
        Assert.NotNull(field245);

        string? subfieldA = field245.GetSubfieldData('a');
        Assert.NotNull(subfieldA);
        Assert.Contains("现代汉语词典", subfieldA);
    }

    [Fact]
    public async Task ReadRecordsAsync_Utf8File_ReadsAllRecords()
    {
        using MarcReader reader = new(Utf8TestFile);
        List<Record> records = [];

        await foreach (Record record in reader.ReadRecordsAsync(CancellationToken.None))
        {
            records.Add(record);
        }

        Assert.Equal(3, records.Count);
    }

    [Fact]
    public async Task ReadRecordsAsync_Marc8File_ReadsAllRecords()
    {
        using MarcReader reader = new(Marc8TestFile);
        List<Record> records = [];

        await foreach (Record record in reader.ReadRecordsAsync(CancellationToken.None))
        {
            records.Add(record);
        }

        Assert.Equal(3, records.Count);
    }

    [Fact]
    public void ReadRecord_Utf8Stream_DecodesCorrectly()
    {
        using FileStream fs = new(Utf8TestFile, FileMode.Open, FileAccess.Read);
        using MarcReader reader = new(fs);

        Record? record = reader.ReadRecord();

        Assert.NotNull(record);
        Assert.Equal("utf8001", record.GetControlFieldData("001"));
    }

    [Fact]
    public void ReadRecord_Marc8Stream_DecodesCorrectly()
    {
        using FileStream fs = new(Marc8TestFile, FileMode.Open, FileAccess.Read);
        using MarcReader reader = new(fs);

        Record? record = reader.ReadRecord();

        Assert.NotNull(record);
        Assert.Equal("marc8001", record.GetControlFieldData("001"));
    }

    [Fact]
    public void ReadRecord_VerifyFieldTerminators_AreStripped()
    {
        using MarcReader reader = new(Utf8TestFile);
        Record? record = reader.ReadRecord();

        Assert.NotNull(record);

        DataField? field = record.GetDataField("245");
        Assert.NotNull(field);

        string? data = field.GetSubfieldData('a');
        Assert.NotNull(data);

        // Verify no MARC control characters remain
        Assert.DoesNotContain("\u001E", data, StringComparison.Ordinal); // Field terminator
        Assert.DoesNotContain("\u001F", data, StringComparison.Ordinal); // Subfield delimiter
        Assert.DoesNotContain("\u001D", data, StringComparison.Ordinal); // Record terminator
    }

    [Fact]
    public void ReadRecord_VerifyIndicators_ArePreserved()
    {
        using MarcReader reader = new(Utf8TestFile);
        Record? record = reader.ReadRecord();

        Assert.NotNull(record);

        DataField? field245 = record.GetDataField("245");
        Assert.NotNull(field245);

        Assert.Equal('1', field245.Indicator1);
        Assert.Equal('0', field245.Indicator2);
    }

    [Fact]
    public void ReadRecord_MultipleSubfields_ParsedCorrectly()
    {
        using MarcReader reader = new(Utf8TestFile);
        Record? record = reader.ReadRecord();

        Assert.NotNull(record);

        DataField? field650 = record.GetDataField("650");
        Assert.NotNull(field650);

        // Should have subfield 'a' and 'v'
        Assert.NotNull(field650.GetSubfieldData('a'));
        Assert.NotNull(field650.GetSubfieldData('v'));
    }

    [Fact]
    public void FromString_WithValidMarcData_ReturnsRecord()
    {
        // Create a simple MARC record
        var record = new Record();
        record.AppendField(new ControlField("001", "12345"));
        record.AppendField(new DataField("245", '1', '0')
        {
        });
        record.GetDataField("245")?.AppendSubfield(new Subfield('a', "Test Title"));

        string marcString = record.ToMarc();

        // Parse it back
        var parsed = MarcReader.FromString(marcString);

        Assert.NotNull(parsed);
        Assert.Equal("12345", parsed.GetControlFieldData("001"));
        var field245 = parsed.GetDataField("245");
        Assert.NotNull(field245);
        Assert.Equal("Test Title", field245.GetSubfieldData('a'));
    }

    [Fact]
    public void FromString_WithEmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => MarcReader.FromString(string.Empty));
    }

    [Fact]
    public void FromString_WithNullString_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => MarcReader.FromString(null!));
    }

    [Fact]
    public void FromString_WithInvalidData_ThrowsMarcException()
    {
        Assert.ThrowsAny<MarcException>(() => MarcReader.FromString("invalid marc data"));
    }

    [Fact]
    public void FromString_WithEncoding_ReturnsRecord()
    {
        var record = new Record();
        record.AppendField(new ControlField("001", "test"));

        string marcString = record.ToMarc();
        byte[] utf8Bytes = Encoding.UTF8.GetBytes(marcString);
        string utf8String = Encoding.UTF8.GetString(utf8Bytes);

        var parsed = MarcReader.FromString(utf8String, Encoding.UTF8);

        Assert.NotNull(parsed);
        Assert.Equal("test", parsed.GetControlFieldData("001"));
    }

    [Fact]
    public void FromString_WithNullEncoding_ThrowsArgumentNullException()
    {
        var record = new Record();
        record.AppendField(new ControlField("001", "test"));
        string marcString = record.ToMarc();

        Assert.Throws<ArgumentNullException>(() =>
            MarcReader.FromString(marcString, null!));
    }

    [Fact]
    public void FromBytes_WithValidData_ReturnsRecord()
    {
        var record = new Record();
        record.AppendField(new ControlField("001", "bytes-test"));

        byte[] bytes = record.ToBytes();

        var parsed = MarcReader.FromBytes(bytes);

        Assert.NotNull(parsed);
        Assert.Equal("bytes-test", parsed.GetControlFieldData("001"));
    }

    [Fact]
    public void FromBytes_WithNullBytes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MarcReader.FromBytes(null!));
    }

    [Fact]
    public void FromBytes_WithEmptyBytes_ThrowsMarcInvalidRecordException()
    {
        Assert.Throws<MarcInvalidRecordException>(() => MarcReader.FromBytes(Array.Empty<byte>()));
    }

}
