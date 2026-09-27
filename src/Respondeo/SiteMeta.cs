namespace Respondeo;

/// <summary>
/// Shared site-wide metadata used for SEO and social sharing (Open Graph / Twitter Cards) and by the
/// build-time sitemap generator. This is the single source of truth for the site's public identity so
/// the static tags in <c>index.html</c>, the per-page <see cref="Components.SeoHead"/> overrides, and
/// the generated <c>sitemap.xml</c> all agree on the canonical origin, name, and default description.
/// </summary>
public static class SiteMeta
{
    /// <summary>The canonical origin (apex domain root) the site is served from, without a trailing slash.</summary>
    public const string BaseUrl = "https://respondeo.faith";

    /// <summary>The site / brand name, as shown in titles and the Open Graph <c>og:site_name</c>.</summary>
    public const string SiteName = "Respondeo";

    /// <summary>The default page title used when a page supplies none.</summary>
    public const string DefaultTitle = "Respondeo — Real questions. Reasoned answers.";

    /// <summary>
    /// The default meta description, used for the site root and as a fallback for pages that do not
    /// supply their own. Kept under ~160 characters so search engines do not truncate it.
    /// </summary>
    public const string DefaultDescription =
        "The complete Summa Theologiae of St. Thomas Aquinas, plus reasoned answers to real questions about God, Christ, and the Church — browsable, searchable, and fully offline.";

    /// <summary>The site-relative path to the social preview image used for Open Graph / Twitter Cards.</summary>
    public const string SocialImagePath = "icon-512.png";

    /// <summary>The absolute URL of the social preview image.</summary>
    public const string SocialImageUrl = BaseUrl + "/" + SocialImagePath;

    /// <summary>Builds an absolute URL from a site-relative path (e.g. <c>summa/prima-q002</c>).</summary>
    public static string AbsoluteUrl(string relativePath)
    {
        var trimmed = relativePath.TrimStart('/');
        return trimmed.Length == 0 ? BaseUrl + "/" : $"{BaseUrl}/{trimmed}";
    }

    /// <summary>
    /// Serializes a JSON-LD <c>WebSite</c> node for the site root, advertising the brand and the
    /// in-app search endpoint so search engines can surface a sitelinks search box.
    /// </summary>
    public static string WebSiteJsonLd() =>
        System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["name"] = SiteName,
            ["url"] = BaseUrl + "/",
            ["description"] = DefaultDescription,
        });

    /// <summary>
    /// Serializes a JSON-LD <c>Article</c> node for a content page, using the canonical URL, title,
    /// and description so rich results can attribute the piece to the site.
    /// </summary>
    public static string ArticleJsonLd(string title, string description, string canonicalUrl) =>
        System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Article",
            ["headline"] = title,
            ["description"] = description,
            ["url"] = canonicalUrl,
            ["image"] = SocialImageUrl,
            ["isPartOf"] = new Dictionary<string, object?>
            {
                ["@type"] = "WebSite",
                ["name"] = SiteName,
                ["url"] = BaseUrl + "/",
            },
            ["publisher"] = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = SiteName,
                ["logo"] = new Dictionary<string, object?>
                {
                    ["@type"] = "ImageObject",
                    ["url"] = SocialImageUrl,
                },
            },
        });
}
