using System.Runtime.CompilerServices;
using System.Text.Json;
using Respondeo;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Guards the folder-derived stage system: every content sub-folder must correspond to a known
/// masthead stage slug in <see cref="SiteNavigation"/>. This catches a mistyped or orphaned
/// folder at test time — before it produces a node URL that no navigation tab can highlight.
/// </summary>
public class StageFolderIntegrityTests
{
    private static string ContentDirectory([CallerFilePath] string thisFile = "")
    {
        // This file lives at <repo>/Respondeo.UnitTests/Content/StageFolderIntegrityTests.cs.
        var repoRoot = Directory.GetParent(thisFile)!.Parent!.Parent!.FullName;
        return Path.Combine(repoRoot, "Respondeo.Content.Markdown", "wwwroot", "content");
    }

    private static IReadOnlyList<string> ReadManifestFiles(string contentDir)
    {
        var manifestPath = Path.Combine(contentDir, "manifest.json");
        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<ContentManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return manifest?.Files ?? [];
    }

    [Fact]
    public void Every_content_subfolder_maps_to_a_known_navigation_stage()
    {
        var contentDir = ContentDirectory();

        var knownStages = SiteNavigation.Items
            .Select(i => i.Href.Trim('/'))
            .Where(h => h.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // The stage of each manifest entry is its leading folder segment (files at the root have none).
        var stages = ReadManifestFiles(contentDir)
            .Select(f => f.Replace('\\', '/'))
            .Where(f => f.Contains('/'))
            .Select(f => f[..f.IndexOf('/')])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unknown = stages
            .Where(s => !knownStages.Contains(s))
            .ToList();

        Assert.True(unknown.Count == 0, $"Content folders do not map to a SiteNavigation stage: {string.Join(", ", unknown)}");
    }

    private sealed class ContentManifest
    {
        public List<string> Files { get; set; } = new();
    }
}
