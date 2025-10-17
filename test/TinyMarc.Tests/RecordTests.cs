using System.Text;
using System.Text.Json;

namespace TinyMarc.Tests;

public sealed class RecordTests
{

    #region CalculateLeader Tests

    [Fact]
    public void CalculateLeader_ReturnsExactly24Characters()
    {
        Record record = new Record();

        string leader = record.CalculateLeader();

        Assert.Equal(MarcConstants.LeaderLength, leader.Length);
    }

    [Fact]
    public void CalculateLeader_PlacesSemanticPropertiesInCorrectPositions()
    {
        var record = new Record
        {
            RecordType = RecordType.NotatedMusic,
            CharacterEncoding = CharacterEncoding.MARC8
        };

        string leader = record.CalculateLeader();

        // Verify semantic properties are placed correctly
        Assert.Equal((char)RecordType.NotatedMusic, leader[6]);
        Assert.Equal((char)CharacterEncoding.MARC8, leader[9]);

        // Verify constants
        Assert.Equal("22", leader.Substring(10, 2));
        Assert.Equal("4500", leader.Substring(20, 4));
    }

    [Fact]
    public void CalculateLeader_WithNoFields_CalculatesMinimalLength()
    {
        Record record = new Record();

        string leader = record.CalculateLeader();

        // Positions 0-4: Record length (should be minimal for empty record)
        string lengthStr = leader.Substring(0, 5);
        int length = int.Parse(lengthStr);

        // Empty record: leader(24) + directory terminator(1) + record terminator(1) = 26
        Assert.Equal(26, length);
    }

    [Fact]
    public void CalculateLeader_WithFields_CalculatesLengthAndBaseAddress()
    {
        var record = new Record();
        record.AppendField(new ControlField("001", "12345"));

        string leader = record.CalculateLeader();

        // Verify length
        int length = int.Parse(leader.Substring(0, 5));
        Assert.Equal(44, length);

        // Verify base address
        int baseAddress = int.Parse(leader.Substring(12, 5));
        Assert.Equal(37, baseAddress);
    }

    [Fact]
    public void CalculateLeader_WithLargeRecord_AddsWarningAndCapsLength()
    {
        Record record = new Record();

        // Add many large fields to exceed max length
        for (int i = 0; i < 1000; i++)
        {
            record.AppendField(new ControlField("001", new string('x', 100)));
        }

        string leader = record.CalculateLeader();

        Assert.NotEmpty(record.Warnings);
        Assert.Contains(record.Warnings, w => w.Contains("exceeds maximum"));

        // Length should be capped
        string lengthStr = leader.Substring(0, 5);
        int length = int.Parse(lengthStr);
        Assert.Equal(MarcConstants.MaxRecordLength, length);
    }

    #endregion

    #region SetLeaderFromString Tests

    [Fact]
    public void SetLeaderFromString_ExtractsAllSemanticPositions()
    {
        Record record = new Record();
        // Construct a leader with specific values at semantic positions
        char[] leaderChars = new string(' ', 24).ToCharArray();
        leaderChars[5] = (char)RecordStatus.Deleted;
        leaderChars[6] = (char)RecordType.ComputerFile;
        leaderChars[7] = (char)BibliographicLevel.Collection;
        leaderChars[8] = (char)TypeOfControl.Archival;
        leaderChars[9] = (char)CharacterEncoding.MARC8;
        leaderChars[17] = (char)EncodingLevel.AbbreviatedLevel;
        leaderChars[18] = (char)DescriptiveCatalogingForm.AACR2;
        leaderChars[19] = (char)MultipartResourceLevel.Set;
        string leader = new string(leaderChars);

        record.SetLeaderFromString(leader);

        Assert.Equal(RecordStatus.Deleted, record.RecordStatus);
        Assert.Equal(RecordType.ComputerFile, record.RecordType);
        Assert.Equal(BibliographicLevel.Collection, record.BibliographicLevel);
        Assert.Equal(TypeOfControl.Archival, record.TypeOfControl);
        Assert.Equal(CharacterEncoding.MARC8, record.CharacterEncoding);
        Assert.Equal(EncodingLevel.AbbreviatedLevel, record.EncodingLevel);
        Assert.Equal(DescriptiveCatalogingForm.AACR2, record.CatalogingForm);
        Assert.Equal(MultipartResourceLevel.Set, record.MultipartLevel);
    }

