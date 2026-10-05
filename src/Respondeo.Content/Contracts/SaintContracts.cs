namespace Respondeo.Content.Contracts;

// ---------------------------------------------------------------------------
// Saints. Public DTOs returned by ISaintService. The parsing/domain types
// live internally in the Saints folder.
// ---------------------------------------------------------------------------

/// <summary>
/// The lightweight browse/search index for the whole saints catalog: every saint's typed facets and summary, but without the heavy rendered profile bodies.
/// </summary>
public sealed class SaintIndex
{
    /// <summary>Every cataloged saint, in load order.</summary>
    public IReadOnlyList<SaintIndexEntry> Entries { get; init; } = [];
}

/// <summary>
/// A single saint as it appears in the browse index: enough to render a card and apply every facet filter, without fetching the full body.
/// </summary>
public sealed class SaintIndexEntry
{
    /// <summary>Stable id / URL slug for the saint (e.g. "francis-of-assisi").</summary>
    public required string Id { get; init; }

    /// <summary>Display title (e.g. "St. Francis of Assisi").</summary>
    public required string Title { get; init; }

    /// <summary>Value used for alphabetical sorting; the front-matter sortKey when set, otherwise the title.</summary>
    public string SortValue { get => string.IsNullOrWhiteSpace(field) ? Title : field; init; } = string.Empty;

    /// <summary>Short one-line description for cards and previews.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The historical era the saint belongs to (facet), as a slug.</summary>
    public required string Era { get; init; }

    /// <summary>Broad geographic grouping (coarse facet), as a slug.</summary>
    public string Region { get; init; } = "unknown";

    /// <summary>The patronage slugs associated with the saint (facet). Ordered; the first is the primary patronage.</summary>
    public required IReadOnlyList<string> Patronages { get; init; }

    /// <summary>The saint's states of life (facet), as slugs. A saint may hold several (e.g. religious, priest, martyr).</summary>
    public required IReadOnlyList<string> StatesOfLife { get; init; }

    /// <summary>The saint's canonization statuses (facet), as slugs (e.g. canonized plus doctor-of-the-church).</summary>
    public required IReadOnlyList<string> Canonizations { get; init; }

    /// <summary>Free-text life dates (e.g. "1181–1226"), for display.</summary>
    public string? Dates { get; init; }

    /// <summary>Free-text tags for extra searchable categorization.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// The full content of a single saint, including the profile body as rendered HTML.
/// </summary>
public sealed class SaintRecord
{
    /// <summary>Stable id / URL slug (matches <see cref="SaintIndexEntry.Id"/>).</summary>
    public required string Id { get; init; }

    /// <summary>Display title.</summary>
    public required string Title { get; init; }

    /// <summary>Value used for alphabetical sorting; the front-matter sortKey when set, otherwise the title.</summary>
    public string SortValue { get => string.IsNullOrWhiteSpace(field) ? Title : field; init; } = string.Empty;

    /// <summary>Short one-line description.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The historical era the saint belongs to, as a slug.</summary>
    public required string Era { get; init; }

    /// <summary>Broad geographic grouping, as a slug.</summary>
    public string Region { get; init; } = "unknown";

    /// <summary>The patronages associated with the saint, as free-text display values. Ordered; the first is the primary patronage.</summary>
    public required IReadOnlyList<string> Patronages { get; init; }

    /// <summary>The saint's states of life, as slugs. A saint may hold several (e.g. religious, priest, martyr).</summary>
    public required IReadOnlyList<string> StatesOfLife { get; init; }

    /// <summary>The saint's canonization statuses, as slugs (e.g. canonized plus doctor-of-the-church).</summary>
    public required IReadOnlyList<string> Canonizations { get; init; }

    /// <summary>Free-text life dates (e.g. "354–430"). Null when unknown.</summary>
    public string? Dates { get; init; }

    /// <summary>Optional feast day associated with the saint (e.g. "October 4").</summary>
    public string? FeastDay { get; init; }

    /// <summary>Free-text tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>True when the saint is flagged as not yet reviewed; the detail page shows a notice.</summary>
    public bool IsUnvetted { get; init; }

    /// <summary>Rendered HTML of the full Markdown body (safe, author-curated).</summary>
    public string BodyHtml { get; init; } = string.Empty;

    /// <summary>Citations and further-reading references.</summary>
    public IReadOnlyList<SaintSource> Sources { get; init; } = [];
}

/// <summary>A citation or further-reading reference for a saint.</summary>
public sealed class SaintSource : IContentSource
{
    /// <summary>The display label of the source.</summary>
    public required string Label { get; init; }

    /// <summary>Optional URL of the source.</summary>
    public string? Url { get; init; }
}
