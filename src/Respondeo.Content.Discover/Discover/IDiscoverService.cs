namespace Respondeo.Content.Discover;

/// <summary>
/// The public contract for reading the bundled Discover content (prayers, devotions, and articles for
/// living the Catholic life). Implementations own loading, parsing, and caching; consumers depend only
/// on this surface. The lightweight index is loaded once for browsing, while a single item's full
/// content is fetched on demand.
/// </summary>
public interface IDiscoverService
{
    /// <summary>Returns the browse index (summaries for every prayer, devotion, and article), loading it if needed.</summary>
    Task<DiscoverIndex> GetIndexAsync();

    /// <summary>Returns the full text of a single prayer by id, or null if it does not exist.</summary>
    Task<Prayer?> GetPrayerAsync(string id);

    /// <summary>Returns the full, data-driven definition of a single devotion by id, or null if it does not exist.</summary>
    Task<Devotion?> GetDevotionAsync(string id);

    /// <summary>Returns the full content of a single article by id, or null if it does not exist.</summary>
    Task<Article?> GetArticleAsync(string id);
}
