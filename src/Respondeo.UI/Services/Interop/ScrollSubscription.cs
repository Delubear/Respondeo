using Microsoft.JSInterop;

namespace Respondeo.Services.Interop;

/// <summary>
/// Reusable scroll-position subscription over <c>wwwroot/js/scroll.js</c>.
/// Several components (the layout's reading-progress/back-to-top control, the Summa reading guide, the prayer guide) need to react to window scroll,
/// which requires importing the module, registering a <see cref="DotNetObjectReference{T}"/>,
/// tracking a subscription id, and tearing all of that down safely during disposal.
/// This helper owns that boilerplate so each component keeps only its own scroll logic.
/// </summary>
/// <remarks>
/// A component creates one instance, calls <see cref="StartAsync"/> from its first <c>OnAfterRenderAsync</c>,
/// and awaits <see cref="DisposeAsync"/> from its own disposal.
/// The <paramref name="onScroll"/> callback receives the shared metrics: reading progress (0-100) and
/// whether the page has scrolled past the back-to-top reveal threshold.
/// </remarks>
public sealed class ScrollSubscription(IJSRuntime js) : IAsyncDisposable
{
    private DotNetObjectReference<ScrollSubscription>? _ref;
    private readonly JsModule _module = new(js, "./js/scroll.js");
    private int _subscription;
    private Action<double, bool>? _onScroll;

    /// <summary>
    /// Imports <c>scroll.js</c> and subscribes to window scroll.
    /// The <paramref name="onScroll"/> callback is invoked immediately and on every subsequent scroll with <c>(progress, backToTopVisible)</c>.
    /// </summary>
    public async Task StartAsync(Action<double, bool> onScroll)
    {
        _onScroll = onScroll;
        _ref = DotNetObjectReference.Create(this);
        _subscription = await _module.InvokeAsync<int>("register", _ref);
    }

    /// <summary>Invokes a parameterless void export on the scroll module.</summary>
    public ValueTask InvokeVoidAsync(string identifier) => _module.IsLoaded ? _module.InvokeVoidAsync(identifier) : ValueTask.CompletedTask;

    /// <summary>Invoked by scroll.js on every scroll; forwards the metrics to the component callback.</summary>
    [JSInvokable]
    public void OnScroll(double progress, bool backToTopVisible) => _onScroll?.Invoke(progress, backToTopVisible);

    public async ValueTask DisposeAsync()
    {
        await _module.TryInvokeVoidAsync("unregister", _subscription);
        await _module.DisposeAsync();
        _ref?.Dispose();
    }
}
