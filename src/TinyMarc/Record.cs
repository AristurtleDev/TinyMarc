// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TinyMarc;

/// <summary>
/// Represents a MARC-21 bibliographic record containing a leader and fields.
/// </summary>
public sealed partial class Record
{
    private readonly List<Field> _fields = [];
    private readonly List<string> _warnings = [];

    /// <summary>
    /// Gets or sets the record status (position 5).
    /// </summary>
    public RecordStatus RecordStatus { get; set; } = RecordStatus.New;

    /// <summary>
    /// Gets or sets the type of record (position 6).
    /// </summary>
    public RecordType RecordType { get; set; } = RecordType.LanguageMaterial;

    /// <summary>
    /// Gets or sets the bibliographic level (position 7).
    /// </summary>
    public BibliographicLevel BibliographicLevel { get; set; } = BibliographicLevel.Monograph;

    /// <summary>
    /// Gets or sets the type of control (position 8).
    /// </summary>
    public TypeOfControl TypeOfControl { get; set; } = TypeOfControl.NoSpecifiedType;

    /// <summary>
    /// Gets or sets the character encoding scheme (position 9).
    /// </summary>
    public CharacterEncoding CharacterEncoding { get; set; } = CharacterEncoding.UTF8;

    /// <summary>
    /// Gets or sets the encoding level (position 17).
    /// </summary>
    public EncodingLevel EncodingLevel { get; set; } = EncodingLevel.FullLevel;

    /// <summary>
    /// Gets or sets the descriptive cataloging form (position 18).
    /// </summary>
    public DescriptiveCatalogingForm CatalogingForm { get; set; } = DescriptiveCatalogingForm.NonISBD;

    /// <summary>
    /// Gets or sets the multipart resource record level (position 19).
    /// </summary>
    public MultipartResourceLevel MultipartLevel { get; set; } = MultipartResourceLevel.NotSpecified;

    /// <summary>
    /// Gets the field collection.
    /// </summary>
    public ReadOnlyCollection<Field> Fields { get; }