    [Fact]
    public void SetLeaderFromString_WithEmpty_ThrowsArgumentException()
    {
        Record record = new Record();

        Assert.Throws<ArgumentException>(() => record.SetLeaderFromString(string.Empty));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("this is way too long for a leader")]
    [InlineData("12345678901234567890abc")] // 23 chars
    [InlineData("12345678901234567890abcde")] // 25 chars
    public void SetLeaderFromString_WithWrongLength_ThrowsMarcInvalidLeaderLengthException(string leader)
    {
        Record record = new Record();

        Assert.Throws<MarcInvalidLeaderLengthException>(() => record.SetLeaderFromString(leader));
    }

    #endregion

    #region Warning Tests

    [Fact]
    public void AddWarning_AddsToWarningsList()
    {
        Record record = new Record();

        record.AddWarning("Test warning");

        Assert.Single(record.Warnings);
        Assert.Contains("Test warning", record.Warnings);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddWarning_WithNullOrEmpty_ThrowsArgumentException(string? warning)
    {
        Record record = new Record();

        Assert.ThrowsAny<ArgumentException>(() => record.AddWarning(warning!));
    }

    #endregion

    #region AppendField Tests

    [Fact]
    public void AppendField_AddsToEnd()
    {
        var record = new Record();
        var field1 = new ControlField("001", "first");
        var field2 = new ControlField("005", "second");

        record.PrependField(field1);  // Add via different method
        record.AppendField(field2);   // Now test AppendField

        Assert.Equal(2, record.Fields.Count);
        Assert.Same(field1, record.Fields[0]);
        Assert.Same(field2, record.Fields[1]);  // ← Proves it went to the END
    }

    [Fact]
    public void AppendField_WithNull_ThrowsArgumentNullException()
    {
        Record record = new Record();

        Assert.Throws<ArgumentNullException>(() => record.AppendField(null!));
    }

    #endregion

    #region PrependField Tests

    [Fact]
    public void PrependField_AddsFieldToStart()
    {
        Record record = new Record();
        ControlField field1 = new ControlField("001", "first");
        ControlField field2 = new ControlField("005", "second");

        record.AppendField(field1);
        record.PrependField(field2);

        Assert.Equal(2, record.Fields.Count);
        Assert.Same(field2, record.Fields[0]);
        Assert.Same(field1, record.Fields[1]);
    }

    [Fact]
    public void PrependField_WithNull_ThrowsArgumentNullException()
    {
        Record record = new Record();

        Assert.Throws<ArgumentNullException>(() => record.PrependField(null!));
    }

    #endregion

    #region InsertField Tests

    [Fact]
    public void InsertField_BeforeExisting_InsertsAtCorrectPosition()
    {
        Record record = new Record();
        ControlField field1 = new ControlField("001", "first");
        ControlField field2 = new ControlField("005", "second");
        ControlField newField = new ControlField("003", "inserted");

        record.AppendField(field1);
        record.AppendField(field2);
        record.InsertField(newField, field2, before: true);

        Assert.Equal(3, record.Fields.Count);
        Assert.Same(field1, record.Fields[0]);
        Assert.Same(newField, record.Fields[1]);
        Assert.Same(field2, record.Fields[2]);
    }

    [Fact]
    public void InsertField_AfterExisting_InsertsAtCorrectPosition()
    {
        Record record = new Record();
        ControlField field1 = new ControlField("001", "first");
        ControlField field2 = new ControlField("005", "second");
        ControlField newField = new ControlField("003", "inserted");

        record.AppendField(field1);
        record.AppendField(field2);
        record.InsertField(newField, field1, before: false);

        Assert.Equal(3, record.Fields.Count);
        Assert.Same(field1, record.Fields[0]);
        Assert.Same(newField, record.Fields[1]);
        Assert.Same(field2, record.Fields[2]);
    }

    [Fact]
    public void InsertField_WithNonExistentField_ThrowsArgumentException()
    {
        Record record = new Record();
        ControlField field1 = new ControlField("001", "first");
        ControlField nonExistent = new ControlField("005", "not added");
        ControlField newField = new ControlField("003", "new");

        record.AppendField(field1);

        var ex = Assert.Throws<ArgumentException>(() =>
            record.InsertField(newField, nonExistent));

        Assert.Equal("existingField", ex.ParamName);
    }

    [Fact]
    public void InsertField_WithNullNewField_ThrowsArgumentNullException()
    {
        Record record = new Record();
        ControlField existing = new ControlField("001", "test");
        record.AppendField(existing);

        Assert.Throws<ArgumentNullException>(() =>
            record.InsertField(null!, existing));
    }

    [Fact]
    public void InsertField_WithNullExistingField_ThrowsArgumentNullException()
    {
        Record record = new Record();
        ControlField newField = new ControlField("001", "test");

        Assert.Throws<ArgumentNullException>(() => record.InsertField(newField, null!));
    }

    #endregion

    #region RemoveField Tests

    [Fact]
    public void RemoveField_WithExisting_ReturnsTrue()
    {
        Record record = new Record();
        ControlField field = new ControlField("001", "test");
        record.AppendField(field);

        bool result = record.RemoveField(field);

        Assert.True(result);
        Assert.Empty(record.Fields);
    }

    [Fact]
    public void RemoveField_WithNonExistent_ReturnsFalse()
    {
        Record record = new Record();
        ControlField field1 = new ControlField("001", "test");
        ControlField field2 = new ControlField("005", "other");
        record.AppendField(field1);

        bool result = record.RemoveField(field2);

        Assert.False(result);
        Assert.Single(record.Fields);
    }

    #endregion

    #region RemoveFields Tests

    [Fact]
    public void RemoveFields_WithMatchingTag_RemovesAll()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "first"));
        record.AppendField(new ControlField("001", "second"));
        record.AppendField(new ControlField("005", "other"));

        int removed = record.RemoveFields("001");

        Assert.Equal(2, removed);
        Assert.Single(record.Fields);
        Assert.Equal("005", record.Fields[0].Tag);
    }

    [Fact]
    public void RemoveFields_WithNoMatches_ReturnsZero()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "test"));

        int removed = record.RemoveFields("999");

        Assert.Equal(0, removed);
        Assert.Single(record.Fields);
    }

    [Fact]
    public void RemoveFields_WithRegex_RemovesMatching()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "test"));
        record.AppendField(new ControlField("002", "test"));
        record.AppendField(new ControlField("003", "test"));
        record.AppendField(new DataField("100", '1', ' '));

        int removed = record.RemoveFields("00[1-3]", useRegex: true);

        Assert.Equal(3, removed);
        Assert.Single(record.Fields);
        Assert.Equal("100", record.Fields[0].Tag);
    }

    #endregion

    #region GetField Tests

    [Fact]
    public void GetField_WithMatch_ReturnsFirst()
    {
        Record record = new Record();
        ControlField field1 = new ControlField("001", "first");
        ControlField field2 = new ControlField("001", "second");
        ControlField field3 = new ControlField("005", "other");

        record.AppendField(field1);
        record.AppendField(field2);
        record.AppendField(field3);

        var result = record.GetField("001");

        Assert.Same(field1, result);
    }

    [Fact]
    public void GetField_WithNoMatch_ReturnsNull()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "test"));

        var result = record.GetField("999");

        Assert.Null(result);
    }

    [Fact]
    public void GetField_WithRegex_ReturnsFirstMatch()
    {
        Record record = new Record();
        ControlField field1 = new ControlField("001", "test");
        ControlField field2 = new ControlField("002", "test");
        DataField field3 = new DataField("100", '1', ' ');

        record.AppendField(field1);
        record.AppendField(field2);
        record.AppendField(field3);

        var result = record.GetField("00[1-3]", useRegex: true);

        Assert.Same(field1, result);
    }

    #endregion

    #region GetFields Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetFields_WithNullOrEmpty_ReturnsAllFields(string? tag)
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "test"));
        record.AppendField(new ControlField("005", "test"));

        List<Field> results = record.GetFields(tag).ToList();

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void GetFields_WithTag_ReturnsAllMatching()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "first"));
        record.AppendField(new ControlField("001", "second"));
        record.AppendField(new ControlField("005", "other"));

        List<Field> results = record.GetFields("001").ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, f => Assert.Equal("001", f.Tag));
    }

    [Fact]
    public void GetFields_WithNoMatches_ReturnsEmpty()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "test"));

        var results = record.GetFields("999");

        Assert.Empty(results);
    }

    [Fact]
    public void GetFields_WithRegex_ReturnsAllMatching()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "test"));
        record.AppendField(new ControlField("002", "test"));
        record.AppendField(new ControlField("003", "test"));
        record.AppendField(new DataField("100", '1', ' '));

        List<Field> results = record.GetFields("00[1-3]", useRegex: true).ToList();

        Assert.Equal(3, results.Count);
        Assert.All(results, f => Assert.StartsWith("00", f.Tag));
    }

    #endregion

    #region ToMarc Tests

    [Fact]
    public void ToMarc_WithEmptyRecord_ReturnsLeaderWithTerminators()
    {
        Record record = new Record();

        string marc = record.ToMarc();

        Assert.EndsWith("\u001D", marc); // Ends with record terminator
        Assert.Contains("\u001E", marc); // Contains field terminator
    }

    [Fact]
    public void ToMarc_WithFields_IncludesCalculatedLeader()
    {
        Record record = new Record
        {
            RecordType = RecordType.NotatedMusic
        };
        record.AppendField(new ControlField("001", "12345"));

        string marc = record.ToMarc();

        // Leader should have RecordType at position 6
        Assert.Equal((char)RecordType.NotatedMusic, marc[6]);
    }

    [Fact]
    public void ToMarc_WithFields_IncludesDirectoryAndFieldData()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "12345"));

        string marc = record.ToMarc();

        // Should contain field tag in directory
        Assert.Contains("001", marc);
        // Should contain field data
        Assert.Contains("12345", marc);
        // Should end with record terminator
        Assert.EndsWith("\u001D", marc);
    }

    [Fact]
    public void ToMarc_SkipsEmptyFields()
    {
        var record = new Record();
        record.AppendField(new ControlField("001", "test"));
        record.AppendField(new DataField("999", '1', ' '));

        string marc = record.ToMarc();

        Assert.Contains("001", marc);
        Assert.DoesNotContain("999", marc);
    }

    [Fact]
    public void ToMarc_LeaderLengthMatchesActualLength()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "test"));

        string marc = record.ToMarc();

        // Extract declared length from leader
        string lengthStr = marc.Substring(0, 5);
        int declaredLength = int.Parse(lengthStr);

        Assert.Equal(marc.Length, declaredLength);
    }

    [Fact]
    public void ToMarc_WithDataField_IncludesIndicatorsAndSubfields()
    {
        Record record = new Record();
        DataField dataField = new DataField("100", '1', '0');
        dataField.AppendSubfield(new Subfield('a', "Author"));
        record.AppendField(dataField);

        string marc = record.ToMarc();

        Assert.Contains("100", marc);
        Assert.Contains("Author", marc);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_IncludesCalculatedLeader()
    {
        Record record = new Record
        {
            RecordType = RecordType.ComputerFile
        };

        string result = record.ToString();

        Assert.Contains("LDR", result);
        Assert.Contains("m", result); // ComputerFile = 'm'
    }

    [Fact]
    public void ToString_IncludesFields()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "test"));

        string result = record.ToString();

        Assert.Contains("001", result);
        Assert.Contains("test", result);
    }

    [Fact]
    public void ToString_SkipsEmptyFields()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "test"));
        record.AppendField(new DataField("100", '1', ' ')); // Empty

        string result = record.ToString();

        Assert.Contains("001", result);
        Assert.DoesNotContain("100", result);
    }

    #endregion

    #region ToJson Tests

    [Fact]
    public void ToJson_IncludesCalculatedLeader()
    {
        Record record = new Record
        {
            RecordType = RecordType.MusicalSoundRecording
        };

        string json = record.ToJson();
        JsonDocument doc = JsonDocument.Parse(json);

        string leader = doc.RootElement.GetProperty("leader").GetString()!;
        Assert.Equal((char)RecordType.MusicalSoundRecording, leader[6]);
    }

    [Fact]
    public void ToJson_IncludesControlFields()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "12345"));

        string json = record.ToJson();
        JsonDocument doc = JsonDocument.Parse(json);

        var fields = doc.RootElement.GetProperty("fields");
        Assert.Equal(1, fields.GetArrayLength());

        var field = fields[0];
        Assert.True(field.TryGetProperty("001", out var value));
        Assert.Equal("12345", value.GetString());
    }

    [Fact]
    public void ToJson_IncludesDataFields()
    {
        Record record = new Record();

        DataField dataField = new DataField("100", '1', '0');
        dataField.AppendSubfield(new Subfield('a', "Test Author"));
        record.AppendField(dataField);

        string json = record.ToJson();
        JsonDocument doc = JsonDocument.Parse(json);

        var fields = doc.RootElement.GetProperty("fields");
        var field = fields[0].GetProperty("100");

        Assert.Equal("1", field.GetProperty("ind1").GetString());
        Assert.Equal("0", field.GetProperty("ind2").GetString());

        var subfields = field.GetProperty("subfields");
        Assert.Equal(1, subfields.GetArrayLength());
        Assert.Equal("Test Author", subfields[0].GetProperty("a").GetString());
    }

    [Fact]
    public void ToJson_SkipsEmptyFields()
    {
        Record record = new Record();
        record.AppendField(new ControlField("001", "test"));
        record.AppendField(new DataField("100", '1', ' ')); // Empty

        string json = record.ToJson();
        JsonDocument doc = JsonDocument.Parse(json);

        var fields = doc.RootElement.GetProperty("fields");
        Assert.Equal(1, fields.GetArrayLength()); // Only non-empty field
    }

    #endregion

    [Fact]
    public void ToBytes_ReturnsValidByteArray()
    {
        var record = new Record();
        record.AppendField(new ControlField("001", "test-bytes"));

        byte[] bytes = record.ToBytes();

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);

        // Should be able to parse back
        var parsed = MarcReader.FromBytes(bytes);
        Assert.Equal("test-bytes", parsed.GetControlFieldData("001"));
    }

    [Fact]
    public void ToBytes_WithUTF8Encoding_UsesUTF8()
    {
        var record = new Record
        {
            CharacterEncoding = CharacterEncoding.UTF8
        };
        record.AppendField(new ControlField("001", "utf8-test"));

        byte[] bytes = record.ToBytes();

        // Verify encoding is UTF-8 by checking leader position 9
        string leader = Encoding.ASCII.GetString(bytes, 0, 24);
        Assert.Equal('a', leader[9]); // 'a' indicates UTF-8
    }

    [Fact]
    public void ToBytes_WithMARC8Encoding_UsesMARC8()
    {
        var record = new Record
        {
            CharacterEncoding = CharacterEncoding.MARC8
        };
        record.AppendField(new ControlField("001", "marc8-test"));

        byte[] bytes = record.ToBytes();

        // Verify encoding is MARC-8 by checking leader position 9
        string leader = Encoding.ASCII.GetString(bytes, 0, 24);
        Assert.Equal(' ', leader[9]); // space indicates MARC-8
    }

    [Fact]
    public void ToBytes_WithSpecifiedEncoding_UsesSpecifiedEncoding()
    {
        var record = new Record();
        record.AppendField(new ControlField("001", "encoding-test"));

        byte[] utf8Bytes = record.ToBytes(Encoding.UTF8);
        byte[] asciiBytes = record.ToBytes(Encoding.ASCII);

        Assert.NotNull(utf8Bytes);
        Assert.NotNull(asciiBytes);
        // Different encodings may produce different byte lengths
        Assert.True(utf8Bytes.Length > 0);
        Assert.True(asciiBytes.Length > 0);
    }

    [Fact]
    public void ToBytes_WithNullEncoding_ThrowsArgumentNullException()
    {
        var record = new Record();

        Assert.Throws<ArgumentNullException>(() => record.ToBytes(null!));
    }
}
