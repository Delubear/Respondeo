using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Guards the content manifest against drift: every file it lists must actually exist on disk,
/// and every Markdown file on disk (other than the manifest itself) should be listed.
/// This catches typos, renames, and forgotten manifest updates at test time rather than as a broken page at runtime.
/// </summary>
public class ManifestIntegrityTests
{
    private static string ContentDirectory([CallerFilePath] string thisFile = "")
    {
        // This file lives at <repo>/Respondeo.UnitTests/Content/ManifestIntegrityTests.cs.
        // Walk up to the repo root, then into the content library's wwwroot/content folder.
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
    public void Every_file_listed_in_the_manifest_exists_on_disk()
    {
        var contentDir = ContentDirectory();
        var files = ReadManifestFiles(contentDir);

        Assert.NotEmpty(files);

        var missing = files
            .Where(f => !File.Exists(Path.Combine(contentDir, f)))
            .ToList();

        Assert.True(missing.Count == 0, $"manifest.json lists files that do not exist: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Every_markdown_file_on_disk_is_listed_in_the_manifest()
    {
        var contentDir = ContentDirectory();
        var listed = ReadManifestFiles(contentDir)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var onDisk = Directory
            .EnumerateFiles(contentDir, "*.md", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)!
            // example*.md are documented copy-me templates, deliberately not shipped in the manifest.
            .Where(f => !f!.StartsWith("example", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var unlisted = onDisk
            .Where(f => !listed.Contains(f!))
            .ToList();

        Assert.True(unlisted.Count == 0, $"Markdown files exist that are not listed in manifest.json: {string.Join(", ", unlisted)}");
    }

    private sealed class ContentManifest
    {
        public List<string> Files { get; set; } = new();
    }
}
