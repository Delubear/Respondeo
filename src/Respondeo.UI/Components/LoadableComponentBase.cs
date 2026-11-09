using Microsoft.AspNetCore.Components;

namespace Respondeo.Components;

/// <summary>
/// Base for components that load one value whenever their parameters change. Tracks the loading and
/// error states, and drops a stale result when a newer load starts before the previous one finishes.
/// </summary>
public abstract class LoadableComponentBase<T> : ComponentBase, IDisposable
{
    private CancellationTokenSource? _cts;

    protected T? Value { get; private set; }

    protected bool IsLoading { get; private set; } = true;

    protected Exception? Error { get; private set; }

    protected abstract Task<T?> LoadAsync(CancellationToken cancellationToken);

    protected override Task OnParametersSetAsync() => ReloadAsync();

    protected async Task ReloadAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        var cts = _cts = new CancellationTokenSource();

        IsLoading = true;
        Error = null;
        try
        {
            var value = await LoadAsync(cts.Token);
            if (cts.IsCancellationRequested)
            {
                return;
            }

            Value = value;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            Value = default;
            Error = ex;
        }

        IsLoading = false;
    }

    public virtual void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        GC.SuppressFinalize(this);
    }
}
