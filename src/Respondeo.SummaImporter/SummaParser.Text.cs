using System.Text;
using System.Text.RegularExpressions;

namespace Respondeo.SummaImporter;

// Paragraph extraction from the hard-wrapped source lines and tokenisation of the classic Summa section cues
// (Objection, On the contrary, I answer that, Reply to Objection).
internal static partial class SummaParser
{
    // Turns the indented, hard-wrapped source lines into Markdown paragraphs.
    // Blank lines separate paragraphs; the recognised inner markers (Objection, On the contrary, I answer that, Reply)
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

            // A treatise heading (e.g. "TREATISE ON HABITS (QQ[49]-54)") sits between questions but falls inside
            // the previous question's range, so it would otherwise leak into the last article's body. Once seen,
            // nothing after it belongs to the current article, so stop extracting here.
            if (TreatiseHeadingRegex().IsMatch(line))
            {
                break;
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
        // Emit neutral tokens for the classic Summa section cues instead of baking presentation (e.g. bold) into the corpus.
        // The render stage decides the markup and styling, so these structural markers can be restyled without regenerating the JSON.
        // Tokens take the form {{scue|<kind>|<number?>}}: "objection"/"reply" carry the number, "contra"/"respondeo" do not.
        if (text.StartsWith("On the contrary,", StringComparison.Ordinal))
        {
            return $"{{{{scue|contra}}}}{text["On the contrary,".Length..]}";
        }

        if (text.StartsWith("I answer that,", StringComparison.Ordinal))
        {
            return $"{{{{scue|respondeo}}}}{text["I answer that,".Length..]}";
        }

        var reply = ReplyRegex().Match(text);
        if (reply.Success)
        {
            return $"{{{{scue|reply|{reply.Groups["n"].Value}}}}}{text[reply.Length..]}";
        }

        var objection = ObjectionRegex().Match(text);
        if (objection.Success)
        {
            return $"{{{{scue|objection|{objection.Groups["n"].Value}}}}}{text[objection.Length..]}";
        }

        return text;
    }

    [GeneratedRegex(@"^Reply to Objection (?<n>\d+):", RegexOptions.Compiled)]
    private static partial Regex ReplyRegex();

    [GeneratedRegex(@"^Objection (?<n>\d+):", RegexOptions.Compiled)]
    private static partial Regex ObjectionRegex();

    // A treatise heading between questions. The source uses several formats, e.g.
    // "TREATISE ON HABITS (QQ[49]-54)", "TREATISE ON THE CREATION (QQ 44-46)",
    // "TREATISE ON THE DISTINCTION OF THINGS IN GENERAL (Q[47])" and
    // "TREATISE ON SACRED DOCTRINE [1](Q[1])". These are not part of any article body. The match is
    // case-sensitive, so the all-caps heading is caught while ordinary prose ("the treatise on
    // charity") is left untouched.
    [GeneratedRegex(@"^\s*TREATISE\b", RegexOptions.Compiled)]
    private static partial Regex TreatiseHeadingRegex();

    // Turns a raw treatise heading line into a clean display title. The descriptive name always
    // precedes the first "[" or "(", which introduce the CCEL question-range/footnote (e.g.
    // "(QQ[22]-48)", "[1](Q[1])") and any trailing tail like "GOOD HABITS, i.e. VIRTUES"; everything
    // from that bracket onward is dropped and the remainder is title-cased for display.
    internal static string CleanTreatiseTitle(string line)
    {
        var text = line.Trim();
        var cut = text.IndexOfAny(['[', '(']);
        if (cut >= 0)
        {
            text = text[..cut];
        }

        return TitleCase(text.Trim());
    }
}
