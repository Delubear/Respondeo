using Respondeo.Content.Shared;

namespace Respondeo.Content.Prayers;

/// <summary>
/// Loads the bundled prayer treasury from static Markdown files shipped by the Respondeo.Content library under the
/// <c>_content/Respondeo.Content/discover/prayers</c> static-web-asset path.
/// Runs entirely client-side: fetches files via <see cref="HttpClient"/>, delegates parsing to <see cref="PrayerParser"/>,
/// pairs each vernacular prayer with its Latin translation, and caches the parsed prayers and derived index in memory for the app's lifetime.
/// </summary>
internal sealed class PrayerService(HttpClient http, IContentHtmlRenderer html) : IPrayerService
{
    private const string PrayersRoot = "_content/Respondeo.Content/discover/prayers";
    private const string ManifestPath = PrayersRoot + "/prayers-manifest.json";

    private readonly PrayerParser _parser = new(html);
    private readonly ContentFetcher _fetcher = new(http, ContentCachePolicy.Immutable);
    private readonly AsyncInitCache<Catalog> _catalog = new();

    /// <summary>Returns the browse index, loading the catalog once and caching it.</summary>
    public async Task<IReadOnlyList<PrayerSummary>> GetIndexAsync() => (await Load()).Summaries;

    /// <summary>Returns the full text of a single prayer by id, or null if it does not exist.</summary>
    public async Task<Prayer?> GetPrayerAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var catalog = await Load();
        return catalog.Prayers.TryGetValue(id, out var prayer) ? prayer : null;
    }

    private Task<Catalog> Load() => _catalog.GetAsync(async () =>
    {
        var manifest = await _fetcher.GetFromJsonAsync<ContentManifest>(ManifestPath) ?? new ContentManifest();

        var prayers = await Task.WhenAll(manifest.Files.Select(LoadPrayerAsync));
        var loaded = prayers.Where(p => p is not null).Cast<Prayer>().ToList();
        PairTranslations(loaded);

        // Every prayer (including Latin) stays addressable by id, but only the primary-language
        // prayers appear in the browse index so translations are not listed as separate rows.
        var map = loaded.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        var summaries = loaded
            .Where(IsPrimaryLanguage)
            .Select(PrayerParser.ToSummary)
            .ToList();

        return new Catalog(map, summaries);
    });

    private async Task<Prayer?> LoadPrayerAsync(string fileName)
    {
        try
        {
            var raw = await _fetcher.GetStringAsync($"{PrayersRoot}/{fileName}");
            return _parser.Parse(raw);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    // A prayer is "primary" (browsable) unless it is a Latin translation of another prayer.
    private static bool IsPrimaryLanguage(Prayer prayer) =>
        !string.Equals(prayer.Language, "la", StringComparison.OrdinalIgnoreCase);

    // Populate each primary prayer's LatinHtml from the Latin file that shares its translation key,
    // so the detail page can render the two languages together.
    private static void PairTranslations(IReadOnlyList<Prayer> prayers)
    {
        var latinByKey = prayers
            .Where(p => !IsPrimaryLanguage(p) && !string.IsNullOrWhiteSpace(p.TranslationKey))
            .GroupBy(p => p.TranslationKey!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var prayer in prayers)
        {
            if (IsPrimaryLanguage(prayer)
                && prayer.TranslationKey is not null
                && latinByKey.TryGetValue(prayer.TranslationKey, out var latin))
            {
                prayer.LatinHtml = latin.Html;
            }
        }
    }

    private sealed record Catalog(Dictionary<string, Prayer> Prayers, IReadOnlyList<PrayerSummary> Summaries);
}
