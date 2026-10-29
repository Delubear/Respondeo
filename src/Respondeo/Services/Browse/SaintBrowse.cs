using Respondeo.Content.Contracts;

namespace Respondeo.Services.Browse;

/// <summary>
/// Pure, stateless browse logic for the saints catalog: facet-option derivation, the combined facet/search/sort, and the card presentation helpers (pills and summary).
/// All of it depends only on the entries plus the loaded <see cref="SaintFacetCatalog"/>,
/// so it is kept out of <see cref="Pages.Saints"/> and unit-testable, mirroring <see cref="MiracleBrowse"/>.
/// </summary>
public sealed class SaintBrowse
{
    // The historical order of the era facet, oldest first.
    // Era slugs are a small fixed vocabulary (see saints/facets.json), so chronological sorting ranks by this order; any unknown slug sorts last.
    // This is the single source of era chronology used by the "Chronological" sort options.
    private static readonly string[] _eraOrder = ["old-testament", "early-church", "medieval", "early-modern", "modern"];

    /// <summary>
    /// The chronological rank of an era slug (0 = oldest). Unmapped slugs rank after all known eras so they sort to the end, keeping the ordering deterministic.
    /// </summary>
    public static int EraRank(string? era)
    {
        var index = Array.FindIndex(_eraOrder, e => string.Equals(e, era?.Trim(), StringComparison.OrdinalIgnoreCase));
        return index < 0 ? _eraOrder.Length : index;
    }

    public static readonly Facet<SaintIndexEntry> EraFacet = Facet<SaintIndexEntry>.Single("era", s => s.Era);
    public static readonly Facet<SaintIndexEntry> RegionFacet = Facet<SaintIndexEntry>.Single("region", s => s.Region);
    public static readonly Facet<SaintIndexEntry> StateOfLifeFacet = Facet<SaintIndexEntry>.Multi("state", s => s.StatesOfLife);
    public static readonly Facet<SaintIndexEntry> DesignationFacet = Facet<SaintIndexEntry>.Multi("designation", s => s.Designations);
    public static readonly Facet<SaintIndexEntry> SexFacet = Facet<SaintIndexEntry>.Single("sex", s => s.Sex);
    public static readonly Facet<SaintIndexEntry> ReligiousOrderFacet = Facet<SaintIndexEntry>.Multi("order", s => s.ReligiousOrders);
    public static readonly Facet<SaintIndexEntry> CanonizationFacet = Facet<SaintIndexEntry>.Multi("canonization", s => s.Canonizations);

    /// <summary>Every filterable saint facet, in rail order. Keys double as shareable-link query names and remembered-state keys.</summary>
    public static readonly IReadOnlyList<Facet<SaintIndexEntry>> AllFacets =
        [EraFacet, RegionFacet, StateOfLifeFacet, DesignationFacet, SexFacet, ReligiousOrderFacet, CanonizationFacet];

    /// <summary>The display-label lookup for a facet key.</summary>
    public static Func<string, string> Label(SaintFacetCatalog facets, string key) => key switch
    {
        _ when key == EraFacet.Key => facets.Era,
        _ when key == RegionFacet.Key => facets.Region,
        _ when key == StateOfLifeFacet.Key => facets.StateOfLife,
        _ when key == DesignationFacet.Key => facets.Designation,
        _ when key == SexFacet.Key => facets.Sex,
        _ when key == ReligiousOrderFacet.Key => facets.ReligiousOrder,
        _ when key == CanonizationFacet.Key => facets.Canonization,
        _ => v => v,
    };

    /// <summary>The distinct values of a facet that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> Options(IEnumerable<SaintIndexEntry> entries, SaintFacetCatalog facets, Facet<SaintIndexEntry> facet) =>
        FacetFilter.Options(entries, facet, Label(facets, facet.Key));

    /// <summary>The distinct era slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> Eras(IEnumerable<SaintIndexEntry> entries, SaintFacetCatalog facets) => Options(entries, facets, EraFacet);

    /// <summary>The distinct region slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> Regions(IEnumerable<SaintIndexEntry> entries, SaintFacetCatalog facets) => Options(entries, facets, RegionFacet);

