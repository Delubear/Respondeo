using Microsoft.JSInterop;
using Respondeo.Bible;
using Respondeo.Services.Interop;

namespace Respondeo.Services;

/// <summary>
/// Remembers which Bible translation the reader prefers,
/// persisting the choice in the browser's <c>localStorage</c> so it survives navigation and future sessions.
/// </summary>
public interface IBibleTranslationService
{
    /// <summary>The currently preferred translation.</summary>
    BibleTranslation Current { get; }

    /// <summary>Loads the stored preference (call once before first use).</summary>
    Task InitializeAsync();

    /// <summary>Sets and persists the preference.</summary>
    Task SetAsync(BibleTranslation translation);
}

/// <summary>An <see cref="IBibleTranslationService"/> backed by <c>localStorage</c>, modeled on <see cref="Prayers.PrayerLanguageService"/>.</summary>
public sealed class BibleTranslationService(IJSRuntime js) : IBibleTranslationService
{
    private const string StorageKey = "respondeo.bibleTranslation";
    private readonly LocalStorage _storage = new(js);

    public BibleTranslation Current { get; private set; }

    public async Task InitializeAsync()
    {
        Current = BibleTranslations.Parse(await _storage.GetAsync(StorageKey)) ?? BibleTranslation.DouayRheims;
    }

    public async Task SetAsync(BibleTranslation translation)
    {
        Current = translation;
        await _storage.SetAsync(StorageKey, translation.Code());
    }
}
