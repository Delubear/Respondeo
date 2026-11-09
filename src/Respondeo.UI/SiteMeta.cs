namespace Respondeo;

/// <summary>
/// Shared site-wide metadata used for SEO and social sharing (Open Graph / Twitter Cards) and by the build-time sitemap generator.
/// This is the single source of truth for the site's public identity so the static tags in <c>index.html</c>, the per-page <see cref="Components.SeoHead"/> overrides,
/// and the generated <c>sitemap.xml</c> all agree on the canonical origin, name, and default description.
/// </summary>
public static class SiteMeta
{
    /// <summary>The canonical origin (apex domain root) the site is served from, without a trailing slash.</summary>
    public const string BaseUrl = "https://respondeo.faith";

    /// <summary>The site / brand name, as shown in titles and the Open Graph <c>og:site_name</c>.</summary>
    public const string SiteName = "Respondeo";

    /// <summary>The separator between the segments of a page title (a spaced em dash).</summary>
    public const string TitleSeparator = " \u2014 ";

    /// <summary>The site tagline.</summary>
    public const string Tagline = "Real questions. Reasoned answers.";

    /// <summary>The default page title used when a page supplies none.</summary>
    public const string DefaultTitle = SiteName + TitleSeparator + Tagline;

    /// <summary>Joins title segments with <see cref="TitleSeparator"/>, skipping blank ones.</summary>
    public static string JoinTitle(params string?[] parts) => string.Join(TitleSeparator, parts.Where(p => !string.IsNullOrWhiteSpace(p)));

    /// <summary>The full document title for a page: its segments followed by the site name, or <see cref="DefaultTitle"/> when it has none.</summary>
    public static string FullTitle(params string?[] parts)
    {
        var title = JoinTitle(parts);
        return title.Length == 0 ? DefaultTitle : title + TitleSeparator + SiteName;
    }

    /// <summary>
    /// The default meta description, used for the site root and as a fallback for pages that do not
    /// supply their own. Kept under ~160 characters so search engines do not truncate it.
    /// </summary>
    public const string DefaultDescription =
        "Real questions about God, Christ, and the Catholic Church, met with the reasoned answers of St. Thomas Aquinas. Browsable, searchable, and yours offline.";

    /// <summary>The site-relative path to the social preview image used for Open Graph / Twitter Cards.</summary>
    public const string SocialImagePath = "og-image.png";

    /// <summary>The absolute URL of the social preview image (1200x630, sized for large link cards).</summary>
    public const string SocialImageUrl = BaseUrl + "/" + SocialImagePath;

    /// <summary>Alt text describing the social preview image.</summary>
    public const string SocialImageAlt = "Respondeo: Real questions. Reasoned answers.";

    /// <summary>The absolute URL of the square brand logo used as the publisher logo in structured data.</summary>
    public const string LogoUrl = BaseUrl + "/icon-512.png";

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
    /// Serializes a JSON-LD <c>Article</c> node for a content page, using the canonical URL, title, and description so rich results can attribute the piece to the site.
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
                    ["url"] = LogoUrl,
                },
            },
        });

    /// <summary>
    /// Serializes a JSON-LD <c>BreadcrumbList</c> so search engines can show the page's place in the site
    /// (e.g. Respondeo › Summa Theologiae › Question) instead of a bare URL in results. SEO-only; not rendered visibly.
    /// </summary>
    /// <param name="items">Ordered (name, site-relative path) pairs from the root to the current page.</param>
    public static string BreadcrumbJsonLd(IEnumerable<(string Name, string Path)> items) =>
        System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = items.Select((item, index) => new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = index + 1,
                ["name"] = item.Name,
                ["item"] = AbsoluteUrl(item.Path),
            }).ToList(),
        });

    /// <summary>
    /// Serializes a JSON-LD <c>Person</c> node for a saint's detail page, so search engines can model
    /// the subject as a person (name, description, canonical URL) rather than a generic article.
    /// Life dates are intentionally omitted from <c>birthDate</c>/<c>deathDate</c> because the source
    /// value is free text (ranges, BC dates) that cannot be reliably coerced to ISO 8601.
    /// </summary>
    public static string PersonJsonLd(string name, string description, string canonicalUrl) =>
        System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Person",
            ["name"] = name,
            ["description"] = description,
            ["url"] = canonicalUrl,
            ["image"] = SocialImageUrl,
            ["subjectOf"] = new Dictionary<string, object?>
            {
                ["@type"] = "WebSite",
                ["name"] = SiteName,
                ["url"] = BaseUrl + "/",
            },
        });
}