    /// <summary>
    /// Gets the warnings generated during record processing.
    /// </summary>
    public ReadOnlyCollection<string> Warnings { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Record"/> class.
    /// </summary>
    public Record()
    {
        Fields = _fields.AsReadOnly();
        Warnings = _warnings.AsReadOnly();
    }

    /// <summary>
    /// Calculates the complete 24-character leader based on semantic properties
    /// and current record structure.
    /// </summary>
    /// <returns>Complete MARC-21 leader string.</returns>
    public string CalculateLeader()
    {
        (string _, string _, int recordLength, int baseAddress) = BuildDirectory();
        return CalculateLeader(recordLength, baseAddress);
    }

    /// <summary>
    /// Calculates the complete 24-character leader based on semantic properties
    /// and provided record metrics.
    /// </summary>
    /// <param name="recordLength">Total record length in bytes.</param>
    /// <param name="baseAddress">Base address of data field area.</param>
    /// <returns>Complete MARC-21 leader string.</returns>
    private string CalculateLeader(int recordLength, int baseAddress)
    {
        if (recordLength > MarcConstants.MaxRecordLength)
        {
            AddWarning($"Record length {recordLength} exceeds maximum allowed length {MarcConstants.MaxRecordLength}");
            recordLength = MarcConstants.MaxRecordLength;
        }

        char[] leader = new char[MarcConstants.LeaderLength];

        // Positions 0-4: Record length (calculated)
        recordLength.ToString("00000").CopyTo(0, leader, 0, 5);

        // Positions 5-9: Semantic values (user-controlled)
        leader[5] = (char)RecordStatus;
        leader[6] = (char)RecordType;
        leader[7] = (char)BibliographicLevel;
        leader[8] = (char)TypeOfControl;
        leader[9] = (char)CharacterEncoding;

        // Positions 10-11: Indicator count, subfield code count (constant)
        "22".CopyTo(0, leader, 10, 2);

        // Positions 12-16: Base address of data (calculated)
        baseAddress.ToString("00000").CopyTo(0, leader, 12, 5);

        // Positions 17-19: Semantic values (user-controlled)
        leader[17] = (char)EncodingLevel;
        leader[18] = (char)CatalogingForm;
        leader[19] = (char)MultipartLevel;

        // Positions 20-23: Entry map (constant)
        "4500".CopyTo(0, leader, 20, 4);

        return new string(leader);
    }

    /// <summary>
    /// Sets semantic leader properties from a complete 24-character leader string.
    /// </summary>
    /// <param name="leader">24-character leader string.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="leader"/> is empty.</exception>
    /// <exception cref="MarcInvalidLeaderLengthException">
    /// Thrown when <paramref name="leader"/> is not exactly 24 characters.
    /// </exception>
    public void SetLeaderFromString(ReadOnlySpan<char> leader)
    {
        if (leader.IsEmpty)
        {
            throw new ArgumentException($"{nameof(leader)} cannot be empty");
        }

        if (leader.Length != MarcConstants.LeaderLength)
        {
            throw new MarcInvalidLeaderLengthException($"Invalid leader length: Leader must be {MarcConstants.LeaderLength} characters; actual length {leader.Length}");
        }

        // Extract semantic values
        RecordStatus = (RecordStatus)leader[5];
        RecordType = (RecordType)leader[6];
        BibliographicLevel = (BibliographicLevel)leader[7];
        TypeOfControl = (TypeOfControl)leader[8];
        CharacterEncoding = (CharacterEncoding)leader[9];
        EncodingLevel = (EncodingLevel)leader[17];
        CatalogingForm = (DescriptiveCatalogingForm)leader[18];
        MultipartLevel = (MultipartResourceLevel)leader[19];
    }

    /// <summary>
    /// Adds a warning message to the record.
    /// </summary>
    /// <param name="warning">Warning message.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="warning"/> is <see langword="null"/> or empty.</exception>
    public void AddWarning(string warning)
    {
        ArgumentException.ThrowIfNullOrEmpty(warning);
        _warnings.Add(warning);
    }

    /// <summary>
    /// Appends a field to the end of the fields list.
    /// </summary>
    /// <param name="field">Field to append.</param>
    /// <exception cref="ArgumentNullException">Throw when <see langword="field"/> is <see langword="null"/>.</exception>
    public void AppendField(Field field)
    {
        ArgumentNullException.ThrowIfNull(field);
        _fields.Add(field);
    }

    /// <summary>
    /// Prepends a field to the start of the fields list.
    /// </summary>
    /// <param name="field">Field to prepend.</param>
    /// <exception cref="ArgumentNullException">Thrown when <see langword="field"/> is <see langword="null"/>.</exception>
    public void PrependField(Field field)
    {
        ArgumentNullException.ThrowIfNull(field);
        _fields.Insert(0, field);
    }

    /// <summary>
    /// Inserts a field before or after an existing field.
    /// </summary>
    /// <param name="newField">Field to insert.</param>
    /// <param name="existingField">Reference field.</param>
    /// <param name="before"><see langword="true"/> to insert before; <see langword="false"/> to insert after.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="newField"/> or <paramref name="existingField"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="existingField"/> is not found.</exception>
    public void InsertField(Field newField, Field existingField, bool before = false)
    {
        ArgumentNullException.ThrowIfNull(newField);
        ArgumentNullException.ThrowIfNull(existingField);

        int index = _fields.IndexOf(existingField);
        if (index == -1)
        {
            throw new ArgumentException("Existing field not found in this record", nameof(existingField));
        }

        if (!before)
        {
            index++;
        }

        _fields.Insert(index, newField);
    }

    /// <summary>
    /// Removes a field from the record.
    /// </summary>
    /// <param name="field">Field to remove.</param>
    /// <returns><see langword="true"/> if the field was removed; otherwise, <see langword="false"/>.</returns>
    public bool RemoveField(Field field) => _fields.Remove(field);

    /// <summary>
    /// Removes all fields with the specified tag.
    /// </summary>
    /// <param name="tag">Field tag.</param>
    /// <param name="useRegex"><see langword="true"/> to treat as regular expression.</param>
    /// <returns>Number of fields removed.</returns>
    public int RemoveFields(string tag, bool useRegex = false)
    {
        if (useRegex)
        {
            Regex regex = GetTagRegex(tag);
            return _fields.RemoveAll(f => regex.IsMatch(f.Tag));
        }

        return _fields.RemoveAll(f => f.Tag == tag);
    }

    /// <summary>
    /// Gets the first field with the specified tag.
    /// </summary>
    /// <param name="tag">Field tag.</param>
    /// <param name="useRegex"><see langword="true"/> to treater as a regular expression.</param>
    /// <returns>First matching field, or <see langword="null"/> if not found.</returns>
    public Field? GetField(string tag, bool useRegex = false)
    {
        if (useRegex)
        {
            Regex regex = GetTagRegex(tag);
            return _fields.FirstOrDefault(f => regex.IsMatch(f.Tag));
        }

        return _fields.FirstOrDefault(f => f.Tag == tag);
    }

    /// <summary>
    /// Gets all fields with the specified tag.
    /// </summary>
    /// <param name="tag">Field tag (<see langword="null"/> returns all fields).</param>
    /// <param name="useRegex"><see langword="true"/> to treat as a regular expression.</param>
    /// <returns>Collection of matching fields.</returns>
    public IEnumerable<Field> GetFields(string? tag = null, bool useRegex = false)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return _fields;
        }

        if (useRegex)
        {
            Regex regex = GetTagRegex(tag);
            return _fields.Where(f => regex.IsMatch(f.Tag));
        }

