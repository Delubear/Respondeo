using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// Remembers the Summa search text across page remounts so it survives leaving the page and pressing
/// Back. Registered as a scoped service, which in Blazor WebAssembly lives for the whole app session,
/// so the state outlives the repeated remounts of <c>Summa.razor</c> that happen on navigation.
/// </summary>
/// <remarks>
/// Which parts and treatises are expanded is deliberately NOT tracked here. A <c>&lt;details&gt;</c>
/// element is natively browser-controlled; having Blazor own its <c>open</c> attribute (and react to
/// its <c>toggle</c> event) creates a re-render/re-apply feedback loop that made the page "jump"
/// endlessly on Back. Instead the accordion state is persisted purely in JavaScript
/// (<c>window.respondeoSummaBrowse</c>, backed by <c>sessionStorage</c>), so Blazor never re-renders
/// on a toggle. This service just clears that JS store when the visitor leaves the Summa area.
/// <para>
/// The state is deliberately kept in memory/sessionStorage rather than the URL: it is transient view
/// state, not a shareable address. It resets itself whenever the visitor navigates out of the Summa
/// area (any route that is not <c>/summa</c> or <c>/summa/...</c>) so that returning to the Summa
/// later from elsewhere starts fresh, while moving between the list, a search, and an individual
/// question preserves it.
/// </para>
/// </remarks>
public sealed class SummaBrowseState : IDisposable
{
    private readonly NavigationManager _nav;
    private readonly IJSRuntime _js;

    public SummaBrowseState(NavigationManager nav, IJSRuntime js)
    {
        _nav = nav;
        _js = js;
        _nav.LocationChanged += OnLocationChanged;
    }

    /// <summary>The active search query, or an empty string when browsing the full list.</summary>
    public string Query { get; set; } = string.Empty;

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        var path = new Uri(e.Location).AbsolutePath.Trim('/');
        var inSumma = path.Equals("summa", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("summa/", StringComparison.OrdinalIgnoreCase);

        // Individual question pages live at summa/{id}; those still count as "in Summa" so the browse
        // state is kept while drilling into a question and coming back. Only a route outside the
        // Summa area clears it.
        if (!inSumma)
        {
            Reset();
        }
    }

    private void Reset()
    {
        Query = string.Empty;

        // Clear the JS-owned accordion snapshot too so a later return to the Summa starts collapsed.
        // Fire-and-forget: LocationChanged is synchronous and we are leaving the area anyway.
        _ = _js.InvokeVoidAsync("respondeoSummaBrowse.clear");
    }

    public void Dispose() => _nav.LocationChanged -= OnLocationChanged;
}
