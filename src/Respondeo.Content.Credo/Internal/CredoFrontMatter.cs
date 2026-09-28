using Respondeo.Content.Abstractions;
using YamlDotNet.Serialization;

namespace Respondeo.Content.Credo.Internal;

/// <summary>
/// The structured metadata parsed from a prayer file's YAML front-matter block.
/// Internal serialization DTO; the parser maps it onto the public <see cref="Prayer"/> model.
/// Inherits the shared id/title/summary/tags fields from <see cref="ContentFrontMatterBase"/>.
/// </summary>
internal sealed class PrayerFrontMatter : ContentFrontMatterBase
{
    [YamlMember(Alias = "category")]
    public string Category { get; set; } = string.Empty;

    [YamlMember(Alias = "language")]
    public string Language { get; set; } = string.Empty;

    [YamlMember(Alias = "translationKey")]
    public string? TranslationKey { get; set; }

    [YamlMember(Alias = "attribution")]
    public string? Attribution { get; set; }
}

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
}

/// <summary>A citation entry in an article's front-matter.</summary>
internal sealed class SourceFrontMatter
{
    [YamlMember(Alias = "label")]
    public string Label { get; set; } = string.Empty;

    [YamlMember(Alias = "url")]
    public string? Url { get; set; }
}
