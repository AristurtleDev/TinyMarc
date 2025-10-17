// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using TinyMarc.Extensions;

namespace TinyMarc.PatternExtraction;

/// <summary>
/// An extract used to extract data from the Data Fields of <see cref="Record"/>.
/// </summary>
internal sealed class DataFieldExtractor : IFieldExtractor
{
    //  The data field tag value to match when extracting values.
    private string _tag;

    //  An optional first indicator to match when extracting values.
    private char? _indicator1 = default;

    //  An optional second indicator to match when extracting values.
    private char? _indicator2 = default;

    //  An optional mapping of subfields to extract where the key is the
    //  subfield code and the value is a boolean specifying if multiple
    //  instances of that subfield should have the data joined in one
    //  extract.
    //  A mapping of all subfields to extract where the key is the subfield
    //  code and the value is a boolean specifying if multiple instances of
    //  that subfield should have the data joined in one extract
    //
    //  When no subfields are provided in the pattern it is assumed that all
    //  subfields will be extracted, in that case, the pattern parser will
    //  inject a '*' code into this lookup.
    private readonly Dictionary<char, bool> _subfieldLookup = [];

    /// <summary>
    /// Creates a new <see cref="DataFieldExtractor"/> class instance initialized to extract data based on the
    /// <paramref name="pattern"/> given.
    /// </summary>
    /// <param name="pattern">The Data Field pattern that describes what data to extract.</param>
    public DataFieldExtractor(string pattern) => ParsePattern(pattern);

    /// <summary>
    /// Parses the pattern given to be used by this <see cref="DataFieldExtractor"/>
    /// </summary>
    /// <param name="pattern">The pattern to parse.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if the pattern given is an invalid data field extractor pattern.
    /// </exception>
    [MemberNotNull(nameof(_tag))]
    private void ParsePattern(string pattern)
    {
        //  Data field patterns must at minimum be 3 characters that are
        //  numeric and define the tag of the field that this will extract from.
        if (pattern.Length < 3)
        {
            throw new ArgumentException(nameof(pattern));
        }

        //  Get the tag
        _tag = pattern[0..3];

        //  Validate that the tag is numeric and that it is within the
        //  acceptable range for data fields (010 - 999)
        if (!int.TryParse(_tag, out int tag))
        {
            throw new ArgumentException(nameof(pattern));
        }
        else if (tag < 10)
        {
            throw new ArgumentException(nameof(pattern));
        }

        //  Remove the tag from the pattern
        pattern = pattern[3..];

        //  If there are still values in the pattern to parse, then there might
        //  be indicators specified and/or subfield codes
        if (pattern.Length > 0)
        {
            //  Check if there are '|' pipe characters that indicate data field
            //  indicators are being specified
            if (pattern.Contains('|'))
            {
                if (pattern.IndexOf('|') != 0)
                {
                    //  there is a '|' pipe character, but it does not appear
                    //  directly after the tag in the pattern.
                    throw new ArgumentException("Indicators must be enclosed by '|' characters and come directly after the tag", nameof(pattern));
                }

                //  Indicators should always be 4 characters in pattern, the
                //  two enclosing '|' pipes and the two indicator values between
                //  them.
                if (pattern.Length < 4 || pattern[3] != '|')
                {
                    throw new ArgumentException("Only two indicators can be given in a data field pattern", nameof(pattern));
                }

                //  Retrieve the 2nd and 3rd characters as the first and second
                //  indicators respectively
                _indicator1 = pattern[1];
                _indicator2 = pattern[2];

                //  Remove the indicator specification from the pattern
                pattern = pattern[4..];
            }

            //  The remainder of the pattern, if there is any, will be the
            //  subfield codes.
            if (pattern.Length > 0)
            {
                Regex codeMatch = new("^[0-9a-z]{1}$");

                for (int i = 0; i < pattern.Length; i++)
                {
                    char code = pattern[i];

                    //  Validate that the code is alphanumeric
                    if (!codeMatch.IsMatch($"{code}"))
                    {
                        throw new ArgumentException($"Invalid subfield code '{code}' specified in data field pattern.  Subfield codes must be alphanumeric (0-9 or a-z)", nameof(pattern));
                    }

                    //  Check if the code has already been added.  Repeated
                    //  codes are marked for join all
                    if (_subfieldLookup.ContainsKey(code))
                    {
                        _subfieldLookup[code] = true;
                    }
                    else
                    {
                        _subfieldLookup.Add(code, false);
                    }
                }
            }
            else
            {
                //  There were no subfield codes in the pattern, which means
                //  that when using this extractor, all subfields will be
                //  extracted.  To handle this, we'll add a '*' character as
                //  the subfield pattern
                _subfieldLookup.Add('*', false);
            }
        }
    }

