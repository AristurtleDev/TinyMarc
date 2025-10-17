
namespace TinyMarc.Tests;

public sealed class DataFieldTests
{
    [Theory]
    [InlineData("010")]
    [InlineData("100")]
    [InlineData("999")]
    public void Constructor_WithValidParams_Succeeds(string tag)
    {
        DataField field = new DataField(tag, '1', '2');

        Assert.Equal(tag, field.Tag);
        Assert.Equal('1', field.Indicator1);
        Assert.Equal('2', field.Indicator2);
    }

    [Fact]
    public void Constructor_WithSubfields_InitializesCollection()
    {
        Subfield subfield1 = new Subfield('a', "first");
        Subfield subfield2 = new Subfield('b', "second");
        var subfields = new[] { subfield1, subfield2 };

        DataField field = new DataField("010", '1', ' ', subfields);

        Assert.Equal(2, field.Subfields.Count);
        Assert.Contains(subfield1, field.Subfields);
        Assert.Contains(subfield2, field.Subfields);
    }

    [Theory]
    [InlineData('a')]
    [InlineData('Z')]
    [InlineData('#')]
    [InlineData('!')]
    [InlineData('\t')]
    public void Constructor_WithInvalidIndicator_ThrowsException(char indicator)
    {
        Assert.Throws<MarcInvalidIndicatorException>(() => new DataField("245", indicator, indicator));
    }

