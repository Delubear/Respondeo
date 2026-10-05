namespace Respondeo.Content.Contracts;

/// <summary>
/// The public contract for reading the bundled, data-driven devotions (the Rosary, chaplets, litanies).
/// Implementations own loading, parsing, and caching; consumers depend only on this surface.
/// The lightweight index is loaded once for browsing, while a single devotion's full definition is fetched on demand.
/// </summary>
public interface IDevotionService
{
    /// <summary>Returns the browse index (a summary for every devotion), loading it if needed.</summary>
    Task<IReadOnlyList<DevotionSummary>> GetIndexAsync();

    /// <summary>Returns the full, data-driven definition of a single devotion by id, or null if it does not exist.</summary>
    Task<Devotion?> GetDevotionAsync(string id);

    /// <summary>
    /// Fetches every distinct prayer referenced anywhere in the devotion's sequence (including its
    /// per-mystery steps), keyed by prayer id, so a caller can render each step's text without
    /// further round trips. Ids that resolve to no prayer are omitted.
    /// </summary>
    Task<IReadOnlyDictionary<string, Prayer>> GetPrayersForAsync(Devotion devotion);
}
