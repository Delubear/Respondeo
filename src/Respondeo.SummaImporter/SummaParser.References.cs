using System.Text.RegularExpressions;

namespace Respondeo.SummaImporter;

// Turns CCEL cross-references and objection citations in body/prologue text into neutral,
// render-agnostic placeholder tokens so link format, routing and styling stay in the app's render stage and never require regenerating the JSON.
internal static partial class SummaParser
{
    // CCEL hyperlink-index cruft like "[6]" that precedes a reference token (e.g. "[6]Q[2]", "[991]FP" or the grouped link-table form "[476](Q[51], A[1])").
    // These are leftover anchor numbers from the source's link table and carry no reading meaning, so they are removed.
    // Two shapes are stripped:
    //   1. A bracket number immediately followed by a reference letter or "(", e.g. "[1433]Q[28]".
    //   2. A bracket number sitting right after a "(" or ";" list separator, even when a space follows before the
    //      reference, e.g. "A[5];[1434] Q[37]". The lookbehind keeps this from touching meaningful "Q[2]"/"A[2]"
    //      brackets (which are preceded by a letter, and are followed by punctuation or whitespace, never a letter or "(").
    [GeneratedRegex(@"\[\d+\](?=[A-Za-z(])|(?<=[;(])\[\d+\](?=\s*[A-Za-z(])", RegexOptions.Compiled)]
    private static partial Regex CrossRefCruftRegex();

    // A cross-reference to another question, e.g. "Q[2], A[2]" (same part) or "FP, Q[22], A[2]" (explicit part).
    // Captures the optional part token, the question number, the first article number,
    // and any additional article numbers from a multi-article citation like "AA[1],3" (which cites articles 1 and 3).
    // An optional trailing objection citation like ", OBJ[3]", ", Reply to OBJ[1]" or the lower-case ", ad 2" form
    // (which cites the reply to objection 2) is captured too. Run after the hyperlink cruft has been stripped.
    [GeneratedRegex(@"(?:(?<part>FP|FS|SS|TP|XP),\s*)?Q\[(?<q>\d+)\](?:\s*,\s*A{1,2}\[(?<a>\d+)\](?<am>(?:\s*,\s*\d+)*))?(?:\s*,\s*(?:(?<reply>Reply(?:\s+to)?\s+)?OBJ\[(?<obj>\d+)\]|ad\s+(?<ad>\d+)))?", RegexOptions.Compiled)]
    private static partial Regex QuestionRefRegex();

    // A same-question article reference that stands alone (no preceding "Q[...]"), e.g. "AA[1],3".
    // Captures the first article number, any additional article numbers, and an optional trailing objection
    // citation like ", OBJ[3]", ", Reply to OBJ[1]" or the lower-case ", ad 2" reply form.
    [GeneratedRegex(@"A{1,2}\[(?<a>\d+)\](?<am>(?:\s*,\s*\d+)*)(?:\s*,\s*(?:(?<reply>Reply(?:\s+to)?\s+)?OBJ\[(?<obj>\d+)\]|ad\s+(?<ad>\d+)))?", RegexOptions.Compiled)]
    private static partial Regex ArticleRefRegex();

    // Pulls each additional article number out of a multi-article tail like ",3" or ", 11, 12".
    [GeneratedRegex(@",\s*(?<n>\d+)", RegexOptions.Compiled)]
    private static partial Regex ExtraArticleRegex();

    // A standalone objection self-reference that survives the question/article passes, e.g. "(cf. OBJ[3])" or "(Reply OBJ[1])".
    // These point at an objection within the current article.
    [GeneratedRegex(@"(?<reply>Reply(?:\s+to)?\s+)?OBJ\[(?<obj>\d+)\]", RegexOptions.Compiled)]
    private static partial Regex StandaloneObjectionRegex();

    // A bare parenthesised reply self-reference, e.g. "(ad 1)", citing the reply to objection n in the
    // current article. Restricted to the "(ad n)" shape so it never matches Latin prose containing "ad".
    // Only the "ad n" span is captured; the surrounding parentheses are preserved by the lookbehind/lookahead.
    [GeneratedRegex(@"(?<=\()ad\s+(?<ad>\d+)(?=\))", RegexOptions.Compiled)]
    private static partial Regex SelfReplyRegex();

    // Maps a CCEL part token (FP/FS/SS/TP/XP) to our internal part id (fp/fs/ss/tp/xp).
    // Maps a CCEL part token (FP/FS/SS/TP/XP) to our internal stable storage key (p1/p2a/p2b/p3/sup).
    private static string PartTokenToId(string token) => token.ToUpperInvariant() switch
    {
        "FP" => "p1",
        "FS" => "p2a",
        "SS" => "p2b",
        "TP" => "p3",
        "XP" => "sup",
        _ => token.ToLowerInvariant(),
    };

    // Turns CCEL cross-references in body/prologue text into neutral, render-agnostic placeholder tokens rather than baking final HTML anchors into the corpus.
    // This keeps link format, routing and styling decisions in the app's render stage, so changing them never requires regenerating the JSON.
    // First the leftover hyperlink-index cruft (e.g. "[6]") is removed, then references of the form "Q[2], A[2]", "FP, Q[22], A[2]", or a lone "AA[1],3" become tokens:
    //   {{sref|<kind>|<partId>|<q>|<a>}}
    // where <kind> is "qp" (question, show part), "q" (question, same part) or "a" (article only), and <a> may be empty.
    // The tokens survive Markdig untouched and are expanded on the page.
    private static string Linkify(string text, string currentPartId, int currentNumber, int currentArticle = 0)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        text = CrossRefCruftRegex().Replace(text, string.Empty);

