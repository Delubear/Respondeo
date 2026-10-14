using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;

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

        if (!AppLoading().IsMatch(shell))
        {
            Console.Error.WriteLine("error: index.html is missing the app-loading placeholder the entry pages replace with static content");
            return 1;
        }

        File.WriteAllText(Path.Combine(siteDir, "404.html"), RenderNotFound(shell));

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

        var body = StaticBody(meta);
        if (body is not null)
        {
            // Blazor replaces everything inside #app when it boots. The static copy sits beside the spinner and is
            // hidden whenever JavaScript runs (html.js, set inline in the head), so visitors still see the spinner
            // while crawlers and no-JS readers get the page content.
            html = AppLoading().Replace(html, m => m.Value + body, 1);
        }

        return html;
    }

    // The SPA fallback. GitHub Pages can't redirect, so a trailing-slash variant of a real route
    // (e.g. /discover/saints/) lands here; send it to the canonical slash-less URL before Blazor boots.
    internal static string RenderNotFound(string shell)
    {
        const string script =
            "<script>(function () { var p = location.pathname; " +
            "if (p.length > 1 && p.charAt(p.length - 1) === '/') { " +
            "location.replace(p.replace(/\\/+$/, '') + location.search + location.hash); } })();</script>";
        var head = shell.IndexOf("<head>", StringComparison.Ordinal);
        return head < 0 ? shell : shell.Insert(head + "<head>".Length, Environment.NewLine + "    " + script);
    }

    private static readonly MarkdownPipeline Markdown = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    // Builds a readable, crawlable copy of the page: its heading and summary, then the content body where one exists.
    internal static string? StaticBody(PageMeta meta)
    {
        if (string.IsNullOrWhiteSpace(meta.Title))
        {
            return null;
        }

        var html = new StringBuilder();
        html.Append("<main class=\"page static-entry\"><article class=\"content-body\">");
        html.Append($"<h1>{Attr(meta.Title.Trim())}</h1>");
        if (!string.IsNullOrWhiteSpace(meta.Description))
        {
            html.Append($"<p class=\"lede\">{Attr(meta.Description.Trim())}</p>");
        }

        html.Append(SourceBody(meta.SourcePath));
        html.Append("</article></main>");
        return html.ToString();
    }

    private static string SourceBody(string? path)
    {
        if (path is null || !File.Exists(path))
        {
            return string.Empty;
        }

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".md" => Markdig.Markdown.ToHtml(StripFrontMatter(File.ReadAllText(path)), Markdown),
            ".json" when Path.GetFileName(path) != "summa-index.json" => JsonBody(path),
            _ => string.Empty,
        };
    }

    private static string StripFrontMatter(string markdown)
    {
        var match = FrontMatter().Match(markdown);
        return match.Success ? markdown[match.Length..] : markdown;
    }

    // Summa questions carry pre-rendered HTML per article; devotions are step lists. Both become plain sections.
    private static string JsonBody(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var html = new StringBuilder();

        if (root.TryGetProperty("articles", out var articles) && articles.ValueKind == JsonValueKind.Array)
        {
            html.Append(String(root, "prologueHtml"));
            foreach (var article in articles.EnumerateArray())
            {
                html.Append($"<h2>Article {Attr(article.TryGetProperty("number", out var n) ? n.ToString() : string.Empty)}: ");
                html.Append($"{Attr(String(article, "title"))}</h2>");
                html.Append(String(article, "sedContraHtml"));
                html.Append(String(article, "respondeoHtml"));
            }

            return Macro().Replace(html.ToString(), string.Empty);
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var step in property.Value.EnumerateArray())
            {
                if (step.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var title = String(step, "title");
                if (title.Length > 0)
                {
                    html.Append($"<h2>{Attr(title)}</h2>");
                }

                foreach (var key in new[] { "text", "explanation" })
                {
                    var text = String(step, key);
                    if (text.Length > 0)
                    {
                        html.Append(Markdig.Markdown.ToHtml(text, Markdown));
                    }
                }
            }
        }

        return html.ToString();
    }

    private static string String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    [GeneratedRegex(@"<div class=""app-loading""[\s\S]*?<div class=""app-loading__bar"">[\s\S]*?</div>\s*</div>")]
    private static partial Regex AppLoading();

    [GeneratedRegex(@"\A---\r?\n[\s\S]*?\r?\n---\r?\n")]
    private static partial Regex FrontMatter();

    // Summa render macros such as {{scue|respondeo}} that the app expands; crawlers get the plain text.
    [GeneratedRegex(@"\{\{[^}]*\}\}\s?")]
    private static partial Regex Macro();

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
