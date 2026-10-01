namespace Respondeo.Content.Contracts;

// ---------------------------------------------------------------------------
// Inquiry content nodes. Public DTOs returned by IContentService. The
// parsing/domain types live internally in the Infrastructure/Inquiry folders.
// ---------------------------------------------------------------------------

/// <summary>
/// A fully materialized inquiry node: author-curated metadata plus the rendered HTML of its Markdown body.
/// This is the pure DTO exposed to consumers; it carries no serialization concerns.
/// </summary>
public sealed class InquiryNode
{
    /// <summary>Stable unique id used for linking between nodes (the graph key) and the URL slug.</summary>
    public required string Id { get; init; }

    /// <summary>Headline shown on the node page and in link cards.</summary>
    public required string Title { get; init; }

    /// <summary>Short one-line description used on cards and previews.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Rendered HTML of the Markdown body (safe, author-curated).</summary>
    public required string BodyHtml { get; init; }

    /// <summary>Free-text tags this node belongs to, used to group and filter articles.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Ordered ids of child nodes to present as collapsible sections on this page.</summary>
    public IReadOnlyList<string> Sections { get; init; } = [];

    /// <summary>The stage this node belongs to (e.g. "why-god"), derived from its content sub-folder. Null for root nodes.</summary>
    public string? Stage { get; init; }
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

    /// <summary>Optional name of the emblem shown in the golden box (e.g. "cross", "compass"). Falls back to the cross when unset.</summary>
    public string? Icon { get; init; }
}

/// <summary>
/// A directed link from one node to another, optionally labelled so the prompt can be phrased for the visitor.
/// </summary>
public sealed class BranchLink
{
    /// <summary>The id of the target <see cref="InquiryNode"/>.</summary>
    public required string To { get; init; }

    /// <summary>Optional override label; falls back to the target's title.</summary>
    public string? Label { get; init; }

    /// <summary>Optional short reason/prompt shown beneath the label.</summary>
    public string? Prompt { get; init; }
}

/// <summary>
/// A stage of the Inquiry journey as declared by the orchestration file (flow.json): its identity,
/// ordered position, and the node it opens on. Exposed so callers can present the journey's stages
/// in order without reaching into content files. Branch edges and the stage transition are resolved
/// separately through <see cref="Respondeo.Content.IInquiryFlow"/>.
/// </summary>
public sealed class InquiryStage
{
    /// <summary>Stage key, matching the content sub-folder its nodes live in (e.g. "why-god").</summary>
    public required string Id { get; init; }

    /// <summary>Display name for the stage (e.g. "Why God?").</summary>
    public required string Title { get; init; }

    /// <summary>Optional short kicker shown above the stage's landing title (e.g. "The First Question").</summary>
    public string? Kicker { get; init; }

    /// <summary>Explicit 1-based position of this stage in the journey.</summary>
    public required int Order { get; init; }

    /// <summary>Id of the node this stage opens on.</summary>
    public required string Entry { get; init; }
}
