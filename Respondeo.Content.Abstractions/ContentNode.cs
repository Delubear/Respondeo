namespace Respondeo.Content.Abstractions;

/// <summary>
/// A fully materialized content node: author-curated metadata plus the rendered HTML of its Markdown body.
/// This is the pure domain model exposed to consumers; it carries no serialization concerns.
/// </summary>
public sealed class ContentNode
{
    /// <summary>Stable unique id used for linking between nodes (the graph key) and the URL slug.</summary>
    public required string Id { get; init; }

    /// <summary>Headline shown on the node page and in link cards.</summary>
    public required string Title { get; init; }

    /// <summary>Short one-line description used on cards and previews.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Rendered HTML of the Markdown body (safe, author-curated).</summary>
    public required string BodyHtml { get; init; }

    /// <summary>True if this node is a top-level starting point shown on the home page.</summary>
    public bool IsEntryPoint { get; init; }

    /// <summary>Categories this node belongs to, used to group and filter articles (e.g. "Existence of God", "St. Thomas Aquinas").</summary>
    public IReadOnlyList<string> Topics { get; init; } = [];

    /// <summary>Directed links to other nodes, forming a graph rather than a tree.</summary>
    public IReadOnlyList<BranchLink> Branches { get; init; } = [];

    /// <summary>Ordered ids of child nodes to present as collapsible sections on this page.</summary>
    public IReadOnlyList<string> Sections { get; init; } = [];

    /// <summary>
    /// Optional culminating transition to the next stage of the journey (e.g. from the end of
    /// "Why God?" onward to "Why Jesus?"). Unlike a branch, this points at a stage landing route
    /// rather than another node, and is presented as a distinct, prominent call to action.
    /// </summary>
    public StageLink? NextStage { get; init; }
}

/// <summary>
/// A prominent link that carries the visitor from the end of one stage to the beginning of the next.
/// It targets a page route (e.g. "/why-jesus") rather than a node id.
/// </summary>
public sealed class StageLink
{
    /// <summary>The route of the next stage's landing page (e.g. "/why-jesus").</summary>
    public required string Href { get; init; }

    /// <summary>Headline shown on the transition button.</summary>
    public required string Label { get; init; }

    /// <summary>Optional short reason/prompt shown beneath the label.</summary>
    public string? Prompt { get; init; }
}

/// <summary>
/// A directed link from one node to another, optionally labelled so the prompt can be phrased for the visitor.
/// </summary>
public sealed class BranchLink
{
    /// <summary>The id of the target <see cref="ContentNode"/>.</summary>
    public required string To { get; init; }

    /// <summary>Optional override label; falls back to the target's title.</summary>
    public string? Label { get; init; }

    /// <summary>Optional short reason/prompt shown beneath the label.</summary>
    public string? Prompt { get; init; }
}
