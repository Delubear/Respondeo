namespace Respondeo.Content.Discover.Internal;

/// <summary>
/// The shared shape of a per-type content manifest: a single ordered list of file names to load (e.g. prayers-manifest.json, articles-manifest.json).
/// Each content service reads its own manifest.
/// </summary>
internal sealed class ContentManifest
{
    public List<string> Files { get; set; } = [];
}
