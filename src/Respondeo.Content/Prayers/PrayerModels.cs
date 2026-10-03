namespace Respondeo.Content.Prayers;

// ---------------------------------------------------------------------------
// Prayer domain models (internal). Produced by PrayerParser, paired with Latin
// translations by PrayerService, then mapped onto the public
// Respondeo.Content.Contracts DTOs at the service boundary.
// ---------------------------------------------------------------------------

/// <summary>The parsed full text of a single prayer (internal domain model).</summary>
internal sealed class PrayerDocument
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    /// <summary>Value used for alphabetical sorting; the front-matter sortKey when set, otherwise the title.</summary>
    public string SortValue { get => string.IsNullOrWhiteSpace(field) ? Title : field; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Category { get; init; } = "other";

    public string Language { get; init; } = "en";

    /// <summary>Optional key shared with the prayer's translations.</summary>
    public string? TranslationKey { get; init; }

    public required string Html { get; init; }

    /// <summary>Optional Latin rendered HTML, populated by the service from the linked Latin file.</summary>
    public string? LatinHtml { get; set; }

    /// <summary>Optional Latin title, populated by the service from the linked Latin file.</summary>
    public string? LatinTitle { get; set; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public string? Attribution { get; init; }
}
