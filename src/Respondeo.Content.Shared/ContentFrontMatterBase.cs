using YamlDotNet.Serialization;

namespace Respondeo.Content.Shared;

/// <summary>
/// The common metadata every content pillar's front-matter block shares: a stable id, a title, a
/// short summary, and a set of free-text tags. Pillar-specific front-matter DTOs inherit this so the
/// four shared fields stay identical (same YAML aliases, same defaults) across Markdown journey
/// nodes, Credo prayers/articles, and miracles. Pillar-specific fields (categories, topics, sources,
/// translation keys, etc.) are added by the derived classes.
/// </summary>
public abstract class ContentFrontMatterBase
{
    /// <summary>Stable unique id / URL slug.</summary>
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Headline shown on the detail page and in cards.</summary>
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Short one-line description used on cards and previews.</summary>
    [YamlMember(Alias = "summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>Free-text tags for grouping, filtering, and searchable categorization.</summary>
    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } = [];
}
