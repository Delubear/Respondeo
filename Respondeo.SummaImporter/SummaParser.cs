using System.Text;
using System.Text.RegularExpressions;

namespace Respondeo.SummaImporter;

/// <summary>
/// Parses the CCEL public-domain plain-text dump of the Summa Theologica into a structured model.
/// The parser is content-driven (not line-number driven): it recognises the five Part headers, the
/// ALL-CAPS question headers that end in "(N ARTICLES)", and the rule-line + 4-space "Whether ...?"
/// article titles. Inner sections (Objections, On the contrary, I answer that, Reply to Objection)
/// are preserved verbatim inside each article body.
/// </summary>
internal static partial class SummaParser
{
    // The five parts in reading order, each keyed by the marker that appears in the text.
    private static readonly (string Id, string Title, string Marker)[] Parts =
    [
        ("fp", "First Part", "FIRST PART (FP"),
        ("fs", "First Part of the Second Part", "FIRST PART OF THE SECOND PART"),
        ("ss", "Second Part of the Second Part", "SECOND PART OF THE SECOND PART"),
        ("tp", "Third Part", "THIRD PART (TP"),
        ("xp", "Supplement", "SUPPLEMENT (XP"),
    ];

    // An ALL-CAPS question header ends in "(N ARTICLES)" or "(ONE ARTICLE)", optionally followed by
    // a bracketed translator footnote like "[*...]". The article-count may also wrap onto its own
    // line, in which case the title sits on the preceding line (handled in FindQuestionHeaders).
    [GeneratedRegex(@"^\s{2,}(?<title>[A-Z0-9].*?)\s*\((?:ONE ARTICLE|TWO ARTICLES|THREE ARTICLES|FOUR ARTICLES|FIVE ARTICLES|SIX ARTICLES|SEVEN ARTICLES|EIGHT ARTICLES|NINE ARTICLES|TEN ARTICLES|ELEVEN ARTICLES|TWELVE ARTICLES|THIRTEEN ARTICLES|FOURTEEN ARTICLES|FIFTEEN ARTICLES|SIXTEEN ARTICLES|SEVENTEEN ARTICLES|EIGHTEEN ARTICLES)\)(?:\s*\[\*.*)?\s*$", RegexOptions.Compiled)]
    private static partial Regex QuestionHeaderRegex();

    // The article-count when it has wrapped onto its own line (title is on the preceding line).
    [GeneratedRegex(@"^\s{2,}\((?:ONE ARTICLE|TWO ARTICLES|THREE ARTICLES|FOUR ARTICLES|FIVE ARTICLES|SIX ARTICLES|SEVEN ARTICLES|EIGHT ARTICLES|NINE ARTICLES|TEN ARTICLES|ELEVEN ARTICLES|TWELVE ARTICLES|THIRTEEN ARTICLES|FOURTEEN ARTICLES|FIFTEEN ARTICLES|SIXTEEN ARTICLES|SEVENTEEN ARTICLES|EIGHTEEN ARTICLES)\)(?:\s*\[\*.*)?\s*$", RegexOptions.Compiled)]
    private static partial Regex WrappedCountRegex();

    // A header whose "(N ARTICLES)" marker is split across two lines because the title is long: the
    // title line ends with "(NUMBER" and the following line starts with "ARTICLES)". Captures the
    // title text that precedes the opening bracket.
    [GeneratedRegex(@"^\s{2,}(?<title>[A-Z0-9].*?)\s*\((?:ONE|TWO|THREE|FOUR|FIVE|SIX|SEVEN|EIGHT|NINE|TEN|ELEVEN|TWELVE|THIRTEEN|FOURTEEN|FIFTEEN|SIXTEEN|SEVENTEEN|EIGHTEEN)\s*$", RegexOptions.Compiled)]
    private static partial Regex SplitCountTitleRegex();

    // The continuation line of a split "(N ARTICLES)" marker: "ARTICLE)" or "ARTICLES)".
    [GeneratedRegex(@"^\s*ARTICLES?\)(?:\s*\[\*.*)?\s*$", RegexOptions.Compiled)]
    private static partial Regex SplitCountContinuationRegex();

    // A horizontal rule line (a run of underscores). Marks article and section boundaries.
    [GeneratedRegex(@"^\s*_{20,}\s*$", RegexOptions.Compiled)]
    private static partial Regex RuleRegex();

    // A 4-space-indented line that begins an article title. Titles may wrap over several lines and
    // end with a question mark.
    [GeneratedRegex(@"^    (?<start>\S.*)$", RegexOptions.Compiled)]
    private static partial Regex TitleStartRegex();

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

