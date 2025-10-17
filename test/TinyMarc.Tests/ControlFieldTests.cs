using System.Text;

namespace TinyMarc.Tests;

public sealed class ControlFieldTests
{
    [Theory]
    [InlineData("001")]
    [InlineData("005")]
    [InlineData("009")]
    public void Constructor_WithValidTag_Succeeds(string tag)
    {
        ControlField field = new ControlField(tag, "test");

        Assert.Equal(tag, field.Tag);
    }

    [Theory]
    [InlineData("010")] // Data field
    [InlineData("000")] // Too low
    [InlineData("99")]  // Too short
    [InlineData("")]    // Empty
    [InlineData("abc")] // Non-numeric
    public void Constructor_WithInvalidTag_ThrowsMarcInvalidTagException(string tag)
    {
        Assert.Throws<MarcInvalidTagException>(() => new ControlField(tag, "test"));
    }

    [Fact]
    public void Constructor_WithNullData_InitializesAsEmptyString()
    {
        ControlField field = new ControlField("001", null!);

        Assert.Equal(string.Empty, field.Data);
    }

    [Fact]
    public void Tag_SetWithInvalidTag_ThrowsMarcInvalidTagException()
    {
        ControlField field = new ControlField("001", "test");

        Assert.Throws<MarcInvalidTagException>(() => field.Tag = "010");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void IsEmpty_WhenDataIsNullOrEmpty_ReturnsTrue(string? data)
    {
        ControlField field = new ControlField("001", data!);

        Assert.True(field.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WhenDataExists_ReturnsFalse()
    {
        ControlField field = new ControlField("001", "test");

        Assert.False(field.IsEmpty);
    }

    [Fact]
    public void ToMarc_FormatsWithEndOfFieldMarker()
    {
        ControlField field = new ControlField("001", "test");

        string marc = field.ToMarc();

        Assert.Equal("test\u001E", marc);
    }

    [Fact]
    public void ToString_ReturnsFormattedTagAndData()
    {
        ControlField field = new ControlField("001", "test");

        Assert.Equal("001    test", field.ToString());
    }

    [Fact]
    public void IsControlField_ReturnsTrue()
    {
        ControlField field = new ControlField("001", "test");

        Assert.True(field.IsControlField);
    }

    [Fact]
    public void IsDataField_ReturnsFalse()
    {
        ControlField field = new ControlField("001", "test");

        Assert.False(field.IsDataField);
    }
}
