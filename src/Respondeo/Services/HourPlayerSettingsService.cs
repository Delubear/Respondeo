using System.Text.Json;
using Microsoft.JSInterop;
using Respondeo.LiturgyOfTheHours;
using Respondeo.Services.Interop;

namespace Respondeo.Services;

/// <summary>
/// The reader's preferences for the Liturgy of the Hours player.
/// </summary>
public sealed record HourPlayerSettings
{
    /// <summary>Open every part's text when the Hour loads.</summary>
    public bool ExpandAll { get; init; }

    /// <summary>Say the invitatory antiphon only before the psalm (and after the Glory Be), not after every stanza.</summary>
    public bool AntiphonAtStartOnly { get; init; }

    /// <summary>How the morning invitatory psalm is chosen.</summary>
    public InvitatoryMode InvitatoryMode { get; init; } = InvitatoryMode.ShowAll;

    /// <summary>The key of the psalm used in <see cref="InvitatoryMode.Specific"/> mode.</summary>
    public string? InvitatoryPsalm { get; init; }

    /// <summary>The position in the cycle used in <see cref="InvitatoryMode.Cycle"/> mode.</summary>
    public int InvitatoryCycleIndex { get; init; }

    /// <summary>The day the cycle index was last advanced.</summary>
    public DateOnly? InvitatoryCycleDate { get; init; }
}

/// <summary>
/// Reads, persists, and broadcasts the reader's <see cref="HourPlayerSettings"/>.
/// </summary>
public interface IHourPlayerSettingsService
{
    /// <summary>The current settings. Defaults until <see cref="LoadAsync"/> completes.</summary>
    HourPlayerSettings Current { get; }

    /// <summary>Raised after the settings change.</summary>
    event Action? Changed;

    /// <summary>Reads the persisted settings, falling back to the defaults when missing or corrupt.</summary>
    Task LoadAsync();

    /// <summary>Applies <paramref name="update"/>, persists the result, and raises <see cref="Changed"/>.</summary>
    Task UpdateAsync(Func<HourPlayerSettings, HourPlayerSettings> update);
}

/// <summary>
/// An <see cref="IHourPlayerSettingsService"/> that stores the settings as JSON in the browser's <c>localStorage</c>.
/// </summary>
public sealed class HourPlayerSettingsService(IJSRuntime js) : IHourPlayerSettingsService
{
    internal const string StorageKey = "respondeo.hours.settings";
    private readonly LocalStorage _storage = new(js);
    private bool _loaded;

    public HourPlayerSettings Current { get; private set; } = new();

    public event Action? Changed;

    public async Task LoadAsync()
    {
        if (_loaded)
        {
            return;
        }

        Current = Deserialize(await _storage.GetAsync(StorageKey));
        _loaded = true;
    }

    public async Task UpdateAsync(Func<HourPlayerSettings, HourPlayerSettings> update)
    {
        Current = update(Current);
        await _storage.SetAsync(StorageKey, JsonSerializer.Serialize(Current));
        Changed?.Invoke();
    }

    internal static HourPlayerSettings Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new();
        }

        try
        {
            return JsonSerializer.Deserialize<HourPlayerSettings>(json) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }
}
