using Respondeo.Components;
using Respondeo.Content.Contracts;

namespace Respondeo.Services;

/// <summary>
/// Pure, stateless browse logic for the saints catalog: facet-option derivation, the combined
/// facet/search/sort, and the card presentation helpers (pills and summary). All of it depends only
/// on the entries plus the loaded <see cref="SaintFacetCatalog"/>, so it is kept out of
/// <see cref="Pages.Saints"/> and unit-testable, mirroring <see cref="MiracleBrowse"/>.
/// </summary>
public sealed class SaintBrowse
{
    /// <summary>The distinct era slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> Eras(IEnumerable<SaintIndexEntry> entries, SaintFacetCatalog facets) =>
        entries.Select(s => s.Era)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(facets.Era, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>The distinct region slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> Regions(IEnumerable<SaintIndexEntry> entries, SaintFacetCatalog facets) =>
        entries.Select(s => s.Region)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(facets.Region, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>The distinct state-of-life slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> StatesOfLife(IEnumerable<SaintIndexEntry> entries, SaintFacetCatalog facets) =>
        entries.SelectMany(s => s.StatesOfLife)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(facets.StateOfLife, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>The distinct canonization slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> Canonizations(IEnumerable<SaintIndexEntry> entries, SaintFacetCatalog facets) =>
        entries.SelectMany(s => s.Canonizations)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(facets.Canonization, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>
    /// Applies the active facets, free-text query, and sort to the catalog. A saint matches when it
    /// satisfies every active facet group (intersection across groups, union within a group) and its
    /// title/summary/dates/tags/facet-labels contain the search query. Patronage is deliberately not a
    /// filter facet (it is high-cardinality and sparse), but it is still searched and shown on cards.
    /// </summary>
    public IReadOnlyList<SaintIndexEntry> Filter(
        IReadOnlyList<SaintIndexEntry> entries,
        SaintFacetCatalog facets,
        string query,
        IReadOnlySet<string> selectedEras,
        IReadOnlySet<string> selectedRegions,
        IReadOnlySet<string> selectedStatesOfLife,
        IReadOnlySet<string> selectedCanonizations,
        SortOption<SaintIndexEntry> sort)
    {
        var term = query?.Trim() ?? string.Empty;

        var results = entries.Where(s =>
            (selectedEras.Count == 0 || selectedEras.Contains(s.Era))
            && (selectedRegions.Count == 0 || selectedRegions.Contains(s.Region))
            && (selectedStatesOfLife.Count == 0 || s.StatesOfLife.Any(selectedStatesOfLife.Contains))
            && (selectedCanonizations.Count == 0 || s.Canonizations.Any(selectedCanonizations.Contains))
            && Matches(s, term, facets));

        return [.. sort.Apply(results)];
    }

    private static bool Matches(SaintIndexEntry s, string query, SaintFacetCatalog facets)
    {
        if (query.Length == 0)
        {
            return true;
        }

        return s.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || s.Summary.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (s.Dates?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || s.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))
            || s.Patronages.Any(p => p.Contains(query, StringComparison.OrdinalIgnoreCase))
            || facets.Era(s.Era).Contains(query, StringComparison.OrdinalIgnoreCase)
            || facets.Region(s.Region).Contains(query, StringComparison.OrdinalIgnoreCase)
            || s.StatesOfLife.Select(facets.StateOfLife).Any(v => v.Contains(query, StringComparison.OrdinalIgnoreCase))
            || s.Canonizations.Select(facets.Canonization).Any(v => v.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The card summary line: the saint's summary followed by its canonization label(s), era, dates, and
    /// region (whichever are known), joined as a middot-separated facet trail.
    /// </summary>
    public string CardSummary(SaintIndexEntry s, SaintFacetCatalog facets)
    {
        var trail = new List<string>();
        trail.AddRange(s.Canonizations.Select(facets.Canonization));
        trail.Add(facets.Era(s.Era));
        if (!string.IsNullOrWhiteSpace(s.Dates))
        {
            trail.Add(s.Dates!);
        }

        if (!string.Equals(s.Region, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            trail.Add(facets.Region(s.Region));
        }

        return $"{s.Summary}  \u2014  {string.Join(" \u00b7 ", trail)}";
    }

    /// <summary>One pill per patronage, mirroring the category/kind pill on the Discover lists.</summary>
    public IReadOnlyList<string> Pills(SaintIndexEntry s, SaintFacetCatalog facets) =>
        [.. s.Patronages.Where(p => !string.IsNullOrWhiteSpace(p))];
}
