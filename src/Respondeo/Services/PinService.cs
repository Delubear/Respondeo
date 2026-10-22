using System.Text.Json;
using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// The visitor's pinned quick-menu destinations, persisted across sessions.
/// </summary>
public interface IPinService
{
    /// <summary>Gets the pinned hrefs, in pin order.</summary>
    IReadOnlyList<string> Pins { get; }

    /// <summary>Raised when the pins change.</summary>
    event Action? Changed;

    /// <summary>Returns whether the given href is pinned.</summary>
    bool IsPinned(string href);

    /// <summary>Loads the persisted pins (falling back to the defaults).</summary>
    Task InitializeAsync();

    /// <summary>Pins the href if unpinned, or unpins it if pinned, and persists the change.</summary>
    Task TogglePinAsync(string href);

    /// <summary>Moves a pinned href by the given offset within the pin order, and persists the change.</summary>
    Task MovePinAsync(string href, int offset);
}

/// <summary>
/// An <see cref="IPinService"/> that stores the pins as a JSON array in <c>localStorage</c>.
/// </summary>
public sealed class PinService(IJSRuntime js) : IPinService
{
    private const string StorageKey = "respondeo.pins";
    private readonly LocalStorage _storage = new(js);
    private List<string> _pins = [.. DefaultPins];
    private bool _initialized;

    /// <summary>The pins a first-time visitor starts with (none).</summary>
    public static readonly IReadOnlyList<string> DefaultPins = [];

    public IReadOnlyList<string> Pins => _pins;

    public event Action? Changed;

    public bool IsPinned(string href) => _pins.Contains(href, StringComparer.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var stored = await _storage.GetAsync(StorageKey);
        if (stored is null)
        {
            return;
        }

        try
        {
            _pins = JsonSerializer.Deserialize<List<string>>(stored) ?? [];
        }
        catch (JsonException)
        {
            _pins = [.. DefaultPins];
        }

        Changed?.Invoke();
    }

    public async Task TogglePinAsync(string href)
    {
        var existing = _pins.FindIndex(p => string.Equals(p, href, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0)
        {
            _pins.RemoveAt(existing);
        }
        else
        {
            _pins.Add(href);
        }

        await _storage.SetAsync(StorageKey, JsonSerializer.Serialize(_pins));
        Changed?.Invoke();
    }

    public async Task MovePinAsync(string href, int offset)
    {
        var index = _pins.FindIndex(p => string.Equals(p, href, StringComparison.OrdinalIgnoreCase));
        var target = index + offset;
        if (index < 0 || target < 0 || target >= _pins.Count || target == index)
        {
            return;
        }

        (_pins[index], _pins[target]) = (_pins[target], _pins[index]);
        await _storage.SetAsync(StorageKey, JsonSerializer.Serialize(_pins));
        Changed?.Invoke();
    }
}
