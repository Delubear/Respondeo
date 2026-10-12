using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// A thin typed wrapper over the browser's <c>localStorage</c>, so persistence services share one
/// interop surface instead of each repeating the raw JS identifiers.
/// </summary>
internal sealed class LocalStorage(IJSRuntime js)
{
    public ValueTask<string?> GetAsync(string key) => js.InvokeAsync<string?>("localStorage.getItem", key);

    public ValueTask SetAsync(string key, string value) => js.InvokeVoidAsync("localStorage.setItem", key, value);

    public ValueTask RemoveAsync(string key) => js.InvokeVoidAsync("localStorage.removeItem", key);
}
