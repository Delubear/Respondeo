using System.Net;
using System.Text.RegularExpressions;

// Writes one copy of the published index.html per route (e.g. discover/saints.html, served by GitHub Pages at /discover/saints)
// with that page's own title, meta description, canonical URL, and Open Graph / Twitter tags.
// The app is client-rendered, so without this every route's raw HTML is identical to the home page and crawlers treat them as duplicates.
// Values are HTML-encoded here in C# rather than substituted with sed, so titles containing &, quotes, or < survive intact.
internal static partial class EntryPages
{
    public static int Write(RouteSet routes, string siteDir)
    {
        var indexPath = Path.Combine(siteDir, "index.html");
        if (!File.Exists(indexPath))
        {
            Console.Error.WriteLine($"error: published index.html not found at {indexPath}");
            return 1;
        }

        var shell = File.ReadAllText(indexPath);

        // Fail loudly if index.html drifts so a tag silently stops being rewritten.
        foreach (var (name, pattern) in Patterns)
        {
            if (!pattern.IsMatch(shell))
            {
                Console.Error.WriteLine($"error: index.html is missing the {name} tag the entry pages rewrite");
                return 1;
            }
        }

        var count = 0;
        foreach (var route in routes.Sorted())
        {
            if (route.Length == 0)
            {
                continue; // The home page is index.html itself.
            }

            var target = Path.Combine(siteDir, route.Replace('/', Path.DirectorySeparatorChar) + ".html");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, Render(shell, route, routes.MetaFor(route)));
            count++;
        }

        Console.WriteLine($"Wrote {count} route entry pages: {siteDir}");
        return count > 0 ? 0 : 1;
    }

    internal static string Render(string shell, string route, PageMeta meta)
    {
        var url = Attr(SitemapGenerator.AbsoluteUrl(route));
        var html = Canonical().Replace(shell, $"<link rel=\"canonical\" href=\"{url}\" />", 1);
        html = OgUrl().Replace(html, $"<meta property=\"og:url\" content=\"{url}\" />", 1);

        if (!string.IsNullOrWhiteSpace(meta.Title))
        {
            var title = Attr(SitemapGenerator.FullTitle(meta.Title.Trim()));
            html = Title().Replace(html, $"<title>{title}</title>", 1);
            html = OgTitle().Replace(html, $"<meta property=\"og:title\" content=\"{title}\" />", 1);
            html = TwitterTitle().Replace(html, $"<meta name=\"twitter:title\" content=\"{title}\" />", 1);
        }

        if (!string.IsNullOrWhiteSpace(meta.Description))
        {
            var description = Attr(meta.Description.Trim());
            html = Description().Replace(html, $"<meta name=\"description\" content=\"{description}\" />", 1);
            html = OgDescription().Replace(html, $"<meta property=\"og:description\" content=\"{description}\" />", 1);
            html = TwitterDescription().Replace(html, $"<meta name=\"twitter:description\" content=\"{description}\" />", 1);
        }

        return html;
    }

    // Front matter values may already contain entities (e.g. &mdash;); decode first so they are not double-encoded.
    private static string Attr(string value) => WebUtility.HtmlEncode(WebUtility.HtmlDecode(value));

    private static readonly (string Name, Regex Pattern)[] Patterns =
    [
        ("<title>", Title()),
        ("canonical", Canonical()),
        ("description", Description()),
        ("og:url", OgUrl()),
        ("og:title", OgTitle()),
        ("og:description", OgDescription()),
        ("twitter:title", TwitterTitle()),
        ("twitter:description", TwitterDescription()),
    ];

    [GeneratedRegex(@"<title>[^<]*</title>")]
    private static partial Regex Title();

    [GeneratedRegex(@"<link rel=""canonical"" href=""[^""]*"" />")]
    private static partial Regex Canonical();

    [GeneratedRegex(@"<meta name=""description"" content=""[^""]*"" />")]
    private static partial Regex Description();

    [GeneratedRegex(@"<meta property=""og:url"" content=""[^""]*"" />")]
    private static partial Regex OgUrl();

    [GeneratedRegex(@"<meta property=""og:title"" content=""[^""]*"" />")]
    private static partial Regex OgTitle();

    [GeneratedRegex(@"<meta property=""og:description"" content=""[^""]*"" />")]
    private static partial Regex OgDescription();

    [GeneratedRegex(@"<meta name=""twitter:title"" content=""[^""]*"" />")]
    private static partial Regex TwitterTitle();

    [GeneratedRegex(@"<meta name=""twitter:description"" content=""[^""]*"" />")]
    private static partial Regex TwitterDescription();
}
