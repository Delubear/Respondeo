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

    /// <summary>Categories are selected by their display label, so the facet's values are labels (null for the neutral bucket).</summary>
    public static readonly Facet<PrayerSummary> CategoryFacet = Facet<PrayerSummary>.Single("category", p => Format(p.Category));
    public static readonly Facet<PrayerSummary> TagFacet = Facet<PrayerSummary>.Multi("tags", p => p.Tags);

    /// <summary>Every filterable prayer facet, in rail order.</summary>
    public static readonly IReadOnlyList<Facet<PrayerSummary>> AllFacets = [CategoryFacet, TagFacet];

    /// <summary>
    /// Turns a category slug (e.g. "marian") into a display label (e.g. "Marian"), or null for the blank / "other" bucket so those prayers simply carry no pill.
    /// </summary>
    public string? FormatCategory(string category) => Format(category);

    private static string? Format(string category) =>
        string.IsNullOrWhiteSpace(category) || category == OtherCategory ? null : char.ToUpperInvariant(category[0]) + category[1..];

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
    public IReadOnlyList<string> Tags(IEnumerable<PrayerSummary> prayers) => FacetFilter.Options(prayers, TagFacet, t => t);

    /// <summary>The display-label lookup for the facet with the given key (category values are already labels).</summary>
    public static Func<string, string> Label(string key) => key == TagFacet.Key ? TagLabel.Format : c => c;

    /// <summary>The distinct option values of <paramref name="facet"/> that occur in the index, ordered by value.</summary>
    public IReadOnlyList<string> Options(IEnumerable<PrayerSummary> prayers, Facet<PrayerSummary> facet) => FacetFilter.Options(prayers, facet, v => v);

    /// <summary>
    /// Applies the active facets, free-text query, and sort to the index.
    /// Categories are selected by their display label; tags OR within the facet and AND across facets;
    /// the query matches title, Latin title, summary, tags, and category label.
    /// </summary>
    public IReadOnlyList<PrayerSummary> Filter(
        IReadOnlyList<PrayerSummary> prayers,
        string query,
        IReadOnlySet<string> selectedCategories,
        IReadOnlySet<string> selectedTags,
        SortOption<PrayerSummary> sort) =>
        Filter(prayers, query, key => key == CategoryFacet.Key ? selectedCategories : selectedTags, sort);

    /// <summary>Applies the facet selections (looked up by facet key), query, and sort; <paramref name="exceptKey"/> skips one facet so its option counts can be computed.</summary>
    public IReadOnlyList<PrayerSummary> Filter(
        IReadOnlyList<PrayerSummary> prayers,
        string query,
        Func<string, IReadOnlySet<string>> selected,
        SortOption<PrayerSummary> sort,
        string? exceptKey = null)
    {
        var results = FacetFilter.Apply(prayers, AllFacets, selected, exceptKey);

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
