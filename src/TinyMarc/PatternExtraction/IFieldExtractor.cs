// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace TinyMarc.PatternExtraction;

/// <summary>
/// Exposes methods for extracting data from a <see cref="Record"/>.
/// </summary>
internal interface IFieldExtractor
{
    /// <summary>
    /// Extracts data from the <paramref name="record"/> given.
    /// </summary>
    /// <param name="record">The <see cref="Record"/> to extract the data from.</param>
    /// <param name="options">
    /// An <see cref="ExtractorOptions"/> value tha defines additional options to use when extracting the data.
    /// </param>
    /// <returns>
    /// A new <see cref="Array"/> of <see cref="string"/> elements where each element is the data extracted.
    /// </returns>
    string[] Extract(Record record, ExtractorOptions options);
}
