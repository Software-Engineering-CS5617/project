using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Cloud.Crash;

/// <summary>
/// Cleans text before it goes into a crash report.
/// </summary>
public static partial class CrashText
{
    private const string UserPlaceholder = "<user>";

    /// <summary>
    /// Removes characters that are not allowed in XML 1.0, such as control characters and lone surrogates.
    /// </summary>
    /// <param name="value">The text to clean.</param>
    /// <returns>The cleaned text, never null.</returns>
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder? builder = null;

        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            int length = 1;
            bool keep = true;

            if (!XmlConvert.IsXmlChar(current))
            {
                if (i + 1 < value.Length && XmlConvert.IsXmlSurrogatePair(value[i + 1], current))
                {
                    length = 2;
                }
                else
                {
                    keep = false;
                }
            }

            if (!keep)
            {
                builder ??= new StringBuilder(value, 0, i, value.Length);
            }
            else if (builder is not null)
            {
                builder.Append(value, i, length);
            }

            i += length - 1;
        }

        return builder?.ToString() ?? value;
    }

    /// <summary>
    /// Replaces user names in profile paths (for example <c>C:\Users\alice\</c>) with a placeholder.
    /// </summary>
    /// <param name="value">The text to scrub.</param>
    /// <returns>The scrubbed text, never null.</returns>
    public static string ScrubPaths(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string scrubbed = WindowsUserPathRegex().Replace(value, "${prefix}" + UserPlaceholder);
        return UnixUserPathRegex().Replace(scrubbed, "${prefix}" + UserPlaceholder);
    }

    /// <summary>
    /// Shortens text to at most the given number of characters, ending with "..." when it was cut.
    /// </summary>
    /// <param name="value">The text to shorten.</param>
    /// <param name="maxLength">The maximum length, at least 4.</param>
    /// <returns>The shortened text, never null.</returns>
    public static string Truncate(string? value, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, 4);

        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value ?? string.Empty;
        }

        int cut = maxLength - 3;

        // Do not split a surrogate pair.
        if (char.IsHighSurrogate(value[cut - 1]))
        {
            cut--;
        }

        return string.Concat(value.AsSpan(0, cut), "...");
    }

    [GeneratedRegex(@"(?<prefix>[A-Za-z]:[\\/]+Users[\\/]+)[^\\/:""'<>|\r\n]+(?=[\\/])", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, 200)]
    private static partial Regex WindowsUserPathRegex();

    [GeneratedRegex(@"(?<prefix>/(?:Users|home)/)[^/:""'<>|\r\n]+(?=/)", RegexOptions.CultureInvariant, 200)]
    private static partial Regex UnixUserPathRegex();
}
