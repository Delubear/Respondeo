using System.Net.Http.Json;
using Respondeo.Content.Abstractions;

namespace Respondeo.Content.Markdown.Services;

/// <summary>
/// Loads author-curated content nodes from static Markdown files shipped by the Respondeo.Content.Markdown library.
/// The files live in that library's <c>wwwroot/content/</c> and are served by Blazor under the <c>_content/Respondeo.Content.Markdown/</c> static-web-asset path.
/// Runs entirely client-side: it fetches files via <see cref="HttpClient"/>, delegates parsing to <see cref="ContentParser"/>, and caches the parsed graph in memory for the app's lifetime.
/// </summary>
internal sealed class ContentService(HttpClient http, ContentParser parser) : IContentService
{
    private const string ContentRoot = "_content/Respondeo.Content.Markdown/content";
    private const string ManifestPath = "_content/Respondeo.Content.Markdown/content/manifest.json";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, ContentNode>? _nodes;

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

    /// <summary>Returns the top-level entry-point nodes for the home page.</summary>
    public async Task<IReadOnlyList<ContentNode>> GetEntryPointsAsync()
    {
        var nodes = await EnsureLoadedAsync();
        return nodes.Values.Where(n => n.IsEntryPoint).OrderBy(n => n.Title, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<Dictionary<string, ContentNode>> EnsureLoadedAsync()
    {
        if (_nodes is not null)
        {
            return _nodes;
        }

        await _gate.WaitAsync();
        try
        {
            if (_nodes is not null)
            {
                return _nodes;
            }

            var manifest = await http.GetFromJsonAsync<ContentManifest>(ManifestPath) ?? new ContentManifest();

            var loaded = new Dictionary<string, ContentNode>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in manifest.Files)
            {
                var node = await LoadNodeAsync(file);
                if (node is not null)
                {
                    loaded[node.Id] = node;
                }
            }

            _nodes = loaded;
            return _nodes;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ContentNode?> LoadNodeAsync(string fileName)
    {
        var raw = await http.GetStringAsync($"{ContentRoot}/{fileName}");
        return parser.Parse(raw);
    }

    private sealed class ContentManifest
    {
        public List<string> Files { get; set; } = new();
    }
}
