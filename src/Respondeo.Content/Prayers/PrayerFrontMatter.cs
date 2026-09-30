using Respondeo.Content.Infrastructure;
using YamlDotNet.Serialization;

namespace Respondeo.Content.Prayers;

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
