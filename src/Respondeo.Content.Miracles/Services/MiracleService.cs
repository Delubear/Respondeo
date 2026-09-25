using System.Net.Http.Headers;
using System.Net.Http.Json;
using Respondeo.Content.Abstractions;

namespace Respondeo.Content.Miracles.Services;

/// <summary>
/// Loads the bundled catalog of Catholic miracles from static Markdown files shipped by the
/// Respondeo.Content.Miracles library, served under the
/// <c>_content/Respondeo.Content.Miracles/</c> static-web-asset path.
/// Runs entirely client-side: fetches files via <see cref="HttpClient"/>, delegates parsing to
/// <see cref="MiracleParser"/>, and caches the parsed records and derived index in memory for the
/// app's lifetime. The whole (hand-authored) catalog is small, so it is loaded once up front.
/// </summary>
internal sealed class MiracleService(HttpClient http, IContentHtmlRenderer html) : IMiracleService
{
    private const string MiraclesRoot = "_content/Respondeo.Content.Miracles/miracles";
    private const string ManifestPath = MiraclesRoot + "/miracles-manifest.json";
    private const string FacetsPath = MiraclesRoot + "/facets.json";

    private readonly MiracleParser _parser = new(html);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, MiracleRecord>? _records;
    private MiracleIndex? _index;
    private MiracleFacetCatalog? _facets;

    /// <summary>Returns the browse/search index, loading the catalog once and caching it.</summary>
    public async Task<MiracleIndex> GetIndexAsync()
    {
        await EnsureLoadedAsync();
        return _index!;
    }

    /// <summary>Returns the slug&#8594;label facet catalog, loading and caching it once.</summary>
    public async Task<MiracleFacetCatalog> GetFacetsAsync()
    {
        if (_facets is not null)
        {
            return _facets;
        }

        await _gate.WaitAsync();
        try
        {
            if (_facets is not null)
            {
                return _facets;
            }

            var dto = await GetFromJsonCachedAsync<FacetsDto>(FacetsPath);
            _facets = dto?.ToCatalog() ?? MiracleFacetCatalog.Empty;
            return _facets;
        }
        catch (HttpRequestException)
        {
            // Missing facets file must not break browsing; labels fall back to humanized slugs.
            _facets = MiracleFacetCatalog.Empty;
            return _facets;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Returns the full content of a single miracle by id, or null if it does not exist.</summary>
    public async Task<MiracleRecord?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var records = await EnsureLoadedAsync();
        return records.TryGetValue(id, out var record) ? record : null;
    }

    private async Task<Dictionary<string, MiracleRecord>> EnsureLoadedAsync()
    {
        if (_records is not null)
        {
            return _records;
        }

        await _gate.WaitAsync();
        try
        {
            if (_records is not null)
            {
                return _records;
            }

            var manifest = await GetFromJsonCachedAsync<MiracleManifest>(ManifestPath) ?? new MiracleManifest();

            // Fetch every file concurrently rather than sequentially so the cold load overlaps the
            // network round trips; results are assembled in manifest order for deterministic index order.
            var parsed = await Task.WhenAll(manifest.Files.Select(LoadRecordAsync));

            var records = new Dictionary<string, MiracleRecord>(StringComparer.OrdinalIgnoreCase);
            var entries = new List<MiracleIndexEntry>();
            foreach (var record in parsed)
            {
                if (record is null)
                {
                    continue;
                }

                records[record.Id] = record;
                entries.Add(MiracleParser.ToIndexEntry(record));
            }

            _records = records;
            _index = new MiracleIndex { Entries = entries };
            return _records;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<MiracleRecord?> LoadRecordAsync(string fileName)
    {
        try
        {
            var raw = await GetStringCachedAsync($"{MiraclesRoot}/{fileName}");
            return _parser.Parse(raw);
        }
        catch (HttpRequestException)
        {
            // A single missing or unreachable file must not take down the whole catalog; skip it.
            return null;
        }
    }

    // The bundled catalog is immutable for the lifetime of a deploy and the static assets are
    // fingerprinted per build, so requests opt into the browser cache for a year with no risk of
    // serving stale content across deploys.
    private async Task<string> GetStringCachedAsync(string url)
    {
        using var response = await SendCachedAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<T?> GetFromJsonCachedAsync<T>(string url)
    {
        using var response = await SendCachedAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private Task<HttpResponseMessage> SendCachedAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Headers = { CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromDays(365) } }
        };
        return http.SendAsync(request);
    }

    private sealed class MiracleManifest
    {
        public List<string> Files { get; set; } = [];
    }

    // Serialization shape for facets.json: three slug->label maps.
    private sealed class FacetsDto
    {
        public Dictionary<string, string> Categories { get; set; } = [];
        public Dictionary<string, string> Approvals { get; set; } = [];
        public Dictionary<string, string> Regions { get; set; } = [];

        public MiracleFacetCatalog ToCatalog() => new()
        {
            Categories = new Dictionary<string, string>(Categories, StringComparer.OrdinalIgnoreCase),
            Approvals = new Dictionary<string, string>(Approvals, StringComparer.OrdinalIgnoreCase),
            Regions = new Dictionary<string, string>(Regions, StringComparer.OrdinalIgnoreCase),
        };
    }
}
