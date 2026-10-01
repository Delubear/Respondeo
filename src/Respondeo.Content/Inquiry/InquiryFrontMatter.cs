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
}
