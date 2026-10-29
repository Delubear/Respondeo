using System.Text.RegularExpressions;
using Respondeo.Content.Summa.Contracts;

namespace Respondeo.Content.Summa;

/// <summary>
/// Expands the neutral placeholders emitted by the Summa importer into HTML at render time.
/// The importer writes tokens instead of baking final markup, so link routing,
/// wording and the styling of the classic article section cues live here and can change without regenerating the corpus.
/// Part labels/slugs are resolved through <see cref="ISummaPartMap"/> so this renderer stays free of any dependency on the Summa corpus data.
/// Two token families are handled:
/// <list type="bullet">
/// <item><c>{{sref|kind|partId|q|a}}</c> — a cross-reference, rendered as an anchor.</item>
/// <item><c>{{scue|kind|number?}}</c> — an Objection/Reply/On the contrary/I answer that cue.</item>
/// </list>
/// </summary>
internal sealed partial class SummaReferenceRenderer(ISummaPartMap parts) : ISummaReferenceRenderer
{

    // Matches a single reference placeholder:
    // {{sref|<kind>|<partId>|<q>|<a>}} where <a> may be empty (question-only) or a comma-separated list of article numbers (multi-article citation).
    [GeneratedRegex(@"\{\{sref\|(?<kind>qp|q|a)\|(?<part>[a-z0-9]+)\|(?<q>\d+)\|(?<a>[\d,]*)\}\}", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    // Matches a section-cue placeholder: {{scue|<kind>|<number?>}} where number is only present for "objection" and "reply".
    [GeneratedRegex(@"\{\{scue\|(?<kind>objection|reply|contra|respondeo)(?:\|(?<n>\d+))?\}\}", RegexOptions.Compiled)]
    private static partial Regex CueRegex();

    // Matches an objection cross-reference placeholder: {{sobj|<partId>|<q>|<a>|<kind>|<n>}} where kind is "objection" or "reply".
    // Emitted by the importer alongside the reference an "OBJ[n]" citation trails, and rendered as a link to that objection/reply within the cited article.
    [GeneratedRegex(@"\{\{sobj\|(?<part>[a-z0-9]+)\|(?<q>\d+)\|(?<a>\d+)\|(?<kind>objection|reply)\|(?<n>\d+)\}\}", RegexOptions.Compiled)]
    private static partial Regex ObjectionRegex();

    // Matches a combined citation placeholder:
    // {{scite|<labelKind>|<partId>|<q>|<a>|<kind>|<n>}} where labelKind is "qp" (show part), "q" (same part), "a" (article only) or "self" (bare reply in the current article),
    // and kind is "objection" or "reply". Rendered as a single deep link straight to the cited objection/reply, so "Q. 13, A. 1, ad 2" becomes one link instead of two.
    [GeneratedRegex(@"\{\{scite\|(?<label>qp|q|a|self)\|(?<part>[a-z0-9]+)\|(?<q>\d+)\|(?<a>\d+)\|(?<kind>objection|reply)\|(?<n>\d+)\}\}", RegexOptions.Compiled)]
    private static partial Regex CiteRegex();

    // Display label for a reference that points at another part (e.g. p2b -> "II-II").
    // Sourced from the injected part map so labels can be re-styled without regenerating the corpus.
    private string PartLabel(string partId) => parts.LabelForKey(partId);

    // Public URL slug for a stable part key (e.g. p1 -> "prima"), used to build hrefs.
    private string PartSlug(string partId) => parts.SlugForKey(partId);

    /// <inheritdoc />
    public string Expand(string? html, int? articleNumber = null)
    {
        if (string.IsNullOrEmpty(html) || !html.Contains("{{", StringComparison.Ordinal))
        {
            return html ?? string.Empty;
        }

        html = ExpandReferences(html);
        html = ExpandCitations(html);
        html = ExpandObjections(html);
        html = ExpandCues(html, articleNumber);
        return html;
    }

    // Runs a token regex only when its marker is present, avoiding regex work on the common token-free paragraph.
    private static string ReplaceTokens(string html, string marker, Regex regex, MatchEvaluator evaluator) =>
        html.Contains(marker, StringComparison.Ordinal) ? regex.Replace(html, evaluator) : html;

    private string QuestionHref(string partId, int questionNumber) => $"summa/{PartSlug(partId)}-q{questionNumber:D3}";

    private string ExpandReferences(string html) => ReplaceTokens(html, "{{sref|", TokenRegex(), match =>
    {
        var kind = match.Groups["kind"].Value;
        var partId = match.Groups["part"].Value;
        var questionNumber = int.Parse(match.Groups["q"].Value);
        var articles = match.Groups["a"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var questionHref = QuestionHref(partId, questionNumber);
        var prefix = kind == "qp" ? $"{PartLabel(partId)}, " : string.Empty;

        if (articles.Length == 0)
        {
            // Question-only reference.
            return Anchor(questionHref, $"{prefix}Q. {questionNumber}");
        }

        // Each cited article is its own link. For question references (not same-question "a" references) the first link also carries the question label,
        // e.g. "Q. 2, A. 1, A. 3"; same-question references render as "A. 1, A. 3".
        return string.Join(", ", articles.Select((a, i) => Anchor(
            $"{questionHref}#article-{a}",
            i == 0 && kind != "a" ? $"{prefix}Q. {questionNumber}, A. {a}" : $"A. {a}")));
    });

    private static string Anchor(string href, string display) => $"<a class=\"summa-ref\" href=\"{href}\">{display}</a>";

    // Shared by {{scite}} and {{sobj}}: the deep link to a cited objection/reply and its short "obj. N"/"ad N" label.
    private (string PartId, int QuestionNumber, string Article, string Href, string Cite) ReadDeepLink(Match match)
    {
        var partId = match.Groups["part"].Value;
        var questionNumber = int.Parse(match.Groups["q"].Value);
        var article = match.Groups["a"].Value;
        var kind = match.Groups["kind"].Value;
        var number = match.Groups["n"].Value;

        var href = $"{QuestionHref(partId, questionNumber)}#article-{article}-{kind}-{number}";
        var cite = kind == "reply" ? $"ad&nbsp;{number}" : $"obj.&nbsp;{number}";
        return (partId, questionNumber, article, href, cite);
    }

    private string ExpandCitations(string html) => ReplaceTokens(html, "{{scite|", CiteRegex(), match =>
    {
        var (partId, questionNumber, article, href, cite) = ReadDeepLink(match);
        var display = match.Groups["label"].Value switch
        {
            "self" => cite,
            "a" => $"A.&nbsp;{article}, {cite}",
            "qp" => $"{PartLabel(partId)}, Q.&nbsp;{questionNumber}, A.&nbsp;{article}, {cite}",
            _ => $"Q.&nbsp;{questionNumber}, A.&nbsp;{article}, {cite}",
        };

        return Anchor(href, display);
    });

    private string ExpandObjections(string html) => ReplaceTokens(html, "{{sobj|", ObjectionRegex(), match =>
    {
        var (_, _, _, href, cite) = ReadDeepLink(match);
        return ", " + Anchor(href, cite);
    });

    private static string ExpandCues(string html, int? articleNumber)
    {
        return ReplaceTokens(html, "{{scue|", CueRegex(), match =>
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

            // Section cues get a stable anchor id so cross-references can deep-link straight to them, but only when we know which article they belong to.
            // Objection/reply ids carry the objection number; contra/respondeo are unique within an article so need no number.
            var id = articleNumber is int a
                ? kind switch
                {
                    "objection" or "reply" when number.Length > 0 => $" id=\"article-{a}-{kind}-{number}\"",
                    "contra" or "respondeo" => $" id=\"article-{a}-{kind}\"",
                    _ => string.Empty,
                }
                : string.Empty;

            return $"<strong{id} class=\"summa-cue summa-cue--{cssModifier}\">{label}</strong>";
        });
    }
}