        return _fields.Where(f => f.Tag == tag);
    }

    /// <summary>
    /// Gets the first control field with the specified tag.
    /// </summary>
    /// <param name="tag">Control field tag (001 - 009).</param>
    /// <param name="useRegex"><see langword="true"/> to treat as a regular expression.</param>
    /// <returns>First matching control field, or <see langword="null"/> if not found.</returns>
    public ControlField? GetControlField(string tag, bool useRegex = false)
    {
        Field? field = GetField(tag, useRegex);
        return field as ControlField;
    }

    /// <summary>
    /// Gets the data from the first control field with the specified tag.
    /// </summary>
    /// <param name="tag">Control field tag (001 - 009).</param>
    /// <returns>Control field data, or <see langword="null"/> if not found.</returns>
    public string? GetControlFieldData(string tag)
    {
        Field? field = GetField(tag);
        return field is ControlField controlField ? controlField.Data : null;
    }

    /// <summary>
    /// Gets the first data field with the specified tag.
    /// </summary>
    /// <param name="tag">Data field tag (010 - 999).</param>
    /// <param name="useRegex"><see langword="true"/> to treat as a regular expression.</param>
    /// <returns>First matching data field, or <see langword="null"/> if not found.</returns>
    public DataField? GetDataField(string tag, bool useRegex = false)
    {
        Field? field = GetField(tag, useRegex);
        return field as DataField;
    }

    /// <summary>
    /// Returns a MARC formatted string representation of the record.
    /// </summary>
    public string ToMarc()
    {
        (string? fieldData, string? directory, int recordLength, int baseAddress) = BuildDirectory();
        string leader = CalculateLeader(recordLength, baseAddress);

        StringBuilder sb = new(recordLength);
        sb.Append(leader);
        sb.Append(directory);
        sb.Append(MarcConstants.EndOfField);
        sb.Append(fieldData);
        sb.Append(MarcConstants.EndOfRecord);

        return sb.ToString();
    }

    /// <summary>
    /// Returns a formatted, human-readable string representation of the record.
    /// </summary>
    /// <returns>Formatted MARC record.</returns>
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append("LDR     ").AppendLine(CalculateLeader());

        foreach (Field field in _fields)
        {
            if (!field.IsEmpty)
            {
                sb.AppendLine(field.ToString());
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts the record to JSON format.
    /// </summary>
    /// <returns>JSON representation of the MARC record.</returns>
    public string ToJson()
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("leader", CalculateLeader());
            writer.WriteStartArray("fields");

            foreach (Field field in _fields)
            {
                if (field.IsEmpty)
                {
                    continue;
                }

                writer.WriteStartObject();

                if (field is ControlField controlField)
                {
                    writer.WriteString(controlField.Tag, controlField.Data);
                }
                else if (field is DataField dataField)
                {
                    writer.WriteStartObject(dataField.Tag);
                    writer.WriteString("ind1", dataField.Indicator1.ToString());
                    writer.WriteString("ind2", dataField.Indicator2.ToString());
                    writer.WriteStartArray("subfields");

                    foreach (Subfield subfield in dataField.Subfields)
                    {
                        writer.WriteStartObject();
                        writer.WriteString(subfield.Code.ToString(), subfield.Data);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Converts the record to byte encoded format.
    /// </summary>
    /// <returns>A byte array representation of the MARC record.</returns>
    public byte[] ToBytes()
    {
        Encoding encoding = CharacterEncoding == CharacterEncoding.MARC8
            ? new Marc8Encoding()
            : Encoding.UTF8;
        return ToBytes(encoding);
    }

    /// <summary>
    /// Converts the record to byte encoded format using the specified encoding.
    /// </summary>
    /// <param name="encoding">The encoding to use</param>
    /// <returns>A byte array representation of the MARC record.</returns>
    public byte[] ToBytes(Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);
        return encoding.GetBytes(ToMarc());
    }


    private (string FieldData, string Directory, int RecordLength, int BaseAddress) BuildDirectory()
    {
        Encoding encoding = CharacterEncoding switch
        {
            CharacterEncoding.UTF8 => Encoding.UTF8,
            CharacterEncoding.MARC8 => new Marc8Encoding(),
            _ => throw new InvalidOperationException($"Unknown character encoding '{CharacterEncoding}'")
        };

        StringBuilder fieldDataBuilder = new();
        StringBuilder directoryBuilder = new();
        int dataEnd = 0;

        foreach (Field field in _fields)
        {
            if (field.IsEmpty)
            {
                continue;
            }

            StringBuilder fieldBuilder = new();
            fieldBuilder.Append(field.ToMarc());
            string fieldData = fieldBuilder.ToString();

            fieldDataBuilder.Append(fieldData);

            int length = encoding.GetByteCount(fieldData);
            directoryBuilder.AppendFormat("{0:000}{1:0000}{2:00000}", field.Tag, length, dataEnd);
            dataEnd += length;
        }

        int baseAddress = MarcConstants.LeaderLength + directoryBuilder.Length + 1;
        int recordLength = baseAddress + dataEnd + 1;

        return (fieldDataBuilder.ToString(), directoryBuilder.ToString(), recordLength, baseAddress);
    }

    private static Regex GetTagRegex(string pattern) => new(pattern, RegexOptions.Compiled);
}
