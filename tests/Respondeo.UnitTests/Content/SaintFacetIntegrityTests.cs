using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Guards the data-driven saint taxonomy. Two things are enforced:
/// <list type="bullet">
/// <item><description>Every facet slug used in a saint's front matter (era, region, state of
/// life, canonization) must have a display label defined in <c>facets.json</c>.</description></item>
/// <item><description>Every saint must declare at least one state of life and at least one canonization,
/// since those are multi-valued facets with no invented fallback.</description></item>
/// </list>
/// Because the vocabulary lives in content rather than in a C# enum, these tests catch typos, newly
/// introduced slugs with no label, and un-populated required facets at test time.
/// </summary>
public class SaintFacetIntegrityTests
{
    private static string SaintsDirectory([CallerFilePath] string thisFile = "")
    {
        // This file lives at <repo>/tests/Respondeo.UnitTests/Content/SaintFacetIntegrityTests.cs.
        var repoRoot = Directory.GetParent(thisFile)!.Parent!.Parent!.Parent!.FullName;
        return Path.Combine(repoRoot, "src", "Respondeo.Content", "wwwroot", "discover", "saints");
    }

    // The era map carries {label, description} objects; the rest are slug->label maps. We only need the keys here.
    private sealed record EraEntry(string Label, string Description);

    private sealed record FacetMaps(
        Dictionary<string, EraEntry> Eras,
        Dictionary<string, string> Regions,
        Dictionary<string, string> StatesOfLife,
        Dictionary<string, string> Canonizations);

    private static FacetMaps ReadFacets(string saintsDir)
    {
        var json = File.ReadAllText(Path.Combine(saintsDir, "facets.json"));
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<FacetMaps>(json, options);
        Assert.NotNull(dto);
        return dto!;
    }

    private sealed record SaintSlugs(string? Era, string? Region, List<string> StatesOfLife, List<string> Canonizations);

    // A tiny front-matter reader: pulls the "key: value" and "key: [a, b]" lines between the two "---"
    // fences. Good enough for the flat facet fields we need to validate.
    private static SaintSlugs ReadFacetSlugs(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var start = Array.IndexOf(lines, "---");
        string? era = null;
        string? region = null;
        var statesOfLife = new List<string>();
        var canonizations = new List<string>();

        for (var i = start + 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line == "---")
            {
                break;
            }

            if (line.StartsWith("era:", StringComparison.OrdinalIgnoreCase))
            {
                era = line["era:".Length..].Trim().Trim('"', '\'');
            }
            else if (line.StartsWith("region:", StringComparison.OrdinalIgnoreCase))
            {
                region = line["region:".Length..].Trim().Trim('"', '\'');
            }
            else if (line.StartsWith("statesOfLife:", StringComparison.OrdinalIgnoreCase))
            {
                statesOfLife.AddRange(ReadList(line, "statesOfLife:"));
            }
            else if (line.StartsWith("canonizations:", StringComparison.OrdinalIgnoreCase))
            {
                canonizations.AddRange(ReadList(line, "canonizations:"));
            }
        }

        return new SaintSlugs(era, region, statesOfLife, canonizations);
    }

    private static IEnumerable<string> ReadList(string line, string key)
    {
        var value = line[key.Length..].Trim().Trim('[', ']');
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.Trim('"', '\''));
    }

    [Fact]
    public void Every_facet_slug_used_in_content_has_a_label_in_facets_json()
    {
        var saintsDir = SaintsDirectory();
        var facets = ReadFacets(saintsDir);

        var missing = new List<string>();

        foreach (var file in Directory.EnumerateFiles(saintsDir, "*.md", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(file);
            var slugs = ReadFacetSlugs(file);

            if (!string.IsNullOrWhiteSpace(slugs.Era) && !facets.Eras.ContainsKey(slugs.Era))
            {
                missing.Add($"{name}: era '{slugs.Era}'");
            }

            if (!string.IsNullOrWhiteSpace(slugs.Region) && !facets.Regions.ContainsKey(slugs.Region))
            {
                missing.Add($"{name}: region '{slugs.Region}'");
            }

            foreach (var state in slugs.StatesOfLife.Where(s => !facets.StatesOfLife.ContainsKey(s)))
            {
                missing.Add($"{name}: stateOfLife '{state}'");
            }

            foreach (var canonization in slugs.Canonizations.Where(c => !facets.Canonizations.ContainsKey(c)))
            {
                missing.Add($"{name}: canonization '{canonization}'");
            }
        }

        Assert.True(
            missing.Count == 0,
            $"facets.json is missing labels for slugs used in content: {string.Join("; ", missing)}");
    }

    [Fact]
    public void Every_saint_declares_at_least_one_state_of_life_and_canonization()
    {
        var saintsDir = SaintsDirectory();

        var unpopulated = new List<string>();

        foreach (var file in Directory.EnumerateFiles(saintsDir, "*.md", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(file);
            var slugs = ReadFacetSlugs(file);

            if (slugs.StatesOfLife.Count == 0)
            {
                unpopulated.Add($"{name}: statesOfLife");
            }

            if (slugs.Canonizations.Count == 0)
            {
                unpopulated.Add($"{name}: canonizations");
            }
        }

        Assert.True(
            unpopulated.Count == 0,
            $"saints missing required multi-valued facets: {string.Join("; ", unpopulated)}");
    }
}
