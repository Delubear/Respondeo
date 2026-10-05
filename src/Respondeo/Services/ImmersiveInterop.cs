using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// Typed wrapper over the window-level immersive-mode helper (see <c>wwwroot/js/site.js</c>) that toggles the <c>is-immersive</c>
/// body class so a full-screen praying view can hide the surrounding chrome.
/// </summary>
/// <remarks>
/// Centralising these calls keeps the JS method names in one place instead of scattering magic strings across page code,
/// matching how <see cref="NavigationInterop"/> and the other services wrap their JS.
/// Callers that run during teardown keep their own <see cref="JSDisconnectedException"/> handling.
/// </remarks>
public sealed class ImmersiveInterop(IJSRuntime js)
{
    /// <summary>Enters immersive mode, hiding the surrounding chrome for a focused praying view.</summary>
    public ValueTask EnterAsync() => js.InvokeVoidAsync("respondeoImmersive.enter");

    /// <summary>Leaves immersive mode, restoring the surrounding chrome.</summary>
    public ValueTask LeaveAsync() => js.InvokeVoidAsync("respondeoImmersive.leave");
}
