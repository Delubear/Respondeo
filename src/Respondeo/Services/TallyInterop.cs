using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// Typed wrapper over the <c>window.respondeoTally</c> JS helper (see <c>wwwroot/js/site.js</c>) that
/// relays the embedded Tally feedback form's <c>Tally.FormSubmitted</c> window message back to Blazor.
/// </summary>
/// <remarks>
/// Centralising these calls keeps the JS method names in one place instead of scattering magic strings across component code,
/// matching how <see cref="NavigationInterop"/> and the other services wrap their JS.
/// Callers that run during teardown keep their own <see cref="JSDisconnectedException"/> handling.
/// </remarks>
public sealed class TallyInterop(IJSRuntime js)
{
    /// <summary>
    /// Starts listening for the embedded Tally form's submission, invoking <paramref name="callback"/>'s
    /// <c>[JSInvokable]</c> <c>OnSubmitted</c> when it fires. Returns a subscription id for <see cref="StopListeningAsync"/>.
    /// </summary>
    public ValueTask<int> ListenAsync<T>(DotNetObjectReference<T> callback) where T : class =>
        js.InvokeAsync<int>("respondeoTally.listen", callback);

    /// <summary>Stops the subscription created by <see cref="ListenAsync{T}"/> so the window listener is removed.</summary>
    public ValueTask StopListeningAsync(int id) => js.InvokeVoidAsync("respondeoTally.stopListening", id);
}
