namespace Respondeo.Services;

/// <summary>
/// A saved snapshot of an in-progress devotion: which devotion and mystery set the reader chose and
/// how many prayer rows they had completed. Enough to rebuild the same bead thread and restore the
/// completion pointer when they return.
/// </summary>
/// <param name="DevotionId">The id of the devotion being prayed.</param>
/// <param name="SetId">The chosen mystery set id, or <c>null</c> for a single-set devotion.</param>
/// <param name="CompletedCount">How many prayer rows had been marked complete, in order.</param>
public sealed record DevotionProgress(string DevotionId, string? SetId, int CompletedCount);

/// <summary>
/// Persists the reader's place in a devotion across navigation and sessions, so an accidental (or
/// deliberate) exit can be resumed rather than lost. Only a single active devotion is tracked at a
/// time — beginning or resuming one replaces any previously saved slot.
/// </summary>
public interface IDevotionProgressService
{
    /// <summary>
    /// Returns the saved progress for <paramref name="devotionId"/>, or <c>null</c> if there is no
    /// saved slot or it belongs to a different devotion.
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
