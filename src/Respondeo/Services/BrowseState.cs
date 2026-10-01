using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Respondeo.Services;

/// <summary>
/// One generic store for every browse page's transient view state (search text and facet filters).
/// Each list page owns a <see cref="BrowseSection"/> keyed by its own area path (e.g. <c>discover/prayers</c>
/// or <c>summa</c>). On every navigation this store resets only the sections whose area the visitor has
/// left, so each sub-area (Articles, Prayers, Devotions, Miracles, Summa) clears independently rather than
/// all of Discover resetting together. A section may carry an optional <c>onExit</c> callback to forget its
/// matching JS-owned store (scroll snapshot, accordion state) when it resets.
/// </summary>
/// <remarks>
/// Registered as a scoped service, which in Blazor WebAssembly lives for the whole app session, so the state
/// outlives the browse-page remounts that happen when drilling into a detail page and pressing Back. The
/// per-section area lifecycle replaces the former per-instance <c>AreaBrowseState</c> base class, moving the
/// reset boundary from the service to the individual section so one store can serve every area at once.
/// </remarks>
public sealed class BrowseState : IDisposable
{
    private readonly NavigationManager _nav;
    private readonly List<BrowseSection> _sections = [];

    public BrowseState(NavigationManager nav)
    {
        _nav = nav;
        _nav.LocationChanged += OnLocationChanged;
    }

    /// <summary>
    /// Returns the (lazily created) browse section for <paramref name="area"/>. A route counts as "in the
    /// section" when it equals <paramref name="area"/> or begins with it followed by "/", so drilling into a
    /// detail page keeps the state while leaving the sub-area clears it.
    /// </summary>
    /// <param name="area">The path that scopes the section, e.g. <c>discover/prayers</c> or <c>summa</c>.</param>
    /// <param name="onExit">
    /// Optional callback invoked when the section resets on area exit; used to forget the section's JS-owned
    /// store. Fired fire-and-forget by the synchronous navigation handler.
    /// </param>
    public BrowseSection Section(string area, Func<ValueTask>? onExit = null)
    {
        foreach (var section in _sections)
        {
            if (string.Equals(section.Area, area, StringComparison.OrdinalIgnoreCase))
            {
                return section;
            }
        }

        var created = new BrowseSection(area, onExit);
        _sections.Add(created);
        return created;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var path = new Uri(e.Location).AbsolutePath.Trim('/');
        foreach (var section in _sections)
        {
            var inArea = path.Equals(section.Area, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(section.Area + "/", StringComparison.OrdinalIgnoreCase);

            if (!inArea)
            {
                section.Reset();
            }
        }
    }

    // Only unhooks a managed event; there are no unmanaged resources and no finalizer,
    // so GC.SuppressFinalize would be a no-op. Suppressed rather than adding meaningless noise.
    [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "No finalizer to suppress; type holds only managed state.")]
    public void Dispose() => _nav.LocationChanged -= OnLocationChanged;
}

/// <summary>
/// The remembered browse view state for a single area: one free-text query plus any number of independent
/// facet filter selections keyed by a facet name (e.g. <c>tag</c>, <c>category</c>, <c>type</c>). Created and
/// owned by <see cref="BrowseState"/>; pages read and write it so their search survives remounts.
/// </summary>
public sealed class BrowseSection
{
    private readonly Func<ValueTask>? _onExit;
    private readonly Dictionary<string, HashSet<string>> _filters = new(StringComparer.Ordinal);

    internal BrowseSection(string area, Func<ValueTask>? onExit)
    {
        Area = area;
        _onExit = onExit;
    }

    /// <summary>The area path that scopes this section's reset lifecycle.</summary>
    public string Area { get; }

    /// <summary>The active free-text search query, or an empty string when browsing the full list.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// The remembered set of selected values for <paramref name="facet"/> (e.g. the chosen prayer
    /// categories), or an empty set if none. Several facets can be kept independently within one section.
    /// </summary>
    public IReadOnlySet<string> GetFilter(string facet) => _filters.TryGetValue(facet, out var set) ? set : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Records the current selection for <paramref name="facet"/>.</summary>
    public void SetFilter(string facet, IReadOnlySet<string> selected) => _filters[facet] = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);

    /// <summary>True when the query or any facet filter is active.</summary>
    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(Query) || _filters.Values.Any(set => set.Count > 0);

    /// <summary>Clears the query and every facet selection. Does not touch the JS-owned store.</summary>
    public void Clear()
    {
        Query = string.Empty;
        _filters.Clear();
    }

    // Called by BrowseState when the visitor leaves this section's area: clear the in-memory state and
    // fire the optional JS cleanup so a later return starts fresh.
    internal void Reset()
    {
        Clear();
        if (_onExit is not null)
        {
            _ = _onExit();
        }
    }
}
