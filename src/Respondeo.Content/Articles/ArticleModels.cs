namespace Respondeo.Content.Articles;

// ---------------------------------------------------------------------------
// Article domain models (internal). Produced by ArticleParser from Markdown +
// front-matter, then mapped onto the public Respondeo.Content.Contracts DTOs
// at the service boundary. Never exposed to consumers.
// ---------------------------------------------------------------------------

/// <summary>The parsed full content of a single article (internal domain model).</summary>
internal sealed class ArticleDocument
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    /// <summary>Value used for alphabetical sorting; the front-matter sortKey when set, otherwise the title.</summary>
    public string SortValue { get => string.IsNullOrWhiteSpace(field) ? Title : field; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Topic { get; init; } = "general";

    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>True when the front-matter marks this article as not yet reviewed.</summary>
    public bool IsUnvetted { get; init; }

    public string BodyHtml { get; init; } = string.Empty;

    public IReadOnlyList<ArticleSourceDocument> Sources { get; init; } = [];
}

/// <summary>A citation or further-reading reference for an article (internal domain model).</summary>
internal sealed class ArticleSourceDocument
{
    public required string Label { get; init; }

    public string? Url { get; init; }
}
