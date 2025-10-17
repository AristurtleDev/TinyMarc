namespace TinyMarc.Tests;

public sealed class SubfieldTests
{
    [Fact]
    public void Constructor_WithValidCodeAndData_SetsProperties()
    {
        char expectedCode = 'a';
        string expectedData = "test data";

        var subfield = new Subfield(expectedCode, expectedData);

        Assert.Equal(expectedCode, subfield.Code);
        Assert.Equal(expectedData, subfield.Data);
    }

    [Fact]
    public void Constructor_WithNullData_InitializesAsEmptyString()
    {
        char code = 'a';

        var subfield = new Subfield(code, null!);

        Assert.Equal(string.Empty, subfield.Data);
    }

    [Theory]
    [InlineData('a')]
    [InlineData('z')]
    [InlineData('0')]
    [InlineData('9')]
    [InlineData('A')]
    [InlineData('Z')]
    public void Constructor_WithVariousValidCodes_SetsCode(char code)
    {
        var subfield = new Subfield(code, "test");

        Assert.Equal(code, subfield.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void IsEmpty_WhenDataIsNullOrEmpty_ReturnsTrue(string? data)
    {
        var subfield = new Subfield('a', data!);

        Assert.True(subfield.IsEmpty);
    }

    [Theory]
    [InlineData("test")]
    [InlineData(" ")]
    [InlineData("a")]
    public void IsEmpty_WhenDataExists_ReturnsFalse(string data)
    {
        var subfield = new Subfield('a', data);

        Assert.False(subfield.IsEmpty);
    }

    [Fact]
    public void IsEmpty_AfterSettingDataToEmpty_ReturnsTrue()
    {
        var subfield = new Subfield('a', "initial data");

        subfield.Data = string.Empty;

        Assert.True(subfield.IsEmpty);
    }

    [Fact]
    public void IsEmpty_AfterSettingDataToNull_ReturnsTrue()
    {
        var subfield = new Subfield('a', "initial data");

        subfield.Data = null!;

        Assert.True(subfield.IsEmpty);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        char code = 'a';
        string data = "test data";
        var subfield = new Subfield(code, data);

        string result = subfield.ToString();

        Assert.Equal("a| test data", result);
    }

    [Fact]
    public void ToString_WithEmptyData_ReturnsFormattedStringWithEmptyData()
    {
        var subfield = new Subfield('b', "");

        string result = subfield.ToString();

        Assert.Equal("b| ", result);
    }

    [Fact]
    public void ToString_WithSpecialCharacters_PreservesCharacters()
    {
        var subfield = new Subfield('a', "test & data <>");

        string result = subfield.ToString();

        Assert.Equal("a| test & data <>", result);
    }

    [Fact]
    public void ToMarc_ReturnsFormattedStringWithSubfieldIndicator()
    {
        char code = 'a';
        string data = "test data";
        var subfield = new Subfield(code, data);

        string result = subfield.ToMarc();

        Assert.Equal("\u001Fatest data", result);
    }

    [Fact]
    public void ToMarc_WithEmptyData_ReturnsIndicatorAndCodeOnly()
    {
        var subfield = new Subfield('b', "");

        string result = subfield.ToMarc();

        Assert.Equal("\u001Fb", result);
    }

    [Fact]
    public void ToMarc_WithMultipleSubfields_EachHasOwnIndicator()
    {
        var subfield1 = new Subfield('a', "first");
        var subfield2 = new Subfield('b', "second");

        string marc1 = subfield1.ToMarc();
        string marc2 = subfield2.ToMarc();

        Assert.Equal("\u001Fafirst", marc1);
        Assert.Equal("\u001Fbsecond", marc2);
    }

    [Theory]
    [InlineData("simple")]
    [InlineData("with spaces")]
    [InlineData("with\ttabs")]
    [InlineData("with\nnewlines")]
    [InlineData("unicode: ñ é ü")]
    public void ToMarc_WithVariousDataTypes_PreservesData(string data)
    {
        var subfield = new Subfield('a', data);

        string result = subfield.ToMarc();

        Assert.Contains(data, result);
        Assert.StartsWith("\u001Fa", result);
    }
}
