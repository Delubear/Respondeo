using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// An <see cref="IThemeService"/> that persists the choice in the browser's <c>localStorage</c>
/// (so it survives across sessions) and applies it by setting <c>data-theme</c> on the document root.
/// When no preference has been stored it falls back to the operating system's color scheme.
/// </summary>
public sealed class ThemeService(IJSRuntime js) : IThemeService
{
    private const string StorageKey = "respondeo.theme";

    public Theme Current { get; private set; } = Theme.Light;

    public async Task InitializeAsync()
    {
        var stored = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);

        Current = stored switch
        {
            "dark" => Theme.Dark,
            _ => Theme.Light,
        };

        await ApplyAsync();
    }

    public async Task ToggleAsync()
    {
        Current = Current == Theme.Dark ? Theme.Light : Theme.Dark;
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, Name(Current));
        await ApplyAsync();
    }

    private async Task ApplyAsync() => await js.InvokeVoidAsync("respondeoTheme.apply", Name(Current));

    private static string Name(Theme theme) => theme == Theme.Dark ? "dark" : "light";
}
