using System.Text;
using System.Text.RegularExpressions;

namespace Respondeo.SummaImporter;

// Question body parsing: separating the prologue from the articles and detecting article starts.
internal static partial class SummaParser
{
    // A horizontal rule line (a run of underscores). Marks article and section boundaries.
    [GeneratedRegex(@"^\s*_{20,}\s*$", RegexOptions.Compiled)]
    private static partial Regex RuleRegex();

    // A 4-space-indented line that begins an article title. Titles may wrap over several lines and end with a question mark.
    [GeneratedRegex(@"^    (?<start>\S.*)$", RegexOptions.Compiled)]
    private static partial Regex TitleStartRegex();

    private static ParsedQuestion ParseQuestion(string partId, int number, string title, string? treatise, string[] lines, int start, int end)
    {
        // Skip any leftover header lines (the title continuation lines and the wrapped/split "(N ARTICLES)" marker)
        // so they do not leak into the prologue or article body.
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

        // Some single-article questions (e.g. "On the Work of the Fifth Day") open the article body directly after the header,
        // with no rule line and no "Whether ...?" title.
        // When no article structure is detected but the content clearly contains article prose,
        // treat the whole question body as a single article so the text is not lost.
        if (articleStarts.Count == 0 && ContainsArticleProse(lines, contentStart, end))
        {
            var body = Linkify(ExtractText(lines, contentStart, end), partId, number, 1);
            return new ParsedQuestion(
                $"{partId}-q{number:D3}",
                partId,
                number,
                title,
                treatise,
                string.Empty,
                [new ParsedArticle(1, title, SplitArticleSections(body))]);
        }

        var prologueEnd = articleStarts.Count > 0 ? articleStarts[0].RuleLine : end;
        var prologue = Linkify(TrimInquiryList(ExtractText(lines, contentStart, prologueEnd)), partId, number);

        // A handful of questions have two consecutive articles run together in the source with no rule
        // divider and no second title, which would otherwise merge them into one over-long article. Split
        // those undivided boundaries and recover the missing titles from the question's inquiry list.
        var inquiryTitles = ExtractInquiryTitles(lines, contentStart, prologueEnd);

        var articles = new List<ParsedArticle>();
        for (var a = 0; a < articleStarts.Count; a++)
        {
            var bodyStart = articleStarts[a].BodyLine;
            var bodyEnd = a + 1 < articleStarts.Count ? articleStarts[a + 1].RuleLine : end;

            var segments = SplitUndividedArticles(lines, bodyStart, bodyEnd);
            for (var s = 0; s < segments.Count; s++)
            {
                var articleNumber = articles.Count + 1;
                // The first segment keeps the parsed title; any split-off segments had no title in the
                // source, so recover it from the inquiry list by the article's position.
                var articleTitle = s == 0
                    ? articleStarts[a].Title
                    : InquiryTitleAt(inquiryTitles, articleNumber) ?? articleStarts[a].Title;

                var body = Linkify(ExtractText(lines, segments[s].Start, segments[s].End), partId, number, articleNumber);
                articles.Add(new ParsedArticle(articleNumber, articleTitle, SplitArticleSections(body)));
            }
        }

        return new ParsedQuestion($"{partId}-q{number:D3}", partId, number, title, treatise, prologue, articles);
    }

    // Splits a single article's body line range at "undivided" article boundaries: places where a fresh
    // "Objection 1" cue appears after the article has already emitted a "Reply to Objection" (i.e. the
    // article is finished). The source occasionally omits the rule divider and title between two articles,
    // which would otherwise merge them. Returns one segment per detected article, in order.
    private static List<(int Start, int End)> SplitUndividedArticles(string[] lines, int start, int end)
    {
        var segments = new List<(int Start, int End)>();
        var segmentStart = start;
        var sawReply = false;

        for (var i = start; i < end && i < lines.Length; i++)
        {
            var text = lines[i].Trim();
            var isObjectionOne = text.StartsWith("Objection 1:", StringComparison.Ordinal) || text.StartsWith("Objection 1.", StringComparison.Ordinal);

            if (isObjectionOne && sawReply)
            {
                segments.Add((segmentStart, i));
                segmentStart = i;
                sawReply = false;
            }
            else if (text.StartsWith("Reply to Objection ", StringComparison.Ordinal))
            {
                sawReply = true;
            }
        }

        segments.Add((segmentStart, end));
        return segments;
    }

