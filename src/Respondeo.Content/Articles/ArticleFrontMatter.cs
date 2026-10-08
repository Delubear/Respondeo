using Respondeo.Content.Infrastructure;
using YamlDotNet.Serialization;

namespace Respondeo.Content.Articles;

/// <summary>
/// The structured metadata parsed from an article file's YAML front-matter block.
/// Internal serialization DTO; the parser maps it onto the public <see cref="Article"/> model.
/// Inherits the shared id/title/summary/tags fields from <see cref="ContentFrontMatterBase"/>.
/// </summary>
internal sealed class ArticleFrontMatter : ContentFrontMatterBase
{
    [YamlMember(Alias = "topic")]
    public string Topic { get; set; } = string.Empty;

    [YamlMember(Alias = "sources")]
    public List<SourceFrontMatter> Sources { get; set; } = [];

    /// <summary>
    /// Optional review status. <c>unvetted</c> marks AI-drafted or not-yet-reviewed content so the detail page renders a standard notice;
    /// any other value (or blank) is treated as vetted. Prefer this flag over hand-written inline HTML notes in the body.
    /// </summary>
    [YamlMember(Alias = "reviewStatus")]
    public string? ReviewStatus { get; set; }

    /// <summary>True when <see cref="ReviewStatus"/> declares the content as not yet reviewed.</summary>
    public bool IsUnvetted => string.Equals(ReviewStatus?.Trim(), "unvetted", StringComparison.OrdinalIgnoreCase);
}

/// <summary>A citation entry in an article's front-matter.</summary>
internal sealed class SourceFrontMatter
{
    [YamlMember(Alias = "label")]
    public string Label { get; set; } = string.Empty;

    [YamlMember(Alias = "url")]
    public string? Url { get; set; }
}
