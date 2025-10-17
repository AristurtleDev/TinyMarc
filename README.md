# TinyMarc - Modern MARC-21 Library for .NET

A high-performance, modern C# library for reading, writing, and manipulating MARC-21 (MAchine-Readable Cataloging) bibliographic records.

## Features

- **MARC-21 Record Reading and Writing**: Read from files, streams, bytes, or strings
- **UTF-8 and MARC-8 Encoding Support**: Automatic encoding detection and conversion
- **Multiple Output Formats**: Export to MARC binary, JSON, or human-readable text formats
- **Async/Await Support**: Asynchronous operations for I/O intensive tasks
- **Field and Subfield Manipulation**: Complete API for adding, removing, and modifying MARC fields
- **Regex Field Matching**: Pattern-based field selection and filtering
- **Strongly-Typed Leader Properties**: Type-safe access to all MARC leader positions

## Usage

### Reading MARC Records

```csharp
using TinyMarc;

// Read from file
using MarcReader reader = new MarcReader("records.mrc");
await foreach (Record record in reader.ReadRecordsAsync())
{
    // Access fields by tag
    DataField titleField = record.GetDataField("245");
    string controlNumber = record.GetControlFieldData("001");

    Console.WriteLine($"Record: {controlNumber}");
    Console.WriteLine($"Leader: {record.CalculateLeader()}");
}

// Read from bytes or string
Record record = MarcReader.FromBytes(marcBytes);
Record recordFromString = MarcReader.FromString(marcString);
```

### Creating MARC Records

```csharp
Record record = new Record
{
    RecordType = RecordType.LanguageMaterial,
    BibliographicLevel = BibliographicLevel.Monograph,
    CharacterEncoding = CharacterEncoding.UTF8
};

// Add control field
record.AppendField(new ControlField("001", "12345"));

// Add data field with subfields
DataField titleField = new DataField("245", '1', '0');
titleField.AppendSubfield(new Subfield('a', "Title of the work"));
titleField.AppendSubfield(new Subfield('c', "Author name"));
record.AppendField(titleField);
```

### Writing MARC Records

```csharp
// Write to file
using MarcWriter writer = new MarcWriter("output.mrc");
await writer.WriteRecordAsync(record);

// Export to different formats
string marcBinary = record.ToMarc();      // Raw MARC format
string json = record.ToJson();            // JSON representation
string readable = record.ToString();      // Human-readable format
byte[] bytes = record.ToBytes();          // Byte array
```

### Working with Fields and Subfields

```csharp
// Get fields using tags (supports regex)
IEnumerable<Field> subjects = record.GetFields("6", useRegex: true);  // All 6xx fields
DataField author = record.GetDataField("100");
string isbn = record.GetDataField("020")?.GetSubfieldData('a');

// Manipulate fields
record.RemoveFields("590");  // Remove all 590 fields
record.InsertField(newField, existingField, before: true);

// Access subfields
foreach (Subfield subfield in author?.Subfields ?? new List<Subfield>())
{
    Console.WriteLine($"${subfield.Code}: {subfield.Data}");
}
```

### Pattern-Based Field Extraction

TinyMarc includes a powerful pattern extraction system for complex data retrieval:

```csharp
using TinyMarc.PatternExtraction;

// Basic field extraction
PatternExtractor extractor = new PatternExtractor("245a");
string[] titles = extractor.Extract(record);

// Multiple fields in one pattern
PatternExtractor multiExtractor = new PatternExtractor("100a:110a:111a");
string[] authors = multiExtractor.Extract(record);

// Control field extraction with slicing
PatternExtractor yearExtractor = new PatternExtractor("008[7-10]");
string[] years = yearExtractor.Extract(record);

// Data field with indicators and multiple subfields
PatternExtractor titleExtractor = new PatternExtractor("245|10|abc");
string[] titleData = titleExtractor.Extract(record);
```

### Advanced Pattern Extraction Options

```csharp
// Configure extraction behavior
ExtractorOptions options = new ExtractorOptions(
    First: true,                                    // Only first value
    TrimPunctuation: true,                         // Remove punctuation
    Default: "Unknown",                            // Default if no data found
    AllowDuplicates: false,                        // Remove duplicates
    Separator: " -- ",                             // Join subfields
    AlternateField: AlternateField.Include         // Include 880 fields
);

PatternExtractor extractor = new PatternExtractor("245abc", options);
string[] results = extractor.Extract(record);

// Working with alternate script fields (880 fields)
ExtractorOptions alternateOnly = new ExtractorOptions(
    AlternateField: AlternateField.Only
);
PatternExtractor altExtractor = new PatternExtractor("245a", alternateOnly);
string[] alternateScriptTitles = altExtractor.Extract(record);
```

#### Pattern Syntax

- **Control Fields**: `001`, `008[0-5]` (with optional character slicing)
- **Data Fields**: `245abc` (tag + subfield codes)
- **With Indicators**: `245|10|abc` (tag + |indicators| + subfield codes)
- **Multiple Patterns**: `100a:110a:245a` (colon-separated)
- **All Subfields**: `245` (omit subfield codes to get all)
- **Repeated Subfields**: `650aa` (repeat code to join multiple instances)

## Character Encoding Support

TinyMarc automatically detects and handles both UTF-8 and MARC-8 encodings:

```csharp
// Encoding is detected automatically from leader position 9
using MarcReader reader = new MarcReader("mixed_encoding_file.mrc");
Record record = reader.ReadRecord();

// Character encoding is preserved in the record
Console.WriteLine($"Encoding: {record.CharacterEncoding}");

// Output maintains the original encoding
byte[] bytes = record.ToBytes();  // Uses record's encoding
byte[] utf8Bytes = record.ToBytes(Encoding.UTF8);  // Force UTF-8
```

## Error Handling and Validation

```csharp
try
{
    Record record = MarcReader.FromString(malformedMarc);

    // Check for warnings (non-fatal issues)
    foreach (string warning in record.Warnings)
    {
        Console.WriteLine($"Warning: {warning}");
    }
}
catch (MarcInvalidRecordException ex)
{
    Console.WriteLine($"Invalid MARC record: {ex.Message}");
}
catch (MarcException ex)
{
    Console.WriteLine($"MARC processing error: {ex.Message}");
}
```

## License

This library is licensed under the MIT License. See the [LICENSE](LICENSE) file for full license information.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.