        text = QuestionRefRegex().Replace(text, match =>
        {
            var hasPart = match.Groups["part"].Success;
            var partId = hasPart ? PartTokenToId(match.Groups["part"].Value) : currentPartId;
            var questionNumber = int.Parse(match.Groups["q"].Value);
            var articles = JoinArticles(match.Groups["a"], match.Groups["am"]);

            // A single-article reference that also cites an objection/reply collapses to one deep link
            // straight to that objection/reply, instead of an article link followed by a separate one.
            var combined = TryBuildCombined(match, partId, questionNumber, questionShown: true, hasPart);
            if (combined is not null)
            {
                return combined;
            }

            var kind = hasPart ? "qp" : "q";
            var reference = $"{{{{sref|{kind}|{partId}|{questionNumber}|{articles}}}}}";
            return reference + Objection(match, partId, questionNumber, match.Groups["a"]);
        });

        text = ArticleRefRegex().Replace(text, match =>
        {
            var articles = JoinArticles(match.Groups["a"], match.Groups["am"]);

            var combined = TryBuildCombined(match, currentPartId, currentNumber, questionShown: false, hasPart: false);
            if (combined is not null)
            {
                return combined;
            }

            var reference = $"{{{{sref|a|{currentPartId}|{currentNumber}|{articles}}}}}";
            return reference + Objection(match, currentPartId, currentNumber, match.Groups["a"]);
        });

        // Any remaining "OBJ[n]"/"Reply OBJ[n]" is a self-reference to an objection within the current article (e.g. "as stated above (cf. OBJ[3])").
        // It can only be anchored when the article number is known, so this pass is skipped for prologues (currentArticle == 0).
        if (currentArticle > 0)
        {
            text = StandaloneObjectionRegex().Replace(text, match =>
            {
                var kind = match.Groups["reply"].Success ? "reply" : "objection";
                var objectionNumber = match.Groups["obj"].Value;
                return $"{{{{sobj|{currentPartId}|{currentNumber}|{currentArticle}|{kind}|{objectionNumber}}}}}";
            });

            // A bare "(ad n)" cites the reply to objection n within the current article, e.g.
            // "As stated above (ad 1), ...". Rendered as a stand-alone deep link to that reply.
            text = SelfReplyRegex().Replace(text, match =>
                $"{{{{scite|self|{currentPartId}|{currentNumber}|{currentArticle}|reply|{match.Groups["ad"].Value}}}}}");
        }

        return text;
    }

    // Builds a single combined citation token when a reference points at exactly one article (or a
    // single-article question where the article is implied) and carries an objection/reply citation.
    // The whole "Q. 13, A. 1, ad 2" style phrase then renders as one deep link straight to the
    // objection/reply, instead of an article link followed by a separate objection link. Returns null
    // when the citation has no objection/reply, or when multiple articles are cited (which keeps the
    // existing per-article link behaviour so no link is dropped).
    //   {{scite|<labelKind>|<partId>|<q>|<article>|<kind>|<n>}}
    // where <labelKind> is "qp" (show part + Q + A), "q" (Q + A) or "a" (article only).
    private static string? TryBuildCombined(Match match, string partId, int questionNumber, bool questionShown, bool hasPart)
    {
        var isAd = match.Groups["ad"].Success;
        if (!match.Groups["obj"].Success && !isAd)
        {
            return null;
        }

        // Multiple articles cited: keep the existing separate-link rendering.
        if (match.Groups["am"].Success && match.Groups["am"].Value.Length > 0)
        {
            return null;
        }

        // An article number is required to anchor the objection/reply. CCEL omits it only for
        // single-article questions, where the target is article 1.
        var article = match.Groups["a"].Success ? match.Groups["a"].Value : "1";

        var kind = isAd || match.Groups["reply"].Success ? "reply" : "objection";
        var number = isAd ? match.Groups["ad"].Value : match.Groups["obj"].Value;

        var labelKind = !questionShown ? "a" : hasPart ? "qp" : "q";
        return $"{{{{scite|{labelKind}|{partId}|{questionNumber}|{article}|{kind}|{number}}}}}";
    }

    // Builds the companion objection token for a reference that carries a trailing "OBJ[n]" (or "Reply to OBJ[n]")
    // citation, or the lower-case "ad n" form (which always cites the reply to objection n). Used only for the
    // multi-article fallback path now that single-article citations collapse into a combined scite token.
    // The objection is scoped to the article named in the same reference. When no article is present there is no anchor to
    // link to, so the citation is preserved as readable text rather than being silently dropped from the sentence.
    private static string Objection(Match match, string partId, int questionNumber, Group article)
    {
        var isAd = match.Groups["ad"].Success;
        if (!match.Groups["obj"].Success && !isAd)
        {
            return string.Empty;
        }

        var kind = isAd || match.Groups["reply"].Success ? "reply" : "objection";
        var objectionNumber = isAd ? match.Groups["ad"].Value : match.Groups["obj"].Value;

        if (!article.Success)
        {
            return kind == "reply" ? $", Reply to Obj. {objectionNumber}" : $", Obj. {objectionNumber}";
        }

        return $"{{{{sobj|{partId}|{questionNumber}|{article.Value}|{kind}|{objectionNumber}}}}}";
    }

    // Builds a comma-separated article list from the first article number plus any additional numbers captured from a multi-article citation
    // (e.g. "AA[1],3" -> "1,3"). Returns an empty string when there is no article at all.
    private static string JoinArticles(Group first, Group additional)
    {
        if (!first.Success)
        {
            return string.Empty;
        }

        var numbers = new List<string> { first.Value };
        if (additional.Success && additional.Value.Length > 0)
        {
            foreach (Match extra in ExtraArticleRegex().Matches(additional.Value))
            {
                numbers.Add(extra.Groups["n"].Value);
            }
        }

        return string.Join(',', numbers);
    }
}
