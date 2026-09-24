using System.Runtime.CompilerServices;
using System.Text.Json;
using Respondeo;
using Respondeo.Content.Abstractions;
using Respondeo.Content.Markdown.Services;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Guards the link between the folder-based content stages and the navigation defined in
/// <see cref="SiteNavigation"/>. Two ways this can drift:
///   1. a navigation stage has no landing node (the masthead tab would dead-end), or
///   2. a content node lives in a folder that no navigation stage recognizes (its stage-scoped
///      URL and breadcrumb root would point at a section the site does not advertise).
/// Catching either at test time turns a would-be runtime navigation gap into a build failure.
/// </summary>
public class StageNavigationConsistencyTests
{
    private static string ContentDirectory([CallerFilePath] string thisFile = "")
    {
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

    // Mirrors ContentService.StageFromFileName: a node's stage is its content sub-folder.
    private static string? StageFromFile(string file)
    {
        var normalized = file.Replace('\\', '/');
        var slash = normalized.IndexOf('/');
        return slash > 0 ? normalized[..slash] : null;
    }

    private static IReadOnlyList<ContentNode> LoadNodes(string contentDir)
    {
        var parser = new ContentParser();
        return ReadManifestFiles(contentDir)
            .Select(file => parser.Parse(File.ReadAllText(Path.Combine(contentDir, file)), StageFromFile(file)))
            .Where(node => node is not null)
            .Select(node => node!)
            .ToList();
    }

    private static IReadOnlyList<string> StageSlugs() =>
        SiteNavigation.Items.Where(i => i.Href.Length > 0).Select(i => i.Href).ToList();

    [Fact]
    public void Every_navigation_stage_has_a_landing_node()
    {
        var contentDir = ContentDirectory();
        var ids = LoadNodes(contentDir).Select(n => n.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = StageSlugs().Where(slug => !ids.Contains(slug)).ToList();

        Assert.True(missing.Count == 0, $"Navigation stages without a landing node:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    [Fact]
    public void Every_node_stage_maps_to_a_navigation_stage()
    {
        var contentDir = ContentDirectory();
        var knownStages = StageSlugs().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orphaned = LoadNodes(contentDir)
            .Where(n => n.Stage is { Length: > 0 } && !knownStages.Contains(n.Stage))
            .Select(n => $"{n.Id}: stage '{n.Stage}'")
            .ToList();

        Assert.True(orphaned.Count == 0, $"Nodes whose stage is not in SiteNavigation:{Environment.NewLine}{string.Join(Environment.NewLine, orphaned)}");
    }

    private sealed class ContentManifest
    {
        public List<string> Files { get; set; } = new();
    }
}
