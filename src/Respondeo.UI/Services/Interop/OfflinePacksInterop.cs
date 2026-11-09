using Microsoft.JSInterop;

namespace Respondeo.Services.Interop;

/// <summary>
/// Typed wrapper over the <c>window.respondeoPacks</c> JS module (see <c>wwwroot/js/offline-packs.js</c>)
/// that talks to the service worker about the opt-in offline content packs (the Summa and the Bible).
/// </summary>
public sealed class OfflinePacksInterop(IJSRuntime js)
{
    /// <summary>Registers a callback target and returns whether packs can be managed (a published worker controls the page).</summary>
    public ValueTask<bool> RegisterAsync<T>(DotNetObjectReference<T> target) where T : class => js.InvokeAsync<bool>("respondeoPacks.register", target);

    /// <summary>Releases a registered callback target.</summary>
    public ValueTask UnregisterAsync<T>(DotNetObjectReference<T> target) where T : class => js.InvokeVoidAsync("respondeoPacks.unregister", target);

    /// <summary>Asks the worker to save a pack for offline use.</summary>
    public ValueTask DownloadAsync(string pack) => js.InvokeVoidAsync("respondeoPacks.download", pack);

    /// <summary>Asks the worker to delete a saved pack.</summary>
    public ValueTask RemoveAsync(string pack) => js.InvokeVoidAsync("respondeoPacks.remove", pack);

    /// <summary>Whether the browser currently reports a network connection.</summary>
    public ValueTask<bool> IsOnlineAsync() => js.InvokeAsync<bool>("respondeoPacks.isOnline");

    /// <summary>Whether the one-time "save for offline" prompt should show (installed app, not yet asked).</summary>
    public ValueTask<bool> ShouldPromptAsync() => js.InvokeAsync<bool>("respondeoPacks.shouldPrompt");

    /// <summary>Remembers that the one-time prompt has been answered.</summary>
    public ValueTask MarkPromptedAsync() => js.InvokeVoidAsync("respondeoPacks.markPrompted");
}
