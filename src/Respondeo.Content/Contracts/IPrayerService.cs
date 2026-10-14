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

    /// <summary>
    /// Replaces each <c>[Title](prayer:some-id)</c> reference in rendered content HTML (a saint profile, article, or miracle)
    /// with the referenced prayer's full text as a quotation, followed by a link to the prayer's own page.
    /// Unknown ids become a plain link; HTML without references is returned unchanged.
    /// </summary>
    Task<string> QuotePrayerReferencesAsync(string html);
}
