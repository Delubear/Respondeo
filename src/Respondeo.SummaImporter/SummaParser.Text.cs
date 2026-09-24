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

    // The section header is normally colon-delimited ("Objection 1:") but the source occasionally uses a
    // period ("Objection 1."); accept either so the objection/reply is tokenised rather than leaking into
    // the preamble.
    [GeneratedRegex(@"^Reply to Objection (?<n>\d+)[:.]", RegexOptions.Compiled)]
    private static partial Regex ReplyRegex();

    [GeneratedRegex(@"^Objection (?<n>\d+)[:.]", RegexOptions.Compiled)]
    private static partial Regex ObjectionRegex();

    // Matches the leading section-cue token that ExtractText/FormatParagraph puts at the start of each
    // classic Summa section paragraph, e.g. "{{scue|objection|2}}" or "{{scue|respondeo}}".
    [GeneratedRegex(@"^\{\{scue\|(?<kind>objection|reply|contra|respondeo)(?:\|(?<n>\d+))?\}\}", RegexOptions.Compiled)]
    private static partial Regex SectionCueRegex();

    // Splits an already-linkified article body (Markdown paragraphs joined by blank lines, each classic
    // section paragraph carrying a leading {{scue|...}} token) into the structural pieces of a Summa
    // article. The {{scue|...}} tokens are preserved inside each bucket so the render stage still emits
    // the same labels and deep-link anchor ids. Paragraphs before the first cue become the preamble;
    // paragraphs without their own cue attach to the currently open section (e.g. a multi-paragraph
    // "I answer that").
    internal static ArticleSections SplitArticleSections(string bodyMarkdown)
    {
        var preamble = new List<string>();
        var objections = new List<(int Number, List<string> Paragraphs)>();
        var replies = new List<(int Number, List<string> Paragraphs)>();
        var sedContra = new List<string>();
        var respondeo = new List<string>();

        // The bucket that trailing (cue-less) paragraphs get appended to.
        List<string> current = preamble;

        if (!string.IsNullOrEmpty(bodyMarkdown))
        {
            foreach (var paragraph in bodyMarkdown.Split("\n\n", StringSplitOptions.None))
            {
                if (paragraph.Length == 0)
                {
                    continue;
                }

                var cue = SectionCueRegex().Match(paragraph);
                if (!cue.Success)
                {
                    current.Add(paragraph);
                    continue;
                }

                var kind = cue.Groups["kind"].Value;
                switch (kind)
                {
                    case "objection":
                        objections.Add((int.Parse(cue.Groups["n"].Value), current = [paragraph]));
                        break;
                    case "reply":
                        replies.Add((int.Parse(cue.Groups["n"].Value), current = [paragraph]));
                        break;
                    case "contra":
                        current = sedContra;
                        current.Add(paragraph);
                        break;
                    case "respondeo":
                        current = respondeo;
                        current.Add(paragraph);
                        break;
                }
            }
        }

        static string Join(List<string> paragraphs) => string.Join("\n\n", paragraphs);

        return new ArticleSections(
            Join(preamble),
            objections.Select(o => new NumberedSection(o.Number, Join(o.Paragraphs))).ToList(),
            sedContra.Count > 0 ? Join(sedContra) : null,
            respondeo.Count > 0 ? Join(respondeo) : null,
            replies.Select(r => new NumberedSection(r.Number, Join(r.Paragraphs))).ToList());
    }

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
