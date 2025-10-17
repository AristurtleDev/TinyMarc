namespace TinyMarc;

/// <summary>
/// Defines helper methods used in MARC-21 record processing.
/// </summary>
public static class MarcHelper
{
    /// <summary>
    /// Determines whether a tag represents a control field (001 - 009).
    /// </summary>
    /// <param name="tag">Three-character tag.</param>
    /// <returns><see langword="true"/> if <paramref name="tag"/> is a control field; otherwise, <see langword="false"/>.</returns>
    public static bool IsControlField(ReadOnlySpan<char> tag)
    {
        if (tag.Length != 3)
        {
            return false;
        }

        return tag[0] == '0' && tag[1] == '0' && tag[2] >= '1' && tag[2] <= '9';
    }

    /// <summary>
    /// Validates a MARC tag format (three alphanumeric characters).
    /// </summary>
    /// <param name="tag">Tag to validate.</param>
    /// <returns><see langword="true"/> if <paramref name="tag"/> is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidTag(ReadOnlySpan<char> tag)
    {
        if (tag.Length != 3)
        {
            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            char c = tag[i];
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether an indicator character is valid according to MARC-21 specifications.
    /// </summary>
    /// <param name="indicator">Indicator character to check.</param>
    /// <returns><see langword="true"/> if the indicator is blank or a digit (0-9); otherwise, <see langword="false"/>.</returns>
    public static bool IsValidIndicator(char indicator)
    {
        return indicator == ' ' || (indicator >= '0' && indicator <= '9');
    }

    /// <summary>
    /// Normalizes an indicator value to a single character (space if <see langword="null"/> or empty).
    /// </summary>
    /// <param name="indicator">Indicator character or <see langword="null"/>.</param>
    /// <returns>Single character indicator.</returns>
    public static char NormalizeIndicator(char? indicator) => indicator ?? ' ';
}
