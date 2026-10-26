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
        var isPsalm = slug == "psalms";
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

            if (isPsalm)
            {
                if (!TryAddPsalm(spans, startChapter, startVerse, endChapter, endVerse))
                {
                    return null;
                }
            }
            else
            {
                spans.Add(new VerseSpan(startChapter, startVerse, endChapter, endVerse));
            }
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

    private static bool TryAddPsalm(List<VerseSpan> spans, int startChapter, int startVerse, int endChapter, int endVerse)
    {
        if (startChapter != endChapter)
        {
            return false;
        }

        // Psalms split or merged between the Hebrew and Vulgate numbering need verse shifts.
        var (chapter, offset) = (startChapter, startVerse) switch
        {
            (116, >= 10) => (115, 9),
            (147, >= 12) => (147, 11),
            (9, _) => (9, 0),
            (10, _) => (9, -21),
            (114, _) => (113, 0),
            (115, _) => (113, -8),
            _ => (VulgateNumber(startChapter), 0),
        };

        if (chapter == 0)
        {
            return false;
        }

        spans.Add(new VerseSpan(chapter, startVerse - offset, chapter, endVerse - offset));
        return true;
    }

    private static int VulgateNumber(int hebrew) => hebrew switch
    {
        <= 8 => hebrew,
        <= 113 => hebrew - 1,
        116 => 114,
        <= 146 => hebrew - 1,
        147 => 146,
        _ => hebrew,
    };

    private static readonly Regex Pattern = new(@"^(?<book>(?:\d\s)?[A-Za-z][A-Za-z ]*?)\s+(?<refs>\d.*)$", RegexOptions.Compiled);

    private static Regex CitationPattern() => Pattern;
}
