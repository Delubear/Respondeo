using Respondeo.Content.Shared;
using Respondeo.Content.Markdown.Internal;

namespace Respondeo.Content.Markdown.Services;

/// <summary>
/// Loads author-curated content nodes from static Markdown files shipped by the Respondeo.Content.Markdown library.
/// The files live in that library's <c>wwwroot/content/</c> and are served by Blazor under the <c>_content/Respondeo.Content.Markdown/</c> static-web-asset path.
/// Runs entirely client-side: it fetches files via <see cref="HttpClient"/>, delegates parsing to <see cref="ContentParser"/>, and caches the parsed graph in memory for the app's lifetime.
/// The shared <see cref="MarkdownContentLoader{TFrontMatter,TModel}"/> base provides the fetch-cache-parse plumbing.
/// </summary>
internal sealed class ContentService : MarkdownContentLoader<ContentFrontMatter, ContentNode>, IContentService
{
    private const string ContentRoot = "_content/Respondeo.Content.Markdown/content";
    private const string ManifestPath = "_content/Respondeo.Content.Markdown/content/manifest.json";

    private readonly ContentParser _parser;
    private readonly AsyncInitCache<Dictionary<string, ContentNode>> _nodes = new();

    // Content is fetched at runtime and served as ordinary static files, so on hosts where we cannot
    // set server cache headers (e.g. GitHub Pages) requests must revalidate with the origin so users
    // never run against an outdated manifest or node.
    public ContentService(HttpClient http, ContentParser parser)
        : base(new ContentFetcher(http, ContentCachePolicy.Revalidate), new FrontMatterReader()) =>
        _parser = parser;

    /// <summary>Returns every loaded node, loading the content set if needed.</summary>
    public async Task<IReadOnlyCollection<ContentNode>> GetAllAsync()
    {
        var nodes = await EnsureLoadedAsync();
        return nodes.Values;
    }

    /// <summary>Returns a single node by id, or null if it does not exist.</summary>
    public async Task<ContentNode?> GetByIdAsync(string id)
    {
        var nodes = await EnsureLoadedAsync();
        return nodes.TryGetValue(id, out var node) ? node : null;
    }

    protected override ContentNode Map(ContentFrontMatter meta, string body, string fileName) =>
        _parser.Map(meta, body, StageFromFileName(fileName));

    private Task<Dictionary<string, ContentNode>> EnsureLoadedAsync() => _nodes.GetAsync(async () =>
    {
        var manifest = await Fetcher.GetFromJsonAsync<ContentManifest>(ManifestPath) ?? new ContentManifest();

        // Fetch every node concurrently rather than sequentially: on a cold load the content set is
        // dozens of small files, and awaiting them one at a time serialises the network round trips
        // into a noticeable first-load delay. Results are assembled in manifest order for determinism.
        var nodes = await LoadFilesAsync(manifest.Files.Select(f => ($"{ContentRoot}/{f}", f)));

        var loaded = new Dictionary<string, ContentNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes)
        {
            if (node is not null)
            {
                loaded[node.Id] = node;
            }
        }

        return loaded;
    });

    // A node's stage is the content sub-folder it lives in (e.g. "why-god/aquinas-five-ways.md"
    // belongs to the "why-god" stage). Files at the content root belong to no stage. Deriving it
    // from the folder keeps the folder layout as the single source of truth, so it survives header
    // renames and never drifts out of sync with a hand-authored value.
    private static string? StageFromFileName(string fileName)
    {
        var normalized = fileName.Replace('\\', '/');
        var slash = normalized.IndexOf('/');
        return slash > 0 ? normalized[..slash] : null;
    }

    private sealed class ContentManifest
    {
        public List<string> Files { get; set; } = new();
    }
}
