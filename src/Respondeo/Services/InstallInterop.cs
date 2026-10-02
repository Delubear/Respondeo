using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// Typed wrapper over the <c>window.respondeoInstall</c> JS module (see <c>wwwroot/js/install.js</c>) that
/// drives the PWA install affordance: registering for availability changes, triggering the browser prompt,
/// and remembering a dismissal.
/// </summary>
/// <remarks>
/// Centralising these calls keeps the JS method names in one place instead of scattering magic strings across component code,
/// matching how <see cref="SummaRefInterop"/> and the other services wrap their JS (including passing a <see cref="DotNetObjectReference{T}"/> for callbacks).
/// Callers that run during teardown keep their own <see cref="JSDisconnectedException"/> handling.
/// </remarks>
public sealed class InstallInterop(IJSRuntime js)
{
    /// <summary>
    /// Registers the component's <see cref="DotNetObjectReference{T}"/> so availability changes call back into it,
    /// returning whether an install prompt is currently available.
    /// </summary>
    public ValueTask<bool> RegisterAsync<T>(DotNetObjectReference<T> component) where T : class =>
        js.InvokeAsync<bool>("respondeoInstall.register", component);

    /// <summary>Triggers the browser's install prompt, returning whether the user accepted.</summary>
    public ValueTask<bool> PromptAsync() => js.InvokeAsync<bool>("respondeoInstall.prompt");

    /// <summary>Remembers that the reader dismissed the banner so it does not reappear on future visits.</summary>
    public ValueTask DismissAsync() => js.InvokeVoidAsync("respondeoInstall.dismiss");

    /// <summary>Releases the registered callback reference.</summary>
    public ValueTask UnregisterAsync() => js.InvokeVoidAsync("respondeoInstall.unregister");
}
