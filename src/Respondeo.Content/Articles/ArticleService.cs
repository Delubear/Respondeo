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
internal sealed class ArticleService(HttpClient http, IContentHtmlRenderer html) :
    MarkdownContentLoader<ArticleFrontMatter, ArticleDocument>(new ContentFetcher(http, ContentCachePolicy.Immutable), new FrontMatterReader()), IArticleService
{
    private const string ArticlesRoot = DiscoverRoot + "/articles";
    private const string ManifestPath = ArticlesRoot + "/articles-manifest.json";

    private readonly ArticleParser _parser = new(html);
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

    protected override ArticleDocument Map(ArticleFrontMatter meta, string body, string fileName) => _parser.Map(meta, body);

    private Task<Catalog> Load() => _catalog.GetAsync(async () =>
    {
        var articles = await LoadManifestAsync(ArticlesRoot, ManifestPath);
        var (byId, summaries) = BuildCatalog(articles, a => a.Id, a => a.ToSummaryContract());
        return new Catalog(byId, summaries);
    });

    private sealed record Catalog(Dictionary<string, ArticleDocument> Articles, IReadOnlyList<ArticleSummary> Summaries);
}
