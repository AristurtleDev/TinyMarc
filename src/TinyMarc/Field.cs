// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace TinyMarc;

public abstract class Field
{
    private string _tag;

    /// <summary>
    /// Gets or sets the three-character field tag.
    /// </summary>
    /// <exception cref="MarcInvalidTagException">Thrown when setting an invalid tag.</exception>
    public string Tag
    {
        get => _tag;
        set
        {
            if (!IsValidTag(value))
            {
                throw new MarcInvalidTagException($"Tag '{value}' is not a valid tag.");
            }
            _tag = value;
        }
    }

    /// <summary>
    /// Gets a value that indicates whether this field is empty;
    /// </summary>
    public abstract bool IsEmpty { get; }

    /// <summary>
    /// Gets a value that indicates whether this field is a control field (001 - 009).
    /// </summary>
    public bool IsControlField => this is ControlField;

    /// <summary>
    /// Gets a value that indicates whether this field is a data field (010 - 999).
    /// </summary>
    public bool IsDataField => this is DataField;

    /// <summary>
    /// Initializes a new instance of the <see cref="Field"/> class.
    /// </summary>
    /// <param name="tag">Three character field tag.</param>
    /// <exception cref="MarcInvalidTagException">Thrown when tag is invalid.</exception>
    protected Field(string tag)
    {
        if (!IsValidTag(tag))
        {
            throw new MarcInvalidTagException(tag);
        }
        _tag = tag;
    }

    /// <summary>
    /// Returns a MARC formatted string representation of the field.
    /// </summary>
    internal abstract string ToMarc();

    /// <summary>
    /// Determines whether a MARC tag (three alphanumeric characters) is valid.
    /// </summary>
    /// <param name="tag">Tag to validate.</param>
    /// <returns><see langword="true"/> if the tag is valid; otherwise, <see langword="false"/>.</returns>
    protected abstract bool IsValidTag(string tag);

    /// <summary>
    /// Returns a formatted string representation of the field.
    /// </summary>
    /// <returns>Formatted field string.</returns>
    public override string ToString() => Tag;
}