    [Fact]
    public void Constructor_WithNullSubfields_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DataField("010", '1', ' ', null!));
    }

    [Theory]
    [InlineData("009")] // Control field
    [InlineData("00")] // Too short
    [InlineData("1000")] // Too large
    [InlineData("abc")] // Non-numeric
    [InlineData("")] // Empty
    public void Constructor_WithInvalidTag_ThrowsMarcInvalidTagException(string tag)
    {
        Assert.Throws<MarcInvalidTagException>(() =>
            new DataField(tag, '1', ' '));
    }

    [Fact]
    public void Tag_SetWithInvalidTag_ThrowsMarcInvalidTagException()
    {
        DataField field = new DataField("010", '1', ' ');

        Assert.Throws<MarcInvalidTagException>(() => field.Tag = "009");
    }

    [Fact]
    public void Indicator_SetWithInvalidIndicator_ThrowsMarcInvalidIndicatorException()
    {
        DataField field = new DataField("245", '1', '0');

        Assert.Throws<MarcInvalidIndicatorException>(() => field.Indicator1 = 'X');
        Assert.Throws<MarcInvalidIndicatorException>(() => field.Indicator2 = '#');
    }

    [Fact]
    public void IsEmpty_WhenNoSubfields_ReturnsTrue()
    {
        DataField field = new DataField("010", '1', ' ');

        Assert.True(field.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WhenSubfieldsExist_ReturnsFalse()
    {
        DataField field = new DataField("010", '1', ' ');
        field.AppendSubfield(new Subfield('a', "test"));

        Assert.False(field.IsEmpty);
    }

    [Fact]
    public void AppendSubfield_AddsToEnd()
    {
        DataField field = new DataField("010", '1', ' ');
        Subfield subfield = new Subfield('a', "test");

        field.AppendSubfield(subfield);

        Assert.Single(field.Subfields);
        Assert.Same(subfield, field.Subfields[0]);
    }

    [Fact]
    public void AppendSubfield_WithNull_ThrowsArgumentNullException()
    {
        DataField field = new DataField("010", '1', ' ');

        Assert.Throws<ArgumentNullException>(() => field.AppendSubfield(null!));
    }

    [Fact]
    public void PrependSubfield_AddsToStart()
    {
        DataField field = new DataField("010", '1', ' ');
        Subfield subfield1 = new Subfield('a', "first");
        Subfield subfield2 = new Subfield('b', "second");

        field.AppendSubfield(subfield1);
        field.PrependSubfield(subfield2);

        Assert.Same(subfield2, field.Subfields[0]);
        Assert.Same(subfield1, field.Subfields[1]);
    }

    [Fact]
    public void PrependSubfield_WithNull_ThrowsArgumentNullException()
    {
        DataField field = new DataField("010", '1', ' ');

        Assert.Throws<ArgumentNullException>(() => field.PrependSubfield(null!));
    }

    [Fact]
    public void InsertSubfield_BeforeExisting_InsertsAtCorrectPosition()
    {
        DataField field = new DataField("010", '1', ' ');
        Subfield subfield1 = new Subfield('a', "first");
        Subfield subfield2 = new Subfield('b', "second");
        Subfield newSubfield = new Subfield('c', "inserted");

        field.AppendSubfield(subfield1);
        field.AppendSubfield(subfield2);
        field.InsertSubfield(newSubfield, subfield2, before: true);

        Assert.Equal(3, field.Subfields.Count);
        Assert.Same(newSubfield, field.Subfields[1]);
    }

    [Fact]
    public void InsertSubfield_AfterExisting_InsertsAtCorrectPosition()
    {
        DataField field = new DataField("010", '1', ' ');
        Subfield subfield1 = new Subfield('a', "first");
        Subfield subfield2 = new Subfield('b', "second");
        Subfield newSubfield = new Subfield('c', "inserted");

        field.AppendSubfield(subfield1);
        field.AppendSubfield(subfield2);
        field.InsertSubfield(newSubfield, subfield1, before: false);

        Assert.Equal(3, field.Subfields.Count);
        Assert.Same(newSubfield, field.Subfields[1]);
    }

    [Fact]
    public void InsertSubfield_WithNonExistentSubfield_ThrowsArgumentException()
    {
        DataField field = new DataField("010", '1', ' ');
        Subfield subfield = new Subfield('a', "test");
        Subfield nonExistent = new Subfield('b', "not added");

        var ex = Assert.Throws<ArgumentException>(() =>
            field.InsertSubfield(subfield, nonExistent));

        Assert.Equal("existingSubField", ex.ParamName);
    }

    [Fact]
    public void RemoveSubfield_WithExisting_ReturnsTrue()
    {
        DataField field = new DataField("010", '1', ' ');
        Subfield subfield = new Subfield('a', "test");
        field.AppendSubfield(subfield);

        bool result = field.RemoveSubfield(subfield);

        Assert.True(result);
        Assert.Empty(field.Subfields);
    }

    [Fact]
    public void RemoveSubfield_WithNonExistent_ReturnsFalse()
    {
        DataField field = new DataField("010", '1', ' ');
        field.AppendSubfield(new Subfield('a', "test"));

        bool result = field.RemoveSubfield(new Subfield('b', "other"));

        Assert.False(result);
    }

    [Fact]
    public void RemoveSubfields_RemovesAllMatchingCode()
    {
        DataField field = new DataField("010", '1', ' ');
        field.AppendSubfield(new Subfield('a', "first"));
        field.AppendSubfield(new Subfield('a', "second"));
        field.AppendSubfield(new Subfield('b', "other"));

        int removed = field.RemoveSubfields('a');

        Assert.Equal(2, removed);
        Assert.Single(field.Subfields);
        Assert.Equal('b', field.Subfields[0].Code);
    }

    [Fact]
    public void GetSubfield_WithMatch_ReturnsFirst()
    {
        DataField field = new DataField("010", '1', ' ');
        Subfield subfield1 = new Subfield('a', "first");
        Subfield subfield2 = new Subfield('a', "second");

        field.AppendSubfield(subfield1);
        field.AppendSubfield(subfield2);

        var result = field.GetSubfield('a');

        Assert.Same(subfield1, result);
    }

    [Fact]
    public void GetSubfield_WithNoMatch_ReturnsNull()
    {
        DataField field = new DataField("010", '1', ' ');
        field.AppendSubfield(new Subfield('a', "test"));

        var result = field.GetSubfield('z');

        Assert.Null(result);
    }

    [Fact]
    public void GetSubfields_ReturnsAllMatching()
    {
        DataField field = new DataField("010", '1', ' ');
        field.AppendSubfield(new Subfield('a', "first"));
        field.AppendSubfield(new Subfield('b', "other"));
        field.AppendSubfield(new Subfield('a', "second"));

        List<Subfield> results = field.GetSubfields('a').ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("first", results[0].Data);
        Assert.Equal("second", results[1].Data);
    }

    [Fact]
    public void GetSubfields_WithNoMatches_ReturnsEmpty()
    {
        DataField field = new DataField("010", '1', ' ');
        field.AppendSubfield(new Subfield('a', "test"));

        var results = field.GetSubfields('z');

        Assert.Empty(results);
    }

    [Fact]
    public void ToMarc_FormatsWithIndicatorsAndSubfields()
    {
        DataField field = new DataField("010", '1', ' ');
        field.AppendSubfield(new Subfield('a', "test"));

        string result = field.ToMarc();

        Assert.Equal("1 \u001Fatest\u001E", result);
    }

    [Fact]
    public void ToMarc_SkipsEmptySubfields()
    {
        DataField field = new DataField("010", '1', ' ');
        field.AppendSubfield(new Subfield('a', "data"));
        field.AppendSubfield(new Subfield('b', ""));
        field.AppendSubfield(new Subfield('c', "more"));

        string result = field.ToMarc();

        Assert.Contains("\u001Fadata", result);
        Assert.Contains("\u001Fcmore", result);
        Assert.DoesNotContain("\u001Fb", result);
    }

    [Fact]
    public void ToString_ReturnsFormattedField()
    {
        DataField field = new DataField("010", '1', ' ');
        field.AppendSubfield(new Subfield('a', "test"));

        string result = field.ToString();

        Assert.Equal("010 1  a| test", result);
    }

    [Fact]
    public void IsControlField_ReturnsFalse()
    {
        DataField field = new DataField("010", '1', ' ');

        Assert.False(field.IsControlField);
    }

    [Fact]
    public void IsDataField_ReturnsTrue()
    {
        DataField field = new DataField("010", '1', ' ');

        Assert.True(field.IsDataField);
    }
}
