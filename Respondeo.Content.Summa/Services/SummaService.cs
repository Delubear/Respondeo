using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Respondeo.Content.Summa.Services;

/// <summary>
/// Loads the bundled Summa Theologica from static JSON files shipped by the Respondeo.Content.Summa
/// library (produced by the SummaImporter tool). The lightweight browse/search index is fetched once
/// and cached; each question's full content is fetched on demand and cached individually so the
/// initial load stays small even though the whole corpus is bundled.
/// </summary>
internal sealed class SummaService(HttpClient http) : ISummaService
{
    private const string SummaRoot = "_content/Respondeo.Content.Summa/summa";
    private const string IndexPath = SummaRoot + "/summa-index.json";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, SummaQuestionContent> _questions = new(StringComparer.OrdinalIgnoreCase);
    private SummaIndex? _index;

    /// <summary>Returns the browse/search index, loading it once and caching it.</summary>
    public async Task<SummaIndex> GetIndexAsync()
    {
        if (_index is not null)
        {
            return _index;
        }

        await _gate.WaitAsync();
        try
        {
            _index ??= await GetFromJsonCachedAsync<SummaIndex>(IndexPath) ?? new SummaIndex();
            return _index;
        }
        finally
        {
            _gate.Release();
        }
    }

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
                var content = await GetFromJsonCachedAsync<SummaQuestionContent>($"{SummaRoot}/{PartFolder(id)}/{id}.json");
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

    // The bundled Summa corpus is large but immutable for the lifetime of a deploy, so it is cached
    // aggressively: each fetched item is held in-memory for the session (above), and the HTTP request
    // opts into the browser cache so repeat visits and reloads reuse the stored JSON instead of
    // re-downloading it. The static assets are fingerprinted per deploy, so a new build produces new
    // URLs and there is no risk of serving stale content.
    private async Task<T?> GetFromJsonCachedAsync<T>(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Headers =
            {
                CacheControl = new CacheControlHeaderValue
                {
                    // Prefer a stored response and treat it as fresh for up to a year.
                    MaxAge = TimeSpan.FromDays(365),
                }
            }
        };

        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    // Question content is split into one subfolder per part. The importer names those folders after
    // the human-readable part title in kebab-case; a question id is "<partId>-q<number>", so the part
    // id is the prefix before the first '-'.
    private static string PartFolder(string questionId)
    {
        var separator = questionId.IndexOf('-');
        var partId = separator > 0 ? questionId[..separator] : questionId;
        return partId switch
        {
            "fp" => "first-part",
            "fs" => "first-part-of-the-second-part",
            "ss" => "second-part-of-the-second-part",
            "tp" => "third-part",
            "xp" => "supplement",
            _ => partId,
        };
    }
}
