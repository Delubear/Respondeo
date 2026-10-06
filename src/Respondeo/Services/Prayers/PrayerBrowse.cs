using Respondeo.Content.Contracts;

namespace Respondeo.Services.Prayers;

/// <summary>
/// Pure, stateless browse logic for the prayer treasury: category formatting, facet derivation, and the combined search/filter/sort.
/// Kept out of <see cref="Pages.Prayers"/> so it can be unit-tested, mirroring <see cref="SummaSearch"/>.
/// </summary>
public sealed class PrayerBrowse
{
    /// <summary>The neutral bucket that carries no pill and is never offered as a filterable facet.</summary>
    private const string OtherCategory = "other";

    /// <summary>
    /// Turns a category slug (e.g. "marian") into a display label (e.g. "Marian"), or null for the blank / "other" bucket so those prayers simply carry no pill.
    /// </summary>
    public string? FormatCategory(string category) => string.IsNullOrWhiteSpace(category) || category == OtherCategory ? null : char.ToUpperInvariant(category[0]) + category[1..];

    /// <summary>
    /// The distinct, alphabetically ordered category slugs that actually occur in the index, excluding the neutral "other" bucket so only meaningful facets are offered.
    /// </summary>
    public IReadOnlyList<string> Categories(IEnumerable<PrayerSummary> prayers) =>
        [.. prayers.Select(p => p.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c) && c != OtherCategory)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>The display labels for <see cref="Categories"/>, in the same order.</summary>
    public IReadOnlyList<string> CategoryLabels(IEnumerable<string> categories) => [.. categories.Select(c => FormatCategory(c) ?? c)];

    /// <summary>The distinct, alphabetically ordered tags that occur across the index.</summary>
    public IReadOnlyList<string> Tags(IEnumerable<PrayerSummary> prayers) =>
        [.. prayers.SelectMany(p => p.Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>
    /// Applies the active facets, free-text query, and sort to the index.
    /// Categories are selected by their display label (matched back to the slug); tags OR within the facet and AND across facets;
    /// the query matches title, Latin title, summary, tags, and category label.
    /// </summary>
    public IReadOnlyList<PrayerSummary> Filter(
        IReadOnlyList<PrayerSummary> prayers,
        string query,
        IReadOnlySet<string> selectedCategories,
        IReadOnlySet<string> selectedTags,
        SortOption<PrayerSummary> sort)
    {
        IEnumerable<PrayerSummary> results = prayers;

        // Categories are chosen by their display label; match back to the underlying slug.
        if (selectedCategories.Count > 0)
        {
            results = results.Where(p => selectedCategories.Contains(FormatCategory(p.Category) ?? p.Category));
        }

        // Tags: OR within the facet (match any selected tag), consistent with the articles browse.
        if (selectedTags.Count > 0)
        {
            results = results.Where(p => p.Tags.Any(t => selectedTags.Contains(t)));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            results = results.Where(p =>
                p.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (p.LatinTitle?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || p.Summary.Contains(q, StringComparison.OrdinalIgnoreCase)
                || p.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase))
                || (FormatCategory(p.Category)?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return [.. sort.Apply(results)];
    }
}
