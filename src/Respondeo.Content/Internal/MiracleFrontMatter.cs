using Respondeo.Content.Shared;
using YamlDotNet.Serialization;

namespace Respondeo.Content.Discover.Internal;

/// <summary>
/// The structured metadata parsed from a miracle file's YAML front-matter block.
/// This is an internal serialization DTO; the parser maps it onto the public typed models so YAML concerns never leak past the boundary.
/// Inherits the shared id/title/summary/tags fields from <see cref="ContentFrontMatterBase"/>.
/// </summary>
internal sealed class MiracleFrontMatter : ContentFrontMatterBase
{
    /// <summary>The miracle category slugs (e.g. ["marian", "image", "stigmata"]).</summary>
    [YamlMember(Alias = "types")]
    public List<string> Types { get; set; } = [];

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
