using Respondeo.Content.Contracts;

namespace Respondeo.Services;

/// <summary>
/// Pure, stateless browse logic for the miracles catalog: facet-option derivation, the combined facet/search/sort, and the card presentation helpers (pills and summary).
/// All of it depends only on the entries plus the loaded <see cref="MiracleFacetCatalog"/>,
/// so it is kept out of <see cref="Pages.Miracles"/> and unit-testable, mirroring <see cref="PrayerBrowse"/>.
/// </summary>
public sealed class MiracleBrowse
{
    /// <summary>The distinct category (kind) slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> Types(IEnumerable<MiracleIndexEntry> entries, MiracleFacetCatalog facets) =>
        [.. entries.SelectMany(m => m.Types)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(facets.Category, StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>The distinct approval slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> Approvals(IEnumerable<MiracleIndexEntry> entries, MiracleFacetCatalog facets) =>
        [.. entries.Select(m => m.Approval)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(facets.Approval, StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>The distinct region slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> Regions(IEnumerable<MiracleIndexEntry> entries, MiracleFacetCatalog facets) =>
        [.. entries.Select(m => m.Region)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(facets.Region, StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>
    /// Applies the active facets, free-text query, and sort to the catalog.
    /// A miracle matches when it satisfies every active facet group (intersection across groups, union within a group)
    /// and its title/summary/country/tags/facet-labels contain the search query.
    /// </summary>
    public IReadOnlyList<MiracleIndexEntry> Filter(
        IReadOnlyList<MiracleIndexEntry> entries,
        MiracleFacetCatalog facets,
        string query,
        IReadOnlySet<string> selectedTypes,
        IReadOnlySet<string> selectedApprovals,
        IReadOnlySet<string> selectedRegions,
        SortOption<MiracleIndexEntry> sort)
    {
        var term = query?.Trim() ?? string.Empty;

        var results = entries.Where(m =>
            (selectedTypes.Count == 0 || m.Types.Any(selectedTypes.Contains))
            && (selectedApprovals.Count == 0 || selectedApprovals.Contains(m.Approval))
            && (selectedRegions.Count == 0 || selectedRegions.Contains(m.Region))
            && Matches(m, term, facets));

        return [.. sort.Apply(results)];
    }

    private static bool Matches(MiracleIndexEntry m, string query, MiracleFacetCatalog facets)
    {
        if (query.Length == 0)
        {
            return true;
        }

        return m.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || m.Summary.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (m.Country?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || m.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase))
            || m.Types.Select(facets.Category).Any(k => k.Contains(query, StringComparison.OrdinalIgnoreCase))
            || facets.Approval(m.Approval).Contains(query, StringComparison.OrdinalIgnoreCase)
            || facets.Region(m.Region).Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The card summary line: the miracle's summary followed by its approval label, century, and country (whichever are known), joined as a middot-separated facet trail.
    /// </summary>
    public string CardSummary(MiracleIndexEntry m, MiracleFacetCatalog facets)
    {
        var trail = new List<string> { facets.Approval(m.Approval) };
        var century = MiracleFacets.CenturyLabel(m.Year);
        if (century is not null)
        {
            trail.Add(century);
        }

        if (!string.IsNullOrWhiteSpace(m.Country))
        {
            trail.Add(m.Country!);
        }

        return $"{m.Summary}  \u2014  {string.Join(" \u00b7 ", trail)}";
    }

    /// <summary>One pill per kind, mirroring the category/kind pill on the Discover lists.</summary>
    public IReadOnlyList<string> Pills(MiracleIndexEntry m, MiracleFacetCatalog facets) => [.. m.Types.Select(facets.Category).Where(k => !string.IsNullOrWhiteSpace(k))];
}
