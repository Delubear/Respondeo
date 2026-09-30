using YamlDotNet.Serialization;

namespace Respondeo.Content.Shared;

/// <summary>
/// The common metadata every content pillar's front-matter block shares: a stable id, a title, a short summary, and a set of free-text tags.
/// Pillar-specific front-matter DTOs inherit this so the four shared fields stay identical (same YAML aliases, same defaults)
/// across Markdown journey nodes, Discover prayers/articles, and miracles.
/// Pillar-specific fields (categories, topics, sources, translation keys, etc.) are added by the derived classes.
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

    /// <summary>
    /// Optional filing key used for alphabetical sorting in place of the title.
    /// Set this when the natural sort order differs from the display title — for example to drop a leading article
    /// ("The Miracle of Lanciano" -> "Miracle of Lanciano"). Left blank, sorting falls back to the title.
    /// </summary>
    [YamlMember(Alias = "sortKey")]
    public string? SortKey { get; set; }

    /// <summary>Free-text tags for grouping, filtering, and searchable categorization.</summary>
    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>The effective alphabetical-sort value: <see cref="SortKey"/> when set, otherwise the title.</summary>
    public string SortValue => string.IsNullOrWhiteSpace(SortKey) ? Title : SortKey.Trim();
}
