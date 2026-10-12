using Microsoft.JSInterop;

namespace Respondeo.Services.Prayers;

/// <summary>
/// An <see cref="IPrayerLanguageService"/> that persists the reader's Latin/vernacular choice in the browser's <c>localStorage</c> (so it survives across sessions),
/// modeled on <see cref="ThemeService"/>.
/// </summary>
public sealed class PrayerLanguageService(IJSRuntime js) : IPrayerLanguageService
{
    private const string StorageKey = "respondeo.prayerLanguage";
    private readonly LocalStorage _storage = new(js);

    public bool ShowLatin { get; private set; }

    public async Task InitializeAsync()
    {
        var stored = await _storage.GetAsync(StorageKey);
        ShowLatin = stored == "la";
    }

    public async Task SetAsync(bool showLatin)
    {
        ShowLatin = showLatin;
        await _storage.SetAsync(StorageKey, showLatin ? "la" : "en");
    }
}
