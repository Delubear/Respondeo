using Respondeo.Content.Summa.Contracts;

namespace Respondeo.Services;

/// <summary>
/// A single searchable Summa entry (a question or one of its articles) with its link metadata pre-resolved,
/// so each keystroke scans a flat list instead of walking the nested index.
/// </summary>
public sealed record SummaSearchEntry(
    string PartTitle,
    string QuestionId,
    int QuestionNumber,
    string Title,
    int? ArticleNumber,
    string? Treatise);

/// <summary>The outcome of a search: the (capped) matches and whether the cap was hit.</summary>
public sealed record SummaSearchResult(IReadOnlyList<SummaSearchEntry> Matches, bool Truncated);

/// <summary>
/// Pure, stateless title/treatise search over the Summa index.
/// Flattening and matching are kept out of <see cref="Pages.Summa"/> so they can be unit-tested, mirroring <see cref="SummaQuestionPresenter"/>.
/// </summary>
public sealed class SummaSearch
{
    /// <summary>Cap on returned results so a broad query stays responsive and doesn't flood the DOM.</summary>
    public const int MaxResults = 50;

    /// <summary>
    /// Flattens the nested index into a single array of entries with pre-resolved link metadata.
    /// Prologues carry no articles and are part-level framing rather than searchable questions, so they are skipped.
    /// </summary>
    public SummaSearchEntry[] BuildEntries(SummaIndex index)
    {
        var entries = new List<SummaSearchEntry>();
        foreach (var part in index.Parts)
        {
            foreach (var question in part.Questions)
            {
                if (question.IsPrologue)
                {
                    continue;
                }

                entries.Add(new SummaSearchEntry(part.Title, question.Id, question.Number, question.Title, null, question.Treatise));

                foreach (var article in question.Articles)
                {
                    entries.Add(new SummaSearchEntry(part.Title, question.Id, question.Number, article.Title, article.Number, question.Treatise));
                }
            }
        }

        return [.. entries];
    }

    /// <summary>
    /// Title/treatise search over the pre-flattened entries: matches question titles and article ("Whether ...?") titles,
    /// plus the owning treatise name. Results stop accumulating once <paramref name="maxResults"/> is reached, flagging the result as truncated.
    /// </summary>
    public SummaSearchResult Search(IReadOnlyList<SummaSearchEntry> entries, string query, int maxResults = MaxResults)
    {
        var term = query.Trim();
        if (term.Length == 0)
        {
            return new SummaSearchResult(Array.Empty<SummaSearchEntry>(), false);
        }

        var matches = new List<SummaSearchEntry>();
        var truncated = false;
        foreach (var entry in entries)
        {
            if (!entry.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                && !(entry.Treatise?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                continue;
            }

            if (matches.Count >= maxResults)
            {
                truncated = true;
                break;
            }

            matches.Add(entry);
        }

        return new SummaSearchResult(matches, truncated);
    }
}
