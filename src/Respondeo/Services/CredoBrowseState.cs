using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// Remembers the transient browse state (search text) for the Credo list pages so that returning to
/// a list via the browser's Back button restores the visitor's search rather than a reset page. The
/// scroll position is owned by JavaScript (<c>window.respondeoCredoBrowse</c>, backed by
/// <c>sessionStorage</c>); this service coordinates the search text and clears both stores when the
/// visitor leaves the Credo area. Registered as a scoped service, which in Blazor WebAssembly lives
/// for the whole app session, so it outlives the repeated remounts of the browse pages.
/// </summary>
public sealed class CredoBrowseState : IDisposable
{
    private readonly NavigationManager _nav;
    private readonly IJSRuntime _js;
    private readonly Dictionary<string, string> _queries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _filters = new(StringComparer.Ordinal);
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

    public CredoBrowseState(NavigationManager nav, IJSRuntime js)
    {
        _nav = nav;
        _js = js;
        _nav.LocationChanged += OnLocationChanged;
    }

    /// <summary>The remembered search query for <paramref name="key"/>, or empty if none.</summary>
    public string GetQuery(string key) => _queries.TryGetValue(key, out var q) ? q : string.Empty;

    /// <summary>Records the current search query for <paramref name="key"/>.</summary>
    public void SetQuery(string key, string query)
    {
        _keys.Add(key);
        _queries[key] = query ?? string.Empty;
    }

    /// <summary>
    /// The remembered set of selected filter values for <paramref name="key"/> (e.g. the chosen
    /// prayer categories), or an empty set if none. A composite key such as "prayers.category" lets a
    /// single list page keep several independent facet selections.
    /// </summary>
    public IReadOnlySet<string> GetFilter(string key) =>
        _filters.TryGetValue(key, out var set) ? set : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Records the current filter selection for <paramref name="key"/>.</summary>
    public void SetFilter(string key, IReadOnlySet<string> selected)
    {
        _keys.Add(key);
        _filters[key] = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var path = new Uri(e.Location).AbsolutePath.Trim('/');
        var inCredo = path.Equals("credo", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("credo/", StringComparison.OrdinalIgnoreCase);

        // Item detail pages live under credo/...; those still count as "in Credo" so the browse state
        // is kept while drilling into an item and coming back. Only a route outside Credo clears it.
        if (!inCredo)
        {
            Reset();
        }
    }

    private void Reset()
    {
        _queries.Clear();
        _filters.Clear();

        // Forget the JS-owned scroll snapshots too so a later return to Credo starts at the top.
        // Fire-and-forget: LocationChanged is synchronous and we are leaving the area anyway.
        foreach (var key in _keys)
        {
            _ = _js.InvokeVoidAsync("respondeoCredoBrowse.clear", key);
        }

        _keys.Clear();
    }

    public void Dispose() => _nav.LocationChanged -= OnLocationChanged;
}
