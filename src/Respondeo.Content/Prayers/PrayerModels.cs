namespace Respondeo.Content.Prayers;

// ---------------------------------------------------------------------------
// Prayers
// ---------------------------------------------------------------------------

/// <summary>
/// A single prayer as it appears in the browse index: enough to render a card without fetching the full text.
/// Only traditional / public-domain prayers are catalogued; each carries an optional attribution so the source can be shown where one is required.
/// </summary>
public sealed class PrayerSummary
{
    /// <summary>Stable id / URL slug for the prayer (e.g. "hail-mary").</summary>
    public required string Id { get; init; }

    /// <summary>Display title (e.g. "Hail Mary").</summary>
    public required string Title { get; init; }

    /// <summary>Value used for alphabetical sorting; the front-matter sortKey when set, otherwise the title.</summary>
    public string SortValue { get => string.IsNullOrWhiteSpace(field) ? Title : field; init; } = string.Empty;

    /// <summary>Short one-line description for cards and previews.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The category slug the prayer belongs to (e.g. "marian", "daily", "mass").</summary>
    public string Category { get; init; } = "other";

    /// <summary>Free-text tags for extra searchable categorization.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// The full text of a single prayer, rendered as HTML, fetched on demand when a reader opens it.
/// </summary>
public sealed class Prayer
{
    /// <summary>Stable id / URL slug (matches <see cref="PrayerSummary.Id"/>).</summary>
    public required string Id { get; init; }

    /// <summary>Display title.</summary>
    public required string Title { get; init; }

    /// <summary>Value used for alphabetical sorting; the front-matter sortKey when set, otherwise the title.</summary>
    public string SortValue { get => string.IsNullOrWhiteSpace(field) ? Title : field; init; } = string.Empty;

    /// <summary>Short one-line description.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The category slug the prayer belongs to.</summary>
    public string Category { get; init; } = "other";

    /// <summary>The language of this prayer's text (e.g. "en", "la"). Defaults to "en".</summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Optional key shared with the prayer's translations.
    /// Files with the same key are the same prayer in different languages; the service uses it to pair an English prayer with its Latin.
    /// </summary>
    public string? TranslationKey { get; init; }

    /// <summary>The rendered HTML of the prayer text.</summary>
    public required string Html { get; init; }

    /// <summary>
    /// Optional Latin (or original-language) rendered HTML, shown alongside the vernacular.
    /// Populated by the service from the linked Latin file that shares this prayer's <see cref="TranslationKey"/>.
    /// </summary>
    public string? LatinHtml { get; set; }

    /// <summary>Free-text tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Optional attribution / source note (e.g. a translation credit) for copyright care.</summary>
    public string? Attribution { get; init; }
}
