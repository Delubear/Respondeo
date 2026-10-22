using System.Text.RegularExpressions;

namespace Respondeo.Bible;

/// <summary>A contiguous span of verses, possibly crossing chapters, in one book of the Douay-Rheims dataset.</summary>
internal sealed record VerseSpan(int Chapter, int StartVerse, int EndChapter, int EndVerse);

/// <summary>A citation resolved to a Douay-Rheims book file and its verse spans.</summary>
internal sealed record BiblePassage(string BookSlug, IReadOnlyList<VerseSpan> Spans);

/// <summary>Parses modern citations such as "Psalm 5:2-10, 12-13" or "Isaiah 61:10-62:5" into Douay-Rheims passages.</summary>
internal static class BibleCitation
{
    private static readonly Dictionary<string, string> Books = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Psalm"] = "psalms", ["Psalms"] = "psalms",
        ["Ezra"] = "1-esdras", ["Nehemiah"] = "2-esdras",
        ["1 Samuel"] = "1-kings", ["2 Samuel"] = "2-kings", ["1 Kings"] = "3-kings", ["2 Kings"] = "4-kings",
        ["1 Chronicles"] = "1-paralipomenon", ["2 Chronicles"] = "2-paralipomenon",
        ["Obadiah"] = "abdias", ["Haggai"] = "aggeus", ["Revelation"] = "apocalypse",
        ["Song of Songs"] = "canticle-of-canticles", ["Sirach"] = "ecclesiasticus", ["Ezekiel"] = "ezechiel",
        ["Habakkuk"] = "habacuc", ["Isaiah"] = "isaie", ["Jeremiah"] = "jeremie", ["Jonah"] = "jonas",
        ["Joshua"] = "josue", ["Malachi"] = "malachie", ["Micah"] = "micheas", ["Hosea"] = "osee",
        ["Zephaniah"] = "sophonias", ["Tobit"] = "tobias", ["Zechariah"] = "zacharias",
        ["1 Maccabees"] = "1-machabees", ["2 Maccabees"] = "2-machabees",
    };

    /// <summary>A sentinel end verse meaning "to the end of the chapter".</summary>
    public const int EndOfChapter = int.MaxValue;

    public static BiblePassage? Parse(string citation)
    {
        var match = CitationPattern().Match(citation.Trim());
        if (!match.Success)
        {
            return null;
        }

        var book = match.Groups["book"].Value.Trim();
        var slug = Books.TryGetValue(book, out var mapped) ? mapped : book.ToLowerInvariant().Replace(' ', '-');
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
