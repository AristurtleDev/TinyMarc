// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using TinyMarc.Extensions;

namespace TinyMarc.PatternExtraction;

/// <summary>
/// An extractor used to extract data from the Control Fields of a <see cref="Record"/>.
/// </summary>
internal class ControlFieldExtractor : IFieldExtractor
{
    /// <summary>
    /// Gets the Variable Control Field tag that this extractor will match when extracting values.
    /// </summary>
    public string Tag { get; private set; }

    /// <summary>
    /// Gets an optional <see cref="Range"/> value that defines a slice of the Control Field's data to extract instead
    /// of the entire data string.
    /// </summary>
    public Range? Range { get; private set; } = default;

    /// <summary>
    /// Creates a new <see cref="ControlFieldExtractor"/> class instance initialized to extract data based on the
    /// <paramref name="pattern"/> given.
    /// </summary>
    /// <param name="pattern">The Data Field pattern that describes what data to extract.</param>
    public ControlFieldExtractor(string pattern) => ParsePattern(pattern);

    /// <summary>
    /// Parses the pattern given to be used by this <see cref="ControlFieldExtractor"/>
    /// </summary>
    /// <param name="pattern">The pattern to parse.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if the pattern given is an invalid control field extractor pattern.
    /// </exception>
    [MemberNotNull(nameof(Tag))]
    private void ParsePattern(string pattern)
    {
        //  Control field patterns must be a minimum 3 characters that are
        //  numeric and define the tag of the field this will extract from.
        if (pattern.Length < 3)
        {
            throw new ArgumentException(nameof(pattern));
        }

        //  Get the tag
        Tag = pattern[0..3];

        //  Validate that the tag is numeric and that is is within the
        //  acceptable range for control fields (001 - 009)
        if (!int.TryParse(Tag, out int tag))
        {
            throw new ArgumentException(nameof(pattern));
        }
        else if (tag > 9)
        {
            throw new ArgumentException(nameof(pattern));
        }

        //  Remove the tag from the pattern
        pattern = pattern[3..];

        //  If there are still values in the pattern to parse then that means
        //  a slice was given
        if (pattern.Length > 0)
        {
            //  Slices must be between [] brackets. So the first and last
            //  characters remaining in the pattern must be the [] brackets
            if (pattern[0] != '[' || pattern[^1] != ']')
            {
                throw new ArgumentException(nameof(pattern));
            }

            //  Remove the [] brackets from the pattern
            pattern = pattern[1..^1];

            //  The remaining can be either a number that defines a single
            //  character position or it could be a range (two numbers separated
            //  by a '-' hyphen).
            string[] split = pattern.Split('-', StringSplitOptions.RemoveEmptyEntries);

            int start;
            int end;

            //  Try to parse the start value out
            if (!int.TryParse(split[0], out start))
            {
                throw new ArgumentException(nameof(pattern));
            }

            //  If there's a second value, try to parse it for the end value
            if (split.Length > 1)
            {
                //  Try to parse out the end value
                if (!int.TryParse(split[1], out end))
                {
                    throw new ArgumentException(nameof(pattern));
                }
            }
            else
            {
                //  There was not a second value, so the start and end are the
                //  same.
                end = start;
            }

            //  Regardless of the above, the end value needs to be incremented
            //  by 1 since our pattern is end 'inclusive' but C# Range.End is
            //  'exclusive'
            end++;

            Range = new(start, end);
        }
    }

    /// <summary>
    /// Extracts data from the <paramref name="record"/> given  based on the pattern used to initialize this
    /// <see cref="ControlFieldExtractor"/>.
    /// </summary>
    /// <param name="record">The <see cref="Record"/> to extract the data from.</param>
    /// <param name="options">
    /// An <see cref="ExtractorOptions"/> value tha defines additional options to use when extracting the data.
    /// </param>
    /// <returns>
    /// A new <see cref="Array"/> of <see cref="string"/> elements where
    /// each element is the data extracted.
    /// </returns>
    public string[] Extract(Record record, ExtractorOptions options)
    {
        //  Used to quickly check if a value is already added in case the options
        //  specify that duplicates aren't allowed.
        HashSet<string> duplicateLookup = new();

        //  Will hold the values that we extract.
        List<string> extracted = new();

        //  Even though most control fields are labeled NR (non-repeatable),
        //  some, such as field 006 are repeatable. So we just enumerate based
        //  on tag instead of pulling a single field
        foreach (ControlField field in record.GetFields(Tag))
        {
            string data;

            //  If a range was given in the patter, only extract the characters
            //  within that range.
            if (Range is not null)
            {
                data = field.Data[Range.Value.Start..Range.Value.End];
            }
            else
            {
                //  No range defined, get the entire data value
                data = field.Data;
            }

            //  Check here for duplicate.  We could check after punctuation is
            //  trimmed, but it would be the same result regardless.  Doing it
            //  here those saves the step of trimming then checking
            if (!options.AllowDuplicates)
            {
                if (duplicateLookup.Contains(data))
                {
                    //  Just move on to the next enumeration
                    continue;
                }

                duplicateLookup.Add(data);
            }

            //  Should punctuation be trimmed?
            if (options.TrimPunctuation)
            {
                data = data.TrimPunctuation();
            }

            extracted.Add(data);

            //  If we were told to extract only the first value found, then
            //  break out early
            if (options.First)
            {
                break;
            }
        }

        return extracted.ToArray();
    }
}
