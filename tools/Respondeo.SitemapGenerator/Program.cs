using System.Text;
using System.Text.Json;
using Respondeo.Content.Summa;

// Build-time sitemap generator for Respondeo.
//
// Invoked by Respondeo.csproj before publish (and build).
// It enumerates every canonical, crawlable route from the content manifests that ship in the site and writes wwwroot/sitemap.xml.
// Slugs are resolved through SummaParts so the emitted URLs always match the app's real routes.
//
// Usage: Respondeo.SitemapGenerator <repoRoot> <outputSitemapPath>
//
// Structure: Main parses the arguments, then asks each Collect* method to contribute the routes for one content source into a shared RouteSet.
// The set is sorted and rendered by WriteSitemap. Each source is self-contained so a new content pillar is added by writing one more Collect* method.

return SitemapGenerator.Run(args);

internal static class SitemapGenerator
{
    private const string BaseUrl = "https://respondeo.faith";

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
        CollectMiracleRoutes(routes, paths.MiraclesManifest);
        CollectJourneyRoutes(routes, paths.ContentRoot, paths.ContentManifest);
        CollectCredoRoutes(routes, paths.CredoRoot, paths.CredoManifest);

        return WriteSitemap(routes, outputPath);
    }

    // 1. Top-level static routes that always exist regardless of content.
    private static void CollectStaticRoutes(RouteSet routes)
    {
        routes.Add("");            // Home
        routes.Add("summa");
        routes.Add("miracles");
        routes.Add("articles");
        routes.Add("credo");
        routes.Add("credo/prayers");
        routes.Add("credo/devotions");
        routes.Add("credo/articles");
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
            routes.Add($"summa/part/{SummaParts.SlugForKey(partKey)}");

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
    private static void CollectMiracleRoutes(RouteSet routes, string miraclesManifestPath)
    {
        if (!File.Exists(miraclesManifestPath))
        {
            Console.Error.WriteLine($"warning: Miracles manifest not found at {miraclesManifestPath}; skipping miracle routes.");
            return;
        }

        foreach (var file in ReadManifestFiles(miraclesManifestPath))
        {
            routes.Add($"miracles/{Path.GetFileNameWithoutExtension(file)}");
        }
    }

    // 4. Journey content nodes + their stage landing pages.
    // A node's stage is its content sub-folder; its id comes from the front matter. Root-level nodes fall back to the flat node/{id} route.
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
            if (!string.IsNullOrEmpty(stage))
            {
                stages.Add(stage);
            }

            routes.Add(string.IsNullOrEmpty(stage) ? $"node/{id}" : $"{stage}/node/{id}");
        }

        foreach (var stage in stages)
        {
            routes.Add(stage);
        }
    }

    // 5. Credo detail pages: prayers, devotions, and articles.
    // The Credo manifest groups files into separate arrays rather than a single "files" list.
    // Prayer ids come from the markdown front matter, but only primary-language (non-Latin) prayers get their own browse/detail route;
    // Latin translations are shown inline on the English prayer's page, so they are excluded here.
    // Devotion ids come from the JSON "id" field, article ids from the markdown front matter.
    private static void CollectCredoRoutes(RouteSet routes, string credoRoot, string credoManifestPath)
    {
        if (!File.Exists(credoManifestPath))
        {
            Console.Error.WriteLine($"warning: Credo manifest not found at {credoManifestPath}; skipping Credo routes.");
            return;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(credoManifestPath));
        var root = doc.RootElement;

        foreach (var file in ReadCredoArray(root, "prayers"))
        {
            var fullPath = Path.Combine(credoRoot, "prayers", file);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var language = ReadFrontMatterValue(fullPath, "language") ?? "en";
            if (string.Equals(language, "la", StringComparison.OrdinalIgnoreCase))
            {
                continue; // Latin translations render inline on the English page, not their own route.
            }

            var id = ReadFrontMatterId(fullPath);
            if (!string.IsNullOrWhiteSpace(id))
            {
                routes.Add($"credo/prayers/{id}");
            }
        }

        foreach (var file in ReadCredoArray(root, "devotions"))
        {
            var fullPath = Path.Combine(credoRoot, "devotions", file);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var id = ReadJsonId(fullPath);
            if (!string.IsNullOrWhiteSpace(id))
            {
                routes.Add($"credo/devotions/{id}");
            }
        }

        foreach (var file in ReadCredoArray(root, "articles"))
        {
            var fullPath = Path.Combine(credoRoot, "articles", file);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var id = ReadFrontMatterId(fullPath);
            if (!string.IsNullOrWhiteSpace(id))
            {
                routes.Add($"credo/articles/{id}");
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
        return $"{SummaParts.SlugForKey(key)}-{rest}";
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

    // Reads a named string array (e.g. "prayers") from the Credo manifest.
    private static IEnumerable<string> ReadCredoArray(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in array.EnumerateArray())
        {
            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }
    }

    private static string Escape(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");
}

// The absolute paths to every content manifest and root the generator reads, derived once from the repo root so the individual Collect* methods do not each re-assemble them.
internal readonly record struct ContentPaths(
    string SummaIndex,
    string MiraclesManifest,
    string ContentRoot,
    string ContentManifest,
    string CredoRoot,
    string CredoManifest)
{
    public static ContentPaths ForRepo(string repoRoot)
    {
        var contentRoot = Path.Combine(repoRoot, "src", "Respondeo.Content.Markdown", "wwwroot", "content");
        var miraclesRoot = Path.Combine(repoRoot, "src", "Respondeo.Content.Miracles", "wwwroot", "miracles");
        var credoRoot = Path.Combine(repoRoot, "src", "Respondeo.Content.Credo", "wwwroot", "credo");

        return new ContentPaths(
            SummaIndex: Path.Combine(repoRoot, "src", "Respondeo.Content.Summa", "wwwroot", "summa", "summa-index.json"),
            MiraclesManifest: Path.Combine(miraclesRoot, "miracles-manifest.json"),
            ContentRoot: contentRoot,
            ContentManifest: Path.Combine(contentRoot, "manifest.json"),
            CredoRoot: credoRoot,
            CredoManifest: Path.Combine(credoRoot, "credo-manifest.json"));
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
