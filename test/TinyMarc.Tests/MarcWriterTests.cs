using System.Diagnostics;

namespace TinyMarc.Tests;

public sealed class MarcWriterTests
{
    [Theory]
    [InlineData("test_utf8.mrc")]
    [InlineData("test_marc8.mrc")]
    public void WriteRecord_ToFile_CreatesValidFile(string path)
    {
        using MarcReader reader = new MarcReader(path);
        Record? record = reader.ReadRecord();

        Assert.NotNull(record);

        string outputPath = Path.GetTempFileName();
        try
        {
            using (MarcWriter writer = new MarcWriter(outputPath))
            {
                writer.WriteRecord(record);
            }

            // Verify the file was created and has content
            Assert.True(File.Exists(outputPath));
            FileInfo fileInfo = new FileInfo(outputPath);
            Assert.True(fileInfo.Length > 0);

            // Verify we can read it back
            using var verifyReader = new MarcReader(outputPath);
            Record? readBack = verifyReader.ReadRecord();
            Assert.NotNull(readBack);

            Debug.WriteLine("Original: " + record.ToString());
            Debug.WriteLine("ReadBack: " + readBack.ToString());

            Assert.Equal(record.ToString(), readBack.ToString());
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void WriteToString_ReturnsValidMarcString()
    {
        var record = new Record();
        record.AppendField(new ControlField("001", "string-test"));
        record.AppendField(new DataField("245", '1', '0'));
        record.GetDataField("245")?.AppendSubfield(new Subfield('a', "Title"));

        string marcString = MarcWriter.WriteToString(record);

        Assert.NotNull(marcString);
        Assert.NotEmpty(marcString);

        // Should start with a valid leader
        Assert.True(marcString.Length >= 24);

        // Should be parseable
        var parsed = MarcReader.FromString(marcString);
        Assert.Equal("string-test", parsed.GetControlFieldData("001"));
    }

    [Fact]
    public void WriteToString_WithNullRecord_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MarcWriter.WriteToString(null!));
    }

    [Fact]
    public void WriteToString_WithComplexRecord_PreservesData()
    {
        var record = new Record
        {
            RecordType = RecordType.ComputerFile,
            BibliographicLevel = BibliographicLevel.Monograph
        };

        record.AppendField(new ControlField("001", "complex-test"));
        record.AppendField(new ControlField("008", "230101s2023    nyu     o     000 0 eng d"));

        var field245 = new DataField("245", '1', '0');
        field245.AppendSubfield(new Subfield('a', "Complex Title /"));
        field245.AppendSubfield(new Subfield('c', "by Author."));
        record.AppendField(field245);

        string marcString = MarcWriter.WriteToString(record);
        var parsed = MarcReader.FromString(marcString);

        Assert.Equal(RecordType.ComputerFile, parsed.RecordType);
        Assert.Equal(BibliographicLevel.Monograph, parsed.BibliographicLevel);
        Assert.Equal("complex-test", parsed.GetControlFieldData("001"));

        var parsedField = parsed.GetDataField("245");
        Assert.NotNull(parsedField);
        Assert.Equal("Complex Title /", parsedField.GetSubfieldData('a'));
        Assert.Equal("by Author.", parsedField.GetSubfieldData('c'));
    }

    [Fact]
    public void RoundTrip_ToStringFromString_PreservesRecord()
    {
        var original = new Record();
        original.AppendField(new ControlField("001", "roundtrip-1"));
        original.AppendField(new DataField("100", '1', ' '));
        original.GetDataField("100")?.AppendSubfield(new Subfield('a', "Smith, John"));

        string marcString = MarcWriter.WriteToString(original);
        var parsed = MarcReader.FromString(marcString);

        Assert.Equal(original.ToString(), parsed.ToString());
    }
}
