using System.Net.Http.Headers;
using System.Net.Http.Json;
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
internal sealed class CredoService(HttpClient http, IContentHtmlRenderer html) : ICredoService
{
    private const string CredoRoot = "_content/Respondeo.Content.Credo/credo";
    private const string ManifestPath = CredoRoot + "/credo-manifest.json";

    private readonly CredoParser _parser = new(html);
    private readonly SemaphoreSlim _gate = new(1, 1);

    private Dictionary<string, Prayer>? _prayers;
    private Dictionary<string, Devotion>? _devotions;
    private Dictionary<string, Article>? _articles;
    private CredoIndex? _index;

    /// <summary>Returns the browse index, loading the catalog once and caching it.</summary>
    public async Task<CredoIndex> GetIndexAsync()
    {
        await EnsureLoadedAsync();
        return _index!;
    }

    /// <summary>Returns the full text of a single prayer by id, or null if it does not exist.</summary>
    public async Task<Prayer?> GetPrayerAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        await EnsureLoadedAsync();
        return _prayers!.TryGetValue(id, out var prayer) ? prayer : null;
    }

    /// <summary>Returns the full definition of a single devotion by id, or null if it does not exist.</summary>
    public async Task<Devotion?> GetDevotionAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        await EnsureLoadedAsync();
        return _devotions!.TryGetValue(id, out var devotion) ? devotion : null;
    }

    /// <summary>Returns the full content of a single article by id, or null if it does not exist.</summary>
    public async Task<Article?> GetArticleAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        await EnsureLoadedAsync();
        return _articles!.TryGetValue(id, out var article) ? article : null;
    }

    private async Task EnsureLoadedAsync()
    {
        if (_index is not null)
        {
            return;
        }

        await _gate.WaitAsync();
        try
        {
            if (_index is not null)
            {
                return;
            }

            var manifest = await GetFromJsonCachedAsync<CredoManifest>(ManifestPath) ?? new CredoManifest();

            var prayers = await Task.WhenAll(manifest.Prayers.Select(LoadPrayerAsync));
            var devotions = await Task.WhenAll(manifest.Devotions.Select(LoadDevotionAsync));
            var articles = await Task.WhenAll(manifest.Articles.Select(LoadArticleAsync));

            var loadedPrayers = prayers.Where(p => p is not null).Cast<Prayer>().ToList();
            PairTranslations(loadedPrayers);

            // Every prayer (including Latin) stays addressable by id, but only the primary-language
            // prayers appear in the browse index so translations are not listed as separate rows.
            _prayers = loadedPrayers.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
            var prayerSummaries = loadedPrayers
                .Where(IsPrimaryLanguage)
                .Select(CredoParser.ToSummary)
                .ToList();

            _devotions = BuildMap(devotions, d => d.Id, out var devotionSummaries, CredoParser.ToSummary);
            _articles = BuildMap(articles, a => a.Id, out var articleSummaries, CredoParser.ToSummary);

            _index = new CredoIndex
            {
                Prayers = prayerSummaries,
                Devotions = devotionSummaries,
                Articles = articleSummaries,
            };
        }
        finally
        {
            _gate.Release();
        }
    }

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
            var raw = await GetStringCachedAsync($"{CredoRoot}/prayers/{fileName}");
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
            var raw = await GetStringCachedAsync($"{CredoRoot}/articles/{fileName}");
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
            var dto = await GetFromJsonCachedAsync<DevotionDto>($"{CredoRoot}/devotions/{fileName}");
            return _parser.ParseDevotion(dto);
        }
        catch (HttpRequestException)
        {
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

    private sealed class CredoManifest
    {
        public List<string> Prayers { get; set; } = [];
        public List<string> Devotions { get; set; } = [];
        public List<string> Articles { get; set; } = [];
    }
}
