using Respondeo.Content.Contracts;
using Respondeo.Content.Infrastructure;
using Respondeo.Content.Rendering;
using Respondeo.Content.Shared;

namespace Respondeo.Content.Inquiry;

/// <summary>
/// Loads author-curated content nodes from static Markdown files shipped by the Respondeo.Content library (Inquiry pillar).
/// The files live in that library's <c>wwwroot/inquiry/</c> and are served by Blazor under the <c>_content/Respondeo.Content/</c> static-web-asset path.
/// Runs entirely client-side: it fetches files via <see cref="HttpClient"/>, delegates parsing to <see cref="InquiryParser"/>,
/// and caches the parsed graph in memory for the app's lifetime.
/// The shared <see cref="MarkdownContentLoader{TFrontMatter,TModel}"/> base provides the fetch-cache-parse plumbing.
/// </summary>
internal sealed class InquiryService(HttpClient http, InquiryParser parser) : MarkdownContentLoader<InquiryFrontMatter, InquiryNodeDocument>(new ContentFetcher(http, ContentCachePolicy.Revalidate), new FrontMatterReader()), IContentService
{
    private const string ContentRoot = "_content/Respondeo.Content/inquiry";
    private const string ManifestPath = "_content/Respondeo.Content/inquiry/manifest.json";
    private readonly AsyncInitCache<Dictionary<string, InquiryNodeDocument>> _nodes = new();

    /// <summary>Returns every loaded node, loading the content set if needed.</summary>
    public async Task<IReadOnlyCollection<InquiryNode>> GetAllAsync()
    {
        var nodes = await EnsureLoadedAsync();
        return [.. nodes.Values.Select(n => n.ToContract())];
    }

    /// <summary>Returns a single node by id, or null if it does not exist.</summary>
    public async Task<InquiryNode?> GetByIdAsync(string id)
    {
        var nodes = await EnsureLoadedAsync();
        return nodes.TryGetValue(id, out var node) ? node.ToContract() : null;
    }

    protected override InquiryNodeDocument Map(InquiryFrontMatter meta, string body, string fileName) => parser.Map(meta, body, StageFromFileName(fileName));

    private Task<Dictionary<string, InquiryNodeDocument>> EnsureLoadedAsync() => _nodes.GetAsync(async () =>
    {
        var manifest = await Fetcher.GetFromJsonAsync<ContentManifest>(ManifestPath) ?? new ContentManifest();

        // Fetch every node concurrently rather than sequentially: on a cold load the content set is dozens of small files,
        // and awaiting them one at a time serialises the network round trips into a noticeable first-load delay.
        // Results are assembled in manifest order for determinism.
        var nodes = await LoadFilesAsync(manifest.Files.Select(f => ($"{ContentRoot}/{f}", f)));

        var loaded = new Dictionary<string, InquiryNodeDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in nodes)
        {
            if (node is not null)
            {
                loaded[node.Id] = node;
            }
        }

        return loaded;
    });

    // A node's stage is the content sub-folder it lives in (e.g. "why-god/aquinas-five-ways.md" belongs to the "why-god" stage).
    // Files at the content root belong to no stage.
    // Deriving it from the folder keeps the folder layout as the single source of truth, so it survives header renames and never drifts out of sync with a hand-authored value.
    private static string? StageFromFileName(string fileName)
    {
        var normalized = fileName.Replace('\\', '/');
        var slash = normalized.IndexOf('/');
        return slash > 0 ? normalized[..slash] : null;
    }
}
