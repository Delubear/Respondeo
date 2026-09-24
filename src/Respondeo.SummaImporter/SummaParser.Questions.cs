using System.Text.RegularExpressions;

namespace Respondeo.SummaImporter;

// Detection and normalisation of the ALL-CAPS question headers that introduce each question.
internal static partial class SummaParser
{
    // An ALL-CAPS question header ends in "(N ARTICLES)" or "(ONE ARTICLE)",
    // optionally followed by a bracketed translator footnote like "[*...]".
    // The article-count may also wrap onto its own line, in which case the title sits on the preceding line (handled in FindQuestionHeaders).
    [GeneratedRegex(@"^\s{2,}(?<title>[A-Z0-9].*?)\s*\((?:ONE ARTICLE|TWO ARTICLES|THREE ARTICLES|FOUR ARTICLES|FIVE ARTICLES|SIX ARTICLES|SEVEN ARTICLES|EIGHT ARTICLES|NINE ARTICLES|TEN ARTICLES|ELEVEN ARTICLES|TWELVE ARTICLES|THIRTEEN ARTICLES|FOURTEEN ARTICLES|FIFTEEN ARTICLES|SIXTEEN ARTICLES|SEVENTEEN ARTICLES|EIGHTEEN ARTICLES)\)(?:\s*\[\*.*)?\s*$", RegexOptions.Compiled)]
    private static partial Regex QuestionHeaderRegex();

    // The article-count when it has wrapped onto its own line (title is on the preceding line).
    [GeneratedRegex(@"^\s{2,}\((?:ONE ARTICLE|TWO ARTICLES|THREE ARTICLES|FOUR ARTICLES|FIVE ARTICLES|SIX ARTICLES|SEVEN ARTICLES|EIGHT ARTICLES|NINE ARTICLES|TEN ARTICLES|ELEVEN ARTICLES|TWELVE ARTICLES|THIRTEEN ARTICLES|FOURTEEN ARTICLES|FIFTEEN ARTICLES|SIXTEEN ARTICLES|SEVENTEEN ARTICLES|EIGHTEEN ARTICLES)\)(?:\s*\[\*.*)?\s*$", RegexOptions.Compiled)]
    private static partial Regex WrappedCountRegex();

    // A header whose "(N ARTICLES)" marker is split across two lines because the title is long:
    // the title line ends with "(NUMBER" and the following line starts with "ARTICLES)".
    // Captures the title text that precedes the opening bracket.
    [GeneratedRegex(@"^\s{2,}(?<title>[A-Z0-9].*?)\s*\((?:ONE|TWO|THREE|FOUR|FIVE|SIX|SEVEN|EIGHT|NINE|TEN|ELEVEN|TWELVE|THIRTEEN|FOURTEEN|FIFTEEN|SIXTEEN|SEVENTEEN|EIGHTEEN)\s*$", RegexOptions.Compiled)]
    private static partial Regex SplitCountTitleRegex();

    // The continuation line of a split "(N ARTICLES)" marker: "ARTICLE)" or "ARTICLES)".
    [GeneratedRegex(@"^\s*ARTICLES?\)(?:\s*\[\*.*)?\s*$", RegexOptions.Compiled)]
    private static partial Regex SplitCountContinuationRegex();

    private static List<(string Title, int Line)> FindQuestionHeaders(string[] lines, int start, int end)
    {
        var headers = new List<(string Title, int Line)>();
        for (var i = start; i < end; i++)
        {
            var match = QuestionHeaderRegex().Match(lines[i]);
            if (match.Success)
            {
                // The title may have wrapped across lines.
                // Header/title lines are indented exactly two spaces (prose paragraphs use three),
                // so gather any contiguous preceding two-space title lines and prepend them.
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

            // Handle the variant where the "(N ARTICLES)" marker itself is split across two lines because the title is long:
            // the title line ends with "(NUMBER" and the next line starts with "ARTICLES)". The title is the text before the opening bracket on this line.
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

            // Handle the variant where "(N ARTICLES)" wrapped onto its own line: the title is on the preceding non-blank line(s),
            // which may themselves span multiple lines.
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

    // A wrapped question-title continuation line: indented exactly two spaces (prose paragraphs use three),
    // non-blank, not a rule line, and containing no lowercase letters (titles are ALL-CAPS).
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
}
