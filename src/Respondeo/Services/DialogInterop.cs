using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// Typed wrapper over the <c>window.respondeoDialog</c> JS helper (see <c>wwwroot/js/site.js</c>) that opens and closes the native <c>&lt;dialog&gt;</c>
/// element as a modal (focus trapping, Esc-to-close, body scroll lock).
/// </summary>
/// <remarks>
/// Centralising these calls keeps the JS method names in one place instead of scattering magic strings across component code,
/// matching how <see cref="NavigationInterop"/> and the other services wrap their JS.
/// Callers that run during teardown keep their own <see cref="JSDisconnectedException"/> handling.
/// </remarks>
public sealed class DialogInterop(IJSRuntime js)
{
    /// <summary>Opens <paramref name="dialog"/> as a modal.</summary>
    public ValueTask ShowAsync(ElementReference dialog) => js.InvokeVoidAsync("respondeoDialog.show", dialog);

    /// <summary>Closes <paramref name="dialog"/> if it is open.</summary>
    public ValueTask CloseAsync(ElementReference dialog) => js.InvokeVoidAsync("respondeoDialog.close", dialog);
}
