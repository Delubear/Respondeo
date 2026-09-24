using System.Text;
using System.Text.RegularExpressions;

namespace Respondeo.SummaImporter;

// Question body parsing: separating the prologue from the articles and detecting article starts.
internal static partial class SummaParser
{
    // A horizontal rule line (a run of underscores). Marks article and section boundaries.
    [GeneratedRegex(@"^\s*_{20,}\s*$", RegexOptions.Compiled)]
    private static partial Regex RuleRegex();

    // A 4-space-indented line that begins an article title. Titles may wrap over several lines and
    // end with a question mark.
    [GeneratedRegex(@"^    (?<start>\S.*)$", RegexOptions.Compiled)]
    private static partial Regex TitleStartRegex();

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
            var body = Linkify(ExtractText(lines, contentStart, end), partId, number, 1);
            return new ParsedQuestion(
                $"{partId}-q{number:D3}",
                partId,
                number,
                title,
                string.Empty,
                [new ParsedArticle(1, title, body)]);
        }

        var prologueEnd = articleStarts.Count > 0 ? articleStarts[0].RuleLine : end;
        var prologue = Linkify(TrimInquiryList(ExtractText(lines, contentStart, prologueEnd)), partId, number);

        var articles = new List<ParsedArticle>();
        for (var a = 0; a < articleStarts.Count; a++)
        {
            var bodyStart = articleStarts[a].BodyLine;
            var bodyEnd = a + 1 < articleStarts.Count ? articleStarts[a + 1].RuleLine : end;
            var body = Linkify(ExtractText(lines, bodyStart, bodyEnd), partId, number, a + 1);
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
}
