using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Guards the data-driven prayer taxonomy: every <c>prayedAt</c> and <c>tags</c> slug used in a prayer's front matter
/// must have a display label in <c>prayers/facets.json</c>, and every label defined there must be used by at least one prayer.
/// This catches a typo or a newly introduced slug at test time rather than as a humanized fallback label in production.
/// </summary>
public class PrayerFacetIntegrityTests
{
    private static string PrayersDirectory([CallerFilePath] string thisFile = "")
    {
        // This file lives at <repo>/tests/Respondeo.UnitTests/Content/PrayerFacetIntegrityTests.cs.
        var repoRoot = Directory.GetParent(thisFile)!.Parent!.Parent!.Parent!.FullName;
        return Path.Combine(repoRoot, "src", "Respondeo.Content", "wwwroot", "discover", "prayers");
    }

    private sealed record FacetMaps(Dictionary<string, string> PrayedAt, Dictionary<string, string> Tags);

    private static FacetMaps ReadFacets(string prayersDir)
    {
        var json = File.ReadAllText(Path.Combine(prayersDir, "facets.json"));
        var dto = JsonSerializer.Deserialize<FacetMaps>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(dto);
        return dto!;
    }

    // A tiny front-matter reader for the "key: [a, b]" and YAML block-list ("key:" followed by "  - a") forms.
    private static Dictionary<string, List<string>> ReadLists(string filePath)
    {
        var lists = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) { ["prayedAt"] = [], ["tags"] = [] };
        var lines = File.ReadAllLines(filePath);
        var start = Array.IndexOf(lines, "---");
        string? key = null;

        for (var i = start + 1; i < lines.Length; i++)
        {
            var raw = lines[i];
            var line = raw.Trim();
            if (line == "---")
            {
                break;
            }

            if (line.StartsWith('-') && key is not null && char.IsWhiteSpace(raw[0]))
            {
                lists[key].Add(line[1..].Trim().Trim('"', '\''));
                continue;
            }

            key = null;
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var name = line[..colon].Trim();
            if (!lists.ContainsKey(name))
            {
                continue;
            }

            var value = line[(colon + 1)..].Trim();
            if (value.Length == 0)
            {
                key = name;
            }
            else
            {
                lists[name].AddRange(value.Trim('[', ']')
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.Trim('"', '\'')));
            }
        }

        return lists;
    }

    private static IEnumerable<(string File, Dictionary<string, List<string>> Lists)> PrayerFiles(string prayersDir) =>
        Directory.EnumerateFiles(prayersDir, "*.md", SearchOption.TopDirectoryOnly)
            .Where(f => !Path.GetFileName(f).StartsWith("example", StringComparison.OrdinalIgnoreCase))
            .Select(f => (Path.GetFileName(f), ReadLists(f)));

    [Fact]
    public void Every_facet_slug_used_in_content_has_a_label_in_facets_json()
    {
        var prayersDir = PrayersDirectory();
        var facets = ReadFacets(prayersDir);
        var missing = new List<string>();

        foreach (var (name, lists) in PrayerFiles(prayersDir))
        {
            missing.AddRange(lists["prayedAt"].Where(s => !facets.PrayedAt.ContainsKey(s)).Select(s => $"{name}: prayedAt '{s}'"));
            missing.AddRange(lists["tags"].Where(s => !facets.Tags.ContainsKey(s)).Select(s => $"{name}: tag '{s}'"));
        }

        Assert.True(missing.Count == 0, $"prayers/facets.json is missing labels for slugs used in content: {string.Join("; ", missing)}");
    }

    [Fact]
    public void Every_label_in_facets_json_is_used_by_a_prayer()
    {
        var prayersDir = PrayersDirectory();
        var facets = ReadFacets(prayersDir);
        var files = PrayerFiles(prayersDir).ToList();
        var usedPrayedAt = files.SelectMany(f => f.Lists["prayedAt"]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var usedTags = files.SelectMany(f => f.Lists["tags"]).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unused = facets.PrayedAt.Keys.Where(k => !usedPrayedAt.Contains(k)).Select(k => $"prayedAt '{k}'")
            .Concat(facets.Tags.Keys.Where(k => !usedTags.Contains(k)).Select(k => $"tag '{k}'"))
            .ToList();

        Assert.True(unused.Count == 0, $"prayers/facets.json defines labels no prayer uses: {string.Join("; ", unused)}");
    }
}
