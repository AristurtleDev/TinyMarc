// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using TinyMarc.PatternExtraction;

namespace TinyMarc.Tests;

public class PatternExtractorTests
{
    [Theory]
    [InlineData("001")]
    [InlineData("001[1]")]
    [InlineData("001[1-2]")]
    [InlineData("010")]
    [InlineData("010|12|")]
    [InlineData("010|*2|")]
    [InlineData("010|1*|")]
    [InlineData("010a")]
    [InlineData("010|12|a")]
    public void Constructor_OnValidSinglePatternGiven_NoExceptionThrown(string pattern)
    {
        Exception? ex = Xunit.Record.Exception(() => new PatternExtractor(pattern));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("1")]           //  Tag must be 3 characters
    [InlineData("01")]          //  Tag must be 3 characters
    [InlineData("00A")]         //  Tag must be numerical
    [InlineData("001[1")]       //  Range must be enclosed in []
    [InlineData("0011]")]       //  Range must be enclosed in []
    [InlineData("001[A]")]      //  Range must be numerical
    [InlineData("001[A-1]")]    //  Both range values must be numerical
    [InlineData("001[1-A]")]    //  Both range values must be numerical
    [InlineData("010|12")]      //  Indicators must be enclosed in ||
    [InlineData("01012|")]      //  Indicators must be enclosed in ||
    [InlineData("010|1|")]      //  Two indicators must be given if any at all
    [InlineData("010a|12|")]    //  If indicators given, they must come after tag and before subfields
    [InlineData("010.")]        //  Subfields must be alphanumerics
    public void Constructor_OnInvalidSinglePatternGiven_ExceptionThrown(string pattern)
    {
        Exception? ex = Xunit.Record.Exception(() => new PatternExtractor(pattern));
        Assert.NotNull(ex);
    }

    [Theory]
    [InlineData("001:010")]
    [InlineData("001[1]:010|12|")]
    [InlineData("001[1-2]:010|*2|")]
    [InlineData("001:010|1*|")]
    [InlineData("001[1]:010a")]
    [InlineData("001[1-2]:010|12|a")]
    public void Constructor_OnValidMultiPatternGiven_NoExceptionThrown(string pattern)
    {
        Exception? ex = Xunit.Record.Exception(() => new PatternExtractor(pattern));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("001-010-200")]             //  Patterns must be separated by ':' colon
    [InlineData("001:010|1|")]              //  Second pattern is invalid data field pattern
    [InlineData("010|12|abcdefg:01[1-2]")]  //  Second pattern is invalid control field pattern
    [InlineData("01:10")]                   //  Both patterns invalid tag length
    public void Constructor_OnInvalidMultiPatternGiven_ExceptionThrown(string pattern)
    {
        Exception? ex = Xunit.Record.Exception(() => new PatternExtractor(pattern));
        Assert.NotNull(ex);
    }

    [Fact]
    public void Extract_FirstOnly_ReturnExpectedValue()
    {
        //  Create the record
        Record record = new();

        //  Add the fields, three of the same tag, but different data.
        record.AppendField(new ControlField("001", "TestData1"));
        record.AppendField(new ControlField("001", "TestData2"));
        record.AppendField(new ControlField("001", "TestData3"));

        //  Create the extractor
        PatternExtractor extractor = new("001", new(First: true));

        //  Extract
        string[] extracted = extractor.Extract(record);

        int expectedLen = 1;
        int actualLen = extracted.Length;
        Assert.Equal(expectedLen, actualLen);

        string expectedValue = "TestData1";
        string actualValue = extracted[0];
        Assert.Equal(expectedValue, actualValue);

    }

    [Fact]
    public void Extract_TrimPunctuationTrue_ReturnExpectedValues()
    {
        string expected1 = "This has a leading whitespace";
        string expected2 = "This has trailing punctuation";

        //  Create a new record
        Record record = new();

        //  Add two control fields, one leading whitespace, the other with
        //  trailing punctuation
        record.AppendField(new ControlField("001", $" {expected1}"));
        record.AppendField(new ControlField("002", $"{expected2}.!?"));

        //   Create the extractor
        PatternExtractor extractor = new("001:002", new(TrimPunctuation: true));

        string[] extracted = extractor.Extract(record);

        Assert.Equal(expected1, extracted[0]);
        Assert.Equal(expected2, extracted[1]);
    }

