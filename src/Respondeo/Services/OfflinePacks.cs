using Microsoft.JSInterop;
using Respondeo.Services.Interop;

namespace Respondeo.Services;

/// <summary>
/// App-wide state for the opt-in offline content packs. One instance listens to the service worker
/// (through <see cref="OfflinePacksInterop"/>) and raises <see cref="Changed"/> so every pack control stays in sync.
/// </summary>
public sealed class OfflinePacks(OfflinePacksInterop interop) : IAsyncDisposable
{
    public const string Summa = "summa";
    public const string Bible = "bible";

    private readonly Dictionary<string, PackState> _states = new(StringComparer.Ordinal)
    {
        [Summa] = new PackState(),
        [Bible] = new PackState()
    };

    private DotNetObjectReference<OfflinePacks>? _ref;
    private Task? _start;

    /// <summary>Whether packs can be managed here (false in development or browsers without a controlling worker).</summary>
    public bool Supported { get; private set; }

    /// <summary>Raised whenever any pack's state changes.</summary>
    public event Action? Changed;

    /// <summary>Starts listening to the worker; safe to call repeatedly.</summary>
    public Task StartAsync() => _start ??= StartCoreAsync();

    private async Task StartCoreAsync()
    {
        _ref = DotNetObjectReference.Create(this);
        Supported = await interop.RegisterAsync(_ref);
        Changed?.Invoke();
    }

    public PackState Get(string pack) => _states[pack];

    public async Task DownloadAsync(string pack)
    {
        var state = _states[pack];
        state.Downloading = true;
        state.Error = null;
        state.Done = 0;
        Changed?.Invoke();
        await interop.DownloadAsync(pack);
    }

    public async Task RemoveAsync(string pack) => await interop.RemoveAsync(pack);

    public ValueTask<bool> IsOnlineAsync() => interop.IsOnlineAsync();

    public ValueTask<bool> ShouldPromptAsync() => interop.ShouldPromptAsync();

    public ValueTask MarkPromptedAsync() => interop.MarkPromptedAsync();

    [JSInvokable]
    public void OnPackStatus(Dictionary<string, PackStatusDto> packs)
    {
        foreach (var (name, status) in packs)
        {
            if (_states.TryGetValue(name, out var state))
            {
                state.Installed = status.Installed && !status.Stale;
                state.Total = status.AssetCount;
                if (state.Installed)
                {
                    state.Downloading = false;
                }
            }
        }
        Changed?.Invoke();
    }

    [JSInvokable]
    public void OnPackProgress(PackProgressDto progress)
    {
        if (_states.TryGetValue(progress.Pack, out var state))
        {
            state.Downloading = progress.Done < progress.Total;
            state.Done = progress.Done;
            state.Total = progress.Total;
            Changed?.Invoke();
        }
    }

    [JSInvokable]
    public void OnPackError(PackErrorDto error)
    {
        if (_states.TryGetValue(error.Pack, out var state))
        {
            state.Downloading = false;
            state.Error = "The download did not finish. Check your connection and try again.";
            Changed?.Invoke();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_ref is not null)
        {
            try
            {
                await interop.UnregisterAsync(_ref);
            }
            catch (JSDisconnectedException)
            {
                // The runtime is going away; nothing to unregister.
            }
            _ref.Dispose();
        }
    }

    public sealed class PackState
    {
        public bool Installed { get; set; }
        public bool Downloading { get; set; }
        public int Done { get; set; }
        public int Total { get; set; }
        public string? Error { get; set; }
        public int Percent => Total == 0 ? 0 : (int)Math.Round(100.0 * Done / Total);
    }

    public sealed record PackStatusDto(bool Installed, bool Stale, int AssetCount);

    public sealed record PackProgressDto(string Pack, int Done, int Total);

    public sealed record PackErrorDto(string Pack, string? Message);
}
