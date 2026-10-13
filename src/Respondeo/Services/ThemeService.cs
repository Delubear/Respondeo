using Microsoft.JSInterop;

namespace Respondeo.Services;

/// <summary>
/// The available color themes for the app.
/// </summary>
public enum Theme
{
    Light,
    Dark,
}

/// <summary>
/// Reads and applies the visitor's preferred color <see cref="Theme"/>, persisting the choice across sessions.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the currently applied theme. Defaults to <see cref="Theme.Light"/> until initialized.
    /// </summary>
    Theme Current { get; }

    /// <summary>
    /// Reads the persisted preference (falling back to the OS setting) and applies it to the document.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Switches between light and dark, persists the choice, and applies it to the document.
    /// </summary>
    Task ToggleAsync();
}

/// <summary>
/// An <see cref="IThemeService"/> that persists the choice in the browser's <c>localStorage</c>
/// (so it survives across sessions) and applies it by setting <c>data-theme</c> on the document root.
/// When no preference has been stored it falls back to the operating system's color scheme.
/// </summary>
public sealed class ThemeService(IJSRuntime js) : IThemeService
{
    private const string StorageKey = "respondeo.theme";
    private readonly LocalStorage _storage = new(js);

    public Theme Current { get; private set; } = Theme.Light;

    public async Task InitializeAsync()
    {
        var stored = await _storage.GetAsync(StorageKey);

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
        await _storage.SetAsync(StorageKey, Name(Current));
        await ApplyAsync();
    }

    private async Task ApplyAsync() => await js.InvokeVoidAsync("respondeoTheme.apply", Name(Current));

    private static string Name(Theme theme) => theme == Theme.Dark ? "dark" : "light";
}
