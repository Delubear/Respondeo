using Respondeo.Content.Shared;

namespace Respondeo.Content.Summa.Services;

/// <summary>
/// Loads the bundled Summa Theologica from static JSON files shipped by the Respondeo.Content.Summa library (produced by the SummaImporter tool).
/// The lightweight browse/search index is fetched once and cached;
/// each question's full content is fetched on demand and cached individually so the initial load stays small even though the whole corpus is bundled.
/// </summary>
internal sealed class SummaService : ISummaService
{
    private const string SummaRoot = "_content/Respondeo.Content.Summa/summa";
    private const string IndexPath = SummaRoot + "/summa-index.json";

    private readonly ContentFetcher _fetcher;
    private readonly AsyncInitCache<SummaIndex> _index = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, SummaQuestionContent> _questions = new(StringComparer.OrdinalIgnoreCase);

    // The bundled Summa corpus is large but immutable for the lifetime of a deploy, so it is cached
    // aggressively: each fetched item is held in-memory for the session, and the HTTP request opts into
    // the browser cache so repeat visits reuse the stored JSON. The static assets are fingerprinted per
    // deploy, so a new build produces new URLs and there is no risk of serving stale content.
    public SummaService(HttpClient http) =>
        _fetcher = new ContentFetcher(http, ContentCachePolicy.Immutable);

    /// <summary>Returns the browse/search index, loading it once and caching it.</summary>
    public Task<SummaIndex> GetIndexAsync() =>
        _index.GetAsync(async () => await _fetcher.GetFromJsonAsync<SummaIndex>(IndexPath) ?? new SummaIndex());

    /// <summary>Returns the full content of a single question by id, or null if it does not exist.</summary>
    public async Task<SummaQuestionContent?> GetQuestionAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (_questions.TryGetValue(id, out var cached))
        {
            return cached;
        }

        await _gate.WaitAsync();
        try
        {
            if (_questions.TryGetValue(id, out cached))
            {
                return cached;
            }

            try
            {
                var content = await _fetcher.GetFromJsonAsync<SummaQuestionContent>($"{SummaRoot}/{PartFolder(id)}/{id}.json");
                if (content is not null)
                {
                    _questions[id] = content;
                }

                return content;
            }
            catch (HttpRequestException)
            {
                // A missing question file simply resolves to "not found" rather than breaking the page.
                return null;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    // Question content is split into one subfolder per part.
    // The folder is derived from the question's stable part key (the prefix before the first '-'),
    // via the shared SummaParts registry, so file layout stays independent of the URL slug and display label.
    private static string PartFolder(string questionId)
    {
        var separator = questionId.IndexOf('-');
        var partKey = separator > 0 ? questionId[..separator] : questionId;
        return SummaParts.FolderForKey(partKey);
    }
}
