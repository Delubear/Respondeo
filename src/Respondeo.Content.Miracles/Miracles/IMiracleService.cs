namespace Respondeo.Content.Miracles;

/// <summary>
/// The public contract for reading the bundled catalog of Catholic miracles.
/// Implementations own loading, parsing, and caching; consumers depend only on this surface.
/// The lightweight index is loaded once for browsing/filtering/search, while a single miracle's full
/// content is fetched on demand.
/// </summary>
public interface IMiracleService
{
    /// <summary>Returns the browse/search index (facets + summaries for every miracle), loading it if needed.</summary>
    Task<MiracleIndex> GetIndexAsync();

    /// <summary>Returns the full content of a single miracle by id, or null if it does not exist.</summary>
    Task<MiracleRecord?> GetByIdAsync(string id);
}
