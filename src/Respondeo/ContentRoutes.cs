using Respondeo.Content.Contracts;

namespace Respondeo;

/// <summary>
/// Builds the relative URLs for content in one place, so every link site (cards, breadcrumbs, branch links,
/// discover pages) stays consistent. Lives in the app because these are the app's own Blazor <c>@page</c>
/// routes; the content libraries have no knowledge of the URL scheme.
/// Inquiry nodes are always nested under their stage slug (e.g. <c>why-god/aquinas-five-ways</c>)
/// so the URL reflects the section and the masthead tab lights up via its prefix match. Every node
/// belongs to a stage (its content sub-folder), so there is no stageless node route.
/// </summary>
public static class ContentRoutes
{
    /// <summary>Builds the href for a node, nesting it under its stage.</summary>
    public static string NodeHref(InquiryNode node) => NodeHref(node.Id, node.Stage);

    /// <summary>
    /// Builds the href for a node id and its stage. Every inquiry node belongs to a stage, so a missing
    /// stage is a content error rather than a flat-route fallback.
    /// </summary>
    public static string NodeHref(string id, string? stage)
    {
        if (string.IsNullOrEmpty(stage))
        {
            throw new ArgumentException($"Node '{id}' has no stage; every inquiry node must belong to a stage folder.", nameof(stage));
        }

        return $"{stage}/{id}";
    }

    /// <summary>Builds the href for a prayer detail page.</summary>
    public static string PrayerHref(string id) => $"discover/prayers/{id}";

    /// <summary>Builds the href for a saint detail page.</summary>
    public static string SaintHref(string id) => $"discover/saints/{id}";

    /// <summary>Builds the href for a miracle detail page.</summary>
    public static string MiracleHref(string id) => $"discover/miracles/{id}";

    /// <summary>Builds the href for a devotion detail page.</summary>
    public static string DevotionHref(string id) => $"discover/devotions/{id}";

    /// <summary>Builds the href for an article detail page.</summary>
    public static string ArticleHref(string id) => $"discover/articles/{id}";
}
