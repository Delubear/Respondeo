using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// Typed wrapper over the <c>window.respondeoAccessibility</c> JS helper (see <c>wwwroot/js/site.js</c>)
/// that posts the current page title to a polite live region so assistive technology announces SPA route changes.
/// </summary>
/// <remarks>
/// Centralising this call keeps the JS method name in one place instead of scattering magic strings across layout code,
/// matching how <see cref="NavigationInterop"/> and the other services wrap their JS.
/// Callers that run during teardown keep their own <see cref="JSDisconnectedException"/> handling.
/// </remarks>
public sealed class AccessibilityInterop(IJSRuntime js)
{
    /// <summary>Announces the current page to assistive technology via the polite live region.</summary>
    public ValueTask AnnounceAsync() => js.InvokeVoidAsync("respondeoAccessibility.announce");
}
