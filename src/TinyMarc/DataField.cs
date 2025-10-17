// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Text;

namespace TinyMarc;

/// <summary>
/// Represents a MARC data field with indicators and subfields.
/// </summary>
public sealed class DataField : Field
{
    private readonly List<Subfield> _subfields = [];
    private char _indicator1;
    private char _indicator2;

    /// <summary>
    /// Gets or sets the first indicator.
    /// </summary>
    public char Indicator1
    {
        get => _indicator1;
        set
        {
            if (!MarcHelper.IsValidIndicator(value))
            {
                throw new MarcInvalidIndicatorException($"Illegal indicators '{value}'");
            }
            _indicator1 = value;
        }
    }

    /// <summary>
    /// Gets the second indicator.
    /// </summary>
    public char Indicator2
    {
        get => _indicator2;
        set
        {
            if (!MarcHelper.IsValidIndicator(value))
            {
                throw new MarcInvalidIndicatorException($"Illegal indicators '{value}'");
            }
            _indicator2 = value;
        }
    }

    /// <summary>
    /// Gets the subfields collection.
    /// </summary>
    public ReadOnlyCollection<Subfield> Subfields { get; }

    /// <summary>
    /// Gets a value that indicates whether this data field is empty (no subfields).
    /// </summary>
    public override bool IsEmpty => _subfields.Count == 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataField"/> class.
    /// </summary>
    /// <param name="tag">Three-character field tag (010 - 999).</param>
    /// <param name="indicator1">First indicator (space if null).</param>
    /// <param name="indicator2">Second indicator (space if null).</param>
    public DataField(string tag, char indicator1, char indicator2) : base(tag)
    {
        Subfields = _subfields.AsReadOnly();
        Indicator1 = indicator1;
        Indicator2 = indicator2;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataField"/> class with subfields.
    /// </summary>
    /// <param name="tag">Three-character field tag (010-999).</param>
    /// <param name="subfields">Initial collection of subfields.</param>
    /// <param name="indicator1">First indicator character (blank or 0-9).</param>
    /// <param name="indicator2">Second indicator character (blank or 0-9).</param>
    public DataField(string tag, char indicator1, char indicator2, IEnumerable<Subfield> subfields)
        : this(tag, indicator1, indicator2)
    {
        ArgumentNullException.ThrowIfNull(subfields);
        _subfields.AddRange(subfields);
    }

    /// <summary>
    /// Appends a subfield to the end of the subfields list.
    /// </summary>
    /// <param name="subfield">Subfield to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="subfield"/> is <see langword="null"/>.</exception>
    public void AppendSubfield(Subfield subfield)
    {
        ArgumentNullException.ThrowIfNull(subfield);
        _subfields.Add(subfield);
    }

    /// <summary>
    /// Prepends a subfield to the start of the subfields list.
    /// </summary>
    /// <param name="subfield">Subfield to prepend.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="subfield"/> is <see langword="null"/>.</exception>
    public void PrependSubfield(Subfield subfield)
    {
        ArgumentNullException.ThrowIfNull(subfield);
        _subfields.Insert(0, subfield);
    }

    /// <summary>
    /// Inserts a subfield before or after an existing subfield.
    /// </summary>
    /// <param name="newSubField">Subfield to insert.</param>
    /// <param name="existingSubField">Reference subfield.</param>
    /// <param name="before"><see langword="true"/> to insert before; <see langword="false"/> to insert after.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="newSubField"/> or <paramref name="existingSubField"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="existingSubField"/> is not found in this data field.</exception>
    public void InsertSubfield(Subfield newSubField, Subfield existingSubField, bool before = false)
    {
        ArgumentNullException.ThrowIfNull(newSubField);
        ArgumentNullException.ThrowIfNull(existingSubField);

        int index = _subfields.IndexOf(existingSubField);
        if (index == -1)
        {
            throw new ArgumentException("Existing subfield not found in this data field", nameof(existingSubField));
        }

        if (!before)
        {
            index++;
        }

        _subfields.Insert(index, newSubField);
    }

    /// <summary>
    /// Removes a subfield from the field.
    /// </summary>
    /// <param name="subfield">Subfield to remove.</param>
    /// <returns><see langword="true"/> if the subfield was removed; otherwise, <see langword="false"/>.</returns>
    public bool RemoveSubfield(Subfield subfield) => _subfields.Remove(subfield);

    /// <summary>
    /// Removes all subfields with the specified code.
    /// </summary>
    /// <param name="code">Subfield code to remove.</param>
    /// <returns>Number of subfields removed.</returns>
    public int RemoveSubfields(char code) => _subfields.RemoveAll(s => s.Code == code);

    /// <summary>
    /// Gets the first subfield with the specified code.
    /// </summary>
    /// <param name="code">Subfield code.</param>
    /// <returns>First matching subfield, or <see langword="null"/> if not found.</returns>
    public Subfield? GetSubfield(char code) => _subfields.FirstOrDefault(s => s.Code == code);

    /// <summary>
    /// Gets all subfields with the specified code.
    /// </summary>
    /// <param name="code">Subfield code.</param>
    /// <returns>Collection of matching subfields.</returns>
    public IEnumerable<Subfield> GetSubfields(char code) => _subfields.Where(s => s.Code == code);

    /// <summary>
    /// Gets the data from the first subfield with the specified code.
    /// </summary>
    /// <param name="code">Subfield code.</param>
    /// <returns>Subfield data, or <see langword="null"/> if not found.</returns>
    public string? GetSubfieldData(char code)
    {
        Subfield? subfield = GetSubfield(code);
        return subfield?.Data;
    }

    /// <summary>
    /// Returns a formatted string representation of the data field.
    /// </summary>
    /// <returns>Formatted data field string.</returns>
    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append(base.ToString());
        sb.Append(' ').Append(Indicator1).Append(Indicator2);
        sb.Append(' ');

        foreach (Subfield subfield in _subfields)
        {
            sb.Append(subfield.ToString());
        }

        return sb.ToString();
    }

    /// <summary>
    /// Returns a MARC formatted string representation of the data field.
    /// </summary>
    internal override string ToMarc()
    {
        StringBuilder sb = new();
        sb.Append(Indicator1);
        sb.Append(Indicator2);

        foreach (Subfield subfield in _subfields)
        {
            if (!subfield.IsEmpty)
            {
                sb.Append(subfield.ToMarc());
            }
        }

        sb.Append(MarcConstants.EndOfField);

        return sb.ToString();
    }

    /// <summary>
    /// Determines whether the specified tag is a valid MARC data field tag.
    /// </summary>
    /// <param name="tag">The tag to validate. Must be a string of exactly three characters.</param>
    /// <returns><see langword="true"/> if the tag is a three-character string representing a numeric value between 10 and 999,
    /// inclusive; otherwise, <see langword="false"/>.</returns>
    protected override bool IsValidTag(string tag)
    {
        return tag.Length == 3
               && int.TryParse(tag, out int value)
               && value >= 10 && value <= 999;
    }
}
