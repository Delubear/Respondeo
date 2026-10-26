using System.Text;
using System.Text.Json;
using Respondeo.Bible.Contracts;
using Respondeo.Content.Summa;
using Respondeo.Content.Summa.Contracts;

// Build-time sitemap generator for Respondeo.
//
// Invoked by Respondeo.csproj before publish (and build).
// It enumerates every canonical, crawlable route from the content manifests that ship in the site and writes wwwroot/sitemap.xml.
// Slugs are resolved through the Summa part catalog so the emitted URLs always match the app's real routes.
//
// Usage:
//   Respondeo.SitemapGenerator <repoRoot> <outputSitemapPath>
//   Respondeo.SitemapGenerator <repoRoot> --entry-pages <publishedSiteDir>
//
// The --entry-pages mode runs after publish: for every route it writes a copy of the published index.html
// (e.g. discover/saints.html, which GitHub Pages serves at /discover/saints) with that page's own title,
// description, canonical URL, and social tags, so crawlers see a distinct page before the app renders.
//
// Structure: Main parses the arguments, then asks each Collect* method to contribute the routes for one content source into a shared RouteSet.
// The set is sorted and rendered by WriteSitemap. Each source is self-contained so a new content pillar is added by writing one more Collect* method.

return SitemapGenerator.Run(args);

internal static class SitemapGenerator
{
    private const string BaseUrl = "https://respondeo.faith";
    private const string SiteName = "Respondeo";
    private const string TitleSeparator = " \u2014 ";

    // The catalog is pure and stateless, so the build-time tool simply news one up rather than resolving it from DI.
    private static readonly ISummaPartCatalog Parts = new SummaPartCatalog();

    public static int Run(string[] args)
    {
        if (args.Length < 2 || (args[1] == "--entry-pages" && args.Length < 3))
        {
            Console.Error.WriteLine("Usage: Respondeo.SitemapGenerator <repoRoot> <outputSitemapPath>");
            Console.Error.WriteLine("       Respondeo.SitemapGenerator <repoRoot> --entry-pages <publishedSiteDir>");
            return 1;
        }

        var repoRoot = Path.GetFullPath(args[0]);
        var paths = ContentPaths.ForRepo(repoRoot);

        var routes = new RouteSet(GitDates.Load(repoRoot));
        CollectStaticRoutes(routes);
        CollectSummaRoutes(routes, paths.SummaIndex);
        CollectBibleRoutes(routes, repoRoot);
        CollectMiracleRoutes(routes, paths.DiscoverRoot, paths.MiraclesManifest);
        CollectSaintRoutes(routes, paths.SaintsManifest);
        CollectJourneyRoutes(routes, paths.ContentRoot, paths.ContentManifest);
        CollectDiscoverRoutes(routes, paths.DiscoverRoot, paths.PrayersManifest, paths.DevotionsManifest, paths.ArticlesManifest);

        return args[1] == "--entry-pages"
            ? EntryPages.Write(routes, Path.GetFullPath(args[2]))
            : WriteSitemap(routes, Path.GetFullPath(args[1]));
    }

    // 1. Top-level static routes that always exist regardless of content.
    // Titles and descriptions mirror each page's <PageShell> so the pre-render shell matches what the app later sets.
    private static void CollectStaticRoutes(RouteSet routes)
    {
        routes.Add("");            // Home keeps index.html's own site-wide title and description.
        routes.Add("summa", "Summa Theologiae",
            "Read St. Thomas Aquinas's Summa Theologiae online \u2014 browse by part and question, with cross-references and plain-language guidance.");
        routes.Add("bible", "Holy Bible",
            "Read the Douay-Rheims Bible online \u2014 all 73 books of the Catholic canon plus the Clementine Vulgate appendix.");
        routes.Add("discover", "Discover Catholicism",
            "Explore the riches of the Catholic faith: recognized miracles, the lives of the saints, a treasury of prayers, interactive devotions, and articles on the Church's teaching and practice.");
        routes.Add("discover/miracles", "Catholic Miracles",
            "A growing catalog of reported and recognized Catholic miracles \u2014 Eucharistic miracles, Marian apparitions, and healings \u2014 with the Church's judgment on each.");
        routes.Add("discover/saints", "Catholic Saints",
            "A growing gallery of the saints \u2014 their lives, patronages, state of life, and the Church's recognition of their holiness, from the early Church to our own day.");
        routes.Add("discover/prayers", "Catholic Prayers",
            "A treasury of the Church's traditional prayers \u2014 the Our Father, Hail Mary, Glory Be, Apostles' Creed, and more, with the Latin alongside.");
        routes.Add("discover/devotions", "Catholic Devotions and Sacraments",
            "Pray the Rosary and the chaplets bead by bead, and walk through the Mass and Confession step by step with an interactive guide.");
        routes.Add("discover/articles", "Living the Faith" + TitleSeparator + "Articles",
            "Deeper dives into the Catholic life \u2014 Confession, the Eucharist, and the Church's teaching \u2014 the why beneath the practice.");
        routes.Add("thanks", "Thanks & Sources",
            "The inspiration behind Respondeo, the public-domain and open sources it draws on, and further reading we recommend for exploring the Catholic faith.");
    }

