using Respondeo.Content.Shared;
using Respondeo.Content.Summa.Contracts;

namespace Respondeo.Content.Summa;

/// <summary>
/// Loads the bundled Summa Theologica from static JSON files shipped by the Respondeo.Content.Summa library (produced by the SummaImporter tool).
/// The lightweight browse/search index is fetched once and cached;
/// each question's full content is fetched on demand and cached individually so the initial load stays small even though the whole corpus is bundled.
/// </summary>
internal sealed class SummaService(HttpClient http, ISummaPartCatalog parts) : ISummaService
{
    private const string SummaRoot = "_content/Respondeo.Content.Summa/summa";
    private const string IndexPath = SummaRoot + "/summa-index.json";

    private readonly ContentFetcher _fetcher = new(http, ContentCachePolicy.Immutable);
    private readonly AsyncInitCache<SummaIndex> _index = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, SummaQuestionContent> _questions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the browse/search index, loading it once and caching it.</summary>
    public Task<SummaIndex> GetIndexAsync() => _index.GetAsync(async () => await _fetcher.GetFromJsonAsync<SummaIndex>(IndexPath) ?? new SummaIndex());

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
    // via the part catalog, so file layout stays independent of the URL slug and display label.
    private string PartFolder(string questionId)
    {
        var separator = questionId.IndexOf('-');
        var partKey = separator > 0 ? questionId[..separator] : questionId;
        return parts.FolderForKey(partKey);
    }
}
