using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// Remembers the transient browse state (search text and facet filters)
/// for the Credo list pages so that returning to a list via the browser's Back button restores the visitor's search rather than a reset page.
/// The scroll position is owned by JavaScript (<c>window.respondeoCredoBrowse</c>, backed by <c>sessionStorage</c>);
/// this service coordinates the search text and clears both stores when the visitor leaves the Credo area. The shared navigation lifecycle lives in
/// <see cref="AreaBrowseState"/>.
/// </summary>
public sealed class CredoBrowseState(NavigationManager nav, IJSRuntime js) : AreaBrowseState(nav, "credo")
{
    private readonly Dictionary<string, string> _queries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _filters = new(StringComparer.Ordinal);
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

    /// <summary>The remembered search query for <paramref name="key"/>, or empty if none.</summary>
    public string GetQuery(string key) => _queries.TryGetValue(key, out var q) ? q : string.Empty;

    /// <summary>Records the current search query for <paramref name="key"/>.</summary>
    public void SetQuery(string key, string query)
    {
        _keys.Add(key);
        _queries[key] = query ?? string.Empty;
    }

    /// <summary>
    /// The remembered set of selected filter values for <paramref name="key"/> (e.g. the chosen prayer categories), or an empty set if none.
    /// A composite key such as "prayers.category" lets a single list page keep several independent facet selections.
    /// </summary>
    public IReadOnlySet<string> GetFilter(string key) => _filters.TryGetValue(key, out var set) ? set : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Records the current filter selection for <paramref name="key"/>.</summary>
    public void SetFilter(string key, IReadOnlySet<string> selected)
    {
        _keys.Add(key);
        _filters[key] = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);
    }

    protected override void OnExitArea()
    {
        _queries.Clear();
        _filters.Clear();

        // Forget the JS-owned scroll snapshots too so a later return to Credo starts at the top.
        // Fire-and-forget: LocationChanged is synchronous and we are leaving the area anyway.
        foreach (var key in _keys)
        {
            _ = js.InvokeVoidAsync("respondeoCredoBrowse.clear", key);
        }

        _keys.Clear();
    }
}
