using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// Typed wrapper over the <c>window.respondeoBrowseState</c> JS module (see <c>wwwroot/js/browse-state.js</c>),
/// which remembers each browse list's scroll position and (optionally)
/// its accordion open/closed state in <c>sessionStorage</c> so a visitor who drills into an item and presses Back returns to exactly where they were.
/// </summary>
/// <remarks>
/// Every browse page identifies its list by a short <paramref name="key"/> (e.g. <c>summa</c>,
/// <c>prayers</c>) that also appears as <c>data-browse-scroll="&lt;key&gt;"</c> on the list root in the markup.
/// Centralising the interop here keeps the JS method names in one place instead of scattering magic strings across the page components,
/// matching how <see cref="BreadcrumbTrail"/> and the other services wrap their JS.
/// </remarks>
public sealed class BrowseStateInterop(IJSRuntime js)
{
    /// <summary>Restores the remembered scroll position for the <paramref name="key"/> list.</summary>
    public ValueTask RestoreScrollAsync(string key) => js.InvokeVoidAsync("respondeoBrowseState.restoreScroll", key);

    /// <summary>
    /// Returns the ids of every currently-expanded accordion section for the <paramref name="key"/> list, used at mount to seed the initial <c>open</c> attributes.
    /// </summary>
    public ValueTask<string[]> GetOpenAsync(string key) => js.InvokeAsync<string[]>("respondeoBrowseState.getOpen", key);

    /// <summary>
    /// Forgets all remembered state (scroll and accordion) for the <paramref name="key"/> list.
    /// Pass this as a <see cref="BrowseSection"/> exit callback so the JS store resets when the visitor leaves the area.
    /// </summary>
    public ValueTask ClearAsync(string key) => js.InvokeVoidAsync("respondeoBrowseState.clear", key);
}
