// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace TinyMarc.PatternExtraction;

/// <summary>
/// Defines values that describe the how to include alternate script fields when extracting data from a
/// <see cref="Record"/> using an <see cref="IFieldExtractor"/>.
/// </summary>
public enum AlternateField
{
    /// <summary>
    /// Defines that alternate script fields should be included along with the original data fields they are linked too.
    /// </summary>
    Include,

    /// <summary>
    /// Defines that alternate script fields should not be included.
    /// </summary>
    DontInclude,

    /// <summary>
    ///  Defines that only the alternate script fields should be included and not the data fields they are linked too.
    /// </summary>
    Only
}