    // Every Bible book chapter, driven by the same translation manifest the app routes with.
    private static void CollectBibleRoutes(RouteSet routes, string repoRoot)
    {
        Respondeo.Bible.BibleCatalog.LoadFromDirectory(Path.Combine(repoRoot, "src", "Respondeo.Bible", "wwwroot"));
        var translation = BibleTranslation.DouayRheims;
        foreach (var book in translation.Books())
        {
            var name = translation.BookName(book);
            for (var chapter = 1; chapter <= translation.ChapterCount(book); chapter++)
            {
                routes.Add(translation.ChapterHref(book, chapter), JoinTitle($"{name} {chapter}", "Holy Bible"),
                    $"Read {name} chapter {chapter} in the Douay-Rheims Bible.");
            }
        }
    }

    // 2. Summa part landing pages
    private static void CollectSummaRoutes(RouteSet routes, string summaIndexPath)
    {
        if (!File.Exists(summaIndexPath))
        {
            Console.Error.WriteLine($"warning: Summa index not found at {summaIndexPath}; skipping Summa routes.");
            return;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(summaIndexPath));
        if (!doc.RootElement.TryGetProperty("parts", out var parts))
        {
            return;
        }

        foreach (var part in parts.EnumerateArray())
        {
            var partKey = part.GetProperty("id").GetString() ?? string.Empty;
            var partTitle = part.TryGetProperty("title", out var pt) ? pt.GetString() ?? string.Empty : string.Empty;
            routes.Add($"summa/part/{Parts.SlugForKey(partKey)}", JoinTitle(partTitle, "Summa Theologiae"), null, summaIndexPath);

            if (!part.TryGetProperty("questions", out var questions))
            {
                continue;
            }

            foreach (var question in questions.EnumerateArray())
            {
                var questionId = question.GetProperty("id").GetString();
                if (!string.IsNullOrWhiteSpace(questionId))
                {
                    var title = question.TryGetProperty("title", out var t) ? t.GetString() : null;
                    var questionFile = Directory.EnumerateFiles(Path.GetDirectoryName(summaIndexPath)!, questionId + ".json", SearchOption.AllDirectories)
                        .FirstOrDefault();
                    routes.Add($"summa/{ToQuestionSlug(questionId)}", title, SummaQuestionDescription(question, title, partTitle), questionFile);
                }
            }
        }
    }

    // 3. Miracle detail pages. The id is the file name without its .md extension.
    // Miracles are listed in miracles/miracles-manifest.json under a "files" array.
    private static void CollectMiracleRoutes(RouteSet routes, string discoverRoot, string miraclesManifestPath)
    {
        if (!File.Exists(miraclesManifestPath))
        {
            Console.Error.WriteLine($"warning: Miracles manifest not found at {miraclesManifestPath}; skipping miracle routes.");
            return;
        }

        foreach (var file in ReadManifestFiles(miraclesManifestPath))
        {
            var fullPath = Path.Combine(discoverRoot, "miracles", file);
            routes.Add($"discover/miracles/{Path.GetFileNameWithoutExtension(file)}", ReadTitle(fullPath), ReadSummary(fullPath), fullPath);
        }
    }

    // 3b. Saint detail pages. The id is the file name without its .md extension.
    // Saints are listed in saints/saints-manifest.json under a "files" array.
    private static void CollectSaintRoutes(RouteSet routes, string saintsManifestPath)
    {
        if (!File.Exists(saintsManifestPath))
        {
            Console.Error.WriteLine($"warning: Saints manifest not found at {saintsManifestPath}; skipping saint routes.");
            return;
        }

        foreach (var file in ReadManifestFiles(saintsManifestPath))
        {
            var fullPath = Path.Combine(Path.GetDirectoryName(saintsManifestPath)!, file);
            routes.Add($"discover/saints/{Path.GetFileNameWithoutExtension(file)}", ReadTitle(fullPath), ReadSummary(fullPath), fullPath);
        }
    }

