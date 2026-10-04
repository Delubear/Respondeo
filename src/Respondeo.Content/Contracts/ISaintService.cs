namespace Respondeo.Content.Contracts;

/// <summary>
/// The public contract for reading the bundled catalog of Catholic saints.
/// Implementations own loading, parsing, and caching; consumers depend only on this surface.
/// The lightweight index is loaded once for browsing/filtering/search, while a single saint's full content is fetched on demand.
/// </summary>
public interface ISaintService
{
    /// <summary>Returns the browse/search index (facets + summaries for every saint), loading it if needed.</summary>
    Task<SaintIndex> GetIndexAsync();

    /// <summary>
    /// Returns the slug&#8594;label catalog for the browse facets (era, region, state of life, canonization),
    /// loaded from the bundled <c>facets.json</c> content file so labels stay data-driven.
    /// </summary>
    Task<SaintFacetCatalog> GetFacetsAsync();

    /// <summary>Returns the full content of a single saint by id, or null if it does not exist.</summary>
    Task<SaintRecord?> GetByIdAsync(string id);
}
