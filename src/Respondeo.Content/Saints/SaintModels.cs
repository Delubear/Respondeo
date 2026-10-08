namespace Respondeo.Content.Saints;

// ---------------------------------------------------------------------------
// Saint domain models (internal). Produced by SaintParser / the content loader,
// then mapped onto the public Respondeo.Content.Contracts DTOs at the service boundary. Never exposed to consumers.
// ---------------------------------------------------------------------------

/// <summary>The lightweight browse/search index for the whole saints catalog (internal domain model).</summary>
internal sealed class SaintIndexDocument
{
    public IReadOnlyList<SaintIndexEntryDocument> Entries { get; init; } = [];
}

/// <summary>A single saint as it appears in the browse index (internal domain model).</summary>
internal sealed class SaintIndexEntryDocument
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    /// <summary>Value used for alphabetical sorting; the front-matter sortKey when set, otherwise the title.</summary>
    public string SortValue { get => string.IsNullOrWhiteSpace(field) ? Title : field; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public required string Era { get; init; }

    public string Region { get; init; } = "unknown";

    public required IReadOnlyList<string> Patronages { get; init; }

    public required IReadOnlyList<string> StatesOfLife { get; init; }

    public required IReadOnlyList<string> Canonizations { get; init; }

    public string? Dates { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>The full content of a single saint (internal domain model).</summary>
internal sealed class SaintRecordDocument
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    /// <summary>Value used for alphabetical sorting; the front-matter sortKey when set, otherwise the title.</summary>
    public string SortValue { get => string.IsNullOrWhiteSpace(field) ? Title : field; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public required string Era { get; init; }

    public string Region { get; init; } = "unknown";

    public required IReadOnlyList<string> Patronages { get; init; }

    public required IReadOnlyList<string> StatesOfLife { get; init; }

    public required IReadOnlyList<string> Canonizations { get; init; }

    public string? Dates { get; init; }

    public string? FeastDay { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>True when the front-matter marks this saint as not yet reviewed.</summary>
    public bool IsUnvetted { get; init; }

    public string BodyHtml { get; init; } = string.Empty;

    public IReadOnlyList<SaintSourceDocument> Sources { get; init; } = [];
}

/// <summary>A citation or further-reading reference for a saint (internal domain model).</summary>
internal sealed class SaintSourceDocument
{
    public required string Label { get; init; }

    public string? Url { get; init; }
}