    // 4. Journey content nodes + their stage landing pages.
    // A node's stage is its content sub-folder; its id comes from the front matter. Every node belongs to a stage, so a root-level node is a content error and is skipped with a warning.
    private static void CollectJourneyRoutes(RouteSet routes, string contentRoot, string contentManifestPath)
    {
        if (!File.Exists(contentManifestPath))
        {
            Console.Error.WriteLine($"warning: Content manifest not found at {contentManifestPath}; skipping journey routes.");
            return;
        }

        // Stage landing pages (Stage.razor: "/{Slug}") are emitted after the nodes, once per stage,
        // so collect the stages we encounter while walking the nodes and add them at the end.
        var stages = new RouteSet(routes.Dates);

        foreach (var file in ReadManifestFiles(contentManifestPath))
        {
            var fullPath = Path.Combine(contentRoot, file.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var id = ReadFrontMatterId(fullPath);
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var slashIndex = file.IndexOf('/');
            var stage = slashIndex > 0 ? file[..slashIndex] : null;
            if (string.IsNullOrEmpty(stage))
            {
                Console.Error.WriteLine($"warning: Node '{id}' ({file}) is not in a stage folder; skipping (every node must belong to a stage).");
                continue;
            }

            stages.Add(stage);
            routes.Add($"{stage}/{id}", ReadTitle(fullPath), ReadSummary(fullPath), fullPath);

            // A stage's landing page (/{stage}) renders the node whose id is the stage slug.
            if (string.Equals(id, stage, StringComparison.OrdinalIgnoreCase))
            {
                stages.Add(stage, ReadTitle(fullPath), ReadSummary(fullPath), fullPath);
            }
        }

        foreach (var stage in stages)
        {
            routes.Add(stage, stages.MetaFor(stage));
        }
    }

    // 5. Discover detail pages: prayers, devotions, and articles.
    // Each content type ships its own manifest with a single "files" list.
    // Prayer ids come from the markdown front matter, but only primary-language (non-Latin), listed prayers get their own browse/detail route;
    // Latin translations are shown inline on the English prayer's page, and unlisted contextual prayers are hidden, so both are excluded here.
    // Devotion ids come from the JSON "id" field, article ids from the markdown front matter.
    private static void CollectDiscoverRoutes(
        RouteSet routes,
        string discoverRoot,
        string prayersManifestPath,
        string devotionsManifestPath,
        string articlesManifestPath)
    {
        CollectPrayerRoutes(routes, discoverRoot, prayersManifestPath);
        CollectDevotionRoutes(routes, discoverRoot, devotionsManifestPath);
        CollectArticleRoutes(routes, discoverRoot, articlesManifestPath);
    }

    private static void CollectPrayerRoutes(RouteSet routes, string discoverRoot, string prayersManifestPath)
    {
        if (!File.Exists(prayersManifestPath))
        {
            Console.Error.WriteLine($"warning: Prayers manifest not found at {prayersManifestPath}; skipping prayer routes.");
            return;
        }

        foreach (var file in ReadManifestFiles(prayersManifestPath))
        {
            var fullPath = Path.Combine(discoverRoot, "prayers", file);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var language = ReadFrontMatterValue(fullPath, "language") ?? "en";
            if (string.Equals(language, "la", StringComparison.OrdinalIgnoreCase))
            {
                continue; // Latin translations render inline on the English page, not their own route.
            }

            var unlisted = ReadFrontMatterValue(fullPath, "unlisted");
            if (string.Equals(unlisted, "true", StringComparison.OrdinalIgnoreCase))
            {
                continue; // Contextual prayers (e.g. Dominican versicles) are hidden from the catalog and crawlers.
            }

            var id = ReadFrontMatterId(fullPath);
            if (!string.IsNullOrWhiteSpace(id))
            {
                routes.Add($"discover/prayers/{id}", ReadTitle(fullPath), ReadSummary(fullPath), fullPath);
            }
        }
    }

    private static void CollectDevotionRoutes(RouteSet routes, string discoverRoot, string devotionsManifestPath)
    {
        if (!File.Exists(devotionsManifestPath))
        {
            Console.Error.WriteLine($"warning: Devotions manifest not found at {devotionsManifestPath}; skipping devotion routes.");
            return;
        }

        foreach (var file in ReadManifestFiles(devotionsManifestPath))
        {
            var fullPath = Path.Combine(discoverRoot, "devotions", file);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var id = ReadJsonId(fullPath);
            if (!string.IsNullOrWhiteSpace(id))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(fullPath));
                routes.Add($"discover/devotions/{id}", ReadJsonString(doc, "title"), ReadJsonString(doc, "summary"), fullPath);
            }
        }
    }

    private static void CollectArticleRoutes(RouteSet routes, string discoverRoot, string articlesManifestPath)
    {
        if (!File.Exists(articlesManifestPath))
        {
            Console.Error.WriteLine($"warning: Articles manifest not found at {articlesManifestPath}; skipping article routes.");
            return;
        }

        foreach (var file in ReadManifestFiles(articlesManifestPath))
        {
            var fullPath = Path.Combine(discoverRoot, "articles", file);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var id = ReadFrontMatterId(fullPath);
            if (!string.IsNullOrWhiteSpace(id))
            {
                routes.Add($"discover/articles/{id}", ReadTitle(fullPath), ReadSummary(fullPath), fullPath);
            }
        }
    }

    // Renders the collected routes to sitemap.xml, skipping the write when nothing changed so the build's up-to-date checks and Git stay quiet.
    private static int WriteSitemap(RouteSet routes, string outputPath)
    {
        // Deterministic ordering (sorted) keeps diffs small across regenerations.
        var ordered = routes.Sorted();

        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.AppendLine("<!-- Generated by Respondeo.SitemapGenerator at build time. Do not edit by hand. -->");
        xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        foreach (var route in ordered)
        {
            var loc = route.Length == 0 ? $"{BaseUrl}/" : $"{BaseUrl}/{route}";
            xml.AppendLine("  <url>");
            xml.AppendLine($"    <loc>{Escape(loc)}</loc>");
            if (routes.MetaFor(route).LastModified is { } lastModified)
            {
                xml.AppendLine($"    <lastmod>{lastModified:yyyy-MM-dd}</lastmod>");
            }

            xml.AppendLine("  </url>");
        }
        xml.AppendLine("</urlset>");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var content = xml.ToString();
        if (File.Exists(outputPath) && File.ReadAllText(outputPath) == content)
        {
            Console.WriteLine($"Sitemap unchanged ({ordered.Count} urls): {outputPath}");
            return 0;
        }

        File.WriteAllText(outputPath, content);
        Console.WriteLine($"Sitemap written ({ordered.Count} urls): {outputPath}");
        return 0;
    }

    // Converts a Summa storage id (e.g. "p1-q001") to its URL slug (e.g. "prima-q001").
    private static string ToQuestionSlug(string questionId)
    {
        var dash = questionId.IndexOf('-');
        if (dash <= 0)
        {
            return questionId;
        }

        var key = questionId[..dash];
        var rest = questionId[(dash + 1)..];
        return $"{Parts.SlugForKey(key)}-{rest}";
    }

    // Reads the "files" array from a { "files": [ ... ] } manifest.
    private static IEnumerable<string> ReadManifestFiles(string manifestPath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        if (!doc.RootElement.TryGetProperty("files", out var files))
        {
            yield break;
        }

        foreach (var file in files.EnumerateArray())
        {
            var value = file.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }
    }

    // Reads the "id:" value from a markdown file's YAML front matter without a full YAML parse.
    private static string? ReadFrontMatterId(string path) => ReadFrontMatterValue(path, "id");

    // Reads a named scalar value (e.g. "id" or "language") from a markdown file's YAML front matter without a full YAML parse.
    private static string? ReadFrontMatterValue(string path, string key)
    {
        var prefix = key + ":";
        var inFrontMatter = false;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.TrimEnd();
            if (line == "---")
            {
                if (!inFrontMatter)
                {
                    inFrontMatter = true;
                    continue;
                }

                break; // End of front matter.
            }

            if (inFrontMatter && line.StartsWith(prefix, StringComparison.Ordinal))
            {
                return line[prefix.Length..].Trim().Trim('"', '\'');
            }
        }

        return null;
    }

    // Reads the "id" property from a devotion JSON file.
    private static string? ReadJsonId(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
    }

    private static string? ReadJsonString(JsonDocument doc, string name) =>
        doc.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string? ReadTitle(string path) => File.Exists(path) ? ReadFrontMatterValue(path, "title") : null;

    private static string? ReadSummary(string path) => File.Exists(path) ? ReadFrontMatterValue(path, "summary") : null;

    private static string JoinTitle(params string?[] parts) =>
        string.Join(TitleSeparator, parts.Where(p => !string.IsNullOrWhiteSpace(p)));

    // Mirrors SummaQuestionPresenter.BuildSeoDescription so the pre-render shell matches the app's description.
    private static string? SummaQuestionDescription(JsonElement question, string? title, string partTitle)
    {
        if (title is null)
        {
            return null;
        }

        if (question.TryGetProperty("isPrologue", out var p) && p.ValueKind == JsonValueKind.True)
        {
            return $"{partTitle} \u00B7 Prologue: {title}. From St. Thomas Aquinas's Summa Theologiae.";
        }

        var articles = question.TryGetProperty("articles", out var a) && a.ValueKind == JsonValueKind.Array
            ? string.Join("; ", a.EnumerateArray().Select(x => x.TryGetProperty("title", out var at) ? at.GetString() : null).Where(s => !string.IsNullOrWhiteSpace(s)))
            : string.Empty;
        var number = question.TryGetProperty("number", out var n) ? n.ToString() : string.Empty;

        return string.IsNullOrWhiteSpace(articles)
            ? $"{partTitle} \u00B7 Question {number}: {title}. From St. Thomas Aquinas's Summa Theologiae."
            : $"{title} \u2014 St. Thomas Aquinas asks: {articles}.";
    }

    // The full document title for a page, matching SiteMeta.FullTitle in the app.
    internal static string FullTitle(string title) => title + TitleSeparator + SiteName;

    internal static string AbsoluteUrl(string route) => route.Length == 0 ? $"{BaseUrl}/" : $"{BaseUrl}/{route}";

    private static string Escape(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");
}

