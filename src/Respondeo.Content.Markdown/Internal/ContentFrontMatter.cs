using Respondeo.Content.Abstractions;
using YamlDotNet.Serialization;

namespace Respondeo.Content.Markdown.Internal;

/// <summary>
/// The structured metadata parsed from a content file's YAML front-matter block.
/// This is an internal serialization DTO; the parser maps it onto the public <see cref="Abstractions.ContentNode"/> so YAML concerns never leak past the boundary.
/// Inherits the shared id/title/summary/tags fields from <see cref="ContentFrontMatterBase"/>.
/// </summary>
internal sealed class ContentFrontMatter : ContentFrontMatterBase
{
    /// <summary>Child branches; several nodes may link to the same child id, forming a graph.</summary>
    [YamlMember(Alias = "branches")]
    public List<BranchLinkDto> Branches { get; set; } = [];

    /// <summary>Ordered ids of child content nodes to present as collapsible sections on this page.</summary>
    [YamlMember(Alias = "sections")]
    public List<string> Sections { get; set; } = [];

    /// <summary>Optional culminating transition to the next stage's landing page.</summary>
    [YamlMember(Alias = "nextStage")]
    public StageLinkDto? NextStage { get; set; }
}

/// <summary>
/// Serialization DTO for a culminating stage transition declared in front-matter.
/// </summary>
internal sealed class StageLinkDto
{
    [YamlMember(Alias = "href")]
    public string Href { get; set; } = string.Empty;

    [YamlMember(Alias = "label")]
    public string Label { get; set; } = string.Empty;

    [YamlMember(Alias = "prompt")]
    public string? Prompt { get; set; }

    [YamlMember(Alias = "icon")]
    public string? Icon { get; set; }
}

/// <summary>
/// Serialization DTO for a directed link declared in front-matter.
/// </summary>
internal sealed class BranchLinkDto
{
    [YamlMember(Alias = "to")]
    public string To { get; set; } = string.Empty;

    [YamlMember(Alias = "label")]
    public string? Label { get; set; }

    [YamlMember(Alias = "prompt")]
    public string? Prompt { get; set; }
}