    /// <summary>
    /// Extracts data from the <paramref name="record"/> given  based on the pattern used to initialize this
    /// <see cref="DataFieldExtractor"/>.
    /// </summary>
    /// <param name="record">The <see cref="Record"/> to extract the data from.</param>
    /// <param name="options">
    /// An <see cref="ExtractorOptions"/> value tha defines additional options to use when extracting the data.
    /// </param>
    /// <returns>
    /// A new <see cref="Array"/> of <see cref="string"/> elements where each element is the data extracted.
    /// </returns>
    public string[] Extract(Record record, ExtractorOptions options)
    {
        //  Holds the values that we extract
        List<string> extracted = [];

        //  Are we only included Alternate Script field data?
        if (options.AlternateField == AlternateField.Only)
        {
            extracted.AddRange(ExtractAlternateScriptFields(record, options));
        }
        else
        {
            //  Enumerate all data fields that match the tag for this extractor
            foreach (DataField field in record.GetFields(_tag))
            {
                extracted.AddRange(ExtractSubfields(field, options));
            }

            //  If we are including Alternate Fields, add them now
            if (options.AlternateField == AlternateField.Include)
            {
                extracted.AddRange(ExtractAlternateScriptFields(record, options));
            }
        }


        //  Through all of the extracting that just happen, if no values were
        //  actually extracted, and there was a default value specified in the
        //  options, add the default value
        if (extracted.Count == 0 && !string.IsNullOrEmpty(options.Default))
        {
            extracted.Add(options.Default);
        }

        //  If the options allow for duplicates, then we can return as is.
        //  Otherwise, we return a deduped collection.
        //  Duplications are checked during the subfield extraction part and is
        //  probably not needed here, but will need to test before removing.
        if (!options.AllowDuplicates)
        {
            return extracted.ToArray();
        }
        else
        {
            return extracted.Distinct().ToArray();
        }
    }

    private List<string> ExtractAlternateScriptFields(Record record, ExtractorOptions options)
    {
        //  Will hold the values we extract.
        List<string> extracted = new();

        //  Alternate Script fields are Field Tag 880 where the Subfield Code
        //  $6 is the same as the tag for the Data Field we are extracting as
        //  part of this extractor.
        foreach (DataField field880 in record.GetFields("880"))
        {
            //  Get Subfield $6
            Subfield? subfield6 = field880.GetSubfield('6');

            if (subfield6 is not null)
            {
                //  The data for this subfield is 3 characters being the tag
                //  it is linked too followed by a '-' then a number
                //  representing the occurrence. We only care about the tag
                string tag = subfield6.Data[0..3];

                //  If the tag matches the tag this extractor is for, the
                //  we extract this 880 field
                if (tag == _tag)
                {
                    extracted.AddRange(ExtractSubfields(field880, options));
                }
            }
        }

        return extracted;
    }

    /// <summary>
    /// Extracts the <see cref="Subfield"/> data from the given <paramref name="field"/>.
    /// </summary>
    /// <param name="field">
    /// The <see cref="DataField"/> to extract the <see cref="Subfield"/> data from.
    /// </param>
    /// <param name="options">
    /// An <see cref="ExtractorOptions"/> value tha defines additional options to use when extracting the data.
    /// </param>
    /// <returns>
    /// A new <see cref="List{T}"/> of <see cref="string"/> elements where each element is the data extracted.
    /// </returns>
    private List<string> ExtractSubfields(DataField field, ExtractorOptions options)
    {
        //  Used to check for duplicates if we're told to not allow duplicates
        HashSet<string> duplicateLookup = new();

        //  Holds all values that we extract from the subfields
        List<string> extracted = new();

        //  Go code-by-code from the pattern and extract the values for that
        //  code.
        foreach (KeyValuePair<char, bool> patternCode in _subfieldLookup)
        {
            //  Just pulling these out to make the code more readable.
            char code = patternCode.Key;
            bool joinAll = patternCode.Value;

            //  This will hold the values we extract for this subfield code.
            //  If the pattern specified that that we should join all instances
            //  of this code, then we'll join this before adding to the
            //  main extracted list.
            List<string> patternExtracted = new();

            //  Go subfield-by-subfield for the current pattern code.
            foreach (Subfield subfield in field.GetSubfields(code))
            {
                string data = subfield.Data;

                //  Check for duplication here to avoid trimming if it's
                //  a duplicate and not needed
                if (!options.AllowDuplicates)
                {
                    if (duplicateLookup.Contains(data))
                    {
                        //  This is a duplicate, move to the next iteration
                        continue;
                    }
                    else
                    {
                        //  Not a duplicate, add to lookup
                        duplicateLookup.Add(data);
                    }
                }

                //  Should we trim punctuation?
                if (options.TrimPunctuation)
                {
                    data = data.TrimPunctuation();
                }

                //  Add to the extracted values
                patternExtracted.Add(data);
            }

            //  Should we join all instances of this subfield together? If so
            //  we join them with a ' ' space separator
            if (joinAll)
            {
                extracted.Add(string.Join(' ', patternExtracted));
            }
            else
            {
                extracted.AddRange(patternExtracted);
            }
        }

        //  If no separator is defined in the options, we can return the
        //  extracted results as is; one entry per subfield code.
        //  Otherwise, we need to join all extracted subfield data using the
        //  separator
        if (options.Separator is null)
        {
            return extracted;
        }
        else
        {
            List<string> joined = [];
            joined.Add(string.Join(options.Separator, extracted));
            return joined;
        }
    }

}