// The absolute paths to every content manifest and root the generator reads, derived once from the repo root so the individual Collect* methods do not each re-assemble them.
internal readonly record struct ContentPaths(
    string SummaIndex,
    string ContentRoot,
    string ContentManifest,
    string DiscoverRoot,
    string PrayersManifest,
    string DevotionsManifest,
    string ArticlesManifest,
    string MiraclesManifest,
    string SaintsManifest)
{
    public static ContentPaths ForRepo(string repoRoot)
    {
        var contentRoot = Path.Combine(repoRoot, "src", "Respondeo.Content", "wwwroot", "inquiry");
        var discoverRoot = Path.Combine(repoRoot, "src", "Respondeo.Content", "wwwroot", "discover");

        return new ContentPaths(
            SummaIndex: Path.Combine(repoRoot, "src", "Respondeo.Content.Summa", "wwwroot", "summa", "summa-index.json"),
            ContentRoot: contentRoot,
            ContentManifest: Path.Combine(contentRoot, "manifest.json"),
            DiscoverRoot: discoverRoot,
            PrayersManifest: Path.Combine(discoverRoot, "prayers", "prayers-manifest.json"),
            DevotionsManifest: Path.Combine(discoverRoot, "devotions", "devotions-manifest.json"),
            ArticlesManifest: Path.Combine(discoverRoot, "articles", "articles-manifest.json"),
            MiraclesManifest: Path.Combine(discoverRoot, "miracles", "miracles-manifest.json"),
            SaintsManifest: Path.Combine(discoverRoot, "saints", "saints-manifest.json"));
    }
}

