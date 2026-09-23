using System.Text;
using System.Text.RegularExpressions;

namespace Respondeo.Services;

/// <summary>
/// Expands the neutral placeholders emitted by the Summa importer into HTML at render time. The
/// importer writes tokens instead of baking final markup, so link routing, wording and the styling
/// of the classic article section cues live here in the app and can change without regenerating the
/// corpus. Two token families are handled:
/// <list type="bullet">
/// <item><c>{{sref|kind|partId|q|a}}</c> — a cross-reference, rendered as an anchor.</item>
/// <item><c>{{scue|kind|number?}}</c> — an Objection/Reply/On the contrary/I answer that cue.</item>
/// </list>
/// </summary>
public static partial class SummaReferenceRenderer
{
    // Matches a single reference placeholder: {{sref|<kind>|<partId>|<q>|<a>}} where <a> may be empty.
    [GeneratedRegex(@"\{\{sref\|(?<kind>qp|q|a)\|(?<part>[a-z]+)\|(?<q>\d+)\|(?<a>\d*)\}\}", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    // Matches a section-cue placeholder: {{scue|<kind>|<number?>}} where number is only present for
    // "objection" and "reply".
    [GeneratedRegex(@"\{\{scue\|(?<kind>objection|reply|contra|respondeo)(?:\|(?<n>\d+))?\}\}", RegexOptions.Compiled)]
    private static partial Regex CueRegex();

    // CCEL part tokens for display when a reference points at another part.
    private static string PartLabel(string partId) => partId.ToUpperInvariant();

    /// <summary>
    /// Replaces every reference and section-cue placeholder in the given HTML. Returns the input
    /// unchanged when it contains no tokens.
    /// </summary>
    public static string Expand(string? html)
    {
        if (string.IsNullOrEmpty(html) || !html.Contains("{{", StringComparison.Ordinal))
        {
            return html ?? string.Empty;
        }

        html = ExpandReferences(html);
        html = ExpandCues(html);
        return html;
    }

    private static string ExpandReferences(string html)
    {
        if (!html.Contains("{{sref|", StringComparison.Ordinal))
        {
            return html;
        }

        return TokenRegex().Replace(html, match =>
        {
            var kind = match.Groups["kind"].Value;
            var partId = match.Groups["part"].Value;
            var questionNumber = int.Parse(match.Groups["q"].Value);
            var article = match.Groups["a"].Value;

            var href = new StringBuilder($"summa/{partId}-q{questionNumber:D3}");
            var display = new StringBuilder();

            if (kind == "a")
            {
                href.Append($"#article-{article}");
                display.Append($"A. {article}");
            }
            else
            {
                if (kind == "qp")
                {
                    display.Append($"{PartLabel(partId)}, ");
                }

                display.Append($"Q. {questionNumber}");
                if (article.Length > 0)
                {
                    href.Append($"#article-{article}");
                    display.Append($", A. {article}");
                }
            }

            return $"<a class=\"summa-ref\" href=\"{href}\">{display}</a>";
        });
    }

    private static string ExpandCues(string html)
    {
        if (!html.Contains("{{scue|", StringComparison.Ordinal))
        {
            return html;
        }

        return CueRegex().Replace(html, match =>
        {
            var kind = match.Groups["kind"].Value;
            var number = match.Groups["n"].Success ? match.Groups["n"].Value : string.Empty;

            var (cssModifier, label) = kind switch
            {
                "objection" => ("objection", $"Objection {number}:"),
                "reply" => ("reply", $"Reply to Objection {number}:"),
                "contra" => ("contra", "On the contrary,"),
                "respondeo" => ("respondeo", "I answer that,"),
                _ => ("", string.Empty),
            };

            return $"<strong class=\"summa-cue summa-cue--{cssModifier}\">{label}</strong>";
        });
    }
}
