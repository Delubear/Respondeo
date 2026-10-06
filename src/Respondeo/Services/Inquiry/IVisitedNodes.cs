namespace Respondeo.Services.Inquiry;

/// <summary>
/// Tracks the set of content nodes a visitor has opened during the current journey, so the UI can mark "where to go next" cards that lead to a node already read.
/// Unlike <see cref="IBreadcrumbTrail"/>, which reflects the current route and truncates on back-navigation,
/// this accumulates every node seen and only resets when the journey does (returning to the Start page).
/// </summary>
public interface IVisitedNodes
{
    /// <summary>
    /// Records that <paramref name="nodeId"/> has been visited. Idempotent: visiting an already-recorded node is a no-op.
    /// </summary>
    Task MarkVisitedAsync(string nodeId);

    /// <summary>
    /// Loads the set of visited node ids for the current journey.
    /// </summary>
    Task<IReadOnlySet<string>> GetVisitedAsync();

    /// <summary>
    /// Clears all visited nodes. Called when the visitor returns to the Start page so a new journey begins fresh.
    /// </summary>
    Task ClearAsync();
}