// A page's own title (without the site suffix) and meta description; either may be null to keep the site default.
// SourcePath is the content file the page renders (used for its static body), and LastModified its last commit date.
internal readonly record struct PageMeta(string? Title, string? Description, string? SourcePath = null, DateTimeOffset? LastModified = null);

// An insertion-ordered, de-duplicating collection of relative routes, each with optional page metadata.
// Adding the same route twice keeps the first entry but fills in metadata the first add lacked; leading slashes are normalized away so "/summa" and "summa" are the same route.
internal sealed class RouteSet(GitDates dates)
{
    private readonly List<string> _routes = [];
    private readonly Dictionary<string, PageMeta> _meta = new(StringComparer.Ordinal);

    public GitDates Dates => dates;

    public void Add(string relative) => Add(relative, null, null);

    public void Add(string relative, PageMeta meta) => Add(relative, meta.Title, meta.Description, meta.SourcePath);

    public void Add(string relative, string? title, string? description, string? sourcePath = null)
    {
        var normalized = relative.TrimStart('/');
        var lastModified = dates.For(sourcePath);
        if (_meta.TryGetValue(normalized, out var existing))
        {
            _meta[normalized] = new PageMeta(
                existing.Title ?? title,
                existing.Description ?? description,
                existing.SourcePath ?? sourcePath,
                existing.LastModified ?? lastModified);
            return;
        }

        _routes.Add(normalized);
        _meta[normalized] = new PageMeta(title, description, sourcePath, lastModified);
    }

    public PageMeta MetaFor(string route) => _meta.GetValueOrDefault(route);

    public IEnumerator<string> GetEnumerator() => _routes.GetEnumerator();

    // Returns the routes in deterministic ordinal order for stable sitemap diffs.
    public List<string> Sorted()
    {
        var ordered = new List<string>(_routes);
        ordered.Sort(StringComparer.Ordinal);
        return ordered;
    }
}
