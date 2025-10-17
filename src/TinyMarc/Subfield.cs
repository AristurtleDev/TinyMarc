// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace TinyMarc;

/// <summary>
/// Represents a subfield within a MARC data field.
/// </summary>
public sealed class Subfield
{
    /// <summary>
    /// Gets the single-character subfield code.
    /// </summary>
    public char Code { get; }

    /// <summary>
    /// Gets or sets the subfield data.
    /// </summary>
    public string Data { get; set; }

    /// <summary>
    /// Gets a value that indicates whether this subfield is empty (no data).
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Data);

    /// <summary>
    /// Initializes a new instance of the <see cref="Subfield"/> class.
    /// </summary>
    /// <param name="code">Single-character subfield code.</param>
    /// <param name="data">Subfield data.</param>
    public Subfield(char code, string data)
    {
        Code = code;
        Data = data ?? string.Empty;
    }

    /// <summary>
    /// Returns a string representation of the subfield.
    /// </summary>
    /// <returns>Formatted subfield string.</returns>
    public override string ToString() => $"{Code}| {Data}";

    /// <summary>
    /// Returns a MARC formatted string representation of the subfield.
    /// </summary>
    internal string ToMarc() => $"{MarcConstants.SubfieldIndicator}{Code}{Data}";
}
