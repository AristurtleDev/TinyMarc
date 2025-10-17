// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace TinyMarc.PatternExtraction;

/// <summary>
/// Creates a new <see cref="ExtractorOptions"/> record instance.
/// </summary>
/// <remarks>
/// Exposes configurable options for the a <see cref="MarcExtractor"/> instance.
/// </remarks>
/// <param name="First">Extract only the first value found from the extraction pattern.</param>
/// <param name="TrimPunctuation">Trim leading and trailing punctuation marks from each value extracted.</param>
/// <param name="Default">A default value to provided if no values are found.</param>
/// <param name="AllowDuplicates">Whether the result should include duplicate values.</param>
/// <param name="Separator">The separator to use when combining multiple subfield values together.</param>
/// <param name="AlternateField">
/// An <see cref="AlternateField"/> enum value that specifies if alternate data from a linked 880 field should be
/// include, not not included, or only use the alternate field data.
/// </param>
public record ExtractorOptions(bool First = false,
                                   bool TrimPunctuation = false,
                                   string? Default = default,
                                   bool AllowDuplicates = false,
                                   string? Separator = default,
                                   AlternateField AlternateField = AlternateField.Include);
