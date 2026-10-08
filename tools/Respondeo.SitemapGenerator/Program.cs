using System.Text;
using System.Text.Json;
using Respondeo.Content.Summa;
using Respondeo.Content.Summa.Contracts;

// Build-time sitemap generator for Respondeo.
//
// Invoked by Respondeo.csproj before publish (and build).
// It enumerates every canonical, crawlable route from the content manifests that ship in the site and writes wwwroot/sitemap.xml.
// Slugs are resolved through the Summa part catalog so the emitted URLs always match the app's real routes.
//
// Usage: Respondeo.SitemapGenerator <repoRoot> <outputSitemapPath>
//
// Structure: Main parses the arguments, then asks each Collect* method to contribute the routes for one content source into a shared RouteSet.
// The set is sorted and rendered by WriteSitemap. Each source is self-contained so a new content pillar is added by writing one more Collect* method.

return SitemapGenerator.Run(args);

internal static class SitemapGenerator
{
    private const string BaseUrl = "https://respondeo.faith";

    // The catalog is pure and stateless, so the build-time tool simply news one up rather than resolving it from DI.
    private static readonly ISummaPartCatalog Parts = new SummaPartCatalog();

    public static int Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: Respondeo.SitemapGenerator <repoRoot> <outputSitemapPath>");
            return 1;
        }

        var repoRoot = Path.GetFullPath(args[0]);
        var outputPath = Path.GetFullPath(args[1]);
        var paths = ContentPaths.ForRepo(repoRoot);

        var routes = new RouteSet();
        CollectStaticRoutes(routes);
        CollectSummaRoutes(routes, paths.SummaIndex);
        CollectMiracleRoutes(routes, paths.DiscoverRoot, paths.MiraclesManifest);
        CollectSaintRoutes(routes, paths.SaintsManifest);
        CollectJourneyRoutes(routes, paths.ContentRoot, paths.ContentManifest);
        CollectDiscoverRoutes(routes, paths.DiscoverRoot, paths.PrayersManifest, paths.DevotionsManifest, paths.ArticlesManifest);

        return WriteSitemap(routes, outputPath);
    }

    // 1. Top-level static routes that always exist regardless of content.
    private static void CollectStaticRoutes(RouteSet routes)
    {
        routes.Add("");            // Home
        routes.Add("summa");
        routes.Add("discover");
        routes.Add("discover/miracles");
        routes.Add("discover/saints");
        routes.Add("discover/prayers");
        routes.Add("discover/devotions");
        routes.Add("discover/articles");
        routes.Add("thanks");
    }

    // 2. Summa part landing pages + every question, mapping storage keys (p1) to URL slugs (prima).
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
            routes.Add($"summa/part/{Parts.SlugForKey(partKey)}");

            if (!part.TryGetProperty("questions", out var questions))
            {
                continue;
            }

            foreach (var question in questions.EnumerateArray())
            {
                var questionId = question.GetProperty("id").GetString();
                if (!string.IsNullOrWhiteSpace(questionId))
                {
                    routes.Add($"summa/{ToQuestionSlug(questionId)}");
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
            routes.Add($"discover/miracles/{Path.GetFileNameWithoutExtension(file)}");
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
            routes.Add($"discover/saints/{Path.GetFileNameWithoutExtension(file)}");
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
        var stages = new RouteSet();

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
            routes.Add($"{stage}/{id}");
        }

        foreach (var stage in stages)
        {
            routes.Add(stage);
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
                routes.Add($"discover/prayers/{id}");
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
                routes.Add($"discover/devotions/{id}");
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
                routes.Add($"discover/articles/{id}");
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

// An insertion-ordered, de-duplicating collection of relative routes.
// Adding the same route twice is a no-op; leading slashes are normalized away so "/summa" and "summa" are the same route.
internal sealed class RouteSet
{
    private readonly List<string> _routes = [];
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

    public void Add(string relative)
    {
        var normalized = relative.TrimStart('/');
        if (_seen.Add(normalized))
        {
            _routes.Add(normalized);
        }
    }

    public IEnumerator<string> GetEnumerator() => _routes.GetEnumerator();

    // Returns the routes in deterministic ordinal order for stable sitemap diffs.
    public List<string> Sorted()
    {
        var ordered = new List<string>(_routes);
        ordered.Sort(StringComparer.Ordinal);
        return ordered;
    }
}
