using Respondeo.Content.Contracts;
using Respondeo.Content.Infrastructure;
using Respondeo.Content.Rendering;
using Respondeo.Content.Shared;

namespace Respondeo.Content.Saints;

/// <summary>
/// Loads the bundled catalog of Catholic saints from static Markdown files shipped by the Respondeo.Content library,
/// served under the <c>_content/Respondeo.Content/</c> static-web-asset path.
/// Runs entirely client-side: fetches files via <see cref="HttpClient"/>, delegates parsing to <see cref="SaintParser"/>,
/// and caches the parsed records and derived index in memory for the app's lifetime. The whole (hand-authored) catalog is small, so it is loaded once up front.
/// The shared <see cref="MarkdownContentLoader{TFrontMatter,TModel}"/> base provides the fetch-cache-parse plumbing.
/// </summary>
internal sealed class SaintService(HttpClient http, IContentHtmlRenderer html) :
    MarkdownContentLoader<SaintFrontMatter, SaintRecordDocument>(new ContentFetcher(http, ContentCachePolicy.Immutable), new FrontMatterReader()), ISaintService
{
    private const string DiscoverRoot = "_content/Respondeo.Content/discover";
    private const string SaintsRoot = DiscoverRoot + "/saints";
    private const string ManifestPath = SaintsRoot + "/saints-manifest.json";
    private const string FacetsPath = SaintsRoot + "/facets.json";

    private readonly SaintParser _parser = new(html);
    private readonly AsyncInitCache<Catalog> _catalog = new();
    private readonly AsyncInitCache<SaintFacetCatalog> _facets = new();

    /// <summary>Returns the browse/search index, loading the catalog once and caching it.</summary>
    public async Task<SaintIndex> GetIndexAsync()
    {
        var catalog = await _catalog.GetAsync(LoadCatalogAsync);
        return catalog.Index;
    }

    /// <summary>Returns the slug&#8594;label facet catalog, loading and caching it once.</summary>
    public Task<SaintFacetCatalog> GetFacetsAsync() => _facets.GetAsync(async () =>
    {
        try
        {
            var dto = await Fetcher.GetFromJsonAsync<FacetsDto>(FacetsPath);
            return dto?.ToCatalog() ?? SaintFacetCatalog.Empty;
        }
        catch (HttpRequestException)
        {
            // Missing facets file must not break browsing; labels fall back to humanized slugs.
            return SaintFacetCatalog.Empty;
        }
    });

    /// <summary>Returns the full content of a single saint by id, or null if it does not exist.</summary>
    public async Task<SaintRecord?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var catalog = await _catalog.GetAsync(LoadCatalogAsync);
        return catalog.Records.TryGetValue(id, out var record) ? record.ToContract() : null;
    }

    protected override SaintRecordDocument Map(SaintFrontMatter meta, string body, string fileName) => _parser.Map(meta, body);

    private async Task<Catalog> LoadCatalogAsync()
    {
        var manifest = await Fetcher.GetFromJsonAsync<ContentManifest>(ManifestPath) ?? new ContentManifest();

        // Fetch every file concurrently rather than sequentially so the cold load overlaps the network round trips;
        // results are assembled in manifest order for deterministic index order.
        var parsed = await LoadFilesAsync(manifest.Files.Select(f => ($"{SaintsRoot}/{f}", f)));

        var records = new Dictionary<string, SaintRecordDocument>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<SaintIndexEntry>();
        foreach (var record in parsed)
        {
            if (record is null)
            {
                continue;
            }

            records[record.Id] = record;
            entries.Add(record.ToIndexEntryContract());
        }

        return new Catalog(records, new SaintIndex { Entries = entries });
    }

    private sealed record Catalog(Dictionary<string, SaintRecordDocument> Records, SaintIndex Index);

    // Serialization shape for facets.json: the era map carries label+description objects, the rest are slug->label maps.
    private sealed class FacetsDto
    {
        public Dictionary<string, EraDto> Eras { get; set; } = [];
        public Dictionary<string, string> Regions { get; set; } = [];
        public Dictionary<string, string> StatesOfLife { get; set; } = [];
        public Dictionary<string, string> Canonizations { get; set; } = [];

        public SaintFacetCatalog ToCatalog() => new()
        {
            Eras = new Dictionary<string, SaintEra>(
                Eras.Select(kvp => new KeyValuePair<string, SaintEra>(kvp.Key, new SaintEra(kvp.Value.Label, kvp.Value.Description))),
                StringComparer.OrdinalIgnoreCase),
            Regions = new Dictionary<string, string>(Regions, StringComparer.OrdinalIgnoreCase),
            StatesOfLife = new Dictionary<string, string>(StatesOfLife, StringComparer.OrdinalIgnoreCase),
            Canonizations = new Dictionary<string, string>(Canonizations, StringComparer.OrdinalIgnoreCase),
        };

        // A single era entry: its display label and a short description of the period it covers.
        public sealed class EraDto
        {
            public string Label { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}
