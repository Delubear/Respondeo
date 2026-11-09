namespace Respondeo.Services.Browse;

/// <summary>
/// Counts how many rows carry each facet value. Browse pages pass the rows filtered by every OTHER active facet (plus the search query),
/// so each option's count answers "how many results would I get if I also picked this?".
/// </summary>
public static class FacetCounts
{
    public static IReadOnlyDictionary<string, int> Of<T>(IEnumerable<T> rows, Func<T, IEnumerable<string?>> values)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            // A row counts once per distinct value, even if it lists the same value twice.
            foreach (var value in values(row).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase))
            {
                counts[value] = counts.GetValueOrDefault(value) + 1;
            }
        }

        return counts;
    }

    public static IReadOnlyDictionary<string, int> Of<T>(IEnumerable<T> rows, Func<T, string?> value) => Of(rows, row => new[] { value(row) });
}
