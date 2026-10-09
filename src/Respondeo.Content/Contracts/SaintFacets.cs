namespace Respondeo.Content.Contracts;

/// <summary>
/// The slug&#8594;label maps for the four saint browse facets (era, region, state of life, canonization),
/// loaded from the bundled <c>saints/facets.json</c> content file.
/// Because the vocabulary lives in content rather than in code, authors can introduce a new facet value by editing content only.
/// Any slug missing from a map still renders via <see cref="SaintFacets.Humanize"/>, so the UI never breaks;
/// a content-integrity test flags unmapped slugs so they can be given a proper label.
/// </summary>
public sealed class SaintFacetCatalog
{
    /// <summary>Era slug &#8594; display label (e.g. "medieval" &#8594; "Medieval").</summary>
    public IReadOnlyDictionary<string, string> Eras { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Region slug &#8594; display label (e.g. "middle-east" &#8594; "Middle East").</summary>
    public IReadOnlyDictionary<string, string> Regions { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>State-of-life slug &#8594; display label (e.g. "religious" &#8594; "Religious").</summary>
    public IReadOnlyDictionary<string, string> StatesOfLife { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Designation slug &#8594; display label (e.g. "martyr" &#8594; "Martyr", "doctor-of-the-church" &#8594; "Doctor of the Church").</summary>
    public IReadOnlyDictionary<string, string> Designations { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Sex slug &#8594; display label (e.g. "male" &#8594; "Male").</summary>
    public IReadOnlyDictionary<string, string> Sexes { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Religious-order slug &#8594; display label (e.g. "dominican" &#8594; "Dominican").</summary>
    public IReadOnlyDictionary<string, string> ReligiousOrders { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Canonization slug &#8594; display label (e.g. "canonized" &#8594; "Canonized").</summary>
    public IReadOnlyDictionary<string, string> Canonizations { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>An empty catalog used before content has loaded (every slug falls back to humanized text).</summary>
    public static SaintFacetCatalog Empty { get; } = new();

    /// <summary>Resolves an era slug to its label, humanizing the slug when it is unmapped.</summary>
    public string Era(string slug) => Lookup(Eras, slug);

    /// <summary>Resolves a region slug to its label, humanizing the slug when it is unmapped.</summary>
    public string Region(string slug) => Lookup(Regions, slug);

    /// <summary>Resolves a state-of-life slug to its label, humanizing the slug when it is unmapped.</summary>
    public string StateOfLife(string slug) => Lookup(StatesOfLife, slug);

    /// <summary>Resolves a designation slug to its label, humanizing the slug when it is unmapped.</summary>
    public string Designation(string slug) => Lookup(Designations, slug);

    /// <summary>Resolves a sex slug to its label, humanizing the slug when it is unmapped.</summary>
    public string Sex(string slug) => Lookup(Sexes, slug);

    /// <summary>Resolves a religious-order slug to its label, humanizing the slug when it is unmapped.</summary>
    public string ReligiousOrder(string slug) => Lookup(ReligiousOrders, slug);

    /// <summary>Resolves a canonization slug to its label, humanizing the slug when it is unmapped.</summary>
    public string Canonization(string slug) => Lookup(Canonizations, slug);

    private static string Lookup(IReadOnlyDictionary<string, string> map, string slug) =>
        !string.IsNullOrWhiteSpace(slug) && map.TryGetValue(slug.Trim(), out var label) ? label : SaintFacets.Humanize(slug);
}

/// <summary>
/// Small presentation helpers for saint facets that do not depend on the loaded catalog: turning a raw slug into readable text.
/// </summary>
public static class SaintFacets
{
    /// <summary>
    /// Turns a slug (e.g. "middle-east") into a readable label ("Middle east") as a safe fallback for any slug that is not present in <see cref="SaintFacetCatalog"/>.
    /// Returns "Other" for empty input.
    /// </summary>
    public static string Humanize(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return "Other";
        }

        var words = slug.Trim().Replace('_', '-').Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return "Other";
        }

        words[0] = char.ToUpperInvariant(words[0][0]) + words[0][1..];
        return string.Join(' ', words);
    }
}
