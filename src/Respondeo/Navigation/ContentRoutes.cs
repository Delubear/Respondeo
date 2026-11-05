using Respondeo.Content.Contracts;
using Respondeo.Services.Summa;

namespace Respondeo.Navigation;

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
    /// <summary>The Discover hub.</summary>
    public const string Discover = "discover";

    /// <summary>The articles list.</summary>
    public const string Articles = "discover/articles";

    /// <summary>The miracles list.</summary>
    public const string Miracles = "discover/miracles";

    /// <summary>The saints list.</summary>
    public const string Saints = "discover/saints";

    /// <summary>The prayers list.</summary>
    public const string Prayers = "discover/prayers";

    /// <summary>The devotions list.</summary>
    public const string Devotions = "discover/devotions";

    /// <summary>The Liturgy of the Hours list.</summary>
    public const string Hours = "discover/hours";

    /// <summary>The Bible landing page.</summary>
    public const string Bible = "bible";

    /// <summary>The Summa landing page.</summary>
    public const string Summa = "summa";

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
    public static string PrayerHref(string id) => $"{Prayers}/{id}";

    /// <summary>Builds the href for a saint detail page.</summary>
    public static string SaintHref(string id) => $"{Saints}/{id}";

    /// <summary>Builds the href for a miracle detail page.</summary>
    public static string MiracleHref(string id) => $"{Miracles}/{id}";

    /// <summary>Builds the href for a devotion detail page.</summary>
    public static string DevotionHref(string id) => $"{Devotions}/{id}";

    /// <summary>Builds the href for an article detail page.</summary>
    public static string ArticleHref(string id) => $"{Articles}/{id}";

    /// <summary>Builds the href for a Summa question page from its URL id.</summary>
    public static string SummaHref(string urlId) => $"{Summa}/{urlId}";

    /// <summary>Builds the href for a specific article within a Summa question page.</summary>
    public static string SummaHref(string urlId, int articleNumber) => $"{Summa}/{urlId}#{SummaFragment.ArticleAnchor(articleNumber)}";

    /// <summary>Builds the href for a Summa question page with an explicit fragment (without the leading '#').</summary>
    public static string SummaHref(string urlId, string fragment) => $"{Summa}/{urlId}#{fragment}";

    /// <summary>Builds the href for a Liturgy of the Hours page.</summary>
    public static string HourHref(string slug) => $"{Hours}/{slug}";
}
