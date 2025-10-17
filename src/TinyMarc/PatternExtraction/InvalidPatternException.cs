// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace TinyMarc.PatternExtraction;

public class InvalidPatternException : Exception
{
    /// <summary>
    /// Gets the pattern that was given that was invalid.
    /// </summary>
    public string Pattern { get; set; }

    public InvalidPatternException(string pattern, string message, Exception? innerException = default)
        : base(message, innerException) => Pattern = pattern;
}
