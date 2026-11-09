using Respondeo.Content.Contracts;

namespace Respondeo.Services.Prayers;

/// <summary>
/// Pure, stateless browse logic for the prayer treasury: prayed-at formatting, facet derivation, and the combined search/filter/sort.
/// Kept out of <see cref="Pages.Prayers"/> so it can be unit-tested, mirroring <see cref="SummaSearch"/>.
/// </summary>
public sealed class PrayerBrowse
{
    /// <summary>Prayed-at occasions are selected by their display label, so the facet's values are labels.</summary>
    public static readonly Facet<PrayerSummary> PrayedAtFacet = Facet<PrayerSummary>.Multi("prayedAt", p => p.PrayedAt);
    public static readonly Facet<PrayerSummary> TagFacet = Facet<PrayerSummary>.Multi("tags", p => p.Tags);

    /// <summary>Every filterable prayer facet, in rail order.</summary>
    public static readonly IReadOnlyList<Facet<PrayerSummary>> AllFacets = [PrayedAtFacet, TagFacet];

    /// <summary>The slug-to-label vocabulary from <c>prayers/facets.json</c>; set once the page has loaded it.</summary>
    public PrayerFacetCatalog Facets { get; set; } = PrayerFacetCatalog.Empty;

    /// <summary>The card pill label: the prayer's first prayed-at occasion, or null when it has none.</summary>
    public string? PrimaryPrayedAt(PrayerSummary prayer) => prayer.PrayedAt.Count > 0 ? Facets.PrayedAtLabel(prayer.PrayedAt[0]) : null;

    /// <summary>The distinct, alphabetically ordered tags that occur across the index.</summary>
    public IReadOnlyList<string> Tags(IEnumerable<PrayerSummary> prayers) => FacetFilter.Options(prayers, TagFacet, t => t);

    /// <summary>The display-label lookup for the facet with the given key.</summary>
    public Func<string, string> Label(string key) => key == TagFacet.Key ? Facets.TagLabel : Facets.PrayedAtLabel;

    /// <summary>The distinct option slugs of <paramref name="facet"/> that occur in the index, ordered by display label.</summary>
    public IReadOnlyList<string> Options(IEnumerable<PrayerSummary> prayers, Facet<PrayerSummary> facet) => FacetFilter.Options(prayers, facet, Label(facet.Key));

    /// <summary>
    /// Applies the active facets, free-text query, and sort to the index.
    /// Prayed-at occasions and tags are selected by slug; values OR within a facet and AND across facets;
    /// the query matches title, Latin title, summary, tags, and prayed-at labels.
    /// </summary>
    public IReadOnlyList<PrayerSummary> Filter(
        IReadOnlyList<PrayerSummary> prayers,
        string query,
        IReadOnlySet<string> selectedPrayedAt,
        IReadOnlySet<string> selectedTags,
        SortOption<PrayerSummary> sort) =>
        Filter(prayers, query, key => key == PrayedAtFacet.Key ? selectedPrayedAt : selectedTags, sort);

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
                || p.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase) || Facets.TagLabel(t).Contains(q, StringComparison.OrdinalIgnoreCase))
                || p.PrayedAt.Any(s => Facets.PrayedAtLabel(s).Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        return [.. sort.Apply(results)];
    }
}
