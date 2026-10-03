using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Respondeo.UnitTests.Content;

/// <summary>
/// Guards the four Discover manifests (prayers, articles, miracles, devotions) against drift:
/// every file a manifest lists must exist on disk, and every content file on disk must be listed.
/// This is the sibling of <see cref="ManifestIntegrityTests"/> (which covers the Inquiry manifest)
/// and catches the single most common authoring mistake — adding a content file but forgetting to
/// register it in its manifest — at test time rather than as a missing item at runtime.
/// </summary>
public class DiscoverManifestIntegrityTests
{
    // Each Discover content type: its folder, the content file extension, and non-content files to
    // ignore when scanning the folder (manifests and data files are not content items).
    public static TheoryData<string, string, string[]> ContentTypes() => new()
    {
        { "prayers", "*.md", new[] { "prayers-manifest.json" } },
        { "articles", "*.md", new[] { "articles-manifest.json" } },
        { "miracles", "*.md", new[] { "miracles-manifest.json", "facets.json" } },
        { "devotions", "*.json", new[] { "devotions-manifest.json" } },
    };

    private static string DiscoverDirectory([CallerFilePath] string thisFile = "")
    {
        // This file lives at <repo>/tests/Respondeo.UnitTests/Content/DiscoverManifestIntegrityTests.cs.
        var repoRoot = Directory.GetParent(thisFile)!.Parent!.Parent!.Parent!.FullName;
        return Path.Combine(repoRoot, "src", "Respondeo.Content", "wwwroot", "discover");
    }

    private static IReadOnlyList<string> ReadManifestFiles(string typeDir, string folder)
    {
        var manifestPath = Path.Combine(typeDir, $"{folder}-manifest.json");
        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<ContentManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return manifest?.Files ?? [];
    }

    [Theory]
    [MemberData(nameof(ContentTypes))]
    public void Every_file_listed_in_the_manifest_exists_on_disk(string folder, string pattern, string[] ignore)
    {
        _ = pattern;
        _ = ignore;
        var typeDir = Path.Combine(DiscoverDirectory(), folder);
        var files = ReadManifestFiles(typeDir, folder);

        Assert.NotEmpty(files);

        var missing = files
            .Where(f => !File.Exists(Path.Combine(typeDir, f)))
            .ToList();

        Assert.True(missing.Count == 0, $"{folder}-manifest.json lists files that do not exist: {string.Join(", ", missing)}");
    }

    [Theory]
    [MemberData(nameof(ContentTypes))]
    public void Every_content_file_on_disk_is_listed_in_the_manifest(string folder, string pattern, string[] ignore)
    {
        var typeDir = Path.Combine(DiscoverDirectory(), folder);
        var listed = ReadManifestFiles(typeDir, folder)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var ignored = ignore.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var onDisk = Directory
            .EnumerateFiles(typeDir, pattern, SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(f => f is not null && !ignored.Contains(f!))
            // example*.md / example*.json are documented copy-me templates, deliberately unshipped.
            .Where(f => !f!.StartsWith("example", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var unlisted = onDisk
            .Where(f => !listed.Contains(f!))
            .ToList();

        Assert.True(unlisted.Count == 0, $"{folder} files exist that are not listed in {folder}-manifest.json: {string.Join(", ", unlisted)}");
    }

    private sealed class ContentManifest
    {
        public List<string> Files { get; set; } = new();
    }
}