    // The ordered "Whether ...?" titles enumerated in a question's inquiry list, e.g. "(1) Whether God is a
    // body?". Used to recover titles for articles whose source header (rule divider + title) is missing.
    private static List<string> ExtractInquiryTitles(string[] lines, int start, int end)
    {
        var titles = new List<string>();
        var buffer = new StringBuilder();

        void Flush()
        {
            if (buffer.Length > 0)
            {
                foreach (Match m in InquiryItemRegex().Matches(buffer.ToString()))
                {
                    titles.Add(NormalizeTitle(m.Groups["title"].Value));
                }

                buffer.Clear();
            }
        }

        for (var i = start; i < end && i < lines.Length; i++)
        {
            var text = lines[i].Trim();
            if (text.Length == 0)
            {
                Flush();
                continue;
            }

            buffer.Append(buffer.Length == 0 ? "" : " ").Append(text);
        }

        Flush();
        return titles;
    }

    // Returns the inquiry-list title for the given 1-based article number, or null when unavailable.
    private static string? InquiryTitleAt(List<string> inquiryTitles, int articleNumber)
        => articleNumber >= 1 && articleNumber <= inquiryTitles.Count ? inquiryTitles[articleNumber - 1] : null;

    // A single "(N) <title>?" item inside an inquiry list. Several items may share a paragraph
    // (e.g. "(1) ...?(2) ...?"), so the title is captured non-greedily up to its terminating '?'.
    [GeneratedRegex(@"\(\d+\)\s*(?<title>[^?]*\?)", RegexOptions.Compiled)]
    private static partial Regex InquiryItemRegex();

    // Detects whether a span of text contains the tell-tale markers of an article body (Objections and "I answer that").
    // Used to rescue single-article questions that lack a rule/title header.
    private static bool ContainsArticleProse(string[] lines, int start, int end)
    {
        var sawObjection = false;
        var sawAnswer = false;
        for (var i = start; i < end && i < lines.Length; i++)
        {
            var text = lines[i].Trim();
            if (text.StartsWith("Objection 1:", StringComparison.Ordinal) || text.StartsWith("Objection 1.", StringComparison.Ordinal))
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

    // An article begins at a rule line, followed (after blanks) by a 4-space-indented "Whether ...?" title that may wrap across lines and ends with '?'.
    // Returns the rule line (boundary), the title, and the body start line (just after the title).
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

            // A title must be a 4-space-indented line. If the next content is another rule or a question header, this rule was a section divider, not an article start.
            var titleMatch = TitleStartRegex().Match(lines[j]);
            if (!titleMatch.Success || QuestionHeaderRegex().IsMatch(lines[j]))
            {
                continue;
            }

            // Accumulate the (possibly wrapped) title until a line containing '?'. The '?' may sit mid-line
            // when a footnote or cross-reference annotation trails it (e.g. "... pleasant? [*"Bonum honestum" ...]"),
            // so we cut the title at the first '?' and discard the trailing annotation.
            var sb = new StringBuilder();
            var k = j;
            while (k < end)
            {
                var text = lines[k].Trim();
                if (text.Length == 0)
                {
                    break;
                }

                var mark = text.IndexOf('?');
                if (mark >= 0)
                {
                    sb.Append(sb.Length == 0 ? "" : " ").Append(text[..(mark + 1)]);
                    k++;
                    break;
                }

                sb.Append(sb.Length == 0 ? "" : " ").Append(text);
                k++;
            }

            var title = NormalizeTitle(sb.ToString());
            if (!title.EndsWith('?'))
            {
                // Most article titles are "Whether ...?" questions, but a few are declarative statements
                // (e.g. "The difference of aeviternity and time") that open straight into "Objection 1".
                // Accept those; otherwise treat the rule as a section divider.
                if (!StartsArticleBody(lines, k, end))
                {
                    continue;
                }
            }

            // A footnote/cross-reference annotation can wrap onto lines after the title's '?'. These use the
            // 4-space title indentation (article body prose is 3-space indented), so skip any such trailing
            // annotation lines to keep them out of the article body.
            while (k < end && TitleStartRegex().IsMatch(lines[k]))
            {
                k++;
            }

            starts.Add((i, title, k));
        }

        return starts;
    }

    // Whether the first non-blank line at or after 'from' opens an article body ("Objection 1"). Used to
    // recognise the handful of articles whose title is a declarative statement rather than a "Whether ...?"
    // question.
    private static bool StartsArticleBody(string[] lines, int from, int end)
    {
        for (var i = from; i < end; i++)
        {
            var text = lines[i].Trim();
            if (text.Length == 0)
            {
                continue;
            }

            return text.StartsWith("Objection 1:", StringComparison.Ordinal) || text.StartsWith("Objection 1.", StringComparison.Ordinal);
        }

        return false;
    }
}
