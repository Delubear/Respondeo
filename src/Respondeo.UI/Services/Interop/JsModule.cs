using Microsoft.JSInterop;

namespace Respondeo.Services.Interop;

/// <summary>
/// Lazily imports one JavaScript ES module and disposes it safely.
/// Centralizes the import/invoke/dispose boilerplate (including tolerance of a torn-down JS runtime during disposal)
/// that components and interop services previously repeated by hand.
/// </summary>
public sealed class JsModule(IJSRuntime js, string path) : IAsyncDisposable
{
    private Task<IJSObjectReference>? _import;

    /// <summary>True once the import has been requested.</summary>
    public bool IsLoaded => _import is not null;

    /// <summary>Imports the module on first use and returns the cached reference thereafter.</summary>
    public Task<IJSObjectReference> GetAsync() => _import ??= js.InvokeAsync<IJSObjectReference>("import", path).AsTask();

    public async ValueTask InvokeVoidAsync(string identifier, params object?[]? args)
    {
        var module = await GetAsync();
        await module.InvokeVoidAsync(identifier, args);
    }

    public async ValueTask<T> InvokeAsync<T>(string identifier, params object?[]? args)
    {
        var module = await GetAsync();
        return await module.InvokeAsync<T>(identifier, args);
    }

    /// <summary>
    /// Invokes a void export only if the module was already imported, ignoring a disconnected runtime.
    /// Intended for teardown calls such as <c>unregister</c>.
    /// </summary>
    public async ValueTask TryInvokeVoidAsync(string identifier, params object?[]? args)
    {
        if (_import is null)
        {
            return;
        }

        try
        {
            var module = await _import;
            await module.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException)
        {
            // Runtime already gone during teardown.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_import is null)
        {
            return;
        }

        try
        {
            var module = await _import;
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Runtime already gone during teardown.
        }
    }
}