    /// <summary>The distinct state-of-life slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> StatesOfLife(IEnumerable<SaintIndexEntry> entries, SaintFacetCatalog facets) => Options(entries, facets, StateOfLifeFacet);

    /// <summary>The distinct designation slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> Designations(IEnumerable<SaintIndexEntry> entries, SaintFacetCatalog facets) => Options(entries, facets, DesignationFacet);

    /// <summary>The distinct sex slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> Sexes(IEnumerable<SaintIndexEntry> entries, SaintFacetCatalog facets) => Options(entries, facets, SexFacet);

    /// <summary>The distinct religious-order slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> ReligiousOrders(IEnumerable<SaintIndexEntry> entries, SaintFacetCatalog facets) => Options(entries, facets, ReligiousOrderFacet);

    /// <summary>The distinct canonization slugs that occur in the catalog, ordered by display label.</summary>
    public IReadOnlyList<string> Canonizations(IEnumerable<SaintIndexEntry> entries, SaintFacetCatalog facets) => Options(entries, facets, CanonizationFacet);

    /// <summary>
    /// Applies the active facets, free-text query, and sort to the catalog.
    /// A saint matches when it satisfies every active facet group (intersection across groups, union within a group)
    /// and its title/summary/dates/tags/facet-labels contain the search query.
    /// Patronage is deliberately not a filter facet (it is high-cardinality and sparse), but it is still searched and shown on cards.
    /// </summary>
    public IReadOnlyList<SaintIndexEntry> Filter(
        IReadOnlyList<SaintIndexEntry> entries,
        SaintFacetCatalog facets,
        string query,
        IReadOnlySet<string> selectedEras,
        IReadOnlySet<string> selectedRegions,
        IReadOnlySet<string> selectedStatesOfLife,
        IReadOnlySet<string> selectedDesignations,
        IReadOnlySet<string> selectedSexes,
        IReadOnlySet<string> selectedReligiousOrders,
        IReadOnlySet<string> selectedCanonizations,
        SortOption<SaintIndexEntry> sort)
    {
        var byKey = new Dictionary<string, IReadOnlySet<string>>
        {
            [EraFacet.Key] = selectedEras,
            [RegionFacet.Key] = selectedRegions,
            [StateOfLifeFacet.Key] = selectedStatesOfLife,
            [DesignationFacet.Key] = selectedDesignations,
            [SexFacet.Key] = selectedSexes,
            [ReligiousOrderFacet.Key] = selectedReligiousOrders,
            [CanonizationFacet.Key] = selectedCanonizations,
        };

        return Filter(entries, facets, query, key => byKey[key], sort);
    }

    /// <summary>
    /// Applies every facet's selection (looked up by facet key), the free-text query, and the sort.
    /// When <paramref name="exceptKey"/> is given, that facet's own selection is ignored, which is what per-option counts need.
    /// </summary>
    public IReadOnlyList<SaintIndexEntry> Filter(
        IReadOnlyList<SaintIndexEntry> entries,
        SaintFacetCatalog facets,
        string query,
        Func<string, IReadOnlySet<string>> selected,
        SortOption<SaintIndexEntry> sort,
        string? exceptKey = null)
    {
        var term = query?.Trim() ?? string.Empty;
        var results = FacetFilter.Apply(entries, AllFacets, selected, exceptKey).Where(s => Matches(s, term, facets));
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
            || s.Designations.Select(facets.Designation).Any(v => v.Contains(query, StringComparison.OrdinalIgnoreCase))
            || s.ReligiousOrders.Select(facets.ReligiousOrder).Any(v => v.Contains(query, StringComparison.OrdinalIgnoreCase))
            || s.Canonizations.Select(facets.Canonization).Any(v => v.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The card summary line: the saint's summary followed by its canonization label(s), era, dates, and region (whichever are known), joined as a middot-separated facet trail.
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
    public IReadOnlyList<string> Pills(SaintIndexEntry s) => [.. s.Patronages.Where(p => !string.IsNullOrWhiteSpace(p))];
}
