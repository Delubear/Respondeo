using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Guards the data-driven miracle taxonomy: every facet slug used in a miracle's front matter
/// (category, approval, region) must have a display label defined in <c>facets.json</c>. Because the
/// vocabulary now lives in content rather than in a C# enum, this test is what catches a typo or a
/// newly introduced slug that nobody added a label for — at test time rather than as a humanized
/// fallback label slipping into production.
/// </summary>
public class MiracleFacetIntegrityTests
{
    private static string MiraclesDirectory([CallerFilePath] string thisFile = "")
    {
        // This file lives at <repo>/tests/Respondeo.UnitTests/Content/MiracleFacetIntegrityTests.cs.
        var repoRoot = Directory.GetParent(thisFile)!.Parent!.Parent!.Parent!.FullName;
        return Path.Combine(repoRoot, "src", "Respondeo.Content.Miracles", "wwwroot", "miracles");
    }

    private sealed record FacetMaps(
        Dictionary<string, string> Categories,
        Dictionary<string, string> Approvals,
        Dictionary<string, string> Regions);

    private static FacetMaps ReadFacets(string miraclesDir)
    {
        var json = File.ReadAllText(Path.Combine(miraclesDir, "facets.json"));
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<FacetMaps>(json, options);
        Assert.NotNull(dto);
        return dto!;
    }

    // A tiny front-matter reader: pulls the "key: value" and "key: [a, b]" lines between the two "---"
    // fences. Good enough for the flat facet fields we need to validate.
    private static (List<string> Types, string? Approval, string? Region) ReadFacetSlugs(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var start = Array.IndexOf(lines, "---");
        var types = new List<string>();
        string? approval = null;
        string? region = null;

        for (var i = start + 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line == "---")
            {
                break;
            }

            if (line.StartsWith("types:", StringComparison.OrdinalIgnoreCase))
            {
                var value = line["types:".Length..].Trim().Trim('[', ']');
                types.AddRange(value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.Trim('"', '\'')));
            }
            else if (line.StartsWith("approval:", StringComparison.OrdinalIgnoreCase))
            {
                approval = line["approval:".Length..].Trim().Trim('"', '\'');
            }
            else if (line.StartsWith("region:", StringComparison.OrdinalIgnoreCase))
            {
                region = line["region:".Length..].Trim().Trim('"', '\'');
            }
        }

        return (types, approval, region);
    }

    [Fact]
    public void Every_facet_slug_used_in_content_has_a_label_in_facets_json()
    {
        var miraclesDir = MiraclesDirectory();
        var facets = ReadFacets(miraclesDir);

        var missing = new List<string>();

        foreach (var file in Directory.EnumerateFiles(miraclesDir, "*.md", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(file);
            var (types, approval, region) = ReadFacetSlugs(file);

            foreach (var type in types.Where(t => !facets.Categories.ContainsKey(t)))
            {
                missing.Add($"{name}: category '{type}'");
            }

            if (!string.IsNullOrWhiteSpace(approval) && !facets.Approvals.ContainsKey(approval))
            {
                missing.Add($"{name}: approval '{approval}'");
            }

            if (!string.IsNullOrWhiteSpace(region) && !facets.Regions.ContainsKey(region))
            {
                missing.Add($"{name}: region '{region}'");
            }
        }

        Assert.True(
            missing.Count == 0,
            $"facets.json is missing labels for slugs used in content: {string.Join("; ", missing)}");
    }
}
