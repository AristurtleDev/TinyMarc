// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace TinyMarc;

/// <summary>
/// Represents a MARC control field (tags 001 - 009).
/// </summary>
public class ControlField : Field
{
    /// <summary>
    /// Gets or sets the control field data.
    /// </summary>
    public string Data { get; set; }

    /// <summary>
    /// Gets a value that indicates whether this control field is empty.
    /// </summary>
    public override bool IsEmpty => string.IsNullOrEmpty(Data);

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlField"/> class.
    /// </summary>
    /// <param name="tag">Three-character field tag (001 - 009).</param>
    /// <param name="data">Control field data.</param>
    /// <exception cref="MarcInvalidTagException">Thrown when tag is invalid.</exception>
    public ControlField(string tag, string data) : base(tag)
    {
        Data = data ?? string.Empty;
    }

    /// <summary>
    /// Returns a formatted string representation of the control field.
    /// </summary>
    /// <returns>Formatted control field string.</returns>
    public override string ToString() => $"{base.ToString()}    {Data}";

    /// <summary>
    /// Returns a MARC formatted string representation of the control field.
    /// </summary>
    internal override string ToMarc() => Data + MarcConstants.EndOfField;

    /// <summary>
    /// Determines whether the specified tag is a valid MARC control field tag.
    /// </summary>
    /// <param name="tag">The tag to validate. Must be a string of exactly three characters.</param>
    /// <returns><see langword="true"/> if the tag is a valid three-character numeric string within the range of 0 to 9;
    /// otherwise, <see langword="false"/>.</returns>
    protected override bool IsValidTag(string tag)
    {
        return tag.Length == 3
               && int.TryParse(tag, out int value)
               && value > 0 && value < 10;
    }
}
