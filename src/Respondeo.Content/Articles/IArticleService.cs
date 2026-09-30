namespace Respondeo.Content.Discover.Articles;

/// <summary>
/// The public contract for reading the bundled articles (deeper dives on the sacraments, practice, apologetics).
/// Implementations own loading, parsing, and caching; consumers depend only on this surface.
/// The lightweight index is loaded once for browsing, while a single article's full content is fetched on demand.
/// </summary>
public interface IArticleService
{
    /// <summary>Returns the browse index (a summary for every article), loading it if needed.</summary>
    Task<IReadOnlyList<ArticleSummary>> GetIndexAsync();

    /// <summary>Returns the full content of a single article by id, or null if it does not exist.</summary>
    Task<Article?> GetArticleAsync(string id);
}
