namespace Respondeo.Content.Contracts;

/// <summary>
/// The public contract for reading the bundled prayer treasury.
/// Implementations own loading, parsing, and caching; consumers depend only on this surface.
/// The lightweight index is loaded once for browsing, while a single prayer's full text (with any paired Latin) is fetched on demand.
/// </summary>
public interface IPrayerService
{
    /// <summary>Returns the browse index (a summary for every primary-language prayer), loading it if needed.</summary>
    Task<IReadOnlyList<PrayerSummary>> GetIndexAsync();

    /// <summary>Returns the full text of a single prayer by id, or null if it does not exist.</summary>
    Task<Prayer?> GetPrayerAsync(string id);
}
