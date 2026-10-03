using Respondeo.Content.Infrastructure;
using YamlDotNet.Serialization;

namespace Respondeo.Content.Inquiry;

/// <summary>
/// The structured metadata parsed from a content file's YAML front-matter block.
/// This is an internal serialization DTO; the parser maps it onto the public <see cref="Contracts.InquiryNode"/> so YAML concerns never leak past the boundary.
/// Inherits the shared id/title/summary/tags fields from <see cref="ContentFrontMatterBase"/>.
/// </summary>
internal sealed class InquiryFrontMatter : ContentFrontMatterBase
{
    /// <summary>Ordered ids of child content nodes to present as collapsible sections on this page.</summary>
    [YamlMember(Alias = "sections")]
    public List<string> Sections { get; set; } = [];

    /// <summary>
    /// Optional review status. <c>unvetted</c> marks AI-drafted or not-yet-reviewed content so the node
    /// page renders a standard notice; any other value (or blank) is treated as vetted. Prefer this flag
    /// over hand-written inline HTML notes in the body.
    /// </summary>
    [YamlMember(Alias = "reviewStatus")]
    public string? ReviewStatus { get; set; }

    /// <summary>True when <see cref="ReviewStatus"/> declares the content as not yet reviewed.</summary>
    public bool IsUnvetted => string.Equals(ReviewStatus?.Trim(), "unvetted", StringComparison.OrdinalIgnoreCase);
}
