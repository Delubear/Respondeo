using System.Net.Http.Json;
using Markdig;
using Respondeo.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Respondeo.Services;

/// <summary>
/// Loads author-curated content nodes from static Markdown files under <c>wwwroot/content/</c>.
/// Runs entirely client-side: it fetches files via <see cref="HttpClient"/>,
/// splits YAML front-matter from the Markdown body,
/// and caches the parsed graph in memory for the app's lifetime.
/// </summary>
public sealed class ContentService
{
    private const string ContentRoot = "content";
    private const string ManifestPath = "content/manifest.json";

    private readonly HttpClient _http;
    private readonly IDeserializer _yaml;
    private readonly MarkdownPipeline _markdown;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, ContentNode>? _nodes;

    public ContentService(HttpClient http)
    {
        _http = http;
        _yaml = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        _markdown = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Use<ContentContainerExtension>()
            .Build();
    }

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
        return nodes.Values.Where(n => n.Meta.IsEntryPoint).OrderBy(n => n.Title, StringComparer.OrdinalIgnoreCase).ToList();
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

            var manifest = await _http.GetFromJsonAsync<ContentManifest>(ManifestPath)
                           ?? new ContentManifest();

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
        var raw = await _http.GetStringAsync($"{ContentRoot}/{fileName}");
        var (frontMatter, body) = SplitFrontMatter(raw);
        if (frontMatter is null)
        {
            return null;
        }

        var meta = _yaml.Deserialize<ContentFrontMatter>(frontMatter);
        if (meta is null || string.IsNullOrWhiteSpace(meta.Id))
        {
            return null;
        }

        var html = Markdown.ToHtml(body, _markdown);
        return new ContentNode { Meta = meta, BodyHtml = html };
    }

    /// <summary>
    /// Splits a "---" delimited YAML front-matter block from the Markdown body.
    /// Returns (null, raw) when no front-matter block is present.
    /// </summary>
    private static (string? FrontMatter, string Body) SplitFrontMatter(string raw)
    {
        var text = raw.Replace("\r\n", "\n").TrimStart('\uFEFF', ' ', '\n');
        if (!text.StartsWith("---\n"))
        {
            return (null, raw);
        }

        var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0)
        {
            return (null, raw);
        }

        var frontMatter = text.Substring(4, end - 4);
        var bodyStart = text.IndexOf('\n', end + 1);
        var body = bodyStart < 0 ? string.Empty : text[(bodyStart + 1)..];
        return (frontMatter, body);
    }

    private sealed class ContentManifest
    {
        public List<string> Files { get; set; } = new();
    }
}
