using System.Text;
using System.Text.RegularExpressions;

namespace Respondeo.SummaImporter;

// Paragraph extraction from the hard-wrapped source lines and tokenisation of the classic Summa
// section cues (Objection, On the contrary, I answer that, Reply to Objection).
internal static partial class SummaParser
{
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
        // Emit neutral tokens for the classic Summa section cues instead of baking presentation
        // (e.g. bold) into the corpus. The render stage decides the markup and styling, so these
        // structural markers can be restyled without regenerating the JSON. Tokens take the form
        // {{scue|<kind>|<number?>}}: "objection"/"reply" carry the number, "contra"/"respondeo" do not.
        if (text.StartsWith("On the contrary,", StringComparison.Ordinal))
        {
            return $"{{{{scue|contra}}}}{text["On the contrary,".Length..]}";
        }

        if (text.StartsWith("I answer that,", StringComparison.Ordinal))
        {
            return $"{{{{scue|respondeo}}}}{text["I answer that,".Length..]}";
        }

        var reply = Regex.Match(text, @"^Reply to Objection (?<n>\d+):");
        if (reply.Success)
        {
            return $"{{{{scue|reply|{reply.Groups["n"].Value}}}}}{text[reply.Length..]}";
        }

        var objection = Regex.Match(text, @"^Objection (?<n>\d+):");
        if (objection.Success)
        {
            return $"{{{{scue|objection|{objection.Groups["n"].Value}}}}}{text[objection.Length..]}";
        }

        return text;
    }
}
