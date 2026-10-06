using Microsoft.JSInterop;

namespace Respondeo.Services.Prayers;

/// <summary>
/// An <see cref="IPrayerLanguageService"/> that persists the reader's Latin/vernacular choice in the browser's <c>localStorage</c> (so it survives across sessions),
/// modeled on <see cref="ThemeService"/>.
/// </summary>
public sealed class PrayerLanguageService(IJSRuntime js) : IPrayerLanguageService
{
    private const string StorageKey = "respondeo.prayerLanguage";

    public bool ShowLatin { get; private set; }

    public async Task InitializeAsync()
    {
        var stored = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        ShowLatin = stored == "la";
    }

    public async Task SetAsync(bool showLatin)
    {
        ShowLatin = showLatin;
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, showLatin ? "la" : "en");
    }
}
