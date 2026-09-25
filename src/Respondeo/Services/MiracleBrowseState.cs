using Microsoft.AspNetCore.Components;
using Respondeo.Content.Miracles;

namespace Respondeo.Services;

/// <summary>
/// Remembers the miracles browse view — the search query and the selected facet filters (type,
/// approval, region) — across page remounts, so leaving a miracle's detail page and pressing Back
/// returns to the same filtered list. Registered as a scoped service, which in Blazor WebAssembly
/// lives for the whole app session, so the state outlives the repeated remounts of the browse page.
/// </summary>
/// <remarks>
/// The state is kept in memory rather than the URL: it is transient view state. It resets itself
/// whenever the visitor navigates out of the miracles area (any route that is not <c>/miracles</c> or
/// <c>/miracles/...</c>) so returning later starts fresh, while moving between the list and an
/// individual miracle preserves it. This mirrors <see cref="SummaBrowseState"/>.
/// </remarks>
public sealed class MiracleBrowseState : IDisposable
{
    private readonly NavigationManager _nav;

    public MiracleBrowseState(NavigationManager nav)
    {
        _nav = nav;
        _nav.LocationChanged += OnLocationChanged;
    }

    /// <summary>The active free-text search query, or an empty string when browsing the full list.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>The selected miracle types to include; empty means "all types".</summary>
    public HashSet<MiracleType> Types { get; } = [];

    /// <summary>The selected approval statuses to include; empty means "all statuses".</summary>
    public HashSet<ApprovalStatus> Approvals { get; } = [];

    /// <summary>The selected regions to include; empty means "all regions".</summary>
    public HashSet<MiracleRegion> Regions { get; } = [];

    /// <summary>True when any query or facet filter is active.</summary>
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(Query) || Types.Count > 0 || Approvals.Count > 0 || Regions.Count > 0;

    /// <summary>Toggles a type facet on or off.</summary>
    public void ToggleType(MiracleType type) => Toggle(Types, type);

    /// <summary>Toggles an approval facet on or off.</summary>
    public void ToggleApproval(ApprovalStatus approval) => Toggle(Approvals, approval);

    /// <summary>Toggles a region facet on or off.</summary>
    public void ToggleRegion(MiracleRegion region) => Toggle(Regions, region);

    /// <summary>Clears the query and every selected facet.</summary>
    public void Clear()
    {
        Query = string.Empty;
        Types.Clear();
        Approvals.Clear();
        Regions.Clear();
    }

    private static void Toggle<T>(HashSet<T> set, T value)
    {
        if (!set.Add(value))
        {
            set.Remove(value);
        }
    }

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        var path = new Uri(e.Location).AbsolutePath.Trim('/');
        var inMiracles = path.Equals("miracles", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("miracles/", StringComparison.OrdinalIgnoreCase);

        if (!inMiracles)
        {
            Clear();
        }
    }

    public void Dispose() => _nav.LocationChanged -= OnLocationChanged;
}
