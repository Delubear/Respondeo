using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// Watches for the calendar day rolling over while the app sits open in a browser tab,
/// so date-driven surfaces (the liturgical orb, the masthead seasonal portrait, the saint of the day)
/// can refresh themselves in place instead of silently showing yesterday's date after a tab is left open for hours or days.
/// Everything here runs client-side, so without this a long-lived tab never re-reads "today".
/// </summary>
/// <remarks>
/// Instead of a background timer (which would wake an idle tab all night for no benefit, since the only thing
/// that changes is the date at midnight), the watcher re-checks the date when the user returns to the tab, via
/// the browser's <c>visibilitychange</c> event. That is exactly the "left it open overnight" case, and a hidden
/// tab stays completely quiet, which respects battery life.
/// </remarks>
public interface IDayWatcher : IAsyncDisposable
{
    /// <summary>The date the watcher last observed; updated immediately before <see cref="DayChanged"/> fires.</summary>
    DateOnly Today { get; }

    /// <summary>
    /// Raised once each time the current date changes (typically detected when the tab becomes visible after midnight).
    /// Subscribers should re-read the date and re-render; handlers may be invoked from a JS interop callback,
    /// so Blazor components must marshal back to the renderer with <c>InvokeAsync</c>.
    /// </summary>
    event Action? DayChanged;

    /// <summary>
    /// Begins listening for tab visibility changes. Call once from a long-lived host component
    /// (the layout) in its first <c>OnAfterRenderAsync</c>; calling again is a no-op.
    /// </summary>
    Task StartAsync();
}

/// <summary>
/// Default <see cref="IDayWatcher"/>: re-reads the injected clock whenever the tab becomes visible and raises
/// <see cref="DayChanged"/> only when the <see cref="DateOnly"/> actually advances, so subscribers wake at most
/// once per rollover rather than on every visibility change.
/// The current date is injected (not read directly) so the rollover logic stays deterministic and unit-testable.
/// </summary>
/// <param name="today">Supplies the current date (the same source the date-driven services use).</param>
/// <param name="js">Used to import the visibility module and register the rollover callback.</param>
internal sealed class DayWatcher(Func<DateOnly> today, IJSRuntime js) : IDayWatcher
{
    private readonly Func<DateOnly> _today = today;
    private DotNetObjectReference<DayWatcher>? _ref;
    private IJSObjectReference? _module;
    private bool _disposed;

    public DateOnly Today { get; private set; } = today();

    public event Action? DayChanged;

    public async Task StartAsync()
    {
        if (_disposed || _module is not null)
        {
            return;
        }

        _module = await js.InvokeAsync<IJSObjectReference>("import", "./js/day-watcher.js");
        _ref = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("register", _ref);
    }

    /// <summary>Invoked by day-watcher.js when the tab becomes visible; re-checks the date.</summary>
    [JSInvokable]
    public void OnBecameVisible() => Poll();

    // Compares the clock to the last observed date and raises DayChanged once when it advances.
    // Extracted from the interop callback so rollover behavior is unit-testable without a real browser event.
    internal void Poll()
    {
        if (_disposed)
        {
            return;
        }

        var current = _today();
        if (current == Today)
        {
            return;
        }

        Today = current;
        DayChanged?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("unregister");
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The runtime is already gone during teardown; nothing to unregister.
            }
        }

        _ref?.Dispose();
    }
}
