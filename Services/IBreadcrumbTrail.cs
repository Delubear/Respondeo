namespace Respondeo.Services;

/// <summary>
/// Tracks the path of content nodes a visitor has actually navigated through, so the UI can render a breadcrumb of their journey.
/// Because the content is a graph (a node may have several parents), the trail reflects the route taken rather than a fixed structural hierarchy.
/// </summary>
public interface IBreadcrumbTrail
{
    /// <summary>
    /// Records a visit to <paramref name="nodeId"/> and returns the resulting trail (ordered from the first visited node to the current one, inclusive).
    /// Revisiting a node already in the trail truncates the trail back to that node, so loops and back-navigation don't accumulate duplicates.
    /// </summary>
    Task<IReadOnlyList<string>> VisitAsync(string nodeId);

    /// <summary>
    /// Clears the trail. Called when the visitor returns to the Start page so a new journey begins fresh rather than continuing a previous one.
    /// </summary>
    Task ClearAsync();
}