    private static List<(string Title, int Line)> FindQuestionHeaders(string[] lines, int start, int end)
    {
        var headers = new List<(string Title, int Line)>();
        for (var i = start; i < end; i++)
        {
            var match = QuestionHeaderRegex().Match(lines[i]);
            if (match.Success)
            {
                // The title may have wrapped across lines. Header/title lines are indented exactly
                // two spaces (prose paragraphs use three), so gather any contiguous preceding
                // two-space title lines and prepend them.
                var titleParts = new List<string> { match.Groups["title"].Value };
                var headerLine = i;
                var t = i - 1;
                while (t >= start && IsTitleContinuationLine(lines[t]))
                {
                    titleParts.Insert(0, lines[t]);
                    headerLine = t;
                    t--;
                }

                var title = NormalizeTitle(string.Join(" ", titleParts));
                if (title.Length > 0)
                {
                    headers.Add((title, headerLine));
                }

                continue;
            }

            // Handle the variant where the "(N ARTICLES)" marker itself is split across two lines
            // because the title is long: the title line ends with "(NUMBER" and the next line starts
            // with "ARTICLES)". The title is the text before the opening bracket on this line.
            var splitMatch = SplitCountTitleRegex().Match(lines[i]);
            if (splitMatch.Success
                && i + 1 < end
                && SplitCountContinuationRegex().IsMatch(lines[i + 1]))
            {
                // The title may itself have wrapped, so gather any preceding contiguous non-blank,
                // non-rule lines and prepend them to the portion captured on this line.
                var titleParts = new List<string> { splitMatch.Groups["title"].Value };
                var j = i - 1;
                while (j >= start && lines[j].Trim().Length > 0 && !RuleRegex().IsMatch(lines[j]))
                {
                    titleParts.Insert(0, lines[j]);
                    j--;
                }

                var title = NormalizeTitle(string.Join(" ", titleParts));
                if (title.Length > 0)
                {
                    headers.Add((title, i));
                }

                continue;
            }

            // Handle the variant where "(N ARTICLES)" wrapped onto its own line: the title is on
            // the preceding non-blank line(s), which may themselves span multiple lines.
            if (WrappedCountRegex().IsMatch(lines[i]))
            {
                var j = i - 1;
                while (j >= start && lines[j].Trim().Length == 0)
                {
                    j--;
                }

                if (j >= start && !RuleRegex().IsMatch(lines[j]))
                {
                    // Gather all contiguous preceding non-blank, non-rule lines as the full title.
                    var titleLines = new List<string>();
                    var k = j;
                    while (k >= start && lines[k].Trim().Length > 0 && !RuleRegex().IsMatch(lines[k]))
                    {
                        titleLines.Insert(0, lines[k]);
                        k--;
                    }

                    var title = NormalizeTitle(string.Join(" ", titleLines));
                    if (title.Length > 0)
                    {
                        headers.Add((title, k + 1));
                    }
                }
            }
        }

        return headers;
    }

    // A wrapped question-title continuation line: indented exactly two spaces (prose paragraphs use
    // three), non-blank, not a rule line, and containing no lowercase letters (titles are ALL-CAPS).
    private static bool IsTitleContinuationLine(string line)
    {
        if (line.Length < 3 || line[0] != ' ' || line[1] != ' ' || line[2] == ' ')
        {
            return false;
        }

        var trimmed = line.Trim();
        if (trimmed.Length == 0 || RuleRegex().IsMatch(line))
        {
            return false;
        }

        foreach (var c in trimmed)
        {
            if (char.IsLower(c))
            {
                return false;
            }
        }

        return true;
    }

    private static ParsedQuestion ParseQuestion(string partId, int number, string title, string[] lines, int start, int end)
    {
        // Skip any leftover header lines (the title continuation lines and the wrapped/split
        // "(N ARTICLES)" marker) so they do not leak into the prologue or article body.
        var contentStart = start + 1;
        for (var s = start + 1; s < end && s < start + 6; s++)
        {
            // A split marker: a title line ending in "(NUMBER" followed by an "ARTICLES)" line.
            if (SplitCountTitleRegex().IsMatch(lines[s])
                && s + 1 < end
                && SplitCountContinuationRegex().IsMatch(lines[s + 1]))
            {
                contentStart = s + 2;
                break;
            }

            if (QuestionHeaderRegex().IsMatch(lines[s])
                || WrappedCountRegex().IsMatch(lines[s])
                || SplitCountContinuationRegex().IsMatch(lines[s]))
            {
                contentStart = s + 1;
                break;
            }

            // Stop once real prose begins (a three-space-indented paragraph line).
            if (lines[s].Trim().Length > 0 && lines[s].StartsWith("   ", StringComparison.Ordinal))
            {
                break;
            }

            contentStart = s + 1;
        }

        // The prologue is the text between the question header and the first article rule/title.
        var articleStarts = FindArticleStarts(lines, contentStart, end);

        // Some single-article questions (e.g. "On the Work of the Fifth Day") open the article body
        // directly after the header, with no rule line and no "Whether ...?" title. When no article
        // structure is detected but the content clearly contains article prose, treat the whole
        // question body as a single article so the text is not lost.
        if (articleStarts.Count == 0 && ContainsArticleProse(lines, contentStart, end))
        {
            var body = ExtractText(lines, contentStart, end);
            return new ParsedQuestion(
                $"{partId}-q{number:D3}",
                partId,
                number,
                title,
                string.Empty,
                [new ParsedArticle(1, title, body)]);
        }

        var prologueEnd = articleStarts.Count > 0 ? articleStarts[0].RuleLine : end;
        var prologue = ExtractText(lines, contentStart, prologueEnd);

        var articles = new List<ParsedArticle>();
        for (var a = 0; a < articleStarts.Count; a++)
        {
            var bodyStart = articleStarts[a].BodyLine;
            var bodyEnd = a + 1 < articleStarts.Count ? articleStarts[a + 1].RuleLine : end;
            var body = ExtractText(lines, bodyStart, bodyEnd);
            articles.Add(new ParsedArticle(a + 1, articleStarts[a].Title, body));
        }

        return new ParsedQuestion($"{partId}-q{number:D3}", partId, number, title, prologue, articles);
    }

