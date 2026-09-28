using Microsoft.AspNetCore.Components;

namespace Respondeo.Services;

/// <summary>
/// Remembers the miracles browse view — the search query and the selected facet filters (type, approval, region) — across page remounts,
/// so leaving a miracle's detail page and pressing Back returns to the same filtered list.
/// Resets itself when the visitor leaves the miracles area. The shared navigation lifecycle lives in <see cref="AreaBrowseState"/>.
/// </summary>
/// <remarks>
/// The state is kept in memory rather than the URL: it is transient view state.
/// It resets itself whenever the visitor navigates out of the Discover area (any route that is not <c>/discover</c> or
/// <c>/discover/...</c>) so returning later starts fresh, while moving between the list and an individual miracle preserves it. This mirrors <see cref="SummaBrowseState"/>.
/// </remarks>
public sealed class MiracleBrowseState(NavigationManager nav) : AreaBrowseState(nav, "discover")
{

    /// <summary>The active free-text search query, or an empty string when browsing the full list.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>The selected miracle category slugs to include; empty means "all types".</summary>
    public HashSet<string> Types { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The selected approval status slugs to include; empty means "all statuses".</summary>
    public HashSet<string> Approvals { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The selected region slugs to include; empty means "all regions".</summary>
    public HashSet<string> Regions { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>True when any query or facet filter is active.</summary>
    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(Query) || Types.Count > 0 || Approvals.Count > 0 || Regions.Count > 0;

    /// <summary>Clears the query and every selected facet.</summary>
    public void Clear()
    {
        Query = string.Empty;
        Types.Clear();
        Approvals.Clear();
        Regions.Clear();
    }

    protected override void OnExitArea() => Clear();
}
