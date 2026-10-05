using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// Typed wrapper over the small window-level navigation helpers (see <c>wwwroot/js/scroll.js</c>,
/// <c>wwwroot/js/site.js</c>) that pages use to keep a URL shareable and bring an element into view without triggering a full Blazor navigation.
/// </summary>
/// <remarks>
/// Centralising these calls keeps the JS method names in one place instead of scattering magic strings across page and component code,
/// matching how <see cref="BrowseStateInterop"/> and the other services wrap their JS.
/// Callers that run during teardown keep their own <see cref="JSDisconnectedException"/> handling.
/// </remarks>
public sealed class NavigationInterop(IJSRuntime js)
{
    /// <summary>
    /// Updates the address bar to <paramref name="url"/> via the History API (no navigation, focus shift, or scroll),
    /// so a deep link stays shareable while the visitor stays put.
    /// </summary>
    public ValueTask ReplaceUrlAsync(string url) => js.InvokeVoidAsync("respondeoUrl.replace", url);

    /// <summary>Smoothly scrolls the element named by <paramref name="target"/> (id or fragment) into view.</summary>
    public ValueTask ScrollIntoViewAsync(string target) => js.InvokeVoidAsync("respondeoScroll.intoView", target);

    /// <summary>Centers the element with <paramref name="elementId"/> within its scrollable parent.</summary>
    public ValueTask CentreInParentAsync(string elementId) => js.InvokeVoidAsync("respondeoScroll.centreInParent", elementId);

    /// <summary>Moves keyboard focus to the element with <paramref name="elementId"/>.</summary>
    public ValueTask FocusAsync(string elementId) => js.InvokeVoidAsync("respondeoFocus.byId", elementId);
}
