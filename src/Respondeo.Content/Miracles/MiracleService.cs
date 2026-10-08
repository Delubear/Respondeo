using Respondeo.Content.Contracts;
using Respondeo.Content.Infrastructure;
using Respondeo.Content.Rendering;
using Respondeo.Content.Shared;

namespace Respondeo.Content.Miracles;

/// <summary>
/// Loads the bundled catalog of Catholic miracles from static Markdown files shipped by the Respondeo.Content library,
/// served under the <c>_content/Respondeo.Content/</c> static-web-asset path.
/// Runs entirely client-side: fetches files via <see cref="HttpClient"/>, delegates parsing to <see cref="MiracleParser"/>,
/// and caches the parsed records and derived index in memory for the app's lifetime. The whole (hand-authored) catalog is small, so it is loaded once up front.
/// The shared <see cref="MarkdownContentLoader{TFrontMatter,TModel}"/> base provides the fetch-cache-parse plumbing.
/// </summary>
internal sealed class MiracleService(HttpClient http, IContentHtmlRenderer html) :
    MarkdownContentLoader<MiracleFrontMatter, MiracleRecordDocument>(new ContentFetcher(http, ContentCachePolicy.Immutable), new FrontMatterReader()), IMiracleService
{
    private const string MiraclesRoot = DiscoverRoot + "/miracles";
    private const string ManifestPath = MiraclesRoot + "/miracles-manifest.json";
    private const string FacetsPath = MiraclesRoot + "/facets.json";

    private readonly MiracleParser _parser = new(html);
    private readonly AsyncInitCache<Catalog> _catalog = new();
    private readonly AsyncInitCache<MiracleFacetCatalog> _facets = new();

    /// <summary>Returns the browse/search index, loading the catalog once and caching it.</summary>
    public async Task<MiracleIndex> GetIndexAsync()
    {
        var catalog = await _catalog.GetAsync(LoadCatalogAsync);
        return catalog.Index;
    }

    /// <summary>Returns the slug&#8594;label facet catalog, loading and caching it once.</summary>
    public Task<MiracleFacetCatalog> GetFacetsAsync() =>
        _facets.GetAsync(() => LoadFacetsAsync<FacetsDto, MiracleFacetCatalog>(FacetsPath, dto => dto.ToCatalog(), MiracleFacetCatalog.Empty));

    /// <summary>Returns the full content of a single miracle by id, or null if it does not exist.</summary>
    public async Task<MiracleRecord?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var catalog = await _catalog.GetAsync(LoadCatalogAsync);
        return catalog.Records.TryGetValue(id, out var record) ? record.ToContract() : null;
    }

    protected override MiracleRecordDocument Map(MiracleFrontMatter meta, string body, string fileName) => _parser.Map(meta, body);

    private async Task<Catalog> LoadCatalogAsync()
    {
        var parsed = await LoadManifestAsync(MiraclesRoot, ManifestPath);
        var (records, entries) = BuildCatalog(parsed, r => r.Id, r => r.ToIndexEntryContract());
        return new Catalog(records, new MiracleIndex { Entries = entries });
    }

    private sealed record Catalog(Dictionary<string, MiracleRecordDocument> Records, MiracleIndex Index);

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
