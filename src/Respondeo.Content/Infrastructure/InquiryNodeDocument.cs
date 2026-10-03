namespace Respondeo.Content.Infrastructure;

/// <summary>
/// A fully materialized inquiry node (internal domain model): author-curated metadata plus the rendered HTML of its Markdown body.
/// Produced by the inquiry parser/loader and mapped onto the public <c>Respondeo.Content.Contracts.InquiryNode</c> DTO at the service boundary.
/// </summary>
internal sealed class InquiryNodeDocument
{
    /// <summary>Stable unique id used for linking between nodes (the graph key) and the URL slug.</summary>
    public required string Id { get; init; }

    /// <summary>Headline shown on the node page and in link cards.</summary>
    public required string Title { get; init; }

    /// <summary>Short one-line description used on cards and previews.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Rendered HTML of the Markdown body (safe, author-curated).</summary>
    public required string BodyHtml { get; init; }

    /// <summary>Free-text tags this node belongs to, used to group and filter articles (e.g. "Existence of God", "St. Thomas Aquinas").</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>True when the front-matter marks this node as not yet reviewed.</summary>
    public bool IsUnvetted { get; init; }

    /// <summary>Ordered ids of child nodes to present as collapsible sections on this page.</summary>
    public IReadOnlyList<string> Sections { get; init; } = [];

    /// <summary>
    /// The stage this node belongs to (e.g. "why-god"), derived from its content sub-folder.
    /// Null for nodes that live at the content root and belong to no stage.
    /// Used to nest node routes under the stage so the URL reflects the section and the masthead tab lights up.
    /// </summary>
    public string? Stage { get; init; }
}
