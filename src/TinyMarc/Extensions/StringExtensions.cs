// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace TinyMarc.Extensions;

internal static class StringExtensions
{
    /// <summary>
    /// Trims any non-word character from <paramref name="value"/> given.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to trim.</param>
    /// <returns>A new <see cref="string"/>.</returns>
    public static string TrimPunctuation(this string value)
    {
        //  Matches any non-word character specifically at the end of the
        //  string.
        value = Regex.Replace(value, @"\W+$", "");

        //  Perform an additional leading and trailing trim since the above
        //  regex trim could leave additional spaces.
        return value.Trim();
    }
}
