using System.Text.RegularExpressions;

namespace Respondeo.SummaImporter;

// Title clean-up and title-casing helpers shared by the question- and article-header parsers.
internal static partial class SummaParser
{
    private static string NormalizeTitle(string title)
    {
        var cleaned = WhitespaceRegex().Replace(title, " ").Trim();
        // Strip stray CCEL cross-reference brackets like "[76]" that can appear in headers.
        cleaned = BracketReferenceRegex().Replace(cleaned, string.Empty).Trim();
        // Remove orphan footnote asterisks (e.g. "Of Fear*", "Irony*").
        // These point to an editorial note that lived beside the dropped "(N ARTICLES)" marker, so the bare "*" is left unexplained.
        // Keep asterisks that open an inline gloss like "[*Scientia]" (i.e. those immediately after "[").
        cleaned = OrphanAsteriskRegex().Replace(cleaned, string.Empty).Trim();
        return cleaned;
    }

    // Question titles arrive in ALL CAPS (e.g. "THE EXISTENCE OF GOD"). Convert to sentence-ish title case for display while keeping short function words lowercase.
    private static string TitleCase(string upper)
    {
        var words = upper.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var small = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "and", "as", "at", "by", "for", "in", "of", "on", "or", "the", "to", "with",
        };

        var result = new List<string>(words.Length);
        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            var lower = word.ToLowerInvariant();
            if (i > 0 && small.Contains(lower))
            {
                result.Add(lower);
            }
            else
            {
                result.Add(char.ToUpperInvariant(lower[0]) + lower[1..]);
            }
        }

        return string.Join(' ', result);
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\[\d+\]", RegexOptions.Compiled)]
    private static partial Regex BracketReferenceRegex();

    [GeneratedRegex(@"(?<!\[)\*", RegexOptions.Compiled)]
    private static partial Regex OrphanAsteriskRegex();
}
