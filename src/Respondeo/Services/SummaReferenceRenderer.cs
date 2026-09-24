using System.Text.RegularExpressions;
using Respondeo.Content.Summa;

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
    // Matches a single reference placeholder: {{sref|<kind>|<partId>|<q>|<a>}} where <a> may be
    // empty (question-only) or a comma-separated list of article numbers (multi-article citation).
    [GeneratedRegex(@"\{\{sref\|(?<kind>qp|q|a)\|(?<part>[a-z0-9]+)\|(?<q>\d+)\|(?<a>[\d,]*)\}\}", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    // Matches a section-cue placeholder: {{scue|<kind>|<number?>}} where number is only present for
    // "objection" and "reply".
    [GeneratedRegex(@"\{\{scue\|(?<kind>objection|reply|contra|respondeo)(?:\|(?<n>\d+))?\}\}", RegexOptions.Compiled)]
    private static partial Regex CueRegex();

    // Matches an objection cross-reference placeholder: {{sobj|<partId>|<q>|<a>|<kind>|<n>}} where
    // kind is "objection" or "reply". Emitted by the importer alongside the reference an "OBJ[n]"
    // citation trails, and rendered as a link to that objection/reply within the cited article.
    [GeneratedRegex(@"\{\{sobj\|(?<part>[a-z0-9]+)\|(?<q>\d+)\|(?<a>\d+)\|(?<kind>objection|reply)\|(?<n>\d+)\}\}", RegexOptions.Compiled)]
    private static partial Regex ObjectionRegex();

    // Matches a combined citation placeholder: {{scite|<labelKind>|<partId>|<q>|<a>|<kind>|<n>}} where
    // labelKind is "qp" (show part), "q" (same part), "a" (article only) or "self" (bare reply in the
    // current article), and kind is "objection" or "reply". Rendered as a single deep link straight to
    // the cited objection/reply, so "Q. 13, A. 1, ad 2" becomes one link instead of two.
    [GeneratedRegex(@"\{\{scite\|(?<label>qp|q|a|self)\|(?<part>[a-z0-9]+)\|(?<q>\d+)\|(?<a>\d+)\|(?<kind>objection|reply)\|(?<n>\d+)\}\}", RegexOptions.Compiled)]
    private static partial Regex CiteRegex();

    // Display label for a reference that points at another part (e.g. p2b -> "II-II"). Sourced from
    // the shared SummaParts registry so labels can be re-styled without regenerating the corpus.
    private static string PartLabel(string partId) => SummaParts.LabelForKey(partId);

    // Public URL slug for a stable part key (e.g. p1 -> "prima"), used to build hrefs.
    private static string PartSlug(string partId) => SummaParts.SlugForKey(partId);

    /// <summary>
    /// Replaces every reference and section-cue placeholder in the given HTML. Returns the input
    /// unchanged when it contains no tokens. When <paramref name="articleNumber"/> is supplied, the
    /// section cues emit stable in-page anchor ids (e.g. "article-3-objection-2", "article-3-contra")
    /// so cross-references can deep-link to them.
    /// </summary>
    public static string Expand(string? html, int? articleNumber = null)
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
            var articles = match.Groups["a"].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var questionHref = $"summa/{PartSlug(partId)}-q{questionNumber:D3}";

            if (kind == "a")
            {
                // Same-question article reference: render each cited article as its own link,
                // e.g. "AA[1],3" -> "A. 1, A. 3" with both numbers clickable.
                return string.Join(", ", articles.Select(a =>
                    Anchor($"{questionHref}#article-{a}", $"A. {a}")));
            }

            var prefix = kind == "qp" ? $"{PartLabel(partId)}, " : string.Empty;

            if (articles.Length == 0)
            {
                // Question-only reference.
                return Anchor(questionHref, $"{prefix}Q. {questionNumber}");
            }

            // Question + one or more articles: the first link carries the question label, and each
            // additional cited article is rendered as its own trailing link.
            var links = new List<string>
            {
                Anchor($"{questionHref}#article-{articles[0]}", $"{prefix}Q. {questionNumber}, A. {articles[0]}"),
            };

            for (var i = 1; i < articles.Length; i++)
            {
                links.Add(Anchor($"{questionHref}#article-{articles[i]}", $"A. {articles[i]}"));
            }

            return string.Join(", ", links);
        });
    }

    private static string Anchor(string href, string display) =>
        $"<a class=\"summa-ref\" href=\"{href}\">{display}</a>";

    private static string ExpandCitations(string html)
    {
        if (!html.Contains("{{scite|", StringComparison.Ordinal))
        {
            return html;
        }

        return CiteRegex().Replace(html, match =>
        {
            var label = match.Groups["label"].Value;
            var partId = match.Groups["part"].Value;
            var questionNumber = int.Parse(match.Groups["q"].Value);
            var article = match.Groups["a"].Value;
            var kind = match.Groups["kind"].Value;
            var number = match.Groups["n"].Value;

            var fragment = $"article-{article}-{kind}-{number}";
            var href = $"summa/{PartSlug(partId)}-q{questionNumber:D3}#{fragment}";

            var cite = kind == "reply" ? $"ad&nbsp;{number}" : $"obj.&nbsp;{number}";
            var display = label switch
            {
                "self" => cite,
                "a" => $"A.&nbsp;{article}, {cite}",
                "qp" => $"{PartLabel(partId)}, Q.&nbsp;{questionNumber}, A.&nbsp;{article}, {cite}",
                _ => $"Q.&nbsp;{questionNumber}, A.&nbsp;{article}, {cite}",
            };

            return Anchor(href, display);
        });
    }

    private static string ExpandObjections(string html)
    {
        if (!html.Contains("{{sobj|", StringComparison.Ordinal))
        {
            return html;
        }

        return ObjectionRegex().Replace(html, match =>
        {
            var partId = match.Groups["part"].Value;
            var questionNumber = int.Parse(match.Groups["q"].Value);
            var article = match.Groups["a"].Value;
            var kind = match.Groups["kind"].Value;
            var number = match.Groups["n"].Value;

            var fragment = $"article-{article}-{kind}-{number}";
            var href = $"summa/{PartSlug(partId)}-q{questionNumber:D3}#{fragment}";
            var label = kind == "reply" ? $"ad&nbsp;{number}" : $"obj.&nbsp;{number}";
            return ", " + Anchor(href, label);
        });
    }

    private static string ExpandCues(string html, int? articleNumber)
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

            // Section cues get a stable anchor id so cross-references can deep-link straight to
            // them, but only when we know which article they belong to. Objection/reply ids carry
            // the objection number; contra/respondeo are unique within an article so need no number.
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
