using Respondeo.Content.Contracts;
using Respondeo.Content.Infrastructure;
using Respondeo.Content.Rendering;
using Respondeo.Content.Shared;

namespace Respondeo.Content.Articles;

/// <summary>
/// Loads the bundled articles from static Markdown files shipped by the Respondeo.Content library under the
/// <c>_content/Respondeo.Content/discover/articles</c> static-web-asset path.
/// Runs entirely client-side: fetches files via <see cref="HttpClient"/>, delegates parsing to <see cref="ArticleParser"/>,
/// and caches the parsed articles and derived index in memory for the app's lifetime.
/// </summary>
internal sealed class ArticleService(HttpClient http, IContentHtmlRenderer html) : IArticleService
{
    private const string ArticlesRoot = "_content/Respondeo.Content/discover/articles";
    private const string ManifestPath = ArticlesRoot + "/articles-manifest.json";

    private readonly ArticleParser _parser = new(html);
    private readonly ContentFetcher _fetcher = new(http, ContentCachePolicy.Immutable);
    private readonly AsyncInitCache<Catalog> _catalog = new();

    /// <summary>Returns the browse index, loading the catalog once and caching it.</summary>
    public async Task<IReadOnlyList<ArticleSummary>> GetIndexAsync() => (await Load()).Summaries;

    /// <summary>Returns the full content of a single article by id, or null if it does not exist.</summary>
    public async Task<Article?> GetArticleAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var catalog = await Load();
        return catalog.Articles.TryGetValue(id, out var article) ? article.ToContract() : null;
    }

    private Task<Catalog> Load() => _catalog.GetAsync(async () =>
    {
        var manifest = await _fetcher.GetFromJsonAsync<ContentManifest>(ManifestPath) ?? new ContentManifest();

        var articles = await Task.WhenAll(manifest.Files.Select(LoadArticleAsync));

        var map = new Dictionary<string, ArticleDocument>(StringComparer.OrdinalIgnoreCase);
        var summaries = new List<ArticleSummary>();
        foreach (var article in articles)
        {
            if (article is null)
            {
                continue;
            }

            map[article.Id] = article;
            summaries.Add(article.ToSummaryContract());
        }

        return new Catalog(map, summaries);
    });

    private async Task<ArticleDocument?> LoadArticleAsync(string fileName)
    {
        try
        {
            var raw = await _fetcher.GetStringAsync($"{ArticlesRoot}/{fileName}");
            return _parser.Parse(raw);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private sealed record Catalog(Dictionary<string, ArticleDocument> Articles, IReadOnlyList<ArticleSummary> Summaries);
}
