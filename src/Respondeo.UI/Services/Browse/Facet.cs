namespace Respondeo.Services.Browse;

/// <summary>
/// One filterable facet of a browse catalog: a stable key (also its shareable-link query name and remembered-state key)
/// and the value(s) each row carries for it. Single-valued facets yield one value; multi-valued facets yield several.
/// </summary>
public sealed record Facet<T>(string Key, Func<T, IEnumerable<string?>> Values)
{
    public static Facet<T> Single(string key, Func<T, string?> value) => new(key, row => [value(row)]);

    public static Facet<T> Multi(string key, Func<T, IEnumerable<string?>> values) => new(key, values);

    /// <summary>A row passes when nothing is selected, or when any of its values is selected (union within a group).</summary>
    public bool Accepts(T row, IReadOnlySet<string> selected) => selected.Count == 0 || Values(row).Any(v => v is not null && selected.Contains(v));
}

/// <summary>
/// Shared facet logic for browse pages: intersection across facet groups, per-facet option derivation, and
/// "what would I get if I also picked this?" counts that ignore the facet's own selection.
/// </summary>
public static class FacetFilter
{
    /// <summary>Rows passing every facet group, optionally ignoring the selection of <paramref name="exceptKey"/>.</summary>
    public static IEnumerable<T> Apply<T>(
        IEnumerable<T> rows,
        IReadOnlyList<Facet<T>> facets,
        Func<string, IReadOnlySet<string>> selected,
        string? exceptKey = null) =>
        rows.Where(row => facets.All(f => f.Key == exceptKey || f.Accepts(row, selected(f.Key))));

    /// <summary>The distinct non-blank values a facet takes across the rows, ordered by display label.</summary>
    public static IReadOnlyList<string> Options<T>(IEnumerable<T> rows, Facet<T> facet, Func<string, string> label) =>
        [.. rows.SelectMany(facet.Values)
            .OfType<string>()
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(label, StringComparer.CurrentCultureIgnoreCase)];

    /// <summary>Counts per value of one facet over rows already filtered by every other active facet (and the search).</summary>
    public static IReadOnlyDictionary<string, int> Counts<T>(IEnumerable<T> rows, Facet<T> facet) => FacetCounts.Of(rows, facet.Values);
}