    [Fact]
    public void Extract_TrimPunctuationFalse_ReturnExpectedValues()
    {
        string expected1 = " This has a leading whitespace";
        string expected2 = "This has trailing punctuation.!?";

        //  Create a new record
        Record record = new();

        //  Add two control fields, one leading whitespace, the other with
        //  trailing punctuation
        record.AppendField(new ControlField("001", expected1));
        record.AppendField(new ControlField("002", expected2));

        //   Create the extractor
        PatternExtractor extractor = new("001:002", new(TrimPunctuation: false));

        string[] extracted = extractor.Extract(record);

        Assert.Equal(expected1, extracted[0]);
        Assert.Equal(expected2, extracted[1]);
    }

    [Fact]
    public void Extract_DefaultValueIsNull_ReturnExpectedValues()
    {
        //  Create the record
        Record record = new();

        //  Add a field
        record.AppendField(new ControlField("001", "TestData1"));

        //  Create the extract set the pattern to a field that isn't there
        PatternExtractor extractor = new("002", new(Default: null));

        //  Extract
        string[] extracted = extractor.Extract(record);

        int expectedLen = 0;
        int actualLen = extracted.Length;

        Assert.Equal(expectedLen, actualLen);
    }

    [Fact]
    public void Extract_DefaultValueGiven_ReturnExpectedValues()
    {
        string expected = "Default Value";

        //  Create the record
        Record record = new();

        //  Add a field
        record.AppendField(new ControlField("001", "TestData1"));

        //  Create the extract set the pattern to a field that isn't there
        PatternExtractor extractor = new("002", new(Default: expected));

        //  Extract
        string[] extracted = extractor.Extract(record);

        Assert.Equal(expected, extracted[0]);
    }

    [Fact]
    public void Extract_AllowDuplicates_ReturnExpectedValues()
    {
        //  Create the record
        Record record = new();

        //  Add three fields all with the same data
        record.AppendField(new ControlField("001", "TestData1"));
        record.AppendField(new ControlField("001", "TestData1"));
        record.AppendField(new ControlField("001", "TestData1"));

        //  Create the extractor
        PatternExtractor extractor = new("001", new(AllowDuplicates: true));

        //  Extract
        string[] extracted = extractor.Extract(record);

        int expectedLen = 3;
        int actualLen = extracted.Length;
        Assert.Equal(expectedLen, actualLen);

        //  Ensure it's the same duplicate 3 times
        for (int i = 0; i < extracted.Length; i++)
        {
            Assert.Equal("TestData1", extracted[i]);
        }
    }

    [Fact]
    public void Extract_DoNotAllowDuplicates_ReturnExpectedValues()
    {
        //  Create the record
        Record record = new();

        //  Add three fields all with the same data
        record.AppendField(new ControlField("001", "TestData1"));
        record.AppendField(new ControlField("001", "TestData1"));
        record.AppendField(new ControlField("001", "TestData1"));

        //  Create the extractor
        PatternExtractor extractor = new("001", new(AllowDuplicates: false));

        //  Extract
        string[] extracted = extractor.Extract(record);

        int expectedLen = 1;
        int actualLen = extracted.Length;
        Assert.Equal(expectedLen, actualLen);

        //  Ensure it's the correct data
        Assert.Equal("TestData1", extracted[0]);
    }

    [Fact]
    public void Extract_WithSeparator_ReturnExpectedValues()
    {
        string separator = "(o^_^o)";

        //  Create the record
        Record record = new();

        //  Add a Data Field
        List<Subfield> subfields = new();
        subfields.Add(new('a', "TestData1"));
        subfields.Add(new('b', "TestData2"));
        subfields.Add(new('c', "TestData3"));
        record.AppendField(new DataField("010", '1', ' ', subfields));

        //  Create the extractor
        string pattern = $"010{'a'}{'b'}{'c'}";
        PatternExtractor extractor = new(pattern, new(Separator: separator));

        //  Extract
        string[] extracted = extractor.Extract(record);

        int expectedLen = 1;
        int actualLen = extracted.Length;
        Assert.Equal(expectedLen, actualLen);

        string expectedData = string.Format("{0}{3}{1}{3}{2}", "TestData1", "TestData2", "TestData3", separator);
        string actualData = extracted[0];

        //  Ensure it's the correct data
        Assert.Equal(expectedData, actualData);
    }

    [Fact]
    public void Extract_WithoutSeparator_ReturnExpectedValues()
    {
        //  Create the record
        Record record = new();

        //  Add a Data Field
        List<Subfield> subfields = new();
        subfields.Add(new('a', "TestData1"));
        subfields.Add(new('b', "TestData2"));
        subfields.Add(new('c', "TestData3"));
        record.AppendField(new DataField("010", '1', ' ', subfields));

        //  Create the extractor
        string pattern = $"010{'a'}{'b'}{'c'}";
        PatternExtractor extractor = new(pattern, new(Separator: null));

        //  Extract
        string[] extracted = extractor.Extract(record);

        int expectedLen = 3;
        int actualLen = extracted.Length;
        Assert.Equal(expectedLen, actualLen);

        string[] expectedData = new string[] { "TestData1", "TestData2", "TestData3" };
        Assert.Equal(expectedData, extracted);
    }

