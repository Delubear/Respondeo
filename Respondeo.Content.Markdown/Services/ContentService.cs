using System.Net.Http.Headers;
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

            var manifest = await GetFromJsonNoCacheAsync<ContentManifest>(ManifestPath) ?? new ContentManifest();

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
        try
        {
            var raw = await GetStringNoCacheAsync($"{ContentRoot}/{fileName}");
            return parser.Parse(raw);
        }
        catch (HttpRequestException)
        {
            // A single missing or unreachable file (e.g. a stale static-web-asset manifest returning 404) must not take down the entire content set.
            // Skip it and keep the rest of the site working; the absent node simply resolves to "not found".
            return null;
        }
    }

    // Content is fetched at runtime and served as ordinary static files, so the browser/CDN would
    // otherwise be free to hand back a stale copy after a deploy. On hosts where we cannot set
    // server cache headers (e.g. GitHub Pages), sending a no-cache request directive forces the
    // browser to revalidate with the origin so users never run against an outdated manifest or node.
    private async Task<string> GetStringNoCacheAsync(string url)
    {
        using var response = await SendNoCacheAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<T?> GetFromJsonNoCacheAsync<T>(string url)
    {
        using var response = await SendNoCacheAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private Task<HttpResponseMessage> SendNoCacheAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Headers = { CacheControl = new CacheControlHeaderValue { NoCache = true } }
        };
        return http.SendAsync(request);
    }

    private sealed class ContentManifest
    {
        public List<string> Files { get; set; } = new();
    }
}
