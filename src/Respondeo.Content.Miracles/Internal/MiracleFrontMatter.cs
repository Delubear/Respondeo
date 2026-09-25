using YamlDotNet.Serialization;

namespace Respondeo.Content.Miracles.Internal;

/// <summary>
/// The structured metadata parsed from a miracle file's YAML front-matter block.
/// This is an internal serialization DTO; the parser maps it onto the public typed models so YAML
/// concerns never leak past the boundary.
/// </summary>
internal sealed class MiracleFrontMatter
{
    /// <summary>Stable unique id / URL slug (e.g. "lanciano").</summary>
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Headline shown on the detail page and in cards.</summary>
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Short one-line description used on cards and previews.</summary>
    [YamlMember(Alias = "summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>The miracle type slug (e.g. "eucharistic").</summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>The approval status slug (e.g. "approved").</summary>
    [YamlMember(Alias = "approval")]
    public string Approval { get; set; } = string.Empty;

    /// <summary>The region slug (e.g. "europe").</summary>
    [YamlMember(Alias = "region")]
    public string Region { get; set; } = string.Empty;

    /// <summary>Free-text country of origin.</summary>
    [YamlMember(Alias = "country")]
    public string? Country { get; set; }

    /// <summary>Approximate year of the event.</summary>
    [YamlMember(Alias = "year")]
    public int? Year { get; set; }

    /// <summary>Optional feast day.</summary>
    [YamlMember(Alias = "feastDay")]
    public string? FeastDay { get; set; }

    /// <summary>Free-text tags for extra searchable categorization.</summary>
    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>Citations / further reading.</summary>
    [YamlMember(Alias = "sources")]
    public List<MiracleSourceDto> Sources { get; set; } = [];
}

/// <summary>Serialization DTO for a citation declared in front-matter.</summary>
internal sealed class MiracleSourceDto
{
    [YamlMember(Alias = "label")]
    public string Label { get; set; } = string.Empty;

    [YamlMember(Alias = "url")]
    public string? Url { get; set; }
}
