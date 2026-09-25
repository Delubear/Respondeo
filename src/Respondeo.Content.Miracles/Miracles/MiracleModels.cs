namespace Respondeo.Content.Miracles;

/// <summary>
/// The lightweight browse/search index for the whole miracles catalog: every miracle's typed facets
/// and summary, but without the heavy rendered article bodies. Loaded once so the catalog can be
/// filtered by facet and searched by title/summary/tags with a single pass.
/// </summary>
public sealed class MiracleIndex
{
    /// <summary>Every catalogued miracle, in load order.</summary>
    public IReadOnlyList<MiracleIndexEntry> Entries { get; init; } = [];
}

/// <summary>
/// A single miracle as it appears in the browse index: enough to render a card and apply every facet
/// filter, without fetching the full body.
/// </summary>
public sealed class MiracleIndexEntry
{
    /// <summary>Stable id / URL slug for the miracle (e.g. "lanciano").</summary>
    public required string Id { get; init; }

    /// <summary>Display title (e.g. "The Eucharistic Miracle of Lanciano").</summary>
    public required string Title { get; init; }

    /// <summary>Short one-line description for cards and previews.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The category slugs the miracle belongs to (primary facet). Ordered; the first is the primary kind.</summary>
    public required IReadOnlyList<string> Types { get; init; }

    /// <summary>The Church's stance on the miracle (credibility facet), as a slug.</summary>
    public required string Approval { get; init; }

    /// <summary>Broad geographic grouping (coarse facet), as a slug.</summary>
    public string Region { get; init; } = "unknown";

    /// <summary>Free-text country of origin (e.g. "Italy"), for display.</summary>
    public string? Country { get; init; }

    /// <summary>Approximate year of the event, used for the century facet and date sorting. Null when unknown.</summary>
    public int? Year { get; init; }

    /// <summary>Free-text tags for extra searchable categorization (e.g. "bleeding host", "scientifically studied").</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// The full content of a single miracle, including every prose section as rendered HTML.
/// Fetched on demand when a reader opens a miracle so the initial index stays small.
/// </summary>
public sealed class MiracleRecord
{
    /// <summary>Stable id / URL slug (matches <see cref="MiracleIndexEntry.Id"/>).</summary>
    public required string Id { get; init; }

    /// <summary>Display title.</summary>
    public required string Title { get; init; }

    /// <summary>Short one-line description.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The category slugs the miracle belongs to. Ordered; the first is the primary kind.</summary>
    public required IReadOnlyList<string> Types { get; init; }

    /// <summary>The Church's stance on the miracle, as a slug.</summary>
    public required string Approval { get; init; }

    /// <summary>Broad geographic grouping, as a slug.</summary>
    public string Region { get; init; } = "unknown";

    /// <summary>Free-text country of origin.</summary>
    public string? Country { get; init; }

    /// <summary>Approximate year of the event. Null when unknown.</summary>
    public int? Year { get; init; }

    /// <summary>Optional feast day associated with the miracle or its saint (e.g. "December 12").</summary>
    public string? FeastDay { get; init; }

    /// <summary>Free-text tags.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// The prose body, split into titled sections (Overview, History, Evidence, Church statement, etc.),
    /// in author order. Each section's HTML is rendered from the Markdown body.
    /// </summary>
    public IReadOnlyList<MiracleSection> Sections { get; init; } = [];

    /// <summary>Citations and further-reading references.</summary>
    public IReadOnlyList<MiracleSource> Sources { get; init; } = [];
}

/// <summary>A titled prose section of a miracle article (rendered HTML under an author-supplied heading).</summary>
public sealed class MiracleSection
{
    /// <summary>The section heading (e.g. "What happened", "The scientific findings").</summary>
    public required string Heading { get; init; }

    /// <summary>The rendered HTML of the section body.</summary>
    public required string Html { get; init; }
}

/// <summary>A citation or further-reading reference for a miracle.</summary>
public sealed class MiracleSource
{
    /// <summary>The display label of the source (e.g. "Vatican Exhibition of Eucharistic Miracles").</summary>
    public required string Label { get; init; }

    /// <summary>Optional URL of the source.</summary>
    public string? Url { get; init; }
}
