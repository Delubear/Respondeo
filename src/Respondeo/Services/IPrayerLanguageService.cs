namespace Respondeo.Services;

/// <summary>
/// Remembers whether the reader prefers to see devotion prayers by their Latin names, persisting the
/// choice in the browser's <c>localStorage</c> so it survives navigation and future sessions.
/// </summary>
public interface IPrayerLanguageService
{
    /// <summary>Whether the Latin view is currently preferred.</summary>
    bool ShowLatin { get; }

    /// <summary>Loads the stored preference (call once before first use).</summary>
    Task InitializeAsync();

    /// <summary>Sets and persists the preference.</summary>
    Task SetAsync(bool showLatin);
}
