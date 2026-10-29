namespace Respondeo.Content.Contracts;

/// <summary>
/// The slug&#8594;label maps for the two prayer browse facets (prayed-at occasion and tag), loaded from the bundled <c>prayers/facets.json</c> content file.
/// Because the vocabulary lives in content rather than in code, authors can introduce or rename an occasion or tag by editing content only.
/// Any slug missing from a map still renders via <see cref="MiracleFacets.Humanize"/>, so the UI never breaks;
/// a content-integrity test flags unmapped slugs so they can be given a proper label.
/// </summary>
public sealed class PrayerFacetCatalog
{
    /// <summary>Prayed-at slug &#8594; display label (e.g. "rosary" &#8594; "Rosary").</summary>
    public IReadOnlyDictionary<string, string> PrayedAt { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Tag slug &#8594; display label (e.g. "divine-mercy" &#8594; "Divine Mercy").</summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>An empty catalog used before content has loaded (every slug falls back to humanized text).</summary>
    public static PrayerFacetCatalog Empty { get; } = new();

    /// <summary>Resolves a prayed-at slug to its label, humanizing the slug when it is unmapped.</summary>
    public string PrayedAtLabel(string slug) => Lookup(PrayedAt, slug);

    /// <summary>Resolves a tag slug to its label, humanizing the slug when it is unmapped.</summary>
    public string TagLabel(string slug) => Lookup(Tags, slug);

    private static string Lookup(IReadOnlyDictionary<string, string> map, string slug) =>
        !string.IsNullOrWhiteSpace(slug) && map.TryGetValue(slug.Trim(), out var label) ? label : MiracleFacets.Humanize(slug);
}