    // Detects whether a span of text contains the tell-tale markers of an article body (Objections
    // and "I answer that"). Used to rescue single-article questions that lack a rule/title header.
    private static bool ContainsArticleProse(string[] lines, int start, int end)
    {
        var sawObjection = false;
        var sawAnswer = false;
        for (var i = start; i < end && i < lines.Length; i++)
        {
            var text = lines[i].Trim();
            if (text.StartsWith("Objection 1:", StringComparison.Ordinal))
            {
                sawObjection = true;
            }
            else if (text.StartsWith("I answer that,", StringComparison.Ordinal))
            {
                sawAnswer = true;
            }

            if (sawObjection && sawAnswer)
            {
                return true;
            }
        }

        return false;
    }

    // An article begins at a rule line, followed (after blanks) by a 4-space-indented "Whether ...?"
    // title that may wrap across lines and ends with '?'. Returns the rule line (boundary), the title,
    // and the body start line (just after the title).
    private static List<(int RuleLine, string Title, int BodyLine)> FindArticleStarts(string[] lines, int start, int end)
    {
        var starts = new List<(int RuleLine, string Title, int BodyLine)>();
        for (var i = start; i < end; i++)
        {
            if (!RuleRegex().IsMatch(lines[i]))
            {
                continue;
            }

            // Skip blank lines after the rule.
            var j = i + 1;
            while (j < end && lines[j].Trim().Length == 0)
            {
                j++;
            }

            if (j >= end)
            {
                break;
            }

            // A title must be a 4-space-indented line. If the next content is another rule or a
            // question header, this rule was a section divider, not an article start.
            var titleMatch = TitleStartRegex().Match(lines[j]);
            if (!titleMatch.Success || QuestionHeaderRegex().IsMatch(lines[j]))
            {
                continue;
            }

            // Accumulate the (possibly wrapped) title until a line ending in '?'.
            var sb = new StringBuilder();
            var k = j;
            while (k < end)
            {
                var text = lines[k].Trim();
                if (text.Length == 0)
                {
                    break;
                }

                sb.Append(sb.Length == 0 ? "" : " ").Append(text);
                if (text.EndsWith('?'))
                {
                    k++;
                    break;
                }

                k++;
            }

            var title = NormalizeTitle(sb.ToString());
            if (!title.EndsWith('?'))
            {
                // Not a real article title (no question). Treat the rule as a section divider.
                continue;
            }

            starts.Add((i, title, k));
        }

        return starts;
    }

    // Turns the indented, hard-wrapped source lines into Markdown paragraphs. Blank lines separate
    // paragraphs; the recognised inner markers (Objection, On the contrary, I answer that, Reply)
    // are emphasised so the article structure survives into the rendered HTML.
    private static string ExtractText(string[] lines, int start, int end)
    {
        var paragraphs = new List<string>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length > 0)
            {
                paragraphs.Add(FormatParagraph(current.ToString().Trim()));
                current.Clear();
            }
        }

        for (var i = start; i < end && i < lines.Length; i++)
        {
            var line = lines[i];
            if (RuleRegex().IsMatch(line))
            {
                continue;
            }

            var text = line.Trim();
            if (text.Length == 0)
            {
                Flush();
                continue;
            }

            current.Append(current.Length == 0 ? "" : " ").Append(text);
        }

        Flush();
        return string.Join("\n\n", paragraphs);
    }

    private static string FormatParagraph(string text)
    {
        // Emphasise the structural cues that open the classic article sections.
        foreach (var cue in new[] { "On the contrary,", "I answer that," })
        {
            if (text.StartsWith(cue, StringComparison.Ordinal))
            {
                return $"**{cue}**{text[cue.Length..]}";
            }
        }

        var objection = Regex.Match(text, @"^(Objection \d+:|Reply to Objection \d+:)");
        if (objection.Success)
        {
            return $"**{objection.Value}**{text[objection.Length..]}";
        }

        return text;
    }

    private static string NormalizeTitle(string title)
    {
        var cleaned = Regex.Replace(title, @"\s+", " ").Trim();
        // Strip stray CCEL cross-reference brackets like "[76]" that can appear in headers.
        cleaned = Regex.Replace(cleaned, @"\[\d+\]", string.Empty).Trim();
        return cleaned;
    }

    // Question titles arrive in ALL CAPS (e.g. "THE EXISTENCE OF GOD"). Convert to sentence-ish title
    // case for display while keeping short function words lowercase.
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
