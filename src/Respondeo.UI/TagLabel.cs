namespace Respondeo;

/// <summary>
/// Formats content tag slugs into their display form so every tag filter (Discover articles, prayers, and any future area) capitalizes tags the same way.
/// Tags are stored lowercase in front-matter; this is the single place that decides how they read.
/// </summary>
public static class TagLabel
{
    // Small joining words that stay lowercase in the middle of a tag (e.g. "faith and reason"), the way title case is normally written.
    private static readonly HashSet<string> MinorWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a",
        "an",
        "and",
        "as",
        "at",
        "but",
        "by",
        "for",
        "in",
        "nor",
        "of",
        "on",
        "or",
        "the",
        "to",
        "vs",
        "with",
    };

    /// <summary>
    /// Title-cases a tag slug (e.g. "faith-and-reason" -> "Faith and Reason"): every word is capitalized except minor joining
    /// words, and the first word is always capitalized regardless.
    /// </summary>
    public static string Format(string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return tag;
        }

        var words = tag.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            // The first word is always capitalized; later minor words stay lowercase.
            words[i] = i > 0 && MinorWords.Contains(words[i]) ? words[i].ToLowerInvariant() : Capitalize(words[i]);
        }

        return string.Join(' ', words);
    }

    private static string Capitalize(string word) => char.ToUpperInvariant(word[0]) + word[1..];
}

