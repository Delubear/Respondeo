using Respondeo.Content.Contracts;
using Respondeo.Content.Infrastructure;
using Respondeo.Content.Rendering;
using Respondeo.Content.Shared;
using System.Text.RegularExpressions;

namespace Respondeo.Content.Prayers;

/// <summary>
/// Loads the bundled prayer treasury from static Markdown files shipped by the Respondeo.Content library under the
/// <c>_content/Respondeo.Content/discover/prayers</c> static-web-asset path.
/// Runs entirely client-side: fetches files via <see cref="HttpClient"/>, delegates parsing to <see cref="PrayerParser"/>,
/// pairs each vernacular prayer with its Latin translation, and caches the parsed prayers and derived index in memory for the app's lifetime.
/// </summary>
internal sealed class PrayerService(HttpClient http, IContentHtmlRenderer html) :
    MarkdownContentLoader<PrayerFrontMatter, PrayerDocument>(new ContentFetcher(http, ContentCachePolicy.Immutable), new FrontMatterReader()), IPrayerService
{
    private const string PrayersRoot = DiscoverRoot + "/prayers";
    private const string ManifestPath = PrayersRoot + "/prayers-manifest.json";

    private readonly PrayerParser _parser = new(html);
    private readonly AsyncInitCache<Catalog> _catalog = new();

    // Markdig renders "[Label](prayer:some-id)" as an anchor whose href is the raw "prayer:some-id"
    // scheme. Match that anchor (optionally wrapped in its own <p>) so a reference on its own line is
    // replaced cleanly by the embedded prayer block.
    private static readonly Regex PrayerReference = new(
        "<p>\\s*<a href=\"prayer:(?<id>[^\"]+)\">(?<label>.*?)</a>\\s*</p>|<a href=\"prayer:(?<id>[^\"]+)\">(?<label>.*?)</a>",
        RegexOptions.Compiled | RegexOptions.Singleline);

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
        return catalog.Prayers.TryGetValue(id, out var prayer) ? prayer.ToContract() : null;
    }

    protected override PrayerDocument Map(PrayerFrontMatter meta, string body, string fileName) => _parser.Map(meta, body);

    private Task<Catalog> Load() => _catalog.GetAsync(async () =>
    {
        var loaded = (await LoadManifestAsync(PrayersRoot, ManifestPath)).ToList();
        PairTranslations(loaded);

        // Every prayer (including Latin) stays addressable by id, but only the primary-language,
        // listed prayers appear in the browse index so translations and contextual fragments
        // (e.g. Dominican versicles) are not shown as standalone catalog rows.
        var map = loaded.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        ExpandPrayerReferences(loaded, map);
        var summaries = loaded
            .Where(IsPrimaryLanguage)
            .Where(p => !p.Unlisted)
            .Select(p => p.ToSummaryContract())
            .ToList();

        return new Catalog(map, summaries);
    });

    // A prayer is "primary" (browsable) unless it is a Latin translation of another prayer.
    private static bool IsPrimaryLanguage(PrayerDocument prayer) =>
        !string.Equals(prayer.Language, "la", StringComparison.OrdinalIgnoreCase);

    // Replace standalone "[Title](prayer:some-id)" references with the full text of the referenced
    // prayer, inlined as a labelled block. Authors keep devotions like novenas readable by linking to
    // the Our Father / Hail Mary / Glory Be instead of repeating them; the service expands the links
    // at load time because, unlike the isolated parser, it has the whole catalog in hand.
    private static void ExpandPrayerReferences(IReadOnlyList<PrayerDocument> prayers, IReadOnlyDictionary<string, PrayerDocument> map)
    {
        foreach (var prayer in prayers)
        {
            if (prayer.Html.Contains("prayer:", StringComparison.Ordinal))
            {
                prayer.Html = PrayerReference.Replace(prayer.Html, match =>
                {
                    var id = match.Groups["id"].Value;
                    if (!map.TryGetValue(id, out var target))
                    {
                        // Unknown id: fall back to a plain link to the prayer page so nothing is lost.
                        return $"<a href=\"discover/prayers/{id}\">{match.Groups["label"].Value}</a>";
                    }

                    return $"<div class=\"prayer-embed\">" +
                        $"<p class=\"prayer-embed__label\">{target.Title}</p>" +
                        $"<div class=\"prayer-embed__text\">{target.Html}</div>" +
                        "</div>";
                });
            }
        }
    }

    // Populate each primary prayer's LatinHtml from the Latin file that shares its translation key,
    // so the detail page can render the two languages together.
    private static void PairTranslations(IReadOnlyList<PrayerDocument> prayers)
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
                prayer.LatinTitle = latin.Title;
            }
        }
    }

    private sealed record Catalog(Dictionary<string, PrayerDocument> Prayers, IReadOnlyList<PrayerSummary> Summaries);
}
