namespace Respondeo.SummaImporter;

/// <summary>
/// Parses the CCEL public-domain plain-text dump of the Summa Theologica into a structured model.
/// The parser is content-driven (not line-number driven): it recognises the five Part headers, the
/// ALL-CAPS question headers that end in "(N ARTICLES)", and the rule-line + 4-space "Whether ...?"
/// article titles. Inner sections (Objections, On the contrary, I answer that, Reply to Objection)
/// are preserved verbatim inside each article body.
///
/// The implementation is split across several partial files by responsibility:
///   - SummaParser.cs              : top-level orchestration and the part table.
///   - SummaParser.Questions.cs    : question-header detection and normalisation.
///   - SummaParser.Articles.cs     : question body parsing and article-start detection.
///   - SummaParser.Prologue.cs     : inquiry-list trimming for prologues.
///   - SummaParser.Text.cs         : paragraph extraction and section-cue tokenisation.
///   - SummaParser.References.cs   : cross-reference/objection placeholder tokenisation.
///   - SummaParser.Titles.cs       : title clean-up and title-casing helpers.
/// </summary>
internal static partial class SummaParser
{
    // The five parts in reading order, each keyed by the marker that appears in the text. The Id is
    // the stable, neutral storage key baked into question ids, filenames, and cross-reference tokens
    // (p1/p2a/p2b/p3/sup); the app maps it to a URL slug and display label at render time.
    private static readonly (string Id, string Title, string Marker)[] Parts =
    [
        ("p1", "First Part", "FIRST PART (FP"),
        ("p2a", "First Part of the Second Part", "FIRST PART OF THE SECOND PART"),
        ("p2b", "Second Part of the Second Part", "SECOND PART OF THE SECOND PART"),
        ("p3", "Third Part", "THIRD PART (TP"),
        ("sup", "Supplement", "SUPPLEMENT (XP"),
    ];

    public static IReadOnlyList<ParsedPart> Parse(string[] lines)
    {
        var partStarts = FindPartStarts(lines);
        var parts = new List<ParsedPart>();

        for (var p = 0; p < partStarts.Count; p++)
        {
            var start = partStarts[p].Line;
            var end = p + 1 < partStarts.Count ? partStarts[p + 1].Line : lines.Length;
            var part = ParsePart(partStarts[p].Id, partStarts[p].Title, lines, start, end);
            parts.Add(part);
        }

        return parts;
    }

    private static List<(string Id, string Title, int Line)> FindPartStarts(string[] lines)
    {
        var found = new List<(string Id, string Title, int Line)>();
        foreach (var (id, title, marker) in Parts)
        {
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(marker, StringComparison.Ordinal))
                {
                    found.Add((id, title, i));
                    break;
                }
            }
        }

        found.Sort((a, b) => a.Line.CompareTo(b.Line));
        return found;
    }

    private static ParsedPart ParsePart(string id, string title, string[] lines, int start, int end)
    {
        var questions = new List<ParsedQuestion>();
        var number = 0;

        var headerLines = FindQuestionHeaders(lines, start, end);
        for (var q = 0; q < headerLines.Count; q++)
        {
            var qStart = headerLines[q].Line;
            var qEnd = q + 1 < headerLines.Count ? headerLines[q + 1].Line : end;
            number++;
            questions.Add(ParseQuestion(id, number, TitleCase(headerLines[q].Title), lines, qStart, qEnd));
        }

        return new ParsedPart(id, title, questions);
    }
}

internal sealed record ParsedPart(string Id, string Title, IReadOnlyList<ParsedQuestion> Questions);

internal sealed record ParsedQuestion(
    string Id,
    string PartId,
    int Number,
    string Title,
    string PrologueMarkdown,
    IReadOnlyList<ParsedArticle> Articles);

internal sealed record ParsedArticle(int Number, string Title, string BodyMarkdown);
