using System.Text;
using System.Text.Json;
using Respondeo.Content.Summa;

// Build-time sitemap generator for Respondeo.
//
// Invoked by Respondeo.csproj before publish (and build). It enumerates every canonical, crawlable
// route from the content manifests that ship in the site and writes wwwroot/sitemap.xml. Slugs are
// resolved through SummaParts so the emitted URLs always match the app's real routes.
//
// Usage: Respondeo.SitemapGenerator <repoRoot> <outputSitemapPath>

const string BaseUrl = "https://respondeo.faith";

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: Respondeo.SitemapGenerator <repoRoot> <outputSitemapPath>");
    return 1;
}

var repoRoot = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);

var summaIndexPath = Path.Combine(repoRoot, "src", "Respondeo.Content.Summa", "wwwroot", "summa", "summa-index.json");
var miraclesRoot = Path.Combine(repoRoot, "src", "Respondeo.Content.Miracles", "wwwroot", "miracles");
var miraclesManifestPath = Path.Combine(miraclesRoot, "miracles-manifest.json");
var contentRoot = Path.Combine(repoRoot, "src", "Respondeo.Content.Markdown", "wwwroot", "content");
var contentManifestPath = Path.Combine(contentRoot, "manifest.json");

var credoRoot = Path.Combine(repoRoot, "src", "Respondeo.Content.Credo", "wwwroot", "credo");
var credoManifestPath = Path.Combine(credoRoot, "credo-manifest.json");

// LinkedHashSet-style ordering: preserve discovery order but drop duplicates.
var routes = new List<string>();
var seen = new HashSet<string>(StringComparer.Ordinal);

void Add(string relative)
{
    var normalized = relative.TrimStart('/');
    if (seen.Add(normalized))
    {
        routes.Add(normalized);
    }
}

// 1. Top-level static routes.
Add("");            // Home
Add("summa");
Add("miracles");
Add("articles");
Add("credo");
Add("credo/prayers");
Add("credo/devotions");
Add("credo/articles");

// 2. Summa part landing pages + every question, mapping storage keys (p1) to URL slugs (prima).
if (File.Exists(summaIndexPath))
{
    using var doc = JsonDocument.Parse(File.ReadAllText(summaIndexPath));
    if (doc.RootElement.TryGetProperty("parts", out var parts))
    {
        foreach (var part in parts.EnumerateArray())
        {
            var partKey = part.GetProperty("id").GetString() ?? string.Empty;
            var partSlug = SummaParts.SlugForKey(partKey);
            Add($"summa/part/{partSlug}");

            if (!part.TryGetProperty("questions", out var questions))
            {
                continue;
            }

            foreach (var question in questions.EnumerateArray())
            {
                var questionId = question.GetProperty("id").GetString();
                if (string.IsNullOrWhiteSpace(questionId))
                {
                    continue;
                }

                Add($"summa/{ToQuestionSlug(questionId)}");
            }
        }
    }
}
else
{
    Console.Error.WriteLine($"warning: Summa index not found at {summaIndexPath}; skipping Summa routes.");
}

// 3. Miracle detail pages. The id is the file name without its .md extension.
if (File.Exists(miraclesManifestPath))
{
    foreach (var file in ReadManifestFiles(miraclesManifestPath))
    {
        var id = Path.GetFileNameWithoutExtension(file);
        Add($"miracles/{id}");
    }
}
else
{
    Console.Error.WriteLine($"warning: Miracles manifest not found at {miraclesManifestPath}; skipping miracle routes.");
}

// 4. Journey content nodes + their stage landing pages. A node's stage is its content sub-folder;
//    its id comes from the front matter. Root-level nodes fall back to the flat node/{id} route.
if (File.Exists(contentManifestPath))
{
    var stages = new List<string>();
    var stagesSeen = new HashSet<string>(StringComparer.Ordinal);

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

        if (stage is not null && stagesSeen.Add(stage))
        {
            stages.Add(stage);
        }

        Add(string.IsNullOrEmpty(stage) ? $"node/{id}" : $"{stage}/node/{id}");
    }

    // Stage landing pages (Stage.razor: "/{Slug}") come after the section is known, once per stage.
    foreach (var stage in stages)
    {
        Add(stage);
    }
}
else
{
    Console.Error.WriteLine($"warning: Content manifest not found at {contentManifestPath}; skipping journey routes.");
}

// 5. Credo detail pages: prayers, devotions, and articles. The Credo manifest groups files into
//    separate arrays rather than a single "files" list. Prayer ids come from the markdown front
//    matter, but only primary-language (non-Latin) prayers get their own browse/detail route; Latin
//    translations are shown inline on the English prayer's page, so they are excluded here. Devotion
//    ids come from the JSON "id" field, article ids from the markdown front matter.
if (File.Exists(credoManifestPath))
{
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
            Add($"credo/prayers/{id}");
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
            Add($"credo/devotions/{id}");
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
            Add($"credo/articles/{id}");
        }
    }
}
else
{
    Console.Error.WriteLine($"warning: Credo manifest not found at {credoManifestPath}; skipping Credo routes.");
}

// Write the sitemap. Deterministic ordering (sorted) keeps diffs small across regenerations.
routes.Sort(StringComparer.Ordinal);

var xml = new StringBuilder();
xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
xml.AppendLine("<!-- Generated by Respondeo.SitemapGenerator at build time. Do not edit by hand. -->");
xml.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
foreach (var route in routes)
{
    var loc = route.Length == 0 ? $"{BaseUrl}/" : $"{BaseUrl}/{route}";
    xml.AppendLine("  <url>");
    xml.AppendLine($"    <loc>{Escape(loc)}</loc>");
    xml.AppendLine("  </url>");
}
xml.AppendLine("</urlset>");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

var content = xml.ToString();
// Skip the write when nothing changed so the build's up-to-date checks and Git stay quiet.
if (File.Exists(outputPath) && File.ReadAllText(outputPath) == content)
{
    Console.WriteLine($"Sitemap unchanged ({routes.Count} urls): {outputPath}");
    return 0;
}

File.WriteAllText(outputPath, content);
Console.WriteLine($"Sitemap written ({routes.Count} urls): {outputPath}");
return 0;

// Converts a Summa storage id (e.g. "p1-q001") to its URL slug (e.g. "prima-q001").
static string ToQuestionSlug(string questionId)
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
static IEnumerable<string> ReadManifestFiles(string manifestPath)
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
static string? ReadFrontMatterId(string path) => ReadFrontMatterValue(path, "id");

// Reads a named scalar value (e.g. "id" or "language") from a markdown file's YAML front matter
// without a full YAML parse.
static string? ReadFrontMatterValue(string path, string key)
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
static string? ReadJsonId(string path)
{
    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
}

// Reads a named string array (e.g. "prayers") from the Credo manifest.
static IEnumerable<string> ReadCredoArray(JsonElement root, string property)
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

static string Escape(string value) => value
    .Replace("&", "&amp;")
    .Replace("<", "&lt;")
    .Replace(">", "&gt;");
