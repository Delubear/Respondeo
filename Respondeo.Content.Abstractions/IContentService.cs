namespace Respondeo.Content.Abstractions;

/// <summary>
/// The public contract for reading author-curated content nodes.
/// Implementations own the loading, parsing, and caching details; consumers depend only on this surface.
/// </summary>
public interface IContentService
{
    /// <summary>Returns every loaded node, loading the content set if needed.</summary>
    Task<IReadOnlyCollection<ContentNode>> GetAllAsync();

    /// <summary>Returns a single node by id, or null if it does not exist.</summary>
    Task<ContentNode?> GetByIdAsync(string id);

    /// <summary>Returns the top-level entry-point nodes for the home page.</summary>
    Task<IReadOnlyList<ContentNode>> GetEntryPointsAsync();
}
