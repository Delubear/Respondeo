using YamlDotNet.Serialization;

namespace Respondeo.Content.Models;

/// <summary>
/// The structured metadata parsed from a content file's YAML front-matter.
/// Everything here is author-edited; the Markdown body (below the front-matter) holds the rich content (text, links, images).
/// </summary>
public sealed class ContentFrontMatter
{
    /// <summary>Stable unique id used for linking between nodes (the graph key).</summary>
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Headline shown on the node page and in link cards.</summary>
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Short one-line description used on cards and previews.</summary>
    [YamlMember(Alias = "summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Faith starting points this node speaks to (e.g. "atheist", "agnostic", "protestant", "non-practicing-catholic").
    /// Used to surface entry points.
    /// </summary>
    [YamlMember(Alias = "audiences")]
    public List<string> Audiences { get; set; } = new();

    /// <summary>
    /// True if this node is a top-level starting point shown on the home page.
    /// </summary>
    [YamlMember(Alias = "isEntryPoint")]
    public bool IsEntryPoint { get; set; }

    /// <summary>
    /// Child branches.
    /// Overlap and multiple paths are allowed &mdash; several nodes may link to the same child id, forming a graph rather than a tree.
    /// </summary>
    [YamlMember(Alias = "branches")]
    public List<BranchLink> Branches { get; set; } = new();
}

/// <summary>
/// A directed link from one node to another, optionally labelled so the prompt can be phrased as a question/choice for the visitor.
/// </summary>
public sealed class BranchLink
{
    /// <summary>The id of the target <see cref="ContentNode"/>.</summary>
    [YamlMember(Alias = "to")]
    public string To { get; set; } = string.Empty;

    /// <summary>Optional override label; falls back to the target's title.</summary>
    [YamlMember(Alias = "label")]
    public string? Label { get; set; }

    /// <summary>Optional short reason/prompt shown beneath the label.</summary>
    [YamlMember(Alias = "prompt")]
    public string? Prompt { get; set; }
}

/// <summary>
/// A fully materialized content node: its front-matter metadata plus the rendered HTML produced from the Markdown body.
/// </summary>
public sealed class ContentNode
{
    public required ContentFrontMatter Meta { get; init; }

    /// <summary>Rendered HTML of the Markdown body (safe, author-curated).</summary>
    public required string BodyHtml { get; init; }

    public string Id => Meta.Id;
    public string Title => Meta.Title;
    public string Summary => Meta.Summary;
    public IReadOnlyList<BranchLink> Branches => Meta.Branches;
}
