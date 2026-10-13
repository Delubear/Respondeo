using System.Text.Json;
using Microsoft.JSInterop;

namespace Respondeo.Services.Devotions;

/// <summary>
/// A saved snapshot of an in-progress devotion: which devotion and mystery set the reader chose and how many prayer rows they had completed.
/// Enough to rebuild the same bead thread and restore the completion pointer when they return.
/// </summary>
/// <param name="DevotionId">The id of the devotion being prayed.</param>
/// <param name="SetId">The chosen mystery set id, or <c>null</c> for a single-set devotion.</param>
/// <param name="CompletedCount">How many prayer rows had been marked complete, in order.</param>
public sealed record DevotionProgress(string DevotionId, string? SetId, int CompletedCount);

/// <summary>
/// Persists the reader's place in a devotion across navigation and sessions, so an accidental (or deliberate) exit can be resumed rather than lost.
/// Only a single active devotion is tracked at a time — beginning or resuming one replaces any previously saved slot.
/// </summary>
public interface IDevotionProgressService
{
    /// <summary>
    /// Returns the saved progress for <paramref name="devotionId"/>, or <c>null</c> if there is no saved slot or it belongs to a different devotion.
    /// </summary>
    Task<DevotionProgress?> LoadAsync(string devotionId);

    /// <summary>
    /// Saves (or replaces) the active devotion slot with the given progress.
    /// </summary>
    Task SaveAsync(DevotionProgress progress);

    /// <summary>
    /// Clears the active devotion slot if it belongs to <paramref name="devotionId"/>.
    /// </summary>
    Task ClearAsync(string devotionId);
}

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
