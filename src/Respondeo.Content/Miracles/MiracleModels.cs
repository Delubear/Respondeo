namespace Respondeo.Content.Miracles;

// ---------------------------------------------------------------------------
// Miracle domain models (internal). Produced by MiracleParser / the content
// loader, then mapped onto the public Respondeo.Content.Contracts DTOs at the
// service boundary. Never exposed to consumers.
// ---------------------------------------------------------------------------

/// <summary>The lightweight browse/search index for the whole miracles catalog (internal domain model).</summary>
internal sealed class MiracleIndexDocument
{
    public IReadOnlyList<MiracleIndexEntryDocument> Entries { get; init; } = [];
}

/// <summary>A single miracle as it appears in the browse index (internal domain model).</summary>
internal sealed class MiracleIndexEntryDocument
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    /// <summary>Value used for alphabetical sorting; the front-matter sortKey when set, otherwise the title.</summary>
    public string SortValue { get => string.IsNullOrWhiteSpace(field) ? Title : field; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public required IReadOnlyList<string> Types { get; init; }

    public required string Approval { get; init; }

    public string Region { get; init; } = "unknown";

    public string? Country { get; init; }

    public int? Year { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>The full content of a single miracle (internal domain model).</summary>
internal sealed class MiracleRecordDocument
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    /// <summary>Value used for alphabetical sorting; the front-matter sortKey when set, otherwise the title.</summary>
    public string SortValue { get => string.IsNullOrWhiteSpace(field) ? Title : field; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public required IReadOnlyList<string> Types { get; init; }

    public required string Approval { get; init; }

    public string Region { get; init; } = "unknown";

    public string? Country { get; init; }

    public int? Year { get; init; }

    public string? FeastDay { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>True when the front-matter marks this miracle as not yet reviewed.</summary>
    public bool IsUnvetted { get; init; }

    public string BodyHtml { get; init; } = string.Empty;

    public IReadOnlyList<MiracleSourceDocument> Sources { get; init; } = [];
}

/// <summary>A citation or further-reading reference for a miracle (internal domain model).</summary>
internal sealed class MiracleSourceDocument
{
    public required string Label { get; init; }

    public string? Url { get; init; }
}
