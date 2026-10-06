using Respondeo.Content.Contracts;

namespace Respondeo.Services;

/// <summary>
/// A searchable, filterable row for a Discover article. <see cref="Pill"/> is a precomputed display-only kicker (the article's topic); <see cref="Href"/> is the precomputed card link.
/// </summary>
public sealed record ArticleRow(string Id, string Title, string SortValue, string Summary, string? Pill, IReadOnlyList<string> Tags, string Href);

/// <summary>
/// Pure, stateless browse logic for the Discover articles index: projecting summaries into display rows, tag-facet derivation, and the combined search/filter/sort.
/// Kept out of <see cref="Pages.Articles"/> so it can be unit-tested, mirroring <see cref="PrayerBrowse"/>.
/// </summary>
public sealed class ArticleBrowse
{
    /// <summary>The neutral topic bucket that carries no pill.</summary>
    private const string GeneralTopic = "general";

    /// <summary>
    /// Projects the raw index into display rows (pre-resolving the pill and card href) ordered by title, so each keystroke filters a ready-to-render list.
    /// </summary>
    public IReadOnlyList<ArticleRow> BuildRows(IEnumerable<ArticleSummary> articles) =>
        [.. articles
            .Select(a => new ArticleRow(a.Id, a.Title, a.SortValue, a.Summary, FormatPill(a.Topic), a.Tags, ContentRoutes.ArticleHref(a.Id)))
            .OrderBy(a => a.SortValue, StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>The distinct, alphabetically ordered tags that occur across the rows.</summary>
    public IReadOnlyList<string> Tags(IEnumerable<ArticleRow> rows) =>
        [.. rows.SelectMany(a => a.Tags)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>
    /// Display-only pill for a Discover article: capitalizes the topic slug, or null for the neutral "general" / blank bucket so unlabelled articles simply carry no pill.
    /// </summary>
    public string? FormatPill(string topic) => string.IsNullOrWhiteSpace(topic) || topic == GeneralTopic ? null : char.ToUpperInvariant(topic[0]) + topic[1..];

    /// <summary>
    /// Applies the active tag facet, free-text query, and sort to the rows. Tags OR within the facet
    /// and AND across facets; the query matches title, summary, tags, and the pill.
    /// </summary>
    public IReadOnlyList<ArticleRow> Filter(IReadOnlyList<ArticleRow> rows, string query, IReadOnlySet<string> selectedTags, SortOption<ArticleRow> sort)
    {
        IEnumerable<ArticleRow> results = rows;

        // Tags: OR within the facet (match any selected tag), AND with the other facets.
        if (selectedTags.Count > 0)
        {
            results = results.Where(a => a.Tags.Any(t => selectedTags.Contains(t)));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            results = results.Where(a =>
                a.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || a.Summary.Contains(q, StringComparison.OrdinalIgnoreCase)
                || a.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase))
                || (a.Pill?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return [.. sort.Apply(results)];
    }
}
