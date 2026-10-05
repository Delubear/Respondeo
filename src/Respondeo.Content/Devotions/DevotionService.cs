using Respondeo.Content.Contracts;
using Respondeo.Content.Infrastructure;
using Respondeo.Content.Rendering;
using Respondeo.Content.Shared;

namespace Respondeo.Content.Devotions;

/// <summary>
/// Loads the bundled, data-driven devotions from static JSON files shipped by the Respondeo.Content library under the
/// <c>_content/Respondeo.Content/discover/devotions</c> static-web-asset path.
/// Runs entirely client-side: fetches files via <see cref="HttpClient"/>, delegates parsing to <see cref="DevotionParser"/>,
/// and caches the parsed devotions and derived index in memory for the app's lifetime.
/// </summary>
internal sealed class DevotionService(HttpClient http, IContentHtmlRenderer html, IPrayerService prayers) : IDevotionService
{
    private const string DevotionsRoot = "_content/Respondeo.Content/discover/devotions";
    private const string ManifestPath = DevotionsRoot + "/devotions-manifest.json";

    private readonly DevotionParser _parser = new(html);
    private readonly ContentFetcher _fetcher = new(http, ContentCachePolicy.Immutable);
    private readonly AsyncInitCache<Catalog> _catalog = new();

    /// <summary>Returns the browse index, loading the catalog once and caching it.</summary>
    public async Task<IReadOnlyList<DevotionSummary>> GetIndexAsync() => (await Load()).Summaries;

    /// <summary>Returns the full definition of a single devotion by id, or null if it does not exist.</summary>
    public async Task<Devotion?> GetDevotionAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var catalog = await Load();
        return catalog.Devotions.TryGetValue(id, out var devotion) ? devotion.ToContract() : null;
    }

    /// <summary>
    /// Fetches every distinct prayer referenced in the devotion's sequence (including per-mystery
    /// steps), keyed by id; ids that resolve to no prayer are omitted.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, Prayer>> GetPrayersForAsync(Devotion devotion)
    {
        ArgumentNullException.ThrowIfNull(devotion);

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Collect(IEnumerable<DevotionStep> steps)
        {
            foreach (var step in steps)
            {
                if (!string.IsNullOrWhiteSpace(step.PrayerId))
                {
                    ids.Add(step.PrayerId);
                }

                Collect(step.PerMystery);
            }
        }

        Collect(devotion.Sequence);

        var map = new Dictionary<string, Prayer>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids)
        {
            var prayer = await prayers.GetPrayerAsync(id);
            if (prayer is not null)
            {
                map[id] = prayer;
            }
        }

        return map;
    }

    private Task<Catalog> Load() => _catalog.GetAsync(async () =>
    {
        var manifest = await _fetcher.GetFromJsonAsync<ContentManifest>(ManifestPath) ?? new ContentManifest();

        var devotions = await Task.WhenAll(manifest.Files.Select(LoadDevotionAsync));

        var map = new Dictionary<string, DevotionDocument>(StringComparer.OrdinalIgnoreCase);
        var summaries = new List<DevotionSummary>();
        foreach (var devotion in devotions)
        {
            if (devotion is null)
            {
                continue;
            }

            map[devotion.Id] = devotion;
            summaries.Add(devotion.ToSummaryContract());
        }

        return new Catalog(map, summaries);
    });

    private async Task<DevotionDocument?> LoadDevotionAsync(string fileName)
    {
        try
        {
            var dto = await _fetcher.GetFromJsonAsync<DevotionDto>($"{DevotionsRoot}/{fileName}");
            return _parser.Parse(dto);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private sealed record Catalog(Dictionary<string, DevotionDocument> Devotions, IReadOnlyList<DevotionSummary> Summaries);
}
