namespace Respondeo.Content.Miracles;

/// <summary>
/// The slug&#8594;label maps for the three miracle browse facets (category, approval, region), loaded
/// from the bundled <c>miracles/facets.json</c> content file. Because the vocabulary lives in content
/// rather than in code, authors can introduce a new category/approval/region by editing content only.
/// Any slug missing from a map still renders via <see cref="MiracleFacets.Humanize"/>, so the UI never
/// breaks; a content-integrity test flags unmapped slugs so they can be given a proper label.
/// </summary>
public sealed class MiracleFacetCatalog
{
    /// <summary>Category slug &#8594; display label (e.g. "marian" &#8594; "Marian apparition").</summary>
    public IReadOnlyDictionary<string, string> Categories { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Approval slug &#8594; display label (e.g. "approved" &#8594; "Church-approved").</summary>
    public IReadOnlyDictionary<string, string> Approvals { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Region slug &#8594; display label (e.g. "middle-east" &#8594; "Middle East").</summary>
    public IReadOnlyDictionary<string, string> Regions { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>An empty catalog used before content has loaded (every slug falls back to humanized text).</summary>
    public static MiracleFacetCatalog Empty { get; } = new();

    /// <summary>Resolves a category slug to its label, humanizing the slug when it is unmapped.</summary>
    public string Category(string slug) => Lookup(Categories, slug);

    /// <summary>Resolves an approval slug to its label, humanizing the slug when it is unmapped.</summary>
    public string Approval(string slug) => Lookup(Approvals, slug);

    /// <summary>Resolves a region slug to its label, humanizing the slug when it is unmapped.</summary>
    public string Region(string slug) => Lookup(Regions, slug);

    private static string Lookup(IReadOnlyDictionary<string, string> map, string slug) =>
        !string.IsNullOrWhiteSpace(slug) && map.TryGetValue(slug.Trim(), out var label)
            ? label
            : MiracleFacets.Humanize(slug);
}

/// <summary>
/// Small presentation helpers for miracle facets that do not depend on the loaded catalog: turning a
/// raw slug into readable text and deriving a century label from a year.
/// </summary>
public static class MiracleFacets
{
    /// <summary>
    /// Turns a slug (e.g. "middle-east") into a readable label ("Middle east") as a safe fallback for any
    /// slug that is not present in <see cref="MiracleFacetCatalog"/>. Returns "Other" for empty input.
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

    /// <summary>Derives the century label (e.g. "8th century", "20th century") from a year, or null when unknown.</summary>
    public static string? CenturyLabel(int? year)
    {
        if (year is null || year == 0)
        {
            return null;
        }

        var century = (int)Math.Ceiling(Math.Abs(year.Value) / 100d);
        var suffix = (century % 100) is >= 11 and <= 13
            ? "th"
            : (century % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };

        return year > 0 ? $"{century}{suffix} century" : $"{century}{suffix} century BC";
    }
}
