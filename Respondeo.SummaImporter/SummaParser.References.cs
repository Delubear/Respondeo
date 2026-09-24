using System.Text.RegularExpressions;

namespace Respondeo.SummaImporter;

// Turns CCEL cross-references and objection citations in body/prologue text into neutral,
// render-agnostic placeholder tokens so link format, routing and styling stay in the app's render
// stage and never require regenerating the JSON.
internal static partial class SummaParser
{
    // CCEL hyperlink-index cruft like "[6]" that directly precedes a reference token (e.g. "[6]Q[2]",
    // "[991]FP" or the grouped link-table form "[476](Q[51], A[1])"). These are leftover anchor
    // numbers from the source's link table and carry no reading meaning, so they are removed. The
    // lookahead keeps meaningful "Q[2]"/"A[2]" brackets (which are followed by punctuation or
    // whitespace, never a letter or "(") intact.
    [GeneratedRegex(@"\[\d+\](?=[A-Za-z(])", RegexOptions.Compiled)]
    private static partial Regex CrossRefCruftRegex();

    // A cross-reference to another question, e.g. "Q[2], A[2]" (same part) or "FP, Q[22], A[2]"
    // (explicit part). Captures the optional part token, the question number, the first article
    // number, and any additional article numbers from a multi-article citation like "AA[1],3"
    // (which cites articles 1 and 3). An optional trailing objection citation like ", OBJ[3]" or
    // ", Reply to OBJ[1]" is captured too. Run after the hyperlink cruft has been stripped.
    [GeneratedRegex(@"(?:(?<part>FP|FS|SS|TP|XP),\s*)?Q\[(?<q>\d+)\](?:\s*,\s*A{1,2}\[(?<a>\d+)\](?<am>(?:\s*,\s*\d+)*))?(?:\s*,\s*(?<reply>Reply(?:\s+to)?\s+)?OBJ\[(?<obj>\d+)\])?", RegexOptions.Compiled)]
    private static partial Regex QuestionRefRegex();

    // A same-question article reference that stands alone (no preceding "Q[...]"), e.g. "AA[1],3".
    // Captures the first article number, any additional article numbers, and an optional trailing
    // objection citation like ", OBJ[3]" or ", Reply to OBJ[1]".
    [GeneratedRegex(@"A{1,2}\[(?<a>\d+)\](?<am>(?:\s*,\s*\d+)*)(?:\s*,\s*(?<reply>Reply(?:\s+to)?\s+)?OBJ\[(?<obj>\d+)\])?", RegexOptions.Compiled)]
    private static partial Regex ArticleRefRegex();

    // Pulls each additional article number out of a multi-article tail like ",3" or ", 11, 12".
    [GeneratedRegex(@",\s*(?<n>\d+)", RegexOptions.Compiled)]
    private static partial Regex ExtraArticleRegex();

    // A standalone objection self-reference that survives the question/article passes, e.g.
    // "(cf. OBJ[3])" or "(Reply OBJ[1])". These point at an objection within the current article.
    [GeneratedRegex(@"(?<reply>Reply(?:\s+to)?\s+)?OBJ\[(?<obj>\d+)\]", RegexOptions.Compiled)]
    private static partial Regex StandaloneObjectionRegex();

    // Maps a CCEL part token (FP/FS/SS/TP/XP) to our internal part id (fp/fs/ss/tp/xp).
    private static string PartTokenToId(string token) => token.ToLowerInvariant();

    // Turns CCEL cross-references in body/prologue text into neutral, render-agnostic placeholder
    // tokens rather than baking final HTML anchors into the corpus. This keeps link format, routing
    // and styling decisions in the app's render stage, so changing them never requires regenerating
    // the JSON. First the leftover hyperlink-index cruft (e.g. "[6]") is removed, then references of
    // the form "Q[2], A[2]", "FP, Q[22], A[2]", or a lone "AA[1],3" become tokens:
    //   {{sref|<kind>|<partId>|<q>|<a>}}
    // where <kind> is "qp" (question, show part), "q" (question, same part) or "a" (article only),
    // and <a> may be empty. The tokens survive Markdig untouched and are expanded on the page.
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
            var kind = hasPart ? "qp" : "q";
            var reference = $"{{{{sref|{kind}|{partId}|{questionNumber}|{articles}}}}}";
            return reference + Objection(match, partId, questionNumber, match.Groups["a"]);
        });

        text = ArticleRefRegex().Replace(text, match =>
        {
            var articles = JoinArticles(match.Groups["a"], match.Groups["am"]);
            var reference = $"{{{{sref|a|{currentPartId}|{currentNumber}|{articles}}}}}";
            return reference + Objection(match, currentPartId, currentNumber, match.Groups["a"]);
        });

        // Any remaining "OBJ[n]"/"Reply OBJ[n]" is a self-reference to an objection within the
        // current article (e.g. "as stated above (cf. OBJ[3])"). It can only be anchored when the
        // article number is known, so this pass is skipped for prologues (currentArticle == 0).
        if (currentArticle > 0)
        {
            text = StandaloneObjectionRegex().Replace(text, match =>
            {
                var kind = match.Groups["reply"].Success ? "reply" : "objection";
                var objectionNumber = match.Groups["obj"].Value;
                return $"{{{{sobj|{currentPartId}|{currentNumber}|{currentArticle}|{kind}|{objectionNumber}}}}}";
            });
        }

        return text;
    }

    // Builds the companion objection token for a reference that carries a trailing "OBJ[n]" (or
    // "Reply to OBJ[n]") citation. The objection is scoped to the article named in the same
    // reference; when no article is present there is nothing to anchor to, so the token is omitted.
    private static string Objection(Match match, string partId, int questionNumber, Group article)
    {
        if (!match.Groups["obj"].Success || !article.Success)
        {
            return string.Empty;
        }

        var kind = match.Groups["reply"].Success ? "reply" : "objection";
        var objectionNumber = match.Groups["obj"].Value;
        return $"{{{{sobj|{partId}|{questionNumber}|{article.Value}|{kind}|{objectionNumber}}}}}";
    }

    // Builds a comma-separated article list from the first article number plus any additional
    // numbers captured from a multi-article citation (e.g. "AA[1],3" -> "1,3"). Returns an empty
    // string when there is no article at all.
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
