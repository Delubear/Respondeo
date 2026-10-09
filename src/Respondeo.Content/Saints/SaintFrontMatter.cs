using Respondeo.Content.Infrastructure;
using YamlDotNet.Serialization;

namespace Respondeo.Content.Saints;

/// <summary>
/// The structured metadata parsed from a saint file's YAML front-matter block.
/// This is an internal serialization DTO; the parser maps it onto the public typed models so YAML concerns never leak past the boundary.
/// Inherits the shared id/title/summary/tags fields from <see cref="ContentFrontMatterBase"/>.
/// </summary>
internal sealed class SaintFrontMatter : ContentFrontMatterBase
{
    /// <summary>The historical era slug (e.g. "patristic", "medieval", "modern").</summary>
    [YamlMember(Alias = "era")]
    public string Era { get; set; } = string.Empty;

    /// <summary>The region slug (e.g. "europe").</summary>
    [YamlMember(Alias = "region")]
    public string Region { get; set; } = string.Empty;

    /// <summary>Free-text patronages shown on cards and the detail page (e.g. ["Missions", "Youth"]). Not a filter facet.</summary>
    [YamlMember(Alias = "patronages")]
    public List<string> Patronages { get; set; } = [];

    /// <summary>The state-of-life slugs (e.g. ["religious", "priest"]).</summary>
    [YamlMember(Alias = "statesOfLife")]
    public List<string> StatesOfLife { get; set; } = [];

    /// <summary>The designation slugs (e.g. ["martyr", "virgin", "doctor-of-the-church"]).</summary>
    [YamlMember(Alias = "designations")]
    public List<string> Designations { get; set; } = [];

    /// <summary>The sex slug ("male" or "female").</summary>
    [YamlMember(Alias = "sex")]
    public string Sex { get; set; } = string.Empty;

    /// <summary>The religious-order slugs (e.g. ["dominican"]).</summary>
    [YamlMember(Alias = "religiousOrders")]
    public List<string> ReligiousOrders { get; set; } = [];

    /// <summary>The canonization-status slugs (e.g. ["canonized"]).</summary>
    [YamlMember(Alias = "canonizations")]
    public List<string> Canonizations { get; set; } = [];

    /// <summary>Free-text life dates (e.g. "1873–1897").</summary>
    [YamlMember(Alias = "dates")]
    public string? Dates { get; set; }

    /// <summary>Optional feast day.</summary>
    [YamlMember(Alias = "feastDay")]
    public string? FeastDay { get; set; }

    /// <summary>Citations / further reading.</summary>
    [YamlMember(Alias = "sources")]
    public List<SaintSourceDto> Sources { get; set; } = [];

    /// <summary>
    /// Optional review status. <c>unvetted</c> marks AI-drafted or not-yet-reviewed content so the detail page renders a standard notice;
    /// any other value (or blank) is treated as vetted. Prefer this flag over hand-written inline HTML notes in the body.
    /// </summary>
    [YamlMember(Alias = "reviewStatus")]
    public string? ReviewStatus { get; set; }

    /// <summary>True when <see cref="ReviewStatus"/> declares the content as not yet reviewed.</summary>
    public bool IsUnvetted => string.Equals(ReviewStatus?.Trim(), "unvetted", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Serialization DTO for a citation declared in front-matter.</summary>
internal sealed class SaintSourceDto
{
    [YamlMember(Alias = "label")]
    public string Label { get; set; } = string.Empty;

    [YamlMember(Alias = "url")]
    public string? Url { get; set; }
}
