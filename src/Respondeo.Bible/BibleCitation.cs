using System.Text.RegularExpressions;

namespace Respondeo.Bible;

/// <summary>A contiguous span of verses, possibly crossing chapters, in one book of a translation.</summary>
internal sealed record VerseSpan(int Chapter, int StartVerse, int EndChapter, int EndVerse);

/// <summary>A citation resolved to a translation's book file and its verse spans.</summary>
internal sealed record BiblePassage(string BookSlug, IReadOnlyList<VerseSpan> Spans);

/// <summary>Parses modern citations such as "Psalm 5:2-10, 12-13" or "Isaiah 61:10-62:5" into passages, resolving books via the translation manifest.</summary>
internal static class BibleCitation
{
    /// <summary>Sentinel end verse meaning "to the end of the chapter".</summary>
    public const int EndOfChapter = int.MaxValue;

    public static BiblePassage? Parse(string citation, BibleTranslation translation)
    {
        var match = CitationPattern().Match(citation.Trim());
        if (!match.Success)
        {
            return null;
        }

        var book = BibleReferenceParser.FindBook(match.Groups["book"].Value, translation);
        if (book is null)
        {
            return null;
        }

        var slug = book.Slug;
        var convertPsalms = slug == "psalms" && translation.UsesVulgatePsalmNumbering;
        var spans = new List<VerseSpan>();
        var chapter = 0;

        foreach (var part in match.Groups["refs"].Value.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var bounds = part.Split('-', StringSplitOptions.TrimEntries);
            if (bounds.Length > 2 || !TryPoint(bounds[0], ref chapter, out var startChapter, out var startVerse))
            {
                return null;
            }

            var endChapter = startChapter;
            var endVerse = startVerse;
            if (bounds.Length == 2 && !TryPoint(bounds[1], ref chapter, out endChapter, out endVerse))
            {
                return null;
            }

            if (startChapter == 0)
            {
                return null;
            }

            if (convertPsalms)
            {
                (startChapter, startVerse) = HebrewToVulgatePsalm(startChapter, startVerse);
                (endChapter, endVerse) = HebrewToVulgatePsalm(endChapter, endVerse);
            }

            spans.Add(new VerseSpan(startChapter, startVerse, endChapter, endVerse));
        }

        return spans.Count == 0 ? null : new BiblePassage(slug, spans);
    }

    private static bool TryPoint(string text, ref int chapter, out int pointChapter, out int verse)
    {
        var colon = text.IndexOf(':');
        if (colon >= 0)
        {
            if (!int.TryParse(text[..colon], out chapter))
            {
                pointChapter = verse = 0;
                return false;
            }

            text = text[(colon + 1)..];
        }

        pointChapter = chapter;
        return int.TryParse(text.TrimEnd('a', 'b', 'c'), out verse);
    }

    /// <summary>
    /// Converts a Hebrew-numbered psalm point (as cited) to the Vulgate numbering. The Vulgate merges
    /// Hebrew 9+10 and 114+115, and splits Hebrew 116 and 147, so most psalms in between are one lower.
    /// </summary>
    private static (int Chapter, int Verse) HebrewToVulgatePsalm(int psalm, int verse)
    {
        var (chapter, verseShift) = (psalm, verse) switch
        {
            (<= 8, _) => (psalm, 0),
            (9, _) => (9, 0),                  // Hebrew 9 is the first half of Vulgate 9
            (10, _) => (9, 21),                // Hebrew 10:1 is Vulgate 9:22
            (<= 113, _) => (psalm - 1, 0),
            (114, _) => (113, 0),              // Hebrew 114 is the first half of Vulgate 113
            (115, _) => (113, 8),              // Hebrew 115:1 is Vulgate 113:9
            (116, <= 9) => (114, 0),           // Hebrew 116:1-9 is Vulgate 114
            (116, _) => (115, -9),             // Hebrew 116:10 is Vulgate 115:1
            (<= 146, _) => (psalm - 1, 0),
            (147, <= 11) => (146, 0),          // Hebrew 147:1-11 is Vulgate 146
            (147, _) => (147, -11),            // Hebrew 147:12 is Vulgate 147:1
            _ => (psalm, 0),
        };

        return (chapter, verse == EndOfChapter ? EndOfChapter : verse + verseShift);
    }

    private static readonly Regex Pattern = new(@"^(?<book>(?:\d\s)?[A-Za-z][A-Za-z ]*?)\s+(?<refs>\d.*)$", RegexOptions.Compiled);

    private static Regex CitationPattern() => Pattern;
}
