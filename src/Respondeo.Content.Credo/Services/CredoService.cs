using Respondeo.Content.Abstractions;
using Respondeo.Content.Credo.Internal;

namespace Respondeo.Content.Credo.Services;

/// <summary>
/// Loads the bundled Credo content (prayers, devotions, articles) from static files shipped by the
/// Respondeo.Content.Credo library under the <c>_content/Respondeo.Content.Credo/</c> static-web-asset
/// path. Runs entirely client-side: fetches files via <see cref="HttpClient"/>, delegates parsing to
/// <see cref="CredoParser"/>, and caches the parsed items and derived index in memory for the app's
/// lifetime. The whole hand-authored catalog is small, so it is loaded once up front.
/// </summary>
internal sealed class CredoService : ICredoService
{
    private const string CredoRoot = "_content/Respondeo.Content.Credo/credo";
    private const string ManifestPath = CredoRoot + "/credo-manifest.json";

    private readonly CredoParser _parser;
    private readonly ContentFetcher _fetcher;
    private readonly AsyncInitCache<Catalog> _catalog = new();

    // The bundled catalog is immutable for the lifetime of a deploy and the static assets are
    // fingerprinted per build, so requests opt into the browser cache for a year with no risk of
    // serving stale content across deploys.
    public CredoService(HttpClient http, IContentHtmlRenderer html)
    {
        _parser = new CredoParser(html);
        _fetcher = new ContentFetcher(http, ContentCachePolicy.Immutable);
    }

    /// <summary>Returns the browse index, loading the catalog once and caching it.</summary>
    public async Task<CredoIndex> GetIndexAsync() => (await Load()).Index;

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

    /// <summary>Returns the full definition of a single devotion by id, or null if it does not exist.</summary>
    public async Task<Devotion?> GetDevotionAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var catalog = await Load();
        return catalog.Devotions.TryGetValue(id, out var devotion) ? devotion : null;
    }

    /// <summary>Returns the full content of a single article by id, or null if it does not exist.</summary>
    public async Task<Article?> GetArticleAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var catalog = await Load();
        return catalog.Articles.TryGetValue(id, out var article) ? article : null;
    }

    private Task<Catalog> Load() => _catalog.GetAsync(async () =>
    {
        var manifest = await _fetcher.GetFromJsonAsync<CredoManifest>(ManifestPath) ?? new CredoManifest();

        var prayers = await Task.WhenAll(manifest.Prayers.Select(LoadPrayerAsync));
        var devotions = await Task.WhenAll(manifest.Devotions.Select(LoadDevotionAsync));
        var articles = await Task.WhenAll(manifest.Articles.Select(LoadArticleAsync));

        var loadedPrayers = prayers.Where(p => p is not null).Cast<Prayer>().ToList();
        PairTranslations(loadedPrayers);

        // Every prayer (including Latin) stays addressable by id, but only the primary-language
        // prayers appear in the browse index so translations are not listed as separate rows.
        var prayerMap = loadedPrayers.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        var prayerSummaries = loadedPrayers
            .Where(IsPrimaryLanguage)
            .Select(CredoParser.ToSummary)
            .ToList();

        var devotionMap = BuildMap(devotions, d => d.Id, out var devotionSummaries, CredoParser.ToSummary);
        var articleMap = BuildMap(articles, a => a.Id, out var articleSummaries, CredoParser.ToSummary);

        var index = new CredoIndex
        {
            Prayers = prayerSummaries,
            Devotions = devotionSummaries,
            Articles = articleSummaries,
        };

        return new Catalog(prayerMap, devotionMap, articleMap, index);
    });

    private sealed record Catalog(
        Dictionary<string, Prayer> Prayers,
        Dictionary<string, Devotion> Devotions,
        Dictionary<string, Article> Articles,
        CredoIndex Index);

    // A prayer is "primary" (browsable) unless it is a Latin translation of another prayer.
    private static bool IsPrimaryLanguage(Prayer prayer) =>
        !string.Equals(prayer.Language, "la", StringComparison.OrdinalIgnoreCase);

    // Populate each primary prayer's LatinHtml from the Latin file that shares its translation key,
    // so the detail page can render the two languages together as before.
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

    // Assembles a case-insensitive id->item map and a parallel, load-ordered list of summaries, skipping
    // any items that failed to load (null).
    private static Dictionary<string, TItem> BuildMap<TItem, TSummary>(
        IEnumerable<TItem?> items,
        Func<TItem, string> idOf,
        out IReadOnlyList<TSummary> summaries,
        Func<TItem, TSummary> toSummary)
        where TItem : class
    {
        var map = new Dictionary<string, TItem>(StringComparer.OrdinalIgnoreCase);
        var list = new List<TSummary>();
        foreach (var item in items)
        {
            if (item is null)
            {
                continue;
            }

            map[idOf(item)] = item;
            list.Add(toSummary(item));
        }

        summaries = list;
        return map;
    }

    private async Task<Prayer?> LoadPrayerAsync(string fileName)
    {
        try
        {
            var raw = await _fetcher.GetStringAsync($"{CredoRoot}/prayers/{fileName}");
            return _parser.ParsePrayer(raw);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private async Task<Article?> LoadArticleAsync(string fileName)
    {
        try
        {
            var raw = await _fetcher.GetStringAsync($"{CredoRoot}/articles/{fileName}");
            return _parser.ParseArticle(raw);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private async Task<Devotion?> LoadDevotionAsync(string fileName)
    {
        try
        {
            var dto = await _fetcher.GetFromJsonAsync<DevotionDto>($"{CredoRoot}/devotions/{fileName}");
            return _parser.ParseDevotion(dto);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private sealed class CredoManifest
    {
        public List<string> Prayers { get; set; } = [];
        public List<string> Devotions { get; set; } = [];
        public List<string> Articles { get; set; } = [];
    }
}