    [Fact]
    public void Extract_AlternateFieldInclude_ReturnExpectedValues()
    {
        //  Create the record
        Record record = new();

        //  Add a Data Field
        List<Subfield> subfields = new();
        subfields.Add(new('6', "880-02"));
        subfields.Add(new('a', "TestData1"));
        record.AppendField(new DataField("010", '1', ' ', subfields));

        //  Add an alternate subfield with different data.
        List<Subfield> alternateSubfields1 = new();
        alternateSubfields1.Add(new('6', "010-01"));
        alternateSubfields1.Add(new('a', "TestData2"));
        record.AppendField(new DataField("880", '1', '1', alternateSubfields1));

        //  Add a second alternate subfield with different data
        List<Subfield> alternateSubfields2 = new();
        alternateSubfields2.Add(new('6', "010-02"));
        alternateSubfields2.Add(new('a', "TestData3"));
        record.AppendField(new DataField("880", '1', '1', alternateSubfields2));

        //  Create the extractor
        string pattern = $"010{'a'}";
        PatternExtractor extractor = new(pattern, new(AlternateField: AlternateField.Include));

        //  Extract
        string[] extracted = extractor.Extract(record);

        int expectedLen = 3;
        int actualLen = extracted.Length;
        Assert.Equal(expectedLen, actualLen);

        string[] expectedData = new string[] { "TestData1", "TestData2", "TestData3" };
        Assert.Equal(expectedData, extracted);
    }

    [Fact]
    public void Extract_AlternateFieldOnly_ReturnExpectedValues()
    {
        //  Create the record
        Record record = new();

        //  Add a Data Field
        List<Subfield> subfields = new();
        subfields.Add(new('6', "880-02"));
        subfields.Add(new('a', "TestData1"));
        record.AppendField(new DataField("010", '1', ' ', subfields));

        //  Add an alternate subfield with different data.
        List<Subfield> alternateSubfields1 = new();
        alternateSubfields1.Add(new('6', "010-01"));
        alternateSubfields1.Add(new('a', "TestData2"));
        record.AppendField(new DataField("880", '1', '1', alternateSubfields1));

        //  Add a second alternate subfield with different data
        List<Subfield> alternateSubfields2 = new();
        alternateSubfields2.Add(new('6', "010-02"));
        alternateSubfields2.Add(new('a', "TestData3"));
        record.AppendField(new DataField("880", '1', '1', alternateSubfields2));

        //  Create the extractor
        string pattern = $"010{'a'}";
        PatternExtractor extractor = new(pattern, new(AlternateField: AlternateField.Only));

        //  Extract
        string[] extracted = extractor.Extract(record);

        int expectedLen = 2;
        int actualLen = extracted.Length;
        Assert.Equal(expectedLen, actualLen);

        string[] expectedData = new string[] { "TestData2", "TestData3" };
        Assert.Equal(expectedData, extracted);
    }

    [Fact]
    public void Extract_AlternateFieldDontInclude_ReturnExpectedValues()
    {
        //  Create the record
        Record record = new();

        //  Add a Data Field
        List<Subfield> subfields = new();
        subfields.Add(new('6', "880-02"));
        subfields.Add(new('a', "TestData1"));
        record.AppendField(new DataField("010", '1', ' ', subfields));

        //  Add an alternate subfield with different data.
        List<Subfield> alternateSubfields1 = new();
        alternateSubfields1.Add(new('6', "010-01"));
        alternateSubfields1.Add(new('a', "TestData2"));
        record.AppendField(new DataField("880", '1', '1', alternateSubfields1));

        //  Add a second alternate subfield with different data
        List<Subfield> alternateSubfields2 = new();
        alternateSubfields2.Add(new('6', "010-02"));
        alternateSubfields2.Add(new('a', "TestData3"));
        record.AppendField(new DataField("880", '1', '1', alternateSubfields2));

        //  Create the extractor
        string pattern = $"010{'a'}";
        PatternExtractor extractor = new(pattern, new(AlternateField: AlternateField.DontInclude));

        //  Extract
        string[] extracted = extractor.Extract(record);

        int expectedLen = 1;
        int actualLen = extracted.Length;
        Assert.Equal(expectedLen, actualLen);

        string[] expectedData = new string[] { "TestData1" };
        Assert.Equal(expectedData, extracted);
    }

}
