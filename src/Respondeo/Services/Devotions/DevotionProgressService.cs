using System.Text.Json;
using Microsoft.JSInterop;

namespace Respondeo.Services.Devotions;

/// <summary>
/// An <see cref="IDevotionProgressService"/> that keeps a single active-devotion slot in the browser's <c>localStorage</c> (so progress survives navigation, refreshes and sessions).
/// The slot holds a JSON <see cref="DevotionProgress"/>; loading is scoped by devotion id so a saved slot for one devotion never leaks into another.
/// </summary>
public sealed class DevotionProgressService(IJSRuntime js) : IDevotionProgressService
{
    private const string StorageKey = "respondeo.devotion.progress";
    private readonly LocalStorage _storage = new(js);

    public async Task<DevotionProgress?> LoadAsync(string devotionId)
    {
        var stored = await _storage.GetAsync(StorageKey);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        DevotionProgress? progress;
        try
        {
            progress = JsonSerializer.Deserialize<DevotionProgress>(stored);
        }
        catch (JsonException)
        {
            // Corrupt or legacy value; treat as no saved progress.
            return null;
        }

        // Only surface the slot when it belongs to the devotion being asked about.
        return progress is not null && progress.DevotionId == devotionId ? progress : null;
    }

    public async Task SaveAsync(DevotionProgress progress)
    {
        var json = JsonSerializer.Serialize(progress);
        await _storage.SetAsync(StorageKey, json);
    }

    public async Task ClearAsync(string devotionId)
    {
        // Only clear the slot if it still belongs to this devotion, so we never wipe another devotion's in-progress state.
        var existing = await LoadAsync(devotionId);
        if (existing is not null)
        {
            await _storage.RemoveAsync(StorageKey);
        }
    }
}
